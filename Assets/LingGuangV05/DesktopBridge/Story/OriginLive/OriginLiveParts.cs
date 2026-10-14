using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The film's stage header (lib.tsx StageTag): "STAGE 0n / 06", the circled number and title, the technique line, an optional amber
    /// "在你这儿 · 2016.06" line on the left; the big year and six progress bars on the right. Call <see cref="Render"/> every frame with the
    /// scene's LOCAL time (seconds since the scene started) and local frame number.
    /// </summary>
    public sealed class OriginStageTag
    {
        readonly OriginText kicker, title, tech, mine, year;
        readonly OriginCanvas bars;
        readonly RectTransform root;
        readonly CanvasGroup group;
        const string Circled = "①②③④⑤⑥";

        public OriginStageTag(OriginLiveContext ctx, RectTransform parent)
        {
            root = ctx.Rect("Stage Tag", parent);
            group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            kicker = ctx.Text("Kicker", root, ctx.Mono, 15, OriginInk.Dim(), 5, TextAlignmentOptions.Left);
            title = ctx.Text("Title", root, ctx.Serif, 40, OriginInk.Ink, 4, TextAlignmentOptions.Left);
            title.SetFace(OriginLive.SerifWeight, 0);
            tech = ctx.Text("Tech", root, ctx.Mono, 16, OriginInk.Lime, 2, TextAlignmentOptions.Left, new OriginTextLayer { Rgb = OriginInk.Lime, Alpha = .22f, Dilate = 0f, Softness = .5f });
            mine = ctx.Text("Mine", root, ctx.Mono, 14, OriginInk.Amber, 3, TextAlignmentOptions.Left);
            year = ctx.Text("Year", root, ctx.Mono, 52, OriginInk.Lime, 2, TextAlignmentOptions.Right, new OriginTextLayer { Rgb = OriginInk.Lime, Alpha = .3f, Dilate = 0f, Softness = .4f });
            tech.SetHaloStrength(0, .22f, 0f, .5f);
            year.SetHaloStrength(0, .3f, 0f, .4f);
            bars = ctx.Canvas("Stage Bars", root);
        }

        /// <summary>Shrinks a left-anchored text about its left edge so it stays clear of the parameter HUD (which starts at x = 700).</summary>
        static void Fit(OriginText text, float maxWidth)
        {
            float w = text.Main.preferredWidth;
            float k = w > maxWidth ? maxWidth / w : 1;
            text.Root.localScale = new Vector3(k, k, 1);
        }

        /// <param name="n">Stage number 1–6.</param>
        /// <param name="mineText">The amber line ("2016.06"); null for none.</param>
        public void Render(float t, int frame, int n, string titleText, string techText, string yearText, string mineText = null)
        {
            float a = (float)XgOriginColdOpen.Ramp(t, .1, .9);
            float dx = (1 - a) * -16;
            group.alpha = OriginInk.Fade(a);
            float md = OriginLive.MonoDy, sd = OriginLive.SerifDy;
            kicker.Set("STAGE " + n.ToString("00") + " / 06");
            kicker.Place(84 + dx, 62 + 15 * OriginLive.LhMono * .5f + md, 0, 500, 22);
            title.Set(Circled[n - 1] + " " + titleText);
            title.Place(84 + dx, 62 + 15 * OriginLive.LhMono + 10 + OriginLive.LhSerif * 40 * .5f + sd, 0, 1200, 60);
            Fit(title, 600);
            tech.Set(techText);
            float techTop = 62 + 15 * OriginLive.LhMono + 10 + OriginLive.LhSerif * 40 + 10;
            tech.Place(84 + dx, techTop + 16 * OriginLive.LhMono * .5f + md, 0, 1400, 24);
            Fit(tech, 600);
            bool hasMine = !string.IsNullOrEmpty(mineText);
            if (hasMine)
            {
                mine.Set(OriginLiveContext.T("在你这儿 · ", "ON YOUR SIDE · ") + mineText);
                mine.Place(84 + dx, techTop + 16 * OriginLive.LhMono + 8 + 14 * OriginLive.LhMono * .5f + md, 0, 800, 22);
                mine.Alpha(OriginInk.Fade((float)XgOriginColdOpen.Ramp(t, .6, 1.4)));
            }
            else mine.Alpha(0);

            string yr = yearText;
            if (t < .8) yr = XgOriginColdOpen.Scramble(yearText, XgOriginColdOpen.Ramp(t, .15, .8, XgOriginColdOpen.Linear), "yr" + frame);
            year.Set(yr);
            year.Place(1836 + 2, 56 + 52 * OriginLive.LhMono * .5f + md, 1, 700, 80);

            var pen = bars.Begin();
            float by = 56 + 52 * OriginLive.LhMono + 12, bx = 1836 - (6 * 34 + 5 * 6);
            for (int i = 1; i <= 6; i++)
            {
                float x = bx + (i - 1) * 40;
                if (i < n) pen.Rect(x, by, 34, 3, OriginInk.Lime, .45f);
                else if (i == n) pen.Rect(x, by, 34, 3, OriginInk.Lime, 1, 3);
                else pen.Rect(x, by, 34, 3, Color.white, .12f);
            }
            bars.End();
        }
    }

    /// <summary>
    /// The film's typed "那时我说 ›" speech (lib.tsx Speech): a dim label over a big serif line typed at cps characters per second with a blinking
    /// lime caret. One instance per speech line; <see cref="Render"/> with the scene's local time. Markup: [lime] and {red}.
    /// </summary>
    public sealed class OriginSpeech
    {
        readonly OriginText label, say;
        readonly OriginCaretGraphic caret;
        readonly float size;

        public OriginSpeech(OriginLiveContext ctx, RectTransform parent, float size)
        {
            this.size = size;
            var soft = new OriginTextLayer { Rgb = new Color32(230, 255, 200, 255), Alpha = .3f, Dilate = .1f, Softness = 1 };
            var glow = new OriginTextLayer { Rgb = OriginInk.Lime, Alpha = .6f, Dilate = .25f, Softness = 1, HighlightsOnly = true };
            label = ctx.Text("Say Label", parent, ctx.Mono, 15, OriginInk.Dim(), 5, TextAlignmentOptions.Left);
            say = ctx.Text("Say", parent, ctx.Serif, size, OriginInk.Ink, 2, TextAlignmentOptions.Left, soft, glow);
            caret = ctx.Gfx<OriginCaretGraphic>("Say Caret", parent);
            var crt = caret.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0, 1); crt.pivot = new Vector2(0, .5f); crt.sizeDelta = new Vector2(3, size * .86f);
        }

        /// <param name="x">Centre x (center = true) or left edge.</param>
        /// <param name="y">Vertical centre of the whole label + line block.</param>
        public void Render(float t, string text, float start, float x, float y, float cps, bool center = true, string label_ = null, float end = 999)
        {
            if (t < start - .35f) { Hide(); return; }
            float a = Mathf.Min((float)XgOriginColdOpen.Ramp(t, start - .35, start), 1 - (float)XgOriginColdOpen.Ramp(t, end - .4, end));
            int total = XgOriginColdOpen.PlainLength(text);
            int n = Mathf.Max(0, Mathf.Min(total, Mathf.FloorToInt((t - start) * cps)));
            bool cursor = Mathf.FloorToInt((t - start) * cps) < total || Mathf.FloorToInt(t * 2.4f) % 2 == 0;
            string lab = label_ ?? OriginLiveContext.T("那时我说 ›", "BACK THEN I SAID ›");
            if (a <= .001f) { Hide(); return; }
            float md = OriginLive.MonoDy, sd = OriginLive.SerifDy;
            label.Set(lab);
            say.SetFace(OriginLive.SerifWeight, 0);
            say.Set(text, n);
            say.SetHaloStrength(0, OriginLive.SaySoft.Alpha, OriginLive.SaySoft.Dilate, OriginLive.SaySoft.Soft);
            say.SetHaloStrength(1, OriginLive.SayLime.Alpha, OriginLive.SayLime.Dilate, OriginLive.SayLime.Soft);
            float full = say.Width(text) + 14;
            float wl = label.Width(lab);
            float boxW = Mathf.Max(full, wl);
            float left = center ? x - boxW * .5f : x;
            float lhL = OriginLive.LhMono * 15, lhT = OriginLive.LhSerif * size;
            float top = y - (lhL + 14 + lhT) * .5f;
            label.Place(left, top + lhL * .5f + md, 0, 600, 24);
            float textCy = top + lhL + 14 + lhT * .5f;
            say.Place(left, textCy + sd, 0, full + 40, lhT);
            say.Alpha(OriginInk.Fade(a));
            float cx = left + say.CaretX(n, text) + 8;
            var crt = caret.rectTransform;
            crt.anchoredPosition = new Vector2(cx, -(textCy + size * .03f));
            caret.Alpha = cursor ? .9f * a : 0;
            caret.SetVerticesDirty();
            caret.gameObject.SetActive(true);
            float fa = OriginInk.Fade(a);
            label.Alpha(fa); say.Alpha(fa);
        }

        void Hide() { label.Alpha(0); say.Alpha(0); caret.gameObject.SetActive(false); }
    }
}
