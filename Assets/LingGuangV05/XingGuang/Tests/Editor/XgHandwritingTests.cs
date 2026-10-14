using System;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgHandwritingTests
    {
        sealed class Host : IXgHost
        {
            public double Money { get; private set; } = 1000;
            public double Compute => 10;
            public double VramMB => 4096;
            public string Blocker { get; set; }
            public void Earn(double amount) { Money += amount; }
            public bool Spend(double amount) { if (amount < 0 || Money < amount) return false; Money -= amount; return true; }
            public void Train(double seconds) { }
        }

        static XgSim OpenSim()
        {
            var s = new XgSim();
            s.S.desksOpen.Add(XgSim.HandwritingDesk);
            return s;
        }

        static bool[] Stroke(float x0, float y0, float x1, float y1)
        {
            var r = new bool[XgHandwriting.Grid * XgHandwriting.Grid];
            XgHandwriting.Line(r, x0, y0, x1, y1);
            return r;
        }

        /// <summary>Accuracy of a recogniser on fresh drawings of one writer (20 per digit), and its mean confidence.</summary>
        static double Accuracy(XgHandwritingRecognizer model, HandwritingWriter writer, out double confidence)
        {
            var rng = new Random(999); int right = 0, n = 0; confidence = 0;
            for (int i = 0; i < 20; i++) for (int d = 0; d < 10; d++)
            {
                var guess = model.Recognize(XgHandwriting.Extract(writer.Draw(d, rng)));
                if (guess.digit == d) right++;
                confidence += guess.confidence; n++;
            }
            confidence /= n;
            return right / (double)n;
        }

        static void Teach(XgHandwritingRecognizer model, HandwritingWriter writer, Random rng, int from, int to)
        {
            for (int i = from; i < to; i++) { int d = i % 10; model.Teach(d, XgHandwriting.Extract(writer.Draw(d, rng))); }
        }

        // ───────────── the recogniser ─────────────

        [Test] public void BlankDotAndTickAreNotUsable()
        {
            Assert.IsFalse(XgHandwriting.Usable(new bool[XgHandwriting.Grid * XgHandwriting.Grid]));
            var dot = new bool[XgHandwriting.Grid * XgHandwriting.Grid]; XgHandwriting.Dot(dot, 30, 30);
            Assert.IsFalse(XgHandwriting.Usable(dot)); Assert.IsNull(XgHandwriting.Extract(dot));
            Assert.IsFalse(XgHandwriting.Usable(Stroke(30, 30, 36, 33)));
            Assert.IsFalse(XgHandwriting.Usable(null));
            Assert.IsFalse(XgHandwriting.Usable(new bool[10]));
            Assert.IsTrue(XgHandwriting.Usable(Stroke(32, 8, 32, 56)));
        }

        [Test] public void FeaturesIgnorePositionAndSizeAndHaveUnitLength()
        {
            var a = XgHandwriting.Extract(Stroke(20, 10, 20, 50));
            var b = XgHandwriting.Extract(Stroke(44, 20, 44, 44));
            Assert.AreEqual(XgHandwriting.Length, a.Length);
            double norm = 0, dot = 0; for (int i = 0; i < a.Length; i++) { norm += a[i] * a[i]; dot += a[i] * b[i]; }
            Assert.AreEqual(1, norm, 1e-4);
            Assert.Greater(dot, .95, "the same shape elsewhere and smaller reads the same");
        }

        [Test] public void EveryBuiltInTemplateReadsAsItself()
        {
            var model = new XgHandwritingRecognizer();
            Assert.AreEqual(10 * XgHandwriting.TemplatesPerDigit, model.BuiltInCount);
            Assert.AreEqual(0, model.LearnedCount);
            for (int d = 0; d < 10; d++)
                for (int v = 0; v < XgHandwriting.TemplatesPerDigit; v++)
                    Assert.AreEqual(d, model.Recognize(XgHandwriting.Extract(XgHandwriting.Template(d, v))).digit, "digit " + d);
        }

        [Test] public void InvalidInputsAreIgnored()
        {
            var model = new XgHandwritingRecognizer();
            Assert.IsFalse(model.Teach(10, new float[XgHandwriting.Length]));
            Assert.IsFalse(model.Teach(-1, new float[XgHandwriting.Length]));
            Assert.IsFalse(model.Teach(3, null));
            Assert.IsFalse(model.Teach(3, new float[5]));
            Assert.IsFalse(model.Recognize(null).valid);
            Assert.IsFalse(model.Recognize(new float[3]).valid);
            Assert.IsFalse(new XgHandwritingRecognizer(false).Recognize(new float[XgHandwriting.Length]).valid);
        }

        [Test] public void OddsAddUpAndConfidenceNeverExceedsThem()
        {
            var model = new XgHandwritingRecognizer();
            var guess = model.Recognize(XgHandwriting.Extract(XgHandwriting.Template(4, 0)));
            double sum = 0; foreach (double p in guess.odds) sum += p;
            Assert.AreEqual(1, sum, 1e-9);
            Assert.LessOrEqual(guess.confidence, guess.odds[guess.digit] + 1e-12);
            Assert.GreaterOrEqual(guess.confidence, 0);
        }

        [Test] public void ItStartsPoorAndGetsBetterAsDrawingsAreTaught()
        {
            int[] marks = { 0, 10, 30, 60 };
            var accuracy = new double[marks.Length]; var confidence = new double[marks.Length];
            const int writers = 6;
            for (int w = 1; w <= writers; w++)
            {
                var writer = new HandwritingWriter(w * 77); var model = new XgHandwritingRecognizer(); var rng = new Random(w);
                double first = 0, last = 0;
                for (int m = 0; m < marks.Length; m++)
                {
                    Teach(model, writer, rng, m == 0 ? 0 : marks[m - 1], marks[m]);
                    double a = Accuracy(model, writer, out double c);
                    accuracy[m] += a / writers; confidence[m] += c / writers;
                    if (m == 0) first = a; last = a;
                }
                Assert.Greater(last, first + .25, "writer " + w + " must read clearly better after 60 drawings");
            }
            TestContext.WriteLine($"held-out accuracy after 0/10/30/60 taught drawings: {accuracy[0]:P0} / {accuracy[1]:P0} / {accuracy[2]:P0} / {accuracy[3]:P0}; confidence {confidence[0]:P0} / {confidence[1]:P0} / {confidence[2]:P0} / {confidence[3]:P0}");
            Assert.Less(accuracy[0], .6, "visibly poor before it has seen your hand");
            Assert.Greater(accuracy[1], accuracy[0] + .2);
            Assert.Greater(accuracy[2], .88);
            Assert.GreaterOrEqual(accuracy[3], accuracy[2] - .02);
            Assert.Greater(confidence[3], confidence[0] + .2, "it also becomes surer");
        }

        [Test] public void LearnedMemoryIsCappedAndForgetsTheOldestFirst()
        {
            var model = new XgHandwritingRecognizer();
            var f = XgHandwriting.Extract(Stroke(32, 8, 32, 56));
            for (int i = 0; i < XgHandwritingRecognizer.LearnedCapacity + 25; i++) Assert.IsTrue(model.Teach(1, f));
            Assert.AreEqual(XgHandwritingRecognizer.LearnedCapacity, model.LearnedCount);
            Assert.AreEqual(10 * XgHandwriting.TemplatesPerDigit, model.BuiltInCount, "the built-in templates stay");
        }

        // ───────────── the desk: credit, pay, rejection, save ─────────────

        [Test] public void AHandedInDrawingIsOneHandLabelledSampleWithTheDesksHandPay()
        {
            var s = OpenSim(); var h = new Host();
            double labels = s.Labels("mnist"), samples = s.Samples("mnist"), money = h.Money, pay = s.ManualPayFor("mnist", s.LevelOf("mnist"));
            int hand = s.S.handCorrect, done = s.DayDone, combo = s.S.combo;
            var r = s.SubmitHandwriting(7, XgHandwriting.Template(7, 0), 30, h);
            Assert.IsTrue(r.accepted); Assert.IsFalse(r.rejected);
            Assert.AreEqual(labels + 1, s.Labels("mnist"));
            Assert.AreEqual(samples + 1, s.Samples("mnist"), 1e-9, "the data page and the ability bar read Samples()");
            Assert.AreEqual(hand + 1, s.S.handCorrect);
            Assert.AreEqual(done + 1, s.DayDone);
            Assert.AreEqual(pay, r.pay, 1e-9);
            Assert.AreEqual(money + pay, h.Money, 1e-9, "no speed bonus after 30 seconds");
            Assert.AreEqual(combo, s.S.combo, "the shared combo is left alone");
            Assert.AreEqual(1, s.HandwritingMemory);
            Assert.AreEqual(1, s.S.miniActionDrawings.Count);
            Assert.AreEqual(7, s.S.miniActionDrawings[0].digit);
        }

        [Test] public void AFastRightReadingAddsASpeedBonusAndASlowOrWrongOneDoesNot()
        {
            var raster = XgHandwriting.Template(5, 0);
            var s = OpenSim(); var h = new Host(); double pay = s.ManualPayFor("mnist", s.LevelOf("mnist"));
            s.LearnHandwriting(5, raster); // it has seen this very hand before, so it reads it right
            double before = h.Money;
            var fast = s.SubmitHandwriting(5, raster, 1, h);
            Assert.IsTrue(fast.hit);
            Assert.AreEqual(pay * (1 - 1 / XgSim.HandwritingFastSeconds), fast.bonus, 1e-9);
            Assert.AreEqual(before + pay + fast.bonus, h.Money, 1e-9);

            before = h.Money;
            var slow = s.SubmitHandwriting(5, raster, XgSim.HandwritingFastSeconds + .5, h);
            Assert.IsTrue(slow.hit); Assert.AreEqual(0, slow.bonus); Assert.AreEqual(before + pay, h.Money, 1e-9);

            // Asked for an 8 but this hand has only ever been taught as a 5: wrong reading, still paid, no bonus, no fine.
            before = h.Money;
            var miss = s.SubmitHandwriting(8, raster, 0.5, h);
            Assert.IsFalse(miss.hit); Assert.AreEqual(5, miss.guess); Assert.AreEqual(0, miss.bonus);
            Assert.AreEqual(before + pay, h.Money, 1e-9);
            Assert.Less(s.S.handWrong, 1, "a misread drawing is not a wrong answer");
        }

        [Test] public void EmptyDotAndTickDrawingsAreRejectedAndNeverPaid()
        {
            var s = OpenSim(); var h = new Host();
            double money = h.Money, labels = s.Labels("mnist");
            var dot = new bool[XgHandwriting.Grid * XgHandwriting.Grid]; XgHandwriting.Dot(dot, 20, 20);
            foreach (var raster in new[] { new bool[XgHandwriting.Grid * XgHandwriting.Grid], dot, Stroke(10, 10, 15, 12), null, new bool[3] })
            {
                var r = s.SubmitHandwriting(3, raster, 1, h);
                Assert.IsTrue(r.rejected); Assert.IsFalse(r.accepted);
            }
            Assert.IsTrue(s.SubmitHandwriting(10, Stroke(32, 8, 32, 56), 1, h).rejected, "no such digit");
            Assert.AreEqual(money, h.Money); Assert.AreEqual(labels, s.Labels("mnist"));
            Assert.AreEqual(0, s.HandwritingMemory); Assert.AreEqual(0, s.S.handwritingDrawn);
            Assert.AreEqual(0, s.S.handCorrect);
        }

        [Test] public void ALockedDeskTakesNoDrawings()
        {
            var s = new XgSim(); var h = new Host();
            s.S.desksOpen.Remove(XgSim.HandwritingDesk);
            Assert.IsTrue(s.SubmitHandwriting(7, XgHandwriting.Template(7, 0), 1, h).rejected);
            Assert.AreEqual(1000, h.Money);
        }

        [Test] public void TheRecentWindowKeepsTwentyAndTheAccuracyFollowsIt()
        {
            var s = OpenSim(); var h = new Host();
            Assert.AreEqual(0, s.HandwritingAccuracy(out int none)); Assert.AreEqual(0, none);
            var one = XgHandwriting.Template(1, 0);
            s.LearnHandwriting(1, one);
            for (int i = 0; i < 12; i++) Assert.IsTrue(s.SubmitHandwriting(1, one, 1, h).hit);
            for (int i = 0; i < 10; i++) Assert.IsFalse(s.SubmitHandwriting(2, one, 1, h).hit);
            double accuracy = s.HandwritingAccuracy(out int count);
            Assert.AreEqual(XgSim.HandwritingWindow, count);
            Assert.AreEqual(10 / 20.0, accuracy, 1e-9, "the last twenty are 10 right, 10 not (the first two right ones scrolled out)");
            Assert.AreEqual(22, s.S.handwritingDrawn); Assert.AreEqual(12, s.S.handwritingRight);
            Assert.AreEqual(XgSim.HandwritingWindow, s.HandwritingRecent().Length);
        }

        [Test] public void TheSavedMemoryStaysAtOneHundredAndStillLearns()
        {
            var s = OpenSim(); var h = new Host();
            var writer = new HandwritingWriter(5); var rng = new Random(5);
            for (int i = 0; i < 120; i++) Assert.IsTrue(s.SubmitHandwriting(i % 10, writer.Draw(i % 10, rng), 9, h).accepted);
            Assert.AreEqual(XgMiniActionMemory.Capacity, s.S.miniActionDrawings.Count);
            Assert.AreEqual(XgMiniActionMemory.Capacity, s.HandwritingMemory);
            Assert.AreEqual(120, s.Labels("mnist"), "every drawing still counted as a sample");
            Assert.AreEqual(120, s.S.handwritingDrawn);
        }

        [Test] public void ReadingWhileDrawingChangesNothing()
        {
            var s = OpenSim(); var h = new Host();
            var guess = s.ReadHandwriting(XgHandwriting.Template(2, 0));
            Assert.IsTrue(guess.valid); Assert.AreEqual(2, guess.digit);
            Assert.IsFalse(s.ReadHandwriting(new bool[XgHandwriting.Grid * XgHandwriting.Grid]).valid);
            Assert.AreEqual(1000, h.Money); Assert.AreEqual(0, s.Labels("mnist")); Assert.AreEqual(0, s.HandwritingMemory);
        }

        [Test] public void TheHallsDrawGameAndTheDeskShareOneMemory()
        {
            var s = OpenSim(); var h = new Host();
            var raster = XgHandwriting.Template(6, 1);
            s.LearnHandwriting(6, raster); // what the hall's draw game does on submit
            Assert.AreEqual(1, s.HandwritingMemory);
            Assert.AreEqual(6, s.ReadHandwriting(raster).digit);
            Assert.IsTrue(s.SubmitHandwriting(6, raster, 2, h).hit);
            Assert.AreEqual(2, s.S.miniActionDrawings.Count);
        }

        #if UNITY_5_3_OR_NEWER
        [Test] public void DrawingsAndResultsSurviveASaveRoundTripAndOldSavesLoad()
        {
            var s = OpenSim(); var h = new Host();
            var writer = new HandwritingWriter(3); var rng = new Random(3);
            for (int i = 0; i < 30; i++) s.SubmitHandwriting(i % 10, writer.Draw(i % 10, rng), 3, h);
            double accuracy = s.HandwritingAccuracy(out int count);
            var probe = XgHandwriting.Extract(writer.Draw(4, new Random(77)));
            var expected = s.Handwriting.Recognize(probe);

            var state = UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(s.S));
            var loaded = new XgSim(state);
            Assert.AreEqual(30, loaded.HandwritingMemory);
            Assert.AreEqual(accuracy, loaded.HandwritingAccuracy(out int loadedCount), 1e-9); Assert.AreEqual(count, loadedCount);
            Assert.AreEqual(30, loaded.S.handwritingDrawn);
            var again = loaded.Handwriting.Recognize(probe);
            Assert.AreEqual(expected.digit, again.digit); Assert.AreEqual(expected.confidence, again.confidence, 1e-6);

            // A save from before this feature has none of the fields; null lists and strings must not break anything.
            var old = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>("{}"));
            old.S.handwritingRecent = null; old.S.miniActionDrawings = null;
            Assert.AreEqual(0, old.HandwritingAccuracy(out int oldCount)); Assert.AreEqual(0, oldCount);
            Assert.AreEqual(0, old.HandwritingMemory);
            old.S.desksOpen.Add("mnist");
            Assert.IsTrue(old.SubmitHandwriting(1, XgHandwriting.Template(1, 0), 2, new Host()).accepted);
        }
        #endif
    }
}
