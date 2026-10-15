using System.Diagnostics;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// The crawler learning to walk (prototype): a clumsy start that falls, a learner that only keeps what scores better,
    /// and, after 80 generations, a gait whose neighbouring legs take turns (the alternating tetrapod or a wave gait).
    /// </summary>
    public sealed class XgGaitTests
    {
        static readonly int[] Seeds = { 1, 2, 3, 4, 5 };
        const int Generations = 80;

        /// <summary>The alternating tetrapod (L1 R2 L3 R4 against R1 L2 R3 L4); lag puts each pair that much of a cycle behind the pair in front.</summary>
        static XgGaitGenes Tetrapod(double duty, double stride, double freq, double lag = 0)
        {
            var g = new XgGaitGenes { duty = duty, stride = stride, freq = freq };
            for (int i = 0; i < XgGaitGenes.Legs; i++)
            {
                int pair = i / 2, side = i % 2;
                double p = (pair + side) % 2 * .5 - pair * lag;
                g.phase[i] = p - System.Math.Floor(p);
            }
            return g;
        }

        /// <summary>Walks a gait for one trial and fails if two planted feet on the same side ever stand closer than the gap or cross.</summary>
        static void AssertNoPlantedNeighboursCross(XgGaitGenes g, string what)
        {
            var w = new XgGaitWalk(g);
            int steps = (int)System.Math.Round(XgGaitLearner.TrialSeconds / XgGaitLearner.Dt);
            for (int k = 0; k < steps; k++)
            {
                w.Step(XgGaitLearner.Dt);
                for (int i = 0; i + 2 < XgGaitGenes.Legs; i++)
                {
                    var front = w.legs[i]; var back = w.legs[i + 2];
                    if (front.stance && back.stance)
                        Assert.That(front.footX - back.footX, Is.GreaterThanOrEqualTo(XgGaitWalk.FootGap - 1e-9), what + ": " + i + " and " + (i + 2) + " at " + w.time.ToString("0.00") + " s");
                }
            }
        }

        [Test]
        public void TheSameGenesWalkTheSameWay()
        {
            var a = XgGaitLearner.Score(XgGaitGenes.Clumsy(3));
            var b = XgGaitLearner.Score(XgGaitGenes.Clumsy(3));
            Assert.That(a.distance, Is.EqualTo(b.distance));
            Assert.That(a.falls, Is.EqualTo(b.falls));
            Assert.That(a.fitness, Is.EqualTo(b.fitness));
        }

        [Test]
        public void TheSameSeedLearnsTheSameGait()
        {
            var a = new XgGaitLearner(4);
            var b = new XgGaitLearner(4);
            for (int i = 0; i < 30; i++) { a.Generate(); b.Generate(); }
            Assert.That(a.BestScore.fitness, Is.EqualTo(b.BestScore.fitness));
            for (int i = 0; i < XgGaitGenes.Legs; i++) Assert.That(a.Best.phase[i], Is.EqualTo(b.Best.phase[i]));
            Assert.That(a.Best.stride, Is.EqualTo(b.Best.stride));
        }

        [Test]
        public void TheClumsyStartFalls()
        {
            foreach (int seed in Seeds)
            {
                var s = XgGaitLearner.Score(XgGaitGenes.Clumsy(seed));
                Assert.That(s.falls, Is.GreaterThanOrEqualTo(3), "seed " + seed);
            }
        }

        [Test]
        public void AFootCannotComeDownOnOrPastItsPlantedNeighbour()
        {
            // A long stride with neighbours planted together would cross; the late foot hovers instead.
            var g = Tetrapod(.75, 40, 2);
            AssertNoPlantedNeighboursCross(g, "long stride");
            var w = new XgGaitWalk(g);
            for (int k = 0; k < 250; k++) w.Step(XgGaitLearner.Dt);
            Assert.That(w.hoverTime, Is.GreaterThan(0));
            Assert.That(XgGaitLearner.Score(g).hover, Is.GreaterThan(0));
            foreach (int seed in Seeds) AssertNoPlantedNeighboursCross(XgGaitGenes.Clumsy(seed), "clumsy " + seed);
        }

        [Test]
        public void TheAlternatingTetrapodWithShortStepsAndALittleLagIsAGoodGait()
        {
            // The tetrapod still works under the no-crossing rule: a stride a little past the leg spacing, each pair a
            // fortieth of a cycle behind the one in front, and no foot ever has to wait.
            var g = Tetrapod(.52, 26, 4, .025);
            var s = XgGaitLearner.Score(g);
            Assert.That(s.falls, Is.EqualTo(0));
            Assert.That(s.hover, Is.EqualTo(0));
            Assert.That(XgGaitLearner.Tetrapod(g), Is.GreaterThan(.95));
            AssertNoPlantedNeighboursCross(g, "tetrapod");
            foreach (int seed in Seeds)
            {
                var learner = new XgGaitLearner(seed);
                for (int i = 0; i < Generations; i++) learner.Generate();
                Assert.That(s.fitness, Is.GreaterThan(learner.BestScore.fitness), "seed " + seed + " found better than the tetrapod");
            }
        }

        [Test]
        public void TheAlternatingTetrapodWalksWithoutFallingOrWaddling()
        {
            var g = Tetrapod(.65, 20, 2.8);
            var s = XgGaitLearner.Score(g);
            Assert.That(s.falls, Is.EqualTo(0));
            Assert.That(s.wobble, Is.LessThan(.001));
            Assert.That(XgGaitLearner.Tetrapod(g), Is.GreaterThan(.95));
            Assert.That(XgGaitLearner.Wave(g), Is.GreaterThan(.95));
            Assert.That(s.distance, Is.GreaterThan(XgGaitLearner.Score(XgGaitGenes.Clumsy(1)).distance * 3));
            // Neighbours never share the air in a tetrapod.
            var w = new XgGaitWalk(g);
            for (int i = 0; i < 200; i++) { w.Step(XgGaitLearner.Dt); Assert.That(w.Clash(), Is.EqualTo(0)); }
        }

        [Test]
        public void ScatteredPhasesWaddleAndScoreLessThanTheSameStepsInTurn()
        {
            var scattered = XgGaitGenes.Clumsy(2);
            scattered.duty = .65; scattered.stride = 20; scattered.freq = 2.8;
            var a = XgGaitLearner.Score(scattered);
            var b = XgGaitLearner.Score(Tetrapod(.65, 20, 2.8));
            Assert.That(a.wobble, Is.GreaterThan(b.wobble));
            Assert.That(a.fitness, Is.LessThan(b.fitness));
        }

        [Test]
        public void ItLearnsToWalkStopsFallingAndItsLegsTakeTurns()
        {
            double tetStart = 0, tetEnd = 0, waveStart = 0, waveEnd = 0, cleanStart = 0, cleanEnd = 0, strides = 0;
            foreach (int seed in Seeds)
            {
                var learner = new XgGaitLearner(seed);
                var start = learner.BestScore;
                double t0 = XgGaitLearner.Tetrapod(learner.Best), w0 = XgGaitLearner.Wave(learner.Best);
                for (int i = 0; i < Generations; i++) learner.Generate();
                var end = learner.BestScore; var b = learner.Best;
                double t1 = XgGaitLearner.Tetrapod(b), w1 = XgGaitLearner.Wave(b);
                tetStart += t0; tetEnd += t1; waveStart += w0; waveEnd += w1; strides += b.stride;
                cleanStart += System.Math.Max(t0, w0); cleanEnd += System.Math.Max(t1, w1);
                TestContext.Progress.WriteLine("seed " + seed + ": " + start.distance.ToString("0") + " / " + start.falls + " falls, tetrapod " + t0.ToString("0.00") + " wave " + w0.ToString("0.00")
                    + " → " + end.distance.ToString("0") + " / " + end.falls + " falls, tetrapod " + t1.ToString("0.00") + " wave " + w1.ToString("0.00")
                    + ", duty " + b.duty.ToString("0.00") + " stride " + b.stride.ToString("0") + " freq " + b.freq.ToString("0.00"));

                Assert.That(end.falls, Is.EqualTo(0), "seed " + seed + " still falls");
                Assert.That(end.distance, Is.GreaterThan(start.distance * 5), "seed " + seed);
                Assert.That(b.stride, Is.LessThanOrEqualTo(XgGaitWalk.StrideReach + 1e-9), "seed " + seed + " overreaches");
                Assert.That(b.stride, Is.InRange(15, 36), "seed " + seed + " stride");
                Assert.That(end.hover, Is.LessThan(1), "seed " + seed + " keeps a foot waiting on its neighbour");
                AssertNoPlantedNeighboursCross(b, "seed " + seed);
                Assert.That(learner.History.Count, Is.EqualTo(Generations + 1));
                for (int i = 1; i < learner.History.Count; i++)
                    Assert.That(learner.History[i].fitness, Is.GreaterThanOrEqualTo(learner.History[i - 1].fitness), "seed " + seed + " generation " + i);
            }
            int n = Seeds.Length;
            tetStart /= n; tetEnd /= n; waveStart /= n; waveEnd /= n; cleanStart /= n; cleanEnd /= n; strides /= n;
            TestContext.Progress.WriteLine("mean tetrapod " + tetStart.ToString("0.00") + " → " + tetEnd.ToString("0.00") + ", wave " + waveStart.ToString("0.00") + " → " + waveEnd.ToString("0.00")
                + ", tetrapod-or-wave " + cleanStart.ToString("0.00") + " → " + cleanEnd.ToString("0.00") + ", stride " + strides.ToString("0"));
            // Either clean gait counts: the better of the two scores, averaged over the seeds.
            Assert.That(cleanEnd, Is.GreaterThan(.6));
            Assert.That(cleanEnd, Is.GreaterThan(cleanStart + .4));
            Assert.That(tetEnd, Is.GreaterThan(tetStart + .25));
            Assert.That(waveEnd, Is.GreaterThan(waveStart + .25));
            // Shorter than the old reach-long strides: about the spacing of the legs.
            Assert.That(strides, Is.InRange(18, 30));
        }

        [Test]
        public void NearlyEverySeedEndsInACleanGait()
        {
            // Tetrapod or wave above .7 by generation 80 on at least 95% of seeds.
            int clean = 0, n = 30;
            double distance = 0;
            for (int seed = 1; seed <= n; seed++)
            {
                var learner = new XgGaitLearner(seed);
                for (int i = 0; i < Generations; i++) learner.Generate();
                if (System.Math.Max(XgGaitLearner.Tetrapod(learner.Best), XgGaitLearner.Wave(learner.Best)) > .7) clean++;
                distance += learner.BestScore.distance;
            }
            TestContext.Progress.WriteLine("clean gaits: " + clean + "/" + n + ", mean distance " + (distance / n).ToString("0"));
            Assert.That(clean, Is.GreaterThanOrEqualTo(n * 95 / 100));
            Assert.That(distance / n, Is.GreaterThan(650));
        }

        [Test]
        public void AGenerationRunsWellUnderFiveMilliseconds()
        {
            var learner = new XgGaitLearner(7);
            for (int i = 0; i < 5; i++) learner.Generate();
            var clock = Stopwatch.StartNew();
            for (int i = 0; i < 40; i++) learner.Generate();
            double ms = clock.Elapsed.TotalMilliseconds / 40;
            TestContext.Progress.WriteLine("one generation: " + ms.ToString("0.00") + " ms");
            Assert.That(ms, Is.LessThan(5));
        }
    }
}
