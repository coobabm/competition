using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Drag to pan with inertia. Wheel pans horizontally, Shift-wheel vertically; buttons zoom.
    /// The desktop canvas is Screen Space – Camera rendered into an LCD texture, so pointer positions are converted with
    /// the event camera into the viewport's local space; raw pixel deltas would be wrong by the canvas scale.
    /// </summary>
    public sealed class XgPan : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public RectTransform content, viewport;
        public float minZoom = .5f, maxZoom = 1.6f;
        public const float ReadableZoom = .85f;
        public bool Dragging { get; private set; }
        Vector2 last, velocity, zoomAnchor;
        float targetZoom = -1;
        Vector2? glide;

        bool Local(PointerEventData e, out Vector2 p)
        {
            var cam = e.pressEventCamera != null ? e.pressEventCamera : e.enterEventCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, cam, out p);
        }

        public void OnBeginDrag(PointerEventData e) { Dragging = Local(e, out last); velocity = Vector2.zero; glide = null; }

        public void OnDrag(PointerEventData e)
        {
            if (!Dragging || !Local(e, out var p)) return;
            var d = p - last; last = p;
            content.anchoredPosition += d;
            Clamp();
            velocity = Vector2.Lerp(velocity, d / Mathf.Max(Time.unscaledDeltaTime, .005f), .5f);
        }

        public void OnEndDrag(PointerEventData e) { Dragging = false; }

        public void OnScroll(PointerEventData e)
        {
            if (content == null) return;
            var keyboard = Keyboard.current;
            // The wheel reads down a column; Shift-wheel (or a sideways swipe) moves between stages.
            bool shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            bool sideways = Mathf.Abs(e.scrollDelta.x) > Mathf.Abs(e.scrollDelta.y);
            float delta = (sideways ? e.scrollDelta.x : e.scrollDelta.y) * 80;
            content.anchoredPosition += shift || sideways ? new Vector2(delta, 0) : new Vector2(0, -delta);
            velocity = Vector2.zero; glide = null; Clamp();
        }

        /// <summary>Zoom around the viewport centre (the +/− buttons).</summary>
        public void ZoomBy(float factor)
        {
            zoomAnchor = viewport.rect.center;
            float from = targetZoom > 0 ? targetZoom : content.localScale.x;
            targetZoom = Mathf.Clamp(from * factor, minZoom, maxZoom);
        }

        /// <summary>Explicit fit/reset without leaving an old zoom or inertia animation running.</summary>
        public void SetView(float zoom, Vector2 position)
        {
            if (content == null || viewport == null) return;
            targetZoom = -1; velocity = Vector2.zero; glide = null; Dragging = false;
            content.localScale = Vector3.one * Mathf.Clamp(zoom, minZoom, maxZoom);
            content.anchoredPosition = position;
            Clamp();
        }

        /// <summary>Stage overview: fit every lane vertically, keep its header below the top edge, and glide only horizontally.</summary>
        public void FitColumn(float centreX, float padding = 16)
        {
            if (content == null || viewport == null || content.sizeDelta.y <= 0) return;
            var size = viewport.rect.size;
            if (size.x <= 0 || size.y <= padding * 2) return;
            // Readable first: names must stay large, so the column is not shrunk to fit; pan down for the lower lanes.
            float fit = Mathf.Clamp((size.y - padding * 2) / content.sizeDelta.y, ReadableZoom, .95f);
            // A small window still needs a genuine fit; the normal manual minimum must not undo it.
            minZoom = Mathf.Min(.5f, fit);
            targetZoom = -1; velocity = Vector2.zero;
            content.localScale = Vector3.one * fit;
            var position = content.anchoredPosition; position.y = -padding; content.anchoredPosition = position;
            glide = new Vector2(size.x * .5f - centreX * fit, -padding);
            Clamp();
        }

        /// <summary>Slide so this content point (local, top-left origin) sits near the viewport centre.</summary>
        public void GlideTo(Vector2 contentPoint)
        {
            float z = targetZoom > 0 ? targetZoom : content.localScale.x;
            Vector2 view = viewport.rect.size;
            glide = new Vector2(view.x * .45f - contentPoint.x * z, -view.y * .42f - contentPoint.y * z);
            velocity = Vector2.zero;
        }

        void Update()
        {
            if (content == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (!Dragging && velocity.sqrMagnitude > 4)
            {
                content.anchoredPosition += velocity * dt;
                velocity *= Mathf.Exp(-7 * dt);
                Clamp();
            }
            float z = content.localScale.x;
            if (targetZoom > 0)
            {
                float next = Mathf.Abs(z - targetZoom) < .002f ? targetZoom : Mathf.Lerp(z, targetZoom, 1 - Mathf.Exp(-14 * dt));
                Vector2 topLeft = new Vector2(viewport.rect.xMin, viewport.rect.yMax);
                Vector2 local = zoomAnchor - topLeft;
                Vector2 before = (local - content.anchoredPosition) / z;
                content.localScale = Vector3.one * next;
                content.anchoredPosition = local - before * next;
                Clamp();
                if (next == targetZoom) targetZoom = -1;
            }
            if (glide.HasValue && !Dragging)
            {
                var target = glide.Value;
                content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, target, 1 - Mathf.Exp(-10 * dt));
                Clamp();
                if ((content.anchoredPosition - target).sqrMagnitude < 1) glide = null;
            }
        }

        public void Clamp()
        {
            float z = content.localScale.x;
            Vector2 size = content.sizeDelta * z, view = viewport.rect.size;
            var p = content.anchoredPosition;
            p.x = Mathf.Clamp(p.x, Mathf.Min(40, view.x - size.x - 40), 40);
            p.y = Mathf.Clamp(p.y, -40, Mathf.Max(-40, size.y - view.y + 40));
            content.anchoredPosition = p;
        }
    }

    /// <summary>One node: press and hold 0.6 s to buy; dragging pans the canvas instead.</summary>
    public sealed class XgNodeView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public XgNode node;
        public XgTreePage page;
        public XgNodeGraphic graphic;
        public TMP_Text label, cost;
        public float charge;
        public bool held;
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { held = true; page.Press(this); } }
        public void OnPointerUp(PointerEventData e) { if (held) { held = false; page.Release(this); } }
        public void OnPointerEnter(PointerEventData e) { page.Hover(this); }
        public void OnPointerExit(PointerEventData e) { if (held) { held = false; page.Release(this); } }
        public void OnBeginDrag(PointerEventData e) { if (held) { held = false; page.Release(this); } page.Pan.OnBeginDrag(e); }
        public void OnDrag(PointerEventData e) { page.Pan.OnDrag(e); }
        public void OnEndDrag(PointerEventData e) { page.Pan.OnEndDrag(e); }
    }

    /// <summary>One progressively revealed horizontal tree. The simulation owns visibility, prices and purchases.</summary>
    public sealed class XgTreePage : XgPage
    {
        public const float HoldSeconds = .6f;
        const float ChargeDelay = .15f;
        RectTransform viewport, content, miniMap, frontierWall, eraCard;
        XgLinksGraphic links;
        readonly List<XgNodeView> nodes = new List<XgNodeView>();
        readonly Dictionary<int, TMP_Text> stageTitles = new Dictionary<int, TMP_Text>();
        readonly Dictionary<int, XgBtn> miniButtons = new Dictionary<int, XgBtn>();
        readonly Dictionary<XgNodeView, float> entrances = new Dictionary<XgNodeView, float>();
        readonly Dictionary<string, double> purchaseCosts = new Dictionary<string, double>();
        readonly List<XgBtn> toolbar = new List<XgBtn>();
        TMP_Text header, infoTitle, infoBody, infoCost, eraTitle, eraBody;
        XgNodeView selected, charging;
        XgSim displayedSim;
        float chargeHeld, eraRemaining;
        int displayedStage;
        bool built;
        public XgPan Pan { get; private set; }
        public int VisibleStageCount => Sim == null ? 0 : Sim.S.stage;
        public int VisibleNodeCount { get { int count = 0; foreach (var n in nodes) if (n.gameObject.activeSelf) count++; return count; } }
        public bool IsNodeShown(string id) => nodes.Exists(n => n.node.id == id && n.gameObject.activeSelf);

        static Color LaneColor(string lane) => lane == "vision" ? XgDark.Accent : lane == "sequence" ? new Color32(18, 135, 125, 255)
            : lane == "label" ? new Color32(200, 120, 30, 255) : lane == "trunk" ? new Color32(90, 70, 170, 255) : lane == "atlas" ? new Color32(20, 140, 160, 255) : new Color32(130, 80, 210, 255);
        static Vector2 Pos(XgNode n) => new Vector2(n.x, -(70 + n.y));

        public override void Build(RectTransform area)
        {
            root = ui.Card(area, "tree", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // 科技 is a section of 道具 (the redesign): the way back sits before the title.
            var back = ui.Button(root, T("← 道具", "← Items"), () => { Fx.Play(XgJuice.Sfx.Id.Click); host.ShowTab("items"); }, 13);
            PlaceTopLeft(back, 12, 8, 84, 30);
            back.rt.gameObject.name = "BackToItems";
            back.Set(T("← 道具", "← Items"), true, null, XgDark.Link);
            header = ui.Text(Strip("Header", root, 8, 30, 106, 560), "", 18, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            viewport = Rect("Viewport", root, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-300, -88));
            viewport.gameObject.AddComponent<CanvasRenderer>();
            var grid = viewport.gameObject.AddComponent<XgGridGraphic>(); grid.color = XgDark.Page;
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            content = Rect("Content", viewport, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0, 1);
            content.sizeDelta = new Vector2(XgCatalog.StageWidth * 2 + 64, XgCatalog.TreeHeight + 70);
            content.localScale = Vector3.one * .9f;
            Panel(content, Color.clear).raycastTarget = true;
            Pan = viewport.gameObject.AddComponent<XgPan>(); Pan.content = content; Pan.viewport = viewport;
            // Lane bands as alternating stripes behind everything, so each row of the tree reads as one track.
            int band = 0;
            foreach (var lane in XgCatalog.Lanes)
            {
                float top = 70 + XgCatalog.LaneTop(lane) - 44, height = XgCatalog.LaneHeight(lane) + 48;
                var stripe = Rect("LaneBand " + lane, content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -top - height), new Vector2(0, -top));
                Panel(stripe, band++ % 2 == 0 ? new Color32(11, 19, 28, 200) : new Color32(0, 0, 0, 0)).raycastTarget = false;
            }
            var linkRt = Rect("Links", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            linkRt.pivot = new Vector2(0, 1);
            links = linkRt.gameObject.AddComponent<XgLinksGraphic>(); links.raycastTarget = false;
            frontierWall = XgUi.TopLeft("UnexploredBoundary", content, 0, 60, 10, XgCatalog.TreeHeight);
            frontierWall.pivot = new Vector2(.5f, 1);
            Panel(frontierWall, new Color32(58, 85, 102, 180)).raycastTarget = false;
            miniMap = Strip("DiscoveredStages", root, 48, 28, 16, 310);
            string[] labels = { "⓪ 标注", "前沿", "可买", "−", "+" };
            string[] labelsEn = { "⓪ Labels", "Frontier", "Buyable", "−", "+" };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                toolbar.Add(ui.Button(root, T(labels[i], labelsEn[i]), () => Toolbar(index), 15));
            }
            float right = 304;
            for (int i = toolbar.Count - 1; i >= 0; i--)
            {
                float width = i >= 3 ? 34 : i == 0 ? 86 : 72;
                PlaceTopRight(toolbar[i], right + width, 8, width, 30); right += width + 4;
            }
            UiTip.Add(toolbar[0].rt, () => Sim != null && Sim.AutoLabelHidden ? Lang.T("跳到标注技能（加薪）。") : Lang.T("跳到标注技能（加薪、自动答题）。"));
            UiTip.Add(toolbar[1].rt, "跳到当前阶段的最前沿。", "Jump to the current stage's frontier.");
            UiTip.Add(toolbar[2].rt, "依次跳到现在买得起的节点。", "Cycle through the nodes you can afford now.");
            UiTip.Add(toolbar[3].rt, "缩小。", "Zoom out.");
            UiTip.Add(toolbar[4].rt, "放大。", "Zoom in.");
            var info = Rect("Info", root, new Vector2(1, 0), Vector2.one, new Vector2(-296, 8), new Vector2(-8, -44));
            Panel(info, XgDark.Panel);
            infoTitle = ui.Text(Strip("Title", info, 12, 50, 14, 14), "", 22, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            infoTitle.fontStyle = FontStyles.Bold;
            infoCost = ui.Text(Strip("Cost", info, 66, 26, 14, 14), "", 19, XgDark.Money, TextAlignmentOptions.MidlineLeft);
            infoBody = ui.Text(Rect("Body", info, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -102)), "", 16, XgDark.Ink, TextAlignmentOptions.TopLeft);
            eraCard = Rect("EraCard", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-240, -92), new Vector2(240, 92));
            Panel(eraCard, XgDark.Hud);
            eraTitle = ui.Text(Strip("EraTitle", eraCard, 16, 36, 20, 20), "", 24, XgDark.Gold, TextAlignmentOptions.Center);
            eraBody = ui.Text(Strip("EraDescription", eraCard, 60, 76, 24, 24), "", 16, Color.white, TextAlignmentOptions.Center);
            var dismiss = ui.Button(eraCard, Lang.T("继续"), () => { eraRemaining = 0; eraCard.gameObject.SetActive(false); }, 14);
            PlaceTopRight(dismiss, 105, 142, 85, 28);
            eraCard.gameObject.SetActive(false);
            built = true;
            SyncRevealedContent(false);
        }

        void SyncRevealedContent(bool animate)
        {
            bool rebound = displayedSim != Sim;
            if (rebound) { displayedSim = Sim; displayedStage = Sim.S.stage; selected = charging = null; entrances.Clear(); }
            int stage = Sim.S.stage;
            for (int i = 0; i <= stage; i++)
            {
                if (!stageTitles.ContainsKey(i))
                {
                    var title = ui.Text(XgUi.TopLeft("StageHeader" + i, content, i * XgCatalog.StageWidth + 24, 12, XgCatalog.StageWidth - 40, 36), "", 21, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                    title.fontStyle = FontStyles.Bold; stageTitles.Add(i, title);
                    int column = i;
                    var button = ui.Button(miniMap, "", () => FocusStage(column), 13);
                    button.rt.anchorMin = button.rt.anchorMax = new Vector2(0, .5f); button.rt.pivot = new Vector2(0, .5f);
                    button.rt.anchoredPosition = new Vector2(i * 56, 0); button.rt.sizeDelta = new Vector2(50, 25);
                    miniButtons.Add(i, button);
                    if (i >= 3)
                    {
                        LaneTitle("VisionLane" + i, i, "视觉", "Vision", "vision");
                        LaneTitle("SequenceLane" + i, i, "序列", "Sequence", "sequence");
                    }
                    if (i >= 1) LaneTitle("TrunkLane" + i, i, "主干", "Core", "trunk");
                    if (i >= 1) { LaneTitle("ResearchLane" + i, i, "研究", "Research", "research"); LaneTitle("AutomationLane" + i, i, "自动化", "Automation", "auto"); LaneTitle("AtlasLane" + i, i, "图鉴 · 现象与能力", "Atlas · phenomena and abilities", "atlas"); }
                }
            }
            foreach (var pair in stageTitles)
            {
                bool visible = pair.Key <= stage; pair.Value.gameObject.SetActive(visible);
                miniButtons[pair.Key].rt.gameObject.SetActive(visible);
                if (visible)
                {
                    int i = pair.Key;
                    pair.Value.text = (i == 0 ? "⓪ " : i + " · ") + T(XgCatalog.StageNames[i], XgCatalog.StageNamesEn[i]) + (i > 0 ? "  <size=14><color=#6F95A5>" + XgCatalog.StageYears[i] + "</color></size>" : "");
                    miniButtons[i].Set(i == 0 ? Lang.T("标注") : i.ToString(), true, i == stage ? XgDark.AccentSoft : XgDark.Button);
                }
                // Lane titles from a previous save must not survive a new-game bind.
                foreach (string prefix in new[] { "TrunkLane", "VisionLane", "SequenceLane", "ResearchLane", "AutomationLane", "AtlasLane" })
                {
                    var lane = content.Find(prefix + pair.Key);
                    if (lane != null)
                    {
                        lane.gameObject.SetActive(visible);
                        var label = lane.GetComponent<TMP_Text>();
                        label.text = prefix == "TrunkLane" ? Lang.T("主干") : prefix == "VisionLane" ? Lang.T("视觉") : prefix == "SequenceLane" ? Lang.T("序列") : prefix == "ResearchLane" ? Lang.T("研究") : prefix == "AtlasLane" ? Lang.T("图鉴 · 现象与能力") : Lang.T("自动化");
                    }
                }
            }
            foreach (var node in XgCatalog.Nodes)
            {
                bool visible = Sim.NodeVisible(node);
                var item = nodes.Find(n => n.node.id == node.id);
                if (item == null && visible)
                {
                    item = MakeNode(node); nodes.Add(item);
                    if (animate && !rebound && !Fx.Reduced && node.stage > displayedStage) entrances[item] = 0;
                }
                if (item != null)
                {
                    item.gameObject.SetActive(visible);
                    if (!visible && selected == item) selected = null;
                    if (!visible && charging == item) { charging.charge = 0; charging = null; }
                }
            }
            content.sizeDelta = new Vector2((stage + 1) * XgCatalog.StageWidth + 54, XgCatalog.TreeHeight + 70);
            frontierWall.gameObject.SetActive(stage < 6);
            frontierWall.anchoredPosition = new Vector2((stage + 1) * XgCatalog.StageWidth - 16, -60);
            miniMap.gameObject.SetActive(stage > 1); toolbar[1].rt.gameObject.SetActive(stage > 1);
            if (animate && !rebound && stage > displayedStage) FocusFrontier();
            displayedStage = stage; Pan.Clamp();
            eraCard.SetAsLastSibling();
        }

        void LaneTitle(string id, int stage, string zh, string en, string lane)
        {
            var label = ui.Text(XgUi.TopLeft(id, content, stage * XgCatalog.StageWidth + 40, 70 + XgCatalog.LaneTop(lane) - 34, 200, 26), T(zh, en), 17, LaneColor(lane), TextAlignmentOptions.MidlineLeft);
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
        }

        static bool Hexagon(XgNode node) => node.kind == XgNodeKind.Breakthrough || node.kind == XgNodeKind.Project;
        /// <summary>Half the drawn width: where links start and end.</summary>
        static float HalfWidth(XgNode node) => Hexagon(node) ? XgCatalog.BreakthroughSize * .4f : XgCatalog.NodeWidth * .5f - 4;

        XgNodeView MakeNode(XgNode node)
        {
            var rt = Rect("Node " + node.id, content, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            bool hex = Hexagon(node);
            rt.sizeDelta = hex ? new Vector2(XgCatalog.BreakthroughSize, XgCatalog.BreakthroughSize) : new Vector2(XgCatalog.NodeWidth, XgCatalog.NodeHeight);
            rt.anchoredPosition = Pos(node);
            var graphic = rt.gameObject.AddComponent<XgNodeGraphic>();
            var item = rt.gameObject.AddComponent<XgNodeView>(); item.node = node; item.page = this; item.graphic = graphic;
            // Names are the point of the tree: large, bold, two lines at most, shrinking only for long English names.
            var labelRect = hex ? Rect("Label", rt, Vector2.zero, Vector2.one, new Vector2(12, 30), new Vector2(-12, -30))
                                : Rect("Label", rt, Vector2.zero, Vector2.one, new Vector2(10, node.maxLevel > 1 ? 15 : 9), new Vector2(-10, node.maxLevel > 1 ? -5 : -7));
            item.label = ui.Text(labelRect, "", 19, XgDark.Ink, TextAlignmentOptions.Center);
            item.label.fontStyle = FontStyles.Bold;
            item.label.enableAutoSizing = true; item.label.fontSizeMin = hex ? 12 : 13; item.label.fontSizeMax = hex ? 17 : 19;
            item.label.lineSpacing = -8;
            // Levelled nodes keep name and level on one line above their pips; shrink rather than wrap.
            if (node.maxLevel > 1) item.label.textWrappingMode = TextWrappingModes.NoWrap;
            item.cost = ui.Text(Rect("Cost", rt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(-10, -19), new Vector2(10, 1)), "", 15, XgDark.Money, TextAlignmentOptions.Center);
            item.cost.fontStyle = FontStyles.Bold;
            item.cost.raycastTarget = false;
            return item;
        }

        void Toolbar(int index)
        {
            Fx.Play(XgJuice.Sfx.Id.Click);
            if (index == 0) { FocusStage(0); return; }
            if (index == 1) { FocusFrontier(); return; }
            if (index == 3) { Pan.ZoomBy(1 / 1.25f); return; }
            if (index == 4) { Pan.ZoomBy(1.25f); return; }
            var buyable = nodes.FindAll(n => n.gameObject.activeSelf && Sim.Status(n.node, Host) == XgSim.NodeStatus.Buyable);
            if (buyable.Count == 0) { view.ShowToast(Lang.T("现在没有买得起的节点"), 2); return; }
            int at = selected != null ? buyable.IndexOf(selected) : -1;
            var next = buyable[(at + 1) % buyable.Count]; Hover(next); Focus(next); Fx.Knock((RectTransform)next.transform, .2f);
        }

        public void FocusFrontier() => FocusStage(Sim.S.stage);
        void FocusStage(int stage)
        {
            stage = Math.Max(0, Math.Min(Sim.S.stage, stage));
            Canvas.ForceUpdateCanvases();
            Pan.FitColumn(stage * XgCatalog.StageWidth + XgCatalog.StageWidth * .48f);
        }
        public RectTransform NodeTarget(string id)
        {
            var node = nodes.Find(n => n.node.id == id && n.gameObject.activeSelf);
            return node != null ? (RectTransform)node.transform : null;
        }
        public bool FocusNode(string id)
        {
            SyncRevealedContent(false);
            var node = nodes.Find(n => n.node.id == id && n.gameObject.activeSelf);
            if (node == null) return false;
            Hover(node); Focus(node); return true;
        }
        public override void Shown() { SyncRevealedContent(false); FocusFrontier(); }
        void Focus(XgNodeView node) => Pan.GlideTo(((RectTransform)node.transform).anchoredPosition);
        public void Hover(XgNodeView node)
        {
            if (!node.gameObject.activeSelf) return;
            bool changed = selected != node; selected = node;
            // The selected node's prerequisites in other lanes are drawn: rebuild the links at once.
            if (changed) Refresh(); else RefreshInfo();
        }
        /// <summary>Shows a self-insight card ("pretrain"); set by the card overlay.</summary>
        public static Action<string> OpenInsightCard;

        string InsightOf(XgNode n)
        {
            if (n == null || n.kind != XgNodeKind.Secret) return null;
            return n.id == "secret.6" && Sim.S.insights.Contains("pretrain") ? "pretrain" : null;
        }

        public void Press(XgNodeView node)
        {
            Hover(node);
            string card = InsightOf(node.node);
            if (card != null && OpenInsightCard != null) { OpenInsightCard(card); return; }
            var status = Sim.Status(node.node, Host);
            if (status == XgSim.NodeStatus.Buyable) { charging = node; node.charge = 0; chargeHeld = 0; return; }
            Fx.Knock((RectTransform)node.transform, .06f, new Vector2(8, 0));
            Fx.Play(status == XgSim.NodeStatus.Owned ? XgJuice.Sfx.Id.Click : XgJuice.Sfx.Id.Thud, 1.2f, .6f);
        }
        public void Release(XgNodeView node) { if (charging == node) { charging = null; node.charge = 0; } }
        public bool Buy(XgNode node, RectTransform from)
        {
            if (node == null) return false;
            purchaseCosts[node.id] = Sim.NodeCost(node);
            if (!Sim.BuyNode(node.id, Host)) { purchaseCosts.Remove(node.id); Fx.Play(XgJuice.Sfx.Id.Thud); return false; }
            Fx.Knock(from, .2f); Fx.Shockwave(Fx.At(from), XgDark.Gold, 160, .35f, 8); Fx.Burst(Fx.At(from), 24, XgDark.Gold, XgJuice.Shape.Yen, 300); return true;
        }

        public void OnBought(XgNode node)
        {
            double paid = purchaseCosts.TryGetValue(node.id, out var actual) ? actual : node.cost;
            purchaseCosts.Remove(node.id);
            SyncRevealedContent(true);
            Fx.Play(XgJuice.Sfx.Id.Unlock);
            var item = nodes.Find(n => n.node.id == node.id);
            if (item != null && root.gameObject.activeInHierarchy)
            {
                var rt = (RectTransform)item.transform; var at = Fx.At(rt);
                Fx.Knock(rt, .3f, Vector2.zero, .4f); Fx.Shockwave(at, LaneColor(node.lane), 200, .4f, 10);
                Fx.Burst(at, 24, XgDark.Gold, XgJuice.Shape.Yen, 320); Fx.Float(at + new Vector2(0, 60), "-¥" + Money(paid), XgDark.Money, 24);
                Fx.Flash(Color.white, .08f, .3f); Fx.HitStop(80);
                if (IsEraItem(node.id))
                {
                    for (int y = -120; y <= 120; y += 60) Fx.Crumble(at + new Vector2(0, y), Fx.Reduced ? 2 : 8, XgDark.Muted);
                    OnBreakthrough(node.id);
                }
            }
            RefreshInfo();
        }

        /// <summary>An architecture item's mark on its card: the ability it lowers and by how much (③×0.7).</summary>
        static string ItemTag(XgNode node)
        {
            var discounts = XgSim.DiscountsOf(node.id);
            if (discounts.Count == 0) return "";
            var sb = new System.Text.StringBuilder(" <size=80%><color=#C88A00>");
            foreach (var d in discounts)
                sb.Append("①②③④⑤⑥"[Mathf.Clamp(d.ability - 1, 0, 5)]).Append("×").Append(N(Math.Min(d.paramsFactor, d.samplesFactor), "0.0#"));
            return sb.Append("</color></size>").ToString();
        }

        /// <summary>The architecture items that turn the tree to their year (they were breakthroughs before the items).</summary>
        static bool IsEraItem(string id) => id == "mlp" || id == "lenet" || id == "rnn" || id == "lstm" || id == "resnet" || id == "seq2seq" || id == "attention" || id == "caption" || id == "transformer";

        public void OnBreakthrough(string id)
        {
            int year; string zh, en;
            switch (id)
            {
                case "mlp": year = 1986; zh = "反向传播 · 错误可以一层一层往回传"; en = "Backpropagation · errors can travel backward through layers"; break;
                case "lenet": year = 1989; zh = "卷积网络 · 先看局部，再看整体（LeCun 1989；LeNet-5 是 1998）"; en = "Convolution · local structure before the whole (LeCun 1989; LeNet-5 in 1998)"; break;
                case "rnn": year = 1990; zh = "循环网络 · 前一个词影响后一个词"; en = "Recurrent networks · the previous word affects the next"; break;
                case "lstm": year = 1997; zh = "LSTM · 用门守住记忆（「遗忘门」是 1999–2000 年加上的）"; en = "LSTM · gates guard the memory (the forget gate came in 1999–2000)"; break;
                case "resnet": year = 2015; zh = "ResNet · 让信息原样穿过深层网络"; en = "ResNet · let information pass through deep layers"; break;
                case "seq2seq": year = 2014; zh = "编码器-解码器 · 先读完整句，再从头说一遍"; en = "Encoder–decoder · read the whole sentence, then say it again"; break;
                case "attention": year = 2014; zh = "注意力 · 需要时回头看"; en = "Attention · look back when needed"; break;
                case "caption": year = 2015; zh = "空间注意力 · 先找到图里重要的部分"; en = "Spatial attention · find the part that matters"; break;
                case "transformer": year = 2017; zh = "Transformer · 只要注意力\n这是游戏中的提前突破；年份是真实历史。"; en = "Transformer · attention is all we need\nA fictional early discovery; the date is historical."; break;
                default: return;
            }
            SyncRevealedContent(true);
            eraTitle.text = year + " · " + Lang.T("时代卡"); eraBody.text = T(zh, en);
            eraCard.gameObject.SetActive(true); eraCard.SetAsLastSibling(); eraRemaining = 8;
            FocusFrontier();
            if (id == "transformer") { Fx.Flash(Color.white, .2f, .8f); Fx.HitStop(240); Fx.Shockwave(Fx.At(eraCard), XgDark.Gold, 500, .8f, 16); }
        }

        public override void Tick(float dt)
        {
            if (!built) return;
            if (eraRemaining > 0) { eraRemaining -= dt; if (eraRemaining <= 0) eraCard.gameObject.SetActive(false); }
            if (charging != null)
            {
                if (Pan.Dragging || !charging.gameObject.activeSelf || Sim.Status(charging.node, Host) != XgSim.NodeStatus.Buyable) { charging.charge = 0; charging = null; }
                else
                {
                    float before = chargeHeld; chargeHeld += dt;
                    if (chargeHeld >= ChargeDelay && before < ChargeDelay) Fx.Play(XgJuice.Sfx.Id.Charge, 1, .7f);
                    if (chargeHeld >= ChargeDelay) charging.charge = Mathf.Clamp01((chargeHeld - ChargeDelay) / (HoldSeconds - ChargeDelay));
                    if (charging.charge >= 1)
                    {
                        var item = charging; charging = null; item.charge = 0; item.held = false;
                        purchaseCosts[item.node.id] = Sim.NodeCost(item.node);
                        if (!Sim.BuyNode(item.node.id, Host)) purchaseCosts.Remove(item.node.id);
                        view.Refresh(true);
                    }
                }
            }
            if (entrances.Count > 0)
            {
                var current = new List<XgNodeView>(entrances.Keys);
                foreach (var node in current)
                {
                    float progress = Mathf.Min(1, entrances[node] + dt * 3); entrances[node] = progress;
                    float ease = 1 - Mathf.Pow(1 - progress, 3);
                    var rt = (RectTransform)node.transform; rt.localScale = Vector3.one * Mathf.Lerp(.75f, 1, ease);
                    rt.anchoredPosition = Pos(node.node) + new Vector2((1 - ease) * 90, 0);
                    if (progress >= 1) entrances.Remove(node);
                }
            }
            // Painted once per frame from here only, with a steady glow: nothing breathes or blinks (Refresh used to repaint
            // at a fixed glow every 0.2 s against this frame's pulse, which made buyable nodes flicker).
            foreach (var node in nodes) if (node.gameObject.activeSelf) Paint(node, SteadyGlow);
        }

        const float SteadyGlow = .6f;

        void Paint(XgNodeView node, float pulse)
        {
            var status = Sim.Status(node.node, Host); var color = LaneColor(node.node.lane); bool hover = node == selected;
            if (node.node.kind == XgNodeKind.Secret && InsightOf(node.node) != null)
            {
                // Worked out alone: the secret card turns gold (design v1.1 §5.3).
                node.graphic.Dashed = false; node.graphic.SetShape(false, 0, 1);
                node.graphic.Set(new Color32(255, 236, 170, 255), XgDark.Gold, 5, XgDark.Gold, .45f + .25f * pulse, 0);
                return;
            }
            if (XgSim.IsAtlas(node.node))
            {
                // 图鉴: dashed and grey until it happens; phenomena stay dashed, abilities turn gold.
                bool lit = status == XgSim.NodeStatus.Owned, ability = node.node.kind == XgNodeKind.Ability;
                node.graphic.Dashed = !ability || !lit;
                node.graphic.SetShape(false, 0, 1);
                if (!lit) node.graphic.Set(new Color32(13, 21, 30, 255), new Color32(58, 85, 102, 255), 3, Color.clear, 0, 0);
                else if (ability) node.graphic.Set(XgDark.Gold, new Color32(200, 140, 20, 255), 4, XgDark.Gold, hover ? .6f : .25f, 0);
                else node.graphic.Set(new Color32(14, 40, 44, 255), color, 4, color, hover ? .5f : 0, 0);
                return;
            }
            node.graphic.SetShape(node.node.kind == XgNodeKind.Breakthrough || node.node.kind == XgNodeKind.Project,
                node.node.tree == "label" ? Sim.LabelNodeLevel(node.node) : Sim.Has(node.node.id) ? 1 : 0, node.node.maxLevel);
            switch (status)
            {
                case XgSim.NodeStatus.Owned: node.graphic.Set(color, Color.Lerp(color, Color.black, .25f), 4, color, hover ? .6f : 0, 0); break;
                case XgSim.NodeStatus.Buyable: node.graphic.Set(XgDark.Card, XgDark.Gold, 6, XgDark.Gold, .5f + .5f * pulse + node.charge, node.charge); break;
                case XgSim.NodeStatus.TooExpensive: node.graphic.Set(XgDark.Card, Color.Lerp(color, XgDark.Card, .3f), 4, color, hover ? .4f : 0, 0); break;
                default: node.graphic.Set(new Color32(13, 21, 30, 255), new Color32(40, 60, 75, 255), 3, Color.clear, 0, 0); break;
            }
        }

        public override void Refresh()
        {
            if (!built) return;
            SyncRevealedContent(true);
            int buyable = 0;
            foreach (var node in nodes) if (node.gameObject.activeSelf && Sim.Status(node.node, Host) == XgSim.NodeStatus.Buyable) buyable++;
            header.text = "<b>" + Lang.T("科技") + "</b>  <color=#FAC775>¥ " + Money(Host.Money) + "</color>";
            toolbar[0].Set(Lang.T("⓪ 标注"), true); toolbar[1].Set(Lang.T("前沿"), true);
            toolbar[2].Set(Lang.T("可买 ") + buyable, true, buyable > 0 ? XgDark.AccentSoft : XgDark.Button);
            var edges = new List<XgLinksGraphic.Link>();
            foreach (var item in nodes)
            {
                if (!item.gameObject.activeSelf) continue;
                var node = item.node; var status = Sim.Status(node, Host); double cost = Sim.NodeCost(node);
                int level = node.tree == "label" ? Sim.LabelNodeLevel(node) : Sim.Has(node.id) ? 1 : 0;
                if (XgSim.IsAtlas(node))
                {
                    bool lit = status == XgSim.NodeStatus.Owned;
                    item.label.text = lit || node.kind == XgNodeKind.Ability ? Sim.NodeName(node) : Lang.T("？？？");
                    item.label.color = lit && node.kind == XgNodeKind.Ability ? Color.white : lit ? XgDark.Ink : XgDark.Muted;
                    item.cost.text = lit ? "<color=#5DCAA5>✓</color>" : "";
                    continue;
                }
                bool mystery = Sim.NodeMystery(node);
                item.label.text = Sim.NodeName(node) + (node.maxLevel > 1 && !mystery ? "  <size=70%><color=#" + (status == XgSim.NodeStatus.Owned ? "FFFFFFCC" : "68748C") + ">" + level + "/" + node.maxLevel + "</color></size>" : "");
                item.label.color = status == XgSim.NodeStatus.Owned ? Color.white : XgDark.Ink;
                item.cost.text = (status == XgSim.NodeStatus.Owned ? "<color=#5DCAA5>✓</color>" : mystery ? "" : cost > 0 && !double.IsInfinity(cost) ? "¥" + Money(cost) : "") + ItemTag(node);
                AddEdge(node.parent, node, false);
                foreach (string need in node.needs) AddEdge(need, node, true);
            }
            links.SetLinks(edges); RefreshInfo();
            void AddEdge(string parentId, XgNode node, bool dashed)
            {
                var parent = XgCatalog.Node(parentId);
                if (parent == null || !Sim.NodeVisible(parent)) return;
                // Only links inside a lane are always drawn; a prerequisite in another lane shows while either end is selected.
                bool crossLane = XgCatalog.Band(parent.lane) != XgCatalog.Band(node.lane);
                bool focus = selected != null && (selected.node == node || selected.node == parent);
                if (crossLane && !focus) return;
                var start = Pos(parent); var end = Pos(node);
                bool forward = end.x > start.x;
                float from = HalfWidth(parent), to = HalfWidth(node);
                var a = start + new Vector2(forward ? from : -from, 0); var b = end + new Vector2(forward ? -to : to, 0);
                // Links into another stage turn in that stage's left gutter, so a fan from one parent shares one bus;
                // links within a stage turn just left of the child.
                float? turn = !forward ? (float?)null : parent.stage != node.stage ? node.stage * XgCatalog.StageWidth + 40 : b.x - 16;
                Color color = focus ? XgDark.Gold : Sim.Has(node.id) ? LaneColor(node.lane) : new Color32(58, 85, 102, 210);
                edges.Add(new XgLinksGraphic.Link { a = a, b = b, color = color, width = focus ? 4 : Sim.Has(node.id) ? 4 : 2, dashed = dashed, turnX = turn });
            }
        }

        void RefreshInfo()
        {
            if (infoTitle == null) return;
            if (selected == null) { infoTitle.text = Lang.T("沿着前沿探索"); infoCost.text = ""; infoBody.text = Lang.T("只显示你已经发现的技术。\n\n拖动平移 · 滚轮上下\nShift + 滚轮左右 · −/+ 缩放\n按住节点 0.6 秒购买。"); return; }
            var node = selected.node; var status = Sim.Status(node, Host); double cost = Sim.NodeCost(node);
            if (XgSim.IsAtlas(node))
            {
                bool lit = status == XgSim.NodeStatus.Owned, ability = node.kind == XgNodeKind.Ability;
                infoTitle.text = lit || ability ? Sim.NodeName(node) : Lang.T("？？？");
                infoCost.text = lit ? Lang.T("已点亮") : Lang.T("未发现");
                infoBody.text = lit || ability ? T(node.note, node.noteEn) + "\n\n<color=#6F95A5>" + Sim.Why(node, Host) + "</color>"
                    : Lang.T("还没发生过。规则叠在一起，它自己会冒出来；第一次发生时会记进图鉴。");
                return;
            }
            if (Sim.NodeMystery(node))
            {
                // 自动答题 before the protagonist has the idea: no name, no price, only a hint (XgSim.Epiphany.cs).
                infoTitle.text = Sim.NodeName(node); infoCost.text = Lang.T("未发现"); infoBody.text = Sim.NodeNote(node);
                return;
            }
            infoTitle.text = Sim.NodeName(node);
            infoCost.text = status == XgSim.NodeStatus.Owned ? Lang.T("已拥有") : double.IsInfinity(cost) ? T("已满级", "Maxed") : "¥ " + Money(cost);
            var text = new System.Text.StringBuilder(Sim.NodeNote(node)).Append("\n\n");
            void Condition(bool ok, string zh, string en) => text.Append(ok ? "<color=#5DCAA5>✓ " : "<color=#E07B4F>○ ").Append(T(zh, en)).Append("</color>\n");
            if (node.parent != null) Condition(Sim.Has(node.parent) || node.parent == "label.raise" && Sim.RaiseLevel > 0 || node.parent == "label.auto" && Sim.GlobalAutoLevel > 0, Sim.NodeName(XgCatalog.Node(node.parent)), Sim.NodeName(XgCatalog.Node(node.parent)));
            foreach (string need in node.needs) Condition(Sim.Has(need), Sim.NodeName(XgCatalog.Node(need)), Sim.NodeName(XgCatalog.Node(need)));
            if (node.id == "caption") Condition(Sim.S.stage >= 5, "第五阶段", "Stage five");
            if (node.id == "transformer") Condition(Sim.TransformerIdeaReached, "它自己问出「如果只用注意力呢？」", "It asked: what if only attention?");
            // An architecture item: which ability's thresholds it lowers (参数量与数据量主线 §6).
            foreach (var d in XgSim.DiscountsOf(node.id))
            {
                string ability = XgSim.AbilityName(d.ability, Sim.English);
                text.Append("<color=#C88A00>").Append(T("道具 · 能力「" + ability + "」", "Item · ability '" + ability + "'"));
                if (d.paramsFactor < 1) text.Append(T(" 参数门槛 ×", " parameters ×")).Append(N(d.paramsFactor, "0.0#"));
                if (d.samplesFactor < 1) text.Append(T(" 数据门槛 ×", " data ×")).Append(N(d.samplesFactor, "0.0#"));
                text.Append("</color>\n");
            }
            if (status != XgSim.NodeStatus.Owned)
            {
                Condition(Host.Money + 1e-9 >= cost, "经费 ¥" + Money(cost), "Funds ¥" + Money(cost));
                text.Append("\n").Append(status == XgSim.NodeStatus.Buyable ? Lang.T("按住节点 0.6 秒购买") : Sim.Why(node, Host));
            }
            infoBody.text = text.ToString();
        }
    }
}
