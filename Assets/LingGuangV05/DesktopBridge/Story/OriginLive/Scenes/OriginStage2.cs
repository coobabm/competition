using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using N = LingGuangV05.XingGuang.XgOriginStage2;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Scene 3, stage 2 (40–58 s): the hidden layer, backprop pulses, the XOR heat map that snaps after a long plateau, and the three combination cards. Maths: <see cref="XgOriginStage2"/>.</summary>
    public sealed class OriginStage2 : OriginScene
    {
        static readonly Color Panel = new Color32(11, 16, 13, 255), Panel2 = new Color32(12, 18, 14, 255), Panel3 = new Color32(10, 14, 12, 255), Hot = new Color32(244, 255, 224, 255);
        static readonly Color Faint = new Color32(200, 232, 212, 255);
        const float FaintA = .12f;

        OriginCanvas canvas, cardGfx;
        OriginLabels labels, cardLabels;
        OriginStageTag tag;
        OriginSpeech speech;
        readonly N.Frame frame = new N.Frame();
        readonly Vector2[] pts = new Vector2[64];

        static string T(string zh, string en) => OriginLiveContext.T(zh, en);

        protected override void OnBuild()
        {
            canvas = Ctx.Canvas("Draw", Root);
            labels = Ctx.Labels("Labels", Root);
            cardGfx = Ctx.Canvas("Cards", Root);
            cardLabels = Ctx.Labels("Card Labels", Root);
            tag = new OriginStageTag(Ctx, Root);
            speech = new OriginSpeech(Ctx, Root, 64);
        }

        public override IEnumerable<string> WarmText()
        {
            yield return "隐藏层误差沿连线往回传非线性激活分对训练中第轮还是条件弹幕反话标题党逻辑组合题前方高能辣眼睛震惊竟然下雨没带伞会淋湿学会组合特征多层感知机反向传播不确定那时我说在你这儿";
            yield return "HIDDEN error flows back along the wires BACKPROP nonlinear activation right training epoch still condition danmaku sarcasm clickbait logic combo high alert ahead eye-burning SHOCKING unbelievably rain no umbrella get wet Learning to combine features MLP + backprop";
        }

        // Clip a segment to x >= 0, y >= 0 (the film's filter region, see Quadrant); false if nothing is left.
        static bool ClipQuadrant(ref Vector2 a, ref Vector2 b)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                float va = pass == 0 ? a.x : a.y, vb = pass == 0 ? b.x : b.y;
                if (va < 0 && vb < 0) return false;
                if (va < 0) a = Vector2.Lerp(a, b, -va / (vb - va));
                else if (vb < 0) b = Vector2.Lerp(a, b, va / (va - vb));
            }
            return true;
        }

        public override void Render(float t)
        {
            float s = Local(t);
            N.Evaluate(s, frame);
            var f = frame;
            var dim = OriginInk.Dim();
            var lime = OriginInk.Lime; var red = OriginInk.Red; var ink = OriginInk.Ink; var amber = OriginInk.Amber;
            var pen = canvas.Begin();
            labels.Begin();

            // ---------- network ----------
            float netA = (float)f.NetA;
            if (netA > .001f)
            {
                pen.Opacity = netA;
                for (int i = 0; i < 12; i++)
                {
                    var e = N.Edges[i];
                    float hidA = (float)XgOriginColdOpen.Ramp(s, .4 + (e.K % 4) * .25, 1.3 + (e.K % 4) * .25);
                    double w = f.EdgeWeight[i];
                    pen.Opacity = netA * hidA;
                    pen.Line((float)e.A.X, (float)e.A.Y, (float)e.B.X, (float)e.B.Y, .6f + Mathf.Min(4, (float)System.Math.Abs(w) * 1.6f), w >= 0 ? lime : red, w >= 0 ? .6f : .55f);
                }
                pen.Opacity = netA;
                if (f.Fw >= 0)
                    for (int i = 0; i < 12; i++)
                    {
                        double p = i < 8 ? f.Fw * 2 : f.Fw * 2 - 1;
                        if (p < 0 || p > 1) continue;
                        var e = N.Edges[i];
                        pen.Disc((float)XgOriginColdOpen.Lerp(e.A.X, e.B.X, p), (float)XgOriginColdOpen.Lerp(e.A.Y, e.B.Y, p), 3.6f, lime, 1, 2.5f);
                    }
                if (f.Bw >= 0)
                    for (int i = 0; i < 12; i++)
                    {
                        double p = i < 8 ? 1 - (f.Bw * 2 - 1) : 1 - f.Bw * 2;
                        if (p < 0 || p > 1) continue;
                        var e = N.Edges[i];
                        pen.Disc((float)XgOriginColdOpen.Lerp(e.A.X, e.B.X, p), (float)XgOriginColdOpen.Lerp(e.A.Y, e.B.Y, p), 3.6f, amber, 1, 2.5f);
                    }
                for (int i = 0; i < 2; i++)
                {
                    var n = N.In[i];
                    pen.Disc((float)n.X, (float)n.Y, 24, Panel);
                    pen.Ring((float)n.X, (float)n.Y, 24, 1.5f, ink, .6f);
                    labels.Serif(i == 0 ? "A" : "B", (float)n.X, (float)n.Y, 24, ink, netA);
                }
                for (int i = 0; i < 4; i++)
                {
                    var n = N.Hidden[i];
                    float a = (float)XgOriginColdOpen.Ramp(s, .4 + i * .25, 1.3 + i * .25);
                    float act = (float)XgOriginColdOpen.Ramp(s, 3.0 + i * .15, 3.8 + i * .15);
                    if (a <= .001f) continue;
                    // The film draws these with filter="url(#g1)" inside translate(x y) scale(k): the filter region starts at the node's own
                    // origin, so only the lower-right quadrant (x, y >= 0) of each node shows. Reproduced as rendered.
                    float k = Mathf.Lerp(.4f, 1, a);
                    pen.Opacity = netA * a;
                    pen.Push((float)n.X, (float)n.Y, k);
                    pen.Pie(0, 0, 26, 0, 90, Panel2);
                    pen.Arc(0, 0, 26, 0, 90, 2, lime, 1, 2.5f);
                    pen.Opacity = netA * a * act;
                    Vector2 c0 = new Vector2(-13, 7), c1 = new Vector2(0, 7), c2 = new Vector2(12, -9);
                    var q0 = c1; var q1 = c2;
                    if (ClipQuadrant(ref q0, ref q1)) pen.Line(q0, q1, 2.2f, lime, 1, 2.5f);
                    pen.Pop();
                }
                pen.Opacity = netA;
                pen.Disc((float)N.Out.X, (float)N.Out.Y, 30, Panel2);
                pen.Ring((float)N.Out.X, (float)N.Out.Y, 30, 2, ink);
                labels.Serif(T("是/否", "y/n"), (float)N.Out.X, (float)N.Out.Y, 22, ink, netA);
                labels.Mono(T("隐藏层 HIDDEN", "HIDDEN LAYER"), 480, 222, 13, lime, netA * (float)XgOriginColdOpen.Ramp(s, .6, 1.4), OriginAnchor.Middle);
                labels.Mono(T("← 误差沿连线往回传 · BACKPROP", "← the error flows back along the wires · BACKPROP"), 480, 740, 14, amber, netA * (float)XgOriginColdOpen.Ramp(s, 6.6, 7.2), OriginAnchor.Middle);
                // activation inset
                float ia = netA * (float)XgOriginColdOpen.Ramp(s, 3, 3.8);
                if (ia > .001f)
                {
                    pen.Opacity = ia;
                    pen.Push(840, 600);
                    pen.Rect(0, 0, 190, 130, new Color32(8, 12, 10, 255), .8f);
                    pen.StrokeRect(0, 0, 190, 130, 1, Faint, FaintA);
                    pen.Line(10, 110, 180, 110, 1, Faint, FaintA);
                    double kk = XgOriginColdOpen.Ramp(s, 3, 4.6, XgOriginColdOpen.Linear);
                    int cnt = 0;
                    for (int i = 0; i <= 40; i++)
                    {
                        if (i / 40.0 > kk) break;
                        double x = -6 + i * .3;
                        pts[cnt++] = new Vector2(15 + i * 4, (float)(110 - XgOriginColdOpen.Sigmoid(x) * 90));
                    }
                    pen.Polyline(pts, cnt, 2.2f, lime, 1, false, 2.5f);
                    pen.Pop();
                    labels.Mono("σ(x) " + T("非线性激活", "nonlinear activation"), 840 + 12, 600 + 14, 12, lime, ia);
                }
            }

            // ---------- XOR heat map ----------
            float ha = (float)f.HeatA;
            if (ha > .001f)
            {
                pen.Opacity = ha;
                float flash = (float)f.SnapFlash;
                pen.StrokeRect((float)N.HX - 1, (float)N.HY - 1, (float)N.HS + 2, (float)N.HS + 2, 1 + flash * 4, flash > 0 ? lime : Faint, flash > 0 ? 1 : FaintA, flash > 0 ? 7 : 0);
                float cs = (float)(N.HS / N.Grid);
                for (int i = 0; i < N.Grid; i++)
                    for (int j = 0; j < N.Grid; j++)
                    {
                        double o = N.Mlp((i + .5) / N.Grid, (j + .5) / N.Grid, f.Params);
                        bool edge = System.Math.Abs(o - .5) < .07;
                        pen.Rect((float)N.HX + i * cs, (float)(N.HY + N.HS) - (j + 1) * cs, cs - 1, cs - 1, lime, edge ? .55f : (float)(o * .26));
                    }
                for (int i = 0; i < 4; i++)
                {
                    var p = N.Xor[i]; bool yes = p[2] > 0;
                    bool ok = (N.Mlp(p[0], p[1], f.Params) > .5) == yes;
                    float cx = (float)(N.HX + p[0] * N.HS), cy = (float)(N.HY + N.HS - p[1] * N.HS);
                    if (yes) pen.Disc(cx, cy, 17, lime, 1, 7);
                    else { pen.Disc(cx, cy, 15, new Color32(10, 14, 12, 255)); pen.Ring(cx, cy, 15, 2.5f, ink); }
                    if (!ok) pen.Ring(cx, cy, 28 + 3 * Mathf.Sin(s * 9), 2.4f, red);
                }
                labels.Mono(T("分对 " + f.Correct + " / 4" + (f.Correct == 4 ? " ✓" : ""), f.Correct + " / 4 right" + (f.Correct == 4 ? " ✓" : "")), (float)(N.HX + N.HS), (float)N.HY - 22, 15, f.Correct == 4 ? lime : red, ha, OriginAnchor.End);
                labels.Mono(T("↑ 条件 B", "↑ condition B"), (float)N.HX, (float)N.HY - 22, 13, dim, ha);
                pen.Rect((float)N.HX, (float)N.HY, (float)N.HS, (float)N.HS, Hot, flash * .45f);
                labels.Mono(T("训练中 · 第 " + f.Epoch + " 轮 · 还是 2 / 4", "training · epoch " + f.Epoch + " · still 2 / 4"), (float)(N.HX + N.HS / 2), (float)(N.HY + N.HS / 2), 13, dim, ha * (float)f.TrainingTextA, OriginAnchor.Middle);
                labels.Mono(T("条件 A →", "condition A →"), (float)(N.HX + N.HS), (float)(N.HY + N.HS) + 24, 13, dim, ha, OriginAnchor.End);
                labels.Mono("Rumelhart · Hinton · Williams 1986", (float)N.HX, (float)(N.HY + N.HS) + 24, 12, dim, ha);
            }
            pen.Opacity = 1;
            labels.End(); canvas.End();

            RenderCards(s);
            tag.Render(s, Mathf.FloorToInt(s * 30 + .001f), 2, T("学会组合特征", "Learning to combine features"), T("MLP · 多层感知机 + 反向传播", "MLP · multi-layer perceptron + backprop"), "1986", "2016.07");
            speech.Render(s, T("……[不确定]。", "…[not sure]."), 15.2f, 110, 640, 6, false);
        }

        OriginText measure;

        float Measure(string text, float size)
        {
            if (measure == null) { measure = Ctx.Text("Measure", Root, Ctx.Serif, 24, Color.white, 0, TMPro.TextAlignmentOptions.Left); measure.Alpha(0); }
            return measure.Width(text);
        }

        /// <summary>Greedy line breaking like the browser's: break anywhere inside CJK runs, at spaces otherwise.</summary>
        List<string> Wrap(string text, float maxWidth)
        {
            var lines = new List<string>();
            string line = "";
            int i = 0;
            while (i < text.Length)
            {
                int j = i + 1;
                if (text[i] < 0x2E80) while (j < text.Length && text[j] != ' ' && text[j] < 0x2E80) j++;
                string chunk = text.Substring(i, j - i);
                if (j < text.Length && text[j] == ' ') { chunk += " "; j++; }
                if (line.Length > 0 && Measure((line + chunk).TrimEnd(), 24) > maxWidth) { lines.Add(line.TrimEnd()); line = ""; }
                line += chunk; i = j;
            }
            if (line.Length > 0) lines.Add(line.TrimEnd());
            return lines;
        }

        sealed class CardInfo { public string[] Parts; public float PlusW, Shrink, RowH, Height; public float[] Basis; public List<string>[] Lines; }

        const float CardContent = 270 - 42, CardLh24 = 0;

        CardInfo Layout(N.Card card)
        {
            var info = new CardInfo { Parts = OriginLiveContext.English ? card.PartsEn : card.Parts };
            var parts = info.Parts;
            // flex row: boxes (text + 18 padding/border) and the 18 px plus signs, gap 8; when it overflows the boxes shrink and their text wraps
            info.PlusW = Measure("+", 24) * 18f / 24f;
            info.Basis = new float[parts.Length];
            float total = 8 * (parts.Length - 1) * 2 + info.PlusW * (parts.Length - 1), sum = 0;
            for (int j = 0; j < parts.Length; j++) { info.Basis[j] = Measure(parts[j], 24) + 18; sum += info.Basis[j]; }
            total += sum;
            info.Shrink = total > CardContent ? (total - CardContent) / sum : 0;
            info.Lines = new List<string>[parts.Length];
            int maxLines = 1;
            for (int j = 0; j < parts.Length; j++)
            {
                float bw = info.Basis[j] * (1 - info.Shrink);
                info.Lines[j] = info.Shrink > 0 ? Wrap(parts[j], bw - 18 + 1) : new List<string> { parts[j] };
                maxLines = Mathf.Max(maxLines, info.Lines[j].Count);
            }
            info.RowH = maxLines * OriginLive.LhSerif * 24 + 6;
            info.Height = 1 + 18 + 14 * OriginLive.LhMono + 16 + info.RowH + 16 + OriginLive.LhSerif * 28 + 18 + 1;
            return info;
        }

        void RenderCards(float s)
        {
            var pen = cardGfx.Begin();
            cardLabels.Begin();
            var lime = OriginInk.Lime; var amber = OriginInk.Amber; var ink = OriginInk.Ink; var dim = OriginInk.Dim();
            // The three cards are flex items, so they stretch to the tallest one (the first card's text wraps); their content stays at the top.
            var infos = new CardInfo[N.Cards.Length];
            float tallest = 0;
            for (int c = 0; c < infos.Length; c++) { infos[c] = Layout(N.Cards[c]); tallest = Mathf.Max(tallest, infos[c].Height); }
            float lh = OriginLive.LhSerif * 24, titleH = 14 * OriginLive.LhMono, outH = OriginLive.LhSerif * 28;
            for (int c = 0; c < N.Cards.Length; c++)
            {
                float a = (float)XgOriginColdOpen.Ramp(s, 12.4 + c * .45, 13.2 + c * .45);
                if (a <= .001f) continue;
                var card = N.Cards[c]; var info = infos[c];
                float x = 110 + c * (270 + 22), y = 300 + (1 - a) * 24;
                pen.Opacity = a;
                pen.Rect(x, y, 270, tallest, new Color32(14, 22, 17, 255), .72f);
                pen.StrokeRect(x + .5f, y + .5f, 269, tallest - 1, 1, lime, .28f);
                float cx = x + 21;
                cardLabels.Mono(T(card.Title, card.TitleEn), cx, y + 19 + titleH * .5f, 14, lime, a, OriginAnchor.Start, 3);
                float rowTop = y + 19 + titleH + 16, rowC = rowTop + info.RowH * .5f;
                float px = cx;
                for (int j = 0; j < info.Parts.Length; j++)
                {
                    if (j > 0)
                    {
                        cardLabels.Serif("+", px + 8, rowC, 18, dim, a, OriginAnchor.Start, 0, 400);
                        px += 8 + info.PlusW + 8;
                    }
                    float bw = info.Basis[j] * (1 - info.Shrink), bh = info.Lines[j].Count * lh + 6;
                    for (int k = 0; k < info.Lines[j].Count; k++)
                        cardLabels.Serif(info.Lines[j][k], px + 9, rowC - bh * .5f + 3 + lh * (k + .5f), 24, ink, a, OriginAnchor.Start, 0, 400);
                    pen.StrokeRect(px + .5f, rowC - bh * .5f + .5f, bw - 1, bh - 1, 1, Color.white, .18f);
                    px += bw;
                }
                float outTop = rowTop + info.RowH + 16;
                cardLabels.Serif("→ " + T(card.Out, card.OutEn), cx, outTop + outH * .5f, 28, amber, a, OriginAnchor.Start, 0, 700, new OriginGlow(amber, .25f, 0f, .4f));
            }
            cardLabels.End(); cardGfx.End();
        }
    }
}
