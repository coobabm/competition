using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgSim
    {
        public event Action<XgEpoch> EpochStarted;
        public bool LastEpochWasHand { get; private set; } = true;
        public bool AnyEpochActive => S.vision.epochActive || S.sequence.epochActive;
        public static double DurationFor(XgRun run) => run.arch == "transformer" ? EpochSeconds * .3 : EpochSeconds;

        /// <summary>Player/automation entry point. Starting never grants samples, income, or a completed epoch.</summary>
        public bool BeginEpoch(XgTrack track, IXgHost host, bool hand = true)
        {
            var run = Run(track);
            if (run.epochActive || !TrainingUnlocked(track)) return false;
            EnsureData(run);
            AutoConfigure(run, host);
            string blocked = StartBlocker(run, host);
            if (blocked != null) { if (hand) Say(blocked); return false; }
            run.epochActive = true;
            run.epochHand = hand;
            run.epochProgress = 0;
            run.epochDuration = DurationFor(run);
            EpochStarted?.Invoke(new XgEpoch { track = (int)track, epoch = run.epoch + 1, hand = hand });
            return true;
        }

        void TickEpochs(double dt, IXgHost host)
        {
            // Snapshot this slice before either epoch can finish and release its reservation.
            double visionProgress = S.vision.epochActive && Blocker(S.vision, host) == null ? TrainingProgressFactor(S.vision) : 0;
            double sequenceProgress = S.sequence.epochActive && Blocker(S.sequence, host) == null ? TrainingProgressFactor(S.sequence) : 0;
            double visionLearning = TrainingFactor(wiringView, XgTrack.Vision, false);
            double sequenceLearning = TrainingFactor(wiringView, XgTrack.Sequence, false);
            foreach (var run in Runs)
            {
                double progressFactor = run.track == (int)XgTrack.Vision ? visionProgress : sequenceProgress;
                if (!run.epochActive || progressFactor <= 0 || Blocker(run, host) != null) continue;
                run.epochProgress = Math.Min(1, run.epochProgress + dt * progressFactor / Math.Max(.01, run.epochDuration));
                if (run.epochProgress < 1 - 1e-8) continue;
                bool hand = run.epochHand;
                double learningFactor = run.track == (int)XgTrack.Vision ? visionLearning : sequenceLearning;
                run.epochActive = false;
                run.epochProgress = 0;
                LastEpochWasHand = hand;
                var previousLearning = timedEpochLearning;
                timedEpochLearning = (run.track, learningFactor);
                try { TrainEpoch((XgTrack)run.track, host, hand); }
                finally { timedEpochLearning = previousLearning; }
            }
        }
    }
}
