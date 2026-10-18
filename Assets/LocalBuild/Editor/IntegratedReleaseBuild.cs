using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// Builds beside earlier releases; keeps the existing company/product save identity.
public static class IntegratedReleaseBuild
{
    public static void Run()
    {
        string oldVersion=PlayerSettings.bundleVersion;
        bool oldDefaultGraphics=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
        var oldGraphics=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
        bool oldGraphicsJobs=PlayerSettings.graphicsJobs;
        var oldBackend=PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        string version="2026.10.18.1";
        string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        string directory=Path.GetFullPath("Build/MyComputerAlive-"+version+"-"+stamp);
        string[] scenes={"Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity","Assets/LingGuangV05/Scenes/Main.unity"};
        foreach(var scene in scenes)if(!File.Exists(scene))throw new FileNotFoundException(scene);
        Directory.CreateDirectory(directory);
        var provenance=CaptureSource();
        File.WriteAllText(Path.Combine(directory,"source-provenance.json"),JsonUtility.ToJson(provenance,true));
        try
        {
            PlayerSettings.bundleVersion=version;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            PlayerSettings.graphicsJobs=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(directory,"MyComputerAlive.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            var record=new Record{version=version,sourceCommit=provenance.commit,sourceDigest=provenance.digest,sourceDirty=provenance.dirty,utc=DateTime.UtcNow.ToString("o"),result=result.summary.result.ToString(),errors=result.summary.totalErrors,warnings=result.summary.totalWarnings,path=directory,company=PlayerSettings.companyName,product=PlayerSettings.productName};
            File.WriteAllText(Path.Combine(directory,"build-result.json"),JsonUtility.ToJson(record,true));
            Directory.CreateDirectory("LocalVerification/Release");
            File.WriteAllText("LocalVerification/Release/latest-build.json",JsonUtility.ToJson(record,true));
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Integrated Windows build failed: "+directory);
            Debug.Log("INTEGRATED_RELEASE_BUILD_PASS "+directory);
        }
        finally
        {
            PlayerSettings.bundleVersion=oldVersion;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,oldGraphics);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,oldDefaultGraphics);
            PlayerSettings.graphicsJobs=oldGraphicsJobs;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,oldBackend);
            AssetDatabase.SaveAssets();
        }
    }
    // Hash the working files, including untracked source, rather than pretending HEAD contains dirty edits.
    // Large binary payloads are listed separately; the model has its own SHA256SUMS.txt.
    static SourceProvenance CaptureSource()
    {
        var entries=new List<SourceEntry>();
        var omitted=new List<string>();
        foreach(string root in new[]{"Assets","Packages","ProjectSettings","Design"})
        {
            if(!Directory.Exists(root))continue;
            foreach(string file in Directory.GetFiles(root,"*",SearchOption.AllDirectories))
            {
                string path=file.Replace('\\','/');
                var info=new FileInfo(file);
                if((info.Attributes & FileAttributes.ReparsePoint)!=0 || info.Length>16L*1024*1024 ||
                    path.EndsWith(".gguf",StringComparison.OrdinalIgnoreCase))
                {omitted.Add(path+" ("+info.Length+" bytes)");continue;}
                using(var input=File.OpenRead(file))
                using(var sha=SHA256.Create())entries.Add(new SourceEntry{path=path,sha256=Hex(sha.ComputeHash(input)),bytes=info.Length});
            }
        }
        entries.Sort((a,b)=>StringComparer.Ordinal.Compare(a.path,b.path));
        omitted.Sort(StringComparer.Ordinal);
        var canonical=new StringBuilder();
        foreach(var entry in entries)canonical.Append(entry.path).Append('\t').Append(entry.bytes.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t').Append(entry.sha256).Append('\n');
        string digest;
        using(var sha=SHA256.Create())digest=Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString())));
        string status=Git("status --porcelain --untracked-files=normal -- Assets Packages ProjectSettings Design");
        return new SourceProvenance{commit=Git("rev-parse HEAD").Trim(),dirty=!string.IsNullOrWhiteSpace(status),gitStatus=status,digest=digest,algorithm="SHA256 of ordinal path TAB decimal size TAB file SHA256 LF; roots Assets, Packages, ProjectSettings, Design; excluded GGUF and files >16 MiB",files=entries.ToArray(),omitted=omitted.ToArray()};
    }
    static string Hex(byte[] value){return BitConverter.ToString(value).Replace("-","").ToLowerInvariant();}
    static string Git(string arguments)
    {
        using(var process=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git",arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,WorkingDirectory=Directory.GetCurrentDirectory()}))
        {
            if(process==null)throw new InvalidOperationException("Cannot capture Git provenance");
            string output=process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if(process.ExitCode!=0)throw new InvalidOperationException("Cannot capture Git provenance: "+arguments);
            return output;
        }
    }
    [Serializable] sealed class SourceEntry{public string path,sha256;public long bytes;}
    [Serializable] sealed class SourceProvenance{public string commit,digest,algorithm,gitStatus;public bool dirty;public SourceEntry[] files;public string[] omitted;}
    [Serializable] sealed class Record{public string version,utc,result,path,company,product,sourceCommit,sourceDigest;public bool sourceDirty;public int errors,warnings;}
}
