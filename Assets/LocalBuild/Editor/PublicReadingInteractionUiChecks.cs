#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Bodu;
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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Isolated Main: fixture messages and eligibility, production Update/Tray/LCD/YY rendering.
// No model, natural story/calendar progression, physical OS input or photorealistic-generation claim.
public static class PublicReadingInteractionUiChecks
{
    const string Key="SidebarLayoutChecks.PublicReadingInteraction";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    public static bool Requested=>SessionState.GetBool(Key,false);
    public static void ClearRequest()=>SessionState.EraseBool(Key);
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount{get;private set;}
    public const string Scope="Isolated Main actual LCD UI. Twenty-six bilingual captures verify all 13 scene resources: three licensed photos (Qinghai, GTX1080, noodles) and ten explicitly labelled project-original illustrations, including the portrait graduation image. Actual authored era strings are inserted as explicit YY message fixtures; this is not natural delivery, not image-model output, and not completion of the original photorealistic-photo request. Exact sprite, rendered mesh aspect, group title, sender, caption, illustration notice and viewport geometry are checked. Explicit stage3/public-date/elapsed-eligibility fixtures exercise live notifier Update, native tray quiet timing, focused TMP input and synthetic InputSystem mouse-state gates. Actual LCD pointer-down/up spans at least three player frames while an onAfterUpdate fixture maintains the synthetic held device after native input processing and synchronizes player/editor state buffers; automatic InputSystem UI modules are temporarily disabled to avoid duplicate pointer delivery, so this is not physical OS input. Real native tray and browser routing, exact core 29/30-second scheduler checks, a naturally elapsed 30-safe-second tray quiet window, and explicit life-open/result-pending priority fixtures. Independent story/economic drivers are frozen, no model or natural progression claim.";
    [MenuItem("Tools/Local Verification/Public reading interactions actual LCD")]
    public static void Run()
    {
        var hook=typeof(SidebarLayoutChecks).GetField("PublicReadingInteractionHookVersion",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
        if(hook==null||Convert.ToInt32(hook.GetRawConstantValue())<1)throw new InvalidOperationException("Import the PublicReadingInteraction Sidebar hook before Run.");
        SessionState.SetBool(Key,true);try{BookLibraryUiChecks.Run();}catch{ClearRequest();throw;}
    }
    static T Get<T>(object o,string name)
    {
        for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,F);if(f!=null)return (T)f.GetValue(o);}
        throw new InvalidOperationException("Missing production field "+name);
    }
    static void Need(bool ok,string why){AssertionCount++;if(!ok)throw new InvalidOperationException("Public reading interactions: "+why);}
    static void Done(string text){Checks.Add(text);Debug.Log("Public reading interactions: "+text);}
    static IEnumerable<object> Wait(double seconds=.65)
    {double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerable<object> Until(Func<bool> ready,string why,double seconds=12)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!ready()){if(EditorApplication.timeSinceStartup>=end)Need(false,why);yield return null;}Canvas.ForceUpdateCanvases();yield return null;yield return null;}
    static WindowManager Window(Component view)=>Get<WindowManager>(view,"window");
    static IEnumerable<object> Ready(WindowManager window)
    {
        foreach(var f in Until(()=>
        {
            if(!window.isOn||!window.gameObject.activeInHierarchy)return false;
            foreach(var g in window.GetComponentsInParent<CanvasGroup>(true))if(g.alpha<.95f||!g.interactable||!g.blocksRaycasts)return false;
            var a=window.windowAnimator;return a==null||!a.isActiveAndEnabled||!a.IsInTransition(0)&&a.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"native window becomes interactive: "+window.name))yield return f;
        foreach(var f in Wait())yield return f;
    }
    static Button Named(Component root,string name)
    {
        var buttons=root.GetComponentsInChildren<Button>(true).Where(b=>b.name==name&&b.gameObject.activeInHierarchy).ToArray();
        Need(buttons.Length==1,"one actual visible "+name);return buttons[0];
    }
    static void Inside(RectTransform child,RectTransform viewport,string why)
    {
        var corners=new Vector3[4];child.GetWorldCorners(corners);
        foreach(var c in corners){var p=viewport.InverseTransformPoint(c);Need(p.x>=viewport.rect.xMin-1&&p.x<=viewport.rect.xMax+1&&p.y>=viewport.rect.yMin-1&&p.y<=viewport.rect.yMax+1,why);}
    }
    static void Live(ChapterOneRuntime rt)
    {typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(rt,new object[]{false});rt.useDiskSave=false;rt.tickEnabled=false;}
    static ChapterOneSim Fixture(bool english,EraEvent photo=null)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=english;
        lab.S.stage=lab.S.stageVision=lab.S.stageSequence=3;lab.S.abilitiesEmerged=new List<int>{1,2,3};
        lab.S.openingFocus=false;lab.S.teachingFlow=false;lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;lab.MarkAfterthought("first.drift");
        var yy=new YYState{lingguangUnlocked=true,selected=YYChatHub.LaoZhou};
        foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        if(photo!=null)yy.conversations.Single(c=>c.id==YYChatHub.EraGroup).messages.Add(new YYMessage{
            from=YYChatHub.EraGroup,kind=YYKind.Text,text="["+(english?photo.whoEn:photo.who)+"] "+(english?photo.textEn:photo.text)});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);house.S.chatState=JsonUtility.ToJson(yy);
        GameCalendar.Advance(house.S,GameCalendar.DayFor(3,0));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
        return house;
    }
    static void Bind(XingGuangController ctl,ChapterOneSim house)
    {
        ctl.runtime.SetSimulationForTests(house);Live(ctl.runtime);
        Need(ctl.BoundTo(house)&&YYChatHub.Instance.BoundTo(house),"controller and YY bind exact fixture owner");
        DesktopNotifications.Install(ctl.runtime);
        XgPublicDiscourseDesktop.SetReleaseProjection(house,41,20161231,"","",Array.Empty<string>());
    }
    static XgPublicDiscourseDeliveryState EligibleFixture(XingGuangController ctl)
    {
        var s=new XgPublicDiscourseDeliveryState{initialized=true,stage=3,safeSeconds=600,stageSafeSeconds=60};
        ctl.Sim.S.publicDiscourseDelivery=s;
        Need(XgPublicDiscourseDesktop.Catalog.All.Any(r=>XgPublicDiscourseSchedule.Eligible(s,r,XgPublicDiscourseDesktop.Context(r.platform),XgPublicDiscourseDesktop.Catalog)),"explicit stage/date/elapsed fixture has an eligible actual catalog record");
        return s;
    }
    static RectTransform Popup(TrayPopups tray,string who=null)
    {
        foreach(var t in tray.GetComponentsInChildren<RectTransform>(true))
            if(t.name=="Tray Popup"&&t.gameObject.activeInHierarchy&&(who==null||t.Find("Inner/Bar/Who").GetComponent<TMP_Text>().text==who))return t;
        return null;
    }
    static bool PopupReady(TrayPopups tray,string who)
    {var p=Popup(tray,who);return p!=null&&Mathf.Abs(p.anchoredPosition.y)<.01f;}
    static IEnumerable<object> CloseFixtureNotices(TrayPopups tray,Action<RectTransform> click)
    {
        double deadline=EditorApplication.timeSinceStartup+30;int closed=0;
        while(tray.IsBusy)
        {
            Need(EditorApplication.timeSinceStartup<deadline,"existing fixture tray drains through actual Close; count="+closed);
            var popup=tray.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(p=>
                (p.name=="Tray Popup"||p.name=="System Balloon")&&p.gameObject.activeInHierarchy&&Mathf.Abs(p.anchoredPosition.y)<.01f);
            if(popup==null){yield return null;continue;}
            var close=(RectTransform)popup.Find(popup.name=="Tray Popup"?"Inner/Bar/Close":"Close");
            Need(close!=null&&close.GetComponent<Button>().IsInteractable(),"existing tray has real active Close control");
            click(close);closed++;
            foreach(var f in Until(()=>popup==null,"real Close completes existing tray animation"))yield return f;
        }
        Done("Independent pre-notifier fixture notices closed through actual LCD controls: "+closed+".");
    }
    static void CoreQuiet()
    {
        var s=new XgPublicDiscourseDeliveryState{initialized=true,stage=3,safeSeconds=600,stageSafeSeconds=60};
        XgPublicDiscourseSchedule.Advance(s,3,1,false,true);
        Need(s.safeSeconds==600&&s.stageSafeSeconds==60&&s.quietRemaining==30,"busy event resets quiet30 without adding safe time");
        for(int i=0;i<29;i++)XgPublicDiscourseSchedule.Advance(s,3,1,true,false);
        Need(s.quietRemaining==1,"29 safe seconds cannot consume all quiet30");
        XgPublicDiscourseSchedule.Advance(s,3,1,false,false);
        Need(s.quietRemaining==1&&s.safeSeconds==629,"background time cannot consume remaining quiet");
        XgPublicDiscourseSchedule.Advance(s,3,1,true,false);
        Need(s.quietRemaining==0&&s.safeSeconds==630,"30th safe second ends the interval exactly");
        Done("Core scheduler exact 29/30 boundary and background-safe-time check executed; explicit pure state fixture.");
    }
    static void FullyRendered(TMP_Text label,string why)
    {
        Need(label!=null&&label.gameObject.activeInHierarchy&&!label.canvasRenderer.cull,why+" is active and visible");
        label.ForceMeshUpdate(true,true);
        Need(!label.isTextOverflowing&&!label.isTextTruncated&&label.preferredHeight<=label.rectTransform.rect.height+1,why+" fully rendered without ellipsis");
    }
    static IEnumerable<object> Photos(XingGuangController ctl,YYChatView view,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        var resource=Resources.Load<TextAsset>("LingGuangV05/Era/era_events");Need(resource!=null,"authored era resource imported");
        var events=EraEvents.Parse(resource.text);
        // Preserve the original three licensed-photo cases first; ten originals are additional cases.
        string[] ids={"lili_qinghai","photo_ajie_1080","photo_ajie_noodles",
            "lili_grad","photo_lili_gulangyu","lili_westlake","photo_xg_euro","photo_dw_desk",
            "photo_dw_ti6","iphone7_queue","photo_xg_civ","photo_lili_cart","photo_lili_ticket"};
        string[] keys={"chat_lili_qinghai","chat_ajie_1080","chat_ajie_noodles",
            "chat_lili_grad","chat_lili_gulangyu","chat_lili_westlake","chat_xg_tv","chat_dw_desk",
            "chat_dw_cafe","chat_dw_iphone","chat_xg_dawn","chat_lili_parcels","chat_lili_ticket"};
        Need(ids.Length==13&&keys.Length==13&&ids.Distinct().Count()==13&&keys.Distinct().Count()==13,"exactly thirteen distinct authored events and scene keys");
        var textures=new HashSet<int>();int captures=0,portraitCases=0,licensedCases=0,illustrationCases=0;
        foreach(bool en in new[]{false,true})for(int i=0;i<ids.Length;i++)
        {
            bool illustration=i>=3,portrait=keys[i]=="chat_lili_grad";
            language(en);var e=events.Get(ids[i]);Need(e!=null&&e.channel==EraEvents.YY,"approved authored YY event "+ids[i]);
            // Missing event/label is a wiring failure, never replaced with a made-up test-only message.
            Need(!string.IsNullOrEmpty(e.text)&&e.text.StartsWith("[图片：",StringComparison.Ordinal)&&
                !string.IsNullOrEmpty(e.textEn)&&e.textEn.StartsWith("[Image: ",StringComparison.Ordinal),"both authored image labels exist for "+ids[i]);
            Bind(ctl,Fixture(en,e));Need(view.EnsureReady(),"native YY built");
            Window(view).OpenWindow();view.OnOpened(YYChatHub.LaoZhou);Window(view).FocusToWindow();
            foreach(var f in Ready(Window(view)))yield return f;
            click((RectTransform)Named(view,"Session_"+YYChatHub.EraGroup).transform);
            foreach(var f in Wait())yield return f;
            Need(YYChatHub.Instance.S.selected==YYChatHub.EraGroup,"actual LCD contact click selected group");
            var title=Get<TMP_Text>(view,"peerName");
            Need(title.text==YYGroup.Title(YYChatHub.Contact(YYChatHub.EraGroup)),"exact localized group title and member count");
            FullyRendered(title,"native group title");
            var images=view.GetComponentsInChildren<Image>().Where(p=>p.name=="Photograph"&&p.gameObject.activeInHierarchy).ToArray();
            Need(images.Length==1,"one actual rendered scene for fixture "+e.id);
            var image=images[0];var expected=DesktopMedia.Picture(keys[i]);
            Need(expected!=null&&expected.texture!=null&&image.sprite==expected&&image.sprite.texture==expected.texture,"actual native Image holds exact imported scene "+keys[i]);
            Need(image.preserveAspect&&!image.canvasRenderer.cull&&image.color.a>.95f&&image.rectTransform.rect.width>100&&image.rectTransform.rect.height>100,"scene remains visible and preserves aspect");
            Need(expected.rect.x==0&&expected.rect.y==0&&expected.rect.width==expected.texture.width&&expected.rect.height==expected.texture.height,"full source sprite is retained without crop");
            float sourceAspect=expected.rect.width/expected.rect.height;
            Need(Mathf.Abs(sourceAspect-(portrait?.75f:4f/3f))<.005f,"imported source retains approved portrait/landscape aspect for "+keys[i]);
            // Inspect the actual native CanvasRenderer quad, not merely the preserveAspect flag.
            Canvas.ForceUpdateCanvases();var mesh=image.canvasRenderer.GetMesh();
            Need(mesh!=null&&mesh.vertexCount>=4,"actual native scene mesh exists");
            var vertices=mesh.vertices;
            float meshWidth=vertices.Max(v=>v.x)-vertices.Min(v=>v.x),meshHeight=vertices.Max(v=>v.y)-vertices.Min(v=>v.y);
            Need(meshWidth>100&&meshHeight>100&&Mathf.Abs(meshWidth/meshHeight-sourceAspect)<.005f,"actual rendered scene mesh keeps native source aspect");
            if(portrait){Need(meshHeight>meshWidth,"graduation remains visibly portrait");portraitCases++;}
            if(!en)textures.Add(image.sprite.texture.GetInstanceID());
            var viewport=Get<ScrollRect>(view,"scroll").viewport;
            Inside(image.rectTransform,viewport,"entire scene fits message viewport");
            var senders=view.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="Sender"&&t.gameObject.activeInHierarchy).ToArray();
            Need(senders.Length==1&&senders[0].text==(en?e.whoEn:e.who),"one exact authored localized sender");
            FullyRendered(senders[0],"native sender");Inside(senders[0].rectTransform,viewport,"entire sender fits message viewport");
            var caption=image.transform.parent.Find("Caption").GetComponent<TMP_Text>();
            string text=en?e.textEn:e.text;string wanted=text.Substring(text.IndexOf(']')+1).TrimStart();
            Need(caption.text==wanted&&!caption.text.Contains("[图片：")&&!caption.text.Contains("[Image:"),"native caption uses exact authored words without image token");
            FullyRendered(caption,"native caption");Inside(caption.rectTransform,viewport,"entire caption fits message viewport");
            var notice=image.transform.parent.Find("Illustration Notice");
            if(illustration)
            {
                Need(notice!=null,"original illustration carries visible provenance notice");
                var label=notice.GetComponent<TMP_Text>();
                Need(label!=null&&label.text==(en?"Original illustration · not a photo":"原创情景插画 · 非实拍"),"exact localized original-illustration notice");
                FullyRendered(label,"original-illustration notice");Inside(label.rectTransform,viewport,"entire illustration notice fits message viewport");
                illustrationCases++;
            }
            else{Need(notice==null,"licensed photograph is not mislabeled as original illustration");licensedCases++;}
            capture("reading-photo-"+(en?"en":"zh")+"-"+e.id+".png");captures++;
            Done((en?"en":"zh")+": actual YY LCD group selection, exact imported "+keys[i]+", rendered aspect, title, sender, authored caption, provenance notice and viewport geometry verified (explicit message fixture; "+(illustration?"original illustration":"licensed photograph")+").");
        }
        Need(textures.Count==13,"thirteen distinct imported scene textures");
        Need(captures==26&&portraitCases==2&&licensedCases==6&&illustrationCases==20,"all thirteen bilingual scenes captured, including portrait and both provenance categories");
        Window(view).CloseWindow();foreach(var f in Wait())yield return f;
    }
    // This iterator runs on EditorApplication.update, whose input view is separate from the player.
    // Keep only this synthetic device consistent in both views; never alter application focus.
    sealed class HeldMouseFixture : IDisposable
    {
        readonly Mouse mouse;
        Vector2 position;
        bool held;
        int lastPlayerFrame=-1;
        bool playerPending;
        public int PlayerFrames{get;private set;}
        public HeldMouseFixture(Mouse mouse)
        {this.mouse=mouse;InputSystem.onAfterUpdate+=AfterInputUpdate;}
        void Apply(InputUpdateType update)
        {InputState.Change(mouse,new MouseState { position=position }.WithButton(MouseButton.Left,held),update);}
        public void Set(bool pressed,Vector2 at)
        {
            held=pressed;position=at;PlayerFrames=0;lastPlayerFrame=-1;playerPending=true;Refresh();
        }
        public void Refresh()
        {
            // Keep editor writes in the editor phase; cross-buffer writes leave the player press cache stale.
            Apply(InputUpdateType.Editor);
            if(held)mouse.MakeCurrent();
        }
        void AfterInputUpdate()
        {
            if(!mouse.added||(!held&&!playerPending))return;
            var update=InputState.currentUpdateType;
            if(update!=InputUpdateType.Dynamic&&update!=InputUpdateType.Fixed)return;
            // Native mouse events can replace Mouse.current. Reassert the explicit fixture after
            // event processing, before production MonoBehaviour.Update reads its real InputBusy gate.
            Apply(update);playerPending=false;
            if(!held)return;
            mouse.MakeCurrent();
            if(lastPlayerFrame!=Time.frameCount){lastPlayerFrame=Time.frameCount;PlayerFrames++;}
        }
        public void Dispose()
        {
            InputSystem.onAfterUpdate-=AfterInputUpdate;
            // The owner immediately removes this synthetic device; never cross-write player state on disposal.
            held=false;playerPending=false;
        }
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        Checks.Clear();AssertionCount=0;CoreQuiet();
        var rt=ctl.runtime;var yy=YYChatHub.Instance;var view=yy?.View;
        Need(view!=null&&!rt.TestMode&&!rt.useDiskSave&&!rt.tickEnabled,"native YY and isolated Main live host with economy frozen");
        Need(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready,"model service inactive");
        var tray=PrologueDirector.Desk?.layer?.GetComponent<TrayPopups>();Need(tray!=null&&tray.isActiveAndEnabled,"real native tray present");
        var bridge=Object.FindAnyObjectByType<AeroLcdSourceBridge>();Need(bridge!=null&&bridge.inputSurface!=null,"real LCD input bridge present");
        var browser=Object.FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);Need(browser!=null,"native browser present");
        XgPublicDiscourseNotifier.Install(ctl);var notifier=ctl.GetComponent<XgPublicDiscourseNotifier>();
        var original=rt.Sim;bool oldEnglish=GameText.IsEnglish,oldCtl=ctl.enabled,oldNotifier=notifier.enabled,oldYy=yy.enabled;
        bool oldLabOpen=ctl.Window.isOn,oldYyOpen=Window(view).isOn,oldBrowserOpen=Window(browser).isOn;
        var oldProbe=StoryFocus.Probe;var oldExclusive=StoryFocus.ExclusiveProbe;var oldSelected=EventSystem.current.currentSelectedGameObject;
        var priorMouse=Mouse.current;Mouse mouse=null;HeldMouseFixture heldMouse=null;bool completed=false;
        var notifications=DesktopNotifications.Install(rt);
        // Independent drivers are frozen only in this isolated UI fixture; production notifier/tray stay live.
        var quietNames=new HashSet<string>{"ShopNotices","XgEndingPreparationHost","XgResidentSpider","ZhouCardLoan","XgLifeEventCard","XgAppAcquisitionHost","XgDailyUnlockNotifier","GameEraClock","XgEraDeliveryDesk","XgEraAdvantageBridge"};
        var drivers=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(b=>b==notifications||quietNames.Contains(b.GetType().Name)).ToArray();
        var driverStates=drivers.Select(b=>b.enabled).ToArray();
        var modules=Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include,FindObjectsSortMode.None);var moduleStates=modules.Select(m=>m.enabled).ToArray();
        ctl.enabled=false;yy.enabled=false;notifier.enabled=false;
        foreach(var d in drivers)d.enabled=false;
        StoryFocus.Probe=()=>false;StoryFocus.ExclusiveProbe=()=>false;
        try
        {
            // Freeze independent producers before swapping the fixture date/stage. The tray stays live.
            ctl.Window.CloseWindow();Window(browser).CloseWindow();
            var brain=ctl.GetComponent<XgBrainWindow>();if(brain!=null&&brain.Visible){click((RectTransform)brain.Window.Find("Title/Close"));foreach(var f in Wait())yield return f;}
            foreach(var f in CloseFixtureNotices(tray,click))yield return f;
            foreach(var f in Photos(ctl,view,language,click,capture))yield return f;
            language(false);Bind(ctl,Fixture(false));
            Done("Before notifier: InnerVoice="+InnerVoice.Busy+", tray="+tray.IsBusy+", reading="+XgLifeHostActivity.ReadingWindowVisible+", paused="+XgLifeHostActivity.GamePaused+".");
            foreach(var f in Until(()=>!InnerVoice.Busy&&!tray.IsBusy&&!XgLifeHostActivity.ReadingWindowVisible&&!XgLifeHostActivity.GamePaused,"fixture has no independent presentation conflict"))yield return f;
            foreach(var m in modules)m.enabled=false;
            mouse=InputSystem.AddDevice<Mouse>("PublicReadingQaMouse");mouse.MakeCurrent();heldMouse=new HeldMouseFixture(mouse);
            notifier.enabled=true;foreach(var f in Wait(.2))yield return f;
            // Give the operator time to restore the real Game view after startup/import.
            // This observes the original gate; it never sets focus flags or advances safe time.
            foreach(var f in Until(()=>Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground application required; never fabricate focus",60))yield return f;
            Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground application required; never fabricate focus");

            // Focus a real native text field, but explicitly fixture focus rather than claiming typed user text.
            Window(view).OpenWindow();view.OnOpened(YYChatHub.LaoZhou);Window(view).FocusToWindow();foreach(var f in Ready(Window(view)))yield return f;
            var input=Get<TMP_InputField>(view,"input");EventSystem.current.SetSelectedGameObject(input.gameObject);input.ActivateInputField();
            foreach(var f in Until(()=>input.isFocused,"native TMP field focus"))yield return f;
            var delivery=EligibleFixture(ctl);double safe=delivery.safeSeconds;
            foreach(var f in Wait(.5))yield return f;
            Need(delivery.notifiedIds.Count==0&&delivery.safeSeconds==safe&&delivery.quietRemaining==30,"focused native input prevents scheduling and safe-time accumulation");
            capture("reading-notifier-01-input-held.png");input.DeactivateInputField();EventSystem.current.SetSelectedGameObject(null);Window(view).CloseWindow();
            foreach(var f in Wait())yield return f;
            heldMouse.Set(true,mouse.position.ReadValue());delivery=EligibleFixture(ctl);safe=delivery.safeSeconds;
            foreach(var f in Wait(.5)){heldMouse.Refresh();yield return f;}
            Need(Mouse.current==mouse&&mouse.leftButton.isPressed,"explicit synthetic held-mouse state still active");
            Need(heldMouse.PlayerFrames>=3,"held mouse spans at least three actual player frames");
            Need(delivery.notifiedIds.Count==0&&delivery.safeSeconds==safe&&delivery.quietRemaining==30,"held pointer outside notice prevents new notification and safe-time accumulation");
            heldMouse.Set(false,mouse.position.ReadValue());

            tray.EnqueueNotification("QA tray timing fixture","Existing tray activity",20,()=>{},()=>true,()=>false);
            delivery=EligibleFixture(ctl);safe=delivery.safeSeconds;
            foreach(var f in Until(()=>PopupReady(tray,"QA tray timing fixture"),"actual neutral tray appears"))yield return f;
            foreach(var f in Wait(.5))yield return f;
            Need(delivery.notifiedIds.Count==0&&delivery.safeSeconds==safe&&delivery.quietRemaining==30,"actual tray busy resets quiet30 and earns zero safe seconds");
            capture("reading-notifier-02-tray-busy.png");
            click((RectTransform)Popup(tray,"QA tray timing fixture").Find("Inner/Bar/Close"));
            foreach(var f in Until(()=>!tray.IsBusy,"actual close finishes tray lifecycle"))yield return f;
            Need(delivery.quietRemaining>29&&delivery.notifiedIds.Count==0,"quiet interval begins only after tray lifecycle releases");
            double start=EditorApplication.timeSinceStartup;
            while(delivery.notifiedIds.Count==0)
            {
                Need(EditorApplication.timeSinceStartup-start<45,"natural30-safe-second interval completes in foreground");
                if(delivery.quietRemaining>0)Need(Popup(tray,"公开讨论")==null,"no optional popup during natural quiet interval");
                yield return null;
            }
            // The accepted notice now makes tray.IsBusy true and can reset quietRemaining again.
            // Check total earned safe time, not the transient value after acceptance.
            Need(delivery.safeSeconds-safe>=30,"delivery follows naturally accumulated safe quiet interval");
            string intendedId=delivery.notifiedIds.Single();
            foreach(var f in Until(()=>PopupReady(tray,"公开讨论"),"actual public notice settles"))yield return f;
            var popup=Popup(tray,"公开讨论");var action=(RectTransform)popup.Find("Inner/Action");
            var pointer=(PointerEventData)typeof(FilesWindowUiChecks).GetMethod("Pointer",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{action});
            var surface=bridge.inputSurface;surface.OnPointerEnter(pointer);
            var hit=Get<PointerEventData>(surface,"hoverData")?.pointerCurrentRaycast.gameObject;
            Need(hit!=null&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit)==action.gameObject,"actual LCD raycast owns tray action before pointer-down");
            heldMouse.Set(true,pointer.position);surface.OnPointerDown(pointer);int heldFrames=0;
            foreach(var f in Wait(.55)){heldMouse.Refresh();Need(popup!=null&&popup.gameObject.activeInHierarchy&&action.GetComponent<Button>().IsInteractable(),"popup stays visible/clickable throughout held pointer frames");heldFrames++;yield return f;}
            Need(heldFrames>=3&&Mouse.current==mouse&&mouse.leftButton.isPressed,"held-pointer regression crosses at least three real frames with device state pressed");
            Need(heldMouse.PlayerFrames>=3,"tray pointer hold spans at least three actual player frames");
            Need(Get<string>(browser,"page")!="discourse","no browser route before pointer-up");
            capture("reading-notifier-03-pointer-held.png");heldMouse.Set(false,mouse.position.ReadValue());surface.OnPointerUp(pointer);
            foreach(var f in Until(()=>Get<string>(browser,"page")=="discourse"&&Get<string>(browser,"discourseId")==intendedId,"actual pointer-up opens the delivered catalog item"))yield return f;
            foreach(var f in Ready(Window(browser)))yield return f;capture("reading-notifier-04-pointer-opened.png");
            Done("Live notifier: native text focus, held synthetic input, real tray busy/safe time, natural quiet30, actual LCD split-frame pointer-down/up and exact delivered browser route verified.");
            Window(browser).CloseWindow();foreach(var f in Until(()=>!tray.IsBusy,"clicked optional tray finishes"))yield return f;

            // Explicit pending life states exercise priority, without claiming a natural life event or consequence.
            delivery=EligibleFixture(ctl);ctl.Sim.S.lifeOpen="zhou_card";safe=delivery.safeSeconds;
            foreach(var f in Wait(.5))yield return f;
            Need(ctl.Sim.LifeEventOpen&&delivery.notifiedIds.Count==0&&delivery.safeSeconds==safe&&delivery.quietRemaining==30,"LifeEventOpen has scheduling priority");
            ctl.Sim.S.lifeOpen="";ctl.Sim.S.lifeResultId="zhou_card";delivery=EligibleFixture(ctl);safe=delivery.safeSeconds;
            foreach(var f in Wait(.5))yield return f;
            Need(ctl.Sim.LifeEventResultPending&&delivery.notifiedIds.Count==0&&delivery.safeSeconds==safe&&delivery.quietRemaining==30,"LifeEventResultPending has scheduling priority");
            ctl.Sim.S.lifeResultId="";delivery=EligibleFixture(ctl);
            foreach(var f in Until(()=>PopupReady(tray,"公开讨论"),"second explicit eligibility fixture shows optional notice"))yield return f;
            var displaced=Popup(tray,"公开讨论");
            ctl.Sim.S.lifeOpen="zhou_card";
            tray.EnqueueNotification("QA life priority fixture","Important event can use the native tray",10,()=>{},()=>true,()=>false);
            foreach(var f in Until(()=>displaced==null&&PopupReady(tray,"QA life priority fixture"),"pending life event invalidates optional notice and releases tray to queued priority fixture"))yield return f;
            capture("reading-notifier-05-life-priority.png");
            click((RectTransform)Popup(tray,"QA life priority fixture").Find("Inner/Bar/Close"));
            foreach(var f in Until(()=>!tray.IsBusy,"priority fixture closes through native control"))yield return f;
            Need(delivery.notifiedIds.Count==1,"cancelled optional keeps one stage receipt, preventing a repeated interruption");
            Done("Explicit life-open/result-pending gates and live optional-notice cancellation release native tray for a queued priority fixture; no natural event or economic consequence claimed.");
            completed=true;Done(Scope);Done("Assertions: "+AssertionCount+". Actual screenshots still require visual review.");
        }
        finally
        {
            if(!completed)try{capture("reading-interactions-failure.png");}catch(Exception ex){Checks.Add("Failure capture unavailable: "+ex.Message);}
            notifier.enabled=false;
            heldMouse?.Dispose();
            if(mouse!=null&&mouse.added){InputSystem.RemoveDevice(mouse);}if(priorMouse!=null&&priorMouse.added)priorMouse.MakeCurrent();
            bridge.inputSurface.CancelInteraction();
            for(int i=0;i<modules.Length;i++)if(modules[i]!=null)modules[i].enabled=moduleStates[i];
            tray.enabled=false;tray.enabled=true;
            rt.SetSimulationForTests(original);Live(rt);language(oldEnglish);
            StoryFocus.Probe=oldProbe;StoryFocus.ExclusiveProbe=oldExclusive;
            for(int i=0;i<drivers.Length;i++)if(drivers[i]!=null)drivers[i].enabled=driverStates[i];
            yy.enabled=oldYy;ctl.enabled=oldCtl;notifier.enabled=oldNotifier;
            if(oldLabOpen)ctl.Window.OpenWindow();else ctl.Window.CloseWindow();
            if(oldYyOpen)Window(view).OpenWindow();else Window(view).CloseWindow();
            if(oldBrowserOpen)Window(browser).OpenWindow();else Window(browser).CloseWindow();
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(oldSelected);
        }
    }
}
#endif
