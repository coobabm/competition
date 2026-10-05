namespace LingGuangV05.Core
{
    /// <summary>Unscaled presentation budget shared across desktop message channels; never persisted.</summary>
    public sealed class DesktopNotificationBudget
    {
        public const double SoundGap = 1.5, PopupGap = 7.5;
        double soundAt = double.NegativeInfinity, popupAt = double.NegativeInfinity;

        public bool TrySound(double now, bool muted, bool held)
        {
            if (double.IsNaN(now) || double.IsInfinity(now)) return false;
            if (muted || held || now - soundAt < SoundGap) return false;
            soundAt = now;
            return true;
        }

        public bool TryPopup(double now, bool held, bool trayBusy)
        {
            if (double.IsNaN(now) || double.IsInfinity(now)) return false;
            if (held || trayBusy || now - popupAt < PopupGap) return false;
            popupAt = now;
            return true;
        }

        public void Reset() { soundAt = popupAt = double.NegativeInfinity; }
    }
}
