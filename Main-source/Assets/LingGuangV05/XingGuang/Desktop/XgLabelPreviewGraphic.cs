using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Renders the actual 5x5 card bitmaps, top row first. No answer or model weights enter this graphic.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgLabelPreviewGraphic : UnityEngine.UI.MaskableGraphic
    {
        string first = "", second = "";
        public string PatternA => first;
        public string PatternB => second;
        public bool HasTwoPatterns => Valid(second);

        public void Show(string a, string b)
        {
            a = a ?? ""; b = b ?? "";
            if (a == first && b == second) return;
            first = a; second = b; SetVerticesDirty();
        }
        public static bool Valid(string pattern)
        {
            if (pattern == null || pattern.Length != 25) return false;
            foreach (char cell in pattern) if (cell != '0' && cell != '1') return false;
            return true;
        }
        public static bool Lit(string pattern, int column, int row)
            => Valid(pattern) && column >= 0 && column < 5 && row >= 0 && row < 5 && pattern[row * 5 + column] == '1';

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            if (!Valid(first)) return;
            var r = GetPixelAdjustedRect();
            bool two = HasTwoPatterns;
            if (r.width < (two ? 54 : 12) || r.height < 12) return;
            float size = Mathf.Min(r.height - 6, two ? (r.width - 42) * .5f : r.width - 6);
            var a = new Rect(two ? r.center.x - size - 21 : r.center.x - size * .5f, r.center.y - size * .5f, size, size);
            Draw(vh, a, first, XgPalette.Accent);
            if (!two) return;
            var b = new Rect(r.center.x + 21, a.yMin, size, size);
            Draw(vh, b, second, new Color32(18, 135, 125, 255));
            Vector2 from = new Vector2(a.xMax + 7, r.center.y), to = new Vector2(b.xMin - 7, r.center.y);
            XgDraw.Seg(vh, from, to, 2, XgPalette.Muted);
            XgDraw.Seg(vh, to, to + new Vector2(-6, 5), 2, XgPalette.Muted);
            XgDraw.Seg(vh, to, to + new Vector2(-6, -5), 2, XgPalette.Muted);
        }
        static void Draw(UnityEngine.UI.VertexHelper vh, Rect rect, string pattern, Color on)
        {
            XgDraw.Box(vh, rect.min - Vector2.one, rect.max + Vector2.one, XgPalette.Line);
            float cell = rect.width / 5, gap = Mathf.Max(1, cell * .07f);
            for (int row = 0; row < 5; row++) for (int column = 0; column < 5; column++)
            {
                var min = new Vector2(rect.xMin + column * cell, rect.yMax - (row + 1) * cell);
                XgDraw.Box(vh, min + Vector2.one * gap, min + Vector2.one * (cell - gap), pattern[row * 5 + column] == '1' ? on : Color.white);
            }
        }
    }
}
