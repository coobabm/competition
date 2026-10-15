using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// The hand-training combo across rounds that take seconds (XgSim.TrainPace.cs): the 1.5 s window counts from the end
    /// of a round and holds while a hand round runs, so pressing 训练一轮 again promptly keeps it.
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
        public void PromptPressesKeepTheComboAcrossLongRounds()
        {
            var sim = Ready(); var host = new Host();
            int combo = 0;
            for (int i = 0; i < 5; i++)
            {
                Assert.That(sim.BeginEpoch(XgTrack.Vision, host, true), Is.True, "round " + i);
                Assert.That(sim.S.vision.epochDuration, Is.GreaterThan(XgSim.EpochComboWindow), "a round outlasts the window");
                Land(sim, host);
                // +1 a round (+5 more when its assessment sets a record by hand).
                Assert.That(sim.S.combo, Is.GreaterThan(combo), "round " + i + " adds to the combo");
                combo = sim.S.combo;
                sim.Tick(.5, host); // a moment to press again
            }
        }

        [Test]
        public void TheWindowCountsFromTheEndOfTheRound()
        {
            var sim = Ready(); var host = new Host();
            sim.BeginEpoch(XgTrack.Vision, host, true);
            Land(sim, host);
            int combo = sim.S.combo;
            Assert.That(combo, Is.GreaterThan(0));
            Assert.That(sim.HandRoundRunning, Is.False);
            sim.Tick(XgSim.EpochComboWindow - .2, host);
            Assert.That(sim.S.combo, Is.EqualTo(combo), "still inside the window after the round");
            sim.Tick(.4, host);
            Assert.That(sim.S.combo, Is.Zero, "waiting past the window breaks it");
        }

        [Test]
        public void AnAutomaticRoundDoesNotHoldTheTimer()
        {
            var sim = Ready(); var host = new Host();
            sim.BeginEpoch(XgTrack.Vision, host, true);
            Land(sim, host);
            Assert.That(sim.BeginEpoch(XgTrack.Vision, host, false), Is.True);
            Assert.That(sim.HandRoundRunning, Is.False);
            sim.Tick(XgSim.EpochComboWindow + .1, host);
            Assert.That(sim.S.combo, Is.Zero, "only a hand round holds the combo");
        }
    }
}
