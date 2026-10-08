using System;
using System.Collections.Generic;
using LingGuangV05.Core.Hardware;

namespace LingGuangV05.Core
{
    /// <summary>The kinds of household bill (the 结算 ledger lists them).</summary>
    public enum BillKind { Rent, Power, IdlePower, Aircon, Broadband, Repair, Breaker, Ticket }

    /// <summary>
    /// Rent, bills and bankruptcy (经济压力与破产). From the lab's first day (1 June) the house pays rent for every
    /// calendar day the 2016 calendar moves (¥300, ¥350 from October), broadband on the first of each month, the air
    /// conditioner in July and August, the electricity of every training round at the month's 阶梯电价 tier, an
    /// electrician after each breaker trip, card repairs after heavy training, and the train ticket of a promised
    /// weekend trip. Bills may take the wallet below zero; purchases never do.
    /// While the wallet is below zero the daily settlement (every <see cref="GameConfig.dayLengthSeconds"/> of play)
    /// counts the days: the first the landlord texts, the second he cuts the power (hand labelling still works), the
    /// third the computer is sold (<see cref="GameState.bankrupt"/>, the failure ending). Money back above zero
    /// clears it at once. Nothing here applies before the first rent, so older tests and the prologue are unchanged.
    /// </summary>
    public sealed partial class ChapterOneSim
    {
        public const int EconomySchema = 1;
        /// <summary>Settlements in debt before the landlord texts, cuts the power, and the computer is sold.</summary>
        public const int DebtTextDay = 1, DebtCutDay = 2, DebtSaleDay = 3;
        /// <summary>The day rent goes up (the landlord's YY message).</summary>
        public static readonly DateTime RentRiseDate = new DateTime(2016, 10, 1);

        /// <summary>The lab's first day (1 June): the first rent is due on it.</summary>
        public static int FirstRentDay => GameCalendar.DayIndex(GameCalendar.FirstDay(1));

        /// <summary>Rent is being paid: bills, debt and bankruptcy apply.</summary>
        public bool EconomyActive => S.rentDay > 0;
        /// <summary>What the wallet is short by (0 when not in debt).</summary>
        public double Debt => Math.Max(0, -S.money);

