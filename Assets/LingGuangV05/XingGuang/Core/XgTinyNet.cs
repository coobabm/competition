using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// A small deterministic random source (xorshift32). The tiny real nets use it instead of System.Random so the
    /// same seed gives the same weights under Unity's Mono and under .NET.
    /// </summary>
    public sealed class XgTinyRandom
    {
        uint state;
        public XgTinyRandom(int seed) { state = (uint)seed * 2654435761u ^ 0x9E3779B9u; if (state == 0) state = 1; Next(); }
        public uint Next() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return state; }
        /// <summary>Uniform in [0, 1).</summary>
        public double NextDouble() => (Next() >> 8) / 16777216.0;
        public double Range(double min, double max) => min + (max - min) * NextDouble();
        public int Below(int n) => n <= 0 ? 0 : (int)((Next() >> 8) % (uint)n);
    }

    /// <summary>One 8×8 picture with its yes/no answer.</summary>
    public sealed class XgTinySample
    {
        /// <summary>64 pixels, row by row from the top, each 0 (paper) to 1 (ink).</summary>
        public double[] pixels = new double[XgTinyData.Pixels];
        public bool label;
        /// <summary>The digit drawn (−1 for the XOR pictures).</summary>
        public int digit = -1;
    }

    /// <summary>
    /// The pictures the real nets learn from: the four XOR pictures (left lamp, right lamp) and hand-drawn 8×8
    /// digits for "is it a 0?". All generated from seeds; nothing is loaded from disk.
    /// </summary>
    public static class XgTinyData
    {
        public const int Side = 8, Pixels = Side * Side;

        /// <summary>
        /// The four XOR pictures: the left half lit means input A, the right half lit means input B, and the answer is
        /// "exactly one of them". The pictures are exact (no noise), so a single layer sees nothing but A and B.
        /// </summary>
        public static List<XgTinySample> Xor()
        {
            var list = new List<XgTinySample>();
            for (int a = 0; a < 2; a++)
                for (int b = 0; b < 2; b++)
                {
                    var s = new XgTinySample { label = (a ^ b) == 1 };
                    for (int y = 1; y < Side - 1; y++)
                        for (int x = 0; x < Side; x++)
                        {
                            bool left = x >= 1 && x <= 2, right = x >= 5 && x <= 6;
                            if (left && a == 1 || right && b == 1) s.pixels[y * Side + x] = 1;
                        }
                    list.Add(s);
                }
            return list;
        }

        static readonly string[][] Glyphs =
        {
            new[] { "..####..", ".##..##.", ".#....#.", ".#....#.", ".#....#.", ".#....#.", ".##..##.", "..####.." },
            new[] { "...##...", "..###...", "...##...", "...##...", "...##...", "...##...", "...##...", "..####.." },
            new[] { "..####..", ".#....#.", "......#.", ".....#..", "....#...", "...#....", "..#.....", ".######." },
            new[] { "..####..", ".#....#.", "......#.", "...###..", "......#.", "......#.", ".#....#.", "..####.." },
            new[] { "....##..", "...#.#..", "..#..#..", ".#...#..", ".######.", ".....#..", ".....#..", ".....#.." },
            new[] { ".######.", ".#......", ".#......", ".#####..", "......#.", "......#.", ".#....#.", "..####.." },
            new[] { "..####..", ".#......", ".#......", ".#####..", ".#....#.", ".#....#.", ".#....#.", "..####.." },
            new[] { ".######.", "......#.", ".....#..", "....#...", "...#....", "...#....", "...#....", "...#...." },
            new[] { "..####..", ".#....#.", ".#....#.", "..####..", ".#....#.", ".#....#.", ".#....#.", "..####.." },
            new[] { "..####..", ".#....#.", ".#....#.", "..#####.", "......#.", "......#.", ".....#..", "..###..." },
        };

        /// <summary>The zero in the hand of the deleted 0.txt: narrow and leaning, never in any training set.</summary>
        static readonly string[] TheZeroGlyph = { "...###..", "..#...#.", "..#...#.", ".#....#.", ".#...#..", ".#...#..", ".#..#...", "..##...." };

        /// <summary>A hand-drawn digit: the glyph shifted by up to one pixel sideways, uneven ink, a few stray or missing pixels.</summary>
        public static XgTinySample Digit(int digit, int seed)
        {
            var r = new XgTinyRandom(seed * 31 + digit * 7919 + 17);
            var glyph = Glyphs[Math.Max(0, Math.Min(9, digit))];
            int dx = r.Below(3) - 1;
            var s = new XgTinySample { digit = digit, label = digit == 0 };
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++)
                {
                    int sx = x - dx;
                    bool ink = sx >= 0 && sx < Side && glyph[y][sx] == '#';
                    double flip = r.NextDouble();
                    if (flip < .04) ink = !ink;
                    s.pixels[y * Side + x] = ink ? r.Range(.65, 1) : r.NextDouble() < .05 ? r.Range(.1, .3) : 0;
                }
            return s;
        }

        /// <summary>The 0.txt zero as a picture (always answer "0").</summary>
        public static XgTinySample TheZero()
        {
            var s = new XgTinySample { digit = 0, label = true };
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++)
                    if (TheZeroGlyph[y][x] == '#') s.pixels[y * Side + x] = .9;
            return s;
        }

        /// <summary>"Is it a 0?": half zeros, the other half spread over 1–9, interleaved so every pass sees both.</summary>
        public static List<XgTinySample> ZeroOrNot(int count, int seed)
        {
            var list = new List<XgTinySample>(count);
            for (int i = 0; i < count; i++)
            {
                int digit = i % 2 == 0 ? 0 : 1 + (i / 2) % 9;
                list.Add(Digit(digit, seed * 1000 + i));
            }
            return list;
        }
    }

    /// <summary>
    /// A real neural net small enough to train in milliseconds: either a perceptron (one layer, step output,
    /// Rosenblatt's rule) or a two-layer MLP (S-curve units, trained by backpropagation). Pure C#, deterministic.
    /// </summary>
    public sealed class XgTinyNet
    {
        public readonly int inputs, hidden;
        /// <summary>Hidden weights (hidden × inputs, row per unit) and biases. A perceptron keeps its only layer in <see cref="w2"/>.</summary>
        public readonly double[] w1, b1, w2;
        public double b2;
        readonly double[] h;

        public bool IsPerceptron => hidden == 0;
        public int ParameterCount => IsPerceptron ? inputs + 1 : hidden * inputs + hidden + hidden + 1;

        /// <param name="hidden">0 for a perceptron; otherwise the number of S-curve units in the hidden layer.</param>
        public XgTinyNet(int inputs, int hidden, int seed)
        {
            this.inputs = inputs; this.hidden = Math.Max(0, hidden);
            var r = new XgTinyRandom(seed);
            if (IsPerceptron)
            {
                w1 = new double[0]; b1 = new double[0]; h = new double[0];
                w2 = new double[inputs];
                for (int i = 0; i < inputs; i++) w2[i] = r.Range(-.05, .05);
                return;
            }
            w1 = new double[this.hidden * inputs]; b1 = new double[this.hidden]; w2 = new double[this.hidden]; h = new double[this.hidden];
            double scale = 1.5 / Math.Sqrt(inputs);
            for (int i = 0; i < w1.Length; i++) w1[i] = r.Range(-scale, scale);
            for (int j = 0; j < this.hidden; j++) { b1[j] = r.Range(-.5, .5); w2[j] = r.Range(-1, 1); }
        }

        static double Sigmoid(double z) => z >= 0 ? 1 / (1 + Math.Exp(-z)) : Math.Exp(z) / (1 + Math.Exp(z));

        /// <summary>The output for one picture: 0 or 1 for a perceptron, a probability of "yes" for the MLP.</summary>
        public double Forward(double[] x)
        {
            if (IsPerceptron)
            {
                double z = b2;
                for (int i = 0; i < inputs; i++) z += w2[i] * x[i];
                return z > 0 ? 1 : 0;
            }
            double o = b2;
            for (int j = 0; j < hidden; j++)
            {
                double z = b1[j];
                int row = j * inputs;
                for (int i = 0; i < inputs; i++) z += w1[row + i] * x[i];
                h[j] = Sigmoid(z);
                o += w2[j] * h[j];
            }
            return Sigmoid(o);
        }

        public bool Predict(double[] x) => Forward(x) > .5;

        /// <summary>One learning step on one picture.</summary>
        public void Learn(XgTinySample s, double rate)
        {
            double y = s.label ? 1 : 0;
            double o = Forward(s.pixels);
            if (IsPerceptron)
            {
                // Rosenblatt (1958): move the line only when the answer was wrong.
                double e = y - o;
                if (e == 0) return;
                for (int i = 0; i < inputs; i++) w2[i] += rate * e * s.pixels[i];
                b2 += rate * e;
                return;
            }
            // Backpropagation (Rumelhart, Hinton, Williams 1986): the output's error flows back through the S-curves.
            double d = o - y; // cross-entropy gradient at a sigmoid output
            for (int j = 0; j < hidden; j++)
            {
                double back = d * w2[j] * h[j] * (1 - h[j]);
                w2[j] -= rate * d * h[j];
                b1[j] -= rate * back;
                int row = j * inputs;
                for (int i = 0; i < inputs; i++) w1[row + i] -= rate * back * s.pixels[i];
            }
            b2 -= rate * d;
        }

        /// <summary>Mean squared error over a set: for a perceptron this is simply the share of wrong answers.</summary>
        public double Loss(IList<XgTinySample> data)
        {
            if (data == null || data.Count == 0) return 0;
            double sum = 0;
            foreach (var s in data) { double e = (s.label ? 1 : 0) - Forward(s.pixels); sum += e * e; }
            return sum / data.Count;
        }

        public double ErrorRate(IList<XgTinySample> data)
        {
            if (data == null || data.Count == 0) return 0;
            int wrong = 0;
            foreach (var s in data) if (Predict(s.pixels) != s.label) wrong++;
            return wrong / (double)data.Count;
        }

        /// <summary>The weights one unit gives the 64 pixels (an 8×8 picture of what it looks for).</summary>
        public double[] UnitWeights(int unit)
        {
            var w = new double[inputs];
            if (IsPerceptron) { Array.Copy(w2, w, inputs); return w; }
            Array.Copy(w1, Math.Max(0, Math.Min(hidden - 1, unit)) * inputs, w, 0, inputs);
            return w;
        }

        public int Units => IsPerceptron ? 1 : hidden;
    }

    /// <summary>Which real-training moment (design v1.1 §11.8.1).</summary>
    public enum XgTinyLesson { XorPerceptron, XorMlp, Zero }

    /// <summary>
    /// One live real-training run for the desktop: the net, its pictures, and the loss after every pass. The desktop
    /// calls <see cref="Step"/> a few passes per frame so the player watches the real error fall (or not fall).
    /// </summary>
    public sealed class XgTinyRun
    {
        public const int Seed = 2016;
        public readonly XgTinyLesson lesson;
        public readonly XgTinyNet net;
        public readonly List<XgTinySample> train, test;
        public readonly int epochs;
        public readonly double rate;
        /// <summary>Loss (mean squared error on the training pictures) before training and after every pass (every picture for a perceptron).</summary>
        public readonly List<float> loss = new List<float>();
        public int Epoch { get; private set; }
        public bool Done => Epoch >= epochs;
        /// <summary>Wall-clock time spent inside real training, in milliseconds.</summary>
        public double Milliseconds => watch.Elapsed.TotalMilliseconds;
        readonly Stopwatch watch = new Stopwatch();
        readonly XgTinyRandom order;
        readonly int[] index;

        public XgTinyRun(XgTinyLesson lesson, int seed = Seed)
        {
            this.lesson = lesson;
            switch (lesson)
            {
                case XgTinyLesson.XorPerceptron:
                    train = XgTinyData.Xor(); test = train;
                    net = new XgTinyNet(XgTinyData.Pixels, 0, seed); epochs = 60; rate = .1;
                    break;
                case XgTinyLesson.XorMlp:
                    train = XgTinyData.Xor(); test = train;
                    net = new XgTinyNet(XgTinyData.Pixels, 4, seed); epochs = 400; rate = .15;
                    break;
                default:
                    train = XgTinyData.ZeroOrNot(60, seed); test = XgTinyData.ZeroOrNot(40, seed + 1);
                    net = new XgTinyNet(XgTinyData.Pixels, 3, seed); epochs = 120; rate = .02;
                    break;
            }
            order = new XgTinyRandom(seed + 99);
            index = new int[train.Count];
            for (int i = 0; i < index.Length; i++) index[i] = i;
            loss.Add((float)net.Loss(train));
        }

        /// <summary>Runs up to <paramref name="passes"/> passes over the training pictures, each in a fresh order.</summary>
        public void Step(int passes = 1)
        {
            watch.Start();
            for (int p = 0; p < passes && !Done; p++)
            {
                for (int i = index.Length - 1; i > 0; i--) { int j = order.Below(i + 1); int t = index[i]; index[i] = index[j]; index[j] = t; }
                foreach (int i in index)
                {
                    net.Learn(train[i], rate);
                    // The perceptron is shown after every picture, so its error visibly jumps around without settling.
                    if (net.IsPerceptron) loss.Add((float)net.Loss(train));
                }
                Epoch++;
                if (!net.IsPerceptron) loss.Add((float)net.Loss(train));
            }
            watch.Stop();
        }

        /// <summary>Trains to the end at once (tests, or a skipped animation).</summary>
        public XgTinyRun Finish() { Step(epochs); return this; }

        public double TrainError => net.ErrorRate(train);
        public double TestError => net.ErrorRate(test);
        public int Parameters => net.ParameterCount;
    }
}
