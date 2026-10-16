using System;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>One milestone on the finale's capability-versus-parameters timeline.</summary>
    public sealed class XgFinaleNode
    {
        public readonly string Name, Params, Month, Say, NameEn, ParamsEn, MonthEn, SayEn;
        public readonly double H;
        public XgFinaleNode(string name, string p, string say, string month, double h, string nameEn, string pEn, string sayEn, string monthEn)
        { Name = name; Params = p; Say = say; Month = month; H = h; NameEn = nameEn; ParamsEn = pEn; SayEn = sayEn; MonthEn = monthEn; }
    }

    /// <summary>
    /// The finale of the origin film (film time 145–161 s): the capability and loss curves, the collapse into one point, the burst,
    /// the title, the embers and the references. A port of the Remotion Finale scene as a pure function of scene-local time.
    /// </summary>
    public static class XgOriginFinale
    {
        static double R(double t, double a, double b, Func<double, double> ease = null) { return XgOriginFx.Ramp(t, a, b, ease); }
        public const double Start = 145, End = 161, Duration = End - Start;
        public const double FadeIn = .4, FadeOut = 1.4;
        public const double Boom = 7.6;
        public const double Cx = 960, Cy = 450;
        public const double Base = 680, Top = 200;

        /// <summary>The big centre word: the film's own 涌现 (the one place the game shows that word, at the user's request).</summary>
        public const string TitleZh = "涌现";
        public const string TitleEn = "EMERGENCE";
        /// <summary>The line under the title: the film is not the end, the last page (the rules page of the finale) is left to the player.</summary>
        public const string SubLineEn = "EMERGENCE · THE LAST PAGE IS YOURS";
        public const string SubLine = "EMERGENCE · 最后一页，留给你";

        public static readonly XgFinaleNode[] Nodes =
        {
            new XgFinaleNode("感知机", "3", "「是。否。」", "6月", .04, "Perceptron", "3", "“Yes. No.”", "Jun"),
            new XgFinaleNode("多层感知机", "17", "「……不确定」", "7月", .07, "MLP", "17", "“…not sure”", "Jul"),
            new XgFinaleNode("CNN · RNN", "6 万", "「我。看。」", "8月", .11, "CNN · RNN", "60K", "“I. See.”", "Aug"),
            new XgFinaleNode("LSTM · ResNet", "2560 万", "「你今天还没跟我说话。」", "9月", .16, "LSTM · ResNet", "25.6M", "“You haven't spoken to me today.”", "Sep"),
            new XgFinaleNode("注意力", "1.2 亿", "「你说「又」？」", "10–11月", .22, "Attention", "120M", "“You said ‘again’?”", "Oct–Nov"),
            new XgFinaleNode("Transformer", "2.13 亿", "「你觉得呢？」", "12月", 1.0, "Transformer", "213M", "“What do you think?”", "Dec"),
        };
        /// <summary>The line it said at node i. Node 2 is the save's own first word (看图 or 读字).</summary>
        public static string NodeSay(int i, bool english, bool sequenceFirst)
        {
            if (i == 2 && sequenceFirst) return english ? "“I. Read.”" : "「我。读。」";
            return english ? Nodes[i].SayEn : Nodes[i].Say;
        }

        /// <summary>
        /// The parameter count written under node i. The film's own picture (3, 17, 60K, 25.6M, 120M, 213M) when there is no real count;
        /// otherwise the same numbers the HUD counter passes (<see cref="XgOriginFilm.HudKeys"/>), ending on the player's own count.
        /// </summary>
        public static string NodeParams(int i, double finalParams, bool english)
        {
            var n = Nodes[i];
            if (i < 2 || Math.Abs(finalParams - XgOriginFilm.FilmFinalParams) <= 1) return english ? n.ParamsEn : n.Params;
            var keys = XgOriginFilm.HudKeys(finalParams);
            double v = i == 2 ? keys[5][1] : i == 3 ? keys[7][1] : i == 4 ? keys[9][1] : finalParams;
            return english ? XgSim.ParamsText(v / 1000) : XgOriginColdOpen.ParamsWordZh(v);
        }

        public static double Nx(int i) { return 300 + i * 265; }
        public static double Ny(int i) { return Base - Nodes[i].H * (Base - Top); }
        public static double Loss(double x) { return 250 + 330 * (1 - Math.Exp(-((x - 280) / 1400) * 2.4)); }

        public const string Refs =
            "Rosenblatt 1958 · Minsky & Papert 1969 · Rumelhart et al. 1986 · Elman 1990 · Hochreiter & Schmidhuber 1997 · LeCun et al. 1998 · Krizhevsky et al. 2012 · Cho et al. 2014 · Simonyan & Zisserman 2014 · Szegedy et al. 2014 · Bahdanau et al. 2014 · He et al. 2015 · Xu et al. 2015";

        public struct Spark { public double A, V, R, Life; public bool White, Amber, Streak; }
        public struct Ray { public double A, W, L; }
        public struct Ember { public double X, Y, Born, Life, Drift, Rise, R; }
        public static readonly Spark[] Sparks = new Spark[320];
        public static readonly Ray[] Rays = new Ray[28];
        public static readonly Ember[] Embers = new Ember[180];

        static string K(string p, int i) { return p + i.ToString(CultureInfo.InvariantCulture); }
        static double Rnd(string p, int i) { return XgOriginColdOpen.Rand(K(p, i)); }

        static XgOriginFinale()
        {
            for (int i = 0; i < Sparks.Length; i++)
            {
                double fast = Rnd("ff", i);
                Sparks[i] = new Spark
                {
                    A = Rnd("fa", i) * Math.PI * 2,
                    V = 300 + Math.Pow(fast, 1.6) * 1900,
                    R = .8 + Rnd("fr", i) * 2.6,
                    Life = .9 + Rnd("fl", i) * 2.6,
                    White = fast > .86,
                    Amber = !(fast > .86) && Rnd("fc", i) > .82,
                    Streak = fast > .55,
                };
            }
            for (int i = 0; i < Rays.Length; i++)
                Rays[i] = new Ray { A = (i / 28.0) * Math.PI * 2 + Rnd("ra", i) * .12, W = .6 + Rnd("rw", i) * 2.4, L = .55 + Rnd("rl", i) * .6 };
            for (int i = 0; i < Embers.Length; i++)
                Embers[i] = new Ember
                {
                    X = Cx + (Rnd("ex", i) - .5) * 560,
                    Y = Cy + (Rnd("ey", i) - .5) * 210,
                    Born = 9.6 + Rnd("eb", i) * 4.6,
                    Life = 2.2 + Rnd("el", i) * 2.6,
                    Drift = (Rnd("ed", i) - .5) * 70,
                    Rise = 60 + Rnd("er", i) * 170,
                    R = .8 + Rnd("erad", i) * 2.2,
                };
        }

        public sealed class Frame
        {
            public double T, Since;
            public double Pull, LineA, Slam, Flash, ShakeK, ShakeX, ShakeY, Chroma, Fade, TitleA, Scale, Blur, Breathe, Glow, Spread;
            public double AxesA, SubA, RefsA;
            /// <summary>The player's own numbers, set by the scene (the film's own by default).</summary>
            public double FinalParams = XgOriginFilm.FilmFinalParams;
            public bool SequenceFirst;
        }

        public static void Evaluate(double t, Frame f)
        {
            f.T = t;
            f.Pull = R(t, 6.7, Boom, XgOriginColdOpen.Cubic);
            f.LineA = 1 - R(t, 7.25, Boom);
            f.Since = t - Boom;
            double boomF = t * 30 - Math.Round(Boom * 30);
            f.Slam = boomF < 0 ? 0 : XgOriginFx.Spring(boomF, 8, 150, .7);
            f.Flash = f.Since < 0 ? 0 : 1 - R(t, Boom, Boom + .55);
            f.ShakeK = f.Since < 0 ? 0 : 1 - R(t, Boom, Boom + .9);
            long fr = (long)Math.Floor(t * 30 + 1e-6);
            f.ShakeX = (XgOriginColdOpen.Rand("sx" + fr.ToString(CultureInfo.InvariantCulture)) - .5) * 34 * f.ShakeK;
            f.ShakeY = (XgOriginColdOpen.Rand("sy" + fr.ToString(CultureInfo.InvariantCulture)) - .5) * 22 * f.ShakeK;
            f.Chroma = f.Since < 0 ? 0 : (1 - R(t, Boom, Boom + .7)) * 16;
            f.Fade = R(t, 10.2, 15.2, XgOriginFx.InOutSin);
            f.TitleA = (f.Since < 0 ? 0 : R(t, Boom, Boom + .08)) * (1 - f.Fade);
            f.Scale = XgOriginFx.Lerp(1.9, 1, f.Slam) * (1 + .07 * f.Fade);
            f.Blur = (f.Since < 0 ? 0 : (1 - R(t, Boom, Boom + .45)) * 22) + f.Fade * f.Fade * 10;
            f.Breathe = 1 + .1 * Math.Sin(t * 2.1);
            f.Glow = 1 + 1.6 * f.Flash;
            f.Spread = 36 + f.Fade * 26;
            f.AxesA = R(t, .1, .8) * (1 - R(t, 6.4, 7.0));
            f.SubA = R(t, Boom + 1, Boom + 2) * (1 - R(t, 10.6, 13.8));
            f.RefsA = R(t, 12.6, 13.4);
        }

        /// <summary>Where along the timeline node i's segment is drawn (0..1), and when it starts.</summary>
        public static double SegmentStart(int i) { return i == 5 ? 4.6 : .4 + i * .75; }
        public static double SegmentProgress(int i, double t)
        {
            double st = SegmentStart(i);
            return XgOriginFx.Ramp(t, st, st + (i == 5 ? .35 : .6), i == 5 ? (Func<double, double>)XgOriginColdOpen.Quad : null);
        }
        public static double NodeAlpha(int i, double t) { double st = i == 5 ? 4.95 : .3 + i * .75; return XgOriginFx.Ramp(t, st, st + .4); }
        public static double NodeTextAlpha(double t) { return 1 - XgOriginFx.Ramp(t, 6.4, 6.9); }
        public static double NodePop(int i, double t) { return i == 5 ? 1 - XgOriginFx.Ramp(t, 4.95, 5.8) : 0; }
        public static double LossDrawn(double t) { return XgOriginFx.Ramp(t, .3, 5.2, XgOriginColdOpen.Linear); }

        public static bool SparkAt(int i, double since, out double x, out double y, out double op, out double back)
        {
            var p = Sparks[i];
            x = y = op = back = 0;
            double k = Math.Min(1, since / p.Life);
            if (k >= 1 || since < 0) return false;
            double d = p.V * (1 - Math.Pow(1 - k, 3)) * .62;
            x = Cx + Math.Cos(p.A) * d;
            y = Cy + Math.Sin(p.A) * d * .7 + k * k * 60;
            op = (1 - k) * (1 - k);
            back = p.Streak && k < .45 ? Math.Max(8, p.V * .05 * (1 - k * 2)) : 0;
            return true;
        }

        public static bool EmberAt(int i, double t, out double x, out double y, out double op)
        {
            var e = Embers[i];
            x = y = op = 0;
            double k = (t - e.Born) / e.Life;
            if (k <= 0 || k >= 1) return false;
            x = e.X + e.Drift * k + Math.Sin(t * 1.3 + i) * 6;
            y = e.Y - e.Rise * (1 - Math.Pow(1 - k, 2));
            op = Math.Sin(Math.PI * k) * .85;
            return true;
        }
    }
}
