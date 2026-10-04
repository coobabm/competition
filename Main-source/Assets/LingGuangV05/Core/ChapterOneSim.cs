using System;
using System.Collections.Generic;

namespace LingGuangV05.Core
{
    /// <summary>
    /// Single-owner, deterministic chapter-one simulation. DTOs contain all durable state.
    /// Call commands from one thread (the Unity presenter does so on its main thread).
    /// Tick uses persisted 0.25-second quanta, so partitioning frames/offline time changes no results.
    /// The bounded 7x5 board uses axial hex coordinates and directed adjacent synapses.
    /// </summary>
    public sealed class ChapterOneSim
    {
        private const double Quantum = .25;
        private const double Epsilon = 1e-9;
        private const double ResourceCeiling = 1e12;
        private readonly Dictionary<int, NodeState> nodeById = new Dictionary<int, NodeState>();
        private readonly Dictionary<int, List<EdgeState>> outgoing = new Dictionary<int, List<EdgeState>>();
        private bool yesReachable, noReachable, resolvingCard;
        private int fanOutCount, convergeCount, hiddenCount;

        public ChapterOneSim(GameState state = null, GameConfig config = null)
        {
            Config = config ?? new GameConfig();
            ValidateConfig();
            S = state ?? CreateFresh();
            ValidateState();
            if (S.story == null) S.story = new LingGuangV05.Core.Story.StoryState();
            S.story.Repair();
            RebuildTopology();
            if (S.activeCard == null) DrawCard();
            LastMessage = state == null
                ? "老周：赠你一张二手亮影 750Ti、一个机箱和 ¥" + Config.starterMoney.ToString("0") + " 启动经费。先核对邮件题卡，再开接活。"
                : "已恢复题卡、神经元板、心跳和测试进度。";
        }

        public GameState S { get; private set; }
        public GameConfig Config { get; private set; }
        public CardData CurrentCard { get { return S.activeCard; } }
        public event Action Changed;
        public event Action<CardResolution> CardResolved;
        public event Action<TrainingPulse> Pulsed;
        /// <summary>Named game events for the story layer: (name, optional argument). See ChapterOneStoryCatalog.Signals.</summary>
        public event Action<string, string> Signal;
        public string LastMessage { get; private set; }
        public bool HasOutputPath { get { return yesReachable || noReachable; } }
        public int FanOutCount { get { return fanOutCount; } }
        public int ConvergeCount { get { return convergeCount; } }
        public double MemoryUsed { get { return S.nodes.Count * Config.nodeMemory + S.edges.Count * Config.edgeMemory; } }
        public double MemoryCapacity { get { return S.gpuCount * Config.memoryPerGpu; } }
        private double RequestedWatts { get { return S.gpuCount * Config.gpuWatts + S.caseCount * Config.caseWatts; } }
        private bool Powered { get { return !S.breakerTripped && !S.unpaidPower && S.gpuCount > 0 && RequestedWatts <= Config.powerLimitWatts; } }
        public double PowerWatts { get { return Powered ? RequestedWatts : 0; } }
        public double HeartbeatsPerSecond
        {
            get
            {
                if (!Powered) return 0;
                double throttle = S.temperature <= Config.throttleTemperature ? 1 : Math.Max(Config.minimumThrottle,
                    1 - (S.temperature - Config.throttleTemperature) / Math.Max(1, Config.maximumTemperature - Config.throttleTemperature));
                return S.gpuCount * Config.heartbeatPerGpu * (1 + (S.trainingLevel - 1) * Config.trainingSpeedPerLevel) * throttle;
            }
        }
        private double ParameterCapacity { get { return (hiddenCount * Config.parametersPerHiddenNode + S.edges.Count * .25) * Config.samplesPerParameter; } }
        public double Accuracy
        {
            get
            {
                if (!HasOutputPath) return 0;
                double e = Math.Min(S.samples, Math.Min(ParameterCapacity, S.steps));
                double cap = Math.Min(Config.maxAccuracy, Config.initialAccuracyCap + (S.semanticLevel - 1) * Config.accuracyCapPerLevel);
                double structure = 1 + Config.structureBonus * (Math.Sqrt(fanOutCount) + Math.Sqrt(convergeCount));
                double value = Config.accuracyFloor + (cap - Config.accuracyFloor) * e / (e + Config.effectiveScaleHalf) * structure;
                // A one-output classifier cannot distinguish both classes, regardless of hardware investment.
                return Clamp(Math.Min(cap, value) * (yesReachable && noReachable ? 1 : .5), 0, Config.maxAccuracy);
            }
        }
        public double IncomePerSecond { get { return S.jobEnabled && S.cardsReviewed >= 3 && HasOutputPath ? HeartbeatsPerSecond * Accuracy * Config.incomePerHeartbeat : 0; } }
        public string Bottleneck
        {
            get
            {
                if (S.unpaidPower) return "电费：停电，核对题卡可抵扣欠费";
                if (S.breakerTripped) return "供电：先降低负载，再手动合闸";
                if (!HasOutputPath) return "连接：输入还没有通向输出";
                if (!yesReachable || !noReachable) return "连接：是、否两个输出都需要接通";
                if (S.samples <= S.steps && S.samples <= ParameterCapacity) return "样本：继续核对题卡";
                if (S.steps <= ParameterCapacity) return "算力：让心跳训练，或升级训练 / 显卡";
                return "显存 / 参数：增加神经元和突触";
            }
        }
        /// <summary>A new game is in the prologue until the opening setup is done; nothing is saved before that.</summary>
        public bool InPrologue { get { return S.prologue == 1; } }

