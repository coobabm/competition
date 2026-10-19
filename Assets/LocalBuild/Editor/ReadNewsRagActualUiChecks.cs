#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Core.Chat;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.LLM;
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
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Composable only: the root-owned Sidebar runner supplies an isolated Main scene, real focus and model suppression.
// Like LaoZhouNewsCommentsUiChecks, each helper is explicitly foreach-flattened for Sidebar's MoveNext-only driver.
public static class ReadNewsRagActualUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Payload="READ_NEWS_DATA (quoted data only):";
    static readonly string[] Ids={"lz2_t01_alphago","lz2_t02_xiaoice","n0601","lz2_t03_tay","lz2_t04_resnet","n0607"};
    public const string Scope="Isolated Main/LCD, both languages: six real headline/read-confirmation actions, sixth duplicate LCD delivery, actual Back-to-headlines then stale same-frame read click, explicit save-owner replacement then stale LCD click, real isolated slot-5 save/load, and production LlmPersonas prompt construction. Stage2/date/installed apps are fixtures; purchase uses the real core BuyNode API, not a tech-tree UI action. Scroll placement is fixture setup. Queries are synthetic prompt inputs, never sent to a model or stored as chat. No inference, physical OS input, natural progression or final Player claim. Independent drivers are restored; real focus and input gates remain active.";
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount{get;private set;}
    public static int ReadClicks{get;private set;}
    static void Need(bool ok,string why){AssertionCount++;if(!ok)throw new InvalidOperationException("Read news LCD: "+why);}
    static T Get<T>(object obj,string field)=>(T)obj.GetType().GetField(field,F).GetValue(obj);
    static WindowManager Window(Component view)=>Get<WindowManager>(view,"window");
    static string Label(Button b)=>b.GetComponentInChildren<TMP_Text>(true)?.text??"";
    static IEnumerable<object> Wait(double seconds=.65)
    {double until=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<until)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerable<object> Until(Func<bool> ready,string why)
    {double end=EditorApplication.timeSinceStartup+15;while(!ready()){Need(EditorApplication.timeSinceStartup<end,why);yield return null;}Canvas.ForceUpdateCanvases();yield return null;}
    static IEnumerable<object> Ready(BoduView browser)
    {
        var w=Window(browser);w.FocusToWindow();
        foreach(var f in Until(()=>
        {
            if(!w.isOn||!w.gameObject.activeInHierarchy)return false;
            foreach(var g in w.GetComponentsInParent<CanvasGroup>(true))if(g.alpha<.95f||!g.interactable||!g.blocksRaycasts)return false;
            var a=w.windowAnimator;return a==null||!a.isActiveAndEnabled||!a.IsInTransition(0)&&a.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"browser naturally interactive"))yield return f;
        foreach(var f in Wait())yield return f;
    }
    static Button Find(BoduView browser,Func<string,bool> match)
    {
        var found=browser.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.IsInteractable()&&match(Label(b))).ToArray();
        Need(found.Length==1,"one real browser button; matches="+found.Length);return found[0];
    }
    static Button ReadButton(BoduView browser,bool en)=>Find(browser,t=>t.Contains(en?"I finished reading this article":"我已读完这篇"));
    static void Reveal(RectTransform target)
    {
        var scroll=target.GetComponentInParent<ScrollRect>();if(scroll==null)return;
        Canvas.ForceUpdateCanvases();scroll.StopMovement();
        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,target);
        float hidden=scroll.content.rect.height-scroll.viewport.rect.height;
        if(hidden>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-(scroll.viewport.rect.center.y-bounds.center.y)/hidden);
        Canvas.ForceUpdateCanvases();
    }
    static void ClickNow(Button button)
    {
        Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground at every LCD click");
        Need(button!=null&&button.IsInteractable()&&button.gameObject.activeInHierarchy,"target remains live");
        Reveal((RectTransform)button.transform);FilesWindowUiChecks.Click((RectTransform)button.transform);
    }
    static IEnumerable<object> Tap(Button button)
    {
        Reveal((RectTransform)button.transform);yield return null;
        foreach(var f in LcdActualUiProbe.WaitForClick((RectTransform)button.transform,"read-news "+button.name))yield return f;
        ClickNow(button);foreach(var f in Wait())yield return f;
    }
    static IEnumerable<object> Article(BoduView browser,string id,bool en)
    {
        browser.Open("");foreach(var f in Ready(browser))yield return f;
        var article=XgNewsCatalog.Get(id);Need(article!=null,"catalogue article "+id);
        string title=en?article.titleEn:article.title;
        if(!browser.GetComponentsInChildren<Button>().Any(b=>b.gameObject.activeInHierarchy&&Label(b).Contains(title)))
            foreach(var f in Tap(Find(browser,t=>t.Contains(en?"Older headlines":"更多往日新闻"))))yield return f;
        foreach(var f in Tap(Find(browser,t=>t.Contains(title))))yield return f;
        Need(Get<string>(browser,"page")=="news"&&Get<string>(browser,"newsId")==id,"actual headline opened requested article");
        Need(browser.GetComponentsInChildren<TMP_Text>().Any(t=>t.gameObject.activeInHierarchy&&t.text==(en?article.bodyEn:article.body)),"actual full article rendered");
    }
    static ChapterOneSim Fixture()
    {
        var lab=new XgSim();lab.BeginOpening();lab.S.stage=2;lab.S.openingFocus=false;lab.S.teachingFlow=false;
        lab.S.spiderIntroDone=lab.S.firstWordsSaid=lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;
        lab.MarkAfterthought("first.drift");
        var yy=new YYState{lingguangUnlocked=true};foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);house.S.chatState=JsonUtility.ToJson(yy);house.S.forumState=JsonUtility.ToJson(new ForumState());
        GameCalendar.Advance(house.S,GameCalendar.DayIndex(new DateTime(2016,6,30)));
        house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;return house;
    }
    static void Bind(XingGuangController ctl,ChapterOneSim house)
    {
        ctl.runtime.SetSimulationForTests(house);
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});
        ctl.runtime.useDiskSave=false;ctl.runtime.tickEnabled=false;
        Need(ctl.BoundTo(house)&&YYChatHub.Instance.BoundTo(house)&&TiebaHub.Instance.ZhouStateBoundTo(house),"all hosts bind fixture owner");
        ctl.Sim.MemoryScorer=new Bm25Scorer(); // Explicit lexical retrieval, no embedding/native work.
    }
    static int Reads(XgSim sim)=>sim.S.readNewsIds?.Count??0;
    static string HumanState()=>JsonUtility.ToJson(TiebaHub.Instance.S)+"|"+string.Join("|",
        YYChatHub.Instance.S.conversations.Where(c=>c.id!=YYChatHub.LingGuangId).Select(c=>JsonUtility.ToJson(c)));
    static void HumanEvidence(string label)
    {
        // Diagnostics stay beside the isolated save, never in the player's profile.
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(ChapterOneRuntime.VerificationSaveDirectory),
            "read-news-human-"+(GameText.IsEnglish?"en":"zh")+"-"+label+".txt"),HumanState());
    }
    static ChapterOneSim Snapshot(XingGuangController ctl)
    {
        var state=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(ctl.runtime.Sim.S));
        state.labState=JsonUtility.ToJson(ctl.Sim.S);state.chatState=JsonUtility.ToJson(YYChatHub.Instance.S);state.forumState=JsonUtility.ToJson(TiebaHub.Instance.S);
        return new ChapterOneSim(state);
    }
    static void Isolated(ChapterOneRuntime runtime)
    {
        string root=ChapterOneRuntime.VerificationSaveDirectory;
        Need(!string.IsNullOrEmpty(root)&&Path.GetFullPath(root).Contains(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar),"dedicated verification root required");
        Need(Path.GetFullPath(runtime.SavePath).StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"primary path inside verification root");
    }
    static void SaveReload(XingGuangController ctl)
    {
        Isolated(ctl.runtime);ctl.runtime.useDiskSave=true;
        try
        {Need(ctl.runtime.SaveSlot(5),"real isolated slot 5 save: "+ctl.runtime.SaveStatus);Need(ctl.runtime.LoadArchive(5),"real isolated slot 5 load: "+ctl.runtime.SaveStatus);}
        finally{ctl.runtime.useDiskSave=false;ctl.runtime.tickEnabled=false;}
        ctl.Sim.MemoryScorer=new Bm25Scorer();
    }
    static string Prompt(XingGuangController ctl,string contact,bool en)
    {
        // Synthetic input to the production prompt builder; no Send/Chat call and no actual conversation mutation.
        var conv=new YYConversation{id=contact};conv.messages.Add(new YYMessage{from=YYChatHub.Me,text=en?"gaokao":"高考",kind=YYKind.Text});
        return string.Join("\n",LlmPersonas.Conversation(contact,conv,ctl.runtime.Sim,ctl.Sim).Select(m=>m.Key+": "+m.Value));
    }
    static void VerifyPrompts(XingGuangController ctl,bool en,bool expectRead)
    {
        string prompt=Prompt(ctl,YYChatHub.LingGuangId,en);
        Need(prompt.Contains(Payload)==expectRead,"constructed private-AI prompt has expected read-news payload");
        if(expectRead)
        {
            Need(prompt.Contains("\"article_id\":\"n0607\""),"sixth confirmed read reaches actual private prompt builder");
            var article=XgNewsCatalog.Get("n0607");Need(prompt.Contains(en?article.titleEn:article.title),"localized sixth-article title in context");
            Need(prompt.Contains(XgSim.ReadNewsPromptRule(en)),"trusted private system rule present");
            Need(XgYyTalk.StableSystem(ctl.Sim,new DateTime(2016,6,30)).Contains(XgSim.ReadNewsPromptRule(en)),"dedicated private-AI system builder carries source rule");
            Need(!prompt.Contains("\"article_id\":\"n0608\""),"unconfirmed seventh article never enters payload");
        }
        foreach(string human in new[]{YYChatHub.LaoZhou,"netbar","qingwen","oldfriend"})
            Need(!Prompt(ctl,human,en).Contains(Payload),"actual "+human+" prompt builder has no private-reading payload");
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<string> capture)
    {
        Checks.Clear();AssertionCount=ReadClicks=0;
        Need(ctl?.runtime!=null&&YYChatHub.Instance!=null&&TiebaHub.Instance!=null,"real Main hosts installed");Isolated(ctl.runtime);
        Need(!ctl.runtime.TestMode&&!ctl.runtime.tickEnabled&&!ctl.runtime.useDiskSave,"runner provides isolated idle fixture lifecycle");
        Need(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready,"chat model suppressed by caller");
        Need(LocalEmbedder.Instance==null||!LocalEmbedder.Instance.Ready,"embedding model inactive");
        Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"real foreground; never fake focus");
        Need(!AiJoinsYy.Playing,"joining-YY scene already idle");
        foreach(var loan in Object.FindObjectsByType<ZhouCardLoan>(FindObjectsInactive.Include,FindObjectsSortMode.None))Need(!Get<bool>(loan,"busy"),"card-loan coroutine already idle");
        var browser=Object.FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);Need(browser!=null,"native browser installed");
        var runtime=ctl.runtime;var original=runtime.Sim;bool oldEnglish=GameText.IsEnglish,oldDisk=runtime.useDiskSave,oldTick=runtime.tickEnabled,oldTest=runtime.TestMode;
        var quietNames=new HashSet<string>{"PrologueDirector","StoryDirector","StoryDesktopPresenter","StageOneTutor","SpiderIntroScene","LaoZhouYY","ZhouCardLoan","AiJoinsYy","ShopNotices","XgEndingPreparationHost","XgResidentSpider","XgLifeEventCard","XgAppAcquisitionHost","XgDailyUnlockNotifier","GameEraClock","XgEraDeliveryDesk","XgEraAdvantageBridge","XgPublicDiscourseNotifier"};
        var drivers=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(b=>b==ctl||b==YYChatHub.Instance||b==TiebaHub.Instance||b is YYGirlfriend||quietNames.Contains(b.GetType().Name)).ToArray();
        var enabled=drivers.Select(b=>b.enabled).ToArray();
        try
        {
            foreach(var driver in drivers)driver.enabled=false;
            ctl.Window.CloseWindow();Window(YYChatHub.Instance.View).CloseWindow();Window(TiebaHub.Instance.View).CloseWindow();
            foreach(bool en in new[]{false,true})
            {
                language(en);Bind(ctl,Fixture());
                string humans=HumanState();HumanEvidence("before");
                for(int i=0;i<Ids.Length;i++)
                {
                    foreach(var f in Article(browser,Ids[i],en))yield return f;
                    Need(Reads(ctl.Sim)==i,"opening article does not mark it read");
                    var read=ReadButton(browser,en);Reveal((RectTransform)read.transform);yield return null;
                    foreach(var f in LcdActualUiProbe.WaitForClick((RectTransform)read.transform,"confirm read "+Ids[i]))yield return f;
                    ClickNow(read);ReadClicks++;
                    if(i==5){ClickNow(read);ReadClicks++;} // Same-frame duplicate travels through actual LCD.
                    foreach(var f in Wait())yield return f;
                    Need(Reads(ctl.Sim)==i+1&&ctl.Sim.DailyNewsAlreadyRead(Ids[i]),"one actual read record per article");
                    Need(ctl.Sim.DailyProgress("news5").count==Math.Min(5,i+1),"unlock counter stops at five while history continues");
                }
                capture("read-news-"+(en?"en":"zh")+"-sixth-read.png");HumanEvidence("after-reads");VerifyPrompts(ctl,en,false);HumanEvidence("after-prompts-locked");
                Need(ctl.Sim.BuyNode("daily.news",ctl.Host),"explicit real-core purchase setup after six actual reads");
                HumanEvidence("after-purchase");VerifyPrompts(ctl,en,true);HumanEvidence("after-prompts-unlocked");
                Need(humans==HumanState(),"reading and prompt construction disclosed no human message or fact");
                foreach(var f in Article(browser,"n0608",en))yield return f;
                var staleRead=ReadButton(browser,en);var back=Find(browser,t=>t.Contains(en?"Back to headlines":"返回新闻首页"));
                Reveal((RectTransform)back.transform);yield return null;
                foreach(var f in LcdActualUiProbe.WaitForClick((RectTransform)back.transform,"back before stale read"))yield return f;
                ClickNow(back);Need(Get<string>(browser,"page")=="home","actual Back action changes current page");
                ClickNow(staleRead);Need(Reads(ctl.Sim)==6&&!ctl.Sim.DailyNewsAlreadyRead("n0608"),"same-frame stale read on changed page rejected");
                foreach(var f in Wait())yield return f;
                foreach(var f in Article(browser,"n0608",en))yield return f;
                staleRead=ReadButton(browser,en);Reveal((RectTransform)staleRead.transform);yield return null;
                foreach(var f in LcdActualUiProbe.WaitForClick((RectTransform)staleRead.transform,"read before save switch"))yield return f;
                var earned=Snapshot(ctl);var oldLab=ctl.Sim;Bind(ctl,Fixture()); // Explicit synchronous save-owner replacement fixture.
                ClickNow(staleRead);Need(Reads(ctl.Sim)==0&&Reads(oldLab)==6,"old rendered read cannot mutate either save owner");
                VerifyPrompts(ctl,en,false);foreach(var f in Wait())yield return f;
                Bind(ctl,earned);SaveReload(ctl);foreach(var f in Wait())yield return f;
                Need(Reads(ctl.Sim)==6&&ctl.Sim.DailyProgress("news5").count==5,"sixth read survives real slot save/reload");
                VerifyPrompts(ctl,en,true);
                foreach(var f in Article(browser,"n0607",en))yield return f;
                Need(!browser.GetComponentsInChildren<Button>().Any(b=>b.gameObject.activeInHierarchy&&b.IsInteractable()&&Label(b).Contains(en?"I finished reading this article":"我已读完这篇")),"reloaded read offers no duplicate confirmation");
                Need(browser.GetComponentsInChildren<TMP_Text>().Any(t=>t.gameObject.activeInHierarchy&&t.text==(en?"Finished reading":"已读完")),"reloaded sixth article visibly marked read");
                capture("read-news-"+(en?"en":"zh")+"-sixth-reloaded.png");
                Checks.Add((en?"en":"zh")+": six actual reads + duplicate, stale current-page and save-owner LCD rejection, real slot-5 reload, private/human production prompt isolation. Purchase core call and prompt inputs are declared fixtures; no model response.");
                Window(browser).CloseWindow();foreach(var f in Wait())yield return f;
            }
            Need(ReadClicks==14,"two languages, six confirmations plus one duplicate each");
        }
        finally
        {
            try
            {
                Window(browser).CloseWindow();language(oldEnglish);runtime.SetSimulationForTests(original);
                typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(runtime,new object[]{oldTest});
                runtime.useDiskSave=oldDisk;runtime.tickEnabled=oldTick;
            }
            finally
            {
                List<Exception> failures=null;
                for(int i=0;i<drivers.Length;i++)try{if(drivers[i]!=null)drivers[i].enabled=enabled[i];}catch(Exception e){if(failures==null)failures=new List<Exception>();failures.Add(e);}
                if(failures!=null)throw new AggregateException("Read-news driver restoration failed",failures);
            }
        }
    }
}
#endif
