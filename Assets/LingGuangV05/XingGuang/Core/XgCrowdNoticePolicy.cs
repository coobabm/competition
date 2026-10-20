using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>Presentation-only gate; it does not change app acquisition, income or data-pack stages.</summary>
    public static class XgCrowdNoticePolicy
    {
        public const int FirstStage = 3;
        public static bool IsCrowdSender(string sender) => !string.IsNullOrEmpty(sender)
            && (sender.IndexOf("摆渡众包", StringComparison.Ordinal) >= 0
                || sender.IndexOf("Bodu Crowd", StringComparison.OrdinalIgnoreCase) >= 0);

        public static bool AllowsMessage(string message, int stage) => stage >= FirstStage || string.IsNullOrEmpty(message)
            || !(message.StartsWith("摆渡众包", StringComparison.Ordinal)
                || message.StartsWith("Bodu Crowd", StringComparison.OrdinalIgnoreCase));

        // Null means an unrelated notification: preserve its existing lifecycle unchanged.
        // Crowd guards stay bound to their originating save and recheck the live stage on every call.
        public static Func<bool> Capture(string sender, XgSim origin, Func<XgSim> current)
        {
            if (!IsCrowdSender(sender)) return null;
            return () => origin != null && origin.S.stage >= FirstStage && current != null
                && ReferenceEquals(origin, current());
        }
    }
}
