using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>A picture question on the sheet as the tiny conv net sees it (「它怎么看的」, 它在学 §2.3).</summary>
    public sealed class XgSheetLook
    {
        /// <summary>The 8×8 picture shown. Honest: <see cref="XgTinySample.digit"/> is the digit drawn.</summary>
        public XgTinySample sample;
        /// <summary>The digit drawn and the one asked about (「这是 asked 吗？」).</summary>
        public int digit, asked;
        /// <summary>The right answer, and what it answers (always the sheet's answer).</summary>
        public bool truth, said;
        /// <summary>Not a digit question (captcha, meme, Go): a digit picture stands in, with its own true label.</summary>
        public bool standIn;
        /// <summary>A sloppy picture: this share of a picture of <see cref="blendDigit"/> was mixed in (−1 none).</summary>
        public int blendDigit = -1;
        public double blend;
        public XgTinyConvTrace trace;
        /// <summary>The probability that it is <see cref="asked"/>, and the digit it finds most likely.</summary>
        public double pYes;
        public int top;
        /// <summary>The digit its answer took the picture for, and the one it weighed it against.</summary>
        public int seen, against;
        /// <summary>Per map: how far that map pushed toward <see cref="seen"/> over <see cref="against"/> (through the dense layer).</summary>
        public double[] push = new double[XgTinyConvNet.Kernels];
        /// <summary>The map that pushed hardest: the one that "looked wrong" when the answer is wrong.</summary>
        public int key;
        public bool Right => said == truth;
    }

    /// <summary>One real backward step of the microscope's MLP, for the moment the player corrects it (它在学 §2.4).</summary>
    public sealed class XgGradLesson
    {
        /// <summary>The net as it answered, and the same net after one step against the gradient.</summary>
        public XgTinyNet before, after;
        public XgTinySample sample;
        public XgTinyGrad grad;
        /// <summary>The step size used for <see cref="after"/> (enlarged so the change is visible).</summary>
        public double rate;
    }

    /// <summary>
    /// Maps sheet questions and corrections onto the tiny real nets. Sheet questions carry no pixels, so each picture
    /// question gets a generated 8×8 picture, chosen deterministically from the question's seed:
    /// <list type="bullet">
    /// <item>a digit question 「这是 k 吗？」 gets a hand-drawn picture of its own digit;</item>
    /// <item>captcha, meme and Go questions get a stand-in digit picture whose label is true for that picture (shown as a
    /// stand-in), with the same yes/no answer as the card;</item>
    /// <item>the sheet's ✓/× comes from mastery, not from this net, so the picture is picked to agree with it: the first
    /// of up to 24 drawings of the digit (seeded by the question) on which the conv net gives the sheet's answer. When
    /// none does (a wrong answer on an easy digit, usually), the picture is mixed step by step (10% at a time) with a
    /// clear drawing of the digit it would have to be confused with, until the net gives that answer; the view says how
    /// much was mixed in. Nothing is forced: every number shown is the net's real output on the picture shown.</item>
    /// </list>
    /// </summary>
    public static class XgTinyLook
    {
        const int Drawings = 24, Clear = 40;

        public static bool IsPicture(string dataset) => XgBoardData.Region(dataset) == "vision";

        /// <summary>The look for a sheet question; null when it is not a picture question or has no card.</summary>
        public static XgSheetLook For(XgSim sim, XgSheetItem item)
        {
            if (sim == null || item == null || !IsPicture(item.dataset)) return null;
            var b = sim.SheetCard(item);
            if (b == null) return null;
            // The sheet's own rule (XgSim.SheetAnswer): the right answer once known or while the label is dirty.
            bool said = item.right || sim.SheetDirty(item) ? b.truth : !b.truth;
            var src = b.source;
            bool standIn = item.dataset != "mnist" || src == null;
            int digit, asked;
            if (!standIn) { digit = Clamp(src.digit); asked = Clamp(src.asked); }
            else
            {
                var r = new XgTinyRandom(item.seed ^ 0x51EE7);
                digit = r.Below(10);
                asked = b.truth ? digit : (digit + 1 + r.Below(9)) % 10;
            }
            // A hand card's answer is its label; the look follows the picture's own truth (digit == asked).
            if (!standIn && (digit == asked) != b.truth) said = !said;
            return Look(XgTinyConvNet.Digits, item.seed, digit, asked, said, standIn);
        }

        static int Clamp(int d) => Math.Max(0, Math.Min(9, d));

        /// <summary>A picture of <paramref name="digit"/> on which <paramref name="net"/> answers 「是 asked 吗」 with <paramref name="said"/>.</summary>
        public static XgSheetLook Look(XgTinyConvNet net, int seed, int digit, int asked, bool said, bool standIn = false)
        {
            digit = Clamp(digit); asked = Clamp(asked);
            var look = new XgSheetLook { digit = digit, asked = asked, truth = digit == asked, said = said, standIn = standIn };
            int start = (int)(new XgTinyRandom(seed).Next() & 0x3fffffff);
            XgTinySample first = null;
            for (int t = 0; t < Drawings; t++)
            {
                var s = XgTinyData.Digit(digit, start + t);
                first = first ?? s;
                if (net.P(s.pixels, asked) > .5 == said) return Finish(net, look, s);
            }
            // No drawing gives that answer: mix in a clear picture of what it would have to see.
            var p = net.Trace(first.pixels).probs;
            int toward = said ? asked : digit != asked ? digit : Best(p, asked);
            XgTinySample clear = null;
            for (int t = 0; t < Clear && clear == null; t++)
            {
                var c = XgTinyData.Digit(toward, start + 7919 + t);
                if (net.P(c.pixels, toward) > .5) clear = c;
            }
            clear = clear ?? XgTinyData.Digit(toward, start + 7919);
            XgTinySample mixed = null;
            for (int k = 1; k <= 10; k++)
            {
                double a = k / 10.0;
                mixed = new XgTinySample { digit = digit, label = digit == asked };
                for (int i = 0; i < XgTinyData.Pixels; i++) mixed.pixels[i] = (1 - a) * first.pixels[i] + a * clear.pixels[i];
                look.blendDigit = toward; look.blend = a;
                if (net.P(mixed.pixels, asked) > .5 == said) break;
            }
            return Finish(net, look, mixed);
        }

        /// <summary>The likeliest digit other than <paramref name="not"/>.</summary>
        static int Best(double[] p, int not)
        {
            int best = -1;
            for (int o = 0; o < p.Length; o++) if (o != not && (best < 0 || p[o] > p[best])) best = o;
            return best;
        }

        static XgSheetLook Finish(XgTinyConvNet net, XgSheetLook look, XgTinySample s)
        {
            s.digit = look.digit; s.label = look.truth;
            look.sample = s;
            var t = look.trace = net.Trace(s.pixels);
            look.pYes = t.probs[look.asked];
            look.top = Best(t.probs, -1);
            // Yes: it saw the asked digit, against the runner-up (or the real one). No: it saw something else, against the asked one.
            look.seen = look.said ? look.asked : Best(t.probs, look.asked);
            look.against = !look.said ? look.asked : look.digit != look.asked ? look.digit : Best(t.probs, look.asked);
            int F = XgTinyConvNet.Features, cells = XgTinyConvNet.PoolCells;
            look.key = 0;
            for (int m = 0; m < XgTinyConvNet.Kernels; m++)
            {
                double sum = 0;
                for (int i = 0; i < cells; i++)
                {
                    int f = m * cells + i;
                    sum += t.pooled[f] * (net.dense[look.seen * F + f] - net.dense[look.against * F + f]);
                }
                look.push[m] = sum;
                if (sum > look.push[look.key]) look.key = m;
            }
            return look;
        }

        static XgTinyNet lessonNet;
        /// <summary>The microscope's net (64 → 4 → 1, 「这是 0 吗？」) part-way through learning: it has a shape and still errs.</summary>
        public static XgTinyNet LessonNet
        {
            get
            {
                if (lessonNet != null) return lessonNet;
                var run = new XgTinyRun(XgTinyLesson.Microscope);
                run.Step(15);
                return lessonNet = run.net;
            }
        }

        /// <summary>
        /// The correction as one real backward step (它在学 §2.4). The player's card becomes a picture for 「这是 0 吗？」
        /// with the card's answer (yes: a 0; no: another digit, from the seed), one the net gets wrong as the AI did: the
        /// first wrong one of up to 24 drawings, else one mixed toward a clear picture of the other answer until it errs.
        /// The gradient is real; the step that makes <see cref="XgGradLesson.after"/> follows it with a rate enlarged so
        /// the widest move is about a sixth of the largest weight, so the links visibly shift.
        /// </summary>
        public static XgGradLesson Correction(int seed, bool truth)
        {
            var net = LessonNet.Clone();
            var r = new XgTinyRandom(seed ^ 0x6A2D);
            int digit = truth ? 0 : 1 + r.Below(9), start = (int)(r.Next() & 0x3fffffff);
            XgTinySample sample = null, first = null;
            for (int t = 0; t < Drawings && sample == null; t++)
            {
                var s = XgTinyData.Digit(digit, start + t);
                first = first ?? s;
                if (net.Predict(s.pixels) != truth) sample = s;
            }
            if (sample == null)
            {
                int other = truth ? 1 + r.Below(9) : 0;
                XgTinySample clear = null;
                for (int t = 0; t < Clear && clear == null; t++) { var c = XgTinyData.Digit(other, start + 7919 + t); if (net.Predict(c.pixels) != truth) clear = c; }
                clear = clear ?? XgTinyData.Digit(other, start + 7919);
                for (int k = 1; k <= 10; k++)
                {
                    double a = k / 10.0;
                    sample = new XgTinySample { digit = digit, label = truth };
                    for (int i = 0; i < XgTinyData.Pixels; i++) sample.pixels[i] = (1 - a) * first.pixels[i] + a * clear.pixels[i];
                    if (net.Predict(sample.pixels) != truth) break;
                }
            }
            sample.label = truth;
            var g = net.Gradients(sample);
            double maxW = 1e-9, maxG = 1e-12;
            for (int i = 0; i < net.w1.Length; i++) { maxW = Math.Max(maxW, Math.Abs(net.w1[i])); maxG = Math.Max(maxG, Math.Abs(g.w1[i])); }
            for (int i = 0; i < net.w2.Length; i++) { maxW = Math.Max(maxW, Math.Abs(net.w2[i])); maxG = Math.Max(maxG, Math.Abs(g.w2[i])); }
            double rate = Math.Max(.05, Math.Min(50, maxW / 6 / maxG));
            var after = net.Clone();
            after.Step(g, rate);
            return new XgGradLesson { before = net, after = after, sample = sample, grad = g, rate = rate };
        }
    }
}
