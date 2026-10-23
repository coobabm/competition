#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using LingGuangV05.Desktop.Apps;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Root runs serially through its existing isolated Main/LCD runner. No standalone Unity launch.
// Stage/date/household readiness are explicit fixtures. App receipts, script replies, clicked choices,
// offered links, downloaded bytes and installation completion are never fabricated.
public static class AppAcquisitionActualUiChecks
{
    const string Key="SidebarLayoutChecks.AppAcquisitionActualUi";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static bool Requested=>SessionState.GetBool(Key,false);
    public const string Scope="Bilingual actual LCD app acquisition: all six runtime scripts, one selected branch per choice, real player choices/link cards/browser bytes and Next/Install/Finish clicks, native desktop icons, save failure/retry, actual isolated archive reload and adversarial stale callbacks. Explicit stage/date/powered-household fixtures and elapsed eligibility fixtures: natural 180/240-second waits (and other eligibility delays) are NOT verified. External StoryFocus and network-disconnection signals are fixtures. No natural full progression, data-file import, contract signing or model inference claim.";
    public static void ClearRequest()=>SessionState.EraseBool(Key);
    [MenuItem("Tools/Local Verification/App acquisition actual LCD")]
    public static void Run()
    {
        var hook=typeof(SidebarLayoutChecks).GetField("AppAcquisitionActualUiHookVersion",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        if(hook==null||Convert.ToInt32(hook.GetRawConstantValue())<1)throw new InvalidOperationException("Import the reviewed AppAcquisition phase19 Sidebar hook before Run.");
        SessionState.SetBool(Key,true);try{BookLibraryUiChecks.Run();}catch{ClearRequest();throw;}
    }
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount{get;private set;}
    static T Get<T>(object value,string field)=>(T)value.GetType().GetField(field,F).GetValue(value);
    static void Need(bool ok,string message){AssertionCount++;if(!ok)throw new InvalidOperationException("App acquisition actual UI: "+message);}
    static void Done(string message){Checks.Add(message);Debug.Log("App acquisition actual UI: "+message);}
    static IEnumerable<object> Settle(int count=3){Canvas.ForceUpdateCanvases();while(count-->0)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerable<object> Wait(double seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
    static IEnumerable<object> Until(Func<bool> ready,string message,double seconds=12)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!ready()){if(EditorApplication.timeSinceStartup>=end)throw new InvalidOperationException("App acquisition timeout: "+message);yield return null;}}
    static IEnumerable<object> Ready(WindowManager window)
    {
        foreach(var f in Until(()=>
        {
            if(window==null||!window.isOn||!window.gameObject.activeInHierarchy)return false;
            foreach(var group in window.GetComponentsInParent<CanvasGroup>(true))if(group.alpha<.95f||!group.interactable||!group.blocksRaycasts)return false;
            var animator=window.windowAnimator;return animator==null||!animator.isActiveAndEnabled||!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"native window naturally interactive"))yield return f;
    }
    static XgAppAcquisitionHost Host(XingGuangController ctl)=>ctl.GetComponent<XgAppAcquisitionHost>();
    static XgAppAcquisitionState State(XingGuangController ctl)=>ctl.Sim.S.appAcquisition;
    static YYConversation Group=>YYChatHub.Instance.Conversation(YYChatHub.EraGroup);
    static Button LabelButton(Component root,string label)
    {
        Need(root!=null,"button parent exists: "+label);
        var buttons=root.GetComponentsInChildren<Button>(true).Where(b=>b.gameObject.activeInHierarchy&&b.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text==label)).ToArray();
        Need(buttons.Length==1,"one visible actual button: "+label+" ("+buttons.Length+")");
        Need(buttons[0].IsInteractable(),"button interactable: "+label);return buttons[0];
    }
    static IEnumerable<object> Tap(Button button,Action<RectTransform> click)
    {Need(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable(),"actual LCD click target live");click((RectTransform)button.transform);foreach(var f in Settle())yield return f;}
    static void Disk(ChapterOneRuntime rt)
    {
        string directory=ChapterOneRuntime.VerificationSaveDirectory;
        Need(!string.IsNullOrEmpty(directory)&&Path.GetFullPath(directory).Contains(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar),"explicit LocalVerification save directory");
        Need(Path.GetFullPath(rt.SavePath).StartsWith(Path.GetFullPath(directory)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"disk writes remain inside isolated directory");rt.useDiskSave=true;
    }
    static ChapterOneSim Fixture(bool english)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=english;
        lab.S.stage=lab.S.stageVision=lab.S.stageSequence=2;lab.S.gradualSystems=true;lab.S.teachingFlow=false;lab.S.fullOpen=true;
        lab.S.abilitiesEmerged=new List<int>{1,2};lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;
        var yy=new YYState{lingguangUnlocked=true};foreach(var contact in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=contact.id});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);house.S.chatState=JsonUtility.ToJson(yy);
        GameCalendar.Advance(house.S,GameCalendar.DayIndex(new DateTime(2016,7,20)));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201607;
        return house;
    }
    static void Bind(XingGuangController ctl,ChapterOneSim save)
    {
        ctl.runtime.useDiskSave=false;ctl.runtime.SetSimulationForTests(save);
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});ctl.runtime.tickEnabled=false;
        Need(ctl.BoundTo(save)&&YYChatHub.Instance.EndingBoundTo(save),"controller and YY bound to exact isolated save");
        Disk(ctl.runtime);Need(ctl.SaveNow(),"explicit fixture saved with real SaveNow");Host(ctl).enabled=true;
    }
    static IEnumerable<object> OpenGroup(XingGuangController ctl)
    {
        Need(YYChatHub.Instance.router.Open(YYChatHub.AppId,YYChatHub.EraGroup),"real YY route opens group");
        foreach(var f in Ready(YYChatHub.Instance.router.Get(YYChatHub.AppId).Window))yield return f;
        foreach(var f in Settle())yield return f;
        var scroll=Get<ScrollRect>(YYChatHub.Instance.View,"scroll");scroll.StopMovement();scroll.verticalNormalizedPosition=0;
        foreach(var f in Settle())yield return f;
    }
    static void Stage(XingGuangController ctl,int value)
    {ctl.Sim.S.stage=ctl.Sim.S.stageVision=ctl.Sim.S.stageSequence=value;ctl.Sim.S.abilitiesEmerged=Enumerable.Range(1,value).ToList();Done("Explicit stage precondition changed to "+value+"; no natural progression claim.");}
    static void ElapsedFixture(XingGuangController ctl,XgGroupIntroScript script,bool enabled)
    {
        if(!enabled)return;
        var state=State(ctl);state.activeSeconds+=script.trigger.delaySec+1;
        ctl.Sim.IntroProgress(script.id).eligibleSince=state.activeSeconds-script.trigger.delaySec;
        Done(script.id+": explicit elapsed-eligibility fixture; this run does NOT verify the natural waiting duration.");
    }
    static IEnumerable<object> PauseWindow(XingGuangController ctl,Action<string> capture,string name)
    {
        var host=Host(ctl);double before=State(ctl).activeSeconds;var probe=StoryFocus.ExclusiveProbe;
        var pending=Get<RectTransform>(host,"thought");var oldThought=pending!=null?Get<XgBtn>(host,"thoughtNext")?.button.onClick:null;
        try
        {
            StoryFocus.ExclusiveProbe=()=>true;
            foreach(var f in Wait(.35))yield return f;
            Need(host.Held&&Math.Abs(State(ctl).activeSeconds-before)<1e-9,"external story-focus fixture stops active acquisition clock");
            Need(Get<RectTransform>(host,"thought")==null,"interrupted thought removed before deferred Destroy");
            var page=Get<RectTransform>(host,"downloadPage");Need(page==null||!page.gameObject.activeInHierarchy,"interrupted download page hidden");
            int revision=host.Revision;oldThought?.Invoke();Need(host.Revision==revision,"adversarial retained thought callback cannot act while held");
            capture(name+"-held.png");
        }
        finally{StoryFocus.ExclusiveProbe=probe;}
        foreach(var f in Until(()=>!host.Held,"external hold released"))yield return f;
    }
    static IEnumerable<object> Script(XingGuangController ctl,XgGroupIntroScript script,bool english,int variant,bool delayFixture,Action<RectTransform> click,Action<string> capture)
    {
        var host=Host(ctl);string tag="app-"+(english?"en-":"zh-")+script.id;
        ElapsedFixture(ctl,script,delayFixture);
        foreach(var f in Until(()=>State(ctl).activeScript==script.id,"start "+script.id,script.trigger.delaySec+25))yield return f;
        if(script.steps[0].type=="group"&&!YYChatHub.Instance.IsShowing(YYChatHub.EraGroup))
        {
            string first=GameText.T(script.steps[0].text,script.steps[0].textEn);
            foreach(var f in Until(()=>Group.messages.Any(m=>m.from==YYChatHub.EraGroup&&m.text==first),"first real NPC line before opening YY"))yield return f;
            Need(Get<RectTransform>(host,"attention")?.gameObject.activeInHierarchy==true,"actual unread intro line pulses YY taskbar");capture(tag+"-taskbar.png");
        }
        foreach(var f in OpenGroup(ctl))yield return f;
        bool faultDone=false,held=false;int selected=0;double end=EditorApplication.timeSinceStartup+150;
        while(State(ctl).activeScript==script.id)
        {
            if(EditorApplication.timeSinceStartup>=end)throw new InvalidOperationException("Intro did not finish: "+script.id);
            var p=ctl.Sim.IntroProgress(script.id);var step=p.step<script.steps.Count?script.steps[p.step]:null;
            if(step?.type=="self")
            {
                foreach(var f in Until(()=>Get<RectTransform>(host,"thought")!=null,"real private thought"))yield return f;
                if(!held){foreach(var f in PauseWindow(ctl,capture,tag))yield return f;held=true;foreach(var f in Until(()=>Get<RectTransform>(host,"thought")!=null,"same private thought resumes"))yield return f;}
                capture(tag+"-thought-"+p.step+".png");
                foreach(var f in Tap(Get<XgBtn>(host,"thoughtNext").button,click))yield return f;
            }
            else if(step?.type=="choice"&&p.choice<0)
            {
                foreach(var f in OpenGroup(ctl))yield return f;
                foreach(var f in Until(()=>Get<RectTransform>(YYChatHub.Instance.View,"introInput")?.gameObject.activeInHierarchy==true,"actual replacement choices"))yield return f;
                var input=Get<RectTransform>(YYChatHub.Instance.View,"introInput");
                string choice=GameText.T(step.options[variant%step.options.Count].text,step.options[variant%step.options.Count].textEn);
                var button=LabelButton(input,choice);capture(tag+"-choice-"+selected+".png");
                if(script.id=="taohuo_intro"&&!faultDone)
                {
                    int lines=Group.messages.Count,calls=0;Action fault=()=>{calls++;throw new IOException("App acquisition QA injected save failure");};
                    ctl.runtime.Saving+=fault;try{foreach(var f in Tap(button,click))yield return f;}finally{ctl.runtime.Saving-=fault;}
                    Need(calls>0&&Group.messages.Count==lines&&ctl.Sim.IntroProgress(script.id).choice<0,"failed real choice save leaves no player line or selected option");
                    capture(tag+"-choice-save-failure.png");foreach(var f in Wait(1.1))yield return f;faultDone=true;
                    input=Get<RectTransform>(YYChatHub.Instance.View,"introInput");button=LabelButton(input,choice);
                }
                int before=Group.messages.Count;foreach(var f in Tap(button,click))yield return f;
                Need(Group.messages.Count==before+1&&Group.messages.Last().from==YYChatHub.Me&&Group.messages.Last().text==choice,"actual chosen text sent once as player");selected++;
            }
            else yield return null;
        }
        var done=ctl.Sim.IntroProgress(script.id);Need(done.completed&&!done.skipped&&done.todoOffered,"real script completes with todo: "+script.id);
        Need(selected==script.steps.Count(s=>s.type=="choice"),"every choice round clicked: "+script.id);
        Done(tag+": all authored steps played through real UI; selected branch variant "+variant+" only, no other-branch claim.");
    }
    static RectTransform Icon(XingGuangController ctl,string app)
    {
        string route=XgSim.DesktopRoute(app);var binding=DesktopWindowIndex.GetBindings(ctl.runtime).FirstOrDefault(x=>x.ApplicationId==route);
        if(binding?.DesktopShortcut!=null)return binding.DesktopShortcut.transform as RectTransform;
        string native=app=="zhongbao"?"Calculator":app=="miaoyu"?"Commander":"";
        return GameObject.Find("Desktop List")?.transform.Find(native) as RectTransform;
    }
    static WindowManager AppWindow(XingGuangController ctl,string app)
    {
        var binding=DesktopWindowIndex.GetBindings(ctl.runtime).FirstOrDefault(x=>x.ApplicationId==XgSim.DesktopRoute(app));
        if(binding?.Window!=null)return binding.Window;
        return DesktopWindowIndex.Get(ctl.runtime).FirstOrDefault(w=>w.name==(app=="miaoyu"?"Commander":app=="zhongbao"?"Calculator":app=="gamecenter"?"Game Hub":""));
    }
    static IEnumerable<object> Link(XingGuangController ctl,string app,Action<RectTransform> click)
    {
        foreach(var f in OpenGroup(ctl))yield return f;
        var message=Group.messages.LastOrDefault(m=>m.kind==YYKind.AppLink&&m.introApp==app);
        Need(message!=null,"actual persisted link message exists for "+app);
        var button=LabelButton(YYChatHub.Instance.View,GameText.T(message.introLabelZh,message.introLabelEn));
        // Explicit scroll geometry setup; the acquisition action remains an LCD pointer click.
        var scroll=Get<ScrollRect>(YYChatHub.Instance.View,"scroll");var target=(RectTransform)button.transform;
        float range=scroll.content.rect.height-scroll.viewport.rect.height;
        if(range>0){float y=scroll.viewport.InverseTransformPoint(target.TransformPoint(target.rect.center)).y;scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-(scroll.viewport.rect.center.y-y)/range);}
        foreach(var f in Settle())yield return f;
        foreach(var f in Tap(button,click))yield return f;
    }
    static IEnumerable<object> Install(XingGuangController ctl,string app,bool english,Action<RectTransform> click,Action<string> capture,bool exerciseRollback)
    {
        string tag="app-"+(english?"en-":"zh-")+app;var host=Host(ctl);
        Need(!ctl.Sim.DesktopAppInstalled(app),"no fixture-installed downloadable app: "+app);
        var icon=Icon(ctl,app);Need(icon!=null&&!icon.gameObject.activeInHierarchy,"desktop icon absent before real installation: "+app);
        foreach(var f in Link(ctl,app,click))yield return f;
        var browser=Get<WebBrowserApp>(host,"browser");var window=Get<WindowManager>(host,"browserWindow");
        Need(window!=null&&window.isOn&&Get<RectTransform>(host,"downloadPage")?.gameObject.activeInHierarchy==true,"link opens actual native browser download page");
        foreach(var f in Ready(window))yield return f;
        Need(browser?.networkManager?.isConnected==true,"actual network fixture connected before download");
        bool previousNetwork=browser.networkManager.isConnected;
        try
        {
        foreach(var f in Tap(Get<XgBtn>(host,"downloadAction").button,click))yield return f;
        foreach(var f in Until(()=>ctl.Sim.AppReceipt(app).receivedMB>0,"real download receives bytes"))yield return f;
        var progress=Get<RectTransform>(host,"downloadProgress");Need(progress!=null&&progress.anchorMax.x>0,"actual browser progress bar reflects received bytes");
        if(exerciseRollback)
        {
            browser.networkManager.isConnected=false;double received=ctl.Sim.AppReceipt(app).receivedMB;
            Need(!ctl.Sim.AppReceipt(app).downloaded,"interrupt before full download, not a completed-byte fixture");
            foreach(var f in Wait(.4))yield return f;
            Need(ctl.Sim.AppReceipt(app).receivedMB==received,"actual disconnected network signal pauses bytes");
            foreach(var f in PauseWindow(ctl,capture,tag+"-partial"))yield return f;
            Need(ctl.Sim.AppReceipt(app).receivedMB==received,"disconnected partial bytes remain exact after external hold");
            Need(ctl.runtime.SaveSlot(3),"real partial download saved through archive");
            string slotPath=Path.Combine(ChapterOneRuntime.VerificationSaveDirectory,"slots","3","chapter-one.sav");
            Need(AtomicSaveStore.TryReadFile(slotPath,out var payload),"partial archive primary passes checksum without backup recovery");
            var diskHouse=JsonUtility.FromJson<GameState>(payload);
            var disk=JsonUtility.FromJson<XgState>(diskHouse.labState).appAcquisition.apps.Single(r=>r.id==app);
            string serializedEvidence="memory="+received.ToString("R",System.Globalization.CultureInfo.InvariantCulture)
                +", disk="+disk.receivedMB.ToString("R",System.Globalization.CultureInfo.InvariantCulture)
                +", deltaMB="+(disk.receivedMB-received).ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            Done(tag+": partial archive numeric evidence: "+serializedEvidence);
            // Only JSON double rounding is allowed; at this app's <10 MB size four ULPs are <1e-8 bytes.
            double ulp=BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(received)+1)-received;
            Need(received>0&&disk.receivedMB>0&&!double.IsInfinity(received)&&!double.IsInfinity(disk.receivedMB)
                &&Math.Abs(disk.receivedMB-received)<=4*ulp,"partial archive preserves bytes within four double ULPs: "+serializedEvidence);
            Need(disk.offered&&disk.downloading&&!disk.downloaded&&!disk.installed&&!disk.legacyGranted
                &&disk.installStep==0&&disk.installedAt==0,"disk partial receipt retains offered download and no installation entitlement");
            Need(ctl.runtime.LoadArchive(3),"real partial download reloaded through archive");
            foreach(var f in Until(()=>Host(ctl).Current,"host rebound after real archive reload"))yield return f;host=Host(ctl);
            var restored=ctl.Sim.AppReceipt(app);
            string restoredEvidence=serializedEvidence+", restored="+restored.receivedMB.ToString("R",System.Globalization.CultureInfo.InvariantCulture)
                +", reloadDeltaMB="+(restored.receivedMB-disk.receivedMB).ToString("R",System.Globalization.CultureInfo.InvariantCulture)
                +", online="+(Get<WebBrowserApp>(host,"browser")?.networkManager?.isConnected==true);
            Done(tag+": partial reload numeric evidence: "+restoredEvidence);
            Need(restored.receivedMB==disk.receivedMB&&restored.offered&&restored.downloading&&!restored.downloaded
                &&!restored.installed&&!restored.legacyGranted&&restored.installStep==0&&restored.installedAt==0,
                "partial bytes survive disk reload exactly as persisted without install: "+restoredEvidence);
            foreach(var f in Link(ctl,app,click))yield return f;
            foreach(var f in Ready(Get<WindowManager>(host,"browserWindow")))yield return f;
            browser=Get<WebBrowserApp>(host,"browser");browser.networkManager.isConnected=true;
        }
        foreach(var f in Until(()=>ctl.Sim.AppReceipt(app).downloaded,"natural 2.5 MB/s download completes",15))yield return f;
        foreach(var f in Settle())yield return f;capture(tag+"-downloaded.png");
        for(int step=0;step<3;step++)
        {
            var receipt=ctl.Sim.AppReceipt(app);Need(receipt.installStep==step&&!receipt.installed,"installation requires next distinct action "+step);
            string label=english?(step==0?"Next":step==1?"Install":"Finish"):(step==0?"下一步":step==1?"安装":"完成");
            var action=Get<XgBtn>(host,"downloadAction");Need(action.label.text==label,"actual wizard label: "+label);
            if(exerciseRollback&&step==2)
            {
                int calls=0;Action fault=()=>{calls++;throw new IOException("App acquisition QA finish save failure");};ctl.runtime.Saving+=fault;
                try{foreach(var f in Tap(action.button,click))yield return f;}finally{ctl.runtime.Saving-=fault;}
                Need(calls>0&&!ctl.Sim.AppReceipt(app).installed&&ctl.Sim.AppReceipt(app).installStep==2,"failed Finish save rolls back install and desktop entitlement");
                capture(tag+"-finish-save-failure.png");foreach(var f in Wait(1.1))yield return f;
            }
            foreach(var f in Tap(Get<XgBtn>(host,"downloadAction").button,click))yield return f;
            Need(ctl.Sim.AppReceipt(app).installStep==step+1,"real wizard click advances once");
        }
        Need(ctl.Sim.DesktopAppInstalled(app)&&!ctl.Sim.AppReceipt(app).legacyGranted,"new installation completed without legacy grant");
        foreach(var f in Until(()=>Icon(ctl,app)?.gameObject.activeInHierarchy==true,"installed desktop icon appears",4))yield return f;
        var todos=new List<XgGuideStep>();host.AddTodos(todos);Need(!todos.Any(t=>t.arg==app+"_intro"),"installation todo completes only after real Finish");
        foreach(var f in Tap(Get<XgBtn>(host,"downloadAction").button,click))yield return f;
        Need(Get<RectTransform>(host,"downloadPage")==null&&AppWindow(ctl,app)?.isOn==true,"actual Open action opens installed app window");
        foreach(var native in DesktopWindowIndex.Get(ctl.runtime))if(native.isOn)native.CloseWindow();
        foreach(var f in Wait(.5))yield return f;
        foreach(var f in Settle())yield return f;capture(tag+"-installed-icon.png");
        var installedIcon=Icon(ctl,app);
        var nativeIcon=installedIcon.GetComponent<ButtonManager>();
        bool nativeDouble=nativeIcon!=null&&nativeIcon.checkForDoubleClick
            && Enumerable.Range(0,nativeIcon.onDoubleClick.GetPersistentEventCount()).Any(i=>
                nativeIcon.onDoubleClick.GetPersistentTarget(i)==AppWindow(ctl,app)
                && nativeIcon.onDoubleClick.GetPersistentMethodName(i)==nameof(WindowManager.OpenWindow));
        click(installedIcon);
        if(nativeDouble)click(installedIcon); // The native desktop shortcut opens on its actual double-click binding.
        foreach(var f in Settle())yield return f;
        Need(AppWindow(ctl,app)?.isOn==true,"real newly visible desktop icon opens installed app");
        Done(tag+": actual link, browser, naturally received bytes, Next/Install/Finish, saved installed receipt and visible desktop icon.");
        }
        finally{if(browser?.networkManager!=null)browser.networkManager.isConnected=previousNetwork;}
    }
    static IEnumerable<object> AskForSamples(XingGuangController ctl,bool english,Action<RectTransform> click,Action<string> capture)
    {
        var host=Host(ctl);int initial=Group.messages.Count(m=>m.from==YYChatHub.Me);
        foreach(var f in Until(()=>Get<RectTransform>(host,"thought")!=null,"private shortage prompt after Taohuo installation"))yield return f;
        Need(!State(ctl).sampleHelpRequested&&!ctl.Sim.IntroProgress("datapack_intro").completed,"private shortage has not sent public dialogue");
        foreach(var f in Tap(LabelButton(Get<RectTransform>(host,"thought"),english?"Ask later":"稍后再问"),click))yield return f;
        Need(Group.messages.Count(m=>m.from==YYChatHub.Me)==initial&&!State(ctl).sampleHelpRequested,"Ask later sends no player line");
        Need(ctl.runtime.SaveSlot(3)&&ctl.runtime.LoadArchive(3),"actual reload of unanswered optional prompt");
        foreach(var f in Until(()=>Host(ctl).Current&&Get<RectTransform>(Host(ctl),"thought")!=null,"unanswered prompt available on new loaded owner"))yield return f;
        host=Host(ctl);double end=EditorApplication.timeSinceStartup+5;
        while(!State(ctl).sampleHelpRequested)
        {Need(EditorApplication.timeSinceStartup<end,"public ask click completes");foreach(var f in Tap(Get<XgBtn>(host,"thoughtNext").button,click))yield return f;}
        Need(Group.messages.Any(m=>m.from==YYChatHub.Me&&m.text==(english?"Does anyone know where to find labelled pictures?":"有人知道哪里能找到标好的图片吗？")),"actual explicit neutral public sample question");
        capture("app-"+(english?"en":"zh")+"-sample-request.png");
    }
    static IEnumerable<object> StaleSave(XingGuangController ctl,bool english,Action<RectTransform> click)
    {
        // A separate adversarial callback test, not counted as a real acquisition action.
        foreach(var f in OpenGroup(ctl))yield return f;
        var msg=Group.messages.Last(m=>m.kind==YYKind.AppLink&&m.introApp=="taohuo");
        var stale=LabelButton(YYChatHub.Instance.View,GameText.T(msg.introLabelZh,msg.introLabelEn)).onClick;
        Need(ctl.runtime.SaveSlot(3),"completed genuine acquisition checkpoint before save switch");
        Bind(ctl,Fixture(english));int revision=State(ctl).revision;stale.Invoke();
        Need(State(ctl).revision==revision&&!ctl.Sim.AppReceipt("taohuo").offered&&!ctl.Sim.DesktopAppInstalled("taohuo"),"retained old link callback cannot modify replacement save");
        Need(ctl.runtime.LoadArchive(3),"return to genuine completed acquisition save");
        foreach(var f in Until(()=>Host(ctl).Current,"restored completed save rebound"))yield return f;
    }
    static IEnumerable<object> Skip(XingGuangController ctl,XgGroupIntroScript script,bool english,bool delayFixture,Action<RectTransform> click,Action<string> capture)
    {
        Need(ctl.runtime.SaveSlot(3),"genuinely completed save checkpoint before separate skip branch");
        Bind(ctl,Fixture(english));ElapsedFixture(ctl,script,delayFixture);
        foreach(var f in Until(()=>State(ctl).activeScript==script.id,"independent skip branch starts",script.trigger.delaySec+25))yield return f;
        foreach(var f in OpenGroup(ctl))yield return f;
        foreach(var f in Until(()=>Get<RectTransform>(YYChatHub.Instance.View,"introInput")?.gameObject.activeInHierarchy==true,"actual Skip control"))yield return f;
        int before=Group.messages.Count(m=>m.from==YYChatHub.Me);
        foreach(var f in Tap(LabelButton(Get<RectTransform>(YYChatHub.Instance.View,"introInput"),english?"Skip":"跳过"),click))yield return f;
        var p=ctl.Sim.IntroProgress(script.id);
        Need(p.completed&&p.skipped&&p.todoOffered&&ctl.Sim.AppReceipt("taohuo").offered,"actual Skip delivers link and todo with explicit skipped flag");
        Need(!ctl.Sim.DesktopAppInstalled("taohuo")&&Group.messages.Count(m=>m.from==YYChatHub.Me)==before,"Skip cannot fake installation or a chosen player line");
        capture("app-"+(english?"en":"zh")+"-actual-skip.png");
        Need(ctl.runtime.LoadArchive(3),"restore genuine six-intro completed save after separate skip branch");
        foreach(var f in Until(()=>Host(ctl).Current,"completed owner restored after skip test"))yield return f;
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> actualLCDclick,Action<string> capture,bool elapsedDelayFixtures=true,int branchVariant=0)
    {
        Checks.Clear();AssertionCount=0;
        Need(ctl?.runtime!=null&&ctl.Sim!=null&&YYChatHub.Instance?.View!=null,"real Main controller/YY/view initialized");
        Need(!ctl.runtime.TestMode&&!ctl.runtime.tickEnabled,"root disabled household ticks while retaining real host");
        Need(branchVariant>=0,"nonnegative selected branch variant");
        var original=ctl.runtime.Sim;bool disk=ctl.runtime.useDiskSave,englishBefore=GameText.IsEnglish,controllerEnabled=ctl.enabled;var host=Host(ctl);Need(host!=null,"production acquisition host installed");bool hostEnabled=host.enabled;
        ctl.enabled=false;
        try
        {
            for(int languageIndex=0;languageIndex<2;languageIndex++)
            {
                bool english=languageIndex==1;language(english);
                foreach(var window in DesktopWindowIndex.Get(ctl.runtime))if(window.isOn)window.CloseWindow();
                Bind(ctl,Fixture(english));ctl.Sim.English=english;
                foreach(var f in Until(()=>host.Current&&!host.Held,"optional UI host current and unheld"))yield return f;
                Need(State(ctl).apps.Count(a=>a.installed)==1&&ctl.Sim.DesktopAppInstalled("gamecenter"),"fresh fixture preinstalls Game Hub only");
                var catalog=Resources.Load<TextAsset>("LingGuangV05/Data/group_intro_scripts");Need(catalog!=null,"runtime six-script catalog loaded");
                var scripts=JsonUtility.FromJson<XgGroupIntroCatalog>(catalog.text);Need(scripts.scripts.Count==6,"all six authored introductions present");
                foreach(var f in Script(ctl,scripts.scripts.Single(s=>s.id=="taohuo_intro"),english,branchVariant,elapsedDelayFixtures,actualLCDclick,capture))yield return f;
                foreach(var f in Install(ctl,"taohuo",english,actualLCDclick,capture,true))yield return f;
                foreach(var f in AskForSamples(ctl,english,actualLCDclick,capture))yield return f;
                foreach(var f in Script(ctl,scripts.scripts.Single(s=>s.id=="datapack_intro"),english,branchVariant,elapsedDelayFixtures,actualLCDclick,capture))yield return f;
                Stage(ctl,3);
                foreach(var f in Script(ctl,scripts.scripts.Single(s=>s.id=="zhongbao_intro"),english,branchVariant,elapsedDelayFixtures,actualLCDclick,capture))yield return f;
                foreach(var f in Install(ctl,"zhongbao",english,actualLCDclick,capture,false))yield return f;
                foreach(var f in Script(ctl,scripts.scripts.Single(s=>s.id=="miaoyu_intro"),english,branchVariant,elapsedDelayFixtures,actualLCDclick,capture))yield return f;
                foreach(var f in Install(ctl,"miaoyu",english,actualLCDclick,capture,false))yield return f;
                Stage(ctl,4);
                foreach(var f in Script(ctl,scripts.scripts.Single(s=>s.id=="enterprise_intro"),english,branchVariant,elapsedDelayFixtures,actualLCDclick,capture))yield return f;
                foreach(var f in Script(ctl,scripts.scripts.Single(s=>s.id=="gamecenter_intro"),english,branchVariant,elapsedDelayFixtures,actualLCDclick,capture))yield return f;
                foreach(var f in Link(ctl,"gamecenter",actualLCDclick))yield return f;
                Need(State(ctl).gamesOpened,"actual preinstalled Game Hub link opened native window");
                foreach(var f in StaleSave(ctl,english,actualLCDclick))yield return f;
                foreach(var f in Skip(ctl,scripts.scripts.Single(s=>s.id=="taohuo_intro"),english,elapsedDelayFixtures,actualLCDclick,capture))yield return f;
                foreach(var script in scripts.scripts)Need(ctl.Sim.IntroProgress(script.id).completed&&!ctl.Sim.IntroProgress(script.id).skipped,"saved real completion survives owner switch: "+script.id);
                Need(Group.messages.Count(m=>m.kind==YYKind.AppLink)==4,"saved script reload has exactly four actual software cards, no replayed links");
                Done((english?"EN":"ZH")+": six real introductions; three acquired applications; Game Hub preinstalled/opened. Data-file import and contract signing are deliberately NOT claimed by this check.");
            }
        }
        finally
        {
            host.enabled=false;ctl.runtime.useDiskSave=false;ctl.runtime.SetSimulationForTests(original);
            typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});
            language(englishBefore);host.enabled=hostEnabled;ctl.enabled=controllerEnabled;ctl.runtime.useDiskSave=disk;
        }
    }
}
#endif
