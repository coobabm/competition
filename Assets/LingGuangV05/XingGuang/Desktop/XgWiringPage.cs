using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>A native, readable workbench over the game's existing resource graph. No second simulation or save format.</summary>
    public sealed class XgWiringPage : XgPage
    {
        // The redesign's dark scanner palette (XgDark), shared by every 灵光 page.
        static readonly Color Bg = XgDark.Page, Card = XgDark.Card;
        static readonly Color Ink = XgDark.Ink, Mute = XgDark.Muted;
        static readonly Color Line = XgDark.Line, Soft = XgDark.Panel3;
        static readonly Color Good = XgDark.Good, Bad = XgDark.Bad, Warn = XgDark.Hot;
        static readonly Color[] Tone = { new Color32(226, 170, 70, 255), new Color32(93, 168, 232, 255), new Color32(169, 155, 242, 255), new Color32(93, 202, 165, 255) };
        const float CardWidth = 198, CardHeight = 120, LaneStep = 242, Gap = 26, WorldWidth = 970;
        const int WorkingTasks = 3;

        sealed class NodeView
        {
            public string key;
            public XgWireLane lane;
            public RectTransform rt, inPort, outPort, fill;
            public Image frame, accent;
            public TMP_Text title, sub, status, metric;
            public CanvasGroup group;
            public Vector2 position;
            public bool visible;
        }
        sealed class Cable { public string from, to; public XgWireLane lane; public bool live; }
        sealed class Pulse { public string task; public float progress; public bool correct; }

        readonly Dictionary<string, NodeView> views = new Dictionary<string, NodeView>();
        readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
        readonly List<NodeView> order = new List<NodeView>();
        readonly List<Cable> cables = new List<Cable>();
        readonly HashSet<string> pinned = new HashSet<string>(), visibleKeys = new HashSet<string>();
        readonly List<Pulse> pulses = new List<Pulse>();
        readonly Dictionary<string, int> completed = new Dictionary<string, int>();
        readonly List<XgBtn> filterButtons = new List<XgBtn>();
        readonly TMP_Text[] values = new TMP_Text[5], labels = new TMP_Text[5];
        readonly RectTransform[] meters = new RectTransform[3];
        RectTransform viewport, canvas, nodesBox, inventory, inventoryContent, detail, detailContent, detailActions;
        XgWiringGraphic back, front;
        XgPan pan;
        XgBtn inventoryButton, detailButton, disconnectButton, trainButton, resetButton;
        TMP_Text footer, scope, zoomLabel, detailTitle, detailStatus, detailRows, detailNote, logText, detailLinks, tripText;
        TMP_Text inventoryTitle;
        RectTransform tripBanner;
        XgSim boundSim;
        string selected, hovered, armed, selectedFrom, selectedTo;
        string topologyKey = "", inventoryKey = "";
        int workspaceFilter;
        bool armedInput, inventoryOpen, detailOpen, fitPending = true;
        bool draggingNode, draggingPort;
        string dragNodeKey;
        Vector2 dragStart, nodeStart, pointer;
        float refreshClock, animationClock, suppressClickUntil;
        string hint = "";
        float hintUntil;
        Vector2 lastViewport;

        public string QaSelected => selected;
        public string QaArmed => armed;
        public string QaHovered => hovered;
        public float QaZoom => canvas != null ? canvas.localScale.x : 0;
        public int QaVisibleNodeCount => order.Count;
        public int QaWorkspaceFilter => workspaceFilter;
        public float QaScroll(int lane) => canvas != null ? canvas.anchoredPosition.y : 0;
        public float QaMaxScroll(int lane) => canvas != null && viewport != null ? Mathf.Max(0, canvas.rect.height * QaZoom - viewport.rect.height) : 0;

        static Color Alpha(Color c, float a) { c.a = a; return c; }
        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
        static string F(double n, string format = "0") => N(n, format);
        static string Plain(string s) => (s ?? "").Replace("<", "‹").Replace(">", "›");
        static string Tint(string text, Color color) => "<color=#" + Hex(color) + ">" + Plain(text) + "</color>";
        Image Rounded(RectTransform rt, Color color, bool raycast = false)
        { var image = XgCardArt.Sliced(rt, color, 2); image.raycastTarget = raycast; return image; }
        XgBtn Button(Transform parent, string name, string text, Action action, float size = 12)
        {
            var b = ui.Button(parent, text, () => action(), size);
            b.rt.name = name; b.image.sprite = XgCardArt.Rounded(); b.image.type = Image.Type.Sliced; b.image.pixelsPerUnitMultiplier = 2;
            b.image.color = Soft; b.label.color = Ink;
            return b;
        }
        TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            var t = ui.Text(rt, text, size, color, alignment);
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        public override void Build(RectTransform area)
        {
            root = Rect("wiring", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(root, Bg).raycastTarget = false;
            Text(Strip("WorkbenchTitle", root, 6, 38, 18, 210), T("让模型，替你工作。", "Put your models to work."), 20, Ink, TextAlignmentOptions.MidlineLeft);
            Text(Strip("WorkbenchSubtitle", root, 42, 20, 18, 210), T("电力 → 算力 → 模型 → 任务  /  游戏资源实时接入", "POWER → COMPUTE → MODEL → TASK  /  LIVE GAME RESOURCES"), 12, Mute, TextAlignmentOptions.MidlineLeft);
            inventoryButton = Button(root, "InventoryToggle", T("+ 资源仓库", "+ Resources"), () => { inventoryOpen = !inventoryOpen; inventory.gameObject.SetActive(inventoryOpen); });
            PlaceTopRight(inventoryButton, 202, 19, 91, 29);
            detailButton = Button(root, "DetailToggle", T("节点详情", "Inspector"), () => SetDetails(!detailOpen));
            PlaceTopRight(detailButton, 102, 19, 88, 29);

            var kpi = Strip("ResourceMetrics", root, 72, 64, 14, 14);
            string[] names = { T("家庭电力负载", "HOME POWER"), T("算力占用", "COMPUTE"), T("显存占用", "VRAM"), T("执行中 / 已接入", "RUNNING / WIRED"), T("任务净收入", "NET INCOME") };
            for (int i = 0; i < 5; i++)
            {
                var box = Rect("Metric" + i, kpi, new Vector2(i / 5f, 0), new Vector2((i + 1) / 5f, 1), new Vector2(3, 0), new Vector2(-3, 0));
                Rounded(box, Card);
                labels[i] = Text(Strip("Label", box, 7, 17, 11, 8), names[i], 11, Mute, TextAlignmentOptions.MidlineLeft);
                values[i] = Text(Strip("Value", box, 27, 25, 11, 8), "—", 20, i == 4 ? Good : Ink, TextAlignmentOptions.MidlineLeft);
                values[i].textWrappingMode = TextWrappingModes.NoWrap;
                if (i < 3) meters[i] = Bar(Rect("Meter", box, Vector2.zero, new Vector2(1, 0), new Vector2(11, 5), new Vector2(-11, 8)), "Fill", Soft, Tone[i]);
            }
            string[] filters = { T("工作集", "Working set"), T("全部", "All"), T("订单", "Orders"), T("标注", "Labels"), T("训练", "Training"), T("生活", "Life") };
            for (int i = 0; i < filters.Length; i++)
            {
                int filter = i;
                var b = Button(root, "Filter" + i, filters[i], () => SetWorkspaceFilter(filter), 11);
                PlaceTopLeft(b, 16 + i * 65, 149, 60, 26); filterButtons.Add(b);
            }
            scope = Text(Rect("Scope", root, new Vector2(0, 1), Vector2.one, new Vector2(418, -175), new Vector2(-181, -149)), "", 11, Mute, TextAlignmentOptions.MidlineRight);
            var arrange = Button(root, "Arrange", T("整理节点", "Arrange"), Arrange, 11); PlaceTopRight(arrange, 169, 149, 77, 26);
            var fit = Button(root, "Fit", T("适应画布", "Fit"), Fit, 11); PlaceTopRight(fit, 87, 149, 73, 26);

            viewport = Rect("WorkbenchViewport", root, Vector2.zero, Vector2.one, new Vector2(12, 38), new Vector2(-12, -184));
            Panel(viewport, Bg);
            viewport.gameObject.AddComponent<RectMask2D>();
            canvas = Rect("WorkbenchCanvas", viewport, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            canvas.pivot = new Vector2(0, 1); canvas.sizeDelta = new Vector2(WorldWidth, 460);
            pan = viewport.gameObject.AddComponent<XgPan>(); pan.content = canvas; pan.viewport = viewport; pan.minZoom = .65f; pan.maxZoom = 1.5f;
            back = Layer("Wires", false);
            nodesBox = Rect("Nodes", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            front = Layer("Activity", true);
            for (int i = 0; i < 4; i++)
                Text(TopLeft("Lane" + i, nodesBox, 20 + i * LaneStep, 9, CardWidth, 20), "0" + (i + 1) + "  " + new[] { T("电力供给", "POWER"), T("可用算力", "COMPUTE"), T("模型", "MODELS"), T("游戏任务", "TASKS") }[i], 12, Mute, TextAlignmentOptions.MidlineLeft);

            footer = Text(Rect("Footer", root, Vector2.zero, new Vector2(1, 0), new Vector2(17, 6), new Vector2(-168, 31)), "", 11, Mute, TextAlignmentOptions.MidlineLeft);
            footer.textWrappingMode = TextWrappingModes.NoWrap;
            var minus = Button(root, "ZoomOut", "−", () => pan.ZoomBy(1 / 1.15f), 16); PlaceBottomLeft(minus, 0, 0, 29, 25); AnchorBottomRight(minus.rt, 153, 6, 29, 25);
            zoomLabel = Text(Rect("ZoomLevel", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-116, 6), new Vector2(-65, 31)), "100%", 11, Mute, TextAlignmentOptions.Center);
            var plus = Button(root, "ZoomIn", "+", () => pan.ZoomBy(1.15f), 16); AnchorBottomRight(plus.rt, 58, 6, 29, 25);

            inventory = Drawer("ResourceWarehouse", 232, true, out inventoryContent);
            inventoryTitle = Text(Strip("Title", inventory, 8, 25, 12, 44), T("资源仓库", "Resources"), 16, Ink, TextAlignmentOptions.MidlineLeft);
            var closeInv = Button(inventory, "CloseInventory", "×", () => { inventoryOpen = false; inventory.gameObject.SetActive(false); }, 18); PlaceTopRight(closeInv, 38, 8, 28, 25);
            detail = Drawer("Inspector", 264, false, out detailContent);
            Text(Strip("InspectorTitle", detail, 8, 25, 12, 45), T("节点详情", "Inspector"), 15, Ink, TextAlignmentOptions.MidlineLeft);
            var closeDetail = Button(detail, "CloseInspector", "×", () => SetDetails(false), 18); PlaceTopRight(closeDetail, 38, 8, 28, 25);
            detailTitle = Text(Strip("SelectedName", detailContent, 7, 47, 12, 12), "", 17, Ink, TextAlignmentOptions.MidlineLeft);
            detailStatus = Text(Strip("SelectedStatus", detailContent, 55, 24, 12, 12), "", 12, Good, TextAlignmentOptions.MidlineLeft);
            detailRows = Text(Strip("Details", detailContent, 87, 157, 12, 12), "", 12, Ink, TextAlignmentOptions.TopLeft);
            detailRows.lineSpacing = 8;
            detailNote = Text(Strip("Reason", detailContent, 253, 65, 12, 12), "", 12, Mute, TextAlignmentOptions.TopLeft);
            detailActions = Rect("InspectorActions", detail, Vector2.zero, new Vector2(1, 0), new Vector2(4, 4), new Vector2(-4, 84));
            Panel(detailActions, Card).raycastTarget = false;
            disconnectButton = Button(detailActions, "Disconnect", T("断开输入线路", "Disconnect input"), DisconnectSelected, 12); PlaceTopLeft(disconnectButton, 12, 326, 228, 29);
            trainButton = Button(detailActions, "TrainOnce", T("训练一次", "Run one epoch"), TrainSelected, 12); PlaceTopLeft(trainButton, 12, 362, 228, 29);
            detailLinks = Text(Strip("Connections", detailContent, 402, 120, 12, 12), "", 11, Mute, TextAlignmentOptions.TopLeft);
            Text(Strip("LogTitle", detailContent, 537, 22, 12, 12), T("运行记录", "Activity"), 13, Ink, TextAlignmentOptions.MidlineLeft);
            logText = Text(Strip("ActivityLog", detailContent, 570, 190, 12, 12), "", 11, Mute, TextAlignmentOptions.TopLeft);
            detailContent.sizeDelta = new Vector2(0, 776);
            inventory.gameObject.SetActive(false); detail.gameObject.SetActive(false);

            tripBanner = Rect("PowerWarning", root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(22, 47), new Vector2(-22, 90));
            Rounded(tripBanner, new Color32(60, 30, 20, 255), true);
            tripText = Text(Rect("Warning", tripBanner, Vector2.zero, Vector2.one, new Vector2(12, 5), new Vector2(-153, -5)), "", 12, Warn, TextAlignmentOptions.MidlineLeft);
            resetButton = Button(tripBanner, "ResetBreaker", T("尝试合闸", "Reset breaker"), ResetBreaker, 11);
            PlaceTopRight(resetButton, 136, 8, 124, 27);
            tripBanner.gameObject.SetActive(false);
        }

        static void AnchorBottomRight(RectTransform rt, float right, float y, float width, float height)
        { rt.anchorMin = rt.anchorMax = new Vector2(1, 0); rt.offsetMin = new Vector2(-right, y); rt.offsetMax = new Vector2(-right + width, y + height); }
        RectTransform Drawer(string name, float width, bool left, out RectTransform content)
        {
            var outer = Rect(name, root, new Vector2(left ? 0 : 1, 0), new Vector2(left ? 0 : 1, 1), new Vector2(left ? 16 : -width - 16, 45), new Vector2(left ? width + 16 : -16, -185));
            Rounded(outer, Line, true);
            Rounded(Rect("DrawerBody", outer, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1)), Card);
            var clip = Rect("ScrollViewport", outer, Vector2.zero, Vector2.one, new Vector2(4, 6), new Vector2(-4, -40));
            Panel(clip, Color.clear); clip.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", clip, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero); content.pivot = new Vector2(.5f, 1);
            var scroll = clip.gameObject.AddComponent<ScrollRect>(); scroll.content = content; scroll.viewport = clip; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            return outer;
        }
        XgWiringGraphic Layer(string name, bool foreground)
        {
            var rt = Rect(name, canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); rt.pivot = new Vector2(0, 1);
            if (rt.GetComponent<CanvasRenderer>() == null) rt.gameObject.AddComponent<CanvasRenderer>();
            var g = rt.gameObject.AddComponent<XgWiringGraphic>(); g.page = this; g.front = foreground; g.raycastTarget = !foreground; return g;
        }
        public override void Shown() { refreshClock = 0; if (order.Count == 0) fitPending = true; }
        public override void Refresh()
        {
            if (Sim == null || root == null) return;
            Bind(); var v = Sim.Wiring;
            RefreshMetrics(v); RefreshInventory(v); Reconcile(v); RefreshCards(v); RefreshInspector(v);
        }
        void Bind()
        {
            if (ReferenceEquals(boundSim, Sim)) return;
            if (boundSim != null) { boundSim.AutoRouted -= OnRouted; boundSim.EpochDone -= OnEpoch; boundSim.WireItem -= OnItem; }
            boundSim = Sim;
            boundSim.AutoRouted += OnRouted; boundSim.EpochDone += OnEpoch; boundSim.WireItem += OnItem;
            positions.Clear(); pinned.Clear(); pulses.Clear(); completed.Clear(); selected = armed = hovered = selectedFrom = selectedTo = null;
            topologyKey = inventoryKey = ""; fitPending = true;
        }
        public override void Tick(float dt)
        {
            if (Sim == null || root == null) return;
            if (root.gameObject.activeInHierarchy && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CancelWire();
            animationClock += dt; refreshClock -= dt;
            if (refreshClock <= 0) { refreshClock = .2f; Refresh(); }
            if (viewport.rect.size != lastViewport) { lastViewport = viewport.rect.size; fitPending = true; }
            if (fitPending && viewport.rect.width > 1 && order.Count > 0) { fitPending = false; Fit(); }
            if (armed != null && !draggingPort && XgCardArt.Pointer(canvas, out var p)) pointer = p;
            for (int i = pulses.Count - 1; i >= 0; i--) { pulses[i].progress += dt * .72f; if (pulses[i].progress >= 1) pulses.RemoveAt(i); }
            zoomLabel.text = Mathf.RoundToInt(QaZoom * 100) + "%";
            footer.text = Time.unscaledTime < hintUntil ? hint : T("拖动节点整理 · 空白处拖动平移 · 点击或拖动端口接线 · 点线查看", "Drag cards / canvas · Click or drag ports to wire · Click a cable to inspect");
            front.SetVerticesDirty();
        }

        void RefreshMetrics(XgWiringView v)
        {
            values[0].text = F(v.psuW) + Small(" / " + F(v.psuLimit) + " W");
            values[1].text = F(v.cuUse) + Small(" / " + F(v.cuOn) + " CU");
            values[2].text = F(v.vramUse / 1024, "0.0") + Small(" / " + F(v.vramOn / 1024, "0.#") + " GB");
            values[3].text = v.running + Small(" / " + v.wired);
            var house = host.Controller != null && host.Controller.runtime != null ? host.Controller.runtime.Sim : null;
            double bill = house != null ? house.PowerWatts / 1000 * 24 / house.Config.dayLengthSeconds * house.Config.electricityPrice : 0;
            double net = v.incomePerSecond - v.rentPerSecond - bill;
            values[4].text = (net >= 0 ? "+" : "−") + F(Math.Abs(net), "0.00") + Small(T(" ¥/秒", " ¥/s")); values[4].color = net >= 0 ? Good : Bad;
            SetBar(meters[0], v.psuLimit > 0 ? (float)(v.psuW / v.psuLimit) : 0);
            SetBar(meters[1], v.cuOn > 0 ? (float)(v.cuUse / v.cuOn) : 0);
            SetBar(meters[2], v.vramOn > 0 ? (float)(v.vramUse / v.vramOn) : 0);
            tripBanner.gameObject.SetActive(v.tripped || Sim.AjiePending);
            if (v.tripped) { tripText.text = T("机箱跳闸：断开部分显卡后，尝试合闸。任务进度保留。", "Breaker tripped. Unplug a card, then reset. Progress is kept."); resetButton.Set(T("尝试合闸", "Reset breaker"), true, Soft, Warn); }
            else if (Sim.AjiePending) { tripText.text = T("阿杰在问网吧的机器为什么一直运转。你可以去 YY 回答，或告诉他正在训练 AI。", "Ajie asks why the café PCs keep running. Answer in YY or tell him about training."); resetButton.Set(T("告诉他在训练 AI", "Tell the truth"), true, Soft, Good); }
        }
        static string Small(string text) => "<size=11><color=#7C8D71>" + text + "</color></size>";

        bool InCategory(XgWNode n)
        {
            if (workspaceFilter <= 1) return true;
            int category = n.key.StartsWith("contract:", StringComparison.Ordinal) || n.key == "ajie" ? 2 : n.key.StartsWith("label:", StringComparison.Ordinal) ? 3 : n.key.StartsWith("train:", StringComparison.Ordinal) || n.key == "pretrain" ? 4 : 5;
            return category == workspaceFilter;
        }
        void Include(XgWiringView v, string key)
        {
            var n = v.Node(key); if (n == null || !n.available || !visibleKeys.Add(key)) return;
            if (n.lane == XgWireLane.Task) Include(v, n.model.Length > 0 ? n.model : n.candidate);
            else if (n.lane == XgWireLane.Model) { if (n.run) foreach (string card in n.cards) Include(v, card); else Include(v, n.card); }
            else if (n.lane == XgWireLane.Gpu) Include(v, n.source);
        }
        void BuildVisible(XgWiringView v)
        {
            visibleKeys.Clear();
            int count = 0;
            foreach (var n in v.nodes)
            {
                if (n.lane != XgWireLane.Task || !n.available || !n.shown || !InCategory(n)) continue;
                if (workspaceFilter == 0 && count >= WorkingTasks) continue;
                Include(v, n.key); count++;
            }
            foreach (string key in pinned)
            {
                var pinnedNode = v.Node(key);
                if (pinnedNode != null && pinnedNode.lane == XgWireLane.Task && workspaceFilter >= 2 && !InCategory(pinnedNode)) continue;
                Include(v, key);
            }
            if (count == 0)
                foreach (var n in v.nodes)
                    if (n.available && n.shown && n.lane != XgWireLane.Task && (!n.repo || pinned.Contains(n.key))) Include(v, n.key);
            // Keep owned resources discoverable even when this working set has no active job.
            foreach (var n in v.nodes) if (n.available && n.shown && (n.lane == XgWireLane.Power && n.key != "idc" || n.lane == XgWireLane.Gpu && n.house)) Include(v, n.key);
        }
        string GraphKey(XgWiringView v)
        {
            var sb = new StringBuilder().Append(workspaceFilter).Append('|');
            foreach (var n in v.nodes)
            {
                if (!visibleKeys.Contains(n.key)) continue;
                sb.Append(n.key).Append(':').Append(n.source).Append('/').Append(n.card).Append('/').Append(n.model).Append('/').Append(n.online).Append('/').Append((int)n.state);
                foreach (var c in n.cards) sb.Append(c).Append(',');
                sb.Append(';');
            }
            return sb.ToString();
        }
        void Reconcile(XgWiringView v)
        {
            BuildVisible(v); string key = GraphKey(v); if (key == topologyKey) return; topologyKey = key;
            order.Clear(); var rows = new int[4];
            foreach (var n in v.nodes)
            {
                if (!visibleKeys.Contains(n.key)) continue;
                int lane = (int)n.lane;
                if (!positions.TryGetValue(n.key, out var position))
                {
                    position = new Vector2(18 + LaneStep * lane, 47 + rows[lane] * (CardHeight + Gap));
                    bool occupied;
                    do
                    {
                        occupied = false;
                        foreach (var saved in positions)
                            if (visibleKeys.Contains(saved.Key) && Mathf.Abs(saved.Value.x - position.x) < CardWidth && Mathf.Abs(saved.Value.y - position.y) < CardHeight + 8)
                            { position.y += CardHeight + Gap; occupied = true; break; }
                    } while (occupied);
                    positions[n.key] = position;
                }
                rows[lane]++;
                if (!views.TryGetValue(n.key, out var node)) views[n.key] = node = CreateNode(n);
                node.position = position; node.visible = true; node.rt.gameObject.SetActive(true); Position(node); order.Add(node);
            }
            foreach (var kv in views) if (!visibleKeys.Contains(kv.Key)) { kv.Value.visible = false; kv.Value.rt.gameObject.SetActive(false); }
            UpdateWorldBounds(); BuildCables(v);
            int tasks = 0, total = 0; foreach (var n in v.nodes) if (n.lane == XgWireLane.Task && n.available && n.shown) total++;
            foreach (var n in order) if (n.lane == XgWireLane.Task) tasks++;
            scope.text = T("画布 ", "Showing ") + tasks + " / " + total + T(" 个任务", " tasks");
            for (int i = 0; i < filterButtons.Count; i++) filterButtons[i].Set(filterButtons[i].label.text, true, i == workspaceFilter ? XgDark.OnFill : Bg, i == workspaceFilter ? Good : Mute);
            back.SetVerticesDirty(); front.SetVerticesDirty();
        }
        NodeView CreateNode(XgWNode n)
        {
            var node = new NodeView { key = n.key, lane = n.lane };
            node.rt = TopLeft("Node " + n.key, nodesBox, 0, 0, CardWidth, CardHeight);
            node.group = node.rt.gameObject.AddComponent<CanvasGroup>();
            node.frame = Rounded(node.rt, Line, true);
            Rounded(Rect("Body", node.rt, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1)), Card);
            var button = node.rt.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = node.frame; button.transition = Selectable.Transition.None;
            string key = n.key; button.onClick.AddListener(() => Select(key));
            if (ui.window != null) node.rt.gameObject.AddComponent<ChapterOneWindowFocus>().Window = ui.window;
            var drag = node.rt.gameObject.AddComponent<XgWiringHover>(); drag.page = this; drag.key = key;
            var dot = TopLeft("Kind", node.rt, 12, 15, 6, 6); node.accent = Rounded(dot, Tone[(int)n.lane]);
            node.title = Text(TopLeft("Title", node.rt, 25, 8, 160, 39), "", 14, Ink, TextAlignmentOptions.MidlineLeft);
            node.title.maxVisibleLines = 2;
            node.sub = Text(TopLeft("Resource", node.rt, 13, 52, 172, 21), "", 11, Mute, TextAlignmentOptions.MidlineLeft); node.sub.textWrappingMode = TextWrappingModes.NoWrap;
            node.fill = Bar(TopLeft("Load", node.rt, 13, 81, 172, 4), "Fill", Soft, Tone[(int)n.lane]);
            node.status = Text(TopLeft("State", node.rt, 13, 94, 105, 19), "", 11, Good, TextAlignmentOptions.MidlineLeft); node.status.textWrappingMode = TextWrappingModes.NoWrap;
            node.metric = Text(TopLeft("Metric", node.rt, 117, 94, 68, 19), "", 10, Mute, TextAlignmentOptions.MidlineRight); node.metric.textWrappingMode = TextWrappingModes.NoWrap;
            if (n.lane != XgWireLane.Power) node.inPort = Port(node.rt, true, key);
            if (n.lane != XgWireLane.Task) node.outPort = Port(node.rt, false, key);
            return node;
        }
        RectTransform Port(RectTransform parent, bool input, string key)
        {
            var rt = Rect(input ? "In" : "Out", parent, new Vector2(input ? 0 : 1, .5f), new Vector2(input ? 0 : 1, .5f), new Vector2(-13, -13), new Vector2(13, 13));
            var image = Panel(rt, Color.clear); var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = image; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => PortClick(key, input));
            var relay = rt.gameObject.AddComponent<XgWiringPort>(); relay.page = this; relay.key = key; relay.input = input;
            if (ui.window != null) rt.gameObject.AddComponent<ChapterOneWindowFocus>().Window = ui.window;
            return rt;
        }
        void Position(NodeView n) { n.rt.offsetMin = new Vector2(n.position.x, -n.position.y - CardHeight); n.rt.offsetMax = new Vector2(n.position.x + CardWidth, -n.position.y); }
        void UpdateWorldBounds()
        {
            float width = WorldWidth, height = 420;
            foreach (var n in order) { width = Mathf.Max(width, n.position.x + CardWidth + 22); height = Mathf.Max(height, n.position.y + CardHeight + 26); }
            canvas.sizeDelta = new Vector2(width, height);
        }
        void BuildCables(XgWiringView v)
        {
            cables.Clear();
            foreach (var n in v.nodes)
            {
                if (!visibleKeys.Contains(n.key)) continue;
                if (n.lane == XgWireLane.Gpu) AddCable(n.source, n.key, XgWireLane.Power, n.online);
                if (n.lane == XgWireLane.Model) { if (n.run) foreach (string card in n.cards) AddCable(card, n.key, XgWireLane.Gpu, n.online); else AddCable(n.card, n.key, XgWireLane.Gpu, n.online); }
                if (n.lane == XgWireLane.Task) AddCable(n.model, n.key, XgWireLane.Model, n.state == XgTaskState.Run);
            }
        }
        void AddCable(string from, string to, XgWireLane lane, bool live)
        { if (!string.IsNullOrEmpty(from) && visibleKeys.Contains(from)) cables.Add(new Cable { from = from, to = to, lane = lane, live = live }); }
        string NodeName(XgWNode n)
        {
            if (n == null) return "";
            if (n.key.StartsWith("contract:", StringComparison.Ordinal)) { var c = XgCatalog.Contract(n.key.Substring(9)); if (c != null) return T(c.job, c.jobEn); }
            return n.name;
        }
        string Status(XgWNode n)
        {
            if (!n.available) return T("未解锁", "Locked");
            if (n.bad) return T("需要处理", "Blocked");
            if (n.lane == XgWireLane.Task) return n.state == XgTaskState.Run ? n.warn ? T("算力受限", "Limited") : T("执行中", "Running") : n.held ? T("已暂停", "Paused") : T("待命", "Ready");
            return n.online ? n.warn ? T("资源受限", "Limited") : T("已就绪", "Ready") : T("等待接入", "Offline");
        }
        string Resource(XgWNode n)
        {
            if (n.lane == XgWireLane.Power) return F(n.loadW) + " / " + F(n.capW) + " W";
            if (n.lane == XgWireLane.Gpu) return F(n.load) + "/" + F(n.cu) + " CU  ·  " + F(n.vramUse / 1024, "0.0") + "/" + F(n.vramMB / 1024, "0.#") + " GB";
            if (n.lane == XgWireLane.Model) return n.run ? T("多卡训练线", "Training pipeline") : n.repo ? T("仓库检查点 · 可回炉", "Checkpoint · can retrain") : n.self ? T("通用模型 · 生活协作", "General · everyday tasks") : T("准确率 ", "Accuracy ") + F(n.acc * 100, "0.0") + "% · " + F(n.vramNeed, "0") + " MB";
            return n.state == XgTaskState.Run ? (n.earns ? "¥" + F(n.pay, "0.##") + "/" + n.unit + "  ·  " : "") + F(n.rate, "0.##") + " " + n.unit + T("/秒", "/s") : !string.IsNullOrEmpty(n.why) ? n.why : T("等待模型分配", "Waiting for a model");
        }
        float Fill(XgWNode n)
        {
            if (n.lane == XgWireLane.Power) return n.capW > 0 ? (float)(n.loadW / n.capW) : 0;
            if (n.lane == XgWireLane.Gpu) return n.cu > 0 ? (float)(n.load / n.cu) : 0;
            if (n.lane == XgWireLane.Model) return n.online ? (float)n.speed : 0;
            if (n.key.StartsWith("train:", StringComparison.Ordinal)) { var r = Sim.Run(n.key == "train:0" ? XgTrack.Vision : XgTrack.Sequence); return r.epochActive && r.epochDuration > 0 ? (float)(r.epochProgress / r.epochDuration) : 0; }
            return n.state == XgTaskState.Run ? (float)n.speed : 0;
        }
        void RefreshCards(XgWiringView v)
        {
            foreach (var node in order)
            {
                var n = v.Node(node.key); if (n == null) continue;
                node.title.text = Plain(NodeName(n)); node.sub.text = Plain(Resource(n)); node.status.text = Status(n); node.status.color = n.bad ? Bad : n.warn ? Warn : n.online || n.state == XgTaskState.Run ? Good : Mute;
                bool pick = selected == n.key || hovered == n.key || armed == n.key;
                node.frame.color = pick ? Tone[(int)n.lane] : n.bad ? new Color32(140, 55, 45, 255) : Line;
                SetBar(node.fill, Fill(n));
                int count = 0; foreach (var c in cables) if (c.from == n.key) count++;
                node.metric.text = n.lane == XgWireLane.Task ? n.realItems ? completed.TryGetValue(n.key, out int done) ? done + T(" 次完成", " done") : T("事件驱动", "Events") : T("持续任务", "Continuous") : count + T(" 条输出", " outputs");
                node.group.alpha = 1;
            }
        }

        void RefreshInventory(XgWiringView v)
        {
            var sb = new StringBuilder().Append(Lang.English);
            foreach (var n in v.nodes) sb.Append(n.key).Append(n.available).Append(n.name).Append(n.lockWhy);
            string key = sb.ToString(); if (key == inventoryKey) return; inventoryKey = key;
            foreach (Transform child in inventoryContent) UnityEngine.Object.Destroy(child.gameObject);
            float y = 7; string[] names = { T("电力供给", "Power"), T("可用算力", "Compute"), T("模型与检查点", "Models & checkpoints"), T("游戏任务", "Tasks") };
            for (int lane = 0; lane < 4; lane++)
            {
                Text(Strip("Category" + lane, inventoryContent, y, 22, 9, 9), names[lane], 12, Tone[lane], TextAlignmentOptions.MidlineLeft); y += 29;
                foreach (var n in v.nodes)
                {
                    if ((int)n.lane != lane) continue;
                    string id = n.key;
                    var b = Button(inventoryContent, "Resource " + id, "", () => InventoryClick(id), 12);
                    b.rt.anchorMin = new Vector2(0, 1); b.rt.anchorMax = new Vector2(1, 1); b.rt.offsetMin = new Vector2(6, -y - 49); b.rt.offsetMax = new Vector2(-6, -y);
                    b.label.alignment = TextAlignmentOptions.MidlineLeft; b.label.textWrappingMode = TextWrappingModes.Normal; b.label.overflowMode = TextOverflowModes.Ellipsis;
                    b.Set(Plain(NodeName(n)) + "\n<size=10>" + Tint(n.available ? InventoryInfo(n) : T("未解锁：", "Locked: ") + n.lockWhy, n.available ? Mute : Warn) + "</size>", true, n.available ? Soft : Bg, n.available ? Ink : Mute);
                    y += 55;
                }
                y += 9;
            }
            inventoryContent.sizeDelta = new Vector2(0, y + 6);
        }
        string InventoryInfo(XgWNode n)
        {
            if (n.lane == XgWireLane.Power) return F(n.capW) + " W" + (n.key == "cafe" ? " · ¥" + F(Sim.CafeRentPerSecond(Sim.CafeNight), "0.00") + T("/秒/台", "/s per PC") : "");
            if (n.lane == XgWireLane.Gpu) return F(n.cu) + " CU · " + F(n.vramMB / 1024, "0.#") + " GB" + (n.cafe ? " · ¥" + F(Sim.CafeRentPerSecond(Sim.CafeNight), "0.00") + T("/秒", "/s") : "");
            if (n.lane == XgWireLane.Task) return n.earns ? "¥" + F(n.pay, "0.##") + "/" + n.unit : T("游戏内协作任务", "In-game cooperation");
            return n.sub;
        }
        void InventoryClick(string key)
        {
            var n = Sim.Wiring.Node(key); if (n == null) return;
            if (!n.available) { Hint(n.lockWhy); host.ShowToast(n.lockWhy, 4); return; }
            pinned.Add(key); topologyKey = ""; Refresh();
            inventoryOpen = false; inventory.gameObject.SetActive(false); selected = key; selectedFrom = selectedTo = null;
            SetDetails(true); FocusNode(key); RefreshInspector(Sim.Wiring);
        }
        /// <summary>Navigate an in-game task request without asking the player to hunt through the whole warehouse.</summary>
        public void FocusTask(string key)
        {
            var node = Sim != null ? Sim.Wiring.Node(key) : null;
            if (node == null || !node.available) return;
            pinned.Add(key); workspaceFilter = 5;
            selected = key; selectedFrom = selectedTo = null;
            CancelWire(); SetDetails(false); positions.Clear(); topologyKey = "";
            Refresh(); fitPending = true;
        }

        public void SetWorkspaceFilter(int filter)
        {
            workspaceFilter = Mathf.Clamp(filter, 0, 5); selectedFrom = selectedTo = null; CancelWire(); topologyKey = "";
            Refresh(); Arrange(); Hint(workspaceFilter == 0 ? T("工作集默认展示 3 项任务，也保留手动加入的节点；其他任务仍在运行。", "The working set shows 3 jobs; other jobs keep running. Add more from Resources.") : T("仅切换视图，不会暂停任何任务。", "This changes the view only; no job is paused."));
        }
        void Arrange() { positions.Clear(); topologyKey = ""; Refresh(); fitPending = true; }
        void Fit()
        {
            if (canvas == null || viewport.rect.width <= 0) return;
            fitPending = false;
            float fit = Mathf.Clamp(Mathf.Min((viewport.rect.width - 24) / canvas.sizeDelta.x, (viewport.rect.height - 15) / Mathf.Min(canvas.sizeDelta.y, 530)), .68f, 1);
            // Never shrink text to microscopic size just to fit a late-game inventory. Pan for lower cards.
            pan.SetView(fit, new Vector2(Mathf.Max(8, (viewport.rect.width - canvas.sizeDelta.x * fit) / 2), -6));
        }
        void FocusNode(string key) { if (views.TryGetValue(key, out var n)) pan.GlideTo(new Vector2(n.position.x + CardWidth * .5f, -n.position.y - CardHeight * .5f)); }
        void SetDetails(bool open) { detailOpen = open; detail.gameObject.SetActive(open); if (open) { detail.SetAsLastSibling(); RefreshInspector(Sim.Wiring); } }
        void Select(string key)
        {
            if (Time.unscaledTime < suppressClickUntil) return;
            selected = key; selectedFrom = selectedTo = null; SetDetails(true); RefreshCards(Sim.Wiring); back.SetVerticesDirty();
        }
        public void Hover(string key, bool on) { if (on) hovered = key; else if (hovered == key) hovered = null; }
        string InputSummary(XgWNode n)
        {
            if (n.lane == XgWireLane.Gpu) return n.source;
            if (n.lane == XgWireLane.Model) return n.run ? string.Join(", ", n.cards) : n.card;
            if (n.lane == XgWireLane.Task) return n.model;
            return "";
        }
        void RefreshInspector(XgWiringView v)
        {
            var n = v.Node(selected);
            if (selectedFrom != null)
            {
                detailTitle.text = T("连接线路", "Connection"); detailStatus.text = T("已选中 · 尚未断开", "Selected · still connected"); detailStatus.color = Tone[2];
                var a = v.Node(selectedFrom); var b = v.Node(selectedTo);
                detailRows.text = Tint(T("输出端", "From"), Mute) + "\n" + Plain(a != null ? a.name : selectedFrom) + "\n\n" + Tint(T("输入端", "To"), Mute) + "\n" + Plain(b != null ? b.name : selectedTo);
                detailNote.text = T("只有点击下面的按钮才会断线。节点和任务进度不会删除。", "Disconnect explicitly below. Nodes and job progress are retained.");
                disconnectButton.Show(true);
                disconnectButton.Set(T("断开这条线路", "Disconnect this cable"), true, new Color32(60, 25, 25, 255), Bad); trainButton.Show(false); detailLinks.text = "";
            }
            else if (n == null)
            {
                detailTitle.text = T("选择一个节点", "Select a node"); detailStatus.text = T("真正的游戏资源，不是额外模拟器", "Real game resources, not another simulation"); detailStatus.color = Mute;
                detailRows.text = T("1. 从仓库加入模型或任务\n2. 接上供电与算力\n3. 模型输出接到任务输入\n\n连接不满足条件时会说明原因。", "1. Add models or tasks\n2. Connect power and compute\n3. Connect a model to a task\n\nInvalid connections explain why.");
                detailNote.text = T("库存、准确率、钱、电费和任务结果沿用游戏原有规则。", "Inventory, accuracy, money, bills and task results use the existing game rules.");
                disconnectButton.Show(false); trainButton.Show(false); detailLinks.text = "";
            }
            else
            {
                detailTitle.text = Plain(n.name); detailStatus.text = Status(n) + " · " + Plain(n.tag); detailStatus.color = n.bad ? Bad : n.warn ? Warn : Good;
                var rows = new StringBuilder();
                void Row(string name, string value) { rows.Append(Tint(name, Mute)).Append("\n").Append(Plain(value)).Append("\n"); }
                if (n.lane == XgWireLane.Power) { Row(T("电力负载 / 上限", "Load / capacity"), F(n.loadW) + " / " + F(n.capW) + " W"); Row(T("供电范围", "Circuit"), n.sub); }
                else if (n.lane == XgWireLane.Gpu) { Row(T("算力占用 / 容量", "Compute used / capacity"), F(n.load) + " / " + F(n.cu) + " CU"); Row(T("显存占用 / 容量", "VRAM used / capacity"), F(n.vramUse / 1024, "0.00") + " / " + F(n.vramMB / 1024, "0.#") + " GB"); Row(T("额定功耗", "Rated power"), F(n.watts) + " W"); }
                else if (n.lane == XgWireLane.Model) { Row(T("数据集 / 架构", "Dataset / architecture"), n.dataset + " / " + n.arch); Row(T("准确率 / 显存需求", "Accuracy / memory"), F(n.acc * 100, "0.0") + "%  /  " + F(n.vramNeed, "0") + " MB"); Row(T("执行速度", "Execution speed"), F(n.speed * 100, "0") + "%"); }
                else { Row(T("要求的数据 / 准确率", "Required data / accuracy"), n.dataset + (n.min > 0 ? " ≥ " + F(n.min * 100) + "%" : "")); Row(T("当前执行速率", "Current rate"), F(n.rate, "0.00") + " " + n.unit + T("/秒", "/s")); Row(T("任务收益", "Task payout"), n.earns ? "¥" + F(n.pay, "0.##") + "/" + n.unit : T("不直接产生收入", "No direct income")); }
                detailRows.text = rows.ToString();
                detailNote.text = Plain(!n.available ? n.lockWhy : !string.IsNullOrEmpty(n.why) ? n.why : n.lane == XgWireLane.Model ? T("拖动输出端口，把能力分配给匹配的任务。多个任务共享同一设备。", "Wire this model to matching tasks. Tasks share their device.") : T("资源状态来自游戏。断开线路不会删除已有成果。", "State comes from the game. Unplugging preserves existing results."));
                if (n.cafe || n.key == "cafe") detailNote.text = T("接通网吧电路即开始租赁：每台 ¥", "Connecting café power starts rent: ¥") + F(Sim.CafeRentPerSecond(Sim.CafeNight), "0.00") + T("/秒（含电费）。闲置也收费，拔掉电源线停止计费。", "/s per PC, power included. Idle PCs still cost money; unplug power to stop rent.");
                if (n.rack || n.key == "idc" || n.key == "pretrain") detailNote.text += T(" 机房预训练运行时另收租金 ¥", " Server-room pre-training additionally costs ¥") + XgSim.DatacenterRent + T("/秒，停止预训练即停止租金。", "/s; stopping pre-training stops this rent.");
                string input = InputSummary(n); disconnectButton.Show(input.Length > 0);
                disconnectButton.Set(T("断开输入线路", "Disconnect input"), input.Length > 0, Soft, Warn);
                bool training = n.key.StartsWith("train:", StringComparison.Ordinal) || n.run;
                trainButton.Show(training);
                if (training) { var r = Sim.Run(n.key.EndsWith(":0", StringComparison.Ordinal) ? XgTrack.Vision : XgTrack.Sequence); trainButton.Set(r.epochActive ? T("本轮训练中", "Epoch in progress") : T("训练一次", "Run one epoch"), !r.epochActive, XgDark.OnFill, Good); }
                var links = new StringBuilder(T("已连接\n", "Connections\n"));
                foreach (var c in cables)
                    if (c.from == n.key || c.to == n.key) { var other = v.Node(c.from == n.key ? c.to : c.from); links.Append(c.from == n.key ? "→ " : "← ").Append(Plain(other != null ? other.name : "")).Append('\n'); }
                detailLinks.text = links.ToString();
            }
            int actions = (disconnectButton.rt.gameObject.activeSelf ? 1 : 0) + (trainButton.rt.gameObject.activeSelf ? 1 : 0);
            detailActions.gameObject.SetActive(actions > 0);
            detailActions.sizeDelta = new Vector2(detailActions.sizeDelta.x, actions * 35 + 10);
            PlaceTopLeft(disconnectButton, 8, 5, 240, 29);
            PlaceTopLeft(trainButton, 8, disconnectButton.rt.gameObject.activeSelf ? 40 : 5, 240, 29);
            ((RectTransform)detailContent.parent).offsetMin = new Vector2(4, actions > 0 ? actions * 35 + 19 : 6);
            var log = Sim.WiringLog; var logLines = new StringBuilder();
            for (int i = log.Count - 1, count = 0; i >= 0 && count < 6; i--, count++) logLines.Append(Tint(((int)log[i].at / 60).ToString("00") + ":" + ((int)log[i].at % 60).ToString("00"), Mute)).Append("  ").Append(Plain(log[i].text)).Append('\n');
            logText.text = logLines.ToString();
        }
        void DisconnectSelected()
        {
            if (selectedFrom != null) { Sim.Disconnect(selectedFrom, selectedTo, Host); selectedFrom = selectedTo = null; }
            else
            {
                var n = Sim.Wiring.Node(selected); if (n == null) return;
                if (n.lane == XgWireLane.Model && n.run) { var cards = new List<string>(n.cards); foreach (string c in cards) Sim.Disconnect(c, n.key, Host); }
                else { string input = InputSummary(n); if (input.Length == 0) return; Sim.Disconnect(input, n.key, Host); }
            }
            topologyKey = ""; Refresh(); Hint(T("线路已断开。", "Cable disconnected."));
        }
        void TrainSelected()
        {
            var n = Sim.Wiring.Node(selected); if (n == null) return;
            var track = n.key.EndsWith(":0", StringComparison.Ordinal) ? XgTrack.Vision : XgTrack.Sequence;
            bool ok = Sim.BeginEpoch(track, Host);
            Hint(ok ? T("已启动一轮训练。", "Started one epoch.") : Sim.Blocker(Sim.Run(track), Host) ?? T("暂时无法开始训练。", "Cannot start training yet.")); Refresh();
        }
        void ResetBreaker()
        { if (Sim.Wiring.tripped) Hint(Sim.ResetBreakerFromWiring(Host) ? T("供电已恢复。", "Power restored.") : T("仍有过载，请先断开部分显卡。", "Still overloaded; unplug a card first.")); else if (Sim.AjiePending) Sim.AnswerAjie("ai"); Refresh(); }
        void Hint(string text) { hint = text; hintUntil = Time.unscaledTime + 7; }

        public void PortClick(string key, bool input)
        {
            if (Time.unscaledTime < suppressClickUntil) return;
            if (armed == null) { Arm(key, input); return; }
            if (armed == key && armedInput == input) { CancelWire(); return; }
            if (armedInput == input) { Arm(key, input); return; }
            ConnectPorts(key, input);
        }
        void Arm(string key, bool input)
        { armed = key; armedInput = input; if (views.TryGetValue(key, out var n)) pointer = End(n, !input); Hint(input ? T("选择上一级的输出端口。Esc 取消。", "Choose an upstream output port.") : T("选择下一级的输入端口。也可以直接拖线。", "Choose a downstream input, or drag a cable.")); front.SetVerticesDirty(); }
        void ConnectPorts(string key, bool input)
        {
            if (armed == null || armedInput == input) return;
            string from = input ? armed : key, to = input ? key : armed;
            string why = Sim.Connect(from, to, Host);
            if (why != null) { Hint(why); host.ShowToast(why, 4); return; }
            pinned.Add(to); pinned.Add(from); selected = to; selectedFrom = selectedTo = null; CancelWire(); topologyKey = "";
            Refresh(); Hint(T("已接通：", "Connected: ") + NodeName(Sim.Wiring.Node(from)) + " → " + NodeName(Sim.Wiring.Node(to)));
        }
        public void CancelWire() { armed = null; draggingPort = false; if (front != null) front.SetVerticesDirty(); }
        Vector2 Local(PointerEventData e)
        { RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, e.position, e.pressEventCamera != null ? e.pressEventCamera : e.enterEventCamera, out var p); return p; }
        public void BeginPortDrag(string key, bool input, PointerEventData e) { if (e.button != PointerEventData.InputButton.Left) return; Arm(key, input); draggingPort = true; pointer = Local(e); }
        public void DragPort(PointerEventData e) { pointer = Local(e); front.SetVerticesDirty(); }
        public void DropPort(string key, bool input) { if (draggingPort) ConnectPorts(key, input); }
        public void EndPortDrag() { CancelWire(); draggingPort = false; suppressClickUntil = Time.unscaledTime + .15f; }
        public void BeginNodeDrag(string key, PointerEventData e)
        {
            if (!views.TryGetValue(key, out var node)) return;
            draggingNode = true; dragNodeKey = key; dragStart = Local(e); nodeStart = node.position; selected = key; selectedFrom = selectedTo = null;
        }
        public void DragNode(PointerEventData e)
        {
            if (!draggingNode || !views.TryGetValue(dragNodeKey, out var node)) return;
            Vector2 delta = Local(e) - dragStart;
            node.position = new Vector2(Mathf.Clamp(nodeStart.x + delta.x, 6, 4000), Mathf.Clamp(nodeStart.y - delta.y, 35, 4000));
            positions[node.key] = node.position; Position(node); UpdateWorldBounds(); back.SetVerticesDirty(); front.SetVerticesDirty();
        }
        public void EndNodeDrag() { draggingNode = false; dragNodeKey = null; suppressClickUntil = Time.unscaledTime + .15f; }
        public void BeginPan(PointerEventData e) { pan.OnBeginDrag(e); }
        public void DragPan(PointerEventData e) { pan.OnDrag(e); }
        public void EndPan(PointerEventData e) { pan.OnEndDrag(e); suppressClickUntil = Time.unscaledTime + .15f; }
        public void CanvasScroll(PointerEventData e) { pan.OnScroll(e); }
        public void CanvasClick(PointerEventData e)
        {
            if (Time.unscaledTime < suppressClickUntil) return;
            var p = Local(e); Cable nearest = null; float best = 9 / Mathf.Max(.65f, QaZoom);
            foreach (var c in cables)
            {
                if (!Ends(c, out var a, out var b)) continue;
                var prev = a;
                for (int i = 1; i <= 28; i++) { var q = At(a, b, i / 28f); float distance = Distance(p, prev, q); if (distance < best) { best = distance; nearest = c; } prev = q; }
            }
            if (nearest != null) { selectedFrom = nearest.from; selectedTo = nearest.to; selected = null; SetDetails(true); back.SetVerticesDirty(); return; }
            CancelWire();
        }
        static float Distance(Vector2 p, Vector2 a, Vector2 b) { var v = b - a; return Vector2.Distance(p, a + v * (v.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, v) / v.sqrMagnitude))); }
        static Vector2 End(NodeView n, bool output) => new Vector2(n.position.x + (output ? CardWidth : 0), -n.position.y - CardHeight * .5f);
        bool Ends(Cable c, out Vector2 a, out Vector2 b)
        { a = b = Vector2.zero; if (!views.TryGetValue(c.from, out var from) || !views.TryGetValue(c.to, out var to)) return false; a = End(from, true); b = End(to, false); return from.visible && to.visible; }
        static Vector2 At(Vector2 a, Vector2 b, float t) { float dx = Mathf.Max(35, Mathf.Abs(b.x - a.x) * .48f); return XgSoftDraw.Bezier(a, a + Vector2.right * dx, b - Vector2.right * dx, b, t); }
        bool FocusCable(Cable c) => selectedFrom != null ? selectedFrom == c.from && selectedTo == c.to : (hovered ?? selected) == null || c.from == (hovered ?? selected) || c.to == (hovered ?? selected);
        public void DrawBack(VertexHelper vh)
        {
            Vector2 size = canvas.rect.size;
            float gridStep = Mathf.Max(24, Mathf.Sqrt(size.x * size.y / 1500f));
            for (float x = 8; x < size.x; x += gridStep)
                for (float y = 5; y < size.y; y += gridStep) XgSoftDraw.Disc(vh, new Vector2(x, -y), .65f, 0, new Color32(30, 52, 64, 180), 4);
            foreach (var c in cables)
            {
                if (!Ends(c, out var a, out var b)) continue;
                bool focused = FocusCable(c); var color = Alpha(Tone[(int)c.lane], focused ? c.live ? .9f : .44f : .18f);
                Vector2 prev = a;
                for (int i = 1; i <= 28; i++) { Vector2 next = At(a, b, i / 28f); if (c.live || i % 2 == 0) XgSoftDraw.Seg(vh, prev, next, selectedFrom == c.from && selectedTo == c.to ? 3 : 1.8f, .6f, color); prev = next; }
            }
        }
        public void DrawFront(VertexHelper vh)
        {
            foreach (var n in order)
            {
                if (n.inPort != null) DrawPort(vh, End(n, false), Tone[Mathf.Max(0, (int)n.lane - 1)], armed == n.key && armedInput);
                if (n.outPort != null) DrawPort(vh, End(n, true), Tone[(int)n.lane], armed == n.key && !armedInput);
            }
            if (armed != null && views.TryGetValue(armed, out var armedNode))
            {
                Vector2 fixedEnd = End(armedNode, !armedInput), a = armedInput ? pointer : fixedEnd, b = armedInput ? fixedEnd : pointer, prev = a;
                for (int i = 1; i <= 24; i++) { var q = At(a, b, i / 24f); if (i % 2 == 0) XgSoftDraw.Seg(vh, prev, q, 2, .5f, Tone[(int)armedNode.lane]); prev = q; }
            }
            // Flow dots denote throughput, not invented success/failure events.
            foreach (var c in cables) if (c.live && Ends(c, out var a, out var b)) XgSoftDraw.Disc(vh, At(a, b, (animationClock * .28f) % 1), 2.7f, 1, Tone[(int)c.lane], 10);
            foreach (var pulse in pulses)
            {
                var cable = cables.Find(c => c.to == pulse.task); if (cable == null || !Ends(cable, out var a, out var b)) continue;
                XgSoftDraw.Disc(vh, At(a, b, pulse.progress), 4, 1, pulse.correct ? Good : Bad, 12);
            }
        }
        static void DrawPort(VertexHelper vh, Vector2 p, Color tone, bool armed)
        { XgSoftDraw.Disc(vh, p, 7, 1, Card, 16); XgSoftDraw.Ring(vh, p, 5.7f, 1.7f, tone, 20); XgSoftDraw.Disc(vh, p, armed ? 3.6f : 2, .5f, tone, 10); }
        void OnRouted(XgAutoRecord r) { if (r != null) OnItem("label:" + r.dataset, r.correct); }
        void OnEpoch(XgEpoch e) { if (e != null) OnItem("train:" + e.track, !e.diverged); }
        void OnItem(string task, bool correct)
        {
            completed.TryGetValue(task, out int count); completed[task] = count + 1;
            if (root == null || !root.gameObject.activeInHierarchy || pulses.Count >= 32) return;
            pulses.Add(new Pulse { task = task, correct = correct });
        }
        public bool QaPoint(string from, string to, bool port, out Vector3 world)
        {
            world = Vector3.zero;
            if (port) { if (!views.TryGetValue(from, out var n) || !n.visible) return false; world = canvas.TransformPoint(End(n, to != "in")); return true; }
            var c = cables.Find(e => e.from == from && e.to == to); if (c == null || !Ends(c, out var a, out var b)) return false;
            world = canvas.TransformPoint(At(a, b, .5f)); return true;
        }
    }
}
