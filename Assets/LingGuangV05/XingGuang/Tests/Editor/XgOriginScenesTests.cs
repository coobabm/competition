using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Key beats of the title card and stages 1–3 (pure functions of scene-local time) against the Remotion sources.</summary>
    public sealed class XgOriginScenesTests
    {
        [Test]
        public void TitleBurstAndFlash()
        {
            var f = new XgOriginTitle.Frame();
            XgOriginTitle.Evaluate(0, f);
            Assert.AreEqual(1, f.Flash, 1e-9);
            Assert.AreEqual(0, f.TitleAlpha, 1e-9);
            Assert.AreEqual(1.14, f.Scale, 1e-9);
            XgOriginTitle.Evaluate(2, f);
            Assert.AreEqual(0, f.Flash, 1e-9);
            Assert.AreEqual(1, f.TitleAlpha, 1e-9);
            Assert.AreEqual(1, f.Scale, 1e-9);
            Assert.AreEqual(140, XgOriginTitle.ParticleCount);
            Assert.IsTrue(XgOriginTitle.ParticleAt(0, 0).Alive);
            Assert.IsFalse(XgOriginTitle.ParticleAt(0, 4).Alive, "every particle is gone after 3.4 s");
            Assert.AreEqual(XgOriginColdOpen.Rand("pa0") * System.Math.PI * 2, XgOriginTitle.Angle[0], 1e-12);
            StringAssert.DoesNotContain("涌现", XgOriginTitle.TitleZh + XgOriginTitle.SubtitleZh);
        }

        [Test]
        public void Stage1RunsARealPerceptron()
        {
            Assert.AreEqual(46, XgOriginStage1.Points.Length);
            Assert.AreEqual(12, XgOriginStage1.States.Length, "the recorded run is thinned to 12 states");
            var f = new XgOriginStage1.Frame();
            XgOriginStage1.Evaluate(1, f);
            Assert.AreEqual(XgOriginStage1.States[0].W0, f.W0, 1e-12, "before training starts the line is the first state");
            Assert.AreEqual(XgOriginStage1.Out.No, f.Output, "the output blinks yes/no: at 1 s floor(1.25) is odd -> no");
            XgOriginStage1.Evaluate(2, f);
            Assert.AreEqual(XgOriginStage1.Out.Yes, f.Output, "at 2 s floor(2.5) is even -> yes");
        }

        [Test]
        public void Stage1TrainsThenSwitchesToXor()
        {
            var f = new XgOriginStage1.Frame();
            XgOriginStage1.Evaluate(9, f);
            Assert.AreEqual(XgOriginStage1.Points.Length, f.Correct, "after training the line separates all 46 points");
            Assert.IsFalse(f.XorPhase);
            XgOriginStage1.Evaluate(12, f);
            Assert.IsTrue(f.XorPhase);
            int wrong = 0; foreach (bool w in f.Wrong) if (w) wrong++;
            Assert.LessOrEqual(wrong, 2, "a line gets at most half of the XOR wrong (the film flips when it would be more)");
            XgOriginStage1.Evaluate(13.6, f);
            Assert.AreEqual(1, f.Stamp, 1e-9);
        }

        [Test]
        public void Stage2PlateauThenSnap()
        {
            var f = new XgOriginStage2.Frame();
            XgOriginStage2.Evaluate(10.5, f);
            Assert.AreEqual(2, f.Correct, "stuck at 2/4 on the plateau");
            Assert.Less(f.Train, .2);
            XgOriginStage2.Evaluate(11.3, f);
            Assert.AreEqual(1, f.Train, 1e-9);
            Assert.AreEqual(4, f.Correct, "XOR solved all at once");
            Assert.Greater(f.SnapFlash, 0);
            XgOriginStage2.Evaluate(13, f);
            Assert.AreEqual(0, f.NetA, 1e-9, "the network fades out for the cards");
            Assert.AreEqual(12, XgOriginStage2.Edges.Length);
            Assert.AreEqual((XgOriginColdOpen.Rand("w00") - .5) * 1.6, XgOriginStage2.W0[0], 1e-12);
            Assert.AreEqual((XgOriginColdOpen.Rand("w111") - .5) * 3.2, XgOriginStage2.W1[11], 1e-12);
        }

        [Test]
        public void Stage3PixelsAndKernel()
        {
            Assert.AreEqual(256, XgOriginStage3.Pix.Length);
            int on = 0; foreach (int p in XgOriginStage3.Pix) on += p;
            Assert.AreEqual(54, on, "the seven has 54 lit pixels");
            Assert.AreEqual(1, XgOriginStage3.FeatureMap.Length > 0 ? System.Linq.Enumerable.Max(XgOriginStage3.FeatureMap) : 0, 1e-12, "normalised to the strongest edge");
            var f = new XgOriginStage3.Frame();
            XgOriginStage3.Evaluate(0, f);
            XgOriginStage3.PixelAt(17, f, out double x, out double y, out double s);
            Assert.AreEqual(960 - 176 + 1 * 22, x, 1e-9, "the pixel starts on the centred 16x16 grid");
            Assert.AreEqual(300 + 1 * 22, y, 1e-9);
            Assert.AreEqual(20, s, 1e-9);
            XgOriginStage3.Evaluate(4, f);
            XgOriginStage3.PixelAt(17, f, out x, out y, out s);
            Assert.AreEqual(XgOriginStage3.ImgX + 1 * 16, x, 1e-9, "then it lies in the input panel");
            Assert.AreEqual(14.5, s, 1e-9);
            XgOriginStage3.Evaluate(9.4, f);
            Assert.AreEqual(195, f.KIdx);
            Assert.AreEqual(14 * 14 - 1, f.KIdx);
            XgOriginStage3.Evaluate(12.15, f);
            Assert.AreEqual(3, f.Dot, .01, "the RNN dot is halfway along the seven tokens");
        }
    }
}
