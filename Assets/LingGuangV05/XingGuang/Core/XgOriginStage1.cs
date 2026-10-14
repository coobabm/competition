using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Stage 1, the perceptron (22.5–40 s), as pure functions of scene-local time: the toy SMS data set, a real perceptron run recorded once
    /// (the line swings through its actual updates), the XOR tries and every ramp the picture needs. A port of Remotion's Stage1.tsx.
    /// </summary>
    public static class XgOriginStage1
    {
        public struct Pt { public double X, Y; public bool Spam; }
        public struct State { public double W0, W1, B; public int Idx; }

        public static readonly Pt[] Points;
        public static readonly State[] States;

        public static readonly double[][] Xor =
        {
            new[] { .22, .22, 0 }, new[] { .78, .22, 1 }, new[] { .22, .78, 1 }, new[] { .78, .78, 0 },
        };
        public static readonly string[] XorLabels = { "A✗ B✗", "A✓ B✗", "A✗ B✓", "A✓ B✓" };
        public static readonly double[][] Tries =
        {
            new[] { 1, 0, -.5 }, new[] { 1, 1, -1.0 }, new[] { 0, 1, -.5 }, new[] { 1, -1, .05 }, new[] { 1, .6, -.45 }, new[] { 1, 1, -.55 },
        };


        static XgOriginStage1()
        {
            var pts = new List<Pt>();
            int i = 0;
            while (pts.Count < 46)
            {
                string n = i.ToString(CultureInfo.InvariantCulture);
                double x = .06 + XgOriginColdOpen.Rand("sx" + n) * .88;
                double y = .06 + XgOriginColdOpen.Rand("sy" + n) * .88;
                i++;
                double d = x + y - 1.0;
                if (Math.Abs(d) < .13) continue;
                pts.Add(new Pt { X = x, Y = y, Spam = d > 0 });
            }
            Points = pts.ToArray();

            double w0 = .9, w1 = -.7, b = .1;
            var rec = new List<State> { new State { W0 = w0, W1 = w1, B = b, Idx = -1 } };
            for (int ep = 0; ep < 200; ep++)
            {
                int err = 0;
                for (int k = 0; k < Points.Length; k++)
                {
                    var p = Points[k];
                    double y = p.Spam ? 1 : -1;
                    if (y * (w0 * p.X + w1 * p.Y + b) <= 0)
                    {
                        w0 += .3 * y * p.X; w1 += .3 * y * p.Y; b += .3 * y;
                        rec.Add(new State { W0 = w0, W1 = w1, B = b, Idx = k });
                        err++;
                    }
                }
                if (err == 0) break;
            }
            if (rec.Count <= 12) States = rec.ToArray();
            else
            {
                var pick = new State[12];
                for (int j = 0; j < 12; j++) pick[j] = rec[(int)Math.Floor(j / 11.0 * (rec.Count - 1) + .5)];
                States = pick;
            }
        }

        public enum Out { Yes, No, Wrong }

        /// <summary>The line geometry of Stage1's lineGeom: two far points of the decision line and the half-plane quad (unit plot space, y up).</summary>
        public struct Line { public double Ax, Ay, Bx, By; public double[] Half; }

        public static Line LineGeom(double w0, double w1, double b)
        {
            double nn = Math.Sqrt(w0 * w0 + w1 * w1); if (nn == 0) nn = 1e-6;
            double n0 = w0 / nn, n1 = w1 / nn;
            double p0x = -b * w0 / (nn * nn), p0y = -b * w1 / (nn * nn);
            double d0 = -n1, d1 = n0;
            double ax = p0x - 3 * d0, ay = p0y - 3 * d1, bx = p0x + 3 * d0, by = p0y + 3 * d1;
            return new Line { Ax = ax, Ay = ay, Bx = bx, By = by, Half = new[] { ax, ay, bx, by, bx + 3 * n0, by + 3 * n1, ax + 3 * n0, ay + 3 * n1 } };
        }

        public sealed class Frame
        {
            public double W0, W1, B, XW0, XW1, XB;
            public int FlashIdx; public double FlashA;
            public int Correct, Fixes;
            public double ScatterA, XorA, LineA, Stamp, DiagramA, Pulse, PlotA;
            public bool XorPhase;
            public readonly bool[] Wrong = new bool[4];
            public Out Output;
            public Line Line;
            public bool PointOk(int i) { var p = Points[i]; return (W0 * p.X + W1 * p.Y + B > 0) == p.Spam; }
        }

        public static void Evaluate(double t, Frame f)
        {
            int n = States.Length;
            double u = XgOriginColdOpen.Ramp(t, 3.4, 8.84, XgOriginColdOpen.Linear) * (n - 1);
            int k = Math.Min(n - 2, (int)Math.Floor(u));
            double fr = XgOriginColdOpen.EaseInOut(Math.Min(1, u - k));
            var s0 = States[k]; var s1 = States[k + 1];
            f.W0 = XgOriginColdOpen.Lerp(s0.W0, s1.W0, fr); f.W1 = XgOriginColdOpen.Lerp(s0.W1, s1.W1, fr); f.B = XgOriginColdOpen.Lerp(s0.B, s1.B, fr);
            f.FlashIdx = u < n - 1 ? s1.Idx : -1;
            f.FlashA = 1 - (u - Math.Floor(u));
            int correct = 0;
            foreach (var p in Points) if ((f.W0 * p.X + f.W1 * p.Y + f.B > 0) == p.Spam) correct++;
            f.Correct = correct;
            f.Fixes = Math.Min(n - 1, (int)Math.Floor(u) + 1);

            f.ScatterA = 1 - XgOriginColdOpen.Ramp(t, 9.0, 9.8);
            f.XorA = XgOriginColdOpen.Ramp(t, 9.6, 10.4);
            f.PlotA = XgOriginColdOpen.Ramp(t, .8, 1.8);

            int m = Tries.Length;
            double v = XgOriginColdOpen.Ramp(t, 10.2, 14.6, XgOriginColdOpen.Linear) * (m - 1);
            int j = Math.Min(m - 2, (int)Math.Floor(v));
            double g = XgOriginColdOpen.EaseInOut(Math.Min(1, v - j));
            f.XW0 = XgOriginColdOpen.Lerp(Tries[j][0], Tries[j + 1][0], g); f.XW1 = XgOriginColdOpen.Lerp(Tries[j][1], Tries[j + 1][1], g); f.XB = XgOriginColdOpen.Lerp(Tries[j][2], Tries[j + 1][2], g);
            int wrongCount = 0;
            for (int i = 0; i < 4; i++)
            {
                f.Wrong[i] = (f.XW0 * Xor[i][0] + f.XW1 * Xor[i][1] + f.XB > 0) != (Xor[i][2] > 0);
                if (f.Wrong[i]) wrongCount++;
            }
            if (wrongCount > 2) for (int i = 0; i < 4; i++) f.Wrong[i] = !f.Wrong[i];

            f.XorPhase = t >= 9.8;
            f.Line = f.XorPhase ? LineGeom(f.XW0, f.XW1, f.XB) : LineGeom(f.W0, f.W1, f.B);
            f.LineA = XgOriginColdOpen.Ramp(t, 2.6, 3.4);
            f.Stamp = XgOriginColdOpen.Ramp(t, 13.0, 13.5);
            f.DiagramA = XgOriginColdOpen.Ramp(t, .5, 1.6);
            f.Pulse = (t * .8) % 1;
            bool anyWrong = false; foreach (bool w in f.Wrong) anyWrong |= w;
            f.Output = !f.XorPhase ? (((long)Math.Floor(t * 1.25)) % 2 == 0 ? Out.Yes : Out.No) : anyWrong ? Out.Wrong : Out.Yes;
        }
    }
}