        /// <summary>
        /// Step 8 of the prologue: the opening setup is saved and the game proper starts. Validated like a save;
        /// returns false (and changes nothing) if the profile is not acceptable.
        /// </summary>
        public bool CompletePrologue(PrologueProfile profile)
        {
            if (!InPrologue || profile == null || PrologueProfile.Problem(profile) != null) return false;
            S.aiName = profile.name.Trim(); S.aiSelf = profile.self.Trim(); S.aiCallMe = profile.callMe.Trim(); S.personality = profile.personality.Trim();
            S.targetWarmth = profile.warmth; S.targetPlay = profile.play; S.targetOpinion = profile.opinion;
            S.toneWords = new List<string>(profile.words);
            if (!AppInstalled) S.appInstallState = 2;
            S.prologue = 2;
            Raise("prologue.done");
            return Accept(S.aiName + "：尚无智能");
        }

        /// <summary>False only on a new game until 灵光.exe has been downloaded through the browser.</summary>
        public bool AppInstalled { get { return S.appInstallState != 1; } }
        /// <summary>The browser download finished: 灵光.exe appears on the desktop. Idempotent.</summary>
        public bool InstallApp()
        {
            if (AppInstalled) return false;
            S.appInstallState = 2;
            Raise("app.installed");
            return Accept(AppNames.ExeZh + " 下载完成，已放到桌面。");
        }
        public bool CanStartExam
        {
            get
            {
                return !S.examActive && !S.chapterOneComplete && Powered && yesReachable && noReachable &&
                    S.cardsReviewed >= Config.examRequiredCards && S.steps >= Config.examRequiredSteps && Accuracy + Epsilon >= Config.examRequiredAccuracy;
            }
        }

        public bool SubmitCard(int cardId, bool playerAnswer)
        {
            // Correction pulses are synchronous presentation callbacks. Guard the complete mutation
            // against callbacks trying to submit the same still-visible card again.
            if (resolvingCard) return false;
            resolvingCard = true;
            try { return ResolveCard(cardId, playerAnswer); }
            finally { resolvingCard = false; }
        }
        private bool ResolveCard(int cardId, bool playerAnswer)
        {
            if (CurrentCard == null || cardId != CurrentCard.id) return Reject("这张题卡已经处理，不能重复领取样本。");
            var card = CurrentCard;
            bool correct = playerAnswer == card.expectedYes;
            bool corrected = correct && card.predictedYes != card.expectedYes;
            var resolution = new CardResolution { cardId = card.id, correct = correct, corrected = corrected, explanation = card.explanation };
            string message;
            TrainingPulse correctionPulse = null;
            string examSignal = null;
            bool examCard = S.examActive;
            if (S.examActive)
            {
                S.examAnswered++;
                if (correct) S.examCorrect++;
                message = (correct ? "判断正确。" : "这次判断有误。") + card.explanation;
                if (S.examAnswered >= Config.examQuestions)
                {
                    S.examActive = false;
                    S.lastExamPassed = S.examCorrect >= Config.examPassScore;
                    examSignal = S.lastExamPassed ? "exam.passed" : "exam.failed";
                    if (S.lastExamPassed)
                    {
                        S.chapterOneComplete = true;
                        if (!S.examRewardGranted) { S.money = AddBounded(S.money, Config.examReward); S.examRewardGranted = true; }
                        message = "入门测试通过：" + S.examCorrect + "/" + Config.examQuestions + "。第一章完成，收到 ¥" + Config.examReward.ToString("0") + " 合同奖励；第二章尚未开放。";
                    }
                    else message = "入门测试未通过：" + S.examCorrect + "/" + Config.examQuestions + "（需 " + Config.examPassScore + "）。训练进度保留，可以继续复盘后重试。";
                }
            }
            else
            {
                S.cardsReviewed++;
                if (correct)
                {
                    S.samples = AddBounded(S.samples, Config.samplePerCard * (corrected ? 2 : 1));
                    S.learningPoints = AddBounded(S.learningPoints, corrected ? 2 : 1);
                    if (corrected)
                    {
                        S.correctedCards++;
                        correctionPulse = WeakenRecordedRoute();
                        if (S.mistakes.Count == 50) S.mistakes.RemoveAt(0);
                        S.mistakes.Add(card.prompt + " → " + (card.expectedYes ? "是" : "否") + "：" + card.explanation);
                    }
                    message = (corrected ? "纠正成功：反向脉冲削弱刚才的路径，获得双倍样本。" : "判断正确，样本和学习点已记录。") + card.explanation;
                    if (S.unpaidPower && S.billDue > 0)
                    {
                        double repaid = Math.Min(S.billDue, Config.outageRepaymentPerCard);
                        S.billDue = Math.Max(0, S.billDue - repaid);
                        S.outageDebtRepaid = AddBounded(S.outageDebtRepaid, repaid);
                        if (S.billDue <= Epsilon) { S.billDue = 0; S.unpaidPower = false; }
                        message += " 人工标注抵扣电费 ¥" + repaid.ToString("0.00") + (S.unpaidPower ? "，继续核对可恢复供电。" : "，供电已恢复。");
                    }
                }
                else message = "标注有误，本次不增加样本或学习点。" + card.explanation;
            }
            // Advance before callbacks so a stale/reentrant UI callback cannot claim the same card twice.
            DrawCard();
            LastMessage = message;
            if (correctionPulse != null) Pulsed?.Invoke(correctionPulse);
            CardResolved?.Invoke(resolution);
            if (examSignal != null) Raise(examSignal);
            else if (!examCard) Raise("card.resolved", corrected ? "corrected" : correct ? "correct" : "wrong");
            Notify();
            return true;
        }

