#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace LingGuangV05.Desktop.LLM.Editor
{
    /// <summary>
    /// StreamingAssets/LingGuang/llama holds one llama-server bundle per platform. After a player build, delete the
    /// bundles the target cannot run (a Windows build does not need the macOS dylibs and vice versa).
    /// macOS builds are ad-hoc signed by Unity, so the app is re-signed ad-hoc after the files are removed.
    /// </summary>
    public sealed class LlamaBuildStrip : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;
            string output = report.summary.outputPath;
            string streaming;
            string[] drop;
            if (target == BuildTarget.StandaloneOSX)
            {
                streaming = Path.Combine(output, "Contents", "Resources", "Data", "StreamingAssets");
                drop = new[] { "win-x64" };
            }
            else if (target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneWindows)
            {
                streaming = Path.Combine(Path.GetDirectoryName(output) ?? "", Path.GetFileNameWithoutExtension(output) + "_Data", "StreamingAssets");
                drop = new[] { "mac-arm64" };
            }
            else return;

            string llama = Path.Combine(streaming, "LingGuang", "llama");
            if (!Directory.Exists(llama)) return;
            foreach (var name in drop)
            {
                string dir = Path.Combine(llama, name);
                if (Directory.Exists(dir)) { Directory.Delete(dir, true); Debug.Log("[灵光] 构建里删掉了用不上的 llama 包：" + name); }
            }
            foreach (var meta in Directory.GetFiles(llama, "*.meta", SearchOption.AllDirectories)) File.Delete(meta);

            if (target == BuildTarget.StandaloneOSX)
            {
                try
                {
                    using (var sign = Process.Start(new ProcessStartInfo("/usr/bin/codesign", "--force --deep --sign - \"" + output + "\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true }))
                    {
                        string err = sign.StandardError.ReadToEnd();
                        sign.WaitForExit(120000);
                        if (sign.ExitCode != 0) Debug.LogWarning("[灵光] 重新签名失败：" + err);
                    }
                }
                catch (Exception error) { Debug.LogWarning("[灵光] 重新签名失败：" + error.Message); }
            }
        }
    }
}
#endif
