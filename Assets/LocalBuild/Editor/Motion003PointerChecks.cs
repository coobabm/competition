using System;
using System.Collections;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Integration candidate only. Run in an isolated validation scene after compilation.
// Uses the installed input module for click/drag routing, not a copied click predicate.
public static class Motion003PointerChecks
{
    [UnityEditor.MenuItem("Tools/Design Pass/Check Motion 003 Pointer Boundaries")]
    public static void Run()
    {
        Require(EventSystem.current == null, "Use an isolated scene with no EventSystem");
        Require(Cursor.lockState == CursorLockMode.None, "Unlock the cursor before this check");
        for (int scenario = 0; scenario < 7; scenario++)
        {
            int standard = ClickCount<Button>(scenario);
            int feedback = ClickCount<XgButtonPressFeedback>(scenario);
            int expected = new[] { 1, 0, 1, 0, 0, 2, 1 }[scenario];
            Require(standard == expected && feedback == standard,
                "Business callback mismatch for scenario " + scenario);
        }
        using (var f = new Fixture<XgButtonPressFeedback>())
        {
            var a = f.Down(0); // Zero is a valid touch id, not an unowned sentinel.
            var right = new PointerEventData(f.Events) { pointerId = -2, button = PointerEventData.InputButton.Right };
            ExecuteEvents.Execute(f.Button.gameObject, right, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(f.Button.gameObject, right, ExecuteEvents.pointerExitHandler);
            f.Active("Right button must neither own nor cancel the active touch feedback");
            f.Deform();
            f.Move(a, false);
            f.Restored("Owner exit must restore non-default scale and position immediately");
            f.Up(a);

            a = f.Down(11);
            f.Deform();
            f.Drag(a);
            f.Restored("Parent drag takeover must reset even before eligibility is cleared");
            Require(!a.eligibleForClick && a.pointerPress == null, "Module must cancel dragged click");
            f.Up(a);

            a = f.Down(11);
            var b = f.Down(22);
            f.Deform();
            f.Move(a, false);
            f.Active("Older finger exit must preserve the newest finger's feedback");
            f.Drag(a);
            f.Active("Older finger drag must preserve the newest finger's feedback");
            ExecuteEvents.Execute(f.Button.gameObject, a, ExecuteEvents.cancelHandler);
            f.Active("Pointer-scoped cancellation of an older finger must not reset the owner");
            f.Up(a);
            f.Active("Older finger release must preserve the owner");
            f.Move(b, false);
            f.Restored("Owner exit must reset after another finger has released");
            f.Up(b);

            a = f.Down(11);
            f.Deform();
            var cancel = new BaseEventData(f.Events);
            ExecuteEvents.Execute(f.Button.gameObject, cancel, ExecuteEvents.cancelHandler);
            f.Restored("Navigation Cancel must reset feedback");
            Require(!cancel.used && a.eligibleForClick, "Cancel must not consume or mutate pointer eligibility");
            f.Up(a);

            a = f.Down(11);
            f.Deform();
            // PointerInputModule maps TouchPhase.Canceled to an empty raycast before release.
            a.pointerCurrentRaycast = new RaycastResult();
            f.Up(a);
            f.Restored("Canceled touch release plus exit must restore feedback");
            Require(f.HostPunches == 1, "All cleanup paths must preserve business Knock");
        }
        Debug.Log("Motion003PointerChecks: assertions completed. Hardware input, frame timing and visual acceptance remain separate.");
    }

    static int ClickCount<T>(int scenario) where T : Button
    {
        using (var f = new Fixture<T>())
        {
            var a = f.Down(11);
            switch (scenario)
            {
                case 1: f.Move(a, false); break; // Release outside.
                case 2: f.Move(a, false); f.Move(a, true); break; // Return without drag: click survives.
                case 3: f.Drag(a); break; // Parent owns drag: no click even inside.
                case 4: a.pointerCurrentRaycast = new RaycastResult(); break; // OS cancellation mapping.
                case 5: var b = f.Down(22); f.Up(b); break; // Two valid touches: two base clicks.
                case 6:
                    ExecuteEvents.Execute(f.Button.gameObject, new BaseEventData(f.Events), ExecuteEvents.cancelHandler);
                    break; // Navigation Cancel is not a pointer cancellation in base Button.
            }
            f.Up(a);
            return f.Clicks;
        }
    }

    sealed class Fixture<T> : IDisposable where T : Button
    {
        readonly GameObject root, eventsObject;
        readonly StandaloneInputModule module;
        readonly RectTransform label;
        readonly XgJuice host;
        readonly Vector3 restScale = new Vector3(1.2f, .9f, 1);
        readonly Vector2 restPosition = new Vector2(7, 3);
        public readonly T Button;
        public readonly EventSystem Events;
        public int Clicks;
        public int HostPunches => Punches(host).Count;

        public Fixture()
        {
            root = new GameObject("Motion003Root", typeof(RectTransform), typeof(ScrollRect));
            eventsObject = new GameObject("Motion003Events", typeof(EventSystem), typeof(StandaloneInputModule));
            Events = eventsObject.GetComponent<EventSystem>();
            module = eventsObject.GetComponent<StandaloneInputModule>();
            // AddComponent in EditMode does not guarantee UIBehaviour.OnEnable ran.
            // Initialize through the installed module's own lifecycle method, rather than
            // writing its private EventSystem field or entering Play Mode.
            Call(typeof(BaseInputModule), "OnEnable");
            Require(ReferenceEquals(typeof(BaseInputModule).GetProperty("eventSystem",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(module), Events),
                "Fixture input module must be bound to its EventSystem before dispatch");
            module.enabled = false; // The checks dispatch synchronously; do not poll real devices.
            host = root.AddComponent<XgJuice>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            root.GetComponent<ScrollRect>().content = (RectTransform)content.transform;
            var target = new GameObject("Button", typeof(RectTransform), typeof(Image));
            target.transform.SetParent(content.transform, false);
            Button = target.AddComponent<T>();
            Button.targetGraphic = target.GetComponent<Image>();
            Button.navigation = new Navigation { mode = Navigation.Mode.None };
            Button.onClick.AddListener(() => Clicks++);
            label = (RectTransform)new GameObject("Label", typeof(RectTransform)).transform;
            label.SetParent(target.transform, false);
            label.localScale = restScale;
            label.anchoredPosition = restPosition;
            if (Button is XgButtonPressFeedback feedback) feedback.Label = label;
            host.Knock((RectTransform)target.transform, .2f, Vector2.right);
        }

        public PointerEventData Down(int id)
        {
            var data = new PointerEventData(Events) { pointerId = id, button = PointerEventData.InputButton.Left };
            data.pointerCurrentRaycast = new RaycastResult { gameObject = Button.gameObject };
            Call(typeof(StandaloneInputModule), "ProcessTouchPress", data, true, false);
            Require(data.pointerDrag == root, "Feedback must not steal the parent's ScrollRect drag handler");
            return data;
        }

        public void Up(PointerEventData data) => Call(typeof(StandaloneInputModule), "ProcessTouchPress", data, false, true);
        public void Move(PointerEventData data, bool inside)
        {
            data.pointerCurrentRaycast = new RaycastResult { gameObject = inside ? Button.gameObject : null };
            Call(typeof(PointerInputModule), "ProcessMove", data);
        }
        public void Drag(PointerEventData data)
        {
            data.position = new Vector2(100, 100);
            data.delta = new Vector2(100, 100);
            Call(typeof(PointerInputModule), "ProcessDrag", data);
        }
        public void Deform()
        {
            Active("Press must first register an effect");
            var fx = label.GetComponent<XgJuice>();
            var punch = Punches(fx)[label];
            punch.GetType().GetField("t").SetValue(punch, .04f);
            typeof(XgJuice).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fx, null);
        }
        public void Active(string message)
        {
            var fx = label.GetComponent<XgJuice>();
            Require(fx != null && fx.enabled && Punches(fx).Count == 1, message);
        }
        public void Restored(string message)
        {
            var fx = label.GetComponent<XgJuice>();
            Require(fx != null && !fx.enabled && Punches(fx).Count == 0 &&
                label.localScale == restScale && label.anchoredPosition == restPosition, message);
        }
        void Call(Type type, string method, params object[] arguments) =>
            type.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(module, arguments);
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(eventsObject);
        }
    }
    static IDictionary Punches(XgJuice fx) => (IDictionary)typeof(XgJuice)
        .GetField("punches", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fx);
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Motion003: " + message);
    }
}
