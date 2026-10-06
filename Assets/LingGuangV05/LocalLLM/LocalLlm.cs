using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using LingGuangV05.Core.Chat;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using AppNames = LingGuangV05.Core.AppNames;
using LingGuangV05.Core;
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
        /// <summary>Why it failed, what it is doing while starting, or (while ready) a short slow-hardware note.</summary>
        public string Detail { get; private set; } = "";
        public string ModelName { get; private set; } = "";
        public bool Ready => State == Status.Ready;
        /// <summary>Any real work queued or running (warm-up requests do not count).</summary>
        public bool Busy => active.Count > activeWarmups || visible.Count > 0 || background.Count > 0 || judges.Count > 0;
        /// <summary>A reply the player is waiting for is queued or running.</summary>
        public bool ChatBusy => visible.Count > 0 || activeChats > 0;
        public bool JudgeReady => Ready && !changingSlots && Time.realtimeSinceStartup >= judgeRetryAfter;
        public int ParallelSlots { get; private set; } = 1;
        public bool OwnsServer => server != null;
        public bool ParallelUnavailable => parallelJudging && server == null && ParallelSlots < LlmScheduling.ParallelJudgingSlots;
        public int ActiveJudges => activeJudges;
        /// <summary>Visible chat replies in flight.</summary>
        public int ActiveChats => activeChats;
        /// <summary>Requests are pinned to per-character slots (owned game server, or an external one with 3+ slots).</summary>
        public bool Routed => routed;
        /// <summary>How the owned server was started, and how many of the model's layers llama.cpp put on the GPU (-1 unknown).</summary>
        public LlmLaunchMode LaunchMode { get; private set; } = LlmLaunchMode.Gpu;
        public int GpuLayers => gpuLayers;
        public int ModelLayers => modelLayers;
        /// <summary>Part or all of the model runs on the CPU: replies are slower and timeouts longer.</summary>
        public bool SlowMode { get; private set; }
        /// <summary>The player typed in a focused text field within the last moment: background work holds off.</summary>
        public bool PlayerTyping => Time.realtimeSinceStartup - lastTypingAt < TypingPause;
        /// <summary>For text inputs the automatic check cannot see (e.g. UI Toolkit fields): call on each keystroke.</summary>
        public void NoteTyping() { lastTypingAt = Time.realtimeSinceStartup; }
        public int OwnedProcessId { get { try { return server != null && !server.HasExited ? server.Id : 0; } catch (InvalidOperationException) { return 0; } } }
        public int LastStoppedProcessId { get; private set; }
        public bool LastStopConfirmed { get; private set; } = true;
        public string LastOwnedLogPath { get; private set; } = "";
        public float LastJudgeSeconds { get; private set; }
        public string LastJudgeFailure { get; private set; } = "";

        const int MaxQueuedChats = 16, MaxQueuedJudges = 12, MaxQueuedBackground = 6;
        const float TypingPause = 1.5f, AutoRestartWindow = 600, WarmupRecheck = 2, WarmupMinGap = 30;
        sealed class Job
        {
            public string body;
            public Action<string> done;
            public Action<double?, string> judged;
            public Action<string> rawObserver;
            public Func<bool> dispatch;
            public object owner;
            public UnityWebRequest request;
            public LlmSeat seat = LlmSeat.Others;
            public LlmLane lane = LlmLane.Visible;
            /// <summary>pinned: a slot this job must use (warm-up); slot: the slot it was sent to (-1 = server's choice).</summary>
            public int pinned = -1, slot = -1;
            public float queuedAt, deadline;
            public bool warmup, cancelled, completed;
            public bool Judge => judged != null;
        }
        // Visible chat, background chat (incl. warm-ups) and judges wait in separate lists; see Pump for the order.
        readonly List<Job> visible = new List<Job>();
        readonly List<Job> background = new List<Job>();
        readonly List<Job> judges = new List<Job>();
        readonly HashSet<Job> active = new HashSet<Job>();
        readonly bool[] slotBusy = new bool[16];
        int activeChats, activeBackground, activeJudges, activeWarmups, desiredSlots = LlmScheduling.NormalSlots;
        bool changingSlots, disposing, routed, parallelJudging;
        float judgeRetryAfter, lastTypingAt = float.NegativeInfinity, lastAutoRestartAt = float.NegativeInfinity;
        Process server;
        string editorQaLogPath;

        // Server output is read on worker threads: only the layer count and a short tail are kept.
        readonly object outputLock = new object();
        readonly Queue<string> outputTail = new Queue<string>();
        int serverGeneration, gpuLayers = -1, modelLayers = -1;

        // Warm-up: one tiny request per character slot with that slot's stable prompt prefix (LlmWarmup).
        readonly int[] warmedHash = new int[3];
        readonly float[] warmedAt = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
        float nextWarmupCheck;

        // Typing watch: the focused TMP input's text changing counts as typing.
        GameObject typedObject;
        string typedText;

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
            LaunchMode = LlmLaunchMode.Gpu;
            if (!editorQaInstance) Instance = this;
            StartCoroutine(Boot());
        }

        IEnumerator Boot(bool probeExisting = true)
        {
            State = Status.Starting;
            Detail = Lang.T("正在唤醒本地模型…");
            SlowMode = false; routed = false;
            for (int i = 0; i < warmedHash.Length; i++) { warmedHash[i] = 0; warmedAt[i] = float.NegativeInfinity; }
            // A server from an earlier play session may still be up: reuse it.
            bool alive = false;
            if (probeExisting) yield return Health(ok => alive = ok);
            if (alive)
            {
                if (editorQaInstance) { Fail("QA port is already occupied; refusing to reuse or manage that service."); yield break; }
                // Never restart/kill an external service. Ask it how many slots it has; requests are pinned to the
                // per-character slots only if it has at least three, otherwise the server picks.
                int slots = 1;
                yield return ProbeSlots(n => slots = n);
                ParallelSlots = slots; routed = LlmScheduling.Routed(slots);
                ModelName = "llama-server :" + endpointPort; State = Status.Ready; Detail = "";
                Debug.Log("[灵光] 使用已在运行的本地模型服务 :" + endpointPort + "（" + slots + " slots，" + (routed ? "按角色分槽" : "不分槽") + "）");
                yield break;
            }

            string model = FindModel(), exe = FindServer();
            if (model == null) { Fail(Lang.T("没找到模型文件（.gguf）。放到 StreamingAssets/LingGuang/Models 或项目根目录 Models/。")); yield break; }
            if (exe == null) { Fail(Lang.T("没找到 llama-server。")); yield break; }
            ModelName = Path.GetFileNameWithoutExtension(model);
            // Unity does not promise to keep the executable bit when it copies StreamingAssets into a build.
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
            {
                try { using (var chmod = Process.Start(new ProcessStartInfo("/bin/chmod", "+x \"" + exe + "\"") { UseShellExecute = false, CreateNoWindow = true })) chmod.WaitForExit(3000); }
                catch (Exception error) { Debug.LogWarning("[灵光] chmod 失败：" + error.Message); }
            }
            ParallelSlots = desiredSlots;
            // The QA harness keeps its historic unpinned behaviour; the game pins each character to its own slot.
            bool routeOwned = !editorQaInstance && LlmScheduling.Routed(ParallelSlots);
            // Start ladder: GPU with --fit (offloads what fits, rest on CPU) → same with an f16 KV cache → CPU only.
            var mode = LaunchMode;
            while (true)
            {
                float started = Time.realtimeSinceStartup;
                if (!StartProcess(exe, model, mode)) yield break;
                bool exited = false;
                alive = false;
                while (Time.realtimeSinceStartup < started + 120)
                {
                    if (server == null || server.HasExited) { exited = true; break; }
                    yield return Health(ok => alive = ok);
                    if (alive) break;
                    yield return new WaitForSecondsRealtime(.5f);
                }
                if (alive)
                {
                    LaunchMode = mode; routed = routeOwned;
                    int onGpu = Volatile.Read(ref gpuLayers), total = Volatile.Read(ref modelLayers);
                    SlowMode = mode == LlmLaunchMode.Cpu || (total > 0 && onGpu < total);
                    State = Status.Ready;
                    Detail = LlmScheduling.SlowNotice(mode, onGpu, total, GameText.IsEnglish);
                    Debug.Log("[灵光] 本地模型就绪：" + ModelName + " · " + mode + " · GPU 层 " + (total > 0 ? onGpu + "/" + total : "?") +
                        " · " + ParallelSlots + " slots × " + LlmScheduling.SlotContext + " · 启动 " + (Time.realtimeSinceStartup - started).ToString("0.0") + "s" +
                        (Detail.Length > 0 ? " · " + Detail : ""));
                    yield break;
                }
                if (!exited) { Fail(Lang.T("本地模型启动超时。")); yield break; }
                string tail = OutputTail();
                if (!LlmScheduling.Fallback(mode, out var next))
                {
                    Debug.LogWarning("[灵光] llama-server 启动后退出（" + mode + "）：\n" + tail);
                    Fail(Lang.T("llama-server 启动后退出了。"));
                    yield break;
                }
                Debug.LogWarning("[灵光] llama-server 以 " + mode + " 启动失败，改用 " + next + " 重试：\n" + tail);
                StopServer();
                mode = next;
                Detail = Lang.T("显存不够，换一种方式启动本地模型…");
            }
        }

        bool StartProcess(string exe, string model, LlmLaunchMode mode)
        {
            try
            {
                LastOwnedLogPath = editorQaInstance && !string.IsNullOrEmpty(editorQaLogPath)
                    ? editorQaLogPath + ".np" + ParallelSlots + "." + DateTime.UtcNow.Ticks + ".log" : "";
                var info = new ProcessStartInfo(exe, LlmScheduling.ServerArgs(mode, model, endpointPort, ParallelSlots, LastOwnedLogPath))
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Path.GetDirectoryName(exe) };
                int generation = Interlocked.Increment(ref serverGeneration);
                Volatile.Write(ref gpuLayers, -1); Volatile.Write(ref modelLayers, -1);
                lock (outputLock) outputTail.Clear();
                var process = new Process { StartInfo = info };
                // The pipes must be drained continuously or the server blocks on a full buffer.
                process.OutputDataReceived += (_, e) => OnServerLine(generation, e.Data);
                process.ErrorDataReceived += (_, e) => OnServerLine(generation, e.Data);
                process.Start();
                server = process;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return true;
            }
            catch (Exception error) { Fail("llama-server: " + error.Message); return false; }
        }

        /// <summary>Worker thread: remember the GPU layer count and keep a short tail for failure reports.</summary>
        void OnServerLine(int generation, string line)
        {
            if (line == null || generation != Volatile.Read(ref serverGeneration)) return;
            if (LlmScheduling.TryParseOffload(line, out int onGpu, out int total)) { Volatile.Write(ref gpuLayers, onGpu); Volatile.Write(ref modelLayers, total); }
            lock (outputLock)
            {
                outputTail.Enqueue(line.Length > 300 ? line.Substring(0, 300) : line);
                while (outputTail.Count > 40) outputTail.Dequeue();
            }
        }

        string OutputTail() { lock (outputLock) return string.Join("\n", outputTail); }

        [Serializable] sealed class Props { public int total_slots; }

        /// <summary>Slot count of an external server from /props (1 if it does not say).</summary>
        IEnumerator ProbeSlots(Action<int> result)
        {
            using (var request = UnityWebRequest.Get(BaseUrl + "/props"))
            {
                request.timeout = 3;
                request.redirectLimit = 0;
                yield return request.SendWebRequest();
                int slots = 1;
                if (request.result == UnityWebRequest.Result.Success)
                    try { var props = JsonUtility.FromJson<Props>(request.downloadHandler.text); if (props != null && props.total_slots > 0) slots = props.total_slots; }
                    catch (Exception) { }
                result(Math.Min(slots, slotBusy.Length));
            }
        }

        void Fail(string why)
        {
            State = Status.Failed;
            Detail = why;
            Debug.LogWarning("[灵光] 本地模型不可用，改用预设回复：" + why);
            StopServer();
            FinishQueued("offline");
        }

        /// <summary>Completes every queued (not yet sent) request with null / the failure.</summary>
        void FinishQueued(string failure)
        {
            foreach (var list in new[] { visible, background, judges })
            {
                var drained = new List<Job>(list);
                list.Clear();
                foreach (var job in drained) Finish(job, null, failure);
            }
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

        /// <summary>Queue a chat completion. done(null) on failure or timeout. Messages are (role, content) pairs.</summary>
        /// <param name="thinking">Thinking mode (design v1.1 §11.5: only at stage 6's full open).</param>
        /// <param name="sampling">
        /// Optional sampler settings: a GBNF grammar (stages 1–2 of 灵光's speech) and repeat / presence / frequency / DRY
        /// penalties. Null keeps the defaults every other caller uses (top_p .8, top_k 20, presence_penalty 1.2).
        /// </param>
        /// <param name="seat">Who speaks: each character keeps its own server slot (and prompt cache).</param>
        /// <param name="lane">
        /// Visible: the player waits for it (served first, ~20 s budget). Background: nobody waits (queued behind all
        /// visible work, held while the player types, ~40 s budget, then done(null)).
        /// </param>
        public void Chat(IList<KeyValuePair<string, string>> messages, int maxTokens, float temperature, Action<string> done, bool thinking = false, XgSampling sampling = null,
            LlmSeat seat = LlmSeat.Others, LlmLane lane = LlmLane.Visible)
        {
            var queue = lane == LlmLane.Visible ? visible : background;
            if ((!Ready && !changingSlots) || disposing || messages == null || queue.Count >= (lane == LlmLane.Visible ? MaxQueuedChats : MaxQueuedBackground)) { done?.Invoke(null); return; }
            Enqueue(queue, new Job { body = Body(messages, maxTokens, temperature, thinking, sampling), done = done, seat = seat, lane = lane });
        }

        /// <summary>
        /// Background request that is dropped instead of queued while the model is busy: returns false (and never calls
        /// <paramref name="done"/>) when the model is not ready, a visible reply is waiting or running, the player is
        /// typing, or the background queue is full. When it returns true, done is called exactly once (null on
        /// failure or after the ~40 s background budget).
        /// </summary>
        public bool ChatBackground(IList<KeyValuePair<string, string>> messages, int maxTokens, float temperature, Action<string> done, XgSampling sampling = null, LlmSeat seat = LlmSeat.Others)
        {
            if (!Ready || changingSlots || disposing || messages == null || done == null || background.Count >= MaxQueuedBackground ||
                !LlmScheduling.BackgroundMayStart(visible.Count, activeChats, PlayerTyping)) return false;
            Enqueue(background, new Job { body = Body(messages, maxTokens, temperature, false, sampling), done = done, seat = seat, lane = LlmLane.Background });
            return true;
        }

        void Enqueue(List<Job> queue, Job job)
        {
            float now = Time.realtimeSinceStartup;
            job.queuedAt = now;
            job.deadline = now + LlmScheduling.Timeout(job.lane, SlowMode);
            queue.Add(job);
            Pump();
        }

        static string Body(IList<KeyValuePair<string, string>> messages, int maxTokens, float temperature, bool thinking, XgSampling sampling)
        {
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
            return sb.ToString();
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
            // Judges are background work: they wait for visible chat and give up after the background budget.
            Enqueue(judges, new Job { body = body, judged = done, dispatch = dispatch, owner = owner, rawObserver = rawObserver, lane = LlmLane.Background });
            return true;
        }

        public void SetParallelJudging(bool enabled)
        {
            parallelJudging = enabled;
            desiredSlots = LlmScheduling.SlotCount(enabled, editorQaInstance);
            // Owned services are resized only when all in-flight jobs have drained. An external service is untouched.
            Pump();
        }

        public void CancelJudges(object owner)
        {
            if (owner == null) return;
            for (int i = 0; i < judges.Count; i++)
            {
                var job = judges[i];
                if (!ReferenceEquals(job.owner, owner)) continue;
                judges.RemoveAt(i--);
                job.cancelled = true;
                Finish(job, null, "cancelled");
            }
            // Aborting is asynchronous; its coroutine still owns disposal and removes the active entry.
            foreach (var job in active)
                if (job.Judge && ReferenceEquals(job.owner, owner)) { job.cancelled = true; job.request?.Abort(); }
        }

        void Update()
        {
            WatchTyping();
            Pump();
            QueueWarmup();
        }

        /// <summary>A focused TMP / uGUI input whose text changed counts as typing (UI Toolkit fields call NoteTyping).</summary>
        void WatchTyping()
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            var selected = events != null ? events.currentSelectedGameObject : null;
            string text = null;
            if (selected != null)
            {
                var tmp = selected.GetComponent<TMPro.TMP_InputField>();
                if (tmp != null) { if (tmp.isFocused) text = tmp.text; }
                else { var legacy = selected.GetComponent<UnityEngine.UI.InputField>(); if (legacy != null && legacy.isFocused) text = legacy.text; }
            }
            if (text != null && ReferenceEquals(selected, typedObject) && typedText != null && text != typedText) lastTypingAt = Time.realtimeSinceStartup;
            typedObject = text != null ? selected : null;
            typedText = text;
        }

        /// <summary>
        /// Dispatch order: visible chat first, each to its character's slot; background work (judges, background chat,
        /// warm-ups) only while no visible reply is waiting or running and the player is not typing.
        /// </summary>
        void Pump()
        {
            if (!Ready || changingSlots || disposing) return;
            if (server != null && server.HasExited) { ServerDied(); return; }
            if (server != null && desiredSlots != ParallelSlots)
            {
                foreach (var job in active) if (job.warmup) { job.cancelled = true; job.request?.Abort(); }
                if (active.Count == 0) StartCoroutine(ChangeSlots());
                return;
            }
            Expire(visible); Expire(background); Expire(judges);
            for (int i = 0; i < visible.Count; i++)
            {
                var job = visible[i];
                int slot = PickSlot(job);
                if (slot == LlmScheduling.Wait) { if (routed) AbortWarmupOn(LlmScheduling.SeatSlot(job.seat, ParallelSlots)); continue; }
                visible.RemoveAt(i--);
                Run(job, slot);
            }
            if (!LlmScheduling.BackgroundMayStart(visible.Count, activeChats, PlayerTyping)) return;
            while (true)
            {
                bool judgeOk = judges.Count > 0 && JudgeReady && activeJudges < LlmScheduling.JudgeLimit(ParallelSlots, routed);
                int pick = LlmScheduling.OlderHead(judgeOk, judgeOk ? judges[0].queuedAt : 0, background.Count > 0, background.Count > 0 ? background[0].queuedAt : 0);
                if (pick < 0) return;
                var list = pick == 0 ? judges : background;
                var job = list[0];
                int slot = PickSlot(job);
                if (slot == LlmScheduling.Wait)
                {
                    // A warm-up waiting for its own busy slot must not hold up the rest of the background queue.
                    if (pick == 1 && job.warmup) { background.RemoveAt(0); Finish(job, null, "busy"); continue; }
                    return;
                }
                list.RemoveAt(0);
                Run(job, slot);
            }
        }

        int PickSlot(Job job) =>
            LlmScheduling.PickSlot(job.seat, job.lane, ParallelSlots, routed, active.Count, s => s >= 0 && s < slotBusy.Length && slotBusy[s], job.pinned);

        void Run(Job job, int slot)
        {
            job.slot = slot;
            if (slot >= 0 && slot < slotBusy.Length) slotBusy[slot] = true;
            active.Add(job);
            if (job.warmup) activeWarmups++;
            else if (job.Judge) activeJudges++;
            else if (job.lane == LlmLane.Visible) activeChats++;
            else activeBackground++;
            StartCoroutine(RunJob(job));
        }

        void Release(Job job)
        {
            if (!active.Remove(job)) return;
            if (job.slot >= 0 && job.slot < slotBusy.Length) slotBusy[job.slot] = false;
            if (job.warmup) activeWarmups--;
            else if (job.Judge) activeJudges--;
            else if (job.lane == LlmLane.Visible) activeChats--;
            else activeBackground--;
        }

        void ClearActive()
        {
            active.Clear();
            activeChats = activeBackground = activeJudges = activeWarmups = 0;
            Array.Clear(slotBusy, 0, slotBusy.Length);
        }

        void AbortWarmupOn(int slot)
        {
            foreach (var job in active)
                if (job.warmup && job.slot == slot && !job.cancelled) { job.cancelled = true; job.request?.Abort(); }
        }

        /// <summary>Queued requests past their budget complete with null instead of waiting forever.</summary>
        void Expire(List<Job> list)
        {
            float now = Time.realtimeSinceStartup;
            for (int i = 0; i < list.Count; i++)
            {
                if (now <= list[i].deadline) continue;
                var job = list[i];
                list.RemoveAt(i--);
                Finish(job, null, "timeout");
            }
        }

        /// <summary>
        /// The owned server died while in use: callers get null now (they fall back to offline lines) and it is
        /// restarted once; a second death within <see cref="AutoRestartWindow"/> seconds is a failure.
        /// </summary>
        void ServerDied()
        {
            string tail = OutputTail();
            if (editorQaInstance || Time.realtimeSinceStartup - lastAutoRestartAt < AutoRestartWindow)
            {
                Debug.LogWarning("[灵光] 本地模型进程退出：\n" + tail);
                Fail(Lang.T("本地服务已退出。"));
                return;
            }
            lastAutoRestartAt = Time.realtimeSinceStartup;
            Debug.LogWarning("[灵光] 本地模型进程意外退出，自动重启一次：\n" + tail);
            foreach (var job in active) { job.cancelled = true; try { job.request?.Abort(); } catch (Exception) { } }
            FinishQueued("offline");
            if (!StopServer()) { Fail("Owned local process did not exit; refusing to start another."); return; }
            StartCoroutine(Boot(false));
        }

        IEnumerator ChangeSlots()
        {
            changingSlots = true;
            State = Status.Starting;
            Detail = Lang.T("正在调整本地推理并发…");
            if (!StopServer()) { State = Status.Failed; Detail = "Owned local process did not exit; refusing to start another."; changingSlots = false; yield break; }
            yield return null;
            yield return Boot(false);
            changingSlots = false;
        }

        // ───────────── warm-up ─────────────

        /// <summary>
        /// When idle, sends each character slot one tiny request with its stable prompt prefix (LlmWarmup), so the
        /// server holds a checkpoint there and the first real reply only processes the conversation. Re-sent when the
        /// prefix changes (at most every <see cref="WarmupMinGap"/> s). Background priority; aborted if its slot is needed.
        /// </summary>
        void QueueWarmup()
        {
            float now = Time.realtimeSinceStartup;
            if (!Ready || !routed || disposing || changingSlots || now < nextWarmupCheck) return;
            nextWarmupCheck = now + WarmupRecheck;
            if (active.Count > 0 || visible.Count > 0 || background.Count > 0 || judges.Count > 0 || PlayerTyping) return;
            for (int seat = 0; seat < warmedHash.Length && seat < ParallelSlots; seat++)
            {
                if (now < warmedAt[seat] + WarmupMinGap) continue;
                IList<KeyValuePair<string, string>> prefix;
                try { prefix = LlmWarmup.Prefix((LlmSeat)seat); }
                catch (Exception error) { Debug.LogException(error); warmedAt[seat] = now; continue; }
                if (prefix == null || prefix.Count == 0) continue;
                var messages = new List<KeyValuePair<string, string>>(prefix) { new KeyValuePair<string, string>("user", "在吗") };
                string body = Body(messages, 1, 0, false, null);
                int hash = body.GetHashCode();
                if (hash == warmedHash[seat]) continue;
                warmedHash[seat] = hash; warmedAt[seat] = now;
                Enqueue(background, new Job { body = body, seat = (LlmSeat)seat, lane = LlmLane.Background, pinned = seat, warmup = true });
                return;
            }
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
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(job.slot >= 0 ? LlmScheduling.WithSlot(job.body, job.slot) : job.body));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    // Judges keep their own short limit; chat gets what is left of its visible / background budget.
                    request.timeout = job.Judge ? 12 : Math.Max(1, Mathf.CeilToInt(job.deadline - started));
                    request.redirectLimit = 0;
                    yield return request.SendWebRequest();
                    if (job.cancelled) failure = "cancelled";
                    else if (request.result == UnityWebRequest.Result.Success) raw = request.downloadHandler.text;
                    else failure = request.error != null && request.error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0 ? "timeout" : "request_failed";
                    job.request = null;
                }
            }
            Release(job);
            if (job.Judge) LastJudgeSeconds = Time.realtimeSinceStartup - started;
            Finish(job, raw, failure);
        }

        void Finish(Job job, string raw, string failure)
        {
            if (job.completed) return;
            job.completed = true;
            // A warm-up that did not get through may be tried again later.
            if (job.warmup && failure != null && (int)job.seat < warmedHash.Length) warmedHash[(int)job.seat] = 0;
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
                ClearActive();
                foreach (var job in cancelled)
                {
                    job.cancelled = true;
                    try { job.request?.Abort(); } catch (Exception) { }
                    try { job.request?.Dispose(); } catch (Exception) { }
                    job.request = null;
                    Finish(job, null, "cancelled");
                }
                FinishQueued("cancelled");
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
