using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// A round that went back because the model overfit (背答案, <see cref="XgEpoch.overfit"/>) says so out loud: a banner
    /// over the curve, a toast with what fixes it, and the 回档 button lit red for a while so the way back is obvious.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        const float OverfitNudgeSeconds = 20, OverfitToastGap = 20;
        float overfitNudgeUntil = -1, overfitToastAt = -100;

        bool OverfitNudging => Time.unscaledTime < overfitNudgeUntil;

        void OverfitNotice(double points)
        {
            var at = Fx.At(chartArea);
            Fx.Shockwave(at, XgDark.Bad, 200, .45f, 8);
            Fx.Float(at, T("过拟合 · 它在背答案", "Overfitting · it memorised the answers"), XgDark.Bad, 28, 60, 1.4f, 1.1f);
            overfitNudgeUntil = Time.unscaledTime + OverfitNudgeSeconds;
            var checkpoint = BestRollback();
            if (checkpoint != null && !RollbackBusy)
            {
                Fx.Knock(saveModel.rt, .2f);
                Fx.Burst(Fx.At(saveModel.rt), 14, XgDark.Bad, XgJuice.Shape.Spark, 220);
            }
            // The toast explains once in a while; the banner and the red button say it every time.
            if (Time.unscaledTime - overfitToastAt < OverfitToastGap) return;
            overfitToastAt = Time.unscaledTime;
            string fix = Sim.Has("dropout") ? T("多标数据、买数据包", "more labels or data packs") : T("多标数据、买数据包或学 Dropout", "more labels, data packs or Dropout");
            string back = checkpoint != null && !RollbackBusy
                ? T("。想退回去：点右上「回档」回到最好的检查点（" + N(checkpoint.score, "0.0") + " 分）。", ". To go back: 回档 (top right) restores the best checkpoint (" + N(checkpoint.score, "0.0") + ").")
                : T("。", ".");
            view.ShowToast(T("过拟合了：训练题背熟了，新题反而退步 " + N(points < 0 ? -points : points, "0") + " 分。" + fix + "能治" + back,
                "Overfitting: it memorised the training set and lost " + N(points < 0 ? -points : points, "0") + " points on new questions. " + fix + " fix it" + back), 6);
        }
    }
}
