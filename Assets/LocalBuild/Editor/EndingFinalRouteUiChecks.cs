#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.CaseMenu;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Composable checks for the shared isolated Main/LCD runner. No scene boot or runner edits here.
// Preconditions must be earned by the existing actual-UI suite or loaded from its real disk checkpoints.
// Only the unrelated save used during the night switch is an explicit in-memory fixture.
public static partial class EndingFinalRouteUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount {get;private set;}
    public static bool TitleReturnStarted {get;private set;}
    public static string CompletionReceipt {get;private set;}="";
    public static void ResetReport(){Checks.Clear();AssertionCount=0;TitleReturnStarted=false;CompletionReceipt="";}
    static T Get<T>(object owner,string field)=>(T)owner.GetType().GetField(field,F).GetValue(owner);
    static void Need(bool ok,string message){AssertionCount++;if(!ok)throw new InvalidOperationException("Ending final UI: "+message);}
    static void Done(string message){Checks.Add(message);Debug.Log("Ending final UI: "+message);}
    static IEnumerable<object> Wait(double seconds)
    {double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
    static IEnumerable<object> Settle(int frames=3)
    {Canvas.ForceUpdateCanvases();while(frames-->0)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerable<object> Until(Func<bool> ready,string label,double seconds=10)
    {
        double end=EditorApplication.timeSinceStartup+seconds;
        while(!ready()){if(EditorApplication.timeSinceStartup>=end)Need(false,label+" timed out");yield return null;}
        foreach(var f in Settle())yield return f;
    }
    static IEnumerable<object> NativeReady(WindowManager window)
    {
        foreach(var f in Until(()=>
        {
            if(window==null||!window.isOn||!window.gameObject.activeInHierarchy)return false;
            foreach(var group in window.GetComponentsInParent<CanvasGroup>(true))if(group.alpha<.95f||!group.interactable||!group.blocksRaycasts)return false;
            var animator=window.windowAnimator;return animator==null||!animator.isActiveAndEnabled||!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"native mailbox naturally interactive"))yield return f;
    }
    static Button Named(Component root,string name)
    {
        var matches=root.GetComponentsInChildren<Button>(true).Where(b=>b.name==name&&b.gameObject.activeInHierarchy).ToArray();
        Need(matches.Length==1,"one visible native button "+name);Need(matches[0].IsInteractable(),"native button is interactable: "+name);return matches[0];
    }
    static IEnumerable<object> Tap(Component root,string name,Action<RectTransform> click)
    {var button=Named(root,name);foreach(var f in Settle())yield return f;click((RectTransform)button.transform);foreach(var f in Settle())yield return f;}
    static IEnumerable<object> TapEpilogue(XingGuangController ctl,string field,Action<RectTransform> click)
    {
        var view=Get<XgEpilogueView>(ctl.Epilogue,"view");Need(view!=null&&view.Showing,"real epilogue view is visible");
        var button=Get<XgBtn>(view,field);Need(button.rt.gameObject.activeInHierarchy&&button.button.IsInteractable(),"epilogue button live: "+field);
        // A newly constructed view can be active before its Graphic.depth is assigned by the canvas.
        // Wait for the actual LCD ray to reach it; keep Click's strict production hit validation.
        foreach(var f in LcdActualUiProbe.WaitForClick(button.rt,"epilogue "+field))yield return f;
        click(button.rt);foreach(var f in Settle())yield return f;
    }
    static XgEndingPreparationState Prep(XingGuangController ctl)=>ctl.Sim.S.endingPreparation;
    static XgLaunchNightState Night(XingGuangController ctl)=>Prep(ctl)?.night;
    static bool SameResponse(string saved,XgLaunchNightResponse response)
    {
        var before=JsonUtility.FromJson<XgLaunchNightResponse>(saved);var after=response.Copy();
        if(!(Math.Abs(before.refusalRoll-after.refusalRoll)<=1e-12))return false;
        before.refusalRoll=after.refusalRoll=0;
        return JsonUtility.ToJson(before)==JsonUtility.ToJson(after);
    }
    static bool SameNight(string saved,XgLaunchNightState current)
    {
        var before=JsonUtility.FromJson<XgLaunchNightState>(saved);var after=current.Copy();
        if(!(Math.Abs(before.elapsedSeconds-after.elapsedSeconds)<=1e-9)||!(Math.Abs(before.currentSeconds-after.currentSeconds)<=1e-9)||
            !(Math.Abs(before.powerWh-after.powerWh)<=1e-12)||!(Math.Abs(before.temperatureC-after.temperatureC)<=1e-10)||before.responses.Count!=after.responses.Count)return false;
        for(int i=0;i<before.responses.Count;i++)
        {if(!SameResponse(JsonUtility.ToJson(before.responses[i]),after.responses[i]))return false;before.responses[i].refusalRoll=after.responses[i].refusalRoll=0;}
        before.elapsedSeconds=after.elapsedSeconds=before.currentSeconds=after.currentSeconds=0;
        before.powerWh=after.powerWh=before.temperatureC=after.temperatureC=0;
        return JsonUtility.ToJson(before)==JsonUtility.ToJson(after);
    }
    static string VisibleText(Component root)=>string.Join("\n",root.GetComponentsInChildren<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy).Select(t=>t.text));
    static string IsolatedDirectory(XingGuangController ctl)
    {
        Need(ctl!=null&&ctl.runtime!=null&&ctl.Sim!=null,"current real controller exists");var rt=ctl.runtime;
        Need(!rt.TestMode&&!rt.tickEnabled&&!ctl.enabled,"shared runner disabled unrelated economy/lab ticks without test-host substitutions");
        Need(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready,"model service is disabled; no inference claim");
        string root=ChapterOneRuntime.VerificationSaveDirectory;Need(!string.IsNullOrWhiteSpace(root),"explicit verification save directory supplied");root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        Need(root.IndexOf(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)>=0,"verification directory belongs to LocalVerification");
        string path=Path.GetFullPath(rt.SavePath);Need(path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"actual save path stays under isolated verification directory");
        rt.useDiskSave=true;return Path.GetDirectoryName(path);
    }
    static string ReadPayload(string directory)
    {var result=new AtomicSaveStore(directory).Load();Need(result.Success&&!result.Recovered,"primary disk snapshot loads without backup recovery");return result.Payload;}
    static XgState ReadLab(string directory)=>JsonUtility.FromJson<XgState>(JsonUtility.FromJson<GameState>(ReadPayload(directory)).labState);
    static IEnumerable<object> ShowHost(XingGuangController ctl,XgEndingPreparationHost host)
    {Need(host!=null&&host.enabled&&host.Show(PrologueDirector.Desk?.layer),"production preparation host opens on LCD");foreach(var f in Settle(4))yield return f;}

    // Call on a separately reloaded pre-route checkpoint. This production choice is deterministic, not a model reply.
    public static IEnumerator AiChoiceFrames(XingGuangController ctl,Action<RectTransform> click,Action<string> capture)
    {
        string directory=IsolatedDirectory(ctl);var host=ctl.GetComponent<XgEndingPreparationHost>();foreach(var f in ShowHost(ctl,host))yield return f;
        var d=Prep(ctl);Need(d!=null&&d.phase=="asked"&&d.route==""&&!string.IsNullOrEmpty(d.memoryReviewReceipt),"actual reviewed pre-route checkpoint required");
        int good=d.memories.Count(m=>m.lean>0),bad=d.memories.Count(m=>m.lean<0);double opinion=ctl.Sim.ActualAxis(2),play=ctl.Sim.ActualAxis(1);
        string expected=good>bad?(opinion>=50?"online":"local_only"):(play>=50?"public_model":"local_only");
        var panel=Get<EndingPreparationPanel>(host,"preparation");foreach(var f in Tap(panel,"Tab1",click))yield return f;
        foreach(var f in Tap(panel,"Route3",click))yield return f;d=Prep(ctl);
        Need(d.decisionOwner=="ai"&&d.route==expected&&d.aiDecision!=null&&d.aiDecision.route==expected,"actual Ask it button saves the declared deterministic route");
        Need(d.pinnedIds.Count==0&&d.pinReceipt==""&&!XgEpilogueFlow.HasCommittedRelease(ctl.Sim.S),"asking does not pin rules or publish automatically");
        Need(d.aiDecision.citedMemoryIds.Count==2&&d.aiDecision.citedMemoryIds.Distinct().Count()==2&&d.aiDecision.citedMemoryIds.All(id=>d.memories.Any(m=>m.id==id&&m.sources.Count>0)),"AI-labelled conclusion cites two actual source-backed cards");
        Need(ReadLab(directory).endingPreparation.aiDecision.id==d.aiDecision.id,"AI-labelled decision receipt persisted on disk");
        Need(!Get<XgBtn[]>(panel,"routes").Any(b=>b.button.IsInteractable()),"route buttons lock after Ask it");
        capture("ending-final-ask-it-"+(GameText.IsEnglish?"en":"zh")+".png");
        Done("Ask it uses the real deterministic core and actual LCD control, with source-backed citations and disk receipt. No external response or model inference was used.");
    }

    // Call immediately after actual online publication; the night must not have been started or cut short.
    // No explicit AdvanceNight call, frame-time injection or timeScale change occurs in this check.
    public static IEnumerator FullNightFrames(XingGuangController ctl,Action<RectTransform> click,Action<string> capture,int checkpointSlot=6,double timeoutSeconds=360)
    {
        string directory=IsolatedDirectory(ctl);Need(checkpointSlot>=2&&checkpointSlot<=6,"dedicated isolated checkpoint slot 2–6");
        Need(timeoutSeconds>=240&&timeoutSeconds<=600,"bounded natural-night timeout 240–600 seconds");
        var host=ctl.GetComponent<XgEndingPreparationHost>();foreach(var f in ShowHost(ctl,host))yield return f;
        Need(XgEpilogueFlow.HasCommittedRelease(ctl.Sim.S)&&ctl.Sim.S.releaseOutcome.route=="online"&&ctl.Sim.S.epilogue.phase=="launch_night"&&Night(ctl)==null,"actual unstarted online release required");
        var panel=Get<EndingLaunchNightPanel>(host,"night");double began=EditorApplication.timeSinceStartup;bool finished=false;
        try
        {
            foreach(var f in Tap(panel,"Start",click))yield return f;
            var n=Night(ctl);Need(n!=null&&n.phase=="running"&&n.requests.Count==40&&n.responses.Count==40&&!n.throttled,"actual Start builds exactly forty normal-speed requests");
            string run=n.runId,release=n.releaseReceiptId;var fixedResponses=n.responses.Select(r=>JsonUtility.ToJson(r)).ToArray();
            foreach(var f in Wait(1.2))yield return f;foreach(var f in Tap(panel,"Pause",click))yield return f;
            Need(Night(ctl).paused,"actual Pause persists");string paused=JsonUtility.ToJson(Night(ctl));
            foreach(var f in Wait(1.2))yield return f;Need(JsonUtility.ToJson(Night(ctl))==paused,"paused natural frames change neither elapsed time nor power/request counters");
            foreach(var f in Tap(panel,"Pause",click))yield return f;foreach(var f in Wait(.8))yield return f;
            Need(!Night(ctl).paused,"actual resume unpauses before save-switch test");
            host.Hide();Need(ctl.runtime.SaveSlot(checkpointSlot),"unpaused night checkpoint uses real archive save");
            var originalLab=ctl.Sim.S;string switchedSnapshot=JsonUtility.ToJson(originalLab.endingPreparation.night);var originalSave=ctl.runtime.Sim;
            // Explicit unrelated-save fixture, never written to disk. The real active night remains in its archive.
            var temporary=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(originalSave.S));var temporaryLab=JsonUtility.FromJson<XgState>(temporary.labState);
            temporaryLab.releaseOutcome=null;temporaryLab.epilogue=null;temporaryLab.endingPreparation=null;temporaryLab.chapterComplete=false;temporary.labState=JsonUtility.ToJson(temporaryLab);
            ctl.runtime.SetSimulationForTests(new ChapterOneSim(temporary));
            typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});ctl.runtime.tickEnabled=false;
            Need(!ReferenceEquals(ctl.runtime.Sim,originalSave)&&ctl.BoundTo(ctl.runtime.Sim),"controller switched to another actual save identity");
            foreach(var f in Wait(1.2))yield return f;
            Need(JsonUtility.ToJson(originalLab.endingPreparation.night)==switchedSnapshot,"detached unpaused night does not advance while another save is active");
            IsolatedDirectory(ctl);Need(ctl.runtime.LoadArchive(checkpointSlot),"real archive restores the original night without copying timer fields");
            foreach(var f in ShowHost(ctl,host))yield return f;panel=Get<EndingLaunchNightPanel>(host,"night");
            string restoredNight=JsonUtility.ToJson(Night(ctl));
            if(restoredNight!=switchedSnapshot)
            {File.WriteAllText(Path.Combine(directory,"night-before-reload.json"),switchedSnapshot);File.WriteAllText(Path.Combine(directory,"night-after-reload.json"),restoredNight);}
            Need(SameNight(switchedSnapshot,Night(ctl))&&Night(ctl).runId==run,"disk reload retains exact counters/receipts/answers and numeric values within serialization rounding; diagnostic JSON in isolated Save directory");
            Need(!Get<bool>(panel,"ticking"),"reloaded panel requires explicit resume");foreach(var f in Wait(1.2))yield return f;
            Need(SameNight(switchedSnapshot,Night(ctl)),"returning to night does not catch up time or resume itself");
            foreach(var f in Tap(panel,"Pause",click))yield return f;
            int last=Night(ctl).answered;double deadline=began+timeoutSeconds;
            while(Night(ctl).phase=="running")
            {
                if(EditorApplication.timeSinceStartup>=deadline)Need(false,"forty natural requests timed out at "+Night(ctl).answered);
                if(panel.Session.Failed)Need(false,"natural night has an unexpected persistence failure");
                n=Night(ctl);if(n.answered!=last){Need(n.answered==last+1,"requests complete one at a time under natural frames");last=n.answered;if(last%10==0)Done("Natural night: "+last+" / 40 requests, elapsed "+n.elapsedSeconds.ToString("F2")+" s.");}
                yield return null;
            }
            n=Night(ctl);Need(n.phase=="completed"&&n.answered==40&&n.currentIndex==40,"fortieth request reaches the real completed night state");
            Need(Math.Abs(n.elapsedSeconds-210)<1e-6&&EditorApplication.timeSinceStartup-began>=210,"normal night consumes 210 simulated seconds and at least 210 wall-clock seconds");
            Need(n.runId==run&&n.releaseReceiptId==release&&n.responses.Count==fixedResponses.Length&&n.responses.Select((r,i)=>SameResponse(fixedResponses[i],r)).All(x=>x),"pause and archive round-trip never reroll fixed responses");
            Need(!string.IsNullOrEmpty(n.terminalReceiptId)&&!ctl.Sim.S.chapterComplete&&ctl.Sim.S.releaseOutcome.launchNight==null,"terminal night receipt exists before epilogue transfer, without chapter completion");
            capture("ending-final-night-forty-complete.png");string terminal=n.terminalReceiptId;int failures=0;Action fail=()=>{failures++;throw new IOException("Ending final QA night receipt save fault");};
            ctl.runtime.Saving+=fail;
            try{foreach(var f in Tap(panel,"Continue",click))yield return f;}
            finally{ctl.runtime.Saving-=fail;}
            Need(failures>0&&ctl.Sim.S.epilogue.phase=="launch_night"&&ctl.Sim.S.releaseOutcome.launchNight==null&&!ctl.Sim.S.chapterComplete,"failed receipt transfer keeps completed-night scene and does not enter or finish epilogue");
            Need(Night(ctl).terminalReceiptId==terminal&&panel.Session.Failed,"failed Continue preserves the terminal night for retry");
            foreach(var f in Tap(panel,"Continue",click))yield return f;
            Need(ctl.Sim.S.epilogue.phase=="date"&&ctl.Sim.S.releaseOutcome.launchNight?.receiptId==terminal&&!ctl.Sim.S.chapterComplete,"actual retry transfers terminal receipt and enters date phase only");
            var saved=ReadLab(directory);Need(saved.endingPreparation.night.answered==40&&saved.releaseOutcome.launchNight?.receiptId==terminal,"all forty answers and transferred receipt verified in primary disk snapshot");finished=true;
            Done("Full natural 40-request night passed with pause, detached-save freeze, exact disk reload, explicit resume and real Continue save failure/retry. Economy ticks were intentionally disabled by the shared fixture; this does not certify economic simulation.");
        }
        finally{if(!finished&&panel!=null)panel.enabled=false;} // Freeze the failing visible night; never mark it complete to make QA exit.
    }

    static IEnumerable<object> OpenRealLetter(XingGuangController ctl,Action<RectTransform> click,Action<string> capture=null)
    {
        Need(XgEpilogueFlow.CanEnterEpilogue(ctl.Sim.S)&&!ctl.Sim.S.chapterComplete,"actual released unfinished epilogue required");
        var host=ctl.GetComponent<XgEndingPreparationHost>();if(host!=null)host.Hide();
        Need(ctl.Epilogue.Show(PrologueDirector.Desk?.layer),"real epilogue director opens");foreach(var f in Settle())yield return f;
        int steps=0;while(ctl.Sim.S.epilogue.phase!="letter"&&ctl.Sim.S.epilogue.phase!="reading")
        {
            Need(steps++<3,"date/headline/route path is bounded");
            foreach(var f in WaitEpilogueTransition(ctl))yield return f;
            if(EndingPresentationActualUiChecks.Requested)
            {
                if(ctl.Sim.S.epilogue.phase=="route")Need(Get<int>(EpView(ctl),"cardCount")==3,"actual route presents three source-gated excerpt cards");
                capture?.Invoke("ending-presentation-"+ctl.Sim.S.epilogue.phase+"-"+(GameText.IsEnglish?"en":"zh")+".png");
            }
            foreach(var f in TapEpilogue(ctl,"next",click))yield return f;
        }
        foreach(var f in TapEpilogue(ctl,"inbox",click))yield return f;
        var router=ctl.runtime.GetComponent<ChapterOneDesktopRouter>();Need(router!=null,"native app router exists");
        foreach(var f in Until(()=>router.Get("mail")?.Content.GetComponent<MailInboxView>()?.EndingLetter?.Visible==true,"real mailbox letter visible"))yield return f;
        foreach(var f in NativeReady(router.Get("mail").Window))yield return f;
        var p=router.Get("mail").Content.GetComponent<MailInboxView>().EndingLetter;
        Need(ReferenceEquals(p.Snapshot.owner,ctl.runtime.Sim)&&!p.Snapshot.readOnly,"mail bridge is bound to exact current unfinished save");
        if(!p.Snapshot.saved)foreach(var f in Tap(p,"SaveArticle",click))yield return f;
        if(p.Session.View!=EndingLetterView.Reading)foreach(var f in Tap(p,"OpenLetter",click))yield return f;
        if(p.Session.View==EndingLetterView.Consent)foreach(var f in Tap(p,"ReadFirst",click))yield return f;
        foreach(var f in Until(()=>p.Session.View==EndingLetterView.Reading,"actual letter reading state"))yield return f;
        Need(p.Snapshot.saved&&p.Snapshot.opened&&!ctl.Sim.S.chapterComplete,"native save and consent do not complete chapter");
    }
    static EndingLetterPresenter Presenter(XingGuangController ctl)=>ctl.runtime.GetComponent<ChapterOneDesktopRouter>()?.Get("mail")?.Content.GetComponent<MailInboxView>()?.EndingLetter;

    // Leaves the real letter open and retryable. Call CompleteReturnFrames next only when the runner is ready to lose Main.
    public static IEnumerator LetterFailureFrames(XingGuangController ctl,Action<RectTransform> click,Action<string> capture)
    {
        string directory=IsolatedDirectory(ctl);
        if(EndingPresentationActualUiChecks.Requested)foreach(var f in PresentationBeforeLetter(ctl,click,capture))yield return f;
        foreach(var f in OpenRealLetter(ctl,click,capture))yield return f;
        var p=Presenter(ctl);foreach(var f in Wait(.2))yield return f;var scroll=p.Root.GetComponentInChildren<ScrollRect>();
        var finishBefore=p.Root.GetComponentsInChildren<Button>().Single(b=>b.name=="CompleteEnding");
        Need(!p.ReadToEnd&&!finishBefore.IsInteractable(),"real long letter finish disabled before reading reaches end");
        click((RectTransform)finishBefore.transform);foreach(var f in Settle())yield return f;
        Need(!ctl.Sim.S.chapterComplete&&string.IsNullOrEmpty(ctl.Sim.S.epilogue.completionReceipt),"disabled real finish cannot complete or create receipt");
        var wheel=EndingLetterUiChecks.ScrollToEnd(p);while(wheel.MoveNext())yield return wheel.Current;
        Need(!ctl.Sim.S.chapterComplete&&string.IsNullOrEmpty(ctl.Sim.S.epilogue.completionReceipt),"actual LCD wheel to bottom does not complete or create receipt");
        capture("ending-final-read-to-end-"+(GameText.IsEnglish?"en":"zh")+".png");
        Need(p.Session.Current&&p.Session.View==EndingLetterView.Reading,"actual reading position settled before completion fault");
        string before=ReadPayload(directory),epBefore=JsonUtility.ToJson(ctl.Sim.S.epilogue);int failures=0,completionEvents=0;
        var menu=UnityEngine.Object.FindAnyObjectByType<CaseMenuController>();int activeScene=SceneManager.GetActiveScene().handle;
        Need(menu==null||menu.HasEnteredGame&&!menu.IsReturningToTitle,"existing title shell, if present, is in gameplay before finish");
        Action fault=()=>{failures++;throw new IOException("Ending final QA completion snapshot fault");};Action<string> completed=r=>completionEvents++;
        ctl.runtime.Saving+=fault;ctl.Epilogue.CompletionSaved+=completed;
        try{foreach(var f in Tap(p,"CompleteEnding",click))yield return f;foreach(var f in Wait(.5))yield return f;}
        finally{ctl.runtime.Saving-=fault;ctl.Epilogue.CompletionSaved-=completed;}
        Need(failures>0&&completionEvents==0&&!ctl.Sim.S.chapterComplete&&JsonUtility.ToJson(ctl.Sim.S.epilogue)==epBefore,"failed final SaveNow rolls back completion receipt/phase exactly and emits no completion event");
        Need(ReadPayload(directory)==before,"failed completion leaves the exact primary disk snapshot unchanged");
        Need(p.Visible&&p.Session.View==EndingLetterView.Reading&&p.Session.ErrorCopyId=="UI-FAIL"&&Named(p,"CompleteEnding").IsInteractable(),"failed native finish leaves the same reading page with a visible retryable error");
        Need(SceneManager.GetActiveScene().handle==activeScene&&UnityEngine.Object.FindAnyObjectByType<CaseMenuController>()==menu&&(menu==null||!menu.IsReturningToTitle&&menu.HasEnteredGame),"failed completion leaves the same scene and does not return to title");
        capture("ending-final-completion-save-failed-"+(GameText.IsEnglish?"en":"zh")+".png");
        Done("Native CompleteEnding save fault left the reading page, reading position, incomplete phase and exact disk snapshot intact; no final route or completion event fired.");
    }

    // Terminal operation: actual successful button -> real scene return. Do not restore old controller/save in a runner finally.
    // captureTitle must capture the whole physical title scene, not a cached Main/LCD render texture.
    public static IEnumerator CompleteReturnFrames(XingGuangController ctl,Action<RectTransform> click,Action<string> captureTitle,Action<string> captureClosure=null)
    {
        string directory=IsolatedDirectory(ctl);var p=Presenter(ctl);var menu=UnityEngine.Object.FindAnyObjectByType<CaseMenuController>();
        Need(p!=null&&p.Visible&&p.Session.View==EndingLetterView.Reading&&!ctl.Sim.S.chapterComplete,"current real unfinished reading page required");
        Need(p.ReadToEnd,"real reading geometry gate must be earned before final successful click");
        Need(menu==null||menu.HasEnteredGame&&!menu.IsTransitioning,"existing physical shell can return, or production Main fallback is used");
        Need(captureTitle!=null,"whole-title capture callback required");int oldMenuId=menu==null?0:menu.GetInstanceID(),events=0;string accepted="";bool savedBeforeReturn=false,hadShell=menu!=null;
        var director=ctl.Epilogue;Action<string> saved=receipt=>
        {events++;accepted=receipt;var disk=ReadLab(directory);savedBeforeReturn=disk.chapterComplete&&disk.epilogue?.completionReceipt==receipt;};
        director.CompletionSaved+=saved;
        try
        {
            if(EndingPresentationActualUiChecks.Requested)ctl.Sim.S.reduceFx=false; // Explicit preference fixture for natural successful-save fade.
            var finish=Named(p,"CompleteEnding");click((RectTransform)finish.transform);
            Need(events==1&&!string.IsNullOrEmpty(accepted)&&savedBeforeReturn,"completion event fires once only after matching chapter completion is durably readable");
            Need(p.Session.View==EndingLetterView.Complete&&p.Snapshot.readOnly,"presenter accepts completion reply before bridge can route on next frame");
            CompletionReceipt=accepted;TitleReturnStarted=true;
            if(EndingPresentationActualUiChecks.Requested)foreach(var f in PresentationAfterSaved(ctl,captureClosure))yield return f;
            foreach(var f in Until(()=>ctl==null||menu!=null&&menu.IsReturningToTitle||UnityEngine.Object.FindAnyObjectByType<CaseMenuController>()!=null&&UnityEngine.Object.FindAnyObjectByType<CaseMenuController>().GetInstanceID()!=oldMenuId,"production bridge starts native return",10))yield return f;
            foreach(var f in Until(()=>
            {
                var current=UnityEngine.Object.FindAnyObjectByType<CaseMenuController>();
                return current!=null&&current.GetInstanceID()!=oldMenuId&&!current.HasEnteredGame&&!current.IsTransitioning&&current.IsMainReady;
            },"new title scene landed and preloaded Main",60))yield return f;
            var title=UnityEngine.Object.FindAnyObjectByType<CaseMenuController>();Need(title.menuCamera!=null&&title.menuCamera.enabled&&title.startButton.gameObject.activeInHierarchy,"new title camera and power button are actually active");
            var diskAfter=ReadLab(directory);Need(diskAfter.chapterComplete&&diskAfter.epilogue.phase=="completed"&&diskAfter.epilogue.completionReceipt==accepted,"completion receipt survives actual title scene reload");
            captureTitle("ending-final-real-title-return.png");Done("Native letter finish saved completion and emitted one completion event; the presenter accepted the result, then the real bridge returned to a new physical title scene via "+(hadShell?"the existing shell return flight":"the production Main-to-title fallback")+". Matching completion receipt was re-read from primary disk after scene replacement.");
        }
        finally{if(director!=null)director.CompletionSaved-=saved;}
    }

    // Run after re-entering a genuinely completed save, preferably the one produced by CompleteReturnFrames.
    public static IEnumerator ReplayNativeDismissFrames(XingGuangController ctl,Action<RectTransform> click,Action<string> capture)
    {
        string directory=IsolatedDirectory(ctl);
        if(EndingPresentationActualUiChecks.Requested)foreach(var f in PresentationCompletedFrames(ctl,click,capture))yield return f;
        Need(ctl.Sim.S.chapterComplete&&ctl.Sim.S.epilogue?.phase=="completed"&&!string.IsNullOrEmpty(ctl.Sim.S.epilogue.completionReceipt),"real persisted completed save required for replay");
        Need(ReadLab(directory).epilogue.completionReceipt==ctl.Sim.S.epilogue.completionReceipt,"replay completion receipt matches primary disk");
        var host=ctl.GetComponent<XgEndingPreparationHost>();if(host!=null)host.Hide();Need(ctl.SaveNow(),"completed save checkpoint resets autosave timer before read-only measurement");
        string disk=ReadPayload(directory),lab=JsonUtility.ToJson(ctl.Sim.S);int saves=0,completes=0;
        Action saving=()=>saves++;Action<string> complete=r=>completes++;ctl.runtime.Saving+=saving;ctl.Epilogue.CompletionSaved+=complete;
        try
        {
            foreach(string dismiss in new[]{"Close","Minimize"})
            {
                Need(ctl.Epilogue.ShowReplay(PrologueDirector.Desk?.layer),"production read-only replay opens");foreach(var f in Settle())yield return f;
                for(int i=0;i<3;i++){foreach(var f in WaitEpilogueTransition(ctl))yield return f;foreach(var f in TapEpilogue(ctl,"next",click))yield return f;}
                foreach(var f in TapEpilogue(ctl,"inbox",click))yield return f;
                foreach(var f in Until(()=>Presenter(ctl)?.Visible==true,"native replay letter visible"))yield return f;var p=Presenter(ctl);
                foreach(var f in NativeReady(ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Get("mail").Window))yield return f;
                Need(p.Snapshot.readOnly&&!p.Snapshot.completionEligible,"real bridge marks replay read-only");foreach(var f in Tap(p,"OpenLetter",click))yield return f;
                Need(p.Session.View==EndingLetterView.Reading&&!p.Root.GetComponentsInChildren<Button>(true).Any(b=>b.name=="CompleteEnding"&&b.gameObject.activeInHierarchy),"replay exposes reading but no completion control");
                Need(ctl.Epilogue.HoldingSimulation,"visible replay temporarily holds presentation");
                var scroll=p.Root.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=.35f;foreach(var f in Wait(.7))yield return f;
                capture("ending-final-replay-before-"+dismiss.ToLowerInvariant()+".png");
                var window=ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Get("mail").Window;
                foreach(var f in Tap(window.windowContainer,"ChapterOne"+dismiss,click))yield return f;
                Need(!p.Visible&&p.Session.View==EndingLetterView.Closed&&!ctl.Epilogue.HoldingSimulation&&!ctl.Epilogue.Showing,"actual native "+dismiss+" hides reader and releases completed replay hold");
                Need(saves==0&&completes==0&&JsonUtility.ToJson(ctl.Sim.S)==lab&&ReadPayload(directory)==disk,"native replay "+dismiss+" causes zero saves, no duplicate receipt and no persisted state change");
            }
            Done("Actual native Close and Minimize each dismissed the real read-only public-letter replay, released presentation hold and made zero SaveNow/completion calls; persisted state stayed byte-for-byte unchanged.");
        }
        finally{ctl.runtime.Saving-=saving;ctl.Epilogue.CompletionSaved-=complete;}
    }
}
#endif
