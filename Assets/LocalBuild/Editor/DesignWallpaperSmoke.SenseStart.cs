using System;
using System.Collections;
using System.IO;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Isolated scene acceptance for the default common-sense lesson and arithmetic progression.
public static partial class DesignWallpaperSmoke
{
    [MenuItem("Tools/Design Pass/Common Sense Start Scene Smoke")]
    public static void RunSenseStart() { Start("sense-start"); }

    static void TickSenseStart(XingGuangController controller, ChapterOneRuntime runtime)
    {
        if (step != 240) return;
        step = 241;
        controller.StartCoroutine(CheckSenseStart(controller, runtime));
    }

    static IEnumerator CheckSenseStart(XingGuangController controller, ChapterOneRuntime runtime)
    {
        var checks = SenseStartSteps(controller, runtime);
        Exception failure = null;
        while (true)
        {
            bool more = false; object current = null;
            try { more = checks.MoveNext(); if (more) current = checks.Current; }
            catch (Exception error) { failure = error; }
            if (failure != null || !more) break;
            yield return current;
        }
        (checks as IDisposable)?.Dispose();
        File.WriteAllText(Path.Combine(Output, "result.txt"), failure == null ? "PASS" : "FAIL: " + failure);
        Finish(failure == null ? 0 : 1);
    }

    static T SenseStartField<T>(object owner, string field) => (T)owner.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

