using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using Anchor = LingGuangV05.Desktop.Story.OriginTextPool.Anchor;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Draws film stage 5 (see <see cref="XgOriginStage5"/>): the squeeze into a vector, the attention weights and matrix, and the captioned bird.</summary>
    public static class OriginStage5Paint
    {
        static string T(string zh, string en) { return OriginLiveContext.T(zh, en); }

        public static readonly Rect Image = new Rect((float)XgOriginStage5.ImgX, (float)XgOriginStage5.ImgY, (float)XgOriginStage5.ImgW, (float)XgOriginStage5.ImgH);

        public static void Paint(double t, XgOriginStage5.Frame f, OriginBrush pen, OriginBrush clip, OriginBrush front, OriginTextPool tx)
        {
            var Lime = OriginPal.Lime; var Ink = OriginPal.Ink; var Red = OriginPal.Red;
            var Dim = OriginPal.DimRgb; float dim = OriginPal.DimA;


            // ---------- A: squeeze into a vector ----------
            if (f.AA > .001)
            {
                float aA = (float)f.AA;
                pen.Alpha = aA;
                float bx = (float)XgOriginStage5.BoxX, by = (float)XgOriginStage5.BoxY, bw = (float)XgOriginStage5.BoxW, bh = (float)XgOriginStage5.BoxH;
                pen.Box(bx - bw / 2, by - bh / 2, bw, bh, OriginPal.Panel, .9f, Lime, 1, 1.6f, OriginBrush.G1);
                for (int k = 0; k < 8; k++)
                    pen.Rect(bx - 128 + k * 32, by - 12, 24, 24, Lime, .1f + (float)XgOriginStage5.SlotFill(k, t) * .7f);
                tx.Text(T("固定长度向量 · 整句话只能塞进这里", "fixed-length vector · the whole sentence has to fit in here"), bx, by - 56, 13, Lime, aA, Anchor.Middle, false, 1.5f);
                for (int j = 0; j < XgOriginStage5.Src.Length; j++)
                {
                    double x, y, size, op; bool lost;
                    XgOriginStage5.SrcWord(j, t, out x, out y, out size, out op, out lost);
                    tx.Text(XgOriginStage5.Src[j], (float)XgOriginStage5.SrcX[j], (float)XgOriginStage5.SrcY, 22, Ink, .18f * aA, Anchor.Middle, false, 0);
                    tx.Text(XgOriginStage5.Src[j], (float)x, (float)y, (float)size, lost ? Red : Ink, (float)op * aA, Anchor.Middle, false, 0);
                }
                for (int i = 0; i < XgOriginStage5.Dec.Length; i++)
                {
                    string w = XgOriginStage5.Dec[i];
                    tx.Text(w.Replace("{", "").Replace("}", ""), 560 + i * 92, 720, 34, w.StartsWith("{") ? Red : Ink, (float)XgOriginStage5.DecAlpha(i, t) * aA, Anchor.Middle, true);
                }
                tx.Text(T("译文 →", "output →"), 560 - 46, 668, 12, Dim, dim * aA * (float)XgOriginFx.Ramp(t, 2.9, 3.3), Anchor.Start, false, 1.5f);
                pen.Alpha = 1;
            }

            // ---------- B: attention ----------
            if (f.BA > .001)
            {
                float bA = (float)f.BA;
                pen.Alpha = bA;
                int cur = f.Cur; float curP = (float)f.CurP;
                var tgt = XgOriginStage5.Tgt; var src2 = XgOriginStage5.Src2; var att = XgOriginStage5.Att;
                for (int i = 0; i < tgt.Length; i++)
                {
                    Color c = i == cur ? Lime : i < cur ? Ink : Dim;
                    float ca = i == cur || i < cur ? 1 : dim;
                    tx.Text(tgt[i], (float)XgOriginStage5.Tx(i), 300, 40, c, ca * bA, Anchor.Middle, true, 0, i == cur ? 1 : 0);
                }
                for (int j = 0; j < src2.Length; j++)
                {
                    float a = (float)att[cur][j] * curP;
                    float tcx = (float)XgOriginStage5.Tx(cur), tjx = (float)XgOriginStage5.Tx(j);
                    var curve = OriginBrush.Cubic(new Vector2(tcx, 330), new Vector2(tcx, 460), new Vector2(tjx, 470), new Vector2(tjx, 590), 28);
                    pen.Stroke(curve, 1 + a * 12, Lime, .12f + a * .85f, a > .3f ? OriginBrush.G1 : 0);
                    tx.Text(src2[j], tjx, 620, 28, Ink, bA, Anchor.Middle);
                    pen.Rect(tjx - 50, 652, 100 * a, 6, Lime, 1);
                    tx.Text(XgOriginFx.Fixed(a, 2), tjx, 676, 13, Lime, bA, Anchor.Middle, false, 1.5f);
                }
                tx.Text(T("译文：每写一个词 ——", "output: for every word I write —"), (float)XgOriginStage5.Tx(0) - 60, 250, 13, Dim, dim * bA, Anchor.Start, false, 1.5f);
                tx.Text(T("—— 回头看原文，权重越大看得越重 · Bahdanau et al. 2014", "— I look back at the source; the bigger the weight, the harder I look · Bahdanau et al. 2014"), (float)XgOriginStage5.Tx(0) - 60, 720, 13, Dim, dim * bA, Anchor.Start, false, 1.5f);
                float mx = (float)XgOriginStage5.MatX, my = (float)XgOriginStage5.MatY, mc = (float)XgOriginStage5.MatCell;
                for (int i = 0; i < att.Length; i++)
                    for (int j = 0; j < att[i].Length; j++)
                    {
                        bool shown = i < cur || (i == cur && curP > 0);
                        float x = mx + j * mc, y = my + i * mc;
                        if (shown) pen.Rect(x, y, mc - 3, mc - 3, Lime, .05f + (float)att[i][j] * .9f * (i == cur ? curP : 1));
                        else pen.Rect(x, y, mc - 3, mc - 3, Color.white, .03f);
                        if (i == cur) pen.StrokeRect(x, y, mc - 3, mc - 3, 1, Lime, .6f);
                    }
                for (int i = 0; i < tgt.Length; i++)
                    tx.Text(tgt[i], mx - 14, my + i * mc + mc / 2 - 1, 18, Dim, dim * bA, Anchor.End, true);
                for (int j = 0; j < src2.Length; j++)
                    tx.Text(src2[j], mx + j * mc + mc / 2, my + 5 * mc + 16, 11, Dim, dim * bA, Anchor.Middle, false, 1.5f);
                tx.Text(T("注意力矩阵", "attention matrix"), mx, my - 26, 12, Lime, bA, Anchor.Start, false, 1.5f);
                pen.Alpha = 1;
            }

            // ---------- C: show, attend and tell ----------
            if (f.CA > .001)
            {
                float cA = (float)f.CA;
                PaintImage(f, clip, cA);
                pen.Alpha = cA; front.Alpha = cA;
                front.StrokeRect(Image.x, Image.y, Image.width, Image.height, 1, OriginPal.Faint, OriginPal.FaintA);
                front.Dashed(OriginBrush.EllipsePoints((float)f.FocusX, (float)f.FocusY, (float)f.FocusR * .72f, (float)f.FocusR * .72f, 0, 96), 6, 6, 1.5f, Lime, .7f, true);
                tx.Text(T("看图说话 · Show, Attend and Tell · Xu et al. 2015", "image captioning · Show, Attend and Tell · Xu et al. 2015"), 960, 330, 13, Dim, dim * cA, Anchor.Start, false, 1.5f);
                float x = 960;
                var words = OriginLiveContext.English ? XgOriginStage5.WordsEn : XgOriginStage5.Words;
                for (int i = 0; i < words.Length; i++)
                {
                    string w = words[i];
                    float width = tx.Width(w, 56, true, 0);
                    bool on = i == f.WordIdx;
                    float wa = (float)XgOriginStage5.WordAlpha(i, t) * (on ? 1 : .7f) * cA;
                    tx.Text(w, x, 400 + 1.42f * 56 * .5f, 56, on ? Lime : Ink, wa, Anchor.Start, true, 0, on ? 2 : 0);
                    if (on) pen.Rect(x, 400 + 1.42f * 56, width, 2, Lime, cA * (float)XgOriginStage5.WordAlpha(i, t));
                    x += width + 14;
                }
                pen.Alpha = 1; front.Alpha = 1;
            }

            // ---------- D: it points out the word ----------
            if (f.DAlpha > .001)
            {
                float a = (float)f.DAlpha;
                string lab = T("你说 ›", "YOU SAID ›");
                string p1 = T("我", "I stayed up labelling "), p2 = T("又", "again"), p3 = T("标到半夜了。", ".");
                float wl = tx.Width(lab, 14, false, 5), w1 = tx.Width(p1, 36, true, 0), w2 = tx.Width(p2, 36, true, 0), w3 = tx.Width(p3, 36, true, 0);
                float total = wl + 18 + w1 + w2 + w3;
                float x0 = 960 - total / 2, cy = 300 + 1.42f * 36 * .5f;
                tx.Text(lab, x0, cy + 4, 14, Dim, dim * a, Anchor.Start, false, 5);
                float xs = x0 + wl + 18;
                tx.Text(p1, xs, cy, 36, Dim, dim * a, Anchor.Start, true);
                tx.Text(p2, xs + w1, cy, 36, Lime, a, Anchor.Start, true, 0, 2);
                pen.Rect(xs + w1, 300 + 1.42f * 36, w2, 2, Lime, a);
                tx.Text(p3, xs + w1 + w2, cy, 36, Dim, dim * a, Anchor.Start, true);
            }
        }

        static void PaintImage(XgOriginStage5.Frame f, OriginBrush p, float alpha)
        {
            p.Alpha = alpha;
            Rect r = Image;
            p.RectGradientV(r.x, r.y, r.width, r.height, OriginPal.Hex("0d1b22"), OriginPal.Hex("14241c"), 1);
            p.Disc(720, 300, 44, OriginPal.Hex("f3f7dc"), 1, OriginBrush.G3);
            var wood = OriginPal.Hex("3c2e22");
            var b1 = OriginBrush.Cubic(new Vector2(200, 560), new Vector2(330, 520), new Vector2(420, 512), new Vector2(520, 505), 20);
            var b2 = OriginBrush.Cubic(new Vector2(520, 505), new Vector2(600, 500), new Vector2(680, 488), new Vector2(760, 470), 20);
            b1.AddRange(b2.GetRange(1, b2.Count - 1));
            p.Stroke(b1, 14, wood, 1);
            p.Disc(200, 560, 7, wood, 1); p.Disc(760, 470, 7, wood, 1);
            var tw = OriginBrush.Cubic(new Vector2(560, 502), new Vector2(590, 470), new Vector2(620, 466), new Vector2(640, 470), 16);
            p.Stroke(tw, 7, wood, 1);
            p.Disc(560, 502, 3.5f, wood, 1); p.Disc(640, 470, 3.5f, wood, 1);
            var leaf = OriginPal.Hex("2f5a3a");
            float[][] leaves = { new float[] { 330, 528, -30 }, new float[] { 610, 470, 20 }, new float[] { 700, 476, -15 }, new float[] { 400, 515, 25 } };
            foreach (var l in leaves)
                p.Fill(OriginBrush.EllipsePoints(l[0], l[1] - 12, 18, 7, l[2], 28, l[0], l[1]), leaf, 1);
            // the bird, translated to (474, 452)
            const float ox = 474, oy = 452;
            Vector2 B(float x, float y) { return new Vector2(ox + x, oy + y); }
            var body = OriginPal.Hex("e6ecd8");
            p.Fill(new[] { B(-46, 4), B(-74, -6), B(-70, 12) }, OriginPal.Hex("cfd8c4"), 1);
            p.FillEllipse(ox - 10, oy, 40, 24, body, 1);
            p.FillEllipse(ox + 26, oy - 20, 17, 17, body, 1);
            p.Fill(new[] { B(41, -22), B(56, -17), B(41, -13) }, OriginPal.Amber, 1);
            p.FillEllipse(ox + 31, oy - 24, 2.6f, 2.6f, OriginPal.Hex("111111"), 1, 0, 14);
            var wing = OriginBrush.Cubic(B(-26, -4), B(-8, -22), B(14, -10), B(4, 6), 16);
            p.Fill(wing, OriginPal.Hex("b8c4ad"), 1);
            p.Line(ox - 6, oy + 22, ox - 10, oy + 44, 3, OriginPal.Amber, 1);
            p.Line(ox + 8, oy + 22, ox + 8, oy + 44, 3, OriginPal.Amber, 1);
            // spotlight: everything outside the focus circle is veiled
            float fr = (float)f.FocusR;
            p.Alpha = alpha;
            p.RadialAlpha((float)f.FocusX, (float)f.FocusY, Color.black, new[] { new Vector2(0, 0), new Vector2(fr * .7f, 0), new Vector2(fr, .78f), new Vector2(3000, .78f) }, 72, true);
            p.Alpha = 1;
        }
    }
}
