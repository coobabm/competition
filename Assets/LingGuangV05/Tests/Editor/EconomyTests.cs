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

        [Test] public void NoRentBeforeTheLabsFirstDayThenRentByPlayTime()
        {
            var sim = new ChapterOneSim();
            double start = sim.S.money;
            sim.Tick(5);
            Assert.That(sim.EconomyActive, Is.False, "the prologue day pays nothing");
            Assert.That(sim.S.bills.Total, Is.Zero);
            GameCalendar.Advance(sim.S, Day(6, 1));
            sim.Tick(.25);
            Assert.That(sim.EconomyActive, Is.True);
            Assert.That(sim.S.bills.broadband, Is.EqualTo(100), "June's broadband");
            Assert.That(sim.S.bills.rent, Is.Zero, "rent is charged as play time passes");
            MoveTo(sim, Day(6, 20));
            Assert.That(sim.S.bills.rent, Is.Zero, "a calendar jump bills no rent at once");
            PlayDay(sim);
            Assert.That(sim.S.bills.rent, Is.EqualTo(300 * DaysPerSettlement(sim)).Within(1e-6), "one settlement of play");
            Assert.That(start, Is.GreaterThanOrEqualTo(300 * DaysPerSettlement(sim) + 100), "the starting money covers the first settlement");
        }

        [Test] public void FromOctoberTheRentIsThreeHundredAndFiftyAndTheLandlordSaysSo()
        {
            var sim = Started(Day(9, 29));
            string raised = null;
            sim.Signal += (name, arg) => { if (name == "rent.raised") raised = name; };
            MoveTo(sim, Day(10, 2));
            Assert.That(raised, Is.EqualTo("rent.raised"));
            PlayDay(sim);
            Assert.That(sim.S.bills.rent, Is.EqualTo(350 * DaysPerSettlement(sim)).Within(1e-6));
            Assert.That(sim.RentFor(new DateTime(2016, 12, 31)), Is.EqualTo(350));
        }

        [Test] public void SummerAirConditionerInJulyAndAugustAndBroadbandOnTheFirst()
        {
            var sim = Started(Day(6, 30));
            MoveTo(sim, Day(7, 3));
            Assert.That(sim.S.bills.broadband, Is.EqualTo(100), "July's broadband when the month begins");
            Assert.That(sim.S.bills.aircon, Is.Zero, "the air conditioner runs with play time");
            PlayDay(sim);
            Assert.That(sim.S.bills.aircon, Is.EqualTo(40 * DaysPerSettlement(sim)).Within(1e-6));
            MoveTo(sim, Day(9, 2));
            Assert.That(sim.S.bills.broadband, Is.EqualTo(300), "July, August and September, once each");
            double summer = sim.S.bills.aircon;
            PlayDay(sim);
            Assert.That(sim.S.bills.aircon, Is.EqualTo(summer), "not in September");
        }

        [Test] public void AnOlderSaveStartsPayingFromTheDayItIsLoadedOn()
        {
            var old = new ChapterOneSim().S;
            old.prologue = 2; old.appInstallState = 2;
            GameCalendar.Advance(old, Day(9, 10));
            old.economyVersion = 0; old.rentDay = 0;
            old.money = 500;
            var sim = new ChapterOneSim(old);
            sim.SettleCalendar();
            Assert.That(sim.S.money, Is.EqualTo(500), "no back rent for June to September");
            PlayDay(sim);
            Assert.That(sim.S.bills.rent, Is.EqualTo(300 * DaysPerSettlement(sim)).Within(1e-6));
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
            Assert.That(sim.S.bills.rent, Is.EqualTo(300 * DaysPerSettlement(sim)).Within(1e-6));
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

        [Test] public void BeforeTheFirstRentTrainingStaysOnTheOldDailyBill()
        {
            var sim = new ChapterOneSim();
            double before = sim.S.money;
            Assert.That(sim.BillTraining(.6), Is.Zero);
            Assert.That(sim.S.money, Is.EqualTo(before));
            Assert.That(sim.S.energyKwh, Is.GreaterThan(0));
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

        [Test] public void ThreeDaysBelowZeroTextThenCutThenSell()
        {
            var sim = Started(Day(6, 5), -500);
            string signals = "";
            sim.Signal += (name, arg) => { if (name == "rent.due" || name == "power.cut" || name == "bankrupt") signals += name + ";"; };
            double day = sim.Config.dayLengthSeconds;
            Assert.That(sim.HeartbeatsPerSecond, Is.GreaterThan(0));
            sim.Tick(day);
            Assert.That(sim.S.debtDays, Is.EqualTo(1));
            Assert.That(signals, Is.EqualTo("rent.due;"), "day 1: the landlord texts");
            Assert.That(sim.S.landlordCut, Is.False);
            sim.Tick(day);
            Assert.That(signals, Is.EqualTo("rent.due;power.cut;"));
            Assert.That(sim.S.landlordCut, Is.True, "day 2: the power is cut");
            Assert.That(sim.HeartbeatsPerSecond, Is.Zero, "training is blocked");
            Assert.That(sim.S.bankrupt, Is.False);
            sim.Tick(day);
            Assert.That(sim.S.bankrupt, Is.True, "day 3: the computer is sold");
            Assert.That(signals, Is.EqualTo("rent.due;power.cut;bankrupt;"));
            Assert.That(sim.S.soldFor, Is.GreaterThan(0));
            double money = sim.S.money;
            sim.Tick(day * 3);
            Assert.That(sim.S.money, Is.EqualTo(money), "nothing runs after the sale");
        }

        [Test] public void PayingTheDebtBackStopsTheCountdownAndBringsThePowerBack()
        {
            var sim = Started(Day(6, 5), -500);
            double day = sim.Config.dayLengthSeconds;
            sim.Tick(day); sim.Tick(day);
            Assert.That(sim.S.landlordCut, Is.True);
            sim.S.money = 50;
            sim.Tick(.25);
            Assert.That(sim.S.landlordCut, Is.False);
            Assert.That(sim.S.debtDays, Is.Zero);
            Assert.That(sim.HeartbeatsPerSecond, Is.GreaterThan(0));
            sim.S.money = -10;
            sim.Tick(day);
            Assert.That(sim.S.debtDays, Is.EqualTo(1), "the count starts over");
        }

        [Test] public void NoBankruptcyBeforeTheFirstRent()
        {
            var sim = new ChapterOneSim();
            sim.S.money = 0;
            for (int i = 0; i < 5; i++) sim.Tick(sim.Config.dayLengthSeconds);
            Assert.That(sim.S.debtDays, Is.Zero);
            Assert.That(sim.S.bankrupt, Is.False);
        }

        [Test] public void ADebtorsSaveLoadsWithItsNegativeWallet()
        {
            var sim = Started(Day(6, 5), -1234.5);
            var json = UnityEngine.JsonUtility.ToJson(sim.S);
            var loaded = new ChapterOneSim(UnityEngine.JsonUtility.FromJson<GameState>(json));
            Assert.That(loaded.S.money, Is.EqualTo(-1234.5));
            Assert.That(loaded.Debt, Is.EqualTo(1234.5));
        }
    }
}
