using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// One square of the 科技 tree (Upgrade Tree PRO's UpgradeButton, rebuilt for 灵光): click to buy the next level,
    /// hover for the tooltip, drag to pan. Purchases go through <see cref="XgTechTree.Buy"/>, which is the simulation's
    /// own <see cref="XgSim.BuyNode"/>.
    /// </summary>
    public sealed class XgTechNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public XgTechEntry entry;
        public XgTechTreePage page;
        /// <summary>The square (fill, icon and frame): what the buy shake moves. The name below stays still.</summary>
        public RectTransform square;
        public Image fill, frame, ring;
        public XgTechIcon icon;
        public TMP_Text glyph, badge, label, cost;
        public Vector2 home;
        bool dragged;

        public bool Covers(string id) => entry != null && entry.Covers(id);
        public void OnPointerEnter(PointerEventData e) => page.Hover(this, true);
        public void OnPointerExit(PointerEventData e) => page.Hover(this, false);
        public void OnPointerDown(PointerEventData e) { dragged = false; }
        public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left && !dragged) page.Click(this); }
        public void OnBeginDrag(PointerEventData e) { dragged = true; page.Pan.OnBeginDrag(e); }
        public void OnDrag(PointerEventData e) => page.Pan.OnDrag(e);
        public void OnEndDrag(PointerEventData e) => page.Pan.OnEndDrag(e);
        void OnDisable() { if (page != null) page.Hover(this, false); }
    }

    /// <summary>
    /// The 科技 page in the Upgrade Tree PRO style: square icon nodes whose frame shows the state (green affordable,
    /// red too expensive, grey locked, gold maxed), connector lines, a hover tooltip with name, description, Lv. n/m and
    /// price, a DOTween shake and a sound on buy, and a pannable view. The squares are generated from
    /// <see cref="XgTechTree"/> (the catalog, with ladders folded into levels); the simulation owns visibility, prices
    /// and purchases. Only the package's art and tweens are used, never its managers.
    /// </summary>
    public sealed class XgTechTreePage : XgPage
    {
        /// <summary>Square size, slot pitch and row pitch of the generated layout, in content pixels.</summary>
        public const float SquareSize = 56, SlotWidth = 128, RowHeight = 118, LaneHeader = 34, LaneGap = 12, TopMargin = 64;
        public const int Columns = 5;
        public const float StageWidth = Columns * SlotWidth + 40;
        const float TooltipWidth = 312;

        RectTransform viewport, content, bandLayer, linkLayer, nodeLayer, miniMap, frontierWall, eraCard, tooltip;
        TMP_Text header, tipName, tipLevel, tipBody, tipCost, tipGlyph, eraTitle, eraBody;
        Image tipCostBar;
        XgTechIcon tipIcon;
        readonly List<XgTechNodeView> views = new List<XgTechNodeView>();
        readonly Dictionary<XgTechEntry, XgTechNodeView> viewOf = new Dictionary<XgTechEntry, XgTechNodeView>();
        readonly Dictionary<XgTechEntry, Vector2> position = new Dictionary<XgTechEntry, Vector2>();
        readonly Dictionary<string, float> bandTop = new Dictionary<string, float>(), bandHeight = new Dictionary<string, float>();
        readonly Dictionary<int, TMP_Text> stageTitles = new Dictionary<int, TMP_Text>();
        readonly Dictionary<int, XgBtn> miniButtons = new Dictionary<int, XgBtn>();
        readonly List<RectTransform> laneTitles = new List<RectTransform>();
        readonly Dictionary<string, double> purchaseCosts = new Dictionary<string, double>();
        readonly HashSet<XgTechNodeView> shown = new HashSet<XgTechNodeView>();
        readonly List<XgBtn> toolbar = new List<XgBtn>();
        readonly List<Link> links = new List<Link>();
        XgTechNodeView hovered, pinned;
        XgSim displayedSim;
        XgTechTreeSkin skin;
        float treeHeight, eraRemaining;
        int displayedStage;
        bool built;

        sealed class Link { public XgTechNodeView from, to; public Image image; public bool crossBand, crossStage, need; }

        public XgPan Pan { get; private set; }
        public int VisibleStageCount => Sim == null ? 0 : Sim.S.stage;
        /// <summary>Catalog nodes on squares that are shown now (a ladder counts all its steps).</summary>
        public int VisibleNodeCount { get { int count = 0; foreach (var v in views) if (v.gameObject.activeSelf) count += v.entry.members.Count; return count; } }
        public bool IsNodeShown(string id) => views.Exists(v => v.Covers(id) && v.gameObject.activeSelf);

        static readonly string[] Bands = { "trunk", "vision", "sequence", "research", "auto", "atlas" };

        static Color LaneColor(string lane) => lane == "vision" ? XgDark.Accent : lane == "sequence" ? new Color32(18, 135, 125, 255)
            : lane == "label" ? new Color32(200, 120, 30, 255) : lane == "trunk" ? new Color32(110, 90, 200, 255) : lane == "atlas" ? new Color32(20, 140, 160, 255)
            : lane == "auto" ? new Color32(70, 130, 200, 255) : new Color32(150, 100, 230, 255);
        /// <summary>The four state colours of the package, in the dark palette.</summary>
        static readonly Color Affordable = XgDark.Good, Unaffordable = XgDark.Bad, LockedGrey = new Color32(74, 92, 106, 255), Maxed = XgDark.Gold;

        // ───────────── build ─────────────

        public override void Build(RectTransform area)
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            skin = XgTechTreeSkin.Load();
            root = ui.Card(area, "tree", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // 科技 has its own nav row (养成), so the page needs no way back to 道具.
            header = ui.Text(Strip("Header", root, 8, 30, 16, 520), "", 18, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            viewport = Rect("Viewport", root, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -78));
            viewport.gameObject.AddComponent<CanvasRenderer>();
            var grid = viewport.gameObject.AddComponent<XgGridGraphic>(); grid.color = XgDark.Page;
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", viewport, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0, 1);
            Panel(content, Color.clear).raycastTarget = true;
            Pan = viewport.gameObject.AddComponent<XgPan>(); Pan.content = content; Pan.viewport = viewport;
            Pan.minZoom = .6f; Pan.maxZoom = 1.5f;
            bandLayer = Layer("Bands"); linkLayer = Layer("Links"); nodeLayer = Layer("Nodes");
            Layout();
            content.sizeDelta = new Vector2(StageWidth * 2, treeHeight);
            int band = 0;
            foreach (var lane in Bands)
            {
                if (!bandTop.ContainsKey(lane)) continue;
                var stripe = Rect("LaneBand " + lane, bandLayer, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -bandTop[lane] - bandHeight[lane] - LaneGap * .5f), new Vector2(0, -bandTop[lane] + LaneGap * .5f));
                Panel(stripe, band++ % 2 == 0 ? new Color32(11, 19, 28, 200) : new Color32(0, 0, 0, 0)).raycastTarget = false;
            }
            for (int i = 0; i < XgCatalog.StageNames.Length; i++) BuildStageHeader(i);
            foreach (var e in XgTechTree.Entries) MakeNode(e);
            foreach (var v in views) MakeLinks(v);
            frontierWall = TopLeft("UnexploredBoundary", content, 0, 50, 8, treeHeight - 50);
            frontierWall.pivot = new Vector2(.5f, 1);
            Panel(frontierWall, new Color32(58, 85, 102, 180)).raycastTarget = false;
            BuildToolbar();
            BuildLegend();
            BuildTooltip();
            BuildEraCard();
            built = true;
            Sync(false);
        }

        RectTransform Layer(string name)
        {
            var rt = Rect(name, content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rt.pivot = new Vector2(0, 1);
            return rt;
        }

        /// <summary>
        /// Places every square once (positions never move, so links are built once). One column per stage, the lanes as
        /// bands; inside a stage and band a square sits right of its parent when it can, otherwise in the first free slot.
        /// </summary>
        void Layout()
        {
            var taken = new HashSet<string>();
            var slot = new Dictionary<XgTechEntry, Vector2Int>();
            var rows = new Dictionary<string, int>();
            foreach (var e in XgTechTree.Entries)
            {
                string bandId = XgCatalog.Band(e.Lane), key = e.Stage + ":" + bandId;
                var parent = XgTechTree.EntryOf(e.First.parent);
                int r = -1, c = -1;
                if (parent != null && slot.TryGetValue(parent, out var ps) && parent.Stage == e.Stage && XgCatalog.Band(parent.Lane) == bandId
                    && ps.x + 1 < Columns && !taken.Contains(key + ":" + ps.y + ":" + (ps.x + 1)))
                { r = ps.y; c = ps.x + 1; }
                for (int row = 0; r < 0; row++)
                    for (int col = 0; col < Columns && r < 0; col++)
                        if (!taken.Contains(key + ":" + row + ":" + col)) { r = row; c = col; }
                taken.Add(key + ":" + r + ":" + c);
                slot[e] = new Vector2Int(c, r);
                rows.TryGetValue(bandId, out int used); rows[bandId] = Math.Max(used, r + 1);
            }
            float top = TopMargin;
            foreach (var lane in Bands)
            {
                if (!rows.TryGetValue(lane, out int count)) continue;
                bandTop[lane] = top; bandHeight[lane] = LaneHeader + count * RowHeight;
                top += bandHeight[lane] + LaneGap;
            }
            treeHeight = top + 20;
            foreach (var kv in slot)
            {
                string bandId = XgCatalog.Band(kv.Key.Lane);
                float x = kv.Key.Stage * StageWidth + 24 + kv.Value.x * SlotWidth + SlotWidth * .5f;
                float y = bandTop[bandId] + LaneHeader + kv.Value.y * RowHeight + SquareSize * .5f + 6;
                position[kv.Key] = new Vector2(x, -y);
            }
        }

        void BuildStageHeader(int i)
        {
            var title = ui.Text(TopLeft("StageHeader" + i, content, i * StageWidth + 24, 12, StageWidth - 40, 36), "", 21, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold; stageTitles.Add(i, title);
            foreach (var lane in Bands)
            {
                if (!bandTop.ContainsKey(lane)) continue;
                bool any = false;
                foreach (var e in XgTechTree.Entries) if (e.Stage == i && XgCatalog.Band(e.Lane) == lane) { any = true; break; }
                if (!any) continue;
                var label = ui.Text(TopLeft("Lane " + lane + " " + i, content, i * StageWidth + 28, bandTop[lane] + 4, 300, 26), "", 16, LaneColor(lane == "trunk" && i == 0 ? "label" : lane), TextAlignmentOptions.MidlineLeft);
                label.fontStyle = FontStyles.Bold;
                label.rectTransform.SetSiblingIndex(nodeLayer.GetSiblingIndex());
                laneTitles.Add(label.rectTransform);
            }
        }

        static string LaneTitle(string lane, int stage)
        {
            switch (lane)
            {
                case "trunk": return stage == 0 ? T("标注技能", "Labelling skills") : T("主干", "Core");
                case "vision": return T("视觉", "Vision");
                case "sequence": return T("序列", "Sequence");
                case "research": return T("研究", "Research");
                case "auto": return T("自动化", "Automation");
                default: return T("图鉴 · 现象与能力", "Atlas · phenomena and abilities");
            }
        }

        void MakeNode(XgTechEntry e)
        {
            var at = position[e];
            var rt = Rect("Node " + e.id, nodeLayer, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            rt.sizeDelta = new Vector2(SquareSize, SquareSize);
            rt.anchoredPosition = at;
            var view = rt.gameObject.AddComponent<XgTechNodeView>();
            view.entry = e; view.page = this; view.home = at;
            // The UpgradeButton hierarchy: bg (fill) holding the icon and the border; the name sits under the square.
            view.square = Rect("Square", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view.fill = view.square.gameObject.AddComponent<Image>();
            view.fill.sprite = skin.fill; view.fill.raycastTarget = true;
            var iconRt = Rect("Icon", view.square, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
            iconRt.gameObject.AddComponent<CanvasRenderer>();
            view.icon = iconRt.gameObject.AddComponent<XgTechIcon>(); view.icon.raycastTarget = false;
            view.glyph = ui.Text(Rect("Glyph", view.square, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5)), "", 17, XgDark.Ink, TextAlignmentOptions.Center);
            view.glyph.fontStyle = FontStyles.Bold; view.glyph.textWrappingMode = TextWrappingModes.NoWrap;
            view.glyph.enableAutoSizing = true; view.glyph.fontSizeMin = 9; view.glyph.fontSizeMax = 18;
            var frameRt = Rect("Border", view.square, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view.frame = frameRt.gameObject.AddComponent<Image>(); view.frame.raycastTarget = false;
            if (skin.frame != null) view.frame.sprite = skin.frame;
            else { view.frame.enabled = false; var rim = view.square.gameObject.AddComponent<Outline>(); rim.effectDistance = new Vector2(3, -3); }
            var ringRt = Rect("HoverRing", view.square, Vector2.zero, Vector2.one, new Vector2(-6, -6), new Vector2(6, 6));
            view.ring = ringRt.gameObject.AddComponent<Image>(); view.ring.raycastTarget = false;
            view.ring.sprite = skin.ring != null ? skin.ring : skin.frame; view.ring.color = Color.clear;
            if (view.ring.sprite == null) view.ring.enabled = false;
            var badgeRt = Rect("Level", view.square, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, -7), new Vector2(7, 11));
            Panel(badgeRt, new Color32(7, 12, 18, 235)).raycastTarget = false;
            view.badge = ui.Text(Rect("Text", badgeRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 12, XgDark.Ink, TextAlignmentOptions.Center);
            view.badge.fontStyle = FontStyles.Bold; view.badge.textWrappingMode = TextWrappingModes.NoWrap;
            // Names are what the player reads: two lines at most under the square, shrinking only for long English names.
            view.label = ui.Text(Rect("Name", rt, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-SlotWidth * .5f + 3, -42), new Vector2(SlotWidth * .5f - 3, -4)), "", 15, XgDark.Ink, TextAlignmentOptions.Top);
            view.label.fontStyle = FontStyles.Bold; view.label.lineSpacing = -12;
            view.label.enableAutoSizing = true; view.label.fontSizeMin = 12; view.label.fontSizeMax = 15;
            view.cost = ui.Text(Rect("Cost", rt, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-SlotWidth * .5f, -60), new Vector2(SlotWidth * .5f, -42)), "", 14, XgDark.Money, TextAlignmentOptions.Center);
            view.cost.fontStyle = FontStyles.Bold; view.cost.textWrappingMode = TextWrappingModes.NoWrap;
            rt.gameObject.SetActive(false);
            views.Add(view); viewOf[e] = view;
        }

        void MakeLinks(XgTechNodeView child)
        {
            var e = child.entry;
            AddLink(XgTechTree.EntryOf(e.First.parent), child, false);
            foreach (var need in e.First.needs) AddLink(XgTechTree.EntryOf(need), child, true);
        }

        void AddLink(XgTechEntry parent, XgTechNodeView child, bool need)
        {
            if (parent == null || parent == child.entry || !viewOf.TryGetValue(parent, out var from)) return;
            // Package style: a straight 3-pixel line from centre to centre, under the squares.
            Vector2 a = position[parent], b = position[child.entry], d = b - a;
            var rt = Rect("Link " + parent.id + ">" + child.entry.id, linkLayer, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            rt.pivot = new Vector2(0, .5f);
            rt.sizeDelta = new Vector2(d.magnitude, need ? 2 : 3);
            rt.anchoredPosition = a;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var image = rt.gameObject.AddComponent<Image>(); image.sprite = skin.line; image.raycastTarget = false;
            rt.gameObject.SetActive(false);
            links.Add(new Link { from = from, to = child, image = image, need = need, crossBand = XgCatalog.Band(parent.Lane) != XgCatalog.Band(child.entry.Lane), crossStage = parent.Stage != child.entry.Stage });
        }

        void BuildToolbar()
        {
            miniMap = Strip("DiscoveredStages", root, 48, 28, 16, 310);
            miniMap.gameObject.SetActive(false);
            string[] labels = { "⓪ 标注", "前沿", "可买", "−", "+" };
            string[] labelsEn = { "⓪ Labels", "Frontier", "Buyable", "−", "+" };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                toolbar.Add(ui.Button(root, T(labels[i], labelsEn[i]), () => Toolbar(index), 15));
            }
            float right = 12;
            for (int i = toolbar.Count - 1; i >= 0; i--)
            {
                float width = i >= 3 ? 34 : i == 0 ? 92 : 80;
                PlaceTopRight(toolbar[i], right + width, 8, width, 30); right += width + 4;
            }
            UiTip.Add(toolbar[0].rt, "跳到标注技能（加薪、自动答题）。", "Jump to the labelling skills (raises, auto labelling).");
            UiTip.Add(toolbar[1].rt, "跳到当前阶段的最前沿。", "Jump to the current stage's frontier.");
            UiTip.Add(toolbar[2].rt, "依次跳到现在买得起的节点。", "Cycle through the nodes you can afford now.");
            UiTip.Add(toolbar[3].rt, "缩小。", "Zoom out.");
            UiTip.Add(toolbar[4].rt, "放大。", "Zoom in.");
        }

        /// <summary>The four frame colours, bottom left, so the states read without the tooltip.</summary>
        void BuildLegend()
        {
            // On the second header row, right of the stage buttons, so it never covers a square.
            var box = Rect("Legend", root, Vector2.one, Vector2.one, new Vector2(-12 - 470, -44 - 30), new Vector2(-12, -44));
            box.pivot = Vector2.one;
            Panel(box, new Color32(7, 12, 18, 220)).raycastTarget = false;
            var row = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 10, 4, 4); row.spacing = 6; row.childAlignment = TextAnchor.MiddleRight;
            row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = row.childForceExpandHeight = false;
            void Item(Color c, string zh, string en)
            {
                var sw = Rect("Swatch", box, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                var img = sw.gameObject.AddComponent<Image>(); img.sprite = skin.frame; img.color = c; img.raycastTarget = false;
                var le = sw.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = le.minWidth = 16; le.preferredHeight = 16;
                var t = ui.Text(Rect("Label", box, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), "", 14, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.gameObject.AddComponent<XgTechText>().Set(zh, en);
                t.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;
            }
            Item(Affordable, "可买", "Affordable");
            Item(Unaffordable, "钱不够", "Too expensive");
            Item(LockedGrey, "未解锁", "Locked");
            Item(Maxed, "满级", "Maxed");
            var fitter = box.gameObject.AddComponent<ContentSizeFitter>(); fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>The package's Toolltip prefab, rebuilt: header (name and icon), Lv. n/m, a line, the description, the price bar.</summary>
        void BuildTooltip()
        {
            tooltip = Rect("Tooltip", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            tooltip.pivot = new Vector2(.5f, 0); tooltip.sizeDelta = new Vector2(TooltipWidth, 0);
            var bg = Panel(tooltip, new Color32(5, 9, 14, 250)); bg.sprite = skin.fill; bg.raycastTarget = false;
            var rim = tooltip.gameObject.AddComponent<Outline>(); rim.effectColor = XgDark.Line; rim.effectDistance = new Vector2(1, -1);
            var column = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(12, 12, 10, 10); column.spacing = 6;
            column.childControlWidth = column.childControlHeight = true; column.childForceExpandWidth = true; column.childForceExpandHeight = false;
            tooltip.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var head = Rect("Header", tooltip, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var headRow = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            headRow.spacing = 8; headRow.childAlignment = TextAnchor.MiddleLeft;
            headRow.childControlWidth = headRow.childControlHeight = true; headRow.childForceExpandWidth = headRow.childForceExpandHeight = false;
            tipName = ui.Text(Rect("Name", head, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), "", 19, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            tipName.fontStyle = FontStyles.Bold;
            tipName.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var iconBg = Rect("IconBg", head, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var iconFill = Panel(iconBg, XgDark.Card); iconFill.sprite = skin.fill; iconFill.raycastTarget = false;
            var iconLe = iconBg.gameObject.AddComponent<LayoutElement>(); iconLe.preferredWidth = iconLe.minWidth = iconLe.preferredHeight = iconLe.minHeight = 36;
            var iconRt = Rect("Icon", iconBg, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));
            iconRt.gameObject.AddComponent<CanvasRenderer>();
            tipIcon = iconRt.gameObject.AddComponent<XgTechIcon>(); tipIcon.raycastTarget = false;
            tipGlyph = ui.Text(Rect("Glyph", iconBg, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2)), "", 12, XgDark.Ink, TextAlignmentOptions.Center);
            tipGlyph.fontStyle = FontStyles.Bold; tipGlyph.textWrappingMode = TextWrappingModes.NoWrap;
            tipGlyph.enableAutoSizing = true; tipGlyph.fontSizeMin = 8; tipGlyph.fontSizeMax = 13;

            tipLevel = ui.Text(Rect("Level", tooltip, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), "", 15, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            Divider();
            tipBody = ui.Text(Rect("Description", tooltip, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), "", 15, XgDark.Ink, TextAlignmentOptions.TopLeft);
            tipBody.lineSpacing = -6;
            Divider();
            var bar = Rect("CostBG", tooltip, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            tipCostBar = Panel(bar, XgDark.Bad); tipCostBar.sprite = skin.fill; tipCostBar.raycastTarget = false;
            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
            tipCost = ui.Text(Rect("Cost", bar, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0)), "", 16, Color.white, TextAlignmentOptions.Center);
            tipCost.fontStyle = FontStyles.Bold; tipCost.textWrappingMode = TextWrappingModes.NoWrap;
            tooltip.gameObject.SetActive(false);

            void Divider()
            {
                var line = Rect("Line", tooltip, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                var img = Panel(line, new Color(1, 1, 1, .31f)); img.sprite = skin.line; img.raycastTarget = false;
                line.gameObject.AddComponent<LayoutElement>().preferredHeight = 2;
            }
        }

        void BuildEraCard()
        {
            eraCard = Rect("EraCard", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-240, -92), new Vector2(240, 92));
            Panel(eraCard, XgDark.Hud);
            eraTitle = ui.Text(Strip("EraTitle", eraCard, 16, 36, 20, 20), "", 24, XgDark.Gold, TextAlignmentOptions.Center);
            eraBody = ui.Text(Strip("EraDescription", eraCard, 60, 76, 24, 24), "", 16, Color.white, TextAlignmentOptions.Center);
            var dismiss = ui.Button(eraCard, Lang.T("继续"), () => { eraRemaining = 0; eraCard.gameObject.SetActive(false); }, 14);
            PlaceTopRight(dismiss, 105, 142, 85, 28);
            eraCard.gameObject.SetActive(false);
        }

        // ───────────── reveal ─────────────

        /// <summary>Shows the stages and squares the simulation has revealed; new squares pop in after the first sync.</summary>
        void Sync(bool animate)
        {
            bool rebound = displayedSim != Sim;
            if (rebound) { displayedSim = Sim; displayedStage = Sim.S.stage; hovered = pinned = null; shown.Clear(); HideTooltip(); }
            int stage = Sim.S.stage;
            foreach (var pair in stageTitles)
            {
                int i = pair.Key;
                bool visible = i <= stage;
                pair.Value.gameObject.SetActive(visible);
                if (visible) pair.Value.text = (i == 0 ? "⓪ " : i + " · ") + T(XgCatalog.StageNames[i], XgCatalog.StageNamesEn[i]) + (i > 0 ? "  <size=14><color=#6F95A5>" + XgCatalog.StageYears[i] + "</color></size>" : "");
                if (visible && !miniButtons.ContainsKey(i))
                {
                    int column = i;
                    var button = ui.Button(miniMap, "", () => FocusStage(column), 13);
                    button.rt.anchorMin = button.rt.anchorMax = new Vector2(0, .5f); button.rt.pivot = new Vector2(0, .5f);
                    button.rt.anchoredPosition = new Vector2(i * 56, 0); button.rt.sizeDelta = new Vector2(50, 25);
                    miniButtons.Add(i, button);
                }
                if (miniButtons.TryGetValue(i, out var mini))
                {
                    mini.rt.gameObject.SetActive(visible);
                    if (visible) mini.Set(i == 0 ? Lang.T("标注") : i.ToString(), true, i == stage ? XgDark.AccentSoft : XgDark.Button);
                }
            }
            foreach (var title in laneTitles)
            {
                // "Lane <band> <stage>"
                var parts = title.name.Split(' ');
                int i = int.Parse(parts[2]);
                title.gameObject.SetActive(i <= stage);
                if (i <= stage) title.GetComponent<TMP_Text>().text = LaneTitle(parts[1], i);
            }
            foreach (var view in views)
            {
                bool visible = XgTechTree.Visible(view.entry, Sim);
                if (view.gameObject.activeSelf != visible) view.gameObject.SetActive(visible);
                if (!visible) { shown.Remove(view); if (pinned == view) pinned = null; continue; }
                if (shown.Add(view) && animate && !rebound && !Fx.Reduced)
                {
                    var rt = (RectTransform)view.transform;
                    rt.DOKill(); rt.localScale = Vector3.one * .4f;
                    rt.DOScale(1, .35f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(view.gameObject);
                }
            }
            content.sizeDelta = new Vector2((stage + 1) * StageWidth + 40, treeHeight);
            frontierWall.gameObject.SetActive(stage < XgCatalog.StageNames.Length - 1);
            frontierWall.anchoredPosition = new Vector2((stage + 1) * StageWidth - 6, -50);
            miniMap.gameObject.SetActive(stage > 1); toolbar[1].rt.gameObject.SetActive(stage > 1);
            if (animate && !rebound && stage > displayedStage) FocusFrontier();
            displayedStage = stage; Pan.Clamp();
            tooltip.SetAsLastSibling(); eraCard.SetAsLastSibling();
        }

        // ───────────── paint ─────────────

        public override void Refresh()
        {
            if (!built) return;
            Sync(true);
            int buyable = 0;
            foreach (var v in views)
            {
                if (!v.gameObject.activeSelf) continue;
                if (XgTechTree.Status(v.entry, Sim, Host) == XgSim.NodeStatus.Buyable) buyable++;
                Paint(v);
            }
            header.text = "<b>" + Lang.T("科技") + "</b>  <color=#FAC775>¥ " + Money(Host.Money) + "</color>";
            toolbar[0].Set(T("⓪ 标注", "⓪ Labels"), true); toolbar[1].Set(T("前沿", "Frontier"), true);
            toolbar[2].Set(T("可买 ", "Buyable ") + buyable, true, buyable > 0 ? XgDark.AccentSoft : XgDark.Button);
            PaintLinks();
            if (hovered != null && tooltip.gameObject.activeSelf) FillTooltip(hovered);
        }

        string InsightOf(XgNode n)
        {
            if (n == null || n.kind != XgNodeKind.Secret) return null;
            return n.id == "secret.6" && Sim.S.insights.Contains("pretrain") ? "pretrain" : null;
        }

        void Paint(XgTechNodeView v)
        {
            var e = v.entry; var node = e.First;
            var status = XgTechTree.Status(e, Sim, Host);
            int level = XgTechTree.Level(e, Sim), max = XgTechTree.MaxLevel(e);
            bool atlas = XgSim.IsAtlas(node), mystery = Sim.NodeMystery(node), insight = InsightOf(node) != null;
            bool lit = status == XgSim.NodeStatus.Owned;
            Color state = insight || lit ? Maxed : status == XgSim.NodeStatus.Buyable ? Affordable : status == XgSim.NodeStatus.TooExpensive ? Unaffordable : LockedGrey;
            Color lane = LaneColor(node.lane);
            bool locked = status == XgSim.NodeStatus.Locked && !insight;
            v.frame.color = state;
            if (!v.frame.enabled) { var rim = v.square.GetComponent<Outline>(); if (rim != null) rim.effectColor = state; }
            v.fill.color = locked ? XgDark.Disabled : lit ? Color.Lerp(XgDark.Card, Maxed, .16f) : status == XgSim.NodeStatus.Buyable ? Color.Lerp(XgDark.Card, Affordable, .14f) : XgDark.Card;
            v.ring.color = v == hovered || v == pinned ? new Color(1, 1, 1, .85f) : Color.clear;
            // The picture: architectures by their short name, everything else by a shape of its kind.
            string glyph = Glyph(e);
            var kind = atlas && !lit ? XgTechIcon.Kind.None : IconOf(e);
            if (mystery) { glyph = "?"; kind = XgTechIcon.Kind.None; }
            else if (atlas && !lit) glyph = "?";
            Color ink = locked || atlas && !lit ? new Color32(96, 118, 132, 255) : lit && atlas ? Maxed : Color.Lerp(lane, Color.white, .35f);
            v.icon.color = ink; v.icon.Set(kind, locked);
            v.glyph.text = kind == XgTechIcon.Kind.None ? glyph : "";
            v.glyph.color = ink;
            v.badge.transform.parent.gameObject.SetActive(max > 1 && !mystery);
            if (max > 1) v.badge.text = level + "/" + max;
            v.badge.color = level >= max ? Maxed : XgDark.Ink;
            v.label.text = atlas && !lit && node.kind != XgNodeKind.Ability ? Lang.T("？？？") : XgTechTree.Name(e, Sim);
            v.label.color = locked ? XgDark.Muted : XgDark.Ink;
            double cost = XgTechTree.Cost(e, Sim);
            var next = XgTechTree.Next(e, Sim);
            if (atlas) v.cost.text = lit ? "<color=#5DCAA5>✓</color>" : "";
            else if (lit) v.cost.text = max > 1 ? "<color=#FFBE28>MAX</color>" : "<color=#5DCAA5>✓</color>";
            else if (mystery || double.IsInfinity(cost) || next == null || !Sim.NodeVisible(next)) v.cost.text = "";
            else v.cost.text = Hex(status == XgSim.NodeStatus.Buyable ? XgDark.Money : status == XgSim.NodeStatus.TooExpensive ? Unaffordable : XgDark.Dim) + (cost > 0 ? "¥" + Money(cost) : T("免费", "Free")) + "</color>";
        }

        void PaintLinks()
        {
            foreach (var link in links)
            {
                bool ends = link.from.gameObject.activeSelf && link.to.gameObject.activeSelf;
                // Links inside a band are always drawn; a prerequisite in another band shows while either end is under the pointer.
                bool focus = hovered != null && (hovered == link.from || hovered == link.to) || pinned != null && (pinned == link.from || pinned == link.to);
                bool on = ends && (!link.crossBand && !link.need || focus);
                if (link.image.gameObject.activeSelf != on) link.image.gameObject.SetActive(on);
                if (!on) continue;
                bool owned = XgTechTree.Level(link.to.entry, Sim) > 0;
                // Long links into a later stage run under other squares: drawn fainter so the names over them stay clear.
                float alpha = link.crossStage ? .4f : 1;
                link.image.color = focus ? XgDark.Gold : owned ? new Color(1, 1, 1, .72f * alpha) : new Color(.23f, .33f, .4f, .86f * alpha);
            }
        }

        static string Glyph(XgTechEntry e)
        {
            var n = e.First;
            switch (n.id)
            {
                case "perceptron": return "P";
                case "mlp": return "MLP";
                case "lenet": return "LeNet";
                case "alexnet": return "Alex";
                case "vgg": return "VGG";
                case "googlenet": return "Incep";
                case "resnet": return "Res";
                case "rnn": return "RNN";
                case "lstm": return "LSTM";
                case "gru": return "GRU";
                case "seq2seq": return "S2S";
                case "attention": return "Attn";
                case "textcnn": return "1D";
                case "caption": return "Cap";
                case "transformer": return "TF";
                case "label.raise": return "¥";
                case "label.coop": return "?";
                case "label.hard": return "×1.5";
                case "label.parallel": return "×4";
                case "label.captcha": return "Aa";
            }
            return n.nameEn != null && n.nameEn.Length <= 4 ? n.nameEn : "";
        }

        static XgTechIcon.Kind IconOf(XgTechEntry e)
        {
            var n = e.First;
            if (n.kind == XgNodeKind.Arch || Glyph(e) != "" && n.tree == "label") return XgTechIcon.Kind.None;
            switch (n.id)
            {
                case "label.auto": return XgTechIcon.Kind.Gear;
                case "label.brain": return XgTechIcon.Kind.Network;
                case "label.audit": return XgTechIcon.Kind.Check;
                case "datacenter": return XgTechIcon.Kind.Data;
            }
            switch (n.kind)
            {
                case XgNodeKind.Depth: return XgTechIcon.Kind.Layers;
                case XgNodeKind.Width: return XgTechIcon.Kind.Channels;
                case XgNodeKind.LrKnob: return XgTechIcon.Kind.Knob;
                case XgNodeKind.Dataset: return XgTechIcon.Kind.Data;
                case XgNodeKind.Auto: return XgTechIcon.Kind.Gear;
                case XgNodeKind.Secret: return XgTechIcon.Kind.Key;
                case XgNodeKind.Phenomenon: return XgTechIcon.Kind.Eye;
                case XgNodeKind.Ability: return XgTechIcon.Kind.Star;
                case XgNodeKind.Label: return n.tree == "label" ? XgTechIcon.Kind.Eye : XgTechIcon.Kind.Spark;
                default: return XgTechIcon.Kind.Spark;
            }
        }

        // ───────────── interaction ─────────────

        public void Hover(XgTechNodeView v, bool on)
        {
            if (!built || v == null) return;
            if (on)
            {
                if (hovered == v || !v.gameObject.activeInHierarchy) return;
                hovered = v;
                Fx.Play(XgJuice.Sfx.Id.Tick, 1.5f, .25f);
                if (!Fx.Reduced) { v.square.DOKill(); v.square.DOScale(1.08f, .12f).SetUpdate(true).SetLink(v.gameObject); }
                ShowTooltip(v);
            }
            else
            {
                if (hovered != v) return;
                hovered = null;
                v.square.DOKill(); v.square.localScale = Vector3.one; v.square.localRotation = Quaternion.identity;
                HideTooltip();
            }
            PaintLinks();
            foreach (var view in views) if (view.gameObject.activeSelf) view.ring.color = view == hovered || view == pinned ? new Color(1, 1, 1, .85f) : Color.clear;
        }

        public void Click(XgTechNodeView v)
        {
            var e = v.entry;
            string card = InsightOf(e.First);
            if (card != null && XgTreePage.OpenInsightCard != null) { XgTreePage.OpenInsightCard(card); return; }
            var status = XgTechTree.Status(e, Sim, Host);
            if (status == XgSim.NodeStatus.Buyable)
            {
                var next = XgTechTree.Next(e, Sim);
                purchaseCosts[next.id] = Sim.NodeCost(next);
                if (XgTechTree.Buy(e, Sim, Host)) { if (view != null) view.Refresh(true); return; }
                purchaseCosts.Remove(next.id);
            }
            // Not for sale now: a sideways shake and a thud (a click on a maxed square only clicks).
            if (!Fx.Reduced)
            {
                v.square.DOKill(true); v.square.localScale = Vector3.one;
                v.square.DOShakeAnchorPos(.25f, new Vector2(7, 0), 24, 0, false, true).SetUpdate(true).SetLink(v.gameObject);
            }
            Fx.Play(status == XgSim.NodeStatus.Owned ? XgJuice.Sfx.Id.Click : XgJuice.Sfx.Id.Thud, 1.2f, .6f);
        }

        /// <summary>Buys this exact catalog node for another page (道具); the tree plays its own effects in <see cref="OnBought"/>.</summary>
        public bool Buy(XgNode node, RectTransform from)
        {
            if (node == null) return false;
            purchaseCosts[node.id] = Sim.NodeCost(node);
            if (!Sim.BuyNode(node.id, Host)) { purchaseCosts.Remove(node.id); Fx.Play(XgJuice.Sfx.Id.Thud); return false; }
            Fx.Knock(from, .2f); Fx.Shockwave(Fx.At(from), XgDark.Gold, 160, .35f, 8); Fx.Burst(Fx.At(from), 24, XgDark.Gold, XgJuice.Shape.Yen, 300);
            return true;
        }

        /// <summary>Any purchase (here, in 道具 or by AutoML): the package's buy shake on the square, the lab's coins and sound.</summary>
        public void OnBought(XgNode node)
        {
            double paid = purchaseCosts.TryGetValue(node.id, out var actual) ? actual : Sim.NodeCost(node);
            purchaseCosts.Remove(node.id);
            if (!built) return;
            Sync(true);
            Fx.Play(XgJuice.Sfx.Id.Unlock);
            var entry = XgTechTree.EntryOf(node.id);
            if (entry != null && viewOf.TryGetValue(entry, out var v) && v.gameObject.activeInHierarchy && root.gameObject.activeInHierarchy)
            {
                if (!Fx.Reduced)
                {
                    v.square.DOKill(true); v.square.localScale = Vector3.one;
                    v.square.DOShakeScale(.3f, .2f).SetUpdate(true).SetLink(v.gameObject);
                }
                Fx.Play(XgJuice.Sfx.Id.Coin, 1.1f, .7f);
                var at = Fx.At(v.square);
                Fx.Shockwave(at, LaneColor(node.lane), 160, .35f, 8);
                Fx.Burst(at, 18, XgDark.Gold, XgJuice.Shape.Yen, 280);
                if (paid > 0 && !double.IsInfinity(paid)) Fx.Float(at + new Vector2(0, 50), "-¥" + Money(paid), XgDark.Money, 22);
                Fx.HitStop(50);
                if (IsEraItem(node.id))
                {
                    Fx.Flash(Color.white, .08f, .3f);
                    for (int y = -120; y <= 120; y += 60) Fx.Crumble(at + new Vector2(0, y), Fx.Reduced ? 2 : 8, XgDark.Muted);
                    OnBreakthrough(node.id);
                }
            }
            Refresh();
        }

        /// <summary>The architecture items that turn the tree to their year.</summary>
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
            if (!built) return;
            Sync(true);
            eraTitle.text = year + " · " + Lang.T("时代卡"); eraBody.text = T(zh, en);
            eraCard.gameObject.SetActive(true); eraCard.SetAsLastSibling(); eraRemaining = 8;
            FocusFrontier();
            if (id == "transformer") { Fx.Flash(Color.white, .2f, .8f); Fx.HitStop(240); Fx.Shockwave(Fx.At(eraCard), XgDark.Gold, 500, .8f, 16); }
        }

        // ───────────── tooltip ─────────────

        void ShowTooltip(XgTechNodeView v)
        {
            FillTooltip(v);
            bool wasOpen = tooltip.gameObject.activeSelf;
            tooltip.gameObject.SetActive(true); tooltip.SetAsLastSibling(); eraCard.SetAsLastSibling();
            PlaceTooltip(v);
            // The package's UpgradeTooltip.OnEnable shake, kept small on the z axis so the text stays legible.
            tooltip.DOKill(); tooltip.localRotation = Quaternion.identity; tooltip.localScale = Vector3.one;
            if (!Fx.Reduced && !wasOpen)
            {
                tooltip.DOShakeRotation(.3f, new Vector3(0, 0, 6), 20, 90, true).SetUpdate(true).SetLink(tooltip.gameObject);
                tooltip.DOShakeScale(.2f, .12f, 5).SetUpdate(true).SetLink(tooltip.gameObject);
            }
        }

        void HideTooltip()
        {
            if (tooltip == null) return;
            tooltip.DOKill(); tooltip.localRotation = Quaternion.identity; tooltip.localScale = Vector3.one;
            tooltip.gameObject.SetActive(false);
        }

        /// <summary>Above the square, or below it when there is no room; always inside the page.</summary>
        void PlaceTooltip(XgTechNodeView v)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);
            var bounds = root.rect;
            float h = tooltip.rect.height, w = tooltip.rect.width;
            Vector2 top = root.InverseTransformPoint(v.square.TransformPoint(new Vector3(0, SquareSize * .5f, 0)));
            Vector2 bottom = root.InverseTransformPoint(v.transform.TransformPoint(new Vector3(0, -SquareSize * .5f - 62, 0)));
            bool above = top.y + 10 + h <= bounds.yMax - 4;
            tooltip.pivot = new Vector2(.5f, above ? 0 : 1);
            float y = above ? top.y + 10 : Mathf.Max(bottom.y - 4, bounds.yMin + h + 4);
            float x = Mathf.Clamp(top.x, bounds.xMin + w * .5f + 4, bounds.xMax - w * .5f - 4);
            tooltip.anchoredPosition = new Vector2(x, y);
        }

        void FillTooltip(XgTechNodeView v)
        {
            var e = v.entry; var node = e.First;
            var status = XgTechTree.Status(e, Sim, Host);
            int level = XgTechTree.Level(e, Sim), max = XgTechTree.MaxLevel(e);
            var next = XgTechTree.Next(e, Sim);
            double cost = XgTechTree.Cost(e, Sim);
            bool atlas = XgSim.IsAtlas(node), lit = status == XgSim.NodeStatus.Owned;
            tipIcon.color = v.icon.color; tipIcon.Set(IconOfShown(v), false);
            tipGlyph.text = v.glyph.text; tipGlyph.color = v.glyph.color;
            var body = new StringBuilder();
            if (atlas)
            {
                bool ability = node.kind == XgNodeKind.Ability;
                tipName.text = lit || ability ? Sim.NodeName(node) : Lang.T("？？？");
                tipLevel.text = lit ? Hex(Maxed) + T("已点亮", "Lit") + "</color>" : T("未发现", "Not seen yet");
                body.Append(lit || ability ? T(node.note, node.noteEn) + "\n\n<color=#6F95A5>" + Sim.Why(node, Host) + "</color>"
                    : Lang.T("还没发生过。规则叠在一起，它自己会冒出来；第一次发生时会记进图鉴。"));
                SetCost(lit ? Maxed : LockedGrey, lit ? "✓ " + T("已点亮", "Lit") : T("不能购买 · 它自己会发生", "Not for sale · it happens on its own"));
                tipBody.text = body.ToString();
                return;
            }
            if (Sim.NodeMystery(node))
            {
                // 自动答题 before the protagonist has the idea: no name, no price, only a hint (XgSim.Epiphany.cs).
                tipName.text = Sim.NodeName(node); tipLevel.text = T("未发现", "Not discovered");
                tipBody.text = Sim.NodeNote(node);
                SetCost(LockedGrey, "？");
                return;
            }
            tipName.text = XgTechTree.Name(e, Sim);
            string word = lit ? Hex(Maxed) + (max > 1 ? T("已满级", "Maxed") : T("已拥有", "Owned"))
                : status == XgSim.NodeStatus.Buyable ? Hex(Affordable) + T("可以买", "Affordable")
                : status == XgSim.NodeStatus.TooExpensive ? Hex(Unaffordable) + T("钱不够", "Too expensive") : Hex(LockedGrey) + T("未解锁", "Locked");
            tipLevel.text = "Lv. " + level + " / " + max + "    " + word + "</color>";
            var shownNode = next ?? e.members[e.members.Count - 1];
            body.Append(Sim.NodeNote(shownNode));
            if (node.id == "label.raise")
            {
                int r = Sim.RaiseLevel;
                body.Append("\n<color=#FAC775>").Append(T("人工标注报酬 ×", "Hand pay ×")).Append(N(XgCatalog.RaiseMultipliers[r], "0"));
                if (r < XgCatalog.RaiseMax) body.Append(" → ×").Append(N(XgCatalog.RaiseMultipliers[r + 1], "0"));
                body.Append("  · ").Append(XgCatalog.RaiseTitle(r, Sim.English)).Append("</color>");
            }
            if (e.IsChain) body.Append("\n").Append(Ladder(e));
            if (next != null && status != XgSim.NodeStatus.Owned)
            {
                body.Append("\n");
                void Condition(bool ok, string text) => body.Append("\n").Append(ok ? "<color=#5DCAA5>✓ " : "<color=#E07B4F>○ ").Append(text).Append("</color>");
                if (next.parent != null && next.tree != "label")
                    Condition(Sim.Has(next.parent), Sim.NodeName(XgCatalog.Node(next.parent)));
                foreach (string need in next.needs) Condition(Sim.Has(need), Sim.NodeName(XgCatalog.Node(need)));
                if (next.id == "caption") Condition(Sim.S.stage >= 5, T("第五阶段", "Stage five"));
                if (next.id == "transformer") Condition(Sim.TransformerIdeaReached, T("它自己问出「如果只用注意力呢？」", "It asked: what if only attention?"));
                if (!double.IsInfinity(cost)) Condition(Host.Money + 1e-9 >= cost, T("经费 ¥", "Funds ¥") + Money(cost));
                if (status == XgSim.NodeStatus.Locked) body.Append("\n<color=#6F95A5>").Append(Sim.Why(next, Host)).Append("</color>");
            }
            foreach (var d in XgSim.DiscountsOf(shownNode.id))
            {
                string ability = XgSim.AbilityName(d.ability, Sim.English);
                body.Append("\n<color=#C88A00>").Append(T("道具 · 能力「" + ability + "」", "Item · ability '" + ability + "'"));
                if (d.paramsFactor < 1) body.Append(T(" 参数门槛 ×", " parameters ×")).Append(N(d.paramsFactor, "0.0#"));
                if (d.samplesFactor < 1) body.Append(T(" 数据门槛 ×", " data ×")).Append(N(d.samplesFactor, "0.0#"));
                body.Append("</color>");
            }
            tipBody.text = body.ToString();
            if (lit) SetCost(Maxed, max > 1 ? T("已满级 · MAX", "MAX") : "✓ " + T("已拥有", "Owned"));
            else if (double.IsInfinity(cost)) SetCost(LockedGrey, T("未解锁", "Locked"));
            else if (status == XgSim.NodeStatus.Buyable) SetCost(Color.Lerp(Affordable, Color.black, .35f), "¥ " + Money(cost) + "  ·  " + T("点击购买", "click to buy"));
            else if (status == XgSim.NodeStatus.TooExpensive) SetCost(Color.Lerp(Unaffordable, Color.black, .3f), "¥ " + Money(cost) + "  ·  " + T("还差 ¥", "short ¥") + Money(cost - Host.Money));
            else SetCost(Color.Lerp(LockedGrey, Color.black, .3f), "¥ " + Money(cost) + "  ·  " + T("未解锁", "locked"));
        }

        XgTechIcon.Kind IconOfShown(XgTechNodeView v)
        {
            var node = v.entry.First;
            if (Sim.NodeMystery(node) || XgSim.IsAtlas(node) && XgTechTree.Status(v.entry, Sim, Host) != XgSim.NodeStatus.Owned) return XgTechIcon.Kind.None;
            return IconOf(v.entry);
        }

        void SetCost(Color bar, string text) { tipCostBar.color = bar; tipCost.text = text; }

        /// <summary>A ladder's steps in one line (2 · 3 · 4 层): owned green, the next one gold, later ones grey with their stage.</summary>
        string Ladder(XgTechEntry e)
        {
            var sb = new StringBuilder();
            bool numeric = e.First.kind == XgNodeKind.Depth || e.First.kind == XgNodeKind.Width;
            var next = XgTechTree.Next(e, Sim);
            sb.Append(T("各级：", "Levels: "));
            for (int i = 0; i < e.members.Count; i++)
            {
                var m = e.members[i];
                string text = numeric ? (m.kind == XgNodeKind.Width ? XgCatalog.Widths[Mathf.Clamp(m.value, 0, XgCatalog.Widths.Length - 1)].ToString() : m.value.ToString()) : Sim.NodeName(m);
                if (!Sim.NodeVisible(m)) text += T("（阶段 ", " (stage ") + m.stage + T("）", ")");
                string color = Sim.Has(m.id) ? "#5DCAA5" : m == next ? "#FFBE28" : "#6F95A5";
                if (i > 0) sb.Append(numeric ? " · " : "\n");
                sb.Append("<color=").Append(color).Append('>').Append(m == next ? "<b>" + text + "</b>" : text).Append("</color>");
            }
            if (e.First.kind == XgNodeKind.Depth) sb.Append(T(" 层", " layers"));
            return sb.ToString();
        }

        // ───────────── navigation ─────────────

        void Toolbar(int index)
        {
            Fx.Play(XgJuice.Sfx.Id.Click);
            if (index == 0) { FocusStage(0); return; }
            if (index == 1) { FocusFrontier(); return; }
            if (index == 3) { Pan.ZoomBy(1 / 1.25f); return; }
            if (index == 4) { Pan.ZoomBy(1.25f); return; }
            var buyable = views.FindAll(v => v.gameObject.activeSelf && XgTechTree.Status(v.entry, Sim, Host) == XgSim.NodeStatus.Buyable);
            if (buyable.Count == 0) { if (view != null) view.ShowToast(Lang.T("现在没有买得起的节点"), 2); return; }
            int at = pinned != null ? buyable.IndexOf(pinned) : -1;
            var target = buyable[(at + 1) % buyable.Count];
            Pin(target); Focus(target);
            if (!Fx.Reduced) target.square.DOPunchScale(Vector3.one * .15f, .3f, 6).SetUpdate(true).SetLink(target.gameObject);
        }

        void Pin(XgTechNodeView v)
        {
            pinned = v;
            foreach (var view in views) if (view.gameObject.activeSelf) view.ring.color = view == hovered || view == pinned ? new Color(1, 1, 1, .85f) : Color.clear;
            PaintLinks();
        }

        public void FocusFrontier() => FocusStage(Sim.S.stage);

        void FocusStage(int stage)
        {
            stage = Math.Max(0, Math.Min(Sim.S.stage, stage));
            Canvas.ForceUpdateCanvases();
            float centre = stage * StageWidth + StageWidth * .5f;
            Pan.FitColumn(centre);
            // Lanes this stage leaves empty (the labelling band after stage 0) would fill the view: glide down to the
            // first lane that has squares in this stage, keeping its title in view.
            float top = float.MinValue;
            foreach (var v in views) if (v.gameObject.activeSelf && v.entry.Stage == stage) top = Mathf.Max(top, v.home.y);
            if (top == float.MinValue) return;
            float zoom = content.localScale.x, height = viewport.rect.height, width = viewport.rect.width;
            float lift = SquareSize * .5f + LaneHeader + 18;
            if (-(top + lift) * zoom < height * .35f) return;
            Pan.GlideTo(new Vector2(centre + width * .05f / zoom, top + lift + (60 - height * .42f) / zoom));
        }

        public RectTransform NodeTarget(string id)
        {
            var v = views.Find(n => n.Covers(id) && n.gameObject.activeSelf);
            return v != null ? (RectTransform)v.transform : null;
        }

        public bool FocusNode(string id)
        {
            if (!built) return false;
            Sync(false);
            var v = views.Find(n => n.Covers(id) && n.gameObject.activeSelf);
            if (v == null) return false;
            Pin(v); Focus(v); return true;
        }

        void Focus(XgTechNodeView v) => Pan.GlideTo(v.home);

        public override void Shown() { if (!built) return; Sync(false); FocusFrontier(); }

        public override void Tick(float dt)
        {
            if (!built) return;
            if (eraRemaining > 0) { eraRemaining -= dt; if (eraRemaining <= 0) eraCard.gameObject.SetActive(false); }
            // The tooltip follows its square while the view pans or zooms.
            if (hovered != null && tooltip.gameObject.activeSelf)
            {
                if (!hovered.gameObject.activeInHierarchy) { hovered = null; HideTooltip(); }
                else PlaceTooltip(hovered);
            }
        }
    }

    /// <summary>A label that keeps its Chinese and English text and shows the one for the current language.</summary>
    public sealed class XgTechText : MonoBehaviour
    {
        string zh, en;
        TMP_Text text;
        bool english;
        public void Set(string chinese, string english)
        {
            zh = chinese; en = english; text = GetComponent<TMP_Text>();
            this.english = GameText.IsEnglish; text.text = GameText.T(zh, en);
        }
        void Update() { if (text != null && GameText.IsEnglish != english) Set(zh, en); }
    }
}
