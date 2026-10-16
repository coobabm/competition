using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// One timed caption line of the origin film. [lime] and {red} mark the highlighted words. A caption that quotes the player's
    /// parameter count has a second pair of templates (ZhPersonal / EnPersonal, {0} = the count) used when the player's number differs from the film's.
    /// </summary>
    public sealed class XgFilmCaption
    {
        public readonly double From, To;
        public readonly string Zh, En, ZhPersonal, EnPersonal;
        public XgFilmCaption(double from, double to, string zh, string en = "", string zhPersonal = null, string enPersonal = null)
        { From = from; To = to; Zh = zh; En = en; ZhPersonal = zhPersonal; EnPersonal = enPersonal; }
    }

    /// <summary>One question-and-answer beat of the cold open: the architecture, the year, the neuron count and what the AI said back then.</summary>
    public sealed class XgFilmPhase
    {
        public readonly double From, To, Size, CharsPerSecond;
        public readonly string Question, QuestionEn, Answer, AnswerEn, Arch;
        public readonly int Year, Nodes;
        public XgFilmPhase(double from, double to, string q, string a, string arch, int year, int nodes, double size, double cps, string qEn = "", string aEn = "")
        { From = from; To = to; Question = q; Answer = a; Arch = arch; Year = year; Nodes = nodes; Size = size; CharsPerSecond = cps; QuestionEn = qEn; AnswerEn = aEn; }
    }

    /// <summary>
    /// The origin film's cold open (0–17 s) as a pure function of time: a port of the Remotion project's ColdOpen scene,
    /// its easing helpers and its seeded random. Nothing here touches Unity, so any frame can be evaluated (and tested) alone.
    /// Everything on screen — the neuron field, the counter, the Q/A beats, the collapse, the captions — is read from
    /// <see cref="Evaluate"/>; the view only draws what it returns.
    /// </summary>
    public static class XgOriginColdOpen
    {
        public const double Duration = 17;
        public const int NodeCount = 170;

        /// <summary>Parameter counts shown at the four beats (the film's own numbers). The last one is replaced by the player's when there is one.</summary>
        public static readonly double[] DefaultParams = { 3, 17, 60000, 2.13e8 };

        public static readonly XgFilmPhase[] Phases =
        {
            // What the game really has: a spam question (yes), a 夸/骂 question it is unsure about, a picture it names with one word,
            // and the question it is asked about itself (the origin question's own answer, 「你教的」), which it turns back on you.
            new XgFilmPhase(0, 3.5, "「恭喜您中奖！点击领取」——是垃圾短信吗？", "是。", "PERCEPTRON", 1958, 4, 110, 4,
                "\"Congratulations, you won! Click to claim\" — is this spam?", "Yes."),
            new XgFilmPhase(3.5, 7, "这条弹幕，是在夸，还是在骂？", "……不确定。", "MLP", 1986, 20, 72, 11,
                "This danmaku comment: praise, or insult?", "...Not sure."),
            new XgFilmPhase(7, 10.5, "［一张猫的照片］这是什么？", "猫。", "RNN", 1990, 64, 64, 11,
                "[A photo of a cat] What is this?", "Cat."),
            new XgFilmPhase(10.5, 14, "你是怎么学会的？", "你一张一张教的。你觉得，[我学得像吗]？", "TRANSFORMER", 2016, 170, 54, 11,
                "How did you learn all this?", "You taught me, card by card. What do you think—[did I learn it well]?"),
        };

        public static readonly string[] AllArch = { "PERCEPTRON", "MLP", "CNN · RNN", "LSTM · RESNET", "ATTENTION", "TRANSFORMER" };
        public static readonly int[] BarYears = { 1958, 1986, 1990, 1997, 2014, 2016 };

        /// <summary>The cold open's five captions: the first five lines of the film's master table (<see cref="XgOriginFilm.Captions"/>).</summary>
        public static readonly XgFilmCaption[] Captions =
        {
            XgOriginFilm.Captions[0], XgOriginFilm.Captions[1], XgOriginFilm.Captions[2], XgOriginFilm.Captions[3], XgOriginFilm.Captions[4],
        };

        // ───────────── Remotion helpers ─────────────

        static int HashCode(string s)
        {
            int hash = 0;
            foreach (char c in s) hash = unchecked((hash << 5) - hash + c);
            return hash;
        }

        /// <summary>Remotion's random(seed) for a string seed (mulberry32 over a Java-style string hash).</summary>
        public static double Rand(string seed)
        {
            uint a = unchecked((uint)HashCode(seed));
            uint t = unchecked(a + 0x6d2b79f5u);
            t = unchecked((t ^ (t >> 15)) * (t | 1u));
            t ^= unchecked(t + (t ^ (t >> 7)) * (t | 61u));
            return (t ^ (t >> 14)) / 4294967296.0;
        }

        /// <summary>CSS cubic-bezier easing (what Remotion's Easing.bezier is).</summary>
        public static double Bezier(double x1, double y1, double x2, double y2, double x)
        {
            if (x <= 0) return 0;
            if (x >= 1) return 1;
            double lo = 0, hi = 1, s = x;
            for (int i = 0; i < 40; i++)
            {
                double inv = 1 - s;
                double bx = 3 * inv * inv * s * x1 + 3 * inv * s * s * x2 + s * s * s;
                if (Math.Abs(bx - x) < 1e-9) break;
                if (bx < x) lo = s; else hi = s;
                s = (lo + hi) * .5;
            }
            double i2 = 1 - s;
            return 3 * i2 * i2 * s * y1 + 3 * i2 * s * s * y2 + s * s * s;
        }

        public static double EaseOut(double x) { return Bezier(.16, 1, .3, 1, x); }
        public static double EaseInOut(double x) { return Bezier(.65, 0, .35, 1, x); }
        public static double Linear(double x) { return x; }
        public static double Sigmoid(double x) { return 1 / (1 + Math.Exp(-x)); }
        public static double Quad(double x) { return x * x; }
        public static double Cubic(double x) { return x * x * x; }

        public static double Clamp(double x, double a = 0, double b = 1) { return Math.Max(a, Math.Min(b, x)); }
        public static double Lerp(double a, double b, double p) { return a + (b - a) * p; }

        /// <summary>0→1 over [a, b] with an easing curve, clamped; the film's ramp().</summary>
        public static double Ramp(double t, double a, double b, Func<double, double> ease = null)
        {
            double x = Clamp((t - a) / (b - a));
            return (ease ?? EaseOut)(x);
        }

        /// <summary>Fade in at a, hold, fade out toward b.</summary>
        public static double Win(double t, double a, double b, double fadeIn = .4, double fadeOut = .4)
        {
            return Math.Max(0, Math.Min(Ramp(t, a, a + fadeIn), 1 - Ramp(t, b - fadeOut, b, Quad)));
        }

        const string Glyphs = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#%&";

        /// <summary>Letters resolve left to right out of random glyphs; spaces stay.</summary>
        public static string Scramble(string str, double p, string seed)
        {
            var sb = new StringBuilder(str.Length);
            int keep = (int)Math.Floor(Clamp(p) * str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (i < keep || c == ' ') sb.Append(c);
                else sb.Append(Glyphs[(int)Math.Floor(Rand(seed + "-" + i.ToString(CultureInfo.InvariantCulture)) * Glyphs.Length)]);
            }
            return sb.ToString();
        }

        /// <summary>JS Math.round then toLocaleString('en-US'): 213,000,000.</summary>
        public static string FormatCount(double v)
        {
            return ((long)Math.Floor(v + .5)).ToString("N0", CultureInfo.InvariantCulture);
        }

        // ───────────── rich text: [lime] {red} ─────────────

        public enum Kind { Normal, Lime, Red }
        public struct Seg { public string Text; public Kind Kind; }

        public static List<Seg> Parse(string str)
        {
            var list = new List<Seg>();
            int i = 0;
            while (i < str.Length)
            {
                int a = str.IndexOfAny(new[] { '[', '{' }, i);
                if (a < 0) { list.Add(new Seg { Text = str.Substring(i), Kind = Kind.Normal }); break; }
                char open = str[a], close = open == '[' ? ']' : '}';
                int b = str.IndexOf(close, a + 1);
                if (b < 0) { list.Add(new Seg { Text = str.Substring(i), Kind = Kind.Normal }); break; }
                if (a > i) list.Add(new Seg { Text = str.Substring(i, a - i), Kind = Kind.Normal });
                list.Add(new Seg { Text = str.Substring(a + 1, b - a - 1), Kind = open == '[' ? Kind.Lime : Kind.Red });
                i = b + 1;
            }
            return list;
        }

        public static int PlainLength(string str)
        {
            int n = 0;
            foreach (var s in Parse(str)) n += s.Text.Length;
            return n;
        }

        // ───────────── the neuron field ─────────────

        public static readonly double[] NodeX = new double[NodeCount], NodeY = new double[NodeCount];
        /// <summary>Pairs (i, j) with j &lt; i: every node links to its two nearest older neighbours, so the web grows from the centre.</summary>
        public static readonly int[] EdgeA, EdgeB;

        static XgOriginColdOpen()
        {
            var raw = new List<(double x, double y, double d)>(NodeCount);
            for (int i = 0; i < NodeCount; i++)
            {
                double ang = Rand("a" + i.ToString(CultureInfo.InvariantCulture)) * Math.PI * 2;
                double r = Math.Pow(Rand("r" + i.ToString(CultureInfo.InvariantCulture)), .7);
                raw.Add((960 + Math.Cos(ang) * r * 820, 470 + Math.Sin(ang) * r * 330, r));
            }
            // Stable sort by distance from the centre (List.Sort is not stable).
            var order = new int[NodeCount];
            for (int i = 0; i < NodeCount; i++) order[i] = i;
            Array.Sort(order, (p, q) => { int c = raw[p].d.CompareTo(raw[q].d); return c != 0 ? c : p.CompareTo(q); });
            for (int i = 0; i < NodeCount; i++) { NodeX[i] = raw[order[i]].x; NodeY[i] = raw[order[i]].y; }
            var ea = new List<int>(); var eb = new List<int>();
            var near = new List<(int j, double d)>();
            for (int i = 0; i < NodeCount; i++)
            {
                near.Clear();
                for (int j = 0; j < i; j++) { double dx = NodeX[i] - NodeX[j], dy = NodeY[i] - NodeY[j]; near.Add((j, dx * dx + dy * dy)); }
                near.Sort((p, q) => { int c = p.d.CompareTo(q.d); return c != 0 ? c : p.j.CompareTo(q.j); });
                for (int k = 0; k < Math.Min(2, near.Count); k++) { ea.Add(i); eb.Add(near[k].j); }
            }
            EdgeA = ea.ToArray(); EdgeB = eb.ToArray();
        }

        // ───────────── the frame ─────────────

        /// <summary>Everything the view needs for one instant. Reuse one instance; <see cref="Evaluate"/> overwrites it.</summary>
        public sealed class Frame
        {
            public double T;
            public double SceneAlpha, SceneScale;
            /// <summary>How many neurons are on (fractional: the last one is fading in).</summary>
            public double Nodes, Collapse, FieldAlpha, HudAlpha;
            public double ParamValue, Spin;
            public string ParamText, Arch;
            public int Year;
            public double BarProgress;
            public int Phase;
            public readonly double[] QuestionAlpha = new double[4];
            public readonly double[] SpeechAlpha = new double[4];
            public readonly int[] SpeechChars = new int[4];
            public readonly bool[] SpeechCursor = new bool[4];
            public int Caption = -1;
            public double CaptionAlpha, CaptionBlur, CaptionLift;
            /// <summary>How lit neuron i is: 0 until it is reached, 1 once fully in.</summary>
            public double NodeVisibility(int i) { return Clamp(Nodes - i); }
        }

        /// <summary>
        /// The state at time t (seconds from the start of the film). finalParams is the last beat's parameter count; the
        /// counter then spins up to the same factor above it that the film uses (2.13e8 → 2e9).
        /// </summary>
        public static void Evaluate(double t, Frame f, double finalParams = 2.13e8, bool english = false)
        {
            var P = DefaultParams;
            f.T = t;
            f.SceneAlpha = Math.Min(Ramp(t, 0, .6), 1 - Ramp(t, Duration - .05, Duration, Quad));
            f.SceneScale = 1 + .02 * (t / Duration);

            int ph = -1;
            for (int i = 0; i < Phases.Length; i++) if (t >= Phases[i].From && t < Phases[i].To) { ph = i; break; }
            f.Phase = ph;

            double nCur = 0;
            for (int i = 0; i < Phases.Length; i++)
            {
                var p = Phases[i];
                double prev = i == 0 ? 0 : Phases[i - 1].Nodes;
                if (t >= p.From) nCur = Lerp(prev, p.Nodes, Ramp(t, p.From, p.From + 1.4));
            }
            if (t >= 14) nCur = NodeCount;
            f.Nodes = nCur;
            f.Collapse = Ramp(t, 15.4, 16.95, Cubic);
            f.FieldAlpha = 1 - Ramp(t, 16.6, 16.98);

            var cur = ph >= 0 ? Phases[ph] : Phases[3];
            string arch = cur.Arch;
            int year = cur.Year;
            double archP = ph >= 0 ? Ramp(t, cur.From, cur.From + .6, Linear) : 1;
            if (t >= 14)
            {
                int k = Math.Min(AllArch.Length - 1, (int)Math.Floor((t - 14) / .32));
                arch = AllArch[k];
                archP = ((t - 14) % .32) / .32 + .3;
                year = 2016;
            }
            int prevYear = ph > 0 ? Phases[ph - 1].Year : 1958;
            int shown = ph >= 0 ? (int)Math.Floor(Lerp(prevYear, cur.Year, Ramp(t, cur.From, cur.From + .8)) + .5) : year;
            f.Year = shown;
            f.BarProgress = (shown - 1958) / (double)(2016 - 1958);
            f.HudAlpha = Ramp(t, .2, 1) * (1 - Ramp(t, 16.7, 16.98));
            f.Arch = Scramble(arch, archP, "arch" + ((long)Math.Floor(t * 30 + 1e-6)).ToString(CultureInfo.InvariantCulture));

            double logP = Math.Log10(P[0]);
            for (int i = 1; i < Phases.Length; i++)
            {
                double from = Phases[i].From;
                double lo = Math.Log10(P[i - 1]);
                double hi = i == Phases.Length - 1 ? Math.Log10(finalParams) : Math.Log10(P[i]);
                if (t >= from) logP = Lerp(lo, hi, Ramp(t, from, from + .9));
            }
            if (t >= 14) logP = Lerp(Math.Log10(finalParams), Math.Log10(finalParams) + (9.3 - Math.Log10(2.13e8)), Ramp(t, 14, 16.9, Cubic));
            f.ParamValue = Math.Pow(10, logP);
            f.ParamText = FormatCount(f.ParamValue);
            f.Spin = Ramp(t, 14.5, 16.9, Quad);

            for (int i = 0; i < Phases.Length; i++)
            {
                var p = Phases[i];
                f.QuestionAlpha[i] = Win(t, p.From + .1, p.To - .05, .4, .3);
                double start = p.From + .75, end = p.To - .05;
                if (t < start - .35) { f.SpeechAlpha[i] = 0; f.SpeechChars[i] = 0; f.SpeechCursor[i] = false; continue; }
                f.SpeechAlpha[i] = Math.Min(Ramp(t, start - .35, start), 1 - Ramp(t, end - .4, end));
                // The English answer is longer: it types faster, so it still finishes where the Chinese one does.
                string answer = english && p.AnswerEn.Length > 0 ? p.AnswerEn : p.Answer;
                int total = PlainLength(answer);
                double cps = p.CharsPerSecond * (answer == p.Answer ? 1 : Math.Max(1, total / (double)PlainLength(p.Answer)));
                int n = (int)Math.Floor((t - start) * cps);
                f.SpeechChars[i] = Math.Max(0, Math.Min(total, n));
                f.SpeechCursor[i] = n < total || (long)Math.Floor(t * 2.4) % 2 == 0;
            }

            f.Caption = -1; f.CaptionAlpha = 0; f.CaptionBlur = 0; f.CaptionLift = 0;
            for (int i = 0; i < Captions.Length; i++)
            {
                var c = Captions[i];
                if (t < c.From || t >= c.To) continue;
                f.Caption = i;
                f.CaptionAlpha = Win(t, c.From, c.To, .35, .3);
                f.CaptionBlur = (1 - Ramp(t, c.From, c.From + .5)) * 8;
                f.CaptionLift = (1 - f.CaptionAlpha) * 10;
                break;
            }
        }

        /// <summary>
        /// The one personal touch: the player's own trained parameter count (thousands, as XgSim.TrainedParamsK) in place of the film's 2.13e8.
        /// Returns the film's number when there is nothing real to show (a fresh or tiny save would make the counter run backwards).
        /// </summary>
        public static double FinalParams(XgSim sim)
        {
            if (sim == null) return DefaultParams[3];
            double real = sim.TrainedParamsK * 1000.0;
            return real > DefaultParams[2] ? real : DefaultParams[3];
        }

        /// <summary>The count the way the film writes it in Chinese: 32 万, 2.13 亿.</summary>
        public static string ParamsWordZh(double v)
        {
            if (v >= 1e8) return (v / 1e8).ToString(v >= 1e9 ? "0" : "0.##", CultureInfo.InvariantCulture) + " 亿";
            if (v >= 1e4) return (v / 1e4).ToString(v >= 1e5 ? "0" : "0.#", CultureInfo.InvariantCulture) + " 万";
            return ((long)Math.Floor(v + .5)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Caption text (Chinese) for a cold-open line: personal when the player's count differs from the film's.</summary>
        public static string CaptionZh(int index, double finalParams) { return XgOriginFilm.CaptionText(index, false, finalParams); }
    }
}
