using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgOfferDownloadEstimateTests
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

        static void TickDownloads(XgSim sim, double seconds)
        {
            typeof(XgSim).GetMethod("TickDownloads", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(sim, new object[] { seconds });
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void OfferEstimateMatchesActualNewPurchaseAtEachTier(int tier)
        {
            foreach (string id in new[] { "spam.pack", "mnist.pack", "cifar.pack", "cifar.junk", "imagenet.story", "translate.story" })
            {
                var sim = new XgSim { GoldChance = 0 }; var host = new Host();
                sim.S.stage = sim.S.stageVision = sim.S.stageSequence = sim.S.fundedAbility = 6;
                foreach (string node in new[] { "perceptron", "mlp", "lenet", "resnet", "attention" }) sim.S.unlocked.Add(node);
                if (id.EndsWith(".story", StringComparison.Ordinal))
                {
                    Assert.IsTrue(sim.AskForData(id.Split('.')[0]));
                    sim.Tick(XgSim.DataReplySeconds + 1, host);
                    Assert.IsTrue(sim.DataForumFlag("answered." + id.Split('.')[0]));
                }
                sim.S.broadband = tier;
                double advertised = sim.Offer(id).downloadSec;
                Assert.IsTrue(sim.BuyDataOffer(id, host), id);
                var queue = sim.DownloadQueue();
                Assert.IsNotEmpty(queue, id);
                Assert.AreEqual(queue.Max(x => x.eta), advertised, 1e-8, id + " purchase estimate matches full queue duration");
                if (id == "imagenet.story")
                {
                    Assert.AreEqual(10, queue.Count, "retain the existing ten-batch story purchase");
                    Assert.AreEqual(tier == 0 ? 300 : 150, advertised, 1e-8);
                }
                if (id == "cifar.junk" && tier == 0) Assert.AreEqual(12, advertised, 1e-8);
                if (id == "cifar.pack" && tier == 0) Assert.AreEqual(8, advertised, 1e-8, "public purchase is the full pack");
                if (id == "translate.story") Assert.AreEqual(15, advertised, 1e-8, "campus delivery is independent of broadband");
                TickDownloads(sim, advertised - .01);
                Assert.IsNotEmpty(sim.DownloadQueue(), id + " must not finish before its estimate");
                TickDownloads(sim, .02);
                Assert.IsEmpty(sim.DownloadQueue(), id + " finishes at its estimate");
                Assert.AreEqual(0, sim.PackSamples(id.Split('.')[0]), "arrival still does not install");
            }
        }

        [Test]
        public void LegacyCampusTimingRemainsUnchanged()
        {
            var sim = new XgSim();
            var def = XgSim.DataOfferDef("translate.story");
            Assert.AreEqual(120, def.downloadSec, "legacy timing metadata must remain stable");
            var file = new XgDownload { key = "translate", dataset = "translate", offer = def.id,
                fileId = "legacy-campus-test", sizeMB = def.sizeMB, samples = def.samples,
                campus = true, ownDrive = true, batches = XgSim.PackBatches("translate"), paceVersion = 0 };
            sim.S.downloadQueue.Add(file);
            Assert.AreEqual(120, sim.DownloadQueue().Single().eta, 1e-8);
            TickDownloads(sim, 60);
            Assert.AreEqual(.5, sim.DownloadQueue().Single().progress, 1e-8);
            TickDownloads(sim, 60);
            Assert.IsEmpty(sim.DownloadQueue());
            Assert.IsTrue(sim.DataFileReady(file.fileId));
            Assert.IsFalse(file.installed);
        }

        [Test]
        public void OneMegabyteNewFileStillQueuesForThreeSeconds()
        {
            var sim = new XgSim();
            var file = new XgDownload { key = "one-megabyte", dataset = "spam", sizeMB = 1, batches = 1, samples = 1 };
            typeof(XgSim).GetMethod("Enqueue", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(sim, new object[] { file, false });
            Assert.AreEqual(1, sim.DownloadQueue().Count);
            Assert.AreEqual(3, sim.DownloadQueue().Single().eta, 1e-8);
            TickDownloads(sim, 1.5);
            Assert.AreEqual(.5, sim.DownloadQueue().Single().progress, 1e-8);
            TickDownloads(sim, 1.5);
            Assert.IsEmpty(sim.DownloadQueue());
            Assert.IsTrue(sim.DataFileReady(file.fileId));
            Assert.IsFalse(file.installed);
        }
    }
}
