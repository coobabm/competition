using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>淘货, 喵鱼 and the card models (design v1.1 §3, §14.3).</summary>
    public sealed class HardwareTests
    {
        static DateTime D(int month, int day) => new DateTime(2016, month, day);
        static ChapterOneSim Rich(double money = 100000) { var sim = new ChapterOneSim(); sim.S.money = money; return sim; }

        [Test]
        public void SaleDiscountCanReduceNormalizedPriceBelowHundred()
        {
            var listing = new GpuModel { price = 120 };
            Assert.That(HardwareCatalog.Price(listing, HardwareCatalog.SinglesDay), Is.EqualTo(96));
            Assert.That(HardwareCatalog.Price(listing, D(11, 10)), Is.EqualTo(120));
        }

        [Test]
        public void CheapUsedPurchaseUsesTheQuoteWithoutRaisingResaleValue()
        {
            var g = HardwareCatalog.Gpu(HardwareCatalog.Gtx750Ti);
            double previousPrice = g.usedPrice;
            try
            {
                g.usedPrice = 100;
                var date = g.release;
                Assert.That(HardwareCatalog.UsedPrice(g, date), Is.EqualTo(100));
                Assert.That(HardwareCatalog.UsedPurchasePrice(g, date, true), Is.EqualTo(95));
                var poor = Rich(94);
                Assert.That(poor.BuyUsedCard(g.id, date, true), Is.False);
                Assert.That(poor.S.money, Is.EqualTo(94));
                var exact = Rich(95);
                Assert.That(exact.BuyUsedCard(g.id, date, true), Is.True);
                Assert.That(exact.S.money, Is.Zero);
                Assert.That(HardwareCatalog.UsedPrice(g, date), Is.EqualTo(100));
            }
            finally { g.usedPrice = previousPrice; }
        }

        [Test]
        public void ANewGameStartsWithOneGtx980TiAtTimesOne()
        {
            var sim = new ChapterOneSim();
            Assert.That(sim.Cards, Is.EqualTo(new[] { HardwareCatalog.Gtx980Ti }));
            Assert.That(sim.CardCompute, Is.EqualTo(1).Within(1e-9));
            Assert.That(sim.HeartbeatsPerSecond, Is.EqualTo(1).Within(1e-9));
            Assert.That(sim.MemoryCapacity, Is.EqualTo(6144));
            Assert.That(sim.LoadWatts, Is.EqualTo(250 + sim.Config.caseWatts));
        }

        [TestCase(HardwareCatalog.Gtx1080, 5, 27)]
        [TestCase(HardwareCatalog.Gtx1070, 6, 10)]
        [TestCase(HardwareCatalog.Rx480, 6, 29)]
        [TestCase(HardwareCatalog.Gtx1060, 7, 19)]
        [TestCase(HardwareCatalog.TitanXp, 8, 2)]
        public void CardsGoOnSaleOnTheirLaunchDay(string id, int month, int day)
        {
            var g = HardwareCatalog.Gpu(id);
            var launch = D(month, day);
            Assert.That(HardwareCatalog.OnSale(g, launch.AddDays(-1)), Is.False);
            Assert.That(HardwareCatalog.OnSale(g, launch), Is.True);
            var sim = Rich();
            Assert.That(sim.BuyCard(id, launch.AddDays(-1)), Is.False);
            Assert.That(sim.BuyCard(id, launch), Is.True);
            Assert.That(sim.Cards[sim.Cards.Count - 1], Is.EqualTo(id));
        }

        [Test]
        public void The1080TiIsOnlyARumourAndNeverOnSale()
        {
            var ti = HardwareCatalog.Gpu(HardwareCatalog.Gtx1080Ti);
            Assert.That(HardwareCatalog.OnSale(ti, GameCalendar.Ending), Is.False);
            Assert.That(HardwareCatalog.Listings(D(10, 31)).Contains(ti), Is.False);
            Assert.That(HardwareCatalog.Listings(GameCalendar.Ending)[HardwareCatalog.Listings(GameCalendar.Ending).Count - 1], Is.SameAs(ti));
            var sim = Rich();
            Assert.That(sim.BuyCard(HardwareCatalog.Gtx1080Ti, GameCalendar.Ending), Is.False);
            Assert.That(sim.S.gpuCount, Is.EqualTo(1));
            Assert.That(HardwareCatalog.Ownable(HardwareCatalog.Gtx1080Ti), Is.False);
        }

        [Test]
        public void ComputeVramAndWattsAreTheSumOfTheCards()
        {
            var sim = Rich();
            Assert.That(sim.BuyCard(HardwareCatalog.Gtx1080, D(7, 1)), Is.True);
            double both = HardwareCatalog.Gpu(HardwareCatalog.Gtx980Ti).compute + HardwareCatalog.Gpu(HardwareCatalog.Gtx1080).compute;
            Assert.That(sim.CardCompute, Is.EqualTo(both).Within(1e-9));
            Assert.That(sim.MemoryCapacity, Is.EqualTo(6144 + 8192));
            Assert.That(sim.CardWatts, Is.EqualTo(250 + 180));
            Assert.That(sim.HeartbeatsPerSecond, Is.EqualTo(both).Within(1e-9));
            Assert.That(sim.S.money, Is.EqualTo(100000 - 5399).Within(1e-9));
            // Both slots of the one case are full.
            Assert.That(sim.BuyCard(HardwareCatalog.Gtx1070, D(7, 1)), Is.False);
            Assert.That(sim.BuyNvme(D(8, 31)), Is.False);
            Assert.That(sim.BuyNvme(D(9, 1)), Is.True);
            Assert.That(sim.CardCompute, Is.EqualTo(both * HardwareCatalog.NvmeBoost).Within(1e-9));
        }

        [Test]
        public void ThePurchaseRaisesGpuBoughtWithTheModelForLuDaShi()
        {
            var sim = Rich();
            var got = new List<string>();
            sim.Signal += (n, a) => got.Add(a == null ? n : n + ":" + a);
            sim.BuyCard(HardwareCatalog.Gtx1080, D(6, 1));
            Assert.Contains("gpu.bought:gtx1080", got);
            foreach (var g in got) Assert.IsTrue(ChapterOneStoryVars.IsKnownSignal(g), g);
            Assert.That(HardwareCatalog.LudashiPercent(HardwareCatalog.Gtx1080), Is.EqualTo(99.7));
            Assert.That(HardwareCatalog.LudashiPercent(HardwareCatalog.Gtx1070), Is.EqualTo(98));
        }

        [Test]
        public void OldCardsLoseValueWhenNewOnesLaunch()
        {
            var ti = HardwareCatalog.Gpu(HardwareCatalog.Gtx980Ti);
            double may = HardwareCatalog.UsedPrice(ti, D(5, 26)), launch = HardwareCatalog.UsedPrice(ti, D(5, 27));
            double july = HardwareCatalog.UsedPrice(ti, D(7, 25)), december = HardwareCatalog.UsedPrice(ti, D(12, 31));
            Assert.That(launch, Is.LessThan(may));
            Assert.That(july, Is.LessThan(launch));
            Assert.That(december, Is.LessThanOrEqualTo(july));
            // §14.3: selling the 980 Ti on 喵鱼 brings back well under its old price (the 2016-10-06 retune moved it up from ¥2000).
            Assert.That(july, Is.InRange(ti.usedPrice * .6, ti.usedPrice * .9));
            Assert.That(december, Is.GreaterThanOrEqualTo(ti.usedPrice * HardwareCatalog.UsedFloor - 10));
        }

        [Test]
        public void SellingOnMiaoyuPaysTheAgreedPriceAndKeepsOneCard()
        {
            var sim = Rich(6000);
            var aug = D(8, 10);
            Assert.That(sim.SellCard(0, 100, aug), Is.False, "the last card stays");
            Assert.That(sim.BuyCard(HardwareCatalog.Gtx1080, aug), Is.True);
            double value = HardwareCatalog.UsedPrice(HardwareCatalog.Gpu(HardwareCatalog.Gtx980Ti), aug);
            Assert.That(sim.SellCard(0, value + 1, aug), Is.False, "no buyer pays above the market");
            double before = sim.S.money, haggled = Math.Floor(value * .85);
            Assert.That(sim.SellCard(0, haggled, aug), Is.True);
            Assert.That(sim.S.money, Is.EqualTo(before + haggled).Within(1e-9));
            Assert.That(sim.Cards, Is.EqualTo(new[] { HardwareCatalog.Gtx1080 }));
            Assert.That(sim.CardCompute, Is.EqualTo(HardwareCatalog.Gpu(HardwareCatalog.Gtx1080).compute).Within(1e-9));
        }

        [Test]
        public void EightNetCafeMachinesMakeASmallCluster()
        {
            var sim = Rich();
            Assert.That(sim.BuyCafeBox(D(9, 30)), Is.False);
            for (int i = 0; i < HardwareCatalog.CafeBoxLimit; i++) Assert.That(sim.BuyCafeBox(D(10, 2)), Is.True, "box " + i);
            Assert.That(sim.BuyCafeBox(D(10, 2)), Is.False);
            Assert.That(sim.S.caseCount, Is.EqualTo(1 + HardwareCatalog.CafeBoxLimit));
            Assert.That(sim.S.gpuCount, Is.EqualTo(1 + HardwareCatalog.CafeBoxLimit));
            // §14.3: the eight 970s add their compute to the 980 Ti's.
            double cluster = HardwareCatalog.Gpu(HardwareCatalog.Gtx980Ti).compute + HardwareCatalog.CafeBoxLimit * HardwareCatalog.Gpu(HardwareCatalog.Gtx970).compute;
            Assert.That(sim.CardCompute, Is.EqualTo(cluster).Within(1e-9));
            Assert.That(sim.S.breakerTripped, Is.False, "the cluster fits under the house's 3500 W");
            var reloaded = new ChapterOneSim(Clone(sim.S));
            Assert.That(reloaded.CardCompute, Is.EqualTo(cluster).Within(1e-9));
        }

        [Test]
        public void UsedCardsFromOtherSellersFollowTheMarket()
        {
            var sim = Rich(1000);
            var aug = D(8, 10);
            double price = HardwareCatalog.UsedPrice(HardwareCatalog.Gpu(HardwareCatalog.Gtx750Ti), aug);
            Assert.That(sim.BuyUsedCard(HardwareCatalog.Gtx750Ti, aug), Is.True);
            Assert.That(sim.S.money, Is.EqualTo(1000 - price).Within(1e-9));
            Assert.That(sim.CardCompute, Is.EqualTo(HardwareCatalog.Gpu(HardwareCatalog.Gtx980Ti).compute + HardwareCatalog.Gpu(HardwareCatalog.Gtx750Ti).compute).Within(1e-9));
        }

        [Test]
        public void AnRx480TripsTheBreakerAtMostOnce()
        {
            int burns = 0;
            for (long seed = 1; seed < 200; seed++)
            {
                var sim = Rich();
                sim.S.rngState = seed;
                sim.S.caseCount = 3;
                for (int i = 0; i < 3; i++)
                {
                    bool before = sim.S.pcieBurned;
                    sim.BuyCard(HardwareCatalog.Rx480, D(7, 1));
                    if (!before && sim.S.pcieBurned) { burns++; Assert.That(sim.S.breakerTripped, Is.True); Assert.That(sim.ResetBreaker(), Is.True); }
                }
                Assert.That(sim.S.gpuCount, Is.EqualTo(4));
            }
            Assert.That(burns, Is.InRange(20, 199));
        }

        [Test]
        public void SinglesDayTakesTwentyPercentOff()
        {
            var g = HardwareCatalog.Gpu(HardwareCatalog.Gtx1080);
            Assert.That(HardwareCatalog.Price(g, D(11, 11)), Is.EqualTo(Math.Round(5399 * .8)));
            Assert.That(HardwareCatalog.Price(g, D(11, 12)), Is.EqualTo(5399));
        }

        [Test]
        public void AnOldSaveCountsEachOfItsCardsAsTheStarting980Ti()
        {
            var old = Clone(new ChapterOneSim().S);
            old.caseCount = 2; old.gpuCount = 3; old.gpuModels = null;
            var sim = new ChapterOneSim(old);
            Assert.That(sim.Cards, Is.EqualTo(new[] { HardwareCatalog.LegacyModel, HardwareCatalog.LegacyModel, HardwareCatalog.LegacyModel }));
            Assert.That(sim.CardCompute, Is.EqualTo(3).Within(1e-9), "the old save keeps its compute");
            Assert.That(sim.MemoryCapacity, Is.EqualTo(3 * 6144));

            var empty = Clone(new ChapterOneSim().S);
            empty.gpuModels.Clear(); empty.gpuCount = 2;
            Assert.That(new ChapterOneSim(empty).Cards.Count, Is.EqualTo(2));

            var mismatch = Clone(new ChapterOneSim().S);
            mismatch.gpuCount = 2;
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(mismatch));
            var unknown = Clone(new ChapterOneSim().S);
            unknown.gpuModels[0] = "voodoo2";
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(unknown));
            var rumour = Clone(new ChapterOneSim().S);
            rumour.gpuModels[0] = HardwareCatalog.Gtx1080Ti;
            Assert.Throws<ArgumentException>(() => new ChapterOneSim(rumour));
        }

        [Test]
        public void TheOldStyleBuyAndSellStillWork()
        {
            var sim = new ChapterOneSim(config: new GameConfig { starterMoney = 650 });
            Assert.That(sim.BuyGpu(), Is.True);
            Assert.That(sim.Cards[1], Is.EqualTo(HardwareCatalog.Gtx750Ti));
            Assert.That(sim.SellGpu(), Is.True);
            Assert.That(sim.Cards, Is.EqualTo(new[] { HardwareCatalog.Gtx980Ti }), "the weakest card goes");
        }

        static GameState Clone(GameState s)
        {
            var c = (GameState)typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(s, null);
            c.gpuModels = s.gpuModels == null ? null : new List<string>(s.gpuModels);
            c.nodes = new List<NodeState>(s.nodes); c.edges = new List<EdgeState>(s.edges);
            return c;
        }
    }
}