    static IEnumerator SenseStartSteps(XingGuangController controller, ChapterOneRuntime runtime)
    {
        // Legacy four-sample entry checks are replaced below by real twelve-answer/purchase flow.
        Check(runtime != null && !runtime.useDiskSave && !runtime.Sim.InPrologue,
            "Common-sense scene uses the isolated post-setup fixture");
        var savedHouse = runtime.Sim;
        string houseJson = JsonUtility.ToJson(savedHouse.S);
        bool savedTick = runtime.tickEnabled, savedController = controller.enabled, savedView = controller.View.enabled;
        try
        {
            runtime.tickEnabled = false; controller.enabled = false; controller.View.enabled = true;
            controller.View.Open("teach");
            yield return new WaitForSecondsRealtime(.2f);
            var sim = controller.Sim;
            var teach = controller.View.Teach;
            string freshJson = JsonUtility.ToJson(sim.S);
            var statement = ActualSenseStatement(teach);
            Check(sim.S.stage == 1 && sim.S.desk == XgMemes.SenseDesk
                && sim.S.sequence.dataset == XgMemes.SenseDesk,
                "Real new-game teaching selection and stored sequence training data are common sense");
            bool authoredOpening=false;
            foreach(var phrase in XgMemes.SenseBank)authoredOpening|=phrase.text==(string)statement.GetType().GetField("text").GetValue(statement)&&phrase.yes==ActualSenseTruth(statement);
            Check(controller.View.Tab == "teach" && authoredOpening,
                "Actual visible quiz presents an authored common-sense statement");
            Check(!sim.DeskOpen("arith") && !sim.ProgressionDeskAvailable("arith"),
                "Stage-one scene keeps arithmetic unavailable");
            // Existing teaching-mode policy omits locked desks before stage three. Do not
            // change teachMode or invent a grey row merely to make this test click one.
            Check(SenseStartHallRow(teach, "arith") == null,
                "Stage-one teaching hall offers no arithmetic row under its existing locked-desk policy");
            yield return new WaitForEndOfFrame();
            SenseStartCapture("sense-start-default-stage-one");

            // Preserve actual pointer input and exact sample/dataset handoff while
            // enforcing the current 12 answers -> tech -> paid curve gates.
            for(int i=0;i<12;i++)
            {
                statement=ActualSenseStatement(teach);double before=sim.Labels(XgMemes.SenseDesk);
                ClickActualSense(teach,true);
                yield return new WaitForSecondsRealtime(.3f);
                Check(!ReferenceEquals(statement,ActualSenseStatement(teach)),"Actual game advances its displayed statement");
                Check(sim.Labels(XgMemes.SenseDesk)==before+1,"Real input contributes one accurate common-sense sample");
                Check(sim.TeachingAnswers==i+1 && !sim.TeachingTrainOpen,"Twelve-question progress never grants unpaid training");
                Check(sim.TeachingTreeOpen==(i==11),"Tech opens precisely after question twelve");
            }
            controller.View.Open("tree");Check(controller.View.Tab=="tree","Unlocked tech page opens");
            double cash=controller.Host.Money;
            Check(sim.BuyNode("train.curve",controller.Host)&&controller.Host.Money<cash,"Real curve purchase deducts funds");
            controller.View.Open("train");yield return new WaitForSecondsRealtime(.3f);
            Check(controller.View.Tab=="train"&&sim.SelectedTrack==XgTrack.Sequence&&sim.Selected.dataset==XgMemes.SenseDesk&&sim.S.sequence.epoch==0,"Training handoff preserves actual taught dataset without auto epoch");
            // The new answer sheet replaces the removed legacy learningText comparison.
            // Assert every rendered question and answer against the exact saved sheet item.
            var rows=SenseStartField<RectTransform>(controller.View.Train,"sheetRowsRoot");
            var sheet=sim.Sheet(sim.Selected);
            Check(rows.gameObject.activeInHierarchy&&sheet.Count>0,"Actual training answer sheet is visible");
            for(int i=0;i<sheet.Count;i++)
            {
                var row=rows.Find("Question"+(i+1));Check(row!=null&&row.gameObject.activeInHierarchy,"Actual answer row exists "+i);
                string question=sim.SheetQuestion(sheet[i]),answer=sim.SheetAnswer(sim.Selected,sheet[i]);
                bool hasQuestion=false,hasAnswer=false;
                foreach(var label in row.GetComponentsInChildren<TMP_Text>()){hasQuestion|=label.text.Contains(question);hasAnswer|=label.text.Contains(answer);}
                Check(hasQuestion&&hasAnswer&&sheet[i].dataset==XgMemes.SenseDesk,"Visible comparison binds saved sense question and model answer "+i);
                var source=sim.SheetCard(sheet[i]);bool authored=false;
                foreach(var phrase in XgMemes.SenseBank)authored|=phrase.text==source.source.question&&phrase.yes==source.truth;
                Check(authored,"Sheet question comes from authored common-sense bank "+i);
            }
            string comparisonState=JsonUtility.ToJson(sim.S);controller.View.Refresh(true);
            Check(comparisonState==JsonUtility.ToJson(sim.S),"Refreshing sheet preserves learning and saved state");
            yield return new WaitForEndOfFrame();SenseStartCapture("sense-start-training-handoff");

            // Stage two is an explicit boundary fixture, not a claim of playing through
            // the chapter. Production loading and Tick perform the real desk unlock.
            var second = JsonUtility.FromJson<XgState>(freshJson); second.stage = 2; second.openingFocus=false;
            SenseStartLoad(controller, runtime, houseJson, second);
            controller.Sim.Tick(.1, controller.Host);
            controller.View.Open("teach"); controller.View.Refresh(true);
            yield return new WaitForSecondsRealtime(.2f);
            Check(controller.Sim.S.stage == 2 && controller.Sim.DeskOpen("arith")
                && !controller.Sim.Has("arith.pack") && !controller.Sim.Owns("arith")
                && controller.Sim.DeskConditionText(XgCatalog.Desk("arith")) == "",
                "Stage two exposes arithmetic through its real unlock path without a purchased pack");
            SenseStartClickDesk(controller, "arith");
            yield return new WaitForSecondsRealtime(.2f);
            Check(controller.Sim.S.desk == "arith" && ActualDeskGame(controller.View.Teach)!=null && (string)LessonGoalField<object>(controller.View.Teach,"deskGame").GetType().GetProperty("SelectedGame").GetValue(LessonGoalField<object>(controller.View.Teach,"deskGame"))=="dadishu",
                "Actual stage-two hall click enters the arithmetic game");
            yield return new WaitForEndOfFrame();
            SenseStartCapture("sense-start-stage-two-arithmetic");

            // Representative valid old save, built and serialized in memory only. Keep
            // the selected desk, training dataset, existing card and labels on reload.
            var legacy = JsonUtility.FromJson<XgState>(freshJson);
            legacy.desksOpen.Add("arith"); legacy.desk = legacy.sequence.dataset = "arith";
            legacy.labels.Add(new XgLabelCount { dataset = "arith", count = 7 });
            var legacySim = new XgSim(legacy);
            long legacyCard = legacySim.Card("arith").id;
            string legacyCardJson = JsonUtility.ToJson(legacySim.Card("arith"));
            SenseStartLoad(controller, runtime, houseJson, legacySim.S);
            controller.View.Open("teach");
            yield return new WaitForSecondsRealtime(.2f);
            Check(controller.Sim.S.stage == 1 && controller.Sim.S.desk == "arith"
                && controller.Sim.S.sequence.dataset == "arith" && controller.Sim.Labels("arith") == 7
                && controller.Sim.DeskOpen("arith") && controller.Sim.ProgressionDeskAvailable("arith")
                && JsonUtility.ToJson(controller.Sim.Card("arith")) == legacyCardJson,
                "Real in-memory save binding preserves old stage-one arithmetic selection, data, labels and card");
            SenseStartClickDesk(controller, XgMemes.SenseDesk);
            yield return new WaitForSecondsRealtime(.2f);
            SenseStartClickDesk(controller, "arith");
            yield return new WaitForSecondsRealtime(.2f);
            Check(controller.Sim.S.desk == "arith" && ActualDeskGame(controller.View.Teach)!=null && (string)LessonGoalField<object>(controller.View.Teach,"deskGame").GetType().GetProperty("SelectedGame").GetValue(LessonGoalField<object>(controller.View.Teach,"deskGame"))=="dadishu" && controller.Sim.Card("arith").id==legacyCard
                && controller.Sim.Labels("arith") == 7 && controller.Sim.DeskConditionText(XgCatalog.Desk("arith")) == "",
                "Actual hall clicks can leave and re-enter an old unlocked arithmetic desk at stage one");
            yield return new WaitForEndOfFrame();
            SenseStartCapture("sense-start-old-unlocked-arithmetic");
            Check(!runtime.useDiskSave && !runtime.tickEnabled && !controller.enabled,
                "Common-sense scene retains disk-save and background-tick isolation");
        }
        finally
        {
            runtime.SetSimulationForTests(savedHouse);
            controller.View.enabled = savedView; controller.enabled = savedController; runtime.tickEnabled = savedTick;
        }
    }

