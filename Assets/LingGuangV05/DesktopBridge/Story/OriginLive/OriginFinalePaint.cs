using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using Anchor = LingGuangV05.Desktop.Story.OriginTextPool.Anchor;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Draws the film's finale (see <see cref="XgOriginFinale"/>): timeline, collapse, burst, title, embers and references.</summary>
    public static class OriginFinalePaint
    {
        static string T(string zh, string en) { return OriginLiveContext.T(zh, en); }
        static bool En { get { return OriginLiveContext.English; } }
        static string Title { get { return En ? XgOriginFinale.TitleEn : XgOriginFinale.TitleZh; } }
        const float CX = (float)XgOriginFinale.Cx, CY = (float)XgOriginFinale.Cy, Base = (float)XgOriginFinale.Base, Top = (float)XgOriginFinale.Top;

        public static void Paint(double t, XgOriginFinale.Frame f, OriginBrush pen, OriginBrush front, OriginTextPool tx)
        {
            var Lime = OriginPal.Lime; var Ink = OriginPal.Ink; var Dim = OriginPal.DimRgb; float dim = OriginPal.DimA; var Hot = OriginPal.Hot;
            pen.Dx = front.Dx = tx.Dx = (float)f.ShakeX; pen.Dy = front.Dy = tx.Dy = (float)f.ShakeY;
            float pull = (float)f.Pull, lineA = (float)f.LineA;
            var nodes = XgOriginFinale.Nodes;

            // ---------- the timeline ----------
            if (lineA > .001f)
            {
                float axes = (float)f.AxesA * lineA;
                if (axes > .001f)
                {
                    pen.Line(260, Base, 1690, Base, 1, Ink, .35f * axes);
                    pen.Line(260, Base, 260, Top - 30, 1, Ink, .35f * axes);
                    tx.Text(T("↑ 能力", "↑ capability"), 260, Top - 50, 13, Dim, dim * axes, Anchor.Start, false, 1.5f);
                    tx.Text(T("参数量（对数刻度）→", "parameters (log scale) →"), 1690, Base - 16, 13, Dim, dim * axes, Anchor.End, false, 1.5f);
                    pen.Line(1250, Top - 46, 1280, Top - 46, 2.5f, Lime, axes);
                    float lx = En ? 60 : 0;
                    tx.Text(T("能力", "capability"), 1290, Top - 46, 12, Lime, axes, Anchor.Start, false, 1.5f);
                    pen.Dashed(new[] { new Vector2(1360 + lx, Top - 46), new Vector2(1390 + lx, Top - 46) }, 5, 4, 2, OriginPal.Lav, axes);
                    tx.Text(T("loss（越低越懂）", "loss (lower = better)"), 1400 + lx, Top - 46, 12, OriginPal.Lav, axes, Anchor.Start, false, 1.5f);
                }
                // loss: smooth the whole way
                double drawn = XgOriginFinale.LossDrawn(t);
                var loss = new List<Vector2>(120);
                for (int k = 0; k < 120; k++)
                {
                    double x = 280 + (k / 119.0) * 1380 * drawn;
                    loss.Add(new Vector2((float)XgOriginFx.Lerp(x, CX, pull), (float)XgOriginFx.Lerp(XgOriginFinale.Loss(x), CY, pull)));
                }
                pen.Dashed(loss, 7, 6, 2, OriginPal.Lav, .8f * lineA);
                // capability: flat, flat, flat ... then straight up
                for (int i = 1; i < nodes.Length; i++)
                {
                    float k = (float)XgOriginFinale.SegmentProgress(i, t);
                    if (k <= 0) continue;
                    float ax = (float)XgOriginFinale.Nx(i - 1), ay = (float)XgOriginFinale.Ny(i - 1);
                    float bx = Mathf.Lerp(ax, (float)XgOriginFinale.Nx(i), k), by = Mathf.Lerp(ay, (float)XgOriginFinale.Ny(i), k);
                    pen.Line(Mathf.Lerp(ax, CX, pull), Mathf.Lerp(ay, CY, pull), Mathf.Lerp(bx, CX, pull), Mathf.Lerp(by, CY, pull),
                        i == 5 ? 4 : 2.5f, i == 5 ? new Color32(244, 255, 224, 255) : Lime, lineA, i == 5 ? OriginBrush.G3 : OriginBrush.G1);
                }
                for (int i = 0; i < nodes.Length; i++)
                {
                    float a = (float)XgOriginFinale.NodeAlpha(i, t) * lineA;
                    if (a <= .001f) continue;
                    bool last = i == 5;
                    float textA = (float)XgOriginFinale.NodeTextAlpha(t) * a;
                    float px = Mathf.Lerp((float)XgOriginFinale.Nx(i), CX, pull), py = Mathf.Lerp((float)XgOriginFinale.Ny(i), CY, pull);
                    float pop = (float)XgOriginFinale.NodePop(i, t);
                    float r = (last ? 13 : 8) * (1 + pull) + pop * 30;
                    pen.Disc(px, py, r, last ? new Color32(242, 255, 214, 255) : Lime, a, last ? OriginBrush.G3 : OriginBrush.G2);
                    if (last && pop > 0)
                        pen.Ellipse(px, py, 20 + (1 - pop) * 160, 20 + (1 - pop) * 160, 3 * pop, Lime, pop * a);
                    if (textA > .001f)
                    {
                        var n = nodes[i];
                        float nx = (float)XgOriginFinale.Nx(i), ny = (float)XgOriginFinale.Ny(i);
                        tx.Text(En ? n.NameEn : n.Name, nx, Base + 34, 24, Ink, textA, Anchor.Middle, true);
                        tx.Text(XgOriginFinale.NodeParams(i, f.FinalParams, En) + T(" 个参数", " parameters"), nx, Base + 64, 15, Lime, textA, Anchor.Middle, false, 1.5f);
                        tx.Text(T("在你这儿 · ", "ON YOUR SIDE · ") + (En ? n.MonthEn : n.Month), nx, Base + 88, 12, OriginPal.Amber, textA, Anchor.Middle, false, 1.5f);
                        string said = XgOriginFinale.NodeSay(i, En, f.SequenceFirst);
                        if (last) tx.Text(said, nx - 30, ny, 30, new Color32(244, 255, 224, 255), textA, Anchor.End, true);
                        else tx.Text(said, nx, ny - 34, 19, Dim, dim * textA, Anchor.Middle, true);
                    }
                }
                if (pull > .6f) pen.Disc(CX, CY, 10 + pull * 22, new Color32(246, 255, 224, 255), lineA, OriginBrush.G3);
            }

            // ---------- explosion ----------
            double since = f.Since;
            if (since >= 0 && since < 4)
            {
                double B = XgOriginFinale.Boom;
                for (int i = 0; i < XgOriginFinale.Rays.Length; i++)
                {
                    var r = XgOriginFinale.Rays[i];
                    float k = (float)XgOriginFx.Ramp(t, B, B + .35);
                    float a = (float)((1 - XgOriginFx.Ramp(t, B + .15, B + 1.8)) * .55);
                    if (a <= .002f) continue;
                    float ang = (float)(r.A + since * .08), L = 1500 * (float)r.L * k;
                    pen.Line(CX, CY, CX + Mathf.Cos(ang) * L, CY + Mathf.Sin(ang) * L * .75f, (float)r.W * 3, i % 3 != 0 ? Lime : Color.white, a, OriginBrush.G2);
                }
                float[] ds = { 0, .12f, .3f };
                for (int i = 0; i < 3; i++)
                {
                    float k = (float)XgOriginFx.Ramp(t, B + ds[i], B + ds[i] + 1.6, XgOriginFx.OutCubic);
                    if (k <= 0 || k >= 1) continue;
                    float rx = 60 + k * 1300;
                    pen.Ellipse(CX, CY, rx, rx * .62f, (1 - k) * (i == 0 ? 18 : 8), i == 1 ? OriginPal.Amber : Lime, (1 - k) * .9f, OriginBrush.G2, 72);
                }
                for (int i = 0; i < XgOriginFinale.Sparks.Length; i++)
                {
                    double x, y, op, back;
                    if (!XgOriginFinale.SparkAt(i, since, out x, out y, out op, out back)) continue;
                    var p = XgOriginFinale.Sparks[i];
                    Color col = p.White ? Color.white : p.Amber ? OriginPal.Amber : Lime;
                    if (back > 0)
                    {
                        float bx = (float)(x - System.Math.Cos(p.A) * back), by = (float)(y - System.Math.Sin(p.A) * back * .7);
                        pen.Line((float)x, (float)y, bx, by, (float)p.R, col, (float)op, OriginBrush.G1);
                    }
                    else pen.Disc((float)x, (float)y, (float)p.R, col, (float)op, OriginBrush.G1, 8);
                }
            }

            // ---------- title ----------
            if (f.TitleA > .001)
            {
                float a = (float)f.TitleA;
                float spread = (float)f.Spread, scale = (float)f.Scale, blur = (float)f.Blur;
                float size = 210 * scale, sp = spread * scale;
                float cy = 320 + 1.42f * 210 * .5f;
                float xc = CX + sp * .5f;
                if (f.Chroma > .3)
                {
                    float ch = (float)f.Chroma;
                    tx.Text(Title, xc - ch, cy, size, new Color32(255, 70, 90, 255), a * .5f, Anchor.Middle, true, sp, 0, blur);
                    tx.Text(Title, xc + ch, cy, size, new Color32(80, 230, 255, 255), a * .5f, Anchor.Middle, true, sp, 0, blur);
                }
                float glow = (float)f.Glow, br = (float)f.Breathe, fade = (float)f.Fade;
                // the film's three text-shadows: 150, 60 and 18 px blurs of lime. The softness stays tight (an SDF
                // halo spreads over the whole glyph padding at this size), or the title smears into a blob over the sub-line.
                var halos = new[]
                {
                    new OriginGlowSpec(.7f, 0f, .1f * (1 - fade) * Mathf.Min(1.6f, glow)),
                    new OriginGlowSpec(.4f, 0f, .22f * (1 - fade * .6f) * Mathf.Min(1.5f, .6f + glow * .4f)),
                    new OriginGlowSpec(.18f, 0f, .5f * Mathf.Min(1.4f, .7f + glow * .3f)),
                };
                tx.Text(Title, xc, cy, size, new Color32(245, 255, 226, 255), a, Anchor.Middle, true, sp, 0, blur, halos, Lime);
            }
            float sub = (float)f.SubA;
            if (sub > .001f)
                tx.Text(En ? XgOriginFinale.SubLineEn : XgOriginFinale.SubLine, CX + (10 + (float)f.Fade * 8) * .5f, CY + 132 + 1.17f * 18 * .5f, 18, Dim, dim * sub, Anchor.Middle, false, 10 + (float)f.Fade * 8);

            // ---------- embers ----------
            for (int i = 0; i < XgOriginFinale.Embers.Length; i++)
            {
                double x, y, op;
                if (!XgOriginFinale.EmberAt(i, t, out x, out y, out op)) continue;
                front.Disc((float)x, (float)y, (float)XgOriginFinale.Embers[i].R, i % 7 == 0 ? OriginPal.Amber : Lime, (float)op, OriginBrush.G1, 8);
            }

            // ---------- the white flash ----------
            if (f.Flash > .001)
            {
                front.Alpha = (float)f.Flash * .75f;
                front.RadialColors(960, 453.6f, new List<(float, Color, float)>
                {
                    (0, Color.white, 1), (401, new Color32(244, 255, 224, 255), 1), (1146, new Color32(244, 255, 224, 255), .6f), (1300, new Color32(244, 255, 224, 255), .6f),
                }, 72);
                front.Alpha = 1;
            }

            // ---------- references ----------
            float ra = (float)f.RefsA;
            if (ra > .001f)
            {
                string refs = XgOriginFinale.Refs;
                int cut = 254; int at = refs.LastIndexOf(' ', Mathf.Min(cut, refs.Length - 1));
                string l1 = refs.Substring(0, at), l2 = refs.Substring(at + 1);
                float lh = 1.7f * 11;
                tx.Text(l1, 960, 1040 - lh * 1.5f, 11, Dim, .3f * ra, Anchor.Middle);
                tx.Text(l2, 960, 1040 - lh * .5f, 11, Dim, .3f * ra, Anchor.Middle);
            }
        }
    }
}
