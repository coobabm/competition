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
            foreach (var id in new[] { "mlp", "rnn", "lenet", "resnet", "train.curve", XgSim.DataEntryNode }) sim.S.unlocked.Add(id);
            return sim;
        }

        static double Seconds(double mb, double mbps) => XgSim.PacedDownloadSeconds(mb, mbps, false, false);

        static void InstallReady(XgSim sim, string dataset)
        {
            var file = sim.S.dataFiles.Find(x => x.dataset == dataset && !x.installed);
            Assert.IsNotNull(file, dataset + " must have a downloaded file");
            Assert.IsTrue(sim.DataFileReady(file.fileId));
            Assert.IsTrue(sim.InstallDataFile(file.fileId));
            Assert.IsFalse(sim.InstallDataFile(file.fileId), "repeat drops are no-ops");
        }

        [Test]
        public void TheQueueDownloadsOneItemAtATimeAtTheLineSpeed()
        {
            var sim = Lab(); var host = new Host();
            var imagenet = XgCatalog.Dataset("imagenet"); var news = XgCatalog.Dataset("news");
            Assert.IsTrue(sim.BuyNode("imagenet.pack", host));
            Assert.IsTrue(sim.BuyNode("news.pack", host));
            double first = Seconds(imagenet.sizeMB, XgSim.BroadbandMBps[0]), second = Seconds(news.sizeMB, XgSim.BroadbandMBps[0]);
            Assert.That(first, Is.InRange(20, 45), "15 GB of ImageNet on the starting 20M line takes about half a minute");
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
            Assert.AreEqual(0, sim.PackSamples("imagenet"), "ready files do not count as training data");
            Assert.AreEqual(XgSim.PackBatches(imagenet), sim.BatchesArrived("imagenet"));
            Assert.IsFalse(sim.Owns("imagenet"));
            InstallReady(sim, "imagenet");
            Assert.AreEqual(imagenet.samples, sim.PackSamples("imagenet"), 1e-6);
            Assert.IsTrue(sim.Owns("imagenet"));
            sim.Tick(second, host);
            Assert.AreEqual(0, sim.PackSamples("news"));
            InstallReady(sim, "news");
            Assert.AreEqual(news.samples, sim.PackSamples("news"), 1e-6);
            Assert.AreEqual(0, sim.DownloadQueue().Count);
        }

        [Test]
        public void SmallTextPacksDownloadVisiblyAndNeedInstallation()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.unlocked.Add(XgSim.DataEntryNode);
            Assert.IsTrue(sim.BuyNode("spam.pack", host));
            Assert.IsTrue(sim.Downloading("spam"));
            Assert.That(sim.DownloadLeft("spam"), Is.InRange(2, 30));
            Assert.AreEqual(0, sim.PackSamples("spam"));
            sim.Tick(sim.DownloadLeft("spam") + 1, host);
            Assert.IsFalse(sim.Downloading("spam"));
            Assert.AreEqual(0, sim.PackSamples("spam"));
            InstallReady(sim, "spam");
            Assert.AreEqual(XgCatalog.Dataset("spam").samples, sim.PackSamples("spam"), 1e-6);
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
            Assert.IsNotNull(sim.BroadbandBlocker(out _), "300M waits for stage five");
            before = host.money;
            sim.Tick(1, host);
            Assert.AreEqual(before, host.money, 1e-9, "this month is paid");
            sim.Today = 20160801; sim.Tick(1, host);
            Assert.AreEqual(before - sim.BroadbandExtraPerMonth, host.money, 1e-9, "the extra fee each new month");
            host.money = 0; sim.Today = 20160901; sim.Tick(1, host);
            Assert.AreEqual(0, sim.BroadbandTier, "unpaid, the line goes back to ADSL");
        }

        [Test]
        public void TheStartingLineIsFastAndEveryUpgradeIsMeaningfullyFaster()
        {
            // 1 Mbps = 0.125 MB/s: the 20M line you start with is 2.5 MB/s, 100M fibre 12.5, 300M fibre 37.5.
            Assert.AreEqual(2.5, XgSim.BroadbandMBps[0], 1e-9);
            Assert.AreEqual("ADSL 20M", XgSim.BroadbandName(0, false));
            for (int i = 1; i < XgSim.BroadbandMBps.Length; i++)
                Assert.GreaterOrEqual(XgSim.BroadbandMBps[i], XgSim.BroadbandMBps[i - 1] * 3, "each tier is at least three times the one before");
            Assert.AreEqual(12.5, XgSim.BroadbandMBps[1], 1e-9); Assert.AreEqual("光纤 100M", XgSim.BroadbandName(1, false)); Assert.AreEqual("100M fibre", XgSim.BroadbandName(1, true));
            Assert.AreEqual(37.5, XgSim.BroadbandMBps[2], 1e-9); Assert.AreEqual("光纤 300M", XgSim.BroadbandName(2, false));
            Assert.AreEqual(2.5, new XgSim().LineMBps, 1e-9, "a new game starts on the 20M line");
        }

        [Test]
        public void TheUpgradeButtonNamesTheNextLineItsSpeedAndItsPrice()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.fundedAbility = XgSim.AbilityCount;
            Assert.IsFalse(sim.BroadbandUpgradeOffered);
            Assert.AreEqual("", sim.BroadbandUpgradeLabel(false, true), "nothing on offer before stage two");
            sim.S.stage = 2; sim.Today = 20160705;
            Assert.IsTrue(sim.BroadbandUpgradeOffered);
            Assert.AreEqual("升级宽带 ¥350 → 光纤 100M · 12.5 MB/s", sim.BroadbandUpgradeLabel(false, true));
            Assert.AreEqual("Upgrade line ¥350 → 100M fibre · 12.5 MB/s", sim.BroadbandUpgradeLabel(true, true));
            Assert.AreEqual("升级宽带 ¥350", sim.BroadbandUpgradeLabel(false, false));
            Assert.IsTrue(sim.BuyBroadband(host));
            Assert.AreEqual(12.5, sim.LineMBps, 1e-9);
            Assert.IsFalse(sim.BroadbandUpgradeOffered, "300M waits for stage five");
            sim.S.stage = 5;
            StringAssert.Contains("光纤 300M", sim.BroadbandUpgradeLabel(false, true));
            Assert.IsTrue(sim.BuyBroadband(host));
            Assert.AreEqual("", sim.BroadbandUpgradeLabel(false, true), "the top line");
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
        public void ANewPackBuysTheWholeFileAndInstallsItsSamplesOnce()
        {
            var sim = Lab(); var host = new Host();
            var d = XgCatalog.Dataset("poems");
            double before = host.money, price = sim.NodeCost(XgCatalog.Node("poems.pack"));
            Assert.IsTrue(sim.BuyNode("poems.pack", host));
            Assert.AreEqual(before - price, host.money, 1e-9);
            Assert.AreEqual(XgSim.PackBatches(d), sim.BatchesBought("poems"));
            Assert.AreEqual(d.samples, sim.DownloadQueue()[0].samples);
            Assert.AreEqual(d.sizeMB, sim.DownloadQueue()[0].sizeMB);
            Assert.AreEqual(0, sim.PackSamples("poems"));
            Assert.IsFalse(sim.BuyBatch("poems", host));
            sim.Tick(sim.DownloadLeft("poems") + 1, host);
            Assert.AreEqual(0, sim.PackSamples("poems"));
            InstallReady(sim, "poems");
            Assert.AreEqual(d.samples, sim.PackSamples("poems"), 1e-6);
            Assert.IsTrue(double.IsPositiveInfinity(sim.NextBatchPrice("poems")));
        }

        [Test]
        public void TheNoteOffersAnOlderPartialPackAsItsNextBatch()
        {
            var state = new XgState { stage = 5, stageVision = 5, stageSequence = 5,
                progressionVersion = XgSim.ProgressionSchemaWalls, downloadsVersion = XgSim.DownloadsSchema };
            foreach (var node in XgCatalog.Nodes) state.unlocked.Add(node.id);
            foreach (var ds in XgCatalog.Datasets)
            {
                state.owned.Add(ds.id);
                int batches = ds.id == "imagenet" ? 1 : XgSim.PackBatches(ds);
                state.batchesBought.Add(new XgLabelCount { dataset = ds.id, count = batches });
                state.batchesArrived.Add(new XgLabelCount { dataset = ds.id, count = batches });
            }
            var sim = new XgSim(state);
            var buy = XgGuide.DataPlan(sim, 6, 1e9);
            Assert.IsNotNull(buy, "an older partially downloaded pack retains its next-batch option");
            Assert.AreEqual("imagenet", buy.batch);
            Assert.AreEqual(sim.NextBatchPrice(buy.batch), buy.cost, 1e-9);
            Assert.AreEqual(XgCatalog.Dataset(buy.batch).dataWeight * XgSim.BatchSamples(XgCatalog.Dataset(buy.batch)), buy.gain, 1e-6);
        }

        [Test]
        public void TheDataPlanCountsTheWholeNewPackThatThePurchaseQueues()
        {
            var state = new XgState { stage = 5, stageVision = 5, stageSequence = 5,
                progressionVersion = XgSim.ProgressionSchemaWalls, downloadsVersion = XgSim.DownloadsSchema };
            foreach (var node in XgCatalog.Nodes) if (node.id != "imagenet.pack") state.unlocked.Add(node.id);
            foreach (var ds in XgCatalog.Datasets) if (ds.id != "imagenet") state.owned.Add(ds.id);
            var sim = new XgSim(state);
            var buy = XgGuide.DataPlan(sim, 6, 1e9);
            Assert.IsNotNull(buy);
            Assert.AreEqual("imagenet.pack", buy.target.id);
            Assert.IsNull(buy.batch);
            var dataset = XgCatalog.Dataset("imagenet");
            Assert.AreEqual(dataset.samples * dataset.dataWeight, buy.gain, 1e-6);
            Assert.IsTrue(sim.BuyNode(buy.target.id, new Host()));
            Assert.AreEqual(sim.DownloadQueue()[0].samples * dataset.dataWeight, buy.gain, 1e-6,
                "the planner counts the full file the purchase queues, before the existing noise adjustment");
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
            sim.Tick(10, host);
            Assert.AreEqual(0, host.money, 1e-9, "no tier and no welcome before 摆渡众包 is installed");
            Assert.AreEqual(0, sim.IncomePerSecond);
            Assert.AreEqual(1, sim.S.fundedAbility, "the welcome waits for the install");
            sim.AppReceipt(XgSim.CrowdApp).installed = true;
            sim.Tick(.01, host);
            int welcome = XgSim.AbilityResearchGrant[2];
            Assert.AreEqual(welcome + .01 * XgSim.CrowdTierIncome[2], host.money, 1e-6, "paid once, on install");
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

        [Test] public void CrowdEconomyUsesTheApprovedSmallSupplementAndKeepsPaidLedgers()
        {
            CollectionAssert.AreEqual(new double[]{0,0,.2,.8,1.5,3,5},XgSim.CrowdTierIncome);
            CollectionAssert.AreEqual(new int[]{0,0,24,96,180,360,600},XgSim.AbilityResearchGrant);
            var sim=new XgSim{GoldChance=0};var host=new Host{money=15120};
            sim.S.abilitiesEmerged.Add(2);sim.S.abilitiesEmerged.Add(3);sim.S.abilitiesEmerged.Add(4);
            sim.AppReceipt(XgSim.CrowdApp).installed=true;
            sim.S.fundedAbility=3;sim.S.crowdTier=3;sim.S.totalIncome=15120;
            sim.Tick(.25,host);
            Assert.AreEqual(15120+180+.25*1.5,host.money,1e-6,"old paid gifts are retained; only the unpaid fourth ability gets its new gift");
            Assert.AreEqual(4,sim.S.fundedAbility);
            StringAssert.Contains("1.5",sim.S.log.Last(l=>l.StartsWith("摆渡众包升级")),"fractional rates remain visible");
        }

        [TestCase(3, 60)]
        [TestCase(6, 600)]
        public void DelayedInstallationPaysEachReachedTierOnceAcrossSerializedReloads(int reached, double waitingSeconds)
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host { money = 0 };
            for (int i = 2; i <= reached; i++) sim.S.abilitiesEmerged.Add(i);
            sim.Tick(waitingSeconds, host);
            Assert.AreEqual(0, host.money, 1e-9, "waiting before installation earns nothing");
            Assert.AreEqual(1, sim.S.fundedAbility);
            Assert.AreEqual(0, sim.CrowdOrderIncome);

            sim = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S))) { GoldChance = 0 };
            Assert.IsFalse(sim.DesktopAppInstalled(XgSim.CrowdApp), "loading an uninstalled current save must not grant installation");
            var receipt = sim.AppReceipt(XgSim.CrowdApp);
            receipt.offered = receipt.downloaded = true;
            Assert.IsTrue(sim.AdvanceDesktopInstall(XgSim.CrowdApp));
            Assert.IsTrue(sim.AdvanceDesktopInstall(XgSim.CrowdApp));
            Assert.IsFalse(sim.DesktopAppInstalled(XgSim.CrowdApp));
            sim.Tick(1, host);
            Assert.AreEqual(0, host.money, 1e-9, "partial installation is not enough");
            Assert.IsTrue(sim.AdvanceDesktopInstall(XgSim.CrowdApp));
            Assert.IsFalse(sim.AdvanceDesktopInstall(XgSim.CrowdApp), "repeated completion is a no-op");

            double welcome = 0;
            for (int i = 2; i <= reached; i++) welcome += XgSim.AbilityResearchGrant[i];
            double rate = XgSim.CrowdTierIncome[reached];
            sim.Tick(.25, host);
            Assert.AreEqual(welcome + .25 * rate, host.money, 1e-6, "only tier gifts and the current tick; no pay for waiting before install");
            Assert.AreEqual(reached, sim.S.fundedAbility);
            Assert.AreEqual(reached, sim.S.crowdTier);
            Assert.AreEqual(reached - 1, sim.S.log.Count(l => l.StartsWith("摆渡众包升级")));

            for (int reload = 0; reload < 2; reload++)
            {
                sim = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S))) { GoldChance = 0 };
                double before = host.money;
                Assert.IsTrue(sim.DesktopAppInstalled(XgSim.CrowdApp));
                sim.Tick(2, host);
                Assert.AreEqual(before + 2 * rate, host.money, 1e-6, "serialized paid ledger prevents replaying all reached gifts");
                Assert.AreEqual(reached, sim.S.fundedAbility);
                Assert.AreEqual(reached - 1, sim.S.log.Count(l => l.StartsWith("摆渡众包升级")));
            }
        }

        [Test]
        public void UninstalledSignedContractsWaitForInstallationWithoutAnotherSigningBonus()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host { money = 0 };
            var contract = XgCatalog.Contract("cheque");
            sim.S.stage = 3;
            sim.S.best.Add(new XgBest { dataset = contract.dataset, acc = 1 });
            Assert.IsFalse(sim.CanSign(contract));
            Assert.IsFalse(sim.Sign(contract.id, host));
            Assert.AreEqual(0, host.money);
            sim.S.contracts.Add(contract.id); // A persisted contract with an uninstalled receipt.
            Assert.AreEqual(0, sim.ContractIncome(contract));
            Assert.AreEqual(0, sim.IncomePerSecond);
            sim.AppReceipt(XgSim.CrowdApp).installed = true;
            Assert.Greater(sim.ContractIncome(contract), 0);
            Assert.Greater(sim.IncomePerSecond, 0);
            Assert.IsFalse(sim.Sign(contract.id, host), "installation must not pay the signing bonus again");
            Assert.AreEqual(0, host.money);
            sim.AppReceipt(XgSim.CrowdApp).installed = false;
            Assert.AreEqual(0, sim.IncomePerSecond, "income rechecks the live installation state");
        }

        [Test]
        public void TheNoteSaysWhatIsDownloadingAndWhenTheLineIsSlow()
        {
            var sim = Lab(); var host = new Host();
            sim.S.stage = 4;
            // Something to train on (a whole pack from before the queue), then ImageNet's right and enough batches for the bar.
            Assert.IsTrue(sim.BuyNode("poems.pack", host));
            sim.Tick(sim.DownloadLeft("poems") + 1, host);
            InstallReady(sim, "poems");
            sim.S.epochs = 1; sim.S.assessments = 1; sim.S.treeIntroDelivered = true; // past the first-round steps, the 科技 page open
            sim.S.stage = 5;
            Assert.IsTrue(sim.BuyNode("news.pack", host));
            Assert.IsTrue(sim.BuyNode("spam.pack", host));
            sim.S.stage = 4;
            Assert.IsTrue(sim.BuyNode("imagenet.pack", host));
            // Lab starts at stage five; explicitly model a stage-four learner waiting for ability five.
            sim.S.abilitiesEmerged.RemoveAll(i => i >= 5);
            sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 4;
            Assert.That(sim.NextAbility, Is.EqualTo(5));
            double need = sim.SamplesThreshold(5) - sim.TrainedSamples - sim.QueuedDataSamples(out _);
            Assert.That(need, Is.LessThanOrEqualTo(0), "the two full files cover the data gap");
            var steps = XgGuide.Current(sim, host.money);
            var wait = steps.Find(x => x.id == "ability.download");
            Assert.IsNotNull(wait, string.Join(" | ", steps));
            StringAssert.StartsWith("等下载：ImageNet 还要 ", wait.zh);
            StringAssert.Contains(XgSim.ClockText(sim.LineQueueSeconds), wait.zh, "the last queued batch is what the bar waits for");
            Assert.IsTrue(sim.BroadbandBottleneck);
            var line = steps.Find(x => x.id == "broadband");
            Assert.IsNotNull(line, "ADSL is the bottleneck and fibre is on offer: " + string.Join(" | ", steps));
            StringAssert.Contains("光纤 100M", line.zh);
            StringAssert.Contains("升级宽带", line.zh, "the line says which button to press");
            StringAssert.Contains("UpgradeBroadband", line.target, "and the ring lands on it");
            Assert.AreEqual("data", line.tab);
            Assert.IsTrue(sim.BuyBroadband(host));
            Assert.IsNull(XgGuide.Current(sim, host.money).Find(x => x.id == "broadband" && x.zh.Contains("光纤 100M")));
        }
    }
}
