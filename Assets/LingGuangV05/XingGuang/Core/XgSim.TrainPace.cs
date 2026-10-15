using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Optional host capability: the cards are hot enough to clock themselves down. The desktop host maps it onto the
    /// household temperature (ChapterOneSim: above Config.throttleTemperature); a host without it is never hot.
    /// </summary>
    public interface IXgThermal
    {
        bool Throttling { get; }
    }

    /// <summary>Where a training round is: 加载数据 → 训练 → 验证.</summary>
    public enum XgPacePhase { None = 0, Load = 1, Train = 2, Validate = 3 }

    /// <summary>Why the bar is (nearly) standing still right now.</summary>
    public enum XgPaceStall { None = 0, Throttle = 1, Disk = 2, SharedCard = 3, Swap = 4 }

    /// <summary>What the training page's progress bar shows for one line (see <see cref="XgSim.Pace"/>).</summary>
    public sealed class XgPace
    {
        public bool active;
        /// <summary>0–1, the same value as <see cref="XgRun.epochProgress"/>.</summary>
        public double progress;
        public XgPacePhase phase;
        public string phaseLabel = "";
        /// <summary>Progress rate this moment over the phase's average (1 = on pace, 0 = standing still), after the card's share.</summary>
        public double speed;
        public XgPaceStall stall;
        /// <summary>The stall's reason, or "" when the round is moving.</summary>
        public string stallReason = "";
        /// <summary>Wall seconds the lab guesses are left: wobbles like a real ETA, never negative, 0 at the end.</summary>
        public double eta;
        /// <summary>The round's planned length at full speed (<see cref="XgRun.epochDuration"/>).</summary>
        public double nominal;
    }

    /// <summary>
    /// 进度条仿真: a round takes real time that grows with the model and the samples it reads and shrinks with the cards
    /// that train it, and the bar moves like a real one: bursty while the data loads, steady while it trains, a pause on
    /// validation and then the jump to 100%, with now and then a stall that has a period-correct cause. Everything is a
    /// pure function of the round's seed and the round clock, so the same save and the same inputs give the same bar.
    /// The effect of the round (TrainEpoch) still lands when the bar reaches 1 (XgSim.EpochAction.cs).
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Seconds of a tiny model on one GTX 980 Ti (compute 1) at the reference sample count.</summary>
        public const double PaceBaseSeconds = 1.4;
        /// <summary>Extra length per decade of parameters: ×(1 + this · log10(1 + K params)). 2K ≈ ×1.5, 2M ≈ ×4.3.</summary>
        public const double PaceParamsWeight = 1.0;
        /// <summary>Cards one round reads at which the data term is ×1 (CardsPerEpoch's base).</summary>
        public const double PaceReferenceCards = 24;
        /// <summary>The data term is (cards / reference)^this: reading more samples takes longer, much less than linearly.</summary>
        public const double PaceCardsExponent = .15;
        /// <summary>Compute at which the compute term is ×1 (the starting GTX 980 Ti).</summary>
        public const double PaceReferenceCompute = 1;
        /// <summary>Time ∝ compute^−this: more or faster cards help, data parallelism has its overheads.</summary>
        public const double PaceComputeExponent = .65;
        /// <summary>Compute floor for the formula (a 750 Ti is .25).</summary>
        public const double PaceMinCompute = .2;
        /// <summary>Transformer: every word at once instead of one after another, the same idea as <see cref="DurationFor"/>'s ×0.3.</summary>
        public const double PaceTransformerFactor = .5;
        /// <summary>A round is never shorter / longer than this at full speed.</summary>
        public const double PaceMinSeconds = 1, PaceMaxSeconds = 11;

        /// <summary>Progress at which loading ends and training starts, and at which validation starts.</summary>
        public const double PaceTrainAt = .12, PaceValidateAt = .92;
        /// <summary>Shares of the nominal time: loading, training and validation (they add up to 1).</summary>
        public const double PaceLoadShare = .18, PaceTrainShare = .72, PaceValidateShare = .10;
        /// <summary>The speed changes this often (round clock seconds) while training, and while loading.</summary>
        public const double PaceBucketSeconds = .25, PaceLoadBucketSeconds = .12;
        /// <summary>The whole round lands between these multiples of its nominal time (stalls included).</summary>
        public const double PaceMinFactor = .7, PaceMaxFactor = 1.5;
        /// <summary>Training reaches validation by this multiple at the latest, so validation still gets a moment.</summary>
        public const double PaceTrainDeadline = 1.45;
        /// <summary>Speed during a stall, as a share of the phase's own speed (not 0: the bar creeps, as real ones do).</summary>
        public const double PaceStallSpeed = .04;
        /// <summary>Chances per round: 显卡降频 (only when hot), 硬盘读得慢, 内存不够换页.</summary>
        public const double PaceThrottleChance = .5, PaceDiskChance = .12, PaceSwapChance = .03;
        /// <summary>The ETA's smoothing time (seconds): short enough to wobble with the bursts.</summary>
        public const double PaceEtaSmoothing = .8;

        /// <summary>
        /// Planned length of a round at full speed (wall seconds before stalls and jitter):
        /// base × (1 + w·log10(1 + K params)) × (cards / 24)^0.15 / arch speed × (Transformer ×0.5) / (compute)^0.65,
        /// where compute is the host's compute on the cards wired to this line. Power bills this length (<see cref="BilledSeconds"/>).
        /// </summary>
        public double PaceSeconds(XgRun run, IXgHost host)
        {
            if (run == null) return EpochSeconds;
            double share = TrainingFactor(wiringView, (XgTrack)run.track, false);
            if (!(share > 0)) share = 1;
            double compute = host == null ? PaceReferenceCompute : Math.Max(0, host.Compute) * share;
            if (!PaceFinite(compute)) compute = PaceReferenceCompute;
            compute = Math.Max(PaceMinCompute, compute);
            double size = 1 + PaceParamsWeight * Math.Log10(1 + Math.Max(0, ParamsK(run)));
            int cards = CardsPerEpoch(run, Math.Max(.5, compute), false);
            double data = Math.Pow(Math.Max(1, cards) / PaceReferenceCards, PaceCardsExponent);
            var a = XgCatalog.Arch(run.arch);
            double arch = 1 / Math.Max(.25, a == null ? 1 : a.speed) * (run.arch == "transformer" ? PaceTransformerFactor : 1);
            double s = PaceBaseSeconds * size * data * arch / Math.Pow(compute / PaceReferenceCompute, PaceComputeExponent);
            return PaceFinite(s) ? Math.Max(PaceMinSeconds, Math.Min(PaceMaxSeconds, s)) : PaceMinSeconds;
        }

        /// <summary>The bar for one line, for the training page. Idle lines report an empty, inactive bar.</summary>
        public XgPace Pace(XgRun run)
        {
            var p = new XgPace();
            if (run == null) return p;
            p.nominal = Math.Max(.01, run.epochDuration);
            if (!run.epochActive) return p;
            p.active = true;
            p.progress = PaceClamp01(run.epochProgress);
            p.phase = PhaseAt(p.progress);
            p.phaseLabel = PhaseLabel(p.phase);
            double factor = PaceClamp01(run.paceFactor);
            if (run.paceSeed == 0) { p.speed = factor; p.eta = (1 - p.progress) * p.nominal / Math.Max(.05, factor); return p; }
            double speed = factor <= 0 || p.phase == XgPacePhase.Validate ? 0 : RateAt(run, p.nominal, run.paceClock, p.progress) / PhaseRate(p.progress, p.nominal) * factor;
            p.speed = PaceFinite(speed) ? Math.Max(0, Math.Min(10, speed)) : 10;
            p.stall = StallNow(run);
            p.stallReason = StallReason(p.stall);
            p.eta = Eta(run, p.nominal, factor);
            return p;
        }

        public XgPace Pace(XgTrack track) => Pace(Run(track));

        public string PhaseLabel(XgPacePhase phase)
        {
            switch (phase)
            {
                case XgPacePhase.Load: return T("加载数据", "Loading data");
                case XgPacePhase.Train: return T("训练", "Training");
                case XgPacePhase.Validate: return T("验证", "Validating");
                default: return "";
            }
        }

        public string StallReason(XgPaceStall stall)
        {
            switch (stall)
            {
                case XgPaceStall.Throttle: return T("显卡降频（太热了）", "The card clocked down (too hot)");
                case XgPaceStall.Disk: return T("硬盘读得慢", "The hard disk is reading slowly");
                case XgPaceStall.SharedCard: return T("另一条线在用卡", "The other line is using the card");
                case XgPaceStall.Swap: return T("内存不够，换页中", "Out of memory, swapping to disk");
                default: return "";
            }
        }

        static XgPacePhase PhaseAt(double progress) => progress < PaceTrainAt ? XgPacePhase.Load : progress < PaceValidateAt ? XgPacePhase.Train : XgPacePhase.Validate;

        /// <summary>
        /// Starts a round's pace (BeginEpoch): its length and its seed, drawn from the save's RNG state without
        /// advancing it, and at most one stall whose conditions hold now.
        /// </summary>
        void PlanPace(XgRun run, IXgHost host, double nominal)
        {
            run.epochDuration = Math.Max(.01, nominal);
            run.paceSeed = PaceSeedFor(run);
            run.paceClock = 0;
            run.paceRate = 1 / run.epochDuration;
            run.paceFactor = 1;
            run.paceValidateUntil = -1;
            run.paceStall = (int)XgPaceStall.None; run.paceStallAt = 0; run.paceStallLength = 0; run.paceStallClock = -1;
            int seed = run.paceSeed;
            bool hot = host is IXgThermal thermal && thermal.Throttling;
            if (hot && Hash01(seed, 9001) < PaceThrottleChance)
            {
                // Hot cards clock down in the thick of it: mid-training, for a good while.
                run.paceStall = (int)XgPaceStall.Throttle;
                run.paceStallAt = .3 + .45 * Hash01(seed, 9002);
                run.paceStallLength = (.15 + .15 * Hash01(seed, 9003)) * run.epochDuration;
            }
            else if (Hash01(seed, 9011) < PaceDiskChance)
            {
                // A mechanical disk seeking through the samples: while the data loads.
                run.paceStall = (int)XgPaceStall.Disk;
                run.paceStallAt = .03 + .07 * Hash01(seed, 9012);
                run.paceStallLength = (.1 + .12 * Hash01(seed, 9013)) * run.epochDuration;
            }
            else if (Hash01(seed, 9021) < PaceSwapChance)
            {
                run.paceStall = (int)XgPaceStall.Swap;
                run.paceStallAt = .2 + .6 * Hash01(seed, 9022);
                run.paceStallLength = (.12 + .13 * Hash01(seed, 9023)) * run.epochDuration;
            }
        }

        /// <summary>A round started before pacing (an old save, or a test that set it by hand) plans from what it has.</summary>
        void EnsurePace(XgRun run)
        {
            if (run.paceSeed != 0) return;
            double nominal = Math.Max(.01, PaceFinite(run.epochDuration) && run.epochDuration > 0 ? run.epochDuration : EpochSeconds);
            double progress = PaceClamp01(run.epochProgress);
            PlanPace(run, null, nominal);
            run.epochProgress = progress;
            run.paceClock = progress * run.epochDuration;
        }

        int PaceSeedFor(XgRun run)
        {
            unchecked
            {
                ulong x = (ulong)S.rng ^ ((ulong)(uint)run.track * 0x9E3779B97F4A7C15UL) ^ ((ulong)(uint)(run.epoch + 1) * 0xC2B2AE3D27D4EB4FUL);
                x ^= x >> 33; x *= 0xFF51AFD7ED558CCDUL; x ^= x >> 33; x *= 0xC4CEB9FE1A85EC53UL; x ^= x >> 33;
                int s = (int)(x & 0x7FFFFFFF);
                return s == 0 ? 1 : s;
            }
        }

        /// <summary>Uniform in [0, 1) from a seed and an index: the same pair always gives the same number.</summary>
        static double Hash01(int seed, int i)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u ^ (uint)i * 0x85EBCA77u;
                h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
                return (h >> 8) / 16777216.0;
            }
        }

        bool StallActive(XgRun run, double clock) =>
            run.paceStall != (int)XgPaceStall.None && run.paceStallClock >= 0 && clock < run.paceStallClock + run.paceStallLength;

        /// <summary>A phase's own average progress per round-clock second (what speed 1 means).</summary>
        static double PhaseRate(double progress, double nominal) => progress < PaceTrainAt
            ? PaceTrainAt / (PaceLoadShare * nominal) : (PaceValidateAt - PaceTrainAt) / (PaceTrainShare * nominal);

        /// <summary>Progress per round-clock second at this moment (before the line's share of the card).</summary>
        double RateAt(XgRun run, double nominal, double clock, double progress)
        {
            if (progress >= PaceValidateAt) return 0;
            double size = BucketSeconds(progress);
            int bucket = (int)Math.Floor(clock / size);
            double u = Hash01(run.paceSeed, bucket), v = Hash01(run.paceSeed, bucket + 7919);
            double rate;
            if (progress < PaceTrainAt)
            {
                // Loading: the disk hands over the samples in bursts and gaps, mostly taking turns.
                bool gap = ((bucket + run.paceSeed) & 1) == 0 ? u < .8 : u < .2;
                rate = PhaseRate(progress, nominal) * (gap ? .1 + .3 * v : 1.1 + .9 * v);
            }
            else
            {
                // Forward and backward: steady, with a little jitter.
                rate = PhaseRate(progress, nominal) * (.85 + .3 * u);
            }
            if (StallActive(run, clock)) rate *= PaceStallSpeed;
            // Never past the deadline: once what is left at the phases' own speeds no longer fits before it, the bar
            // catches up just enough, so the whole round stays within ×1.5.
            double left = PaceTrainDeadline * nominal - clock;
            double expected = Math.Max(0, PaceTrainAt - progress) * PaceLoadShare * nominal / PaceTrainAt
                + (PaceValidateAt - Math.Max(progress, PaceTrainAt)) * PaceTrainShare * nominal / (PaceValidateAt - PaceTrainAt);
            if (left <= 1e-9) return double.PositiveInfinity;
            if (expected > left) rate = Math.Max(rate, PhaseRate(progress, nominal) * expected / left);
            return rate;
        }

        /// <summary>How often the speed changes: quicker while loading (the bursts), steadier while training.</summary>
        static double BucketSeconds(double progress) => progress < PaceTrainAt ? PaceLoadBucketSeconds : PaceBucketSeconds;

        /// <summary>
        /// Advances the round by <paramref name="seconds"/> of effective GPU time (wall time × the line's share of its
        /// cards). Integrates exactly piece by piece (speed buckets, phase and stall edges), so the frame rate does not matter.
        /// </summary>
        void AdvancePace(XgRun run, double seconds, double factor)
        {
            EnsurePace(run);
            run.paceFactor = factor;
            double nominal = Math.Max(.01, run.epochDuration);
            double before = run.epochProgress, left = seconds;
            for (int guard = 0; left > 1e-12 && run.epochProgress < 1 && guard < 100000; guard++)
            {
                double t = run.paceClock, p = run.epochProgress;
                if (p >= PaceValidateAt - 1e-12)
                {
                    // Validation: a pause, then the jump to 100%.
                    if (run.paceValidateUntil < 0) run.paceValidateUntil = ValidateEnd(run, nominal, t);
                    double wait = run.paceValidateUntil - t;
                    if (wait > left) { run.paceClock += left; left = 0; break; }
                    wait = Math.Max(0, wait);
                    run.paceClock += wait; left -= wait;
                    run.epochProgress = 1;
                    break;
                }
                if (run.paceStall != (int)XgPaceStall.None && run.paceStallClock < 0 && p >= run.paceStallAt) run.paceStallClock = t;
                double bucket = BucketSeconds(p);
                double step = Math.Min(left, (Math.Floor(t / bucket) + 1) * bucket - t);
                if (StallActive(run, t)) step = Math.Min(step, run.paceStallClock + run.paceStallLength - t);
                double deadline = PaceTrainDeadline * nominal - t;
                if (deadline <= 1e-9) { run.epochProgress = PaceValidateAt; continue; }
                step = Math.Min(step, deadline);
                double rate = RateAt(run, nominal, t, p);
                double edge = p < PaceTrainAt ? PaceTrainAt : PaceValidateAt;
                if (run.paceStall != (int)XgPaceStall.None && run.paceStallClock < 0 && run.paceStallAt > p) edge = Math.Min(edge, run.paceStallAt);
                if (rate > 0 && (edge - p) / rate < step) step = (edge - p) / rate;
                step = Math.Max(step, 1e-9);
                double next = p + rate * step;
                run.epochProgress = next >= edge - 1e-12 ? edge : next;
                run.paceClock = t + step;
                left -= step;
            }
            // The ETA follows a smoothed rate, the way real ETAs wobble with the last few seconds.
            if (seconds > 0)
            {
                double observed = (run.epochProgress - before) / seconds;
                double k = 1 - Math.Exp(-seconds / PaceEtaSmoothing);
                run.paceRate += (observed - run.paceRate) * k;
            }
        }

        /// <summary>The validation pause ends at its own length after it starts, but never before ×0.7 or after ×1.5.</summary>
        double ValidateEnd(XgRun run, double nominal, double clock)
        {
            double hold = PaceValidateShare * nominal * (.7 + .6 * Hash01(run.paceSeed, 9101));
            return Math.Max(PaceMinFactor * nominal, Math.Min(PaceMaxFactor * nominal, clock + hold));
        }

        XgPaceStall StallNow(XgRun run)
        {
            if (StallActive(run, run.paceClock)) return (XgPaceStall)run.paceStall;
            return SharesCardWithOtherLine(run) ? XgPaceStall.SharedCard : XgPaceStall.None;
        }

        /// <summary>Both lines are in a round and at least one card trains both (no rig: the one virtual card).</summary>
        bool SharesCardWithOtherLine(XgRun run)
        {
            XgRun other = null;
            foreach (var r in Runs) if (r != run && r.track != run.track) other = r;
            if (other == null || !other.epochActive || !run.epochActive) return false;
            var view = wiringView;
            if (view == null) return true;
            var a = view.Node("run:" + run.track);
            var b = view.Node("run:" + other.track);
            if (a == null || b == null) return true;
            foreach (string key in a.cards) if (b.cards.Contains(key)) return true;
            return false;
        }

        /// <summary>
        /// Remaining wall seconds: what is left of training at the smoothed rate, plus validation. Bounded by what the
        /// round can still take, so it never runs past the end, and 0 once the bar is full.
        /// </summary>
        double Eta(XgRun run, double nominal, double factor)
        {
            double p = PaceClamp01(run.epochProgress), t = run.paceClock;
            if (p >= 1) return 0;
            double clockLeft;
            if (p >= PaceValidateAt)
                clockLeft = Math.Max(0, (run.paceValidateUntil >= 0 ? run.paceValidateUntil : t + PaceValidateShare * nominal) - t);
            else
            {
                double rate = Math.Max(run.paceRate, .02 / nominal);
                clockLeft = (PaceValidateAt - p) / rate + PaceValidateShare * nominal;
            }
            clockLeft = Math.Max(0, Math.Min(clockLeft, PaceMaxFactor * nominal - t));
            double eta = clockLeft / Math.Max(.05, factor);
            return PaceFinite(eta) ? Math.Max(0, eta) : 0;
        }

        static bool PaceFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
        static double PaceClamp01(double v) => PaceFinite(v) ? Math.Max(0, Math.Min(1, v)) : 0;
    }
}
