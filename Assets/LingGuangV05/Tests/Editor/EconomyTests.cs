using System;
using NUnit.Framework;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;

namespace LingGuangV05.Tests
{
    /// <summary>Rent, bills and bankruptcy (经济压力与破产, ChapterOneSim.Bills.cs).</summary>
    public sealed class EconomyTests
    {
        static int Day(int month, int day) => GameCalendar.DayIndex(new DateTime(2016, month, day));

        /// <summary>A new game past the prologue whose calendar is on <paramref name="day"/> (rent paid up to it).</summary>
        static ChapterOneSim Started(int day, double money = 100000)
        {
            var sim = new ChapterOneSim();
            sim.S.prologue = 2; sim.S.appInstallState = 2;
            GameCalendar.Advance(sim.S, day);
            sim.SettleCalendar();
            sim.S.money = money;
            sim.S.bills = new HouseBills();
            return sim;
        }

        static void MoveTo(ChapterOneSim sim, int day) { GameCalendar.Advance(sim.S, day); sim.SettleCalendar(); }

        // ───────────── rent ─────────────

        /// <summary>One settlement of play (a day of the house's clock): rent follows play time, not the calendar.</summary>
        static void PlayDay(ChapterOneSim sim) => sim.Tick(sim.Config.dayLengthSeconds);
        /// <summary>Rent days in one settlement of play.</summary>
        static double DaysPerSettlement(ChapterOneSim sim) => sim.Config.dayLengthSeconds / sim.Config.rentDaySeconds;

        [Test] public void NoRentBeforeTheLabsFirstDayThenMonthlyInvoiceWithoutDebit()
        {
            var sim = new ChapterOneSim(); double start = sim.S.money;
            sim.Tick(5); Assert.IsFalse(sim.EconomyActive); Assert.That(sim.S.bills.Total, Is.Zero);
            GameCalendar.Advance(sim.S, Day(6, 1)); sim.Tick(.25);
            Assert.IsTrue(sim.EconomyActive); Assert.AreEqual(100, sim.S.bills.broadband);
            MoveTo(sim, Day(6, 20)); Assert.That(sim.S.bills.rent, Is.Zero);
            PlayDay(sim);
            Assert.AreEqual(2100, sim.Debt, "one calendar-month invoice, not an automatic debit");
            Assert.That(sim.S.bills.rent, Is.Zero); Assert.That(sim.S.money, Is.EqualTo(start - sim.S.bills.Total).Within(1e-6));
            Assert.IsTrue(sim.TransferRent(100, out _)); Assert.AreEqual(2000, sim.Debt);
            Assert.AreEqual(100, sim.S.bills.rent); Assert.That(sim.S.money, Is.EqualTo(start - sim.S.bills.Total).Within(1e-6));
            Assert.That(start, Is.GreaterThanOrEqualTo(300 * DaysPerSettlement(sim) + 100));
        }

        [Test] public void FromOctoberTheRentIsThreeHundredAndFiftyAndTheLandlordSaysSo()
        {
            var sim = Started(Day(9, 29)); string raised = null;
            Assert.IsTrue(sim.TransferRent(sim.Debt, out _)); sim.S.bills = new HouseBills();
            sim.Signal += (name, arg) => { if (name == "rent.raised") raised = name; };
            MoveTo(sim, Day(10, 2)); Assert.AreEqual("rent.raised", raised);
            PlayDay(sim); Assert.AreEqual(2450, sim.Debt); Assert.That(sim.S.bills.rent, Is.Zero);
            Assert.IsTrue(sim.TransferRent(2450, out _)); Assert.AreEqual(2450, sim.S.bills.rent);
            Assert.AreEqual(350, sim.RentFor(new DateTime(2016, 12, 31)));
        }

