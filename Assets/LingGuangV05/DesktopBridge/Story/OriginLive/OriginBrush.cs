using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// A buffered immediate-mode pen for the film's code-drawn scenes. Coordinates are the film's 1920×1080 reference space (x right,
    /// y down). It re-creates the SVG look: anti-aliased strokes, Gaussian "glow" (the film's feGaussianBlur merge filters g1/g2/g3),
    /// dashes, soft discs, and groups with an opacity. Translucent colours go through <see cref="OriginInk.Over"/> so a Linear
    /// colour space blends like the film's gamma-space render.
    /// </summary>
    public sealed class OriginBrush
    {
        /// <summary>The film's blur filters: standard deviations of g1, g2, g3.</summary>
        public const float G1 = 2.5f, G2 = 7f, G3 = 16f;

        readonly List<UIVertex> verts = new List<UIVertex>(4096);
        readonly List<int> tris = new List<int>(8192);
        /// <summary>One screen pixel in reference units; the anti-aliasing feather.</summary>
        public float Pixel = 1;
        /// <summary>Multiplied into every alpha drawn: the film's group opacity.</summary>
        public float Alpha = 1;
        float ox = 960, oy = 540;
        /// <summary>A shake offset in reference units applied to everything drawn (the film's translate on a group).</summary>
        public float Dx, Dy;

        public int VertexCount { get { return verts.Count; } }

        /// <summary>The reference-space point at the centre of the graphic this pen feeds.</summary>
        public void Begin(float centerX, float centerY, float pixel)
        {
            verts.Clear(); tris.Clear(); ox = centerX; oy = centerY; Pixel = pixel; Alpha = 1; Dx = Dy = 0;
        }

        public void CopyTo(VertexHelper vh)
        {
            vh.Clear();
            int n = Mathf.Min(verts.Count, 64000);
            for (int i = 0; i < n; i++) vh.AddVert(verts[i]);
            for (int i = 0; i + 2 < tris.Count; i += 3)
                if (tris[i] < n && tris[i + 1] < n && tris[i + 2] < n) vh.AddTriangle(tris[i], tris[i + 1], tris[i + 2]);
        }

        Vector2 P(float x, float y) { return new Vector2(x - ox + Dx, oy - y - Dy); }
        Vector2 P(Vector2 p) { return new Vector2(p.x - ox + Dx, oy - p.y - Dy); }

        Color C(Color rgb, float a) { return OriginInk.Over(rgb, a * Alpha); }

        int V(Vector2 local, Color c)
        {
            var v = UIVertex.simpleVert; v.position = local; v.color = c;
            verts.Add(v);
            return verts.Count - 1;
        }

        void T(int a, int b, int c) { tris.Add(a); tris.Add(b); tris.Add(c); }

        // ───────────── math ─────────────

        static float Erf(float x)
        {
            float s = Mathf.Sign(x); x = Mathf.Abs(x);
            float t = 1f / (1f + .3275911f * x);
            float y = 1f - (((((1.061405429f * t - 1.453152027f) * t) + 1.421413741f) * t - .284496736f) * t + .254829592f) * t * Mathf.Exp(-x * x);
            return s * y;
        }

        /// <summary>Coverage of a width-w line blurred by sigma, at signed distance d from its centre.</summary>
        static float Blurred(float d, float w, float sigma)
        {
            float h = w * .5f, k = sigma * 1.41421356f;
            return .5f * (Erf((d + h) / k) - Erf((d - h) / k));
        }

        public static List<Vector2> Quadratic(Vector2 p0, Vector2 p1, Vector2 p2, int n = 20)
        {
            var l = new List<Vector2>(n + 1);
            for (int i = 0; i <= n; i++) { float t = i / (float)n, u = 1 - t; l.Add(u * u * p0 + 2 * u * t * p1 + t * t * p2); }
            return l;
        }

        public static List<Vector2> Cubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int n = 24)
        {
            var l = new List<Vector2>(n + 1);
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n, u = 1 - t;
                l.Add(u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3);
            }
            return l;
        }

        public static float Length(IList<Vector2> pts)
        {
            float len = 0;
            for (int i = 1; i < pts.Count; i++) len += (pts[i] - pts[i - 1]).magnitude;
            return len;
        }

        /// <summary>The first `distance` units of a polyline.</summary>
        public static List<Vector2> Trim(IList<Vector2> pts, float distance)
        {
            var l = new List<Vector2>(pts.Count);
            if (pts.Count == 0) return l;
            l.Add(pts[0]);
            float acc = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                float seg = (pts[i] - pts[i - 1]).magnitude;
                if (acc + seg >= distance)
                {
                    float k = seg > 1e-5f ? (distance - acc) / seg : 0;
                    l.Add(Vector2.Lerp(pts[i - 1], pts[i], Mathf.Clamp01(k)));
                    return l;
                }
                acc += seg; l.Add(pts[i]);
            }
            return l;
        }

        // ───────────── ribbons (strokes) ─────────────

        void Ribbon(IList<Vector2> pts, bool closed, float[] offs, float[] alphas, Color rgb, float a)
        {
            int n = pts.Count;
            if (n < 2 || a * Alpha <= .0005f) return;
            int rows = offs.Length;
            var pos = new Vector2[n];
            for (int i = 0; i < n; i++) pos[i] = P(pts[i]);
            var nor = new Vector2[n]; var scale = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 d1 = Vector2.zero, d2 = Vector2.zero;
                int ip = i - 1, inx = i + 1;
                if (closed) { ip = (i + n - 1) % n; inx = (i + 1) % n; }
                if (ip >= 0) d1 = (pos[i] - pos[ip]);
                if (inx < n) d2 = (pos[inx] - pos[i]);
                if (d1.sqrMagnitude < 1e-10f) d1 = d2; if (d2.sqrMagnitude < 1e-10f) d2 = d1;
                if (d1.sqrMagnitude < 1e-10f) { nor[i] = Vector2.up; scale[i] = 1; continue; }
                d1.Normalize(); d2.Normalize();
                Vector2 n1 = new Vector2(-d1.y, d1.x), n2 = new Vector2(-d2.y, d2.x);
                Vector2 nm = n1 + n2;
                if (nm.sqrMagnitude < 1e-6f) nm = n1;
                nm.Normalize();
                nor[i] = nm; scale[i] = 1f / Mathf.Max(.4f, Vector2.Dot(nm, n1));
            }
            int baseIdx = verts.Count;
            for (int i = 0; i < n; i++)
                for (int k = 0; k < rows; k++)
                {
                    var c = C(rgb, a * alphas[k]); 
                    V(pos[i] + nor[i] * offs[k] * scale[i], c);
                }
            int segs = closed ? n : n - 1;
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % n;
                for (int k = 0; k < rows - 1; k++)
                {
                    int a0 = baseIdx + i * rows + k, b0 = baseIdx + j * rows + k;
                    T(a0, a0 + 1, b0 + 1); T(a0, b0 + 1, b0);
                }
            }
        }

        static readonly Dictionary<long, (float[] o, float[] a)> glowCache = new Dictionary<long, (float[], float[])>();

        static (float[] o, float[] a) GlowProfile(float w, float sigma)
        {
            long key = ((long)Mathf.RoundToInt(w * 20) << 24) | (long)Mathf.RoundToInt(sigma * 20);
            if (glowCache.TryGetValue(key, out var hit)) return hit;
            float h = w * .5f;
            var pos = new List<float> { 0 };
            if (h > sigma * .7f) pos.Add(h * .5f);
            pos.Add(h); pos.Add(h + .6f * sigma); pos.Add(h + 1.3f * sigma); pos.Add(h + 2.1f * sigma); pos.Add(h + 3f * sigma); pos.Add(h + 4f * sigma);
            int m = pos.Count;
            var o = new float[m * 2 - 1]; var a = new float[m * 2 - 1];
            for (int i = 0; i < m; i++)
            {
                float v = Blurred(pos[i], w, sigma);
                o[m - 1 + i] = pos[i]; a[m - 1 + i] = v;
                o[m - 1 - i] = -pos[i]; a[m - 1 - i] = v;
            }
            a[0] = a[a.Length - 1] = 0;
            return glowCache[key] = (o, a);
        }

        /// <summary>A stroke: optional Gaussian glow (sigma > 0, blurred copy under the line), then the anti-aliased line.</summary>
        public void Stroke(IList<Vector2> pts, float width, Color rgb, float a, float glow = 0, bool closed = false)
        {
            if (pts.Count < 2) return;
            if (glow > 0)
            {
                var g = GlowProfile(width, glow);
                Ribbon(pts, closed, g.o, g.a, rgb, a);
            }
            float px = Pixel;
            float inner = Mathf.Max(0, (width - px) * .5f), outer = inner + px;
            float aa = width < px ? width / px : 1;
            Ribbon(pts, closed, new[] { -outer, -inner, inner, outer }, new[] { 0f, aa, aa, 0f }, rgb, a);
        }

        public void Line(float x1, float y1, float x2, float y2, float width, Color rgb, float a, float glow = 0)
        {
            Stroke(new[] { new Vector2(x1, y1), new Vector2(x2, y2) }, width, rgb, a, glow);
        }

        public void StrokeRect(float x, float y, float w, float h, float width, Color rgb, float a, float glow = 0)
        {
            Stroke(new[] { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h) }, width, rgb, a, glow, true);
        }

        /// <summary>A dashed polyline (SVG stroke-dasharray). Round dashes are not needed; ends are butt.</summary>
        public void Dashed(IList<Vector2> pts, float on, float off, float width, Color rgb, float a, bool closed = false, float phase = 0)
        {
            var path = new List<Vector2>(pts);
            if (closed && path.Count > 0) path.Add(path[0]);
            float pos = -phase;
            float period = on + off;
            var run = new List<Vector2>();
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 p0 = path[i - 1], p1 = path[i];
                float seg = (p1 - p0).magnitude;
                if (seg < 1e-5f) continue;
                float at = 0;
                while (at < seg)
                {
                    float m = Mathf.Repeat(pos + at, period);
                    bool isOn = m < on;
                    float toggle = isOn ? on - m : period - m;
                    float step = Mathf.Min(toggle, seg - at);
                    if (isOn)
                    {
                        if (run.Count == 0) run.Add(Vector2.Lerp(p0, p1, at / seg));
                        run.Add(Vector2.Lerp(p0, p1, (at + step) / seg));
                    }
                    else if (run.Count > 0) { Stroke(run, width, rgb, a); run.Clear(); }
                    at += step;
                    if (step <= 1e-6f) at += 1e-3f;
                }
                pos += seg;
            }
            if (run.Count > 1) Stroke(run, width, rgb, a);
        }

        // ───────────── fills ─────────────

        public void Rect(float x, float y, float w, float h, Color rgb, float a)
        {
            if (a * Alpha <= .0005f) return;
            var c = C(rgb, a);
            int i = V(P(x, y), c); V(P(x + w, y), c); V(P(x + w, y + h), c); V(P(x, y + h), c);
            T(i, i + 1, i + 2); T(i, i + 2, i + 3);
        }

        /// <summary>Vertical gradient fill (top colour to bottom colour), both opaque stops scaled by a.</summary>
        public void RectGradientV(float x, float y, float w, float h, Color top, Color bottom, float a)
        {
            var ct = C(top, a); var cb = C(bottom, a);
            int i = V(P(x, y), ct); V(P(x + w, y), ct); V(P(x + w, y + h), cb); V(P(x, y + h), cb);
            T(i, i + 1, i + 2); T(i, i + 2, i + 3);
        }

        /// <summary>Filled rectangle with an outline and optional glow on the outline.</summary>
        public void Box(float x, float y, float w, float h, Color fill, float fa, Color stroke, float sa, float sw, float glow = 0)
        {
            if (fa > 0) Rect(x, y, w, h, fill, fa);
            if (sa > 0 && sw > 0) StrokeRect(x, y, w, h, sw, stroke, sa, glow);
        }

        /// <summary>A convex polygon (triangle fan).</summary>
        public void Fill(IList<Vector2> pts, Color rgb, float a)
        {
            if (pts.Count < 3 || a * Alpha <= .0005f) return;
            var c = C(rgb, a);
            int b = verts.Count;
            for (int i = 0; i < pts.Count; i++) V(P(pts[i]), c);
            for (int i = 1; i + 1 < pts.Count; i++) T(b, b + i, b + i + 1);
        }

        public void FillEllipse(float cx, float cy, float rx, float ry, Color rgb, float a, float rotDeg = 0, int seg = 40)
        {
            var pts = EllipsePoints(cx, cy, rx, ry, rotDeg, seg);
            Fill(pts, rgb, a);
        }

        public static List<Vector2> EllipsePoints(float cx, float cy, float rx, float ry, float rotDeg = 0, int seg = 64, float pivotX = float.NaN, float pivotY = float.NaN)
        {
            var l = new List<Vector2>(seg);
            float rot = rotDeg * Mathf.Deg2Rad, cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
            if (float.IsNaN(pivotX)) { pivotX = cx; pivotY = cy; }
            for (int i = 0; i < seg; i++)
            {
                float an = i * Mathf.PI * 2 / seg;
                float x = cx + Mathf.Cos(an) * rx - pivotX, y = cy + Mathf.Sin(an) * ry - pivotY;
                l.Add(new Vector2(pivotX + x * cs - y * sn, pivotY + x * sn + y * cs));
            }
            return l;
        }

        public void Ellipse(float cx, float cy, float rx, float ry, float width, Color rgb, float a, float glow = 0, int seg = 96)
        {
            Stroke(EllipsePoints(cx, cy, rx, ry, 0, seg), width, rgb, a, glow, true);
        }

        // ───────────── discs and halos ─────────────

        static readonly float[] HaloR = { 0f, .8f, 1.6f, 2.4f, 3.2f, 4f };
        static readonly float[] HaloA = { 1f, .726f, .278f, .0561f, .006f, 0f };

        /// <summary>A Gaussian-looking glow: peak alpha at the centre, sigma = one standard deviation.</summary>
        public void Halo(float cx, float cy, float sigma, Color rgb, float peak, int seg = 20)
        {
            if (peak * Alpha <= .003f) return;
            var c0 = P(cx, cy);
            int b = verts.Count;
            V(c0, C(rgb, peak));
            for (int k = 1; k < HaloR.Length; k++)
            {
                var col = C(rgb, peak * HaloA[k]);
                for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c0 + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * sigma * HaloR[k], col); }
            }
            for (int i = 0; i < seg; i++) T(b, b + 1 + i, b + 1 + (i + 1) % seg);
            for (int k = 1; k < HaloR.Length - 1; k++)
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg, a0 = b + 1 + (k - 1) * seg, b0 = b + 1 + k * seg;
                    T(a0 + i, b0 + i, b0 + j); T(a0 + i, b0 + j, a0 + j);
                }
        }

        /// <summary>A disc blurred by sigma: soft edge for big discs, a Gaussian for small ones.</summary>
        void SoftDisc(float cx, float cy, float r, float sigma, Color rgb, float a, int seg)
        {
            if (r <= sigma * 1.2f)
            {
                float peak = 1 - Mathf.Exp(-r * r / (2 * sigma * sigma));
                Halo(cx, cy, Mathf.Sqrt(sigma * sigma + r * r / 3f), rgb, a * peak, seg);
                return;
            }
            var radii = new List<float> { 0 };
            foreach (float k in new[] { -2f, -1f, -.5f, 0f, .5f, 1f, 2f, 3f, 4f })
            {
                float rr = r + k * sigma;
                if (rr > radii[radii.Count - 1] + .01f) radii.Add(rr);
            }
            var c0 = P(cx, cy);
            int b = verts.Count;
            V(c0, C(rgb, a * Ring(0, r, sigma)));
            for (int k = 1; k < radii.Count; k++)
            {
                var col = C(rgb, a * Ring(radii[k], r, sigma));
                for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c0 + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * radii[k], col); }
            }
            for (int i = 0; i < seg; i++) T(b, b + 1 + i, b + 1 + (i + 1) % seg);
            for (int k = 1; k < radii.Count - 1; k++)
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg, a0 = b + 1 + (k - 1) * seg, b0 = b + 1 + k * seg;
                    T(a0 + i, b0 + i, b0 + j); T(a0 + i, b0 + j, a0 + j);
                }
        }

        static float Ring(float rho, float r, float sigma) { return .5f * (1 - Erf((rho - r) / (sigma * 1.41421356f))); }

        /// <summary>A filled disc (anti-aliased rim) with an optional Gaussian glow under it (the film's blur-and-merge).</summary>
        public void Disc(float cx, float cy, float r, Color rgb, float a, float glow = 0, int seg = 0)
        {
            if (a * Alpha <= .0005f || r <= 0) return;
            if (seg <= 0) seg = r < 4 ? 10 : r < 20 ? 20 : r < 80 ? 36 : 64;
            if (glow > 0) SoftDisc(cx, cy, r, glow, rgb, a, glow > 5 ? Mathf.Max(seg, 28) : Mathf.Min(seg, 14));
            float px = Pixel;
            float inner = Mathf.Max(0, r - px * .5f), outer = r + px * .5f;
            float fa = a;
            if (r < px * .5f) { fa *= Mathf.Clamp01(r / (px * .5f)) * .8f; inner = 0; }
            var core = C(rgb, fa); var edge = core; edge.a = 0;
            var c0 = P(cx, cy);
            int b = verts.Count;
            V(c0, core);
            for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c0 + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * inner, core); }
            for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c0 + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * outer, edge); }
            for (int i = 0; i < seg; i++)
            {
                int j = (i + 1) % seg;
                T(b, b + 1 + i, b + 1 + j);
                T(b + 1 + i, b + 1 + seg + i, b + 1 + seg + j);
                T(b + 1 + i, b + 1 + seg + j, b + 1 + j);
            }
        }

        /// <summary>A soft annulus with radial alpha stops: used for vignettes and spotlight masks. Alpha stops are (radius, alpha) ascending.</summary>
        public void RadialAlpha(float cx, float cy, Color rgb, IList<Vector2> stops, int seg = 72, bool veil = false)
        {
            if (stops.Count < 2) return;
            var c0 = P(cx, cy);
            int rings = stops.Count;
            int b = verts.Count;
            for (int k = 0; k < rings; k++)
            {
                var col = veil ? new Color(rgb.r, rgb.g, rgb.b, OriginInk.Veil(stops[k].y * Alpha)) : C(rgb, stops[k].y);
                if (stops[k].x <= 0.001f) { V(c0, col); continue; }
                for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c0 + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * stops[k].x, col); }
            }
            bool centre = stops[0].x <= 0.001f;
            if (centre)
            {
                for (int i = 0; i < seg; i++) T(b, b + 1 + i, b + 1 + (i + 1) % seg);
            }
            int ringStart = centre ? 1 : 0;
            for (int k = ringStart; k < rings - 1; k++)
            {
                int a0 = b + (centre ? 1 + (k - 1) * seg : k * seg), b0 = a0 + seg;
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg;
                    T(a0 + i, b0 + i, b0 + j); T(a0 + i, b0 + j, a0 + j);
                }
            }
        }

        /// <summary>Points of a rounded rectangle (clockwise from the top-left arc).</summary>
        public static List<Vector2> RoundRectPoints(float x, float y, float w, float h, float r, int arc = 5)
        {
            var l = new List<Vector2>();
            Vector2[] centres = { new Vector2(x + r, y + r), new Vector2(x + w - r, y + r), new Vector2(x + w - r, y + h - r), new Vector2(x + r, y + h - r) };
            for (int c = 0; c < 4; c++)
                for (int i = 0; i <= arc; i++)
                {
                    float an = (180 + c * 90 + i * 90f / arc) * Mathf.Deg2Rad;
                    l.Add(centres[c] + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * r);
                }
            return l;
        }

        public void RoundBox(float x, float y, float w, float h, float r, Color fill, float fa, Color stroke, float sa, float sw, float glow = 0)
        {
            var pts = RoundRectPoints(x, y, w, h, r);
            if (fa > 0) Fill(pts, fill, fa);
            if (sa > 0 && sw > 0) Stroke(pts, sw, stroke, sa, glow, true);
        }

        /// <summary>A radial gradient with a colour and alpha per stop (radius, colour, alpha), ascending radius; the last stop extends outward.</summary>
        public void RadialColors(float cx, float cy, IList<(float r, Color rgb, float a)> stops, int seg = 72)
        {
            var c0 = P(cx, cy);
            int b = verts.Count;
            bool centre = stops[0].r <= .001f;
            for (int k = 0; k < stops.Count; k++)
            {
                var col = C(stops[k].rgb, stops[k].a);
                if (stops[k].r <= .001f) { V(c0, col); continue; }
                for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c0 + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * stops[k].r, col); }
            }
            if (centre) for (int i = 0; i < seg; i++) T(b, b + 1 + i, b + 1 + (i + 1) % seg);
            for (int k = centre ? 1 : 0; k < stops.Count - 1; k++)
            {
                int a0 = b + (centre ? 1 + (k - 1) * seg : k * seg), b0 = a0 + seg;
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg;
                    T(a0 + i, b0 + i, b0 + j); T(a0 + i, b0 + j, a0 + j);
                }
            }
        }
    }

    /// <summary>A stage-sized graphic that shows what an <see cref="OriginBrush"/> drew (full-stage reference coordinates, centred on the stage).</summary>
    public sealed class OriginBrushCanvas : OriginGraphic
    {
        public readonly OriginBrush Brush = new OriginBrush();

        /// <summary>Starts a frame: clears the brush, sets the feather width. Draw on the returned brush, then call <see cref="End"/>.</summary>
        public OriginBrush Begin() { Brush.Begin(960, 540, Pixel); return Brush; }
        public void End() { SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh) { Brush.CopyTo(vh); }
    }
}
