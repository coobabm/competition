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
                if (i == 0) Assert.AreEqual("我去，答题还能赚钱？", sim.TakeTeachingThought(true).Value.zh);
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
        public void MoneyReactionsUseSettledAmountsQueueInOrderAndPersistOnce()
        {
            var sim = new XgSim { GoldChance = 0 }; sim.BeginOpening();
            var host = new Host { money = 0 };
            Assert.IsTrue(sim.TakeTeachingThought(true).HasValue); // first-open line owns its turn
            var card = sim.Card(XgMemes.SenseDesk);
            var unpaid = sim.Answer(XgMemes.SenseDesk, !card.truth, host);
            Assert.AreEqual(0, unpaid.fine);
            Assert.IsFalse(sim.S.answerFineThoughtRecorded, "A wrong answer without an actual deduction must not claim money was lost");
            card = sim.Card(XgMemes.SenseDesk);
            var earned = sim.Answer(XgMemes.SenseDesk, card.truth, host);
            Assert.Greater(earned.pay, 0);
            card = sim.Card(XgMemes.SenseDesk);
            var charged = sim.Answer(XgMemes.SenseDesk, !card.truth, host);
            Assert.Greater(charged.fine, 0);
            Assert.IsNull(sim.TakeTeachingThought(false), "Wait for a visible window without losing queued reactions");
            var loaded = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S)));
            Assert.AreEqual("我去，答题还能赚钱？", loaded.TakeTeachingThought(true).Value.zh);
            Assert.AreEqual("我去，答错还会扣钱？", loaded.TakeTeachingThought(true).Value.zh);
            StringAssert.Contains("这都什么破题", loaded.TakeTeachingThought(true).Value.zh);
            host.money = 100;
            card = loaded.Card(XgMemes.SenseDesk); loaded.Answer(XgMemes.SenseDesk, card.truth, host);
            card = loaded.Card(XgMemes.SenseDesk); loaded.Answer(XgMemes.SenseDesk, !card.truth, host);
            var reopened = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(loaded.S)));
            Assert.IsNull(reopened.TakeTeachingThought(true), "Neither money reaction repeats after another answer or reload");
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
        public void MoneyThoughtReceiptsAreWrittenBeforePresentationAndDiskReloadDoesNotReplayThem()
        {
            string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"LingGuang-thought-"+System.Guid.NewGuid().ToString("N")+".json");
            try
            {
                var sim=new XgSim{GoldChance=0};var host=new Host();
                sim.SettleMiniAction("arith",true,host,automatic:true);
                sim.SettleMiniAction("arith",false,host,automatic:true);
                System.IO.File.WriteAllText(path,UnityEngine.JsonUtility.ToJson(sim.S));
                sim=new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(System.IO.File.ReadAllText(path)));
                Assert.IsFalse(sim.S.answerPayThoughtRecorded||sim.S.answerFineThoughtRecorded,
                    "automatic answers followed by an actual disk load must not consume manual firsts");
                sim.SettleMiniAction("arith",true,host);
                sim.SettleMiniAction("arith",false,host);
                CollectionAssert.AreEqual(new[]{"pay","fine"},sim.S.pendingAnswerMoneyThoughts);
                Assert.IsNull(sim.TakeTeachingThought(true,()=>false),"failed persistence must not hand a line to the voice player");
                Assert.IsFalse(sim.S.answerPayThoughtConsumed);
                CollectionAssert.AreEqual(new[]{"pay","fine"},sim.S.pendingAnswerMoneyThoughts,"failed write keeps the same ordered pending events");
                foreach(string kind in new[]{"pay","fine"})
                {
                    var line=sim.TakeTeachingThought(true,()=>{System.IO.File.WriteAllText(path,UnityEngine.JsonUtility.ToJson(sim.S));return true;});
                    Assert.IsTrue(line.HasValue);
                    StringAssert.Contains(kind=="pay"?"赚钱":"扣钱",line.Value.zh);
                    sim=new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(System.IO.File.ReadAllText(path)));
                    Assert.IsTrue(kind=="pay"?sim.S.answerPayThoughtConsumed:sim.S.answerFineThoughtConsumed);
                }
                sim.SettleMiniAction("arith",true,host);sim.SettleMiniAction("arith",false,host);
                Assert.IsNull(sim.TakeTeachingThought(true));
                Assert.IsEmpty(sim.S.pendingAnswerMoneyThoughts);
                var fresh=new XgSim();fresh.SettleMiniAction("arith",true,host);
                Assert.IsTrue(fresh.TakeTeachingThought(true).HasValue,"a separate new game keeps its own first income line");
            }
            finally { if(System.IO.File.Exists(path))System.IO.File.Delete(path); }
        }

        [Test] public void ConsumedReceiptsWinOverStalePendingDuplicatesWithoutDroppingUnheardEvents()
        {
            var state=new XgSim().S;
            state.answerPayThoughtConsumed=true;
            state.pendingAnswerMoneyThoughts.AddRange(new[]{"pay","pay","fine","fine","invalid"});
            var sim=new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(state)));
            CollectionAssert.AreEqual(new[]{"fine"},sim.S.pendingAnswerMoneyThoughts);
            StringAssert.Contains("扣钱",sim.TakeTeachingThought(true).Value.zh);
            Assert.IsNull(sim.TakeTeachingThought(true));
            var legacy=new XgSim().S;legacy.answerPayThoughtRecorded=true;
            var loaded=new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(legacy)));
            Assert.IsTrue(loaded.S.answerPayThoughtConsumed,"old recorded-and-drained receipts stay consumed");
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
        public void ProgressExplanationKeepsTheLessonForItsRecommendedAction()
        {
            var sim = Learned(new Host());
            var samples = new XgGuideStep { id = XgGuide.SampleLessonId, target = "node:spam.pack", tab = "tree" };
            Assert.AreEqual("data", sim.TutorDue(samples).id);
            sim.MarkTutor(sim.TutorDue(samples));
            Assert.IsNull(sim.TutorDue(samples), "the wrapper shares the original lesson's once-only receipt");
            Assert.AreEqual("arch", sim.TutorFor(new XgGuideStep { id = XgGuide.ParameterLessonId, target = "node:mlp", tab = "tree" }).id);
            Assert.AreEqual("grade", sim.TutorFor(new XgGuideStep { id = XgGuide.ParameterLessonId, target = "label:训练一轮|Train 1 epoch", tab = "train" }).id);
            Assert.AreEqual("label", sim.TutorFor(new XgGuideStep { id = XgGuide.SampleLessonId, tab = "teach" }).id);
            Assert.AreEqual("more", sim.TutorFor(new XgGuideStep { id = XgGuide.SampleLessonId, tab = "train" }).id);
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
