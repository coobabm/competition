using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
namespace LingGuangV05.Desktop
{
    static class PlayerWindowSize
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential)] struct WorkRect { public int left, top, right, bottom; }
        [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint action, uint param, out WorkRect value, uint flags);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Resize()
        {
            const string key = "LingGuang.WindowSize.Handoff2";
            if (Screen.fullScreen || PlayerPrefs.HasKey(key)) return;
            if (SystemParametersInfo(0x0030, 0, out var work, 0)) {
                Screen.SetResolution(Mathf.Max(800, work.right - work.left - 64), Mathf.Max(600, work.bottom - work.top - 80), FullScreenMode.Windowed);
                PlayerPrefs.SetInt(key, 1); PlayerPrefs.Save();
            }
        }
#endif
    }
}
