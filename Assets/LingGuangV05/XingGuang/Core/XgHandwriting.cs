using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One live guess of the handwriting recogniser: the digit it would say, how sure it is, and its odds for all ten.</summary>
    public struct XgHandwritingGuess
    {
        public int digit;
        public double confidence;
        public double[] odds;
        /// <summary>Cosine similarity to the closest memory of the guessed digit.</summary>
        public double similarity;
        public bool valid;
    }

    /// <summary>
    /// Handwriting for the 手写数字 desk (pure C#, no engine types). A drawing is a 64×64 ink raster; it is cropped to its
    /// bounding box, scaled to fit a 16×16 grid, centred on its centre of mass and softened, then compared by cosine
    /// similarity with everything the recogniser remembers: a few built-in printed templates per digit plus every
    /// drawing the player has handed in. It starts out poor and gets better only by remembering more of the player's hand.
    /// </summary>
    public static class XgHandwriting
    {
        public const int Grid = 64, Side = 16, Length = Side * Side;
        /// <summary>Fewest ink pixels and shortest bounding-box side (in grid cells) of a drawing worth keeping.</summary>
        public const int MinInk = 20, MinSpan = 10;
        const float Glyph = 12f;

        /// <summary>True when the raster holds a real stroke: not blank, not a dot or a tick.</summary>
        public static bool Usable(bool[] raster)
        {
            if (raster == null || raster.Length != Grid * Grid) return false;
            return Bounds(raster, out int minX, out int minY, out int maxX, out int maxY, out int ink)
                && ink >= MinInk && Math.Max(maxX - minX, maxY - minY) + 1 >= MinSpan;
        }

        static bool Bounds(bool[] r, out int minX, out int minY, out int maxX, out int maxY, out int ink)
        {
            minX = minY = Grid; maxX = maxY = -1; ink = 0;
            for (int y = 0; y < Grid; y++) for (int x = 0; x < Grid; x++) if (r[y * Grid + x])
            { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; ink++; }
            return ink > 0;
        }

        /// <summary>The 16×16 feature vector of a raster (unit length), or null when the drawing is not usable.</summary>
        public static float[] Extract(bool[] raster)
        {
            if (!Usable(raster)) return null;
            Bounds(raster, out int minX, out int minY, out int maxX, out int maxY, out _);
            int bw = maxX - minX + 1, bh = maxY - minY + 1;
            float scale = Glyph / Math.Max(bw, bh);
            // Centre of mass of the scaled glyph, then slide it to the middle of the grid (keeping it inside).
            double sx = 0, sy = 0; int n = 0;
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++) if (raster[y * Grid + x])
            { sx += (x - minX + .5f) * scale; sy += (y - minY + .5f) * scale; n++; }
            float ox = Shift(sx / n, bw * scale), oy = Shift(sy / n, bh * scale);
            var img = new float[Length];
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++) if (raster[y * Grid + x])
            {
                float fx = ox + (x - minX + .5f) * scale - .5f, fy = oy + (y - minY + .5f) * scale - .5f;
                int ix = (int)Math.Floor(fx), iy = (int)Math.Floor(fy);
                float ax = fx - ix, ay = fy - iy;
                Splat(img, ix, iy, (1 - ax) * (1 - ay)); Splat(img, ix + 1, iy, ax * (1 - ay));
                Splat(img, ix, iy + 1, (1 - ax) * ay); Splat(img, ix + 1, iy + 1, ax * ay);
            }
            Blur(img);
            double norm = 0; for (int i = 0; i < Length; i++) norm += img[i] * img[i];
            norm = Math.Sqrt(norm); if (norm < 1e-9) return null;
            for (int i = 0; i < Length; i++) img[i] = (float)(img[i] / norm);
            return img;
        }

        static float Shift(double com, float span)
        {
            double o = (Side - 1) / 2.0 + .5 - com;
            double lo = 1.5, hi = Side - 1.5 - span;
            if (hi < lo) hi = lo;
            return (float)Math.Max(lo, Math.Min(hi, o));
        }
        static void Splat(float[] img, int x, int y, float w) { if (x >= 0 && x < Side && y >= 0 && y < Side) img[y * Side + x] += w; }
        static void Blur(float[] img)
        {
            var tmp = new float[Length];
            for (int y = 0; y < Side; y++) for (int x = 0; x < Side; x++)
                tmp[y * Side + x] = (2 * img[y * Side + x] + (x > 0 ? img[y * Side + x - 1] : 0) + (x < Side - 1 ? img[y * Side + x + 1] : 0)) / 4f;
            for (int y = 0; y < Side; y++) for (int x = 0; x < Side; x++)
                img[y * Side + x] = (2 * tmp[y * Side + x] + (y > 0 ? tmp[(y - 1) * Side + x] : 0) + (y < Side - 1 ? tmp[(y + 1) * Side + x] : 0)) / 4f;
        }

        // ───────────── built-in templates ─────────────

        // Two printed fonts per digit (5×7 and 3×5 cells). They are drawn as thin strokes, like a pen would.
        static readonly string[] FontA = {
            "01110/10001/10011/10101/11001/10001/01110", "00100/01100/00100/00100/00100/00100/01110", "01110/10001/00001/00010/00100/01000/11111",
            "11110/00001/00001/01110/00001/00001/11110", "00010/00110/01010/10010/11111/00010/00010", "11111/10000/11110/00001/00001/10001/01110",
            "00110/01000/10000/11110/10001/10001/01110", "11111/00001/00010/00100/01000/01000/01000", "01110/10001/10001/01110/10001/10001/01110",
            "01110/10001/10001/01111/00001/00010/01100" };
        static readonly string[] FontB = {
            "111/101/101/101/111", "010/110/010/010/111", "111/001/111/100/111", "111/001/111/001/111", "101/101/111/001/001",
            "111/100/111/001/111", "111/100/111/101/111", "111/001/001/001/001", "111/101/111/101/111", "111/101/111/001/111" };

        /// <summary>Number of built-in templates per digit.</summary>
        public const int TemplatesPerDigit = 1;

        /// <summary>A built-in template as a 64×64 raster (strokes about as thick as the pad's pen).</summary>
        public static bool[] Template(int digit, int variant)
        {
            string[] rows = (variant == 0 ? FontA : FontB)[digit].Split('/');
            int h = rows.Length, w = rows[0].Length;
            float cell = variant == 0 ? 6f : 8f;
            float x0 = (Grid - (w - 1) * cell) / 2f, y0 = (Grid - (h - 1) * cell) / 2f;
            var r = new bool[Grid * Grid];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (rows[y][x] == '1')
            {
                float px = x0 + x * cell, py = y0 + y * cell;
                Dot(r, px, py);
                if (x + 1 < w && rows[y][x + 1] == '1') Line(r, px, py, px + cell, py);
                if (y + 1 < h && rows[y + 1][x] == '1') Line(r, px, py, px, py + cell);
            }
            return r;
        }

        /// <summary>The pen: a 3×3 dot, the same one the pad stamps along a stroke.</summary>
        public static void Dot(bool[] r, float x, float y)
        {
            int cx = (int)Math.Round(x), cy = (int)Math.Round(y);
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            { int xx = cx + dx, yy = cy + dy; if (xx >= 0 && xx < Grid && yy >= 0 && yy < Grid) r[yy * Grid + xx] = true; }
        }

        /// <summary>Stamps the pen along a segment (grid coordinates).</summary>
        public static void Line(bool[] r, float ax, float ay, float bx, float by)
        {
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(bx - ax), Math.Abs(by - ay))));
            for (int i = 0; i <= steps; i++) { float t = i / (float)steps; Dot(r, ax + (bx - ax) * t, ay + (by - ay) * t); }
        }
    }

    /// <summary>
    /// k-nearest-neighbour memory over 16×16 handwriting features. Each digit is scored by the mean of its k closest
    /// examples; the ten scores become odds. Built-in templates are never forgotten; the player's drawings are kept up to
    /// <see cref="LearnedCapacity"/>, the oldest dropping first.
    /// </summary>
    public sealed class XgHandwritingRecognizer
    {
        public const int LearnedCapacity = 100;
        /// <summary>How sharply the score gaps turn into odds.</summary>
        const double Sharpness = 28;
        /// <summary>A drawing of the player's own counts a little more than a printed template at equal similarity.</summary>
        const double OwnBonus = .02;
        /// <summary>Similarity at which a guess is treated as barely familiar, and as fully familiar.</summary>
        const double NearFloor = .55, NearCeil = .97;
        struct Example { public float[] v; public int digit; public bool mine; }
        readonly List<Example> examples = new List<Example>();
        int learned;

        public int Count => examples.Count;
        public int BuiltInCount => examples.Count - learned;
        /// <summary>How many of the player's drawings it remembers.</summary>
        public int LearnedCount => learned;

        public XgHandwritingRecognizer(bool builtIn = true)
        {
            if (!builtIn) return;
            for (int d = 0; d < 10; d++) for (int v = 0; v < XgHandwriting.TemplatesPerDigit; v++)
            {
                var f = XgHandwriting.Extract(XgHandwriting.Template(d, v));
                if (f != null) examples.Add(new Example { v = f, digit = d });
            }
        }

        /// <summary>Remembers one labelled drawing (a feature vector from <see cref="XgHandwriting.Extract"/>).</summary>
        public bool Teach(int digit, float[] feature, int capacity = LearnedCapacity)
        {
            if (digit < 0 || digit > 9 || feature == null || feature.Length != XgHandwriting.Length) return false;
            if (learned >= capacity)
            {
                int oldest = examples.FindIndex(e => e.mine);
                if (oldest >= 0) { examples.RemoveAt(oldest); learned--; }
            }
            examples.Add(new Example { v = (float[])feature.Clone(), digit = digit, mine = true });
            learned++;
            return true;
        }

        /// <summary>Its best guess for a feature vector. <paramref name="k"/> examples per digit are averaged (1 = nearest only).</summary>
        public XgHandwritingGuess Recognize(float[] feature, int k = 1)
        {
            var guess = new XgHandwritingGuess { digit = -1, odds = new double[10] };
            if (feature == null || feature.Length != XgHandwriting.Length || examples.Count == 0) return guess;
            k = Math.Max(1, Math.Min(5, k));
            var best = new double[10][];
            for (int d = 0; d < 10; d++) { best[d] = new double[k]; for (int i = 0; i < k; i++) best[d][i] = double.NegativeInfinity; }
            foreach (var e in examples)
            {
                double s = 0; for (int i = 0; i < XgHandwriting.Length; i++) s += feature[i] * e.v[i];
                if (e.mine) s += OwnBonus;
                var row = best[e.digit];
                // Insert into this digit's descending top-k list.
                for (int i = 0; i < k; i++) if (s > row[i]) { for (int j = k - 1; j > i; j--) row[j] = row[j - 1]; row[i] = s; break; }
            }
            var score = new double[10]; double top = double.NegativeInfinity;
            for (int d = 0; d < 10; d++)
            {
                double sum = 0; int n = 0;
                for (int i = 0; i < k; i++) if (!double.IsNegativeInfinity(best[d][i])) { sum += best[d][i]; n++; }
                score[d] = n == 0 ? double.NegativeInfinity : sum / n;
                if (score[d] > top) { top = score[d]; guess.digit = d; }
            }
            double total = 0;
            for (int d = 0; d < 10; d++) { guess.odds[d] = double.IsNegativeInfinity(score[d]) ? 0 : Math.Exp((score[d] - top) * Sharpness); total += guess.odds[d]; }
            for (int d = 0; d < 10; d++) guess.odds[d] /= total;
            // Sure only when it has seen something close: odds alone would be as bold with ten blurry templates as with a hundred drawings.
            guess.similarity = top;
            double near = Math.Max(0, Math.Min(1, (top - NearFloor) / (NearCeil - NearFloor)));
            guess.confidence = guess.odds[guess.digit] * near;
            guess.valid = true;
            return guess;
        }
    }
}
