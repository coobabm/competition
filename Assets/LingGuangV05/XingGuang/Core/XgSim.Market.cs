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

        /// <summary>Legacy earned escrow from removed high-price orders; released once on the next valid tick.</summary>
        public double slaHeld;
    }

    /// <summary>Monthly meme drift and subcontracting for the crowdsourcing platform.</summary>
    public sealed partial class XgSim
    {
        public const double MemeDriftPoints = 15, MemeDriftHandRecovery = .5, MemeDriftEpochRecovery = 1;

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

        /// <summary>A month's meme reached the desks (popup: 弹幕审核题库更新：8 月新梗).</summary>
        public event Action<XgMemeDriftNotice> MemeDrifted;
        /// <summary>A desk's checkpoint caught up with its meme (drift back to 0).</summary>
        public event Action<string> MemeDriftCleared;
        /// <summary>Repairs drift and retained escrow without reactivating removed work.</summary>
        void RepairMarket()
        {
            if (S.mkDrift == null) S.mkDrift = new List<XgMemeDrift>();
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

            if (!FiniteMarket(S.slaHeld) || S.slaHeld < 0) S.slaHeld = 0;
            RepairSubcontract();
        }

        static bool FiniteMarket(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        /// <summary>Once per step, after automatic labelling: drift, legacy escrow and the 网吧 workers.</summary>
        void TickMarket(double dt, IXgHost host)
        {
            if (dt <= 0 || !FiniteMarket(dt)) return;
            TickMemeDrift();
            if (host != null && S.slaHeld > 0)
            {
                double held = S.slaHeld;
                host.Earn(held);
                S.totalIncome += held;
                S.slaHeld = 0;
            }
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
    }
}
