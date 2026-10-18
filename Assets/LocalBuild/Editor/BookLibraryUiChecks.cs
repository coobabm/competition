#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main/LCD integration suite. The shared runner owns scene boot, model suppression, screenshots and exit.
// No model is started here. Elapsed reading time, save faults and persisted model receipts are explicit fixtures.
public static class BookLibraryUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Key="SidebarLayoutChecks.BookLibraryOnly", SetupKey="BookLibraryUiChecks.Setup";
    [Serializable] sealed class SceneEntry { public string path; public bool loaded,active; }
    [Serializable] sealed class EditorSetup
    { public List<SceneEntry> scenes=new List<SceneEntry>(); public string save,desktop; public int rate,vsync; }
    static Mouse addedMouse;
    static Keyboard addedKeyboard;
    public static bool Requested=>SessionState.GetBool(Key,false);
    public static void ClearRequest()=>SessionState.EraseBool(Key);
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount {get;private set;}

    [MenuItem("Tools/Local Verification/Book library actual LCD")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in clean EditMode");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene edits before scene-switching QA");
        var setup=new EditorSetup{save=ChapterOneRuntime.VerificationSaveDirectory,desktop=(string)typeof(DreamOSDataManager).GetField("dataPath",BindingFlags.Static|BindingFlags.NonPublic)?.GetValue(null),rate=Application.targetFrameRate,vsync=QualitySettings.vSyncCount};
        setup.scenes=EditorSceneManager.GetSceneManagerSetup().Select(x=>new SceneEntry{path=x.path,loaded=x.isLoaded,active=x.isActive}).ToList();
        SessionState.SetString(SetupKey,JsonUtility.ToJson(setup));SessionState.SetBool(Key,true);
        try { SidebarLayoutChecks.Run(); }
        catch { ClearRequest();try {RestoreEditor();} finally {if(Application.isBatchMode)EditorApplication.Exit(1);}throw; }
    }
    public static void PrepareInput()
    {
        if(!Requested)return;
        Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
        if(Mouse.current==null){addedMouse=InputSystem.AddDevice<Mouse>("BookLibraryQaMouse");addedMouse.MakeCurrent();}
        if(Keyboard.current==null){addedKeyboard=InputSystem.AddDevice<Keyboard>("BookLibraryQaKeyboard");addedKeyboard.MakeCurrent();}
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
        SessionState.EraseString(SetupKey);ClearRequest();var s=JsonUtility.FromJson<EditorSetup>(json);
        ChapterOneRuntime.VerificationSaveDirectory=string.IsNullOrEmpty(s.save)?null:s.save;
        typeof(DreamOSDataManager).GetField("dataPath",BindingFlags.Static|BindingFlags.NonPublic)?.SetValue(null,string.IsNullOrEmpty(s.desktop)?null:s.desktop);
        Application.targetFrameRate=s.rate;QualitySettings.vSyncCount=s.vsync;
        var scenes=s.scenes.Where(x=>!string.IsNullOrEmpty(x.path)).Select(x=>new SceneSetup{path=x.path,isLoaded=x.loaded,isActive=x.active}).ToArray();
        if(scenes.Length>0)EditorSceneManager.RestoreSceneManagerSetup(scenes);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    }
    static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,F).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,F).SetValue(o,value);
    static void Need(bool ok,string why){AssertionCount++;if(!ok)throw new InvalidOperationException("C4 UI: "+why);}
    static void Done(string text){Checks.Add(text);Debug.Log("C4 UI: "+text);}
    static IEnumerable<object> FramesFor(double seconds)
    {double until=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<until)yield return null;}
    static IEnumerable<object> Until(Func<bool> test,string why,double seconds=10)
    {
        double until=EditorApplication.timeSinceStartup+seconds;
        while(!test()){if(EditorApplication.timeSinceStartup>=until)Need(false,why+" [focus="+StoryFocus.Active+", voice="+InnerVoice.Busy+"]");yield return null;}
        Canvas.ForceUpdateCanvases();yield return null;yield return null;
    }
    static IEnumerable<object> Ready(WindowManager w)
    {
        foreach(var f in Until(()=>{
            if(!w.isOn||!w.gameObject.activeInHierarchy)return false;
            foreach(var g in w.GetComponentsInParent<CanvasGroup>(true))if(g.alpha<.95f||!g.interactable||!g.blocksRaycasts)return false;
            var a=w.windowAnimator;return a==null||!a.isActiveAndEnabled||!a.IsInTransition(0)&&a.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"window naturally interactive: "+w.name))yield return f;
    }
    static string Label(Button b)=>b.GetComponentInChildren<TMP_Text>(true)?.text??"";
    static Button Find(Component root,Func<Button,bool> match)
    {
        var bs=root.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.IsInteractable()&&match(b)).ToArray();
        Need(bs.Length==1,"one live button matched in "+root.name+", got "+bs.Length);return bs[0];
    }
    static Button Named(Component root,string name)=>Find(root,b=>b.name==name);
    static Button TextButton(Component root,string label)=>Find(root,b=>Label(b)==label);
    static string Texts(Component root)=>string.Join("\n",root.GetComponentsInChildren<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy).Select(t=>t.text));
    static void Reveal(BoduView browser,Button button)
    {
        var scroll=Get<ScrollRect>(browser,"scroll");Canvas.ForceUpdateCanvases();scroll.StopMovement();
        // Scroll geometry only; the activation itself always uses the actual LCD input bridge.
        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,button.transform);
        float hidden=scroll.content.rect.height-scroll.viewport.rect.height;
        if(hidden>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-(scroll.viewport.rect.center.y-bounds.center.y)/hidden);
        Canvas.ForceUpdateCanvases();
    }
    static IEnumerable<object> Tap(BoduView browser,Func<Button,bool> match,Action<RectTransform> click)
    {
        var b=Find(browser,match);Reveal(browser,b);yield return null;click((RectTransform)b.transform);
        foreach(var f in FramesFor(.65))yield return f;
    }
    static IEnumerable<object> Store(BoduView browser,XgReadingWindow reader)
    {
        reader.Hide();browser.OpenLibrary();Get<WindowManager>(browser,"window").FocusToWindow();
        foreach(var f in Ready(Get<WindowManager>(browser,"window")))yield return f;
        foreach(var f in FramesFor(.65))yield return f;
    }
    static IEnumerable<object> Detail(BoduView browser,XgReadingWindow reader,string id,Action<RectTransform> click)
    {
        foreach(var f in Store(browser,reader))yield return f;
        string all=GameText.IsEnglish?"All titles":"全部书目";
        foreach(var f in Tap(browser,b=>Label(b).EndsWith(all,StringComparison.Ordinal),click))yield return f;
        string title=GameText.IsEnglish?XgBooks.LibraryEntry(id).titleEn:XgBooks.LibraryEntry(id).title;
        int guard=0;
        while(!browser.GetComponentsInChildren<Button>().Any(b=>Label(b).StartsWith("<u>"+title+"</u>",StringComparison.Ordinal)))
        {
            Need(guard++<10,"catalogue reaches "+id);
            foreach(var f in Tap(browser,b=>Label(b)==(GameText.IsEnglish?"Next page":"下一页"),click))yield return f;
        }
        foreach(var f in Tap(browser,b=>Label(b).StartsWith("<u>"+title+"</u>",StringComparison.Ordinal),click))yield return f;
    }
    static Action RetainCallback(Button b)
    {
        // Deliberate stale-callback fault injection. These calls are never reported as real pointer clicks.
        var calls=(IEnumerable)typeof(UnityEventBase).GetMethod("PrepareInvoke",F).Invoke(b.onClick,null);
        var retained=calls.Cast<object>().ToArray();
        return ()=>{foreach(var c in retained)c.GetType().GetMethod("Invoke",F,null,new[]{typeof(object[])},null).Invoke(c,new object[]{Array.Empty<object>()});};
    }
    static ChapterOneSim NewHouse(int stage,bool en=false)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=en;
        lab.S.stage=lab.S.stageVision=lab.S.stageSequence=stage;lab.S.abilitiesEmerged=Enumerable.Range(1,stage).ToList();
        lab.S.teachingFlow=false;lab.S.spiderIntroDone=true;lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;lab.MarkAfterthought("first.drift");
        var yy=new YYState{lingguangUnlocked=true};foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);house.S.chatState=JsonUtility.ToJson(yy);
        GameCalendar.Advance(house.S,GameCalendar.DayFor(stage,0));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
        return house;
    }
    static void Bind(XingGuangController ctl,ChapterOneSim house)
    {
        ctl.runtime.SetSimulationForTests(house);
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});
        ctl.runtime.useDiskSave=false;ctl.runtime.tickEnabled=false;
        Need(ctl.BoundTo(house)&&ReferenceEquals(ctl.runtime.Sim,house)&&YYChatHub.Instance.BoundTo(house),"controller and YY synchronously bind actual replacement save");
        Need(!ctl.runtime.TestMode&&ctl.Host.Compute>0&&string.IsNullOrEmpty(ctl.Host.Blocker),"real powered household economy");
        DesktopNotifications.Install(ctl.runtime); // Clears the presentation queue on an explicit fixture save switch.
    }
    static void EnableDisk(ChapterOneRuntime rt)
    {
        string folder=ChapterOneRuntime.VerificationSaveDirectory;
        Need(!string.IsNullOrEmpty(folder)&&Path.GetFullPath(folder).Contains(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar),"verification folder is explicit");
        Need(Path.GetFullPath(rt.SavePath).StartsWith(Path.GetFullPath(folder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"disk writes stay inside verification folder");rt.useDiskSave=true;
    }
    static void Save(ChapterOneRuntime rt,int slot)
    {EnableDisk(rt);try{Need(rt.SaveSlot(slot),"SaveSlot "+slot+": "+rt.SaveStatus);}finally{rt.useDiskSave=false;}}
    static void Load(ChapterOneRuntime rt,int slot)
    {EnableDisk(rt);try{Need(rt.LoadArchive(slot),"LoadArchive "+slot+": "+rt.SaveStatus);}finally{rt.useDiskSave=false;}}
    static int Receipts(string id)=>YYChatHub.Instance.Conversation(YYChatHub.LingGuangId).messages.Count(m=>m.attentionWord=="library:"+XgSim.BookCompletionKey(id));
    static object Notice(DesktopNotifications service,string prefix)
    {
        foreach(var item in Get<IEnumerable>(service,"pending"))if(Get<string>(item,"body").StartsWith(prefix,StringComparison.Ordinal))return item;
        return null;
    }
    static IEnumerable<object> ReadAll(XingGuangController ctl,XgReadingWindow reader,string id,Action<RectTransform> click)
    {
        XgReadingWindow.Open(ctl,id);foreach(var f in FramesFor(.3))yield return f;
        var sim=ctl.Sim;int count=0;
        while(!sim.BookDone(id))
        {
            Need(count++<20,"bounded chapter iteration");int before=sim.ChaptersRead(id);
            var read=Get<Button>(reader,"readButton");Need(read.IsInteractable(),"real read-next enabled: "+id+" / "+Label(read));click((RectTransform)read.transform);
            Need(sim.ReadingBook?.id==id,"real read-next starts selected owned book");
            sim.Tick(40,ctl.Host); // Explicit elapsed-time fixture; production Tick, power and rewards remain real.
            Need(sim.ChaptersRead(id)==before+1,"one production chapter completion per elapsed-time fixture");
            foreach(var f in FramesFor(.3))yield return f;
        }
    }
    static IEnumerable<object> OpenYy(YYChatView view,XgReadingWindow reader)
    {
        reader.Hide();view.EnsureReady();view.OnOpened(YYChatHub.LingGuangId);var w=Get<WindowManager>(view,"window");w.OpenWindow();w.FocusToWindow();
        foreach(var f in Ready(w))yield return f;foreach(var f in FramesFor(.3))yield return f;
    }
    static void SendAnswer(YYChatView view,string answer,Action<RectTransform> click)
    {Get<TMP_InputField>(view,"input").text=answer;click((RectTransform)Named(view,"Send").transform);}

    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        Checks.Clear();AssertionCount=0;PrepareInput();
        var rt=ctl.runtime;var yy=YYChatHub.Instance;var browser=UnityEngine.Object.FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);
        Need(rt!=null&&!rt.useDiskSave&&!rt.tickEnabled&&!rt.TestMode,"isolated real Main fixture, not a test-economy stub");
        Need(browser!=null&&yy?.View!=null,"real Bodu and YY installed");
        Need(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready,"model service disabled before UI suite");
        var reader=XgReadingWindow.Install(ctl);var talk=ctl.GetComponent<XgYyTalk>();Need(talk!=null,"real YY talk bridge installed");
        var notifications=DesktopNotifications.Install(rt);Need(notifications!=null,"real desktop notification service");
        bool notificationsEnabled=notifications.enabled;notifications.enabled=false;
        // Retain real notification requests for inspection; no claim that each coalesced request is a separate popup.
        var original=rt.Sim;bool originalEnabled=ctl.enabled;bool originalLanguage=GameText.IsEnglish;
        var oldProbe=StoryFocus.Probe;var oldExclusive=StoryFocus.ExclusiveProbe;bool held=true;
        var loans=UnityEngine.Object.FindObjectsByType<ZhouCardLoan>(FindObjectsInactive.Include,FindObjectsSortMode.None);var loanStates=loans.Select(x=>x.enabled).ToArray();
        Action saveFault=null;
        StoryFocus.Probe=()=>held;StoryFocus.ExclusiveProbe=()=>false;ctl.enabled=false;
        foreach(var loan in loans){loan.StopAllCoroutines();loan.enabled=false;}
        try
        {
            language(false);ctl.Window.CloseWindow();Get<WindowManager>(yy.View,"window").CloseWindow();
            Bind(ctl,NewHouse(1));foreach(var f in FramesFor(.6))yield return f;
            browser.Open("");Get<WindowManager>(browser,"window").FocusToWindow();
            foreach(var f in Ready(Get<WindowManager>(browser,"window")))yield return f;foreach(var f in FramesFor(.65))yield return f;
            foreach(var f in Tap(browser,b=>Label(b)=="图书馆 · 买书与我的书架",click))yield return f;
            Need(Texts(browser).Contains("阶段 2")&&Texts(browser).Contains("¥500"),"homepage link reaches locked library with approved price");
            Get<TMP_InputField>(browser,"search").text="library.bodu";click((RectTransform)Named(browser,"Go").transform);
            foreach(var f in FramesFor(.65))yield return f;
            Need(Get<string>(browser,"page")=="library","actual address field and Go button route library.bodu");
            double cash=ctl.Host.Money;Need(!ctl.Sim.BuyNode(XgSim.LibraryNode,ctl.Host)&&ctl.Host.Money==cash,"stage-one real-host entry purchase blocked");
            foreach(var f in Tap(browser,b=>Label(b)=="打开我的书架",click))yield return f;
            Need(reader.Visible&&ctl.Sim.OwnedBooks().Count==0&&Texts(reader.Window).Contains("书架里还没有书"),"new shelf empty and reachable before website unlock");
            capture("c4-01-locked-empty");
            Bind(ctl,NewHouse(2));foreach(var f in FramesFor(.35))yield return f;
            cash=ctl.Host.Money;Need(ctl.Sim.BuyNode(XgSim.LibraryNode,ctl.Host)&&ctl.Host.Money==cash-500,"actual stage-two entry costs exactly 500");
            Need(ctl.Sim.OwnedBooks().Count==0,"entry grants no books");
            foreach(var f in Detail(browser,reader,"qianziwen",click))yield return f;
            rt.Sim.S.money=79;foreach(var f in FramesFor(.65))yield return f;
            Need(Texts(browser).Contains("经费不足")&&!browser.GetComponentsInChildren<Button>().Any(b=>Label(b).StartsWith("购买阅读材料")),"insufficient funds renders non-clickable purchase blocker");
            rt.Sim.S.money=1000000;foreach(var f in FramesFor(.65))yield return f;
            var buy=Find(browser,b=>Label(b)=="购买阅读材料 · ¥80");var duplicate=RetainCallback(buy);cash=ctl.Host.Money;
            Reveal(browser,buy);yield return null;click((RectTransform)buy.transform);duplicate();
            Need(ctl.Sim.BookOwned("qianziwen")&&ctl.Host.Money==cash-80&&ctl.Sim.S.bookPurchases.Count==1,"actual LCD purchase plus duplicate callback charges once");
            Need(reader.Visible&&Get<string>(reader,"selected")=="qianziwen","purchase opens independent reader on owned material");
            foreach(var f in FramesFor(.35))yield return f;
            Need(Texts(reader.Window).Contains("原创导读")&&Texts(reader.Window).Contains("非原著全文"),"reader labels supplied guide honestly");
            int chapter=ctl.Sim.ChaptersRead("qianziwen");click((RectTransform)Named(reader.Window,"NextChapter").transform);
            Need(Get<int>(reader,"chapter")==1&&ctl.Sim.ChaptersRead("qianziwen")==chapter,"browsing material never grants reading progress");
            click((RectTransform)Named(reader.Window,"PreviousChapter").transform);
            capture("c4-02-purchased-reader");
            foreach(var f in Detail(browser,reader,"qianziwen",click))yield return f;
            cash=ctl.Host.Money;foreach(var f in Tap(browser,b=>Label(b)=="已拥有 · 在书架中打开",click))yield return f;
            Need(reader.Visible&&ctl.Host.Money==cash&&ctl.Sim.S.bookPurchases.Count==1,"actual owned-material click reopens without payment");
            foreach(var f in Detail(browser,reader,"shiji",click))yield return f;
            Need(Texts(browser).Contains(ctl.Sim.BuyBookBlocker("shiji",ctl.Host))&&!ctl.Sim.BookOwned("shiji"),"later-stage book remains gated in actual storefront");
            int today=ctl.Sim.Today;ctl.Sim.Today=18000101;
            foreach(var f in Detail(browser,reader,"dizigui",click))yield return f;
            Need(Texts(browser).Contains("这本书尚未到馆"),"explicit pre-publication date fixture renders storefront blocker");ctl.Sim.Today=today;
            Done("01-02: homepage, locked/new shelf, real entry debit, funds/stage gates, LCD purchase and owned reopen; duplicate is retained-callback injection.");

            foreach(var f in Store(browser,reader))yield return f;
            var titles=new HashSet<string>();
            for(int p=0;p<10;p++)
            {
                var rows=browser.GetComponentsInChildren<Button>().Where(b=>Label(b).StartsWith("<u>",StringComparison.Ordinal)).ToArray();
                Need(rows.Length==10,"ten catalogue rows on page "+(p+1));foreach(var b in rows)Need(titles.Add(Label(b)),"catalogue row unique");
                if(p<9)foreach(var f in Tap(browser,b=>Label(b)=="下一页",click))yield return f;
            }
            Need(titles.Count==100,"all 100 catalogue entries reachable by actual page buttons");
            foreach(var f in Tap(browser,b=>Label(b)=="○ 中文原作",click))yield return f;Need(Texts(browser).Contains("第 1 / 9"),"90 Chinese originals yield nine pages");
            foreach(var f in Tap(browser,b=>Label(b)=="○ 英文原作",click))yield return f;Need(Texts(browser).Contains("第 1 / 1"),"10 English originals yield one page");
            language(true);ctl.Sim.English=true;foreach(var f in FramesFor(.65))yield return f;
            Need(Texts(browser).Contains("English originals")&&Texts(browser).Contains("Page 1 / 1"),"language change redraws catalogue labels");
            Need(Texts(browser).Contains("Added to your shelf.")&&!Texts(browser).Contains("已加入书架"),"purchase status translates after live language change");
            capture("c4-03-english-catalogue");
            language(false);ctl.Sim.English=false;
            foreach(var f in Detail(browser,reader,"huozhe",click))yield return f;
            Need(Texts(browser).Contains("原创导读")&&Texts(browser).Contains("非原著全文")&&Texts(browser).Contains("1992")&&Texts(browser).Contains("材料来源与许可"),"modern title shows guide, publication and rights before payment");
            Done("03: 100 distinct visible catalogue rows, language filters, English redraw and modern-guide disclosure.");

            XgReadingWindow.Open(ctl,"qianziwen");foreach(var f in FramesFor(.35))yield return f;
            Need(XgLifeHostActivity.ReadingWindowVisible,"actual visible reader owns cadence gate");
            click((RectTransform)Get<Button>(reader,"readButton").transform);Need(ctl.Sim.Reading,"LCD starts chapter");
            ctl.Sim.Tick(1,ctl.Host);double clock=ctl.Sim.S.bookClock;Save(rt,2);Load(rt,2);foreach(var f in FramesFor(.35))yield return f;
            Need(!reader.Visible&&!XgLifeHostActivity.ReadingWindowVisible,"save switch hides reader and releases cadence gate");
            Need(ctl.Sim.BookOwned("qianziwen")&&ctl.Sim.Reading&&ctl.Sim.S.bookClock==clock,"actual archive preserves ownership and active chapter clock");
            XgReadingWindow.Open(ctl,"qianziwen");foreach(var f in FramesFor(.35))yield return f;
            click((RectTransform)Named(reader.Window,"Minimize").transform);Need(!reader.Visible&&!XgLifeHostActivity.ReadingWindowVisible&&ctl.Sim.Reading,"minimize only hides window, not background reading");
            ctl.Sim.Tick(40,ctl.Host);Need(ctl.Sim.ChaptersRead("qianziwen")==1,"hidden reader still completes actual timed chapter");
            foreach(var f in ReadAll(ctl,reader,"qianziwen",click))yield return f;
            Need(ctl.Sim.PendingBookCompletion!=null&&Receipts("qianziwen")==0,"StoryFocus holds durable completion without immediate duplicate YY message");
            Need(ctl.Sim.LearningRecordsNewest().Count(r=>r.sourceKind=="book")==1,"one C3 completion history record");
            Save(rt,2);Load(rt,2);foreach(var f in FramesFor(.35))yield return f;
            Need(ctl.Sim.PendingBookCompletion!=null&&Receipts("qianziwen")==0,"undelivered completion survives disk reload");
            held=false;foreach(var f in Until(()=>ctl.Sim.PendingBookCompletion==null,"actual YY Update delivers idle completion",35))yield return f;
            Need(Receipts("qianziwen")==1&&ctl.Sim.BookPuzzlePending==null,"stage-two simple response has one stable receipt and no puzzle");
            string completionText=ctl.Sim.BookCompletionText(ctl.Sim.S.bookCompletions.Single());
            var notice=Notice(notifications,completionText);Need(notice!=null&&Get<int>(notice,"count")==1,"completion requests exactly one real desktop notification");
            var staleNotice=Get<Action>(notice,"open");
            Save(rt,2);Load(rt,2);foreach(var f in FramesFor(.8))yield return f;
            Need(Receipts("qianziwen")==1&&ctl.Sim.PendingBookCompletion==null,"ack and receipt reload atomically without replay");
            DesktopNotifications.Install(rt);foreach(var f in FramesFor(.5))yield return f;
            Need(Notice(notifications,completionText)==null,"reloaded receipt does not request another desktop notification");
            Get<WindowManager>(yy.View,"window").CloseWindow();staleNotice?.Invoke();
            Need(!Get<WindowManager>(yy.View,"window").isOn,"retained old notification callback cannot open replacement save conversation");
            foreach(var f in OpenYy(yy.View,reader))yield return f;capture("c4-04-low-stage-yy");
            Done("04-06: actual archive round trip, active chapter recovery, minimize/background progress, StoryFocus queue, one YY receipt and one C3 history record.");

            held=true;Get<WindowManager>(yy.View,"window").CloseWindow();Bind(ctl,NewHouse(4));foreach(var f in FramesFor(.4))yield return f;
            Need(ctl.Sim.BuyNode(XgSim.LibraryNode,ctl.Host),"stage-four queue fixture buys entry");
            foreach(string id in new[]{"qianziwen","dizigui"}){Need(ctl.Sim.BuyBook(id,ctl.Host),"queue fixture real-host purchase "+id);foreach(var f in ReadAll(ctl,reader,id,click))yield return f;}
            reader.Hide();held=false;foreach(var f in Until(()=>Receipts("qianziwen")==1,"first completion delivered",35))yield return f;
            string puzzle=ctl.Sim.S.bookPuzzle;Need(puzzle==XgBooks.Puzzles("qianziwen")[0].id&&Receipts("dizigui")==0&&ctl.Sim.PendingBookCompletion?.bookId=="dizigui","second completed book waits for first answer owner");
            foreach(var f in OpenYy(yy.View,reader))yield return f;
            SendAnswer(yy.View,XgBooks.Puzzles("qianziwen")[0].keys[0],click);
            foreach(var f in Until(()=>ctl.Sim.S.bookAnswers.Any(a=>a.id==puzzle),"real YY input/send records answer"))yield return f;
            Need(ctl.Sim.S.bookAnswers.Single(a=>a.id==puzzle).verdict==XgSim.PuzzleRight,"offline judge uses existing correct-answer rules");
            foreach(var f in Until(()=>Receipts("dizigui")==1,"second completion resumes after answer"))yield return f;
            Need(ctl.Sim.BookPuzzleBook?.id=="dizigui","queue advances without replacing earlier pending owner");
            SendAnswer(yy.View,"跳过",click);foreach(var f in Until(()=>ctl.Sim.BookPuzzlePending==null&&!talk.Thinking,"YY skip clears final puzzle"))yield return f;
            double samples=ctl.Sim.BookCharsRead;Save(rt,3);Load(rt,3);foreach(var f in FramesFor(.5))yield return f;
            Need(ctl.Sim.S.bookAnswers.Count==2&&ctl.Sim.BookCharsRead==samples&&Receipts("qianziwen")==1&&Receipts("dizigui")==1,"answered records and receipts survive without duplicate reward");
            capture("c4-05-yy-answers");Done("07: actual YY send, existing offline verdict, one-answer queue, skip and saved answer compatibility.");

            held=true;Get<WindowManager>(yy.View,"window").CloseWindow();Bind(ctl,NewHouse(5));foreach(var f in FramesFor(.4))yield return f;
            Need(ctl.Sim.BuyNode(XgSim.LibraryNode,ctl.Host)&&ctl.Sim.BuyBook(XgSim.RedMansionId,ctl.Host),"continuation fixture actual entry/book payments");
            var red=XgBooks.Get(XgSim.RedMansionId);
            // Existing progress is explicit fixture data; the final chapter, completion and YY answer below are real.
            ctl.Sim.S.books.Add(new XgScore{key=red.id,count=red.chapters.Length-1});
            foreach(var f in ReadAll(ctl,reader,red.id,click))yield return f;reader.Hide();held=false;
            foreach(var f in Until(()=>Receipts(red.id)==1,"red-mansion completion reaches YY",35))yield return f;
            Need(ctl.Sim.S.redMansionBeat==1&&ctl.Sim.BookPuzzlePending==null,"continuation owns pending answer without competing puzzle");
            foreach(var f in OpenYy(yy.View,reader))yield return f;
            SendAnswer(yy.View,"后四十回是别人续的。",click);
            foreach(var f in Until(()=>ctl.Sim.S.redMansionBeat==2,"actual YY continuation answer advances preserved story beat"))yield return f;
            Need(Receipts(red.id)==1,"continuation answer never duplicates completion receipt");
            Done("07b: saved prior-chapter fixture plus real final chapter, red-mansion YY completion and continuation answer.");

            held=true;Get<WindowManager>(yy.View,"window").CloseWindow();Bind(ctl,NewHouse(2));foreach(var f in FramesFor(.4))yield return f;
            Need(ctl.Sim.BuyNode(XgSim.LibraryNode,ctl.Host)&&ctl.Sim.BuyBook("qianziwen",ctl.Host),"save-fault fixture real purchases");
            foreach(var f in ReadAll(ctl,reader,"qianziwen",click))yield return f;reader.Hide();
            int faults=0;saveFault=()=>{faults++;throw new IOException("C4 QA injected application snapshot failure");};
            EnableDisk(rt);rt.Saving+=saveFault;held=false;
            foreach(var f in Until(()=>Get<string>(talk,"pendingBookToast")!=null,"failed actual SaveNow leaves deferred notification retry",35))yield return f;
            Need(Receipts("qianziwen")==1&&ctl.Sim.PendingBookCompletion==null&&faults>0,"failed save retains in-memory receipt and ack together");
            completionText=ctl.Sim.BookCompletionText(ctl.Sim.S.bookCompletions.Single());
            Need(Notice(notifications,completionText)==null,"failed save has not requested completion notification");
            int faultCount=faults;float retry=Get<float>(talk,"nextBookSaveAttempt");foreach(var f in FramesFor(.6))yield return f;
            Need(faults==faultCount&&Get<float>(talk,"nextBookSaveAttempt")==retry,"failure retry is throttled rather than every frame");
            rt.Saving-=saveFault;saveFault=null;Set(talk,"nextBookSaveAttempt",Time.unscaledTime); // Explicit retry-clock fixture, no ten-second sleep.
            foreach(var f in Until(()=>Get<string>(talk,"pendingBookToast")==null,"normal save retry succeeds"))yield return f;
            notice=Notice(notifications,completionText);Need(notice!=null&&Get<int>(notice,"count")==1,"successful retry requests one desktop notification");
            rt.useDiskSave=false;Save(rt,4);Load(rt,4);foreach(var f in FramesFor(.4))yield return f;
            Need(Receipts("qianziwen")==1&&ctl.Sim.PendingBookCompletion==null,"recovered save retains exactly one receipt");
            Done("08: actual SaveNow snapshot fault, durable in-memory transaction, retry throttle and successful recovery. Popup coalescing is not counted as individual popups.");

            held=true;Bind(ctl,NewHouse(2));foreach(var f in FramesFor(.4))yield return f;
            Need(ctl.Sim.BuyNode(XgSim.LibraryNode,ctl.Host),"stale callback fixture entry");
            foreach(var f in Detail(browser,reader,"qianziwen",click))yield return f;
            var staleBuy=RetainCallback(Find(browser,b=>Label(b)=="购买阅读材料 · ¥80"));var oldSim=ctl.Sim;
            Bind(ctl,NewHouse(2));Need(ctl.Sim.BuyNode(XgSim.LibraryNode,ctl.Host),"same-title replacement fixture entry");cash=ctl.Host.Money;staleBuy();
            Need(ctl.Host.Money==cash&&!ctl.Sim.BookOwned("qianziwen")&&!oldSim.BookOwned("qianziwen"),"retained old purchase cannot debit either save");
            Need(ctl.Sim.BuyBook("qianziwen",ctl.Host),"old reader fixture owns target");XgReadingWindow.Open(ctl,"qianziwen");foreach(var f in FramesFor(.4))yield return f;
            var staleRead=RetainCallback(Get<Button>(reader,"readButton"));var staleShelf=RetainCallback(Named(reader.Window,"Book_qianziwen"));oldSim=ctl.Sim;
            Bind(ctl,NewHouse(2));Need(ctl.Sim.BuyNode(XgSim.LibraryNode,ctl.Host)&&ctl.Sim.BuyBook("qianziwen",ctl.Host),"replacement also owns identical book");
            staleRead();staleShelf();Need(!oldSim.Reading&&!ctl.Sim.Reading,"retained read/shelf callbacks cannot start either save's chapter");
            foreach(var f in FramesFor(.4))yield return f;Need(!reader.Visible&&!XgLifeHostActivity.ReadingWindowVisible,"reader bind invalidates old visible selection");
            Done("09: explicit retained purchase/read/shelf callback fault injection across same-title saves; neither simulation mutates.");

            var legacy=NewHouse(2);var legacyState=JsonUtility.FromJson<XgState>(legacy.S.labState);legacyState.bookLibraryVersion=0;
            legacyState.books.Add(new XgScore{key="lunyu",count=2});legacyState.bookReading="lunyu";legacyState.bookChapter=2;legacyState.bookClock=2;legacyState.bookDuration=10;
            legacy.S.labState=JsonUtility.ToJson(legacyState);Bind(ctl,legacy);foreach(var f in FramesFor(.4))yield return f;
            Need(ctl.Sim.OwnedBooks().Count==8&&!ctl.Sim.LibraryOpen&&ctl.Sim.S.bookClock==2&&ctl.Sim.ChaptersRead("lunyu")==2,"legacy eight rights and active progress migrate without entry fee");
            XgReadingWindow.Open(ctl,"lunyu");foreach(var f in FramesFor(.35))yield return f;
            Need(reader.Visible&&Texts(reader.Window).Contains("公版原作选段")&&ctl.Sim.S.bookCompletions.Count==0,"legacy material remains readable and creates no invented completion");
            click((RectTransform)Named(reader.Window,"Primer").transform);Need(Texts(reader.Window).Contains("随附笔记"),"bundled primer preserved separately from purchased count");
            Save(rt,5);Load(rt,5);foreach(var f in FramesFor(.35))yield return f;Need(ctl.Sim.OwnedBooks().Count==8&&ctl.Sim.S.bookCompletions.Count==0,"migration idempotent after actual reload");
            Done("11: legacy eight entitlements, active chapter, separate bundled note and no invented historical completion.");

            Bind(ctl,NewHouse(6));foreach(var f in FramesFor(.4))yield return f;
            // Explicit owned/progress fixture for shelf geometry and persisted reaction recovery, not a purchase/completion claim.
            foreach(var entry in XgBooks.Library.Take(12))ctl.Sim.S.bookOwned.Add(entry.id);
            ctl.Sim.S.books.Add(new XgScore{key="qianziwen",count=2});XgReadingWindow.Open(ctl,"qianziwen");foreach(var f in FramesFor(.4))yield return f;
            Need(reader.Window.rect.width==820&&reader.Window.rect.height==570,"reader uses declared Aero dimensions");
            Need(Get<RectTransform>(reader,"shelf").GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Book_"))==9,"owned shelf first page contains nine rows");
            click((RectTransform)Named(reader.Window,"NextPage").transform);yield return null;
            Need(Get<RectTransform>(reader,"shelf").GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Book_"))==3,"owned shelf second page contains remaining three rows");
            click((RectTransform)Named(reader.Window,"PreviousPage").transform);yield return null;
            Need(ctl.Sim.S.bookReaderReactions.Count==0,"disabled service accepts no stage-six generation request");
            var receipt=new XgBookReaderReaction{key="book.chapter:qianziwen:1",bookId="qianziwen",chapter=1,status="returned",reply="QA 持久化读后感",english=false};
            ctl.Sim.S.bookReaderReactions.Add(receipt);foreach(var f in FramesFor(.35))yield return f;
            Need(Get<TMP_Text>(reader,"reactionText").text.Contains(receipt.reply),"persisted successful reaction displays in visible read chapter");
            language(true);ctl.Sim.English=true;foreach(var f in FramesFor(.35))yield return f;
            Need(!Get<TMP_Text>(reader,"reactionText").text.Contains(receipt.reply)&&ctl.Sim.S.bookReaderReactions.Count==1,"language change uses fixed feedback without another accepted request");
            receipt.english=true;receipt.reply=null;foreach(var f in FramesFor(.35))yield return f;
            Need(Get<TMP_Text>(reader,"reactionText").text.StartsWith("Reading response",StringComparison.Ordinal),"null saved result safely falls back");
            receipt.status="requested";foreach(var f in FramesFor(.35))yield return f;
            Need(receipt.status=="interrupted","orphaned saved request becomes interrupted and is not retried");
            receipt.status="requested";Set(reader,"reactionInFlight",receipt);int ticket=Get<int>(reader,"reactionTicket");
            click((RectTransform)Named(reader.Window,"Close").transform);
            Need(receipt.status=="cancelled"&&Get<int>(reader,"reactionTicket")>ticket&&Get<XgBookReaderReaction>(reader,"reactionInFlight")==null,"hide invalidates synthetic in-flight result ticket and records cancellation");
            XgReadingWindow.Open(ctl,"qianziwen");foreach(var f in FramesFor(.35))yield return f;
            Save(rt,6);Load(rt,6);foreach(var f in FramesFor(.35))yield return f;
            Need(!reader.Visible&&ctl.Sim.S.bookReaderReactions.Count==1&&ctl.Sim.S.bookReaderReactions[0].status=="cancelled","cancelled receipt survives disk load without replay");
            XgReadingWindow.Open(ctl,"qianziwen");foreach(var f in FramesFor(.35))yield return f;
            reader.enabled=false;Need(!reader.Visible&&!XgLifeHostActivity.ReadingWindowVisible,"component disable releases actual visible gate");reader.enabled=true;
            XgReadingWindow.Open(ctl,"qianziwen");foreach(var f in FramesFor(.35))yield return f;capture("c4-06-stage-six-reader");
            Need(LocalLlm.Instance==null||!LocalLlm.Instance.Ready,"suite never loaded or ran a model");
            Done("10: real shelf pagination/visibility plus synthetic persisted returned/null/interrupted/cancelled reaction boundaries. Live generation and native late-result delivery remain untested; no hard cancellation claim.");
        }
        finally
        {
            if(saveFault!=null)rt.Saving-=saveFault;
            rt.useDiskSave=false;reader.Hide();StoryFocus.Probe=oldProbe;StoryFocus.ExclusiveProbe=oldExclusive;
            language(originalLanguage);Bind(ctl,original);ctl.enabled=originalEnabled;
            notifications.enabled=notificationsEnabled;
            for(int i=0;i<loans.Length;i++)if(loans[i]!=null)loans[i].enabled=loanStates[i];
        }
    }
}
#endif
