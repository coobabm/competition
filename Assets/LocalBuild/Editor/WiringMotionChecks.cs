using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Yield from the shared PlayMode runner: yield return WiringMotionChecks.Frames(Font);
// Uses in-memory rules and real production page construction. No controller or save APIs.
public static class WiringMotionChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    sealed class Wallet : IXgHost
    {
        public double Money => 10000;
        public double Compute => 1;
        public double VramMB => 6144;
        public string Blocker => null;
        public bool Spend(double amount) => false;
        public void Earn(double amount) { throw new InvalidOperationException("Unexpected wallet mutation"); }
        public void Train(double seconds) { throw new InvalidOperationException("Unexpected training"); }
    }

    sealed class Host : IXgPageHost
    {
        public XingGuangController Controller => null;
        public XgSim Sim { get; } = new XgSim();
        public IXgHost HostWallet = new Wallet();
        IXgHost IXgPageHost.Host => HostWallet;
        public XgJuice Juice { get; set; }
        public int BuyAmount { get; set; } = 1;
        public bool Visible => true;
        public string Tab => "wiring";
        public RectTransform ComboChip => null;
        public XgWiringPage Page;
        public string LastToast;
        public void Refresh(bool force) => Page.Refresh();
        public void ShowToast(string text, float seconds) => LastToast = text;
        public void ShowTab(string id) { throw new InvalidOperationException("Unexpected navigation: " + id); }
        public bool BuyNode(XgNode node, RectTransform from) => false;
    }

    public static IEnumerator Frames(TMP_FontAsset font, Action<string> capture = null)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("WiringMotionChecks requires PlayMode natural frames");
        if (font == null) throw new ArgumentNullException(nameof(font), "Use the actual Desktop CJK font");
        var root = new GameObject("WiringMotionChecks", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        GameObject ownedEvents = null;
        var previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        try
        {
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var events = EventSystem.current;
            if (events == null) { ownedEvents = new GameObject("WiringMotionEvents", typeof(EventSystem)); events = ownedEvents.GetComponent<EventSystem>(); }
            var area = XgUi.Rect("Area", root.transform, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20));
            var host = new Host { Juice = root.AddComponent<XgJuice>() };
            var page = new XgWiringPage(); host.Page = page;
            page.Init(host, new XgUi(font, null, true)); page.Build(area);
            host.Sim.RefreshWiring(host.HostWallet);
            var gpu = host.Sim.Wiring.nodes.Find(n => n.lane == XgWireLane.Gpu && n.house);
            Check(gpu != null, "The actual rules expose a home GPU");
            host.Sim.Disconnect("psu", gpu.key, host.HostWallet);
            page.Refresh();
            host.Juice.StartCoroutine(TickPage(page));
            yield return null;
            Canvas.ForceUpdateCanvases(); page.Tick(0);
            yield return null;

            var nodes = (IDictionary)Field(page, "views");
            yield return new WaitForEndOfFrame();
            CheckRenderedMesh(page);
            capture?.Invoke("wiring-rest");
            var psuNode = nodes["psu"]; var gpuNode = nodes[gpu.key];
            var nodeRect = (RectTransform)Field(psuNode, "rt");
            var outRect = (RectTransform)Field(psuNode, "outPort");
            var inRect = (RectTransform)Field(gpuNode, "inPort");
            var gpuOutRect = (RectTransform)Field(gpuNode, "outPort");
            var nodeButton = nodeRect.GetComponent<XgButtonPressFeedback>();
            var outButton = outRect.GetComponent<XgButtonPressFeedback>();
            var inButton = inRect.GetComponent<XgButtonPressFeedback>();
            Check(nodeButton != null && outButton != null && inButton != null, "Production node and both port types reuse the existing feedback Button");
            Check(nodeButton.Label == ((TMP_Text)Field(psuNode, "title")).rectTransform, "Node feedback targets only its title");
            Check(outButton.Label != outRect && inButton.Label != inRect, "Ports keep independent visual targets");
            Check(outButton.Label.GetComponent<Graphic>() == null && inButton.Label.GetComponent<Graphic>() == null,
                "Port proxies add no raycast target or second drawing layer");

            foreach (var button in new[] { nodeButton, outButton, inButton })
            {
                var hit = (RectTransform)button.transform;
                var target = button.Label;
                var hitPosition = hit.anchoredPosition; var hitSize = hit.rect.size; var hitScale = hit.localScale;
                var restPosition = target.anchoredPosition; var restScale = target.localScale;
                int calls = 0; button.onClick.AddListener(() => calls++);
                var pointer = Pointer(events, button);

                pointer.button = PointerEventData.InputButton.Right;
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Check(calls == 0 && target.GetComponent<XgJuice>() == null, "Right click produces no private feedback or business callback");
                pointer.button = PointerEventData.InputButton.Left;
                button.interactable = false;
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                Check(target.GetComponent<XgJuice>() == null, "Disabled control does not allocate feedback");
                button.interactable = true;

                Vector3[] beforeMesh = Mesh(page);
                Check(page.QaPoint("psu", "out", true, out var endpoint), "The actual endpoint is available");
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                var fx = target.GetComponent<XgJuice>();
                Check(fx != null && fx.HasActiveKnock(target) && calls == 0, "Press begins feedback without an early action");
                yield return null;
                Check(target.localScale != restScale || target.anchoredPosition != restPosition, "Natural Update visibly changes the private target");
                yield return new WaitForEndOfFrame();
                CheckRenderedMesh(page);
                capture?.Invoke(button == nodeButton ? "wiring-node-pressed" : button == outButton ? "wiring-output-pressed" : "wiring-input-pressed");
                Check(hit.anchoredPosition == hitPosition && hit.rect.size == hitSize && hit.localScale == hitScale, "Feedback leaves the hit rect and layout untouched");
                Check(page.QaPoint("psu", "out", true, out var endpointAfter) && endpointAfter == endpoint, "Feedback does not move the cable anchor");
                if (button != nodeButton) Check(!SameMesh(beforeMesh, Mesh(page)), "Original foreground geometry actually consumes the port pose");
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                Check(calls == 0, "Release alone does not invoke business");
                yield return WaitRest(button, restPosition, restScale);
                Check(button.Label.GetComponents<XgJuice>().Length == 1, "The target has one reusable private effect component");

                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                yield return null;
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerExitHandler);
                Check(!fx.HasActiveKnock(target) && target.anchoredPosition == restPosition && target.localScale == restScale, "Pointer exit cancels and restores the effect");
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);

                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                yield return null;
                button.enabled = false;
                Check(!fx.HasActiveKnock(target) && target.localScale == restScale, "Disabling the component restores immediately");
                button.enabled = true;
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                yield return null;
                root.GetComponent<CanvasGroup>().interactable = false;
                yield return null;
                Check(!fx.HasActiveKnock(target) && target.anchoredPosition == restPosition, "Parent input gating clears active feedback");
                root.GetComponent<CanvasGroup>().interactable = true;

                host.Juice.Reduced = true;
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                yield return null;
                Check(target.anchoredPosition == restPosition && Mathf.Abs(target.localScale.x / restScale.x - 1) <= .0151f,
                    "Reduced feedback has no translation and keeps the existing small amplitude");
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                yield return WaitRest(button, restPosition, restScale);
                host.Juice.Reduced = false;

                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                yield return null;
                host.Juice.Reduced = true;
                yield return WaitRest(button, restPosition, restScale);
                host.Juice.Reduced = false;
                Check(calls == 0, "Visual cancellation and mode changes do not fabricate clicks");
            }

            int resultClicks = 0;
            nodeButton.onClick.AddListener(() => resultClicks++);
            outButton.onClick.AddListener(() => resultClicks++);
            inButton.onClick.AddListener(() => resultClicks++);
            Click(events, nodeButton);
            Check(page.QaSelected == "psu" && ((RectTransform)Field(page, "detail")).gameObject.activeSelf,
                "Node click retains actual selection and inspector result");
            Click(events, outButton);
            Check(page.QaArmed == "psu" && ((string)Field(page, "hint")).Length > 0, "First port click retains the original wiring hint");
            Click(events, inButton);
            Check(host.Sim.Wiring.Node(gpu.key).source == "psu" && page.QaArmed == null && ((string)Field(page, "hint")).Length > 0,
                "Successful connection retains the actual core graph and result message");
            Check(ReferenceEquals(nodes["psu"], psuNode) && ReferenceEquals(nodes[gpu.key], gpuNode), "Click results reuse the existing node views");
            Check(resultClicks == 3, "Each selection or connection click dispatches exactly once");

            host.LastToast = null;
            Click(events, gpuOutRect.GetComponent<XgButtonPressFeedback>());
            Click(events, inButton);
            Check(!string.IsNullOrEmpty(host.LastToast) && host.LastToast == (string)Field(page, "hint"),
                "Invalid same-node connection retains its specific refusal in both existing surfaces");
            Check(host.Sim.Wiring.Node(gpu.key).source == "psu" && page.QaArmed == gpu.key,
                "Refusal preserves the real connection and retry state");
            Check(resultClicks == 4, "Refusal dispatches the input-port action exactly once");
            yield return new WaitForEndOfFrame();
            CheckRenderedMesh(page);
            capture?.Invoke("wiring-refused");
            yield return DragChecks(host, page, events, gpu.key, nodeButton, outButton, inButton,
                gpuOutRect.GetComponent<XgButtonPressFeedback>());
            Debug.Log("WIRING_MOTION_CHECKS_PASS: natural poses and rendered meshes, fixed hit/cable anchors, input cancellation, Reduced, selection, same-object node drag, actual port drop/cancel/refusal. Physical input and screenshots still require acceptance.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            if (ownedEvents != null) UnityEngine.Object.DestroyImmediate(ownedEvents);
            else if (previousSelection != null && previousSelection.activeInHierarchy && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection);
        }
    }

    static IEnumerator TickPage(XgWiringPage page)
    {
        // The real view ticks its active page each frame, which invalidates the port mesh.
        // Keep that production path running here; private pose changes alone are not visible geometry.
        while (true) { yield return null; page.Tick(Time.unscaledDeltaTime); }
    }

    static IEnumerator DragChecks(Host host, XgWiringPage page, EventSystem events, string gpuKey,
        XgButtonPressFeedback node, XgButtonPressFeedback output, XgButtonPressFeedback input,
        XgButtonPressFeedback wrongOutput)
    {
        page.CancelWire(); // Clear the preceding click-refusal fixture before exercising drags.
        // The preceding click checks may still have a pulse. Capture only naturally settled resting poses.
        double settleDeadline = Time.realtimeSinceStartupAsDouble + 5;
        var buttons = new[] { node, output, input, wrongOutput };
        bool settling;
        do
        {
            settling = false;
            foreach (var button in buttons)
            {
                var fx = button.Label.GetComponent<XgJuice>();
                settling |= fx != null && fx.HasActiveKnock(button.Label);
            }
            Check(Time.realtimeSinceStartupAsDouble < settleDeadline, "Earlier click feedback naturally settles before drag baselines are captured");
            if (settling) yield return null;
        } while (settling);
        yield return null;
        var nodeRect = (RectTransform)node.transform;
        var nodePosition = nodeRect.anchoredPosition; var nodeSize = nodeRect.rect.size; var nodeScale = nodeRect.localScale;
        var labelPosition = node.Label.anchoredPosition; var labelScale = node.Label.localScale;
        var portRect = (RectTransform)output.transform;
        var portPosition = portRect.anchoredPosition; var portSize = portRect.rect.size; var portScale = portRect.localScale;
        int nodeClicks = 0; node.onClick.AddListener(() => nodeClicks++);
        var pointer = DragPointer(events, node);
        ExecuteEvents.Execute(node.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        yield return null;
        ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.beginDragHandler);
        pointer.dragging = true;
        pointer.delta = new Vector2(45, -20); pointer.position += pointer.delta;
        ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.dragHandler);
        Check(nodeRect.anchoredPosition != nodePosition && page.QaSelected == "psu" && nodeClicks == 0,
            "The same-object node relay moves and selects the actual node before any release click");
        var movedPosition = nodeRect.anchoredPosition;
        Check(nodeRect.rect.size == nodeSize && nodeRect.localScale == nodeScale && portRect.anchoredPosition == portPosition
            && portRect.rect.size == portSize && portRect.localScale == portScale, "Node dragging preserves body size and port-local hit geometry");
        CheckPortAnchor(page, "psu", "out", portRect);
        yield return new WaitForEndOfFrame();
        CheckRenderedMesh(page);
        ExecuteEvents.Execute(node.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        // A same-object drag retains click eligibility in uGUI; release over that node clicks before EndDrag.
        ExecuteEvents.Execute(node.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
        pointer.dragging = false;
        Check(nodeClicks == 1 && !(bool)Field(page, "draggingNode"), "Node release dispatches once and the actual relay ends dragging");
        yield return WaitRest(node, labelPosition, labelScale);
        Check(nodeRect.anchoredPosition == movedPosition, "Finishing private feedback does not undo the user's node movement");
        CheckPortAnchor(page, "psu", "out", portRect);

        Check(host.Sim.Disconnect("psu", gpuKey, host.HostWallet), "Port-drop fixture starts with the real GPU unplugged");
        page.Refresh();
        yield return null;
        int messages = 0, portClicks = 0;
        host.Sim.WiringLogged += _ => messages++;
        output.onClick.AddListener(() => portClicks++);
        input.onClick.AddListener(() => portClicks++);
        wrongOutput.onClick.AddListener(() => portClicks++);
        // Cancellation, rejected same-node drop, then a successful source-to-target drop.
        for (int mode = 0; mode < 3; mode++)
        {
            var source = mode == 1 ? wrongOutput : output;
            var sourceRect = (RectTransform)source.transform;
            var inputRect = (RectTransform)input.transform;
            var sourcePosition = sourceRect.anchoredPosition; var sourceSize = sourceRect.rect.size; var sourceScale = sourceRect.localScale;
            var inputPosition = inputRect.anchoredPosition; var inputSize = inputRect.rect.size; var inputScale = inputRect.localScale;
            var restPosition = source.Label.anchoredPosition; var restScale = source.Label.localScale;
            int beforeMessages = messages;
            string beforeSource = host.Sim.Wiring.Node(gpuKey).source;
            host.LastToast = null;
            pointer = DragPointer(events, source);
            ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return null;
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.beginDragHandler);
            pointer.dragging = true;
            Check(page.QaArmed == (mode == 1 ? gpuKey : "psu") && (bool)Field(page, "draggingPort"),
                "The existing same-object port relay arms the actual source");
            Vector2 destination = mode == 0 ? pointer.position + new Vector2(60, 50) : ScreenCenter(inputRect);
            pointer.delta = destination - pointer.position; pointer.position = destination;
            pointer.pointerCurrentRaycast = new RaycastResult { gameObject = mode == 0 ? null : input.gameObject };
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            Check(sourceRect.anchoredPosition == sourcePosition && sourceRect.rect.size == sourceSize && sourceRect.localScale == sourceScale
                && inputRect.anchoredPosition == inputPosition && inputRect.rect.size == inputSize && inputRect.localScale == inputScale,
                "Dragging a cable never moves either port's hit geometry");
            CheckPortAnchor(page, mode == 1 ? gpuKey : "psu", "out", sourceRect);
            CheckPortAnchor(page, gpuKey, "in", inputRect);
            yield return new WaitForEndOfFrame();
            CheckRenderedMesh(page);
            ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            if (mode != 0) ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.dropHandler);
            if (mode == 0) Check(messages == beforeMessages && host.Sim.Wiring.Node(gpuKey).source == beforeSource,
                "A release without a drop target makes no connection or refusal");
            else if (mode == 1) Check(messages == beforeMessages + 1 && !string.IsNullOrEmpty(host.LastToast)
                && host.LastToast == (string)Field(page, "hint") && host.Sim.Wiring.Node(gpuKey).source == beforeSource,
                "A real wrong-target drop refuses exactly once and preserves the graph");
            else Check(messages == beforeMessages + 1 && host.Sim.Wiring.Node(gpuKey).source == "psu" && page.QaArmed == null,
                "A real source-to-target drop connects exactly once");
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
            pointer.dragging = false;
            Check(page.QaArmed == null && !(bool)Field(page, "draggingPort") && portClicks == 0,
                "Every port drag ends cleanly without fabricating any button clicks");
            yield return WaitRest(source, restPosition, restScale);
            Check(sourceRect.anchoredPosition == sourcePosition && inputRect.anchoredPosition == inputPosition,
                "Settling the private pose leaves both port positions intact");
            CheckPortAnchor(page, mode == 1 ? gpuKey : "psu", "out", sourceRect);
            CheckPortAnchor(page, gpuKey, "in", inputRect);
            yield return new WaitForEndOfFrame();
            CheckRenderedMesh(page);
        }
        Debug.Log("WIRING_DRAG_CHECKS_PASS: same-object node movement and port drop/cancel/refusal; single result dispatch and stable anchors.");
    }

    static PointerEventData DragPointer(EventSystem events, XgButtonPressFeedback button)
    {
        var pointer = Pointer(events, button);
        pointer.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(button.gameObject);
        Check(pointer.pointerDrag == button.gameObject, "The feedback button preserves its existing same-object drag handler");
        pointer.position = pointer.pressPosition = ScreenCenter((RectTransform)button.transform);
        pointer.pointerCurrentRaycast = new RaycastResult { gameObject = button.gameObject };
        pointer.eligibleForClick = true;
        return pointer;
    }
    static Vector2 ScreenCenter(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    static void CheckPortAnchor(XgWiringPage page, string key, string side, RectTransform hit)
    {
        Check(page.QaPoint(key, side, true, out var anchor) && Vector3.Distance(anchor, hit.TransformPoint(hit.rect.center)) < .001f,
            "The actual " + key + "/" + side + " cable anchor stays at the unchanged port hit center");
    }

    // Called only after a natural end-of-frame; never force a rebuild to make the assertion pass.
    static void CheckRenderedMesh(XgWiringPage page)
    {
        var front = (XgWiringGraphic)Field(page, "front");
        var rendered = front.canvasRenderer.GetMesh();
        Check(rendered != null, "The foreground renderer has an actual mesh");
        var actual = rendered.vertices;
        var expected = Mesh(page);
        Check(actual.Length > 0 && actual.Length == expected.Length, "The renderer contains the complete current foreground mesh");
        for (int i = 0; i < actual.Length; i++)
            Check(actual[i].Equals(expected[i]), "Rendered foreground vertex " + i + " matches DrawFront in the same completed frame");
    }

    static PointerEventData Pointer(EventSystem events, Button button) => new PointerEventData(events)
    { pointerId = -1, button = PointerEventData.InputButton.Left, pointerPress = button.gameObject, pointerDrag = button.gameObject };
    static void Click(EventSystem events, Button button)
    {
        var pointer = Pointer(events, button);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator WaitRest(XgButtonPressFeedback button, Vector2 position, Vector3 scale)
    {
        var fx = button.Label.GetComponent<XgJuice>();
        double deadline = Time.realtimeSinceStartupAsDouble + 5;
        double animationTime = 0;
        while (fx.HasActiveKnock(button.Label))
        {
            Check(Time.realtimeSinceStartupAsDouble < deadline && animationTime <= .2501, "Private animation completes within its natural-time bound");
            yield return null;
            animationTime += Math.Min(Time.unscaledDeltaTime, .05f);
        }
        yield return null;
        Check(button.Label.anchoredPosition == position && button.Label.localScale == scale && !fx.enabled, "Natural finish restores exactly and idles the private component");
    }
    static Vector3[] Mesh(XgWiringPage page)
    {
        using (var vertices = new VertexHelper())
        {
            page.DrawFront(vertices);
            var positions = new Vector3[vertices.currentVertCount];
            var vertex = default(UIVertex);
            for (int i = 0; i < positions.Length; i++) { vertices.PopulateUIVertex(ref vertex, i); positions[i] = vertex.position; }
            return positions;
        }
    }
    static bool SameMesh(Vector3[] a, Vector3[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }
    static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("WiringMotionChecks: " + message); }
}
