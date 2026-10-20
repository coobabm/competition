using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Windows batch-mode only, no Play mode, no GPU and no user-save access:
// Unity -batchmode -projectPath . -executeMethod LocalModelSmoke.RunCpu --verification-save-dir <absolute isolated directory>
// The isolation directory must be outside the project and ordinary saves. Exit: 0 pass, 1 failure, 124 timeout.
public static class LocalModelSmoke
{
    const BindingFlags Any = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
    public static void Run() => RunCpu();

    public static void RunCpu()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run model smoke in a separate batch-mode Editor.");
        int exitCode = 1;
        string report = null;
        string stage = "initialization";
        Action<string> progress = value => { stage = value; if (report != null) File.AppendAllText(report, DateTime.UtcNow.ToString("O") + " " + value + "\n"); };
        var log = new StringBuilder();
        // A native call (including disposal) can block the Editor main thread. The watchdog must not depend on it.
        using (var watchdog = new System.Threading.Timer(_ => { try { progress("TIMEOUT during " + stage); } finally { Environment.Exit(124); } }, null, 300000, System.Threading.Timeout.Infinite))
        {
            try
            {
                if (Application.platform != RuntimePlatform.WindowsEditor) throw new InvalidOperationException("Windows CPU smoke requires Windows Editor.");
                string root = LingGuangV05.Runtime.Persistence.VerificationSavePaths.Root;
                if (string.IsNullOrEmpty(root)) throw new InvalidOperationException("Missing --verification-save-dir; ordinary save paths are not permitted.");
                report = Path.Combine(root, "model-smoke.txt");
                File.WriteAllText(report, "RUNNING Windows CPU smoke; incomplete report means failure or timeout (exit 124).\n");
                progress("payload verification");
                IntegratedReleaseBuild.VerifyPayload(Application.streamingAssetsPath);
                progress("payload verified");
                var llm = typeof(LingGuangV05.Desktop.LLM.LocalLlm);
                string model = Path.Combine(Application.streamingAssetsPath, "LingGuang/Models/", LingGuangV05.Desktop.LLM.LocalModelRelease.FileName);
                object native = null;
                try
                {
                    var watch = Stopwatch.StartNew();
                    progress("loading chat model");
                    native = llm.GetMethod("CreateNative", Any).Invoke(null, new object[] { LingGuangV05.Core.Chat.LlmLaunchMode.Cpu, model, 1, 0 });
                    progress("chat model loaded");
                    log.AppendLine("CPU chat loaded in " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");
                    var client = ((Array)native.GetType().GetField("clients").GetValue(native)).GetValue(0);
                    foreach (string ask in new[] { "你好，请用一句话介绍自己。", "What is two plus three? Reply briefly." })
                    {
                        var messages = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", "用简短自然的语言回答。"), new KeyValuePair<string, string>("user", ask) };
                        var parameters = (JObject)llm.GetMethod("ChatParameters", Any).Invoke(null, new object[] { 64, .7f, null });
                        parameters["seed"] = 20161020;
                        progress("completion: " + ask);
                        var result = JObject.Parse((string)llm.GetMethod("Complete", Any).Invoke(null, new object[] { native, client, messages, parameters, false, 0 }));
                        progress("completion returned");
                        string answer = LingGuangV05.Core.Chat.LlmScheduling.AfterThinking(result.Value<string>("content"), false);
                        if (result["error"] != null || string.IsNullOrWhiteSpace(answer)) throw new InvalidDataException("Chat produced an error or empty answer: " + result);
                        log.AppendLine("Q " + ask + "\nA " + answer.Trim());
                    }
                }
                finally { if (native != null) { progress("freeing chat model"); llm.GetMethod("FreeNative", Any).Invoke(null, new[] { native }); progress("chat model freed"); } }

                string embedding = Path.Combine(Application.streamingAssetsPath, "LingGuang/Embedding/", LingGuangV05.Desktop.LLM.LocalModelRelease.EmbeddingFileName);
                var embedder = typeof(LingGuangV05.Desktop.LLM.LocalEmbedder);
                progress("loading embedding model");
                using (var service = (UndreamAI.LlamaLib.LLMService)embedder.GetMethod("Create", Any).Invoke(null, new object[] { embedding, false }))
                {
                    progress("embedding model loaded");
                    var client = new UndreamAI.LlamaLib.LLMClient(service);
                    try
                    {
                        int dimensions = service.EmbeddingSize();
                        if (dimensions != LingGuangV05.Desktop.LLM.LocalModelRelease.EmbeddingDimensions) throw new InvalidDataException("Unexpected BGE dimensions: " + dimensions);
                        string instruction = LingGuangV05.Desktop.LLM.LocalEmbedder.InstructionFor(Path.GetFileName(embedding), out bool chineseOnly);
                        if (!chineseOnly) throw new InvalidDataException("BGE Chinese instruction not selected.");
                        foreach (string text in new[] { instruction + "期末考试如何准备？", "十一月二日考概率论。" })
                        {
                            progress("embedding vector");
                            string raw = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(service.llamaLib.LLM_Embeddings(client.llm, text));
                            float[] vector = JArray.Parse(raw ?? "[]").ToObject<float[]>();
                            if (vector.Length != dimensions || vector.Any(v => float.IsNaN(v) || float.IsInfinity(v)) || !vector.Any(v => v != 0))
                                throw new InvalidDataException("Embedding must be a finite nonzero vector of " + dimensions + " dimensions.");
                            log.AppendLine("CPU embedding: " + vector.Length + " finite dimensions");
                        }
                    }
                    finally
                    {
                        try { if (client.llm != IntPtr.Zero) service.llamaLib.LLM_Delete(client.llm); }
                        finally { client.llm = IntPtr.Zero; service.Stop(); }
                    }
                }
                log.AppendLine("MODEL_SMOKE_PASS");
                exitCode = 0;
            }
            catch (Exception error) { log.AppendLine("MODEL_SMOKE_FAILED " + (error.InnerException ?? error)); }
            finally
            {
                try { if (report != null) File.AppendAllText(report, log.ToString()); }
                catch (Exception writeError) { exitCode = 1; log.AppendLine("Report write failed: " + writeError.Message); }
                if (exitCode == 0) Debug.Log(log.ToString()); else Debug.LogError(log.ToString());
            }
        }
        UnityEditor.EditorApplication.Exit(exitCode);
    }

