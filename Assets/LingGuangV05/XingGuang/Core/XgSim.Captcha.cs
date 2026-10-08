using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public partial class XgState
    {
        /// <summary>The platform's suspicion that a machine is labelling (0–1); a captcha is due when it is full.</summary>
        public double qcSuspicion;
        /// <summary>Labels per second over the last minute, as sixty one-second buckets (ring buffer).</summary>
        public List<double> qcSpeed = new List<double>();
        public int qcSpeedHead;
        public double qcSpeedClock;
        /// <summary>A captcha is waiting: its four digits, the drawing seed and the seconds left to answer.</summary>
        public bool qcCaptcha;
        public string qcCaptchaCode = "";
        public int qcCaptchaSeed;
        public double qcCaptchaLeft;
        /// <summary>Seconds until the platform may ask again; automatic pause after a failed captcha.</summary>
        public double qcCaptchaCooldown, qcAutoPause;
        /// <summary>Seconds until 验证码代填 submits the answer (negative = not scheduled).</summary>
        public double qcAutofillIn = -1;
        /// <summary>Failed captchas in a row, captchas asked, autofilled answers, repeated high-speed autofills flagged.</summary>
        public int qcCaptchaFails, qcCaptchaTotal, qcAutofills, qcAutofillFlags;
    }

    /// <summary>
    /// 人机验证: the platform watches how fast the account labels (automatic plus hand). While the rolling one-minute
    /// rate is above 1.5 labels per second a suspicion meter fills; when it is full the platform asks for a
    /// four-digit handwritten captcha (at most one every six minutes). Automatic labelling waits until it is
    /// answered. A wrong or late answer pauses automatic labelling for two minutes; three in a row get the account
    /// reported as a suspected bot. 验证码代填 lets the AI answer instead; only repeated use at a high submission rate
    /// may draw a platform flag, so its first successes are not punished.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int SpeedWindow = 60, CaptchaLength = 4, CaptchaFailsToReport = 3;
        public const double SpeedLimit = 1.5, SuspicionFillSeconds = 150, SuspicionDrainSeconds = 120;
        public const double CaptchaGap = 360, CaptchaTimeout = 30, CaptchaPause = 120;
        public const double CaptchaCreditPass = 1, CaptchaCreditFail = 3;
        public const double AutofillMinDelay = 3, AutofillMaxDelay = 6, AutofillFlagChance = .08, AutofillFlagCredit = 10;
        public const int AutofillGracePasses = 3;
        public const string CaptchaAutofillNode = "label.captcha";

        /// <summary>Tests may pin the roll that decides whether an autofilled captcha is flagged (negative = random).</summary>
        public double ForcedAutofillRoll = -1;

        public event Action CaptchaRequired;
        /// <summary>A captcha was answered right (true when 验证码代填 answered it).</summary>
        public event Action<bool> CaptchaSolved;
        /// <summary>A captcha was answered wrong or timed out: (timed out, failures in a row).</summary>
        public event Action<bool, int> CaptchaFailed;
        /// <summary>The platform flagged repeated autofilling while submissions remain above the speed limit.</summary>
        public event Action CaptchaFlagged;

        public bool CaptchaPending => QualityActive && S.qcCaptcha;
        public string CaptchaCode => CaptchaPending ? S.qcCaptchaCode : "";
        public int CaptchaSeed => S.qcCaptchaSeed;
        public double CaptchaSecondsLeft => CaptchaPending ? S.qcCaptchaLeft : 0;
        public double CaptchaPauseLeft => QualityActive ? S.qcAutoPause : 0;
        public double Suspicion => S.qcSuspicion;
        /// <summary>Automatic labelling cannot submit: frozen, waiting for a captcha, or paused after a failed one.</summary>
        public bool AutomationHeld => QualityFrozen || CaptchaPending || CaptchaPauseLeft > 0;
        /// <summary>Labels per second over the last minute.</summary>
        public double LabelRate { get { double n = 0; foreach (double b in S.qcSpeed) n += b; return n / SpeedWindow; } }
        /// <summary>验证码代填 needs stage 3 and a digits checkpoint of 95% or more.</summary>
        public bool CaptchaAutofillReady => S.stage >= 3 && BestAcc("mnist") >= .95 - 1e-9;

        void PrepareCaptcha()
        {
            EnsureSpeedWindow();
            for (int i = 0; i < SpeedWindow; i++) if (!FiniteCollaboration(S.qcSpeed[i]) || S.qcSpeed[i] < 0) S.qcSpeed[i] = 0;
            if (S.qcSpeedHead < 0 || S.qcSpeedHead >= SpeedWindow) S.qcSpeedHead = 0;
            S.qcSpeedClock = Clamp(S.qcSpeedClock, 0, .999999);
            S.qcSuspicion = Clamp(S.qcSuspicion, 0, 1);
            S.qcCaptchaCooldown = Clamp(S.qcCaptchaCooldown, 0, CaptchaGap);
            S.qcAutoPause = Clamp(S.qcAutoPause, 0, CaptchaPause);
            S.qcCaptchaFails = Math.Max(0, Math.Min(CaptchaFailsToReport - 1, S.qcCaptchaFails));
            S.qcCaptchaCode = S.qcCaptchaCode ?? "";
            if (S.qcCaptcha && !ValidCode(S.qcCaptchaCode)) S.qcCaptcha = false;
            // Nobody can answer while the game is closed: a waiting captcha gets a fresh clock on load.
            if (S.qcCaptcha) S.qcCaptchaLeft = CaptchaTimeout;
            else S.qcCaptchaLeft = 0;
            if (!FiniteCollaboration(S.qcAutofillIn) || S.qcAutofillIn > AutofillMaxDelay) S.qcAutofillIn = -1;
            S.qcCaptchaTotal = Math.Max(0, S.qcCaptchaTotal);
            S.qcAutofills = Math.Max(0, S.qcAutofills); S.qcAutofillFlags = Math.Max(0, Math.Min(S.qcAutofills, S.qcAutofillFlags));
        }

        void EnsureSpeedWindow()
        {
            if (S.qcSpeed != null && S.qcSpeed.Count == SpeedWindow) return;
            S.qcSpeed = new List<double>(SpeedWindow);
            for (int i = 0; i < SpeedWindow; i++) S.qcSpeed.Add(0);
            S.qcSpeedHead = 0;
        }

        static double Clamp(double v, double lo, double hi) => !FiniteCollaboration(v) ? lo : Math.Max(lo, Math.Min(hi, v));
        static bool ValidCode(string code)
        {
            if (code == null || code.Length != CaptchaLength) return false;
            foreach (char c in code) if (c < '0' || c > '9') return false;
            return true;
        }

        /// <summary>One label left the account (automatic submission or a hand answer).</summary>
        void NoteLabelSpeed()
        {
            if (!QualityActive) return;
            EnsureSpeedWindow();
            S.qcSpeed[S.qcSpeedHead] += 1;
        }

        void TickCaptcha(double dt)
        {
            if (!QualityActive) return;
            EnsureSpeedWindow();
            // Age the one-minute speed window.
            S.qcSpeedClock += dt;
            if (S.qcSpeedClock >= SpeedWindow) { for (int i = 0; i < SpeedWindow; i++) S.qcSpeed[i] = 0; S.qcSpeedClock = 0; }
            while (S.qcSpeedClock >= 1)
            {
                S.qcSpeedClock -= 1;
                S.qcSpeedHead = (S.qcSpeedHead + 1) % SpeedWindow;
                S.qcSpeed[S.qcSpeedHead] = 0;
            }
            S.qcCaptchaCooldown = Math.Max(0, S.qcCaptchaCooldown - dt);
            if (S.qcAutoPause > 0)
            {
                S.qcAutoPause = Math.Max(0, S.qcAutoPause - dt);
                if (S.qcAutoPause <= 0) Say(T("摆渡众包：自动标注恢复提交。"));
            }
            if (S.qcCaptcha)
            {
                if (S.qcAutofillIn >= 0)
                {
                    S.qcAutofillIn -= dt;
                    if (S.qcAutofillIn <= 0) { SolveCaptcha(true); return; }
                }
                // The clock waits while the game catches up on time spent closed.
                if (OfflineSimulation) return;
                S.qcCaptchaLeft -= dt;
                if (S.qcCaptchaLeft <= 0) FailCaptcha(true);
                return;
            }
            double rate = LabelRate;
            S.qcSuspicion = rate > SpeedLimit + 1e-9 ? Math.Min(1, S.qcSuspicion + dt / SuspicionFillSeconds) : Math.Max(0, S.qcSuspicion - dt / SuspicionDrainSeconds);
            if (S.qcSuspicion >= 1 - 1e-9 && S.qcCaptchaCooldown <= 0 && !OfflineSimulation && !QualityFrozen) RequireCaptcha();
        }

        void RequireCaptcha()
        {
            var code = new char[CaptchaLength];
            for (int i = 0; i < CaptchaLength; i++) code[i] = (char)('0' + (int)(QualityRoll() * 10) % 10);
            S.qcCaptchaCode = new string(code);
            S.qcCaptchaSeed = 1 + (int)(QualityRoll() * 1000000);
            S.qcCaptcha = true;
            S.qcCaptchaLeft = CaptchaTimeout;
            S.qcCaptchaCooldown = CaptchaGap;
            S.qcSuspicion = 0;
            S.qcCaptchaTotal++;
            S.qcAutofillIn = Has(CaptchaAutofillNode) ? AutofillMinDelay + (AutofillMaxDelay - AutofillMinDelay) * QualityRoll() : -1;
            Say(T("摆渡众包：检测到操作过快，请完成人机验证（30 秒内），自动标注暂停。"));
            CaptchaRequired?.Invoke();
        }

        /// <summary>The player's answer to the waiting captcha. Only digits count; anything else is ignored.</summary>
        public bool SubmitCaptcha(string answer)
        {
            if (!CaptchaPending) return false;
            var digits = new System.Text.StringBuilder();
            foreach (char c in answer ?? "") if (c >= '0' && c <= '9') digits.Append(c);
            if (digits.ToString() == S.qcCaptchaCode) { SolveCaptcha(false); return true; }
            FailCaptcha(false);
            return false;
        }

        void SolveCaptcha(bool autofilled)
        {
            S.qcCaptcha = false; S.qcCaptchaLeft = 0; S.qcAutofillIn = -1;
            S.qcCaptchaFails = 0;
            AddCredit(CaptchaCreditPass);
            if (autofilled) S.qcAutofills++;
            Say(autofilled ? T("验证码代填：已提交，自动标注继续。")
                : T("摆渡众包：验证通过，信用 +1。"));
            CaptchaSolved?.Invoke(autofilled);
            if (!autofilled || S.qcAutofills <= AutofillGracePasses || LabelRate <= SpeedLimit + 1e-9) return;
            double roll = ForcedAutofillRoll >= 0 ? ForcedAutofillRoll : QualityRoll();
            if (roll >= AutofillFlagChance) return;
            S.qcAutofillFlags++;
            AddCredit(-AutofillFlagCredit);
            Say(T("摆渡众包：多次代填后仍持续高速提交，触发自动化异常抽查，信用 −" + F(AutofillFlagCredit, "0") + "。放慢提交速度可降低风险。", "Bodu Crowdsourcing: submissions stayed above the speed limit after repeated autofills, triggering an automation review. Credit −" + F(AutofillFlagCredit, "0") + ". Slow down submissions to reduce the risk."));
            CaptchaFlagged?.Invoke();
        }

        void FailCaptcha(bool timeout)
        {
            S.qcCaptcha = false; S.qcCaptchaLeft = 0; S.qcAutofillIn = -1;
            S.qcCaptchaFails++;
            AddCredit(-CaptchaCreditFail);
            S.qcAutoPause = CaptchaPause;
            int inARow = S.qcCaptchaFails;
            Say(timeout ? T("摆渡众包：验证超时，自动标注暂停 2 分钟，信用 −3。")
                : T("摆渡众包：验证码错误，自动标注暂停 2 分钟，信用 −3。"));
            CaptchaFailed?.Invoke(timeout, inARow);
            if (inARow < CaptchaFailsToReport) return;
            S.qcCaptchaFails = 0;
            Report(XgReportReason.SuspectedBot);
        }

        /// <summary>A report supersedes any captcha business: the account is frozen anyway.</summary>
        void ClearCaptcha()
        {
            S.qcCaptcha = false; S.qcCaptchaLeft = 0; S.qcAutofillIn = -1;
            S.qcAutoPause = 0; S.qcSuspicion = 0;
        }
    }
}
