using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Colours for the origin film's code-drawn layers. The Remotion render blends in gamma (sRGB) space; a Linear project blends in
    /// linear space, which makes every translucent layer far brighter than the film. <see cref="Over"/> gives the colour that, drawn
    /// with the given alpha over the film's near-black backdrop, lands where gamma-space blending would.
    /// </summary>
    public static class OriginInk
    {
        public static readonly Color Ink = new Color32(238, 244, 234, 255);
        public static readonly Color Lime = new Color32(212, 245, 90, 255);
        public static readonly Color Red = new Color32(255, 93, 82, 255);
        public static readonly Color Amber = new Color32(255, 180, 77, 255);
        /// <summary>rgba(214,232,220,0.42): the film's dim grey-green.</summary>
        public static readonly Color DimRgb = new Color32(214, 232, 220, 255);
        public const float DimAlpha = .42f;
        public static readonly Color Hot = new Color32(244, 255, 224, 255);

        /// <summary>Typical backdrop level (gamma) under the film's translucent layers.</summary>
        const float Back = .04f;

        public static bool Linear => QualitySettings.activeColorSpace == ColorSpace.Linear;

        static float ToLin(float c) => c <= .04045f ? c / 12.92f : Mathf.Pow((c + .055f) / 1.055f, 2.4f);
        static float ToGamma(float c) => c <= .0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1 / 2.4f) - .055f;

        static float Channel(float c, float a)
        {
            float result = a * c + (1 - a) * Back;
            float want = (ToLin(result) - (1 - a) * ToLin(Back)) / a;
            return ToGamma(Mathf.Clamp01(want));
        }

        public static Color Over(Color rgb, float a)
        {
            a = Mathf.Clamp01(a);
            if (!Linear || a >= .999f || a <= .0005f) return new Color(rgb.r, rgb.g, rgb.b, a);
            return new Color(Channel(rgb.r, a), Channel(rgb.g, a), Channel(rgb.b, a), a);
        }

        /// <summary>
        /// Alpha for a black veil (the vignette) so that bright content darkens like the film's gamma-space blend. Linear blending
        /// darkens bright pixels far less than gamma blending does, so the alpha is raised for a bright reference level.
        /// </summary>
        public static float Veil(float a)
        {
            if (!Linear || a <= 0) return a;
            const float bright = .9f;
            float want = (1 - a) * bright;
            return Mathf.Clamp01(1 - ToLin(want) / ToLin(bright));
        }

        /// <summary>
        /// A group opacity for bright layers: fading a bright layer by alpha a in linear blending looks far too bright next to the film's
        /// gamma-space fade, so the alpha is curved (a^2.2-ish) to land where the film does.
        /// </summary>
        public static float Fade(float a)
        {
            if (!Linear || a >= .999f) return Mathf.Clamp01(a);
            if (a <= 0) return 0;
            return Mathf.Clamp01(ToLin(.9f * a) / ToLin(.9f));
        }

        public static Color Dim(float a = 1) => Over(DimRgb, DimAlpha * a);
    }

    /// <summary>Anti-aliased primitives for the film's graphics. The UI draws without MSAA, so thin lines and small discs get a one-pixel feathered edge.</summary>
    public static class OriginDraw
    {
        static void Vert(VertexHelper vh, Vector2 p, Color c)
        {
            var v = UIVertex.simpleVert; v.position = p; v.color = c; vh.AddVert(v);
        }

        /// <summary>A line of the given width (reference units) with a feathered edge px wide. Thinner than a pixel means fainter, not aliased.</summary>
        public static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c, float px)
        {
            Vector2 d = b - a; float len = d.magnitude; if (len < 1e-4f) return;
            Vector2 n = new Vector2(-d.y, d.x) / len;
            float inner = Mathf.Max(0, (width - px) * .5f), outer = inner + px;
            if (width < px) c.a *= width / px;
            var edge = c; edge.a = 0;
            int i = vh.currentVertCount;
            Vert(vh, a - n * outer, edge); Vert(vh, a - n * inner, c); Vert(vh, a + n * inner, c); Vert(vh, a + n * outer, edge);
            Vert(vh, b - n * outer, edge); Vert(vh, b - n * inner, c); Vert(vh, b + n * inner, c); Vert(vh, b + n * outer, edge);
            for (int k = 0; k < 3; k++)
            {
                vh.AddTriangle(i + k, i + k + 1, i + 4 + k + 1);
                vh.AddTriangle(i + k, i + 4 + k + 1, i + 4 + k);
            }
        }

        /// <summary>A filled disc with a feathered rim.</summary>
        public static void Disc(VertexHelper vh, Vector2 c, float r, Color color, float px, int seg = 20)
        {
            float inner = Mathf.Max(0, r - px * .5f), outer = r + px * .5f;
            if (r < px * .5f) { color.a *= Mathf.Clamp01(r / (px * .5f)) * .8f; inner = 0; }
            var edge = color; edge.a = 0;
            int s = vh.currentVertCount;
            Vert(vh, c, color);
            for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; Vert(vh, c + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * inner, color); }
            for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; Vert(vh, c + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * outer, edge); }
            for (int i = 0; i < seg; i++)
            {
                int j = (i + 1) % seg;
                vh.AddTriangle(s, s + 1 + i, s + 1 + j);
                vh.AddTriangle(s + 1 + i, s + 1 + seg + i, s + 1 + seg + j);
                vh.AddTriangle(s + 1 + i, s + 1 + seg + j, s + 1 + j);
            }
        }

        static readonly float[] HaloShape = { 1f, .61f, .135f, .011f, 0f };

        /// <summary>A soft Gaussian-looking halo: peak alpha at the centre, scale = one standard deviation, reaching zero at 4 sigma.</summary>
        public static void Halo(VertexHelper vh, Vector2 c, float sigma, Color rgb, float peak, int seg = 20)
        {
            if (peak <= .002f) return;
            int rings = HaloShape.Length;
            int s = vh.currentVertCount;
            Vert(vh, c, OriginInk.Over(rgb, peak * HaloShape[0]));
            for (int k = 1; k < rings; k++)
            {
                var col = OriginInk.Over(rgb, peak * HaloShape[k]);
                for (int i = 0; i < seg; i++) { float an = i * Mathf.PI * 2 / seg; Vert(vh, c + new Vector2(Mathf.Cos(an), Mathf.Sin(an)) * sigma * k, col); }
            }
            for (int i = 0; i < seg; i++) vh.AddTriangle(s, s + 1 + i, s + 1 + (i + 1) % seg);
            for (int k = 1; k < rings - 1; k++)
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg;
                    int a0 = s + 1 + (k - 1) * seg, b0 = s + 1 + k * seg;
                    vh.AddTriangle(a0 + i, b0 + i, b0 + j);
                    vh.AddTriangle(a0 + i, b0 + j, a0 + j);
                }
        }

        /// <summary>A solid rectangle with a glow fringe fading out over `spread` reference units.</summary>
        public static void GlowRect(VertexHelper vh, Rect r, Color core, float glowAlpha, float spread, Color glowRgb)
        {
            int s = vh.currentVertCount;
            Color g = OriginInk.Over(glowRgb, glowAlpha), g0 = g; g0.a = 0;
            // 4 inner corners (core), 4 outer corners (glow → 0)
            Vector2[] inner = { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
            for (int i = 0; i < 4; i++) Vert(vh, inner[i], core);
            vh.AddTriangle(s, s + 1, s + 2); vh.AddTriangle(s, s + 2, s + 3);
            if (glowAlpha <= .002f) return;
            int f = vh.currentVertCount;
            for (int i = 0; i < 4; i++) Vert(vh, inner[i], g);
            Vector2[] outer = { new Vector2(r.xMin - spread, r.yMin - spread), new Vector2(r.xMax + spread, r.yMin - spread), new Vector2(r.xMax + spread, r.yMax + spread), new Vector2(r.xMin - spread, r.yMax + spread) };
            for (int i = 0; i < 4; i++) Vert(vh, outer[i], g0);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                vh.AddTriangle(f + i, f + 4 + i, f + 4 + j);
                vh.AddTriangle(f + i, f + 4 + j, f + j);
            }
        }
    }

    /// <summary>Base for the film's graphics: they fill the 1920×1080 stage and draw in its top-left, y-down reference coordinates.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class OriginGraphic : MaskableGraphic
    {
        /// <summary>One screen pixel in reference units (1 / stage scale); the feather width of anti-aliased edges.</summary>
        public float Pixel = 1;
        public override bool Raycast(Vector2 sp, Camera eventCamera) => false;
        protected override void Awake() { base.Awake(); raycastTarget = false; }

        /// <summary>A point in this graphic's own box (x right, y down from its top-left corner) to its local mesh space.</summary>
        protected Vector2 Pt(double x, double y)
        {
            var r = rectTransform.rect; var pv = rectTransform.pivot;
            return new Vector2((float)x - r.width * pv.x, r.height * (1 - pv.y) - (float)y);
        }
    }

    /// <summary>The neuron field: edges, travelling pulses, glowing nodes and the white-hot core of the collapse.</summary>
    public sealed class OriginFieldGraphic : OriginGraphic
    {
        public XgOriginColdOpen.Frame Frame;

        const float Sigma = 2.5f;

        static float BlurPeak(float alpha, float radius) => alpha * (1 - Mathf.Exp(-radius * radius / (2 * Sigma * Sigma)));

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var f = Frame;
            if (f == null || f.Nodes <= 0) return;
            double t = f.T, collapse = f.Collapse;
            int n = XgOriginColdOpen.NodeCount;
            var pos = new Vector2[n];
            for (int i = 0; i < n; i++)
                pos[i] = Pt(XgOriginColdOpen.Lerp(XgOriginColdOpen.NodeX[i], 960, collapse), XgOriginColdOpen.Lerp(XgOriginColdOpen.NodeY[i], 500, collapse));

            var ea = XgOriginColdOpen.EdgeA; var eb = XgOriginColdOpen.EdgeB;
            for (int k = 0; k < ea.Length; k++)
            {
                float a = (float)System.Math.Min(f.NodeVisibility(ea[k]), f.NodeVisibility(eb[k]));
                if (a <= 0) continue;
                OriginDraw.Line(vh, pos[ea[k]], pos[eb[k]], 1, OriginInk.Over(OriginInk.Lime, .13f * a), Pixel);
            }
            for (int k = 0; k < ea.Length; k += 4)
            {
                float a = (float)System.Math.Min(f.NodeVisibility(ea[k]), f.NodeVisibility(eb[k]));
                if (a <= 0) continue;
                float pulse = (float)((t * .55 + k * .137) % 1);
                var c = Vector2.Lerp(pos[ea[k]], pos[eb[k]], pulse);
                OriginDraw.Halo(vh, c, Sigma * .8f, OriginInk.Lime, BlurPeak(.7f * a, 1.8f), 12);
                OriginDraw.Disc(vh, c, 1.8f, OriginInk.Over(OriginInk.Lime, .7f * a), Pixel, 10);
            }
            for (int i = 0; i < n; i++)
            {
                float a = (float)f.NodeVisibility(i);
                if (a <= 0) continue;
                float tw = .6f + .4f * Mathf.Sin((float)(t * 2.2 + i * 1.7));
                float alpha = .5f * a * tw + .15f;
                float r = i < 4 ? 4 : 2.4f;
                if (i < 20) OriginDraw.Halo(vh, pos[i], Mathf.Sqrt(Sigma * Sigma + r * r / 3f), OriginInk.Lime, BlurPeak(alpha, r), 14);
                OriginDraw.Disc(vh, pos[i], r, OriginInk.Over(OriginInk.Lime, alpha), Pixel, r > 3 ? 18 : 12);
            }
            if (collapse > .85)
            {
                float r = (float)(6 + collapse * 6);
                var core = Pt(960, 500);
                const float big = 16;
                OriginDraw.Halo(vh, core, Mathf.Sqrt(big * big + r * r / 3f), OriginInk.Hot, 1 - Mathf.Exp(-r * r / (2 * big * big)), 28);
                OriginDraw.Disc(vh, core, r, OriginInk.Hot, Pixel, 28);
            }
        }
    }

    /// <summary>The faint 64-px blueprint grid, fading toward the edges (the film's Background pattern under a radial mask).</summary>
    public sealed class OriginGridGraphic : OriginGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            const int steps = 24;
            var rgb = new Color32(190, 235, 205, 255);
            // Pattern lines sit on the tile edges and only the half inside the tile paints: a half-pixel hairline at 4 % ink.
            for (int x = 0; x <= 1920; x += 64) Strip(vh, rgb, steps, true, x + .25);
            for (int y = 0; y <= 1080; y += 64) Strip(vh, rgb, steps, false, y + .25);
        }

        static float Fade(double x, double y)
        {
            double dx = (x - 960) / 1152.0, dy = (y - 486) / 648.0;
            return (float)System.Math.Max(0, 1 - System.Math.Sqrt(dx * dx + dy * dy));
        }

        void Strip(VertexHelper vh, Color rgb, int steps, bool vertical, double at)
        {
            double length = vertical ? 1080 : 1920;
            float w = .5f;
            float px = Pixel;
            float inner = 0, outer = px;
            float width = w < px ? w / px : 1;
            int s = vh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                double along = length * i / steps;
                double x = vertical ? at : along, y = vertical ? along : at;
                float a = .04f * Fade(x, y) * width;
                Color c = OriginInk.Over(rgb, a), e = c; e.a = 0;
                Vector2 p = Pt(x, y), nrm = vertical ? Vector2.right : Vector2.up;
                var v = UIVertex.simpleVert;
                v.position = p - nrm * outer; v.color = e; vh.AddVert(v);
                v.position = p + nrm * inner; v.color = c; vh.AddVert(v);
                v.position = p + nrm * outer; v.color = e; vh.AddVert(v);
            }
            for (int i = 0; i < steps; i++)
            {
                int a0 = s + i * 3, b0 = s + (i + 1) * 3;
                for (int k = 0; k < 2; k++)
                {
                    vh.AddTriangle(a0 + k, a0 + k + 1, b0 + k + 1);
                    vh.AddTriangle(a0 + k, b0 + k + 1, b0 + k);
                }
            }
        }
    }

    /// <summary>The 1958–2016 timeline under the counter: a grey rail, a lime fill and a tick per milestone year. Drawn in a 620×34 box, top-left origin.</summary>
    public sealed class OriginBarGraphic : OriginGraphic
    {
        public double Progress;
        public int Year = 1958;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float px = Pixel;
            OriginDraw.Line(vh, P(0, 10), P(600, 10), 1, OriginInk.Over(Color.white, .14f), px);
            OriginDraw.Line(vh, P(0, 10), P((float)(600 * Progress), 10), 2, OriginInk.Lime, px);
            foreach (int y in XgOriginColdOpen.BarYears)
            {
                float x = (y - 1958) / 58f * 600;
                OriginDraw.Line(vh, P(x, 5), P(x, 15), 1, y <= Year ? OriginInk.Lime : OriginInk.Over(Color.white, .25f), px);
            }
        }

        Vector2 P(float x, float y) => Pt(x, y);
    }

    /// <summary>The blinking lime caret after the typed answer, with its glow.</summary>
    public sealed class OriginCaretGraphic : OriginGraphic
    {
        public float Alpha = 1;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Alpha <= 0) return;
            var r = rectTransform.rect;
            var rect = new Rect(r.center.x - r.width * .5f, r.center.y - r.height * .5f, r.width, r.height);
            OriginDraw.GlowRect(vh, rect, OriginInk.Over(OriginInk.Lime, Alpha), .22f * Alpha, 9, OriginInk.Lime);
        }
    }
}
