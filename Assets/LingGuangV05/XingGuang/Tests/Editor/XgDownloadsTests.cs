using System.Linq;
using LingGuangV05.Core.Hardware;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>节奏重做 §2.1–2.4: the download queue, the disk, broadband, batches and the crowd-order income tiers.</summary>
    public sealed class XgDownloadsTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000000;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        /// <summary>Stage five with the parents of the big packs owned (the stages' welcome payments are not counted here).</summary>
        static XgSim Lab()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 5;
            sim.S.fundedAbility = 5;
            foreach (var id in new[] { "mlp", "rnn", "lenet", "resnet" }) sim.S.unlocked.Add(id);
            return sim;
        }

        static double Seconds(double mb, double mbps) => mb / mbps * XgSim.DownloadTimeScale;

        [Test]
        public void TheQueueDownloadsOneItemAtATimeAtTheLineSpeed()
        {
            var sim = Lab(); var host = new Host();
            var imagenet = XgCatalog.Dataset("imagenet"); var news = XgCatalog.Dataset("news");
            Assert.IsTrue(sim.BuyNode("imagenet.pack", host));
            Assert.IsTrue(sim.BuyNode("news.pack", host));
            double first = Seconds(XgSim.BatchMB(imagenet), XgSim.BroadbandMBps[0]), second = Seconds(XgSim.BatchMB(news), XgSim.BroadbandMBps[0]);
            Assert.That(first, Is.InRange(120, 250), "15 GB of ImageNet on 4M ADSL takes minutes: fibre is the fix");
            Assert.AreEqual(first, sim.DownloadLeft("imagenet"), 1e-6);
            Assert.AreEqual(first + second, sim.DownloadLeft("news"), 1e-6, "news waits behind ImageNet");
            var queue = sim.DownloadQueue();
            Assert.AreEqual(new[] { "imagenet", "news" }, queue.Select(q => q.key).ToArray());
            Assert.IsTrue(queue[0].active); Assert.IsFalse(queue[1].active);

            sim.Tick(first - 5, host);
            Assert.IsTrue(sim.Downloading("imagenet") && !sim.Owns("imagenet"));
            Assert.AreEqual(0, sim.PackSamples("imagenet"), "samples arrive at the end of the item");
            Assert.AreEqual(0, sim.DownloadQueue()[1].doneMB, "the second item has not started");
            sim.Tick(6, host);
            Assert.AreEqual(XgSim.BatchSamples(imagenet), sim.PackSamples("imagenet"), 1e-6);
            Assert.AreEqual(1, sim.BatchesArrived("imagenet"));
            Assert.IsTrue(sim.Owns("imagenet"));
            sim.Tick(second, host);
            Assert.AreEqual(XgSim.BatchSamples(news), sim.PackSamples("news"), 1e-6);
            Assert.AreEqual(0, sim.DownloadQueue().Count);
        }

        [Test]
        public void SmallTextPacksArriveAtOnce()
        {
            var sim = new XgSim(); var host = new Host();
            Assert.IsTrue(sim.BuyNode("spam.pack", host));
            Assert.IsFalse(sim.Downloading("spam"));
            Assert.AreEqual(XgSim.BatchSamples(XgCatalog.Dataset("spam")), sim.PackSamples("spam"), 1e-6);
        }

        [Test]
        public void FasterBroadbandIsAPurchaseAndAMonthlyFee()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.fundedAbility = XgSim.AbilityCount; // no welcome payments in the wallet sums below
            Assert.IsNotNull(sim.BroadbandBlocker(out _), "fibre comes with stage two");
            Assert.IsFalse(sim.BuyBroadband(host));
            sim.S.stage = 2; sim.Today = 20160705;
            double price = sim.NextBroadbandPrice, before = host.money;
            Assert.AreEqual(XgSim.BroadbandInstall[1] + XgSim.BroadbandMonthly[1] - XgSim.BroadbandMonthly[0], price);
            Assert.IsTrue(sim.BuyBroadband(host));
            Assert.AreEqual(before - price, host.money, 1e-9);
            Assert.AreEqual(XgSim.BroadbandMBps[1], sim.LineMBps);
            Assert.IsNotNull(sim.BroadbandBlocker(out _), "100M waits for stage four");
            before = host.money;
            sim.Tick(1, host);
            Assert.AreEqual(before, host.money, 1e-9, "this month is paid");
            sim.Today = 20160801; sim.Tick(1, host);
            Assert.AreEqual(before - sim.BroadbandExtraPerMonth, host.money, 1e-9, "the extra fee each new month");
            host.money = 0; sim.Today = 20160901; sim.Tick(1, host);
            Assert.AreEqual(0, sim.BroadbandTier, "unpaid, the line goes back to ADSL");
        }

        [Test]
        public void AFullDiskPausesTheQueueUntilADriveIsBought()
        {
            var sim = Lab(); var host = new Host();
            // ImageNet bought before the queue (a whole pack) plus the 2.4M-picture junk crawl: more than the 400 GB free.
            sim.S.owned.Add("imagenet"); sim.S.unlocked.Add("imagenet.pack"); sim.S.dataOffers.Add("imagenet.junk");
            Assert.Greater(sim.DiskUsedMB, sim.DiskCapacityMB);
            Assert.IsNull(sim.DownloadBlocker(out _), "nothing queued, nothing blocked");
            Assert.IsTrue(sim.BuyNode("news.pack", host));
            sim.Tick(5, host);
            Assert.IsTrue(sim.Downloading("news"));
            Assert.AreEqual(0, sim.DownloadQueue()[0].doneMB);
            Assert.IsNotNull(sim.DownloadBlocker(out string en));
            StringAssert.Contains("Disk full", en);
            Assert.IsTrue(sim.DiskFullSeen);
            Assert.IsTrue(sim.DownloadQueue()[0].paused);
            var guide = XgGuide.Current(sim, host.money);
            var disk = guide.Find(x => x.id == "disk.full");
            Assert.IsNotNull(disk, string.Join(" | ", guide));
            StringAssert.StartsWith("硬盘满了", disk.zh);
            Assert.AreEqual(XgGuide.Shop, disk.app);

            Assert.IsTrue(sim.BuyDrive(host));
            Assert.AreEqual(1, sim.DrivesOwned);
            sim.Tick(sim.DownloadLeft("news") + 1, host);
            Assert.IsFalse(sim.Downloading("news"));
            Assert.IsNull(sim.DownloadBlocker(out _));
        }

        [Test]
        public void EachDriveCostsThirtyPercentMoreThanTheLast()
        {
            var sim = new XgSim(); var host = new Host();
            Assert.AreEqual(HardwareCatalog.PortableDrive.price, sim.NextDrivePrice);
            double last = 0, capacity = sim.DiskCapacityMB;
            for (int i = 0; i < 4; i++)
            {
                double price = sim.NextDrivePrice, before = host.money;
                if (last > 0) Assert.AreEqual(HardwareCatalog.DrivePriceGrowth, price / last, .01);
                Assert.IsTrue(sim.BuyDrive(host));
                Assert.AreEqual(before - price, host.money, 1e-9);
                last = price;
            }
            Assert.AreEqual(capacity + 4 * HardwareCatalog.PortableDrive.capacityMB, sim.DiskCapacityMB, 1e-6);
        }

        [Test]
        public void DataCleaningCompressesStoredData()
        {
            var sim = Lab();
            sim.S.owned.Add("imagenet");
            double stored = sim.StoredDataMB;
            Assert.AreEqual(XgCatalog.Dataset("imagenet").sizeMB, stored, 1e-6);
            sim.S.unlocked.Add(XgSim.DataCleanId);
            Assert.AreEqual(stored * (1 - XgSim.CleanCompression), sim.StoredDataMB, 1e-6);
        }

        [Test]
        public void BatchesGetDearerAndEachDownloadsItsShare()
        {
            var sim = Lab(); var host = new Host();
            var d = XgCatalog.Dataset("poems");
            int all = XgSim.PackBatches(d);
            Assert.That(all, Is.InRange(5, 10));
            Assert.IsNotNull(sim.BatchBlocker("poems", out _), "the pack node is the download right");
            Assert.IsTrue(sim.BuyNode("poems.pack", host));
            Assert.AreEqual(1, sim.BatchesBought("poems"));
            double baseline = sim.BatchPrice("poems", 1) / XgSim.BatchPriceGrowth;
            for (int k = 1; k < all; k++)
            {
                double price = sim.NextBatchPrice("poems"), before = host.money;
                Assert.AreEqual(baseline * System.Math.Pow(XgSim.BatchPriceGrowth, k), price, 1.0, "batch " + (k + 1) + " (prices are rounded up to whole yuan)");
                Assert.IsTrue(sim.BuyBatch("poems", host));
                Assert.AreEqual(before - price, host.money, 1e-9);
                sim.Tick(sim.DownloadLeft("poems") + .1, host);
                Assert.AreEqual(XgSim.BatchSamples(d) * (k + 1), sim.PackSamples("poems"), 1e-6);
            }
            Assert.AreEqual(d.samples, sim.PackSamples("poems"), 1e-6, "every batch adds up to the whole pack");
            Assert.IsNotNull(sim.BatchBlocker("poems", out _));
            Assert.IsFalse(sim.BuyBatch("poems", host), "no batch past the last");
            Assert.IsTrue(double.IsPositiveInfinity(sim.NextBatchPrice("poems")));
        }

        [Test]
        public void TheNoteOffersAnOwnedPackOnlyAsItsNextBatch()
        {
            var sim = Lab(); var host = new Host();
            foreach (var n in XgCatalog.Nodes)
                if (n.kind == XgNodeKind.Dataset && sim.Status(n, host) == XgSim.NodeStatus.Buyable) sim.BuyNode(n.id, host);
            sim.Tick(600, host);
            var buy = XgGuide.DataPlan(sim, sim.NextAbility, 1e9);
            Assert.IsNotNull(buy);
            Assert.IsNotNull(buy.batch, "every pack is owned: only batches are left");
            Assert.AreEqual(sim.NextBatchPrice(buy.batch), buy.cost, 1e-9);
            Assert.AreEqual(XgCatalog.Dataset(buy.batch).dataWeight * XgSim.BatchSamples(XgCatalog.Dataset(buy.batch)), buy.gain, 1e-6);
        }

        [Test]
        public void OldSavesKeepEveryPackTheyBoughtAsDownloaded()
        {
            var state = new XgState { stage = 5, stageVision = 5, stageSequence = 5, fundedAbility = 4 };
            state.unlocked.AddRange(new[] { "perceptron", "mlp", "rnn", "lenet", "resnet", "imagenet.pack", "news.pack" });
            state.owned.Add("imagenet"); state.owned.Add("news");
            state.dataOffers.Add("imagenet.junk");
            // The old countdowns: news and the junk crawl were still coming down.
            state.downloads.Add(new XgScore { key = "news", value = 40, count = 60 });
            state.downloads.Add(new XgScore { key = "imagenet.junk", value = 80, count = 90 });
            var sim = new XgSim(state);
            Assert.AreEqual(XgSim.DownloadsSchema, sim.S.downloadsVersion);
            Assert.AreEqual(0, sim.S.downloads.Count);
            Assert.IsFalse(sim.Downloading("news") || sim.Downloading("imagenet.junk"));
            Assert.AreEqual(1200000 + 2400000, sim.PackSamples("imagenet"), "the public pack and the junk crawl");
            Assert.AreEqual(5000000, sim.PackSamples("news"));
            Assert.IsTrue(sim.OwnsExtraPack("imagenet"));
            Assert.AreEqual(XgSim.PackBatches("imagenet"), sim.BatchesArrived("imagenet"));
            Assert.IsNotNull(sim.BatchBlocker("imagenet", out _), "a whole pack has no batch left to buy");
            Assert.LessOrEqual(sim.DiskUsedMB, sim.DiskCapacityMB, "an old save gets the room its data takes");
            Assert.AreEqual(4, sim.S.crowdTier, "the milestone payments it collected became the order tier it reached");
            Assert.AreEqual(XgSim.CrowdTierIncome[4], sim.CrowdOrderIncome);

            // The new fields survive a reload unchanged.
            var again = new XgSim(sim.S);
            Assert.AreEqual(4, again.S.crowdTier);
            Assert.AreEqual(sim.S.diskBonusMB, again.S.diskBonusMB);
            Assert.IsNotNull(again.S.downloadQueue); Assert.IsNotNull(again.S.batchesBought); Assert.IsNotNull(again.S.batchesArrived);
        }

        [Test]
        public void AnAbilityPaysASmallWelcomeAndRaisesTheIncomePerSecond()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host { money = 0 };
            Assert.AreEqual(0, sim.IncomePerSecond);
            sim.S.abilitiesEmerged.Add(2);
            sim.Tick(.01, host);
            int welcome = XgSim.AbilityResearchGrant[2];
            Assert.AreEqual(System.Math.Round(XgSim.CrowdTierIncome[2] * XgSim.WelcomeSeconds), welcome, "about two minutes of the new income");
            Assert.That(welcome, Is.LessThan(2000), "a welcome, not a chapter's budget");
            Assert.AreEqual(2, sim.S.crowdTier);
            Assert.AreEqual(XgSim.CrowdTierIncome[2], sim.IncomePerSecond, 1e-9);
            StringAssert.Contains("每秒", sim.S.log.Last(l => l.Contains("摆渡众包升级")));
            double after = host.money;
            sim.Tick(10, host);
            Assert.AreEqual(after + 10 * XgSim.CrowdTierIncome[2], host.money, 1e-6);

            sim.S.abilitiesEmerged.Add(3);
            sim.Tick(.01, host);
            Assert.AreEqual(3, sim.S.crowdTier);
            Assert.Greater(XgSim.CrowdTierIncome[3], XgSim.CrowdTierIncome[2]);
            for (int i = 3; i <= XgSim.AbilityCount; i++) Assert.Greater(XgSim.CrowdTierIncome[i], XgSim.CrowdTierIncome[i - 1], "each ability raises the tier");
        }

        [Test]
        public void TheNoteSaysWhatIsDownloadingAndWhenTheLineIsSlow()
        {
            var sim = Lab(); var host = new Host();
            sim.S.stage = 4;
            // Something to train on (a whole pack from before the queue), then ImageNet's right and enough batches for the bar.
            sim.S.owned.Add("poems"); sim.S.unlocked.Add("poems.pack");
            sim.S.epochs = 1; sim.S.assessments = 1; sim.S.treeIntroDelivered = true; // past the first-round steps, the 科技 page open
            Assert.IsTrue(sim.BuyNode("imagenet.pack", host));
            Assume.That(sim.NextAbility, Is.EqualTo(5));
            double need = sim.SamplesThreshold(5) - sim.TrainedSamples - sim.QueuedDataSamples(out _);
            for (int i = 0; need > 0 && i < 9; i++) { Assert.IsTrue(sim.BuyBatch("imagenet", host)); need = sim.SamplesThreshold(5) - sim.TrainedSamples - sim.QueuedDataSamples(out _); }
            Assume.That(need, Is.LessThanOrEqualTo(0));
            var steps = XgGuide.Current(sim, host.money);
            var wait = steps.Find(x => x.id == "ability.download");
            Assert.IsNotNull(wait, string.Join(" | ", steps));
            StringAssert.StartsWith("等下载：ImageNet 还要 ", wait.zh);
            StringAssert.Contains(XgSim.ClockText(sim.LineQueueSeconds), wait.zh, "the last queued batch is what the bar waits for");
            Assert.IsTrue(sim.BroadbandBottleneck);
            var line = steps.Find(x => x.id == "broadband");
            Assert.IsNotNull(line, "ADSL is the bottleneck and fibre is on offer: " + string.Join(" | ", steps));
            StringAssert.Contains("光纤 20M", line.zh);
            Assert.IsTrue(sim.BuyBroadband(host));
            Assert.IsNull(XgGuide.Current(sim, host.money).Find(x => x.id == "broadband" && x.zh.Contains("光纤 20M")));
        }
    }
}
