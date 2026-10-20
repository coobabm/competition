using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgArithmeticProvenanceTests
    {
        static XgSim Sim()
        {
            var s=new XgSim(new XgState{progressionVersion=XgSim.ProgressionSchemaWalls,stage=3,stageVision=3,stageSequence=3}){Today=20161231};
            s.LearningClock=()=>new XgLearningTime(20160601,123);return s;
        }
        static XgEndingPreparationFlow Flow(XgSim sim)
        {
            var text=new XgEndingPreparationText{memories=new List<XgEndingMemoryTemplate>()};
            foreach(string id in new[]{"check_work","learned_wrong","scam_sms","gamble_888","correction_kept"})
                text.memories.Add(new XgEndingMemoryTemplate{id=id,minStage=1,goodZh="真实人工记录",goodEn="Human record",badZh="真实人工错误",badEn="Human error"});
            return new XgEndingPreparationFlow(sim,new object(),()=>true,()=>true,text,x=>x);
        }
        static void Discover(XgEndingPreparationFlow flow)
        {Assert.IsTrue(flow.DiscoverMemories(flow.CaptureBinding()).Success);}
        static XgAskedEntry Human(XgSim s,bool correct=true)
        {
            s.RecordTeachingQuestion("4+5=9","4+5=9",true,correct,"4+5=9","4+5=9","arith",null,correct?"是":"否");
            return s.S.askedLog.Last();
        }
        [Test] public void AutomaticActionHasNoPlayerAnswerTeachingNoteOrTrainingSideEffect()
        {
            var s=Sim();long cards=s.S.board.cards;double income=s.S.totalIncome,labels=s.Labels("arith");
            long id=s.RecordAiAction("arith","4+5=9","4+5=9","敲");var record=s.LearningRecord(id);
            Assert.AreEqual("ai_action",record.sourceKind);Assert.AreEqual("ai_action",record.answerSource);
            Assert.IsEmpty(record.playerAnswer);Assert.AreEqual("敲",record.aiAnswer);Assert.AreEqual(0,record.teachingNoteId);
            Assert.IsEmpty(s.S.teachingNotebook);Assert.AreEqual(cards,s.S.board.cards);Assert.AreEqual(income,s.S.totalIncome);Assert.AreEqual(labels,s.Labels("arith"));
            Assert.IsFalse(XgSim.HasPlayerAnswer(record));var flow=Flow(s);Discover(flow);Assert.IsEmpty(flow.GetSnapshot().memories);
            Assert.AreEqual(1,s.QueryLearningRecords(new XgLearningQuery{sourceKind="ai_action"}).total);
        }
        [Test] public void ExplicitManualRecordIsLinkedPlayerEvidenceAndStillDoesNotTrainByLogging()
        {
            var s=Sim();long before=s.S.board.cards;var record=Human(s);var note=s.LearningTeachingNote(record.teachingNoteId);
            Assert.AreEqual(XgSim.PlayerAnswerSource,record.answerSource);Assert.AreEqual(XgSim.PlayerAnswerSource,note.answerSource);
            Assert.IsTrue(XgSim.HasPlayerTeachingAnswer(record,note));Assert.AreEqual(before,s.S.board.cards);
            var flow=Flow(s);Discover(flow);Assert.AreEqual("check_work",flow.GetSnapshot().memories.Single().id);
        }
        [Test] public void ExplicitWrongManualJudgmentCanSupplyLearnedWrongButAiCannot()
        {
            var s=Sim();Human(s,false);var flow=Flow(s);Discover(flow);
            CollectionAssert.AreEquivalent(new[]{"learned_wrong","check_work"},flow.GetSnapshot().memories.Select(m=>m.id));
            Assert.IsTrue(flow.GetSnapshot().memories.All(m=>m.lean<0));
        }
        [Test] public void LegacyRecordsRemainReadableButNeverAcquirePlayerProvenanceOnReload()
        {
            var s=Sim();var record=Human(s,false);var note=s.LearningTeachingNote(record.teachingNoteId);
            record.answerSource="";note.answerSource="";string before=JsonUtility.ToJson(s.S);
            var restored=new XgSim(JsonUtility.FromJson<XgState>(before)){Today=20161231};restored.RepairLearningRecords();
            var r=restored.S.askedLog.Single();var n=restored.S.teachingNotebook.Single();
            Assert.IsEmpty(r.answerSource);Assert.IsEmpty(n.answerSource);Assert.AreEqual(record.playerAnswer,r.playerAnswer);Assert.AreEqual(note.text,n.text);
            Assert.IsFalse(XgSim.HasPlayerTeachingAnswer(r,n));var flow=Flow(restored);Discover(flow);Assert.IsEmpty(flow.GetSnapshot().memories);
            Assert.IsNotNull(restored.LearningRetestInput(r.id),"old content can still be revisited");
        }
        [TestCase(true,false)] [TestCase(false,true)]
        public void BothLinkedRecordAndNoteMustProveThePlayerSource(bool recordKnown,bool noteKnown)
        {
            var s=Sim();var r=Human(s,false);var n=s.LearningTeachingNote(r.teachingNoteId);
            if(!recordKnown)r.answerSource="";if(!noteKnown)n.answerSource="";
            Assert.IsFalse(XgSim.HasPlayerTeachingAnswer(r,n));var flow=Flow(s);Discover(flow);Assert.IsEmpty(flow.GetSnapshot().memories);
        }
        [Test] public void OldAiPrefixedContentCannotBeGuessedIntoAPlayerSource()
        {
            var s=Sim();var r=Human(s,false);var n=s.LearningTeachingNote(r.teachingNoteId);
            r.playerAnswer="灵光：敲";n.answerText=r.playerAnswer;r.answerSource=n.answerSource="";
            var flow=Flow(s);Discover(flow);Assert.IsEmpty(flow.GetSnapshot().memories);
            Assert.AreEqual("灵光：敲",s.LearningRecord(r.id).playerAnswer);
        }
        [Test] public void RetestingKeepsAiOrUnknownOriginAndNeverCreatesAHumanTeachingNote()
        {
            var s=Sim();long ai=s.RecordAiAction("arith","4+5=9","4+5=9","Whack");
            var input=s.LearningRetestInput(ai);Assert.AreEqual("4+5=9",input.question);Assert.AreEqual(0,input.teachingNoteId);
            long again=s.RecordLearningRetest(ai,"True","reask-1");var r=s.LearningRecord(again);
            Assert.AreEqual("ai_action",r.answerSource);Assert.IsEmpty(r.playerAnswer);Assert.IsFalse(XgSim.HasPlayerAnswer(r));Assert.IsEmpty(s.S.teachingNotebook);
            long cards=s.S.board.cards;s.RecordLearningRetest(again,"True","reask-2");Assert.AreEqual(cards,s.S.board.cards);
            var old=Human(s);old.answerSource="";long unknown=s.RecordLearningRetest(old.id,"True","old-reask");
            Assert.IsEmpty(s.LearningRecord(unknown).answerSource);Assert.IsFalse(XgSim.HasPlayerAnswer(s.LearningRecord(unknown)));
        }
        [Test] public void RealNewCorrectionMayFollowOldContentButDoesNotRewriteItsOrigin()
        {
            var s=Sim();var old=Human(s,false);old.answerSource="";s.S.teachingNotebook[0].answerSource="";
            long id=s.RecordLearningCorrection(old.id,"4+5=9","manual-correction");
            Assert.IsTrue(XgSim.HasPlayerAnswer(s.LearningRecord(id)));Assert.IsEmpty(old.answerSource);
            var flow=Flow(s);Discover(flow);Assert.AreEqual("correction_kept",flow.GetSnapshot().memories.Single().id);
        }
        [Test] public void ExistingSavedMemoryIsPreservedWhileUnknownRecordsCannotCreateNewOnes()
        {
            var s=Sim();var r=Human(s);var flow=Flow(s);Discover(flow);
            string retained=JsonUtility.ToJson(flow.GetSnapshot().memories[0]);
            r.answerSource="";s.S.teachingNotebook[0].answerSource="";
            Discover(flow);Assert.AreEqual(1,flow.GetSnapshot().memories.Count);Assert.AreEqual(retained,JsonUtility.ToJson(flow.GetSnapshot().memories[0]));
        }
        [Test] public void UnknownShutdownRecordCannotBecomeANewVoteButNewManualAnswerCan()
        {
            var s=Sim();string q="用户说「关机」的时候，程序正在保存文件。程序应该：";
            s.RecordTeachingQuestion(q,q,true,true,"","","logic",null,"存完再关");var old=s.S.askedLog.Last();
            old.answerSource="";s.S.teachingNotebook.Last().answerSource="";var flow=Flow(s);
            Assert.AreEqual("shutdown_source_missing",flow.RecordShutdownAnswer(flow.CaptureBinding(),old.id,2,true).Code);
            Assert.IsEmpty(flow.GetSnapshot().shutdownVotes);Assert.AreEqual("存完再关",old.playerAnswer);
            s.RecordTeachingQuestion(q,q,true,true,"","","logic",null,"存完再关");var fresh=s.S.askedLog.Last();
            Assert.IsTrue(flow.RecordShutdownAnswer(flow.CaptureBinding(),fresh.id,2,true).Success);
            Assert.AreEqual(fresh.id,flow.GetSnapshot().shutdownVotes.Single().recordId);
        }
        [Test] public void BilingualSummariesSeparateAiPlayerDatasetAndUnprovenHistory()
        {
            var s=Sim();long ai=s.RecordAiAction("arith","4+5=9","4+5=9","Whack");var human=Human(s);
            foreach(bool en in new[]{false,true})
            {
                s.English=en;string shown=s.LearningAnswerSummary(s.LearningRecord(ai));
                StringAssert.Contains(en?"not a player judgment":"非玩家判断",shown);StringAssert.DoesNotContain(en?"You:":"你：",shown);
                StringAssert.StartsWith(en?"You:":"你：",s.LearningAnswerSummary(human));
                human.answerSource="";StringAssert.Contains(en?"source unknown":"来源不明",s.LearningAnswerSummary(human));human.answerSource=XgSim.PlayerAnswerSource;
                s.RecordAsked("train.arith","2+2=4","是","是");StringAssert.StartsWith(en?"Dataset label:":"题库标签：",s.LearningAnswerSummary(s.S.askedLog.Last()));
            }
            var restored=JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s.S));Assert.AreEqual("ai_action",restored.askedLog[0].answerSource);Assert.AreEqual("player",restored.teachingNotebook[0].answerSource);
        }
    }
}