    // Exploratory comparison only; RunCpu is the strict Windows release gate.
    // Side by side on the game's real prompts (LoRA check, 2026-10-20):
    // Unity -batchmode -quit -projectPath . -executeMethod LocalModelSmoke.Compare -smokeEval <eval.jsonl> -smokeModels <a.gguf>,<b.gguf> -smokeOut <report.jsonl>
    // Each eval line: {id, persona, stage, maxTokens, temperature, messages: [{role, content}]}. Every model answers every
    // prompt (GPU, or CPU with -smokeCpu) with the game's sampler for that persona (晴雯's grammar, 灵光's stage settings) and a fixed seed.
    public static void Compare()
    {
        string eval = Arg("-smokeEval"), output = Arg("-smokeOut");
        var models = Arg("-smokeModels").Split(',');
        var mode = Environment.GetCommandLineArgs().Contains("-smokeCpu") ? LingGuangV05.Core.Chat.LlmLaunchMode.Cpu : LingGuangV05.Core.Chat.LlmLaunchMode.Gpu;
        var llm = typeof(LingGuangV05.Desktop.LLM.LocalLlm);
        var lines = File.ReadAllLines(eval);
        var answers = new Dictionary<string, JObject>();
        foreach (var model in models)
        {
            object native = null;
            try
            {
                var watch = Stopwatch.StartNew();
                native = llm.GetMethod("CreateNative", Any).Invoke(null, new object[] { mode, model, 1, 0 });
                Debug.Log("SMOKE_COMPARE loaded " + model + " in " + watch.Elapsed.TotalSeconds.ToString("0.0") + " s");
                var client = ((Array)native.GetType().GetField("clients").GetValue(native)).GetValue(0);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var item = JObject.Parse(line);
                    string id = item.Value<string>("id"), persona = item.Value<string>("persona");
                    var messages = new List<KeyValuePair<string, string>>();
                    foreach (var m in (JArray)item["messages"]) messages.Add(new KeyValuePair<string, string>(m.Value<string>("role"), m.Value<string>("content")));
                    LingGuangV05.XingGuang.XgSampling sampling = persona == "qingwen" ? LingGuangV05.Desktop.LLM.GirlfriendPrompt.Sampling()
                        : persona == "lingguang" ? LingGuangV05.XingGuang.XgSpeechPolicy.Sampling(item.Value<int>("stage")) : null;
                    var parameters = (JObject)llm.GetMethod("ChatParameters", Any).Invoke(null, new object[] { item.Value<int>("maxTokens"), item.Value<float>("temperature"), sampling });
                    parameters["seed"] = 20161020;
                    watch.Restart();
                    string raw = (string)llm.GetMethod("Complete", Any).Invoke(null, new object[] { native, client, messages, parameters, false, 0 });
                    var parsed = JObject.Parse(raw);
                    if (!answers.TryGetValue(id, out var row)) answers[id] = row = new JObject { ["id"] = id, ["persona"] = persona, ["stage"] = item["stage"], ["his"] = messages[messages.Count - 1].Value };
                    row[Path.GetFileName(model)] = parsed.Value<string>("content")?.Trim();
                    row[Path.GetFileName(model) + " s"] = Math.Round(watch.Elapsed.TotalSeconds, 2);
                }
            }
            catch (Exception error) { Debug.LogError("SMOKE_COMPARE FAILED " + model + ": " + (error.InnerException ?? error)); }
            finally { if (native != null) llm.GetMethod("FreeNative", Any).Invoke(null, new[] { native }); }
        }
        using (var writer = new StreamWriter(output, false, new UTF8Encoding(false)))
            foreach (var row in answers.Values) writer.WriteLine(row.ToString(Newtonsoft.Json.Formatting.None));
        Debug.Log("SMOKE_COMPARE_DONE " + answers.Count + " prompts → " + output);
    }

    static string Arg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
        throw new ArgumentException("missing " + name);
    }
}
