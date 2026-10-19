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
using LingGuangV05.Desktop.LLM;

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
        var oldModels=LLMUnity.LLMManager.modelEntries;
        bool oldDownload=LLMUnity.LLMManager.downloadOnStart;
        string managerPath=Path.Combine(Application.streamingAssetsPath,"LLMManager.json");
        byte[] oldManager=File.Exists(managerPath)?File.ReadAllBytes(managerPath):null;
        byte[] oldManagerMeta=File.Exists(managerPath+".meta")?File.ReadAllBytes(managerPath+".meta"):null;
        string version="2026.10.19.5";
        string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        string directory=Path.GetFullPath("Build/MyComputerAlive-"+version+"-"+stamp);
        string[] scenes={"Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity","Assets/LingGuangV05/Scenes/Main.unity"};
        foreach(var scene in scenes)if(!File.Exists(scene))throw new FileNotFoundException(scene);
        Directory.CreateDirectory(directory);
        var provenance=CaptureSource();
        File.WriteAllText(Path.Combine(directory,"source-provenance.json"),JsonUtility.ToJson(provenance,true));
        try
        {
            // Build from one explicit release entry; stale shared-editor presets must not
            // copy an older model. Restore the original list and disk config in finally.
            const string modelName=LocalModelRelease.FileName;
            string modelRelative="LingGuang/Models/"+modelName;
            string modelSource=Path.Combine(Application.streamingAssetsPath,modelRelative);
            if(!File.Exists(modelSource))throw new FileNotFoundException("Release model missing",modelSource);
            VerifyModel(modelSource);
            LLMUnity.LLMManager.modelEntries=new List<LLMUnity.ModelEntry>{new LLMUnity.ModelEntry(modelSource){filename=modelRelative,path=modelSource,includeInBuild=true}};
            LLMUnity.LLMManager.downloadOnStart=false;
            PlayerSettings.bundleVersion=version;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            PlayerSettings.graphicsJobs=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(directory,"MyComputerAlive.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(result.summary.result==BuildResult.Succeeded)
            {
                string outputAssets=Path.Combine(directory,"MyComputerAlive_Data/StreamingAssets");
                string expected=Path.GetFullPath(Path.Combine(outputAssets,modelRelative));
                string[] models=Directory.GetFiles(outputAssets,"*.gguf",SearchOption.AllDirectories);
                if(models.Length!=1||!string.Equals(Path.GetFullPath(models[0]),expected,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Release requires exactly the canonical Qwen GGUF; unexpected model output: "+string.Join(", ",models));
                VerifyModel(expected);
            }
            var record=new Record{version=version,model=LocalModelRelease.Name,modelSha256=LocalModelRelease.Sha256,modelBytes=LocalModelRelease.Bytes,sourceCommit=provenance.commit,sourceDigest=provenance.digest,sourceDirty=provenance.dirty,utc=DateTime.UtcNow.ToString("o"),result=result.summary.result.ToString(),errors=result.summary.totalErrors,warnings=result.summary.totalWarnings,path=directory,company=PlayerSettings.companyName,product=PlayerSettings.productName};
            File.WriteAllText(Path.Combine(directory,"build-result.json"),JsonUtility.ToJson(record,true));
            Directory.CreateDirectory("LocalVerification/Release");
            File.WriteAllText("LocalVerification/Release/latest-build.json",JsonUtility.ToJson(record,true));
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Integrated Windows build failed: "+directory);
            Debug.Log("INTEGRATED_RELEASE_BUILD_PASS "+directory);
        }
        finally
        {
            // Drain the package's queued cleanup before restoring original files; its
            // later delayed Reset then has no temporary-file entries left to remove.
            try { LLMUnity.LLMBuilder.Reset(); }
            finally
            {
                LLMUnity.LLMManager.modelEntries=oldModels;
                LLMUnity.LLMManager.downloadOnStart=oldDownload;
                RestoreFile(managerPath,oldManager);
                RestoreFile(managerPath+".meta",oldManagerMeta);
            }
            PlayerSettings.bundleVersion=oldVersion;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,oldGraphics);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,oldDefaultGraphics);
            PlayerSettings.graphicsJobs=oldGraphicsJobs;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,oldBackend);
            AssetDatabase.SaveAssets();
        }
    }
    static void RestoreFile(string path,byte[] bytes)
    {
        if(bytes!=null)File.WriteAllBytes(path,bytes);
        else if(File.Exists(path))File.Delete(path);
    }
    static void VerifyModel(string path)
    {
        if(new FileInfo(path).Length!=LocalModelRelease.Bytes)throw new InvalidDataException("Release model size mismatch: "+path);
        using(var input=File.OpenRead(path))
        using(var sha=SHA256.Create())
            if(Hex(sha.ComputeHash(input))!=LocalModelRelease.Sha256)throw new InvalidDataException("Release model checksum mismatch: "+path);
    }
    // Hash the working files, including untracked source, rather than pretending HEAD contains dirty edits.
    // Large binary payloads are listed separately; the model has its own SHA256SUMS.txt.
    static SourceProvenance CaptureSource()
    {
        var entries=new List<SourceEntry>();
        const string auraLogs="Packages/Aura/Editor/AuraLogs/";
        // Aura keeps its generated console log open while the Editor runs. It is
        // already gitignored and is neither source nor a reproducible build input.
        var omitted=new List<string>{auraLogs+" (generated Editor diagnostics; excluded)"};
        foreach(string root in new[]{"Assets","Packages","ProjectSettings","Design"})
        {
            if(!Directory.Exists(root))continue;
            foreach(string file in Directory.GetFiles(root,"*",SearchOption.AllDirectories))
            {
                string path=file.Replace('\\','/');
                if(path.StartsWith(auraLogs,StringComparison.OrdinalIgnoreCase))continue;
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
        return new SourceProvenance{commit=Git("rev-parse HEAD").Trim(),dirty=!string.IsNullOrWhiteSpace(status),gitStatus=status,digest=digest,algorithm="SHA256 of ordinal path TAB decimal size TAB file SHA256 LF; roots Assets, Packages, ProjectSettings, Design; excluded generated Packages/Aura/Editor/AuraLogs/, GGUF and files >16 MiB",files=entries.ToArray(),omitted=omitted.ToArray()};
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
    [Serializable] sealed class Record{public string version,utc,result,path,company,product,sourceCommit,sourceDigest,model,modelSha256;public long modelBytes;public bool sourceDirty;public int errors,warnings;}
}
