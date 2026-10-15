using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgRun
    {
        /// <summary>
        /// 练过的题: the accuracy on the cards it trained on, one point per round (XgSim.Holdout.cs). Aligned with the tail
        /// of <see cref="histVal"/> (新题) and never longer; null or short in an older save (filled from histTrain).
        /// </summary>
        public List<float> histPractice = new List<float>();
    }

    /// <summary>
    /// 双曲线 (它在学_可视化借鉴设计.md §2.1): 练过的题 against 新题. 新题 is the validation accuracy the lab scores
    /// (<see cref="XgRun.valAcc"/>); 练过的题 is how many of its own practice cards it gets right. The two part when
    /// the model is big for its data: it learns the practice cards by heart. How much it memorises is the share the drop
    /// risk's 背答案 term uses (params per sample on a log scale, Dropout halves it; augmentation counts as more samples),
    /// times how often each practice card has come round (cards trained over samples). Never below the curve's own
    /// training accuracy (<see cref="XgRun.trainAcc"/>), which is the same idea in the scaling law (<see cref="OverfitScale"/>).
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>A gap this wide (accuracy) is 背答案 on the curve card: shaded red, with the note.</summary>
        public const double MemorizingGap = .06;

        /// <summary>
        /// Share of what it cannot do on new cards that it does on its practice cards: 0 at one parameter per sample or
        /// fewer, 1 at a thousand; Dropout halves it. The same share as <see cref="XgDropRisk.overfit"/> / 0.24.
        /// </summary>
        public double MemorizeShare(XgRun run)
        {
            if (run == null || XgCatalog.Dataset(run.dataset) == null) return 0;
            double perSample = ParamsK(run) * 1000 / Math.Max(1, SamplesEffective(run));
            double share = Math.Log10(Math.Max(1, perSample)) / 3;
            return HoldoutFinite(share) ? Math.Max(0, Math.Min(1, share)) * (Has("dropout") ? .5 : 1) : 0;
        }

        /// <summary>练过的题 now: the new-card accuracy plus what it has memorised of the rest, at least the training accuracy.</summary>
        public double PracticeAccuracy(XgRun run)
        {
            var d = run == null ? null : XgCatalog.Dataset(run.dataset);
            if (d == null) return 0;
            double chance = 1 - d.chanceError, top = 1 - d.floorError;
            double val = HoldoutFinite(run.valAcc) ? Math.Max(chance, Math.Min(top, run.valAcc)) : chance;
            // A card is learned by heart only after it has come round a few times: 1 − e^(−passes over the samples).
            double passes = HoldoutFinite(run.steps) ? Math.Max(0, run.steps) / Math.Max(1, SamplesEffective(run)) : 0;
            double settle = run.epoch <= 0 ? 0 : 1 - Math.Exp(-passes);
            double practice = val + (top - val) * MemorizeShare(run) * settle;
            double train = HoldoutFinite(run.trainAcc) ? run.trainAcc : val;
            return Math.Max(val, Math.Min(top, Math.Max(practice, train)));
        }

        /// <summary>练过的题 minus 新题 now (≥ 0).</summary>
        public double HoldoutGap(XgRun run) => run == null ? 0 : Math.Max(0, PracticeAccuracy(run) - (HoldoutFinite(run.valAcc) ? run.valAcc : 0));

        /// <summary>The two lines have parted far enough to say 它在背答案.</summary>
        public bool Memorizing(XgRun run) => run != null && run.epoch > 0 && HoldoutGap(run) >= MemorizingGap;

        /// <summary>The 练过的题 series, aligned point for point with <see cref="XgRun.histVal"/> (新题).</summary>
        public List<float> PracticeSeries(XgRun run)
        {
            if (run == null) return new List<float>();
            SyncHoldout(run, run.histVal == null ? 0 : run.histVal.Count);
            return run.histPractice;
        }

        /// <summary>One round's 练过的题 point, right after the round's 新题 point (TrainEpoch, a loaded model).</summary>
        void PushHoldout(XgRun run)
        {
            int count = run.histVal == null ? 0 : run.histVal.Count;
            if (count == 0) return;
            SyncHoldout(run, count - 1);
            run.histPractice.Add((float)PracticeAccuracy(run));
        }

        /// <summary>
        /// Keeps the series as long as <paramref name="count"/>: the newest points stay when the curve was cleared or
        /// capped; missing old points (an older save) are the curve's own training accuracy of that round.
        /// </summary>
        static void SyncHoldout(XgRun run, int count)
        {
            if (run.histPractice == null) run.histPractice = new List<float>();
            var list = run.histPractice;
            if (list.Count > count) list.RemoveRange(0, list.Count - count);
            if (list.Count >= count) return;
            int missing = count - list.Count;
            var fill = new List<float>(missing);
            for (int i = 0; i < missing; i++)
            {
                float val = run.histVal != null && i < run.histVal.Count ? run.histVal[i] : 0;
                float train = run.histTrain != null && run.histVal != null && run.histTrain.Count == run.histVal.Count && i < run.histTrain.Count ? run.histTrain[i] : val;
                fill.Add(Math.Max(val, train));
            }
            list.InsertRange(0, fill);
        }

        static bool HoldoutFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
