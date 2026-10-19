using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// 灵光: an incremental game about training CNNs and RNNs. Pure rules, no Unity.
    /// Loop: label yes/no cards (¥ + samples) → press 训练一轮 (one epoch) → every few epochs an assessment scores the
    /// model 0–1000 and pays for new records → spend ¥ in the tech tree (architectures, layers, width, rates, data,
    /// research). One combo counter spans labelling and training; only hand actions build it.
    ///
    /// The curve is a stylised scaling law:
    ///   capacity error  ∝ bias · (scale / params)^0.6, plus depth / memory / encoder / data penalties
    ///   training error  = floor + (start − floor) · (1 + steps/τ)^−2
    ///   validation      = training + overfit, overfit ∝ (params / samples)^0.8 · ln(1 + steps / 4τ)
    /// so validation accuracy rises, peaks and slowly falls; assessments keep the best checkpoint.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int HistoryLength = 160;
        public const int LogLength = 40;
        /// <summary>GPU-seconds of training in one epoch (a hand press at 0.6 s cadence ≈ continuous training).</summary>
        public const double EpochSeconds = .6;
        /// <summary>A new record must beat the old one by this much on yes/no cards (about one standard error of the test set).</summary>
        public const double RecordMargin = .02;
        /// <summary>Seconds a hand round keeps the combo alive, counted from the end of the round (the timer holds while a hand round runs).</summary>
        public const double EpochComboWindow = 1.5;
        /// <summary>Chance of a 前方高能 card (tests set 0 for exact pay).</summary>
        public double GoldChance = .05;
        public const int DuelLength = 5;
        /// <summary>¥ per score point gained on a record, × the dataset's rewardBase.</summary>
        public const double RewardPerPoint = .2;

        public XgState S { get; private set; }
        public bool English;
        /// <summary>Game date (yyyymmdd). Phrases dated later stay hidden.</summary>
        public int Today { get => today; set { today = value; TodayKnown = true; } }
        int today = 20160601;
        /// <summary>
        /// The host has told the lab the real game date. Until then <see cref="Today"/> is only a placeholder, so
        /// anything keyed to the calendar (the monthly meme drift) waits instead of reading the wrong month.
        /// </summary>
        public bool TodayKnown { get; private set; }
        /// <summary>The 灵光 window is open: card timers only run then.</summary>
        public bool WindowOpen;
        /// <summary>灵光 game clock (seconds) for model timestamps.</summary>
        public double Clock;
        /// <summary>Unstarred, undeployed models beyond this many are cleaned up, oldest first.</summary>
        public const int MaxModels = 80;
        public event Action<XgModelEntry> ModelSaved;

        public event Action<string> Message;
        /// <summary>Training tore into NaN. Never raised since the model configures its own rate (drops replaced it); kept for the view's subscription.</summary>
#pragma warning disable CS0067
        public event Action<int> Diverged;
