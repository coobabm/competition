using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class DesignWallpaperSmoke
{
    // Called once after the installed synchronous checks, in the existing empty Play scene.
    static void StartMotionFrameChecks()
    {
        Check(Application.isPlaying && EventSystem.current == null, "Motion frames use the isolated Play scene");
        due = double.PositiveInfinity;
        var runner = new GameObject("MotionFrameRunner").AddComponent<XgJuice>();
        runner.StartCoroutine(CheckMotionFrames());
    }

    static IEnumerator CheckMotionFrames()
    {
        var steps = MotionFrameSteps();
        Exception failure = null;
        while (true)
        {
            object current = null;
            bool more = false;
            try { more = steps.MoveNext(); if (more) current = steps.Current; }
            catch (Exception error) { failure = error; }
            if (failure != null || !more) break;
            yield return current;
        }
        (steps as IDisposable)?.Dispose();
        File.WriteAllText(Path.Combine(Output, "result.txt"), failure == null ? "PASS" : "FAIL: " + failure);
        Finish(failure == null ? 0 : 1);
    }

    static IEnumerator MotionFrameSteps()
    {
        var f = new MotionFrameFixture();
        float previousScale = Time.timeScale;
        var endFrame = new WaitForEndOfFrame();
        var samples = new StringBuilder("phase,frame,unscaledTime,row,scaleX,scaleY,positionX,positionY,active\n");
        var waits = new StringBuilder("phase,frame,waitFrames,wallSeconds,cappedSeconds,targetAge,targetLife\n");
        try
        {
            yield return endFrame;
            Canvas.ForceUpdateCanvases();
            foreach (var row in f.Rows)
            {
                row.Button.label.ForceMeshUpdate();
                Check(row.Button.label.textInfo.characterCount > 0 && row.Button.label.font == f.Font,
                    "Motion row uses the actual desktop font: " + row.Name);
                Check(row.Button.label.rectTransform.GetComponent<XgJuice>() == null,
                    "Unused label has no private effect: " + row.Name);
            }
            MotionScreenshot("motion-rest");

            Time.timeScale = 0;
            foreach (var row in f.Rows)
            {
                row.Host.Knock(row.Button.rt, .12f, Vector2.right * 5, .9f);
                row.Down();
            }
            int frames = 0;
            var budget = new MotionFrameBudget("natural", .18f, waits);
            while (f.AnyActive())
            {
                budget.BeforeWait();
                yield return endFrame;
                f.Sample(samples, "natural");
                if (++frames == 1) MotionScreenshot("motion-first-active");
            }
            budget.Complete();
            foreach (var row in f.Rows)
            {
                Check(row.SawDeformation, "Real Update deforms the label while timeScale is zero: " + row.Name);
                row.Restored("Natural completion");
                Check(row.Host.HasActiveKnock(row.Button.rt), "Label completion retains the longer host Knock: " + row.Name);
                row.Up(); row.Host.Clear();
            }
            Check(Time.timeScale == 0, "Natural label completion leaves the paused simulation clock unchanged");
            yield return new WaitForSecondsRealtime(.08f);
            yield return endFrame;
            MotionScreenshot("motion-settled");

            // Repeated inputs advance through real Unity frames, without resetting Punch.t.
            for (int cycle = 0; cycle < 12; cycle++)
            {
                foreach (var row in f.Rows) row.Down();
                yield return endFrame;
                foreach (var row in f.Rows) row.Down();
                budget = new MotionFrameBudget("retrigger-" + cycle, .18f, waits);
                while (f.AnyActive())
                {
                    budget.BeforeWait();
                    yield return endFrame;
                }
                budget.Complete();
                foreach (var row in f.Rows)
                {
                    row.Restored("Retrigger cycle " + cycle);
                    Check(row.Button.label.GetComponents<XgJuice>().Length == 1 && row.Fx == row.FirstFx,
                        "Real-frame retriggers reuse one private component: " + row.Name);
                    row.Up();
                }
            }

            var first = f.Rows[0];
            first.Down();
            yield return endFrame;
            first.Host.Reduced = true;
            yield return endFrame; // Production LateUpdate registers the quiet return.
            float lastDistance = first.Distance();
            budget = new MotionFrameBudget("reduced-return", .06f, waits);
            while (first.Active)
            {
                budget.BeforeWait();
                yield return endFrame;
                float distance = first.Distance();
                Check(distance <= lastDistance + .0001f, "Reduced return never increases deformation after registration");
                lastDistance = distance;
                f.Sample(samples, "reduced-return");
            }
            budget.Complete();
            first.Restored("Reduced switch"); first.Up(); first.Host.Reduced = false;

            first.Down();
            yield return endFrame;
            first.Host.Visible = () => false;
            yield return endFrame;
            first.Restored("Host visibility polling");
            first.Down(false);
            Check(!first.Active, "Hidden host refuses a new private pulse");
            first.Up(); first.Host.Visible = () => true;
            first.Down();
            yield return endFrame;
            first.Button.rt.gameObject.SetActive(false);
            first.Restored("Button deactivation");
            first.Button.rt.gameObject.SetActive(true);
            yield return endFrame;
            Check(!first.Active, "Reopening a button does not replay its old pulse");

            // Real-frame completion must preserve an external absolute single-axis layout write.
            first.Down();
            yield return endFrame;
            var position = first.Button.label.rectTransform.anchoredPosition;
            position.x = 81; first.Button.label.rectTransform.anchoredPosition = position;
            budget = new MotionFrameBudget("layout", .18f, waits);
            while (first.Active)
            {
                budget.BeforeWait();
                yield return endFrame;
            }
            budget.Complete();
            Check(first.Button.label.rectTransform.anchoredPosition == new Vector2(81, 3) &&
                first.Button.label.rectTransform.localScale == MotionFrameRow.RestScale,
                "Real-frame completion preserves only the changed layout axis");
            first.Button.label.rectTransform.anchoredPosition = MotionFrameRow.RestPosition;
            first.Up();

            var shared = f.Root.AddComponent<XgJuice>();
            var early = f.Target("Early"); var late = f.Target("Late"); var removed = f.Target("Removed");
            shared.Knock(early, seconds: .06f);
            shared.Knock(late, seconds: .45f);
            shared.Knock(removed, seconds: .45f);
            budget = new MotionFrameBudget("shared-late", .45f, waits, shared, late);
            var earlyBudget = new MotionFrameBudget("shared-early", .06f, waits, shared, early);
            yield return endFrame;
            UnityEngine.Object.Destroy(removed.gameObject);
            while (shared.HasActiveKnock(early))
            {
                earlyBudget.BeforeWait(); budget.BeforeWait();
                yield return endFrame;
            }
            earlyBudget.Complete(); budget.BeforeWait();
            yield return endFrame; // Observe normal deferred Destroy and the following production Update.
            Check(removed == null && shared.HasActiveKnock(late), "Deferred target destruction preserves a later active target");
            Check(MotionPunchCount(shared) == 1 && MotionCompletedCount(shared) == 0,
                "Partial completion and deferred destruction release only finished references");
            var cached = MotionCompleted(shared);
            while (shared.HasActiveKnock(late))
            {
                budget.BeforeWait();
                yield return endFrame;
            }
            budget.Complete();
            Check(early.localScale == Vector3.one && late.localScale == Vector3.one && MotionPunchCount(shared) == 0,
                "Shared targets finish and restore across natural frames");
            shared.Knock(early, seconds: .06f); shared.Knock(late, seconds: .06f);
            budget = new MotionFrameBudget("shared-simultaneous", .06f, waits, shared, late);
            while (shared.HasActiveKnock(early) || shared.HasActiveKnock(late))
            {
                budget.BeforeWait();
                yield return endFrame;
            }
            budget.Complete();
            Check(ReferenceEquals(cached, MotionCompleted(shared)) && MotionCompletedCount(shared) == 0,
                "Shared simultaneous completion reuses the same emptied scratch collection");
            Check(Time.timeScale == 0, "All real-frame cases complete with the simulation clock paused");
            yield return endFrame;
            MotionScreenshot("motion-final");
            Check(true, "Motion real-frame checks complete; screenshots require visual review, hardware input and full-page business results remain separate");
        }
        finally
        {
            Time.timeScale = previousScale;
            File.WriteAllText(Path.Combine(Output, "motion-frame-samples.csv"), samples.ToString());
            File.WriteAllText(Path.Combine(Output, "motion-wait-samples.csv"), waits.ToString());
            f.Dispose();
        }
    }

    sealed class MotionFrameBudget
    {
        readonly string phase;
        readonly float life, began = Time.realtimeSinceStartup;
        readonly StringBuilder trace;
        readonly XgJuice shared;
        readonly RectTransform target;
        int lastFrame = Time.frameCount, frames;
        float elapsed;
        public MotionFrameBudget(string phase, float life, StringBuilder trace, XgJuice shared = null, RectTransform target = null)
        { this.phase = phase; this.life = life; this.trace = trace; this.shared = shared; this.target = target; }
        public void BeforeWait()
        {
            // A fixed frame count is not a time limit in the uncapped Editor: 120 frames
            // can be shorter than a valid .45-second Knock. Observe the production clock
            // contract, allow at most one capped frame for completion, and retain the
            // independent wall-clock timeout. Never drive or rewrite the production clock.
            if (lastFrame != Time.frameCount)
            {
                lastFrame = Time.frameCount; frames++;
                elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
                string age = "", duration = "";
                if (shared != null && target != null)
                {
                    var punches = (IDictionary)typeof(XgJuice).GetField("punches", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shared);
                    var punch = punches[target];
                    if (punch != null)
                    {
                        age = ((float)punch.GetType().GetField("t").GetValue(punch)).ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                        duration = ((float)punch.GetType().GetField("life").GetValue(punch)).ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                trace.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3:F6},{4:F6},{5},{6}\n",
                    phase, lastFrame, frames, Time.realtimeSinceStartup - began, elapsed, age, duration);
            }
            if (elapsed > life + .0501f || Time.realtimeSinceStartup - began > 5)
                Check(false, phase + " exceeded its completion budget: frames=" + frames +
                    ", cappedSeconds=" + elapsed + ", life=" + life + ", wallSeconds=" + (Time.realtimeSinceStartup - began));
        }
        public void Complete()
        {
            BeforeWait();
            Check(true, phase + " completed within its animation and 5-second wall-clock budgets after " + frames + " frames");
        }
    }

    static object MotionCompleted(XgJuice fx) => typeof(XgJuice).GetField("completedPunches", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fx);
    static int MotionCompletedCount(XgJuice fx) => ((IList)MotionCompleted(fx)).Count;
    static int MotionPunchCount(XgJuice fx) => ((IDictionary)typeof(XgJuice).GetField("punches", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fx)).Count;
    static void MotionScreenshot(string name)
    {
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG()); }
        finally { UnityEngine.Object.Destroy(texture); }
    }

    sealed class MotionFrameFixture : IDisposable
    {
        public readonly GameObject Root;
        public readonly TMP_FontAsset Font;
        public readonly MotionFrameRow[] Rows = new MotionFrameRow[4];
        readonly GameObject events;
        public MotionFrameFixture()
        {
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
            Check(Font != null, "Actual Desktop CJK font is installed");
            Root = new GameObject("MotionFrameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = Root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            events = new GameObject("MotionFrameEvents", typeof(EventSystem));
            for (int i = 0; i < Rows.Length; i++)
            {
                bool dark = i % 2 == 0, reduced = i >= 2;
                var panel = XgUi.Rect("MotionRow" + i, Root.transform, new Vector2(0, 1 - (i + 1) * .25f), new Vector2(1, 1 - i * .25f), Vector2.zero, Vector2.zero);
                XgUi.Panel(panel, dark ? XgDark.Page : XgPalette.Page);
                var host = panel.gameObject.AddComponent<XgJuice>(); host.Reduced = reduced;
                var ui = new XgUi(Font, null, dark);
                var button = ui.Button(panel, "开始训练 · 确认购买 Aa09", null, 24);
                button.rt.anchorMin = button.rt.anchorMax = new Vector2(.5f, .5f);
                button.rt.sizeDelta = new Vector2(560, 76);
                button.button.navigation = new Navigation { mode = Navigation.Mode.None };
                Rows[i] = new MotionFrameRow((dark ? "dark" : "light") + (reduced ? "-reduced" : "-normal"), button, host, events.GetComponent<EventSystem>());
                var caption = XgUi.Rect("Caption", panel, new Vector2(.03f, .76f), new Vector2(.97f, .99f), Vector2.zero, Vector2.zero);
                ui.Text(caption, Rows[i].Name, 18, dark ? XgDark.Ink : XgPalette.Ink, TextAlignmentOptions.Left);
            }
        }
        public bool AnyActive() { foreach (var row in Rows) if (row.Active) return true; return false; }
        public RectTransform Target(string name)
        {
            var target = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            target.SetParent(Root.transform, false); return target;
        }
        public void Sample(StringBuilder text, string phase)
        {
            foreach (var row in Rows)
            {
                var rt = row.Button.label.rectTransform;
                row.SawDeformation |= row.Distance() > .0001f;
                text.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2:F6},{3},{4:F6},{5:F6},{6:F6},{7:F6},{8}\n",
                    phase, Time.frameCount, Time.unscaledTime, row.Name, rt.localScale.x, rt.localScale.y, rt.anchoredPosition.x, rt.anchoredPosition.y, row.Active);
            }
        }
        public void Dispose() { UnityEngine.Object.Destroy(Root); UnityEngine.Object.Destroy(events); }
    }

    sealed class MotionFrameRow
    {
        public static readonly Vector3 RestScale = new Vector3(1.2f, .9f, 1);
        public static readonly Vector2 RestPosition = new Vector2(7, 3);
        public readonly string Name;
        public readonly XgBtn Button;
        public readonly XgJuice Host;
        public XgJuice FirstFx;
        public bool SawDeformation;
        readonly PointerEventData pointer;
        public MotionFrameRow(string name, XgBtn button, XgJuice host, EventSystem events)
        {
            Name = name; Button = button; Host = host;
            button.label.rectTransform.localScale = RestScale;
            button.label.rectTransform.anchoredPosition = RestPosition;
            pointer = new PointerEventData(events) { button = PointerEventData.InputButton.Left, pointerId = 11 };
        }
        public XgJuice Fx => Button.label.GetComponent<XgJuice>();
        public bool Active => Fx != null && Fx.enabled;
        public void Down(bool expectPulse = true)
        {
            ExecuteEvents.Execute(Button.rt.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            if (FirstFx == null) FirstFx = Fx;
            if (expectPulse) Check(Active && Fx.HasActiveKnock(Button.label.rectTransform), "Input starts a private pulse: " + Name);
        }
        public void Up() => ExecuteEvents.Execute(Button.rt.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        public float Distance() => Vector3.Distance(Button.label.rectTransform.localScale, RestScale) + Vector2.Distance(Button.label.rectTransform.anchoredPosition, RestPosition);
        public void Restored(string phase) => Check(!Active && Button.label.rectTransform.localScale == RestScale &&
            Button.label.rectTransform.anchoredPosition == RestPosition, phase + " restores and disables the private pulse: " + Name);
    }
}
