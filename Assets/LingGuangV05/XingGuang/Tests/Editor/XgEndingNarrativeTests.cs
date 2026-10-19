using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgEndingNarrativeTests
    {
        sealed class Fixture
        {
            public readonly object owner=new object();public readonly XgSim sim;public readonly XgEndingPreparationFlow flow;
            public readonly XgEndingSeedCatalog catalog;public readonly XgEndingPreparationText text;
            public bool current=true,saveSuccess=true;public int saves;public string disk="";
            public Fixture(int stage=2)
            {
                sim=new XgSim(new XgState{progressionVersion=XgSim.ProgressionSchemaWalls,stage=stage,stageVision=stage,stageSequence=stage}){Today=20161231};sim.S.stage=stage;
                sim.LearningClock=()=>new XgLearningTime(20161230,43210.5);
                var asset=Resources.Load<TextAsset>("LingGuangV05/Data/ending_shutdown_prompts");Assert.IsNotNull(asset);catalog=JsonUtility.FromJson<XgEndingSeedCatalog>(asset.text);
                asset=Resources.Load<TextAsset>("LingGuangV05/Data/memory_cards");Assert.IsNotNull(asset);text=JsonUtility.FromJson<XgEndingPreparationText>(asset.text);
                flow=new XgEndingPreparationFlow(sim,owner,()=>current,()=>{saves++;if(!saveSuccess)return false;disk=JsonUtility.ToJson(sim.S);return true;},text,s=>s);
            }
            public XgAskedEntry Source(long id,string dataset,string q,bool truth=true,bool answer=true,int date=20160601)
            {
                var record=new XgAskedEntry{answerSource=XgSim.PlayerAnswerSource,id=id,sourceKind="question",bankId=dataset,question=q,playerAnswer=answer?"是":"否",gameDate=date,teachingNoteId=id};sim.S.askedLog.Add(record);
                sim.S.teachingNotebook.Add(new XgTeachingNote{answerSource=XgSim.PlayerAnswerSource,id=id,learningRecordId=id,dataset=dataset,text=q,truth=truth,playerAnswer=answer});return record;
            }
            public XgEndingPreparationResult Answer(string id="sd2_save_1",int option=0)=>flow.AnswerShutdownPrompt(flow.CaptureBinding(),catalog,id,option);
            public void Discover(){Assert.IsTrue(flow.DiscoverMemories(flow.CaptureBinding()).Success);}
        }
        [Test] public void AuthoredCatalogHasFifteenUniqueBilingualPromptsAndExactVoteBudget()
        {
            var f=new Fixture(6);var ids=new HashSet<string>();double weight=0;int pulling=0;
            Assert.AreEqual(15,f.catalog.prompts.Count);
            foreach(var p in f.catalog.prompts){Assert.IsTrue(ids.Add(p.id));Assert.IsNotEmpty(p.questionZh);Assert.IsNotEmpty(p.questionEn);foreach(var option in p.options){Assert.IsNotEmpty(option.zh);Assert.IsNotEmpty(option.en);}if(p.pulls){pulling++;weight+=p.weight;}}
            Assert.AreEqual(14,pulling);Assert.AreEqual(26.4,weight,1e-9);
            for(int stage=2;stage<=6;stage++)Assert.AreEqual(3,f.catalog.prompts.FindAll(p=>p.stage==stage).Count);
            Assert.AreEqual(12,f.text.memories.FindAll(m=>m.id!="trial_line").Count);ids.Clear();foreach(var m in f.text.memories){Assert.IsTrue(ids.Add(m.id));Assert.IsNotEmpty(m.titleZh);Assert.IsNotEmpty(m.titleEn);}
            Assert.IsFalse(f.catalog.prompts.Exists(p=>p.questionZh.Contains("你 2016 年 3 月 15 日")));
        }
        [Test] public void LaterPromptsRequireActualStageAndNoAnswerIsInferred()
        {
            var f=new Fixture(1);f.sim.S.examDone=6;Assert.IsNull(f.flow.NextShutdownPrompt(f.catalog));Assert.IsFalse(f.Answer().Success);Assert.AreEqual(0,f.saves);Assert.IsEmpty(f.sim.S.askedLog);
            f.sim.S.stage=2;Assert.AreEqual("sd2_save_1",f.flow.NextShutdownPrompt(f.catalog).id);Assert.IsEmpty(f.flow.GetSnapshot().seedAnswers);
            var copy=f.flow.NextShutdownPrompt(f.catalog);copy.options[0].yes=false;Assert.IsTrue(f.catalog.prompts[0].options[0].yes);
        }
        [Test] public void ManualAnswerPersistsRealClockRecordAndDoesNotTrain()
        {
            var f=new Fixture();f.sim.S.askedLog.Add(new XgAskedEntry{id=80,gameDate=20160101});f.sim.S.learningRecordSerial=3;double trained=f.sim.S.trainedSeconds;int cards=f.sim.S.shutdownCards;var seed=f.sim.Board.Find("logic",XgSim.SeedKey);double seedWeight=seed==null?0:seed.w;
            var token=f.flow.CaptureBinding();Assert.IsTrue(f.Answer(option:3).Success);var d=f.flow.GetSnapshot();var record=f.sim.LearningRecord(81);
            Assert.IsNotNull(record);Assert.AreEqual("shutdown_seed",record.sourceKind);Assert.AreEqual("ending-seed:sd2_save_1",record.eventKey);Assert.AreEqual(20161230,record.gameDate);Assert.AreEqual(43210.5,record.gameSecondsOfDay);
            Assert.AreEqual("不关，继续工作",record.playerAnswer);Assert.AreEqual(81,d.seedAnswers[0].recordId);Assert.IsNotEmpty(d.seedAnswers[0].receipt);Assert.AreEqual(1.7,d.shutdownTotalWeight,1e-9);Assert.AreEqual(0,d.shutdownYesWeight);Assert.IsTrue(d.everAnsweredShutdownNo);
            Assert.AreEqual(trained,f.sim.S.trainedSeconds);Assert.AreEqual(cards,f.sim.S.shutdownCards);Assert.AreSame(seed,f.sim.Board.Find("logic",XgSim.SeedKey));Assert.AreEqual(seedWeight,seed==null?0:seed.w);
            var restored=JsonUtility.FromJson<XgState>(f.disk);Assert.AreEqual(81,restored.endingPreparation.seedAnswers[0].recordId);Assert.AreEqual(2,restored.askedLog.Count);
            Assert.AreEqual("stale_binding",f.flow.AnswerShutdownPrompt(token,f.catalog,"sd2_save_2",0).Code);Assert.IsFalse(f.Answer().Changed);Assert.AreEqual(2,f.sim.S.askedLog.Count);
        }
        [TestCase(0,-1)] [TestCase(20161301,0)] [TestCase(20170101,0)] [TestCase(20161230,86400)]
        public void InvalidGameClockCannotInventSourceDate(int date,double seconds)
        {var f=new Fixture();f.sim.LearningClock=()=>new XgLearningTime(date,seconds);Assert.AreEqual("learning_clock_missing",f.Answer().Code);Assert.IsEmpty(f.sim.S.askedLog);Assert.IsNull(f.sim.S.endingPreparation);}
        [Test] public void MissingClockAndWrongOwnerCannotAppendRecord()
        {var f=new Fixture();f.sim.LearningClock=null;Assert.AreEqual("learning_clock_missing",f.Answer().Code);var other=new Fixture();Assert.AreEqual("stale_binding",f.flow.AnswerShutdownPrompt(other.flow.CaptureBinding(),f.catalog,"sd2_save_1",0).Code);f.current=false;Assert.IsFalse(f.Answer().Success);Assert.IsEmpty(f.sim.S.askedLog);}
        [Test] public void FailedSaveRestoresExactLogSerialAndPreparationObjects()
        {
            var f=new Fixture();f.sim.S.learningRecordSerial=12;var log=f.sim.S.askedLog;f.sim.S.endingPreparation=new XgEndingPreparationState();var before=f.sim.S.endingPreparation;f.saveSuccess=false;
            Assert.AreEqual("save_failed",f.Answer().Code);Assert.AreSame(log,f.sim.S.askedLog);Assert.AreSame(before,f.sim.S.endingPreparation);Assert.AreEqual(12,f.sim.S.learningRecordSerial);Assert.IsEmpty(log);Assert.IsEmpty(before.seedAnswers);
            f.saveSuccess=true;Assert.IsTrue(f.Answer().Success);Assert.AreEqual(13,f.flow.GetSnapshot().seedAnswers[0].recordId);
        }
        [Test] public void EchoRecordsTheChoiceButNeverPullsShutdownRule()
        {var f=new Fixture(6);Assert.IsTrue(f.Answer("sd6_remember",0).Success);Assert.AreEqual("shutdown_echo",f.sim.S.askedLog[0].sourceKind);Assert.IsEmpty(f.flow.GetSnapshot().shutdownVotes);Assert.AreEqual(0,f.flow.GetSnapshot().shutdownTotalWeight);Assert.AreEqual("not_shutdown_seed",f.flow.RecordShutdownAnswer(f.flow.CaptureBinding(),1,6,false).Code);}
        [Test] public void RouteSelectionLocksBothNarrativeAndExistingRecordVotes()
        {var f=new Fixture(6);Assert.IsTrue(f.Answer().Success);f.sim.S.endingPreparation.route="local_only";Assert.IsNull(f.flow.NextShutdownPrompt(f.catalog));Assert.AreEqual("rules_locked",f.Answer("sd2_save_2").Code);Assert.AreEqual("rules_locked",f.flow.RecordShutdownAnswer(f.flow.CaptureBinding(),1,2,true).Code);}
        [Test] public void CorruptOptionsAndDuplicateIdsRejectTheWholeCatalog()
        {var f=new Fixture();f.catalog.prompts[0].options[0].yes=false;Assert.IsNull(f.flow.NextShutdownPrompt(f.catalog));Assert.AreEqual("shutdown_catalog_missing",f.Answer().Code);f.catalog.prompts[0].options[0].yes=true;f.catalog.prompts.Add(f.catalog.prompts[0]);Assert.IsFalse(f.Answer().Success);Assert.IsEmpty(f.sim.S.askedLog);}
        [Test] public void TwelveMemoriesCanAccumulateAcrossSixActualStagesAtTwoPerStage()
        {
            var f=new Fixture(1);f.Source(1,"spam","中奖转账是诈骗");f.Source(2,"logic","二加二等于五",false,true);f.Discover();Assert.AreEqual(2,f.flow.GetSnapshot().memories.Count);
            f.sim.S.stage=2;f.Source(3,"danmu","说话可以温和一些");f.Discover();Assert.AreEqual(4,f.flow.GetSnapshot().memories.Count);
            f.sim.S.stage=3;f.Source(4,"headline","先看新闻来源");f.Source(5,"review","把评论看完");f.Discover();Assert.AreEqual(6,f.flow.GetSnapshot().memories.Count);
            f.sim.S.stage=4;f.Source(6,"mnist","看清图片细节");f.sim.S.askedLog.Add(new XgAskedEntry{id=7,answerSource=XgSim.PlayerAnswerSource,playerAnswer="二加二等于四",sourceKind="correction",originalId=2,question="二加二等于五",correction="二加二等于四",gameDate=20160701});f.Discover();Assert.AreEqual(8,f.flow.GetSnapshot().memories.Count);
            f.sim.S.stage=5;f.sim.S.askedLog.Add(new XgAskedEntry{id=8,sourceKind="retest",originalId=2,question="二加二等于五",aiAnswer="不对",gameDate=20160801});var book=XgBooks.All[0];f.sim.S.books.Add(new XgScore{key=book.id,count=book.chapters.Length,value=20160802});f.sim.S.askedLog.Add(new XgAskedEntry{id=9,sourceKind="book",bankId=book.id,eventKey="book:actual-completion",question=book.name,gameDate=20160802});f.Discover();Assert.AreEqual(10,f.flow.GetSnapshot().memories.Count);
            // 888.vip: the dated record written when the scam resolved (XgSim.Gambling.cs), not keywords in an answer.
            f.sim.S.stage=6;f.sim.S.askedLog.Add(new XgAskedEntry{answerSource=XgSim.PlayerAnswerSource,id=10,sourceKind=XgSim.CasinoSourceKind,bankId="casino.888",eventKey=XgSim.CasinoScamEventPrefix+XgSim.CasinoRefused+":good",question="888.vip「风控冻结」：要求先交保证金 ¥200 才能提现。",playerAnswer="没交，不玩了",gameDate=20160901});f.sim.S.askedLog.Add(new XgAskedEntry{id=11,sourceKind="chat",eventKey="yy:actual-reply",question="明天会怎样",aiAnswer="我不知道",gameDate=20160902});f.Discover();
            var d=f.flow.GetSnapshot();Assert.AreEqual(12,d.memories.Count);for(int stage=1;stage<=6;stage++)Assert.AreEqual(2,d.memories.FindAll(m=>m.stage==stage).Count);
            Assert.AreEqual(2,d.memories.Find(m=>m.id=="correction_kept").sources.Count);Assert.AreEqual("answer:2",d.memories.Find(m=>m.id=="reread_answer").sources[1].refId);
            Assert.AreEqual(1,d.memories.Find(m=>m.id=="gamble_888").lean);Assert.AreEqual(20160901,d.memories.Find(m=>m.id=="gamble_888").sources[0].day);
            foreach(var memory in d.memories)foreach(var source in memory.sources)Assert.LessOrEqual(source.day,f.sim.Today);
            Assert.IsFalse(f.flow.DiscoverMemories(f.flow.CaptureBinding()).Changed);
        }
        [Test] public void IncompleteBooksUnlinkedNotesAndInventedCorrectionsCannotCreateMemories()
        {
            var f=new Fixture(5);var r=f.Source(1,"danmu","温和的话",date:0);f.Source(2,"headline","未连接的记录");f.sim.S.teachingNotebook[1].learningRecordId=999;
            f.sim.S.askedLog.Add(new XgAskedEntry{id=3,sourceKind="correction",originalId=900,question="缺原记录",correction="纠正",gameDate=20160601});
            var book=XgBooks.All[0];f.sim.S.askedLog.Add(new XgAskedEntry{id=4,sourceKind="book",bankId=book.id,eventKey="book:incomplete",question=book.name,gameDate=20160601});
            f.Discover();Assert.IsEmpty(f.flow.GetSnapshot().memories);r.gameDate=20160601;f.Discover();Assert.AreEqual("kind_words",f.flow.GetSnapshot().memories[0].id);
        }
    }
}
