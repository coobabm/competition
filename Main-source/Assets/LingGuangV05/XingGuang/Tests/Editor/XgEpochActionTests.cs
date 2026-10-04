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
            sim.Tick(.3, host);
            Assert.That(sim.S.epochs, Is.Zero); Assert.That(sim.S.vision.epochProgress, Is.EqualTo(.5).Within(.001));
            sim.Tick(.3, host);
            Assert.That(sim.S.epochs, Is.EqualTo(1)); Assert.That(host.energy, Is.EqualTo(.6).Within(.001));
            Assert.That(starts, Is.EqualTo(1)); Assert.That(completed, Is.EqualTo(1));
            sim.Tick(1, host); Assert.That(sim.S.epochs, Is.EqualTo(1));
        }
        [Test]
        public void PowerLossPausesAndReloadCancelsAnUnsettledEpoch()
        {
            var sim = Ready(); var host = new Host();
            sim.BeginEpoch(XgTrack.Vision, host); sim.Tick(.2, host);
            host.blocker = "Power cut"; sim.Tick(10, host);
            Assert.That(sim.S.epochs, Is.Zero); Assert.That(host.energy, Is.Zero);
            Assert.That(sim.S.vision.epochProgress, Is.EqualTo(1.0/3).Within(.001));
            var loaded = new XgSim(sim.S);
            Assert.That(loaded.AnyEpochActive, Is.False); Assert.That(loaded.S.epochs, Is.Zero);
        }
        [Test]
        public void TransformerEpochTakesPointOneEightSeconds()
        {
            var sim = Ready(); var host = new Host();
            sim.S.unlocked.Add("transformer"); sim.S.vision.arch = "transformer";
            Assert.That(sim.BeginEpoch(XgTrack.Vision, host), Is.True);
            sim.Tick(.17, host); Assert.That(sim.S.epochs, Is.Zero);
            sim.Tick(.01, host); Assert.That(sim.S.epochs, Is.EqualTo(1));
            Assert.That(host.energy, Is.EqualTo(.18).Within(.0001));
        }
    }
}
