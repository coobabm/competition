using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>What one answered item of a desk game did to the books (<see cref="XgSim.SettleMiniAction"/>).</summary>
    public struct XgMiniAction
    {
        /// <summary>The action was counted (the desk exists and a wallet was given).</summary>
        public bool accepted;
        public bool correct;
        /// <summary>Samples added to the desk's dataset: 1 for a right answer (a waiting log can add more), 0 for a wrong one.</summary>
        public int samples;
        /// <summary>The desk's per-label pay, the speed or combo bonus on top of it, and the fine of a wrong answer.</summary>
        public double pay, bonus, fine;
        public double Total => pay + bonus;
    }

    /// <summary>
    /// One pay rule for every desk game (切短信, 打地鼠, 对暗号 … and the writing view of 手写数字). A game answers items;
    /// each valid item is one action:
    ///  - right: one sample of the desk's own dataset and the desk's hand pay (<see cref="XgSim.ManualPayFor"/>, which already
    ///    holds the raise and <see cref="XgCatalog.HandPayScale"/>), plus a bonus of at most +100% of that pay for being quick
    ///    (<see cref="MiniFastSeconds"/>) or for a streak (the shared combo), whichever is larger; the two never add up;
    ///  - wrong: no sample, no pay, the combo breaks, and the desk's own fine if it has one (XgDesk.fine; only 算术 does).
    /// The size of a game round, its score or the value of an item never changes the pay.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The bonus never exceeds this share of the base pay.</summary>
        public const double MiniBonusCap = 1;
        /// <summary>An answer this fast earns the full speed bonus; it fades to nothing at this many seconds.</summary>
        public const double MiniFastSeconds = 5;

        /// <summary>What one right answer on a desk pays before any bonus.</summary>
        public double MiniBasePay(string desk) { return ManualPayFor(desk, LevelOf(desk)); }

        /// <summary>The speed share of the bonus: +100% for an instant answer, fading linearly to 0 at five seconds. A negative time means "not timed".</summary>
        public static double MiniSpeedShare(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0 || seconds > MiniFastSeconds) return 0;
            return Math.Max(0, Math.Min(MiniBonusCap, 1 - seconds / MiniFastSeconds));
        }

        /// <summary>The bonus as a share of the base pay: the larger of the speed share and the combo share, capped at <see cref="MiniBonusCap"/>.</summary>
        public static double MiniBonusShare(double seconds, double comboMultiplier)
        {
            double combo = double.IsNaN(comboMultiplier) ? 0 : comboMultiplier - 1;
            return Math.Max(0, Math.Min(MiniBonusCap, Math.Max(MiniSpeedShare(seconds), combo)));
        }

        /// <summary>
        /// Books one answered item on <paramref name="desk"/>. <paramref name="seconds"/> is how long the answer took (negative:
        /// not timed, so only the combo can earn a bonus); <paramref name="useCombo"/> false leaves the shared combo alone
        /// (the writing view pays for speed only).
        /// </summary>
        public XgMiniAction SettleMiniAction(string desk, bool correct, IXgHost host, double seconds = -1, bool useCombo = true)
        {
            var info = XgCatalog.Desk(desk);
            var result = new XgMiniAction { correct = correct };
            if (info == null || host == null) return result;
            result.accepted = true;
            NoteLabelSpeed();
            if (correct)
            {
                if (useCombo) Hit(info.comboWindow, 1);
                result.pay = MiniBasePay(desk);
                result.bonus = desk == XgMemes.SenseDesk
                    ? result.pay * ((useCombo ? System.Math.Min(2, ComboMultiplier) : 1) - 1
                        + (seconds >= 0 && seconds < 7.5 ? .5 : 0))
                    : result.pay * MiniBonusShare(seconds, useCombo ? ComboMultiplier : 1);
                host.Earn(result.Total); S.totalIncome += result.Total;
                S.handCorrect++; QualityHandCorrect();
                AddLabel(desk);
                result.samples = 1 + (int)HandLabelLog(desk);
                MemeDriftLabelled(desk);
                NoteDayAnswer(true, result.Total, result.samples);
            }
            else
            {
                if (useCombo) BreakCombo();
                S.handWrong++;
                result.fine = ChargeHandFine(desk, LevelOf(desk), host);
                NoteDayAnswer(false, -result.fine, 0);
            }
            result.samples += RelicOnMiniAction(desk, correct);
            CheckDesks();
            return result;
        }
    }
}