#pragma warning restore CS0067
        public event Action<XgEpoch> EpochDone;
        public event Action<XgAssessment> Assessed;
        public event Action<XgNode> NodeBought;
        /// <summary>Combo crossed tier i (10, 25, 50, 100).</summary>
        public event Action<int> ComboTier;
        /// <summary>Combo of this size (≥ 3) was lost.</summary>
        public event Action<int> ComboBroken;
        public event Action<XgDesk> DeskOpened;
        /// <summary>The visible card ran out of time (前方高能 / 斗图).</summary>
        public event Action<string> CardTimedOut;
        /// <summary>斗图 finished: (all right, bonus ¥).</summary>
        public event Action<bool, double> DuelDone;

        public XgSim(XgState state = null)
        {
            S = state ?? new XgState();
            RepairBoardSelfPlayPlaceholders();
            S.endingPreparation?.RepairOptionalPlaceholders();
            S.releaseOutcome?.RepairOptionalPlaceholders();
            InitializeBookLibrary(state == null);
            // Data sources first: everything below evaluates runs, which reads samples (XgSim.DataSources.cs).
            RepairDataSources();
            RepairTraces();
            RepairFlywheel();
            PrepareProgression(state == null);
            Repair();
            FinishProgressionRepair();
            FinishCollaborationRepair();
            RepairEpiphany();
            RepairMarket();
            RepairAfterthoughts();
            RepairAlignment();
            RepairMemoryBook();
            RepairKnowledge();
            RepairWiring();
            RepairConsumablesRetired();
            RepairAutoRetired();
            RepairRelics();
            EnsureSeed();
            RepairStagePresentations();
            RepairLedgerAccess(state == null);
            InitializeAppAcquisition(state == null);
        }

        void Repair()
        {
            PrepareCollaboration();
            if (S.vision == null) S.vision = new XgState().vision;
            if (S.sequence == null) S.sequence = new XgState().sequence;
            S.vision.track = 0; S.sequence.track = 1;
            if (S.unlocked == null) S.unlocked = new List<string>();
            if (S.owned == null) S.owned = new List<string>();
            if (S.labels == null) S.labels = new List<XgLabelCount>();
            if (S.cards == null) S.cards = new List<XgCard>();
            if (S.desksOpen == null) S.desksOpen = new List<string>();
            if (S.autoLevels == null) S.autoLevels = new List<XgLabelCount>();
            if (S.autoTimers == null) S.autoTimers = new List<XgLabelCount>();
            if (S.contracts == null) S.contracts = new List<string>();
            // Retired work cannot remain active through saved selections, cards or signed orders.
            S.contracts.RemoveAll(id => XgCatalog.Contract(id) == null);
            S.desksOpen.RemoveAll(id => XgCatalog.Desk(id) == null);
            S.cards.RemoveAll(c => c == null || XgCatalog.Desk(c.dataset) == null);
            S.autoTimers.RemoveAll(c => c == null || XgCatalog.Desk(c.dataset) == null);
            if (S.best == null) S.best = new List<XgBest>();
            if (S.grades == null) S.grades = new List<string>();
            if (S.models == null) S.models = new List<XgModelEntry>();
            foreach (var m in S.models) { if (m.curve == null) m.curve = new List<float>(); if (m.id >= S.nextModelId) S.nextModelId = m.id + 1; }
            if (S.nextModelId < 1) S.nextModelId = 1;
            // Checkpoints from before the repository existed get an entry, so the deployed model shows up there.
            foreach (var b in S.best)
            {
                if (b.acc <= 0 || Model(b.modelId) != null) continue;
                var d = XgCatalog.Dataset(b.dataset);
                if (d == null) continue;
                var same = Run(d.track).dataset == b.dataset ? Run(d.track) : null;
                var e = new XgModelEntry
                {
                    id = S.nextModelId++, track = (int)d.track, dataset = b.dataset, arch = string.IsNullOrEmpty(b.arch) ? (d.track == XgTrack.Vision ? "lenet" : "rnn") : b.arch,
                    depth = same != null ? same.depth : 1, width = same != null ? same.width : 0, lr = same != null ? same.lr : DefaultLr(d.track),
                    epoch = same != null ? same.epoch : 0, steps = same != null ? same.steps : 0, acc = b.acc, trainAcc = b.acc, score = Score(b.dataset, b.acc), record = true,
                };
                e.name = FileName(e);
                S.models.Insert(0, e);
                b.modelId = e.id;
            }
            if (S.log == null) S.log = new List<string>();
            if (S.rng == 0) S.rng = 0x5EED5EEDL;
            // First-layout fields: auto-answer per track and the desk index.
            if (S.autoVision > 0) { SetCount(S.autoLevels, "mnist", S.autoVision); S.autoVision = 0; }
            if (S.autoSequence > 0) { SetCount(S.autoLevels, "poems", S.autoSequence); S.autoSequence = 0; }
            if (S.autoLogic > 0) { SetCount(S.autoLevels, "logic", S.autoLogic); S.autoLogic = 0; }
            // Supplemental teaching desks may have no legacy XgDesk. Restore the
            // selected, available pack before applying legacy-save fallbacks.
            var teachingPack = XgTeachingPacks.Find(S.teachingPack);
            bool restoreTeachingPack = teachingPack != null && TeachingPackAvailable(teachingPack.id);
            if (restoreTeachingPack) S.desk = teachingPack.dataset;
            if (!restoreTeachingPack && (string.IsNullOrEmpty(S.desk) || XgCatalog.Desk(S.desk) == null))
                S.desk = XgCatalog.Desks[Math.Max(0, Math.Min(2, S.labelTrack))].id;
            S.visionCard = S.sequenceCard = S.logicCard = null;
            foreach (var d in XgCatalog.Desks) if (d.unlock == XgDeskUnlock.Start && !S.desksOpen.Contains(d.id) && !OpeningLocksDesk(d.id)) S.desksOpen.Add(d.id);
            if (!restoreTeachingPack && !DeskOpen(S.desk)) S.desk = S.openingFocus ? XgMemes.SenseDesk : "logic";
            S.selected = S.selected == 1 ? 1 : 0;
            S.combo = Math.Max(0, S.combo);
            // The first version sold a one-off ×2 raise as a research node; it maps to raise level 5 (×2).
            if (S.unlocked.Remove("manual_pay")) S.payRaise = Math.Max(S.payRaise, 5);
            S.payRaise = Math.Max(0, Math.Min(XgCatalog.RaiseMax, S.payRaise));
            // Training without knobs: a knob-era save keeps every accuracy it had (XgSim.AutoModel.cs).
            if (S.trainingVersion < TrainingSchemaAuto) MigrateToAutoTraining();
            foreach (var run in Runs)
            {
                // Unfinished UI actions have no settlement and do not survive a save reload.
                RefundTrainingQuizBonus(run);
                run.epochActive = false; run.epochProgress = 0; run.epochDuration = DurationFor(run);
                var track = (XgTrack)run.track;
                var a = XgCatalog.Arch(run.arch);
                if (!ArchitectureFits(a, track) || !Has(run.arch)) run.arch = DefaultArchitecture(track);
                var d = XgCatalog.Dataset(run.dataset);
                if (d == null || d.track != track || !DatasetAvailable(run.dataset)) run.dataset = track == XgTrack.Vision ? "mnist" : "spam";
                run.depth = Math.Max(1, Math.Min(DepthCap(track), run.depth));
                run.width = Math.Max(0, Math.Min(WidthCap(track), run.width));
                run.lr = Math.Max(0, Math.Min(XgCatalog.LearningRates.Length - 1, run.lr));
                if (run.accs == null) run.accs = new List<XgScore>();
                run.accs.RemoveAll(x => x == null || XgCatalog.Dataset(x.key) == null || XgCatalog.Dataset(x.key).track != track);
                if (double.IsNaN(run.steps) || run.steps < 0) run.steps = 0;
                if (run.histTrain == null) run.histTrain = new List<float>();
                if (run.histVal == null) run.histVal = new List<float>();
                Evaluate(run);
            }
        }

        public XgRun[] Runs { get { return new[] { S.vision, S.sequence }; } }
        public XgRun Run(XgTrack track) { return track == XgTrack.Vision ? S.vision : S.sequence; }
        public XgTrack SelectedTrack { get { return S.selected == 1 ? XgTrack.Sequence : XgTrack.Vision; } set { S.selected = value == XgTrack.Sequence ? 1 : 0; } }
        public XgRun Selected { get { return Run(SelectedTrack); } }
        public static string TreeOf(XgTrack track) { return track == XgTrack.Vision ? "vision" : "sequence"; }

        string T(string zh, string en) { return English ? en : zh; }
        /// <summary>Chinese source text; the English comes from the table in Core/Lang.En.cs (a line it lacks stays Chinese).</summary>
        string T(string zh) { return English ? LingGuangV05.Core.Lang.En(zh) : zh; }
        static string F(double v, string format) { return v.ToString(format, CultureInfo.InvariantCulture); }
        public static string Pct(double v) { return F(v * 100, v >= .995 ? "0.00" : "0.0") + "%"; }

        // ───────────── tech tree ─────────────

        public bool Has(string id) { return id != null && id != PersonaArchiveNode && id.StartsWith("persona.", StringComparison.Ordinal) ? OwnsPersonaPack(id.Substring(8)) : S.unlocked.Contains(id); }
        /// <summary>Some pack of this dataset is in: the public pack, or a junk / story pack (XgSim.DataSources.cs).</summary>
        public bool Owns(string datasetId) { return S.owned.Contains(datasetId) && InstalledBatches(datasetId) > 0 || OwnsExtraPack(datasetId); }

        public enum NodeStatus { Owned, Buyable, TooExpensive, Locked }

        public NodeStatus Status(XgNode n, IXgHost host)
        {
            if (n == null) return NodeStatus.Locked;
            if (IsAtlas(n)) return AtlasLit(n) ? NodeStatus.Owned : NodeStatus.Locked;
            if (n.tree == "label") return StatusLabelNode(n, host);
            if (AnyEpochActive && (n.kind == XgNodeKind.Breakthrough || n.kind == XgNodeKind.Project)) return NodeStatus.Locked;
            if (Has(n.id)) return NodeStatus.Owned;
            if (!NodeVisible(n) || BoardLogicBlocker(n.id) != null || BoardRecordsBlocker(n.id) != null || DailyNodeBlocker(n.id) != null || ProgressionBlocker(n) != null || !ChatNodeReady(n.id)) return NodeStatus.Locked;
            if (n.parent != null && !Has(n.parent)) return NodeStatus.Locked;
            foreach (var need in n.needs) if (!Has(need)) return NodeStatus.Locked;
            if (host == null || host.Money + 1e-9 < NodeCost(n)) return NodeStatus.TooExpensive;
            return NodeStatus.Buyable;
        }

        public string Why(XgNode n, IXgHost host)
        {
            if (n == null) return T("未知节点");
            if (n.tree == "label") return WhyLabelNode(n, host);
            if (IsAtlas(n)) return AtlasLit(n) ? T("已点亮") : n.kind == XgNodeKind.Ability ? T("它学会时自动点亮") : T("第一次发生时自动点亮");
            switch (Status(n, host))
            {
                case NodeStatus.Owned: return T("已拥有");
                case NodeStatus.Locked:
                    if (AnyEpochActive && (n.kind == XgNodeKind.Breakthrough || n.kind == XgNodeKind.Project)) return T("先完成当前训练轮次");
                    var boardLogic = BoardLogicBlocker(n.id);
                    if(boardLogic != null)return boardLogic;
                    var board = BoardRecordsBlocker(n.id);
                    if(board != null)return board;
                    var daily = DailyNodeBlocker(n.id);
                    if (daily != null) return daily;
                    var progression = ProgressionBlocker(n);
                    if (progression != null) return progression;
                    if (!ChatNodeReady(n.id)) return T("对它说一句话：____。", "Say something to it: ____. ") + ChatNodeHint(n.id);
                    var missing = new List<string>();
                    if (n.parent != null && !Has(n.parent)) missing.Add(NodeName(XgCatalog.Node(n.parent)));
                    foreach (var need in n.needs) if (!Has(need)) missing.Add(NodeName(XgCatalog.Node(need)));
                    return T("先解锁 ") + string.Join(T("、"), missing);
                case NodeStatus.TooExpensive: return T("经费不足 ¥") + F(NodeCost(n), "0");
                default: return T("按住购买 ¥") + F(NodeCost(n), "0");
            }
        }

        /// <summary>「科技树买「RNN」，再买「LSTM」解锁」: the unowned nodes on the way to <paramref name="nodeId"/>, oldest first.</summary>
        public string UnlockPath(string nodeId)
        {
            var path = new List<string>();
            CollectUnowned(XgCatalog.Node(nodeId), path, new HashSet<string>());
            if (path.Count == 0) return T("科技树里解锁", "Unlock it in the tech tree");
            var names = new List<string>(); foreach (var id in path) names.Add(T("「", "\"") + NodeName(XgCatalog.Node(id)) + T("」", "\""));
            return T("科技树买", "Tech tree: buy ") + string.Join(T("，再买", ", then "), names) + T(" 解锁", " to unlock");
        }
        void CollectUnowned(XgNode n, List<string> path, HashSet<string> seen)
        {
            if (n == null || Has(n.id) || !seen.Add(n.id)) return;
            if (!string.IsNullOrEmpty(n.parent)) CollectUnowned(XgCatalog.Node(n.parent), path, seen);
            foreach (var need in n.needs) CollectUnowned(XgCatalog.Node(need), path, seen);
            path.Add(n.id);
        }

        public string NodeName(XgNode n) { return n == null ? "?" : NodeMystery(n) ? T("？？？") : n.id == "label.ai" ? AiDisplayName + T("代答", " answers") : n.id == "label.brain" ? AiDisplayName + T("的大脑", "'s brain") : T(n.name, n.nameEn); }
        /// <summary>The node's description; a hint instead while it is still a mystery.</summary>
        public string NodeNote(XgNode n) { return n == null ? "" : NodeMystery(n) ? T("也许有更省力的办法……") : T(n.note, n.noteEn); }
        /// <summary>Shown as 「？？？」 with no price: 自动答题 before the protagonist has the idea.</summary>
        public bool NodeMystery(XgNode n) { return n != null && n.id == "label.auto" && AutoLabelHidden; }

        public bool BuyNode(string id, IXgHost host)
        {
            var n = XgCatalog.Node(id);
            if (n == null) return false;
            if (n.id!=PersonaArchiveNode&&n.id.StartsWith("persona.",StringComparison.Ordinal)) return BuyPersonaPack(n.id.Substring(8),host);
            if (n.tree == "label") return BuyLabelNode(n, host);
            if (Status(n, host) != NodeStatus.Buyable) { Say(Why(n, host)); return false; }
            double price = NodeCost(n);
            if (!host.Spend(price)) { Say(T("经费不足 ¥") + F(NodeCost(n), "0")); return false; }
            S.totalSpent += price;
            if (n.id != PersonaArchiveNode) UseDiagnosisCoupon(n);
            if (n.kind != XgNodeKind.Project) S.unlocked.Add(n.id);
            Say(T("解锁 ") + NodeName(n));
            switch (n.kind)
            {
                case XgNodeKind.Dataset:
                    QueueTeachingData(n.target, n.id);
                    break;
            }
            ApplyProgressionNode(n);
            ApplyTeachingPackPurchase(n);
            // A new structure, width, depth or technique: the models reconfigure themselves (XgSim.AutoModel.cs).
            AutoConfigureAll(host);
            foreach (var run in Runs) Evaluate(run);
            CheckDesks();
            NodeBought?.Invoke(n);
            return true;
        }

        public int DepthCap(XgTrack track)
        {
            int cap = ProgressionDepthCap(track);
            string tree = TreeOf(track);
            foreach (var n in XgCatalog.Nodes) if (n.kind == XgNodeKind.Depth && (n.tree == tree || n.tree == "trunk") && Has(n.id)) cap = Math.Max(cap, n.value);
            return cap;
        }

        public int WidthCap(XgTrack track)
        {
            int cap = 0;
            string tree = TreeOf(track);
            foreach (var n in XgCatalog.Nodes) if (n.kind == XgNodeKind.Width && (n.tree == tree || n.tree == "trunk") && Has(n.id)) cap = Math.Max(cap, n.value);
            return cap;
        }

        public bool HasLrKnob(XgTrack track) { return Has("shared.lr") || Has("v.lr") || Has("s.lr"); }
        public static int DefaultLr(XgTrack track) { return 2; }

        /// <summary>Every epoch ends with an exam on unseen cards (a record saves the checkpoint and pays).</summary>
        public int EvalEvery => 1;
        /// <summary>Assessments in a row without a record before the hint.</summary>
        public const int StaleHintEpochs = 12;

        public XgResearch Optimizer
        {
            get
            {
                XgResearch best = null;
                foreach (var r in XgCatalog.Research)
                    if (r.kind == XgResearchKind.Optimizer && Has(r.id) && (best == null || r.speed > best.speed)) best = r;
                return best;
            }
        }
        public string OptimizerName { get { var o = Optimizer; return o == null ? "SGD" : T(o.name, o.nameEn); } }

        /// <summary>
        /// The number a rate button shows. Adaptive optimisers (RMSProp, Adam) divide each step by the gradient's own
        /// size, so the same step needs a far smaller number: Adam's usual 0.001 is SGD's 0.1. The board trains on the
        /// step size (<see cref="RateValues"/>); only the label follows the optimiser.
        /// </summary>
        public double RateDisplayScale { get { var o = Optimizer; return o != null && (o.id == "adam" || o.id == "rmsprop") ? .01 : 1; } }

        public string RateLabel(int index)
        {
            int i = Math.Max(0, Math.Min(RateValues.Length - 1, index));
            return (RateValues[i] * RateDisplayScale).ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>How much more an optimiser tolerates a big step before it tears (adaptive steps are a little steadier).</summary>
        public double OptimizerSteadiness { get { var o = Optimizer; return o == null ? 1 : o.id == "adam" ? 1.25 : o.id == "rmsprop" ? 1.15 : 1; } }
        double OptSpeed { get { var o = Optimizer; return o == null ? 1 : o.speed; } }
        public double Stability { get { var o = Optimizer; return (o == null ? 1 : o.stability) * (Has("batchnorm") ? 1.5 : 1); } }
        double SpeedResearch { get { return (Has("batchnorm") ? 1.2 : 1) * (Has("cudnn") ? 1.3 : 1) * FeelSpeed; } }

        // ───────────── model shape ─────────────

        public static double ParamsK(XgRun run)
        {
            var a = XgCatalog.Arch(run.arch);
            double w = XgCatalog.Widths[run.width];
            return run.depth * w * w * (a == null ? 1 : a.paramFactor) / 1000.0;
        }

        /// <summary>
        /// Training memory on one card: the framework and CUDA context (~300 MB), 16 bytes per parameter (float32
        /// weights, gradients and two Adam moments) and the activations kept for the backward pass (batch 32; feature
        /// maps make convolutions the hungriest).
        /// </summary>
        public static double VramNeedMB(XgRun run)
        {
            var a = XgCatalog.Arch(run.arch);
            double w = XgCatalog.Widths[Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, run.width))];
            double perUnit = a != null && a.track == XgTrack.Vision && run.arch != "caption" ? .12 : .02;
            return 300 + ParamsK(run) * .016 + Math.Max(1, run.depth) * w * perUnit;
        }

        /// <summary>Parameter count (in thousands) of a run with another depth or width, for previews.</summary>
        public static double ParamsKWith(XgRun run, int depth, int width)
        {
            return ParamsK(new XgRun { arch = run.arch, depth = Math.Max(1, depth), width = Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, width)) });
        }

        /// <summary>Well-known models up to 2016 and their published parameter counts (thousands), for a sense of scale.</summary>
        static readonly (string zh, string en, double k)[] ParamLandmarks =
        {
            ("LeNet-5（1998）", "LeNet-5 (1998)", 60),
            ("ResNet-50（2015）", "ResNet-50 (2015)", 25600),
            ("AlexNet（2012）", "AlexNet (2012)", 61000),
            ("VGG-16（2014）", "VGG-16 (2014)", 138000),
            ("谷歌翻译 GNMT（2016）", "Google's GNMT (2016)", 278000),
        };

        /// <summary>
        /// The parameter count next to the nearest famous model: 「约 LeNet-5（1998）的 1/7」, "about 2× AlexNet (2012)".
        /// Purely a sense of scale; the game's own formula decides what the model can learn.
        /// </summary>
        public static string ParamScale(double paramsK, bool english)
        {
            if (!(paramsK > 0)) return "";
            var best = ParamLandmarks[0];
            foreach (var m in ParamLandmarks)
                if (Math.Abs(Math.Log(paramsK / m.k)) < Math.Abs(Math.Log(paramsK / best.k))) best = m;
            double ratio = paramsK / best.k;
            string name = english ? best.en : best.zh;
            if (ratio >= 1.5) { string n = ratio >= 10 ? ratio.ToString("0") : ratio.ToString("0.#"); return english ? "about " + n + "× " + name : "约 " + name + " 的 " + n + " 倍"; }
            if (ratio <= 1 / 1.5) { string n = (1 / ratio).ToString("0"); return english ? "about 1/" + n + " of " + name : "约 " + name + " 的 1/" + n; }
            return english ? "about the size of " + name : "和 " + name + " 差不多大";
        }

        public int MaxDepth(XgRun run)
        {
            var a = XgCatalog.Arch(run.arch);
            if (a == null) return 5;
            if (a.maxDepth >= 999) return 999;
            return a.maxDepth + (Has("batchnorm") ? 6 : 0);
        }

        static XgRun Shape(XgRun run, int depth, int width)
        { return new XgRun { track = run.track, arch = run.arch, dataset = run.dataset, depth = depth, width = width, lr = run.lr }; }

        /// <summary>
        /// 迁移学习 on the board: the run's region keeps its raw concepts under the new wiring's names (the place a stroke
        /// or word sat is dropped). A wiring that binds every position (fully connected, or one head with position
        /// tags) has no place-free names, so nothing carries over into it.
        /// </summary>
        int CarryConcepts(XgRun run)
        {
            var k = Knobs(run);
            bool sequence = RegionOf(run.dataset) == "sequence";
            bool bound = k.wiring == XgWiring.Full || k.wiring == XgWiring.AnyToAny && k.position && !k.multiHead || k.wiring == XgWiring.LocalShared && sequence && k.position;
            if (bound) { Board.Reinitialise(RegionOf(run.dataset)); return 0; }
            return Board.CarryOver(RegionOf(run.dataset), key =>
            {
                if (key.IndexOf('+') >= 0) return null;
                int at = key.IndexOf('@');
                string name = at >= 0 ? key.Substring(0, at) : key;
                return name.StartsWith("^", StringComparison.Ordinal) && k.wiring != XgWiring.Recurrent && k.wiring != XgWiring.GatedRecurrent ? null : name;
            });
        }

        /// <summary>Bought datasets, and hand-labelled ones whose desk is open (mnist / poems / logic from the start).</summary>
        public bool DatasetAvailable(string id)
        {
            var d = XgCatalog.Dataset(id);
            if (d == null) return false;
            if (Owns(id)) return true;
            // Free milestone desks migrated to a pack unlock, not pack ownership. Preserve their existing
            // training access without restoring paid tasks or inventing the full pack's sample count.
            if ((id == "translate" || id == "crosssentence") && (FreeDataAccess(id) || InstalledBatches(id) > 0)) return true;
            return d.handLabel && (XgCatalog.Desk(id) == null || DeskOpen(id));
        }

        /// <summary>
        /// Which dataset a track trains on (the one choice left on the training page). The model keeps its accuracy on
        /// every dataset it has trained; switching back finds it where it was.
        /// </summary>
        public bool SetDataset(XgTrack track, string id)
        {
            if (Run(track).epochActive) return false;
            var d = XgCatalog.Dataset(id);
            var run = Run(track);
            if (d == null || d.track != track || !DatasetAvailable(id) || run.dataset == id) return false;
            Remember(run);
            run.dataset = id;
            run.valAcc = StoredAcc(run, id);
            run.lastGain = 0;
            run.sinceEval = 0; run.staleEvals = 0;
            run.histTrain.Clear(); run.histVal.Clear();
            Evaluate(run);
            return true;
        }


        // ───────────── the curve ─────────────

        public double Tau(XgRun run)
        {
            var d = XgCatalog.Dataset(run.dataset);
            return 250 * d.complexity * Math.Pow(1 + ParamsK(run), .3);
        }

        /// <summary>Error fraction (0 = dataset floor, 1 = chance) the model converges to.</summary>
        public double CapacityFraction(XgRun run, int lr)
        {
            var a = XgCatalog.Arch(run.arch);
            var d = XgCatalog.Dataset(run.dataset);
            int over = Math.Max(0, run.depth - MaxDepth(run));
            double neff = ParamsK(run) * Math.Pow(Has("relu") ? .85 : .75, over);
            double frac = a.bias * .25 * Math.Pow(d.scale / Math.Max(1e-6, neff), .6);
            frac += over * .03;
            frac += .05 * Math.Pow(d.need / SamplesEffective(run), .6);
            if (d.track == XgTrack.Sequence)
            {
                if (a.span < d.needSpan) frac += .35 * (1 - (double)a.span / d.needSpan);
                if (d.needSeq2Seq && !a.seq2seq) frac += .5;
            }
            frac *= XgCatalog.LrFinal[lr];
            return Math.Max(0, Math.Min(.97, frac));
        }

        public double SamplesEffective(XgRun run)
        {
            var d = XgCatalog.Dataset(run.dataset);
            return EffectiveLabelSamples(d.id) * AugmentFactor(d.id);
        }

        public double OverfitScale(XgRun run)
        {
            var d = XgCatalog.Dataset(run.dataset);
            int over = Math.Max(0, run.depth - MaxDepth(run));
            double neff = ParamsK(run) * Math.Pow(Has("relu") ? .85 : .75, over);
            double ratio = neff * 1000 / SamplesEffective(run);
            return (d.chanceError - d.floorError) * .0015 * Math.Pow(ratio, .8) * (Has("dropout") ? .5 : 1);
        }

        /// <summary>
        /// Refreshes what follows from the stored accuracy: the ceiling the model can reach now, the training accuracy
        /// (the gap to it widens when the model is big for its data, as in <see cref="OverfitScale"/>) and 近亲繁殖.
        /// The accuracy itself only moves by training (<see cref="StepAccuracy"/>) or a change of model.
        /// </summary>
        public void Evaluate(XgRun run)
        {
            var d = XgCatalog.Dataset(run.dataset);
            if (d == null) return;
            double chance = 1 - d.chanceError, top = 1 - d.floorError;
            if (!Finite(run.valAcc)) run.valAcc = chance;
            run.valAcc = Math.Max(chance, Math.Min(top, run.valAcc));
            run.ceiling = CeilingAcc(run);
            double progress = run.ceiling > chance ? Math.Min(1, (run.valAcc - chance) / (run.ceiling - chance)) : 1;
            run.trainAcc = Math.Min(top, run.valAcc + Math.Min(.2, OverfitScale(run) * Math.Log(1 + 4 * progress)));
            ApplyInbreeding(run); // 近亲繁殖: uncaught wrong automatic labels cap what the data can teach (XgSim.Inbreeding.cs)
        }

        /// <summary>The old closed-form curve over steps (tests of the scaling law; training now moves the stored accuracy).</summary>
        public void LegacyEvaluate(XgRun run)
        {
            var d = XgCatalog.Dataset(run.dataset);
            double e0 = d.chanceError, emin = d.floorError;
            double einf = emin + (e0 - emin) * CapacityFraction(run, run.lr);
            double tau = Tau(run);
            double etrain = einf + (e0 - einf) * Math.Pow(1 + run.steps / tau, -2);
            double eval = Math.Min(e0, etrain + OverfitScale(run) * Math.Log(1 + run.steps / (4 * tau)));
            run.trainAcc = 1 - etrain;
            run.valAcc = 1 - eval;
            ApplyProgressionAccuracy(run);
        }

        /// <summary>Best validation accuracy this exact model can ever reach (bot and hints).</summary>
        public double PeakAccuracy(XgRun run, int lr)
        {
            var copy = Shape(run, run.depth, run.width); copy.lr = lr;
            double tau = Tau(copy), best = 0;
            for (double k = 0; k <= 400; k += k < 4 ? .25 : k < 40 ? 2 : 20)
            { copy.steps = k * tau; LegacyEvaluate(copy); best = Math.Max(best, copy.valAcc); }
            return best;
        }

        public double StepsPerSecond(XgRun run, double compute)
        {
            var a = XgCatalog.Arch(run.arch);
            return compute * 150 * OptSpeed * XgCatalog.LrSpeed[run.lr] * a.speed * SpeedResearch * ProgressionSpeed(run) / Math.Sqrt(1 + ParamsK(run));
        }

        /// <summary>Divergence chance per GPU-second at this rate (0 = safe).</summary>
        public double Hazard(XgRun run)
        {
            var a = XgCatalog.Arch(run.arch);
            double instability = a.instability * (Has("gradclip") ? .2 : 1);
            return .02 * Math.Max(0, XgCatalog.LrRisk[run.lr] + instability - Stability);
        }

        public int SafeLr(XgRun run)
        {
            if (UseBoard)
            {
                // The largest step that cannot tear even on a confidently wrong card (|error| = 2).
                var k = Knobs(run);
                for (int i = 0; i < RateValues.Length; i++)
                    if (RateValues[i] * 2 <= k.TearAt + 1e-9) return i;
                return RateValues.Length - 1;
            }
            var a = XgCatalog.Arch(run.arch);
            double instability = a.instability * (Has("gradclip") ? .2 : 1);
            for (int i = 0; i < XgCatalog.LearningRates.Length; i++)
                if (XgCatalog.LrRisk[i] + instability <= Stability + 1e-9) return i;
            return XgCatalog.LearningRates.Length - 1;
        }

        // ───────────── training: one epoch per press ─────────────

        public string Blocker(XgRun run, IXgHost host)
        {
            if (LifeTrainingPaused) return T("临时停机，稍后继续", "Temporarily paused; training resumes shortly");
            if (host == null) return T("设备未就绪");
            if (host.Blocker != null) return host.Blocker;
            if (ProjectActive) return T("研发占用显卡");
            if (SystemUnlocked("vram") && VramNeedMB(run) > TrainVram(run, host))
                return VramNeedMB(run) <= AutoVram(run, host, ParamsK(run)) + 1e-6
                    ? T("显卡正被另一条线占着：等它这一轮练完", "The card is busy with the other line: this round waits its turn")
                    : T("显存不足：模型要 ") + F(VramNeedMB(run), "0") + " MB";
            if (host.Compute <= 0) return T("没有算力");
            string wired = TrainWiringBlocker(run);
            if (wired != null) return wired;
            return null;
        }

        /// <summary>Any available dataset on this track with enough samples unlocks training.</summary>
        public bool TrainingUnlocked(XgTrack track)
        {
            if (!TeachingTrainOpen) return false;
            foreach (var d in XgCatalog.DatasetsFor(track)) if (DatasetAvailable(d.id) && Samples(d.id) >= XgCatalog.SamplesToTrain) return true;
            return false;
        }

        /// <summary>Train on whichever dataset of this track actually has data (e.g. logic labels while poems is empty).</summary>
        void EnsureData(XgRun run)
        {
            if (Samples(run.dataset) >= XgCatalog.SamplesToTrain) return;
            XgDataset best = null;
            foreach (var d in XgCatalog.DatasetsFor((XgTrack)run.track))
                if (DatasetAvailable(d.id) && Samples(d.id) >= XgCatalog.SamplesToTrain && (best == null || Samples(d.id) > Samples(best.id))) best = d;
            if (best != null) SetDataset((XgTrack)run.track, best.id);
        }

        /// <summary>Combo multiplier for hand labels and hand epochs: +5% per hit, up to ×2 (a relic pair raises the cap).</summary>
        public double ComboMultiplier { get { return 1 + Math.Min(1 + RelicFx.comboCapBonus, .05 * S.combo); } }

        /// <summary>
        /// Why a round cannot start. Like <see cref="Blocker"/>, except that the other line's running epoch is no reason:
        /// the round starts and waits for the card (<see cref="TickEpochs"/>).
        /// </summary>
        public string StartBlocker(XgRun run, IXgHost host)
        {
            string blocked = Blocker(run, host);
            if (blocked == null || host == null || host.Blocker != null || ProjectActive || host.Compute <= 0 || TrainWiringBlocker(run) != null) return blocked;
            return VramNeedMB(run) <= AutoVram(run, host, ParamsK(run)) + 1e-6 ? null : blocked;
        }

        /// <summary>
        /// One round. The model configures itself first (XgSim.AutoModel.cs). Hand presses build the combo and feed
        /// ×(combo multiplier) cards. The brain learns from the cards; the accuracy moves towards
        /// the model's ceiling, or goes down a little (<see cref="StepAccuracy"/>). Bills the GPU time, and every
        /// <see cref="EvalEvery"/> rounds runs an assessment.
        /// </summary>
        public XgEpoch TrainEpoch(XgTrack track, IXgHost host, bool hand = true)
        {
            // Deterministic completion primitive; live inputs call BeginEpoch and settle through TickEpochs.
            if (Run(track).epochActive) return null;
            LastEpochWasHand = hand;
            if (!TrainingUnlocked(track))
            {
                if (hand) Say(T("样本不够：先在标注台标 ") + XgCatalog.SamplesToTrain + T(" 条", " samples first"));
                return null;
            }
            var run = Run(track);
            EnsureData(run);
            AutoConfigure(run, host);
            string blocker = Blocker(run, host);
            if (blocker != null) { if (hand) Say(blocker); return null; }
            if (hand) S.clicks++; // training has no combo (2026-10-18): the streak belongs to labelling
            int baseCards = CardsPerEpoch(run, Math.Max(.5, host.Compute * TrainLearningFactor(track)), hand);
            double quizBonus = ConsumeTrainingQuizBonus(run);
            // Extra real training samples are rounded; pace, electricity and data ownership stay unchanged.
            int cards = (int)Math.Round(baseCards * quizBonus);
            // A rate the player set too big may tear the round (XgSim.Rates.cs); 自动 never rolls.
            double tearChance = TearChance(run);
            bool torn = tearChance > 0 && Roll() < tearChance;
            // The brain itself learns from the cards (the 大脑 page, the phenomena); the score follows the scaling law.
#if UNITY_5_3_OR_NEWER
            using (ProfileEpochTrainBoard.Auto())
#endif
            {
            if (UseBoard && !torn) TrainBoard(run, cards);
            }
            run.steps += cards;
            run.epoch++; run.sinceEval++; S.epochs++;
            MemeDriftTrained(run.dataset);
            ReleaseFirstWords(run);
            double billed = BilledSeconds(run);
            ChargeTraining(host, billed, true);
            S.trainedSeconds += billed;
            var e = new XgEpoch { track = (int)track, epoch = run.epoch, hand = hand, steps = cards, diverged = torn };
#if UNITY_5_3_OR_NEWER
            using (ProfileEpochAccuracy.Auto())
#endif
            {
            StepAccuracy(run, baseCards, e, quizBonus);
            }
            RelicRoundDone();
            run.shapeRounds++;
#if UNITY_5_3_OR_NEWER
            using (ProfileEpochEvaluatePhenomena.Auto())
#endif
            {
#if UNITY_5_3_OR_NEWER
            using (ProfileEpochEvaluate.Auto())
#endif
            { Evaluate(run); }
            if (UseBoard)
            {
#if UNITY_5_3_OR_NEWER
                using (ProfileEpochObservePhenomena.Auto())
#endif
                { ObservePhenomena(run); }
#if UNITY_5_3_OR_NEWER
                using (ProfileEpochRecordTrace.Auto())
#endif
                { RecordTrace(run, false, cards); }
#if UNITY_5_3_OR_NEWER
                using (ProfileEpochWatchCure.Auto())
#endif
                { WatchCure(run); }
            }
            }
#if UNITY_5_3_OR_NEWER
            using (ProfileEpochSheet.Auto())
#endif
            {
            SheetAfterRound(run);
            }
            S.stageEpochs++;
            Push(run.histTrain, (float)run.trainAcc);
            Push(run.histVal, (float)run.valAcc);
            PushHoldout(run);
            e.trainAcc = run.trainAcc; e.valAcc = run.valAcc;
            CheckDesks();
            AutoCountTrainedParams(run);
#if UNITY_5_3_OR_NEWER
            using (ProfileEpochAssessment.Auto())
#endif
            {
            if (run.sinceEval >= EvalEvery) e.assessment = Assess(track, host, hand);
            }
#if UNITY_5_3_OR_NEWER
            using (ProfileEpochProgression.Auto())
#endif
            {
            ObserveProgression(run);
            CheckUnprompted();
            }
            if (e.diverged) Diverged?.Invoke((int)track);
#if UNITY_5_3_OR_NEWER
            using (ProfileEpochDoneSubscribers.Auto())
#endif
            {
            EpochDone?.Invoke(e);
            }
            return e;
        }

        static void Push(List<float> list, float v) { list.Add(v); if (list.Count > HistoryLength) list.RemoveAt(0); }

        // ───────────── assessment ─────────────

        /// <summary>0 = chance, 1000 = the dataset's floor error.</summary>
        public static double Score(string datasetId, double acc)
        {
            var d = XgCatalog.Dataset(datasetId);
            if (d == null || acc <= 0) return 0;
            double v = 1000 * (acc - (1 - d.chanceError)) / (d.chanceError - d.floorError);
            return Math.Max(0, Math.Min(1000, Math.Floor(v)));
        }

        public static int Grade(double score)
        {
            int g = 0;
            for (int i = 0; i < XgCatalog.GradeScore.Length; i++) if (score >= XgCatalog.GradeScore[i]) g = i;
            return g;
        }

        public double BestScore(string datasetId) { return Score(datasetId, BestAcc(datasetId)); }

        /// <summary>
        /// Scores the current model. A record saves the checkpoint (contracts and auto-answer use it), pays
        /// (score gain × rewardBase × 0.2) plus a one-time bonus per new grade, and adds 5 to a hand combo.
        /// </summary>
        public XgAssessment Assess(XgTrack track, IXgHost host, bool hand = true)
        {
            if (Run(track).epochActive) return null;
            var run = Run(track);
            if (!TrainingUnlocked(track)) return null;
            Evaluate(run);
            var d = XgCatalog.Dataset(run.dataset);
            var a = new XgAssessment { track = (int)track, dataset = d.id, acc = run.valAcc, hand = hand, inbreeding = run.inbreedingPenalty };
            if (a.inbreeding > 0) SayInbreeding(d, a.inbreeding);
            a.score = Score(d.id, run.valAcc);
            a.previousBest = BestScore(d.id);
            a.grade = Grade(a.score);
            // A record has to beat the last one by more than the test set's own noise (about one standard error).
            // A model that reads the whole sentence at once also counts for realtime jobs (直播实时字幕).
            if (!SerialWiring(Knobs(run)) && run.valAcc > ParallelAcc(d.id))
            {
                if (S.parallelBest == null) S.parallelBest = new List<XgScore>();
                Count(S.parallelBest, d.id).value = run.valAcc;
            }
            a.record = run.valAcc > BestAcc(d.id) + 1e-4
                && (!UseBoard || BestAcc(d.id) <= 0 || BinaryAccuracy(d.id, run.valAcc) >= BinaryAccuracy(d.id, BestAcc(d.id)) + RecordMargin - 1e-9);
            if (a.record)
            {
                a.reward = Math.Max(0, a.score - a.previousBest) * d.rewardBase * RewardPerPoint;
                var b = Best(d.id);
                if (b == null) { b = new XgBest { dataset = d.id }; S.best.Add(b); }
                b.acc = run.valAcc; b.arch = run.arch;
                var entry = SaveModel(track, true);
                b.modelId = entry.id; a.modelId = entry.id;
                S.records++;
                run.staleEvals = 0;
            }
            for (int g = 1; g <= a.grade; g++)
            {
                string key = d.id + "#" + g;
                if (S.grades.Contains(key)) continue;
                S.grades.Add(key);
                a.gradeBonus += XgCatalog.GradeBonus[g] * d.rewardBase;
                a.newGrade = g;
            }
            double pay = a.reward + a.gradeBonus;
            if (pay > 0) { host.Earn(pay); S.totalIncome += pay; S.paidAssessmentReached = true; }
            if (a.record && a.grade >= RelicRecordGrade) TryRelicDrop("record", RelicRecordChance);
            if (!a.record)
            {
                run.staleEvals++;
                if (run.staleEvals == StaleHintEpochs) Say(StaleHint(run));
            }
            run.lastScore = a.score;
            run.sinceEval = 0;
            S.assessments++;
            // 参数量与数据量主线: a model graded C or better counts towards the trained parameters.
            NoteTrainedParams(run, a);
            CheckAbilities();
            RecordCheckup(a);
            Assessed?.Invoke(a);
            return a;
        }

        // ───────────── 模型仓库 ─────────────

        /// <summary>Store the current model of a track in the repository (null if it has not trained yet).</summary>
        public XgModelEntry SaveModel(XgTrack track, bool record = false)
        {
            if (Run(track).epochActive) return null;
            var run = Run(track);
            if (run.epoch <= 0 && !record) { Say(T("还没训练过，没什么可存的")); return null; }
            Evaluate(run);
            var e = new XgModelEntry
            {
                id = S.nextModelId++, track = run.track, dataset = run.dataset, arch = run.arch, depth = run.depth, width = run.width, lr = run.lr,
                epoch = run.epoch, steps = run.steps, acc = run.valAcc, trainAcc = run.trainAcc, score = Score(run.dataset, run.valAcc),
                gameSeconds = Clock, date = Today, record = record,
            };
            e.name = FileName(e);
            // Keep the curve small: at most 32 points.
            int n = run.histVal.Count, step = Math.Max(1, (int)Math.Ceiling(n / 32.0));
            for (int i = 0; i < n; i += step) e.curve.Add(run.histVal[i]);
            if (n > 0 && (n - 1) % step != 0) e.curve.Add(run.histVal[n - 1]);
            if (UseBoard) e.weights = Board.SnapshotRegion(RegionOf(run.dataset));
            S.models.Add(e);
            CleanModels(e.id);
            TrimSnapshots();
            ModelSaved?.Invoke(e);
            return e;
        }

        /// <summary>A file-like name: lenet_mnist_d3w32_e48_912.ckpt.</summary>
        public static string FileName(XgModelEntry e)
        {
            return e.arch + "_" + e.dataset + "_d" + e.depth + "w" + XgCatalog.Widths[Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, e.width))] + "_e" + e.epoch + "_" + e.score.ToString("0", CultureInfo.InvariantCulture) + ".ckpt";
        }

        /// <summary>Float32 weights plus optimizer state, in MB (flavour for the repository list).</summary>
        public static double SizeMB(XgModelEntry e)
        {
            return ParamsK(new XgRun { arch = e.arch, depth = e.depth, width = e.width }) * 1000 * 4 * 3 / (1024.0 * 1024.0);
        }

        public XgModelEntry Model(int id) { foreach (var m in S.models) if (m.id == id) return m; return null; }

        /// <summary>Checkpoints that keep their weights besides the starred ones (the newest first).</summary>
        public const int SnapshotsKept = 6;

        /// <summary>Older unstarred checkpoints drop their weights and keep only their settings (a small save).</summary>
        void TrimSnapshots()
        {
            int kept = 0;
            for (int i = S.models.Count - 1; i >= 0; i--)
            {
                var m = S.models[i];
                if (!m.HasWeights || m.starred || IsBrainInstalled(m.id)) continue;
                if (++kept > SnapshotsKept) m.weights = new List<XgConcept>();
            }
        }

        /// <summary>The repository entry behind a dataset's deployed checkpoint.</summary>
        public bool IsDeployed(XgModelEntry e) { var b = Best(e.dataset); return b != null && b.modelId == e.id; }

        void CleanModels(int justSaved)
        {
            int extra = S.models.Count - MaxModels;
            for (int i = 0; i < S.models.Count && extra > 0;)
            {
                var m = S.models[i];
                if (m.id != justSaved && !m.starred && !IsDeployed(m) && !IsBrainInstalled(m.id)) { S.models.RemoveAt(i); extra--; } else i++;
            }
        }

        public void StarModel(int id, bool on) { var m = Model(id); if (m != null) m.starred = on; }

        public bool DeleteModel(int id)
        {
            var m = Model(id);
            if (m == null) return false;
            if (IsDeployed(m)) { Say(T("这个模型正在部署，删不了")); return false; }
            if (IsBrainInstalled(id)) { Say(T("这个模型已装入大脑，先拆下再删除。", "This model is installed in the brain; remove it before deleting.")); return false; }
            S.models.Remove(m);
            return true;
        }

        /// <summary>Why a saved model cannot be loaded into training right now, or null.</summary>
        public string CannotLoad(XgModelEntry m)
        {
            if (m == null) return T("模型不存在");
            if (Run((XgTrack)m.track).epochActive) return T("先完成当前训练轮次");
            if (!DatasetAvailable(m.dataset)) return T("数据集不可用");
            // The training line configures itself: only a checkpoint of the structure it runs now carries on.
            var run = Run((XgTrack)m.track);
            if (m.arch != run.arch)
            {
                var now = XgCatalog.Arch(run.arch);
                return T("结构不同：训练线现在自动用 " + (now != null ? now.name : run.arch), "Another structure: the training line now runs " + (now != null ? now.nameEn : run.arch));
            }
            return null;
        }

        /// <summary>
        /// Continue training from a saved model of the current structure: its dataset, its weights (when the checkpoint
        /// kept them) and its accuracy come back; a checkpoint of a smaller size grows into the current model and keeps
        /// most of its progress. The curve restarts.
        /// </summary>
        public bool LoadModel(int id)
        {
            var m = Model(id);
            if (m == null) return false;
            string why = CannotLoad(m);
            if (why != null) { Say(why); return false; }
            var run = Run((XgTrack)m.track);
            bool reshaped = run.depth != m.depth || run.width != m.width;
            Remember(run);
            run.dataset = m.dataset;
            var d = XgCatalog.Dataset(m.dataset);
            double chance = d == null ? 0 : 1 - d.chanceError;
            run.valAcc = reshaped ? chance + Math.Max(0, m.acc - chance) * (Has("transfer") ? GrowKeepTransfer : GrowKeep) : m.acc;
            Remember(run);
            run.epoch = m.epoch; run.sinceEval = 0; run.staleEvals = 0; run.lastGain = 0;
            if (reshaped) run.shapeRounds = 0;
            run.histTrain.Clear(); run.histVal.Clear();
            if (UseBoard)
            {
                if (m.HasWeights) Board.RestoreRegion(RegionOf(m.dataset), m.weights);
                else if (run.depth != m.depth) ReinitialiseBoard(run);
            }
            Evaluate(run);
            Push(run.histTrain, (float)run.trainAcc); Push(run.histVal, (float)run.valAcc); PushHoldout(run);
            SelectedTrack = (XgTrack)m.track;
            Say(T("已加载 ") + m.name + (reshaped ? T("：长成现在的大小，保留大部分进度。", ": grown to the current size, keeping most of its progress.") : ""));
            return true;
        }

        /// <summary>Several assessments without a record: say what is holding the model back.</summary>
        public string StaleHint(XgRun run)
        {
            string plateau = PlateauText(run, out _);
            if (plateau.Length > 0) return plateau;
            var r = DropRisk(run);
            if (r.level >= 1) return DropRiskText(run) + T("。", ".");
            return T("还在涨，只是慢：多练几轮，或者攒连击。", "Still improving, only slowly: train more rounds, or build the combo.");
        }

        // ───────────── checkpoints, contracts ─────────────

        public XgBest Best(string datasetId)
        {
            foreach (var b in S.best) if (b.dataset == datasetId) return b;
            return null;
        }
        public double BestAcc(string datasetId) { var b = Best(datasetId); return b == null ? 0 : b.acc; }

        public bool Signed(string contractId) { return S.contracts.Contains(contractId); }

        /// <summary>The stage that opens business orders for signing.</summary>
        public const int ContractStage = 4;

        public static int ContractRequiredStage(XgContract c) => c != null &&
            (c.id == "cheque" || c.id == "acrostic" || c.id == "danmaku") ? 3 : ContractStage;

        public bool CanSign(XgContract c) { return c != null && S.stage >= ContractRequiredStage(c) && !Signed(c.id) && ContractAcc(c) + 1e-9 >= c.threshold; }

        /// <summary>The score a contract judges: the best checkpoint, or for a realtime job the best parallel one.</summary>
        public double ContractAcc(XgContract c) => c.realtime ? ParallelAcc(c.dataset) : BestAcc(c.dataset);

        /// <summary>Best validation accuracy a model that reads the whole sentence at once (no loop) reached on this dataset.</summary>
        public double ParallelAcc(string dataset) { if (S.parallelBest != null) foreach (var x in S.parallelBest) if (x.key == dataset) return x.value; return 0; }

        public bool Sign(string contractId, IXgHost host)
        {
            var c = XgCatalog.Contract(contractId);
            if (c == null || !CanSign(c)) return false;
            S.contracts.Add(c.id);
            host.Earn(c.signBonus);
            S.totalIncome += c.signBonus;
            Say(T("签约 ") + T(c.client, c.clientEn) + T("：", ": ") + T(c.job, c.jobEn) + T("，首付 ¥") + F(c.signBonus, "0"));
            return true;
        }

        public double ContractIncome(XgContract c)
        {
            if (!Signed(c.id)) return 0;
            double acc = ContractAcc(c);
            if (acc < c.threshold) return 0;
            return c.income * (1 + (acc - c.threshold) / Math.Max(.01, 1 - c.threshold)) * DriftPay(c) * WireSpeed("contract:" + c.id);
        }

        /// <summary>Points of 新题型 drift at which a contract's pay halves (the floor).</summary>
        public const double DriftHalfPay = 15;

        /// <summary>
        /// 新题型: the deployed checkpoint misses this month's new meme, so the client's results got worse and the pay
        /// drops with the drift (down to half at <see cref="DriftHalfPay"/> points). Retraining the desk wins it back.
        /// </summary>
        public double DriftPay(XgContract c) => Math.Max(.5, 1 - MemeDrift(c.dataset) / (2 * DriftHalfPay));

        public double IncomePerSecond
        {
            // Signed contracts plus the crowd-order tier the abilities reached (XgSim.StoryPacing.cs).
            get { double sum = CrowdOrderIncome; foreach (var c in XgCatalog.Contracts) sum += ContractIncome(c); return sum * RelicIncomeFactor; }
        }

        public bool ContractsUnlocked { get { return S.best.Count > 0 || S.contracts.Count > 0; } }

        // ───────────── combo ─────────────

        /// <summary>A round pressed by hand is on the bar.</summary>
        public bool HandRoundRunning => S.vision.epochActive && S.vision.epochHand || S.sequence.epochActive && S.sequence.epochHand;

        void Hit(double window, int amount)
        {
            int before = S.combo;
            S.combo += amount;
            if (S.openingFocus) window *= OpeningComboWindow;
            S.comboTimer = Math.Max(S.comboTimer, window);
            if (S.combo > S.bestCombo) S.bestCombo = S.combo;
            for (int i = 0; i < XgCatalog.ComboTiers.Length; i++)
                if (before < XgCatalog.ComboTiers[i] && S.combo >= XgCatalog.ComboTiers[i]) ComboTier?.Invoke(i);
        }

        void BreakCombo()
        {
            if (S.combo <= 0 || RelicFx.comboKeep) return;
            int was = S.combo;
            S.combo = 0; S.comboTimer = 0;
            if (was >= 3) ComboBroken?.Invoke(was);
        }

        public static int TierOf(int combo)
        {
            int tier = -1;
            for (int i = 0; i < XgCatalog.ComboTiers.Length; i++) if (combo >= XgCatalog.ComboTiers[i]) tier = i;
            return tier;
        }

        // ───────────── labelling desk ─────────────

        public static XgDataset HandDataset(XgTrack track)
        {
            foreach (var d in XgCatalog.Datasets) if (d.track == track && d.handLabel) return d;
            return null;
        }

        public double Labels(string datasetId)
        {
            foreach (var l in S.labels) if (l.dataset == datasetId) return l.count;
            return 0;
        }

        public double TotalLabels { get { double n = 0; foreach (var l in S.labels) n += l.count; return n; } }

        void AddLabel(string datasetId)
        {
            int oldLevel = LevelOf(datasetId);
            double before = Samples(datasetId);
            SetCount(S.labels, datasetId, Labels(datasetId) + 1);
            var d = XgCatalog.Dataset(datasetId);
            if (TeachingTrainOpen && before < XgCatalog.SamplesToTrain && Samples(datasetId) >= XgCatalog.SamplesToTrain)
                Say(T("攒够 " + XgCatalog.SamplesToTrain + " 条样本：「" + d.name + "」可以训练了，去「训练」页按「训练一轮」", d.nameEn + ": " + XgCatalog.SamplesToTrain + " samples, training unlocked"));
            RelicLabelBonus(datasetId);
            foreach (var run in Runs) if (run.dataset == datasetId) Evaluate(run);
            int newLevel = LevelOf(datasetId);
            if (newLevel > oldLevel) DeskLevelUp?.Invoke(datasetId, newLevel);
        }

        static double Count(List<XgLabelCount> list, string id) { foreach (var l in list) if (l.dataset == id) return l.count; return 0; }

        static void SetCount(List<XgLabelCount> list, string id, double value)
        {
            foreach (var l in list) if (l.dataset == id) { l.count = value; return; }
            list.Add(new XgLabelCount { dataset = id, count = value });
        }

        /// <summary>Bought pack plus every correct label (hand or auto).</summary>
        public double Samples(string datasetId)
        {
            var d = XgCatalog.Dataset(datasetId);
            // Packs (public, junk, story), crowd rows and model-labelled logs (XgSim.DataSources.cs, XgSim.Flywheel.cs).
            return (d != null ? PackSamples(datasetId) + ExtraSamples(datasetId) : 0) + Labels(datasetId);
        }

        public bool DeskOpen(string id) { return XgCatalog.Desk(id) != null && (TeachingPackDeskOpen(id) || S.desksOpen.Contains(id) && !OpeningLocksDesk(id)); }

        public List<XgDesk> OpenDesks()
        {
            var list = new List<XgDesk>();
            foreach (var d in XgCatalog.Desks) if (DeskOpen(d.id)) list.Add(d);
            return list;
        }

        public bool DeskCondition(XgDesk desk)
        {
            return desk != null && ProgressionDeskAvailable(desk.id);
        }

        /// <summary>
        /// How a locked desk opens, under the current rules: arithmetic and danmaku open by themselves with the second
        /// ability; the rest with their data pack in the tech tree, some of which also need an item first.
        /// </summary>
        public string DeskConditionText(XgDesk desk)
        {
            // The opening keeps only desks shut that have their own reason too (the starting desks are open), so that reason is shown.
            if (desk == null || ProgressionDeskAvailable(desk.id) && !OpeningLocksDesk(desk.id)) return "";
            if ((desk.id == "arith" || desk.id == "danmu") && S.stage < 2)
                return T("学会「" + AbilityName(2, false) + "」后自动开放。", "Opens by itself once it learns '" + AbilityName(2, true) + "'.");
            var pack = XgCatalog.Node(desk.id + ".pack");
            if (pack == null) return "";
            if (Has(pack.id) && S.owned.Contains(desk.id) && InstalledBatches(desk.id) == 0)
                return T("下载好数据包后，从资料文件夹拖进练页安装。", "After downloading, drag the pack from your Files folder into Train to install.");
            string zh = "在科技买「" + pack.name + "」数据包（¥" + F(NodeCost(pack), "0") + "）";
            string en = "buy the " + pack.nameEn + " data pack in the tech tree (¥" + F(NodeCost(pack), "0") + ")";
            var items = new List<XgNode>();
            if (pack.parent != null && !Has(pack.parent)) items.Add(XgCatalog.Node(pack.parent));
            foreach (var need in pack.needs) if (!Has(need)) items.Add(XgCatalog.Node(need));
            foreach (var item in items)
            {
                if (item == null) continue;
                zh += "，还要先有「" + item.name + "」";
                en += ", which needs " + item.nameEn + " first";
            }
            if (pack.stage > S.stage)
            {
                string ability = AbilityName(pack.stage, false), abilityEn = AbilityName(pack.stage, true);
                return T("学会「" + ability + "」后，" + zh + "。", "Once it learns '" + abilityEn + "': " + en + ".");
            }
            return T(zh + "。", char.ToUpperInvariant(en[0]) + en.Substring(1) + ".");
        }

        void CheckDesks()
        {
            // The first-minutes focus ends here, before the desks it was holding back open.
            bool opened = EndOpeningIfDone(out bool learned);
            foreach (var d in XgCatalog.Desks)
            {
                if (DeskOpen(d.id) || !DeskCondition(d)) continue;
                if (!S.desksOpen.Contains(d.id)) S.desksOpen.Add(d.id);
                if (opened) continue;
                Say(T("新标注桌开放：") + T(d.name, d.nameEn));
                DeskOpened?.Invoke(d);
            }
            if (learned) AnnounceYesNoLearned();
        }

        public void SelectDesk(string id)
        {
            if (!DeskOpen(id)) return;
            S.desk = id;
            Card(id).age = 0;
        }

        public XgCard Card(string desk)
        {
            foreach (var c in S.cards) if (c.dataset == desk && Valid(c)) return c;
            return NewCard(desk);
        }

        static bool Valid(XgCard c)
        {
            var desk = XgCatalog.Desk(c.dataset);
            if (desk == null) return false;
            switch (desk.kind)
            {
                case XgDeskKind.Poem: return !string.IsNullOrEmpty(c.line);
                case XgDeskKind.Logic: case XgDeskKind.Arith: case XgDeskKind.Text: return !string.IsNullOrEmpty(c.question);
                case XgDeskKind.Go: return c.line != null && c.line.Length == XgVisual.Board * XgVisual.Board;
                default: return true;
            }
        }

        /// <summary>Difficulty of a desk, from its own label count.</summary>
        public int LevelOf(string desk)
        {
            var d = XgCatalog.Desk(desk);
            if (d == null) return 1;
            return Math.Min(MaxLevelOf(desk), 1 + (int)(Labels(desk) / LabelsPerLevel(desk)));
        }

        public static int MaxLevelOf(string desk)
        {
            var d = XgCatalog.Desk(desk);
            if (d == null) return 1;
            switch (d.kind)
            {
                case XgDeskKind.Logic: return XgLogic.MaxLevel;
                case XgDeskKind.Arith: return XgArith.MaxLevel;
                case XgDeskKind.Text: return XgMemes.MaxLevel;
                case XgDeskKind.Captcha: return XgVisual.CaptchaMaxLevel;
                case XgDeskKind.Meme: return XgVisual.MemeMaxLevel;
                case XgDeskKind.Go: return XgVisual.GoMaxLevel;
                default: return 1;
            }
        }

        public static int LabelsPerLevel(string desk)
        {
            var d = XgCatalog.Desk(desk);
            return d != null && (d.kind == XgDeskKind.Text || d.kind == XgDeskKind.Meme) ? 30 : 40;
        }

        /// <summary>Logic difficulty 1–5 (kept for the YY helpers).</summary>
        public int LogicLevel { get { return LevelOf("logic"); } }

        /// <summary>Harder cards pay more (+50% per level).</summary>
        public double PayFor(string desk, int level) { return XgCatalog.LabelPay * (XgCatalog.Desk(desk)?.pay ?? 1) * (1 + .5 * (Math.Max(1, level) - 1)); }

        /// <summary>Manual labelling only (hand answers and chat questions); automated answers keep their base pay.</summary>
        public double ManualPayFor(string desk, int level) { return PayFor(desk, level) * RaiseMultiplier * XgCatalog.HandPayScale; }

        /// <summary>What a wrong pick costs on this desk (0 when the desk does not fine): its fine share of a right answer's base pay.</summary>
        public double HandFineFor(string desk, int level)
        {
            var info = XgCatalog.Desk(desk);
            if (info == null) return 0;
            return (info.fine > 0 ? info.fine : XgCatalog.HandFine) * ManualPayFor(desk, level);
        }

        // ───────────── 加薪 ─────────────

        public int RaiseLevel { get { return S.payRaise; } }
        public double RaiseMultiplier { get { return XgCatalog.RaiseMultipliers[Math.Max(0, Math.Min(XgCatalog.RaiseMax, S.payRaise))]; } }
        public double NextRaiseCost { get { return S.payRaise >= XgCatalog.RaiseMax ? double.PositiveInfinity : XgCatalog.RaiseCost(S.payRaise); } }

        /// <summary>Total price of the next <paramref name="count"/> raises (fewer if the ladder ends).</summary>
        public double RaiseCostFor(int count, out int levels)
        {
            double sum = 0; levels = 0;
            for (int l = S.payRaise; l < XgCatalog.RaiseMax && levels < count; l++, levels++) sum += XgCatalog.RaiseCost(l);
            return sum;
        }

        /// <summary>How many raises the wallet covers right now (for a "Max" button).</summary>
        public int AffordableRaises(IXgHost host)
        {
            double money = host.Money; int n = 0;
            for (int l = S.payRaise; l < XgCatalog.RaiseMax && money + 1e-9 >= XgCatalog.RaiseCost(l); l++) { money -= XgCatalog.RaiseCost(l); n++; }
            return n;
        }

        /// <summary>Buy up to <paramref name="count"/> raises in a row; returns how many were bought.</summary>
        public int BuyRaise(IXgHost host, int count = 1)
        {
            int bought = 0;
            while (bought < count && S.payRaise < XgCatalog.RaiseMax)
            {
                double cost = XgCatalog.RaiseCost(S.payRaise);
                if (!host.Spend(cost)) break;
                S.totalSpent += cost;
                S.payRaise++;
                bought++;
            }
            if (bought == 0) Say(S.payRaise >= XgCatalog.RaiseMax ? T("已经是最高薪了") : T("经费不足 ¥") + F(NextRaiseCost, "0"));
            else Say(T("加薪！") + XgCatalog.RaiseTitle(S.payRaise, English) + T("，人工标注 ×") + F(RaiseMultiplier, "0"));
            return bought;
        }

        public string Topic { get { return XgMemes.TopicOf(Today); } }

        int duelLeft;
        bool duelOk;
        public bool InDuel { get { return duelLeft > 0; } }
        public int DuelLeft { get { return duelLeft; } }

        XgCard NewCard(string desk)
        {
            var card = CreateLabelCard(desk, true);
            S.cards.RemoveAll(c => c.dataset == desk);
            S.cards.Add(card);
            return card;
        }

        public XgCard CreateLabelCard(string desk, bool allowFamily = false, bool companyWork = false)
        {
            var info = XgCatalog.Desk(desk);
            var d = XgCatalog.Dataset(desk);
            // These are internal teaching examples, not crowd tasks. All paid submission paths require a desk.
            if (info == null && (desk == "translate" || desk == "crosssentence"))
            {
                var example = new XgCard { track = (int)d.track, dataset = desk, seed = (int)(Roll() * int.MaxValue), level = 1, roll = Roll() };
                DecorateProgressionCard(example);
                EnsureCardId(example);
                return example;
            }
            var card = new XgCard { track = (int)d.track, dataset = desk, truth = Roll() < .5, seed = (int)(Roll() * int.MaxValue) };
            var r = new Random(card.seed);
            int level = LevelOf(desk);
            card.level = level;
            switch (info.kind)
            {
                case XgDeskKind.Digit:
                    card.digit = (int)(Roll() * 10) % 10;
                    card.asked = card.truth ? card.digit : (card.digit + 1 + (int)(Roll() * 9) % 9) % 10;
                    break;
                case XgDeskKind.Poem:
                    var lines = XgCatalog.PoemLines;
                    card.line = lines[(int)(Roll() * lines.Length) % lines.Length];
                    card.shown = 1 + (int)(Roll() * (card.line.Length - 1)) % (card.line.Length - 1);
                    string next = card.line.Substring(card.shown, 1);
                    card.askedChar = next;
                    if (!card.truth)
                        for (int tries = 0; tries < 20 && card.askedChar == next; tries++)
                        {
                            string other = lines[(int)(Roll() * lines.Length) % lines.Length];
                            card.askedChar = other.Substring((int)(Roll() * other.Length) % other.Length, 1);
                        }
                    if (card.askedChar == next) card.truth = true;
                    break;
                case XgDeskKind.Logic:
                    var q = XgLogic.Generate(card.seed, level, Today);
                    card.truth = q.truth; card.level = q.level;
                    card.question = q.text; card.questionEn = q.textEn; card.why = q.why; card.whyEn = q.whyEn;
                    card.category = q.category; card.categoryEn = q.categoryEn;
                    break;
                case XgDeskKind.Arith:
                    var sum = XgArith.Generate(card.seed, level);
                    card.truth = sum.truth; card.level = sum.level;
                    card.question = sum.text; card.questionEn = sum.textEn; card.why = sum.why; card.whyEn = sum.whyEn;
                    card.category = sum.category; card.categoryEn = sum.categoryEn;
                    break;
                case XgDeskKind.Text:
                    var p = XgMemes.Pick(desk, r, level, Today, Topic, allowFamily && TotalLabels >= XgCatalog.SamplesToTrain);
                    if (p == null) break; // Progression-only text desks are filled by DecorateProgressionCard.
                    card.truth = p.yes; card.trick = p.trick;
                    card.question = p.text; card.questionEn = p.textEn.Length > 0 ? p.textEn : p.text;
                    card.why = p.why; card.whyEn = p.whyEn.Length > 0 ? p.whyEn : p.why; card.source = p.source; card.from = p.from; card.family = p.family;
                    card.category = p.trick ? "老司机题" : info.name; card.categoryEn = p.trick ? "Trick card" : info.nameEn;
                    break;
                case XgDeskKind.Captcha: XgVisual.Captcha(card, r, level); break;
                case XgDeskKind.Meme: XgVisual.Meme(card, r, level, Today); break;
                case XgDeskKind.Go: XgVisual.Go(card, r, level); break;
            }
            bool slow = info.kind == XgDeskKind.Logic || info.kind == XgDeskKind.Go || info.kind == XgDeskKind.Text;
            if (duelLeft > 0 && desk == "meme") card.timeLimit = 2.5;
            else if (Roll() < GoldChance) { card.gold = true; card.timeLimit = slow ? 8 : info.kind == XgDeskKind.Arith ? 5 : 3; }
            if (info.kind == XgDeskKind.Logic) MaybeBounty(card, info);
            if (card.family) { card.gold = false; card.timeLimit = 0; }
            card.roll = Roll();
            DecorateProgressionCard(card);
            if (!companyWork) MaybeShutdownCard(card);
            EnsureCardId(card);
            return card;
        }

        /// <summary>The deployed checkpoint's guess for the current card; false (the UI's "no model yet") until the desk has a trained checkpoint.</summary>
        public bool Suggestion(string desk, out bool yes, out double confidence)
        {
            if (S.stage == 1)
            {
                var shown = ReviewCard(desk) ?? Card(desk);
                if (shown.kind == "combo") return TryGetComboSuggestion(shown, out yes, out confidence);
            }
            if (TryCollaborativeSuggestion(desk, out yes, out confidence)) { ObserveUncertainty(confidence); return true; }
            yes = false; confidence = 0;
            // No trained checkpoint on this desk: no model, and no guess (the board's chance level would read as a 50% coin flip).
            if (!(BestAcc(desk) > 0)) return false;
            var card = Card(desk);
            double acc = CardAccuracy(Run(XgCatalog.Dataset(desk).track), card, BestAcc(desk));
            if (acc <= 0) return false;
            bool right = card.roll < acc;
            yes = right ? card.truth : !card.truth;
            confidence = acc;
            ObserveUncertainty(confidence);
            return true;
        }

        /// <summary>
        /// Hand answer. Right: ¥ (× combo, ×3 on 前方高能, harder cards pay more), one more sample, combo +1 (+2 on a
        /// 老司机 trick). Wrong or too late: no pay, combo reset, label discarded. Either way the next card comes up.
        /// </summary>
        public XgAnswer Answer(string desk, bool yes, IXgHost host)
        {
            var info = XgCatalog.Desk(desk);
            if (info == null || host == null) return default;
            var card = Card(desk);
            var traceReceiver = CardAnswered;
            long traceCardId = card.id;
            string traceDataset = card.dataset;
            var traceSource = AnswerTraceSource(card);
            List<XgConceptPull> correctionPulls = null;
            if (card.family)
            {
                var familyResult = AnswerFamily(card, yes, host);
                PublishAnswerTrace(traceReceiver, traceCardId, traceDataset, yes, traceSource, null, familyResult);
                return familyResult;
            }
            bool hadGhost = GhostBeforeAnswer(desk, card, out bool ghost);
            bool teaching = TeachingGuess(card, out bool taughtGuess, out XgPredictionTrace predictionTrace, traceReceiver != null);
            bool correcting = teaching && taughtGuess != yes;
            ObserveProgressionAnswer(card);
            NoteLabelSpeed();
            // Timers affect only the gold quick-answer bonus, never truth or the player's streak.
            bool quickBonus = card.gold && (card.timeLimit <= 0 || card.age <= card.timeLimit);
            bool timeout = false;
            // The SI's planted question has no right answer: whatever you choose is what the "？" cell learns.
            bool shutdown = card.kind == "shutdown";
            if (shutdown) { card.truth = yes; S.shutdownCards++; S.shutdownLean += yes ? 1 : -1; }
            NoteLabelLean(yes);
            var result = new XgAnswer { truth = card.truth, correct = yes == card.truth, gold = quickBonus, trick = card.trick, timeout = false, bounty = card.bounty };
            if (result.correct)
            {
                if (teaching)
                {
                    S.modelStreak = taughtGuess == card.truth ? S.modelStreak + 1 : 0;
                    S.bestModelStreak = Math.Max(S.bestModelStreak, S.modelStreak);
                }
                Hit(info.comboWindow, card.trick ? 2 : 1);
                result.corrected = correcting;
                result.pay = ManualPayFor(desk, card.level) * (teaching ? (correcting ? CorrectionPayMultiplier : 1) : ComboMultiplier) * (quickBonus ? 3 : 1) * (card.bounty ? BountyMultiplier : 1);
                host.Earn(result.pay);
                S.totalIncome += result.pay;
                S.handCorrect++;
                QualityHandCorrect();
                MemeDriftLabelled(desk);
                if (card.law)
                {
                    // Paid outside work: a law firm's question is no training data for this lab's tasks.
                    result.samples = 0;
                }
                else
                {
                    AddLabel(desk);
                    result.samples = 1 + (int)HandLabelLog(desk) + RelicLabelExtra();
                    if (traceReceiver != null && correcting) correctionPulls = new List<XgConceptPull>();
                    TeachBoard(card, card.truth, correctionPulls);
                    RememberSheetCard(card);
                    RememberTaught(card);
                    if (correcting) RememberCorrection(card);
                }
            }
            else
            {
                BreakCombo(); S.modelStreak = 0; S.handWrong++;
                if (!timeout && !shutdown) result.fine = ChargeHandFine(desk, card.level, host);
            }
            result.combo = S.combo;
            NoteDayAnswer(result.correct, result.pay - result.fine, result.samples);
            DuelStep(desk, result.correct, host);
            if (desk == "meme" && duelLeft == 0 && result.correct && LevelOf("meme") >= 3 && Roll() < .08)
            {
                duelLeft = DuelLength; duelOk = true;
                Say(T("阿杰向你发起斗图！连对 " + DuelLength + " 张有奖", "阿杰 starts a meme battle! " + DuelLength + " in a row pays a bonus"));
            }
            NewCard(desk);
            CheckDesks();
            ObserveGhost(hadGhost, ghost, yes, timeout);
            PublishAnswerTrace(traceReceiver, traceCardId, traceDataset, yes, traceSource, predictionTrace, result, correctionPulls);
            return result;
        }

        void DuelStep(string desk, bool correct, IXgHost host)
        {
            if (duelLeft <= 0 || desk != "meme") return;
            duelLeft--;
            if (!correct) { duelOk = false; duelLeft = 0; }
            if (duelLeft > 0) return;
            double bonus = duelOk ? 3 * XgCatalog.Dataset("meme").rewardBase : 0;
            if (bonus > 0 && host != null) { host.Earn(bonus); S.totalIncome += bonus; }
            Say(duelOk ? T("斗图赢了！+¥") + F(bonus, "0") : T("斗图输了。阿杰：「就这？」"));
            DuelDone?.Invoke(duelOk, bonus);
        }

        /// <summary>
        /// A card answered somewhere else (老周 or the group asking in YY). Right: one sample and the desk's pay, no combo.
        /// Returns the pay.
        /// </summary>
        public double ChatLabel(string desk, bool correct, IXgHost host)
        {
            if (XgCatalog.Desk(desk) == null) return 0;
            if (!correct) { S.handWrong++; return 0; }
            double pay = ManualPayFor(desk, LevelOf(desk));
            host.Earn(pay); S.totalIncome += pay;
            S.handCorrect++;
            AddLabel(desk);
            MemeDriftLabelled(desk);
            return pay;
        }

        // ───────────── auto-answer ─────────────

        public int AutoLevel(string desk) { return GlobalAutoLevel; }
        public int AutoLevelTotal { get { return GlobalAutoLevel; } }

        public bool CanBuyAuto(string desk, out string why) { return CanBuyGlobalAuto(out why); }
        public bool BuyAuto(string desk, IXgHost host) { return BuyGlobalAuto(host); }
        public double AutoCardsPerSecond(string desk, IXgHost host) { return CollaborationRate(desk, host); }
        public double AutoIncome(string desk, IXgHost host) { return CollaborationIncome(desk, host); }
        void AutoAnswer(double dt, IXgHost host) { TickCollaboration(dt, host); }

