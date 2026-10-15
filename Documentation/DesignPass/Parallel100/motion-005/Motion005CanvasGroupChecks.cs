using System;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Candidate validation: copy into the coordinator's isolated Editor validation assembly.
// Has NOT been run by motion-005. No controller, save access, scene loading or asset writes.
public static class Motion005CanvasGroupChecks
{
    [UnityEditor.MenuItem("Tools/Design Pass/Check Motion 005 CanvasGroup")]
    public static void Run()
    {
        using (var f = new Fixture())
        {
            var p = f.Down();
            f.Deform();
            f.Outer.interactable = false;
            f.Restored("ancestor disabled during press");
            f.Outer.interactable = true;
            f.NotPressed("ancestor re-enabled before release");
            f.Release(p);
            Check(f.Clicks == 0, "cancelled gesture must not invoke business after re-enable");
            f.Release(f.Down());
            Check(f.Clicks == 1, "fresh click after cancellation must work exactly once");
        }
        using (var f = new Fixture())
        {
            f.Outer.interactable = false;
            f.Inner.ignoreParentGroups = true;
            Check(f.Button.IsInteractable(), "inner ignoreParentGroups shields disabled ancestor");
            var p = f.Down();
            f.Outer.interactable = true;
            f.Outer.interactable = false;
            f.Release(p);
            Check(f.Clicks == 1, "shielded ancestor changes must not cancel valid click");

            p = f.Down();
            f.Deform();
            f.Inner.ignoreParentGroups = false;
            f.Restored("ignoreParentGroups removed during press");
            f.Inner.ignoreParentGroups = true;
            f.NotPressed("ignoreParentGroups restored");
            f.Release(p);
            Check(f.Clicks == 1, "restoring isolation must not revive old click");
            f.Release(f.Down());
            Check(f.Clicks == 2, "new isolated click remains functional");

            f.Inner.interactable = false;
            Check(!f.Button.IsInteractable(), "ignoreParentGroups does not override own interactable=false");
            p = f.Down();
            f.Submit();
            Check(f.Clicks == 2, "blocked Submit must not invoke business");
            f.Inner.interactable = true;
            f.Release(p);
            Check(f.Clicks == 2, "press started while blocked must not activate on enable");
        }
        using (var f = new Fixture())
        {
            f.Outer.interactable = false;
            f.Outer.enabled = false;
            Check(f.Button.IsInteractable(), "disabled CanvasGroup must not impose interactable=false");
            var p = f.Down();
            f.Deform();
            f.Outer.enabled = true;
            f.Restored("CanvasGroup enabled with interactable=false");
            f.Outer.enabled = false;
            f.Release(p);
            Check(f.Clicks == 0, "disabling group again must not revive old gesture");
        }
        using (var f = new Fixture())
        {
            var first = f.Down(-1);
            var second = f.Down(2);
            f.Outer.interactable = false;
            f.Outer.interactable = true;
            f.Release(f.Down(3));
            f.Release(first);
            f.Release(second);
            Check(f.Clicks == 1, "new pointer must not revive cancelled gestures on other pointers");
        }
        using (var f = new Fixture())
        {
            var p = f.Down();
            f.Deform();
            f.Blocked.interactable = false;
            f.Button.transform.SetParent(f.Blocked.transform, false);
            f.Restored("reparent under blocked CanvasGroup");
            f.Button.transform.SetParent(f.Inner.transform, false);
            f.Release(p);
            Check(f.Clicks == 0, "return to original parent must not revive click");
        }
        using (var f = new Fixture())
        {
            var p = f.Down(91);
            f.Outer.interactable = false;
            f.Outer.interactable = true;
            // Release outside the target: input modules send Up without Click.
            ExecuteEvents.Execute(f.Button.gameObject, p, ExecuteEvents.pointerUpHandler);
            typeof(XgButtonPressFeedback).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(f.Button, null);
            var tracking = (System.Collections.ICollection)typeof(XgButtonPressFeedback)
                .GetField("pointerGestures", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Button);
            Check(tracking.Count == 0, "released pointer IDs must be reclaimed even without Click");
            f.Release(f.Down(91));
            Check(f.Clicks == 1, "reused pointer ID must begin a new valid gesture");
        }
        Debug.Log("Motion005CanvasGroupChecks completed: synchronous event/state assertions only; real input, animation frames and visual acceptance remain separate.");
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("motion-005: " + message);
    }

    sealed class Fixture : IDisposable
    {
        readonly GameObject root;
        readonly RectTransform label;
        readonly Vector3 scale = new Vector3(1.2f, .9f, 1);
        readonly Vector2 position = new Vector2(7, 3);
        public readonly CanvasGroup Outer, Inner, Blocked;
        public readonly XgButtonPressFeedback Button;
        public int Clicks;

        public Fixture()
        {
            root = new GameObject("motion-005 isolated fixture", typeof(RectTransform), typeof(CanvasGroup));
            Outer = root.GetComponent<CanvasGroup>();
            Inner = Group("Inner", root.transform);
            Blocked = Group("Blocked", root.transform);
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Inner.transform, false);
            Button = go.AddComponent<XgButtonPressFeedback>();
            Button.navigation = new Navigation { mode = Navigation.Mode.None };
            var text = new GameObject("Label", typeof(RectTransform));
            text.transform.SetParent(go.transform, false);
            label = text.GetComponent<RectTransform>();
            label.localScale = scale;
            label.anchoredPosition = position;
            Button.Label = label;
            Button.onClick.AddListener(() => Clicks++);
        }

        static CanvasGroup Group(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            return go.GetComponent<CanvasGroup>();
        }

        public PointerEventData Down(int id = -1)
        {
            var data = new PointerEventData(null) {
                pointerId = id, button = PointerEventData.InputButton.Left, eligibleForClick = true
            };
            ExecuteEvents.Execute(Button.gameObject, data, ExecuteEvents.pointerDownHandler);
            return data;
        }

        public void Release(PointerEventData data)
        {
            ExecuteEvents.Execute(Button.gameObject, data, ExecuteEvents.pointerUpHandler);
            // Intentionally send Click even after cancellation: the new Input System can cache
            // isClick before Up and restores per-button eligibility into shared eventData.
            ExecuteEvents.Execute(Button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        public void Submit() => ExecuteEvents.Execute(Button.gameObject,
            new BaseEventData(null), ExecuteEvents.submitHandler);

        public void Deform()
        {
            Check(label.GetComponent<XgJuice>() != null, "press must have allocated label effect");
            // Artificial mid-animation transform: verifies restoration, not animation playback.
            label.localScale = scale * .97f;
            label.anchoredPosition = position + Vector2.down;
        }

        public void Restored(string scenario)
        {
            Check(!Button.IsInteractable(), scenario + ": effective interaction must be false");
            var fx = label.GetComponent<XgJuice>();
            Check(fx != null && !fx.enabled && label.localScale == scale &&
                label.anchoredPosition == position, scenario + ": label must restore immediately");
        }

        public void NotPressed(string scenario)
        {
            var method = typeof(Selectable).GetMethod("IsPressed", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(!(bool)method.Invoke(Button, null), scenario + ": inherited pressed state must be cleared");
        }

        public void Dispose() => UnityEngine.Object.DestroyImmediate(root);
    }
}
