#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Editor;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Batch driver for LingGuangV05.Editor.PrologueLifecyclePlayQa (2026-10-23): Main PlayMode with an isolated save and
// DreamOS profile, no model, memory-only runtime. Boots the desktop like P1OpeningQa, then hands the run to the
// lifecycle QA (real LCD pointer routing) and writes its report under LocalVerification/github-sync-20261023.
//   -executeMethod PrologueLifecyclePlayQaBatch.RunNo    shutdown question answered 否 (extraction branch)
//   -executeMethod PrologueLifecyclePlayQaBatch.RunYes   shutdown question answered 是
// Without -quit; exits 0 when every check passed.
[InitializeOnLoad]
public static class PrologueLifecyclePlayQaBatch
{
    const string Key = "PrologueLifecyclePlayQaBatch.Root", BranchKey = "PrologueLifecyclePlayQaBatch.Branch";
    const string OutBase = "C:/Users/xomem/Documents/ChatGPT/games/LocalVerification/github-sync-20261023";
    static string Root => SessionState.GetString(Key, "");
    static bool Yes => SessionState.GetString(BranchKey, "") == "yes";
    static bool booted, started, finishing;
    static double timeout;

    static PrologueLifecyclePlayQaBatch() { EditorApplication.playModeStateChanged += State; }

    public static void RunNo() => Begin("no");
    public static void RunYes() => Begin("yes");

    static void Begin(string branch)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start in Edit Mode");
        string dir = Path.Combine(OutBase, "prologue-lifecycle-" + branch + "-" + DateTime.Now.ToString("HHmmss"));
        Directory.CreateDirectory(Path.Combine(dir, "Save")); Directory.CreateDirectory(Path.Combine(dir, "DesktopProfile"));
        SessionState.SetString(Key, dir); SessionState.SetString(BranchKey, branch);
        EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if (string.IsNullOrEmpty(Root)) return;
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(Root, "Save");
        typeof(LingGuangV05.Desktop.OS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, Path.Combine(Root, "DesktopProfile") + Path.DirectorySeparatorChar);
        SceneManager.sceneLoaded += DisableModel;
    }

    static void DisableModel(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(Root)) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var rt in root.GetComponentsInChildren<ChapterOneRuntime>(true))
            {
                rt.useDiskSave = false;
                var llm = rt.GetComponent<LocalLlm>() ?? rt.gameObject.AddComponent<LocalLlm>(); llm.enabled = false;
            }
    }

    static void State(PlayModeStateChange state)
    {
        if (string.IsNullOrEmpty(Root)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            booted = started = finishing = false; timeout = EditorApplication.timeSinceStartup + 420;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick; SceneManager.sceneLoaded -= DisableModel;
            string path = Root; bool ok = File.Exists(Path.Combine(path, "passed.txt"));
            SessionState.EraseString(Key); SessionState.EraseString(BranchKey); ChapterOneRuntime.VerificationSaveDirectory = null;
            Debug.Log("PROLOGUE_LIFECYCLE_QA " + (ok ? "PASSED" : "FAILED") + ": " + path);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }

    static void Finish(string error)
    {
        if (finishing) return;
        finishing = true;
        var r = PrologueLifecyclePlayQa.Result;
        string json = r == null ? "{}" : JsonUtility.ToJson(r, true);
        File.WriteAllText(Path.Combine(Root, "result.json"), json);
        if (error == null && r != null && r.complete && r.checks.Count > 0)
            File.WriteAllText(Path.Combine(Root, "passed.txt"), "Prologue lifecycle " + r.branch + " passed " + r.checks.Count + " checks");
        else Debug.LogError("Prologue lifecycle QA failed: " + (error ?? string.Join(" | ", r == null ? new string[0] : r.checks.Where(c => !c.passed).Select(c => c.name + " " + c.detail))));
        EditorApplication.isPlaying = false;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || finishing) return;
        try
        {
            if (EditorApplication.timeSinceStartup > timeout) throw new TimeoutException("Prologue lifecycle QA timeout");
            if (started) { if (!PrologueLifecyclePlayQa.Running) Finish(null); return; }
            var runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            var director = UnityEngine.Object.FindAnyObjectByType<PrologueDirector>();
            if (runtime == null || director == null || runtime.Sim == null) return;
            if (!booted)
            {
                var boot = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include);
                if (boot != null) { boot.CancelInvoke(); boot.StopAllCoroutines(); if (boot.bootAnimator != null) boot.bootAnimator.gameObject.SetActive(false); }
                var user = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.UserManager>(FindObjectsInactive.Include);
                if (user == null || !user.isActiveAndEnabled) return;
                runtime.useDiskSave = false;
                user.ShowDesktop(); booted = true; return;
            }
            PrologueLifecyclePlayQa.Begin(Yes); started = true;
        }
        catch (Exception e) { Finish(e.ToString()); }
    }
}
#endif
