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

        /// <summary>
        /// Splits a set into the pictures it trains on and the ones it never sees (every <paramref name="every"/>-th, in
        /// order), so the microscope can draw the error on new pictures next to the error on trained ones.
        /// </summary>
        public static void HoldOut(IList<XgTinySample> all, int every, out List<XgTinySample> train, out List<XgTinySample> held)
        {
            train = new List<XgTinySample>(); held = new List<XgTinySample>();
            every = Math.Max(2, every);
            for (int i = 0; i < all.Count; i++) (i % every == every - 1 ? held : train).Add(all[i]);
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

        XgTinyNet(XgTinyNet from)
        {
            inputs = from.inputs; hidden = from.hidden;
            w1 = (double[])from.w1.Clone(); b1 = (double[])from.b1.Clone(); w2 = (double[])from.w2.Clone(); b2 = from.b2;
            h = new double[from.h.Length];
        }

        /// <summary>An independent copy (same weights), so a step can be shown without touching the original.</summary>
        public XgTinyNet Clone() => new XgTinyNet(this);

        /// <summary>The MLP's cross-entropy on one picture: the loss whose gradient <see cref="Learn"/> follows.</summary>
        public double CrossEntropy(XgTinySample s)
        {
            double o = Math.Max(1e-12, Math.Min(1 - 1e-12, Forward(s.pixels)));
            return s.label ? -Math.Log(o) : -Math.Log(1 - o);
        }

        /// <summary>
        /// One real backward pass on one picture without changing anything (纠错的瞬间, 它在学 §2.4): the gradient of
        /// <see cref="CrossEntropy"/> for every weight, the same numbers <see cref="Learn"/> steps along (Learn at rate r
        /// is w −= r · gradient). For a perceptron it is the negative of Rosenblatt's move: zero when the answer is right.
        /// </summary>
        public XgTinyGrad Gradients(XgTinySample s)
        {
            double y = s.label ? 1 : 0;
            double o = Forward(s.pixels);
            var g = new XgTinyGrad { output = o, error = o - y, w1 = new double[w1.Length], b1 = new double[b1.Length], w2 = new double[w2.Length], hidden = new double[hidden], blame = new double[hidden] };
            if (IsPerceptron)
            {
                double e = y - o;
                for (int i = 0; i < inputs; i++) g.w2[i] = -e * s.pixels[i];
                g.b2 = -e;
                return g;
            }
            double d = o - y;
            for (int j = 0; j < hidden; j++)
            {
                double back = d * w2[j] * h[j] * (1 - h[j]);
                g.hidden[j] = h[j]; g.blame[j] = back;
                g.w2[j] = d * h[j];
                g.b1[j] = back;
                int row = j * inputs;
                for (int i = 0; i < inputs; i++) g.w1[row + i] = back * s.pixels[i];
            }
            g.b2 = d;
            return g;
        }

        /// <summary>Moves every weight against <paramref name="g"/> by <paramref name="rate"/> (Learn's step, from a gradient already taken).</summary>
        public void Step(XgTinyGrad g, double rate)
        {
            for (int i = 0; i < w1.Length; i++) w1[i] -= rate * g.w1[i];
            for (int i = 0; i < b1.Length; i++) b1[i] -= rate * g.b1[i];
            for (int i = 0; i < w2.Length; i++) w2[i] -= rate * g.w2[i];
            b2 -= rate * g.b2;
        }
    }

    /// <summary>One backward pass of <see cref="XgTinyNet"/>: a gradient per weight, and what each hidden unit was blamed.</summary>
    public sealed class XgTinyGrad
    {
        /// <summary>Gradients in the net's own layout (w1 hidden × inputs, b1, w2, b2).</summary>
        public double[] w1, b1, w2;
        public double b2;
        /// <summary>The answer (probability of "yes") and its error o − y: the red dot at the output.</summary>
        public double output, error;
        /// <summary>Each hidden unit's output, and the error that reached it (d loss / d its input sum): 「该怪它」.</summary>
        public double[] hidden, blame;
    }

    /// <summary>
    /// A real convolutional net small enough to train in milliseconds (它在学 §2.3, after CNN Explainer): 8×8 picture →
    /// 4 kernels 3×3 (6×6 maps) → ReLU → 2×2 max-pool (3×3 maps) → one dense layer to the answer. With one class it
    /// answers yes/no (a sigmoid on <see cref="XgTinySample.label"/>); with more it names the digit (softmax over
    /// <see cref="XgTinySample.digit"/>). The kernels start as the four strokes 横 竖 撇 捺 plus a little noise and are
    /// then trained by backpropagation like everything else. Pure C#, deterministic from the seed.
    /// </summary>
    public sealed class XgTinyConvNet
    {
        public const int Kernels = 4, K = 3, MapSide = XgTinyData.Side - K + 1, PoolSide = MapSide / 2;
        public const int MapCells = MapSide * MapSide, PoolCells = PoolSide * PoolSide, Features = Kernels * PoolCells;
        public const int Seed = 2016;
        /// <summary>The four strokes the kernels start from (their names stay, the weights move with training).</summary>
        public static readonly string[] KernelNames = { "横", "竖", "撇", "捺" }, KernelNamesEn = { "horizontal", "vertical", "rising", "falling" };

        /// <summary>1 for yes/no; otherwise the number of digits it names.</summary>
        public readonly int classes;
        /// <summary>Kernels (4 × 9, row by row), their biases, the dense layer (outputs × 36) and its biases.</summary>
        public readonly double[] kernels, kernelBias, dense, denseBias;
        public int Outputs => classes <= 1 ? 1 : classes;
        public int ParameterCount => kernels.Length + kernelBias.Length + dense.Length + denseBias.Length;

        public XgTinyConvNet(int classes, int seed)
        {
            this.classes = Math.Max(1, classes);
            var r = new XgTinyRandom(seed);
            kernels = new double[Kernels * K * K]; kernelBias = new double[Kernels];
            string[] strokes = { "...###...", ".#..#..#.", "..#.#.#..", "#...#...#" };
            for (int m = 0; m < Kernels; m++)
                for (int i = 0; i < K * K; i++)
                    kernels[m * K * K + i] = (strokes[m][i] == '#' ? .6 : -.3) + r.Range(-.1, .1);
            dense = new double[Outputs * Features]; denseBias = new double[Outputs];
            double scale = 1 / Math.Sqrt(Features);
            for (int i = 0; i < dense.Length; i++) dense[i] = r.Range(-scale, scale);
        }

        /// <summary>Everything one picture lights up on its way through, for the 「它怎么看的」 view.</summary>
        public XgTinyConvTrace Trace(double[] x)
        {
            var t = new XgTinyConvTrace { input = x, maps = new double[Kernels * MapCells], pooled = new double[Features], from = new int[Features], logits = new double[Outputs], probs = new double[Outputs] };
            int n = XgTinyData.Side;
            for (int m = 0; m < Kernels; m++)
                for (int r = 0; r < MapSide; r++)
                    for (int c = 0; c < MapSide; c++)
                    {
                        double z = kernelBias[m];
                        for (int a = 0; a < K; a++)
                            for (int b = 0; b < K; b++)
                                z += kernels[m * K * K + a * K + b] * x[(r + a) * n + c + b];
                        t.maps[m * MapCells + r * MapSide + c] = z;
                    }
            for (int m = 0; m < Kernels; m++)
                for (int r = 0; r < PoolSide; r++)
                    for (int c = 0; c < PoolSide; c++)
                    {
                        // Max of the four ReLU'd cells; the first wins a tie (all four at 0 pass no gradient anyway).
                        int best = -1; double v = double.NegativeInfinity;
                        for (int a = 0; a < 2; a++)
                            for (int b = 0; b < 2; b++)
                            {
                                int k = m * MapCells + (2 * r + a) * MapSide + 2 * c + b;
                                double relu = Math.Max(0, t.maps[k]);
                                if (relu > v) { v = relu; best = k; }
                            }
                        int f = m * PoolCells + r * PoolSide + c;
                        t.pooled[f] = v; t.from[f] = best;
                    }
            for (int o = 0; o < Outputs; o++)
            {
                double z = denseBias[o];
                for (int f = 0; f < Features; f++) z += dense[o * Features + f] * t.pooled[f];
                t.logits[o] = z;
            }
            if (Outputs == 1) t.probs[0] = Sigmoid(t.logits[0]);
            else
            {
                double max = double.NegativeInfinity, sum = 0;
                foreach (var z in t.logits) max = Math.Max(max, z);
                for (int o = 0; o < Outputs; o++) { t.probs[o] = Math.Exp(t.logits[o] - max); sum += t.probs[o]; }
                for (int o = 0; o < Outputs; o++) t.probs[o] /= sum;
            }
            return t;
        }

        static double Sigmoid(double z) => z >= 0 ? 1 / (1 + Math.Exp(-z)) : Math.Exp(z) / (1 + Math.Exp(z));

        /// <summary>Yes/no: the probability of "yes". Digits: the probability that it is <paramref name="digit"/>.</summary>
        public double P(double[] x, int digit = 0)
        {
            var p = Trace(x).probs;
            return Outputs == 1 ? p[0] : p[Math.Max(0, Math.Min(Outputs - 1, digit))];
        }

        /// <summary>Its answer: yes/no, or the digit it finds most likely.</summary>
        public int Answer(double[] x)
        {
            var p = Trace(x).probs;
            if (Outputs == 1) return p[0] > .5 ? 1 : 0;
            int best = 0;
            for (int o = 1; o < Outputs; o++) if (p[o] > p[best]) best = o;
            return best;
        }

        public bool Right(XgTinySample s) => Outputs == 1 ? Answer(s.pixels) == 1 == s.label : Answer(s.pixels) == s.digit;

        public double ErrorRate(IList<XgTinySample> data)
        {
            if (data == null || data.Count == 0) return 0;
            int wrong = 0;
            foreach (var s in data) if (!Right(s)) wrong++;
            return wrong / (double)data.Count;
        }

        /// <summary>Cross-entropy on one picture (the loss <see cref="Learn"/> descends).</summary>
        public double CrossEntropy(XgTinySample s)
        {
            var p = Trace(s.pixels).probs;
            if (Outputs == 1) { double o = Math.Max(1e-12, Math.Min(1 - 1e-12, p[0])); return s.label ? -Math.Log(o) : -Math.Log(1 - o); }
            return -Math.Log(Math.Max(1e-12, p[Math.Max(0, Math.Min(Outputs - 1, s.digit))]));
        }

        /// <summary>One real backward pass (nothing changes): the gradient of <see cref="CrossEntropy"/> for every weight.</summary>
        public XgTinyConvGrad Gradients(XgTinySample s)
        {
            var t = Trace(s.pixels);
            var g = new XgTinyConvGrad { kernels = new double[kernels.Length], kernelBias = new double[Kernels], dense = new double[dense.Length], denseBias = new double[Outputs] };
            var dLogit = new double[Outputs];
            if (Outputs == 1) dLogit[0] = t.probs[0] - (s.label ? 1 : 0);
            else for (int o = 0; o < Outputs; o++) dLogit[o] = t.probs[o] - (o == s.digit ? 1 : 0);
            var dPooled = new double[Features];
            for (int o = 0; o < Outputs; o++)
            {
                g.denseBias[o] = dLogit[o];
                for (int f = 0; f < Features; f++)
                {
                    g.dense[o * Features + f] = dLogit[o] * t.pooled[f];
                    dPooled[f] += dLogit[o] * dense[o * Features + f];
                }
            }
            // The pool passes the error to the one cell that won; ReLU passes it only where that cell was above 0.
            int n = XgTinyData.Side;
            for (int f = 0; f < Features; f++)
            {
                int k = t.from[f];
                if (k < 0 || t.maps[k] <= 0) continue;
                int m = k / MapCells, r = k % MapCells / MapSide, c = k % MapSide;
                g.kernelBias[m] += dPooled[f];
                for (int a = 0; a < K; a++)
                    for (int b = 0; b < K; b++)
                        g.kernels[m * K * K + a * K + b] += dPooled[f] * s.pixels[(r + a) * n + c + b];
            }
            return g;
        }

        public void Learn(XgTinySample s, double rate)
        {
            var g = Gradients(s);
            for (int i = 0; i < kernels.Length; i++) kernels[i] -= rate * g.kernels[i];
            for (int i = 0; i < Kernels; i++) kernelBias[i] -= rate * g.kernelBias[i];
            for (int i = 0; i < dense.Length; i++) dense[i] -= rate * g.dense[i];
            for (int i = 0; i < denseBias.Length; i++) denseBias[i] -= rate * g.denseBias[i];
        }

        /// <summary>Plain SGD, every pass in a fresh order drawn from the seed.</summary>
        public static XgTinyConvNet Train(int classes, IList<XgTinySample> data, int epochs, double rate, int seed)
        {
            var net = new XgTinyConvNet(classes, seed);
            var order = new XgTinyRandom(seed + 99);
            var index = new int[data.Count];
            for (int i = 0; i < index.Length; i++) index[i] = i;
            for (int e = 0; e < epochs; e++)
            {
                for (int i = index.Length - 1; i > 0; i--) { int j = order.Below(i + 1); int t = index[i]; index[i] = index[j]; index[j] = t; }
                foreach (int i in index) net.Learn(data[i], rate);
            }
            return net;
        }

        /// <summary>Hand-drawn digits for naming 0–9: <paramref name="perDigit"/> of each, interleaved.</summary>
        public static List<XgTinySample> DigitSet(int perDigit, int seed)
        {
            var list = new List<XgTinySample>(perDigit * 10);
            for (int i = 0; i < perDigit * 10; i++) list.Add(XgTinyData.Digit(i % 10, seed * 1000 + i));
            return list;
        }

        static XgTinyConvNet digits;
        /// <summary>
        /// The net behind the sheet's 「它怎么看的」: names the digit, trained once at first use (3,000 steps, a few tens of
        /// milliseconds) and kept. Same seed, same weights, on any machine.
        /// </summary>
        public static XgTinyConvNet Digits => digits ?? (digits = Train(10, DigitSet(30, Seed), 10, .1, Seed));
    }

    /// <summary>One picture through <see cref="XgTinyConvNet"/>: the 4 maps before ReLU, the pooled maps, the answer.</summary>
    public sealed class XgTinyConvTrace
    {
        public double[] input;
        /// <summary>4 × 6×6 kernel outputs before ReLU (negative = ReLU sets it to 0).</summary>
        public double[] maps;
        /// <summary>4 × 3×3 after ReLU and 2×2 max-pool, and which map cell each one came from.</summary>
        public double[] pooled;
        public int[] from;
        public double[] logits, probs;
    }

    /// <summary>One backward pass of <see cref="XgTinyConvNet"/>, in the net's own layout.</summary>
    public sealed class XgTinyConvGrad
    {
        public double[] kernels, kernelBias, dense, denseBias;
    }

    /// <summary>Which real-training moment (design v1.1 §11.8.1), or the microscope the player opens any time (它在学 §2.1).</summary>
    public enum XgTinyLesson { XorPerceptron, XorMlp, Zero, Microscope }

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
        /// <summary>The microscope only: loss on the held-out pictures, before training and after every pass (empty otherwise).</summary>
        public readonly List<float> testLoss = new List<float>();
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
                case XgTinyLesson.Microscope:
                    // "Is it a 0?" on 48 pictures, every third held out; four units, so four heat maps (它在学 §2.1).
                    XgTinyData.HoldOut(XgTinyData.ZeroOrNot(48, seed), 3, out train, out test);
                    net = new XgTinyNet(XgTinyData.Pixels, 4, seed); epochs = 100; rate = .03;
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
            if (lesson == XgTinyLesson.Microscope) testLoss.Add((float)net.Loss(test));
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
                if (lesson == XgTinyLesson.Microscope) testLoss.Add((float)net.Loss(test));
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
