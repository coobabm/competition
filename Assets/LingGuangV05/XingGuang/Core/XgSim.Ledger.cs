using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>
        /// Closing balances of the last days (oldest first, at most <see cref="XgSim.BalanceDaysKept"/>), for the 结算 page's
        /// sparkline. Filled by the desktop when the in-game day changes (the household wallet lives outside the lab).
        /// Additive: an older save simply starts with an empty list.
        /// </summary>
        public List<double> balanceDays = new List<double>();
        /// <summary>The calendar day (GameCalendar day index) of the last closing balance, so one day is recorded once.</summary>
        public int balanceLastDay = -1;
    }

    /// <summary>The daily balance history behind 结算's sparkline. Plain data: no clock, no UI.</summary>
    public sealed partial class XgSim
    {
        public const int BalanceDaysKept = 10;

        /// <summary>
        /// Records the closing balance of the day that just ended. <paramref name="endedDay"/> is the calendar day index
        /// that ended; the same day is never recorded twice (a reload or a repeated call changes nothing).
        /// Returns true when a point was added.
        /// </summary>
        public bool RecordDayBalance(int endedDay, double balance)
        {
            if (S.balanceDays == null) S.balanceDays = new List<double>();
            if (double.IsNaN(balance) || double.IsInfinity(balance)) return false;
            if (endedDay <= S.balanceLastDay) return false;
            S.balanceLastDay = endedDay;
            S.balanceDays.Add(balance);
            while (S.balanceDays.Count > BalanceDaysKept) S.balanceDays.RemoveAt(0);
            return true;
        }

        /// <summary>The recorded closing balances, oldest first (finite values only; never null).</summary>
        public List<double> BalanceHistory()
        {
            var list = new List<double>();
            if (S.balanceDays == null) return list;
            foreach (var v in S.balanceDays) if (!double.IsNaN(v) && !double.IsInfinity(v)) list.Add(v);
            while (list.Count > BalanceDaysKept) list.RemoveAt(0);
            return list;
        }
    }
}
