using System;
using NUnit.Framework;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;
namespace LingGuangV05.Tests
{
    public sealed class DailyBargainingTests
    {
        [Test] public void NegotiatedPurchaseDebitsOnceAndAddsOneCard()
        {
            var sim=new ChapterOneSim();sim.S.money=100000;sim.S.caseCount=4;
            var today=new DateTime(2016,10,15);string id=HardwareCatalog.UsedListings[0];
            double price=HardwareCatalog.UsedPrice(HardwareCatalog.Gpu(id),today);int count=sim.S.gpuCount;
            Assert.True(sim.BuyUsedCard(id,today,true));Assert.AreEqual(100000-price*.95,sim.S.money,1e-6);Assert.AreEqual(count+1,sim.S.gpuCount);
        }
        [Test] public void FailedNegotiatedPurchaseDoesNotDebitOrAddCard()
        {
            var sim=new ChapterOneSim();sim.S.money=0;sim.S.caseCount=4;int count=sim.S.gpuCount;
            Assert.False(sim.BuyUsedCard(HardwareCatalog.UsedListings[0],new DateTime(2016,10,15),true));
            Assert.AreEqual(0,sim.S.money);Assert.AreEqual(count,sim.S.gpuCount);
        }
        [Test] public void OriginalCallStillUsesFullPrice()
        {
            var sim=new ChapterOneSim();sim.S.money=100000;sim.S.caseCount=4;
            var today=new DateTime(2016,10,15);string id=HardwareCatalog.UsedListings[0];double price=HardwareCatalog.UsedPrice(HardwareCatalog.Gpu(id),today);
            Assert.True(sim.BuyUsedCard(id,today));Assert.AreEqual(100000-price,sim.S.money,1e-6);
        }
    }
}
