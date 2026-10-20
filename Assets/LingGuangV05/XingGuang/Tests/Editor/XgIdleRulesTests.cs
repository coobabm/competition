using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgIdleRulesTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1234, compute = 1;
            public string blocker;
            public int spends, earnings, training;
            public double earned;
            public double Money => money;
            public double Compute => compute;
            public double VramMB => 16000;
            public string Blocker => blocker;
            public bool Spend(double amount) { spends++; money -= amount; return true; }
            public void Earn(double amount) { earnings++; earned += amount; money += amount; }
            public void Train(double seconds) { training++; }
        }

        static XgSim Ready()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.autoLevel = 1;
            sim.S.desksOpen.Add("mnist");
            sim.S.best.Add(new XgBest { dataset = "mnist", acc = .8, arch = "perceptron" });
            return sim;
        }

        [Test]
        public void BasicAutomationAloneUnlocksIdleMode()
        {
            Assert.IsFalse(XgIdleRules.Unlocked(null));
            var sim = new XgSim();
            Assert.IsFalse(XgIdleRules.Unlocked(sim));
            sim.S.autoLevel = 1;
            Assert.IsTrue(XgIdleRules.Unlocked(sim));
            Assert.IsFalse(sim.Has("label.coop"));
            Assert.AreEqual("label.auto", XgIdleRules.UnlockNode);
        }

        [Test]
        public void SuccessiveSecondSamplesApproachOnlySettledMoney()
        {
            double displayed = 100, settled = 200;
            for (int second = 0; second < 3; second++)
            {
                double next = XgIdleRules.CatchUp(displayed, settled);
                Assert.Greater(next, displayed);
                Assert.Less(next, settled, "a large change is shown progressively, not all at once");
                displayed = next;
            }
            for (int second = 0; second < 100; second++)
            {
                displayed = XgIdleRules.CatchUp(displayed, settled);
                Assert.LessOrEqual(displayed, settled);
            }
            Assert.AreEqual(settled, displayed);
        }

        [Test]
        public void PausedSettledBalanceDoesNotCreateMoney()
        {
            double displayed = 125.25;
            for (int second = 0; second < 60; second++)
                Assert.AreEqual(125.25, displayed = XgIdleRules.CatchUp(displayed, 125.25));
        }

        [TestCase(200, 95)]
        [TestCase(200, 0)]
        [TestCase(200, -20)]
        public void SpendingImmediatelyLowersTheDisplayedBalance(double displayed, double settled)
        {
            Assert.AreEqual(settled, XgIdleRules.CatchUp(displayed, settled));
        }

        [Test]
        public void IncomeEstimateRespectsPauseAndHostAvailabilityWithoutSettling()
        {
            var sim = Ready(); var host = new Host();
            double rate = XgIdleRules.IncomeRate(sim, host);
            Assert.Greater(rate, 0, "the fixture has an eligible trained desk");
            sim.AutoPaused = true;
            Assert.AreEqual(0, XgIdleRules.IncomeRate(sim, host));
            sim.AutoPaused = false;
            Assert.AreEqual(rate, XgIdleRules.IncomeRate(sim, host));
            host.blocker = "power disconnected";
            Assert.AreEqual(0, XgIdleRules.IncomeRate(sim, host));
            host.blocker = null; host.compute = 0;
            Assert.AreEqual(0, XgIdleRules.IncomeRate(sim, host));
            Assert.AreEqual(0, XgIdleRules.IncomeRate(sim, null));
            Assert.AreEqual(1234, host.money);
            Assert.AreEqual(0, host.spends + host.earnings + host.training, "the compact display never simulates a second income stream");
        }

        [Test]
        public void ActualAutomaticPaymentsMatchIdleReceiptsAndPauseStopsNewPayments()
        {
            var sim = Ready(); var host = new Host();
            for (int second = 0; second < 120; second++) sim.TickCollaboration(1, host);
            Assert.Greater(host.earned, 0, "eligible automation actually earns, not just an estimated rate");
            Assert.AreEqual(host.earned, sim.S.idleAutomaticIncome, 1e-8);
            double paid = host.earned, receipts = sim.S.idleAutomaticIncome;
            sim.AutoPaused = true;
            for (int second = 0; second < 60; second++) sim.TickCollaboration(1, host);
            Assert.AreEqual(paid, host.earned);
            Assert.AreEqual(receipts, sim.S.idleAutomaticIncome);
            sim.AutoPaused = false;
            host.blocker = "power disconnected";
            for (int second = 0; second < 60; second++) sim.TickCollaboration(1, host);
            Assert.AreEqual(paid, host.earned);
            Assert.AreEqual(receipts, sim.S.idleAutomaticIncome);
        }

        [Test]
        public void NoAutomationOrWeakCheckpointReportsNoAutomaticIncome()
        {
            var sim = Ready(); var host = new Host();
            sim.S.autoLevel = 0;
            Assert.AreEqual(0, XgIdleRules.IncomeRate(sim, host));
            sim.S.autoLevel = 1; sim.S.best.Clear();
            Assert.AreEqual(0, XgIdleRules.IncomeRate(sim, host));
            Assert.AreEqual(0, XgIdleRules.IncomeRate(null, host));
        }
    }
}
