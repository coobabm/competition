using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// Training has no combo (2026-10-18): a round pressed by hand neither adds to the combo nor holds its timer, so a
    /// combo built by labeling fades while the bar runs, as it would anywhere else.
    /// </summary>
    public sealed class XgTrainComboTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6;
            public double Compute => 1;
            public double VramMB => 4096;
            public double Money => money;
            public string Blocker => null;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
        }

        static XgSim Ready()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.desksOpen.Add("mnist");
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 20 });
            return sim;
        }

        /// <summary>Ticks until the running round lands.</summary>
        static void Land(XgSim sim, Host host)
        {
            int before = sim.S.epochs;
            for (double t = 0; sim.S.epochs == before && t < 60; t += .05) sim.Tick(.05, host);
            Assert.That(sim.S.epochs, Is.EqualTo(before + 1), "the round lands");
        }

        [Test]
        public void HandRoundsDoNotBuildACombo()
        {
            var sim = Ready(); var host = new Host();
            for (int i = 0; i < 5; i++)
            {
                Assert.That(sim.BeginEpoch(XgTrack.Vision, host, true), Is.True, "round " + i);
                Land(sim, host);
                Assert.That(sim.S.combo, Is.Zero, "round " + i + " adds nothing to the combo");
                sim.Tick(.5, host);
            }
            Assert.That(sim.S.clicks, Is.GreaterThanOrEqualTo(5), "the presses still count as clicks");
        }

        [Test]
        public void AHandRoundDoesNotHoldALabelingCombo()
        {
            var sim = Ready(); var host = new Host();
            sim.S.combo = 7; sim.S.comboTimer = 1;
            Assert.That(sim.BeginEpoch(XgTrack.Vision, host, true), Is.True);
            Assert.That(sim.HandRoundRunning, Is.True);
            sim.Tick(1.2, host);
            Assert.That(sim.S.combo, Is.Zero, "the combo fades while the round runs");
        }
    }
}
