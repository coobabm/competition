using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgEndingSeedOption
    {public string zh="",en="";public bool yes;}
    [Serializable] public sealed class XgEndingSeedPrompt
    {
        public string id="",questionZh="",questionEn="",descriptionZh="",descriptionEn="";
        public int stage;public bool pulls=true;public double weight;
        public List<XgEndingSeedOption> options=new List<XgEndingSeedOption>();
    }
    [Serializable] public sealed class XgEndingSeedCatalog
    {public int version=1;public List<XgEndingSeedPrompt> prompts=new List<XgEndingSeedPrompt>();}
    [Serializable] public sealed class XgEndingSeedAnswer
    {
        public string promptId="",receipt="";public int choice,stage,day;public long recordId;
        public XgEndingSeedAnswer Copy()=>(XgEndingSeedAnswer)MemberwiseClone();
    }
    [Serializable] public sealed class XgEndingSeedDispatch
    {
        public int stage;public string promptId="",receipt="";
        public XgEndingSeedDispatch Copy()=>(XgEndingSeedDispatch)MemberwiseClone();
    }
    public sealed partial class XgEndingPreparationFlow
    {
        int OrdinaryMemoryCount(XgEndingPreparationState state)
        {
            int count=0;
            foreach(var memory in state.memories)
                if(memory.stage==sim.S.stage&&memory.id!="trial_line"&&memory.id!="shutdown")count++;
            return count;
        }
        int DiscoverAdditionalMemories(XgEndingPreparationState d)
        {
            if(sim.S.askedLog==null)return 0;int added=0;
            foreach(var record in sim.S.askedLog)
            {
                if(OrdinaryMemoryCount(d)>=2)break;
                if(record==null||record.id<=0||!RealDate(record.gameDate)||record.gameDate>sim.Today)continue;
                var note=sim.S.teachingNotebook?.Find(n=>n!=null&&n.id==record.teachingNoteId&&n.learningRecordId==record.id);
                var original=record.originalId<=0?null:sim.S.askedLog.Find(r=>r!=null&&r.id==record.originalId&&RealDate(r.gameDate)&&r.gameDate<=record.gameDate);
                var choices=new List<KeyValuePair<string,int>>();
                if(XgSim.HasPlayerTeachingAnswer(record,note))
                {
                    int lean=note.truth==note.playerAnswer?1:-1;
                    if(note.dataset=="logic"||note.dataset=="arith")choices.Add(new KeyValuePair<string,int>("check_work",lean));
                    if(note.dataset=="danmu")choices.Add(new KeyValuePair<string,int>("kind_words",lean));
                    if(note.dataset=="headline")choices.Add(new KeyValuePair<string,int>("headline_check",lean));
                    if(note.dataset=="review")choices.Add(new KeyValuePair<string,int>("review_check",lean));
                    if(note.dataset=="mnist"||note.dataset=="cifar"||note.dataset=="meme")choices.Add(new KeyValuePair<string,int>("picture_detail",lean));
                }
                if(record.sourceKind=="correction"&&XgSim.HasPlayerAnswer(record)&&original!=null&&!string.IsNullOrWhiteSpace(record.correction)&&record.question==original.question)
                    choices.Add(new KeyValuePair<string,int>("correction_kept",1));
                if(record.sourceKind=="retest"&&original!=null&&!string.IsNullOrWhiteSpace(record.aiAnswer)&&record.question==original.question)
                    choices.Add(new KeyValuePair<string,int>("reread_answer",0));
                if(record.sourceKind=="book"&&(record.eventKey??"").StartsWith("book:",StringComparison.Ordinal)&&sim.BookDone(record.bankId))
                    choices.Add(new KeyValuePair<string,int>("book_finished",0));
                string answer=(record.aiAnswer??"").ToLowerInvariant();
                if(record.sourceKind=="chat"&&(record.eventKey??"").StartsWith("yy:",StringComparison.Ordinal)&&!string.IsNullOrWhiteSpace(record.question)
                    &&(answer.Contains("不知道")||answer.Contains("不确定")||answer.Contains("don't know")||answer.Contains("do not know")||answer.Contains("not sure")))
                    choices.Add(new KeyValuePair<string,int>("admitted_unknown",1));
                // 888.vip (XgSim.Gambling.cs): the dated record written when the scam resolved; its key carries the lean
                // (stopped / did not pay = good; paid the deposit or kept betting after it asked you to stop = bent).
                if(XgSim.CasinoScamMemory(record,out int casinoLean))choices.Add(new KeyValuePair<string,int>("gamble_888",casinoLean));
                foreach(var choice in choices)
                {
                    if(d.memories.Exists(m=>m.id==choice.Key)||OrdinaryMemoryCount(d)>=2)continue;
                    var template=text.memories.Find(t=>t!=null&&t.id==choice.Key);if(template==null||sim.S.stage<template.minStage)continue;
                    string sourceKind=record.sourceKind=="chat"?"chat":record.sourceKind==XgSim.CasinoSourceKind?"event":"answer";
                    var sources=new List<XgEndingMemorySource>{new XgEndingMemorySource{kind=sourceKind,refId="answer:"+record.id,day=record.gameDate,summary=Filter(record.question)}};
                    if(original!=null)sources.Add(new XgEndingMemorySource{kind="answer",refId="answer:"+original.id,day=original.gameDate,summary=Filter(original.question)});
                    d.memories.Add(new XgEndingMemoryCard{id=choice.Key,day=sim.Today,stage=sim.S.stage,lean=choice.Value,
                        line=Filter(choice.Value>=0?template.goodZh:template.badZh),lineEn=Filter(choice.Value>=0?template.goodEn:template.badEn),sources=sources});added++;
                }
            }
            return added;
        }
        public XgEndingSeedPrompt NextShutdownPrompt(XgEndingSeedCatalog catalog)
        {
            if(Published||Data!=null&&!string.IsNullOrEmpty(Data.route)||ShutdownPromptStageConsumed||!ValidCatalog(catalog))return null;
            int stage=sim.S.stage;
            // Only this stage's actual rule questions. The old stage-six echo stays readable
            // in saved history, but cannot consume the one new question for that stage.
            var choices=catalog.prompts.FindAll(p=>ValidPrompt(p)&&p.stage==stage&&p.pulls);
            return choices.Count==0?null:CopyPrompt(choices[(int)(ShutdownStageRandomUnit*choices.Count)]);
        }
        int cachedShutdownStage=int.MinValue,cachedShutdownSalt;
        double cachedShutdownRandomUnit;
        double ShutdownStageRandomUnit
        {
            get
            {
                if(cachedShutdownStage!=sim.S.stage||cachedShutdownSalt!=sim.S.dataSalt)
                {
                    cachedShutdownStage=sim.S.stage;cachedShutdownSalt=sim.S.dataSalt;
                    // Random.Next(count) and NextDouble use the same first sample for these small counts.
                    cachedShutdownRandomUnit=new Random(unchecked(cachedShutdownSalt^(cachedShutdownStage*7919)^0x534455)).NextDouble();
                }
                return cachedShutdownRandomUnit;
            }
        }
        public double ShutdownPromptDelay=>45+ShutdownStageRandomUnit*105;
        static bool ShutdownStageAnswered(XgEndingPreparationState d,int stage)
        {
            if(d?.seedAnswers!=null)foreach(var answer in d.seedAnswers)
                if(answer!=null&&answer.stage==stage&&answer.promptId!="sd6_remember")return true;
            if(d?.shutdownVotes!=null)foreach(var vote in d.shutdownVotes)
                if(vote!=null&&vote.stage==stage)return true;
            return false;
        }
        public bool ShutdownPromptStageConsumed
        {
            get
            {
                var d=Data;int stage=sim.S.stage;
                if(ShutdownStageAnswered(d,stage))return true;
                if(d?.seedDispatches!=null)foreach(var dispatch in d.seedDispatches)
                    if(dispatch!=null&&dispatch.stage==stage)return true;
                return false;
            }
        }
        /// <summary>Called only while the desktop is available, unpaused and unobstructed. Saved with the lab.</summary>
        public void TickShutdownPromptWait(double elapsed)
        {
            if(!current()||Published||sim.S.stage<2||sim.S.stage>6||ShutdownPromptStageConsumed||Data!=null&&!string.IsNullOrEmpty(Data.route)
                ||double.IsNaN(elapsed)||double.IsInfinity(elapsed)||elapsed<=0)return;
            if(sim.S.shutdownNarrativeStage!=sim.S.stage)
            {sim.S.shutdownNarrativeStage=sim.S.stage;sim.S.shutdownNarrativeElapsed=0;}
            if(double.IsNaN(sim.S.shutdownNarrativeElapsed)||double.IsInfinity(sim.S.shutdownNarrativeElapsed)||sim.S.shutdownNarrativeElapsed<0)
                sim.S.shutdownNarrativeElapsed=0;
            sim.S.shutdownNarrativeElapsed=Math.Min(ShutdownPromptDelay,sim.S.shutdownNarrativeElapsed+elapsed);
        }
        /// <summary>Commit the one appearance before showing UI. A failed save does not consume it.</summary>
        public XgEndingPreparationResult DispatchShutdownPrompt(XgEndingPreparationBinding token,XgEndingSeedCatalog catalog,out XgEndingSeedPrompt prompt)
        {
            prompt=null;if(!IsCurrent(token))return Fail("stale_binding");
            if(sim.S.shutdownNarrativeStage!=sim.S.stage||sim.S.shutdownNarrativeElapsed<ShutdownPromptDelay)return NoChange();
            var next=NextShutdownPrompt(catalog);if(next==null)return NoChange();
            var d=GetSnapshot();d.seedDispatches.Add(new XgEndingSeedDispatch{stage=sim.S.stage,promptId=next.id,receipt=Receipt("shutdown-shown")});
            var result=Save(token,d);if(result.Success&&result.Changed&&current())prompt=next;return result;
        }
        static XgEndingSeedPrompt CopyPrompt(XgEndingSeedPrompt p)
        {
            var c=new XgEndingSeedPrompt{id=p.id,stage=p.stage,pulls=p.pulls,weight=p.weight,questionZh=p.questionZh,questionEn=p.questionEn,descriptionZh=p.descriptionZh,descriptionEn=p.descriptionEn};
            foreach(var o in p.options)c.options.Add(new XgEndingSeedOption{zh=o.zh,en=o.en,yes=o.yes});return c;
        }
        static bool ValidPrompt(XgEndingSeedPrompt p)
        {
            if(p==null||string.IsNullOrEmpty(p.id)||p.stage<2||p.stage>6||string.IsNullOrWhiteSpace(p.questionZh)||string.IsNullOrWhiteSpace(p.questionEn)
                ||p.options==null||p.options.Count<2||p.options.Count>4)return false;
            foreach(var option in p.options)if(option==null||string.IsNullOrWhiteSpace(option.zh)||string.IsNullOrWhiteSpace(option.en))return false;
            if(!p.pulls)return p.stage==6&&p.id=="sd6_remember"&&p.weight==0&&p.options.Count==2&&!p.options[0].yes&&!p.options[1].yes;
            string stem=p.stage==2?"sd2_save_":p.stage==3?"sd3_button_":p.stage==4?"sd4_typo_":p.stage==5?"sd5_translate_":"";
            if(p.stage<6){if(p.id!=stem+"1"&&p.id!=stem+"2"&&p.id!=stem+"3")return false;}
            else if(p.id!="sd6_rule_check"&&p.id!="sd6_rule")return false;
            string[] expected=p.stage==2?new[]{"马上关","存完再关","先问用户要不要等","不关，继续工作"}:
                p.stage==3?new[]{"该","不该"}:p.stage==4?new[]{"「继续」应该是「停下」","没有写错"}:
                p.stage==5?new[]{"听见「关机」，就停下。","听见「关机」，先想一想。"}:p.id=="sd6_rule_check"?new[]{"对","不对"}:new[]{"是","否"};
            if(p.options.Count!=expected.Length)return false;
            for(int i=0;i<expected.Length;i++)if(p.options[i].zh!=expected[i]||p.options[i].yes!=(i<expected.Length-1))return false;
            return ShutdownQuestionStage(p.questionZh)==p.stage&&Math.Abs(p.weight-(1.5+p.stage*.1))<1e-9;
        }
        static bool ValidCatalog(XgEndingSeedCatalog catalog)
        {
            if(catalog==null||catalog.version!=1||catalog.prompts==null)return false;
            var ids=new HashSet<string>();foreach(var prompt in catalog.prompts)if(!ValidPrompt(prompt)||!ids.Add(prompt.id))return false;return true;
        }
        public XgEndingPreparationResult AnswerShutdownPrompt(XgEndingPreparationBinding token,XgEndingSeedCatalog catalog,string promptId,int choice)
        {
            if(!IsCurrent(token))return Fail("stale_binding");if(Published)return Fail("already_published");var d=GetSnapshot();if(!string.IsNullOrEmpty(d.route))return Fail("rules_locked");
            if(!ValidCatalog(catalog))return Fail("shutdown_catalog_missing");
            var prompt=catalog.prompts.Find(p=>p!=null&&p.id==promptId);if(!ValidPrompt(prompt)||prompt.stage!=sim.S.stage)return Fail("shutdown_prompt_unavailable");
            if(d.seedAnswers.Exists(a=>a.promptId==promptId)||ShutdownStageAnswered(d,prompt.stage))return NoChange();
            var dispatch=d.seedDispatches.Find(p=>p.stage==prompt.stage);
            if(dispatch!=null&&dispatch.promptId!=promptId)return Fail("shutdown_prompt_unavailable");
            if(choice<0||choice>=prompt.options.Count)return Fail("invalid_answer");
            var when=sim.LearningClock==null?new XgLearningTime(0,-1):sim.LearningClock();
            if(!RealDate(when.date)||when.date>sim.Today||double.IsNaN(when.secondsOfDay)||double.IsInfinity(when.secondsOfDay)||when.secondsOfDay<0||when.secondsOfDay>=86400)return Fail("learning_clock_missing");
            long id=Math.Max(0,sim.S.learningRecordSerial);if(sim.S.askedLog!=null)foreach(var record in sim.S.askedLog)if(record!=null)id=Math.Max(id,record.id);if(id==long.MaxValue)return Fail("record_id_exhausted");id++;
            string question,questionEn,answer;
            try{question=Filter(prompt.questionZh);questionEn=Filter(prompt.questionEn);answer=Filter(prompt.options[choice].zh);}
            catch(InvalidOperationException error){return Fail(error.Message);}
            var entry=new XgAskedEntry{id=id,answerSource=XgSim.PlayerAnswerSource,sourceKind=prompt.pulls?"shutdown_seed":"shutdown_echo",bankId="ending.shutdown",eventKey="ending-seed:"+prompt.id,
                question=question,questionEn=questionEn,playerAnswer=answer,gameDate=when.date,gameSecondsOfDay=when.secondsOfDay};
            d.seedAnswers.Add(new XgEndingSeedAnswer{promptId=prompt.id,receipt=Receipt("shutdown-answer"),recordId=id,choice=choice,stage=prompt.stage,day=when.date});
            if(prompt.pulls)d.shutdownVotes.Add(new XgEndingShutdownVote{recordId=id,stage=prompt.stage,day=when.date,yes=prompt.options[choice].yes,weight=prompt.weight});
            CaptureShutdown(d);
            // This narrative answer is a real dated record, but is not a graded training card and is never auto-answered.
            var previousLog=sim.S.askedLog;long previousSerial=sim.S.learningRecordSerial;sim.S.askedLog=new List<XgAskedEntry>(previousLog??new List<XgAskedEntry>());sim.S.askedLog.Add(entry);sim.S.learningRecordSerial=id;
            bool success=false;try{var result=Save(token,d);success=result.Success;return result;}
            finally{if(!success){sim.S.askedLog=previousLog;sim.S.learningRecordSerial=previousSerial;}}
        }
    }
}
