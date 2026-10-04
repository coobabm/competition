using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>金标题: the platform's known-answer trap items and the 题库比对 node that remembers them.</summary>
    public sealed class XgQualityTrapTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim Ready(bool coop = false)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.desksOpen.Add("mnist");
            sim.S.autoLevel = 1;
            sim.S.best.Add(new XgBest { dataset = "mnist", acc = .8, arch = "perceptron" });
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .8, arch = "perceptron" });
            if (coop) sim.S.unlocked.Add("label.coop");
            sim.ForcedCheckChance = 0; // only traps are checked in these tests
            return sim;
        }

        static XgCard Trap(XgSim sim, int index, bool correct, double confidence = .99, string desk = "spam")
        {
            var c = sim.CreateTrapCard(desk, index);
            c.hasJudgment = true; c.guess = correct ? c.truth : !c.truth; c.confidence = confidence; c.judgeSource = "checkpoint";
            sim.S.prefetch.Add(c);
            return c;
        }

        [Test]
        public void TrapItemsAreAFixedPoolPerDesk()
        {
            var sim = Ready();
            var a = sim.CreateTrapCard("spam", 3); var b = sim.CreateTrapCard("spam", 3); var other = sim.CreateTrapCard("spam", 4);
            Assert.AreEqual("spam#3", a.trapId);
            Assert.AreEqual(a.seed, b.seed); Assert.AreEqual(a.truth, b.truth); Assert.AreEqual(a.question, b.question); Assert.AreEqual(a.roll, b.roll);
            Assert.AreNotEqual(a.id, b.id, "each appearance is its own card");
            Assert.AreNotEqual(a.seed, other.seed);
            Assert.IsNull(sim.CreateTrapCard("spam", XgSim.TrapPoolSize)); Assert.IsNull(sim.CreateTrapCard("nope", 0));
            long rng = sim.S.rng;
            sim.CreateTrapCard("mnist", 0);
            Assert.AreEqual(rng, sim.S.rng, "trap items never move the lab's own random stream");
            Assert.IsFalse(a.gold);
        }

        [Test]
        public void TrapItemsAreAlwaysCheckedAndRemembered()
        {
            var sim = Ready(); var host = new Host();
            var c = Trap(sim, 1, true);
            Assert.IsTrue(sim.Route(c.id, host));
            Assert.AreEqual(1, sim.S.qcCheckedTotal, "checked although ordinary labels are not");
            Assert.AreEqual(1, sim.SpotChecks, "a right trap answer is a normal pass");
            Assert.AreEqual(XgSim.QcCreditStart + XgSim.QcCreditPass, sim.Credit, 1e-9);
            Assert.IsTrue(sim.TrapRemembered(c)); Assert.AreEqual(1, sim.TrapsRemembered);
            Assert.IsFalse(sim.TrapRecognized(c), "remembering needs 题库比对");
        }

        [Test]
        public void AWrongTrapCostsFiveTimesThePayEightCreditAndTwoErrors()
        {
            var sim = Ready(); var host = new Host();
            XgQcFine fined = null; sim.QualityFined += f => fined = f;
            Assert.IsFalse(sim.ForumFlag(XgSim.TrapForumFlag));
            var c = Trap(sim, 2, false);
            double money = host.money, pay = sim.PayFor("spam", c.level);
            Assert.IsTrue(sim.Route(c.id, host));
            Assert.AreEqual(money - 5 * pay, host.money, 1e-9);
            Assert.IsNotNull(fined); Assert.IsTrue(fined.trap); Assert.AreEqual(5 * pay, fined.fine, 1e-9);
            Assert.AreEqual(XgSim.QcCreditStart - XgSim.TrapCreditFail, sim.Credit, 1e-9);
            CollectionAssert.AreEqual(new[] { false, false }, sim.S.qcChecks, "counts as two errors in the window");
            Assert.AreEqual(1, sim.S.qcTrapFails);
            Assert.IsTrue(sim.S.qcTrapRevealed); Assert.IsTrue(sim.ForumFlag(XgSim.TrapForumFlag), "the forum thread about trap items opens");
            Assert.IsFalse(sim.ForumFlag("something.else"));
        }

        [Test]
        public void AboutTwoPercentOfAutomaticCardsAreTrapItems()
        {
            var sim = Ready();
            int traps = 0, total = 0;
            for (int i = 0; i < 4000; i++)
            {
                sim.FillJudgmentBuffer();
                foreach (var c in sim.S.prefetch) { total++; if (XgSim.IsTrap(c)) traps++; }
                sim.S.prefetch.Clear();
            }
            Assert.AreEqual(XgSim.TrapChance, (double)traps / total, .006);

            var off = Ready(); off.PlatformChecks = false;
            for (int i = 0; i < 300; i++) { off.FillJudgmentBuffer(); foreach (var c in off.S.prefetch) Assert.IsFalse(XgSim.IsTrap(c)); off.S.prefetch.Clear(); }
        }

        [Test]
        public void TrapsDoNotShiftTheLabsOwnRandomStream()
        {
            var with = Ready(); var without = Ready(); without.PlatformChecks = false;
            with.ForcedTrapChance = 1;
            for (int i = 0; i < 20; i++) { with.FillJudgmentBuffer(); without.FillJudgmentBuffer(); with.S.prefetch.Clear(); without.S.prefetch.Clear(); }
            Assert.AreEqual(without.S.rng, with.S.rng);
        }

        [Test]
        public void TrapMemorySendsARememberedItemToReviewWithAFamiliarTag()
        {
            var sim = Ready(coop: true); var host = new Host();
            var node = XgCatalog.Node(XgSim.TrapMemoryNode);
            Assert.IsNotNull(node); Assert.AreEqual("label.coop", node.parent); Assert.AreEqual(800, node.cost);
            Assert.AreEqual(XgSim.NodeStatus.Buyable, sim.StatusLabelNode(node, host));
            Assert.AreEqual(XgSim.NodeStatus.Locked, Ready(coop: false).StatusLabelNode(node, host));

            var first = Trap(sim, 5, false);
            Assert.IsTrue(sim.Route(first.id, host));
            Assert.AreEqual(0, sim.ReviewCount(), "a new trap item is submitted like any card");
            var again = Trap(sim, 5, false);
            Assert.IsFalse(sim.TrapRecognized(again));
            Assert.IsTrue(sim.Route(again.id, host));
            Assert.AreEqual(0, sim.ReviewCount(), "without the node it is submitted again");
            int checks = sim.S.qcCheckedTotal;

            Assert.IsTrue(sim.BuyLabelNode(node, host));
            var third = Trap(sim, 5, false);
            Assert.IsTrue(sim.TrapRecognized(third));
            Assert.IsTrue(sim.Route(third.id, host));
            Assert.AreEqual(1, sim.ReviewCount(), "remembered: a human answers it");
            Assert.AreEqual(checks, sim.S.qcCheckedTotal, "and the platform never sees the model's answer");
            Assert.AreSame(third, sim.ReviewCard("spam"));
            var result = sim.AnswerQueued(third.id, third.truth, host);
            Assert.IsTrue(result.correct);
            Assert.AreEqual(checks, sim.S.qcCheckedTotal, "hand answers are never checked");

            var fresh = Trap(sim, 6, true);
            Assert.IsFalse(sim.TrapRecognized(fresh), "an item it has not met yet is not familiar");
        }

        [Test]
        public void TrapMemoryIsRepairedOnLoad()
        {
            var state = new XgState { qcTrapsSeen = null, qcTrapFails = -2 };
            var sim = new XgSim(state);
            Assert.IsNotNull(sim.S.qcTrapsSeen); Assert.AreEqual(0, sim.S.qcTrapFails);
            sim.S.qcTrapsSeen.Add(""); sim.S.qcTrapsSeen.Add("spam#1");
            var again = new XgSim(sim.S);
            CollectionAssert.AreEqual(new[] { "spam#1" }, again.S.qcTrapsSeen);
        }
    }
}
