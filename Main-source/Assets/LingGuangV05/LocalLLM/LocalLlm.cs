using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using AppNames = LingGuangV05.Core.AppNames;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// Local language model for free chat (Qwen3.5-4B GGUF by default). Starts llama.cpp's llama-server on
    /// 127.0.0.1 and talks to its OpenAI-style /v1/chat/completions endpoint. Offline: nothing leaves the machine.
    /// Model search: StreamingAssets/LingGuang/Models/*.gguf, then (editor) &lt;project&gt;/Models/*.gguf.
    /// Server search: StreamingAssets/LingGuang/llama/llama-server, then Homebrew / /usr/local.
    /// If either is missing, <see cref="Ready"/> stays false and callers fall back to scripted replies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalLlm : MonoBehaviour
    {
        public const int Port = 18088;
        public enum Status { Off, Starting, Ready, Failed }

        public static LocalLlm Instance { get; private set; }
        [SerializeField, HideInInspector] int endpointPort = Port;
        [SerializeField, HideInInspector] bool initialized, editorQaInstance, resumeOnEnable;
        public int EndpointPort => endpointPort;
        public Status State { get; private set; } = Status.Off;
        public string Detail { get; private set; } = "";
        public string ModelName { get; private set; } = "";
        public bool Ready => State == Status.Ready;
        public bool Busy => active.Count > 0 || jobs.Count > 0 || judges.Count > 0;
        public bool ChatBusy => jobs.Count > 0 || activeChats > 0;
        public bool JudgeReady => Ready && !changingSlots && Time.realtimeSinceStartup >= judgeRetryAfter;
        public int ParallelSlots { get; private set; } = 1;
        public bool OwnsServer => server != null;
        public bool ParallelUnavailable => desiredSlots > ParallelSlots && server == null;
        public int ActiveJudges => activeJudges;
        public int ActiveChats => activeChats;
        public int OwnedProcessId { get { try { return server != null && !server.HasExited ? server.Id : 0; } catch (InvalidOperationException) { return 0; } } }
        public int LastStoppedProcessId { get; private set; }
        public bool LastStopConfirmed { get; private set; } = true;
        public string LastOwnedLogPath { get; private set; } = "";
        public float LastJudgeSeconds { get; private set; }
        public string LastJudgeFailure { get; private set; } = "";

        const int MaxQueuedChats = 16, MaxQueuedJudges = 12;
        sealed class Job
        {
            public string body;
            public Action<string> done;
            public Action<double?, string> judged;
            public Action<string> rawObserver;
            public Func<bool> dispatch;
            public object owner;
            public UnityWebRequest request;
            public bool cancelled, completed;
            public bool Judge => judged != null;
        }
        readonly Queue<Job> jobs = new Queue<Job>();
        readonly Queue<Job> judges = new Queue<Job>();
        readonly HashSet<Job> active = new HashSet<Job>();
        int activeChats, activeJudges, desiredSlots = 1;
        bool changingSlots, disposing;
        float judgeRetryAfter;
        Process server;
        string editorQaLogPath;

        string BaseUrl => "http://127.0.0.1:" + endpointPort;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.TestMode) return;
            var llm = runtime.GetComponent<LocalLlm>() ?? runtime.gameObject.AddComponent<LocalLlm>();
            llm.Begin();
        }

        void Begin()
        {
            initialized = resumeOnEnable = true;
            StartService();
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
#endif
            if (initialized && resumeOnEnable && Application.isPlaying) StartService();
        }

        void StartService()
        {
            if (!isActiveAndEnabled || (State != Status.Off && State != Status.Failed)) return;
            if (server != null && !StopServer()) return;
            disposing = false; changingSlots = false; judgeRetryAfter = 0;
            if (!editorQaInstance) Instance = this;
            StartCoroutine(Boot());
        }

        IEnumerator Boot(bool probeExisting = true)
        {
            State = Status.Starting;
            Detail = GameText.T("正在唤醒本地模型…", "Waking the local model…");
            // A server from an earlier play session may still be up: reuse it.
            bool alive = false;
            if (probeExisting) yield return Health(ok => alive = ok);
            if (alive)
            {
                if (editorQaInstance) { Fail("QA port is already occupied; refusing to reuse or manage that service."); yield break; }
                // Do not assume an externally managed service has four slots, and never restart/kill it.
                ParallelSlots = 1;
                ModelName = "llama-server :" + endpointPort; State = Status.Ready; Detail = ""; yield break;
            }

            string model = FindModel(), exe = FindServer();
            if (model == null) { Fail(GameText.T("没找到模型文件（.gguf）。放到 StreamingAssets/LingGuang/Models 或项目根目录 Models/。", "No .gguf model found (StreamingAssets/LingGuang/Models or <project>/Models).")); yield break; }
            if (exe == null) { Fail(GameText.T("没找到 llama-server。", "llama-server not found.")); yield break; }
            ModelName = Path.GetFileNameWithoutExtension(model);
            // Unity does not promise to keep the executable bit when it copies StreamingAssets into a build.
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
            {
                try { using (var chmod = Process.Start(new ProcessStartInfo("/bin/chmod", "+x \"" + exe + "\"") { UseShellExecute = false, CreateNoWindow = true })) chmod.WaitForExit(3000); }
                catch (Exception error) { Debug.LogWarning("[灵光] chmod 失败：" + error.Message); }
            }
            try
            {
                ParallelSlots = desiredSlots;
                LastOwnedLogPath = editorQaInstance && !string.IsNullOrEmpty(editorQaLogPath)
                    ? editorQaLogPath + ".np" + ParallelSlots + "." + DateTime.UtcNow.Ticks + ".log" : "";
                var info = new ProcessStartInfo(exe,
                    "-m \"" + model + "\" --host 127.0.0.1 --port " + endpointPort + " -c " + (8192 * ParallelSlots) + " -np " + ParallelSlots + " -ngl 99 --jinja --reasoning-budget 0"
                    + (LastOwnedLogPath.Length > 0 ? " --log-file \"" + LastOwnedLogPath + "\"" : ""))
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = false, RedirectStandardError = false, WorkingDirectory = Path.GetDirectoryName(exe) };
                server = Process.Start(info);
            }
            catch (Exception error) { Fail("llama-server: " + error.Message); yield break; }

            float deadline = Time.realtimeSinceStartup + 120;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (server == null || server.HasExited) { Fail(GameText.T("llama-server 启动后退出了。", "llama-server exited during startup.")); yield break; }
                yield return Health(ok => alive = ok);
                if (alive) { State = Status.Ready; Detail = ""; Debug.Log("[灵光] 本地模型就绪：" + ModelName); yield break; }
                yield return new WaitForSecondsRealtime(.5f);
            }
            Fail(GameText.T("本地模型启动超时。", "Local model start timed out."));
        }

        void Fail(string why)
        {
            State = Status.Failed;
            Detail = why;
            Debug.LogWarning("[灵光] 本地模型不可用，改用预设回复：" + why);
            StopServer();
            while (jobs.Count > 0) Finish(jobs.Dequeue(), null, "offline");
            while (judges.Count > 0) Finish(judges.Dequeue(), null, "offline");
        }

        IEnumerator Health(Action<bool> result)
        {
            using (var request = UnityWebRequest.Get(BaseUrl + "/health"))
            {
                request.timeout = 2;
                request.redirectLimit = 0;
                yield return request.SendWebRequest();
                result(request.result == UnityWebRequest.Result.Success);
            }
        }

        static string FindModel()
        {
            var dirs = new List<string> { Path.Combine(Application.streamingAssetsPath, "LingGuang", "Models") };
            if (Application.isEditor) dirs.Add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Models")));
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                var files = Directory.GetFiles(dir, "*.gguf");
                Array.Sort(files, StringComparer.Ordinal);
                foreach (var f in files) if (!Path.GetFileName(f).StartsWith("mmproj", StringComparison.OrdinalIgnoreCase) && new FileInfo(f).Length > 100_000_000) return f;
            }
            return null;
        }

        /// <summary>The bundled server for this OS and CPU (llama/mac-arm64 or llama/win-x64), then a system install.</summary>
        static string FindServer()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "LingGuang", "llama");
            var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
            var candidates = new List<string>();
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    if (arch == System.Runtime.InteropServices.Architecture.X64) candidates.Add(Path.Combine(root, "win-x64", "llama-server.exe"));
                    break;
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    if (arch == System.Runtime.InteropServices.Architecture.Arm64) candidates.Add(Path.Combine(root, "mac-arm64", "llama-server"));
                    candidates.Add("/opt/homebrew/bin/llama-server");
                    candidates.Add("/usr/local/bin/llama-server");
                    break;
                default:
                    candidates.Add("/usr/local/bin/llama-server");
                    candidates.Add("/usr/bin/llama-server");
                    break;
            }
            foreach (var c in candidates) if (File.Exists(c)) return c;
            return null;
        }

        // ───────────── chat ─────────────

        /// <summary>Queue a chat completion. done(null) on failure. Messages are (role, content) pairs.</summary>
        /// <param name="thinking">Thinking mode (design v1.1 §11.5: only at stage 6's full open).</param>
        /// <param name="sampling">
        /// Optional sampler settings: a GBNF grammar (stages 1–2 of 灵光's speech) and repeat / presence / frequency / DRY
        /// penalties. Null keeps the defaults every other caller uses (top_p .8, top_k 20, presence_penalty 1.2).
        /// </param>
        public void Chat(IList<KeyValuePair<string, string>> messages, int maxTokens, float temperature, Action<string> done, bool thinking = false, XgSampling sampling = null)
        {
            if ((!Ready && !changingSlots) || disposing || messages == null || jobs.Count >= MaxQueuedChats) { done?.Invoke(null); return; }
            var sb = new StringBuilder("{\"messages\":[");
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"role\":").Append(Quote(messages[i].Key)).Append(",\"content\":").Append(Quote(messages[i].Value)).Append('}');
            }
            sb.Append("],\"max_tokens\":").Append(maxTokens)
              .Append(",\"temperature\":").Append(temperature.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
              .Append(sampling != null ? sampling.JsonFields() : ",\"top_p\":0.8,\"top_k\":20,\"presence_penalty\":1.2")
              .Append(",\"stream\":false")
              .Append(",\"chat_template_kwargs\":{\"enable_thinking\":").Append(thinking ? "true" : "false").Append("}}");
            jobs.Enqueue(new Job { body = sb.ToString(), done = done });
            Pump();
        }

        /// <summary>
        /// Queues a bounded, low-priority real judge request. dispatch runs immediately before sending:
        /// callers revalidate save identity/power and bill work there, not while this request is waiting.
        /// Returns false if not accepted; accepted jobs complete once, including explicit failure/cancellation.
        /// </summary>
        public bool Judge(string prompt, Action<double?, string> done, Func<bool> dispatch = null, object owner = null)
            => QueueJudge(prompt, done, dispatch, owner);

        bool QueueJudge(string prompt, Action<double?, string> done, Func<bool> dispatch, object owner, int qaSlot = -1, Action<string> rawObserver = null)
        {
            if (done == null || !JudgeReady || disposing || judges.Count >= MaxQueuedJudges || string.IsNullOrWhiteSpace(prompt)) return false;
            string body;
            try { body = XgJudgeProtocol.RequestBody(prompt); }
            catch (ArgumentException) { return false; }
            if (qaSlot >= 0) body = body.Substring(0, body.Length - 1) + ",\"id_slot\":" + qaSlot + "}";
            judges.Enqueue(new Job { body = body, judged = done, dispatch = dispatch, owner = owner, rawObserver = rawObserver });
            Pump();
            return true;
        }

        public void SetParallelJudging(bool enabled)
        {
            desiredSlots = enabled ? 4 : 1;
            // Owned services are resized only when all in-flight jobs have drained. An external service is untouched.
            Pump();
        }

        public void CancelJudges(object owner)
        {
            if (owner == null) return;
            int count = judges.Count;
            while (count-- > 0)
            {
                var job = judges.Dequeue();
                if (!ReferenceEquals(job.owner, owner)) { judges.Enqueue(job); continue; }
                job.cancelled = true;
                Finish(job, null, "cancelled");
            }
            // Aborting is asynchronous; its coroutine still owns disposal and removes the active entry.
            foreach (var job in active)
                if (job.Judge && ReferenceEquals(job.owner, owner)) { job.cancelled = true; job.request?.Abort(); }
        }

        void Update() { Pump(); }

        void Pump()
        {
            if (!Ready || changingSlots || disposing) return;
            if (server != null && server.HasExited) { Fail(GameText.T("本地服务已退出。", "Local server exited.")); return; }
            if (server != null && desiredSlots != ParallelSlots)
            {
                if (active.Count == 0) StartCoroutine(ChangeSlots());
                return;
            }
            while (active.Count < ParallelSlots)
            {
                Job job = null;
                if (jobs.Count > 0) job = jobs.Dequeue();
                else if (JudgeReady && judges.Count > 0 && activeJudges < (ParallelSlots >= 4 ? 3 : 1)) job = judges.Dequeue();
                if (job == null) break;
                active.Add(job);
                if (job.Judge) activeJudges++; else activeChats++;
                StartCoroutine(RunJob(job));
            }
        }

        IEnumerator ChangeSlots()
        {
            changingSlots = true;
            State = Status.Starting;
            Detail = GameText.T("正在调整本地推理并发…", "Reconfiguring local inference slots…");
            if (!StopServer()) { State = Status.Failed; Detail = "Owned local process did not exit; refusing to start another."; changingSlots = false; yield break; }
            yield return null;
            yield return Boot(false);
            changingSlots = false;
        }

        IEnumerator RunJob(Job job)
        {
            bool allowed = !job.cancelled;
            if (allowed && job.dispatch != null)
            {
                try { allowed = job.dispatch(); }
                catch (Exception error) { allowed = false; Debug.LogException(error); }
            }
            string raw = null, failure = allowed ? null : "cancelled";
            float started = Time.realtimeSinceStartup;
            if (allowed)
            {
                using (var request = new UnityWebRequest(BaseUrl + "/v1/chat/completions", "POST"))
                {
                    job.request = request;
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(job.body));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = job.Judge ? 12 : 60;
                    request.redirectLimit = 0;
                    yield return request.SendWebRequest();
                    if (job.cancelled) failure = "cancelled";
                    else if (request.result == UnityWebRequest.Result.Success) raw = request.downloadHandler.text;
                    else failure = "request_failed";
                    job.request = null;
                }
            }
            active.Remove(job);
            if (job.Judge) { activeJudges--; LastJudgeSeconds = Time.realtimeSinceStartup - started; }
            else activeChats--;
            Finish(job, raw, failure);
        }

        void Finish(Job job, string raw, string failure)
        {
            if (job.completed) return;
            job.completed = true;
            try
            {
                if (job.Judge)
                {
                    double? p = null;
                    if (failure == null && XgJudgeProtocol.TryParseProbability(raw, out double value, out failure)) p = value;
                    LastJudgeFailure = failure ?? "";
                    if (failure != null && failure != "cancelled") judgeRetryAfter = Time.realtimeSinceStartup + 2;
                    try { job.rawObserver?.Invoke(raw); } catch (Exception error) { Debug.LogException(error); }
                    job.judged(p, failure);
                    return;
                }
                string reply = null;
                if (failure == null) try
                {
                    var parsed = JsonUtility.FromJson<Response>(raw);
                    if (parsed?.choices != null && parsed.choices.Length > 0 && parsed.choices[0].message != null)
                        reply = Clean(parsed.choices[0].message.content);
                }
                catch (Exception error) { Debug.LogWarning("[灵光] 模型回复解析失败：" + error.Message); }
                job.done?.Invoke(string.IsNullOrWhiteSpace(reply) ? null : reply);
            }
            catch (Exception error)
            {
                Debug.LogWarning("[灵光] 模型回复处理失败：" + error.Message);
            }
        }

        [Serializable] sealed class Response { public Choice[] choices; }
        [Serializable] sealed class Choice { public Message message; }
        [Serializable] sealed class Message { public string content; }

        static readonly Regex Think = new Regex("<think>[\\s\\S]*?</think>", RegexOptions.Compiled);
        static readonly Regex Speaker = new Regex("^(老周|表姐|灵光|" + AppNames.AiZh + "|我|阿杰|老板|网管|小刚)[：:]\\s*", RegexOptions.Compiled);

        /// <summary>Chat-window text: no reasoning, no markdown, no speaker prefix, at most three short lines.</summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = Think.Replace(text, "").Replace("<think>", "").Replace("</think>", "");
            text = text.Replace("**", "").Replace("##", "").Replace("`", "").Trim().Trim('"', '“', '”', '「', '」');
            text = Speaker.Replace(text, "");
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var kept = new List<string>();
            foreach (var l in lines) { var t = l.Trim(); if (t.Length > 0) kept.Add(t); if (kept.Count == 3) break; }
            text = string.Join("\n", kept);
            return text.Length > 260 ? text.Substring(0, 260) + "…" : text;
        }

        static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
                }
            }
            return sb.Append('"').ToString();
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
#endif
            Shutdown();
            if (Instance == this) Instance = null;
        }
        void OnDestroy() { Shutdown(); if (Instance == this) Instance = null; }
        void OnApplicationQuit() { resumeOnEnable = false; Shutdown(); }