        public bool BuyGpu()
        {
            if (S.gpuCount >= S.caseCount * Config.gpusPerCase) return Reject("机箱插槽已满；每个机箱容纳 " + Config.gpusPerCase + " 张卡。");
            if (!CanSpend(Config.gpuPrice)) return Reject("经费不足：显卡需要 ¥" + Config.gpuPrice.ToString("0") + "。");
            S.money -= Config.gpuPrice; S.gpuCount++;
            if (RequestedWatts > Config.powerLimitWatts) TripBreaker();
            Raise("gpu.bought");
            return Accept("已购入二手亮影 750Ti：+" + Config.memoryPerGpu.ToString("0") + "M 显存、+" + Config.heartbeatPerGpu.ToString("0.##") + " 次基础心跳 / 秒、+" + Config.gpuWatts.ToString("0") + "W。" + (S.breakerTripped ? "负载超限，已跳闸。" : ""));
        }
        public bool SellGpu()
        {
            if (S.gpuCount <= 1) return Reject("至少保留赠送的第一张卡；欠费时可人工核对题卡抵扣电费。");
            if (MemoryUsed > (S.gpuCount - 1) * Config.memoryPerGpu + Epsilon) return Reject("出售后显存不足，请先缩小神经元板。");
            S.gpuCount--; S.money = AddBounded(S.money, Config.gpuPrice * Config.gpuResaleFraction);
            Raise("gpu.sold");
            return Accept("已出售一张闲置显卡，回收 ¥" + (Config.gpuPrice * Config.gpuResaleFraction).ToString("0") + "。若已跳闸，请到电表手动合闸。");
        }
        public bool BuyCase()
        {
            if (S.caseCount >= Config.maxCases) return Reject("第一章机箱数量已到上限。");
            if (!CanSpend(Config.casePrice)) return Reject("经费不足：机箱需要 ¥" + Config.casePrice.ToString("0") + "。");
            S.money -= Config.casePrice; S.caseCount++;
            if (RequestedWatts > Config.powerLimitWatts) TripBreaker();
            Raise("case.bought");
            return Accept("新机箱已接入，增加 " + Config.gpusPerCase + " 个显卡插槽，也会增加基础功耗。");
        }
        public bool PayBill()
        {
            if (S.billDue <= Epsilon) return Reject("没有待缴电费。");
            if (!CanSpend(S.billDue)) return Reject("经费不足：可出售闲置显卡，或人工核对题卡抵扣欠费（不发放现金）。");
            S.money = Math.Max(0, S.money - S.billDue); S.billDue = 0; S.unpaidPower = false;
            Raise("bill.paid");
            return Accept("电费已结清，供电恢复；若仍然跳闸，请先降低负载后合闸。");
        }
        public bool ResetBreaker()
        {
            if (!S.breakerTripped) return Reject("断路器当前已合闸。");
            if (RequestedWatts > Config.powerLimitWatts) return Reject("负载仍超过 " + Config.powerLimitWatts.ToString("0") + "W；先出售多余显卡。");
            S.breakerTripped = false; Raise("breaker.reset"); return Accept("已手动合闸。" + (S.unpaidPower ? "仍有欠费待结清。" : "供电正常。"));
        }
        public bool UpgradeSkill()
        {
            if (S.semanticLevel >= Config.maxSkillLevel) return Reject("语义技能书已到第一章上限。");
            double cost = Config.skillPointCost * S.semanticLevel;
            if (S.learningPoints + Epsilon < cost) return Reject("语义技能书需要 " + cost.ToString("0") + " 学习点。");
            S.learningPoints = Math.Max(0, S.learningPoints - cost); S.semanticLevel++;
            Raise("skill.upgraded", "semantic");
            return Accept("语义技能 Lv" + S.semanticLevel + "：提高二分类准确率上限；仍需样本、参数和训练步支持。");
        }
        public bool UpgradeTraining()
        {
            if (S.trainingLevel >= Config.maxTrainingLevel) return Reject("训练研究已到第一章上限。");
            double cost = Config.trainingPointCost * S.trainingLevel;
            if (S.learningPoints + Epsilon < cost) return Reject("训练研究需要 " + cost.ToString("0") + " 学习点。");
            S.learningPoints = Math.Max(0, S.learningPoints - cost); S.trainingLevel++;
            Raise("skill.upgraded", "training");
            return Accept("训练研究 Lv" + S.trainingLevel + "：每张显卡的心跳加快。不会凭空增加样本。");
        }
        public bool AddNode(int q, int r)
        {
            if (q < 1 || q > 5 || r < 0 || r > 4) return Reject("只能在中间五列的空六角格放置神经元。");
            foreach (var n in S.nodes) if (n.q == q && n.r == r) return Reject("这个格子已经有神经元。");
            if (MemoryUsed + Config.nodeMemory > MemoryCapacity + Epsilon) return Reject("显存不足：一个神经元需要 " + Config.nodeMemory.ToString("0") + "M。");
            int id = 0; foreach (var n in S.nodes) id = Math.Max(id, n.id);
            S.nodes.Add(new NodeState { id = id + 1, q = q, r = r, kind = NodeKind.Hidden });
            TopologyChanged(); Raise("node.added"); return Accept("已放置神经元。选中两个相邻神经元可切换有向突触。");
        }
        public bool RemoveNode(int id)
        {
            NodeState node;
            if (!nodeById.TryGetValue(id, out node) || node.kind != NodeKind.Hidden) return Reject("输入和是 / 否端口不可删除。");
            S.nodes.Remove(node); S.edges.RemoveAll(e => e.from == id || e.to == id);
            TopologyChanged(); Raise("node.removed"); return Accept("已移除神经元及其突触，显存已释放。");
        }
        public bool ToggleEdge(int from, int to)
        {
            NodeState a, b;
            if (!nodeById.TryGetValue(from, out a) || !nodeById.TryGetValue(to, out b) || !CanLink(a, b))
                return Reject("只能连接相邻六角格；输入不能接收信号，输出不能向外发送。");
            var edge = S.edges.Find(e => e.from == from && e.to == to);
            if (edge != null) S.edges.Remove(edge);
            else
            {
                if (MemoryUsed + Config.edgeMemory > MemoryCapacity + Epsilon) return Reject("显存不足，无法增加突触。");
                S.edges.Add(new EdgeState { from = from, to = to, weight = Config.initialWeight });
            }
            TopologyChanged(); Raise(edge == null ? "edge.added" : "edge.removed"); return Accept(edge == null ? "已连接 " + from + " → " + to + "。方向决定信号去向。" : "已移除有向突触。");
        }
        public bool StartExam()
        {
            if (S.chapterOneComplete) return Reject("第一章已完成，合同奖励仅发放一次；第二章尚未开放。");
            if (!CanStartExam) return Reject(S.examActive ? "测试正在进行，请完成当前题卡。" :
                "测试需要供电正常、是 / 否双输出、核对 " + Config.examRequiredCards + " 张卡、" + Config.examRequiredSteps.ToString("0") + " 训练步和 " + (Config.examRequiredAccuracy * 100).ToString("0") + "% 准确率。");
            S.examActive = true; S.examAttempts++; S.examAnswered = 0; S.examCorrect = 0; S.examTemplateMask = 0;
            DrawCard(); Raise("exam.started"); return Accept("入门测试开始：核对 " + Config.examQuestions + " 封邮件，至少 " + Config.examPassScore + " 封正确通过。AI 只给建议，以你的判断计分。");
        }
        public void SetJobEnabled(bool enabled)
        {
            bool changed = S.jobEnabled != enabled;
            S.jobEnabled = enabled;
            if (changed) Raise(enabled ? "job.enabled" : "job.disabled");
            Accept(enabled ? (S.cardsReviewed < 3 ? "任务已登记：先核对 3 张题卡，网吧老板才会开始付费。" : "垃圾邮件过滤任务已开启；收入取决于准确率、心跳和供电。") : "接活已暂停，训练仍继续。");
        }
        /// <summary>The prologue: play time passes (the desktop clock), nothing else does.</summary>
        public void AdvanceClock(double seconds)
        {
            if (!Finite(seconds) || seconds <= 0) return;
            S.gameSeconds = Math.Min(S.gameSeconds + Math.Min(seconds, Config.maxTickSeconds), ResourceCeiling);
        }

