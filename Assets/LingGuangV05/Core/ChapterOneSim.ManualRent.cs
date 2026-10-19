using System;

namespace LingGuangV05.Core
{
    public sealed partial class ChapterOneSim
    {
        // Preserve the established ¥2100 / ¥2450 invoice prices. The legacy config stores price units,
        // not the new billing interval; rent is now invoiced once per story-calendar month.
        public double MonthlyRentFor(DateTime date) => RentFor(date) * 7 + Math.Max(0, S.rentExtraPerPeriod);
        public DateTime NextRentDueDate => S.rentBilledMonth > 0
            ? new DateTime(S.rentBilledMonth / 100, S.rentBilledMonth % 100, 1).AddMonths(1)
            : new DateTime(GameCalendar.Now(S).Year, GameCalendar.Now(S).Month, 1);
        public double RentDaysUntilDue => Math.Max(0, (NextRentDueDate - GameCalendar.Now(S).Date).TotalDays);

        void MigrateManualRent()
        {
            if (S.rentInvoiceOutbox == null) S.rentInvoiceOutbox = new System.Collections.Generic.List<RentInvoice>();
            if (S.rentTransferVersion >= 2) return;
            // Earlier builds charged rent into a negative wallet. Preserve net wealth and the debt,
            // but make it explicitly payable instead of silently repaying it from future income.
            if (S.rentTransferVersion < 1 && S.money < 0 && S.debtDays > 0)
            {
                S.rentOutstanding = -S.money;
                S.money = 0;
                S.rentClockDays = Math.Max(S.rentClockDays, Math.Min(3, S.debtDays));
                S.rentOldestDueDays = S.rentClockDays - Math.Min(3, S.debtDays);
                S.rentNextDueDays = S.rentClockDays + 1;
                S.rentPeriodsOpen = 1;
            }
            // Current-version saves with the previous weekly schedule keep all outstanding debt and
            // payments. Treat their current month as issued, so loading never adds a duplicate bill.
            if (S.rentSystemStarted) S.rentBilledMonth = MonthOf(GameCalendar.CurrentDay(S));
            S.rentTransferVersion = 2;
        }

        void StartManualRent()
        {
            if (S.rentSystemStarted || !EconomyActive) return;
            S.rentSystemStarted = true;
            LastMessage = T("房租每月 " + Yuan(MonthlyRentFor(GameCalendar.Now(S))) + "。这个月的账单我发在 YY 里，以后每月一号发，记得手动转给我。",
                "Rent is " + Yuan(MonthlyRentFor(GameCalendar.Now(S))) + " a month. I'll send this month's bill in YY, then one on the first of each month. Please transfer it manually.");
            Raise("rent.started");
        }

        void SettleMonthlyRent()
        {
            if (!EconomyActive || !S.rentSystemStarted || S.bankrupt) return;
            var now = GameCalendar.DateOf(GameCalendar.CurrentDay(S));
            var month = new DateTime(now.Year, now.Month, 1);
            var due = S.rentBilledMonth > 0 ? NextRentDueDate : month;
            // A progress jump may cross November inside stage 5. Each elapsed month gets its own
            // invoice, using that month's price, rather than silently skipping it or charging daily.
            while (due <= month)
            {
                S.rentBilledMonth = due.Year * 100 + due.Month; // Advance before event callbacks / saves.
                double amount = MonthlyRentFor(due);
                if (amount > Epsilon)
                {
                    if (Debt <= Epsilon) S.rentOldestDueDays = S.rentClockDays;
                    S.rentOutstanding += amount;
                    S.rentPeriodsOpen++;
                    var invoice = new RentInvoice { month = S.rentBilledMonth, amount = amount, total = Debt, gameSeconds = S.gameSeconds };
                    S.rentInvoiceOutbox.Add(invoice); // YY may not exist yet on the first loaded frame.
                    LastMessage = RentInvoiceText(invoice);
                    Raise("rent.due", Yuan(Debt));
                }
                due = due.AddMonths(1);
            }
        }

