using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Stage 3, CNN and RNN (58–77.5 s), as pure functions of scene-local time: the 16×16 digit and its 3×3 edge-detector feature map,
    /// the pixels' journey (grid → one row → panel), the kernel scan, the depth bars and the RNN chain. A port of Remotion's Stage3.tsx.
    /// </summary>
    public static class XgOriginStage3
    {
        static readonly string[] Digit =
        {
            "................", "................", "..############..", "..############..", "...........###..", "..........###...", ".........###....", "........###.....",
            ".......###......", "......###.......", "......##........", ".....###........", ".....##.........", "....###.........", "....##..........", "................",
        };
        public static readonly int[] Pix = new int[256];
        /// <summary>The 14×14 map of edge strength, 0..1.</summary>
        public static readonly double[] FeatureMap = new double[196];

        public const double ImgX = 150, ImgY = 270, ImgCell = 16, FmX = 560, FmY = 286, FmCell = 16;

        public sealed class Bar { public string Name, Year; public int Layers; }
        public static readonly Bar[] Bars =
        {
            new Bar { Name = "LeNet", Year = "1998", Layers = 7 }, new Bar { Name = "AlexNet", Year = "2012", Layers = 8 },
            new Bar { Name = "VGG", Year = "2014", Layers = 19 }, new Bar { Name = "GoogLeNet", Year = "2014", Layers = 22 },
        };
        public static readonly string[] Tokens = { "今", "晚", "的", "月", "亮", "很", "圆" };
        public static readonly string[] TokensEn = { "Tonight", "the", "moon", "is", "very", "round", "." };
        public static double RnnX(int i) { return 1090 + i * 108; }
        public const double RnnY = 400;

        static XgOriginStage3()
        {
            for (int r = 0; r < 16; r++) for (int c = 0; c < 16; c++) Pix[r * 16 + c] = Digit[r][c] == '#' ? 1 : 0;
            double max = 0;
            for (int r = 0; r < 14; r++)
                for (int c = 0; c < 14; c++)
                {
                    double gx = 0, gy = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        gy += P(r, c + i) - P(r + 2, c + i);
                        gx += P(r + i, c) - P(r + i, c + 2);
                    }
                    double v = Math.Sqrt(gx * gx + gy * gy);
                    FeatureMap[r * 14 + c] = v;
                    max = Math.Max(max, v);
                }
            for (int i = 0; i < 196; i++) FeatureMap[i] /= max;
        }

        static int P(int r, int c) { return r < 0 || c < 0 || r > 15 || c > 15 ? 0 : Pix[r * 16 + c]; }

        public sealed class Frame
        {
            public double ToRow, ToPanel, Scan, SplitA, Dot, ReadoutA, RowNoteA, RnnA;
            public int KIdx, KC, KR;
        }

        public static void Evaluate(double t, Frame f)
        {
            f.ToRow = XgOriginColdOpen.Ramp(t, .3, 1.9, XgOriginColdOpen.EaseInOut);
            f.ToPanel = XgOriginColdOpen.Ramp(t, 2.4, 3.5, XgOriginColdOpen.EaseInOut);
            f.Scan = XgOriginColdOpen.Ramp(t, 3.6, 9.4, XgOriginColdOpen.Linear) * 196;
            f.KIdx = Math.Min(195, (int)Math.Floor(f.Scan));
            f.KC = f.KIdx % 14; f.KR = f.KIdx / 14;
            f.SplitA = XgOriginColdOpen.Ramp(t, 3.0, 3.8);
            f.Dot = XgOriginColdOpen.Ramp(t, 10.3, 14.0, XgOriginColdOpen.Linear) * (Tokens.Length - 1);
            f.RowNoteA = XgOriginColdOpen.Ramp(t, 1.4, 1.9) * (1 - XgOriginColdOpen.Ramp(t, 2.3, 2.7));
            f.RnnA = .3 * XgOriginColdOpen.Ramp(t, 3.6, 4.4) + .7 * XgOriginColdOpen.Ramp(t, 9.4, 10.2);
        }

        /// <summary>Pixel i's position and size: the digit grid, flattened to a row, then laid into the input panel.</summary>
        public static void PixelAt(int i, Frame f, out double x, out double y, out double size)
        {
            int r = i / 16, c = i % 16;
            double g0x = 960 - 176 + c * 22, g0y = 300 + r * 22, g0s = 20;
            double rx = 132 + i * 6.5, ry = 520, rs = 5.5;
            double g1x = ImgX + c * ImgCell, g1y = ImgY + r * ImgCell, g1s = ImgCell - 1.5;
            double d = Math.Min(1, Math.Max(0, f.ToRow * 1.15 - (i / 256.0) * .15));
            double ax = XgOriginColdOpen.Lerp(g0x, rx, d), ay = XgOriginColdOpen.Lerp(g0y, ry, d), asz = XgOriginColdOpen.Lerp(g0s, rs, d);
            x = XgOriginColdOpen.Lerp(ax, g1x, f.ToPanel); y = XgOriginColdOpen.Lerp(ay, g1y, f.ToPanel); size = XgOriginColdOpen.Lerp(asz, g1s, f.ToPanel);
        }
    }
}
