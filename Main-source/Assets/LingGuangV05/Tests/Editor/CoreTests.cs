using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LingGuangV05.Core;

namespace LingGuangV05.Tests
{
    public sealed class CoreTests
    {
        [Test] public void FreshWalletStartsAtZeroButKeepsStarterHardware()
        {
            Assert.That(new GameConfig().starterMoney, Is.Zero);
            Assert.That(new GameState().money, Is.Zero);
            var sim = new ChapterOneSim();
            Assert.That(sim.S.money, Is.Zero);
            Assert.That(sim.S.gpuCount, Is.EqualTo(1));
            Assert.That(sim.S.caseCount, Is.EqualTo(1));
        }

        [Test] public void LoadingExistingBalanceDoesNotResetItToTheNewStarterAmount()
        {
            var state = new ChapterOneSim().S;
            state.money = 123.45;
            Assert.That(new ChapterOneSim(state).S.money, Is.EqualTo(123.45));
        }

        [Test] public void FreshStateHasGiftGpuGrantAndBothDirectedOutputPaths()
        {
            var sim = new ChapterOneSim();
            Assert.That(sim.S.gpuCount, Is.EqualTo(1));
            Assert.That(sim.S.money, Is.EqualTo(sim.Config.starterMoney));
            Assert.That(sim.HasOutputPath, Is.True);
            Assert.That(sim.MemoryUsed, Is.LessThanOrEqualTo(sim.MemoryCapacity));
            Assert.That(sim.CurrentCard.prompt, Is.Not.Empty);
            Assert.That(sim.S.chapter, Is.EqualTo(1));
        }
        [Test] public void NewGameStartsWithoutLingGuangAndDownloadInstallsItOnce()
        {
            var sim = new ChapterOneSim(); var signals = new List<string>(); sim.Signal += (n, a) => signals.Add(n);
            Assert.That(sim.AppInstalled, Is.False);
            Assert.That(sim.InstallApp(), Is.True);
            Assert.That(sim.AppInstalled, Is.True);
            Assert.That(sim.InstallApp(), Is.False);
            Assert.That(signals.Count(n => n == "app.installed"), Is.EqualTo(1));
            var legacy = new GameState(); legacy.nodes = new ChapterOneSim().S.nodes; legacy.edges = new ChapterOneSim().S.edges;
            legacy.appInstallState = 0;
            Assert.That(new ChapterOneSim(legacy).AppInstalled, Is.True, "saves from before the download flow keep the app");
            var bad = new ChapterOneSim().S; bad.appInstallState = 3;
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(bad));
        }
        [Test] public void WrongLabelGivesNoSamplesOrLearningPointsButExplainsAnswer()
        {
            var sim = new ChapterOneSim(); var samples = sim.S.samples;
            CardResolution resolution = null; sim.CardResolved += x => resolution = x;
            Assert.That(sim.SubmitCard(sim.CurrentCard.id, !sim.CurrentCard.expectedYes), Is.True);
            Assert.That(sim.S.samples, Is.EqualTo(samples));
            Assert.That(sim.S.learningPoints, Is.Zero);
            Assert.That(resolution.correct, Is.False);
            Assert.That(resolution.explanation, Is.Not.Empty);
        }
        [Test] public void ReplayedCardIdIsRejectedWithoutDuplicateReward()
        {
            var sim = new ChapterOneSim(); var card = sim.CurrentCard;
            Assert.That(sim.SubmitCard(card.id, card.expectedYes), Is.True);
            double samples = sim.S.samples, points = sim.S.learningPoints;
            Assert.That(sim.SubmitCard(card.id, card.expectedYes), Is.False);
            Assert.That(sim.S.samples, Is.EqualTo(samples)); Assert.That(sim.S.learningPoints, Is.EqualTo(points));
        }
        [Test] public void CorrectionDoublesSamplesWeakensRecordedRouteAndEmitsReversePulse()
        {
            var sim = new ChapterOneSim();
            for (int i = 0; sim.CurrentCard.predictedYes == sim.CurrentCard.expectedYes && i < 50; i++)
                sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes);
            Assert.That(sim.CurrentCard.predictedYes, Is.Not.EqualTo(sim.CurrentCard.expectedYes));
            var route = sim.S.activeRouteEdges.Select(e => Tuple.Create(e.from, e.to)).ToArray();
            Assert.That(route.Length, Is.GreaterThan(0));
            var before = sim.S.edges.ToDictionary(e => Tuple.Create(e.from, e.to), e => e.weight);
            double samples = sim.S.samples; TrainingPulse reverse = null;
            sim.Pulsed += p => { if (p.correction) reverse = p; };
            sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes);
            Assert.That(sim.S.samples - samples, Is.EqualTo(sim.Config.samplePerCard * 2));
            foreach (var pair in route) Assert.That(sim.S.edges.Single(e => e.from == pair.Item1 && e.to == pair.Item2).weight, Is.LessThan(before[pair]));
            Assert.That(reverse, Is.Not.Null); Assert.That(sim.S.mistakes, Is.Not.Empty);
        }
        [Test] public void SameSeedProducesSameCardsPredictionsAndConfidence()
        {
            var a = new ChapterOneSim(); var b = new ChapterOneSim();
            for (int i = 0; i < 40; i++)
            {
                Assert.That(a.CurrentCard.prompt, Is.EqualTo(b.CurrentCard.prompt));
                Assert.That(a.CurrentCard.predictedYes, Is.EqualTo(b.CurrentCard.predictedYes));
                Assert.That(a.CurrentCard.confidence, Is.EqualTo(b.CurrentCard.confidence));
                a.SubmitCard(a.CurrentCard.id, a.CurrentCard.expectedYes); b.SubmitCard(b.CurrentCard.id, b.CurrentCard.expectedYes);
            }
        }
        [Test] public void SavedActiveCardRngAndPulseStateContinueExactly()
        {
            var original = new ChapterOneSim(); Review(original, 8); original.Tick(7.13);
            var restored = new ChapterOneSim(Clone(original.S));
            Assert.That(restored.CurrentCard.prompt, Is.EqualTo(original.CurrentCard.prompt));
            for (int i = 0; i < 8; i++)
            {
                original.Tick(1.17); restored.Tick(1.17);
                Assert.That(restored.S.steps, Is.EqualTo(original.S.steps));
                Assert.That(restored.CurrentCard.predictedYes, Is.EqualTo(original.CurrentCard.predictedYes));
                original.SubmitCard(original.CurrentCard.id, original.CurrentCard.expectedYes);
                restored.SubmitCard(restored.CurrentCard.id, restored.CurrentCard.expectedYes);
                Assert.That(restored.CurrentCard.prompt, Is.EqualTo(original.CurrentCard.prompt));
            }
        }
        [Test] public void DirectedPathRemovalStopsTrainingAndJobIncome()
        {
            var sim = new ChapterOneSim(); Review(sim, 5); sim.SetJobEnabled(true);
            Assert.That(sim.ToggleEdge(1, 2), Is.True); Assert.That(sim.HasOutputPath, Is.False);
            double money = sim.S.money; sim.Tick(10);
            Assert.That(sim.S.steps, Is.Zero); Assert.That(sim.S.money, Is.EqualTo(money)); Assert.That(sim.IncomePerSecond, Is.Zero);
        }
        [Test] public void ConnectedBoardActuallyTrainsAndStrengthensUsedSynapses()
        {
            var sim = new ChapterOneSim(); double weight = sim.S.edges[0].weight; sim.Tick(20);
            Assert.That(sim.S.steps, Is.GreaterThan(0)); Assert.That(sim.S.edges[0].weight, Is.GreaterThan(weight));
            Assert.That(sim.S.nodes.Any(n => n.fatigue > 0), Is.True);
        }
        [Test] public void UnusedSynapseDecaysWhileChargeLeaksAndFatigueRecovers()
        {
            var sim = new ChapterOneSim(); sim.ToggleEdge(1, 2);
            var node = sim.S.nodes.Single(n => n.id == 3); node.charge = .5; node.fatigue = 1;
            double weight = sim.S.edges[0].weight; sim.Tick(2);
            Assert.That(node.charge, Is.LessThan(.5)); Assert.That(node.fatigue, Is.LessThan(1));
            Assert.That(sim.S.edges[0].weight, Is.LessThan(weight));
        }
        [Test] public void OnlyHexAdjacentDirectedEdgesAreAllowedAndPortsAreProtected()
        {
            var sim = new ChapterOneSim();
            Assert.That(sim.ToggleEdge(1, 7), Is.False); Assert.That(sim.ToggleEdge(2, 1), Is.False);
            Assert.That(sim.ToggleEdge(7, 6), Is.False); Assert.That(sim.ToggleEdge(2, 2), Is.False);
            Assert.That(sim.RemoveNode(1), Is.False); Assert.That(sim.RemoveNode(7), Is.False);
            Assert.That(sim.AddNode(0, 1), Is.False); Assert.That(sim.AddNode(3, 5), Is.False);
            Assert.That(sim.AddNode(2, 1), Is.True);
            int added = sim.S.nodes.Max(n => n.id);
            Assert.That(sim.ToggleEdge(3, added), Is.True); // (2,2) -> (2,1)
            Assert.That(sim.ToggleEdge(4, added), Is.False); // (3,2) -> (2,1): not adjacent
        }
        [Test] public void RemovingHiddenNodeAlsoRemovesIncidentEdgesAndClearsPath()
        {
            var sim = new ChapterOneSim(); Assert.That(sim.RemoveNode(4), Is.True);
            Assert.That(sim.S.edges.Any(e => e.from == 4 || e.to == 4), Is.False); Assert.That(sim.HasOutputPath, Is.False);
        }
        [Test] public void VramPreventsOversizedBoardWithoutChargingMoney()
        {
            var sim = new ChapterOneSim(); double cash = sim.S.money; int accepted = 0;
            for (int q = 1; q <= 5; q++) for (int r = 0; r <= 4; r++) if (sim.AddNode(q, r)) accepted++;
            Assert.That(accepted, Is.GreaterThan(0)); Assert.That(sim.MemoryUsed, Is.LessThanOrEqualTo(sim.MemoryCapacity));
            Assert.That(sim.AddNode(5, 4), Is.False); Assert.That(sim.S.money, Is.EqualTo(cash));
        }
        [Test] public void ExactFourNodeFanOutAndConvergenceAreRecognizedNotLargerStars()
        {
            var sim = new ChapterOneSim(); int[] ids = AddStar(sim);
            for (int i = 1; i < 4; i++) Assert.That(sim.ToggleEdge(ids[0], ids[i]), Is.True);
            Assert.That(sim.FanOutCount, Is.EqualTo(1)); Assert.That(sim.ConvergeCount, Is.Zero);
            for (int i = 1; i < 4; i++) { sim.ToggleEdge(ids[0], ids[i]); sim.ToggleEdge(ids[i], ids[0]); }
            Assert.That(sim.FanOutCount, Is.Zero); Assert.That(sim.ConvergeCount, Is.EqualTo(1));
            Assert.That(sim.ToggleEdge(ids[1], ids[3]), Is.True);
            Assert.That(sim.ConvergeCount, Is.Zero);
        }
        [Test] public void SamplesStepsAndBoardParametersEachCanBeTheBottleneck()
        {
            var sim = new ChapterOneSim(); sim.S.samples = 0; sim.S.steps = 100;
            Assert.That(sim.Bottleneck, Does.Contain("样本")); sim.S.samples = 100; sim.S.steps = 0;
            Assert.That(sim.Bottleneck, Does.Contain("算力")); sim.S.steps = 10000; sim.S.samples = 10000;
            Assert.That(sim.Bottleneck, Does.Contain("显存"));
        }
        [Test] public void AccuracyStaysFiniteAndBelowCapEvenWithStructuresAndHugeResources()
        {
            var sim = new ChapterOneSim(); sim.S.samples = 1e9; sim.S.steps = 1e9; sim.S.semanticLevel = 5;
            Assert.That(sim.Accuracy, Is.InRange(0, sim.Config.maxAccuracy));
        }
        [Test] public void JobsPayAccordingToAccuracyAndOnlyWhileEnabled()
        {
            var sim = new ChapterOneSim(); Review(sim, 6); double before = sim.S.money;
            sim.Tick(2); Assert.That(sim.S.money, Is.EqualTo(before));
            sim.SetJobEnabled(true); sim.Tick(2); Assert.That(sim.S.money, Is.GreaterThan(before));
            Assert.That(sim.S.totalEarned, Is.GreaterThan(0));
            sim.SetJobEnabled(false); before = sim.S.money; sim.Tick(2); Assert.That(sim.S.money, Is.EqualTo(before));
        }
        [Test] public void GpuSlotsCaseCostAndResaleDoNotCreateFreeMoney()
        {
            var sim = new ChapterOneSim(config: new GameConfig { starterMoney = 650 }); double before = sim.S.money;
            Assert.That(sim.BuyGpu(), Is.True); Assert.That(sim.S.gpuCount, Is.EqualTo(2));
            Assert.That(sim.BuyGpu(), Is.False); Assert.That(sim.S.money, Is.EqualTo(before - sim.Config.gpuPrice));
            Assert.That(sim.SellGpu(), Is.True); Assert.That(sim.S.money, Is.LessThan(before));
            Assert.That(sim.SellGpu(), Is.False); sim.S.money = sim.Config.casePrice;
            Assert.That(sim.BuyCase(), Is.True); Assert.That(sim.S.caseCount, Is.EqualTo(2)); Assert.That(sim.S.money, Is.Zero);
        }
        [Test] public void UpgradesSpendPointsExactlyOnceAndRespectLevelCaps()
        {
            var sim = new ChapterOneSim(); Assert.That(sim.UpgradeSkill(), Is.False); sim.S.learningPoints = 1000;
            double points = sim.S.learningPoints; Assert.That(sim.UpgradeSkill(), Is.True);
            Assert.That(sim.S.learningPoints, Is.EqualTo(points - sim.Config.skillPointCost));
            Assert.That(sim.UpgradeTraining(), Is.True); Assert.That(sim.HeartbeatsPerSecond, Is.GreaterThan(1));
            while (sim.UpgradeSkill()) { }
            Assert.That(sim.S.semanticLevel, Is.EqualTo(sim.Config.maxSkillLevel));
        }
        [Test] public void DailyBillingAndSimulationAreIndependentOfTickPartition()
        {
            var config = new GameConfig { dayLengthSeconds = 10 };
            var a = new ChapterOneSim(null, config); var b = new ChapterOneSim(null, config);
            Review(a, 5); Review(b, 5); a.SetJobEnabled(true); b.SetJobEnabled(true);
            a.Tick(31.37); for (int i = 0; i < 3137; i++) b.Tick(.01);
            Assert.That(a.S.day, Is.EqualTo(4)); Assert.That(b.S.day, Is.EqualTo(a.S.day));
            Assert.That(b.S.money, Is.EqualTo(a.S.money).Within(1e-7));
            Assert.That(b.S.steps, Is.EqualTo(a.S.steps)); Assert.That(b.S.temperature, Is.EqualTo(a.S.temperature).Within(1e-8));
            Assert.That(b.S.energyKwh, Is.EqualTo(a.S.energyKwh).Within(1e-8));
        }
        [Test] public void UnpaidPowerStopsJobsAndCorrectManualLabelsRepayDebtWithoutCashGrant()
        {
            var sim = new ChapterOneSim(null, new GameConfig { dayLengthSeconds = 1 });
            sim.S.money = 0; sim.Tick(1); Assert.That(sim.S.unpaidPower, Is.True);
            double debt = sim.S.billDue; Assert.That(debt, Is.GreaterThan(0));
            sim.SetJobEnabled(true); double steps = sim.S.steps; sim.Tick(5);
            Assert.That(sim.S.steps, Is.EqualTo(steps)); Assert.That(sim.S.money, Is.Zero);
            for (int i = 0; sim.S.unpaidPower && i < 100; i++) sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes);
            Assert.That(sim.S.unpaidPower, Is.False); Assert.That(sim.S.billDue, Is.Zero);
            Assert.That(sim.S.money, Is.Zero); Assert.That(sim.PayBill(), Is.False);
        }
        [Test] public void SpareGpuSaleAndPayBillRecoverWithoutDuplicatingSettlement()
        {
            var sim = new ChapterOneSim(config: new GameConfig { starterMoney = 650 }); sim.BuyGpu(); sim.S.money = 0; sim.S.billDue = 10; sim.S.unpaidPower = true;
            Assert.That(sim.SellGpu(), Is.True); Assert.That(sim.PayBill(), Is.True); double cash = sim.S.money;
            Assert.That(sim.PayBill(), Is.False); Assert.That(sim.S.money, Is.EqualTo(cash)); Assert.That(sim.HeartbeatsPerSecond, Is.GreaterThan(0));
        }
        [Test] public void OverloadRequiresReducedLoadBeforeManualBreakerReset()
        {
            var sim = new ChapterOneSim(null, new GameConfig { powerLimitWatts = 150, starterMoney = 650 }); sim.BuyGpu(); sim.Tick(1);
            Assert.That(sim.S.breakerTripped, Is.True); Assert.That(sim.ResetBreaker(), Is.False);
            Assert.That(sim.SellGpu(), Is.True); Assert.That(sim.ResetBreaker(), Is.True); Assert.That(sim.ResetBreaker(), Is.False);
        }
        [Test] public void HeatCausesThrottlingAndOutageAllowsCooling()
        {
            var sim = new ChapterOneSim(null, new GameConfig { throttleTemperature = 40 });
            sim.Tick(50); Assert.That(sim.S.temperature, Is.GreaterThan(40)); Assert.That(sim.HeartbeatsPerSecond, Is.LessThan(1));
            double hot = sim.S.temperature; sim.S.unpaidPower = true; sim.S.billDue = 100; sim.Tick(5);
            Assert.That(sim.S.temperature, Is.LessThan(hot)); Assert.That(sim.HeartbeatsPerSecond, Is.Zero);
        }
        [Test] public void InvalidTimeDoesNothingAndCorruptNumericSaveIsRejected()
        {
            var sim = new ChapterOneSim(); sim.Tick(double.NaN); sim.Tick(double.PositiveInfinity); sim.Tick(-1);
            Assert.That(sim.S.gameSeconds, Is.Zero); var bad = Clone(sim.S); bad.money = double.NaN;
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(bad));
        }
        [Test] public void CorruptDuplicateNodesDanglingEdgesAndOversizedBoardAreRejected()
        {
            var sim = new ChapterOneSim(); var a = Clone(sim.S); a.nodes.Add(a.nodes[0]);
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(a));
            var b = Clone(sim.S); b.edges.Add(new EdgeState { from = 100, to = 7, weight = 1 });
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(b));
            var c = Clone(sim.S); c.caseCount = -1; Assert.Throws<ArgumentException>(() => new ChapterOneSim(c));
        }
        [Test] public void ExamGateExplainsMissingPreparation()
        {
            var sim = new ChapterOneSim(); Assert.That(sim.CanStartExam, Is.False); Assert.That(sim.StartExam(), Is.False);
            Assert.That(sim.LastMessage, Is.Not.Empty);
        }
        [Test] public void FullFreshWalkthroughEarnsIncomeUpgradesAndCompletesOneChapterOnly()
        {
            var sim = Prepared(); Assert.That(sim.S.totalEarned, Is.GreaterThan(0)); Assert.That(sim.CanStartExam, Is.True);
            Assert.That(sim.StartExam(), Is.True); Assert.That(sim.StartExam(), Is.False);
            for (int i = 0; i < 20; i++) Assert.That(sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes), Is.True);
            Assert.That(sim.S.examActive, Is.False); Assert.That(sim.S.examCorrect, Is.EqualTo(20));
            Assert.That(sim.S.chapterOneComplete, Is.True); Assert.That(sim.S.chapter, Is.EqualTo(1));
        }
        [Test] public void FailedExamCanRetryAndRewardIsGrantedOnlyOnce()
        {
            var sim = Prepared(); sim.StartExam();
            for (int i = 0; i < 20; i++) sim.SubmitCard(sim.CurrentCard.id, !sim.CurrentCard.expectedYes);
            Assert.That(sim.S.chapterOneComplete, Is.False); Assert.That(sim.S.examAttempts, Is.EqualTo(1));
            Assert.That(sim.StartExam(), Is.True); double cash = sim.S.money;
            for (int i = 0; i < 20; i++) sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes);
            Assert.That(sim.S.money, Is.EqualTo(cash + sim.Config.examReward));
            Assert.That(sim.StartExam(), Is.False); Assert.That(new ChapterOneSim(Clone(sim.S)).StartExam(), Is.False);
        }
        [Test] public void ExamSaveRestorePreservesQuestionIndexScoreAndRemainingDeck()
        {
            var original = Prepared(); original.StartExam(); Review(original, 7);
            var restored = new ChapterOneSim(Clone(original.S));
            Assert.That(restored.S.examAnswered, Is.EqualTo(7)); Assert.That(restored.S.examCorrect, Is.EqualTo(7));
            for (int i = 7; i < 20; i++)
            {
                Assert.That(restored.CurrentCard.prompt, Is.EqualTo(original.CurrentCard.prompt));
                Assert.That(restored.CurrentCard.expectedYes, Is.EqualTo(original.CurrentCard.expectedYes));
                original.SubmitCard(original.CurrentCard.id, original.CurrentCard.expectedYes); restored.SubmitCard(restored.CurrentCard.id, restored.CurrentCard.expectedYes);
            }
            Assert.That(restored.S.money, Is.EqualTo(original.S.money)); Assert.That(restored.S.chapterOneComplete, Is.True);
        }
        [Test] public void InvalidExamProgressSaveIsRejectedRatherThanGrantingReward()
        {
            var state = Clone(Prepared().S); state.examActive = true; state.examAnswered = 21;
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(state));
        }
        [Test] public void ReversePulseCallbackCannotSubmitSameCardReentrantly()
        {
            var sim = new ChapterOneSim();
            while (sim.CurrentCard.predictedYes == sim.CurrentCard.expectedYes) sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes);
            var card = sim.CurrentCard; double samples = sim.S.samples; bool attempted = false, accepted = true;
            sim.Pulsed += pulse => { if (pulse.correction && !attempted) { attempted = true; accepted = sim.SubmitCard(card.id, card.expectedYes); } };
            sim.SubmitCard(card.id, card.expectedYes);
            Assert.That(attempted, Is.True); Assert.That(accepted, Is.False);
            Assert.That(sim.S.samples - samples, Is.EqualTo(sim.Config.samplePerCard * 2));
        }
        [Test] public void FourteenQuickReviewsThenNinetySecondsReachFirstExamWithoutMandatoryPurchase()
        {
            var sim = new ChapterOneSim(); Review(sim, 14); sim.Tick(90);
            Assert.That(sim.CanStartExam, Is.True, "Fast reviewers should not be trapped by route correction weakening.");
        }
        [Test] public void DuplicateRecordedRoutesAndOverflowedCountersAreRejected()
        {
            var sim = new ChapterOneSim(); var duplicate = Clone(sim.S); duplicate.activeRoute.Add(duplicate.activeRoute[0]);
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(duplicate));
            var overflow = Clone(sim.S); overflow.cardsReviewed = int.MaxValue;
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(overflow));
        }
        [Test] public void PowerOutagePreservesLearnedWeightsWhileCooling()
        {
            var sim = new ChapterOneSim(); sim.Tick(4); sim.S.billDue = 10; sim.S.unpaidPower = true;
            var weights = sim.S.edges.Select(e => e.weight).ToArray(); sim.Tick(100);
            Assert.That(sim.S.edges.Select(e => e.weight).ToArray(), Is.EqualTo(weights));
        }
        [Test] public void ExhaustedUnusedSynapseIsPrunedAndCanBeReconnectedWithoutCash()
        {
            var sim = new ChapterOneSim(); sim.ToggleEdge(1, 2); var edge = sim.S.edges[0]; edge.weight = sim.Config.minimumWeight + .00001;
            int from = edge.from, to = edge.to; double cash = sim.S.money; sim.Tick(1);
            Assert.That(sim.S.edges.Any(e => e.from == from && e.to == to), Is.False);
            Assert.That(sim.ToggleEdge(from, to), Is.True); Assert.That(sim.S.money, Is.EqualTo(cash));
        }
        [Test] public void LowChargeBelowThresholdDoesNotProduceTrainingStep()
        {
            var sim = new ChapterOneSim(null, new GameConfig { signalStrength = .01 }); sim.Tick(1);
            Assert.That(sim.S.steps, Is.Zero); Assert.That(sim.S.nodes.All(n => n.fatigue == 0), Is.True);
        }
        [Test] public void RestoreChecksSerializingDuringCorrectionCallbackCannotReplayRewards()
        {
            var sim = new ChapterOneSim();
            while (sim.CurrentCard.predictedYes == sim.CurrentCard.expectedYes) sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes);
            var card = sim.CurrentCard; GameState snapshot = null;
            sim.Pulsed += p => { if (p.correction) snapshot = Clone(sim.S); };
            sim.SubmitCard(card.id, card.expectedYes);
            var restored = new ChapterOneSim(snapshot);
            Assert.That(restored.SubmitCard(card.id, card.expectedYes), Is.False);
        }
        private static void Review(ChapterOneSim sim, int count)
        {
            for (int i = 0; i < count; i++) sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes);
        }
        private static ChapterOneSim Prepared()
        {
            var sim = new ChapterOneSim();
            // Alternate review and real training time: repeated immediate corrections alone weaken routes.
            for (int i = 0; i < 20; i++) { sim.Tick(3); sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes); }
            sim.UpgradeSkill(); sim.UpgradeTraining(); sim.SetJobEnabled(true); sim.Tick(40);
            return sim;
        }
        private static int[] AddStar(ChapterOneSim sim)
        {
            int[,] locations = { { 2, 0 }, { 1, 0 }, { 3, 0 }, { 1, 1 } }; var ids = new int[4];
            for (int i = 0; i < 4; i++) { Assert.That(sim.AddNode(locations[i, 0], locations[i, 1]), Is.True); ids[i] = sim.S.nodes.Max(n => n.id); }
            return ids;
        }
        private static GameState Clone(GameState source)
        {
            // Explicit DTO round trip without Unity or third-party serializers; also detects forgotten state fields.
            var result = new GameState();
            foreach (var field in typeof(GameState).GetFields())
            {
                object value = field.GetValue(source);
                if (value is List<NodeState> nodes) value = nodes.Select(n => new NodeState { id = n.id, q = n.q, r = n.r, kind = n.kind, charge = n.charge, fatigue = n.fatigue }).ToList();
                else if (value is List<EdgeState> edges) value = edges.Select(e => new EdgeState { from = e.from, to = e.to, weight = e.weight }).ToList();
                else if (value is List<int> ids) value = new List<int>(ids);
                else if (value is List<string> strings) value = new List<string>(strings);
                else if (value is CardData card) value = new CardData { id = card.id, prompt = card.prompt, explanation = card.explanation, category = card.category, expectedYes = card.expectedYes, predictedYes = card.predictedYes, confidence = card.confidence };
                field.SetValue(result, value);
            }
            return result;
        }
    }
}
