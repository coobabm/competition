using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Vertex helpers shared by the lab's code-drawn graphics.</summary>
    public static class XgDraw
    {
        public static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            v.position = a; vh.AddVert(v); v.position = b; vh.AddVert(v); v.position = c; vh.AddVert(v); v.position = d; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        public static void Box(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        { Quad(vh, min, new Vector2(min.x, max.y), max, new Vector2(max.x, min.y), color); }

        public static void Seg(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a; if (d.sqrMagnitude < 1e-6f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            Quad(vh, a - n, a + n, b + n, b - n, color);
        }

        public static void Dashed(VertexHelper vh, Vector2 a, Vector2 b, float width, float dash, Color color)
        {
            float len = Vector2.Distance(a, b); if (len < 1e-3f) return;
            Vector2 dir = (b - a) / len;
            for (float s = 0; s < len; s += dash * 2) Seg(vh, a + dir * s, a + dir * Mathf.Min(len, s + dash), width, color);
        }

        public static void Disc(VertexHelper vh, Vector2 c, float radius, Color color, int n = 24) { Ellipse(vh, c, radius, radius, color, n); }

        public static void Ellipse(VertexHelper vh, Vector2 c, float rx, float ry, Color color, int n = 24)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color; v.position = c; vh.AddVert(v);
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.position = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); vh.AddVert(v); }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % n);
        }

        /// <summary>Filled pie from angle a0 to a1 (radians, counter-clockwise).</summary>
        public static void Pie(VertexHelper vh, Vector2 c, float radius, float a0, float a1, Color color, int n = 24)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color; v.position = c; vh.AddVert(v);
            for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(a0, a1, (float)i / n); v.position = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; vh.AddVert(v); }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 2 + i);
        }

        /// <summary>Stroked arc (ring segment) from a0 to a1.</summary>
        public static void Arc(VertexHelper vh, Vector2 c, float radius, float width, float a0, float a1, Color color, int n = 32)
        {
            if (Mathf.Abs(a1 - a0) < 1e-4f) return;
            float r0 = radius - width * .5f, r1 = radius + width * .5f;
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(a0, a1, (float)i / n);
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                v.position = c + d * r0; vh.AddVert(v);
                v.position = c + d * r1; vh.AddVert(v);
            }
            for (int i = 0; i < n; i++) { int k = start + i * 2; vh.AddTriangle(k, k + 1, k + 3); vh.AddTriangle(k, k + 3, k + 2); }
        }

        public static void Ring(VertexHelper vh, Vector2 c, float radius, float width, Color color, int n = 40) { Arc(vh, c, radius, width, 0, Mathf.PI * 2, color, n); }

        /// <summary>Convex polygon (fan from the first point).</summary>
        public static void Poly(VertexHelper vh, IList<Vector2> pts, Color color)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            foreach (var p in pts) { v.position = p; vh.AddVert(v); }
            for (int i = 1; i + 1 < pts.Count; i++) vh.AddTriangle(start, start + i, start + i + 1);
        }

        public static void Polyline(VertexHelper vh, IList<Vector2> pts, float width, Color color)
        { for (int i = 0; i + 1 < pts.Count; i++) Seg(vh, pts[i], pts[i + 1], width, color); }
    }

    /// <summary>A ring: shockwaves, the hold-to-buy charge arc and grade stamps. <see cref="Fill"/> draws a partial arc from 12 o'clock.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgRingGraphic : MaskableGraphic
    {
        float width = 4, fill = 1;
        public float Width { get => width; set { if (width != value) { width = value; SetVerticesDirty(); } } }
        public float Fill { get => fill; set { value = Mathf.Clamp01(value); if (fill != value) { fill = value; SetVerticesDirty(); } } }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float radius = Mathf.Min(r.width, r.height) * .5f - width * .5f;
            if (radius <= 0 || fill <= 0) return;
            float top = Mathf.PI / 2;
            XgDraw.Arc(vh, r.center, radius, width, top, top - fill * Mathf.PI * 2, color, Mathf.Max(8, (int)(48 * fill)));
        }
    }

    /// <summary>A skill-tree node: soft glow, filled disc, outline ring and the charge arc while held.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgNodeGraphic : MaskableGraphic
    {
        public Color Fill = Color.white, Outline = Color.gray, Glow = Color.clear, Charge = new Color32(255, 200, 60, 255);
        public float OutlineWidth = 4, GlowAmount, ChargeAmount;
        /// <summary>图鉴 nodes (phenomena) have a dashed frame instead of a solid one.</summary>
        public bool Dashed;
        bool hexagonal;
        int level, maxLevel = 1;
        public void SetShape(bool hexagon, int currentLevel, int maximum)
        {
            maximum = Mathf.Max(1, maximum); currentLevel = Mathf.Clamp(currentLevel, 0, maximum);
            if (hexagonal == hexagon && level == currentLevel && maxLevel == maximum) return;
            hexagonal = hexagon; level = currentLevel; maxLevel = maximum; SetVerticesDirty();
        }

        public void Set(Color fill, Color outline, float outlineWidth, Color glow, float glowAmount, float charge)
        {
            if (Fill == fill && Outline == outline && Mathf.Approximately(OutlineWidth, outlineWidth) && Glow == glow && Mathf.Abs(GlowAmount - glowAmount) < .02f && Mathf.Approximately(ChargeAmount, charge)) return;
            Fill = fill; Outline = outline; OutlineWidth = outlineWidth; Glow = glow; GlowAmount = glowAmount; ChargeAmount = charge;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            // Ordinary nodes are wide cards with room for a readable name; breakthroughs stay hexagons.
            if (!hexagonal && r.width > r.height * 1.3f) { Card(vh, r); return; }
            float radius = Mathf.Min(r.width, r.height) * .5f * .78f;
            var c = r.center;
            if (GlowAmount > 0)
                for (int i = 3; i >= 1; i--)
                {
                    var g = Glow; g.a *= GlowAmount * .22f;
                    XgDraw.Disc(vh, c, radius * (1 + .1f * i * (.6f + GlowAmount * .4f)), g, 32);
                }
            if (hexagonal)
            {
                var points = new Vector2[7];
                for (int i = 0; i < 7; i++) { float angle = Mathf.PI / 6 + i * Mathf.PI / 3; points[i] = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius; }
                XgDraw.Poly(vh, points, Fill); XgDraw.Polyline(vh, points, OutlineWidth, Outline);
            }
            else
            {
                XgDraw.Disc(vh, c, radius, Fill, 36);
                XgDraw.Ring(vh, c, radius - OutlineWidth * .5f, OutlineWidth, Outline, 40);
            }
            if (maxLevel > 1)
            {
                float step = Mathf.PI * 2 / maxLevel;
                for (int i = 0; i < maxLevel; i++)
                {
                    float start = Mathf.PI / 2 - i * step;
                    XgDraw.Arc(vh, c, radius + 4, 4, start - .035f, start - step + .035f, i < level ? new Color32(47, 158, 68, 255) : new Color32(200, 208, 220, 255), 5);
                }
            }
            if (ChargeAmount > 0)
                XgDraw.Arc(vh, c, radius + 7, 6, Mathf.PI / 2, Mathf.PI / 2 - ChargeAmount * Mathf.PI * 2, Charge, 48);
        }

        /// <summary>A rounded card: glow, outline, fill, level pips along the bottom and the hold-to-buy bar.</summary>
        void Card(VertexHelper vh, Rect r)
        {
            var body = new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8);
            float corner = Mathf.Min(16, body.height * .32f);
            if (GlowAmount > 0)
                for (int i = 3; i >= 1; i--)
                {
                    var g = Glow; g.a *= GlowAmount * .2f;
                    float grow = 3 * i * (.6f + GlowAmount * .4f);
                    XgDraw.Poly(vh, Rounded(new Rect(body.x - grow, body.y - grow, body.width + grow * 2, body.height + grow * 2), corner + grow), g);
                }
            float w = Mathf.Max(1, OutlineWidth * .75f);
            if (Dashed)
            {
                XgDraw.Poly(vh, Rounded(body, corner), Fill);
                float inset = w * .5f;
                Vector2 a = new Vector2(body.xMin + corner, body.yMax - inset), b = new Vector2(body.xMax - corner, body.yMax - inset);
                Vector2 c = new Vector2(body.xMax - inset, body.yMax - corner), d = new Vector2(body.xMax - inset, body.yMin + corner);
                Vector2 e = new Vector2(body.xMax - corner, body.yMin + inset), f = new Vector2(body.xMin + corner, body.yMin + inset);
                Vector2 g = new Vector2(body.xMin + inset, body.yMin + corner), h = new Vector2(body.xMin + inset, body.yMax - corner);
                XgDraw.Dashed(vh, a, b, w, 7, Outline); XgDraw.Dashed(vh, c, d, w, 7, Outline);
                XgDraw.Dashed(vh, e, f, w, 7, Outline); XgDraw.Dashed(vh, g, h, w, 7, Outline);
            }
            else
            {
                XgDraw.Poly(vh, Rounded(body, corner), Outline);
                XgDraw.Poly(vh, Rounded(new Rect(body.x + w, body.y + w, body.width - w * 2, body.height - w * 2), Mathf.Max(2, corner - w)), Fill);
            }
            if (maxLevel > 1)
            {
                // One pip per level (at most 17), filled up to the current level.
                float gap = 2, pipW = Mathf.Min(10, (body.width - 24 - gap * (maxLevel - 1)) / maxLevel), total = pipW * maxLevel + gap * (maxLevel - 1);
                float x0 = body.center.x - total * .5f, y0 = body.y + w + 4;
                for (int i = 0; i < maxLevel; i++)
                    XgDraw.Box(vh, new Vector2(x0 + i * (pipW + gap), y0), new Vector2(x0 + i * (pipW + gap) + pipW, y0 + 4),
                        i < level ? (Color)new Color32(47, 158, 68, 255) : new Color32(200, 208, 220, 255));
            }
            if (ChargeAmount > 0)
                XgDraw.Box(vh, new Vector2(body.x + corner, body.yMax + 3), new Vector2(body.x + corner + (body.width - corner * 2) * ChargeAmount, body.yMax + 8), Charge);
        }

        static readonly List<Vector2> RoundedPoints = new List<Vector2>(48);
        static List<Vector2> Rounded(Rect r, float radius)
        {
            RoundedPoints.Clear();
            radius = Mathf.Max(0, Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f));
            RoundedPoints.Add(r.center);
            var corners = new[] { new Vector2(r.xMax - radius, r.yMax - radius), new Vector2(r.xMin + radius, r.yMax - radius), new Vector2(r.xMin + radius, r.yMin + radius), new Vector2(r.xMax - radius, r.yMin + radius) };
            for (int k = 0; k < 4; k++)
                for (int i = 0; i <= 8; i++)
                {
                    float a = (k * 90 + i * 90f / 8) * Mathf.Deg2Rad;
                    RoundedPoints.Add(corners[k] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
            RoundedPoints.Add(RoundedPoints[1]);
            return RoundedPoints;
        }
    }

    /// <summary>Links between skill-tree nodes, in the content's local space.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgLinksGraphic : MaskableGraphic
    {
        public struct Link { public Vector2 a, b; public Color color; public float width; public bool dashed; }
        readonly List<Link> links = new List<Link>();

        public void SetLinks(IEnumerable<Link> list) { links.Clear(); links.AddRange(list); SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (var l in links)
            {
                if (l.dashed) XgDraw.Dashed(vh, l.a, l.b, l.width, 8, l.color);
                else
                {
                    // Horizontal progression: right from the parent, across lanes, then into the child.
                    float midX = (l.a.x + l.b.x) * .5f;
                    var p1 = new Vector2(midX, l.a.y); var p2 = new Vector2(midX, l.b.y);
                    XgDraw.Seg(vh, l.a, p1, l.width, l.color);
                    XgDraw.Seg(vh, p1, p2, l.width, l.color);
                    XgDraw.Seg(vh, p2, l.b, l.width, l.color);
                }
            }
        }
    }
}
