using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Girlfriend
{
    public enum GfEventKind { Say, Signature, Shake, Voice, Choices, Topic }

    /// <summary>Something the YY layer should do: send her bubbles, change her signature, shake the window, a first-time inner voice.</summary>
    public sealed class GfEvent
    {
        public GfEventKind kind;
        public string key = "";
        /// <summary>Bubbles (Say), the signature (Signature) or choice labels (Choices), zh and en in parallel.</summary>
        public List<string> zh = new List<string>(), en = new List<string>();

        public static GfEvent Say(string key, params string[] pairs)
        {
            var e = new GfEvent { kind = GfEventKind.Say, key = key ?? "" };
            for (int i = 0; i + 1 < pairs.Length; i += 2) { e.zh.Add(pairs[i]); e.en.Add(pairs[i + 1]); }
            return e;
        }

        public static GfEvent Of(GfEventKind kind, string key, params string[] pairs)
        {
            var e = Say(key, pairs);
            e.kind = kind;
            return e;
        }
    }

    /// <summary>What one line of his did (design §2), for the reply prompt and for the tests.</summary>
    public sealed class GfTurn
    {
        public bool quick, opened, greetingOnly, apology, byAi;
        public string recalled = "";
        public GfHoliday wished;
        public bool wishedFirst;
        public string promised = "", declined = "";
        /// <summary>"work" or "ai" when he answered 「你哪来的钱」; "admit" or "deny" when he answered being caught.</summary>
        public string answered = "";
        public int moodBefore, affectionBefore;
        public readonly List<GfEvent> events = new List<GfEvent>();
    }

    public static partial class GirlfriendRules
    {
        public const double Hour = 3600;
        public const int MaxMemories = 24;
        public const int PromptMemories = 8;

        // ───────────── basic changes ─────────────

        public static void AddMood(GirlfriendState s, int d) { s.mood = Math.Max(-3, Math.Min(3, s.mood + d)); }
        public static void AddAffection(GirlfriendState s, int d) { s.affection = Math.Max(0, Math.Min(100, s.affection + d)); }

        /// <summary>Lists a hand-edited or partial save left out come back empty, strings come back "".</summary>
        public static void Repair(GirlfriendState s)
        {
            if (s == null) return;
            if (s.memories == null) s.memories = new List<string>();
            if (s.memoryKeys == null) s.memoryKeys = new List<string>();
            while (s.memoryKeys.Count < s.memories.Count) s.memoryKeys.Insert(0, "");
            while (s.memoryKeys.Count > s.memories.Count) s.memoryKeys.RemoveAt(0);
            if (s.recalled == null) s.recalled = new List<string>();
            if (s.promises == null) s.promises = new List<GfPromise>();
            if (s.flags == null) s.flags = new List<string>();
            if (s.done == null) s.done = new List<string>();
            if (s.orders == null) s.orders = new List<GfGiftOrder>();
            if (s.recentLines == null) s.recentLines = new List<string>();
            s.signature = s.signature ?? ""; s.signatureEn = s.signatureEn ?? ""; s.signatureKey = s.signatureKey ?? "";
            s.asking = s.asking ?? ""; s.sulkZh = s.sulkZh ?? ""; s.sulkEn = s.sulkEn ?? "";
            s.affection = Math.Max(0, Math.Min(100, s.affection));
            s.mood = Math.Max(-3, Math.Min(3, s.mood));
        }

        /// <summary>She appears in YY: the seed is fixed and the day is noted. Her first line comes from the life list (「还没睡？」).</summary>
        public static void Begin(GirlfriendState s, GfNow now, int seed)
        {
            if (s == null || s.started) return;
            s.started = true;
            s.seed = seed == 0 ? 20160524 : seed;
            s.today = now.day;
            s.lastTickGame = now.game;
            s.moodStepAt = now.Story;
            s.nextProactiveGame = now.game + 4;
            var sig = PickSignature(s, now);
            s.signatureKey = sig.key; s.signature = sig.zh; s.signatureEn = sig.en; s.signatureAt = now.game;
        }

        // ───────────── the clock ─────────────

        /// <summary>
        /// Advances her state to <paramref name="now"/>: a new calendar day (holidays and promises settled, she
        /// wakes up), mood drifting back to 0, an unanswered message turning into neglect, long silences, the end of a
        /// quarrel that cooled off, and her signature. Returns what the YY layer should show.
        /// </summary>
        public static List<GfEvent> Tick(GirlfriendState s, GfNow now)
        {
            var events = new List<GfEvent>();
            if (s == null || !s.started) return events;
            Repair(s);
            double dt = s.lastTickGame < 0 ? 0 : Math.Max(0, Math.Min(5, now.game - s.lastTickGame));
            s.lastTickGame = now.game;
            if (s.today < 0) s.today = now.day;
            if (now.day > s.today) NewDay(s, now, events);
            s.dayGame += dt;

            // Mood: one step back towards 0 every 6 story hours.
            if (s.moodStepAt <= 0) s.moodStepAt = now.Story;
            double since = now.Story - s.moodStepAt;
            if (since >= 6 * Hour)
            {
                int steps = (int)(since / (6 * Hour));
                for (int i = 0; i < steps && s.mood != 0; i++) s.mood += s.mood > 0 ? -1 : 1;
                s.moodStepAt += steps * 6 * Hour;
            }

            // Neglect: her message unanswered while she is awake.
            if (s.awaitingReply && Schedule(s, now).presence != GfPresence.Asleep)
            {
                double waited = now.Story - s.herLastAt;
                if (waited >= 2 * Hour && s.neglectLevel < 1) { s.neglectLevel = 1; AddMood(s, -1); }
                if (waited >= 8 * Hour && s.neglectLevel < 2) { s.neglectLevel = 2; AddAffection(s, -3); }
            }

            // Long silence: after a week with no word either way, −1 every 3 days, never below 25 on its own.
            if (s.lastAnyAt > 0 && now.Story - s.lastAnyAt >= 7 * 24 * Hour && now.Story - Math.Max(s.driftAt, s.lastAnyAt + 4 * 24 * Hour) >= 3 * 24 * Hour)
            {
                s.driftAt = now.Story;
                if (s.affection > 25) AddAffection(s, -1);
            }

            // A quarrel that has cooled off for three days ends quietly (no make-up moment).
            if (s.fighting && s.fightDay >= 0 && now.day - s.fightDay >= 3 && s.mood >= 0) s.fighting = false;

            var sig = PickSignature(s, now);
            if (sig.key != s.signatureKey && (now.game - s.signatureAt >= 45 || s.signatureKey.Length == 0))
            {
                s.signatureKey = sig.key; s.signature = sig.zh; s.signatureEn = sig.en; s.signatureAt = now.game;
                events.Add(GfEvent.Of(GfEventKind.Signature, sig.key, sig.zh, sig.en));
                if (sig.sad) ColdSignal(s, now);
            }
            return events;
        }

        /// <summary>The calendar moved on: settle the day that ended (only that one: days the calendar jumped over were not lived), wake her up.</summary>
        static void NewDay(GirlfriendState s, GfNow now, List<GfEvent> events)
        {
            int ended = s.today;
            var endedDate = GameCalendar.DateOf(ended);
            bool lived = s.dayGame >= 30 || s.linesDay == ended;
            var h = HolidayOn(endedDate);
            if (h != null && !s.done.Contains("wish:" + h.key) && !s.done.Contains("forgot:" + h.key) && endedDate >= h.date.AddDays(h.span) && h.forgot < 0)
            {
                if (lived)
                {
                    s.done.Add("forgot:" + h.key);
                    AddAffection(s, h.forgot);
                    AddMood(s, -1);
                    if (h.sulkZh.Length > 0) { s.sulkUntil = now.day + 3; s.sulkZh = h.sulkZh; s.sulkEn = h.sulkEn; }
                    if (h.key == "birthday" || h.key == "qixi") StartFight(s, now, events);
                }
            }
            foreach (var p in s.promises)
            {
                if (p.state != 0 || p.dueDay > ended) continue;
                if (p.dueDay < ended) { p.state = 3; continue; } // skipped by the calendar: neither kept nor broken
                bool kept = s.linesDay == ended && (p.key != "movie" || HasTickets(s, ended));
                if (kept) { p.state = 1; AddAffection(s, p.key == "movie" ? 6 : 5); AddMood(s, 1); }
                else if (lived || p.key == "movie" && HasTickets(s, ended)) { p.state = 2; AddAffection(s, -8); AddMood(s, -1); StartFight(s, now, events); }
                else p.state = 3;
            }
            if (s.asking == "qixi" || s.asking == "movie" || s.asking == "weekend") { if (now.day - s.askDay > 2) s.asking = ""; }
            s.today = now.day;
            s.dayGame = 0;
            if (s.asleepDay >= 0 && s.asleepDay < now.day) s.asleepDay = -1;
        }

        static bool HasTickets(GirlfriendState s, int byDay)
        {
            foreach (var o in s.orders) if (o.gift == GiftTickets && o.state != 2 && o.orderedDay <= byDay) return true;
            return false;
        }

        // ───────────── her lines ─────────────

        /// <summary>
        /// She sent these bubbles: she now waits for an answer, memories are picked out (design §3), and a bare
        /// 「哦 / 嗯 / 随便」 is a cold signal (§6.1).
        /// </summary>
        public static void OnHerLines(GirlfriendState s, GfNow now, IList<string> bubbles, bool hesitated = false)
        {
            if (s == null || bubbles == null || bubbles.Count == 0) return;
            if (s.awaitingReply) s.unansweredRun++; else s.unansweredRun = 1;
            s.awaitingReply = true;
            s.neglectLevel = 0;
            s.herLastAt = now.Story; s.herLastGame = now.game; s.lastAnyAt = now.Story;
            foreach (var b in bubbles)
            {
                Count(s, now, true);
                var m = ExtractMemory(b);
                if (m.key.Length > 0) Remember(s, m.key, "她说：" + m.text);
            }
            if (hesitated || (bubbles.Count == 1 && ColdWord(bubbles[0]))) ColdSignal(s, now);
        }

        static void Count(GirlfriendState s, GfNow now, bool hers)
        {
            s.totalMessages++;
            if (hers) s.herMessages++;
            if (s.firstMessageAt < 0) { s.firstMessageAt = now.game; s.firstMessageDay = now.day; }
        }

        public static void ColdSignal(GirlfriendState s, GfNow now)
        {
            s.lastColdSignalAt = Math.Max(1e-3, now.game);
            s.lastColdSignalDay = now.day;
        }

        static readonly string[] ColdWords = { "哦", "嗯", "随便", "哦哦", "嗯嗯", "噢", "好吧", "ok", "oh", "k", "fine", "whatever", "mm", "hm", "sure" };

        /// <summary>A whole reply that is only 「哦」「嗯」「随便」 (punctuation aside).</summary>
        public static bool ColdWord(string text)
        {
            string t = Letters(text);
            if (t.Length == 0) return false;
            foreach (var w in ColdWords) if (t == w) return true;
            return false;
        }

        static string Letters(string text)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in Chat.YYFaces.StripFaces(text ?? "")) if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        // ───────────── his lines ─────────────

        static readonly string[] Greetings = { "在吗", "在不在", "在干嘛", "在干嘛呢", "干嘛呢", "你在干嘛", "嗨", "hi", "hello", "hey", "在", "早", "早安", "晚上好", "吃了吗", "吃饭了吗", "你好", "areyouthere", "whatsup", "sup", "whatareyoudoing" };
        static readonly string[] Apologies = { "对不起", "我错了", "别生气", "抱歉", "原谅", "不好意思", "sorry", "apologi", "myfault", "forgive" };
        static readonly string[] Yes = { "好", "行", "可以", "嗯", "一定", "当然", "没问题", "必须", "好啊", "ok", "yes", "sure", "ofcourse", "deal", "yeah", "promise" };
        static readonly string[] No = { "不", "没空", "算了", "下次", "no", "can't", "cannot", "busy", "nextime" };
        static readonly string[] AiWords = { "ai", "人工智能", "程序", "灵光", "模型", "训练", "电脑帮", "机器" };

        public static bool IsGreeting(string text)
        {
            string t = Letters(text);
            if (t.Length == 0 || t.Length > 8) return false;
            foreach (var g in Greetings) if (t == Letters(g)) return true;
            return false;
        }

        static bool AnyIn(string text, string[] words)
        {
            string t = (text ?? "").ToLowerInvariant().Replace(" ", "");
            foreach (var w in words) if (t.Contains(w)) return true;
            return false;
        }

        public static bool Affirmative(string text) => !AnyIn(text, new[] { "不行", "不可以", "没空", "算了", "不了", "no", "can't", "busy" }) && AnyIn(text, Yes);

        /// <summary>
        /// He wrote to her (design §2): a quick answer lifts her mood, opening a topic after three quiet hours and
        /// bringing up something she said pay in affection, a holiday wish pays more if it came first, and an open
        /// question of hers (七夕 video, the film, 「你哪来的钱」, 「这不是你吧？」) takes his answer.
        /// </summary>
        public static GfTurn OnPlayerLine(GirlfriendState s, GfNow now, string text, bool byAi = false)
        {
            var turn = new GfTurn { byAi = byAi };
            if (s == null) return turn;
            turn.moodBefore = s.mood; turn.affectionBefore = s.affection;
            text = text ?? "";
            Count(s, now, false);
            if (s.linesDay != now.day) { s.linesDay = now.day; s.linesToday = 0; }
            s.linesToday++;
            turn.greetingOnly = IsGreeting(text);
            turn.apology = AnyIn(text, Apologies);

            if (s.awaitingReply && now.Story - s.herLastAt <= 5 * 60) { turn.quick = true; AddMood(s, 1); }
            if (!s.awaitingReply && (s.lastAnyAt <= 0 || now.Story - s.lastAnyAt >= 3 * Hour))
            {
                if (s.openersDay != now.day) { s.openersDay = now.day; s.openersToday = 0; }
                if (s.openersToday < 3) { s.openersToday++; AddAffection(s, 1); turn.opened = true; }
            }
            // Bringing up something she said: once per memory.
            for (int i = 0; i < s.memoryKeys.Count && i < s.memories.Count; i++)
            {
                string key = s.memoryKeys[i];
                if (key.Length < 2 || s.recalled.Contains(key) || !s.memories[i].StartsWith("她说", StringComparison.Ordinal)) continue;
                if (text.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0) continue;
                s.recalled.Add(key); AddAffection(s, 2); turn.recalled = key;
                break;
            }
            var h = HolidayOn(now.clock);
            if (h != null && h.wish.Length > 0 && !s.done.Contains("wish:" + h.key) && AnyIn(text, h.wish))
            {
                s.done.Add("wish:" + h.key);
                turn.wished = h;
                turn.wishedFirst = !s.done.Contains("hint:" + h.key);
                AddAffection(s, turn.wishedFirst ? h.first : h.late);
                AddMood(s, 1);
            }
            switch (s.asking)
            {
                case "qixi":
                case "movie":
                    if (Affirmative(text))
                    {
                        var due = s.asking == "qixi" ? HolidayOn(new DateTime(2016, 8, 9)).date : MovieDay;
                        s.promises.Add(new GfPromise { key = s.asking, dueDay = GameCalendar.DayIndex(due), zh = s.asking == "qixi" ? "七夕那天视频" : "12 月 2 日一起看《你的名字。》", en = s.asking == "qixi" ? "a video call on Qixi" : "watch Your Name together on 2 December" });
                        Remember(s, s.asking == "qixi" ? "视频" : "电影", s.asking == "qixi" ? "他答应七夕那天视频" : "他答应 12 月 2 日一起看《你的名字。》");
                        turn.promised = s.asking;
                    }
                    else { turn.declined = s.asking; AddMood(s, -1); }
                    s.asking = "";
                    break;
                case "weekend":
                    if (Affirmative(text))
                    {
                        Remember(s, "周末", "他答应这周末来看她");
                        AddAffection(s, 3); AddMood(s, 1);
                        turn.promised = "weekend";
                    }
                    else { turn.declined = "weekend"; AddMood(s, -1); }
                    s.asking = "";
                    break;
                case "money":
                    bool ai = AnyIn(text, AiWords);
                    turn.answered = ai ? "ai" : "work";
                    Mark(s, FlagMoneyAnswered);
                    if (ai) KnowAi(s, now);
                    s.asking = "";
                    break;
                case "caught":
                    bool admit = AnyIn(text, new[] { "是ai", "是 ai", "ai帮", "ai 帮", "代回", "承认", "对不起", "是它", "灵光", "program", "it was", "admit", "sorry" })
                        && !AnyIn(text, new[] { "不是ai", "不是 ai", "就是我", "没有", "哪有", "was me", "it's me" });
                    turn.answered = admit ? "admit" : "deny";
                    if (admit) { AddAffection(s, -5); KnowAi(s, now); s.flags.Remove(FlagDeniedAi); }
                    else { AddAffection(s, Has(s, FlagDeniedAi) && s.aiCaught > 1 ? -20 : -10); Mark(s, FlagDeniedAi); AddMood(s, -1); }
                    s.asking = "";
                    break;
                case "aiWorry":
                    s.asking = "";
                    break;
            }
            if (s.naggedDay == now.day && ++s.linesAfterNag == 3) AddMood(s, -1);

            s.awaitingReply = false; s.unansweredRun = 0; s.neglectLevel = 0;
            s.meLastAt = now.Story; s.meLastGame = now.game; s.lastAnyAt = now.Story;
            if (byAi) s.aiReplies++;
            if (s.mood <= -3 && !s.fighting) StartFight(s, now, turn.events);
            return turn;
        }

        static void KnowAi(GirlfriendState s, GfNow now)
        {
            if (Mark(s, FlagKnowsAi)) s.done.Add("knewAi@" + now.day);
        }

        /// <summary>
        /// The model's −2…+2 for his last line (design §2), clamped by what code knows: a greeting or small talk is 0,
        /// a quarrel without an apology gives nothing back, the cold tier gives at most +1. Then the mood scales it
        /// (a good mood takes praise better and slights lighter) and at most ±2 lands.
        /// </summary>
        public static int ApplyModelDelta(GirlfriendState s, GfTurn turn, int delta)
        {
            if (s == null) return 0;
            delta = Math.Max(-2, Math.Min(2, delta));
            if (turn != null && turn.greetingOnly) delta = Math.Min(Math.Max(delta, 0), 0);
            if (s.fighting && (turn == null || !turn.apology)) delta = Math.Min(delta, 0);
            if (Tier(s) == GirlfriendTier.Cold) delta = Math.Min(delta, 1);
            double factor = delta > 0 ? (s.mood >= 2 ? 1.5 : s.mood <= -2 ? .5 : 1) : (s.mood >= 2 ? .5 : s.mood <= -2 ? 1.5 : 1);
            double total = delta * factor + s.affectionCarry;
            int whole = (int)Math.Truncate(total);
            whole = Math.Max(-2, Math.Min(2, whole));
            s.affectionCarry = Math.Max(-1, Math.Min(1, total - whole));
            AddAffection(s, whole);
            if (delta > 0 && s.mood < 3 && delta == 2) AddMood(s, 1);
            if (delta < 0 && delta == -2) AddMood(s, -1);
            if (s.fighting && turn != null && turn.apology && delta >= 1) MakeUp(s, turn.events);
            else if (s.fighting && s.mood >= 1 && turn != null && turn.quick) MakeUp(s, turn.events);
            if (s.mood <= -3 && !s.fighting) StartFight(s, default(GfNow), turn != null ? turn.events : new List<GfEvent>());
            return whole;
        }

        public static void StartFight(GirlfriendState s, GfNow now, List<GfEvent> events)
        {
            if (s.fighting) return;
            s.fighting = true; s.fightSince = now.game; s.fightDay = s.today >= 0 ? s.today : now.day;
            if (Mark(s, FlagFought)) events.Add(GfEvent.Of(GfEventKind.Voice, "first.fight"));
        }

        public static void MakeUp(GirlfriendState s, List<GfEvent> events)
        {
            if (!s.fighting) return;
            s.fighting = false;
            s.mood = Math.Max(s.mood, 1);
            if (Mark(s, FlagMadeUp)) events.Add(GfEvent.Of(GfEventKind.Voice, "first.makeup"));
        }

        // ───────────── memories (design §3) ─────────────

        static readonly string[] Topics =
        {
            "概率论", "六级", "四级", "体测", "考试", "期末", "煤球", "口红", "星辰", "手环", "跑步", "减肥", "没电", "充电宝", "熊本熊", "阿狸",
            "奶茶", "手机壳", "欢乐颂", "微微一笑", "你的名字", "鼓浪屿", "厦门", "西湖", "杭州", "北京", "上海", "成都", "重庆", "迪士尼", "室友", "感冒", "发烧",
        };

        /// <summary>
        /// The rule pass over one of her messages: dates and exams, places, her pet, things she wants and places she
        /// wants to go. Returns the keyword it is recognised by and the text to keep (empty key: nothing).
        /// </summary>
        public static (string key, string text) ExtractMemory(string message)
        {
            string m = Chat.YYFaces.StripFaces(message ?? "").Trim();
            if (m.Length < 2) return ("", "");
            string text = m.Length > 30 ? m.Substring(0, 30) : m;
            foreach (var t in Topics) if (m.Contains(t)) return (t, text);
            foreach (var lead in new[] { "想要", "想去", "想吃", "叫" })
            {
                int i = m.IndexOf(lead, StringComparison.Ordinal);
                if (i < 0) continue;
                string rest = m.Substring(i + lead.Length);
                var sb = new System.Text.StringBuilder();
                foreach (char c in rest) { if (c == ' ' || char.IsPunctuation(c) || c == '的' || c == '了' || c == '啊' || c == '呀' || c == '吧' || sb.Length >= 6) break; sb.Append(c); }
                if (sb.Length >= 2) return (sb.ToString(), text);
            }
            string lower = m.ToLowerInvariant();
            foreach (var t in new[] { "exam", "probability", "cet", "lipstick", "cat", "running", "battery", "bubble tea", "phone case", "kumamon" })
                if (lower.Contains(t)) return (t, text);
            return ("", "");
        }

        /// <summary>Keeps a memory (at most 24, oldest dropped); a key already known only refreshes its text.</summary>
        public static void Remember(GirlfriendState s, string key, string text)
        {
            if (s == null || string.IsNullOrEmpty(text)) return;
            key = key ?? "";
            int at = key.Length > 0 ? s.memoryKeys.IndexOf(key) : -1;
            if (at >= 0 && at < s.memories.Count) { s.memories.RemoveAt(at); s.memoryKeys.RemoveAt(at); }
            if (s.memories.Contains(text)) return;
            s.memories.Add(text); s.memoryKeys.Add(key);
            while (s.memories.Count > MaxMemories) { s.memories.RemoveAt(0); if (s.memoryKeys.Count > 0) s.memoryKeys.RemoveAt(0); }
            while (s.memoryKeys.Count < s.memories.Count) s.memoryKeys.Insert(0, "");
        }

        /// <summary>The model's `remember` field: something about him worth keeping (his plans, his promises).</summary>
        public static void RememberHim(GirlfriendState s, string remember)
        {
            remember = (remember ?? "").Trim();
            if (remember.Length < 2 || remember.Length > 40) return;
            // It is about him: a line that talks about her own birthday or her own plans is the model confusing the two.
            if (remember.Contains("我的生日") || remember.StartsWith("我", StringComparison.Ordinal)) return;
            var m = ExtractMemory(remember);
            Remember(s, m.key, "他说：" + remember.TrimEnd('。', '.'));
        }

        public static bool HasMemory(GirlfriendState s, params string[] keys)
        {
            if (s == null) return false;
            foreach (var k in keys)
            {
                if (s.memoryKeys.Contains(k)) return true;
                foreach (var m in s.memories) if (m.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        // ───────────── signature (design §3) ─────────────

        public struct GfSignature { public string key, zh, en; public bool sad; }

        static readonly string[][] Signatures =
        {
            // key, zh, en, sad (1/0), band
            new[] { "cold1", "有些人，说了也不懂。", "Some people never get it, however you say it.", "1", "cold" },
            new[] { "cold2", "不想说话", "Don't feel like talking", "1", "cold" },
            new[] { "cold3", "算了", "Forget it", "1", "cold" },
            new[] { "dist1", "有点累", "A bit tired", "1", "distant" },
            new[] { "dist2", "随便吧", "Whatever", "1", "distant" },
            new[] { "dist3", "嗯", "Mm", "1", "distant" },
            new[] { "bad1", "心情不好 别惹我", "Bad mood, leave me alone", "1", "bad" },
            new[] { "bad2", "烦", "Annoyed", "1", "bad" },
            new[] { "norm1", "今天的月亮好圆", "The moon is so round tonight", "0", "normal" },
            new[] { "norm2", "好好学习 天天向上", "Study hard, improve every day", "0", "normal" },
            new[] { "norm3", "追剧中 勿扰", "Binge-watching, do not disturb", "0", "normal" },
            new[] { "warm1", "今天也要开心呀", "Be happy today too", "0", "warm" },
            new[] { "warm2", "嘿嘿", "Hehe", "0", "warm" },
            new[] { "warm3", "暑假余额不足", "Summer balance: running low", "0", "warmSummer" },
            new[] { "warm4", "概率论再见", "Goodbye, probability", "0", "warmTerm" },
            new[] { "sweet1", "有人在等我说晚安", "Someone's waiting for my good night", "0", "sweet" },
            new[] { "sweet2", "想见一个人", "I want to see someone", "0", "sweet" },
            new[] { "sweet3", "今天超开心！", "Super happy today!", "0", "sweet" },
        };

        /// <summary>The signature she would have now: her tier and mood pick a band, the day rotates within it; a forgotten day overrides.</summary>
        public static GfSignature PickSignature(GirlfriendState s, GfNow now)
        {
            if (s.sulkUntil >= now.day && s.sulkZh.Length > 0) return new GfSignature { key = "sulk:" + s.sulkZh, zh = s.sulkZh, en = s.sulkEn, sad = true };
            var tier = Tier(s);
            string band;
            if (tier == GirlfriendTier.Cold) band = "cold";
            else if (tier == GirlfriendTier.Distant) band = "distant";
            else if (s.mood <= -2 || s.fighting) band = "bad";
            else if (tier == GirlfriendTier.Normal) band = "normal";
            else if (tier == GirlfriendTier.Warm) band = "warm";
            else band = "sweet";
            var options = new List<string[]>();
            foreach (var row in Signatures)
                if (row[4] == band || (band == "warm" && row[4] == (Summer(now.clock) ? "warmSummer" : "warmTerm"))) options.Add(row);
            var pick = options[Math.Min(options.Count - 1, (int)(DayDie(s, now.day / 4, 11) * options.Count))];
            return new GfSignature { key = pick[0], zh = pick[1], en = pick[2], sad = pick[3] == "1" };
        }
    }
}