#if UNITY_5_3_OR_NEWER
        static readonly XgSimulationMarker ProfileStepPayments = new XgSimulationMarker("Xg.Sim.StepPayments");
        static readonly XgSimulationMarker ProfileStepWiring = new XgSimulationMarker("Xg.Sim.StepWiring");
        static readonly XgSimulationMarker ProfileStepAutoConfigure = new XgSimulationMarker("Xg.Sim.StepAutoConfigure");
        static readonly XgSimulationMarker ProfileStepProject = new XgSimulationMarker("Xg.Sim.StepProject");
        static readonly XgSimulationMarker ProfileStepEpochs = new XgSimulationMarker("Xg.Sim.StepEpochs");
        static readonly XgSimulationMarker ProfileStepAutoAnswer = new XgSimulationMarker("Xg.Sim.StepAutoAnswer");
        static readonly XgSimulationMarker ProfileStepMarket = new XgSimulationMarker("Xg.Sim.StepMarket");
        static readonly XgSimulationMarker ProfileStepLife = new XgSimulationMarker("Xg.Sim.StepLife");
        static readonly XgSimulationMarker ProfileStepBooksStages = new XgSimulationMarker("Xg.Sim.StepBooksStages");
        static readonly XgSimulationMarker ProfileStepDataRelics = new XgSimulationMarker("Xg.Sim.StepDataRelics");
        static readonly XgSimulationMarker ProfileStepCard = new XgSimulationMarker("Xg.Sim.StepCard");
        static readonly XgSimulationMarker ProfileStepUnlocksChronicle = new XgSimulationMarker("Xg.Sim.StepUnlocksChronicle");
        static readonly XgSimulationMarker ProfileEpochTrainBoard = new XgSimulationMarker("Xg.Sim.EpochTrainBoard");
        static readonly XgSimulationMarker ProfileEpochAccuracy = new XgSimulationMarker("Xg.Sim.EpochAccuracy");
        static readonly XgSimulationMarker ProfileEpochEvaluatePhenomena = new XgSimulationMarker("Xg.Sim.EpochEvaluatePhenomena");
        static readonly XgSimulationMarker ProfileEpochEvaluate = new XgSimulationMarker("Xg.Sim.EpochEvaluate");
        static readonly XgSimulationMarker ProfileEpochObservePhenomena = new XgSimulationMarker("Xg.Sim.EpochObservePhenomena");
        static readonly XgSimulationMarker ProfileEpochRecordTrace = new XgSimulationMarker("Xg.Sim.EpochRecordTrace");
        static readonly XgSimulationMarker ProfileEpochWatchCure = new XgSimulationMarker("Xg.Sim.EpochWatchCure");
        static readonly XgSimulationMarker ProfileEpochSheet = new XgSimulationMarker("Xg.Sim.EpochSheet");
        static readonly XgSimulationMarker ProfileEpochAssessment = new XgSimulationMarker("Xg.Sim.EpochAssessment");
        static readonly XgSimulationMarker ProfileEpochProgression = new XgSimulationMarker("Xg.Sim.EpochProgression");
        static readonly XgSimulationMarker ProfileEpochDoneSubscribers = new XgSimulationMarker("Xg.Sim.EpochDoneSubscribers");
