using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class RecordingPlayerBuild
{
    public static void Run()
    {
        DesignFirstLessonChecks.Run();
        DesignCrowdPrivacyChecks.Run();
        if (!File.Exists("Build/Recording-Stage4/UserData/chapter-one.sav")) PrepareRecordingSave.Run();
        string company = PlayerSettings.companyName, product = PlayerSettings.productName, version = PlayerSettings.bundleVersion;
        try
        {
            PlayerSettings.companyName = "RippleLocalPreview";
            PlayerSettings.productName = "MyComputerAlive-Recording-Stage4";
            PlayerSettings.bundleVersion = "2026.10.15.2";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity", "Assets/LingGuangV05/Scenes/Main.unity" },
                locationPathName = "Build/Recording-Stage4/MyComputerAlive.exe",
                target = BuildTarget.StandaloneWindows64,
                extraScriptingDefines = new[] { "RECORDING_PREVIEW" }, options = BuildOptions.None
            });
            File.WriteAllText("Build/Recording-Stage4/build-result.txt", report.summary.result + " errors=" + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Recording build failed");
            Debug.Log("RECORDING_BUILD_PASS");
        }
        finally { PlayerSettings.companyName = company; PlayerSettings.productName = product; PlayerSettings.bundleVersion = version; AssetDatabase.SaveAssets(); }
    }
}
