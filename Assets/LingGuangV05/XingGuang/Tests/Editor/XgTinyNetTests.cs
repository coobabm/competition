using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The one real training in the game (design v1.1 §11.8.1): tiny nets on 8×8 pictures, deterministic.</summary>
    public sealed class XgTinyNetTests
    {
        [Test]
        public void APerceptronCannotLearnXor()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var run = new XgTinyRun(XgTinyLesson.XorPerceptron, seed).Finish();
                Assert.That(run.TrainError, Is.GreaterThanOrEqualTo(.25), "one straight line never splits XOR (seed " + seed + ")");
            }
            // It cannot even with ten times the passes.
            var net = new XgTinyNet(XgTinyData.Pixels, 0, XgTinyRun.Seed);
            var data = XgTinyData.Xor();
            for (int i = 0; i < 600; i++) foreach (var s in data) net.Learn(s, .1);
            Assert.That(net.ErrorRate(data), Is.GreaterThanOrEqualTo(.25));
        }

        [Test]
        public void ATwoLayerMlpWithBackpropLearnsXor()
        {
            var run = new XgTinyRun(XgTinyLesson.XorMlp).Finish();
            Assert.That(run.TrainError, Is.EqualTo(0), "the game's seed solves it");
            Assert.That(run.loss[run.loss.Count - 1], Is.LessThan(.05f));
            Assert.That(run.loss[run.loss.Count - 1], Is.LessThan(run.loss[0] / 5), "the error visibly falls");
            int solved = 0;
            for (int seed = 1; seed <= 20; seed++) if (new XgTinyRun(XgTinyLesson.XorMlp, seed).Finish().TrainError == 0) solved++;
            Assert.That(solved, Is.GreaterThanOrEqualTo(15), "most seeds solve it; the few that do not sit in a local minimum");
        }

        [Test]
        public void ZeroOrNotOnEightByEightDigitsConverges()
        {
            var run = new XgTinyRun(XgTinyLesson.Zero).Finish();
            Assert.That(run.TrainError, Is.EqualTo(0));
            Assert.That(run.TestError, Is.LessThanOrEqualTo(.1), "unseen hand-drawn digits");
            Assert.That(run.loss[run.loss.Count - 1], Is.LessThan(run.loss[0] / 10));
            Assert.That(run.net.Predict(XgTinyData.TheZero().pixels), Is.True, "it knows the 0 of 0.txt, which it never trained on");
            foreach (var s in run.train) Assert.That(s.pixels, Is.Not.EqualTo(XgTinyData.TheZero().pixels));
        }

        [Test]
        public void SameSeedSameRun()
        {
            var a = new XgTinyRun(XgTinyLesson.Zero).Finish();
            var b = new XgTinyRun(XgTinyLesson.Zero);
            while (!b.Done) b.Step(7);
            Assert.That(b.loss, Is.EqualTo(a.loss), "stepping in pieces gives the same curve");
            Assert.That(b.net.w1, Is.EqualTo(a.net.w1));
            Assert.That(new XgTinyRun(XgTinyLesson.Zero, 5).Finish().loss, Is.Not.EqualTo(a.loss));
        }

        [Test]
        public void TheNetsAreTinyAndThePicturesAreEightByEight()
        {
            Assert.That(new XgTinyRun(XgTinyLesson.XorPerceptron).Parameters, Is.EqualTo(65));
            Assert.That(new XgTinyRun(XgTinyLesson.XorMlp).Parameters, Is.LessThan(300));
            Assert.That(new XgTinyRun(XgTinyLesson.Zero).Parameters, Is.LessThan(300));
            var xor = XgTinyData.Xor();
            Assert.That(xor.Count, Is.EqualTo(4));
            Assert.That(xor.FindAll(s => s.label).Count, Is.EqualTo(2));
            foreach (var s in XgTinyData.ZeroOrNot(20, 3)) Assert.That(s.pixels.Length, Is.EqualTo(64));
            var digits = new HashSet<int>();
            foreach (var s in XgTinyData.ZeroOrNot(40, 3)) { digits.Add(s.digit); Assert.That(s.label, Is.EqualTo(s.digit == 0)); }
            Assert.That(digits.Count, Is.EqualTo(10));
            Assert.That(new XgTinyNet(64, 3, 1).UnitWeights(2).Length, Is.EqualTo(64));
        }
    }
}
