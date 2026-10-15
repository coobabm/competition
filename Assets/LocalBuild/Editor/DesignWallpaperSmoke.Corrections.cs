using System;
using System.Collections;
using System.IO;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    public static void RunAnswerCorrections() { Start("answer-correction"); }
    static void StartCorrectionChecks(XingGuangController controller, XgBrainWallpaper brain)
    {
        due = double.PositiveInfinity;
        controller.StartCoroutine(CheckCorrectionFrames(controller, brain));
    }
    static IEnumerator CheckCorrectionFrames(XingGuangController controller, XgBrainWallpaper brain)
    {
        var sequence = CorrectionSteps(controller, brain); Exception failure = null;
        while (true)
        {
            bool more = false; object current = null;
            try { more = sequence.MoveNext(); if (more) current = sequence.Current; }
            catch (Exception error) { failure = error; }
            if (failure != null || !more) break;
            yield return current;
        }
        (sequence as IDisposable)?.Dispose(); Time.timeScale = 1;
        File.WriteAllText(Path.Combine(Output, "result.txt"), failure == null ? "PASS" : "FAIL: " + failure);
        Finish(failure == null ? 0 : 1);
    }
    static IEnumerator CorrectionSteps(XingGuangController controller, XgBrainWallpaper brain)
    {
        AnswerTraceChecks.Run();
        var graphic = brain.GetComponentInChildren<XgWallpaperAnswerPath>();
        Check(graphic != null, "Correction uses the actual installed answer-path graphic");
        controller.enabled = false; controller.Sim.SelectDesk("spam"); controller.View.ShowTab("teach");
        var sim = controller.Sim;
        for (int i = 0; i < 4; i++) { PathCard(sim); TeachCard(sim, controller.Host); }
        yield return new WaitForSecondsRealtime(1.2f);
        var card = PathCard(sim); card.truth = false;
        controller.View.Refresh(true);
        yield return null; yield return new WaitForEndOfFrame();
        Check(sim.TeachingGuess(card, out bool guessed) && guessed, "Prepared card has the real wrong Yes prediction");
        XgAnswerTrace trace = null; Action<XgAnswerTrace> receive = value => trace = value;
        sim.CardAnswered += receive;
        try
        {
            Click(controller.View.Teach.NoButton);
            Check(trace != null && trace.CardId == card.id && trace.Outcome.corrected && trace.Outcome.correct,
                "Actual Guessed wrong mouse action settles exactly this successful correction");
            Check(trace.CorrectionPulls.Count > 0 && AnswerPaths(graphic).Count == 1,
                "Correcting preserves the actual prediction route and captures changed concepts");
            var path = AnswerPaths(graphic)[0];
            Check(PathField<bool>(path, "corrected"), "The corrected card has a return phase");
            string board = JsonUtility.ToJson(sim.S.board);
            float started = PathField<float>(path, "started");
            controller.Window.MinimizeWindow();
            // Natural elapsed time, never rewrite path ages or extend the effect for a screenshot.
            while (Time.unscaledTime - started < XgWallpaperAnswerPath.Duration + .08f) yield return null;
            yield return new WaitForEndOfFrame();
            Check(AnswerPaths(graphic).Count == 1, "Return light survives past the ordinary 0.4-second forward phase");
            Capture("correction-return-desktop");
            while (AnswerPaths(graphic).Count > 0 && Time.unscaledTime - started < 2)
            {
                File.AppendAllText(Path.Combine(Output, "correction-frames.csv"), Time.frameCount + "," + (Time.unscaledTime - started) + "," + AnswerPaths(graphic).Count + "\n");
                yield return null;
            }
            Check(AnswerPaths(graphic).Count == 0 && Time.unscaledTime - started >= 1,
                "Forward 0.4s and reverse 0.6s finish naturally without leaving a route");
            Check(JsonUtility.ToJson(sim.S.board) == board, "Return animation never changes learned weights or strengths");

            var presentation = brain.GetComponent<XgBrainWallpaperPresentation>();
            presentation.SetQuiet(true); controller.View.Juice.Reduced = true;
            yield return new WaitForSecondsRealtime(1.2f);
            card = PathCard(sim); sim.TeachingGuess(card, out guessed); card.truth = !guessed;
            sim.Answer("spam", card.truth, controller.Host);
            Check(trace.Outcome.corrected, "Reduced scenario also corrects a real current prediction");
            started = PathField<float>(AnswerPaths(graphic)[0], "started");
            Time.timeScale = 0;
            while (Time.unscaledTime - started < XgWallpaperAnswerPath.Duration + .12f) yield return null;
            yield return new WaitForEndOfFrame();
            Check(AnswerPaths(graphic).Count == 1 && graphic.canvasRenderer.GetAlpha() <= presentation.LightLevel + .001f,
                "Paused Reduced correction retains a static, Quiet-limited return light");
            Capture("correction-quiet-reduced");
            while (AnswerPaths(graphic).Count > 0 && Time.unscaledTime - started < 2) yield return null;
            Check(AnswerPaths(graphic).Count == 0, "Paused correction clears itself on unscaled time");
            Time.timeScale = 1;
            controller.View.Juice.Reduced = false;

            card = PathCard(sim); sim.TeachingGuess(card, out guessed); card.truth = guessed;
            sim.Answer("spam", !card.truth, controller.Host);
            Check(!trace.Outcome.correct && trace.CorrectionPulls.Count == 0 && !PathField<bool>(AnswerPaths(graphic)[0], "corrected"),
                "An incorrect player judgment gets a forward route, never a purple correction reward");
            yield return new WaitForSecondsRealtime(.5f);
            Check(AnswerPaths(graphic).Count == 0, "Incorrect judgment still uses the original 0.4-second duration");
            Check(true, "CORRECTION_REAL_FRAMES_PASS (visual review required)");
        }
        finally { sim.CardAnswered -= receive; Time.timeScale = 1; }
    }
}
