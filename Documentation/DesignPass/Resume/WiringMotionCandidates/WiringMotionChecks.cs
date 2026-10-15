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

// Candidate only. Import into an Editor validation directory after applying changes.patch.
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

    public static IEnumerator Frames(TMP_FontAsset font)
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
            yield return null;
            Canvas.ForceUpdateCanvases(); page.Tick(0);
            yield return null;

            var nodes = (IDictionary)Field(page, "views");
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
            Debug.Log("WIRING_MOTION_CHECKS_PASS: natural private poses, fixed hit/cable anchors, input cancellation, Reduced, selection and actual connection/refusal. Physical input and screenshots still require acceptance.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            if (ownedEvents != null) UnityEngine.Object.DestroyImmediate(ownedEvents);
            else if (previousSelection != null && previousSelection.activeInHierarchy && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection);
        }
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
