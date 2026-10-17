using System;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>One answer the player can pick in a beat: what it does to her, and what she says back (zh, en pairs).</summary>
    public sealed class GfArcChoice
    {
        public string zh = "", en = "";
        public int affection;
        public string flag = "";
        public string[] reply = new string[0];
        /// <summary>What she remembers of it (her memory list, read by her prompt later). Empty = nothing.</summary>
        public string memoryZh = "";
    }

    /// <summary>One conversation of the side story: when it comes, what she says, and the answers on offer.</summary>
    public sealed class GfArcBeat
    {
        public string key = "";
        /// <summary>The lab has to be this far along (stage, abilities learned, rounds trained).</summary>
        public int stage, abilities, epochs;
        public string[] lines = new string[0];
        /// <summary>Extra lines (zh, en pairs) once he has told her he is teaching a computer (<see cref="QingwenArc.FlagToldMl"/>).</summary>
        public string[] toldMlLines = new string[0];
        public GfArcChoice[] choices = new GfArcChoice[0];
        /// <summary>For the model when he types something else: what she just asked him about.</summary>
        public string askedZh = "", askedEn = "";
    }

    /// <summary>
    /// 晴雯的支线 (2026-10-18): six conversations in YY that come with the lab's progress, not with her schedule, so
    /// she is there even when the desktop clock says she is asleep or out. Each one offers three answers as buttons;
    /// they move her affection and set flags later beats read. Anything else he types goes to her model with what she
    /// just asked (<see cref="FreeSituation"/>). She never learns what the program is: he can only say he is teaching
    /// a computer. Pure; YYGirlfriend.Arc.cs plays it.
    /// </summary>
    public static class QingwenArc
    {
        /// <summary>Seconds of play between two beats.</summary>
        public const double Gap = 100;
        /// <summary>Seconds she waits for his answer before the beat closes unanswered.</summary>
        public const double OpenFor = 300;
        public const int UnansweredCost = 2;
        public const string FlagToldMl = "arc.toldMl", FlagPromisedHome = "arc.home", FlagForgot = "arc.forgot", FlagRemembered = "arc.remembered", FlagPromisedFuture = "arc.future";
        const string DonePrefix = "arc:";

        static GfArcChoice C(string zh, string en, int affection, string memoryZh, string flag, params string[] reply)
            => new GfArcChoice { zh = zh, en = en, affection = affection, memoryZh = memoryZh ?? "", flag = flag ?? "", reply = reply };

        public static readonly GfArcBeat[] Beats =
        {
            new GfArcBeat
            {
                key = "hello",
                lines = new[] { "你是不是又在弄电脑", "Let me guess. The computer again" },
                askedZh = "你问他是不是又在弄电脑。", askedEn = "You asked whether he is at the computer again.",
                choices = new[]
                {
                    C("嗯，还在弄电脑", "Yeah, still on the computer", 1, null, null,
                        "就知道[白眼]", "Knew it [白眼]", "弄完早点睡 别又通宵", "Go to bed when you're done. No all-nighter"),
                    C("想你了，睡不着", "Missed you. Can't sleep", 3, "他说想我想得睡不着", null,
                        "油嘴滑舌[偷笑]", "Smooth talker [偷笑]", "……我也是", "…Me too"),
                    C("在忙，晚点说", "Busy, talk later", -2, null, null,
                        "哦", "Oh", "那你忙吧", "Go on then"),
                },
            },
            new GfArcBeat
            {
                key = "busy", epochs = 3,
                lines = new[] { "你最近天天在电脑前 到底在弄啥", "You're at that computer every day. What are you doing?", "室友都问我你是不是在挖矿[疑问]", "My roommates think you're mining bitcoin [疑问]" },
                askedZh = "你问他最近天天在电脑前弄什么。", askedEn = "You asked what he does at the computer every day.",
                choices = new[]
                {
                    C("在自学机器学习，教电脑认字", "Teaching myself machine learning. Teaching a computer to read", 2, "他在自学机器学习，教电脑认字", FlagToldMl,
                        "认字？？", "Read??", "电脑还要你教？", "Computers need you to teach them?", "……听不懂 但感觉好厉害的样子", "…No idea what that means, but it sounds impressive", "教会了给我看看", "Show me when it learns"),
                    C("打游戏呢", "Gaming", -1, null, null,
                        "又是英雄联盟[白眼]", "League again [白眼]", "你上次说戒了的", "You said you'd quit"),
                    C("秘密，以后告诉你", "Secret. I'll tell you later", 0, null, null,
                        "神神秘秘的", "So mysterious", "行吧 我等着", "Fine. I'll wait"),
                },
            },
            new GfArcBeat
            {
                key = "home", stage = 2,
                lines = new[] { "我妈问我你放假回不回老家", "My mum asked if you're going home for the holidays", "我说你在弄什么大项目[偷笑]", "I told her you're on some big project [偷笑]", "你回吗", "Are you?" },
                askedZh = "你问他放假回不回老家。", askedEn = "You asked whether he is going home for the holidays.",
                choices = new[]
                {
                    C("回，一定回", "Yes. Definitely", 2, "他答应放假回老家", FlagPromisedHome,
                        "说好了啊", "That's a promise", "拉钩[害羞]", "Pinky swear [害羞]"),
                    C("这阵子走不开，等忙完这段", "Can't get away yet. After this bit", -1, null, null,
                        "又是忙完这段", "\"After this bit\" again", "你每次都这么说", "You always say that"),
                    C("看情况吧", "We'll see", -1, null, null,
                        "嗯", "Mm"),
                },
            },
            new GfArcBeat
            {
                key = "exam", stage = 3,
                lines = new[] { "完了", "I'm done for", "中级财务会计我感觉要挂", "I think I failed Intermediate Accounting", "合并报表那道大题我只写了一半[大哭]", "I only did half the consolidated statements question [大哭]" },
                askedZh = "你说你中级财务会计可能要挂，合并报表那题只写了一半，想要他安慰。", askedEn = "You told him you may have failed Intermediate Accounting and wanted comfort.",
                choices = new[]
                {
                    C("别慌，成绩出来再说", "Don't panic. Wait for the results", 1, null, null,
                        "你说得轻巧", "Easy for you to say", "……嗯 你说得也对", "…Mm. You're right though"),
                    C("挂了就补考呗，没事", "If you fail, you resit. No big deal", -2, null, null,
                        "你就不能盼我点好吗[生气]", "Could you at least hope I pass [生气]"),
                    C("今晚视频，我陪你复习", "Video call tonight. I'll study with you", 3, "考试前他陪我视频复习", null,
                        "你又不会会计", "You don't even know accounting", "……那十点 不许迟到", "…Ten o'clock then. Don't be late"),
                },
            },
            new GfArcBeat
            {
                key = "day", stage = 4,
                lines = new[] { "今天是什么日子 你知道吗", "Do you know what day it is today?" },
                askedZh = "你问他今天是什么日子（你们在一起两周年）。", askedEn = "You asked him what day it is (your second anniversary).",
                choices = new[]
                {
                    C("我们在一起两周年", "Our second anniversary", 3, "两周年那天他记得", FlagRemembered,
                        "算你有良心[玫瑰]", "So you do have a heart [玫瑰]", "礼物呢？", "And my present?"),
                    C("……周三？", "…Wednesday?", -4, "两周年那天他忘了", FlagForgot,
                        "林晴雯现在不想说话", "Lin Qingwen does not wish to speak right now"),
                    C("你生日？", "Your birthday?", -3, null, FlagForgot,
                        "我生日是九月！！", "My birthday is in September!!", "你再好好想想", "Think harder"),
                },
            },
            new GfArcBeat
            {
                key = "future", stage = 5,
                lines = new[] { "我室友问我", "My roommate asked me", "你男朋友整天对着电脑 你们以后怎么办", "\"Your boyfriend's glued to a computer. What's your future?\"", "我没答上来", "I didn't know what to say", "你说呢", "What would you say?" },
                toldMlLines = new[] { "你那个教电脑认字的 教会了没", "And your computer that's learning to read. Has it learned?" },
                askedZh = "你问他：他整天对着电脑，你们以后怎么办。", askedEn = "You asked him what your future is, with him always at the computer.",
                choices = new[]
                {
                    C("等我做出点名堂，就去你那边", "Once this works out, I'm moving to your city", 3, "他说做出名堂就来我这边", FlagPromisedFuture,
                        "你说的哦[爱心]", "You said it [爱心]", "我截图了", "I took a screenshot"),
                    C("还没想好", "I haven't figured it out", -1, null, null,
                        "……嗯", "…Mm", "我也是", "Me neither"),
                    C("别听她们瞎说", "Don't listen to them", 0, null, null,
                        "我没听她们的", "I'm not listening to them", "我是在问你", "I'm asking you"),
                },
            },
        };

        public static GfArcBeat Beat(string key) => Array.Find(Beats, b => b.key == key);

        public static bool Done(GirlfriendState s, GfArcBeat b) => s != null && b != null && s.done != null && s.done.Contains(DonePrefix + b.key);

        /// <summary>The beat waiting for his answer, if any.</summary>
        public static GfArcBeat Open(GirlfriendState s) => s == null || string.IsNullOrEmpty(s.arcOpen) ? null : Beat(s.arcOpen);

        /// <summary>The next beat due now: the lab far enough, none open, the last one <see cref="Gap"/> seconds back.</summary>
        public static GfArcBeat Due(GirlfriendState s, int stage, int abilities, int epochs, double game)
        {
            if (s == null || !s.started || !string.IsNullOrEmpty(s.arcOpen)) return null;
            if (s.arcAt > 0 && game - s.arcAt < Gap) return null;
            foreach (var b in Beats)
            {
                if (Done(s, b)) continue;
                if (stage < b.stage || abilities < b.abilities || epochs < b.epochs) return null; // in order
                return b;
            }
            return null;
        }

        /// <summary>
        /// Her lines for a beat (zh, en pairs). The first one opens with her hello for the time of day; the later ones
        /// add the toldMl lines when he told her what he is doing.
        /// </summary>
        public static string[] Lines(GirlfriendState s, GfArcBeat b, int hour, int minute)
        {
            var lines = b.lines;
            if (b.key == "hello" && !s.done.Contains("life:firstNight")) lines = Concat(GirlfriendRules.FirstHello(hour, minute), lines);
            if (b.toldMlLines.Length > 0 && GirlfriendRules.Has(s, FlagToldMl)) lines = Concat(lines, b.toldMlLines);
            return lines;
        }

        static string[] Concat(string[] a, string[] b)
        {
            var r = new string[a.Length + b.Length];
            a.CopyTo(r, 0); b.CopyTo(r, a.Length);
            return r;
        }

        /// <summary>The beat has been sent: it waits for his answer. Her ordinary first hello is then not needed.</summary>
        public static void Opened(GirlfriendState s, GfArcBeat b, double game)
        {
            if (s == null || b == null) return;
            s.arcOpen = b.key; s.arcAt = game;
            if (b.key == "hello" && !s.done.Contains("life:firstNight")) s.done.Add("life:firstNight");
        }

        public static string[] Choices(GfArcBeat b, bool english)
        {
            var r = new string[b.choices.Length];
            for (int i = 0; i < r.Length; i++) r[i] = english ? b.choices[i].en : b.choices[i].zh;
            return r;
        }

        /// <summary>One of the beat's answers, by its exact text in either language. Null for anything else he typed.</summary>
        public static GfArcChoice Match(GfArcBeat b, string text)
        {
            if (b == null || text == null) return null;
            text = text.Trim();
            foreach (var c in b.choices) if (c.zh == text || c.en == text) return c;
            return null;
        }

        /// <summary>
        /// His answer to the open beat. A picked answer moves her affection, sets its flag and memory, and returns it (her
        /// reply is its lines). Anything else closes the beat too, with no change: her model answers it. Null when no
        /// beat is open.
        /// </summary>
        public static GfArcBeat Answer(GirlfriendState s, string text, out GfArcChoice picked)
        {
            picked = null;
            var b = Open(s);
            if (b == null) return null;
            picked = Match(b, text);
            if (picked != null)
            {
                GirlfriendRules.AddAffection(s, picked.affection);
                if (picked.flag.Length > 0) GirlfriendRules.Mark(s, picked.flag);
                if (picked.memoryZh.Length > 0) GirlfriendRules.Remember(s, "arc." + b.key, "他说：" + picked.memoryZh);
            }
            Close(s, b);
            return b;
        }

        /// <summary>He left the beat unanswered too long: it closes and costs a little.</summary>
        public static bool Expire(GirlfriendState s, double game)
        {
            var b = Open(s);
            if (b == null || game - s.arcAt < OpenFor) return false;
            GirlfriendRules.AddAffection(s, -UnansweredCost);
            Close(s, b);
            return true;
        }

        static void Close(GirlfriendState s, GfArcBeat b)
        {
            s.arcOpen = "";
            if (!s.done.Contains(DonePrefix + b.key)) s.done.Add(DonePrefix + b.key);
        }

        /// <summary>For her model when he typed his own answer to a beat.</summary>
        public static string FreeSituation(GfArcBeat b, bool english)
            => english
                ? b.askedEn + " He did not pick an answer; he wrote the line above in his own words. Take it in your own way, and if it dodged your question, ask once more."
                : b.askedZh + "他没有照你想听的说，而是自己说了上面那句。按你的性格接住他这句话；要是他躲开了你的问题，就再追问一次。";
    }
}
