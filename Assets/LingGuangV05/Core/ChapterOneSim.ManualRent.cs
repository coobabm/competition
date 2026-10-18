using System;

namespace LingGuangV05.Core
{
    public sealed partial class ChapterOneSim
    {
        public const int RentPeriodDays = 7;
        public double RentDaysUntilDue => Math.Max(0, S.rentNextDueDays - S.rentClockDays);

        void MigrateManualRent()
        {
            if (S.rentTransferVersion >= 1) return;
            S.rentTransferVersion = 1;
            S.rentNextDueDays = 1;
            // Earlier builds charged rent into a negative wallet. Preserve net wealth and the debt,
            // but make it explicitly payable instead of silently repaying it from future income.
            if (S.money < 0 && S.debtDays > 0)
            {
                S.rentOutstanding = -S.money;
                S.money = 0;
                S.rentClockDays = Math.Max(S.rentClockDays, Math.Min(3, S.debtDays));
                S.rentOldestDueDays = S.rentClockDays - Math.Min(3, S.debtDays);
                S.rentNextDueDays = S.rentClockDays + 1;
                S.rentPeriodsOpen = 1;
            }
        }

        void StartManualRent()
        {
            if (S.rentSystemStarted || !EconomyActive) return;
            S.rentSystemStarted = true;
            LastMessage = T("明天开始收房租了哈，每七天一期，一期 " + Yuan((RentFor(GameCalendar.Now(S)) * RentPeriodDays + Math.Max(0, S.rentExtraPerPeriod))) + "。到时候在 YY 里转给我。",
                "Rent starts tomorrow: one payment every seven days, " + Yuan((RentFor(GameCalendar.Now(S)) * RentPeriodDays + Math.Max(0, S.rentExtraPerPeriod))) + " per period. Transfer it to me in YY.");
            Raise("rent.started");
        }

        void TickManualRent(double dt)
        {
            if (!EconomyActive || !S.rentSystemStarted || S.bankrupt) return;
            S.rentClockDays += Math.Max(0, dt) / Math.Max(1, Config.rentDaySeconds);
            while (S.rentClockDays + Epsilon >= S.rentNextDueDays)
            {
                if (Debt <= Epsilon) S.rentOldestDueDays = S.rentNextDueDays;
                S.rentOutstanding += (RentFor(GameCalendar.Now(S)) * RentPeriodDays + Math.Max(0, S.rentExtraPerPeriod));
                S.rentPeriodsOpen++;
                S.rentNextDueDays += RentPeriodDays;
                LastMessage = T("小伙子，这期房租该交了，目前应付 " + Yuan(Debt) + "，今天转一下哈。", "The rent is due. Please transfer " + Yuan(Debt) + " today.");
                Raise("rent.due", Yuan(Debt));
            }
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
