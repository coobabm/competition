using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class WindowsReleaseBuild
{
    public static void Build()
    {
        const string scene = "Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity";
        const string main = "Assets/LingGuangV05/Scenes/Main.unity";
        if (!File.Exists(scene) || !File.Exists(main)) throw new Exception("Build scene missing");
        if (GraphicsSettings.defaultRenderPipeline == null) throw new Exception("Render pipeline asset missing");
        Debug.Log("LOCAL_RENDER_PIPELINE " + GraphicsSettings.defaultRenderPipeline.name);
        PlayerSettings.companyName = "RippleLocalPreview";
        // Keep the installed Oct 8 player's save location across this upgrade.
        PlayerSettings.productName = "LingGuang-Playability-20261008";
        PlayerSettings.bundleVersion = "2026.10.11.2";
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        PlayerSettings.graphicsJobs = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1366;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene, true), new EditorBuildSettingsScene(main, true) };
        AssetDatabase.SaveAssets();
        string output = Path.GetFullPath("Build/ClaudeHandoff-Windows/LingGuang.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scene, main }, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        });
        File.WriteAllText("build-result.json", JsonUtility.ToJson(new Result {
            result = report.summary.result.ToString(), errors = report.summary.totalErrors,
            warnings = report.summary.totalWarnings, completedUtc = DateTime.UtcNow.ToString("o"), path = output
        }, true));
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed");
        Debug.Log("COMPETITION_WINDOWS_BUILD_SUCCEEDED " + output);
    }
    [Serializable] class Result { public string result, completedUtc, path; public int errors, warnings; }
}
