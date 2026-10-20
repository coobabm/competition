using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>第一阶段全程引导: stage one after the opening keeps the note to its main line, and each step has its lesson, told once.</summary>
    public sealed class XgStageOneTutorTests
    {
        sealed class Host : IXgHost
        {
            public double money = 5000, compute = 50;
            public double Money => money; public double Compute => compute; public double VramMB => 1e5; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        static XgSim Learned(Host host)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.BeginOpening();
            for (int round = 0; round < 40 && sim.OpeningFocus; round++)
            {
                for (int i = 0; i < 10; i++) { var c = sim.Card(XgMemes.SenseDesk); sim.Answer(XgMemes.SenseDesk, c.truth, host); }
                if (sim.TeachingTreeOpen && !sim.Has("train.curve"))
                    Assert.IsTrue(sim.BuyNode("train.curve", host), "buy the current opening training prerequisite");
                sim.TrainEpoch(XgTrack.Sequence, host);
            }
            Assert.IsFalse(sim.OpeningFocus, "the opening ends");
            Assert.AreEqual(1, sim.S.stage);
            return sim;
        }

        [Test]
        public void OpeningThoughtsWaitForVisibleExeAndThreeRealAnswersAndSurviveSave()
        {
            var sim = new XgSim(); sim.BeginOpening();
            Assert.IsNull(sim.TakeTeachingThought(false));
            Assert.IsFalse(sim.S.teachIntroSaid);
            Assert.IsTrue(sim.TakeTeachingThought(true).HasValue);
            Assert.IsNull(sim.TakeTeachingThought(true));
            var host = new Host();
            for (int i = 0; i < 2; i++)
            {
                var card = sim.Card(XgMemes.SenseDesk);
                sim.Answer(XgMemes.SenseDesk, card.truth, host);
                Assert.IsNull(sim.TakeTeachingThought(true), "No third-question thought before three submitted answers");
            }
            var third = sim.Card(XgMemes.SenseDesk);
            sim.Answer(XgMemes.SenseDesk, third.truth, host);
            Assert.IsNull(sim.TakeTeachingThought(false), "Minimized/closed exe does not narrate");
            StringAssert.Contains("这都什么破题", sim.TakeTeachingThought(true).Value.zh);
            var loaded = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S)));
            Assert.IsNull(loaded.TakeTeachingThought(true), "Saved thoughts do not repeat on reopen/reload");
        }

        [Test]
        public void OlderSaveDoesNotBackfillCompletedThoughts()
        {
            // Raw XgState defaults contain legacy lenet/rnn and migrate to stage three.
            // Use a real stage-one save, predating only the thought-version field.
            var old = new XgSim().S;
            old.teachingFlow = true; old.teachingThoughtVersion = 0;
            old.handCorrect = 12; old.epochs = 1;
            var sim = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(old)));
            Assert.AreEqual(1, sim.S.stage, "Fixture must remain in the teaching stage after load");
            Assert.AreEqual(12, sim.TeachingAnswers);
            Assert.IsNull(sim.TakeTeachingThought(true));
            Assert.IsTrue(sim.S.teachThirdSaid);
            Assert.IsTrue(sim.S.teachTreeSaid);
            Assert.IsTrue(sim.S.teachTrainSaid);
            Assert.IsTrue(sim.S.teachIntroSaid);
            Assert.IsNull(sim.TakeTeachingThought(true));
        }

        [Test]
        public void EveryLessonIsWrittenInBothLanguagesAndNeverSaysTheBannedWord()
        {
            Assert.GreaterOrEqual(XgSim.TutorLessons.Length, 10);
            foreach (var l in XgSim.TutorLessons)
                foreach (var text in new[] { l.innerZh, l.innerEn, l.zhouZh, l.zhouEn, l.whyZh, l.whyEn })
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(text), l.id);
                    StringAssert.DoesNotContain("涌现", text, l.id);
                }
        }

        [Test]
        public void AfterTheOpeningTheNoteKeepsToTheMainLineWithItsLesson()
        {
            var host = new Host();
            var sim = Learned(host);
            sim.AddLine("me", "冰是烫的吗？"); // the opening's chat step done
            var steps = XgGuide.Current(sim, host.money);
            Assert.LessOrEqual(steps.Count, XgGuide.TutorLines);
            foreach (var s in steps)
            {
                Assert.AreNotEqual(XgGuideKind.Side, s.kind, s.id);
                Assert.AreNotEqual(XgGuideKind.Boost, s.kind, s.id);
            }
            var top = steps[0];
            var lesson = sim.TutorFor(top);
            Assert.IsNotNull(lesson, top.id + " " + top.target + " has a lesson");
            StringAssert.StartsWith(lesson.whyZh, top.whyZh, "the hover text leads with it");
            StringAssert.StartsWith(lesson.whyEn, top.whyEn);
        }

        [Test]
        public void EachLessonIsToldOnceAndOnlyInStageOne()
        {
            var host = new Host();
            var sim = Learned(host);
            var step = new XgGuideStep { id = "ability.train" };
            var due = sim.TutorDue(step);
            Assert.AreEqual("grade", due.id);
            sim.MarkTutor(due);
            Assert.IsNull(sim.TutorDue(step), "told once");
            Assert.IsNotNull(sim.TutorFor(step), "still the step's lesson for the hover text");
            Assert.AreEqual("data", sim.TutorFor(new XgGuideStep { id = "ability.study", target = "node:spam.pack" }).id);
            Assert.AreEqual("arch", sim.TutorFor(new XgGuideStep { id = "ability.buy", target = "node:mlp" }).id);
            Assert.AreEqual("capacity", sim.TutorFor(new XgGuideStep { id = "ability.study", target = "node:s.w1" }).id);
            Assert.AreEqual("exam", sim.TutorFor(new XgGuideStep { id = XgGuide.ConfirmStageId }).id);
            Assert.AreEqual("plateau", sim.TutorFor(new XgGuideStep { id = "plateau.diagnose" }).id);
            sim.S.stage = 2;
            Assert.IsNull(sim.TutorFor(step), "stage two has no lessons");
            var fresh = new XgSim { GoldChance = 0 }; fresh.BeginOpening();
            Assert.IsNull(fresh.TutorFor(step), "nor the opening, which has its own note");
        }
    }
}
