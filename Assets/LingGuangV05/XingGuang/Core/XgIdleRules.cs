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
        public static double IncomeRate(XgSim sim, IXgHost host)
        {
            if (!Unlocked(sim)) return 0;
            double total = 0;
            foreach (var desk in XgCatalog.Desks) total += sim.CollaborationIncome(desk.id, host);
            return total;
        }
    }
}