        static string T(string zh, string en) => Lang.English ? en : zh;
        static string Yuan(double v) => "¥" + Math.Round(v).ToString("0", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>The rent of a calendar day.</summary>
        public double RentFor(DateTime date) => date.Date >= RentRiseDate ? Config.rentPerDayFromOctober : Config.rentPerDay;

        /// <summary>Additive fields of older saves; an older save starts paying rent from the day after the one it is on.</summary>
        void MigrateEconomy()
        {
            if (S.repairs == null) S.repairs = new List<CardRepair>();
            S.repairs.RemoveAll(r => r == null || !HardwareCatalog.Ownable(r.model) || !Finite(r.readyAt) || r.readyAt < 0);
            if (S.bills == null) S.bills = new HouseBills();
            if (S.billsToday == null) S.billsToday = new HouseBills();
            if (S.economyVersion >= EconomySchema) return;
            S.economyVersion = EconomySchema;
            int today = GameCalendar.CurrentDay(S);
            if (today >= FirstRentDay)
            {
                S.rentDay = today;
                S.broadbandMonth = MonthOf(today);
                // The old unpaid-bill outage becomes debt in the one wallet.
                if (S.unpaidPower) { S.money -= S.billDue; S.billDue = 0; S.unpaidPower = false; }
            }
        }

        static int MonthOf(int day) { var d = GameCalendar.DateOf(day); return d.Year * 100 + d.Month; }

        // ───────────── charging ─────────────

        void Bill(BillKind kind, double amount)
        {
            if (!Finite(amount) || amount <= 0) return;
            S.money = Math.Max(-ResourceCeiling, S.money - amount);
            int today = GameCalendar.CurrentDay(S);
            if (S.billsDay != today || S.billsToday == null) { S.billsDay = today; S.billsToday = new HouseBills(); }
            if (S.bills == null) S.bills = new HouseBills();
            Add(S.bills, kind, amount);
            Add(S.billsToday, kind, amount);
        }

        static void Add(HouseBills b, BillKind kind, double amount)
        {
            switch (kind)
            {
                case BillKind.Rent: b.rent += amount; break;
                case BillKind.Power: b.power += amount; break;
                case BillKind.IdlePower: b.idlePower += amount; break;
                case BillKind.Aircon: b.aircon += amount; break;
                case BillKind.Broadband: b.broadband += amount; break;
                case BillKind.Repair: b.repair += amount; break;
                case BillKind.Breaker: b.breaker += amount; break;
                default: b.ticket += amount; break;
            }
        }

        /// <summary>
        /// Rent (and broadband, the summer air conditioner, a booked train ticket) for every calendar day reached since
        /// the last one paid. Runs at the start of every tick; the calendar itself is moved by the lab.
        /// </summary>
        public void SettleCalendar()
        {
            if (S.bankrupt) return;
            int today = GameCalendar.CurrentDay(S), first = FirstRentDay;
            if (today < first) return;
            if (S.rentDay < first - 1) S.rentDay = first - 1;
            while (S.rentDay < today) { S.rentDay++; ChargeDay(S.rentDay); }
            if (S.ticketDay > 0 && today >= S.ticketDay)
            {
                S.ticketDay = 0;
                Bill(BillKind.Ticket, Config.weekendTicket);
                LastMessage = T("周末的火车票 " + Yuan(Config.weekendTicket) + " 扣了。", "The weekend train ticket, " + Yuan(Config.weekendTicket) + ", is paid.");
                Raise("ticket.paid");
            }
        }

        void ChargeDay(int day)
        {
            var date = GameCalendar.DateOf(day);
            int month = date.Year * 100 + date.Month;
            if (S.broadbandMonth != month) { S.broadbandMonth = month; Bill(BillKind.Broadband, Config.broadbandPerMonth); }
            Bill(BillKind.Rent, RentFor(date));
            if (date.Month == 7 || date.Month == 8) Bill(BillKind.Aircon, Config.summerAirconPerDay);
            if (date.Date == RentRiseDate) Raise("rent.raised");
        }

        // ───────────── training electricity and wear ─────────────

        /// <summary>Watts the plugged-in cards draw while training.</summary>
        public double TrainingWatts => Math.Max(0, CardWatts - UnpluggedWatts) * Config.trainingLoad;

        /// <summary>kWh of a training round (or of pre-training) lasting this many seconds.</summary>
        public double TrainingKwh(double seconds) => TrainingWatts / 1000 * Math.Max(0, seconds) * Config.trainingHoursPerSecond;

        /// <summary>The price of <paramref name="kwh"/> more this month, after <paramref name="usedBefore"/> kWh, at 2016's three tiers.</summary>
        public static double TieredCost(double usedBefore, double kwh, GameConfig c)
        {
            if (c == null || !(kwh > 0)) return 0;
            double at = Math.Max(0, usedBefore), end = at + kwh, cost = 0;
            double one = Math.Max(0, Math.Min(end, c.tierOneKwh) - at);
            double two = Math.Max(0, Math.Min(end, c.tierTwoKwh) - Math.Max(at, c.tierOneKwh));
            double three = Math.Max(0, end - Math.Max(at, c.tierTwoKwh));
            cost += one * c.electricityPrice + two * c.electricityPrice * c.tierTwoFactor + three * c.electricityPrice * c.tierThreeFactor;
            return cost;
        }

        /// <summary>The month's training electricity so far (0 once a new calendar month starts).</summary>
        public double MonthKwh => S.kwhMonth == MonthOf(GameCalendar.CurrentDay(S)) ? S.monthKwh : 0;

        /// <summary>What a training round of this length would cost now (the training page shows it).</summary>
        public double TrainingRoundPrice(double seconds) => TrainingRoundPrice(seconds, 1, 1);

        /// <summary>The same with the lab's scaling (see <see cref="BillTraining(double,double,double,double)"/>).</summary>
        public double TrainingRoundPrice(double seconds, double priceFactor, double energyFactor) =>
            EconomyActive ? TieredCost(MonthKwh, TrainingKwh(seconds) * energyFactor, Config) * priceFactor : 0;

        /// <summary>
        /// A training round (or a slice of pre-training) ran for this many seconds: bills its electricity at the
        /// month's tier, and wears the cards. Before the first rent it only adds to the old daily bill.
        /// </summary>
        public double BillTraining(double seconds) => BillTraining(seconds, 1, 1, 1);

        /// <summary>
        /// The same, scaled by what the lab uses: <paramref name="priceFactor"/> on the price (off-peak power),
        /// <paramref name="energyFactor"/> on the kWh (cuDNN: they also fill the month's tier more slowly), and
        /// <paramref name="wearFactor"/> on the card burn-out chance (a cooler halves it, fresh paste stops it).
        /// </summary>
        public double BillTraining(double seconds, double priceFactor, double energyFactor, double wearFactor)
        {
            if (!Finite(seconds) || seconds <= 0 || S.bankrupt) return 0;
            if (!Finite(priceFactor) || priceFactor < 0) priceFactor = 1;
            if (!Finite(energyFactor) || energyFactor < 0) energyFactor = 1;
            if (!Finite(wearFactor) || wearFactor < 0) wearFactor = 1;
            if (!EconomyActive)
            {
                S.energyKwh += TrainingWatts / 1000 * 24 * seconds / Config.dayLengthSeconds * energyFactor;
                return 0;
            }
            double kwh = TrainingKwh(seconds) * energyFactor;
            int month = MonthOf(GameCalendar.CurrentDay(S));
            if (S.kwhMonth != month) { S.kwhMonth = month; S.monthKwh = 0; }
            double cost = TieredCost(S.monthKwh, kwh, Config) * priceFactor;
            S.monthKwh = Math.Min(ResourceCeiling, S.monthKwh + kwh);
            Bill(BillKind.Power, cost);
            Wear(seconds, wearFactor);
            return cost;
        }

        /// <summary>The 阶梯电价 tier the month's training electricity is in (1–3).</summary>
        public int PowerTier => MonthKwh >= Config.tierTwoKwh ? 3 : MonthKwh >= Config.tierOneKwh ? 2 : 1;

        /// <summary>
        /// Non-stop training wears the cards: the streak grows with every slice and a card may burn out once it is
        /// long enough. A cooler or fresh paste scales the chance (<paramref name="wearFactor"/>); 0 never burns one.
        /// </summary>
        void Wear(double seconds, double wearFactor = 1)
        {
            if (S.gameSeconds - S.lastTrainingAt > Config.wearGapSeconds) S.trainingStreak = 0;
            S.trainingStreak = Math.Min(ResourceCeiling, S.trainingStreak + seconds);
            S.lastTrainingAt = S.gameSeconds;
            if (S.trainingStreak < Config.wearAfterSeconds || S.gpuModels.Count < 2 || wearFactor <= 0) return;
            if (NextUnit() >= Config.wearChancePerSecond * seconds * wearFactor) return;
            BreakCard();
        }

        /// <summary>
        /// The cards are hot: two or more of them, training non-stop for long enough that every further second may burn
        /// one out. The training page warns about it.
        /// </summary>
        public bool WearRiskLive => EconomyActive && S.gpuModels.Count >= 2 && S.trainingStreak >= Config.wearAfterSeconds
            && S.gameSeconds - S.lastTrainingAt <= Config.wearGapSeconds;

        /// <summary>The cards cool down: the run of non-stop training starts again (fresh thermal paste).</summary>
        public void CoolCards() { S.trainingStreak = 0; }

        /// <summary>The hardest-working card (the most watts) burns out and goes to the repair shop. Never the last one.</summary>
        public bool BreakCard()
        {
            if (S.gpuModels.Count < 2) return false;
            int pick = -1; double most = -1;
            for (int i = 0; i < S.gpuModels.Count; i++)
            {
                var c = HardwareCatalog.Gpu(S.gpuModels[i]);
                if (c == null || MemoryUsed > CardVram - c.vramMB + Epsilon) continue;
                if (c.watts >= most) { most = c.watts; pick = i; }
            }
            if (pick < 0) return false;
            var g = HardwareCatalog.Gpu(S.gpuModels[pick]);
            S.gpuModels.RemoveAt(pick); S.gpuCount--;
            S.repairs.Add(new CardRepair { model = g.id, readyAt = S.gameSeconds + Config.repairSeconds });
            double fee = RepairFee(g);
            if (EconomyActive) Bill(BillKind.Repair, fee);
            LastMessage = T(g.name + " 烧了：连着练太久，送修 " + Yuan(fee) + "，修好之前少一张卡。", "The " + g.nameEn + " burnt out after training non-stop: repairs cost " + Yuan(fee) + ", and it is gone until they are done.");
            Raise("gpu.broken", g.id);
            Notify();
            return true;
        }

        public double RepairFee(GpuModel g) => g == null ? 0 : Math.Round(Config.repairShare * Math.Max(g.price, g.usedPrice));

        void ReturnRepairedCards()
        {
            if (S.repairs == null || S.repairs.Count == 0) return;
            for (int i = 0; i < S.repairs.Count; i++)
            {
                var r = S.repairs[i];
                var g = HardwareCatalog.Gpu(r.model);
                if (r.readyAt > S.gameSeconds || !SlotFree || g == null) continue;
                S.repairs.RemoveAt(i--);
                AddCard(g);
                LastMessage = T(g.name + " 修好送回来了。", "The " + g.nameEn + " is back from the repair shop.");
                Raise("gpu.repaired", g.id);
            }
        }

        // ───────────── 晴雯's weekend ─────────────

        /// <summary>
        /// He promised 晴雯 the weekend: the train ticket is paid on the coming Saturday (today if it is Saturday).
        /// One trip at a time; false when one is already booked or the calendar has not started.
        /// </summary>
        public bool BookWeekendTrip()
        {
            int today = GameCalendar.CurrentDay(S);
            if (today < FirstRentDay || S.ticketDay > 0 || S.bankrupt) return false;
            var date = GameCalendar.DateOf(today);
            int toSaturday = ((int)DayOfWeek.Saturday - (int)date.DayOfWeek + 7) % 7;
            S.ticketDay = today + toSaturday;
            return true;
        }

        // ───────────── the daily settlement and bankruptcy ─────────────

        /// <summary>The daily settlement once rent is being paid: the idle power bill goes on the wallet, then the debt days count.</summary>
        void SettleDayWithRent()
        {
            double idle = S.billDue;
            S.billDue = 0; S.unpaidPower = false;
            if (idle > Epsilon) Bill(BillKind.IdlePower, idle);
            if (S.money >= -Epsilon) { ClearDebtIfPaid(); return; }
            S.debtDays++;
            string owed = Yuan(Debt);
            if (S.debtDays == DebtTextDay)
            {
                LastMessage = T("房东：房租该交了（欠 " + owed + "）。", "Landlord: the rent is due (" + owed + " owed).");
                Raise("rent.due", owed);
            }
            else if (S.debtDays == DebtCutDay)
            {
                S.landlordCut = true;
                LastMessage = T("房东拉了电闸：欠 " + owed + "，再不交就别住了。显卡全停，手动标注还能挣钱。", "The landlord cut the power: " + owed + " owed. Pay or move out. Every card has stopped; hand labelling still pays.");
                Raise("power.cut", owed);
            }
            else SellComputer();
        }

        /// <summary>Money back to zero or above ends the landlord's countdown and brings the power back.</summary>
        void ClearDebtIfPaid()
        {
            if (S.bankrupt || S.debtDays == 0 && !S.landlordCut || S.money < -Epsilon) return;
            bool wasCut = S.landlordCut;
            S.debtDays = 0; S.landlordCut = false;
            LastMessage = T("房租交上了。" + (wasCut ? "来电了。" : ""), "The rent is paid." + (wasCut ? " The power is back." : ""));
            Raise("rent.settled");
        }

        /// <summary>What the computer and every card fetch second-hand today.</summary>
        public double ComputerSaleValue()
        {
            var today = GameCalendar.Now(S).Date;
            double sum = Config.computerResale;
            foreach (string id in S.gpuModels) sum += HardwareCatalog.UsedPrice(HardwareCatalog.Gpu(id), today);
            if (S.repairs != null) foreach (var r in S.repairs) sum += HardwareCatalog.UsedPrice(HardwareCatalog.Gpu(r.model), today);
            return Math.Floor(sum / 100) * 100;
        }

        /// <summary>The third day in debt: the computer is sold to pay the rent. The failure ending plays on the desktop.</summary>
        void SellComputer()
        {
            if (S.bankrupt) return;
            S.soldFor = ComputerSaleValue();
            S.money = Math.Min(ResourceCeiling, S.money + S.soldFor);
            S.bankrupt = true; S.landlordCut = false; S.jobEnabled = false;
            LastMessage = T("电脑卖了 " + Yuan(S.soldFor) + "。房租交上了。", "The computer sold for " + Yuan(S.soldFor) + ". The rent is paid.");
            Raise("bankrupt", Yuan(S.soldFor));
        }
    }
}