        [Test] public void SummerAirConditionerInJulyAndAugustAndBroadbandOnTheFirst()
        {
            var sim = Started(Day(6, 30));
            MoveTo(sim, Day(7, 3));
            Assert.That(sim.S.bills.broadband, Is.EqualTo(100), "July's broadband when the month begins");
            Assert.That(sim.S.bills.aircon, Is.Zero, "the air conditioner runs with play time");
            PlayDay(sim);
            Assert.That(sim.S.bills.aircon, Is.EqualTo(10 * DaysPerSettlement(sim)).Within(1e-6));
            MoveTo(sim, Day(9, 2));
            Assert.That(sim.S.bills.broadband, Is.EqualTo(300), "July, August and September, once each");
            double summer = sim.S.bills.aircon;
            PlayDay(sim);
            Assert.That(sim.S.bills.aircon, Is.EqualTo(summer), "not in September");
        }

        [Test] public void AnOlderSaveStartsPayingFromTheDayItIsLoadedOn()
        {
            var old = new ChapterOneSim().S; old.prologue = 2; old.appInstallState = 2;
            GameCalendar.Advance(old, Day(9, 10)); old.economyVersion = 0; old.rentDay = 0; old.money = 500;
            var sim = new ChapterOneSim(old); sim.SettleCalendar();
            Assert.AreEqual(500, sim.S.money, "no historical debit on load");
            PlayDay(sim); Assert.That(sim.S.bills.rent, Is.Zero); Assert.AreEqual(2100, sim.Debt);
            Assert.That(sim.S.money, Is.EqualTo(500 - sim.S.bills.Total).Within(1e-6));
            Assert.IsTrue(sim.TransferRent(400, out _));
            Assert.AreEqual(1700, sim.Debt); Assert.AreEqual(400, sim.S.bills.rent);
        }

        [Test] public void TheRentHolidayChargesNothingAndRentStartsFreshAfterIt()
        {
            bool waived = true;
            var sim = new ChapterOneSim { RentWaived = () => waived };
            sim.S.prologue = 2; sim.S.appInstallState = 2;
            MoveTo(sim, Day(6, 1));
            sim.S.money = -50;
            for (int i = 0; i < 4; i++) PlayDay(sim);
            MoveTo(sim, Day(6, 28));
            Assert.That(sim.EconomyActive, Is.False);
            Assert.That(sim.S.bills.Total, Is.Zero, "no rent, broadband, air conditioning or electricity");
            Assert.That(sim.S.debtDays, Is.Zero, "the landlord counts nothing");
            Assert.That(sim.S.bankrupt, Is.False);
            sim.S.money = 1000;
            waived = false;
            MoveTo(sim, Day(7, 30));
            Assert.That(sim.EconomyActive, Is.True);
            Assert.That(sim.S.bills.rent, Is.Zero, "no back rent for the holiday or the calendar jump");
            PlayDay(sim);
            Assert.That(sim.S.bills.rent, Is.Zero, "rent awaits a transfer");
            Assert.AreEqual(2100, sim.Debt);
            Assert.IsTrue(sim.TransferRent(100, out _));
            Assert.AreEqual(100, sim.S.bills.rent);
        }

        [Test] public void MonthlyInvoicesSurviveReloadAndNeverRepeatWithinTheSameMonth()
        {
            var sim = Started(Day(6, 1));
            Assert.AreEqual(2100, sim.Debt);
            Assert.IsTrue(sim.TransferRent(100, out _));
            MoveTo(sim, Day(6, 30));
            for (int i = 0; i < 12; i++) sim.Tick(sim.Config.rentDaySeconds);
            Assert.AreEqual(2000, sim.Debt, "waiting never issues another bill in June");
            sim = new ChapterOneSim(UnityEngine.JsonUtility.FromJson<GameState>(UnityEngine.JsonUtility.ToJson(sim.S)));
            sim.SettleCalendar(); Assert.AreEqual(2000, sim.Debt);
            int invoices = 0; sim.Signal += (name, _) => { if (name == "rent.due") invoices++; };
            MoveTo(sim, Day(7, 1)); Assert.AreEqual(4100, sim.Debt); Assert.AreEqual(1, invoices);
            sim.SettleCalendar(); Assert.AreEqual(1, invoices);
            Assert.AreEqual(201607, sim.S.rentBilledMonth);
            Assert.AreEqual(new DateTime(2016, 8, 1), sim.NextRentDueDate);
        }

