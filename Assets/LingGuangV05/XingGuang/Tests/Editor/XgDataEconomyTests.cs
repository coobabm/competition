using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Data economy (design v1.1 §11.8.3–11.8.4): sources, noise, cleaning, the flywheel and the pool size.</summary>
    public sealed class XgDataEconomyTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000, compute = 1;
            public double Compute => compute;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim AtStage(int stage, params string[] unlocked)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.stage = sim.S.stageVision = sim.S.stageSequence = stage;
            sim.S.fundedAbility = stage; // the stages' milestone payments are not what these tests count
            foreach (var id in unlocked) sim.S.unlocked.Add(id);
            return sim;
        }

        // Invoke the real download tick without unrelated payments/calendar changes.
        static void TickDownloads(XgSim sim, double seconds)
        {
            var tick = typeof(XgSim).GetMethod("TickDownloads", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(tick);
            tick.Invoke(sim, new object[] { seconds });
        }

        static void FinishDownloads(XgSim sim)
        {
            var queue = sim.DownloadQueue();
            Assert.IsNotEmpty(queue);
            TickDownloads(sim, queue.Max(x => x.eta) + 1);
            Assert.IsEmpty(sim.DownloadQueue(), "the real download tick delivers all queued files");
        }

        static void InstallReadyFiles(XgSim sim)
        {
            var files = sim.S.dataFiles.Where(x => !x.installed).ToArray();
            Assert.IsNotEmpty(files);
            foreach (var file in files)
            {
                Assert.IsTrue(sim.DataFileReady(file.fileId));
                Assert.IsFalse(sim.InstallDataFile(file.fileId), "Data Import is still required");
            }
            // This fixture grants the prerequisite; the data files themselves use the manual install API.
            sim.S.unlocked.Add(XgSim.DataEntryNode);
            foreach (var file in files)
            {
                Assert.IsTrue(sim.InstallDataFile(file.fileId));
                double samples = sim.PackSamples(file.dataset);
                Assert.IsFalse(sim.InstallDataFile(file.fileId), "duplicate installation is a no-op");
                Assert.AreEqual(samples, sim.PackSamples(file.dataset));
            }
        }

        // ───── catalog ─────

        [Test]
        public void JunkPacksCostAThirdCarryTwiceTheSamplesAndAreDirty()
        {
            var junk = XgSim.DataOfferCatalog.Where(o => o.source == XgDataSource.Junk).ToList();
            Assert.That(junk.Count, Is.GreaterThanOrEqualTo(10));
            foreach (var o in junk)
            {
                var pack = XgSim.DataOfferDef(o.datasetId + ".pack");
                Assert.IsNotNull(pack, o.id);
                double basePrice = XgCatalog.Node(o.datasetId + ".pack").cost * XgSim.StoryResearchPrice;
                Assert.AreEqual(LingGuangV05.Core.PurchasePricing.AtLeastHundred(System.Math.Round(basePrice * XgSim.JunkPriceFactor)), o.price, o.id);
                Assert.AreEqual(pack.samples * XgSim.JunkSampleFactor, o.samples, o.id);
                Assert.That(o.noise, Is.InRange(.2, .4), o.id);
                Assert.That(o.month, Is.InRange(201606, 201612), o.id);
                Assert.IsFalse(string.IsNullOrEmpty(o.name) || string.IsNullOrEmpty(o.nameEn) || string.IsNullOrEmpty(o.description) || string.IsNullOrEmpty(o.descriptionEn), o.id);
            }
            Assert.IsTrue(junk.Any(o => o.name.Contains("百万条微博语料")));
            Assert.IsTrue(junk.Any(o => o.name.Contains("十万表情包")));
        }

        [Test]
        public void EveryDataNodeIsAPublicOfferWithLittleNoise()
        {
            foreach (var n in XgCatalog.Nodes.Where(n => n.kind == XgNodeKind.Dataset))
            {
                var o = XgSim.DataOfferDef(n.id);
                Assert.IsNotNull(o, n.id);
                Assert.AreEqual(XgDataSource.Public, o.source);
                Assert.AreEqual(LingGuangV05.Core.PurchasePricing.AtLeastHundred(System.Math.Ceiling(n.cost * XgSim.StoryResearchPrice)), o.price, n.id);
                Assert.That(o.noise, Is.InRange(0, .02), n.id);
                Assert.AreEqual(XgSim.StageMonth(n.stage), o.month, n.id);
            }
            foreach (var o in XgSim.DataOfferCatalog)
                Assert.AreEqual(1, XgSim.DataOfferCatalog.Count(x => x.id == o.id), "unique id " + o.id);
        }

        [Test]
        public void BigPacksDownloadOverTime()
        {
            // Real 2016 sizes on the starting 20M ADSL: text arrives at once, pictures take a while, ImageNet's batch of 120k images about half a minute.
            Assert.AreEqual(0, XgSim.DataOfferDef("spam.pack").downloadSec);
            Assert.That(XgSim.DataOfferDef("news.pack").downloadSec, Is.LessThan(5), "a batch of the news corpus is a few seconds at most");
            Assert.That(XgSim.DataOfferDef("imagenet.pack").downloadSec, Is.InRange(20, 60));
            Assert.That(XgSim.DataOfferDef("cifar.junk").downloadSec, Is.GreaterThan(0), "100k captcha photos come down through 迅雷");
        }

        // ───── shop API ─────

        [Test]
        public void TheShopSellsPublicAndJunkPacksReleasedByNow()
        {
            var sim = AtStage(1);
            var ids = sim.ShopDataOffers().Select(o => o.id).ToList();
            CollectionAssert.Contains(ids, "spam.pack");
            CollectionAssert.Contains(ids, "spam.junk");
            CollectionAssert.DoesNotContain(ids, "mnist.junk", "released in July, stage 2");
            CollectionAssert.DoesNotContain(ids, "mnist.crowd");
            CollectionAssert.DoesNotContain(ids, "imagenet.story");
            foreach (var o in sim.ShopDataOffers())
            {
                Assert.That(o.source == XgDataSource.Public || o.source == XgDataSource.Junk, o.id);
                Assert.That(o.month, Is.LessThanOrEqualTo(sim.CurrentMonth), o.id);
            }
            // The newspaper corpus is stage five's data now (参数量与数据量主线), and so is its junk twin.
            sim.S.stage = 5;
            ids = sim.ShopDataOffers().Select(o => o.id).ToList();
            CollectionAssert.Contains(ids, "news.junk");
            Assert.IsTrue(sim.Offer("news.junk").isNew == (sim.CurrentMonth == 201608));
        }

        [Test]
        public void BuyingThePublicOfferIsBuyingTheTreeNode()
        {
            var sim = AtStage(1, "perceptron"); var host = new Host();
            Assert.IsTrue(sim.Offer("spam.pack").available);
            double price = sim.Offer("spam.pack").price;
            Assert.AreEqual(750, price, "ordinary spam pack base price now follows the hundred-yuan policy");
            Assert.AreEqual(sim.NodeCost(XgCatalog.Node("spam.pack")), price);
            Assert.IsTrue(sim.BuyDataOffer("spam.pack", host));
            Assert.IsTrue(sim.Has("spam.pack"));
            Assert.IsFalse(sim.Owns("spam"), "purchase does not install");
            Assert.AreEqual(0, sim.PackSamples("spam"));
            Assert.AreEqual(100000 - price, host.money, 1e-9);
            FinishDownloads(sim);
            Assert.IsFalse(sim.Owns("spam"), "download completion does not install");
            Assert.AreEqual(0, sim.PackSamples("spam"));
            InstallReadyFiles(sim);
            Assert.IsTrue(sim.Owns("spam"));
            Assert.AreEqual(100000 - price, host.money, 1e-9);
            var o = sim.Offer("spam.pack");
            Assert.IsTrue(o.owned); Assert.IsFalse(o.available);
            Assert.IsFalse(sim.BuyDataOffer("spam.pack", host), "never twice");
            Assert.AreEqual(100000 - price, host.money, 1e-9);
        }

        [Test]
        public void AJunkPackDownloadsThenAddsSamplesAndNoise()
        {
            var sim = AtStage(3, "mlp", "lenet"); var host = new Host();
            XgDataOffer bought = null; sim.DataOfferBought += o => bought = o;
            double price = sim.Offer("cifar.junk").price;
            Assert.AreEqual(Math.Round(XgSim.DataOfferDef("cifar.pack").price * XgSim.JunkPriceFactor), price, 1e-9, "a third of the public pack");
            Assert.IsTrue(sim.BuyDataOffer("cifar.junk", host));
            Assert.IsNotNull(bought);
            Assert.AreEqual(100000 - price, host.money, 1e-9);
            var o = sim.Offer("cifar.junk");
            Assert.IsTrue(o.owned && o.downloading);
            Assert.AreEqual(0, o.downloadProgress, 1e-9);
            Assert.AreEqual(0, sim.PackSamples("cifar"), "nothing until it is down");
            Assert.IsFalse(sim.OwnsExtraPack("cifar"));
            sim.Tick(o.downloadSec / 2, host);
            Assert.AreEqual(.5, sim.Offer("cifar.junk").downloadProgress, .05);
            sim.Tick(o.downloadSec, host);
            Assert.IsFalse(sim.Offer("cifar.junk").downloading);
            Assert.AreEqual(0, sim.PackSamples("cifar"), "downloaded files are not installed");
            Assert.IsFalse(sim.OwnsExtraPack("cifar"));
            InstallReadyFiles(sim);
            Assert.AreEqual(100000, sim.PackSamples("cifar"));
            Assert.IsTrue(sim.Owns("cifar") && sim.DatasetAvailable("cifar"));
            Assert.AreEqual(100000 * .25, sim.DataNoise("cifar"), 1e-6);
            Assert.AreEqual(.25, sim.NoiseRatio("cifar"), 1e-3);
            Assert.IsFalse(sim.S.owned.Contains("cifar"), "the public pack is a separate purchase");
            Assert.IsFalse(sim.BuyDataOffer("cifar.junk", host));
        }

        [Test]
        public void SuperMembershipFinishesAJunkDownload()
        {
            var sim = AtStage(3, "mlp", "lenet"); var host = new Host();
            sim.BuyDataOffer("cifar.junk", host);
            double before = host.money, cost = sim.AccelerateOfferCost("cifar.junk");
            Assert.That(cost, Is.GreaterThanOrEqualTo(50));
            Assert.IsTrue(sim.AccelerateOffer("cifar.junk", host));
            Assert.AreEqual(before - cost, host.money, 1e-9);
            Assert.IsFalse(sim.Offer("cifar.junk").downloading);
            Assert.AreEqual(0, sim.PackSamples("cifar"), "acceleration does not install");
            InstallReadyFiles(sim);
            Assert.AreEqual(before - cost, host.money, 1e-9, "installation does not charge again");
            Assert.AreEqual(100000, sim.PackSamples("cifar"));
            Assert.IsFalse(sim.AccelerateOffer("cifar.junk", host));
        }

        [Test]
        public void JunkPacksWaitForTheirStage()
        {
            var sim = AtStage(1); var host = new Host();
            Assert.IsFalse(sim.BuyDataOffer("imagenet.junk", host));
            Assert.AreEqual(100000, host.money);
            Assert.IsFalse(sim.Offer("imagenet.junk").available);
        }

        // ───── noise ─────

        [Test]
        public void TheNoiseShareCountsEveryRowNotJustHandLabels()
        {
            var sim = AtStage(2, "mlp"); var host = new Host();
            Assert.IsTrue(sim.BuyDataOffer("mnist.pack", host));
            FinishDownloads(sim);
            Assert.AreEqual(0, sim.PackSamples("mnist"));
            InstallReadyFiles(sim);
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 1000 });
            sim.S.noise.Add(new XgLabelCount { dataset = "mnist", count = 100 });
            double packNoise = 60000 * XgSim.DataOfferDef("mnist.pack").noise;
            Assert.AreEqual(packNoise, sim.DataNoise("mnist"), 1e-9);
            Assert.AreEqual((100 + packNoise) / (61000 + 100), sim.NoiseRatio("mnist"), 1e-9);
        }

        [Test]
        public void LittleNoiseIsDrownedOutAndMuchNoiseFlipsCards()
        {
            var sim = AtStage(2);
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 1000 });
            sim.S.crowdNoise.Add(new XgLabelCount { dataset = "spam", count = 20 });
            Assert.AreEqual(.02, sim.NoiseRatio("spam"), 1e-9);
            Assert.Less(sim.BoardFlipRate("spam"), .001, "2% costs next to nothing");
            sim.S.crowdNoise[0].count = 450;
            Assert.AreEqual(.45, sim.NoiseRatio("spam"), 1e-9);
            Assert.AreEqual(.45, sim.BoardFlipRate("spam"), 1e-9, "past the tolerance every noisy row flips");
            sim.S.crowdNoise[0].count = 900;
            Assert.AreEqual(.5, sim.NoiseRatio("spam"), 1e-9, "capped at a coin toss");
        }

        [Test, Timeout(900000)] // three 160-epoch LeNet trainings: ~25 s on .NET, several minutes under the editor's Mono
        public void JunkNoiseHurtsTheBoardAndCleaningWinsMostOfItBack()
        {
            double Acc(double noise, bool clean)
            {
                var sim = AtStage(3);
                foreach (var n in XgCatalog.Nodes)
                    if (n.stage <= 3 && n.tree != "label" && n.kind != XgNodeKind.Dataset && n.kind != XgNodeKind.Breakthrough && n.kind != XgNodeKind.Project && n.id != XgSim.DataCleanId)
                        sim.S.unlocked.Add(n.id);
                sim.S.desksOpen.Add("mnist");
                sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 20000 });
                if (noise > 0) sim.S.crowdNoise.Add(new XgLabelCount { dataset = "mnist", count = 20000 * noise });
                if (clean) sim.S.unlocked.Add(XgSim.DataCleanId);
                var host = new Host { compute = 4 };
                var run = sim.S.vision; run.arch = "lenet"; run.dataset = "mnist"; run.depth = 3; run.width = 2;
                double sum = 0;
                for (int i = 0; i < 160; i++) { sim.TrainEpoch(XgTrack.Vision, host, false); if (i >= 140) sum += run.valAcc; }
                return sum / 20;
            }
            double clean = Acc(0, false), dirty = Acc(.35, false), cleaned = Acc(.35, true);
            Assert.Less(dirty, clean - .05, "a dirty pack alone pulls accuracy down");
            Assert.Greater(cleaned, dirty, "数据清洗 halves the noise");
        }

        [Test]
        public void DataCleaningHalvesJunkAndCrowdNoiseButNotPublicNoise()
        {
            var sim = AtStage(5, "mlp", "rnn"); var host = new Host();
            Assert.IsTrue(sim.BuyDataOffer("news.pack", host));
            Assert.IsTrue(sim.BuyDataOffer("news.junk", host));
            FinishDownloads(sim);
            Assert.AreEqual(0, sim.PackSamples("news"));
            InstallReadyFiles(sim);
            sim.S.crowdNoise.Add(new XgLabelCount { dataset = "news", count = 100 });
            double pub = 5e6 * XgSim.DataOfferDef("news.pack").noise, junk = 1e7 * .35;
            Assert.AreEqual(pub + junk + 100, sim.DataNoise("news"), 1e-3);
            Assert.AreEqual(.35, sim.Offer("news.junk").effectiveNoise, 1e-9);
            sim.S.unlocked.Add(XgSim.DataCleanId);
            Assert.AreEqual(pub + (junk + 100) * .5, sim.DataNoise("news"), 1e-3);
            Assert.AreEqual(.175, sim.Offer("news.junk").effectiveNoise, 1e-9);
            Assert.AreEqual(.02, sim.Offer("news.pack").effectiveNoise, 1e-9);
        }

        [Test]
        public void DataCleaningIsAResearchNodeBesideAugmentation()
        {
            var node = XgCatalog.Node(XgSim.DataCleanId);
            Assert.IsNotNull(node);
            Assert.AreEqual(XgCatalog.Node("augment").parent, node.parent);
            Assert.AreEqual(XgCatalog.Node("augment").stage, node.stage);
            Assert.IsNotNull(XgCatalog.ResearchItem(XgSim.DataCleanId));
        }

        [Test]
        public void TheAuditCleanUpReachesOutsideNoiseAfterOwnMistakes()
        {
            var sim = AtStage(3, "label.audit"); var host = new Host();
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 1000 });
            sim.S.noise.Add(new XgLabelCount { dataset = "spam", count = 4 });
            sim.S.crowdNoise.Add(new XgLabelCount { dataset = "spam", count = 20 });
            Assert.AreEqual(10, sim.CleanNoise("spam", host));
            Assert.AreEqual(0, sim.Noise("spam"), 1e-9, "own mistakes go first");
            Assert.AreEqual(14, sim.DataNoise("spam"), 1e-9);
            Assert.AreEqual(100000 - 10 * XgSim.NoiseCleanMoneyPerItem, host.money, 1e-9);
        }

        // ───── pool size ─────

        [Test]
        public void ThePoolGrowsWithTheLogOfTheSamplesPastFourThousand()
        {
            Assert.AreEqual(1, XgSim.BoardPoolSize(0));
            Assert.AreEqual(500, XgSim.BoardPoolSize(500));
            Assert.AreEqual(XgBoardData.PoolLimit, XgSim.BoardPoolSize(XgBoardData.PoolLimit), "hand-label counts are unchanged");
            int pack = XgSim.BoardPoolSize(60000), junk = XgSim.BoardPoolSize(120000), imagenet = XgSim.BoardPoolSize(1.2e6);
            Assert.That(pack, Is.InRange(14000, 16000));
            Assert.Greater(junk, pack); Assert.Greater(imagenet, junk);
            Assert.That(XgSim.BoardPoolSize(1e30), Is.EqualTo(XgSim.PoolCeiling));
        }

        [Test]
        public void ABigPacksExtraCardsOpenUpAsTheBoardTrains()
        {
            int limit = XgBoardData.PoolLimit;
            Assert.AreEqual(limit, XgSim.BoardPoolAt(60000, 0), "familiar cards first");
            Assert.AreEqual(limit + 1000, XgSim.BoardPoolAt(60000, 1000 * XgSim.PoolGrowth));
            Assert.AreEqual(XgSim.BoardPoolSize(60000), XgSim.BoardPoolAt(60000, 1e9));
            Assert.AreEqual(500, XgSim.BoardPoolAt(500, 1e9), "small datasets are unaffected");
        }

        // ───── crowd and story ─────

        [Test]
        public void ACrowdTaskTricklesInPaidLabelsWithSomeMistakes()
        {
            var sim = AtStage(2); var host = new Host();
            sim.S.desksOpen.Add("danmu");
            Assert.IsTrue(sim.BuyDataOffer("danmu.crowd", host));
            Assert.IsTrue(sim.CrowdPosted("danmu"));
            double before = sim.Samples("danmu");
            sim.Tick(10, host);
            var o = XgSim.DataOfferDef("danmu.crowd");
            Assert.AreEqual(before + 10 * o.samples, sim.Samples("danmu"), 1e-6);
            Assert.AreEqual(10 * o.samples * o.noise, sim.DataNoise("danmu"), 1e-6);
            Assert.That(o.noise, Is.InRange(.05, .1));
            Assert.AreEqual(100000 - 10 * o.samples * o.price, host.money, 1e-6);
            host.money = 0;
            sim.Tick(1, host);
            Assert.IsFalse(sim.CrowdPosted("danmu"), "taken down when the money runs out");
        }

        [Test]
        public void LaoZhouOffersImageNetThroughHisSchoolMail()
        {
            var sim = AtStage(4, "resnet"); var host = new Host();
            var lines = new List<string>(); sim.StoryDataOffered += (o, zh, en) => lines.Add(o.id);
            sim.Tick(1, host); sim.Tick(1, host);
            Assert.IsEmpty(lines, "no offer before the player posts");
            Assert.IsFalse(sim.Offer("imagenet.story").available);
            Assert.IsFalse(sim.DataForumFlag("asked.imagenet"));
            Assert.IsTrue(sim.AskForData("imagenet"));
            Assert.IsTrue(sim.DataForumFlag("asked.imagenet"));
            Assert.IsFalse(sim.DataForumFlag("answered.imagenet"));
            Assert.IsFalse(sim.AskForData("imagenet"), "one request cannot be posted twice");
            sim.Tick(XgSim.DataReplySeconds - 1, host);
            Assert.IsEmpty(lines, "a post does not instantly invent an answer");
            Assert.IsFalse(sim.Offer("imagenet.story").available);
            sim.Tick(2, host); sim.Tick(1, host);
            Assert.IsTrue(sim.DataForumFlag("answered.imagenet"));
            CollectionAssert.Contains(lines, "imagenet.story");
            Assert.AreEqual(1, lines.Count(x => x == "imagenet.story"), "said once");
            var offer = sim.Offer("imagenet.story");
            Assert.IsTrue(offer.available);
            Assert.Less(offer.price, XgSim.DataOfferDef("imagenet.pack").price);
            Assert.Less(offer.noise, .01);
            Assert.IsTrue(sim.BuyDataOffer("imagenet.story", host));
            Assert.IsTrue(sim.Has("imagenet.pack") && sim.S.owned.Contains("imagenet"));
            Assert.IsTrue(sim.Downloading("imagenet"));
            Assert.IsFalse(sim.Offer("imagenet.pack").available, "the public pack is the same data");
            sim.Tick(offer.downloadSec + 1, host);
            Assert.IsFalse(sim.Downloading("imagenet"));
            Assert.AreEqual(0, sim.PackSamples("imagenet"), "the story reply and download do not install");
            InstallReadyFiles(sim);
            Assert.AreEqual(1200000, sim.PackSamples("imagenet"), "counted once");
            Assert.AreEqual(1200000, sim.Samples("imagenet"));
        }

        [Test]
        public void StoryDataIsNotOfferedBeforeItsStage()
        {
            var sim = AtStage(3); var host = new Host();
            sim.Tick(1, host);
            Assert.IsFalse(sim.Offer("imagenet.story").available);
            Assert.IsFalse(sim.BuyDataOffer("imagenet.story", host));
        }

        // ───── flywheel ─────

        static XgSim WithContract(int stage)
        {
            var sim = AtStage(stage);
            sim.S.desksOpen.Add("danmu");
            sim.S.best.Add(new XgBest { dataset = "danmu", acc = .9, arch = "rnn" });
            sim.S.contracts.Add("danmaku");
            return sim;
        }

        [Test]
        public void AMetContractSendsBackLogsSlowlyBeforeStageFive()
        {
            var c = XgCatalog.Contract("danmaku");
            Assert.AreEqual(c.income * XgSim.LogPerIncome, XgSim.LogPerSec(c), 1e-9);
            var early = WithContract(3); var host = new Host();
            early.Tick(10, host);
            Assert.AreEqual(10 * c.income * XgSim.LogPerIncome * XgSim.LogEarlyFactor, early.Logs("danmu"), 1e-6);
            var late = WithContract(5);
            late.Tick(10, host);
            Assert.AreEqual(10 * c.income * XgSim.LogPerIncome, late.Logs("danmu"), 1e-6);
            Assert.IsTrue(late.S.log.Exists(l => l.Contains("聊天记录")));

            var unmet = WithContract(5);
            unmet.S.best[0].acc = .5;
            unmet.Tick(10, host);
            Assert.AreEqual(0, unmet.Logs("danmu"), "only contracts that meet their threshold");
        }

        [Test]
        public void HandLabellingALogIsCleanAndWorthMoreSamples()
        {
            var sim = WithContract(5); var host = new Host();
            sim.S.logs.Add(new XgLabelCount { dataset = "danmu", count = 2 });
            var card = sim.Card("danmu");
            double before = sim.Samples("danmu");
            var r = sim.Answer("danmu", card.truth, host);
            Assert.IsTrue(r.correct);
            Assert.AreEqual((int)XgSim.LogHandSamples, r.samples);
            Assert.AreEqual(before + XgSim.LogHandSamples, sim.Samples("danmu"), 1e-9);
            Assert.AreEqual(1, sim.Logs("danmu"), 1e-9);
            Assert.AreEqual(1, sim.S.combo);
            Assert.AreEqual(0, sim.NoiseRatio("danmu"), 1e-9);
            var wrong = sim.Answer("danmu", !sim.Card("danmu").truth, host);
            Assert.IsFalse(wrong.correct);
            Assert.AreEqual(1, sim.Logs("danmu"), 1e-9, "a wrong answer labels nothing");
        }

        [Test]
        public void TheModelLabelsLogsAsWellAsItIsRight()
        {
            var sim = WithContract(5); var host = new Host();
            sim.S.logs.Add(new XgLabelCount { dataset = "danmu", count = 1000 });
            // The checkpoint's score is on the dataset's own scale; the yes/no cards it labels are wrong as often as 1 − its binary accuracy.
            double noise = 1 - XgSim.BinaryAccuracy("danmu", .9);
            Assert.That(noise, Is.GreaterThan(0).And.LessThan(.2));
            Assert.AreEqual(noise, sim.LogAutoNoise("danmu"), 1e-9);
            Assert.AreEqual(1000, sim.AutoLabelLogs("danmu", host));
            Assert.AreEqual(0, sim.Logs("danmu"), 1e-9);
            Assert.AreEqual(1000 * (1 - noise) * XgSim.PseudoLabelWorth, sim.ExtraSamples("danmu"), 1e-6, "labelled by itself, a log is worth a fraction of a hand label");
            Assert.AreEqual(1000 * noise, sim.Noise("danmu"), 1e-6, "its mistakes are its own noise (近亲繁殖)");
            Assert.AreEqual(1000, sim.S.logsAuto, 1e-9);
        }

        [Test]
        public void AutomaticLogLabellingRunsEveryTickAndNeedsACheckpoint()
        {
            var none = AtStage(5);
            Assert.IsFalse(none.SetLogAuto("news", true));
            var sim = WithContract(5); var host = new Host();
            Assert.IsTrue(sim.SetLogAuto("danmu", true));
            sim.Tick(5, host);
            Assert.AreEqual(0, sim.Logs("danmu"), 1e-6, "labelled as fast as they come");
            Assert.Greater(sim.ExtraSamples("danmu"), 0);
            Assert.IsTrue(sim.SetLogAuto("danmu", false));
            sim.Tick(5, host);
            Assert.Greater(sim.Logs("danmu"), 0);
        }

        [Test]
        public void TheLogPileIsCapped()
        {
            var sim = WithContract(5); var host = new Host();
            sim.S.logs.Add(new XgLabelCount { dataset = "danmu", count = XgSim.LogPileCap - 1 });
            sim.Tick(10, host);
            Assert.AreEqual(XgSim.LogPileCap, sim.Logs("danmu"), 1e-6);
        }

        [Test]
        public void ThePrivacyRuleComesFromTheLogs()
        {
            var sim = WithContract(5);
            var cards = sim.RuleCards();
            var privacy = cards.Find(c => c.id == "privacy");
            Assert.That(privacy == null || privacy.source == "从没教过");
            sim.Tick(1, new Host());
            cards = sim.RuleCards();
            privacy = cards.Find(c => c.id == "privacy");
            Assert.IsNotNull(privacy);
            Assert.AreEqual("不偷看聊天记录", privacy.text);
            Assert.AreEqual("Don't read people's chats", privacy.textEn);
            Assert.AreNotEqual("从没教过", privacy.source);
            Assert.That(cards.Count, Is.LessThanOrEqualTo(XgSim.MaxRuleCards));
        }

        [Test]
        public void EveryRuleFitsOnThePageWhenAllAreEarned()
        {
            var sim = WithContract(5);
            sim.S.logsTotal = 1;
            foreach (var c in XgCatalog.Contracts) sim.S.contracts.Add(c.id);
            foreach (var d in XgCatalog.Datasets) sim.S.best.Add(new XgBest { dataset = d.id, acc = .5 });
            var cards = sim.RuleCards();
            Assert.AreEqual(XgSim.MaxRuleCards, cards.Count);
            Assert.IsTrue(cards.Exists(c => c.id == "privacy"));
        }

        // ───── saves ─────

        [Test]
        public void DataEconomyStateSurvivesASaveRoundTrip()
        {
            var sim = WithContract(5); var host = new Host();
            sim.S.unlocked.Add("mlp");
            sim.BuyDataOffer("danmu.junk", host);
            sim.SetCrowd("danmu", true);
            sim.Tick(3, host);
            sim.SetLogAuto("danmu", true);
            var again = new XgSim(sim.S);
            Assert.IsTrue(again.OfferOwned("danmu.junk"));
            Assert.IsTrue(again.CrowdPosted("danmu"));
            Assert.IsTrue(again.LogAutoOn("danmu"));
            Assert.AreEqual(sim.Samples("danmu"), again.Samples("danmu"), 1e-9);
            Assert.AreEqual(sim.DataNoise("danmu"), again.DataNoise("danmu"), 1e-9);
            Assert.AreEqual(sim.S.logsTotal, again.S.logsTotal, 1e-9);
        }

        [Test]
        public void OldSavesWithoutDataEconomyFieldsLoad()
        {
            var state = new XgState();
            state.owned.Add("mnist");
            state.dataOffers = null; state.dataExtra = null; state.crowdNoise = null; state.dataCleaned = null; state.crowdOn = null;
            state.storyOffered = null; state.logs = null; state.logAuto = null; state.logsTotal = double.NaN;
            state.dataOffers = null;
            var sim = new XgSim(state);
            Assert.IsNotNull(sim.S.dataOffers); Assert.IsNotNull(sim.S.dataExtra); Assert.IsNotNull(sim.S.crowdNoise);
            Assert.IsNotNull(sim.S.dataCleaned); Assert.IsNotNull(sim.S.crowdOn); Assert.IsNotNull(sim.S.storyOffered);
            Assert.IsNotNull(sim.S.logs); Assert.IsNotNull(sim.S.logAuto);
            Assert.AreEqual(0, sim.S.logsTotal);
            Assert.AreEqual(60000, sim.Samples("mnist"), "an owned public pack still counts");
            Assert.AreEqual(60000 * XgSim.DataOfferDef("mnist.pack").noise, sim.DataNoise("mnist"), 1e-9, "its noise is worked out, not stored");
            sim.Tick(1, new Host());
        }

        [Test]
        public void UnknownOfferIdsInASaveAreDropped()
        {
            var state = new XgState();
            state.dataOffers.Add("nope.junk"); state.crowdOn.Add("nope");
            var sim = new XgSim(state);
            Assert.IsEmpty(sim.S.dataOffers); Assert.IsEmpty(sim.S.crowdOn);
            Assert.IsFalse(sim.BuyDataOffer("nope.junk", new Host()));
        }
    }
}
