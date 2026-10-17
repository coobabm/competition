using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The learning rate the player picks (XgSim.Rates.cs): 自动 is safe, bigger steps learn faster and may 炸.</summary>
    public sealed class XgRateTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000000, compute = 2;
            public double Compute => compute;
            public double VramMB => 100000;
            public double Money => money;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim Trained(out Host host)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 3, stageVision = 3, stageSequence = 3 };
            var sim = new XgSim(state) { GoldChance = 0 };
            foreach (var n in XgCatalog.Nodes)
                if (n.stage <= 3 && n.tree != "label" && n.kind != XgNodeKind.Secret && n.kind != XgNodeKind.Label && n.id != "gradclip" && n.id != "warmup" && !sim.Has(n.id))
                    sim.S.unlocked.Add(n.id);
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 3000 });
            if (!sim.S.desksOpen.Contains("mnist")) sim.S.desksOpen.Add("mnist");
            sim = new XgSim(sim.S) { GoldChance = 0 };
            host = new Host();
            sim.SetDataset(XgTrack.Vision, "mnist");
            sim.AutoConfigure(sim.S.vision, host);
            return sim;
        }

        [Test]
        public void AutoIsTheSafeRateAndNeverTears()
        {
            var sim = Trained(out var host);
            var run = sim.S.vision;
            Assert.IsTrue(run.autoLr);
            Assert.AreEqual(sim.SafeLr(run), run.lr);
            Assert.AreEqual(0, sim.TearChance(run));
            for (int i = 0; i < 30; i++) Assert.IsFalse(sim.TrainEpoch(XgTrack.Vision, host).diverged);
            Assert.AreEqual(0, sim.S.divergences);
        }

        [Test]
        public void ABigStepBlowsUpAndCostsTheModel()
        {
            var sim = Trained(out var host);
            var run = sim.S.vision;
            for (int i = 0; i < 10; i++) sim.TrainEpoch(XgTrack.Vision, host);
            Assert.IsTrue(sim.SetRate(XgTrack.Vision, 3));
            Assert.IsFalse(run.autoLr);
            Assert.AreEqual(XgSim.TearChanceFarUp, sim.TearChance(run), 1e-9, "no clipping or warm-up owned here");
            Assert.AreEqual(2.5, XgSim.RateGainAt(3), 1e-9);
            Assert.AreEqual(0, XgSim.RateSteadyAt(-2), "the safest step never goes down");
            Assert.AreEqual(sim.RateShown(run, 0) * 9, sim.RateShown(run, 3), 1e-12, "the top step is nine times the safe rate");
            var fired = new List<int>();
            sim.Diverged += t => fired.Add(t);
            XgEpoch torn = null;
            double before = 0;
            for (int i = 0; i < 20 && torn == null; i++)
            {
                before = run.valAcc;
                var e = sim.TrainEpoch(XgTrack.Vision, host);
                if (e.diverged) torn = e;
            }
            Assert.IsNotNull(torn, "a 90% chance blows up within 20 rounds");
            Assert.That(run.valAcc, Is.LessThan(before), "the model falls back");
            Assert.That(sim.S.divergences, Is.GreaterThanOrEqualTo(1));
            CollectionAssert.Contains(fired, (int)XgTrack.Vision);
        }

        [Test]
        public void ASmallStepIsSafeButSlow()
        {
            var sim = Trained(out var host);
            var run = sim.S.vision;
            int safe = sim.SafeLr(run);
            Assert.IsTrue(sim.SetRate(XgTrack.Vision, -1));
            Assert.AreEqual(0, sim.TearChance(run));
            Assert.That(XgSim.RateGainAt(-1), Is.LessThan(1));
            Assert.That(XgSim.RateSteadyAt(-1), Is.LessThan(1));
            Assert.AreEqual(System.Math.Min(XgSim.RateValues.Length - 1, safe + 1), run.lr, "the board trains one step smaller");
            sim.AutoConfigure(run, host);
            Assert.AreEqual(-1, sim.RateNotch(run), "the automatic model keeps the player's step");
            Assert.IsTrue(sim.SetRate(XgTrack.Vision, 0, auto: true));
            Assert.IsTrue(run.autoLr); Assert.AreEqual(safe, run.lr); Assert.AreEqual(0, sim.RateNotch(run));
        }

        [Test]
        public void TheRateCannotChangeMidRound()
        {
            var sim = Trained(out var host);
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision, host, true));
            Assert.IsFalse(sim.SetRate(XgTrack.Vision, 1));
        }

        [Test]
        public void AnOldSaveStartsOnAuto()
        {
            var state = new XgState();
            state.vision.autoLr = false; state.vision.lr = 0;
            var sim = new XgSim(state) { GoldChance = 0 };
            Assert.IsTrue(sim.S.vision.autoLr);
            Assert.AreEqual(2, sim.S.rateSchema);
        }

        [Test]
        public void SixStepsWithOneBetweenNineAndThreeTimes()
        {
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 0, -1, -2 }, XgSim.RateNotches);
            Assert.That(XgSim.RateMultiple(2), Is.GreaterThan(XgSim.RateMultiple(1)).And.LessThan(XgSim.RateMultiple(3)));
            Assert.That(XgSim.RateGainAt(2), Is.GreaterThan(XgSim.RateGainAt(1)).And.LessThan(XgSim.RateGainAt(3)));
            var sim = Trained(out _);
            var run = sim.S.vision;
            Assert.IsTrue(sim.SetRate(XgTrack.Vision, 2));
            Assert.AreEqual(XgSim.TearChanceMidUp, sim.TearChance(run), 1e-9);
            Assert.That(XgSim.TearChanceMidUp, Is.GreaterThan(XgSim.TearChanceOneUp).And.LessThan(XgSim.TearChanceFarUp));
            Assert.IsTrue(sim.SetRate(XgTrack.Vision, 4));
            Assert.AreEqual(3, sim.RateNotch(run), "clamped to the top step");
        }

        [Test]
        public void AFiveStepSaveKeepsItsNineTimesPick()
        {
            var state = new XgState { rateSchema = 1 };
            state.vision.autoLr = false; state.vision.rateNotch = 2;
            state.sequence.autoLr = false; state.sequence.rateNotch = 1;
            var sim = new XgSim(state) { GoldChance = 0 };
            Assert.AreEqual(3, sim.S.vision.rateNotch, "×9 moved from +2 to +3");
            Assert.AreEqual(1, sim.S.sequence.rateNotch);
            Assert.AreEqual(2, sim.S.rateSchema);
        }
    }
}
