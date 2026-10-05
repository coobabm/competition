using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The long-sentence sample: real diagnostic groups, real mistakes, fair trials, an independent exam, hints.</summary>
    public sealed class XgDiagnosticsTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6, vram = 1e5;
            public double Money => money; public double Compute => 2; public double VramMB => vram; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        /// <summary>A stage-3 save standing at the long-sentence wall, training a plain loop on it.</summary>
        static XgSim AtLongWall(out Host host, int salt = 7)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchemaWalls, stage = 3, stageVision = 3, stageSequence = 3, dataSalt = salt };
            var sim = new XgSim(state);
            foreach (var n in XgCatalog.Nodes)
                if (n.stage <= 3 && n.tree != "label" && n.kind != XgNodeKind.Secret && n.kind != XgNodeKind.Project && n.kind != XgNodeKind.Label && !sim.Has(n.id))
                    sim.S.unlocked.Add(n.id);
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 300 });
            sim = new XgSim(sim.S) { GoldChance = 0, PracticeStrict = false };
            host = new Host();
            var s = XgTrack.Sequence;
            for (int i = 0; i < XgSim.WallPracticeEpochs + 2; i++) sim.TrainEpoch(s, host);
            Assert.IsTrue(sim.WallSeen("length"));
            sim.SetDataset(s, "longtext"); sim.SetArch(s, "rnn"); sim.SetDepth(s, 3, host); sim.SetWidth(s, 3, host); sim.SetClip(s, true); sim.SetLr(s, 2);
            for (int i = 0; i < 30; i++) sim.TrainEpoch(s, host);
            return sim;
        }

        static string BoardStamp(XgBoardState s)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            var b = new System.Text.StringBuilder();
            b.Append(s.nextId).Append('|').Append(s.cards).Append('|').Append(s.superposed).Append('|').Append(s.evicted).Append('|').Append(s.created);
            foreach (var c in s.concepts)
                b.Append('\n').Append(c.id).Append('|').Append(c.region).Append('|').Append(c.key).Append('|').Append(c.alt).Append('|').Append(c.layer)
                 .Append('|').Append(c.w.ToString("R", ci)).Append('|').Append(c.s.ToString("R", ci)).Append('|').Append(c.seen).Append('|').Append(c.seed).Append('|').Append(c.pinned);
            if (s.links != null) foreach (var l in s.links) b.Append('\n').Append(l.a).Append('|').Append(l.b).Append('|').Append(l.c.ToString("R", ci));
            return b.ToString();
        }

        [Test]
        public void DraftDepthTrialDoesNotEraseTheLiveModel()
        {
            var sim = AtLongWall(out var host);
            var before = BoardStamp(sim.Board.S); var cursor = sim.S.sequence.cursor;
            double steps = sim.S.sequence.steps, money = host.money;
            var draft = sim.BeginTrialDraft(XgTrack.Sequence);
            draft.proposal.depth = 2;
            sim.Trial(draft, 600);
            Assert.That(BoardStamp(sim.Board.S), Is.EqualTo(before));
            Assert.That(sim.S.sequence.cursor, Is.EqualTo(cursor));
            Assert.That(sim.S.sequence.depth, Is.EqualTo(3));
            Assert.That(sim.S.sequence.steps, Is.EqualTo(steps));
            Assert.That(host.money, Is.EqualTo(money));
            Assert.That(sim.IsTrialCurrent(draft), Is.True, "discarding this draft needs no rollback");
        }

        [Test]
        public void DraftPendingDepthBaselineRebuildsLikeTheNextRealEpoch()
        {
            var sim = AtLongWall(out var host); var run = sim.S.sequence;
            Assert.That(run.formal.depth, Is.EqualTo(3));
            Assert.That(sim.SetDepth(XgTrack.Sequence, 2, host), Is.True);
            var before = BoardStamp(sim.Board.S);
            var expected = new XgBoard(sim.Board.S.Clone());
            expected.Reinitialise(XgSim.RegionOf(run.dataset));
            double afterReset = sim.Diagnose(run.dataset, sim.Knobs(run), expected).overall;
            double beforeReset = sim.Diagnose(run.dataset, sim.Knobs(run)).overall;
            Assert.That(afterReset, Is.Not.EqualTo(beforeReset), "fixture must distinguish learned and reset boards");
            var draft = sim.BeginTrialDraft(XgTrack.Sequence);
            var result = sim.Trial(draft, 0);
            Assert.That(result.now.overall, Is.EqualTo(beforeReset), "now is before the pending rebuild");
            Assert.That(result.baseline.overall, Is.EqualTo(afterReset), "keeping pending depth 2 rebuilds at next epoch");
            Assert.That(result.proposal, Is.SameAs(result.baseline));
            Assert.That(BoardStamp(sim.Board.S), Is.EqualTo(before));
        }

        [TestCase(false)][TestCase(true)]
        public void DraftRestoringFormalDepthDoesNotRebuildTheProposal(bool changeWidth)
        {
            var sim = AtLongWall(out var host); var run = sim.S.sequence;
            Assert.That(sim.SetDepth(XgTrack.Sequence, 2, host), Is.True);
            var draft = sim.BeginTrialDraft(XgTrack.Sequence);
            draft.proposal.depth = run.formal.depth;
            if (changeWidth) draft.proposal.width = run.width - 1;
            var expectedKnobs = sim.Knobs(run).Copy(); expectedKnobs.depth = draft.proposal.depth;
            expectedKnobs.width = XgCatalog.Widths[draft.proposal.width] / 2;
            double expected = sim.Diagnose(run.dataset, expectedKnobs).overall;
            Assert.That(expected, Is.GreaterThan(.5), "fixture contains useful learned concepts");
            var before = BoardStamp(sim.Board.S);
            var result = sim.Trial(draft, 0);
            Assert.That(result.proposal.overall, Is.EqualTo(expected), "returning to the trained depth cancels pending reset; width alone preserves concepts");
            Assert.That(sim.TryApplyTrial(draft, host, out var reason), Is.True, reason);
            Assert.That(BoardStamp(sim.Board.S), Is.EqualTo(before));
            Assert.That(run.depth, Is.EqualTo(run.formal.depth));
        }

        [Test]
        public void DraftPendingWidthOnlyPreservesKnowledgeOnBothCopies()
        {
            var sim = AtLongWall(out var host); var run = sim.S.sequence;
            Assert.That(sim.SetWidth(XgTrack.Sequence, run.width - 1, host), Is.True);
            var draft = sim.BeginTrialDraft(XgTrack.Sequence); draft.proposal.width--;
            var expectedKnobs = sim.Knobs(run).Copy(); expectedKnobs.width = XgCatalog.Widths[draft.proposal.width] / 2;
            var result = sim.Trial(draft, 0);
            Assert.That(result.baseline.overall, Is.EqualTo(sim.Diagnose(run.dataset, sim.Knobs(run)).overall));
            Assert.That(result.proposal.overall, Is.EqualTo(sim.Diagnose(run.dataset, expectedKnobs).overall));
        }

        [Test]
        public void DraftTrialsRepeatFromFrozenEffectiveKnobsAndCards()
        {
            var sim = AtLongWall(out _);
            sim.S.unlocked.Remove("relu"); sim.S.sequence.act = -1;
            var draft = sim.BeginTrialDraft(XgTrack.Sequence); draft.proposal.arch = "lstm";
            var a = sim.Trial(draft);
            sim.S.unlocked.Add("relu"); sim.Today++; sim.S.dataSalt++;
            Assert.That(sim.IsTrialCurrent(draft), Is.False);
            var b = sim.Trial(draft);
            Assert.That(b.baseline.overall, Is.EqualTo(a.baseline.overall));
            Assert.That(b.proposal.overall, Is.EqualTo(a.proposal.overall));
            Assert.That(b.baseline.Group("far").right, Is.EqualTo(a.baseline.Group("far").right));
        }

        [TestCase("weight")][TestCase("pin")][TestCase("link")][TestCase("idle")]
        [TestCase("warmup")][TestCase("labels")][TestCase("salt")][TestCase("date")][TestCase("dataset")][TestCase("global")][TestCase("noise")][TestCase("reinitialise")]
        public void DraftDetectsChangesBeyondEpochCounters(string mutation)
        {
            var sim = AtLongWall(out var host); var draft = sim.BeginTrialDraft(XgTrack.Sequence);
            switch (mutation)
            {
                case "weight": sim.Board.S.concepts[sim.Board.S.concepts.Count - 1].w += .01; break;
                case "pin": sim.Board.S.concepts[0].pinned = !sim.Board.S.concepts[0].pinned; break;
                case "link": sim.Board.S.links.Add(new XgLink { a = 1, b = 2, c = .3 }); break;
                case "idle": sim.Board.Idle(10); break;
                case "warmup": sim.S.sequence.warmup = !sim.S.sequence.warmup; break;
                case "labels": sim.S.labels.Add(new XgLabelCount { dataset = "longtext", count = 1 }); break;
                case "salt": sim.S.dataSalt++; break;
                case "date": sim.Today++; break;
                case "dataset": sim.S.sequence.dataset = "spam"; break;
                case "global": if (!sim.S.unlocked.Remove("bias")) sim.S.unlocked.Add("bias"); break;
                case "noise": sim.S.noise.Add(new XgLabelCount { dataset = "longtext", count = 2 }); break;
                case "reinitialise": sim.Board.Reinitialise(XgSim.RegionOf(sim.S.sequence.dataset)); break;
            }
            Assert.That(sim.IsTrialCurrent(draft), Is.False);
            Assert.That(sim.TryApplyTrial(draft, host, out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        [Test]
        public void DraftRejectsTrainingOrAnotherOwner()
        {
            var sim = AtLongWall(out var host); var draft = sim.BeginTrialDraft(XgTrack.Sequence);
            var other = new XgSim(sim.S);
            Assert.That(other.TryApplyTrial(draft, host, out _), Is.False);
            sim.S.sequence.running = true;
            Assert.That(sim.TryApplyTrial(draft, host, out _), Is.False);
            sim.S.sequence.running = false;
            sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.That(sim.TryApplyTrial(draft, host, out _), Is.False);
        }

        [Test]
        public void DraftInvalidCombinationDoesNotPartiallyApply()
        {
            var sim = AtLongWall(out var host); var draft = sim.BeginTrialDraft(XgTrack.Sequence);
            var before = BoardStamp(sim.Board.S); var run = sim.S.sequence;
            var arch = run.arch; int depth = run.depth, width = run.width;
            draft.proposal.arch = "lstm"; draft.proposal.depth = 2;
            host.vram = 1;
            Assert.That(sim.TryApplyTrial(draft, host, out _), Is.False);
            Assert.That(run.arch, Is.EqualTo(arch)); Assert.That(run.depth, Is.EqualTo(depth)); Assert.That(run.width, Is.EqualTo(width));
            Assert.That(BoardStamp(sim.Board.S), Is.EqualTo(before));
        }

        [Test]
        public void DraftCombinedShapeUsesFinalVramAndResetsOnlyAtRealTraining()
        {
            var sim = AtLongWall(out var host); var draft = sim.BeginTrialDraft(XgTrack.Sequence);
            var run = sim.S.sequence; var before = BoardStamp(sim.Board.S);
            draft.proposal.depth = 1; draft.proposal.width = sim.WidthCap(XgTrack.Sequence);
            host.vram = XgSim.VramNeedMB(new XgRun { arch = run.arch, depth = 1, width = draft.proposal.width });
            Assert.That(sim.TryApplyTrial(draft, host, out var reason), Is.True, reason);
            Assert.That(run.depth, Is.EqualTo(1)); Assert.That(run.width, Is.EqualTo(draft.proposal.width));
            Assert.That(BoardStamp(sim.Board.S), Is.EqualTo(before), "preserve existing deferred shape semantics");
            Assert.That(sim.TryApplyTrial(draft, host, out _), Is.False, "a committed draft is one-shot");
            sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.That(run.formal.depth, Is.EqualTo(1));
        }

        [Test]
        public void ApplyingADraftKeepsTheAutomaticActivationUnlessItWasChanged()
        {
            var sim = AtLongWall(out var host); var run = sim.S.sequence;
            sim.S.unlocked.Remove("relu"); run.act = -1;
            var draft = sim.BeginTrialDraft(XgTrack.Sequence); // unchanged proposal
            Assert.That(sim.TryApplyTrial(draft, host, out var reason), Is.True, reason);
            Assert.That(run.act, Is.EqualTo(-1), "an untouched activation stays automatic");
            sim.S.unlocked.Add("relu");
            Assert.That(sim.EffectiveActivation(run), Is.EqualTo(2), "a later ReLU purchase still upgrades it");
            var second = sim.BeginTrialDraft(XgTrack.Sequence); second.proposal.act = 0;
            Assert.That(sim.TryApplyTrial(second, host, out reason), Is.True, reason);
            Assert.That(run.act, Is.EqualTo(0), "an explicit choice is kept");
        }

        [Test]
        public void ADraftThatDoesNotGrowTheShapeAppliesEvenWhenVramShrank()
        {
            var sim = AtLongWall(out var host); var run = sim.S.sequence;
            int depth = run.depth;
            host.vram = 1;
            var draft = sim.BeginTrialDraft(XgTrack.Sequence); // unchanged proposal
            Assert.That(sim.TryApplyTrial(draft, host, out var reason), Is.True, reason);
            var shrink = sim.BeginTrialDraft(XgTrack.Sequence); shrink.proposal.depth = 1;
            Assert.That(sim.TryApplyTrial(shrink, host, out reason), Is.True, reason);
            var grow = sim.BeginTrialDraft(XgTrack.Sequence); grow.proposal.depth = depth;
            Assert.That(sim.TryApplyTrial(grow, host, out reason), Is.False, "growing past the cards is still refused");
            Assert.That(run.depth, Is.EqualTo(1));
        }

        [TestCase("arch")][TestCase("act")][TestCase("depth")][TestCase("width")][TestCase("lr")][TestCase("locked")]
        public void InvalidDraftParameterLeavesAllLiveKnobsUntouched(string parameter)
        {
            var sim = AtLongWall(out var host); var draft = sim.BeginTrialDraft(XgTrack.Sequence);
            var before = BoardStamp(sim.Board.S); var run = sim.S.sequence;
            string arch = run.arch; int act = run.act, depth = run.depth, width = run.width, lr = run.lr;
            draft.proposal.arch = "lstm";
            switch (parameter)
            {
                case "arch": draft.proposal.arch = "missing"; break;
                case "act": draft.proposal.act = 99; break;
                case "depth": draft.proposal.depth = sim.DepthCap(XgTrack.Sequence) + 1; break;
                case "width": draft.proposal.width = 99; break;
                case "lr": draft.proposal.lr = -1; break;
                case "locked": draft.proposal.position = true; Assert.That(sim.PositionOwned, Is.False); break;
            }
            Assert.That(sim.TryApplyTrial(draft, host, out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty); Assert.That(BoardStamp(sim.Board.S), Is.EqualTo(before));
            Assert.That(run.arch, Is.EqualTo(arch)); Assert.That(run.act, Is.EqualTo(act)); Assert.That(run.depth, Is.EqualTo(depth));
            Assert.That(run.width, Is.EqualTo(width)); Assert.That(run.lr, Is.EqualTo(lr));
        }

        [Test]
        public void DraftWarmupAppearsInChangesAndTrialBudgetIsBounded()
        {
            var sim = AtLongWall(out _); sim.S.unlocked.Add("warmup");
            var draft = sim.BeginTrialDraft(XgTrack.Sequence); draft.proposal.warmup = !draft.proposal.warmup;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var result = sim.Trial(draft);
            watch.Stop();
            TestContext.WriteLine("Frozen 600-card trial: " + watch.Elapsed.TotalMilliseconds.ToString("0.0") + " ms");
            Assert.That(result.changes.Count, Is.EqualTo(1)); Assert.That(result.sameSettings, Is.False);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sim.Trial(draft, -1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sim.Trial(draft, XgSim.MaxTrialCards + 1));
            Assert.That(sim.S.trials, Is.Zero, "draft experiments do not mutate persistent counters");
        }

        [Test]
        public void ChangingTheDepthLeavesTheRealModelAloneUntilItTrains()
        {
            var sim = AtLongWall(out var host);
            var s = XgTrack.Sequence; var run = sim.S.sequence;
            string region = XgSim.RegionOf(run.dataset);
            int concepts = sim.Board.Count(region), formal = run.depth;
            double steps = run.steps;
            Assert.Greater(concepts, 0);
            Assert.IsTrue(sim.SetDepth(s, formal > 1 ? formal - 1 : formal + 1, host));
            Assert.AreEqual(concepts, sim.Board.Count(region), "a changed knob is only a proposal");
            Assert.AreEqual(steps, run.steps);
            var trial = sim.Trial(s);
            Assert.IsFalse(trial.sameSettings);
            Assert.AreEqual(concepts, sim.Board.Count(region), "a trial trains copies");
            Assert.IsTrue(sim.SetDepth(s, formal, host));
            Assert.AreEqual(concepts, sim.Board.Count(region), "setting it back loses nothing");
            sim.TrainEpoch(s, host);
            Assert.Greater(run.steps, steps, "no reshape penalty for a shape that never changed");
        }

        [Test]
        public void TheNextRealEpochRebuildsTheNetworkAtTheNewDepth()
        {
            var sim = AtLongWall(out var host);
            var s = XgTrack.Sequence; var run = sim.S.sequence;
            string region = XgSim.RegionOf(run.dataset);
            var ids = sim.S.board.concepts.FindAll(c => c.region == region).ConvertAll(c => c.id);
            int formal = run.depth;
            Assert.IsTrue(sim.SetDepth(s, formal > 1 ? formal - 1 : formal + 1, host));
            sim.TrainEpoch(s, host);
            Assert.IsFalse(sim.S.board.concepts.Exists(c => c.region == region && ids.Contains(c.id)), "the real epoch starts a fresh network");
            Assert.AreEqual(run.depth, run.formal.depth);
        }

        [Test]
        public void DiagnosticGroupsComeFromTheCardsRealDistance()
        {
            var sim = AtLongWall(out _);
            var d = sim.Diagnose("longtext", sim.Knobs(sim.S.sequence));
            int total = 0; foreach (var g in d.groups) total += g.total;
            Assert.That(total, Is.EqualTo(sim.TestSet("longtext").Count));
            Assert.That(d.groups.ConvertAll(g => g.id), Is.EqualTo(new[] { "near", "mid", "far" }));
            foreach (var card in sim.TestSet("longtext")) Assert.That(XgSim.GroupOf(card).id, Is.EqualTo(card.distance <= 4 ? "near" : card.distance <= 9 ? "mid" : "far"));
            foreach (var m in d.mistakes)
            {
                Assert.That(m.answer, Is.Not.EqualTo(m.truth), "a mistake is a real wrong answer");
                Assert.That(m.text, Does.Contain("【"), "the sample shows the actual sentence");
                Assert.That(m.group, Is.EqualTo(XgSim.GroupOf(new XgBoardCard { distance = m.distance }).id));
            }
            Assert.That(d.mistakes.Count, Is.InRange(1, XgSim.MistakesShown));
        }

        [Test]
        public void APlainLoopShowsTheDistanceProblem()
        {
            var sim = AtLongWall(out _);
            var d = sim.Diagnose("longtext", sim.Knobs(sim.S.sequence));
            Assert.That(d.Group("far").Rate, Is.LessThan(d.Group("near").Rate - .15), "near " + d.Group("near").Rate + " far " + d.Group("far").Rate);
            Assert.That(d.mainError, Does.Contain("远"));
        }

        [Test]
        public void TrialsAreFairAndChangeNothingReal()
        {
            var sim = AtLongWall(out var host);
            var s = XgTrack.Sequence;
            int concepts = sim.Board.S.concepts.Count; long cards = sim.Board.S.cards, cursor = sim.S.sequence.cursor;
            double money = host.money, firstWeight = sim.Board.S.concepts[0].w; int epochs = sim.S.epochs, stage = sim.S.stage;
            var same = sim.Trial(s);
            Assert.IsTrue(same.sameSettings); Assert.That(same.proposal, Is.SameAs(same.baseline));

            sim.SetArch(s, "lstm");
            var a = sim.Trial(s); var b = sim.Trial(s);
            Assert.That(a.changes.Count, Is.EqualTo(1), string.Join(",", a.changes));
            Assert.That(a.proposal.Group("far").right, Is.EqualTo(b.proposal.Group("far").right), "same snapshot, same cards, same budget: same result");
            Assert.That(a.baseline.Group("far").right, Is.EqualTo(b.baseline.Group("far").right));
            Assert.That(a.proposal.Group("far").Rate, Is.GreaterThan(a.baseline.Group("far").Rate + .1), "gated memory fixes the far clues in the trial");

            Assert.That(sim.Board.S.concepts.Count, Is.EqualTo(concepts)); Assert.That(sim.Board.S.cards, Is.EqualTo(cards));
            Assert.That(sim.Board.S.concepts[0].w, Is.EqualTo(firstWeight)); Assert.That(sim.S.sequence.cursor, Is.EqualTo(cursor));
            Assert.That(host.money, Is.EqualTo(money)); Assert.That(sim.S.epochs, Is.EqualTo(epochs)); Assert.That(sim.S.stage, Is.EqualTo(stage));
        }

        [Test]
        public void TrialsCompareAgainstTheSettingsTheModelReallyTrainedWith()
        {
            var sim = AtLongWall(out var host);
            var s = XgTrack.Sequence;
            sim.SetArch(s, "lstm"); sim.SetLr(s, 3);
            var t = sim.Trial(s);
            Assert.That(t.changes.Count, Is.EqualTo(2));
            sim.TrainEpoch(s, host);
            Assert.That(sim.Trial(s).sameSettings, Is.True, "after a real epoch the new settings are the baseline");
        }

        [Test]
        public void TheExamIsIndependentFarAndUnseen()
        {
            var sim = AtLongWall(out _);
            var check = XgSim.WallFor(3).checks[0];
            var exam = sim.ExamSet("longtext", check);
            Assert.That(exam.Count, Is.EqualTo(XgBoardData.TestSize));
            foreach (var c in exam) Assert.That(c.distance, Is.GreaterThanOrEqualTo(10));
            var seen = new HashSet<int>();
            foreach (var c in sim.TestSet("longtext")) seen.Add(c.seed);
            for (int i = 0; i < XgBoardData.PoolLimit; i++) seen.Add(XgBoardData.Seed("longtext", i, XgBoardData.Use.Train, sim.S.dataSalt));
            foreach (var c in exam) Assert.That(seen.Contains(c.seed), Is.False, "exam cards are never trained on or shown");
        }

        [Test]
        public void PassingIsJudgedByBehaviourNotByTheReferenceSetting()
        {
            var sim = AtLongWall(out var host);
            var s = XgTrack.Sequence;
            // Not the reference (that asks for lr ≤ 0.01): a gated loop at 0.1 still has to pass on behaviour.
            sim.SetArch(s, "lstm"); sim.SetLr(s, 2);
            Assert.IsFalse(XgSim.WallFor(3).checks[0].golden(sim, sim.S.sequence, sim.Knobs(sim.S.sequence)));
            for (int i = 0; i < 120 && sim.S.stage == 3; i++) sim.TrainEpoch(s, host);
            Assert.That(sim.S.stage, Is.EqualTo(4));
        }

        [Test]
        public void HintsGoFromPhenomenonToExperimentAndDoNotCostTheInsight()
        {
            var sim = AtLongWall(out var host);
            Assert.That(sim.HintLevel("length"), Is.Zero);
            for (int i = 0; i < 5; i++) sim.RevealHint("length");
            Assert.That(sim.HintLevel("length"), Is.EqualTo(3), "the fourth step is the reference itself, behind the secret");
            Assert.That(sim.HintText("length", 1), Does.Contain("远"));
            var s = XgTrack.Sequence;
            sim.SetArch(s, "lstm");
            for (int i = 0; i < 120 && sim.S.stage == 3; i++) sim.TrainEpoch(s, host);
            Assert.That(sim.S.insights, Does.Contain("length"), "free hints are help, not the answer");
        }
    }
}
