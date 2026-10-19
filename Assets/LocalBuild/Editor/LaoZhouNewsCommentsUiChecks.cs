#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Auxiliary Frames suite for the existing isolated Sidebar/Main runner. No shared runner modification here.
// Actual shares and Add-on-YY use FilesWindowUiChecks.Click: LCD mapping, hit ownership, pointer down/up.
// Fixture clock, stage, scroll positioning and in-memory JsonUtility rebind are explicit setup, not natural play.
public static class LaoZhouNewsCommentsUiChecks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly string[] Ids = {"lz2_t01_alphago", "lz2_t03_tay", "lz2_t04_resnet"};
    public static readonly List<string> Checks = new List<string>();
    public static int AssertionCount { get; private set; }
    public static int ShareClicks { get; private set; }
    public static int AcceptanceClicks { get; private set; }
    public const string Scope = "Isolated Main/LCD candidate: stage1 and 2016-06-06 are fixtures; 12 news shares (3 articles x 2 locales x 2 actual contact routes), optional invitation acceptance through actual LCD, two same-frame LCD share taps, passive article/read-confirmation/AI-forwarding privacy checks, in-memory JsonUtility save-owner replacement and opposite-language revisit, existing private-chat rendering. FilesWindowUiChecks.Click uses real LCD pointer routing and raycast ownership, not physical OS input. Scroll placement is explicit fixture setup. Independent story/economic/news producers are frozen; production share handler, Lao Zhou invitation, views, input gates and idle checks are retained. No model, disk persistence, natural progression or full playthrough claim.";

    static T Get<T>(object o, string name) => (T)o.GetType().GetField(name,F).GetValue(o);
    static void Need(bool yes, string reason)
    { AssertionCount++; if(!yes)throw new InvalidOperationException("Lao Zhou news LCD: " + reason); }
    static void Done(string text) { Checks.Add(text); Debug.Log("Lao Zhou news LCD: " + text); }
    static IEnumerable<object> Wait(double seconds = .65)
    { double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;Canvas.ForceUpdateCanvases(); }
    static IEnumerable<object> Until(Func<bool> condition, string why, double seconds = 15)
    {
        double end=EditorApplication.timeSinceStartup+seconds;
        while(!condition()){if(EditorApplication.timeSinceStartup>=end)Need(false,why);yield return null;}
        Canvas.ForceUpdateCanvases();yield return null;yield return null;
    }
    static WindowManager Window(Component view) => Get<WindowManager>(view,"window");
    static IEnumerable<object> Ready(Component view)
    {
        var w=Window(view);w.FocusToWindow();
        foreach(var f in Until(()=>
        {
            if(!w.isOn||!w.gameObject.activeInHierarchy)return false;
            foreach(var g in w.GetComponentsInParent<CanvasGroup>(true))if(g.alpha<.95f||!g.interactable||!g.blocksRaycasts)return false;
            var a=w.windowAnimator;return a==null||!a.isActiveAndEnabled||!a.IsInTransition(0)&&a.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"native window naturally interactive: "+w.name))yield return f;
        foreach(var f in Wait())yield return f;
    }
    static string Label(Button button) => button.GetComponentInChildren<TMP_Text>(true)?.text ?? "";
    static Button Find(Component view, Func<string,bool> predicate)
    {
        var found=view.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.IsInteractable()&&predicate(Label(b))).ToArray();
        Need(found.Length==1,"one real live target in "+view.name+"; matches="+found.Length);return found[0];
    }
    static bool HasButton(Component view, string label) => view.GetComponentsInChildren<Button>().Any(b=>b.gameObject.activeInHierarchy&&b.IsInteractable()&&Label(b)==label);
    static TMP_Text Text(Component view, string exact)
    {
        var matches=view.GetComponentsInChildren<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy&&t.text==exact).ToArray();
        Need(matches.Length==1,"one rendered text: "+exact);return matches[0];
    }
    static void Reveal(RectTransform target)
    {
        var scroll=target.GetComponentInParent<ScrollRect>();if(scroll==null)return;
        Canvas.ForceUpdateCanvases();scroll.StopMovement();
        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,target);
        float hidden=scroll.content.rect.height-scroll.viewport.rect.height;
        if(hidden>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-(scroll.viewport.rect.center.y-bounds.center.y)/hidden);
        Canvas.ForceUpdateCanvases();
    }
    static IEnumerable<object> Tap(Button button)
    {
        Reveal((RectTransform)button.transform);yield return null;
        Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"foreground remains real at LCD tap");
        FilesWindowUiChecks.Click((RectTransform)button.transform);
        foreach(var f in Wait())yield return f;
    }
    static IEnumerable<object> Article(BoduView browser, string id, bool en)
    {
        browser.Open("");foreach(var f in Ready(browser))yield return f;
        var a=XgNewsCatalog.Get(id);Need(a!=null,"real catalog contains "+id);
        string title=en?a.titleEn:a.title;
        if(!browser.GetComponentsInChildren<Button>().Any(b=>b.gameObject.activeInHierarchy&&Label(b).Contains(title)))
            foreach(var f in Tap(Find(browser,t=>t.Contains(en?"Older headlines":"更多往日新闻"))))yield return f;
        foreach(var f in Tap(Find(browser,t=>t.Contains(title))))yield return f;
        Need(Get<string>(browser,"page")=="news"&&Get<string>(browser,"newsId")==id,"LCD headline opens exact article "+id);
        Text(browser,en?a.bodyEn:a.body);
    }
    static ChapterOneSim Fixture(bool en)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=en;lab.S.stage=1;lab.S.openingFocus=false;
        lab.S.teachingFlow=false;lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;lab.MarkAfterthought("first.drift");
        var yy=new YYState{lingguangUnlocked=true};foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        var forum=new ForumState();
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);house.S.chatState=JsonUtility.ToJson(yy);house.S.forumState=JsonUtility.ToJson(forum);
        GameCalendar.Advance(house.S,GameCalendar.DayIndex(new DateTime(2016,6,6)));
        house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
        return house;
    }
    static void Bind(XingGuangController ctl, ChapterOneSim house)
    {
        ctl.runtime.SetSimulationForTests(house);
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});
        ctl.runtime.tickEnabled=false;ctl.runtime.useDiskSave=false;
        Need(ctl.BoundTo(house)&&TiebaHub.Instance.ZhouStateBoundTo(house)&&YYChatHub.Instance.BoundTo(house),"all owners synchronously rebound");
    }
    static IEnumerable<object> Idle(TiebaHub forum)
    {
        foreach(var f in Until(()=>!InnerVoice.Busy&&(bool)typeof(TiebaHub).GetMethod("ZhouIdle",F).Invoke(forum,null),
            "production ZhouIdle becomes true without forcing focus/input",30))yield return f;
    }
    static IEnumerable<object> Accept(TiebaHub forum, YYChatHub yy, bool en)
    {
        Need(forum.ZhouQa.yyInvitationSent&&!forum.ZhouYyLinked,"real pending invitation, no preaccepted fixture");
        forum.View.Open("chat",ForumLibrary.LaoZhou);foreach(var f in Ready(forum.View))yield return f;
        foreach(var f in Tap(Find(forum.View,t=>t==LaoZhouQuestions.AcceptLabel(en))))yield return f;
        AcceptanceClicks++;
        Need(forum.ZhouQa.yyLinkSource=="tieba_dm_accept"&&yy.ZhouContactReady,"actual Add-on-YY click establishes contact");
        Need(forum.Conversation(ForumLibrary.LaoZhou).messages.Any(m=>m.from==ForumLibrary.Me&&m.text==LaoZhouQuestions.AcceptLabel(en)),"actual acceptance is a saved player line");
        Window(forum.View).CloseWindow();foreach(var f in Wait())yield return f;
    }
    static int ForumCount(TiebaHub forum) => forum.Conversation(ForumLibrary.LaoZhou).messages.Count;
    static int YyCount(YYChatHub yy) => yy.Conversation(YYChatHub.LaoZhou).messages.Count;
    static int Receipts(TiebaHub forum) => forum.ZhouQa.newsComments?.Count ?? 0;
    static void NoDisclosure(TiebaHub forum,YYChatHub yy,int beforeForum,int beforeYy,string beforeQa,string why)
    {
        Need(ForumCount(forum)==beforeForum&&YyCount(yy)==beforeYy,"no message from "+why);
        Need(JsonUtility.ToJson(forum.ZhouQa)==beforeQa,"no share receipt or knowledge from "+why);
    }
    static IEnumerable<object> Passive(BoduView browser,TiebaHub forum,YYChatHub yy,XingGuangController ctl,string id,bool en)
    {
        int beforeForum=ForumCount(forum),beforeYy=YyCount(yy);string qa=JsonUtility.ToJson(forum.ZhouQa);
        foreach(var f in Article(browser,id,en))yield return f;
        foreach(var f in Wait(1.1))yield return f;
        NoDisclosure(forum,yy,beforeForum,beforeYy,qa,"passive article browsing "+id);
        foreach(var f in Tap(Find(browser,t=>t.Contains(en?"I finished reading this article":"我已读完这篇"))))yield return f;
        Need(ctl.Sim.DailyNewsAlreadyRead(id),"real finished-reading button records private read "+id);
        NoDisclosure(forum,yy,beforeForum,beforeYy,qa,"private finished-reading confirmation "+id);
        if(XgNewsCatalog.Teachable(XgNewsCatalog.Get(id)))
        {
            foreach(var f in Tap(Find(browser,t=>t.Contains(en?"Show it this article":"转给它看"))))yield return f;
            Need(ctl.Sim.NewsAlreadyTaught(id),"real show-to-AI button records private article teaching "+id);
            NoDisclosure(forum,yy,beforeForum,beforeYy,qa,"private AI forwarding "+id);
        }
    }
    static IEnumerable<object> Share(BoduView browser,TiebaHub forum,YYChatHub yy,string id,string channel,bool en,Action<string> capture)
    {
        foreach(var f in Idle(forum))yield return f;
        int beforeForum=ForumCount(forum),beforeYy=YyCount(yy),beforeReceipts=Receipts(forum);
        bool forumOpen=Window(forum.View).isOn,yyOpen=Window(yy.View).isOn;
        var button=Find(browser,t=>t==LaoZhouNewsComments.Label(channel,false,en));
        Reveal((RectTransform)button.transform);yield return null;
        Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"foreground remains real at share");
        // Both deliveries travel through the real LCD, before the next frame destroys the old button.
        FilesWindowUiChecks.Click((RectTransform)button.transform);ShareClicks++;
        FilesWindowUiChecks.Click((RectTransform)button.transform);ShareClicks++;
        foreach(var f in Wait())yield return f;
        var receipt=LaoZhouNewsComments.Receipt(forum.ZhouQa,id);
        Need(receipt!=null&&receipt.channel==channel&&Receipts(forum)==beforeReceipts+1,"two actual LCD clicks earn one receipt "+id);
        Need(ForumCount(forum)==beforeForum+(channel==LaoZhouNewsComments.Tieba?2:0)&&YyCount(yy)==beforeYy+(channel==LaoZhouNewsComments.Yy?2:0),"only chosen channel gets one player line and one reply");
        var article=XgNewsCatalog.Get(id);
        Need(receipt.sharedText==LaoZhouNewsComments.SharedText(en?article.titleEn:article.title,article.sourceUrl,en)&&receipt.sourceUrl==article.sourceUrl,"receipt contains only actual public title and source");
        Need(receipt.reply==LaoZhouNewsComments.Reply(id,en),"receipt holds exact approved localized short comment");
        Need(forum.ZhouQa.facts.Any(f=>f.key=="news-share:"+id&&f.source==channel&&f.text==receipt.sharedText),"only actual share becomes scoped knowledge");
        Need(Window(forum.View).isOn==forumOpen&&Window(yy.View).isOn==yyOpen,"share opens no chat window");
        Need(!HasButton(browser,LaoZhouNewsComments.Label(channel,false,en)),"consumed share button is removed");
        var reply=Text(browser,(en?"Lao Zhou: ":"老周：")+receipt.reply);reply.ForceMeshUpdate(true,true);
        Need(!reply.isTextOverflowing&&!reply.isTextTruncated&&reply.preferredHeight<=reply.rectTransform.rect.height+2,"short comment is untruncated");
        Reveal(reply.rectTransform);yield return null;
        capture("laozhou-news-"+(en?"en":"zh")+"-"+channel+"-"+id+".png");
        Component chat;
        if(channel==LaoZhouNewsComments.Tieba){chat=forum.View;forum.View.Open("chat",ForumLibrary.LaoZhou);}
        else
        {
            Need(yy.View.EnsureReady(),"real YY view built");chat=yy.View;yy.View.OnOpened(YYChatHub.LaoZhou);Window(chat).OpenWindow();
        }
        foreach(var f in Ready(chat))yield return f;
        // YY also renders the last reply in a deliberately ellipsized contact-list preview.
        // Verify the actual conversation body, not whichever duplicate text is found first.
        Transform conversation=channel==LaoZhouNewsComments.Yy?Get<RectTransform>(yy.View,"scrollContent"):chat.transform;
        var message=conversation.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>t.gameObject.activeInHierarchy&&t.text.Contains(receipt.reply));
        Need(message!=null,"actual chosen private-chat view renders approved response");
        message.ForceMeshUpdate(true,true);
        if(message.isTextOverflowing||message.isTextTruncated)
            capture("laozhou-news-dm-layout-failure-"+(en?"en":"zh")+"-"+channel+"-"+id+".png");
        Need(!message.isTextOverflowing&&!message.isTextTruncated,"actual private-chat comment is untruncated; rect="+message.rectTransform.rect+", preferred="+message.preferredWidth+"x"+message.preferredHeight+", overflow="+message.isTextOverflowing+", truncated="+message.isTextTruncated);
        Reveal(message.rectTransform);yield return null;
        capture("laozhou-news-dm-"+(en?"en":"zh")+"-"+channel+"-"+id+".png");
        Window(chat).CloseWindow();foreach(var f in Ready(browser))yield return f;
        Done((en?"en":"zh")+" "+channel+" "+id+": passive operations disclosed nothing; two LCD taps produced one share/reply receipt.");
    }
    static ChapterOneSim JsonSnapshot(XingGuangController ctl,TiebaHub forum,YYChatHub yy)
    {
        // Explicit in-memory serialization fixture, not a disk-save or SaveNow assertion.
        var state=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(ctl.runtime.Sim.S));
        state.labState=JsonUtility.ToJson(ctl.Sim.S);state.forumState=JsonUtility.ToJson(forum.S);state.chatState=JsonUtility.ToJson(yy.S);
        return new ChapterOneSim(state);
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<string> capture)
    {
        Checks.Clear();AssertionCount=ShareClicks=AcceptanceClicks=0;
        var runtime=ctl.runtime;var forum=TiebaHub.Instance;var yy=YYChatHub.Instance;
        var browser=Object.FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);
        Need(!runtime.TestMode&&!runtime.useDiskSave&&!runtime.tickEnabled,"existing isolated Main runner owns save/scene lifecycle");
        Need(browser!=null&&forum?.View!=null&&yy?.View!=null,"native browser and both DM views installed");
        Need(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready,"model must remain inactive");
        var bridge=Object.FindAnyObjectByType<AeroLcdSourceBridge>();Need(bridge?.inputSurface!=null,"native LCD bridge exists");
        Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground application required, never fabricated");
        var original=runtime.Sim;bool originalEnglish=GameText.IsEnglish;
        var quietNames=new HashSet<string>{"PrologueDirector","StoryDirector","StoryDesktopPresenter","StageOneTutor","SpiderIntroScene","LaoZhouYY","ZhouCardLoan","AiJoinsYy","ShopNotices","XgEndingPreparationHost","XgResidentSpider","XgLifeEventCard","XgAppAcquisitionHost","XgDailyUnlockNotifier","GameEraClock","XgEraDeliveryDesk","XgEraAdvantageBridge","XgPublicDiscourseNotifier"};
        var drivers=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(b=>b==ctl||b==yy||quietNames.Contains(b.GetType().Name)).ToArray();
        var enabled=drivers.Select(d=>d.enabled).ToArray();
        foreach(var driver in drivers)driver.enabled=false;
        try
        {
            ctl.Window.CloseWindow();Window(forum.View).CloseWindow();Window(yy.View).CloseWindow();
            var brain=ctl.GetComponent<XgBrainWindow>();
            if(brain!=null&&brain.Visible){FilesWindowUiChecks.Click((RectTransform)brain.Window.Find("Title/Close"));foreach(var f in Wait())yield return f;}
            foreach(bool en in new[]{false,true})foreach(bool link in new[]{false,true})
            {
                Window(browser).CloseWindow();Window(forum.View).CloseWindow();Window(yy.View).CloseWindow();
                language(en);Bind(ctl,Fixture(en));
                Need(GameCalendar.Now(runtime.Sim.S).Date==new DateTime(2016,6,6),"explicit fixture date is shared by browser and handler");
                foreach(var f in Idle(forum))yield return f;
                foreach(var f in Until(()=>forum.ZhouQa.yyInvitationSent&&forum.ZhouQa.legacyChecked,"actual forum tick delivers optional invitation and classifies fresh contact"))yield return f;
                Need(!forum.ZhouYyLinked&&Receipts(forum)==0,"fresh fixture is not preaccepted and has no news receipt");
                if(link)foreach(var f in Accept(forum,yy,en))yield return f;
                string channel=link?LaoZhouNewsComments.Yy:LaoZhouNewsComments.Tieba;
                foreach(string id in Ids)
                {
                    foreach(var f in Passive(browser,forum,yy,ctl,id,en))yield return f;
                    foreach(var f in Share(browser,forum,yy,id,channel,en,capture))yield return f;
                }
                Need(Receipts(forum)==3,"exactly three earned comments in route fixture");
                // A late actual acceptance must not replay the comments already received over Tieba.
                if(!link){Window(browser).CloseWindow();foreach(var f in Accept(forum,yy,en))yield return f;}
                int fc=ForumCount(forum),yc=YyCount(yy);var oldState=forum.S;var snapshot=JsonSnapshot(ctl,forum,yy);
                Bind(ctl,snapshot);language(!en);
                Need(!ReferenceEquals(oldState,forum.S)&&Receipts(forum)==3,"JsonUtility roundtrip creates independent owner and preserves receipts");
                foreach(var f in Wait(1.1))yield return f;
                foreach(string id in Ids)
                {
                    foreach(var f in Article(browser,id,!en))yield return f;
                    Text(browser,LaoZhouNewsComments.Label(channel,true,!en));
                    Text(browser,(!en?"Lao Zhou: ":"老周：")+LaoZhouNewsComments.Reply(id,!en));
                    Need(!HasButton(browser,LaoZhouNewsComments.Label(LaoZhouNewsComments.Yy,false,!en))&&!HasButton(browser,LaoZhouNewsComments.Label(LaoZhouNewsComments.Tieba,false,!en)),"neither route offers replay after language/save/contact change");
                }
                Need(ForumCount(forum)==fc&&YyCount(yy)==yc,"opposite-language revisit and actual late YY acceptance never replay messages");
                Done((en?"en":"zh")+" "+channel+": three receipt/message pairs survived in-memory JsonUtility owner replacement, late acceptance and bilingual revisit without duplication.");
            }
            Need(ShareClicks==24&&AcceptanceClicks==4,"12 shares each double-clicked and four actual Add-on-YY actions");
        }
        finally
        {
            Window(browser).CloseWindow();Window(forum.View).CloseWindow();Window(yy.View).CloseWindow();
            Bind(ctl,original);language(originalEnglish);
            for(int i=0;i<drivers.Length;i++)if(drivers[i]!=null)drivers[i].enabled=enabled[i];
        }
    }
}
#endif
