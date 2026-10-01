using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Project assembly and a one-shot runtime check; never runs during ordinary editing.
[InitializeOnLoad]
public static class RippleProjectSetup
{
    const string ScenePath = "Assets/Scenes/Ripple.unity";
    const string CheckKey = "Ripple.ImportCheck";
    static double deadline;
    static int stage;
    static int triggered;
    static bool ended;
    static string failure;
    static int resetFrame;

    static RippleProjectSetup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void CreateAndCheck()
    {
        try
        {
            CreateScene();
            deadline = EditorApplication.timeSinceStartup + 4;
            EditorApplication.update += WaitForStartup;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    static void WaitForStartup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.timeSinceStartup < deadline) return;
        EditorApplication.update -= WaitForStartup;
        SessionState.SetBool(CheckKey, true);
        EditorApplication.isPlaying = true;
    }

    static void CreateScene()
    {
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.3f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.035f, 0.065f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<BoardCamera>();

        new GameObject("Ripple Simulation").AddComponent<RippleSim>();
        new GameObject("Board - 60 Beings").AddComponent<BoardSpawner>();
        new GameObject("Sound and Feedback").AddComponent<RippleFeedback>();

        PlayerSettings.companyName = "RipplePrototype";
        PlayerSettings.productName = "Ripple";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("RIPPLE_SCENE_CREATED: " + ScenePath);
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(CheckKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            stage = 0;
            triggered = 0;
            ended = false;
            failure = null;
            deadline = EditorApplication.timeSinceStartup + 50;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += CheckRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(CheckKey, false);
            EditorApplication.Exit(string.IsNullOrEmpty(failure) ? 0 : 1);
        }
    }

    static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            failure = message + "\n" + trace;
    }

    static void CheckRuntime()
    {
        try
        {
            if (failure != null) throw new Exception(failure);
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Runtime check timed out.");
            var sim = RippleSim.I;
            var spawner = UnityEngine.Object.FindFirstObjectByType<BoardSpawner>();
            var beings = UnityEngine.Object.FindObjectsByType<Being>(FindObjectsSortMode.None);
            if (stage == 0)
            {
                if (sim == null || spawner == null || beings.Length == 0 || Time.frameCount < 3) return;
                if (beings.Length != 60) throw new Exception("Expected 60 beings, got " + beings.Length);
                if (Camera.main == null || !Camera.main.orthographic) throw new Exception("Missing 2D camera.");
                if (Shader.Find("Sprites/Default") == null) throw new Exception("Missing ripple shader.");
                var feedback = UnityEngine.Object.FindFirstObjectByType<RippleFeedback>();
                if (feedback == null || feedback.blip == null) throw new Exception("Sound was not initialized.");
                var firstBubble = Array.Find(beings, b => b.type == BeingType.Bubble);
                if (firstBubble == null) throw new Exception("No bubble in board.");
                sim.OnTriggered += (source, depth) => { if (source != null) triggered++; };
                sim.OnTurnEnded += result => ended = true;
                typeof(RippleSim).GetMethod("StartTurn", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(sim, new object[] { firstBubble.Pos });
                stage = 1;
            }
            else if (stage == 1 && ended)
            {
                if (triggered == 0 || sim.Busy) throw new Exception("Ripple chain did not complete.");
                Debug.Log("RIPPLE_CHAIN_OK: " + triggered + " reactions");
                spawner.Respawn(false);
                resetFrame = Time.frameCount;
                stage = 2;
            }
            else if (stage == 2 && Time.frameCount > resetFrame + 1)
            {
                if (beings.Length != 60) throw new Exception("Same board reset failed.");
                int seed = spawner.seed;
                spawner.Respawn(true);
                if (spawner.seed != seed + 1) throw new Exception("New board seed failed.");
                resetFrame = Time.frameCount;
                stage = 3;
            }
            else if (stage == 3 && Time.frameCount > resetFrame + 1)
            {
                if (beings.Length != 60) throw new Exception("New board reset failed.");
                CapturePreview();
                Finish(null);
            }
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }

    static void CapturePreview()
    {
        var camera = Camera.main;
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = new RenderTexture(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes("Logs/BoardPreview.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    static void Finish(string error)
    {
        failure = error;
        EditorApplication.update -= CheckRuntime;
        Application.logMessageReceived -= OnLog;
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/ImportCheck.txt", error == null
            ? "PASS: scripts compiled; scene loaded; 60 beings; camera; sound; ripple chain (" + triggered
                + " reactions); turn completed; same board reset; new board reset."
            : "FAIL: " + error);
        Debug.Log(error == null ? "RIPPLE_IMPORT_CHECK_PASS" : "RIPPLE_IMPORT_CHECK_FAIL: " + error);
        EditorApplication.isPlaying = false;
    }
}
