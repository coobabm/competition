using System;
using System.Collections.Generic;
using LingGuangV05.Core.Chat;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.YY
{
    /// <summary>
    /// The 2016-style 表情 panel that pops up above the YY input: a 默认 tab (recently used row, then every face in a
    /// grid) and a 斗图 tab of original meme stickers. Picking a face hands it to <see cref="onFace"/> (the view
    /// inserts its code at the caret); picking a sticker sends it at once. Clicking outside closes the panel.
    /// </summary>
    public sealed class YYFacePicker : MonoBehaviour
    {
        const int Columns = 12, RecentMax = Columns;
        const float CellSize = 30, Pad = 8, TabHeight = 28, StickerCell = 84;
        public const float Width = Pad * 2 + Columns * CellSize, Height = 300;
        const string RecentKey = "LingGuangV05.YY.RecentFaces";

        static readonly Color PanelColor = new Color32(255, 255, 255, 255);
        static readonly Color Border = new Color32(196, 208, 222, 255);
        static readonly Color TabBar = new Color32(240, 244, 249, 255);
        static readonly Color TabOn = new Color32(255, 255, 255, 255);
        static readonly Color Ink = new Color32(34, 40, 52, 255);
        static readonly Color Muted = new Color32(128, 138, 154, 255);
        static readonly Color Hover = new Color32(214, 233, 252, 255);

        RectTransform blocker, panel, body;
        TMP_FontAsset font;
        Func<DateTime> today;
        Action<YYFace> onFace;
        Action<YYSticker> onSticker;
        Action<GameObject> focus;
        bool stickersTab;

        public bool IsOpen => panel != null && panel.gameObject.activeSelf;

        /// <summary>Builds the (hidden) picker over <paramref name="host"/>; the panel's bottom-left sits at <paramref name="bottomLeft"/>.</summary>
        public static YYFacePicker Create(RectTransform host, Vector2 bottomLeft, TMP_FontAsset font, Func<DateTime> today,
            Action<YYFace> onFace, Action<YYSticker> onSticker, Action<GameObject> focus)
        {
            var go = PrologueDesk.Rect("YY Face Picker", host, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var picker = go.AddComponent<YYFacePicker>();
            picker.font = font; picker.today = today; picker.onFace = onFace; picker.onSticker = onSticker; picker.focus = focus;
            picker.blocker = (RectTransform)go.transform;
            var hit = go.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0);
            var close = go.AddComponent<Button>(); close.transition = Selectable.Transition.None;
            close.onClick.AddListener(picker.Hide);
            focus?.Invoke(go);
            picker.panel = PrologueDesk.Rect("Panel", go.transform, Vector2.zero, Vector2.zero, bottomLeft, bottomLeft + new Vector2(Width, Height));
            var shape = picker.panel.gameObject.AddComponent<YYRoundRect>();
            shape.radius = 4; shape.color = PanelColor; shape.border = 1; shape.borderColor = Border;
            // The panel swallows clicks so they do not reach the blocker behind it.
            picker.panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            go.SetActive(false);
            return picker;
        }

        public void Toggle() { if (gameObject.activeSelf) Hide(); else Show(); }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Rebuild();
        }

        public void Hide() { gameObject.SetActive(false); }

        void Rebuild()
        {
            for (int i = panel.childCount - 1; i >= 0; i--) Destroy(panel.GetChild(i).gameObject);
            body = PrologueDesk.Rect("Body", panel, Vector2.zero, Vector2.one, new Vector2(0, TabHeight), Vector2.zero);
            if (stickersTab) BuildStickers(); else BuildFaces();
            BuildTabs();
        }

        // ───────────── 默认 tab ─────────────

        void BuildFaces()
        {
            float y = Pad;
            Label(body, GameText.T("最近使用", "Recently used"), Pad, y, 12, Muted);
            y += 18;
            var recent = Recent();
            if (recent.Count == 0) Label(body, GameText.T("（还没有。点一个表情试试）", "(None yet. Pick a face below.)"), Pad + 4, y + 6, 12, Muted);
            for (int i = 0; i < recent.Count; i++) FaceCell(recent[i], Pad + i * CellSize, y);
            y += CellSize + 6;
            var line = PrologueDesk.Rect("Divider", body, new Vector2(0, 1), new Vector2(1, 1), new Vector2(Pad, -y), new Vector2(-Pad, -y + 1));
            PrologueDesk.Fill(line, Border, false);
            y += 4;
            Label(body, GameText.T("默认表情", "Default faces"), Pad, y, 12, Muted);
            y += 18;
            for (int i = 0; i < YYFaces.Count; i++) FaceCell(YYFaces.At(i), Pad + (i % Columns) * CellSize, y + (i / Columns) * CellSize);
        }

        void FaceCell(YYFace face, float x, float y)
        {
            var cell = PrologueDesk.Rect("Face_" + face.Id, body, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - CellSize), new Vector2(x + CellSize, -y));
            var bg = cell.gameObject.AddComponent<Image>(); bg.color = new Color(1, 1, 1, 0);
            var button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors; colors.normalColor = new Color(1, 1, 1, 0); colors.highlightedColor = Hover; colors.pressedColor = Hover; colors.selectedColor = new Color(1, 1, 1, 0);
            button.colors = colors;
            button.onClick.AddListener(() => Pick(face));
            var icon = PrologueDesk.Rect("Icon", cell, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            var img = icon.gameObject.AddComponent<Image>(); img.sprite = YYFaceRenderer.SpriteFor(face); img.preserveAspect = true; img.raycastTarget = false;
            UiTip.Add(cell, () => face.Name(GameText.IsEnglish) + "  <color=#9AA4B2>" + (GameText.IsEnglish ? face.EnAlias : face.Code) + "</color>");
            focus?.Invoke(cell.gameObject);
        }

        void Pick(YYFace face)
        {
            Remember(face);
            Hide();
            onFace?.Invoke(face);
        }

        // ───────────── 斗图 tab ─────────────

        void BuildStickers()
        {
            var list = YYStickers.AvailableOn(today != null ? today() : DateTime.MaxValue);
            const int cols = 4;
            float gap = (Width - Pad * 2 - cols * StickerCell) / (cols - 1);
            for (int i = 0; i < list.Count; i++)
            {
                var sticker = list[i];
                float x = Pad + (i % cols) * (StickerCell + gap), y = Pad + (i / cols) * (StickerCell + 4);
                var cell = PrologueDesk.Rect("Sticker_" + sticker.Id, body, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - StickerCell), new Vector2(x + StickerCell, -y));
                var bg = cell.gameObject.AddComponent<Image>(); bg.color = new Color(1, 1, 1, 0);
                var button = cell.gameObject.AddComponent<Button>(); button.targetGraphic = bg;
                var colors = button.colors; colors.normalColor = new Color(1, 1, 1, 0); colors.highlightedColor = Hover; colors.pressedColor = Hover; colors.selectedColor = new Color(1, 1, 1, 0);
                button.colors = colors;
                button.onClick.AddListener(() => { Hide(); onSticker?.Invoke(sticker); });
                StickerView(cell, sticker, font, 2);
                UiTip.Add(cell, () => GameText.T("斗图：", "Sticker: ") + sticker.Caption(GameText.IsEnglish) + GameText.T("\n<color=#9AA4B2>点一下直接发送</color>", "\n<color=#9AA4B2>Click to send</color>"));
                focus?.Invoke(cell.gameObject);
            }
        }

        /// <summary>A sticker picture with its bold caption, filling <paramref name="parent"/> (inset by <paramref name="inset"/>).</summary>
        public static void StickerView(RectTransform parent, YYSticker sticker, TMP_FontAsset font, float inset)
        {
            var pic = PrologueDesk.Rect("Picture", parent, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
            var img = pic.gameObject.AddComponent<Image>(); img.sprite = YYFaceRenderer.StickerSprite(sticker); img.raycastTarget = false;
            // The picture leaves its lower third empty for the caption.
            var cap = PrologueDesk.Rect("Caption", pic, Vector2.zero, new Vector2(1, .4f), new Vector2(2, 2), new Vector2(-2, 0));
            var t = cap.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = sticker.Caption(GameText.IsEnglish);
            t.fontStyle = FontStyles.Bold; t.color = new Color32(20, 20, 20, 255); t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = 15; t.lineSpacing = -20; t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow; t.raycastTarget = false; t.richText = false;
        }

        // ───────────── tabs ─────────────

        void BuildTabs()
        {
            var bar = PrologueDesk.Rect("Tabs", panel, Vector2.zero, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-1, TabHeight));
            PrologueDesk.Fill(bar, TabBar, false);
            Tab(bar, 0, GameText.T("☺ 默认", "☺ Faces"), !stickersTab, () => { stickersTab = false; Rebuild(); });
            Tab(bar, 1, GameText.T("斗图", "Stickers"), stickersTab, () => { stickersTab = true; Rebuild(); });
        }

        void Tab(RectTransform bar, int index, string label, bool on, Action click)
        {
            const float w = 78;
            var rt = PrologueDesk.Rect("Tab" + index, bar, new Vector2(0, 0), new Vector2(0, 1), new Vector2(4 + index * (w + 2), 0), new Vector2(4 + index * (w + 2) + w, 0));
            var bg = rt.gameObject.AddComponent<Image>(); bg.color = on ? TabOn : new Color(1, 1, 1, 0);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = bg;
            b.onClick.AddListener(() => click());
            var t = Label(rt, label, 0, 0, 13, on ? Ink : Muted);
            var trt = t.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            t.alignment = TextAlignmentOptions.Center;
            focus?.Invoke(rt.gameObject);
        }

        TMP_Text Label(RectTransform parent, string text, float x, float y, float size, Color color)
        {
            var rt = PrologueDesk.Rect("Label", parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(x, -y - 18), new Vector2(-Pad, -y));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.MidlineLeft;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        // ───────────── recently used ─────────────

        static List<string> recentIds;

        public static List<YYFace> Recent()
        {
            if (recentIds == null)
            {
                recentIds = new List<string>();
                foreach (var id in PlayerPrefs.GetString(RecentKey, "").Split(','))
                    if (YYFaces.ById(id) != null && !recentIds.Contains(id)) recentIds.Add(id);
            }
            var list = new List<YYFace>();
            foreach (var id in recentIds) { var f = YYFaces.ById(id); if (f != null) list.Add(f); }
            return list;
        }

        public static void Remember(YYFace face)
        {
            if (face == null) return;
            Recent();
            recentIds.Remove(face.Id);
            recentIds.Insert(0, face.Id);
            if (recentIds.Count > RecentMax) recentIds.RemoveRange(RecentMax, recentIds.Count - RecentMax);
            PlayerPrefs.SetString(RecentKey, string.Join(",", recentIds));
        }
    }
}
