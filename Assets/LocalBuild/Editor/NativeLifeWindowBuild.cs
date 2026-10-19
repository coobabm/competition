#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class NativeLifeWindowBuild
{
    public static void Run()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Use a dedicated batch Editor in EditMode");
        string id = Guid.NewGuid().ToString("N");
        string directory = Path.Combine(Path.GetTempPath(), "LingGuang-NativeLifeBuild-" + id);
        string scenePath = "Assets/LocalBuild/NativeLifeWindowQA-" + id + ".unity";
        string company = PlayerSettings.companyName, product = PlayerSettings.productName;
        bool graphicsDefault = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
        var graphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
        bool jobs = PlayerSettings.graphicsJobs;
        var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        var oldScenes = EditorSceneManager.GetSceneManagerSetup();
        foreach (var item in oldScenes) if (item.isLoaded && !string.IsNullOrEmpty(item.path) && UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty)
            throw new InvalidOperationException("Refusing to replace a dirty scene");
        Directory.CreateDirectory(directory);
        try
        {
            PlayerSettings.companyName = "LingGuangLocalVerification";
            PlayerSettings.productName = "NativeLifeWindowQA-" + id;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.graphicsJobs = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Native life window verification").AddComponent<NativeLifeWindowProbe>();
            var camera = new GameObject("Verification camera").AddComponent<Camera>();
            camera.backgroundColor = new Color(.1f, .16f, .23f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("Cannot save isolated probe scene");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { scenePath }, locationPathName = Path.Combine(directory, "NativeLifeWindowProbe.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None,
                extraScriptingDefines = new[] { "NATIVE_LIFE_WINDOW_QA" }
            });
            var record = new Record { directory = directory, executable = Path.Combine(directory, "NativeLifeWindowProbe.exe"),
                result = result.summary.result.ToString(), errors = result.summary.totalErrors, warnings = result.summary.totalWarnings,
                company = PlayerSettings.companyName, product = PlayerSettings.productName, runInBackground = PlayerSettings.runInBackground };
            File.WriteAllText(Path.Combine(directory, "build-result.json"), JsonUtility.ToJson(record, true));
            Directory.CreateDirectory("LocalVerification/NativeLifeWindow");
            File.WriteAllText("LocalVerification/NativeLifeWindow/latest-build.json", JsonUtility.ToJson(record, true));
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Native probe build failed: " + directory);
            Debug.Log("NATIVE_LIFE_WINDOW_BUILD_PASS " + directory);
        }
        finally
        {
            PlayerSettings.companyName = company; PlayerSettings.productName = product;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, graphics);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, graphicsDefault);
            PlayerSettings.graphicsJobs = jobs;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(scenePath);
            if (oldScenes.Length > 0 && !string.IsNullOrEmpty(oldScenes[0].path)) EditorSceneManager.RestoreSceneManagerSetup(oldScenes);
            AssetDatabase.SaveAssets();
        }
    }
    [Serializable] sealed class Record { public string directory, executable, result, company, product; public int errors, warnings; public bool runInBackground; }
}
#endif
