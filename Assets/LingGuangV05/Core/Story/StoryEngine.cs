using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.Core.Story
{
    /// <summary>
    /// Pure selection logic. Given a signal and the current facts, returns the beats that should play.
    /// Ideas borrowed from rule-database dialogue (Valve), storylets (Failbetter, Short) and priority
    /// tiers with no-repeat selection (Supergiant):
    /// <list type="bullet">
    /// <item>Ungrouped beats: every matching beat plays (story beats).</item>
    /// <item>Grouped beats: one winner per group: highest priority, then most conditions (most specific),
    /// then least played, then least recently played, then file order. Special cases beat generic barks,
    /// and variety comes for free.</item>
    /// </list>
    /// Does not mutate state; the runner records plays after a beat finishes.
    /// </summary>
    public sealed class StoryEngine
    {
        public const string AnySignal = "*";
        private readonly Dictionary<string, List<StoryBeat>> index = new Dictionary<string, List<StoryBeat>>(StringComparer.Ordinal);
        private readonly List<StoryBeat> candidates = new List<StoryBeat>();
        private readonly Dictionary<string, StoryBeat> groupWinners = new Dictionary<string, StoryBeat>(StringComparer.Ordinal);
        private readonly HashSet<string> broken = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> errors = new List<string>();

        /// <summary>Beats disabled at runtime because a condition named an unknown variable.</summary>
        public IReadOnlyList<string> Errors { get { return errors; } }

        public StoryEngine(StoryLibrary library)
        {
            Library = library;
            foreach (var beat in library.Beats)
                foreach (var on in beat.On)
                {
                    List<StoryBeat> list;
                    if (!index.TryGetValue(on, out list)) index[on] = list = new List<StoryBeat>();
                    list.Add(beat);
                }
        }

        public StoryLibrary Library { get; private set; }

        /// <summary>Signal keys a beat can listen to: "name", "name:arg" and "*".</summary>
        public void Evaluate(string signal, string arg, StoryContext context, ICollection<string> busyIds, List<StoryBeat> results)
        {
            results.Clear();
            candidates.Clear();
            groupWinners.Clear();
            Collect(signal);
            if (!string.IsNullOrEmpty(arg)) Collect(signal + ":" + arg);
            Collect(AnySignal);
            candidates.Sort(ByOrder);
            StoryBeat previous = null;
            foreach (var beat in candidates)
            {
                if (beat == previous) continue; // listed under several keys
                previous = beat;
                if (broken.Contains(beat.Id)) continue;
                bool eligible;
                try { eligible = IsEligible(beat, context, busyIds); }
                catch (StoryUnknownVariableException e)
                {
                    // One bad beat must not silence the whole story. Content lint catches these in tests.
                    broken.Add(beat.Id);
                    errors.Add(beat.Id + ": " + e.Message);
                    continue;
                }
                if (!eligible) continue;
                if (beat.Group == null) { results.Add(beat); continue; }
                StoryBeat best;
                if (!groupWinners.TryGetValue(beat.Group, out best) || Better(beat, best, context.State)) groupWinners[beat.Group] = beat;
            }
            foreach (var winner in groupWinners.Values) results.Add(winner);
            results.Sort(ByPriorityThenOrder);
        }

        private void Collect(string key)
        {
            List<StoryBeat> list;
            if (index.TryGetValue(key, out list)) candidates.AddRange(list);
        }

        public bool IsEligible(StoryBeat beat, StoryContext context, ICollection<string> busyIds)
        {
            var state = context.State;
            if (beat.Once && state.HasFired(beat.Id)) return false;
            if (busyIds != null && busyIds.Contains(beat.Id)) return false;
            if (beat.Cooldown > 0)
            {
                var play = state.GetPlay(beat.Id);
                if (play != null && state.clock - play.last < beat.Cooldown) return false;
            }
            if (beat.Group != null && beat.GroupCooldown > 0)
            {
                var play = state.GetPlay("@" + beat.Group);
                if (play != null && state.clock - play.last < beat.GroupCooldown) return false;
            }
            for (int i = 0; i < beat.When.Length; i++)
                if (!beat.When[i].Eval(context)) return false;
            return true;
        }

        private static bool Better(StoryBeat a, StoryBeat b, StoryState state)
        {
            if (a.Priority != b.Priority) return a.Priority > b.Priority;
            if (a.Specificity != b.Specificity) return a.Specificity > b.Specificity;
            var pa = state.GetPlay(a.Id); var pb = state.GetPlay(b.Id);
            int ca = pa == null ? 0 : pa.count, cb = pb == null ? 0 : pb.count;
            if (ca != cb) return ca < cb;
            double la = pa == null ? double.MinValue : pa.last, lb = pb == null ? double.MinValue : pb.last;
            if (la != lb) return la < lb;
            return a.Order < b.Order;
        }

        private static int ByOrder(StoryBeat a, StoryBeat b) { return a.Order.CompareTo(b.Order); }
        private static int ByPriorityThenOrder(StoryBeat a, StoryBeat b)
        {
            int p = b.Priority.CompareTo(a.Priority);
            return p != 0 ? p : a.Order.CompareTo(b.Order);
        }

        /// <summary>Debug text: why a beat would or would not play right now.</summary>
        public string Explain(string beatId, StoryContext context)
        {
            var beat = Library.Get(beatId);
            if (beat == null) return beatId + ": no such beat";
            var state = context.State;
            var sb = new StringBuilder(beatId);
            if (beat.Once && state.HasFired(beat.Id)) return sb.Append(": already fired").ToString();
            for (int i = 0; i < beat.When.Length; i++)
            {
                double v;
                context.TryGet(beat.When[i].Field, out v);
                if (!beat.When[i].Eval(context)) return sb.Append(": blocked by \"").Append(beat.When[i].Source).Append("\" (")
                    .Append(beat.When[i].Field).Append(" = ").Append(v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(")").ToString();
            }
            return sb.Append(": ready (waits for ").Append(string.Join(", ", beat.On)).Append(")").ToString();
        }
    }
}