#if UNITY_EDITOR
        void BeforeAssemblyReload()
        {
            // The QA coordinator cannot survive a reload; never silently recreate its test process.
            if (editorQaInstance) resumeOnEnable = false;
            Shutdown();
        }

        /// <summary>Isolated Play-only QA instance. Never changes Instance and never adopts an occupied port.</summary>
        public void StartEditorQa(int port, string logPath = null)
        {
            if (!Application.isPlaying || !isActiveAndEnabled) throw new InvalidOperationException("QA inference requires an active Play-mode component.");
            if (port < 1024 || port > 65535 || port == Port) throw new ArgumentOutOfRangeException(nameof(port), "QA must use a distinct loopback port.");
            if (logPath != null && logPath.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0) throw new ArgumentException("Invalid QA log path.", nameof(logPath));
            if (initialized && !editorQaInstance || server != null || State == Status.Starting || State == Status.Ready)
                throw new InvalidOperationException("Cannot repurpose an initialized/live inference instance.");
            editorQaInstance = true; endpointPort = port; editorQaLogPath = logPath;
            initialized = resumeOnEnable = true; desiredSlots = 1;
            StartService();
        }

        /// <summary>Exercises the real queue/parser with an explicit backend slot and raw reply evidence.</summary>
        public bool JudgeForEditorQa(string prompt, int slot, Action<double?, string, string> done, Func<bool> dispatch = null, object owner = null)
        {
            if (!editorQaInstance || done == null || slot < 0 || slot >= ParallelSlots) return false;
            string raw = null;
            return QueueJudge(prompt, (p, failure) => done(p, failure, raw), dispatch, owner, slot, value => raw = value);
        }

        public void StopEditorQa()
        {
            if (!editorQaInstance) throw new InvalidOperationException("Only an isolated QA instance may use this stop seam.");
            resumeOnEnable = false; Shutdown();
        }
