#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

// Script-only validation. Does not build scenes, copy assets/models, start a Player, or publish a package.
// Use a dedicated idle Editor: -batchmode -buildTarget Win64 -executeMethod WindowsPlayerScriptsCompileQa.Run -quit
public static class WindowsPlayerScriptsCompileQa
{
    [Serializable] sealed class Report
    {
        public string status="running",error="",unityVersion,target="StandaloneWindows64",output;
        public string scope="Player scripts only; verifies native Windows imports in compiled metadata. Does not validate OS calls at runtime.";
        public List<string> assemblies=new List<string>(),nativeImports=new List<string>(),errors=new List<string>();
    }
    [MenuItem("Tools/Local Verification/Windows player scripts only")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Wait until the Editor is idle in Edit Mode.");
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)
            throw new InvalidOperationException("Start the dedicated Editor with -buildTarget Win64; this check never switches an active workspace target.");
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))
            throw new InvalidOperationException("Windows player build support is not installed.");
        string output=Path.GetFullPath(Path.Combine("Temp","WindowsPlayerScriptsQa",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(output);
        var report=new Report{unityVersion=Application.unityVersion,output=output};
        Application.LogCallback capture=(message,stack,kind)=>{if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)report.errors.Add(message+"\n"+stack);};
        Exception failure=null;
        Application.logMessageReceived+=capture;
        try
        {
            var settings=new ScriptCompilationSettings {
                target=BuildTarget.StandaloneWindows64,
                group=BuildTargetGroup.Standalone,
                subtarget=(int)StandaloneBuildSubtarget.Player,
                options=ScriptCompilationOptions.None
            };
            var result=PlayerBuildInterface.CompilePlayerScripts(settings,output);
            try
            {
                if(result.assemblies!=null)foreach(var assembly in result.assemblies)report.assemblies.Add(assembly);
                if(report.assemblies.Count==0||report.errors.Count>0)throw new InvalidOperationException("Windows player script compilation produced no assemblies or logged errors.");
                var matches=Directory.GetFiles(output,"Assembly-CSharp.dll",SearchOption.AllDirectories);
                if(matches.Length!=1)throw new InvalidOperationException("Expected one generated Assembly-CSharp.dll, found "+matches.Length);
                InspectWindowsBranch(matches[0],report.nativeImports);
                report.status="passed";
            }
            finally{result.typeDB?.Dispose();}
        }
        catch(Exception e){failure=e;report.status="failed";report.error=e.ToString();}
        finally
        {
            Application.logMessageReceived-=capture;
            File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(report,true));
            string reports=Path.GetFullPath("LocalVerification/WindowsPlayerScriptsQa");Directory.CreateDirectory(reports);
            File.WriteAllText(Path.Combine(reports,"report.json"),JsonUtility.ToJson(report,true));
        }
        Debug.Log("WindowsPlayerScriptsCompileQa "+report.status+": "+Path.Combine(output,"report.json"));
        if(failure!=null)throw new InvalidOperationException("Windows player scripts check failed; see report.json",failure);
    }
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static void InspectWindowsBranch(string path,List<string> imports)
    {
        // Read IL metadata without loading the compiled game assembly or invoking any Win32 method.
        // Unity ships this Cecil build; use reflection to avoid a new project assembly dependency.
        string cecil=Path.Combine(EditorApplication.applicationContentsPath,"Managed","Unity.Cecil.dll");
        if(!File.Exists(cecil))throw new FileNotFoundException("Bundled Unity.Cecil is unavailable",cecil);
        var type=Assembly.LoadFrom(cecil).GetType("Mono.Cecil.AssemblyDefinition",true);
        var definition=type.GetMethod("ReadAssembly",new[]{typeof(string)}).Invoke(null,new object[]{path});
        try
        {
            object found=null;
            foreach(var candidate in (IEnumerable)Property(Property(definition,"MainModule"),"Types"))
                if((string)Property(candidate,"FullName")=="LingGuangV05.Desktop.XingGuang.XgIdleNativeWindow"){found=candidate;break;}
            if(found==null)throw new InvalidOperationException("Native compact window type was not compiled into the Player.");
            var expected=new HashSet<string>{"SystemParametersInfo","SetWindowPos","GetWindowRect","GetWindowLong","SetWindowLong","ReleaseCapture","SendMessage"};
            foreach(var method in (IEnumerable)Property(found,"Methods"))
            {
                string name=(string)Property(method,"Name");if(!expected.Contains(name))continue;
                var pinvoke=Property(method,"PInvokeInfo");
                if(pinvoke==null)throw new InvalidOperationException(name+" exists but is not a native import.");
                string module=(string)Property(Property(pinvoke,"Module"),"Name");
                if(!module.Equals("user32.dll",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(name+" has unexpected import library "+module);
                imports.Add(name+" -> "+module);expected.Remove(name);
            }
            if(expected.Count!=0)throw new InvalidOperationException("Missing Windows-only imports: "+string.Join(", ",expected));
        }
        finally{(definition as IDisposable)?.Dispose();}
    }
}
#endif
