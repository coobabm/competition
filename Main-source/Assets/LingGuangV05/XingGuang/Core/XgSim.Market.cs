using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>A text desk whose question bank picked up a new meme the checkpoint never saw (新题型).</summary>
    [Serializable]
    public sealed class XgMemeDrift
    {
        public string dataset = "", meme = "";
        /// <summary>The month the meme arrived (yyyymm).</summary>
        public int month;
        /// <summary>Accuracy points the checkpoint loses on this desk (0–15).</summary>
        public double points;
    }

    /// <summary>A client that comes back some time after its last high-paying contract ended.</summary>
    [Serializable]
    public sealed class XgMarketTimer
    {
        public string id = "";
        public double seconds;
    }

    /// <summary>A 甲方高价单 (SLA contract): about twice a normal contract's rate, held back until a quality clause is judged.</summary>
    public sealed class XgSlaOffer
    {
        public string id, client, clientEn, job, jobEn, dataset;
        /// <summary>The normal contract on the same desk: the SLA pays <see cref="XgSim.SlaIncomeFactor"/> × its base rate and shares its bar.</summary>
        public string baseContract;
        /// <summary>Contract length in game seconds (8–15 game minutes).</summary>
        public double duration;
        public double Threshold => XgCatalog.Contract(baseContract).threshold;
        public double Income => XgSim.SlaIncomeFactor * XgCatalog.Contract(baseContract).income;
    }

    public enum XgSlaOutcome { FiveStar = 0, Docked = 1, Unjudged = 2, Cancelled = 3 }

    /// <summary>How an SLA contract ended: what was paid out of the 尾款 and what was lost.</summary>
    public sealed class XgSlaResult
    {
        public string id = "";
        public XgSlaOutcome outcome;
        public double paid, forfeited, earned;
        public int checks, passed;
        public double PassRate => checks == 0 ? 1 : (double)passed / checks;
    }

    /// <summary>A month's new meme reached the text desks.</summary>
    public sealed class XgMemeDriftNotice
    {
        public int month;
        public string meme = "", memeEn = "";
        public List<string> desks = new List<string>();
    }

    public sealed partial class XgState
    {
        /// <summary>0 for saves from before the market systems; repair sets the drift baseline once.</summary>
        public int mkVersion;
        /// <summary>The last month (yyyymm) whose meme reached the desks; -1 = take the current month as the baseline without a drift.</summary>
        public int mkDriftMonth;
        public List<XgMemeDrift> mkDrift = new List<XgMemeDrift>();

        /// <summary>The running 甲方高价单 ("" = none) and its books.</summary>
        public string slaId = "";
        public double slaLeft, slaHeld, slaEarned;
        public int slaChecks, slaPassed;
        /// <summary>Clients waiting before they offer again.</summary>
        public List<XgMarketTimer> slaCooldowns = new List<XgMarketTimer>();
        public int slaFiveStars, slaDocked, slaCancelled;
    }

    /// <summary>
    /// Two 摆渡众包 market systems on top of the platform's quality control (XgSim.Quality.cs):
    ///
    /// 新题型 (monthly meme drift). 2016 slang moves every month. When the month's meme first appears (its date in
    /// <see cref="XgMemes.HotWords"/> or <see cref="XgMemes.Topics"/>), every open meme desk (弹幕, 短信, 标题党, 刷单评论,
    /// 表情包) gets a new kind of card. The deployed checkpoint has never seen it, so its automatic and ghost judgments
    /// on that desk lose 15 accuracy points (<see cref="CardAccuracy"/>); hand labelling is unaffected. Each new correct
    /// human label on the desk recovers 0.5 points and each training epoch on that dataset recovers 1 point. A drifted
    /// desk fails more spot checks, which is intended: it pushes the player back to hand labelling or retraining.
    ///
    /// 甲方高价单 (SLA contracts). Clients offer about twice a normal contract's rate for 8–15 game minutes with a quality
    /// clause: at least 95% of the platform's spot checks on that desk must pass. Half of the income is held as 尾款.
    /// A report cancels the contract (the 尾款 is forfeited, credit −5). At the end the 尾款 is paid in full on a pass
    /// (or with fewer than 10 checks), and only half of it below the clause.
    /// </summary>
    public sealed partial class XgSim
    {
        public const double MemeDriftPoints = 15, MemeDriftHandRecovery = .5, MemeDriftEpochRecovery = 1;
        public const double SlaIncomeFactor = 2, SlaHeldShare = .5, SlaClause = .95, SlaDockedShare = .5, SlaCancelCredit = 5, SlaMinCredit = 85;
        public const int SlaMinChecks = 10;
        public const double SlaCooldownSeconds = 600;

        /// <summary>Desks whose cards are 2016 internet slang. Digits, poems, logic, captcha, Go and Chinglish never drift.</summary>
        public static readonly string[] MemeDriftDesks = { "danmu", "spam", "headline", "review", "meme" };

        /// <summary>
        /// One meme per month, all already in the 2016 lexicon (XgMemes): the month a word appears in is the month of its
        /// date there. English glosses for the en build.
        /// </summary>
        public static readonly (string meme, string en)[] MonthMemes =
        {
            ("为了部落", "For the Horde"), ("葛优躺", "the Ge You slouch"), ("洪荒之力", "primordial power"), ("非酋", "gacha-cursed"),
            ("蓝瘦香菇", "so sad, want to cry"), ("厉害了我的哥", "amazing, bro"), ("你的名字", "Your Name"),
        };

        public static readonly XgSlaOffer[] SlaOffers =
        {
            new XgSlaOffer { id = "sla.danmu", client = "某视频网站弹幕组", clientEn = "A video site's danmaku team", job = "弹幕实时审核", jobEn = "Live danmaku moderation", dataset = "danmu", baseContract = "danmaku", duration = 480 },
            new XgSlaOffer { id = "sla.meme", client = "某表情包 App", clientEn = "A sticker app", job = "表情包上架审核", jobEn = "Sticker upload review", dataset = "meme", baseContract = "memetag", duration = 540 },
            new XgSlaOffer { id = "sla.takeout", client = "某外卖平台", clientEn = "A food-delivery app", job = "刷单好评识别", jobEn = "Fake five-star review detection", dataset = "review", baseContract = "fakereview", duration = 600 },
            new XgSlaOffer { id = "sla.portal", client = "某门户新闻客户端", clientEn = "A portal's news app", job = "标题党审核", jobEn = "Clickbait review", dataset = "headline", baseContract = "clickbait", duration = 600 },
            new XgSlaOffer { id = "sla.bank", client = "某银行反欺诈", clientEn = "A bank's anti-fraud desk", job = "诈骗短信识别", jobEn = "Scam SMS screening", dataset = "spam", baseContract = "antifraud", duration = 720 },
            new XgSlaOffer { id = "sla.homework", client = "某拍照搜题 App", clientEn = "A photo homework-search app", job = "判断题批改", jobEn = "True-or-false marking", dataset = "logic", baseContract = "homework", duration = 720 },
            new XgSlaOffer { id = "sla.courier", client = "某快递公司", clientEn = "A courier company", job = "面单手写单号识别", jobEn = "Handwritten waybill numbers", dataset = "mnist", baseContract = "cheque", duration = 900 },
        };

        public static XgSlaOffer SlaOffer(string id) { foreach (var o in SlaOffers) if (o.id == id) return o; return null; }

        /// <summary>A month's meme reached the desks (popup: 弹幕审核题库更新：8 月新梗).</summary>
        public event Action<XgMemeDriftNotice> MemeDrifted;
        /// <summary>A desk's checkpoint caught up with its meme (drift back to 0).</summary>
        public event Action<string> MemeDriftCleared;
        public event Action<XgSlaOffer> SlaSigned;
        /// <summary>An SLA contract ended: settled at the end or cancelled by a report.</summary>
        public event Action<XgSlaResult> SlaSettled;

        bool marketHooked;

        /// <summary>Called once from the constructor: repairs the market state and listens to the platform's events.</summary>
        void RepairMarket()
        {
            if (S.mkDrift == null) S.mkDrift = new List<XgMemeDrift>();
            if (S.slaCooldowns == null) S.slaCooldowns = new List<XgMarketTimer>();
            if (S.mkVersion == 0)
            {
                // A save with progress takes its current month as the baseline instead of drifting on load.
                bool progressed = S.epochs > 0 || S.totalIncome > 0 || S.handCorrect > 0;
                S.mkDriftMonth = progressed ? -1 : 0;
                S.mkVersion = 1;
            }
            if (S.mkDriftMonth < -1) S.mkDriftMonth = -1;
            S.mkDrift.RemoveAll(d => d == null || Array.IndexOf(MemeDriftDesks, d.dataset) < 0 || !FiniteMarket(d.points) || d.points <= 0);
            var seen = new HashSet<string>();
            S.mkDrift.RemoveAll(d => !seen.Add(d.dataset));
            foreach (var d in S.mkDrift) { d.points = Math.Min(MemeDriftPoints, d.points); d.meme = d.meme ?? ""; }

            S.slaId = S.slaId ?? "";
            if (S.slaId.Length > 0 && SlaOffer(S.slaId) == null) S.slaId = "";
            if (S.slaId.Length == 0) { S.slaLeft = S.slaHeld = S.slaEarned = 0; S.slaChecks = S.slaPassed = 0; }
            else
            {
                var offer = SlaOffer(S.slaId);
                if (!FiniteMarket(S.slaLeft) || S.slaLeft < 0) S.slaLeft = 0;
                S.slaLeft = Math.Min(offer.duration, S.slaLeft);
                if (!FiniteMarket(S.slaHeld) || S.slaHeld < 0) S.slaHeld = 0;
                if (!FiniteMarket(S.slaEarned) || S.slaEarned < 0) S.slaEarned = 0;
                S.slaChecks = Math.Max(0, S.slaChecks);
                S.slaPassed = Math.Max(0, Math.Min(S.slaChecks, S.slaPassed));
            }
            S.slaCooldowns.RemoveAll(t => t == null || SlaOffer(t.id) == null || !FiniteMarket(t.seconds) || t.seconds <= 0);
            foreach (var t in S.slaCooldowns) t.seconds = Math.Min(SlaCooldownSeconds, t.seconds);
            S.slaFiveStars = Math.Max(0, S.slaFiveStars); S.slaDocked = Math.Max(0, S.slaDocked); S.slaCancelled = Math.Max(0, S.slaCancelled);
            RepairSubcontract();
            if (!marketHooked)
            {
                marketHooked = true;
                AutoRouted += OnMarketAutoRouted;
                QualityReported += OnMarketReported;
            }
        }

        static bool FiniteMarket(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        /// <summary>Once per step, after automatic labelling: drift, SLA books and the 网吧 workers.</summary>
        void TickMarket(double dt, IXgHost host)
        {
            if (dt <= 0 || !FiniteMarket(dt)) return;
            TickMemeDrift();
            TickSla(dt, host);
            TickSubcontract(dt, host);
        }

        // ───────────── 新题型 ─────────────

        /// <summary>The date (yyyymmdd) a month's meme first appears in the 2016 lexicon, or 0 if it is missing.</summary>
        public static int MemeSince(string meme)
        {
            foreach (var w in XgMemes.HotWords) if (w.word == meme) return w.since;
            foreach (var t in XgMemes.Topics) if (t.topic == meme) return t.since;
            return 0;
        }

        /// <summary>The latest month meme that has appeared by <paramref name="today"/> (index into <see cref="MonthMemes"/>), or -1.</summary>
        public static int LatestMonthMeme(int today)
        {
            int best = -1, bestSince = 0;
            for (int i = 0; i < MonthMemes.Length; i++)
            {
                int since = MemeSince(MonthMemes[i].meme);
                if (since > 0 && since <= today && since > bestSince) { best = i; bestSince = since; }
            }
            return best;
        }

        public static bool IsMemeDriftDesk(string desk) => Array.IndexOf(MemeDriftDesks, desk) >= 0;

        XgMemeDrift DriftOf(string desk) { if (S.mkDrift != null) foreach (var d in S.mkDrift) if (d.dataset == desk) return d; return null; }

        /// <summary>Accuracy points (0–15) the checkpoint loses on this desk's new cards.</summary>
        public double MemeDrift(string desk) { var d = DriftOf(desk); return d == null ? 0 : d.points; }
        public string MemeDriftMeme(string desk) { var d = DriftOf(desk); return d == null ? "" : d.meme; }
        /// <summary>Subtracted from the checkpoint's per-card accuracy (automatic and ghost judgments) on a drifted desk.</summary>
        public double MemeDriftPenalty(string desk) => MemeDrift(desk) / 100.0;

        public static string MemeGloss(string meme)
        {
            foreach (var m in MonthMemes) if (m.meme == meme) return m.en;
            return meme;
        }

        /// <summary>The desk chip: 「新题型：洪荒之力 −15%」, or "" when the desk has no drift.</summary>
        public string MemeDriftChip(string desk)
        {
            var d = DriftOf(desk);
            if (d == null) return "";
            string pts = "−" + F(d.points, "0.#") + "%";
            return T("新题型：" + d.meme + " " + pts, "New card type: " + d.meme + " (" + MemeGloss(d.meme) + ") " + pts);
        }

        /// <summary>The desk's moderation queue on the platform (弹幕审核, 短信审核 …).</summary>
        public static string DriftQueueName(string desk, bool english)
        {
            switch (desk)
            {
                case "danmu": return english ? "Danmaku moderation" : "弹幕审核";
                case "spam": return english ? "SMS screening" : "短信审核";
                case "headline": return english ? "Headline review" : "标题审核";
                case "review": return english ? "Review moderation" : "评论审核";
                case "meme": return english ? "Sticker review" : "表情包审核";
                default: return english ? "Labelling" : "标注";
            }
        }

        void TickMemeDrift()
        {
            // A save loads before the desktop restores its date: reading the placeholder month here would set the
            // wrong baseline, and the real date one frame later would look like a new month's meme.
            if (!TodayKnown) return;
            int index = LatestMonthMeme(Today);
            int month = index < 0 ? 0 : MemeSince(MonthMemes[index].meme) / 100;
            if (S.mkDriftMonth < 0) { S.mkDriftMonth = month; return; }
            if (index < 0 || month <= S.mkDriftMonth) return;
            S.mkDriftMonth = month;
            var notice = new XgMemeDriftNotice { month = month, meme = MonthMemes[index].meme, memeEn = MonthMemes[index].en };
            foreach (var desk in MemeDriftDesks)
            {
                if (!DeskOpen(desk)) continue;
                var d = DriftOf(desk);
                if (d == null) S.mkDrift.Add(d = new XgMemeDrift { dataset = desk });
                // A new meme replaces an old one that is still being learned; drift never stacks past 15.
                d.meme = notice.meme; d.month = month; d.points = MemeDriftPoints;
                notice.desks.Add(desk);
            }
            if (notice.desks.Count == 0) return;
            int m = month % 100;
            Say(T(m + " 月新梗「" + notice.meme + "」上了题库：检查点在这些桌上的准确率 −15%，手标或重新训练能追回来。",
                "New meme for month " + m + " (" + notice.memeEn + ") reached the question banks: the checkpoint loses 15 points on those desks until you hand-label or retrain."));
            MemeDrifted?.Invoke(notice);
        }

        void RecoverMemeDrift(string desk, double points)
        {
            var d = DriftOf(desk);
            if (d == null || points <= 0) return;
            d.points = Math.Max(0, d.points - points);
            if (d.points > 1e-9) return;
            S.mkDrift.Remove(d);
            Say(T(XgCatalog.Desk(desk).name + "桌追上了新梗「" + d.meme + "」，检查点准确率恢复。", XgCatalog.Desk(desk).nameEn + " caught up with the new meme (" + MemeGloss(d.meme) + "); checkpoint accuracy restored."));
            MemeDriftCleared?.Invoke(desk);
        }

        /// <summary>A new correct human label on the desk: the data now has the new meme.</summary>
        void MemeDriftLabelled(string desk) => RecoverMemeDrift(desk, MemeDriftHandRecovery);
        /// <summary>One finished training epoch on this dataset.</summary>
        void MemeDriftTrained(string dataset) => RecoverMemeDrift(dataset, MemeDriftEpochRecovery);

        // ───────────── 甲方高价单 ─────────────

        public XgSlaOffer ActiveSla => S.slaId.Length > 0 ? SlaOffer(S.slaId) : null;
        public bool SlaActive => ActiveSla != null;
        public double SlaSecondsLeft => SlaActive ? S.slaLeft : 0;
        public double SlaHeld => S.slaHeld;
        public double SlaEarned => S.slaEarned;
        public int SlaChecks => S.slaChecks;
        public int SlaPassed => S.slaPassed;
        /// <summary>Share of the contract's spot checks that passed (1 before any).</summary>
        public double SlaPassRate => S.slaChecks == 0 ? 1 : (double)S.slaPassed / S.slaChecks;
        public bool SlaJudged => S.slaChecks >= SlaMinChecks;
        /// <summary>甲方高价单 come through the platform: visible once 摆渡众包 is checking the account.</summary>
        public bool SlaVisible => QualityActive;

        public double SlaCooldown(string id) { foreach (var t in S.slaCooldowns) if (t.id == id) return t.seconds; return 0; }

        /// <summary>Offers worth showing: their desk is open (plus the running one).</summary>
        public List<XgSlaOffer> SlaOffersVisible()
        {
            var list = new List<XgSlaOffer>();
            if (!SlaVisible) return list;
            foreach (var o in SlaOffers) if (DeskOpen(o.dataset) || S.slaId == o.id) list.Add(o);
            return list;
        }

        public bool CanSignSla(XgSlaOffer offer, out string why)
        {
            why = null;
            if (offer == null) { why = T("没有这个订单", "No such offer"); return false; }
            if (!SlaVisible) { why = T("需要先接入摆渡众包（买自动答题）", "Join Bodu Crowdsourcing first (buy auto labelling)"); return false; }
            if (SlaActive) { why = S.slaId == offer.id ? T("正在履约", "In progress") : T("一次只能接一单高价单", "One high-paying contract at a time"); return false; }
            if (QualityFrozen) { why = T("账号冻结中，甲方不接", "The account is frozen; clients won't sign"); return false; }
            if (!DeskOpen(offer.dataset)) { why = T("还没开这张标注桌", "That desk is not open yet"); return false; }
            double cooldown = SlaCooldown(offer.id);
            if (cooldown > 0) { why = T("甲方 " + FreezeClock(cooldown) + " 后再派单", "The client sends a new order in " + FreezeClock(cooldown)); return false; }
            if (Credit + 1e-9 < SlaMinCredit) { why = T("信用分需 ≥ " + F(SlaMinCredit, "0") + "（现在 " + F(Credit, "0") + "）", "Credit must be at least " + F(SlaMinCredit, "0") + " (now " + F(Credit, "0") + ")"); return false; }
            if (BestAcc(offer.dataset) + 1e-9 < offer.Threshold) { why = T("检查点准确率需 ≥ " + Pct(offer.Threshold), "Checkpoint must reach " + Pct(offer.Threshold)); return false; }
            return true;
        }

        public bool SignSla(string id)
        {
            var offer = SlaOffer(id);
            if (!CanSignSla(offer, out string why)) { if (why != null) Say(why); return false; }
            S.slaId = offer.id; S.slaLeft = offer.duration; S.slaHeld = S.slaEarned = 0; S.slaChecks = S.slaPassed = 0;
            Say(T("接下高价单：" + offer.client + "「" + offer.job + "」，" + F(offer.duration / 60, "0") + " 分钟内抽检合格率需 ≥ 95%，一半收入押作尾款。",
                "Signed a high-paying contract: " + offer.clientEn + ", " + offer.jobEn + ". Keep at least 95% of spot checks passing for " + F(offer.duration / 60, "0") + " min; half the income is held as the balance due."));
            SlaSigned?.Invoke(offer);
            return true;
        }

        /// <summary>Income per second of the running SLA contract, before the 尾款 is held back.</summary>
        public double SlaIncome
        {
            get
            {
                var offer = ActiveSla;
                if (offer == null) return 0;
                double acc = BestAcc(offer.dataset);
                if (acc + 1e-9 < offer.Threshold) return 0;
                return offer.Income * (1 + Math.Max(0, acc - offer.Threshold) / Math.Max(.01, 1 - offer.Threshold)) * (Winter ? .5 : 1);
            }
        }

        void TickSla(double dt, IXgHost host)
        {
            for (int i = S.slaCooldowns.Count - 1; i >= 0; i--)
            {
                S.slaCooldowns[i].seconds -= dt;
                if (S.slaCooldowns[i].seconds <= 0) S.slaCooldowns.RemoveAt(i);
            }
            var offer = ActiveSla;
            if (offer == null) return;
            double step = Math.Min(dt, S.slaLeft);
            double income = SlaIncome * step;
            if (income > 0 && host != null)
            {
                double now = income * (1 - SlaHeldShare);
                host.Earn(now); S.totalIncome += now;
                S.slaHeld += income - now;
                S.slaEarned += income;
            }
            S.slaLeft -= dt;
            if (S.slaLeft <= 1e-9) SettleSla(host);
        }

        /// <summary>Counts one platform spot check toward the running contract's clause (automatic and 转包 labels alike).</summary>
        void SlaObserveCheck(string dataset, bool passed)
        {
            if (!SlaActive || ActiveSla.dataset != dataset) return;
            S.slaChecks++;
            if (passed) S.slaPassed++;
        }

        void OnMarketAutoRouted(XgAutoRecord record)
        {
            if (record != null && record.spotChecked) SlaObserveCheck(record.dataset, record.correct);
        }

        void OnMarketReported(XgReportReason reason, double seconds)
        {
            var offer = ActiveSla;
            if (offer == null) return;
            var result = new XgSlaResult { id = offer.id, outcome = XgSlaOutcome.Cancelled, forfeited = S.slaHeld, earned = S.slaEarned, checks = S.slaChecks, passed = S.slaPassed };
            S.qcCredit = Math.Max(0, Math.Min(QcCreditMax, S.qcCredit - SlaCancelCredit));
            S.slaCancelled++;
            EndSla(offer);
            Say(T("账号被举报，" + offer.client + "终止合作：尾款 ¥" + F(result.forfeited, "0.##") + " 作废，信用分再 −" + F(SlaCancelCredit, "0") + "。",
                offer.clientEn + " cancelled after the report: the ¥" + F(result.forfeited, "0.##") + " balance is forfeited and credit drops another " + F(SlaCancelCredit, "0") + "."));
            SlaSettled?.Invoke(result);
        }

        void SettleSla(IXgHost host)
        {
            var offer = ActiveSla;
            if (offer == null) return;
            var result = new XgSlaResult { id = offer.id, earned = S.slaEarned, checks = S.slaChecks, passed = S.slaPassed };
            double held = S.slaHeld;
            if (S.slaChecks < SlaMinChecks) { result.outcome = XgSlaOutcome.Unjudged; result.paid = held; }
            else if (SlaPassRate + 1e-9 >= SlaClause) { result.outcome = XgSlaOutcome.FiveStar; result.paid = held; }
            else { result.outcome = XgSlaOutcome.Docked; result.paid = held * SlaDockedShare; }
            result.forfeited = held - result.paid;
            if (host == null) { result.paid = 0; result.forfeited = held; }
            else if (result.paid > 0) { host.Earn(result.paid); S.totalIncome += result.paid; }
            if (result.outcome == XgSlaOutcome.Docked) S.slaDocked++;
            if (result.outcome == XgSlaOutcome.FiveStar) S.slaFiveStars++;
            EndSla(offer);
            string rate = Pct(result.PassRate);
            switch (result.outcome)
            {
                case XgSlaOutcome.FiveStar:
                    Say(T(offer.client + "：五星好评！抽检合格率 " + rate + "，尾款 ¥" + F(result.paid, "0.##") + " 已结清。", offer.clientEn + ": five stars! Spot-check pass rate " + rate + "; the ¥" + F(result.paid, "0.##") + " balance is paid.")); break;
                case XgSlaOutcome.Docked:
                    Say(T(offer.client + "：质量不达标（抽检合格率 " + rate + "），扣一半尾款，实付 ¥" + F(result.paid, "0.##") + "。", offer.clientEn + ": quality below the clause (" + rate + "); half the balance withheld, ¥" + F(result.paid, "0.##") + " paid.")); break;
                default:
                    Say(T(offer.client + "：抽检不足 " + SlaMinChecks + " 条，尾款 ¥" + F(result.paid, "0.##") + " 全额结清。", offer.clientEn + ": fewer than " + SlaMinChecks + " spot checks; the ¥" + F(result.paid, "0.##") + " balance is paid in full.")); break;
            }
            SlaSettled?.Invoke(result);
        }

        void EndSla(XgSlaOffer offer)
        {
            S.slaId = ""; S.slaLeft = S.slaHeld = S.slaEarned = 0; S.slaChecks = S.slaPassed = 0;
            S.slaCooldowns.RemoveAll(t => t.id == offer.id);
            S.slaCooldowns.Add(new XgMarketTimer { id = offer.id, seconds = SlaCooldownSeconds });
        }
    }
}
