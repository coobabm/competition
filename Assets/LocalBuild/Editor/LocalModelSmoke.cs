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

// Batch-mode check of the shipped models through the game's own LlamaLib path (no Play mode needed):
// Unity -batchmode -quit -projectPath . -executeMethod LocalModelSmoke.Run
// Writes LocalVerification/model-smoke.txt: load time, replies and speed on GPU and CPU, plus one embedding.
public static class LocalModelSmoke
{
    const BindingFlags Any = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    public static void Run()
    {
        var log = new StringBuilder();
        var llm = typeof(LingGuangV05.Desktop.LLM.LocalLlm);
        string model = (string)llm.GetMethod("FindModel", Any).Invoke(null, null);
        log.AppendLine("model " + model + " " + (model != null ? new FileInfo(model).Length / 1e9 : 0).ToString("0.000") + " GB");
        string system = "你是晴雯，2016 年在武汉读大二的女生，会计专业，县城独生女，妈妈是小学语文老师。你在 YY 上和异地的男朋友聊天。" +
            "说话口语、简短，一次一两句，不用书面语，不加引号和动作描写。今天是 2016 年 10 月 20 日。";
        string[] asks = { "期末准备得怎么样了", "我今天加班到十点，好累", "你还记得我送你的项链吗", "周末开YY吗" };
        foreach (var mode in new[] { LingGuangV05.Core.Chat.LlmLaunchMode.Gpu, LingGuangV05.Core.Chat.LlmLaunchMode.Cpu })
        {
            object native = null;
            try
            {
                var watch = Stopwatch.StartNew();
                native = llm.GetMethod("CreateNative", Any).Invoke(null, new object[] { mode, model, 1, 0 });
                log.AppendLine("\n== " + mode + " loaded in " + watch.Elapsed.TotalSeconds.ToString("0.0") + " s");
                var client = ((Array)native.GetType().GetField("clients").GetValue(native)).GetValue(0);
                foreach (var ask in asks)
                {
                    var messages = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", system), new KeyValuePair<string, string>("user", ask) };
                    var parameters = (JObject)llm.GetMethod("ChatParameters", Any).Invoke(null, new object[] { 80, .7f, null });
                    watch.Restart();
                    string raw = (string)llm.GetMethod("Complete", Any).Invoke(null, new object[] { native, client, messages, parameters, false, 0 });
                    double s = watch.Elapsed.TotalSeconds;
                    var parsed = JObject.Parse(raw);
                    var timings = parsed["timings"];
                    log.AppendLine("Q " + ask + "\nA " + parsed.Value<string>("content")?.Trim().Replace("\n", " / ") +
                        "\n   " + s.ToString("0.00") + " s · prompt " + timings?["prompt_per_second"]?.Value<double>().ToString("0") + " tok/s · gen " + timings?["predicted_per_second"]?.Value<double>().ToString("0") + " tok/s");
                }
            }
            catch (Exception error) { log.AppendLine("FAILED " + mode + ": " + (error.InnerException ?? error).Message); }
            finally { if (native != null) llm.GetMethod("FreeNative", Any).Invoke(null, new[] { native }); }
        }
        Directory.CreateDirectory("LocalVerification");
        File.WriteAllText("LocalVerification/model-smoke.txt", log.ToString());
        Debug.Log("MODEL_SMOKE_DONE\n" + log);
    }

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
