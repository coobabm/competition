using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public static partial class DesignWallpaperSmoke
{
    // Candidate: the shared dispatcher opens the real Main scene and supplies its isolated runtime.
    public static void RunWiringScene() { Start("wiring-scene"); }

    static void StartWiringSceneChecks(XingGuangController controller, ChapterOneRuntime runtime)
    {
        due = double.PositiveInfinity;
        controller.StartCoroutine(WiringSceneFrames(controller, runtime));
    }

    static IEnumerator WiringSceneFrames(XingGuangController controller, ChapterOneRuntime runtime)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(WiringSceneSteps(controller, runtime));
        Exception failure = null;
        while (stack.Count > 0)
        {
            object current = null; bool more = false;
            try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
            catch (Exception error) { failure = error; }
            if (failure != null) break;
            if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
            if (current is IEnumerator nested) { stack.Push(nested); continue; }
            yield return current;
        }
        while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
        File.WriteAllText(Path.Combine(Output, "result.txt"), failure == null ? "PASS" : "FAIL: " + failure);
        Finish(failure == null ? 0 : 1);
    }

    static IEnumerator WiringSceneSteps(XingGuangController controller, ChapterOneRuntime runtime)
    {
        Check(runtime != null && runtime.TestMode && !runtime.useDiskSave && controller?.View?.Wiring != null,
            "Wiring scene uses the common isolated Main scene, real controller and production page");
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        Check(bridge != null && bridge.sourceCamera != null && bridge.sourceCanvas != null && bridge.inputSurface != null,
            "Main owns the rendered LCD source canvas and real input relay");
        var page = controller.View.Wiring;
        var sim = controller.Sim;
        Check(EventSystem.current != null && bridge.inputSurface.inputEnabled && bridge.sourceCamera.targetTexture != null,
            "Main has its active event system, powered LCD input and rendered source texture");
        bool oldTick = runtime.tickEnabled, oldController = controller.enabled, oldView = controller.View.enabled, oldTest = runtime.TestMode;
        bool oldSurface = bridge.inputSurface.enabled;
        int oldStage = sim.S.stage;
        var modules = EventSystem.current.GetComponents<BaseInputModule>();
        var enabledModules = new bool[modules.Length];
        var observers = new List<KeyValuePair<Button, UnityAction>>();
        int logCount = 0; XgWLog lastLog = null;
        Action<XgWLog> logged = value => { logCount++; lastLog = value; };
        try
        {
            runtime.tickEnabled = false; controller.enabled = false; controller.View.enabled = true;
            // The real Host refuses wallet/compute reads in TestMode. Keep disk I/O and both simulation ticks
            // disabled while enabling that actual adapter for this isolated scene, as in the Growth/Tech checks.
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { false });
            sim.S.stage = Math.Max(2, sim.S.stage); // Isolated access fixture; no extra hardware or fake graph nodes.
            for (int i = 0; i < modules.Length; i++) { enabledModules[i] = modules[i].enabled; modules[i].enabled = false; }
            bridge.inputSurface.enabled = true;
            Check(!runtime.useDiskSave && !runtime.tickEnabled && !controller.enabled && controller.Host is XingGuangHost,
                "Live resource reads keep real saves and simulation progression disabled");
            var graph = sim.RefreshWiring(controller.Host);
            var gpu = graph.nodes.Find(n => n.lane == XgWireLane.Gpu && n.house);
            Check(graph.hasRig && gpu != null && !gpu.unlimited && FiniteWiring(gpu.cu) && FiniteWiring(gpu.vramMB)
                && gpu.cu > 0 && gpu.vramMB > 0 && FiniteWiring(graph.psuLimit) && graph.psuLimit > 0,
                "The scene uses an installed real GPU with finite CU, VRAM and household power");
            File.WriteAllText(Path.Combine(Output, "wiring-scene-input.txt"), "GPU=" + gpu.key + " CU=" + gpu.cu
                + " VRAM=" + gpu.vramMB + " power=" + graph.psuLimit + " money=" + controller.Host.Money + "\n");
            // Set up an unplugged starting state with the actual rules. Every connection under test is relay input.
            Check(sim.Disconnect("psu", gpu.key, controller.Host), "Actual rules prepare the real starter GPU unplugged");
            controller.View.ShowTab("wiring");
            Check(controller.View.Tab == "wiring" && page.root.gameObject.activeInHierarchy,
                "The real application opens its existing Wiring page");
            yield return new WaitForSecondsRealtime(.8f);
            yield return WiringSceneDismissNotifications(bridge);
            yield return new WaitForEndOfFrame();
            var windowFit = page.root.GetComponentInParent<UiFitScale>();
            Check(windowFit != null && FiniteWiring(windowFit.Scale) && windowFit.Scale > 0
                && controller.Window.windowContainer.gameObject.activeInHierarchy,
                "Wiring retains the actual scaled application window instead of a standalone overlay");
            File.AppendAllText(Path.Combine(Output, "wiring-scene-input.txt"), "Source=" + bridge.sourceCamera.targetTexture.width
                + "x" + bridge.sourceCamera.targetTexture.height + " screen=" + Screen.width + "x" + Screen.height
                + " window=" + controller.Window.windowContainer.rect.size + " fit=" + windowFit.Scale + "\n");
            var node = WiringSceneNode(page, "psu"); var gpuNode = WiringSceneNode(page, gpu.key);
            var nodeRect = WiringSceneField<RectTransform>(node, "rt");
            var output = WiringSceneField<RectTransform>(node, "outPort");
            var input = WiringSceneField<RectTransform>(gpuNode, "inPort");
            var wrongOutput = WiringSceneField<RectTransform>(gpuNode, "outPort");
            var nodeButton = nodeRect.GetComponent<XgButtonPressFeedback>();
            var outButton = output.GetComponent<XgButtonPressFeedback>();
            Check(nodeButton != null && outButton != null && input.GetComponent<XgButtonPressFeedback>() != null,
                "The real page installs the reviewed feedback controls on its node and ports");
            foreach (var label in WiringSceneField<TMP_Text[]>(page, "values"))
                Check(!label.text.Contains("Infinity") && !label.text.Contains("∞") && !label.text.Contains("NaN"),
                    "Actual resource metrics contain finite presentation values: " + label.text);
            WiringSceneCapture("wiring-scene-rest");
            sim.WiringLogged += logged;

            int nodeClicks = 0, portClicks = 0;
            WiringSceneObserve(observers, nodeButton, () => nodeClicks++);
            WiringSceneObserve(observers, outButton, () => portClicks++);
            WiringSceneObserve(observers, input.GetComponent<Button>(), () => portClicks++);
            WiringSceneObserve(observers, wrongOutput.GetComponent<Button>(), () => portClicks++);
            var restLabel = nodeButton.Label.anchoredPosition; var restScale = nodeButton.Label.localScale;
            var nodePosition = nodeRect.anchoredPosition;
            var pointer = WiringScenePress(bridge, nodeRect.TransformPoint(nodeRect.rect.center), nodeButton.gameObject, "node press");
            Check(nodeClicks == 0, "Real node pointer-down does not invoke its business callback");
            yield return null; yield return new WaitForEndOfFrame();
            Check(nodeButton.Label.anchoredPosition != restLabel || nodeButton.Label.localScale != restScale,
                "The actual node title deforms on a natural frame while held");
            Check(nodeRect.anchoredPosition == nodePosition, "Node press feedback leaves the actual hit rect stationary");
            WiringSceneCapture("wiring-scene-node-pressed");
            bridge.inputSurface.OnPointerUp(pointer);
            Check(nodeClicks == 1 && page.QaSelected == "psu" && WiringSceneField<RectTransform>(page, "detail").gameObject.activeSelf,
                "LCD node release selects the actual PSU and opens its inspector once");
            yield return new WaitForSecondsRealtime(.25f);

            var outPosition = output.anchoredPosition; var outScale = output.localScale; var outSize = output.rect.size;
            var outRest = outButton.Label.anchoredPosition; var outRestScale = outButton.Label.localScale;
            pointer = WiringScenePress(bridge, WiringScenePort(page, "psu", "out"), outButton.gameObject, "output press");
            yield return null; yield return new WaitForEndOfFrame();
            Check(outButton.Label.anchoredPosition != outRest || outButton.Label.localScale != outRestScale,
                "The actual output-port visual deforms through the existing private feedback component");
            Check(output.anchoredPosition == outPosition && output.localScale == outScale && output.rect.size == outSize,
                "Real output press leaves the port's hit geometry unchanged");
            WiringSceneAnchor(page, "psu", "out", output);
            WiringSceneRenderedMesh(page);
            WiringSceneCapture("wiring-scene-output-pressed");
            bridge.inputSurface.OnPointerUp(pointer);
            Check(page.QaArmed == "psu" && portClicks == 1, "LCD output click arms that real port exactly once");
            int beforeLog = logCount;
            WiringSceneClick(bridge, WiringScenePort(page, gpu.key, "in"), input.gameObject, "input connect");
            Check(sim.Wiring.Node(gpu.key).source == "psu" && page.QaArmed == null && logCount == beforeLog + 1 && portClicks == 2,
                "Actual LCD port clicks connect the real starter GPU once");
            yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame();
            WiringSceneRenderedMesh(page); WiringSceneCapture("wiring-scene-connected");

            WiringSceneClick(bridge, WiringScenePort(page, gpu.key, "out"), wrongOutput.gameObject, "wrong source arm");
            beforeLog = logCount;
            WiringSceneClick(bridge, WiringScenePort(page, gpu.key, "in"), input.gameObject, "wrong target click");
            Check(logCount == beforeLog + 1 && lastLog != null && lastLog.tone >= 3 && page.QaArmed == gpu.key
                && sim.Wiring.Node(gpu.key).source == "psu" && !string.IsNullOrEmpty(WiringSceneField<string>(page, "hint")),
                "Actual wrong-target clicks refuse once, preserve the connection and keep the retry source");
            yield return new WaitForEndOfFrame();
            WiringSceneCapture("wiring-scene-refused");
            var blank = WiringSceneBlank(bridge, page);
            WiringSceneClick(bridge, blank, WiringSceneField<XgWiringGraphic>(page, "back").gameObject, "blank cancel");
            Check(page.QaArmed == null && sim.Wiring.Node(gpu.key).source == "psu" && logCount == beforeLog + 1,
                "A real blank-canvas click cancels the armed port without touching any cable or connection");
            yield return new WaitForSecondsRealtime(.25f);

            nodePosition = nodeRect.anchoredPosition;
            int beforeNodeClicks = nodeClicks;
            var start = bridge.sourceCamera.WorldToScreenPoint(nodeRect.TransformPoint(nodeRect.rect.center));
            pointer = WiringScenePress(bridge, nodeRect.TransformPoint(nodeRect.rect.center), nodeButton.gameObject, "node drag press");
            WiringSceneMove(bridge, pointer, (Vector2)start + new Vector2(20, 0), true);
            yield return null;
            WiringSceneMove(bridge, pointer, (Vector2)start + new Vector2(55, -20), false);
            yield return null; yield return new WaitForEndOfFrame();
            Check(nodeRect.anchoredPosition != nodePosition && WiringSceneField<bool>(page, "draggingNode"),
                "The real LCD relay crosses its drag threshold and moves the existing node");
            WiringSceneAnchor(page, "psu", "out", output);
            WiringSceneCapture("wiring-scene-node-dragged");
            bridge.inputSurface.OnPointerUp(pointer); bridge.inputSurface.OnEndDrag(pointer);
            Check(!WiringSceneField<bool>(page, "draggingNode") && nodeClicks == beforeNodeClicks,
                "The LCD drag release ends node dragging without a false click");
            var movedNode = nodeRect.anchoredPosition;
            yield return new WaitForSecondsRealtime(.25f);
            Check(nodeRect.anchoredPosition == movedNode, "Ending feedback does not undo the player's actual node drag");

            for (int mode = 0; mode < 3; mode++)
            {
                if (mode == 2)
                {
                    Check(sim.Disconnect("psu", gpu.key, controller.Host), "Actual rules prepare a fresh unplugged drag fixture");
                    page.Refresh(); yield return null;
                }
                var source = mode == 1 ? wrongOutput : output;
                string sourceKey = mode == 1 ? gpu.key : "psu";
                var from = WiringScenePort(page, sourceKey, "out");
                var to = mode == 0 ? WiringSceneBlank(bridge, page) : WiringScenePort(page, gpu.key, "in");
                var toSource = (Vector2)bridge.sourceCamera.WorldToScreenPoint(to);
                var fromSource = (Vector2)bridge.sourceCamera.WorldToScreenPoint(from);
                var initialSource = sim.Wiring.Node(gpu.key).source;
                int initialLog = logCount, initialClicks = portClicks;
                var sourcePosition = source.anchoredPosition; var targetPosition = input.anchoredPosition;
                pointer = WiringScenePress(bridge, from, source.gameObject, "port drag " + mode);
                WiringSceneMove(bridge, pointer, Vector2.Lerp(fromSource, toSource, .2f), true);
                yield return null;
                WiringSceneMove(bridge, pointer, toSource, false);
                Check(page.QaArmed == sourceKey && WiringSceneField<bool>(page, "draggingPort"),
                    "Actual port relay retains the correct dragged source: case " + mode);
                yield return null; yield return new WaitForEndOfFrame();
                WiringSceneAnchor(page, sourceKey, "out", source); WiringSceneAnchor(page, gpu.key, "in", input);
                WiringSceneRenderedMesh(page);
                if (mode == 2) WiringSceneCapture("wiring-scene-cable-dragging");
                bridge.inputSurface.OnPointerUp(pointer); bridge.inputSurface.OnEndDrag(pointer);
                Check(page.QaArmed == null && !WiringSceneField<bool>(page, "draggingPort") && portClicks == initialClicks,
                    "LCD release ends the actual port drag and does not fabricate a click: case " + mode);
                if (mode == 0) Check(logCount == initialLog && sim.Wiring.Node(gpu.key).source == initialSource,
                    "Dropping on real blank canvas cancels without changing the graph");
                else if (mode == 1) Check(logCount == initialLog + 1 && lastLog.tone >= 3 && sim.Wiring.Node(gpu.key).source == initialSource,
                    "A real same-node drop refuses once and preserves the graph");
                else Check(logCount == initialLog + 1 && sim.Wiring.Node(gpu.key).source == "psu",
                    "A real source-to-target drop changes the unplugged GPU to connected exactly once");
                yield return new WaitForSecondsRealtime(.25f);
                Check(source.anchoredPosition == sourcePosition && input.anchoredPosition == targetPosition,
                    "Real port dragging and settling keep both hit positions fixed");
            }
            yield return new WaitForEndOfFrame();
            WiringSceneRenderedMesh(page); WiringSceneCapture("wiring-scene-drop-connected");
            Check(ReferenceEquals(node, WiringSceneNode(page, "psu")) && ReferenceEquals(gpuNode, WiringSceneNode(page, gpu.key)),
                "All actual actions retain the existing node views");
            Check(!runtime.useDiskSave && !runtime.tickEnabled && !controller.enabled,
                "The real scene interaction checks finish without saving or advancing the simulation");
            Check(true, "WIRING_MAIN_SCENE_PASS: real finite rig, source-canvas raycasts, LCD clicks/drags and rendered screenshots; visual review required");
        }
        finally
        {
            sim.WiringLogged -= logged;
            foreach (var observer in observers) if (observer.Key != null) observer.Key.onClick.RemoveListener(observer.Value);
            bridge.inputSurface.CancelInteraction(); bridge.inputSurface.enabled = oldSurface;
            for (int i = 0; i < modules.Length; i++) if (modules[i] != null) modules[i].enabled = enabledModules[i];
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { oldTest });
            sim.S.stage = oldStage;
            runtime.tickEnabled = oldTick; controller.enabled = oldController; controller.View.enabled = oldView;
        }
    }

    static bool FiniteWiring(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    static void WiringSceneObserve(List<KeyValuePair<Button, UnityAction>> observers, Button button, UnityAction action)
    {
        button.onClick.AddListener(action); observers.Add(new KeyValuePair<Button, UnityAction>(button, action));
    }
    static T WiringSceneField<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(owner);
    static object WiringSceneNode(XgWiringPage page, string key) => WiringSceneField<IDictionary>(page, "views")[key];
    static Vector3 WiringScenePort(XgWiringPage page, string key, string side)
    {
        Check(page.QaPoint(key, side, true, out var point), "Actual source-canvas port exists: " + key + "/" + side);
        return point;
    }
    static void WiringSceneAnchor(XgWiringPage page, string key, string side, RectTransform hit)
    {
        Check(Vector3.Distance(WiringScenePort(page, key, side), hit.TransformPoint(hit.rect.center)) < .001f,
            "Actual cable anchor matches its port hit center: " + key + "/" + side);
    }

    static Vector2 WiringScenePhysical(AeroLcdSourceBridge bridge, Vector2 source)
    {
        var surface = bridge.inputSurface;
        var texture = bridge.sourceCamera.targetTexture;
        var uv = new Vector2(source.x / texture.width, source.y / texture.height);
        if (surface.flipY) uv.y = 1 - uv.y;
        var rect = surface.displayRect;
        var displayCanvas = rect.GetComponentInParent<Canvas>();
        var camera = displayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : displayCanvas.worldCamera;
        var physical = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.min + Vector2.Scale(rect.rect.size, uv)));
        Check(surface.TryMap(physical, out var mapped) && Vector2.Distance(mapped, source) < .5f,
            "LCD source/display coordinate mapping round-trips at " + source);
        return physical;
    }
    static PointerEventData WiringScenePointer(AeroLcdSourceBridge bridge, Vector3 world)
        => new PointerEventData(EventSystem.current) { pointerId = -1, button = PointerEventData.InputButton.Left,
            position = WiringScenePhysical(bridge, bridge.sourceCamera.WorldToScreenPoint(world)) };
    static GameObject WiringSceneHover(AeroLcdSourceBridge bridge, PointerEventData pointer)
    {
        bridge.inputSurface.OnPointerEnter(pointer);
        var hover = WiringSceneField<PointerEventData>(bridge.inputSurface, "hoverData");
        return hover?.pointerCurrentRaycast.gameObject;
    }
    static PointerEventData WiringScenePress(AeroLcdSourceBridge bridge, Vector3 world, GameObject expected, string context)
    {
        var pointer = WiringScenePointer(bridge, world);
        var hit = WiringSceneHover(bridge, pointer);
        var receiver = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
        File.AppendAllText(Path.Combine(Output, "wiring-scene-input.txt"), context + " physical=" + pointer.position
            + " source=" + bridge.sourceCamera.WorldToScreenPoint(world) + " hit=" + hit + " receiver=" + receiver + " expected=" + expected + "\n");
        Check(receiver == expected, context + ": actual source-canvas raycast reaches the intended control");
        bridge.inputSurface.OnPointerDown(pointer);
        return pointer;
    }
    static void WiringSceneClick(AeroLcdSourceBridge bridge, Vector3 world, GameObject expected, string context)
    {
        var pointer = WiringScenePress(bridge, world, expected, context);
        bridge.inputSurface.OnPointerUp(pointer);
    }
    static void WiringSceneMove(AeroLcdSourceBridge bridge, PointerEventData pointer, Vector2 source, bool begin)
    {
        pointer.delta = WiringScenePhysical(bridge, source) - pointer.position;
        pointer.position += pointer.delta;
        if (begin) bridge.inputSurface.OnBeginDrag(pointer);
        else bridge.inputSurface.OnDrag(pointer);
    }
    static Vector3 WiringSceneBlank(AeroLcdSourceBridge bridge, XgWiringPage page)
    {
        var viewport = WiringSceneField<RectTransform>(page, "viewport");
        var background = WiringSceneField<XgWiringGraphic>(page, "back").gameObject;
        // The strip above the first node row is deliberately clear of cable geometry; no cable is clicked here.
        foreach (float fraction in new[] { .12f, .22f, .32f, .42f })
        {
            var world = viewport.TransformPoint(new Vector2(Mathf.Lerp(viewport.rect.xMin, viewport.rect.xMax, fraction), viewport.rect.yMax - 12));
            var hit = WiringSceneHover(bridge, WiringScenePointer(bridge, world));
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) == background) return world;
        }
        throw new InvalidOperationException("No unobscured blank canvas point is reachable through the real LCD relay");
    }
    static IEnumerator WiringSceneDismissNotifications(AeroLcdSourceBridge bridge)
    {
        var dismissed = new HashSet<Button>();
        float deadline = Time.unscaledTime + 15;
        while (true)
        {
            bool busy = false;
            foreach (var tray in UnityEngine.Object.FindObjectsByType<TrayPopups>(FindObjectsSortMode.None))
            {
                if (!tray.IsBusy) continue;
                busy = true;
                foreach (var button in tray.GetComponentsInChildren<Button>())
                {
                    if (button.name != "Close" || dismissed.Contains(button)) continue;
                    var rect = (RectTransform)button.transform;
                    var point = rect.TransformPoint(rect.rect.center);
                    var source = bridge.sourceCamera.WorldToScreenPoint(point);
                    var texture = bridge.sourceCamera.targetTexture;
                    if (source.z <= 0 || source.x <= 0 || source.y <= 0 || source.x >= texture.width || source.y >= texture.height) continue;
                    var hit = WiringSceneHover(bridge, WiringScenePointer(bridge, point));
                    if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) != button.gameObject) continue;
                    WiringSceneClick(bridge, point, button.gameObject, "dismiss real tray notification");
                    dismissed.Add(button);
                }
            }
            if (!busy) yield break;
            Check(Time.unscaledTime < deadline, "Real tray notification close animations finish before scene captures");
            yield return new WaitForSecondsRealtime(.2f);
        }
    }
    static void WiringSceneRenderedMesh(XgWiringPage page)
    {
        var actual = WiringSceneField<XgWiringGraphic>(page, "front").canvasRenderer.GetMesh();
        Check(actual != null && actual.vertexCount > 0, "Real source canvas has a rendered Wiring foreground");
        using (var expected = new VertexHelper())
        {
            page.DrawFront(expected);
            var vertices = actual.vertices;
            Check(vertices.Length == expected.currentVertCount, "Rendered Main-scene Wiring mesh has the current vertex count");
            var vertex = default(UIVertex); int firstMismatch = -1;
            for (int i = 0; i < vertices.Length; i++)
            {
                expected.PopulateUIVertex(ref vertex, i);
                if (!vertices[i].Equals(vertex.position)) { firstMismatch = i; break; }
            }
            Check(firstMismatch < 0, "Rendered Main-scene foreground matches all " + vertices.Length
                + " current vertices; first mismatch=" + firstMismatch);
        }
    }
    static void WiringSceneCapture(string name)
    {
        Capture(name); // Complete source-canvas image, retaining the real window and desktop.
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Path.Combine(Output, name + "-full.png"), image.EncodeToPNG()); }
        finally { UnityEngine.Object.Destroy(image); }
    }
}
