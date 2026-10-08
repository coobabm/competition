using System;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.Zhongbao
{
    /// <summary>
    /// The look of 摆渡众包 (the approved redesign, Documentation/LingGuang-Incremental/zhongbao-redesign/index.html): a
    /// 2016 Chinese web platform. White boxes on a pale grey page, thin grey rules, the platform blue for the header and
    /// links, small red badges and red money. Colours and the few building blocks every 摆渡众包 page shares.
    /// </summary>
    public static class ZhongbaoSkin
    {
        public static readonly Color Blue = new Color32(41, 50, 225, 255);
        public static readonly Color Blue2 = new Color32(78, 110, 242, 255);
        public static readonly Color Blue3 = new Color32(238, 242, 255, 255);
        public static readonly Color Ink = new Color32(34, 34, 34, 255);
        public static readonly Color Mute = new Color32(122, 127, 140, 255);
        public static readonly Color Dim = new Color32(180, 184, 194, 255);
        public static readonly Color Line = new Color32(227, 230, 236, 255);
        public static readonly Color Page = new Color32(244, 245, 247, 255);
        public static readonly Color Red = new Color32(227, 62, 51, 255);
        public static readonly Color RedSoft = new Color32(255, 242, 241, 255);
        public static readonly Color RedLine = new Color32(243, 180, 175, 255);
        public static readonly Color Green = new Color32(47, 158, 68, 255);
        public static readonly Color GreenSoft = new Color32(240, 250, 242, 255);
        public static readonly Color GreenLine = new Color32(168, 216, 180, 255);
        public static readonly Color Gold = new Color32(232, 163, 23, 255);
        public static readonly Color Orange = new Color32(242, 123, 29, 255);
        public static readonly Color OrangeSoft = new Color32(255, 246, 238, 255);
        public static readonly Color Track = new Color32(236, 238, 242, 255);
        /// <summary>The spider's colour on the platform (its panel, its bubble, the reaching hand).</summary>
        public static readonly Color Spider = new Color32(138, 120, 232, 255);
        public static readonly Color SpiderSoft = new Color32(243, 241, 255, 255);
        public static readonly Color SpiderLine = new Color32(220, 214, 251, 255);

        /// <summary>The height of a box's title strip.</summary>
        public const float HeaderHeight = 34;

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        /// <summary>A white box with a thin grey rim (the mockup's .box).</summary>
        public static RectTransform Box(XgUi ui, Transform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var border = Rect(name, parent, min, max, offMin, offMax);
            Panel(border, Line);
            var inner = Rect("Inner", border, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            Panel(inner, Color.white).raycastTarget = false;
            return border;
        }

        /// <summary>The title strip of a box: bold title on the left, a muted note on the right, a rule underneath.</summary>
        public static void Header(XgUi ui, RectTransform box, string zh, string en, out TMP_Text title, out TMP_Text note)
        {
            var strip = Rect("Header", box, new Vector2(0, 1), Vector2.one, new Vector2(1, -HeaderHeight), new Vector2(-1, -1));
            Hairline(strip, 0, false);
            title = ui.Text(Rect("Title", strip, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-12, 0)), T(zh, en), 15, Ink, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            note = ui.Text(Rect("Note", strip, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-12, 0)), "", 12, Mute, TextAlignmentOptions.MidlineRight);
            note.textWrappingMode = TextWrappingModes.NoWrap; note.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>A one-pixel rule across the parent (at the bottom, or at the top when <paramref name="top"/>).</summary>
        public static Image Hairline(RectTransform parent, float inset, bool top)
        {
            var r = top
                ? Rect("Rule", parent, new Vector2(0, 1), Vector2.one, new Vector2(inset, -1), new Vector2(-inset, 0))
                : Rect("Rule", parent, Vector2.zero, new Vector2(1, 0), new Vector2(inset, 0), new Vector2(-inset, 1));
            var img = Panel(r, Line); img.raycastTarget = false;
            return img;
        }

        /// <summary>A small bordered tag such as 高价 / 新 / 已签约. Returns the tag's rect (put it where you like).</summary>
        public static RectTransform Tag(XgUi ui, Transform parent, string name, string text, Color ink, Color back, Color rim, float width, float height = 17)
        {
            var r = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(width, height));
            r.pivot = Vector2.zero;
            Panel(r, rim).raycastTarget = false;
            var inner = Rect("Inner", r, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            Panel(inner, back).raycastTarget = false;
            var t = ui.Text(Rect("Text", inner, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), text, 11, ink, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return r;
        }

        /// <summary>The platform's primary button look: solid blue, white text.</summary>
        public static void Primary(XgBtn b, string text, bool enabled) => b.Set(text, enabled, enabled ? Blue : (Color?)null, enabled ? Color.white : (Color?)null);

        /// <summary>The outlined look: white, blue text.</summary>
        public static void Ghost(XgBtn b, string text, bool enabled) => b.Set(text, enabled, Color.white, Blue);

        /// <summary>A red badge: a filled red pill with a short white count or word, hidden when the text is empty.</summary>
        public static TMP_Text Badge(XgUi ui, Transform parent, string name, Vector2 anchor, Vector2 center, float width, float height = 17)
        {
            var r = Rect(name, parent, anchor, anchor, center - new Vector2(width / 2, height / 2), center + new Vector2(width / 2, height / 2));
            Panel(r, Red).raycastTarget = false;
            var t = ui.Text(Rect("Text", r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 11, Color.white, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>Shows or hides a badge by its text (the badge's background goes with it).</summary>
        public static void SetBadge(TMP_Text badge, string text)
        {
            if (badge == null) return;
            badge.text = text ?? "";
            var go = badge.transform.parent.gameObject;
            bool on = !string.IsNullOrEmpty(text);
            if (go.activeSelf != on) go.SetActive(on);
        }

        /// <summary>A box with its own rim and fill (the red-tinted debt box, a pale-blue note); same shape as <see cref="Box"/>.</summary>
        public static RectTransform TintBox(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color rim, Color fill)
        {
            var border = Rect(name, parent, min, max, offMin, offMax);
            Panel(border, rim);
            var inner = Rect("Inner", border, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            Panel(inner, fill).raycastTarget = false;
            return border;
        }

        /// <summary>
        /// A button with a one-pixel rim (white boxes have no button shadow, so a plain ghost button would be invisible).
        /// The returned button sits inside a frame: place <see cref="Frame"/>, not <c>b.rt</c>. Style it with
        /// <see cref="Ghost"/> / <see cref="Primary"/> and recolour the rim with <see cref="SetRim"/>.
        /// </summary>
        public static XgBtn Outlined(XgUi ui, Transform parent, string label, UnityAction onClick, float size, Color rim)
        {
            var frame = Rect("Frame", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Panel(frame, rim).raycastTarget = false;
            var b = ui.Button(frame, label, onClick, size);
            b.rt.anchorMin = Vector2.zero; b.rt.anchorMax = Vector2.one; b.rt.offsetMin = new Vector2(1, 1); b.rt.offsetMax = new Vector2(-1, -1);
            return b;
        }

        public static RectTransform Frame(XgBtn b) => b.rt.parent as RectTransform;

        public static void SetRim(XgBtn b, Color rim) { var f = Frame(b); if (f != null) { var img = f.GetComponent<Image>(); if (img != null) img.color = rim; } }

        /// <summary>A filled disc (an avatar face) drawn in the rect's largest inscribed circle.</summary>
        public static XgCrawlerLayer Disc(RectTransform rt, Color color)
            => Canvas(rt, vh => { var r = rt.rect; XgDraw.Disc(vh, r.center, Mathf.Min(r.width, r.height) * .5f, color, 36); });

        /// <summary>A code-drawn graphic (sparkline, dial) whose owner fills the mesh; never takes input.</summary>
        public static XgCrawlerLayer Canvas(RectTransform rt, Action<VertexHelper> draw)
        {
            var g = rt.gameObject.AddComponent<XgCrawlerLayer>();
            g.raycastTarget = false; g.color = Color.white; g.draw = draw;
            return g;
        }
    }
}
