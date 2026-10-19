using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgSim
    {
        public event Action<XgEpoch> EpochStarted;
        public bool LastEpochWasHand { get; private set; } = true;
        public bool AnyEpochActive => S.vision.epochActive || S.sequence.epochActive;
        /// <summary>GPU-seconds a round without a planned pace bills (a round trained directly, an old save's round).</summary>
        public static double DurationFor(XgRun run) => run.arch == "transformer" ? EpochSeconds * .3 : EpochSeconds;

        /// <summary>
        /// Billed GPU-seconds per second of a round's planned length (<see cref="PaceSeconds"/>): the reference round, a tiny
        /// model on one GTX 980 Ti, still bills the old <see cref="EpochSeconds"/>, and a round that takes longer (a bigger
        /// model, more samples) uses the cards longer and bills more (节奏重做, 2026-10-16).
        /// </summary>
        public const double PowerSecondsPerPaceSecond = EpochSeconds / PaceBaseSeconds;

        /// <summary>
        /// GPU-seconds one round bills (electricity, trainedSeconds): its planned length at full speed, scaled by
        /// <see cref="PowerSecondsPerPaceSecond"/>. Stalls do not bill (the card is waiting). A round without a planned
        /// pace bills <see cref="DurationFor"/>.
        /// </summary>
        public double BilledSeconds(XgRun run) =>
            run != null && run.paceSeed != 0 && run.epochDuration > 0 && !double.IsNaN(run.epochDuration) && !double.IsInfinity(run.epochDuration)
                ? run.epochDuration * PowerSecondsPerPaceSecond : DurationFor(run);

        /// <summary>Player/automation entry point. Starting never grants samples, income, or a completed epoch.</summary>
        public bool BeginEpoch(XgTrack track, IXgHost host, bool hand = true)
        {
            if (StagePresentationHeld) return false;
            if (TrainingLessonModalOpen) return false;
            var run = Run(track);
            if (run.epochActive || !TrainingUnlocked(track)) return false;
            EnsureData(run);
            AutoConfigure(run, host);
            string blocked = StartBlocker(run, host);
            if (blocked != null) { if (hand) Say(blocked); return false; }
            run.epochActive = true;
            run.epochHand = hand;
            run.epochProgress = 0;
            // The bar takes real time and moves like a real one (XgSim.TrainPace.cs).
            PlanPace(run, host, PaceSeconds(run, host));
            ReserveTrainingQuizBonus(run);
            EpochStarted?.Invoke(new XgEpoch { track = (int)track, epoch = run.epoch + 1, hand = hand });
            return true;
        }

        void TickEpochs(double dt, IXgHost host)
        {
            if (StagePresentationHeld) return;
            if (TrainingLessonModalOpen) return;
            // Snapshot this slice before either epoch can finish and release its reservation.
            double visionProgress = S.vision.epochActive && Blocker(S.vision, host) == null ? TrainingProgressFactor(S.vision) : 0;
            double sequenceProgress = S.sequence.epochActive && Blocker(S.sequence, host) == null ? TrainingProgressFactor(S.sequence) : 0;
            double visionLearning = TrainingFactor(wiringView, XgTrack.Vision, false);
            double sequenceLearning = TrainingFactor(wiringView, XgTrack.Sequence, false);
            foreach (var run in Runs)
            {
                double progressFactor = (run.track == (int)XgTrack.Vision ? visionProgress : sequenceProgress) * LifeTrainingBoost;
                if (!run.epochActive) continue;
                if (progressFactor <= 0 || Blocker(run, host) != null) { run.paceFactor = 0; continue; }
                AdvancePace(run, dt * progressFactor, progressFactor);
                if (run.epochProgress < 1 - 1e-8) continue;
                bool hand = run.epochHand;
                double learningFactor = run.track == (int)XgTrack.Vision ? visionLearning : sequenceLearning;
                run.epochActive = false;
                run.epochProgress = 0;
                LastEpochWasHand = hand;
                var previousLearning = timedEpochLearning;
                timedEpochLearning = (run.track, learningFactor);
                try { if (TrainEpoch((XgTrack)run.track, host, hand) == null) RefundTrainingQuizBonus(run); }
                finally { timedEpochLearning = previousLearning; }
            }
        }
    }
}
