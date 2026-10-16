using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Scene 0, the cold open (0–17 s): the neuron field, the counter and architecture timeline, the question and the answer typed back.
    /// The maths is <see cref="XgOriginColdOpen"/>; the captions are the host's. The scene wrapper (fade, drift) is the host's too, so nothing
    /// here fades the whole scene.
    /// </summary>
    public sealed class OriginColdOpen : OriginScene
    {
        RectTransform hud, bar;
        CanvasGroup hudGroup;
        OriginFieldGraphic field;
        OriginBarGraphic barGraphic;
        OriginText hudLabel, hudValue, hudArch, hudYear;
        OriginText[] ticks, qLabel, qText, sayLabel, sayText;
        OriginCaretGraphic[] caret;
        readonly XgOriginColdOpen.Frame frame = new XgOriginColdOpen.Frame();

        static string T(string zh, string en) => OriginLiveContext.T(zh, en);

        protected override void OnBuild()
        {
            var ctx = Ctx;
            field = ctx.Gfx<OriginFieldGraphic>("Field", Root);
            field.Frame = frame;
            var mono = ctx.Mono; var serif = ctx.Serif;

            // HUD: the parameter counter and the architecture timeline
            hud = ctx.Rect("Hud", Root);
            hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
            hudLabel = new OriginText("Label", hud, mono, 14, OriginInk.Dim(), 5, TextAlignmentOptions.Left);
            hudLabel.Set(T("我的参数 · PARAMETERS", "MY PARAMETERS"));
            hudValue = new OriginText("Value", hud, mono, 44, OriginInk.Lime, 0, TextAlignmentOptions.Left, new OriginTextLayer { Rgb = OriginInk.Lime, Alpha = .55f, Dilate = .2f, Softness = 1 });
            hudArch = new OriginText("Arch", hud, mono, 18, OriginInk.Ink, 2, TextAlignmentOptions.Left);
            hudYear = new OriginText("Year", hud, mono, 16, OriginInk.Dim(), 0, TextAlignmentOptions.Left);
            bar = ctx.Rect("Bar", hud, false);
            bar.anchorMin = bar.anchorMax = new Vector2(0, 1); bar.pivot = new Vector2(0, 1); bar.sizeDelta = new Vector2(620, 34); bar.anchoredPosition = new Vector2(84, -179);
            bar.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            barGraphic = ctx.Gfx<OriginBarGraphic>("Rail", bar);
            ticks = new OriginText[XgOriginColdOpen.BarYears.Length];
            for (int i = 0; i < ticks.Length; i++)
            {
                int y = XgOriginColdOpen.BarYears[i];
                ticks[i] = new OriginText("Tick " + y, bar, mono, 10, OriginInk.Dim(), 0, TextAlignmentOptions.Center);
                ticks[i].Set(y.ToString());
                ticks[i].Place((y - 1958) / 58f * 600, 25.5f, .5f, 60, 14);
            }

            // dialogue: the question above, the answer typed below
            int n = XgOriginColdOpen.Phases.Length;
            qLabel = new OriginText[n]; qText = new OriginText[n]; sayLabel = new OriginText[n]; sayText = new OriginText[n]; caret = new OriginCaretGraphic[n];
            var soft = new OriginTextLayer { Rgb = new Color32(230, 255, 200, 255), Alpha = .3f, Dilate = .1f, Softness = 1 };
            var glow = new OriginTextLayer { Rgb = OriginInk.Lime, Alpha = .6f, Dilate = .25f, Softness = 1, HighlightsOnly = true };
            for (int i = 0; i < n; i++)
            {
                var p = XgOriginColdOpen.Phases[i];
                qLabel[i] = new OriginText("Ask " + i, Root, mono, 14, OriginInk.Dim(), 5, TextAlignmentOptions.Left);
                qText[i] = new OriginText("Question " + i, Root, serif, 30, OriginInk.Dim(), 2, TextAlignmentOptions.Left);
                sayLabel[i] = new OriginText("Say " + i, Root, mono, 15, OriginInk.Dim(), 5, TextAlignmentOptions.Left);
                sayText[i] = new OriginText("Answer " + i, Root, serif, (float)p.Size, OriginInk.Ink, 2, TextAlignmentOptions.Left, soft, glow);
                caret[i] = ctx.Gfx<OriginCaretGraphic>("Caret " + i, Root);
                var crt = caret[i].rectTransform;
                crt.anchorMin = crt.anchorMax = new Vector2(0, 1); crt.pivot = new Vector2(0, .5f); crt.sizeDelta = new Vector2(3, (float)p.Size * .86f);
                qLabel[i].Set(T("你问 ›", "YOU ASKED ›")); sayLabel[i].Set(T("那时我说 ›", "BACK THEN I SAID ›"));
            }
        }

        public override IEnumerable<string> WarmText()
        {
            foreach (var p in XgOriginColdOpen.Phases) { yield return p.Question; yield return p.QuestionEn; yield return p.Answer; yield return p.AnswerEn; }
            yield return "你问 ›YOU ASKED ›那时我说 ›BACK THEN I SAID ›我的参数 · PARAMETERS MY PARAMETERS";
        }

        public override void Render(float t)
        {
            XgOriginColdOpen.Evaluate(t, frame, Ctx.FinalParams, OriginLiveContext.English);
            var f = frame;
            field.Frame = f; field.canvasRenderer.SetAlpha(OriginInk.Fade(Mathf.Clamp01((float)f.FieldAlpha))); field.SetVerticesDirty();
            RenderHud(t, f);
            RenderDialogue(t, f);
        }

        void RenderHud(float t, XgOriginColdOpen.Frame f)
        {
            float MonoDy = OriginLive.MonoDy;
            hudGroup.alpha = OriginInk.Fade((float)f.HudAlpha);
            hudLabel.Place(84, 72.5f + MonoDy, 0, 400, 20);
            double spin = f.Spin;
            hudValue.SetColor(spin > .6 ? OriginInk.Hot : OriginInk.Lime);
            hudValue.Set(f.ParamText);
            var HudValue = OriginLive.HudValue;
            hudValue.SetHaloStrength(0, HudValue.Alpha * (float)(.55 + .4 * spin) / .55f, HudValue.Dilate, Mathf.Clamp01(HudValue.Soft * (16 + 30 * (float)spin) / 16f));
            hudValue.Place(84 + (float)(System.Math.Sin(t * 97) * 3 * spin), 117 + MonoDy, 0, 600, 60);
            hudArch.Set(f.Arch);
            hudArch.Place(84, 161 + MonoDy, 0, 220, 26);
            hudYear.Set(f.Year.ToString());
            hudYear.Place(84 + 220 + 18, 162 + MonoDy, 0, 120, 22);
            barGraphic.Progress = f.BarProgress; barGraphic.Year = f.Year; barGraphic.SetVerticesDirty();
        }

        void RenderDialogue(float t, XgOriginColdOpen.Frame f)
        {
            float MonoDy = OriginLive.MonoDy, SerifDy = OriginLive.SerifDy, LhSerif = OriginLive.LhSerif, LhMono = OriginLive.LhMono;
            var SaySoft = OriginLive.SaySoft; var SayLime = OriginLive.SayLime;
            for (int i = 0; i < XgOriginColdOpen.Phases.Length; i++)
            {
                var p = XgOriginColdOpen.Phases[i];
                // The question, centred as one line: the label, 18 px, the sentence.
                float qa = (float)f.QuestionAlpha[i];
                if (qa > .001f)
                {
                    string q = OriginLiveContext.Pick(p.Question, p.QuestionEn);
                    qText[i].Set(q); qLabel[i].Set(T("你问 ›", "YOU ASKED ›"));
                    float wl = qLabel[i].Width(T("你问 ›", "YOU ASKED ›")), wq = qText[i].Width(q);
                    float x0 = 960 - (wl + 18 + wq) * .5f;
                    float cy = 380 + LhSerif * 30 * .5f;
                    qLabel[i].Place(x0, cy + 4.5f + MonoDy, 0, wl + 20, 24);
                    qText[i].Place(x0 + wl + 18, cy + SerifDy, 0, wq + 20, 50);
                }
                qLabel[i].Alpha(OriginInk.Fade(qa)); qText[i].Alpha(OriginInk.Fade(qa));

                float sa = (float)f.SpeechAlpha[i];
                if (sa > .001f)
                {
                    string a = OriginLiveContext.Pick(p.Answer, p.AnswerEn);
                    float size = (float)p.Size;
                    sayText[i].Alpha(OriginInk.Fade(sa));
                    sayText[i].SetFace(OriginLive.SerifWeight, 0);
                    sayText[i].Set(a, f.SpeechChars[i]);
                    sayText[i].SetHaloStrength(0, SaySoft.Alpha, SaySoft.Dilate, SaySoft.Soft);
                    sayText[i].SetHaloStrength(1, SayLime.Alpha, SayLime.Dilate, SayLime.Soft);
                    float full = sayText[i].Width(a) + 14;
                    float left = 960 - full * .5f;
                    float lhL = LhMono * 15, lhT = LhSerif * size;
                    float top = 540 - (lhL + 14 + lhT) * .5f;
                    sayLabel[i].Place(left, top + lhL * .5f + MonoDy, 0, 400, 24);
                    float textCy = top + lhL + 14 + lhT * .5f;
                    sayText[i].Place(left, textCy + SerifDy, 0, full + 40, lhT);
                    float cx = left + sayText[i].CaretX(f.SpeechChars[i], a) + 8;
                    var crt = caret[i].rectTransform;
                    crt.anchoredPosition = new Vector2(cx, -(textCy + size * .03f));
                    caret[i].Alpha = f.SpeechCursor[i] ? .9f * sa : 0;
                    caret[i].SetVerticesDirty();
                    caret[i].gameObject.SetActive(true);
                }
                else caret[i].gameObject.SetActive(false);
                sayLabel[i].Alpha(OriginInk.Fade(sa)); sayText[i].Alpha(OriginInk.Fade(sa));
            }
        }
    }
}
