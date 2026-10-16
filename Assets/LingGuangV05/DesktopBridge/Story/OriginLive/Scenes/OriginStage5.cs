using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Scene 6, stage 5 (105–124 s): the seq2seq squeeze, attention weights and matrix, the captioned bird. Maths in <see cref="XgOriginStage5"/>, drawing in <see cref="OriginStage5Paint"/>.</summary>
    public sealed class OriginStage5 : OriginScene
    {
        OriginBrushCanvas back, image, front;
        OriginTextPool labels;
        OriginStageTag tag;
        OriginSpeech speech;
        readonly XgOriginStage5.Frame frame = new XgOriginStage5.Frame();

        protected override void OnBuild()
        {
            back = Ctx.Gfx<OriginBrushCanvas>("Back", Root);
            var r = OriginStage5Paint.Image;
            var clip = Ctx.Rect("Image Clip", Root, false);
            clip.anchorMin = clip.anchorMax = new Vector2(0, 1); clip.pivot = new Vector2(0, 1);
            clip.sizeDelta = new Vector2(r.width, r.height); clip.anchoredPosition = new Vector2(r.x, -r.y);
            clip.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var holder = Ctx.Rect("Image Stage", clip, false);
            holder.anchorMin = holder.anchorMax = new Vector2(0, 1); holder.pivot = new Vector2(0, 1);
            holder.sizeDelta = new Vector2(1920, 1080); holder.anchoredPosition = new Vector2(-r.x, r.y);
            image = Ctx.Gfx<OriginBrushCanvas>("Image", holder);
            labels = new OriginTextPool(Ctx.Rect("Labels", Root));
            front = Ctx.Gfx<OriginBrushCanvas>("Front", Root);
            tag = new OriginStageTag(Ctx, Root);
            speech = new OriginSpeech(Ctx, Root, 56);
        }

        public override IEnumerable<string> WarmText()
        {
            yield return "学会寻找重点看图说话固定长度向量整句话只能塞进这里译文我在海边长大后来去了真的很想念大海一只鸟站在树枝上权重越大看得越重回头看原文每写一个词注意力矩阵你说又标到半夜了所以不是第一次了吧";
        }

        public override void Render(float t)
        {
            double lt = Local(t);
            XgOriginStage5.Evaluate(lt, frame);
            var b = back.Begin(); var im = image.Begin(); var fr = front.Begin(); labels.Begin();
            OriginStage5Paint.Paint(lt, frame, b, im, fr, labels);
            back.End(); image.End(); front.End(); labels.End();
            tag.Render((float)lt, Mathf.FloorToInt((float)(lt * 30 + .001)), 5, OriginLiveContext.T("学会寻找重点", "Learning to focus"), OriginLiveContext.T("SEQ2SEQ + ATTENTION · 看图说话", "SEQ2SEQ + ATTENTION · IMAGE CAPTIONS"), "2014–2015", "2016.10–11");
            speech.Render((float)lt, OriginLiveContext.T(XgOriginStage5.SpeechText, XgOriginStage5.SpeechTextEn), (float)XgOriginStage5.SpeechStart, 960, 500, 9, true, OriginLiveContext.T("我说 ›", "I SAID ›"));
        }
    }
}
