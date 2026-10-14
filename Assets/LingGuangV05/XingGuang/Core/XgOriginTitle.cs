using System;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The origin film's title card (17–22.5 s) as a pure function of scene-local time: the burst of 140 particles, the flash, the
    /// flicker/blur/scale of the big title and the breathing of its glow. A port of Remotion's Title.tsx.
    /// </summary>
    public static class XgOriginTitle
    {
        public const int ParticleCount = 140;
        /// <summary>The big title the film is named for, in Chinese, and the line under it (the English word is fine in either language).</summary>
        public const string TitleZh = "学会";
        public const string TitleEn = "EMERGENCE";
        public static string SubtitleZh => "EMERGENCE · 从感知机到 TRANSFORMER";
        public static string SubtitleEn => "EMERGENCE · FROM PERCEPTRON TO TRANSFORMER";

        public static readonly double[] Angle = new double[ParticleCount], Speed = new double[ParticleCount], Radius = new double[ParticleCount], Life = new double[ParticleCount];

        static XgOriginTitle()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                string n = i.ToString(CultureInfo.InvariantCulture);
                Angle[i] = XgOriginColdOpen.Rand("pa" + n) * Math.PI * 2;
                Speed[i] = 180 + XgOriginColdOpen.Rand("pv" + n) * 820;
                Radius[i] = 1 + XgOriginColdOpen.Rand("pr" + n) * 2.4;
                Life[i] = 1.2 + XgOriginColdOpen.Rand("pl" + n) * 2.2;
            }
        }

        public struct Particle { public bool Alive; public double X, Y, R, Opacity; }

        /// <summary>Particle i at local time t (a burst from (960, 500), flattened to 60 % vertically, easing out).</summary>
        public static Particle ParticleAt(int i, double t)
        {
            double k = Math.Min(1, t / Life[i]);
            if (k >= 1) return default;
            double d = Speed[i] * (1 - Math.Pow(1 - k, 3));
            return new Particle { Alive = true, X = 960 + Math.Cos(Angle[i]) * d, Y = 500 + Math.Sin(Angle[i]) * d * .6, R = Radius[i], Opacity = (1 - k) * .8 };
        }

        public sealed class Frame
        {
            public double Flash, TitleAlpha, Blur, Scale, Breathe, SubtitleAlpha;
        }

        public static void Evaluate(double t, Frame f)
        {
            f.Flash = 1 - XgOriginColdOpen.Ramp(t, 0, .35);
            double inA = XgOriginColdOpen.Ramp(t, 0, .5);
            double flicker = t < .45 ? .75 + .25 * Math.Sin(t * 90) : 1;
            f.TitleAlpha = inA * flicker;
            f.Blur = (1 - XgOriginColdOpen.Ramp(t, 0, .6)) * 18;
            f.Scale = XgOriginColdOpen.Lerp(1.14, 1, XgOriginColdOpen.Ramp(t, 0, 1.4));
            f.Breathe = 1 + .08 * Math.Sin(t * 2.2);
            f.SubtitleAlpha = XgOriginColdOpen.Ramp(t, .8, 1.8);
        }
    }
}
