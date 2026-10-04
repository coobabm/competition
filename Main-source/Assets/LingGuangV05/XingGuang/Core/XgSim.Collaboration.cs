using System;
using System.Collections.Generic;
using System.Threading;

namespace LingGuangV05.XingGuang
{
    /// <summary>Bounded, saveable collaboration. Transport only supplies predictions; this owns all settlement.</summary>
    public sealed partial class XgSim
    {
        public const int ReviewCapacity = 20, PrefetchPerDesk = 3, RouteWindow = 100, BrainWindow = 50, FeedWindow = 6;
        public const double NoisePenalty = 2, AuditProbability = .4, BrainGpuSeconds = .05;
        public const int NoiseCleanBatchLimit = 10;
        public const double NoiseCleanMoneyPerItem = 2, NoiseCleanGpuSecondsPerItem = .1;
        static long nextCollaborationGeneration;
        readonly long collaborationGeneration = Interlocked.Increment(ref nextCollaborationGeneration);
        public long CollaborationGeneration => collaborationGeneration;
        public bool BrainOnline;
        public string BrainStatus = "";
        public bool OfflineSimulation;
        public bool CollaborationEnabled => Has("label.coop");
        /// <summary>0 cards, 1 experiment, 2 final question, 3 chapter-two entry preview.</summary>
        public int LabelPresentationMode(bool endingQuestionReady) => S.chapterComplete ? 3 : EndingAvailable && endingQuestionReady ? 2 : ProjectAwaitingAnswer ? 1 : 0;
        public bool ReviewFull => S.queue.Count >= ReviewCapacity;
        public int GlobalAutoLevel => S.autoLevel;
        public event Action<XgAutoRecord> AutoRouted;

        // No reference to XgCatalog here: its static node initialization appends these nodes.
        public static readonly XgNode[] LabelNodes =
        {
            LabelNode("label.raise", null, "加薪", "Pay raise", "手动标注报酬 ×1 → ×30，每级至少 +0.5；与标注台商店共用。", "Hand pay ×1 to ×30, at least +0.5 per level; shared with the labelling shop.", 10, 17, 0),
            LabelNode("label.auto", "label.raise", "自动答题", "Auto labelling", "全局每级每桌 +0.3 题/秒；需任一 60% 检查点。", "Each global level adds 0.3 cards/s per eligible desk; needs any 60% checkpoint.", 60, 10, 130),
            LabelNode("label.coop", "label.auto", "拿不准才问我", "Ask when unsure", "用把握阈值分流；待复核总量 20，满时自动暂停。", "Route by confidence; automation pauses when the 20-card review inbox is full.", 300, 1, 260),
            LabelNode("label.brain", "label.coop", LingGuangV05.Core.AppNames.AiZh + "的大脑", "Local brain", "第四阶段：文字题用本地模型；离线明确回退检查点。", "Stage 4: judge text locally; offline predictions clearly use checkpoints.", 1200, 1, 390),
            LabelNode("label.hard", "label.coop", "难例加价", "Hard-case bonus", "人工复核答对：报酬 ×1.5，样本 ×2。", "Correct human reviews: pay ×1.5, samples ×2.", 800, 1, 520),
            LabelNode("label.audit", "label.coop", "抽检", "Quality audit", "拦下 40% 自动错题，退回复核，不污染训练数据。", "Return 40% of wrong automatic labels for review before they contaminate training data.", 1500, 1, 650),
            LabelNode("label.parallel", "label.brain", "多开推理", "Parallel inference", "本地推理并发 1 → 4，保留聊天优先；实际速度取决于本机。", "Local concurrency 1 to 4, with chat priority; measured speed depends on this computer.", 3000, 1, 780),
            LabelNode(TrapMemoryNode, "label.coop", "题库比对", "Trap memory", "记住平台抽过的金标题；再遇到时交给你人工复核，不自动提交。", "Remember the platform's known-answer trap items; when one comes back it goes to your review inbox instead of being auto-submitted.", 800, 1, 910),
            LabelNode(CaptchaAutofillNode, "label.coop", "验证码代填", "Captcha autofill", "需要第三阶段、手写数字检查点 ≥ 95%。它替你填验证码，3–6 秒后提交；太规律会被平台盯上。", "Needs stage 3 and a digits checkpoint of 95% or more. It fills in captchas for you after 3–6 s; being too regular gets noticed.", 2000, 1, 1040),
        };

