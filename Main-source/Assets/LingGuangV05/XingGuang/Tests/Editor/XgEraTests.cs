using System.Collections.Generic;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Design v1.1 §11.7 (the month follows the lab's progress) and §14.2 (hot words by month and destination).</summary>
    public sealed class XgEraTests
    {
        [Test]
        public void TheMonthFillsWithPracticeAndRunsOutAfterTheWall()
        {
            var sim = new XgSim(new XgState { progressionVersion = XgSim.ProgressionSchemaWalls, stage = 1, stageVision = 1, stageSequence = 1 }) { PracticeStrict = true };
            Assert.AreEqual(0, sim.MonthProgress, 1e-9, "a fresh stage is the first of the month");
            sim.S.stageEpochs = XgSim.PracticeFor(1) / 2; sim.S.stageSeconds = XgSim.MinutesFor(1) * 60;
            Assert.AreEqual(.5 * XgSim.WallMonthShare, sim.MonthProgress, 1e-9, "practice is half done");
            sim.S.stageEpochs = XgSim.PracticeFor(1); sim.S.stageSeconds = XgSim.MinutesFor(1) * 30;
            Assert.AreEqual(.5 * XgSim.WallMonthShare, sim.MonthProgress, 1e-9, "the slower of epochs and minutes decides");
            sim.S.stageSeconds = XgSim.MinutesFor(1) * 600;
            Assert.AreEqual(XgSim.WallMonthShare, sim.MonthProgress, 1e-9, "the date waits at the wall's day until the wall shows");

            sim.S.walls.Add("combo"); sim.S.wallSeenAt = sim.S.stageSeconds;
            Assert.AreEqual(XgSim.WallMonthShare, sim.MonthProgress, 1e-9, "the wall shows near the end of the month");
            sim.S.stageSeconds += XgSim.AfterWallSeconds / 2;
            Assert.AreEqual(XgSim.WallMonthShare + (1 - XgSim.WallMonthShare) / 2, sim.MonthProgress, 1e-9);
            sim.S.stageSeconds += XgSim.AfterWallSeconds * 10;
            Assert.AreEqual(1, sim.MonthProgress, 1e-9, "the last day of the month, no further");
        }

        [Test]
        public void PassingTheWallStartsTheNextMonthWhateverTheDate()
        {
            var sim = new XgSim(new XgState { progressionVersion = XgSim.ProgressionSchemaWalls, stage = 3, stageVision = 3, stageSequence = 3 }) { PracticeStrict = true };
            sim.S.stageSeconds = 30; sim.S.walls.Add("length"); sim.S.wallSeenAt = 30;
            Assert.AreEqual(XgSim.WallMonthShare, sim.MonthProgress, 1e-9);
            int advanced = 0;
            sim.StageAdvanced += from => advanced = from;
            typeof(XgSim).GetMethod("AdvanceStage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(sim, new object[] { 3 });
            Assert.AreEqual(3, advanced);
            Assert.AreEqual(4, sim.S.stage);
            Assert.AreEqual(0, sim.S.wallSeenAt);
            Assert.AreEqual(0, sim.MonthProgress, 1e-9, "the calendar never makes a player wait for the month to end");
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
