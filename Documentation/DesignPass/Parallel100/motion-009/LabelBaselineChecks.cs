using System;
using System.Collections;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Candidate Editor check; not imported or executed by motion-009.
// Run only in the integrator's isolated validation session after patch integration.
public static class LabelBaselineChecks
{
    [UnityEditor.MenuItem("Tools/Design Pass/Check Label Baseline 009")]
    public static void Run()
    {
        using (var f = new Fixture())
        {
            var restScale = f.label.localScale;
            var restPos = f.label.anchoredPosition;
            f.Press(); f.Render();
            Require(f.label.localScale != restScale, "Precondition: actual effect writer must deform label");
            f.root.localScale = new Vector3(-1.7f, .65f, 1.3f);
            for (int i = 0; i < 40; i++) { f.Press(); f.Render(); }
            f.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 730);
            f.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 110);
            f.Render(true);
            Near(f.label.localScale, restScale, "Natural completion / non-unit and zero scale");
            Near(f.label.anchoredPosition, restPos, "Repeated presses must not accumulate displacement");
            Require(f.label.anchoredPosition3D.z == 11, "Local Z must survive 2D position writes");
            Near(f.root.localScale, new Vector3(-1.7f, .65f, 1.3f), "Parent scale ownership");
            Near(f.label.position, f.control.label.rectTransform.position, "Anchor-only resize follows the unanimated control");

            f.Press(); f.Render();
            var pos = f.label.anchoredPosition; pos.x = 87; f.label.anchoredPosition = pos;
            var scale = f.label.localScale; scale.x = -.65f; f.label.localScale = scale;
            f.button.interactable = false; // No Update between external write and cancellation.
            Near(f.label.anchoredPosition, new Vector2(87, restPos.y), "Only written position axis becomes baseline");
            Near(f.label.localScale, new Vector3(-.65f, restScale.y, restScale.z), "Only written scale axis becomes baseline");

            f.button.interactable = true;
            f.Press(); f.Render();
            var declaredScale = f.label.localScale;
            var declaredPos = f.label.anchoredPosition;
            f.fx.SetKnockRestTransform(f.label, declaredScale, declaredPos);
            f.button.interactable = false;
            Near(f.label.localScale, declaredScale, "Explicit rest handshake at identical rendered value");
            Near(f.label.anchoredPosition, declaredPos, "Explicit position handshake at identical rendered value");
        }

        foreach (bool cancel in new[] { false, true })
        using (var f = new Fixture())
        {
            var layout = AddLayout(f.button.transform);
            var controlLayout = AddLayout(f.control.button.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(f.button.transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(f.control.rt);
            f.Press(); f.Render();
            // Actual layout writer and a no-animation control provide the expected baseline.
            // The root models a resized window; this does not alter project window defaults.
            f.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 810);
            f.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 145);
            layout.padding.left = controlLayout.padding.left = 29;
            layout.padding.bottom = controlLayout.padding.bottom = 13;
            f.root.localScale = new Vector3(.7f, 1.8f, 1);
            LayoutRebuilder.ForceRebuildLayoutImmediate(f.button.transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(f.control.rt);
            var expectedPos = f.control.label.rectTransform.anchoredPosition;
            var expectedScale = f.control.label.rectTransform.localScale;
            if (cancel) f.button.interactable = false;
            else { f.Render(); f.Press(); f.Render(true); }
            Near(f.label.anchoredPosition, expectedPos, "Layout/window resize, cancel=" + cancel);
            Near(f.label.localScale, expectedScale, "Layout must not capture animated scale, cancel=" + cancel);
        }

        using (var f = new Fixture())
        {
            // Ordinary business Knock retains its original ownership semantics.
            var host = f.root.GetComponent<XgJuice>();
            var rest = f.control.rt.anchoredPosition;
            host.Knock(f.control.rt);
            f.control.rt.anchoredPosition = new Vector2(777, 333);
            host.Clear();
            Near(f.control.rt.anchoredPosition, rest, "Non-opted-in business effects remain unchanged");
        }
        Debug.Log("LabelBaselineChecks assertions completed. Real-frame timing and visual acceptance remain pending.");
    }

    static HorizontalLayoutGroup AddLayout(Transform parent)
    {
        var group = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.childControlWidth = group.childControlHeight = false;
        group.childForceExpandWidth = group.childForceExpandHeight = false;
        group.childScaleWidth = group.childScaleHeight = false;
        group.childAlignment = TextAnchor.LowerRight;
        group.padding = new RectOffset(9, 17, 5, 7);
        return group;
    }

    sealed class Fixture : IDisposable
    {
        public readonly RectTransform root;
        public readonly XgButtonPressFeedback button;
        public readonly XgBtn control;
        public readonly RectTransform label;
        public XgJuice fx;
        readonly GameObject events;
        readonly PointerEventData data;
        public Fixture()
        {
            root = new GameObject("LabelBaseline009", typeof(RectTransform), typeof(Canvas)).transform as RectTransform;
            root.sizeDelta = new Vector2(420, 70);
            root.gameObject.AddComponent<XgJuice>();
            events = new GameObject("LabelBaseline009Events", typeof(EventSystem));
            data = new PointerEventData(events.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Left };
            var ui = new XgUi(null, null);
            var animated = ui.Button(root, "Animated", null, 16);
            control = ui.Button(root, "Control", null, 16);
            foreach (var b in new[] { animated, control })
            {
                b.rt.anchorMin = Vector2.zero; b.rt.anchorMax = Vector2.one;
                b.rt.offsetMin = b.rt.offsetMax = Vector2.zero;
                b.button.navigation = new Navigation { mode = Navigation.Mode.None };
                b.label.rectTransform.anchorMin = b.label.rectTransform.anchorMax = new Vector2(.3f, .7f);
                b.label.rectTransform.pivot = new Vector2(.2f, .8f);
                b.label.rectTransform.sizeDelta = new Vector2(64, 22);
                b.label.rectTransform.localScale = new Vector3(-1.2f, .9f, 0);
                b.label.rectTransform.anchoredPosition3D = new Vector3(7, 3, 11);
            }
            button = (XgButtonPressFeedback)animated.button;
            label = animated.label.rectTransform;
        }
        public void Press()
        {
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerDownHandler);
            fx = label.GetComponent<XgJuice>();
            Require(fx != null, "Feedback component expected");
        }
        public void Render(bool finish = false)
        {
            var punches = (IDictionary)typeof(XgJuice).GetField("punches", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fx);
            var p = punches[label];
            Require(p != null, "Active punch expected");
            // Exercise the production writer at a bounded phase; no duplicated waveform oracle.
            p.GetType().GetField("t").SetValue(p, finish ? .2f : .04f);
            typeof(XgJuice).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fx, null);
        }
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            UnityEngine.Object.DestroyImmediate(events);
        }
    }

    static void Near(Vector3 actual, Vector3 expected, string message)
    {
        Require((actual - expected).sqrMagnitude < .000001f, message + ": " + actual + " != " + expected);
    }
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("LabelBaseline009: " + message);
    }
}
