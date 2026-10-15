using System;
using System.Collections;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;

// Candidate verification only, outside Assets. Integrator may call Run in an isolated
// Unity editor session after merging changes.patch. No controllers, saves, or audio Init.
// Deterministic synchronous probes of production methods, NOT actual frame/visual tests.
public static class ReducedSwitchChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Run()
    {
        var root = new GameObject("ReducedSwitchChecks", typeof(RectTransform));
        try
        {
            var host = root.AddComponent<XgJuice>();
            var body = new GameObject("Button", typeof(RectTransform));
            body.transform.SetParent(root.transform, false);
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(body.transform, false);
            var label = (RectTransform)labelGo.transform;
            var button = body.AddComponent<XgButtonPressFeedback>();
            button.Label = label;
            var restScale = new Vector3(1.2f, .9f, 1.1f);
            var restPos = new Vector2(7, -3);
            label.localScale = restScale;
            label.anchoredPosition = restPos;
            host.Knock((RectTransform)body.transform, .2f, new Vector2(8, 0));
            object business = Punches(host)[body.transform];

            // Early compression, overshoot, and nearly-finished press.
            foreach (float age in new[] { .02f, .11f, .175f })
            {
                host.Reduced = false;
                Call(button, "PressLabel");
                var fx = label.GetComponent<XgJuice>();
                object punch = Punches(fx)[label];
                Check((float)Get(punch, "amp") == .045f &&
                    (Vector2)Get(punch, "dir") == Vector2.down &&
                    (float)Get(punch, "life") == .18f, "Normal parameters changed");
                Sample(fx, punch, age);
                Vector3 atScale = label.localScale;
                Vector2 atPos = label.anchoredPosition;
                host.Reduced = true;
                // Isolate mode observation from lifetime expiration; frame ordering is
                // covered separately by the integration checklist, not this probe.
                Set(button, "elapsed", 0f);
                Call(button, "LateUpdate");
                Check((bool)Get(punch, "settling"), "Live enable did not settle");
                Check(label.localScale == atScale && label.anchoredPosition == atPos,
                    "Switch registration snapped the current pose");
                float life = (float)Get(punch, "life");
                Check(life > 0 && life <= .060001f && life <= .18f - age + .000001f,
                    "Quiet return extends the allowed lifetime");

                Sample(fx, punch, life * .25f);
                float scaleDistance = Vector3.Distance(label.localScale, restScale);
                float posDistance = Vector2.Distance(label.anchoredPosition, restPos);
                float previousAge = (float)Get(punch, "t");
                host.Reduced = false;
                Set(button, "elapsed", 0f);
                Call(button, "LateUpdate");
                host.Reduced = true;
                Set(button, "elapsed", 0f);
                Call(button, "LateUpdate");
                Check((float)Get(punch, "t") == previousAge &&
                    (float)Get(punch, "life") == life, "Toggling replayed the return");
                Sample(fx, punch, life * .75f);
                Check(Vector3.Distance(label.localScale, restScale) <= scaleDistance + .000001f &&
                    Vector2.Distance(label.anchoredPosition, restPos) <= posDistance + .000001f,
                    "Return increased deformation");
                Sample(fx, punch, life + .00001f);
                Check(label.localScale == restScale && label.anchoredPosition == restPos &&
                    Punches(fx).Count == 0, "Return failed to restore exact rest");
                Check(ReferenceEquals(business, Punches(host)[body.transform]) &&
                    (float)Get(business, "amp") == .2f, "Host business effect was changed");
            }

            host.Reduced = true;
            Call(button, "PressLabel");
            var reducedFx = label.GetComponent<XgJuice>();
            var reduced = Punches(reducedFx)[label];
            Check((float)Get(reduced, "amp") == .015f &&
                (Vector2)Get(reduced, "dir") == Vector2.zero &&
                (float)Get(reduced, "life") == .12f, "Reduced start parameters changed");
            host.Reduced = false;
            Call(button, "LateUpdate");
            Check((float)Get(reduced, "amp") == .015f, "Live disable amplified an old press");
            // A genuinely new input must restore the complete normal effect, including
            // when the private stack had been settling on an earlier press.
            Call(button, "PressLabel");
            Check((float)Get(reduced, "amp") == .045f && !(bool)Get(reduced, "settling"),
                "New normal press did not leave reduced/settling mode");
            host.Reduced = true;
            Call(button, "LateUpdate");
            button.enabled = false;
            Check(label.localScale == restScale && label.anchoredPosition == restPos &&
                Punches(reducedFx).Count == 0, "Disable during return left deformation");
            Debug.Log("ReducedSwitchChecks completed synchronous probes; actual frames and visual acceptance remain pending.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void Sample(XgJuice fx, object punch, float desiredAge)
    {
        Set(punch, "t", desiredAge - Mathf.Min(Time.unscaledDeltaTime, .05f));
        Call(fx, "Update");
    }
    static IDictionary Punches(XgJuice fx) => (IDictionary)Get(fx, "punches");
    static object Get(object o, string name) => o.GetType().GetField(name, Flags).GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Flags).SetValue(o, value);
    static void Call(object o, string name) => o.GetType().GetMethod(name, Flags).Invoke(o, null);
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("ReducedSwitchChecks: " + message);
    }
}
