using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;

// Feasibility probe: can LLamaSharp run the game's Qwen3.5-4B in-process, and how fast is it next to llama-server?
// Usage: dotnet run -c Release [-- --server]   (--server also times the same prompt on the running llama-server)

string model = "/Users/kk/unity/competition/Assets/StreamingAssets/LingGuang/Models/Qwen_Qwen3.5-4B-Q4_K_M.gguf";
bool alsoServer = args.Contains("--server");

const string Gbnf = "root ::= [^\\n<]{2,60}\n";

var sys = new StringBuilder();
sys.Append("你是林晴雯，2016 年在省城读大三的女生，是玩家的女朋友。你们异地，主要在 YY 上聊天。");
sys.Append("说话口语、简短，一次只回一句，像真实聊天，不用书面标点，不用括号描写动作，不说自己是 AI。");
sys.Append("你有自己的生活：上课、社团、室友小敏、周末去图书馆。你嘴硬心软，不高兴时会变冷淡，但不会骂人。");
sys.Append("不知道的事情就说不知道，不编造玩家没提过的经历。只用 2016 年以前的网络用语。");
var history = new (string role, string text)[]
{
    ("user", "在吗"), ("assistant", "在呀 刚下课"),
    ("user", "今天上什么课"), ("assistant", "数据结构 老师讲得我快睡着了"),
    ("user", "哈哈 我最近在弄一个程序"), ("assistant", "又是那个什么训练？"),
    ("user", "对 它好像能自己挣钱了"), ("assistant", "真的假的 那你请我吃饭"),
    ("user", "行啊 等你放假"), ("assistant", "说话算话哦"),
};
string last = "你室友小敏最近怎么样";

string Prompt(string latest)
{
    var p = new StringBuilder();
    p.Append("<|im_start|>system\n").Append(sys).Append("<|im_end|>\n");
    foreach (var (role, text) in history) p.Append("<|im_start|>").Append(role).Append('\n').Append(text).Append("<|im_end|>\n");
    p.Append("<|im_start|>user\n【当前状态】晚上 22:10，心情：平常。\n").Append(latest).Append("<|im_end|>\n");
    // An empty think block turns thinking off, as enable_thinking=false does in the server's template.
    p.Append("<|im_start|>assistant\n<think>\n\n</think>\n\n");
    return p.ToString();
}

var sw = Stopwatch.StartNew();
NativeLibraryConfig.All.WithLibrary("/tmp/lsprobe/llama-b10221/llama-b10221/libllama.0.dylib", "/tmp/lsprobe/llama-b10221/llama-b10221/libmtmd.0.dylib").WithLogCallback((level, msg) => { if (level >= LLamaLogLevel.Warning) Console.Write("[native] " + msg); });
var mp = new ModelParams(model)
{
    ContextSize = 4096,
    GpuLayerCount = 999,
    FlashAttention = true,
    TypeK = GGMLType.GGML_TYPE_Q8_0,
    TypeV = GGMLType.GGML_TYPE_Q8_0,
};
using var weights = LLamaWeights.LoadFromFile(mp);
Console.WriteLine($"load: {sw.Elapsed.TotalSeconds:F2}s");

using var ctx = weights.CreateContext(mp);
var exec = new StatelessExecutor(weights, mp);

foreach (var q in new[] { last, "你那边下雨了吗", "我想你了" })
{
    string prompt = Prompt(q);
    int promptTokens = ctx.Tokenize(prompt, addBos: false, special: true).Length;
    var inf = new InferenceParams
    {
        MaxTokens = 60,
        AntiPrompts = new[] { "<|im_end|>" },
        SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.8f, TopP = 0.95f, TopK = 40, MinP = 0.05f, Grammar = new Grammar(Gbnf, "root") },
    };
    sw.Restart();
    double first = -1;
    var reply = new StringBuilder();
    int n = 0;
    await foreach (var piece in exec.InferAsync(prompt, inf))
    {
        if (first < 0) first = sw.Elapsed.TotalSeconds;
        reply.Append(piece);
        n++;
    }
    double total = sw.Elapsed.TotalSeconds;
    Console.WriteLine($"[in-process] Q: {q}\n  A: {reply.ToString().Replace("<|im_end|>", "").Trim()}\n  prompt {promptTokens} tok, first token {first:F2}s, total {total:F2}s, {n} pieces, gen {(n - 1) / Math.Max(.001, total - first):F1} tok/s");

    if (alsoServer)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        var body = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["prompt"] = prompt, ["n_predict"] = 60, ["temperature"] = 0.8, ["top_p"] = 0.95, ["top_k"] = 40, ["min_p"] = 0.05,
            ["grammar"] = Gbnf, ["stop"] = new[] { "<|im_end|>" }, ["cache_prompt"] = false,
        });
        sw.Restart();
        var res = await http.PostAsync("http://127.0.0.1:18088/completion", new StringContent(body, Encoding.UTF8, "application/json"));
        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        var t = json.GetProperty("timings");
        Console.WriteLine($"[server]     A: {json.GetProperty("content").GetString().Trim()}\n  wall {sw.Elapsed.TotalSeconds:F2}s, prompt eval {t.GetProperty("prompt_ms").GetDouble() / 1000:F2}s, gen {t.GetProperty("predicted_per_second").GetDouble():F1} tok/s");
    }
}
Console.WriteLine($"process RSS: {Process.GetCurrentProcess().WorkingSet64 / 1048576} MB");
