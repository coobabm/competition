using System;
using System.Collections.Generic;

namespace LingGuangV05.Core
{
    [Serializable] public sealed class FundsEntry
    {
        public string reason;
        public double amount, balance, seconds;
    }
    public sealed partial class ChapterOneSim
    {
        // Reconcile the single shared wallet. Old saves begin with a truthful opening
        // balance rather than invented historical transactions.
        public void RecordFunds(string reason)
        {
            if (!Finite(S.money)) return;
            if (S.funds == null) S.funds = new List<FundsEntry>();
            if (!S.fundsStarted) {
                S.fundsStarted = true; S.fundsBalance = S.money;
                S.funds.Add(new FundsEntry { reason = "期初余额", balance = S.money, seconds = S.gameSeconds });
                return;
            }
            double amount = S.money - S.fundsBalance;
            if (Math.Abs(amount) < 1e-9) return;
            S.fundsBalance = S.money;
            if (amount > 0) S.fundsIncome += amount; else S.fundsExpense -= amount;
            reason = string.IsNullOrWhiteSpace(reason) ? "其他收支" : reason;
            if (reason.Length > 36) reason = reason.Substring(0, 36);
            var last = S.funds.Count > 0 ? S.funds[S.funds.Count - 1] : null;
            // Combine rapid slices of the same transaction, retaining exact totals.
            if (last != null && last.reason == reason && Math.Sign(last.amount) == Math.Sign(amount)
                && S.gameSeconds - last.seconds < 10) {
                last.amount += amount; last.balance = S.money; last.seconds = S.gameSeconds;
            } else S.funds.Add(new FundsEntry { reason = reason, amount = amount, balance = S.money, seconds = S.gameSeconds });
            if (S.funds.Count > 300) S.funds.RemoveRange(0, S.funds.Count - 300);
        }
        static string BillTitle(BillKind kind)
        {
            switch (kind) {
                case BillKind.Power: return "训练电费";
                case BillKind.IdlePower: return "待机电费结算";
                case BillKind.Rent: return "房租";
                case BillKind.Aircon: return "空调电费";
                case BillKind.Broadband: return "宽带费";
                case BillKind.Repair: return "显卡维修";
                case BillKind.Breaker: return "电路维修";
                default: return "车票";
            }
        }
    }
}
