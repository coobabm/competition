using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Story
{
    /// <summary>A step handed to the presentation layer.</summary>
    public sealed class StoryCommand
    {
        public string Op;
        public string BeatId;
        public int BeatPriority;
        /// <summary>Resolved and formatted text (line lookup + placeholders), when the step has text.</summary>
        public string Text;
        /// <summary>Extra delay in story seconds, already divided by the time scale (real seconds).</summary>
        public double DelaySeconds;
        public StoryStep Step;
        public string Arg(string key, string fallback = null) { return Step == null ? fallback : Step.Str(key, fallback); }
        public double Num(string key, double fallback = 0) { return Step == null ? fallback : Step.Num(key, fallback); }
    }

    /// <summary>
    /// Presentation side. Blocking ops ("narrate", "cutscene") receive a non-null <c>done</c> and must call it
    /// exactly once (late or repeated calls are ignored). Other ops get <c>done == null</c> and must not block.
    /// </summary>
    public interface IStoryOutput
    {
        void Execute(StoryCommand command, Action done);
    }

    /// <summary>
    /// Plays beats one at a time. Engine-agnostic and deterministic: the host calls <see cref="Signal"/> and
    /// <see cref="Tick"/>. Beats are marked fired only after the last step, so quitting mid-beat replays it.
    /// </summary>
    public sealed class StoryRunner
    {
        public static readonly string[] KnownOps =
        { "narrate", "say", "notify", "open", "unlock", "cutscene", "wait", "waitSignal", "flag", "set", "emit", "first", "unfire", "log" };
        public static readonly string[] BlockingOps = { "narrate", "cutscene" };
        /// <summary>Beats at or above this priority ignore pacing and busy gates.</summary>
        public const int UrgentPriority = 100;

        private readonly StoryEngine engine;
        private readonly List<StoryBeat> queue = new List<StoryBeat>();
        private readonly List<StoryBeat> matches = new List<StoryBeat>();
        private readonly HashSet<string> busyIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<KeyValuePair<string, string>> signals = new Queue<KeyValuePair<string, string>>();
        private StoryBeat current;
        private int stepIndex, token;
        private bool narratedInBeat;
        private WaitKind wait;
        private double waitLeft, waitRealLeft;
        private string waitSignal;
        private bool processing;

        private enum WaitKind { None, Timer, Done, Signal }

        public StoryRunner(StoryLibrary library, StoryContext context, IStoryOutput output)
        {
            if (library == null) throw new ArgumentNullException("library");
            if (context == null) throw new ArgumentNullException("context");
            engine = new StoryEngine(library);
            Library = library;
            Context = context;
            Output = output;
        }

        public StoryLibrary Library { get; private set; }
        public StoryEngine Engine { get { return engine; } }
        public StoryContext Context { get; private set; }
        public IStoryOutput Output { get; set; }
        public IStoryVarsWriter Writer { get; set; }
        /// <summary>Story time multiplier (debug fast-forward). Waits, cooldowns and delays all scale.</summary>
        public double TimeScale { get; set; } = 1;
        /// <summary>Minimum story seconds between narration beats (not between lines inside one beat).</summary>
        public double MinNarrationGap { get; set; } = 40;
        /// <summary>When true, narration and cutscenes wait (player is mid-exam, dragging, typing...).</summary>
        public Func<bool> IsBusy { get; set; }
        /// <summary>Placeholder resolver for {name}, {date}... Null leaves tokens untouched.</summary>
        public Func<string, string> Resolve { get; set; }
        public event Action<string> Log;
        /// <summary>Raised when durable story state changed and should be saved.</summary>
        public event Action Dirty;
        /// <summary>Raised when a beat starts and finishes (for debug views and tests).</summary>
        public event Action<StoryBeat> BeatStarted, BeatFinished;

        public StoryBeat Current { get { return current; } }
        public int QueueCount { get { return queue.Count; } }
        public bool IsWaitingForOutput { get { return wait == WaitKind.Done; } }

        public void Signal(string name, string arg = null)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (wait == WaitKind.Signal && (waitSignal == name || (arg != null && waitSignal == name + ":" + arg))) wait = WaitKind.None;
            signals.Enqueue(new KeyValuePair<string, string>(name, arg));
        }

        public void Tick(double realSeconds)
        {
            if (double.IsNaN(realSeconds) || realSeconds < 0) realSeconds = 0;
            double dt = realSeconds * TimeScale;
            Context.State.clock += dt;
            ProcessSignals();
            Advance(dt, realSeconds);
            // Signals emitted by steps this tick get evaluated and started now, so chains do not lag a frame.
            if (signals.Count > 0)
            {
                ProcessSignals();
                Advance(0, 0);
            }
        }

        /// <summary>Releases whatever the current step waits for (debug skip, player click-through).</summary>
        public void SkipWait()
        {
            if (wait == WaitKind.None) return;
            token++;
            wait = WaitKind.None;
        }

        /// <summary>Drops queued and current beats without marking them fired (on save reload / reset).</summary>
        public void Clear()
        {
            token++;
            queue.Clear();
            signals.Clear();
            current = null;
            wait = WaitKind.None;
        }

        private void ProcessSignals()
        {
            if (processing) return;
            processing = true;
            try
            {
                int guard = 0;
                while (signals.Count > 0 && guard++ < 256)
                {
                    var s = signals.Dequeue();
                    RebuildBusy();
                    engine.Evaluate(s.Key, s.Value, Context, busyIds, matches);
                    foreach (var beat in matches) Enqueue(beat);
                }
            }
            finally { processing = false; }
        }

        private void RebuildBusy()
        {
            busyIds.Clear();
            if (current != null) busyIds.Add(current.Id);
            for (int i = 0; i < queue.Count; i++) busyIds.Add(queue[i].Id);
        }

        private void Enqueue(StoryBeat beat)
        {
            int at = queue.Count;
            while (at > 0 && queue[at - 1].Priority < beat.Priority) at--;
            queue.Insert(at, beat);
            busyIds.Add(beat.Id);
            Emit("queued " + beat.Id);
        }

        private void Advance(double dt, double realDt)
        {
            for (int guard = 0; guard < 128; guard++)
            {
                if (wait == WaitKind.Timer)
                {
                    waitLeft -= dt; dt = 0;
                    if (waitLeft > 1e-9) return;
                    wait = WaitKind.None;
                }
                else if (wait == WaitKind.Done)
                {
                    waitRealLeft -= realDt; realDt = 0;
                    if (waitRealLeft > 0) return;
                    Emit("timeout waiting for output in " + (current != null ? current.Id : "?"));
                    token++;
                    wait = WaitKind.None;
                }
                else if (wait == WaitKind.Signal)
                {
                    if (waitLeft > 0)
                    {
                        waitLeft -= dt; dt = 0;
                        if (waitLeft <= 1e-9) { wait = WaitKind.None; continue; }
                    }
                    return;
                }

                if (current == null)
                {
                    if (queue.Count == 0) return;
                    current = queue[0];
                    queue.RemoveAt(0);
                    stepIndex = 0;
                    narratedInBeat = false;
                    Emit("start " + current.Id);
                    var started = BeatStarted; if (started != null) started(current);
                }

                if (stepIndex >= current.Steps.Length) { Finish(); continue; }
                var step = current.Steps[stepIndex];
                if (MustHold(step)) return;
                stepIndex++;
                Run(step);
            }
        }

        private bool MustHold(StoryStep step)
        {
            if (current.Priority >= UrgentPriority) return false;
            bool presenting = step.Op == "narrate" || step.Op == "cutscene";
            if (!presenting) return false;
            var busy = IsBusy;
            if (busy != null && busy()) return true;
            if (step.Op == "narrate" && !narratedInBeat && Context.State.clock - Context.State.lastNarration < MinNarrationGap) return true;
            return false;
        }

        private void Finish()
        {
            var beat = current;
            current = null;
            var state = Context.State;
            if (beat.Once) state.MarkFired(beat.Id);
            state.NotePlay(beat.Id);
            if (beat.Group != null) state.NotePlay("@" + beat.Group);
            Emit("finish " + beat.Id);
            var finished = BeatFinished; if (finished != null) finished(beat);
            RaiseDirty();
        }

        private void Run(StoryStep step)
        {
            var state = Context.State;
            switch (step.Op)
            {
                case "wait":
                    wait = WaitKind.Timer;
                    waitLeft = Math.Max(0, step.Num("seconds"));
                    return;
                case "waitSignal":
                    wait = WaitKind.Signal;
                    waitSignal = step.Str("ev");
                    waitLeft = Math.Max(0, step.Num("timeout"));
                    if (string.IsNullOrEmpty(waitSignal)) { Emit("waitSignal without ev in " + current.Id); wait = WaitKind.None; }
                    return;
                case "flag":
                    state.SetFlag(step.Str("key", ""), step.Num("value", 1));
                    RaiseDirty();
                    return;
                case "set":
                    if (Writer == null || !Writer.TrySet(step.Str("name", ""), step.Num("value")))
                        Emit("set refused: " + step.Str("name", "") + " in " + current.Id);
                    else RaiseDirty();
                    return;
                case "emit":
                    Signal(step.Str("ev"), step.Str("arg"));
                    return;
                case "unfire":
                    state.Unfire(step.Str("id", ""));
                    RaiseDirty();
                    return;
                case "log":
                    Emit(step.Str("text", ""));
                    return;
                case "first":
                    RecordFirst(step);
                    return;
                case "unlock":
                    state.SetFlag("unlock." + step.Str("id", ""), 1);
                    RaiseDirty();
                    Present(step, null);
                    return;
                case "narrate":
                    narratedInBeat = true;
                    state.lastNarration = state.clock;
                    RaiseDirty();
                    PresentBlocking(step);
                    return;
                case "cutscene":
                    PresentBlocking(step);
                    return;
                case "say":
                case "notify":
                case "open":
                    Present(step, null);
                    return;
                default:
                    Emit("unknown op \"" + step.Op + "\" in " + current.Id);
                    return;
            }
        }

        private void PresentBlocking(StoryStep step)
        {
            var command = Build(step);
            if (Output == null) return; // headless: nothing to wait for
            int mine = ++token;
            wait = WaitKind.Done;
            double textTime = command.Text == null ? 0 : command.Text.Length * .25;
            waitRealLeft = step.Has("timeout") ? step.Num("timeout") : (step.Op == "cutscene" ? 180 : Math.Max(8, textTime + 6));
            Output.Execute(command, () => { if (mine == token && wait == WaitKind.Done) wait = WaitKind.None; });
        }

        private void Present(StoryStep step, Action done)
        {
            var output = Output;
            if (output != null) output.Execute(Build(step), done);
        }

        private StoryCommand Build(StoryStep step)
        {
            var command = new StoryCommand
            {
                Op = step.Op,
                BeatId = current.Id,
                BeatPriority = current.Priority,
                Step = step,
                DelaySeconds = Math.Max(0, step.Num("delay")) / Math.Max(1e-3, TimeScale),
            };
            command.Text = TextOf(step);
            return command;
        }

        private string TextOf(StoryStep step)
        {
            string key = step.Str("key");
            string raw = null;
            if (key != null)
            {
                var play = Context.State.GetPlay(current.Id);
                if (!Library.TryGetLine(key, play == null ? 0 : play.count, out raw))
                {
                    Emit("missing line \"" + key + "\" in " + current.Id);
                    raw = "[" + key + "]";
                }
            }
            else raw = step.Str("text");
            return raw == null ? null : StoryText.Format(raw, Resolve);
        }

        private void RecordFirst(StoryStep step)
        {
            double day, seconds, value = 0;
            Context.TryGet("day", out day);
            Context.TryGet("gameSeconds", out seconds);
            string valueVar = step.Str("valueOf");
            if (valueVar != null && !Context.TryGet(valueVar, out value)) Emit("first: unknown valueOf \"" + valueVar + "\"");
            var record = new StoryFirst
            {
                id = step.Str("id", ""),
                day = (int)day,
                gameSeconds = seconds,
                text = TextOf(step) ?? "",
                value = value,
            };
            if (Context.State.RecordFirst(record)) RaiseDirty();
        }

        private void RaiseDirty() { var d = Dirty; if (d != null) d(); }
        private void Emit(string message) { var l = Log; if (l != null) l(message); }
    }
}
