using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Pure content and rolls behind the desk games, kept out of the views: the message banks, the sums, and how often
    /// LingGuang's purple hint is right (it follows the checkpoint's accuracy on the desk). Texts are { 中文, English }.
    /// </summary>
    public static class XgMiniContent
    {
        /// <summary>切短信 (垃圾短信 desk): spam to cut and ordinary messages to keep.</summary>
        public static readonly string[][] SpamMessages = {
            new[] { "中奖二等奖！先交手续费", "Second prize! Pay the fee first" },
            new[] { "高薪刷单，日结五百", "Easy money faking orders, 500 a day" },
            new[] { "我是儿子，急需转账到新号", "It's your son, new number, send money now" },
            new[] { "您的银行卡已冻结，点链接解冻", "Your bank card is frozen, tap to unfreeze" },
            new[] { "您被《我是歌手》抽中，请回复领奖", "You were picked by a TV show, reply to claim" },
            new[] { "低息贷款无需抵押，加微信", "No-collateral loan, add me on WeChat" },
        };
        public static readonly string[][] NormalMessages = {
            new[] { "明天下午会议改到四点", "Tomorrow's meeting moved to 4 pm" },
            new[] { "快递已放门卫", "Parcel left with the doorman" },
            new[] { "记得带伞，晚上有雨", "Bring an umbrella, rain tonight" },
            new[] { "晚上我做饭，早点回来", "I'm cooking tonight, come home early" },
            new[] { "房租已收到，谢谢", "Rent received, thanks" },
            new[] { "生日快乐", "Happy birthday" },
        };

        /// <summary>左滑右滑 (标题党 desk): the first five are clickbait, the rest honest headlines.</summary>
        public const int ClickbaitCount = 5;
        public static readonly string[][] Headlines = {
            new[] { "震惊！喝一杯这个，医生都惊呆了", "Shocking! One cup of this left the doctors speechless" },
            new[] { "不转不是中国人！今晚必须转发", "Don't share and you're no patriot! Forward tonight" },
            new[] { "一个动作年轻十岁，九成人不知道", "One move to look ten years younger, 90% don't know it" },
            new[] { "速看！所有银行卡明天全部作废", "Read now! Every bank card is void tomorrow" },
            new[] { "男人沉默女人流泪的惊天秘密", "The secret that left men silent and women in tears" },
            new[] { "小区路灯故障，街道办次日完成修复", "Street lamp out in the estate, repaired next day by the district office" },
            new[] { "北京地铁早高峰增开列车", "Beijing metro adds trains at the morning rush" },
            new[] { "天宫二号空间实验室发射成功", "Tiangong-2 space lab launched successfully" },
            new[] { "铁路部门公布春运增开列车安排", "Railways publish extra trains for the Spring Festival rush" },
            new[] { "中国女排夺得里约奥运会金牌", "China's women's volleyball team wins Rio Olympic gold" },
        };

        /// <summary>One mole of 打地鼠 (算术 desk): a sum on show, which may carry a wrong answer.</summary>
        public struct Mole
        {
            /// <summary>The sum up to and including "=", the answer it shows and the true one; <see cref="Wrong"/> when they differ.</summary>
            public string Prefix;
            public int Shown, Truth;
            public bool Wrong;
            /// <summary>The mole wears LingGuang's purple hat: its answer is the checkpoint's own, wrong with the checkpoint's error rate.</summary>
            public bool Ai;
            public string TruthText => Prefix + Truth;
            public string ShownText => Prefix + Shown;
        }

        /// <summary>Rolls the next mole: one in four wears the hat, otherwise half the sums are wrong.</summary>
        public static Mole NextMole(Random random, double skill)
        {
            int op = random.Next(3), a = random.Next(3, 35), b = random.Next(2, 13);
            int truth = XgMiniActionRules.Arithmetic(a, b, op);
            bool ai = random.Next(4) == 0;
            bool wrong = ai ? random.NextDouble() >= skill : random.Next(2) == 0;
            int shown = truth + (wrong ? (random.Next(2) == 0 ? -1 : 1) * random.Next(1, 4) : 0);
            return new Mole { Prefix = a + (op == 0 ? "+" : op == 1 ? "−" : "×") + b + "=", Shown = shown, Truth = truth, Wrong = wrong, Ai = ai };
        }

        /// <summary>The 开心辞典 ladder on the 常识判断 desk: twelve questions, the nursery first, then school facts, then a little more.</summary>
        public const int SenseLadder = 12;

        /// <summary>The statement-bank level of ladder step <paramref name="step"/> (0-based): 1 for the first four, 2 for the next four, 3 after that.</summary>
        public static int SenseLevel(int step) { return 1 + Math.Max(0, Math.Min(2, step / 4)); }

        /// <summary>
        /// The next true/false statement of the 开心辞典 ladder, straight from the 常识判断 bank (<see cref="XgMemes.SenseBank"/>) and
        /// dated like the desk's own cards (<paramref name="today"/> is a yyyymmdd). Never one already in <paramref name="used"/>;
        /// once the level's statements run dry it relaxes to any statement not used yet.
        /// </summary>
        public static XgPhrase SenseStatement(Random random, int step, int today, ICollection<string> used)
        {
            int level = SenseLevel(step);
            for (int attempt = 0; attempt < 60; attempt++)
            {
                var p = XgMemes.Pick(XgMemes.SenseDesk, random, level, today, "", false);
                if (p != null && !used.Contains(p.text)) return p;
            }
            var rest = new List<XgPhrase>();
            foreach (var p in XgMemes.SenseBank) if (p.since <= today && !used.Contains(p.text)) rest.Add(p);
            if (rest.Count == 0) { used.Clear(); return XgMemes.Pick(XgMemes.SenseDesk, random, level, today, "", false); }
            return rest[random.Next(rest.Count)];
        }

        /// <summary>LingGuang's purple opinion about a yes/no item: right with the checkpoint's accuracy on the desk.</summary>
        public static bool Opinion(Random random, double skill, bool truth) { return random.NextDouble() < skill ? truth : !truth; }

        /// <summary>LingGuang's pick among <paramref name="count"/> options: the true one with the checkpoint's accuracy, otherwise another.</summary>
        public static int Guess(Random random, double skill, int truth, int count)
        {
            return random.NextDouble() < skill ? truth : (truth + 1 + random.Next(Math.Max(1, count - 1))) % count;
        }
    }
}
