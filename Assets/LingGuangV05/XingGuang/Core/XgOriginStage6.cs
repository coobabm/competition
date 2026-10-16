using System;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Stage 6 of the origin film (film time 124–145 s): the serial RNN row, the crossed-out recurrence and convolution, the
    /// ATTENTION ONLY title, the all-to-all web, the six-layer stack and the capability table lighting up at once.
    /// A port of the Remotion Stage6 scene as a pure function of scene-local time.
    /// </summary>
    public static class XgOriginStage6
    {
        static double R(double t, double a, double b, Func<double, double> ease = null) { return XgOriginFx.Ramp(t, a, b, ease); }
        public const double Start = 124, End = 145, Duration = End - Start;
        public const double FadeIn = .4, FadeOut = .4;
        /// <summary>The boom moment, scene-local (film 137.5).</summary>
        public const double Boom = 13.5;

        public static readonly string[] Tok = { "它", "终于", "学会", "了", "同时", "看", "所有", "词" };
        public static readonly string[] TokEn = { "It", "finally", "learned", "to", "look", "at", "every", "word" };
        public static readonly string[] Caps = { "是/否", "多选一", "看图说词", "短句", "长记忆·翻译", "全部放开" };
        public static readonly string[] CapsEn = { "yes/no", "pick one", "picture to word", "short sentences", "memory · translation", "everything" };
        public const int HeadCount = 4;
        public static readonly string[] HeadHex = { "d4f55a", "7fe6ff", "ffb44d", "b9c3ff" };

        public const string SpeechText = "我明白了。不过我想先问你——你觉得，[是]，还是[否]？";
        public const string SpeechTextEn = "I see. But first let me ask you — do you think [yes], or [no]?";
        public const double SpeechStart = 17.8;
        public static int SpeechTotal { get { return XgOriginColdOpen.PlainLength(SpeechText); } }

        public static double RowX(int i) { return 330 + i * 180; }
        public const double RowY = 470;

        public static void Ring(int i, double cx, double cy, double r, out double x, out double y)
        {
            double a = -Math.PI / 2 + (i / (double)Tok.Length) * Math.PI * 2;
            x = cx + Math.Cos(a) * r; y = cy + Math.Sin(a) * r;
        }

        public sealed class Frame
        {
            public double T;
            public int Step;
            public double ArrowsA, TitleA, ToRing, ToLeft, WebA, TokDim, EndDim, BoomK, CapA, Cx, R;
            public readonly double[] PosX = new double[8], PosY = new double[8];
            public double TagAlpha; public string YearText;
            public double ShakeX, ShakeY;
            public XgOriginFx.SpeechState Speech;
        }

        public static void Evaluate(double t, Frame f)
        {
            f.T = t;
            f.Step = Math.Min(Tok.Length, (int)Math.Floor((t - .3) / .38) + 1);
            f.ArrowsA = 1 - R(t, 4.6, 5.6);
            f.TitleA = R(t, 7, 7.8) * (1 - R(t, 9.7, 10.3));
            f.ToRing = R(t, 9.9, 11.0, XgOriginFx.EaseInOut);
            f.ToLeft = R(t, 12.4, 13.3, XgOriginFx.EaseInOut);
            f.WebA = R(t, 10.6, 11.4);
            f.TokDim = t > 6.8 && t < 10 ? .25 : 1;
            f.EndDim = 1 - .85 * R(t, 17, 17.6);
            f.BoomK = t >= Boom ? 1 - R(t, Boom, Boom + .8) : 0;
            f.CapA = R(t, 12.9, 13.3) * (1 - R(t, 17, 17.6));
            f.Cx = XgOriginFx.Lerp(960, 560, f.ToLeft);
            f.R = XgOriginFx.Lerp(270, 200, f.ToLeft);
            for (int i = 0; i < 8; i++)
            {
                double rx, ry; Ring(i, f.Cx, 470, f.R, out rx, out ry);
                f.PosX[i] = XgOriginFx.Lerp(RowX(i), rx, f.ToRing);
                f.PosY[i] = XgOriginFx.Lerp(RowY, ry, f.ToRing);
            }
            long fr = (long)Math.Floor(t * 30 + 1e-6);
            f.ShakeX = (XgOriginColdOpen.Rand("x" + fr.ToString(CultureInfo.InvariantCulture)) - .5) * 26 * f.BoomK;
            f.ShakeY = (XgOriginColdOpen.Rand("y" + fr.ToString(CultureInfo.InvariantCulture)) - .5) * 16 * f.BoomK;
            XgOriginFx.StageTag(t, "2016.12", out f.TagAlpha, out f.YearText);
            f.Speech = XgOriginFx.Speech(t, SpeechStart, 10, SpeechTotal);
        }

        /// <summary>Strike-through state of the crossed-out words: (alpha, strike progress).</summary>
        public static void Crossed(int k, double t, out double alpha, out double strike)
        {
            double at = k == 0 ? 3.8 : 4.4;
            alpha = XgOriginFx.Ramp(t, at, at + .4) * (1 - XgOriginFx.Ramp(t, 6.6, 7.2));
            strike = XgOriginFx.Ramp(t, at + .4, at + .8);
        }

        /// <summary>Position of the flowing dot on the web edge (i, j) head h: the 0..1 phase along the curve.</summary>
        public static double Flow(double t, int i, int j, int h) { return (t * .9 + (i * 8 + j) * .07 + h * .25) % 1; }

        /// <summary>Whether the capability cells are lit (all six at once at the boom).</summary>
        public static bool CapsOn(double t) { return t >= Boom; }
        public static double CapFlicker(double t, int i)
        {
            return t < Boom && t > 13.2 ? .06 + .06 * Math.Max(0, Math.Sin(t * 17 + i * 2.1)) : 0;
        }

        /// <summary>Transformer layer i's rise.</summary>
        public static double StackA(int i, double t) { return XgOriginFx.Ramp(t, 13.7 + i * .28, 14.2 + i * .28); }
    }
}
