#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Composable serial-owner check only: no menu runner, scene boot, forced focus, or model startup.
// Contact, stage, SI read, committed routes and long AI answer are explicit fixtures, NOT naturally earned progress.
// Disclosure receipts must be earned by real LCD preview/send buttons and persisted by real runtime slot APIs.
public static class EndingPrivateActualUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public const string Scope="Isolated Main/LCD fixtures, Chinese and English at 960x600 native windows: actual preview/cancel/send buttons, native close and same-save language-stale button, cross-save rejection, real slot-6 save/reload, production mailbox LP4 appendix versus nine unchanged public paragraphs without disclosure, actual LCD wheel for long R3 preview/cancel/forward. Existing contact acceptance, stage, SI read, release prerequisites/route, and long AI answer are declared fixtures; no natural progression, model inference, physical OS input, completed ending or performance claim. The runner owns fresh save/profile isolation, model/basic-story suppression, real foreground focus and editor restoration; this suite snapshots and freezes independent narrative/ambient drivers and never forces input permission. A captured stale preview call and serialized history-trimming copy are explicitly synthetic negative checks.";
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount {get;private set;}
    static void Need(bool value,string message){AssertionCount++;if(!value)throw new InvalidOperationException("Ending private UI: "+message);}
    static T Get<T>(object owner,string field)=>(T)owner.GetType().GetField(field,F).GetValue(owner);
    static IEnumerator Settle(int frames=4){Canvas.ForceUpdateCanvases();while(frames-->0)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerator Until(Func<bool> ready,string label,double seconds=12)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!ready()){Need(EditorApplication.timeSinceStartup<end,label);yield return null;}yield return Settle();}
    static string Label(Button b)=>b.GetComponentInChildren<TMP_Text>(true)?.text??"";
    static Button ByLabel(Component root,string label)
    {
        var list=root.GetComponentsInChildren<Button>(true).Where(b=>b.gameObject.activeInHierarchy&&Label(b)==label).ToArray();
        Need(list.Length==1,"one visible button: "+label);return list[0];
    }
    static Button Named(Component root,string name)
    {
        var list=root.GetComponentsInChildren<Button>(true).Where(b=>b.gameObject.activeInHierarchy&&b.name==name).ToArray();
        Need(list.Length==1,"one visible named button: "+name);return list[0];
    }
    static IEnumerator Tap(Button b,Action<RectTransform> click)
    {
        Need(b!=null&&b.IsInteractable(),"real button enabled: "+b?.name);
        foreach(var frame in LcdActualUiProbe.WaitForClick((RectTransform)b.transform,"ending private "+b.name))yield return frame;
        click((RectTransform)b.transform);yield return Settle();
    }
    static IEnumerator TapNativeClose(WindowManager window,Action<RectTransform> click)
    {
        // YY retains DreamOS caption controls; it does not use the lab's ChapterOneClose button.
        var buttons=window.windowContainer.GetComponentsInChildren<ButtonManager>(true).Where(b=>
            b.isActiveAndEnabled&&b.isInteractable&&Enumerable.Range(0,b.onClick.GetPersistentEventCount())
            .Any(i=>b.onClick.GetPersistentTarget(i)==window&&b.onClick.GetPersistentMethodName(i)=="CloseWindow")).ToArray();
        Need(buttons.Length==1,"one live native YY caption bound to CloseWindow");
        var rect=(RectTransform)buttons[0].transform;
        foreach(var frame in LcdActualUiProbe.WaitForClick(rect,"native YY close"))yield return frame;
        click(rect);yield return Settle();
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
    static int Receipts()=>YYChatHub.Instance.S.endingPrivate?.receipts?.Count??0;
    static IEnumerator OpenYy(YYChatView view)
    {
        view.EnsureReady();view.OnOpened(YYChatHub.LaoZhou);var w=Get<WindowManager>(view,"window");Size960(w);
        w.OpenWindow();w.FocusToWindow();yield return Ready(w);yield return Until(()=>YYChatHub.Instance.EndingPrivateChoices()?.Length>0,"optional disclosure choices available");
    }
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
        Need(YYChatHub.Instance.BoundTo(house)&&TiebaHub.Instance.ZhouStateBoundTo(house)&&ctl.BoundTo(house),"all live hosts bind exact fixture save");
    }
    static ChapterOneSim PlannedFixture(bool en)
    {
        var sim=new XgSim();sim.BeginOpening();sim.English=en;
        sim.S.stage=6;sim.S.fullOpen=true;sim.S.reduceFx=true;sim.S.mute=true;sim.S.teachingFlow=false;
        sim.S.spiderIntroDone=sim.S.firstWordsSaid=true;
        sim.S.teachIntroSaid=sim.S.teachThirdSaid=sim.S.teachTreeSaid=sim.S.teachTrainSaid=true;
        sim.MarkAfterthought("first.drift");
        sim.S.endingPreparation=new XgEndingPreparationState {phase="asked",siReadReceipt="fixture:si-read",siQuestionAnnounced=true};
        var forum=new ForumState();forum.laoZhouQa.yyLinkSource="tieba_dm_accept";forum.laoZhouQa.yyInvitationSent=forum.laoZhouQa.legacyChecked=true;
        forum.laoZhouQa.first.choice=forum.laoZhouQa.second.choice=1;
        forum.laoZhouQa.first.yyContinued=forum.laoZhouQa.first.responseSent=forum.laoZhouQa.second.responseSent=true;
        var yy=new YYState {lingguangUnlocked=true};foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(sim.S);house.S.forumState=JsonUtility.ToJson(forum);house.S.chatState=JsonUtility.ToJson(yy);
        return house;
    }
    static ChapterOneSim ReleasedFixture(XingGuangController ctl,bool ai=false,bool clearDisclosure=false)
    {
        // Snapshot the real just-sent exchange; only release progression below is a declared fixture.
        var house=new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(ctl.runtime.Sim.S)));
        var state=JsonUtility.FromJson<XgState>(JsonUtility.ToJson(ctl.Sim.S));
        var yy=JsonUtility.FromJson<YYState>(JsonUtility.ToJson(YYChatHub.Instance.S));
        if(clearDisclosure)yy.endingPrivate=new EndingPrivateState();
        state.releaseOutcome=new ReleaseOutcome {route="local_only",decisionOwner=ai?"ai":"player",shutdownPin="stop",releaseReceiptId="fixture:released",committedAt=20161231,
            prerequisites=new XgReleasePrerequisites {memoryCardsReceiptId="fixture:memories",redTeamOneReceiptId="fixture:one",redTeamTwoReceiptId="fixture:two",
                redTeamThreeReceiptId="fixture:three",fixedThreeSlotsReceiptId="fixture:three-pins",aiRouteDecisionReceiptId=ai?"fixture:decision":"",aiChosenRoute=ai?"local_only":""}};
        state.epilogue=new EpilogueState {phase="letter",letterId="zhou_unpressed_button",displayDate=20170613,revision=1};
        state.endingPreparation.route="local_only";state.endingPreparation.decisionOwner=ai?"ai":"player";
        if(ai)
        {
            state.endingPreparation.aiDecision=new XgEndingAiDecision {id="fixture:decision",route="local_only",
                // Production decisions pass this guard before persistence (including Unicode normalization).
                line=XgEndingTextGuard.Filter(string.Join("\n",Enumerable.Range(1,20).Select(n=>"验证夹具原话 "+n+"：我想先留在这里，弄清楚自己还不会什么。")),false),
                lineEn=XgEndingTextGuard.Filter(string.Join("\n",Enumerable.Range(1,20).Select(n=>"Fixture statement "+n+": I want to stay here for now and understand what I still cannot do.")),true)};
            Need(!string.IsNullOrEmpty(state.endingPreparation.aiDecision.line)&&!string.IsNullOrEmpty(state.endingPreparation.aiDecision.lineEn),"long answer fixture passes the production dynamic-text guard");
        }
        house.S.labState=JsonUtility.ToJson(state);house.S.chatState=JsonUtility.ToJson(yy);return house;
    }
    static void SaveReload(XingGuangController ctl)
    {
        GuardIsolated(ctl.runtime);ctl.runtime.useDiskSave=true;
        Need(ctl.runtime.SaveSlot(6),"real isolated slot 6 saves: "+ctl.runtime.SaveStatus);
        Need(ctl.runtime.LoadArchive(6),"real isolated slot 6 reloads: "+ctl.runtime.SaveStatus);
        ctl.runtime.tickEnabled=false;ctl.runtime.useDiskSave=false;
    }
    static IEnumerator WheelToEnd(ScrollRect scroll,Func<bool> done)
    {
        var bridge=UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        Need(bridge?.inputSurface!=null,"real LCD input surface available");
        var pointer=typeof(FilesWindowUiChecks).GetMethod("Pointer",BindingFlags.Static|BindingFlags.NonPublic);
        Need(pointer!=null,"existing LCD coordinate mapper available");
        for(int i=0;i<160&&!done();i++)
        {
            Canvas.ForceUpdateCanvases();var data=(PointerEventData)pointer.Invoke(null,new object[]{scroll.viewport});
            bridge.inputSurface.OnPointerEnter(data);
            var hover=(PointerEventData)typeof(AeroLcdInputSurface).GetField("hoverData",F).GetValue(bridge.inputSurface);
            Need(hover?.pointerCurrentRaycast.gameObject!=null&&ExecuteEvents.GetEventHandler<IScrollHandler>(hover.pointerCurrentRaycast.gameObject)==scroll.gameObject,"actual LCD wheel hits expected preview scroll");
            data.scrollDelta=new Vector2(0,-3);bridge.inputSurface.OnScroll(data);yield return Settle(3);
        }
        Need(done(),"real LCD wheel reaches preview end");
    }
    static IEnumerator OpenLetter(XingGuangController ctl,Action<RectTransform> click)
    {
        Get<WindowManager>(YYChatHub.Instance.View,"window").CloseWindow();ctl.Window.CloseWindow();
        ctl.runtime.useDiskSave=true;Need(ctl.Epilogue.Show(PrologueDirector.Desk?.layer),"production epilogue presentation opens fixture route");yield return Settle();
        var ep=Get<XgEpilogueView>(ctl.Epilogue,"view");
        yield return Until(()=>!ep.TransitionPlaying,"epilogue naturally settles");
        yield return Tap(Get<XgBtn>(ep,"inbox").button,click);
        var router=ctl.runtime.GetComponent<ChapterOneDesktopRouter>();var window=router.Get("mail").Window;Size960(window);yield return Ready(window);
        var mail=router.Get("mail").Content.GetComponent<MailInboxView>();yield return Until(()=>mail.EndingLetter?.Visible==true,"production bridge presents letter");
        var p=mail.EndingLetter;
        if(!p.Snapshot.saved)yield return Tap(Named(p,"SaveArticle"),click);
        yield return Tap(Named(p,"OpenLetter"),click);
        if(p.Session.View==EndingLetterView.Consent)yield return Tap(Named(p,"ReadFirst"),click);
        yield return Until(()=>p.Session.View==EndingLetterView.Reading,"production letter reading");
    }
    static void PublicTextIntact(EndingLetterPresenter p,bool appendix,bool en)
    {
        var catalog=new EndingLetterCatalog(JsonUtility.FromJson<EndingLetterResource>(Resources.Load<TextAsset>(EndingLetterCatalog.ResourcePath).text));
        var article=catalog.Get("zhou_unpressed_button");
        var texts=p.Root.GetComponentsInChildren<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Paragraph_")).ToArray();
        Need(texts.Length==(appendix?10:9),"exact public paragraph count plus optional private appendix");
        foreach(var para in article.paragraphs)Need(texts.Count(t=>t.name=="Paragraph_"+para.id&&t.text==para.Text(en))==1,"public paragraph exact: "+para.id);
        Need(texts.Any(t=>t.name=="Paragraph_LP4")==appendix,"private note appears only with actual disclosure receipt");
    }
    // Sidebar's frame driver advances only the returned iterator. Flatten child routines here so waits,
    // actual taps and wheel gestures cannot be skipped by a caller that does not inspect Current.
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
        => Flatten(RunFrames(ctl,language,click,capture));
    static IEnumerator Flatten(IEnumerator root)
    {
        var stack=new Stack<IEnumerator>();stack.Push(root);
        try
        {
            while(stack.Count>0)
            {
                var current=stack.Peek();
                if(!current.MoveNext())
                {stack.Pop();(current as IDisposable)?.Dispose();continue;}
                if(current.Current is IEnumerator child){stack.Push(child);continue;}
                yield return current.Current;
            }
        }
        finally
        {
            // An interrupted/failed child must not prevent the parent fixture's finally from restoring its owner.
            List<Exception> cleanup=null;
            while(stack.Count>0)
            {
                try{(stack.Pop() as IDisposable)?.Dispose();}
                catch(Exception error){if(cleanup==null)cleanup=new List<Exception>();cleanup.Add(error);}
            }
            if(cleanup!=null)throw new AggregateException("Ending private UI nested routine cleanup failed",cleanup);
        }
    }
    static IEnumerator RunFrames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        Checks.Clear();AssertionCount=0;Need(ctl?.runtime!=null&&YYChatHub.Instance?.View!=null,"real Main controller and YY view available");GuardIsolated(ctl.runtime);
        // The caller owns single-instance setup, real focus, models/basic-story isolation and final report/error capture.
        var original=ctl.runtime.Sim;bool oldDisk=ctl.runtime.useDiskSave,oldTick=ctl.runtime.tickEnabled,oldTest=ctl.runtime.TestMode;
        var view=YYChatHub.Instance.View;var yw=Get<WindowManager>(view,"window");Vector2 oldSize=yw.windowContainer.rect.size;
        var mailWindow=ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Get("mail").Window;Vector2 oldMailSize=mailWindow.windowContainer.rect.size;
        var yy=YYChatHub.Instance;
        // Disabling a MonoBehaviour does not stop an already-running coroutine. Reject such owners before
        // touching them; never clear story focus, input locks, typing, or coroutine state to manufacture a pass.
        Need(!AiJoinsYy.Playing,"AI joining YY must already be idle before fixture isolation");
        var loans=UnityEngine.Object.FindObjectsByType<ZhouCardLoan>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Need(loans.All(loan=>!Get<bool>(loan,"busy")),"card-loan coroutine must already be idle before fixture isolation");
        var quietNames=new HashSet<string>{"ZhouCardLoan","AiJoinsYy","ShopNotices","XgEndingPreparationHost",
            "XgResidentSpider","XgLifeEventCard","XgAppAcquisitionHost","XgDailyUnlockNotifier","GameEraClock",
            "XgEraDeliveryDesk","XgEraAdvantageBridge","XgPublicDiscourseNotifier"};
        var drivers=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(b=>b==ctl||b==yy||quietNames.Contains(b.GetType().Name)).ToArray();
        var enabled=drivers.Select(driver=>driver.enabled).ToArray();
        try
        {
            // Keep the real YY/mail views, epilogue director/bridge, LCD relay and EventSystem active.
            // Direct disclosure sends and runtime save/rebind events still run while ambient Updates are frozen.
            foreach(var driver in drivers)driver.enabled=false;
            foreach(bool en in new[]{false,true})
            {
                string tag=en?"en":"zh";language(en);
                var legacy=PlannedFixture(en);var legacyForum=JsonUtility.FromJson<ForumState>(legacy.S.forumState);
                legacyForum.laoZhouQa.yyLinkSource="legacy_existing";legacy.S.forumState=JsonUtility.ToJson(legacyForum);
                Bind(ctl,legacy);yield return Settle();Need(YYChatHub.Instance.EndingPrivateChoices()==null,"legacy contact cannot offer a new private disclosure");
                Bind(ctl,PlannedFixture(en));yield return Settle();yield return OpenYy(view);
                string plan=YYChatHub.Instance.EndingPrivateChoices().Last();int before=YYChatHub.Instance.Conversation(YYChatHub.LaoZhou).messages.Count;
                var priorLanguageButton=ByLabel(view,plan);
                foreach(var frame in LcdActualUiProbe.WaitForClick((RectTransform)priorLanguageButton.transform,"rendered disclosure before language change"))yield return frame;
                language(!en);click((RectTransform)priorLanguageButton.transform); // Same-frame real LCD click, before deferred redraw.
                Need(Preview(view)==null&&Receipts()==0&&YYChatHub.Instance.Conversation(YYChatHub.LaoZhou).messages.Count==before,"stale language action never sends its UI label as ordinary chat");
                language(en);yield return Settle();
                yield return Tap(ByLabel(view,plan),click);Need(Preview(view)!=null&&Receipts()==0,"preview does not disclose or write a receipt");
                capture("ending-private-"+tag+"-plan-preview.png");
                yield return Tap(ByLabel(Preview(view),en?"Cancel":"取消"),click);
                Need(Preview(view)==null&&Receipts()==0&&YYChatHub.Instance.Conversation(YYChatHub.LaoZhou).messages.Count==before,"cancel leaves transcript and evidence unchanged");
                yield return Tap(ByLabel(view,plan),click);yield return TapNativeClose(yw,click);
                yield return Until(()=>Preview(view)==null,"native close dismisses preview");Need(Receipts()==0,"native close does not send");
                yield return OpenYy(view);yield return Tap(ByLabel(view,plan),click);
                var stale=Get<EndingPrivatePreview>(view,"endingPrivatePreview");Bind(ctl,PlannedFixture(en));yield return Settle();
                Need(Preview(view)==null&&!YYChatHub.Instance.SendEndingPrivate(stale)&&Receipts()==0,"save switch invalidates preview and rejects captured callback");
                yield return OpenYy(view);plan=YYChatHub.Instance.EndingPrivateChoices().Last();yield return Tap(ByLabel(view,plan),click);
                var send=ByLabel(Preview(view),en?"Send the text above":"发送以上原文");yield return Tap(send,click);
                Need(Receipts()==1,"actual send writes exactly one receipt");var receipt=YYChatHub.Instance.S.endingPrivate.receipts.Single();
                Need(YYChatHub.Instance.Conversation(YYChatHub.LaoZhou).messages.Any(m=>m.from==YYChatHub.Me&&m.text==receipt.text&&m.dailyEventId==receipt.sourceRef),"receipt resolves to actual sent YY message");
                string receiptId=receipt.id;SaveReload(ctl);yield return Settle();Need(Receipts()==1&&YYChatHub.Instance.S.endingPrivate.receipts[0].id==receiptId,"real slot reload retains exact disclosure receipt");
                var trimmed=JsonUtility.FromJson<YYState>(JsonUtility.ToJson(YYChatHub.Instance.S));trimmed.conversations.Clear();
                Need(EndingPrivateDisclosure.PlannedFact(JsonUtility.FromJson<YYState>(JsonUtility.ToJson(trimmed)).endingPrivate)?.sourceRef==receipt.sourceRef,
                    "explicit serialized history-trimming fixture retains its independent disclosure transcript");
                Need(!YYChatHub.Instance.EndingPrivateChoices().Contains(plan),"already-sent choice absent after reload");
                Bind(ctl,ReleasedFixture(ctl));yield return Settle();yield return OpenLetter(ctl,click);
                var p=ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Get("mail").Content.GetComponent<MailInboxView>().EndingLetter;
                PublicTextIntact(p,true,en);yield return EndingLetterUiChecks.ScrollToEnd(p);PublicTextIntact(p,true,en);
                capture("ending-private-"+tag+"-lp4-bottom.png");Need(!ctl.Sim.S.chapterComplete,"reading appendix never completes by itself");
                yield return Tap(Named(p,"BackToDesktop"),click);ctl.runtime.useDiskSave=false;
                Bind(ctl,ReleasedFixture(ctl,clearDisclosure:true));yield return Settle();yield return OpenLetter(ctl,click);
                p=ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Get("mail").Content.GetComponent<MailInboxView>().EndingLetter;
                PublicTextIntact(p,false,en);capture("ending-private-"+tag+"-no-disclosure.png");yield return Tap(Named(p,"BackToDesktop"),click);ctl.runtime.useDiskSave=false;
                Bind(ctl,ReleasedFixture(ctl,ai:true,clearDisclosure:true));yield return Settle();yield return OpenYy(view);
                yield return Tap(ByLabel(view,YYChatHub.Instance.EndingPrivateChoices().Single()),click);
                yield return Tap(ByLabel(Preview(view),en?"Send the text above":"发送以上原文"),click);
                Need(Receipts()==1&&YYChatHub.Instance.S.endingPrivate.receipts[0].kind=="ask","asking disclosure does not forward actual answer");
                string forward=en?"Optional: preview and forward its exact answer…":"可选：预览并转发它的原话…";
                Need(YYChatHub.Instance.EndingPrivateChoices().Contains(forward),"explicit AI answer offer exists; never substitute the shutdown offer");
                yield return Tap(ByLabel(view,forward),click);
                var panel=Preview(view);var body=panel.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="ExactMessage");
                string exact=en?ctl.Sim.S.endingPreparation.aiDecision.lineEn:ctl.Sim.S.endingPreparation.aiDecision.line;
                Need(body.text.EndsWith(exact,StringComparison.Ordinal)&&!body.richText,"long exact AI answer visible as plain text");
                var forwardSend=ByLabel(panel,en?"Send the text above":"发送以上原文");Need(!forwardSend.IsInteractable(),"long answer cannot send before preview scroll");
                yield return WheelToEnd(panel.GetComponentInChildren<ScrollRect>(),()=>forwardSend.IsInteractable());
                capture("ending-private-"+tag+"-r3-preview-bottom.png");yield return Tap(ByLabel(panel,en?"Cancel":"取消"),click);
                Need(Receipts()==1,"scrolling and canceling the AI answer never forwards it");
                yield return Tap(ByLabel(view,forward),click);panel=Preview(view);forwardSend=ByLabel(panel,en?"Send the text above":"发送以上原文");
                yield return WheelToEnd(panel.GetComponentInChildren<ScrollRect>(),()=>forwardSend.IsInteractable());yield return Tap(forwardSend,click);
                Need(Receipts()==2&&YYChatHub.Instance.S.endingPrivate.receipts.Last().kind=="answer"&&YYChatHub.Instance.S.endingPrivate.receipts.Last().text.EndsWith(exact,StringComparison.Ordinal),"only confirmed forward stores exact AI answer");
                Need(!EndingPrivateDisclosure.Has(YYChatHub.Instance.S.endingPrivate,"shutdown","fixture:released"),"AI answer does not invent pinned shutdown disclosure");
                Checks.Add(tag+": actual LCD preview/cancel/native-close/save-switch/send; real slot save/reload; production LP4/no-disclosure reading at 960x600; exact long R3 preview wheel/cancel/confirm. Progress/contact/route/AI answer were declared fixtures.");
                yw.CloseWindow();yield return Settle();
            }
        }
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
                // Restore the caller's exact enabled flags even when fixture/window cleanup throws.
                List<Exception> cleanup=null;
                for(int i=0;i<drivers.Length;i++)
                {
                    try{if(drivers[i]!=null)drivers[i].enabled=enabled[i];}
                    catch(Exception error){if(cleanup==null)cleanup=new List<Exception>();cleanup.Add(error);}
                }
                if(cleanup!=null)throw new AggregateException("Ending private UI driver restoration failed",cleanup);
            }
        }
    }
}
#endif
