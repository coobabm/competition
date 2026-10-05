#if UNITY_EDITOR
using UnityEditor;

namespace LingGuangV05.Editor
{
    /// <summary>Bounded QA-only guard against other in-flight script edits invalidating a Play run.</summary>
    public static class ChapterOneQaReloadGuard
    {
        static bool held;
        static double deadline;
        public static void LockForPlayQa(double seconds=150)
        {
            if(held) Release();
            deadline=EditorApplication.timeSinceStartup+System.Math.Min(240,System.Math.Max(10,seconds));
            EditorApplication.LockReloadAssemblies(); held=true;
            EditorApplication.update+=Watch;
            EditorApplication.playModeStateChanged+=Mode;
        }
        public static void Release()
        {
            if(!held)return;
            held=false; EditorApplication.update-=Watch; EditorApplication.playModeStateChanged-=Mode;
            EditorApplication.UnlockReloadAssemblies();
        }
        static void Watch(){if(held&&EditorApplication.timeSinceStartup>=deadline)Release();}
        static void Mode(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode)Release();}
    }
}
#endif
