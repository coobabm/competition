using System;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Shared pure helpers for the later origin-film scenes (stages 4–6 and the finale): the easings Remotion's stage code uses on top of
    /// <see cref="XgOriginColdOpen"/>'s, the typed-speech state and a port of Remotion's frame-stepped spring().
    /// </summary>
    public static class XgOriginFx
    {
        public static double Ramp(double t, double a, double b, Func<double, double> ease = null) { return XgOriginColdOpen.Ramp(t, a, b, ease); }
        public static double Lin(double t, double a, double b) { return XgOriginColdOpen.Ramp(t, a, b, XgOriginColdOpen.Linear); }
        public static double Lerp(double a, double b, double p) { return a + (b - a) * p; }
        public static double Clamp(double x, double a = 0, double b = 1) { return Math.Max(a, Math.Min(b, x)); }

        /// <summary>Easing.bezier(0.65, 0, 0.35, 1).</summary>
        public static double EaseInOut(double x) { return XgOriginColdOpen.Bezier(.65, 0, .35, 1, x); }
        /// <summary>Easing.out(Easing.cubic).</summary>
        public static double OutCubic(double x) { double i = 1 - x; return 1 - i * i * i; }
        /// <summary>Easing.inOut(Easing.sin).</summary>
        public static double InOutSin(double x) { return (1 - Math.Cos(Math.PI * x)) * .5; }
        /// <summary>Easing.in(Easing.cubic) is Cubic; Easing.in(Easing.quad) is Quad.</summary>
        public static double Cubic(double x) { return x * x * x; }

        public static string Fixed(double v, int digits)
        {
            return v.ToString(digits == 0 ? "0" : "0." + new string('0', digits), CultureInfo.InvariantCulture);
        }

        // ───────────── scene chrome ─────────────

        /// <summary>The stage header: its fade-in and the scrambling year (resolves by 0.8 s).</summary>
        public static void StageTag(double t, string year, out double alpha, out string yearText)
        {
            alpha = Ramp(t, .1, .9);
            long f = (long)Math.Floor(t * 30 + 1e-6);
            yearText = t < .8 ? XgOriginColdOpen.Scramble(year, Ramp(t, .15, .8, XgOriginColdOpen.Linear), "yr" + f.ToString(CultureInfo.InvariantCulture)) : year;
        }

        /// <summary>The scene wrapper's fade: in over fadeIn, out (quad) over the last fadeOut.</summary>
        public static double SceneAlpha(double t, double dur, double fadeIn = .4, double fadeOut = .4)
        {
            return Math.Min(Ramp(t, 0, fadeIn), 1 - Ramp(t, dur - fadeOut, dur, XgOriginColdOpen.Quad));
        }

        // ───────────── typed speech ─────────────

        public struct SpeechState { public bool Visible; public double Alpha; public int Chars; public bool Cursor; }

        /// <summary>The film's typed "it says" block: appears 0.35 s before start, types at cps, caret blinks once done.</summary>
        public static SpeechState Speech(double t, double start, double cps, int total, double end = 999)
        {
            var s = new SpeechState();
            if (t < start - .35) return s;
            s.Visible = true;
            s.Alpha = Math.Min(Ramp(t, start - .35, start), 1 - Ramp(t, end - .4, end));
            int n = (int)Math.Floor((t - start) * cps);
            s.Chars = Math.Max(0, Math.Min(total, n));
            s.Cursor = n < total || (long)Math.Floor(t * 2.4) % 2 == 0;
            return s;
        }

        // ───────────── Remotion spring ─────────────

        /// <summary>Remotion's spring() from 0 to 1 at 30 fps, integrated frame by frame exactly as the library does. frame may be fractional.</summary>
        public static double Spring(double frame, double damping, double stiffness, double mass, double fps = 30)
        {
            double toValue = 1, current = 0, velocity = 0, last = 0;
            double fc = Math.Max(0, frame);
            int whole = (int)Math.Floor(fc);
            for (int f = 0; f <= whole; f++) Advance(f / fps * 1000, ref current, ref velocity, ref last, toValue, damping, stiffness, mass);
            if (fc % 1 > 0) Advance(fc / fps * 1000, ref current, ref velocity, ref last, toValue, damping, stiffness, mass);
            return current;
        }

        static void Advance(double now, ref double current, ref double velocity, ref double last, double toValue, double c, double k, double m)
        {
            double dtMs = Math.Min(now - last, 64);
            double v0 = -velocity, x0 = toValue - current;
            double zeta = c / (2 * Math.Sqrt(k * m)), omega0 = Math.Sqrt(k / m);
            double omega1 = omega0 * Math.Sqrt(1 - zeta * zeta);
            double t = dtMs / 1000;
            double sin1 = Math.Sin(omega1 * t), cos1 = Math.Cos(omega1 * t);
            double env = Math.Exp(-zeta * omega0 * t);
            double frag = env * (sin1 * ((v0 + zeta * omega0 * x0) / omega1) + x0 * cos1);
            double pos = toValue - frag;
            double vel = zeta * omega0 * frag - env * (cos1 * (v0 + zeta * omega0 * x0) - omega1 * x0 * sin1);
            if (zeta >= 1)
            {
                double e2 = Math.Exp(-omega0 * t);
                pos = toValue - e2 * (x0 + (v0 + omega0 * x0) * t);
                vel = e2 * (v0 * (t * omega0 - 1) + t * x0 * omega0 * omega0);
            }
            last = now; current = pos; velocity = vel;
        }
    }
}
