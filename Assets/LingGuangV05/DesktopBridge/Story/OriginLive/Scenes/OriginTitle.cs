using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Scene 1, the title card (17–22.5 s): a particle burst, a flash and the big glowing title. Maths: <see cref="XgOriginTitle"/>.</summary>
    public sealed class OriginTitle : OriginScene
    {
        OriginCanvas canvas, flash;
        OriginText title, subtitle;
        readonly XgOriginTitle.Frame frame = new XgOriginTitle.Frame();
        static readonly Color TitleInk = new Color32(245, 255, 226, 255);
        static readonly Color FlashInk = new Color32(244, 255, 224, 255);
        // Title scale: 250 px in Chinese, a narrower word in English.
        const float ZhSize = 250, EnSize = 150;
        /// <summary>SDF dilation for the 250 px Black face (a pixel offset of about 1.5 px at this size).</summary>
        public static float TitleWeight = .015f;

        protected override void OnBuild()
        {
            canvas = Ctx.Canvas("Burst", Root);
            var layers = new[]
            {
                new OriginTextLayer { Rgb = OriginInk.Lime, Alpha = .12f, Dilate = .01f, Softness = 1f },
                new OriginTextLayer { Rgb = OriginInk.Lime, Alpha = .22f, Dilate = 0f, Softness = .4f },
                new OriginTextLayer { Rgb = OriginInk.Lime, Alpha = .6f, Dilate = 0f, Softness = .15f },
            };
            title = Ctx.Text("Title", Root, Ctx.Serif, ZhSize, TitleInk, 40, TextAlignmentOptions.Center, layers);
            title.SetFace(TitleWeight, 0);
            subtitle = Ctx.Text("Subtitle", Root, Ctx.Mono, 20, OriginInk.Dim(), 12, TextAlignmentOptions.Center);
            flash = Ctx.Canvas("Flash", Root);
        }

        public override IEnumerable<string> WarmText() { yield return XgOriginTitle.TitleZh + XgOriginTitle.TitleEn + XgOriginTitle.SubtitleZh + XgOriginTitle.SubtitleEn; }

        public override void Render(float t)
        {
            float s = Local(t);
            XgOriginTitle.Evaluate(s, frame);
            var f = frame;

            var pen = canvas.Begin();
            for (int i = 0; i < XgOriginTitle.ParticleCount; i++)
            {
                var p = XgOriginTitle.ParticleAt(i, s);
                if (!p.Alive) continue;
                pen.Disc((float)p.X, (float)p.Y, (float)p.R, OriginInk.Lime, (float)p.Opacity, 2.5f);
            }
            canvas.End();

            bool en = OriginLiveContext.English;
            string text = en ? XgOriginTitle.TitleEn : XgOriginTitle.TitleZh;
            float size = en ? EnSize : ZhSize;
            title.Set(text);
            float lh = OriginLive.LhSerif * ZhSize;
            float cy = 540 - (lh + 20 + OriginLive.LhMono * 20) * .5f + lh * .5f;
            title.Root.localScale = new Vector3((float)f.Scale * (size / ZhSize), (float)f.Scale * (size / ZhSize), 1);
            title.Place(960 + 20, cy + OriginLive.SerifDy, .5f, 1500, lh);
            float blur = (float)f.Blur;
            title.SetFace(TitleWeight, Mathf.Clamp01(blur / 70f));
            float br = (float)f.Breathe;
            title.SetHaloStrength(0, .12f * (1 + (br - 1) * 1.5f), .01f, 1f);
            title.SetHaloStrength(1, .22f * br, 0f, .4f);
            title.SetHaloStrength(2, .6f * br, 0f, .15f);
            title.Alpha(OriginInk.Fade((float)f.TitleAlpha));

            subtitle.Set(en ? XgOriginTitle.SubtitleEn : XgOriginTitle.SubtitleZh);
            subtitle.Place(960, 540 - (lh + 20 + OriginLive.LhMono * 20) * .5f + lh + 20 + OriginLive.LhMono * 20 * .5f + OriginLive.MonoDy, .5f, 1800, 30);
            subtitle.Alpha(OriginInk.Fade((float)f.SubtitleAlpha));

            var fp = flash.Begin();
            fp.Rect(0, 0, 1920, 1080, FlashInk, (float)f.Flash * .32f);
            flash.End();
        }
    }
}
