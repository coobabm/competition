using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>摆渡众包's 自动提交 page: which desks the model labels, and with which model.</summary>
    public sealed class XgAutoPlanTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000, compute = 1;
            public double Compute => compute;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim Lab()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.desksOpen.Add("mnist"); sim.S.desksOpen.Add("spam"); sim.S.desksOpen.Add("logic");
            sim.S.best.Add(new XgBest { dataset = "mnist", acc = .9, arch = "lenet" });
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .4, arch = "perceptron" });
            return sim;
        }

        static XgAutoDeskPlan Plan(XgSim sim, Host host, string desk) => sim.AutoDeskPlans(host).Find(p => p.desk.id == desk);

        [Test]
        public void WithoutAutoLabellingEveryDeskIsOff()
        {
            var sim = Lab(); var host = new Host();
            Assert.AreEqual(XgAutoDeskState.Off, sim.AutoGlobalState(host));
            foreach (var p in sim.AutoDeskPlans(host)) Assert.AreEqual(XgAutoDeskState.Off, p.state, p.desk.id);
        }

        [Test]
        public void EachOpenDeskShowsItsCheckpointAndWhyItIsOrIsNotLabelled()
        {
            var sim = Lab(); var host = new Host();
            sim.S.autoLevel = 2;
            var mnist = Plan(sim, host, "mnist");
            Assert.AreEqual(XgAutoDeskState.Running, mnist.state);
            Assert.AreEqual("lenet", mnist.arch.id);
            Assert.AreEqual(.9, mnist.accuracy, 1e-9);
            Assert.AreEqual(sim.CollaborationRate("mnist", host), mnist.rate, 1e-9);
            Assert.Greater(mnist.rate, 0);
            Assert.IsFalse(mnist.usesBrain);
            Assert.AreEqual(XgAutoDeskState.BelowBar, Plan(sim, host, "spam").state, "a 40% checkpoint never answers for the player");
            Assert.AreEqual(0, Plan(sim, host, "spam").rate);
            Assert.AreEqual(XgAutoDeskState.NoCheckpoint, Plan(sim, host, "logic").state);
            Assert.IsNull(Plan(sim, host, "cifar"), "closed desks are not listed");
        }

        [Test]
        public void GlobalHoldsStopEveryEligibleDesk()
        {
            var sim = Lab(); var host = new Host();
            sim.S.autoLevel = 1;
            sim.AutoPaused = true;
            Assert.AreEqual(XgAutoDeskState.PausedByYou, Plan(sim, host, "mnist").state);
            Assert.AreEqual(XgAutoDeskState.BelowBar, Plan(sim, host, "spam").state, "the desk's own reason comes first");
            sim.AutoPaused = false; host.compute = 0;
            Assert.AreEqual(XgAutoDeskState.NoPower, Plan(sim, host, "mnist").state);
            Assert.AreEqual(0, Plan(sim, host, "mnist").rate);
        }

        [Test]
        public void TextDesksGoToTheBrainOnlyWhileItIsOnline()
        {
            var sim = Lab(); var host = new Host();
            sim.S.autoLevel = 1;
            sim.S.best.Add(new XgBest { dataset = "logic", acc = .85, arch = "mlp" });
            sim.S.unlocked.Add("label.brain");
            sim.BrainOnline = false;
            var logic = Plan(sim, host, "logic");
            Assert.IsFalse(logic.usesBrain); Assert.IsTrue(logic.brainFallback, "offline: the checkpoint answers");
            sim.BrainOnline = true;
            logic = Plan(sim, host, "logic");
            Assert.IsTrue(logic.usesBrain); Assert.IsFalse(logic.brainFallback);
            Assert.IsFalse(Plan(sim, host, "mnist").usesBrain, "pictures never go to the language model");
        }
    }
}
