#if UNITY_EDITOR
using System;
using System.IO;
using LingGuangV05.Desktop.XingGuang;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallDataFilesWindow
{
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Leave Play Mode before scene migration.");
        const string path = "Assets/LingGuangV05/Scenes/Main.unity";
        string backup = Path.GetFullPath(Path.Combine("LocalVerification", "FilesWindowSceneBackup", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(backup);
        File.Copy(path, Path.Combine(backup, "Main.unity"));
        var scene = EditorSceneManager.OpenScene(path);
        bool exists = false;
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<XgFilesWindow>(true) != null) exists = true;
        if (!exists) LingGuangV05.Editor.ChapterOneInstaller.InstallFilesWindow();
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Unable to save files-window scene migration.");
        Debug.Log("FILES_WINDOW_SCENE_INSTALLED; prior scene preserved at " + backup);
    }
}
#endif