        [Test] public void CalendarJumpBillsEachMonthAtItsOwnPriceWithoutInstantForeclosure()
        {
            var sim = Started(Day(9, 30)); Assert.IsTrue(sim.TransferRent(sim.Debt, out _));
            var months = new System.Collections.Generic.List<int>();
            sim.Signal += (name, _) => { if (name == "rent.due") months.Add(sim.S.rentBilledMonth); };
            double money = sim.S.money;
            MoveTo(sim, Day(12, 1));
            CollectionAssert.AreEqual(new[] { 201610, 201611, 201612 }, months);
            Assert.AreEqual(7350, sim.Debt); Assert.AreEqual(0, sim.S.debtDays);
            Assert.IsFalse(sim.S.landlordCut); Assert.AreEqual(money - 300, sim.S.money, "only broadband debits automatically");
            MoveTo(sim, Day(12, 30)); Assert.AreEqual(3, months.Count);
        }

        [Test] public void PreviousWeeklyScheduleKeepsDebtAndPaidMonthsWithoutDuplicateInvoiceOnLoad()
        {
            foreach (double outstanding in new[] { 0.0, 1234.5 })
            {
                var state = Started(Day(8, 12)).S;
                state.rentTransferVersion = 1; state.rentBilledMonth = 0;
                state.rentOutstanding = outstanding; state.rentPeriodsOpen = outstanding > 0 ? 1 : 0;
                double wallet = state.money;
                var sim = new ChapterOneSim(UnityEngine.JsonUtility.FromJson<GameState>(UnityEngine.JsonUtility.ToJson(state)));
                sim.SettleCalendar();
                Assert.AreEqual(wallet, sim.S.money); Assert.AreEqual(outstanding, sim.Debt);
                Assert.AreEqual(201608, sim.S.rentBilledMonth);
                MoveTo(sim, Day(9, 1)); Assert.AreEqual(outstanding + 2100, sim.Debt);
            }
        }

        // ───────────── electricity ─────────────

        [Test] public void EveryTrainingRoundBillsItsElectricity()
        {
            var sim = Started(Day(6, 5));
            double before = sim.S.money;
            double kwh = sim.TrainingKwh(.6);
            Assert.That(kwh, Is.EqualTo(250 * .5 / 1000 * .6 * 40).Within(1e-9), "a GTX 980 Ti at half load for a day of GPU time");
            double cost = sim.BillTraining(.6);
            Assert.That(cost, Is.EqualTo(kwh * .52).Within(1e-9));
            Assert.That(cost, Is.InRange(1, 20), "a round is a felt cost");
            Assert.That(sim.S.money, Is.EqualTo(before - cost).Within(1e-9));
            Assert.That(sim.S.bills.power, Is.EqualTo(cost).Within(1e-9));
            sim.S.gpuModels.Add(HardwareCatalog.Gtx1080); sim.S.gpuCount++; sim.S.caseCount = 2;
            Assert.That(sim.TrainingRoundPrice(.6), Is.GreaterThan(cost), "more cards, dearer rounds");
        }

