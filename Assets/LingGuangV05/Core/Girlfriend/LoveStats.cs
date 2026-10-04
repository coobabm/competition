using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>One line of the chat with her, stripped of everything the statistics do not need.</summary>
    public struct LoveMessage
    {
        public bool fromHer;
        public string text;
        /// <summary>Play time the line was written at (real seconds since the save began).</summary>
        public double gameSeconds;
        /// <summary>The desktop clock at that moment (the story calendar's date, the real time of day).</summary>
        public DateTime clock;

        public LoveMessage(bool fromHer, string text, double gameSeconds, DateTime clock)
        {
            this.fromHer = fromHer; this.text = text ?? ""; this.gameSeconds = gameSeconds; this.clock = clock;
        }
    }

    /// <summary>Why a message was picked for the attention links in the 「她爱我吗」 cutscene.</summary>
    public enum LovePickKind { Longest, Latest, Cold, Unanswered }

    public struct LovePick
    {
        public LovePickKind kind;
        public int index;
        public LoveMessage message;
    }

    /// <summary>One week of the three mini charts. Delays are in seconds; -1 means there was nothing to measure.</summary>
    public struct LoveWeek
    {
        public double herReplyDelay;
        public int herGoodnightFirst, myGoodnightFirst;
        public double herLength, herEmoji;
        public int herMessages;
    }

    /// <summary>
    /// The numbers the 「她爱我吗」 cutscene shows (design 女友系统 §6.4). Everything is worked out from the real chat:
    /// how many lines and days, four messages for the attention links, and three weekly series (her average reply
    /// delay, who said goodnight first, her message length and emoji count). Pure C#, no engine types.
    /// Calendar days come from <see cref="LoveMessage.clock"/>; delays come from play time, because the story calendar
    /// can jump forward while the real time between two lines stays short.
    /// </summary>
    public sealed class LoveStats
    {
        /// <summary>Trigger floor (§6.1): at least this many lines with her, spanning at least this many calendar days.</summary>
        public const int MinMessages = 60, MinDays = 7;
        /// <summary>A reply later than this is not counted as a reply (she or you simply came back the next day).</summary>
        public const double MaxReplyDelay = 86400;
        /// <summary>Her line counts as unanswered when your next line comes this much later, or never.</summary>
        public const double UnansweredAfter = 7200;
        /// <summary>The weeks the charts show at most (the latest ones).</summary>
        public const int ChartWeeks = 8;

        static readonly string[] ColdWords = { "哦", "噢", "嗯", "随便", "哦哦", "ok", "k", "fine", "whatever", "oh", "mm", "hm" };
        static readonly string[] GoodnightWords = { "晚安", "睡了", "睡啦", "先睡", "good night", "goodnight", "night night", "nighty night" };

        public readonly List<LoveMessage> messages;
        public int Count => messages.Count;
        public int HerCount { get; private set; }
        public int MyCount { get; private set; }
        /// <summary>Calendar days from the first line to the last, both included (0 with no lines).</summary>
        public int DaySpan { get; private set; }
        public readonly List<LovePick> picks = new List<LovePick>();
        public readonly List<LoveWeek> weeks = new List<LoveWeek>();
        /// <summary>Your average reply delay over the last 7 days of the chat (-1 if you never replied then).</summary>
        public double MyRecentReplyDelay { get; private set; } = -1;
        /// <summary>Her average reply delay over the whole chat (-1 if she never replied).</summary>
        public double HerReplyDelay { get; private set; } = -1;
        public int HerGoodnightFirst { get; private set; }
        public int MyGoodnightFirst { get; private set; }

        /// <summary>
        /// Total lines and first line time when the chat history kept in the save was trimmed; the counter and the day
        /// span use them when they are larger than what is left.
        /// </summary>
        public LoveStats(IEnumerable<LoveMessage> chat, int totalMessages = 0, DateTime? firstEver = null)
        {
            messages = new List<LoveMessage>();
            if (chat != null)
                foreach (var m in chat) messages.Add(new LoveMessage(m.fromHer, m.text, m.gameSeconds, m.clock));
            // Lines are in the order they were written; a stable sort only guards against a shuffled list.
            var order = new List<int>();
            for (int i = 0; i < messages.Count; i++) order.Add(i);
            order.Sort((a, b) => { int c = messages[a].gameSeconds.CompareTo(messages[b].gameSeconds); return c != 0 ? c : a.CompareTo(b); });
            var sorted = new List<LoveMessage>(); foreach (int i in order) sorted.Add(messages[i]);
            messages.Clear(); messages.AddRange(sorted);

            foreach (var m in messages) if (m.fromHer) HerCount++; else MyCount++;
            TotalMessages = Math.Max(totalMessages, messages.Count);
            if (messages.Count > 0)
            {
                DateTime first = messages[0].clock.Date, last = messages[messages.Count - 1].clock.Date;
                if (firstEver.HasValue && firstEver.Value.Date < first) first = firstEver.Value.Date;
                DaySpan = Math.Max(1, (int)Math.Round((last - first).TotalDays) + 1);
            }
            Pick();
            Weekly();
            MyRecentReplyDelay = MyRecent();
        }

        /// <summary>The counter's number: every line ever written when the save knows it, else the lines kept.</summary>
        public int TotalMessages { get; private set; }

        /// <summary>§6.1: enough history with her to read (the cold signal and stage are checked by the caller).</summary>
        public bool EnoughHistory => TotalMessages >= MinMessages && DaySpan >= MinDays;

        // ───────────── text helpers ─────────────

        /// <summary>Text elements (so an emoji counts as one) without surrounding spaces.</summary>
        public static int Length(string text)
        {
            string t = (text ?? "").Trim();
            return t.Length == 0 ? 0 : new StringInfo(t).LengthInTextElements;
        }

        /// <summary>The bare word of a short line: punctuation, spaces and trailing tildes dropped, lower case.</summary>
        static string Bare(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in (text ?? "").Trim().ToLowerInvariant())
                if (!char.IsPunctuation(c) && !char.IsWhiteSpace(c) && c != '~' && c != '～' && !char.IsSymbol(c)) sb.Append(c);
            return sb.ToString();
        }

        /// <summary>A cold one-word reply: 哦, 嗯, 随便 and the like (§6.1).</summary>
        public static bool IsColdWord(string text)
        {
            string b = Bare(text);
            if (b.Length == 0 || b.Length > 8) return false;
            foreach (var w in ColdWords) if (b == w) return true;
            return false;
        }

        public static bool SaysGoodnight(string text)
        {
            string t = (text ?? "").ToLowerInvariant();
            foreach (var w in GoodnightWords) if (t.Contains(w)) return true;
            string b = Bare(t);
            return b == "gn" || b == "night" || b == "安";
        }

        /// <summary>Emoji in a line: YY faces like [微笑], picture characters, and the (^_^) style faces.</summary>
        public static int Emoji(string text)
        {
            string t = text ?? "";
            int n = 0;
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] == '[')
                {
                    int close = t.IndexOf(']', i + 1);
                    if (close > i + 1 && close - i - 1 <= 4) { n++; i = close; continue; }
                }
                if (t[i] == '(' && i + 4 < t.Length && t[i + 1] == '^') { int close = t.IndexOf(')', i + 1); if (close > 0 && close - i <= 6) { n++; i = close; continue; } }
                if (char.IsHighSurrogate(t[i])) { n++; i++; continue; }
                if (CharUnicodeInfo.GetUnicodeCategory(t[i]) == UnicodeCategory.OtherSymbol) n++;
            }
            return n;
        }

        static bool IsQuestion(string text) => (text ?? "").IndexOf('？') >= 0 || (text ?? "").IndexOf('?') >= 0;

        /// <summary>How late in the night a line is: minutes after 06:00, so 01:00 is later than 23:00.</summary>
        static int Lateness(DateTime clock) => ((clock.Hour + 18) % 24) * 60 + clock.Minute;

        // ───────────── the four attention links ─────────────

        void Pick()
        {
            picks.Clear();
            var used = new HashSet<int>();
            int longest = -1, latest = -1, cold = -1, unanswered = -1, unansweredScore = -1;
            for (int i = 0; i < messages.Count; i++)
            {
                var m = messages[i];
                if (!m.fromHer) continue;
                int len = Length(m.text);
                if (longest < 0 || len > Length(messages[longest].text)) longest = i;
                if (latest < 0 || Lateness(m.clock) >= Lateness(messages[latest].clock)) latest = i;
                if (IsColdWord(m.text)) cold = i;
                // Something she brought up that you never answered: a real line (not a cold word or goodnight),
                // and your next line came two hours later or never. Questions first, then the most recent.
                if (len >= 6 && !IsColdWord(m.text) && !SaysGoodnight(m.text) && Unanswered(i))
                {
                    int score = (IsQuestion(m.text) ? 1000000 : 0) + i;
                    if (score >= unansweredScore) { unansweredScore = score; unanswered = i; }
                }
            }
            Add(LovePickKind.Longest, longest, used);
            Add(LovePickKind.Latest, latest, used);
            Add(LovePickKind.Cold, cold, used);
            Add(LovePickKind.Unanswered, unanswered, used);
        }

        void Add(LovePickKind kind, int index, HashSet<int> used)
        {
            if (index < 0 || !used.Add(index)) return;
            picks.Add(new LovePick { kind = kind, index = index, message = messages[index] });
        }

        bool Unanswered(int i)
        {
            for (int j = i + 1; j < messages.Count; j++)
                if (!messages[j].fromHer) return messages[j].gameSeconds - messages[i].gameSeconds > UnansweredAfter;
            return true;
        }

        public bool Has(LovePickKind kind) { foreach (var p in picks) if (p.kind == kind) return true; return false; }
        public LovePick Get(LovePickKind kind) { foreach (var p in picks) if (p.kind == kind) return p; return new LovePick { index = -1 }; }

        // ───────────── weekly series ─────────────

        void Weekly()
        {
            weeks.Clear();
            if (messages.Count == 0) return;
            DateTime start = messages[0].clock.Date;
            int count = Week(start, messages[messages.Count - 1].clock) + 1;
            var delaySum = new double[count]; var delayN = new int[count];
            var lenSum = new double[count]; var emoSum = new double[count]; var herN = new int[count];
            var herFirst = new int[count]; var myFirst = new int[count];
            double allDelay = 0; int allN = 0;
            var nights = new HashSet<DateTime>();
            for (int i = 0; i < messages.Count; i++)
            {
                var m = messages[i];
                int w = Math.Max(0, Week(start, m.clock));
                if (m.fromHer)
                {
                    herN[w]++; lenSum[w] += Length(m.text); emoSum[w] += Emoji(m.text);
                    if (i > 0 && !messages[i - 1].fromHer)
                    {
                        double d = m.gameSeconds - messages[i - 1].gameSeconds;
                        if (d >= 0 && d <= MaxReplyDelay) { delaySum[w] += d; delayN[w]++; allDelay += d; allN++; }
                    }
                }
                // Who said goodnight first: one count per night, the night running from 06:00 to 06:00.
                if (SaysGoodnight(m.text) && nights.Add(m.clock.AddHours(-6).Date))
                {
                    if (m.fromHer) { herFirst[w]++; HerGoodnightFirst++; } else { myFirst[w]++; MyGoodnightFirst++; }
                }
            }
            HerReplyDelay = allN > 0 ? allDelay / allN : -1;
            for (int w = 0; w < count; w++)
                weeks.Add(new LoveWeek
                {
                    herReplyDelay = delayN[w] > 0 ? delaySum[w] / delayN[w] : -1,
                    herGoodnightFirst = herFirst[w], myGoodnightFirst = myFirst[w],
                    herLength = herN[w] > 0 ? lenSum[w] / herN[w] : 0,
                    herEmoji = herN[w] > 0 ? emoSum[w] / herN[w] : 0,
                    herMessages = herN[w],
                });
        }

        static int Week(DateTime start, DateTime at) => (int)Math.Floor((at.Date - start).TotalDays / 7);

        /// <summary>The latest weeks for the charts (at most <see cref="ChartWeeks"/>).</summary>
        public List<LoveWeek> ChartSlice()
        {
            int from = Math.Max(0, weeks.Count - ChartWeeks);
            return weeks.GetRange(from, weeks.Count - from);
        }

        double MyRecent()
        {
            if (messages.Count == 0) return -1;
            DateTime since = messages[messages.Count - 1].clock.Date.AddDays(-6);
            double sum = 0; int n = 0;
            for (int i = 1; i < messages.Count; i++)
            {
                var m = messages[i];
                if (m.fromHer || !messages[i - 1].fromHer || m.clock.Date < since) continue;
                double d = m.gameSeconds - messages[i - 1].gameSeconds;
                if (d >= 0 && d <= MaxReplyDelay) { sum += d; n++; }
            }
            return n > 0 ? sum / n : -1;
        }

        /// <summary>-1 falling, 0 flat, +1 rising: the later half of the weeks against the earlier half (15% either way).</summary>
        public static int Trend(IList<double> values)
        {
            var v = new List<double>();
            foreach (var x in values) if (x >= 0) v.Add(x);
            if (v.Count < 2) return 0;
            int half = v.Count / 2;
            double a = 0, b = 0;
            for (int i = 0; i < half; i++) a += v[i];
            for (int i = v.Count - half; i < v.Count; i++) b += v[i];
            a /= half; b /= half;
            if (a <= 1e-9) return b > 1e-9 ? 1 : 0;
            double r = b / a;
            return r > 1.15 ? 1 : r < 1 / 1.15 ? -1 : 0;
        }

        public List<double> Series(Func<LoveWeek, double> pick)
        {
            var list = new List<double>();
            foreach (var w in ChartSlice()) list.Add(pick(w));
            return list;
        }

        // ───────────── words for the model prompt and the offline lines ─────────────

        /// <summary>A delay in words: 「3 小时 12 分」 / "3 h 12 min".</summary>
        public static string Duration(double seconds, bool english)
        {
            if (seconds < 0) return english ? "unknown" : "未知";
            int s = (int)Math.Round(seconds);
            if (s < 60) return english ? s + " s" : s + " 秒";
            if (s < 3600) return english ? s / 60 + " min" : s / 60 + " 分钟";
            int h = s / 3600, min = s % 3600 / 60;
            return english ? h + " h " + min + " min" : h + " 小时 " + min + " 分";
        }

        /// <summary>A clock time as people say it: 「12 点 40」 / "12:40 a.m.".</summary>
        public static string Spoken(DateTime clock, bool english)
        {
            string minute = clock.Minute.ToString("00", CultureInfo.InvariantCulture);
            if (english) return clock.Hour == 0 ? "12:" + minute + " a.m." : clock.Hour + ":" + minute;
            return (clock.Hour == 0 ? 12 : clock.Hour) + " 点 " + minute;
        }

        /// <summary>The 「回复时间趋势」 slot: her average reply delay in the first and the latest chart week.</summary>
        public string ReplyTrendText(bool english)
        {
            var v = new List<double>();
            foreach (var w in ChartSlice()) if (w.herReplyDelay >= 0) v.Add(w.herReplyDelay);
            if (v.Count == 0) return english ? "no replies to measure" : "没有可算的回复";
            string mine = MyRecentReplyDelay >= 0 ? (english ? "; you reply to her in " + Duration(MyRecentReplyDelay, true) + " on average lately" : "；你最近回她平均要 " + Duration(MyRecentReplyDelay, false)) : "";
            if (v.Count == 1) return (english ? "her average reply: " + Duration(v[0], true) : "她平均 " + Duration(v[0], false) + " 回你") + mine;
            return (english ? "her average reply went from " + Duration(v[0], true) + " to " + Duration(v[v.Count - 1], true) + " a week"
                : "她平均回复时间从 " + Duration(v[0], false) + " 变成 " + Duration(v[v.Count - 1], false)) + mine;
        }

        /// <summary>The 「谁先说晚安」 slot.</summary>
        public string GoodnightText(bool english)
        {
            int her = 0, me = 0;
            foreach (var w in ChartSlice()) { her += w.herGoodnightFirst; me += w.myGoodnightFirst; }
            if (her + me == 0) return english ? "nobody said goodnight" : "没人说过晚安";
            return english ? "goodnight first: her " + her + " nights, you " + me : "先说晚安的：她 " + her + " 晚，你 " + me + " 晚";
        }

        /// <summary>The 「她最近的冷信号」 slot: her latest cold word with its time, or the signature when given.</summary>
        public string ColdText(bool english, string signature = null)
        {
            var sb = new StringBuilder();
            if (Has(LovePickKind.Cold))
            {
                var c = Get(LovePickKind.Cold).message;
                sb.Append(english ? "she replied \"" + c.text.Trim() + "\" at " + c.clock.ToString("MMM d H:mm", CultureInfo.InvariantCulture)
                    : "她 " + c.clock.Month + "月" + c.clock.Day + "日 " + c.clock.ToString("HH:mm", CultureInfo.InvariantCulture) + " 回了「" + c.text.Trim() + "」");
            }
            if (!string.IsNullOrEmpty(signature))
            {
                if (sb.Length > 0) sb.Append(english ? "; " : "；");
                sb.Append(english ? "her signature says \"" + signature + "\"" : "她的签名是「" + signature + "」");
            }
            return sb.Length > 0 ? sb.ToString() : english ? "none" : "没有";
        }
    }
}
