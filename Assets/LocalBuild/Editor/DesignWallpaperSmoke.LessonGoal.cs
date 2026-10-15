using System;
using System.Collections;
using System.IO;
using System.Reflection;
using HongmengOS.Aero2010;
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
    [MenuItem("Tools/Design Pass/Four Card Lesson Goal Scene Smoke")]
    public static void RunLessonGoal() { Start("lesson-goal"); }

    static void TickLessonGoal(XingGuangController controller, ChapterOneRuntime runtime)
    {
        if (step != 250) return;
        step = 251;
        controller.StartCoroutine(CheckLessonGoal(controller, runtime));
    }

    static IEnumerator CheckLessonGoal(XingGuangController controller, ChapterOneRuntime runtime)
    {
        var checks = LessonGoalSteps(controller, runtime);
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

    static T LessonGoalField<T>(object owner, string field) => (T)owner.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

    // Shared with the existing real-setup first-minute checks; it does not bypass setup,
    // change focus timing, award samples, or invoke button callbacks directly.
    static void CheckLessonGoalState(XingGuangController controller, int samples)
    {
        var sim = controller.Sim;
        var page = controller.View.Teach;
        var goal = LessonGoalField<TMP_Text>(page, "lessonProgress");
        var companion = controller.View.GetComponentInChildren<XgTeachCompanion>(true);
        var train = LessonGoalField<XgBtn>(companion, "train");
        string before = JsonUtility.ToJson(sim.S);
        int count = Math.Min(samples, XgCatalog.SamplesToTrain);
        Check(sim.Samples(sim.S.desk) == samples && goal.isActiveAndEnabled
            && goal.text == count + "/" + XgCatalog.SamplesToTrain,
            "Real card progress shows only the matching sample goal: " + count + "/4");
        Check(!LessonGoalField<TMP_Text>(page, "meta").gameObject.activeInHierarchy
            && !LessonGoalField<TMP_Text>(page, "qno").gameObject.activeInHierarchy,
            "The goal row replaces category, stars and question number during the first round");
        goal.ForceMeshUpdate();
        Check(goal.fontSize >= 16 && !goal.isTextOverflowing && goal.textInfo.lineCount == 1,
            "The card goal is readable on one line without overflow");
        var goalCorners = new Vector3[4]; var paperCorners = new Vector3[4];
        goal.rectTransform.GetWorldCorners(goalCorners); page.Paper.GetWorldCorners(paperCorners);
        var space = page.Paper.parent;
        Check(space.InverseTransformPoint(goalCorners[0]).y >= space.InverseTransformPoint(paperCorners[1]).y,
            "The actual progress rectangle lies above the question card");
        bool ready = samples >= XgCatalog.SamplesToTrain;
        Check(train.rt.gameObject.activeInHierarchy == ready && train.button.IsInteractable() == ready,
            ready ? "The actual training action is visible and clickable at four examples"
                : "The training action is absent and unavailable below four examples");
        Check(JsonUtility.ToJson(sim.S) == before, "Reading the visible goal never changes the saved sample or learning state");
    }

    static IEnumerator LessonGoalSteps(XingGuangController controller, ChapterOneRuntime runtime)
    {
        Check(runtime != null && runtime.TestMode && !runtime.useDiskSave && !runtime.Sim.InPrologue,
            "Lesson-goal scene uses the isolated post-setup fixture");
        var savedHouse = runtime.Sim;
        string houseJson = JsonUtility.ToJson(savedHouse.S);
        bool savedTick = runtime.tickEnabled, savedController = controller.enabled, savedView = controller.View.enabled;
        bool savedEnglish = controller.Sim.English;
        IDisposable locale = null;
        try
        {
            runtime.tickEnabled = false; controller.enabled = false; controller.View.enabled = true;
            controller.View.Open("teach");
            yield return new WaitForSecondsRealtime(.2f);
            var sim = controller.Sim;
            string freshJson = JsonUtility.ToJson(sim.S);
            Check(sim.S.stage == 1 && sim.S.desk == XgMemes.SenseDesk && sim.S.epochs == 0,
                "Goal begins on the real default common-sense desk before its first epoch");
            CheckLessonGoalState(controller, 0);
            yield return new WaitForEndOfFrame();
            LessonGoalCapture("lesson-goal-0");

            // A misjudgment must not count as a learned sample. Submit the wrong evaluation
            // through the real UI; never change the card's truth or inject training labels.
            var card = sim.Card(XgMemes.SenseDesk);
            Check(sim.TeachingGuess(card, out bool guess), "The natural first question supplies an actual model guess");
            Click(card.truth ? controller.View.Teach.NoButton : controller.View.Teach.YesButton);
            yield return new WaitForSecondsRealtime(.2f);
            Check(sim.S.handWrong == 1 && sim.Labels(XgMemes.SenseDesk) == 0,
                "A real incorrect judgment leaves the sample goal at zero");
            CheckLessonGoalState(controller, 0);

            for (int count = 1; count <= XgCatalog.SamplesToTrain; count++)
            {
                card = sim.Card(XgMemes.SenseDesk);
                Check(sim.TeachingGuess(card, out guess), "Next natural sense card has a production model guess");
                Click(card.truth ? controller.View.Teach.YesButton : controller.View.Teach.NoButton);
                yield return new WaitForSecondsRealtime(.2f);
                Check(controller.View.Tab == "teach" && sim.S.epochs == 0,
                    "Completing an example neither navigates away nor starts training automatically");
                CheckLessonGoalState(controller, count);
                if (OpeningSequence.FirstLessonActive) CheckFirstMinuteVisibleObjectives(controller);
                yield return new WaitForEndOfFrame();
                LessonGoalCapture("lesson-goal-" + count);
            }

            // Check the previously hidden action's actual English glyphs when it becomes
            // visible; the existing English first-card scene still verifies its hidden state.
            locale = GameText.Bind(() => GameText.English);
            sim.English = true; GameText.PublishChanged(); controller.View.Refresh(true);
            yield return new WaitForSecondsRealtime(.2f);
            Check(GameText.IsEnglish && Lang.English && sim.English, "The completed goal uses the live English locale");
            CheckLessonGoalState(controller, 4);
            var companion = controller.View.GetComponentInChildren<XgTeachCompanion>(true);
            var train = LessonGoalField<XgBtn>(companion, "train");
            Check(train.label.text == "Train →" || train.label.text == "Train together →",
                "The newly visible training action has its actual English label");
            CheckFirstMinuteEnglishTextFits(train.label, "English completed-goal training button");
            yield return new WaitForEndOfFrame();
            LessonGoalCapture("lesson-goal-4-english");
            var source = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
            var pointer = GrowthRaycast(source, train.rt);
            Check(pointer.pointerCurrentRaycast.gameObject != null
                && pointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(train.rt),
                "The real LCD source can reach the newly visible training button");
            Click(train.rt);
            Check(controller.View.Tab == "train" && sim.SelectedTrack == XgTrack.Sequence
                && sim.Selected.dataset == XgMemes.SenseDesk && sim.S.epochs == 0,
                "Actual goal-button input opens training on the same sense dataset without starting an epoch");

            // Explicit old-save boundary fixtures use in-memory binding only. Progress is
            // derived from existing samples rather than a new saved counter or click total.
            foreach (int samples in new[] { 2, 7 })
            {
                var state = JsonUtility.FromJson<XgState>(freshJson);
                state.labels.Add(new XgLabelCount { dataset = XgMemes.SenseDesk, count = samples });
                var house = JsonUtility.FromJson<GameState>(houseJson);
                house.labState = JsonUtility.ToJson(state);
                runtime.SetSimulationForTests(new ChapterOneSim(house));
                controller.Sim.English = true;
                controller.View.Open("teach"); controller.View.Refresh(true);
                yield return new WaitForSecondsRealtime(.2f);
                CheckLessonGoalState(controller, samples);
                string before = JsonUtility.ToJson(controller.Sim.S);
                controller.View.Refresh(true);
                Check(JsonUtility.ToJson(controller.Sim.S) == before,
                    "Refreshing an existing save preserves its samples and learning: " + samples);
                yield return new WaitForEndOfFrame();
                LessonGoalCapture("lesson-goal-existing-" + samples);
            }
            var trained = controller.Sim;
            trained.S.epochs = 1; // Explicit already-trained boundary, not a simulated epoch.
            controller.View.Refresh(true);
            Check(!LessonGoalField<TMP_Text>(controller.View.Teach, "lessonProgress").gameObject.activeInHierarchy
                && LessonGoalField<TMP_Text>(controller.View.Teach, "meta").gameObject.activeInHierarchy
                && LessonGoalField<TMP_Text>(controller.View.Teach, "qno").gameObject.activeInHierarchy,
                "An already-trained save returns to the established card metadata instead of repeating the first goal");
            Check(runtime.TestMode && !runtime.useDiskSave && !runtime.tickEnabled && !controller.enabled,
                "Goal acceptance keeps disk saves and background simulation isolated");
        }
        finally
        {
            locale?.Dispose(); GameText.PublishChanged();
            runtime.SetSimulationForTests(savedHouse); controller.Sim.English = savedEnglish;
            controller.View.enabled = savedView; controller.enabled = savedController; runtime.tickEnabled = savedTick;
        }
    }

    static void LessonGoalCapture(string name)
    {
        Capture(name); CaptureVisualState(name); DesktopFadeFullScreenshot(name);
    }
}
