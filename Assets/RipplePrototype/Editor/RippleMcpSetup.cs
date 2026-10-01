using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RippleMcpSetup
{
    static string Marker => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/SetupUnityMcp.request"));
    static bool resolving;
    static RippleMcpSetup()
    {
        if (File.Exists(Marker)) EditorApplication.update += Setup;
    }

    static void Setup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying) return;
        Type boot = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            boot = assembly.GetType("MCPForUnity.Editor.McpCiBoot");
            if (boot != null) break;
        }
        if (boot == null)
        {
            if (!resolving) { resolving = true; UnityEditor.PackageManager.Client.Resolve(); }
            return;
        }
        EditorApplication.update -= Setup;
        EditorPrefs.SetBool("MCPForUnity.AutoRegisterEnabled", false);
        var configType = boot.Assembly.GetType("MCPForUnity.Editor.Services.EditorConfigurationCache");
        var config = configType.GetProperty("Instance").GetValue(null);
        configType.GetMethod("SetUseHttpTransport").Invoke(config, new object[] { false });
        boot.GetMethod("StartStdioForCi", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        EditorApplication.ExecuteMenuItem("Window/MCP for Unity/Toggle MCP Window");
        File.Delete(Marker);
        File.WriteAllText(Path.ChangeExtension(Marker, ".txt"), "Unity MCP v10.0.0 configured for local stdio. Project: " + Application.dataPath);
    }
}
