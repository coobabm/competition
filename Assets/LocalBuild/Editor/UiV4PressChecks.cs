using System;
using System.Collections;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;

public static class UiV4PressChecks
{
    public static IEnumerator Frames()
    {
        var root = new GameObject("UiV4PressChecks", typeof(RectTransform));
        var eventsGo = EventSystem.current == null ? new GameObject("PressEvents", typeof(EventSystem)) : null;
        try
        {
            var host = root.AddComponent<XgJuice>(); host.DisableMotionWhenReduced = true;
            var kit = new XgUi(null, null, true);
            var built = kit.Button(root.transform, "Press", null, 16);
            var button = (XgButtonPressFeedback)built.button;
            var label = built.label.rectTransform;
            var target = built.Visual;
            var hit = built.rt;
            hit.sizeDelta = new Vector2(240, 100);
            var sub = kit.Text(XgUi.Rect("Sub", target, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "Detail", 12, Color.white, TMPro.TextAlignmentOptions.Bottom);
            var nested = kit.Button(hit, "Nested", null, 12); // Independent interactive child stays on the fixed root.
            var light = new XgUi(null, null, false).Button(root.transform, "Light", null, 12);
            Check(built.image.rectTransform == target && label.parent == target && sub.transform.parent == target, "All visual graphics share shell");
            Check(target != hit && button.PressTarget == target && button.targetGraphic == built.image, "Factory wires whole visual target");
            Check(hit.GetComponent<UnityEngine.UI.Image>().raycastTarget && hit.GetComponent<UnityEngine.UI.Image>().color.a == 0 && !built.image.raycastTarget, "Fixed transparent hit area and nonblocking face");
            Check(light.Visual == light.rt && light.image.rectTransform == light.rt && !((XgButtonPressFeedback)light.button).V4Press, "Light factory hierarchy and behavior unchanged");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = -1 };
            int clicks = 0; button.onClick.AddListener(() => clicks++);
            button.OnPointerDown(pointer);
            Check(Vector3.Distance(target.localScale, Vector3.one * .95f) < .00001f, "Immediate 0.95 press");
            yield return new WaitForSecondsRealtime(.3f);
            Check(Vector3.Distance(target.localScale, Vector3.one * .95f) < .00001f, "Held beyond release duration");
            Check(hit.localScale == Vector3.one, "Hit rect unchanged");
            Check(Vector3.Distance(label.lossyScale, target.lossyScale)<.00001f && Vector3.Distance(sub.transform.lossyScale,target.lossyScale)<.00001f, "Face, label and decoration scale together");
            Check(nested.rt.localScale==Vector3.one && nested.rt.parent==hit, "Nested control is not moved/scaled with parent visual");
            button.OnPointerDown(pointer);
            Check(Vector3.Distance(target.localScale, Vector3.one * .95f) < .00001f, "Repeated down never compounds");
            button.OnPointerUp(pointer);
            Check(clicks == 0, "Release never fabricates action");
            yield return new WaitForSecondsRealtime(.3f);
            Check(target.localScale == Vector3.one, "Release exact rest");
            button.OnPointerDown(pointer); host.Reduced = true;
            yield return null; yield return null;
            Check(target.localScale == Vector3.one && !target.GetComponent<XgJuice>().HasActiveKnock(target), "Reduce stops immediately");
            button.OnPointerDown(pointer);
            Check(target.localScale == Vector3.one, "Reduce skips new feedback");
            host.Knock(hit);
            Check(!host.HasActiveKnock(hit), "Reduced blocks opted-in business motion");
            host.Reduced = false;
            host.Knock(hit); Check(host.HasActiveKnock(hit), "Normal business motion retained");
            host.Reduced = true; yield return null; yield return null;
            Check(!host.HasActiveKnock(hit) && hit.localScale == Vector3.one, "Reduce clears existing business motion");
            host.Reduced = false;
            host.Knock(hit, .10f, Vector2.right * 7, .8f);
            yield return null;
            button.OnPointerDown(pointer);button.OnCancel(new BaseEventData(EventSystem.current));
            Check(target.localScale == Vector3.one && host.HasActiveKnock(hit), "Cancel shell does not cancel business root animation");
            host.Clear();
            Check(hit.localScale==Vector3.one && target.localScale==Vector3.one, "Independent stacks restore exact rest");
            button.OnSubmit(new BaseEventData(EventSystem.current));
            Check(clicks == 1, "Keyboard action once");
            yield return new WaitForSecondsRealtime(.3f);
            Check(target.localScale == Vector3.one, "Keyboard releases automatically");
            button.OnPointerDown(pointer); button.OnCancel(new BaseEventData(EventSystem.current));
            Check(target.localScale == Vector3.one, "Cancel immediate rest");
            button.OnPointerDown(pointer); button.interactable = false;
            Check(target.localScale == Vector3.one, "Disable immediate rest");
            Debug.Log("UI_V4_PRESS_CHECKS_PASS");
        }
        finally { UnityEngine.Object.Destroy(root); if (eventsGo != null) UnityEngine.Object.Destroy(eventsGo); }
    }
    static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); }
}
