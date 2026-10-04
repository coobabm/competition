using System;
using System.Collections.Generic;
using LingGuangV05.Core.Chat;

namespace LingGuangV05.Desktop.YY
{
    /// <summary>Straight RGBA colour in 0–1 floats (kept engine-free so the art can be rendered outside Unity).</summary>
    public readonly struct Rgba
    {
        public readonly float R, G, B, A;
        public Rgba(float r, float g, float b, float a = 1) { R = r; G = g; B = b; A = a; }
        public static Rgba Hex(uint rgb, float a = 1) => new Rgba(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);
        public Rgba WithAlpha(float a) => new Rgba(R, G, B, a);
        public static Rgba Lerp(Rgba x, Rgba y, float t)
        {
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            return new Rgba(x.R + (y.R - x.R) * t, x.G + (y.G - x.G) * t, x.B + (y.B - x.B) * t, x.A + (y.A - x.A) * t);
        }
    }

    /// <summary>
    /// A tiny signed-distance painter. Shapes are described in a 32×32 design space (y down) and rasterised at any
    /// pixel size with one-pixel anti-aliasing, so a face reads the same in a 32 px chat cell and a 64 px picker tile.
    /// </summary>
    public sealed class YYCanvas
    {
        public readonly int Size;
        /// <summary>Premultiplied RGBA, row 0 at the top.</summary>
        public readonly float[] Pixels;
        readonly float scale;
        /// <summary>Optional design-space transform applied to every sample (mirror, shift).</summary>
        public bool FlipY, FlipX;
        public float ShiftX, ShiftY;

        public YYCanvas(int size)
        {
            Size = size;
            Pixels = new float[size * size * 4];
            scale = size / 32f;
        }

        public void Paint(Func<float, float, float> sdf, Func<float, float, Rgba> fill, Rgba outline = default, float outlineWidth = 0)
        {
            for (int py = 0; py < Size; py++)
                for (int px = 0; px < Size; px++)
                {
                    float x = (px + .5f) / scale, y = (py + .5f) / scale;
                    if (FlipX) x = 32 - x;
                    if (FlipY) y = 32 - y;
                    x -= ShiftX; y -= ShiftY;
                    float d = sdf(x, y) * scale;
                    if (outlineWidth > 0 && outline.A > 0)
                    {
                        float co = Clamp01(.5f - (d - outlineWidth * scale));
                        if (co > 0) Blend(px, py, outline, co);
                    }
                    float ci = Clamp01(.5f - d);
                    if (ci > 0) Blend(px, py, fill(x, y), ci);
                }
        }

        public void Paint(Func<float, float, float> sdf, Rgba fill, Rgba outline = default, float outlineWidth = 0) =>
            Paint(sdf, (x, y) => fill, outline, outlineWidth);

        void Blend(int px, int py, Rgba c, float coverage)
        {
            float a = c.A * coverage;
            if (a <= 0) return;
            int i = (py * Size + px) * 4;
            float k = 1 - a;
            Pixels[i] = c.R * a + Pixels[i] * k;
            Pixels[i + 1] = c.G * a + Pixels[i + 1] * k;
            Pixels[i + 2] = c.B * a + Pixels[i + 2] * k;
            Pixels[i + 3] = a + Pixels[i + 3] * k;
        }

        /// <summary>Straight-alpha RGBA bytes, row 0 at the top.</summary>
        public byte[] ToBytes()
        {
            var bytes = new byte[Pixels.Length];
            for (int i = 0; i < Pixels.Length; i += 4)
            {
                float a = Pixels[i + 3];
                float inv = a > 1e-5f ? 1 / a : 0;
                bytes[i] = B(Pixels[i] * inv); bytes[i + 1] = B(Pixels[i + 1] * inv); bytes[i + 2] = B(Pixels[i + 2] * inv); bytes[i + 3] = B(a);
            }
            return bytes;
        }

        public float Coverage()
        {
            float sum = 0;
            for (int i = 3; i < Pixels.Length; i += 4) sum += Pixels[i];
            return sum / (Size * Size);
        }

        static byte B(float v) => (byte)(Clamp01(v) * 255 + .5f);
        static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }

    /// <summary>Signed distance functions in design units (negative inside).</summary>
    public static class Sdf
    {
        public static Func<float, float, float> Circle(float cx, float cy, float r) => (x, y) => Len(x - cx, y - cy) - r;

        public static Func<float, float, float> Ellipse(float cx, float cy, float rx, float ry) => (x, y) =>
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            float k = Len(dx, dy);
            if (k < 1e-5f) return -Math.Min(rx, ry);
            // First-order correction: divide by the gradient length for a near-Euclidean distance.
            float gx = dx / (rx * k), gy = dy / (ry * k);
            return (k - 1) / Len(gx, gy);
        };

        public static Func<float, float, float> Box(float x0, float y0, float x1, float y1, float r = 0) => (x, y) =>
        {
            float cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, hx = (x1 - x0) / 2 - r, hy = (y1 - y0) / 2 - r;
            float qx = Math.Abs(x - cx) - hx, qy = Math.Abs(y - cy) - hy;
            return Len(Math.Max(qx, 0), Math.Max(qy, 0)) + Math.Min(Math.Max(qx, qy), 0) - r;
        };

        public static Func<float, float, float> Capsule(float x0, float y0, float x1, float y1, float r) => (x, y) => SegDist(x, y, x0, y0, x1, y1) - r;

        /// <summary>A stroked open polyline of width <paramref name="w"/> with round caps.</summary>
        public static Func<float, float, float> Stroke(IList<float> pts, float w) => (x, y) =>
        {
            float best = float.MaxValue;
            for (int i = 0; i + 3 < pts.Count; i += 2) best = Math.Min(best, SegDist(x, y, pts[i], pts[i + 1], pts[i + 2], pts[i + 3]));
            return best - w / 2;
        };

        /// <summary>A filled polygon (even-odd) given as x,y pairs.</summary>
        public static Func<float, float, float> Polygon(IList<float> pts) => (x, y) =>
        {
            int n = pts.Count / 2;
            float best = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = pts[i * 2], yi = pts[i * 2 + 1], xj = pts[j * 2], yj = pts[j * 2 + 1];
                best = Math.Min(best, SegDist(x, y, xi, yi, xj, yj));
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside ? -best : best;
        };

        public static Func<float, float, float> Union(params Func<float, float, float>[] shapes) => (x, y) =>
        {
            float d = float.MaxValue;
            foreach (var s in shapes) d = Math.Min(d, s(x, y));
            return d;
        };

        public static Func<float, float, float> Intersect(Func<float, float, float> a, Func<float, float, float> b) => (x, y) => Math.Max(a(x, y), b(x, y));
        public static Func<float, float, float> Subtract(Func<float, float, float> a, Func<float, float, float> b) => (x, y) => Math.Max(a(x, y), -b(x, y));
        public static Func<float, float, float> Ring(float cx, float cy, float r, float w) => (x, y) => Math.Abs(Len(x - cx, y - cy) - r) - w / 2;
        public static Func<float, float, float> Move(Func<float, float, float> s, float dx, float dy) => (x, y) => s(x - dx, y - dy);

        /// <summary>A shape rotated by <paramref name="degrees"/> around (cx, cy).</summary>
        public static Func<float, float, float> Rotate(Func<float, float, float> s, float cx, float cy, float degrees)
        {
            float a = -degrees * (float)Math.PI / 180, c = (float)Math.Cos(a), sn = (float)Math.Sin(a);
            return (x, y) => { float dx = x - cx, dy = y - cy; return s(cx + dx * c - dy * sn, cy + dx * sn + dy * c); };
        }