#endif

        void Shutdown()
        {
            if (disposing) { if (server != null) StopServer(); return; }
            disposing = true;
            try
            {
                StopAllCoroutines();
                var cancelled = new List<Job>(active);
                active.Clear(); activeChats = activeJudges = 0;
                foreach (var job in cancelled)
                {
                    job.cancelled = true;
                    try { job.request?.Abort(); } catch (Exception) { }
                    try { job.request?.Dispose(); } catch (Exception) { }
                    job.request = null;
                    Finish(job, null, "cancelled");
                }
                while (jobs.Count > 0) Finish(jobs.Dequeue(), null, "cancelled");
                while (judges.Count > 0) Finish(judges.Dequeue(), null, "cancelled");
            }
            finally { StopServer(); changingSlots = false; State = Status.Off; }
        }

        bool StopServer()
        {
            // Ownership is the Process handle created above, never a discovered/listening PID.
            if (server == null) return true;
            try
            {
                LastStoppedProcessId = server.Id;
                if (!server.HasExited) { server.Kill(); server.WaitForExit(5000); }
                LastStopConfirmed = server.HasExited;
                if (!LastStopConfirmed) { Debug.LogError("[灵光] Owned inference process did not exit: " + LastStoppedProcessId); return false; }
                server.Dispose(); server = null;
                return true;
            }
            catch (Exception error)
            {
                LastStopConfirmed = false;
                Debug.LogWarning("[灵光] Owned inference cleanup failed: " + error.Message);
                return false;
            }
        }
    }
}