        [Test] public void OffPeakPowerHalvesThePriceAndCuDnnTheEnergy()
        {
            var sim = Started(Day(6, 5));
            double kwh = sim.TrainingKwh(.6), plain = sim.TrainingRoundPrice(.6);
            Assert.That(sim.TrainingRoundPrice(.6, .5, 1), Is.EqualTo(plain * .5).Within(1e-9), "the button shows the off-peak price");
            Assert.That(sim.TrainingRoundPrice(.6, 1, .7), Is.EqualTo(plain * .7).Within(1e-9));
            double before = sim.S.money, month = sim.S.monthKwh;
            double paid = sim.BillTraining(.6, .5, .7, 1);
            Assert.That(paid, Is.EqualTo(kwh * .7 * .52 * .5).Within(1e-9));
            Assert.That(sim.S.money, Is.EqualTo(before - paid).Within(1e-9));
            Assert.That(sim.S.monthKwh - month, Is.EqualTo(kwh * .7).Within(1e-9), "cuDNN fills the month's tier more slowly; off-peak does not");
            Assert.That(sim.S.bills.power, Is.EqualTo(paid).Within(1e-9));
            Assert.That(sim.BillTraining(.6), Is.EqualTo(plain).Within(1e-9), "the plain overload is unchanged");
        }

        [Test] public void WearFactorScalesTheBurnOutChanceAndZeroNeverBurns()
        {
            int Burns(double factor)
            {
                int burnt = 0;
                for (int trial = 0; trial < 200; trial++)
                {
                    var sim = Started(Day(6, 5));
                    sim.S.caseCount = 2; sim.S.gpuModels.Add(HardwareCatalog.Gtx1080); sim.S.gpuCount = 2;
                    sim.S.rngState = 1000 + trial;
                    for (int i = 0; i < 200 && sim.S.gpuCount == 2; i++) { sim.BillTraining(.6, 1, 1, factor); sim.S.gameSeconds += .6; }
                    if (sim.S.gpuCount < 2) burnt++;
                }
                return burnt;
            }
            int normal = Burns(1), cooler = Burns(.5), paste = Burns(0);
            Assert.That(paste, Is.Zero, "fresh paste: no card burns out");
            Assert.That(normal, Is.GreaterThan(0));
            Assert.That(cooler, Is.LessThan(normal), "a cooler burns fewer cards than none");
        }

        [Test] public void WearRiskIsLiveOnlyWithTwoCardsAfterALongRunAndCoolingResetsIt()
        {
            var sim = Started(Day(6, 5));
            Assert.That(sim.WearRiskLive, Is.False);
            sim.S.caseCount = 2; sim.S.gpuModels.Add(HardwareCatalog.Gtx1080); sim.S.gpuCount = 2;
            for (int i = 0; i < 80; i++) { sim.BillTraining(.6, 1, 1, 0); sim.S.gameSeconds += .6; }
            Assert.That(sim.WearRiskLive, Is.True, "48 seconds of non-stop training with two cards");
            sim.CoolCards();
            Assert.That(sim.WearRiskLive, Is.False);
            sim.S.gameSeconds += 30;
            Assert.That(sim.WearRiskLive, Is.False, "and it ends once training pauses");
        }

        [Test] public void ElectricityIsBilledFromTheFirstRoundEvenBeforeTheFirstRent()
        {
            var sim = new ChapterOneSim();
            double before = sim.S.money;
            double cost = sim.BillTraining(.6);
            Assert.That(cost, Is.GreaterThan(0), "electricity is always paid; only the rent waits for its day");
            Assert.That(sim.S.money, Is.EqualTo(before - cost).Within(1e-9));
            Assert.That(sim.S.bills.power, Is.EqualTo(cost).Within(1e-9));
            Assert.That(sim.EconomyActive, Is.False, "and rent has not started");
            // A lab that has not unlocked the power system yet (PowerCostsEnabled off) pays nothing.
            var free = new ChapterOneSim { PowerCostsEnabled = false };
            double wallet = free.S.money;
            Assert.That(free.BillTraining(.6), Is.Zero);
            Assert.That(free.S.money, Is.EqualTo(wallet));
        }