        static XgNode LabelNode(string id, string parent, string name, string en, string note, string noteEn, double cost, int max, float y)
        { return new XgNode { id = id, tree = "label", parent = parent, kind = XgNodeKind.Label, name = name, nameEn = en, note = note, noteEn = noteEn, cost = cost, maxLevel = max, stage = 0, lane = "label", x = 0, y = y }; }

        public void PrepareCollaboration()
        {
            if (S.noise == null) S.noise = new List<XgLabelCount>();
            S.noise.RemoveAll(n => n == null || XgCatalog.Dataset(n.dataset) == null || !FiniteCollaboration(n.count) || n.count < 0);
            if (S.prefetch == null) S.prefetch = new List<XgCard>();
            if (S.queue == null) S.queue = new List<XgCard>();
            if (S.routeHistory == null) S.routeHistory = new List<XgRouteRecord>();
            if (S.brainStats == null) S.brainStats = new List<XgBrainRecord>();
            if (S.autoFeed == null) S.autoFeed = new List<XgAutoRecord>();
            if (S.autoTimers == null) S.autoTimers = new List<XgLabelCount>();
            if (S.coopVersion == 0)
            {
                int level = Math.Max(S.autoLevel, Math.Max(S.autoVision, Math.Max(S.autoSequence, S.autoLogic)));
                if (S.autoLevels != null)
                    foreach (var old in S.autoLevels)
                        if (old != null && FiniteCollaboration(old.count)) level = Math.Max(level, (int)Math.Max(0, Math.Min(XgCatalog.AutoMaxLevel, old.count)));
                S.autoLevel = level;
                if (S.autoLevels != null) S.autoLevels.Clear();
                S.autoVision = S.autoSequence = S.autoLogic = 0;
                S.coopVersion = 1;
            }
            S.autoLevel = Math.Max(0, Math.Min(XgCatalog.AutoMaxLevel, S.autoLevel));
            S.coopThreshold = !FiniteCollaboration(S.coopThreshold) || S.coopThreshold < .5 ? .8 : Math.Min(.99, S.coopThreshold);
            if (S.nextCardId < 1) S.nextCardId = 1;
            PrepareQuality();
        }

        public void FinishCollaborationRepair()
        {
            // Network requests do not survive load. Predictions do; retry gets a new ticket and generation.
            var all = new List<XgCard>();
            if (S.cards != null) all.AddRange(S.cards);
            all.AddRange(S.prefetch); all.AddRange(S.queue);
            foreach (var c in all) if (c != null && c.id >= S.nextCardId && c.id < long.MaxValue) S.nextCardId = c.id + 1;
            var ids = new HashSet<long>();
            foreach (var c in all)
            {
                if (c == null) continue;
                if (c.id <= 0 || !ids.Add(c.id)) { c.id = 0; EnsureCardId(c); ids.Add(c.id); }
                c.awaitingBrain = c.brainDispatched = false;
                bool comboSnapshot = c.kind == "combo" && c.judgeSource == "checkpoint" && c.comboPredictionReady;
                if (!FiniteCollaboration(c.confidence) || c.confidence < (comboSnapshot ? 0 : .5) || c.confidence > 1) c.hasJudgment = false;
                c.judgeSource = c.judgeSource ?? ""; c.judgeFailure = c.judgeFailure ?? "";
            }
            S.prefetch.RemoveAll(c => c == null || XgCatalog.Desk(c.dataset) == null);
            S.queue.RemoveAll(c => c == null || XgCatalog.Desk(c.dataset) == null);
            // Recover oversize legacy/corrupt buffers without silently paying or losing user review cards.
            if (S.queue.Count > ReviewCapacity)
            {
                S.prefetch.AddRange(S.queue.GetRange(ReviewCapacity, S.queue.Count - ReviewCapacity));
                S.queue.RemoveRange(ReviewCapacity, S.queue.Count - ReviewCapacity);
            }
            S.noise.RemoveAll(n => n == null || XgCatalog.Dataset(n.dataset) == null || !FiniteCollaboration(n.count) || n.count < 0);
            Trim(S.routeHistory, RouteWindow); Trim(S.autoFeed, FeedWindow);
            S.brainStats.RemoveAll(r => r == null || XgCatalog.Desk(r.dataset) == null);
            foreach (var desk in XgCatalog.Desks) TrimBrainStats(desk.id);
        }

