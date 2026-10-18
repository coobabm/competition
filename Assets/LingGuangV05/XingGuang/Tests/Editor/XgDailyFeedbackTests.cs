using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgDailyFeedbackTests
    {
        [Test] public void FreshThresholdEventIsOnceAndNotBackfilledOnLoad()
        {
            var sim=new XgSim();var notices=new List<string>();sim.DailyConditionCompleted+=notices.Add;
            sim.RecordDailyGameCompleted("gomoku","a",true);sim.RecordDailyGameCompleted("gomoku","b",true);
            Assert.IsEmpty(notices);sim.RecordDailyGameCompleted("gomoku","c",true);
            CollectionAssert.AreEqual(new[]{"daily.dice"},notices);
            sim.RecordDailyGameCompleted("gomoku","c",true);sim.RecordDailyGameCompleted("gomoku","d",true);
            Assert.AreEqual(1,notices.Count);
            var restored=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));var after=new List<string>();restored.DailyConditionCompleted+=after.Add;
            Assert.IsTrue(restored.DailyProgress("games3").complete);restored.RecordDailyGameCompleted("gomoku","e",true);
            Assert.IsEmpty(after,"loading or passing an old threshold is not a new discovery");
        }
        [Test] public void UnsentOrDuplicateEvidenceDoesNotNotify()
        {
            var sim=new XgSim();int calls=0;sim.DailyConditionCompleted+=_=>calls++;
            for(int i=0;i<10;i++){sim.RecordDailyGameCompleted("gomoku","local",false);sim.RecordDailySongCompleted("same","play"+i);sim.RecordDailySentMessage("qingwen","sticker"+i,"hello","face");}
            Assert.AreEqual(0,calls);sim.RecordDailySongCompleted("second","two");sim.RecordDailySongCompleted("third","three");
            Assert.AreEqual(1,calls);
        }
        [Test] public void DeliveryAcknowledgementRequiresActualEligibilityAndPersists()
        {
            var sim=new XgSim();sim.S.stage=1;sim.RecordDailyMiaoyuPurchase("t1","card");
            Assert.False(sim.AcknowledgeDailyNotice("daily.bargain"));sim.S.stage=3;
            Assert.True(sim.AcknowledgeDailyNotice("daily.bargain"));Assert.False(sim.AcknowledgeDailyNotice("daily.bargain"));
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Assert.True(loaded.DailyNoticeAcknowledged("daily.bargain"));Assert.False(loaded.AcknowledgeDailyNotice("daily.bargain"));
        }
        [Test] public void EveryDailyCardHasThreeRealLocalizedLines()
        {
            var sim=new XgSim();sim.S.stage=6;sim.S.fullOpen=true;
            foreach(var daily in XgDailyLife.Nodes)
            {
                sim.S.unlocked.Add(daily.id);var node=XgCatalog.Node(daily.id);Assert.True(XgTechTreeCopy.Covered(node));
                foreach(bool english in new[]{false,true})
                {
                    sim.English=english;string card=XgTechTreeCopy.Card(node,sim);var lines=card.Split('\n');
                    Assert.AreEqual(3,lines.Length);Assert.True(lines.All(x=>!string.IsNullOrWhiteSpace(x)));
                    StringAssert.DoesNotContain("daily.",card);StringAssert.DoesNotContain("待核对",card);StringAssert.DoesNotContain("needs review",card);
                    Assert.IsNotEmpty(XgTechTreeCopy.Detail(node,sim));
                }
            }
        }
        static void Level(float[] data,float level){for(int i=0;i<data.Length;i++)data[i]=(i%2==0?level:-level);}
        [Test] public void ActualEnergyRiseRequiredRatherThanSyntheticTempo()
        {
            var gate=new XgMusicOnsetGate();var samples=new float[256];
            Level(samples,.01f);for(int i=0;i<10;i++)Assert.False(gate.Observe(samples,i*.033));
            Level(samples,.06f);Assert.True(gate.Observe(samples,.33));
            Assert.False(gate.Observe(samples,.363),"steady loud audio is not another onset");
            Level(samples,0);for(int i=0;i<20;i++)Assert.False(gate.Observe(samples,.396+i*.033));
            gate.Reset();Level(samples,.06f);Assert.False(gate.Observe(samples,2),"new track needs a baseline");
        }
        [Test] public void InvalidOrDiscontinuousSamplesCannotBeCalledBeats()
        {
            var gate=new XgMusicOnsetGate();var data=new float[64];Level(data,.01f);
            for(int i=0;i<5;i++)gate.Observe(data,i*.033);
            Level(data,.1f);Assert.False(gate.Observe(data,5),"gap resets baseline");
            data[0]=float.NaN;Assert.False(gate.Observe(data,5.033));Assert.False(gate.Observe(null,5.066));
        }
        [Test] public void RiskGateDoesNotInventAnIndependentForecastConsumer()
        {
            var sim=new XgSim();sim.S.unlocked.Add("daily.dice");Assert.False(sim.DailyRiskEnhancementAvailable(false));
            Assert.False(sim.PreciseRiskForecast);
            sim.S.unlocked.Add("train.monitor");Assert.True(sim.PreciseRiskForecast);
            StringAssert.Contains("之后购买训练监控",XgDailyLifeCopy.Detail("daily.dice",false));
            StringAssert.Contains("later buy Training monitor",XgDailyLifeCopy.Detail("daily.dice",true));
        }
        [Test] public void BirthdayQuestionsAndQuotedHypothesesAreNotDisclosures()
        {
            Assert.False(XgDailyLife.DisclosesBirthday("如果我的生日是9月27日，你会记住吗？"));
            Assert.False(XgDailyLife.DisclosesBirthday("我的生日是9月27日吗？"));
            Assert.False(XgDailyLife.DisclosesBirthday("Is it true that my birthday is 27 September?"));
            Assert.False(XgDailyLife.DisclosesBirthday("\"My birthday is September 27\" is an example."));
            Assert.True(XgDailyLife.DisclosesBirthday("我的生日是9月27日。你呢？"));
            Assert.True(XgDailyLife.DisclosesBirthday("My birthday is 27 September."));
        }
    }
}
