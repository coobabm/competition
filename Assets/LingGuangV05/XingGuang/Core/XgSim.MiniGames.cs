using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgSim
    {
        // Tickets are intentionally not saved. A reloaded save cannot settle a previous scene's result.
        long miniSerial, miniTicket;
        int miniSequence;
        string miniId;
        bool miniOwnsCrawl;
        /// <summary>The active native surface owns the card timer, not the economy clock.</summary>
        public Func<bool> MiniGameSurfaceCheck;
        public bool MiniGameSurfaceActive => MiniGameSurfaceCheck != null && MiniGameSurfaceCheck();
        public bool MiniGameTicketValid(long ticket) => ticket != 0 && ticket == miniTicket;

        public XgMiniRecord MiniGameRecord(string id)
        {
            if (S.miniGames == null) S.miniGames = new List<XgMiniRecord>();
            foreach (var r in S.miniGames) if (r != null && r.id == id) return r;
            // Unknown IDs never expand the persistent collection.
            var record = new XgMiniRecord { id = id };
            if (XgMiniGames.Find(id) != null) S.miniGames.Add(record);
            return record;
        }
        public string MiniGameBlocker(string id)
        {
            var pack = XgTeachingPacks.Find(id);
            if (pack != null) return TeachingPackBlocker(pack);
            var g = XgMiniGames.Find(id);
            if (g == null) return "未知小游戏";
            if (S.stage < g.Stage) return "第 " + g.Stage + " 阶段开放";
            if (g.Reward == XgMiniReward.Label && !DeskOpen(g.Desk)) return "先解锁「" + (XgCatalog.Desk(g.Desk)?.name ?? g.Desk) + "」数据集";
            return null;
        }
        public double MiniGameSkill(string id)
        {
            var g = XgMiniGames.Find(id);
            string dataset = g == null || string.IsNullOrEmpty(g.Desk) ? Selected.dataset : g.Desk;
            double accuracy = BestAcc(dataset);
            return double.IsNaN(accuracy) || double.IsInfinity(accuracy) ? .2 : Math.Max(.2, Math.Min(.97, accuracy));
        }
        public long BeginMiniGame(string id, IXgHost host)
        {
            if (host == null || MiniGameBlocker(id) != null) return 0;
            CancelMiniGame(miniTicket);
            var g = XgMiniGames.Find(id);
            if (g.Reward == XgMiniReward.Crawl)
            {
                // Do not steal a crawler run already owned by the data-page panel.
                if (CrawlActive || !StartCrawl(host)) return 0;
                miniOwnsCrawl = true;
            }
            miniId = id; miniSequence = 0;
            miniTicket = ++miniSerial;
            return miniTicket;
        }
        public void CancelMiniGame(long ticket)
        {
            if (ticket == 0 || ticket != miniTicket) return;
            if (miniOwnsCrawl) EndCrawl();
            miniOwnsCrawl = false; miniTicket = 0; miniId = null; miniSequence = 0;
        }
        /// <summary>
        /// Settles one answered item of the running game. Whatever the game's own scoring, an item is one action:
        /// see <see cref="SettleMiniAction"/> for the pay and the sample.
        /// </summary>
        public XgMiniResult SubmitMiniGame(long ticket, int sequence, bool correct, IXgHost host, double seconds = -1, bool automatic = false)
        {
            if (host == null || ticket == 0 || ticket != miniTicket || sequence != miniSequence + 1 || MiniGameBlocker(miniId) != null)
                return new XgMiniResult { note = T("这一题已经过期了", "That round has expired") };
            // Consume before invoking any host/event callback; reentrant duplicate claims cannot pay twice.
            miniSequence = sequence;
            var g = XgMiniGames.Find(miniId); var record = MiniGameRecord(miniId);
            var r = new XgMiniResult { accepted = true };
            if (correct) record.correct++; else record.wrong++;
            if (g.Reward == XgMiniReward.Training || g.Reward == XgMiniReward.Organize)
            {
                if (correct)
                {
                    if (g.Reward == XgMiniReward.Organize) record.organized++;
                    r.trainingStarted = BeginEpoch(SelectedTrack, host);
                    if (r.trainingStarted) record.trainingStarts++;
                    r.note = r.trainingStarted ? T("训练开始了", "Training started")
                        : T("训练没能开始：", "Training could not start: ") + (Blocker(Selected, host) ?? T("样本不够，或者已经在训练", "not enough samples, or a run is already going"));
                }
                else r.note = T("这次没有开训", "No training this time");
                return r;
            }
            if (g.Reward == XgMiniReward.Crawl)
            {
                r.samples = correct && miniOwnsCrawl ? CrawlGrab(1) : 0;
                record.samples += r.samples;
                r.note = r.samples > 0 ? T("百科词语 +" + r.samples, "Encyclopedia words +" + r.samples)
                    : T("本次没有采集；本轮余量 " + CrawlLeft, "Nothing collected; " + CrawlLeft + " left this run");
                return r;
            }
            var a = SettleMiniAction(g.Desk, correct, host, seconds, automatic: automatic);
            r.samples = a.samples; r.pay = a.Total; r.bonus = a.bonus; r.fine = a.fine; r.multiplier = correct ? a.multiplier : 0;
            record.samples += r.samples; record.income += a.Total - a.fine;
            r.note = correct
                ? T("样本 +" + r.samples + " · 收入 +¥" + r.pay.ToString("0.00") + (r.multiplier > 1 ? " · 连对 ×" + r.multiplier : "") + (r.bonus > 0 ? "（含奖励 ¥" + r.bonus.ToString("0.00") + "）" : ""),
                    "Sample +" + r.samples + " · +¥" + r.pay.ToString("0.00") + (r.multiplier > 1 ? " · streak ×" + r.multiplier : "") + (r.bonus > 0 ? " (bonus ¥" + r.bonus.ToString("0.00") + " included)" : ""))
                : T("样本不入库" + (r.fine > 0 ? " · 扣款 ¥" + r.fine.ToString("0.00") : " · 无收入"), "Not stored as a sample" + (r.fine > 0 ? " · fined ¥" + r.fine.ToString("0.00") : " · no pay"));
            return r;
        }
    }
}
