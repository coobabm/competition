using System.Globalization;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Relays the lab's 摆渡众包 quality-control events to the desktop's tray popups (deep-blue platform theme):
    /// warnings, reports, unfreezes and captchas right away, fines (and the trap items among them) batched into at
    /// most one popup every 30 seconds.
    /// Lives on the controller so it keeps working while the 灵光 window is closed. Presentation only.
    /// </summary>
    public sealed class XgPlatformPopups : MonoBehaviour
    {
        public const float FineInterval = 30;
        XingGuangController controller;
        XgSim bound;
        int pendingFines, pendingTraps;
        double pendingMoney;
        float sinceFinePopup = FineInterval;

        static string T(string zh, string en) => GameText.T(zh, en);
        static string Who => T("摆渡众包", "Bodu Crowdsourcing");
        static string Yuan(double v) => "¥" + v.ToString(v >= 10 ? "0" : "0.##", CultureInfo.InvariantCulture);
        static void Popup(string text, float seconds) { PrologueDirector.Desk?.StoryPopup(Who, text, seconds); }

        public void Bind(XingGuangController owner) { controller = owner; }

        void Update()
        {
            if (controller == null || controller.Sim == null) return;
            if (!ReferenceEquals(bound, controller.Sim)) Rebind(controller.Sim);
            sinceFinePopup += Time.unscaledDeltaTime;
            if (pendingFines > 0 && sinceFinePopup >= FineInterval) FlushFines();
        }

        void Rebind(XgSim sim)
        {
            Unsubscribe();
            bound = sim;
            pendingFines = 0; pendingTraps = 0; pendingMoney = 0; sinceFinePopup = FineInterval;
            bound.QualityFined += OnFined;
            bound.CaptchaRequired += OnCaptcha;
            bound.CaptchaSolved += OnCaptchaSolved;
            bound.CaptchaFailed += OnCaptchaFailed;
            bound.CaptchaFlagged += OnCaptchaFlagged;
            bound.QualityWarned += OnWarned;
            bound.QualityReported += OnReported;
            bound.QualityUnfrozen += OnUnfrozen;
        }

        void Unsubscribe()
        {
            if (bound == null) return;
            bound.QualityFined -= OnFined; bound.QualityWarned -= OnWarned;
            bound.QualityReported -= OnReported; bound.QualityUnfrozen -= OnUnfrozen;
            bound.CaptchaRequired -= OnCaptcha; bound.CaptchaSolved -= OnCaptchaSolved;
            bound.CaptchaFailed -= OnCaptchaFailed; bound.CaptchaFlagged -= OnCaptchaFlagged;
        }

        void OnDestroy() { Unsubscribe(); }

        void OnFined(XgQcFine fine)
        {
            if (fine == null) return;
            pendingFines++; pendingMoney += fine.fine;
            if (fine.trap) pendingTraps++;
        }

        void FlushFines()
        {
            string money = pendingMoney > 0 ? Lang.T("，扣款 ") + Yuan(pendingMoney) + T("", " deducted") : Lang.T("，余额不足未扣款");
            // 金标题: the platform says so, and the forum thread about them is open from now on.
            string traps = pendingTraps > 0 ? T("。其中 " + pendingTraps + " 条是平台预置的金标题", ". " + pendingTraps + " of them " + (pendingTraps == 1 ? "was a" : "were") + " known-answer trap item" + (pendingTraps == 1 ? "" : "s") + " planted by the platform") : "";
            Popup(T("抽检不合格 " + pendingFines + " 条", pendingFines + (pendingFines == 1 ? " label" : " labels") + " failed spot checks") + money + traps + T("。", "."), pendingTraps > 0 ? 9 : 6);
            pendingFines = 0; pendingTraps = 0; pendingMoney = 0; sinceFinePopup = 0;
        }

        void OnWarned(double errorRate)
        {
            string pass = ((1 - errorRate) * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
            string line = ((1 - XgSim.QcReportRate) * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
            Popup(T("近期抽检合格率 " + pass + "，低于 " + line + " 将被举报。\n建议调高「拿不准才问我」阈值。",
                "Recent spot-check pass rate " + pass + ". Below " + line + " the account will be reported.\nTry raising the 'Ask when unsure' threshold."), 9);
        }

        void OnReported(XgReportReason reason, double seconds)
        {
            if (pendingFines > 0) FlushFines();
            string minutes = (seconds / 60).ToString("0", CultureInfo.InvariantCulture);
            Popup(T("您的账号被举报：" + XgSim.ReportReasonText(reason, false) + "。\n自动标注冻结 " + minutes + " 分钟，可在标注台申诉。",
                "Your account was reported: " + XgSim.ReportReasonText(reason, true) + ".\nAuto labelling is frozen for " + minutes + " min; appeal on the labelling page."), 10);
        }

        void OnCaptcha()
        {
            Popup(bound != null && bound.Has(XgSim.CaptchaAutofillNode)
                ? Lang.T("检测到操作过快，需要人机验证。验证码代填已接手。")
                : T("检测到操作过快，请在 30 秒内完成人机验证：打开摆渡众包的标注台输入验证码。自动标注已暂停。", "Unusually fast activity. Complete the captcha within 30 s: open the labelling page in Bodu Crowd. Auto labelling is paused."), 10);
        }

        void OnCaptchaSolved(bool autofilled)
        {
            if (autofilled) Popup(Lang.T("验证码代填：已提交，验证通过。"), 4);
        }

        void OnCaptchaFailed(bool timeout, int inARow)
        {
            string left = (XgSim.CaptchaFailsToReport - inARow).ToString(CultureInfo.InvariantCulture);
            Popup((timeout ? Lang.T("人机验证超时") : Lang.T("验证码错误")) + Lang.T("，自动标注暂停 2 分钟，信用 −3。")
                + (inARow < XgSim.CaptchaFailsToReport ? T("\n再失败 " + left + " 次将按疑似机器操作处理。", "\n" + left + " more in a row and the account is treated as a bot.") : ""), 9);
        }

        void OnCaptchaFlagged()
        {
            Popup(LingGuangV05.Runtime.GameText.T("持续高速提交且多次自动代填，本次被标记，信用 −10。", "Repeated autofill during high-rate submissions was flagged. Credit −10."), 8);
        }

        void OnUnfrozen(bool appealed)
        {
            Popup(appealed ? Lang.T("申诉通过，账号已解冻。") : Lang.T("冻结期满，账号已解冻，自动标注恢复。"), 6);
        }
    }
}