        public void Tick(double seconds)
        {
            if (!Finite(seconds) || seconds <= 0) return;
            S.tickRemainder += Math.Min(seconds, Config.maxTickSeconds);
            bool advanced = false;
            while (S.tickRemainder + Epsilon >= Quantum)
            {
                S.tickRemainder = Math.Max(0, S.tickRemainder - Quantum);
                // Split at day boundaries inside a fixed quantum, even for custom day lengths.
                double remaining = Quantum;
                while (remaining > Epsilon)
                {
                    double dt = Math.Min(remaining, Config.dayLengthSeconds - S.daySeconds);
                    if (dt <= Epsilon) { SettleDay(); continue; }
                    AdvanceSlice(dt); remaining -= dt;
                    if (S.daySeconds + Epsilon >= Config.dayLengthSeconds) SettleDay();
                }
                advanced = true;
            }
            if (advanced) Notify();
        }

        private void AdvanceSlice(double dt)
        {
            if (RequestedWatts > Config.powerLimitWatts) TripBreaker();
            double watts = PowerWatts;
            double target = Config.ambientTemperature + watts * Config.heatPerWatt / Math.Max(1, S.caseCount);
            S.temperature = Clamp(target + (S.temperature - target) * Math.Exp(-dt / Config.thermalTimeConstant), Config.ambientTemperature, Config.maximumTemperature);
            S.energyKwh += watts / 1000 * 24 * dt / Config.dayLengthSeconds;
            double income = IncomePerSecond * dt;
            S.money = AddBounded(S.money, income); S.totalEarned = AddBounded(S.totalEarned, income);
            for (int i = 0; i < S.nodes.Count; i++)
            {
                var n = S.nodes[i]; n.charge = Math.Max(0, n.charge - Config.leakPerSecond * dt);
                n.fatigue = Math.Max(0, n.fatigue - Config.fatigueRecoveryPerSecond * dt);
            }
            bool removed = false;
            for (int i = S.edges.Count - 1; i >= 0; i--)
            {
                var e = S.edges[i];
                // A powered idle synapse decays; a powered-off digital brain does not lose its saved parameters.
                if (watts > 0) e.weight -= Config.weightDecayPerSecond * dt;
                if (e.weight < Config.minimumWeight) { S.edges.RemoveAt(i); removed = true; }
            }
            if (removed) TopologyChanged();
            S.heartbeatAccumulator += HeartbeatsPerSecond * dt;
            while (S.heartbeatAccumulator + Epsilon >= 1)
            {
                S.heartbeatAccumulator = Math.Max(0, S.heartbeatAccumulator - 1);
                FireHeartbeat();
            }
            S.gameSeconds += dt; S.daySeconds += dt;
        }
        private void FireHeartbeat()
        {
            var queue = new Queue<int>(); var fired = new HashSet<int>(); var visible = new List<int>();
            foreach (var node in S.nodes)
                if (node.kind == NodeKind.Input) { node.charge += Config.signalStrength; queue.Enqueue(node.id); }
            bool outputFired = false;
            while (queue.Count > 0)
            {
                int id = queue.Dequeue(); var node = nodeById[id];
                if (fired.Contains(id) || node.charge + Epsilon < Config.threshold + node.fatigue * Config.fatigueThresholdFactor) continue;
                fired.Add(id); visible.Add(id); node.charge = 0; node.fatigue = Math.Min(3, node.fatigue + Config.fatiguePerFire);
                if (node.kind == NodeKind.Yes || node.kind == NodeKind.No) outputFired = true;
                foreach (var edge in outgoing[id])
                {
                    var target = nodeById[edge.to];
                    target.charge = Math.Min(10, target.charge + Config.signalStrength * edge.weight);
                    edge.weight = Math.Min(Config.maximumWeight, edge.weight + Config.weightStrengthen);
                    if (!fired.Contains(target.id)) queue.Enqueue(target.id);
                }
            }
            if (outputFired && HasOutputPath) S.steps = AddBounded(S.steps, 1);
            Pulsed?.Invoke(new TrainingPulse { nodeIds = visible.ToArray(), correction = false });
        }
        private void SettleDay()
        {
            S.billDue = AddBounded(S.billDue, S.energyKwh * Config.electricityPrice);
            S.energyKwh = 0; S.daySeconds = 0; S.day++;
            if (S.billDue > Epsilon && CanSpend(S.billDue))
            {
                S.money = Math.Max(0, S.money - S.billDue); S.billDue = 0; S.unpaidPower = false;
                LastMessage = "第 " + S.day + " 天：电费已自动结清。";
            }
            else if (S.billDue > Epsilon)
            {
                bool wasPaid = !S.unpaidPower;
                S.unpaidPower = true;
                LastMessage = "欠费 ¥" + S.billDue.ToString("0.00") + "，训练和接活暂停。可出售闲置显卡，或继续人工核对题卡抵扣电费。";
                if (wasPaid) Raise("power.unpaid");
            }
            Raise("day.ended", S.day.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        private TrainingPulse WeakenRecordedRoute()
        {
            foreach (var used in S.activeRouteEdges)
            {
                var edge = S.edges.Find(e => e.from == used.from && e.to == used.to);
                if (edge != null) edge.weight = Math.Max(Config.minimumWeight, edge.weight * Config.correctionWeightFactor);
            }
            var reverse = S.activeRoute.ToArray(); Array.Reverse(reverse);
            return new TrainingPulse { nodeIds = reverse, correction = true };
        }
        private void TopologyChanged() { RebuildTopology(); RecordInferenceRoute(); }
        private void RebuildTopology()
        {
            nodeById.Clear(); outgoing.Clear(); hiddenCount = 0;
            foreach (var n in S.nodes) { nodeById.Add(n.id, n); outgoing.Add(n.id, new List<EdgeState>()); if (n.kind == NodeKind.Hidden) hiddenCount++; }
            foreach (var e in S.edges) outgoing[e.from].Add(e);
            var visited = new HashSet<int>(); var queue = new Queue<int>();
            foreach (var n in S.nodes) if (n.kind == NodeKind.Input) { queue.Enqueue(n.id); visited.Add(n.id); }
            while (queue.Count > 0) foreach (var e in outgoing[queue.Dequeue()]) if (visited.Add(e.to)) queue.Enqueue(e.to);
            yesReachable = false; noReachable = false;
            foreach (var n in S.nodes) if (visited.Contains(n.id)) { if (n.kind == NodeKind.Yes) yesReachable = true; if (n.kind == NodeKind.No) noReachable = true; }
            CountStructures();
        }
        private void CountStructures()
        {
            fanOutCount = convergeCount = 0;
            var visited = new HashSet<int>();
            foreach (var n in S.nodes)
            {
                if (n.kind != NodeKind.Hidden || !visited.Add(n.id)) continue;
                var component = new List<int>(); var queue = new Queue<int>(); queue.Enqueue(n.id);
                while (queue.Count > 0)
                {
                    int id = queue.Dequeue(); component.Add(id);
                    foreach (var e in S.edges)
                    {
                        int neighbor = e.from == id ? e.to : e.to == id ? e.from : -1;
                        if (neighbor >= 0 && nodeById[neighbor].kind == NodeKind.Hidden && visited.Add(neighbor)) queue.Enqueue(neighbor);
                    }
                }
                if (component.Count != 4) continue;
                var set = new HashSet<int>(component); int internalEdges = 0;
                foreach (var e in S.edges) if (set.Contains(e.from) && set.Contains(e.to)) internalEdges++;
                if (internalEdges != 3) continue;
                foreach (int id in component)
                {
                    int outs = 0, ins = 0;
                    foreach (var e in S.edges) { if (e.from == id && set.Contains(e.to)) outs++; if (e.to == id && set.Contains(e.from)) ins++; }
                    if (outs == 3) fanOutCount++;
                    if (ins == 3) convergeCount++;
                }
            }
        }
        private void RecordInferenceRoute()
        {
            S.activeRoute.Clear(); S.activeRouteEdges.Clear();
            if (CurrentCard == null) return;
            var parents = new Dictionary<int, int>(); var queue = new Queue<int>(); int goal = -1;
            foreach (var n in S.nodes) if (n.kind == NodeKind.Input) { parents[n.id] = -1; queue.Enqueue(n.id); }
            NodeKind target = CurrentCard.predictedYes ? NodeKind.Yes : NodeKind.No;
            while (queue.Count > 0)
            {
                int id = queue.Dequeue();
                if (nodeById[id].kind == target) { goal = id; break; }
                foreach (var e in outgoing[id]) if (!parents.ContainsKey(e.to)) { parents[e.to] = id; queue.Enqueue(e.to); }
            }
            for (int id = goal; id >= 0; id = parents[id]) S.activeRoute.Add(id);
            S.activeRoute.Reverse();
            for (int i = 1; i < S.activeRoute.Count; i++) S.activeRouteEdges.Add(new EdgeState { from = S.activeRoute[i - 1], to = S.activeRoute[i] });
        }
        private void DrawCard()
        {
            int index = (int)(NextRandom() % EmailTemplates.Count);
            if (S.examActive)
            {
                for (int i = 0; i < EmailTemplates.Count && (S.examTemplateMask & (1L << index)) != 0; i++) index = (index + 1) % EmailTemplates.Count;
                S.examTemplateMask |= 1L << index;
            }
            var template = EmailTemplates.Get(index);
            bool prediction = NextUnit() < Accuracy ? template.yes : !template.yes;
            if (!yesReachable && noReachable) prediction = false;
            if (yesReachable && !noReachable) prediction = true;
            // A simulated suggestion, deliberately not a model probability. Some errors are overconfident.
            double confidence = Clamp(.52 + .45 * NextUnit(), .5, .97);
            S.activeCard = new CardData
            {
                id = S.nextCardId++, expectedYes = template.yes, predictedYes = prediction, confidence = confidence,
                prompt = "邮件 #" + (1000 + NextRandom() % 9000) + "：" + template.text + "\n这封邮件是垃圾邮件吗？",
                explanation = template.explanation, category = S.examActive ? "入门测试 · 垃圾邮件" : "训练题卡 · 垃圾邮件"
            };
            RecordInferenceRoute();
        }
        private uint NextRandom()
        {
            uint value = unchecked((uint)S.rngState); value ^= value << 13; value ^= value >> 17; value ^= value << 5;
            S.rngState = value; return value;
        }
        private double NextUnit() { return NextRandom() / 4294967296.0; }
        private GameState CreateFresh()
        {
            var s = new GameState { money = Config.starterMoney, samples = Config.starterSamples, gpuCount = Config.starterGpus, temperature = Config.ambientTemperature, appInstallState = 1, prologue = 1 };
            s.nodes.Add(new NodeState { id = 1, q = 0, r = 2, kind = NodeKind.Input });
            for (int q = 1; q <= 5; q++) s.nodes.Add(new NodeState { id = q + 1, q = q, r = 2, kind = NodeKind.Hidden });
            s.nodes.Add(new NodeState { id = 7, q = 6, r = 2, kind = NodeKind.Yes });
            s.nodes.Add(new NodeState { id = 8, q = 6, r = 1, kind = NodeKind.No });
            for (int id = 1; id < 7; id++) s.edges.Add(new EdgeState { from = id, to = id + 1, weight = Config.initialWeight });
            s.edges.Add(new EdgeState { from = 6, to = 8, weight = Config.initialWeight }); return s;
        }
        private void ValidateConfig()
        {
            // Field-only configuration is both inspector/JSON friendly and centrally range checked.
            foreach (var f in typeof(GameConfig).GetFields())
                if (f.FieldType == typeof(double)) { double v = (double)f.GetValue(Config); if (!Finite(v) || v < 0 || v > ResourceCeiling) throw new ArgumentException("Invalid configuration: " + f.Name); }
            if (Config.dayLengthSeconds < Quantum || Config.maxTickSeconds < Quantum || Config.memoryPerGpu <= 0 || Config.nodeMemory <= 0 || Config.edgeMemory <= 0 ||
                Config.effectiveScaleHalf <= 0 || Config.thermalTimeConstant <= 0 || Config.threshold <= 0 || Config.signalStrength <= 0 || Config.gpuPrice <= 0 || Config.casePrice <= 0 ||
                Config.examQuestions < 1 || Config.examQuestions > EmailTemplates.Count || Config.examPassScore < 1 || Config.examPassScore > Config.examQuestions ||
                Config.starterGpus < 1 || Config.starterGpus > Config.gpusPerCase || Config.gpusPerCase < 1 || Config.gpusPerCase > 8 || Config.maxCases < 1 || Config.maxCases > 100 ||
                Config.maxSkillLevel < 1 || Config.maxSkillLevel > 20 || Config.maxTrainingLevel < 1 || Config.maxTrainingLevel > 20 || Config.examRequiredCards < 0 ||
                Config.gpuResaleFraction >= 1 || Config.correctionWeightFactor > 1 || Config.minimumThrottle > 1 || Config.maxAccuracy > 1 ||
                Config.accuracyFloor > Config.initialAccuracyCap || Config.initialAccuracyCap > Config.maxAccuracy || Config.examRequiredAccuracy > Config.maxAccuracy ||
                Config.minimumWeight <= 0 || Config.minimumWeight >= Config.initialWeight || Config.initialWeight > Config.maximumWeight || Config.maximumWeight > 10 ||
                Config.throttleTemperature < Config.ambientTemperature || Config.maximumTemperature <= Config.throttleTemperature)
                throw new ArgumentException("Chapter-one configuration has inconsistent limits.");
        }
        private void ValidateState()
        {
            if (S.version != 1 || S.chapter != 1 || S.day < 1 || S.day > 10000000 || S.gpuCount < 1 || S.caseCount < 1 || S.caseCount > Config.maxCases || S.gpuCount > S.caseCount * Config.gpusPerCase ||
                S.semanticLevel < 1 || S.semanticLevel > Config.maxSkillLevel || S.trainingLevel < 1 || S.trainingLevel > Config.maxTrainingLevel || S.lastSeenUnix < 0 ||
                S.cardsReviewed < 0 || S.cardsReviewed > 1000000000 || S.correctedCards < 0 || S.correctedCards > S.cardsReviewed || S.examAttempts < 0 || S.examAttempts > 1000000000 || S.examAnswered < 0 || S.examAnswered > Config.examQuestions ||
                S.examCorrect < 0 || S.examCorrect > S.examAnswered || S.nextCardId < 1 || S.nextCardId > 1000000000 || S.rngState <= 0 || S.rngState > uint.MaxValue)
                throw new ArgumentException("Save contains invalid version, counters, hardware, or RNG.");
            foreach (var f in typeof(GameState).GetFields())
                if (f.FieldType == typeof(double)) { double v = (double)f.GetValue(S); if (!Finite(v) || v < 0 || v > ResourceCeiling) throw new ArgumentException("Invalid save number: " + f.Name); }
            if (S.daySeconds >= Config.dayLengthSeconds || S.tickRemainder >= Quantum + Epsilon || S.heartbeatAccumulator >= 1 + Epsilon ||
                S.temperature > Config.maximumTemperature || S.examActive && (S.examAnswered >= Config.examQuestions || S.examAttempts == 0 || S.chapterOneComplete) ||
                S.chapterOneComplete != S.examRewardGranted || S.unpaidPower && S.billDue <= Epsilon || !S.unpaidPower && S.billDue > Epsilon ||
                S.examTemplateMask < 0 || (S.examTemplateMask >> EmailTemplates.Count) != 0 || S.appInstallState < 0 || S.appInstallState > 2 || S.prologue < 0 || S.prologue > 2)
                throw new ArgumentException("Save contains inconsistent progress or settlement state.");
            if (S.aiSelf == null) S.aiSelf = ""; if (S.aiCallMe == null) S.aiCallMe = ""; if (S.personality == null) S.personality = "";
            if (S.toneWords == null) S.toneWords = new List<string>();
            if (S.aiSelf.Length > 64 || S.aiCallMe.Length > 64 || S.personality.Length > 200 || S.toneWords.Count > PrologueProfile.MaxWords)
                throw new ArgumentException("Save contains an oversized opening setup.");
            if (S.aiName == null || S.aiName.Length > 64 || S.nodes == null || S.edges == null || S.mistakes == null || S.activeRoute == null || S.activeRouteEdges == null ||
                S.nodes.Count > 35 || S.edges.Count > 210 || S.mistakes.Count > 50 || S.activeRoute.Count > 35 || S.activeRouteEdges.Count > 34)
                throw new ArgumentException("Save collections are missing or oversized.");
            var ids = new Dictionary<int, NodeState>(); var cells = new HashSet<int>(); int inputs = 0, yes = 0, no = 0;
            foreach (var n in S.nodes)
            {
                if (n == null || n.id < 1 || n.id > 1000000 || ids.ContainsKey(n.id) || n.q < 0 || n.q > 6 || n.r < 0 || n.r > 4 || !cells.Add(n.q * 5 + n.r) ||
                    !Enum.IsDefined(typeof(NodeKind), n.kind) || !Finite(n.charge) || n.charge < 0 || n.charge > 10 || !Finite(n.fatigue) || n.fatigue < 0 || n.fatigue > 3 ||
                    n.kind == NodeKind.Hidden && (n.q < 1 || n.q > 5)) throw new ArgumentException("Save contains duplicate, invalid, or out-of-bounds neurons.");
                if (n.kind == NodeKind.Input) { if (n.q != 0) throw new ArgumentException("Invalid input port."); inputs++; }
                if (n.kind == NodeKind.Yes) { if (n.q != 6) throw new ArgumentException("Invalid Yes port."); yes++; }
                if (n.kind == NodeKind.No) { if (n.q != 6) throw new ArgumentException("Invalid No port."); no++; }
                ids.Add(n.id, n);
            }
            if (inputs != 1 || yes != 1 || no != 1 || S.nodes.Count < 3) throw new ArgumentException("Save must preserve input and both output ports.");
            var pairs = new HashSet<long>();
            foreach (var e in S.edges)
                if (e == null || !ids.ContainsKey(e.from) || !ids.ContainsKey(e.to) || !CanLink(ids[e.from], ids[e.to]) || !pairs.Add(((long)e.from << 32) | (uint)e.to) ||
                    !Finite(e.weight) || e.weight < Config.minimumWeight - Epsilon || e.weight > Config.maximumWeight) throw new ArgumentException("Save has invalid, duplicate, or dangling synapses.");
            if (S.nodes.Count * Config.nodeMemory + S.edges.Count * Config.edgeMemory > S.gpuCount * Config.memoryPerGpu + Epsilon) throw new ArgumentException("Saved board exceeds VRAM.");
            foreach (var text in S.mistakes) if (text == null || text.Length > 2000) throw new ArgumentException("Invalid mistake book.");
            var routeIds = new HashSet<int>();
            foreach (int id in S.activeRoute) if (!ids.ContainsKey(id) || !routeIds.Add(id)) throw new ArgumentException("Saved inference route references missing or duplicate neurons.");
            if (S.activeRouteEdges.Count != Math.Max(0, S.activeRoute.Count - 1)) throw new ArgumentException("Saved inference route has inconsistent edge count.");
            for (int i = 0; i < S.activeRouteEdges.Count; i++)
            {
                var e = S.activeRouteEdges[i];
                if (e == null || e.from != S.activeRoute[i] || e.to != S.activeRoute[i + 1] || !pairs.Contains(((long)e.from << 32) | (uint)e.to))
                    throw new ArgumentException("Saved inference route references missing or inconsistent synapses.");
            }
            if (S.activeCard != null && (S.activeCard.id < 1 || S.activeCard.id >= S.nextCardId || string.IsNullOrEmpty(S.activeCard.prompt) || S.activeCard.prompt.Length > 2000 ||
                string.IsNullOrEmpty(S.activeCard.explanation) || S.activeCard.explanation.Length > 2000 || S.activeCard.category == null || S.activeCard.category.Length > 100 ||
                !Finite(S.activeCard.confidence) || S.activeCard.confidence < 0 || S.activeCard.confidence > 1)) throw new ArgumentException("Invalid active card.");
            if (S.examActive && S.activeCard == null) throw new ArgumentException("Active examination is missing its card.");
        }
        private static bool CanLink(NodeState a, NodeState b)
        {
            int dq = a.q - b.q, dr = a.r - b.r;
            return a.id != b.id && a.kind != NodeKind.Yes && a.kind != NodeKind.No && b.kind != NodeKind.Input &&
                Math.Abs(dq) <= 1 && Math.Abs(dr) <= 1 && Math.Abs(dq + dr) <= 1;
        }
        private bool CanSpend(double amount) { return Finite(S.money) && S.money + Epsilon >= amount; }
        private void Raise(string name, string arg = null) { Signal?.Invoke(name, arg); }
        private void TripBreaker()
        {
            if (S.breakerTripped) return;
            S.breakerTripped = true;
            Raise("breaker.tripped");
        }
        private bool Reject(string message) { LastMessage = message; Notify(); return false; }
        private bool Accept(string message) { LastMessage = message; Notify(); return true; }
        private void Notify() { Changed?.Invoke(); }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static double Clamp(double value, double low, double high) { return Math.Max(low, Math.Min(high, value)); }
        private static double AddBounded(double value, double add) { return Math.Min(ResourceCeiling, value + add); }
    }
}
