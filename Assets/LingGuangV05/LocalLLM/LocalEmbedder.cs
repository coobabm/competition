using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LingGuangV05.Core.Chat;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using LlamaNative = UndreamAI.LlamaLib.LlamaLib;
using NativeService = UndreamAI.LlamaLib.LLMService;
using NativeClient = UndreamAI.LlamaLib.LLMClient;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// The embedding model behind retrieval (RAG): bge-small-zh-v1.5 (26 MB, so the game fits in 2 GB; Qwen3-Embedding-0.6B
    /// still works when it is the file found instead) in its own LlamaLib service next to the chat
    /// model, called from one worker thread. It fills <see cref="RagVectors.Source"/>: texts are embedded once and
    /// cached (also on disk, shared by all saves), so a lookup never waits. A search text is embedded first, through
    /// <see cref="Prepare"/>, right before a prompt is built (≈10–30 ms on the GPU); stored texts are embedded in the
    /// background and fall back to BM25 until they are. Without the model everything stays BM25.
    /// Model search: StreamingAssets/LingGuang/Embedding/*.gguf, then (editor) &lt;project&gt;/Models/Embedding/*.gguf.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalEmbedder : MonoBehaviour, IVectorSource
    {
        public static LocalEmbedder Instance { get; private set; }
        public bool Ready => Volatile.Read(ref ready);
        public string ModelName { get; private set; } = "";
        public int Dimensions { get; private set; }
        /// <summary>Texts embedded and kept (memory and disk).</summary>
        public int Cached { get { lock (gate) return cache.Count; } }
        /// <summary>Texts waiting to be embedded.</summary>
        public int Pending { get { lock (gate) return urgent.Count + later.Count; } }

        /// <summary>Qwen3-Embedding wants an instruction in front of a search text (not in front of stored texts).</summary>
        public const string QueryInstruction = "Instruct: Given a chat message, retrieve memories, facts and past conversations relevant to it\nQuery:";
        /// <summary>bge-*-zh's instruction for a short search text against passages.</summary>
        public const string BgeZhQueryInstruction = "为这个句子生成表示以用于检索相关文章：";
        /// <summary>
        /// The search instruction of a model file, and whether it only reads Chinese: bge-small-zh's English vectors
        /// are noise (measured against Qwen3-Embedding on the knowledge files: F1 .50 in English, .63 in Chinese), so
        /// texts without Chinese get no vector and fall back to BM25, which suits English well.
        /// </summary>
        public static string InstructionFor(string modelName, out bool chineseOnly)
        {
            string name = (modelName ?? "").ToLowerInvariant();
            chineseOnly = name.Contains("bge") && name.Contains("-zh");
            return chineseOnly ? BgeZhQueryInstruction : QueryInstruction;
        }
        /// <summary>How long a prompt waits for its search text at most; after that it is built with BM25 alone.</summary>
        public const float PrepareSeconds = 1.5f;
        const int MaxChars = 600, MaxCached = 12000, MaxLater = 4000, SaveEvery = 64, StopWaitMs = 4000;
        const string CacheVersion = "rag1";

        readonly object gate = new object();
        readonly Dictionary<string, float[]> cache = new Dictionary<string, float[]>(StringComparer.Ordinal);
        readonly Queue<string> order = new Queue<string>();
        readonly Queue<string> urgent = new Queue<string>(), later = new Queue<string>();
        readonly HashSet<string> queued = new HashSet<string>(StringComparer.Ordinal);
        Thread worker;
        volatile bool stopping;
        /// <summary>Attached by the game: a disabled host frees the model, enabling it again restarts it.</summary>
        bool wanted;
        bool ready;
        int unsaved;
        string cachePath, modelPath, instruction = QueryInstruction;
        bool chineseOnly;
        Task<NativeService> starting;
        NativeService service;
        NativeClient client;

        /// <summary>Adds the embedder next to the chat model (LocalLlm.Attach).</summary>
        public static void Attach(GameObject host)
        {
            if (host == null) return;
            var e = host.GetComponent<LocalEmbedder>() ?? host.AddComponent<LocalEmbedder>();
            e.Begin();
        }

        void Begin()
        {
            wanted = true;
            if (service != null || starting != null || !isActiveAndEnabled) return;
            Instance = this;
            modelPath = FindModel();
            if (modelPath == null) { Debug.Log("[灵光] 没有向量模型（StreamingAssets/LingGuang/Embedding/*.gguf）：检索只用 BM25。"); return; }
            ModelName = Path.GetFileNameWithoutExtension(modelPath);
            instruction = InstructionFor(ModelName, out chineseOnly);
            cachePath = Path.Combine(LingGuangV05.Runtime.Persistence.VerificationSavePaths.ModelCacheDirectory ?? Path.Combine(Application.persistentDataPath, "LingGuangV05"), "embeddings-" + ModelName + ".bin");
            stopping = false;
            string model = modelPath, path = cachePath;
            bool gpu = Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor;
            starting = Task.Run(() => { LoadCache(path); return Create(model, gpu); });
            StartCoroutine(AwaitStart());
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
#endif
            if (wanted && Application.isPlaying) Begin();
        }
        void OnDisable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
#endif
            Shutdown();
        }
        void OnApplicationQuit() => Shutdown();

        IEnumerator AwaitStart()
        {
            float began = Time.realtimeSinceStartup;
            var task = starting;
            while (!task.IsCompleted) yield return null;
            // Shut down meanwhile: Shutdown frees the service when its start finishes.
            if (task != starting) yield break;
            if (task.Status != TaskStatus.RanToCompletion || task.Result == null)
            {
                Debug.LogWarning("[灵光] 向量模型启动失败，检索只用 BM25：" + task.Exception?.GetBaseException().Message);
                starting = null;
                yield break;
            }
            service = task.Result;
            starting = null;
            client = new NativeClient(service);
            Dimensions = service.EmbeddingSize();
            Volatile.Write(ref ready, true);
            RagVectors.Source = this;
            worker = new Thread(Work) { IsBackground = true, Name = "LingGuang embeddings" };
            worker.Start();
            RagLibrary.Warm(this);
            Debug.Log("[灵光] 向量模型就绪：" + ModelName + " · " + Dimensions + " 维 · " + service.llamaLib?.architecture + " · 缓存 " + Cached + " 条 · 启动 " + (Time.realtimeSinceStartup - began).ToString("0.0") + "s");
        }

        static NativeService Create(string model, bool gpu)
        {
            LlamaNative library = null;
            NativeService created = null;
            try
            {
                library = new LlamaNative(gpu);
                // One slot; the whole text in one batch (embeddings need it); pooling comes from the GGUF (last token).
                string args = "-m \"" + model + "\" -c 2048 -np 1 -b 2048 -ub 2048 --embedding" + (gpu ? "" : " --n-gpu-layers 0 --device none");
                created = new NativeService(library, library.LLMService_From_Command(args));
                library = null;
                if (!created.Start()) throw new InvalidOperationException("embedding service did not start");
                return created;
            }
            catch
            {
                try { created?.Dispose(); } catch (Exception) { }
                try { library?.Dispose(); } catch (Exception) { }
                throw;
            }
        }

        static string FindModel()
        {
            var dirs = new List<string> { Path.Combine(Application.streamingAssetsPath, "LingGuang", "Embedding") };
            if (Application.isEditor) dirs.Add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Models", "Embedding")));
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                var files = Directory.GetFiles(dir, "*.gguf");
                Array.Sort(files, StringComparer.Ordinal);
                if (files.Length > 0) return files[0];
            }
            return null;
        }

        // ───────────── IVectorSource ─────────────

        static string QueryKey(string text) => "q\u0001" + Trim(text);
        static string DocumentKey(string text) => "d\u0001" + Trim(text);
        static bool HasChinese(string text) { foreach (char c in text) if (c >= '\u4e00' && c <= '\u9fff') return true; return false; }
        static string Trim(string text) { text = (text ?? "").Trim(); return text.Length > MaxChars ? text.Substring(0, MaxChars) : text; }

        public float[] Query(string text) => Lookup(QueryKey(text), false);
        public float[] Document(string text) => Lookup(DocumentKey(text), true);

        float[] Lookup(string key, bool queueMissing)
        {
            if (key.Length <= 2 || chineseOnly && !HasChinese(key)) return null;
            lock (gate)
            {
                if (cache.TryGetValue(key, out var v)) return v;
                if (queueMissing && Ready && later.Count < MaxLater && queued.Add(key)) { later.Enqueue(key); Monitor.Pulse(gate); }
                return null;
            }
        }

        /// <summary>Embeds stored texts in the background (knowledge files on load, new chat lines).</summary>
        public void Warm(IEnumerable<string> texts)
        {
            if (texts == null || !Ready) return;
            foreach (var t in texts) Lookup(DocumentKey(t), true);
        }

        /// <summary>
        /// Embeds the search texts of the next prompt, then calls <paramref name="done"/> on the main thread (at once
        /// without a model, at most <see cref="PrepareSeconds"/> later otherwise). The prompt then finds by meaning.
        /// </summary>
        public static void Prepare(IList<string> queries, Action done)
        {
            var e = Instance;
            if (done == null) return;
            if (e == null || !e.Ready || !e.isActiveAndEnabled || queries == null || queries.Count == 0) { done(); return; }
            var keys = new List<string>();
            lock (e.gate)
                foreach (var q in queries)
                {
                    if (string.IsNullOrWhiteSpace(q)) continue;
                    string key = QueryKey(q);
                    if (e.cache.ContainsKey(key) || e.chineseOnly && !HasChinese(key)) continue;
                    keys.Add(key);
                    if (e.queued.Add(key)) e.urgent.Enqueue(key);
                    Monitor.Pulse(e.gate);
                }
            if (keys.Count == 0) { done(); return; }
            e.StartCoroutine(e.AwaitQueries(keys, done));
        }

        IEnumerator AwaitQueries(List<string> keys, Action done)
        {
            float until = Time.realtimeSinceStartup + PrepareSeconds;
            while (Time.realtimeSinceStartup < until && Ready)
            {
                bool all = true;
                lock (gate) foreach (var k in keys) if (!cache.ContainsKey(k)) { all = false; break; }
                if (all) break;
                yield return null;
            }
            try { done(); } catch (Exception error) { Debug.LogException(error); }
        }

        // ───────────── worker ─────────────

        void Work()
        {
            while (!stopping)
            {
                string key;
                lock (gate)
                {
                    while (!stopping && urgent.Count == 0 && later.Count == 0) Monitor.Wait(gate, 1000);
                    if (stopping) break;
                    key = urgent.Count > 0 ? urgent.Dequeue() : later.Dequeue();
                }
                float[] vector = null;
                try
                {
                    string text = key.StartsWith("q\u0001", StringComparison.Ordinal) ? instruction + key.Substring(2) : key.Substring(2);
                    vector = JArray.Parse(Marshal.PtrToStringUTF8(service.llamaLib.LLM_Embeddings(client.llm, text)) ?? "[]").ToObject<float[]>();
                }
                catch (Exception error) { Debug.LogWarning("[灵光] 向量计算失败：" + error.Message); }
                bool save = false;
                lock (gate)
                {
                    queued.Remove(key);
                    if (vector != null && vector.Length > 0 && (Dimensions == 0 || vector.Length == Dimensions)) { Add(key, vector); save = ++unsaved >= SaveEvery; }
                }
                if (save) SaveCache();
            }
        }

        /// <summary>Caller holds <see cref="gate"/>. The oldest texts go first when the cache is full.</summary>
        void Add(string key, float[] vector)
        {
            if (!cache.ContainsKey(key)) order.Enqueue(key);
            cache[key] = vector;
            while (order.Count > MaxCached) cache.Remove(order.Dequeue());
        }

        // ───────────── disk cache ─────────────

        void LoadCache(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                using (var reader = new BinaryReader(File.OpenRead(path)))
                {
                    if (reader.ReadString() != CacheVersion) return;
                    int dims = reader.ReadInt32(), count = reader.ReadInt32();
                    if (dims <= 0 || dims > 8192 || count < 0 || count > MaxCached) return;
                    lock (gate)
                        for (int i = 0; i < count; i++)
                        {
                            string key = reader.ReadString();
                            var v = new float[dims];
                            for (int j = 0; j < dims; j++) v[j] = reader.ReadSingle();
                            Add(key, v);
                        }
                }
            }
            catch (Exception error) { Debug.LogWarning("[灵光] 向量缓存读不了，重新计算：" + error.Message); lock (gate) { cache.Clear(); order.Clear(); } }
        }

        void SaveCache()
        {
            if (string.IsNullOrEmpty(cachePath)) return;
            List<KeyValuePair<string, float[]>> rows;
            lock (gate)
            {
                rows = new List<KeyValuePair<string, float[]>>(order.Count);
                foreach (var key in order) rows.Add(new KeyValuePair<string, float[]>(key, cache[key]));
                unsaved = 0;
            }
            if (rows.Count == 0) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                string temp = cachePath + ".tmp";
                using (var writer = new BinaryWriter(File.Create(temp)))
                {
                    int dims = rows[0].Value.Length;
                    writer.Write(CacheVersion); writer.Write(dims);
                    int count = 0;
                    foreach (var r in rows) if (r.Value.Length == dims) count++;
                    writer.Write(count);
                    foreach (var r in rows)
                    {
                        if (r.Value.Length != dims) continue;
                        writer.Write(r.Key);
                        foreach (float f in r.Value) writer.Write(f);
                    }
                }
                if (File.Exists(cachePath)) File.Replace(temp, cachePath, null); else File.Move(temp, cachePath);
            }
            catch (Exception error) { Debug.LogWarning("[灵光] 向量缓存写不进去：" + error.Message); }
        }

        // ───────────── shutdown ─────────────

        void Shutdown()
        {
            if (RagVectors.Source == (IVectorSource)this) RagVectors.Source = null;
            Volatile.Write(ref ready, false);
            stopping = true;
            lock (gate) Monitor.PulseAll(gate);
            bool stopped = worker == null || worker.Join(StopWaitMs);
            worker = null;
            if (starting != null)
            {
                // Still loading: free the service whenever it is ready.
                starting.ContinueWith(t => { if (t.Status == TaskStatus.RanToCompletion) Free(t.Result, null); });
                starting = null;
            }
            if (service != null)
            {
                if (stopped) { SaveCache(); Free(service, client); }
                else Debug.LogError("[灵光] Embedding worker did not stop; its service is left running.");
                service = null; client = null;
            }
            lock (gate) { urgent.Clear(); later.Clear(); queued.Clear(); }
            if (Instance == this) Instance = null;
        }

        static void Free(NativeService target, NativeClient c)
        {
            try
            {
                if (c != null && c.llm != IntPtr.Zero) { try { target.llamaLib?.LLM_Delete(c.llm); } catch (Exception) { } c.llm = IntPtr.Zero; }
                try { target.Stop(); } catch (Exception) { }
                target.Dispose();
            }
            catch (Exception error) { Debug.LogWarning("[灵光] Embedding service cleanup failed: " + error.Message); }
        }
    }
}
