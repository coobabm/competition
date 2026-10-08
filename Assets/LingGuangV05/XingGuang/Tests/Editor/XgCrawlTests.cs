using System;
using System.Collections.Generic;
using System.Linq;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>摆渡百科爬虫 (XgSim.Crawl.cs, XgCrawlArticle.cs): a data source paid per run, one sample per grabbed word.</summary>
    public sealed class XgCrawlTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000, compute = 1;
            public double Compute => compute;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim AtStage(int stage)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.stage = sim.S.stageVision = sim.S.stageSequence = stage;
            return sim;
        }

        [Test]
        public void TheCrawlerIsAStageThreeTextSourcePricedLikeTheCrowd()
        {
            var def = XgSim.DataOfferDef(XgSim.CrawlOfferId);
            Assert.IsNotNull(def);
            Assert.AreEqual(XgDataSource.Crawl, def.source);
            Assert.AreEqual(XgSim.CrawlDataset, def.datasetId);
            Assert.IsNotNull(XgCatalog.Dataset(def.datasetId));
            Assert.AreEqual(XgSim.CrawlCap, def.samples);
            Assert.AreEqual(XgSim.CrawlCap * XgSim.CrowdPricePerLabel, def.price, 1e-9, "the same ¥ per row as a crowd task");
            Assert.AreEqual(3, def.stage);
            Assert.IsFalse(string.IsNullOrEmpty(def.nameEn) || string.IsNullOrEmpty(def.descriptionEn));

            Assert.IsFalse(AtStage(2).Offer(XgSim.CrawlOfferId).available, "not before stage 3");
            Assert.IsFalse(AtStage(2).StartCrawl(new Host()));
            Assert.IsTrue(AtStage(3).Offer(XgSim.CrawlOfferId).available);
            Assert.IsFalse(AtStage(3).ShopDataOffers().Any(o => o.id == XgSim.CrawlOfferId), "淘货 does not sell it");
        }

        [Test]
        public void EveryGrabbedWordIsOneSampleUpToTheCap()
        {
            var sim = AtStage(3); var host = new Host();
            double before = sim.Samples(XgSim.CrawlDataset);
            Assert.AreEqual(0, sim.CrawlGrab(), "nothing counts before a run is paid for");
            Assert.IsTrue(sim.BuyDataOffer(XgSim.CrawlOfferId, host), "buying the offer starts a run");
            Assert.AreEqual(1000 - XgSim.CrawlPrice, host.money, 1e-9);
            Assert.IsTrue(sim.CrawlActive);
            Assert.IsTrue(sim.Offer(XgSim.CrawlOfferId).owned, "owned while the run is going on");
            Assert.IsFalse(sim.StartCrawl(host), "one run at a time");
            Assert.AreEqual(1000 - XgSim.CrawlPrice, host.money, 1e-9, "a refused start costs nothing");

            for (int i = 0; i < 150; i++) Assert.AreEqual(1, sim.CrawlGrab());
            Assert.AreEqual(before + 150, sim.Samples(XgSim.CrawlDataset), 1e-9);
            Assert.AreEqual(XgSim.CrawlCap - 150, sim.CrawlGrab(100), "the cap trims the last grab");
            Assert.AreEqual(0, sim.CrawlGrab());
            Assert.AreEqual(before + XgSim.CrawlCap, sim.Samples(XgSim.CrawlDataset), 1e-9);
            Assert.AreEqual(XgSim.CrawlCap, sim.EndCrawl());
            Assert.IsFalse(sim.CrawlActive);
            Assert.AreEqual(1, sim.S.crawlRuns);
            Assert.AreEqual(XgSim.CrawlCap, sim.S.crawlWords, 1e-9);
        }

        [Test]
        public void ClosingEarlyKeepsWhatWasGrabbed()
        {
            var sim = AtStage(3); var host = new Host();
            Assert.IsTrue(sim.StartCrawl(host));
            sim.CrawlGrab(37);
            Assert.AreEqual(37, sim.EndCrawl());
            Assert.AreEqual(37, sim.Samples(XgSim.CrawlDataset), 1e-9);
            Assert.AreEqual(0, sim.CrawlGrab(), "the rest of a closed run is gone");
            Assert.AreEqual(0, sim.EndCrawl(), "ending twice does nothing");
            Assert.IsTrue(sim.StartCrawl(host), "another run is paid again");
            Assert.AreEqual(1000 - 2 * XgSim.CrawlPrice, host.money, 1e-9);
            Assert.AreEqual(0, sim.CrawlTaken);
            sim.CrawlGrab(5); sim.EndCrawl();
            Assert.AreEqual(42, sim.Samples(XgSim.CrawlDataset), 1e-9);
        }

        [Test]
        public void ShortMoneyStartsNothing()
        {
            var sim = AtStage(3); var host = new Host { money = XgSim.CrawlPrice - 1 };
            Assert.IsFalse(sim.StartCrawl(host));
            Assert.IsFalse(sim.CrawlActive);
            Assert.AreEqual(0, sim.S.crawlRuns);
        }

        [Test]
        public void TheDataBarCountsCrawledRowsAtTheDatasetWeightLessTheirNoise()
        {
            var sim = AtStage(3); var host = new Host();
            double d0 = sim.TrainedSamples;
            sim.StartCrawl(host); sim.CrawlGrab(100); sim.EndCrawl();
            var ds = XgCatalog.Dataset(XgSim.CrawlDataset);
            double noise = 100 * XgSim.CrawlNoise;
            Assert.AreEqual(noise, sim.DataNoise(XgSim.CrawlDataset), 1e-9, "junk rows ride along like a crowd task's");
            Assert.AreEqual(ds.dataWeight * (100 - XgSim.NoisePenalty * noise), sim.TrainedSamples - d0, 1e-9);
            Assert.AreEqual(100, sim.ExtraSamples(XgSim.CrawlDataset), 1e-9, "the crowd-task path, not a pack");

            sim.S.unlocked.Add(XgSim.DataCleanId);
            Assert.AreEqual(noise * XgSim.CleanNoiseFactor, sim.DataNoise(XgSim.CrawlDataset), 1e-9, "数据清洗 halves it");
            Assert.AreEqual(XgSim.CrawlNoise * XgSim.CleanNoiseFactor, sim.Offer(XgSim.CrawlOfferId).effectiveNoise, 1e-9);
        }

        [Test]
        public void GrabbedRowsSurviveASaveButARunDoesNot()
        {
            var sim = AtStage(3); var host = new Host();
            sim.StartCrawl(host); sim.CrawlGrab(12);
            var again = new XgSim(sim.S);
            Assert.AreEqual(12, again.Samples(XgSim.CrawlDataset), 1e-9);
            Assert.AreEqual(sim.DataNoise(XgSim.CrawlDataset), again.DataNoise(XgSim.CrawlDataset), 1e-9);
            Assert.IsFalse(again.CrawlActive, "a loaded game has no half-finished run");
            Assert.AreEqual(0, again.CrawlGrab());
        }

        [Test]
        public void OldSavesAreUnaffected()
        {
            var state = new XgState();
            state.owned.Add("mnist");
            var sim = new XgSim(state);
            Assert.AreEqual(0, sim.S.crawlRuns);
            Assert.AreEqual(0, sim.S.crawlWords);
            Assert.AreEqual(0, sim.Samples(XgSim.CrawlDataset));
            Assert.AreEqual(60000, sim.Samples("mnist"));
            Assert.IsFalse(sim.CrawlActive);

            var broken = new XgState { crawlRuns = -3, crawlWords = double.NaN };
            var repaired = new XgSim(broken);
            Assert.AreEqual(0, repaired.S.crawlRuns);
            Assert.AreEqual(0, repaired.S.crawlWords);
        }

        // ───── the generated page ─────

        static readonly IList<string[]> Headlines = new List<string[]>
        {
            new[] { "AlphaGo 4:1 战胜李世石", "AlphaGo beats Lee Sedol 4–1" },
            new[] { "GTX 1080 国行今日上市，4999 元起", "GTX 1080 goes on sale in China today from ¥4,999" },
            new[] { "2016 年高考今日开考，940 万人报名", "The 2016 gaokao starts today with 9.4 million candidates" },
            new[] { "英国公投决定脱离欧盟", "The UK votes to leave the EU" },
        };

        static IEnumerable<XgCrawlArticle> Pages(bool english)
        {
            for (int seed = 0; seed < 40; seed++)
                foreach (int today in new[] { 20160601, 20160815, 20161220 })
                    yield return XgCrawlArticle.Make(seed, today, english, Headlines);
        }

        [Test]
        public void PagesHaveATitleSectionsAndReferencesAndEnoughWords()
        {
            foreach (bool english in new[] { false, true })
                foreach (var a in Pages(english))
                {
                    Assert.AreEqual(XgCrawlArticle.Kind.Title, a.blocks[0].kind);
                    Assert.That(a.blocks.Count(b => b.kind == XgCrawlArticle.Kind.Heading), Is.GreaterThanOrEqualTo(3));
                    Assert.That(a.blocks.Count(b => b.kind == XgCrawlArticle.Kind.Reference), Is.GreaterThanOrEqualTo(3));
                    Assert.That(a.blocks.Count(b => b.kind == XgCrawlArticle.Kind.News), Is.GreaterThanOrEqualTo(1));
                    Assert.That(a.WordCount, Is.GreaterThanOrEqualTo(60), a.title);
                    Assert.IsTrue(a.Url.StartsWith("baike.bodu.com/item/", StringComparison.Ordinal));
                }
            Assert.AreEqual(XgCrawlArticle.Make(7, 20160815, false, Headlines).blocks.Sum(b => b.words.Count),
                XgCrawlArticle.Make(7, 20160815, false, Headlines).blocks.Sum(b => b.words.Count), "same seed, same page");
            Assert.That(Enumerable.Range(0, 16).Select(s => XgCrawlArticle.Make(s, 20160815, false).title).Distinct().Count(), Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void WordStartsMatchTheText()
        {
            foreach (bool english in new[] { false, true })
                foreach (var a in Pages(english))
                    foreach (var b in a.blocks)
                    {
                        string text = b.Text(english);
                        var starts = b.Starts(english);
                        for (int i = 0; i < b.words.Count; i++)
                            Assert.AreEqual(b.words[i], text.Substring(starts[i], b.words[i].Length), a.title + ": " + text);
                    }
        }

        [Test]
        public void PagesStayIn2016AndEnglishPagesCarryNoChinese()
        {
            var zh = Pages(false).SelectMany(a => a.blocks.Select(b => b.Text(false))).ToList();
            var en = Pages(true).SelectMany(a => a.blocks.Select(b => b.Text(true))).ToList();
            Assert.IsEmpty(EraLexicon.Audit(zh.Concat(en)));
            foreach (var line in zh.Concat(en)) Assert.IsFalse(line.Contains("涌现"), line);
            foreach (var line in en) Assert.IsFalse(line.Any(c => c >= 0x3400 && c <= 0x9FFF), line);
            // Hot words dated after the page's day never show.
            foreach (var a in Enumerable.Range(0, 40).Select(s => XgCrawlArticle.Make(s, 20160601, false)))
                foreach (var b in a.blocks) Assert.IsFalse(b.Text(false).Contains("洪荒之力") || b.Text(false).Contains("葛优躺"), b.Text(false));
        }

        [Test]
        public void SegmentKeepsLatinWholeAndCutsChineseIntoShortWords()
        {
            var w = XgCrawlArticle.Segment("GTX 1080 国行今日上市，4999 元起");
            CollectionAssert.AreEqual(new[] { "GTX", "1080", "国行", "今日", "上市", "，", "4999", "元起" }, w);
            Assert.IsFalse(XgCrawlArticle.Grabbable("，"));
            Assert.IsTrue(XgCrawlArticle.Grabbable("显卡"));
            CollectionAssert.AreEqual(new[] { "床前", "明月光" }, XgCrawlArticle.Segment("床前明月光"));
        }
    }
}
