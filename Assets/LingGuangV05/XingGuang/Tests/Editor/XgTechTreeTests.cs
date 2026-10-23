using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public class XgTechTreeTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e9;
            public bool rejectSpend;
            public double Compute => 4;
            public double VramMB => 1 << 16;
            public double Money => money;
            public bool Spend(double a) { if (rejectSpend || money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        [Test]
        public void EmptyRootPrerequisiteDoesNotInventAnUnpurchasableParent()
        {
            var sim = new XgSim(); var host = new Host(); sim.S.stage = 3;
            var node = XgCatalog.Node(XgSim.SpiderAcquireNode);
            Assert.IsNotNull(node); Assert.IsTrue(string.IsNullOrEmpty(node.parent));
            Assert.AreEqual(XgSim.NodeStatus.Buyable, sim.Status(node, host), sim.Why(node, host));
            double before = host.money, price = sim.NodeCost(node);
            int bought = 0; sim.NodeBought += n => { if (n.id == node.id) bought++; };
            Assert.IsTrue(XgTechTree.Buy(XgTechTree.EntryOf(node.id), sim, host));
            Assert.AreEqual(before-price, host.money, 1e-6);
            Assert.AreEqual(XgSim.NodeStatus.Owned, sim.Status(node, host));
            Assert.IsFalse(sim.BuyNode(node.id, host));
            Assert.AreEqual(before-price, host.money, 1e-6); Assert.AreEqual(1, bought);
        }

        [TestCase("spider.fast", "spider.acquire")]
        [TestCase("spider.browse", "spider.fast")]
        [TestCase("spider.autoanswer", "spider.acquire")]
        [TestCase("data.review", "brain.window")]
        [TestCase("arith.powers", "arith.mixed")]
        [TestCase("arith.calculus", "arith.powers")]
        public void RecentSkillBooksKeepTheirRealParentAndPurchaseExactlyOnce(string id, string parent)
        {
            var sim = new XgSim(); var host = new Host(); sim.S.stage = 6;
            var node = XgCatalog.Node(id); Assert.IsNotNull(node); Assert.AreEqual(parent, node.parent);
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.Status(node, host));
            double before = host.money;
            Assert.IsFalse(sim.BuyNode(id, host)); Assert.AreEqual(before, host.money);
            sim.S.unlocked.Add(parent);
            Assert.AreEqual(XgSim.NodeStatus.Buyable, sim.Status(node, host), sim.Why(node, host));
            host.rejectSpend = true;
            Assert.IsFalse(sim.BuyNode(id, host)); Assert.IsFalse(sim.Has(id)); Assert.AreEqual(before, host.money);
            host.rejectSpend = false; double price = sim.NodeCost(node);
            Assert.IsTrue(XgTechTree.Buy(XgTechTree.EntryOf(id), sim, host));
            Assert.IsTrue(sim.Has(id)); Assert.AreEqual(before-price, host.money, 1e-6);
            Assert.IsFalse(sim.BuyNode(id, host)); Assert.AreEqual(before-price, host.money, 1e-6);
        }

        [Test]
        public void MlpNeedsDeliveredExactChatButNoLaterStagePurchase()
        {
            using (new XgYyCatalogTestScope())
            {
                var sim = new XgSim(); var host = new Host();
                var node = XgCatalog.Node("mlp"); var entry = XgTechTree.EntryOf("mlp");
                Assert.AreEqual(1, sim.S.stage); Assert.IsTrue(sim.Has("perceptron"));
                Assert.IsTrue(XgTechTree.Visible(entry, sim));
                Assert.AreEqual(XgSim.NodeStatus.Locked, sim.Status(node, host));
                string zhHint = sim.Why(node, host);
                StringAssert.Contains("异或怎么分？", zhHint);
                StringAssert.Contains("收到回复", zhHint);
                StringAssert.DoesNotContain("图书馆", zhHint);
                sim.English = true;
                string enHint = sim.Why(node, host);
                StringAssert.Contains("How do you split XOR?", enHint);
                StringAssert.Contains("receive its reply", enHint);
                sim.English = false;
                double before = host.money;
                Assert.IsFalse(sim.BuyNode("mlp", host)); Assert.AreEqual(before, host.money);
                sim.RegisterYyReply("异或怎么分", "用多层网络试试。", true);
                Assert.IsFalse(sim.ChatNodeReady("mlp"), "the hint does not relax exact phrase matching");
                sim.RegisterYyReply("异或怎么分？", "", false);
                Assert.IsFalse(sim.ChatNodeReady("mlp"), "a failed request cannot bypass the conversation requirement");
                sim.RegisterYyReply("异或怎么分？", "用多层网络试试。", true);
                Assert.IsTrue(sim.ChatNodeReady("mlp"));
                Assert.AreEqual(XgSim.NodeStatus.Buyable, sim.Status(node, host), sim.Why(node, host));
                double price = sim.NodeCost(node);
                Assert.IsTrue(XgTechTree.Buy(entry, sim, host));
                Assert.IsTrue(sim.Has("mlp")); Assert.AreEqual(before-price, host.money, 1e-6);
            }
        }

        [Test]
        public void EveryBuyableCatalogNodeUsesTheAdvertisedPriceAndCannotChargeTwice()
        {
            using (new XgYyCatalogTestScope())
            {
                var sim = new XgSim(); var host = new Host();
                var bought = new HashSet<string>();
                // Stage snapshots exercise real purchase gates; conversation gates use delivered replies.
                // No prerequisite or activity unlock is injected just to make a purchase succeed.
                for (int stage = 1; stage <= 6; stage++)
                {
                    sim.S.stage = stage;
                    foreach (var gate in XgSim.ChatUnlocks)
                        if (stage >= gate.stage) XgYyCatalogTestScope.DeliverUnlock(sim, gate.node);
                    bool progressed;
                    do
                    {
                        progressed = false;
                        foreach (var node in XgCatalog.Nodes)
                        {
                            // Repeatable salary upgrades have their separate level/price tests below.
                            if (node.tree == "label" || sim.Status(node, host) != XgSim.NodeStatus.Buyable) continue;
                            double before = host.money, price = sim.NodeCost(node);
                            Assert.IsTrue(sim.BuyNode(node.id, host), node.id + ": " + sim.Why(node, host));
                            Assert.AreEqual(before-price, host.money, 1e-6, node.id + " quoted price");
                            Assert.IsTrue(sim.Has(node.id), node.id);
                            Assert.IsFalse(sim.BuyNode(node.id, host), node.id + " duplicate purchase");
                            Assert.AreEqual(before-price, host.money, 1e-6, node.id + " duplicate charge");
                            Assert.IsTrue(bought.Add(node.id), node.id);
                            progressed = true;
                        }
                    } while (progressed);
                }
                Assert.That(bought.Count, Is.GreaterThan(10), "The traversal must exercise meaningful catalog coverage");
                foreach (var node in XgCatalog.Nodes)
                {
                    if (node.tree == "label" || sim.Has(node.id)) continue;
                    Assert.AreNotEqual(XgSim.NodeStatus.Buyable, sim.Status(node, host), node.id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(sim.Why(node, host)), node.id + " needs an actionable blocker");
                    double before = host.money;
                    Assert.IsFalse(sim.BuyNode(node.id, host), node.id + " unsatisfied gate");
                    Assert.AreEqual(before, host.money, 1e-6, node.id + " blocked charge");
                }
            }
        }

        [Test]
        public void StageOnePricesMultiplyTheRoundedDiscountedQuoteOnlyOnceAndDoNotRebillOldSaves()
        {
            var sim=new XgSim();var host=new Host();
            var node=new XgNode {id="price.test",tree="trunk",stage=1,cost=57};
            // 定价重做 2026-10-24: raw = ceil(57 * .25) * 10 = 150 (not ceil(57 * 2.5)); quote = Nice(.78 * 150^.75) = 35.
            Assert.AreEqual(35,sim.NodeCost(node),"stage-one raw price x10 once, then the stage formula");
            node.stage=2;Assert.AreEqual(30,sim.NodeCost(node),"stage two: raw 15, 1.4 * 15^.75 = 11, raised to the stage floor 30");
            node.stage=1;sim.S.stage=6;Assert.AreEqual(35,sim.NodeCost(node),"later purchase uses node stage");
            // Every stage-one functional unlock is at least the stage floor; no purchase is retroactively charged.
            foreach(var n in XgCatalog.Nodes)
            {
                if(XgSim.EarlyTechnologyPriceFactor(n)!=10)continue;
                double price=sim.NodeCost(n);
                Assert.IsTrue(price == 0 || price >= XgSim.StagePriceFloor[1], n.id);
            }
            var owned=XgCatalog.Node("train.curve");sim.S.unlocked.Add(owned.id);
            double money=host.money,spent=sim.S.totalSpent;
            var restored=new XgSim(sim.S);
            Assert.IsTrue(restored.Has(owned.id));Assert.AreEqual(spent,restored.S.totalSpent);
            Assert.IsFalse(restored.BuyNode(owned.id,host));Assert.AreEqual(money,host.money,"owned node is never charged again");
            Assert.AreEqual(XgCatalog.RaiseCost(0),restored.NextRaiseCost,"salary upgrades unchanged");
            Assert.AreEqual(1,XgSim.EarlyTechnologyPriceFactor(XgCatalog.Node("spam.pack")),"training data price unchanged");
            Assert.AreEqual(1,XgSim.EarlyTechnologyPriceFactor(XgCatalog.Node(XgSim.MoleMixedNode)),"question pack price unchanged");
            int earlyPacks=0;
            foreach(var pack in XgCatalog.Nodes)
                if(pack.stage==1&&pack.id.StartsWith("pack.",System.StringComparison.Ordinal))
                {earlyPacks++;Assert.AreEqual(1,XgSim.EarlyTechnologyPriceFactor(pack),pack.id+" is a question pack, not a functional unlock");}
            Assert.Greater(earlyPacks,0,"actual stage-one question packs covered");
            foreach(var id in new[]{"label.raise","label.auto","label.coop","label.hard","label.audit",XgSim.TrapMemoryNode})
                Assert.AreEqual(1,XgSim.EarlyTechnologyPriceFactor(XgCatalog.Node(id)),id);

        }

        [Test]
        public void CatalogPrerequisitesResolveAndHaveNoCycles()
        {
            var nodes = new Dictionary<string,XgNode>();
            foreach (var n in XgCatalog.Nodes) { Assert.IsFalse(nodes.ContainsKey(n.id), n.id); nodes.Add(n.id,n); }
            foreach (var n in XgCatalog.Nodes) CheckPrerequisites(n, nodes, new HashSet<string>(), new HashSet<string>());
        }
        static void CheckPrerequisites(XgNode node, Dictionary<string,XgNode> nodes, HashSet<string> visiting, HashSet<string> done)
        {
            if (done.Contains(node.id)) return;
            Assert.IsTrue(visiting.Add(node.id), "Cyclic purchase prerequisite: "+node.id);
            var dependencies = new List<string>(node.needs);
            if (!string.IsNullOrEmpty(node.parent)) dependencies.Add(node.parent);
            foreach (var id in dependencies)
            {
                Assert.IsTrue(nodes.TryGetValue(id,out var parent), node.id+" references missing prerequisite "+id);
                CheckPrerequisites(parent,nodes,visiting,done);
            }
            visiting.Remove(node.id); done.Add(node.id);
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
                if (e.id == "board.logic1")
                {
                    Assert.AreEqual(XgNodeKind.Research, e.First.kind, "Board logic keeps its research behavior");
                    Assert.AreEqual(4, e.members.Count, "The designed logic node has exactly four levels");
                    for (int i = 0; i < 4; i++)
                        Assert.AreEqual("board.logic" + (i + 1), e.members[i].id, "Logic level order");
                }
                else Assert.IsTrue(XgTechTree.Ladder(e.First.kind), e.id);
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
            var logic = XgTechTree.EntryOf("board.logic1");
            Assert.IsNotNull(logic); Assert.IsTrue(logic.IsChain, "Board logic must remain one four-level square");
            for (int i = 1; i <= 4; i++)
                Assert.AreSame(logic, XgTechTree.EntryOf("board.logic" + i));
            Assert.AreNotSame(logic, XgTechTree.EntryOf("nextmove"), "The optional question desk is a separate purchase");
            var depth = XgTechTree.EntryOf("v.d4");
            Assert.IsNotNull(depth); Assert.IsTrue(depth.IsChain);
            Assert.IsNull(XgCatalog.Node("auto2"), "the automation lane is gone");
            foreach (var n in XgCatalog.Nodes) Assert.AreNotEqual("auto", n.lane, n.id);
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
            var auto = XgTechTree.EntryOf("v.w1");
            Assert.IsTrue(auto.IsChain);
            foreach (var id in new[] { "lenet", "mlp", "alexnet" }) if (!sim.Has(id)) sim.S.unlocked.Add(id);
            int max = XgTechTree.MaxLevel(auto);
            for (int level = 0; level < max; level++)
            {
                var next = XgTechTree.Next(auto, sim);
                Assert.AreEqual(auto.members[level], next);
                Assert.AreEqual(XgSim.NodeStatus.Buyable, XgTechTree.Status(auto, sim, host), next.id + ": " + sim.Why(next, host));
                double before = host.money;
                Assert.IsTrue(XgTechTree.Buy(auto, sim, host));
                Assert.IsTrue(sim.Has(next.id));
                Assert.AreEqual(sim.NodeCost(next), before - host.money, 1e-9);
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
