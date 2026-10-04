using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.YY
{
    /// <summary>Rounded rectangle with optional 1px border; used for bubbles, cards and badges.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class YYRoundRect : MaskableGraphic
    {
        public float radius = 8;
        public float border;
        public Color borderColor = Color.clear;
        /// <summary>Optional vertical gradient: top colour (bottom uses <see cref="Graphic.color"/>).</summary>
        public bool gradient;
        public Color top = Color.white;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            if (border > 0 && borderColor.a > 0)
            {
                Fill(vh, r, radius, borderColor, borderColor, false);
                r = new Rect(r.x + border, r.y + border, r.width - border * 2, r.height - border * 2);
                Fill(vh, r, Mathf.Max(0, radius - border), color, top, gradient);
            }
            else Fill(vh, r, radius, color, top, gradient);
        }

        static void Fill(VertexHelper vh, Rect r, float radius, Color bottom, Color top, bool gradient)
        {
            float rad = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            Vector2 c = r.center;
            v.position = c; v.color = gradient ? Color.Lerp(bottom, top, .5f) : bottom; vh.AddVert(v);
            Vector2[] corners = { new Vector2(r.xMax - rad, r.yMax - rad), new Vector2(r.xMin + rad, r.yMax - rad), new Vector2(r.xMin + rad, r.yMin + rad), new Vector2(r.xMax - rad, r.yMin + rad) };
            int seg = 5, count = 0;
            for (int k = 0; k < 4; k++)
                for (int i = 0; i <= seg; i++)
                {
                    float a = (k * 90 + i * 90f / seg) * Mathf.Deg2Rad;
                    var p = corners[k] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                    v.position = p;
                    v.color = gradient ? Color.Lerp(bottom, top, Mathf.InverseLerp(r.yMin, r.yMax, p.y)) : bottom;
                    vh.AddVert(v); count++;
                }
            for (int i = 0; i < count; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
        }
    }

    /// <summary>Filled circle (avatar frames, status dots, unread badges, and as a Mask for round avatars).</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class YYCircle : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float rad = Mathf.Min(r.width, r.height) * .5f;
            if (rad <= 0) return;
            int n = 32, start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color; v.position = r.center; vh.AddVert(v);
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.position = r.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad; vh.AddVert(v); }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % n);
        }
    }

    /// <summary>File-type tile for file messages: a page with a folded corner and a coloured band.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class YYFileIcon : MaskableGraphic
    {
        public Color band = new Color32(59, 130, 246, 255);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width <= 0) return;
            float fold = r.width * .3f;
            var page = new Color32(246, 248, 252, 255);
            var edge = new Color32(190, 200, 214, 255);
            Poly(vh, edge, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax - fold), new Vector2(r.xMax - fold, r.yMax), new Vector2(r.xMin, r.yMax));
            var inner = new Rect(r.x + 1.5f, r.y + 1.5f, r.width - 3, r.height - 3);
            Poly(vh, page, new Vector2(inner.xMin, inner.yMin), new Vector2(inner.xMax, inner.yMin), new Vector2(inner.xMax, inner.yMax - fold), new Vector2(inner.xMax - fold, inner.yMax), new Vector2(inner.xMin, inner.yMax));
            Poly(vh, edge, new Vector2(r.xMax - fold, r.yMax), new Vector2(r.xMax - fold, r.yMax - fold), new Vector2(r.xMax, r.yMax - fold));
            Poly(vh, band, new Vector2(inner.xMin, inner.yMin + inner.height * .12f), new Vector2(inner.xMax, inner.yMin + inner.height * .12f), new Vector2(inner.xMax, inner.yMin + inner.height * .42f), new Vector2(inner.xMin, inner.yMin + inner.height * .42f));
        }

        static void Poly(VertexHelper vh, Color color, params Vector2[] pts)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            foreach (var p in pts) { v.position = p; vh.AddVert(v); }
            for (int i = 1; i + 1 < pts.Length; i++) vh.AddTriangle(start, start + i, start + i + 1);
        }
    }
}
