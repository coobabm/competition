using System;

namespace LingGuangV05.Core
{
    /// <summary>
    /// The opening quiet window. From boot until the lab has learned its second ability (多选一), ambient desktop chatter
    /// is dropped, not saved for later: tray software (360, 鲁大师, the Windows 10 offer), shop and cloud-drive pitches,
    /// the platform's meme notices, the YY group's dated small talk (it still lands in the group, without a popup), the
    /// casino ad and flash cards that are not part of the story (they wait in the album). Story moments are never held
    /// by it: the prologue's own popups, the AI joining YY, 晴雯, 老周, the spider and the love question. The to-do note
    /// is then the one voice telling the player what to do next.
    /// One place decides: <see cref="Active"/>. Callers that pop something up ask <see cref="Allows"/>.
    /// The first minute after the setup also runs one thing at a time, in the order of <see cref="OpeningBeat"/>.
    /// </summary>
    public static class OpeningQuiet
    {
        /// <summary>The window closes when this ability has emerged.</summary>
        public const int EndsWithAbility = 2;
        /// <summary>Seconds between one opening beat finishing and the next starting.</summary>
        public const double BeatGap = 6;
        /// <summary>While the window is open, 晴雯 starts something by herself at most this often (seconds).</summary>
        public const double QingwenSpacing = 90;

        /// <summary>The desktop's reading of the running save (set by the story presenter). Unset outside the game: not quiet.</summary>
        public static Func<bool> Probe;

        /// <summary>The window is open right now.</summary>
        public static bool Active
        {
            get
            {
                var probe = Probe;
                if (probe == null) return false;
                try { return probe(); }
                catch (Exception) { return false; }
            }
        }

        /// <summary>The rule itself: quiet during the prologue and until the second ability, never in test mode.</summary>
        public static bool Rule(bool testMode, bool inPrologue, int abilities) => !testMode && (inPrologue || abilities < EndsWithAbility);

        /// <summary>A story moment always passes; ambient chatter only outside the window.</summary>
        public static bool Allows(bool story) => Allows(story, Active);
        public static bool Allows(bool story, bool quiet) => story || !quiet;

        /// <summary>
        /// Whether a beat of the first minute may start: every beat before it is done. Patience fallbacks (a beat that
        /// can never come, such as 晴雯 being asleep) are the caller's.
        /// </summary>
        public static bool Turn(OpeningBeat beat, bool monthCardDone, bool aiJoined, bool qingwenSpoke)
        {
            switch (beat)
            {
                case OpeningBeat.MonthCard: return true;
                case OpeningBeat.AiJoins: return monthCardDone;
                case OpeningBeat.Qingwen: return monthCardDone && aiJoined;
                default: return monthCardDone && aiJoined && qingwenSpoke;
            }
        }
    }

    /// <summary>The first minute after the setup, in order: the month card, the AI joining YY, 晴雯's first message, then 老周.</summary>
    public enum OpeningBeat { MonthCard, AiJoins, Qingwen, LaoZhou }

    /// <summary>
    /// A beat's turn: ready once the condition before it has held for <see cref="Gap"/> seconds without a break.
    /// Pure (the caller passes its clock), so it can be tested.
    /// </summary>
    public sealed class OpeningTurn
    {
        double since = double.NaN;
        public double Gap { get; }

        public OpeningTurn(double gap = OpeningQuiet.BeatGap) { Gap = Math.Max(0, gap); }

        public bool Ready(bool before, double now)
        {
            if (!before || double.IsNaN(now) || double.IsInfinity(now)) { since = double.NaN; return false; }
            if (double.IsNaN(since)) since = now;
            return now - since >= Gap;
        }

        public void Reset() { since = double.NaN; }
    }
}
