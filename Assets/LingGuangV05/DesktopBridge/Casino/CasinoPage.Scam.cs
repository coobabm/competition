using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Casino
{
    /// <summary>
    /// 888.vip 的骗局线在网页上的样子（逻辑在 XingGuang/Core/XgSim.Gambling.cs）：
    /// 「风控通知」盖住整张牌桌（交保证金 / 不玩了），网站没了以后整页换成「该网站已无法访问」。
    /// 它说的话进 YY（XgSim.AddLine），群友的吐槽和它不在 YY 时的弹窗由 CasinoDesktop 发，这里只管页面。
    /// 都是静态画面：没有闪烁、没有抖动；减少特效（S.reduceFx）时通知不等开奖动画，直接出现。
    /// </summary>
    public sealed partial class CasinoPage
    {
        RectTransform scamFrozen, scamClosed;
        TMP_Text scamBody, scamPayLabel, scamError;
        XgSim scamOwner;
        bool scamWasFrozen, scamNoticeLive;
        float scamNoticeAt;

        /// <summary>冻结或跑路：这一页不能下注。</summary>
        bool ScamBlocks => bound != null && bound.CasinoScamBlocks;

        static readonly Color NoticePaper = new Color32(250, 246, 240, 255), NoticeBar = new Color32(160, 40, 44, 255), NoticeInk = new Color32(40, 30, 30, 255);
        static readonly Color ClosedPaper = new Color32(242, 242, 242, 255), ClosedInk = new Color32(70, 70, 70, 255), ClosedMuted = new Color32(140, 140, 140, 255);

        void BuildScam()
        {
            var owner = bound;
            // 风控冻结：盖住整个牌桌、挡住下面所有点击的通知框。
            scamFrozen = Rect("Risk Notice", board, 0, 0, 1120, 660);
            PrologueDesk.Fill(scamFrozen, new Color(0, 0, 0, .72f)).raycastTarget = true;
            var box = Rect("Risk Notice Box", scamFrozen, 260, 128, 600, 394);
            PrologueDesk.Fill(box, NoticePaper);
            PrologueDesk.Fill(Rect("Risk Notice Bar", box, 0, 0, 600, 46), NoticeBar);
            Label("Risk Notice Title", box, 18, 6, 564, 34, T("【风控通知】", "[Risk control notice]"), 22, Color.white);
            scamBody = Label("Risk Notice Body", box, 24, 60, 552, 170, "", 18, NoticeInk, TextAlignmentOptions.TopLeft);
            var pay = Button("Pay Deposit", box, 24, 246, 300, 50, "", () => ResolveScam(owner, true));
            scamPayLabel = pay.GetComponentInChildren<TMP_Text>();
            Button("Quit Casino", box, 340, 246, 236, 50, T("不玩了", "I'm done"), () => ResolveScam(owner, false));
            scamError = Label("Risk Notice Error", box, 24, 302, 552, 28, "", 14, NoticeBar);
            Label("Risk Notice Fiction", box, 24, 336, 552, 24, T("（游戏内虚构网站 · 仅游戏币）", "(A fictional in-game site · game money only)"), 12, ClosedMuted, TextAlignmentOptions.Center);
            LingGuangV05.Runtime.AiContentNotice.CreateFooter(box, font, ClosedInk, 4);
            scamFrozen.gameObject.SetActive(false);

            // 跑路 / 查封：整页换掉（朴素的灰色错误页，不仿任何真实机构的样式）。
            scamClosed = Rect("Site Closed", board, 0, 0, 1120, 660);
            PrologueDesk.Fill(scamClosed, ClosedPaper).raycastTarget = true;
            Label("Closed Title", scamClosed, 0, 190, 1120, 60, T("该网站已无法访问", "This site can no longer be reached"), 40, ClosedInk, TextAlignmentOptions.Center);
            Label("Closed Body", scamClosed, 160, 270, 800, 90,
                T("888.vip 涉嫌网络赌博、诈骗，已被查封。\n如遇网络诈骗，请及时报警。", "888.vip has been shut down for illegal gambling and fraud.\nIf you have been defrauded online, report it to the police."),
                20, ClosedInk, TextAlignmentOptions.Center);
            Label("Closed Fiction", scamClosed, 0, 600, 1120, 26, T("（游戏内虚构网站）", "(A fictional in-game site)"), 13, ClosedMuted, TextAlignmentOptions.Center);
            LingGuangV05.Runtime.AiContentNotice.CreateFooter(scamClosed, font, ClosedInk, 4);
            scamClosed.gameObject.SetActive(false);
            // Rebuilt (language switch): resync on the next tick, so the inner voice is not replayed.
            scamOwner = null;
        }

        void ResolveScam(XgSim owner, bool pay)
        {
            if (owner == null || !ReferenceEquals(owner, bound) || controller == null || !ReferenceEquals(controller.Sim, owner)
                || !controller.BoundTo(controller.runtime?.Sim) || !PageVisible() || !bound.CasinoFrozenNow) return;
            if (!bound.ResolveCasinoScam(pay, controller.Host))
            {
                scamError.text = T("余额不够交保证金。", "Not enough money for the deposit.");
                return;
            }
            Sound("lose", XgJuice.Sfx.Id.Thud, .9f, .8f);
            if (pay) InnerVoice.Say("……钱交了，网站没了。", "…Paid, and the site's gone.");
            else InnerVoice.Say("……行吧，就当交学费了。", "…Fine. Call it tuition.");
            controller.runtime.MarkDirty();
            controller.SaveNow();
        }

        /// <summary>Every visible frame from Update: which layer shows, and the notice text.</summary>
        void TickScam()
        {
            if (bound == null || scamFrozen == null || scamClosed == null) return;
            bool fresh = !ReferenceEquals(scamOwner, bound);
            bool frozen = bound.CasinoFrozenNow, closed = bound.CasinoClosed;
            if (fresh) { scamOwner = bound; scamWasFrozen = frozen; scamNoticeLive = false; scamNoticeAt = 0; }
            // The freeze happening in play: the round's own reveal finishes first, then the notice (0.8 s later; at
            // once with reduced effects). Bets are already refused by the sim in the meantime. A loaded save shows it at once.
            if (frozen && !scamWasFrozen) { scamNoticeLive = true; scamNoticeAt = Time.unscaledTime + (bound.S.reduceFx ? 0 : .8f); }
            scamWasFrozen = frozen;
            bool waitReveal = scamNoticeLive && (PresentationBusy || pendingPayout != null || Time.unscaledTime < scamNoticeAt);
            // An accepted hand can still stand/hit after a loaded freeze; never hide its settlement controls.
            bool showNotice = frozen && !bound.BlackjackActive && !waitReveal;
            if (scamFrozen.gameObject.activeSelf != showNotice)
            {
                scamFrozen.gameObject.SetActive(showNotice);
                scamError.text = "";
                if (showNotice && scamNoticeLive) InnerVoice.Say("冻结……提现还要先交钱？", "Frozen… I have to pay before I can withdraw?");
                if (showNotice) scamNoticeLive = false;
            }
            if (scamClosed.gameObject.activeSelf != (closed && !bound.BlackjackActive)) scamClosed.gameObject.SetActive(closed && !bound.BlackjackActive);
            if (showNotice)
            {
                scamFrozen.SetAsLastSibling();
                long fee = bound.CasinoFee, frozenMoney = bound.S.casinoFrozen;
                scamBody.text = T("尊敬的会员：系统检测到您的账户存在异常套利行为，" + (frozenMoney > 0 ? "盈利 ¥" + Money(frozenMoney) + " 已冻结。" : "提现通道已暂停。")
                        + "\n\n缴纳保证金 ¥" + Money(fee) + " 即可解冻，24 小时内连同盈利一起提现。",
                    "Dear member: unusual arbitrage was detected on your account; " + (frozenMoney > 0 ? "winnings of ¥" + Money(frozenMoney) + " are frozen." : "withdrawals are suspended.")
                        + "\n\nPay a deposit of ¥" + Money(fee) + " to unfreeze, and withdraw everything within 24 hours.");
                scamPayLabel.text = T("缴纳保证金 ¥", "Pay deposit ¥") + Money(fee);
            }
            if (closed) scamClosed.SetAsLastSibling();
        }
    }
}
