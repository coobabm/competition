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
    /// StreamingAssets/LingGuang/llama holds the llama-server bundles the game used before it switched to LLM for Unity
    /// (in-process LlamaLib, whose per-platform libraries LLM for Unity's own build step selects). After a player build,
    /// delete that folder from the output: no build runs it any more. macOS builds are ad-hoc signed by Unity, so the
    /// app is re-signed ad-hoc after the files are removed.
    /// </summary>
    public sealed class LlamaBuildStrip : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;
        /// <summary>The release must stay under 2 GB (decimal, so it also fits a 2 GiB limit and its zip).</summary>
        public const long SizeLimitBytes = 2_000_000_000;

        public void OnPostprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;
            string output = report.summary.outputPath;
            string streaming;
            if (target == BuildTarget.StandaloneOSX)
                streaming = Path.Combine(output, "Contents", "Resources", "Data", "StreamingAssets");
            else if (target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneWindows)
                streaming = Path.Combine(Path.GetDirectoryName(output) ?? "", Path.GetFileNameWithoutExtension(output) + "_Data", "StreamingAssets");
            else return;

            bool changed = false;
            string llama = Path.Combine(streaming, "LingGuang", "llama");
            if (Directory.Exists(llama))
            {
                Directory.Delete(llama, true);
                changed = true;
                Debug.Log("[灵光] 构建里删掉了不再使用的 llama-server 包（本地模型改由 LLM for Unity 运行）");
            }
            // Windows runs the CPU runtime only (LocalLlm.StartService), so LlamaLib's GPU builds there (CUDA via
            // tinyblas ~360 MB, Vulkan ~70 MB) and the import libraries would be dead weight. Turn Windows GPU on in
            // LocalLlm and these must stay.
            if (target != BuildTarget.StandaloneOSX)
                foreach (var lib in Directory.GetDirectories(streaming, "LlamaLib-*"))
                {
                    string native = Path.Combine(lib, "win-x64", "native");
                    if (!Directory.Exists(native)) continue;
                    long freed = 0;
                    foreach (var file in Directory.GetFiles(native))
                    {
                        string name = Path.GetFileName(file);
                        if (!(name.EndsWith(".lib", StringComparison.OrdinalIgnoreCase) || name.Contains("tinyblas") || name.Contains("vulkan") || name.Contains("cublas") || name.Contains("cudart"))) continue;
                        freed += new FileInfo(file).Length;
                        File.Delete(file);
                    }
                    if (freed > 0) Debug.Log("[灵光] Windows 构建只用 CPU 推理，删掉了 LlamaLib 的 GPU 库：" + (freed / (1024 * 1024)) + " MB");
                }
            CheckSize(target == BuildTarget.StandaloneOSX ? output : Path.GetDirectoryName(output));
            if (!changed) return;

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

        /// <summary>Logs the shipped size (Burst's DoNotShip folder left out) and an error above <see cref="SizeLimitBytes"/>.</summary>
        static void CheckSize(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;
            long total = 0, models = 0;
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                if (file.Contains("_DoNotShip")) continue;
                long size = new FileInfo(file).Length;
                total += size;
                if (file.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)) models += size;
            }
            string line = "[灵光] 包体 " + (total / 1e9).ToString("0.000") + " GB（模型 " + (models / 1e9).ToString("0.000") + " GB，上限 " + (SizeLimitBytes / 1e9).ToString("0.0") + " GB）：" + root;
            if (total > SizeLimitBytes) Debug.LogError(line + "，超了 " + ((total - SizeLimitBytes) / 1e6).ToString("0") + " MB");
            else Debug.Log(line);
        }
    }
}
#endif
