using System;
using System.Collections;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Candidate Editor check, not installed or run. No controller or player save access.
public static class Motion008HoldChecks
{
    [UnityEditor.MenuItem("Tools/Design Pass/Check Motion 008 Hold Exit")]
    public static void Run()
    {
        foreach (bool feedbackFirst in new[] { true, false })
            foreach (bool reduced in new[] { false, true })
                CheckSequence(feedbackFirst, reduced);
        Debug.Log("Motion008 synchronous event checks completed. Natural animation and real input still require scene verification.");
    }

    static void CheckSequence(bool feedbackFirst, bool reduced)
    {
        var root = new GameObject("Motion008Checks", typeof(RectTransform));
        var events = new GameObject("Motion008Events", typeof(EventSystem));
        try
        {
            var host = root.AddComponent<XgJuice>();
            host.Reduced = reduced;
            var b = new XgUi(null, null).Button(root.transform, "Hold", null, 16);
            var button = (XgButtonPressFeedback)b.button;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var hold = b.rt.gameObject.AddComponent<XgHold>();
            int downs = 0, ups = 0, clicks = 0;
            hold.Down = () => downs++;
            hold.Up = () => ups++;
            button.onClick.AddListener(() => clicks++);
            var data = new PointerEventData(events.GetComponent<EventSystem>())
                { button = PointerEventData.InputButton.Left };
            var label = b.label.rectTransform;
            var scale = new Vector3(1.2f, .9f, 1);
            var pos = new Vector2(7, 3);
            label.localScale = scale;
            label.anchoredPosition = pos;

            button.OnPointerEnter(data);
            button.OnPointerDown(data);
            hold.OnPointerDown(data);
            Require(hold.Held && downs == 1 && ups == 0 && clicks == 0, "Down business count");
            Require(State(button) == "Pressed", "Held background must remain pressed");
            var press = label.GetComponent<XgJuice>();
            Require(press != null && Punches(press).Count == 1, "One initial pulse");
            host.Knock(b.rt, .2f, Vector2.right);
            object business = Punches(host)[b.rt];
            // Simulated intermediate pose tests cancellation, not natural animation.
            label.localScale = scale * .98f;
            label.anchoredPosition = pos + Vector2.down;
            if (feedbackFirst) { button.OnPointerExit(data); hold.OnPointerExit(data); }
            else { hold.OnPointerExit(data); button.OnPointerExit(data); }
            Require(!hold.Held && downs == 1 && ups == 1 && clicks == 0, "Exit business count");
            Require(State(button) != "Pressed", "Exit must release stale pressed background");
            Require(!press.enabled && Punches(press).Count == 0 &&
                label.localScale == scale && label.anchoredPosition == pos, "Exit restores label rest pose");
            Require(ReferenceEquals(business, Punches(host)[b.rt]), "Strong host effect retained");

            button.OnPointerEnter(data);
            Require(State(button) == "Highlighted" && !hold.Held && downs == 1 && ups == 1,
                "Re-entry hovers without rearming hold");
            Require(!press.enabled && Punches(press).Count == 0 && clicks == 0, "Re-entry never replays pulse or click");
            button.OnPointerExit(data);
            hold.OnPointerExit(data);
            button.OnPointerUp(data);
            hold.OnPointerUp(data);
            Require(downs == 1 && ups == 1 && clicks == 0, "Release outside does not duplicate Up");
            // A fresh press must work after the cancelled gesture.
            button.OnPointerEnter(data);
            button.OnPointerDown(data);
            hold.OnPointerDown(data);
            Require(State(button) == "Pressed" && hold.Held && downs == 2 && Punches(press).Count == 1,
                "Fresh press rearms once");
            button.OnPointerUp(data);
            hold.OnPointerUp(data);
            Require(ups == 2 && clicks == 0, "Normal release only ends hold");
            // An input module may dispatch an eligible click after an inside release.
            button.OnPointerClick(data);
            Require(clicks == 1 && downs == 2 && ups == 2, "Existing eligible click still invokes exactly once");

            UnityEngine.Object.DestroyImmediate(hold);
            button.OnPointerDown(data);
            button.OnPointerExit(data);
            Require(State(button) == "Pressed" && press.enabled,
                "Ordinary Button retains existing uGUI exit and pulse behavior");
            button.OnPointerUp(data);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(events);
        }
    }

    static string State(Selectable button) => typeof(Selectable).GetProperty("currentSelectionState",
        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(button).ToString();
    static IDictionary Punches(XgJuice fx) => (IDictionary)typeof(XgJuice).GetField("punches",
        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fx);
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Motion008: " + message);
    }
}
