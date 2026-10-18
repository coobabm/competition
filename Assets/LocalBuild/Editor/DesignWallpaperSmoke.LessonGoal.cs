using System;
using System.Collections;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    [MenuItem("Tools/Design Pass/Twelve Question Lesson Goal Scene Smoke")]
    public static void RunLessonGoal() { Start("lesson-goal"); }
    static void TickLessonGoal(XingGuangController controller, ChapterOneRuntime runtime)
    { if(step!=250)return;step=251;controller.StartCoroutine(CheckLessonGoal(controller,runtime)); }
    static IEnumerator CheckLessonGoal(XingGuangController controller,ChapterOneRuntime runtime)
    {
        var steps=LessonGoalSteps(controller,runtime);Exception failure=null;
        while(true){bool more=false;object current=null;try{more=steps.MoveNext();if(more)current=steps.Current;}catch(Exception e){failure=e;}if(failure!=null||!more)break;yield return current;}
        (steps as IDisposable)?.Dispose();File.WriteAllText(Path.Combine(Output,"result.txt"),failure==null?"PASS":"FAIL: "+failure);Finish(failure==null?0:1);
    }
    static T LessonGoalField<T>(object owner,string field)=>(T)owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
    static object ActualDeskGame(XgLabelPage page)
    {
        var desk=LessonGoalField<object>(page,"deskGame");
        var root=(RectTransform)desk.GetType().GetField("root").GetValue(desk);
        Check(root.gameObject.activeInHierarchy,"Actual desk game is visible over classic cards");
        var game=LessonGoalField<object>(desk,"game");
        Check(game!=null,"Actual visible game has started");return game;
    }
    static object ActualSenseStatement(XgLabelPage page)
        => LessonGoalField<object>(ActualDeskGame(page),"statement");
    static bool ActualSenseTruth(object statement)
        => (bool)statement.GetType().GetField("yes").GetValue(statement);
    static void ClickActualSense(XgLabelPage page,bool correct)
    {
        var game=ActualDeskGame(page);var statement=LessonGoalField<object>(game,"statement");
        var control=LessonGoalField<object>(game,ActualSenseTruth(statement)==correct?"yes":"no");
        var button=(UnityEngine.UI.Button)control.GetType().GetField("button").GetValue(control);
        Check(button.isActiveAndEnabled&&button.IsInteractable(),"Visible quiz answer is interactive");
        Click((RectTransform)button.transform);
    }
    // Samples and completed questions are separate: an incorrect answer advances the
    // twelve-question introduction without awarding an accurate teaching sample.
    static void CheckLessonGoalState(XingGuangController controller,int samples)
    {
        var sim=controller.Sim;var page=controller.View.Teach;string before=JsonUtility.ToJson(sim.S);
        var desk=LessonGoalField<object>(page,"deskGame");
        var deskRoot=(RectTransform)desk.GetType().GetField("root").GetValue(desk);
        bool actualGame=deskRoot.gameObject.activeInHierarchy;
        if(actualGame)
        {
            var game=ActualDeskGame(page);var progress=LessonGoalField<TMP_Text>(game,"progress");
            Check(sim.Samples(sim.S.desk)==samples&&progress.isActiveAndEnabled&&progress.text.Contains((sim.TeachingAnswers%12)+"/12"),"Visible game's current set progress matches answered questions: "+sim.TeachingAnswers);
            progress.ForceMeshUpdate();Check(progress.textInfo.characterCount>0&&!progress.isTextOverflowing,"Visible game progress has readable rendered text");
        }
        else
        {
        var goal=LessonGoalField<TMP_Text>(page,"lessonProgress");
        string expected=sim.S.teachingFlow?Math.Min(12,sim.TeachingAnswers)+"/12":Math.Min(samples,XgCatalog.SamplesToTrain)+"/"+XgCatalog.SamplesToTrain;
        Check(sim.Samples(sim.S.desk)==samples && goal.isActiveAndEnabled && goal.text==expected,"Visible progress matches current opening rule: "+expected);
        Check(!LessonGoalField<TMP_Text>(page,"meta").gameObject.activeInHierarchy&&!LessonGoalField<TMP_Text>(page,"qno").gameObject.activeInHierarchy,"First goal replaces competing category/star/question counters");
        goal.ForceMeshUpdate();Check(goal.fontSize>=16&&!goal.isTextOverflowing&&goal.textInfo.lineCount==1,"Goal is readable on one line");
        var a=new Vector3[4];var b=new Vector3[4];goal.rectTransform.GetWorldCorners(a);page.Paper.GetWorldCorners(b);
        Check(page.Paper.parent.InverseTransformPoint(a[0]).y>=page.Paper.parent.InverseTransformPoint(b[1]).y,"Goal remains above the actual card");
        }
        if(sim.S.teachingFlow)
        {
            Check(sim.TeachingTreeOpen==(sim.TeachingAnswers>=12||sim.Has("train.curve")),"Tech gate follows twelve questions");
            Check(sim.TeachingTrainOpen==sim.Has("train.curve"),"Training requires actual paid curve ownership");
            Check(controller.FeatureVisible("train")==sim.TeachingTrainOpen,"Actual training navigation follows purchase gate");
        }
        Check(JsonUtility.ToJson(sim.S)==before,"Reading progress never mutates learned state");
    }
    static IEnumerator LessonGoalSteps(XingGuangController controller,ChapterOneRuntime runtime)
    {
        Check(runtime!=null&&!runtime.useDiskSave&&!runtime.Sim.InPrologue&&!runtime.tickEnabled,"Goal fixture is isolated from disk and world ticks");
        controller.View.Open("teach");yield return new WaitForSecondsRealtime(.3f);
        var sim=controller.Sim;string initial=JsonUtility.ToJson(sim.S);var houseJson=JsonUtility.ToJson(runtime.Sim.S);
        Check(sim.S.teachingFlow&&sim.S.stage==1&&sim.TeachingAnswers==0&&sim.S.epochs==0,"BeginOpening supplies untouched twelve-question flow");
        CheckLessonGoalState(controller,0);
        var statement=ActualSenseStatement(controller.View.Teach);
        ClickActualSense(controller.View.Teach,false);yield return new WaitForSecondsRealtime(.3f);
        Check(sim.S.handWrong==1&&sim.Labels(XgMemes.SenseDesk)==0&&sim.TeachingAnswers==1,"Incorrect judgment advances introduction without adding sample");
        Check(!ReferenceEquals(ActualSenseStatement(controller.View.Teach),statement),"Wrong answer also immediately advances to next card");CheckLessonGoalState(controller,0);
        for(int count=2;count<=12;count++)
        {
            statement=ActualSenseStatement(controller.View.Teach);ClickActualSense(controller.View.Teach,true);yield return new WaitForSecondsRealtime(.25f);
            Check(sim.TeachingAnswers==count&&!ReferenceEquals(ActualSenseStatement(controller.View.Teach),statement),"Real pointer advances question "+count);
            Check(controller.View.Tab=="teach"&&sim.S.epochs==0,"Answer never changes page or starts training");CheckLessonGoalState(controller,count-1);
            Check(!sim.TeachingTrainOpen,"Even enough samples cannot bypass paid curve");
        }
        Check(sim.TeachingTreeOpen&&!sim.TeachingTrainOpen,"Question twelve exposes tech only");
        controller.View.Open("train");Check(controller.View.Tab!="train","Production page navigation rejects unpaid training");
        controller.View.Open("tree");Check(controller.View.Tab=="tree","Production page navigation accepts unlocked tech");
        double cash=controller.Host.Money;Check(sim.BuyNode("train.curve",controller.Host),"Buy real curve after twelve questions");
        Check(controller.Host.Money<cash&&sim.TeachingTrainOpen,"Curve purchase charges money and opens training");
        controller.View.Open("train");yield return new WaitForSecondsRealtime(.25f);
        Check(controller.View.Tab=="train"&&sim.Selected.dataset==XgMemes.SenseDesk&&sim.S.epochs==0,"Training opens on taught data without automatic epoch");
        using(GameText.Bind(()=>GameText.English))
        {
            sim.English=true;GameText.PublishChanged();controller.View.Open("teach");controller.View.Refresh(true);yield return new WaitForSecondsRealtime(.25f);
            CheckLessonGoalState(controller,11);Check(GameText.IsEnglish&&Lang.English,"Live locale switched to English");
            foreach(var text in controller.View.Teach.root.GetComponentsInChildren<TMP_Text>())
                if(text.name=="LessonProgress"){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"English goal fits actual text mesh");}
            LessonGoalCapture("lesson-goal-twelve-english");
        }
        GameText.PublishChanged();
        // Serialize interrupted new-flow progress and paid flow separately. No new
        // counters or inferred purchases may be awarded during restoration.
        foreach(int answers in new[]{2,7,12})
        {
            var state=JsonUtility.FromJson<XgState>(initial);state.handCorrect=answers;state.labels.Add(new XgLabelCount{dataset=XgMemes.SenseDesk,count=answers});
            var house=JsonUtility.FromJson<GameState>(houseJson);house.labState=JsonUtility.ToJson(state);runtime.SetSimulationForTests(new ChapterOneSim(house));
            controller.View.Open("teach");controller.View.Refresh(true);yield return new WaitForSecondsRealtime(.3f);
            CheckLessonGoalState(controller,answers);Check(!controller.Sim.TeachingTrainOpen,"Reload never grants unpaid curve: "+answers);
            string before=JsonUtility.ToJson(controller.Sim.S);controller.View.Refresh(true);Check(before==JsonUtility.ToJson(controller.Sim.S),"Refresh preserves resumed opening "+answers);
        }
        controller.Sim.S.epochs=1;controller.View.Refresh(true);
        Check(ActualDeskGame(controller.View.Teach)!=null&&controller.Sim.S.epochs==1,"Trained save retains the actual game and completed training progress");
        Check(!runtime.useDiskSave&&!runtime.tickEnabled,"All goal checks leave player disk save isolated");
    }
    static void LessonGoalCapture(string name){Capture(name);CaptureVisualState(name);DesktopFadeFullScreenshot(name);}
}
