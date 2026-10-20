#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LingGuangV05.Desktop.XingGuang;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ReleaseFeedbackChecks
{
    public static void RunScreenshotUI()
    {
        var isolatedRoot=LingGuangV05.Runtime.Persistence.VerificationSavePaths.Root;
        if(string.IsNullOrEmpty(isolatedRoot))throw new Exception("External verification save root required");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        StartMenuSettingsChecks.Run();
        TaskQueueUiChecks.Run();
        Debug.Log("SCREENSHOT_FEEDBACK_UI_PASS");
    }
    public static void BuildChecked() { TechIconResourceChecks.Run(); IntegratedReleaseBuild.Run(); }
    public static void RunIdle() { EditorSearchWarmup.Run(); DesignWallpaperSmoke.RunIdle(); }
    public static void Run()
    {
        var isolatedRoot=LingGuangV05.Runtime.Persistence.VerificationSavePaths.Root;
        if(string.IsNullOrEmpty(isolatedRoot))throw new Exception("External verification save root required");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        TaskQueueUiChecks.Run();
        IdleWindowLayoutChecks.Run();
        TitleArchivesQa.Run();
        var scene=EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        var files=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<XgFilesWindow>(true)).Single();
        if(files.window==null||files.shortcutRoot==null||files.desktopLabel==null||files.client==null)throw new Exception("Files folder scene bindings missing");
        if(files.shortcutRoot.transform.parent.name!="Desktop List")throw new Exception("Files folder must belong to desktop icons");
        var paths=Resources.Load<TextAsset>("LingGuangV05/Icons/tech/signal_paths");
        if(paths==null||!paths.text.Contains("auto"))throw new Exception("Tech signal resource missing");
        Debug.Log("RELEASE_FEEDBACK_CHECKS_PASS: task callbacks, isolated disk new game, autosave, folder scene bindings and tech signals");
    }
}
#endif
