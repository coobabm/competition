using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgCollaborationTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000, trained;
            public string blocker = null;
            public double compute = 1;
            public int spendCalls, trainCalls;
            public bool rejectSpend = false;
            public double Compute => compute;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { spendCalls++; if (rejectSpend || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { trainCalls++; trained += seconds; }
            public string Blocker => blocker;
        }
        static XgSim Ready(bool coop = true, bool brain = false)
        {
            var sim = new XgSim { GoldChance = 0, BrainOnline = brain };
            sim.S.desksOpen.Add("mnist"); // digits open at stage two; these tests exercise both desks
            sim.S.autoLevel = 1;
            sim.S.best.Add(new XgBest { dataset = "mnist", acc = .8, arch = "perceptron" });
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .8, arch = "perceptron" });
            if (coop) sim.S.unlocked.Add("label.coop");
            if (brain) sim.S.unlocked.Add("label.brain");
            return sim;
        }
        static XgCard Judged(XgSim sim, bool correct, double confidence, string desk = "spam")
        {
            var c = sim.CreateLabelCard(desk);
            c.hasJudgment = true; c.guess = correct ? c.truth : !c.truth; c.confidence = confidence;
            c.judgeSource = "checkpoint";
            sim.S.prefetch.Add(c);
            return c;
        }
        static XgJudgeTicket Ticket(XgSim sim, Host host)
        {
            sim.FillJudgmentBuffer();
            Assert.IsTrue(sim.TryTakeBrainTicket(out var ticket));
            Assert.IsTrue(sim.DispatchBrainTicket(ticket, host));
            return ticket;
        }
        static double CorrectProbability(XgSim sim, XgJudgeTicket ticket, bool correct = true)
        {
            bool truth = sim.S.prefetch.Find(c => c.id == ticket.cardId).truth;
            return truth == correct ? .97 : .03;
        }

        [Test]
        public void LegacyDeskAutomationMigratesToMaximumGlobalLevel()
        {
            var state = new XgState();
            state.autoLevels.Add(new XgLabelCount { dataset = "mnist", count = 2 });
            state.autoLevels.Add(new XgLabelCount { dataset = "logic", count = 4 });
            var sim = new XgSim(state);
            Assert.AreEqual(4, sim.AutoLevel("spam"));
            Assert.AreEqual(4, sim.AutoLevelTotal, "Do not add old levels together");
            Assert.AreEqual(4, new XgSim(sim.S).GlobalAutoLevel, "Migration is idempotent");
        }
        [Test]
        public void LegacyTrackAutomationAndMalformedLevelsAreBounded()
        {
            var s = new XgState { autoVision = 2, autoSequence = 7, autoLogic = 3 };
            s.autoLevels.Add(new XgLabelCount { dataset = "spam", count = double.NaN });
            var sim = new XgSim(s);
            Assert.AreEqual(7, sim.GlobalAutoLevel);
            Assert.AreEqual(0, sim.S.autoSequence);
            sim.S.autoLevel = 100;
            Assert.AreEqual(10, new XgSim(sim.S).GlobalAutoLevel);
        }
        [Test]
        public void ThresholdHasSafeDefaultsAndRejectsNonFiniteValues()
        {
            var sim = new XgSim();
            Assert.AreEqual(.8, sim.S.coopThreshold);
            sim.SetCoopThreshold(.1); Assert.AreEqual(.5, sim.S.coopThreshold);
            sim.SetCoopThreshold(2); Assert.AreEqual(.99, sim.S.coopThreshold);
            sim.SetCoopThreshold(double.NaN); Assert.AreEqual(.99, sim.S.coopThreshold);
            sim.S.coopThreshold = double.PositiveInfinity;
            Assert.AreEqual(.8, new XgSim(sim.S).S.coopThreshold);
        }
        [Test]
        public void LabelNodesShareTheExistingRaiseAndGlobalAutoPurchase()
        {
            var sim = Ready(); var host = new Host();
            Assert.AreEqual(9, XgSim.LabelNodes.Length, "seven collaboration nodes plus 题库比对 and 验证码代填");
            var raise = XgCatalog.Node("label.raise");
            var auto = XgCatalog.Node("label.auto");
            Assert.AreEqual(17, raise.maxLevel); Assert.AreEqual(10, auto.maxLevel);
            Assert.IsTrue(sim.BuyNode(raise.id, host));
            Assert.AreEqual(1, sim.RaiseLevel); Assert.AreEqual(1, sim.LabelNodeLevel(raise));
            Assert.AreEqual(1, sim.BuyRaise(host)); Assert.AreEqual(2, sim.LabelNodeLevel(raise));
            Assert.IsTrue(sim.BuyNode(auto.id, host));
            Assert.AreEqual(2, sim.GlobalAutoLevel); Assert.AreEqual(2, sim.AutoLevel("mnist"));
        }
        [Test]
        public void BrainRequiresStageFour()
        {
            var sim = Ready(); var host = new Host(); var node = XgCatalog.Node("label.brain");
            sim.S.stage = 3;
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.StatusLabelNode(node, host));
            sim.S.stage = 4;
            Assert.AreEqual(XgSim.NodeStatus.Buyable, sim.StatusLabelNode(node, host));
        }
        [Test]
        public void PrefetchIsThreePerDeskAndNeverReplacesTheVisibleCard()
        {
            var sim = Ready(brain: true); var visible = sim.Card("spam");
            for (int i = 0; i < 30; i++) sim.FillJudgmentBuffer();
            Assert.AreEqual(3, sim.PrefetchCount("spam")); Assert.AreEqual(3, sim.PrefetchCount("mnist"));
            Assert.AreSame(visible, sim.Card("spam"));
            var ids = new HashSet<long> { visible.id };
            foreach (var c in sim.S.prefetch) Assert.IsTrue(ids.Add(c.id));
        }
        [Test]
        public void TicketsContainOnlyAllowedPromptAndDispatchBillsOnce()
        {
            var sim = Ready(brain: true); var host = new Host();
            var ticket = Ticket(sim, host);
            Assert.AreEqual("spam", ticket.dataset); Assert.IsNotEmpty(ticket.prompt);
            Assert.IsNull(typeof(XgJudgeTicket).GetField("truth"));
            Assert.IsNull(typeof(XgJudgeTicket).GetField("why"));
            Assert.IsNull(typeof(XgJudgeTicket).GetField("roll"));
            Assert.IsFalse(sim.DispatchBrainTicket(ticket, host));
            Assert.AreEqual(.05, host.trained, 1e-9);
        }
        [Test]
        public void RealPredictionIsRecordedBeforeTruthIsUsedForSettlement()
        {
            var sim = Ready(brain: true); var host = new Host(); var ticket = Ticket(sim, host);
            double money = host.money;
            Assert.IsTrue(sim.CompleteBrainTicket(ticket, CorrectProbability(sim, ticket)));
            var card = sim.S.prefetch.Find(c => c.id == ticket.cardId);
            Assert.AreEqual("brain", card.judgeSource); Assert.AreEqual(.97, card.confidence, 1e-9);
            Assert.AreEqual(money, host.money); Assert.AreEqual(0, sim.Labels("spam"));
            Assert.AreEqual(1, sim.BrainJudgmentCount("spam"));
            Assert.IsTrue(sim.Route(ticket.cardId, host));
            Assert.AreEqual(1, sim.Labels("spam")); Assert.Greater(host.money, money);
            money = host.money;
            Assert.IsFalse(sim.Route(ticket.cardId, host));
            Assert.IsFalse(sim.CompleteBrainTicket(ticket, .97)); Assert.AreEqual(money, host.money);
        }
        [Test]
        public void LowConfidenceWrongGuessRoutesToHumanWithoutUsingTruthToChooseRoute()
        {
            var sim = Ready(); var host = new Host();
            var right = Judged(sim, true, .62); var wrong = Judged(sim, false, .62);
            Assert.IsTrue(sim.Route(right.id, host)); Assert.IsTrue(sim.Route(wrong.id, host));
            Assert.AreEqual(2, sim.ReviewCount()); Assert.AreEqual(0, sim.S.autoCorrect + sim.S.autoWrong);
            Assert.AreEqual(0, sim.NoiseTotal);
        }
        [Test]
        public void ReviewQueueAtTwentyPausesAllAutomationAndResumeDoesNotBurst()
        {
            var sim = Ready(); var host = new Host();
            for (int i = 0; i < 20; i++) { var c = Judged(sim, true, .5); Assert.IsTrue(sim.Route(c.id, host)); }
            var waiting = Judged(sim, true, .99, "mnist");
            long next = sim.S.nextCardId; double labels = sim.TotalLabels;
            Assert.IsFalse(sim.Route(waiting.id, host));
            for (int i = 0; i < 100; i++) sim.TickCollaboration(1, host);
            Assert.AreEqual(20, sim.ReviewCount()); Assert.AreEqual(next, sim.S.nextCardId); Assert.AreEqual(labels, sim.TotalLabels);
            var review = sim.ReviewCard("spam"); Assert.IsTrue(sim.AnswerQueued(review.id, review.truth, host).accepted);
            sim.TickCollaboration(.01, host);
            Assert.AreEqual(19, sim.ReviewCount()); Assert.AreEqual(labels + 1, sim.TotalLabels);
        }
        [Test]
        public void NoisePenalizesEffectiveSamplesAndCurrentEvaluation()
        {
            var sim = Ready(); sim.UseBoard = false; var host = new Host();
            sim.PlatformChecks = false; // a spot-checked wrong label never becomes noise; see XgQualityTests
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 100 });
            sim.S.sequence.dataset = "spam"; sim.S.sequence.steps = 10000;
            sim.Evaluate(sim.S.sequence); double before = sim.S.sequence.valAcc;
            for (int i = 0; i < 20; i++) { var c = Judged(sim, false, .98); sim.Route(c.id, host); }
            Assert.AreEqual(20, sim.Noise("spam")); Assert.AreEqual(60, sim.EffectiveLabelSamples("spam"));
            Assert.Less(sim.S.sequence.valAcc, before);
            Assert.AreEqual(100, sim.Labels("spam"), "Noise is not a good sample");
        }
        [Test]
        public void WithoutCoopWrongAutomaticAnswersRemainDiscarded()
        {
            var sim = Ready(coop: false); var host = new Host(); var c = Judged(sim, false, .5);
            sim.PlatformChecks = false; // platform fines for wrong automatic labels are covered in XgQualityTests
            double money = host.money; Assert.IsTrue(sim.Route(c.id, host));
            Assert.AreEqual(0, sim.ReviewCount()); Assert.AreEqual(0, sim.NoiseTotal);
            Assert.AreEqual(1, sim.S.autoWrong); Assert.AreEqual(money, host.money);
        }
        [Test]
        public void QualityAuditInterceptsApproximatelyFortyPercentBeforeNoiseOrPay()
        {
            var sim = Ready(); var host = new Host(); sim.S.unlocked.Add("label.audit");
            sim.PlatformChecks = false; // 2000 wrong labels in a row would get the account reported and frozen
            int audited = 0; double start = host.money;
            for (int i = 0; i < 2000; i++)
            {
                var c = Judged(sim, false, .99); sim.Route(c.id, host);
                if (sim.ReviewCount() > 0) { audited++; sim.S.queue.Clear(); }
            }
            Assert.That(audited, Is.InRange(700, 900));
            Assert.AreEqual(2000 - audited, sim.NoiseTotal); Assert.AreEqual(start, host.money);
        }
        [Test]
        public void HumanCorrectionRewardsHardCasesAndIsExactlyOnce()
        {
            var sim = Ready(); var host = new Host(); sim.S.unlocked.Add("label.hard");
            sim.S.payRaise = 5;
            var c = Judged(sim, false, .6); sim.Route(c.id, host);
            var result = sim.AnswerQueued(c.id, c.truth, host);
            Assert.IsTrue(result.accepted && result.queued && result.corrected && result.correct);
            Assert.AreEqual(2, result.combo); Assert.AreEqual(2, result.samples); Assert.AreEqual(2, sim.Labels("spam"));
            Assert.AreEqual(sim.PayFor("spam", c.level) * XgCatalog.RaiseMultipliers[5] * 1.1 * 1.5, result.pay, 1e-9);
            double money = host.money;
            Assert.IsFalse(sim.AnswerQueued(c.id, c.truth, host).accepted); Assert.AreEqual(money, host.money);
        }
        [Test]
        public void WrongReviewClearsComboAndNeverPays()
        {
            var sim = Ready(); var host = new Host(); sim.S.combo = 8;
            var c = Judged(sim, true, .6); sim.Route(c.id, host); double money = host.money;
            var result = sim.AnswerQueued(c.id, !c.truth, host);
            Assert.IsTrue(result.accepted); Assert.IsFalse(result.correct);
            Assert.AreEqual(0, sim.S.combo); Assert.AreEqual(money, host.money); Assert.AreEqual(0, sim.Labels("spam"));
        }
        [Test]
        public void ReloadPreservesCompletedPredictionsButRejectsOldAndRetriedTickets()
        {
            var sim = Ready(brain: true); var host = new Host();
            var done = Ticket(sim, host); sim.CompleteBrainTicket(done, CorrectProbability(sim, done));
            var pending = Ticket(sim, host);
            var restored = new XgSim(sim.S) { BrainOnline = true };
            Assert.AreEqual("brain", restored.S.prefetch.Find(c => c.id == done.cardId).judgeSource);
            Assert.IsTrue(restored.S.prefetch.Find(c => c.id == done.cardId).hasJudgment);
            Assert.IsFalse(restored.CompleteBrainTicket(pending, .9));
            Assert.IsTrue(restored.TryTakeBrainTicket(out var retry));
            Assert.AreEqual(pending.cardId, retry.cardId);
            Assert.AreNotEqual(pending.generation, retry.generation);
            Assert.Greater(retry.attempt, pending.attempt);
        }
        [Test]
        public void PowerCutBlocksDispatchAndRoutingButNotHumanWork()
        {
            var sim = Ready(brain: true); var host = new Host(); sim.FillJudgmentBuffer();
            Assert.IsTrue(sim.TryTakeBrainTicket(out var ticket)); host.blocker = "power-cut";
            Assert.IsFalse(sim.DispatchBrainTicket(ticket, host)); Assert.AreEqual(0, host.trained);
            var c = Judged(sim, true, .99); Assert.IsFalse(sim.Route(c.id, host));
            Assert.IsTrue(sim.ReleaseBrainTicket(ticket));
        }
        [Test]
        public void FailureFallsBackExplicitlyAndDoesNotCountAsBrainAccuracy()
        {
            var sim = Ready(brain: true); var host = new Host(); var ticket = Ticket(sim, host);
            Assert.IsTrue(sim.CompleteBrainTicket(ticket, null, "timeout"));
            var card = sim.S.prefetch.Find(c => c.id == ticket.cardId);
            Assert.AreEqual("checkpoint", card.judgeSource); Assert.AreEqual("timeout", card.judgeFailure);
            Assert.AreEqual(-1, card.brainP); Assert.AreEqual(0, sim.BrainJudgmentCount("spam"));
            Assert.IsFalse(sim.CompleteBrainTicket(ticket, .9), "The fallback also consumes the ticket");
        }
        [Test]
        public void OfflineSimulationNeverIssuesRealModelTickets()
        {
            var sim = Ready(brain: true); sim.OfflineSimulation = true; sim.FillJudgmentBuffer();
            Assert.IsFalse(sim.TryTakeBrainTicket(out _));
            foreach (var c in sim.S.prefetch)
            {
                Assert.IsTrue(c.hasJudgment); Assert.AreEqual("checkpoint", c.judgeSource);
                if (c.dataset == "spam") Assert.AreEqual("offline-simulation", c.judgeFailure);
            }
            Assert.AreEqual(0, sim.S.brainStats.Count);
        }
        [Test]
        public void RollingWindowsHaveFixedBoundsAndSeparateModelProvenance()
        {
            var sim = Ready(brain: true); var host = new Host();
            for (int i = 0; i < 120; i++)
            {
                var ticket = Ticket(sim, host); sim.CompleteBrainTicket(ticket, CorrectProbability(sim, ticket)); sim.Route(ticket.cardId, host);
            }
            Assert.AreEqual(50, sim.BrainJudgmentCount("spam")); Assert.AreEqual(1, sim.BrainAccuracy("spam"));
            Assert.AreEqual(100, sim.S.routeHistory.Count); Assert.AreEqual(6, sim.S.autoFeed.Count);
            var stats = sim.CollaborationStats(); Assert.AreEqual(1, stats.automaticFraction); Assert.AreEqual(1, stats.automaticAccuracy);
            Assert.AreEqual(0, sim.BrainJudgmentCount("mnist"));
        }
        [Test]
        public void CheckpointThresholdHigherMeansLessAutomaticAndBetterPrecision()
        {
            var source = Ready(); int lowAuto = 0, highAuto = 0, lowCorrect = 0, highCorrect = 0;
            for (int i = 0; i < 800; i++)
            {
                source.FillJudgmentBuffer();
                foreach (var card in source.S.prefetch)
                {
                    if (card.dataset != "spam") continue;
                    if (card.confidence >= .5) { lowAuto++; if (card.guess == card.truth) lowCorrect++; }
                    if (card.confidence >= .8) { highAuto++; if (card.guess == card.truth) highCorrect++; }
                }
                source.S.prefetch.Clear();
            }
            Assert.Less(highAuto, lowAuto);
            Assert.Greater((double)highCorrect / highAuto, (double)lowCorrect / lowAuto);
        }
        [Test]
        public void CheckpointJudgingUsesDeployedArchitectureRatherThanCurrentTrainingArchitecture()
        {
            var sim = Ready();
            sim.S.best.Find(b => b.dataset == "mnist").acc = .99;
            sim.S.vision.arch = "lenet";
            var card = sim.CreateLabelCard("mnist");
            card.kind = "combo"; card.truth = true; card.roll = .7;
            sim.S.prefetch.Add(card);
            sim.FillJudgmentBuffer();
            Assert.IsFalse(card.guess, "The deployed perceptron is capped at .55 on XOR even if another run trains a CNN");
            Assert.AreEqual("checkpoint", card.judgeSource);
        }
        [Test]
        public void MalformedNoiseCannotPoisonTrainingDuringLoad()
        {
            var sim = Ready();
            sim.S.noise.Add(new XgLabelCount { dataset = "spam", count = double.NaN });
            var restored = new XgSim(sim.S);
            Assert.IsFalse(double.IsNaN(restored.S.sequence.valAcc));
            Assert.AreEqual(0, restored.Noise("spam"));
        }

        [Test]
        public void LabelPresentationWaitsForTheStoryGateAndPrioritizesExperiments()
        {
            var sim = Ready();
            var api = typeof(XgSim).GetMethod("LabelPresentationMode");
            Assert.IsNotNull(api, "The same pure policy must drive the real label UI");
            Func<bool, int> mode = ready => (int)api.Invoke(sim, new object[] { ready });
            Assert.AreEqual(0, mode(false));
            sim.S.project.started = true; sim.S.project.awaitingAnswer = true;
            Assert.AreEqual(1, mode(false));
            sim.S.project.awaitingAnswer = false; sim.S.project.completed = true;
            Assert.AreEqual(0, mode(false), "Do not show the ending before its story message");
            Assert.AreEqual(2, mode(true));
            Assert.IsTrue(sim.EndingAnswer(false));
            Assert.AreEqual(3, mode(false)); Assert.AreEqual(3, mode(true));
        }

        // ReviewingCombinationChallengesStillAdvancesTheObservedWall was retired with the design v1.1 walls: the XOR wall
        // is its own dataset now, not a count of reviewed combination cards (see XgProgressionTests).

        [Test]
        public void ResearchReservesGpuAndPausesAutomationButAllowsHumanReview()
        {
            var sim = Ready(brain: true); var host = new Host();
            var review = Judged(sim, true, .6); sim.Route(review.id, host);
            var inFlight = Ticket(sim, host);
            Assert.IsTrue(sim.TryTakeBrainTicket(out var notDispatched));
            sim.S.project.started = true;
            double money = host.money, energy = host.trained, labels = sim.TotalLabels;
            long nextId = sim.S.nextCardId;
            Assert.AreEqual(0, sim.CollaborationRate("spam", host));
            Assert.IsFalse(sim.DispatchBrainTicket(notDispatched, host));
            sim.FillJudgmentBuffer(); Assert.AreEqual(nextId, sim.S.nextCardId);
            Assert.IsTrue(sim.CompleteBrainTicket(inFlight, CorrectProbability(sim, inFlight)));
            Assert.IsFalse(sim.Route(inFlight.cardId, host), "In-flight results may be cached, not settled during research");
            for (int segment = 0; segment < 3; segment++)
            {
                sim.Tick(200, host);
                Assert.IsTrue(sim.ProjectAwaitingAnswer);
                Assert.AreEqual(labels, sim.TotalLabels); Assert.AreEqual(money, host.money);
                if (segment < 2) Assert.IsTrue(sim.ProjectAnswer(true));
            }
            Assert.AreEqual(600, host.trained - energy, 1e-9);
            Assert.IsTrue(sim.AnswerQueued(review.id, review.truth, host).accepted, "Human reviews need no GPU");
            Assert.Greater(sim.TotalLabels, labels);
        }

        static int Clean(XgSim sim, string dataset, Host host, int maximum = 10)
        {
            var method = typeof(XgSim).GetMethod("CleanNoise");
            Assert.IsNotNull(method, "Simulation dataset cleaning needs an authoritative Core action");
            return (int)method.Invoke(sim, new object[] { dataset, host, maximum });
        }
        static XgSim Noisy()
        {
            var sim = Ready(); sim.UseBoard = false; sim.S.unlocked.Add("label.audit");
            sim.S.noise.Add(new XgLabelCount { dataset = "spam", count = 12 });
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 30 });
            sim.S.sequence.dataset = "spam"; sim.S.sequence.steps = 1200;
            sim.Evaluate(sim.S.sequence); return sim;
        }
        [Test]
        public void CleaningNoiseDiscardsBadRowsWithoutCreatingSamplesCheckpointsOrRewards()
        {
            var sim = Noisy(); var host = new Host(); sim.S.combo = 7;
            double money = host.money, income = sim.S.totalIncome, samples = sim.Samples("spam"), labels = sim.Labels("spam");
            double steps = sim.S.sequence.steps, score = sim.S.sequence.valAcc, best = sim.BestAcc("spam"), trainedSeconds = sim.S.trainedSeconds;
            int models = sim.S.models.Count, epochs = sim.S.epochs;
            Assert.AreEqual(10, Clean(sim, "spam", host));
            Assert.AreEqual(2, sim.Noise("spam")); Assert.AreEqual(money - 20, host.money, 1e-9);
            Assert.AreEqual(1, host.trained, 1e-9); Assert.AreEqual(1, host.spendCalls); Assert.AreEqual(1, host.trainCalls);
            Assert.AreEqual(20, sim.S.totalSpent, 1e-9);
            Assert.AreEqual(samples, sim.Samples("spam")); Assert.AreEqual(labels, sim.Labels("spam"));
            Assert.AreEqual(income, sim.S.totalIncome); Assert.AreEqual(best, sim.BestAcc("spam"));
            Assert.AreEqual(models, sim.S.models.Count); Assert.AreEqual(epochs, sim.S.epochs);
            Assert.AreEqual(steps, sim.S.sequence.steps); Assert.AreEqual(trainedSeconds, sim.S.trainedSeconds); Assert.AreEqual(7, sim.S.combo);
            Assert.Greater(sim.S.sequence.valAcc, score, "Only current simulated evaluation benefits from discarding noisy rows");
        }
        [Test]
        public void CleaningNoiseCapsAtTenAndAnEmptyRepeatDoesNotChargeAgain()
        {
            var sim = Noisy(); var host = new Host();
            Assert.AreEqual(10, Clean(sim, "spam", host, int.MaxValue));
            Assert.AreEqual(2, Clean(sim, "spam", host));
            double money = host.money, energy = host.trained; int spends = host.spendCalls, bills = host.trainCalls;
            Assert.AreEqual(0, Clean(sim, "spam", host)); Assert.AreEqual(0, sim.Noise("spam"));
            Assert.AreEqual(money, host.money); Assert.AreEqual(energy, host.trained);
            Assert.AreEqual(spends, host.spendCalls); Assert.AreEqual(bills, host.trainCalls);
        }
        [Test]
        public void CleaningNoiseUsesTheAffordableRequestedSubset()
        {
            var sim = Noisy(); var host = new Host { money = 5 };
            Assert.AreEqual(2, Clean(sim, "spam", host)); Assert.AreEqual(1, host.money, 1e-9); Assert.AreEqual(.2, host.trained, 1e-9);
            Assert.AreEqual(10, sim.Noise("spam")); host.money = 100;
            Assert.AreEqual(3, Clean(sim, "spam", host, 3)); Assert.AreEqual(7, sim.Noise("spam"));
        }
        [TestCase("locked")][TestCase("power")][TestCase("compute")][TestCase("project")]
        [TestCase("money")][TestCase("unknown")][TestCase("zero")][TestCase("negative")]
        [TestCase("nan-money")][TestCase("nan-noise")][TestCase("empty")]
        public void CleaningNoiseFailsClosedWithoutChargingOrElectricity(string reason)
        {
            var sim = Noisy(); var host = new Host(); string dataset = "spam"; int maximum = 10;
            switch (reason)
            {
                case "locked": sim.S.unlocked.Remove("label.audit"); break;
                case "power": host.blocker = "power-off"; break;
                case "compute": host.compute = 0; break;
                case "project": sim.S.project.started = true; break;
                case "money": host.money = 1; break;
                case "unknown": dataset = "unknown"; break;
                case "zero": maximum = 0; break;
                case "negative": maximum = -1; break;
                case "nan-money": host.money = double.NaN; break;
                case "nan-noise": sim.S.noise[0].count = double.NaN; break;
                case "empty": sim.S.noise[0].count = 0; break;
            }
            double before = sim.Noise("spam");
            Assert.AreEqual(0, Clean(sim, dataset, host, maximum));
            Assert.AreEqual(0, host.spendCalls); Assert.AreEqual(0, host.trainCalls); Assert.AreEqual(0, host.trained);
            Assert.AreEqual(before, sim.Noise("spam")); Assert.AreEqual(0, sim.S.totalSpent);
        }
        [Test]
        public void CleaningNoiseDoesNotMutateAnythingWhenHostRejectsPayment()
        {
            var sim = Noisy(); var host = new Host { rejectSpend = true }; double money = host.money;
            Assert.AreEqual(0, Clean(sim, "spam", host));
            Assert.AreEqual(12, sim.Noise("spam")); Assert.AreEqual(money, host.money);
            Assert.AreEqual(1, host.spendCalls); Assert.AreEqual(0, host.trainCalls); Assert.AreEqual(0, sim.S.totalSpent);
        }

        [TestCase("longtext")][TestCase("crosssentence")][TestCase("poems")]
        public void AllNewTextDesksAreEligibleForLocalJudging(string desk)
        { Assert.IsTrue(XgSim.IsBrainDesk(desk)); }

        [TestCase("mnist")][TestCase("cifar")][TestCase("meme")][TestCase("go")]
        public void UnsupportedDesksNeverClaimRealTextModelInference(string desk)
        { Assert.IsFalse(XgSim.IsBrainDesk(desk)); }
    }
}