        [Test] public void TieredPricingAboveTwoHundredFortyAndFourHundredKwh()
        {
            var c = new GameConfig();
            Assert.That(ChapterOneSim.TieredCost(0, 100, c), Is.EqualTo(100 * .52).Within(1e-9));
            Assert.That(ChapterOneSim.TieredCost(200, 100, c), Is.EqualTo(40 * .52 + 60 * .52 * 1.1).Within(1e-9));
            Assert.That(ChapterOneSim.TieredCost(350, 100, c), Is.EqualTo(50 * .52 * 1.1 + 50 * .52 * 1.5).Within(1e-9));
            Assert.That(ChapterOneSim.TieredCost(500, 10, c), Is.EqualTo(10 * .52 * 1.5).Within(1e-9));

            var sim = Started(Day(6, 5));
            sim.S.kwhMonth = 201606; sim.S.monthKwh = 500;
            Assert.That(sim.PowerTier, Is.EqualTo(3));
            double cost = sim.BillTraining(.6);
            Assert.That(cost, Is.EqualTo(sim.TrainingKwh(.6) * .52 * 1.5).Within(1e-9), "third tier");
            MoveTo(sim, Day(7, 1));
            Assert.That(sim.PowerTier, Is.EqualTo(1), "a new month starts at the first tier");
            Assert.That(sim.BillTraining(.6), Is.EqualTo(sim.TrainingKwh(.6) * .52).Within(1e-9));
        }

        // ───────────── wear, the breaker, the weekend ─────────────

        [Test] public void HeavyTrainingBurnsACardThatComesBackRepaired()
        {
            var sim = Started(Day(6, 5));
            sim.S.caseCount = 2; sim.S.gpuModels.Add(HardwareCatalog.Gtx1080); sim.S.gpuCount = 2;
            var config = sim.Config;
            Assert.That(config.wearChancePerSecond, Is.GreaterThan(0));
            // Short of the streak, nothing ever breaks.
            for (int i = 0; i < 40; i++) { sim.BillTraining(.6); sim.S.gameSeconds += .6; }
            Assert.That(sim.S.gpuCount, Is.EqualTo(2), "24 seconds of training is not a heavy streak");
            string broken = null;
            sim.Signal += (name, arg) => { if (name == "gpu.broken") broken = arg; };
            for (int i = 0; i < 100000 && broken == null; i++) { sim.BillTraining(.6); sim.S.gameSeconds += .6; }
            Assert.That(broken, Is.EqualTo(HardwareCatalog.Gtx980Ti), "the hardest-working card (most watts) fails");
            Assert.That(sim.S.gpuCount, Is.EqualTo(1));
            Assert.That(sim.S.repairs.Count, Is.EqualTo(1));
            Assert.That(sim.S.bills.repair, Is.EqualTo(sim.RepairFee(HardwareCatalog.Gpu(HardwareCatalog.Gtx980Ti))));
            Assert.That(sim.S.bills.repair, Is.EqualTo(Math.Round(.2 * Math.Max(HardwareCatalog.Gpu(HardwareCatalog.Gtx980Ti).price, HardwareCatalog.Gpu(HardwareCatalog.Gtx980Ti).usedPrice))));
            Assert.That(sim.BreakCard(), Is.False, "the last working card never fails");
            sim.Tick(config.repairSeconds + 1);
            Assert.That(sim.S.gpuCount, Is.EqualTo(2), "back from the repair shop");
            Assert.That(sim.S.repairs, Is.Empty);
        }

        [Test] public void EachBreakerTripCostsAnElectrician()
        {
            var sim = Started(Day(6, 5));
            sim.TripBreakerNow();
            Assert.That(sim.S.bills.breaker, Is.EqualTo(50));
            sim.TripBreakerNow();
            Assert.That(sim.S.bills.breaker, Is.EqualTo(50), "one fee per trip");
            Assert.That(sim.ResetBreaker(), Is.True);
            sim.TripBreakerNow();
            Assert.That(sim.S.bills.breaker, Is.EqualTo(100));
            var before = new ChapterOneSim();
            before.TripBreakerNow();
            Assert.That(before.S.bills.breaker, Is.Zero, "nothing before the first rent");
        }

