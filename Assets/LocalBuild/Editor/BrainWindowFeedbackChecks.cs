#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Disposable Edit Mode checks. No scene, save, registry or PlayerPrefs access.</summary>
public static class BrainWindowFeedbackChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires Edit Mode.");
        var gate = new XgBrainReactionGate();
        Require(gate.TryStart(0, "vision") && gate.TryStart(.1f, "sequence") && gate.TryStart(.2f, "tone"), "Three independent onsets are allowed.");
        Require(!gate.TryStart(.3f, "logic") && !gate.TryStart(.99f, "vision"), "A fourth onset within any second is rejected.");
        Require(gate.TryStart(1.01f, "logic"), "An expired onset leaves capacity.");
        gate.Clear();
        Require(gate.TryStart(0, "vision") && !gate.TryStart(.32f, "vision") && gate.TryStart(.34f, "vision"), "A region cannot restart faster than 333ms.");
        gate.Clear(); Require(gate.TryStart(.35f, "vision"), "Switch/reset clears the previous gate history.");
        foreach (var size in new[] { new Vector2(960, 180), new Vector2(240, 720), new Vector2(320, 240) })
        {
            float scale = XgBrainWindow.DrawingScale(size);
            Require(scale > 0 && scale * 1280 <= size.x + .001f && scale * 832 <= size.y + .001f, "The unchanged 1280:832 drawing fits extreme window sizes.");
        }
        Require(XgBoardData.Region("chess") == "sequence" && XgBoardData.DisplayRegion("chess") == "logic", "Chess has a visual reasoning destination without moving saved concepts.");
        GameObject root = null;
        try
        {
            root = new GameObject("Disposable brain feedback checks", typeof(RectTransform)); root.SetActive(false);
            var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(1280, 832);
            var feedback = root.AddComponent<XgBrainWindowFeedback>();
            // True is evaluated before GamePerformance.Low: this fixture does not read ordinary PlayerPrefs.
            feedback.ReducedMotion = () => true;
            feedback.Region("vision", true); feedback.Region("sequence", false); feedback.Gold();
            feedback.Training("logic"); feedback.Sweep("logic"); feedback.Assessment("logic", true);
            feedback.NewRegion("chess"); feedback.Stage(3); feedback.Rebirth(); feedback.Diverged("vision");
            Require(feedback.FeedbackCount == 10 && feedback.LastReaction == "nan", "Every real input retains a static acknowledgement.");
            Require(!feedback.Animating && feedback.AnimationStarts == 0, "Reduced motion never starts an animation.");
            var first = Mesh(feedback);
            typeof(XgBrainWindowFeedback).GetField("began", Private).SetValue(feedback, -1000f);
            var second = Mesh(feedback);
            Require(first.Count == second.Count && first.Count > 0, "Reduced feedback remains visible.");
            for (int i = 0; i < first.Count; i++)
                Require(first[i].position == second[i].position && first[i].color.Equals(second[i].color), "Static mesh is independent of animation time.");
            foreach (string dataset in new[] { "sense", "spam", "poems", "translate", "mnist", "go", "chess", "logic" })
            {
                feedback.Answer(dataset, true);
                Require(feedback.LastSignature == dataset && !feedback.Animating, "A real dataset selects its own static signature: " + dataset);
                var vertices = Mesh(feedback);
                Require(vertices.Count > 0 && vertices.Count < 8192, "Signature geometry is bounded: " + dataset);
                foreach (var vertex in vertices)
                    Require(vertex.position.x >= rect.rect.xMin - .001f && vertex.position.x <= rect.rect.xMax + .001f
                        && vertex.position.y >= rect.rect.yMin - .001f && vertex.position.y <= rect.rect.yMax + .001f,
                        "Signature geometry stays inside the virtual client: " + dataset);
            }
            feedback.Answer("chess", false);
            Require(feedback.LastSignature == "" && feedback.LastReaction == "wrong", "Incorrect answers keep their result marker without a success signature.");
            feedback.ResetFeedback();
            Require(feedback.FeedbackCount == 0 && feedback.LastReaction == "" && !feedback.Animating && Mesh(feedback).Count == 0, "Save rebinding clears all feedback state.");
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
        Debug.Log("BRAIN_WINDOW_FEEDBACK_CHECKS_PASS: rate cap, fit, visual-only chess mapping, eight static signatures and reset. Live UI/events and Player remain separate checks.");
    }
    static List<UIVertex> Mesh(XgBrainWindowFeedback feedback)
    {
        using (var helper = new VertexHelper())
        {
            typeof(XgBrainWindowFeedback).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(VertexHelper) }, null).Invoke(feedback, new object[] { helper });
            var vertices = new List<UIVertex>(); helper.GetUIVertexStream(vertices); return vertices;
        }
    }
    static void Require(bool valid, string detail) { if (!valid) throw new InvalidOperationException(detail); }
}
#endif
