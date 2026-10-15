using System;
using System.Collections;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Isolated, save-free checks; run explicitly through the shared validation session.</summary>
public static class MotionButtonChecks
{
    [UnityEditor.MenuItem("Tools/Design Pass/Check Motion Button Foundation")]
    public static void Run()
    {
        var root = new GameObject("MotionButtonChecks", typeof(RectTransform), typeof(CanvasGroup));
        var events = new GameObject("MotionButtonCheckEvents", typeof(EventSystem));
        try
        {
            var host = root.AddComponent<XgJuice>();
            var b = new XgUi(null, null).Button(root.transform, "Check", null, 16);
            var button = b.button as XgButtonPressFeedback;
            Check(button != null, "XgUi must install the feedback Button subtype");
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = b.label.rectTransform;
            var restScale = new Vector3(1.2f, .9f, 1);
            var restPos = new Vector2(7, 3);
            label.localScale = restScale;
            label.anchoredPosition = restPos;
            var data = new PointerEventData(events.GetComponent<EventSystem>());
            int clicks = 0;
            bool disableOnClick = false, feedbackBeforeClick = false;
            button.onClick.AddListener(() =>
            {
                clicks++;
                var fx = label.GetComponent<XgJuice>();
                feedbackBeforeClick = fx != null && Punches(fx).Count == 1;
                host.Knock(b.rt, .2f, new Vector2(8, 0));
                if (disableOnClick) button.interactable = false;
            });

            data.button = PointerEventData.InputButton.Right;
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerClickHandler);
            Check(clicks == 0 && label.GetComponent<XgJuice>() == null, "Right click must do nothing");
            data.button = PointerEventData.InputButton.Left;
            button.interactable = false;
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.submitHandler);
            Check(clicks == 0 && label.GetComponent<XgJuice>() == null, "Disabled input must do nothing");
            button.interactable = true;
            var group = root.GetComponent<CanvasGroup>();
            group.interactable = false;
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerDownHandler);
            Check(label.GetComponent<XgJuice>() == null, "CanvasGroup must block feedback");
            group.interactable = true;

            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerDownHandler);
            var press = label.GetComponent<XgJuice>();
            Check(press != null && Punches(press).Count == 1 && clicks == 0,
                "Pointer down must start one label effect without invoking business");
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerClickHandler);
            Check(clicks == 1 && feedbackBeforeClick, "Click must invoke business exactly once");
            var businessPunch = Punches(host)[b.rt];
            // Use the actual writer: arbitrary label writes now represent new layout values.
            RenderPress(press, label);
            for (int i = 0; i < 40; i++)
                ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerDownHandler);
            Check(Punches(press).Count == 1, "Rapid presses must retain one effect");
            Check(ReferenceEquals(businessPunch, Punches(host)[b.rt]) &&
                (float)Field(businessPunch, "amp") == .2f,
                "Generic feedback must preserve stronger business Knock");
            button.interactable = false;
            Check(Punches(press).Count == 0 && !press.enabled &&
                label.localScale == restScale && label.anchoredPosition == restPos,
                "Disabling must cancel and restore the original resting transform");
            Check(Punches(host).Count == 1, "Cancellation must not clear business effects");

            button.interactable = true;
            disableOnClick = true;
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.submitHandler);
            Check(clicks == 2 && feedbackBeforeClick && !press.enabled,
                "Keyboard submit must start feedback before one callback, then honor disable");

            button.interactable = true;
            host.Reduced = true;
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerDownHandler);
            var reduced = Punches(press)[label];
            Check((float)Field(reduced, "amp") == .015f && (Vector2)Field(reduced, "dir") == Vector2.zero,
                "Reduced effects must use a smaller press with no translation");
            b.rt.gameObject.SetActive(false);
            Check(!press.enabled && Punches(press).Count == 0 && label.localScale == restScale,
                "Hiding must reset feedback");
            b.rt.gameObject.SetActive(true);
            host.Reduced = false;
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerDownHandler);
            RenderPress(press, label);
            var atScale = label.localScale;
            var atPosition = label.anchoredPosition;
            host.Reduced = true;
            typeof(XgButtonPressFeedback).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(button, null);
            Check(press.enabled && label.localScale == atScale && label.anchoredPosition == atPosition,
                "Turning on Reduced must register a quiet return without snapping the label");
            RenderPress(press, label, true);
            typeof(XgButtonPressFeedback).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(button, null);
            Check(!press.enabled && label.localScale == restScale && label.anchoredPosition == restPos,
                "Quiet return must restore the label and stop its private stack");
            host.Visible = () => false;
            ExecuteEvents.Execute(b.rt.gameObject, data, ExecuteEvents.pointerDownHandler);
            Check(!press.enabled, "Hidden host must suppress feedback");
            Debug.Log("MotionButtonChecks PASS: input guards, submit order, rapid presses, business isolation, reset, reduced FX. Scene/screenshots still require acceptance.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(events);
        }
    }

    static IDictionary Punches(XgJuice fx) => (IDictionary)Field(fx, "punches");
    static void RenderPress(XgJuice fx, RectTransform target, bool finish = false)
    {
        var punch = Punches(fx)[target];
        punch.GetType().GetField("t").SetValue(punch, finish ? .3f : .04f);
        typeof(XgJuice).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fx, null);
    }
    static object Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("MotionButtonChecks: " + message);
    }
}