        [Test] public void PromisingTheWeekendPaysATrainTicketOnSaturday()
        {
            var sim = Started(Day(10, 12)); // a Wednesday
            Assert.That(GameCalendar.DateOf(Day(10, 12)).DayOfWeek, Is.EqualTo(DayOfWeek.Wednesday));
            Assert.That(sim.BookWeekendTrip(), Is.True);
            Assert.That(sim.BookWeekendTrip(), Is.False, "one trip at a time");
            MoveTo(sim, Day(10, 14));
            Assert.That(sim.S.bills.ticket, Is.Zero, "not before Saturday");
            MoveTo(sim, Day(10, 15));
            Assert.That(sim.S.bills.ticket, Is.EqualTo(120));
            Assert.That(sim.S.ticketDay, Is.Zero);
        }

        // ───────────── bankruptcy ─────────────

        [Test] public void ThreeDaysBelowZeroTextThenCutThenAskThenSell()
        {
            var sim = new ChapterOneSim(); string signals = "";
            sim.S.prologue = 2; sim.S.appInstallState = 2;
            sim.Signal += (name, arg) => { if (name == "rent.due" || name == "rent.reminder" || name == "rent.warning" || name == "power.cut" || name == "rent.decision" || name == "bankrupt") signals += name + ";"; };
            double day = sim.Config.rentDaySeconds; Assert.That(sim.HeartbeatsPerSecond, Is.GreaterThan(0));
            MoveTo(sim, Day(6, 5)); sim.S.money = -500;
            Assert.AreEqual(0, sim.S.debtDays); Assert.AreEqual("rent.due;", signals);
            Assert.IsFalse(sim.ConfirmComputerSale(), "invoice day is not foreclosure");
            sim.Tick(day); Assert.AreEqual(1, sim.S.debtDays); Assert.IsFalse(sim.S.landlordCut);
            Assert.AreEqual("rent.due;rent.reminder;", signals);
            sim.Tick(day); Assert.AreEqual(2, sim.S.debtDays); Assert.IsFalse(sim.S.landlordCut);
            Assert.AreEqual("rent.due;rent.reminder;rent.warning;", signals);
            sim.Tick(day); Assert.AreEqual(3, sim.S.debtDays); Assert.IsTrue(sim.S.landlordCut);
            Assert.That(sim.HeartbeatsPerSecond, Is.Zero); Assert.IsFalse(sim.S.bankrupt);
            Assert.AreEqual("rent.due;rent.reminder;rent.warning;power.cut;", signals);
            sim.Tick(day); Assert.AreEqual(4, sim.S.debtDays); Assert.IsFalse(sim.S.bankrupt);
            Assert.AreEqual("rent.due;rent.reminder;rent.warning;power.cut;rent.decision;", signals);
            Assert.IsTrue(sim.ConfirmComputerSale()); Assert.IsTrue(sim.S.bankrupt);
            Assert.AreEqual("rent.due;rent.reminder;rent.warning;power.cut;rent.decision;bankrupt;", signals);
            Assert.IsFalse(sim.ConfirmComputerSale()); Assert.That(sim.S.soldFor, Is.GreaterThan(0));
            double money = sim.S.money; sim.Tick(day * 3); Assert.AreEqual(money, sim.S.money);
        }

        [Test] public void PayingTheDebtBackStopsTheCountdownAndBringsThePowerBack()
        {
            var sim = Started(Day(6, 5), -500); double day = sim.Config.rentDaySeconds;
            for (int i = 0; i < 4; i++) sim.Tick(day);
            Assert.IsTrue(sim.S.landlordCut); Assert.AreEqual(2100, sim.Debt);
            sim.S.money = 3000; sim.Tick(.25);
            Assert.IsTrue(sim.S.landlordCut, "earning money cannot silently pay the landlord");
            Assert.IsTrue(sim.TransferRent(100, out _)); Assert.AreEqual(2000, sim.Debt); Assert.IsTrue(sim.S.landlordCut);
            Assert.IsTrue(sim.TransferRent(2000, out _)); Assert.IsFalse(sim.S.landlordCut);
            Assert.AreEqual(0, sim.S.debtDays); Assert.AreEqual(2100, sim.S.bills.rent);
            Assert.That(sim.HeartbeatsPerSecond, Is.GreaterThan(0));
            double paid = sim.S.money; Assert.IsFalse(sim.TransferRent(1, out _)); Assert.AreEqual(paid, sim.S.money);
            sim.Tick(day * 4); Assert.AreEqual(0, sim.Debt, "play time alone cannot create another month's rent");
            MoveTo(sim, Day(7, 1)); Assert.AreEqual(2100, sim.Debt, "next month's invoice is separate"); Assert.AreEqual(2100, sim.S.bills.rent);
            sim.Tick(day); Assert.AreEqual(1, sim.S.debtDays, "new invoice restarts escalation");
        }

