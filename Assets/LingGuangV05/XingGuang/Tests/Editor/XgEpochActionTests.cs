using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgEpochActionTests
    {
        [Test]
        public void InteractiveTrainingHasAnExplicitStartInsteadOfInstantSettlement()
        {
            Assert.That(typeof(XgSim).GetMethod("BeginEpoch"), Is.Not.Null,
                "Player training needs a start/tick/complete action; TrainEpoch currently settles immediately.");
        }
        sealed class Host : IXgHost
        {
            public double money = 1000, energy;
            public string blocker;
            public double Compute => 1;
            public double VramMB => 4096;
            public double Money => money;
            public string Blocker => blocker;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { energy += seconds; }
        }
        static XgSim Ready()
        {
            var sim = new XgSim();
            sim.S.desksOpen.Add("mnist"); // digits open at stage two; these tests exercise the vision track
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 20 });
            return sim;
        }
        [Test]
        public void EpochSettlesOnlyAfterItsDurationAndDoubleClickDoesNothing()
        {
            var sim = Ready(); var host = new Host(); int starts = 0, completed = 0;
            sim.EpochStarted += _ => starts++; sim.EpochDone += _ => completed++;
            Assert.That(sim.BeginEpoch(XgTrack.Vision, host), Is.True);
            Assert.That(sim.BeginEpoch(XgTrack.Vision, host), Is.False);
            Assert.That(sim.S.epochs, Is.Zero); Assert.That(host.energy, Is.Zero);
            Assert.That(sim.Assess(XgTrack.Vision, host), Is.Null);
            // The round takes real time (XgSim.TrainPace.cs): it lands between ×0.7 and ×1.5 of its planned length.
            double nominal = sim.S.vision.epochDuration, t = 0;
            Assert.That(nominal, Is.GreaterThan(XgSim.EpochSeconds));
            while (sim.S.epochs == 0 && t < 30) { sim.Tick(.05, host); t += .05; if (t < XgSim.PaceMinFactor * nominal - .05) Assert.That(sim.S.epochs, Is.Zero); }
            Assert.That(t, Is.InRange(XgSim.PaceMinFactor * nominal - .05, XgSim.PaceMaxFactor * nominal + .05));
            Assert.That(sim.S.epochs, Is.EqualTo(1));
            Assert.That(host.energy, Is.EqualTo(nominal * XgSim.PowerSecondsPerPaceSecond).Within(.001), "the bill follows the round's planned length");
            Assert.That(starts, Is.EqualTo(1)); Assert.That(completed, Is.EqualTo(1));
            sim.Tick(1, host); Assert.That(sim.S.epochs, Is.EqualTo(1));
        }
        [Test]
        public void PowerLossPausesAndReloadCancelsAnUnsettledEpoch()
        {
            var sim = Ready(); var host = new Host();
            sim.BeginEpoch(XgTrack.Vision, host); sim.Tick(.2, host);
            double progress = sim.S.vision.epochProgress;
            Assert.That(progress, Is.GreaterThan(0));
            host.blocker = "Power cut"; sim.Tick(10, host);
            Assert.That(sim.S.epochs, Is.Zero); Assert.That(host.energy, Is.Zero);
            Assert.That(sim.S.vision.epochProgress, Is.EqualTo(progress).Within(1e-12));
            Assert.That(sim.Pace(XgTrack.Vision).speed, Is.Zero);
            var loaded = new XgSim(sim.S);
            Assert.That(loaded.AnyEpochActive, Is.False); Assert.That(loaded.S.epochs, Is.Zero);
        }
        [Test]
        public void TransformerEpochBillsItsShorterRound()
        {
            var sim = Ready(); var host = new Host();
            sim.S.unlocked.Add("transformer"); sim.S.vision.arch = "transformer";
            Assert.That(sim.BeginEpoch(XgTrack.Vision, host), Is.True);
            double nominal = sim.S.vision.epochDuration;
            for (int i = 0; i < 400 && sim.S.epochs == 0; i++) sim.Tick(.05, host);
            Assert.That(sim.S.epochs, Is.EqualTo(1));
            Assert.That(host.energy, Is.EqualTo(nominal * XgSim.PowerSecondsPerPaceSecond).Within(.0001));
            Assert.That(host.energy, Is.LessThan(XgSim.EpochSeconds), "every word at once: a shorter round than the reference, a smaller bill");
        }
    }
}
