using System;
using System.Collections;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    public static void RunAnswerPaths() { Start("answer-path"); }
    static void TickAnswerPaths(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        if (step != 210) return;
        step = 211; controller.StartCoroutine(CheckAnswerPaths(controller, runtime, brain));
    }
    static IList AnswerPaths(XgWallpaperAnswerPath graphic) => (IList)typeof(XgWallpaperAnswerPath)
        .GetField("paths", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(graphic);
    static T PathField<T>(object path, string key) => (T)path.GetType().GetField(key).GetValue(path);

    static void LogAnswerPathState(XgWallpaperAnswerPath graphic, string context)
    {
        var paths = AnswerPaths(graphic);
        var line = new System.Text.StringBuilder(context).Append("; paths=").Append(paths.Count);
        for (int i = 0; i < paths.Count; i++)
        {
            bool corrected = PathField<bool>(paths[i], "corrected");
            line.Append("; path[").Append(i).Append("] corrected=").Append(corrected)
                .Append(" age=").Append((Time.unscaledTime - PathField<float>(paths[i], "started")).ToString("0.000"))
                .Append(" lifetime=").Append(XgWallpaperAnswerPath.Duration + (corrected ? XgWallpaperAnswerPath.CorrectionDuration : 0));
        }
        Debug.Log("ANSWER_PATH_DIAGNOSTICS: " + line);
        File.AppendAllText(Path.Combine(Output, "answer-path-diagnostics.txt"), line + "\n");
    }

    static IEnumerator CheckAnswerPaths(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        var sequence = AnswerPathSteps(controller, runtime, brain);
        Exception failure = null;
        while (true)
        {
            object current = null;
            bool more = false;
            try { more = sequence.MoveNext(); if (more) current = sequence.Current; }
            catch (Exception error) { failure = error; }
            if (failure != null || !more) break;
            yield return current;
        }
        (sequence as IDisposable)?.Dispose(); Time.timeScale = 1;
        File.WriteAllText(Path.Combine(Output, "result.txt"), failure == null ? "PASS" : "FAIL: " + failure);
        Finish(failure == null ? 0 : 1);
    }

    static XgCard PathCard(XgSim sim)
    {
        var card = sim.Card("spam");
        card.seed = 9137; card.truth = true; card.question = "恭喜您中奖，请点击领取";
        return card;
    }

    static IEnumerator AnswerPathSteps(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        NamingChecks.Validate();
        // Twelve-question prerequisites are exercised by the shared purchase fixture.
        AnswerTraceChecks.Run();
        var graphic = brain.GetComponentInChildren<XgWallpaperAnswerPath>();
        Check(graphic != null && !graphic.raycastTarget && graphic.GetComponent<CanvasRenderer>() != null,
            "Answer path is mounted in the actual non-editable purchased brain window");
        controller.enabled = false;
        controller.Sim.SelectDesk("spam"); // This regression deliberately visits the SMS desk after the default lesson changed.
        controller.View.ShowTab("teach");
        yield return new WaitForSecondsRealtime(1);
        var sim = controller.Sim;
        for (int i = 0; i < 4; i++) { PathCard(sim); TeachCard(sim, controller.Host); }
        yield return new WaitForSecondsRealtime(.8f);
        var card = PathCard(sim);
        sim.Board.Predict(XgBoardData.FromCard(card), sim.Knobs(sim.Run(XgCatalog.Dataset(card.dataset).track)),
            out _, out var prepared);
        LogAnswerPathState(graphic, "After original 0.8s preparation wait; used=" + prepared.UsedConcepts.Count
            + "; guessed=" + prepared.Guessed + "; answerYes=" + prepared.AnswerYes);
        // Preparation can contain a successful correction: its natural lifetime is 0.4 + 0.6 seconds.
        // Keep the old wait above as evidence, then isolate the next answer without clearing or aging any path.
        double clearDeadline = Time.realtimeSinceStartupAsDouble + 2;
        while (AnswerPaths(graphic).Count > 0 && Time.realtimeSinceStartupAsDouble < clearDeadline) yield return null;
        LogAnswerPathState(graphic, "After natural preparation cleanup");
        Check(AnswerPaths(graphic).Count == 0, "All preparation paths expire naturally within the bounded wait");
        controller.View.Refresh(true);
        yield return null;
        yield return new WaitForEndOfFrame();
        XgAnswerTrace trace = null;
        Action<XgAnswerTrace> observe = value => trace = value;
        sim.CardAnswered += observe;
        try
        {
            Check(sim.TeachingGuess(card, out bool guess), "Real prepared card has an actual prediction");
            string board = JsonUtility.ToJson(sim.S.board);
            // Deliberately misjudge a known card: the readout must still appear without a success halo.
            Click(card.truth ? controller.View.Teach.NoButton : controller.View.Teach.YesButton);
            Check(trace != null && !trace.Outcome.correct && trace.CardId == card.id && trace.Prediction != null,
                "Actual wrong statement judgment publishes the original card's local prediction once");
            LogAnswerPathState(graphic, "After the single wrong UI evaluation; used=" + trace.Prediction.UsedConcepts.Count
                + "; guessed=" + trace.Prediction.Guessed + "; corrected=" + trace.Outcome.corrected);
            Check(trace.Prediction.UsedConcepts.Count > 0,
                "The answered card uses existing learned concepts; used=" + trace.Prediction.UsedConcepts.Count);
            Check(AnswerPaths(graphic).Count == 1,
                "The single settled answer adds exactly one path after preparation cleanup; paths=" + AnswerPaths(graphic).Count);
            var path = AnswerPaths(graphic)[0];
            var ids = PathField<int[]>(path, "concepts");
            foreach (int id in ids)
            {
                bool real = false;
                foreach (var used in trace.Prediction.UsedConcepts) if (used.Id == id) real = true;
                Check(real, "Path cell belongs to this prediction: " + id);
            }
            Check(ids.Length > 0 && PathField<System.Collections.Generic.List<Vector2>>(path, "points").Count == ids.Length + 2,
                "Path has input, real concepts and output with no fabricated intermediate cells");
            CheckAnswerPathEdges(graphic);
            yield return new WaitForEndOfFrame();
            Capture("answer-path-default-window");
            yield return new WaitForSecondsRealtime(.5f);
            Check(AnswerPaths(graphic).Count == 0 && JsonUtility.ToJson(sim.S.board) == board,
                "Readout expires naturally without changing the learned board");

            controller.Window.MinimizeWindow();
            yield return new WaitForSecondsRealtime(1);
            PathCard(sim); sim.Answer("spam", false, controller.Host);
            Time.timeScale = 0;
            float started = Time.unscaledTime;
            int frames = 0;
            while (AnswerPaths(graphic).Count > 0 && Time.unscaledTime - started < 2)
            {
                yield return new WaitForEndOfFrame();
                File.AppendAllText(Path.Combine(Output, "answer-path-frames.csv"), Time.frameCount + "," + (Time.unscaledTime - started) + "," + AnswerPaths(graphic).Count + "\n");
                if (++frames == 2) Capture("answer-path-on-desktop");
            }
            Check(AnswerPaths(graphic).Count == 0 && Time.unscaledTime - started >= XgWallpaperAnswerPath.Duration,
                "The 0.4-second readout finishes on unscaled time while the simulation is paused");
            Time.timeScale = 1;
            var presentation = brain.GetComponent<XgBrainWallpaperPresentation>();
            presentation.SetQuiet(true); controller.View.Juice.Reduced = true;
            yield return new WaitForSecondsRealtime(1);
            PathCard(sim); sim.Answer("spam", false, controller.Host);
            Check(Mathf.Abs(graphic.canvasRenderer.GetAlpha() - presentation.LightLevel) < .001f,
                "First answer after idle immediately adopts current Quiet brightness");
            yield return new WaitForEndOfFrame();
            Check(graphic.canvasRenderer.GetAlpha() <= presentation.LightLevel + .001f,
                "Quiet mode limits the answer light while reduced effects retain a static route");
            Capture("answer-path-quiet-reduced");
            yield return new WaitForSecondsRealtime(.5f);
            controller.View.Juice.Reduced = false;
            for (int i = 0; i < 8; i++) { PathCard(sim); sim.Answer("spam", false, controller.Host); }
            Check(AnswerPaths(graphic).Count == 4, "Rapid answers keep a bounded set of current paths");
            graphic.enabled = false;
            Check(AnswerPaths(graphic).Count == 0, "Disable clears all paths and unsubscribes");
            sim.Answer("spam", false, controller.Host);
            Check(AnswerPaths(graphic).Count == 0, "Hidden presentation receives no answer events");
            graphic.enabled = true;
            Check(AnswerPaths(graphic).Count == 0, "Re-enable does not replay history");
            sim.Answer("spam", false, controller.Host);
            Check(AnswerPaths(graphic).Count == 1, "Re-enable observes new answers once");
            runtime.SetSimulationForTests(new ChapterOneSim());
            Check(AnswerPaths(graphic).Count == 0, "Changing the save synchronously clears the old answer path");
            sim.Answer("spam", false, controller.Host);
            Check(AnswerPaths(graphic).Count == 0, "The old simulation cannot illuminate the replacement save");
        }
        finally { sim.CardAnswered -= observe; }
    }
}
