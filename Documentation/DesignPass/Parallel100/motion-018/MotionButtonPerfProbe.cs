// Candidate editor-only measurement helper. Integrator places this in a temporary Editor folder.
// Does not run automatically, load a controller, touch saves, or enter Play Mode.
using System;
using System.Diagnostics;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MotionButtonPerfProbe
{
    const int Samples = 2048, ColdButtons = 64;
    static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    struct Measurement
    {
        public string name;
        public long bytes, ticks;
        public int samples;
        public override string ToString() => name + ": samples=" + samples +
            ", managed bytes total=" + bytes + ", bytes/sample=" + (bytes / (double)samples).ToString("F2") +
            ", microseconds/sample=" + (ticks * 1e6 / Stopwatch.Frequency / samples).ToString("F3");
    }

    [UnityEditor.MenuItem("Tools/Design Pass/Measure Motion Button Allocation (isolated edit mode)")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use an empty, isolated edit-mode scene.");
        var root = new GameObject("MotionButtonPerfProbe", typeof(RectTransform));
        root.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var host = root.AddComponent<XgJuice>();
            var ui = new XgUi(null, null);
            var data = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
            var buttons = new XgButtonPressFeedback[ColdButtons];
            for (int i = 0; i < buttons.Length; i++) buttons[i] = Create(ui, root.transform);
            var warm = Create(ui, root.transform);
            warm.OnPointerDown(data);
            var fx = warm.Label.GetComponent<XgJuice>();
            var tick = Bind(fx, "Update");
            var late = Bind(warm, "LateUpdate");
            // Warm type initialization, dictionary completion capacity and both measurement branches.
            fx.Knock(warm.Label, seconds: -1);
            tick();
            warm.interactable = false;
            warm.interactable = true;
            warm.OnPointerDown(data);
            tick();
            Measure("warmup", 4, null, () => { });

            var rows = new Measurement[8];
            rows[0] = Measure("empty delegate / timer baseline", Samples, null, () => { });
            int coldIndex = 0;
            rows[1] = Measure("first input, constructed button (creation excluded)", ColdButtons,
                null, () => buttons[coldIndex++].OnPointerDown(data));
            rows[2] = Measure("retrigger while active", Samples, null, () => warm.OnPointerDown(data));
            rows[3] = Measure("one active XgJuice.Update call", Samples,
                () => warm.OnPointerDown(data), tick);
            rows[4] = Measure("cancel active via interactable=false", Samples,
                () => { warm.interactable = true; warm.OnPointerDown(data); },
                () => warm.interactable = false);
            rows[5] = Measure("restart after cancellation", Samples,
                () => { warm.interactable = false; warm.interactable = true; },
                () => warm.OnPointerDown(data));
            warm.interactable = false;
            warm.interactable = true;
            rows[6] = Measure("idle feedback LateUpdate call", Samples, null, late);
            host.Visible = () => false;
            rows[7] = Measure("hidden host input rejection", Samples, null, () => warm.OnPointerDown(data));

            // Count components and allocate report strings only AFTER timed scopes.
            for (int i = 0; i < buttons.Length; i++)
                Require(buttons[i].Label.GetComponents<XgJuice>().Length == 1, "Cold label component count");
            Require(warm.Label.GetComponents<XgJuice>().Length == 1, "Repeated label component count");
            Require(warm.GetComponents<Button>().Length == 1, "Button subtype replaces original Button");
            Require(!fx.enabled, "Hidden input must not restart private effect");
            Require(root.GetComponentsInChildren<AudioSource>(true).Length == 0, "Unexpected audio creation");
            foreach (var row in rows) UnityEngine.Debug.Log("MotionButtonPerfProbe " + row);
            UnityEngine.Debug.Log("MotionButtonPerfProbe measurements completed; NOT frame/FPS, Play Mode, or visual acceptance.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static XgButtonPressFeedback Create(XgUi ui, Transform parent)
    {
        var b = (XgButtonPressFeedback)ui.Button(parent, "Probe", null, 16).button;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        Require(b.Label.GetComponents<XgJuice>().Length == 0, "Unused label should have no private effect");
        return b;
    }

    static Action Bind(object target, string method)
    {
        var info = target.GetType().GetMethod(method, PrivateInstance);
        if (info == null) throw new MissingMethodException(target.GetType().FullName, method);
        return (Action)Delegate.CreateDelegate(typeof(Action), target, info);
    }

    static Measurement Measure(string name, int count, Action prepare, Action operation)
    {
        var result = new Measurement { name = name, samples = count };
        for (int i = 0; i < count; i++)
        {
            // Preparation and all closures/reflection/component queries are outside measured scope.
            prepare?.Invoke();
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            operation();
            long end = Stopwatch.GetTimestamp();
            result.bytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
            result.ticks += end - start;
        }
        return result;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("MotionButtonPerfProbe: " + message);
    }
}
