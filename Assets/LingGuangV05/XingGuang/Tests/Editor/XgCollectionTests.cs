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
            sim.S.stage = 6;
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.Status(ab6, host), "everything opens only after the abilities emerge");
            sim.S.abilities = true;
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
        public void SelfInsightRevealsACardOnceAndAllSixEarnTheHiddenAchievement()
        {
            var sim = new XgSim(); var host = new Host();
            var cards = new List<string>(); var earned = new List<string>();
            sim.InsightCard += w => cards.Add(w); sim.AchievementEarned += a => earned.Add(a.id);
            sim.S.insights.Add("combo");
            sim.Tick(1, host); sim.Tick(1, host);
            Assert.AreEqual(new[] { "combo" }, cards.ToArray(), "one card, once");
            Assert.That(earned, Does.Contain("insight.combo"));
            Assert.IsFalse(sim.HasAchievement("lingguang"));
            foreach (var w in XgSim.InsightWalls) if (!sim.S.insights.Contains(w)) sim.S.insights.Add(w);
            sim.Tick(1, host);
            Assert.IsTrue(sim.HasAchievement("lingguang"));
            Assert.AreEqual(XgSim.InsightWalls.Length, cards.Count);
        }

        [Test]
        public void StoryPhenomenaFollowTheWallsAndTheAbilities()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.walls.Add("translation"); sim.S.walls.Add("parallel"); sim.S.abilities = true;
            sim.Tick(1, host);
            Assert.IsTrue(sim.PhenomenonSeen("translation"));
            Assert.IsTrue(sim.PhenomenonSeen("serial"));
            Assert.IsTrue(sim.PhenomenonSeen("emergence"));
        }

        [Test]
        public void BigPacksDownloadSlowlyUnlessAccelerated()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.stage = 4; sim.S.unlocked.Add("rnn");
            foreach (var n in XgCatalog.Nodes) if (n.stage <= 4 && n.kind != XgNodeKind.Dataset && n.kind != XgNodeKind.Secret && !XgSim.IsAtlas(n) && n.tree != "label" && !sim.Has(n.id)) sim.S.unlocked.Add(n.id);
            Assert.IsTrue(sim.BuyNode("news.pack", host), "人民日报 has 5 million samples");
            Assert.IsTrue(sim.Downloading("news")); Assert.IsFalse(sim.Owns("news"));
            sim.Tick(sim.DownloadLeft("news") + 1, host);
            Assert.IsTrue(sim.Owns("news"));
            Assert.IsTrue(sim.BuyNode("review.pack", host));
            Assert.IsFalse(sim.Downloading("review"), "small packs arrive at once");
        }

        [Test]
        public void AccelerationFinishesTheDownloadForATenth()
        {
            var sim = new XgSim(); var host = new Host();
            sim.S.owned.Add("translate"); sim.S.downloads.Add(new XgScore { key = "translate", value = 60 });
            double before = host.money, cost = sim.AccelerateCost("translate");
            Assert.IsTrue(sim.Accelerate("translate", host));
            Assert.AreEqual(before - cost, host.money, 1e-9);
            Assert.IsTrue(sim.Owns("translate"));
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
