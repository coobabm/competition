using System;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgRiskKnownDatesTests
    {
        static XgSim Fresh()
        {var sim=new XgSim();var run=sim.Run(XgTrack.Vision);run.dataset="mnist";run.valAcc=.8;run.autoLr=false;run.rateNotch=0;return sim;}
        [Test] public void BothPurchasesAreNeededForPrecisionButNotForQuestions()
        {
            var s=Fresh();Assert.IsFalse(s.PreciseRiskForecast);s.S.unlocked.Add("daily.dice");
            Assert.IsTrue(s.DailyProbabilityQuestions);Assert.IsFalse(s.PreciseRiskForecast);
            s.S.unlocked.Add("train.monitor");Assert.IsTrue(s.PreciseRiskForecast);
            s.S.unlocked.Remove("daily.dice");Assert.IsFalse(s.PreciseRiskForecast);
        }
        [Test] public void SlowerRatesMatchActualSteadyMultiplierAndSafestHasNoDrop()
        {
            var s=Fresh();var run=s.Run(XgTrack.Vision);var normal=s.TrainingRiskForecast(run);
            run.rateNotch=-1;var slower=s.TrainingRiskForecast(run);Assert.AreEqual(normal.scoreDrop*.4,slower.scoreDrop,1e-12);
            run.rateNotch=-2;var safest=s.TrainingRiskForecast(run);Assert.AreEqual(0,safest.scoreDrop);Assert.AreEqual(0,safest.tear);
        }
        [Test] public void BlowupAndOrdinaryDropAreExclusivePaths()
        {
            var s=Fresh();var run=s.Run(XgTrack.Vision);run.rateNotch=1;var f=s.TrainingRiskForecast(run);
            Assert.AreEqual(.35,f.tear,1e-12);Assert.AreEqual(.65*f.ordinaryRoll,f.ordinaryDrop,1e-12);
            Assert.AreEqual(.35+f.ordinaryDrop,f.scoreDrop,1e-12);
            Assert.Less(f.scoreDrop,f.tear+f.ordinaryRoll);
        }
        [Test] public void ScoreFloorAndChargedGuardPreventLossButNotBlowup()
        {
            var s=Fresh();var run=s.Run(XgTrack.Vision);run.rateNotch=3;run.valAcc=1-XgCatalog.Dataset(run.dataset).chanceError;
            var floor=s.TrainingRiskForecast(run);Assert.IsTrue(floor.atFloor);Assert.AreEqual(0,floor.scoreDrop);Assert.Greater(floor.tear,0);
            run.valAcc=.8;Assert.IsTrue(s.GrantRelic("resnet_sticker"));s.S.relicCharge=8;
            var guarded=s.TrainingRiskForecast(run);Assert.IsTrue(guarded.guarded);Assert.AreEqual(0,guarded.scoreDrop);Assert.Greater(guarded.tear,0);
        }
        [Test] public void ForecastProjectsOneRoundMemeRecoveryWithoutChangingSaveOrRng()
        {
            var s=Fresh();var run=s.Run(XgTrack.Vision);run.dataset="spam";s.S.mkDrift.Add(new XgMemeDrift{dataset="spam",points=1});
            string before=JsonUtility.ToJson(s.S);var f=s.TrainingRiskForecast(run);Assert.AreEqual(before,JsonUtility.ToJson(s.S));
            s.S.mkDrift.Clear();Assert.AreEqual(s.TrainingRiskForecast(run).ordinaryRoll,f.ordinaryRoll,1e-12);
        }
        [Test] public void UnknownOrNonfiniteRunHasNoNumericForecast()
        {var s=Fresh();Assert.IsFalse(s.TrainingRiskForecast(null).available);var run=s.Run(XgTrack.Vision);run.valAcc=double.NaN;Assert.IsFalse(s.TrainingRiskForecast(run).available);}
        [Test] public void AnniversaryOnlyAcceptsAffirmativeCanonicalDisclosure()
        {
            Assert.IsTrue(XgDailyKnownDates.DisclosesAnniversary("我们的纪念日是1月1日。"));
            Assert.IsTrue(XgDailyKnownDates.DisclosesAnniversary("Our anniversary is 1 January."));
            foreach(var text in new[]{"我们的纪念日是12月31日。","如果我们的纪念日是1月1日，你会记得吗？","我们的纪念日是1月1日吗？","\"Our anniversary is 1 January\" is an example.","Our anniversary is not 1 January.","我同学说我们的纪念日是1月1日。"})
                Assert.IsFalse(XgDailyKnownDates.DisclosesAnniversary(text),text);
        }
        [Test] public void AnniversaryReminderCrossesTheYearWithoutInventingAStartYear()
        {
            Assert.AreEqual(7,XgDailyKnownDates.DaysUntil(new DateTime(2016,12,25),1,1));
            Assert.AreEqual(0,XgDailyKnownDates.DaysUntil(new DateTime(2017,1,1),1,1));
            Assert.Greater(XgDailyKnownDates.DaysUntil(new DateTime(2017,1,2),1,1),7);
        }
        [Test] public void AnniversaryReferenceIsBackedByTheActualAuthoredResource()
        {
            var source=Resources.Load<TextAsset>("LingGuang/Rag/qingwen");Assert.NotNull(source);
            StringAssert.Contains("\"id\": \"us-anniversary\"",source.text);
            StringAssert.Contains("你坚持纪念日是1月1日，他非说是12月31日",source.text);
            Assert.AreEqual("LingGuang/Rag/qingwen#us-anniversary",XgDailyKnownDates.AnniversarySource);
        }
    }
}
