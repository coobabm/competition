using System;
using System.Collections.Generic;
using NUnit.Framework;
using LingGuangV05.Core.Incremental;

namespace LingGuangV05.Tests
{
    public sealed class IncrementalTests
    {
        private static IncrementalSim Rich(double score = 1e12, double money = 1e12)
        {
            var sim = new IncrementalSim();
            sim.S.score = score; sim.S.money = money;
            return sim;
        }

        [Test] public void FreshStartHasGiftHardwareInputLitAndOnlySpamUnlocked()
        {
            var sim = new IncrementalSim();
            Assert.That(sim.S.score, Is.Zero);
            Assert.That(sim.S.money, Is.EqualTo(sim.C.starterMoney));
            Assert.That(sim.S.hardware[IncrementalCatalog.GpuUsed], Is.EqualTo(1));
            Assert.That(sim.S.hardware[IncrementalCatalog.Case], Is.EqualTo(1));
            Assert.That(sim.IsLit(IncrementalCatalog.InputQ, IncrementalCatalog.InputR), Is.True);
            Assert.That(sim.LitCount, Is.EqualTo(1));
            Assert.That(sim.JobUnlocked(0), Is.True);
            for (int j = 1; j < IncrementalCatalog.Jobs.Length; j++) Assert.That(sim.JobUnlocked(j), Is.False);
            Assert.That(sim.ScorePerSecond, Is.Zero, "no automatic income before the first job level");
        }

        [Test] public void CorrectHandAnswerGivesOnePointWrongGivesNothingAndResetsStreak()
        {
            var sim = new IncrementalSim();
            Assert.That(sim.AnswerManual(true), Is.EqualTo(1).Within(1e-9));
            Assert.That(sim.S.score, Is.EqualTo(1).Within(1e-9));
            Assert.That(sim.AnswerManual(false), Is.Zero);
            Assert.That(sim.S.streak, Is.Zero);
            Assert.That(sim.S.score, Is.EqualTo(1).Within(1e-9));
            Assert.That(sim.S.money, Is.EqualTo(sim.C.starterMoney), "hand answers pay 分 only until 人工经验 is lit");
        }

        [Test] public void StreakRaisesHandAnswerValueUpToCap()
        {
            var sim = new IncrementalSim();
            double last = 0;
            for (int i = 0; i < 40; i++) last = sim.AnswerManual(true);
            Assert.That(last, Is.EqualTo(1 + sim.C.streakBonusPerAnswer * sim.C.streakCap).Within(1e-9));
            Assert.That(sim.S.bestStreak, Is.EqualTo(40));
        }

        [Test] public void TenHandAnswersBuyTheFirstAutomaticWorker()
        {
            var sim = new IncrementalSim();
            Assert.That(sim.LevelJob(0), Is.False);
            for (int i = 0; i < 10; i++) sim.AnswerManual(true);
            Assert.That(sim.LevelJob(0), Is.True);
            Assert.That(sim.S.jobLevels[0], Is.EqualTo(1));
            Assert.That(sim.ScorePerSecond, Is.GreaterThan(0));
            Assert.That(sim.JobCost(0), Is.EqualTo(IncrementalCatalog.Jobs[0].baseCost * IncrementalCatalog.Jobs[0].growth).Within(1e-9));
        }

        [Test] public void RefusedCommandsChangeNothing()
        {
            var sim = new IncrementalSim();
            double score = sim.S.score, money = sim.S.money;
            Assert.That(sim.LevelJob(1), Is.False, "locked job");
            Assert.That(sim.Light(5, 0), Is.False, "not adjacent");
            Assert.That(sim.Light(1, 2), Is.False, "too poor");
            Assert.That(sim.Buy(IncrementalCatalog.GpuFlagship), Is.False, "not revealed");
            Assert.That(sim.Sell(IncrementalCatalog.GpuUsed), Is.False, "last card");
            Assert.That(sim.PayBill(), Is.False);
            Assert.That(sim.S.score, Is.EqualTo(score)); Assert.That(sim.S.money, Is.EqualTo(money));
            Assert.That(sim.LastMessage, Is.Not.Empty);
        }

        [Test] public void AutomaticIncomeIsIndependentOfFramePartitioning()
        {
            var a = Rich(1000, 650); var b = Rich(1000, 650);
            for (int i = 0; i < 5; i++) { a.LevelJob(0); b.LevelJob(0); }
            a.Tick(300);
            for (int i = 0; i < 1200; i++) b.Tick(.25);
            Assert.That(b.S.score, Is.EqualTo(a.S.score).Within(1e-6));
            Assert.That(b.S.money, Is.EqualTo(a.S.money).Within(1e-6));
            Assert.That(b.S.day, Is.EqualTo(a.S.day));
            Assert.That(a.S.autoCorrect, Is.EqualTo(a.S.autoAnswers * a.Accuracy).Within(1e-6));
        }