        /// <summary>Quadratic Bézier sampled to a polyline (x0,y0)→(x2,y2) with control (x1,y1).</summary>
        public static float[] Quad(float x0, float y0, float x1, float y1, float x2, float y2, int steps = 12)
        {
            var pts = new float[(steps + 1) * 2];
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1 - t;
                pts[i * 2] = u * u * x0 + 2 * u * t * x1 + t * t * x2;
                pts[i * 2 + 1] = u * u * y0 + 2 * u * t * y1 + t * t * y2;
            }
            return pts;
        }

        /// <summary>An arc of a circle from <paramref name="a0"/> to <paramref name="a1"/> degrees (0 = right, 90 = down).</summary>
        public static float[] Arc(float cx, float cy, float r, float a0, float a1, int steps = 16)
        {
            var pts = new float[(steps + 1) * 2];
            for (int i = 0; i <= steps; i++)
            {
                float a = (a0 + (a1 - a0) * i / steps) * (float)Math.PI / 180;
                pts[i * 2] = cx + (float)Math.Cos(a) * r;
                pts[i * 2 + 1] = cy + (float)Math.Sin(a) * r;
            }
            return pts;
        }

        public static float[] Pts(params float[] p) => p;

        public static float Len(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        public static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay, wx = px - ax, wy = py - ay;
            float l2 = vx * vx + vy * vy;
            float t = l2 > 0 ? Math.Max(0, Math.Min(1, (wx * vx + wy * vy) / l2)) : 0;
            return Len(wx - vx * t, wy - vy * t);
        }

        /// <summary>The classic parametric heart as a polygon, centred on (cx, cy), about 2·<paramref name="size"/> wide.</summary>
        public static float[] HeartPoints(float cx, float cy, float size, int steps = 48)
        {
            var pts = new float[steps * 2];
            for (int i = 0; i < steps; i++)
            {
                double t = i * Math.PI * 2 / steps;
                double hx = 16 * Math.Pow(Math.Sin(t), 3);
                double hy = 13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t);
                pts[i * 2] = cx + (float)(hx / 16 * size);
                pts[i * 2 + 1] = cy - (float)((hy + 2) / 16 * size);
            }
            return pts;
        }
    }

    /// <summary>
    /// Original procedural artwork for every YY face and 斗图 head: small round yellow 2016-style faces with brown
    /// outlines, and simple icons for the object faces. Nothing is traced or copied from any existing emoticon set.
    /// </summary>
    public static class YYFaceArt
    {
        public const int Cell = 32;

        static readonly Rgba Outline = Rgba.Hex(0xA8661A);
        static readonly Rgba FaceTop = Rgba.Hex(0xFFE96E);
        static readonly Rgba FaceBottom = Rgba.Hex(0xF7B528);
        static readonly Rgba Ink = Rgba.Hex(0x3E2410);
        static readonly Rgba MouthDark = Rgba.Hex(0x7A2E16);
        static readonly Rgba Tongue = Rgba.Hex(0xF2697A);
        static readonly Rgba Blush = Rgba.Hex(0xFF7A7A, .55f);
        static readonly Rgba Tear = Rgba.Hex(0x47AEFF);
        static readonly Rgba TearEdge = Rgba.Hex(0x1F78C8);
        static readonly Rgba White = Rgba.Hex(0xFFFFFF);
        static readonly Rgba Red = Rgba.Hex(0xE8322B);
        static readonly Rgba RedEdge = Rgba.Hex(0x9E1A16);
        static readonly Rgba SkinTone = Rgba.Hex(0xFFD45A);

        /// <summary>Faces with a second animation frame (a blink, a tear, a wave). Others have one frame.</summary>
        public static int Frames(YYFace face) => face != null && Animated.Contains(face.Id) ? 2 : 1;
        static readonly HashSet<string> Animated = new HashSet<string> { "sob", "bye", "dizzy", "heart" };

        public static YYCanvas DrawFace(YYFace face, int size = Cell, int frame = 0)
        {
            var c = new YYCanvas(size);
            if (face == null) return c;
            Draw(c, face.Id, frame);
            return c;
        }

        // ───────────── shared face parts ─────────────

        const float CX = 16, CY = 16.6f, R = 13.8f;
        const float LX = 11.4f, RX = 20.6f, EY = 14.2f;

        static void Head(YYCanvas c, Rgba? top = null, Rgba? bottom = null, Rgba? tintTop = null, float tintTo = 0)
        {
            Rgba t = top ?? FaceTop, b = bottom ?? FaceBottom;
            c.Paint(Sdf.Circle(CX, CY, R), (x, y) =>
            {
                float k = (y - (CY - R)) / (2 * R);
                var col = Rgba.Lerp(t, b, k);
                if (tintTop.HasValue && k < tintTo) col = Rgba.Lerp(tintTop.Value, col, k / tintTo);
                return col;
            }, Outline, 1.1f);
            // A soft gloss in the upper left, like the glossy 2016 sets.
            c.Paint(Sdf.Ellipse(10.5f, 7.6f, 4.2f, 2.2f), White.WithAlpha(.42f));
        }

        static void Dots(YYCanvas c, float ry = 2.2f, float dx = 0, float dy = 0)
        {
            c.Paint(Sdf.Ellipse(LX + dx, EY + dy, 1.55f, ry), Ink);
            c.Paint(Sdf.Ellipse(RX + dx, EY + dy, 1.55f, ry), Ink);
            c.Paint(Sdf.Circle(LX + dx + .5f, EY + dy - .8f, .5f), White.WithAlpha(.9f));
            c.Paint(Sdf.Circle(RX + dx + .5f, EY + dy - .8f, .5f), White.WithAlpha(.9f));
        }

        static void Line(YYCanvas c, float[] pts, float w, Rgba? col = null) => c.Paint(Sdf.Stroke(pts, w), col ?? Ink);
        static void Seg(YYCanvas c, float x0, float y0, float x1, float y1, float w, Rgba? col = null) => c.Paint(Sdf.Capsule(x0, y0, x1, y1, w / 2), col ?? Ink);

        static void Smile(YYCanvas c, float half = 4.6f, float depth = 3.2f, float y = 20.2f, float w = 1.5f) =>
            Line(c, Sdf.Quad(CX - half, y, CX, y + depth * 2, CX + half, y), w);

        static void Frown(YYCanvas c, float half = 3.8f, float depth = 2.2f, float y = 23.6f, float w = 1.5f) =>
            Line(c, Sdf.Quad(CX - half, y, CX, y - depth * 2, CX + half, y), w);

        static void HappyEyes(YYCanvas c, float w = 1.5f, float lift = 0)
        {
            Line(c, Sdf.Quad(LX - 2.4f, EY + 1.2f - lift, LX, EY - 2.6f - lift, LX + 2.4f, EY + 1.2f - lift), w);
            Line(c, Sdf.Quad(RX - 2.4f, EY + 1.2f - lift, RX, EY - 2.6f - lift, RX + 2.4f, EY + 1.2f - lift), w);
        }

        static void ClosedEyes(YYCanvas c, float w = 1.4f, float drop = 0)
        {
            Line(c, Sdf.Quad(LX - 2.4f, EY - .6f + drop, LX, EY + 2.2f + drop, LX + 2.4f, EY - .6f + drop), w);
            Line(c, Sdf.Quad(RX - 2.4f, EY - .6f + drop, RX, EY + 2.2f + drop, RX + 2.4f, EY - .6f + drop), w);
        }

        static void BlushCheeks(YYCanvas c, float alpha = .55f)
        {
            c.Paint(Sdf.Ellipse(7.8f, 19.2f, 2.6f, 1.5f), Blush.WithAlpha(alpha));
            c.Paint(Sdf.Ellipse(24.2f, 19.2f, 2.6f, 1.5f), Blush.WithAlpha(alpha));
        }

        static void BigEyes(YYCanvas c, float r = 3.2f, float pr = 1.5f, float pdx = 0, float pdy = 0, float y = EY)
        {
            foreach (float ex in new[] { LX, RX })
            {
                c.Paint(Sdf.Circle(ex, y, r), White, Ink, .9f);
                c.Paint(Sdf.Circle(ex + pdx, y + pdy, pr), Ink);
            }
        }

        static void Brows(YYCanvas c, float innerY, float outerY, float w = 1.4f)
        {
            Seg(c, LX - 2.6f, outerY, LX + 2.2f, innerY, w);
            Seg(c, RX + 2.6f, outerY, RX - 2.2f, innerY, w);
        }

        static Func<float, float, float> DropShape(float x, float y, float r) =>
            Sdf.Union(Sdf.Circle(x, y, r), Sdf.Polygon(Sdf.Pts(x - r * .92f, y - r * .4f, x, y - r * 2.4f, x + r * .92f, y - r * .4f)));

        static void Drop(YYCanvas c, float x, float y, float r) => c.Paint(DropShape(x, y, r), Tear, TearEdge, .6f);

        static void OpenMouth(YYCanvas c, float cx, float cy, float rx, float ry, bool tongue = false)
        {
            c.Paint(Sdf.Ellipse(cx, cy, rx, ry), MouthDark, Ink, .8f);
            if (tongue) c.Paint(Sdf.Intersect(Sdf.Ellipse(cx, cy + ry * .55f, rx * .7f, ry * .55f), Sdf.Ellipse(cx, cy, rx, ry)), Tongue);
        }

        static Func<float, float, float> Hand(float x0, float y0, float x1, float y1) => Sdf.Box(x0, y0, x1, y1, 2.4f);

        static void Skin(YYCanvas c, Func<float, float, float> shape) => c.Paint(shape, SkinTone, Outline, 1f);

        static void Heart(YYCanvas c, float cx, float cy, float size, Rgba fill, Rgba edge, float outline = 1f)
        {
            c.Paint(Sdf.Polygon(Sdf.HeartPoints(cx, cy, size)), fill, edge, outline);
        }

        // ───────────── the faces ─────────────

        static void Draw(YYCanvas c, string id, int frame)
        {
            switch (id)
            {
                case "smile":
                    Head(c); Dots(c); Smile(c, 4.4f, 2.4f); break;
                case "grimace":
                    Head(c); Dots(c);
                    Line(c, Sdf.Pts(10.6f, 22.4f, 13.2f, 21.2f, 16f, 22.6f, 18.8f, 21.4f, 21.4f, 23.4f), 1.5f);
                    break;
                case "lovestruck":
                    Head(c);
                    Heart(c, LX, EY + .4f, 3.3f, Red, RedEdge, .6f); Heart(c, RX, EY + .4f, 3.3f, Red, RedEdge, .6f);
                    OpenMouth(c, CX, 22f, 4f, 2.6f, true);
                    c.Paint(DropShape(19.4f, 26.6f, 1f), Tear.WithAlpha(.85f));
                    break;
                case "dazed":
                    Head(c); BigEyes(c, 3.3f, .9f);
                    Seg(c, 13.4f, 22.6f, 18.6f, 22.6f, 1.4f);
                    break;
                case "smug":
                    Head(c);
                    c.Paint(Sdf.Union(Sdf.Box(7f, 11.2f, 15f, 16.6f, 2f), Sdf.Box(17f, 11.2f, 25f, 16.6f, 2f), Sdf.Box(14f, 11.6f, 18f, 12.8f)), Ink);
                    c.Paint(Sdf.Box(8.4f, 12.2f, 10.8f, 13.4f, .6f), White.WithAlpha(.6f));
                    c.Paint(Sdf.Box(18.4f, 12.2f, 20.8f, 13.4f, .6f), White.WithAlpha(.6f));
                    Line(c, Sdf.Quad(12f, 21.4f, 17.5f, 24.6f, 21.6f, 20.2f), 1.5f);
                    break;
                case "tears":
                    Head(c); ClosedEyes(c, 1.4f, .4f); Brows(c, 9.6f, 11.2f, 1.2f);
                    c.Paint(Sdf.Union(Sdf.Box(9.6f, 16.2f, 12.4f, 26f, 1.3f), Sdf.Box(19.6f, 16.2f, 22.4f, 26f, 1.3f)), Tear.WithAlpha(.9f));
                    Frown(c, 2.6f, 1.2f, 23.4f, 1.4f);
                    break;
                case "shy":
                    Head(c); ClosedEyes(c, 1.3f, .2f); BlushCheeks(c, .85f);
                    Seg(c, 6.4f, 18.4f, 7.8f, 20.4f, .7f, RedEdge.WithAlpha(.6f)); Seg(c, 8.4f, 18.4f, 9.8f, 20.4f, .7f, RedEdge.WithAlpha(.6f));
                    Seg(c, 22.2f, 18.4f, 23.6f, 20.4f, .7f, RedEdge.WithAlpha(.6f)); Seg(c, 24.2f, 18.4f, 25.6f, 20.4f, .7f, RedEdge.WithAlpha(.6f));
                    Smile(c, 2f, 1f, 21.6f, 1.3f);
                    break;
                case "zipit":
                    Head(c); Dots(c);
                    Seg(c, 10.4f, 22f, 21.6f, 22f, 1.5f);
                    for (float x = 11.6f; x < 21; x += 2.2f) Seg(c, x, 20.6f, x, 23.4f, .9f);
                    c.Paint(Sdf.Box(20.6f, 20.4f, 23.2f, 24.6f, .7f), Rgba.Hex(0xB9C2CF), Ink, .6f);
                    break;
                case "asleep":
                    Head(c); ClosedEyes(c, 1.4f, .6f);
                    c.Paint(Sdf.Ellipse(16.4f, 22.6f, 1.6f, 1.3f), MouthDark);
                    Line(c, Sdf.Pts(22.6f, 2.4f, 27.2f, 2.4f, 22.6f, 6.6f, 27.2f, 6.6f), 1.2f, Rgba.Hex(0x3B6FD6));
                    Line(c, Sdf.Pts(27.4f, 8.4f, 30.2f, 8.4f, 27.4f, 11f, 30.2f, 11f), 1f, Rgba.Hex(0x3B6FD6));
                    break;
                case "sob":
                    Head(c);
                    Line(c, Sdf.Pts(9.2f, 11.6f, 13.2f, 13.8f, 9.2f, 16f), 1.4f);
                    Line(c, Sdf.Pts(22.8f, 11.6f, 18.8f, 13.8f, 22.8f, 16f), 1.4f);
                    OpenMouth(c, CX, 22.4f, 5.2f, 3.6f);
                    float spray = frame == 0 ? 0 : 1.2f;
                    Drop(c, 5.4f - spray, 16.4f + spray, 1.7f); Drop(c, 26.6f + spray, 16.4f + spray, 1.7f);
                    Drop(c, 3.4f - spray, 21.4f, 1.2f); Drop(c, 28.6f + spray, 21.4f, 1.2f);
                    break;
                case "awkward":
                    Head(c); Dots(c, 1.8f);
                    c.Paint(Sdf.Box(11.2f, 20.4f, 20.8f, 23.8f, 1f), White, Ink, .9f);
                    Seg(c, 11.4f, 22.1f, 20.6f, 22.1f, .7f);
                    BlushCheeks(c, .45f);
                    Drop(c, 25.6f, 9.6f, 1.6f);
                    break;
                case "angry":
                    Head(c, Rgba.Hex(0xFF8A4C), Rgba.Hex(0xF2552C));
                    Brows(c, 13f, 10.2f, 1.7f);
                    c.Paint(Sdf.Ellipse(LX + .4f, EY + 1.6f, 1.5f, 1.6f), Ink); c.Paint(Sdf.Ellipse(RX - .4f, EY + 1.6f, 1.5f, 1.6f), Ink);
                    Frown(c, 3.6f, 1.6f, 24f, 1.6f);
                    var vein = Rgba.Hex(0xB0120E);
                    Line(c, Sdf.Quad(22f, 4.2f, 23.4f, 5.8f, 22.4f, 7.4f), 1f, vein); Line(c, Sdf.Quad(26.8f, 4.6f, 25f, 5.6f, 26.4f, 7.8f), 1f, vein);
                    break;
                case "cheeky":
                    Head(c);
                    c.Paint(Sdf.Ellipse(LX, EY, 1.55f, 2.2f), Ink);
                    Line(c, Sdf.Pts(RX - 2.4f, EY - 1.6f, RX + 1.6f, EY, RX - 2.4f, EY + 1.6f), 1.4f);
                    Smile(c, 4.4f, 1.6f, 20.4f);
                    c.Paint(Sdf.Intersect(Sdf.Box(14.6f, 21f, 20.6f, 27.4f, 2.8f), Sdf.Box(14f, 22f, 21.4f, 30f)), Tongue, RedEdge, .7f);
                    Seg(c, 17.6f, 23.4f, 17.6f, 25.6f, .6f, RedEdge);
                    break;
                case "grin":
                    Head(c); HappyEyes(c, 1.5f);
                    c.Paint(Sdf.Intersect(Sdf.Ellipse(CX, 19.2f, 7.2f, 6f), Sdf.Box(4f, 19.4f, 28f, 30f)), White, Ink, 1f);
                    Seg(c, 9.4f, 22.2f, 22.6f, 22.2f, .7f);
                    for (float x = 11.8f; x < 21; x += 2.8f) Seg(c, x, 19.8f, x, 24.6f, .6f);
                    break;
                case "surprised":
                    Head(c); BigEyes(c, 2.6f, 1.2f, 0, 0, 13.6f);
                    Line(c, Sdf.Quad(8.8f, 9f, 11.4f, 7f, 13.8f, 8.6f), 1.2f); Line(c, Sdf.Quad(18.2f, 8.6f, 20.6f, 7f, 23.2f, 9f), 1.2f);
                    OpenMouth(c, CX, 22.6f, 2.4f, 3f);
                    break;
                case "sad":
                    Head(c); Dots(c, 1.9f, 0, .6f); Brows(c, 10.2f, 12.2f, 1.3f);
                    Frown(c, 3.6f, 1.6f, 24.2f);
                    break;
                case "nosepick":
                    Head(c);
                    Seg(c, LX - 2, EY, LX + 2, EY, 1.4f); Seg(c, RX - 2, EY, RX + 2, EY, 1.4f);
                    c.Paint(Sdf.Ellipse(LX, EY + 1f, 1.2f, 1f), Ink); c.Paint(Sdf.Ellipse(RX, EY + 1f, 1.2f, 1f), Ink);
                    Seg(c, 13f, 23.4f, 18f, 23f, 1.3f);
                    Skin(c, Sdf.Union(Hand(16.4f, 22.6f, 25.4f, 30.6f), Sdf.Capsule(18.6f, 23.4f, 16.6f, 17.6f, 1.5f)));
                    Seg(c, 19.6f, 26f, 24f, 26f, .6f, Outline); Seg(c, 19.6f, 28.2f, 24f, 28.2f, .6f, Outline);
                    break;
                case "bye":
                    Head(c); HappyEyes(c, 1.4f); Smile(c, 3.8f, 2f, 20.6f);
                    float tilt = frame == 0 ? -12 : 10;
                    Skin(c, Sdf.Rotate(Sdf.Union(Hand(22.4f, 17.6f, 30.4f, 25.4f),
                        Sdf.Capsule(23.6f, 18f, 23.2f, 12.6f, 1.2f), Sdf.Capsule(25.8f, 18f, 25.8f, 11.8f, 1.2f),
                        Sdf.Capsule(28f, 18f, 28.4f, 12.4f, 1.2f), Sdf.Capsule(30f, 20f, 31f, 15.4f, 1.1f)), 26f, 24f, tilt));
                    Line(c, Sdf.Arc(26f, 20f, 7.4f, frame == 0 ? 200 : 290, frame == 0 ? 240 : 330, 6), .8f, Outline);
                    break;
                case "giggle":
                    Head(c); HappyEyes(c, 1.4f, -.4f); BlushCheeks(c, .45f);
                    Skin(c, Sdf.Union(Hand(10.4f, 19.6f, 21.6f, 27.8f)));
                    for (float x = 13.2f; x < 21; x += 2.6f) Seg(c, x, 20f, x, 23.4f, .6f, Outline);
                    break;
                case "cute":
                    Head(c);
                    foreach (float ex in new[] { LX, RX })
                    {
                        c.Paint(Sdf.Ellipse(ex, EY + .4f, 2.4f, 2.9f), Ink);
                        c.Paint(Sdf.Circle(ex + .8f, EY - .6f, 1f), White);
                        c.Paint(Sdf.Circle(ex - .7f, EY + 1.6f, .5f), White);
                    }
                    BlushCheeks(c, .7f);
                    Line(c, Sdf.Pts(13f, 21.2f, 14.5f, 22.6f, 16f, 21.2f, 17.5f, 22.6f, 19f, 21.2f), 1.2f);
                    break;
                case "eyeroll":
                    Head(c);
                    foreach (float ex in new[] { LX, RX })
                    {
                        c.Paint(Sdf.Ellipse(ex, EY, 3f, 2.7f), White, Ink, .9f);
                        c.Paint(Sdf.Intersect(Sdf.Circle(ex + .6f, EY - 2.4f, 1.5f), Sdf.Ellipse(ex, EY, 3f, 2.7f)), Ink);
                    }
                    Seg(c, 12.6f, 22.4f, 19.4f, 22f, 1.4f);
                    break;
                case "haughty":
                    Head(c);
                    foreach (float ex in new[] { LX, RX })
                    {
                        c.Paint(Sdf.Intersect(Sdf.Ellipse(ex, EY, 2.8f, 2.4f), Sdf.Box(ex - 4, EY - .2f, ex + 4, EY + 4)), White, Ink, .8f);
                        c.Paint(Sdf.Intersect(Sdf.Circle(ex + 1.4f, EY + .9f, 1.2f), Sdf.Box(ex - 4, EY - .2f, ex + 4, EY + 4)), Ink);
                        Seg(c, ex - 2.9f, EY - .2f, ex + 2.9f, EY - .2f, 1.3f);
                    }
                    Seg(c, 9f, 9.6f, 13.6f, 10.6f, 1.2f); Seg(c, 18.4f, 10.6f, 23f, 9.6f, 1.2f);
                    Line(c, Sdf.Quad(14f, 22.6f, 18f, 23.4f, 21f, 21.2f), 1.4f);
                    break;
                case "sleepy":
                    Head(c);
                    foreach (float ex in new[] { LX, RX })
                    {
                        c.Paint(Sdf.Intersect(Sdf.Ellipse(ex, EY + .8f, 2.6f, 2f), Sdf.Box(ex - 4, EY + .9f, ex + 4, EY + 4)), Ink);
                        Seg(c, ex - 2.8f, EY + .8f, ex + 2.8f, EY + .8f, 1.2f);
                    }
                    c.Paint(Sdf.Ellipse(CX, 22.8f, 1.8f, 1.4f), MouthDark);
                    c.Paint(Sdf.Circle(19.6f, 19.6f, 2.4f), Rgba.Hex(0xBFE6FF, .85f), Rgba.Hex(0x5AA9E6), .5f);
                    c.Paint(Sdf.Circle(19f, 18.9f, .6f), White);
                    break;
                case "terrified":
                    Head(c, null, null, Rgba.Hex(0x7B8CF0), .5f);
                    for (float x = 10.4f; x < 22; x += 2.8f) Seg(c, x, 4.6f, x, 8.6f, .9f, Rgba.Hex(0x2C3A8C));
                    BigEyes(c, 3f, .8f, 0, 0, 13.8f);
                    c.Paint(Sdf.Ellipse(CX, 23f, 3.2f, 4f), MouthDark, Ink, .8f);
                    Line(c, Sdf.Pts(14f, 20.6f, 15.3f, 21.4f, 16.6f, 20.6f, 17.9f, 21.4f), .6f, White);
                    break;
                case "sweat":
                    Head(c); Dots(c, 2f); Seg(c, 12.8f, 22.4f, 19.2f, 22.4f, 1.4f);
                    c.Paint(DropShape(26f, 12.6f, 2.6f), Tear, TearEdge, .7f);
                    c.Paint(Sdf.Ellipse(25.2f, 12.6f, .7f, 1.1f), White.WithAlpha(.8f));
                    break;
                case "goofy":
                    Head(c); HappyEyes(c, 1.5f); BlushCheeks(c, .4f);
                    c.Paint(Sdf.Intersect(Sdf.Ellipse(CX, 19.4f, 6f, 6f), Sdf.Box(4f, 19.4f, 28f, 30f)), MouthDark, Ink, .8f);
                    c.Paint(Sdf.Intersect(Sdf.Ellipse(CX, 25.4f, 3.6f, 2.4f), Sdf.Ellipse(CX, 19.4f, 6f, 6f)), Tongue);
                    c.Paint(Sdf.Box(13.4f, 19.4f, 18.6f, 21f, .4f), White);
                    break;
                case "fighting":
                    Head(c);
                    c.Paint(Sdf.Intersect(Sdf.Box(0, 6.4f, 32, 10f), Sdf.Circle(CX, CY, R + .2f)), Red, RedEdge, .6f);
                    Line(c, Sdf.Quad(27.6f, 8.2f, 30.6f, 9.2f, 31f, 13.6f), 1.6f, Red); Line(c, Sdf.Quad(27.6f, 8.6f, 31f, 7.4f, 31.4f, 5f), 1.4f, Red);
                    Brows(c, 13.4f, 11.2f, 1.6f);
                    c.Paint(Sdf.Circle(LX + .4f, EY + 1.8f, 1.4f), Ink); c.Paint(Sdf.Circle(RX - .4f, EY + 1.8f, 1.4f), Ink);
                    Line(c, Sdf.Pts(12.4f, 23.4f, 14.2f, 22f, 16f, 23.4f, 17.8f, 22f, 19.6f, 23.4f), 1.4f);
                    break;
                case "puzzled":
                    Head(c); Dots(c, 2f, -1f);
                    Line(c, Sdf.Quad(8.4f, 10.4f, 10.4f, 8.4f, 13f, 9.6f), 1.2f); Seg(c, 17.4f, 11.2f, 21.6f, 11.4f, 1.2f);
                    Line(c, Sdf.Quad(11.6f, 22.6f, 14.6f, 21f, 17.6f, 22.6f), 1.4f);
                    Line(c, Sdf.Pts(23.6f, 3.6f, 25.4f, 1.8f, 28.2f, 2f, 29.4f, 4f, 28.6f, 6.2f, 26.4f, 7.4f, 26.4f, 9.6f), 1.6f, Rgba.Hex(0x2F6FE0));
                    c.Paint(Sdf.Circle(26.4f, 12.2f, 1.1f), Rgba.Hex(0x2F6FE0));
                    break;
                case "shush":
                    Head(c); Dots(c, 2f, .8f);
                    c.Paint(Sdf.Ellipse(CX, 22.4f, 2.2f, 1.6f), MouthDark);
                    Skin(c, Sdf.Union(Sdf.Capsule(16.2f, 27.6f, 16.2f, 18f, 1.6f), Hand(13.4f, 25f, 21.4f, 31.6f)));
                    break;
                case "dizzy":
                    Head(c);
                    float spin = frame == 0 ? 0 : 180;
                    foreach (float ex in new[] { LX, RX })
                    {
                        var spiral = new List<float>();
                        for (int i = 0; i <= 28; i++)
                        {
                            float a = (i * 26 + spin) * (float)Math.PI / 180, rr = .2f + i * .1f;
                            spiral.Add(ex + (float)Math.Cos(a) * rr); spiral.Add(EY + (float)Math.Sin(a) * rr);
                        }
                        Line(c, spiral.ToArray(), 1f);
                    }
                    Line(c, Sdf.Pts(10.8f, 22.4f, 12.6f, 21.2f, 14.4f, 22.6f, 16.2f, 21.2f, 18f, 22.6f, 19.8f, 21.2f, 21.4f, 22.4f), 1.3f);
                    break;
                case "doomed":
                    Head(c, null, null, Rgba.Hex(0x7E8A6A), .55f);
                    for (float x = 8.4f; x < 24; x += 3f) Seg(c, x, 4.2f, x, 9.6f, 1f, Rgba.Hex(0x3C4632));
                    Brows(c, 10.6f, 12.4f, 1.2f);
                    foreach (float ex in new[] { LX, RX })
                        c.Paint(Sdf.Intersect(Sdf.Ellipse(ex, EY + 1.2f, 2.2f, 1.8f), Sdf.Box(ex - 4, EY + 1.2f, ex + 4, EY + 4)), Ink);
                    Frown(c, 3.4f, 1.2f, 24f, 1.4f);
                    break;
                case "bonk":
                    Head(c);
                    Line(c, Sdf.Pts(9.2f, 12f, 13.2f, 16f), 1.3f); Line(c, Sdf.Pts(13.2f, 12f, 9.2f, 16f), 1.3f);
                    Line(c, Sdf.Pts(18.8f, 12f, 22.8f, 16f), 1.3f); Line(c, Sdf.Pts(22.8f, 12f, 18.8f, 16f), 1.3f);
                    OpenMouth(c, CX, 22.6f, 2.4f, 1.8f);
                    c.Paint(Sdf.Circle(17.6f, 4.4f, 2.6f), Rgba.Hex(0xFFB0A0), Outline, .8f);
                    c.Paint(Sdf.Rotate(Sdf.Box(21.4f, 2.4f, 31.6f, 4.6f, 1f), 24f, 3.6f, -35), Rgba.Hex(0x9B6A3A), Rgba.Hex(0x5A3A1A), .6f);
                    c.Paint(Sdf.Rotate(Sdf.Box(18.6f, -1.4f, 23.4f, 8.8f, 1.4f), 21f, 3.6f, -35), Rgba.Hex(0xB8C0CC), Rgba.Hex(0x4C5563), .8f);
                    break;
                case "phew":
                    Head(c); ClosedEyes(c, 1.4f); Smile(c, 3.6f, 1.6f, 21f);
                    Skin(c, Sdf.Rotate(Sdf.Union(Hand(19.4f, 4.6f, 30.2f, 11.4f)), 24.8f, 8f, -20));
                    c.Paint(DropShape(13.6f, 9.2f, 1.5f), Tear, TearEdge, .6f);
                    break;
                case "clap":
                    Head(c); HappyEyes(c, 1.4f); Smile(c, 3.4f, 1.8f, 19.6f, 1.4f);
                    Skin(c, Sdf.Rotate(Hand(9.4f, 22f, 16.4f, 31f), 13f, 26.5f, 18));
                    Skin(c, Sdf.Rotate(Hand(15.6f, 22f, 22.6f, 31f), 19f, 26.5f, -18));
                    Seg(c, 6.4f, 22f, 4.6f, 20.2f, .9f, Outline); Seg(c, 25.6f, 22f, 27.4f, 20.2f, .9f, Outline);
                    Seg(c, 6f, 25.6f, 3.6f, 25.6f, .9f, Outline); Seg(c, 26f, 25.6f, 28.4f, 25.6f, .9f, Outline);
                    break;
                case "smirk":
                    Head(c);
                    Seg(c, 8.8f, 10.6f, 13.6f, 12.2f, 1.3f); Seg(c, 18.4f, 12.2f, 23.2f, 10.6f, 1.3f);
                    foreach (float ex in new[] { LX, RX })
                        c.Paint(Sdf.Intersect(Sdf.Ellipse(ex + .6f, EY + .6f, 2f, 1.7f), Sdf.Box(ex - 4, EY + .2f, ex + 4, EY + 4)), Ink);
                    Line(c, Sdf.Quad(10.4f, 21.6f, 17.4f, 24.4f, 22.6f, 19.2f), 1.5f);
                    break;
                case "yawn":
                    Head(c);
                    Line(c, Sdf.Pts(LX - 2.4f, EY - .6f, LX + 2.2f, EY + .4f), 1.3f); Line(c, Sdf.Pts(RX + 2.4f, EY - .6f, RX - 2.2f, EY + .4f), 1.3f);
                    OpenMouth(c, CX, 22.4f, 3.4f, 4.4f, true);
                    Drop(c, 7.6f, 17.6f, 1.2f);
                    break;
                case "scorn":
                    Head(c);
                    foreach (float ex in new[] { LX, RX })
                    {
                        c.Paint(Sdf.Intersect(Sdf.Ellipse(ex, EY, 2.9f, 2.4f), Sdf.Box(ex - 4, EY - .6f, ex + 4, EY + 4)), White, Ink, .8f);
                        c.Paint(Sdf.Intersect(Sdf.Circle(ex - 1.5f, EY + .8f, 1.2f), Sdf.Box(ex - 4, EY - .6f, ex + 4, EY + 4)), Ink);
                        Seg(c, ex - 3f, EY - .6f, ex + 3f, EY - .6f, 1.3f);
                    }
                    Line(c, Sdf.Quad(11.4f, 21.6f, 14f, 24.4f, 18.4f, 23f), 1.4f);
                    Line(c, Sdf.Pts(20.2f, 22.6f, 21.6f, 21.2f), 1.2f);
                    break;
                case "wronged":
                    Head(c); Brows(c, 9.8f, 11.8f, 1.2f);
                    foreach (float ex in new[] { LX, RX })
                    {
                        c.Paint(Sdf.Ellipse(ex, EY + .6f, 1.8f, 2.2f), Ink);
                        c.Paint(Sdf.Circle(ex + .6f, EY - .2f, .6f), White);
                    }
                    c.Paint(Sdf.Ellipse(CX + .4f, 22.6f, 2.2f, 1.6f), Rgba.Hex(0xE0605A), Ink, .8f);
                    Seg(c, CX - 1.2f, 22.6f, CX + 2f, 22.6f, .6f, MouthDark);
                    BlushCheeks(c, .4f);
                    break;
                case "abouttocry":
                    Head(c); Brows(c, 9.6f, 11.6f, 1.2f);
                    foreach (float ex in new[] { LX, RX })
                    {
                        c.Paint(Sdf.Ellipse(ex, EY + .6f, 2.6f, 3f), Ink);
                        c.Paint(Sdf.Circle(ex + .9f, EY - .4f, 1f), White);
                        c.Paint(Sdf.Circle(ex - .8f, EY + 1.6f, .5f), White);
                        c.Paint(Sdf.Intersect(Sdf.Ellipse(ex, EY + 3.1f, 2.9f, 1f), Sdf.Box(ex - 4, EY + 2.4f, ex + 4, EY + 6)), Tear.WithAlpha(.9f));
                    }
                    Line(c, Sdf.Pts(12.6f, 23.6f, 14.1f, 22.4f, 15.6f, 23.6f, 17.1f, 22.4f, 18.6f, 23.6f, 19.6f, 22.8f), 1.2f);
                    break;
                case "sly":
                    Head(c, null, null, Rgba.Hex(0xB98A3A), .38f);
                    Seg(c, 8.6f, 11.6f, 13.6f, 13f, 1.3f); Seg(c, 18.4f, 13f, 23.4f, 11.6f, 1.3f);
                    foreach (float ex in new[] { LX, RX })
                        c.Paint(Sdf.Intersect(Sdf.Ellipse(ex + 1.2f, EY + 1.2f, 1.8f, 1.2f), Sdf.Box(ex - 4, EY + .6f, ex + 4, EY + 4)), Ink);
                    c.Paint(Sdf.Intersect(Sdf.Ellipse(CX, 19.6f, 7f, 4.6f), Sdf.Box(4f, 19.6f, 28f, 30f)), MouthDark, Ink, .8f);
                    for (float x = 11f; x < 22; x += 2.5f) c.Paint(Sdf.Polygon(Sdf.Pts(x, 19.6f, x + 2.4f, 19.6f, x + 1.2f, 21.6f)), White);
                    break;
                case "kiss":
                    Head(c);
                    c.Paint(Sdf.Ellipse(LX, EY, 1.55f, 2.2f), Ink);
                    Line(c, Sdf.Quad(RX - 2.4f, EY + .4f, RX, EY - 2f, RX + 2.4f, EY + .4f), 1.4f);
                    BlushCheeks(c, .6f);
                    c.Paint(Sdf.Union(Sdf.Ellipse(15.4f, 21.4f, 1.8f, 1.4f), Sdf.Ellipse(15.4f, 23.8f, 1.8f, 1.4f)), Rgba.Hex(0xF0506A), RedEdge, .6f);
                    Heart(c, 24f, 23.6f, 3.2f, Red, RedEdge, .6f);
                    break;
                case "puppyeyes":
                    Head(c); Brows(c, 9.4f, 11.2f, 1.1f);
                    foreach (float ex in new[] { LX, RX })
                    {
                        c.Paint(Sdf.Circle(ex, EY + 1f, 3.2f), Ink);
                        c.Paint(Sdf.Circle(ex + 1f, EY - .2f, 1.3f), White);
                        c.Paint(Sdf.Circle(ex - 1.2f, EY + 2.2f, .7f), White);
                        c.Paint(Sdf.Circle(ex + 1.4f, EY + 2.6f, .4f), White);
                    }
                    c.Paint(Sdf.Ellipse(CX, 23.2f, 1.6f, 1f), MouthDark);
                    break;
                case "rose":
                    Seg(c, 16f, 15f, 16.6f, 30.4f, 1.6f, Rgba.Hex(0x2E9A3A));
                    c.Paint(Sdf.Rotate(Sdf.Ellipse(20.6f, 23f, 4f, 1.8f), 20.6f, 23f, -30), Rgba.Hex(0x45B84F), Rgba.Hex(0x1F6E28), .6f);
                    c.Paint(Sdf.Rotate(Sdf.Ellipse(11.6f, 25f, 3.6f, 1.6f), 11.6f, 25f, 30), Rgba.Hex(0x45B84F), Rgba.Hex(0x1F6E28), .6f);
                    c.Paint(Sdf.Union(Sdf.Circle(16f, 9.6f, 7.4f), Sdf.Polygon(Sdf.Pts(9.4f, 10f, 16f, 18.6f, 22.6f, 10f))), Rgba.Hex(0xE5263A), Rgba.Hex(0x8C0F1E), 1f);
                    Line(c, Sdf.Quad(10.6f, 9.4f, 16f, 17.4f, 21.4f, 9.4f), 1f, Rgba.Hex(0x9E1324));
                    Line(c, Sdf.Arc(16f, 9f, 2.6f, 150, 390, 12), 1f, Rgba.Hex(0x9E1324));
                    c.Paint(Sdf.Ellipse(12.6f, 6.2f, 1.8f, 1f), White.WithAlpha(.45f));
                    break;
                case "heart":
                    float beat = frame == 0 ? 11.6f : 12.6f;
                    Heart(c, 16f, 16.6f, beat, Red, RedEdge, 1.1f);
                    c.Paint(Sdf.Rotate(Sdf.Ellipse(10.4f, 10.2f, 2.8f, 1.6f), 10.4f, 10.2f, -40), White.WithAlpha(.6f));
                    break;
                case "heartbreak":
                {
                    var heart = Sdf.Polygon(Sdf.HeartPoints(16f, 16.6f, 11.6f));
                    var left = Sdf.Polygon(Sdf.Pts(-2, -2, 16, -2, 16, 6.4f, 13.6f, 11.4f, 18.2f, 15.6f, 13.8f, 20.4f, 17.4f, 24.6f, 16, 34, -2, 34));
                    var right = Sdf.Polygon(Sdf.Pts(16, -2, 34, -2, 34, 34, 16, 34, 17.4f, 24.6f, 13.8f, 20.4f, 18.2f, 15.6f, 13.6f, 11.4f, 16, 6.4f));
                    var gray = Rgba.Hex(0xC23A3A);
                    c.Paint(Sdf.Rotate(Sdf.Move(Sdf.Intersect(heart, left), -1.6f, .4f), 10, 18, -8), gray, RedEdge, 1f);
                    c.Paint(Sdf.Rotate(Sdf.Move(Sdf.Intersect(heart, right), 1.6f, .8f), 22, 18, 8), gray, RedEdge, 1f);
                    break;
                }
                case "hug":
                    Heart(c, 16f, 15.4f, 8.6f, Rgba.Hex(0xFF7FA6), Rgba.Hex(0xC2406A), 1f);
                    Skin(c, Sdf.Union(Sdf.Capsule(2.4f, 26f, 10.4f, 20f, 2.2f), Sdf.Circle(11.4f, 19.4f, 3f)));
                    Skin(c, Sdf.Union(Sdf.Capsule(29.6f, 26f, 21.6f, 20f, 2.2f), Sdf.Circle(20.6f, 19.4f, 3f)));
                    break;
                case "thumbsup":
                case "thumbsdown":
                    if (id == "thumbsdown") c.FlipY = true;
                    c.Paint(Sdf.Box(3.4f, 15.6f, 10.2f, 28.4f, 1.2f), Rgba.Hex(0x3E7BE0), Rgba.Hex(0x234E9A), 1f);
                    Skin(c, Sdf.Union(Sdf.Box(9.4f, 14.4f, 25.6f, 28.6f, 3.4f), Sdf.Capsule(13.8f, 15.6f, 15.4f, 4.4f, 3f)));
                    for (float y = 18.6f; y < 27; y += 3.2f) Seg(c, 19.4f, y, 25f, y, .8f, Outline);
                    break;
                case "handshake":
                    c.Paint(Sdf.Box(.6f, 13f, 8.4f, 24.6f, 1.2f), Rgba.Hex(0x3E7BE0), Rgba.Hex(0x234E9A), 1f);
                    c.Paint(Sdf.Box(23.6f, 13f, 31.4f, 24.6f, 1.2f), Rgba.Hex(0xF08A2A), Rgba.Hex(0xA4561A), 1f);
                    Skin(c, Sdf.Box(7.4f, 12.4f, 24.6f, 25.2f, 4.2f));
                    Skin(c, Sdf.Capsule(11.4f, 13.4f, 19.6f, 11.4f, 2.1f));
                    for (float x = 14.4f; x < 23; x += 2.8f) Seg(c, x, 17.6f, x - 1.6f, 24.2f, .7f, Outline);
                    break;
                case "victory":
                    Skin(c, Sdf.Union(Sdf.Capsule(13.4f, 19.6f, 9.8f, 4f, 2.4f), Sdf.Capsule(18.6f, 19.6f, 22.4f, 4f, 2.4f)));
                    Skin(c, Sdf.Box(9.6f, 16.6f, 23.4f, 30.4f, 3.6f));
                    Seg(c, 16f, 19.4f, 16f, 23.6f, .8f, Outline);
                    Skin(c, Sdf.Capsule(10.4f, 22.6f, 17.4f, 24.2f, 2.1f));
                    break;
                case "ok":
                    Skin(c, Sdf.Union(Sdf.Capsule(16.6f, 17f, 16.2f, 3.8f, 2f), Sdf.Capsule(20.4f, 17f, 21.4f, 4.6f, 2f), Sdf.Capsule(24f, 18.6f, 26.2f, 8f, 1.9f)));
                    Skin(c, Sdf.Box(12.6f, 15.6f, 27.6f, 30.2f, 4.2f));
                    c.Paint(Sdf.Ring(10.4f, 17.6f, 4.4f, 3.4f), SkinTone, Outline, 1f);
                    c.Paint(Sdf.Ring(10.4f, 17.6f, 4.4f, 3.4f), SkinTone);
                    break;
                case "moon":
                    c.Paint(Sdf.Subtract(Sdf.Circle(15f, 16f, 12.4f), Sdf.Circle(21.6f, 12.4f, 10.6f)), (x, y) => Rgba.Lerp(Rgba.Hex(0xFFF2A0), Rgba.Hex(0xF5C531), y / 30), Rgba.Hex(0xB8861A), 1f);
                    Line(c, Sdf.Quad(7.4f, 17f, 8.6f, 19.2f, 10.6f, 18.4f), 1.1f);
                    Star(c, 24.6f, 21.6f, 2.8f); Star(c, 27.6f, 6.6f, 1.8f);
                    break;
                case "sun":
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45 * (float)Math.PI / 180;
                        c.Paint(Sdf.Capsule(16 + (float)Math.Cos(a) * 11f, 16 + (float)Math.Sin(a) * 11f, 16 + (float)Math.Cos(a) * 14.4f, 16 + (float)Math.Sin(a) * 14.4f, 1.5f), Rgba.Hex(0xFF9A1E));
                    }
                    c.Paint(Sdf.Circle(16f, 16f, 8.8f), (x, y) => Rgba.Lerp(Rgba.Hex(0xFFE04A), Rgba.Hex(0xFF8F1C), (y - 7) / 18), Rgba.Hex(0xC4610E), 1f);
                    c.Paint(Sdf.Ellipse(12.8f, 12f, 2.6f, 1.5f), White.WithAlpha(.5f));
                    break;
                default:
                    Head(c); Dots(c); Smile(c); break;
            }
        }

        static void Star(YYCanvas c, float x, float y, float r)
        {
            c.Paint(Sdf.Polygon(Sdf.Pts(x, y - r, x + r * .3f, y - r * .3f, x + r, y, x + r * .3f, y + r * .3f, x, y + r, x - r * .3f, y + r * .3f, x - r, y, x - r * .3f, y - r * .3f)), Rgba.Hex(0xFFE36A), Rgba.Hex(0xB8861A), .5f);
        }

        // ───────────── 斗图 heads (white blob heads with thick black lines) ─────────────

        static readonly Rgba Blob = Rgba.Hex(0xFAFAF6);
        static readonly Rgba BlobInk = Rgba.Hex(0x161616);

        /// <summary>A sticker picture; the caption is drawn over it by the chat view in bold text.</summary>
        public static YYCanvas DrawSticker(YYSticker sticker, int size = 96)
        {
            var c = new YYCanvas(size);
            if (sticker == null) return c;
            string head = sticker.Head;
            // Every sticker sits on a pale card so the bold caption below it reads on any bubble colour.
            c.Paint(Sdf.Box(.5f, .5f, 31.5f, 31.5f, 3f), Rgba.Hex(0xFFFFFF), Rgba.Hex(0xD6DCE4), .4f);
            if (head == "square")
                c.Paint(Sdf.Box(8f, 3.4f, 24f, 19.4f, 1.6f), Blob, BlobInk, 1.1f);
            else if (head != "boat")
                c.Paint(Sdf.Ellipse(16f, 11.6f, 8.6f, 8f), Blob, BlobInk, 1.1f);
            const float lx = 12.6f, rx = 19.4f, ey = 10.4f;
            switch (head)
            {
                case "dizzy":
                    foreach (float ex in new[] { lx, rx })
                    {
                        var spiral = new List<float>();
                        for (int i = 0; i <= 24; i++) { float a = i * 30 * (float)Math.PI / 180, rr = .2f + i * .09f; spiral.Add(ex + (float)Math.Cos(a) * rr); spiral.Add(ey + (float)Math.Sin(a) * rr); }
                        c.Paint(Sdf.Stroke(spiral, .8f), BlobInk);
                    }
                    c.Paint(Sdf.Stroke(Sdf.Pts(12.6f, 15.6f, 14.2f, 14.8f, 15.8f, 15.8f, 17.4f, 14.8f, 19f, 15.6f), .9f), BlobInk);
                    c.Paint(Sdf.Ellipse(9.6f, 14f, 1.6f, .9f), Blush); c.Paint(Sdf.Ellipse(22.4f, 14f, 1.6f, .9f), Blush);
                    break;
                case "deadpan":
                    c.Paint(Sdf.Capsule(10.6f, ey, 14.4f, ey, .5f), BlobInk); c.Paint(Sdf.Capsule(17.6f, ey, 21.4f, ey, .5f), BlobInk);
                    c.Paint(Sdf.Circle(13.4f, ey + .6f, .7f), BlobInk); c.Paint(Sdf.Circle(20.4f, ey + .6f, .7f), BlobInk);
                    c.Paint(Sdf.Capsule(13.6f, 15.6f, 18.4f, 15.6f, .45f), BlobInk);
                    break;
                case "shock":
                    foreach (float ex in new[] { lx, rx }) { c.Paint(Sdf.Circle(ex, ey, 2.2f), Blob, BlobInk, .7f); c.Paint(Sdf.Circle(ex, ey, .6f), BlobInk); }
                    c.Paint(Sdf.Ellipse(16f, 16f, 1.8f, 2.4f), BlobInk);
                    for (float x = 11.6f; x < 21; x += 2.2f) c.Paint(Sdf.Capsule(x, 4.6f, x, 6.6f, .3f), Rgba.Hex(0x3050C0));
                    break;
                case "boat":
                    c.Paint(Sdf.Rotate(Sdf.Polygon(Sdf.Pts(5f, 12f, 27f, 12f, 23f, 18f, 9f, 18f)), 16f, 15f, -18), Rgba.Hex(0xE07A3A), BlobInk, .8f);
                    c.Paint(Sdf.Rotate(Sdf.Polygon(Sdf.Pts(16f, 3f, 16f, 11.6f, 22.6f, 11.6f)), 16f, 15f, -18), Blob, BlobInk, .7f);
                    c.Paint(Sdf.Stroke(Sdf.Pts(2f, 20.4f, 6f, 19f, 10f, 20.4f, 14f, 19f, 18f, 20.4f, 22f, 19f, 26f, 20.4f, 30f, 19f), .9f), Rgba.Hex(0x3C8CE6));
                    c.Paint(Sdf.Circle(9f, 9.4f, 2.8f), Blob, BlobInk, .7f);
                    c.Paint(Sdf.Circle(8.2f, 9f, .4f), BlobInk); c.Paint(Sdf.Circle(9.8f, 9f, .4f), BlobInk);
                    break;
                case "flex":
                    c.Paint(Sdf.Capsule(lx - 1.6f, ey - 1.6f, lx + 1.4f, ey - .4f, .45f), BlobInk); c.Paint(Sdf.Capsule(rx + 1.6f, ey - 1.6f, rx - 1.4f, ey - .4f, .45f), BlobInk);
                    c.Paint(Sdf.Circle(lx, ey + 1f, .9f), BlobInk); c.Paint(Sdf.Circle(rx, ey + 1f, .9f), BlobInk);
                    c.Paint(Sdf.Box(13f, 14.6f, 19f, 17f, .8f), Blob, BlobInk, .6f);
                    c.Paint(Sdf.Union(Sdf.Capsule(24.6f, 17.4f, 27.6f, 11.6f, 2f), Sdf.Circle(26.6f, 14f, 2.6f), Sdf.Circle(27.4f, 9.8f, 1.8f)), Blob, BlobInk, .8f);
                    c.Paint(Sdf.Stroke(Sdf.Pts(28.6f, 5.4f, 30.4f, 3.6f), .6f), Rgba.Hex(0xE04A2A)); c.Paint(Sdf.Stroke(Sdf.Pts(30.4f, 8f, 31.4f, 7.2f), .6f), Rgba.Hex(0xE04A2A));
                    break;
                case "cry":
                    c.Paint(Sdf.Ellipse(16f, 11.6f, 8.6f, 8f), Rgba.Hex(0xC9DAFF), BlobInk, 1.1f);
                    c.Paint(Sdf.Stroke(Sdf.Quad(lx - 1.8f, ey, lx, ey + 1.6f, lx + 1.8f, ey), .7f), BlobInk); c.Paint(Sdf.Stroke(Sdf.Quad(rx - 1.8f, ey, rx, ey + 1.6f, rx + 1.8f, ey), .7f), BlobInk);
                    c.Paint(Sdf.Box(lx - .8f, ey + 1.4f, lx + .8f, 19f, .8f), Tear.WithAlpha(.85f)); c.Paint(Sdf.Box(rx - .8f, ey + 1.4f, rx + .8f, 19f, .8f), Tear.WithAlpha(.85f));
                    c.Paint(Sdf.Stroke(Sdf.Quad(14f, 16.4f, 16f, 14.6f, 18f, 16.4f), .8f), BlobInk);
                    c.Paint(Sdf.Union(Sdf.Intersect(Sdf.Circle(26.6f, 18.4f, 3.4f), Sdf.Box(22f, 13f, 31f, 18.4f)), Sdf.Box(25.4f, 18f, 27.8f, 21.4f, .6f)), Rgba.Hex(0x4F7FE0), BlobInk, .6f);
                    break;
                case "square":
                    c.Paint(Sdf.Box(10.6f, 8.4f, 13.6f, 11.4f), BlobInk); c.Paint(Sdf.Box(18.4f, 8.4f, 21.4f, 11.4f), BlobInk);
                    c.Paint(Sdf.Box(13.4f, 14.4f, 18.6f, 16.4f), Blob, BlobInk, .6f);
                    c.Paint(Sdf.Rotate(Sdf.Box(23.6f, 2f, 28.6f, 7f), 26f, 4.5f, 20), Blob, BlobInk, .6f);
                    break;
                case "pout":
                    c.Paint(Sdf.Capsule(lx - 1.8f, ey - 2.2f, lx + 1.6f, ey - 3f, .4f), BlobInk); c.Paint(Sdf.Capsule(rx + 1.8f, ey - 2.2f, rx - 1.6f, ey - 3f, .4f), BlobInk);
                    foreach (float ex in new[] { lx, rx }) { c.Paint(Sdf.Ellipse(ex, ey, 1.4f, 1.8f), BlobInk); c.Paint(Sdf.Circle(ex + .5f, ey - .5f, .5f), Blob); }
                    c.Paint(Sdf.Stroke(Sdf.Pts(14f, 16.4f, 15.3f, 15.4f, 16.6f, 16.4f, 17.9f, 15.4f), .7f), BlobInk);
                    c.Paint(Sdf.Ellipse(9.6f, 14f, 1.6f, .9f), Blush); c.Paint(Sdf.Ellipse(22.4f, 14f, 1.6f, .9f), Blush);
                    break;
                case "thumbs":
                    c.Paint(Sdf.Stroke(Sdf.Quad(lx - 1.8f, ey + .6f, lx, ey - 1.4f, lx + 1.8f, ey + .6f), .8f), BlobInk); c.Paint(Sdf.Stroke(Sdf.Quad(rx - 1.8f, ey + .6f, rx, ey - 1.4f, rx + 1.8f, ey + .6f), .8f), BlobInk);
                    c.Paint(Sdf.Intersect(Sdf.Ellipse(16f, 13.4f, 3.6f, 3.4f), Sdf.Box(10, 13.4f, 22, 20)), BlobInk);
                    c.Paint(Sdf.Union(Sdf.Box(22.6f, 13.6f, 29.4f, 20.4f, 1.8f), Sdf.Capsule(24.4f, 14f, 25f, 8.6f, 1.6f)), Blob, BlobInk, .7f);
                    break;
                case "kneel":
                    c.Paint(Sdf.Stroke(Sdf.Pts(lx - 1.6f, ey + 1.2f, lx + 1.6f, ey + 1.2f), .8f), BlobInk); c.Paint(Sdf.Stroke(Sdf.Pts(rx - 1.6f, ey + 1.2f, rx + 1.6f, ey + 1.2f), .8f), BlobInk);
                    c.Paint(Sdf.Ellipse(16f, 16f, 1f, .7f), BlobInk);
                    c.Paint(Sdf.Union(Sdf.Capsule(6.6f, 17.6f, 10.6f, 13.6f, 1.6f), Sdf.Capsule(25.4f, 17.6f, 21.4f, 13.6f, 1.6f)), Blob, BlobInk, .7f);
                    break;
                case "glasses":
                    c.Paint(Sdf.Union(Sdf.Ring(lx, ey, 2.6f, .9f), Sdf.Ring(rx, ey, 2.6f, .9f), Sdf.Capsule(lx + 2.6f, ey, rx - 2.6f, ey, .4f)), BlobInk);
                    c.Paint(Sdf.Circle(lx, ey, 2.2f), Rgba.Hex(0xBFE6FF, .9f)); c.Paint(Sdf.Circle(rx, ey, 2.2f), Rgba.Hex(0xBFE6FF, .9f));
                    c.Paint(Sdf.Capsule(lx - 1.2f, ey + .8f, lx + .8f, ey - 1.2f, .35f), Blob); c.Paint(Sdf.Capsule(rx - 1.2f, ey + .8f, rx + .8f, ey - 1.2f, .35f), Blob);
                    c.Paint(Sdf.Stroke(Sdf.Quad(13.6f, 15.6f, 17f, 17f, 19f, 14.6f), .8f), BlobInk);
                    break;
                case "melon":
                    c.Paint(Sdf.Circle(lx, ey, 1f), BlobInk); c.Paint(Sdf.Circle(rx, ey, 1f), BlobInk);
                    c.Paint(Sdf.Intersect(Sdf.Circle(16f, 13.4f, 6.4f), Sdf.Box(6, 15.4f, 26, 24)), Rgba.Hex(0x2E9A3A), BlobInk, .7f);
                    c.Paint(Sdf.Intersect(Sdf.Circle(16f, 13.4f, 5.4f), Sdf.Box(6, 15.4f, 26, 24)), Rgba.Hex(0xF2465A));
                    for (float x = 13.4f; x < 19; x += 2.6f) c.Paint(Sdf.Ellipse(x, 17f, .4f, .6f), BlobInk);
                    break;
            }
            return c;
        }
    }
}
