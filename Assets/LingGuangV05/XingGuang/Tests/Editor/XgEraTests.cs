using System.Collections.Generic;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Design v1.1 §11.7 (the month follows the lab's progress) and §14.2 (hot words by month and destination).</summary>
    public sealed class XgEraTests
    {
        sealed class Host : IXgHost
        {
            public double Compute => 1;
            public double VramMB => 100000;
            public double Money => 1000000;
            public bool Spend(double amount) => true;
            public void Earn(double amount) { }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static void SetLabels(XgSim sim, string dataset, double count)
        {
            sim.S.labels.RemoveAll(l => l.dataset == dataset);
            sim.S.labels.Add(new XgLabelCount { dataset = dataset, count = count });
        }

        [Test]
        public void TheMonthFollowsTheSlowerBarAndNeverRunsBack()
        {
            var sim = new XgSim();
            Assert.AreEqual(0, sim.MonthProgress, 1e-9, "a fresh stage is the first of the month");
            double p = sim.ParamsThreshold(2), d = sim.SamplesThreshold(2);
            sim.S.trainedParamsK = p * .5; SetLabels(sim, "logic", d);
            Assert.AreEqual(.5, sim.MonthProgress, 1e-9, "parameters are the slower bar");
            sim.S.trainedParamsK = p; SetLabels(sim, "logic", d * .3);
            Assert.AreEqual(.5, sim.MonthProgress, 1e-9, "losing data never takes the date back");
            SetLabels(sim, "logic", d * .8);
            Assert.AreEqual(.8, sim.MonthProgress, 1e-9, "now the data is the slower bar");
            SetLabels(sim, "logic", d * .999);
            Assert.That(sim.MonthProgress, Is.LessThan(1), "the last day waits for the ability itself");
        }

        [Test]
        public void AnAbilityStartsTheNextMonthWhateverTheDate()
        {
            var sim = new XgSim(new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 3, stageVision = 3, stageSequence = 3 });
            SetLabels(sim, "poems", sim.SamplesThreshold(4));
            sim.S.trainedParamsK = sim.ParamsThreshold(4) * .4;
            Assert.AreEqual(.4, sim.MonthProgress, 1e-9);
            int advanced = 0;
            sim.StageAdvanced += from => advanced = from;
            sim.S.trainedParamsK = sim.ParamsThreshold(4);
            sim.Tick(1, new Host());
            Assert.AreEqual(3, advanced);
            Assert.AreEqual(4, sim.S.stage);
            // A new stage is a new month (GameCalendar.DayFor(4, p) is past every day of stage 3's month for any p).
            Assert.That(sim.MonthProgress, Is.InRange(0, 1));
            // Rent is paid per calendar day (经济压力与破产): a new month opens on its first day, its bars counted from where they stood.
            Assert.AreEqual(0, sim.MonthProgress, 1e-9, "the month starts on its first day");
            sim.S.trainedParamsK = sim.S.stageStartParamsK + (sim.ParamsThreshold(5) - sim.S.stageStartParamsK) * .5;
            SetLabels(sim, "poems", sim.SamplesThreshold(5));
            Assert.AreEqual(.5, sim.MonthProgress, 1e-9, "half the way from the stage's start to the fifth ability");
        }

        [Test]
        public void TopicsFollowTheMonth()
        {
            var august = new HashSet<string> { "葛优躺", "里约奥运", "洪荒之力" };
            for (int day = 10; day <= 20; day++) Assert.That(august.Contains(XgMemes.TopicOf(20160800 + day)), XgMemes.TopicOf(20160800 + day));
            var spring = new HashSet<string> { "太阳的后裔", "AlphaGo", "科比", "papi酱", "集五福", "老司机", "友谊的小船", "叶问3" };
            Assert.That(spring.Contains(XgMemes.TopicOf(20160601)), "nothing fresh at the start of June: any earlier topic");
            foreach (var t in XgMemes.Topics) Assert.LessOrEqual(t.since, 20161231, t.topic);
        }

        [Test]
        public void HotWordsWaitForTheirMonthAndKnowWhereTheyGo()
        {
            var june = XgMemes.HotWordsFor("Y", 20160601).ConvertAll(w => w.word);
            Assert.Contains("屁股", june);
            Assert.IsFalse(june.Contains("醒醒"), "醒醒 is the August volleyball morning");
            Assert.Contains("醒醒", XgMemes.HotWordsFor("Y", 20160821).ConvertAll(w => w.word));
            Assert.IsFalse(XgMemes.HotWordsFor("M", 20160901).ConvertAll(w => w.word).Contains("蓝瘦香菇"));
            Assert.Contains("蓝瘦香菇", XgMemes.HotWordsFor("M", 20161010).ConvertAll(w => w.word));
            foreach (var w in XgMemes.HotWords) { Assert.IsNotEmpty(w.to, w.word); Assert.IsTrue(w.since <= 20161231, w.word); }
            Assert.IsTrue(XgMemes.HotWords.Length >= 40);
        }

        [Test]
        public void NoPost2016MemeOnAnyDesk()
        {
            var lines = new List<string>();
            foreach (var desk in new[] { "danmu", "spam", "headline", "review", "translate" })
                foreach (var p in XgMemes.Bank(desk)) { lines.Add(p.text); lines.Add(p.why); }
            foreach (var w in XgMemes.HotWords) lines.Add(w.word);
            Assert.IsEmpty(EraLexicon.Audit(lines));
        }

        [Test]
        public void EachLabMonthBringsNewDeskPhrases()
        {
            int[] monthStarts = { 20160601, 20160701, 20160801, 20160901, 20161001, 20161101, 20161201 };
            for (int i = 1; i < monthStarts.Length; i++)
            {
                int fresh = 0;
                foreach (var desk in new[] { "danmu", "spam", "headline" })
                    foreach (var p in XgMemes.Bank(desk)) if (p.since >= monthStarts[i - 1] && p.since < monthStarts[i]) fresh++;
                Assert.GreaterOrEqual(fresh, 2, "phrases first seen in the month starting " + monthStarts[i - 1]);
            }
        }
    }
}
