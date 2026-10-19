using NUnit.Framework;
using UnityEngine;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgTrainingLessonTests
    {
        sealed class Host : IXgHost
        {
            public double Money {get; private set;}=1000;
            public string blocker; public double energy;
            public double Compute=>50; public double VramMB=>100000; public string Blocker=>blocker;
            public void Earn(double x){Money+=x;} public bool Spend(double x){if(Money<x)return false;Money-=x;return true;} public void Train(double x){energy+=x;}
        }
        [Test] public void ActualQueuedAnswerGrantsOneRoundBonusAndSurvivesReload()
        {
            var sim=new XgSim();var host=new Host();sim.InitializeTrainingLessons();
            var q=sim.PendingTrainingQuiz();Assert.NotNull(q);
            double before=host.Money;
            Assert.IsFalse(sim.AnswerTrainingQuiz(q.id,-1,host,out _));
            Assert.IsTrue(sim.AnswerTrainingQuiz(q.id,q.answer,host,out bool right));Assert.IsTrue(right);
            Assert.AreEqual(before,host.Money);Assert.IsTrue(sim.TrainingQuizBonusAvailable);Assert.IsFalse(sim.AnswerTrainingQuiz(q.id,q.answer,host,out _));
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));loaded.InitializeTrainingLessons();
            Assert.IsTrue(loaded.TrainingAtlasLit("quiz."+q.id));Assert.IsFalse(loaded.AnswerTrainingQuiz(q.id,q.answer,host,out _));
            Assert.AreEqual(before,host.Money);Assert.IsTrue(loaded.TrainingQuizBonusAvailable);
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
            Assert.IsFalse(sim.TrainingQuizBonusHeld);
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
        static XgSim Ready()
        {
            var sim=new XgSim();
            sim.S.desksOpen.Add("mnist");
            sim.S.labels.Add(new XgLabelCount{dataset="mnist",count=20});
            sim.S.labels.Add(new XgLabelCount{dataset="spam",count=20});
            return sim;
        }
        static void AnswerRight(XgSim sim,Host host)
        {
            var q=sim.PendingTrainingQuiz();Assert.NotNull(q);
            Assert.IsTrue(sim.AnswerTrainingQuiz(q.id,q.answer,host,out bool right));Assert.IsTrue(right);
        }
        [Test] public void BonusSurvivesBlockedStartAndReloadOfReservedRound()
        {
            var sim=Ready();var host=new Host();AnswerRight(sim,host);
            host.blocker="Power cut";Assert.IsFalse(sim.BeginEpoch(XgTrack.Vision,host));
            Assert.IsTrue(sim.TrainingQuizBonusAvailable);
            host.blocker=null;Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision,host));
            Assert.IsFalse(sim.TrainingQuizBonusAvailable);Assert.IsTrue(sim.S.vision.trainingQuizBonusReserved);
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Assert.IsFalse(loaded.AnyEpochActive);Assert.IsTrue(loaded.TrainingQuizBonusAvailable);
            Assert.IsFalse(loaded.S.vision.trainingQuizBonusReserved);Assert.AreEqual(0,loaded.S.epochs);
        }
        [Test] public void TwoTracksAndMoreCorrectAnswersCannotStackBonus()
        {
            var sim=Ready();var host=new Host();AnswerRight(sim,host);
            sim.NoteTrainingPhenomenon("plateau");AnswerRight(sim,host);
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision,host));
            sim.NoteTrainingPhenomenon("gap");AnswerRight(sim,host);
            Assert.IsFalse(sim.TrainingQuizBonusAvailable,"a reserved reward also blocks stacking");
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Sequence,host));
            Assert.IsTrue(sim.S.vision.trainingQuizBonusReserved);Assert.IsFalse(sim.S.sequence.trainingQuizBonusReserved);
        }
        [Test] public void AnswerDuringRoundWaitsForNextRound()
        {
            var sim=Ready();var host=new Host();Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision,host));
            AnswerRight(sim,host);Assert.IsFalse(sim.S.vision.trainingQuizBonusReserved);
            for(int i=0;i<2000&&sim.S.vision.epochActive;i++)sim.Tick(.05,host);
            Assert.AreEqual(1,sim.S.epochs);Assert.IsTrue(sim.TrainingQuizBonusAvailable);
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision,host));Assert.IsTrue(sim.S.vision.trainingQuizBonusReserved);
        }
        [Test] public void BonusImprovesOneSettlementWithoutExtraElectricityOrMoney()
        {
            var boosted=Ready();var host=new Host();AnswerRight(boosted,host);
            var baseline=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(boosted.S)));
            baseline.S.trainingQuizBonusPending=false;var plainHost=new Host();
            var plain=baseline.TrainEpoch(XgTrack.Vision,plainHost);
            var extra=boosted.TrainEpoch(XgTrack.Vision,host);
            Assert.NotNull(plain);Assert.NotNull(extra);
            Assert.AreEqual(System.Math.Round(plain.steps*1.05),extra.steps);
            Assert.AreEqual(baseline.S.vision.lastRate*1.05,boosted.S.vision.lastRate,1e-10);
            Assert.AreEqual(plainHost.energy,host.energy);Assert.AreEqual(plainHost.Money,host.Money);
            Assert.IsFalse(boosted.TrainingQuizBonusHeld);
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(boosted.S)));
            Assert.IsFalse(loaded.TrainingQuizBonusHeld);
            var second=boosted.TrainEpoch(XgTrack.Vision,host);Assert.AreEqual(plain.steps,second.steps);
        }
        [Test] public void OfflineSettlementDoesNotConsumeQuizReward()
        {
            var sim=Ready();var host=new Host();AnswerRight(sim,host);sim.OfflineSimulation=true;
            Assert.NotNull(sim.TrainEpoch(XgTrack.Vision,host));Assert.IsTrue(sim.TrainingQuizBonusAvailable);
            sim.OfflineSimulation=false;Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision,host));
            sim.OfflineSimulation=true;
            for(int i=0;i<2000&&sim.S.vision.epochActive;i++)sim.Tick(.05,host);
            Assert.IsFalse(sim.S.vision.epochActive);Assert.IsTrue(sim.TrainingQuizBonusAvailable);
            Assert.IsFalse(sim.S.vision.trainingQuizBonusReserved);
        }
    }
}
