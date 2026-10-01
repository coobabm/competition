using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;

// Runs only when explicitly requested by a local marker, never on a teammate's checkout.
[InitializeOnLoad]
public static class RippleCollaborationSetup
{
    const string Marker = "Logs/SetupCollaboration.request";
    static double nextAttempt;
    static bool resolved;

    static RippleCollaborationSetup()
    {
        if (File.Exists(Marker)) EditorApplication.update += Setup;
    }

    static void Setup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying)
            return;
        if (EditorApplication.timeSinceStartup < nextAttempt) return;
        nextAttempt = EditorApplication.timeSinceStartup + 5;
        EditorSettings.serializationMode = SerializationMode.ForceText;
        VersionControlSettings.mode = "Visible Meta Files";
        AssetDatabase.SaveAssets();

        Type windowType = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            windowType = assembly.GetType("Unity.PlasticSCM.Editor.UVCSWindow");
            if (windowType != null) break;
        }
        if (windowType == null)
        {
            if (!resolved) { resolved = true; Client.Resolve(); }
            return;
        }
        EditorApplication.update -= Setup;
        File.Delete(Marker);
        foreach (var window in UnityEngine.Resources.FindObjectsOfTypeAll<EditorWindow>())
            if (window.maximized) window.maximized = false;
        EditorApplication.ExecuteMenuItem("Window/Version Control/Unity Version Control");
        const string manualMarker = "Logs/ManualCloudLogin.request";
        if (File.Exists(manualMarker))
        {
            File.Delete(manualMarker);
            var vcsWindow = EditorWindow.GetWindow(windowType);
            var welcome = windowType.GetMethod("GetWelcomeView", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(vcsWindow, null);
            var restApi = welcome.GetType().GetField("mPlasticWebRestApi", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(welcome);
            var cloudWindowType = windowType.Assembly.GetType(
                "Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome.CloudEditionWelcomeWindow");
            cloudWindowType.GetMethod("ShowWindow", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { restApi, welcome, false });
        }
        const string retryMarker = "Logs/RetryCloudLogin.request";
        if (File.Exists(retryMarker))
        {
            File.Delete(retryMarker);
            var loginType = windowType.Assembly.GetType(
                "Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome.AutoLogin");
            var login = Activator.CreateInstance(loginType, true);
            loginType.GetMethod("Run", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(login, new object[] { null });
        }
        File.WriteAllText("Logs/CollaborationSetup.txt",
            "Unity Version Control installed and opened.\nForce Text: enabled.\nVisible Meta Files: enabled.\n"
            + "Organization: " + CloudProjectSettings.organizationName + "\n"
            + "Cloud project: " + CloudProjectSettings.projectName + "\n"
            + "Repository connection and member invitation must be checked separately.\n");
    }
}
