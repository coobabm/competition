using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// Words the spiders read count as samples (XgSim.Reading.cs): 1 word = 1 sample of the text corpus, for the paid
    /// crawler and for the resident spider's free reading of the desktop alike.
    /// </summary>
    public sealed class XgReadTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000;
            public double Compute => 1;
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
        public void OneNewWordIsOneSampleOfTheTextCorpusAndTheDataBar()
        {
            var sim = AtStage(1);
            double d0 = sim.TrainedSamples, s0 = sim.Samples(XgSim.ReadDataset);
            Assert.AreEqual("news", XgSim.ReadDataset);
            Assert.AreEqual(1, XgSim.SamplesPerWord, "the documented ratio is 1:1");
            Assert.IsTrue(sim.ResidentRead("灵光"));
            Assert.AreEqual(s0 + 1, sim.Samples(XgSim.ReadDataset), 1e-9);
            Assert.AreEqual(1, sim.ExtraSamples(XgSim.ReadDataset), 1e-9, "booked as crowd-style rows, not a pack");
            Assert.AreEqual(XgCatalog.Dataset(XgSim.ReadDataset).dataWeight, sim.TrainedSamples - d0, 1e-9, "no noise on the resident's reads");
            Assert.AreEqual(1, sim.S.residentWords, 1e-9);
            Assert.AreEqual(0, sim.DataNoise(XgSim.ReadDataset), 1e-9);
        }

        [Test]
        public void ARecentWordDoesNotCountAgainAndNonWordsNeverDo()
        {
            var sim = AtStage(1);
            Assert.IsTrue(sim.ResidentRead("Hello,"));
            Assert.IsFalse(sim.ResidentRead("hello"), "case and punctuation do not make a new word");
            Assert.IsFalse(sim.ResidentRead("\"HELLO\""));
            Assert.IsFalse(sim.ResidentRead(null));
            Assert.IsFalse(sim.ResidentRead(""));
            Assert.IsFalse(sim.ResidentRead("，。"), "punctuation is not a word");
            Assert.IsFalse(sim.ResidentRead("2016"), "digits alone are not a word");
            Assert.IsFalse(sim.ResidentRead(new string('a', 41)), "absurdly long runs are not words");
            Assert.AreEqual(1, sim.S.residentWords, 1e-9);
            Assert.AreEqual(1, sim.Samples(XgSim.ReadDataset), 1e-9);
        }

        [Test]
        public void TheResidentForgetsAfterItsMemoryIsFull()
        {
            var sim = AtStage(1);
            Assert.IsTrue(sim.ResidentRead("first"));
            for (int i = 0; i < XgSim.ReadMemory; i++) Assert.IsTrue(sim.ResidentRead("w" + i + "x"));
            Assert.IsTrue(sim.ResidentRead("first"), "pushed out of memory, it can pay again");
            Assert.IsFalse(sim.ResidentRead("w" + (XgSim.ReadMemory - 1) + "x"), "a recent one still does not");
        }

        [Test]
        public void CrawlerAndResidentFeedTheSameBarAtTheSameRatio()
        {
            var sim = AtStage(3); var host = new Host();
            double d0 = sim.TrainedSamples;
            sim.StartCrawl(host); sim.CrawlGrab(100); sim.EndCrawl();
            for (int i = 0; i < 50; i++) sim.ResidentRead("spider" + i + "q");
            Assert.AreEqual(150, sim.ReadWordsTotal, 1e-9);
            Assert.AreEqual(150 * XgSim.SamplesPerWord, sim.ReadSamplesTotal, 1e-9);
            Assert.AreEqual(150, sim.Samples(XgSim.ReadDataset), 1e-9);
            double crawlNet = 100 - XgSim.NoisePenalty * 100 * XgSim.CrawlNoise;
            Assert.AreEqual(crawlNet + 50, sim.TrainedSamples - d0, 1e-9, "crawled rows lose their junk share, the resident's are clean");
            Assert.AreEqual(100, sim.S.crawlWords, 1e-9);
            Assert.AreEqual(50, sim.S.residentWords, 1e-9);
        }

        [Test]
        public void ReadingMovesTheAbilitySampleBar()
        {
            var sim = AtStage(1);
            int next = sim.NextAbility;
            double p0 = sim.SamplesProgress(next);
            for (int i = 0; i < 500; i++) sim.ResidentRead("bar" + i + "z");
            Assert.AreEqual(p0 + 500 / sim.SamplesThreshold(next), sim.SamplesProgress(next), 1e-9);
        }

        [Test]
        public void TheLabSaysSoEveryBatchOfWords()
        {
            var sim = AtStage(1);
            int said = sim.S.log.Count;
            for (int i = 0; i < XgSim.ReadSayEvery - 1; i++) sim.ResidentRead("log" + i + "y");
            Assert.AreEqual(said, sim.S.log.Count);
            sim.ResidentRead("logfinal");
            Assert.AreEqual(said + 1, sim.S.log.Count);
            StringAssert.Contains("样本", sim.S.log.Last());
            StringAssert.Contains(XgCatalog.Dataset(XgSim.ReadDataset).name, sim.S.log.Last());
        }

        [Test]
        public void TheDisplayTextShowsTheRuleTheCountAndBothSources()
        {
            var sim = AtStage(3); var host = new Host();
            Assert.AreEqual("", sim.ReadLine(), "nothing to show before anything was read");
            Assert.AreEqual("", sim.ReadShort());
            sim.StartCrawl(host); sim.CrawlGrab(120); sim.EndCrawl();
            for (int i = 0; i < 30; i++) sim.ResidentRead("show" + i + "w");
            sim.English = false;
            string zh = sim.ReadLine();
            StringAssert.Contains("读到的词 150", zh);
            StringAssert.Contains("+150 样本", zh);
            StringAssert.Contains("爬虫 120", zh);
            StringAssert.Contains("蜘蛛 30", zh);
            StringAssert.Contains("1 个词 = 1 条样本", zh);
            StringAssert.Contains("+150 样本", sim.ReadShort());
            sim.English = true;
            string en = sim.ReadLine();
            StringAssert.Contains("Words read 150", en);
            StringAssert.Contains("+150 samples", en);
            StringAssert.Contains("crawler 120", en);
            StringAssert.Contains("spider 30", en);
            StringAssert.Contains("1 word = 1 sample", en);
            StringAssert.Contains("+150 samples", sim.ReadShort());
            Assert.AreNotEqual(zh, en);
            sim.English = false;
            string end = sim.S.log.First(l => l.StartsWith("爬虫收工", System.StringComparison.Ordinal));
            StringAssert.Contains("+120 样本", end);
        }

        [Test]
        public void ReadingSurvivesASaveAndOldSavesAreCreditedOnceAtMost()
        {
            var sim = AtStage(3); var host = new Host();
            sim.StartCrawl(host); sim.CrawlGrab(80); sim.EndCrawl();
            for (int i = 0; i < 40; i++) sim.ResidentRead("save" + i + "v");
            double samples = sim.Samples(XgSim.ReadDataset), bar = sim.TrainedSamples;
            Assert.AreEqual(XgSim.ReadSchemaNow, sim.S.readSchema, "a new game is stamped");
            var loaded = new XgSim(sim.S);
            Assert.AreEqual(samples, loaded.Samples(XgSim.ReadDataset), 1e-9, "a current save is not credited again");
            Assert.AreEqual(bar, loaded.TrainedSamples, 1e-9);
            Assert.AreEqual(40, loaded.S.residentWords, 1e-9);
            Assert.AreEqual(80, loaded.S.crawlWords, 1e-9);
            Assert.IsTrue(loaded.ResidentRead("fresh"), "the loaded sim keeps counting");

            // An old save: crawler words already booked when they were grabbed (as the old CrawlGrab did).
            var old = new XgState { crawlRuns = 1, crawlWords = 80, readSchema = 0 };
            old.dataExtra.Add(new XgLabelCount { dataset = XgSim.ReadDataset, count = 80 });
            old.crowdNoise.Add(new XgLabelCount { dataset = XgSim.ReadDataset, count = 80 * XgSim.CrawlNoise });
            var a = new XgSim(old);
            Assert.AreEqual(80, a.Samples(XgSim.ReadDataset), 1e-9, "no double counting");
            Assert.AreEqual(0, a.S.residentWords, "the resident's old reads were never recorded");
            Assert.AreEqual(XgSim.ReadSchemaNow, a.S.readSchema);
            Assert.AreEqual(80, new XgSim(a.S).Samples(XgSim.ReadDataset), 1e-9, "loading again changes nothing");

            // A tally with no rows behind it is topped up once.
            var gap = new XgState { crawlRuns = 1, crawlWords = 300, readSchema = 0 };
            var b = new XgSim(gap);
            Assert.AreEqual(300, b.Samples(XgSim.ReadDataset), 1e-9);
            Assert.AreEqual(300 * XgSim.CrawlNoise, b.DataNoise(XgSim.ReadDataset), 1e-9, "with its junk share");
            Assert.AreEqual(300, new XgSim(b.S).Samples(XgSim.ReadDataset), 1e-9, "only once");

            var broken = new XgSim(new XgState { residentWords = double.NaN });
            Assert.AreEqual(0, broken.S.residentWords);
        }
            [Test]
        public void LaterStagesMakeEachWordWorthMore()
        {
            CollectionAssert.AreEqual(new double[] { 1, 1, 1, 1, 2, 3, 3 }, XgSim.ReadWorthByStage);
            var sim = AtStage(5);
            Assert.AreEqual(3, sim.ReadWorth, 1e-9);
            double d0 = sim.TrainedSamples;
            Assert.IsTrue(sim.ResidentRead("灵光"));
            Assert.AreEqual(3, sim.ExtraSamples(XgSim.ReadDataset), 1e-9);
            Assert.AreEqual(3, sim.TrainedSamples - d0, 1e-9, "the spider's word moves the bar three samples in stage 5");
            Assert.AreEqual(3, sim.ReadSamplesTotal, 1e-9);
            Assert.AreEqual(1, sim.S.residentWords, 1e-9, "still one word");
            StringAssert.Contains("3", sim.ReadRuleText());
        }

        [Test]
        public void TheCrawlerPaysTheStageWorthToo()
        {
            var sim = AtStage(4); var host = new Host();
            sim.StartCrawl(host); sim.CrawlGrab(10); sim.EndCrawl();
            Assert.AreEqual(20, sim.ExtraSamples(XgSim.ReadDataset), 1e-9);
            Assert.AreEqual(20, sim.ReadSamplesTotal, 1e-9);
        }

        [Test]
        public void AnOldSaveKeepsItsReadSamplesAtOneToOne()
        {
            var sim = new XgSim(new XgState { residentWords = 100, crawlWords = 0, readSchema = 1, stage = 5 }) { GoldChance = 0 };
            Assert.AreEqual(100, sim.ReadSamplesTotal, 1e-9, "words read before the worth went up stay 1:1");
            Assert.AreEqual(XgSim.ReadSchemaNow, sim.S.readSchema);
        }
    }
}
