using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgSpiderUpgradeTests
    {
        [Test] public void NewSaveMustPurchaseBeforeIntro()
        {
            var sim=new XgSim(); sim.S.stage=3;sim.S.epochs=5;
            Assert.IsFalse(sim.SpiderReadingUnlocked);Assert.IsFalse(sim.SpiderIntroDue(true,false,false));
            sim.S.unlocked.Add(XgSim.SpiderAcquireNode);
            Assert.IsTrue(sim.SpiderIntroDue(true,false,false));
            sim.BeginPurchasedSpiderIntro();
            Assert.IsTrue(sim.SpiderAround);Assert.IsTrue(sim.S.spiderPurchasedInitially);
            Assert.IsFalse(sim.SpiderAutomaticBrowsing);
        }
        [Test] public void NewPurchasedSpiderRemainsTierOneAfterReload()
        {
            var sim=new XgSim();sim.S.unlocked.Add(XgSim.SpiderAcquireNode);sim.BeginPurchasedSpiderIntro();
            var loaded=new XgSim(sim.S);
            Assert.IsFalse(loaded.LegacySpiderAbilities);Assert.IsFalse(loaded.Has(XgSim.SpiderBrowseNode));
        }
        [Test] public void OldOwnedSpiderKeepsNavigationWithoutRepurchase()
        {
            var sim=new XgSim(new XgState { spiderIntroDone=true });
            Assert.IsTrue(sim.Has(XgSim.SpiderAcquireNode));Assert.IsTrue(sim.Has(XgSim.SpiderBrowseNode));
            Assert.IsTrue(sim.SpiderAutomaticBrowsing);Assert.IsFalse(sim.Has(XgSim.SpiderFastNode));
            Assert.IsFalse(sim.Has(XgSim.SpiderAutoAnswerNode));
        }
        [Test] public void TravelUpgradeDoesNotAccelerateAnswering()
        {
            var sim=new XgSim();sim.S.unlocked.Add(XgSim.SpiderFastNode);
            Assert.AreEqual(2,sim.SpiderTravelSpeed);Assert.AreEqual(2,sim.SpiderReadingSpeed);
            Assert.AreEqual(3,sim.SpiderAnswerPeriod);
            sim.S.unlocked.Add(XgSim.SpiderAutoAnswerNode);Assert.AreEqual(1,sim.SpiderAnswerPeriod);
        }
        [Test] public void IndependentAnswerUpgradeDoesNotGrantBrowsing()
        {
            var sim=new XgSim();sim.S.unlocked.Add(XgSim.SpiderAcquireNode);sim.BeginPurchasedSpiderIntro();
            sim.S.unlocked.Add(XgSim.SpiderAutoAnswerNode);
            Assert.AreEqual(1,sim.SpiderAnswerPeriod);Assert.IsFalse(sim.SpiderAutomaticBrowsing);
            Assert.AreEqual(1,sim.SpiderTravelSpeed);
        }
        [Test] public void CatalogHasOriginalThreeTierChainAndIndependentAnswerBranch()
        {
            var acquire=XgCatalog.Node(XgSim.SpiderAcquireNode);var fast=XgCatalog.Node(XgSim.SpiderFastNode);
            var browse=XgCatalog.Node(XgSim.SpiderBrowseNode);var answer=XgCatalog.Node(XgSim.SpiderAutoAnswerNode);
            Assert.NotNull(acquire);Assert.NotNull(fast);Assert.NotNull(browse);Assert.NotNull(answer);
            Assert.AreEqual(3,acquire.stage);Assert.AreEqual(1500,acquire.cost);
            Assert.AreEqual(acquire.id,fast.parent);Assert.AreEqual(fast.id,browse.parent);
            Assert.AreEqual(acquire.id,answer.parent);
        }
    }
}