        public static string RentInvoiceText(RentInvoice invoice)
        {
            var date = new DateTime(invoice.month / 100, invoice.month % 100, 1);
            return T(date.Month + " 月房租账单：本月 " + Yuan(invoice.amount) + "，待付合计 " + Yuan(invoice.total) + "。在 YY 转给我就行，可以分次付。",
                "Rent bill for " + date.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture) + ": " + Yuan(invoice.amount) + " this month, " + Yuan(invoice.total) + " outstanding in total. Transfer it in YY; partial payments are fine.");
        }

        void TickManualRent(double dt)
        {
            if (!EconomyActive || !S.rentSystemStarted || S.bankrupt) return;
            // Keep the existing playable grace period. Calendar jumps must not instantly foreclose.
            S.rentClockDays += Math.Max(0, dt) / Math.Max(1, Config.rentDaySeconds);
            if (Debt <= Epsilon) return;
            int overdue = Math.Min(DebtSaleDay, Math.Max(0, (int)Math.Floor(S.rentClockDays - S.rentOldestDueDays + Epsilon)));
            while (S.debtDays < overdue)
            {
                S.debtDays++;
                if (S.debtDays == 1)
                {
                    S.rentEverLate = true;
                    LastMessage = T("别忘了房租哈，还差 " + Yuan(Debt) + "。", "Don't forget the rent: " + Yuan(Debt) + " is still due.");
                    Raise("rent.reminder", Yuan(Debt));
                }
                else if (S.debtDays == 2)
                {
                    LastMessage = T("还差 " + Yuan(Debt) + "，再不交我明天拉闸了。", Yuan(Debt) + " is still due. Pay today or I cut the power tomorrow.");
                    Raise("rent.warning", Yuan(Debt));
                }
                else if (S.debtDays == DebtCutDay)
                {
                    S.landlordCut = true;
                    LastMessage = T("电闸先拉了，还差 " + Yuan(Debt) + "。补齐就恢复。", "Power is off until the remaining " + Yuan(Debt) + " is paid.");
                    Raise("power.cut", Yuan(Debt));
                }
                else
                {
                    LastMessage = T("电脑先押我这儿。你可以继续手动做题还租，或确认卖电脑抵账。", "The computer is now collateral. Keep answering by hand to pay the rent, or confirm selling it to settle the debt.");
                    Raise("rent.decision", Yuan(Debt));
                }
            }
        }

        public bool TransferRent(double amount, out string problem)
        {
            problem = "";
            if (S.bankrupt || Debt <= Epsilon) { problem = T("目前没有待付房租。", "There is no unpaid rent."); return false; }
            if (!Finite(amount) || amount <= 0 || Math.Abs(amount * 100 - Math.Round(amount * 100)) > .00001)
            { problem = T("请输入大于零、最多两位小数的金额。", "Enter a positive amount with at most two decimal places."); return false; }
            if (amount > Debt + Epsilon) { problem = T("金额超过待付房租。", "That exceeds the unpaid rent."); return false; }
            if (amount > S.money + Epsilon) { problem = T("钱包余额不够，可以先转一部分。", "Not enough in your wallet. You can transfer a smaller amount."); return false; }
            bool onTime = S.debtDays == 0;
            bool wasCut = S.landlordCut;
            Bill(BillKind.Rent, amount);
            S.rentOutstanding = Math.Max(0, S.rentOutstanding - amount);
            if (Debt <= Epsilon)
            {
                if (onTime) S.rentOnTimePeriods += S.rentPeriodsOpen;
                S.rentPeriodsOpen = 0;
                ClearDebtIfPaid();
                LastMessage = T(S.rentOnTimePeriods >= 3 ? "收到，这个月还挺准时。" : "收到。", S.rentOnTimePeriods >= 3 ? "Received. On time again this month." : "Received.");
                if (wasCut) LastMessage += T("电闸给你合上了。", "The power is back on.");
                Raise("rent.settled");
            }
            else
            {
                LastMessage = T("先转这些？行，还差 " + Yuan(Debt) + "，尽快补上。", "A partial payment? All right. Please send the remaining " + Yuan(Debt) + " soon.");
                Raise("rent.partial", Yuan(Debt));
            }
            Notify();
            return true;
        }
    }
}
