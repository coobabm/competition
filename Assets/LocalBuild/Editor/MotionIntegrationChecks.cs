using System;
using System.Collections;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Synchronous, isolated checks. These do not constitute real-frame or visual acceptance.</summary>
public static class MotionIntegrationChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [UnityEditor.MenuItem("Tools/Design Pass/Check Integrated Motion Foundation")]
    public static void Run()
    {
        Require(EventSystem.current == null, "Use an isolated scene with no EventSystem");
        MotionButtonChecks.Run();
        Motion003PointerChecks.Run();
        Motion008HoldChecks.Run();
        LabelBaselineChecks.Run();
        ReducedSwitchChecks.Run();
        CheckTrainingInputs();
        CheckClockAndLayoutDuringSettle();
        Debug.Log("MotionIntegrationChecks synchronous checks completed; real inputs, frames, purchase results and screenshots remain pending.");
    }

    static void CheckTrainingInputs()
    {
        var root = new GameObject("MotionTrainingInputChecks", typeof(RectTransform), typeof(CanvasGroup));
        var events = new GameObject("MotionTrainingEvents", typeof(EventSystem));
        try
        {
            var b = new XgUi(null, null).Button(root.transform, "Train", null, 16);
            b.button.navigation = new Navigation { mode = Navigation.Mode.None };
            var hold = b.rt.gameObject.AddComponent<XgHold>();
            var submit = b.rt.gameObject.AddComponent<XgTrainingSubmit>();
            int downs = 0, ups = 0, submits = 0;
            hold.Down = () => downs++;
            hold.Up = () => ups++;
            submit.Submit = () => submits++;
            var e = new PointerEventData(events.GetComponent<EventSystem>())
                { button = PointerEventData.InputButton.Left, pointerId = 11 };
            b.button.interactable = false;
            hold.OnPointerDown(e); submit.OnSubmit(e);
            Require(downs == 0 && submits == 0 && !hold.Held, "Disabled training handlers must reject input");
            b.button.interactable = true;
            root.GetComponent<CanvasGroup>().interactable = false;
            hold.OnPointerDown(e); submit.OnSubmit(e);
            Require(downs == 0 && submits == 0, "Inherited CanvasGroup must also block training handlers");
            root.GetComponent<CanvasGroup>().interactable = true;
            hold.OnPointerDown(e); hold.OnPointerDown(e);
            var other = new PointerEventData(events.GetComponent<EventSystem>())
                { button = PointerEventData.InputButton.Right, pointerId = 11 };
            hold.OnPointerDown(other); hold.OnPointerUp(other);
            other.button = PointerEventData.InputButton.Left; other.pointerId = 22;
            hold.OnPointerDown(other); hold.OnPointerUp(other); hold.OnPointerExit(other);
            Require(downs == 1 && ups == 0 && hold.Held, "Other buttons/fingers must not retrigger or end the hold");
            hold.OnPointerExit(e); hold.OnPointerUp(e);
            Require(ups == 1 && !hold.Held, "Owner exit must end the hold exactly once");
            submit.OnSubmit(e);
            Require(submits == 1, "Enabled training Submit must invoke once");
            submit.enabled = false;
            submit.OnSubmit(e);
            Require(submits == 1, "Disabled training Submit component must reject direct dispatch");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(events);
        }
    }

    static void CheckClockAndLayoutDuringSettle()
    {
        var root = new GameObject("MotionClockAndLayout", typeof(RectTransform));
        try
        {
            var b = new XgUi(null, null).Button(root.transform, "Check", null, 16);
            var button = (XgButtonPressFeedback)b.button;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = b.label.rectTransform;
            var restScale = new Vector3(1.2f, .9f, 1);
            label.localScale = restScale;
            label.anchoredPosition = new Vector2(7, 3);
            button.OnPointerDown(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
            var fx = label.GetComponent<XgJuice>();
            object p = Punches(fx)[label];
            Set(p, "t", .1799f);
            Call(button, "LateUpdate");
            Require(fx.enabled && fx.HasActiveKnock(label), "Button cleanup must wait for the actual effect writer");
            Sample(fx, p, .19f);
            Call(button, "LateUpdate");
            Require(!fx.enabled && !fx.HasActiveKnock(label) && label.localScale == restScale,
                "Completed effect must restore first, then stop its private stack");

            button.OnPointerDown(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
            p = Punches(fx)[label];
            Sample(fx, p, .04f);
            fx.SettleKnock(label);
            var pos = label.anchoredPosition; pos.x = 81; label.anchoredPosition = pos;
            Sample(fx, p, .02f);
            var scale = label.localScale; scale.x = -.77f; label.localScale = scale;
            fx.Clear();
            Require(label.anchoredPosition == new Vector2(81, 3) &&
                label.localScale == new Vector3(-.77f, .9f, 1),
                "Layout changes during quiet return must retain new axes without absorbing other-axis deformation");

            var scratch = Get(fx, "completedPunches");
            Require(scratch != null && ((IList)scratch).Count == 0, "Completion scratch must release references");
            fx.Knock(label);
            Sample(fx, Punches(fx)[label], .3f);
            Require(ReferenceEquals(scratch, Get(fx, "completedPunches")) && ((IList)scratch).Count == 0,
                "Completion scratch must be reused across cycles");
            var transient = new GameObject("DestroyedTarget", typeof(RectTransform));
            transient.transform.SetParent(root.transform, false);
            fx.Knock((RectTransform)transient.transform);
            UnityEngine.Object.DestroyImmediate(transient);
            Call(fx, "Update");
            Require(Punches(fx).Count == 0 && ((IList)scratch).Count == 0,
                "Destroyed targets must not remain in active or scratch collections");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void Sample(XgJuice fx, object p, float age)
    {
        Set(p, "t", age - Mathf.Min(Time.unscaledDeltaTime, .05f));
        Call(fx, "Update");
    }
    static IDictionary Punches(XgJuice fx) => (IDictionary)Get(fx, "punches");
    static object Get(object o, string name) => o.GetType().GetField(name, Flags).GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Flags).SetValue(o, value);
    static void Call(object o, string name) => o.GetType().GetMethod(name, Flags).Invoke(o, null);
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("MotionIntegrationChecks: " + message);
    }
}
