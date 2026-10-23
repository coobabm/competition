using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using LlamaNative = UndreamAI.LlamaLib.LlamaLib;

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// LLM for Unity loads LlamaLib with LoadLibraryA, and Mono marshals that string as UTF-8. Unzipped into a folder
    /// with Chinese in its path (桌面\我的电脑活了\…), every DLL load fails and both models fall back
    /// ("Unable to load symbol 'Has_GPU_Layers'"). Before the first load, the library folder is swapped for an
    /// ASCII path to the same files: the 8.3 short name, else a copy under ProgramData or the temp folder.
    /// The model path gets the short name when there is one; llama.cpp opens UTF-8 paths itself otherwise.
    /// </summary>
    static class NativePathGuard
    {
        static readonly object gate = new object();
        static bool prepared;

        public static void PrepareLibraries()
        {
            lock (gate)
            {
                if (prepared) return;
                prepared = true;
                string dir = LlamaNative.baseLibraryPath;
                if (string.IsNullOrEmpty(dir) || IsAscii(dir)) return;
                string safe = ShortPath(dir) ?? CopyToAscii(dir);
                if (safe == null) { Debug.LogWarning("[灵光] 安装路径含非 ASCII 字符，且找不到可用的英文路径，本地模型可能无法启动：" + dir); return; }
                LlamaNative.baseLibraryPath = safe;
                Debug.Log("[灵光] 安装路径含中文，改从英文路径加载 LlamaLib：" + safe);
            }
        }

        public static string Model(string path) => string.IsNullOrEmpty(path) || IsAscii(path) ? path : ShortPath(path) ?? path;

        static bool IsAscii(string s) { foreach (char c in s) if (c > 127) return false; return true; }

        static string ShortPath(string path)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var buffer = new StringBuilder(1024);
                uint n = GetShortPathNameW(path, buffer, (uint)buffer.Capacity);
                if (n == 0 || n >= buffer.Capacity) return null;
                string shortPath = buffer.ToString();
                return IsAscii(shortPath) && (Directory.Exists(shortPath) || File.Exists(shortPath)) ? shortPath : null;
            }
            catch (Exception) { return null; }
#else
            return null;
#endif
        }

        // 8.3 names can be turned off per volume: then copy the few CPU/runtime DLLs (tens of MB) somewhere ASCII.
        static string CopyToAscii(string dir)
        {
            string tail = Path.Combine("MyComputerAlive", "LlamaLib", new DirectoryInfo(dir).Name);
            foreach (string root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ShortPath(Path.GetTempPath()) ?? Path.GetTempPath() })
            {
                if (string.IsNullOrEmpty(root) || !IsAscii(root)) continue;
                try
                {
                    string target = Path.Combine(root, tail);
                    Directory.CreateDirectory(target);
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        string to = Path.Combine(target, Path.GetFileName(file));
                        if (!IsAscii(to)) continue;
                        if (!File.Exists(to) || new FileInfo(to).Length != new FileInfo(file).Length) File.Copy(file, to, true);
                    }
                    return target;
                }
                catch (Exception error) { Debug.LogWarning("[灵光] 复制 LlamaLib 到 " + root + " 失败：" + error.Message); }
            }
            return null;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern uint GetShortPathNameW(string longPath, StringBuilder shortPath, uint size);
#endif
    }
}
