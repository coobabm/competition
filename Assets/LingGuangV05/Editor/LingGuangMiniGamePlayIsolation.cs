#if UNITY_EDITOR
using LingGuangV05.Runtime;
using UnityEditor;

/// <summary>Opt-in, editor-session-only sandbox using the runtime's existing verification save hook.</summary>
[InitializeOnLoad]
public static class LingGuangMiniGamePlayIsolation
{
    const string Key = "LingGuang.NativeMiniGames.QASave";
    static LingGuangMiniGamePlayIsolation()
    {
        string path = SessionState.GetString(Key, "");
        if (!string.IsNullOrEmpty(path)) ChapterOneRuntime.VerificationSaveDirectory = path;
    }
    public static void Arm(string path)
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Arm before Play");
        if (string.IsNullOrEmpty(path) || !System.IO.Path.GetFullPath(path).StartsWith("/tmp/lg-minigames-play."))
            throw new System.ArgumentException("A task-specific temporary save directory is required");
        SessionState.SetString(Key, path); ChapterOneRuntime.VerificationSaveDirectory = path;
    }
    public static void Disarm()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play before disarming");
        SessionState.EraseString(Key); ChapterOneRuntime.VerificationSaveDirectory = null;
    }
}
#endif
