using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// The tiny conv net behind the sheet's 「它怎么看的」 and the gradients behind the correction animation
    /// (它在学_可视化借鉴设计 §2.3, §2.4): real training, real backprop, deterministic, and the old nets untouched.
    /// </summary>
    public sealed class XgTinyConvTests
    {
        [Test]
        public void TheConvNetLearnsIsItAZeroOnPicturesItNeverSaw()
        {
            var net = XgTinyConvNet.Train(1, XgTinyData.ZeroOrNot(120, 5), 10, .05, 5);
            var held = XgTinyData.ZeroOrNot(200, 6);
            Assert.That(net.ErrorRate(held), Is.LessThan(.1), "above 90% right on held-out pictures");
            var untrained = new XgTinyConvNet(1, 5);
            Assert.That(untrained.ErrorRate(held), Is.GreaterThan(net.ErrorRate(held)), "the training did it");
        }

        [Test]
        public void TheSheetsNetNamesDigitsItNeverSaw()
        {
            var net = XgTinyConvNet.Digits;
            Assert.That(net.ErrorRate(XgTinyConvNet.DigitSet(20, 77)), Is.LessThan(.15));
            Assert.That(net.ParameterCount, Is.EqualTo(4 * 9 + 4 + 36 * 10 + 10));
        }

        [Test]
        public void SameSeedSameWeights()
        {
            var a = XgTinyConvNet.Train(10, XgTinyConvNet.DigitSet(6, 3), 3, .1, 9);
            var b = XgTinyConvNet.Train(10, XgTinyConvNet.DigitSet(6, 3), 3, .1, 9);
            CollectionAssert.AreEqual(a.kernels, b.kernels);
            CollectionAssert.AreEqual(a.dense, b.dense);
            var c = XgTinyConvNet.Train(10, XgTinyConvNet.DigitSet(6, 3), 3, .1, 10);
            CollectionAssert.AreNotEqual(a.kernels, c.kernels);
            var x = XgTinyData.Digit(3, 1).pixels;
            CollectionAssert.AreEqual(a.Trace(x).probs, b.Trace(x).probs);
        }

        [Test]
        public void ConvGradientsMatchFiniteDifferences()
        {
            foreach (int classes in new[] { 1, 10 })
            {
                var net = XgTinyConvNet.Train(classes, XgTinyConvNet.DigitSet(3, 4), 2, .05, 4);
                var s = XgTinyData.Digit(6, 123);
                if (classes == 1) s.label = false;
                var g = net.Gradients(s);
                Check(net.kernels, g.kernels, () => net.CrossEntropy(s), "kernel");
                Check(net.kernelBias, g.kernelBias, () => net.CrossEntropy(s), "kernel bias");
                Check(net.dense, g.dense, () => net.CrossEntropy(s), "dense");
                Check(net.denseBias, g.denseBias, () => net.CrossEntropy(s), "dense bias");
            }
        }

        [Test]
        public void MlpGradientsMatchFiniteDifferencesAndLearnsStep()
        {
            var run = new XgTinyRun(XgTinyLesson.Microscope);
            run.Step(5);
            var net = run.net;
            var s = XgTinyData.Digit(0, 42);
            var g = net.Gradients(s);
            Check(net.w1, g.w1, () => net.CrossEntropy(s), "w1");
            Check(net.b1, g.b1, () => net.CrossEntropy(s), "b1");
            Check(net.w2, g.w2, () => net.CrossEntropy(s), "w2");
            double b2 = net.b2, eps = 1e-6;
            net.b2 = b2 + eps; double up = net.CrossEntropy(s);
            net.b2 = b2 - eps; double down = net.CrossEntropy(s);
            net.b2 = b2;
            Assert.That(g.b2, Is.EqualTo((up - down) / (2 * eps)).Within(1e-5));
            Assert.That(g.error, Is.EqualTo(g.output - 1).Within(1e-12));

            // Gradients changes nothing, and stepping along it is exactly Learn.
            var a = net.Clone(); var b = net.Clone();
            a.Learn(s, .03);
            b.Step(b.Gradients(s), .03);
            for (int i = 0; i < a.w1.Length; i++) Assert.That(b.w1[i], Is.EqualTo(a.w1[i]).Within(1e-15));
            for (int i = 0; i < a.w2.Length; i++) Assert.That(b.w2[i], Is.EqualTo(a.w2[i]).Within(1e-15));
            Assert.That(b.b2, Is.EqualTo(a.b2).Within(1e-15));
        }

        static void Check(double[] w, double[] g, Func<double> loss, string what)
        {
            const double eps = 1e-6;
            for (int i = 0; i < w.Length; i++)
            {
                double keep = w[i];
                w[i] = keep + eps; double up = loss();
                w[i] = keep - eps; double down = loss();
                w[i] = keep;
                double numeric = (up - down) / (2 * eps);
                Assert.That(g[i], Is.EqualTo(numeric).Within(1e-5 + 1e-4 * Math.Abs(numeric)), what + " " + i);
            }
        }

        [Test]
        public void ThePerceptronAndMlpRunsAreUnchanged()
        {
            // Final loss, a weighted weight sum and the held-out error, recorded before the conv path was added.
            Expect(XgTinyLesson.XorPerceptron, 0.5f, -1.2374053895473485, 0.5);
            Expect(XgTinyLesson.XorMlp, 0.015734056f, -39.7973932107952, 0);
            Expect(XgTinyLesson.Zero, 0.0005330297f, -0.7974946459194936, 0.025);
            Expect(XgTinyLesson.Microscope, 0.00075552263f, -1.8945268207851715, 0.0625);
        }

        static void Expect(XgTinyLesson lesson, float loss, double sum, double testError)
        {
            var r = new XgTinyRun(lesson).Finish();
            double s = 0;
            foreach (var w in r.net.w1) s += w * 1.37;
            foreach (var w in r.net.w2) s += w;
            Assert.That(r.loss[r.loss.Count - 1], Is.EqualTo(loss), lesson.ToString());
            Assert.That(s + r.net.b2, Is.EqualTo(sum).Within(1e-9), lesson.ToString());
            Assert.That(r.TestError, Is.EqualTo(testError).Within(1e-12), lesson.ToString());
        }

        [Test]
        public void ALookAlwaysGivesTheSheetsAnswer()
        {
            var net = XgTinyConvNet.Digits;
            int mixed = 0, wrongLooks = 0;
            for (int seed = 1; seed <= 300; seed++)
            {
                int digit = seed % 10, asked = seed % 3 == 0 ? digit : (digit + seed % 7 + 1) % 10;
                foreach (bool said in new[] { true, false })
                {
                    var look = XgTinyLook.Look(net, seed, digit, asked, said);
                    Assert.That(look.pYes > .5, Is.EqualTo(said), "seed " + seed + " " + digit + "/" + asked);
                    Assert.That(look.sample.digit, Is.EqualTo(digit), "the picture's label stays honest");
                    Assert.That(look.key, Is.InRange(0, XgTinyConvNet.Kernels - 1));
                    if (look.blendDigit >= 0) mixed++;
                    if (!look.Right) wrongLooks++;
                    var again = XgTinyLook.Look(net, seed, digit, asked, said);
                    CollectionAssert.AreEqual(look.sample.pixels, again.sample.pixels, "deterministic");
                }
            }
            Assert.That(wrongLooks, Is.EqualTo(300));
            Assert.That(mixed, Is.LessThan(400), "most answers come from plain drawings");
        }

        [Test]
        public void ACorrectionIsARealWrongAnswerAndAStepAgainstIt()
        {
            for (int seed = 1; seed <= 40; seed++)
                foreach (bool truth in new[] { true, false })
                {
                    var c = XgTinyLook.Correction(seed, truth);
                    Assert.That(c.before.Predict(c.sample.pixels), Is.Not.EqualTo(truth), "it was wrong");
                    Assert.That(c.sample.label, Is.EqualTo(truth));
                    Assert.That(c.after.CrossEntropy(c.sample), Is.LessThan(c.before.CrossEntropy(c.sample)), "the step helps");
                    Assert.That(Math.Abs(c.grad.error), Is.GreaterThan(.5), "a wrong answer is far off");
                }
            var a = XgTinyLook.Correction(7, true); var b = XgTinyLook.Correction(7, true);
            CollectionAssert.AreEqual(a.after.w1, b.after.w1);
        }
    }
}
