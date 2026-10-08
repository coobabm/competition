using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Learned tone habits (TAMER-style): a linear model H(s, a) of the 赞 / 踩 the owner would give tone a in
    /// situation s. Weights are stored as one flat list, row-major [action, feature].
    /// </summary>
    [Serializable]
    public sealed class XgHabits
    {
        public List<float> w = new List<float>();
        public int picks, explored, rated;
        public long rng;
    }

    public sealed partial class XgState
    {
        public XgHabits habits = new XgHabits();
    }

    /// <summary>
    /// 语气习惯 (stage 4+ on the 对话 page). Before each model reply it picks one of six tones from the three
    /// personality axes: argmax of H(s, a) plus a small prior from the tone board, or a random one with chance
    /// <see cref="HabitExplore"/>. The pick goes into the prompt for that turn, and 赞 / 踩 on the reply updates the
    /// row of that tone (normalised LMS), so the same situation gets a different tone next time.
    /// The tone board (<see cref="RateReply"/>) still learns who it is overall; this learns when to be which.
    /// </summary>
    public sealed partial class XgSim
    {
        public const double HabitExplore = .15, HabitRate = .6, HabitPrior = .3;
        public const float HabitClamp = 2f;

        /// <summary>The six tones, in pairs per axis: high, low. Each maps to a 语气 element of the tone board.</summary>
        public static readonly string[] HabitTones = { "热情", "冷静", "皮", "正经", "有主见", "顺从" };
        public static readonly string[] HabitTonesEn = { "warm", "calm", "cheeky", "serious", "opinionated", "agreeable" };

        /// <summary>What it notices about the situation. Feature 0 is always on (the tone's base lean).</summary>
        public static readonly string[] HabitFeatures = { "平时", "你在问", "你难过", "你开心", "你叫它做事", "你话很短", "你话很长", "深夜", "刚被踩", "刚被赞", "问它自己" };
        public static readonly string[] HabitFeaturesEn = { "always", "a question", "you're down", "you're happy", "a request", "very short", "very long", "late night", "just got 踩", "just got 赞", "about itself" };

        static readonly string[] AskWords = { "？", "?", "吗", "呢", "什么", "怎么", "为什么", "哪", "谁", "多少", "what", "why", "how", "who" };
        static readonly string[] SadWords = { "难过", "累", "烦", "哭", "不开心", "郁闷", "唉", "困", "伤心", "失败", "挂了", "sad", "tired", "upset" };
        static readonly string[] HappyWords = { "哈哈", "开心", "耶", "棒", "666", "好玩", "233", "嘿嘿", "高兴", "太好了", "lol", "haha", "yay" };
        static readonly string[] AskDoWords = { "帮我", "给我", "你去", "快点", "必须", "你得", "请你", "替我", "help me", "please", "go and" };
        static readonly string[] SelfWords = { "你是", "你会", "你喜欢", "你觉得", "你怎么看", "你想", "你自己", "are you", "do you", "you think" };

        /// <summary>The tone picked for the reply being generated: action and situation bits, -1 when none.</summary>
        int habitAct = -1, habitCtx;
        bool habitExplored;

        public int HabitActions => HabitTones.Length;
        public int HabitWidth => HabitFeatures.Length;

        XgHabits Habits
        {
            get
            {
                if (S.habits == null) S.habits = new XgHabits();
                int size = HabitActions * HabitWidth;
                if (S.habits.w == null) S.habits.w = new List<float>();
                while (S.habits.w.Count < size) S.habits.w.Add(0);
                if (S.habits.w.Count > size) S.habits.w.RemoveRange(size, S.habits.w.Count - size);
                return S.habits;
            }
        }

        public float HabitWeight(int action, int feature) => Habits.w[action * HabitWidth + feature];
        public int HabitPicks => Habits.picks;
        public int HabitRatings => Habits.rated;

        /// <summary>The situation bits for what the owner just said.</summary>
        public int HabitContext(string question, DateTime? now = null)
        {
            string q = question ?? "";
            int bits = 1;
            if (Has(q, AskWords)) bits |= 1 << 1;
            if (Has(q, SadWords)) bits |= 1 << 2;
            if (Has(q, HappyWords)) bits |= 1 << 3;
            if (Has(q, AskDoWords)) bits |= 1 << 4;
            int length = q.Trim().Length;
            if (length > 0 && length <= 4) bits |= 1 << 5;
            if (length >= 20) bits |= 1 << 6;
            if (now.HasValue && (now.Value.Hour >= 23 || now.Value.Hour < 5)) bits |= 1 << 7;
            int rated = LastRating();
            if (rated < 0) bits |= 1 << 8;
            if (rated > 0) bits |= 1 << 9;
            if (Has(q, SelfWords)) bits |= 1 << 10;
            return bits;
        }

        static bool Has(string text, string[] words)
        {
            foreach (var w in words) if (text.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>H(s, a): the 赞 (+1) or 踩 (−1) it expects for tone <paramref name="action"/> in situation <paramref name="ctx"/>.</summary>
        public double HabitValue(int ctx, int action)
        {
            double sum = 0;
            for (int i = 0; i < HabitWidth; i++) if ((ctx & (1 << i)) != 0) sum += HabitWeight(action, i);
            return sum;
        }

        /// <summary>The tone board's current lean towards this tone, in [−<see cref="HabitPrior"/>, +<see cref="HabitPrior"/>].</summary>
        public double HabitPriorFor(int action)
        {
            double lean = (ActualAxis(action / 2) - 50) / 50;
            return HabitPrior * (action % 2 == 0 ? lean : -lean);
        }

        public int HabitGreedy(int ctx)
        {
            int best = 0; double score = double.MinValue;
            for (int a = 0; a < HabitActions; a++)
            {
                double s = HabitValue(ctx, a) + HabitPriorFor(a);
                if (s > score + 1e-9) { score = s; best = a; }
            }
            return best;
        }

        /// <summary>Picks this reply's tone (stage 4+). Returns the action, or -1 when speech is still too limited for tone.</summary>
        public int PickHabit(string question, DateTime? now = null)
        {
            habitAct = -1;
            if (SpeechStage < 4) return -1;
            var h = Habits;
            habitCtx = HabitContext(question, now);
            habitExplored = HabitRoll(h) < HabitExplore;
            habitAct = habitExplored ? (int)(HabitRoll(h) * HabitActions) % HabitActions : HabitGreedy(habitCtx);
            h.picks++;
            if (habitExplored) h.explored++;
            return habitAct;
        }

        double HabitRoll(XgHabits h)
        {
            long x = h.rng == 0 ? 0x7AE3E2L : h.rng;
            x ^= x << 13; x ^= (long)((ulong)x >> 7); x ^= x << 17;
            h.rng = x == 0 ? 0x7AE3E2L : x;
            return (double)((ulong)h.rng >> 11) / (1UL << 53);
        }

        /// <summary>The per-turn prompt line for the picked tone, empty when none.</summary>
        public string HabitOrder()
        {
            if (habitAct < 0) return "";
            return T("这一句用「" + HabitTones[habitAct] + "」的语气说。", "Say this one in a " + HabitTonesEn[habitAct] + " tone.");
        }

        /// <summary>
        /// Attaches the picked tone to the reply just added at <paramref name="index"/> and clears the pick. Only a model
        /// reply carries it: an offline line was not written in that tone, so rating it must not teach the habit.
        /// </summary>
        public void TagHabit(int index, bool fromModel)
        {
            if (habitAct >= 0 && fromModel && index >= 0 && index < S.chat.Count && S.chat[index].from == "ai")
            {
                S.chat[index].tone = habitAct + 1;
                S.chat[index].ctx = habitCtx;
                S.chat[index].explored = habitExplored;
            }
            habitAct = -1;
        }

        /// <summary>The TAMER update for a rated line: move H(s, a) towards the rating, spread over the active features.</summary>
        void LearnHabit(XgChatLine line, bool up)
        {
            if (line.tone <= 0 || line.tone > HabitActions) return;
            var h = Habits;
            int a = line.tone - 1, ctx = line.ctx, active = 0;
            for (int i = 0; i < HabitWidth; i++) if ((ctx & (1 << i)) != 0) active++;
            if (active == 0) return;
            double err = (up ? 1 : -1) - HabitValue(ctx, a);
            float step = (float)(HabitRate * err / active);
            for (int i = 0; i < HabitWidth; i++)
            {
                if ((ctx & (1 << i)) == 0) continue;
                int k = a * HabitWidth + i;
                h.w[k] = Math.Max(-HabitClamp, Math.Min(HabitClamp, h.w[k] + step));
            }
            h.rated++;
        }

        /// <summary>Why a line had its tone: "试试看" for exploration, else the situation that pulled hardest.</summary>
        public string HabitWhy(XgChatLine line)
        {
            if (line == null || line.tone <= 0 || line.tone > HabitActions) return "";
            int a = line.tone - 1;
            string tone = T(HabitTones[a], HabitTonesEn[a]);
            if (line.explored) return T("「" + tone + "」· 试试看", tone + " · trying it out");
            int best = -1; float pull = 0;
            for (int i = 0; i < HabitWidth; i++)
                if ((line.ctx & (1 << i)) != 0 && Math.Abs(HabitWeight(a, i)) > Math.Abs(pull)) { pull = HabitWeight(a, i); best = i; }
            if (best < 0 || Math.Abs(pull) < .05f) return T("「" + tone + "」· 还没学到，按性格来", tone + " · nothing learned yet, by personality");
            return T("「" + tone + "」· 因为" + HabitFeatures[best], tone + " · because " + HabitFeaturesEn[best]);
        }
    }
}