        [Test] public void ProductionIsLevelsTimesRateTimesComputeTimesSpeedTimesAccuracyTimesValue()
        {
            var sim = Rich();
            for (int i = 0; i < 3; i++) sim.LevelJob(0);
            var d = IncrementalCatalog.Jobs[0];
            double expected = 3 * d.rate * 1 * 1 * sim.Accuracy * d.score;
            Assert.That(sim.ScorePerSecond, Is.EqualTo(expected).Within(1e-9));
            Assert.That(sim.MoneyPerSecond, Is.EqualTo(expected / d.score * d.pay).Within(1e-9));
        }

        [Test] public void JobMilestoneDoublesThatJobSpeed()
        {
            var sim = Rich();
            int m = IncrementalCatalog.Jobs[0].milestones[0];
            for (int i = 0; i < m - 1; i++) sim.LevelJob(0);
            double before = sim.JobSpeed(0);
            sim.LevelJob(0);
            Assert.That(sim.JobSpeed(0), Is.EqualTo(before * m / (m - 1) * 2).Within(1e-9));
            Assert.That(sim.NextMilestone(0), Is.EqualTo(IncrementalCatalog.Jobs[0].milestones[1]));
        }

        [Test] public void TreeLightsOnlyAdjacentCellsAndCostGrows()
        {
            var sim = Rich();
            double first = sim.TreeCost;
            Assert.That(sim.CanLight(1, 2), Is.True);
            Assert.That(sim.CanLight(2, 2), Is.False);
            Assert.That(sim.Light(1, 2), Is.True);
            Assert.That(sim.TreeCost, Is.EqualTo(first * sim.C.treeGrowth).Within(1e-6));
            Assert.That(sim.CanLight(2, 2), Is.True);
            Assert.That(sim.Light(1, 2), Is.False, "already lit");
        }

        [Test] public void MultCellsAddAndPortsMultiply()
        {
            var sim = Rich();
            Assert.That(sim.TreeMultiplier, Is.EqualTo(1));
            sim.Light(0, 1); sim.Light(1, 1); // speed, then 特征组合 +50%
            Assert.That(sim.TreeMultiplier, Is.EqualTo(1.5).Within(1e-9));
            Assert.That(sim.SpeedMultiplier, Is.EqualTo(1.3).Within(1e-9));
            sim.Light(2, 0);                  // another +50%
            Assert.That(sim.TreeMultiplier, Is.EqualTo(2).Within(1e-9), "additive, not 1.5 × 1.5");
        }

        [Test] public void JobKeystoneUnlocksJob()
        {
            var sim = Rich();
            sim.Light(1, 2); Assert.That(sim.Light(2, 1), Is.True);
            Assert.That(sim.JobUnlocked(1), Is.True);
            Assert.That(sim.LevelJob(1), Is.True);
        }

        [Test] public void HardJobsUseLowerAccuracy()
        {
            var sim = new IncrementalSim();
            Assert.That(sim.JobAccuracy(4), Is.EqualTo(sim.Accuracy - IncrementalCatalog.Jobs[4].difficulty).Within(1e-9));
            Assert.That(sim.JobAccuracy(0), Is.EqualTo(sim.Accuracy));
        }

