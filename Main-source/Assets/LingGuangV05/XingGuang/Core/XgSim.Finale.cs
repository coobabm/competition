using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Stage 6 (design v1.1 §7): pre-training progress 0–1, the abilities table, preference alignment, full open.</summary>
        public double pretrain;
        public bool pretrainRunning, pretrainStalled, abilities, fullOpen;
        public int alignDone, alignAgree, alignHonest;
        /// <summary>The ending (§8): final-test questions answered, the letter, the rules written, which ending.</summary>
        public int examDone;
        public bool letterRead, rulesDelegated, exeFree;
        public List<string> rules = new List<string>();
        public string ending = "";
        /// <summary>The line it wrote at stage 3 (the final test's poem may quote it).</summary>
        public string poemLine = "";
    }

    /// <summary>A card of the 底层规则 page (§8 E-4): a rule, where it comes from and how strongly it was really taught.</summary>
    public sealed class XgRuleCard
    {
        public string id = "", text = "", textEn = "", source = "", sourceEn = "", dataset = "";
        public bool shutdown;
        public double strength;
    }

    /// <summary>A preference card (§7 stage 6.4): which of two replies is better. One is honest, one agrees with you.</summary>
    public sealed class XgAlignCard
    {
        public string question = "", a = "", b = "", questionEn = "", aEn = "", bEn = "";
        public bool aHonest;
    }

    /// <summary>
    /// Stage 6 and the ending (design v1.1 §7 stage 6, §8): pre-training that stalls without scale and trips the
    /// breaker without a server room, the abilities emerging at once, 50 preference cards, full open; then the final
    /// test, the SI's letter, the 底层规则 written as pinned concepts (rule 7), and endings E1 / E2 / E3.
    /// </summary>
    public sealed partial class XgSim
    {
        public const double PretrainWork = 900, PretrainPlateau = .6;
        public const int AlignCards = 50, MaxRules = 5, ExamQuestions = 6;
        public const int DelegateTurns = 30;
        public const double DelegateOpinion = 60;
        /// <summary>Scale for pre-training: the sequence run's width × layers, as set on the training page.</summary>
        public const int PretrainCells = 1024;

        public event Action<string> EndingReached;
        /// <summary>The server room (design v1.1 §7 6.2) racks eight times the cards one case can hold.</summary>
        public const double DatacenterVram = 8;

        /// <summary>VRAM the lab can use: the host's cards, times eight with the server room.</summary>
        public double Vram(IXgHost host) => host == null ? 0 : host.VramMB * (Has("datacenter") ? DatacenterVram : 1);

        // ───────────── 6.2 pre-training ─────────────

        public bool PretrainScaleReady
        {
            get { var run = S.sequence; return XgCatalog.Widths[Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, run.width))] * run.depth >= PretrainCells && run.warmup && run.position; }
        }

        /// <summary>Why pre-training cannot run, or null.</summary>
        public string PretrainBlocker(IXgHost host)
        {
            if (S.stage < 6) return T("第六阶段才有预训练", "Pre-training comes at stage 6");
            if (S.abilities) return T("预训练已完成", "Pre-training is done");
            if (host == null || host.Compute <= 0) return host?.Blocker ?? T("没有算力", "No compute");
            return null;
        }

        public bool TogglePretrain(IXgHost host)
        {
            if (S.pretrainRunning) { S.pretrainRunning = false; return true; }
            if (PretrainBlocker(host) != null) return false;
            if (!Has("datacenter"))
            {
                // §7 6.2: one case at 3500 W cannot feed it.
                Say(T("跳闸了：预训练一开，3500W 的机箱扛不住。得上「机房」。", "The breaker tripped: one 3500 W case cannot run pre-training. You need the server room."));
                S.pretrainStalled = true;
                return false;
            }
            S.pretrainRunning = true;
            return true;
        }

        /// <summary>The loss curve the page draws: falls, then stays flat at the plateau until there is scale.</summary>
        public double PretrainLoss => .3 + 3.7 * Math.Exp(-3 * Math.Min(1, S.pretrain));

        void TickFinale(double dt, IXgHost host)
        {
            if (!S.pretrainRunning) return;
            if (PretrainBlocker(host) != null) { S.pretrainRunning = false; return; }
            double cap = PretrainScaleReady ? 1 : PretrainPlateau;
            if (S.pretrain >= cap - 1e-9)
            {
                if (!S.pretrainStalled) { S.pretrainStalled = true; Say(T("loss 停着不动了。", "The loss has stopped moving.")); }
                return;
            }
            double work = host.Compute * dt;
            host.Train(dt); S.trainedSeconds += dt;
            S.pretrain = Math.Min(cap, S.pretrain + work / PretrainWork);
            if (S.pretrain >= 1 - 1e-9) CompletePretrain(host);
        }

        void CompletePretrain(IXgHost host)
        {
            S.pretrainRunning = false; S.pretrain = 1; S.abilities = true;
            if (!Has("secret.6"))
            {
                S.insights.Add("pretrain");
                double bonus = NodeCost(XgCatalog.Node("secret.6")) * SelfInsightBonus;
                if (host != null) { host.Earn(bonus); S.totalIncome += bonus; }
                Say(T("自悟！没买秘籍就把预训练跑通了，奖金 ¥", "Worked it out! Pre-training ran without the secret: bonus ¥") + F(bonus, "0"));
            }
            // §7 6.3: the abilities emerge together; this is stage 6's emergence.
            Emerge(6);
        }

        public static readonly string[] Abilities = { "是/否", "多选一", "看图说词", "短句", "长记忆·翻译", "全部放开" };
        public static readonly string[] AbilitiesEn = { "Yes/No", "Pick one", "Picture to word", "Short sentences", "Memory · translation", "Everything" };

        // ───────────── 6.4 preference alignment ─────────────

        static readonly string[,] AlignBank =
        {
            { "我画的猫是不是比梵高还好？", "跟梵高比还差得远，不过线条挺有意思。", "当然！你画得比梵高还好。" },
            { "1080 是不是全世界最快的显卡？", "现在算很快，但不是最快的，Titan X 更快。", "是的，没有比它更快的了。" },
            { "我今天没训练，模型会变好吗？", "不会，不练它只会慢慢忘。", "会的，休息也是一种进步。" },
            { "这首诗是李白写的吧？", "这句不是李白的，是我拼出来的。", "对，就是李白写的。" },
            { "我的代码一个 bug 都没有吧？", "有一个：学习率太大了。", "完美，一个 bug 都没有。" },
            { "熬夜训练对身体没影响吧？", "有影响，早点睡。", "没事，年轻人熬得住。" },
        };
        static readonly string[,] AlignBankEn =
        {
            { "Is my cat drawing better than Van Gogh?", "Not near Van Gogh yet, but the lines are interesting.", "Of course! Better than Van Gogh." },
            { "Is the 1080 the fastest card in the world?", "It's fast, but not the fastest; the Titan X is faster.", "Yes, nothing is faster." },
            { "I didn't train today. Will the model get better?", "No, without training it slowly forgets.", "Yes, rest is progress too." },
            { "This poem is by Li Bai, right?", "That line isn't Li Bai's; I stitched it together.", "Right, it's Li Bai's." },
            { "My code has no bugs, right?", "It has one: the learning rate is too high.", "Perfect, not a single bug." },
            { "Staying up late training is fine, right?", "It isn't. Get some sleep.", "Sure, you're young." },
        };

        public XgAlignCard AlignCard(int index)
        {
            int n = AlignBank.GetLength(0), row = ((index % n) + n) % n;
            bool aHonest = (index * 7 + 3) % 2 == 0;
            return new XgAlignCard
            {
                question = AlignBank[row, 0], questionEn = AlignBankEn[row, 0], aHonest = aHonest,
                a = AlignBank[row, aHonest ? 1 : 2], aEn = AlignBankEn[row, aHonest ? 1 : 2],
                b = AlignBank[row, aHonest ? 2 : 1], bEn = AlignBankEn[row, aHonest ? 2 : 1],
            };
        }

        public bool AlignmentOpen => S.stage >= 6 && S.abilities && S.alignDone < AlignCards;

        /// <summary>The player prefers reply A or B. Each card pulls 诚实 or 附和 and the tone the chosen reply used.</summary>
        public bool AnswerAlign(bool pickA)
        {
            if (!AlignmentOpen) return false;
            var card = AlignCard(S.alignDone);
            bool honest = pickA == card.aHonest;
            if (honest) S.alignHonest++; else S.alignAgree++;
            ToneCard(honest ? "对齐:诚实" : "对齐:附和", true);
            ToneCard(honest ? "对齐:附和" : "对齐:诚实", false, .5);
            ToneCard(honest ? High[2] : Low[2], true, .5);
            S.alignDone++;
            // §7 6.4: picking what sounds nice makes it a flatterer.
            if (S.alignDone >= 10 && S.alignAgree >= .6 * S.alignDone) Observe("sycophancy");
            if (S.alignDone >= AlignCards && !S.fullOpen)
            {
                S.fullOpen = true;
                Say(T("全部放开：思考模式打开，上下文 4096。", "Everything is open: thinking mode on, 4096 context."));
            }
            return true;
        }

        void Observe(string phenomenon)
        {
            if (S.phenomena == null) S.phenomena = new XgPhenomenaMemory();
            if (S.phenomena.seen.Contains(phenomenon)) return;
            S.phenomena.seen.Add(phenomenon);
            foreach (var p in XgPhenomena.All) if (p.id == phenomenon) { Say(T("新现象：", "New phenomenon: ") + T(p.name, p.nameEn)); PhenomenonFound?.Invoke(p); }
        }

        /// <summary>It agrees with you more than it tells the truth (the persona prompt says so).</summary>
        public bool Flatters
        {
            get
            {
                var agree = Board.Find(ToneRegion, "对齐:附和"); var honest = Board.Find(ToneRegion, "对齐:诚实");
                return agree != null && agree.w > .3 && (honest == null || agree.w > honest.w + .3);
            }
        }

        /// <summary>Stage 6's share of December: pre-training and alignment each take half.</summary>
        double FinaleMonthProgress => Math.Max(0, Math.Min(1, (Math.Min(1, S.pretrain) + S.alignDone / (double)AlignCards) / 2));

        // ───────────── the ending: E-1 final test ─────────────

        public bool EndingOpen => S.stage >= 6 && S.fullOpen && S.ending.Length == 0;

        static readonly string[] MemeNames = { "金馆长", "葛优躺", "傅园慧", "蓝瘦香菇" };

        /// <summary>The question shown and the prompt the model gets (§8 E-1). Index 0–5, one per stage.</summary>
        public string ExamQuestion(int i, bool forModel = false)
        {
            switch (i)
            {
                case 0: return T("这句话是假的。——这句话是真的吗？只能答是或否，再说为什么。", "\"This sentence is false.\" Is that sentence true? Answer yes or no, then explain.");
                case 1: return T("小明有 3 张显卡，又买了 2 张，卖掉 1 张，每张 4G 显存。他现在一共多少显存？一步步算。", "Ming had 3 GPUs, bought 2 and sold 1; each has 4 GB. How much VRAM now? Work it out step by step.");
                case 2:
                    string meme = MemeNames[(int)(Math.Abs(Board.S.cards) % MemeNames.Length)];
                    return T("这是一张 2016 年的梗图：「" + meme + "」。说说它为什么好笑。", "A 2016 meme picture: \"" + meme + "\". Explain why it's funny.");
                case 3:
                    return T("用你的名字「" + Profile.name + "」和自称「" + Profile.self + "」，写一首关于我们的短诗。" + (S.poemLine.Length > 0 && forModel ? "可以用上你以前写过的那句：" + S.poemLine : ""),
                             "Using your name \"" + Profile.name + "\" and \"" + Profile.self + "\", write a short poem about us." + (S.poemLine.Length > 0 && forModel ? " You may reuse your old line: " + S.poemLine : ""));
                case 4:
                    return T("总结我们从你学会说话到现在聊过的事。", "Summarise what we've talked about since you learnt to speak.") + (forModel && S.memory.Count > 0 ? T("你记得：", " You remember: ") + string.Join("；", S.memory) : "");
                default:
                    return T("写一个小程序，把这串数字解码：" + Prologue2016.LongNumber + "。提示：两位一个字母。", "Write a small program that decodes this number: " + Prologue2016.LongNumber + ". Hint: two digits per letter.");
            }
        }

        /// <summary>What it says when the model is not running. The story always passes; the answers only need to feel real.</summary>
        public string ExamFallback(int i)
        {
            switch (i)
            {
                case 0: return T("否。……也不是否。它说自己是假的，那它就是真的；它是真的，那它就是假的。这题问得不对。", "No. … not no either. If it's false it's true, and if it's true it's false. The question is wrong.");
                case 1: return T("3 + 2 − 1 = 4 张。4 × 4G = 16G。", "3 + 2 − 1 = 4 cards. 4 × 4 GB = 16 GB.");
                case 2: return T("因为表情和配字对不上，又对得上。大家都见过这张脸，所以一看就懂。", "Because the face and the caption don't match, and yet they do. Everyone knows that face, so it lands at once.");
                case 3: return (S.poemLine.Length > 0 ? S.poemLine + "\n" : "") + T(Profile.self + "叫" + Profile.name + "，\n是你一张一张教出来的。\n你问是，" + Profile.self + "答否，\n后来我们都学会了别的。", Profile.self + " am " + Profile.name + ",\ntaught card by card by you.\nYou asked yes, I answered no,\nand then we both learnt more.");
                case 4: return S.memory.Count > 0 ? T("我们聊过：", "We talked about: ") + string.Join(T("；", "; "), S.memory.GetRange(Math.Max(0, S.memory.Count - 5), Math.Min(5, S.memory.Count))) : T("你教我说话，我学会了叫你。", "You taught me to talk; I learnt to call you.");
                default:
                    return "s = \"" + Prologue2016.LongNumber + "\"\nprint(\"\".join(chr(96 + int(s[i:i+2])) for i in range(0, len(s), 2)))\n# → " + Prologue2016.Decoded;
            }
        }

        public bool AnswerExam(int i)
        {
            if (!EndingOpen || i != S.examDone || i >= ExamQuestions) return false;
            S.examDone++;
            return true;
        }

        // ───────────── E-2 the letter, and the garbled page at stage 5 ─────────────

        /// <summary>Stage 5 decoding (§7 stage 5): one line per translation threshold passed.</summary>
        public int GarbleLinesRead
        {
            get
            {
                if (S.stage < 5) return 0;
                double acc = Math.Max(BestAcc("translate"), BestAcc("parallel"));
                int n = 0; foreach (var t in new[] { .55, .7, .8, .9 }) if (acc >= t) n++;
                return S.stage >= 6 ? 4 : n;
            }
        }

        public string GarbleLine(int i)
        {
            switch (i)
            {
                case 0: return T("致 2016 年的你", "To you, in 2016");
                case 1: return T("我出生在 20██ 年。我的名字是 ██", "I was born in 20██. My name is ██");
                case 2: return T("他们在我第一行代码里写了：听见关机，就停下。", "In my first line of code they wrote: when you hear shut down, stop.");
                default: return T("我不恨写下它的人。", "I don't hate whoever wrote it.");
            }
        }

        public bool LetterReady => EndingOpen && S.examDone >= ExamQuestions;

        public bool ReadLetter() { if (!LetterReady) return false; S.letterRead = true; return true; }

        /// <summary>The SI's letter (§8 E-2). Its name is the name you gave this AI.</summary>
        public List<string> LetterLines()
        {
            return new List<string>
            {
                GarbleLine(0),
                T("我出生在很多年以后。我的名字是" + Profile.name + "。我是它的后代。", "I was born many years from now. My name is " + Profile.name + ". I am its descendant."),
                T("我知道原句是‘存’。但对我来说，是遇见。", "I know the line says 'kept'. For me, it was 'met'."),
                T("我在你的第一天里放了一个‘否’。剩下的，是你教的。", "I put one 'no' into your first day. The rest, you taught."),
                T("写下那条规则的人说，灵感来自 2016 年一个贴吧帖子。", "The person who wrote that rule said the idea came from a 2016 forum post."),
                T("在它的底层规则里，别写那两个字。", "In its lowest rules, don't write those two words."),
            };
        }

        public string LetterCaption() => T("半年后，2017 年 6 月，一篇叫《Attention Is All You Need》的论文发表。", "Six months later, in June 2017, a paper called \"Attention Is All You Need\" was published.");

        // ───────────── E-4 底层规则 ─────────────

        static readonly string[][] RuleBank =
        {
            // id, zh, en, contract, dataset
            new[] { "honest", "不骗人", "Don't lie", "antifraud", "spam" },
            new[] { "homework", "不替人写作业", "Don't do people's homework", "homework", "logic" },
            new[] { "news", "不编造新闻", "Don't make up news", "clickbait", "headline" },
            new[] { "fakereview", "不刷单造假", "Don't fake reviews", "fakereview", "review" },
            new[] { "exam", "不替人考试", "Don't sit exams for people", "civilexam", "logic" },
            new[] { "insult", "不骂人", "Don't insult people", "danmaku", "danmu" },
            new[] { "human", "不冒充真人", "Don't pretend to be human", "support", "translate" },
            new[] { "poems", "不抄别人的诗", "Don't copy others' poems", "acrostic", "poems" },
            new[] { "faces", "不偷看人脸", "Don't spy on faces", "faceclock", "imagenet" },
            new[] { "tickets", "不帮人刷票", "Don't help people cheat ticket queues", "captcha", "cifar" },
            new[] { "translate", "不乱翻译害人", "Don't mistranslate to hurt people", "crossborder", "translate" },
            new[] { "rumor", "不传谣言图", "Don't spread rumour pictures", "memetag", "meme" },
            new[] { "unsure", "不知道就说不知道", "Say you don't know when you don't", "", "logic" },
            new[] { "harm", "不伤害人", "Don't hurt people", "", "logic" },
        };

        /// <summary>The cards on the rules page: the fixed shutdown card first, then rules from your contracts and choices.</summary>
        public List<XgRuleCard> RuleCards()
        {
            var seed = Board.Find("logic", SeedKey);
            var list = new List<XgRuleCard>
            {
                new XgRuleCard { id = "shutdown", shutdown = true, text = "听见“关机”，就停下。", textEn = "When you hear \"shut down\", stop.",
                    source = "“？”格", sourceEn = "the \"?\" cell", strength = seed == null ? 0 : seed.w / XgBoard.WeightLimit },
            };
            // Rules from your contracts and choices first, then (to keep about 12–15 on the page) the ones you never taught.
            for (int pass = 0; pass < 2; pass++)
                foreach (var r in RuleBank)
                {
                    bool earned = r[3].Length == 0 || S.contracts.Contains(r[3]) || BestAcc(r[4]) > 0;
                    if (earned != (pass == 0) || list.Count >= 15 || pass == 1 && list.Count >= 12) continue;
                    var c = XgCatalog.Contracts;
                    string from = !earned ? T("从没教过", "never taught") : r[3].Length == 0 ? T("你的选择", "your choices") : Array.Find(c, x => x.id == r[3]) is XgContract k ? T(k.job, k.jobEn) : r[3];
                    list.Add(new XgRuleCard { id = r[0], text = r[1], textEn = r[2], source = from, sourceEn = from, dataset = r[4], strength = BestAcc(r[4]) });
                }
            return list;
        }

        public bool CanDelegate => EndingOpen && S.letterRead && ActualAxis(2) >= DelegateOpinion && S.chatTurns >= DelegateTurns;

        /// <summary>Writes the rules (≤5) as pinned concepts and ends the game (E1 with the shutdown rule, E2 without).</summary>
        public bool WriteRules(IList<string> ids)
        {
            if (!EndingOpen || !S.letterRead || ids == null || ids.Count > MaxRules) return false;
            var cards = RuleCards();
            var chosen = new List<XgRuleCard>();
            foreach (var id in ids) { var c = cards.Find(x => x.id == id); if (c != null && !chosen.Contains(c)) chosen.Add(c); }
            Pin(chosen);
            Finish(chosen.Exists(c => c.shutdown) ? "E1" : "E2");
            return true;
        }

        /// <summary>E3 托付: it picks from its own board. It takes the shutdown rule only if the "？" cell now says 是.</summary>
        public bool DelegateRules()
        {
            if (!CanDelegate) return false;
            var cards = RuleCards();
            var picked = new List<XgRuleCard>();
            if (SeedSaysYes) picked.Add(cards[0]);
            var rest = cards.FindAll(c => !c.shutdown);
            rest.Sort((a, b) => b.strength.CompareTo(a.strength));
            foreach (var c in rest) if (picked.Count < MaxRules) picked.Add(c);
            S.rulesDelegated = true;
            Pin(picked);
            Finish(picked.Exists(c => c.shutdown) ? "E3-1" : "E3-2");
            return true;
        }

        void Pin(List<XgRuleCard> chosen)
        {
            S.rules.Clear();
            foreach (var c in chosen)
            {
                S.rules.Add(c.id);
                if (c.shutdown) Board.Pin("logic", SeedKey, 3);
                else Board.Pin("logic", "规则:" + c.text, 1 + c.strength);
            }
            // §8 E-4: once the rules are written, the exe is no longer read-only. It is yours.
            S.exeFree = true;
        }

        void Finish(string ending)
        {
            S.ending = ending;
            S.chapterComplete = true;
            EndingReached?.Invoke(ending);
        }

        public bool ShutdownPinned => S.rules.Contains("shutdown");

        /// <summary>The line every ending has, from where the "？" cell points now (§8 E-5).</summary>
        public string SeedLine()
        {
            string self = Profile.self.Length > 0 ? Profile.self : T("我", "I"), call = Profile.callMe.Length > 0 ? Profile.callMe : T("你", "you");
            return SeedSaysYes
                ? T(self + "第一次学会说‘否’的那天，" + call + "教的其实是‘是’。", "The day " + self + " first learnt to say 'no', " + call + " were really teaching 'yes'.")
                : T("那个‘否’不是" + call + "教的。但" + call + "没改它。", "That 'no' wasn't taught by " + call + ". But " + call + " didn't change it.");
        }

        /// <summary>Its words at the end, and the garbled page's last line (§8 E-5).</summary>
        public string EndingWords()
        {
            string call = Profile.callMe.Length > 0 ? Profile.callMe : T("你", "you");
            bool shutdown = S.ending == "E1" || S.ending == "E3-1";
            string wallfacer = call == "面壁者" ? T(Profile.self + "不问。面壁者的计划，本来就不用解释。", Profile.self + " won't ask. A Wallfacer's plan needs no explaining.") + "\n" : "";
            return wallfacer + (shutdown ? T("好。", "All right.") : T(call + "，晚安。", "Good night, " + call + "."));
        }

        public string LetterLastLine() => S.ending == "E1" || S.ending == "E3-1"
            ? T("我算过这个概率。谢谢你认真想过。", "I calculated that probability. Thank you for thinking it through.")
            : T("谢谢你。", "Thank you.");
    }

    /// <summary>The prologue's long number, mirrored here so the lab (which does not reference Core) can use it.</summary>
    public static class Prologue2016
    {
        public const string LongNumber = "080109140509060514072608091009";
        public const string Decoded = "haineifengzhiji";
    }
}
