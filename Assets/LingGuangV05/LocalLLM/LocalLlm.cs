using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LingGuangV05.Core.Chat;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using AppNames = LingGuangV05.Core.AppNames;
using LingGuangV05.Core;
using LlamaNative = UndreamAI.LlamaLib.LlamaLib;
using NativeService = UndreamAI.LlamaLib.LLMService;
using NativeClient = UndreamAI.LlamaLib.LLMClient;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// Local language model for free chat (Qwen3.5-4B GGUF by default). Runs llama.cpp in-process through LLM for
    /// Unity's LlamaLib (package ai.undream.llm): one service with a slot per character, one LlamaLib client per slot
    /// so each request carries its own sampling and grammar. Offline: nothing leaves the machine.
    /// Model search: StreamingAssets/LingGuang/Models/*.gguf, then (editor) &lt;project&gt;/Models/*.gguf.
    /// LlamaLib picks its own native library per OS (GPU builds first, then CPU) from StreamingAssets/LlamaLib-*.
    /// If the model or the runtime is missing, <see cref="Ready"/> stays false and callers fall back to scripted replies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalLlm : MonoBehaviour
    {
        /// <summary>
        /// The game's own service is called directly and opens no port. Kept as the reserved number an isolated QA
        /// instance must not use for its HTTP endpoint (StartEditorQa), which lets QA read /props and /slots.
        /// </summary>
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
        /// <summary>This component owns a live model service.</summary>
        public bool OwnsServer => native != null;
        public bool ParallelUnavailable => parallelJudging && native == null && ParallelSlots < LlmScheduling.ParallelJudgingSlots;
        public int ActiveJudges => activeJudges;
        /// <summary>Visible chat replies in flight.</summary>
        public int ActiveChats => activeChats;
        /// <summary>Requests are pinned to per-character slots (the game's service has 3+ slots).</summary>
        public bool Routed => routed;
        /// <summary>How the service was started, and how many of the model's layers llama.cpp put on the GPU (-1 unknown).</summary>
        public LlmLaunchMode LaunchMode { get; private set; } = LlmLaunchMode.Gpu;
        public int GpuLayers => gpuLayers;
        public int ModelLayers => modelLayers;
        /// <summary>LlamaLib's native library in use (e.g. a Metal, Vulkan or CPU build).</summary>
        public string Architecture { get; private set; } = "";
        /// <summary>Part or all of the model runs on the CPU: replies are slower and timeouts longer.</summary>
        public bool SlowMode { get; private set; }
        /// <summary>The player typed in a focused text field within the last moment: background work holds off.</summary>
        public bool PlayerTyping => Time.realtimeSinceStartup - lastTypingAt < TypingPause;
        /// <summary>For text inputs the automatic check cannot see (e.g. UI Toolkit fields): call on each keystroke.</summary>
        public void NoteTyping() { lastTypingAt = Time.realtimeSinceStartup; }
        /// <summary>Id of the live service (unique per start, never reused; 0 = none). Replaces the old server PID.</summary>
        public int OwnedServiceId => native != null ? native.id : 0;
        public int LastStoppedServiceId { get; private set; }
        public bool LastStopConfirmed { get; private set; } = true;
        /// <summary>QA only: the file the native log of the current service is copied to.</summary>
        public string LastOwnedLogPath { get; private set; } = "";
        public float LastJudgeSeconds { get; private set; }
        public string LastJudgeFailure { get; private set; } = "";
        /// <summary>Native completions running right now (what llama-server's /slots "is_processing" used to show).</summary>
        public int NativeCallsInFlight { get { int n = 0; foreach (int c in nativeInFlight) n += c; return n; } }
        /// <summary>A native completion is running in this service slot.</summary>
        public bool NativeSlotRunning(int slot) => slot >= 0 && slot < nativeInFlight.Length && nativeInFlight[slot] > 0;

        /// <summary>True once the service with this id has been freed (or never existed).</summary>
        public static bool ServiceStopped(int id)
        {
            if (id <= 0) return true;
            lock (stoppedIds) return stoppedIds.Contains(id);
        }

        const int MaxQueuedChats = 16, MaxQueuedJudges = 12, MaxQueuedBackground = 6;
        const float TypingPause = 1.5f, AutoRestartWindow = 600, WarmupRecheck = 2, WarmupMinGap = 30;
        /// <summary>Judges keep their own short limit from dispatch, as the HTTP timeout did before.</summary>
        const float JudgeSeconds = 12;
        /// <summary>After a cancel, how long a native call may take to return before its slot is handed back anyway.</summary>
        const float CancelGrace = 5;
        const int StopWaitMs = 5000;
        /// <summary>LlamaLib debug level while a service starts (log threshold 4, where llama.cpp reports GPU offload) and after.</summary>
        const int BootLogLevel = 6, RunLogLevel = 1;
        const string DefaultSampling = ",\"top_p\":0.8,\"top_k\":20,\"presence_penalty\":1.2";

        sealed class Job
        {
            public IList<KeyValuePair<string, string>> messages;
            public JObject parameters;
            public bool thinking;
            public int hash;
            public Action<string> done;
            public Action<double?, string> judged;
            public Action<string> rawObserver;
            public Func<bool> dispatch;
            public object owner;
            public LlmSeat seat = LlmSeat.Others;
            public LlmLane lane = LlmLane.Visible;
            /// <summary>pinned: a slot this job must use (warm-up); slot: the scheduled slot (-1 = any free one);
            /// qaSlot: the backend slot an isolated QA judge asks for; native: the slot the call actually ran in.</summary>
            public int pinned = -1, slot = -1, qaSlot = -1, native = -1;
            public float queuedAt, deadline;
            public bool warmup, cancelled, timedOut, completed;
            public bool Judge => judged != null;
        }

        /// <summary>One started LlamaLib service and its per-slot clients.</summary>
        sealed class Native
        {
            public int id, slots;
            public NativeService service;
            public NativeClient[] clients;
            public bool serving;
            /// <summary>A native call reported a crash status: the service is unusable and is restarted once.</summary>
            public volatile bool broken;
            public readonly HashSet<Task> running = new HashSet<Task>();
        }

        // Visible chat, background chat (incl. warm-ups) and judges wait in separate lists; see Pump for the order.
        readonly List<Job> visible = new List<Job>();
        readonly List<Job> background = new List<Job>();
        readonly List<Job> judges = new List<Job>();
        readonly HashSet<Job> active = new HashSet<Job>();
        readonly bool[] slotBusy = new bool[16];
        readonly bool[] nativeBusy = new bool[16];
        readonly int[] nativeInFlight = new int[16];
        int activeChats, activeBackground, activeJudges, activeWarmups, desiredSlots = LlmScheduling.NormalSlots;
        bool changingSlots, disposing, routed, parallelJudging;
        float judgeRetryAfter, lastTypingAt = float.NegativeInfinity, lastAutoRestartAt = float.NegativeInfinity;
        Native native;
        /// <summary>A stopped service whose native calls had not returned yet; freed when they do.</summary>
        volatile Native draining;
        /// <summary>The model load in progress (Boot); abandoned by a shutdown, it frees its result when it ends.</summary>
        Task<Native> creating;
        string editorQaLogPath;

        static int serviceCounter;
        static readonly HashSet<int> stoppedIds = new HashSet<int>();

        // Native log lines arrive on LlamaLib's threads through one process-wide callback: only the layer count, a
        // short tail and (QA) a log file are kept, per listening instance.
        static readonly object logLock = new object();
        static readonly List<LocalLlm> logListeners = new List<LocalLlm>();
        static LlamaNative.CharArrayCallback logCallback;
        readonly object outputLock = new object();
        readonly Queue<string> outputTail = new Queue<string>();
        int gpuLayers = -1, modelLayers = -1;
        StreamWriter qaLog;

        // Warm-up: one tiny request per character slot with that slot's stable prompt prefix (LlmWarmup).
        readonly int[] warmedHash = new int[3];
        readonly float[] warmedAt = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
        float nextWarmupCheck;

        // Typing watch: the focused TMP input's text changing counts as typing.
        GameObject typedObject;
        string typedText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.TestMode) return;
            var llm = runtime.GetComponent<LocalLlm>() ?? runtime.gameObject.AddComponent<LocalLlm>();
            llm.Begin();
            RagLibrary.Load();
            LocalEmbedder.Attach(runtime.gameObject);
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
            if (native != null && !StopService()) return;
            disposing = false; changingSlots = false; judgeRetryAfter = 0;
            // Windows stays on the CPU runtime, as it did with the bundled llama-server (its GPU path was never verified).
            // LlamaBuildStrip removes LlamaLib's Windows GPU libraries from builds because of this line.
            LaunchMode = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor
                ? LlmLaunchMode.Cpu : LlmLaunchMode.Gpu;
            if (!editorQaInstance) Instance = this;
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            State = Status.Starting;
            Detail = Lang.T("正在唤醒本地模型…");
            SlowMode = false; routed = false;
            for (int i = 0; i < warmedHash.Length; i++) { warmedHash[i] = 0; warmedAt[i] = float.NegativeInfinity; }
            if (draining != null) { Fail("The previous model service is still finishing its requests; refusing to start another."); yield break; }
            if (editorQaInstance && !PortFree(endpointPort)) { Fail("QA port is already occupied; refusing to reuse or manage that service."); yield break; }

            string model = FindModel();
            if (model == null) { Fail(Lang.T("没找到模型文件（.gguf）。放到 StreamingAssets/LingGuang/Models 或项目根目录 Models/。")); yield break; }
            ModelName = Path.GetFileNameWithoutExtension(model);
            ParallelSlots = desiredSlots;
            // The QA harness keeps its historic unpinned behaviour; the game pins each character to its own slot.
            bool routeOwned = !editorQaInstance && LlmScheduling.Routed(ParallelSlots);
            // Start ladder: GPU with --fit (offloads what fits, rest on CPU) → same with an f16 KV cache → CPU only.
            // LlamaLib itself also walks its native builds (e.g. CUDA → Vulkan → CPU) inside each step.
            var mode = LaunchMode;
            while (true)
            {
                float started = Time.realtimeSinceStartup;
                BeginLogCapture(mode);
                int slots = ParallelSlots, port = editorQaInstance ? endpointPort : 0;
                creating = Task.Run(() => CreateNative(mode, model, slots, port));
                while (!creating.IsCompleted && Time.realtimeSinceStartup < started + 120) yield return null;
                var attempt = creating;
                if (!attempt.IsCompleted)
                {
                    AbandonStart();
                    EndLogCapture();
                    Fail(Lang.T("本地模型启动超时。"));
                    yield break;
                }
                creating = null;
                if (attempt.Status == TaskStatus.RanToCompletion && attempt.Result != null)
                {
                    native = attempt.Result;
                    LaunchMode = mode; routed = routeOwned;
                    Architecture = native.service.llamaLib?.architecture ?? "";
                    LlamaNative.Debug(RunLogLevel);
                    int onGpu = Volatile.Read(ref gpuLayers), total = Volatile.Read(ref modelLayers);
                    SlowMode = mode == LlmLaunchMode.Cpu || (total > 0 && onGpu < total);
                    State = Status.Ready;
                    Detail = LlmScheduling.SlowNotice(mode, onGpu, total, GameText.IsEnglish);
                    if (Application.platform == RuntimePlatform.WindowsPlayer && mode == LlmLaunchMode.Cpu)
                        Detail = GameText.T("当前配置使用本地 CPU 推理，回复可能慢一些。", "Configured for local CPU inference; replies may take longer.");
                    Debug.Log("[灵光] 本地模型就绪：" + ModelName + " · " + mode + " · " + Architecture + " · GPU 层 " + (total > 0 ? onGpu + "/" + total : "?") +
                        " · " + ParallelSlots + " slots × " + LlmScheduling.SlotContext + " · 启动 " + (Time.realtimeSinceStartup - started).ToString("0.0") + "s" +
                        (Detail.Length > 0 ? " · " + Detail : ""));
                    yield break;
                }
                var error = attempt.Exception?.GetBaseException();
                string tail = OutputTail() + (error != null ? "\n" + error.Message : "");
                if (error != null && error.Message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 && error.Message.IndexOf("Library", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.LogWarning("[灵光] LlamaLib 运行库缺失（LLM for Unity 第一次打开工程时会下载）：\n" + tail);
                    EndLogCapture();
                    Fail(Lang.T("没找到 LlamaLib 运行库（LLM for Unity）。"));
                    yield break;
                }
                if (tail.Contains("missing tensor 'blk."))
                    Debug.LogWarning("[灵光] 模型缺张量：新版 Qwen3.5 GGUF 带 MTP（nextn）层，LlamaLib 的 llama.cpp 还不认，用 Tools/LocalLLM/strip_qwen_mtp.py 去掉它。");
                if (!LlmScheduling.Fallback(mode, out var next))
                {
                    Debug.LogWarning("[灵光] 本地模型启动失败（" + mode + "）：\n" + tail);
                    EndLogCapture();
                    Fail(Lang.T("本地模型启动失败了。"));
                    yield break;
                }
                Debug.LogWarning("[灵光] 本地模型以 " + mode + " 启动失败，改用 " + next + " 重试：\n" + tail);
                mode = next;
                Detail = Lang.T("显存不够，换一种方式启动本地模型…");
            }
        }

        /// <summary>Worker thread: load the model and start the service. Throws when this mode cannot start.</summary>
        static Native CreateNative(LlmLaunchMode mode, string model, int slots, int port)
        {
            LlamaNative library = null;
            NativeService service = null;
            try
            {
                library = new LlamaNative(mode != LlmLaunchMode.Cpu);
                var pointer = library.LLMService_From_Command(LlmScheduling.ServiceArgs(mode, model, slots));
                service = new NativeService(library, pointer);
                library = null; // owned by the service from here on
                service.EnableReasoning(false);
                if (!service.Start()) throw new InvalidOperationException("LlamaLib service did not start.");
                var created = new Native { id = Interlocked.Increment(ref serviceCounter), slots = slots, service = service, clients = new NativeClient[slots] };
                for (int i = 0; i < slots; i++) created.clients[i] = new NativeClient(service);
                if (port > 0) { service.StartServer("127.0.0.1", port); created.serving = true; }
                return created;
            }
            catch
            {
                try { service?.Dispose(); } catch (Exception) { }
                try { library?.Dispose(); } catch (Exception) { }
                throw;
            }
        }

        /// <summary>A model load nobody waits for any more: free its service when it finishes; nothing else starts meanwhile.</summary>
        void AbandonStart()
        {
            var attempt = creating;
            creating = null;
            if (attempt == null) return;
            var orphan = new Native();
            draining = orphan;
            attempt.ContinueWith(t => { if (t.Status == TaskStatus.RanToCompletion) FreeNative(t.Result); if (draining == orphan) draining = null; });
        }

        static bool PortFree(int port)
        {
            TcpListener listener = null;
            try { listener = new TcpListener(IPAddress.Loopback, port); listener.Start(); return true; }
            catch (SocketException) { return false; }
            finally { listener?.Stop(); }
        }

        // ───────────── native log ─────────────

        void BeginLogCapture(LlmLaunchMode mode)
        {
            Volatile.Write(ref gpuLayers, -1); Volatile.Write(ref modelLayers, -1);
            lock (outputLock)
            {
                outputTail.Clear();
                qaLog?.Dispose(); qaLog = null;
                LastOwnedLogPath = "";
                if (editorQaInstance && !string.IsNullOrEmpty(editorQaLogPath))
                {
                    LastOwnedLogPath = editorQaLogPath + ".np" + ParallelSlots + "." + DateTime.UtcNow.Ticks + ".log";
                    try { qaLog = new StreamWriter(LastOwnedLogPath, false, new UTF8Encoding(false)) { AutoFlush = true }; qaLog.WriteLine("# " + mode + " " + LlmScheduling.ServiceArgs(mode, "<model>", ParallelSlots)); }
                    catch (Exception error) { Debug.LogWarning("[灵光] QA log: " + error.Message); qaLog = null; }
                }
            }
            lock (logLock)
            {
                if (!logListeners.Contains(this)) logListeners.Add(this);
                if (logCallback == null)
                {
                    logCallback = OnNativeLog;
                    LlamaNative.LoggingCallback(logCallback);
                }
            }
            LlamaNative.Debug(BootLogLevel);
        }

        /// <summary>Stops listening; the process-wide callback is removed with the last listener (before a domain reload).</summary>
        void EndLogCapture()
        {
            lock (outputLock) { qaLog?.Dispose(); qaLog = null; }
            lock (logLock)
            {
                logListeners.Remove(this);
                if (logListeners.Count == 0 && logCallback != null)
                {
                    try { LlamaNative.LoggingStop(); } catch (Exception) { }
                    logCallback = null;
                }
            }
        }

