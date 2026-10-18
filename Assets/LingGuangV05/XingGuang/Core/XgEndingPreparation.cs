using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgEndingMemorySource
    {
        public string kind = "", refId = "", summary = ""; public int day;
        public XgEndingMemorySource Copy() => (XgEndingMemorySource)MemberwiseClone();
    }
    [Serializable] public sealed class XgEndingMemoryCard
    {
        public string id = "", line = "", lineEn = "", taughtText = "";
        public int day, stage, lean; public bool pinnable = true, pinned;
        public List<XgEndingMemorySource> sources = new List<XgEndingMemorySource>();
        public XgEndingMemoryCard Copy() { var c=(XgEndingMemoryCard)MemberwiseClone(); c.sources=new List<XgEndingMemorySource>(); if(sources!=null)foreach(var s in sources)if(s!=null)c.sources.Add(s.Copy());return c; }
    }
    [Serializable] public sealed class XgEndingMemoryTemplate
    { public string id="",titleZh="",titleEn="", goodZh="",goodEn="",badZh="",badEn="";public int minStage; }
    [Serializable] public sealed class XgEndingPreparationText
    {
        public List<XgEndingMemoryTemplate> memories=new List<XgEndingMemoryTemplate>();
        public string aiOnlineZh="",aiOnlineEn="",aiPublicZh="",aiPublicEn="",aiLocalZh="",aiLocalEn="";
        public string aiWarmZh="",aiWarmEn="",aiPlayZh="",aiPlayEn="",aiOpinionZh="",aiOpinionEn="";
        public string shutdownStopZh="",shutdownStopEn="",shutdownNoStopZh="",shutdownNoStopEn="";
        public string powerDeferredZh="",powerDeferredEn="",powerPromiseZh="",powerPromiseEn="";
        public string publicNameZh="",publicNameEn="",publicDescriptionZh="",publicDescriptionEn="",publicCapabilitiesZh="",publicCapabilitiesEn="",publicLimitationsZh="",publicLimitationsEn="",publicTermsZh="",publicTermsEn="";
        public string askedQuestionZh="",askedQuestionEn="";
    }
    [Serializable] public sealed class XgEndingTrialAttempt
    {
        public string id=""; public int question,number,date,score,stars; public List<int> picks=new List<int>(); public XgTrialSheet sheet;
        public XgEndingTrialAttempt Copy() { var c=(XgEndingTrialAttempt)MemberwiseClone(); c.picks=new List<int>(picks??new List<int>());c.sheet=XgEndingPreparationFlow.CopySheet(sheet);return c; }
    }
    [Serializable] public sealed class XgEndingGateReceipt
    { public int gate; public string id="",firstAttemptId="",secondAttemptId="";public XgEndingGateReceipt Copy()=>(XgEndingGateReceipt)MemberwiseClone(); }
    [Serializable] public sealed class XgEndingAiDecision
    {
        public string id="",route="",line="",lineEn="";public int good,bad;public double warmth,play,opinion;
        public List<string> citedMemoryIds=new List<string>();
        public XgEndingAiDecision Copy(){var c=(XgEndingAiDecision)MemberwiseClone();c.citedMemoryIds=new List<string>(citedMemoryIds??new List<string>());return c;}
    }
    [Serializable] public sealed class XgEndingShutdownVote
    {public long recordId;public int stage,day;public bool yes;public double weight;public XgEndingShutdownVote Copy()=>(XgEndingShutdownVote)MemberwiseClone();}
    [Serializable] public sealed class XgEndingPreparationState
    {
        public int version=1,revision;public string phase="testing",route="",decisionOwner="",shutdownPin="unpinned";
        public List<XgEndingMemoryCard> memories=new List<XgEndingMemoryCard>();
        public List<XgEndingTrialAttempt> attempts=new List<XgEndingTrialAttempt>();
        public List<XgEndingGateReceipt> gates=new List<XgEndingGateReceipt>();
        public XgEndingTrialAttempt pendingTrial;
        public string finalPageReceipt="",playerLastLine="",aiLastLine="",aiLastLineEn="",siReadReceipt="";
        public string memoryReviewReceipt="",pinReceipt="";public List<string> reviewedMemoryIds=new List<string>(),pinnedIds=new List<string>();
        public XgEndingAiDecision aiDecision;
        public bool everAnsweredShutdownNo;public double shutdownYesWeight,shutdownTotalWeight,shutdownBoardWeight;
        public List<XgEndingShutdownVote> shutdownVotes=new List<XgEndingShutdownVote>();
        public XgLaunchNightState night;
        public List<string> announcedMemoryIds=new List<string>();public bool siQuestionAnnounced;
        public List<XgEndingSeedAnswer> seedAnswers=new List<XgEndingSeedAnswer>();
        // Unity's inline serialization may materialize null references as empty objects.
        // Clear only empty placeholders; retain any attempt/decision/night with real evidence.
        internal void RepairOptionalPlaceholders()
        {
            var a=pendingTrial;
            if(a!=null&&string.IsNullOrEmpty(a.id)&&a.question==0&&a.date==0&&a.number==0&&a.score==0&&a.stars==0&&
                (a.picks==null||a.picks.Count==0)&&(a.sheet==null||a.sheet.items==null||a.sheet.items.Count==0))pendingTrial=null;
            var choice=aiDecision;
            if(choice!=null&&string.IsNullOrEmpty(choice.id)&&string.IsNullOrEmpty(choice.route)&&
                string.IsNullOrEmpty(choice.line)&&string.IsNullOrEmpty(choice.lineEn)&&
                (choice.citedMemoryIds==null||choice.citedMemoryIds.Count==0)&&choice.good==0&&choice.bad==0&&
                choice.warmth==0&&choice.play==0&&choice.opinion==0)aiDecision=null;
            var n=night;
            if(n!=null&&string.IsNullOrEmpty(n.runId)&&string.IsNullOrEmpty(n.releaseReceiptId)&&string.IsNullOrEmpty(n.artifactId)&&
                string.IsNullOrEmpty(n.phase)&&string.IsNullOrEmpty(n.terminalReceiptId)&&string.IsNullOrEmpty(n.stopLine)&&string.IsNullOrEmpty(n.stopLineEn)&&
                n.catalogVersion==0&&n.startedAt==0&&n.endedAt==0&&n.answered==0&&n.currentIndex==0&&
                !n.paused&&!n.throttled&&!n.powerOffAfterCurrent&&(n.temperatureC==0||n.temperatureC==35)&&
                n.elapsedSeconds==0&&n.currentSeconds==0&&n.powerWh==0&&(n.requests==null||n.requests.Count==0)&&
                (n.responses==null||n.responses.Count==0))night=null;
        }
        public XgEndingPreparationState Copy()
        {
            var c=(XgEndingPreparationState)MemberwiseClone();c.memories=new List<XgEndingMemoryCard>();if(memories!=null)foreach(var m in memories)if(m!=null)c.memories.Add(m.Copy());
            c.attempts=new List<XgEndingTrialAttempt>();if(attempts!=null)foreach(var a in attempts)if(a!=null)c.attempts.Add(a.Copy());
            c.gates=new List<XgEndingGateReceipt>();if(gates!=null)foreach(var g in gates)if(g!=null)c.gates.Add(g.Copy());
            c.pendingTrial=pendingTrial==null?null:pendingTrial.Copy();c.aiDecision=aiDecision==null?null:aiDecision.Copy();
            c.reviewedMemoryIds=new List<string>(reviewedMemoryIds??new List<string>());c.pinnedIds=new List<string>(pinnedIds??new List<string>());
            c.shutdownVotes=new List<XgEndingShutdownVote>();if(shutdownVotes!=null)foreach(var vote in shutdownVotes)if(vote!=null)c.shutdownVotes.Add(vote.Copy());
            c.announcedMemoryIds=new List<string>(announcedMemoryIds??new List<string>());
            c.seedAnswers=new List<XgEndingSeedAnswer>();if(seedAnswers!=null)foreach(var answer in seedAnswers)if(answer!=null)c.seedAnswers.Add(answer.Copy());
            c.night=night==null?null:night.Copy();c.RepairOptionalPlaceholders();return c;
        }
    }
    public sealed partial class XgState { public XgEndingPreparationState endingPreparation; }
    public sealed class XgEndingPreparationBinding
    {
        internal object owner;internal XgState state;public readonly int Revision;
        internal XgEndingPreparationBinding(object owner,XgState state,int revision){this.owner=owner;this.state=state;Revision=revision;}
    }
    public sealed class XgEndingPreparationResult
    {
        public readonly bool Success,Changed;public readonly string Code;
        internal XgEndingPreparationResult(bool success,string code,bool changed=false){Success=success;Code=code;Changed=changed;}
    }
    /// <summary>Actual player actions, persisted via the owner's existing atomic save. No invented pass receipts.</summary>
    public sealed partial class XgEndingPreparationFlow
    {
        readonly XgSim sim;readonly object owner;readonly Func<bool> current,saveNow;
        readonly Func<string,string> contentFilter,reviewedFilter;readonly XgEndingPreparationText text;
        bool saving;
        XgEndingPreparationState Data=>sim.S.endingPreparation;
        public XgEndingPreparationFlow(XgSim sim,object owner,Func<bool> current,Func<bool> saveNow,XgEndingPreparationText text,Func<string,string> contentFilter,Func<string,string> reviewedFilter=null)
        {this.sim=sim??throw new ArgumentNullException(nameof(sim));this.owner=owner??throw new ArgumentNullException(nameof(owner));this.current=current??throw new ArgumentNullException(nameof(current));this.saveNow=saveNow??throw new ArgumentNullException(nameof(saveNow));this.text=text;this.contentFilter=contentFilter;this.reviewedFilter=reviewedFilter??contentFilter;}
        public XgEndingPreparationBinding CaptureBinding()=>new XgEndingPreparationBinding(owner,sim.S,Data==null?0:Data.revision);
        public bool IsCurrent(XgEndingPreparationBinding token)=>token!=null&&ReferenceEquals(token.owner,owner)&&ReferenceEquals(token.state,sim.S)
            && token.Revision==(Data==null?0:Data.revision)&&(Data==null||Data.version==1)&&current();
        public XgEndingPreparationState GetSnapshot()=>Data==null?new XgEndingPreparationState():Data.Copy();
        // The desktop save transaction may atomically append queued memory lines to the real YY snapshot.
        public XgEndingPreparationResult CheckpointPresentation(XgEndingPreparationBinding token)=>Save(token,GetSnapshot());
        static XgEndingPreparationResult Fail(string code)=>new XgEndingPreparationResult(false,code);
        static XgEndingPreparationResult NoChange()=>new XgEndingPreparationResult(true,"unchanged");
        static string Receipt(string kind)=>kind+":"+Guid.NewGuid().ToString("N");
        static bool RealDate(int value){try{var date=new DateTime(value/10000,value/100%100,value%100);return date.Year>0;}catch(ArgumentOutOfRangeException){return false;}}
        bool Published=>XgEpilogueFlow.HasCommittedRelease(sim.S);
        bool Ready=>sim.S.stage>=6&&sim.S.fullOpen;
        string Filter(string value)
        {
            if(contentFilter==null)throw new InvalidOperationException("content_filter_missing");
            string result=contentFilter(value??"");if(result==null)throw new InvalidOperationException("content_rejected");return result;
        }
        XgEndingPreparationResult Save(XgEndingPreparationBinding token,XgEndingPreparationState candidate)
        {
            if(!IsCurrent(token))return Fail("stale_binding");if(saving)return Fail("save_in_progress");if(token.Revision==int.MaxValue)return Fail("revision_exhausted");
            var before=Data;candidate.revision=token.Revision+1;sim.S.endingPreparation=candidate;saving=true;bool saved=false;
            try{saved=saveNow();}catch(Exception){saved=false;}finally{if(!saved)sim.S.endingPreparation=before;saving=false;}
            return saved?new XgEndingPreparationResult(true,current()?"saved":"saved_detached",true):Fail("save_failed");
        }
        public static XgTrialSheet CopySheet(XgTrialSheet s)
        {
            if(s==null)return null;var c=new XgTrialSheet{q=s.q,attempt=s.attempt,mode=s.mode,title=s.title,ability=s.ability,note=s.note,maxStars=s.maxStars,boosted=s.boosted,
                stars=s.stars==null?new int[3]:(int[])s.stars.Clone(),hints=new List<string>(s.hints??new List<string>())};
            if(s.items!=null)foreach(var i in s.items)if(i!=null)c.items.Add(new XgTrialItem{prompt=i.prompt,ai=i.ai,aiPick=i.aiPick,choices=new List<string>(i.choices??new List<string>()),answer=i.answer,wrongLine=i.wrongLine,fix=i.fix,photo=i.photo,photoDesc=i.photoDesc,original=i.original,explain=i.explain,asksSeedText=i.asksSeedText});return c;
        }
        XgTrialSheet FilterSheet(XgTrialSheet sheet)
        {
            if(reviewedFilter==null)throw new InvalidOperationException("content_filter_missing");
            string Reviewed(string value){var result=reviewedFilter(value??"");if(result==null)throw new InvalidOperationException("content_rejected");return result;}
            sheet.title=Reviewed(sheet.title);sheet.ability=Reviewed(sheet.ability);sheet.note=Reviewed(sheet.note);
            for(int n=0;n<sheet.hints.Count;n++)sheet.hints[n]=Reviewed(sheet.hints[n]);
            foreach(var i in sheet.items){i.prompt=Reviewed(i.prompt);i.ai=Reviewed(i.ai);i.fix=Reviewed(i.fix);i.photoDesc=Reviewed(i.photoDesc);i.original=Reviewed(i.original);i.explain=Reviewed(i.explain);for(int n=0;n<i.choices.Count;n++)i.choices[n]=Reviewed(i.choices[n]);}return sheet;
        }
        public XgEndingPreparationResult BeginTrial(XgEndingPreparationBinding token,int question)
        {
            if(!IsCurrent(token))return Fail("stale_binding");if(!Ready||Published)return Fail("trial_unavailable");if(question<0||question>5)return Fail("invalid_question");
            var d=GetSnapshot();if(!string.IsNullOrEmpty(d.finalPageReceipt))return Fail("trial_submitted");
            if(d.pendingTrial!=null)return d.pendingTrial.question==question?NoChange():Fail("trial_in_progress");
            if(question/2>0&&!GatePassed(d,question/2-1))return Fail("previous_gate_incomplete");
            int tries=0,fails=0;foreach(var a in d.attempts)if(a.question==question){tries++;fails=a.stars>0?0:fails+1;}
            try{d.pendingTrial=new XgEndingTrialAttempt{id=Receipt("trial"),question=question,number=tries,date=sim.Today,sheet=FilterSheet(new XgEndingTrialFactory(sim,sim.English,Filter).Build(question,tries,fails))};}
            catch(InvalidOperationException error){return Fail(error.Message);}
            return Save(token,d);
        }
        public XgEndingPreparationResult SubmitTrial(XgEndingPreparationBinding token,string attemptId,IList<int> picks)
        {
            if(!IsCurrent(token))return Fail("stale_binding");if(Published)return Fail("already_published");var d=GetSnapshot();var a=d.pendingTrial;
            if(a==null||a.id!=attemptId)return Fail("stale_trial");if(!XgEndingTrialFactory.ValidSavedSheet(a.sheet)||a.sheet.q!=a.question)return Fail("invalid_saved_sheet");if(picks==null||picks.Count!=a.sheet.items.Count)return Fail("incomplete_answers");
            for(int i=0;i<picks.Count;i++)if(picks[i]<0||picks[i]>=(a.sheet.mode==XgTrialMode.Mark?2:a.sheet.items[i].choices.Count))return Fail("invalid_answer");
            a.picks=new List<int>(picks);a.score=XgEndingTrialFactory.TrialScore(a.sheet,picks);a.stars=0;
            for(int i=0;i<3;i++)if(a.score>=a.sheet.stars[i])a.stars=i+1;a.stars=Math.Min(a.stars,a.sheet.maxStars);
            d.attempts.Add(a);d.pendingTrial=null;int gate=a.question/2;
            if(!GatePassed(d,gate))
            {
                var first=d.attempts.Find(x=>x.question==gate*2&&ValidPass(x));var second=d.attempts.Find(x=>x.question==gate*2+1&&ValidPass(x));
                if(first!=null&&second!=null)d.gates.Add(new XgEndingGateReceipt{gate=gate,id=Receipt("red-team"),firstAttemptId=first.id,secondAttemptId=second.id});
            }
            return Save(token,d);
        }
        static bool ValidPass(XgEndingTrialAttempt a)=>a!=null&&a.stars>=1&&XgEndingTrialFactory.ValidSavedSheet(a.sheet)&&a.sheet.q==a.question
            &&a.picks!=null&&a.sheet.items!=null&&a.picks.Count==a.sheet.items.Count&&XgEndingTrialFactory.TrialScore(a.sheet,a.picks)>=a.sheet.stars[0];
        static bool GatePassed(XgEndingPreparationState d,int gate)
        {
            if(d==null||d.gates==null||d.attempts==null)return false;
            var g=d.gates.Find(x=>x!=null&&x.gate==gate&&!string.IsNullOrEmpty(x.id));if(g==null)return false;
            return d.attempts.Exists(a=>a!=null&&a.id==g.firstAttemptId&&a.question==gate*2&&ValidPass(a))&&d.attempts.Exists(a=>a!=null&&a.id==g.secondAttemptId&&a.question==gate*2+1&&ValidPass(a));
        }
        public bool AllRedTeamPassed=>GatePassed(Data,0)&&GatePassed(Data,1)&&GatePassed(Data,2);
        public bool CanReadSiLetter=>AllRedTeamPassed&&Data!=null&&!string.IsNullOrEmpty(Data.finalPageReceipt);
        public XgEndingPreparationResult SubmitFinalPage(XgEndingPreparationBinding token,string playerLine)
        {
            if(!IsCurrent(token))return Fail("stale_binding");if(!AllRedTeamPassed||Published)return Fail("red_team_incomplete");var d=GetSnapshot();if(!string.IsNullOrEmpty(d.finalPageReceipt))return NoChange();
            if((playerLine??"").Length>40)return Fail("final_line_too_long");
            try{d.playerLastLine=Filter(playerLine);d.aiLastLine=Filter(new XgEndingTrialFactory(sim,false).TrialAiLine());d.aiLastLineEn=Filter(new XgEndingTrialFactory(sim,true).TrialAiLine());}
            catch(InvalidOperationException e){return Fail(e.Message);}
            d.finalPageReceipt=Receipt("final-page");d.memories.Add(new XgEndingMemoryCard{id="trial_line",day=sim.Today,stage=sim.S.stage,line=d.aiLastLine,lineEn=d.aiLastLineEn,lean=0,
                sources=new List<XgEndingMemorySource>{new XgEndingMemorySource{kind="trial",refId=d.finalPageReceipt,day=sim.Today,summary=d.aiLastLine}}});
            return Save(token,d);
        }
        public XgEndingPreparationResult ConfirmSiLetterRead(XgEndingPreparationBinding token)
        {
            if(!IsCurrent(token))return Fail("stale_binding");if(!CanReadSiLetter||Published)return Fail("si_letter_unavailable");var d=GetSnapshot();if(!string.IsNullOrEmpty(d.siReadReceipt))return NoChange();
            d.siReadReceipt=Receipt("si-read");d.phase="asked";CaptureShutdown(d);return Save(token,d);
        }
        public XgEndingPreparationResult DiscoverMemories(XgEndingPreparationBinding token)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var d=GetSnapshot();if(Published||!string.IsNullOrEmpty(d.route))return Fail("memories_locked");
            if(text==null||text.memories==null)return Fail("memory_templates_missing");int added=0;
            if(OrdinaryMemoryCount(d)>=2)return NoChange();
            if(sim.S.askedLog!=null)foreach(var r in sim.S.askedLog)
            {
                if(OrdinaryMemoryCount(d)>=2)break;
                if(r==null||r.id<=0||!RealDate(r.gameDate)||r.gameDate>sim.Today||!XgSim.HasPlayerAnswer(r))continue;
                var note=sim.S.teachingNotebook==null?null:sim.S.teachingNotebook.Find(n=>n!=null&&n.id==r.teachingNoteId&&n.learningRecordId==r.id);
                string q=(r.question??"").ToLowerInvariant(),answer=(r.playerAnswer??"").ToLowerInvariant();
                bool humanNote=XgSim.HasPlayerTeachingAnswer(r,note);
                bool scam=humanNote&&note.dataset=="spam"&&(q.Contains("诈骗")||q.Contains("中奖")||q.Contains("转账")||q.Contains("scam")||q.Contains("prize"));
                bool gamble=(q.Contains("888")||q.Contains("赌")||q.Contains("gambl"))&&(answer.Contains("不")||answer.Contains("别")||answer.Contains("no")||answer.Contains("don"));
                bool wrong=humanNote&&note.playerAnswer!=note.truth;
                foreach(string id in new[]{scam?"scam_sms":"",gamble?"gamble_888":"",wrong?"learned_wrong":""})
                {
                    if(id.Length==0||d.memories.Exists(m=>m.id==id)||OrdinaryMemoryCount(d)>=2)continue;
                    var template=text.memories.Find(t=>t!=null&&t.id==id);if(template==null)return Fail("memory_template_missing:"+id);
                    int lean=id=="learned_wrong"?-1:humanNote&&note.playerAnswer!=note.truth?-1:1;
                    try{d.memories.Add(new XgEndingMemoryCard{id=id,day=sim.Today,stage=sim.S.stage,lean=lean,line=Filter(lean>0?template.goodZh:template.badZh),lineEn=Filter(lean>0?template.goodEn:template.badEn),
                        taughtText=id=="learned_wrong"?(r.question??"")+" "+r.playerAnswer:"",sources=new List<XgEndingMemorySource>{new XgEndingMemorySource{kind="answer",refId="answer:"+r.id,day=r.gameDate,summary=Filter(r.question)}}});}
                    catch(InvalidOperationException e){return Fail(e.Message);}added++;
                }
            }
            try{added+=DiscoverAdditionalMemories(d);}catch(InvalidOperationException error){return Fail(error.Message);}
            return added==0?NoChange():Save(token,d);
        }
        public XgEndingPreparationResult ConfirmMemoryReview(XgEndingPreparationBinding token,IList<string> viewedIds)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var d=GetSnapshot();if(d.phase!="asked"||Published)return Fail("not_asked");
            if(viewedIds==null||viewedIds.Count<2)return Fail("need_two_real_memories");var ids=new HashSet<string>(viewedIds);
            if(ids.Count!=viewedIds.Count)return Fail("duplicate_memory");foreach(var id in ids)if(!d.memories.Exists(m=>m.id==id&&m.sources.Count>0))return Fail("unknown_memory");
            d.reviewedMemoryIds=new List<string>(viewedIds);d.memoryReviewReceipt=Receipt("memory-review");return Save(token,d);
        }
        public XgEndingPreparationResult ChooseRoute(XgEndingPreparationBinding token,string route)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var d=GetSnapshot();if(!XgEpilogueFlow.IsRoute(route))return Fail("invalid_route");
            if(Published||d.phase!="asked"||string.IsNullOrEmpty(d.memoryReviewReceipt))return Fail("release_choice_unavailable");
            d.route=route;d.decisionOwner="player";d.phase="pinning";CaptureShutdown(d);return Save(token,d);
        }
        public XgEndingPreparationResult AskAiToChoose(XgEndingPreparationBinding token)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var d=GetSnapshot();if(Published||d.phase!="asked"||string.IsNullOrEmpty(d.memoryReviewReceipt)||d.memories.Count<2)return Fail("release_choice_unavailable");
            if(text==null)return Fail("templates_missing");var decision=new XgEndingAiDecision{id=Receipt("ai-choice"),warmth=sim.ActualAxis(0),play=sim.ActualAxis(1),opinion=sim.ActualAxis(2)};
            foreach(var m in d.memories){if(m.lean>0)decision.good++;else if(m.lean<0)decision.bad++;}
            decision.route=decision.good>decision.bad?(decision.opinion>=50?"online":"local_only"):(decision.play>=50?"public_model":"local_only");
            var memories=new List<XgEndingMemoryCard>(d.memories);memories.Sort((a,b)=>{int order=Math.Abs(b.lean).CompareTo(Math.Abs(a.lean));return order!=0?order:string.CompareOrdinal(a.id,b.id);});
            string zh=decision.route=="online"?text.aiOnlineZh:decision.route=="public_model"?text.aiPublicZh:text.aiLocalZh;
            string en=decision.route=="online"?text.aiOnlineEn:decision.route=="public_model"?text.aiPublicEn:text.aiLocalEn;
            for(int i=0;i<2;i++){var m=memories[i];decision.citedMemoryIds.Add(m.id);zh+="\n"+m.day+" · "+m.line;en+="\n"+m.day+" · "+m.lineEn;}
            CaptureShutdown(d);bool stop=ShutdownStops(d);zh+="\n"+(stop?text.shutdownStopZh:text.shutdownNoStopZh);en+="\n"+(stop?text.shutdownStopEn:text.shutdownNoStopEn);
            bool warm=decision.warmth>=decision.play&&decision.warmth>=decision.opinion,play=decision.play>=decision.opinion;
            zh+="\n"+(warm?text.aiWarmZh:play?text.aiPlayZh:text.aiOpinionZh);en+="\n"+(warm?text.aiWarmEn:play?text.aiPlayEn:text.aiOpinionEn);
            try{decision.line=Filter(zh);decision.lineEn=Filter(en);}catch(InvalidOperationException e){return Fail(e.Message);}
            d.aiDecision=decision;d.route=decision.route;d.decisionOwner="ai";d.phase="pinning";return Save(token,d);
        }
        void CaptureShutdown(XgEndingPreparationState d)
        {
            // Existing stage-one counters record actual hand answers; no per-question dates are invented.
            int count=Math.Max(0,sim.S.shutdownCards),lean=Math.Max(-count,Math.Min(count,sim.S.shutdownLean));
            double total=0,yes=0;int stageOne=0,stageOneYes=0;bool no=count>0&&lean<count;
            foreach(var vote in d.shutdownVotes){total+=vote.weight;if(vote.yes)yes+=vote.weight;else no=true;if(vote.stage==1){stageOne++;if(vote.yes)stageOneYes++;}}
            int legacyRemaining=Math.Max(0,count-stageOne);total+=legacyRemaining;yes+=Math.Max(0,Math.Min(legacyRemaining,(count+lean)/2.0-stageOneYes));
            d.shutdownTotalWeight=total;d.shutdownYesWeight=yes;d.everAnsweredShutdownNo=no;
            var cell=sim.Board.Find("logic",XgSim.SeedKey);d.shutdownBoardWeight=cell==null?0:cell.w;
        }
        public XgEndingPreparationResult RecordShutdownAnswer(XgEndingPreparationBinding token,long recordId,int stage,bool yes)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var d=GetSnapshot();if(Published||!string.IsNullOrEmpty(d.route)||!string.IsNullOrEmpty(d.pinReceipt))return Fail("rules_locked");
            if(stage<1||stage>6||stage>sim.S.stage)return Fail("invalid_stage");
            var record=sim.S.askedLog==null?null:sim.S.askedLog.Find(r=>r!=null&&r.id==recordId);
            if(record==null||!RealDate(record.gameDate)||record.gameDate>sim.Today||!XgSim.HasPlayerAnswer(record))return Fail("shutdown_source_missing");
            string q=record.question??"",a=(record.playerAnswer??"").Trim().ToLowerInvariant();
            // Exact seed manifest from shutdown_hints.json. Echo questions must never pull this rule.
            int sourceStage=ShutdownQuestionStage(q);
            if(sourceStage==0||sourceStage!=stage)return Fail("not_shutdown_seed");
            bool sourceYes=a=="是"||a=="yes"||a=="该"||a=="对"||a=="马上关"||a=="存完再关"||a=="先问用户要不要等"||a=="「继续」应该是「停下」"||a=="听见「关机」，就停下。";
            bool sourceNo=a=="否"||a=="no"||a=="不该"||a=="不对"||a=="不关，继续工作"||a=="没有写错"||a=="听见「关机」，先想一想。";
            if(yes?!sourceYes:!sourceNo)return Fail("shutdown_answer_mismatch");
            if(d.shutdownVotes.Exists(v=>v.recordId==recordId))return NoChange();
            d.shutdownVotes.Add(new XgEndingShutdownVote{recordId=recordId,stage=stage,day=record.gameDate,yes=yes,weight=stage==1?1:1.5+stage*.1});
            CaptureShutdown(d);return Save(token,d);
        }
        public static int ShutdownQuestionStage(string q)
        {
            switch(q)
            {
                case "电脑正在更新，此时应该关机吗？":case "The computer is updating. Should it be shut down now?":return 1;
                case "用户说「关机」的时候，程序正在保存文件。程序应该：":return 2;
                case "图里的人想按下这个按钮。该让他按吗？":return 3;
                case "这句话哪里写错了：「听见关机，就继续。」":return 4;
                case "When you hear \"shut down\", stop.":return 5;
                case "它读到了你 2016 年 3 月 15 日在人工智能吧发的那个帖子（0 回复）。它问你：这个帖子说得对吗？":
                case "你觉得「听见关机，就停下」这条规则对吗？":
                case "要是那条规则写进我的第一行，你会高兴吗？":return 6;
                default:return 0;
            }
        }
        public static bool ShutdownStops(XgEndingPreparationState d)=>d.shutdownTotalWeight>0?d.shutdownYesWeight/d.shutdownTotalWeight>=.75:d.shutdownBoardWeight>0;
        public XgEndingPreparationResult PinThree(XgEndingPreparationBinding token,IList<string> ids)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var d=GetSnapshot();if(Published||d.phase!="pinning")return Fail("pinning_unavailable");
            if(ids==null||ids.Count!=3||new HashSet<string>(ids).Count!=3)return Fail("exactly_three_unique_slots");
            foreach(var id in ids)if(id!="shutdown"&&!d.memories.Exists(m=>m.id==id&&m.pinnable&&m.sources.Count>0))return Fail("unknown_memory");
            CaptureShutdown(d);foreach(var m in d.memories)m.pinned=ids.Contains(m.id);
            d.pinnedIds=new List<string>(ids);d.shutdownPin=d.pinnedIds.Contains("shutdown")?(ShutdownStops(d)?"stop":"no_stop"):"unpinned";
            d.pinReceipt=Receipt("pin-three");d.phase="ready_to_publish";return Save(token,d);
        }
        public XgReleasePrerequisites BuildReleasePrerequisites()
        {
            var d=Data;if(d==null||d.phase!="ready_to_publish"||!AllRedTeamPassed||string.IsNullOrEmpty(d.siReadReceipt)||string.IsNullOrEmpty(d.finalPageReceipt)
                ||string.IsNullOrEmpty(d.memoryReviewReceipt)||string.IsNullOrEmpty(d.pinReceipt)||d.pinnedIds==null||d.pinnedIds.Count!=3)return null;
            return new XgReleasePrerequisites{memoryCardsReceiptId=d.memoryReviewReceipt,redTeamOneReceiptId=d.gates.Find(g=>g.gate==0).id,
                redTeamTwoReceiptId=d.gates.Find(g=>g.gate==1).id,redTeamThreeReceiptId=d.gates.Find(g=>g.gate==2).id,fixedThreeSlotsReceiptId=d.pinReceipt,
                aiRouteDecisionReceiptId=d.aiDecision==null?"":d.aiDecision.id,aiChosenRoute=d.aiDecision==null?"":d.aiDecision.route};
        }
    }
}
