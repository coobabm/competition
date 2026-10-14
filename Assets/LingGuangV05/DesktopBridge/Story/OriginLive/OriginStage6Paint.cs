using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using Anchor = LingGuangV05.Desktop.Story.OriginTextPool.Anchor;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Draws film stage 6 (see <see cref="XgOriginStage6"/>): serial row, strike-outs, title, attention web, stack and capability table.</summary>
    public static class OriginStage6Paint
    {
        static string T(string zh, string en) { return OriginLiveContext.T(zh, en); }

        public static void Paint(double t, XgOriginStage6.Frame f, OriginBrush pen, OriginBrush front, OriginTextPool tx)
        {
            var Lime = OriginPal.Lime; var Ink = OriginPal.Ink; var Red = OriginPal.Red; var Dim = OriginPal.DimRgb; float dim = OriginPal.DimA;
            var tok = OriginLiveContext.English ? XgOriginStage6.TokEn : XgOriginStage6.Tok;


            // everything in the shaken group
            pen.Dx = front.Dx = tx.Dx = (float)f.ShakeX; pen.Dy = front.Dy = tx.Dy = (float)f.ShakeY;
            float end = (float)f.EndDim;

            // sequential RNN arrows
            float arrows = (float)(f.ArrowsA * (1 - f.ToRing)) * end;
            if (arrows > .001f)
            {
                for (int i = 0; i < tok.Length - 1; i++)
                {
                    bool on = i < f.Step - 1;
                    var c = on ? Lime : Color.white; float ca = (on ? 1 : .25f) * arrows;
                    float x1 = (float)XgOriginStage6.RowX(i) + 50, x2 = (float)XgOriginStage6.RowX(i + 1) - 50;
                    pen.Line(x1, 470, x2, 470, 2, c, ca);
                    pen.Stroke(new[] { new Vector2(x2 - 8, 464), new Vector2(x2, 470), new Vector2(x2 - 8, 476) }, 2, c, ca);
                }
                tx.Text(T("串行 · 第 " + Mathf.Max(1, f.Step) + " / 8 步", "serial · step " + Mathf.Max(1, f.Step) + " / 8"), 960, 590, 20, Red, arrows, Anchor.Middle, false, 1.5f);
            }

            // crossed out
            for (int k = 0; k < 2; k++)
            {
                double a, s; XgOriginStage6.Crossed(k, t, out a, out s);
                if (a <= .001) continue;
                float kx = k == 0 ? 700 : 1220;
                tx.Text(k == 0 ? T("循环 RECURRENCE", "RECURRENCE") : T("卷积 CONVOLUTION", "CONVOLUTION"), kx, 320, 24, Ink, (float)a * end, Anchor.Middle, false, 1.5f);
                front.Line(kx - 130, 320, kx - 130 + 260 * (float)s, 320, 4, Red, (float)a * end, OriginBrush.G1);
            }

            // all-to-all attention web
            if (f.WebA > .001)
            {
                float wa = (float)f.WebA * end;
                pen.Alpha = wa;
                for (int i = 0; i < 8; i++)
                    for (int j = i + 1; j < 8; j++)
                        for (int h = 0; h < XgOriginStage6.HeadCount; h++)
                        {
                            var col = OriginPal.Hex(XgOriginStage6.HeadHex[h]);
                            Vector2 p = new Vector2((float)f.PosX[i], (float)f.PosY[i]), q = new Vector2((float)f.PosX[j], (float)f.PosY[j]);
                            float bend = (h - 1.5f) * 26;
                            Vector2 m = (p + q) * .5f;
                            Vector2 nrm = new Vector2(-(q.y - p.y), q.x - p.x);
                            float nl = nrm.magnitude; if (nl < 1e-4f) nl = 1; nrm /= nl;
                            Vector2 ctl = m + nrm * bend;
                            pen.Stroke(OriginBrush.Quadratic(p, ctl, q, 10), 1.1f, col, .125f);
                            if ((i + j + h) % 3 == 0)
                            {
                                float fl = (float)XgOriginStage6.Flow(t, i, j, h), u = 1 - fl;
                                Vector2 d = u * u * p + 2 * u * fl * ctl + fl * fl * q;
                                pen.Disc(d.x, d.y, 2.2f, col, 1, OriginBrush.G1);
                            }
                        }
                tx.Text(T("所有词 · 同时 · 1 步", "all words · at once · 1 step"), (float)f.Cx, 470 + (float)f.R + 70, 18, Lime, wa * (1 - (float)XgOriginFx.Ramp(t, 12.0, 12.5)), Anchor.Middle, false, 1.5f);
                float leg = wa * (1 - (float)f.ToLeft);
                if (leg > .001f)
                    for (int h = 0; h < XgOriginStage6.HeadCount; h++)
                    {
                        pen.Rect(1380, 700 + h * 26, 22, 4, OriginPal.Hex(XgOriginStage6.HeadHex[h]), 1);
                        tx.Text(T("注意力头 ", "attention head ") + (h + 1), 1412, 702 + h * 26, 12, Dim, dim * leg, Anchor.Start, false, 1.5f);
                    }
                pen.Alpha = 1;
            }

            // tokens
            for (int i = 0; i < tok.Length; i++)
            {
                bool lit = f.ToRing > .5 || i < f.Step;
                float x = (float)f.PosX[i], y = (float)f.PosY[i];
                float ta = (float)f.TokDim * end;
                pen.RoundBox(x - 50, y - 32, 100, 64, 4,
                    lit ? new Color32(20, 32, 22, 255) : new Color32(10, 14, 12, 255), (lit ? .95f : .9f) * ta,
                    lit ? Lime : Color.white, (lit ? 1f : .2f) * ta, 1.5f, lit && f.ToRing < .5 && i == f.Step - 1 ? OriginBrush.G2 : 0);
                tx.Text(tok[i], x, y, OriginLiveContext.English ? 21 : 30, lit ? Ink : Dim, (lit ? 1 : dim) * ta, Anchor.Middle, true);
            }

            // transformer stack
            for (int i = 0; i < 6; i++)
            {
                float a = (float)XgOriginStage6.StackA(i, t) * end;
                if (a <= .001f) continue;
                float y = 742 - i * 82 + (1 - (float)XgOriginStage6.StackA(i, t)) * 30;
                pen.Box(1120, y - 62, 560, 66, new Color32(14, 22, 17, 255), .85f * a, Lime, .5f * a, 1);
                pen.Box(1134, y - 52, 250, 46, Lime, .14f * a, Lime, a, 1);
                tx.Text(T("多头注意力", "multi-head attention"), 1259, y - 29, 20, Lime, a, Anchor.Middle, true);
                pen.Box(1404, y - 52, 200, 46, OriginPal.Cyan, .08f * a, OriginPal.Cyan, a, 1);
                tx.Text(T("前馈网络", "feed-forward"), 1504, y - 29, 20, OriginPal.Cyan, a, Anchor.Middle, true);
                pen.Line(1650, y + 4, 1650, y - 62, 1.6f, OriginPal.Amber, a);
                tx.Text("+x", 1662, y - 29, 10, OriginPal.Amber, a, Anchor.Start, false, 1.5f);
            }
            tx.Text(T("× 6 层 · Add & Norm · 残差 + 注意力", "× 6 layers · Add & Norm · residual + attention"), 1120, 240, 16, Lime, (float)XgOriginFx.Ramp(t, 14.2, 14.8) * end, Anchor.Start, false, 1.5f);

            // the paper title
            if (f.TitleA > .001)
            {
                float a = (float)f.TitleA;
                float sc = (float)XgOriginFx.Lerp(1.06, 1, XgOriginFx.Ramp(t, 7, 8.5));
                float top = 540 - (1.17f * 66 + 22 + 1.17f * 18) * .5f;
                tx.Text("ATTENTION ONLY", 960, top + 1.17f * 66 * .5f, 66 * sc, new Color32(243, 255, 224, 255), a, Anchor.Middle, false, 2 * sc, 3, 0, null, Lime);
                tx.Text(T("2016.12 · 在你的电脑上", "2016.12 · ON YOUR COMPUTER"), 960, top + 1.17f * 66 + 22 + 1.17f * 18 * .5f, 18, Dim, dim * a, Anchor.Middle, false, 6);
            }
            pen.Dx = pen.Dy = front.Dx = front.Dy = tx.Dx = tx.Dy = 0;

            // capability table
            if (f.CapA > .001)
            {
                float ca = (float)f.CapA;
                bool on = XgOriginStage6.CapsOn(t);
                tx.Text(on ? T("能力表 · 6 / 6 · 同时亮起", "capabilities · 6 / 6 · all lit at once") : T("能力表 · 参数还在涨……", "capabilities · parameters still climbing…"), 960, 772, 13, on ? Lime : Dim, (on ? 1 : dim) * ca, Anchor.Middle, false, 4);
                for (int i = 0; i < XgOriginStage6.Caps.Length; i++)
                {
                    float x = 960 + (i - 2.5f) * 172;
                    float fa = on ? .28f + .5f * (float)f.BoomK : (float)XgOriginStage6.CapFlicker(t, i);
                    pen.Alpha = ca;
                    pen.Box(x - 76, 790, 152, 46, Lime, fa, on ? Lime : Color.white, on ? 1 : .18f, on ? 2 : 1, on ? OriginBrush.G2 : 0);
                    pen.Alpha = 1;
                    tx.Text(OriginLiveContext.English ? XgOriginStage6.CapsEn[i] : XgOriginStage6.Caps[i], x, 813, OriginLiveContext.English ? 17 : 22, on ? new Color32(244, 255, 224, 255) : Dim, (on ? 1 : dim) * ca, Anchor.Middle, true);
                }
            }

            // the flash of the boom
            float bk = (float)(f.BoomK * f.BoomK * .6);
            if (bk > .001f)
            {
                var stops = new List<(float, Color, float)>
                {
                    (0, Color.white, 1),
                    (502, new Color32(244, 255, 224, 255), .7f),
                    (1256, new Color32(212, 245, 90, 255), .25f),
                    (1320, new Color32(212, 245, 90, 255), .25f),
                };
                front.Alpha = bk;
                front.RadialColors(960, 810, stops, 72);
                front.Alpha = 1;
            }
        }
    }
}
