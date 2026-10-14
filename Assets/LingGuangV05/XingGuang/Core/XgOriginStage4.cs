using System;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Stage 4 of the origin film (film time 77.5–105 s): the vanishing signal along a sentence, the 20-vs-56-layer degradation plot,
    /// the LSTM conveyor and ResNet shortcuts. A port of the Remotion Stage4 scene as a pure function of scene-local time.
    /// </summary>
    public static class XgOriginStage4
    {
        static double R(double t, double a, double b, Func<double, double> ease = null) { return XgOriginFx.Ramp(t, a, b, ease); }
        public const double Start = 77.5, End = 105, Duration = End - Start;
        public const double FadeIn = .4, FadeOut = .4;

        public static readonly char[] Sentence = "我在海边长大，后来去北方读书工作，很多年没回去，现在最想念的是".ToCharArray();
        public const int Key = 2;
        public const double ChainY = 470;
        public static double Cx(int i) { return 150 + i * 56.5; }
        public static double Rb(int i) { return 160 + i * 62; }

        public const double PlotX = 560, PlotY = 230, PlotW = 800, PlotH = 440;
        public const int CurveN = 120;
        public static readonly double[] C20 = Curve("a", .07, 4.2), C56 = Curve("b", .15, 3.0);

        static double[] Curve(string seed, double floor, double k)
        {
            var ys = new double[CurveN];
            for (int i = 0; i < CurveN; i++)
            {
                double x = i / 119.0;
                ys[i] = floor + .8 * Math.Exp(-k * x) + (XgOriginColdOpen.Rand(seed + i.ToString(CultureInfo.InvariantCulture)) - .5) * .025 * (1 - x * .5);
            }
            return ys;
        }

        public const string SpeechText = "你今天还没跟我说话。你说过，你想[海]了。";
        public const string SpeechTextEn = "You haven't spoken to me today. You once said you missed the [sea].";
        public const double SpeechStart = 23.6;
        public static int SpeechTotal { get { return XgOriginColdOpen.PlainLength(SpeechText); } }

        public sealed class Frame
        {
            public double T;
            public double U, Pos, Sig, ChainA, PlotA, Draw, Hw, Flash, LstmA, Conv, ConvPos, ResA, Arcs, EndDim, Upper;
            public bool Hit;
            public int Layers;
            public double TagAlpha; public string YearText;
            public XgOriginFx.SpeechState Speech;
        }

        public static void Evaluate(double t, Frame f)
        {
            f.T = t;
            f.U = XgOriginFx.Lin(t, .8, 6.4);
            f.Pos = Key + f.U * (Sentence.Length - 1 - Key);
            f.Sig = Math.Pow(.75, f.Pos - Key);
            f.ChainA = R(t, .1, .8) * (1 - R(t, 6.8, 7.6));
            f.PlotA = R(t, 7.2, 8) * (1 - R(t, 13.4, 14.2));
            f.Draw = XgOriginFx.Lin(t, 7.6, 11.8);
            f.Hw = R(t, 14.2, 15.9);
            f.Hit = t >= 16;
            f.Flash = f.Hit ? 1 - R(t, 16, 16.45) : 0;
            f.LstmA = R(t, 16, 16.6);
            f.Conv = XgOriginFx.Lin(t, 16.4, 18.6);
            f.ConvPos = Key + f.Conv * (Sentence.Length - 1 - Key);
            f.ResA = R(t, 19, 19.6);
            f.Arcs = XgOriginFx.Lin(t, 19.2, 21.8);
            f.Layers = (int)Math.Floor(XgOriginFx.Lerp(20, 152, R(t, 19.4, 22.2)) + .5);
            f.EndDim = 1 - .8 * R(t, 22.8, 23.5);
            f.Upper = (f.Hit ? f.LstmA * (1 - .55 * f.ResA) : f.ChainA) * f.EndDim;
            XgOriginFx.StageTag(t, "1997 / 2015", out f.TagAlpha, out f.YearText);
            f.Speech = XgOriginFx.Speech(t, SpeechStart, 8, SpeechTotal);
        }

        /// <summary>How lit chain cell i is (0..1 of the lime fill strength, before the .05 base).</summary>
        public static double CellLit(Frame f, int i)
        {
            double lit = 0;
            if (!f.Hit && i >= Key && i <= f.Pos) lit = Math.Pow(.75, i - Key);
            if (f.Hit && i >= Key && i <= f.ConvPos) lit = Math.Pow(.997, i - Key) * .7;
            return lit;
        }

        public static string SignalLabel(Frame f)
        {
            double n = Math.Floor(f.Pos - Key);
            return "0.75^" + XgOriginFx.Fixed(n, 0) + " = " + (f.Sig < .001 ? XgOriginFx.Fixed(f.Sig, 4) : XgOriginFx.Fixed(f.Sig, 3));
        }

        /// <summary>Y of curve value v inside the plot (top-left, y down).</summary>
        public static double PlotYOf(double v) { return PlotY + PlotH - v * PlotH; }
        public static double PlotXOf(int i) { return PlotX + (i / (double)(CurveN - 1)) * PlotW; }
        /// <summary>How many curve points are drawn at draw-progress p (the film's path()).</summary>
        public static int CurvePoints(double p) { return Math.Max(2, (int)Math.Floor(CurveN * p)); }
    }
}