#endif

        // ───────────── time ─────────────

        public void Tick(double seconds, IXgHost host)
        {
            if (seconds <= 0 || double.IsNaN(seconds)) return;
            double left = Math.Min(seconds, 86400);
            while (left > 0)
            {
                double dt = Math.Min(left, 1);
                Step(dt, host);
                left -= dt;
            }
        }

        void Step(double dt, IXgHost host)
        {
#if UNITY_5_3_OR_NEWER
            using (ProfileStepPayments.Auto())
#endif
            {
            PayAbilityResearch(host);
            PayAutoRefund(host);
            PayConsumableRefund(host);
            }
            // 接线 first: every job below reads its speed from this tick's wiring (XgSim.Wiring.cs).
#if UNITY_5_3_OR_NEWER
            using (ProfileStepWiring.Auto())
#endif
            {
            TickWiring(dt, host);
            }
            // Training without knobs: between rounds the models follow what is owned and what fits (XgSim.AutoModel.cs).
#if UNITY_5_3_OR_NEWER
            using (ProfileStepAutoConfigure.Auto())
#endif
            {
            AutoConfigureAll(host);
            }
#if UNITY_5_3_OR_NEWER
            using (ProfileStepProject.Auto())
#endif
            {
            TickProject(dt, host);
            }
#if UNITY_5_3_OR_NEWER
            using (ProfileStepEpochs.Auto())
#endif
            {
            TickEpochs(dt, host);
            }
#if UNITY_5_3_OR_NEWER
            using (ProfileStepBooksStages.Auto())
#endif
            {
            TickBooks(dt, host);
            TickBookPuzzles(dt);
            NoteRegionWiring();
            TickStages(dt);
            if (S.stage >= 6) TickFinale(dt, host);
            TickCollection();
            }
#if UNITY_5_3_OR_NEWER
            using (ProfileStepDataRelics.Auto())
#endif
            {
            TickDownloads(dt);
            TickDataSources(dt, host);
            TickFlywheel(dt, host);
            RelicTick(dt);
            }
            // 网吧通宵卡: the combo timer holds, so the combo does not fade. Training has no combo (2026-10-18): a round on the bar neither holds nor feeds it.
            if (S.comboTimer > 0 && !RelicFx.comboKeep)
            {
                S.comboTimer = Math.Max(0, S.comboTimer - dt);
                // Reading slowly never breaks a teaching streak; only a wrong answer does.
            }
#if UNITY_5_3_OR_NEWER
            using (ProfileStepCard.Auto())
#endif
            {
            if (WindowOpen && !MiniGameSurfaceActive) TickCard(dt);
            }

#if UNITY_5_3_OR_NEWER
            using (ProfileStepAutoAnswer.Auto())
#endif
            {
            AutoAnswer(dt, host);
            }
#if UNITY_5_3_OR_NEWER
            using (ProfileStepMarket.Auto())
#endif
            {
            TickMarket(dt, host);
            }
#if UNITY_5_3_OR_NEWER
            using (ProfileStepLife.Auto())
#endif
            {
            TickLifeEvents(dt, host);
            }
            double income = IncomePerSecond * dt;
            if (income > 0) { host.Earn(income); S.totalIncome += income; }
#if UNITY_5_3_OR_NEWER
            using (ProfileStepUnlocksChronicle.Auto())
#endif
            {
            CheckAbilities();
            CheckDesks();
            TickChronicle();
            }
        }

        void TickCard(double dt)
        {
            if (ReviewCard(S.desk) != null || ProjectAwaitingAnswer || EndingAvailable || S.chapterComplete) return;
            XgCard card = null;
            foreach (var c in S.cards) if (c.dataset == S.desk) card = c;
            if (card == null || card.timeLimit <= 0) return;
            card.age += dt;
            // Keep this question until the player answers; exceeding the timer only loses its bonus.
        }

        double Roll()
        {
            long x = S.rng;
            x ^= x << 13; x ^= (long)((ulong)x >> 7); x ^= x << 17;
            S.rng = x == 0 ? 0x5EED5EEDL : x;
            return (double)((ulong)S.rng >> 11) / (1UL << 53);
        }

        void Say(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            S.log.Add(text);
            if (S.log.Count > LogLength) S.log.RemoveAt(0);
            Message?.Invoke(text);
        }
    }
}
