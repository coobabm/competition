using System;
using System.Collections;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    static IEnumerator PersonaFeedbackScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgBrainWallpaper brain, XgWallpaperTraining training)
    {
        TrainingSceneCase("Persona: explicit neutral tone board and local reply/content fixtures; actual rating and teaching APIs");
        var sim = controller.Sim; var halo = brain.GetComponentInChildren<XgWallpaperPersona>();
        bool reduced = controller.View.Juice.Reduced; float scale = Time.timeScale;
        try
        {
            sim.S.board = new XgBoardState(); // Replace owner so Core indexes rebuild with this neutral fixture.
            controller.View.Juice.Reduced = false;
            yield return new WaitForSecondsRealtime(1.5f);
            Color before = halo.HaloColor; float warmth = halo.Axes.x, play = halo.Axes.y;
            for (int i = 0; i < 10; i++)
            {
                sim.S.chat.Add(new XgChatLine { from = "ai", text = "好呀，嘿嘿！" });
                Check(sim.RateReply(sim.S.chat.Count - 1, true), "Real praise trains reply tone: " + i);
            }
            float targetWarmth = (float)sim.ActualAxis(0) / 100;
            yield return new WaitForSecondsRealtime(.15f);
            Check(halo.Axes.x > warmth && halo.Axes.x < targetWarmth, "Praise begins a gradual drift instead of snapping the halo");
            yield return new WaitForSecondsRealtime(1.6f); yield return new WaitForEndOfFrame();
            Check(halo.Axes.y > play && halo.HaloColor != before && Mathf.Abs(halo.Axes.x - targetWarmth) < .005f,
                "Actual warmer/playful personality reaches the visible halo");
            TrainingSceneCapture(training, "persona-feedback-praise");
            float praised = halo.Axes.x;
            for (int i = 0; i < 10; i++)
            {
                sim.S.chat.Add(new XgChatLine { from = "ai", text = "好呀，嘿嘿！" });
                Check(sim.RateReply(sim.S.chat.Count - 1, false), "Real dislike weakens reply tone: " + i);
            }
            yield return new WaitForSecondsRealtime(1.7f); yield return new WaitForEndOfFrame();
            Check(halo.Axes.x < praised && Mathf.Abs(halo.Axes.x - (float)sim.ActualAxis(0) / 100) < .005f,
                "Dislike reverses the learned drift without a display-only fake personality");
            TrainingSceneCapture(training, "persona-feedback-dislike");
            sim.S.board = new XgBoardState(); // Replace owner so Core indexes rebuild with this neutral fixture.
            for (int i = 0; i < 10; i++)
            {
                sim.SelectDesk("sense"); var card = sim.Card("sense");
                card.question = "哈哈，逗你玩！"; // Explicit textual fixture, normal evaluation/training path.
                Check(sim.TeachingGuess(card, out bool guess) && sim.TryEvaluateTeachingGuess("sense", card.id, guess,
                    guess == card.truth, controller.Host, out var answer) && answer.correct, "Actual taught-content path learns tone: " + i);
            }
            Check(sim.ActualAxis(1) > 50, "Feeding playful language changes the real learned play axis");
            yield return new WaitForSecondsRealtime(1.7f); yield return new WaitForEndOfFrame();
            TrainingSceneCapture(training, "persona-feedback-content");
            string state = TrainingSceneState(controller, runtime);
            controller.View.Juice.Reduced = true; float phase = halo.Phase;
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.4f);
            Check(halo.Phase == phase, "Reduced holds the halo outline still even at paused simulation time");
            Check(state == TrainingSceneState(controller, runtime), "Rendering alone changes no personality, money or learning state");
            var mesh = halo.canvasRenderer.GetMesh();
            Check(mesh != null && mesh.vertexCount > 0 && mesh.vertexCount <= 4096 && !halo.raycastTarget,
                "Persona halo remains within its mesh budget and never intercepts clicks");
        }
        finally { controller.View.Juice.Reduced = reduced; Time.timeScale = scale; }
    }
}

