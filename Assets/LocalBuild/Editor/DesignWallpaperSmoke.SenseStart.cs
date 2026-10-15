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
        DesignFirstLessonChecks.Run();
        PlayabilityChecks.Run();
        Check(runtime != null && runtime.TestMode && !runtime.useDiskSave && !runtime.Sim.InPrologue,
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
            var card = sim.Card(XgMemes.SenseDesk);
            Check(sim.S.stage == 1 && sim.S.desk == XgMemes.SenseDesk
                && sim.S.sequence.dataset == XgMemes.SenseDesk,
                "Real new-game teaching selection and stored sequence training data are common sense");
            Check(controller.View.Tab == "teach" && teach.Paper.gameObject.activeInHierarchy
                && teach.ShownCardId == card.id && card.dataset == XgMemes.SenseDesk,
                "Actual teaching paper presents the unmodified natural common-sense card");
            Check(!sim.DeskOpen("arith") && !sim.ProgressionDeskAvailable("arith"),
                "Stage-one scene keeps arithmetic unavailable");
            // Existing teaching-mode policy omits locked desks before stage three. Do not
            // change teachMode or invent a grey row merely to make this test click one.
            Check(SenseStartHallRow(teach, "arith") == null,
                "Stage-one teaching hall offers no arithmetic row under its existing locked-desk policy");
            yield return new WaitForEndOfFrame();
            SenseStartCapture("sense-start-default-stage-one");

            // Earn the four samples through real teaching input; do not seed labels or
            // replace cards. This exercises the companion's actual dataset/track handoff.
            for (int i = 0; sim.Samples(XgMemes.SenseDesk) < XgCatalog.SamplesToTrain && i < XgCatalog.SamplesToTrain + 1; i++)
            {
                card = sim.Card(XgMemes.SenseDesk);
                Check(sim.TeachingGuess(card, out bool guess), "Natural sense card has a production model guess");
                double before = sim.Labels(XgMemes.SenseDesk);
                Click(card.truth ? teach.YesButton : teach.NoButton);
                Check(sim.Labels(XgMemes.SenseDesk) == before + 1,
                    "Actual teaching pointer contributes exactly one common-sense sample");
                yield return new WaitForSecondsRealtime(.35f);
            }
            var companion = controller.View.GetComponentInChildren<XgTeachCompanion>(true);
            var train = SenseStartField<XgBtn>(companion, "train");
            Check(sim.Samples(XgMemes.SenseDesk) >= XgCatalog.SamplesToTrain
                && train.rt.gameObject.activeInHierarchy && train.button.IsInteractable(),
                "Teaching common sense enables the real companion training action");
            Click(train.rt);
            yield return new WaitForSecondsRealtime(.2f);
            Check(controller.View.Tab == "train" && sim.SelectedTrack == XgTrack.Sequence
                && sim.Selected.dataset == XgMemes.SenseDesk && sim.S.sequence.epoch == 0,
                "Actual companion click opens sequence training on sense without starting an epoch");
            var numbers = SenseStartField<TMP_Text>(controller.View.Train, "numbers");
            var dataset = XgCatalog.Dataset(XgMemes.SenseDesk);
            Check(numbers.gameObject.activeInHierarchy && numbers.text.Contains(sim.English ? dataset.nameEn : dataset.name),
                "Rendered training summary names the same common-sense dataset");
            var learning = SenseStartField<TMP_Text>(controller.View.Train, "learningText");
            var fixedSense = XgBoardData.Make(XgMemes.SenseDesk, 9137, 1, 20160524);
            string comparison = fixedSense.source.question;
            bool authored = false;
            foreach (var phrase in XgMemes.SenseBank)
                authored |= phrase.text == comparison && phrase.yes == fixedSense.truth;
            Check(authored && learning.gameObject.activeInHierarchy
                && learning.text == sim.CheckupText(dataset: XgMemes.SenseDesk)
                && learning.text.Contains(comparison) && !learning.text.Contains("中奖"),
                "Visible learning comparison shows the real common-sense bank question for the selected dataset");
            XgCheckup baseline = null;
            foreach (var checkup in sim.S.checkups)
                if (checkup.dataset == XgMemes.SenseDesk && !checkup.taught) baseline = checkup;
            Check(baseline != null && baseline.assessment == 0 && baseline.question == comparison
                && learning.text.Contains(" → " + baseline.answer),
                "Visible learning comparison renders the stored pre-training model answer to that same sense question");
            string comparisonState = JsonUtility.ToJson(sim.S);
            controller.View.Refresh(true);
            Check(JsonUtility.ToJson(sim.S) == comparisonState,
                "Refreshing the real training comparison preserves learning and saved state");
            yield return new WaitForEndOfFrame();
            SenseStartCapture("sense-start-training-handoff");

            // Stage two is an explicit boundary fixture, not a claim of playing through
            // the chapter. Production loading and Tick perform the real desk unlock.
            var second = JsonUtility.FromJson<XgState>(freshJson); second.stage = 2;
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
            Check(controller.Sim.S.desk == "arith" && controller.View.Teach.ShownCardId == controller.Sim.Card("arith").id,
                "Actual stage-two hall click enters a generated arithmetic card");
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
            Check(controller.Sim.S.desk == "arith" && controller.View.Teach.ShownCardId == legacyCard
                && controller.Sim.Labels("arith") == 7 && controller.Sim.DeskConditionText(XgCatalog.Desk("arith")) == "",
                "Actual hall clicks can leave and re-enter an old unlocked arithmetic desk at stage one");
            yield return new WaitForEndOfFrame();
            SenseStartCapture("sense-start-old-unlocked-arithmetic");
            Check(runtime.TestMode && !runtime.useDiskSave && !runtime.tickEnabled && !controller.enabled,
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
