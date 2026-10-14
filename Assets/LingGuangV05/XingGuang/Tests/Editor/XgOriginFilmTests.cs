using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The origin film as a whole: scene table, the master caption table (zh + en, markup), the personal numbers and the parameter HUD.</summary>
    public sealed class XgOriginFilmTests
    {
        [Test]
        public void ScenesTileTheFilm()
        {
            var s = XgOriginFilm.Spans;
            Assert.AreEqual(9, s.Length);
            Assert.AreEqual(0, s[0].Start, 1e-9);
            for (int i = 1; i < s.Length; i++) Assert.AreEqual(s[i - 1].End, s[i].Start, 1e-9, "scene " + i + " starts where the last ends");
            Assert.AreEqual(XgOriginFilm.Duration, s[s.Length - 1].End, 1e-9);
            Assert.AreEqual(0, XgOriginFilm.SceneAt(0)); Assert.AreEqual(1, XgOriginFilm.SceneAt(17)); Assert.AreEqual(2, XgOriginFilm.SceneAt(22.5));
            Assert.AreEqual(8, XgOriginFilm.SceneAt(160.9)); Assert.AreEqual(8, XgOriginFilm.SceneAt(161));
        }

        [Test]
        public void SceneWrapperFadesLikeMainTsx()
        {
            var cold = XgOriginFilm.Spans[0];
            Assert.AreEqual(0, cold.Alpha(0), 1e-9);
            Assert.AreEqual(1, cold.Alpha(5), 1e-9);
            Assert.AreEqual(1.02, cold.Scale(17), 1e-9);
            var fin = XgOriginFilm.Spans[8];
            Assert.AreEqual(1, fin.Alpha(150), 1e-9);
            Assert.AreEqual(0, fin.Alpha(161), 1e-9, "the finale fades out over its last 1.4 s");
            Assert.Greater(fin.Alpha(160), 0);
        }

        [Test]
        public void EveryCaptionHasEnglishAndBalancedMarkup()
        {
            Assert.AreEqual(47, XgOriginFilm.Captions.Length);
            double last = -1;
            for (int i = 0; i < XgOriginFilm.Captions.Length; i++)
            {
                var c = XgOriginFilm.Captions[i];
                Assert.IsNotEmpty(c.Zh, "zh " + i); Assert.IsNotEmpty(c.En, "en " + i);
                Assert.IsTrue(XgOriginFilm.MarkupBalanced(c.Zh), "zh markup " + i + ": " + c.Zh);
                Assert.IsTrue(XgOriginFilm.MarkupBalanced(c.En), "en markup " + i + ": " + c.En);
                if (c.ZhPersonal != null) { Assert.IsTrue(XgOriginFilm.MarkupBalanced(c.ZhPersonal), "zh personal " + i); StringAssert.Contains("<P>", c.ZhPersonal); Assert.IsNotNull(c.EnPersonal, "en personal " + i); }
                if (c.EnPersonal != null) { Assert.IsTrue(XgOriginFilm.MarkupBalanced(c.EnPersonal), "en personal " + i); StringAssert.Contains("<P>", c.EnPersonal); }
                Assert.Less(c.From, c.To, "caption " + i);
                Assert.GreaterOrEqual(c.From, last - 1e-9, "captions are in time order");
                last = c.From;
                StringAssert.DoesNotContain("涌现", c.Zh);
                if (c.ZhPersonal != null) StringAssert.DoesNotContain("涌现", c.ZhPersonal);
                // Where the Chinese marks a word, the English marks one too.
                if (c.Zh.IndexOfAny(new[] { '[', '{' }) >= 0) Assert.GreaterOrEqual(c.En.IndexOfAny(new[] { '[', '{' }), 0, "en keeps a highlight where zh has one: " + i);
            }
        }

        [Test]
        public void MarkupBalanceDetectsBrokenText()
        {
            Assert.IsTrue(XgOriginFilm.MarkupBalanced("a [b] c {d}"));
            Assert.IsFalse(XgOriginFilm.MarkupBalanced("a [b c"));
            Assert.IsFalse(XgOriginFilm.MarkupBalanced("a ]b["));
            Assert.IsFalse(XgOriginFilm.MarkupBalanced("[a {b}]"));
        }

        [Test]
        public void CaptionTimingFollowsTheFilm()
        {
            Assert.AreEqual(-1, XgOriginFilm.CaptionAt(18).Index, "no caption over the title card");
            var f = XgOriginFilm.CaptionAt(24);
            Assert.AreEqual(5, f.Index);
            Assert.AreEqual(1, f.Alpha, 1e-9);
            Assert.AreEqual(5, XgOriginFilm.CaptionAt(22.7).Index);
            Assert.AreEqual(0, XgOriginFilm.CaptionAt(22.7).Alpha, 1e-9);
            Assert.AreEqual(46, XgOriginFilm.CaptionAt(155).Index);
            Assert.AreEqual(-1, XgOriginFilm.CaptionAt(158).Index);
        }

        [Test]
        public void PersonalCaptionsUseThePlayersCount()
        {
            Assert.AreEqual("参数一点点地加。然后某一天——", XgOriginFilm.CaptionText(4, false, 2.13e8));
            Assert.AreEqual("The parameters grew bit by bit. And then one day—", XgOriginFilm.CaptionText(4, true, 2.13e8));
            StringAssert.Contains("3.2 亿", XgOriginFilm.CaptionText(4, false, 3.2e8));
            StringAssert.Contains("320 million", XgOriginFilm.CaptionText(4, true, 3.2e8));
            StringAssert.Contains("参数涨到两亿", XgOriginFilm.CaptionText(42, false, 2.13e8));
            StringAssert.Contains("参数涨到3.2 亿", XgOriginFilm.CaptionText(42, false, 3.2e8).Replace("到 ", "到"));
            StringAssert.Contains("两亿", XgOriginFilm.CaptionText(44, false, 2.13e8));
            Assert.AreEqual(XgOriginFilm.Captions[1].Zh, XgOriginFilm.CaptionText(1, false, 3.2e8), "captions without a number never change");
        }

        [Test]
        public void ParamHudClimbsAndFlashesOnTheBeats()
        {
            var h = new XgOriginFilm.HudFrame();
            XgOriginFilm.EvaluateHud(22.4, h, 2.13e8);
            Assert.AreEqual(0, h.Alpha, 1e-9);
            XgOriginFilm.EvaluateHud(30, h, 2.13e8);
            Assert.AreEqual("3", h.Text);
            Assert.AreEqual(0, h.Capabilities);
            XgOriginFilm.EvaluateHud(31.4, h, 2.13e8);
            Assert.AreEqual(1, h.Capabilities);
            Assert.Greater(h.Pulse, .5);
            XgOriginFilm.EvaluateHud(45, h, 2.13e8);
            Assert.AreEqual("17", h.Text);
            XgOriginFilm.EvaluateHud(143, h, 2.13e8);
            Assert.AreEqual(6, h.Capabilities);
            Assert.AreEqual(6, h.BreakOn.Count(x => x > .99));
            Assert.AreEqual("213,000,000", h.Text, "the film's own final count");
        }

        [Test]
        public void ParamHudEndsOnThePlayersCountAndNeverRunsBackwards()
        {
            var h = new XgOriginFilm.HudFrame();
            double prev = 0;
            for (double t = 23; t <= 145; t += .5)
            {
                XgOriginFilm.EvaluateHud(t, h, 3.2e8);
                Assert.GreaterOrEqual(h.Value, prev * .99, "counter at " + t);
                prev = h.Value;
            }
            XgOriginFilm.EvaluateHud(144, h, 3.2e8);
            Assert.AreEqual(3.2e8, h.Value, 3.2e8 * .01);
            // a tiny count must not drag the counter below the film's earlier beats
            var keys = XgOriginFilm.HudKeys(1e6);
            Assert.AreEqual(2.13e8, keys[11][1], 1);
            for (int i = 1; i < keys.Length; i++) Assert.GreaterOrEqual(keys[i][1], keys[i - 1][1]);
        }

        [Test]
        public void ChapterSixPlaysTheLiveFilmByDefault()
        {
            Assert.IsTrue(XgOriginFilm.LiveFilmByDefault);
            var sim = new XgSim();
            sim.S.stage = 5;
            Assert.IsFalse(sim.TakeChapterSixOrigin(), "not before chapter six");
            sim.S.stage = XgSim.OriginVideoStage;
            Assert.IsTrue(sim.TakeChapterSixOrigin(), "reaching chapter six asks for the film once");
            Assert.IsFalse(sim.TakeChapterSixOrigin());
            Assert.IsTrue(sim.S.originExplained);
            Assert.IsTrue(sim.OriginVideoFinished(), "ending or skipping it makes it ask 你觉得呢");
            Assert.IsTrue(sim.S.originAwaitingAnswer);
        }
    }
}
