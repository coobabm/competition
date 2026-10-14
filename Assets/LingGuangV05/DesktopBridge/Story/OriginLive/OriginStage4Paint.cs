using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using Anchor = LingGuangV05.Desktop.Story.OriginTextPool.Anchor;


namespace LingGuangV05.Desktop.Story
{
    /// <summary>Draws film stage 4 (see <see cref="XgOriginStage4"/>) with the pen: the chain, plot, conveyor and ResNet shortcuts.</summary>
    public static class OriginStage4Paint
    {
        static string T(string zh, string en) { return OriginLiveContext.T(zh, en); }

        const float CY = (float)XgOriginStage4.ChainY;

        public static void Paint(double t, XgOriginStage4.Frame f, OriginBrush pen, OriginBrush front, OriginTextPool tx)
        {
            var S = XgOriginStage4.Sentence;
            int n = S.Length;
            float cxLast = (float)XgOriginStage4.Cx(n - 1);
            float cxKey = (float)XgOriginStage4.Cx(XgOriginStage4.Key);
            var Lime = OriginPal.Lime; var Ink = OriginPal.Ink; var Red = OriginPal.Red;
            float dim = OriginPal.DimA;


            // chain (shared by parts A and C)
            pen.Alpha = (float)f.Upper;
            for (int i = 0; i < n; i++)
            {
                float lit = (float)XgOriginStage4.CellLit(f, i);
                float cx = (float)XgOriginStage4.Cx(i);
                bool key = i == XgOriginStage4.Key;
                pen.Rect(cx - 19, CY - 19, 38, 38, Lime, .05f + lit * .7f);
                pen.StrokeRect(cx - 19, CY - 19, 38, 38, key ? 2 : 1, key ? Lime : OriginPal.Frame, key ? 1 : .3f);
                tx.Text(S[i].ToString(), cx, CY + 52, 24, key ? Lime : Ink, (key ? 1 : .75f) * pen.Alpha, Anchor.Middle, true);
            }
            if (!f.Hit && t > .8)
            {
                float sx = (float)XgOriginFx.Lerp(cxKey, cxLast, f.U);
                float sig = (float)f.Sig;
                pen.Disc(sx, CY - 40, 4 + 7 * sig, Lime, .15f + .85f * sig, OriginBrush.G2);
                tx.Text(XgOriginStage4.SignalLabel(f), sx, CY - 72, 15, sig < .01f ? Red : Lime, pen.Alpha, Anchor.Middle, false, 1.5f);
            }
            if (!f.Hit && t > 6.4)
                tx.Text("？", cxLast + 4, CY - 120, 44, Red, pen.Alpha, Anchor.Middle, true, 0, 1);
            pen.Alpha = 1;

            // degradation plot
            if (f.PlotA > .001)
            {
                pen.Alpha = (float)f.PlotA;
                float px = (float)XgOriginStage4.PlotX, py = (float)XgOriginStage4.PlotY, pw = (float)XgOriginStage4.PlotW, ph = (float)XgOriginStage4.PlotH;
                pen.Line(px, py + ph, px + pw, py + ph, 1, Ink, .35f);
                pen.Line(px, py, px, py + ph, 1, Ink, .35f);
                tx.Text(T("↑ 训练误差（CIFAR-10）", "↑ training error (CIFAR-10)"), px, py - 24, 13, OriginPal.DimRgb, dim * pen.Alpha, Anchor.Start, false, 1.5f);
                tx.Text(T("训练轮数 →", "epochs →"), px + pw, py + ph + 24, 13, OriginPal.DimRgb, dim * pen.Alpha, Anchor.End, false, 1.5f);
                tx.Text(T("He et al. 2015 · 示意", "He et al. 2015 · illustrative"), px + pw, py - 24, 12, OriginPal.DimRgb, dim * pen.Alpha, Anchor.End, false, 1.5f);
                int cnt = XgOriginStage4.CurvePoints(f.Draw);
                pen.Stroke(CurvePts(XgOriginStage4.C20, cnt), 2.4f, OriginPal.Lav, 1, OriginBrush.G1);
                pen.Stroke(CurvePts(XgOriginStage4.C56, cnt), 2.4f, Red, 1, OriginBrush.G1);
                float la = (float)XgOriginFx.Ramp(t, 11.4, 12) * pen.Alpha;
                tx.Text(T("20 层", "20 layers"), px + pw + 12, (float)XgOriginStage4.PlotYOf(XgOriginStage4.C20[119]), 16, OriginPal.Lav, la, Anchor.Start, false, 1.5f);
                tx.Text(T("56 层 · 更差", "56 layers · worse"), px + pw + 12, (float)XgOriginStage4.PlotYOf(XgOriginStage4.C56[119]), 16, Red, la, Anchor.Start, false, 1.5f);
                pen.Alpha = 1;
            }

            // highway / conveyor
            float x0 = (float)XgOriginStage4.Cx(0) - 20, x1 = cxLast + 20;
            float hx = Mathf.Lerp(x0, x1, (float)f.Hw);
            if (f.Hit) pen.Line(x0, CY - 90, hx, CY - 90, 3, Lime, f.Upper > 0 ? (float)f.Upper : .0001f, OriginBrush.G2);
            else pen.Line(x0, CY - 90, hx, CY - 90, 1.5f, Ink, .35f * (1 - (float)XgOriginFx.Ramp(t, 15.95, 16)));
            if (f.Hit)
            {
                pen.Alpha = (float)f.Upper;
                for (int i = 0; i < n; i++)
                {
                    float cx = (float)XgOriginStage4.Cx(i);
                    pen.Line(cx, CY - 19, cx, CY - 90, 1, Lime, .25f);
                    if (i % 3 == 0)
                    {
                        pen.Disc(cx, CY - 54, 7, OriginPal.Panel, 1);
                        pen.Stroke(OriginBrush.EllipsePoints(cx, CY - 54, 7, 7, 0, 28), 1.2f, Lime, 1, 0, true);
                    }
                }
                float bx = Mathf.Lerp(cxKey, cxLast, (float)f.Conv);
                pen.Disc(bx, CY - 90, 9, new Color32(242, 255, 216, 255), 1, OriginBrush.G3);
                float u = pen.Alpha;
                tx.Text(T("细胞状态 · 记忆的传送带", "cell state · the memory conveyor"), x0, CY - 118, 14, Lime, u, Anchor.Start, false, 1.5f);
                if (f.Conv > .1 && f.Conv < .92)
                    tx.Text(XgOriginFx.Fixed(System.Math.Pow(.997, f.ConvPos - XgOriginStage4.Key), 2), bx, CY - 118, 14, Lime, u, Anchor.Middle, false, 1.5f);
                tx.Text(T("σ 门：遗忘门 / 输入门 / 输出门 —— 该留的留，该忘的忘 · LSTM 1997 · GRU 2014 简化为两扇门", "σ gates: forget / input / output — keep what matters, drop the rest · LSTM 1997 · GRU 2014 folds them into two gates"), x0, CY + 96, 13, OriginPal.DimRgb, dim * u, Anchor.Start, false, 1.5f);
                float ga = (float)XgOriginFx.Ramp(t, 18.5, 19) * u;
                tx.Text(T("→ 海", "→ sea"), cxLast - 30, CY - 150, 44, Lime, ga, Anchor.End, true, 0, 2);
                pen.Alpha = 1;
            }

            // ResNet
            float ra = (float)(f.ResA * f.EndDim);
            if (ra > .001f)
            {
                pen.Alpha = ra;
                tx.Text(T("ResNet 2015 · 残差捷径：输出 = F(x) + x · ImageNet top-5 错误率 3.57%", "ResNet 2015 · residual shortcut: output = F(x) + x · ImageNet top-5 error 3.57%"), 160, 650, 14, Lime, ra, Anchor.Start, false, 1.5f);
                tx.Text(f.Layers + T(" 层", " layers"), 1760, 650, 30, Lime, ra, Anchor.End, false, 1.5f);
                for (int i = 0; i < 26; i++)
                {
                    float rx = (float)XgOriginStage4.Rb(i);
                    pen.Rect(rx, 712, 44, 46, Lime, .06f + (f.Arcs * 26 > i ? .3f : 0));
                    pen.StrokeRect(rx, 712, 44, 46, 1, OriginPal.Frame, .35f);
                }
                for (int k = 0; k < 13; k++)
                {
                    int i = k * 2;
                    if (i + 2 > 25) continue;
                    float p = Mathf.Clamp01((float)(f.Arcs * 13 - k));
                    if (p <= 0) continue;
                    float ax = (float)XgOriginStage4.Rb(i) + 22, bxx = (float)XgOriginStage4.Rb(i + 2) + 22;
                    var arc = OriginBrush.Quadratic(new Vector2(ax, 712), new Vector2((ax + bxx) / 2, 664), new Vector2(bxx, 712), 24);
                    float len = OriginBrush.Length(arc);
                    var part = OriginBrush.Trim(arc, Mathf.Min(len, 200 * p));
                    pen.Stroke(part, 2, Lime, 1, OriginBrush.G1);
                }
                pen.Alpha = 1;
            }

            // the white flash when the highway lands
            front.Rect(0, 0, 1920, 1080, new Color32(244, 255, 224, 255), (float)f.Flash * .28f);
        }

        static List<Vector2> CurvePts(double[] ys, int count)
        {
            var l = new List<Vector2>(count);
            for (int i = 0; i < count && i < ys.Length; i++) l.Add(new Vector2((float)XgOriginStage4.PlotXOf(i), (float)XgOriginStage4.PlotYOf(ys[i])));
            return l;
        }
    }
}
