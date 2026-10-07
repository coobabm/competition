using System.Collections.Generic;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Saved models assembled into LingGuang's active capabilities, not network-parameter cards.</summary>
    public sealed partial class XgBoardPage
    {
        // The dark scanner palette of the redesign: ink is light, the core and the drag ghost are dark teal.
        static readonly Color AssemblyInk = XgDark.Ink;
        static readonly Color AssemblyCore = new Color32(16, 52, 50, 255);
        static readonly Color AssemblyMuted = XgDark.Muted;
        static readonly Color AssemblyTeal = new Color32(45, 170, 160, 255);
        static readonly Color AssemblyViolet = new Color32(150, 125, 230, 255);
        static readonly Color AssemblyAmber = new Color32(220, 160, 60, 255);
        static readonly Color AssemblyPaper = XgDark.Card;
        static readonly Color AssemblyLine = XgDark.Line;
        const float BrainCardWidth = 184, BrainCardHeight = 112;

        sealed class BrainModelCard
        {
            public int id;
            public XgBtn button, remove;
            public TMP_Text category, version, accuracy, state;
            public Outline outline;
        }

        RectTransform assemblyView, assemblyShelfContent, assemblyBrainViewport, assemblyCanvas, assemblyCore, assemblyGhost;
        XgPan assemblyPan;
        XgBrainAssemblyGraphic assemblyGraphic;
        TMP_Text assemblySummary, assemblyCapabilities, assemblyStatus, assemblyCount, assemblyEmptyLibrary, assemblyGhostName;
        XgBtn assemblyRemove;
        readonly XgBtn[] assemblyFilters = new XgBtn[3];
        readonly RectTransform[] assemblyMissing = new RectTransform[3];
        readonly Dictionary<int, BrainModelCard> assemblyLibrary = new Dictionary<int, BrainModelCard>();
        readonly Dictionary<int, BrainModelCard> assemblyInstalled = new Dictionary<int, BrainModelCard>();
        readonly Dictionary<string, Vector2> assemblyPositions = new Dictionary<string, Vector2>();
        readonly List<XgModelEntry> assemblyModels = new List<XgModelEntry>();
        string assemblyLibraryKey = "!", assemblyMembershipKey = "!", assemblyMessage = "";
        int assemblySelectedId, assemblyDraggingId, assemblyFilter;
        bool assemblyDraggingInstalled;
        float assemblyTimer;

        public RectTransform QaAssemblyRoot => assemblyView;
        public RectTransform QaBrainDropRect => assemblyBrainViewport;
        public int QaInstalledCount => assemblyInstalled.Count;
        public int QaSelectedModelId => assemblySelectedId;
        public RectTransform QaRemoveRect => assemblyRemove?.rt;
        public RectTransform QaModelCardRect(int modelId) => assemblyLibrary.TryGetValue(modelId, out var card) ? card.button.rt : null;
        public RectTransform QaInstalledCardRect(int modelId) => assemblyInstalled.TryGetValue(modelId, out var card) ? card.button.rt : null;

        static Image AssemblyRound(RectTransform rt, Color color, bool raycast = false)
        { var image = XgCardArt.Sliced(rt, color, 2); image.raycastTarget = raycast; return image; }

        XgBtn AssemblyButton(Transform parent, string name, string text, UnityEngine.Events.UnityAction clicked, float size = 13)
        {
            var b = ui.Button(parent, text, clicked, size); b.rt.name = name;
            b.image.sprite = XgCardArt.Rounded(); b.image.type = Image.Type.Sliced; b.image.pixelsPerUnitMultiplier = 2;
            b.label.overflowMode = TextOverflowModes.Ellipsis;
            return b;
        }

        TMP_Text AssemblyText(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        { var t = ui.Text(rt, text, size, color, align); t.overflowMode = TextOverflowModes.Ellipsis; return t; }

        void BuildAssembly(RectTransform card)
        {
            assemblyView = Rect("Assembly", card, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -56));
            AssemblyRound(assemblyView, AssemblyPaper);
            var lifecycle = assemblyView.gameObject.AddComponent<XgBrainCardInput>();
            lifecycle.EscapeCancels = true; lifecycle.Cancel = CancelAssemblyDrag;
            var shelf = Rect("ModelLibrary", assemblyView, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(236, 0));
            AssemblyRound(shelf, XgDark.Panel);
            AssemblyText(Strip("Title", shelf, 10, 32, 14, 14), T("模型卡片", "MODEL CARDS"), 19, AssemblyInk).fontStyle = FontStyles.Bold;
            AssemblyText(Strip("Hint", shelf, 46, 30, 14, 14), T("拖进大脑，或点击接入", "Drag into brain, or click to add"), 12, AssemblyMuted);
            string[] filters = { T("全部", "All"), T("未接入", "Available"), T("已接入", "Installed") };
            for (int i = 0; i < 3; i++)
            {
                int filter = i;
                var b = AssemblyButton(shelf, "Filter" + i, filters[i], () => { CancelAssemblyDrag(); assemblyFilter = filter; RefreshAssembly(); }, 12);
                b.rt.anchorMin = new Vector2(i / 3f, 1); b.rt.anchorMax = new Vector2((i + 1) / 3f, 1);
                b.rt.offsetMin = new Vector2(i == 0 ? 10 : 3, -113); b.rt.offsetMax = new Vector2(i == 2 ? -10 : -3, -81);
                assemblyFilters[i] = b;
            }
            var scrollRoot = Rect("ModelScroll", shelf, Vector2.zero, Vector2.one, new Vector2(8, 40), new Vector2(-8, -124));
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", scrollRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(viewport, Color.white); var mask = viewport.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
            assemblyShelfContent = Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            assemblyShelfContent.pivot = new Vector2(.5f, 1);
            var layout = assemblyShelfContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(2, 2, 2, 5); layout.spacing = 9;
            layout.childControlHeight = layout.childControlWidth = layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fit = assemblyShelfContent.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = assemblyShelfContent; scroll.viewport = viewport; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            assemblyEmptyLibrary = AssemblyText(Rect("EmptyLibrary", viewport, Vector2.zero, Vector2.one, new Vector2(14, 16), new Vector2(-14, -16)), "", 14, AssemblyMuted, TextAlignmentOptions.Center);
            assemblyCount = AssemblyText(Rect("Count", shelf, Vector2.zero, new Vector2(1, 0), new Vector2(13, 7), new Vector2(-13, 33)), "", 12, AssemblyMuted);

            var workspace = Rect("BrainComposition", assemblyView, Vector2.zero, Vector2.one, new Vector2(251, 0), Vector2.zero);
            assemblySummary = AssemblyText(Strip("Summary", workspace, 0, 37, 4, 98), "", 21, AssemblyInk); assemblySummary.fontStyle = FontStyles.Bold;
            assemblyCapabilities = AssemblyText(Strip("Capabilities", workspace, 39, 28, 4, 4), "", 12, AssemblyMuted);
            var arrange = AssemblyButton(workspace, "ArrangeModels", T("归位", "Arrange"), ArrangeAssemblyModels, 12);
            PlaceTopRight(arrange, 87, 5, 82, 30);
            assemblyBrainViewport = Rect("BrainDropArea", workspace, Vector2.zero, Vector2.one, new Vector2(0, 84), new Vector2(0, -75));
            AssemblyRound(assemblyBrainViewport, new Color32(6, 11, 16, 255), true); assemblyBrainViewport.gameObject.AddComponent<RectMask2D>();
            assemblyCanvas = Rect("BrainCanvas", assemblyBrainViewport, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            assemblyCanvas.pivot = new Vector2(0, 1); assemblyCanvas.sizeDelta = new Vector2(798, 404);
            assemblyPan = assemblyBrainViewport.gameObject.AddComponent<XgPan>();
            assemblyPan.content = assemblyCanvas; assemblyPan.viewport = assemblyBrainViewport; assemblyPan.minZoom = .85f; assemblyPan.maxZoom = 1.2f;
            var outline = Rect("BrainOutlineAndLinks", assemblyCanvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            assemblyGraphic = outline.gameObject.AddComponent<XgBrainAssemblyGraphic>(); assemblyGraphic.raycastTarget = false;
            assemblyCore = TopLeft("LingGuangCore", assemblyCanvas, 309, 150, 180, 104);
            AssemblyRound(assemblyCore, AssemblyCore);
            AssemblyText(Strip("Identity", assemblyCore, 7, 40, 10, 10), T("灵光", "LingGuang"), 24, Color.white, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
            AssemblyText(Strip("IdentityContinuity", assemblyCore, 52, 45, 10, 10), T("共同记忆 · 语气习惯\n在对话与经历中延续", "Shared memory · Personal voice\nCarried through conversations"), 12, new Color32(201, 222, 226, 255), TextAlignmentOptions.Center);
            assemblyGraphic.Core = assemblyCore;
            string[] missing = { T("缺一个看图模型", "Add a vision model"), T("缺一个读句子模型", "Add a reading model"), T("缺一个判断模型", "Add a reasoning model") };
            for (int i = 0; i < 3; i++)
            {
                var ghost = TopLeft("MissingRegion" + i, assemblyCanvas, 0, 0, BrainCardWidth, BrainCardHeight);
                AssemblyRound(ghost, Color.Lerp(XgDark.Card, AssemblyRegionColor(i), .1f));
                var border = ghost.gameObject.AddComponent<Outline>(); border.effectColor = Color.Lerp(XgDark.Line, AssemblyRegionColor(i), .5f); border.effectDistance = new Vector2(1, -1);
                AssemblyText(Strip("Region", ghost, 9, 22, 12, 12), AssemblyRegionLabel(i), 12, AssemblyRegionColor(i));
                AssemblyText(Strip("Missing", ghost, 37, 34, 12, 12), missing[i], 16, AssemblyMuted, TextAlignmentOptions.Center);
                AssemblyText(Strip("Hint", ghost, 80, 23, 12, 12), T("训练保存后，拖到这里", "Train, save, then add it"), 12, AssemblyMuted, TextAlignmentOptions.Center);
                assemblyMissing[i] = ghost;
            }
            AssemblyText(Rect("PanHint", workspace, Vector2.zero, new Vector2(1, 0), new Vector2(4, 59), new Vector2(-4, 82)),
                T("连线表示能力接入，不是推理流程。拖空白平移；拖卡片整理位置。", "Links show membership, not inference. Drag the background to pan; drag cards to arrange."), 12, AssemblyMuted);
            assemblyStatus = AssemblyText(Rect("Status", workspace, Vector2.zero, new Vector2(1, 0), new Vector2(4, 1), new Vector2(-304, 56)), "", 12, AssemblyMuted);
            assemblyRemove = AssemblyButton(workspace, "RemoveModel", T("卸下模型", "Remove"), RemoveSelectedAssemblyModel, 12);
            PlaceAssemblyBottomRight(assemblyRemove, 293, 13, 92, 34);
            var repo = AssemblyButton(workspace, "OpenRepository", T("模型仓库", "Repository"), () => host.ShowTab("repo"), 12);
            PlaceAssemblyBottomRight(repo, 193, 13, 92, 34);
            var train = AssemblyButton(workspace, "TrainNewModel", T("训练模型", "Train model"), () => host.ShowTab("train"), 12);
            PlaceAssemblyBottomRight(train, 93, 13, 88, 34);
            assemblyGhost = Rect("ModelDragGhost", assemblyView, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-96, -39), new Vector2(96, 39));
            AssemblyRound(assemblyGhost, AssemblyCore);
            var cg = assemblyGhost.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = cg.interactable = false; cg.alpha = .94f;
            assemblyGhostName = AssemblyText(Strip("Name", assemblyGhost, 8, 34, 12, 12), "", 17, Color.white);
            AssemblyText(Strip("Release", assemblyGhost, 48, 24, 12, 12), T("松开拼入大脑 · Esc 取消", "Release to add · Esc cancels"), 12, new Color32(201, 222, 226, 255));
            assemblyGhost.gameObject.SetActive(false);
        }

        static void PlaceAssemblyBottomRight(XgBtn b, float right, float y, float width, float height)
        { b.rt.anchorMin = b.rt.anchorMax = new Vector2(1, 0); b.rt.offsetMin = new Vector2(-right, y); b.rt.offsetMax = new Vector2(-right + width, y + height); }
        static int AssemblyRegion(string dataset)
        { string r = XgSim.RegionOf(dataset); return r == "vision" ? 0 : r == "sequence" ? 1 : 2; }
        static Color AssemblyRegionColor(int region) => region == 0 ? AssemblyTeal : region == 1 ? AssemblyViolet : AssemblyAmber;
        static string AssemblyRegionLabel(int region) => region == 0 ? T("看图能力", "VISION") : region == 1 ? T("读句子能力", "READING") : T("判断与推理", "REASONING");
        static string AssemblyModelName(XgModelEntry model)
        { var data = XgCatalog.Dataset(model.dataset); return data != null ? T(data.name, data.nameEn) : model.dataset; }
        static string AssemblyArchName(XgModelEntry model)
        { var arch = XgCatalog.Arch(model.arch); return arch != null ? T(arch.name, arch.nameEn) : model.arch; }

        BrainModelCard BuildAssemblyModelCard(XgModelEntry model, bool installed)
        {
            int id = model.id;
            var b = AssemblyButton(installed ? assemblyCanvas : assemblyShelfContent, "Model_" + id, "", null, 17);
            if (installed) { b.rt.anchorMin = b.rt.anchorMax = new Vector2(0, 1); b.rt.sizeDelta = new Vector2(BrainCardWidth, BrainCardHeight); }
            else { var le = b.rt.gameObject.AddComponent<LayoutElement>(); le.minHeight = le.preferredHeight = 130; }
            b.label.alignment = TextAlignmentOptions.MidlineLeft; b.label.fontStyle = FontStyles.Bold;
            b.label.rectTransform.anchorMin = new Vector2(0, 1); b.label.rectTransform.anchorMax = Vector2.one;
            b.label.rectTransform.offsetMin = new Vector2(12, -62); b.label.rectTransform.offsetMax = new Vector2(-10, -29);
            var category = AssemblyText(Strip("Capability", b.rt, 5, 23, 12, installed ? 30 : 12), "", 12, AssemblyTeal);
            var version = AssemblyText(Strip("Version", b.rt, 64, 21, 12, 9), "", 12, AssemblyMuted);
            var accuracy = AssemblyText(Strip("Validation", b.rt, 87, 21, 12, 9), "", 12, AssemblyMuted);
            var state = installed ? null : AssemblyText(Strip("InstallState", b.rt, 110, 20, 12, 9), "", 12, AssemblyTeal);
            var border = b.rt.gameObject.AddComponent<Outline>(); border.effectColor = AssemblyLine; border.effectDistance = new Vector2(1, -1);
            XgBtn remove = null;
            if (installed)
            {
                remove = AssemblyButton(b.rt, "Remove", "×", () => RemoveAssemblyModel(id), 14); PlaceTopRight(remove, 28, 2, 24, 26);
                remove.label.rectTransform.offsetMin = new Vector2(1, 0); remove.label.rectTransform.offsetMax = new Vector2(-1, 0);
                UiTip.Add(remove.rt, () => T("卸下此能力；保存的模型不会删除。", "Remove this capability; the saved model is kept."));
            }
            var input = b.rt.gameObject.AddComponent<XgBrainCardInput>();
            input.Click = () => { if (installed) SelectAssemblyModel(id); else InstallAssemblyModel(id); };
            input.BeginDrag = e => BeginAssemblyDrag(id, installed, e); input.Drag = MoveAssemblyDrag; input.EndDrag = EndAssemblyDrag; input.Cancel = CancelAssemblyDrag;
            UiTip.Add(b.rt, () => AssemblyModelTip(id));
            return new BrainModelCard { id = id, button = b, category = category, version = version, accuracy = accuracy, state = state, outline = border, remove = remove };
        }

        void TickAssembly(float dt)
        { assemblyTimer -= dt; if (assemblyTimer <= 0) { assemblyTimer = .5f; RefreshAssembly(); } }
        static string AssemblyModelKey(IList<XgModelEntry> models)
        { var key = new StringBuilder(); foreach (var m in models) key.Append(m.id).Append(':').Append(m.dataset).Append('|'); return key.ToString(); }

        void RefreshAssembly()
        {
            if (assemblyView == null || Sim == null) return;
            header.text = T("大脑 · 模型拼装", "BRAIN · MODEL ASSEMBLY"); header.fontStyle = FontStyles.Bold;
            string libraryKey = AssemblyModelKey(Sim.S.models);
            if (assemblyLibraryKey != libraryKey)
            {
                assemblyLibraryKey = libraryKey; SyncAssemblyCards(assemblyLibrary, Sim.S.models, false);
                for (int i = 0; i < Sim.S.models.Count; i++) assemblyLibrary[Sim.S.models[i].id].button.rt.SetSiblingIndex(Sim.S.models.Count - 1 - i);
            }
            var installed = Sim.BrainModels();
            installed.Sort((a, b) => { int r = AssemblyRegion(a.dataset).CompareTo(AssemblyRegion(b.dataset)); return r != 0 ? r : string.CompareOrdinal(a.dataset, b.dataset); });
            string memberKey = AssemblyModelKey(installed);
            if (memberKey != assemblyMembershipKey)
            {
                assemblyMembershipKey = memberKey; assemblyModels.Clear(); assemblyModels.AddRange(installed);
                SyncAssemblyCards(assemblyInstalled, installed, true); LayoutAssemblyModels(false);
            }
            else if (Mathf.Abs(assemblyCanvas.rect.width - Mathf.Max(760, assemblyBrainViewport.rect.width)) > 2) LayoutAssemblyModels(false);
            int visible = 0;
            foreach (var pair in assemblyLibrary)
            {
                var model = Sim.Model(pair.Key); if (model == null) continue;
                bool on = Sim.IsBrainInstalled(model.id);
                bool show = assemblyFilter == 0 || assemblyFilter == 1 && !on || assemblyFilter == 2 && on;
                pair.Value.button.Show(show); if (show) visible++;
                RefreshAssemblyModelCard(pair.Value, model, on, false);
            }
            foreach (var pair in assemblyInstalled)
            { var model = Sim.Model(pair.Key); if (model != null) RefreshAssemblyModelCard(pair.Value, model, true, true); }
            for (int i = 0; i < 3; i++) assemblyFilters[i].Set(assemblyFilters[i].label.text, true, i == assemblyFilter ? AssemblyCore : XgDark.Button, i == assemblyFilter ? Color.white : AssemblyMuted);
            assemblyEmptyLibrary.gameObject.SetActive(visible == 0);
            assemblyEmptyLibrary.text = Sim.S.models.Count == 0
                ? T("这里还没有模型卡。\n先训练一个模型并保存，\n再把学会的能力拼进灵光。", "No saved model cards yet.\nTrain and save a model,\nthen add its learned capability.")
                : T("此分类没有模型卡。", "No model cards in this filter.");
            assemblyCount.text = Sim.S.models.Count + T(" 个保存版本 · 滚动查看", " saved versions · scroll to browse");
            assemblySummary.text = assemblyInstalled.Count == 0 ? T("给灵光拼出第一块能力", "Build LingGuang's first capability") : assemblyInstalled.Count + T(" 个模型，拼成一个灵光", " models, one LingGuang");
            int[] counts = new int[3]; foreach (var model in installed) counts[AssemblyRegion(model.dataset)]++;
            assemblyCapabilities.text = T("看图 ", "Vision ") + counts[0] + "   ·   " + T("读句子 ", "Reading ") + counts[1] + "   ·   " + T("判断 ", "Reasoning ") + counts[2]
                + T("  /  每张卡保留自己的验证成绩", "  /  Each model keeps its own validation result");
            for (int i = 0; i < 3; i++) assemblyMissing[i].gameObject.SetActive(counts[i] == 0);
            bool selectedInstalled = Sim.Model(assemblySelectedId) != null && Sim.IsBrainInstalled(assemblySelectedId);
            assemblyRemove.Set(T("卸下模型", "Remove"), selectedInstalled, XgDark.Button, AssemblyInk);
            assemblyStatus.text = assemblyMessage.Length > 0 ? assemblyMessage : T("接入影响灵光的当前能力，不改训练、最佳纪录或订单部署。", "Assembly changes LingGuang's active capabilities, not training, records, or order deployments.");
        }

        void SyncAssemblyCards(Dictionary<int, BrainModelCard> cards, IList<XgModelEntry> models, bool installed)
        {
            var ids = new HashSet<int>(); foreach (var model in models) ids.Add(model.id);
            var removed = new List<int>(); foreach (var pair in cards) if (!ids.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (int id in removed) { cards[id].button.rt.gameObject.SetActive(false); UnityEngine.Object.Destroy(cards[id].button.rt.gameObject); cards.Remove(id); }
            foreach (var model in models) if (!cards.ContainsKey(model.id)) cards.Add(model.id, BuildAssemblyModelCard(model, installed));
        }

        void RefreshAssemblyModelCard(BrainModelCard card, XgModelEntry model, bool on, bool installed)
        {
            Color accent = AssemblyRegionColor(AssemblyRegion(model.dataset));
            card.button.Set(AssemblyModelName(model), true, installed ? XgDark.Card : on ? Color.Lerp(XgDark.Card, accent, .15f) : XgDark.Card, AssemblyInk);
            card.category.text = AssemblyRegionLabel(AssemblyRegion(model.dataset)); card.category.color = accent;
            card.version.text = AssemblyArchName(model) + " · v" + model.id;
            card.accuracy.text = T("验证 ", "Validation ") + N(model.acc * 100, "0.0") + "%" + T(" · 第 ", " · epoch ") + model.epoch + T(" 轮", "");
            if (card.state != null) card.state.text = on ? T("已拼入大脑", "INSTALLED IN BRAIN") : T("点击接入 / 拖进大脑", "Click or drag to install");
            card.outline.effectColor = assemblySelectedId == model.id ? accent : installed ? Color.Lerp(XgDark.Line, accent, .5f) : AssemblyLine;
            card.outline.effectDistance = assemblySelectedId == model.id ? new Vector2(2, -2) : new Vector2(1, -1);
        }

        void LayoutAssemblyModels(bool reset)
        {
            if (reset) assemblyPositions.Clear();
            float width = Mathf.Max(760, assemblyBrainViewport.rect.width);
            int[] counts = new int[3]; foreach (var model in assemblyModels) counts[AssemblyRegion(model.dataset)]++;
            float height = Mathf.Max(390, 66 + Mathf.Max(counts[0], counts[1]) * 128, 266 + counts[2] * 128);
            foreach (var model in assemblyModels)
                if (assemblyPositions.TryGetValue(model.dataset, out var p)) height = Mathf.Max(height, p.y + BrainCardHeight + 16);
            assemblyCanvas.sizeDelta = new Vector2(width, height);
            assemblyCore.anchoredPosition = new Vector2(width * .5f, -202);
            int[] rows = new int[3];
            foreach (var model in assemblyModels)
            {
                int region = AssemblyRegion(model.dataset), row = rows[region]++;
                if (!assemblyPositions.TryGetValue(model.dataset, out var position))
                {
                    position = AssemblyDefaultPosition(region, row, width);
                    while (AssemblyPositionOccupied(position, model.dataset)) position = AssemblyDefaultPosition(region, ++row, width);
                    assemblyPositions[model.dataset] = position;
                }
                position.x = Mathf.Clamp(position.x, 12, width - BrainCardWidth - 12);
                assemblyPositions[model.dataset] = position;
                PositionAssemblyCard(assemblyInstalled[model.id].button.rt, position);
                height = Mathf.Max(height, position.y + BrainCardHeight + 16);
            }
            assemblyCanvas.sizeDelta = new Vector2(width, height);
            for (int i = 0; i < 3; i++) PositionAssemblyCard(assemblyMissing[i], AssemblyDefaultPosition(i, 0, width));
            assemblyGraphic.Members.Clear(); assemblyGraphic.MemberColors.Clear();
            foreach (var model in assemblyModels)
            { assemblyGraphic.Members.Add(assemblyInstalled[model.id].button.rt); assemblyGraphic.MemberColors.Add(AssemblyRegionColor(AssemblyRegion(model.dataset))); }
            assemblyGraphic.SetVerticesDirty();
            if (reset) assemblyPan.SetView(1, Vector2.zero);
        }

        static Vector2 AssemblyDefaultPosition(int region, int row, float width)
        { return region == 0 ? new Vector2(34, 54 + row * 128) : region == 1 ? new Vector2(width - BrainCardWidth - 34, 54 + row * 128) : new Vector2(width * .5f - BrainCardWidth * .5f, 266 + row * 128); }
        static void PositionAssemblyCard(RectTransform rt, Vector2 topLeft)
        { rt.anchoredPosition = new Vector2(topLeft.x + BrainCardWidth * .5f, -topLeft.y - BrainCardHeight * .5f); }
        bool AssemblyPositionOccupied(Vector2 position, string dataset)
        {
            foreach (var model in assemblyModels)
                if (model.dataset != dataset && assemblyPositions.TryGetValue(model.dataset, out var other)
                    && Mathf.Abs(other.x - position.x) < BrainCardWidth + 8 && Mathf.Abs(other.y - position.y) < BrainCardHeight + 8) return true;
            return false;
        }
        void ArrangeAssemblyModels()
        { CancelAssemblyDrag(); LayoutAssemblyModels(true); assemblyMessage = T("模型已归位；只整理画布，不改变能力。", "Layout reset. Capabilities are unchanged."); RefreshAssembly(); }

        string AssemblyModelTip(int id)
        {
            var model = Sim.Model(id); if (model == null) return T("模型已不在仓库中。", "This model is no longer in the repository.");
            var data = XgCatalog.Dataset(model.dataset);
            return AssemblyModelName(model) + "\n" + AssemblyArchName(model) + " · v" + model.id + " · " + T("验证 ", "validation ") + N(model.acc * 100, "0.0") + "%"
                + (data != null ? "\n" + T(data.note, data.noteEn) : "") + "\n"
                + T("同一种能力只接入一个版本；替换不会删除旧模型。", "One version per capability. Replacing it does not delete the previous model.");
        }
        void SelectAssemblyModel(int id)
        {
            assemblySelectedId = id; var model = Sim.Model(id);
            assemblyMessage = model != null ? AssemblyModelName(model) + T("已选中。可拖动整理位置，或卸下这项能力。", " selected. Drag to rearrange, or remove this capability.") : "";
            RefreshAssembly();
        }

        void InstallAssemblyModel(int id)
        {
            CancelAssemblyDrag();
            var model = Sim.Model(id); if (model == null) { assemblyMessage = T("这张模型卡已不存在。", "This saved model no longer exists."); RefreshAssembly(); return; }
            assemblySelectedId = id;
            bool alreadyInstalled = Sim.IsBrainInstalled(id);
            var previous = Sim.BrainModel(model.dataset);
            // An explicit library choice pins this version even when it currently comes from the best-model fallback.
            bool success = Sim.InstallBrainModel(id, out string reason);
            assemblyMessage = success ? AssemblyModelName(model) + (alreadyInstalled ? T("已固定使用这个版本。", " is now fixed to this version.")
                : previous == null ? T("已拼入灵光。", " added to LingGuang.") : T("已换成这个版本；旧模型仍在仓库。", " replaced with this version; the old model is kept.")) : reason;
            if (success) host.Refresh(true);
            RefreshAssembly();
        }
        void RemoveSelectedAssemblyModel() => RemoveAssemblyModel(assemblySelectedId);
        void RemoveAssemblyModel(int id)
        {
            CancelAssemblyDrag(); var model = Sim.Model(id);
            if (model == null || !Sim.IsBrainInstalled(id)) return;
            bool removed = Sim.RemoveBrainModel(model.dataset, out string reason);
            assemblyMessage = removed ? AssemblyModelName(model) + T("已卸下，保存的模型没有删除。", " removed. The saved model is kept.") : reason;
            if (removed) { assemblySelectedId = 0; host.Refresh(true); }
            RefreshAssembly();
        }

        void BeginAssemblyDrag(int id, bool installed, PointerEventData e)
        {
            CancelAssemblyDrag(); var model = Sim.Model(id); if (model == null) return;
            assemblyDraggingId = id; assemblyDraggingInstalled = installed; assemblySelectedId = id;
            assemblyGhostName.text = AssemblyModelName(model); assemblyGhost.gameObject.SetActive(true); assemblyGhost.SetAsLastSibling();
            assemblyGraphic.Highlighted = true; assemblyGraphic.SetVerticesDirty(); MoveAssemblyDrag(e);
        }
        void MoveAssemblyDrag(PointerEventData e)
        {
            if (assemblyDraggingId == 0) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(assemblyView, e.position, e.pressEventCamera, out var local))
                assemblyGhost.anchoredPosition = local + new Vector2(18, -12);
        }
        void EndAssemblyDrag(PointerEventData e)
        {
            int id = assemblyDraggingId; bool moving = assemblyDraggingInstalled;
            if (id == 0) return;
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(assemblyBrainViewport, e.position, e.pressEventCamera);
            CancelAssemblyDrag();
            if (!inside) { assemblyMessage = T("没有放进大脑，当前模型组合未改变。", "Dropped outside the brain. The model assembly is unchanged."); RefreshAssembly(); return; }
            if (!moving) { InstallAssemblyModel(id); return; }
            var model = Sim.Model(id);
            if (model == null || !Sim.IsBrainInstalled(id)) { RefreshAssembly(); return; }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(assemblyCanvas, e.position, e.pressEventCamera, out var point))
            {
                assemblyPositions[model.dataset] = new Vector2(Mathf.Clamp(point.x - BrainCardWidth * .5f, 12, assemblyCanvas.rect.width - BrainCardWidth - 12), Mathf.Clamp(-point.y - BrainCardHeight * .5f, 24, assemblyCanvas.rect.height - BrainCardHeight - 12));
                LayoutAssemblyModels(false); assemblyMessage = T("位置已整理；模型能力与验证成绩不变。", "Card repositioned. Its capability and validation result are unchanged.");
            }
            RefreshAssembly();
        }
        void CancelAssemblyDrag()
        {
            assemblyDraggingId = 0; assemblyDraggingInstalled = false;
            if (assemblyGhost != null) assemblyGhost.gameObject.SetActive(false);
            if (assemblyGraphic != null && assemblyGraphic.Highlighted) { assemblyGraphic.Highlighted = false; assemblyGraphic.SetVerticesDirty(); }
        }
    }
}
