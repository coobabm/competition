using System;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Stage 2, the multi-layer perceptron (40–58 s), as pure functions of scene-local time: the 2-4-1 network and its seeded edge weights, a tiny
    /// 2-2-1 MLP that creeps for a long plateau (stuck at 2/4) and then snaps to the XOR band, and the forward / backward pulses. A port of Remotion's Stage2.tsx.
    /// </summary>
    public static class XgOriginStage2
    {
        public struct Node { public double X, Y; }
        public static readonly Node[] In = { new Node { X = 210, Y = 380 }, new Node { X = 210, Y = 600 } };
        public static readonly Node[] Hidden = { new Node { X = 480, Y = 290 }, new Node { X = 480, Y = 410 }, new Node { X = 480, Y = 530 }, new Node { X = 480, Y = 650 } };
        public static readonly Node Out = new Node { X = 750, Y = 470 };

        public struct Edge { public Node A, B; public int K; }
        public static readonly Edge[] Edges = new Edge[12];
        public static readonly double[] W0 = new double[12], W1 = new double[12];

        public static readonly double[] PStart = { 8, 1, -4, 0, 0, -6, 6, 0, -3 };
        public static readonly double[] PEnd = { 11, 11, -5.5, 11, 11, -16.5, 13, -13, -6.5 };
        public static readonly double[][] Xor = { new[] { .2, .2, 0 }, new[] { .8, .2, 1 }, new[] { .2, .8, 1 }, new[] { .8, .8, 0 } };

        public const double HX = 1130, HY = 210, HS = 560;
        public const int Grid = 40;

        public sealed class Card { public string Title, TitleEn, Out, OutEn; public string[] Parts, PartsEn; }
        public static readonly Card[] Cards =
        {
            new Card { Title = "弹幕反话", TitleEn = "DANMAKU SARCASM", Parts = new[] { "前方高能", "辣眼睛" }, PartsEn = new[] { "whoa!", "my eyes" }, Out = "反话？", OutEn = "sarcasm?" },
            new Card { Title = "标题党", TitleEn = "CLICKBAIT", Parts = new[] { "震惊！", "竟然" }, PartsEn = new[] { "SHOCKING", "wow" }, Out = "标题党", OutEn = "clickbait" },
            new Card { Title = "逻辑组合题", TitleEn = "LOGIC COMBO", Parts = new[] { "下雨", "没带伞" }, PartsEn = new[] { "rain", "no coat" }, Out = "会淋湿", OutEn = "get wet" },
        };

        static XgOriginStage2()
        {
            int e = 0;
            for (int i = 0; i < 2; i++) for (int j = 0; j < 4; j++) Edges[e++] = new Edge { A = In[i], B = Hidden[j], K = i * 4 + j };
            for (int j = 0; j < 4; j++) Edges[e++] = new Edge { A = Hidden[j], B = Out, K = 8 + j };
            for (int i = 0; i < 12; i++)
            {
                string k = Edges[i].K.ToString(CultureInfo.InvariantCulture);
                W0[i] = (XgOriginColdOpen.Rand("w0" + k) - .5) * 1.6;
                W1[i] = (XgOriginColdOpen.Rand("w1" + k) - .5) * 3.2;
            }
        }

        public static double Mlp(double x, double y, double[] p)
        {
            double h1 = XgOriginColdOpen.Sigmoid(p[0] * x + p[1] * y + p[2]);
            double h2 = XgOriginColdOpen.Sigmoid(p[3] * x + p[4] * y + p[5]);
            return XgOriginColdOpen.Sigmoid(p[6] * h1 + p[7] * h2 + p[8]);
        }

        public sealed class Frame
        {
            public double Train, SnapFlash, NetA, Fw, Bw, HeatA, Epoch;
            public readonly double[] Params = new double[9];
            public int Correct;
            public double[] EdgeWeight = new double[12];
            public double TrainingTextA;
        }

        public static void Evaluate(double t, Frame f)
        {
            double creep = .1 * XgOriginColdOpen.Ramp(t, 6, 10.74) + .03 * Math.Sin(t * 2.3) * XgOriginColdOpen.Ramp(t, 6, 7) * (1 - XgOriginColdOpen.Ramp(t, 10.74, 10.84));
            double snap = XgOriginColdOpen.Ramp(t, 10.84, 11.19, XgOriginColdOpen.EaseInOut);
            f.Train = Math.Min(1, creep + snap * (1 - creep));
            f.SnapFlash = t >= 10.84 ? 1 - XgOriginColdOpen.Ramp(t, 10.84, 11.64) : 0;
            for (int i = 0; i < 9; i++) f.Params[i] = XgOriginColdOpen.Lerp(PStart[i], PEnd[i], f.Train);
            f.NetA = 1 - XgOriginColdOpen.Ramp(t, 12, 12.7);
            f.Fw = t > 5.2 && t < 10 ? ((t - 5.2) % 1.6) / 1.6 : -1;
            f.Bw = t > 6.6 && t < 11.4 ? ((t - 6.6) % 1.6) / 1.6 : -1;
            int c = 0;
            foreach (var p in Xor) if ((Mlp(p[0], p[1], f.Params) > .5) == (p[2] > 0)) c++;
            f.Correct = c;
            f.HeatA = XgOriginColdOpen.Ramp(t, 1.2, 2.4);
            f.Epoch = Math.Floor(XgOriginColdOpen.Lerp(1, 4000, XgOriginColdOpen.Ramp(t, 6, 10.74, XgOriginColdOpen.Linear)));
            f.TrainingTextA = XgOriginColdOpen.Ramp(t, 7, 7.6) * (1 - XgOriginColdOpen.Ramp(t, 10.64, 10.84));
            for (int i = 0; i < 12; i++)
                f.EdgeWeight[i] = XgOriginColdOpen.Lerp(W0[i], W1[i], f.Train) + (1 - f.Train) * .35 * Math.Sin(t * 3 + i);
        }
    }
}