        static bool FiniteCollaboration(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        static void Trim<T>(List<T> list, int size) { if (list.Count > size) list.RemoveRange(0, list.Count - size); }
        public void EnsureCardId(XgCard card) { if (card != null && card.id <= 0) card.id = S.nextCardId++; }
        public void SetCoopThreshold(double threshold) { if (FiniteCollaboration(threshold)) S.coopThreshold = Math.Max(.5, Math.Min(.99, threshold)); }
        public double Noise(string dataset) => Count(S.noise, dataset);
        public double NoiseTotal { get { double n = 0; foreach (var item in S.noise) n += item.count; return n; } }
        public double EffectiveLabelSamples(string dataset) => Math.Max(1, Samples(dataset) - NoisePenalty * Noise(dataset));

        /// <summary>Affordable simulated bad rows that can be discarded now; never a request to alter the local GGUF.</summary>
        public int CleanableNoise(string dataset, IXgHost host, int maxCount = NoiseCleanBatchLimit)
        {
            if (host == null || !Has("label.audit") || ProjectActive || maxCount <= 0 || XgCatalog.Dataset(dataset) == null ||
                host.Blocker != null || !FiniteCollaboration(host.Compute) || host.Compute <= 0 || !FiniteCollaboration(host.Money)) return 0;
            double noise = Noise(dataset);
            if (!FiniteCollaboration(noise) || noise < 1 || host.Money < NoiseCleanMoneyPerItem) return 0;
            double limit = Math.Min(Math.Min(maxCount, NoiseCleanBatchLimit), Math.Floor(noise));
            return (int)Math.Min(limit, Math.Floor(host.Money / NoiseCleanMoneyPerItem));
        }

        /// <summary>
        /// Discard at most ten known noisy rows from a simulated dataset. Clean labels, training steps,
        /// best checkpoints and rewards are unchanged; a future human-relabel workflow is separate.
        /// All actions happen synchronously on the simulation owner thread; a failed payment has no other effect.
        /// </summary>
        public int CleanNoise(string dataset, IXgHost host, int maxCount = NoiseCleanBatchLimit)
        {
            int count = CleanableNoise(dataset, host, maxCount);
            if (count == 0) return 0;
            double cost = count * NoiseCleanMoneyPerItem;
            if (!host.Spend(cost)) return 0;
            SetCount(S.noise, dataset, Math.Max(0, Noise(dataset) - count));
            S.totalSpent += cost;
            host.Train(count * NoiseCleanGpuSecondsPerItem);
            foreach (var run in Runs) if (run.dataset == dataset) Evaluate(run);
            return count;
        }

        public int ReviewCount(string desk = null) { int n = 0; foreach (var c in S.queue) if (desk == null || c.dataset == desk) n++; return n; }
        public XgCard ReviewCard(string desk) { foreach (var c in S.queue) if (c.dataset == desk) return c; return null; }
        public int PrefetchCount(string desk) { int n = 0; foreach (var c in S.prefetch) if (c.dataset == desk) n++; return n; }

        public int LabelNodeLevel(XgNode node)
        {
            if (node == null) return 0;
            return node.id == "label.raise" ? RaiseLevel : node.id == "label.auto" ? GlobalAutoLevel : Has(node.id) ? 1 : 0;
        }
        public double LabelNodeCost(XgNode node)
        { return node.id == "label.raise" ? NextRaiseCost : node.id == "label.auto" ? (GlobalAutoLevel >= XgCatalog.AutoMaxLevel ? double.PositiveInfinity : XgCatalog.AutoCost(GlobalAutoLevel)) : node.cost; }
        public NodeStatus StatusLabelNode(XgNode node, IXgHost host)
        {
            if (LabelNodeLevel(node) >= Math.Max(1, node.maxLevel)) return NodeStatus.Owned;
            if (node.id == "label.auto") { if (!CanBuyGlobalAuto(out _)) return NodeStatus.Locked; }
            else if (node.id == "label.coop") { if (GlobalAutoLevel < 1) return NodeStatus.Locked; }
            else if (node.parent != null && !Has(node.parent)) return NodeStatus.Locked;
            if (node.id == "label.brain")
            {
                if (S.stage < 4) return NodeStatus.Locked;
            }
            if (node.id == CaptchaAutofillNode && !CaptchaAutofillReady) return NodeStatus.Locked;
            return host == null || host.Money + 1e-9 < LabelNodeCost(node) ? NodeStatus.TooExpensive : NodeStatus.Buyable;
        }
        public string WhyLabelNode(XgNode node, IXgHost host)
        {
            switch (StatusLabelNode(node, host))
            {
                case NodeStatus.Owned: return T("已满级", "Maxed");
                case NodeStatus.Buyable: return T("可购买", "Available");
                case NodeStatus.TooExpensive: return T("经费不足", "Insufficient funds");
                default:
                    if (node.id == "label.auto") { CanBuyGlobalAuto(out string why); return why; }
                    if (node.id == "label.brain" && S.stage < 4) return T("需要第四阶段", "Requires stage 4");
                    if (node.id == CaptchaAutofillNode && Has(node.parent) && !CaptchaAutofillReady) return T("需要第三阶段，且手写数字检查点 ≥ 95%", "Requires stage 3 and a digits checkpoint of 95% or more");
                    return T("先解锁前置技能", "Unlock the prerequisite first");
            }
        }
        public bool BuyLabelNode(XgNode node, IXgHost host)
        {
            if (node == null || node.tree != "label" || StatusLabelNode(node, host) != NodeStatus.Buyable) return false;
            bool bought;
            if (node.id == "label.raise") bought = BuyRaise(host, 1) == 1;
            else if (node.id == "label.auto") bought = BuyGlobalAuto(host);
            else
            {
                if (!host.Spend(node.cost)) return false;
                S.totalSpent += node.cost; S.unlocked.Add(node.id); bought = true;
            }
            if (bought) NodeBought?.Invoke(node);
            return bought;
        }
        public bool CanBuyGlobalAuto(out string why)
        {
            why = null;
            // Hidden until the protagonist has the idea themselves (XgSim.Epiphany.cs).
            if (AutoLabelHidden) { why = T("也许有更省力的办法……", "Maybe there is an easier way…"); return false; }
            if (GlobalAutoLevel >= XgCatalog.AutoMaxLevel) { why = T("已满级", "Maxed"); return false; }
            foreach (var best in S.best)
                if (best.acc >= XgCatalog.AutoMinAccuracy && DeskOpen(best.dataset)) return true;
            why = T("先评估任一准确率 ≥ 60% 的检查点", "Assess any checkpoint with at least 60% accuracy first"); return false;
        }
        public bool BuyGlobalAuto(IXgHost host)
        {
            if (!CanBuyGlobalAuto(out string why)) { Say(why); return false; }
            double cost = XgCatalog.AutoCost(GlobalAutoLevel);
            if (host == null || !host.Spend(cost)) { Say(T("经费不足", "Insufficient funds")); return false; }
            S.totalSpent += cost; S.autoLevel++;
            if (S.autoLevel == 1) Say(T("全局自动标注已启动；没有检查点的桌仍由你负责。", "Global auto labelling is on; desks without a checkpoint still need you."));
            return true;
        }
        bool EligibleDesk(string desk) => DeskOpen(desk) && BestAcc(desk) > 0 && XgCatalog.Desk(desk) != null;
        public double CollaborationRate(string desk, IXgHost host)
        {
            if (host == null || host.Blocker != null || host.Compute <= 0 || ProjectActive || ReviewFull || AutomationHeld || !EligibleDesk(desk)) return 0;
            return GlobalAutoLevel * XgCatalog.AutoRatePerLevel * Math.Min(1, host.Compute);
        }
        public double CollaborationIncome(string desk, IXgHost host)
        {
            var recent = CollaborationStats(desk);
            double correctFraction = recent.total > 0 ? (double)recent.correct / recent.total : BestAcc(desk);
            return CollaborationRate(desk, host) * correctFraction * PayFor(desk, LevelOf(desk)) * QualityPayMultiplier;
        }
        public static bool IsBrainDesk(string desk)
        { return desk == "logic" || desk == "danmu" || desk == "spam" || desk == "headline" || desk == "review" || desk == "translate" || desk == "longtext" || desk == "crosssentence" || desk == "poems"; }
        bool UsesBrain(string desk) => Has("label.brain") && IsBrainDesk(desk) && BrainOnline && !OfflineSimulation;

        public void FillJudgmentBuffer()
        {
            if (GlobalAutoLevel <= 0 || ReviewFull || ProjectActive) return;
            foreach (var desk in XgCatalog.Desks)
            {
                if (!EligibleDesk(desk.id)) continue;
                int count = PrefetchCount(desk.id);
                for (; count < PrefetchPerDesk; count++) S.prefetch.Add(CreatePrefetchCard(desk.id));
            }
            foreach (var card in S.prefetch)
                if (!card.hasJudgment && !card.awaitingBrain && !UsesBrain(card.dataset))
                    JudgeCheckpoint(card, Has("label.brain") && IsBrainDesk(card.dataset) ? (OfflineSimulation ? "offline-simulation" : "brain-offline") : "");
        }
        void JudgeCheckpoint(XgCard card, string failure)
        {
            if (S.stage == 1 && card.kind == "combo" && TryGetComboSuggestion(card, out var comboGuess, out var comboConfidence))
            {
                card.guess = comboGuess; card.confidence = comboConfidence; card.hasJudgment = true;
                card.brainP = -1; card.judgeSource = "checkpoint"; card.judgeFailure = failure ?? "";
                card.awaitingBrain = card.brainDispatched = false;
                return;
            }
            var best = S.best.Find(b => b.dataset == card.dataset);
            var saved = best == null ? null : Model(best.modelId);
            var deployed = new XgRun
            {
                track = (int)XgCatalog.Dataset(card.dataset).track, dataset = card.dataset,
                arch = best == null || string.IsNullOrEmpty(best.arch) ? DefaultArchitecture(XgCatalog.Dataset(card.dataset).track) : best.arch,
                depth = saved == null ? 1 : saved.depth,
            };
            double accuracy = CardAccuracy(deployed, card, BestAcc(card.dataset));
            double acc = Math.Max(.5, Math.Min(1, accuracy));
            bool correct = card.roll < accuracy;
            // Deliberately a game estimate, never presented as a real model's probability.
            double jitter = ((uint)card.seed % 1009) / 1009.0;
            double difficulty = Math.Max(0, card.level - 1) * .015;
            double confidence = correct ? acc + .08 + jitter * .13 - difficulty : .5 + jitter * .25 - difficulty;
            card.guess = correct ? card.truth : !card.truth;
            card.confidence = Math.Max(.5, Math.Min(.99, confidence));
            card.hasJudgment = true; card.brainP = -1; card.judgeSource = "checkpoint"; card.judgeFailure = failure ?? "";
            card.awaitingBrain = card.brainDispatched = false;
        }
        public bool TryTakeBrainTicket(out XgJudgeTicket ticket)
        {
            ticket = null;
            if (ReviewFull || GlobalAutoLevel <= 0 || ProjectActive) return false;
            foreach (var c in S.prefetch)
            {
                if (!UsesBrain(c.dataset) || !EligibleDesk(c.dataset) || c.hasJudgment || c.awaitingBrain) continue;
                string prompt = XgJudgeProtocol.Prompt(c, English);
                if (string.IsNullOrEmpty(prompt)) { JudgeCheckpoint(c, "unsupported-prompt"); continue; }
                c.awaitingBrain = true; c.brainDispatched = false; c.judgeAttempt++;
                ticket = new XgJudgeTicket(c.id, CollaborationGeneration, c.judgeAttempt, c.dataset, prompt);
                return true;
            }
            return false;
        }
        XgCard TicketCard(XgJudgeTicket ticket)
        {
            if (ticket == null || ticket.generation != CollaborationGeneration) return null;
            foreach (var c in S.prefetch)
                if (c.id == ticket.cardId && c.dataset == ticket.dataset && c.judgeAttempt == ticket.attempt && c.awaitingBrain && !c.hasJudgment) return c;
            return null;
        }
        public bool DispatchBrainTicket(XgJudgeTicket ticket, IXgHost host)
        {
            var card = TicketCard(ticket);
            if (card == null || card.brainDispatched || ProjectActive || ReviewFull || !UsesBrain(card.dataset) || host == null || host.Blocker != null || host.Compute <= 0) return false;
            card.brainDispatched = true;
            host.Train(BrainGpuSeconds);
            return true;
        }
        public bool ReleaseBrainTicket(XgJudgeTicket ticket)
        {
            var card = TicketCard(ticket);
            if (card == null || card.brainDispatched) return false;
            card.awaitingBrain = false; return true;
        }
        public bool CompleteBrainTicket(XgJudgeTicket ticket, double? probability, string failure = null)
        {
            var card = TicketCard(ticket);
            if (card == null || !card.brainDispatched) return false;
            if (!probability.HasValue || !FiniteCollaboration(probability.Value) || probability.Value < 0 || probability.Value > 1)
            {
                JudgeCheckpoint(card, failure ?? "invalid-probability");
                BrainStatus = failure ?? "invalid-probability";
                return true;
            }
            double p = probability.Value;
            card.guess = p >= .5; card.confidence = Math.Max(p, 1 - p); card.brainP = p;
            card.judgeSource = "brain"; card.judgeFailure = ""; card.hasJudgment = true;
            card.awaitingBrain = card.brainDispatched = false;
            S.brainStats.Add(new XgBrainRecord { dataset = card.dataset, correct = card.guess == card.truth });
            TrimBrainStats(card.dataset);
            BrainStatus = "";
            return true;
        }
        void TrimBrainStats(string desk)
        {
            int count = 0; foreach (var r in S.brainStats) if (r.dataset == desk) count++;
            while (count > BrainWindow)
            { int i = S.brainStats.FindIndex(r => r.dataset == desk); S.brainStats.RemoveAt(i); count--; }
        }
        public int BrainJudgmentCount(string desk) { int n = 0; foreach (var r in S.brainStats) if (r.dataset == desk) n++; return n; }
        public double BrainAccuracy(string desk)
        {
            int total = 0, correct = 0;
            foreach (var r in S.brainStats) if (r.dataset == desk) { total++; if (r.correct) correct++; }
            return total == 0 ? 0 : (double)correct / total;
        }
        public bool TryCollaborativeSuggestion(string desk, out bool yes, out double confidence)
        {
            yes = false; confidence = 0;
            if (!CollaborationEnabled) return false;
            var card = ReviewCard(desk);
            if (card == null || !card.hasJudgment) return false;
            yes = card.guess; confidence = card.confidence; return true;
        }
        public XgCollaborationStats CollaborationStats(string desk = null)
        {
            var stats = new XgCollaborationStats();
            foreach (var r in S.routeHistory)
            {
                if (desk != null && r.dataset != desk) continue;
                stats.total++;
                if (r.automatic) { stats.automatic++; if (r.correct) stats.correct++; }
                if (r.audited) stats.audited++;
            }
            return stats;
        }
        void AddRoute(XgCard card, bool automatic, bool correct, bool audited)
        {
            S.routeHistory.Add(new XgRouteRecord { cardId = card.id, dataset = card.dataset, judgeSource = card.judgeSource, automatic = automatic, correct = correct, audited = audited });
            Trim(S.routeHistory, RouteWindow);
        }
        void EnqueueReview(XgCard card)
        {
            // Waiting in an inbox must not silently expire a timed bonus card.
            card.gold = false; card.timeLimit = 0; card.age = 0;
            S.queue.Add(card);
        }
        public bool Route(long cardId, IXgHost host)
        {
            if (host == null || host.Blocker != null || host.Compute <= 0 || ProjectActive || ReviewFull || AutomationHeld) return false;
            var card = S.prefetch.Find(c => c.id == cardId);
            if (card == null || !card.hasJudgment || card.awaitingBrain) return false;
            S.prefetch.Remove(card); // Claim once before any reward or event callback.
            // 题库比对: a trap item the platform already checked once goes to a human instead.
            if (CollaborationEnabled && (card.confidence < S.coopThreshold || TrapRecognized(card)))
            {
                EnqueueReview(card); AddRoute(card, false, false, false); return true;
            }
            bool correct = card.guess == card.truth;
            bool audited = CollaborationEnabled && !correct && Has("label.audit") && Roll() < AuditProbability;
            double pay = 0, fine = 0, unitPay = PayFor(card.dataset, card.level) * QualityPayMultiplier;
            bool spotChecked = false;
            if (audited) EnqueueReview(card);
            else
            {
                // The label leaves the lab: the 摆渡众包 platform may spot-check it (see XgSim.Quality.cs).
                fine = SettleSpotCheck(card, correct, unitPay, host, out spotChecked);
                NoteLabelSpeed();
                if (correct)
                {
                    pay = unitPay;
                    host.Earn(pay); S.totalIncome += pay; S.autoCorrect++; AddLabel(card.dataset);
                }
                else
                {
                    S.autoWrong++;
                    // A label the platform rejected is known to be wrong, so it never enters the lab's own data.
                    if (CollaborationEnabled && !spotChecked)
                    {
                        SetCount(S.noise, card.dataset, Noise(card.dataset) + 1);
                        foreach (var run in Runs) if (run.dataset == card.dataset) Evaluate(run);
                    }
                }
            }
            AddRoute(card, true, correct, audited);
            var record = new XgAutoRecord { cardId = card.id, dataset = card.dataset, question = card.question, judgeSource = card.judgeSource, guess = card.guess, confidence = card.confidence, correct = correct, audited = audited, pay = pay, spotChecked = spotChecked, fine = fine };
            S.autoFeed.Add(record); Trim(S.autoFeed, FeedWindow);
            AutoRouted?.Invoke(record);
            if (spotChecked && !correct) QualityFined?.Invoke(new XgQcFine { cardId = card.id, dataset = card.dataset, pay = unitPay, fine = fine, trap = IsTrap(card) });
            if (spotChecked) JudgeQuality();
            return true;
        }
        public XgAnswer AnswerQueued(long cardId, bool yes, IXgHost host)
        {
            var card = S.queue.Find(c => c.id == cardId);
            if (card == null || host == null) return default;
            S.queue.Remove(card);
            ObserveProgressionAnswer(card);
            NoteLabelSpeed();
            var result = new XgAnswer { accepted = true, queued = true, cardId = card.id, truth = card.truth, correct = yes == card.truth, trick = card.trick };
            if (result.correct)
            {
                result.corrected = card.hasJudgment && yes != card.guess;
                Hit(XgCatalog.Desk(card.dataset).comboWindow, result.corrected || card.trick ? 2 : 1);
                // Re-checked old rows pay as ordinary cards: no hard-case bonus on top of the cleaned noise.
                bool hard = Has("label.hard") && !card.recheck;
                result.pay = ManualPayFor(card.dataset, card.level) * ComboMultiplier * (hard ? 1.5 : 1);
                host.Earn(result.pay); S.totalIncome += result.pay; S.handCorrect++;
                QualityHandCorrect();
                if (card.recheck) RecheckCorrected(card.dataset);
                else MemeDriftLabelled(card.dataset);
                result.samples = hard ? 2 : 1;
                for (int i = 0; i < result.samples; i++) AddLabel(card.dataset);
            }
            else { BreakCombo(); S.handWrong++; }
            result.combo = S.combo;
            CheckDesks();
            return result;
        }
        public void TickCollaboration(double dt, IXgHost host)
        {
            TickQuality(dt);
            if (dt <= 0 || !FiniteCollaboration(dt) || host == null || host.Blocker != null || host.Compute <= 0 || ProjectActive || GlobalAutoLevel <= 0 || ReviewFull) return;
            FillJudgmentBuffer();
            foreach (var desk in XgCatalog.Desks)
            {
                double rate = CollaborationRate(desk.id, host);
                if (rate <= 0) continue;
                double timer = Math.Min(1, Math.Max(0, Count(S.autoTimers, desk.id))) + rate * Math.Min(1, dt);
                int guard = PrefetchPerDesk;
                while (timer >= 1 && guard-- > 0 && !ReviewFull)
                {
                    var card = S.prefetch.Find(c => c.dataset == desk.id && c.hasJudgment && !c.awaitingBrain);
                    if (card == null || !Route(card.id, host)) break;
                    timer -= 1;
                }
                // Backpressure never accrues an unbounded burst while a model or the player is busy.
                SetCount(S.autoTimers, desk.id, Math.Min(timer, .999999));
                if (ReviewFull) break;
            }
        }
    }
}
