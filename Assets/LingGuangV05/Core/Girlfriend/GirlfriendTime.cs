using System;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>
    /// One moment for her rules: play time (game seconds, what chat lines are stamped with), the desktop clock
    /// (GameCalendar: the calendar's date plus the real computer's hours and minutes) and the calendar day index.
    /// Story seconds count each calendar day jump as a whole day passing and never go backwards, even when the
    /// real clock passes midnight or a new session starts at a different hour.
    /// </summary>
    public struct GfNow
    {
        public double game;
        public DateTime clock;
        public int day;
        /// <summary>Monotonic story seconds from the save (GameCalendar.StorySeconds); 0 for test moments built from a clock.</summary>
        public double story;

        public double Story => story > 0 ? story : (clock - GameCalendar.Start).TotalSeconds;
        public int Hour => clock.Hour;
        public double HourOfDay => clock.TimeOfDay.TotalHours;

        public static GfNow Of(GameState s)
        {
            double game = s != null ? s.gameSeconds : 0;
            return new GfNow { game = game, clock = GameCalendar.Now(s), day = GameCalendar.CurrentDay(s), story = GameCalendar.StorySeconds(s, game) };
        }

        /// <summary>A moment for tests: the calendar date and clock, and play time.</summary>
        public static GfNow At(DateTime clock, double game)
        {
            return new GfNow { game = game, clock = clock, day = GameCalendar.DayIndex(clock) };
        }
    }

    /// <summary>What her YY shows: WiFi 在线, 4G 在线, or 离开 (away, asleep or out for the day).</summary>
    public enum GfPresence { Wifi, FourG, Away, Asleep, Out }

    /// <summary>Where she is and what she is doing, for the status line and the prompt's 「此刻」.</summary>
    public struct GfActivity
    {
        public GfPresence presence;
        public string zh, en;
        /// <summary>She has class tomorrow morning (term weekday nights).</summary>
        public bool classTomorrow;
        public bool Present => presence == GfPresence.Wifi || presence == GfPresence.FourG;
    }

    public static partial class GirlfriendRules
    {
        public static readonly DateTime TermStart = new DateTime(2016, 9, 1);
        public static readonly DateTime SummerStart = new DateTime(2016, 6, 1);

        /// <summary>Summer holiday at home (June to August): same town, WiFi at home. Otherwise she is at university.</summary>
        public static bool Summer(DateTime date) => date.Date >= SummerStart && date.Date < TermStart;

        // ───────────── dice ─────────────

        /// <summary>Her next die, 0 ≤ x &lt; 1, from the save's seed and position: the same save rolls the same sequence.</summary>
        public static double Roll(GirlfriendState s)
        {
            if (s == null) return .5;
            ulong z = (ulong)(uint)s.seed * 0x9E3779B97F4A7C15UL + (ulong)(uint)(++s.rolls) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / 9007199254740992.0);
        }

        public static int Pick(GirlfriendState s, int count) => count <= 1 ? 0 : Math.Min(count - 1, (int)(Roll(s) * count));

        /// <summary>A fixed die for a calendar day (does not advance the sequence): weekend outings, a shower.</summary>
        public static double DayDie(GirlfriendState s, int day, int salt)
        {
            ulong z = (ulong)(uint)(s != null ? s.seed : 0) * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)day * 0xD6E8FEB86659FD93UL ^ (ulong)(uint)salt * 0xA0761D6478BD642FUL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / 9007199254740992.0);
        }

        // ───────────── 作息 (design §3) ─────────────

        /// <summary>
        /// Her day by the desktop clock and the calendar. Summer: up at 10, dramas in the afternoon, online late at
        /// home on WiFi. Term: classes on weekday mornings and afternoons (离开), the dorm's WiFi goes off at 23:30 and
        /// she stays on 4G in bed. Some weekends she is out with her roommates all day. She sleeps once she has said
        /// good night, until the calendar turns (the desktop clock mostly shows the small hours, and the calendar's
        /// day is what passes).
        /// </summary>
        public static GfActivity Schedule(GirlfriendState s, GfNow now)
        {
            var date = now.clock;
            double h = now.HourOfDay;
            bool summer = Summer(date);
            bool weekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
            var tomorrow = date.Date.AddDays(h < 6 ? 0 : 1).DayOfWeek;
            bool classTomorrow = !summer && tomorrow != DayOfWeek.Saturday && tomorrow != DayOfWeek.Sunday && !Holiday(date.Date.AddDays(h < 6 ? 0 : 1));
            var a = new GfActivity { classTomorrow = classTomorrow };
            if (s != null && s.asleepDay >= 0 && s.asleepDay == now.day)
            {
                a.presence = GfPresence.Asleep; a.zh = "已经睡了"; a.en = "already asleep";
                return a;
            }
            if (OutForTheDay(s, now))
            {
                bool late = h >= 22 || h < 6;
                a.presence = late ? GfPresence.FourG : GfPresence.Out;
                a.zh = late ? "和室友出去玩了一天，刚回来躺下" : "和室友出去玩了，在外面";
                a.en = late ? "just back from a day out with your roommates, lying down" : "out with your roommates for the day";
                return a;
            }
            if (summer)
            {
                if (h >= 6 && h < 10) { a.presence = GfPresence.Away; a.zh = "还在睡懒觉"; a.en = "still sleeping in"; }
                else if (h < 12) { a.presence = GfPresence.Wifi; a.zh = "暑假在老家，刚起床"; a.en = "home for the summer, just got up"; }
                else if (h < 14) { a.presence = GfPresence.Away; a.zh = "在吃午饭"; a.en = "having lunch"; }
                else if (h < 18) { a.presence = GfPresence.Wifi; a.zh = "暑假在老家，下午在追剧"; a.en = "home for the summer, watching dramas all afternoon"; }
                else if (h < 19) { a.presence = GfPresence.Away; a.zh = "在吃晚饭"; a.en = "having dinner"; }
                else if (h < 23) { a.presence = GfPresence.Wifi; a.zh = "暑假在老家，晚上在房间里刷手机"; a.en = "home for the summer, on your phone in your room"; }
                else { a.presence = GfPresence.Wifi; a.zh = "暑假在老家，半夜躺在床上玩手机"; a.en = "home for the summer, in bed on your phone after midnight"; }
                return a;
            }
            bool weekday = !weekend && !Holiday(date.Date);
            if (h >= 6 && h < 7.5) { a.presence = GfPresence.Away; a.zh = "还没起床"; a.en = "not up yet"; return a; }
            if (weekday && ((h >= 8 && h < 11.67) || (h >= 14 && h < 17.5)))
            {
                a.presence = GfPresence.Away; a.zh = "在上课"; a.en = "in class"; return a;
            }
            if (h >= 7.5 && h < 23.5) { a.presence = GfPresence.Wifi; a.zh = weekday ? "在学校，课间在宿舍" : "周末在宿舍"; a.en = weekday ? "at university, in the dorm between classes" : "in the dorm at the weekend"; return a; }
            a.presence = GfPresence.FourG;
            a.zh = "在宿舍床上，宿舍 23:30 断网了，用 4G 和他聊" + (classTomorrow ? "，明天早上有课" : "");
            a.en = "in bed in the dorm; the dorm WiFi went off at 23:30, so you are on 4G" + (classTomorrow ? "; you have class in the morning" : "");
            return a;
        }

        /// <summary>Some weekend days (and the odd summer day) she is out all day: 离开 until late.</summary>
        public static bool OutForTheDay(GirlfriendState s, GfNow now)
        {
            var d = now.clock.DayOfWeek;
            bool weekend = d == DayOfWeek.Saturday || d == DayOfWeek.Sunday;
            if (HolidayOn(now.clock.Date) != null) return false;
            double die = DayDie(s, now.day, 7);
            return Summer(now.clock) ? die < .08 : weekend && die < .22;
        }

        /// <summary>The status line under her name: 「WiFi 在线」「4G 在线」「离开」.</summary>
        public static string PresenceText(GfPresence p, bool english)
        {
            switch (p)
            {
                case GfPresence.Wifi: return english ? "WiFi online" : "WiFi 在线";
                case GfPresence.FourG: return english ? "4G online" : "4G 在线";
                default: return english ? "Away" : "离开";
            }
        }

        // ───────────── reply delay and typing (design §3) ─────────────

        /// <summary>
        /// Seconds of play before she starts typing a reply. Sweet replies come in seconds, cold ones in minutes
        /// (design: 甜 5–40 s, 冷 5–40 min; the cold end is compressed to 1–4 minutes because a calendar day lasts
        /// well under a minute of play). A bad mood slows her down, a good one speeds her up.
        /// </summary>
        public static double ReplyDelay(GirlfriendState s, GfActivity a)
        {
            double lo, hi;
            switch (Tier(s))
            {
                case GirlfriendTier.Sweet: lo = 3; hi = 12; break;
                case GirlfriendTier.Warm: lo = 5; hi = 20; break;
                case GirlfriendTier.Normal: lo = 8; hi = 32; break;
                case GirlfriendTier.Distant: lo = 25; hi = 80; break;
                default: lo = 60; hi = 240; break;
            }
            double d = lo + (hi - lo) * Roll(s);
            if (s.mood >= 2) d *= .7; else if (s.mood <= -2) d *= 1.5;
            if (s.fighting) d *= 1.4;
            if (a.presence == GfPresence.FourG) d *= 1.15;
            return Math.Round(d, 1);
        }

        /// <summary>Typing time for one message: a quarter of a second per character, 1–8 seconds.</summary>
        public static double TypingSeconds(string text) => Math.Max(1, Math.Min(8, (text ?? "").Length * .25));

        /// <summary>Gap between two bubbles of one reply: 1.5–4 seconds.</summary>
        public static double BubbleGap(GirlfriendState s) => 1.5 + 2.5 * Roll(s);

        /// <summary>
        /// Whether this reply hesitates: 「对方正在输入…」 appears, disappears, and then only 「嗯」 comes (design §3).
        /// Cold tier half the time, a bad mood or a quarrel a third of the time.
        /// </summary>
        public static bool Hesitates(GirlfriendState s)
        {
            double p = Tier(s) == GirlfriendTier.Cold ? .5 : (s.mood <= -2 || s.fighting) ? .33 : Tier(s) == GirlfriendTier.Distant ? .12 : 0;
            return p > 0 && Roll(s) < p;
        }
    }
}
