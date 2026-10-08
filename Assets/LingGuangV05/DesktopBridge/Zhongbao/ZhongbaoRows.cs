using System.Collections.Generic;
using System.Text;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.Zhongbao
{
    /// <summary>
    /// A pooled list of label / value rows for 摆渡众包's boxes (the mockup's .row): a label on the left, a bold value on
    /// the right, a thin dashed rule underneath. Rows wrap when the label is long. The list stays cheap to refresh: when
    /// the items did not change since the last call nothing is touched.
    /// </summary>
    internal sealed class ZhongbaoRows
    {
        public enum Kind
        {
            /// <summary>label … value, dashed rule below.</summary>
            Row,
            /// <summary>a small muted line indented under a row (no rule).</summary>
            Sub,
            /// <summary>a section title on a pale band.</summary>
            Head,
            /// <summary>a bold row without a rule (a total).</summary>
            Total,
            /// <summary>a muted paragraph across the full width.</summary>
            Note,
        }

        public struct Item
        {
            public Kind kind;
            public string label, value;
            public Color? ink;
        }

        public static Item Row(string label, string value, Color? ink = null) => new Item { kind = Kind.Row, label = label, value = value, ink = ink };
        public static Item Sub(string label, string value = "", Color? ink = null) => new Item { kind = Kind.Sub, label = label, value = value, ink = ink };
        public static Item Head(string label) => new Item { kind = Kind.Head, label = label, value = "" };
        public static Item Total(string label, string value, Color? ink = null) => new Item { kind = Kind.Total, label = label, value = value, ink = ink };
        public static Item Note(string text, Color? ink = null) => new Item { kind = Kind.Note, label = text, value = "", ink = ink };

        sealed class Line
        {
            public RectTransform rt, rule;
            public Image band;
            public TMP_Text label, value;
        }

        const float PadX = 12, Gap = 12;
        static readonly Color Body = new Color32(85, 85, 85, 255);

        readonly XgUi ui;
        readonly RectTransform host;
        readonly List<Line> lines = new List<Line>();
        readonly StringBuilder key = new StringBuilder();
        string lastKey = "";
        float lastHeight, lastWidth;

        public ZhongbaoRows(XgUi ui, RectTransform host) { this.ui = ui; this.host = host; }

        Line Get(int i)
        {
            while (lines.Count <= i)
            {
                var l = new Line { rt = Rect("Line", host, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero) };
                l.rt.pivot = new Vector2(.5f, 1);
                l.band = Panel(l.rt, Color.clear); l.band.raycastTarget = false;
                l.label = ui.Text(Rect("Label", l.rt, Vector2.zero, Vector2.one, new Vector2(PadX, 0), new Vector2(-PadX, 0)), "", 13, Body, TextAlignmentOptions.MidlineLeft);
                l.value = ui.Text(Rect("Value", l.rt, Vector2.zero, Vector2.one, new Vector2(PadX, 0), new Vector2(-PadX, 0)), "", 13, ZhongbaoSkin.Ink, TextAlignmentOptions.MidlineRight);
                l.value.textWrappingMode = TextWrappingModes.NoWrap; l.value.fontStyle = FontStyles.Bold;
                l.rule = Rect("Rule", l.rt, Vector2.zero, new Vector2(1, 0), new Vector2(PadX, 0), new Vector2(-PadX, 1));
                var rule = l.rule;
                ZhongbaoSkin.Canvas(rule, vh => { var r = rule.rect; XgDraw.Dashed(vh, new Vector2(r.xMin, r.center.y), new Vector2(r.xMax, r.center.y), 1, 3, ZhongbaoSkin.Line); });
                lines.Add(l);
            }
            return lines[i];
        }

        /// <summary>Shows the items top to bottom and returns the height used (so a scroll list can size itself).</summary>
        public float Set(IList<Item> items)
        {
            float width = host.rect.width;
            if (width <= 1) width = 420;
            key.Length = 0;
            key.Append((int)width).Append('#');
            foreach (var it in items) key.Append((int)it.kind).Append('|').Append(it.label).Append('|').Append(it.value).Append('|').Append(it.ink.HasValue ? ZhongbaoSkin.Hex(it.ink.Value) : "").Append('\n');
            string k = key.ToString();
            if (k == lastKey && Mathf.Approximately(width, lastWidth)) return lastHeight;
            lastKey = k; lastWidth = width;

            float y = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var l = Get(i);
                if (!l.rt.gameObject.activeSelf) l.rt.gameObject.SetActive(true);
                float size = 13, minH = 30, padLeft = PadX, top = 0;
                Color labelInk = Body, valueInk = it.ink ?? ZhongbaoSkin.Ink;
                bool bold = false, rule = it.kind == Kind.Row, hasValue = !string.IsNullOrEmpty(it.value);
                switch (it.kind)
                {
                    case Kind.Sub: size = 12; minH = 22; padLeft = PadX + 14; labelInk = ZhongbaoSkin.Mute; if (!it.ink.HasValue) valueInk = ZhongbaoSkin.Mute; break;
                    case Kind.Head: size = 13; minH = 30; top = i == 0 ? 0 : 4; labelInk = ZhongbaoSkin.Ink; bold = true; break;
                    case Kind.Total: size = 14; minH = 34; labelInk = ZhongbaoSkin.Ink; bold = true; break;
                    case Kind.Note: size = 12; minH = 22; labelInk = it.ink ?? ZhongbaoSkin.Mute; break;
                }
                y += top;
                l.band.color = it.kind == Kind.Head ? ZhongbaoSkin.Page : Color.clear;
                l.label.fontSize = size; l.label.color = labelInk; l.label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
                if (l.label.text != it.label) l.label.text = it.label;
                l.value.fontSize = size; l.value.color = valueInk; l.value.fontStyle = it.kind == Kind.Sub ? FontStyles.Normal : FontStyles.Bold;
                if (l.value.text != it.value) l.value.text = it.value;
                l.value.gameObject.SetActive(hasValue);
                float valueW = hasValue ? l.value.GetPreferredValues(it.value).x : 0;
                float labelW = Mathf.Max(60, width - padLeft - PadX - (hasValue ? valueW + Gap : 0));
                var labelRt = (RectTransform)l.label.transform;
                labelRt.offsetMin = new Vector2(padLeft, 0); labelRt.offsetMax = new Vector2(-PadX - (hasValue ? valueW + Gap : 0), 0);
                l.label.alignment = TextAlignmentOptions.MidlineLeft;
                float h = Mathf.Max(minH, l.label.GetPreferredValues(it.label, labelW, 0).y + 12);
                l.rule.gameObject.SetActive(rule);
                l.rt.offsetMin = new Vector2(0, -y - h); l.rt.offsetMax = new Vector2(0, -y);
                y += h;
            }
            for (int i = items.Count; i < lines.Count; i++) if (lines[i].rt.gameObject.activeSelf) lines[i].rt.gameObject.SetActive(false);
            lastHeight = y + 6;
            return lastHeight;
        }
    }
}
