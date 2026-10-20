#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;

public static class EndingSmallAActualUiChecks
{
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    public const string Scope="Ending A: explicit stage/contact/history/SI fixtures; production A2 checkpoint and A3 autonomous-sender seams, real isolated disk failure/rollback/retry/reload; real YY/Tieba transcripts and runtime persona construction. A1 and A4 production panel screenshots; A5 guide projection. No full ending progression, inference, physical input or Player claim.";
    public static readonly List<string> Checks=new List<string>();
    public static int Assertions {get;private set;}
    static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,F).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,F).SetValue(o,value);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,F).Invoke(o,args);
    static void Need(bool ok,string message){Assertions++;if(!ok)throw new InvalidOperationException("Ending A UI: "+message);}
    static IEnumerator Frames(int n=3){Canvas.ForceUpdateCanvases();while(n-->0)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerator Wait(float seconds){float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until)yield return null;}
    public static IEnumerator Foreground()
    {
        double end=Time.realtimeSinceStartupAsDouble+60,stableSince=-1;
        while(stableSince<0||Time.realtimeSinceStartupAsDouble-stableSince<3)
        {
            Need(Time.realtimeSinceStartupAsDouble<end,"actual foreground must stabilize for three seconds; never fabricate focus");
            if(Application.isFocused&&!PlayerWindowState.IsMinimized){if(stableSince<0)stableSince=Time.realtimeSinceStartupAsDouble;}
            else stableSince=-1;
            yield return null;
        }
    }
    static IEnumerator VisibleFrames(int n=3)
    {
        Canvas.ForceUpdateCanvases();
        while(n-->0){Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground retained during UI observation");yield return null;}
        Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground retained at UI observation end");Canvas.ForceUpdateCanvases();
    }
    static string Texts(Component root)=>string.Join("\n",root.GetComponentsInChildren<TMP_Text>().Where(x=>x.gameObject.activeInHierarchy).Select(x=>x.text));
    static XgEndingPreparationState State(XingGuangController ctl)=>ctl.Sim.S.endingPreparation;

    public static ChapterOneSim Fixture(bool en=false,bool yy=true,int stage=6,bool si=false,bool exchange=true)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=en;
        lab.S.stage=lab.S.stageVision=lab.S.stageSequence=stage;lab.S.fullOpen=true;
        lab.S.abilitiesEmerged=Enumerable.Range(1,stage).ToList();lab.S.reduceFx=true;lab.S.mute=true;lab.S.teachingFlow=false;lab.S.openingFocus=false;lab.S.openingChatPending=false;
        lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;lab.MarkAfterthought("first.drift");
        lab.S.endingPreparation=new XgEndingPreparationState();
        if(si){lab.S.endingPreparation.siReadReceipt="fixture:si-read";lab.S.endingPreparation.phase="asked";}
        var forum=new ForumState();var qa=forum.laoZhouQa;
        qa.legacyChecked=qa.yyInvitationSent=true;qa.yyLinkSource=yy?"tieba_dm_accept":"";
        qa.first.choice=qa.second.choice=1;qa.first.responseSent=qa.second.responseSent=qa.first.yyContinued=true;
        if(exchange)
        {
            var dm=forum.Conversation(ForumLibrary.LaoZhou);
            dm.messages.Add(new ForumMessage{from=ForumLibrary.Me,text="最近实验室忙吗？",gameSeconds=1});
            dm.messages.Add(new ForumMessage{from=ForumLibrary.LaoZhou,text="在跑实验。",gameSeconds=2});
        }
        var chat=new YYState{lingguangUnlocked=true};foreach(var c in YYChatHub.Contacts)chat.conversations.Add(new YYConversation{id=c.id});
        var world=new ChapterOneSim();world.S.prologue=2;world.S.appInstallState=2;world.S.money=1000000;
        world.S.labState=JsonUtility.ToJson(lab.S);world.S.chatState=JsonUtility.ToJson(chat);world.S.forumState=JsonUtility.ToJson(forum);
        GameCalendar.Advance(world.S,GameCalendar.DayFor(6,0));world.S.rentDay=GameCalendar.CurrentDay(world.S);world.S.broadbandMonth=201612;
        return world;
    }
    static XgEndingPreparationHost Bind(XingGuangController ctl,ChapterOneSim world)
    {
        ctl.runtime.useDiskSave=false;ctl.runtime.tickEnabled=false;ctl.GetComponent<XgEndingPreparationHost>()?.Hide();ctl.Epilogue?.Hide();
        ctl.runtime.SetSimulationForTests(world);
        Call(ctl,"BindSave",world);
        Need(YYChatHub.Instance.BoundTo(world)&&TiebaHub.Instance.ZhouStateBoundTo(world)&&ctl.BoundTo(world),"three actual hosts bound to one declared save");
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});
        var host=XgEndingPreparationHost.Install(ctl);host.enabled=false;
        YYChatHub.Instance.enabled=false;TiebaHub.Instance.enabled=false;ctl.enabled=false;
        StoryFocus.Probe=()=>false;StoryFocus.ExclusiveProbe=()=>false;
        GuardPath(ctl.runtime);ctl.runtime.useDiskSave=true;
        Need(ctl.SaveNow(),"baseline fixture commits via actual disk SaveNow");return host;
    }
    static void GuardPath(ChapterOneRuntime rt)
    {
        string root=ChapterOneRuntime.VerificationSaveDirectory;
        Need(!string.IsNullOrEmpty(root)&&Path.GetFullPath(root).Contains(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar),"explicit isolated LocalVerification save root");
        Need(Path.GetFullPath(rt.SavePath).StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"actual save and its tmp path stay inside isolated root");
    }
    // Own one new empty directory at the real temporary-file path; the real FileStream write must fail.
    // Never changes the primary file, access permissions, save delegate or application data directory.
    sealed class DiskFault:IDisposable
    {
        readonly string path;
        public DiskFault(ChapterOneRuntime rt)
        {GuardPath(rt);path=rt.SavePath+".tmp";Need(!File.Exists(path)&&!Directory.Exists(path),"temporary path initially absent");Directory.CreateDirectory(path);}
        public void Dispose(){if(Directory.Exists(path))Directory.Delete(path,false);}
    }
    static GameState Disk(ChapterOneRuntime rt)
    {
        var result=new AtomicSaveStore(Path.GetDirectoryName(rt.SavePath)).Load();
        Need(result.Success&&!result.Recovered,"primary disk snapshot reads without backup recovery");return JsonUtility.FromJson<GameState>(result.Payload);
    }
    static string OtherTranscripts()=>JsonUtility.ToJson(new Others{yy=YYChatHub.Instance.S.conversations.Where(x=>x.id!=YYChatHub.LaoZhou).ToList(),forum=TiebaHub.Instance.S.conversations.Where(x=>x.id!=ForumLibrary.LaoZhou).ToList()});
    [Serializable] sealed class Others {public List<YYConversation> yy;public List<ForumConversation> forum;}
    static void Prompt(XingGuangController ctl,bool en,bool final)
    {
        var yy=YYChatHub.Instance;var world=ctl.runtime.Sim;
        string p=LlmPersonas.Conversation(YYChatHub.LaoZhou,yy.Conversation(YYChatHub.LaoZhou),world,ctl.Sim)[0].Value;
        Need(p.Contains(final?LaoZhouQuestions.FinalPersonaRule(en):LaoZhouQuestions.EarlyPersonaRule(en)),"actual YY prompt uses appropriate author rule");
        Need(!p.Contains("不能扮演知情人、作者")&&!p.Contains("你不是程序作者"),"Lao Zhou prompt does not deny author identity");
        if(!final)Need(p.Contains(en?"Do not proactively reveal":"不要主动点破"),"early author rule prohibits proactive disclosure");
        foreach(string id in new[]{"cousin","netbar","oldfriend"})
        {
            string other=LlmPersonas.Conversation(id,yy.Conversation(id),world,ctl.Sim)[0].Value;
            Need(!other.Contains(LaoZhouQuestions.EarlyPersonaRule(en))&&!other.Contains(LaoZhouQuestions.FinalPersonaRule(en)),"author exception does not enter "+id+" prompt");
            Need(other.Contains("不能扮演知情人、作者"),"other NPC privacy restriction retained: "+id);
        }
    }
    static void Send(bool yy)=>Call(TiebaHub.Instance,"SendZhouFinalApproach",yy?(object)YYChatHub.Instance:null);
    static void Reload(XingGuangController ctl)
    {
        Need(ctl.runtime.ReloadSave(),"real primary reload succeeds");
        Need(ctl.BoundTo(ctl.runtime.Sim)&&YYChatHub.Instance.BoundTo(ctl.runtime.Sim)&&TiebaHub.Instance.ZhouStateBoundTo(ctl.runtime.Sim),"reload rebinds all three live hosts");
    }
    static IEnumerator Channel(XingGuangController ctl,bool en,bool yy,Action<string> capture)
    {
        string tag=(en?"en":"zh")+(yy?"-yy":"-tieba");
        Bind(ctl,Fixture(en,yy));var forum=TiebaHub.Instance;var hub=YYChatHub.Instance;var rt=ctl.runtime;
        Prompt(ctl,en,false);
        var yc=hub.Conversation(YYChatHub.LaoZhou);var fc=forum.S.Conversation(ForumLibrary.LaoZhou);
        var oldY=yc.messages;var oldF=fc.messages;int yu=yc.unread,fu=fc.unread;
        string unrelated=OtherTranscripts(),fj=rt.Sim.S.forumState,cj=rt.Sim.S.chatState,lj=rt.Sim.S.labState;
        byte[] disk=File.ReadAllBytes(rt.SavePath);
        using(new DiskFault(rt))Send(yy);
        Need(rt.SaveStatus.Contains(en?"the previous save was not deleted":"原存档未主动删除"),"A3 reached actual AtomicSaveStore disk-write failure, not a gate/no-op: "+rt.SaveStatus);
        Need(!forum.ZhouQa.finalApproachSent,"A3 failed disk write rolls marker back");
        Need(ReferenceEquals(yc.messages,oldY)&&ReferenceEquals(fc.messages,oldF)&&yc.unread==yu&&fc.unread==fu,"A3 message references and unread counters roll back");
        Need(rt.Sim.S.forumState==fj&&rt.Sim.S.chatState==cj&&rt.Sim.S.labState==lj,"A3 all serialized app snapshots roll back");
        Need(File.ReadAllBytes(rt.SavePath).SequenceEqual(disk),"A3 failed write leaves original primary bytes intact");
        Need(Get<float>(forum,"nextQaCheck")>Time.unscaledTime,"A3 failure establishes retry delay");
        yield return Wait(2.1f);Send(yy);
        Need(forum.ZhouQa.finalApproachSent&&forum.ZhouFinalContactContext,"A3 successful retry persists final-contact context");
        var lines=LaoZhouQuestions.FinalApproach(en);
        Need(yy?yc.messages.Skip(oldY.Count).Select(x=>x.text).SequenceEqual(lines):fc.messages.Skip(oldF.Count).Select(x=>x.text).SequenceEqual(lines),"A3 exact two authored lines in requested actual channel");
        Need(yy?fc.messages.Count==oldF.Count:yc.messages.Count==oldY.Count,"A3 other private channel unchanged");
        Need(OtherTranscripts()==unrelated,"A3 no other-contact or group transcript changes");
        var saved=Disk(rt);Need(JsonUtility.FromJson<ForumState>(saved.forumState).laoZhouQa.finalApproachSent,"A3 marker exists in real primary payload");
        Need(yy?JsonUtility.FromJson<YYState>(saved.chatState).conversations.Find(x=>x.id==YYChatHub.LaoZhou).messages.Count==oldY.Count+2:JsonUtility.FromJson<ForumState>(saved.forumState).Conversation(ForumLibrary.LaoZhou).messages.Count==oldF.Count+2,"A3 transcript and marker in same disk payload");
        Prompt(ctl,en,true);Reload(ctl);Send(yy);
        Need(yy?hub.Conversation(YYChatHub.LaoZhou).messages.Count==oldY.Count+2:forum.S.Conversation(ForumLibrary.LaoZhou).messages.Count==oldF.Count+2,"A3 reload does not resend invitation");

        var host=Bind(ctl,Fixture(en,yy,si:true));var flow=Get<XgEndingPreparationFlow>(host,"flow");
        yc=hub.Conversation(YYChatHub.LaoZhou);fc=forum.S.Conversation(ForumLibrary.LaoZhou);oldY=yc.messages;oldF=fc.messages;yu=yc.unread;fu=fc.unread;
        unrelated=OtherTranscripts();fj=rt.Sim.S.forumState;cj=rt.Sim.S.chatState;lj=rt.Sim.S.labState;disk=File.ReadAllBytes(rt.SavePath);
        using(new DiskFault(rt))Need(!flow.CheckpointPresentation(flow.CaptureBinding()).Success,"A2 real failed checkpoint reports failure");
        Need(rt.SaveStatus.Contains(en?"the previous save was not deleted":"原存档未主动删除"),"A2 reached actual AtomicSaveStore disk-write failure: "+rt.SaveStatus);
        Need(!State(ctl).siQuestionAnnounced,"A2 flow rolls announcement marker back");
        Need(ReferenceEquals(yc.messages,oldY)&&ReferenceEquals(fc.messages,oldF)&&yc.unread==yu&&fc.unread==fu,"A2 transcript references and unread roll back");
        Need(rt.Sim.S.forumState==fj&&rt.Sim.S.chatState==cj&&rt.Sim.S.labState==lj,"A2 all serialized app snapshots roll back");
        Need(File.ReadAllBytes(rt.SavePath).SequenceEqual(disk),"A2 failed write preserves primary bytes");
        Need(flow.CheckpointPresentation(flow.CaptureBinding()).Success&&State(ctl).siQuestionAnnounced,"A2 real retry commits announcement");
        var text=Get<XgEndingPreparationText>(host,"text");string question=XgEndingTextGuard.Filter(en?text.askedQuestionEn:text.askedQuestionZh,en);
        Need(yy?yc.messages.Skip(oldY.Count).Select(x=>x.text).SequenceEqual(new[]{question}):fc.messages.Skip(oldF.Count).Select(x=>x.text).SequenceEqual(new[]{question}),"A2 exact question in selected existing channel");
        Need(yy?fc.messages.Count==oldF.Count:yc.messages.Count==oldY.Count,"A2 does not create a second-channel transcript");
        Need(OtherTranscripts()==unrelated,"A2 no group or other-contact changes");
        saved=Disk(rt);Need(JsonUtility.FromJson<XgState>(saved.labState).endingPreparation.siQuestionAnnounced,"A2 marker persisted to disk");
        Need(yy?JsonUtility.FromJson<YYState>(saved.chatState).conversations.Find(x=>x.id==YYChatHub.LaoZhou).messages.Last().text==question:JsonUtility.FromJson<ForumState>(saved.forumState).Conversation(ForumLibrary.LaoZhou).messages.Last().text==question,"A2 question and marker share primary payload");
        Prompt(ctl,en,true);Reload(ctl);host=XgEndingPreparationHost.Install(ctl);host.enabled=false;flow=Get<XgEndingPreparationFlow>(host,"flow");
        Need(flow.CheckpointPresentation(flow.CaptureBinding()).Success,"A2 repeated checkpoint succeeds");
        Need(yy?hub.Conversation(YYChatHub.LaoZhou).messages.Count==oldY.Count+1:forum.S.Conversation(ForumLibrary.LaoZhou).messages.Count==oldF.Count+1,"A2 reload/checkpoint does not duplicate question");
        if(yy){var view=hub.View;view.EnsureReady();view.OnOpened(YYChatHub.LaoZhou);var w=Get<WindowManager>(view,"window");w.OpenWindow();w.FocusToWindow();}
        else{Need(forum.View!=null,"actual Tieba view exists");forum.View.Open("chat",ForumLibrary.LaoZhou);}
        yield return Foreground();yield return VisibleFrames(5);Need(yy?Texts(hub.View).Contains(question):Texts(forum.View).Contains(question),"actual channel UI renders committed A2 question");capture("ending-A-"+tag+"-question.png");
        if(yy)Get<WindowManager>(hub.View,"window").CloseWindow();else Get<WindowManager>(forum.View,"window").CloseWindow();
        Checks.Add(tag+": A3 + A2 actual temp-file disk failure/rollback/retry/primary reload, exact channels, no duplication, real persona and question UI.");
    }
    static void Boundaries(XingGuangController ctl,bool en)
    {
        Bind(ctl,Fixture(en,stage:5));Send(true);Need(!TiebaHub.Instance.ZhouQa.finalApproachSent,"early stage never sends A3");Prompt(ctl,en,false);
        foreach(bool attempted in new[]{false,true})
        {
            var world=Fixture(en);var s=JsonUtility.FromJson<XgState>(world.S.labState);
            var a=new XgEndingTrialAttempt{id="fixture:already-opened",question=0};
            if(attempted)s.endingPreparation.attempts.Add(a);else s.endingPreparation.pendingTrial=a;
            world.S.labState=JsonUtility.ToJson(s);Bind(ctl,world);Send(true);
            Need(!TiebaHub.Instance.ZhouQa.finalApproachSent,"already opened/attempted save never receives catch-up invitation");
        }
        var legacy=Fixture(en);legacy.S.forumState=legacy.S.forumState.Replace("\"finalApproachSent\":false,","").Replace(",\"finalApproachSent\":false","");
        Need(!legacy.S.forumState.Contains("finalApproachSent"),"legacy fixture really omits new field");Bind(ctl,legacy);
        Need(!TiebaHub.Instance.ZhouQa.finalApproachSent,"Unity deserializes omitted legacy marker as false");
        var host=Bind(ctl,Fixture(en,false,si:true,exchange:false));var flow=Get<XgEndingPreparationFlow>(host,"flow");int count=TiebaHub.Instance.S.Conversation(ForumLibrary.LaoZhou).messages.Count;
        Need(flow.CheckpointPresentation(flow.CaptureBinding()).Success&&!State(ctl).siQuestionAnnounced,"A2 no prior bilateral exchange leaves question pending");
        Need(TiebaHub.Instance.S.Conversation(ForumLibrary.LaoZhou).messages.Count==count,"A2 does not manufacture prior contact");
        Bind(ctl,Fixture(en,false,exchange:false));Send(false);Need(!TiebaHub.Instance.ZhouQa.finalApproachSent,"A3 cannot manufacture a bilateral Tieba exchange");
        Checks.Add((en?"en":"zh")+": early stage, already-opened/attempted, missing legacy field and absent bilateral contact boundaries.");
    }
    static IEnumerator Presentation(XingGuangController ctl,bool en,Action<string> capture)
    {
        var host=Bind(ctl,Fixture(en));Need(host.Show(PrologueDirector.Desk.layer),"actual production ending cover opens");yield return Foreground();yield return VisibleFrames();
        var trial=Get<EndingTrialPanel>(host,"trial");Need(Texts(trial).Contains(EndingTrialPanel.CoverLine(en)),"A1 actual cover includes approved sentence");
        capture("ending-A-"+(en?"en":"zh")+"-cover.png");
        var guide=XgGuide.Current(ctl.Sim,ctl.runtime.Sim.S.money);
        Need(guide.Any(x=>x.id.StartsWith("final.preparation.")&&x.tab=="final"),"A5 actual current guide routes to Finale");
        // Panel-only fixture: no synthetic release receipt is installed or saved. Reuse the actual existing panel.
        trial.gameObject.SetActive(false);Get<EndingPreparationPanel>(host,"preparation").gameObject.SetActive(false);
        var night=Get<EndingLaunchNightPanel>(host,"night");night.gameObject.SetActive(true);
        var source=new XgEndingMemorySource{kind="answer",refId="answer:99999",day=20160602,summary="This is a saved source, not proof of player teaching."};
        string reason=XgEndingPresentation.Evidence(ctl.Sim,source,en);
        var snap=new EndingUiSnapshot{owner=ctl.runtime.Sim,stateOwner=ctl.Sim.S,english=en,reduceFx=true,readOnly=true};
        snap.night=new EndingNightView{requestId="panel-only-fixture",corrupted=true,reason=new EndingUiText{zh=reason,en=reason}};
        night.Bind(snap,new EndingUiCallbacks{IsCurrent=(a,b,c)=>ReferenceEquals(a,ctl.runtime.Sim)&&ReferenceEquals(b,ctl.Sim.S),FilterText=XgEndingTextGuard.Filter,FilterReviewedText=XgEndingTextGuard.FilterReviewed});
        yield return Foreground();yield return VisibleFrames();string shown=Get<TMP_Text>(night,"reason").text;
        Need(shown==(en?"Misled: ":"被带偏：")+XgEndingTextGuard.Filter(reason,en),"A4 actual panel has one corruption prefix and neutral source attribution");
        Need(!shown.Contains(en?"you taught":"教过"),"A4 unknown answer wrapper does not invent player teaching");
        capture("ending-A-"+(en?"en":"zh")+"-source-panel.png");host.Hide();
        Checks.Add((en?"en":"zh")+": actual A1 cover and read-only A4 panel fixture screenshots; A5 actual guide projection, not Bodu click-path coverage.");
    }
    public static IEnumerator Run(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        Checks.Clear();Assertions=0;ctl.Window.CloseWindow();
        Need(YYChatHub.Instance?.View!=null&&TiebaHub.Instance?.View!=null,"actual YY and Tieba views are ready");
        Need(LocalLlm.Instance==null||!LocalLlm.Instance.Ready,"no model inference service");
        for(int i=0;i<2;i++)
        {
            bool en=i==1;language(en);Boundaries(ctl,en);
            yield return Channel(ctl,en,true,capture);yield return Channel(ctl,en,false,capture);
            yield return Presentation(ctl,en,capture);
        }
        ctl.runtime.useDiskSave=false;
    }
}
#endif
