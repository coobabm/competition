using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LingGuangV05.EditorTools
{
    /// <summary>Manual Play probes only. Never switches save mode or touches player save files.</summary>
    public static class XgWiringWorkbenchQa
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Serializable] public sealed class LayoutReport
        {
            public float[] rootRect;
            public int nodeCount, inspectedNodes, textCount;
            public float zoom;
            public string selected, armed, workspaceFilter, state;
            public bool invariantsRun, passed;
            public List<string> invalidRects = new List<string>(), failures = new List<string>();
            public List<string> textOverflow = new List<string>(), boundedClipping = new List<string>();
        }

        public static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Wiring QA: " + message);
        }

        static ChapterOneRuntime Runtime()
        {
            Assert(EditorApplication.isPlaying, "requires Play Mode");
            var runtime = Object.FindAnyObjectByType<ChapterOneRuntime>();
            Assert(runtime != null, "ChapterOneRuntime missing");
            Assert(!runtime.useDiskSave, "refused: useDiskSave must already be false");
            Assert(!runtime.TestMode, "Use normal Play with disk saves off: TestMode disables real host execution");
            return runtime;
        }

        static XingGuangController Controller()
        {
            var controller = Runtime().GetComponent<XingGuangController>();
            Assert(controller != null && controller.View != null && controller.Sim != null, "lab is not bound");
            Assert(controller.View.Wiring != null && controller.View.Wiring.root != null, "wiring page missing");
            return controller;
        }

        /// <summary>Uses the existing safe fixture; caller can choose the new page's workspace filter.</summary>
        public static string SetupFocused()
        {
            Runtime();
            return XgWiringPlayQa.Setup() + "; full memory fixture retained; choose workspace filter for focused capture";
        }

        static Vector2 Physical(AeroLcdInputSurface input, Vector3 world)
        {
            Assert(input.sourceCamera != null && input.displayRect != null, "LCD camera/display missing");
            var camera = input.sourceCamera;
            var target = camera.targetTexture;
            float width = target != null ? target.width : camera.pixelWidth;
            float height = target != null ? target.height : camera.pixelHeight;
            Assert(width > 0 && height > 0, "LCD source dimensions invalid");
            Vector2 source = RectTransformUtility.WorldToScreenPoint(camera, world);
            Vector2 uv = new Vector2(source.x / width, source.y / height);
            if (input.flipY) uv.y = 1 - uv.y;
            var rect = input.displayRect.rect;
            Assert(rect.width > 0 && rect.height > 0, "LCD display dimensions invalid");
            var local = new Vector2(rect.xMin + uv.x * rect.width, rect.yMin + uv.y * rect.height);
            var canvas = input.displayRect.GetComponentInParent<Canvas>();
            var displayCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(displayCamera, input.displayRect.TransformPoint(local));
        }

        /// <summary>Real LCD event route, not direct node/pan callbacks. Synchronous: caller waits frames for inertia.</summary>
        public static string Drag(Vector3 fromWorld, Vector3 toWorld)
        {
            Runtime();
            var input = Object.FindAnyObjectByType<AeroLcdInputSurface>();
            Assert(input != null && input.isActiveAndEnabled && input.inputEnabled, "LCD input unavailable");
            Assert(EventSystem.current != null, "EventSystem missing");
            Vector2 from = Physical(input, fromWorld), to = Physical(input, toWorld);
            Assert(input.TryMap(from, out var sourceFrom), "drag must start inside LCD");
            input.TryMap(to, out var sourceTo); // A captured drag may finish beyond the screen edge.
            float distance = Vector2.Distance(sourceFrom, sourceTo);
            Assert(distance > EventSystem.current.pixelDragThreshold + 1, "drag is shorter than source-pixel threshold");
            var data = new PointerEventData(EventSystem.current)
                { pointerId = -1, button = PointerEventData.InputButton.Left, position = from };
            input.OnPointerEnter(data);
            input.OnPointerMove(data);
            try
            {
                input.OnPointerDown(data); // Bridge initializes the desktop's potential drag itself.
                float start = (EventSystem.current.pixelDragThreshold + 1) / distance;
                data.position = Vector2.Lerp(from, to, start);
                input.OnBeginDrag(data); // First motion crosses threshold and establishes XgPan's local origin.
                for (int i = 1; i <= 8; i++)
                {
                    data.position = Vector2.Lerp(from, to, Mathf.Lerp(start, 1, i / 8f));
                    input.OnPointerMove(data);
                    input.OnDrag(data);
                }
            }
            finally
            {
                input.OnPointerUp(data); // Owns the synthetic desktop endDrag, as in the real input module.
                input.OnEndDrag(data);   // Physical endDrag intentionally does not send another desktop event.
            }
            return "drag dispatched through LCD: " + from + " -> " + to + "; inspect resulting state separately";
        }

        static object Read(object target, string member)
        {
            if (target == null) return null;
            var type = target.GetType();
            return type.GetProperty(member, Flags)?.GetValue(target) ?? type.GetField(member, Flags)?.GetValue(target);
        }

        static string ReadText(object target, string member, string fallback = "") => Read(target, member)?.ToString() ?? fallback;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Valid(Rect rect) => Finite(rect.x) && Finite(rect.y) && Finite(rect.width) && Finite(rect.height) && rect.width >= 0 && rect.height >= 0;
        static string Path(Transform item, Transform root) => item == root || item.parent == null ? item.name : Path(item.parent, root) + "/" + item.name;

        static List<RectTransform> Nodes(XgWiringPage page)
        {
            var nodes = new List<RectTransform>();
            var views = Read(page, "views") as IDictionary;
            if (views != null)
                foreach (var value in views.Values)
                    if (Read(value, "rt") is RectTransform rect && rect.gameObject.activeInHierarchy) nodes.Add(rect);
            if (nodes.Count == 0)
                foreach (var rect in page.root.GetComponentsInChildren<RectTransform>())
                    if (rect.name.StartsWith("Node ", StringComparison.Ordinal) || rect.name.StartsWith("Fold ", StringComparison.Ordinal)) nodes.Add(rect);
            return nodes;
        }

        static LayoutReport Inspect(float minimumNodeWidth, float minimumNodeHeight, bool invariants)
        {
            var controller = Controller();
            var page = controller.View.Wiring;
            Canvas.ForceUpdateCanvases();
            var root = page.root;
            var r = root.rect;
            var nodes = Nodes(page);
            var pan = root.GetComponentInChildren<XgPan>();
            var report = new LayoutReport
            {
                rootRect = new[] { r.x, r.y, r.width, r.height }, inspectedNodes = nodes.Count,
                nodeCount = Read(page, "QaVisibleNodeCount") is int count ? count : nodes.Count,
                zoom = Read(page, "QaZoom") is float zoom ? zoom : pan != null && pan.content != null ? pan.content.localScale.x : -1,
                selected = ReadText(page, "QaSelected", ReadText(page, "selected")),
                armed = ReadText(page, "QaArmed", ReadText(page, "armed")),
                workspaceFilter = ReadText(page, "QaWorkspaceFilter", "unavailable"),
                state = "tab=" + controller.View.Tab + "; stage=" + controller.Sim.S.stage + "; running=" + controller.Sim.Wiring.running
                    + "; wired=" + controller.Sim.Wiring.wired + "; tripped=" + controller.Sim.Wiring.tripped
            };
            foreach (var rect in root.GetComponentsInChildren<RectTransform>())
                if (!Valid(rect.rect)) report.invalidRects.Add(Path(rect, root) + " " + rect.rect);
            foreach (var text in root.GetComponentsInChildren<TMP_Text>())
            {
                if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
                report.textCount++;
                text.ForceMeshUpdate();
                var bounds = text.textBounds;
                bool outside = bounds.size.x > text.rectTransform.rect.width + 1 || bounds.size.y > text.rectTransform.rect.height + 1;
                if (!text.isTextOverflowing && !text.isTextTruncated && !outside) continue;
                string detail = Path(text.transform, root) + " [" + text.overflowMode + "] " + text.text;
                if (text.overflowMode == TextOverflowModes.Overflow) report.textOverflow.Add(detail);
                else report.boundedClipping.Add(detail); // Exposed for review, not assumed to be a design defect.
            }
            if (invariants)
            {
                Check(report, root.gameObject.activeInHierarchy && r.width > 0 && r.height > 0, "active positive-sized root required");
                Check(report, report.invalidRects.Count == 0, "negative/nonfinite RectTransform dimensions");
                Check(report, nodes.Count > 0, "no active node rects discovered");
                foreach (var node in nodes)
                    Check(report, node.rect.width >= minimumNodeWidth && node.rect.height >= minimumNodeHeight, "node too small: " + node.name + " " + node.rect.size);
                Check(report, pan != null && pan.content != null && pan.viewport != null, "XgPan/content/viewport missing");
                Check(report, Finite(report.zoom) && report.zoom > 0, "zoom must be finite and positive");
                Check(report, root.GetComponentsInChildren<Button>().Length >= 3, "interactive workbench controls missing");
                bool graphic = false;
                foreach (var component in root.GetComponentsInChildren<Graphic>()) if (component.GetType().Name == "XgWiringGraphic") graphic = true;
                Check(report, graphic, "XgWiringGraphic missing");
                foreach (string member in new[] { "QaSelected", "QaArmed", "QaZoom", "QaVisibleNodeCount", "QaWorkspaceFilter" })
                    Check(report, page.GetType().GetProperty(member, Flags) != null, member + " API missing");
                Check(report, report.textOverflow.Count == 0, "uncontrolled TMP overflow; see textOverflow");
            }
            report.invariantsRun = invariants;
            report.passed = invariants && report.failures.Count == 0;
            return report;
        }

        static void Check(LayoutReport report, bool condition, string message) { if (!condition) report.failures.Add(message); }
        public static string Snapshot() => JsonUtility.ToJson(Inspect(0, 0, false), true);
        public static string InvariantChecks(float minimumNodeWidth = 100, float minimumNodeHeight = 32) =>
            JsonUtility.ToJson(Inspect(minimumNodeWidth, minimumNodeHeight, true), true);
    }
}
