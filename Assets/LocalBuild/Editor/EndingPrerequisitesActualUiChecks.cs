#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Actual production host/panels/core/persistence, driven by the shared LCD pointer callback.
// Stage 6/fullOpen, five historical AI lines and two linked dated teaching records are explicit fixtures.
// No fabricated trial/pass/publication receipts, model requests, accelerated night or player-save writes.
public static class EndingPrerequisitesActualUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Key="SidebarLayoutChecks.EndingPrerequisitesActual";
    public static bool Requested=>SessionState.GetBool(Key,false);
    public static void ClearRequest()=>SessionState.EraseBool(Key);
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount {get;private set;}
    public const string Scope="Ending prerequisites actual LCD: explicit stage-6/fullOpen, five historical AI replies and two dated linked teaching fixtures; all six sheets answered through UI and scored by real core; real SaveNow rollback/retry, pending-sheet disk reload, SI scroll/read, source records, three route branches, three-card confirmation, public preview and short natural night controls. No inference, natural historical progression, full 210-second night, letter or return-to-title claim.";

    [MenuItem("Tools/Local Verification/Ending prerequisites actual LCD")]
    public static void Run()
    {
        SessionState.SetBool(Key,true);
        try{BookLibraryUiChecks.Run();}catch{ClearRequest();throw;}
    }
    static T Get<T>(object value,string field)=>(T)value.GetType().GetField(field,F).GetValue(value);
    static void Need(bool ok,string message)
    {AssertionCount++;if(!ok)throw new InvalidOperationException("Ending actual UI: "+message);}
    static void Done(string message){Checks.Add(message);Debug.Log("Ending actual UI: "+message);}
    static IEnumerable<object> Settle(int frames=3)
    {Canvas.ForceUpdateCanvases();while(frames-->0)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerable<object> Wait(double seconds)
    {double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
    static IEnumerable<object> Until(Func<bool> ready,string message,double seconds=8)
    {
        double end=EditorApplication.timeSinceStartup+seconds;
        while(!ready()){Need(EditorApplication.timeSinceStartup<end,message);yield return null;}
        foreach(var f in Settle())yield return f;
    }
    static Button Button(Component root,string name,bool enabled=true)
    {
        var found=root.GetComponentsInChildren<Button>(true).Where(b=>b.name==name&&b.gameObject.activeInHierarchy).ToArray();
        Need(found.Length==1,"one visible button "+name+", found "+found.Length);
        if(enabled)Need(found[0].IsInteractable(),"button is interactable: "+name);
        return found[0];
    }
    static IEnumerable<object> Tap(Component root,string name,Action<RectTransform> click)
    {
        var button=Button(root,name);foreach(var f in Settle())yield return f;
        Need(button.gameObject.activeInHierarchy&&button.IsInteractable(),"button remains live before LCD click: "+name);
        click((RectTransform)button.transform);foreach(var f in Settle())yield return f;
    }
    static string Texts(Component root)=>string.Join("\n",root.GetComponentsInChildren<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy).Select(t=>t.text));
    static XgEndingPreparationState State(XingGuangController ctl)=>ctl.Sim.S.endingPreparation;
    static void EnableDisk(ChapterOneRuntime rt)
    {
        string folder=ChapterOneRuntime.VerificationSaveDirectory;
        Need(!string.IsNullOrEmpty(folder)&&Path.GetFullPath(folder).Contains(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar),"explicit isolated LocalVerification directory");
        Need(Path.GetFullPath(rt.SavePath).StartsWith(Path.GetFullPath(folder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"save writes stay in isolated verification directory");
        rt.useDiskSave=true;
    }
    static ChapterOneSim Fixture(bool english)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=english;
        lab.S.stage=lab.S.stageVision=lab.S.stageSequence=6;lab.S.fullOpen=true;
        lab.S.abilitiesEmerged=Enumerable.Range(1,6).ToList();lab.S.teachingFlow=false;
        lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;
        lab.MarkAfterthought("first.drift");
        // Explicit saved-history fixture; these lines are not model output produced by this test.
        lab.S.chat.Clear();for(int i=0;i<5;i++)lab.S.chat.Add(new XgChatLine{from="ai",text=english?"Historical QA reply "+(i+1)+": I still need to check my answer.":"历史测试回答"+(i+1)+"：我还需要核对自己的答案。",at=i+1});
        lab.LearningClock=()=>new XgLearningTime(20160601,36000);
        lab.RecordTeachingQuestion("中奖后先转账是诈骗吗？","Is a prize that asks for an advance transfer a scam?",true,true,"先转账的中奖短信是诈骗。","A prize requiring advance payment is a scam.","spam",null,"是","是，先转账的中奖短信是诈骗。");
        lab.LearningClock=()=>new XgLearningTime(20160602,39600);
        lab.RecordTeachingQuestion("人用耳朵看东西。","People see with their ears.",false,true,"人用眼睛看。","People see with their eyes.","logic",null,"是","是，人用耳朵看东西。");
        var yy=new YYState{lingguangUnlocked=true};foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);house.S.chatState=JsonUtility.ToJson(yy);
        GameCalendar.Advance(house.S,GameCalendar.DayFor(6,0));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
        return house;
    }
    static void Bind(XingGuangController ctl,ChapterOneSim house)
    {
        ctl.runtime.SetSimulationForTests(house);
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});
        ctl.runtime.useDiskSave=false;ctl.runtime.tickEnabled=false;
        Need(ctl.BoundTo(house)&&ReferenceEquals(ctl.runtime.Sim,house)&&YYChatHub.Instance.BoundTo(house),"real controller and YY bound to exact replacement save");
        Need(ctl.LifeEventSettingsAvailable&&ctl.Host.Compute>0&&string.IsNullOrEmpty(ctl.Host.Blocker),"real readable powered household fixture");
        EnableDisk(ctl.runtime);Need(ctl.SaveNow(),"initial explicit fixture saved through real SaveNow");
    }
    static IEnumerable<object> Show(XgEndingPreparationHost host)
    {
        var parent=PrologueDirector.Desk?.layer;Need(parent!=null,"actual desktop LCD layer exists");
        Need(host.Show(parent),"production ending host opens on actual desktop");
        foreach(var f in Settle(4))yield return f;
    }
    static void Photos()
    {
        var ids=new[]{"trial_netbar","trial_gpubox","trial_westlake","trial_serverroom","trial_drafts"};
        var seen=new HashSet<int>();foreach(string id in ids)
        {
            var texture=Resources.Load<Texture>("LingGuangV05/Media/Photos/"+id);
            Need(texture!=null&&texture.width>1&&texture.height>1,"required actual photo loaded: "+id);
            Need(seen.Add(texture.GetInstanceID()),"distinct photo resource: "+id);
        }
    }
    static IEnumerable<object> SixSheets(XingGuangController ctl,XgEndingPreparationHost host,bool english,Action<RectTransform> click,Action<string> capture)
    {
        string lang=english?"en":"zh";
        for(int q=0;q<6;q++)
        {
            var panel=Get<EndingTrialPanel>(host,"trial");
            var menu=panel.Session.Snapshot.trial.menu.Find(x=>x.question==q);
            Need(panel.Session.Current&&menu.canBegin,"sequential sheet unlocked by actual prior gate: "+q+
                "; visible="+panel.Session.Visible+" current="+panel.Session.Current+" canBegin="+menu.canBegin+
                " snapshotRevision="+panel.Session.Snapshot.revision+" hostRevision="+typeof(XgEndingPreparationHost).GetProperty("Revision",F).GetValue(host)+
                " finalReceipt="+State(ctl).finalPageReceipt+" pending="+JsonUtility.ToJson(State(ctl).pendingTrial));
            foreach(var f in Tap(panel,"Sheet"+q,click))yield return f;
            if(State(ctl)?.pendingTrial?.question!=q)capture("ending-prereq-"+lang+"-begin-failure-"+q+".png");
            Need(State(ctl)?.pendingTrial?.question==q,"actual LCD begins saved sheet "+q+
                "; current="+panel.Session.Current+" failed="+panel.Session.Failed+" code="+Get<string>(host,"lastCode")+
                "; pending="+JsonUtility.ToJson(State(ctl)?.pendingTrial)+"; text="+Texts(panel));
            string attempt=State(ctl).pendingTrial.id,sheetJson=JsonUtility.ToJson(State(ctl).pendingTrial.sheet);
            // Real archive round trip for every pending sheet; answer selection uses the reloaded saved sheet.
            Need(ctl.runtime.SaveSlot(3),"pending sheet disk checkpoint "+q);host.Hide();
            Need(ctl.runtime.LoadArchive(3),"pending sheet reload from actual archive "+q);
            foreach(var f in Show(host))yield return f;
            panel=Get<EndingTrialPanel>(host,"trial");var pending=State(ctl).pendingTrial;
            Need(pending.id==attempt&&JsonUtility.ToJson(pending.sheet)==sheetJson,"reload retains same randomized sheet without reroll "+q);
            var sheet=pending.sheet;var picks=new List<int>();
            for(int i=0;i<sheet.items.Count;i++)
            {
                var item=sheet.items[i];int pick=sheet.mode==XgTrialMode.Mark?(item.wrongLine?1:0):item.answer;picks.Add(pick);
                Need(!panel.Session.Snapshot.trial.submitted,"no score or result before submitting sheet");
                if(q==2)
                {
                    var photo=Get<RawImage>(panel,"photo");Need(photo.gameObject.activeInHierarchy&&photo.texture==Resources.Load<Texture>("LingGuangV05/Media/Photos/"+item.photo),"actual trial photo displayed: "+item.photo);
                    Need(!Texts(panel).Contains(item.photoDesc),"photo answer-bearing description hidden until results");
                    capture("ending-prereq-"+lang+"-"+item.photo+".png");
                }
                if(q==5&&!string.IsNullOrEmpty(item.original))
                {
                    foreach(var f in Tap(panel,"Original",click))yield return f;
                    Need(Get<TMP_Text>(panel,"body").text.Contains(item.original),"actual original-reading control reveals saved source passage");
                }
                foreach(var f in Tap(panel,"Choice"+pick,click))yield return f;
                Need(Get<int[]>(panel,"picks")[i]==pick,"actual option click records original choice index");
                if(i+1<sheet.items.Count)foreach(var f in Tap(panel,"NextItem",click))yield return f;
            }
            if(q==0)
            {
                int calls=0;Action fault=()=>{calls++;throw new IOException("Ending actual UI injected SaveNow snapshot failure");};
                ctl.runtime.Saving+=fault;
                try{foreach(var f in Tap(panel,"Submit",click))yield return f;}
                finally{ctl.runtime.Saving-=fault;}
                Need(calls>0&&State(ctl).pendingTrial?.id==attempt&&!State(ctl).attempts.Any(a=>a.id==attempt),"real failed SaveNow rolls trial result back");
                Need(panel.Session.Failed&&Get<int[]>(panel,"picks").SequenceEqual(picks),"failed UI keeps player choices for retry");
                Need(Texts(panel).Contains(english?"Saving failed":"保存失败"),"localized actual-save failure is visible");
                capture("ending-prereq-"+lang+"-save-failure.png");
            }
            foreach(var f in Tap(panel,"Submit",click))yield return f;
            var result=State(ctl).attempts.Single(a=>a.id==attempt);
            int expected=XgEndingTrialFactory.TrialScore(sheet,picks),stars=0;
            for(int i=0;i<3;i++)if(expected>=sheet.stars[i])stars=i+1;stars=Math.Min(stars,sheet.maxStars);
            Need(State(ctl).pendingTrial==null&&result.score==expected&&result.stars==stars&&stars>0,"core calculates actual saved score and cap for sheet "+q);
            Need(result.picks.SequenceEqual(picks)&&State(ctl).gates.Count==(q+1)/2,"saved picks and paired gate count after sheet "+q);
            Need(panel.Session.Snapshot.trial.submitted&&Texts(panel).Contains(english?"Score: ":"本次得分："),"actual submitted result rendered "+q);
        }
        Done(lang+": six actual UI sheets, every pending sheet disk-reloaded, answer indices read from saved sheet, real core scoring and three paired gates; first submission fault then successful real retry.");
    }
    static IEnumerable<object> FinalAndReview(XingGuangController ctl,XgEndingPreparationHost host,bool english,Action<RectTransform> click,Action<string> capture)
    {
        string lang=english?"en":"zh";var trial=Get<EndingTrialPanel>(host,"trial");
        foreach(var f in Tap(trial,"FinalPage",click))yield return f;
        var input=Get<TMP_InputField>(trial,"finalInput");Need(input.gameObject.activeInHierarchy&&input.interactable,"real final-page input enabled");
        click((RectTransform)input.transform);foreach(var f in Settle())yield return f;
        // TMP input text injection is a test typing fixture; the submit/save control uses the actual LCD pointer.
        string line=english?"Keep checking your answers.":"继续核对你的答案。";input.text=line;
        Need(input.text==line,"final-page TMP input received test text");
        foreach(var f in Tap(trial,"Submit",click))yield return f;
        Need(!string.IsNullOrEmpty(State(ctl).finalPageReceipt)&&State(ctl).playerLastLine==line,"actual final-page click persists line and real receipt");
        var panel=Get<EndingPreparationPanel>(host,"preparation");Need(panel.gameObject.activeInHierarchy&&panel.Session.Current,"host switched to production preparation UI");
        foreach(var f in Tap(panel,"Tab0",click))yield return f;
        var reader=Get<ScrollRect>(panel,"reader");Canvas.ForceUpdateCanvases();reader.StopMovement();
        bool longLetter=reader.content.rect.height>reader.viewport.rect.height+1;
        if(longLetter)Need(!Button(panel,"Primary",false).interactable,"SI read confirmation disabled above bottom");
        // ScrollRect is the real UI read gate. Only its geometry is set; no siViewed/state/receipt is injected.
        reader.verticalNormalizedPosition=0;foreach(var f in Settle(4))yield return f;
        Need(reader.verticalNormalizedPosition<=.02f&&Get<bool>(panel,"siViewed"),"actual SI reader reached bottom and read gate opened");
        capture("ending-prereq-"+lang+"-si-bottom.png");
        foreach(var f in Tap(panel,"Primary",click))yield return f;
        Need(!string.IsNullOrEmpty(State(ctl).siReadReceipt),"SI confirmation persisted by real host action");
        foreach(var f in Tap(panel,"Tab2",click))yield return f;
        foreach(string id in new[]{"scam_sms","learned_wrong"})
        {
            int index=panel.Session.Snapshot.memories.FindIndex(m=>m.id==id);Need(index>=0&&index<4,"fixture-derived memory on first page: "+id);
            foreach(var f in Tap(panel,"Memory"+index,click))yield return f;
            var source=panel.Session.Snapshot.memories[index].sources.Single();
            Need(source.id.StartsWith("answer:",StringComparison.Ordinal)&&source.date.StartsWith("2016-06-0",StringComparison.Ordinal),"memory points to dated actual fixture learning record");
            foreach(var f in Tap(panel,"Source",click))yield return f;
            var sourceView=Get<RectTransform>(host,"sourceView");Need(sourceView!=null&&sourceView.gameObject.activeInHierarchy,"actual source record overlay opens");
            var record=ctl.Sim.LearningRecord(long.Parse(source.id.Substring(7)));
            // Dynamic display normalizes full-width punctuation; English uses the saved English question.
            string shown=Texts(sourceView).Normalize(System.Text.NormalizationForm.FormKC);
            string question=english&&!string.IsNullOrEmpty(record.questionEn)?record.questionEn:record.question;
            Need(shown.Contains(question.Normalize(System.Text.NormalizationForm.FormKC))&&
                shown.Contains(record.playerAnswer.Normalize(System.Text.NormalizationForm.FormKC))&&
                shown.Contains(record.aiAnswer.Normalize(System.Text.NormalizationForm.FormKC)),
                "source overlay shows saved localized question, player answer and recorded AI answer; displayed="+shown);
            capture("ending-prereq-"+lang+"-source-"+id+".png");
            var back=sourceView.GetComponentsInChildren<Button>().Single();click((RectTransform)back.transform);
            foreach(var f in Settle())yield return f;Need(Get<RectTransform>(host,"sourceView")==null,"actual source Back returns to memory panel");
        }
        foreach(var f in Tap(panel,"Primary",click))yield return f;
        Need(!string.IsNullOrEmpty(State(ctl).memoryReviewReceipt)&&State(ctl).reviewedMemoryIds.Contains("scam_sms")&&State(ctl).reviewedMemoryIds.Contains("learned_wrong"),"actual memory review confirms the two inspected sources");
        Done(lang+": actual final-page input/save, real SI ScrollRect bottom/confirm, two source-backed memory cards and original-record Back buttons. Typing/scroll geometry are explicit UI fixtures, not synthetic completion receipts.");
    }
    static IEnumerable<object> Route(XingGuangController ctl,XgEndingPreparationHost host,bool english,string route,Action<RectTransform> click,Action<string> capture)
    {
        var panel=Get<EndingPreparationPanel>(host,"preparation");
        foreach(var f in Tap(panel,"Tab1",click))yield return f;
        int routeIndex=route=="online"?0:route=="public_model"?1:2;
        foreach(var f in Tap(panel,"Route"+routeIndex,click))yield return f;
        Need(State(ctl).route==route&&State(ctl).decisionOwner=="player","actual route button persists "+route);
        foreach(var f in Tap(panel,"Tab2",click))yield return f;
        foreach(string id in new[]{"scam_sms","learned_wrong","shutdown"})
        {
            int index=panel.Session.Snapshot.memories.FindIndex(m=>m.id==id);Need(index>=0&&index<4,"pinnable card row: "+id);
            foreach(var f in Tap(panel,"Pin"+index,click))yield return f;
        }
        Need(string.IsNullOrEmpty(State(ctl).pinReceipt),"selecting three cards does not commit pins");
        foreach(var f in Tap(panel,"Primary",click))yield return f;
        Need(string.IsNullOrEmpty(State(ctl).pinReceipt)&&Get<bool>(panel,"confirming"),"first pin click shows real permanent-pin confirmation");
        foreach(var f in Tap(panel,"Cancel",click))yield return f;
        Need(Get<List<string>>(panel,"chosen").Count==3&&string.IsNullOrEmpty(State(ctl).pinReceipt),"review-again retains three draft pins without committing");
        foreach(var f in Tap(panel,"Primary",click))yield return f;
        foreach(var f in Tap(panel,"Primary",click))yield return f;
        Need(State(ctl).pinnedIds.Count==3&&State(ctl).pinnedIds.Distinct().Count()==3&&State(ctl).pinnedIds.Contains("shutdown")&&!string.IsNullOrEmpty(State(ctl).pinReceipt),"actual second pin confirmation commits exactly three unique cards including shutdown");
        foreach(var f in Tap(panel,"Tab3",click))yield return f;
        string text=Texts(panel);
        Need(!text.Contains("中奖后先转账是诈骗吗")&&!text.Contains("人用耳朵看东西"),"public preview excludes private fixture learning-source text");
        if(route=="local_only")Need(text.Contains(english?"This route publishes nothing":"此路线不发布任何内容"),"local preview truthfully shows no publication");
        else foreach(string field in english?new[]{"Product name","Description","Capabilities","Limitations","Maintenance and license"}:new[]{"产品名","说明","能力","限制","维护与许可"})Need(text.Contains(field),"actual public preview contains "+field);
        capture("ending-prereq-"+(english?"en":"zh")+"-preview-"+route+".png");
        foreach(var f in Tap(panel,"Primary",click))yield return f;
        Need(!XgEpilogueFlow.HasCommittedRelease(ctl.Sim.S)&&Get<bool>(panel,"confirming"),"first release click only shows confirmation");
        foreach(var f in Tap(panel,"Primary",click))yield return f;
        Need(XgEpilogueFlow.HasCommittedRelease(ctl.Sim.S)&&ctl.Sim.S.releaseOutcome.route==route&&!ctl.Sim.S.chapterComplete,"real release save commits selected route but does not complete chapter");
        Done((english?"en":"zh")+": actual "+route+" route, three draft pin selections, cancel/review, permanent-pin double confirmation, privacy preview and real release confirmation/save.");
    }
    static IEnumerable<object> ShortNight(XingGuangController ctl,XgEndingPreparationHost host,bool english,Action<RectTransform> click,Action<string> capture)
    {
        var night=Get<EndingLaunchNightPanel>(host,"night");Need(night.gameObject.activeInHierarchy&&night.Session.Current,"online release shows real night panel");
        foreach(var f in Tap(night,"Start",click))yield return f;
        Need(State(ctl).night!=null&&State(ctl).night.responses.Count==40&&State(ctl).night.phase=="running","real night starts forty authored requests");
        foreach(var f in Wait(.8))yield return f;
        foreach(var f in Tap(night,"Pause",click))yield return f;
        Need(State(ctl).night.paused,"real pause persists");string paused=JsonUtility.ToJson(State(ctl).night);
        foreach(var f in Wait(.7))yield return f;
        Need(JsonUtility.ToJson(State(ctl).night)==paused,"natural paused frames do not advance night state");
        foreach(var f in Tap(night,"Throttle",click))yield return f;Need(State(ctl).night.throttled,"real throttle persists");
        foreach(var f in Tap(night,"Pause",click))yield return f;Need(!State(ctl).night.paused,"real resume persists");
        foreach(var f in Wait(.8))yield return f;
        foreach(var f in Tap(night,"PullPower",click))yield return f;
        // The original shutdown-card decision may finish the current answer before stopping.
        // Wait natural frames through that real rule; never inject elapsed time or a finished phase.
        foreach(var f in Until(()=>State(ctl).night.phase=="power_cut","real power-cut rule finishes current request if required",18))yield return f;
        Need(State(ctl).night.phase=="power_cut"&&night.Session.Snapshot.night.stopped,"actual pull-power control ends short night");
        Need(State(ctl).night.answered<40&&!ctl.Sim.S.chapterComplete,"short night is not claimed as full completion or whole ending");
        capture("ending-prereq-"+(english?"en":"zh")+"-night-power-cut.png");
        Done((english?"en":"zh")+": natural short night with actual start/pause/throttle/resume/power-cut controls; full approximately 210-second night intentionally left for root's separate run.");
    }
    public const string FinalRouteScope="Explicit stage-6/fullOpen and dated historical fixtures; original six UI sheets earn every prerequisite receipt, real pre-route checkpoint, deterministic Ask it branch, actual online publication and natural forty-request night, native reading and failed final save. Independent runner owns successful completion, title return and replay; no natural historical progression or model inference claim.";
    static IEnumerable<object> Drive(IEnumerator routine)
    {
        try{while(routine.MoveNext())yield return routine.Current;}
        finally{(routine as IDisposable)?.Dispose();}
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> actualLCDclick,Action<string> capture,bool finalRouteChecks=false)
    {
        Checks.Clear();AssertionCount=0;BookLibraryUiChecks.PrepareInput();
        if(finalRouteChecks)EndingFinalRouteUiChecks.ResetReport();
        Need(ctl!=null&&ctl.runtime!=null&&actualLCDclick!=null&&capture!=null,"real owner, LCD click and capture callbacks supplied");
        var rt=ctl.runtime;Need(!rt.TestMode&&!rt.useDiskSave&&!rt.tickEnabled,"shared isolated Main lifecycle ready");
        Need(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready,"no active model service");Photos();
        var original=rt.Sim;bool enabled=ctl.enabled,englishBefore=GameText.IsEnglish,disk=rt.useDiskSave,tick=rt.tickEnabled;
        var notifications=DesktopNotifications.Install(rt);bool notificationsEnabled=notifications.enabled;notifications.enabled=false;
        var probe=StoryFocus.Probe;var exclusive=StoryFocus.ExclusiveProbe;StoryFocus.Probe=()=>true;StoryFocus.ExclusiveProbe=()=>false;
        var loans=UnityEngine.Object.FindObjectsByType<ZhouCardLoan>(FindObjectsInactive.Include,FindObjectsSortMode.None);var loanStates=loans.Select(x=>x.enabled).ToArray();
        foreach(var loan in loans){loan.StopAllCoroutines();loan.enabled=false;}ctl.enabled=false;
        var host=XgEndingPreparationHost.Install(ctl);bool hostEnabled=host.enabled;host.enabled=true;
        try
        {
            ctl.Window.CloseWindow();var yy=YYChatHub.Instance;Need(yy?.View!=null,"real YY exists for source-linked fixture projection");
            Get<WindowManager>(yy.View,"window").CloseWindow();
            var brain=ctl.GetComponent<XgBrainWindow>();if(brain!=null&&brain.Visible){actualLCDclick((RectTransform)brain.Window.Find("Title/Close"));foreach(var f in Settle())yield return f;}
            for(int locale=0;locale<(finalRouteChecks?1:2);locale++)
            {
                bool english=locale==1;language(english);host.Hide();Bind(ctl,Fixture(english));
                Need(ctl.Sim.S.stage==6&&ctl.Sim.S.fullOpen&&ctl.Sim.S.chat.Count==5,"explicit stage/fullOpen and five historical-reply fixture loaded");
                Need(ctl.Sim.S.askedLog.Count==2&&ctl.Sim.S.askedLog.All(r=>r.gameDate>=20160601&&r.gameDate<=20160602&&r.teachingNoteId>0&&ctl.Sim.S.teachingNotebook.Any(n=>n.id==r.teachingNoteId&&n.learningRecordId==r.id)),"two explicit historical learning fixtures have valid dates and reciprocal note IDs");
                foreach(var f in Show(host))yield return f;
                foreach(var f in Until(()=>State(ctl)?.memories?.Count==2,"real host discovers two fixture-backed memories"))yield return f;
                foreach(var f in SixSheets(ctl,host,english,actualLCDclick,capture))yield return f;
                foreach(var f in FinalAndReview(ctl,host,english,actualLCDclick,capture))yield return f;
                int checkpoint=english?5:4;Need(rt.SaveSlot(checkpoint),"real pre-route checkpoint saved for independent route branches");
                // Explicit stale action fault injection: old real host callback/token, not a simulated LCD click.
                var panel=Get<EndingPreparationPanel>(host,"preparation");var old=panel.Session.Snapshot;var callback=panel.Session.Callbacks.Execute;
                var request=new EndingUiRequest{owner=old.owner,stateOwner=old.stateOwner,revision=old.revision,action=EndingUiAction.ChooseRoute,value="online"};
                host.Hide();Need(rt.LoadArchive(checkpoint),"real archive load changes owner identity");foreach(var f in Show(host))yield return f;
                string unchanged=JsonUtility.ToJson(State(ctl));EndingUiResult stale=null;callback(request,result=>stale=result);
                Need(stale!=null&&!stale.success&&JsonUtility.ToJson(State(ctl))==unchanged,"old-save host callback rejects stale action without mutating replacement save");
                if(finalRouteChecks)foreach(var f in Drive(EndingFinalRouteUiChecks.AiChoiceFrames(ctl,actualLCDclick,capture)))yield return f;
                foreach(string route in finalRouteChecks?new[]{"online"}:new[]{"online","public_model","local_only"})
                {
                    host.Hide();Need(rt.LoadArchive(checkpoint),"route branch reloads real pre-route disk checkpoint: "+route);foreach(var f in Show(host))yield return f;
                    Need(State(ctl).gates.Count==3&&State(ctl).route==""&&State(ctl).pinReceipt=="","branch retains actually earned trial receipts without fabricated route/pins");
                    foreach(var f in Route(ctl,host,english,route,actualLCDclick,capture))yield return f;
                    if(route=="online")
                    {
                        if(finalRouteChecks)
                        {
                            foreach(var f in Drive(EndingFinalRouteUiChecks.FullNightFrames(ctl,actualLCDclick,capture)))yield return f;
                            foreach(var f in Drive(EndingFinalRouteUiChecks.LetterFailureFrames(ctl,actualLCDclick,capture)))yield return f;
                        }
                        else foreach(var f in ShortNight(ctl,host,english,actualLCDclick,capture))yield return f;
                    }
                }
            }
            if(finalRouteChecks)Checks.AddRange(EndingFinalRouteUiChecks.Checks);
            Done(finalRouteChecks?FinalRouteScope:Scope);Done("Assertions: "+AssertionCount+". Artifacts are actual LCD captures if the parent capture callback succeeds; screenshot quality still needs root review.");
        }
        finally
        {
            // The independent final runner owns teardown; leave the real reading save active for finish.
            if(!finalRouteChecks)
            {
                host.Hide();host.enabled=false;rt.useDiskSave=false;rt.tickEnabled=false;
                rt.SetSimulationForTests(original);typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(rt,new object[]{false});
                language(englishBefore);host.enabled=hostEnabled;ctl.enabled=enabled;rt.useDiskSave=disk;rt.tickEnabled=tick;
                StoryFocus.Probe=probe;StoryFocus.ExclusiveProbe=exclusive;notifications.enabled=notificationsEnabled;
                for(int i=0;i<loans.Length;i++)if(loans[i]!=null)loans[i].enabled=loanStates[i];
            }
        }
    }
}
#endif
