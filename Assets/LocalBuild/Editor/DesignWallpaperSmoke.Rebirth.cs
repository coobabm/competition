using System;
using System.Collections;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    static IEnumerator RebirthScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgBrainWallpaper brain, XgWallpaperTraining training)
    {
        TrainingSceneCase("Ability rebirth: explicit P/D threshold fixture, real ConfirmNextStage event, no raised/fabricated presentation event");
        var rebirth = brain.GetComponent<XgWallpaperRebirth>(); var sim = controller.Sim;
        bool reduced = controller.View.Juice.Reduced; float scale = Time.timeScale;
        Check(rebirth != null && !rebirth.Showing, "Loading a completed setup does not replay ability rebirth");
        try
        {
            controller.View.Juice.Reduced = false; Time.timeScale = 1;
            controller.Window.OpenWindow(); yield return new WaitForSecondsRealtime(1);
            var windowGroup = controller.Window.GetComponent<CanvasGroup>();
            float windowAlpha = windowGroup != null ? windowGroup.alpha : 1;
            Check(windowGroup != null && windowAlpha > .9f && XgBrainWallpaperPresentation.IsVisible(controller.Window.windowContainer),
                "Actual AI window finishes opening before the simulation is paused");
            Time.timeScale = 0;
            yield return new WaitForEndOfFrame(); TrainingSceneCapture(training, "ability-rebirth-window-before");
            sim.S.trainedParamsK = 100000;
            var samples = sim.S.labels.Find(value => value.dataset == "sense");
            if (samples == null) sim.S.labels.Add(new XgLabelCount { dataset = "sense", count = 1000000 });
            else samples.count = 1000000; // Existing teaching checks may already have a row; never duplicate it.
            sim.S.stageEpochs = 1;
            int ability = sim.NextAbility, plays = rebirth.Plays;
            Check(ability > 1 && sim.StageReady && sim.ConfirmNextStage(), "Real progression confirmation emits the next ability event");
            float deadline = Time.unscaledTime + 12;
            while (!rebirth.Showing && Time.unscaledTime < deadline) yield return null;
            while (rebirth.Showing && rebirth.Age < .22f) yield return null;
            yield return new WaitForEndOfFrame();
            Check(rebirth.Showing && rebirth.Plays == plays + 1 && rebirth.ShownAbility == ability && rebirth.Age < .3f
                && rebirth.RevealRadius == 0, "Ability starts with a genuine 0.3 second blackout");
            Check(rebirth.WindowFactor == .4f && XgWallpaperRebirth.Playing, "Windows dim to forty percent while story waits for rebirth");
            var mask = brain.GetComponentInChildren<XgRebirthMask>();
            Check(mask.GetComponent<CanvasRenderer>() != null && mask.canvasRenderer.GetMesh().vertexCount >= 4,
                "Blackout has an actual rendered opaque mesh, not just a timer");
            if (windowGroup != null) Check(Mathf.Abs(windowGroup.alpha - windowAlpha * .4f) < .001f,
                "Actual open AI window is faded without adding a duplicate CanvasGroup");
            TrainingSceneCapture(training, "ability-rebirth-dark");
            while (rebirth.Age < .65f) yield return null;
            yield return new WaitForEndOfFrame();
            Check(rebirth.RevealRadius > 0 && rebirth.RevealRadius < 1.3f, "Brain relights outwards through an intermediate radius");
            TrainingSceneCapture(training, "ability-rebirth-growing");
            while (rebirth.Age < 1.6f) yield return null;
            yield return new WaitForEndOfFrame();
            Check(rebirth.RevealRadius >= 1.29f && brain.transform.Find("Ability Rebirth Title").GetComponent<TMPro.TMP_Text>().text
                == XgSim.AbilityName(ability, LingGuangV05.Runtime.GameText.IsEnglish), "New ability name appears over the fully relit brain");
            TrainingSceneCapture(training, "ability-rebirth-title");
            while (rebirth.Showing) yield return null;
            yield return new WaitForEndOfFrame();
            Check(rebirth.WindowFactor == 1 && !XgWallpaperRebirth.Playing && sim.HasAbility(ability),
                "Rebirth finishes on unscaled time, restores windows and keeps the actual new ability");
            if (windowGroup != null) Check(Mathf.Abs(windowGroup.alpha - windowAlpha) < .001f, "Actual AI window returns to its pre-effect alpha");
            TrainingSceneCapture(training, "ability-rebirth-restored");
            controller.View.Juice.Reduced = true; sim.S.stageEpochs = 1;
            Check(sim.StageReady && sim.ConfirmNextStage(), "Second real ability exercises Reduced mode");
            deadline = Time.unscaledTime + 12;
            while (!rebirth.Showing && Time.unscaledTime < deadline) yield return null;
            yield return new WaitForEndOfFrame();
            Check(rebirth.Showing && brain.GetComponentInChildren<XgRebirthMask>().Uniform,
                "Reduced uses a uniform fade rather than a travelling reveal");
            rebirth.enabled = false; yield return null;
            Check(!rebirth.Showing && rebirth.WindowFactor == 1 && !XgWallpaperRebirth.Playing,
                "Disabling mid-effect restores all owned fades and releases story waiting");
            rebirth.enabled = true; yield return null;
            Check(!rebirth.Showing, "Re-enable does not replay already learned abilities");
        }
        finally { rebirth.enabled = true; controller.View.Juice.Reduced = reduced; Time.timeScale = scale; }
    }
}
