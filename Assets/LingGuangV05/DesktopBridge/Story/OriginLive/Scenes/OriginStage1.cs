using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using Out = LingGuangV05.XingGuang.XgOriginStage1.Out;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Scene 2, stage 1 (22.5–40 s): the perceptron, the SMS scatter, the line learning, and XOR that one line cannot split. Maths: <see cref="XgOriginStage1"/>.</summary>
    public sealed class OriginStage1 : OriginScene
    {
        const float X0 = 1030, Y0 = 760, PW = 700, PH = 540;
        static readonly Color Panel = new Color32(11, 16, 13, 255), Panel2 = new Color32(12, 18, 14, 255), StampFill = new Color32(20, 4, 4, 255);
        static float Sx(double x) => (float)(X0 + x * PW);
        static float Sy(double y) => (float)(Y0 - y * PH);

        OriginCanvas under, canvas;
        OriginLabels labels;
        OriginStageTag tag;
        OriginSpeech speech;
        readonly XgOriginStage1.Frame frame = new XgOriginStage1.Frame();
        readonly Vector2[] quad = new Vector2[4], path = new Vector2[5];

        static string T(string zh, string en) => OriginLiveContext.T(zh, en);

        protected override void OnBuild()
        {
            under = Ctx.ClippedCanvas("Plot Line", Root, X0, Y0 - PH, PW, PH);
            canvas = Ctx.Canvas("Draw", Root);
            labels = Ctx.Labels("Labels", Root);
            tag = new OriginStageTag(Ctx, Root);
            speech = new OriginSpeech(Ctx, Root, 64);
        }

        public override IEnumerable<string> WarmText()
        {
            yield return "数字个数「中奖」次数偏置条件过线是否错修正次分对垃圾短信正常短信最多既又不反过来也算一条直线分不开学会简单判断感知机线性分类器那时我说在你这儿";
            yield return "digits you've won count bias condition over the line yes no wrong corrections right spam normal best possible A but not B and the other way round One line can't split it Learning simple decisions linear classifier ON YOUR SIDE";
        }

        public override void Render(float t)
        {
            float s = Local(t);
            XgOriginStage1.Evaluate(s, frame);
            var f = frame;
            var dim = OriginInk.Dim();
            var lime = OriginInk.Lime; var red = OriginInk.Red; var ink = OriginInk.Ink;
            bool xor = f.XorPhase;

            // ---- the decision line and its half-plane, clipped to the plot ----
            var up = under.Begin();
            up.Opacity = (float)f.LineA;
            var L = f.Line;
            for (int i = 0; i < 4; i++) quad[i] = new Vector2(Sx(L.Half[i * 2]), Sy(L.Half[i * 2 + 1]));
            up.Poly(quad, 4, lime, .07f);
            up.Line(Sx(L.Ax), Sy(L.Ay), Sx(L.Bx), Sy(L.By), 2.5f, lime, 1, 7);
            under.End();

            var pen = canvas.Begin();
            labels.Begin();

            // ---- perceptron diagram (left) ----
            float da = (float)f.DiagramA;
            pen.Opacity = da;
            double[] wv = xor ? new[] { f.XW0, f.XW1, f.XB } : new[] { f.W0, f.W1, f.B };
            string[] labs = xor ? new[] { T("条件 A", "condition A"), T("条件 B", "condition B"), T("偏置 b", "bias b") } : new[] { T("数字个数", "digits"), T("「中奖」次数", "\"won\" count"), T("偏置 b", "bias b") };
            float[] ny = { 330, 520, 690 };
            const float nx = 260, sumX = 580, sumY = 510;
            for (int i = 0; i < 3; i++)
            {
                double w = wv[i];
                var col = w >= 0 ? lime : red;
                float px = Mathf.Lerp(nx, sumX, (float)f.Pulse), py = Mathf.Lerp(ny[i], sumY, (float)f.Pulse);
                pen.Opacity = da * .65f;
                pen.Line(nx, ny[i], sumX, sumY, .8f + Mathf.Min(4, (float)System.Math.Abs(w) * 2.2f), col, .75f);
                pen.Opacity = da;
                if (s > 1.6f) pen.Disc(px, py, 3.5f, lime, .9f, 2.5f);
                pen.Disc(nx, ny[i], i == 2 ? 14 : 22, Panel);
                pen.Ring(nx, ny[i], i == 2 ? 14 : 22, 1.5f, ink, .6f);
                labels.Serif(labs[i], nx - 40, ny[i], i == 2 ? 20 : 24, i == 2 ? dim : ink, da, OriginAnchor.End);
                labels.Mono((i == 2 ? "b " : "w" + (i + 1) + " ") + (w >= 0 ? "+" : "") + w.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    Mathf.Lerp(nx, sumX, .48f), Mathf.Lerp(ny[i], sumY, .48f) - 18, 15, w >= 0 ? lime : red, da, OriginAnchor.Middle);
            }
            pen.Disc(sumX, sumY, 56, Panel2);
            pen.Ring(sumX, sumY, 56, 2, lime, 1, 2.5f);
            labels.Serif("Σ", sumX, sumY + 2, 52, lime, da);
            pen.Line(sumX + 56, sumY, 720, sumY, 1, ink, .5f);
            pen.StrokeRect(720, sumY - 30, 70, 60, 1, ink, .5f);
            path[0] = new Vector2(728, sumY + 18); path[1] = new Vector2(755, sumY + 18); path[2] = new Vector2(755, sumY - 18); path[3] = new Vector2(782, sumY - 18);
            pen.Polyline(path, 4, 2, lime);
            pen.Line(790, sumY, 830, sumY, 1, ink, .5f);
            var oc = f.Output == Out.Wrong ? red : lime;
            string outText = f.Output == Out.Yes ? T("是", "yes") : f.Output == Out.No ? T("否", "no") : T("错", "ERR");
            labels.Serif(outText, 880, sumY, 56, oc, da, OriginAnchor.Middle, 0, 700, new OriginGlow(oc, .35f, 0f, .4f));
            labels.Mono(T("过线 → 是", "over the line → yes"), 755, sumY + 52, 12, dim, da, OriginAnchor.Middle);

            // ---- plot frame ----
            pen.Opacity = (float)f.PlotA;
            pen.Line(X0, Y0, X0 + PW, Y0, 1, ink, .35f);
            pen.Line(X0, Y0, X0, Y0 - PH, 1, ink, .35f);
            labels.Mono(xor ? T("条件 A →", "condition A →") : T("数字个数 →", "digits →"), X0 + PW, Y0 + 24, 13, dim, (float)f.PlotA, OriginAnchor.End);
            labels.Mono(xor ? T("↑ 条件 B", "↑ condition B") : T("↑ 「中奖」类关键词", "↑ \"won\" words"), X0, Y0 - PH - 22, 13, dim, (float)f.PlotA);

            // ---- scatter ----
            float sa = (float)(f.ScatterA * XgOriginColdOpen.Ramp(s, 1.0, 2.2));
            if (sa > .001f)
            {
                for (int i = 0; i < XgOriginStage1.Points.Length; i++)
                {
                    var p = XgOriginStage1.Points[i];
                    float ap = sa * (float)XgOriginColdOpen.Ramp(s, 1.0 + i * .02, 1.5 + i * .02);
                    if (ap <= .001f) continue;
                    pen.Opacity = ap;
                    float cx = Sx(p.X), cy = Sy(p.Y);
                    if (p.Spam) pen.Disc(cx, cy, 7, lime, 1, 2.5f); else pen.Ring(cx, cy, 6, 1.8f, ink);
                    if (!f.PointOk(i) && s > 3.4f) pen.Ring(cx, cy, 12, 1.5f, red, .8f);
                    if (i == f.FlashIdx) pen.Ring(cx, cy, 12 + (1 - (float)f.FlashA) * 30, 2, red, (float)f.FlashA);
                }
                pen.Opacity = sa;
                bool all = f.Correct == XgOriginStage1.Points.Length;
                labels.Mono(T("修正 " + f.Fixes + " 次 · 分对 " + f.Correct + " / 46", f.Fixes + " corrections · " + f.Correct + " / 46 right"), X0 + PW, Y0 - PH - 22, 14, all ? lime : ink, sa, OriginAnchor.End);
                pen.Disc(X0 + 20, Y0 + 50, 6, lime);
                labels.Mono(T("垃圾短信（是）", "spam (yes)"), X0 + 34, Y0 + 50, 13, dim, sa);
                pen.Ring(X0 + 200, Y0 + 50, 5, 1.6f, ink);
                labels.Mono(T("正常短信（否）", "normal (no)"), X0 + 214, Y0 + 50, 13, dim, sa);
            }

            // ---- XOR ----
            float xa = (float)f.XorA;
            if (xa > .001f)
            {
                pen.Opacity = xa;
                for (int i = 0; i < 4; i++)
                {
                    var p = XgOriginStage1.Xor[i]; bool yes = p[2] > 0;
                    float cx = Sx(p[0]), cy = Sy(p[1]);
                    if (yes) pen.Disc(cx, cy, 18, lime, 1, 7); else pen.Ring(cx, cy, 16, 2.5f, ink);
                    if (f.Wrong[i] && s > 10.3f) pen.Ring(cx, cy, 30 + 4 * Mathf.Sin(s * 9), 2.5f, red, 1, 2.5f);
                    labels.Mono(XgOriginStage1.XorLabels[i].Replace('✗', '×') + " → " + (yes ? T("是", "yes") : T("否", "no")), cx, cy + 46, 14, ink, xa, OriginAnchor.Middle);
                }
                labels.Mono(T("最多分对 3 / 4", "best possible: 3 / 4 right"), X0 + PW, Y0 - PH - 22, 14, red, xa, OriginAnchor.End);
                labels.Serif(T("既 A，又不 B —— 反过来也算", "A but not B — and the other way round"), X0 + PW / 2, Y0 + 44, 24, ink, xa);
            }

            // ---- stamp ----
            float st = (float)f.Stamp;
            if (st > .001f)
            {
                float k = Mathf.Lerp(1.3f, 1, st);
                float cx = X0 + PW / 2, cy = Y0 - PH / 2;
                pen.Opacity = st;
                pen.Push(cx, cy, k, -4);
                // The film's stamp carries filter="url(#g1)" inside a translated group, and that filter region starts at the group's origin, so only
                // the lower-right quadrant of the box (x, y >= 0 in its own space) is ever painted. Reproduced as rendered.
                pen.Rect(0, 0, 250, 44, StampFill, .75f);
                path[0] = new Vector2(250, 0); path[1] = new Vector2(250, 44); path[2] = new Vector2(0, 44);
                pen.Polyline(path, 3, 2.5f, red, 1, false, 2.5f);
                pen.Pop();
                Vector2 P(float lx, float ly)
                {
                    float r = -4 * Mathf.Deg2Rad, c = Mathf.Cos(r), sn = Mathf.Sin(r);
                    return new Vector2(cx + k * (lx * c - ly * sn), cy + k * (lx * sn + ly * c));
                }
                var a1 = P(0, -8); var a2 = P(0, 26);
                labels.Serif(T("一条直线，分不开", "One line can't split it"), a1.x, a1.y, 34, red, st, OriginAnchor.Middle, 0, 700, default, -4, k);
                labels.Mono("LINEARLY INSEPARABLE · Minsky & Papert 1969", a2.x, a2.y, 13, red, st, OriginAnchor.Middle, 1.5f, default, -4, k);
            }
            pen.Opacity = 1;

            labels.End(); canvas.End();
            tag.Render(s, Mathf.FloorToInt(s * 30 + .001f), 1, T("学会简单判断", "Learning simple decisions"), T("PERCEPTRON · 感知机 / 线性分类器", "PERCEPTRON · linear classifier"), "1958", "2016.06");
            speech.Render(s, T("是。　否。", "yes.　no."), 15.2f, 460, 800, 4);
        }
    }
}