        [Test] public void EveryCellIsReachableAndBothPortsAndAllJobsExist()
        {
            var seen = new HashSet<int>(); var queue = new Queue<SkillCell>();
            var input = IncrementalCatalog.Get(IncrementalCatalog.InputQ, IncrementalCatalog.InputR);
            Assert.That(input.effect, Is.EqualTo(SkillEffect.Input));
            queue.Enqueue(input); seen.Add(input.Index);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var n in IncrementalCatalog.Cells)
                    if (IncrementalCatalog.Adjacent(c.q, c.r, n.q, n.r) && seen.Add(n.Index)) queue.Enqueue(n);
            }
            Assert.That(seen.Count, Is.EqualTo(IncrementalCatalog.Columns * IncrementalCatalog.Rows));
            int yes = 0, no = 0; var jobs = new HashSet<int>();
            foreach (var c in IncrementalCatalog.Cells)
            {
                if (c.effect == SkillEffect.OutputYes) yes++;
                if (c.effect == SkillEffect.OutputNo) no++;
                if (c.effect == SkillEffect.Job) jobs.Add((int)c.value);
                Assert.That(c.name, Is.Not.Empty); Assert.That(c.description, Is.Not.Empty);
            }
            Assert.That(yes, Is.EqualTo(1)); Assert.That(no, Is.EqualTo(1));
            for (int j = 1; j < IncrementalCatalog.Jobs.Length; j++) Assert.That(jobs.Contains(j), "job " + j + " has a keystone");
        }

        [Test] public void ExamNeedsBothPortsAndAccuracy()
        {
            var sim = Rich();
            Assert.That(sim.CanTakeExam, Is.False);
            // shortest route along the middle row to Yes, then No
            // middle row, then up through a speed cell to No, then Yes — no accuracy cells on the way
            foreach (var p in new[] { (1, 2), (2, 2), (3, 2), (4, 2), (5, 1), (6, 1), (6, 2) }) Assert.That(sim.Light(p.Item1, p.Item2), Is.True, p.ToString());
            Assert.That(sim.HasYes && sim.HasNo, Is.True);
            Assert.That(sim.Accuracy, Is.LessThan(sim.C.examRequiredAccuracy), "ports alone are not enough");
            Assert.That(sim.CanTakeExam, Is.False);
            Assert.That(IncrementalCatalog.Get(5, 2).effect, Is.EqualTo(SkillEffect.Accuracy));
            Assert.That(sim.Light(5, 2), Is.True);
            Assert.That(sim.Accuracy, Is.GreaterThanOrEqualTo(sim.C.examRequiredAccuracy - 1e-9));
            Assert.That(sim.CanTakeExam, Is.True);
            Assert.That(sim.GrantExamPass(), Is.True);
            Assert.That(sim.GrantExamPass(), Is.False, "one-time reward");
            Assert.That(sim.CanTakeExam, Is.False);
        }

        [Test] public void GpuNeedsSlotAndPowerAndPriceGrows()
        {
            var sim = Rich(0, 1e9);
            double p1 = sim.HardwarePrice(IncrementalCatalog.GpuUsed);
            Assert.That(p1, Is.EqualTo(IncrementalCatalog.Hardware[0].basePrice), "the gift card is free of the price curve");
            Assert.That(sim.Buy(IncrementalCatalog.GpuUsed), Is.True);
            Assert.That(sim.HardwarePrice(IncrementalCatalog.GpuUsed), Is.EqualTo(p1 * IncrementalCatalog.Hardware[0].growth).Within(1e-6));
            Assert.That(sim.Buy(IncrementalCatalog.GpuUsed), Is.False, "two slots used");
            Assert.That(sim.Buy(IncrementalCatalog.Case), Is.True);
            Assert.That(sim.Buy(IncrementalCatalog.GpuUsed), Is.True);
            // fill power: base 200 + 3×60 + 2×45 = 470 of 500 → the next 60W card exceeds
            Assert.That(sim.Buy(IncrementalCatalog.GpuUsed), Is.False);
            Assert.That(sim.LastMessage, Does.Contain("家庭"));
            Assert.That(sim.UpgradePower(), Is.True);
            Assert.That(sim.Buy(IncrementalCatalog.GpuUsed), Is.True);
            Assert.That(sim.Compute, Is.EqualTo(4)); // four used 750Ti cards
        }

        [Test] public void FlagshipCardAppearsAfterLifetimeEarnings()
        {
            var sim = Rich(0, 1e9);
            Assert.That(sim.HardwareRevealed(IncrementalCatalog.GpuFlagship), Is.False);
            sim.S.totalMoney = IncrementalCatalog.Hardware[IncrementalCatalog.GpuFlagship].revealAtTotalMoney;
            Assert.That(sim.HardwareRevealed(IncrementalCatalog.GpuFlagship), Is.True);
        }

        [Test] public void UnpaidBillStopsWorkAndHandAnswersRepayIt()
        {
            var sim = new IncrementalSim(null, new IncrementalConfig { electricityPrice = 10 });
            for (int i = 0; i < 10; i++) sim.AnswerManual(true);
            sim.LevelJob(0);
            sim.S.money = 0;
            sim.Tick(sim.C.dayLengthSeconds);
            Assert.That(sim.S.unpaidPower, Is.True);
            Assert.That(sim.ScorePerSecond, Is.Zero);
            double due = sim.S.billDue;
            double score = sim.S.score;
            sim.Tick(10);
            Assert.That(sim.S.score, Is.EqualTo(score), "no automatic income while unpaid");
            int answers = (int)Math.Ceiling(due / sim.C.outageRepaymentPerAnswer);
            for (int i = 0; i < answers; i++) sim.AnswerManual(true);
            Assert.That(sim.S.unpaidPower, Is.False);
            Assert.That(sim.S.billDue, Is.Zero);
        }

        [Test] public void BillIsPaidAutomaticallyWhenAffordable()
        {
            var sim = new IncrementalSim();
            double money = sim.S.money;
            sim.Tick(sim.C.dayLengthSeconds);
            double expected = sim.Watts / 1000 * 24 * sim.C.electricityPrice;
            Assert.That(sim.S.money, Is.EqualTo(money - expected).Within(1e-6));
            Assert.That(sim.S.day, Is.EqualTo(2));
        }

        [Test] public void SparkTriplesOutputThenCoolsDown()
        {
            var sim = Rich();
            sim.LevelJob(0);
            double rate = sim.ScorePerSecond;
            Assert.That(sim.Spark(), Is.True);
            Assert.That(sim.ScorePerSecond, Is.EqualTo(rate * sim.C.sparkFactor).Within(1e-9));
            Assert.That(sim.Spark(), Is.False);
            sim.Tick(sim.C.sparkDuration + 1);
            Assert.That(sim.ScorePerSecond, Is.EqualTo(rate).Within(1e-9));
            sim.Tick(sim.C.sparkCooldown);
            Assert.That(sim.SparkReady, Is.True);
        }

        [Test] public void ManualExperiencePaysSecondsOfAutomaticOutput()
        {
            var sim = Rich(1e6, 0);
            for (int i = 0; i < 5; i++) sim.LevelJob(0);
            sim.Light(1, 2); sim.Light(2, 2); // (2,2) is 人工经验
            Assert.That(IncrementalCatalog.Get(2, 2).effect, Is.EqualTo(SkillEffect.Manual));
            double sps = sim.ScorePerSecond, mps = sim.MoneyPerSecond;
            double streak = sim.StreakMultiplier;
            double gain = sim.AnswerManual(true);
            Assert.That(gain, Is.EqualTo((1 + .5 * sps) * streak).Within(1e-9));
            Assert.That(sim.S.money, Is.EqualTo(.5 * mps * streak).Within(1e-9));
        }

        [Test] public void NextGoalPointsAtCheapestPurchaseWithEta()
        {
            var sim = new IncrementalSim();
            var goal = sim.NextGoal();
            Assert.That(goal, Is.Not.Null);
            Assert.That(goal.cost, Is.GreaterThan(0));
            for (int i = 0; i < 10; i++) sim.AnswerManual(true);
            sim.LevelJob(0);
            sim.S.money = 0;
            goal = sim.NextGoal();
            Assert.That(goal.seconds, Is.GreaterThan(0).And.LessThan(double.PositiveInfinity));
        }

        [Test] public void PartialOrOldSaveIsRepaired()
        {
            var state = new IncrementalState { litCells = null, jobLevels = new List<int> { 3 }, hardware = null, jobCorrect = null };
            var sim = new IncrementalSim(state);
            Assert.That(sim.S.jobLevels.Count, Is.EqualTo(IncrementalCatalog.Jobs.Length));
            Assert.That(sim.S.jobLevels[0], Is.EqualTo(3));
            Assert.That(sim.S.hardware[IncrementalCatalog.GpuUsed], Is.EqualTo(1));
            Assert.That(sim.IsLit(IncrementalCatalog.InputQ, IncrementalCatalog.InputR), Is.True);
            var bad = new IncrementalState { score = double.NaN };
            Assert.Throws<ArgumentException>(() => new IncrementalSim(bad));
        }

        [Test] public void StateSurvivesRebuildFromSameData()
        {
            var a = Rich(5000, 5000);
            a.LevelJob(0); a.Light(1, 2); a.Buy(IncrementalCatalog.GpuUsed); a.Tick(37);
            var b = new IncrementalSim(a.S);
            Assert.That(b.ScorePerSecond, Is.EqualTo(a.ScorePerSecond).Within(1e-9));
            Assert.That(b.LitCount, Is.EqualTo(a.LitCount));
        }

        [Test] public void ChineseNumberFormatting()
        {
            Assert.That(Fmt.Num(0), Is.EqualTo("0"));
            Assert.That(Fmt.Num(2.5), Is.EqualTo("2.5"));
            Assert.That(Fmt.Num(9999), Is.EqualTo("9999"));
            Assert.That(Fmt.Num(12345), Is.EqualTo("1.23万"));
            Assert.That(Fmt.Num(3.2e8), Is.EqualTo("3.2亿"));
            Assert.That(Fmt.Num(5e12), Is.EqualTo("5万亿"));
            Assert.That(Fmt.Num(2e17), Is.EqualTo("2.00e17"));
            Assert.That(Fmt.Money(3.5), Is.EqualTo("3.50"));
            Assert.That(Fmt.Duration(75), Is.EqualTo("1 分 15 秒"));
            Assert.That(Fmt.Duration(double.PositiveInfinity), Is.EqualTo("暂时无法达到"));
        }
    }
}
