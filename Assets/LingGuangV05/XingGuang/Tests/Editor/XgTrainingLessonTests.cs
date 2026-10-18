using NUnit.Framework;
using UnityEngine;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgTrainingLessonTests
    {
        sealed class Host : IXgHost
        {
            public double Money {get; private set;}=1000;
            public double Compute=>50; public double VramMB=>100000; public string Blocker=>null;
            public void Earn(double x){Money+=x;} public bool Spend(double x){if(Money<x)return false;Money-=x;return true;} public void Train(double x){}
        }
        [Test] public void ActualQueuedAnswerPaysOnceAndSurvivesReload()
        {
            var sim=new XgSim();var host=new Host();sim.InitializeTrainingLessons();
            var q=sim.PendingTrainingQuiz();Assert.NotNull(q);
            double before=host.Money;
            Assert.IsFalse(sim.AnswerTrainingQuiz(q.id,-1,host,out _));
            Assert.IsTrue(sim.AnswerTrainingQuiz(q.id,q.answer,host,out bool right));Assert.IsTrue(right);
            Assert.AreEqual(before+20,host.Money);Assert.IsFalse(sim.AnswerTrainingQuiz(q.id,q.answer,host,out _));
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));loaded.InitializeTrainingLessons();
            Assert.IsTrue(loaded.TrainingAtlasLit("quiz."+q.id));Assert.IsFalse(loaded.AnswerTrainingQuiz(q.id,q.answer,host,out _));
            Assert.AreEqual(before+20,host.Money);
        }
        [Test] public void OldSaveNullListsAreRepairedWithoutLosingTrainingProgress()
        {
            var sim=new XgSim();sim.S.epochs=13;sim.S.lessonPending=null;sim.S.lessonCollected=null;sim.S.lessonDiscovered=null;sim.S.quizQueued=null;sim.S.quizAnswered=null;
            sim.InitializeTrainingLessons();Assert.AreEqual(13,sim.S.epochs);Assert.NotNull(sim.PendingTrainingQuiz());
            sim.CollectTrainingLesson("nan");Assert.IsFalse(sim.TrainingAtlasLit("lesson.nan"),"cannot collect a phenomenon never seen");
        }
        [Test] public void RepeatedStageTriggersNeverQueueDuplicates()
        {
            var sim=new XgSim();sim.InitializeTrainingLessons();int n=sim.S.quizQueued.Count;
            sim.NoteTrainingPhenomenon("stage_enter");sim.InitializeTrainingLessons();Assert.AreEqual(n,sim.S.quizQueued.Count);
            foreach(var id in sim.S.quizQueued) Assert.AreEqual(sim.S.stage,XgTrainingLessons.Quiz(id).stage);
        }
        [Test] public void PlateauRejectsDeclineInvalidAndShortHistory()
        {
            Assert.IsFalse(XgSim.TrainingHistoryFlat(null));
            Assert.IsFalse(XgSim.TrainingHistoryFlat(new float[]{.8f,.7f,.6f,.5f,.4f,.3f}));
            Assert.IsFalse(XgSim.TrainingHistoryFlat(new float[]{.8f,.7999f,.7998f,.7997f,.7996f,.7995f}));
            Assert.IsFalse(XgSim.TrainingHistoryFlat(new float[]{.1f,.2f,.3f,.4f,.5f,.6f}));
            Assert.IsFalse(XgSim.TrainingHistoryFlat(new float[]{.5f,.5f,float.NaN,.5f,.5f,.5f}));
            Assert.IsTrue(XgSim.TrainingHistoryFlat(new float[]{.5f,.5f,.5f,.5f,.5f,.5f}));
            Assert.IsTrue(XgSim.TrainingHistoryFlat(new float[]{.5f,.5001f,.4999f,.5f,.5001f,.5f}));
        }
        [Test] public void StageFourObserverAcceptsNullHistoryWithoutFalseDeepFailure()
        {
            var sim=new XgSim();sim.S.stage=4;sim.InitializeTrainingLessons();
            var run=sim.Run(XgTrack.Vision);run.depth=8;run.histVal=null;
            var observe=typeof(XgSim).GetMethod("ObserveTrainingEpoch",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Assert.NotNull(observe);
            Assert.DoesNotThrow(()=>observe.Invoke(sim,new object[]{new XgEpoch{track=0}}));
            Assert.IsFalse(sim.S.lessonDiscovered.Contains("ceiling"));
            foreach(var id in sim.S.quizQueued)Assert.AreNotEqual("deep_worse",XgTrainingLessons.Quiz(id).trigger);
        }
        [Test] public void MissingHostCannotConsumeAnswerOrInventIncome()
        {
            var sim=new XgSim();var q=sim.PendingTrainingQuiz();double income=sim.S.totalIncome;
            Assert.IsFalse(sim.AnswerTrainingQuiz(q.id,q.answer,null,out _));
            Assert.AreEqual(income,sim.S.totalIncome);Assert.IsFalse(sim.S.quizAnswered.Contains(q.id));
            Assert.IsTrue(sim.S.quizQueued.Contains(q.id));
        }
        [Test] public void WrongAnswerHasNoRewardAndCannotBeRepeated()
        {
            var sim=new XgSim();var host=new Host();var q=sim.PendingTrainingQuiz();double money=host.Money;
            Assert.IsTrue(sim.AnswerTrainingQuiz(q.id,(q.answer+1)%q.options.Length,host,out bool right));Assert.IsFalse(right);
            Assert.IsFalse(sim.AnswerTrainingQuiz(q.id,q.answer,host,out _));Assert.AreEqual(money,host.Money);
        }
        [Test] public void StalePendingItemsCannotHideUncollectedCards()
        {
            var sim=new XgSim();sim.S.lessonCollected.Add("nan");
            sim.S.lessonPending.AddRange(new[]{"unknown-future-card","nan","overfit"});
            sim.InitializeTrainingLessons();CollectionAssert.AreEqual(new[]{"unknown-future-card","overfit"},sim.S.lessonPending);
            Assert.IsTrue(sim.S.lessonDiscovered.Contains("nan"));Assert.IsTrue(sim.TrainingAtlasLit("lesson.nan"));
        }
        [Test] public void ModalBlocksStartingTrainingWithoutConsumingMoney()
        {
            var sim=new XgSim();var host=new Host();sim.TrainingLessonModalOpen=true;double before=host.Money;
            Assert.IsFalse(sim.BeginEpoch(XgTrack.Sequence,host));Assert.AreEqual(before,host.Money);Assert.IsFalse(sim.S.sequence.epochActive);
        }
    }
}
