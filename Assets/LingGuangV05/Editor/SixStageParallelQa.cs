#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace LingGuangV05.Editor
{
    /// <summary>
    /// Real, asynchronous Unity transport QA. Requires an already isolated memory-only normal Play runtime.
    /// Never points the game singleton at the test instance, never manages an externally discovered process.
    /// </summary>
    public static class SixStageParallelQa
    {
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class RequestRecord
        {
            public string id, phase, requestBody, rawResponse = "", failure = "", reply = "";
            public int slot = -1, callbacks;
            public bool accepted, dispatched, completed, expectCancellation, hasProbability, serverSlotObserved;
            public double probability, enqueuedAt, dispatchedAt, elapsedSeconds;
        }
        [Serializable] public sealed class ProcessRecord
        {
            public int pid, slots, totalContext;
            public double peakRssMiB;
            public string logPath = "";
            public bool exited;
        }
        [Serializable] public sealed class Report
        {
            public string scope = "Real UnityWebRequest; independent owned loopback process; synthetic fixture prompts; no player-save access";
            public string runId, startedUtc, finishedUtc, stage, error = "", outputPath, np1Props = "", np4Props = "", np4Slots = "";
            public string reloadCleanupScope = "Direct beforeAssemblyReload handler test; no actual domain reload is requested by this QA";
            public int port, externalPid, passed, failed, maxJudges, maxChats, maxTotalInference, maxServerProcessing;
            public bool completed, success, singletonPreserved, runtimePreserved, externalPidChecked, externalPidPreserved, threeJudgesAndChatObserved;
            public bool allOwnedProcessesExited, callbacksExactlyOnce;
            public double peakSingleRssMiB, peakParallelRssMiB;
            public List<Check> checks = new List<Check>();
            public List<RequestRecord> requests = new List<RequestRecord>();
            public List<ProcessRecord> processes = new List<ProcessRecord>();
        }

        [Serializable] sealed class Fixtures { public Fixture[] items; }
        [Serializable] sealed class Fixture { public string id, prompt; }
        // LlamaLib's built-in server answers POST /props with the per-slot context only; it has no /slots, so slot
        // activity is read from LocalLlm (native calls in flight per slot) instead.
        [Serializable] sealed class Props { public Generation default_generation_settings; }
        [Serializable] sealed class Generation { public int n_ctx; }

        public static bool Running { get; private set; }
        public static Report Result { get; private set; }
        enum Phase { BootOne, PropsOne, Single, BootFour, PropsFour, SlotsFour, Parallel, FourthSlot, Cancel, Reenable, ResumedJudge, ReloadStopped }
        static Phase phase;
        static LocalLlm service, originalInstance;
        static GameObject ownedObject;
        static ChapterOneRuntime runtime;
        static object originalSimulation;
        static DateTime externalStart;
        static string path;
        static Fixture[] fixtures;
        static readonly List<RequestRecord> batch = new List<RequestRecord>();
        static RequestRecord chat;
        static UnityWebRequest propsRequest;
        static UnityWebRequestAsyncOperation propsOperation;
        static double deadline, nextMemorySample;
        static bool ending;
        static int firstPid, disabledPid;

        /// <summary>Starts work and returns immediately. Poll Running/Result; default port may fall back to a free one.</summary>
        public static string Begin(int preferredPort = 18090, int externalPid = 0)
        {
            if (Running) throw new InvalidOperationException("Parallel QA is already running.");
            runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            if (!Application.isPlaying || runtime == null || runtime.useDiskSave || runtime.TestMode)
                throw new InvalidOperationException("Requires normal Play with useDiskSave=false; this QA never changes save settings.");
            if (preferredPort < 1024 || preferredPort > 65535 || preferredPort == LocalLlm.Port)
                throw new ArgumentOutOfRangeException(nameof(preferredPort));
            originalInstance = LocalLlm.Instance;
            originalSimulation = runtime.Sim;
            externalStart = DateTime.MinValue;
            if (externalPid > 0) using (var process = Process.GetProcessById(externalPid)) externalStart = process.StartTime;
            fixtures = ReadFixtures();
            string runId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
            path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Evidence/SixStage/unity/parallel.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path)) File.Copy(path, Path.Combine(Path.GetDirectoryName(path), "parallel.previous-" + runId + ".json"), false);
            Result = new Report { runId = runId, startedUtc = DateTime.UtcNow.ToString("O"), port = AvailablePort(preferredPort), externalPid = externalPid, outputPath = path };
            batch.Clear(); chat = null; nextMemorySample = 0;
            ending = false; Running = true;
            EditorApplication.update += Advance;
            EditorApplication.playModeStateChanged += PlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Reloading;
            try
            {
                ownedObject = new GameObject("SixStageParallelQa-" + runId) { hideFlags = HideFlags.DontSave };
                ownedObject.SetActive(false);
                service = ownedObject.AddComponent<LocalLlm>();
                CheckNow("default game port remains 18088", service.EndpointPort == LocalLlm.Port && LocalLlm.Port == 18088);
                ownedObject.SetActive(true);
                service.StartEditorQa(Result.port, Path.Combine(Path.GetDirectoryName(path), "parallel-server-" + runId));
                CheckNow("isolated test instance is not the game singleton", ReferenceEquals(LocalLlm.Instance, originalInstance));
                SetPhase(Phase.BootOne, 130);
                return "Parallel QA started on 127.0.0.1:" + Result.port + "; poll Running and Result.";
            }
            catch (Exception error) { Finish(false, error.ToString()); throw; }
        }

        static void Advance()
        {
            if (!Running || ending) return;
            try
            {
                Require(Application.isPlaying && runtime != null && !runtime.useDiskSave && !runtime.TestMode, "Isolated Play preconditions changed.");
                Require(ReferenceEquals(LocalLlm.Instance, originalInstance), "Game singleton changed during isolated inference QA.");
                Require(ReferenceEquals(runtime.Sim, originalSimulation), "The game simulation owner changed during QA.");
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Parallel QA timed out at " + phase);
                Observe();
                if (service.State == LocalLlm.Status.Failed) throw new InvalidOperationException(service.Detail);
                switch (phase)
                {
                    case Phase.BootOne:
                        if (!service.Ready) break;
                        Require(service.OwnsServer && service.ParallelSlots == 1 && service.EndpointPort != LocalLlm.Port, "QA did not own its one-slot service.");
                        firstPid = service.OwnedServiceId; RememberProcess();
                        BeginProps(); SetPhase(Phase.PropsOne, 8); break;
                    case Phase.PropsOne:
                        if (!ReadProps(1)) break;
                        batch.Clear(); batch.Add(QueueJudge("single", 0, 0)); SetPhase(Phase.Single, 20); break;
                    case Phase.Single:
                        if (!BatchDone() || service.Busy) break;
                        CheckValidBatch(); service.SetParallelJudging(true); SetPhase(Phase.BootFour, 130); break;
                    case Phase.BootFour:
                        if (!service.Ready || service.ParallelSlots != 4) break;
                        Require(service.OwnsServer && service.OwnedServiceId != firstPid, "1-to-4 reconfiguration did not create a new owned service.");
                        CheckNow("one-slot service was freed during owned resize", Exited(firstPid) && service.LastStopConfirmed);
                        RememberProcess(); BeginProps(); SetPhase(Phase.PropsFour, 8); break;
                    case Phase.PropsFour:
                        if (!ReadProps(4)) break;
                        SetPhase(Phase.SlotsFour, 8); break;
                    case Phase.SlotsFour:
                        batch.Clear();
                        for (int slot = 0; slot < 3; slot++) batch.Add(QueueJudge("parallel", slot, slot + 1));
                        chat = QueueChat("parallel-chat", false);
                        Observe(); PollSlots(); SetPhase(Phase.Parallel, 65); break;
                    case Phase.Parallel:
                        PollSlots();
                        if (!BatchDone() || chat.callbacks == 0 || service.Busy) break;
                        CheckValidBatch();
                        CheckNow("three judges plus reserved chat slot were simultaneously in flight", Result.threeJudgesAndChatObserved && Result.maxJudges == 3 && Result.maxTotalInference >= 4);
                        CheckNow("service ran four native slots at once", Result.maxServerProcessing >= 4, "maxServerProcessing=" + Result.maxServerProcessing);
                        CheckNow("reserved-slot chat completed once with real text", chat.callbacks == 1 && !string.IsNullOrWhiteSpace(chat.reply));
                        batch.Clear(); batch.Add(QueueJudge("fourth-slot", 3, 4)); SetPhase(Phase.FourthSlot, 20); break;
                    case Phase.FourthSlot:
                        PollSlots();
                        if (!BatchDone() || service.Busy) break;
                        CheckValidBatch();
                        object cancellationOwner = new object(); batch.Clear();
                        for (int slot = 0; slot < 4; slot++) batch.Add(QueueJudge("cancel", slot, slot, true, cancellationOwner));
                        service.CancelJudges(cancellationOwner); SetPhase(Phase.Cancel, 20); break;
                    case Phase.Cancel:
                        if (!BatchDone() || service.Busy) break;
                        foreach (var row in batch) CheckNow(row.id + " callback cancelled exactly once", row.callbacks == 1 && row.failure == "cancelled");
                        disabledPid = service.OwnedServiceId;
                        batch.Clear(); batch.Add(QueueJudge("disable", 0, 1, true));
                        chat = QueueChat("disable-chat", true);
                        service.enabled = false;
                        CheckNow("OnDisable stopped only the owned service", !service.OwnsServer && service.LastStopConfirmed && Exited(disabledPid));
                        CheckNow("OnDisable completed active judge once", batch[0].callbacks == 1 && batch[0].failure == "cancelled");
                        CheckNow("OnDisable completed chat once", chat.callbacks == 1 && chat.reply == null);
                        service.enabled = true; SetPhase(Phase.Reenable, 130); break;
                    case Phase.Reenable:
                        if (!service.Ready) break;
                        CheckNow("re-enable starts a fresh owned service with preserved four-slot configuration", service.OwnsServer && service.OwnedServiceId != disabledPid && service.ParallelSlots == 4);
                        RememberProcess(); batch.Clear(); batch.Add(QueueJudge("resumed", 3, 2)); SetPhase(Phase.ResumedJudge, 20); break;
                    case Phase.ResumedJudge:
                        PollSlots();
                        if (!BatchDone() || service.Busy) break;
                        CheckValidBatch();
                        int reloadPid = service.OwnedServiceId;
                        typeof(LocalLlm).GetMethod("BeforeAssemblyReload", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, null);
                        CheckNow("pre-reload handler stops owned service before reference loss", !service.OwnsServer && service.LastStopConfirmed && Exited(reloadPid));
                        service.enabled = false; service.enabled = true;
                        SetPhase(Phase.ReloadStopped, 3); break;
                    case Phase.ReloadStopped:
                        if (deadline - EditorApplication.timeSinceStartup > 2.5) break;
                        CheckNow("isolated QA service does not silently resurrect after reload cleanup", service.State == LocalLlm.Status.Off && !service.OwnsServer);
                        Finish(true); break;
                }
            }
            catch (Exception error) { Finish(false, error.ToString()); }
        }

        static RequestRecord QueueJudge(string tag, int slot, int fixtureIndex, bool cancel = false, object owner = null)
        {
            var fixture = fixtures[fixtureIndex % fixtures.Length];
            string body = XgJudgeProtocol.RequestBody(fixture.prompt);
            var row = new RequestRecord { id = tag + "-" + Result.requests.Count, phase = tag, slot = slot, expectCancellation = cancel,
                enqueuedAt = EditorApplication.timeSinceStartup, requestBody = body.Substring(0, body.Length - 1) + ",\"id_slot\":" + slot + "}" };
            Result.requests.Add(row);
            row.accepted = service.JudgeForEditorQa(fixture.prompt, slot, (p, failure, raw) =>
            {
                row.callbacks++; row.completed = true; row.hasProbability = p.HasValue; row.probability = p ?? 0;
                row.failure = failure ?? ""; row.rawResponse = raw ?? "";
                row.elapsedSeconds = EditorApplication.timeSinceStartup - (row.dispatched ? row.dispatchedAt : row.enqueuedAt);
                Save();
            }, () => { row.dispatched = true; row.dispatchedAt = EditorApplication.timeSinceStartup; return true; }, owner ?? row);
            Require(row.accepted, "Judge request was rejected: " + row.id);
            Observe(); return row;
        }

        static RequestRecord QueueChat(string tag, bool cancel)
        {
            var row = new RequestRecord { id = tag, phase = tag, expectCancellation = cancel, accepted = true, enqueuedAt = EditorApplication.timeSinceStartup };
            Result.requests.Add(row);
            service.Chat(new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("system", "这是本地队列测试。请用中文写一段约100字、关于雨后校园的连贯短文，不用列表。"),
                new KeyValuePair<string, string>("user", "从窗外的雨滴开始，一直写到傍晚亮起的路灯。") }, 180, 0,
                reply => { row.callbacks++; row.completed = true; row.reply = reply; row.elapsedSeconds = EditorApplication.timeSinceStartup - row.enqueuedAt; Save(); });
            return row;
        }

        static bool BatchDone() { foreach (var row in batch) if (row.callbacks == 0) return false; return true; }
        static void CheckValidBatch()
        {
            foreach (var row in batch)
            {
                bool rawValid = XgJudgeProtocol.TryParseProbability(row.rawResponse, out double p, out string failure);
                CheckNow(row.id + " has one real dual-class logprob result", row.callbacks == 1 && row.hasProbability && row.failure.Length == 0 && rawValid && Math.Abs(p - row.probability) < 1e-12, failure ?? "");
            }
        }

        static void BeginProps()
        {
            propsRequest = new UnityWebRequest("http://127.0.0.1:" + Result.port + "/props", "POST")
            { uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")), downloadHandler = new DownloadHandlerBuffer(), timeout = 5, redirectLimit = 0 };
            propsRequest.SetRequestHeader("Content-Type", "application/json");
            propsOperation = propsRequest.SendWebRequest();
        }
        static bool ReadProps(int slots)
        {
            if (propsOperation == null || !propsOperation.isDone) return false;
            Require(propsRequest.result == UnityWebRequest.Result.Success, "Owned /props failed: " + propsRequest.error);
            string raw = propsRequest.downloadHandler.text;
            if (slots == 1) Result.np1Props = raw; else { Result.np4Props = raw; Result.np4Slots = "service slots=" + service.ParallelSlots; }
            var props = JsonUtility.FromJson<Props>(raw);
            CheckNow("np" + slots + " service has the slot count and per-slot context", service.ParallelSlots == slots && props != null && props.default_generation_settings != null && props.default_generation_settings.n_ctx >= LingGuangV05.Core.Chat.LlmScheduling.SlotContext);
            propsRequest.Dispose(); propsRequest = null; propsOperation = null; return true;
        }
        /// <summary>Samples how many slots run native completions now, and which dispatched rows are among them.</summary>
        static void PollSlots()
        {
            int processing = service.NativeCallsInFlight;
            foreach (var row in batch) if (row.dispatched && !row.completed && service.NativeSlotRunning(row.slot)) row.serverSlotObserved = true;
            Result.maxServerProcessing = Math.Max(Result.maxServerProcessing, processing);
        }

        static void Observe()
        {
            if (service == null) return;
            Result.maxJudges = Math.Max(Result.maxJudges, service.ActiveJudges);
            Result.maxChats = Math.Max(Result.maxChats, service.ActiveChats);
            Result.maxTotalInference = Math.Max(Result.maxTotalInference, service.ActiveJudges + service.ActiveChats);
            if (service.ActiveJudges == 3 && service.ActiveChats > 0) Result.threeJudgesAndChatObserved = true;
            if (EditorApplication.timeSinceStartup < nextMemorySample) return;
            nextMemorySample = EditorApplication.timeSinceStartup + .1;
            int pid = service.OwnedServiceId;
            if (pid <= 0) return;
            var row = RememberProcess();
            // The model runs inside this (editor) process now: its resident memory is the closest measure.
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    process.Refresh(); double rss = process.WorkingSet64 / (1024.0 * 1024);
                    row.peakRssMiB = Math.Max(row.peakRssMiB, rss);
                    if (row.slots == 1) Result.peakSingleRssMiB = Math.Max(Result.peakSingleRssMiB, rss);
                    else Result.peakParallelRssMiB = Math.Max(Result.peakParallelRssMiB, rss);
                }
            }
            catch (InvalidOperationException) { }
        }
        static ProcessRecord RememberProcess()
        {
            int pid = service.OwnedServiceId;
            var row = Result.processes.Find(value => value.pid == pid);
            if (row != null) return row;
            row = new ProcessRecord { pid = pid, slots = service.ParallelSlots, totalContext = service.ParallelSlots * LingGuangV05.Core.Chat.LlmScheduling.SlotContext, logPath = service.LastOwnedLogPath };
            Result.processes.Add(row); return row;
        }

        static void SetPhase(Phase next, double seconds)
        {
            phase = next; Result.stage = next.ToString(); deadline = EditorApplication.timeSinceStartup + seconds; Save();
        }
        static void CheckNow(string name, bool passed, string detail = "") { Result.checks.Add(new Check { name = name, passed = passed, detail = detail }); if (passed) Result.passed++; else Result.failed++; }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static void Save() { if (Result != null && !string.IsNullOrEmpty(path)) File.WriteAllText(path, JsonUtility.ToJson(Result, true)); }
        static void PlayChanged(PlayModeStateChange state) { if (Running && state == PlayModeStateChange.ExitingPlayMode) Finish(false, "Interrupted by Play exit."); }
        static void Reloading() { if (Running) Finish(false, "Interrupted by actual assembly reload; owned service cleaned before reload."); }

        static void Finish(bool completed, string error = "")
        {
            if (ending) return;
            ending = true;
            EditorApplication.update -= Advance; EditorApplication.playModeStateChanged -= PlayChanged; AssemblyReloadEvents.beforeAssemblyReload -= Reloading;
            try
            {
                propsRequest?.Abort(); propsRequest?.Dispose(); propsRequest = null;
                if (service != null) service.StopEditorQa();
                Result.allOwnedProcessesExited = true;
                foreach (var process in Result.processes) { process.exited = Exited(process.pid); Result.allOwnedProcessesExited &= process.exited; }
                Result.callbacksExactlyOnce = true;
                foreach (var request in Result.requests) if (request.accepted && request.callbacks != 1) Result.callbacksExactlyOnce = false;
                Result.singletonPreserved = ReferenceEquals(LocalLlm.Instance, originalInstance);
                Result.runtimePreserved = runtime != null && !runtime.useDiskSave && !runtime.TestMode && ReferenceEquals(runtime.Sim, originalSimulation);
                Result.externalPidChecked = Result.externalPid > 0;
                Result.externalPidPreserved = Result.externalPidChecked && SameExternalProcess(Result.externalPid, externalStart);
                CheckNow("all accepted requests completed/cancelled exactly once", Result.callbacksExactlyOnce);
                CheckNow("all QA-owned services were freed", Result.allOwnedProcessesExited);
                CheckNow("game singleton and memory-only runtime preserved", Result.singletonPreserved && Result.runtimePreserved);
                if (Result.externalPid > 0) CheckNow("pre-existing external PID remained alive and unchanged", Result.externalPidPreserved);
                CheckNow("resident memory was measured in both configurations", Result.peakSingleRssMiB > 0 && Result.peakParallelRssMiB > 0);
            }
            catch (Exception cleanupError) { error += "\nCleanup: " + cleanupError; }
            finally
            {
                if (ownedObject != null) UnityEngine.Object.DestroyImmediate(ownedObject);
                ownedObject = null; service = null; Running = false;
                Result.completed = completed; Result.error = error; Result.success = completed && error.Length == 0 && Result.failed == 0;
                Result.finishedUtc = DateTime.UtcNow.ToString("O"); Save();
                Debug.Log("[SixStageParallelQa] completed=" + Result.completed + " passed=" + Result.passed + " failed=" + Result.failed + " success=" + Result.success);
            }
        }

        /// <summary>The QA service with this id (recorded as "pid" in the report) has been freed.</summary>
        static bool Exited(int pid) => LocalLlm.ServiceStopped(pid);
        static bool SameExternalProcess(int pid, DateTime start)
        {
            try { using (var process = Process.GetProcessById(pid)) return !process.HasExited && process.StartTime == start; }
            catch (ArgumentException) { return false; }
        }
        static int AvailablePort(int preferred)
        {
            TcpListener listener = null;
            try { listener = new TcpListener(IPAddress.Loopback, preferred); listener.Start(); return ((IPEndPoint)listener.LocalEndpoint).Port; }
            catch (SocketException)
            {
                listener?.Stop(); listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally { listener?.Stop(); }
        }
        static Fixture[] ReadFixtures()
        {
            string fixturePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Evidence/SixStage/judge-smoke/fixtures.json"));
            var parsed = JsonUtility.FromJson<Fixtures>("{\"items\":" + File.ReadAllText(fixturePath) + "}");
            Require(parsed?.items != null && parsed.items.Length >= 5, "Synthetic judge fixtures are missing.");
            return parsed.items;
        }
    }
}
#endif
