using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// What the house tells the life events (optional; the desktop and the bot give it as <see cref="XgSim.Household"/>, or
    /// the host may implement it). Without it the events that read the house (the landlord's meter) never come up.
    /// </summary>
    public interface IXgHousehold
    {
        /// <summary>Rent and bills are being charged (the rent holiday is over).</summary>
        bool BillsActive { get; }
        /// <summary>Training electricity this calendar month so far (kWh).</summary>
        double MonthKwh { get; }
        /// <summary>A month at or above this many kWh counts as a high bill (the third 阶梯电价 tier).</summary>
        double HighKwh { get; }
        /// <summary>The desktop clock's seconds since midnight.</summary>
        double SecondsOfDay { get; }
    }

    /// <summary>One answered life event, for the history.</summary>
    [Serializable]
    public sealed class XgLifeRecord
    {
        public string id = "", choice = "";
        /// <summary>Game date (yyyymmdd, 0 when unknown) and the life clock when it was answered.</summary>
        public int date;
        public double at;
    }

    /// <summary>
    /// A timed entry of the life events: a cooldown (id, until), a card unplugged for a while (id "lend" / "quiet", key =
    /// the 接线 card key, amount 1 when the player had already unplugged it), or money coming back (id, amount, until).
    /// </summary>
    [Serializable]
    public sealed class XgLifeTimer
    {
        public string id = "", key = "", name = "", nameEn = "";
        public double until, amount;
    }

    /// <summary>A choice of the open life event as the card shows it.</summary>
    public sealed class XgLifeChoice
    {
        public string id = "", label = "", detail = "";
        public bool enabled = true;
    }

    /// <summary>The open life event as the card shows it (built fresh, in the current language).</summary>
    public sealed class XgLifeEvent
    {
        public string id = "", who = "", title = "", body = "";
        public List<XgLifeChoice> choices = new List<XgLifeChoice>();
    }

    public sealed partial class XgState
    {
        /// <summary>0 = a save from before the life events (its scheduler starts on the next tick).</summary>
        public int lifeVersion;
        /// <summary>The events' own random stream (seeded from <see cref="rng"/> once), so drawing them never moves the lab's dice.</summary>
        public long lifeRng;
        /// <summary>Seconds the life events have been ticked, and the seconds left until the next draw.</summary>
        public double lifeClock, lifeNext;
        /// <summary>The open event ("" = none) and what it is about (a card key, a dataset, an amount).</summary>
        public string lifeOpen = "", lifeArgKey = "", lifeArgName = "", lifeArgNameEn = "";
        public double lifeArg;
        public List<XgLifeRecord> lifeHistory = new List<XgLifeRecord>();
        public List<XgLifeTimer> lifeCooldowns = new List<XgLifeTimer>();
        public List<XgLifeTimer> lifeHolds = new List<XgLifeTimer>();
        public List<XgLifeTimer> lifeReturns = new List<XgLifeTimer>();
        /// <summary>老周 owes a favour (a campus copy of one download), and how many times he was turned down.</summary>
        public int lifeFavour, lifeZhouCool;
        /// <summary>The landlord did not quite believe the last explanation.</summary>
        public bool lifeLandlordWary;
        /// <summary>The month (yyyymm) being watched, its highest training kWh, and how many high months in a row ended.</summary>
        public int lifePowerMonth, lifeHighMonths;
        public double lifeMonthPeak;
        /// <summary>The month (yyyymm) the broadband reminder last came (or the line was first seen above ADSL).</summary>
        public int lifeBroadbandMonth;
        /// <summary>The life clock when training last ran.</summary>
        public double lifeLastTrain = -1e9;
    }

    /// <summary>
    /// 节奏重做 §2.5, the life-event pool (A Dark Room): every 3–5 minutes one event is drawn, evenly, from those whose
    /// conditions hold. One is open at a time; none comes before the opening is over (the second ability), during the
    /// finale, or while something else needs the player (a cutscene, a story beat, an exam, a captcha: <see cref="LifeEventsHeld"/>,
    /// with <see cref="LifeEventHold"/> set by the desktop); a draw that falls then is put off a little. Every event asks
    /// for a real choice, and its result is written into the state through the systems that already exist: the wallet,
    /// the 接线 plugs, the broadband tier, the download queue and the batches on the disk, the hand-label samples.
    /// Hoarding (money over five times the dearest thing on offer) brings a nudge that never takes money by itself.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int LifeSchema = 1;
        public const double LifeIntervalMin = 180, LifeIntervalMax = 300;
        /// <summary>A draw that falls while the player is busy is tried again this much later.</summary>
        public const double LifeHeldRetry = 15;
        /// <summary>老周 keeps the card this long; the night throttle lasts this long.</summary>
        public const double LifeLendSeconds = 600, LifeQuietSeconds = 300;
        /// <summary>Money lent to 老周 / put in 余额宝 comes back after this long.</summary>
        public const double LifeReturnSeconds = 600;
        /// <summary>Money is hoarded past this many times the dearest thing on offer.</summary>
        public const double LifeHoardFactor = 5;
        public const double LifeHoardMinMoney = 1000;
        /// <summary>The disk is this full before it grows bad sectors.</summary>
        public const double LifeDiskShare = .8;
        public const double LifeRedBull = 120, LifeLandlordExtra = 100, LifeLandlordFine = 200;
        /// <summary>Chance the landlord believes the explanation (first time, and once he is wary).</summary>
        public const double LifeExplainChance = .6, LifeExplainWaryChance = .3;
        public const int LifeHistoryKept = 40;

        /// <summary>Every event of the pool.</summary>
        public static readonly string[] LifeEventIds = { "zhou_card", "bad_sectors", "broadband", "landlord_meter", "roommate_fan", "forum_post", "zhou_loan", "licai_call", "zhou_favour" };

        /// <summary>The house as the life events see it (the desktop sets it; null = the host, when it is one).</summary>
        public IXgHousehold Household;
        /// <summary>The house of the last tick: <see cref="Household"/>, else the host.</summary>
        IXgHousehold lifeHouse;
        IXgHousehold House => Household ?? lifeHouse;
        /// <summary>The desktop answers true while something else holds the player (a cutscene, story, exam, the setup). Not saved.</summary>
        public Func<bool> LifeEventHold;

        /// <summary>An event opened (the card and a tray popup show it).</summary>
        public event Action<XgLifeEvent> LifeEventOpened;
        /// <summary>An event was answered: (event id, choice id, the result line).</summary>
        public event Action<string, string, string> LifeEventAnswered;

        // ───────────── state and gates ─────────────

        /// <summary>The repair below ran for this state (it runs once per loaded save, then only the cheap checks).</summary>
        XgState lifeRepaired;

        void EnsureLife()
        {
            if (ReferenceEquals(lifeRepaired, S) && S.lifeHistory != null && S.lifeCooldowns != null && S.lifeHolds != null && S.lifeReturns != null && S.lifeOpen != null) return;
            lifeRepaired = S;
            if (S.lifeHistory == null) S.lifeHistory = new List<XgLifeRecord>();
            if (S.lifeCooldowns == null) S.lifeCooldowns = new List<XgLifeTimer>();
            if (S.lifeHolds == null) S.lifeHolds = new List<XgLifeTimer>();
            if (S.lifeReturns == null) S.lifeReturns = new List<XgLifeTimer>();
            if (S.lifeOpen == null || Array.IndexOf(LifeEventIds, S.lifeOpen) < 0) S.lifeOpen = "";
            if (S.lifeArgKey == null) S.lifeArgKey = "";
            if (S.lifeArgName == null) S.lifeArgName = "";
            if (S.lifeArgNameEn == null) S.lifeArgNameEn = "";
            if (!Finite(S.lifeClock) || S.lifeClock < 0) S.lifeClock = 0;
            if (!Finite(S.lifeArg)) S.lifeArg = 0;
            if (!Finite(S.lifeMonthPeak) || S.lifeMonthPeak < 0) S.lifeMonthPeak = 0;
            if (!Finite(S.lifeLastTrain)) S.lifeLastTrain = -1e9;
            S.lifeFavour = Math.Max(0, Math.Min(1, S.lifeFavour));
            S.lifeZhouCool = Math.Max(0, Math.Min(5, S.lifeZhouCool));
            S.lifeHistory.RemoveAll(r => r == null);
            foreach (var list in new[] { S.lifeCooldowns, S.lifeHolds, S.lifeReturns }) list.RemoveAll(t => t == null || !Finite(t.until) || !Finite(t.amount) || t.id == null || t.key == null);
            if (S.lifeVersion < LifeSchema)
            {
                // A new game or a save from before the pool: its own dice come from the lab's, and the first draw is a whole interval away.
                S.lifeRng = Mix(S.rng ^ 0x4C1FE5EEDL);
                if (S.lifeRng == 0) S.lifeRng = 0x4C1FE5EEDL;
                S.lifeNext = NextLifeInterval();
                S.lifeVersion = LifeSchema;
            }
            if (S.lifeRng == 0) S.lifeRng = 0x4C1FE5EEDL;
            if (!Finite(S.lifeNext) || S.lifeNext > LifeIntervalMax) S.lifeNext = LifeIntervalMax;
        }

        /// <summary>SplitMix64's finaliser: a small seed (7, 42) still starts the stream well mixed.</summary>
        static long Mix(long seed)
        {
            ulong z = (ulong)seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return (long)(z ^ (z >> 31));
        }

        double LifeRoll()
        {
            long x = S.lifeRng;
            x ^= x << 13; x ^= (long)((ulong)x >> 7); x ^= x << 17;
            S.lifeRng = x == 0 ? 0x4C1FE5EEDL : x;
            return (double)((ulong)S.lifeRng >> 11) / (1UL << 53);
        }

        double NextLifeInterval() => LifeIntervalMin + LifeRoll() * (LifeIntervalMax - LifeIntervalMin);

        /// <summary>The pool is running: the opening is over (the second ability is learned) and the finale has not begun.</summary>
        public bool LifeEventsUnlocked => !S.openingFocus && AbilitiesCount >= 2 && !S.fullOpen && string.IsNullOrEmpty(S.ending) && !S.chapterComplete && !EndingAvailable;

        /// <summary>Something else holds the player now: a draw that falls waits.</summary>
        public bool LifeEventsHeld
        {
            get
            {
                bool desk = false;
                try { desk = LifeEventHold != null && LifeEventHold(); } catch (Exception) { desk = false; }
                return desk || EpiphanyPending || CaptchaPending || ProjectAwaitingAnswer || RelicPickPending || ExamPassPending || MiniGameSurfaceActive;
            }
        }

        public bool LifeEventOpen { get { EnsureLife(); return S.lifeOpen.Length > 0; } }
        public string LifeEventId { get { EnsureLife(); return S.lifeOpen; } }
        /// <summary>Seconds until the next draw (while the pool runs and nothing is open).</summary>
        public double LifeEventNext => S.lifeNext;
        public IReadOnlyList<XgLifeRecord> LifeHistory { get { EnsureLife(); return S.lifeHistory; } }
        /// <summary>老周 owes a favour: one download comes over from his campus at once.</summary>
        public bool ZhouFavourOwed => S.lifeFavour > 0;
        /// <summary>Times 老周 was turned down (he cools a little each time; lending him the card again warms him up).</summary>
        public int ZhouCool => S.lifeZhouCool;
        /// <summary>A card is out at 老周's.</summary>
        public bool CardLent { get { EnsureLife(); foreach (var h in S.lifeHolds) if (h.id == "lend") return true; return false; } }

        double Cooldown(string id) { foreach (var c in S.lifeCooldowns) if (c.id == id) return c.until; return 0; }
        bool Cooled(string id) => S.lifeClock >= Cooldown(id);
        void SetCooldown(string id, double seconds)
        {
            var c = S.lifeCooldowns.Find(x => x.id == id);
            if (c == null) S.lifeCooldowns.Add(c = new XgLifeTimer { id = id });
            c.until = S.lifeClock + seconds;
        }

        bool HasHold(string id) { foreach (var h in S.lifeHolds) if (h.id == id) return true; return false; }
        bool HasReturn(string id) { foreach (var r in S.lifeReturns) if (r.id == id) return true; return false; }

        double LifeSecondsOfDay
        {
            get
            {
                var house = House;
                double s = house != null ? house.SecondsOfDay : TimeOfDay;
                return Finite(s) ? (s % 86400 + 86400) % 86400 : TimeOfDay;
            }
        }

        static int AddMonths(int yyyymm, int months)
        {
            int index = yyyymm / 100 * 12 + (yyyymm % 100 - 1) + months;
            return index / 12 * 100 + index % 12 + 1;
        }

        // ───────────── the clock ─────────────

        /// <summary>Hooked in <see cref="Step"/>: the timers, the house watch and the draw.</summary>
        void TickLifeEvents(double dt, IXgHost host)
        {
            if (!(dt > 0)) return;
            EnsureLife();
            lifeHouse = host as IXgHousehold;
            S.lifeClock += dt;
            if (AnyEpochActive || S.pretrainRunning) S.lifeLastTrain = S.lifeClock;
            WatchPowerMonths();
            WatchBroadbandMonth();
            TickLifeHolds(host);
            TickLifeReturns(host);
            if (!LifeEventsUnlocked || S.lifeOpen.Length > 0) return;
            S.lifeNext -= dt;
            if (S.lifeNext > 0) return;
            if (LifeEventsHeld) { S.lifeNext = LifeHeldRetry; return; }
            var pool = LifeEventsEligible(host);
            S.lifeNext = NextLifeInterval();
            if (pool.Count == 0) return;
            OpenLifeEvent(pool[Math.Min(pool.Count - 1, (int)(LifeRoll() * pool.Count))], host);
        }

        /// <summary>Ends a high-bill month into the run of high months; a normal month breaks the run.</summary>
        void WatchPowerMonths()
        {
            var house = House;
            if (house == null || !TodayKnown || !house.BillsActive) return;
            int month = Today / 100;
            if (S.lifePowerMonth == 0) S.lifePowerMonth = month;
            if (month != S.lifePowerMonth)
            {
                S.lifeHighMonths = S.lifeMonthPeak >= house.HighKwh ? S.lifeHighMonths + 1 : 0;
                S.lifeMonthPeak = 0;
                S.lifePowerMonth = month;
            }
            double kwh = house.MonthKwh;
            if (Finite(kwh) && kwh > S.lifeMonthPeak) S.lifeMonthPeak = kwh;
        }

        void WatchBroadbandMonth()
        {
            if (!TodayKnown) return;
            if (BroadbandTier == 0) S.lifeBroadbandMonth = 0;
            else if (S.lifeBroadbandMonth == 0) S.lifeBroadbandMonth = Today / 100;
        }

        void TickLifeHolds(IXgHost host)
        {
            var w = S.wiring;
            if (w == null || w.pulled == null) return;
            for (int i = S.lifeHolds.Count - 1; i >= 0; i--)
            {
                var h = S.lifeHolds[i];
                if (S.lifeClock < h.until)
                {
                    // The card is at 老周's (or off for the night): it stays unplugged.
                    if (!w.pulled.Contains(h.key)) w.pulled.Add(h.key);
                    continue;
                }
                S.lifeHolds.RemoveAt(i);
                if (h.amount <= 0) w.pulled.Remove(h.key);
                if (host != null) RefreshWiring(host);
                if (h.id == "lend")
                    Say(T("老周把" + h.name + "还回来了，用湿巾擦得锃亮，还塞了瓶冰红茶在机箱上，压着张纸条：「你这哥们，靠谱。」插回去了。", "老周 brought the " + h.nameEn + " back, wiped clean, with a bottle of iced tea on the case and a note under it: \"You're a reliable guy.\" It is plugged in again."));
                else if (h.id == "quiet")
                    Say(T(h.name + "插回去了。室友那屋没动静，应该睡熟了。", "The " + h.nameEn + " is plugged in again. Not a sound from the roommate."));
            }
        }

        void TickLifeReturns(IXgHost host)
        {
            if (host == null) return;
            for (int i = S.lifeReturns.Count - 1; i >= 0; i--)
            {
                var r = S.lifeReturns[i];
                if (S.lifeClock < r.until) continue;
                S.lifeReturns.RemoveAt(i);
                if (r.amount > 0) host.Earn(r.amount);
                if (r.id == "zhou") Say(T("老周把 ¥" + F(r.amount, "0") + " 转回来了，附言：谢了哥，回头请你撸串。你这人，靠谱。", "老周 sent the ¥" + F(r.amount, "0") + " back: \"Thanks, man. Skewers are on me. You're reliable.\""));
                else Say(T("余额宝转出 ¥" + F(r.amount, "0") + "，到账了。利息够买两瓶可乐。", "¥" + F(r.amount, "0") + " came back out of Yu'e Bao. The interest buys two Cokes."));
            }
        }

        // ───────────── the pool ─────────────

        /// <summary>The events whose conditions hold now, in the pool's order.</summary>
        public List<string> LifeEventsEligible(IXgHost host)
        {
            EnsureLife();
            if (host is IXgHousehold hh) lifeHouse = hh;
            var list = new List<string>();
            foreach (var id in LifeEventIds) if (LifeEligible(id, host)) list.Add(id);
            return list;
        }

        bool LifeEligible(string id, IXgHost host)
        {
            if (!Cooled(id)) return false;
            var rig = host as IXgRig;
            int cards = rig != null && rig.Cards != null ? rig.Cards.Count : 0;
            switch (id)
            {
                case "zhou_card":
                    return S.stage >= 3 && cards >= 2 && !HasHold("lend") && !HasHold("quiet") && S.lifeFavour == 0;
                case "bad_sectors":
                    return DiskCapacityMB > 0 && DiskUsedMB / DiskCapacityMB >= LifeDiskShare && BadSectorVictim() != null;
                case "broadband":
                    // Once a month, and not while a renewal still covers the months ahead.
                    return TodayKnown && BroadbandTier >= 1 && S.lifeBroadbandMonth > 0 && Today / 100 != S.lifeBroadbandMonth && Today / 100 >= S.broadbandBilled;
                case "landlord_meter":
                    return House != null && House.BillsActive && S.lifeHighMonths >= 2;
                case "roommate_fan":
                {
                    double t = LifeSecondsOfDay;
                    return cards >= 1 && t < 6 * 3600 && S.lifeClock - S.lifeLastTrain <= 30 && !HasHold("quiet") && !HasHold("lend");
                }
                case "forum_post":
                    return S.best != null && S.best.Count > 0 && ForumDataset() != null;
                case "zhou_loan":
                    return S.lifeZhouCool == 0 && !HasReturn("zhou") && Cooled("hoard") && Hoarding(host);
                case "licai_call":
                    return !HasReturn("yuebao") && Cooled("hoard") && Hoarding(host);
                case "zhou_favour":
                    return S.lifeFavour > 0 && FavourTarget() != null;
            }
            return false;
        }

        /// <summary>The dearest thing on offer now (tech nodes that are not locked, the next drive once the disk filled, the next line).</summary>
        public double NextBigPurchase(IXgHost host)
        {
            double best = 0;
            foreach (var n in XgCatalog.Nodes)
            {
                var st = Status(n, host);
                if (st != NodeStatus.Buyable && st != NodeStatus.TooExpensive) continue;
                double c = NodeCost(n);
                if (Finite(c) && c > best) best = c;
            }
            if (S.diskFullSeen && Finite(NextDrivePrice)) best = Math.Max(best, NextDrivePrice);
            if (BroadbandBlocker(out _) == null && Finite(NextBroadbandPrice)) best = Math.Max(best, NextBroadbandPrice);
            return best;
        }

        /// <summary>The wallet holds more than five times the dearest thing on offer.</summary>
        public bool Hoarding(IXgHost host)
        {
            if (host == null || host.Money < LifeHoardMinMoney) return false;
            double next = NextBigPurchase(host);
            return next > 0 && host.Money > LifeHoardFactor * next;
        }

        /// <summary>The dataset whose batches take the most room and has at least two in (story copies sit on their own drive).</summary>
        string BadSectorVictim()
        {
            string best = null; double most = 0;
            if (S.owned == null) return null;
            foreach (var id in S.owned)
            {
                var d = XgCatalog.Dataset(id);
                if (d == null || StoryOwned(id) || BatchesArrived(id) < 2) continue;
                double mb = BatchesArrived(id) * BatchMB(d);
                if (mb > most) { most = mb; best = id; }
            }
            return best;
        }

        string ForumDataset()
        {
            var run = Selected;
            if (run != null && XgCatalog.Dataset(run.dataset) != null) return run.dataset;
            foreach (var r in Runs) if (r != null && XgCatalog.Dataset(r.dataset) != null) return r.dataset;
            return null;
        }

        /// <summary>The favour only fetches a download with at least this many game seconds to go (about 10 GB on the starting 20M line).</summary>
        public const double FavourMinSeconds = 20;

        /// <summary>The download 老周 would bring over: the longest one still to come down the home line that fits on the disk.</summary>
        XgDownload FavourTarget()
        {
            XgDownload best = null; double most = FavourMinSeconds;
            if (S.downloadQueue == null) return null;
            foreach (var x in S.downloadQueue)
            {
                if (x.campus) continue;
                double left = SecondsLeft(x);
                if (left < most) continue;
                if (!x.ownDrive && DiskFreeMB < x.sizeMB - x.doneMB - 1e-6) continue;
                most = left; best = x;
            }
            return best;
        }

        /// <summary>Uses 老周's favour: the longest download comes over from his campus at once (false when none is owed or none fits).</summary>
        public bool RedeemZhouFavour()
        {
            EnsureLife();
            if (S.lifeFavour <= 0) return false;
            var x = FavourTarget();
            if (x == null) return false;
            S.lifeFavour = 0;
            x.doneMB = x.sizeMB;
            string name = DownloadName(x.offer);
            Arrive(x, true);
            Say(T("老周骑车送来一个 U 盘：学校机房的教育网拷的，" + name + "到了。人情还上了。", "老周 cycled over with a USB stick copied on the campus network: " + name + " is in. The favour is settled."));
            return true;
        }

        // ───────────── opening an event ─────────────

        /// <summary>
        /// Opens an event now (tests and the debug menu): only when nothing is open and its conditions hold, or with
        /// <paramref name="force"/> whatever they are. The scheduler's clock is not touched.
        /// </summary>
        public bool OpenLifeEventNow(string id, IXgHost host, bool force = false)
        {
            EnsureLife();
            if (S.lifeOpen.Length > 0 || Array.IndexOf(LifeEventIds, id) < 0) return false;
            if (!force && !LifeEligible(id, host)) return false;
            OpenLifeEvent(id, host);
            return true;
        }

        void OpenLifeEvent(string id, IXgHost host)
        {
            S.lifeOpen = id; S.lifeArg = 0; S.lifeArgKey = S.lifeArgName = S.lifeArgNameEn = "";
            var rig = host as IXgRig;
            switch (id)
            {
                case "zhou_card": case "roommate_fan":
                {
                    // The last card bought: the newest, and the loudest.
                    var cards = rig != null ? rig.Cards : null;
                    if (cards != null && cards.Count > 0)
                    {
                        int last = cards.Count - 1, k = 0;
                        for (int i = 0; i < last; i++) if (cards[i] != null && cards[last] != null && cards[i].id == cards[last].id) k++;
                        var spec = cards[last];
                        if (spec != null)
                        {
                            S.lifeArgKey = CardKey(spec.id, k);
                            S.lifeArgName = spec.name ?? "";
                            S.lifeArgNameEn = string.IsNullOrEmpty(spec.nameEn) ? S.lifeArgName : spec.nameEn;
                        }
                    }
                    break;
                }
                case "bad_sectors":
                {
                    S.lifeArgKey = BadSectorVictim() ?? "";
                    var d = XgCatalog.Dataset(S.lifeArgKey);
                    if (d != null) { S.lifeArgName = d.name; S.lifeArgNameEn = d.nameEn; S.lifeArg = Math.Ceiling(NextDrivePrice * .5 / 10) * 10; }
                    break;
                }
                case "broadband":
                    S.lifeArg = Math.Max(0, BroadbandExtraPerMonth) * 2;
                    break;
                case "forum_post":
                {
                    S.lifeArgKey = ForumDataset() ?? "";
                    var d = XgCatalog.Dataset(S.lifeArgKey);
                    if (d != null) { S.lifeArgName = d.name; S.lifeArgNameEn = d.nameEn; }
                    int next = NextAbility > 0 ? NextAbility : AbilityCount;
                    S.lifeArg = Math.Max(200, Math.Round(SamplesThreshold(next) * .02 / 100) * 100);
                    break;
                }
                case "zhou_loan":
                    S.lifeArg = host == null ? 0 : Math.Max(100, Math.Floor(Math.Min(host.Money * .2, 5000) / 100) * 100);
                    break;
                case "licai_call":
                    S.lifeArg = host == null ? 0 : Math.Max(100, Math.Floor(host.Money * .3 / 100) * 100);
                    break;
                case "zhou_favour":
                {
                    var x = FavourTarget();
                    if (x != null) { S.lifeArgKey = x.key; S.lifeArgName = DownloadName(x.offer); S.lifeArg = SecondsLeft(x); }
                    break;
                }
            }
            LifeEventOpened?.Invoke(LifeEventView(host));
        }

        /// <summary>The open event in the current language (null when none is open). Choices that cannot be paid are disabled.</summary>
        public XgLifeEvent LifeEventView(IXgHost host)
        {
            EnsureLife();
            if (S.lifeOpen.Length == 0) return null;
            double money = host != null ? host.Money : 0;
            string card = T(S.lifeArgName, S.lifeArgNameEn), name = T(S.lifeArgName, S.lifeArgNameEn);
            if (card.Length == 0) card = T("显卡", "card");
            var e = new XgLifeEvent { id = S.lifeOpen };
            XgLifeChoice C(string cid, string label, string detail, bool on = true) { var c = new XgLifeChoice { id = cid, label = label, detail = detail, enabled = on }; e.choices.Add(c); return c; }
            switch (S.lifeOpen)
            {
                case "zhou_card":
                    e.who = T("老周 · 贴吧私信", "老周 · Tieba DM"); e.title = T("老周来借显卡", "老周 wants to borrow a card");
                    // The card burns out once; asking again says why it is again (after a no, after a loan).
                    if (ChronicleHas("laozhou.card"))
                        e.body = T("老周：哥们，又得麻烦你了。上回你那张卡跑出来的数，导师问我是谁帮的忙，我说是个靠谱的哥们。这回导师让补一组对比实验，组里的卡排队排到下周。你那张" + card + "能再借我几天不？还是老规矩，原样还你，大文件我给你拷。",
                                   "老周: Man, sorry to ask again. My advisor asked who helped with the numbers from your card last time, and I said a reliable friend. Now he wants one more comparison run and the group's cards are booked until next week. Could I borrow your " + card + " again for a few days? Same deal: back as it is, and I'll copy big files for you.");
                    else if (S.lifeZhouCool > 0)
                        e.body = T("老周：还是上回那事儿……组里的卡还没修好，导师又在群里点我名了。你那张" + card + "真不能借我几天？用完原样还你。",
                                   "老周: Same thing as last time… the group's card still isn't fixed and my advisor called me out in the group chat again. Really can't lend me your " + card + " for a few days? You'll get it back as it is.");
                    else
                        e.body = T("老周：哥们，我们组那台服务器的卡烧了，导师天天催结果。你那张" + card + "能借我几天不？用完原样还你。回头学校机房有啥大文件，我拿 U 盘给你拷。",
                                   "老周: Man, the card in our group's server burned out and my advisor wants results every day. Can I borrow your " + card + " for a few days? You'll get it back as it is. And if you ever need a big file, I'll copy it for you on the campus network.");
                    C("lend", T("借给他", "Lend it"), T(card + "拔下来 10 分钟；他欠你一个人情：之后一次学校快传", "The " + card + " is out for 10 minutes; he owes you one campus copy of a download"));
                    C("refuse", T("不借", "Say no"), T("他嘴上说没事", "He says it's fine"));
                    break;
                case "bad_sectors":
                {
                    var d = XgCatalog.Dataset(S.lifeArgKey);
                    string lost = d != null ? SamplesText(BatchSamples(d)) : "0";
                    e.who = T("机箱", "The PC"); e.title = T("硬盘坏道", "Bad sectors");
                    e.body = T("机械盘咔哒咔哒响了一晚上。HD Tune 扫出一片红格子：坏道，正好压在《" + name + "》的一批数据上。",
                               "The hard disk clicked all night. HD Tune shows a patch of red blocks: bad sectors, right on a batch of " + name + ".");
                    C("backup", T("送电脑城做备份 ¥" + F(S.lifeArg, "0"), "Have it backed up at the computer market ¥" + F(S.lifeArg, "0")), T("小哥用 PE 盘拷一下午，数据都救回来", "A tech copies it off with a boot disk; nothing is lost"), money + 1e-9 >= S.lifeArg);
                    C("lose", T("那一批不要了", "Let that batch go"), T("《" + name + "》少一批（−" + lost + " 条），以后可以再买", name + " loses a batch (−" + lost + " samples); it can be bought again"));
                    break;
                }
                case "broadband":
                {
                    int tier = BroadbandTier, lower = Math.Max(0, tier - 1);
                    double save = BroadbandMonthly[tier] - BroadbandMonthly[lower];
                    e.who = T("中国电信 10000", "China Telecom 10000"); e.title = T("宽带到期", "Broadband renewal");
                    e.body = T("「您好，您家的" + BroadbandName(tier, false) + "本月到期。现在续费交两个月送一个月；不续的话下个月给您降到" + BroadbandName(lower, false) + "。」",
                               "\"Hello, your " + BroadbandName(tier, true) + " is up for renewal this month. Pay two months now and get a third free; otherwise you go down to " + BroadbandName(lower, true) + " next month.\"");
                    C("renew", T("续费 ¥" + F(S.lifeArg, "0") + "（管三个月）", "Renew ¥" + F(S.lifeArg, "0") + " (three months)"), T("三个月不用再交提速费", "No line fee for three months"), money + 1e-9 >= S.lifeArg);
                    C("drop", T("降一档", "Drop a tier"), T("每月省 ¥" + F(save, "0") + "，下载变慢", "Saves ¥" + F(save, "0") + " a month; downloads slow down"));
                    break;
                }
                case "landlord_meter":
                    e.who = T("房东", "Landlord"); e.title = T("房东查电表", "The landlord reads the meter");
                    e.body = T("房东老王拿着手电筒来抄表：「小伙子，你这电表连着两个月转得跟电风扇似的，屋里开厂呢？」",
                               "Old Wang the landlord came with a torch to read the meter: \"Young man, two months running your meter has spun like a fan. Running a factory in there?\"");
                    C("explain", T("解释：在做科研", "Explain: it's research"), T("他信不信看运气；不信要交 ¥" + F(LifeLandlordFine, "0") + " 线路费，之后说话要小心", "He may or may not buy it; if not, a ¥" + F(LifeLandlordFine, "0") + " wiring fee, and he'll watch you"));
                    C("pay", T("多交 ¥" + F(LifeLandlordExtra, "0"), "Pay ¥" + F(LifeLandlordExtra, "0") + " extra"), T("说声不好意思，这事就过去了", "Say sorry; that settles it"), money + 1e-9 >= LifeLandlordExtra);
                    break;
                case "roommate_fan":
                {
                    e.who = T("室友", "Roommate"); e.title = T("室友被风扇吵醒", "The fans woke the roommate");
                    e.body = T("门被敲响。室友顶着鸡窝头：「哥，你这电脑跟吹风机似的，我明早八点还要打卡……」",
                               "A knock on the door. The roommate, hair like a bird's nest: \"Man, your PC sounds like a hair dryer, and I clock in at eight…\"");
                    C("throttle", T("降频：先拔掉" + card, "Throttle: unplug the " + card), T("最吵的那张卡停 5 分钟", "The loudest card rests for 5 minutes"));
                    C("redbull", T("送他一箱红牛 ¥" + F(LifeRedBull, "0"), "Give him a case of Red Bull ¥" + F(LifeRedBull, "0")), T("他戴上耳塞回去睡了，训练照跑", "He puts in earplugs; training goes on"), money + 1e-9 >= LifeRedBull);
                    break;
                }
                case "forum_post":
                {
                    double pay = ForumMoney;
                    e.who = T("摆渡贴吧 · 人工智能吧", "Bodu Tieba · AI forum"); e.title = T("帖子有人回了", "Replies on your thread");
                    e.body = S.stage >= 4
                        ? T("你在人工智能吧发的帖子「自己在家用显卡训了个小网络」被顶上了首页，三十多楼，有人在楼里直接叫你大神。有个研究生说可以把他们组标好的数据打包发你，还有人说要打赏。",
                            "Your thread on the AI forum, \"Trained a little network at home on my own GPU\", hit the front page with thirty-odd replies, and some call you a big shot outright. A grad student offers his group's labelled data, and someone wants to tip you.")
                        : T("你在人工智能吧发的帖子「自己在家用显卡训了个小网络」被顶上了首页，三十多楼。有个研究生说可以把他们组标好的数据打包发你，还有人说要打赏。",
                            "Your thread on the AI forum, \"Trained a little network at home on my own GPU\", hit the front page with thirty-odd replies. A grad student offers his group's labelled data, and someone wants to tip you.");
                    C("data", T("认真回帖，要数据", "Answer properly, take the data"), T("+" + SamplesText(S.lifeArg) + " 条样本（" + name + "）", "+" + SamplesText(S.lifeArg) + " samples (" + name + ")"));
                    C("tip", T("回个谢谢，收下打赏", "Say thanks, take the tip"), "+¥" + F(pay, "0"));
                    break;
                }
                case "zhou_loan":
                    e.who = T("老周 · 贴吧私信", "老周 · Tieba DM"); e.title = T("老周开口借钱", "老周 asks for a loan");
                    e.body = T("老周：那个……哥们，这个月补助还没发，能先借我 ¥" + F(S.lifeArg, "0") + " 不？发了就还你。\n（账上的钱够买五回下一件大东西了。钱放着不会自己学东西。）",
                               "老周: Um… my stipend is late this month. Could you spot me ¥" + F(S.lifeArg, "0") + "? I'll pay it back when it comes.\n(The wallet holds five times the next big purchase. Money left lying around teaches nothing.)");
                    C("lend", T("借给他", "Lend it"), T("¥" + F(S.lifeArg, "0") + "，10 分钟后还你", "¥" + F(S.lifeArg, "0") + ", back in 10 minutes"), money + 1e-9 >= S.lifeArg);
                    C("decline", T("手头也紧，下次吧", "Tight myself, next time"), T("钱留着买东西", "Keep it for things to buy"));
                    break;
                case "licai_call":
                {
                    double interest = Math.Max(1, Math.Round(S.lifeArg * .01));
                    e.who = T("陌生号码 021-6xxx", "Unknown number 021-6xxx"); e.title = T("理财推销电话", "A wealth-management call");
                    e.body = T("「先生您好，我们是某某财富，P2P 年化 12%，保本保息。您账上的闲钱放着也是贬值……」\n（账上的钱够买五回下一件大东西了。）",
                               "\"Hello sir, this is So-and-so Wealth. P2P, 12% a year, principal guaranteed. Idle money only loses value…\"\n(The wallet holds five times the next big purchase.)");
                    C("hangup", T("挂了，钱拿去买设备", "Hang up; spend it on gear"), T("只是挂电话", "Just hangs up"));
                    C("yuebao", T("存一笔进余额宝", "Park some in Yu'e Bao"), T("¥" + F(S.lifeArg, "0") + " 存 10 分钟，回来多 ¥" + F(interest, "0"), "¥" + F(S.lifeArg, "0") + " for 10 minutes, back with ¥" + F(interest, "0") + " more"), money + 1e-9 >= S.lifeArg);
                    break;
                }
                case "zhou_favour":
                    e.who = T("老周 · 贴吧私信", "老周 · Tieba DM"); e.title = T("老周：我帮你拷吧", "老周: let me copy it");
                    e.body = T("老周：看你那个" + name + "还得下 " + ClockText(S.lifeArg) + "。我们机房是教育网，我拿 U 盘拷一份，回头骑车给你送过去？",
                               "老周: Your " + name + " still needs " + ClockText(S.lifeArg) + ". Our lab is on the campus network; I'll copy it to a USB stick and cycle it over?");
                    C("redeem", T("麻烦你了", "Yes please"), T("用掉他欠的人情，马上到", "Uses the favour he owes; it arrives at once"));
                    C("later", T("先不用，人情留着", "Not now, keep the favour"), T("下回再说", "Another time"));
                    break;
            }
            return e;
        }

        double ForumMoney => Math.Max(100, 150 * Math.Max(1, S.stage));

        // ───────────── answering ─────────────

        /// <summary>Answers the open event by choice id (false when none is open or the choice is not available).</summary>
        public bool AnswerLifeEvent(string choiceId, IXgHost host)
        {
            var v = LifeEventView(host);
            if (v == null) return false;
            for (int i = 0; i < v.choices.Count; i++) if (v.choices[i].id == choiceId) return AnswerLifeEvent(i, host);
            return false;
        }

        /// <summary>Answers the open event with its <paramref name="index"/>th choice; the result is written into the state at once.</summary>
        public bool AnswerLifeEvent(int index, IXgHost host)
        {
            var v = LifeEventView(host);
            if (v == null || index < 0 || index >= v.choices.Count || !v.choices[index].enabled) return false;
            string id = v.id, choice = v.choices[index].id;
            string result = ApplyLifeChoice(id, choice, host);
            if (result == null) return false;
            S.lifeOpen = "";
            S.lifeHistory.Add(new XgLifeRecord { id = id, choice = choice, date = TodayKnown ? Today : 0, at = S.lifeClock });
            while (S.lifeHistory.Count > LifeHistoryKept) S.lifeHistory.RemoveAt(0);
            Say(result);
            LifeEventAnswered?.Invoke(id, choice, result);
            return true;
        }

        string ApplyLifeChoice(string id, string choice, IXgHost host)
        {
            string card = T(S.lifeArgName, S.lifeArgNameEn), name = card;
            if (card.Length == 0) card = T("显卡", "the card");
            double zhouWait = 1500 * (1 + S.lifeZhouCool);
            switch (id + "/" + choice)
            {
                case "zhou_card/lend":
                    SetCooldown(id, zhouWait);
                    S.lifeFavour = 1;
                    NoteChronicle("laozhou.card");
                    S.lifeZhouCool = Math.Max(0, S.lifeZhouCool - 1);
                    if (S.lifeArgKey.Length > 0) Unplug("lend", S.lifeArgKey, LifeLendSeconds, host);
                    return T("把" + card + "装进原来的纸盒给了老周。他拍拍你肩膀：「欠你一回。你这哥们，靠谱。」", "You boxed up the " + card + " for 老周. He pats your shoulder: \"I owe you one. You're a reliable guy.\"");
                case "zhou_card/refuse":
                    S.lifeZhouCool = Math.Min(5, S.lifeZhouCool + 1);
                    SetCooldown(id, 1500 * (1 + S.lifeZhouCool));
                    return T("老周：「行，没事，我再问问别人。」他回得比平时慢了半拍。", "老周: \"Sure, no problem, I'll ask around.\" His reply came a beat slower than usual.");
                case "bad_sectors/backup":
                    if (host == null || !host.Spend(S.lifeArg)) return null;
                    S.totalSpent += S.lifeArg;
                    SetCooldown(id, 1200);
                    return T("电脑城的小哥拷了一下午，坏道上的数据都救回来了，顺手给你标了块「勿用」。", "The tech at the computer market spent the afternoon on it: every byte saved, the bad patch marked off.");
                case "bad_sectors/lose":
                {
                    SetCooldown(id, 1200);
                    var d = XgCatalog.Dataset(S.lifeArgKey);
                    if (d == null || StoryOwned(d.id) || BatchesArrived(d.id) < 1)
                        return T("坏道那块扫了几遍，好像又能读了。这回算运气好。", "After a few scans the bad patch reads again. Lucky this time.");
                    int bought = BatchesBought(d.id), arrived = BatchesArrived(d.id);
                    SetCount(S.batchesBought, d.id, Math.Max(0, bought - 1));
                    SetCount(S.batchesArrived, d.id, Math.Max(0, arrived - 1));
                    foreach (var run in Runs) if (run.dataset == d.id) Evaluate(run);
                    return T("《" + d.name + "》少了一批（−" + SamplesText(BatchSamples(d)) + " 条）。坏道那块已经屏蔽，想补可以再买那一批。",
                             d.nameEn + " lost a batch (−" + SamplesText(BatchSamples(d)) + " samples). The bad patch is fenced off; the batch can be bought again.");
                }
                case "broadband/renew":
                {
                    if (host == null || !host.Spend(S.lifeArg)) return null;
                    S.totalSpent += S.lifeArg;
                    int month = Today / 100;
                    S.broadbandBilled = AddMonths(Math.Max(month, S.broadbandBilled), 3);
                    S.lifeBroadbandMonth = month;
                    return T("续好了：" + BroadbandName(BroadbandTier, false) + "接下来三个月不用再交提速费。", "Renewed: no line fee on the " + BroadbandName(BroadbandTier, true) + " for the next three months.");
                }
                case "broadband/drop":
                    S.broadband = Math.Max(0, BroadbandTier - 1);
                    S.lifeBroadbandMonth = Today / 100;
                    return T("降到了" + BroadbandName(BroadbandTier, false) + "：下载 " + F(LineMBps, "0.#") + " MB/s，月租省下来了。", "Down to " + BroadbandName(BroadbandTier, true) + ": " + F(LineMBps, "0.#") + " MB/s, a smaller bill.");
                case "landlord_meter/explain":
                {
                    S.lifeHighMonths = 0;
                    bool believed = LifeRoll() < (S.lifeLandlordWary ? LifeExplainWaryChance : LifeExplainChance);
                    S.lifeLandlordWary = true;
                    if (believed && S.stage >= 5) EarnSecret("life.praise.landlord");
                    if (believed) return S.stage >= 5 ? T("老王听完摆摆手：「你这孩子有出息，租给你我放心。别把线烧了就行。」走的时候又回头看了一眼电表。", "Old Wang waves it off: \"You'll go far, kid. I'm easy renting to you. Just don't burn the wiring.\" He glances back at the meter on his way out.")
                        : S.stage >= 3 ? T("老王听完摆摆手：「小伙子在搞正事，我看得出来。别把线烧了就行。」走的时候又回头看了一眼电表。", "Old Wang waves it off: \"You're up to something serious, kid, I can tell. Just don't burn the wiring.\" He glances back at the meter on his way out.")
                        : T("老王听完摆摆手：「搞科研好，别把线烧了就行。」走的时候又回头看了一眼电表。", "Old Wang waves it off: \"Research, good. Just don't burn the wiring.\" He glances back at the meter on his way out.");
                    double fine = host != null ? Math.Min(LifeLandlordFine, Math.Max(0, host.Money)) : 0;
                    if (fine > 0 && host.Spend(fine)) S.totalSpent += fine;
                    if (!(fine > 0)) return T("老王不信：「科研？科研用得着这么多电？」看你钱包是空的，摆摆手说下回再这样就涨房租。", "Old Wang isn't having it: \"Research uses this much power?\" Seeing your empty wallet, he only says next time the rent goes up.");
                    return T("老王不信：「科研？科研用得着这么多电？」收了 ¥" + F(fine, "0") + " 线路费，说下回再这样就涨房租。", "Old Wang isn't having it: \"Research uses this much power?\" He takes ¥" + F(fine, "0") + " for the wiring and says next time the rent goes up.");
                }
                case "landlord_meter/pay":
                    if (host == null || !host.Spend(LifeLandlordExtra)) return null;
                    S.totalSpent += LifeLandlordExtra;
                    S.lifeHighMonths = 0;
                    S.lifeLandlordWary = false;
                    return T("多塞了 ¥" + F(LifeLandlordExtra, "0") + "。老王把钱折好放进衬衫口袋：「电线我下礼拜找人看看。」", "You hand over ¥" + F(LifeLandlordExtra, "0") + " extra. Old Wang folds it into his shirt pocket: \"I'll get the wiring checked next week.\"");
                case "roommate_fan/throttle":
                    SetCooldown(id, 1200);
                    if (S.lifeArgKey.Length > 0) Unplug("quiet", S.lifeArgKey, LifeQuietSeconds, host);
                    return S.stage >= 4 ? T("把" + card + "的电源拔了，机箱安静了一半。室友说了句「谢了。你也别熬太晚，你这么拼，我看着都累」，门关上了。", "You unplug the " + card + "; the case is half as loud. \"Thanks. Don't stay up too late either. Watching you work this hard tires me out,\" says the roommate, and the door closes.")
                        : T("把" + card + "的电源拔了，机箱安静了一半。室友说了句「谢了」，门关上了。", "You unplug the " + card + "; the case is half as loud. \"Thanks,\" says the roommate, and the door closes.");
                case "roommate_fan/redbull":
                    if (host == null || !host.Spend(LifeRedBull)) return null;
                    S.totalSpent += LifeRedBull;
                    SetCooldown(id, 2400);
                    return S.stage >= 4 ? T("室友抱着一箱红牛回屋了：「得，你开吧，我戴耳塞。你在折腾的那玩意儿，我虽然不懂，但看你这半年没停过，我服。」", "The roommate carries off a case of Red Bull: \"Fine, keep it running, I've got earplugs. I don't get whatever you're building, but you haven't stopped in half a year, and I respect that.\"")
                        : T("室友抱着一箱红牛回屋了：「得，你开吧，我戴耳塞。」", "The roommate carries off a case of Red Bull: \"Fine, keep it running. I've got earplugs.\"");
                case "forum_post/data":
                {
                    SetCooldown(id, 1200);
                    string ds = XgCatalog.Dataset(S.lifeArgKey) != null ? S.lifeArgKey : ForumDataset();
                    if (ds != null) AddGameSamples(ds, (int)S.lifeArg);
                    if (S.stage >= 4) EarnSecret("life.praise.forum");
                    return T("研究生发来一个压缩包：" + SamplesText(S.lifeArg) + " 条标好的 " + name + "。你回了一长串感谢。", "The grad student sends a zip: " + SamplesText(S.lifeArg) + " labelled " + name + " samples. You reply with a long thank-you.");
                }
                case "forum_post/tip":
                {
                    SetCooldown(id, 1200);
                    double pay = ForumMoney;
                    if (host != null) { host.Earn(pay); S.totalIncome += pay; }
                    if (S.stage >= 4) EarnSecret("life.praise.forum");
                    return S.stage >= 4 ? T("支付宝到账 ¥" + F(pay, "0") + "，附言：大神加油，期待下一帖。", "¥" + F(pay, "0") + " arrives on Alipay: \"Keep going, big shot. Waiting for your next post.\"")
                        : T("支付宝到账 ¥" + F(pay, "0") + "，附言：楼主加油。", "¥" + F(pay, "0") + " arrives on Alipay: \"Keep going, OP.\"");
                }
                case "zhou_loan/lend":
                    if (host == null || !host.Spend(S.lifeArg)) return null;
                    SetCooldown("hoard", 1200); SetCooldown(id, 2400);
                    S.lifeReturns.Add(new XgLifeTimer { id = "zhou", amount = S.lifeArg, until = S.lifeClock + LifeReturnSeconds });
                    return T("转给老周 ¥" + F(S.lifeArg, "0") + "。他回了个抱拳的表情。", "Sent 老周 ¥" + F(S.lifeArg, "0") + ". He replies with a fist-and-palm salute.");
                case "zhou_loan/decline":
                    SetCooldown("hoard", 1200); SetCooldown(id, 2400);
                    return T("老周：「懂，都不容易。」顺手发了个淘货上显卡降价的链接。", "老周: \"Get it, times are tight.\" He drops a link to a GPU sale on Taohuo.");
                case "licai_call/hangup":
                    SetCooldown("hoard", 1200); SetCooldown(id, 2400);
                    return T("挂了。这年头 P2P 跑路的新闻天天有，钱还是换成显卡和数据踏实。", "Hung up. P2P platforms vanish in the news every week; cards and data feel safer.");
                case "licai_call/yuebao":
                {
                    if (host == null || !host.Spend(S.lifeArg)) return null;
                    SetCooldown("hoard", 1200); SetCooldown(id, 2400);
                    double interest = Math.Max(1, Math.Round(S.lifeArg * .01));
                    S.lifeReturns.Add(new XgLifeTimer { id = "yuebao", amount = S.lifeArg + interest, until = S.lifeClock + LifeReturnSeconds });
                    return T("挂了推销，转了 ¥" + F(S.lifeArg, "0") + " 进余额宝。十分钟后连本带利转出来。", "Hung up on the pitch and put ¥" + F(S.lifeArg, "0") + " in Yu'e Bao. It comes back with interest in ten minutes.");
                }
                case "zhou_favour/redeem":
                    SetCooldown(id, 300);
                    if (!RedeemZhouFavour()) return T("老周：「那个文件你那边已经下好了？行，人情先记着。」", "老周: \"Already downloaded? Fine, the favour stays on the books.\"");
                    return T("U 盘插上，进度条一下走满。", "The stick goes in and the bar fills at once.");
                case "zhou_favour/later":
                    SetCooldown(id, 900);
                    return T("老周：「成，啥时候要说一声。」", "老周: \"Sure, just say when.\"");
            }
            return null;
        }

        /// <summary>Unplugs a card on the 接线 page for a while (it is put back by <see cref="TickLifeHolds"/>).</summary>
        void Unplug(string id, string key, double seconds, IXgHost host)
        {
            var w = S.wiring;
            if (w == null || w.pulled == null) return;
            bool already = w.pulled.Contains(key);
            S.lifeHolds.Add(new XgLifeTimer { id = id, key = key, name = S.lifeArgName, nameEn = S.lifeArgNameEn, until = S.lifeClock + seconds, amount = already ? 1 : 0 });
            if (!already) w.pulled.Add(key);
            if (host != null) RefreshWiring(host);
        }
    }
}
