using System;
using System.Collections.Generic;
using System.Linq;
using LingGuang.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuang.Game
{
    /// <summary>Code-built uGUI HUD. Display only; every button calls back into the controller.</summary>
    public sealed class Hud : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Canvas canvas;
        RectTransform root;

        public TextMeshProUGUI stageText, targetText, gateText, lightText, multText, estimateText, startsText, movesText, coinsText, toast, tooltip, hintText;
        public RectTransform lightAnchor, tooltipPanel, candidateBar, itemsPanel, drugsPanel, shopPanel, modal, debugPanel;
        public TextMeshProUGUI modalTitle, modalBody, debugLog, debugInfo;
        public Button modalButton, modalButton2;
        public TMP_InputField seedInput;
        Image progressFill;
        float toastT;
        public Action<int> onCandidate, onBuy, onUseDrug, onSellItem;
        public Action onReview, onReroll, onRefreshShop, onSkipCandidates, onConfirm, onEndRound, onUndo, onTrial, onNewRun, onStep, onSkipAnim, onToggleStep, onToggleInternal, onToggleReduceFlash;
        public Func<string> modalAction1, modalAction2;
        Action modalCb1, modalCb2;
        readonly List<Button> candidateButtons = new List<Button>();
        public int selectedCandidate = -1;
        public readonly List<string> eventLog = new List<string>();
        public int shownLight;
        public float shownMult;
        Button trialButton, reviewBtn, rerollBtn;
        RectTransform insightBar;
        HudUiHint gateHint;
        HudUiHint activeUiHint;
        public HudUiHint ActiveUiHint
        {
            get
            {
                // Normalize Unity's destroyed-object null, including EditMode refreshes
                // where lifecycle callbacks may not have run.
                if (activeUiHint == null || !activeUiHint.isActiveAndEnabled) activeUiHint = null;
                return activeUiHint;
            }
            set => activeUiHint = value;
        }
        const float ScoreWidth = 540;

        static readonly Color PanelColor = Color.clear;
        static readonly Color Accent = IsaacHudSkin.Gold;

        public void Build(TMP_FontAsset f)
        {
            font = f;
            var cgo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root = cgo.GetComponent<RectTransform>();

            // Lightweight corner HUD: counters, chalk lettering and original ink doodles.
            var tl = Panel(root, Vector2.up, Vector2.up, new Vector2(28, -24), new Vector2(300, 158));
            tl.name = "Resources";
            Glyph(tl, "charge", new Vector2(0, -2), 36);
            startsText = Text(tl, "", 28, IsaacHudSkin.Chalk, new Vector2(46, -2), new Vector2(245, 40));
            Glyph(tl, "move", new Vector2(0, -48), 36);
            movesText = Text(tl, "", 21, IsaacHudSkin.Chalk, new Vector2(46, -52), new Vector2(245, 32));
            Glyph(tl, "coin", new Vector2(0, -94), 36);
            coinsText = Text(tl, "", 24, Accent, new Vector2(46, -98), new Vector2(260, 34));

            var tc = Panel(root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-290, -20), new Vector2(580, 164));
            tc.name = "Score";
            stageText = Text(tc, "", 30, IsaacHudSkin.Chalk, new Vector2(0, 0), new Vector2(580, 42));
            stageText.alignment = TextAlignmentOptions.Center;
            lightText = Text(tc, "光量 0", 28, Accent, new Vector2(20, -48), new Vector2(160, 40));
            lightAnchor = lightText.rectTransform;
            multText = Text(tc, "× (1 + 0)", 27, IsaacHudSkin.Chalk, new Vector2(180, -48), new Vector2(250, 40));
            estimateText = Text(tc, "= 0", 32, IsaacHudSkin.Chalk, new Vector2(430, -45), new Vector2(140, 42));
            foreach (var score in new[] { lightText, multText, estimateText })
            { score.enableAutoSizing = true; score.fontSizeMin = 16; score.fontSizeMax = score.fontSize; }
            var bar = Img(tc, new Color(.3f, .27f, .24f, .75f), new Vector2(20, -94), new Vector2(ScoreWidth, 8));
            progressFill = Img(bar.rectTransform, Accent, Vector2.zero, new Vector2(ScoreWidth, 8));
            hintText = Text(tc, "", 17, IsaacHudSkin.Muted, new Vector2(20, -114), new Vector2(ScoreWidth, 46));
            hintText.enableWordWrapping = true;

            var tr = Panel(root, Vector2.one, Vector2.one, new Vector2(-314, -24), new Vector2(290, 112));
            tr.name = "RoundAndGate";
            targetText = Text(tr, "", 23, IsaacHudSkin.Chalk, new Vector2(0, 0), new Vector2(290, 34));
            gateText = Text(tr, "", 20, Accent, new Vector2(0, -42), new Vector2(290, 56));
            gateText.enableWordWrapping = true;
            gateHint = Hint(gateText.gameObject, "");
            gateText.raycastTarget = true;
            insightBar = Panel(root, Vector2.one, Vector2.one, new Vector2(-314, -128), new Vector2(290, 50));
            insightBar.name = "InsightActions";
            reviewBtn = Btn(insightBar, "复盘", new Vector2(0, 0), new Vector2(134, 44), () => onReview?.Invoke(), 18);
            rerollBtn = Btn(insightBar, "再来一次", new Vector2(146, 0), new Vector2(144, 44), () => onReroll?.Invoke(), 18);
            Hint(reviewBtn.gameObject, "消耗领悟复盘，查看传播路径。");
            Hint(rerollBtn.gameObject, "消耗领悟重新抽取候选灵光；本轮放置前可用。");

            candidateBar = Panel(root, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-356, 22), new Vector2(712, 118));
            candidateBar.name = "Candidates";
            Text(candidateBar, "选择灵光 · 点击棋盘放置 · 滚轮旋转", 18, IsaacHudSkin.Muted, new Vector2(8, 0), new Vector2(600, 26));
            itemsPanel = Panel(root, Vector2.up, Vector2.up, new Vector2(28, -200), new Vector2(252, 290));
            itemsPanel.name = "Items";
            drugsPanel = Panel(root, Vector2.zero, Vector2.zero, new Vector2(28, 28), new Vector2(250, 106));
            drugsPanel.name = "Drugs";
            shopPanel = Panel(root, Vector2.one, Vector2.one, new Vector2(-292, -228), new Vector2(268, 276));
            shopPanel.name = "Shop";

            var br = Panel(root, Vector2.right, Vector2.right, new Vector2(-330, 22), new Vector2(306, 126));
            br.name = "RoundActions";
            var confirm = Btn(br, "发动 [Enter]", new Vector2(0, 0), new Vector2(148, 56), () => onConfirm?.Invoke(), 22);
            confirm.GetComponent<Image>().color = IsaacHudSkin.Red;
            confirm.GetComponentInChildren<TextMeshProUGUI>().color = IsaacHudSkin.Chalk;
            Btn(br, "结束本轮", new Vector2(158, 0), new Vector2(148, 56), () => onEndRound?.Invoke(), 22);
            Btn(br, "撤销 [Esc]", new Vector2(0, -66), new Vector2(148, 56), () => onUndo?.Invoke(), 20);
            trialButton = Btn(br, "试运行 [T]", new Vector2(158, -66), new Vector2(148, 56), () => onTrial?.Invoke(), 20);

            // ---- toast & tooltip
            toast = Text(root, "", 26, Color.white, Vector2.zero, new Vector2(1000, 40));
            toast.alignment = TextAlignmentOptions.Center;
            Anchor(toast.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-500, 190));
            tooltipPanel = Panel(root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(360, 140));
            tooltipPanel.pivot = new Vector2(0, 1);
            IsaacHudSkin.ApplyPaper(tooltipPanel.GetComponent<Image>(), IsaacHudSkin.PaperLight);
            tooltip = Text(tooltipPanel, "", 21, IsaacHudSkin.Ink, new Vector2(20, -16), new Vector2(360, 124));
            tooltip.enableWordWrapping = true;
            tooltipPanel.gameObject.SetActive(false);
            foreach (var g in tooltipPanel.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            toast.raycastTarget = false;

            // ---- modal
            modal = Panel(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, 300), new Vector2(840, 600));
            IsaacHudSkin.ApplyPaper(modal.GetComponent<Image>(), IsaacHudSkin.PaperLight);
            modal.GetComponent<Image>().raycastTarget = true;
            modal.pivot = new Vector2(0.5f, 0.5f);
            modal.anchoredPosition = Vector2.zero;
            modalTitle = Text(modal, "", 44, IsaacHudSkin.Red, new Vector2(30, -24), new Vector2(780, 60));
            modalTitle.alignment = TextAlignmentOptions.Center;
            modalBody = Text(modal, "", 24, IsaacHudSkin.Ink, new Vector2(40, -100), new Vector2(760, 400));
            modalBody.enableWordWrapping = true;
            modalBody.enableAutoSizing = true;
            modalBody.fontSizeMin = 18;
            modalBody.fontSizeMax = 24;
            modalButton = Btn(modal, "继续", new Vector2(250, -510), new Vector2(160, 64), () => { var cb = modalCb1; HideModal(); cb?.Invoke(); }, 26);
            modalButton2 = Btn(modal, "", new Vector2(430, -510), new Vector2(160, 64), () => { var cb = modalCb2; HideModal(); cb?.Invoke(); }, 26);
            modal.gameObject.SetActive(false);

            // ---- debug
            debugPanel = Panel(root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 20), new Vector2(520, 520));
            debugPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.88f);
            Text(debugPanel, "调试面板 (F1)", 22, Accent, new Vector2(12, -8), new Vector2(300, 30));
            seedInput = Input(debugPanel, new Vector2(12, -44), new Vector2(170, 40));
            Btn(debugPanel, "新开一局", new Vector2(190, -44), new Vector2(120, 40), () => onNewRun?.Invoke(), 18);
            Btn(debugPanel, "单拍模式", new Vector2(318, -44), new Vector2(90, 40), () => onToggleStep?.Invoke(), 16);
            Btn(debugPanel, "下一拍", new Vector2(414, -44), new Vector2(94, 40), () => onStep?.Invoke(), 16);
            Btn(debugPanel, "跳过动画", new Vector2(12, -92), new Vector2(120, 36), () => onSkipAnim?.Invoke(), 16);
            Btn(debugPanel, "内部电量", new Vector2(140, -92), new Vector2(120, 36), () => onToggleInternal?.Invoke(), 16);
            Btn(debugPanel, "降低闪烁", new Vector2(268, -92), new Vector2(120, 36), () => onToggleReduceFlash?.Invoke(), 16);
            debugInfo = Text(debugPanel, "", 15, new Color(0.8f, 1f, 0.8f), new Vector2(12, -134), new Vector2(496, 60));
            debugLog = Text(debugPanel, "", 13, new Color(0.85f, 0.85f, 0.9f), new Vector2(12, -196), new Vector2(496, 316));
            debugLog.enableWordWrapping = false;
            debugLog.overflowMode = TextOverflowModes.Truncate;
            debugPanel.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ builders

        static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = new Vector2(0, 1); rt.anchoredPosition = pos;
        }

        RectTransform Panel(RectTransform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.pivot = new Vector2(0, aMin.y >= 0.99f ? 1 : aMin.y <= 0.01f ? 0 : 0.5f);
            if (aMin.y <= 0.01f) rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = PanelColor;
            go.GetComponent<Image>().raycastTarget = false;
            return rt;
        }

        Image Img(RectTransform parent, Color c, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Img", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public TextMeshProUGUI Text(RectTransform parent, string text, float size, Color c, Vector2 pos, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = c;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            if (c.r + c.g + c.b > 1.8f) IsaacHudSkin.Outline(t);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = box;
            return t;
        }

        public Button Btn(RectTransform parent, string label, Vector2 pos, Vector2 size, Action onClick, float fontSize = 22)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            IsaacHudSkin.ApplyPaper(img, IsaacHudSkin.Paper);
            var b = go.GetComponent<Button>();
            var cb = b.colors;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(.78f, .70f, .62f, 1f);
            cb.disabledColor = new Color(.55f, .55f, .55f, .7f);
            b.colors = cb;
            b.onClick.AddListener(() => onClick?.Invoke());
            var t = Text(rt, label, fontSize, IsaacHudSkin.Ink, Vector2.zero, size);
            t.fontStyle |= FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = Vector2.zero; t.rectTransform.offsetMax = Vector2.zero;
            t.enableAutoSizing = true; t.fontSizeMin = Mathf.Min(16, fontSize); t.fontSizeMax = fontSize;
            return b;
        }

        TMP_InputField Input(RectTransform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);
            var area = new GameObject("Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(go.transform, false);
            var art = area.GetComponent<RectTransform>();
            art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one; art.offsetMin = new Vector2(8, 4); art.offsetMax = new Vector2(-8, -4);
            var txt = Text(art, "", 20, Color.white, Vector2.zero, size);
            txt.rectTransform.anchorMin = Vector2.zero; txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero; txt.rectTransform.offsetMax = Vector2.zero;
            var field = go.AddComponent<TMP_InputField>();
            field.textViewport = art;
            field.textComponent = txt;
            field.contentType = TMP_InputField.ContentType.IntegerNumber;
            if (font != null) field.fontAsset = font;
            return field;
        }

        void ClearChildren(RectTransform rt, int keep = 0)
        {
            for (int i = rt.childCount - 1; i >= keep; i--)
            {
                var child = rt.GetChild(i).gameObject;
                child.SetActive(false); // No stale raycast targets during same-frame refreshes.
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }

        // ------------------------------------------------------------------ refresh

        public void Refresh(RunState s, bool playing)
        {
            var cfg = s.cfg;
            stageText.text = $"{cfg.stageNames[s.stage]} · 第 {s.roundInStage + 1} 轮{(s.IsBossRound ? "（关口）" : "")}";
            targetText.text = $"目标 {s.Target}    第 {s.RoundIndex + 1}/18 轮";
            var gate = s.StageGate;
            gateText.text = gate != null ? $"关口 · {gate.name}\n<size=16>{(s.IsBossRound ? "生效中" : "第 3 轮生效")} · 悬停查看</size>" : (s.IsBossRound ? "关口轮 · 双倍目标" : "");
            gateHint.Description = gate != null ? $"{gate.name}\n{gate.desc}" : "关口轮目标为基准的 2 倍。";
            if (!playing) SetScore(s.roundLight, s.roundAddMult, s);
            startsText.text = $"{s.startsLeft:00}  起点次数";
            movesText.text = $"{s.movesLeft:00}  移动 · {(s.placedThisRound ? "本轮已放置" : "可放置 1 个")}";
            coinsText.text = $"{s.coins:00}  余光" + (cfg.insightEnabled ? $"    领悟 {s.insight}" : "");
            insightBar.gameObject.SetActive(cfg.insightEnabled && s.phase == Phase.Prepare);
            reviewBtn.interactable = !s.reviewActive && s.insight >= cfg.reviewCost;
            rerollBtn.interactable = !s.placedThisRound && s.candidates.Count > 0 && s.insight >= cfg.rerollCost;
            reviewBtn.GetComponentInChildren<TextMeshProUGUI>().text = s.reviewActive ? "复盘中" : $"复盘 ({cfg.reviewCost})";
            rerollBtn.GetComponentInChildren<TextMeshProUGUI>().text = $"再来一次 ({cfg.rerollCost})";
            trialButton.gameObject.SetActive(s.items.Contains("computer"));

            // candidates
            ClearChildren(candidateBar, 1);
            candidateButtons.Clear();
            for (int i = 0; i < s.candidates.Count; i++)
            {
                int idx = i;
                var shape = s.candidates[i];
                var def = cfg.Shape(shape);
                var b = Btn(candidateBar, $"{def.name}\n<size=16>阈值 {def.threshold / 2f:0.#}  光量 {def.light}\n输出 {def.output / 2f:0.#}</size>", new Vector2(4 + i * 188, -32), new Vector2(180, 84), () => onCandidate?.Invoke(idx), 20);
                var art = NeuronArt.Get(shape);
                if (art != null)
                {
                    var pocket = Img((RectTransform)b.transform, new Color(.10f, .12f, .16f), new Vector2(8, -10), new Vector2(48, 64));
                    IsaacHudSkin.ApplyPaper(pocket, pocket.color);
                    var icon = Img((RectTransform)b.transform, SparkView.ShapeColor(shape, false), new Vector2(4, -6), new Vector2(54, 60));
                    icon.name = "NeuronIcon";
                    icon.sprite = art;
                    icon.material = shape == Shape.Conduct ? Gfx.Neuron : Gfx.NeuronIcon;
                    icon.raycastTarget = false;
                    icon.preserveAspect = true;
                    var caption = b.GetComponentInChildren<TextMeshProUGUI>();
                    caption.rectTransform.anchorMin = new Vector2(0, 0);
                    caption.rectTransform.anchorMax = new Vector2(1, 1);
                    caption.rectTransform.offsetMin = new Vector2(60, 4);
                    caption.rectTransform.offsetMax = new Vector2(-4, -4);
                    caption.alignment = TextAlignmentOptions.MidlineLeft;
                }
                b.GetComponent<Image>().color = idx == selectedCandidate ? IsaacHudSkin.Gold : IsaacHudSkin.PaperLight;
                Hint(b.gameObject, $"{def.name}\n阈值 {def.threshold / 2f:0.#} · 光量 {def.light} · 输出 {def.output / 2f:0.#}\n点击选择，再点击空格放置；滚轮旋转。");
                candidateButtons.Add(b);
            }
            if (s.candidates.Count > 0) Btn(candidateBar, "放弃", new Vector2(572, -32), new Vector2(132, 84), () => onSkipCandidates?.Invoke(), 24);
            else Text(candidateBar, s.placedThisRound ? "本轮已放置或放弃" : "", 20, new Color(1, 1, 1, 0.5f), new Vector2(16, -50), new Vector2(500, 30));

            // Inventory keeps compact labels; complete rules live in hover/focus hints.
            ClearChildren(itemsPanel);
            Text(itemsPanel, $"物件  {s.items.Count}/{s.ItemSlots}", 20, IsaacHudSkin.Muted, new Vector2(0, 0), new Vector2(230, 30));
            for (int i = 0; i < s.items.Count; i++)
            {
                var d = EffectDef.Get(s.items[i]);
                var row = Panel(itemsPanel, Vector2.up, Vector2.up, new Vector2(0, -36 - i * 48), new Vector2(242, 44));
                row.name = "Item_" + d.id;
                row.GetComponent<Image>().raycastTarget = true;
                Glyph(row, IsaacHudSkin.ItemGlyph(d.id), new Vector2(0, -2), 38);
                Text(row, d.name, 23, IsaacHudSkin.Chalk, new Vector2(48, -5), new Vector2(190, 34));
                Hint(row.gameObject, d.name + "\n" + d.desc);
            }
            itemsPanel.sizeDelta = new Vector2(252, 36 + s.items.Count * 48);
            itemsPanel.gameObject.SetActive(s.ItemSlots > 0 || s.items.Count > 0);

            ClearChildren(drugsPanel);
            Text(drugsPanel, $"药品  {s.drugs.Count}/{cfg.drugCarryMax} · 每轮 1 个", 18, IsaacHudSkin.Muted, new Vector2(0, 0), new Vector2(250, 30));
            for (int i = 0; i < s.drugs.Count; i++)
            {
                int idx = i;
                var d = EffectDef.Get(s.drugs[i]);
                var b = Btn(drugsPanel, (d.warning ? "！" : "") + d.name, new Vector2(i * 125, -36), new Vector2(120, 62), () => onUseDrug?.Invoke(idx), 18);
                Hint(b.gameObject, d.name + "\n" + d.desc + "\n点击使用；每轮最多 1 个。");
            }
            drugsPanel.gameObject.SetActive(s.drugs.Count > 0 || s.stage >= cfg.shopFromStage);

            ClearChildren(shopPanel);
            shopPanel.gameObject.SetActive(s.shop.Count > 0);
            Text(shopPanel, "商店", 23, IsaacHudSkin.Chalk, new Vector2(4, 0), new Vector2(100, 34));
            if (s.shop.Count > 0)
                Btn(shopPanel, $"刷新 {s.ShopRerollCost}", new Vector2(146, 0), new Vector2(116, 36), () => onRefreshShop?.Invoke(), 17);
            for (int i = 0; i < s.shop.Count; i++)
            {
                int idx = i;
                var o = s.shop[i];
                var d = EffectDef.Get(o.id);
                string label = o.sold ? $"{d.name} · 已售" : $"{(d.warning ? "！" : "")}{d.name}\n<size=18>{s.PriceOf(o)} 余光</size>";
                var b = Btn(shopPanel, label, new Vector2(0, -48 - i * 76), new Vector2(268, 68), () => onBuy?.Invoke(idx), 22);
                b.interactable = !o.sold;
                var caption = b.GetComponentInChildren<TextMeshProUGUI>();
                caption.rectTransform.offsetMin = new Vector2(62, 6);
                caption.rectTransform.offsetMax = new Vector2(-12, -6);
                caption.alignment = TextAlignmentOptions.MidlineLeft;
                Glyph((RectTransform)b.transform, d.kind == DefKind.Drug ? "pill" : IsaacHudSkin.ItemGlyph(d.id), new Vector2(14, -14), 40);
                Hint(b.gameObject, d.name + "\n" + d.desc + (o.sold ? "\n已售出" : $"\n价格：{s.PriceOf(o)} 余光"));
            }
            shopPanel.sizeDelta = new Vector2(268, 48 + s.shop.Count * 76);
        }

        void Glyph(RectTransform parent, string kind, Vector2 pos, float size)
        {
            var icon = Img(parent, Color.white, pos, Vector2.one * size);
            icon.sprite = IsaacHudSkin.Icon(kind);
            icon.preserveAspect = true;
        }

        HudUiHint Hint(GameObject target, string description)
        {
            if (target.GetComponent<Selectable>() == null)
            {
                var selectable = target.AddComponent<Selectable>();
                selectable.targetGraphic = target.GetComponent<Graphic>();
                selectable.transition = Selectable.Transition.None;
            }
            var hint = target.AddComponent<HudUiHint>();
            hint.Owner = this; hint.Description = description;
            return hint;
        }

        public void SetScore(int light, float addMult, RunState s)
        {
            shownLight = light; shownMult = addMult;
            float settle = s.SettleMultiplier();
            int est = (int)Math.Floor(Math.Max(0, light) * (1 + Math.Max(0, addMult)) * settle + 1e-3);
            lightText.text = $"光量 {light}";
            multText.text = settle != 1f ? $"× (1+{addMult:0.##}) ×{settle:0.##}" : $"× (1 + {addMult:0.##})";
            estimateText.text = $"= {est}";
            float k = s.Target > 0 ? Mathf.Clamp01(est / (float)s.Target) : 0;
            progressFill.rectTransform.sizeDelta = new Vector2(ScoreWidth * k, 8);
            progressFill.color = est >= s.Target ? new Color(0.6f, 1f, 0.7f) : Accent;
            estimateText.color = est >= s.Target ? new Color(0.7f, 1f, 0.75f) : Color.white;
        }

        public void Pump(TextMeshProUGUI t)
        {
            t.transform.localScale = Vector3.one * 1.15f;
        }

        public void Toast(string msg, float seconds = 2.2f)
        {
            toast.text = msg;
            toastT = seconds;
        }

        public void ShowTooltip(string text, Vector2 screenPos)
        {
            if (ModalOpen) { tooltipPanel.gameObject.SetActive(false); return; }
            if (ActiveUiHint != null && ActiveUiHint.isActiveAndEnabled)
            {
                text = ActiveUiHint.Description;
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == ActiveUiHint.gameObject)
                    screenPos = RectTransformUtility.WorldToScreenPoint(null, ActiveUiHint.transform.position);
            }
            if (string.IsNullOrEmpty(text)) { tooltipPanel.gameObject.SetActive(false); return; }
            tooltipPanel.gameObject.SetActive(true);
            tooltipPanel.SetAsLastSibling();
            tooltip.text = PaperText(text);
            tooltip.ForceMeshUpdate();
            float h = Mathf.Max(76, tooltip.preferredHeight + 32);
            tooltipPanel.sizeDelta = new Vector2(400, h);
            tooltip.rectTransform.sizeDelta = new Vector2(360, h - 32);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, null, out var local);
            var size = root.rect.size;
            local += size / 2f + new Vector2(22, -22);
            local.x = Mathf.Clamp(local.x, 12, Mathf.Max(12, size.x - 412));
            local.y = Mathf.Clamp(local.y, h + 12, Mathf.Max(h + 12, size.y - 12));
            tooltipPanel.anchoredPosition = local;
        }

        public void ShowModal(string title, string body, string b1, Action cb1, string b2 = null, Action cb2 = null)
        {
            ActiveUiHint = null;
            tooltipPanel.gameObject.SetActive(false);
            modal.gameObject.SetActive(true);
            modal.SetAsLastSibling();
            modalTitle.text = title;
            modalBody.text = PaperText(body);
            modalButton.GetComponentInChildren<TextMeshProUGUI>().text = b1;
            modalCb1 = cb1;
            modalButton2.gameObject.SetActive(b2 != null);
            if (b2 != null) modalButton2.GetComponentInChildren<TextMeshProUGUI>().text = b2;
            modalCb2 = cb2;
            var rt1 = modalButton.GetComponent<RectTransform>();
            rt1.anchoredPosition = b2 != null ? new Vector2(250, -510) : new Vector2(340, -510);
        }

        public void HideModal() => modal.gameObject.SetActive(false);
        public bool ModalOpen => modal.gameObject.activeSelf;

        // Existing presenters use luminous colors on a dark background. Remap only the
        // known emphasis palette at the presentation boundary; keep their wording intact.
        static string PaperText(string text) => text?.Replace("#ffd9a0", "#713e19")
            .Replace("#a8ffb8", "#275a35").Replace("#ff9a8a", "#8c2923")
            .Replace("#8090a0", "#4b5058").Replace("#9fb6d8", "#364e72")
            .Replace("#7fffe0", "#24634f");

        public void Log(string line)
        {
            eventLog.Add(line);
            if (eventLog.Count > 400) eventLog.RemoveRange(0, eventLog.Count - 400);
            if (debugPanel.gameObject.activeSelf) debugLog.text = string.Join("\n", eventLog.Skip(Math.Max(0, eventLog.Count - 22)));
        }

        public static bool PointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        void Update()
        {
            if (toastT > 0f)
            {
                toastT -= Time.deltaTime;
                var c = toast.color; c.a = Mathf.Clamp01(toastT / 0.4f); toast.color = c;
            }
            foreach (var t in new[] { lightText, multText, estimateText })
                if (t != null) t.transform.localScale = Vector3.Lerp(t.transform.localScale, Vector3.one, Time.deltaTime * 8f);
        }
    }
}
