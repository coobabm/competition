using System;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace LingGuangV05.Desktop
{
    /// <summary>The native player window, never the foreground window of another application.</summary>
    public static class PlayerWindowState
    {
#if UNITY_EDITOR
        // Opt-in QA seam: an Editor test never minimizes the developer's actual desktop.
        public static Func<bool> EditorMinimizedProbe;
#endif
        public static bool IsMinimized
        {
            get
            {
#if UNITY_EDITOR
                return EditorMinimizedProbe != null && EditorMinimizedProbe();
#elif UNITY_STANDALONE_WIN
                return ReadWindowsMinimized();
#else
                return false; // No non-Windows native minimization contract is claimed.
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
#if UNITY_EDITOR
            EditorMinimizedProbe = null;
#elif UNITY_STANDALONE_WIN
            window = IntPtr.Zero; processId = 0; retryAt = 0; unavailable = false;
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        static IntPtr window;
        static uint processId;
        static float retryAt;
        static bool unavailable;
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint process);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassNameW(IntPtr hWnd, StringBuilder name, int maxCount);

        static bool OwnedWindow()
        {
            if (window == IntPtr.Zero || !IsWindow(window)) return false;
            GetWindowThreadProcessId(window, out uint owner);
            return owner == processId;
        }

        static bool ReadWindowsMinimized()
        {
            if (unavailable) return false;
            try
            {
                if (!OwnedWindow())
                {
                    window = IntPtr.Zero;
                    if (Time.realtimeSinceStartup < retryAt) return false;
                    retryAt = Time.realtimeSinceStartup + 1;
                    // Same current-process discovery used by PlayerWindowSize's title setter.
                    using (var process = System.Diagnostics.Process.GetCurrentProcess())
                    {
                        processId = (uint)process.Id;
                        window = process.MainWindowHandle;
                    }
                    if (!OwnedWindow()) { window = IntPtr.Zero; return false; }
                    var className = new StringBuilder(64);
                    if (GetClassNameW(window, className, className.Capacity) <= 0 || className.ToString() != "UnityWndClass")
                    { window = IntPtr.Zero; return false; }
                }
                return IsIconic(window);
            }
            catch (Exception error)
            {
                unavailable = true; window = IntPtr.Zero;
                Debug.LogWarning("Native game-window minimization detection unavailable: " + error.GetType().Name);
                return false;
            }
        }
#endif
    }
}
