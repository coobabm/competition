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
            string blocked = Blocker(run, host);
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
            foreach (var run in Runs)
            {
                if (!run.epochActive || host == null || host.Blocker != null || host.Compute <= 0 || ProjectActive || VramNeedMB(run) > Vram(host)) continue;
                run.epochProgress = Math.Min(1, run.epochProgress + dt / Math.Max(.01, run.epochDuration));
                if (run.epochProgress < 1 - 1e-8) continue;
                bool hand = run.epochHand;
                run.epochActive = false;
                run.epochProgress = 0;
                LastEpochWasHand = hand;
                TrainEpoch((XgTrack)run.track, host, hand);
            }
        }
    }
}
