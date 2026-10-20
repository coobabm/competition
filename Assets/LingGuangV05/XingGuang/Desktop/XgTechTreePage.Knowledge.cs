using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTechTreePage
    {
        RectTransform knowledgeRoot, knowledgeList, knowledgeDetail;
        TMP_InputField knowledgeSearch;
        TMP_Text knowledgeCount, knowledgePlaceholder;
        XgBtn knowledgeTab, knowledgeTreeTab, knowledgeBack;
        readonly List<XgBtn> knowledgeCategories = new List<XgBtn>();
        readonly List<XgBtn> knowledgeRows = new List<XgBtn>();
        int knowledgeCategory = -1;
        bool knowledgeEnglish;
        string knowledgeOrigin;
        XgKnowledge.Entry knowledgeSelected;
        static readonly string[] KnowledgeCategoriesZh = { "全部", "基础", "模型结构", "数据与评估", "硬件" };
        static readonly string[] KnowledgeCategoriesEn = { "All", "Basics", "Models", "Data / tests", "Hardware" };

        void BuildKnowledge()
        {
            knowledgeTreeTab = ui.Button(root, T("科技树", "Tech tree"), CloseKnowledge, 14);
            PlaceTopLeft(knowledgeTreeTab, 12, 44, 100, 30);
            knowledgeTab = ui.Button(root, T("科普", "Knowledge"), () => OpenKnowledge(), 14);
            PlaceTopLeft(knowledgeTab, 116, 44, 110, 30);
            knowledgeRoot = Rect("Knowledge page", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(knowledgeRoot, XgDark.Page);
            knowledgeBack = ui.Button(knowledgeRoot, "", CloseKnowledge, 15);
            PlaceTopLeft(knowledgeBack, 12, 8, 260, 34);
            knowledgeCount = ui.Text(Strip("Knowledge count", knowledgeRoot, 10, 30, 286, 12), "", 15, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            var searchBox = Strip("Search terms", knowledgeRoot, 50, 36, 12, 12);
            var searchImage = Panel(searchBox, XgDark.Button);
            var textArea = Rect("Text area", searchBox, Vector2.zero, Vector2.one, new Vector2(10, 2), new Vector2(-10, -2));
            textArea.gameObject.AddComponent<RectMask2D>();
            var inputText = ui.Text(Rect("Input", textArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 16, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            inputText.richText = false;
            knowledgePlaceholder = ui.Text(Rect("Placeholder", textArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 15, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            knowledgeSearch = searchBox.gameObject.AddComponent<TMP_InputField>();
            knowledgeSearch.textViewport = textArea; knowledgeSearch.textComponent = (TextMeshProUGUI)inputText;
            knowledgeSearch.placeholder = knowledgePlaceholder; knowledgeSearch.targetGraphic = searchImage;
            knowledgeSearch.lineType = TMP_InputField.LineType.SingleLine;
            knowledgeSearch.onValueChanged.AddListener(_ => PaintKnowledgeList());
            var categories = Strip("Categories", knowledgeRoot, 94, 34, 12, 12);
            var layout = categories.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
            for (int i = 0; i < 5; i++)
            {
                int selectedCategory = i - 1;
                var b = ui.Button(categories, "", () => { knowledgeCategory = selectedCategory; PaintKnowledgeList(); }, 13);
                b.rt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                knowledgeCategories.Add(b);
            }
            var left = Rect("Term list", knowledgeRoot, Vector2.zero, new Vector2(.34f, 1), new Vector2(12, 12), new Vector2(-6, -138));
            var right = Rect("Term detail", knowledgeRoot, new Vector2(.34f, 0), Vector2.one, new Vector2(6, 12), new Vector2(-12, -138));
            knowledgeList = KnowledgeScroll(left); knowledgeDetail = KnowledgeScroll(right);
            foreach (var entry in XgKnowledge.Entries)
            {
                var target = entry;
                var b = ui.Button(knowledgeList, "", () => SelectKnowledge(target), 15);
                b.rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
                b.label.enableAutoSizing = true; b.label.fontSizeMin = 11; b.label.fontSizeMax = 15;
                knowledgeRows.Add(b);
            }
            knowledgeRoot.gameObject.SetActive(false);
        }

        RectTransform KnowledgeScroll(RectTransform viewportRect)
        {
            Panel(viewportRect, XgDark.Card);
            viewportRect.gameObject.AddComponent<RectMask2D>();
            var scroll = viewportRect.gameObject.AddComponent<ScrollRect>();
            var body = Rect("Content", viewportRect, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            body.pivot = new Vector2(.5f, 1);
            var layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10); layout.spacing = 8;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = body; scroll.viewport = viewportRect; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = false; scroll.scrollSensitivity = 28;
            return body;
        }

        // Also serves as the destination for terminology-card title/category links.
        public void OpenKnowledge(string term = null, int category = -1, string originNode = null)
        {
            if (knowledgeRoot == null) return;
            knowledgeOrigin = originNode ?? (detailShown != null ? detailShown.entry.First.id : null);
            hovered = null; HideTooltip();
            foreach (var node in views) node.StopMotion();
            knowledgeCategory = Mathf.Clamp(category, -1, 3);
            knowledgeSearch.SetTextWithoutNotify("");
            knowledgeSelected = XgKnowledge.Find(term) ?? knowledgeSelected ?? XgKnowledge.Find("算力");
            if (term != null && knowledgeSelected != null) knowledgeCategory = knowledgeSelected.Category;
            knowledgeRoot.gameObject.SetActive(true); knowledgeRoot.SetAsLastSibling();
            // Suspend the tree sub-canvas while reading: it must not raycast through this page.
            viewport.gameObject.SetActive(false);
            eraCard.gameObject.SetActive(false); eraRemaining = 0;
            RefreshKnowledge(true);
        }

        void CloseKnowledge()
        {
            if (knowledgeRoot == null || !knowledgeRoot.gameObject.activeSelf) return;
            knowledgeSearch.DeactivateInputField();
            knowledgeRoot.gameObject.SetActive(false);
            viewport.gameObject.SetActive(true);
            Refresh();
            if (!string.IsNullOrEmpty(knowledgeOrigin)) FocusNodeDetails(knowledgeOrigin);
        }

        void RefreshKnowledge(bool force = false)
        {
            if (knowledgeTab == null) return;
            knowledgeTab.Set(T("科普", "Knowledge"), true);
            knowledgeTreeTab.Set(T("科技树", "Tech tree"), true, XgDark.AccentSoft);
            if (!knowledgeRoot.gameObject.activeSelf) return;
            if (!force && knowledgeEnglish == En) return;
            knowledgeEnglish = En;
            var origin = XgCatalog.Node(knowledgeOrigin ?? "");
            knowledgeBack.Set(T("← 返回科技树", "← Back to tech tree") + (origin == null ? "" : " · " + XgTechTreeCopy.Name(origin, Sim)), true, XgDark.AccentSoft);
            knowledgeBack.label.enableAutoSizing = true; knowledgeBack.label.fontSizeMin = 11; knowledgeBack.label.fontSizeMax = 15;
            knowledgePlaceholder.text = T("搜索词条，如 卷积、算力、显存", "Search terms, e.g. convolution, compute, VRAM");
            PaintKnowledgeList(); SelectKnowledge(knowledgeSelected);
        }

        string KnowledgeRowName(XgKnowledge.Entry entry)
        {
            string badge = entry.IsNew ? "  <size=70%><color=#" + ColorUtility.ToHtmlStringRGB(XgDark.Good) + ">" + T("新", "NEW") + "</color></size>" : "";
            return entry.Name(En) + badge;
        }

        void PaintKnowledgeList()
        {
            if (knowledgeCount == null) return;
            int visible = 0;
            for (int i = 0; i < XgKnowledge.Entries.Length; i++)
            {
                var entry = XgKnowledge.Entries[i];
                bool show = (knowledgeCategory < 0 || entry.Category == knowledgeCategory) && entry.Matches(knowledgeSearch.text);
                knowledgeRows[i].Show(show);
                knowledgeRows[i].Set(KnowledgeRowName(entry), true, entry == knowledgeSelected ? XgDark.AccentSoft : XgDark.Button);
                if (show) visible++;
            }
            for (int i = 0; i < knowledgeCategories.Count; i++)
            {
                int cat = i - 1, count = 0;
                foreach (var e in XgKnowledge.Entries) if (cat < 0 || e.Category == cat) count++;
                knowledgeCategories[i].Set((En ? KnowledgeCategoriesEn[i] : KnowledgeCategoriesZh[i]) + " " + count, true,
                    cat == knowledgeCategory ? XgDark.AccentSoft : XgDark.Button);
            }
            knowledgeCount.text = visible == 0 ? T("没有匹配词条，试试其他关键词或分类。", "No matches. Try another term or category.") : T("科普 · ", "Knowledge · ") + visible + " / " + XgKnowledge.Entries.Length;
            knowledgeList.anchoredPosition = Vector2.zero;
        }

        void KnowledgeText(string text, int size, Color color)
        {
            var rt = Rect("Paragraph", knowledgeDetail, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ui.Text(rt, text, size, color, TextAlignmentOptions.TopLeft);
        }

        void SelectKnowledge(XgKnowledge.Entry entry)
        {
            if (entry == null) return;
            knowledgeSelected = entry;
            foreach (Transform child in knowledgeDetail) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
            KnowledgeText(entry.Name(En), 23, XgDark.Ink);
            KnowledgeText((En ? KnowledgeCategoriesEn : KnowledgeCategoriesZh)[entry.Category + 1], 14, XgDark.Good);
            KnowledgeText(entry.Body(En), 17, XgDark.Ink);
            string usage = En ? entry.EnglishUsage : entry.ChineseUsage;
            if (!string.IsNullOrEmpty(usage))
            {
                KnowledgeText(T("游戏中在哪里遇到", "Where it appears in the game"), 15, XgDark.Muted);
                KnowledgeText(usage, 16, XgDark.Ink);
            }
            KnowledgeText(T("延伸阅读", "Related terms"), 16, XgDark.Muted);
            foreach (string key in entry.Related)
            {
                var target = XgKnowledge.Find(key);
                if (target == null) continue;
                var b = ui.Button(knowledgeDetail, target.Name(En) + " ›", () => { SelectKnowledge(target); }, 15);
                b.rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
            }
            KnowledgeText(T("以2016年前后技术为背景；游戏数值是简化规则，后续技术会注明年代。", "Set around 2016. Game values simplify reality; later technologies are marked with historical context."), 13, XgDark.Muted);
            knowledgeDetail.anchoredPosition = Vector2.zero;
            for (int i = 0; i < knowledgeRows.Count; i++) knowledgeRows[i].Set(KnowledgeRowName(XgKnowledge.Entries[i]), true, XgKnowledge.Entries[i] == entry ? XgDark.AccentSoft : XgDark.Button);
        }
    }
}
