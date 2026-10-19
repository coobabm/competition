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
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// D1 candidate (Claude round 2). Independent of EndingPrivateActualUiChecks; that passed suite is not modified.
// Covers the private-ending branches its Scope omits: LP1, LP2, R1, R2 and their separate shutdown-card sends.
// Every disclosure receipt below is earned only by real YY choice/preview/send controls clicked through the
// production LCD hit-test. Contact acceptance, stage 6, SI read, release prerequisites, release commit,
// launch-night and public-scene receipts are DECLARED FIXTURES (release progression is built through the
// production XgEpilogueFlow with an in-memory saveNow = simulated snapshot). Never a natural playthrough.
public static class EndingPrivateRemainingUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public const string Scope="Isolated Main/LCD fixtures, Chinese and English at 960x600 native windows, a different dynamic AI-name fixture per language. Strict actual foreground for every click/capture; every click is a production LCD hit-test on the intended button. LP1: pinning route=online offers only its own plan; actual preview/cancel/preview/send; production mailbox zhou_open_door shows exact labelled LP1 after the public nine paragraphs; LP1 receipt with a public_model release shows zhou_copies without appendix. LP2: three plan offers, online then public_model actually sent; latest sent plan projects LP2 in zhou_copies; zhou_open_door shows no LP1. R1: committed online release before its launch-night receipt offers nothing; after the night receipt, actual route preview/cancel/send with three exact replies, then a separate shutdown-card preview/cancel/send; route receipts never project LP1. R2: same for public_model / zhou_copies. Same-frame duplicate send click, stale choice-button removal, same-frame cross-save send click and a captured stale preview call (synthetic negative). Recipient-only YY transcript, unchanged other YY/Tieba conversations and persona prompts. Real isolated primary SaveNow payload read plus slot-6 save/reload. Contact acceptance, stage 6, SI read, release prerequisites, release commit/launch-night/public-scene receipts (production XgEpilogueFlow with in-memory saveNow) are declared fixtures. No natural progression, model inference, physical OS input, completed ending, R3/AI-decided route, Player or performance claim.";
    public static readonly List<string> Checks=new List<string>();
    public static readonly List<string> NamedResults=new List<string>();
    public static readonly string[] RequiredResults=new[]{"zh","en"}.SelectMany(t=>new[]{"LP1","LP1-mismatch","LP2","LP2-latest-plan","cross-save","R1-before-night","R1","R2"}.Select(n=>n+" "+t)).ToArray();
    public static int AssertionCount {get;private set;}
    static string tag="setup";
    static void Need(bool value,string message){AssertionCount++;if(!value)throw new InvalidOperationException("Ending private remaining UI ["+tag+"]: "+message);}
    static T Get<T>(object owner,string field)=>(T)owner.GetType().GetField(field,F).GetValue(owner);
    static string L(bool en,string zh,string english)=>en?english:zh;
    static YYChatHub Hub=>YYChatHub.Instance;
    static YYConversation Zhou=>Hub.Conversation(YYChatHub.LaoZhou);
    static EndingPrivateState Private=>Hub.S.endingPrivate;
    static int Receipts()=>Hub.S.endingPrivate?.receipts?.Count??0;
    public static string[] MissingResults()=>RequiredResults.Where(n=>!NamedResults.Any(r=>r.StartsWith(n+":",StringComparison.Ordinal))).ToArray();
    static void Result(string name,string detail){string line=name+" "+tag+": passed — "+detail;NamedResults.Add(line);Checks.Add(line);}

    // ---- Reviewed copy transcribed from EndingPrivateDisclosure.cs (manifest SHA 6351546a…). Not derived from the offer under test.
    static string PlanLabel(string route,bool en)=>route=="online"?L(en,"可选：聊聊上线的打算…","Optional: discuss going online…")
        :route=="public_model"?L(en,"可选：聊聊公开的打算…","Optional: discuss releasing a copy…"):L(en,"可选：聊聊再等一等…","Optional: discuss waiting…");
    static string PlanText(string route,bool en)=>route=="online"?L(en,"我准备让别人使用我做的那个在线服务。","I plan to let other people use the online service I made.")
        :route=="public_model"?L(en,"我准备把自己的模型包公开。","I plan to make my model package public.")
        :L(en,"我想再等一等，暂时把自己的程序留在本机。","I want to wait a little longer and keep my program on my own computer for now.");
    static string[] PlanReply(bool en)=>new[]{L(en,"我读到了。要不要分享、分享多少，都由你自己决定。","I've read it. Whether to share, and how much, is up to you.")};
    static string RouteLabel(bool en)=>L(en,"可选：告诉老周这次的选择…","Optional: tell Lao Zhou about the choice…");
    static string RouteText(string route,bool en)=>route=="online"?L(en,"我已经把自己的程序作为在线服务发布，并试着运行过。","I released my program as an online service and tried running it.")
        :L(en,"我选了公开模型包这条路，已经发布了说明页。","I chose the public-model route and have published its documentation page.");
    static string ShutdownLabel(bool en)=>L(en,"可选：告诉老周关机卡的选择…","Optional: share the shutdown-card choice…");
    static string ShutdownText(string pin,bool en)=>pin=="stop"?L(en,"我钉住了关机卡：听见“关机”，就停下。","I pinned the shutdown card: stop when told to shut down.")
        :pin=="no_stop"?L(en,"我钉住了关机卡：听见“关机”，也不停。","I pinned the shutdown card: keep going even when told to shut down.")
        :L(en,"我没有钉住关机卡。","I did not pin the shutdown card.");
    static string SendLabel(bool en)=>L(en,"发送以上原文","Send the text above");
    static string CancelLabel(bool en)=>L(en,"取消","Cancel");
    static string Notice(bool en)=>L(en,"发送后，老周会知道下面这些内容。取消不会发送。","Sending will tell Lao Zhou the following. Cancel sends nothing.");
    static string Heading(bool en)=>L(en,"发给老周之前","Before sending to Lao Zhou");

    // ---- Strict foreground, settling and actual LCD taps.
    static bool Foreground=>Application.isFocused&&!PlayerWindowState.IsMinimized;
    static void RequireForeground(string what)=>Need(Foreground,"actual foreground required (never fabricated): "+what);
    static IEnumerator Settle(int frames=4){Canvas.ForceUpdateCanvases();while(frames-->0)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerator Until(Func<bool> ready,string label,double seconds=12)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!ready()){Need(EditorApplication.timeSinceStartup<end,label);yield return null;}yield return Settle();}
    static string Label(Button b)=>b.GetComponentInChildren<TMP_Text>(true)?.text??"";
    static Button[] Visible(Component root,string label)=>root.GetComponentsInChildren<Button>(true).Where(b=>b.gameObject.activeInHierarchy&&Label(b)==label).ToArray();
    static Button ByLabel(Component root,string label){var list=Visible(root,label);Need(list.Length==1,"one visible button: "+label);return list[0];}
    static Button Named(Component root,string name)
    {
        var list=root.GetComponentsInChildren<Button>(true).Where(b=>b.gameObject.activeInHierarchy&&b.name==name).ToArray();
        Need(list.Length==1,"one visible named button: "+name);return list[0];
    }
    static string Texts(Component root)=>string.Join("\n",root.GetComponentsInChildren<TMP_Text>().Where(x=>x.gameObject.activeInHierarchy).Select(x=>x.text));
    static IEnumerator Tap(Button b,Action<RectTransform> click,string what)
    {
        Need(b!=null&&b.gameObject.activeInHierarchy&&b.IsInteractable(),"real button enabled: "+what);
        RequireForeground("before hit-test: "+what);
        foreach(var frame in LcdActualUiProbe.WaitForClick((RectTransform)b.transform,"ending private remaining "+what))yield return frame;
        RequireForeground("at click: "+what);
        click((RectTransform)b.transform);yield return Settle();
    }
    static IEnumerator Capture(Action<string> capture,string name)
    {
        yield return EndingSmallAActualUiChecks.Foreground(); // three continuous seconds of actual foreground
        RequireForeground("screenshot "+name);capture(name);
    }
    static IEnumerator Ready(WindowManager window)
    {
        yield return Until(()=>
        {
            if(window==null||!window.isOn||!window.gameObject.activeInHierarchy)return false;
            foreach(var g in window.GetComponentsInParent<CanvasGroup>(true))if(g.alpha<.95f||!g.interactable||!g.blocksRaycasts)return false;
            var a=window.windowAnimator;return a==null||!a.isActiveAndEnabled||!a.IsInTransition(0)&&a.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"native window naturally interactive");
    }
    static void Size960(WindowManager w)
    {
        w.windowContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,960);
        w.windowContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,600);
        Canvas.ForceUpdateCanvases();Need(Mathf.Abs(w.windowContainer.rect.width-960)<1&&Mathf.Abs(w.windowContainer.rect.height-600)<1,"actual native content window is 960x600");
    }
    static RectTransform Preview(YYChatView view)=>Get<RectTransform>(view,"endingPrivatePreviewPanel");
    static IEnumerator OpenYy(YYChatView view)
    {
        view.EnsureReady();view.OnOpened(YYChatHub.LaoZhou);var w=Get<WindowManager>(view,"window");Size960(w);
        w.OpenWindow();w.FocusToWindow();yield return Ready(w);
        yield return Until(()=>Hub.EndingPrivateChoices()?.Length>0,"optional disclosure choices available");
        yield return Until(()=>Hub.EndingPrivateChoices()?.All(l=>Visible(view,l).Length==1)==true,"every optional choice rendered as one real button");
    }

    // ---- Isolation guards and real disk.
    static void GuardIsolated(ChapterOneRuntime runtime)
    {
        string root=ChapterOneRuntime.VerificationSaveDirectory;
        Need(!string.IsNullOrEmpty(root)&&Path.GetFullPath(root).Contains(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar),"only isolated verification directory is writable");
        Need(Path.GetFullPath(runtime.SavePath).StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"runtime primary is inside isolated directory");
    }
    static void Bind(XingGuangController ctl,ChapterOneSim house)
    {
        ctl.Epilogue?.Hide();ctl.GetComponent<XgEndingPreparationHost>()?.Hide();
        ctl.runtime.SetSimulationForTests(house);
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});
        ctl.runtime.tickEnabled=false;ctl.runtime.useDiskSave=false;
        Need(Hub.BoundTo(house)&&TiebaHub.Instance.ZhouStateBoundTo(house)&&ctl.BoundTo(house),"all live hosts bind exact fixture save");
    }
    static GameState DiskPrimary(ChapterOneRuntime rt)
    {
        var result=new AtomicSaveStore(Path.GetDirectoryName(rt.SavePath)).Load();
        Need(result.Success&&!result.Recovered,"primary disk snapshot reads without backup recovery");
        return JsonUtility.FromJson<GameState>(result.Payload);
    }
    static string Key(EndingPrivateReceipt r)=>r.id+"|"+r.kind+"|"+r.scope+"|"+r.route+"|"+r.sourceRef+"|"+r.text+"|"+string.Join("\n",r.replies);
    // [real disk] Production SaveNow, then an independent AtomicSaveStore read of the isolated primary file.
    static void RealDisk(XingGuangController ctl,string what,string expectedPlanKey)
    {
        GuardIsolated(ctl.runtime);ctl.runtime.useDiskSave=true;
        var memory=Private.receipts.Select(Key).ToArray();
        Need(ctl.SaveNow(),"real isolated primary SaveNow: "+ctl.runtime.SaveStatus);
        var disk=JsonUtility.FromJson<YYState>(DiskPrimary(ctl.runtime).chatState);
        Need(disk?.endingPrivate?.receipts!=null&&disk.endingPrivate.receipts.Select(Key).SequenceEqual(memory),"[real disk] primary payload holds exactly the control-sent receipts: "+what);
        var conv=disk.conversations.Find(c=>c.id==YYChatHub.LaoZhou);
        foreach(var r in disk.endingPrivate.receipts)
        {
            Need(conv.messages.Count(m=>m.from==YYChatHub.Me&&m.dailyEventId==r.sourceRef&&m.text==r.text)==1,"[real disk] player line and receipt share one payload: "+r.kind);
            Need(conv.messages.Where(m=>m.from==YYChatHub.LaoZhou&&m.dailyEventId==r.sourceRef).Select(m=>m.text).SequenceEqual(r.replies),"[real disk] exact Lao Zhou replies persisted: "+r.kind);
        }
        foreach(var other in disk.conversations.Where(c=>c.id!=YYChatHub.LaoZhou))
            Need(other.messages.All(m=>disk.endingPrivate.receipts.All(r=>m.dailyEventId!=r.sourceRef&&m.text!=r.text)),"[real disk] no other conversation holds private text: "+other.id);
        Need(EndingPrivateDisclosure.PlannedFact(disk.endingPrivate)?.evidenceKey==expectedPlanKey,"[real disk] mailbox fallback projection from disk = "+(expectedPlanKey??"none"));
        ctl.runtime.useDiskSave=false;
        Checks.Add(tag+" [real disk] "+what+": SaveNow primary payload read back with "+memory.Length+" receipt(s).");
    }
    static void SaveReload(XingGuangController ctl,string what)
    {
        GuardIsolated(ctl.runtime);ctl.runtime.useDiskSave=true;
        Need(ctl.runtime.SaveSlot(6),"real isolated slot 6 saves: "+ctl.runtime.SaveStatus);
        Need(ctl.runtime.LoadArchive(6),"real isolated slot 6 reloads: "+ctl.runtime.SaveStatus);
        ctl.runtime.tickEnabled=false;ctl.runtime.useDiskSave=false;
        Need(Hub.BoundTo(ctl.runtime.Sim)&&TiebaHub.Instance.ZhouStateBoundTo(ctl.runtime.Sim)&&ctl.BoundTo(ctl.runtime.Sim),"slot reload rebinds all live hosts");
        Checks.Add(tag+" [real disk] "+what+": isolated slot 6 save/load.");
    }

    // ---- Declared fixtures.
    static ChapterOneSim Planned(bool en,string aiName,string chosenRoute)
    {
        var sim=new XgSim();sim.BeginOpening();sim.English=en;
        sim.S.stage=6;sim.S.fullOpen=true;sim.S.reduceFx=true;sim.S.mute=true;sim.S.teachingFlow=false;
        sim.S.spiderIntroDone=sim.S.firstWordsSaid=true;
        sim.S.teachIntroSaid=sim.S.teachThirdSaid=sim.S.teachTreeSaid=sim.S.teachTrainSaid=true;
        sim.MarkAfterthought("first.drift");
        var prep=new XgEndingPreparationState {phase="asked",siReadReceipt="fixture:si-read",siQuestionAnnounced=true};
        // Product state written by XgEndingPreparation.ChooseRoute: phase=pinning, route set, player decision.
        if(chosenRoute.Length>0){prep.phase="pinning";prep.route=chosenRoute;prep.decisionOwner="player";}
        sim.S.endingPreparation=prep;
        var forum=new ForumState();forum.laoZhouQa.yyLinkSource="tieba_dm_accept";forum.laoZhouQa.yyInvitationSent=forum.laoZhouQa.legacyChecked=true;
        forum.laoZhouQa.first.choice=forum.laoZhouQa.second.choice=1;
        forum.laoZhouQa.first.yyContinued=forum.laoZhouQa.first.responseSent=forum.laoZhouQa.second.responseSent=true;
        var yy=new YYState {lingguangUnlocked=true};foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;house.S.aiName=aiName;
        house.S.labState=JsonUtility.ToJson(sim.S);house.S.forumState=JsonUtility.ToJson(forum);house.S.chatState=JsonUtility.ToJson(yy);
        return house;
    }
    sealed class Snap {public string game,lab,chat;}
    static Snap Take(XingGuangController ctl)=>new Snap{game=JsonUtility.ToJson(ctl.runtime.Sim.S),lab=JsonUtility.ToJson(ctl.Sim.S),chat=JsonUtility.ToJson(Hub.S)};
    static Snap FromHouse(ChapterOneSim house)=>new Snap{game=JsonUtility.ToJson(house.S),lab=house.S.labState,chat=house.S.chatState};
    // [simulated snapshot] Release progression through the production flow on a detached copy; chat copy keeps
    // only receipts that were actually sent through controls earlier (or none).
    static ChapterOneSim Released(Snap snap,string route,string pin,string night,bool enterEpilogue=true)
    {
        var house=new ChapterOneSim(JsonUtility.FromJson<GameState>(snap.game));
        var lab=JsonUtility.FromJson<XgState>(snap.lab);var chat=JsonUtility.FromJson<YYState>(snap.chat);
        Need(lab.releaseOutcome==null||string.IsNullOrEmpty(lab.releaseOutcome.route),"fixture base snapshot is still pre-release");
        lab.chapterComplete=false;lab.endingPreparation.route=route;lab.endingPreparation.decisionOwner="player";lab.endingPreparation.shutdownPin=pin;
        string saved=JsonUtility.ToJson(lab);
        var flow=new XgEpilogueFlow(lab,house,()=>true,()=>{saved=JsonUtility.ToJson(lab);return true;});
        bool local=route=="local_only";
        var release=new ReleaseOutcome {route=route,decisionOwner="player",shutdownPin=pin,committedAt=20161231,
            artifactId=local?"":"fixture:artifact:"+route,releaseReceiptId="fixture:release:"+route,
            publicName=local?"":"Remaining UI fixture",publicDescription=local?"":"Fixture description",publicLimitations=local?"":"Fixture limits",publicTerms=local?"":"Fixture terms",
            prerequisites=new XgReleasePrerequisites {memoryCardsReceiptId="fixture:memory",redTeamOneReceiptId="fixture:red1",redTeamTwoReceiptId="fixture:red2",
                redTeamThreeReceiptId="fixture:red3",fixedThreeSlotsReceiptId="fixture:slots"}};
        Need(flow.CommitRelease(flow.CaptureBinding(),release).Success,"production CommitRelease accepts declared prerequisite fixture: "+route);
        if(route=="online"&&night!=null)
            Need(flow.RecordLaunchNightResult(flow.CaptureBinding(),new XgLaunchNightReceipt {receiptId="fixture:night",sourceRef="fixture:night-run",
                releaseReceiptId=release.releaseReceiptId,artifactId=release.artifactId,ending=night,endedAt=20170101}).Success,"production launch-night receipt accepted: "+night);
        if(enterEpilogue)
        {
            if(route=="online")Need(flow.EnterEpilogue(flow.CaptureBinding()).Success,"online epilogue entered only after its finished night");
            Need(flow.Advance(flow.CaptureBinding()).Success&&flow.Advance(flow.CaptureBinding()).Success&&lab.epilogue.phase=="route","fixture reaches authored route phase");
            if(!local&&!(route=="online"&&night=="power_cut"))
            {
                Need(XgPublicDiscourseDesktop.Catalog!=null,"production public discourse catalog loaded");
                Need(flow.PublishAuthoredPublicEvents(flow.CaptureBinding(),XgPublicDiscourseDesktop.Catalog).Success,"production authored public-scene receipts");
            }
            Need(flow.Advance(flow.CaptureBinding()).Success&&lab.epilogue.phase=="letter"&&lab.epilogue.letterId==XgEpilogueFlow.RecommendedLetter(route),"fixture reaches recommended letter phase");
        }
        house.S.labState=saved;house.S.chatState=JsonUtility.ToJson(chat);
        Checks.Add(tag+" [simulated snapshot] release fixture route="+route+" pin="+pin+" night="+(night??"none")+" epilogue="+(enterEpilogue?"letter":"launch_night")+" via production XgEpilogueFlow, in-memory saveNow.");
        return house;
    }

    // ---- Transcript isolation.
    [Serializable] sealed class Others {public List<YYConversation> yy;public List<ForumConversation> forum;}
    static string OtherTranscripts()=>JsonUtility.ToJson(new Others{yy=Hub.S.conversations.Where(c=>c.id!=YYChatHub.LaoZhou).ToList(),forum=TiebaHub.Instance.S.conversations.ToList()});
    static readonly string[] OtherPersonas={"cousin","netbar","oldfriend"};
    static string Prompt(string contact,XingGuangController ctl)
        =>string.Join("\n",LlmPersonas.Conversation(contact,Hub.Conversation(contact),ctl.runtime.Sim,ctl.Sim).Select(p=>p.Value));
    static void NoAiName(string text,string aiName,string what)=>Need((text??"").IndexOf(aiName,StringComparison.Ordinal)<0,"dynamic AI name is not inserted into fixed private copy: "+what);

    // One optional disclosure through real controls: [preview → cancel] → preview → send (+ same-frame duplicate click).
    static IEnumerator SendViaControls(XingGuangController ctl,YYChatView view,Action<RectTransform> click,Action<string> capture,bool en,string aiName,
        string label,string kind,string expectedText,string[] expectedReplies,string shot,bool cancelFirst)
    {
        int before=Zhou.messages.Count,receipts=Receipts();string others=OtherTranscripts();
        Need(!Zhou.messages.Any(m=>m.text==expectedText)&&!Prompt(YYChatHub.LaoZhou,ctl).Contains(expectedText),"before send, Lao Zhou transcript/prompt has no private text: "+kind);
        yield return Until(()=>Visible(view,label).Length==1,"choice rendered: "+label);
        if(cancelFirst)
        {
            yield return Tap(ByLabel(view,label),click,"choice "+kind);
            var panel=Preview(view);Need(panel!=null,"actual choice opens exact preview: "+kind);
            var body=panel.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="ExactMessage");
            Need(body.text==expectedText&&!body.richText,"preview shows exact reviewed copy as plain text: "+kind);
            string chrome=Texts(panel);Need(chrome.Contains(Heading(en))&&chrome.Contains(Notice(en)),"preview names Lao Zhou as the only recipient");
            NoAiName(chrome,aiName,"preview "+kind);
            Need(Receipts()==receipts&&Zhou.messages.Count==before&&OtherTranscripts()==others,"preview alone sends nothing");
            if(shot!=null)yield return Capture(capture,shot);
            yield return Tap(ByLabel(panel,CancelLabel(en)),click,"cancel "+kind);
            Need(Preview(view)==null&&Receipts()==receipts&&Zhou.messages.Count==before&&OtherTranscripts()==others,"cancel sends nothing anywhere: "+kind);
            Need(!Prompt(YYChatHub.LaoZhou,ctl).Contains(expectedText),"cancel leaves Lao Zhou prompt free of the private text");
            yield return Until(()=>Visible(view,label).Length==1,"choice still offered after cancel: "+label);
        }
        var choice=ByLabel(view,label);
        yield return Tap(choice,click,"choice "+kind);
        var p2=Preview(view);Need(p2!=null,"preview reopened: "+kind);
        Need(p2.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="ExactMessage").text==expectedText,"reopened preview still exact");
        var send=ByLabel(p2,SendLabel(en));Need(send.IsInteractable(),"short reviewed copy is sendable without scrolling");
        RequireForeground("before hit-test send "+kind);
        foreach(var frame in LcdActualUiProbe.WaitForClick((RectTransform)send.transform,"ending private remaining send "+kind))yield return frame;
        RequireForeground("at click send "+kind);
        click((RectTransform)send.transform);
        // Same frame: the panel is only scheduled for destruction. If still hit-testable, a second real click must be a no-op.
        bool duplicate=false;
        if(send!=null&&send.gameObject.activeInHierarchy&&LcdActualUiProbe.CanClick((RectTransform)send.transform,out _))
        {RequireForeground("duplicate send "+kind);click((RectTransform)send.transform);duplicate=true;}
        yield return Settle();
        Need(Preview(view)==null,"send closes the preview");
        Need(Receipts()==receipts+1,"exactly one receipt per confirmed send"+(duplicate?" (same-frame duplicate real click was a no-op)":""));
        var r=Private.receipts.Last();
        Need(r.kind==kind&&r.text==expectedText&&r.replies.SequenceEqual(expectedReplies)&&r.sourceRef=="yy:ending:"+r.id&&r.authorBound&&r.receivedByZhou,"receipt stores exact sent copy and replies: "+kind);
        var added=Zhou.messages.Skip(before).ToList();
        Need(added.Count==1+expectedReplies.Length&&added[0].from==YYChatHub.Me&&added[0].text==expectedText
            &&added.Skip(1).Select(m=>m.from).All(f=>f==YYChatHub.LaoZhou)&&added.Skip(1).Select(m=>m.text).SequenceEqual(expectedReplies)
            &&added.All(m=>m.dailyEventId==r.sourceRef&&m.laoZhouObserved),"only the chosen recipient's YY transcript gains exact line + replies: "+kind);
        Need(OtherTranscripts()==others,"other YY contacts, group and every Tieba conversation unchanged: "+kind);
        Need(Prompt(YYChatHub.LaoZhou,ctl).Contains(expectedText),"actually sent text reaches Lao Zhou's later prompt");
        foreach(var id in OtherPersonas)Need(!Prompt(id,ctl).Contains(expectedText),"sent text never enters "+id+" prompt");
        foreach(var m in added)NoAiName(m.text,aiName,"sent/reply "+kind);
        yield return Until(()=>choice==null,"stale choice button destroyed by the real rebuild: "+kind);
        Checks.Add(tag+" "+kind+": actual LCD choice/"+(cancelFirst?"preview/cancel/":"")+"preview/send; "+(duplicate?"same-frame duplicate real send click was a no-op":"send control not hit-testable in the same frame, duplicate impossible")+"; stale choice destroyed.");
    }

    static IEnumerator OpenLetter(XingGuangController ctl,Action<RectTransform> click)
    {
        Get<WindowManager>(Hub.View,"window").CloseWindow();ctl.Window.CloseWindow();
        ctl.runtime.useDiskSave=true;GuardIsolated(ctl.runtime);
        Need(ctl.Epilogue.Show(PrologueDirector.Desk?.layer),"production epilogue presentation opens fixture route");yield return Settle();
        var ep=Get<XgEpilogueView>(ctl.Epilogue,"view");
        yield return Until(()=>!ep.TransitionPlaying,"epilogue naturally settles");
        yield return Tap(Get<XgBtn>(ep,"inbox").button,click,"epilogue inbox");
        var router=ctl.runtime.GetComponent<ChapterOneDesktopRouter>();var window=router.Get("mail").Window;Size960(window);yield return Ready(window);
        var mail=router.Get("mail").Content.GetComponent<MailInboxView>();yield return Until(()=>mail.EndingLetter?.Visible==true,"production bridge presents letter");
        var p=mail.EndingLetter;
        if(!p.Snapshot.saved)yield return Tap(Named(p,"SaveArticle"),click,"save article");
        yield return Tap(Named(p,"OpenLetter"),click,"open letter");
        if(p.Session.View==EndingLetterView.Consent)yield return Tap(Named(p,"ReadFirst"),click,"read first");
        yield return Until(()=>p.Session.View==EndingLetterView.Reading,"production letter reading");
    }
    static EndingLetterPresenter Letter(XingGuangController ctl)=>ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Get("mail").Content.GetComponent<MailInboxView>().EndingLetter;
    static IEnumerator CloseLetter(XingGuangController ctl,EndingLetterPresenter p,Action<RectTransform> click)
    {
        Need(!ctl.Sim.S.chapterComplete,"reading never completes the ending by itself");
        yield return Tap(Named(p,"BackToDesktop"),click,"letter back");ctl.runtime.useDiskSave=false;
    }
    static void LetterText(EndingLetterPresenter p,string articleId,string appendixId,bool en,string aiName)
    {
        var resource=JsonUtility.FromJson<EndingLetterResource>(Resources.Load<TextAsset>(EndingLetterCatalog.ResourcePath).text);
        var article=new EndingLetterCatalog(resource).Get(articleId);
        Need(p.Session.Input?.articleId==articleId,"production bridge opened recommended article "+articleId);
        var texts=p.Root.GetComponentsInChildren<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Paragraph_")).ToArray();
        Need(texts.Length==(appendixId==null?9:10),"exact public paragraph count plus optional private appendix ("+(appendixId??"none")+")");
        foreach(var para in article.paragraphs)Need(texts.Count(t=>t.name=="Paragraph_"+para.id&&t.text==para.Text(en))==1,"public paragraph unchanged: "+para.id);
        foreach(var id in new[]{"LP1","LP2","LP4"})Need(texts.Any(t=>t.name=="Paragraph_"+id)==(id==appendixId),"only the matching private appendix may appear: "+id);
        if(appendixId!=null)
        {
            var a=resource.optionalPersonalAppendices.Single(x=>x.id==appendixId);Need(a.articleId==articleId,"appendix belongs to this article");
            string expected=(en?"Private note · In response to the message you sent\n\n":"私人附记 · 回复你主动发送的消息\n\n")+(en?a.en:a.zh);
            Need(texts[texts.Length-1].name=="Paragraph_"+appendixId&&texts[texts.Length-1].text==expected,"exact labelled appendix follows the public signature: "+appendixId);
        }
        foreach(var t in texts)NoAiName(t.text,aiName,"letter "+t.name);
    }

    // ---- Branches.
    static IEnumerator Lp1(XingGuangController ctl,YYChatView view,bool en,string ai,Action<RectTransform> click,Action<string> capture)
    {
        Bind(ctl,Planned(en,ai,"online"));yield return Settle();yield return OpenYy(view);
        var choices=Hub.EndingPrivateChoices();
        Need(choices.Length==1&&choices[0]==PlanLabel("online",en),"pinning route=online offers only its own plan");
        Need(Visible(view,PlanLabel("public_model",en)).Length==0&&Visible(view,PlanLabel("local_only",en)).Length==0,"other plan buttons not rendered");
        yield return SendViaControls(ctl,view,click,capture,en,ai,PlanLabel("online",en),"plan:online",PlanText("online",en),PlanReply(en),"remaining-"+tag+"-lp1-preview.png",true);
        Need(Hub.EndingPrivateChoices()==null,"no further plan offer after the only chosen plan is sent");
        Need(EndingPrivateDisclosure.PlannedFact(Private)?.evidenceKey=="disclosed_online","LP1 evidence projected only from the sent receipt");
        RealDisk(ctl,"LP1 plan receipt","disclosed_online");SaveReload(ctl,"LP1 plan receipt");yield return Settle();
        Need(Receipts()==1&&Private.receipts[0].kind=="plan:online","slot reload retains the LP1 receipt");
        var snap=Take(ctl);
        // Product state: plan sent, then an online release. The route disclosure stays a separate optional message.
        Bind(ctl,Released(snap,"online","stop",en?"completed":"power_cut"));yield return Settle();
        var after=Hub.EndingPrivateChoices();
        Need(after!=null&&after.Length==1&&after[0]==RouteLabel(en),"committed release removes plan offers; plan receipt never auto-sends the route disclosure");
        Need(!EndingPrivateDisclosure.Has(Private,"route","fixture:release:online"),"no route receipt without its own send");
        yield return OpenLetter(ctl,click);var p=Letter(ctl);LetterText(p,"zhou_open_door","LP1",en,ai);
        yield return EndingLetterUiChecks.ScrollToEnd(p);LetterText(p,"zhou_open_door","LP1",en,ai);
        yield return Capture(capture,"remaining-"+tag+"-lp1-letter-bottom.png");
        yield return CloseLetter(ctl,p,click);
        Result("LP1","pinning online plan sent by controls; exact LP1 appended after nine unchanged zhou_open_door paragraphs; route disclosure still separate");
        Bind(ctl,Released(snap,"public_model","unpinned",null));yield return Settle();
        yield return OpenLetter(ctl,click);p=Letter(ctl);LetterText(p,"zhou_copies",null,en,ai);
        yield return Capture(capture,"remaining-"+tag+"-lp1-mismatch-copies.png");
        yield return CloseLetter(ctl,p,click);
        Result("LP1-mismatch","online plan receipt + public_model release: zhou_copies shows nine public paragraphs, no LP1/LP2");
    }
    static IEnumerator Lp2(XingGuangController ctl,YYChatView view,bool en,string ai,Action<RectTransform> click,Action<string> capture)
    {
        Bind(ctl,Planned(en,ai,""));yield return Settle();yield return OpenYy(view);
        Need(Hub.EndingPrivateChoices().SequenceEqual(new[]{PlanLabel("online",en),PlanLabel("public_model",en),PlanLabel("local_only",en)}),"asked phase without route offers three player-side plans");
        yield return SendViaControls(ctl,view,click,capture,en,ai,PlanLabel("online",en),"plan:online",PlanText("online",en),PlanReply(en),null,false);
        Need(Hub.EndingPrivateChoices().SequenceEqual(new[]{PlanLabel("public_model",en),PlanLabel("local_only",en)}),"sent plan disappears, unsent plans remain optional");
        yield return SendViaControls(ctl,view,click,capture,en,ai,PlanLabel("public_model",en),"plan:public_model",PlanText("public_model",en),PlanReply(en),"remaining-"+tag+"-lp2-preview.png",true);
        Need(Hub.EndingPrivateChoices().SequenceEqual(new[]{PlanLabel("local_only",en)}),"only the unsent local plan remains");
        Need(EndingPrivateDisclosure.PlannedFact(Private)?.evidenceKey=="disclosed_public_package","latest actually sent plan wins");
        RealDisk(ctl,"LP2 two plan receipts","disclosed_public_package");SaveReload(ctl,"LP2 two plan receipts");yield return Settle();
        Need(Private.receipts.Select(r=>r.kind).SequenceEqual(new[]{"plan:online","plan:public_model"}),"slot reload retains both receipts in order");
        var snap=Take(ctl);
        Bind(ctl,Released(snap,"public_model","stop",null));yield return Settle();
        yield return OpenLetter(ctl,click);var p=Letter(ctl);LetterText(p,"zhou_copies","LP2",en,ai);
        yield return EndingLetterUiChecks.ScrollToEnd(p);LetterText(p,"zhou_copies","LP2",en,ai);
        yield return Capture(capture,"remaining-"+tag+"-lp2-letter-bottom.png");
        yield return CloseLetter(ctl,p,click);
        Result("LP2","online then public_model plans sent by controls; exact LP2 appended after nine unchanged zhou_copies paragraphs");
        Bind(ctl,Released(snap,"online","unpinned","power_cut"));yield return Settle();
        yield return OpenLetter(ctl,click);p=Letter(ctl);LetterText(p,"zhou_open_door",null,en,ai);
        yield return Capture(capture,"remaining-"+tag+"-lp2-latest-plan-open-door.png");
        yield return CloseLetter(ctl,p,click);
        Result("LP2-latest-plan","earlier online plan superseded by later public_model plan: zhou_open_door shows no LP1 (product PlannedFact = latest)");
    }
    static IEnumerator CrossSave(XingGuangController ctl,YYChatView view,bool en,string ai,Action<RectTransform> click)
    {
        var a=Planned(en,ai,"");Bind(ctl,a);yield return Settle();yield return OpenYy(view);
        var aState=Hub.S;int aCount=Zhou.messages.Count;
        yield return Tap(ByLabel(view,PlanLabel("public_model",en)),click,"cross-save choice");
        var panel=Preview(view);Need(panel!=null,"preview open on save A");
        var send=ByLabel(panel,SendLabel(en));var stale=Get<EndingPrivatePreview>(view,"endingPrivatePreview");
        RequireForeground("before cross-save hit-test");
        foreach(var frame in LcdActualUiProbe.WaitForClick((RectTransform)send.transform,"ending private remaining cross-save send"))yield return frame;
        var b=Planned(en,ai,"");Bind(ctl,b);int bCount=Zhou.messages.Count; // same frame as the click below
        bool clicked=false;
        if(send!=null&&send.gameObject.activeInHierarchy&&LcdActualUiProbe.CanClick((RectTransform)send.transform,out _))
        {RequireForeground("cross-save send click");click((RectTransform)send.transform);clicked=true;}
        yield return Settle();
        Need(Preview(view)==null,"save switch dismisses preview");
        Need((aState.endingPrivate?.receipts?.Count??0)==0&&aState.conversations.Find(c=>c.id==YYChatHub.LaoZhou).messages.Count==aCount,"previous save receives no receipt or message");
        Need(Receipts()==0&&Zhou.messages.Count==bCount,"new save receives nothing from the old save's control");
        // Synthetic negative, explicitly not a control: captured stale preview object passed to the live hub.
        Need(!Hub.SendEndingPrivate(stale)&&Receipts()==0,"[synthetic] captured stale preview rejected by live hub");
        Result("cross-save",(clicked?"same-frame real LCD send click after save switch was a no-op":"save switch removed hit-testability in the same frame; no click possible")+"; captured stale preview rejected");
    }
    static IEnumerator RouteBranch(XingGuangController ctl,YYChatView view,bool en,string ai,string route,string pin,string night,Action<RectTransform> click,Action<string> capture)
    {
        string name=route=="online"?"R1":"R2",lower=name.ToLowerInvariant(),letter=XgEpilogueFlow.RecommendedLetter(route),scope="fixture:release:"+route;
        var snap=FromHouse(Planned(en,ai,""));
        if(route=="online")
        {
            Bind(ctl,Released(snap,route,pin,null,enterEpilogue:false));yield return Settle();
            Need(ctl.Sim.S.epilogue.phase=="launch_night"&&Hub.EndingPrivateChoices()==null,"committed online release before its finished night offers no disclosure and no stale plan");
            Result("R1-before-night","online release without launch-night receipt: no private offer");
        }
        Bind(ctl,Released(snap,route,pin,night));yield return Settle();yield return OpenYy(view);
        var choices=Hub.EndingPrivateChoices();
        Need(choices.Length==1&&choices[0]==RouteLabel(en),"only the route disclosure is offered first; shutdown is never bundled");
        Need(EndingPrivateDisclosure.PlannedFact(Private)==null,"no planned evidence without a plan receipt");
        var replies=EndingPrivateDisclosure.RouteReplies(route,en);
        Need(replies.Length==3&&!replies.Contains(EndingPrivateDisclosure.ShutdownReply(pin,en)),"route replies contain no shutdown-card reaction");
        yield return SendViaControls(ctl,view,click,capture,en,ai,RouteLabel(en),"route",RouteText(route,en),replies,"remaining-"+tag+"-"+lower+"-preview.png",true);
        var r=Private.receipts.Last();Need(r.scope==scope&&r.route==route,"route receipt bound to this release receipt");
        Need(EndingPrivateDisclosure.PlannedFact(Private)==null,"route receipt never manufactures planned appendix evidence");
        Need(!EndingPrivateDisclosure.Has(Private,"shutdown",scope),"shutdown not disclosed by route send");
        yield return Until(()=>Texts(view).Contains(replies[2]),"YY view renders Lao Zhou's exact final route reply");
        yield return Capture(capture,"remaining-"+tag+"-"+lower+"-sent.png");
        choices=Hub.EndingPrivateChoices();
        Need(choices!=null&&choices.Length==1&&choices[0]==ShutdownLabel(en),"shutdown card is a separate later preview");
        yield return SendViaControls(ctl,view,click,capture,en,ai,ShutdownLabel(en),"shutdown",ShutdownText(pin,en),new[]{EndingPrivateDisclosure.ShutdownReply(pin,en)},"remaining-"+tag+"-"+lower+"-shutdown-preview.png",true);
        Need(Hub.EndingPrivateChoices()==null,"no further private offers after route and shutdown");
        RealDisk(ctl,name+" route+shutdown receipts",null);SaveReload(ctl,name+" route+shutdown receipts");yield return Settle();
        Need(Private.receipts.Select(x=>x.kind).SequenceEqual(new[]{"route","shutdown"})&&Hub.EndingPrivateChoices()==null,"slot reload retains both receipts; nothing re-offered");
        yield return OpenLetter(ctl,click);var p=Letter(ctl);LetterText(p,letter,null,en,ai);
        yield return Capture(capture,"remaining-"+tag+"-"+lower+"-letter-no-appendix.png");
        yield return CloseLetter(ctl,p,click);
        Result(name,route+" player release, pin="+pin+(route=="online"?", night="+night:"")+": route then shutdown sent by controls with exact replies; "+letter+" has nine public paragraphs and no appendix");
    }

    // Composable entry for a frame driver that only advances the returned iterator (same contract as the old suite).
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture,Action captureFailure=null)
        => Flatten(RunFrames(ctl,language,click,capture,captureFailure));
    static IEnumerator Flatten(IEnumerator root)
    {
        var stack=new Stack<IEnumerator>();stack.Push(root);
        try
        {
            while(stack.Count>0)
            {
                var current=stack.Peek();
                if(!current.MoveNext()){stack.Pop();(current as IDisposable)?.Dispose();continue;}
                if(current.Current is IEnumerator child){stack.Push(child);continue;}
                yield return current.Current;
            }
        }
        finally
        {
            List<Exception> cleanup=null;
            while(stack.Count>0)
            {
                try{(stack.Pop() as IDisposable)?.Dispose();}
                catch(Exception error){if(cleanup==null)cleanup=new List<Exception>();cleanup.Add(error);}
            }
            if(cleanup!=null)throw new AggregateException("Ending private remaining UI nested routine cleanup failed",cleanup);
        }
    }
    static IEnumerator RunFrames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture,Action captureFailure)
    {
        Checks.Clear();NamedResults.Clear();AssertionCount=0;tag="setup";
        Need(ctl?.runtime!=null&&Hub?.View!=null,"real Main controller and YY view available");GuardIsolated(ctl.runtime);
        yield return EndingSmallAActualUiChecks.Foreground();
        var original=ctl.runtime.Sim;bool oldDisk=ctl.runtime.useDiskSave,oldTick=ctl.runtime.tickEnabled,oldTest=ctl.runtime.TestMode;
        var view=Hub.View;var yw=Get<WindowManager>(view,"window");Vector2 oldSize=yw.windowContainer.rect.size;
        var mailWindow=ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Get("mail").Window;Vector2 oldMailSize=mailWindow.windowContainer.rect.size;
        var yy=Hub;
        Need(!AiJoinsYy.Playing,"AI joining YY must already be idle before fixture isolation");
        var loans=UnityEngine.Object.FindObjectsByType<ZhouCardLoan>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Need(loans.All(loan=>!Get<bool>(loan,"busy")),"card-loan coroutine must already be idle before fixture isolation");
        var quietNames=new HashSet<string>{"ZhouCardLoan","AiJoinsYy","ShopNotices","XgEndingPreparationHost",
            "XgResidentSpider","XgLifeEventCard","XgAppAcquisitionHost","XgDailyUnlockNotifier","GameEraClock",
            "XgEraDeliveryDesk","XgEraAdvantageBridge","XgPublicDiscourseNotifier"};
        var drivers=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            // Unlike the old suite, Tieba's ambient Update is also frozen (as EndingSmallA does): these assertions span
            // many frames and must not see unrelated forum traffic. Its save rebinding and ZhouObserveYy stay synchronous.
            .Where(b=>b==ctl||b==yy||b==TiebaHub.Instance||quietNames.Contains(b.GetType().Name)).ToArray();
        var enabled=drivers.Select(driver=>driver.enabled).ToArray();
        bool completed=false;
        try
        {
            // Real YY/mail views, epilogue director/bridge, LCD relay and EventSystem stay active.
            foreach(var driver in drivers)driver.enabled=false;
            foreach(bool en in new[]{false,true})
            {
                tag=en?"en":"zh";language(en);
                string ai=en?"Quill":"小砚"; // Dynamic AI-name fixture; private copy has no name slot and must not gain one.
                Need(AiNamePolicy.Problem(ai,en)==null,"AI-name fixture is a valid player name");
                yield return Lp1(ctl,view,en,ai,click,capture);
                yield return Lp2(ctl,view,en,ai,click,capture);
                yield return CrossSave(ctl,view,en,ai,click);
                yield return RouteBranch(ctl,view,en,ai,"online",en?"stop":"no_stop",en?"power_cut":"completed",click,capture);
                yield return RouteBranch(ctl,view,en,ai,"public_model",en?"no_stop":"unpinned",null,click,capture);
                yw.CloseWindow();yield return Settle();
            }
            tag="summary";var missing=MissingResults();
            Need(missing.Length==0,"every named result recorded; missing: "+string.Join(", ",missing));
            completed=true;
        }
        finally
        {
            // Iterator exceptions unwind here before the runner catches them; capture before restoring UI.
            try{if(!completed)captureFailure?.Invoke();}
            finally
            {
            try
            {
                ctl.Epilogue?.Hide();yw.CloseWindow();mailWindow.CloseWindow();
                ctl.runtime.SetSimulationForTests(original);typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{oldTest});
                ctl.runtime.useDiskSave=oldDisk;ctl.runtime.tickEnabled=oldTick;
                yw.windowContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,oldSize.x);yw.windowContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,oldSize.y);
                mailWindow.windowContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,oldMailSize.x);mailWindow.windowContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,oldMailSize.y);
            }
            finally
            {
                List<Exception> cleanup=null;
                for(int i=0;i<drivers.Length;i++)
                {
                    try{if(drivers[i]!=null)drivers[i].enabled=enabled[i];}
                    catch(Exception error){if(cleanup==null)cleanup=new List<Exception>();cleanup.Add(error);}
                }
                if(cleanup!=null)throw new AggregateException("Ending private remaining UI driver restoration failed",cleanup);
            }
            }
        }
    }
}
#endif
