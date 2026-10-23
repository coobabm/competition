#if UNITY_EDITOR
using System.IO;
using LingGuangV05.Runtime.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Isolated save and menu checks. Run the PlayMode idle fixture in a fresh Editor process.</summary>
public static class SaveMenuIdleChecks
{
    public static void Run()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Use a dedicated batch Editor in Edit Mode.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RunIsolated(nameof(TitleArchivesQa), TitleArchivesQa.Run);
        RunIsolated(nameof(IdleSaveAndStageSnapshotQa), IdleSaveAndStageSnapshotQa.Run);
        RunIsolated(nameof(StartMenuSettingsChecks), StartMenuSettingsChecks.Run);
        Debug.Log("SAVE_MENU_CHECKS_PASS; run DesignWallpaperSmoke.RunIdle in a separate Editor process");
    }

    /// <summary>
    /// With --verification-save-dir every runtime saves into that one root (it overrides each check's own folder), so the
    /// player-profile.json or save one check leaves would make the next check's new game skip the prologue. Each check
    /// therefore starts on an empty root, and what it wrote is moved into its own subfolder afterwards.
    /// </summary>
    static void RunIsolated(string name, System.Action check)
    {
        string root = VerificationSavePaths.Root;
        try { check(); }
        finally { if (root != null) Shelve(root, name); }
    }

    static void Shelve(string root, string name)
    {
        string target = Path.Combine(root, "SubChecks", name);
        Directory.CreateDirectory(target);
        foreach (string path in Directory.GetFileSystemEntries(root))
        {
            string entry = Path.GetFileName(path);
            if (entry == VerificationSavePaths.MarkerName || entry == "SubChecks") continue;
            if (Directory.Exists(path)) Directory.Move(path, Path.Combine(target, entry));
            else File.Move(path, Path.Combine(target, entry));
        }
    }
}
#endif
