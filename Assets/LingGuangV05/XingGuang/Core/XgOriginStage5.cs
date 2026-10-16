using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Stage 5 of the origin film (film time 105–124 s): the seq2seq bottleneck, the attention weights and matrix, and the
    /// show-attend-tell bird. A port of the Remotion Stage5 scene as a pure function of scene-local time.
    /// </summary>
    public static class XgOriginStage5
    {
        static double R(double t, double a, double b, Func<double, double> ease = null) { return XgOriginFx.Ramp(t, a, b, ease); }
        public const double Start = 105, End = 124, Duration = End - Start;
        public const double FadeIn = .4, FadeOut = .4;

        public static readonly string[] Src = "I grew up by the sea , then moved north for school and work , and never went back .".Split(' ');
        /// <summary>Menlo 22 px character width.</summary>
        public const double CharW = 13.3;
        public static readonly double[] SrcX = SrcLayout();
        public const double BoxX = 960, BoxY = 530, BoxW = 300, BoxH = 70, SrcY = 360;
        /// <summary>Words ending in "?" markers are the film's red {…} spans.</summary>
        public static readonly string[] Dec = { "我", "在", "海边", "长大", "，", "后来", "去了", "{？？}", "{？？}", "{……}" };

        public static readonly string[] Tgt = { "我", "真的", "很", "想念", "大海" };
        public static readonly string[] Src2 = { "I", "really", "miss", "the", "sea" };
        public static readonly double[][] Att =
        {
            new[] { .86, .05, .04, .02, .03 },
            new[] { .05, .84, .07, .02, .02 },
            new[] { .04, .5, .4, .02, .04 },
            new[] { .03, .07, .84, .02, .04 },
            new[] { .02, .02, .05, .14, .77 },
        };
        public static double Tx(int i) { return 400 + i * 170; }
        public const double MatX = 1440, MatY = 320, MatCell = 58;

        public static readonly string[] Words = { "一只", "鸟", "站在", "树枝", "上" };
        public static readonly string[] WordsEn = { "A", "bird", "sits", "on a", "branch" };
        public struct Focus { public double X, Y, R; public Focus(double x, double y, double r) { X = x; Y = y; R = r; } }
        public static readonly Focus[] Foci = { new Focus(540, 430, 300), new Focus(474, 440, 64), new Focus(474, 492, 52), new Focus(410, 506, 130), new Focus(474, 470, 96) };
        public const double ImgX = 240, ImgY = 220, ImgW = 600, ImgH = 420;

        public const string SpeechText = "你说「[又]」——所以，不是第一次了吧？";
        public const string SpeechTextEn = "You said \"[again]\" — so this isn't the first time?";
        public const double SpeechStart = 16.8;
        public static int SpeechTotal { get { return XgOriginColdOpen.PlainLength(SpeechText); } }

        static double[] SrcLayout()
        {
            var widths = new double[Src.Length];
            double total = 0;
            for (int i = 0; i < Src.Length; i++) { widths[i] = Src[i].Length * CharW + 16; total += widths[i]; }
            double x = 960 - total / 2;
            var res = new double[Src.Length];
            for (int i = 0; i < Src.Length; i++) { res[i] = x + widths[i] / 2; x += widths[i]; }
            return res;
        }

        public sealed class Frame
        {
            public double T;
            public double AA, BA, CA;
            public int Cur, WordIdx;
            public double CurP, FocusX, FocusY, FocusR;
            public double TagAlpha; public string YearText;
            public double DAlpha, DPulse;
            public XgOriginFx.SpeechState Speech;
        }

        public static void Evaluate(double t, Frame f)
        {
            f.T = t;
            f.AA = R(t, .1, .6) * (1 - R(t, 5.8, 6.4));
            f.BA = R(t, 6.2, 6.8) * (1 - R(t, 11.8, 12.4));
            f.CA = R(t, 12.2, 12.8) * (1 - R(t, 15.8, 16.3));
            f.Cur = Math.Min(4, (int)Math.Floor(XgOriginFx.Lin(t, 6.6, 11.6) * 5));
            f.CurP = R(t, 6.6 + f.Cur * 1.0, 6.9 + f.Cur * 1.0);
            f.WordIdx = Math.Min(4, (int)Math.Floor(XgOriginFx.Lin(t, 12.6, 15.6) * 5));
            var prev = Foci[Math.Max(0, f.WordIdx - 1)];
            var cur = Foci[f.WordIdx];
            double fp = R(t, 12.6 + f.WordIdx * .6, 12.95 + f.WordIdx * .6, XgOriginFx.EaseInOut);
            f.FocusX = XgOriginFx.Lerp(prev.X, cur.X, fp);
            f.FocusY = XgOriginFx.Lerp(prev.Y, cur.Y, fp);
            f.FocusR = XgOriginFx.Lerp(prev.R, cur.R, fp);
            XgOriginFx.StageTag(t, "2014–2015", out f.TagAlpha, out f.YearText);
            f.DAlpha = R(t, 16.2, 16.6);
            f.DPulse = 12 + 8 * Math.Sin(t * 5);
            f.Speech = XgOriginFx.Speech(t, SpeechStart, 9, SpeechTotal);
        }

        /// <summary>The state of source word j in the squeeze: position, font size, opacity, and whether it is dropped (red) rather than packed.</summary>
        public static void SrcWord(int j, double t, out double x, out double y, out double size, out double opacity, out bool lostRed)
        {
            double st = .4 + j * .2;
            double p = XgOriginFx.Ramp(t, st, st + .5, XgOriginFx.EaseInOut);
            bool lost = j >= 8;
            x = XgOriginFx.Lerp(SrcX[j], BoxX - 128 + Math.Min(j, 7) * 32 + 12, p);
            y = XgOriginFx.Lerp(SrcY, BoxY, p);
            size = XgOriginFx.Lerp(22, 14, p);
            opacity = 1; lostRed = false;
            if (lost && p > .7)
            {
                double q = XgOriginFx.Ramp(t, st + .35, st + 1.0);
                y = XgOriginFx.Lerp(BoxY - 40, BoxY - 10 + q * 120, q);
                x = x + (j % 2 != 0 ? 1 : -1) * q * 90;
                opacity = 1 - q; lostRed = true;
            }
            else if (!lost && p > .92) opacity = 0;
        }

        /// <summary>How filled vector slot k is (0..1).</summary>
        public static double SlotFill(int k, double t) { return XgOriginFx.Ramp(t, .9 + k * .2, 1.1 + k * .2); }
        public static double DecAlpha(int i, double t) { return XgOriginFx.Ramp(t, 2.9 + i * .22, 3.2 + i * .22); }
        public static double WordAlpha(int i, double t) { return XgOriginFx.Ramp(t, 12.6 + i * .6, 12.9 + i * .6); }
    }
}
