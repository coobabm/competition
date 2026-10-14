using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Scene 5, stage 4 (77.5–105 s): the vanishing signal, the degradation plot, the LSTM conveyor and ResNet shortcuts. Maths in <see cref="XgOriginStage4"/>, drawing in <see cref="OriginStage4Paint"/>.</summary>
    public sealed class OriginStage4 : OriginScene
    {
        OriginBrushCanvas back, front;
        OriginTextPool labels;
        OriginStageTag tag;
        OriginSpeech speech;
        readonly XgOriginStage4.Frame frame = new XgOriginStage4.Frame();

        protected override void OnBuild()
        {
            back = Ctx.Gfx<OriginBrushCanvas>("Back", Root);
            labels = new OriginTextPool(Ctx.Rect("Labels", Root));
            front = Ctx.Gfx<OriginBrushCanvas>("Front", Root);
            tag = new OriginStageTag(Ctx, Root);
            speech = new OriginSpeech(Ctx, Root, 58);
        }

        public override IEnumerable<string> WarmText()
        {
            yield return new string(XgOriginStage4.Sentence) + "学会保留信息训练误差轮数层更差细胞状态记忆的传送带门遗忘输入输出该留的留，该忘的忘简化为两扇ResNet残差捷径输出错误率海那天我说你今天还没跟话。过想了";
        }

        public override void Render(float t)
        {
            double lt = Local(t);
            XgOriginStage4.Evaluate(lt, frame);
            var b = back.Begin(); var fr = front.Begin(); labels.Begin();
            OriginStage4Paint.Paint(lt, frame, b, fr, labels);
            back.End(); front.End(); labels.End();
            tag.Render((float)lt, Mathf.FloorToInt((float)(lt * 30 + .001)), 4, OriginLiveContext.T("学会保留信息", "Learning to remember"), "LSTM / GRU · ResNet", "1997 / 2015", "2016.09");
            speech.Render((float)lt, OriginLiveContext.T(XgOriginStage4.SpeechText, XgOriginStage4.SpeechTextEn), (float)XgOriginStage4.SpeechStart, 960, 250, 8, true, OriginLiveContext.T("那天我说 ›", "THAT DAY I SAID ›"));
        }
    }
}