    static RectTransform SenseStartHallRow(XgLabelPage teach, string id)
        => teach.root.Find("Hall/Viewport/List/Desk " + id) as RectTransform;

    static void SenseStartClickDesk(XingGuangController controller, string id)
    {
        var row = SenseStartHallRow(controller.View.Teach, id);
        Check(row != null && row.gameObject.activeInHierarchy && row.GetComponent<Button>().IsInteractable(),
            "Actual hall contains an interactive row: " + id);
        var source = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var pointer = GrowthRaycast(source, row);
        Check(pointer.pointerCurrentRaycast.gameObject != null
            && pointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(row),
            "Real source canvas can reach the hall row before clicking: " + id);
        Click(row);
        Check(controller.Sim.S.desk == id, "Actual LCD pointer selects the intended desk: " + id);
    }

    static void SenseStartLoad(XingGuangController controller, ChapterOneRuntime runtime, string houseJson, XgState state)
    {
        var house = JsonUtility.FromJson<GameState>(houseJson);
        house.labState = JsonUtility.ToJson(state);
        var previous = controller.Sim;
        runtime.SetSimulationForTests(new ChapterOneSim(house));
        Check(!ReferenceEquals(previous, controller.Sim), "Real runtime save binding creates the scene's replacement simulation");
    }

    static void SenseStartCapture(string name)
    {
        Capture(name); CaptureVisualState(name); DesktopFadeFullScreenshot(name);
    }
}
