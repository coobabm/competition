using System;
using System.Collections.Generic;
using System.IO;

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
        /// <summary>The card itself (pictures have no text; the wall page names them with <see cref="XgSim.CardText"/>).</summary>
        public XgBoardCard card;
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

    /// <summary>Ephemeral experiment. Only proposal is editable; no live model references are exposed.</summary>
    public sealed class XgTrialDraft
    {
        internal XgSim owner;
        internal XgState source;
        public XgTrack track { get; internal set; }
        public string dataset { get; internal set; }
        public XgKnobSnapshot proposal;
        internal XgKnobSnapshot original;
        internal XgKnobs baselineKnobs;
        internal XgBoardState baselineBoard;
        internal List<XgBoardCard> pool, diagnostic;
        internal byte[] fingerprint;
        internal bool skipOwned, clipOwned, positionOwned, warmupOwned, attentionOwned, applied;
        internal long cursor;
        internal int today, dataSalt, trainedDepth;
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
            var d = DiagnoseCards(dataset, k, board, TestSet(dataset));
            foreach (var m in d.mistakes) if (m.text.Length == 0 && m.card != null) m.text = CardText(m.card, dataset);
            return d;
        }

        static XgDiagnosis DiagnoseCards(string dataset, XgKnobs k, XgBoard board, List<XgBoardCard> cards)
        {
            var d = new XgDiagnosis { dataset = dataset };
            int right = 0;
            foreach (var card in cards)
            {
                var g = GroupOf(card);
                var group = d.Group(g.id);
                if (group == null) { d.groups.Add(group = g); }
                bool answer = board.Predict(card, k, out _);
                group.total++;
                if (answer == card.truth) { group.right++; right++; }
                else d.mistakes.Add(new XgMistake { text = card.text, answer = answer, truth = card.truth, distance = card.distance, group = g.id, card = card });
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
            if (f.lr != run.lr) list.Add(T("学习率 ", "Rate ") + RateLabel(f.lr) + " → " + RateLabel(run.lr));
            if (f.clip != run.clip) list.Add(T("梯度裁剪 ", "Clipping ") + (run.clip ? T("开", "on") : T("关", "off")));
            if (f.skip != run.skip) list.Add(T("跨层直连 ", "Skip links ") + (run.skip ? T("开", "on") : T("关", "off")));
            if (f.position != run.position) list.Add(T("位置标记 ", "Positions ") + (run.position ? T("开", "on") : T("关", "off")));
            if (f.attnOnly != run.attnOnly) list.Add(T("只用注意力 ", "Attention only ") + (run.attnOnly ? T("开", "on") : T("关", "off")));
            return list;
        }

        /// <summary>
        /// Legacy compatibility comparison for existing callers/tests; the diagnostic UI uses Trial(draft).
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

        public const int MaxTrialCards = 12000;

        /// <summary>Freezes actual effective knobs, a deep board copy and the bounded training/diagnostic pools.</summary>
        public XgTrialDraft BeginTrialDraft(XgTrack track)
        {
            if (track != XgTrack.Vision && track != XgTrack.Sequence) throw new ArgumentOutOfRangeException(nameof(track));
            var run = Run(track);
            if (run.epochActive || run.running) throw new InvalidOperationException(T("先停止自动训练，等本轮结束。", "Stop auto-training and wait for this epoch to finish."));
            var original = Snapshot(run);
            original.act = EffectiveActivation(run);
            var draft = new XgTrialDraft
            {
                owner = this, source = S, track = track, dataset = run.dataset, original = original,
                proposal = CopySnapshot(original), baselineKnobs = Knobs(run).Copy(), baselineBoard = Board.S.Clone(),
                cursor = run.cursor, today = Today, dataSalt = S.dataSalt,
                trainedDepth = ShapeDeferred(run) ? run.formal.depth : run.depth,
                skipOwned = SkipOwned, clipOwned = ClipOwned, positionOwned = PositionOwned,
                warmupOwned = WarmupOwned, attentionOwned = AttentionOnlyOwned,
                pool = new List<XgBoardCard>(), diagnostic = new List<XgBoardCard>(),
            };
            int poolSize = (int)Math.Max(1, Math.Min(XgBoardData.PoolLimit, Samples(run.dataset)));
            for (int i = 0; i < poolSize; i++) draft.pool.Add(PoolCardAt(run, run.cursor + i));
            foreach (var card in TestSet(run.dataset)) draft.diagnostic.Add(CopyCard(card));
            draft.fingerprint = TrialFingerprint(run);
            return draft;
        }

        static XgKnobSnapshot CopySnapshot(XgKnobSnapshot s) => new XgKnobSnapshot
        { set = s.set, arch = s.arch, act = s.act, depth = s.depth, width = s.width, lr = s.lr,
          clip = s.clip, skip = s.skip, position = s.position, warmup = s.warmup, attnOnly = s.attnOnly };

        static XgBoardCard CopyCard(XgBoardCard c)
        {
            var copy = new XgBoardCard { region = c.region, truth = c.truth, seed = c.seed, distance = c.distance, text = c.text, source = c.source };
            foreach (var f in c.features) copy.Add(f.name, f.x, f.y, f.seq);
            return copy;
        }

        static XgKnobs ProposalKnobs(XgTrialDraft draft)
        {
            var p = draft.proposal;
            if (p == null || XgCatalog.Arch(p.arch) == null || p.act < 0 || p.act > 2 || p.depth < 1 || p.depth > 999
                || p.width < 0 || p.width >= XgCatalog.Widths.Length || p.lr < 0 || p.lr >= RateValues.Length)
                throw new ArgumentException("Invalid trial parameters.", nameof(draft));
            var k = draft.baselineKnobs.Copy();
            k.depth = p.depth; k.width = XgCatalog.Widths[p.width] / 2; k.lr = RateValues[p.lr]; k.activation = (XgActivation)p.act;
            k.wiring = p.arch == "attention" && p.attnOnly && draft.attentionOwned ? XgWiring.AnyToAny : WiringOf(p.arch);
            k.skip = draft.skipOwned && (p.skip || p.arch == "resnet" || p.arch == "transformer");
            k.clip = draft.clipOwned && p.clip; k.position = draft.positionOwned && p.position; k.warmup = draft.warmupOwned && p.warmup;
            return k;
        }

        /// <summary>Even a stale draft can reproduce its frozen experiment, but it cannot be applied.</summary>
        public XgTrialResult Trial(XgTrialDraft draft, int cards = TrialCards)
        {
            if (draft == null || draft.owner != this) throw new ArgumentException("Trial belongs to another save.", nameof(draft));
            if (cards < 0 || cards > MaxTrialCards) throw new ArgumentOutOfRangeException(nameof(cards));
            var k = ProposalKnobs(draft);
            var result = new XgTrialResult { dataset = draft.dataset, cards = cards, changes = TrialChanges(draft) };
            result.sameSettings = result.changes.Count == 0;
            var baseline = new XgBoard(draft.baselineBoard.Clone());
            result.now = DiagnoseCards(draft.dataset, draft.baselineKnobs, baseline, draft.diagnostic);
            var proposal = new XgBoard(draft.baselineBoard.Clone());
            // Mirror ApplyShapeChange against the shape that produced the frozen board, not pending live knobs.
            // Width-only Reshape scales run.steps, not board knowledge; a fixed-card trial has no steps to scale.
            string region = RegionOf(draft.dataset);
            if (draft.baselineKnobs.depth != draft.trainedDepth) baseline.Reinitialise(region);
            if (k.depth != draft.trainedDepth) proposal.Reinitialise(region);
            for (int i = 0; i < cards; i++)
            {
                var card = draft.pool[i % draft.pool.Count];
                baseline.Train(card, draft.baselineKnobs);
                if (!result.sameSettings) proposal.Train(card, k);
            }
            result.baseline = DiagnoseCards(draft.dataset, draft.baselineKnobs, baseline, draft.diagnostic);
            result.proposal = result.sameSettings ? result.baseline : DiagnoseCards(draft.dataset, k, proposal, draft.diagnostic);
            return result;
        }

        List<string> TrialChanges(XgTrialDraft draft)
        {
            var a = draft.original; var b = draft.proposal; var changes = new List<string>();
            void Add(bool changed, string zh, string en) { if (changed) changes.Add(T(zh, en)); }
            Add(a.arch != b.arch, "架构", "Architecture"); Add(a.act != b.act, "激活", "Activation");
            Add(a.depth != b.depth, "层数", "Depth"); Add(a.width != b.width, "宽度", "Width"); Add(a.lr != b.lr, "学习率", "Learning rate");
            Add(a.clip != b.clip, "梯度裁剪", "Clipping"); Add(a.skip != b.skip, "跨层直连", "Skip links");
            Add(a.position != b.position, "位置标记", "Positions"); Add(a.warmup != b.warmup, "预热", "Warm-up");
            Add(a.attnOnly != b.attnOnly, "只用注意力", "Attention only"); return changes;
        }

        public bool IsTrialCurrent(XgTrialDraft draft)
        {
            if (draft == null || draft.owner != this || draft.source != S || draft.applied) return false;
            var run = Run(draft.track);
            if (run.running || run.epochActive || run.dataset != draft.dataset) return false;
            var current = TrialFingerprint(run);
            if (current.Length != draft.fingerprint.Length) return false;
            for (int i = 0; i < current.Length; i++) if (current[i] != draft.fingerprint[i]) return false;
            return true;
        }

        // Exact canonical bytes instead of a lossy hash/epoch: catches direct public concept and link writes.
        // O(concepts + links + unlocked + labels), called on actions/refresh, not every frame.
        byte[] TrialFingerprint(XgRun run)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                void Text(string value) { w.Write(value ?? ""); }
                void Snap(XgKnobSnapshot s)
                {
                    w.Write(s != null); if (s == null) return;
                    w.Write(s.set); Text(s.arch); w.Write(s.act); w.Write(s.depth); w.Write(s.width); w.Write(s.lr);
                    w.Write(s.clip); w.Write(s.skip); w.Write(s.position); w.Write(s.warmup); w.Write(s.attnOnly);
                }
                Text(run.dataset); Snap(Snapshot(run)); Snap(run.formal);
                w.Write(run.cursor); w.Write(run.steps); w.Write(run.epoch); w.Write(run.autoLr); w.Write(UseBoard);
                w.Write(Today); w.Write(S.dataSalt); w.Write(BoardLevel(run.dataset));
                w.Write(Samples(run.dataset)); w.Write(Labels(run.dataset)); w.Write(Noise(run.dataset));
                w.Write(DepthCap((XgTrack)run.track)); w.Write(WidthCap((XgTrack)run.track));
                w.Write(S.unlocked.Count); foreach (var id in S.unlocked) Text(id);
                // Preserve all label/noise rows, including duplicate rows in imported saves.
                w.Write(S.labels.Count); foreach (var row in S.labels) { Text(row.dataset); w.Write(row.count); }
                w.Write(S.noise.Count); foreach (var row in S.noise) { Text(row.dataset); w.Write(row.count); }
                var b = Board.S;
                w.Write(b.nextId); w.Write(b.cards); w.Write(b.superposed); w.Write(b.evicted); w.Write(b.created);
                w.Write(b.concepts.Count);
                foreach (var c in b.concepts)
                {
                    w.Write(c.id); Text(c.region); Text(c.key); Text(c.alt); w.Write(c.layer); w.Write(c.w); w.Write(c.s);
                    w.Write(c.seen); w.Write(c.born); w.Write(c.seed); w.Write(c.pinned);
                }
                w.Write(b.links == null ? -1 : b.links.Count);
                if (b.links != null) foreach (var l in b.links) { w.Write(l.a); w.Write(l.b); w.Write(l.c); }
                return stream.ToArray();
            }
        }

        /// <summary>Validate every final value before writing. Existing deferred reshape applies at most once next epoch.</summary>
        public bool TryApplyTrial(XgTrialDraft draft, IXgHost host, out string reason)
        {
            reason = "";
            if (!IsTrialCurrent(draft)) { reason = T("结果已过期，请停止训练并重新试训。", "Result expired. Stop training and start a new trial."); return false; }
            var p = draft.proposal; var run = Run(draft.track);
            if (p == null || !ArchitectureFits(XgCatalog.Arch(p.arch), draft.track) || !Has(p.arch)
                || p.act < 0 || p.act > 2 || !ActivationOwned(p.act)
                || p.depth < 1 || p.depth > DepthCap(draft.track)
                || p.width < 0 || p.width >= XgCatalog.Widths.Length || p.width > WidthCap(draft.track)
                || p.lr < 0 || p.lr >= RateValues.Length || p.lr != run.lr && !HasLrKnob(draft.track)
                || p.clip && !ClipOwned || p.skip && !SkipOwned || p.position && !PositionOwned
                || p.warmup && !WarmupOwned || p.attnOnly && !AttentionOnlyOwned)
            { reason = T("方案包含未解锁或超出上限的参数。", "The proposal contains locked or out-of-range settings."); return false; }
            // Like SetDepth/SetWidth, only a shape that needs more memory than the live one is checked against VRAM,
            // so a smaller or unchanged shape still applies on a host that lost cards since it last trained.
            var shape = new XgRun { arch = p.arch, depth = p.depth, width = p.width };
            double need = VramNeedMB(shape);
            if (host == null || double.IsNaN(Vram(host)) || need > Vram(host) && need > VramNeedMB(run))
            { reason = T("显存不足，正式设置未改变。", "Insufficient VRAM. Live settings are unchanged."); return false; }
            bool archChanged = run.arch != p.arch, depthChanged = run.depth != p.depth, widthChanged = run.width != p.width;
            bool lrChanged = run.lr != p.lr;
            // An untouched activation keeps the run's automatic choice (act = -1), so a later ReLU purchase still upgrades it.
            if (p.act != draft.original.act) run.act = p.act;
            run.arch = p.arch; run.depth = p.depth; run.width = p.width; run.lr = p.lr;
            run.clip = p.clip; run.skip = p.skip; run.position = p.position; run.warmup = p.warmup; run.attnOnly = p.attnOnly;
            if (lrChanged) run.autoLr = false;
            if (archChanged) Restart(run);
            if (!ShapeDeferred(run) && (depthChanged || widthChanged))
            { if (depthChanged) ReinitialiseBoard(run); Reshape(run); }
            else Evaluate(run);
            draft.applied = true;
            return true;
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
            { "combo", new[]
                {
                    "看「训练图式」：曲线一直在五五开附近晃，加宽、多练都没用。", "Open the training map: the curve hovers around a coin toss; more width or more epochs change nothing.",
                    "猜想：没有哪一个条件单独能说明答案，要看两个条件是不是「不一样」。一层只能一个一个条件地算。", "Hypothesis: no single condition gives the answer; it is whether the two differ. One layer can only weigh them one by one.",
                    "两条路：让它自己长出组合（多一层，还要能把误差传回去）；或者人替它把两个条件拼成一个（特征工程，慢一些）。", "Two roads: let it grow combinations itself (another layer that can pass the error back), or have people pair the two conditions for it (feature engineering, slower).",
                }
            },
            { "structure", new[]
                {
                    "看「训练图式」：格子很快就满了，练过的题会、没见过的位置不会。", "Open the training map: the cells fill up fast; trained cards are right, unseen positions wrong.",
                    "猜想：它把每个位置上的每一笔都当成新东西死记，同一个字挪一格就不认识了。", "Hypothesis: it memorises every stroke at every position as something new; move a digit one step and it is a stranger.",
                    "两条路：换一种只看邻近、到处共用的连法（图看邻居，句子看前文）；或者人先把图居中、把句子拆成字和词再喂（特征工程，慢一些）。", "Two roads: a wiring that only looks nearby and is shared everywhere (images look at neighbours, sentences at what came before), or have people centre the pictures and split the sentences first (feature engineering, slower).",
                }
            },
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
