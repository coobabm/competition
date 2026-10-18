#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.CaseMenu;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;

// Reuses the one serial final-route runner; no second scene owner or inference service.
public static class EndingPresentationActualUiChecks
{
    const string Key="EndingPresentationActualUiChecks.Requested";
    public static bool Requested=>SessionState.GetBool(Key,false);
    public const string Scope="Presentation extension: explicit reduceFx preference fixtures; actual released date and completed-save checkpoints from the final-route suite. Natural date/headline/cards and successful-save closure use ordinary frames. Skip uses actual LCD controls. Save swap uses an explicit unrelated in-memory save. The completed-save race replays the bridge's receipt observer with a real durable receipt; it is a notification-delivery fixture, not a second earned completion. No natural early-stage progression, music/fan or reduced-mode title-return claim.";
    [MenuItem("Tools/Local Verification/Ending presentation actual UI")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run under the serial editor owner from EditMode.");
        SessionState.SetBool(Key,true);
        try{EndingFinalRouteUiRunner.Run();}catch{ClearRequest();throw;}
    }
    public static void ClearRequest()=>SessionState.EraseBool(Key);
    [MenuItem("Tools/Local Verification/Ending completed save replay actual UI")]
    public static void RunCompletedReplay()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run under the serial editor owner from EditMode.");
        SessionState.SetBool(Key,true);
        try{EndingCompletedReplayUiRunner.Run();}catch{ClearRequest();throw;}
    }
    public static void RunCompletedReplayFromEvidence(string reportPath,string saveDirectory=null)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run under the serial editor owner from EditMode.");
        SessionState.SetBool(Key,true);
        try{EndingCompletedReplayUiRunner.RunFromEvidence(reportPath,saveDirectory);}catch{ClearRequest();throw;}
    }
}

