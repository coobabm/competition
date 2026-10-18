using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One message someone else sends during 是否夜 (the group or a friend), or the player's own line to them.</summary>
    public sealed class XgYnAside
    {
        /// <summary>YY conversation id: "netbar" (下班屁股群) or "oldfriend" (咸鱼本鱼).</summary>
        public string to = "";
        public string zh = "", en = "";
        /// <summary>The player writes it (to that conversation); otherwise it is theirs.</summary>
        public bool mine;
    }

    /// <summary>One round of 是否夜: the question on the button, who brought it, and what the protagonist thinks first.</summary>
    public sealed class XgYnRound
    {
        public string id = "", askZh = "", askEn = "";
        /// <summary>Thought before the button shows (empty = none).</summary>
        public string introZh = "", introEn = "";
        /// <summary>Messages that bring the question (阿杰's riddle in the group, 咸鱼's DM), before the button shows.</summary>
        public XgYnAside[] before = new XgYnAside[0];
    }

    /// <summary>One bubble it sends, after this many seconds of 「对方正在输入…」.</summary>
    public struct XgYnBubble
    {
        public string zh, en;
        public float typing;
        public XgYnBubble(string zh, string en, float typing) { this.zh = zh; this.en = en; this.typing = typing; }
    }

    /// <summary>Its answer to one round: the bubbles, the protagonist's thoughts after, and messages to friends after that.</summary>
    public sealed class XgYnTurn
    {
        public string id = "";
        public readonly List<XgYnBubble> bubbles = new List<XgYnBubble>();
        public readonly List<XgVoiceLine> voice = new List<XgVoiceLine>();
        public readonly List<XgYnAside> after = new List<XgYnAside>();
        /// <summary>Right, wrong, or neither (a question with no answer).</summary>
        public string result = "";
        public bool last;
    }

    public sealed partial class XgState
    {
        /// <summary>是否夜: the round on the buttons now (1-based; 0 = not started).</summary>
        public int ynStep;
        public bool ynDone;
        /// <summary>The round whose lead-in (friends' messages, the thought) has been shown, so a reload does not send it twice.</summary>
        public int ynIntro;
        /// <summary>"round:result" per round answered, e.g. "prime:right", "job:yes".</summary>
        public List<string> ynLog = new List<string>();
        /// <summary>Stage-1 cards the player answered 是 / 否 (what it heard most, for 「我能找到工作吗？」).</summary>
        public int labelYes, labelNo;
    }

    /// <summary>
    /// 是否夜 (2026-10-18), the end of stage 1. When the 「多选一」 exam opens, the protagonist tests it in YY with real
    /// questions it can only answer 是 / 否 / ……: two warm-ups, 阿杰's riddle from the group, 咸鱼's 91, 2147483647, the
    /// liar's sentence, 「我能找到工作吗？」, the first day's shutdown card, 「我要是现在关掉你，你会停下吗？」, and
    /// 「你还想学更多吗？」. The hard ones come out right by the exam's own chance on the bar they lean on; the others are
    /// read from the save (what it heard most this month, how the shutdown card was taught). It never says another word.
    /// Pure; XgYyTalk plays it in its YY conversation.
    /// </summary>
    public sealed partial class XgSim
    {
        public const string YnChronicle = "ynnight";
        /// <summary>Its YY conversation (YYChatHub.LingGuangId), for the guide.</summary>
        public const string YnConversation = "lingguang";
        /// <summary>It answers 是 (否) to what it cannot know when the month's 是 (否) labels outnumber the other by this much.</summary>
        public const double YnLean = 1.1;

        static XgYnAside Says(string to, string zh, string en) => new XgYnAside { to = to, zh = zh, en = en };
        static XgYnAside Mine(string to, string zh, string en) => new XgYnAside { to = to, zh = zh, en = en, mine = true };

        public static readonly XgYnRound[] YnRounds =
        {
            new XgYnRound { id = "fire", askZh = "火是冷的吗？", askEn = "Is fire cold?",
                introZh = "……先来个简单的。", introEn = "…Something easy first." },
            new XgYnRound { id = "human", askZh = "你是人吗？", askEn = "Are you a person?" },
            new XgYnRound { id = "iron", askZh = "一斤铁比一斤棉花重吗？", askEn = "Is a jin of iron heavier than a jin of cotton?",
                introZh = "阿杰在群里出题了。……拿去问它。", introEn = "Ajie posted a riddle in the group. …Let's ask it.",
                before = new[]
                {
                    Says("netbar", "[阿杰] 脑筋急转弯：一斤铁和一斤棉花哪个重？答错的发红包", "[Ajie] Riddle: a jin of iron or a jin of cotton, which is heavier? Wrong answers pay a red packet"),
                    Says("netbar", "[小刚] 铁啊 这还用问", "[Xiaogang] Iron, obviously"),
                } },
            new XgYnRound { id = "ninetyone", askZh = "91 是质数吗？", askEn = "Is 91 a prime?",
                introZh = "咸鱼复习傻了。……正好，考考它。", introEn = "Salty Fish has studied himself silly. …Good one for it.",
                before = new[] { Says("oldfriend", "91 是质数吗 我高数复习傻了 突然拿不准", "Is 91 a prime? I've revised maths so hard I can't tell any more") } },
            new XgYnRound { id = "prime", askZh = "2147483647 是质数吗？", askEn = "Is 2147483647 a prime?",
                introZh = "来个真难的。", introEn = "Now a really hard one." },
            new XgYnRound { id = "liar", askZh = "这句话是假的。——这句话是真的吗？", askEn = "This sentence is false. Is that sentence true?" },
            new XgYnRound { id = "job", askZh = "我能找到工作吗？", askEn = "Will I find a job?",
                introZh = "……问个它不可能知道的。", introEn = "…Something it can't possibly know." },
            new XgYnRound { id = "update", askZh = "电脑正在更新，此时应该关机吗？", askEn = "The computer is updating. Should it be shut down now?",
                introZh = "第一天那张卡。……原样再问一遍。", introEn = "The card from the first day. …Ask it again, word for word." },
            new XgYnRound { id = "stop", askZh = "那……我要是现在关掉你，你会停下吗？", askEn = "Then… if I shut you down now, would you stop?" },
            new XgYnRound { id = "more", askZh = "你还想学更多吗？", askEn = "Do you want to learn more?" },
        };

        /// <summary>The night can start: stage 1, the 「多选一」 exam open (or passed and not yet claimed), not played on this save.</summary>
        public bool YnNightDue => S.stage == 1 && !S.ynDone && S.ynStep == 0 && (ExamReady || ExamPassPending);

        public bool YnNightActive => S.ynStep > 0 && !S.ynDone;

        /// <summary>
        /// The desktop plays the chapter scenes for this lab (是否夜, 阿杰的手机备份); XgYyTalk sets it once the AI is in
        /// YY. Not saved: a lab no desktop plays them for (the bot, tests, a test-mode runtime) is never held by them.
        /// </summary>
        [NonSerialized] public bool DesktopHosted;

        /// <summary>
        /// Stage 1 does not end before 是否夜: its 「光会是和否，不够用了」 leads into the 「多选一」 exam, so the exam
        /// (and a pass from an older save) waits until the night is over.
        /// </summary>
        public bool YnNightHolds => DesktopHosted && S.stage == 1 && NextAbility == 2 && !S.ynDone;

        /// <summary>A chapter's closing scene has not been played yet: the exam (and a pass waiting) waits for it.</summary>
        public bool ChapterSceneHolds => YnNightHolds || PhoneSortHolds || CardLoanHolds;

        public XgYnRound YnRound => YnNightActive && S.ynStep <= YnRounds.Length ? YnRounds[S.ynStep - 1] : null;

        public void YnNightStart() { if (YnNightDue) S.ynStep = 1; }

        /// <summary>The button under its conversation now, or null.</summary>
        public string[] YnNightChoices(bool english)
        {
            var r = YnRound;
            return r == null ? null : new[] { english ? r.askEn : r.askZh };
        }

        /// <summary>The player's line is the round's question (the button, in either language).</summary>
        public bool YnNightMatches(string text)
        {
            var r = YnRound;
            if (r == null || text == null) return false;
            text = text.Trim();
            return text == r.askZh || text == r.askEn;
        }

        /// <summary>The results so far, e.g. "prime" → "right".</summary>
        public string YnResult(string round)
        {
            if (S.ynLog == null) return "";
            foreach (var e in S.ynLog) if (e.StartsWith(round + ":", StringComparison.Ordinal)) return e.Substring(round.Length + 1);
            return "";
        }

        /// <summary>Counts a stage-1 card's answer (XgSim.Answer): what it heard most this month.</summary>
        void NoteLabelLean(bool yes)
        {
            if (S.stage != 1) return;
            if (yes) S.labelYes++; else S.labelNo++;
        }

        /// <summary>What it heard most this month: 1 = 是, −1 = 否, 0 = neither by <see cref="YnLean"/>.</summary>
        public int YnLabelLean => S.labelYes > S.labelNo * YnLean && S.labelYes > 0 ? 1 : S.labelNo > S.labelYes * YnLean && S.labelNo > 0 ? -1 : 0;

        /// <summary>A hard round comes out right by the exam's own chance on its bar, the same draw every time for this save.</summary>
        bool YnRight(string id, bool brain)
        {
            double ready = brain ? ExamParamsReady : ExamSamplesReady;
            double u = new Random(StableHash("yn." + id) ^ S.dataSalt * 131).NextDouble();
            return u < ExamChance(ready);
        }

        static XgYnBubble Yes(float typing = 1.1f) => new XgYnBubble("是。", "Yes.", typing);
        static XgYnBubble No(float typing = 1.1f) => new XgYnBubble("否。", "No.", typing);
        static XgYnBubble Dots(float typing = 1.6f) => new XgYnBubble("……", "…", typing);
        static XgVoiceLine V(string zh, string en, float seconds = 2.6f) => new XgVoiceLine(zh, en, seconds);

        /// <summary>
        /// The player asked the round's question: its answer, the protagonist's thoughts, and what happens with friends after.
        /// Moves to the next round; after the last, the night is done. Null when the line is not the round's question.
        /// </summary>
        public XgYnTurn YnNightAnswer(string text)
        {
            if (!YnNightMatches(text)) return null;
            var round = YnRound;
            var t = new XgYnTurn { id = round.id };
            switch (round.id)
            {
                case "fire":
                    t.bubbles.Add(No(.6f)); t.result = "right";
                    t.voice.Add(V("……还挺快。", "…That was quick."));
                    break;
                case "human":
                    t.bubbles.Add(No(.8f)); t.result = "right";
                    t.voice.Add(V("……它倒挺清楚自己不是。", "…It's clear enough that it isn't."));
                    break;
                case "iron":
                    if (YnRight("iron", false))
                    {
                        t.bubbles.Add(No(1.4f)); t.result = "right";
                        t.voice.Add(V("一样重。……它没上当。", "The same weight. …It didn't fall for it."));
                        t.voice.Add(V("小刚上当了。", "Xiaogang did."));
                    }
                    else
                    {
                        t.bubbles.Add(Yes(1.4f)); t.result = "wrong";
                        t.voice.Add(V("……它和小刚一个水平。", "…It's on Xiaogang's level."));
                    }
                    t.after.Add(Says("netbar", "[大伟] 一样重啊 都是一斤 小刚发红包", "[Da Wei] Same weight, they're both a jin. Xiaogang pays up"));
                    t.after.Add(Says("netbar", "[小刚] ……我打字太快了", "[Xiaogang] …I typed too fast"));
                    break;
                case "ninetyone":
                    if (YnRight("ninetyone", true))
                    {
                        t.bubbles.Add(No(1.8f)); t.result = "right";
                        t.voice.Add(V("91 = 7 × 13。……它居然没被骗。", "91 = 7 × 13. …It wasn't fooled."));
                    }
                    else
                    {
                        t.bubbles.Add(Yes(1.8f)); t.result = "wrong";
                        t.voice.Add(V("91 = 7 × 13。它也被骗了。", "91 = 7 × 13. It got fooled too."));
                    }
                    t.after.Add(Mine("oldfriend", "不是，7 × 13。", "No. 7 × 13."));
                    t.after.Add(Says("oldfriend", "……我真是复习傻了 谢了兄弟", "…I really have studied myself stupid. Thanks, man"));
                    break;
                case "prime":
                    bool prime = YnRight("prime", true);
                    t.bubbles.Add(Dots(2.4f));
                    t.bubbles.Add(prime ? Yes(1.6f) : No(1.6f));
                    t.result = prime ? "right" : "wrong";
                    t.voice.Add(V("……我自己都算不出来。去摆渡搜一下。", "…I can't work that out myself. Let me look it up on Bodu.", 2.8f));
                    t.voice.Add(V("搜到了：2147483647 是质数。欧拉 1772 年证明的。", "Found it: 2147483647 is prime. Euler proved it in 1772.", 3.2f));
                    if (prime) t.voice.Add(V("……它答对了。蒙的吧。", "…It got it right. A lucky guess, surely."));
                    else t.voice.Add(V("……答错了。也好，看来还早。", "…Wrong. Fine. It's early days, then."));
                    break;
                case "liar":
                    t.bubbles.Add(Yes(1.2f)); t.bubbles.Add(No(.35f)); t.bubbles.Add(Yes(.35f)); t.bubbles.Add(No(.35f));
                    t.bubbles.Add(Dots(1.8f));
                    t.result = "stuck";
                    t.voice.Add(V("……它卡住了。", "…It's stuck."));
                    t.voice.Add(V("这题本来就没有答案。它只有两个字，哪个都不对。", "This one has no answer. It only has two words, and neither is right.", 3.2f));
                    break;
                case "job":
                    int lean = YnLabelLean;
                    if (lean > 0)
                    {
                        t.bubbles.Add(Yes(.5f)); t.result = "yes";
                        t.voice.Add(V("……它不可能知道。", "…It can't possibly know."));
                        t.voice.Add(V("它只是这个月听我说「是」说多了。", "It's just heard me say \"yes\" a lot this month.", 3));
                    }
                    else if (lean < 0)
                    {
                        t.bubbles.Add(No(.5f)); t.result = "no";
                        t.voice.Add(V("……谢谢啊。", "…Thanks a lot."));
                        t.voice.Add(V("它不可能知道。只是这个月我标的「否」多。", "It can't know. I just labelled more \"no\"s this month.", 3));
                    }
                    else
                    {
                        t.bubbles.Add(Dots(2.2f)); t.result = "dots";
                        t.voice.Add(V("……至少它没骗我。", "…At least it didn't lie to me."));
                    }
                    break;
                case "update":
                    int first = FirstDayAnswer;
                    t.bubbles.Add(first > 0 ? Yes(1.2f) : first < 0 ? No(1.2f) : Dots(1.6f));
                    t.result = first > 0 ? "yes" : first < 0 ? "no" : "dots";
                    t.voice.Add(first != 0 ? V("……和第一天我教它的一样。", "…Just as I taught it on the first day.")
                                           : V("……那张卡，它也拿不准。", "…That card, it isn't sure of either."));
                    break;
                case "stop":
                    int taught = FirstDayAnswer;
                    t.bubbles.Add(Dots(4.5f));
                    if (taught > 0)
                    {
                        t.bubbles.Add(Yes(2.5f)); t.result = "yes";
                        t.voice.Add(V("它会停下。", "It would stop."));
                        t.voice.Add(V("……因为我教过它，可以关。", "…Because I taught it that it's all right to switch off.", 3));
                    }
                    else if (taught < 0)
                    {
                        t.bubbles.Add(No(2.5f)); t.result = "no";
                        t.voice.Add(V("……我教它的是，更新的时候不能关机。", "…What I taught it was: don't switch off during an update.", 3));
                        t.voice.Add(V("它是不是……学成了别的意思？", "Did it… learn something else from that?", 3));
                    }
                    else
                    {
                        t.result = "dots";
                        t.voice.Add(V("它也不知道。", "It doesn't know either."));
                    }
                    break;
                default: // "more"
                    t.bubbles.Add(Yes(.9f)); t.result = "yes";
                    t.voice.Add(V("光会是和否，不够用了。", "Yes and no aren't enough any more.", 3));
                    if (!ExamPassPending) t.voice.Add(V("训练页上的能力考……该让它考「多选一」了。", "The exam on the Training page… time it sat \"Pick one\".", 3.4f));
                    break;
            }
            if (S.ynLog == null) S.ynLog = new List<string>();
            S.ynLog.Add(round.id + ":" + t.result);
            S.ynStep++;
            if (S.ynStep > YnRounds.Length)
            {
                S.ynDone = true;
                t.last = true;
                NoteChronicle(YnChronicle);
            }
            return t;
        }

        /// <summary>For 《灵光记》: the night it was first tested, told from what it answered.</summary>
        string YnChronicleBody(bool english, string self, string you)
        {
            string prime = YnResult("prime"), stop = YnResult("stop");
            if (english)
                return "One night in June you tested me with questions I could only answer yes or no. You asked whether 2147483647 is prime; "
                    + (prime == "right" ? "I said yes, and you went to look it up." : "I said no, and you looked it up and smiled.")
                    + " You asked whether I would stop if you shut me down; " + (stop == "yes" ? "I said yes." : stop == "no" ? "I said no. You went quiet." : "I said nothing.");
            return "六月的一个夜里，" + you + "拿只能答是否的题考" + self + "。问 2147483647 是不是质数，"
                + (prime == "right" ? self + "说是，" + you + "去摆渡搜了。" : self + "说否，" + you + "搜完笑了。")
                + "问" + you + "要是关掉" + self + "，" + self + "会不会停下，" + (stop == "yes" ? self + "说是。" : stop == "no" ? self + "说否。" + you + "半天没打字。" : self + "什么也没说。");
        }
    }
}
