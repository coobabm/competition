using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Incremental
{
    /// <summary>
    /// Incremental economy for 灵光. Two currencies:
    ///   分 (score)  — earned by every correct answer; spent on job levels and the neural skill tree.
    ///   ¥  (money)  — paid by clients for jobs; spent on hardware, household power and electricity.
    /// Manual answers give 分 only. Jobs answer automatically: speed = levels × rate × compute × tree speed.
    /// Deterministic: auto answers use expected values and fixed 0.25 s quanta, so frame partitioning and
    /// offline catch-up give identical results. Commands return false (with LastMessage) and change nothing when refused.
    /// </summary>
    public sealed class IncrementalSim
    {
        private const double Quantum = .25, Epsilon = 1e-9, Ceiling = 1e300;
        private readonly HashSet<int> lit = new HashSet<int>();

        public IncrementalSim(IncrementalState state = null, IncrementalConfig config = null)
        {
            C = config ?? new IncrementalConfig();
            S = state ?? Fresh(C);
            Repair();
            foreach (int i in S.litCells) lit.Add(i);
            LastMessage = "";
        }

        public IncrementalState S { get; private set; }
        public IncrementalConfig C { get; private set; }
        public string LastMessage { get; private set; }
        public event Action Changed;
        /// <summary>(name, argument) for the story layer, e.g. ("tree.lit", "3,2"), ("job.leveled", "spam").</summary>
        public event Action<string, string> Signal;

        // ---------- derived numbers ----------

        public bool IsLit(int q, int r) { var c = IncrementalCatalog.Get(q, r); return c != null && lit.Contains(c.Index); }
        public int LitCount { get { return lit.Count; } }
        public bool HasYes { get { return CountLit(SkillEffect.OutputYes) > 0; } }
        public bool HasNo { get { return CountLit(SkillEffect.OutputNo) > 0; } }

        /// <summary>1 + Σ(Mult cells), × each lit output port, × exam pass. Additive inside the tree keeps cost growth ahead of production.</summary>
        public double TreeMultiplier
        {
            get
            {
                double m = 1 + Sum(SkillEffect.Mult);
                foreach (int i in lit)
                {
                    var c = IncrementalCatalog.Cells[i];
                    if (c.effect == SkillEffect.OutputYes || c.effect == SkillEffect.OutputNo) m *= c.value;
                }
                return m * (S.examPassed ? C.examPassMultiplier : 1);
            }
        }
        public double SpeedMultiplier { get { return 1 + Sum(SkillEffect.Speed); } }
        public double Accuracy
        {
            get { return Math.Min(C.maxAccuracy, C.baseAccuracy + Sum(SkillEffect.Accuracy) + .05 * (CountLit(SkillEffect.OutputYes) + CountLit(SkillEffect.OutputNo))); }
        }
        /// <summary>Seconds of automatic output a correct hand answer also pays (人工经验 cells). Keeps active play relevant late.</summary>
        public double ManualSeconds { get { return Sum(SkillEffect.Manual); } }
        /// <summary>分 for one correct hand answer before streak and boost: 1 + ManualSeconds × unboosted 分/秒.</summary>
        public double ManualValue { get { return C.manualBase + ManualSeconds * ScorePerSecond / BoostFactor; } }
        /// <summary>Multiplier the next correct hand answer gets from the current streak.</summary>
        public double StreakMultiplier { get { return 1 + C.streakBonusPerAnswer * Math.Min(S.streak, C.streakCap); } }
        public bool Boosted { get { return S.gameSeconds < S.boostUntil; } }
        public double BoostFactor { get { return Boosted ? C.sparkFactor : 1; } }
        public bool SparkReady { get { return S.gameSeconds + Epsilon >= S.sparkReadyAt; } }
        public double SparkCooldownLeft { get { return Math.Max(0, S.sparkReadyAt - S.gameSeconds); } }

        public double Compute
        {
            get
            {
                double c = 0;
                for (int i = 0; i < IncrementalCatalog.Hardware.Length; i++) c += S.hardware[i] * IncrementalCatalog.Hardware[i].compute;
                return c;
            }
        }
        public int GpuSlots { get { return S.hardware[IncrementalCatalog.Case] * IncrementalCatalog.Hardware[IncrementalCatalog.Case].slots; } }
        public int GpusInstalled
        {
            get
            {
                int n = 0;
                for (int i = 0; i < IncrementalCatalog.Hardware.Length; i++) if (IncrementalCatalog.Hardware[i].isGpu) n += S.hardware[i];
                return n;
            }
        }
        public double WattFactor { get { double f = 1; foreach (int i in lit) if (IncrementalCatalog.Cells[i].effect == SkillEffect.Efficiency) f *= IncrementalCatalog.Cells[i].value; return f; } }
        public double DeviceWatts
        {
            get
            {
                double w = 0;
                for (int i = 0; i < IncrementalCatalog.Hardware.Length; i++)
                {
                    var h = IncrementalCatalog.Hardware[i];
                    w += S.hardware[i] * h.watts * (h.isGpu ? WattFactor : 1);
                }
                return w;
            }
        }
        public double Watts { get { return C.basePowerWatts + DeviceWatts; } }
        public double PowerCapacity { get { return C.startPowerWatts + S.powerLevel * C.powerStepWatts; } }
        public bool Powered { get { return !S.unpaidPower; } }

        public bool JobUnlocked(int job)
        {
            if (job == 0) return true;
            foreach (int i in lit) { var c = IncrementalCatalog.Cells[i]; if (c.effect == SkillEffect.Job && (int)c.value == job) return true; }
            return false;
        }
        public double JobAccuracy(int job) { return Math.Max(C.minJobAccuracy, Accuracy - IncrementalCatalog.Jobs[job].difficulty); }
        public int MilestonesReached(int job)
        {
            int n = 0; foreach (int m in IncrementalCatalog.Jobs[job].milestones) if (S.jobLevels[job] >= m) n++;
            return n;
        }
        public int NextMilestone(int job)
        {
            foreach (int m in IncrementalCatalog.Jobs[job].milestones) if (S.jobLevels[job] < m) return m;
            return 0;
        }
        /// <summary>Questions answered per second by this job (before accuracy).</summary>
        public double JobSpeed(int job)
        {
            if (!Powered) return 0;
            var d = IncrementalCatalog.Jobs[job];
            return S.jobLevels[job] * d.rate * Compute * SpeedMultiplier * Math.Pow(2, MilestonesReached(job)) * BoostFactor;
        }
        public double JobScorePerSecond(int job) { return JobSpeed(job) * JobAccuracy(job) * IncrementalCatalog.Jobs[job].score * TreeMultiplier; }
        public double JobMoneyPerSecond(int job) { return JobSpeed(job) * JobAccuracy(job) * IncrementalCatalog.Jobs[job].pay * TreeMultiplier; }
        public double ScorePerSecond { get { double t = 0; for (int j = 0; j < IncrementalCatalog.Jobs.Length; j++) t += JobScorePerSecond(j); return t; } }
        public double MoneyPerSecond { get { double t = 0; for (int j = 0; j < IncrementalCatalog.Jobs.Length; j++) t += JobMoneyPerSecond(j); return t; } }
        public double AnswersPerSecond { get { double t = 0; for (int j = 0; j < IncrementalCatalog.Jobs.Length; j++) t += JobSpeed(j); return t; } }
        /// <summary>Electricity cost per second at current load, for net-income display.</summary>
        public double PowerCostPerSecond { get { return Watts / 1000 * 24 / C.dayLengthSeconds * C.electricityPrice; } }

        public double JobCost(int job)
        {
            var d = IncrementalCatalog.Jobs[job];
            return d.baseCost * Math.Pow(d.growth, S.jobLevels[job]);
        }
        public double TreeCost { get { return C.treeBaseCost * Math.Pow(C.treeGrowth, lit.Count - 1); } }
        public double HardwarePrice(int item)
        {
            var h = IncrementalCatalog.Hardware[item];
            int owned = S.hardware[item];
            // The gift card and gift case are not counted against the price curve.
            if (item == IncrementalCatalog.GpuUsed || item == IncrementalCatalog.Case) owned = Math.Max(0, owned - 1);
            return h.basePrice * Math.Pow(h.growth, owned);
        }
        public bool HardwareRevealed(int item) { return S.totalMoney + Epsilon >= IncrementalCatalog.Hardware[item].revealAtTotalMoney; }
        public double PowerUpgradeCost { get { return C.powerUpgradeBase * Math.Pow(C.powerUpgradeGrowth, S.powerLevel); } }

        public bool CanLight(int q, int r)
        {
            var c = IncrementalCatalog.Get(q, r);
            if (c == null || lit.Contains(c.Index)) return false;
            foreach (int i in lit) { var o = IncrementalCatalog.Cells[i]; if (IncrementalCatalog.Adjacent(o.q, o.r, q, r)) return true; }
            return false;
        }
        public bool CanTakeExam { get { return HasYes && HasNo && Accuracy + Epsilon >= C.examRequiredAccuracy && !S.examPassed; } }

        // ---------- commands ----------

        /// <summary>Player answered one question by hand. Correct: +分 (with streak). Wrong: streak resets.</summary>
        public double AnswerManual(bool correct)
        {
            S.manualAnswers++;
            if (!correct)
            {
                S.streak = 0; LastMessage = "答错了，连击中断。"; Notify(); return 0;
            }
            S.manualCorrect++;
            double streak = StreakMultiplier; // bonus from the answers before this one: the first answer is exactly 1 分
            S.streak++; S.bestStreak = Math.Max(S.bestStreak, S.streak);
            double gain = ManualValue * streak * BoostFactor;
            double cash = ManualSeconds * MoneyPerSecond / BoostFactor * streak * BoostFactor;
            AddScore(gain);
            if (cash > 0) AddMoney(cash, true);
            string extra = "";
            if (S.unpaidPower && S.billDue > 0)
            {
                double repaid = Math.Min(S.billDue, C.outageRepaymentPerAnswer);
                S.billDue -= repaid;
                if (S.billDue <= Epsilon) { S.billDue = 0; S.unpaidPower = false; Raise("bill.paid", "manual"); }
                extra = S.unpaidPower ? "，抵扣电费 ¥" + Fmt.Money(repaid) : "，欠费已还清，恢复供电";
            }
            LastMessage = "+" + Fmt.Num(gain) + " 分" + (cash > 0 ? " +¥" + Fmt.Money(cash) : "") + (S.streak >= 5 ? "（连击 " + S.streak + "）" : "") + extra;
            Raise("manual.correct", S.streak.ToString());
            Notify();
            return gain;
        }

        public bool LevelJob(int job)
        {
            if (job < 0 || job >= IncrementalCatalog.Jobs.Length) return Reject("没有这个活。");
            var d = IncrementalCatalog.Jobs[job];
            if (!JobUnlocked(job)) return Reject("「" + d.name + "」还没解锁：在神经网络里点亮对应技能。");
            double cost = JobCost(job);
            if (S.score + Epsilon < cost) return Reject("还差 " + Fmt.Num(cost - S.score) + " 分。");
            S.score -= cost;
            int before = MilestonesReached(job);
            S.jobLevels[job]++;
            bool milestone = MilestonesReached(job) > before;
            Raise("job.leveled", d.id);
            if (milestone) Raise("job.milestone", d.id);
            return Accept("「" + d.name + "」接单 Lv" + S.jobLevels[job] + (milestone ? "，达到里程碑：速度 ×2！" : "。"));
        }

        public bool Light(int q, int r)
        {
            var c = IncrementalCatalog.Get(q, r);
            if (c == null) return Reject("网络外没有神经元。");
            if (lit.Contains(c.Index)) return Reject("这个神经元已经点亮。");
            if (!CanLight(q, r)) return Reject("只能点亮和已亮神经元相邻的格子。");
            double cost = TreeCost;
            if (S.score + Epsilon < cost) return Reject("点亮需要 " + Fmt.Num(cost) + " 分，还差 " + Fmt.Num(cost - S.score) + "。");
            S.score -= cost;
            lit.Add(c.Index); S.litCells.Add(c.Index);
            Raise("tree.lit", q + "," + r);
            if (c.effect == SkillEffect.Job) Raise("job.unlocked", IncrementalCatalog.Jobs[(int)c.value].id);
            if ((c.effect == SkillEffect.OutputYes || c.effect == SkillEffect.OutputNo) && HasYes && HasNo) Raise("tree.outputs");
            return Accept("点亮「" + c.name + "」：" + c.description);
        }

        public bool Buy(int item)
        {
            if (item < 0 || item >= IncrementalCatalog.Hardware.Length) return Reject("没有这件商品。");
            var h = IncrementalCatalog.Hardware[item];
            if (!HardwareRevealed(item)) return Reject("这件商品还没上架。");
            double price = HardwarePrice(item);
            if (S.money + Epsilon < price) return Reject("余额不足：需要 ¥" + Fmt.Money(price) + "。");
            if (h.isGpu && GpusInstalled >= GpuSlots) return Reject("机箱插槽满了，先买一个扩展机箱。");
            double extraWatts = h.watts * (h.isGpu ? WattFactor : 1);
            if (Watts + extraWatts > PowerCapacity + Epsilon)
                return Reject("家里电路带不动：再加 " + Fmt.Num(extraWatts) + "W 会超过 " + Fmt.Num(PowerCapacity) + "W。去「家庭」扩容。");
            S.money -= price; S.hardware[item]++;
            Raise("hardware.bought", h.id);
            return Accept("已购入" + h.name + "。");
        }

        public bool Sell(int item)
        {
            if (item < 0 || item >= IncrementalCatalog.Hardware.Length || !IncrementalCatalog.Hardware[item].isGpu) return Reject("只能出售显卡。");
            if (S.hardware[item] <= 0 || GpusInstalled <= 1) return Reject("至少保留一张显卡。");
            S.hardware[item]--;
            double refund = HardwarePrice(item) * C.gpuResaleFraction;
            AddMoney(refund, false);
            Raise("hardware.sold", IncrementalCatalog.Hardware[item].id);
            return Accept("卖掉一张" + IncrementalCatalog.Hardware[item].name + "，回收 ¥" + Fmt.Money(refund) + "。");
        }

        public bool UpgradePower()
        {
            double cost = PowerUpgradeCost;
            if (S.money + Epsilon < cost) return Reject("电路扩容需要 ¥" + Fmt.Money(cost) + "。");
            S.money -= cost; S.powerLevel++;
            Raise("power.upgraded", S.powerLevel.ToString());
            return Accept("电工来过了：家里电路上限 " + Fmt.Num(PowerCapacity) + "W。");
        }

        public bool PayBill()
        {
            if (S.billDue <= Epsilon) return Reject("没有欠费。");
            if (S.money + Epsilon < S.billDue) return Reject("余额不够交电费：卖张显卡，或亲手答题抵扣。");
            S.money -= S.billDue; S.billDue = 0; S.unpaidPower = false;
            Raise("bill.paid", "money");
            return Accept("电费已交清，恢复供电。");
        }

        /// <summary>灵光一闪: active skill. All production ×sparkFactor for sparkDuration seconds, then cooldown.</summary>
        public bool Spark()
        {
            if (!SparkReady) return Reject("灵光还在积蓄：" + Math.Ceiling(SparkCooldownLeft) + " 秒。");
            S.boostUntil = S.gameSeconds + C.sparkDuration;
            S.sparkReadyAt = S.gameSeconds + C.sparkCooldown;
            Raise("spark");
            return Accept("灵光一闪！" + C.sparkDuration + " 秒内所有收益 ×" + C.sparkFactor + "。");
        }

        /// <summary>Called by the chapter when the admission exam is passed. One-time reward and permanent multiplier.</summary>
        public bool GrantExamPass()
        {
            if (S.examPassed) return false;
            S.examPassed = true; AddMoney(C.examReward, false);
            Raise("exam.reward");
            return Accept("录取！所有收益永久 ×" + C.examPassMultiplier + "，奖励 ¥" + Fmt.Money(C.examReward) + "。");
        }

        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            S.tickRemainder += Math.Min(seconds, C.maxTickSeconds);
            bool advanced = false;
            while (S.tickRemainder + Epsilon >= Quantum)
            {
                S.tickRemainder = Math.Max(0, S.tickRemainder - Quantum);
                double remaining = Quantum;
                while (remaining > Epsilon)
                {
                    double dt = Math.Min(remaining, C.dayLengthSeconds - S.daySeconds);
                    if (dt <= Epsilon) { SettleDay(); continue; }
                    Advance(dt); remaining -= dt;
                    if (S.daySeconds + Epsilon >= C.dayLengthSeconds) SettleDay();
                }
                advanced = true;
            }
            if (advanced) Notify();
        }

        /// <summary>The cheapest useful purchase and how long until it is affordable at the current rate.</summary>
        public Goal NextGoal()
        {
            Goal best = null;
            for (int j = 0; j < IncrementalCatalog.Jobs.Length; j++)
                if (JobUnlocked(j)) best = Better(best, new Goal("接单：" + IncrementalCatalog.Jobs[j].name, JobCost(j), false));
            bool frontier = false;
            for (int q = 0; q < IncrementalCatalog.Columns && !frontier; q++) for (int r = 0; r < IncrementalCatalog.Rows; r++) if (CanLight(q, r)) { frontier = true; break; }
            if (frontier) best = Better(best, new Goal("点亮一个神经元", TreeCost, false));
            Goal money = null;
            if (GpusInstalled < GpuSlots) money = Better(money, new Goal("买显卡", HardwarePrice(IncrementalCatalog.GpuUsed), true));
            else money = Better(money, new Goal("买机箱", HardwarePrice(IncrementalCatalog.Case), true));
            if (best != null) best.seconds = Eta(best.cost, S.score, ScorePerSecond);
            if (money != null) money.seconds = Eta(money.cost, S.money, MoneyPerSecond - PowerCostPerSecond);
            if (best == null) return money;
            if (money == null) return best;
            return money.seconds < best.seconds ? money : best;
        }

        public sealed class Goal
        {
            public string label; public double cost, seconds; public bool money;
            public Goal(string label, double cost, bool money) { this.label = label; this.cost = cost; this.money = money; }
        }

        // ---------- internals ----------

        private void Advance(double dt)
        {
            S.gameSeconds += dt; S.daySeconds += dt;
            S.energyKwh += Watts / 1000 * 24 * dt / C.dayLengthSeconds;
            if (!Powered) return;
            double score = 0, money = 0;
            for (int j = 0; j < IncrementalCatalog.Jobs.Length; j++)
            {
                double answers = JobSpeed(j) * dt;
                if (answers <= 0) continue;
                double correct = answers * JobAccuracy(j);
                S.autoAnswers = Bound(S.autoAnswers + answers); S.autoCorrect = Bound(S.autoCorrect + correct);
                S.jobCorrect[j] = Bound(S.jobCorrect[j] + correct);
                score += correct * IncrementalCatalog.Jobs[j].score * TreeMultiplier;
                money += correct * IncrementalCatalog.Jobs[j].pay * TreeMultiplier;
            }
            AddScore(score); AddMoney(money, true);
        }

        private void SettleDay()
        {
            double bill = S.energyKwh * C.electricityPrice;
            S.energyKwh = 0; S.daySeconds = 0; S.day++;
            S.billDue = Bound(S.billDue + bill);
            if (S.billDue > Epsilon && S.money + Epsilon >= S.billDue)
            {
                S.money -= S.billDue; S.billDue = 0; S.unpaidPower = false;
            }
            else if (S.billDue > Epsilon && !S.unpaidPower)
            {
                S.unpaidPower = true;
                LastMessage = "交不起电费，停电了！欠 ¥" + Fmt.Money(S.billDue) + "。亲手答题可以抵扣，或卖显卡。";
                Raise("power.unpaid");
            }
            Raise("day.ended", S.day.ToString());
        }

        private void AddScore(double v) { S.score = Bound(S.score + v); S.totalScore = Bound(S.totalScore + v); }
        private void AddMoney(double v, bool earned) { S.money = Bound(S.money + v); if (earned) S.totalMoney = Bound(S.totalMoney + v); }
        private static double Bound(double v) { return Math.Min(Ceiling, v); }

        private int CountLit(SkillEffect effect) { int n = 0; foreach (int i in lit) if (IncrementalCatalog.Cells[i].effect == effect) n++; return n; }
        private double Sum(SkillEffect effect) { double t = 0; foreach (int i in lit) if (IncrementalCatalog.Cells[i].effect == effect) t += IncrementalCatalog.Cells[i].value; return t; }

        private static Goal Better(Goal a, Goal b) { return a == null || b.cost < a.cost ? b : a; }
        private static double Eta(double cost, double have, double rate)
        {
            if (have + Epsilon >= cost) return 0;
            return rate > Epsilon ? (cost - have) / rate : double.PositiveInfinity;
        }

        private static IncrementalState Fresh(IncrementalConfig c)
        {
            var s = new IncrementalState { money = c.starterMoney };
            s.litCells.Add(IncrementalCatalog.InputQ * IncrementalCatalog.Rows + IncrementalCatalog.InputR);
            s.sparkReadyAt = 0;
            return s;
        }

        /// <summary>Accepts older or partial saves: pads lists, removes unknown cells, keeps the input lit, rejects non-finite numbers.</summary>
        private void Repair()
        {
            if (S.litCells == null) S.litCells = new List<int>();
            if (S.jobLevels == null) S.jobLevels = new List<int>();
            if (S.jobCorrect == null) S.jobCorrect = new List<double>();
            if (S.hardware == null) S.hardware = new List<int>();
            while (S.jobLevels.Count < IncrementalCatalog.Jobs.Length) S.jobLevels.Add(0);
            while (S.jobCorrect.Count < IncrementalCatalog.Jobs.Length) S.jobCorrect.Add(0);
            while (S.hardware.Count < IncrementalCatalog.Hardware.Length) S.hardware.Add(0);
            if (S.hardware[IncrementalCatalog.GpuUsed] < 1 && S.hardware[IncrementalCatalog.GpuFlagship] < 1) S.hardware[IncrementalCatalog.GpuUsed] = 1;
            if (S.hardware[IncrementalCatalog.Case] < 1) S.hardware[IncrementalCatalog.Case] = 1;
            for (int i = 0; i < S.jobLevels.Count; i++) if (S.jobLevels[i] < 0) S.jobLevels[i] = 0;
            for (int i = 0; i < S.hardware.Count; i++) if (S.hardware[i] < 0) S.hardware[i] = 0;
            var seen = new HashSet<int>();
            S.litCells.RemoveAll(i => i < 0 || i >= IncrementalCatalog.Cells.Length || !seen.Add(i));
            int input = IncrementalCatalog.InputQ * IncrementalCatalog.Rows + IncrementalCatalog.InputR;
            if (!seen.Contains(input)) S.litCells.Insert(0, input);
            if (S.powerLevel < 0) S.powerLevel = 0;
            if (S.day < 1) S.day = 1;
            foreach (double v in new[] { S.score, S.money, S.totalScore, S.totalMoney, S.gameSeconds, S.daySeconds, S.energyKwh, S.billDue, S.autoAnswers, S.autoCorrect, S.sparkReadyAt, S.boostUntil, S.tickRemainder })
                if (double.IsNaN(v) || double.IsInfinity(v) || v < 0) throw new ArgumentException("Incremental save contains an invalid number.");
            if (S.daySeconds >= C.dayLengthSeconds) S.daySeconds = 0;
            if (S.unpaidPower && S.billDue <= Epsilon) S.unpaidPower = false;
        }

        private void Raise(string name, string arg = null) { Signal?.Invoke(name, arg); }
        private bool Reject(string message) { LastMessage = message; Notify(); return false; }
        private bool Accept(string message) { LastMessage = message; Notify(); return true; }
        private void Notify() { Changed?.Invoke(); }
    }
}
