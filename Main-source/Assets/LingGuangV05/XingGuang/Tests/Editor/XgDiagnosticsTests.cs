using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The long-sentence sample: real diagnostic groups, real mistakes, fair trials, an independent exam, hints.</summary>
    public sealed class XgDiagnosticsTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6;
            public double Money => money; public double Compute => 2; public double VramMB => 1e5; public string Blocker => null;
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
