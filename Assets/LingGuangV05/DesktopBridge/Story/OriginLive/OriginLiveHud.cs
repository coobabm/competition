using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The film's ParamHud (src/ParamHud.tsx), drawn over every scene from 22.5 s to 145 s: the parameter counter, six capability boxes, the
    /// loss / capability sparklines and the screen-wide pulse at each breakthrough. The numbers come from <see cref="XgOriginFilm.EvaluateHud"/>.
    /// </summary>
    public sealed class OriginParamHud
    {
        const float W = 520, X0 = 700, SH = 40;
        static readonly Color Lav = new Color32(185, 195, 255, 255);

        readonly RectTransform root;
        readonly CanvasGroup group;
        readonly RawImage pulseImage;
        readonly OriginCanvas canvas;
        readonly OriginLabels labels;
        readonly XgOriginFilm.HudFrame frame = new XgOriginFilm.HudFrame();
        readonly Vector2[] loss = new Vector2[XgOriginFilm.SparkPoints], cap = new Vector2[XgOriginFilm.SparkPoints];
        static Texture2D pulseTexture;

        public OriginParamHud(OriginLiveContext ctx, RectTransform parent)
        {
            root = ctx.Rect("Param Hud", parent);
            group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            pulseImage = ctx.Raw("Pulse", root, PulseTexture());
            canvas = ctx.Canvas("Hud Canvas", root);
            labels = ctx.Labels("Hud Labels", root);
        }

        /// <summary>radial-gradient(ellipse at 50% 45%, rgba(212,245,90,0) 30%, rgba(212,245,90,.22) 100%), farthest-corner, at quarter size.</summary>
        static Texture2D PulseTexture()
        {
            if (pulseTexture != null) return pulseTexture;
            const int w = 480, h = 270;
            pulseTexture = new Texture2D(w, h, TextureFormat.RGBA32, false, false) { name = "OriginPulse", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double sx = (x + .5) / w * 1920, sy = (1 - (y + .5) / h) * 1080;
                    double dx = (sx - 960) / 1357.6, dy = (sy - 486) / 840.0;
                    double k = System.Math.Sqrt(dx * dx + dy * dy);
                    float alpha = .22f * (float)System.Math.Max(0, System.Math.Min(1, (k - .3) / .7));
                    var c = OriginInk.Over(OriginInk.Lime, alpha);
                    px[y * w + x] = new Color(c.r, c.g, c.b, c.a);
                }
            pulseTexture.SetPixels32(px); pulseTexture.Apply(false, true);
            return pulseTexture;
        }

        static string T(string zh, string en) => OriginLiveContext.T(zh, en);

        public void Render(float t, double finalParams)
        {
            XgOriginFilm.EvaluateHud(t, frame, finalParams);
            var f = frame;
            if (f.Alpha <= .0005)
            {
                if (root.gameObject.activeSelf) root.gameObject.SetActive(false);
                return;
            }
            if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);
            group.alpha = OriginInk.Fade((float)f.Alpha);
            float pulse = (float)f.Pulse;
            pulseImage.canvasRenderer.SetAlpha(OriginInk.Fade(pulse * pulse));
            pulseImage.enabled = pulse > .002f;

            var pen = canvas.Begin();
            labels.Begin();
            var dim = OriginInk.Dim();
            float md = OriginLive.MonoDy;

            // header row (CSS flex, space-between): 12 px, letter-spacing 4
            const float row1 = 46 + 12 * 1.17f * .5f;
            labels.Mono(T("我的参数 · PARAMETERS", "MY PARAMETERS"), X0, row1, 12, dim, 1, OriginAnchor.Start, 4);
            labels.Mono(T("能力 · ", "ABILITIES · ") + f.Capabilities + "/6", X0 + W + 4, row1, 12, dim, 1, OriginAnchor.End, 4);

            // counter row: 34 px value, six boxes
            float rowTop = 46 + 12 * 1.17f + 6, valueH = 34 * 1.17f, cy = rowTop + valueH * .5f;
            labels.Mono(f.Text, X0, cy, 34, OriginInk.Ink, 1, OriginAnchor.Start, 1, new OriginGlow(OriginInk.Lime, .12f, 0f, .4f));
            float boxX = X0 + W - (6 * 24 + 5 * 5);
            for (int i = 0; i < 6; i++)
            {
                float on = (float)f.BreakOn[i], fresh = (float)f.BreakFresh[i];
                float bx = boxX + i * 29, by = cy - 7;
                float s = 1 + .5f * fresh;
                pen.Push(bx + 12, cy, s);
                if (on > 0) pen.Rect(-12, -7, 24, 14, OriginInk.Lime, .15f + .85f * on, 3f + 10f * fresh);
                pen.StrokeRect(-11.5f, -6.5f, 23, 13, 1, OriginInk.Lime, .4f);
                pen.Pop();
            }

            // sparklines
            float sy = rowTop + valueH + 6;
            for (int k = 0; k < f.Points; k++)
            {
                float x = X0 + k / 160f * W;
                loss[k] = new Vector2(x, sy + SH - (float)f.Loss[k] * SH);
                cap[k] = new Vector2(x, sy + SH - (float)f.Cap[k] * SH);
            }
            pen.Line(X0, sy + SH, X0 + W, sy + SH, 1, Color.white, .12f);
            pen.Polyline(loss, f.Points, 1.4f, Lav, .75f);
            pen.Polyline(cap, f.Points, 2f, OriginInk.Lime, 1, false, 2f);
            float head = X0 + (float)f.Head * W;
            pen.Line(head, sy, head, sy + SH, 1, Color.white, .35f);
            labels.Mono(T("— loss 平滑地降", "— loss falls smoothly"), X0, sy + SH + 13 - 3.5f, 10, Lav, .8f, OriginAnchor.Start, 0);
            labels.Mono(T("能力 突然亮起 —", "ability lights up at once —"), X0 + W, sy + SH + 13 - 3.5f, 10, OriginInk.Lime, 1, OriginAnchor.End, 0);

            if (pulse > .002f)
            {
                float q = 1 - pulse;
                labels.Mono(T("▲ 质变", "▲ LEAP"), X0 + W + 156 + q * 30, 46 + 62 + 15 * 1.17f * .5f - q * 14, 15, OriginInk.Amber, pulse, OriginAnchor.End, 3, new OriginGlow(OriginInk.Amber, .6f, .2f, .5f));
            }
            labels.End();
            canvas.End();
        }
    }
}
