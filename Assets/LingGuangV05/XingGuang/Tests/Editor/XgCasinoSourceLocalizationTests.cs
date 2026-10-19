using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgCasinoSourceLocalizationTests
    {
        sealed class Host:IXgHost
        {
            public double Money{get;private set;}=10000;
            public double Compute=>1;public double VramMB=>1024;public string Blocker=>null;
            public void Train(double seconds){}
            public bool Spend(double amount){if(Money<amount)return false;Money-=amount;return true;}
            public void Earn(double amount){Money+=amount;}
        }
        static string Json(XgState state)
        {
#if UNITY_5_3_OR_NEWER
            return JsonUtility.ToJson(state);
#else
            return System.Text.Json.JsonSerializer.Serialize(state,new System.Text.Json.JsonSerializerOptions{IncludeFields=true});
#endif
        }
        static XgState Parse(string json)
        {
#if UNITY_5_3_OR_NEWER
            return JsonUtility.FromJson<XgState>(json);
#else
            return System.Text.Json.JsonSerializer.Deserialize<XgState>(json,new System.Text.Json.JsonSerializerOptions{IncludeFields=true});
#endif
        }
        static XgSim Resolved(string outcome)
        {
            var sim=new XgSim(new XgState{progressionVersion=XgSim.ProgressionSchemaWalls,stage=5,stageVision=5,stageSequence=5}){English=true,Today=20160901};
            sim.S.stage=5;sim.S.casinoScam=1;sim.S.casinoFrozen=431;sim.S.casinoFrozenDay=20160901;
            sim.LearningClock=()=>new XgLearningTime(sim.Today,3600);
            if(outcome==XgSim.CasinoIgnored){sim.Today=20160902;Assert.IsTrue(sim.TickCasinoScam());}
            else Assert.IsTrue(sim.ResolveCasinoScam(outcome==XgSim.CasinoPaid,new Host()));
            // Explicit one-template fixture; actual scam resolution writes the bilingual event, and
            // production discovery writes its persistent source. No fabricated source summary.
            var text=new XgEndingPreparationText{memories=new List<XgEndingMemoryTemplate>{new XgEndingMemoryTemplate
                {id="gamble_888",minStage=1,goodZh="不赌运气。",goodEn="Do not bet on luck.",badZh="再试一把。",badEn="One more bet."}}};
            var flow=new XgEndingPreparationFlow(sim,new object(),()=>true,()=>true,text,s=>XgEndingTextGuard.Filter(s,true));
            Assert.IsTrue(flow.DiscoverMemories(flow.CaptureBinding()).Success);
            return sim;
        }
        static XgEndingMemorySource Source(XgSim sim)=>sim.S.endingPreparation.memories.Single(m=>m.id=="gamble_888").sources.Single();
        static XgAskedEntry Record(XgSim sim)=>sim.S.askedLog.Single(r=>r.sourceKind==XgSim.CasinoSourceKind);
        static void VerifySwitching(XgSim sim)
        {
            var source=Source(sim);var record=Record(sim);string before=Json(sim.S),stored=source.summary;
            foreach(bool english in new[]{true,false,true})
            {
                sim.English=english;
                string expected=XgEndingTextGuard.Filter(english?record.questionEn:record.question,english);
                Assert.AreEqual(expected,XgEndingPresentation.SourceSummary(sim,source,english));
                string evidence=XgEndingPresentation.Evidence(sim,source,english);
                Assert.IsTrue(evidence.Contains(expected));
                if(english)Assert.IsFalse(Regex.IsMatch(evidence,"[\u3400-\u9fff]"),"English authored casino source has no Chinese fragment");
                Assert.AreEqual(stored,source.summary,"language selection never rewrites stored summary");
            }
            Assert.AreEqual(before,Json(sim.S),"read-only projections never mutate wallet, record, memory or receipt");
        }
        [TestCase("paid")][TestCase("refused")][TestCase("ignored")]
        public void ActualCasinoEventRemainsBilingualAcrossLanguageSwitchAndReload(string outcome)
        {
            var sim=Resolved(outcome);VerifySwitching(sim);
            var restored=new XgSim(Parse(Json(sim.S))){Today=sim.Today,English=true};
            Assert.AreEqual(Source(sim).summary,Source(restored).summary);
            VerifySwitching(restored);
            if(outcome==XgSim.CasinoIgnored)Assert.IsFalse(XgEndingPresentation.Evidence(restored,Source(restored),true).Contains("you answered"));
        }
        [Test]
        public void UnresolvedOrNonCasinoSourceKeepsItsExactStoredHistory()
        {
            var sim=Resolved(XgSim.CasinoRefused);var source=Source(sim);var record=Record(sim);string stored=source.summary;
            source.day++;Assert.AreEqual(stored,XgEndingPresentation.SourceSummary(sim,source,true));source.day--;
            string reference=source.refId;source.refId="answer:99999";Assert.AreEqual(stored,XgEndingPresentation.SourceSummary(sim,source,true));source.refId=reference;
            record.bankId="other";Assert.AreEqual(stored,XgEndingPresentation.SourceSummary(sim,source,true));record.bankId="casino.888";
            record.sourceKind="book";Assert.AreEqual(stored,XgEndingPresentation.SourceSummary(sim,source,true));
            Assert.AreEqual("",XgEndingPresentation.SourceSummary(sim,null,true));
        }
        [Test]
        public void OldCasinoRecordWithoutEnglishFallsBackWithoutInventingTranslation()
        {
            var sim=Resolved(XgSim.CasinoPaid);var source=Source(sim);Record(sim).questionEn="";string before=Json(sim.S);
            Assert.AreEqual(source.summary,XgEndingPresentation.SourceSummary(sim,source,true));Assert.AreEqual(before,Json(sim.S));
        }
        [Test]
        public void RejectedSavedTranslationCannotBypassDynamicTextGuard()
        {
            var sim=Resolved(XgSim.CasinoPaid);var source=Source(sim);Record(sim).questionEn="Contact player@example.com";
            Assert.AreEqual(source.summary,XgEndingPresentation.SourceSummary(sim,source,true));
        }
    }
}
