using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using N = LingGuangV05.XingGuang.XgOriginStage3;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Scene 4, stage 3 (58–77.5 s): pixels flatten and lay back into a panel, the 3×3 kernel scans, the nets get deeper, the RNN passes its state along. Maths: <see cref="XgOriginStage3"/>.</summary>
    public sealed class OriginStage3 : OriginScene
    {
        static readonly Color Faint = new Color32(200, 232, 212, 255);
        const float FaintA = .12f;
        OriginCanvas canvas;
        OriginLabels labels;
        OriginStageTag tag;
        OriginSpeech speech;
        readonly N.Frame frame = new N.Frame();

        static string T(string zh, string en) => OriginLiveContext.T(zh, en);

        protected override void OnBuild()
        {
            canvas = Ctx.Canvas("Draw", Root);
            labels = Ctx.Labels("Labels", Root);
            tag = new OriginStageTag(Ctx, Root);
            speech = new OriginSpeech(Ctx, Root, 56);
        }

        public override IEnumerable<string> WarmText()
        {
            yield return "256个像素排成一行谁挨着谁没了视觉线序列线输入特征图卷积核层隐状态一格往后传验证码表情包写诗新闻今晚的月亮很圆走向专长那时我说我看读在你这儿✓";
            yield return "256 pixels in one row who sits next to whom is gone VISION LINE SEQUENCE LINE input feature map kernel layers hidden state passed along cell by cell captcha emoji poems news Tonight the moon is very round Specialising Moon. Round.";
        }

        public override void Render(float t)
        {
            float s = Local(t);
            N.Evaluate(s, frame);
            var f = frame;
            var dim = OriginInk.Dim();
            var lime = OriginInk.Lime; var red = OriginInk.Red; var ink = OriginInk.Ink;
            var pen = canvas.Begin();
            labels.Begin();

            // pixels: grid -> row -> panel
            for (int i = 0; i < 256; i++)
            {
                N.PixelAt(i, f, out double x, out double y, out double sz);
                if (N.Pix[i] == 1) pen.Rect((float)x, (float)y, (float)sz, (float)sz, ink, 1);
                else pen.Rect((float)x, (float)y, (float)sz, (float)sz, Color.white, .06f);
            }
            labels.Mono(T("256 个像素排成一行 —— 谁挨着谁，没了", "256 pixels in one row — who sits next to whom is gone"), 960, 580, 15, red, (float)f.RowNoteA, OriginAnchor.Middle);

            float splitA = (float)f.SplitA;
            pen.Opacity = splitA;
            pen.Line(960, 250, 960, 820, 1, Faint, FaintA);
            labels.Mono(T("视觉线 · CNN", "VISION LINE · CNN"), 150, 244, 15, lime, splitA, OriginAnchor.Start, 3);
            labels.Mono(T("序列线 · RNN", "SEQUENCE LINE · RNN"), 1090 - 38, 244, 15, lime, splitA, OriginAnchor.Start, 3);

            // kernel scan
            if (s > 3.6f && s < 9.6f)
            {
                pen.Opacity = 1;
                float kx = (float)(N.ImgX + f.KC * N.ImgCell), ky = (float)(N.ImgY + f.KR * N.ImgCell), cs = (float)N.ImgCell;
                pen.Rect(kx - 1, ky - 1, cs * 3 + 1, cs * 3 + 1, lime, .18f);
                pen.StrokeRect(kx - 1, ky - 1, cs * 3 + 1, cs * 3 + 1, 2, lime, 1, 2.5f);
                pen.Line((float)N.ImgX + (f.KC + 3) * cs, ky, (float)(N.FmX + f.KC * N.FmCell), (float)(N.FmY + f.KR * N.FmCell), 1, lime, .4f);
                pen.Line((float)N.ImgX + (f.KC + 3) * cs, ky + 3 * cs, (float)(N.FmX + f.KC * N.FmCell), (float)(N.FmY + (f.KR + 1) * N.FmCell), 1, lime, .4f);
            }
            pen.Opacity = splitA;
            pen.StrokeRect((float)N.FmX - 1, (float)N.FmY - 1, (float)(14 * N.FmCell + 1), (float)(14 * N.FmCell + 1), 1, Faint, FaintA);
            for (int i = 0; i < 196; i++)
            {
                if (i > f.Scan) break;
                int r = i / 14, c = i % 14;
                pen.Rect((float)(N.FmX + c * N.FmCell), (float)(N.FmY + r * N.FmCell), (float)N.FmCell - 1.5f, (float)N.FmCell - 1.5f, lime, .08f + (float)N.FeatureMap[i] * .85f);
            }
            labels.Mono(T("输入 16×16", "input 16×16"), (float)N.ImgX, (float)(N.ImgY + 16 * N.ImgCell + 22), 12, dim, splitA);
            labels.Mono(T("特征图 · 3×3 卷积核", "feature map · 3×3 kernel"), (float)N.FmX, (float)(N.FmY + 14 * N.FmCell + 22), 12, dim, splitA);

            // depth bars
            for (int i = 0; i < N.Bars.Length; i++)
            {
                var b = N.Bars[i];
                float g = (float)XgOriginColdOpen.Ramp(s, 6.6 + i * .5, 7.8 + i * .5);
                float h = b.Layers * 8 * g;
                float x = 170 + i * 165;
                float oa = (float)XgOriginColdOpen.Ramp(s, 6.4 + i * .5, 6.9 + i * .5);
                if (oa <= .001f) continue;
                pen.Opacity = oa;
                if (h > .01f) { pen.Rect(x, 770 - h, 80, h, lime, .14f + i * .08f);
                pen.StrokeRect(x, 770 - h, 80, h, 1.2f, lime); }
                int shown = Mathf.FloorToInt(b.Layers * g + .5f);
                labels.Mono(T(shown + " 层", shown + " layers"), x + 40, 758 - h, 16, lime, oa, OriginAnchor.Middle);
                labels.Mono(b.Name, x + 40, 790, 13, ink, oa, OriginAnchor.Middle);
                labels.Mono(b.Year, x + 40, 810, 11, dim, oa, OriginAnchor.Middle);
            }
            string[] visionChips = { T("验证码 ✓", "captcha ✓"), T("表情包 ✓", "emoji ✓") };
            string[] seqChips = { T("写诗 ✓", "poems ✓"), T("写新闻 ✓", "news ✓") };
            for (int i = 0; i < 2; i++)
            {
                float a = (float)XgOriginColdOpen.Ramp(s, 13.0 + i * .3, 13.6 + i * .3);
                if (a <= .001f) continue;
                pen.Opacity = a;
                pen.Rect(560 + i * 124, 548, 112, 34, new Color32(14, 22, 17, 255), .8f);
                pen.StrokeRect(560 + i * 124, 548, 112, 34, 1, lime, .4f);
                labels.Serif(visionChips[i], 616 + i * 124, 565, 18, lime, a);
            }

            // RNN chain
            float rnnA = (float)f.RnnA;
            if (rnnA > .001f)
            {
                pen.Opacity = rnnA;
                labels.Mono(T("Elman 1990 · 隐状态 h 一格一格往后传", "Elman 1990 · hidden state h, passed on cell by cell"), 1090 - 38, 284, 12, dim, rnnA);
                var toks = OriginLiveContext.English ? N.TokensEn : N.Tokens;
                for (int i = 0; i < toks.Length; i++)
                {
                    float rx = (float)N.RnnX(i), ry = (float)N.RnnY;
                    float lit = Mathf.Max(0, 1 - Mathf.Abs((float)f.Dot - i) * .9f);
                    float seen = f.Dot >= i ? .25f : 0;
                    if (i < toks.Length - 1) pen.Line(rx + 36, ry, (float)N.RnnX(i + 1) - 36, ry, 1, ink, .4f);
                    pen.Line(rx, ry + 80, rx, ry + 36, 1, ink, .3f);
                    pen.Rect(rx - 36, ry - 36, 72, 72, lime, seen + lit * .5f);
                    pen.StrokeRect(rx - 36, ry - 36, 72, 72, 1.4f, lime, 1, lit > .3f ? 2.5f : 0);
                    labels.Mono("h" + (i + 1), rx, ry, 14, ink, rnnA, OriginAnchor.Middle);
                    labels.Serif(toks[i], rx, ry + 104, OriginLiveContext.English ? 25 : 34, ink, rnnA);
                }
                if (s > 10.3f && s < 14.2f)
                    pen.Disc(Mathf.Lerp((float)N.RnnX(0), (float)N.RnnX(toks.Length - 1), (float)f.Dot / (toks.Length - 1)), (float)N.RnnY - 54, 6, lime, 1, 7);
            }
            string[] chips = seqChips;
            for (int i = 0; i < 2; i++)
            {
                float a = (float)XgOriginColdOpen.Ramp(s, 13.2 + i * .3, 13.8 + i * .3);
                if (a <= .001f) continue;
                pen.Opacity = a;
                pen.Rect(1052 + i * 136, 570, 120, 34, new Color32(14, 22, 17, 255), .8f);
                pen.StrokeRect(1052 + i * 136, 570, 120, 34, 1, lime, .4f);
                labels.Serif(chips[i], 1112 + i * 136, 587, 18, lime, a);
            }
            pen.Opacity = 1;
            labels.End(); canvas.End();
            tag.Render(s, Mathf.FloorToInt(s * 30 + .001f), 3, T("走向专长", "Specialising"), T("CNN（LeNet → AlexNet → VGG / GoogLeNet） · RNN", "CNN (LeNet → AlexNet → VGG / GoogLeNet) · RNN"), "1998 / 1990", "2016.08");
            // Its real first word: the track it was fed more (看图 or 读字) spoke first.
            bool readFirst = Ctx.Sim != null && Ctx.Sim.FirstTrack == "sequence";
            speech.Render(s, readFirst ? T("我。读。", "I. Read.") : T("我。看。", "I. See."), 16.4f, 1052, 720, 5, false);
        }
    }
}
