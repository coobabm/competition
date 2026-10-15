using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>双曲线 (XgSim.Holdout.cs): 练过的题 against 新题, and the gap that means 它在背答案.</summary>
    public sealed class XgHoldoutTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e7, compute = 1, vram = 1e5;
            public double Compute => compute;
            public double VramMB => vram;
            public double Money => money;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        static XgSim Lab(double samples, params string[] owned)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 3, stageVision = 3, stageSequence = 3, trainingVersion = XgSim.TrainingSchemaAuto };
            state.unlocked.Clear();
            var sim = new XgSim(state) { GoldChance = 0, UseBoard = false };
            foreach (var id in owned) sim.S.unlocked.Add(id);
            var d = XgCatalog.Dataset("spam");
            if (XgCatalog.Desk("spam") != null && !sim.S.desksOpen.Contains("spam")) sim.S.desksOpen.Add("spam");
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = samples });
            sim.SetDataset(d.track, "spam");
            return sim;
        }

        /// <summary>A sequence run of this width that has trained a while and stands at the same 新题 accuracy.</summary>
        static XgRun Trained(XgSim sim, int width)
        {
            var run = sim.S.sequence;
            run.arch = "mlp"; run.depth = 2; run.width = width;
            run.epoch = 40; run.steps = 4000;
            var d = XgCatalog.Dataset(run.dataset);
            run.valAcc = 1 - d.chanceError + .4 * (d.chanceError - d.floorError);
            sim.Evaluate(run);
            run.valAcc = 1 - d.chanceError + .4 * (d.chanceError - d.floorError);
            return run;
        }

        [Test]
        public void TheGapGrowsWithParametersPerSample()
        {
            var sim = Lab(400);
            double small = sim.HoldoutGap(Trained(sim, 0)), smallShare = sim.MemorizeShare(sim.S.sequence);
            double big = sim.HoldoutGap(Trained(sim, 4)), bigShare = sim.MemorizeShare(sim.S.sequence);
            Assert.Greater(bigShare, smallShare, "more parameters per sample, more learned by heart");
            Assert.Greater(big, small, "the lines part further for the bigger model");
            Assert.GreaterOrEqual(sim.PracticeAccuracy(sim.S.sequence), sim.S.sequence.valAcc, "practice cards are never worse than new ones");
            Assert.IsTrue(sim.Memorizing(sim.S.sequence), "a big model on 400 samples is 背答案 (gap " + big + ")");
        }

        [Test]
        public void MoreDataOrDropoutShrinkTheGap()
        {
            var scarce = Lab(400);
            double scarceGap = scarce.HoldoutGap(Trained(scarce, 4));
            var plenty = Lab(200000);
            double plentyGap = plenty.HoldoutGap(Trained(plenty, 4));
            Assert.Less(plentyGap, scarceGap, "more data: less to learn by heart");
            Assert.IsFalse(plenty.Memorizing(plenty.S.sequence), "plenty of data: the lines stay together");

            var dropout = Lab(400, "dropout");
            double dropoutGap = dropout.HoldoutGap(Trained(dropout, 4));
            Assert.Less(dropoutGap, scarceGap, "Dropout: less to learn by heart");
            Assert.AreEqual(scarce.MemorizeShare(scarce.S.sequence) * .5, dropout.MemorizeShare(dropout.S.sequence), 1e-9, "Dropout halves it, as in the drop risk");
        }

        [Test]
        public void TheShareIsTheDropRisksOverfittingTerm()
        {
            var sim = Lab(400);
            var run = Trained(sim, 3);
            Assert.AreEqual(sim.DropRisk(run).overfit / .24, sim.MemorizeShare(run), 1e-9);
        }

        [Test]
        public void AnUntrainedRunHasNoGap()
        {
            var sim = Lab(400);
            var run = sim.S.sequence;
            run.epoch = 0; run.steps = 0;
            sim.Evaluate(run);
            Assert.AreEqual(0, sim.HoldoutGap(run), 1e-9);
            Assert.IsFalse(sim.Memorizing(run));
        }

        [Test]
        public void TheSeriesFollowsTheCurveRoundByRound()
        {
            var sim = Lab(400);
            var host = new Host();
            var run = sim.S.sequence;
            for (int i = 0; i < 12; i++) Assert.IsNotNull(sim.TrainEpoch(XgTrack.Sequence, host, false), "round " + i);
            var practice = sim.PracticeSeries(run);
            Assert.AreEqual(run.histVal.Count, practice.Count, "one 练过的题 point per 新题 point");
            for (int i = 0; i < practice.Count; i++) Assert.GreaterOrEqual(practice[i], run.histVal[i] - 1e-6f, "point " + i);
            Assert.AreEqual((float)sim.PracticeAccuracy(run), practice[practice.Count - 1], 1e-6f, "the last point is now");

            // The curve is capped: the series keeps the newest points with it.
            for (int i = 0; i < XgSim.HistoryLength + 5; i++) sim.TrainEpoch(XgTrack.Sequence, host, false);
            Assert.AreEqual(XgSim.HistoryLength, run.histVal.Count);
            Assert.AreEqual(XgSim.HistoryLength, sim.PracticeSeries(run).Count);

            // A new dataset clears the curve; the series starts again with it.
            run.histTrain.Clear(); run.histVal.Clear();
            sim.TrainEpoch(XgTrack.Sequence, host, false);
            Assert.AreEqual(1, sim.PracticeSeries(run).Count);
        }

        [Test]
        public void AnOlderSaveFillsFromTheTrainingCurve()
        {
            var sim = Lab(400);
            var run = sim.S.sequence;
            run.histPractice = null;
            run.histTrain.Clear(); run.histVal.Clear();
            run.histTrain.AddRange(new[] { .7f, .8f, .9f });
            run.histVal.AddRange(new[] { .6f, .75f, .95f });
            var series = sim.PracticeSeries(run);
            CollectionAssert.AreEqual(new[] { .7f, .8f, .95f }, series, "the old training accuracy, never under 新题");
        }
    }
}
