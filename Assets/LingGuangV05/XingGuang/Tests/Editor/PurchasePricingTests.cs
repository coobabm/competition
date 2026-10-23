using System;
using NUnit.Framework;
using LingGuangV05.Core;

namespace LingGuangV05.XingGuang.Tests
{
    public class PurchasePricingTests
    {
        [TestCase(0, 0)]
        [TestCase(8, 800)]
        [TestCase(15, 150)]
        [TestCase(60, 600)]
        [TestCase(99, 990)]
        [TestCase(100, 100)]
        [TestCase(150, 150)]
        [TestCase(12000, 12000)]
        public void QuotesUsePowersOfTenAndNeverRepeatTheIncrease(double original, double expected)
        {
            Assert.AreEqual(expected, PurchasePricing.AtLeastHundred(original));
            Assert.AreEqual(expected, PurchasePricing.AtLeastHundred(expected));
        }

        [Test]
        public void UnavailableSentinelsRemainUnavailable()
        {
            Assert.IsTrue(double.IsPositiveInfinity(PurchasePricing.AtLeastHundred(double.PositiveInfinity)));
            Assert.IsTrue(double.IsNaN(PurchasePricing.AtLeastHundred(double.NaN)));
        }

        // 定价重做（小丑牌式，2026-10-24）: the ×10 "at least ¥100" floor no longer applies to the tech tree. Every quote is a
        // small round number on its stage's scale, never under the stage's floor; hand-picked prices keep their numbers.
        [Test]
        public void EveryTechnologyQuoteIsASmallRoundNumberOnItsStageScale()
        {
            var sim = new XgSim();
            foreach (var n in XgCatalog.Nodes)
            {
                double cost = sim.NodeCost(n);
                if (n.tree == "label") { Assert.AreEqual(cost, sim.LabelNodeCost(n), n.id + " direct label quote"); continue; }
                if (cost == 0 || XgSim.HandPricedNode(n)) continue;
                Assert.AreEqual(XgSim.NicePrice(cost), cost, n.id + " quote=" + cost + " is a round number");
                Assert.GreaterOrEqual(cost, XgSim.StagePriceFloor[System.Math.Max(1, System.Math.Min(6, n.stage))], n.id + " is not under its stage's floor");
            }
            for (int i = 0; i < XgCatalog.RaiseMax; i++) Assert.AreEqual(XgSim.NicePrice(XgCatalog.RaiseCost(i)), XgCatalog.RaiseCost(i), "raise " + i);
            for (int i = 0; i < XgCatalog.AutoMaxLevel; i++) Assert.AreEqual(XgSim.NicePrice(XgCatalog.AutoCost(i)), XgCatalog.AutoCost(i), "auto " + i);
            Assert.AreEqual(10, sim.NodeCost(XgCatalog.Node("train.curve")), "the first buy (Training curve) is about twelve answers' pay");
            Assert.AreEqual(35, sim.NodeCost(XgCatalog.Node("shared.lr")), "stage-one knob: ceil(60 x .25) x 10 = 150, .78 x 150^.75 = 33, so 35");
            Assert.AreEqual(XgSim.TechQuote(150, 1), sim.NodeCost(XgCatalog.Node("shared.lr")));
        }

        [Test]
        public void NicePricesAreSmallRoundNumbers()
        {
            Assert.AreEqual(10, XgSim.NicePrice(10.4)); Assert.AreEqual(13, XgSim.NicePrice(12.6));
            Assert.AreEqual(35, XgSim.NicePrice(33.4)); Assert.AreEqual(210, XgSim.NicePrice(210.6));
            Assert.AreEqual(1200, XgSim.NicePrice(1216)); Assert.AreEqual(12400, XgSim.NicePrice(12356));
            Assert.AreEqual(1, XgSim.NicePrice(.2), "never free once priced");
            Assert.AreEqual(0, XgSim.NicePrice(0));
            Assert.IsTrue(double.IsPositiveInfinity(XgSim.NicePrice(double.PositiveInfinity)));
        }

        [Test]
        public void RaiseAndAutoLaddersNeverGetCheaperFromOneLevelToTheNext()
        {
            // The ×10 floor once made 加薪 Lv5 ¥142 after Lv4's ¥840 (and 自动答题 Lv2 ¥102 after Lv1's ¥600); the ladders no longer use it.
            for (int i = 1; i < XgCatalog.RaiseMax; i++) Assert.Greater(XgCatalog.RaiseCost(i), XgCatalog.RaiseCost(i - 1), "raise " + i);
            for (int i = 1; i < XgCatalog.AutoMaxLevel; i++) Assert.Greater(XgCatalog.AutoCost(i), XgCatalog.AutoCost(i - 1), "auto " + i);
            Assert.AreEqual(40, XgCatalog.AutoCost(0), "自动答题 Lv1 is a small stage-two buy (¥40, x2.2 per level)");
            Assert.AreEqual(30, XgCatalog.RaiseCost(0), "the first raise (x2 hand pay) is a stage-one buy of under a minute (¥30, x1.5 per level)");
        }
    }
}
