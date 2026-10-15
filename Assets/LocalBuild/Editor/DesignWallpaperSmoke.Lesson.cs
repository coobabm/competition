using System;
using System.Collections;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using HongmengOS.Aero2010;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    static IEnumerator CheckLessonPresentation(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        var checks = LessonPresentationSteps(controller, runtime, brain);
        while (true)
        {
            object current;
            try
            {
                if (!checks.MoveNext()) break;
                current = checks.Current;
            }
            catch (Exception error)
            {
                Time.timeScale = 1;
                File.WriteAllText(Path.Combine(Output, "result.txt"), "FAIL: " + error);
                Finish(1); yield break;
            }
            yield return current;
        }
        File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0);
    }

    static float LessonPulse(XgWallpaperConcepts graphic) => (float)typeof(XgWallpaperConcepts)
        .GetProperty("LessonPulse", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(graphic);

    static void TeachCard(XgSim sim, IXgHost host)
    {
        var card = sim.Card("spam");
        Check(sim.TeachingGuess(card, out bool guess), "Actual card supplies a teaching guess");
        Check(sim.TryEvaluateTeachingGuess("spam", card.id, guess, guess == card.truth, host, out var answer)
            && answer.correct, "Actual lesson learns successfully");
    }

    static IEnumerator LessonPresentationSteps(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        var graphic = brain.GetComponentInChildren<XgWallpaperConcepts>();
        var presentation = brain.GetComponent<XgBrainWallpaperPresentation>();
        Check(graphic != null && LessonPulse(graphic) == 0, "Lesson starts with no glow");
        controller.enabled = false; // Keep the isolated lesson deterministic; wallpaper keeps ticking.
        controller.Window.MinimizeWindow();
        yield return new WaitForSecondsRealtime(1.5f); // Setup may reopen the window after the earlier minimize.
        var camera = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera;
        var rect = graphic.rectTransform.rect;
        float scale = Mathf.Min(rect.width / 1920, rect.height / 1080);
        var probe = rect.min + Vector2.Scale(rect.size, XgWallpaperConcepts.RegionCenter(1)) + new Vector2(48 * scale, 0);
        var screenPoint = camera.WorldToScreenPoint(graphic.transform.TransformPoint(probe));
        yield return new WaitForEndOfFrame();
        var baseline = Pixel(screenPoint);
        var oldSim = controller.Sim;
        var card = oldSim.Card("spam");
        TeachCard(oldSim, controller.Host);
        Check(LessonPulse(graphic) > .99f && graphic.Cells.Count > 0, "Successful first lesson updates learned cells in the same frame");
        Check((int)typeof(XgWallpaperConcepts).GetField("lessonRegion", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(graphic) == 1,
            "Original SMS card lights the reading region");
        yield return new WaitForEndOfFrame();
        var bright = Pixel(screenPoint);
        Check(bright.g > baseline.g + .002f, "Real rendered reading halo visibly brightens");
        Capture("lesson-pulse");
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "lesson-pulse-full.png"));
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.8f);
        yield return new WaitForEndOfFrame();
        Check(LessonPulse(graphic) == 0, "Glow naturally finishes while simulation time is paused");
        var settled = Pixel(screenPoint);
        File.AppendAllText(Path.Combine(Output, "checks.txt"), "Halo pixels: " + baseline + " -> " + bright + " -> " + settled + "\n");
        Capture("lesson-settled");
        Check(settled.g < bright.g - .002f, "Rendered halo returns after the lesson");
        Time.timeScale = 1;
        Check(oldSim.TeachingGuess(oldSim.Card("spam"), out bool currentGuess), "Next card remains teachable");
        var nextCard = oldSim.Card("spam");
        Check(oldSim.TryEvaluateTeachingGuess("spam", nextCard.id, currentGuess, currentGuess != nextCard.truth,
            controller.Host, out var wrong) && !wrong.correct && LessonPulse(graphic) == 0, "Wrong player evaluation does not flash success");
        TeachCard(oldSim, controller.Host);
        graphic.enabled = false;
        Check(LessonPulse(graphic) == 0, "Disabling the wallpaper clears active glow");
        TeachCard(oldSim, controller.Host);
        Check(LessonPulse(graphic) == 0, "Disabled wallpaper receives no new glow");
        graphic.enabled = true;
        Check(LessonPulse(graphic) == 0, "Re-enable does not replay old events");
        TeachCard(oldSim, controller.Host);
        Check(LessonPulse(graphic) > .99f, "Re-enable binds current lessons");
        runtime.SetSimulationForTests(new ChapterOneSim());
        Check(LessonPulse(graphic) == 0 && !ReferenceEquals(oldSim, controller.Sim), "Replacing the in-memory save synchronously clears old lesson");
        TeachCard(oldSim, controller.Host);
        Check(LessonPulse(graphic) == 0, "Detached old simulation cannot light a new save");
        runtime.CompletePrologue(new PrologueProfile { name = "小星", self = "我", callMe = "你", personality = "好奇" });
        yield return new WaitForSecondsRealtime(4);
        presentation.SetQuiet(true);
        yield return new WaitForSecondsRealtime(1);
        float quietLight = presentation.LightLevel;
        TeachCard(controller.Sim, controller.Host);
        Check(LessonPulse(graphic) > .99f && graphic.canvasRenderer.GetAlpha() <= quietLight + .001f,
            "New save responds and quiet mode retains its brightness limit");
        yield return new WaitForEndOfFrame();
        Capture("lesson-quiet-pulse");
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "lesson-quiet-pulse-full.png"));
        yield return new WaitForSecondsRealtime(.8f);
        Check(LessonPulse(graphic) == 0, "New-save glow also clears naturally");
        UnityEngine.Object.Destroy(graphic.gameObject);
        yield return null;
        TeachCard(controller.Sim, controller.Host);
        Check(true, "Destroying presentation leaves teaching functional without stale callback");
    }
}
