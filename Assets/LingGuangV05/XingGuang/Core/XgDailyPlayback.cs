using System;

namespace LingGuangV05.XingGuang
{
    // Evidence only: never controls playback, price, volume or training.
    public sealed class XgDailyPlayback
    {
        string song = "", ticket = "";
        double previous, duration;
        bool valid, consumed;
        public string Song => song;
        public string Ticket => ticket;
        public void Reset() { song = ticket = ""; previous = duration = 0; valid = consumed = false; }
        public void Observe(string id, double position, double length, double elapsed, bool playing)
        {
            if (!playing) return;
            double tolerance = Math.Max(.15, Math.Max(0, elapsed) + .05);
            if (string.IsNullOrEmpty(id) || length <= 0 || double.IsNaN(position) || double.IsInfinity(position)) { Reset(); return; }
            if (song != id || position + .05 < previous || consumed)
            { song = id; ticket = Guid.NewGuid().ToString("N"); valid = position <= tolerance; consumed = false; previous = position; duration = length; return; }
            if (position - previous > tolerance || Math.Abs(duration - length) > .01) valid = false;
            previous = position; duration = length;
        }
        public bool Complete(double elapsed)
        {
            if (consumed || !valid || string.IsNullOrEmpty(ticket)) return false;
            // The final audio frame need not have been sampled, but skipped portions never count.
            if (duration - previous > Math.Max(.15, Math.Max(0, elapsed) + .05)) return false;
            consumed = true; return true;
        }
    }
}
