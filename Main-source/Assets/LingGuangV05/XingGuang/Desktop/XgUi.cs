using System;
using System.Globalization;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Colours of 灵光.exe.</summary>
    public static class XgPalette
    {
        public static readonly Color Page = new Color32(238, 242, 248, 255);
        public static readonly Color Card = Color.white;
        public static readonly Color Line = new Color32(213, 222, 234, 255);
        public static readonly Color Hud = new Color32(27, 35, 72, 255);
        public static readonly Color HudChip = new Color32(43, 54, 104, 255);
        public static readonly Color Ink = new Color32(30, 38, 56, 255);
        public static readonly Color Muted = new Color32(104, 116, 140, 255);
        public static readonly Color Accent = new Color32(59, 91, 219, 255);
        public static readonly Color AccentSoft = new Color32(228, 234, 255, 255);
        public static readonly Color Money = new Color32(232, 110, 20, 255);
        public static readonly Color Good = new Color32(47, 158, 68, 255);
        public static readonly Color Bad = new Color32(214, 48, 49, 255);
        public static readonly Color Button = new Color32(241, 244, 250, 255);
        public static readonly Color Disabled = new Color32(226, 229, 236, 255);
        public static readonly Color Star = new Color32(255, 214, 102, 255);
        public static readonly Color Gold = new Color32(255, 190, 40, 255);
        public static readonly Color Paper = new Color32(250, 248, 240, 255);
        public static readonly Color[] Grades = { new Color32(140, 150, 170, 255), new Color32(64, 160, 110, 255), new Color32(59, 120, 219, 255), new Color32(150, 80, 220, 255), new Color32(240, 170, 20, 255) };
        public static readonly Color[] Tiers = { Color.white, new Color32(120, 230, 150, 255), new Color32(255, 180, 80, 255), new Color32(255, 90, 90, 255), new Color32(255, 214, 60, 255) };
    }

    public sealed class XgBtn
    {
        public Button button; public Image image; public TMP_Text label; public RectTransform rt;
        public void Set(string text, bool interactable, Color? fill = null, Color? ink = null)
        {
            if (label.text != text) label.text = text;
            button.interactable = interactable;
            image.color = interactable ? (fill ?? XgPalette.Button) : XgPalette.Disabled;
            label.color = interactable ? (ink ?? XgPalette.Ink) : XgPalette.Muted;
        }
        public void Show(bool on) { if (button.gameObject.activeSelf != on) button.gameObject.SetActive(on); }
    }

    /// <summary>Press-and-hold on a button: fires <see cref="Down"/> / <see cref="Up"/>; a drag passes through to the parent.</summary>
    public sealed class XgHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action Down, Up;
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e) { if (e.button != PointerEventData.InputButton.Left) return; Held = true; Down?.Invoke(); }
        public void OnPointerUp(PointerEventData e) { if (!Held) return; Held = false; Up?.Invoke(); }
        public void OnPointerExit(PointerEventData e) { if (!Held) return; Held = false; Up?.Invoke(); }
        void OnDisable() { if (Held) { Held = false; Up?.Invoke(); } }
    }

    /// <summary>Tiny uGUI kit used by every 灵光 page (rects, panels, cards, text, buttons).</summary>
    public sealed class XgUi
    {
        public readonly TMP_FontAsset font;
        public readonly WindowManager window;
        public XgUi(TMP_FontAsset font, WindowManager window) { this.font = font; this.window = window; }

        public static bool En => GameText.IsEnglish;
        public static string T(string zh, string en) => GameText.T(zh, en);
        public static string N(double v, string f) => v.ToString(f, CultureInfo.InvariantCulture);

        public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        /// <summary>Rect anchored to the parent's top-left corner: x, y from the top, width, height.</summary>
        public static RectTransform TopLeft(string name, Transform parent, float x, float y, float w, float h)
        { return Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - h), new Vector2(x + w, -y)); }

        /// <summary>Full-width strip y pixels from the top, h tall, with side margins.</summary>
        public static RectTransform Strip(string name, Transform parent, float y, float h, float left = 16, float right = 16)
        { return Rect(name, parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(left, -y - h), new Vector2(-right, -y)); }

        public static Image Panel(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public RectTransform Card(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var border = Rect(name, parent, min, max, offMin, offMax);
            Panel(border, XgPalette.Line);
            var inner = Rect("Inner", border, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            Panel(inner, XgPalette.Card).raycastTarget = false;
            return border;
        }

        public TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            return t;
        }

        public XgBtn Button(Transform parent, string label, UnityAction onClick, float size)
        {
            var rt = Rect("Button", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var img = Panel(rt, XgPalette.Button);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(.93f, .95f, 1f); colors.pressedColor = new Color(.82f, .86f, .96f);
            colors.disabledColor = Color.white; colors.selectedColor = Color.white; colors.fadeDuration = .05f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            if (window != null) rt.gameObject.AddComponent<ChapterOneWindowFocus>().Window = window;
            var text = Text(Rect("Label", rt, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0)), label, size, XgPalette.Ink, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return new XgBtn { button = button, image = img, label = text, rt = rt };
        }

        public static void Place(XgBtn b, RectTransform at) { Copy(at, b.rt); UnityEngine.Object.Destroy(at.gameObject); }

        static void Copy(RectTransform from, RectTransform to)
        { to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax; to.offsetMin = from.offsetMin; to.offsetMax = from.offsetMax; }

        public static void PlaceTopLeft(XgBtn b, float x, float y, float w, float h)
        { b.rt.anchorMin = b.rt.anchorMax = new Vector2(0, 1); b.rt.offsetMin = new Vector2(x, -y - h); b.rt.offsetMax = new Vector2(x + w, -y); }

        public static void PlaceBottomLeft(XgBtn b, float x, float y, float w, float h)
        { b.rt.anchorMin = b.rt.anchorMax = Vector2.zero; b.rt.offsetMin = new Vector2(x, y); b.rt.offsetMax = new Vector2(x + w, y + h); }

        public static void PlaceTopRight(XgBtn b, float fromRight, float fromTop, float w, float h)
        { b.rt.anchorMin = b.rt.anchorMax = Vector2.one; b.rt.offsetMin = new Vector2(-fromRight, -fromTop - h); b.rt.offsetMax = new Vector2(-fromRight + w, -fromTop); }

        public static void PlaceBottom(XgBtn b, float y, float h, float left = 16, float right = 16)
        { b.rt.anchorMin = new Vector2(0, 0); b.rt.anchorMax = new Vector2(1, 0); b.rt.offsetMin = new Vector2(left, y); b.rt.offsetMax = new Vector2(-right, y + h); }

        /// <summary>A horizontal fill bar (anchor-scaled, no sprite needed). Returns the fill rect.</summary>
        public static RectTransform Bar(RectTransform parent, string name, Color back, Color fill)
        {
            Panel(parent, back).raycastTarget = false;
            var f = Rect(name, parent, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            Panel(f, fill).raycastTarget = false;
            return f;
        }

        public static void SetBar(RectTransform fill, float k) { var m = fill.anchorMax; m.x = Mathf.Clamp01(k); fill.anchorMax = m; }

        public static XgGlow Glow(RectTransform behind, Color color, float pad = 6)
        {
            var g = Rect("Glow", behind.parent, behind.anchorMin, behind.anchorMax, behind.offsetMin - new Vector2(pad, pad), behind.offsetMax + new Vector2(pad, pad));
            g.SetSiblingIndex(behind.GetSiblingIndex());
            var img = Panel(g, Color.clear); img.raycastTarget = false;
            var glow = g.gameObject.AddComponent<XgGlow>(); glow.image = img; glow.color = color;
            return glow;
        }

        public static string Money(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "—";
            if (En && Math.Abs(v) >= 1e6) return N(v / 1e6, "0.00") + "M";
            if (En && Math.Abs(v) >= 1e4) return N(v / 1e3, "0.0") + "k";
            if (Math.Abs(v) >= 1e8) return N(v / 1e8, "0.00") + "亿";
            if (Math.Abs(v) >= 1e4) return N(v / 1e4, "0.00") + "万";
            return Math.Abs(v) >= 100 ? N(v, "0") : N(v, "0.0");
        }

        public static string Params(double k) { return k >= 1000 ? N(k / 1000, "0.0") + "M" : N(k, k < 10 ? "0.0" : "0") + "K"; }

        public static string Samples(double n)
        {
            if (En) return n >= 1e6 ? N(n / 1e6, "0.#") + "M" : n >= 1e4 ? N(n / 1e3, "0") + "k" : N(n, "0");
            return n >= 1e6 ? N(n / 1e6, "0.#") + " 百万条" : n >= 1e4 ? N(n / 1e4, "0.#") + " 万条" : N(n, "0") + " 条";
        }

        public static string Hex(Color c) { return "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">"; }
    }
}
