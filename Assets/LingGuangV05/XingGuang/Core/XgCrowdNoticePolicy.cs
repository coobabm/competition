using System;
namespace LingGuangV05.XingGuang
{
    /// <summary>Live presentation gate for installed crowd clients; group introductions use their own sender.</summary>
    public static class XgCrowdNoticePolicy
    {
        public const int FirstStage = 2;
        public static bool IsCrowdSender(string sender) => !string.IsNullOrEmpty(sender)
            && (sender.IndexOf("摆渡众包", StringComparison.Ordinal) >= 0
                || sender.IndexOf("Bodu Crowd", StringComparison.OrdinalIgnoreCase) >= 0);
        public static bool AllowsIncome(XgSim sim) => sim != null && sim.S.stage >= FirstStage
            && sim.DesktopAppInstalled("zhongbao");
        public static bool AllowsMessage(string message, XgSim sim) => string.IsNullOrEmpty(message)
            || !(message.StartsWith("摆渡众包", StringComparison.Ordinal)
                || message.StartsWith("Bodu Crowd", StringComparison.OrdinalIgnoreCase)) || AllowsIncome(sim);
        public static Func<bool> Capture(string sender, XgSim origin, Func<XgSim> current)
        {
            if (!IsCrowdSender(sender)) return null;
            return () => AllowsIncome(origin) && current != null && ReferenceEquals(origin, current());
        }
    }
}
