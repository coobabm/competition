using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>What the protagonist is living through at the moment of a hand label (filled by the desktop).</summary>
    public sealed class XgMonologueContext
    {
        /// <summary>Lifetime hand answers (right and wrong).</summary>
        public int handLabels;
        /// <summary>The desk just labelled and how many labels it holds.</summary>
        public string desk = "";
        public double deskLabels;
        /// <summary>The desktop clock (hours 0–23).</summary>
        public int hour = 12, minute;
        /// <summary>¥ on hand and the electricity bill that is coming; unpaid = the power is already cut.</summary>
        public double money, nextBill;
        public bool unpaid;
        /// <summary>Right answers in a row (the shared combo).</summary>
        public int streak;
    }

    /// <summary>One line of inner monologue. Templates use {time}, {n}, {noun}, {total}, {money}, {gap}, {streak}.</summary>
    public sealed class XgMonologueLine
    {
        public string id, pool, zh, en;
        /// <summary>Only on this desk (empty = any desk).</summary>
        public string desk = "";
        /// <summary>Only while the power is cut for an unpaid bill.</summary>
        public bool unpaidOnly;
    }

    /// <summary>
    /// The protagonist's thoughts while hand-labelling before they have the auto-labelling idea (act 1 pressure).
    /// Pure rules: the desktop calls <see cref="Next"/> after each hand label and shows what it returns through
    /// InnerVoice. At most one line per <see cref="LabelGap"/> hand labels and <see cref="SecondsGap"/> seconds
    /// (both must pass), chosen by context (late night, money short of the next bill, a long streak, fatigue),
    /// preferring a different context from the last line and never repeating the previous line.
    /// </summary>
    public sealed class XgInnerMonologue
    {
        public const int LabelGap = 40;
        public const double SecondsGap = 180;
        /// <summary>The first line may come this soon after the first hand label of the session (and 40 labels).</summary>
        public const double FirstSeconds = 60;
        public const int FatigueFrom = 60, StreakFrom = 20;

        public const string Night = "night", Money = "money", Streak = "streak", Fatigue = "fatigue";

        static XgMonologueLine L(string pool, string id, string zh, string en, string desk = "", bool unpaid = false)
            => new XgMonologueLine { pool = pool, id = id, zh = zh, en = en, desk = desk, unpaidOnly = unpaid };

        public static readonly XgMonologueLine[] Lines =
        {
            L(Night, "night.work", "{time} 了，明天还要上班。", "{time} already. Work tomorrow."),
            L(Night, "night.ten", "{time}。再标十个就睡。……上次也是这么说的。", "{time}. Ten more and then bed. … I said that last time too."),
            L(Night, "night.bbq", "楼下烧烤摊都收摊了，我还在点「是」「否」。", "Even the barbecue stall downstairs has packed up, and I'm still clicking Yes and No."),
            L(Night, "night.window", "这个点，整栋楼就我这扇窗还亮着吧。", "At this hour mine must be the only lit window in the building."),

            L(Money, "money.short", "电费单快来了，这点钱不够。", "The power bill is coming, and this won't cover it."),
            L(Money, "money.gap", "¥{money}……还差 ¥{gap} 才够交电费。", "¥{money}… still ¥{gap} short of the power bill."),
            L(Money, "money.rate", "一题几毛钱，电费一度五毛二。", "A few mao a card, and power costs 0.52 a unit."),
            L(Money, "money.gpu", "标一晚上，刚够显卡自己的电费。", "A whole night of labelling barely pays for the GPU's own electricity."),
            L(Money, "money.cut", "停电了还得接着标，标的钱先抵电费。", "The power is off and I still have to label; the money goes to the bill first.", unpaid: true),

            L(Streak, "streak.fast", "连着对了 {streak} 个，手比脑子快了。", "{streak} right in a row. My hand is faster than my head now."),
            L(Streak, "streak.eyes", "我现在闭着眼都能标。", "I could do this with my eyes closed."),
            L(Streak, "streak.machine", "对、对、对……我是不是也成了台机器。", "Right, right, right… am I turning into a machine too?"),

            L(Fatigue, "fatigue.wrist", "手腕好酸。", "My wrist aches."),
            L(Fatigue, "fatigue.count", "这已经是第 {n} {noun}了。", "That's {n} {noun} now."),
            L(Fatigue, "fatigue.button", "鼠标左键快被我按出包浆了。", "The left mouse button is getting a polish from all this clicking."),
            L(Fatigue, "fatigue.blur", "眼睛有点花。是、否、是、否……", "My eyes are swimming. Yes, no, yes, no…"),
            L(Fatigue, "fatigue.total", "标了 {total} 条了，一条才几毛。", "{total} cards so far, a few mao each."),
            L(Fatigue, "fatigue.hand", "要是能多长一只手就好了。", "If only I had a third hand."),
            L(Fatigue, "fatigue.sevens", "闭上眼全是 7。", "I close my eyes and see sevens.", desk: "mnist"),
        };

        /// <summary>A desk's count word: 「第 300 条短信」, "300 texts".</summary>
        public static void Noun(string desk, out string zh, out string en)
        {
            switch (desk)
            {
                case "spam": zh = "条短信"; en = "texts"; return;
                case "danmu": zh = "条弹幕"; en = "danmaku comments"; return;
                case "logic": zh = "道判断题"; en = "logic questions"; return;
                case "arith": zh = "道算术题"; en = "sums"; return;
                case "sense": zh = "条常识"; en = "common-sense cards"; return;
                case "mnist": zh = "个 7 "; en = "sevens"; return; // every digit looks like a 7 by now (the space keeps 「7 了」 apart)
                case "poems": zh = "句诗"; en = "lines of poetry"; return;
                case "meme": zh = "张表情包"; en = "memes"; return;
                case "headline": zh = "条标题"; en = "headlines"; return;
                case "review": zh = "条评论"; en = "reviews"; return;
                case "cifar": zh = "张验证码"; en = "captchas"; return;
                case "translate": zh = "句中式英语"; en = "Chinglish lines"; return;
                case "go": zh = "盘棋"; en = "Go boards"; return;
                case "longtext": zh = "段长句"; en = "long sentences"; return;
                case "crosssentence": zh = "段话"; en = "passages"; return;
                default: zh = "道题"; en = "cards"; return;
            }
        }

        readonly Random rng;
        bool primed;
        int lastLabels;
        double lastTime;
        string lastId = "", lastPool = "";

        public XgInnerMonologue(int seed = 0) { rng = seed == 0 ? new Random() : new Random(seed); }

        public string LastId => lastId;
        public string LastPool => lastPool;

        public static bool IsNight(int hour) => hour < 5 || hour >= 23;
        public static bool MoneyShort(XgMonologueContext c) => c.unpaid || c.nextBill > 0 && c.money < c.nextBill;

        /// <summary>The contexts that apply now.</summary>
        public static List<string> Pools(XgMonologueContext c)
        {
            var pools = new List<string>();
            if (c == null) return pools;
            if (IsNight(c.hour)) pools.Add(Night);
            if (MoneyShort(c)) pools.Add(Money);
            if (c.streak >= StreakFrom) pools.Add(Streak);
            if (c.handLabels >= FatigueFrom) pools.Add(Fatigue);
            return pools;
        }

        /// <summary>
        /// Call after each hand label. Returns a filled line (zh and en) or null when it is too soon or nothing applies.
        /// The first call only starts the clock. <paramref name="now"/> is any steadily rising time in seconds.
        /// </summary>
        public XgMonologueLine Next(XgMonologueContext c, double now)
        {
            if (c == null || double.IsNaN(now) || double.IsInfinity(now)) return null;
            if (!primed) { primed = true; lastLabels = c.handLabels; lastTime = now - SecondsGap + FirstSeconds; return null; }
            if (c.handLabels < lastLabels) lastLabels = c.handLabels; // another save was loaded
            if (c.handLabels - lastLabels < LabelGap || now - lastTime < SecondsGap) return null;
            var pools = Pools(c);
            if (pools.Count == 0) return null;
            // A different context from last time when there is one, otherwise the same pool with another line.
            var fresh = pools.FindAll(p => p != lastPool);
            var order = new List<string>();
            if (fresh.Count > 0) order.Add(fresh[rng.Next(fresh.Count)]);
            foreach (var p in pools) if (!order.Contains(p)) order.Add(p);
            foreach (var pool in order)
            {
                var options = new List<XgMonologueLine>();
                foreach (var line in Lines)
                    if (line.pool == pool && line.id != lastId && Eligible(line, c)) options.Add(line);
                if (options.Count == 0) continue;
                var pick = options[rng.Next(options.Count)];
                lastId = pick.id; lastPool = pool; lastLabels = c.handLabels; lastTime = now;
                return Fill(pick, c);
            }
            return null;
        }

        static bool Eligible(XgMonologueLine line, XgMonologueContext c)
        {
            if (line.desk.Length > 0 && line.desk != c.desk) return false;
            if (line.unpaidOnly && !c.unpaid) return false;
            if (line.id == "fatigue.count" && c.deskLabels < 20) return false;
            if (line.id == "money.gap" && !(c.nextBill > c.money + .5)) return false;
            return true;
        }

        /// <summary>The line with its placeholders filled for this moment.</summary>
        public static XgMonologueLine Fill(XgMonologueLine line, XgMonologueContext c)
        {
            Noun(c.desk, out string nounZh, out string nounEn);
            string time = Math.Max(0, Math.Min(23, c.hour)).ToString(CultureInfo.InvariantCulture) + ":" + Math.Max(0, Math.Min(59, c.minute)).ToString("00", CultureInfo.InvariantCulture);
            string Num(double v) => (double.IsNaN(v) || double.IsInfinity(v) ? 0 : Math.Max(0, v)).ToString("0", CultureInfo.InvariantCulture);
            string Apply(string s, string noun) => s.Replace("{time}", time).Replace("{n}", Num(c.deskLabels)).Replace("{noun}", noun)
                .Replace("{total}", Num(c.handLabels)).Replace("{money}", Num(c.money)).Replace("{gap}", Num(c.nextBill - c.money)).Replace("{streak}", Num(c.streak));
            return new XgMonologueLine { id = line.id, pool = line.pool, desk = line.desk, unpaidOnly = line.unpaidOnly, zh = Apply(line.zh, nounZh), en = Apply(line.en, nounEn) };
        }
    }
}
