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
        /// <summary>The desk's per-label pay, the streak and speed bonus on top of it, and the fine of a wrong answer.</summary>
        public double pay, bonus, fine;
        /// <summary>The streak multiplier this answer was paid at (1 when it was wrong or did not count for the streak).</summary>
        public int multiplier;
        public double Total => pay + bonus;
    }

    /// <summary>
    /// One pay rule for every desk game (切短信, 打地鼠, 对暗号 … and the writing view of 手写数字). A game answers items;
    /// each valid item is one action:
    ///  - right: one sample of the desk's own dataset and the desk's hand pay (<see cref="XgSim.ManualPayFor"/>, which already
    ///    holds the raise and <see cref="XgCatalog.HandPayScale"/>) times the streak multiplier (×1 … ×5, the shared combo:
    ///    the third right answer in a row pays double, each two completed answers unlock the following tier), plus a speed bonus of at most
    ///    <see cref="MiniSpeedCap"/> of the base for being quick (<see cref="MiniFastSeconds"/>; 常识判断: under 7.5 s);
    ///  - wrong: no sample, no pay, the combo breaks, and the desk's own fine if it has one (XgDesk.fine; only 算术 does).
    /// The size of a game round, its score or the value of an item never changes the pay.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The speed bonus never exceeds this share of the base pay (it is not multiplied by the streak).</summary>
        public const double MiniSpeedCap = .5;
        /// <summary>The whole bonus never exceeds this share of the base pay: the ×5 streak plus the full speed bonus.</summary>
        public const double MiniBonusCap = ComboMaxMultiplier - 1 + MiniSpeedCap;
        /// <summary>常识判断 pays its flat speed bonus for answers under this many seconds (its clock is 15 s).</summary>
        public const double SenseFastSeconds = 7.5;
        /// <summary>An answer this fast earns the full speed bonus; it fades to nothing at this many seconds.</summary>
        public const double MiniFastSeconds = 5;

        /// <summary>What one right answer on a desk pays before any bonus.</summary>
        public double MiniBasePay(string desk) { return ManualPayFor(desk, LevelOf(desk)); }

        /// <summary>The speed share of the bonus: +50% for an instant answer, fading linearly to 0 at five seconds. A negative time means "not timed".</summary>
        public static double MiniSpeedShare(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0 || seconds > MiniFastSeconds) return 0;
            return Math.Max(0, Math.Min(MiniSpeedCap, MiniSpeedCap * (1 - seconds / MiniFastSeconds)));
        }

        /// <summary>The bonus as a share of the base pay: the streak's share (multiplier − 1) plus the speed share, capped at <see cref="MiniBonusCap"/>.</summary>
        public static double MiniBonusShare(double seconds, double comboMultiplier, bool sense = false)
        {
            double combo = double.IsNaN(comboMultiplier) ? 0 : Math.Max(0, Math.Min(ComboMaxMultiplier, comboMultiplier) - 1);
            double speed = sense ? (seconds >= 0 && seconds < SenseFastSeconds ? MiniSpeedCap : 0) : MiniSpeedShare(seconds);
            return Math.Max(0, Math.Min(MiniBonusCap, combo + speed));
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
                if (useCombo) Hit(info.comboWindow);
                result.pay = MiniBasePay(desk);
                result.multiplier = useCombo ? StreakMultiplier : 1;
                // The writing view has no streak, so it keeps its old speed bonus of up to +100% (twice the speed share).
                result.bonus = useCombo ? result.pay * MiniBonusShare(seconds, result.multiplier, desk == XgMemes.SenseDesk)
                    : result.pay * 2 * MiniSpeedShare(seconds);
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
