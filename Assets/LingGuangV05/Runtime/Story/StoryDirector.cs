using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Core.Story;
using UnityEngine;

namespace LingGuangV05.Runtime.Story
{
    /// <summary>
    /// Unity host for the story runner. Lives next to <see cref="ChapterOneRuntime"/> (same lifetime as the save),
    /// loads every TextAsset under Resources/LingGuangV05/Story, forwards signals, ticks the runner and polls state.
    /// Knows nothing about the desktop: presentation is an <see cref="IStoryOutput"/> registered by the bridge.
    /// </summary>
    [DefaultExecutionOrder(-1400)]
    [DisallowMultipleComponent]
    public sealed class StoryDirector : MonoBehaviour
    {
        public const string ResourceFolder = "LingGuangV05/Story";
        public ChapterOneRuntime runtime;
        [Tooltip("Seconds between state polls. Rule-style beats (\"on\": \"state\") are evaluated at this rate.")]
        public float statePollSeconds = .25f;
        [Tooltip("Story pacing: minimum story seconds between narration beats.")]
        public float minNarrationGap = 40;
        public bool logToConsole = true;
        [Tooltip("Run without a registered presentation (tests). Otherwise the story waits for SetOutput.")]
        public bool allowHeadless;

        private StoryLibrary library;
        private StoryRunner runner;
        private StoryContext context;
        private ChapterOneSim boundSim;
        private IStoryOutput output;
        private bool stateDirty, booted;
        private float sinceStatePoll;
        private readonly List<string> recentLog = new List<string>();

        public StoryRunner Runner { get { return runner; } }
        public StoryContext Context { get { return context; } }
        public StoryLibrary Library { get { return library; } }
        public IReadOnlyList<string> RecentLog { get { return recentLog; } }
        /// <summary>Extra busy gate from presentation (cutscene playing, player dragging...).</summary>
        public Func<bool> ExternalBusy { get; set; }
        public Func<string, double?> LabFacts { get; set; }
        public Func<string, string> LabText { get; set; }
        public float TimeScale
        {
            get { return runner == null ? 1 : (float)runner.TimeScale; }
            set { if (runner != null) runner.TimeScale = Mathf.Max(.05f, value); }
        }
        /// <summary>False in runtime test mode (SetSimulationForTests): QA scripts drive the desktop without story overlays.</summary>
        public bool Active { get { return runtime != null && !runtime.TestMode && runner != null; } }

        public event Action<StoryDirector> Rebound;

        public void SetOutput(IStoryOutput storyOutput)
        {
            output = storyOutput;
            if (runner != null) runner.Output = output;
        }

        public void Emit(string signal, string arg = null)
        {
            if (Active) runner.Signal(signal, arg);
        }

        private void Awake()
        {
            if (runtime == null) runtime = GetComponent<ChapterOneRuntime>();
            library = LoadLibrary(Log);
        }

        public static StoryLibrary LoadLibrary(Action<string> log)
        {
            var lib = new StoryLibrary();
            TextAsset[] assets = Resources.LoadAll<TextAsset>(ResourceFolder);
            Array.Sort(assets, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (var asset in assets)
            {
                try { lib.Add(asset.text, asset.name); }
                catch (StoryFormatException e) { Debug.LogError("[Story] " + e.Message); }
            }
            if (log != null) log("loaded " + lib.Beats.Count + " beats from " + assets.Length + " files");
            return lib;
        }

        private void OnEnable()
        {
            if (runtime != null) { runtime.Signal += OnRuntimeSignal; runtime.Changed += OnRuntimeChanged; }
        }

        private void OnDisable()
        {
            if (runtime != null) { runtime.Signal -= OnRuntimeSignal; runtime.Changed -= OnRuntimeChanged; }
        }

        private void OnRuntimeSignal(string name, string arg) { if (Active) runner.Signal(name, arg); }
        private void OnRuntimeChanged() { stateDirty = true; }

        private void Update()
        {
            if (runtime == null) return;
            runtime.EnsureInitialized();
            if (runtime.Sim != boundSim) Bind(runtime.Sim);
            if (!Active) return;
            if (output == null && !allowHeadless) return; // never consume cutscenes before the desktop can show them
            sinceStatePoll += Time.unscaledDeltaTime;
            if (stateDirty && sinceStatePoll >= statePollSeconds)
            {
                stateDirty = false;
                sinceStatePoll = 0;
                runner.Signal("state");
            }
            if (!booted) { booted = true; runner.Signal("boot"); }
            runner.Tick(Mathf.Min(Time.unscaledDeltaTime, .5f));
        }

        private void Bind(ChapterOneSim sim)
        {
            boundSim = sim;
            if (runner != null) runner.Clear();
            if (sim == null) { runner = null; return; }
            // A script reload during Play keeps this component but not its loaded library.
            if (library == null) library = LoadLibrary(Log);
            float scale = runner != null ? (float)runner.TimeScale : 1;
            var vars = new ChapterOneStoryVars(() => runtime.Sim, name => LabFacts == null ? null : LabFacts(name));
            context = new StoryContext(vars, sim.S.story);
            runner = new StoryRunner(library, context, output)
            {
                Writer = vars,
                TimeScale = scale,
                MinNarrationGap = minNarrationGap,
                IsBusy = IsBusy,
                Resolve = ResolveToken,
            };
            runner.Log += Log;
            runner.Dirty += () => runtime.MarkDirty();
            booted = false;  // a reloaded or reset save boots again ("boot" beats re-check their conditions)
            stateDirty = true;
            Log("bound to save (fired " + sim.S.story.fired.Count + ")");
            var rebound = Rebound; if (rebound != null) rebound(this);
        }

        private bool IsBusy()
        {
            if (boundSim != null && boundSim.S.examActive) return true;
            var extra = ExternalBusy;
            return extra != null && extra();
        }

        /// <summary>Placeholders available to every line.</summary>
        public string ResolveToken(string token)
        {
            var S = boundSim != null ? boundSim.S : null;
            switch (token)
            {
                case "name": return S == null || string.IsNullOrEmpty(S.aiName) || S.aiName == "the code" ? "它" : S.aiName;
                case "callMe": return "你";
                case "tic": return "";
                case "day": return S == null ? "1" : S.day.ToString(CultureInfo.InvariantCulture);
                case "time": return GameCalendar.Now(S).ToString("HH:mm", CultureInfo.InvariantCulture);
                case "date":
                    var date = LingGuangV05.Core.GameCalendar.Now(S);
                    return date.Month + " 月 " + date.Day + " 日";
                default: return LabText == null ? null : LabText(token);
            }
        }

        /// <summary>Debug: explain every beat that has not fired.</summary>
        public string ExplainAll()
        {
            if (runner == null) return "story not bound";
            var sb = new System.Text.StringBuilder();
            sb.Append("current: ").Append(runner.Current != null ? runner.Current.Id : "-").Append(", queued: ").Append(runner.QueueCount).Append('\n');
            foreach (var beat in library.Beats)
                if (!(beat.Once && context.State.HasFired(beat.Id))) sb.Append(runner.Engine.Explain(beat.Id, context)).Append('\n');
            foreach (var e in runner.Engine.Errors) sb.Append("ERROR ").Append(e).Append('\n');
            return sb.ToString();
        }

        private void Log(string message)
        {
            if (recentLog.Count >= 40) recentLog.RemoveAt(0);
            recentLog.Add(message);
            if (logToConsole) Debug.Log("[Story] " + message);
        }
    }
}
