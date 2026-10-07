using System;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.Zhongbao
{
    /// <summary>Layout helpers shared by 摆渡众包's own pages (分包, 结算, 信用).</summary>
    static class ZhongbaoUi
    {
        /// <summary>A vertical scroll list filling the given anchors of <paramref name="parent"/>; returns the content rect.</summary>
        public static RectTransform Scroll(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var viewport = Rect(name, parent, min, max, offMin, offMax);
            Panel(viewport, new Color(1, 1, 1, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            var list = Rect("List", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            list.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = list; scroll.viewport = viewport; scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            return list;
        }

        /// <summary>A scrolling rich-text pane: the text grows with its content and the pane scrolls over it.</summary>
        public static TMP_Text TextPane(XgUi ui, RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var list = Scroll(parent, name, min, max, offMin, offMax);
            var label = ui.Text(list, "", 15, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            label.margin = new Vector4(4, 4, 8, 8);
            label.lineSpacing = 6;
            var fitter = list.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return label;
        }

        public static string Yuan(double v) => "¥" + Money(v);
        public static string Muted(string text) => "<color=#68748C>" + text + "</color>";
        public static string Head(string zh, string en) => "<size=18><b>" + T(zh, en) + "</b></size>\n";
    }

    /// <summary>
    /// 分包: the 网吧 friends who label under your account (XgSim.Subcontract.cs), moved here from the bottom of the
    /// old 订单 page. The rows are the same <see cref="XgMarketSection"/> as before, showing only its 转包 part.
    /// </summary>
    public sealed class ZhongbaoSubcontractPage : XgPage
    {
        RectTransform list;
        TMP_Text header, locked;
        XgMarketSection market;

        public override void Build(RectTransform area)
        {
            root = ui.Card(area, "subcontract", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            header = ui.Text(Strip("Header", root, 8, 56, 16, 16), "", 15, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            list = ZhongbaoUi.Scroll(root, "Viewport", Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -70));
            market = new XgMarketSection(host, ui, list);
            locked = ui.Text(Rect("Locked", root, Vector2.zero, Vector2.one, new Vector2(60, 60), new Vector2(-60, -80)), "", 20, XgPalette.Muted, TextAlignmentOptions.Center);
        }

        public override void Refresh()
        {
            if (root == null || Sim == null) return;
            header.text = T("<b>分包</b>  网吧兄弟用你的摆渡众包账号标注，按分钟发工资。平台照样抽检他们：罚款、扣信用、举报都算在你头上；没抽到的错题会变成训练数据里的噪声。",
                "<b>Subcontract</b>  Netbar friends label under your Bodu Crowd account for wages by the minute. The platform spot-checks them too: fines, lost credit and reports land on you, and unchecked mistakes become noise in your data.");
            bool open = Sim.SubcontractUnlocked;
            locked.gameObject.SetActive(!open);
            if (!open)
                locked.text = Sim.SubcontractReady
                    ? T("阿杰马上会在 YY 群里问要不要帮你标……", "Ajie is about to ask in the YY group whether you need hands…")
                    : T("还没人来接活。\n\n第 " + XgSim.SubcontractStage + " 阶段起、买下自动答题以后，网吧的阿杰会在 YY 群里问要不要帮你标。",
                        "Nobody has asked for work yet.\n\nFrom stage " + XgSim.SubcontractStage + ", once you own auto-answer, Ajie from the netbar asks in the YY group.");
            list.sizeDelta = new Vector2(0, market.Refresh(0));
        }
    }

    /// <summary>
    /// 结算: a read-only account of what the sim already knows. The lab keeps running totals (income, spending, fines,
    /// spot checks, wages), the last few automatic submissions and the live per-second rates, but no history of
    /// single payments; this session's platform notices come from <see cref="ZhongbaoHub"/> and are not saved.
    /// </summary>
    public sealed class ZhongbaoLedgerPage : XgPage
    {
        readonly ZhongbaoHub hub;
        TMP_Text header, left, right;

        public ZhongbaoLedgerPage(ZhongbaoHub hub) { this.hub = hub; }

        public override void Build(RectTransform area)
        {
            root = ui.Card(area, "ledger", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            header = ui.Text(Strip("Header", root, 8, 40, 16, 16), "", 15, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            left = ZhongbaoUi.TextPane(ui, root, "Now", Vector2.zero, new Vector2(.5f, 1), new Vector2(14, 10), new Vector2(-8, -54));
            right = ZhongbaoUi.TextPane(ui, root, "Recent", new Vector2(.5f, 0), Vector2.one, new Vector2(8, 10), new Vector2(-14, -54));
        }

        static string Line(string label, string value) => label + "  <b>" + value + "</b>\n";

        public override void Refresh()
        {
            if (root == null || Sim == null) return;
            var s = Sim.S;
            header.text = T("<b>结算</b>  " + ZhongbaoUi.Muted("平台只记累计数和最近几条自动提交，不保存逐笔流水。"),
                "<b>Ledger</b>  " + ZhongbaoUi.Muted("The platform keeps running totals and the last few automatic submissions, not every single payment."));

            // ── now, per second ──
            var sb = new StringBuilder();
            sb.Append(ZhongbaoUi.Head("现在每秒", "Per second now"));
            double contracts = Sim.IncomePerSecond;
            sb.Append(Line(T("企业订单", "Business orders"), "+" + ZhongbaoUi.Yuan(contracts) + T("/秒", "/s")));
            foreach (var c in XgCatalog.Contracts)
                if (Sim.Signed(c.id))
                    sb.Append("    ").Append(ZhongbaoUi.Muted(T(c.client, c.clientEn) + " · " + T(c.job, c.jobEn) + "  ¥" + Money(Sim.ContractIncome(c)) + T("/秒", "/s"))).Append("\n");
            double auto = 0;
            foreach (var l in s.autoLevels) auto += Sim.AutoIncome(l.dataset, Host);
            sb.Append(Line(T("自动答题", "Auto-answer"), "+" + ZhongbaoUi.Yuan(auto) + T("/秒", "/s") + (Sim.QualityFrozen ? T("（账号冻结中，暂停）", " (account frozen, paused)") : "")));
            double wages = Sim.WagesPerMinute;
            if (wages > 0) sb.Append(Line(T("分包工资", "Subcontract wages"), "−" + ZhongbaoUi.Yuan(wages) + T("/分钟", "/min")));
            sb.Append(Line(T("合计约", "About"), ZhongbaoUi.Yuan(contracts + auto - wages / XgSim.WageSeconds) + T("/秒", "/s")));
            sb.Append(ZhongbaoUi.Muted(T("电费、显卡这些家里的开销在「家庭」。", "Power, cards and other household costs are in Home."))).Append("\n\n");

            // ── running totals ──
            sb.Append(ZhongbaoUi.Head("存档里记下的累计", "Totals kept in the save"));
            sb.Append(Line(T("实验室总收入", "Lab income"), ZhongbaoUi.Yuan(s.totalIncome)));
            sb.Append(Line(T("实验室总支出", "Lab spending"), ZhongbaoUi.Yuan(s.totalSpent)));
            double bonuses = 0; int signed = 0;
            foreach (var c in XgCatalog.Contracts) if (Sim.Signed(c.id)) { bonuses += c.signBonus; signed++; }
            sb.Append(Line(T("签约首付", "Signing advances"), ZhongbaoUi.Yuan(bonuses) + T("（" + signed + " 单）", " (" + signed + " signed)")));
            sb.Append(Line(T("抽检罚款", "Spot-check fines"), "−" + ZhongbaoUi.Yuan(s.qcFines)));
            sb.Append("    ").Append(ZhongbaoUi.Muted(T("抽检 " + s.qcCheckedTotal + " 次 · 不合格 " + s.qcFailedTotal + " 次 · 其中金标题 " + s.qcTrapFails + " 次 · 举报 " + s.qcReports + " 次",
                s.qcCheckedTotal + " checks · " + s.qcFailedTotal + " failed · " + s.qcTrapFails + " on trap items · " + s.qcReports + " reports"))).Append("\n");
            if (s.scUnlocked)
                foreach (var info in XgSim.Workers)
                {
                    var w = Sim.Worker(info.id);
                    if (w == null || w.labelsTotal == 0 && w.wagesPaid <= 0) continue;
                    sb.Append(Line(T("分包 · " + info.name, "Subcontract · " + info.nameEn), T("工资 −¥" + Money(w.wagesPaid) + " · 挣 +¥" + Money(w.earned) + " · 罚 −¥" + Money(w.fines),
                        "wages −¥" + Money(w.wagesPaid) + " · earned +¥" + Money(w.earned) + " · fined −¥" + Money(w.fines))));
                }
            left.text = sb.ToString();

            // ── recent ──
            sb.Length = 0;
            sb.Append(ZhongbaoUi.Head("最近的自动提交", "Latest automatic submissions"));
            if (s.autoFeed.Count == 0) sb.Append(ZhongbaoUi.Muted(T("还没有。买下自动答题以后，模型交出去的题会出现在这里。", "None yet. Once you own auto-answer, the labels the model hands in show here."))).Append("\n");
            for (int i = s.autoFeed.Count - 1; i >= 0; i--)
            {
                var r = s.autoFeed[i];
                var desk = XgCatalog.Desk(r.dataset);
                string name = desk != null ? T(desk.name, desk.nameEn) : r.dataset;
                if (r.audited) sb.Append("<color=#8C540A>↖ ").Append(name).Append(T("  审核拦下，回到待复核", "  caught by the audit, back to review")).Append("</color>\n");
                else if (r.spotChecked && !r.correct) sb.Append("<color=#BE1414><b>× ").Append(name).Append(T("  抽检不合格 −¥", "  failed spot check −¥")).Append(N(r.fine, "0.##")).Append("</b></color>\n");
                else if (r.correct) sb.Append("<color=#2F9E44>✓ ").Append(name).Append("  +¥").Append(N(r.pay, "0.##")).Append(r.spotChecked ? T("（抽检合格）", " (spot-checked, passed)") : "").Append("</color>\n");
                else sb.Append("<color=#D63031>× ").Append(name).Append(T("  错了，没被抽到（成了噪声）", "  wrong, not checked (now noise)")).Append("</color>\n");
            }
            sb.Append("\n").Append(ZhongbaoUi.Head("这次开机以来的平台通知", "Platform notices since the game started"));
            var notices = hub != null ? hub.Notices : null;
            if (notices == null || notices.Count == 0) sb.Append(ZhongbaoUi.Muted(T("暂无。", "None yet."))).Append("\n");
            else foreach (var n in notices) sb.Append(ZhongbaoUi.Muted(n.clock)).Append("  ").Append(T(n.zh, n.en)).Append("\n");
            if (hub != null && hub.SessionFines > 0) sb.Append(Line(T("本次罚款合计", "Fines this session"), "−" + ZhongbaoUi.Yuan(hub.SessionFines)));
            sb.Append(ZhongbaoUi.Muted(T("（这一栏不存档，重新开游戏会清空。）", "(Not saved: this list starts empty each time the game starts.)")));
            right.text = sb.ToString();
        }
    }

    /// <summary>
    /// 信用: the platform's credit score and the rules that move it, read from XgSim.Quality / QualityTraps / Captcha,
    /// plus the data noise behind 近亲繁殖 (XgSim.Inbreeding), which does not
    /// touch credit but is the other cost of bad labels. Numbers come from the sim's own constants.
    /// </summary>
    public sealed class ZhongbaoCreditPage : XgPage
    {
        TMP_Text score, status, rules;
        RectTransform fill;

        public override void Build(RectTransform area)
        {
            root = ui.Card(area, "credit", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var top = Rect("Score", root, new Vector2(0, 1), new Vector2(.5f, 1), new Vector2(14, -110), new Vector2(-8, -10));
            Panel(top, new Color32(232, 236, 255, 255)).raycastTarget = false;
            score = ui.Text(Rect("Text", top, Vector2.zero, Vector2.one, new Vector2(14, 20), new Vector2(-14, -6)), "", 30, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            var bar = Rect("Bar", top, Vector2.zero, new Vector2(1, 0), new Vector2(14, 10), new Vector2(-14, 18));
            fill = Bar(bar, "Fill", XgPalette.Line, ZhongbaoView.Blue);
            status = ZhongbaoUi.TextPane(ui, root, "Status", Vector2.zero, new Vector2(.5f, 1), new Vector2(14, 10), new Vector2(-8, -120));
            rules = ZhongbaoUi.TextPane(ui, root, "Rules", new Vector2(.5f, 0), Vector2.one, new Vector2(8, 10), new Vector2(-14, -10));
        }

        static string Pct(double v) => N(v * 100, "0") + "%";
        static string Signed(double v) => (v >= 0 ? "+" : "−") + N(Math.Abs(v), "0.#");

        public override void Refresh()
        {
            if (root == null || Sim == null) return;
            var s = Sim.S;
            var tier = Sim.CreditTier;
            string tierName = Sim.CreditTierName(tier);
            string color = tier == XgCreditTier.Gold ? "#966200" : tier == XgCreditTier.Normal ? "#2932E1" : "#C82424";
            score.text = "<b>" + T("信用 ", "Credit ") + "<color=" + color + ">" + N(Math.Floor(Sim.Credit), "0") + "</color></b><size=18> / " + N(XgSim.QcCreditMax, "0") + "  <color=" + color + ">" + tierName + "</color>"
                + (Sim.QualityActive ? "" : "  " + ZhongbaoUi.Muted(T("（未开通）", "(inactive)"))) + "</size>";
            SetBar(fill, (float)(Sim.Credit / XgSim.QcCreditMax));
            fill.GetComponent<Image>().color = tier == XgCreditTier.Gold ? XgPalette.Gold : tier == XgCreditTier.Normal ? (Color)ZhongbaoView.Blue : XgPalette.Bad;

            // ── where the account stands ──
            var sb = new StringBuilder();
            sb.Append(ZhongbaoUi.Head("账号状态", "Account"));
            if (!Sim.QualityActive)
                sb.Append(T("平台还没开始抽检：买下自动答题以后，模型交出去的题才会被抽检、计信用。手动标注从不抽检。",
                    "The platform is not checking yet: it starts spot-checking (and scoring credit) once you own auto-answer. Hand labels are never checked.")).Append("\n\n");
            sb.Append(T("抽检率 ", "Spot-check rate ")).Append("<b>").Append(Pct(Sim.QualityActive ? Sim.SpotCheckChance : XgSim.CheckChanceOf(tier))).Append("</b>")
              .Append(T(" · 自动收入 ×", " · auto income ×")).Append("<b>").Append(N(XgSim.PayMultiplierOf(tier), "0.0#")).Append("</b>\n");
            string rate = Sim.SpotChecks == 0 ? T("还没有抽检", "no checks yet")
                : T("合格率 " + Pct(Sim.PassRate) + "（近 " + Sim.SpotChecks + " 次）", "pass rate " + Pct(Sim.PassRate) + " (last " + Sim.SpotChecks + ")");
            sb.Append(Sim.QualityWarning ? "<color=#D63031>" + rate + T(" · 已警告", " · warned") + "</color>" : rate);
            if (Sim.SpotChecks > 0 && !Sim.PassRateJudged) sb.Append(ZhongbaoUi.Muted(T("  满 " + XgSim.QcMinChecks + " 次才评定", "  judged from " + XgSim.QcMinChecks)));
            sb.Append("\n");
            if (Sim.QualityFrozen) sb.Append("<color=#D63031><b>").Append(T("账号冻结中 ", "Frozen ")).Append(XgSim.FreezeClock(Sim.FreezeSecondsLeft)).Append("</b> · ").Append(Sim.ReportReasonText(Sim.LastReportReason)).Append("</color>\n");
            if (Sim.CaptchaPending) sb.Append("<color=#D63031><b>").Append(T("等待人机验证 ", "Captcha waiting ")).Append(XgSim.FreezeClock(Sim.CaptchaSecondsLeft)).Append("</b>").Append(T("（在标注台输入）", " (enter it on the labelling desk)")).Append("</color>\n");
            else if (Sim.CaptchaPauseLeft > 0) sb.Append("<color=#B36A00>").Append(T("验证未通过，自动标注暂停 ", "Captcha failed, auto labelling paused ")).Append(XgSim.FreezeClock(Sim.CaptchaPauseLeft)).Append("</color>\n");
            if (Sim.MonotoneAnswers) sb.Append("<color=#B36A00>").Append(T("最近的自动答案几乎全是同一个：像脚本。", "Recent automatic answers are nearly all the same: it looks like a script.")).Append("</color>\n");
            sb.Append(T("已被举报 ", "Reports so far ")).Append(s.qcReports).Append(T(" 次 · 下次冻结 ", " · next freeze ")).Append(N(XgSim.FreezeSecondsFor(s.qcReports + 1) / 60, "0")).Append(T(" 分钟", " min")).Append("\n");

            sb.Append(ZhongbaoUi.Head("累计", "Lifetime"));
            sb.Append(T("抽检 " + s.qcCheckedTotal + " 次，不合格 " + s.qcFailedTotal + " 次（金标题 " + s.qcTrapFails + " 次）· 罚款 ¥" + Money(s.qcFines),
                s.qcCheckedTotal + " spot checks, " + s.qcFailedTotal + " failed (" + s.qcTrapFails + " trap items) · fines ¥" + Money(s.qcFines))).Append("\n");
            sb.Append(T("人机验证 " + s.qcCaptchaTotal + " 次 · 代填 " + s.qcAutofills + " 次，被识破 " + s.qcAutofillFlags + " 次",
                s.qcCaptchaTotal + " captchas · " + s.qcAutofills + " autofilled, " + s.qcAutofillFlags + " spotted")).Append("\n");
            if (Sim.TrapsRemembered > 0) sb.Append(T("记住的金标题 ", "Trap items remembered ")).Append(Sim.TrapsRemembered).Append("\n");
            sb.Append("\n");

            sb.Append(ZhongbaoUi.Head("数据噪声（不扣信用，伤模型）", "Data noise (no credit cost, hurts the model)"));
            bool any = false;
            foreach (var d in Sim.OpenDesks())
            {
                double share = Sim.NoiseShare(d.id);
                if (share <= 0) continue;
                any = true;
                double penalty = Sim.InbreedingPenaltyFor(d.id);
                sb.Append(T(d.name, d.nameEn)).Append("  ").Append(T("噪声 ", "noise ")).Append(Pct(share))
                  .Append(penalty > 0 ? "  <color=#D63031>" + T("近亲繁殖 −", "inbreeding −") + Pct(penalty) + "</color>" : "").Append("\n");
            }
            if (!any) sb.Append(ZhongbaoUi.Muted(T("没有记下的错标。", "No wrong labels recorded."))).Append("\n");
            sb.Append(ZhongbaoUi.Muted(T("没被抽到的错题会混进训练数据；超过样本的 " + Pct(XgSim.InbreedingFreeShare) + " 模型就会学回自己的错。标注台的「人工复核旧数据」能清掉它们。",
                "Wrong labels nobody checked go into the training data; above " + Pct(XgSim.InbreedingFreeShare) + " of the samples the model learns its own mistakes back. Re-check old labels on the labelling desk to clear them.")));
            status.text = sb.ToString();

            // ── the rules ──
            sb.Length = 0;
            sb.Append(ZhongbaoUi.Head("信用档位", "Tiers"));
            sb.Append(Tier(XgCreditTier.Gold, "≥ " + N(XgSim.QcCreditGold, "0"), tier));
            sb.Append(Tier(XgCreditTier.Normal, N(XgSim.QcCreditWatch, "0") + "–" + N(XgSim.QcCreditGold - 1, "0"), tier));
            sb.Append(Tier(XgCreditTier.Watch, "< " + N(XgSim.QcCreditWatch, "0"), tier));
            sb.Append("\n").Append(ZhongbaoUi.Head("什么会让信用变", "What moves credit"));
            sb.Append(Rule(XgSim.QcCreditPass, "抽检合格", "A spot check passes"));
            sb.Append(Rule(-XgSim.QcCreditFail, "抽检不合格（罚该题报酬 ×" + N(XgSim.QcFineMultiplier, "0") + "）", "A spot check fails (fine: the card's pay ×" + N(XgSim.QcFineMultiplier, "0") + ")"));
            sb.Append(Rule(-XgSim.TrapCreditFail, "金标题答错（平台预置的已知答案题，罚 ×" + N(XgSim.TrapFineMultiplier, "0") + "，在抽检记录里算两次）", "A trap item is wrong (the platform knew the answer; fine ×" + N(XgSim.TrapFineMultiplier, "0") + ", counts twice in the window)"));
            sb.Append(Rule(-XgSim.QcCreditReport, "被举报（冻结 " + N(XgSim.QcFreezeSeconds[0] / 60, "0") + "/" + N(XgSim.QcFreezeSeconds[1] / 60, "0") + "/" + N(XgSim.QcFreezeSeconds[2] / 60, "0") + " 分钟）", "Reported (frozen " + N(XgSim.QcFreezeSeconds[0] / 60, "0") + "/" + N(XgSim.QcFreezeSeconds[1] / 60, "0") + "/" + N(XgSim.QcFreezeSeconds[2] / 60, "0") + " min)"));
            sb.Append(Rule(XgSim.QcCreditHand, "冻结期间每手动标对一条", "Each right hand label while frozen"));
            sb.Append(Rule(XgSim.CaptchaCreditPass, "人机验证通过", "A captcha passed"));
            sb.Append(Rule(-XgSim.CaptchaCreditFail, "验证码答错或超时（自动标注暂停 " + N(XgSim.CaptchaPause / 60, "0") + " 分钟；连错 " + XgSim.CaptchaFailsToReport + " 次按机器处理）", "A captcha wrong or late (auto labelling pauses " + N(XgSim.CaptchaPause / 60, "0") + " min; " + XgSim.CaptchaFailsToReport + " in a row counts as a bot)"));
            sb.Append(Rule(-XgSim.AutofillFlagCredit, "验证码代填被识破（每次 " + Pct(XgSim.AutofillFlagChance) + " 的可能）", "Captcha autofill spotted (" + Pct(XgSim.AutofillFlagChance) + " chance each time)"));
            sb.Append("\n").Append(ZhongbaoUi.Head("举报规则", "When the account is reported"));
            sb.Append(T("平台每 " + XgSim.QcJudgeEvery + " 次抽检评一次，看最近 " + XgSim.QcWindow + " 次（满 " + XgSim.QcMinChecks + " 次才评）：\n"
                    + "· 错误率 ≥ " + Pct(XgSim.QcWarnRate) + " 警告；\n"
                    + "· 满 " + XgSim.QcWindow + " 次且错误率 ≥ " + Pct(XgSim.QcReportRate) + " 举报；\n"
                    + "· 答案 " + Pct(XgSim.QcMonotoneShare) + " 以上是同一个，像脚本：错误率到 " + Pct(XgSim.QcWarnRate) + " 就举报。\n"
                    + "冻结期间自动标注不能提交；申诉付 ¥" + N(XgSim.QcAppealMin, "0") + " 或余额的 " + Pct(XgSim.QcAppealShare) + "（取较多者），信用不变。",
                "The platform judges every " + XgSim.QcJudgeEvery + " checks over the last " + XgSim.QcWindow + " (once there are " + XgSim.QcMinChecks + "):\n"
                    + "· an error rate of " + Pct(XgSim.QcWarnRate) + " or more warns;\n"
                    + "· with a full " + XgSim.QcWindow + " checks, " + Pct(XgSim.QcReportRate) + " or more reports the account;\n"
                    + "· when " + Pct(XgSim.QcMonotoneShare) + " of the answers are the same it looks like a script, and " + Pct(XgSim.QcWarnRate) + " is enough.\n"
                    + "While frozen, auto labels cannot be submitted. An appeal costs ¥" + N(XgSim.QcAppealMin, "0") + " or " + Pct(XgSim.QcAppealShare) + " of your money (whichever is more); credit stays the same."));
            rules.text = sb.ToString();
        }

        string Tier(XgCreditTier t, string range, XgCreditTier current)
        {
            string line = Sim.CreditTierName(t) + "  " + range + T("  · 抽检 ", "  · checked ") + Pct(XgSim.CheckChanceOf(t)) + T(" · 自动收入 ×", " · auto income ×") + N(XgSim.PayMultiplierOf(t), "0.0#");
            return (t == current ? "<b>▶ " + line + "</b>" : "    " + line) + "\n";
        }

        static string Rule(double delta, string zh, string en)
            => "<color=" + (delta >= 0 ? "#2F9E44" : "#D63031") + "><b>" + Signed(delta) + "</b></color>  " + T(zh, en) + "\n";
    }
}
