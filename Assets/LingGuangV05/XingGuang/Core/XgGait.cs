using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// A gait for the eight-legged crawler (prototype, 2026-10-15): each leg's phase in the step cycle, the share of the
    /// cycle a foot stays on the page (duty), the stride and the step frequency. This is what the learner changes.
    /// </summary>
    public sealed class XgGaitGenes
    {
        public const int Legs = 8;
        public const double DutyMin = .2, DutyMax = .95, StrideMin = 6, FreqMin = .3, FreqMax = 4;
        /// <summary>The longest stride: what the shortest-reaching leg can sweep planted without slipping (about 42).</summary>
        public static readonly double StrideMax = XgGaitWalk.StrideReach;

        /// <summary>Each leg's phase, 0–1, in the walker's leg order: L1, R1, L2, R2, L3, R3, L4, R4.</summary>
        public readonly double[] phase = new double[Legs];
        public double duty = .6, stride = 14, freq = .8;

        public XgGaitGenes Clone()
        {
            var g = new XgGaitGenes { duty = duty, stride = stride, freq = freq };
            Array.Copy(phase, g.phase, Legs);
            return g;
        }

        public void Clamp()
        {
            for (int i = 0; i < Legs; i++) phase[i] = Frac(phase[i]);
            duty = Math.Max(DutyMin, Math.Min(DutyMax, duty));
            stride = Math.Max(StrideMin, Math.Min(StrideMax, stride));
            freq = Math.Max(FreqMin, Math.Min(FreqMax, freq));
        }

        /// <summary>A first, uncoordinated gait: random phases, short slow steps.</summary>
        public static XgGaitGenes Clumsy(int seed)
        {
            var r = new XgTinyRandom(seed);
            var g = new XgGaitGenes();
            for (int i = 0; i < Legs; i++) g.phase[i] = r.NextDouble();
            return g;
        }

        internal static double Frac(double x) => x - Math.Floor(x);
    }

    /// <summary>One leg of <see cref="XgGaitWalk"/> at this moment, in the body frame (x forward, y to the left).</summary>
    public struct XgGaitLeg
    {
        public double hipX, hipY, footX, footY;
        /// <summary>On the page (pushing) rather than swinging forward.</summary>
        public bool stance;
        /// <summary>Planted but stretched or cramped past what the leg can hold: it slips and neither pushes nor supports.</summary>
        public bool slipping;
        /// <summary>Due down but held over the page: its planted neighbour on the same side is in the way (<see cref="XgGaitWalk.FootGap"/>).</summary>
        public bool hovering;
        /// <summary>How far through its swing the foot is (0–1; meaningless while planted).</summary>
        public double swing;
        /// <summary>0 on the page, 1 at the top of a swing (worked out when asked: the sim itself does not need it).</summary>
        public double lift => stance ? 0 : hovering ? XgGaitWalk.HoverLift : Math.Sin(swing * Math.PI);
    }

    /// <summary>
    /// The crawler walking one gait, as a small top-down simulation with no physics engine. A planted foot pushes the
    /// body forward; a swinging foot comes back to the front. The body only moves while the planted feet that hold
    /// (at least three, not stretched past the leg's reach) surround its centre of mass; otherwise it tips over,
    /// loses a moment getting up, and the fall is counted. On top of that (tuned 2026-10-15 so the learned gait looks
    /// like a spider's): a slipping planted foot drags; each side needs <see cref="SidePush"/> feet pushing for full
    /// speed, and the stronger side turns the body (a waddle, <see cref="yaw"/>); and two neighbouring legs in the air
    /// together (same side, next pair; or a left–right pair) leave that corner unheld, so it sags and drags
    /// (<see cref="Clash"/>). Legs on one side cannot cross: a foot will not come down on or past its planted neighbour
    /// (<see cref="FootGap"/>) and hovers until the way is clear (2026-10-15, after play-testing). None of these name a gait; together they favour neighbours that take turns, which is
    /// what the alternating tetrapod and the wave gaits are. Same genes, same walk: there is no randomness in here.
    /// </summary>
    public sealed class XgGaitWalk
    {
        /// <summary>The walker's own leg geometry at size 1 (XgSpiderWalker): hips and rest spots, and its two segments.</summary>
        static readonly double[] HipX = { 7, 2.5, -2.5, -7 }, RestX = { 36, 16, -8, -28 }, RestY = { 30, 40, 40, 32 };
        public const double HipY = 4.5, Upper = 30, Lower = 36;
        /// <summary>A planted leg holds while the foot is within these shares of its full length from the hip.</summary>
        public const double ReachMax = .85, ReachMin = .3;
        const double FarReach2 = ReachMax * (Upper + Lower) * ReachMax * (Upper + Lower), NearReach2 = ReachMin * (Upper + Lower) * ReachMin * (Upper + Lower);
        /// <summary>Fastest a swinging foot moves (units a second); a faster swing lands late and pushes less.</summary>
        public const double SwingSpeedMax = 240;
        /// <summary>The centre of mass sits a little behind the hips (the abdomen), and needs this margin inside the support.</summary>
        public const double MassX = -4, Margin = 3;
        /// <summary>Seconds lost getting up after a fall.</summary>
        public const double GetUp = .25;
        /// <summary>How hard uneven pushing turns the body (radians a second at full imbalance and 100 units a second), and how fast it straightens (per second).</summary>
        public const double YawGain = .8, Straighten = 3;
        /// <summary>Feet a side needs pushing to carry its half of the body at full speed.</summary>
        public const double SidePush = 2;
        /// <summary>How much each pair of neighbouring legs up together slows the body (speed ÷ (1 + Tangle × clashes)).</summary>
        public const double Tangle = 1.5;
        /// <summary>Closest two planted feet on the same side may stand, the front one ahead (units); a foot will not come down closer.</summary>
        public const double FootGap = 3, HoverLift = .15;

        public readonly XgGaitGenes genes;
        public readonly XgGaitLeg[] legs = new XgGaitLeg[XgGaitGenes.Legs];
        public double time, distance;
        public int falls;
        /// <summary>Above 0 while it is down after a fall (seconds left).</summary>
        public double down;
        /// <summary>The body is held by its planted feet this moment.</summary>
        public bool stable = true;
        public double speed;
        /// <summary>The body's turn off the straight line (radians, + to the left), and its running total of |yaw| × seconds.</summary>
        public double yaw, wobble;
        /// <summary>Legs lifted so far (each lift costs effort).</summary>
        public int lifts;
        /// <summary>How far the centre of mass sits inside the holding feet this moment (negative outside), and its running total × seconds.</summary>
        public double margin, marginSum;
        /// <summary>Planted feet, and those of them that hold, this moment.</summary>
        public int planted, holding;
        /// <summary>Feet held up by a neighbour this moment, and the foot-seconds spent so.</summary>
        public int hovering;
        public double hoverTime;
        /// <summary>How far the held feet overlap their neighbours this moment (in <see cref="FootGap"/>s, summed), and its running total × seconds.</summary>
        public double hoverDepth, hoverDepthTime;

        public XgGaitWalk(XgGaitGenes genes)
        {
            this.genes = genes;
            for (int i = 0; i < legs.Length; i++)
            {
                int pair = i / 2; double side = i % 2 == 0 ? 1 : -1;
                legs[i].hipX = HipX[pair]; legs[i].hipY = HipY * side;
            }
            Pose();
            margin = Support();
            stable = margin >= Margin;
        }

        /// <summary>The longest stride every leg can sweep planted without stretching past <see cref="ReachMax"/> (about 42).</summary>
        public static readonly double StrideReach = ComputeStrideReach();

        static double ComputeStrideReach()
        {
            double least = double.MaxValue, far = ReachMax * (Upper + Lower);
            for (int pair = 0; pair < 4; pair++)
            {
                double dy = RestY[pair] - HipY, dx = Math.Sqrt(Math.Max(0, far * far - dy * dy));
                least = Math.Min(least, 2 * (dx - Math.Abs(RestX[pair] - HipX[pair])));
            }
            return least;
        }

        public static double RestXOf(int leg) => RestX[leg / 2];
        public static double RestYOf(int leg) => RestY[leg / 2] * (leg % 2 == 0 ? 1 : -1);
        public static double HipXOf(int leg) => HipX[leg / 2];
        public static double HipYOf(int leg) => HipY * (leg % 2 == 0 ? 1 : -1);

        /// <summary>Where the stance pushes the body: the feet sweep back at stride × freq ÷ duty.</summary>
        public double PushSpeed => genes.stride * genes.freq / genes.duty;

        /// <summary>The share of the push that is kept: a swing faster than a leg can move lands late.</summary>
        public double SwingEfficiency
        {
            get
            {
                double swing = genes.stride * genes.freq / Math.Max(.01, 1 - genes.duty);
                return swing <= SwingSpeedMax ? 1 : SwingSpeedMax / swing;
            }
        }

        public void Step(double dt)
        {
            time += dt;
            for (int i = 0; i < legs.Length; i++) wasStance[i] = legs[i].stance;
            Pose();
            for (int i = 0; i < legs.Length; i++) if (wasStance[i] && !legs[i].stance) lifts++;
            if (top < 0) top = PushSpeed * SwingEfficiency;
            margin = Support();
            bool holds = margin >= Margin;
            if (down > 0)
            {
                down -= dt;
                speed = 0;
            }
            else if (!holds)
            {
                falls++;
                down = GetUp;
                speed = 0;
            }
            else
            {
                // A slipping planted foot drags: only the share of planted feet that hold keeps the push. Each side
                // needs SidePush feet pushing to carry its half of the body at full speed; a side with fewer slows
                // (a stutter), and the stronger side turns the body toward the weaker one (a waddle).
                int left = 0, right = 0;
                for (int i = 0; i < legs.Length; i++)
                {
                    if (!legs[i].stance || legs[i].slipping) continue;
                    if (i % 2 == 0) left++; else right++;
                }
                double pl = Math.Min(1, left / SidePush), pr = Math.Min(1, right / SidePush);
                speed = top * (left + right) / Math.Max(1, planted) * (pl + pr) / 2 / (1 + Tangle * Clash());
                yaw += YawGain * (pr - pl) * speed / 100 * dt;
                distance += speed * Math.Cos(yaw) * dt;
            }
            yaw -= yaw * Math.Min(1, Straighten * dt);
            wobble += Math.Abs(yaw) * dt;
            hoverTime += hovering * dt;
            hoverDepthTime += hoverDepth * dt;
            marginSum += Math.Max(-20, margin) * dt;
            stable = holds && down <= 0;
        }

        readonly bool[] wasStance = new bool[XgGaitGenes.Legs];
        double top = -1;

        /// <summary>
        /// Pairs of neighbouring legs in the air together this moment (a leg and the next one on its side, or a left leg
        /// and its right partner). Two neighbours up at once leave that corner of the body unheld, so it sags and drags.
        /// </summary>
        public int Clash()
        {
            int sum = 0;
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i].stance) continue;
                if (i + 2 < legs.Length && !legs[i + 2].stance) sum++;
                if (i % 2 == 0 && !legs[i + 1].stance) sum++;
            }
            return sum;
        }

        /// <summary>Each leg's place in its cycle: planted and sweeping back, or lifted and swinging forward.</summary>
        void Pose()
        {
            double s = genes.stride, b = genes.duty, cycle = genes.freq * time;
            var phase = genes.phase;
            for (int i = 0; i < legs.Length; i++)
            {
                double p = cycle + phase[i];
                p -= Math.Floor(p);
                ref var l = ref legs[i];
                double rest = RestX[i >> 1];
                l.footY = RestY[i >> 1] * ((i & 1) == 0 ? 1 : -1);
                if (p < b)
                {
                    double k = p / b;
                    l.stance = true;
                    l.footX = rest + s / 2 - s * k;
                }
                else
                {
                    double k = (p - b) / (1 - b), e = k * k * (3 - 2 * k);
                    l.stance = false; l.swing = k;
                    l.footX = rest - s / 2 + s * e;
                }
                if (l.stance)
                {
                    double dx = l.footX - l.hipX, dy = l.footY - l.hipY, reach2 = dx * dx + dy * dy;
                    l.slipping = reach2 > FarReach2 || reach2 < NearReach2;
                }
                else l.slipping = false;
                l.hovering = false;
            }
            // A foot cannot come down on or past its planted neighbour on the same side (closer than FootGap, front foot
            // ahead): it hovers over the page and tries again next moment. Planted feet sweep back together, so two
            // feet that are both down never draw closer; a swinging foot passes over a planted one freely.
            hovering = 0; hoverDepth = 0;
            for (int i = 0; i < legs.Length; i++)
            {
                ref var l = ref legs[i];
                if (!l.stance || wasStance[i]) continue;
                double depth = 0;
                for (int j = i - 2; j <= i + 2; j += 4)
                {
                    if (j < 0 || j >= legs.Length || !legs[j].stance || (!wasStance[j] && j > i)) continue;
                    double gap = j < i ? legs[j].footX - l.footX : l.footX - legs[j].footX;
                    if (gap < FootGap) depth = Math.Max(depth, (FootGap - gap) / FootGap);
                }
                if (depth <= 0) continue;
                l.stance = false; l.slipping = false; l.hovering = true;
                hovering++;
                hoverDepth += depth;
            }
        }

        // Scratch for the support hull (at most eight feet; no allocation per step).
        readonly double[] sx = new double[XgGaitGenes.Legs], sy = new double[XgGaitGenes.Legs];
        readonly double[] hx = new double[XgGaitGenes.Legs * 2], hy = new double[XgGaitGenes.Legs * 2];

        // The hull's edges as last built: each edge's distance to the mass then, and how that distance changes as the
        // feet slide back together. While the same feet stay planted the hull only slides, so it is not rebuilt.
        readonly double[] edgeDist = new double[XgGaitGenes.Legs * 2], edgeSlide = new double[XgGaitGenes.Legs * 2];
        int edgeCount, hullMask = -1, hullRef;
        double hullRefX;

        /// <summary>How far the centre of mass sits inside the hull of the planted feet that hold (−20 when fewer than three hold).</summary>
        double Support()
        {
            int mask = 0, n = 0, first = -1;
            planted = 0;
            for (int i = 0; i < legs.Length; i++)
            {
                if (!legs[i].stance) continue;
                planted++;
                if (legs[i].slipping) continue;
                mask |= 1 << i; n++;
                if (first < 0) first = i;
            }
            holding = n;
            if (n < 3) { hullMask = -1; return -20; }
            if (mask != hullMask) BuildHull(mask, first);
            if (edgeCount < 3) return -20;
            double slide = legs[hullRef].footX - hullRefX, least = double.MaxValue;
            for (int e = 0; e < edgeCount; e++)
            {
                double d = edgeDist[e] + edgeSlide[e] * slide;
                if (d < least) least = d;
            }
            return least;
        }

        void BuildHull(int mask, int first)
        {
            hullMask = mask; hullRef = first; hullRefX = legs[first].footX; edgeCount = 0;
            int n = 0;
            for (int i = 0; i < legs.Length; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                var l = legs[i];
                // Insertion sort by x, then y, as the feet come in.
                int k = n++;
                while (k > 0 && (sx[k - 1] > l.footX || (sx[k - 1] == l.footX && sy[k - 1] > l.footY))) { sx[k] = sx[k - 1]; sy[k] = sy[k - 1]; k--; }
                sx[k] = l.footX; sy[k] = l.footY;
            }
            int h = Hull(n);
            if (h < 3) return;
            // Counter-clockwise hull: the distance of the mass to the left of each edge. Sliding every foot by dx moves
            // that distance by ey ÷ len × dx.
            for (int i = 0; i < h; i++)
            {
                int j = i + 1 == h ? 0 : i + 1;
                double ex = hx[j] - hx[i], ey = hy[j] - hy[i], len = Math.Sqrt(ex * ex + ey * ey);
                if (len < 1e-9) continue;
                edgeDist[edgeCount] = (ex * (0 - hy[i]) - ey * (MassX - hx[i])) / len;
                edgeSlide[edgeCount] = ey / len;
                edgeCount++;
            }
        }

        /// <summary>Andrew's monotone chain over the sorted feet, counter-clockwise; returns the hull's corner count.</summary>
        int Hull(int n)
        {
            int h = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                int start = h;
                for (int k = 0; k < n; k++)
                {
                    int q = pass == 0 ? k : n - 1 - k;
                    while (h >= start + 2 && (hx[h - 1] - hx[h - 2]) * (sy[q] - hy[h - 2]) - (hy[h - 1] - hy[h - 2]) * (sx[q] - hx[h - 2]) <= 0) h--;
                    hx[h] = sx[q]; hy[h] = sy[q]; h++;
                }
                h--;
            }
            return h;
        }
    }

    /// <summary>What one trial walk of a gait scored.</summary>
    public struct XgGaitScore
    {
        public double distance, fitness;
        public int falls;
        /// <summary>|yaw| × seconds (the waddle), the mean margin of the centre of mass inside its support, and foot-seconds held up by a neighbour; legs lifted.</summary>
        public double wobble, margin, hover;
        public int lifts;
    }

    /// <summary>
    /// Learns to walk by trial: an evolution strategy over <see cref="XgGaitGenes"/>, run as <see cref="Lines"/> small
    /// (1+λ) lines side by side. Each generation every line tries its share of <see cref="Children"/> mutated copies
    /// of its parent for <see cref="TrialSeconds"/> and keeps the best child if it scores better (distance, less
    /// <see cref="FallCost"/> a fall, <see cref="WobbleCost"/> for the waddle, <see cref="LiftCost"/> a step and
    /// <see cref="HoverCost"/> for a foot held up by its neighbour). A line widens its mutations after a success and
    /// narrows them after a miss; a line that is not the leader and has stopped improving starts over from fresh random
    /// phases. Mutations mostly move phases relative to neighbours (one side behind a pair, a pair or the back of the
    /// body, one whole side) rather than one leg at a time. <see cref="Best"/> is the best gait any line has walked, so
    /// it never gets worse. Deterministic from its seed; one generation takes about a millisecond, so it runs live
    /// (2026-10-15: the lines, relative mutations and restarts lifted clean gaits from ~83% to ~97% of seeds).
    /// </summary>
    public sealed class XgGaitLearner
    {
        /// <summary>Children tried per generation, shared out among the <see cref="Lines"/>.</summary>
        public const int Children = 40, Lines = 4;
        /// <summary>Generations without a new best before a trailing line starts over (sooner while the best still falls).</summary>
        public const int Stall = 10, StallFalling = 5;
        public const double TrialSeconds = 5, FallCost = 20;
        public const double Dt = .02;
        /// <summary>Fitness lost per radian-second of waddle and per leg lifted (effort).</summary>
        public const double WobbleCost = 1500, LiftCost = 1;
        /// <summary>Fitness lost per second a foot is held up by its neighbour, per <see cref="XgGaitWalk.FootGap"/> of overlap.</summary>
        public const double HoverCost = 300;
        /// <summary>The chance each gene is changed in a child (the others are copied as they are).</summary>
        public const double GeneChance = .2;
        /// <summary>Base mutation sizes: phase, duty, stride, frequency (scaled by each line's step size).</summary>
        public const double PhaseSigma = .18, DutySigma = .06, StrideSigma = 4, FreqSigma = .25;
        /// <summary>
        /// Chances of a child's phase mutation being relative: one side from a pair backwards, both sides from a pair
        /// backwards (or that pair alone), or one whole side. Otherwise each leg's phase may move on its own.
        /// </summary>
        public const double SideBackChance = .2, BodyBackChance = .2, WholeSideChance = .1;
        /// <summary>Step size after a success and a miss (the 1/5th-rule idea), and its floor.</summary>
        public const double StepUp = 1.2, StepDown = .85, StepFloor = .15;

        readonly XgTinyRandom rng;
        public XgGaitGenes Best { get; private set; }
        public XgGaitScore BestScore { get; private set; }
        public int Generation { get; private set; }
        /// <summary>The leading line's mutation step size (1 at the start).</summary>
        public double StepScale { get; private set; } = 1;
        /// <summary>The best score after each generation (index 0 is the clumsy start).</summary>
        public readonly List<XgGaitScore> History = new List<XgGaitScore>();
        /// <summary>Whether the last generation found a better gait.</summary>
        public bool Improved { get; private set; }

        public XgGaitLearner(int seed)
        {
            rng = new XgTinyRandom(seed * 7919 + 17);
            Best = XgGaitGenes.Clumsy(seed);
            BestScore = Score(Best);
            History.Add(BestScore);
        }

        public static XgGaitScore Score(XgGaitGenes g) => Score(g, double.NegativeInfinity, out _);

        /// <summary>
        /// Walks a trial. Stops early (<paramref name="complete"/> false) once even full speed for the rest of the trial
        /// could not beat <paramref name="toBeat"/>: every cost only grows, so such a gait can never win.
        /// </summary>
        public static XgGaitScore Score(XgGaitGenes g, double toBeat, out bool complete)
        {
            var w = new XgGaitWalk(g);
            int steps = TrialSteps;
            double top = w.PushSpeed * w.SwingEfficiency;
            complete = true;
            for (int i = 0; i < steps; i++)
            {
                w.Step(Dt);
                if ((i & 7) == 7 && Fitness(w) + top * (steps - 1 - i) * Dt <= toBeat + 1e-9) { complete = false; break; }
            }
            double margin = w.marginSum / Math.Max(1e-9, w.time);
            return new XgGaitScore
            {
                distance = w.distance, falls = w.falls, wobble = w.wobble, lifts = w.lifts, margin = margin,
                fitness = Fitness(w), hover = w.hoverTime,
            };
        }

        static readonly int TrialSteps = (int)Math.Round(TrialSeconds / Dt);

        static double Fitness(XgGaitWalk w) => w.distance - FallCost * w.falls - WobbleCost * w.wobble - LiftCost * w.lifts - HoverCost * w.hoverDepthTime;

        /// <summary>One line of descent: its parent, the parent's score, its step size and how long it has been stuck.</summary>
        sealed class Line { public XgGaitGenes parent; public XgGaitScore score; public double step = 1; public int stalled; }
        Line[] lines;
        double step;

        /// <summary>One generation: every line tries its children and keeps its best if it beats its parent.</summary>
        public void Generate()
        {
            if (lines == null)
            {
                // The first line goes on from the clumsy start; the others start from fresh random phases.
                lines = new Line[Lines];
                for (int i = 0; i < Lines; i++)
                {
                    lines[i] = new Line { parent = Best, score = BestScore };
                    if (i > 0) Restart(lines[i]);
                }
            }
            Improved = false;
            Line lead = lines[0];
            foreach (var line in lines)
            {
                XgGaitGenes winner = null; var winnerScore = line.score;
                step = line.step;
                for (int c = 0; c < Children / Lines; c++)
                {
                    var child = Mutate(line.parent);
                    var s = Score(child, winnerScore.fitness, out bool complete);
                    if (complete && s.fitness > winnerScore.fitness + 1e-9) { winner = child; winnerScore = s; }
                }
                if (winner != null)
                {
                    line.parent = winner; line.score = winnerScore; line.stalled = 0;
                    line.step = Math.Min(2, line.step * StepUp);
                    if (winnerScore.fitness > BestScore.fitness + 1e-9) { Best = winner; BestScore = winnerScore; Improved = true; }
                }
                else
                {
                    line.step = Math.Max(StepFloor, line.step * StepDown);
                    if (++line.stalled >= (line.score.falls > 0 ? StallFalling : Stall) && line.score.fitness < BestScore.fitness) Restart(line);
                }
                if (line.score.fitness > lead.score.fitness) lead = line;
            }
            StepScale = lead.step;
            Generation++;
            History.Add(BestScore);
        }

        /// <summary>A stuck line starts over from new random phases (the best gait so far stays the best).</summary>
        void Restart(Line line)
        {
            var g = line.parent.Clone();
            for (int i = 0; i < XgGaitGenes.Legs; i++) g.phase[i] = rng.NextDouble();
            line.parent = g; line.score = Score(g); line.stalled = 0; line.step = 1;
        }

        XgGaitGenes Mutate(XgGaitGenes parent)
        {
            var g = parent.Clone();
            double k = step, r = rng.NextDouble();
            if (r < SideBackChance)
            {
                // One side, from a pair backwards: changes one neighbour-to-neighbour step on that side, keeps the rest.
                int side = rng.Below(2), from = 1 + rng.Below(3); double d = Gauss() * PhaseSigma * k;
                for (int pair = from; pair < 4; pair++) g.phase[pair * 2 + side] += d;
            }
            else if (r < SideBackChance + BodyBackChance)
            {
                // Both sides from a pair backwards, or (half the time) that pair alone: the lag between pairs.
                bool single = r >= SideBackChance + BodyBackChance / 2;
                int from = 1 + rng.Below(3); double d = Gauss() * PhaseSigma * k;
                for (int pair = from; pair < (single ? from + 1 : 4); pair++) { g.phase[pair * 2] += d; g.phase[pair * 2 + 1] += d; }
            }
            else if (r < SideBackChance + BodyBackChance + WholeSideChance)
            {
                // One whole side: the timing between left and right.
                int side = rng.Below(2); double d = Gauss() * PhaseSigma * k;
                for (int pair = 0; pair < 4; pair++) g.phase[pair * 2 + side] += d;
            }
            else for (int i = 0; i < XgGaitGenes.Legs; i++) if (Pick()) g.phase[i] += Gauss() * PhaseSigma * k;
            if (Pick()) g.duty += Gauss() * DutySigma * k;
            if (Pick()) g.stride += Gauss() * StrideSigma * k;
            if (Pick()) g.freq += Gauss() * FreqSigma * k;
            g.Clamp();
            return g;
        }

        bool Pick() => rng.NextDouble() < GeneChance;

        double Gauss()
        {
            double u = Math.Max(1e-12, rng.NextDouble()), v = rng.NextDouble();
            return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
        }

        /// <summary>
        /// How close a gait is to the alternating tetrapod real spiders use (L1 R2 L3 R4 against R1 L2 R3 L4): 1 when the
        /// two groups are each in step and half a cycle apart, near 0 when the phases are scattered.
        /// </summary>
        public static double Tetrapod(XgGaitGenes g)
        {
            int[] a = { 0, 3, 4, 7 }, b = { 1, 2, 5, 6 };
            double ax = 0, ay = 0, bx = 0, by = 0;
            foreach (int i in a) { ax += Math.Cos(2 * Math.PI * g.phase[i]); ay += Math.Sin(2 * Math.PI * g.phase[i]); }
            foreach (int i in b) { bx += Math.Cos(2 * Math.PI * g.phase[i]); by += Math.Sin(2 * Math.PI * g.phase[i]); }
            double ra = Math.Sqrt(ax * ax + ay * ay) / 4, rb = Math.Sqrt(bx * bx + by * by) / 4;
            if (ra < 1e-9 || rb < 1e-9) return 0;
            double apart = (1 - (ax * bx + ay * by) / (ra * rb * 16)) / 2;
            return ra * rb * apart;
        }

        /// <summary>
        /// How clean a wave gait is: each side's legs lift one after another with an even spacing (back to front or front
        /// to back), and each left leg is half a cycle from its right partner. 1 for the alternating tetrapod (spacing a
        /// half) and for an even wave of quarters; near 0 when the phases are scattered.
        /// </summary>
        public static double Wave(XgGaitGenes g)
        {
            double ix = 0, iy = 0, cx = 0, cy = 0;
            for (int pair = 0; pair < 4; pair++)
            {
                for (int side = 0; side < 2 && pair < 3; side++)
                {
                    double d = 2 * Math.PI * (g.phase[pair * 2 + side] - g.phase[pair * 2 + 2 + side]);
                    ix += Math.Cos(d); iy += Math.Sin(d);
                }
                double c = 2 * Math.PI * (g.phase[pair * 2] - g.phase[pair * 2 + 1]);
                cx += Math.Cos(c); cy += Math.Sin(c);
            }
            double ri = Math.Sqrt(ix * ix + iy * iy) / 6, rc = Math.Sqrt(cx * cx + cy * cy) / 4;
            // The spacing must be a real step apart (an eighth of a cycle or more), not the whole side lifting at once.
            double spacing = ri < 1e-9 ? 0 : Math.Min(1, (1 - ix / (6 * ri)) / (1 - Math.Cos(Math.PI / 4)));
            double anti = rc < 1e-9 ? 0 : (1 - cx / (4 * rc)) / 2;
            return ri * spacing * rc * anti;
        }
    }
}
