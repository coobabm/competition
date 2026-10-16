using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Scene 9, the finale (145–161 s): capability and loss curves, the collapse, the burst, the title and embers. Maths in <see cref="XgOriginFinale"/>, drawing in <see cref="OriginFinalePaint"/>.</summary>
    public sealed class OriginFinale : OriginScene
    {
        OriginBrushCanvas back, front;
        OriginTextPool labels;
        readonly XgOriginFinale.Frame frame = new XgOriginFinale.Frame();

        protected override void OnBuild()
        {
            back = Ctx.Gfx<OriginBrushCanvas>("Back", Root);
            labels = new OriginTextPool(Ctx.Rect("Labels", Root));
            front = Ctx.Gfx<OriginBrushCanvas>("Front", Root);
        }

        public override IEnumerable<string> WarmText()
        {
            yield return XgOriginFinale.TitleZh + "能力参数量对数刻度感知机多层注意力个参数在你这儿月万亿是。否。不确定我。看。读。你今天还没跟说话？你说又？你觉得呢？最后一页留给";
        }

        public override void Render(float t)
        {
            double lt = Local(t);
            XgOriginFinale.Evaluate(lt, frame);
            frame.FinalParams = Ctx.FinalParams;
            frame.SequenceFirst = Ctx.Sim != null && Ctx.Sim.FirstTrack == "sequence";
            var b = back.Begin(); var fr = front.Begin(); labels.Begin();
            OriginFinalePaint.Paint(lt, frame, b, fr, labels);
            back.End(); front.End(); labels.End();
        }
    }
}
