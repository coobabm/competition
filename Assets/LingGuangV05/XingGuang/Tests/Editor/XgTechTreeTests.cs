using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public class XgTechTreeTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e9;
            public double Compute => 4;
            public double VramMB => 1 << 16;
            public double Money => money;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        [Test]
        public void EveryCatalogNodeIsDrawnByExactlyOneSquare()
        {
            var seen = new HashSet<string>();
            foreach (var e in XgTechTree.Entries)
                foreach (var n in e.members)
                {
                    Assert.IsTrue(seen.Add(n.id), "drawn twice: " + n.id);
                    Assert.AreSame(e, XgTechTree.EntryOf(n.id), n.id);
                }
            foreach (var n in XgCatalog.Nodes) Assert.IsTrue(seen.Contains(n.id), "not drawn: " + n.id);
        }

        [Test]
        public void LaddersBecomeOneSquareInChainOrder()
        {
            foreach (var e in XgTechTree.Entries)
            {
                if (!e.IsChain) continue;
                Assert.IsTrue(XgTechTree.Ladder(e.First.kind), e.id);
                for (int i = 1; i < e.members.Count; i++)
                {
                    Assert.AreEqual(e.members[i - 1].id, e.members[i].parent, "each step needs the one before: " + e.members[i].id);
                    Assert.AreEqual(e.First.kind, e.members[i].kind, e.members[i].id);
                }
            }
            // Every ladder step whose parent is a step of the same kind sits in its parent's square.
            foreach (var n in XgCatalog.Nodes)
            {
                var parent = XgCatalog.Node(n.parent);
                if (XgTechTree.Ladder(n.kind) && parent != null && parent.kind == n.kind)
                    Assert.AreSame(XgTechTree.EntryOf(parent.id), XgTechTree.EntryOf(n.id), n.id);
            }
            var auto = XgTechTree.EntryOf("auto3");
            Assert.IsNotNull(auto); Assert.IsTrue(auto.IsChain); Assert.AreEqual("auto2", auto.id);
        }

        [Test]
        public void LevelledNodesKeepTheirOwnLevels()
        {
            var raise = XgTechTree.EntryOf("label.raise");
            Assert.IsFalse(raise.IsChain);
            Assert.AreEqual(XgCatalog.RaiseMax, XgTechTree.MaxLevel(raise));
            var sim = new XgSim(); var host = new Host();
            Assert.AreEqual(0, XgTechTree.Level(raise, sim));
            Assert.AreEqual(XgSim.NodeStatus.Buyable, XgTechTree.Status(raise, sim, host));
            double before = host.money;
            Assert.IsTrue(XgTechTree.Buy(raise, sim, host));
            Assert.AreEqual(1, sim.RaiseLevel);
            Assert.AreEqual(1, XgTechTree.Level(raise, sim));
            Assert.AreEqual(XgCatalog.RaiseCost(0), before - host.money, 1e-9, "the raise ladder's own price");
        }

        [Test]
        public void BuyingALadderBuysTheNextStepThroughTheSimulation()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.stage = 6; sim.Tick(1, host);
            if (!sim.Has("perceptron")) sim.S.unlocked.Add("perceptron");
            var auto = XgTechTree.EntryOf("auto2");
            int max = XgTechTree.MaxLevel(auto);
            for (int level = 0; level < max; level++)
            {
                var next = XgTechTree.Next(auto, sim);
                Assert.AreEqual(auto.members[level], next);
                Assert.AreEqual(XgSim.NodeStatus.Buyable, XgTechTree.Status(auto, sim, host), next.id + ": " + sim.Why(next, host));
                double before = host.money;
                Assert.IsTrue(XgTechTree.Buy(auto, sim, host));
                Assert.IsTrue(sim.Has(next.id));
                Assert.AreEqual(next.cost, before - host.money, 1e-9);
                Assert.AreEqual(level + 1, XgTechTree.Level(auto, sim));
            }
            Assert.IsNull(XgTechTree.Next(auto, sim));
            Assert.AreEqual(XgSim.NodeStatus.Owned, XgTechTree.Status(auto, sim, host));
            Assert.IsFalse(XgTechTree.Buy(auto, sim, host), "a maxed square buys nothing");
        }

        [Test]
        public void ALockedStepKeepsTheSquareLockedAndBuysNothing()
        {
            var sim = new XgSim(); var host = new Host();
            var vision = XgTechTree.EntryOf("v.d3");
            Assert.IsNotNull(vision);
            Assert.AreNotEqual(XgSim.NodeStatus.Buyable, XgTechTree.Status(vision, sim, host), "vision depth waits for LeNet");
            double before = host.money;
            Assert.IsFalse(XgTechTree.Buy(vision, sim, host));
            Assert.AreEqual(before, host.money);
            Assert.AreEqual(0, XgTechTree.Level(vision, sim));
        }

        [Test]
        public void LadderNamesHaveBothLanguages()
        {
            var sim = new XgSim();
            foreach (var e in XgTechTree.Entries)
            {
                if (!e.IsChain) continue;
                sim.English = false; string zh = XgTechTree.Name(e, sim);
                sim.English = true; string en = XgTechTree.Name(e, sim);
                Assert.IsFalse(string.IsNullOrEmpty(zh) || string.IsNullOrEmpty(en), e.id);
                Assert.AreNotEqual(zh, en, e.id);
            }
        }
    }
}
