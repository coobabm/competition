#if UNITY_EDITOR
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
        TitleArchivesQa.Run();
        IdleSaveAndStageSnapshotQa.Run();
        StartMenuSettingsChecks.Run();
        Debug.Log("SAVE_MENU_CHECKS_PASS; run DesignWallpaperSmoke.RunIdle in a separate Editor process");
    }
}
#endif
