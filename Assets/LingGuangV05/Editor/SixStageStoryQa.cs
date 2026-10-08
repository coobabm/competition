#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Core.Story;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// Three narrow story-lifecycle checks using real components in a temporary, memory-only host.
    /// Run synchronously in Play Mode; no frame is yielded, so unrelated scene objects cannot consume the
    /// temporary YY singleton. The singleton is restored before the temporary host is destroyed.
    /// </summary>
    public static class SixStageStoryQa
    {
        const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Serializable]
        public sealed class Report
        {
            public bool success, restored, storyActive, testModeDisabled;
            public bool abilityBeatStarted, eraRecovered;
            public bool reboundClearedQueue, reboundClearedOverlay, staleChatDiscarded, newSaveTreeStayedHidden;
            public int passed;
            public string error = "", restoreError = "";
            public string scope = "Play Mode, isolated real Runtime/Director/Bridge/Presenter/YY; real UpdateChat and coroutine startup, not screenshot or elapsed-frame validation.";
            public string evidencePath = "";
            public List<string> checks = new List<string>();
        }

        public static string Run()
        {
            var report = new Report();
            GameObject host = null;
            YYChatHub originalYy = YYChatHub.Instance;
            var originalModel = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            YYState originalYyState = originalYy != null ? originalYy.S : null;
            var originalRuntime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            var originalSimulation = originalRuntime != null ? originalRuntime.Sim : null;
            var originalController = originalRuntime != null ? originalRuntime.GetComponent<XingGuangController>() : null;
            var originalLab = originalController != null ? originalController.Sim : null;
            var originalDirector = originalRuntime != null ? originalRuntime.GetComponent<StoryDirector>() : null;
            var originalRunner = originalDirector != null ? originalDirector.Runner : null;
            bool originalDisk = originalRuntime != null && originalRuntime.useDiskSave;
            bool originalTick = originalRuntime != null && originalRuntime.tickEnabled;
            bool originalTestMode = originalRuntime != null && originalRuntime.TestMode;
            float originalTimeScale = Time.timeScale;
            StoryDirector director = null;
            Action<StoryDirector> presenterRebound = null;
            try
            {
                Require(Application.isPlaying, "Run requires Play Mode; it must not initialize a normal disk-backed Editor runtime.");
                Require(originalRuntime != null && originalSimulation != null, "An initialized scene runtime is required.");
                Require(!originalRuntime.useDiskSave, "Refusing QA while the scene runtime has useDiskSave=true. Prepare an isolated memory-only Play session first.");

                host = new GameObject("__SixStageStoryQa", typeof(RectTransform));
                host.hideFlags = HideFlags.HideAndDontSave;
                host.SetActive(false);
                // The object stays inactive until useDiskSave is false; Awake cannot load a player save.
                var runtime = host.AddComponent<ChapterOneRuntime>();
                runtime.useDiskSave = false; runtime.tickEnabled = false;
                director = host.AddComponent<StoryDirector>();
                director.runtime = runtime; director.logToConsole = false; director.allowHeadless = false;
                var controller = host.AddComponent<XingGuangController>();
                controller.runtime = runtime;
                var bridge = host.AddComponent<XgStoryBridge>(); bridge.Bind(controller);
                var presenter = host.AddComponent<StoryDesktopPresenter>(); presenter.director = director;
                presenter.enabled = false; // Prevent Start from binding normal desktop windows; invoke its real methods explicitly.
                var yy = host.AddComponent<YYChatHub>();
                host.SetActive(true);
                yy.Initialize(runtime, null);
                Require(!runtime.useDiskSave && !runtime.TestMode, "The temporary runtime must run the real story, not SetSimulationForTests.");
                report.testModeDisabled = true;

                // Start() normally wires this callback and builds the overlay. Do only these two pieces here:
                // no desktop router bindings, normal windows, save stores or model service are touched.
                presenterRebound = (Action<StoryDirector>)Delegate.CreateDelegate(typeof(Action<StoryDirector>), presenter, Method(presenter, "OnStoryRebound"));
                director.Rebound += presenterRebound;
                BuildTemporaryCutsceneSurface(host.transform, presenter);
                director.ExternalBusy = () => presenter.CutscenePlaying;
                director.SetOutput(presenter);

                BindMemorySave(runtime, controller, director, bridge, new ChapterOneSim());
                report.storyActive = director.Active;
                Check(report, director.Active, "real StoryDirector remains Active with TestMode=false");

                // R1: an ability that emerges from the two bars raises its own beat (lg.emerge:2) through the real bridge.
                var lab = controller.Sim;
                FillBarsForAbilityTwo(lab);
                lab.Tick(1, new MemoryHost());
                Require(lab.HasAbility(2) && lab.S.stage == 2, "Full bars must emerge the second ability and move the stage.");
                director.Runner.Tick(0);
                var current = director.Runner.Current;
                report.abilityBeatStarted = runtime.Sim.S.story.HasFired("emerge_2") || current != null && (current.Id == "emerge_2" || current.Id == "specialty_motivation" || current.Id == "bt_hidden" || current.Id == "month_2");
                Check(report, report.abilityBeatStarted, "an emerged ability reaches the story as its own beat");

                // R2: this is a serialized save whose era card was never marked fired. Rebind real
                // components, let recovery emit the signal, and let the actual presenter start the Title card.
                var purchased = new XgSim();
                Require(purchased.BuyNode("mlp", new MemoryHost()), "The fixture buys the real MLP item, not just a fabricated event.");
                FillBarsForAbilityTwo(purchased);
                purchased.Tick(1, new MemoryHost());
                Require(purchased.S.stage == 2, "The fixture reaches stage two through the bars.");
                var recoveredSave = new ChapterOneSim();
                recoveredSave.S.labState = JsonUtility.ToJson(purchased.S);
                BindMemorySave(runtime, controller, director, bridge, recoveredSave);
                Require(!runtime.Sim.S.story.HasFired("bt_hidden"), "The recovered era must be unfinished.");
                director.Runner.Tick(0);
                var cutsceneRoot = (RectTransform)Get(presenter, "cutsceneRoot");
                bool hasEraText = false;
                foreach (var label in cutsceneRoot.GetComponentsInChildren<TMP_Text>(true))
                    if ((label.text ?? "").Contains("1986")) hasEraText = true;
                report.eraRecovered = controller.Sim.Has("mlp") && director.Runner.Current != null &&
                    director.Runner.Current.Id == "bt_hidden" && director.Runner.IsWaitingForOutput && presenter.CutscenePlaying && hasEraText;
                Check(report, report.eraRecovered, "a reached, unfinished era rebind restores real era Title coroutine and 1986 text");

                // R3: capture an actual old pending entry, rebind to a fresh memory save (which must clear
                // pending output), then reinsert that stale entry to exercise the dequeue identity guard too.
                Invoke(presenter, "Say", "laozhou", "QA-only old-save tree message", 0f, "lz_tree");
                object queue = Get(presenter, "chatQueue");
                object oldPending = queue.GetType().GetMethod("Peek").Invoke(queue, null);
                var freshSave = new ChapterOneSim();
                BindMemorySave(runtime, controller, director, bridge, freshSave);
                report.reboundClearedQueue = QueueCount(presenter) == 0;
                report.reboundClearedOverlay = !presenter.CutscenePlaying && !((CanvasGroup)Get(presenter, "cutsceneGroup")).blocksRaycasts;
                Check(report, report.reboundClearedQueue && report.reboundClearedOverlay, "real Rebound clears pending speech and active era overlay");
                int freshMessages = yy.Conversation(YYChatHub.LaoZhou).messages.Count;
                queue.GetType().GetMethod("Enqueue").Invoke(queue, new[] { oldPending });
                Set(presenter, "chatDueAt", Time.unscaledTime - 1f);
                Invoke(presenter, "UpdateChat");
                report.staleChatDiscarded = QueueCount(presenter) == 0 && yy.Conversation(YYChatHub.LaoZhou).messages.Count == freshMessages;
                report.newSaveTreeStayedHidden = !controller.Sim.S.treeIntroDelivered && !controller.FeatureVisible("tree") && runtime.Sim.S.story.GetFlag("delivered.lz_tree") == 0;
                Check(report, report.staleChatDiscarded && report.newSaveTreeStayedHidden, "stale old-save pending entry is discarded without delivery flag or new-save tree unlock");
                Check(report, !runtime.useDiskSave && !runtime.TestMode && director.Active, "all lifecycle checks ran with disk persistence off and the real story on");
                report.success = true;
            }
            catch (Exception error)
            {
                report.error = Unwrap(error).ToString(); report.success = false;
            }
            finally
            {
                try
                {
                    // Restore the only shared reference before teardown: YY.OnDestroy then cannot clear it.
                    typeof(YYChatHub).GetProperty("Instance", StaticFlags).GetSetMethod(true).Invoke(null, new object[] { originalYy });
                    if (!ReferenceEquals(LingGuangV05.Desktop.LLM.LocalLlm.Instance, originalModel))
                        typeof(LingGuangV05.Desktop.LLM.LocalLlm).GetProperty("Instance", StaticFlags).GetSetMethod(true).Invoke(null, new object[] { originalModel });
                    if (director != null && presenterRebound != null) director.Rebound -= presenterRebound;
                    if (host != null) UnityEngine.Object.DestroyImmediate(host);
                    Time.timeScale = originalTimeScale;
                    report.restored = ReferenceEquals(YYChatHub.Instance, originalYy) && ReferenceEquals(LingGuangV05.Desktop.LLM.LocalLlm.Instance, originalModel) &&
                        (originalYy == null || ReferenceEquals(originalYy.S, originalYyState)) &&
                        (originalRuntime == null || ReferenceEquals(originalRuntime.Sim, originalSimulation) && originalRuntime.useDiskSave == originalDisk && originalRuntime.tickEnabled == originalTick && originalRuntime.TestMode == originalTestMode) &&
                        (originalController == null || ReferenceEquals(originalController.Sim, originalLab)) &&
                        (originalDirector == null || ReferenceEquals(originalDirector.Runner, originalRunner)) && Time.timeScale == originalTimeScale;
                    if (!report.restored) { report.success = false; report.restoreError = "An original scene reference or control flag changed."; }
                }
                catch (Exception error) { report.success = false; report.restoreError = Unwrap(error).ToString(); }
            }
            report.evidencePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Evidence", "SixStage", "unity", "story-lifecycle.json"));
            string result = JsonUtility.ToJson(report, true);
            try { Directory.CreateDirectory(Path.GetDirectoryName(report.evidencePath)); File.WriteAllText(report.evidencePath, result); }
            catch (Exception error) { report.success = false; report.error += "\nCould not write QA evidence: " + error.Message; result = JsonUtility.ToJson(report, true); }
            return result;
        }

        static void BindMemorySave(ChapterOneRuntime runtime, XingGuangController controller, StoryDirector director, XgStoryBridge bridge, ChapterOneSim state)
        {
            Require(!runtime.useDiskSave, "Memory-save binding must not access disk persistence.");
            Invoke(runtime, "Attach", state);
            Invoke(controller, "BindSave", state);
            Invoke(director, "Bind", state);
            Invoke(bridge, "Update");
        }

        static void BuildTemporaryCutsceneSurface(Transform parent, StoryDesktopPresenter presenter)
        {
            var canvasObject = new GameObject("StoryQaOverlay", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            var cutscene = new GameObject("Cutscene", typeof(RectTransform), typeof(CanvasGroup), typeof(UnityEngine.UI.Image));
            cutscene.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)cutscene.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var group = cutscene.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
            Set(presenter, "canvas", canvas); Set(presenter, "root", (RectTransform)canvasObject.transform);
            Set(presenter, "cutsceneRoot", rect); Set(presenter, "cutsceneGroup", group); Set(presenter, "cutsceneBackdrop", cutscene.GetComponent<UnityEngine.UI.Image>());
            // Reuse an already loaded font; never import TMP assets or allocate a persistent font asset.
            Set(presenter, "font", TMP_Settings.defaultFontAsset);
        }

        /// <summary>Gives a lab exactly the trained parameters and samples the second ability asks for.</summary>
        static void FillBarsForAbilityTwo(XgSim lab)
        {
            lab.S.trainedParamsK = lab.ParamsThreshold(2);
            lab.S.labels.Add(new XgLabelCount { dataset = "logic", count = lab.SamplesThreshold(2) + 1 });
        }

        sealed class MemoryHost : IXgHost
        {
            double money = 100000;
            public double Money => money;
            public double Compute => 1;
            public double VramMB => 16000;
            public string Blocker => null;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
        }
        static void Check(Report report, bool condition, string message) { Require(condition, message); report.checks.Add(message); report.passed++; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static Exception Unwrap(Exception error) => error is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : error;
        static MethodInfo Method(object target, string name) => target.GetType().GetMethod(name, InstanceFlags) ?? throw new MissingMethodException(target.GetType().FullName, name);
        static object Invoke(object target, string method, params object[] args) => Method(target, method).Invoke(target, args);
        static object Get(object target, string field) => (target.GetType().GetField(field, InstanceFlags) ?? throw new MissingFieldException(target.GetType().FullName, field)).GetValue(target);
        static void Set(object target, string field, object value) => (target.GetType().GetField(field, InstanceFlags) ?? throw new MissingFieldException(target.GetType().FullName, field)).SetValue(target, value);
        static int QueueCount(StoryDesktopPresenter presenter) { object queue = Get(presenter, "chatQueue"); return (int)queue.GetType().GetProperty("Count").GetValue(queue); }
    }
}
#endif
