using System;
namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState { public double idleAutomaticIncome; }
    public static class XgIdleRules
    {
        public const string UnlockNode = "label.auto";
        public static bool Unlocked(XgSim sim) => sim != null && sim.GlobalAutoLevel > 0;
        // Display only: never credits the wallet, and never displays money already spent.
        public static double CatchUp(double displayed, double settled)
        {
            if (double.IsNaN(settled) || double.IsInfinity(settled)) return displayed;
            if (settled <= displayed) return settled;
            return Math.Min(settled, Math.Round(displayed + Math.Max(.01, Math.Ceiling((settled - displayed) / 3 * 100) / 100), 2));
        }
        /// <summary>
        /// All lab income per second: the crowd tier and contracts (<see cref="XgSim.IncomePerSecond"/>) plus 自动答题 on
        /// every desk. A full review inbox does not stop it (only the unsure cards are skipped then).
        /// </summary>
        public static double IncomeRate(XgSim sim, IXgHost host)
        {
            if (sim == null) return 0;
            return sim.IncomePerSecond + AutoIncomeRate(sim, host);
        }
        /// <summary>自动答题's share of <see cref="IncomeRate"/> (0 before the first level).</summary>
        public static double AutoIncomeRate(XgSim sim, IXgHost host)
        {
            if (!Unlocked(sim)) return 0;
            double total = 0;
            foreach (var desk in XgCatalog.Desks) total += sim.CollaborationIncome(desk.id, host);
            return total;
        }
    }
}
