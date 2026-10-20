#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.Desktop.OS;
using UnityEngine;

/// <summary>Scoped integration checks. Owns only a temporary desktop profile; never opens ordinary saves.</summary>
public static class ClaudeTakeoverChecks
{
    public static void Run()
    {
        TiebaDelayedFloorKeyChecks.Run();
        TiebaSideKeyChecks.Run();
        BrainWindowFeedbackChecks.Run();
        CheckDesktopProfileInitialization();
        NamingChecks.Run();
        Debug.Log("CLAUDE_TAKEOVER_CHECKS_PASS: Tieba state keys, bounded brain feedback, isolated desktop clear/read/write, naming persistence and release identity.");
    }

    static void CheckDesktopProfileInitialization()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var type = typeof(VerificationSavePaths);
        var resolved = type.GetField("resolved", flags);
        var root = type.GetField("root", flags);
        var failure = type.GetField("failure", flags);
        var data = typeof(DreamOSDataManager).GetField("dataPath", flags);
        object oldResolved = resolved.GetValue(null), oldRoot = root.GetValue(null), oldFailure = failure.GetValue(null), oldData = data.GetValue(null);
        string owned = Path.Combine(Path.GetTempPath(), "lingguang-desktop-profile-" + Guid.NewGuid().ToString("N"));
        try
        {
            // Declared isolated fixture: exercise the actual manager's first-operation path.
            resolved.SetValue(null, true); root.SetValue(null, owned); failure.SetValue(null, null); data.SetValue(null, null);
            DreamOSDataManager.DeleteDataCategory(DreamOSDataManager.DataCategory.Network);
            string expected = Path.Combine(owned, "DesktopProfile", "Network.json");
            if (!File.Exists(expected) || File.ReadAllText(expected) != "{}")
                throw new Exception("First clear did not initialize its isolated desktop profile.");
            if ((string)data.GetValue(null) != Path.GetDirectoryName(expected) + Path.DirectorySeparatorChar)
                throw new Exception("Desktop profile did not retain the explicit verification root.");
            if (DreamOSDataManager.ContainsJsonKey(DreamOSDataManager.DataCategory.Network, "missing"))
                throw new Exception("A cleared profile contains an unexpected key.");
            Debug.Log("DESKTOP_PROFILE_INITIALIZATION_PASS: first operation clears only its own verification directory.");
        }
        finally
        {
            data.SetValue(null, oldData); failure.SetValue(null, oldFailure); root.SetValue(null, oldRoot); resolved.SetValue(null, oldResolved);
            if (Directory.Exists(owned)) Directory.Delete(owned, true);
        }
    }
}
#endif
