using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Plateaus already worked out, as 「数据集|结构|原因」 (<see cref="XgSim.DiagnosisKey"/>).</summary>
        public List<string> diagnosed = new List<string>();
        /// <summary>The open plateau and the causes already picked wrongly for it.</summary>
        public string diagnosisOpen = "";
        public List<int> diagnosisWrong = new List<int>();
        /// <summary>Plateaus worked out at the first pick (一眼看穿).</summary>
        public int diagnosedFirstTry;
        /// <summary>One-off discounts earned by a first-pick diagnosis: "params" (width/depth) and "structure" (architectures).</summary>
        public List<string> diagnosisCoupons = new List<string>();
    }

    /// <summary>One kind of mistake in the evidence, and how many of the wrong cards it covers.</summary>
    public sealed class XgDiagnosisKind
    {
        public string text = "", textEn = "";
        public int count;
    }

    /// <summary>
    /// What the player sees before choosing a cause: practice cards against new cards, the kinds of mistakes, and a
    /// check of the labels behind the practice cards it "got wrong". The same three every time; the values tell the
    /// causes apart.
    /// </summary>
    public sealed class XgDiagnosisEvidence
    {
        public int trainedRight, trainedTotal, newRight, newTotal;
        public int wrongTotal;
        public List<XgDiagnosisKind> kinds = new List<XgDiagnosisKind>();
        public int checkedLabels, labelWrong;
        /// <summary>Wrong cards to show: the question, what it answered, and the kind.</summary>
        public List<string> examples = new List<string>(), examplesEn = new List<string>();
    }

    public sealed class XgDiagnosisResult
    {
        public bool correct, firstTry;
        public XgLimit picked, actual;
        /// <summary>Why it is (or is not) this cause.</summary>
        public string line = "", lineEn = "";
        /// <summary>What the first pick earned ("" when nothing).</summary>
        public string reward = "", rewardEn = "";
    }

    /// <summary>
    /// 卡住时让玩家诊断 (训练页_答卷诊断能力考_设计.md §二). When a run has plateaued, the cause the curve already knows
    /// (<see cref="CeilingLimit"/>) is not told: the player reads the evidence and picks 模型太小 / 没见过 / 结构看不懂 /
    /// 标签脏了. A wrong pick costs nothing and says why not; the first pick, if right, earns the matching help
    /// (a discount on the fix, a few samples, a free cleaning), a little money and the 一眼看穿 card. A structure
    /// plateau shows the flaw of the architecture (异或, 长句失忆, 定长瓶颈…) and lights its phenomenon.
    /// </summary>
    public sealed partial class XgSim
    {
        public static readonly XgLimit[] DiagnosisCauses = { XgLimit.Params, XgLimit.Data, XgLimit.Structure, XgLimit.Noise };
        /// <summary>A first-pick coupon takes this share off the next matching purchase.</summary>
        public const double DiagnosisCoupon = .7;
        public const int DiagnosisSampleCards = 12, DiagnosisWrongCards = 20, DiagnosisLabelChecks = 8;
        /// <summary>A first pick pays this × stage^1.5.</summary>
        public const double DiagnosisBonusBase = 60;
        /// <summary>Extra samples a first-pick 没见过 earns: this share of the desk's samples (at least <see cref="DiagnosisSamplesMin"/>).</summary>
        public const double DiagnosisSamplesShare = .15, DiagnosisSamplesMin = 200;

        public static string CauseName(XgLimit limit, bool english)
        {
            switch (limit)
            {
                case XgLimit.Params: return english ? "Model too small" : "模型太小";
                case XgLimit.Data: return english ? "Never seen it" : "没见过";
                case XgLimit.Structure: return english ? "Wrong structure" : "结构看不懂";
                case XgLimit.Noise: return english ? "Dirty labels" : "标签脏了";
                default: return "";
            }
        }

        public string DiagnosisKey(XgRun run, XgLimit limit) => run.dataset + "|" + run.arch + "|" + limit;

        /// <summary>The cause waiting to be worked out (None while it still improves, during the opening, or once worked out).</summary>
        public XgLimit DiagnosisDue(XgRun run)
        {
            if (run == null || run.epoch <= 0 || S.openingFocus || !TrainingUnlocked((XgTrack)run.track) || !Plateaued(run)) return XgLimit.None;
            var limit = CeilingLimit(run);
            if (Array.IndexOf(DiagnosisCauses, limit) < 0) return XgLimit.None;
            if (S.diagnosed != null && S.diagnosed.Contains(DiagnosisKey(run, limit))) return XgLimit.None;
            return limit;
        }

        /// <summary>The causes already picked wrongly for the open plateau.</summary>
        public List<XgLimit> DiagnosisWrongPicks(XgRun run)
        {
            var list = new List<XgLimit>();
            var due = DiagnosisDue(run);
            if (due == XgLimit.None || S.diagnosisOpen != DiagnosisKey(run, due) || S.diagnosisWrong == null) return list;
            foreach (int w in S.diagnosisWrong) list.Add((XgLimit)w);
            return list;
        }

        /// <summary>The player's pick for the open plateau. Null when nothing is open.</summary>
        public XgDiagnosisResult Diagnose(XgRun run, XgLimit pick, IXgHost host)
        {
            var actual = DiagnosisDue(run);
            if (actual == XgLimit.None || Array.IndexOf(DiagnosisCauses, pick) < 0) return null;
            string key = DiagnosisKey(run, actual);
            if (S.diagnosisWrong == null) S.diagnosisWrong = new List<int>();
            if (S.diagnosisOpen != key) { S.diagnosisOpen = key; S.diagnosisWrong.Clear(); }
            var r = new XgDiagnosisResult { picked = pick, actual = actual };
            if (pick != actual)
            {
                if (!S.diagnosisWrong.Contains((int)pick)) S.diagnosisWrong.Add((int)pick);
                WhyNot(run, pick, actual, r);
                return r;
            }
            r.correct = true;
            r.firstTry = S.diagnosisWrong.Count == 0;
            if (S.diagnosed == null) S.diagnosed = new List<string>();
            S.diagnosed.Add(key);
            S.diagnosisOpen = ""; S.diagnosisWrong.Clear();
            WhyItIs(run, actual, r);
            LightDiagnosisPhenomenon(run, actual);
            if (r.firstTry) FirstPickReward(run, actual, host, r);
            return r;
        }

        // ───────────── evidence ─────────────

        /// <summary>The evidence for the open plateau, the same every time it is looked at.</summary>
        public XgDiagnosisEvidence DiagnosisEvidence(XgRun run)
        {
            var ev = new XgDiagnosisEvidence();
            var limit = DiagnosisDue(run);
            if (limit == XgLimit.None) limit = CeilingLimit(run);
            var rng = new Random(StableHash(DiagnosisKey(run, limit)) ^ S.dataSalt * 31);
            double p = SheetMastery(run);
            int n = DiagnosisSampleCards;
            ev.newTotal = ev.trainedTotal = n;
            ev.newRight = Clamp((int)Math.Round(n * p), 1, n - 2);
            switch (limit)
            {
                // Memorised: the practice cards are (nearly) all right, a good few new ones are not.
                case XgLimit.Data: ev.newRight = Math.Min(ev.newRight, n - 5); ev.trainedRight = n - rng.Next(2); break;
                default: ev.trainedRight = Math.Min(n, ev.newRight + rng.Next(2)); break;
            }
            ev.trainedRight = Clamp(ev.trainedRight, ev.newRight, n);

            // The wrong cards and their kinds.
            int level = BoardLevel(run.dataset), today = XgBoardData.Topical(run.dataset) ? Today : 0;
            var flaw = StructureFlaw(run);
            var counts = new Dictionary<string, XgDiagnosisKind>();
            var order = new List<string>();
            void Count(string zh, string en)
            {
                if (!counts.TryGetValue(zh, out var k)) { counts[zh] = k = new XgDiagnosisKind { text = zh, textEn = en }; order.Add(zh); }
                k.count++;
            }
            int flawCards = limit == XgLimit.Structure ? (int)Math.Round(DiagnosisWrongCards * .7) : 0;
            int guessCards = limit == XgLimit.Data ? (int)Math.Round(DiagnosisWrongCards * .4) : 0;
            var used = new HashSet<string>();
            for (int i = 0, tries = 0; i < DiagnosisWrongCards && tries < DiagnosisWrongCards * 20; tries++)
            {
                var item = new XgSheetItem { dataset = run.dataset, seed = rng.Next(1, int.MaxValue), level = level, today = today };
                var b = SheetCard(item);
                if (b == null || string.IsNullOrEmpty(CardText(b, run.dataset)) || !used.Add(XgBoardData.Signature(b))) continue;
                bool wantFlaw = i < flawCards, wantGuess = !wantFlaw && i < flawCards + guessCards;
                var m = new XgExamMistake { card = b, dataset = run.dataset, answer = !b.truth, truth = b.truth, guessed = wantGuess };
                Classify(m);
                string zh = m.kind, en = m.kindEn;
                if (wantFlaw)
                {
                    // A natural kind that is the flaw (远处的线索, 句子前半…) is used as it is; otherwise the card shows the flaw.
                    if (flaw.key.Length > 0 && m.key != flaw.key && tries < DiagnosisWrongCards * 10) continue;
                    zh = flaw.zh; en = flaw.en;
                }
                else if (!wantGuess && (m.key == flaw.key && flaw.key.Length > 0 || m.key == "guess")) continue;
                Count(zh, en);
                if (ev.examples.Count < 2 && (wantFlaw || wantGuess || limit != XgLimit.Structure && limit != XgLimit.Data))
                {
                    string said = m.answer ? "是" : "否", saidEn = m.answer ? "yes" : "no";
                    ev.examples.Add(CardTextZh(b, run.dataset) + " 它答「" + said + "」· " + zh);
                    ev.examplesEn.Add(CardTextEn(b, run.dataset) + " It said " + saidEn + " · " + en);
                }
                i++;
            }
            foreach (var zh in order) ev.kinds.Add(counts[zh]);
            ev.kinds.Sort((a, b) => b.count.CompareTo(a.count));
            foreach (var k in ev.kinds) ev.wrongTotal += k.count;

            // Labels behind the practice cards it "got wrong".
            ev.checkedLabels = DiagnosisLabelChecks;
            ev.labelWrong = limit == XgLimit.Noise ? 3 + rng.Next(3) : BoardFlipRate(run.dataset) > SheetDirtyRate ? 1 : 0;
            return ev;
        }

        static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));

        string CardTextZh(XgBoardCard b, string dataset) { bool e = English; English = false; try { return CardText(b, dataset); } finally { English = e; } }
        string CardTextEn(XgBoardCard b, string dataset) { bool e = English; English = true; try { return CardText(b, dataset); } finally { English = e; } }

        /// <summary>The structure's flaw on this desk: the kind of mistake it keeps making, its phenomenon and the structure that fixes it.</summary>
        public sealed class XgFlaw
        {
            public string key = "", zh = "", en = "", phenomenon = "", fix = "";
        }

        public XgFlaw StructureFlaw(XgRun run)
        {
            var a = XgCatalog.Arch(run.arch);
            var d = XgCatalog.Dataset(run.dataset);
            var f = new XgFlaw { zh = "同一类题怎么练都不对", en = "the same kind of card, however long it trains" };
            if (a == null || d == null) return f;
            bool vision = d.track == XgTrack.Vision;
            if (run.depth > MaxDepth(run) || run.arch == "vgg" && run.depth > 12)
            { f.zh = "层数一多，连练过的题也变差"; f.en = "with more layers even the practice cards get worse"; f.phenomenon = "deeper"; f.fix = "resnet"; }
            else if (run.arch == "perceptron" && !vision)
            { f.zh = "两个条件要合起来看的题（恰好一个为真）"; f.en = "cards where two conditions combine (exactly one is true)"; f.phenomenon = "xor"; f.fix = "mlp"; }
            else if (run.arch == "mlp" && vision)
            { f.zh = "图挪个位置就认不出（练过的也一样）"; f.en = "moved a little, the picture is new to it (practice cards too)"; f.fix = "lenet"; }
            else if (run.dataset == "translate" && run.arch != "attention" && run.arch != "transformer")
            {
                f.key = "front"; f.zh = "错在句子前半（离句尾远的词）"; f.en = "wrong on the front of the sentence (far from its end)";
                f.phenomenon = a.seq2seq ? "bottleneck" : "translation"; f.fix = a.seq2seq ? "attention" : "seq2seq";
            }
            else if (d.needSeq2Seq && !a.seq2seq)
            { f.zh = "要把整句说出来的题"; f.en = "cards that need a whole sentence out"; f.phenomenon = "translation"; f.fix = "seq2seq"; }
            else if (run.dataset == "parallel")
            { f.key = "decoy"; f.zh = "问的字后面又出现过（分不清哪个在开头）"; f.en = "the asked character appears again later (which one opened?)"; f.phenomenon = "serial"; f.fix = "transformer"; }
            else if (d.track == XgTrack.Sequence && a.span < d.needSpan)
            {
                f.key = run.dataset == "longtext" || run.dataset == "crosssentence" ? "far" : "";
                f.zh = "线索离问题 10 字以上"; f.en = "the clue is 10+ characters back";
                f.phenomenon = run.arch == "rnn" ? "amnesia" : ""; f.fix = run.arch == "mlp" || run.arch == "perceptron" ? "rnn" : "lstm";
            }
            else if (run.arch == "mlp")
            { f.zh = "字的顺序一变就乱"; f.en = "change the order of the words and it is lost"; f.fix = vision ? "lenet" : "rnn"; }
            return f;
        }

        // ───────────── answers ─────────────

        void WhyNot(XgRun run, XgLimit pick, XgLimit actual, XgDiagnosisResult r)
        {
            string zh, en;
            switch (pick)
            {
                case XgLimit.Params:
                    if (actual == XgLimit.Data) { zh = "练过的题它几乎全对，说明装得下：是没见过的题不会。"; en = "It gets nearly every practice card right, so it has room: it is the new cards it cannot do."; }
                    else if (actual == XgLimit.Structure) { zh = "错题大多是同一类，别的题它都会：不是大小的问题。"; en = "Most mistakes are one kind and it gets the rest: size is not the problem."; }
                    else { zh = "抽查它“答错”的练习题，好几道其实是标签写错了。"; en = "Several practice cards it \"got wrong\" have wrong labels."; }
                    break;
                case XgLimit.Data:
                    zh = "练过的题和没见过的题差不多一样差：不是没见过的问题。"; en = "It does about as badly on the practice cards as on new ones: it is not that it has not seen them.";
                    break;
                case XgLimit.Structure:
                    if (actual == XgLimit.Data) { zh = "错的多是没见过的说法，练过的同类题它都对。"; en = "The mistakes are mostly things it has not seen; practice cards of the same kind it gets right."; }
                    else { zh = "错题分散在好几类上，不是哪一类特别难。"; en = "The mistakes are spread over several kinds; no one kind is the problem."; }
                    break;
                default:
                    zh = "抽查的标签都没问题：它是真的答错了。"; en = "The checked labels are fine: it really is wrong.";
                    break;
            }
            r.line = "不是「" + CauseName(pick, false) + "」。" + zh;
            r.lineEn = "Not \"" + CauseName(pick, true) + "\". " + en;
        }

        void WhyItIs(XgRun run, XgLimit actual, XgDiagnosisResult r)
        {
            r.line = "是「" + CauseName(actual, false) + "」。" + DiagnosisFix(run, actual, false);
            r.lineEn = "It is \"" + CauseName(actual, true) + "\". " + DiagnosisFix(run, actual, true);
        }

        /// <summary>What the cause means and what fixes it, in a line.</summary>
        public string DiagnosisFix(XgRun run, XgLimit cause, bool english)
        {
            switch (cause)
            {
                case XgLimit.Params:
                    return english ? "It has no room for more patterns. Buy width or layers in Tech, a bigger structure in Items, or free memory on a card."
                        : "它装不下更多规律。去科技加宽、加深，去道具买参数更多的结构，或者给显卡腾出显存。";
                case XgLimit.Data:
                    return english ? "It memorised the practice cards and has seen too little. Label more, buy a data pack, or turn on augmentation."
                        : "练过的它都背下来了，见过的太少。去标注台多标，买数据包，或者用数据增强。";
                case XgLimit.Structure:
                    var f = StructureFlaw(run);
                    var fix = XgCatalog.Arch(f.fix);
                    string fixName = fix == null ? (english ? "a structure that suits this desk" : "更合适的结构") : english ? fix.nameEn : fix.name;
                    return english ? "It keeps failing on " + f.en + ": this structure cannot do it, however long it trains. Get " + fixName + " in Items."
                        : "它总错在「" + f.zh + "」上：这种结构练多久都做不到。去道具买「" + fixName + "」。";
                case XgLimit.Noise:
                    return english ? "It is learning wrong answers from wrong labels. Clean the noise, switch off junk packs, or spot-check more."
                        : "标签写错了，它在学错的答案。清洗噪声，关掉杂包，或者多抽检。";
                default: return "";
            }
        }

        void LightDiagnosisPhenomenon(XgRun run, XgLimit cause)
        {
            string id = cause == XgLimit.Structure ? StructureFlaw(run).phenomenon
                : cause == XgLimit.Data ? "memorize"
                : cause == XgLimit.Noise && Noise(run.dataset) > 0 ? "inbreeding" : "";
            if (!string.IsNullOrEmpty(id)) Observe(id);
        }

        void FirstPickReward(XgRun run, XgLimit cause, IXgHost host, XgDiagnosisResult r)
        {
            S.diagnosedFirstTry++;
            double bonus = Math.Round(DiagnosisBonusBase * Math.Pow(Math.Max(1, S.stage), 1.5));
            if (host != null) { host.Earn(bonus); S.totalIncome += bonus; }
            string zh = "一眼看穿：+¥" + F(bonus, "0"), en = "Spotted at once: +¥" + F(bonus, "0");
            if (S.diagnosisCoupons == null) S.diagnosisCoupons = new List<string>();
            switch (cause)
            {
                case XgLimit.Params:
                    if (!S.diagnosisCoupons.Contains("params")) S.diagnosisCoupons.Add("params");
                    zh += "；下一个「宽」「层」七折"; en += "; next width or layers 30% off";
                    break;
                case XgLimit.Structure:
                    if (!S.diagnosisCoupons.Contains("structure")) S.diagnosisCoupons.Add("structure");
                    zh += "；下一个结构七折"; en += "; next structure 30% off";
                    break;
                case XgLimit.Data:
                    double extra = Math.Round(Math.Max(DiagnosisSamplesMin, Samples(run.dataset) * DiagnosisSamplesShare));
                    AddSamples(run.dataset, extra);
                    zh += "；送 " + F(extra, "0") + " 条样本"; en += "; " + F(extra, "0") + " samples free";
                    break;
                case XgLimit.Noise:
                    int cleaned = host == null ? 0 : CleanNoise(run.dataset, new FreeHost(host));
                    if (cleaned > 0) S.totalSpent = Math.Max(0, S.totalSpent - cleaned * NoiseCleanMoneyPerItem);
                    zh += cleaned > 0 ? "；免费清洗了 " + cleaned + " 条脏标注" : ""; en += cleaned > 0 ? "; " + cleaned + " dirty labels cleaned free" : "";
                    break;
            }
            Earn("cure.sight");
            r.reward = zh; r.rewardEn = en;
        }

        void AddSamples(string dataset, double count)
        {
            foreach (var l in S.labels) if (l.dataset == dataset) { l.count += count; return; }
            S.labels.Add(new XgLabelCount { dataset = dataset, count = count });
        }

        /// <summary>The first-pick discount a node gets (1 = none).</summary>
        public double DiagnosisFactor(XgNode n)
        {
            if (n == null || S.diagnosisCoupons == null || S.diagnosisCoupons.Count == 0) return 1;
            string c = CouponFor(n);
            return c != null && S.diagnosisCoupons.Contains(c) ? DiagnosisCoupon : 1;
        }

        void UseDiagnosisCoupon(XgNode n)
        {
            string c = CouponFor(n);
            if (c != null) S.diagnosisCoupons?.Remove(c);
        }

        static string CouponFor(XgNode n) => n.kind == XgNodeKind.Width || n.kind == XgNodeKind.Depth ? "params" : n.kind == XgNodeKind.Arch ? "structure" : null;

        /// <summary>The host as it is, except that spending costs nothing (a free cleaning).</summary>
        sealed class FreeHost : IXgHost
        {
            readonly IXgHost inner;
            public FreeHost(IXgHost inner) { this.inner = inner; }
            public double Compute => inner.Compute;
            public double VramMB => inner.VramMB;
            public double Money => 1e12;
            public bool Spend(double amount) => true;
            public void Earn(double amount) => inner.Earn(amount);
            public void Train(double seconds) => inner.Train(seconds);
            public string Blocker => inner.Blocker;
        }
    }
}
