#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Dedicated batch invocation: -executeMethod LifeGroupPlaybackRunner.Run (without -quit).
// Empty unsaved scene; no Main/runtime and no modification of Enter Play Mode options.
[InitializeOnLoad]
public static class LifeGroupPlaybackRunner
{
    const string Key = "LifeGroupPlaybackRunner.Directory";
    const string OldOverrideKey = "LifeGroupPlaybackRunner.OldOverride";
    static string Root => SessionState.GetString(Key, "");
    static Report report;
    static double deadline;
    static bool finishing;
    [Serializable] sealed class Report
    {
        public string status = "running", failure = "", unityVersion, checksMessage = "";
        public bool enterPlayModeOptionsEnabled, checksPassed;
        public int enterPlayModeOptions;
        public List<string> errors = new List<string>();
        public string scope = "Batch-only empty scene; actual group coroutine disable/reenable and stale-finally checks. No Main, game runtime, authored delivery or player save. Editor play-mode settings are observed, never modified.";
    }
    static LifeGroupPlaybackRunner() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use a dedicated batch Editor");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start in Edit Mode");
        string dir = Path.Combine(Path.GetTempPath(), "LingGuang-LifeGroupBatch-QA-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        SessionState.SetString(Key, dir);
        SessionState.SetString(OldOverrideKey, ChapterOneRuntime.VerificationSaveDirectory ?? "");
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(dir, "UnusedIsolatedSave");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        report = NewReport(); Save(); EditorApplication.EnterPlaymode();
    }
    static Report NewReport() => new Report { unityVersion = Application.unityVersion,
        enterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled,
        enterPlayModeOptions = (int)EditorSettings.enterPlayModeOptions };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IsolateBeforeLoad()
    {
        if (!string.IsNullOrEmpty(Root)) ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(Root, "UnusedIsolatedSave");
    }
    static void State(PlayModeStateChange state)
    {
        if (string.IsNullOrEmpty(Root)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            report = NewReport(); finishing = false; deadline = EditorApplication.timeSinceStartup + 90;
            Application.logMessageReceived += Log; EditorApplication.update += Watchdog;
            if (UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(FindObjectsInactive.Include) != null)
            { Complete(false, "Unexpected runtime exists in empty-scene fixture"); return; }
            new GameObject("Life group playback isolated runner").AddComponent<LifeGroupPlaybackDriver>().StartChecks();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Application.logMessageReceived -= Log; EditorApplication.update -= Watchdog;
            string dir = Root; bool passed = File.Exists(Path.Combine(dir, "passed.txt"));
            string previous = SessionState.GetString(OldOverrideKey, "");
            ChapterOneRuntime.VerificationSaveDirectory = previous.Length == 0 ? null : previous;
            SessionState.EraseString(OldOverrideKey); SessionState.EraseString(Key);
            Debug.Log("Life group playback batch " + (passed ? "PASS" : "FAIL") + ": " + Path.Combine(dir, "report.json"));
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
    static void Log(string message, string stack, LogType type)
    {
        if (report == null || finishing) return;
        if (message.StartsWith("LIFE_GROUP_PLAYBACK_PASS ", StringComparison.Ordinal))
        { report.checksPassed = true; report.checksMessage = message; }
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) report.errors.Add(message + "\n" + stack);
    }
    static void Watchdog()
    {
        if (!finishing && EditorApplication.timeSinceStartup > deadline) Complete(false, "Timed out after 90 seconds");
    }
    public static void Complete(bool passed, string failure)
    {
        if (finishing) return;
        finishing = true;
        if (report == null) report = NewReport();
        passed = passed && report.errors.Count == 0;
        if (!report.checksPassed) { passed = false; failure += " Missing lifecycle check completion marker."; }
        report.status = passed ? "passed" : "failed"; report.failure = failure ?? ""; Save();
        if (passed) File.WriteAllText(Path.Combine(Root, "passed.txt"), "Group playback lifecycle coroutine passed in empty scene");
        Application.logMessageReceived -= Log; EditorApplication.update -= Watchdog;
        EditorApplication.ExitPlaymode();
    }
    static void Save()
    {
        if (!string.IsNullOrEmpty(Root) && report != null)
            File.WriteAllText(Path.Combine(Root, "report.json"), JsonUtility.ToJson(report, true));
    }
}
public sealed class LifeGroupPlaybackDriver : MonoBehaviour
{
    public void StartChecks() { StartCoroutine(Execute()); }
    IEnumerator Execute()
    {
        var checks = LifeGroupPlaybackChecks.Frames(); string error = null;
        try
        {
            while (true)
            {
                bool more = false; object current = null;
                try { more = checks.MoveNext(); if (more) current = checks.Current; }
                catch (Exception exception) { error = exception.ToString(); }
                if (error != null || !more) break;
                yield return current;
            }
        }
        finally { (checks as IDisposable)?.Dispose(); }
        yield return null; yield return null;
        LifeGroupPlaybackRunner.Complete(error == null, error);
    }
}
#endif
