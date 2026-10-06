using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>A 网吧 friend who labels under the player's 摆渡众包 account.</summary>
    public sealed class XgWorkerInfo
    {
        public string id, name, nameEn;
        /// <summary>¥ per game minute, charged even while idle.</summary>
        public double wage;
        /// <summary>Seconds per card.</summary>
        public double interval;
        public double accuracy;
        /// <summary>Only works the 网吧 night shift (22:00–06:00).</summary>
        public bool nightOnly;
    }

    [Serializable]
    public sealed class XgWorkerState
    {
        public string id = "", desk = "";
        public bool hired;
        /// <summary>Progress toward the next card (0–1) and prepaid wage seconds left.</summary>
        public double timer, wageTimer;
        public int labelsToday, labelsTotal, correctTotal, errorsSeen;
        public double wagesPaid, earned, fines;
    }

    /// <summary>A line for the YY 网吧 group, queued by the lab and delivered by the desktop.</summary>
    [Serializable]
    public sealed class XgYYLine
    {
        /// <summary>Worker id of the speaker (ajie, xiaogang, boss).</summary>
        public string from = "";
        public string zh = "", en = "";
    }

    public sealed partial class XgState
    {
        public int scVersion;
        /// <summary>阿杰 asked to help: the 转包 panel is open.</summary>
        public bool scUnlocked;
        public List<XgWorkerState> scWorkers = new List<XgWorkerState>();
        /// <summary>Game seconds 阿杰 has worked for the player, across hires (the click-script twist clock).</summary>
        public double scAjieSeconds;
        /// <summary>0 honest, 1 小刚 hinted, 2 click script running, 3 fired for it (the one twist is over).</summary>
        public int scTwist;
        /// <summary>Wages paid to 阿杰 while his script ran (he gives them back when fired).</summary>
        public double scScriptWages;
        /// <summary>The calendar day (yyyymmdd) of the "today" counters.</summary>
        public int scDay;
        public long scRng = XgSim.SubcontractSeed;
        public List<XgYYLine> scOutbox = new List<XgYYLine>();
    }

    /// <summary>
    /// 转包: once the player owns 自动答题 (they already outsource to the model) and has reached stage 2, 阿杰 offers in
    /// the YY 网吧 group to label for them, which opens the 转包 panel. Three friends can be hired, each on one open desk:
    /// 阿杰 (cheap, fast, sloppy), 小刚 (slow and careful) and the 网吧 boss (night shift only). They label under the
    /// player's account, so every label leaves through the same platform path as an automatic one
    /// (<see cref="SubmitExternalLabel"/>): spot checks, fines, the speed meter, the monotone-answer detector and the
    /// SLA clause all see them. Right labels pay the desk's automatic rate and add samples; wrong unchecked ones add
    /// noise. Wages run every game minute even while a worker idles (frozen account, pending captcha).
    ///
    /// The twist (once per save): about 20 game minutes into 阿杰's work he quietly switches to a click script that
    /// answers 是 to everything twice as fast. 小刚 warns in YY three minutes before; the monotone answers can get the
    /// account reported. Firing 阿杰 after that brings an apology and the wages paid while the script ran.
    /// </summary>
    public sealed partial class XgSim
    {
        public const long SubcontractSeed = 0x0BADC0DEL;
        public const int SubcontractStage = 2, OutboxLimit = 20;
        public const double WageSeconds = 60;
        public const double AjieScriptAfter = 1200, XiaogangHintBefore = 180, ScriptSpeedup = 2;
        public const double NightStart = 22 * 3600, NightEnd = 6 * 3600;
        /// <summary>Without a desktop clock (tests, tools) the time of day starts at 01:47 (GameCalendar.Start); the desktop sets the real time.</summary>
        public const double ClockStartOfDay = 1 * 3600 + 47 * 60;

        public static readonly XgWorkerInfo[] Workers =
        {
            new XgWorkerInfo { id = "ajie", name = "阿杰", nameEn = "Ajie", wage = 2, interval = 3, accuracy = .85 },
            new XgWorkerInfo { id = "xiaogang", name = "小刚", nameEn = "Xiaogang", wage = 3, interval = 6, accuracy = .97 },
            new XgWorkerInfo { id = "boss", name = "老板", nameEn = "Boss", wage = 4, interval = 4, accuracy = .93, nightOnly = true },
        };
        public static XgWorkerInfo WorkerInfo(string id) { foreach (var w in Workers) if (w.id == id) return w; return null; }

        /// <summary>Seconds since midnight on the desktop clock; the desktop sets it each frame (negative = derive from <see cref="Clock"/>).</summary>
        public double ClockOfDaySeconds = -1;
        public double TimeOfDay => ClockOfDaySeconds >= 0 && FiniteMarket(ClockOfDaySeconds) ? ClockOfDaySeconds % 86400 : ((ClockStartOfDay + Math.Max(0, FiniteMarket(Clock) ? Clock : 0)) % 86400);
        /// <summary>网吧夜班: 22:00–06:00.</summary>
        public bool NightShift { get { double t = TimeOfDay; return t >= NightStart || t < NightEnd; } }

        /// <summary>A label from outside the model left the account (worker, argument: the record).</summary>
        public event Action<string, XgAutoRecord> ExternalLabelled;
        /// <summary>阿杰 offered help and the 转包 panel opened.</summary>
        public event Action SubcontractOpened;
        /// <summary>A worker stopped: (worker id, reason: "fired", "unpaid", "shift").</summary>
        public event Action<string, string> WorkerLeft;
        /// <summary>A worker was hired and paid the first minute (argument: worker id).</summary>
        public event Action<string> WorkerHired;

        public bool SubcontractReady => GlobalAutoLevel >= 1 && S.stage >= SubcontractStage;
        public bool SubcontractUnlocked => S.scUnlocked;
        public XgWorkerState Worker(string id) { foreach (var w in S.scWorkers) if (w.id == id) return w; return null; }
        public bool Hired(string id) { var w = Worker(id); return w != null && w.hired; }
        /// <summary>阿杰's click script is running right now.</summary>
        public bool AjieScripting => S.scTwist == 2 && Hired("ajie");
        public int SubcontractTwist => S.scTwist;
        public double AjieWorkedSeconds => S.scAjieSeconds;
        public double WagesPerMinute { get { double n = 0; foreach (var w in Workers) if (Hired(w.id)) n += w.wage; return n; } }

        /// <summary>Seconds per card for this worker now (the script halves 阿杰's).</summary>
        public double WorkerInterval(string id)
        {
            var info = WorkerInfo(id);
            if (info == null) return 0;
            return id == "ajie" && S.scTwist == 2 ? info.interval / ScriptSpeedup : info.interval;
        }

        /// <summary>Why a hired worker is not labelling right now, or null when they are.</summary>
        public string WorkerIdleReason(string id)
        {
            var w = Worker(id);
            if (w == null || !w.hired) return T("未雇用");
            if (QualityFrozen) return T("账号冻结，干等着");
            if (CaptchaPending) return T("等你输验证码");
            // Their labels leave through your account, so the platform's pause after a failed captcha holds them too.
            if (CaptchaPauseLeft > 0) return T("账号被暂停提交，干等着");
            if (!DeskOpen(w.desk)) return T("这张桌还没开");
            return null;
        }

        void RepairSubcontract()
        {
            if (S.scWorkers == null) S.scWorkers = new List<XgWorkerState>();
            if (S.scOutbox == null) S.scOutbox = new List<XgYYLine>();
            S.scWorkers.RemoveAll(w => w == null || WorkerInfo(w.id) == null);
            var seen = new HashSet<string>();
            S.scWorkers.RemoveAll(w => !seen.Add(w.id));
            foreach (var info in Workers) if (Worker(info.id) == null) S.scWorkers.Add(new XgWorkerState { id = info.id });
            S.scWorkers.Sort((a, b) => Array.FindIndex(Workers, x => x.id == a.id).CompareTo(Array.FindIndex(Workers, x => x.id == b.id)));
            foreach (var w in S.scWorkers)
            {
                w.desk = w.desk ?? "";
                if (XgCatalog.Desk(w.desk) == null) w.desk = "";
                if (!FiniteMarket(w.timer) || w.timer < 0) w.timer = 0;
                w.timer = Math.Min(w.timer, .999999);
                if (!FiniteMarket(w.wageTimer) || w.wageTimer < 0) w.wageTimer = 0;
                w.wageTimer = Math.Min(w.wageTimer, WageSeconds);
                w.labelsToday = Math.Max(0, w.labelsToday); w.labelsTotal = Math.Max(0, w.labelsTotal);
                w.correctTotal = Math.Max(0, Math.Min(w.labelsTotal, w.correctTotal)); w.errorsSeen = Math.Max(0, w.errorsSeen);
                if (!FiniteMarket(w.wagesPaid) || w.wagesPaid < 0) w.wagesPaid = 0;
                if (!FiniteMarket(w.earned) || w.earned < 0) w.earned = 0;
                if (!FiniteMarket(w.fines) || w.fines < 0) w.fines = 0;
                // Nobody can work for a locked panel or without a desk.
                if (!S.scUnlocked || w.desk.Length == 0) w.hired = false;
            }
            if (!FiniteMarket(S.scAjieSeconds) || S.scAjieSeconds < 0) S.scAjieSeconds = 0;
            S.scTwist = Math.Max(0, Math.Min(3, S.scTwist));
            if (!FiniteMarket(S.scScriptWages) || S.scScriptWages < 0) S.scScriptWages = 0;
            S.scOutbox.RemoveAll(l => l == null || WorkerInfo(l.from) == null || string.IsNullOrEmpty(l.zh));
            foreach (var l in S.scOutbox) l.en = string.IsNullOrEmpty(l.en) ? l.zh : l.en;
            Trim(S.scOutbox, OutboxLimit);
            if (S.scRng == 0) S.scRng = SubcontractSeed;
            S.scVersion = 1;
        }

        double SubcontractRoll()
        {
            long x = S.scRng;
            x ^= x << 13; x ^= (long)((ulong)x >> 7); x ^= x << 17;
            S.scRng = x == 0 ? SubcontractSeed : x;
            return (double)((ulong)S.scRng >> 11) / (1UL << 53);
        }

        void Line(string from, string zh, string en)
        {
            S.scOutbox.Add(new XgYYLine { from = from, zh = zh, en = en });
            Trim(S.scOutbox, OutboxLimit);
        }

        /// <summary>YY group lines waiting for the desktop ("[阿杰] …" is added there). Empties the queue.</summary>
        public List<XgYYLine> TakeYYLines()
        {
            var lines = new List<XgYYLine>(S.scOutbox);
            S.scOutbox.Clear();
            return lines;
        }
        /// <summary>The oldest waiting YY line, removed from the queue (null when none).</summary>
        public XgYYLine TakeYYLine()
        {
            if (S.scOutbox.Count == 0) return null;
            var line = S.scOutbox[0];
            S.scOutbox.RemoveAt(0);
            return line;
        }
        public int PendingYYLines => S.scOutbox.Count;

        public string WorkerName(string id) { var i = WorkerInfo(id); return i == null ? id : T(i.name, i.nameEn); }

        string DefaultWorkerDesk()
        {
            if (DeskOpen(S.desk)) return S.desk;
            foreach (var d in XgCatalog.Desks) if (DeskOpen(d.id)) return d.id;
            return "";
        }

        /// <summary>Assigns a worker to an open desk (hired or not).</summary>
        public bool AssignWorkerDesk(string id, string desk)
        {
            var w = Worker(id);
            if (w == null || !S.scUnlocked || !DeskOpen(desk)) return false;
            if (w.desk != desk) w.timer = 0;
            w.desk = desk;
            return true;
        }

        /// <summary>Moves a worker to the next open desk. Returns the new desk.</summary>
        public string CycleWorkerDesk(string id)
        {
            var w = Worker(id);
            if (w == null || !S.scUnlocked) return "";
            var open = OpenDesks();
            if (open.Count == 0) return w.desk;
            int at = open.FindIndex(d => d.id == w.desk);
            AssignWorkerDesk(id, open[(at + 1) % open.Count].id);
            return w.desk;
        }

        public string WorkerDesk(string id)
        {
            var w = Worker(id);
            if (w == null) return "";
            return DeskOpen(w.desk) ? w.desk : DefaultWorkerDesk();
        }

        public bool CanHire(string id, IXgHost host, out string why)
        {
            why = null;
            var info = WorkerInfo(id); var w = Worker(id);
            if (info == null || w == null) { why = T("没有这个人"); return false; }
            if (!S.scUnlocked) { why = T("转包还没开张"); return false; }
            if (w.hired) { why = T("已经在干活了"); return false; }
            if (info.nightOnly && !NightShift) { why = T("老板只上网吧夜班（22:00–06:00）"); return false; }
            if (WorkerDesk(id).Length == 0) { why = T("没有开放的标注桌"); return false; }
            if (host == null || !FiniteMarket(host.Money) || host.Money + 1e-9 < info.wage) { why = T("连第一分钟工钱都付不起"); return false; }
            return true;
        }

        /// <summary>Hires a worker on their assigned desk and pays the first minute up front.</summary>
        public bool Hire(string id, IXgHost host)
        {
            if (!CanHire(id, host, out string why)) { if (why != null) Say(why); return false; }
            var info = WorkerInfo(id); var w = Worker(id);
            if (!host.Spend(info.wage)) { Say(T("经费不足")); return false; }
            S.totalSpent += info.wage; w.wagesPaid += info.wage;
            if (id == "ajie" && S.scTwist == 2) S.scScriptWages += info.wage;
            w.desk = WorkerDesk(id); w.hired = true; w.timer = 0; w.wageTimer = WageSeconds;
            WorkerHired?.Invoke(id);
            switch (id)
            {
                case "ajie":
                    if (S.scTwist == 3) Line("ajie", "这回老老实实手标，真的。", "This time I label by hand, honest.");
                    else Line("ajie", "得嘞，两块一分钟。标错了可别赖我啊。", "Deal, ¥2 a minute. Don't blame me for the odd mistake.");
                    break;
                case "xiaogang": Line("xiaogang", "行，我慢慢标，保证质量。", "OK. I'll go slow and get them right."); break;
                case "boss": Line("boss", "夜班没人上机，我闲着也是闲着。", "Nobody's on the night shift. I'm idle anyway."); break;
            }
            Say(T("转包：" + info.name + " 开始标「" + XgCatalog.Desk(w.desk).name + "」，¥" + F(info.wage, "0") + "/分钟。", "Subcontract: " + info.nameEn + " starts on " + XgCatalog.Desk(w.desk).nameEn + ", ¥" + F(info.wage, "0") + "/min."));
            return true;
        }

        /// <summary>Lets a worker go. Firing 阿杰 while his script runs ends the twist: he apologises and gives the script-time wages back.</summary>
        public bool Fire(string id, IXgHost host = null)
        {
            var w = Worker(id);
            if (w == null || !w.hired) return false;
            w.hired = false; w.timer = 0; w.wageTimer = 0;
            if (id == "ajie" && S.scTwist == 2)
            {
                S.scTwist = 3;
                double refund = S.scScriptWages;
                S.scScriptWages = 0;
                if (refund > 0 && host != null) { host.Earn(refund); S.totalIncome += refund; }
                Line("ajie", "哥，对不起……我开了按键精灵，一路点的「是」。这几分钟的工钱" + (refund > 0 ? "（¥" + F(refund, "0") + "）" : "") + "我退给你了。",
                    "Sorry, bro… I ran a click macro that just hit Yes on everything. I'm giving back the pay for those minutes" + (refund > 0 ? " (¥" + F(refund, "0") + ")" : "") + ".");
                Say(T("你辞退了阿杰。他承认开了按键精灵。"));
            }
            else Say(T("你辞退了" + WorkerInfo(id).name + "。", "You let " + WorkerInfo(id).nameEn + " go."));
            WorkerLeft?.Invoke(id, "fired");
            return true;
        }

        void Leave(XgWorkerState w, string reason)
        {
            w.hired = false; w.timer = 0; w.wageTimer = 0;
            WorkerLeft?.Invoke(w.id, reason);
        }

        void TickSubcontract(double dt, IXgHost host)
        {
            if (!S.scUnlocked)
            {
                if (!SubcontractReady) return;
                S.scUnlocked = true;
                foreach (var w in S.scWorkers) if (w.desk.Length == 0) w.desk = DefaultWorkerDesk();
                Line("ajie", "你那个众包还缺人不？我反正在网吧坐着。", "Does your crowd-labelling gig need hands? I'm sitting in the netbar anyway.");
                Say(T("阿杰在 YY 群里问要不要帮你标：订单页开了「转包」。"));
                SubcontractOpened?.Invoke();
            }
            if (S.scDay != Today) { S.scDay = Today; foreach (var w in S.scWorkers) w.labelsToday = 0; }
            // The friends go home while the game is closed: no work, no wages, no twist clock.
            if (OfflineSimulation) return;
            foreach (var w in S.scWorkers)
            {
                if (!w.hired) continue;
                var info = WorkerInfo(w.id);
                if (info.nightOnly && !NightShift)
                {
                    Leave(w, "shift");
                    Line(w.id, "天亮了，交班睡觉去了。今晚十点后再叫我。", "Sun's up. Handing over the shift and going to bed. Call me after ten tonight.");
                    continue;
                }
                if (!PayWage(w, info, dt, host)) continue;
                if (w.id == "ajie") TickTwist(dt);
                if (WorkerIdleReason(w.id) != null) continue;
                w.timer += dt / WorkerInterval(w.id);
                int guard = 8;
                while (w.timer >= 1 - 1e-9 && guard-- > 0 && w.hired && WorkerIdleReason(w.id) == null)
                {
                    w.timer = Math.Max(0, w.timer - 1);
                    WorkerLabel(w, info, host);
                }
                w.timer = Math.Min(w.timer, .999999);
            }
        }

        /// <summary>Charges a minute's wage whenever the prepaid minute runs out. False when the worker walked off unpaid.</summary>
        bool PayWage(XgWorkerState w, XgWorkerInfo info, double dt, IXgHost host)
        {
            w.wageTimer -= dt;
            int guard = 4;
            while (w.wageTimer <= 1e-9 && guard-- > 0)
            {
                if (host == null || !FiniteMarket(host.Money) || host.Money + 1e-9 < info.wage || !host.Spend(info.wage))
                {
                    Leave(w, "unpaid");
                    Line(w.id, "工钱没到账啊，我先撤了。", "My pay didn't come through, so I'm off.");
                    Say(T(info.name + " 没领到工钱，走了。", info.nameEn + " wasn't paid and left."));
                    return false;
                }
                S.totalSpent += info.wage; w.wagesPaid += info.wage;
                if (w.id == "ajie" && S.scTwist == 2) S.scScriptWages += info.wage;
                w.wageTimer += WageSeconds;
            }
            if (w.wageTimer <= 0) w.wageTimer = WageSeconds;
            return true;
        }

        void TickTwist(double dt)
        {
            S.scAjieSeconds += dt;
            if (S.scTwist == 0 && S.scAjieSeconds >= AjieScriptAfter - XiaogangHintBefore)
            {
                S.scTwist = 1;
                Line("xiaogang", "阿杰刚下了个按键精灵，说待会儿就能标得飞快。你看看他。", "Ajie just downloaded a click macro and says he'll be labelling super fast soon. Keep an eye on him.");
            }
            // Quietly: no message. The answers just turn into 是, twice as fast.
            if (S.scTwist == 1 && S.scAjieSeconds >= AjieScriptAfter) S.scTwist = 2;
        }

        void WorkerLabel(XgWorkerState w, XgWorkerInfo info, IXgHost host)
        {
            var d = XgCatalog.Dataset(w.desk);
            var card = new XgCard { track = (int)d.track, dataset = w.desk, level = LevelOf(w.desk), truth = SubcontractRoll() < .5 };
            bool scripted = w.id == "ajie" && S.scTwist == 2;
            if (scripted) card.guess = true;
            else card.guess = SubcontractRoll() < info.accuracy ? card.truth : !card.truth;
            card.hasJudgment = true; card.confidence = 1; card.judgeSource = "worker:" + w.id;
            var record = SubmitExternalLabel(card, host);
            if (record == null) return;
            w.labelsToday++; w.labelsTotal++;
            if (record.correct) { w.correctTotal++; w.earned += record.pay; }
            if (record.spotChecked && !record.correct) { w.errorsSeen++; w.fines += record.fine; }
        }

        /// <summary>
        /// A label made outside the lab's model (a 转包 worker) leaves the player's account. It goes through the same
        /// platform path as an automatic label: the speed meter, the spot check (fine, credit, monotone detector,
        /// report) and the SLA clause. Right: the desk's automatic pay and one sample (the data has the new meme, so
        /// drift recovers too). Wrong and unchecked: one noisy row. Wrong and checked: the platform's fine.
        /// Never touches the lab's routing statistics or the automatic feed. Returns null if the card is unusable.
        /// </summary>
        public XgAutoRecord SubmitExternalLabel(XgCard card, IXgHost host)
        {
            if (card == null || host == null || XgCatalog.Desk(card.dataset) == null || XgCatalog.Dataset(card.dataset) == null) return null;
            // The platform takes nothing from the account while it is frozen, waiting for a captcha or paused.
            if (AutomationHeld) return null;
            EnsureCardId(card);
            if (card.level < 1) card.level = LevelOf(card.dataset);
            bool correct = card.guess == card.truth;
            double unitPay = PayFor(card.dataset, card.level) * QualityPayMultiplier;
            NoteLabelSpeed();
            double fine = SettleSpotCheck(card, correct, unitPay, host, out bool spotChecked);
            double pay = 0;
            if (correct)
            {
                pay = unitPay;
                host.Earn(pay); S.totalIncome += pay; AddLabel(card.dataset);
                MemeDriftLabelled(card.dataset);
            }
            else if (!spotChecked)
            {
                SetCount(S.noise, card.dataset, Noise(card.dataset) + 1);
                foreach (var run in Runs) if (run.dataset == card.dataset) Evaluate(run);
            }
            var record = new XgAutoRecord { cardId = card.id, dataset = card.dataset, question = card.question ?? "", judgeSource = card.judgeSource ?? "", guess = card.guess, confidence = card.confidence, correct = correct, pay = pay, spotChecked = spotChecked, fine = fine };
            if (spotChecked) SlaObserveCheck(card.dataset, correct);
            ExternalLabelled?.Invoke(card.judgeSource ?? "", record);
            if (spotChecked && !correct) QualityFined?.Invoke(new XgQcFine { cardId = card.id, dataset = card.dataset, pay = unitPay, fine = fine });
            if (spotChecked) JudgeQuality();
            CheckDesks();
            return record;
        }
    }
}
