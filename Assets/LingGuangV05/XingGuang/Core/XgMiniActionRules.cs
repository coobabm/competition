using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>Deterministic geometry and local exemplar matching, not probabilistic recognition.</summary>
    public static class XgMiniActionRules
    {
        public static bool SegmentHits(float ax, float ay, float bx, float by, float x, float y, float w, float h)
        {
            if (w < 0 || h < 0) return false;
            float lo = 0, hi = 1;
            return Clip(-(bx - ax), ax - x, ref lo, ref hi) && Clip(bx - ax, x + w - ax, ref lo, ref hi)
                && Clip(-(by - ay), ay - y, ref lo, ref hi) && Clip(by - ay, y + h - ay, ref lo, ref hi);
        }
        static bool Clip(float p, float q, ref float lo, ref float hi)
        {
            if (Math.Abs(p) < 0.000001f) return q >= 0;
            float r = q / p;
            if (p < 0) { if (r > hi) return false; if (r > lo) lo = r; }
            else { if (r < lo) return false; if (r < hi) hi = r; }
            return true;
        }
        public static int Arithmetic(int a, int b, int op) => op == 0 ? a + b : op == 1 ? a - b : a * b;
        public static bool CircleHitsRect(float cx, float cy, float radius, float x, float y, float w, float h)
        {
            if (radius < 0 || w < 0 || h < 0) return false;
            float nearestX = Math.Max(x, Math.Min(x + w, cx));
            float nearestY = Math.Max(y, Math.Min(y + h, cy));
            float dx = cx - nearestX, dy = cy - nearestY;
            return dx * dx + dy * dy <= radius * radius;
        }
        public static float[] Normalize(bool[] pixels, int width, int height)
        {
            if (pixels == null || width <= 0 || height <= 0 || pixels.Length != width * height) return null;
            int minX = width, minY = height, maxX = -1, maxY = -1, ink = 0;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) if (pixels[y * width + x])
            { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); ink++; }
            if (ink < 6 || Math.Max(maxX - minX, maxY - minY) < 4) return null;
            var v = new float[400];
            float scale = 16f / Math.Max(maxX - minX + 1, maxY - minY + 1);
            float ox = (20 - (maxX - minX + 1) * scale) / 2, oy = (20 - (maxY - minY + 1) * scale) / 2;
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++) if (pixels[y * width + x])
            {
                int xx = Math.Max(0, Math.Min(19, (int)(ox + (x - minX) * scale)));
                int yy = Math.Max(0, Math.Min(19, (int)(oy + (y - minY) * scale)));
                v[yy * 20 + xx] = 1;
            }
            // Distance-to-ink features are less sensitive to stroke thickness than raw raster MSE.
            var result = new float[400];
            for (int i = 0; i < 400; i++)
            {
                float best = 400;
                for (int j = 0; j < 400; j++) if (v[j] > 0)
                { int dx = i % 20 - j % 20, dy = i / 20 - j / 20; best = Math.Min(best, dx * dx + dy * dy); }
                result[i] = (float)Math.Sqrt(best) / 20;
            }
            return result;
        }
    }
    public sealed class XgMiniActionRecognizer
    {
        readonly List<float[]> examples = new List<float[]>();
        readonly List<int> labels = new List<int>();
        public int Count => examples.Count;
        public void Teach(int digit, float[] feature)
        {
            if (digit < 0 || digit > 9 || feature == null || feature.Length != 400) return;
            if (examples.Count >= 110) { examples.RemoveAt(10); labels.RemoveAt(10); }
            examples.Add((float[])feature.Clone()); labels.Add(digit);
        }
        public int Recognize(float[] feature, out double distance)
        {
            distance = double.PositiveInfinity; int answer = -1;
            if (feature == null || feature.Length != 400) return answer;
            for (int e = 0; e < examples.Count; e++)
            {
                double d = 0;
                for (int i = 0; i < 400; i++) { double delta = feature[i] - examples[e][i]; d += delta * delta; }
                if (d < distance) { distance = d; answer = labels[e]; }
            }
            return answer;
        }
    }
}

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgMiniActionDrawing
    {
        public int digit;
        public string raster;
    }
    public sealed partial class XgState
    {
        public List<XgMiniActionDrawing> miniActionDrawings = new List<XgMiniActionDrawing>();
    }
    public static class XgMiniActionMemory
    {
        public const int Capacity = 100;
        public static string Encode(bool[] pixels)
        {
            if (pixels == null || pixels.Length != 4096) return null;
            var bytes = new byte[512];
            for (int i = 0; i < pixels.Length; i++) if (pixels[i]) bytes[i / 8] |= (byte)(1 << (i % 8));
            return Convert.ToBase64String(bytes);
        }
        public static bool[] Decode(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length != 684) return null;
            byte[] bytes; try { bytes = Convert.FromBase64String(text); } catch (FormatException) { return null; }
            if (bytes.Length != 512) return null;
            var pixels = new bool[4096];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = (bytes[i / 8] & (1 << (i % 8))) != 0;
            return pixels;
        }
        public static void Repair(XgState state, int capacity = Capacity)
        {
            if (state.miniActionDrawings == null) state.miniActionDrawings = new List<XgMiniActionDrawing>();
            state.miniActionDrawings.RemoveAll(x => x == null || x.digit < 0 || x.digit > 9 || Decode(x.raster) == null);
            if (state.miniActionDrawings.Count > capacity) state.miniActionDrawings.RemoveRange(0, state.miniActionDrawings.Count - capacity);
        }
        public static void Remember(XgState state, int digit, bool[] pixels, int capacity = Capacity)
        {
            if (digit < 0 || digit > 9 || pixels == null || pixels.Length != 4096) return;
            Repair(state, int.MaxValue);
            while (state.miniActionDrawings.Count >= Math.Max(1, capacity)) state.miniActionDrawings.RemoveAt(0);
            state.miniActionDrawings.Add(new XgMiniActionDrawing { digit = digit, raster = Encode(pixels) });
        }
    }
}
