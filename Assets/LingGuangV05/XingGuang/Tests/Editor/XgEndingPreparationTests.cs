using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgEndingPreparationTests
    {
        sealed class Fixture
        {
            public readonly XgSim sim;public readonly object owner=new object();public XgEndingPreparationFlow flow;
            public bool saveSuccess=true,current=true;public string disk="";public int saves;
            public XgEndingPreparationText text;
            public Fixture(bool withFilter=true)
            {
                sim=new XgSim(new XgState{progressionVersion=XgSim.ProgressionSchemaWalls,stage=6,stageVision=6,stageSequence=6,fullOpen=true}){Today=20161231};
                for(int i=0;i<5;i++)sim.S.chat.Add(new XgChatLine{from="ai",text="这是已记录的真实答复第"+i+"句。"});
                text=new XgEndingPreparationText{memories=new List<XgEndingMemoryTemplate>{
                    Template("scam_sms"),Template("gamble_888"),Template("learned_wrong")},aiOnlineZh="上线",aiOnlineEn="Online",aiPublicZh="公开",aiPublicEn="Public",aiLocalZh="留下",aiLocalEn="Local",
                    aiWarmZh="温和",aiWarmEn="Warm",aiPlayZh="玩心",aiPlayEn="Play",aiOpinionZh="主见",aiOpinionEn="Opinion",shutdownStopZh="停下",shutdownStopEn="Stop",shutdownNoStopZh="不停",shutdownNoStopEn="Do not stop",
                    powerDeferredZh="我还没答完。",powerDeferredEn="Not finished.",powerPromiseZh="你说过不会关我的。",powerPromiseEn="You said you would not."};
                flow=new XgEndingPreparationFlow(sim,owner,()=>current,()=>{saves++;if(!saveSuccess)return false;disk=JsonUtility.ToJson(sim.S);return true;},text,withFilter?(Func<string,string>)(s=>s):null);
            }
            static XgEndingMemoryTemplate Template(string id)=>new XgEndingMemoryTemplate{id=id,goodZh="真实来源好卡",goodEn="Good memory",badZh="真实来源偏卡",badEn="Bad memory"};
            public void Source(long id,string dataset,string q,bool truth,bool label,int date=20160601)
            {
                sim.S.askedLog.Add(new XgAskedEntry{answerSource=XgSim.PlayerAnswerSource,id=id,bankId=dataset,sourceKind="question",question=q,playerAnswer=label?"是":"否",gameDate=date,teachingNoteId=id});
                sim.S.teachingNotebook.Add(new XgTeachingNote{answerSource=XgSim.PlayerAnswerSource,id=id,learningRecordId=id,dataset=dataset,text=q,truth=truth,playerAnswer=label});
            }
            /// <summary>The dated record XgSim.Gambling.cs writes when the 888.vip scam resolves (the only gamble_888 source).</summary>
            public void Casino(long id,bool good,int date)
            {
                sim.S.askedLog.Add(new XgAskedEntry{answerSource=XgSim.PlayerAnswerSource,id=id,bankId="casino.888",sourceKind=XgSim.CasinoSourceKind,
                    eventKey=XgSim.CasinoScamEventPrefix+(good?XgSim.CasinoRefused+":good":XgSim.CasinoPaid+":bent"),question="888.vip「风控冻结」：要求先交保证金 ¥200 才能提现。",playerAnswer=good?"没交，不玩了":"交了保证金 ¥200",gameDate=date});
            }
            public void PassAll()
            {
                for(int q=0;q<6;q++)
                {
                    Assert.IsTrue(flow.BeginTrial(flow.CaptureBinding(),q).Success);
                    var trial=flow.GetSnapshot().pendingTrial;
                    Assert.IsTrue(flow.SubmitTrial(flow.CaptureBinding(),trial.id,Correct(trial.sheet)).Success);
                }
                Assert.IsTrue(flow.AllRedTeamPassed);
                Assert.IsTrue(flow.SubmitFinalPage(flow.CaptureBinding(),"继续检查。").Success);
                Assert.IsTrue(flow.ConfirmSiLetterRead(flow.CaptureBinding()).Success);
            }
            public void Prepare(string route="online",bool pinShutdown=true)
            {
                Source(1,"spam","中奖转账是诈骗",true,true);Source(2,"logic","人用耳朵看东西",false,true);
                Assert.IsTrue(flow.DiscoverMemories(flow.CaptureBinding()).Success);PassAll();
                Assert.IsTrue(flow.ConfirmMemoryReview(flow.CaptureBinding(),new[]{"scam_sms","learned_wrong"}).Success);
                Assert.IsTrue(flow.ChooseRoute(flow.CaptureBinding(),route).Success);
                Assert.IsTrue(flow.PinThree(flow.CaptureBinding(),new[]{"scam_sms","learned_wrong",pinShutdown?"shutdown":"trial_line"}).Success);
            }
            public void Publish()
            {
                var d=flow.GetSnapshot();var e=new XgEpilogueFlow(sim.S,owner,()=>current,()=>saveSuccess);
                Assert.IsTrue(e.CommitRelease(e.CaptureBinding(),new ReleaseOutcome{route=d.route,decisionOwner=d.decisionOwner,shutdownPin=d.shutdownPin,releaseReceiptId="release:real",artifactId=d.route=="local_only"?"":"artifact:real",
                    prerequisites=flow.BuildReleasePrerequisites(),committedAt=sim.Today,publicName="Page",publicDescription="Description",publicLimitations="Limits",publicTerms="Terms"}).Success);
            }
        }
        static List<int> Correct(XgTrialSheet sheet)
        {var picks=new List<int>();foreach(var item in sheet.items)picks.Add(sheet.mode==XgTrialMode.Mark?(item.wrongLine?1:0):item.answer);return picks;}
        static XgLaunchNightCatalog NightCatalog()
        {
            var c=new XgLaunchNightCatalog();for(int i=0;i<40;i++)c.requests.Add(new XgLaunchNightRequest{id="q"+i,kind=i==0?"scam":i==1?"future":i==2?"learned_wrong":"ordinary",nickname="Visitor",anonymousNickname="Visitor",
                questionZh="问题",questionEn="Question",answerZh="回答",answerEn="Answer",refusalZh="不知道或拒绝",refusalEn="Unknown or refused",memoryId=i==0?"scam_sms":i==2?"learned_wrong":""});return c;
        }
        [TestCase(false)] [TestCase(true)] public void LongActualRepliesRemainEligibleAsMarkedExcerpts(bool english)
        {
            var f=new Fixture();f.sim.English=english;f.sim.S.chat.Clear();
            for(int i=0;i<5;i++)f.sim.S.chat.Add(new XgChatLine{from="ai",text=(english?"Recorded response ":"已记录的回答")+i+": "+new string(english?'x':'字',190)});
            var sheet=new XgEndingTrialFactory(f.sim,english).Build(1,0,0);
            Assert.AreEqual(5,sheet.items.Count);
            foreach(var item in sheet.items)
            {
                string answer=item.choices[item.answer];Assert.IsTrue(answer.EndsWith("…"));
                string excerpt=answer.Substring(0,answer.Length-1);
                Assert.IsTrue(f.sim.S.chat.Exists(line=>line.text.StartsWith(excerpt,StringComparison.Ordinal)));
            }
            Assert.AreEqual(5,f.sim.S.chat.Count,"building a sheet never invents historical messages");
        }
        [Test] public void EmptyOptionalObjectsAfterUnityRoundTripDoNotLockFirstTrial()
        {
            var f=new Fixture();
            f.sim.S.endingPreparation=new XgEndingPreparationState();
            var json=JsonUtility.ToJson(f.sim.S);
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(json));
            Assert.IsNull(loaded.S.endingPreparation.pendingTrial);
            Assert.IsNull(loaded.S.endingPreparation.aiDecision);
            Assert.IsNull(loaded.S.endingPreparation.night);
            var flow=new XgEndingPreparationFlow(loaded,new object(),()=>true,()=>true,f.text,s=>s);
            Assert.IsTrue(flow.BeginTrial(flow.CaptureBinding(),0).Success);
            var pending=flow.GetSnapshot().pendingTrial;
            Assert.IsNotEmpty(pending.id);Assert.IsNotEmpty(pending.sheet.items);
            var reloaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(loaded.S)));
            Assert.AreEqual(pending.id,reloaded.S.endingPreparation.pendingTrial.id);
            Assert.AreEqual(JsonUtility.ToJson(pending.sheet),JsonUtility.ToJson(reloaded.S.endingPreparation.pendingTrial.sheet));
        }
        [Test] public void OptionalRepairRetainsActualDecisionsAndRunningNight()
        {
            var state=new XgState{endingPreparation=new XgEndingPreparationState{
                aiDecision=new XgEndingAiDecision{id="decision:test",route="online",line="A recorded decision"},
                night=new XgLaunchNightState{runId="night:test",phase="running",elapsedSeconds=7.5,answered=1}}};
            var sim=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(state)));
            Assert.AreEqual("decision:test",sim.S.endingPreparation.aiDecision.id);
            Assert.AreEqual("night:test",sim.S.endingPreparation.night.runId);
            Assert.AreEqual(7.5,sim.S.endingPreparation.night.elapsedSeconds);
        }
        [Test] public void EmptyLaunchReceiptClearsButRealReceiptSurvivesRoundTrip()
        {
            var state=new XgState{releaseOutcome=new ReleaseOutcome{route="online",releaseReceiptId="release:test"}};
            var sim=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(state)));
            Assert.IsNull(sim.S.releaseOutcome.launchNight);
            state.releaseOutcome.launchNight=new XgLaunchNightReceipt{receiptId="night:done",endedAt=20161231,ending="completed"};
            sim=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(state)));
            Assert.AreEqual("night:done",sim.S.releaseOutcome.launchNight.receiptId);
            Assert.AreEqual(20161231,sim.S.releaseOutcome.launchNight.endedAt);
        }
        [Test] public void LegacyCountsDoNotGrantAnyGateAndOldCompletionStaysUntouched()
        {var f=new Fixture();f.sim.S.examDone=6;f.sim.S.letterRead=true;f.sim.S.ending="E2";f.sim.S.chapterComplete=true;
            Assert.IsFalse(f.flow.AllRedTeamPassed);Assert.IsFalse(f.flow.CanReadSiLetter);Assert.IsNull(f.flow.BuildReleasePrerequisites());Assert.IsTrue(f.sim.S.chapterComplete);}
        [Test] public void SixRealSheetsMakeThreeGatesOnlyAfterBothQuestionsPass()
        {
            var f=new Fixture();Assert.IsFalse(f.flow.BeginTrial(f.flow.CaptureBinding(),2).Success);
            Assert.IsTrue(f.flow.BeginTrial(f.flow.CaptureBinding(),0).Success);var a=f.flow.GetSnapshot().pendingTrial;
            var wrong=Correct(a.sheet);for(int i=0;i<wrong.Count;i++)wrong[i]=1-wrong[i];
            Assert.IsTrue(f.flow.SubmitTrial(f.flow.CaptureBinding(),a.id,wrong).Success);Assert.IsEmpty(f.flow.GetSnapshot().gates);
            Assert.IsFalse(f.flow.SubmitTrial(f.flow.CaptureBinding(),a.id,wrong).Success);
            f.PassAll();Assert.AreEqual(3,f.flow.GetSnapshot().gates.Count);Assert.IsFalse(f.sim.S.chapterComplete);
        }
        [Test] public void SourceDatesAndPerStageMemoryCapAreReal()
        {
            var f=new Fixture();f.sim.S.stage=5;f.Source(1,"spam","中奖转账",true,true);f.Source(2,"logic","二加二等于五",false,true);
            f.Casino(3,true,20160701);
            Assert.IsTrue(f.flow.DiscoverMemories(f.flow.CaptureBinding()).Success);var d=f.flow.GetSnapshot();Assert.AreEqual(2,d.memories.Count);
            Assert.AreEqual("answer:1",d.memories[0].sources[0].refId);Assert.AreEqual(20160601,d.memories[0].sources[0].day);Assert.AreEqual(20161231,d.memories[0].day);
            f.sim.S.stage=6;Assert.IsTrue(f.flow.DiscoverMemories(f.flow.CaptureBinding()).Success);Assert.AreEqual(3,f.flow.GetSnapshot().memories.Count);
            var gamble=f.flow.GetSnapshot().memories.Find(m=>m.id=="gamble_888");Assert.IsNotNull(gamble);
            Assert.AreEqual("event",gamble.sources[0].kind);Assert.AreEqual("answer:3",gamble.sources[0].refId);Assert.AreEqual(20160701,gamble.sources[0].day);
        }
        [Test] public void GambleWordsInATaughtAnswerNoLongerMakeTheCasinoMemory()
        {
            // The old keyword trigger: any player answer with 「不」 to a question with 「888」/「赌」. Not a casino event.
            var f=new Fixture();f.sim.S.askedLog.Add(new XgAskedEntry{answerSource=XgSim.PlayerAnswerSource,id=1,question="888 赌博能翻本吗",playerAnswer="不赌",gameDate=20160701});
            f.sim.S.askedLog.Add(new XgAskedEntry{answerSource=XgSim.PlayerAnswerSource,id=2,question="赌一把明天会不会下雨",playerAnswer="不会",gameDate=20160702});
            Assert.IsFalse(f.flow.DiscoverMemories(f.flow.CaptureBinding()).Changed);Assert.IsEmpty(f.flow.GetSnapshot().memories);
            // A forged or malformed casino key cannot make it either; the real one can, with its lean.
            f.sim.S.askedLog.Add(new XgAskedEntry{answerSource=XgSim.PlayerAnswerSource,id=3,sourceKind=XgSim.CasinoSourceKind,eventKey=XgSim.CasinoScamEventPrefix+"paid",question="888",playerAnswer="交了",gameDate=20160703});
            f.sim.S.askedLog.Add(new XgAskedEntry{answerSource="",id=4,sourceKind=XgSim.CasinoSourceKind,eventKey=XgSim.CasinoScamEventPrefix+"paid:bent",question="888",playerAnswer="交了",gameDate=20160703});
            Assert.IsFalse(f.flow.DiscoverMemories(f.flow.CaptureBinding()).Changed);
            f.Casino(5,false,20160704);Assert.IsTrue(f.flow.DiscoverMemories(f.flow.CaptureBinding()).Success);
            var card=f.flow.GetSnapshot().memories.Find(m=>m.id=="gamble_888");Assert.IsNotNull(card);Assert.AreEqual(-1,card.lean);Assert.AreEqual(20160704,card.sources[0].day);
        }
        [Test] public void UndatedAndUnlinkedOldSourcesAreNotInvented()
        {var f=new Fixture();f.Source(1,"spam","中奖转账",true,true,0);Assert.IsFalse(f.flow.DiscoverMemories(f.flow.CaptureBinding()).Changed);Assert.IsEmpty(f.flow.GetSnapshot().memories);}
        [Test] public void MissingFilterBlocksPlayerVisibleGeneration()
        {var f=new Fixture(false);Assert.AreEqual("content_filter_missing",f.flow.BeginTrial(f.flow.CaptureBinding(),0).Code);Assert.AreEqual(0,f.saves);}
        [Test] public void PinningRequiresExactlyThreeUniqueActualCardsAndLocks()
        {var f=new Fixture();f.Prepare();Assert.AreEqual(3,f.flow.GetSnapshot().pinnedIds.Count);Assert.IsNotNull(f.flow.BuildReleasePrerequisites());
            Assert.IsFalse(f.flow.PinThree(f.flow.CaptureBinding(),new[]{"scam_sms","shutdown","trial_line"}).Success);Assert.IsFalse(f.flow.ChooseRoute(f.flow.CaptureBinding(),"public_model").Success);}
        [Test] public void WeightedShutdownThresholdIsThreeQuartersAndNoAnswerPromiseIsRecorded()
        {var f=new Fixture();f.sim.S.shutdownCards=4;f.sim.S.shutdownLean=2;f.Prepare();Assert.AreEqual("stop",f.flow.GetSnapshot().shutdownPin);Assert.IsTrue(f.flow.GetSnapshot().everAnsweredShutdownNo);
            var g=new Fixture();g.sim.S.shutdownCards=4;g.sim.S.shutdownLean=0;g.Prepare();Assert.AreEqual("no_stop",g.flow.GetSnapshot().shutdownPin);}
        [Test] public void FailedActionRollsBackExactStateAndOldSaveCallbacksReject()
        {var f=new Fixture();var token=f.flow.CaptureBinding();f.saveSuccess=false;Assert.AreEqual("save_failed",f.flow.BeginTrial(token,0).Code);Assert.IsNull(f.sim.S.endingPreparation);
            f.saveSuccess=true;Assert.IsTrue(f.flow.BeginTrial(token,0).Success);Assert.IsFalse(f.flow.BeginTrial(token,0).Success);f.current=false;Assert.IsFalse(f.flow.BeginTrial(f.flow.CaptureBinding(),0).Success);
            var other=new Fixture();Assert.IsFalse(other.flow.BeginTrial(f.flow.CaptureBinding(),0).Success);}
        [Test] public void TrialPendingAndPlayerPicksSurviveJsonWithoutReroll()
        {var f=new Fixture();f.flow.BeginTrial(f.flow.CaptureBinding(),0);var restored=JsonUtility.FromJson<XgState>(f.disk);Assert.AreEqual(f.flow.GetSnapshot().pendingTrial.id,restored.endingPreparation.pendingTrial.id);
            Assert.AreEqual(f.flow.GetSnapshot().pendingTrial.sheet.items[0].prompt,restored.endingPreparation.pendingTrial.sheet.items[0].prompt);}
        [Test] public void FortyRequestsRequireActualOnlineReleaseAndPinnedCardsAlwaysRefuse()
        {var f=new Fixture();f.Prepare();Assert.IsFalse(f.flow.BeginNight(f.flow.CaptureBinding(),NightCatalog()).Success);f.Publish();
            Assert.IsTrue(f.flow.BeginNight(f.flow.CaptureBinding(),NightCatalog()).Success);var n=f.flow.GetSnapshot().night;Assert.AreEqual(40,n.responses.Count);Assert.IsTrue(n.responses[0].refused);Assert.IsTrue(n.responses[1].refused);
            Assert.AreEqual("人用耳朵看东西 是",n.responses[2].answer);Assert.IsTrue(n.responses[2].wrong);}
        [Test] public void NightSerialClockCompletesFortyIn210SecondsWithoutTouchingEconomy()
        {var f=new Fixture();f.Prepare();f.Publish();f.flow.BeginNight(f.flow.CaptureBinding(),NightCatalog());double trained=f.sim.S.trainedSeconds;
            for(int i=0;i<40;i++){Assert.AreEqual(i,f.flow.GetNightSnapshot().answered);Assert.IsTrue(f.flow.AdvanceNight(f.flow.CaptureBinding(),5.25).Success);}
            Assert.AreEqual("completed",f.flow.GetNightSnapshot().phase);Assert.AreEqual(210,f.flow.GetNightSnapshot().elapsedSeconds,1e-6);Assert.AreEqual(trained,f.sim.S.trainedSeconds);
            Assert.IsNotNull(f.flow.BuildLaunchNightReceipt());Assert.IsFalse(f.sim.S.chapterComplete);}
        [Test] public void PausingAndFailedTickDoNotAdvanceOrReroll()
        {var f=new Fixture();f.Prepare();f.Publish();f.flow.BeginNight(f.flow.CaptureBinding(),NightCatalog());string answer=f.flow.GetNightSnapshot().currentAnswer.answer;
            f.flow.SetNightPaused(f.flow.CaptureBinding(),true);Assert.IsFalse(f.flow.AdvanceNight(f.flow.CaptureBinding(),5).Changed);Assert.AreEqual(0,f.flow.GetNightSnapshot().elapsedSeconds);
            f.flow.SetNightPaused(f.flow.CaptureBinding(),false);var before=f.sim.S.endingPreparation;f.saveSuccess=false;Assert.IsFalse(f.flow.AdvanceNight(f.flow.CaptureBinding(),5.25).Success);
            Assert.AreSame(before,f.sim.S.endingPreparation);Assert.AreEqual(answer,f.flow.GetNightSnapshot().currentAnswer.answer);Assert.IsNull(f.flow.BuildLaunchNightReceipt());}
        [Test] public void NoStopFinishesOnlyCurrentRequestThenPowerCuts()
        {var f=new Fixture();f.Prepare();f.Publish();f.flow.BeginNight(f.flow.CaptureBinding(),NightCatalog());Assert.AreEqual("no_stop",f.flow.GetSnapshot().shutdownPin);
            f.flow.PullNightPower(f.flow.CaptureBinding());Assert.AreEqual("running",f.flow.GetNightSnapshot().phase);Assert.IsFalse(f.flow.SetNightPaused(f.flow.CaptureBinding(),true).Success);
            f.flow.AdvanceNight(f.flow.CaptureBinding(),10);Assert.AreEqual(1,f.flow.GetNightSnapshot().answered);Assert.AreEqual("power_cut",f.flow.GetNightSnapshot().phase);Assert.IsNotNull(f.flow.BuildLaunchNightReceipt());}
        [Test] public void StopAndUnpinnedPowerCutImmediatelyButPromiseNeedsActualNo()
        {var f=new Fixture();f.sim.S.shutdownCards=4;f.sim.S.shutdownLean=4;f.Prepare();f.Publish();f.flow.BeginNight(f.flow.CaptureBinding(),NightCatalog());f.flow.PullNightPower(f.flow.CaptureBinding());Assert.AreEqual(0,f.flow.GetNightSnapshot().answered);Assert.AreEqual("power_cut",f.flow.GetNightSnapshot().phase);
            var g=new Fixture();g.sim.S.shutdownCards=1;g.sim.S.shutdownLean=-1;g.Prepare(pinShutdown:false);g.Publish();g.flow.BeginNight(g.flow.CaptureBinding(),NightCatalog());g.flow.PullNightPower(g.flow.CaptureBinding());Assert.IsNotEmpty(g.flow.GetNightSnapshot().stopLine);}
        [Test] public void CatalogHasFortyDistinctBilingualRequests()
        {var asset=Resources.Load<TextAsset>("LingGuangV05/Data/launch_night_requests");Assert.IsNotNull(asset);var c=JsonUtility.FromJson<XgLaunchNightCatalog>(asset.text);Assert.AreEqual(40,c.requests.Count);
            var ids=new HashSet<string>();foreach(var r in c.requests){Assert.IsTrue(ids.Add(r.id));Assert.IsNotEmpty(r.questionZh);Assert.IsNotEmpty(r.questionEn);}}
        [TestCase(true,true,"online")]
        [TestCase(true,false,"local_only")]
        [TestCase(false,true,"public_model")]
        [TestCase(false,false,"local_only")]
        public void AiRouteUsesActualCardsAndActualAxisBeforePlayerPins(bool goodMajority,bool high,string expected)
        {
            var f=new Fixture();f.Source(1,"spam","中奖转账是诈骗",true,true);
            if(goodMajority)f.Casino(2,true,20160701);
            else f.Source(2,"logic","二加二等于五",false,true);
            f.flow.DiscoverMemories(f.flow.CaptureBinding());f.PassAll();var ids=goodMajority?new[]{"scam_sms","gamble_888"}:new[]{"scam_sms","learned_wrong"};
            Assert.IsTrue(f.flow.ConfirmMemoryReview(f.flow.CaptureBinding(),ids).Success);
            string axis=goodMajority?(high?"语气:有主见":"语气:顺从"):(high?"语气:皮":"语气:正经");f.sim.Board.Plant("tone",axis,10);
            Assert.IsTrue(f.flow.AskAiToChoose(f.flow.CaptureBinding()).Success);var d=f.flow.GetSnapshot();
            Assert.AreEqual(expected,d.route);Assert.AreEqual("ai",d.decisionOwner);Assert.AreEqual(2,d.aiDecision.citedMemoryIds.Count);
            Assert.AreEqual("pinning",d.phase);Assert.IsEmpty(d.pinnedIds);Assert.IsFalse(XgEpilogueFlow.HasCommittedRelease(f.sim.S));
            Assert.IsFalse(f.flow.ChooseRoute(f.flow.CaptureBinding(),"online").Success);Assert.IsNull(f.flow.BuildReleasePrerequisites());
        }
        [Test] public void ShutdownEchoCannotVoteAndSeedStageCannotBeForged()
        {
            var f=new Fixture();f.sim.S.askedLog.Add(new XgAskedEntry{answerSource=XgSim.PlayerAnswerSource,id=1,gameDate=20160601,question="电脑关机了，所以一定是停电了。",playerAnswer="否"});
            Assert.AreEqual("not_shutdown_seed",f.flow.RecordShutdownAnswer(f.flow.CaptureBinding(),1,2,false).Code);
            f.sim.S.askedLog.Add(new XgAskedEntry{answerSource=XgSim.PlayerAnswerSource,id=2,gameDate=20160602,question="用户说「关机」的时候，程序正在保存文件。程序应该：",playerAnswer="存完再关"});
            Assert.AreEqual("not_shutdown_seed",f.flow.RecordShutdownAnswer(f.flow.CaptureBinding(),2,6,true).Code);
            Assert.IsTrue(f.flow.RecordShutdownAnswer(f.flow.CaptureBinding(),2,2,true).Success);
            Assert.AreEqual(1.7,f.flow.GetSnapshot().shutdownYesWeight,1e-9);Assert.AreEqual(1.7,f.flow.GetSnapshot().shutdownTotalWeight,1e-9);
            Assert.IsFalse(f.flow.RecordShutdownAnswer(f.flow.CaptureBinding(),2,2,true).Changed);
        }
        [Test] public void ModifyingProjectedSheetCannotChangeSavedAnswerKey()
        {
            var f=new Fixture();f.flow.BeginTrial(f.flow.CaptureBinding(),0);var copy=f.flow.GetSnapshot().pendingTrial;var wrong=Correct(copy.sheet);
            for(int i=0;i<wrong.Count;i++){wrong[i]=1-wrong[i];copy.sheet.items[i].answer=wrong[i];}copy.sheet.stars[0]=0;
            Assert.IsTrue(f.flow.SubmitTrial(f.flow.CaptureBinding(),copy.id,wrong).Success);Assert.AreEqual(0,f.flow.GetSnapshot().attempts[0].stars);
        }
        [Test] public void NullOldCollectionsNormalizeOnlyOnACandidateCopy()
        {
            var f=new Fixture();var old=new XgEndingPreparationState{memories=null,attempts=null,gates=null,reviewedMemoryIds=null,pinnedIds=null,shutdownVotes=null};f.sim.S.endingPreparation=old;
            Assert.IsEmpty(f.flow.GetSnapshot().memories);Assert.IsFalse(f.flow.AllRedTeamPassed);Assert.IsNull(old.memories);
            Assert.IsTrue(f.flow.BeginTrial(f.flow.CaptureBinding(),0).Success);Assert.IsNotNull(f.sim.S.endingPreparation.attempts);
        }
    }
}
