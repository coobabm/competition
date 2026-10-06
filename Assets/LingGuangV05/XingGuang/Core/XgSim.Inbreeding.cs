using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgRun
    {
        /// <summary>Validation accuracy taken away by 近亲繁殖 at the last evaluation (0 when the data is clean enough).</summary>
        public double inbreedingPenalty;
    }

    public partial class XgCard
    {
        /// <summary>An old automatic label sent back to the review inbox by 人工复核旧数据.</summary>
        public bool recheck;
    }

    /// <summary>
    /// 近亲繁殖: wrong automatic labels that nobody caught become dataset noise. Up to a tenth of the samples the
    /// model shrugs it off; beyond that every extra share of noise caps validation accuracy (half of the share above
    /// 10%), because it is learning its own mistakes back. Two cures: discard noisy rows (抽检's CleanNoise) or send
    /// old rows back to a human (<see cref="RecheckNoise"/>), where each right hand answer removes one noisy row.
    /// </summary>
    public sealed partial class XgSim
    {
        public const string InbreedingId = "inbreeding";
        public const double InbreedingFreeShare = .1, InbreedingSlope = .5;
        public const int RecheckBatch = 10;

        /// <summary>Noisy rows as a share of the dataset's samples.</summary>
        public double NoiseShare(string dataset)
        {
            if (S.noise == null) return 0;
            double noise = Noise(dataset);
            if (!FiniteCollaboration(noise) || noise <= 0) return 0;
            return noise / Math.Max(1, Samples(dataset));
        }

        /// <summary>How much validation accuracy the noise costs now: 0.5 × the share above 10%.</summary>
        public double InbreedingPenaltyFor(string dataset) => Math.Max(0, NoiseShare(dataset) - InbreedingFreeShare) * InbreedingSlope;

        void ApplyInbreeding(XgRun run)
        {
            if (run == null) return;
            run.inbreedingPenalty = 0;
            double penalty = InbreedingPenaltyFor(run.dataset);
            if (penalty <= 0 || !FiniteCollaboration(run.valAcc)) return;
            if (UseBoard)
            {
                // The board already trains on its own uncaught mistakes (systematic flips in the pool), so the drag is
                // in the score itself: name it, do not take it off twice.
                run.inbreedingPenalty = penalty;
                Observe(InbreedingId);
                return;
            }
            var d = XgCatalog.Dataset(run.dataset);
            // Never below a coin toss on this dataset.
            double floor = Math.Min(run.valAcc, d != null ? 1 - d.chanceError : .5);
            double capped = Math.Max(floor, run.valAcc - penalty);
            if (run.valAcc - capped <= 1e-9) return;
            run.inbreedingPenalty = run.valAcc - capped;
            run.valAcc = capped;
            Observe(InbreedingId);
        }

        void SayInbreeding(XgDataset d, double penalty)
        {
            Say(T("评估：《" + d.name + "》里混进了它自己没被发现的错题（近亲繁殖），准确率被拖低 " + Pct(penalty) + "。",
                "Assessment: " + d.nameEn + " contains its own uncaught mistakes (inbreeding); accuracy is held down by " + Pct(penalty) + "."));
        }

        public static void InbreedingCardText(out int stage, out string name, out string nameEn, out string golden, out string goldenEn, out string why, out string whyEn)
        {
            stage = 2; name = "近亲繁殖"; nameEn = "Inbreeding";
            golden = "人工复核旧数据 · 抽检清理噪声 · 调高「拿不准才问我」"; goldenEn = "Re-check old labels · clean noise with Quality audit · raise 'Ask when unsure'";
            why = "它标错了，没人发现，这些错题又回到训练数据里。它学会的，是它自己的错。";
            whyEn = "It labelled wrong, nobody noticed, and those labels went back into its training data. What it learned was its own mistakes.";
        }

        /// <summary>Re-check cards for this desk already waiting in the review inbox.</summary>
        public int PendingRechecks(string desk) { int n = 0; foreach (var c in S.queue) if (c.recheck && c.dataset == desk) n++; return n; }

        /// <summary>How many noisy rows of this desk can go back to a human now (inbox space, batch of ten).</summary>
        public int RecheckableNoise(string desk)
        {
            if (XgCatalog.Desk(desk) == null || XgCatalog.Dataset(desk) == null) return 0;
            double noise = Noise(desk);
            if (!FiniteCollaboration(noise) || noise < 1) return 0;
            double open = Math.Floor(noise) - PendingRechecks(desk);
            return (int)Math.Max(0, Math.Min(Math.Min(RecheckBatch, ReviewCapacity - S.queue.Count), open));
        }

        /// <summary>
        /// 人工复核旧数据: pushes up to ten noisy rows of a desk into the review inbox as ordinary cards (no model
        /// suggestion). Each right hand answer removes one noisy row; a wrong one leaves it. Returns the count.
        /// </summary>
        public int RecheckNoise(string desk)
        {
            int n = RecheckableNoise(desk);
            for (int i = 0; i < n; i++)
            {
                XgCard card = null;
                for (int tries = 0; tries < 4 && (card == null || card.kind == "shutdown"); tries++) card = CreateLabelCard(desk);
                card.recheck = true; card.hasJudgment = false; card.trapId = "";
                EnqueueReview(card);
            }
            if (n > 0) Say(T("已把 " + n + " 条旧数据放回待复核，答对一条就清掉一条噪声。", n + " old labels are back in the review inbox; each right answer clears one noisy row."));
            return n;
        }

        void RecheckCorrected(string desk)
        {
            double noise = Noise(desk);
            if (noise <= 0) return;
            SetCount(S.noise, desk, Math.Max(0, noise - 1));
            foreach (var run in Runs) if (run.dataset == desk) Evaluate(run);
        }
    }
}
