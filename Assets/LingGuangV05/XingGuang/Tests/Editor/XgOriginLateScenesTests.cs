using NUnit.Framework;
using LingGuangV05.XingGuang;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Key beats of the origin film's stage 4–6 and finale timelines (pure functions of scene-local time).</summary>
    public sealed class XgOriginLateScenesTests
    {
        const double Eps = 1e-6;

        [Test]
        public void SceneWindowsTileTheFilmEnd()
        {
            Assert.AreEqual(77.5, XgOriginStage4.Start, Eps); Assert.AreEqual(105, XgOriginStage4.End, Eps);
            Assert.AreEqual(XgOriginStage4.End, XgOriginStage5.Start, Eps); Assert.AreEqual(124, XgOriginStage5.End, Eps);
            Assert.AreEqual(XgOriginStage5.End, XgOriginStage6.Start, Eps); Assert.AreEqual(145, XgOriginStage6.End, Eps);
            Assert.AreEqual(XgOriginStage6.End, XgOriginFinale.Start, Eps); Assert.AreEqual(161, XgOriginFinale.End, Eps);
            Assert.AreEqual(1.4, XgOriginFinale.FadeOut, Eps);
        }

        [Test]
        public void Stage4SignalFadesThenHighwayFlashes()
        {
            var f = new XgOriginStage4.Frame();
            XgOriginStage4.Evaluate(.8, f);
            Assert.AreEqual(1, f.Sig, 1e-6, "the signal starts at full strength at the key word");
            XgOriginStage4.Evaluate(6.4, f);
            Assert.Less(f.Sig, 1e-3, "after thirty steps the signal is gone");
            Assert.IsFalse(f.Hit);
            XgOriginStage4.Evaluate(15.99, f);
            Assert.IsFalse(f.Hit);
            Assert.AreEqual(0, f.Flash, Eps);
            XgOriginStage4.Evaluate(16.0, f);
            Assert.IsTrue(f.Hit);
            Assert.AreEqual(1, f.Flash, 1e-6, "the flash peaks the instant the highway lands");
            XgOriginStage4.Evaluate(16.45, f);
            Assert.AreEqual(0, f.Flash, 1e-6);
        }

        [Test]
        public void Stage4PlotAndResNetBeats()
        {
            var f = new XgOriginStage4.Frame();
            XgOriginStage4.Evaluate(10, f);
            Assert.Greater(f.PlotA, .99);
            Assert.Greater(f.Draw, .5);
            XgOriginStage4.Evaluate(20, f);
            Assert.AreEqual(0, f.PlotA, Eps);
            Assert.Greater(f.ResA, .99);
            XgOriginStage4.Evaluate(22.2, f);
            Assert.AreEqual(152, f.Layers, "ResNet reaches 152 layers");
            XgOriginStage4.Evaluate(.5, f);
            Assert.AreEqual(20, f.Layers);
        }

        [Test]
        public void Stage4CurvesDecayAndSpeechTypes()
        {
            Assert.AreEqual(120, XgOriginStage4.C20.Length);
            Assert.Greater(XgOriginStage4.C56[119], XgOriginStage4.C20[119], "the 56-layer net ends worse");
            var f = new XgOriginStage4.Frame();
            XgOriginStage4.Evaluate(23.0, f);
            Assert.IsFalse(f.Speech.Visible);
            XgOriginStage4.Evaluate(24.65, f);
            Assert.IsTrue(f.Speech.Visible);
            Assert.AreEqual(8, f.Speech.Chars, "eight chars per second, one second in");
            XgOriginStage4.Evaluate(27, f);
            Assert.AreEqual(XgOriginStage4.SpeechTotal, f.Speech.Chars);
        }

        [Test]
        public void Stage5SqueezeLosesTheTail()
        {
            double x, y, size, op; bool lost;
            XgOriginStage5.SrcWord(0, 4, out x, out y, out size, out op, out lost);
            Assert.AreEqual(0, op, Eps, "a packed word disappears into the vector");
            Assert.IsFalse(lost);
            XgOriginStage5.SrcWord(12, 3.6, out x, out y, out size, out op, out lost);
            Assert.IsTrue(lost, "words past the eighth slot are dropped");
            XgOriginStage5.SrcWord(12, 5.5, out x, out y, out size, out op, out lost);
            Assert.AreEqual(0, op, 1e-6);
        }

        [Test]
        public void Stage5AttentionSweepsAndBirdMoves()
        {
            var f = new XgOriginStage5.Frame();
            XgOriginStage5.Evaluate(7.0, f);
            Assert.AreEqual(0, f.Cur);
            XgOriginStage5.Evaluate(11.5, f);
            Assert.AreEqual(4, f.Cur);
            XgOriginStage5.Evaluate(13.0, f);
            Assert.AreEqual(0, f.WordIdx);
            Assert.Greater(f.CA, .9);
            XgOriginStage5.Evaluate(15.5, f);
            Assert.AreEqual(4, f.WordIdx);
            Assert.AreEqual(XgOriginStage5.Foci[4].R, f.FocusR, 1);
            Assert.AreEqual(5, XgOriginStage5.Att.Length);
            foreach (var row in XgOriginStage5.Att)
            {
                double sum = 0; foreach (var v in row) sum += v;
                Assert.AreEqual(1, sum, .011, "attention rows sum to one");
            }
        }

        [Test]
        public void Stage6SerialThenRingThenBoom()
        {
            var f = new XgOriginStage6.Frame();
            XgOriginStage6.Evaluate(1, f);
            Assert.AreEqual(2, f.Step);
            XgOriginStage6.Evaluate(3, f);
            Assert.AreEqual(8, f.Step);
            XgOriginStage6.Evaluate(8, f);
            Assert.Greater(f.TitleA, .9);
            Assert.AreEqual(.25, f.TokDim, Eps);
            XgOriginStage6.Evaluate(11.5, f);
            Assert.AreEqual(1, f.ToRing, Eps);
            Assert.AreEqual(960, f.Cx, Eps);
            XgOriginStage6.Evaluate(13.4, f);
            Assert.AreEqual(560, f.Cx, 1e-6);
            Assert.IsFalse(XgOriginStage6.CapsOn(13.4));
            Assert.IsTrue(XgOriginStage6.CapsOn(XgOriginStage6.Boom));
            XgOriginStage6.Evaluate(XgOriginStage6.Boom, f);
            Assert.AreEqual(1, f.BoomK, Eps, "the boom shake is strongest at the boom");
            XgOriginStage6.Evaluate(XgOriginStage6.Boom + .8, f);
            Assert.AreEqual(0, f.BoomK, Eps);
            Assert.AreEqual(0, f.ShakeX, Eps);
        }

        [Test]
        public void Stage6RingPositionsAreOnACircle()
        {
            var f = new XgOriginStage6.Frame();
            XgOriginStage6.Evaluate(11.2, f);
            for (int i = 0; i < 8; i++)
            {
                double dx = f.PosX[i] - f.Cx, dy = f.PosY[i] - 470;
                Assert.AreEqual(f.R, System.Math.Sqrt(dx * dx + dy * dy), 1e-6);
            }
        }

        [Test]
        public void FinaleCollapsesBurstsAndFades()
        {
            var f = new XgOriginFinale.Frame();
            XgOriginFinale.Evaluate(3, f);
            Assert.AreEqual(0, f.Pull, Eps);
            Assert.AreEqual(0, f.TitleA, Eps);
            Assert.AreEqual(0, f.Flash, Eps);
            XgOriginFinale.Evaluate(XgOriginFinale.Boom - .001, f);
            Assert.AreEqual(0, f.TitleA, Eps, "no title before the boom");
            Assert.Greater(f.Pull, .99);
            XgOriginFinale.Evaluate(XgOriginFinale.Boom, f);
            Assert.AreEqual(1, f.Flash, 1e-6);
            Assert.AreEqual(0, f.Slam, 1e-6, "the spring starts from rest");
            XgOriginFinale.Evaluate(XgOriginFinale.Boom + 1.5, f);
            Assert.AreEqual(1, f.Slam, .35, "the slam settles near 1 after overshoot");
            Assert.AreEqual(1, f.TitleA, 1e-6);
            XgOriginFinale.Evaluate(14, f);
            Assert.Less(f.TitleA, .2, "the title dissolves");
            XgOriginFinale.Evaluate(15.5, f);
            Assert.AreEqual(0, f.TitleA, 1e-6);
            Assert.AreEqual(1, f.RefsA, 1e-6);
        }

        [Test]
        public void FinaleTitleIsTheFilmsOwnWord()
        {
            Assert.AreEqual("涌现", XgOriginFinale.TitleZh);
        }

        [Test]
        public void FinaleDataIsDeterministic()
        {
            Assert.AreEqual(320, XgOriginFinale.Sparks.Length);
            Assert.AreEqual(180, XgOriginFinale.Embers.Length);
            double x, y, op, back;
            Assert.IsFalse(XgOriginFinale.SparkAt(0, -.1, out x, out y, out op, out back));
            Assert.IsTrue(XgOriginFinale.SparkAt(0, .05, out x, out y, out op, out back));
            Assert.IsFalse(XgOriginFinale.SparkAt(0, 10, out x, out y, out op, out back), "sparks burn out");
            Assert.AreEqual(XgOriginColdOpen.Rand("fa5") * System.Math.PI * 2, XgOriginFinale.Sparks[5].A, Eps);
        }

        [Test]
        public void SpringMatchesRemotionShape()
        {
            Assert.AreEqual(0, XgOriginFx.Spring(0, 8, 150, .7), 1e-9);
            double peak = 0;
            for (int f = 0; f < 60; f++) peak = System.Math.Max(peak, XgOriginFx.Spring(f, 8, 150, .7));
            Assert.Greater(peak, 1.0, "underdamped: it overshoots");
            Assert.AreEqual(1, XgOriginFx.Spring(120, 8, 150, .7), .01);
        }

        [Test]
        public void StageTagFadesInAndYearResolves()
        {
            double a; string y;
            XgOriginFx.StageTag(0, "1997 / 2015", out a, out y);
            Assert.AreEqual(0, a, Eps);
            XgOriginFx.StageTag(1, "1997 / 2015", out a, out y);
            Assert.AreEqual(1, a, Eps);
            Assert.AreEqual("1997 / 2015", y);
        }
    }
}
