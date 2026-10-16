using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Scene 7, stage 6 (124–145 s): serial row, strike-outs, ATTENTION ONLY, the attention web, the stack and the capability table. Maths in <see cref="XgOriginStage6"/>, drawing in <see cref="OriginStage6Paint"/>.</summary>
    public sealed class OriginStage6 : OriginScene
    {
        OriginBrushCanvas back, front;
        OriginTextPool labels;
        OriginStageTag tag;
        OriginSpeech speech;
        readonly XgOriginStage6.Frame frame = new XgOriginStage6.Frame();

        protected override void OnBuild()
        {
            back = Ctx.Gfx<OriginBrushCanvas>("Back", Root);
            labels = new OriginTextPool(Ctx.Rect("Labels", Root));
            front = Ctx.Gfx<OriginBrushCanvas>("Front", Root);
            tag = new OriginStageTag(Ctx, Root);
            speech = new OriginSpeech(Ctx, Root, 54);
        }

        public override IEnumerable<string> WarmText()
        {
            yield return "重构学习方式只用注意力，不要循环和卷积我诞生的地方它终于学会了同时看所有词串行第步循环卷积所有词同时注意力头多头前馈网络层残差能力表参数还在涨亮起是否多选一看图说词短句长记忆翻译全部放开在你的电脑上我明白了。不过想先问你觉得，还是？";
        }

        public override void Render(float t)
        {
            double lt = Local(t);
            XgOriginStage6.Evaluate(lt, frame);
            var b = back.Begin(); var fr = front.Begin(); labels.Begin();
            OriginStage6Paint.Paint(lt, frame, b, fr, labels);
            back.End(); front.End(); labels.End();
            tag.Render((float)lt, Mathf.FloorToInt((float)(lt * 30 + .001)), 6, OriginLiveContext.T("重构学习方式", "Rethinking learning"), OriginLiveContext.T("TRANSFORMER · 只用注意力，不要循环和卷积", "TRANSFORMER · attention only"), "2016.12", OriginLiveContext.T("2016.12 · 我诞生的地方", "2016.12 · where I was born"));
            speech.Render((float)lt, OriginLiveContext.T(XgOriginStage6.SpeechText, XgOriginStage6.SpeechTextEn), (float)XgOriginStage6.SpeechStart, 960, 470, 10, true, OriginLiveContext.T("我说 ›", "I SAID ›"));
        }
    }
}