public static partial class EndingFinalRouteUiChecks
{
    static XgEpilogueView EpView(XingGuangController ctl)=>Get<XgEpilogueView>(ctl.Epilogue,"view");
    static IEnumerable<object> WaitEpilogueTransition(XingGuangController ctl)
    {
        var view=EpView(ctl);Need(view!=null&&view.Showing,"real presentation exists before natural wait");
        string phase=ctl.Sim.S.epilogue.phase;int revision=ctl.Sim.S.epilogue.revision;
        foreach(var f in Until(()=>view!=null&&!view.TransitionPlaying,"actual presentation finishes naturally",10))yield return f;
        Need(ctl.Sim.S.epilogue.phase==phase&&ctl.Sim.S.epilogue.revision==revision,"animation waiting does not advance the persisted phase/revision");
    }
    static IEnumerable<object> PresentationBeforeLetter(XingGuangController ctl,Action<RectTransform> click,Action<string> capture)
    {
        var host=ctl.GetComponent<XgEndingPreparationHost>();if(host!=null)host.Hide();
        Need(ctl.Sim.S.epilogue?.phase=="date","presentation check begins at actually earned date phase");
        bool reducedBefore=ctl.Sim.S.reduceFx;ctl.Sim.S.reduceFx=false; // Explicit UI preference fixture, not a settings-click claim.
        string directory=IsolatedDirectory(ctl);Need(ctl.SaveNow(),"date checkpoint durably saved before display-only measurements");
        string ep=JsonUtility.ToJson(ctl.Sim.S.epilogue),release=JsonUtility.ToJson(ctl.Sim.S.releaseOutcome),disk=ReadPayload(directory);
        int date=GameCalendar.Yyyymmdd(GameCalendar.Now(ctl.runtime.Sim.S));
        string suffix=GameText.IsEnglish?"en":"zh";
        Need(ctl.Epilogue.Show(PrologueDirector.Desk?.layer),"real date presentation opens");var view=EpView(ctl);
        Need(view.TransitionPlaying,"ordinary motion starts at actual date view");
        foreach(var f in Wait(.8))yield return f;capture("ending-presentation-date-fade-"+suffix+".png");
        foreach(var f in TapEpilogue(ctl,"next",click))yield return f;
        Need(!view.TransitionPlaying&&ctl.Sim.S.epilogue.phase=="date","actual Skip settles the same page without phase advance");
        Need(Get<TMP_Text>(view,"day").text=="13","Skip lands on June 13 static calendar");
        ctl.Epilogue.Hide();ctl.Sim.S.reduceFx=true;Need(ctl.Epilogue.Show(PrologueDirector.Desk?.layer),"reduced date opens");
        Need(!view.TransitionPlaying&&Get<TMP_Text>(view,"day").text=="13","reduced date is immediately static");capture("ending-presentation-date-reduced-"+suffix+".png");
        ctl.Epilogue.Hide();ctl.Sim.S.reduceFx=false;Need(ctl.Epilogue.Show(PrologueDirector.Desk?.layer),"natural calendar reopens");
        double began=EditorApplication.timeSinceStartup;
        foreach(var f in Wait(4.4))yield return f;Need(view.TransitionPlaying,"calendar is still naturally turning after fade");capture("ending-presentation-calendar-turn-"+suffix+".png");
        foreach(var f in WaitEpilogueTransition(ctl))yield return f;
        Need(EditorApplication.timeSinceStartup-began>=6.8&&Get<TMP_Text>(view,"day").text=="13","natural date sequence reaches target after its actual seven-second duration");
        Need(JsonUtility.ToJson(ctl.Sim.S.epilogue)==ep&&JsonUtility.ToJson(ctl.Sim.S.releaseOutcome)==release&&ReadPayload(directory)==disk&&GameCalendar.Yyyymmdd(GameCalendar.Now(ctl.runtime.Sim.S))==date,"skip/reduced/natural date never mutate calendar, release, epilogue receipts or primary disk");
        capture("ending-presentation-calendar-static-"+suffix+".png");
        ctl.Epilogue.Hide();ctl.Sim.S.reduceFx=reducedBefore;
        Done("Presentation "+suffix+": actual date Skip preserved phase; reduced-effects static and full natural seven-second calendar passed with unchanged simulation date/receipts/disk. Preference switching was an explicit fixture.");
    }
    static IEnumerable<object> PresentationAfterSaved(XingGuangController ctl,Action<string> capture)
    {
        var view=EpView(ctl);Need(view!=null&&view.CompletionVisualActive&&ctl.Sim.S.chapterComplete,"actual successful save immediately shows closure while completion is already durable");
        Need(!ctl.Sim.S.reduceFx,"successful-save closure run explicitly exercises ordinary effects");
        double began=EditorApplication.timeSinceStartup;string receipt=ctl.Sim.S.epilogue.completionReceipt;
        foreach(var f in Wait(.85))yield return f;
        Need(ctl!=null&&view!=null&&view.CompletionVisualActive,"successful-save white closure remains visible before title routing");
        Need(Get<TMP_Text>(view,"heading").color.a>.95f,"closure final text naturally fades in");
        capture?.Invoke("ending-presentation-real-saved-closure.png");
        foreach(var f in Until(()=>view==null||!view.CompletionVisualActive,"saved closure natural finish",4))yield return f;
        Need(EditorApplication.timeSinceStartup-began>=2.8,"actual saved closure has a bounded three-second reading interval before routing");
        Done("Successful real CompleteEnding: immediate disk/event semantics preserved; white closure text faded in naturally and remained until its three-second visual interval. Completion receipt: "+receipt);
    }
    static IEnumerable<object> PresentationCompletedFrames(XingGuangController ctl,Action<RectTransform> click,Action<string> capture)
    {
        string directory=IsolatedDirectory(ctl);var director=ctl.Epilogue;string receipt=ctl.Sim.S.epilogue.completionReceipt;
        Need(ctl.Sim.S.chapterComplete&&!string.IsNullOrEmpty(receipt),"closure interruption checks use a genuinely saved completion");
        bool reducedBefore=ctl.Sim.S.reduceFx;ctl.Sim.S.reduceFx=false;
        Need(ctl.SaveNow()&&ctl.runtime.SaveSlot(6),"completed closure checkpoint saved in isolated archive 6");
        string before=JsonUtility.ToJson(ctl.Sim.S),disk=ReadPayload(directory);int saves=0,completes=0;
        Action saving=()=>saves++;Action<string> completed=r=>completes++;ctl.runtime.Saving+=saving;director.CompletionSaved+=completed;
        try
        {
            Need(director.ShowCompletionPresentation(director.CaptureBinding(),receipt),"real saved receipt can present closure without completing again");
            var view=EpView(ctl);foreach(var f in TapEpilogue(ctl,"next",click))yield return f;
            Need(!view.CompletionVisualActive,"actual Return button skips closure presentation");
            Need(director.ShowCompletionPresentation(director.CaptureBinding(),receipt),"closure reopens for Hide check");director.Hide();
            Need(!director.CompletionPresentationPending,"Hide releases closure wait");
            Need(director.ShowCompletionPresentation(director.CaptureBinding(),receipt),"closure reopens for disabled-view check");view.enabled=false;
            Need(!director.CompletionPresentationPending,"disabled view releases closure wait");view.enabled=true;director.Hide();
            Need(director.ShowCompletionPresentation(director.CaptureBinding(),receipt),"closure reopens for destroy check");UnityEngine.Object.Destroy(view.gameObject);
            foreach(var f in Settle())yield return f;Need(!director.CompletionPresentationPending,"destroyed view releases closure wait");
            ctl.Sim.S.reduceFx=true;Need(director.ShowCompletionPresentation(director.CaptureBinding(),receipt),"reduced closure recreates view safely");view=EpView(ctl);
            Need(Get<TMP_Text>(view,"heading").color.a==1,"reduced closure text is fully visible immediately");
            capture("ending-presentation-closure-reduced.png");double began=EditorApplication.timeSinceStartup;
            foreach(var f in Until(()=>!view.CompletionVisualActive,"reduced closure bounded finish",4))yield return f;
            Need(EditorApplication.timeSinceStartup-began>=2.8,"reduced closure keeps bounded reading time without animation");
            director.Hide();ctl.Sim.S.reduceFx=false;
            Need(saves==0&&completes==0&&JsonUtility.ToJson(ctl.Sim.S)==before&&ReadPayload(directory)==disk,"closure skip/Hide/disable/destroy/reduced replay creates no save, receipt or state mutation");
        }
        finally{ctl.runtime.Saving-=saving;director.CompletionSaved-=completed;ctl.Sim.S.reduceFx=reducedBefore;}
        // Delivery-race fixture: replay the bridge observer with the existing receipt, not CompletionSaved or Complete.
        // Swap before the next frame, so no title route may belong to the replacement owner.
        var original=ctl.runtime.Sim;var token=director.CaptureBinding();var bridge=ctl.GetComponent<XgEpilogueMailBridge>();
        Need(bridge!=null,"real bridge exists for saved-notification race");
        typeof(XgEpilogueMailBridge).GetMethod("OnCompletionSaved",F).Invoke(bridge,new object[]{receipt});
        Need(director.CompletionPresentationPending,"observer replay begins the actual closure wait");
        var replacement=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(original.S));var replacementLab=JsonUtility.FromJson<XgState>(replacement.labState);
        replacementLab.releaseOutcome=null;replacementLab.epilogue=null;replacementLab.endingPreparation=null;replacementLab.chapterComplete=false;replacement.labState=JsonUtility.ToJson(replacementLab);
        ctl.runtime.SetSimulationForTests(new ChapterOneSim(replacement));
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(ctl.runtime,new object[]{false});ctl.runtime.tickEnabled=false;
        var menu=UnityEngine.Object.FindAnyObjectByType<CaseMenuController>();int sceneBefore=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        foreach(var f in Wait(4.3))yield return f;
        Need(!director.IsCurrent(token)&&!director.CompletionPresentationPending&&!director.ShowCompletionPresentation(token,receipt),"save swap rejects stale closure binding and hides its visual");
        var menuAfter=UnityEngine.Object.FindAnyObjectByType<CaseMenuController>();
        Need(UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle==sceneBefore&&
            (menu==null?menuAfter==null:menu==menuAfter&&menu.HasEnteredGame&&!menu.IsReturningToTitle),
            "replayed completion notification keeps the same Main/shell and never routes replacement save to title after deadline");
        Need(Get<string>(bridge,"completionPending")=="","replacement save clears pending receipt routing");
        IsolatedDirectory(ctl);Need(ctl.runtime.LoadArchive(6),"actual archive reload restores completed save after race fixture");
        Need(ctl.Sim.S.chapterComplete&&ctl.Sim.S.epilogue.completionReceipt==receipt,"restored completed save retains the original durable receipt");
        Done("Completed-save presentation replay passed actual Skip, Hide, view disable/destroy, reduced static reading interval and stale-owner rejection without new completion. Explicit observer-delivery/save-swap fixture proved pending bridge routing was discarded for another save; actual completed archive was reloaded.");
    }
}
#endif
