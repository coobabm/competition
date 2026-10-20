#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Prepared QA only. Run in the isolated Main fixture; every choice goes through FilesWindowUiChecks.Click.
public static class LaoZhouQuestionsUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const string Key="SidebarLayoutChecks.LaoZhouOnly";
    const string SetupKey="LaoZhouQuestionsUiChecks.Setup";
    [Serializable] sealed class SceneEntry {public string path;public bool loaded,active;}
    [Serializable] sealed class EditorSetup
    {public List<SceneEntry> scenes=new List<SceneEntry>();public string save,desktop;public int rate,vsync;}
    static Mouse addedMouse;
    static Keyboard addedKeyboard;
    public static bool Requested=>SessionState.GetBool(Key,false);
    public static void ClearRequest()=>SessionState.EraseBool(Key);
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount { get; private set; }
    [MenuItem("Tools/Local Verification/Lao Zhou questions actual LCD")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in clean EditMode");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene edits before scene-switching QA");
        var setup=new EditorSetup{save=ChapterOneRuntime.VerificationSaveDirectory,desktop=(string)typeof(LingGuangV05.Desktop.OS.DreamOSDataManager).GetField("dataPath",BindingFlags.Static|BindingFlags.NonPublic)?.GetValue(null),rate=Application.targetFrameRate,vsync=QualitySettings.vSyncCount};
        setup.scenes=EditorSceneManager.GetSceneManagerSetup().Select(x=>new SceneEntry{path=x.path,loaded=x.isLoaded,active=x.isActive}).ToList();
        SessionState.SetString(SetupKey,JsonUtility.ToJson(setup));SessionState.SetBool(Key,true);
        try{SidebarLayoutChecks.Run();}
        catch
        {
            ClearRequest();
            try{RestoreEditor();}
            finally{if(Application.isBatchMode)EditorApplication.Exit(1);}
            throw;
        }
    }
    public static void PrepareInput()
    {
        if(!Requested)return;
        Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
        if(Mouse.current==null){addedMouse=InputSystem.AddDevice<Mouse>("LaoZhouQaMouse");addedMouse.MakeCurrent();}
        if(Keyboard.current==null){addedKeyboard=InputSystem.AddDevice<Keyboard>("LaoZhouQaKeyboard");addedKeyboard.MakeCurrent();}
    }
    public static void CleanupInput()
    {
        if(addedMouse!=null&&addedMouse.added)InputSystem.RemoveDevice(addedMouse);
        if(addedKeyboard!=null&&addedKeyboard.added)InputSystem.RemoveDevice(addedKeyboard);
        addedMouse=null;addedKeyboard=null;
    }
    public static void RestoreEditor()
    {
        string json=SessionState.GetString(SetupKey,"");if(json.Length==0)return;
        SessionState.EraseString(SetupKey);ClearRequest();
        var setup=JsonUtility.FromJson<EditorSetup>(json);
        ChapterOneRuntime.VerificationSaveDirectory=string.IsNullOrEmpty(setup.save)?null:setup.save;
        typeof(LingGuangV05.Desktop.OS.DreamOSDataManager).GetField("dataPath",BindingFlags.Static|BindingFlags.NonPublic)?.SetValue(null,string.IsNullOrEmpty(setup.desktop)?null:setup.desktop);
        Application.targetFrameRate=setup.rate;QualitySettings.vSyncCount=setup.vsync;
        var scenes=setup.scenes.Where(x=>!string.IsNullOrEmpty(x.path)).Select(x=>new SceneSetup{path=x.path,isLoaded=x.loaded,isActive=x.active}).ToArray();
        if(scenes.Length>0)EditorSceneManager.RestoreSceneManagerSetup(scenes);
        else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    }
    static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,F).GetValue(o);
    static void Need(bool value,string label){AssertionCount++;if(!value)throw new InvalidOperationException("LZ1 UI: "+label);}
    static void Done(string label){Checks.Add(label);Debug.Log("LZ1 UI: "+label);}
    static string Label(Button b)=>b.GetComponentInChildren<TMP_Text>(true)?.text??"";
    static IEnumerable<object> FramesFor(double seconds){double until=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<until)yield return null;}
    static IEnumerable<object> Until(Func<bool> ready,string label,double seconds=8)
    {double until=EditorApplication.timeSinceStartup+seconds;while(!ready()){if(EditorApplication.timeSinceStartup>=until)Need(false,label+" | "+IdleDiagnostic());yield return null;}Canvas.ForceUpdateCanvases();yield return null;yield return null;}
    static string IdleDiagnostic()
    {
        var ctl=XingGuangController.Instance;var lab=ctl?.Sim;var view=ctl?.View;var yy=YYChatHub.Instance;
        return "focus="+StoryFocus.Active+", voice="+InnerVoice.Busy+", joining="+AiJoinsYy.Playing+", epiphany="+AutoLabelEpiphany.Playing
            +", stage="+lab?.S.stage+", reading="+(lab?.ReadingBook!=null)+", relic="+lab?.RelicPickPending+", setup="+view?.SetupShowing
            +", holo="+(view?.Holo!=null&&view.Holo.Showing)+", typing="+yy?.TypingShown+", choices="+yy?.ChoicesFor
            +", epochs="+lab?.S.stageEpochs+", psStep="+lab?.S.psStep+", ynStep="+lab?.S.ynStep+", spider="+XgResidentSpider.IntroPlaying
            +", girlfriendStarted="+yy?.S.girlfriend?.started+", girlfriendAffection="+yy?.S.girlfriend?.affection+" | "+VoiceDiagnostic();
    }
    static string VoiceDiagnostic()
    {
        var voice=typeof(InnerVoice).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic)?.GetValue(null);
        string hold;
        try{hold=(InnerVoice.Hold!=null&&InnerVoice.Hold()).ToString();}catch(Exception e){hold=e.GetType().Name+": "+e.Message;}
        if(voice==null)return "voiceInstance=null, hold="+hold;
        var queued=new List<string>();
        foreach(var line in Get<IEnumerable>(voice,"queue"))queued.Add(VoiceLine(line));
        return "voiceHold="+hold+", speaking="+Get<bool>(voice,"speaking")+", current="+VoiceLine(Get<object>(voice,"current"))
            +", resume="+VoiceLine(Get<object>(voice,"resume"))+", queue=["+string.Join(" | ",queued)+"]";
    }
    static string VoiceLine(object line)
    {
        if(line==null)return "none";
        return "#"+Get<int>(line,"ticket")+" "+Get<string>(line,"zh")+" / "+Get<string>(line,"en");
    }
    static IEnumerable<object> Ready(WindowManager w)
    {
        foreach(var f in Until(()=>{
            if(!w.isOn||!w.gameObject.activeInHierarchy)return false;
            foreach(var g in w.GetComponentsInParent<CanvasGroup>(true))if(g.alpha<.95f||!g.interactable||!g.blocksRaycasts)return false;
            var a=w.windowAnimator;return a==null||!a.isActiveAndEnabled||!a.IsInTransition(0)&&a.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"window becomes naturally interactive: "+w.name))yield return f;
    }
    static Button Choice(Component view,string label)
    {
        var matches=view.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.IsInteractable()&&Label(b)==label).ToArray();
        Need(matches.Length==1,"one live button for "+label+" (found "+matches.Length+")");return matches[0];
    }
    static void Geometry(Component view,string[] choices)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var line in choices)
        {
            var b=Choice(view,line);var text=b.GetComponentInChildren<TMP_Text>();text.ForceMeshUpdate(true,true);
            Need(!text.isTextOverflowing,"long authored choice fits TMP rect: "+line);
            var r=(RectTransform)b.transform;Need(r.rect.width>180&&r.rect.height>=40,"full-width multiline choice geometry");
            foreach(char c in line)if(c>=0x4e00&&c<=0x9fff)Need(text.font.HasCharacter(c,true,false),"CJK glyph "+c);
        }
    }
    static void StopUnrelatedLoan()
    {
        foreach(var c in UnityEngine.Object.FindObjectsByType<ZhouCardLoan>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {c.StopAllCoroutines();c.enabled=false;}
    }
    static ChapterOneSim NewHouse(int stage,bool en)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=en;lab.S.stage=stage;lab.S.teachingFlow=false;lab.S.spiderIntroDone=true;lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;
        // These fixtures jump calendar months without playing the earlier game. The first month-update
        // afterthought is already read; keep the real MemeDrifted event and its model effects intact.
        // Otherwise its two naturally timed lines (first.drift) outlast the invitation's idle wait.
        lab.MarkAfterthought("first.drift");
        for(int i=2;i<=stage;i++)if(!lab.S.abilitiesEmerged.Contains(i))lab.S.abilitiesEmerged.Add(i);
        var forum=new ForumState();int n=0;
        foreach(var h in PrologueContent.Lines.History)forum.Conversation(ForumLibrary.LaoZhou).messages.Add(new ForumMessage{from=h.from=="me"?ForumLibrary.Me:ForumLibrary.LaoZhou,text=en?h.en:h.zh,gameSeconds=h.days*86400+n++});
        // This fixture tests Lao Zhou, not the independent AI-joins-YY opening cutscene.
        var yy=new YYState{lingguangUnlocked=true};foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);house.S.forumState=JsonUtility.ToJson(forum);house.S.chatState=JsonUtility.ToJson(yy);
        GameCalendar.Advance(house.S,GameCalendar.DayFor(stage,0));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
        return house;
    }
    static void Bind(XingGuangController ctl,ChapterOneSim house)
    {
        var runtime=ctl.runtime;runtime.SetSimulationForTests(house);
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(runtime,new object[]{false});
        runtime.tickEnabled=false;runtime.useDiskSave=false;
        Need(ReferenceEquals(runtime.Sim,house)&&!runtime.TestMode,"live host switched to explicit isolated fixture");
        Need(TiebaHub.Instance.ZhouStateBoundTo(house)&&YYChatHub.Instance.BoundTo(house),"both hubs bound same new simulation synchronously");
    }
    static void Close(XingGuangController ctl,TiebaView forum,YYChatView yy)
    {ctl.Window.CloseWindow();var fw=Get<WindowManager>(forum,"window");var yw=Get<WindowManager>(yy,"window");if(fw.isOn)fw.CloseWindow();if(yw.isOn)yw.CloseWindow();}
    static IEnumerable<object> ObserveMonthDrift(XgSim sim)
    {
        XgMemeDriftNotice observed=null;Action<XgMemeDriftNotice> onDrift=notice=>observed=notice;
        sim.MemeDrifted+=onDrift;
        try
        {
            foreach(var f in Until(()=>observed!=null,"stage2 fixture still emits the real monthly meme update"))yield return f;
            Need(observed.desks.Count>0&&observed.desks.Any(d=>sim.MemeDrift(d)>0),"month-update event still applies actual model drift");
            Need(sim.AfterthoughtSaid("first.drift")&&sim.PeekAfterthought("first.drift")==null,"only the already-read first month-update thought is suppressed");
        }
        finally{sim.MemeDrifted-=onDrift;}
    }
    static IEnumerable<object> OpenForum(TiebaView view,string id)
    {view.Open("chat",id);foreach(var f in Ready(Get<WindowManager>(view,"window")))yield return f;foreach(var f in FramesFor(.6))yield return f;}
    static IEnumerable<object> OpenYy(YYChatView view)
    {view.EnsureReady();view.OnOpened(YYChatHub.LaoZhou);var w=Get<WindowManager>(view,"window");w.OpenWindow();w.FocusToWindow();foreach(var f in Ready(w))yield return f;foreach(var f in FramesFor(.3))yield return f;}
    static int Count(YYChatHub yy,string text)=>yy.Conversation(YYChatHub.LaoZhou).messages.Count(m=>m.text==text);
    static void LocalRecord(TiebaView view,bool en,int branch)
    {
        string heading=en?"Discussed in YY · Only visible to you":"已在 YY 聊过 · 仅自己可见";
        var texts=view.GetComponentsInChildren<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy).ToArray();
        Need(texts.Count(t=>t.text==heading)==1,"one explicitly private YY completion heading");
        string chosen=LaoZhouQuestions.Choices(1,en)[branch-1];
        Need(texts.Any(t=>t.text.Contains(chosen)),"local record displays actual canonical YY choice");
        Need(!view.GetComponentsInChildren<Button>().Any(b=>b.gameObject.activeInHierarchy&&LaoZhouQuestions.Choices(1,en).Contains(Label(b))),"completed Q1 never asks again");
    }
    static void PromptBoundaries(TiebaHub forum,ChapterOneSim house,XgSim lab,bool en)
    {
        // Synthetic prompt fixture only: never inject generated lines into the live saved conversation.
        const string generated="LZ1_GENERATED_PLAYER_CLAIM", genuine="LZ1_GENUINE_PLAYER_LINE";
        var conv=new YYConversation{id=YYChatHub.LaoZhou};
        conv.messages.Add(new YYMessage{from=YYChatHub.Me,text=generated,byAi=true});
        conv.messages.Add(new YYMessage{from=YYChatHub.Me,text=genuine,byAi=false});
        var prompt=LlmPersonas.Conversation(YYChatHub.LaoZhou,conv,house,lab);
        Need(!prompt.Any(m=>m.Value.Contains(generated)),"legacy generated player text excluded at model boundary");
        Need(prompt.Any(m=>m.Key=="user"&&m.Value.Contains(genuine)),"real player line retained at model boundary");
        var regard=typeof(TiebaHub).GetMethod("Regard",BindingFlags.Static|BindingFlags.NonPublic);
        string low=(string)regard.Invoke(null,new object[]{LaoZhouQuestions.DiscussionId,1});
        Need(low==(string)regard.Invoke(null,new object[]{LaoZhouQuestions.DiscussionId,5}),"generic discussion prompt never gains stage reputation");
        var fallback=typeof(TiebaHub).GetMethod("DiscussionFallback",F);int stage=lab.S.stage;
        try
        {
            foreach(int s in new[]{1,3,5})
            {
                lab.S.stage=s;
                string reply=(string)fallback.Invoke(forum,new object[]{LaoZhouQuestions.DiscussionId,en?"Thanks":"谢谢",0});
                Need(reply==(en?"No problem. Come back and tell us how it goes.":"客气啥，有后续再来楼里说一声。"),"neutral fallback without invented past at stage "+s);
            }
        }
        finally{lab.S.stage=stage;}
    }
    static void EnableDisk(ChapterOneRuntime runtime)
    {
        string isolation=ChapterOneRuntime.VerificationSaveDirectory;
        Need(!string.IsNullOrEmpty(isolation)&&Path.GetFullPath(isolation).Contains(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar),"disk writes restricted to verification directory");
        Need(Path.GetFullPath(runtime.SavePath).StartsWith(Path.GetFullPath(isolation)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"runtime store actually points inside isolated directory");
        runtime.useDiskSave=true;
    }
    static void SaveSlot(ChapterOneRuntime runtime,int slot){EnableDisk(runtime);Need(runtime.SaveSlot(slot),"actual Runtime.SaveSlot "+slot+": "+runtime.SaveStatus);runtime.useDiskSave=false;}
    static void LoadSlot(ChapterOneRuntime runtime,int slot){EnableDisk(runtime);Need(runtime.LoadArchive(slot),"actual Runtime.LoadArchive "+slot+": "+runtime.SaveStatus);runtime.useDiskSave=false;}
    static IEnumerable<object> Link(TiebaHub forum,YYChatHub yy,TiebaView view,bool en,Action<RectTransform> click)
    {
        foreach(var f in Until(()=>forum.ZhouQa.yyInvitationSent,"one optional invitation arrives"))yield return f;
        Need(!forum.ZhouYyLinked&&!yy.IsVisible(YYChatHub.Contact(YYChatHub.LaoZhou)),"invitation alone does not add YY");
        foreach(var f in OpenForum(view,ForumLibrary.LaoZhou))yield return f;
        var b=Choice(view,LaoZhouQuestions.AcceptLabel(en));click((RectTransform)b.transform);
        int after=forum.Conversation(ForumLibrary.LaoZhou).messages.Count;
        click((RectTransform)b.transform); // Same frame, same real LCD target: duplicate must not append.
        Need(forum.ZhouYyLinked&&forum.ZhouQa.yyLinkSource=="tieba_dm_accept"&&yy.IsVisible(YYChatHub.Contact(YYChatHub.LaoZhou)),"actual LCD add-YY click establishes explicit contact");
        Need(forum.Conversation(ForumLibrary.LaoZhou).messages.Count==after,"duplicate actual LCD acceptance click is idempotent");
        yield return null;yield return null;
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        Checks.Clear();AssertionCount=0;
        PrepareInput();Need(Mouse.current!=null&&Keyboard.current!=null,"batch mouse and keyboard available");
        var runtime=ctl.runtime;var forum=TiebaHub.Instance;var yy=YYChatHub.Instance;
        Need(forum!=null&&forum.View!=null&&yy!=null&&yy.View!=null,"real Main Tieba and YY are installed");
        var fv=forum.View;var yv=yy.View;var oldFocus=StoryFocus.Probe;var oldExclusive=StoryFocus.ExclusiveProbe;bool held=false;
        StoryFocus.Probe=()=>held;StoryFocus.ExclusiveProbe=()=>false;StopUnrelatedLoan();
        try
        {
            // Exercise the actual subtitle coroutine and the production ZhouIdle gate independently of StoryFocus.
            held=true;Close(ctl,fv,yv);language(false);Bind(ctl,NewHouse(1,false));
            foreach(var f in Until(()=>!InnerVoice.Busy,"previous narration naturally finishes before voice-gate fixture",30))yield return f;
            bool voiceShown=false;
            InnerVoice.Say("等这一句说完。","Let this thought finish.",1.2f,()=>voiceShown=true,voiced:false);
            Need(InnerVoice.Busy,"real InnerVoice line entered the queue or began speaking");held=false;
            int busyFrames=0;double voiceDeadline=EditorApplication.timeSinceStartup+12;
            while(InnerVoice.Busy)
            {
                Need(!StoryFocus.Active&&!forum.ZhouQa.yyInvitationSent,"InnerVoice alone defers invitation during every busy frame");
                if(EditorApplication.timeSinceStartup>=voiceDeadline)Need(false,"real InnerVoice must finish naturally | "+IdleDiagnostic());
                busyFrames++;yield return null;
            }
            Need(busyFrames>=2&&voiceShown,"real voice was displayed and naturally completed across host updates");
            foreach(var f in Until(()=>forum.ZhouQa.yyInvitationSent,"invitation delivered after natural voice completion"))yield return f;
            Need(!Get<WindowManager>(fv,"window").isOn&&!Get<WindowManager>(yv,"window").isOn,"voice-deferred invitation does not force a window open");
            Done("real InnerVoice busy defers invitation; displayed line naturally finishes, then host delivers");
            // Explicit fresh fixtures: all acceptance and six public branch submissions use the real LCD route.
            foreach(bool en in new[]{false,true})for(int branch=1;branch<=3;branch++)
            {
                held=true;Close(ctl,fv,yv);language(en);Bind(ctl,NewHouse(1,en));held=false;
                string tag=(en?"en":"zh")+"-q1-"+branch;
                foreach(var f in Until(()=>forum.ZhouQa.yyInvitationSent,"unread invitation"))yield return f;
                Need(!Get<WindowManager>(fv,"window").isOn&&!Get<WindowManager>(yv,"window").isOn,"invitation never forces either window open");
                Need(forum.Conversation(ForumLibrary.LaoZhou).unread>0,"unopened invitation stays unread");
                foreach(var f in Link(forum,yy,fv,en,click))yield return f;capture("lz1-"+tag+"-accepted.png");
                foreach(var f in OpenForum(fv,LaoZhouQuestions.DiscussionId))yield return f;
                var choices=LaoZhouQuestions.Choices(1,en);Geometry(fv,choices);capture("lz1-"+tag+"-three-choices.png");
                var button=Choice(fv,choices[branch-1]);click((RectTransform)button.transform);
                int messages=forum.Conversation(LaoZhouQuestions.DiscussionId).messages.Count;click((RectTransform)button.transform);
                Need(forum.ZhouQa.first.choice==branch&&forum.ZhouQa.first.channel=="tieba_public","public button stores actual branch "+branch);
                Need(messages==forum.Conversation(LaoZhouQuestions.DiscussionId).messages.Count,"duplicate public LCD click appends nothing");
                Need(forum.Conversation(LaoZhouQuestions.DiscussionId).messages.Count(m=>m.from==ForumLibrary.LaoZhou&&m.text==LaoZhouQuestions.Response(1,branch,en))==1,"one response authored by Lao Zhou");
                Get<WindowManager>(fv,"window").CloseWindow();
                foreach(var f in Until(()=>forum.ZhouQa.first.yyContinued,"YY continuation of actual public reply"))yield return f;
                Need(!forum.ZhouQa.first.yyOffered&&yy.ZhouContactReady,"does not repeat full Q1 in YY");
                Need(Count(yy,LaoZhouQuestions.Response(1,branch,en))==0,"public response not repeated in YY");
                Need(Count(yy,"“"+choices[branch-1]+"”")==1,"YY quotes actual selected view once");
                Need(yy.Conversation(YYChatHub.LaoZhou).unread>0&&!Get<WindowManager>(yv,"window").isOn,"continuation stays unread without forcing YY");
                foreach(var f in OpenYy(yv))yield return f;capture("lz1-"+tag+"-yy-continuation.png");
                int count=yy.Conversation(YYChatHub.LaoZhou).messages.Count;
                SaveSlot(runtime,2);LoadSlot(runtime,2);foreach(var f in FramesFor(.8))yield return f;
                Need(yy.Conversation(YYChatHub.LaoZhou).messages.Count==count&&forum.ZhouQa.first.choice==branch,"real JsonUtility slot reload retains count and selected branch");
                Done(tag+": acceptance, public fixed branch, duplicate clicks, one YY continuation, real slot reload");
            }
            // Reverse path: YY first, then both views of the original thread. Never publish the private answer.
            foreach(bool en in new[]{false,true})for(int branch=1;branch<=3;branch++)
            {
                held=true;Close(ctl,fv,yv);language(en);Bind(ctl,NewHouse(2,en));held=false;
                foreach(var f in ObserveMonthDrift(ctl.Sim))yield return f;
                foreach(var f in Link(forum,yy,fv,en,click))yield return f;
                foreach(var f in Until(()=>forum.ZhouQa.first.yyOffered,"Q1 offered in YY first"))yield return f;
                Get<WindowManager>(fv,"window").CloseWindow();foreach(var f in OpenYy(yv))yield return f;
                var choices=LaoZhouQuestions.Choices(1,en);Geometry(yv,choices);
                click((RectTransform)Choice(yv,choices[branch-1]).transform);
                Need(forum.ZhouQa.first.choice==branch&&forum.ZhouQa.first.channel=="yy","Q1 genuinely completed in YY");
                Get<WindowManager>(yv,"window").CloseWindow();
                foreach(var f in OpenForum(fv,LaoZhouQuestions.DiscussionId))yield return f;
                LocalRecord(fv,en,branch);capture("lz1-"+(en?"en":"zh")+"-yy-first-"+branch+"-discussion.png");
                fv.Open("thread",LaoZhouQuestions.ThreadId);foreach(var f in FramesFor(.5))yield return f;
                LocalRecord(fv,en,branch);capture("lz1-"+(en?"en":"zh")+"-yy-first-"+branch+"-thread.png");
                Need(forum.Conversation(LaoZhouQuestions.DiscussionId).messages.Count==0,"private YY answer never appended to public conversation");
                Need(Count(yy,LaoZhouQuestions.Response(1,branch,en))==1,"browsing the original thread does not duplicate YY response");
                PromptBoundaries(forum,runtime.Sim,ctl.Sim,en);
                Done((en?"en":"zh")+": YY-first Q1 branch "+branch+", private local record in both views, prompt privacy/source checks");
            }
            // Six independent Q2 fixtures: no Q1 answer; all Q2 choices are clicked in both languages.
            foreach(bool en in new[]{false,true})for(int branch=1;branch<=3;branch++)
            {
                held=true;Close(ctl,fv,yv);language(en);Bind(ctl,NewHouse(6,en));held=false;
                foreach(var f in Link(forum,yy,fv,en,click))yield return f;
                foreach(var f in Until(()=>forum.ZhouQa.second.yyOffered,"Q2 offered without Q1"))yield return f;
                var fresh=LaoZhouQuestions.Opening(2,false,en);var remembered=LaoZhouQuestions.Opening(2,true,en);
                Need(forum.ZhouQa.first.choice==0&&Count(yy,fresh[0])==1&&Count(yy,remembered[0])==0,"Q2 independent opening does not pretend Q1 happened");
                Get<WindowManager>(fv,"window").CloseWindow();foreach(var f in OpenYy(yv))yield return f;
                var choices=LaoZhouQuestions.Choices(2,en);Geometry(yv,choices);capture("lz1-"+(en?"en":"zh")+"-q2-"+branch+"-three-choices.png");
                var b=Choice(yv,choices[branch-1]);click((RectTransform)b.transform);int count=yy.Conversation(YYChatHub.LaoZhou).messages.Count;click((RectTransform)b.transform);
                Need(forum.ZhouQa.second.choice==branch&&forum.ZhouQa.first.choice==0,"actual Q2 branch only");
                Need(count==yy.Conversation(YYChatHub.LaoZhou).messages.Count&&Count(yy,LaoZhouQuestions.Response(2,branch,en))==1,"Q2 duplicate click no repeated response");
                yield return null;yield return null;capture("lz1-"+(en?"en":"zh")+"-q2-"+branch+"-answered.png");
                Done((en?"en":"zh")+": Q2 independent branch "+branch);
            }
            // Scheduling is tested on the real host Update, not just the pure Due function.
            held=true;Close(ctl,fv,yv);language(false);Bind(ctl,NewHouse(2,false));held=false;
            foreach(var f in Link(forum,yy,fv,false,click))yield return f;
            foreach(var f in Until(()=>forum.ZhouQa.first.yyOffered,"real host Q1 offered"))yield return f;
            held=true;ctl.Sim.S.stage=6;foreach(var f in FramesFor(.8))yield return f;
            Need(!forum.ZhouQa.second.yyOffered&&forum.ZhouQa.first.choice==0,"StoryFocus defers Q2 with Q1 unanswered");
            ctl.Sim.S.bookReading=XgBooks.All[0].id;held=false;foreach(var f in FramesFor(.8))yield return f;
            Need(ctl.Sim.ReadingBook!=null&&!forum.ZhouQa.second.yyOffered,"actual reading flag defers Q2");
            ctl.Sim.S.bookReading="";foreach(var f in Until(()=>forum.ZhouQa.second.yyOffered,"Q2 arrives when idle even though Q1 is pending"))yield return f;
            Need(forum.ZhouQa.first.choice==0&&yy.ChoicesOf(YYChatHub.LaoZhou).SequenceEqual(LaoZhouQuestions.Choices(2,false)),"pending Q1 does not monopolize YY choices");
            Done("StoryFocus and reading defer real host delivery; pending Q1 allows Q2 at stage6");
            // Clone an actual accepted pending-Q2 snapshot, then click an old still-rendered button before next frame.
            Get<WindowManager>(fv,"window").CloseWindow();foreach(var f in OpenYy(yv))yield return f;
            SaveSlot(runtime,2);var oldButton=Choice(yv,LaoZhouQuestions.Choices(2,false)[0]);var oldState=forum.S;var oldYy=yy.S;
            int oldCount=yy.Conversation(YYChatHub.LaoZhou).messages.Count;LoadSlot(runtime,2);
            Need(!ReferenceEquals(oldState,forum.S)&&!ReferenceEquals(oldYy,yy.S)&&oldCount==yy.Conversation(YYChatHub.LaoZhou).messages.Count,"same-count new state objects restored");
            click((RectTransform)oldButton.transform);Need(forum.ZhouQa.second.choice==0,"old YY LCD button cannot answer new save before redraw");
            foreach(var f in FramesFor(.6))yield return f;click((RectTransform)Choice(yv,LaoZhouQuestions.Choices(2,false)[1]).transform);
            Need(forum.ZhouQa.second.choice==2,"fresh same-text YY buttons rebuild against new owner");capture("lz1-yy-same-count-swap.png");
            // Pending ordinary fallback is intentionally started by the actual input control, then a real slot load cancels it.
            LoadSlot(runtime,2);foreach(var f in FramesFor(.5))yield return f;
            var input=Get<TMP_InputField>(yv,"input");click((RectTransform)input.transform);input.text="LZ1_OLD_PENDING_MESSAGE";
            var send=yv.GetComponentsInChildren<Button>().Single(b=>b.name=="Send");click((RectTransform)send.transform);
            Need(yy.IsTyping(YYChatHub.LaoZhou),"ordinary old-save fallback queued by actual send button");LoadSlot(runtime,2);
            foreach(var f in FramesFor(5))yield return f;
            Need(!yy.IsTyping(YYChatHub.LaoZhou)&&yy.Conversation(YYChatHub.LaoZhou).messages.Count==oldCount,"old queued reply never lands in restored save");
            Done("real same-size slot swap blocks old LCD closure; pending normal reply cleared without model");
            // Same old-button race for public choices and the add-YY invitation.
            held=true;Close(ctl,fv,yv);Bind(ctl,NewHouse(1,false));held=false;
            foreach(var f in Until(()=>forum.ZhouQa.yyInvitationSent,"invitation for stale-click scenario"))yield return f;
            foreach(var f in OpenForum(fv,ForumLibrary.LaoZhou))yield return f;
            SaveSlot(runtime,2);oldButton=Choice(fv,LaoZhouQuestions.AcceptLabel(false));LoadSlot(runtime,2);click((RectTransform)oldButton.transform);
            Need(!forum.ZhouYyLinked,"old Tieba acceptance button does not link new save");foreach(var f in FramesFor(.7))yield return f;
            click((RectTransform)Choice(fv,LaoZhouQuestions.AcceptLabel(false)).transform);Need(forum.ZhouYyLinked,"new invitation button works after swap");
            foreach(var f in OpenForum(fv,LaoZhouQuestions.DiscussionId))yield return f;
            SaveSlot(runtime,2);oldButton=Choice(fv,LaoZhouQuestions.Choices(1,false)[0]);LoadSlot(runtime,2);click((RectTransform)oldButton.transform);
            Need(forum.ZhouQa.first.choice==0,"old public choice cannot answer new save");foreach(var f in FramesFor(.7))yield return f;
            click((RectTransform)Choice(fv,LaoZhouQuestions.Choices(1,false)[2]).transform);Need(forum.ZhouQa.first.choice==3,"fresh public button owns restored save");
            capture("lz1-tieba-same-count-swap.png");Done("actual LCD stale invitation and public-choice guards on same-count reload");
            // Regression: unanswered Q1 must not stop the real borrow-card host from leaving clStep=0.
            held=true;Close(ctl,fv,yv);Bind(ctl,NewHouse(2,false));held=false;
            foreach(var f in Link(forum,yy,fv,false,click))yield return f;
            foreach(var f in Until(()=>forum.ZhouQa.first.yyOffered,"loan regression starts with unanswered Q1"))yield return f;
            var sim=ctl.Sim;sim.S.stage=3;if(!sim.S.abilitiesEmerged.Contains(3))sim.S.abilitiesEmerged.Add(3);sim.S.stageEpochs=1;
            sim.S.trainedParamsK=sim.ParamsThreshold(sim.NextAbility)*1.25;
            sim.S.labels.RemoveAll(x=>x.dataset=="sense");var labels=new XgLabelCount{dataset="sense"};sim.S.labels.Add(labels);
            double lo=0,hi=sim.SamplesThreshold(sim.NextAbility)*100+100;for(int i=0;i<70;i++){labels.count=(lo+hi)/2;if(sim.TrainedSamples<sim.SamplesThreshold(sim.NextAbility)*1.25)lo=labels.count;else hi=labels.count;}labels.count=hi;
            Need(sim.NextAbility==4&&sim.CardLoanDue&&forum.ZhouQa.first.choice==0&&yy.ChoicesOf(YYChatHub.LaoZhou)!=null,"real stage3 exam-ready pending-Q1 fixture");
            var loan=UnityEngine.Object.FindAnyObjectByType<ZhouCardLoan>(FindObjectsInactive.Include);Need(loan!=null,"real Main borrow-card component installed");loan.enabled=true;
            foreach(var f in Until(()=>sim.S.clStep==1,"real borrow-card Update starts despite pending Q1",6))yield return f;
            Need(forum.ZhouQa.first.choice==0&&sim.CardLoanContactAvailable,"host starts borrowing without forcing knowledge answer");
            foreach(var f in FramesFor(2))yield return f;capture("lz1-pending-q1-loan-started.png");loan.StopAllCoroutines();loan.enabled=false;
            Done("real ZhouCardLoan Update/Ask reaches clStep1 with Q1 unanswered; no direct CardLoanStart invocation");
        }
        finally{runtime.useDiskSave=false;StoryFocus.Probe=oldFocus;StoryFocus.ExclusiveProbe=oldExclusive;StopUnrelatedLoan();}
    }
}
#endif
