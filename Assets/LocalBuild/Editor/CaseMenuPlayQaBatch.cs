#if UNITY_EDITOR
using System;
using System.IO;
using LingGuangV05.CaseMenu;
using LingGuangV05.CaseMenu.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Batch wrapper for CaseMenuPlayQa (which needs Play Mode plus mouse/keyboard devices).
/// Usage: Unity.exe [-batchmode] -projectPath ... -executeMethod CaseMenuPlayQaBatch.Run
///        --verification-save-dir &lt;new external dir&gt; [-logFile ...]   (no -quit; exits 0 on pass, 1 on failure)
/// </summary>
[InitializeOnLoad]
public static class CaseMenuPlayQaBatch
{
    const string ActiveKey = "CaseMenuPlayQaBatch.Active", StartedKey = "CaseMenuPlayQaBatch.Started";
    static double waitStarted;

    static CaseMenuPlayQaBatch()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        waitStarted = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        // Batch mode has no focused Game view, so queued mouse hover never reaches the menu; for the full
        // run launch the Editor without -batchmode (same -executeMethod) and keep hands off mouse/keyboard.
        if (LingGuangV05.Runtime.Persistence.VerificationSavePaths.Root == null)
            throw new ArgumentException("Supply --verification-save-dir so the player save is never touched");
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(StartedKey, false);
        // With Enter Play Mode Options (no domain reload) the static constructor does not run again.
        waitStarted = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorSceneManager.OpenScene(CaseMenuController.ScenePath);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (EditorApplication.timeSinceStartup - waitStarted > 240) { Done(1, "CaseMenuPlayQaBatch timed out"); return; }
        if (!EditorApplication.isPlaying) return;
        if (!SessionState.GetBool(StartedKey, false))
        {
            var menu = UnityEngine.Object.FindAnyObjectByType<CaseMenuController>();
            if (menu == null || !menu.isActiveAndEnabled || Time.frameCount < 10) return;
            if (Mouse.current == null) InputSystem.AddDevice<Mouse>();
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
            SessionState.SetBool(StartedKey, true);
            try { CaseMenuPlayQa.Begin(); }
            catch (Exception error) { Done(1, "CaseMenuPlayQa.Begin failed: " + error); }
            return;
        }
        if (CaseMenuPlayQa.Running) return;
        var result = CaseMenuPlayQa.Result;
        if (result == null) { Done(1, "CaseMenuPlayQa produced no report"); return; }
        var lines = new System.Text.StringBuilder();
        foreach (var check in result.checks) lines.Append(check.passed ? "PASS " : "FAIL ").Append(check.name).Append(string.IsNullOrEmpty(check.detail) ? "" : " :: " + check.detail).Append('\n');
        foreach (var error in result.errors) lines.Append("ERROR ").Append(error).Append('\n');
        bool ok = result.complete && result.failed == 0;
        Done(ok ? 0 : 1, "CASEMENU_PLAY_QA " + (ok ? "PASSED" : "FAILED") + " complete=" + result.complete + " passed=" + result.passed + " failed=" + result.failed
            + " report=" + Path.Combine(result.outputDirectory ?? "", "report.json") + "\n" + lines);
    }

    static void Done(int code, string message)
    {
        EditorApplication.update -= Tick;
        SessionState.EraseBool(ActiveKey); SessionState.EraseBool(StartedKey);
        if (code == 0) Debug.Log(message); else Debug.LogError(message);
        EditorApplication.Exit(code);
    }
}
#endif
