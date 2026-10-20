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

        [Test]
        public void EveryTechnologyAndRepeatableUpgradeHasAFreeOrHundredYuanQuote()
        {
            var sim = new XgSim();
            foreach (var n in XgCatalog.Nodes)
            {
                double cost = sim.NodeCost(n);
                Assert.IsTrue(cost == 0 || cost >= 100, n.id + " quote=" + cost);
                if (n.tree == "label") Assert.AreEqual(cost, sim.LabelNodeCost(n), n.id + " direct label quote");
            }
            for (int i = 0; i < XgCatalog.RaiseMax; i++) Assert.GreaterOrEqual(XgCatalog.RaiseCost(i), 100);
            for (int i = 0; i < XgCatalog.AutoMaxLevel; i++) Assert.GreaterOrEqual(XgCatalog.AutoCost(i), 100);
            Assert.AreEqual(150, sim.NodeCost(XgCatalog.Node("shared.lr")), "already raised stage-one price stays 150");
        }
    }
}
