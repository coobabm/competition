using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The learning rate the player picks before a round (训练参数, 2026-10-16), as a step relative to the model's safe
    /// rate (<see cref="SafeLr"/>, the largest that cannot tear): two bigger, the safe one, two smaller, each ×3 apart,
    /// shown as real numbers (<see cref="RateShown"/>). 「自动」 is the safe rate, as before. A bigger step learns faster
    /// but may 炸: the weights go NaN, the round is wasted and the model falls back a good way. A smaller step is steadier
    /// and slower. The bot and anyone who leaves it on 自动 train exactly as before (no extra rolls).
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The steps the picker offers, biggest first: +2 and +1 are bigger than safe, −1 and −2 smaller.</summary>
        public static readonly int[] RateNotches = { 2, 1, 0, -1, -2 };
        /// <summary>Chance a round tears one step above the safe rate, and two steps above.</summary>
        public const double TearChanceOneUp = .35, TearChanceFarUp = .9;
        /// <summary>A torn round loses this share of the dataset's range (plus up to as much again): about four normal drops.</summary>
        public const double TearFallMin = .04, TearFallSpread = .04;

        static int ClampNotch(int n) => Math.Max(-2, Math.Min(2, n));

        /// <summary>The run's step relative to the safe rate (positive = bigger); 0 on 自动.</summary>
        public int RateNotch(XgRun run) => run == null || run.autoLr ? 0 : ClampNotch(run.rateNotch);

        /// <summary>The rate a step stands for, as the picker shows it (the safe rate ×3 per step, in the optimiser's units).</summary>
        public double RateShown(XgRun run, int notch) => run == null ? 0 : RateValues[SafeLr(run)] * RateDisplayScale * Math.Pow(3, ClampNotch(notch));
        public string RateShownText(XgRun run, int notch)
        {
            double v = RateShown(run, notch);
            return v.ToString(v >= .1 ? "0.##" : v >= .001 ? "0.####" : "0.######", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Chance a round at this step tears (0 at or below the safe rate). 梯度裁剪 and 预热 soften it.</summary>
        public double TearChanceAt(int notch)
        {
            if (notch <= 0) return 0;
            double p = notch == 1 ? TearChanceOneUp : TearChanceFarUp;
            if (ClipOwned) p *= .6;
            if (WarmupOwned) p *= .8;
            return p;
        }

        /// <summary>This round's chance to tear at the run's chosen step.</summary>
        public double TearChance(XgRun run) => TearChanceAt(RateNotch(run));

        /// <summary>
        /// How much faster a round learns at this step than at the safe rate: high risk, high return (+2 ×2.5, +1 ×1.6);
        /// low risk, low return (−1 ×0.7, −2 ×0.45).
        /// </summary>
        public static double RateGainAt(int notch) => notch >= 2 ? 2.5 : notch == 1 ? 1.6 : notch == 0 ? 1 : notch == -1 ? .7 : .45;
        /// <summary>The share of the usual drop chance left at this step: −1 wobbles less, −2 never goes down.</summary>
        public static double RateSteadyAt(int notch) => notch >= 0 ? 1 : notch == -1 ? .4 : 0;
        /// <summary>The tier's name: 高风险 / 冒险 / 刚好 / 稳 / 最稳.</summary>
        public string RateTierName(int notch) => notch >= 2 ? T("高风险", "High risk") : notch == 1 ? T("冒险", "Risky") : notch == 0 ? T("刚好", "Edge") : notch == -1 ? T("稳", "Steady") : T("最稳", "Safest");

        double RateGain(XgRun run) => RateGainAt(RateNotch(run));
        double RateSteady(XgRun run) => RateSteadyAt(RateNotch(run));

        /// <summary>The rate index the board trains with: the safe one moved by the player's step, within the list.</summary>
        int BoardLr(XgRun run) => Math.Max(0, Math.Min(RateValues.Length - 1, SafeLr(run) - RateNotch(run)));

        /// <summary>Picks the step for a line's next rounds (−2…2), or 自动 with <paramref name="auto"/>. Not during a round.</summary>
        public bool SetRate(XgTrack track, int notch, bool auto = false)
        {
            var run = Run(track);
            if (run == null || run.epochActive) return false;
            run.autoLr = auto;
            run.rateNotch = auto ? 0 : ClampNotch(notch);
            run.lr = BoardLr(run);
            return true;
        }

        /// <summary>Saves from before the picker (rateSchema 0) start every line on 自动, whatever an older knob era left.</summary>
        void RepairRates()
        {
            if (S.rateSchema >= 1) return;
            foreach (var run in Runs) if (run != null) { run.autoLr = true; run.rateNotch = 0; run.lr = SafeLr(run); }
            S.rateSchema = 1;
        }

        /// <summary>「学习率 0.9 太大：这一轮炸了（权重变成 NaN），白练一轮，退回 52 分。」 The round's fall, set on <paramref name="e"/>.</summary>
        void TearRound(XgRun run, XgEpoch e, double before, double chance, double range)
        {
            double fallen = Math.Max(chance, before - range * (TearFallMin + TearFallSpread * Roll()));
            string rate = RateShownText(run, RateNotch(run));
            e.diverged = true;
            S.divergences++;
            S.combo = 0; S.comboTimer = 0;
            if (before - fallen > 1e-12 && RelicGuardReady)
            {
                S.relicCharge = 0;
                e.rolledBack = true;
                Say(T("学习率 " + rate + " 太大，这一轮炸了；残差贴纸把它挡住了，不计入。", "Rate " + rate + " was too big and the round blew up; the residual sticker caught it."));
                return;
            }
            run.valAcc = fallen;
            e.dropped = before - fallen > 1e-12;
            e.dropReason = "学习率太大，炸了"; e.dropReasonEn = "the rate was too big and it blew up";
            string points = F((before - fallen) * 1000 / Math.Max(1e-9, range), "0");
            Say(T("学习率 " + rate + " 太大：这一轮炸了（权重变成 NaN），白练一轮，退回 " + points + " 分。",
                "Rate " + rate + " was too big: the round blew up (the weights went NaN), wasted, down " + points + " points."));
        }
    }

    public sealed partial class XgState
    {
        /// <summary>Rounds that blew up because the player's rate was too big.</summary>
        public int divergences;
        /// <summary>1 once every line was put on 自动 for the rate picker (<see cref="XgSim.RepairRates"/>).</summary>
        public int rateSchema;
    }
}
