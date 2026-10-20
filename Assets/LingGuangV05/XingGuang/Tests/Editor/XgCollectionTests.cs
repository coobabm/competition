using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public class XgCollectionTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e9;
            public double Compute => 4;
            public double VramMB => 1 << 16;
            public double Money => money;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        [Test]
        public void AtlasNodesLightThemselvesAndCannotBeBought()
        {
            var sim = new XgSim(); var host = new Host();
            var ph = XgCatalog.Node("ph.xor"); var ab1 = XgCatalog.Node("ab.1"); var ab6 = XgCatalog.Node("ab.6");
            Assert.IsNotNull(ph); Assert.IsNotNull(ab1); Assert.IsNotNull(ab6);
            Assert.AreEqual(XgSim.NodeStatus.Owned, sim.Status(ab1, host), "stage 1 can already say yes or no");
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.Status(ph, host));
            Assert.IsFalse(sim.BuyNode("ph.xor", host));
            sim.S.phenomena.seen.Add("xor");
            Assert.AreEqual(XgSim.NodeStatus.Owned, sim.Status(ph, host));
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.Status(ab6, host), "the sixth cell waits for the sixth ability");
            sim.S.stage = 6; sim.Tick(1, host);
            Assert.AreEqual(XgSim.NodeStatus.Owned, sim.Status(ab6, host));
        }

        [Test]
        public void EveryPhenomenonHasAnAtlasNodeAndBothLanguages()
        {
            foreach (var p in XgPhenomena.All)
            {
                Assert.IsNotNull(XgCatalog.Node("ph." + p.id), p.id);
                Assert.IsFalse(string.IsNullOrEmpty(p.nameEn) || string.IsNullOrEmpty(p.whyEn), p.id);
            }
            Assert.That(XgPhenomena.All.Length, Is.GreaterThanOrEqualTo(17), "design v1.1 §4.5 lists 17");
        }

        [Test]
        public void SelfInsightRevealsACardOnceAndScaleAloneEarnsTheHiddenAchievement()
        {
            var sim = new XgSim(); var host = new Host();
            var cards = new List<string>(); var earned = new List<string>();
            sim.InsightCard += w => cards.Add(w); sim.AchievementEarned += a => earned.Add(a.id);
            sim.S.insights.Add("pretrain");
            sim.Tick(1, host); sim.Tick(1, host);
            Assert.AreEqual(new[] { "pretrain" }, cards.ToArray(), "one card, once");
            Assert.That(earned, Does.Contain("insight.pretrain"));
            Assert.IsFalse(sim.HasAchievement("lingguang"));
            for (int i = 2; i <= XgSim.AbilityCount; i++) sim.S.insights.Add(XgSim.ScaleInsight + i);
            sim.Tick(1, host);
            Assert.IsTrue(sim.HasAchievement("road.scale"));
            Assert.IsTrue(sim.HasAchievement("lingguang"));
            Assert.AreEqual(XgSim.InsightCards.Length, cards.Count);
        }

        [Test]
        public void StoryPhenomenaFollowTheAbilities()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.stage = 5; sim.S.emerged.Add(5); sim.S.abilities = true;
            sim.Tick(1, host); sim.Tick(1, host);
            Assert.IsTrue(sim.PhenomenonSeen("translation"));
            Assert.IsTrue(sim.PhenomenonSeen("serial"));
            Assert.IsTrue(sim.PhenomenonSeen("emergence"));
        }

        [Test]
        public void BigPacksDownloadSlowlyUnlessAccelerated()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.stage = 5; sim.S.unlocked.Add("rnn");
            foreach (var n in XgCatalog.Nodes) if (n.stage <= 5 && n.kind != XgNodeKind.Dataset && n.kind != XgNodeKind.Secret && !XgSim.IsAtlas(n) && n.tree != "label" && !sim.Has(n.id)) sim.S.unlocked.Add(n.id);
            Assert.IsTrue(sim.BuyNode("imagenet.pack", host), "ImageNet's first batch is 15 GB");
            Assert.IsTrue(sim.Downloading("imagenet")); Assert.IsFalse(sim.Owns("imagenet"));
            sim.Tick(sim.DownloadLeft("imagenet") + 1, host);
            Assert.IsFalse(sim.Owns("imagenet"), "download completion only creates a file");
            var ready = sim.S.dataFiles.Find(x => x.dataset == "imagenet");
            Assert.IsNotNull(ready);
            Assert.IsTrue(sim.DataFileReady(ready.fileId));
            sim.S.unlocked.Add(XgSim.DataEntryNode);
            Assert.IsTrue(sim.InstallDataFile(ready.fileId));
            Assert.IsTrue(sim.Owns("imagenet"));
            Assert.IsTrue(sim.BuyNode("review.pack", host));
            Assert.IsTrue(sim.Downloading("review"), "small packs have a visible download too");
            sim.Tick(sim.DownloadLeft("review") + 1, host);
            Assert.IsFalse(sim.Owns("review"));
            Assert.IsTrue(sim.InstallDataFile(sim.S.dataFiles.Find(x => x.dataset == "review").fileId));
            Assert.IsTrue(sim.Owns("review"));
        }

        [Test]
        public void AccelerationFinishesTheDownloadForTheSecondsItSaves()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.stage = 5;
            foreach (var id in new[] { "mlp", "rnn", "lenet", "resnet" }) sim.S.unlocked.Add(id);
            Assert.IsTrue(sim.BuyNode("imagenet.pack", host));
            Assert.IsTrue(sim.Downloading("imagenet"));
            double before = host.money, cost = sim.AccelerateCost("imagenet");
            Assert.AreEqual(System.Math.Max(XgSim.AccelerateMin, System.Math.Ceiling(sim.DownloadLeft("imagenet") * XgSim.AcceleratePerSecond)), cost, 1, "¥ for the seconds it saves");
            Assert.IsTrue(sim.Accelerate("imagenet", host));
            Assert.AreEqual(before - cost, host.money, 1e-9);
            Assert.IsFalse(sim.Owns("imagenet"), "download completion only creates a file");
            var ready = sim.S.dataFiles.Find(x => x.dataset == "imagenet");
            Assert.IsNotNull(ready);
            Assert.IsTrue(sim.DataFileReady(ready.fileId));
            sim.S.unlocked.Add(XgSim.DataEntryNode);
            Assert.IsTrue(sim.InstallDataFile(ready.fileId));
            Assert.IsTrue(sim.Owns("imagenet"));
            Assert.IsFalse(sim.Accelerate("imagenet", host), "nothing left to speed up");
        }

        [Test]
        public void SideResearchHasNodesAndOnlyAddsSpeed()
        {
            foreach (var id in new[] { "earlystop", "wordvec", "beamsearch", "subword", "sft", "cot" })
            {
                var n = XgCatalog.Node(id);
                Assert.IsNotNull(n, id);
                Assert.That(n.stage, Is.InRange(4, 6), id);
                Assert.AreEqual(XgResearchKind.Feel, XgCatalog.ResearchItem(id).kind, id);
            }
        }
    }
}
