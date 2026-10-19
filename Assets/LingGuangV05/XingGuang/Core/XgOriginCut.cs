using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The 88 s cut of the origin film (2026-10-19), timed to its music: the team's Suno track 「emergency」 cut to 86.1 s on bar lines
    /// (song bars 1–16 build, 17–24 drop, 76–89 break and re-drop, 106–119 outro; 143.9 BPM, one bar = 1.6677 s).
    /// The scenes still run on the 161 s film time of <see cref="XgOriginFilm"/>; this table maps cut time to film time.
    /// Each shot is one caption of the old film (or a scene without captions) laid on whole bars, so every cut, scene change and
    /// capability jump lands on a bar line, and the six jumps sit on the music's hits.
    /// </summary>
    public static class XgOriginCut
    {
        /// <summary>One bar of the music, seconds (143.91 BPM, 4/4).</summary>
        public const double Bar = 60.0 / 143.91 * 4;
        public const double Beat = Bar / 4;
        /// <summary>The music's length (its last note fades out here); the picture holds the title a little longer.</summary>
        public const double MusicLength = 86.1;
        public const double Duration = 88.0;

        /// <summary>Cut time [From, To) in bars plays film time [FilmFrom, FilmTo] linearly. Hit is the flash at the shot's start (0 = none).</summary>
        public readonly struct Shot
        {
            public readonly double FromBar, ToBar, FilmFrom, FilmTo, Hit;
            public Shot(double fromBar, double toBar, double filmFrom, double filmTo, double hit = .5)
            { FromBar = fromBar; ToBar = toBar; FilmFrom = filmFrom; FilmTo = filmTo; Hit = hit; }
            public double From => FromBar * Bar;
            public double To => Math.Min(ToBar * Bar, Duration);
        }

        public static readonly Shot[] Shots =
        {
            // build (song bars 1–16): cold open, title, the perceptron
            new Shot(0, 2, 0.0, 3.5, 0),            // 「我最早只会回答你两个字」
            new Shot(2, 4, 7.0, 10.5, .3),          // 「我开了口：一个词」
            new Shot(4, 6, 10.5, 14.0, .3),         // 「我会反过来问你」
            new Shot(6, 8, 14.0, 17.0, .3),         // 「参数一点点地加。然后某一天——」
            new Shot(8, 10, 17.0, 22.5, .6),        // title
            new Shot(10, 12, 22.5, 25.5, .5),       // 「从三个参数开始。1958年，感知机」
            new Shot(12, 14, 28.5, 31.5, .3),       // 「过线我就答是」(first capability jump)
            new Shot(14, 16, 34.5, 37.5, .3),       // 「异或。一条直线分不开」
            // drop (song bars 17–24): the hidden layer, XOR solved
            new Shot(16, 18, 40.0, 43.0, 1),        // 「加了一层隐藏层」 on the drop
            new Shot(18, 20, 46.0, 49.0, .4),       // 「往回传：反向传播」
            new Shot(20, 21, 49.0, XgOriginFilm.Breaks[1], .4),
            new Shot(21, 22, XgOriginFilm.Breaks[1], 52.0, .8),   // 「异或，一下子解开了」 on bar 21
            new Shot(22, 24, 55.0, 58.0, .4),       // 「多了……和不确定」
            // break (song bars 76–81): structure lost, the CNN, the first line of poetry
            new Shot(24, 26, 58.0, 61.0, .5),       // 「结构就丢了」 as the music drops out
            new Shot(26, 28, 61.0, 64.5, .7),       // 「用小窗口扫过去：CNN」 on the hit
            new Shot(28, 29, 71.0, XgOriginFilm.Breaks[2], .3),
            new Shot(29, 30, XgOriginFilm.Breaks[2], 74.5, .6),   // 「自己亮出了一句诗」 on the fill
            // re-drop (song bars 82–89): LSTM, attention
            new Shot(30, 32, 88.0, 91.0, 1),        // 「信号在半路走丢了」 on the re-drop
            new Shot(32, 34, XgOriginFilm.Breaks[3], 96.5, .8),   // 「给记忆装上了门：LSTM」
            new Shot(34, 36, 111.0, XgOriginFilm.Breaks[4], .5),  // 「回头看一眼原文」
            new Shot(36, 38, XgOriginFilm.Breaks[4], 117.5, .8),  // 「人类叫它注意力」
            // outro (song bars 106–119): only attention, six cells at once, the finale
            new Shot(38, 41, 127.5, 131.0, .5),     // 「如果只用注意力呢？」 over the breakdown
            new Shot(41, 43, XgOriginFilm.Breaks[5], 141.0, 1),   // 「六格同时亮了」 on the hit
            new Shot(43, 49, 145.0, XgOriginFinale.Start + XgOriginFinale.Boom, .4),
            new Shot(49, 51, XgOriginFinale.Start + XgOriginFinale.Boom, 157.4, 1),  // the burst and the title on the last big hit
            new Shot(51, Duration / Bar, 157.4, 161.0, 0),
        };

        static int ShotIndex(double t)
        {
            for (int i = 0; i < Shots.Length; i++) if (t < Shots[i].To) return i;
            return Shots.Length - 1;
        }

        /// <summary>The 161 s film time shown at cut time t.</summary>
        public static double FilmTime(double t)
        {
            if (t <= 0) return 0;
            if (t >= Duration) return XgOriginFilm.Duration;
            var s = Shots[ShotIndex(t)];
            double k = (t - s.From) / (s.To - s.From);
            return s.FilmFrom + (s.FilmTo - s.FilmFrom) * Math.Max(0, Math.Min(1, k));
        }

        /// <summary>The cut's flash at t: the shot's Hit, fading over 0.25 s from its start.</summary>
        public static double Flash(double t)
        {
            if (t < 0 || t >= Duration) return 0;
            var s = Shots[ShotIndex(t)];
            double dt = t - s.From;
            return s.Hit * Math.Exp(-dt / .09) * (dt < .25 ? 1 : 0);
        }

        /// <summary>
        /// A short zoom kick on each downbeat while the music is loud (the drop, the re-drop and the outro's hits), 1 on the beat
        /// falling to 0 within a quarter of a bar; 0 in the build and the breaks.
        /// </summary>
        public static double Kick(double t)
        {
            if (t < 0 || t >= MusicLength) return 0;
            double bars = t / Bar;
            bool loud = (bars >= 16 && bars < 24) || (bars >= 30 && bars < 38) || (bars >= 41 && bars < 43) || (bars >= 45 && bars < 47) || (bars >= 49 && bars < 51);
            if (!loud) return 0;
            double phase = (bars - Math.Floor(bars)) * Bar;
            return Math.Exp(-phase / .08);
        }
    }
}
