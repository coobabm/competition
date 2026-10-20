#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Serial batch owner: existing checks create isolated state and restore preferences in finally.
public static class UpstreamIntegrationChecks
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run in a dedicated batch editor.");
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BookLibraryChecks.Run();
            ClaudeTakeoverChecks.Run();
            StartMenuSettingsChecks.Run();
            Debug.Log("UPSTREAM_INTEGRATION_CHECKS_PASS: library core, profile isolation, naming, shared settings callbacks. PlayMode and Player checks are separate.");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
#endif
