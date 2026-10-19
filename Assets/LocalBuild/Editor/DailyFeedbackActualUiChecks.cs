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
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DailyFeedbackActualUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    public const string Scope="Independent actual daily feedback UI. Explicit prerequisites: stage4/fullOpen, opening flags, base training nodes, three historical song and three historical private-AI game receipts, collected lesson/quiz history and sufficient wallet. These are fixture history, not actual playback/gameplay/progression. Real finite household host; real LCD purchases of daily.rhythm/daily.dice/train.monitor and Train controls; natural epoch completion, bilingual observed cue, actual native Minimize during an epoch, reduced-effects preference fixture, actual current risk/power/cooling read-only projection. No fabricated waveform/BPM/epoch. Known-date YY UI and forced hot-hardware cases are not covered.";
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount {get;private set;}
    static T Get<T>(object value,string field)=>(T)value.GetType().GetField(field,F).GetValue(value);
    static void Need(bool value,string message){AssertionCount++;if(!value)throw new InvalidOperationException("Daily feedback actual UI: "+message);}
    static void Done(string message){Checks.Add(message);Debug.Log("Daily feedback actual UI: "+message);}
    static IEnumerator Until(Func<bool> ready,string label,double seconds=20)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!ready()){Need(EditorApplication.timeSinceStartup<end,label+" timed out");yield return null;}}
    static IEnumerator Wait(double seconds)
    {double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
    static IEnumerator Tap(RectTransform target,string label,Action<RectTransform> click)
    {foreach(var frame in LcdActualUiProbe.WaitForClick(target,label))yield return frame;click(target);}
    static bool NativeMinimizeSettled(XingGuangController ctl)
    {
        var window=ctl.Window;
        if(window==null||!window.isOn||!window.gameObject.activeInHierarchy)return false;
        var group=window.GetComponent<CanvasGroup>();var animator=window.windowAnimator;
        if(group==null||group.alpha>.001f||group.interactable||group.blocksRaycasts||animator==null)return false;
        var state=animator.GetCurrentAnimatorStateInfo(0);
        return state.IsName("Minimize")&&(!animator.isActiveAndEnabled||(!animator.IsInTransition(0)&&state.normalizedTime>=1f));
    }
    public static ChapterOneSim Fixture()
    {
        var lab=new XgSim();lab.BeginOpening();lab.S.stage=lab.S.stageVision=lab.S.stageSequence=4;lab.S.fullOpen=true;
        lab.S.abilitiesEmerged=Enumerable.Range(1,4).ToList();lab.S.teachingFlow=false;
        lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;
        lab.MarkAfterthought("first.drift");
        foreach(string id in new[]{"train.curve","shared.lr","train.checkpoint"})if(!lab.S.unlocked.Contains(id))lab.S.unlocked.Add(id);
        for(int i=0;i<3;i++){lab.RecordDailySongCompleted("qa-history-song-"+i,"qa-history-play-"+i);lab.RecordDailyGameCompleted("gomoku","qa-history-game-"+i,true);}
        lab.S.lessonCollected=XgTrainingLessons.Cards.Select(x=>x.id).ToList();lab.S.lessonDiscovered=new List<string>(lab.S.lessonCollected);
        lab.S.lessonPending.Clear();lab.S.quizAnswered=XgTrainingLessons.Quizzes.Select(x=>x.id).ToList();lab.S.quizQueued.Clear();lab.S.trainingLessonObservedStage=4;
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);GameCalendar.Advance(house.S,GameCalendar.DayFor(4,0));house.S.rentDay=GameCalendar.CurrentDay(house.S);
        return house;
    }
    public static IEnumerator DismissAssessment(XingGuangController ctl,Action<RectTransform> click)
    {
        if(!ctl.View.Train.AssessmentBlocksInput)yield break;
        yield return Tap(Get<RectTransform>(ctl.View.Train,"card"),"actual assessment dismissal",click);
        yield return Until(()=>!ctl.View.Train.AssessmentBlocksInput,"assessment dismisses",4);
    }
    static IEnumerator FoldGuideNote(Action<RectTransform> click,Action<string> capture)
    {
        var guide=GuideNote.Instance;
        var note=guide==null?null:Get<RectTransform>(guide,"note");
        var task=note==null?null:note.Find("Paper/Task 0");
        if(note==null||!note.gameObject.activeInHierarchy||task==null||!task.gameObject.activeInHierarchy)yield break;
        var fold=note.Find("Paper/Title Bar/Fold") as RectTransform;
        Need(fold!=null&&fold.GetComponent<Button>().IsInteractable(),"visible guide note has its normal Fold control");
        float expandedHeight=note.rect.height;
        capture("daily-guide-before-fold.png");
        yield return Tap(fold,"normal guide note Fold before window controls",click);
        yield return Until(()=>!task.gameObject.activeInHierarchy,"guide task row hides after actual Fold",3);
        Need(note.gameObject.activeInHierarchy&&fold.gameObject.activeInHierarchy&&note.rect.height<expandedHeight,
            "Fold leaves the guide's visible title strip while clearing its task rows from the window caption");
        Need(fold.GetComponent<Graphic>().raycastTarget,"normal guide Fold remains raycastable");
        capture("daily-guide-after-fold.png");
        Done("Actual LCD click on the guide note's Fold control clears its task overlay before native Minimize; no raycast or sibling-order bypass.");
    }
    static IEnumerator Buy(XingGuangController ctl,string id,Action<RectTransform> click)
    {
        Need(!ctl.Sim.Has(id),"target purchase starts unowned: "+id);
        ctl.View.Open("tree");ctl.View.Refresh(true);yield return null;
        var tree=ctl.View.Tree;Need(tree.FocusNode(id),"navigation fixture focuses actual node "+id);
        yield return Until(()=>!tree.Pan.Moving,"tree focus settles",5);
        var nodes=tree.root.GetComponentsInChildren<XgTechNodeView>(true).Where(x=>x.Covers(id)&&x.gameObject.activeInHierarchy).ToArray();
        Need(nodes.Length==1,"exactly one live node for "+id);
        var node=XgTechTree.Next(nodes[0].entry,ctl.Sim);Need(node!=null&&node.id==id,"real next purchase is the requested node");
        Need(ctl.Sim.Status(node,ctl.Host)==XgSim.NodeStatus.Buyable,"fixture satisfies real purchase requirements: "+id+"; "+ctl.Sim.Why(node,ctl.Host));
        double before=ctl.Host.Money,cost=ctl.Sim.NodeCost(node);
        yield return Tap((RectTransform)nodes[0].transform,"actual purchase "+id,click);
        Need(ctl.Sim.Has(id)&&Math.Abs((before-ctl.Host.Money)-cost)<1e-7,"actual node click owns "+id+" and debits its real price");
        Done("Real tree purchase: "+id+", paid "+cost+" from the finite household wallet.");yield return Wait(.45);
    }
    static void RiskProjection(XingGuangController ctl,bool english)
    {
        var sim=ctl.Sim;var home=ctl.Host as XingGuangHost;Need(home!=null,"risk page uses the real household host");
        string lab=JsonUtility.ToJson(sim.S),house=JsonUtility.ToJson(ctl.runtime.Sim.S);
        ctl.View.Refresh(true);var run=sim.Run(sim.SelectedTrack);var forecast=sim.TrainingRiskForecast(run);
        string risk=Get<TMP_Text>(ctl.View.Train,"risk").text,bill=Get<TMP_Text>(ctl.View.Train,"trainBill").text;
        Need(risk.Contains(sim.TrainingRiskLine(forecast)),"visible risk line includes current production forecast");
        string hot=english?"The cards are hot and may burn out":"显卡太热，可能烧卡";
        Need(risk.Contains(hot)==home.WearRiskLive,"visible cooling warning agrees with current real host");
        double price=home.RoundPrice(XgSim.DurationFor(run),sim.TrainPriceFactor,sim.TrainEnergyFactor);
        Need(bill==(english?"Power ¥":"电费 ¥")+XgUi.N(price,price<10?"0.0":"0"),"visible power price agrees with actual next-round billing");
        Need(JsonUtility.ToJson(sim.S)==lab&&JsonUtility.ToJson(ctl.runtime.Sim.S)==house,"synchronous risk/power/cooling projection has no state mutation");
        Done((english?"English":"Chinese")+" read-only actual forecast/power projection passed; live hot-card warning="+home.WearRiskLive+". No forced thermal boundary claim.");
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        Checks.Clear();AssertionCount=0;var sim=ctl.Sim;bool wasEnabled=ctl.enabled,reduced=sim.S.reduceFx,english=GameText.IsEnglish;
        Need(!ctl.runtime.TestMode&&ctl.Host is XingGuangHost&&ctl.Host.Compute>0&&ctl.Host.VramMB>0,"real finite installed hardware and non-test household host");
        ctl.enabled=false;ctl.runtime.tickEnabled=false;ctl.runtime.Sim.PowerCostsEnabled=true;
        sim.S.reduceFx=false;ctl.View.Juice.Reduced=false;
        try
        {
            ctl.Open();yield return Wait(1.3);Need(ctl.View.Visible,"actual lab window visible");
            Need(sim.DailyNodeReady("daily.rhythm")&&sim.DailyNodeReady("daily.dice"),"explicit historical receipt fixtures qualify both unpaid daily nodes");
            yield return Buy(ctl,"daily.rhythm",click);yield return Buy(ctl,"daily.dice",click);
            Need(!sim.PreciseRiskForecast,"dice alone does not unlock monitor precision");yield return Buy(ctl,"train.monitor",click);
            Need(sim.DailyRhythmFeedback&&sim.PreciseRiskForecast,"real purchases enable rhythm and precise risk");
            sim.SelectDesk("sense");
            for(int i=0;i<5&&sim.Labels("sense")<4;i++)
            {
                var card=sim.Card("sense");Need(sim.TeachingGuess(card,out bool guess)&&sim.TryEvaluateTeachingGuess("sense",card.id,guess,guess==card.truth,ctl.Host,out var result)&&result.correct,"real teaching-rule prerequisite sample "+i);
            }
            Need(sim.Labels("sense")>=4,"real teaching rules supply training samples; no direct label-count fixture");
            sim.SelectedTrack=XgTrack.Sequence;sim.SetDataset(XgTrack.Sequence,"sense");ctl.View.Open("train");ctl.View.Refresh(true);yield return Wait(.5);
            for(int i=0;i<2;i++){language(i==1);sim.English=i==1;ctl.View.Refresh(true);yield return null;RiskProjection(ctl,i==1);capture("daily-forecast-"+(i==1?"en":"zh")+".png");}
            ctl.enabled=true;yield return DailyRhythmActualUiChecks.Frames(ctl,language,click,capture);
            foreach(var check in DailyRhythmActualUiChecks.Checks)Done(check);
            ctl.enabled=false;yield return DismissAssessment(ctl,click);
            yield return FoldGuideNote(click,capture);
            var page=ctl.View.Train;var train=Get<XgBtn>(page,"train");int before=sim.Run(sim.SelectedTrack).epoch;float cue=Get<float>(page,"lastRhythmText");
            Need(!sim.AnyEpochActive&&train.button.IsInteractable(),"hidden case starts idle with real Train enabled");
            yield return Tap(train.rt,"Train before native Minimize",click);Need(sim.AnyEpochActive,"actual Train starts an epoch before hidden-window check");
            var min=ctl.Window.windowContainer.Find("Dragger/ChapterOneCaptionButtons/ChapterOneMinimize");Need(min!=null&&min.GetComponent<Button>().IsInteractable(),"actual native Minimize exists");
            yield return Tap((RectTransform)min,"native Minimize during real epoch",click);
            Need(!ctl.View.Visible&&!Get<bool>(page,"rhythmPending"),"native Minimize immediately suppresses lab presentation and clears pending rhythm");
            yield return Until(()=>NativeMinimizeSettled(ctl),"native Minimize reaches transparent non-interactive animation endpoint",5);
            Need(!ctl.View.Visible&&NativeMinimizeSettled(ctl),"minimized lab remains open but is transparent, non-interactive and non-raycastable");
            capture("daily-rhythm-native-minimized.png");
            Done("Native Minimize retained isOn/active and reached its authored Minimize endpoint: alpha <= 0.001, interactable=false, blocksRaycasts=false; lab presentation is suppressed.");
            ctl.enabled=true;yield return Until(()=>sim.Run(sim.SelectedTrack).epoch>before,"hidden epoch completes naturally");
            Need(sim.Run(sim.SelectedTrack).epoch==before+1&&!Get<bool>(page,"rhythmPending")&&Get<float>(page,"lastRhythmText")==cue,"hidden real round completes without queued or visible rhythm cue");
            ctl.enabled=false;ctl.Open();ctl.View.Open("train");yield return Wait(1.3);
            var restoredGroup=ctl.Window.GetComponent<CanvasGroup>();
            Need(ctl.View.Visible&&restoredGroup!=null&&restoredGroup.alpha>=.99f&&restoredGroup.interactable&&restoredGroup.blocksRaycasts,
                "native reopen restores actual lab visibility and input");
            Need(!Get<bool>(page,"rhythmPending")&&Get<float>(page,"lastRhythmText")==cue,"reopen does not replay stale hidden completion cue");capture("daily-rhythm-hidden-reopened.png");
            Done("Actual Train plus native Minimize: epoch completed while hidden, and reopening emitted no stale rhythm cue.");
            yield return DismissAssessment(ctl,click);sim.S.reduceFx=true;ctl.View.Juice.Reduced=true;ctl.View.Refresh(true);
            before=sim.Run(sim.SelectedTrack).epoch;cue=Get<float>(page,"lastRhythmText");Need(train.button.IsInteractable(),"reduced case Train enabled");
            yield return Tap(train.rt,"reduced-effects actual Train",click);ctl.enabled=true;
            yield return Until(()=>sim.Run(sim.SelectedTrack).epoch>before,"reduced epoch completes naturally");
            Need(sim.Run(sim.SelectedTrack).epoch==before+1&&!Get<bool>(page,"rhythmPending")&&Get<float>(page,"lastRhythmText")==cue,"reduced real round completes without delayed accent or rhythm floating text");
            capture("daily-rhythm-reduced.png");Done("Reduced-effects preference fixture: real Train result completed without delayed rhythm cue.");
            ctl.enabled=false;
            string saveRoot=ChapterOneRuntime.VerificationSaveDirectory;
            Need(!string.IsNullOrWhiteSpace(saveRoot),"explicit verification save root exists");
            saveRoot=Path.GetFullPath(saveRoot).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            Need(saveRoot.IndexOf(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)>=0&&Path.GetFullPath(ctl.runtime.SavePath).StartsWith(saveRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"final save is inside isolated LocalVerification folder");
            ctl.runtime.useDiskSave=true;Need(ctl.SaveNow(),"isolated final checkpoint saves actual purchases and real rounds");
            var loaded=new AtomicSaveStore(saveRoot).Load();Need(loaded.Success&&!loaded.Recovered,"actual final primary save validates without recovery");
            var savedHouse=JsonUtility.FromJson<GameState>(loaded.Payload);
            Need(savedHouse!=null&&!string.IsNullOrEmpty(savedHouse.labState),"primary payload has actual household and lab state");
            var saved=JsonUtility.FromJson<XgState>(savedHouse.labState);
            Need(saved!=null&&saved.unlocked!=null&&saved.sequence!=null,"primary lab has technology unlocks and sequence run");
            foreach(string id in new[]{"daily.rhythm","daily.dice","train.monitor"})
                Done("Primary purchase read: "+id+"; unlocked="+saved.unlocked.Contains(id)+"; live Has="+sim.Has(id)+"; dataset owned="+(saved.owned!=null&&saved.owned.Contains(id))+" (not a technology field).");
            Done("Primary rounds/wallet read: disk epoch="+saved.sequence.epoch+"; live epoch="+sim.Run(XgTrack.Sequence).epoch+
                "; disk money="+savedHouse.money.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+".");
            foreach(string id in new[]{"daily.rhythm","daily.dice","train.monitor"})
                Need(saved.unlocked.Contains(id)&&sim.Has(id),"primary technology unlock and real live purchase agree: "+id);
            Need(saved.sequence.epoch==sim.Run(XgTrack.Sequence).epoch,"primary epoch equals naturally completed live epoch");
            Need(saved.sequence.epoch>=4,"primary records at least four natural clicked rounds");
            Need(!double.IsNaN(savedHouse.money)&&!double.IsInfinity(savedHouse.money)&&savedHouse.money>=0,"primary household wallet is finite and nonnegative");
            Done("Real SaveNow primary stores all three actual purchases and four naturally completed rounds.");
        }
        finally{ctl.enabled=wasEnabled;sim.S.reduceFx=reduced;ctl.View.Juice.Reduced=reduced;language(english);sim.English=english;}
    }
}
#endif
