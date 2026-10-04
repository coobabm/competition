using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One group of diagnostic cards (e.g. 远距离线索) and how many the model answers right.</summary>
    public sealed class XgDiagGroup
    {
        public string id = "", name = "", nameEn = "";
        public int right, total;
        public double Rate => total == 0 ? 0 : (double)right / total;
    }

    /// <summary>A diagnostic card the model got wrong: the question, its answer and the right one.</summary>
    public sealed class XgMistake
    {
        public string text = "", group = "";
        public bool answer, truth;
        public int distance;
    }

    /// <summary>What the diagnostic set says about the model: per-group results, the main error and wrong samples.</summary>
    public sealed class XgDiagnosis
    {
        public string dataset = "";
        public double overall;
        public List<XgDiagGroup> groups = new List<XgDiagGroup>();
        public string mainError = "", mainErrorEn = "";
        public List<XgMistake> mistakes = new List<XgMistake>();
        public XgDiagGroup Group(string id) { foreach (var g in groups) if (g.id == id) return g; return null; }
    }

    /// <summary>Training-page knobs as last used for real training (the "before" of a trial).</summary>
    [Serializable]
    public sealed class XgKnobSnapshot
    {
        public bool set;
        public string arch = "";
        public int act = -1, depth, width, lr;
        public bool clip, skip, position, warmup, attnOnly;
    }

    /// <summary>
    /// A side-by-side trial on copies of the model: both start from the same snapshot, see the same training cards
    /// with the same budget, and are judged on the same diagnostic cards. Nothing real changes.
    /// </summary>
    public sealed class XgTrialResult
    {
        public string dataset = "";
        public int cards;
        public XgDiagnosis now, baseline, proposal;
        public List<string> changes = new List<string>();
        public bool sameSettings;
    }

    public sealed partial class XgRun
    {
        /// <summary>The knobs of the last real epoch (trial baseline).</summary>
        public XgKnobSnapshot formal = new XgKnobSnapshot();
    }

    public sealed partial class XgState
    {
        /// <summary>Hints revealed per wall ("wallId#level").</summary>
        public List<string> hintsShown = new List<string>();
        public int trials;
    }

    public sealed partial class XgSim
    {
        public const int TrialCards = 600;
        public const int MistakesShown = 3;

        // ───────────── diagnosis ─────────────

        /// <summary>The diagnostic group a card belongs to. Only real card attributes decide it.</summary>
        public static XgDiagGroup GroupOf(XgBoardCard card)
        {
            if (card.distance >= 0)
            {
                if (card.distance <= 4) return new XgDiagGroup { id = "near", name = "近距离线索（1–4 字）", nameEn = "Near clue (1–4)" };
                if (card.distance <= 9) return new XgDiagGroup { id = "mid", name = "中距离线索（5–9 字）", nameEn = "Middle clue (5–9)" };
                return new XgDiagGroup { id = "far", name = "远距离线索（10 字以上）", nameEn = "Far clue (10+)" };
            }
            return new XgDiagGroup { id = "all", name = "全部", nameEn = "All" };
        }

        public XgDiagnosis Diagnose(string dataset, XgKnobs k, XgBoard board = null)
        {
            board = board ?? Board;
            var d = new XgDiagnosis { dataset = dataset };
            var order = new List<string>();
            int right = 0;
            var cards = TestSet(dataset);
            foreach (var card in cards)
            {
                var g = GroupOf(card);
                var group = d.Group(g.id);
                if (group == null) { d.groups.Add(group = g); order.Add(g.id); }
                bool answer = board.Predict(card, k, out _);
                group.total++;
                if (answer == card.truth) { group.right++; right++; }
                else if (card.text.Length > 0) d.mistakes.Add(new XgMistake { text = card.text, answer = answer, truth = card.truth, distance = card.distance, group = g.id });
            }
            d.groups.Sort((a, b) => Rank(a.id).CompareTo(Rank(b.id)));
            d.overall = cards.Count == 0 ? 0 : (double)right / cards.Count;
            // Show the most telling mistakes first: the far ones, if the model misses those.
            d.mistakes.Sort((a, b) => b.distance.CompareTo(a.distance));
            if (d.mistakes.Count > MistakesShown) d.mistakes.RemoveRange(MistakesShown, d.mistakes.Count - MistakesShown);
            MainError(d);
            return d;
        }

        static int Rank(string id) => id == "near" ? 0 : id == "mid" ? 1 : id == "far" ? 2 : 3;

        static void MainError(XgDiagnosis d)
        {
            var near = d.Group("near"); var far = d.Group("far");
            if (near != null && far != null && near.total > 0 && far.total > 0)
            {
                if (far.Rate < near.Rate - .15) { d.mainError = "主要错误：离得远的条件记不住——近处的答得好，越远越错。"; d.mainErrorEn = "Main error: far conditions are lost — near ones are fine, the further the worse."; return; }
                if (near.Rate < .65) { d.mainError = "主要错误：近处的条件都还没学会。"; d.mainErrorEn = "Main error: even near conditions are not learnt yet."; return; }
                d.mainError = "近、远距离都答得差不多：没有明显的距离问题。"; d.mainErrorEn = "Near and far score alike: no distance problem."; return;
            }
            d.mainError = d.overall < .65 ? "整体还没学会。" : "没有明显短板。";
            d.mainErrorEn = d.overall < .65 ? "Not learnt yet overall." : "No clear weak spot.";
        }

        // ───────────── trials on copies ─────────────

        static XgKnobSnapshot Snapshot(XgRun run) => new XgKnobSnapshot
        {
            set = true, arch = run.arch, act = run.act, depth = run.depth, width = run.width, lr = run.lr,
            clip = run.clip, skip = run.skip, position = run.position, warmup = run.warmup, attnOnly = run.attnOnly,
        };

        XgKnobs KnobsOf(XgRun run, XgKnobSnapshot s)
        {
            if (s == null || !s.set) return Knobs(run);
            var temp = new XgRun
            {
                track = run.track, dataset = run.dataset, arch = s.arch, act = s.act, depth = s.depth, width = s.width, lr = s.lr,
                clip = s.clip, skip = s.skip, position = s.position, warmup = s.warmup, attnOnly = s.attnOnly,
            };
            return Knobs(temp);
        }

        /// <summary>Remembered after every real epoch, so the next trial compares against what the model really used.</summary>
        void RememberFormalKnobs(XgRun run) { run.formal = Snapshot(run); }

        public List<string> KnobChanges(XgRun run)
        {
            var list = new List<string>();
            var f = run.formal;
            if (f == null || !f.set) return list;
            string Arch(string id) { var a = XgCatalog.Arch(id); return a == null ? id : T(a.name, a.nameEn); }
            string[] acts = { T("阶跃", "step"), T("S 形", "S-curve"), "ReLU" };
            int actBefore = f.act >= 0 && ActivationOwned(f.act) ? f.act : BestActivation, actNow = EffectiveActivation(run);
            if (f.arch != run.arch) list.Add(T("架构 ", "Architecture ") + Arch(f.arch) + " → " + Arch(run.arch));
            if (actBefore != actNow) list.Add(T("激活 ", "Activation ") + acts[actBefore] + " → " + acts[actNow]);
            if (f.depth != run.depth) list.Add(T("层数 ", "Layers ") + f.depth + " → " + run.depth);
            if (f.width != run.width) list.Add(T("宽度 ", "Width ") + XgCatalog.Widths[f.width] + " → " + XgCatalog.Widths[run.width]);
            if (f.lr != run.lr) list.Add(T("学习率 ", "Rate ") + XgCatalog.LearningRates[f.lr] + " → " + XgCatalog.LearningRates[run.lr]);
            if (f.clip != run.clip) list.Add(T("梯度裁剪 ", "Clipping ") + (run.clip ? T("开", "on") : T("关", "off")));
            if (f.skip != run.skip) list.Add(T("跨层直连 ", "Skip links ") + (run.skip ? T("开", "on") : T("关", "off")));
            if (f.position != run.position) list.Add(T("位置标记 ", "Positions ") + (run.position ? T("开", "on") : T("关", "off")));
            if (f.attnOnly != run.attnOnly) list.Add(T("只用注意力 ", "Attention only ") + (run.attnOnly ? T("开", "on") : T("关", "off")));
            return list;
        }

        /// <summary>
        /// Trains two copies of the model from the same snapshot on the same cards with the same budget — one with the
        /// settings of the last real epoch, one with the current settings — and diagnoses both on the same unseen
        /// cards. Free and instant; the real model, wallet, clock and story do not change.
        /// </summary>
        public XgTrialResult Trial(XgTrack track, int cards = TrialCards)
        {
            var run = Run(track);
            var result = new XgTrialResult { dataset = run.dataset, cards = cards };
            var samples = new List<XgBoardCard>(cards);
            long cursor = run.cursor;
            for (int i = 0; i < cards; i++) samples.Add(PoolCardAt(run, cursor + i));
            var baseKnobs = KnobsOf(run, run.formal);
            var nowKnobs = Knobs(run);
            result.changes = KnobChanges(run);
            result.sameSettings = result.changes.Count == 0;
            result.now = Diagnose(run.dataset, baseKnobs);
            result.baseline = Diagnose(run.dataset, baseKnobs, TrainCopy(samples, baseKnobs, RegionOf(run.dataset)));
            result.proposal = result.sameSettings ? result.baseline : Diagnose(run.dataset, nowKnobs, TrainCopy(samples, nowKnobs, RegionOf(run.dataset), nowKnobs.depth != baseKnobs.depth));
            S.trials++;
            return result;
        }

        XgBoard TrainCopy(List<XgBoardCard> samples, XgKnobs k, string region, bool reinitialise = false)
        {
            var copy = new XgBoard(S.board.Clone());
            if (reinitialise) copy.Reinitialise(region);
            foreach (var card in samples) copy.Train(card, k);
            return copy;
        }

        // ───────────── layered hints (design proposal: phenomenon → hypothesis → experiment → reference) ─────────────

        public static readonly Dictionary<string, string[]> Hints = new Dictionary<string, string[]>
        {
            { "length", new[]
                {
                    "看诊断：错误集中在离得远的条件上，近处的几乎都对。", "Look at the diagnosis: the errors sit on far conditions; near ones are almost all right.",
                    "猜想：重要的信息在一个字一个字往后传的路上丢了，传得越远丢得越多。", "Hypothesis: the important bit is lost as it is passed along character by character; the further, the more is lost.",
                    "实验：试试一条能长期携带信息的路径（门控记忆）。一次只改这一项，用试训对比远距离那一行。", "Experiment: try a path that can carry information for long (gated memory). Change only that, and compare the far row in a trial.",
                }
            },
        };

        public int HintLevel(string wallId) { int n = 0; while (S.hintsShown.Contains(wallId + "#" + (n + 1))) n++; return n; }

        /// <summary>Reveals the next free hint (the reference setting itself stays behind the secret).</summary>
        public bool RevealHint(string wallId)
        {
            if (!Hints.TryGetValue(wallId, out var hints)) return false;
            int level = HintLevel(wallId);
            if (level >= hints.Length / 2) return false;
            S.hintsShown.Add(wallId + "#" + (level + 1));
            return true;
        }

        public string HintText(string wallId, int level)
        {
            if (!Hints.TryGetValue(wallId, out var hints) || level < 1 || level > hints.Length / 2) return "";
            return T(hints[(level - 1) * 2], hints[(level - 1) * 2 + 1]);
        }
    }
}
