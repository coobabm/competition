using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Session-only focus for a newly completed setup; never a navigation or simulation lock.</summary>
    public sealed class XgFirstLessonFocus
    {
        public const double Duration = 60;
        XgSim owner;
        double presentedAt = double.NaN;

        public void Arm(XgSim sim)
        {
            if (ReferenceEquals(owner, sim)) return;
            owner = sim;
            presentedAt = double.NaN;
        }

        public void Presented(XgSim sim, double now)
        {
            if (sim == null || !ReferenceEquals(owner, sim) || !double.IsNaN(presentedAt)
                || double.IsNaN(now) || double.IsInfinity(now)) return;
            presentedAt = now;
        }

        public bool ActiveFor(XgSim sim, double now)
        {
            if (!ReferenceEquals(owner, sim))
            {
                owner = null;
                presentedAt = double.NaN;
                return false;
            }
            return owner != null && (double.IsNaN(presentedAt) || now - presentedAt < Duration);
        }
    }

    public static class XgFirstLessonWindow
    {
        static readonly XgFirstLessonFocus focus = new XgFirstLessonFocus();
        public static void Arm(XgSim sim) => focus.Arm(sim);
        public static void Presented(XgSim sim, double now) => focus.Presented(sim, now);
        public static bool ActiveFor(XgSim sim, double now) => focus.ActiveFor(sim, now);
    }
}