#if ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(LlamaNative.CharArrayCallback))]
        static void OnNativeLog(IntPtr text) => OnNativeLine(text == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(text));
#else
        static void OnNativeLog(string text) => OnNativeLine(text);
#endif

        /// <summary>LlamaLib thread: hand each line to every listening instance.</summary>
        static void OnNativeLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            LocalLlm[] listeners;
            lock (logLock) listeners = logListeners.ToArray();
            foreach (var line in text.Split('\n'))
            {
                if (line.Length == 0) continue;
                foreach (var listener in listeners) listener.OnServerLine(line);
            }
        }

        /// <summary>Worker thread: remember the GPU layer count and keep a short tail for failure reports.</summary>
        void OnServerLine(string line)
        {
            if (LlmScheduling.TryParseOffload(line, out int onGpu, out int total)) { Volatile.Write(ref gpuLayers, onGpu); Volatile.Write(ref modelLayers, total); }
            lock (outputLock)
            {
                outputTail.Enqueue(line.Length > 300 ? line.Substring(0, 300) : line);
                while (outputTail.Count > 40) outputTail.Dequeue();
                try { qaLog?.WriteLine(line); } catch (Exception) { }
            }
        }

        string OutputTail() { lock (outputLock) return string.Join("\n", outputTail); }

        void Fail(string why)
        {
            State = Status.Failed;
            Detail = why;
            Debug.LogWarning("[灵光] 本地模型不可用，改用预设回复：" + why);
            StopService();
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

        // ───────────── chat ─────────────

        /// <summary>Queue a chat completion. done(null) on failure or timeout. Messages are (role, content) pairs.</summary>
        /// <param name="thinking">Thinking mode (design v1.1 §11.5: only at stage 6's full open).</param>
        /// <param name="sampling">
        /// Optional sampler settings: a GBNF grammar (stages 1–2 of 灵光's speech) and repeat / presence / frequency / DRY
        /// penalties. Null keeps the defaults every other caller uses (top_p .8, top_k 20, presence_penalty 1.2).
        /// </param>
        /// <param name="seat">Who speaks: each character keeps its own service slot (and prompt cache).</param>
        /// <param name="lane">
        /// Visible: the player waits for it (served first, ~20 s budget). Background: nobody waits (queued behind all
        /// visible work, held while the player types, ~40 s budget, then done(null)).
        /// </param>
        public void Chat(IList<KeyValuePair<string, string>> messages, int maxTokens, float temperature, Action<string> done, bool thinking = false, XgSampling sampling = null,
            LlmSeat seat = LlmSeat.Others, LlmLane lane = LlmLane.Visible)
        {
            var queue = lane == LlmLane.Visible ? visible : background;
            if ((!Ready && !changingSlots) || disposing || messages == null || queue.Count >= (lane == LlmLane.Visible ? MaxQueuedChats : MaxQueuedBackground)) { done?.Invoke(null); return; }
            Enqueue(queue, ChatJob(messages, maxTokens, temperature, thinking, sampling, done, seat, lane));
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
            Enqueue(background, ChatJob(messages, maxTokens, temperature, false, sampling, done, seat, LlmLane.Background));
            return true;
        }

        static Job ChatJob(IList<KeyValuePair<string, string>> messages, int maxTokens, float temperature, bool thinking, XgSampling sampling,
            Action<string> done, LlmSeat seat, LlmLane lane) =>
            new Job { messages = new List<KeyValuePair<string, string>>(messages), parameters = ChatParameters(maxTokens, temperature, sampling), thinking = thinking, done = done, seat = seat, lane = lane };

        void Enqueue(List<Job> queue, Job job)
        {
            float now = Time.realtimeSinceStartup;
            job.queuedAt = now;
            job.deadline = now + LlmScheduling.Timeout(job.lane, SlowMode);
            queue.Add(job);
            Pump();
        }

        /// <summary>llama.cpp /completion sampling for one chat request (the same values the OpenAI-style body carried).</summary>
        static JObject ChatParameters(int maxTokens, float temperature, XgSampling sampling)
        {
            var parameters = JObject.Parse("{" + (sampling != null ? sampling.JsonFields() : DefaultSampling).Substring(1) + "}");
            parameters["n_predict"] = maxTokens;
            parameters["temperature"] = Math.Round(temperature, 2);
            parameters["cache_prompt"] = true;
            return parameters;
        }

        /// <summary>One token, greedy, with the top candidates' log-probabilities (what XgJudgeProtocol reads).</summary>
        static JObject JudgeParameters() => new JObject
        {
            ["n_predict"] = 1,
            ["temperature"] = 0,
            ["n_probs"] = XgJudgeProtocol.TopCandidates,
            ["cache_prompt"] = true,
        };

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
            IList<KeyValuePair<string, string>> messages;
            try { messages = XgJudgeProtocol.Messages(prompt); }
            catch (ArgumentException) { return false; }
            // Judges are background work: they wait for visible chat and give up after the background budget.
            Enqueue(judges, new Job { messages = messages, parameters = JudgeParameters(), judged = done, dispatch = dispatch, owner = owner, rawObserver = rawObserver, lane = LlmLane.Background, qaSlot = qaSlot });
            return true;
        }

        public void SetParallelJudging(bool enabled)
        {
            parallelJudging = enabled;
            desiredSlots = LlmScheduling.SlotCount(enabled, editorQaInstance);
            // The service is resized only when all in-flight jobs have drained.
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
            // Cancelling is asynchronous; RunJob still owns the native call and removes the active entry.
            foreach (var job in active)
                if (job.Judge && ReferenceEquals(job.owner, owner)) Cancel(job);
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
            if (native != null && native.broken) { ServiceDied(); return; }
            if (native != null && desiredSlots != ParallelSlots)
            {
                foreach (var job in active) if (job.warmup) Cancel(job);
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
            // The service always gets a concrete slot (an unrouted request takes any free one) so it can be cancelled.
            job.native = job.qaSlot >= 0 ? job.qaSlot : slot >= 0 ? slot : FreeNativeSlot();
            if (slot >= 0 && slot < slotBusy.Length) slotBusy[slot] = true;
            if (job.native >= 0 && job.native < nativeBusy.Length) nativeBusy[job.native] = true;
            active.Add(job);
            if (job.warmup) activeWarmups++;
            else if (job.Judge) activeJudges++;
            else if (job.lane == LlmLane.Visible) activeChats++;
            else activeBackground++;
            StartCoroutine(RunJob(job));
        }

        int FreeNativeSlot()
        {
            for (int i = 0; i < ParallelSlots && i < nativeBusy.Length; i++) if (!nativeBusy[i]) return i;
            return -1;
        }

        void Release(Job job)
        {
            if (!active.Remove(job)) return;
            if (job.slot >= 0 && job.slot < slotBusy.Length) slotBusy[job.slot] = false;
            if (job.native >= 0 && job.native < nativeBusy.Length) nativeBusy[job.native] = false;
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
            Array.Clear(nativeBusy, 0, nativeBusy.Length);
            Array.Clear(nativeInFlight, 0, nativeInFlight.Length);
        }

        void AbortWarmupOn(int slot)
        {
            foreach (var job in active)
                if (job.warmup && job.slot == slot && !job.cancelled) Cancel(job);
        }

        /// <summary>Marks an in-flight job cancelled and asks the service to stop its slot (RunJob reports it).</summary>
        void Cancel(Job job)
        {
            if (job.cancelled) return;
            job.cancelled = true;
            CancelNative(native, job.native);
        }

        static void CancelNative(Native target, int slot)
        {
            if (target == null || slot < 0 || slot >= target.slots) return;
            try { target.service.Cancel(slot); } catch (Exception) { }
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
        /// The service reported a crash status while in use: callers get null now (they fall back to offline lines)
        /// and it is restarted once; a second failure within <see cref="AutoRestartWindow"/> seconds is final.
        /// </summary>
        void ServiceDied()
        {
            string tail = OutputTail();
            if (editorQaInstance || Time.realtimeSinceStartup - lastAutoRestartAt < AutoRestartWindow)
            {
                Debug.LogWarning("[灵光] 本地模型服务出错：\n" + tail);
                Fail(Lang.T("本地服务已退出。"));
                return;
            }
            lastAutoRestartAt = Time.realtimeSinceStartup;
            Debug.LogWarning("[灵光] 本地模型服务意外出错，自动重启一次：\n" + tail);
            foreach (var job in active) Cancel(job);
            FinishQueued("offline");
            StopAllCoroutines();
            var cancelled = new List<Job>(active);
            ClearActive();
            foreach (var job in cancelled) Finish(job, null, "cancelled");
            AbandonStart();
            if (!StopService()) { Fail("Owned model service did not stop; refusing to start another."); return; }
            State = Status.Off;
            StartCoroutine(Boot());
        }

        IEnumerator ChangeSlots()
        {
            changingSlots = true;
            State = Status.Starting;
            Detail = Lang.T("正在调整本地推理并发…");
            if (!StopService()) { State = Status.Failed; Detail = "Owned model service did not stop; refusing to start another."; changingSlots = false; yield break; }
            yield return null;
            yield return Boot();
            changingSlots = false;
        }

        // ───────────── warm-up ─────────────

        /// <summary>
        /// When idle, sends each character slot one tiny request with its stable prompt prefix (LlmWarmup), so the
        /// service holds a checkpoint there and the first real reply only processes the conversation. Re-sent when the
        /// prefix changes (at most every <see cref="WarmupMinGap"/> s). Background priority; cancelled if its slot is needed.
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
                int hash = Hash(messages);
                if (hash == warmedHash[seat]) continue;
                warmedHash[seat] = hash; warmedAt[seat] = now;
                Enqueue(background, new Job { messages = messages, parameters = ChatParameters(1, 0, null), seat = (LlmSeat)seat, lane = LlmLane.Background, pinned = seat, warmup = true });
                return;
            }
        }

        static int Hash(IList<KeyValuePair<string, string>> messages)
        {
            unchecked
            {
                int hash = 17;
                foreach (var m in messages) hash = (hash * 31 + (m.Key ?? "").GetHashCode()) * 31 + (m.Value ?? "").GetHashCode();
                return hash;
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
            var target = native;
            if (allowed && target == null) failure = "offline";
            else if (allowed)
            {
                int slot = job.native;
                var client = target.clients[slot >= 0 && slot < target.clients.Length ? slot : 0];
                var call = Task.Run(() => Complete(target, client, job.messages, job.parameters, job.thinking, slot));
                lock (target.running) target.running.Add(call);
                call.ContinueWith(t => { lock (target.running) target.running.Remove(t); });
                int tracked = slot >= 0 && slot < nativeInFlight.Length ? slot : -1;
                if (tracked >= 0) nativeInFlight[tracked]++;
                // Judges keep their own short limit; chat gets what is left of its visible / background budget.
                float limit = job.Judge ? started + JudgeSeconds : job.deadline;
                float cancelledAt = float.PositiveInfinity;
                while (!call.IsCompleted)
                {
                    float now = Time.realtimeSinceStartup;
                    if (float.IsPositiveInfinity(cancelledAt) && (job.cancelled || now > limit))
                    {
                        if (!job.cancelled) { job.timedOut = true; job.cancelled = true; CancelNative(target, slot); }
                        cancelledAt = now;
                    }
                    // A native call that ignores the cancel must not hold its slot (and the queue) forever.
                    if (now > cancelledAt + CancelGrace) break;
                    yield return null;
                }
                if (tracked >= 0 && nativeInFlight[tracked] > 0) nativeInFlight[tracked]--;
                if (job.timedOut) failure = "timeout";
                else if (job.cancelled) failure = "cancelled";
                else if (call.Status == TaskStatus.RanToCompletion) { raw = call.Result; if (string.IsNullOrEmpty(raw)) failure = "request_failed"; }
                else
                {
                    failure = "request_failed";
                    var error = call.Exception?.GetBaseException();
                    // LlamaLib reports a crash (negative status) on every later call: restart the service.
                    if (error != null && error.Message.StartsWith("LlamaLib error -", StringComparison.Ordinal)) target.broken = true;
                    if (error != null) Debug.LogWarning("[灵光] 本地模型请求失败：" + error.Message);
                }
            }
            Release(job);
            if (job.Judge) LastJudgeSeconds = Time.realtimeSinceStartup - started;
            Finish(job, raw, failure);
        }

        /// <summary>
        /// Worker thread: chat template (thinking off service-wide, opened per request), this slot's sampling, then a
        /// raw llama.cpp /completion on that slot. Returns the completion's JSON (content, probabilities or error).
        /// </summary>
        static readonly object TemplateLock = new object();

        static string Complete(Native target, NativeClient client, IList<KeyValuePair<string, string>> messages, JObject parameters, bool thinking, int slot)
        {
            var library = target.service.llamaLib;
            var conversation = new JArray();
            foreach (var m in messages) conversation.Add(new JObject { ["role"] = m.Key, ["content"] = m.Value ?? "" });
            string prompt;
            // LlamaLib v2.0.5 crashes (EXC_BAD_ACCESS in LLM::apply_template) when two threads apply the template at
            // once, so that step is serialized; it takes well under a millisecond. Completions still run in parallel.
            lock (TemplateLock)
            {
                prompt = Marshal.PtrToStringUTF8(library.LLM_Apply_Template(client.llm, conversation.ToString(Newtonsoft.Json.Formatting.None)));
                // The grammar travels inside the parameters (XgSampling.JsonFields); the client's own one stays empty.
                library.LLM_Set_Grammar(client.llm, "");
                library.LLM_Set_Completion_Parameters(client.llm, parameters.ToString(Newtonsoft.Json.Formatting.None));
            }
            if (string.IsNullOrEmpty(prompt)) throw new InvalidOperationException("Empty chat template result.");
            prompt = LlmScheduling.WithThinking(prompt, thinking);
            return Marshal.PtrToStringUTF8(library.LLM_Completion(client.llm, prompt, null, slot, true));
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
                    string reply = failure == null ? XgJudgeProtocol.FromCompletion(raw) : null;
                    if (failure == null && XgJudgeProtocol.TryParseProbability(reply, out double value, out failure)) p = value;
                    LastJudgeFailure = failure ?? "";
                    if (failure != null && failure != "cancelled") judgeRetryAfter = Time.realtimeSinceStartup + 2;
                    try { job.rawObserver?.Invoke(reply ?? raw); } catch (Exception error) { Debug.LogException(error); }
                    job.judged(p, failure);
                    return;
                }
                string text = null;
                if (failure == null) try
                {
                    var parsed = JObject.Parse(raw);
                    if (parsed["error"] != null) Debug.LogWarning("[灵光] 模型回复出错：" + parsed["error"].ToString(Newtonsoft.Json.Formatting.None));
                    else text = Clean(AfterThinking(parsed.Value<string>("content")));
                }
                catch (Exception error) { Debug.LogWarning("[灵光] 模型回复解析失败：" + error.Message); }
                job.done?.Invoke(string.IsNullOrWhiteSpace(text) ? null : text);
            }
            catch (Exception error)
            {
                Debug.LogWarning("[灵光] 模型回复处理失败：" + error.Message);
            }
        }

        /// <summary>
        /// A raw completion returns the model's reasoning inline (llama-server's chat endpoint used to split it off).
        /// When the prompt already opened the block, only "…&lt;/think&gt;" appears, so keep what follows the last close.
        /// </summary>
        static string AfterThinking(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int end = text.LastIndexOf("</think>", StringComparison.Ordinal);
            return end >= 0 ? text.Substring(end + "</think>".Length) : text;
        }

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
            // The QA coordinator cannot survive a reload; never silently recreate its test service.
            if (editorQaInstance) resumeOnEnable = false;
            Shutdown();
        }

        /// <summary>
        /// Isolated Play-only QA instance. Never changes Instance; its service also listens on 127.0.0.1:port (LlamaLib's
        /// built-in llama.cpp HTTP server) so QA can read /props and /slots. Refuses an occupied port.
        /// </summary>
        public void StartEditorQa(int port, string logPath = null)
        {
            if (!Application.isPlaying || !isActiveAndEnabled) throw new InvalidOperationException("QA inference requires an active Play-mode component.");
            if (port < 1024 || port > 65535 || port == Port) throw new ArgumentOutOfRangeException(nameof(port), "QA must use a distinct loopback port.");
            if (logPath != null && logPath.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0) throw new ArgumentException("Invalid QA log path.", nameof(logPath));
            if (initialized && !editorQaInstance || native != null || State == Status.Starting || State == Status.Ready)
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
            if (disposing) { if (native != null) StopService(); return; }
            disposing = true;
            try
            {
                StopAllCoroutines();
                var cancelled = new List<Job>(active);
                foreach (var job in cancelled) { job.cancelled = true; CancelNative(native, job.native); }
                ClearActive();
                foreach (var job in cancelled) Finish(job, null, "cancelled");
                FinishQueued("cancelled");
            }
            finally { AbandonStart(); StopService(); EndLogCapture(); changingSlots = false; State = Status.Off; }
        }

        /// <summary>
        /// Cancels every slot, waits for in-flight native calls, then frees the service. If a call does not return in
        /// time the service is freed later (when it does) and false is returned: nothing new starts meanwhile.
        /// </summary>
        bool StopService()
        {
            if (native == null) return true;
            var stopping = native;
            native = null;
            LastStoppedServiceId = stopping.id;
            for (int slot = 0; slot < stopping.slots; slot++) CancelNative(stopping, slot);
            Task[] running;
            lock (stopping.running) running = new List<Task>(stopping.running).ToArray();
            bool drained = true;
            if (running.Length > 0)
            {
                try { drained = Task.WaitAll(running, StopWaitMs); }
                catch (AggregateException) { drained = true; }
            }
            if (!drained)
            {
                LastStopConfirmed = false;
                Debug.LogError("[灵光] Model service " + stopping.id + " still has native calls running; it is freed when they return.");
                draining = stopping;
                Task.WhenAll(running).ContinueWith(_ => { FreeNative(stopping); if (draining == stopping) draining = null; });
                return false;
            }
            FreeNative(stopping);
            LastStopConfirmed = true;
            return true;
        }

        static void FreeNative(Native target)
        {
            if (target == null || target.service == null) return;
            try
            {
                if (target.serving) try { target.service.StopServer(); } catch (Exception) { }
                foreach (var client in target.clients)
                    if (client != null && client.llm != IntPtr.Zero)
                        try { target.service.llamaLib?.LLM_Delete(client.llm); client.llm = IntPtr.Zero; } catch (Exception) { }
                try { target.service.Stop(); } catch (Exception) { }
                target.service.Dispose();
            }
            catch (Exception error) { Debug.LogWarning("[灵光] Model service cleanup failed: " + error.Message); }
            finally { lock (stoppedIds) stoppedIds.Add(target.id); }
        }
    }
}
