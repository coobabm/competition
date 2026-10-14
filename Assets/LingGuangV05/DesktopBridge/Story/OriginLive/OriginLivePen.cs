using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// An immediate-mode vector pen for the film's SVG-style drawing. Coordinates are the Remotion reference space (1920×1080, origin top-left,
    /// y down). Call Clear, draw, then hand it to an <see cref="OriginCanvas"/>. Colours are given as an rgb plus an alpha and go through
    /// <see cref="OriginInk.Over"/> (so translucent layers land where the film's gamma-space blend does); <see cref="Opacity"/> is the SVG group opacity.
    /// Edges are feathered one screen pixel wide (<see cref="Pixel"/>), because the UI draws without MSAA.
    /// </summary>
    public sealed class OriginPen
    {
        public float Pixel = 1;
        /// <summary>Multiplies every alpha drawn (an SVG &lt;g opacity&gt;). Set it before a group, restore it after.</summary>
        public float Opacity = 1;

        readonly List<Vector2> pos = new List<Vector2>(4096);
        readonly List<Color> col = new List<Color>(4096);
        readonly List<int> tri = new List<int>(16384);

        // current transform: x' = a*x + c*y + tx, y' = b*x + d*y + ty
        struct M { public float a, b, c, d, tx, ty; public float Scale; }
        M m = Identity;
        readonly Stack<M> stack = new Stack<M>();
        static readonly M Identity = new M { a = 1, d = 1, Scale = 1 };

        public int VertexCount => pos.Count;

        public void Clear()
        {
            pos.Clear(); col.Clear(); tri.Clear(); stack.Clear(); m = Identity; Opacity = 1;
        }

        /// <summary>SVG transform="translate(tx ty) scale(s) rotate(deg)" for everything drawn until <see cref="Pop"/>.</summary>
        public void Push(float tx, float ty, float scale = 1, float rotateDeg = 0)
        {
            stack.Push(m);
            float r = rotateDeg * Mathf.Deg2Rad, cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            // child = T * S * R; compose onto the current matrix
            float la = scale * cs, lb = scale * sn, lc = -scale * sn, ld = scale * cs;
            var n = new M
            {
                a = m.a * la + m.c * lb, b = m.b * la + m.d * lb,
                c = m.a * lc + m.c * ld, d = m.b * lc + m.d * ld,
                tx = m.a * tx + m.c * ty + m.tx, ty = m.b * tx + m.d * ty + m.ty,
                Scale = m.Scale * scale,
            };
            m = n;
        }

        public void Pop() { m = stack.Count > 0 ? stack.Pop() : Identity; }

        Vector2 X(float x, float y) => new Vector2(m.a * x + m.c * y + m.tx, m.b * x + m.d * y + m.ty);

        /// <summary>The colour a layer of rgb at alpha a (times the group opacity) is drawn with.</summary>
        public Color Ink(Color rgb, float a = 1) => OriginInk.Over(rgb, a * Opacity);

        int V(Vector2 p, Color c) { pos.Add(p); col.Add(c); return pos.Count - 1; }
        void T(int a, int b, int c) { tri.Add(a); tri.Add(b); tri.Add(c); }

        // ───────────── lines ─────────────

        /// <summary>A line of the given width with a one-pixel feathered edge; thinner than a pixel means fainter, not aliased. glow &gt; 0 adds an SVG-style blur halo (sigma in px).</summary>
        public void Line(float x1, float y1, float x2, float y2, float width, Color rgb, float a = 1, float glow = 0)
        {
            if (a * Opacity <= .001f) return;
            if (glow > 0) SoftLine(x1, y1, x2, y2, width, rgb, a, glow);
            Ribbon(X(x1, y1), X(x2, y2), width * m.Scale, Ink(rgb, a));
        }

        public void Line(Vector2 p, Vector2 q, float width, Color rgb, float a = 1, float glow = 0) { Line(p.x, p.y, q.x, q.y, width, rgb, a, glow); }

        void Ribbon(Vector2 a, Vector2 b, float width, Color c)
        {
            Vector2 d = b - a; float len = d.magnitude; if (len < 1e-4f) return;
            Vector2 n = new Vector2(-d.y, d.x) / len;
            float px = Pixel;
            float inner = Mathf.Max(0, (width - px) * .5f), outer = inner + px;
            if (width < px) c.a *= width / px;
            var edge = c; edge.a = 0;
            int i = pos.Count;
            V(a - n * outer, edge); V(a - n * inner, c); V(a + n * inner, c); V(a + n * outer, edge);
            V(b - n * outer, edge); V(b - n * inner, c); V(b + n * inner, c); V(b + n * outer, edge);
            for (int k = 0; k < 3; k++) { T(i + k, i + k + 1, i + 4 + k + 1); T(i + k, i + 4 + k + 1, i + 4 + k); }
        }

        /// <summary>A soft ribbon standing in for a blurred line: peak alpha in the middle, fading to zero ~2.2 sigma out.</summary>
        void SoftLine(float x1, float y1, float x2, float y2, float width, Color rgb, float a, float sigma)
        {
            float peak = a * Erf(width * m.Scale / (2.8284f * sigma * m.Scale));
            if (peak * Opacity <= .004f) return;
            Vector2 p = X(x1, y1), q = X(x2, y2), d = q - p; float len = d.magnitude; if (len < 1e-4f) return;
            Vector2 n = new Vector2(-d.y, d.x) / len;
            float h = 2.2f * sigma * m.Scale;
            var c = Ink(rgb, peak); var e = c; e.a = 0;
            int i = pos.Count;
            V(p - n * h, e); V(p, c); V(p + n * h, e); V(q - n * h, e); V(q, c); V(q + n * h, e);
            T(i, i + 1, i + 4); T(i, i + 4, i + 3); T(i + 1, i + 2, i + 5); T(i + 1, i + 5, i + 4);
        }

        static float Erf(float x)
        {
            float t = 1 / (1 + .3275911f * Mathf.Abs(x));
            float y = 1 - (((((1.061405429f * t - 1.453152027f) * t) + 1.421413741f) * t - .284496736f) * t + .254829592f) * t * Mathf.Exp(-x * x);
            return x >= 0 ? y : -y;
        }

        /// <summary>A continuous stroke through the points (miter joins, so a translucent line does not double-blend at the joins).</summary>
        public void Polyline(IList<Vector2> pts, int count, float width, Color rgb, float a = 1, bool closed = false, float glow = 0)
        {
            if (count < 2 || a * Opacity <= .001f) return;
            if (glow > 0) for (int k = 0; k + 1 < count; k++) SoftLine(pts[k].x, pts[k].y, pts[k + 1].x, pts[k + 1].y, width, rgb, a, glow);
            var c = Ink(rgb, a);
            float w = width * m.Scale, px = Pixel;
            float inner = Mathf.Max(0, (w - px) * .5f), outer = inner + px;
            if (w < px) c.a *= w / px;
            var edge = c; edge.a = 0;
            int n = count;
            var p = new Vector2[n];
            for (int k = 0; k < n; k++) p[k] = X(pts[k].x, pts[k].y);
            int start = pos.Count;
            for (int k = 0; k < n; k++)
            {
                Vector2 dirA = k > 0 ? (p[k] - p[k - 1]) : (closed ? p[0] - p[n - 1] : p[1] - p[0]);
                Vector2 dirB = k < n - 1 ? (p[k + 1] - p[k]) : (closed ? p[0] - p[n - 1] : p[k] - p[k - 1]);
                if (dirA.sqrMagnitude < 1e-8f) dirA = dirB;
                if (dirB.sqrMagnitude < 1e-8f) dirB = dirA;
                dirA.Normalize(); dirB.Normalize();
                Vector2 na = new Vector2(-dirA.y, dirA.x), nb = new Vector2(-dirB.y, dirB.x);
                Vector2 nm = (na + nb); float l = nm.magnitude;
                nm = l < 1e-4f ? na : nm / l;
                float miter = Mathf.Min(2.5f, 1f / Mathf.Max(.4f, Vector2.Dot(nm, na)));
                var nn = nm * miter;
                V(p[k] - nn * outer, edge); V(p[k] - nn * inner, c); V(p[k] + nn * inner, c); V(p[k] + nn * outer, edge);
            }
            int segs = closed ? n : n - 1;
            for (int k = 0; k < segs; k++)
            {
                int a0 = start + k * 4, b0 = start + ((k + 1) % n) * 4;
                for (int j = 0; j < 3; j++) { T(a0 + j, a0 + j + 1, b0 + j + 1); T(a0 + j, b0 + j + 1, b0 + j); }
            }
        }

        // ───────────── discs and rings ─────────────

        /// <summary>A filled disc with a feathered rim. glow &gt; 0 adds an SVG blur halo (sigma in px) under it, as filter="url(#g1)" does.</summary>
        public void Disc(float cx, float cy, float r, Color rgb, float a = 1, float glow = 0)
        {
            if (a * Opacity <= .001f) return;
            if (glow > 0) Halo(cx, cy, Mathf.Sqrt(glow * glow + r * r / 3f), rgb, a * (1 - Mathf.Exp(-r * r / (2 * glow * glow))));
            var c = X(cx, cy); float rr = r * m.Scale; float px = Pixel;
            var color = Ink(rgb, a);
            float inner = Mathf.Max(0, rr - px * .5f), outer = rr + px * .5f;
            if (rr < px * .5f) { color.a *= Mathf.Clamp01(rr / (px * .5f)) * .8f; inner = 0; }
            int seg = Mathf.Clamp(Mathf.CeilToInt(rr * 1.2f), 10, 48);
            var edge = color; edge.a = 0;
            int s = pos.Count;
            V(c, color);
            for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * inner, color); }
            for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * outer, edge); }
            for (int i = 0; i < seg; i++)
            {
                int j = (i + 1) % seg;
                T(s, s + 1 + i, s + 1 + j);
                T(s + 1 + i, s + 1 + seg + i, s + 1 + seg + j);
                T(s + 1 + i, s + 1 + seg + j, s + 1 + j);
            }
        }

        /// <summary>A stroked circle (SVG circle with fill none). glow &gt; 0 adds a soft ring around it.</summary>
        public void Ring(float cx, float cy, float r, float width, Color rgb, float a = 1, float glow = 0)
        {
            if (a * Opacity <= .001f) return;
            var c = X(cx, cy); float rr = r * m.Scale, w = width * m.Scale, px = Pixel;
            int seg = Mathf.Clamp(Mathf.CeilToInt(rr * 1.2f), 16, 64);
            if (glow > 0)
            {
                float peak = a * Erf(width / (2.8284f * glow));
                float h = 2.2f * glow * m.Scale;
                AnnulusFade(c, rr, h, Ink(rgb, peak), seg);
            }
            var color = Ink(rgb, a);
            float inner = Mathf.Max(0, (w - px) * .5f), outer = inner + px;
            if (w < px) color.a *= w / px;
            var edge = color; edge.a = 0;
            int s = pos.Count;
            float[] rad = { Mathf.Max(0, rr - outer), Mathf.Max(0, rr - inner), rr + inner, rr + outer };
            for (int k = 0; k < 4; k++)
                for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * rad[k], k == 0 || k == 3 ? edge : color); }
            for (int k = 0; k < 3; k++)
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg, a0 = s + k * seg, b0 = s + (k + 1) * seg;
                    T(a0 + i, b0 + i, b0 + j); T(a0 + i, b0 + j, a0 + j);
                }
        }

        void AnnulusFade(Vector2 c, float rr, float h, Color peak, int seg)
        {
            var edge = peak; edge.a = 0;
            int s = pos.Count;
            float[] rad = { Mathf.Max(0, rr - h), rr, rr + h };
            for (int k = 0; k < 3; k++)
                for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * rad[k], k == 1 ? peak : edge); }
            for (int k = 0; k < 2; k++)
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg, a0 = s + k * seg, b0 = s + (k + 1) * seg;
                    T(a0 + i, b0 + i, b0 + j); T(a0 + i, b0 + j, a0 + j);
                }
        }

        /// <summary>A stroked arc of a circle, angles in degrees measured from +x towards +y (clockwise on screen, y down).</summary>
        public void Arc(float cx, float cy, float r, float fromDeg, float toDeg, float width, Color rgb, float a = 1, float glow = 0)
        {
            int seg = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(toDeg - fromDeg) / 6f));
            var pts = new Vector2[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float an = Mathf.Deg2Rad * Mathf.Lerp(fromDeg, toDeg, i / (float)seg);
                pts[i] = new Vector2(cx + Mathf.Cos(an) * r, cy + Mathf.Sin(an) * r);
            }
            Polyline(pts, seg + 1, width, rgb, a, false, glow);
        }

        /// <summary>A filled circular sector (centre + arc), convex for spans up to 180 degrees.</summary>
        public void Pie(float cx, float cy, float r, float fromDeg, float toDeg, Color rgb, float a = 1)
        {
            int seg = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(toDeg - fromDeg) / 6f));
            var pts = new Vector2[seg + 2];
            pts[0] = new Vector2(cx, cy);
            for (int i = 0; i <= seg; i++)
            {
                float an = Mathf.Deg2Rad * Mathf.Lerp(fromDeg, toDeg, i / (float)seg);
                pts[i + 1] = new Vector2(cx + Mathf.Cos(an) * r, cy + Mathf.Sin(an) * r);
            }
            Poly(pts, seg + 2, rgb, a);
        }

        static readonly float[] HaloShape = { 1f, .61f, .135f, .011f, 0f };

        /// <summary>A soft Gaussian-looking glow: peak alpha at the centre, sigma = one standard deviation, zero at 4 sigma.</summary>
        public void Halo(float cx, float cy, float sigma, Color rgb, float peak)
        {
            if (peak * Opacity <= .003f) return;
            var c = X(cx, cy); sigma *= m.Scale;
            int seg = Mathf.Clamp(Mathf.CeilToInt(sigma * 3), 12, 40);
            int rings = HaloShape.Length;
            int s = pos.Count;
            V(c, Ink(rgb, peak * HaloShape[0]));
            for (int k = 1; k < rings; k++)
            {
                var color = Ink(rgb, peak * HaloShape[k]);
                for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; V(c + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * sigma * k, color); }
            }
            for (int i = 0; i < seg; i++) T(s, s + 1 + i, s + 1 + (i + 1) % seg);
            for (int k = 1; k < rings - 1; k++)
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg, a0 = s + 1 + (k - 1) * seg, b0 = s + 1 + k * seg;
                    T(a0 + i, b0 + i, b0 + j); T(a0 + i, b0 + j, a0 + j);
                }
        }

        // ───────────── rectangles and polygons ─────────────

        /// <summary>A filled axis-aligned rectangle (in the current transform). glow &gt; 0 adds a fringe of that sigma.</summary>
        public void Rect(float x, float y, float w, float h, Color rgb, float a = 1, float glow = 0)
        {
            if (a * Opacity <= .001f) return;
            var c = Ink(rgb, a);
            if (glow > 0)
            {
                float peak = a * .25f; float sp = glow * 1.6f * m.Scale;
                var g = Ink(rgb, peak); var g0 = g; g0.a = 0;
                int f = pos.Count;
                V(X(x, y), g); V(X(x + w, y), g); V(X(x + w, y + h), g); V(X(x, y + h), g);
                V(X(x, y) + new Vector2(-sp, -sp), g0); V(X(x + w, y) + new Vector2(sp, -sp), g0); V(X(x + w, y + h) + new Vector2(sp, sp), g0); V(X(x, y + h) + new Vector2(-sp, sp), g0);
                for (int i = 0; i < 4; i++) { int j = (i + 1) % 4; T(f + i, f + 4 + i, f + 4 + j); T(f + i, f + 4 + j, f + j); }
            }
            int s = pos.Count;
            V(X(x, y), c); V(X(x + w, y), c); V(X(x + w, y + h), c); V(X(x, y + h), c);
            T(s, s + 1, s + 2); T(s, s + 2, s + 3);
        }

        /// <summary>A vertical colour gradient rectangle (top to bottom); rgb/alpha at each end.</summary>
        public void RectV(float x, float y, float w, float h, Color rgbTop, float aTop, Color rgbBottom, float aBottom)
        {
            int s = pos.Count;
            var ct = Ink(rgbTop, aTop); var cb = Ink(rgbBottom, aBottom);
            V(X(x, y), ct); V(X(x + w, y), ct); V(X(x + w, y + h), cb); V(X(x, y + h), cb);
            T(s, s + 1, s + 2); T(s, s + 2, s + 3);
        }

        /// <summary>A stroked rectangle (SVG rect with fill none); the stroke is centred on the edge.</summary>
        public void StrokeRect(float x, float y, float w, float h, float width, Color rgb, float a = 1, float glow = 0)
        {
            var pts = new[] { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h) };
            Polyline(pts, 4, width, rgb, a, true, glow);
        }

        /// <summary>A filled convex polygon (fan from the first point).</summary>
        public void Poly(IList<Vector2> pts, int count, Color rgb, float a = 1)
        {
            if (count < 3 || a * Opacity <= .001f) return;
            var c = Ink(rgb, a);
            int s = pos.Count;
            for (int i = 0; i < count; i++) V(X(pts[i].x, pts[i].y), c);
            for (int i = 1; i < count - 1; i++) T(s, s + i, s + i + 1);
        }

        // ───────────── output ─────────────

        /// <summary>Copies the drawing into a graphic's mesh; box is the graphic's rect and pivot (reference px → local).</summary>
        public void Upload(VertexHelper vh, Rect box, Vector2 pivot)
        {
            vh.Clear();
            int n = Mathf.Min(pos.Count, 64000);
            float ox = box.width * pivot.x, oy = box.height * (1 - pivot.y);
            var v = UIVertex.simpleVert;
            for (int i = 0; i < n; i++)
            {
                v.position = new Vector3(pos[i].x - ox, oy - pos[i].y, 0);
                v.color = col[i];
                vh.AddVert(v);
            }
            for (int i = 0; i + 2 < tri.Count; i += 3)
                if (tri[i] < n && tri[i + 1] < n && tri[i + 2] < n) vh.AddTriangle(tri[i], tri[i + 1], tri[i + 2]);
        }
    }

    /// <summary>A stage-sized graphic that shows what an <see cref="OriginPen"/> drew. Call <see cref="Begin"/>, draw on <see cref="Pen"/>, then <see cref="End"/>.</summary>
    public sealed class OriginCanvas : OriginGraphic
    {
        public readonly OriginPen Pen = new OriginPen();

        public OriginPen Begin() { Pen.Clear(); Pen.Pixel = Pixel; return Pen; }
        public void End() { SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Pen.Upload(vh, rectTransform.rect, rectTransform.pivot);
        }
    }
}
