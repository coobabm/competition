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

        static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.09f, 0.82f);
        static readonly Color Accent = new Color(1f, 0.88f, 0.62f);

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

            // ---- top-left: stage
            var tl = Panel(root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(430, 150));
            stageText = Text(tl, "", 34, Accent, new Vector2(18, -12), new Vector2(400, 44));
            targetText = Text(tl, "", 24, Color.white, new Vector2(18, -58), new Vector2(400, 32));
            gateText = Text(tl, "", 19, new Color(1f, 0.7f, 0.55f), new Vector2(18, -92), new Vector2(400, 54));
            gateText.enableWordWrapping = true;

            // ---- top-center: score
            var tc = Panel(root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-330, -20), new Vector2(660, 128));
            lightText = Text(tc, "光量 0", 30, new Color(1f, 0.92f, 0.7f), new Vector2(20, -12), new Vector2(210, 40));
            lightAnchor = lightText.rectTransform;
            multText = Text(tc, "× (1 + 0)", 30, new Color(0.7f, 1f, 0.82f), new Vector2(230, -12), new Vector2(220, 40));
            estimateText = Text(tc, "= 0", 36, Color.white, new Vector2(450, -8), new Vector2(200, 46));
            var bar = Img(tc, new Color(1, 1, 1, 0.08f), new Vector2(20, -72), new Vector2(620, 14));
            progressFill = Img(bar.rectTransform, Accent, Vector2.zero, new Vector2(620, 14));
            progressFill.rectTransform.anchorMin = new Vector2(0, 0); progressFill.rectTransform.anchorMax = new Vector2(0, 1);
            progressFill.rectTransform.pivot = new Vector2(0, 0.5f);
            progressFill.rectTransform.anchoredPosition = Vector2.zero;
            hintText = Text(tc, "", 18, new Color(1, 1, 1, 0.6f), new Vector2(20, -92), new Vector2(620, 30));

            // ---- top-right: resources
            var tr = Panel(root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-344, -20), new Vector2(320, 128));
            startsText = Text(tr, "", 26, Color.white, new Vector2(18, -12), new Vector2(300, 34));
            movesText = Text(tr, "", 22, new Color(1, 1, 1, 0.8f), new Vector2(18, -50), new Vector2(300, 30));
            coinsText = Text(tr, "", 22, new Color(1f, 0.85f, 0.4f), new Vector2(18, -84), new Vector2(300, 30));

            // ---- insight spends (§14 lite)
            insightBar = Panel(root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-344, -156), new Vector2(320, 58));
            reviewBtn = Btn(insightBar, "复盘", new Vector2(10, -9), new Vector2(145, 40), () => onReview?.Invoke(), 18);
            rerollBtn = Btn(insightBar, "再来一次", new Vector2(165, -9), new Vector2(145, 40), () => onReroll?.Invoke(), 18);

            // ---- bottom: candidates
            candidateBar = Panel(root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-380, 20), new Vector2(760, 120));
            Text(candidateBar, "候选灵光（点选后放到棋盘上，滚轮旋转）", 18, new Color(1, 1, 1, 0.6f), new Vector2(16, -6), new Vector2(560, 26));

            // ---- left: items & drugs
            itemsPanel = Panel(root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -186), new Vector2(260, 300));
            drugsPanel = Panel(root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 190), new Vector2(260, 100));
            // ---- right: shop
            shopPanel = Panel(root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-284, -228), new Vector2(260, 260));

            // ---- bottom-right: actions
            var br = Panel(root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-384, 20), new Vector2(360, 160));
            Btn(br, "发动 (Enter)", new Vector2(14, -14), new Vector2(160, 60), () => onConfirm?.Invoke(), 24);
            Btn(br, "结束本轮", new Vector2(186, -14), new Vector2(160, 60), () => onEndRound?.Invoke(), 24);
            Btn(br, "撤销 (Esc)", new Vector2(14, -86), new Vector2(160, 60), () => onUndo?.Invoke(), 22);
            trialButton = Btn(br, "试运行 (T)", new Vector2(186, -86), new Vector2(160, 60), () => onTrial?.Invoke(), 22);

            // ---- toast & tooltip
            toast = Text(root, "", 26, Color.white, Vector2.zero, new Vector2(1000, 40));
            toast.alignment = TextAlignmentOptions.Center;
            Anchor(toast.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-500, 190));
            tooltipPanel = Panel(root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(360, 140));
            tooltipPanel.pivot = new Vector2(0, 1);
            tooltip = Text(tooltipPanel, "", 19, Color.white, new Vector2(12, -8), new Vector2(336, 124));
            tooltip.enableWordWrapping = true;
            tooltipPanel.gameObject.SetActive(false);
            foreach (var g in tooltipPanel.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            toast.raycastTarget = false;

            // ---- modal
            modal = Panel(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, 300), new Vector2(840, 600));
            modal.GetComponent<Image>().color = new Color(0.03f, 0.035f, 0.07f, 0.95f);
            modal.pivot = new Vector2(0.5f, 0.5f);
            modal.anchoredPosition = Vector2.zero;
            modalTitle = Text(modal, "", 44, Accent, new Vector2(30, -24), new Vector2(780, 60));
            modalTitle.alignment = TextAlignmentOptions.Center;
            modalBody = Text(modal, "", 24, Color.white, new Vector2(40, -100), new Vector2(760, 400));
            modalBody.enableWordWrapping = true;
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
            img.color = new Color(0.16f, 0.2f, 0.32f, 0.95f);
            var b = go.GetComponent<Button>();
            var cb = b.colors;
            cb.highlightedColor = new Color(1.3f, 1.3f, 1.4f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
            b.colors = cb;
            b.onClick.AddListener(() => onClick?.Invoke());
            var t = Text(rt, label, fontSize, Color.white, Vector2.zero, size);
            t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = Vector2.zero; t.rectTransform.offsetMax = Vector2.zero;
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
            for (int i = rt.childCount - 1; i >= keep; i--) Destroy(rt.GetChild(i).gameObject);
        }

        // ------------------------------------------------------------------ refresh

        public void Refresh(RunState s, bool playing)
        {
            var cfg = s.cfg;
            stageText.text = $"{cfg.stageNames[s.stage]} · 第 {s.roundInStage + 1} 轮{(s.IsBossRound ? "（关口）" : "")}";
            targetText.text = $"目标 {s.Target}    第 {s.RoundIndex + 1}/18 轮";
            var gate = s.StageGate;
            gateText.text = gate != null ? $"关口「{gate.name}」{(s.IsBossRound ? "生效中" : "将在第 3 轮生效")}：{gate.desc}" : (s.IsBossRound ? "关口轮：目标为基准 2 倍" : "");
            if (!playing) SetScore(s.roundLight, s.roundAddMult, s);
            startsText.text = $"起点次数  {s.startsLeft}";
            movesText.text = $"移动 {s.movesLeft}    {(s.placedThisRound ? "本轮已放置" : "可放置 1 个")}";
            coinsText.text = $"余光 {s.coins}" + (cfg.insightEnabled ? $"    领悟 {s.insight}" : "");
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
                var b = Btn(candidateBar, $"{def.name}\n<size=15>阈值 {def.threshold / 2f:0.#}  光量 {def.light}  输出 {def.output / 2f:0.#}</size>", new Vector2(16 + i * 190, -36), new Vector2(180, 72), () => onCandidate?.Invoke(idx), 24);
                var img = b.GetComponent<Image>();
                var sc = SparkView.ShapeColor(shape, false);
                img.color = idx == selectedCandidate ? new Color(sc.r * 0.6f, sc.g * 0.6f, sc.b * 0.6f, 1f) : new Color(sc.r * 0.22f, sc.g * 0.22f, sc.b * 0.3f, 0.95f);
                candidateButtons.Add(b);
            }
            if (s.candidates.Count > 0) Btn(candidateBar, "放弃", new Vector2(600, -36), new Vector2(140, 72), () => onSkipCandidates?.Invoke(), 24);
            else Text(candidateBar, s.placedThisRound ? "本轮已放置或放弃" : "", 20, new Color(1, 1, 1, 0.5f), new Vector2(16, -50), new Vector2(500, 30));

            // items
            ClearChildren(itemsPanel);
            Text(itemsPanel, $"物件 {s.items.Count}/{s.ItemSlots}", 22, Accent, new Vector2(14, -8), new Vector2(230, 30));
            float y = -40;
            for (int i = 0; i < s.items.Count; i++)
            {
                var d = EffectDef.Get(s.items[i]);
                var t = Text(itemsPanel, $"<b>{d.name}</b>  <size=13><color=#b8c0d0>{d.desc}</color></size>", 17, Color.white, new Vector2(14, y), new Vector2(236, 60));
                t.enableWordWrapping = true;
                t.ForceMeshUpdate();
                y -= Mathf.Max(26, t.preferredHeight + 6);
            }
            itemsPanel.sizeDelta = new Vector2(260, Mathf.Max(80, -y + 10));
            itemsPanel.gameObject.SetActive(s.ItemSlots > 0 || s.items.Count > 0);

            ClearChildren(drugsPanel);
            Text(drugsPanel, $"药品 {s.drugs.Count}/{cfg.drugCarryMax}（每轮 1 个）", 20, Accent, new Vector2(14, -8), new Vector2(240, 30));
            for (int i = 0; i < s.drugs.Count; i++)
            {
                int idx = i;
                var d = EffectDef.Get(s.drugs[i]);
                Btn(drugsPanel, (d.warning ? "！" : "") + d.name, new Vector2(14 + i * 120, -44), new Vector2(112, 40), () => onUseDrug?.Invoke(idx), 18);
            }
            drugsPanel.gameObject.SetActive(s.drugs.Count > 0 || s.stage >= cfg.shopFromStage);

            // shop
            ClearChildren(shopPanel);
            shopPanel.gameObject.SetActive(s.shop.Count > 0);
            Text(shopPanel, "商店", 22, Accent, new Vector2(14, -8), new Vector2(100, 30));
            if (s.shop.Count > 0) Btn(shopPanel, $"刷新 {s.ShopRerollCost}", new Vector2(130, -6), new Vector2(116, 32), () => onRefreshShop?.Invoke(), 16);
            float shopY = -44;
            for (int i = 0; i < s.shop.Count; i++)
            {
                int idx = i;
                var o = s.shop[i];
                var d = EffectDef.Get(o.id);
                var b = Btn(shopPanel, o.sold ? $"{d.name}（已售）" : $"{(d.warning ? "！" : "")}{d.name}  {s.PriceOf(o)} 余光", new Vector2(14, shopY), new Vector2(232, 38), () => onBuy?.Invoke(idx), 18);
                b.interactable = !o.sold;
                var t = Text(shopPanel, d.desc, 13, new Color(1, 1, 1, 0.65f), new Vector2(14, shopY - 42), new Vector2(232, 40));
                t.enableWordWrapping = true;
                t.ForceMeshUpdate();
                shopY -= 48 + Mathf.Max(16, t.preferredHeight);
            }
            shopPanel.sizeDelta = new Vector2(260, -shopY + 8);
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
            progressFill.rectTransform.sizeDelta = new Vector2(620 * k, 14);
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
            if (string.IsNullOrEmpty(text)) { tooltipPanel.gameObject.SetActive(false); return; }
            tooltipPanel.gameObject.SetActive(true);
            tooltip.text = text;
            tooltip.ForceMeshUpdate();
            float h = Mathf.Max(60, tooltip.preferredHeight + 20);
            tooltipPanel.sizeDelta = new Vector2(360, h);
            tooltip.rectTransform.sizeDelta = new Vector2(336, h - 16);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, null, out var local);
            var size = root.rect.size;
            local += size / 2f;
            local += new Vector2(22, -22);
            if (local.x + 360 > size.x) local.x -= 400;
            if (local.y - h < 0) local.y += h + 30;
            tooltipPanel.anchoredPosition = local;
        }

        public void ShowModal(string title, string body, string b1, Action cb1, string b2 = null, Action cb2 = null)
        {
            modal.gameObject.SetActive(true);
            modal.SetAsLastSibling();
            modalTitle.text = title;
            modalBody.text = body;
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
