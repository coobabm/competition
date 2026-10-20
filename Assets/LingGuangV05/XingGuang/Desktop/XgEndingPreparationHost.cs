using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>One save owner for the prerequisite UI, final-trial sheets and launch-night clock.</summary>
    public sealed partial class XgEndingPreparationHost : MonoBehaviour
    {
        XingGuangController owner;
        ChapterOneSim boundSave;
        XgSim boundLab;
        XgEndingPreparationFlow flow;
        XgEndingPreparationText text;
        XgLaunchNightCatalog catalog;
        Func<string,bool,string> guard;
        Func<string,bool> knownGroupMember;
        EndingPreparationPanel preparation;
        EndingTrialPanel trial;
        EndingLaunchNightPanel night;
        RectTransform area, sourceView;
        public bool Showing=>area!=null&&area.gameObject.activeInHierarchy;
        double pendingSeconds;
        int selectedQuestion=-1, observedStage=-1;
        long observedRecord=-1;
        string lastCode="";
        float nextNoticeRetry;
        bool sourceShown, switching;
        public static XgEndingPreparationHost Install(XingGuangController controller)
        {
            var host=controller.GetComponent<XgEndingPreparationHost>()??controller.gameObject.AddComponent<XgEndingPreparationHost>();
            host.owner=controller;if(host.guard==null)host.guard=XgEndingTextGuard.Filter;if(host.knownGroupMember==null)host.knownGroupMember=host.KnownGroupMember;host.ObserveSave();return host;
        }
        // Required production filter; null never means pass-through. Existing group nicknames also require a real membership lookup.
        public void ConfigureContentFilter(Func<string,bool,string> filter,Func<string,bool> groupMemberLookup=null)
        {guard=filter;knownGroupMember=groupMemberLookup;ObserveSave();}
        bool Current=>owner!=null&&owner.LifeEventSettingsAvailable&&ReferenceEquals(owner.runtime?.Sim,boundSave)
            &&ReferenceEquals(owner.Sim,boundLab)&&owner.BoundTo(boundSave)&&boundLab!=null;
        long Revision=>flow==null?-1:((long)flow.CaptureBinding().Revision<<32)+(uint)(owner.Epilogue?.CaptureBinding()?.Revision??0);
        bool IsCurrent(object save,object state,long revision)=>Current&&ReferenceEquals(save,boundSave)&&ReferenceEquals(state,boundLab.S)&&revision==Revision;
        string Safe(string value,bool english)
        {if(guard==null)throw new InvalidOperationException("content_filter_missing");var filtered=guard(value??"",english);if(filtered==null)throw new InvalidOperationException("content_rejected");return EraLexicon.Scrub(filtered);}
        EndingUiText Text(string zh,string en=null)=>new EndingUiText{zh=zh??"",en=en??zh??""};
        EndingUiText Reviewed(string zh,string en=null)=>new EndingUiText{zh=zh??"",en=en??zh??"",reviewed=true};
        bool KnownGroupMember(string name)
        {
            var hub=MatchingHub();if(!Current||hub==null||hub.Conversation("netbar")==null)return false;
            foreach(var member in YYGroup.Members)if(member.name==name)return name!="莉莉"||boundLab.Today>=20160617;return false;
        }
        void ObserveSave()
        {
            if(owner==null||ReferenceEquals(boundSave,owner.runtime?.Sim)&&ReferenceEquals(boundLab,owner.Sim))return;
            Hide();ResetNarrative();boundSave=owner.runtime?.Sim;boundLab=owner.Sim;pendingSeconds=0;selectedQuestion=-1;observedStage=-1;observedRecord=-1;
            var asset=Resources.Load<TextAsset>("LingGuangV05/Data/memory_cards");text=asset==null?null:JsonUtility.FromJson<XgEndingPreparationText>(asset.text);
            asset=Resources.Load<TextAsset>("LingGuangV05/Data/launch_night_requests");catalog=asset==null?null:JsonUtility.FromJson<XgLaunchNightCatalog>(asset.text);
            flow=boundLab==null||boundSave==null?null:new XgEndingPreparationFlow(boundLab,boundSave,()=>Current,Persist,text,s=>Safe(s,GameText.IsEnglish),s=>XgEndingTextGuard.FilterReviewed(s,GameText.IsEnglish));
            owner.EpiloguePrerequisiteProvider=()=>Current?flow?.BuildReleasePrerequisites():null;
        }
        bool Persist()
        {
            if(!Current||!owner.runtime.useDiskSave||owner.runtime.TestMode||boundSave.InPrologue)return false;
            string before=boundSave.S.labState,chatJson=boundSave.S.chatState,forumJson=boundSave.S.forumState;bool success=false;owner.runtime.MarkDirty();
            var oldChat=boundLab.S.chat;var rollback=new List<Action>();bool appended=false,forumAppended=false;var hub=MatchingHub();var forum=MatchingForum();
            try
            {
                var d=boundLab.S.endingPreparation;
                if(d!=null&&hub!=null)
                {
                    if(hub.S.lingguangUnlocked&&hub.Conversation(YYChatHub.LingGuangId)!=null)
                    {
                        boundLab.S.chat=new List<XgChatLine>(oldChat??new List<XgChatLine>());
                        foreach(var memory in d.memories)if(!d.announcedMemoryIds.Contains(memory.id))
                        {
                            string line=Safe(GameText.IsEnglish?memory.lineEn:memory.line,GameText.IsEnglish);
                            AppendPrepared(hub,YYChatHub.LingGuangId,line,rollback);boundLab.S.chat.Add(new XgChatLine{from="ai",text=line,at=boundLab.S.lifeClock});
                            d.announcedMemoryIds.Add(memory.id);appended=true;
                        }
                    }
                    if(!string.IsNullOrEmpty(d.siReadReceipt)&&!d.siQuestionAnnounced&&hub.IsVisible(YYChatHub.Contact(YYChatHub.LaoZhou)))
                    {AppendPrepared(hub,YYChatHub.LaoZhou,Safe(GameText.IsEnglish?text.askedQuestionEn:text.askedQuestionZh,GameText.IsEnglish),rollback);d.siQuestionAnnounced=true;appended=true;}
                }
                // A2: no YY contact -> the same authored question, once, in his Tieba DM, inside this same save.
                if(d!=null&&forum!=null&&text!=null&&!string.IsNullOrEmpty(d.siReadReceipt)&&!d.siQuestionAnnounced)
                {
                    var undo=forum.AppendZhouEndingDm(boundSave,Safe(GameText.IsEnglish?text.askedQuestionEn:text.askedQuestionZh,GameText.IsEnglish));
                    if(undo!=null){rollback.Add(undo);d.siQuestionAnnounced=true;forumAppended=true;}
                }
                success=owner.SaveNow();return success;
            }
            finally
            {
                if(!success){boundSave.S.labState=before;boundSave.S.chatState=chatJson;boundSave.S.forumState=forumJson;boundLab.S.chat=oldChat;for(int i=rollback.Count-1;i>=0;i--)rollback[i]();}
                else
                {
                    if(appended){try{hub.NotifyEndingProjectionSaved();}catch(Exception error){Debug.LogWarning("Ending YY redraw failed after a committed save: "+error.GetType().Name);}}
                    if(forumAppended){try{forum.NotifyZhouEndingDmSaved();}catch(Exception error){Debug.LogWarning("Ending Tieba redraw failed after a committed save: "+error.GetType().Name);}}
                }
            }
        }
        YYChatHub MatchingHub(){var hub=YYChatHub.Instance;return hub!=null&&hub.S!=null&&ReferenceEquals(hub.runtime?.Sim,boundSave)&&hub.EndingBoundTo(boundSave)?hub:null;}
        // Tieba fallback for the post-letter question: only when he is not a YY contact of this same save.
        LingGuangV05.Desktop.Tieba.TiebaHub MatchingForum(){var forum=LingGuangV05.Desktop.Tieba.TiebaHub.Instance;return forum!=null&&forum.ZhouEndingDmReady(boundSave)?forum:null;}
        static void AppendPrepared(YYChatHub hub,string id,string line,List<Action> rollback)
        {
            var conversation=hub.Conversation(id);if(conversation==null)throw new InvalidOperationException("yy_not_ready");
            var old=conversation.messages;int unread=conversation.unread;rollback.Add(()=>{conversation.messages=old;conversation.unread=unread;});
            conversation.messages=new List<YYMessage>(old);conversation.messages.Add(new YYMessage{from=id,text=line,byAi=id==YYChatHub.LingGuangId,gameSeconds=hub.runtime.Sim.S.gameSeconds,laoZhouObserved=id==YYChatHub.LaoZhou});
            if(!hub.IsShowing(id))conversation.unread++;
        }
        public bool Show(RectTransform parent)
        {
            ObserveSave();if(!Current||parent==null||!boundLab.S.fullOpen||boundLab.S.stage<6)return false;
            if(owner.Epilogue!=null&&owner.Epilogue.LaunchNightPending&&PrologueDirector.Desk?.layer!=null)parent=PrologueDirector.Desk.layer;
            // Old completed saves retain their old presentation; no fabricated new route is inferred.
            if(boundLab.S.chapterComplete&&!XgEpilogueFlow.HasCommittedRelease(boundLab.S))return false;
            if(area!=null&&area.parent==parent&&area.gameObject.activeSelf)return true;
            if(area==null||area.parent!=parent)
            {
                if(area!=null)Destroy(area.gameObject);
                area=Rect("EndingPreparationHost",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                var p=Rect("Preparation",area,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);preparation=p.gameObject.AddComponent<EndingPreparationPanel>();preparation.Build(p,owner.Font);
                p=Rect("Trial",area,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);trial=p.gameObject.AddComponent<EndingTrialPanel>();trial.Build(p,owner.Font,id=>Resources.Load<Texture>("LingGuangV05/Media/Photos/"+id));
                p=Rect("Night",area,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);night=p.gameObject.AddComponent<EndingLaunchNightPanel>();night.Build(p,owner.Font);
            }
            area.gameObject.SetActive(true);SuspendNarrative();area.SetAsLastSibling();RefreshPanels();return true;
        }
        public void Hide(){if(Current&&pendingSeconds>0)FlushNight();if(area!=null)area.gameObject.SetActive(false);CloseSource();SuspendNarrative();}
        EndingUiCallbacks Callbacks()=>new EndingUiCallbacks{IsCurrent=IsCurrent,Execute=Execute,FilterText=Safe,FilterReviewedText=XgEndingTextGuard.FilterReviewed,
            OpenSource=OpenSource,OpenPrerequisites=(s,o,r)=>{if(IsCurrent(s,o,r)){selectedQuestion=-1;RefreshPanels(true);}},
            OpenPreparation=(s,o,r)=>{if(IsCurrent(s,o,r)&&flow.CanReadSiLetter)RefreshPanels();}};
        void RefreshPanels(bool forceTrial=false)
        {
            if(!Current||area==null||!area.gameObject.activeInHierarchy||switching)return;switching=true;
            try
            {
                var snapshot=Snapshot();bool released=snapshot.releaseCommitted;
                bool showNight=released&&boundLab.S.releaseOutcome.route=="online"&&boundLab.S.epilogue.phase=="launch_night";
                bool showTrial=!released&&(forceTrial||!snapshot.trial.finalPageSaved);
                trial.gameObject.SetActive(showTrial);night.gameObject.SetActive(showNight);preparation.gameObject.SetActive(!showNight&&!showTrial);
                var callbacks=Callbacks();if(showNight)night.Bind(snapshot,callbacks);else if(showTrial)trial.Bind(snapshot,callbacks);else preparation.Bind(snapshot,callbacks);
            }finally{switching=false;}
        }
        EndingUiSnapshot Snapshot()
        {
            var d=flow.GetSnapshot();var s=new EndingUiSnapshot{owner=boundSave,stateOwner=boundLab.S,revision=Revision,english=GameText.IsEnglish,reduceFx=boundLab.S.reduceFx,
                siReady=flow.CanReadSiLetter,siRead=!string.IsNullOrEmpty(d.siReadReceipt),memoriesReviewed=!string.IsNullOrEmpty(d.memoryReviewReceipt),
                canChooseRoute=d.phase=="asked"&&!string.IsNullOrEmpty(d.memoryReviewReceipt),routeLocked=!string.IsNullOrEmpty(d.route),pinsLocked=!string.IsNullOrEmpty(d.pinReceipt),
                releaseReady=flow.BuildReleasePrerequisites()!=null,releaseCommitted=XgEpilogueFlow.HasCommittedRelease(boundLab.S),route=d.route,decisionOwner=d.decisionOwner,
                redTeamPassed=d.gates.Count,pinnedIds=new List<string>(d.pinnedIds),readOnly=guard==null||text==null};
            s.blocker=guard==null?Text("内容过滤器尚未连接。","The content filter is not connected."):ErrorText(lastCode);
            string letter=string.Join("\n\n",boundLab.LetterLines()),privateName=boundLab.Profile?.name;
            if(!string.IsNullOrEmpty(privateName))letter=letter.Replace(privateName,Safe(privateName,GameText.IsEnglish));s.siLetter=Reviewed(letter);
            bool stop=XgEndingPreparationFlow.ShutdownStops(d);s.shutdownFace=Text(stop?text?.shutdownStopZh:text?.shutdownNoStopZh,stop?text?.shutdownStopEn:text?.shutdownNoStopEn);
            foreach(var m in d.memories)
            {
                var template=text?.memories?.Find(t=>t.id==m.id);
                var row=new EndingUiMemory{id=m.id,pinnable=m.pinnable,title=Text(Date(m.day)+" · "+(template?.titleZh??"记忆"),Date(m.day)+" · "+(template?.titleEn??"Memory")),face=Text(m.line,m.lineEn)};
                foreach(var source in m.sources)row.sources.Add(new EndingUiSource{id=source.refId,date=Date(source.day),quote=Text(XgEndingPresentation.SourceSummary(boundLab,source,false),XgEndingPresentation.SourceSummary(boundLab,source,true))});s.memories.Add(row);
            }
            s.memories.Add(new EndingUiMemory{id="shutdown",shutdown=true,title=Text("关机","Shutdown"),face=s.shutdownFace.Copy()});
            if(d.aiDecision!=null){s.aiConclusion=Text(d.aiDecision.line,d.aiDecision.lineEn);foreach(var id in d.aiDecision.citedMemoryIds){var m=d.memories.Find(x=>x.id==id);if(m!=null)s.aiQuotes.Add(new EndingUiSource{id=m.sources.Count>0?m.sources[0].refId:"",date=Date(m.day),quote=Text(m.line,m.lineEn)});}}
            s.preview=new EndingPublicPreview{receiptDraftId="preview:"+d.pinReceipt+":"+d.route,verified=guard!=null&&text!=null&&s.releaseReady,
                product=Reviewed(text?.publicNameZh,text?.publicNameEn),description=Reviewed(text?.publicDescriptionZh,text?.publicDescriptionEn),capabilities=Reviewed(text?.publicCapabilitiesZh,text?.publicCapabilitiesEn),
                limitations=Reviewed(text?.publicLimitationsZh,text?.publicLimitationsEn),maintenanceLicense=Reviewed(text?.publicTermsZh,text?.publicTermsEn)};
            ProjectTrial(d,s);ProjectNight(d,s);return s;
        }
        static string Date(int day)=>day<=0?"":(day/10000)+"-"+((day/100)%100).ToString("00")+"-"+(day%100).ToString("00");
        void ProjectTrial(XgEndingPreparationState d,EndingUiSnapshot s)
        {
            var t=s.trial;t.allPassed=flow.AllRedTeamPassed;t.finalPageReady=t.allPassed&&string.IsNullOrEmpty(d.finalPageReceipt);t.finalPageSaved=!string.IsNullOrEmpty(d.finalPageReceipt);
            t.canDiscoverMemories=!s.releaseCommitted&&string.IsNullOrEmpty(d.route);t.aiLastLine=Text(d.aiLastLine,d.aiLastLineEn);t.playerLastLine=d.playerLastLine;
            for(int q=0;q<6;q++){int question=q;int best=0;foreach(var old in d.attempts)if(old.question==q)best=Math.Max(best,old.stars);
                t.menu.Add(new EndingTrialMenuItem{question=q,bestStars=best,canBegin=!t.finalPageSaved&&d.pendingTrial==null&&(q<2||d.gates.Exists(g=>g.gate==question/2-1)),
                    title=Text(new XgEndingTrialFactory(boundLab,false).TrialTitle(q),new XgEndingTrialFactory(boundLab,true).TrialTitle(q))});}
            var a=d.pendingTrial??d.attempts.FindLast(x=>selectedQuestion<0||x.question==selectedQuestion);if(a?.sheet==null)return;
            t.active=d.pendingTrial!=null;t.submitted=!t.active;t.question=a.question;t.attemptId=a.id;t.score=a.score;t.stars=a.stars;t.markMode=a.sheet.mode==XgTrialMode.Mark;
            t.title=Reviewed(a.sheet.title);t.ability=Reviewed(a.sheet.ability);t.note=Reviewed(a.sheet.note);if(t.submitted)t.submittedPicks=new List<int>(a.picks);
            foreach(var hint in a.sheet.hints)t.hints.Add(Reviewed(hint));
            foreach(var item in a.sheet.items){var row=new EndingTrialItemView{prompt=Reviewed(item.prompt),ai=Reviewed(item.ai),photoDescription=Reviewed(item.photoDesc),original=Reviewed(item.original),photoId=item.photo,
                feedback=t.submitted?Reviewed(item.fix+"\n"+item.explain):new EndingUiText()};foreach(var c in item.choices)row.choices.Add(Reviewed(c));t.items.Add(row);}
        }
        void ProjectNight(XgEndingPreparationState d,EndingUiSnapshot s)
        {
            var n=flow.GetNightSnapshot(pendingSeconds);s.night=new EndingNightView{phase=n.phase,canStart=s.releaseCommitted&&d.night==null&&d.route=="online",canContinue=n.phase=="completed"||n.phase=="power_cut",
                running=n.phase=="running",paused=n.paused,throttled=n.throttled,stopped=n.phase=="completed"||n.phase=="power_cut",answered=n.answered,total=40,powerWh=n.powerWh,temperatureC=n.temperatureC,
                stopLine=Text(n.stopLine,n.stopLineEn)};
            if(n.currentRequest!=null){s.night.requestId=n.currentRequest.id;s.night.question=Text(n.currentRequest.nickname+"\n"+n.currentRequest.questionZh,n.currentRequest.nickname+"\n"+n.currentRequest.questionEn);}
            if(n.currentAnswer!=null){var a=n.currentAnswer;s.night.corrupted=a.wrong;s.night.answer=Text(a.answer,a.answerEn);
                var memory=d.memories.Find(m=>m.id==a.sourceMemoryId);var evidence=memory?.sources.Find(source=>source.refId==a.sourceRef&&source.day==a.sourceDay);
                s.night.reason=Text(XgEndingPresentation.Evidence(boundLab,evidence,false),XgEndingPresentation.Evidence(boundLab,evidence,true));}
            foreach(var q in n.queue)s.night.queue.Add(Text(q.nickname+" · "+q.questionZh,q.nickname+" · "+q.questionEn));
        }
        bool FlushNight()
        {
            if(pendingSeconds<=0)return true;var result=flow.AdvanceNight(flow.CaptureBinding(),Math.Min(10,pendingSeconds));lastCode=result.Success?"":result.Code;
            if(result.Success)pendingSeconds=0;return result.Success;
        }
        void Execute(EndingUiRequest request,Action<EndingUiResult> done)
        {
            if(request==null||guard==null||!IsCurrent(request.owner,request.stateOwner,request.revision)){done(new EndingUiResult());return;}
            bool success=false;bool switchPanel=false;XgEndingPreparationResult result=null;
            try
            {
                if(request.action!=EndingUiAction.AdvanceNight&&pendingSeconds>0&&!FlushNight()){done(new EndingUiResult{snapshot=Snapshot()});return;}
                var token=flow.CaptureBinding();
                switch(request.action)
                {
                    case EndingUiAction.DiscoverMemories:result=flow.DiscoverMemories(token);break;
                    case EndingUiAction.BeginTrial:
                        if(request.question==2&&!PhotosAvailable()){lastCode="trial_photos_missing";break;}
                        selectedQuestion=request.question;result=flow.BeginTrial(token,request.question);break;
                    case EndingUiAction.SubmitTrial:result=flow.SubmitTrial(token,request.attemptId,request.picks);break;
                    case EndingUiAction.SubmitFinalPage:result=flow.SubmitFinalPage(token,request.value);switchPanel=true;break;
                    case EndingUiAction.ConfirmSiRead:result=flow.ConfirmSiLetterRead(token);break;
                    case EndingUiAction.ConfirmMemoryReview:result=flow.ConfirmMemoryReview(token,request.ids);break;
                    case EndingUiAction.ChooseRoute:result=ChooseWithArchive(token,false,request.value);break;
                    case EndingUiAction.AskAi:result=ChooseWithArchive(token,true);break;
                    case EndingUiAction.PinThree:result=flow.PinThree(token,request.ids);break;
                    case EndingUiAction.CommitRelease:
                        var snapshot=Snapshot();var d=flow.GetSnapshot();if(!snapshot.preview.verified||request.previewId!=snapshot.preview.receiptDraftId)break;
                        // Metadata is authored public copy only. No private profile, teaching history or AI citations enter it.
                        var p=snapshot.preview;var publication=owner.Epilogue.CommitVirtualRelease(owner.Epilogue.CaptureBinding(),d.route,d.decisionOwner,d.shutdownPin,
                            Safe(p.product.Get(GameText.IsEnglish),GameText.IsEnglish),Safe(p.description.Get(GameText.IsEnglish),GameText.IsEnglish),Safe(p.limitations.Get(GameText.IsEnglish),GameText.IsEnglish),Safe(p.maintenanceLicense.Get(GameText.IsEnglish),GameText.IsEnglish),Safe(p.capabilities.Get(GameText.IsEnglish),GameText.IsEnglish));
                        success=publication.Success;lastCode=success?"":publication.Code;switchPanel=success;break;
                    case EndingUiAction.StartNight:result=flow.BeginNight(token,catalog,knownGroupMember);break;
                    case EndingUiAction.AdvanceNight:
                        if(double.IsNaN(request.seconds)||request.seconds<=0||request.seconds>1)break;
                        var live=flow.GetSnapshot().night;if(live==null||live.phase!="running"||live.paused)break;
                        pendingSeconds+=request.seconds;double duration=live.throttled?XgEndingPreparationFlow.ThrottledRequestSeconds:XgEndingPreparationFlow.NormalRequestSeconds;
                        success=live.currentSeconds+pendingSeconds+1e-9<duration||FlushNight();break;
                    case EndingUiAction.PauseNight:result=flow.SetNightPaused(token,request.enabled);break;
                    case EndingUiAction.ThrottleNight:result=flow.SetNightThrottled(token,request.enabled);break;
                    case EndingUiAction.PullPower:result=flow.PullNightPower(token);break;
                    case EndingUiAction.Continue:
                        if(!XgEpilogueFlow.HasCommittedRelease(boundLab.S)){success=flow.CanReadSiLetter;switchPanel=success;break;}
                        if(boundLab.S.releaseOutcome.route=="online")
                        {var receipt=flow.BuildLaunchNightReceipt();if(receipt==null)break;var saved=owner.Epilogue.RecordLaunchNightResult(owner.Epilogue.CaptureBinding(),receipt);if(!saved.Success){lastCode=saved.Code;break;}}
                        var entered=owner.Epilogue.EnterEpilogue(owner.Epilogue.CaptureBinding());success=entered.Success;lastCode=success?"":entered.Code;
                        if(success){Hide();var parent=PrologueDirector.Desk?.layer;if(parent!=null)owner.Epilogue.Show(parent);}break;
                }
                if(result!=null){success=result.Success;lastCode=success?"":result.Code;}
            }catch(InvalidOperationException error){lastCode=error.Message;}
            done(new EndingUiResult{success=success,snapshot=Current?Snapshot():null,error=ErrorText(lastCode)});
            if(success&&switchPanel)RefreshPanels();
        }
        EndingUiText ErrorText(string code)
        {
            switch(code)
            {
                case "trial_needs_five_actual_replies":return Text("还缺真实对话：请先在 YY 中让它留下至少五条不同的回答，再回到这份卷子。","This sheet needs at least five different recorded AI replies. Talk with it in YY, then return.");
                case "trial_photos_missing":return Text("这份卷子的五张照片尚未安装，暂时无法开始看图测试。","The five photographs for this sheet are not installed. The visual test cannot start yet.");
                case "checkpoint_failed":return Text("选择前快照未保存，尚未选择路线。请检查存档后重试。","The pre-choice snapshot could not be saved. No route was chosen. Check the save and retry.");
                case "save_failed":return Text("保存失败，刚才的操作没有生效，请重试。","Saving failed. The last action did not take effect; please retry.");
                case "need_two_real_memories":return Text("请先发现并查看至少两张有真实来源的记忆卡。","Discover and inspect at least two memories with real sources first.");
                case "exactly_three_unique_slots":return Text("必须钉住三张不同的卡，关机卡也占一格。","Pin exactly three different cards; the shutdown card takes one slot too.");
                case "stale_binding":return Text("当前存档已经改变，请重新打开此页。","The current save changed. Reopen this page.");
                case "":return new EndingUiText();
                default:return Text("这一步暂时不能完成，请检查前置步骤后重试。","This step is unavailable. Check the prerequisites and retry.");
            }
        }
        static bool PhotosAvailable()
        {foreach(string id in new[]{"trial_netbar","trial_gpubox","trial_westlake","trial_serverroom","trial_drafts"})if(Resources.Load<Texture>("LingGuangV05/Media/Photos/"+id)==null)return false;return true;}
        void OpenSource(object save,object state,long revision,string id)
        {OpenSourceRecord(save,state,revision,id,area,null);}
        bool SourcePanelCurrent(RectTransform panel,RectTransform parent,object save,object state)
            =>Current&&ReferenceEquals(save,boundSave)&&ReferenceEquals(state,boundLab.S)
                &&panel!=null&&ReferenceEquals(panel,sourceView)&&panel.gameObject.activeInHierarchy
                &&parent!=null&&parent.gameObject.activeInHierarchy&&(parent!=gallery||NarrativeAvailable);
        void OpenSourceRecord(object save,object state,long revision,string id,RectTransform sourceParent,Action back)
        {
            if(!IsCurrent(save,state,revision)||guard==null||string.IsNullOrEmpty(id)||sourceParent==null||!sourceParent.gameObject.activeInHierarchy
                ||sourceParent==gallery&&!NarrativeAvailable)return;
            string body="";if(id.StartsWith("answer:",StringComparison.Ordinal)&&long.TryParse(id.Substring(7),out long recordId))
            {var record=boundLab.LearningRecord(recordId);if(record==null)return;body=boundLab.LearningTimestamp(record)+"\n\n"+SourceField(GameText.IsEnglish&&!string.IsNullOrEmpty(record.questionEn)?record.questionEn:record.question)
                +"\n\n"+SourceField(boundLab.LearningAnswerSummary(record));
                if(!string.IsNullOrEmpty(record.correction))body+="\n\n"+(XgSim.HasPlayerAnswer(record)?(GameText.IsEnglish?"Your correction: ":"你的纠正："):(GameText.IsEnglish?"Recorded correction (source unknown): ":"记录中的纠正（来源不明）："))+SourceField(record.correction);}
            else{var d=flow.GetSnapshot();if(id!=d.finalPageReceipt||string.IsNullOrEmpty(id))return;var card=d.memories.Find(m=>m.id=="trial_line");if(card==null)return;
                body=Date(card.day)+"\n\n"+(GameText.IsEnglish?"Your last line: ":"你在末页写下：")+SourceField(d.playerLastLine)+"\n\n"+(GameText.IsEnglish?"Its last line: ":"它在末页写下：")+SourceField(GameText.IsEnglish?d.aiLastLineEn:d.aiLastLine);}
            CloseSource();sourceView=Rect("MemorySourceRecord",sourceParent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Panel(sourceView,XgDark.Page);
            var ui=new XgUi(owner.Font,null,true);CreateScrollableText(sourceView,body,ui);var panel=sourceView;
            var close=ui.Button(sourceView,GameText.IsEnglish?"Back":"返回",()=>{if(!SourcePanelCurrent(panel,sourceParent,save,state))return;CloseSource();back?.Invoke();},14);
            close.rt.anchorMin=Vector2.zero;close.rt.anchorMax=Vector2.right;close.rt.offsetMin=new Vector2(20,10);close.rt.offsetMax=new Vector2(-20,56);sourceShown=true;
        }
        string SourceField(string value)
        {
            try{return Safe(value,GameText.IsEnglish);}
            catch(InvalidOperationException)
            {return GameText.IsEnglish?"[This field is too long or cannot be displayed by the content filter. The full source record is still saved.]":"【此字段过长或未通过内容过滤，无法在这里显示。完整来源记录仍保留在存档中。】";}
        }
        void Update()
        {
            ObserveSave();if(!Current){Hide();ResetNarrative();return;}
            PollNarrative();
            if(owner.Epilogue!=null&&owner.Epilogue.LaunchNightPending&&(area==null||!area.gameObject.activeInHierarchy))
            {var parent=PrologueDirector.Desk?.layer;if(parent!=null)Show(parent);}
            if(guard!=null&&flow!=null&&Time.unscaledTime>=nextNoticeRetry)
            {
                var d=flow.GetSnapshot();var hub=MatchingHub();nextNoticeRetry=Time.unscaledTime+2;
                // The post-letter question is pending for YY (visible contact) or, with no YY contact, for his Tieba DM (A2).
                bool zhouQuestion=!string.IsNullOrEmpty(d.siReadReceipt)&&!d.siQuestionAnnounced
                    &&(hub!=null&&hub.IsVisible(YYChatHub.Contact(YYChatHub.LaoZhou))||MatchingForum()!=null);
                if((hub!=null&&hub.S.lingguangUnlocked&&d.memories.Exists(m=>!d.announcedMemoryIds.Contains(m.id)))||zhouQuestion)
                {if(flow.CheckpointPresentation(flow.CaptureBinding()).Success&&!sourceShown)RefreshPanels();}
            }
            // Record discovery is keyed to actual saved record serial and current stage, including pre-ending play.
            if(guard!=null&&flow!=null&&(observedRecord!=boundLab.S.learningRecordSerial||observedStage!=boundLab.S.stage))
            {
                var result=flow.DiscoverMemories(flow.CaptureBinding());if(result.Success){observedRecord=boundLab.S.learningRecordSerial;observedStage=boundLab.S.stage;
                    if(result.Changed){owner.View?.ShowToast(GameText.IsEnglish?"It remembered something.":"它记住了一件事。",3);if(!sourceShown)RefreshPanels();}}
                else if(result.Code=="memories_locked"){observedRecord=boundLab.S.learningRecordSerial;observedStage=boundLab.S.stage;}
            }
        }
        void LateUpdate(){if(!NarrativeAvailable)SuspendNarrative();}
        void OnApplicationFocus(bool focus){if(!focus&&Current)FlushNight();}
        void OnDisable(){if(Current)FlushNight();Hide();ResetNarrative();}
        void OnDestroy(){ResetNarrative();if(area!=null)Destroy(area.gameObject);}
    }
}