        [Test] public void NoBankruptcyBeforeTheFirstRent()
        {
            var sim = new ChapterOneSim();
            sim.S.money = 0;
            for (int i = 0; i < 5; i++) sim.Tick(sim.Config.dayLengthSeconds);
            Assert.That(sim.S.debtDays, Is.Zero);
            Assert.That(sim.S.bankrupt, Is.False);
        }

        [Test] public void ADebtorsSaveLoadsWithTheFundsLedgerThatFollowedItBelowZero()
        {
            var sim = Started(Day(6, 5), -500);
            sim.RecordFunds("其他收支");
            PlayDay(sim); PlayDay(sim); // rent keeps coming: the ledger's balance follows the wallet down
            Assert.That(sim.S.money, Is.LessThan(0));
            Assert.That(sim.S.fundsBalance, Is.LessThan(0));
            var loaded = new ChapterOneSim(UnityEngine.JsonUtility.FromJson<GameState>(UnityEngine.JsonUtility.ToJson(sim.S)));
            Assert.That(loaded.S.fundsBalance, Is.EqualTo(sim.S.fundsBalance).Within(1e-9), "JSON double round-trip stays far below one cent");
            Assert.That(loaded.S.debtDays, Is.EqualTo(sim.S.debtDays));
        }

        [Test] public void ADebtorsSaveLoadsWithItsNegativeWallet()
        {
            var sim = Started(Day(6, 5), -1234.5);
            var json = UnityEngine.JsonUtility.ToJson(sim.S);
            var loaded = new ChapterOneSim(UnityEngine.JsonUtility.FromJson<GameState>(json));
            Assert.That(loaded.S.money, Is.EqualTo(-1234.5));
            Assert.That(loaded.Debt, Is.EqualTo(sim.Debt), "a negative wallet cannot add to the monthly rent debt on reload");
        }
        [Test] public void LegacyNegativeRentDebtMigratesWithoutChangingNetWealth()
        {
            var old = Started(Day(6, 5), -1234.5).S;
            old.rentTransferVersion = 0; old.debtDays = 2;
            var loaded = new ChapterOneSim(old);
            Assert.AreEqual(0, loaded.S.money);
            Assert.AreEqual(1234.5, loaded.Debt);
            Assert.AreEqual(-1234.5, loaded.S.money - loaded.Debt);
            var again = new ChapterOneSim(loaded.S);
            Assert.AreEqual(1234.5, again.Debt, "migration is idempotent");
        }

        [Test] public void InvalidRentTransfersNeverMutateMoneyOrDebt()
        {
            var sim = Started(Day(6, 5), 100);
            sim.Tick(sim.Config.rentDaySeconds);
            foreach (double amount in new[] { 0, -1, double.NaN, double.PositiveInfinity, 0.001, 101, 2101 })
            {
                double balance = sim.S.money, debt = sim.Debt, paid = sim.S.bills.rent;
                Assert.IsFalse(sim.TransferRent(amount, out _));
                Assert.AreEqual(balance, sim.S.money); Assert.AreEqual(debt, sim.Debt);
                Assert.AreEqual(paid, sim.S.bills.rent);
            }
        }
    }
}
