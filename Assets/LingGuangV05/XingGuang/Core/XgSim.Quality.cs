using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>Why the 摆渡众包 platform reported the account.</summary>
    public enum XgReportReason { None = 0, LowPassRate = 1, MonotoneAnswers = 2, SuspectedBot = 3 }

    /// <summary>The platform's credit tiers: 金牌 (credit 90 and up), 普通 (60 to 89), 重点关注 (below 60).</summary>
    public enum XgCreditTier { Gold, Normal, Watch }

    /// <summary>One failed spot check: the card, the pay it would have earned and the fine actually taken.</summary>
    public sealed class XgQcFine
    {
        public long cardId;
        public string dataset = "";
        public double pay, fine;
        /// <summary>The card was one of the platform's known-answer trap items (金标题).</summary>
        public bool trap;
    }

    /// <summary>Platform quality control state. Saved with the lab and repaired on load.</summary>
    public partial class XgState
    {
        /// <summary>0 for saves from before quality control; repair sets the starting credit once.</summary>
        public int qcVersion;
        public double qcCredit = XgSim.QcCreditStart;
        /// <summary>Seconds left on the current freeze (0 = not frozen).</summary>
        public double qcFrozen;
        public int qcReports, qcReason;
        /// <summary>The current freeze was already appealed; a warning was shown since the rate last dropped under the line.</summary>
        public bool qcAppealed, qcWarned;
        /// <summary>Total fines taken by the platform.</summary>
        public double qcFines;
        /// <summary>Lifetime spot checks and failed spot checks.</summary>
        public int qcCheckedTotal, qcFailedTotal;
        /// <summary>The last spot checks across all desks (true = passed), oldest first.</summary>
        public List<bool> qcChecks = new List<bool>();
        /// <summary>The last automatic answers submitted to the platform (true = 是), oldest first.</summary>
        public List<bool> qcAnswers = new List<bool>();
        /// <summary>A separate random stream so spot checks never shift card generation or the audit rolls.</summary>
        public long qcRng = XgSim.QcSeed;
        /// <summary>Spot checks since the platform last judged the window (it judges every 20).</summary>
        public int qcSinceJudge;
    }

    /// <summary>
    /// 摆渡众包 quality control. The platform spot-checks automatic labels that left the lab (hand labels are never
    /// checked), fines the wrong ones, tracks a credit score and a rolling pass rate over the last 40 checks, and
    /// judges it every 20 checks: a 15% error rate warns, 20% over a full window of 40 reports the account (15% when
    /// the answers look scripted). A report freezes automatic labelling
    /// for a while; hand labelling keeps working, and an appeal can lift the freeze for a fee.
    /// Only active once the player owns the first level of 自动答题.
    /// </summary>
    public sealed partial class XgSim
    {
        public const long QcSeed = 0x0C0FFEE5L;
        public const int QcWindow = 40, QcMinChecks = 20, QcJudgeEvery = 20, QcMonotoneMin = 20;
        public const double QcWarnRate = .15, QcReportRate = .20, QcMonotoneShare = .95;
        public const double QcCreditStart = 80, QcCreditMax = 100, QcCreditGold = 90, QcCreditWatch = 60;
        public const double QcCreditFail = 4, QcCreditPass = 1, QcCreditReport = 20, QcCreditHand = .2;
        public const double QcFineMultiplier = 2, QcAppealMin = 100, QcAppealShare = .1;
        /// <summary>Freeze length for the first, second and third (and later) report.</summary>
        public static readonly double[] QcFreezeSeconds = { 180, 480, 1200 };

        /// <summary>Tests and tools may switch the platform off to look at the lab's own rules in isolation.</summary>
        public bool PlatformChecks = true;
        /// <summary>Tests may pin the spot-check chance (0–1); negative means the credit tier decides.</summary>
        public double ForcedCheckChance = -1;

        public event Action<XgQcFine> QualityFined;
        /// <summary>The rolling error rate crossed the warning line (argument: error rate 0–1).</summary>
        public event Action<double> QualityWarned;
        /// <summary>The account was reported: reason and freeze length in seconds.</summary>
        public event Action<XgReportReason, double> QualityReported;
        /// <summary>The freeze ended (true when lifted by an appeal).</summary>
        public event Action<bool> QualityUnfrozen;

        public bool QualityActive => PlatformChecks && GlobalAutoLevel >= 1;
        public bool QualityFrozen => QualityActive && S.qcFrozen > 0;
        public double FreezeSecondsLeft => QualityFrozen ? S.qcFrozen : 0;
        public double Credit => S.qcCredit;
        public XgCreditTier CreditTier => TierOfCredit(S.qcCredit);
        public XgReportReason LastReportReason => (XgReportReason)S.qcReason;
        public int SpotChecks => S.qcChecks.Count;
        public int FailedSpotChecks { get { int n = 0; foreach (bool ok in S.qcChecks) if (!ok) n++; return n; } }
        /// <summary>Share of the last spot checks that passed (1 before any check).</summary>
        public double PassRate => S.qcChecks.Count == 0 ? 1 : 1 - (double)FailedSpotChecks / S.qcChecks.Count;
        public double ErrorRate => 1 - PassRate;
        /// <summary>The platform only judges the rate once enough checks are in.</summary>
        public bool PassRateJudged => S.qcChecks.Count >= QcMinChecks;
        /// <summary>The rate is at or over the warning line (and the account is not already frozen).</summary>
        public bool QualityWarning => QualityActive && !QualityFrozen && PassRateJudged && ErrorRate >= QcWarnRate - 1e-9;

        public static XgCreditTier TierOfCredit(double credit) => credit >= QcCreditGold ? XgCreditTier.Gold : credit >= QcCreditWatch ? XgCreditTier.Normal : XgCreditTier.Watch;
        public static double CheckChanceOf(XgCreditTier tier) => tier == XgCreditTier.Gold ? .25 : tier == XgCreditTier.Normal ? .4 : .65;
        public static double PayMultiplierOf(XgCreditTier tier) => tier == XgCreditTier.Gold ? 1.2 : tier == XgCreditTier.Normal ? 1 : .8;
        public static double FreezeSecondsFor(int report) => QcFreezeSeconds[Math.Max(0, Math.Min(QcFreezeSeconds.Length - 1, report - 1))];
        /// <summary>Chance that the platform checks one automatic label right now.</summary>
        public double SpotCheckChance => !QualityActive ? 0 : ForcedCheckChance >= 0 ? Math.Min(1, ForcedCheckChance) : CheckChanceOf(CreditTier);
        /// <summary>Credit-tier multiplier on automatic income (1 while the platform is not active).</summary>
        public double QualityPayMultiplier => QualityActive ? PayMultiplierOf(CreditTier) : 1;

        public string CreditTierName(XgCreditTier tier) => tier == XgCreditTier.Gold ? T("金牌") : tier == XgCreditTier.Normal ? T("普通") : T("重点关注");
        public static string ReportReasonText(XgReportReason reason, bool english)
        {
            switch (reason)
            {
                case XgReportReason.MonotoneAnswers: return english ? "answers nearly identical, suspected script" : "答案高度雷同，疑似脚本";
                case XgReportReason.LowPassRate: return english ? "spot-check pass rate too low" : "抽检合格率过低";
                case XgReportReason.SuspectedBot: return english ? "suspected bot" : "疑似机器操作";
                default: return "";
            }
        }
        public string ReportReasonText(XgReportReason reason) => ReportReasonText(reason, English);
        /// <summary>m:ss, rounded up so a running clock never shows 0:00 while still frozen.</summary>
        public static string FreezeClock(double seconds)
        {
            int s = FiniteCollaboration(seconds) ? (int)Math.Ceiling(Math.Max(0, seconds)) : 0;
            return s / 60 + ":" + (s % 60).ToString("00");
        }

        void PrepareQuality()
        {
            if (S.qcChecks == null) S.qcChecks = new List<bool>();
            if (S.qcAnswers == null) S.qcAnswers = new List<bool>();
            if (S.qcVersion == 0) { S.qcCredit = QcCreditStart; S.qcVersion = 1; }
            if (!FiniteCollaboration(S.qcCredit)) S.qcCredit = QcCreditStart;
            S.qcCredit = Math.Max(0, Math.Min(QcCreditMax, S.qcCredit));
            if (!FiniteCollaboration(S.qcFrozen) || S.qcFrozen < 0) S.qcFrozen = 0;
            S.qcFrozen = Math.Min(S.qcFrozen, QcFreezeSeconds[QcFreezeSeconds.Length - 1]);
            S.qcReports = Math.Max(0, S.qcReports);
            if (!FiniteCollaboration(S.qcFines) || S.qcFines < 0) S.qcFines = 0;
            S.qcCheckedTotal = Math.Max(0, S.qcCheckedTotal);
            S.qcFailedTotal = Math.Max(0, Math.Min(S.qcCheckedTotal, S.qcFailedTotal));
            if (!Enum.IsDefined(typeof(XgReportReason), S.qcReason)) S.qcReason = (int)XgReportReason.None;
            if (S.qcFrozen > 0 && S.qcReason == (int)XgReportReason.None) S.qcReason = (int)XgReportReason.LowPassRate;
            Trim(S.qcChecks, QcWindow); Trim(S.qcAnswers, QcWindow);
            S.qcSinceJudge = Math.Max(0, Math.Min(QcJudgeEvery - 1, S.qcSinceJudge));
            if (S.qcRng == 0) S.qcRng = QcSeed;
            PrepareTraps();
            PrepareCaptcha();
        }

        double QualityRoll()
        {
            long x = S.qcRng;
            x ^= x << 13; x ^= (long)((ulong)x >> 7); x ^= x << 17;
            S.qcRng = x == 0 ? QcSeed : x;
            return (double)((ulong)S.qcRng >> 11) / (1UL << 53);
        }

        void AddCredit(double delta) { S.qcCredit = Math.Max(0, Math.Min(QcCreditMax, S.qcCredit + delta)); }

        /// <summary>
        /// Settles one automatic label that reached the platform (not intercepted by the lab's own audit).
        /// Returns the fine taken (0 when unchecked, correct, or the wallet is empty); sets whether it was checked.
        /// </summary>
        double SettleSpotCheck(XgCard card, bool correct, double unitPay, IXgHost host, out bool spotChecked)
        {
            spotChecked = false;
            if (!QualityActive) return 0;
            S.qcAnswers.Add(card.guess); Trim(S.qcAnswers, QcWindow);
            // Trap items (金标题) are always checked; ordinary labels only by chance.
            bool trap = IsTrap(card);
            if (!trap && QualityRoll() >= SpotCheckChance) return 0;
            spotChecked = true;
            S.qcCheckedTotal++;
            if (trap) RememberTrap(card);
            S.qcChecks.Add(correct);
            // A wrong trap answer weighs twice in the window: the platform knew the answer all along.
            if (!correct && trap) S.qcChecks.Add(false);
            Trim(S.qcChecks, QcWindow);
            if (correct) { AddCredit(QcCreditPass); return 0; }
            S.qcFailedTotal++;
            if (trap) NoteTrapFailure();
            AddCredit(-(trap ? TrapCreditFail : QcCreditFail));
            double due = (trap ? TrapFineMultiplier : QcFineMultiplier) * unitPay;
            double money = host != null && FiniteCollaboration(host.Money) ? Math.Max(0, host.Money) : 0;
            double fine = Math.Min(due, money);
            if (fine > 0 && host.Spend(fine)) { S.totalSpent += fine; S.qcFines += fine; }
            else fine = 0;
            return fine;
        }

        /// <summary>After a spot check: report, warn once per crossing, or re-arm the warning.</summary>
        void JudgeQuality()
        {
            // The platform reviews the account in batches: every 20 checks, once 20 are in.
            if (++S.qcSinceJudge < QcJudgeEvery || !PassRateJudged) return;
            S.qcSinceJudge = 0;
            double error = ErrorRate;
            bool warn = error >= QcWarnRate - 1e-9;
            // Reports need a full window, so a short unlucky streak is not enough.
            bool full = S.qcChecks.Count >= QcWindow;
            if (full && error >= QcReportRate - 1e-9) Report(MonotoneAnswers ? XgReportReason.MonotoneAnswers : XgReportReason.LowPassRate);
            else if (full && warn && MonotoneAnswers) Report(XgReportReason.MonotoneAnswers);
            else if (warn)
            {
                if (S.qcWarned) return;
                S.qcWarned = true;
                string pass = F(PassRate * 100, "0") + "%", line = F((1 - QcReportRate) * 100, "0") + "%";
                Say(T("摆渡众包：近期抽检合格率 " + pass + "，低于 " + line + " 将被举报。", "Bodu Crowdsourcing: recent spot-check pass rate " + pass + ". Below " + line + " the account will be reported."));
                QualityWarned?.Invoke(error);
            }
            else S.qcWarned = false;
        }

        /// <summary>Nearly every recent automatic answer is the same (all 是 or all 否).</summary>
        public bool MonotoneAnswers
        {
            get
            {
                int n = S.qcAnswers.Count;
                if (n < QcMonotoneMin) return false;
                int yes = 0; foreach (bool a in S.qcAnswers) if (a) yes++;
                return Math.Max(yes, n - yes) >= QcMonotoneShare * n - 1e-9;
            }
        }

        void Report(XgReportReason reason)
        {
            S.qcReports++;
            AddCredit(-QcCreditReport);
            S.qcChecks.Clear(); S.qcAnswers.Clear(); S.qcSinceJudge = 0;
            S.qcWarned = false; S.qcAppealed = false;
            ClearCaptcha();
            double seconds = FreezeSecondsFor(S.qcReports);
            S.qcFrozen = seconds; S.qcReason = (int)reason;
            string minutes = F(seconds / 60, "0");
            Say(T("摆渡众包：账号被举报（" + ReportReasonText(reason, false) + "），自动标注冻结 " + minutes + " 分钟。手动标注不受影响。",
                "Bodu Crowdsourcing: account reported (" + ReportReasonText(reason, true) + "). Auto labelling is frozen for " + minutes + " min; hand labelling still works."));
            QualityReported?.Invoke(reason, seconds);
        }

        /// <summary>Counts the freeze down by real seconds. Called once per step, before any routing.</summary>
        void TickQuality(double dt)
        {
            if (dt <= 0 || !FiniteCollaboration(dt)) return;
            TickCaptcha(dt);
            if (S.qcFrozen <= 0) return;
            S.qcFrozen = Math.Max(0, S.qcFrozen - dt);
            if (S.qcFrozen > 0) return;
            S.qcAppealed = false;
            Say(T("摆渡众包：账号已解冻，自动标注恢复。"));
            QualityUnfrozen?.Invoke(false);
        }

        /// <summary>A correct hand label while frozen earns a little trust back.</summary>
        void QualityHandCorrect() { if (QualityFrozen) AddCredit(QcCreditHand); }

        /// <summary>申诉: max(¥100, 10% of the wallet).</summary>
        public double AppealCost(IXgHost host)
        {
            double money = host != null && FiniteCollaboration(host.Money) ? Math.Max(0, host.Money) : 0;
            return Math.Max(QcAppealMin, QcAppealShare * money);
        }
        public bool CanAppeal(IXgHost host, out string why)
        {
            why = null;
            if (!QualityFrozen) { why = T("账号没有被冻结"); return false; }
            if (S.qcAppealed) { why = T("这次冻结已经申诉过了"); return false; }
            if (host == null || !FiniteCollaboration(host.Money) || host.Money + 1e-9 < AppealCost(host)) { why = T("经费不足"); return false; }
            return true;
        }
        /// <summary>Pays the appeal fee and lifts the freeze now. Once per freeze; credit is unchanged.</summary>
        public bool Appeal(IXgHost host)
        {
            if (!CanAppeal(host, out string why)) { if (QualityFrozen) Say(why); return false; }
            double cost = AppealCost(host);
            if (!host.Spend(cost)) { Say(T("经费不足")); return false; }
            S.totalSpent += cost;
            S.qcAppealed = true;
            S.qcFrozen = 0;
            Say(T("摆渡众包：申诉通过，账号解冻。信用分不变。"));
            QualityUnfrozen?.Invoke(true);
            return true;
        }
    }
}
