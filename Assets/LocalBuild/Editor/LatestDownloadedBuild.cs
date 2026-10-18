using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class LatestDownloadedBuild
{
    public static void Run()
    {
        string version = PlayerSettings.bundleVersion;
        var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        try
        {
            PlayerSettings.bundleVersion = "2026.10.17.1";
            // Unity 6000.4.5f1 crashes in D3D12ScratchAllocator::DestroyScratch on this Windows build.
            // Pin the previously working Windows renderer for every build made with this entry point.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.graphicsJobs = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            string output = Path.GetFullPath("Build/MyComputerAlive-20261016-1dd68c6/MyComputerAlive.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "build-result.txt"),
                "Source: 1dd68c6de916c7175a07918d5cc4bcab7d0348a4\nVersion: 2026.10.17.1\n" +
                "Result: " + report.summary.result + "\nErrors: " + report.summary.totalErrors +
                "\nWarnings: " + report.summary.totalWarnings + "\nUTC: " + DateTime.UtcNow.ToString("o"));
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Latest Windows build failed");
            Debug.Log("LATEST_WINDOWS_BUILD_SUCCEEDED " + output);
        }
        finally
        {
            PlayerSettings.bundleVersion = version;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
            AssetDatabase.SaveAssets();
        }
    }
}
