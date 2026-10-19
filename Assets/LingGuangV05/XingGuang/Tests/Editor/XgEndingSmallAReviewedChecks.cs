using System;
using System.Collections.Generic;
using LingGuangV05.Core.Forum;

namespace LingGuangV05.XingGuang.Tests
{
    public static class XgEndingSmallAReviewedChecks
    {
        public static int Run(Func<XgState,string> serialize)
        {
            int count=0;Action<bool,string> need=(ok,label)=>{count++;if(!ok)throw new InvalidOperationException(label);};
            var sim=new XgSim();var s=sim.S;s.stage=6;s.fullOpen=true;
            need(XgEndingPresentation.UsesPreparation(s),"new stage6 uses new ending");
            need(!XgEndingPresentation.UsesPreparation(null),"missing state stays unavailable");
            need(XgEndingPresentation.FinalApproachDue(s,false),"unstarted final stage permits invitation");
            need(!XgEndingPresentation.FinalApproachDue(s,true),"persisted invitation does not repeat");
            s.stage=5;need(!XgEndingPresentation.FinalApproachDue(s,false),"stage5 never gets the exception");s.stage=6;
            s.fullOpen=false;need(!XgEndingPresentation.FinalApproachDue(s,false),"locked finale never gets exception");s.fullOpen=true;
            s.chapterComplete=true;need(!XgEndingPresentation.UsesPreparation(s)&&!XgEndingPresentation.FinalApproachDue(s,false),"legacy completion keeps legacy browser and guide");s.chapterComplete=false;
            s.ending="legacy";need(!XgEndingPresentation.FinalApproachDue(s,false),"completed route suppresses invitation");s.ending="";
            s.endingPreparation=new XgEndingPreparationState{pendingTrial=new XgEndingTrialAttempt()};
            need(XgEndingPresentation.FinalApproachDue(s,false),"Unity empty pending placeholder is not invented progress");
            s.endingPreparation.pendingTrial.id="actual";need(!XgEndingPresentation.FinalApproachDue(s,false),"actual open sheet suppresses invitation");s.endingPreparation.pendingTrial=null;
            s.endingPreparation.attempts.Add(new XgEndingTrialAttempt{id="done"});need(!XgEndingPresentation.FinalApproachDue(s,false),"attempt history suppresses invitation");s.endingPreparation.attempts.Clear();
            s.endingPreparation.finalPageReceipt="page";need(!XgEndingPresentation.FinalApproachDue(s,false),"final page suppresses invitation");
            need(XgEndingPresentation.Guide(s).id=="final.preparation.si","guide points to SI inside Finale");
            s.endingPreparation.siReadReceipt="read";need(!XgEndingPresentation.FinalApproachDue(s,false),"read letter suppresses invitation");
            need(XgEndingPresentation.Guide(s).id=="final.preparation.choice","read letter points to choice");
            s.endingPreparation.route="local_only";need(!XgEndingPresentation.FinalApproachDue(s,false),"selected route suppresses invitation");
            need(XgEndingPresentation.Guide(s).id=="final.preparation.pins","chosen route points to pinning");
            s.endingPreparation.pinReceipt="pins";need(XgEndingPresentation.Guide(s).id=="final.preparation.publish","pins point to confirmation");
            var guide=XgGuide.Current(sim,1000000).Find(x=>x.id.StartsWith("final.preparation.",StringComparison.Ordinal));
            need(guide!=null&&guide.app==XgGuide.Lab&&guide.tab=="final"&&guide.target=="","current guide never points at removed Bodu or legacy test control");
            need(!string.IsNullOrWhiteSpace(guide.zh)&&!string.IsNullOrWhiteSpace(guide.en),"new guide is bilingual");
            var source=new XgEndingMemorySource{kind="answer",refId="answer:41",day=20160703,summary="A recorded question"};
            var record=new XgAskedEntry{id=41,sourceKind="book",question=source.summary,gameDate=source.day};s.askedLog.Add(record);
            string before=serialize(s);
            foreach(bool en in new[]{false,true})
            {
                var line=XgEndingPresentation.Evidence(sim,source,en);
                need(line.Contains(en?"July 3":"7 月 3 日"),"citation uses the real saved date");
                need(!line.Contains(en?"you taught":"教过"),"book source never claims player teaching");
            }
            need(before==serialize(s),"citation projection changes no saved field");
            record.sourceKind="retest";need(!XgEndingPresentation.Evidence(sim,source,true).Contains("you taught"),"retest is not teaching");
            record.sourceKind=XgSim.AiActionSource;record.answerSource=XgSim.AiActionSource;need(!XgEndingPresentation.Evidence(sim,source,false).Contains("教过"),"automatic action is not player teaching");
            record.sourceKind="chat";record.eventKey="yy:actual";need(XgEndingPresentation.Evidence(sim,source,true).Contains("you told"),"actual private chat uses chat verb");
            record.sourceKind="question";record.answerSource=XgSim.PlayerAnswerSource;record.playerAnswer="yes";record.teachingNoteId=7;
            s.teachingNotebook.Add(new XgTeachingNote{id=7,learningRecordId=41,answerSource=XgSim.PlayerAnswerSource});
            need(XgEndingPresentation.Evidence(sim,source,true).Contains("you taught"),"linked human teaching proof permits teaching verb");
            need(XgEndingPresentation.Evidence(sim,source,false).Contains("因为你在 7 月 3 日 教过它"),"approved Chinese teaching source line");
            record.sourceKind="shutdown_seed";need(XgEndingPresentation.Evidence(sim,source,true).Contains("you answered"),"narrative choice uses answered not taught");
            source.day=20160704;need(!XgEndingPresentation.Evidence(sim,source,true).Contains("you answered"),"mismatched source date cannot claim a player action");
            source.day=20161332;need(!XgEndingPresentation.Evidence(sim,source,true).Contains("2016-13-32"),"invalid old date is never presented as a real day");
            need(XgEndingPresentation.Evidence(sim,null,false)=="","missing evidence remains absent");
            var qa=new LaoZhouQaState();int messages=0,saves=0;Func<Action> append=()=>{messages++;return ()=>messages--;};
            need(!LaoZhouQuestions.CommitFinalApproach(qa,append,()=>{saves++;return false;})&&!qa.finalApproachSent&&messages==0,"failed commit rolls back message and marker");
            need(!LaoZhouQuestions.CommitFinalApproach(qa,append,()=>throw new InvalidOperationException())&&!qa.finalApproachSent&&messages==0,"thrown save rolls back message and marker");
            need(!LaoZhouQuestions.CommitFinalApproach(qa,()=>null,()=>{saves++;return true;})&&saves==1,"missing channel never writes a sent marker or save");
            need(LaoZhouQuestions.CommitFinalApproach(qa,append,()=>{saves++;return true;})&&qa.finalApproachSent&&messages==1,"successful commit saves one invitation");
            need(!LaoZhouQuestions.CommitFinalApproach(qa,append,()=>{saves++;return true;})&&messages==1&&saves==2,"repeat is idempotent");
            need(qa.facts.Count==0&&qa.yyLinkSource=="","authored exception never fabricates player disclosure or YY consent");
            foreach(bool en in new[]{false,true})
            {
                var lines=LaoZhouQuestions.FinalApproach(en);need(lines.Length==2&&!string.IsNullOrWhiteSpace(lines[0]),"authored final invitation is bilingual");
                need(!string.Join(" ",lines).Contains(en?"quiet lately":"话少"),"invitation invents no player activity");
                need(LaoZhouQuestions.FinalPersonaRule(en).Contains(en?"no specific training results":"没有任何具体训练成绩"),"final author context grants no private training telemetry");
            }
            return count;
        }
#if UNITY_EDITOR
        [NUnit.Framework.Test] public static void SourceSemanticsGuideAndFinalContactTransactions()
        {NUnit.Framework.Assert.Greater(Run(s=>UnityEngine.JsonUtility.ToJson(s)),35);}
#endif
    }
}
