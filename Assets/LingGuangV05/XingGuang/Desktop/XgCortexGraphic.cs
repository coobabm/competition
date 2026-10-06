using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>One concept cell as drawn on the cortex map.</summary>
    public enum XgCellKind { Dim, Yes, No, Superposed, Seed, Wiped, Off }

    public struct XgCellLook
    {
        public XgCellKind kind;
        public float strength;
        public XgCellLook(XgCellKind kind, float strength) { this.kind = kind; this.strength = strength; }
    }

    /// <summary>
    /// 皮层拓扑图: 灵光's one brain seen from the side, four regions where a cortex has its areas (推理区 behind the
    /// forehead, 读字区 above the ear, 看图区 at the back, 语气区 on top). Each region is drawn wired the way its
    /// architecture wires it (a window sliding over the picture, a loop back to the last word, gates on the loop, every
    /// word to every word...), animated, with the region's real concepts as the cells. Changing the structure plays the
    /// topology rewrite (old wires dissolve, new ones grow behind a beam); in stage 6 the borders fade and one wiring
    /// lights the whole cortex. With <see cref="GlyphArch"/> set it draws a single topology (the 接法图鉴 tiles).
    /// The look is a dark scanner screen: glow is drawn as vertex-alpha falloff (feathered lines, radial halos, comet
    /// trails), so it needs no post-processing or extra material.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgCortexGraphic : MaskableGraphic
    {
        public sealed class Region
        {
            public string id;
            public Vector2 c, label, glyph;
            public float rx, ry;
            public Color tint;
            /// <summary>Architecture id ("tone" for the tone region's own wiring, "" = not wired yet).</summary>
            public string arch = "";
            public string chip = "", chipTopo = "", chipChange = "";
            public bool training, empty;
            public readonly List<XgCellLook> cells = new List<XgCellLook>();
            public string oldArch = "";
            public float rewriteStart = -99;
            public readonly float[] axes = { .5f, .5f, .5f };
            public bool initialised;
        }

        /// <summary>The neon palette of the scanner screen.</summary>
        public static class N
        {
            public static readonly Color BgTop = new Color32(8, 13, 34, 255), BgBottom = new Color32(15, 23, 56, 255);
            public static readonly Color Pool = new Color32(48, 72, 170, 255);
            public static readonly Color Accent = new Color32(96, 150, 255, 255), AccentSoft = new Color32(96, 150, 255, 255);
            public static readonly Color Teal = new Color32(40, 230, 210, 255), Purple = new Color32(180, 130, 255, 255);
            public static readonly Color Orange = new Color32(255, 154, 60, 255), Gold = new Color32(255, 206, 90, 255);
            public static readonly Color Star = new Color32(255, 240, 190, 255), Good = new Color32(60, 240, 140, 255);
            public static readonly Color Bad = new Color32(255, 82, 110, 255), Wire = new Color32(120, 145, 220, 255);
            public static readonly Color BrainFill = new Color32(17, 26, 64, 255), Edge = new Color32(110, 180, 255, 255);
            public static readonly Color Sulcus = new Color32(70, 92, 160, 255), Dim = new Color32(50, 62, 108, 255);
            public static readonly Color Line = new Color32(64, 80, 130, 255), Text = new Color32(226, 233, 255, 255);
            public static readonly Color TextMuted = new Color32(132, 148, 200, 255), Glass = new Color32(13, 21, 50, 255);
            public static readonly Color Seed = new Color32(255, 214, 102, 255), Wiped = new Color32(150, 165, 210, 255);
        }

        struct LabelSpec
        {
            public Vector2 pos;
            public string text;
            public float size, alpha;
            public Color color, tint;
            public bool chip;
        }

        sealed class LabelView
        {
            public RectTransform rt, back;
            public Image backImage;
            public UnityEngine.UI.Outline backRim;
            public TMP_Text text;
        }

        public const float RewriteSeconds = 2.4f, UnifySeconds = 2.6f;
        static readonly Color White = Color.white;

        public readonly Region[] Regions =
        {
            new Region { id = "logic", c = new Vector2(215, 262), rx = 112, ry = 104, tint = N.Purple, label = new Vector2(205, 168), glyph = new Vector2(212, 272) },
            new Region { id = "tone", c = new Vector2(432, 150), rx = 122, ry = 72, tint = N.Orange, label = new Vector2(432, 96), glyph = new Vector2(418, 172) },
            new Region { id = "sequence", c = new Vector2(432, 356), rx = 152, ry = 74, tint = N.Teal, label = new Vector2(432, 292), glyph = new Vector2(442, 372) },
            new Region { id = "vision", c = new Vector2(652, 258), rx = 112, ry = 106, tint = N.Accent, label = new Vector2(684, 160), glyph = new Vector2(648, 272) },
        };

        /// <summary>Atlas tile mode: draw only this topology, centred, with lively demo cells.</summary>
        public string GlyphArch;
        /// <summary>Fewer flashes (the 减少特效 switch): no blooms pulsing, no ripples, no sparks, no drifting motes.</summary>
        public bool Reduced;
        public TMP_FontAsset Font;
        /// <summary>Stage 6: the whole cortex wired the same way. Cells of every region, in order.</summary>
        public bool Unified { get; private set; }
        public readonly List<XgCellLook> UnifiedCells = new List<XgCellLook>();
        public string UnifiedTitle = "";
        public readonly string[] ToneLow = { "冷静", "正经", "顺从" }, ToneHigh = { "热情", "皮", "有主见" };
        /// <summary>Region names for the faded labels of the unified view.</summary>
        public readonly string[] RegionNames = { "", "", "", "" };
        /// <summary>The words under the reading loop and the attention rows (Chinese source, English target).</summary>
        public string[] Tokens = { "这", "本", "书", "我", "看", "过" };
        public string[] Source = { "那", "只", "猫", "坐", "在", "垫" }, Target = { "cat", "sat", "on", "mat" };
        public Dictionary<string, string> Captions = new Dictionary<string, string>();

        float unifyStart = -99;
        float dw = 860, dh = 540, s = 1;
        Vector2 o;
        Rect area;
        VertexHelper vh;
        float fade = 1, revealX = float.MaxValue;
        bool grey, dashed, quiet;
        List<XgCellLook> cellsSrc;
        bool emptySrc;
        readonly List<LabelSpec> labels = new List<LabelSpec>(), shown = new List<LabelSpec>();
        readonly List<LabelView> pool = new List<LabelView>();
        static List<XgCellLook> demo;
        static Vector2[] unifiedPoints;
        static int[] heads;

        // ---------------------------------------------------------------- state from the page

        /// <summary>Set a region's wiring; a change after the first plays the topology rewrite.</summary>
        public void SetArch(int region, string arch, bool animate)
        {
            var r = Regions[region];
            arch = arch ?? "";
            if (r.initialised && r.arch == arch) return;
            if (r.initialised && animate && r.arch.Length > 0 && arch.Length > 0) { r.oldArch = r.arch; r.rewriteStart = Time.unscaledTime; }
            else r.rewriteStart = -99;
            r.arch = arch;
            r.initialised = true;
        }

        /// <summary>A rewrite is still playing in this region (the page lingers its "old → new" line meanwhile).</summary>
        public bool Rewriting(int region) => Time.unscaledTime - Regions[region].rewriteStart < RewriteSeconds + 1.2f;

        public void SetUnified(bool on, bool animate)
        {
            if (on == Unified) return;
            Unified = on;
            unifyStart = on && animate ? Time.unscaledTime : -99;
        }

        public void Animate() => SetVerticesDirty();

        // ---------------------------------------------------------------- mapping

        void Fit(Rect r)
        {
            area = r;
            dw = GlyphArch != null ? 290 : 860; dh = GlyphArch != null ? 190 : 540;
            s = Mathf.Max(.01f, Mathf.Min(r.width / dw, r.height / dh));
            o = new Vector2(r.center.x - dw * .5f * s, r.center.y + dh * .5f * s);
        }

        Vector2 P(Vector2 d) => new Vector2(o.x + d.x * s, o.y - d.y * s);
        static Vector2 V(float x, float y) => new Vector2(x, y);
        static float Frac(float x) => x - Mathf.Floor(x);
        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3 - 2 * x); }
        static float Hash(int i) { float x = Mathf.Sin(i * 12.9898f + 78.233f) * 43758.547f; return x - Mathf.Floor(x); }

        Color Ink(Color c, float a = 1)
        {
            if (grey) c = Color.Lerp(c, N.Wiped, .75f);
            c.a *= Mathf.Clamp01(a) * fade;
            return c;
        }

        static Color Clear(Color c) { c.a = 0; return c; }

        bool Hidden(float x) => x > revealX;

        // ---------------------------------------------------------------- soft primitives (screen space)

        void Quad4(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
        {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.position = a; v.color = ca; vh.AddVert(v);
            v.position = b; v.color = cb; vh.AddVert(v);
            v.position = c; v.color = cc; vh.AddVert(v);
            v.position = d; v.color = cd; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>A line with a solid core and edges that fade to nothing: anti-aliased, and a glow when the feather is wide.</summary>
        void SoftSeg(Vector2 a, Vector2 b, float core, float feather, Color col)
        {
            var d = b - a; if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized;
            Vector2 c0 = n * (core * .5f), f0 = n * (core * .5f + feather);
            var edge = Clear(col);
            Quad4(a - c0, a + c0, b + c0, b - c0, col, col, col, col);
            Quad4(a + c0, a + f0, b + f0, b + c0, col, edge, edge, col);
            Quad4(a - f0, a - c0, b - c0, b - f0, edge, col, col, edge);
        }

        /// <summary>A disc that is brightest at the centre and fades out at the rim (halo, bloom, nebula).</summary>
        void HaloScreen(Vector2 c, float rx, float ry, Color col, int n = 20)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = col; v.position = c; vh.AddVert(v);
            v.color = Clear(col);
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.position = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); vh.AddVert(v); }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % n);
        }

        /// <summary>A solid disc whose rim fades out over <paramref name="feather"/> (anti-aliased), 1 + 2n vertices.</summary>
        void SoftDiscScreen(Vector2 c, float r, float feather, Color col, int n)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = col; v.position = c; vh.AddVert(v);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                v.color = col; v.position = c + dir * r; vh.AddVert(v);
                v.color = Clear(col); v.position = c + dir * (r + feather); vh.AddVert(v);
            }
            for (int i = 0; i < n; i++)
            {
                int a = start + 1 + i * 2, b = start + 1 + (i + 1) % n * 2;
                vh.AddTriangle(start, a, b);
                vh.AddTriangle(a, a + 1, b + 1); vh.AddTriangle(a, b + 1, b);
            }
        }

        /// <summary>A ring with feathered inner and outer edges.</summary>
        void SoftRingScreen(Vector2 c, float r, float w, float feather, Color col, int n = 48)
        {
            // a hairline ring is a ridge (clear, lit, clear); a wider one keeps a solid band in the middle
            bool thin = w < 1;
            float[] radii = thin ? new[] { Mathf.Max(0, r - feather), r, r + feather }
                                 : new[] { Mathf.Max(0, r - w * .5f - feather), Mathf.Max(0, r - w * .5f), r + w * .5f, r + w * .5f + feather };
            int m = radii.Length;
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI * 2 / n; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (int k = 0; k < m; k++) { v.position = c + dir * radii[k]; v.color = k == 0 || k == m - 1 ? Clear(col) : col; vh.AddVert(v); }
            }
            for (int i = 0; i < n; i++)
                for (int k = 0; k < m - 1; k++)
                {
                    int p = start + i * m + k, q = start + (i + 1) * m + k;
                    vh.AddTriangle(p, p + 1, q + 1); vh.AddTriangle(p, q + 1, q);
                }
        }

        // ---------------------------------------------------------------- primitives (design coordinates, y down)

        /// <summary>A wire: soft-edged; <paramref name="glow"/> adds a wide faint halo around it.</summary>
        void L(Vector2 a, Vector2 b, float w, Color c, float alpha = 1, float glow = 0)
        {
            if (vh == null || Hidden((a.x + b.x) * .5f) || alpha * fade <= .004f) return;
            if (dashed)
            {
                float len = Vector2.Distance(a, b); if (len < 1e-3f) return;
                var dir = (b - a) / len;
                bool was = dashed; dashed = false;
                for (float t = 0; t < len; t += 8) L(a + dir * t, a + dir * Mathf.Min(len, t + 4), w, c, alpha, glow);
                dashed = was;
                return;
            }
            var col = Ink(c, alpha);
            // faint wiring (most of the stage-6 web) is one plain quad: its edge cannot be seen anyway
            if (glow <= 0 && col.a < .08f) { XgDraw.Seg(vh, P(a), P(b), w * .9f * s, col); return; }
            if (glow > 0 && !grey) SoftSeg(P(a), P(b), w * s, w * 3.5f * s * glow, Ink(c, alpha * .22f));
            SoftSeg(P(a), P(b), w * .7f * s, Mathf.Max(.8f, w * .45f) * s, col);
        }

        void Disc(Vector2 c, float r, Color col, float alpha = 1, int n = 16)
        {
            if (vh == null || Hidden(c.x) || alpha * fade <= .004f) return;
            if (r * s < 2.5f) { XgDraw.Disc(vh, P(c), r * s, Ink(col, alpha), 8); return; }
            SoftDiscScreen(P(c), r * s, .9f, Ink(col, alpha), n);
        }

        void Halo(Vector2 c, float r, Color col, float alpha = 1, int n = 20)
        {
            if (vh == null || Hidden(c.x) || alpha * fade <= .004f) return;
            HaloScreen(P(c), r * s, r * s, Ink(col, alpha), n);
        }

        void HaloEllipse(Vector2 c, float rx, float ry, Color col, float alpha)
        {
            if (vh == null || alpha * fade <= .004f) return;
            HaloScreen(P(c), rx * s, ry * s, Ink(col, alpha), 40);
        }

        void Ring(Vector2 c, float r, float w, Color col, float alpha = 1, int n = 18)
        {
            if (vh == null || Hidden(c.x) || alpha * fade <= .004f) return;
            SoftRingScreen(P(c), r * s, w * .7f * s, Mathf.Max(.8f, w * .4f) * s, Ink(col, alpha), n);
        }

        void GlowRing(Vector2 c, float r, float w, Color col, float alpha, int n = 56)
        {
            if (vh == null || Hidden(c.x) || alpha * fade <= .004f) return;
            SoftRingScreen(P(c), r * s, w * s, w * 4 * s, Ink(col, alpha * .25f), n);
            SoftRingScreen(P(c), r * s, w * .6f * s, w * .6f * s, Ink(col, alpha), n);
        }

        void Box(Vector2 min, Vector2 max, Color col, float alpha = 1)
        {
            if (vh == null || Hidden((min.x + max.x) * .5f) || alpha * fade <= .004f) return;
            var a = P(min); var b = P(max);
            XgDraw.Box(vh, new Vector2(a.x, b.y), new Vector2(b.x, a.y), Ink(col, alpha));
        }

        void Frame(Vector2 min, Vector2 max, float w, Color col, float alpha = 1, bool dash = false, float glow = 0)
        {
            bool was = dashed; dashed |= dash;
            L(min, V(max.x, min.y), w, col, alpha, glow); L(V(max.x, min.y), max, w, col, alpha, glow);
            L(max, V(min.x, max.y), w, col, alpha, glow); L(V(min.x, max.y), min, w, col, alpha, glow);
            dashed = was;
        }

        static Vector2 Bez(Vector2 a, Vector2 c1, Vector2 c2, Vector2 b, float t)
        {
            float u = 1 - t;
            return u * u * u * a + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * b;
        }

        void Curve(Vector2 a, Vector2 c1, Vector2 c2, Vector2 b, float w, Color col, float alpha = 1, int n = 14, float glow = 0)
        {
            var prev = a;
            for (int i = 1; i <= n; i++)
            {
                var p = Bez(a, c1, c2, b, i / (float)n);
                if (!dashed || i % 2 == 1) { bool was = dashed; dashed = false; L(prev, p, w, col, alpha, glow); dashed = was; }
                prev = p;
            }
        }

        void Head(Vector2 tip, Vector2 from, float size, Color col, float alpha = 1)
        {
            if (vh == null || Hidden(tip.x) || alpha * fade <= .004f) return;
            var d = (tip - from).normalized; var n = new Vector2(-d.y, d.x);
            XgDraw.Poly(vh, new[] { P(tip), P(tip - d * size + n * size * .55f), P(tip - d * size - n * size * .55f) }, Ink(col, alpha));
        }

        void Arrow(Vector2 a, Vector2 b, float w, Color col, float alpha = 1)
        {
            var d = (b - a).normalized;
            L(a, b - d * 4, w, col, alpha);
            Head(b, a, 6, col, alpha);
        }

        void Fan(Vector2 center, IList<Vector2> pts, Color col)
        {
            if (vh == null) return;
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = Ink(col);
            v.position = P(center); vh.AddVert(v);
            foreach (var p in pts) { v.position = P(p); vh.AddVert(v); }
            for (int i = 0; i < pts.Count; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % pts.Count);
        }

        void EllipseDashed(Vector2 c, float rx, float ry, float w, Color col, float alpha)
        {
            const int n = 56;
            for (int i = 0; i < n; i += 2)
            {
                float a0 = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
                L(c + V(Mathf.Cos(a0) * rx, Mathf.Sin(a0) * ry), c + V(Mathf.Cos(a1) * rx, Mathf.Sin(a1) * ry), w, col, alpha);
            }
        }

        /// <summary>A glowing signal: bright core, white heart, wide bloom.</summary>
        void Orb(Vector2 c, float r, Color col, float alpha = 1)
        {
            Halo(c, r * 4.5f, col, (Reduced ? .25f : .45f) * alpha);
            Disc(c, r, col, alpha);
            Disc(c, r * .45f, White, .9f * alpha);
        }

        /// <summary>A signal travelling along a path with a fading trail behind it (path parameter 0..1, head at <paramref name="at"/>).</summary>
        void Comet(Func<float, Vector2> path, float at, float span, float r, Color col, float alpha = 1)
        {
            const int n = 9;
            for (int i = n - 1; i >= 1; i--)
            {
                float u = at - span * i / n;
                if (u < 0) continue;
                float k = 1 - i / (float)n;
                var p = path(u);
                if (vh != null && !Hidden(p.x)) XgDraw.Disc(vh, P(p), r * (.35f + .55f * k) * s, Ink(col, .75f * k * k * alpha), 8);
            }
            Orb(path(at), r, col, alpha);
        }

        void Label(Vector2 pos, string text, float size, Color color, float alpha = 1)
        {
            if (quiet || string.IsNullOrEmpty(text) || Hidden(pos.x) || alpha * fade <= .02f) return;
            if (grey) color = Color.Lerp(color, N.Wiped, .75f);
            labels.Add(new LabelSpec { pos = pos, text = text, size = size, color = color, alpha = alpha * fade });
        }

        void Chip(Vector2 pos, string text, Color tint, float alpha = 1)
        {
            if (quiet || string.IsNullOrEmpty(text) || alpha * fade <= .02f) return;
            labels.Add(new LabelSpec { pos = pos, text = text, size = 13, color = N.Text, tint = tint, alpha = alpha * fade, chip = true });
        }

        // ---------------------------------------------------------------- cells: glowing orbs

        XgCellLook Look(int slot)
        {
            if (cellsSrc != null && slot < cellsSrc.Count) return cellsSrc[slot];
            return new XgCellLook(emptySrc ? XgCellKind.Wiped : XgCellKind.Dim, 0);
        }

        void Cell(Vector2 c, float r, int slot) => CellAs(c, r, Look(slot));

        void CellAs(Vector2 c, float r, XgCellLook look)
        {
            if (vh == null || Hidden(c.x)) return;
            float k = Mathf.Clamp01(look.strength);
            switch (look.kind)
            {
                case XgCellKind.Yes: Lit(c, r, N.Good, k); break;
                case XgCellKind.No: Lit(c, r, N.Bad, k); break;
                case XgCellKind.Superposed:
                    Halo(c, r * 2.6f, N.Purple, .3f);
                    XgDraw.Pie(vh, P(c), r * s, Mathf.PI * .5f, Mathf.PI * 1.5f, Ink(Color.Lerp(N.Dim, N.Good, .75f)), 12);
                    XgDraw.Pie(vh, P(c), r * s, -Mathf.PI * .5f, Mathf.PI * .5f, Ink(Color.Lerp(N.Dim, N.Bad, .75f)), 12);
                    Ring(c, r + .6f, 2.2f, N.Purple);
                    break;
                case XgCellKind.Seed:
                    Halo(c, r * 2.8f, N.Seed, .35f);
                    Disc(c, r, new Color32(40, 34, 20, 255)); Ring(c, r - .4f, 1.8f, N.Seed); Disc(c, r * .32f, N.Seed);
                    break;
                case XgCellKind.Wiped:
                    for (int i = 0; i < 8; i++)
                        XgDraw.Arc(vh, P(c), (r - .5f) * s, 1.3f * s, i * Mathf.PI / 4, i * Mathf.PI / 4 + Mathf.PI / 8, Ink(N.Wiped, .55f), 3);
                    break;
                case XgCellKind.Off:
                    Disc(c, r, N.Dim, .55f); Ring(c, r - .5f, 1.2f, N.Line);
                    break;
                default:
                    Disc(c, r, N.Dim); Ring(c, r - .5f, 1, N.Line, .8f);
                    break;
            }
        }

        /// <summary>A learnt concept: a halo as strong as it is sure, the orb, a rim of light and a highlight.</summary>
        void Lit(Vector2 c, float r, Color col, float k)
        {
            Halo(c, r * 2.7f, col, .1f + .3f * k);
            Disc(c, r, Color.Lerp(N.Dim, col, .35f + .65f * k));
            Ring(c, r - .5f, 1.1f, Color.Lerp(col, White, .45f), .35f + .4f * k);
            Disc(c + V(-r * .32f, -r * .32f), r * .3f, White, .15f + .35f * k);
        }

        static List<XgCellLook> Demo()
        {
            if (demo != null) return demo;
            demo = new List<XgCellLook>();
            var rng = new System.Random(7);
            for (int i = 0; i < 64; i++)
            {
                double r = rng.NextDouble();
                var kind = r < .45 ? XgCellKind.Yes : r < .7 ? XgCellKind.No : r < .8 ? XgCellKind.Superposed : r < .86 ? XgCellKind.Seed : XgCellKind.Dim;
                demo.Add(new XgCellLook(kind, (float)(.35 + .65 * rng.NextDouble())));
            }
            return demo;
        }

        // ---------------------------------------------------------------- mesh

        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();
            vh = v; labels.Clear();
            fade = 1; revealX = float.MaxValue; grey = dashed = quiet = false;
            Fit(GetPixelAdjustedRect());
            float t = Time.unscaledTime;
            try
            {
                Backdrop(t);
                if (GlyphArch != null)
                {
                    cellsSrc = Demo(); emptySrc = false;
                    Glyph(GlyphArch, GlyphCenter(GlyphArch), t, 1, null);
                    return;
                }
                float u = Unified ? (unifyStart < 0 ? 1 : Smooth((t - unifyStart) / UnifySeconds)) : 0;
                Motes(t);
                Brain(t, u);
                if (u < 1)
                    for (int i = 0; i < Regions.Length; i++) { fade = 1 - u; DrawRegion(Regions[i], i, t); }
                fade = 1;
                if (u > 0) DrawUnified(t, u);
            }
            finally { vh = null; }
        }

        static Vector2 GlyphCenter(string arch) => arch == "rnn" || arch == "lstm" || arch == "gru" ? V(145, 96) : V(145, 92);

        // ---------------------------------------------------------------- backdrop: a dark scanner screen

        void Backdrop(float t)
        {
            var r = area;
            Quad4(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), N.BgBottom, N.BgTop, N.BgTop, N.BgBottom);
            HaloScreen(r.center, r.width * .55f, r.height * .6f, Ink(N.Pool, .28f), 40);
            // a faint dot grid, like a scope's graticule
            float step = 26;
            for (float y = step * .5f; y < dh; y += step)
                for (float x = step * .5f; x < dw; x += step)
                    XgDraw.Disc(vh, P(V(x, y)), .9f * s, Ink(N.Accent, .1f), 4);
            if (Reduced) return;
            // a slow scan band moving down
            float band = Frac(t * .07f) * (dh + 120) - 60;
            Vector2 a = P(V(0, band - 40)), m = P(V(0, band)), b = P(V(0, band + 40));
            float x0 = r.xMin, x1 = r.xMax;
            var lit = Ink(N.Accent, .06f); var none = Clear(lit);
            Quad4(new Vector2(x0, a.y), new Vector2(x0, m.y), new Vector2(x1, m.y), new Vector2(x1, a.y), none, lit, lit, none);
            Quad4(new Vector2(x0, m.y), new Vector2(x0, b.y), new Vector2(x1, b.y), new Vector2(x1, m.y), lit, none, none, lit);
        }

        /// <summary>Dust drifting slowly through the screen, for depth.</summary>
        void Motes(float t)
        {
            if (Reduced) return;
            for (int i = 0; i < 28; i++)
            {
                float speed = 4 + 10 * Hash(i * 3 + 1);
                float x = Frac(Hash(i) + t * speed / dw) * dw;
                float y = Hash(i * 7 + 2) * dh + Mathf.Sin(t * .4f + i) * 8;
                float twinkle = .5f + .5f * Mathf.Sin(t * (1 + Hash(i + 9)) + i * 2.3f);
                float r = .8f + 1.4f * Hash(i * 5 + 4);
                Halo(V(x, y), r * 4, N.Accent, .08f * twinkle);
                Disc(V(x, y), r, N.Text, .18f + .25f * twinkle);
            }
        }

        // ---------------------------------------------------------------- brain

        static readonly Vector2[][] BrainPath =
        {
            new[] { V(95, 290), V(80, 190), V(160, 95), V(290, 78) },
            new[] { V(290, 78), V(390, 55), V(560, 52), V(660, 95) },
            new[] { V(660, 95), V(760, 140), V(800, 230), V(775, 310) },
            new[] { V(775, 310), V(760, 360), V(715, 385), V(670, 392) },
            new[] { V(670, 392), V(650, 425), V(600, 445), V(545, 438) },
            new[] { V(545, 438), V(500, 470), V(430, 472), V(385, 445) },
            new[] { V(385, 445), V(320, 458), V(220, 440), V(170, 400) },
            new[] { V(170, 400), V(120, 370), V(100, 335), V(95, 290) },
        };
        static readonly Vector2[][] StemPath =
        {
            new[] { V(600, 430), V(590, 470), V(615, 500), V(640, 520) },
            new[] { V(640, 520), V(650, 520), V(660, 520), V(668, 520) },
            new[] { V(668, 520), V(650, 490), V(650, 460), V(660, 430) },
            new[] { V(660, 430), V(640, 430), V(620, 430), V(600, 430) },
        };
        static readonly Vector2[][] Sulci =
        {
            new[] { V(300, 85), V(330, 150), V(300, 220), V(340, 290) },
            new[] { V(520, 70), V(500, 140), V(545, 200), V(520, 268) },
            new[] { V(140, 330), V(230, 300), V(330, 300), V(420, 268) },
            new[] { V(560, 300), V(610, 320), V(690, 315), V(760, 285) },
            new[] { V(250, 420), V(300, 400), V(340, 410), V(380, 440) },
        };
        static List<Vector2> brainOutline, stemOutline;

        static List<Vector2> Sample(Vector2[][] path, int per)
        {
            var pts = new List<Vector2>();
            foreach (var seg in path) for (int i = 0; i < per; i++) pts.Add(Bez(seg[0], seg[1], seg[2], seg[3], i / (float)per));
            return pts;
        }

        void Brain(float t, float u)
        {
            brainOutline = brainOutline ?? Sample(BrainPath, 16);
            stemOutline = stemOutline ?? Sample(StemPath, 8);
            Fan(V(632, 470), stemOutline, new Color32(22, 32, 72, 255));
            HaloEllipse(V(690, 428), 92, 52, N.Edge, .07f);
            if (vh != null) XgDraw.Ellipse(vh, P(V(690, 428)), 78 * s, 40 * s, Ink(new Color32(20, 30, 70, 255)), 40);
            for (int i = 0; i < 40; i++)
            {
                float a0 = i * Mathf.PI * 2 / 40, a1 = (i + 1) * Mathf.PI * 2 / 40;
                L(V(690 + Mathf.Cos(a0) * 78, 428 + Mathf.Sin(a0) * 40), V(690 + Mathf.Cos(a1) * 78, 428 + Mathf.Sin(a1) * 40), 1.2f, N.Edge, .4f);
            }
            for (int i = 0; i < 5; i++)
                Curve(V(628 + i * 6, 410 + i * 7), V(660, 404 + i * 7), V(720, 404 + i * 7), V(752 - i * 4, 418 + i * 6), 1, N.Sulcus, .5f, 10);
            Fan(V(430, 270), brainOutline, N.BrainFill);
            for (int i = 0; i < Regions.Length; i++)
            {
                var r = Regions[i];
                float breathe = Reduced ? 0 : .05f * Mathf.Sin(t * .8f + i * 1.7f);
                float a = (.24f + breathe + (r.training ? .14f : 0)) * (1 - .6f * u);
                HaloEllipse(r.c, r.rx * 1.08f, r.ry * 1.08f, r.tint, a);
                HaloEllipse(r.c, r.rx * .55f, r.ry * .55f, r.tint, a * .5f);
            }
            foreach (var c in Sulci) Curve(c[0], c[1], c[2], c[3], 2, N.Sulcus, .4f, 12);
            foreach (var r in Regions) EllipseDashed(r.c, r.rx, r.ry, 1.2f, r.tint, .4f * (1 - .85f * u));
            int n = brainOutline.Count;
            for (int i = 0; i < n; i++) L(brainOutline[i], brainOutline[(i + 1) % n], 1.8f, N.Edge, .85f, 1.2f);
            if (Reduced) return;
            // a light running round the cortex
            float head = Frac(t * .05f) * n;
            for (int k = 0; k < 22; k++)
            {
                float f = 1 - k / 22f;
                int i0 = ((int)Mathf.Floor(head) - k + n * 2) % n;
                L(brainOutline[i0], brainOutline[(i0 + 1) % n], 2.4f, Color.Lerp(N.Edge, White, f * .7f), f * f, 1.6f);
            }
            Orb(brainOutline[(int)Mathf.Floor(head) % n], 2.6f, N.Edge);
        }

        void DrawRegion(Region r, int index, float t)
        {
            cellsSrc = r.cells; emptySrc = r.empty;
            float k = r.training ? 1.8f : 1;
            if (r.training && !Reduced)
                for (int i = 0; i < 2; i++)
                {
                    float ph = Frac(t * .6f + i * .5f);
                    GlowRing(r.glyph, 30 + 120 * ph, 1.4f, r.tint, .45f * (1 - ph) * (1 - ph));
                }
            float p = (t - r.rewriteStart) / RewriteSeconds;
            if (r.arch.Length == 0) Empty(r.glyph);
            else if (p >= 0 && p < 1 && r.oldArch.Length > 0) Rewrite(r, t, k, p);
            else Glyph(r.arch, r.glyph, t, k, r);
            if (r.training)
            {
                float a = Reduced ? .7f : .5f + .3f * Mathf.Sin(t * 3);
                Frame(r.glyph - V(152, 58), r.glyph + V(152, 64), 1.2f, N.Gold, a, true);
            }
            string chip = r.chip + "\n<size=85%><color=#" + (r.arch.Length > 0 ? "8FB3FF" : "6C789E") + ">" + r.chipTopo + "</color></size>";
            if (r.chipChange.Length > 0) chip = "<size=85%><color=#FF6B81>" + r.chipChange + "</color></size>\n" + chip;
            Chip(r.label, chip, r.tint, r.arch.Length > 0 ? 1 : .6f);
            if (Captions.TryGetValue(r.arch.Length > 0 ? r.arch : "", out var cap)) Label(r.glyph + CaptionOffset(r.arch), cap, 10.5f, N.TextMuted);
        }

        /// <summary>The topology rewrite: the old wires go grey and fall apart, a beam crosses the region, the new wires grow behind it.</summary>
        void Rewrite(Region r, float t, float k, float p)
        {
            float f = fade;
            grey = true; dashed = true; quiet = true; fade = f * (1 - p) * .7f;
            Glyph(r.oldArch, r.glyph, t, k, r);
            grey = false; dashed = false; quiet = false; fade = f;
            float left = r.glyph.x - 150, right = r.glyph.x + 150;
            revealX = Mathf.Lerp(left, right, Smooth(p));
            Glyph(r.arch, r.glyph, t, k, r);
            float x = revealX; revealX = float.MaxValue;
            float beam = 1 - p * p;
            Vector2 top = V(x, r.glyph.y - 70), bottom = V(x, r.glyph.y + 72);
            L(top, bottom, 10, N.Teal, .18f * beam, 1.5f);
            L(top, bottom, 2.2f, White, .95f * beam, 1);
            Orb(top, 2.5f, N.Teal, beam); Orb(bottom, 2.5f, N.Teal, beam);
            if (Reduced) return;
            for (int i = 0; i < 18; i++)
            {
                float ph = Frac(p * 2.2f + Hash(i));
                var spark = V(x + ph * (18 + 40 * Hash(i + 3)), r.glyph.y - 60 + Hash(i + 7) * 130 - ph * 18);
                float a = (1 - ph) * beam;
                Halo(spark, 5, N.Teal, .4f * a);
                Disc(spark, 1.1f + Hash(i + 11), White, .9f * a);
            }
        }

        static Vector2 CaptionOffset(string arch)
        {
            switch (arch)
            {
                case "lenet": case "alexnet": case "vgg": case "googlenet": return V(-78, 70);
                case "rnn": case "lstm": case "gru": return V(-4, 60);
                case "transformer": return V(0, 86);
                case "resnet": return V(0, 40);
                case "tone": return V(0, 50);
                case "": return V(0, 36);
                default: return V(0, 68);
            }
        }

        // ---------------------------------------------------------------- topologies

        void Glyph(string arch, Vector2 c, float t, float k, Region r)
        {
            switch (arch)
            {
                case "perceptron": Perceptron(c, t, k); break;
                case "mlp": Mlp(c, t, k); break;
                case "lenet": case "alexnet": case "vgg": case "googlenet": Conv(c, t, k, arch); break;
                case "resnet": Resnet(c, t, k); break;
                case "rnn": Recurrent(c, t, k, 0); break;
                case "lstm": Recurrent(c, t, k, 3); break;
                case "gru": Recurrent(c, t, k, 2); break;
                case "seq2seq": Seq2Seq(c, t, k); break;
                case "attention": Attention(c, t, k); break;
                case "textcnn": TextCnn(c, t, k); break;
                case "transformer": Transformer(c, t, k); break;
                case "caption": Caption(c, t, k); break;
                case "tone": Tone(c, t, r); break;
                default: Empty(c); break;
            }
        }

        void Perceptron(Vector2 c, float t, float k)
        {
            var ins = new Vector2[4]; for (int i = 0; i < 4; i++) ins[i] = c + V(-62, -39 + i * 26);
            Vector2 yes = c + V(58, -14), no = c + V(58, 14);
            float ph = Frac(t * .6f * k), flash = Reduced ? 0 : Mathf.Exp(-ph * 6);
            foreach (var a in ins) foreach (var b in new[] { yes, no }) L(a, b, 1.3f, N.Purple, .3f + .45f * flash, .6f + flash);
            foreach (var a in ins) foreach (var b in new[] { yes, no }) { var from = a; var to = b; Comet(x => Vector2.Lerp(from, to, x), ph, .25f, 1.6f, N.Gold, 1 - ph * .6f); }
            for (int i = 0; i < 4; i++) Cell(ins[i], 9, i);
            CellAs(yes, 11, new XgCellLook(XgCellKind.Yes, .9f)); CellAs(no, 11, new XgCellLook(XgCellKind.No, .8f));
            Label(yes + V(22, 0), "是", 11, N.Good); Label(no + V(22, 0), "否", 11, N.Bad);
        }

        void Mlp(Vector2 c, float t, float k)
        {
            float[] xs = { -80, -25, 30, 80 }; int[] ns = { 4, 5, 5, 2 };
            var layers = new Vector2[4][];
            for (int l = 0; l < 4; l++) { layers[l] = new Vector2[ns[l]]; for (int i = 0; i < ns[l]; i++) layers[l][i] = c + V(xs[l], (i - (ns[l] - 1) * .5f) * 24); }
            float ph = t * 1.5f * k; int active = (int)Mathf.Floor(ph) % 3; float fr = Frac(ph);
            for (int l = 0; l < 3; l++)
                foreach (var a in layers[l]) foreach (var b in layers[l + 1])
                {
                    L(a, b, 1, N.Purple, l == active ? .5f : .16f, l == active ? 1 : 0);
                    if (l == active) Disc(Vector2.Lerp(a, b, fr), 1.6f, N.Gold, .9f);
                }
            int slot = 0;
            for (int l = 0; l < 3; l++) foreach (var p in layers[l]) Cell(p, 8, slot++);
            CellAs(layers[3][0], 10, new XgCellLook(XgCellKind.Yes, .9f)); CellAs(layers[3][1], 10, new XgCellLook(XgCellKind.No, .8f));
        }

        void Conv(Vector2 c, float t, float k, string variant)
        {
            const float st = 17, s2 = 22;
            float x0 = c.x - 112, y0 = c.y - 34, x1 = c.x - 4, y1 = c.y - 22;
            bool deep = variant == "alexnet" || variant == "vgg";
            float outX = deep ? c.x + 100 : c.x + 90;
            var outs = new[] { V(outX, c.y - 26), V(outX, c.y), V(outX, c.y + 26) };
            Vector2 G(int i, int j) => V(x0 + i * st, y0 + j * st);
            Vector2 L2(int i, int j) => V(x1 + i * s2, y1 + j * s2);
            float ph = t * 1.1f * k; int step = (int)Mathf.Floor(ph) % 9, prev = (step + 8) % 9;
            float e = Smooth(Frac(ph) / .35f);
            int px = step % 3, py = step / 3;
            var win = Vector2.Lerp(G(prev % 3, prev / 3), G(px, py), e);
            var mid = new List<Vector2>();
            if (variant == "alexnet") for (int i = 0; i < 4; i++) mid.Add(V(c.x + 64, c.y - 33 + i * 22));
            if (variant == "vgg") for (int col = 0; col < 3; col++) for (int i = 0; i < 4; i++) mid.Add(V(c.x + 56 + col * 13, c.y - 27 + i * 18));
            var lastLayer = new List<Vector2>();
            for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++) lastLayer.Add(L2(i, j));
            if (mid.Count > 0)
            {
                int firstCol = variant == "vgg" ? 4 : mid.Count;
                foreach (var a in lastLayer) for (int m = 0; m < firstCol; m++) L(a, mid[m], .7f, N.Wire, .3f);
                if (variant == "vgg") for (int col = 0; col < 2; col++) for (int a = 0; a < 4; a++) for (int b = 0; b < 4; b++) L(mid[col * 4 + a], mid[col * 4 + 4 + b], .6f, N.Wire, .3f);
                int lastStart = variant == "vgg" ? 8 : 0;
                for (int m = lastStart; m < mid.Count; m++) foreach (var b in outs) L(mid[m], b, .7f, N.Wire, .35f);
            }
            else foreach (var a in lastLayer) foreach (var b in outs) L(a, b, .8f, N.Wire, .35f);
            if (variant == "googlenet")
            {
                Frame(G(0, 0) - V(10, 10), G(4, 4) + V(10, 10), 1.2f, N.Purple, .5f, true);
                L(win + V(st, st), L2(px, py), 1.2f, N.Gold, .8f * e, 1);
            }
            var prevWin = G(prev % 3, prev / 3);
            Frame(prevWin - V(10, 10), prevWin + V(2 * st + 10, 2 * st + 10), 1, N.Accent, .35f * (1 - e) + .12f, true);
            Box(win - V(10, 10), win + V(2 * st + 10, 2 * st + 10), N.AccentSoft, .14f);
            Frame(win - V(10, 10), win + V(2 * st + 10, 2 * st + 10), 1.8f, N.Accent, 1, false, 1.2f);
            for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++) L(win + V(i * st, j * st), L2(px, py), 1, N.Accent, .2f + .6f * e, e);
            int slot = 0;
            for (int j = 0; j < 5; j++) for (int i = 0; i < 5; i++) Cell(G(i, j), 6, slot++);
            if (variant == "googlenet") Frame(win + V(st, st) - V(8, 8), win + V(st, st) + V(8, 8), 1.5f, N.Gold, 1, false, 1);
            for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++) Cell(L2(i, j), 8, slot++);
            GlowRing(L2(px, py), 11.5f, 1.2f, N.Accent, e);
            foreach (var m in mid) Cell(m, variant == "vgg" ? 4.5f : 7, slot++);
            foreach (var b in outs) Cell(b, 8, slot++);
            Arrow(V(x0 + st * 2.6f, y0 - 17), V(x0 + st * 3.9f, y0 - 17), 1.5f, N.Accent);
            if (e > .98f && !Reduced) Halo(L2(px, py), 22, N.Accent, .5f * (1 - Frac(ph)));
        }

        void Resnet(Vector2 c, float t, float k)
        {
            var xs = new float[5]; for (int i = 0; i < 5; i++) xs[i] = c.x - 96 + i * 48;
            for (int i = 0; i < 4; i++) Arrow(V(xs[i] + 12, c.y), V(xs[i + 1] - 13, c.y), 1.5f, N.Accent);
            int[][] arcs = { new[] { 0, 2 }, new[] { 2, 4 } };
            foreach (var a in arcs)
            {
                Vector2 p0 = V(xs[a[0]], c.y - 16), p1 = V(xs[a[0]] + 10, c.y - 62), p2 = V(xs[a[1]] - 10, c.y - 62), p3 = V(xs[a[1]], c.y - 18);
                Curve(p0, p1, p2, p3, 2, N.Gold, .9f, 14, 1.2f);
                Head(p3, Bez(p0, p1, p2, p3, .9f), 6, N.Gold);
            }
            for (int i = 0; i < 5; i++)
            {
                Box(V(xs[i] - 11, c.y - 15), V(xs[i] + 11, c.y + 15), N.BrainFill);
                Frame(V(xs[i] - 11, c.y - 15), V(xs[i] + 11, c.y + 15), 1, N.Wire, .8f);
                Cell(V(xs[i], c.y - 5), 4.5f, i * 2); Cell(V(xs[i], c.y + 6), 4.5f, i * 2 + 1);
            }
            float ph = Frac(t * .45f * k);
            Comet(x => V(Mathf.Lerp(xs[0], xs[4], x), c.y + 22), ph, .3f, 2.6f, N.Accent);
            float fast = Frac(t * .9f * k); int arc = fast < .5f ? 0 : 1; float local = (fast % .5f) * 2;
            var ar = arcs[arc];
            Comet(x => Bez(V(xs[ar[0]], c.y - 16), V(xs[ar[0]] + 10, c.y - 62), V(xs[ar[1]] - 10, c.y - 62), V(xs[ar[1]], c.y - 18), x), local, .35f, 2.8f, N.Gold);
            Label(c + V(0, -52), Captions.TryGetValue("resnet.skip", out var skip) ? skip : "", 10.5f, N.Gold);
        }

        void Recurrent(Vector2 c, float t, float k, int gates)
        {
            const int n = 6; const float r = 11;
            float head = (t * .55f * k) % (n + 1.5f);
            Color[] gateColors = gates == 2 ? new[] { N.Good, N.Accent } : new[] { N.Good, N.Bad, N.Accent };
            for (int i = 0; i < n; i++)
            {
                float x = c.x - 125 + i * 50, y = c.y + 8;
                if (i < n - 1) Arrow(V(x + r + 2, y), V(x + 50 - r - 3, y), 1.5f, N.Teal);
                float mem = gates > 0 ? (i <= head ? .95f : .3f) : (i <= head ? .15f + .85f * Mathf.Exp(-(head - i) * .55f) : .2f);
                Vector2 a = V(x + 6, y - r), c1 = V(x + 24, y - r - 34), c2 = V(x - 24, y - r - 34), b = V(x - 6, y - r - 1);
                Curve(a, c1, c2, b, gates > 0 ? 2 : 1.7f, N.Teal, mem, 14, mem);
                Head(b, Bez(a, c1, c2, b, .85f), 5.5f, N.Teal, mem);
                if (i <= head) Comet(u => Bez(a, c1, c2, b, u), Frac(t * 1.2f * k + i * .17f), .3f, 1.6f, N.Teal, mem);
                for (int j = 0; j < gates; j++)
                {
                    float open = .5f + .5f * Mathf.Sin(t * 2.4f * k + i * .9f + j * 2.1f);
                    float gx = x + (j - (gates - 1) * .5f) * 9, h = 3 + 5 * open;
                    if (!Reduced) Halo(V(gx, y - r - 26 - h * .5f), 7, gateColors[j], .3f * open);
                    Box(V(gx - 3.5f, y - r - 26 - h), V(gx + 3.5f, y - r - 26), gateColors[j], .4f + .6f * open);
                }
                if (!Reduced) Halo(V(x, y), r * 3, N.Gold, .35f * Mathf.Exp(-Mathf.Abs(head - i) * 2));
                Cell(V(x, y), r, i);
                if (Tokens != null && i < Tokens.Length) Label(V(x, y + r + 15), Tokens[i], 11.5f, N.TextMuted);
            }
            if (head < n - 1)
            {
                float x0 = c.x - 125;
                Comet(u => V(x0 + u * 50 * (n - 1), c.y + 8), head / (n - 1), .12f, 3.4f, N.Gold);
            }
        }

        void Seq2Seq(Vector2 c, float t, float k)
        {
            float y1 = c.y - 18, y2 = c.y + 30; var knot = V(c.x, c.y + 6);
            var enc = new Vector2[4]; var dec = new Vector2[4];
            for (int i = 0; i < 4; i++) { enc[i] = V(c.x - 110 + i * 30, y1); dec[i] = V(c.x + 20 + i * 30, y2); }
            for (int i = 0; i < 3; i++) { L(enc[i] + V(8, 0), enc[i + 1] - V(8, 0), 1.4f, N.Teal); L(dec[i] + V(8, 0), dec[i + 1] - V(8, 0), 1.4f, N.Teal); }
            Vector2 e0 = enc[3] + V(8, 0), e1 = V(knot.x - 10, y1), e2 = V(knot.x - 22, knot.y), e3 = knot - V(13, 0);
            Vector2 d0 = knot + V(13, 0), d1 = V(knot.x + 16, knot.y), d2 = V(dec[0].x - 14, y2), d3 = dec[0] - V(9, 0);
            Curve(e0, e1, e2, e3, 1.4f, N.Teal); Curve(d0, d1, d2, d3, 1.4f, N.Teal);
            float ph = Frac(t * .4f * k), squeeze = ph > .4f && ph < .58f ? Mathf.Sin((ph - .4f) / .18f * Mathf.PI) : 0;
            float dz = 11 * (1 + .35f * squeeze);
            Halo(knot, 26 + 14 * squeeze, N.Gold, .3f + .35f * squeeze);
            if (vh != null && !Hidden(knot.x))
                XgDraw.Poly(vh, new[] { P(knot + V(0, -dz)), P(knot + V(dz, 0)), P(knot + V(0, dz)), P(knot + V(-dz, 0)) }, Ink(N.Gold));
            for (int i = 0; i < 4; i++) { Cell(enc[i], 8, i); Cell(dec[i], 8, 4 + i); }
            Func<float, Vector2> path = q => q < .3f ? Vector2.Lerp(enc[0], enc[3], q / .3f) : q < .4f ? Bez(e0, e1, e2, e3, (q - .3f) / .1f) : q < .58f ? knot
                        : q < .68f ? Bez(d0, d1, d2, d3, (q - .58f) / .1f) : Vector2.Lerp(dec[0], dec[3], (q - .68f) / .32f);
            Comet(path, ph, .12f, 3, N.Gold);
            string[] src = { "我", "爱", "北", "京" }, dst = { "I", "love", "Bei", "jing" };
            for (int i = 0; i < 4; i++) { Label(enc[i] + V(0, -15), src[i], 10.5f, N.TextMuted); Label(dec[i] + V(0, 18), dst[i], 10, N.TextMuted); }
            Label(knot + V(-36, 22), Captions.TryGetValue("seq2seq.knot", out var kn) ? kn : "", 10.5f, N.Gold);
        }

        void Attention(Vector2 c, float t, float k)
        {
            var src = new Vector2[6]; var dec = new Vector2[4];
            for (int i = 0; i < 6; i++) src[i] = V(c.x - 100 + i * 40, c.y - 28);
            for (int j = 0; j < 4; j++) dec[j] = V(c.x - 45 + j * 30, c.y + 34);
            float ph = t * .7f * k; int step = (int)Mathf.Floor(ph) % 4, prev = (step + 3) % 4;
            float[] focus = { 2, 3, 4, 5 };
            float f = Mathf.Lerp(focus[prev], focus[step], Smooth(Frac(ph) / .3f));
            for (int i = 0; i < 5; i++) L(src[i] + V(9, 0), src[i + 1] - V(9, 0), 1.2f, N.Teal, .8f);
            for (int j = 0; j < 3; j++) L(dec[j] + V(9, 0), dec[j + 1] - V(9, 0), 1.2f, N.Teal, .8f);
            for (int i = 0; i < 6; i++)
            {
                float w = Mathf.Exp(-(i - f) * (i - f) / .7f);
                L(src[i], dec[step], .6f + w * 3.4f, N.Accent, .12f + .7f * w, w);
                if (w > .3f) { var from = src[i]; var to = dec[step]; Comet(u => Vector2.Lerp(from, to, u), Frac(t * 1.4f * k + i * .3f), .3f, 1.4f + w, N.Accent, w); }
            }
            for (int i = 0; i < 6; i++) { Cell(src[i], 9, i); if (Source != null && i < Source.Length) Label(src[i] + V(0, -16), Source[i], 10.5f, N.TextMuted); }
            for (int j = 0; j < 4; j++) { Cell(dec[j], 9, 6 + j); if (Target != null && j < Target.Length) Label(dec[j] + V(0, 19), Target[j], 10, j == step ? N.Text : N.TextMuted); }
            GlowRing(dec[step], 12.5f, 1.4f, N.Gold, 1);
        }

        void TextCnn(Vector2 c, float t, float k)
        {
            var low = new Vector2[7]; var up = new Vector2[5];
            for (int i = 0; i < 7; i++) low[i] = V(c.x - 108 + i * 36, c.y + 22);
            for (int i = 0; i < 5; i++) up[i] = V(c.x - 72 + i * 36, c.y - 24);
            float beat = Frac(t * .8f * k), flash = Reduced ? .3f : Mathf.Exp(-beat * 5);
            int w = (int)Mathf.Floor(t * .4f * k) % 5;
            for (int u = 0; u < 5; u++) for (int d = 0; d < 3; d++) L(low[u + d], up[u], u == w ? 1.3f : .9f, N.Teal, (u == w ? .5f : .18f) + .4f * flash, flash);
            Frame(low[w] - V(14, 14), low[w + 2] + V(14, 14), 1.8f, N.Teal, 1, false, 1);
            for (int i = 0; i < 7; i++) Cell(low[i], 9, i);
            for (int i = 0; i < 5; i++) { if (!Reduced) Halo(up[i], 22, N.Gold, .55f * flash); Cell(up[i], 9, 7 + i); }
            string sentence = "今天天气真不错";
            for (int i = 0; i < 7; i++) Label(low[i] + V(0, 24), sentence.Substring(i, 1), 11, N.TextMuted);
        }

        void Transformer(Vector2 c, float t, float k)
        {
            const int n = 7;
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++) { float a = -Mathf.PI / 2 + i * 2 * Mathf.PI / n; pts[i] = c + V(Mathf.Cos(a) * 78, 6 + Mathf.Sin(a) * 56); }
            float beat = Frac(t * .9f * k), flash = Reduced ? .25f : Mathf.Exp(-beat * 4);
            for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) L(pts[i], pts[j], .9f, N.Wire, .16f + .45f * flash, flash);
            int[][] pairs = { new[] { 0, 3, 0 }, new[] { 0, 4, 0 }, new[] { 2, 5, 1 }, new[] { 2, 6, 1 }, new[] { 1, 4, 2 }, new[] { 6, 3, 2 } };
            Color[] headColors = { N.Accent, N.Purple, N.Teal };
            foreach (var p in pairs)
            {
                float a = .35f + .5f * (.5f + .5f * Mathf.Sin(t * 2 * k + p[2] * 2.1f));
                L(pts[p[0]], pts[p[1]], 2, headColors[p[2]], a, 1.2f);
                var from = pts[p[0]]; var to = pts[p[1]];
                Comet(u => Vector2.Lerp(from, to, u), Frac(t * .8f * k + p[1] * .23f), .25f, 1.8f, headColors[p[2]], a);
            }
            if (!Reduced) GlowRing(c + V(0, 6), 30 + 50 * beat, 1.6f, N.Gold, .6f * (1 - beat));
            for (int i = 0; i < n; i++) Cell(pts[i], 10, i);
        }

        void Caption(Vector2 c, float t, float k)
        {
            const float st = 14;
            float x0 = c.x - 120, y0 = c.y - 21;
            int step = (int)Mathf.Floor(t * 1.1f * k) % 9; int px = step % 3, py = step / 3;
            var win = V(x0 + px * st, y0 + py * st);
            Box(win - V(8, 8), win + V(st + 8, st + 8), N.AccentSoft, .14f);
            Frame(win - V(8, 8), win + V(st + 8, st + 8), 1.6f, N.Accent, 1, false, 1);
            int slot = 0;
            for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++) Cell(V(x0 + i * st, y0 + j * st), 5, slot++);
            var chain = new Vector2[4]; string[] words = { "一只", "猫", "在", "睡" };
            for (int i = 0; i < 4; i++) chain[i] = V(c.x - 10 + i * 40, c.y + 2);
            Arrow(V(x0 + 3 * st + 12, c.y + 2), chain[0] - V(13, 0), 1.5f, N.Accent);
            for (int i = 0; i < 3; i++) Arrow(chain[i] + V(11, 0), chain[i + 1] - V(12, 0), 1.5f, N.Teal);
            for (int i = 0; i < 4; i++) { Cell(chain[i], 9, slot++); Label(chain[i] + V(0, 20), words[i], 10.5f, N.TextMuted); }
            float ph = Frac(t * .5f * k);
            var start = win + V(st * .5f, st * .5f);
            Comet(q => q < .3f ? Vector2.Lerp(start, chain[0], q / .3f) : Vector2.Lerp(chain[0], chain[3], (q - .3f) / .7f), ph, .15f, 3, N.Gold);
        }

        void Tone(Vector2 c, float t, Region r)
        {
            for (int a = 0; a < 3; a++)
            {
                float y = c.y - 26 + a * 26;
                Vector2 lo = V(c.x - 60, y), hi = V(c.x + 60, y);
                L(lo, hi, 2, N.Line);
                float v = r != null ? Mathf.Clamp01(r.axes[a]) : .3f + .2f * a;
                float wobble = Reduced ? 0 : Mathf.Sin(t * 1.5f + a * 1.7f) * 1.5f;
                var dot = V(Mathf.Lerp(lo.x, hi.x, v) + wobble, y);
                L(V(c.x, y), dot, 3, N.Orange, .6f, 1);
                CellAs(lo, 7, new XgCellLook(XgCellKind.Dim, 0)); CellAs(hi, 7, new XgCellLook(XgCellKind.Dim, 0));
                Orb(dot, 4.5f, N.Orange);
                Label(lo - V(26, 0), ToneLow[a], 10.5f, N.TextMuted); Label(hi + V(26, 0), ToneHigh[a], 10.5f, N.TextMuted);
            }
        }

        void Empty(Vector2 c)
        {
            float[][] spots = { new[] { -50f, -8 }, new[] { -18f, 10 }, new[] { 16f, -10 }, new[] { 48f, 8 }, new[] { 0f, -30 }, new[] { -34f, -34 }, new[] { 34f, -32 } };
            foreach (var p in spots) CellAs(c + V(p[0], p[1]), 8, new XgCellLook(XgCellKind.Off, 0));
        }

        // ---------------------------------------------------------------- stage 6: one wiring, the whole cortex

        void DrawUnified(float t, float u)
        {
            if (unifiedPoints == null) BuildUnifiedPoints();
            var center = V(440, 270);
            float wave = Frac(t * .3f) * 430;
            var pts = unifiedPoints;
            if (unifyStart > 0 && !Reduced)
            {
                // ignition: the whole cortex flares once as the borders give way
                float flash = Mathf.Exp(-(t - unifyStart) * 1.4f);
                HaloEllipse(center, 400, 250, N.Gold, .45f * flash);
            }
            for (int i = 0; i < pts.Length; i++)
                for (int j = i + 1; j < pts.Length; j++)
                {
                    float d = Vector2.Distance(pts[i], pts[j]);
                    float near = Reduced ? 0 : Mathf.Exp(-Mathf.Pow((Vector2.Distance(pts[i], center) - wave) / 40, 2)) * Mathf.Exp(-Mathf.Pow((Vector2.Distance(pts[j], center) - wave) / 40, 2));
                    L(pts[i], pts[j], .6f, near > .2f ? N.Gold : N.Wire, u * (Mathf.Max(.035f, .2f - d / 2600) + .45f * near));
                }
            Color[] headColors = { N.Accent, N.Purple, N.Teal };
            for (int h = 0; h < 3; h++)
            {
                float a = u * (.35f + .3f * Mathf.Sin(t * 2 + h * 2.1f));
                for (int q = 0; q < 3; q++)
                    for (int kk = 0; kk < 6; kk++)
                    {
                        int at = (h * 3 + q) * 7;
                        var from = pts[heads[at]]; var to = pts[heads[at + 1 + kk]];
                        L(from, to, 1 + (heads[at + 1 + kk] % 5) * .35f, headColors[h], a, 1);
                        if (!Reduced && kk % 3 == 0) Comet(x => Vector2.Lerp(from, to, x), Frac(t * .5f + Hash(at + kk)), .22f, 1.7f, headColors[h], u);
                    }
            }
            if (!Reduced) GlowRing(center, Mathf.Max(1, wave), 2.2f, N.Gold, .7f * (1 - wave / 430) * u, 72);
            float f = fade; fade = u;
            cellsSrc = UnifiedCells; emptySrc = false;
            for (int i = 0; i < pts.Length; i++)
            {
                float near = Reduced ? 0 : Mathf.Exp(-Mathf.Pow((Vector2.Distance(pts[i], center) - wave) / 30, 2));
                if (near > .05f) Halo(pts[i], 24, N.Gold, .55f * near);
                Cell(pts[i], 9, i);
            }
            for (int i = 0; i < Regions.Length; i++) Label(Regions[i].c - V(0, Regions[i].ry * .62f), RegionNames[i], 12, Regions[i].tint, .7f);
            Chip(V(440, 26), "<size=130%><color=#FFD166>" + UnifiedTitle + "</color></size>", N.Gold);
            fade = f;
        }

        void BuildUnifiedPoints()
        {
            var rng = new System.Random(23);
            var pts = new List<Vector2>();
            for (int guard = 0; pts.Count < 54 && guard < 6000; guard++)
            {
                var p = V(100 + (float)rng.NextDouble() * 690, 70 + (float)rng.NextDouble() * 400);
                bool inside = false;
                foreach (var r in Regions) { float dx = (p.x - r.c.x) / r.rx, dy = (p.y - r.c.y) / r.ry; if (dx * dx + dy * dy < .75f) inside = true; }
                if (!inside) continue;
                bool crowded = false;
                foreach (var q in pts) if (Vector2.Distance(p, q) < 38) { crowded = true; break; }
                if (!crowded) pts.Add(p);
            }
            unifiedPoints = pts.ToArray();
            heads = new int[9 * 7];
            for (int i = 0; i < heads.Length; i++) heads[i] = rng.Next(unifiedPoints.Length);
        }

        public static int UnifiedCellCount => 54;

        // ---------------------------------------------------------------- text labels (outside the canvas rebuild)

        /// <summary>Place the text of the last drawn frame (region chips, tokens, captions). Called from the page's tick.</summary>
        public void SyncLabels()
        {
            var rect = rectTransform.rect;
            Fit(rect);
            int count = labels.Count;
            for (int i = 0; i < count; i++)
            {
                var spec = labels[i];
                if (i >= pool.Count) pool.Add(MakeLabel(i));
                var view = pool[i];
                if (!view.rt.gameObject.activeSelf) view.rt.gameObject.SetActive(true);
                bool same = i < shown.Count && shown[i].text == spec.text && shown[i].chip == spec.chip && Mathf.Approximately(shown[i].size, spec.size);
                if (view.text.text != spec.text) view.text.text = spec.text;
                view.text.fontSize = spec.size * s;
                var col = spec.color; col.a *= spec.alpha;
                if (view.text.color != col) view.text.color = col;
                Vector2 pos = P(spec.pos) - rect.center;
                if (spec.chip)
                {
                    if (!same || view.rt.sizeDelta.x <= 0) { var pref = view.text.GetPreferredValues(spec.text, 9999, 9999); view.rt.sizeDelta = new Vector2(pref.x + 20 * s, pref.y + 10 * s); }
                    view.back.gameObject.SetActive(true);
                    // glass: dark, see-through, lit at the rim in the region's colour
                    var bc = N.Glass; bc.a = .78f * spec.alpha; view.backImage.color = bc;
                    var rc = spec.tint; rc.a = .85f * spec.alpha; view.backRim.effectColor = rc;
                    // A long topology name stays inside the map.
                    float room = Mathf.Max(0, rect.width * .5f - view.rt.sizeDelta.x * .5f - 4);
                    pos.x = Mathf.Clamp(pos.x, -room, room);
                }
                else
                {
                    view.back.gameObject.SetActive(false);
                    view.rt.sizeDelta = new Vector2(200 * s, spec.size * 2 * s);
                }
                view.rt.anchoredPosition = pos;
            }
            for (int i = count; i < pool.Count; i++) if (pool[i].rt.gameObject.activeSelf) pool[i].rt.gameObject.SetActive(false);
            shown.Clear(); shown.AddRange(labels);
        }

        LabelView MakeLabel(int i)
        {
            var go = new GameObject("Label" + i, typeof(RectTransform));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            var back = new GameObject("Back", typeof(RectTransform));
            back.layer = gameObject.layer;
            var brt = (RectTransform)back.transform;
            brt.SetParent(rt, false);
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
            var img = back.AddComponent<Image>(); img.raycastTarget = false;
            var rim = back.AddComponent<UnityEngine.UI.Outline>(); rim.effectDistance = new Vector2(1, -1);
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.layer = gameObject.layer;
            var trt = (RectTransform)textGo.transform;
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<TextMeshProUGUI>();
            if (Font != null) text.font = Font;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = true; text.raycastTarget = false;
            return new LabelView { rt = rt, back = brt, backImage = img, backRim = rim, text = text };
        }
    }
}
