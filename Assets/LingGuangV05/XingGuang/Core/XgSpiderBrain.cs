using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>How fast a trip is made. Hop is the quick skipping run the crawler uses between items.</summary>
    public enum XgSpiderGait { Creep = 0, Walk = 1, Scurry = 2, Dash = 3, Hop = 4 }

    /// <summary>
    /// The small things a spider does when it is not walking. None means "standing and reading".
    /// Linger and Scan belong to the crawler's reading; Dangle (a drop on a thread) and Sleep to the desktop.
    /// </summary>
    public enum XgSpiderAct { None = 0, Look, Groom, Stretch, Drum, Hop, Think, Sleep, Dangle, Startle, Dance, Scan, Linger }

    /// <summary>Where the spider is: wandering the desktop, watching the labelling card, or reading a page.</summary>
    public enum XgSpiderCtx { Roam = 0, Watch = 1, Read = 2 }

    /// <summary>Speed profiles of the gaits (scale on the walker's own top speed) and their handling.</summary>
    public static class XgSpiderGaits
    {
        /// <summary>The range a trip's cruise speed is drawn from.</summary>
        public static void Range(XgSpiderGait g, out double lo, out double hi)
        {
            switch (g)
            {
                case XgSpiderGait.Creep: lo = .25; hi = .45; break;
                case XgSpiderGait.Walk: lo = .75; hi = 1.05; break;
                case XgSpiderGait.Scurry: lo = 1.35; hi = 1.8; break;
                case XgSpiderGait.Dash: lo = 2.4; hi = 3.0; break;
                default: lo = 1.1; hi = 1.5; break;
            }
        }

        /// <summary>Exponential rate at which the commanded speed follows its target: a creep eases in slowly, a dash jumps away.</summary>
        public static double Accel(XgSpiderGait g)
        {
            switch (g) { case XgSpiderGait.Creep: return 2.5; case XgSpiderGait.Walk: return 4; case XgSpiderGait.Scurry: return 7; case XgSpiderGait.Dash: return 10; default: return 8; }
        }

        /// <summary>Distance (in body sizes of 40 units) over which it slows down before the end of a trip.</summary>
        public static double Brake(XgSpiderGait g)
        {
            switch (g) { case XgSpiderGait.Creep: return 18; case XgSpiderGait.Walk: return 40; case XgSpiderGait.Scurry: return 70; case XgSpiderGait.Dash: return 110; default: return 45; }
        }

        /// <summary>Turn rate in radians per second: a dash can swerve, a creep turns lazily.</summary>
        public static double Turn(XgSpiderGait g)
        {
            switch (g) { case XgSpiderGait.Creep: return 1.8; case XgSpiderGait.Walk: return 2.6; case XgSpiderGait.Scurry: return 4.2; case XgSpiderGait.Dash: return 5.6; default: return 3.5; }
        }
    }

    /// <summary>
    /// One planned walk: a quadratic curve from start to end (a bend of 0 is a straight line), the gait and cruise
    /// speed, and an optional stop part-way along. Progress only ever grows, measured by projecting the spider onto the chord.
    /// </summary>
    public sealed class XgSpiderTrip
    {
        public double sx, sy, ex, ey, cx, cy, length, bend;
        public XgSpiderGait gait;
        /// <summary>Cruise speed scale drawn from the gait's range.</summary>
        public double speed;
        /// <summary>Progress 0–1 along the chord.</summary>
        public double u;
        /// <summary>Where along the trip it stops for a moment (negative: it does not).</summary>
        public double stopU = -1;
        public double holdSeconds;
        public bool holding, stopDone;

        public void Point(double t, out double x, out double y)
        {
            double a = (1 - t) * (1 - t), b = 2 * (1 - t) * t, c = t * t;
            x = a * sx + b * cx + c * ex; y = a * sy + b * cy + c * ey;
        }

        public void Advance(double px, double py)
        {
            if (length < 1e-6) { u = 1; return; }
            double k = ((px - sx) * (ex - sx) + (py - sy) * (ey - sy)) / (length * length);
            if (k > u) u = Math.Min(1, k);
        }
    }

    /// <summary>
    /// The spider's behaviour as a small state machine, free of the engine so it can be tested with a seeded
    /// <see cref="Random"/>. It decides how a trip is made (gait, speed, bend, stops, direction changes) and which idle
    /// action to play when it rests, by weighted randomness with per-action cooldowns so that the same action does not
    /// come twice in a row (unless nothing else is allowed). The views (resident on the desktop, crawler on the page)
    /// feed it time and what they see (the cursor, an opened window, a learned ability) and read back what to do;
    /// <see cref="XgSpiderPoses"/> turns an action into a pose. Drag and the hand-answer mode suspend it.
    /// </summary>
    public sealed class XgSpiderBrain
    {
        const int ActCount = 13;

        struct Entry
        {
            public XgSpiderAct act; public double weight, cooldown;
            public Entry(XgSpiderAct a, double w, double cd) { act = a; weight = w; cooldown = cd; }
        }

        // Weighted choices per context. "Nothing" (stand and read / breathe) is an option of its own.
        static readonly Entry[] RoamTable =
        {
            new Entry(XgSpiderAct.Look, 22, 6), new Entry(XgSpiderAct.Groom, 14, 14), new Entry(XgSpiderAct.Stretch, 10, 20),
            new Entry(XgSpiderAct.Drum, 12, 10), new Entry(XgSpiderAct.Hop, 8, 18), new Entry(XgSpiderAct.Dangle, 4, 60),
            new Entry(XgSpiderAct.Sleep, 0, 30),
        };
        static readonly Entry[] WatchTable =
        {
            new Entry(XgSpiderAct.Think, 24, 8), new Entry(XgSpiderAct.Look, 18, 6), new Entry(XgSpiderAct.Drum, 18, 8),
            new Entry(XgSpiderAct.Groom, 10, 16), new Entry(XgSpiderAct.Stretch, 8, 22), new Entry(XgSpiderAct.Hop, 5, 25),
        };
        static readonly Entry[] ReadTable =
        {
            new Entry(XgSpiderAct.Linger, 30, 4), new Entry(XgSpiderAct.Scan, 25, 5), new Entry(XgSpiderAct.Drum, 10, 8),
            new Entry(XgSpiderAct.Look, 8, 10), new Entry(XgSpiderAct.Groom, 8, 25), new Entry(XgSpiderAct.Stretch, 6, 30),
            new Entry(XgSpiderAct.Hop, 8, 14),
        };
        static readonly Entry[][] AllTables = { RoamTable, WatchTable, ReadTable };
        const double NothingWeight = 40;

        /// <summary>Seconds without player input after which it may fall asleep (roaming only).</summary>
        public const double SleepAfter = 40;
        /// <summary>Gap between startles, so a cursor sweeping over it does not make it jump all the time.</summary>
        public const double StartleGap = 3.5;

        readonly Random rng;
        readonly double[] readyAt = new double[ActCount];
        XgSpiderAct then;
        double idleLeft = 1.5, startleReady, hopPhase, driftPhase;
        bool pendingJoy;
        double joyAt;
        bool redirect;
        double cur;
        XgSpiderGait lastGait = XgSpiderGait.Walk;

        public XgSpiderBrain(Random rng) { this.rng = rng ?? new Random(); driftPhase = this.rng.NextDouble() * 6.28; }

        /// <summary>Called each time an action starts (tests and the views' sound hooks).</summary>
        public Action<XgSpiderAct> Started;

        public XgSpiderCtx Ctx { get; private set; }
        /// <summary>With the 减少特效 setting: fewer and smaller motions.</summary>
        public bool Reduced;
        /// <summary>Seconds since the player last touched the mouse (set by the view); long quiet lets it sleep.</summary>
        public double Quiet;
        /// <summary>Watching, and the player takes a while: thinking comes up more.</summary>
        public bool Curious;
        /// <summary>The view has a place to hang a thread (a free strip of desktop above it).</summary>
        public bool CanDangle = true;
        /// <summary>Parked in a corner: it sleeps on and is not woken by the player's input.</summary>
        public bool DeepSleep;
        /// <summary>Optional veto on actions (null allows all): a view can rule out what does not fit where it is.</summary>
        public Func<XgSpiderAct, bool> Filter;

        public double Clock { get; private set; }
        public XgSpiderAct Act { get; private set; }
        /// <summary>The last action that started (never repeated straight away).</summary>
        public XgSpiderAct Last { get; private set; }
        public double ActElapsed { get; private set; }
        public double ActSeconds { get; private set; }
        /// <summary>A random sign (−1 or 1) or small count for the current action (which way it looks, which leg, one hop or two).</summary>
        public int Variant { get; private set; } = 1;
        public double ActT => ActSeconds > 0 ? Math.Min(1, ActElapsed / ActSeconds) : 0;
        /// <summary>Seconds left of a standing rest after an arrival.</summary>
        public double RestLeft { get; private set; }
        public bool Resting => Act != XgSpiderAct.None || RestLeft > 0;
        public bool Suspended { get; private set; }
        /// <summary>Unit vector pointing away from what startled it.</summary>
        public double AwayX { get; private set; }
        public double AwayY { get; private set; }
        public XgSpiderTrip Trip { get; private set; }
        public bool HasTrip => Trip != null;
        /// <summary>Per-act start counts (tests).</summary>
        public readonly int[] Counts = new int[ActCount];

        public XgSpiderGait Gait => Trip != null ? Trip.gait : XgSpiderGait.Walk;
        public double TurnRate => XgSpiderGaits.Turn(Gait);
        public double Brake => XgSpiderGaits.Brake(Gait);
        /// <summary>0–1 lift of the skipping gait (the body leaves the page at the middle of each skip).</summary>
        public double TravelLift => Trip != null && Trip.gait == XgSpiderGait.Hop && cur > .1 ? Math.Sin(hopPhase) * Math.Sin(hopPhase) : 0;
        /// <summary>It is standing at a mid-path stop.</summary>
        public bool Holding => Trip != null && Trip.holding;

        /// <summary>The context changed (watching started, a page opened): what it was doing is dropped.</summary>
        public void SetContext(XgSpiderCtx ctx)
        {
            if (ctx == Ctx) return;
            Ctx = ctx;
            Interrupt();
        }

        /// <summary>Stops the current action and trip and starts over calmly (the press of a button, a new context).</summary>
        public void Interrupt()
        {
            Act = XgSpiderAct.None; ActElapsed = ActSeconds = 0; then = XgSpiderAct.None;
            RestLeft = 0; Trip = null; cur = 0; redirect = false;
            idleLeft = 1.2 + rng.NextDouble() * 1.3;
        }

        /// <summary>Dragged, or answering for the player: it does nothing of its own until resumed.</summary>
        public void SetSuspended(bool on)
        {
            if (on == Suspended) return;
            Suspended = on;
            if (on) Interrupt();
            else { idleLeft = 1.5 + rng.NextDouble() * 1.5; startleReady = Math.Max(startleReady, Clock + 1); }
        }

        // ───────────── frame ─────────────

        /// <summary>One step. <paramref name="atRest"/>: it is standing still, so an idle action may start.</summary>
        public void Tick(double dt, bool atRest)
        {
            if (dt < 0 || double.IsNaN(dt)) dt = 0;
            Clock += dt;
            if (Suspended) return;
            if (RestLeft > 0) RestLeft = Math.Max(0, RestLeft - dt);
            if (Trip != null && Trip.holding && Trip.holdSeconds > 0) Trip.holdSeconds -= dt;
            if (pendingJoy && Clock - joyAt > 25) pendingJoy = false;
            if (pendingJoy && Act != XgSpiderAct.Startle) { pendingJoy = false; Begin(XgSpiderAct.Dance, 1.7 + rng.NextDouble() * .5); return; }
            if (Act != XgSpiderAct.None)
            {
                ActElapsed += dt;
                // The player is back: it wakes with a stretch.
                if (Act == XgSpiderAct.Sleep && !DeepSleep && Quiet < 2.5 && ActElapsed > 1.5) { End(XgSpiderAct.Stretch); return; }
                if (ActElapsed >= ActSeconds) End(XgSpiderAct.None);
                return;
            }
            if (atRest)
            {
                idleLeft -= dt;
                if (idleLeft <= 0) PickIdle(false);
            }
        }

        void End(XgSpiderAct follow)
        {
            var next = then != XgSpiderAct.None ? then : follow;
            then = XgSpiderAct.None;
            Act = XgSpiderAct.None; ActElapsed = ActSeconds = 0;
            RestLeft = Math.Min(RestLeft, 1.5);
            idleLeft = Ctx == XgSpiderCtx.Watch ? 2.2 + rng.NextDouble() * 3.3 : .8 + rng.NextDouble() * 1.4;
            if (next != XgSpiderAct.None) Begin(next, Duration(next));
        }

        /// <summary>Starts an action, forced or picked; also the standing rest lasts at least as long as it.</summary>
        void Begin(XgSpiderAct a, double seconds)
        {
            Act = a; ActElapsed = 0; ActSeconds = seconds;
            Last = a;
            Counts[(int)a]++;
            Variant = rng.NextDouble() < .5 ? -1 : 1;
            if (a == XgSpiderAct.Hop && rng.NextDouble() < .35) Variant = 2;
            cur = 0;
            for (int i = 0; i < readyAt.Length; i++) if ((XgSpiderAct)i == a) readyAt[i] = Clock + CooldownOf(a) * (.9 + rng.NextDouble() * .5);
            RestLeft = Math.Max(RestLeft, seconds);
            Started?.Invoke(a);
        }

        double CooldownOf(XgSpiderAct a)
        {
            foreach (var t in AllTables)
                foreach (var e in t) if (e.act == a) return e.cooldown;
            return 8;
        }

        double Duration(XgSpiderAct a)
        {
            double r = rng.NextDouble();
            switch (a)
            {
                case XgSpiderAct.Look: return 1.6 + r * 1.0;
                case XgSpiderAct.Groom: return 1.8 + r * 1.2;
                case XgSpiderAct.Stretch: return 1.3 + r * .6;
                case XgSpiderAct.Drum: return 1.0 + r * .6;
                case XgSpiderAct.Hop: return .6 + r * .3;
                case XgSpiderAct.Think: return 1.6 + r * .8;
                case XgSpiderAct.Sleep: return 9 + r * 9;
                case XgSpiderAct.Dangle: return 3.2 + r * 1.4;
                case XgSpiderAct.Startle: return .9;
                case XgSpiderAct.Dance: return 1.7 + r * .5;
                case XgSpiderAct.Scan: return 1.6 + r * 1.0;
                case XgSpiderAct.Linger: return .6 + r * .7;
                default: return 1;
            }
        }

        bool Allowed(XgSpiderAct a)
        {
            if (Filter != null && !Filter(a)) return false;
            if (Reduced && (a == XgSpiderAct.Hop || a == XgSpiderAct.Dangle)) return false;
            if (a == XgSpiderAct.Dangle && !CanDangle) return false;
            if (a == XgSpiderAct.Sleep && (Quiet < SleepAfter || Ctx != XgSpiderCtx.Roam)) return false;
            return true;
        }

        double WeightOf(Entry e)
        {
            double w = e.weight;
            if (e.act == XgSpiderAct.Sleep) w = 80;
            if (e.act == XgSpiderAct.Think && Curious) w *= 3;
            if (Quiet > SleepAfter && e.act != XgSpiderAct.Sleep) w *= .4;
            return w;
        }

        Entry[] Table => Ctx == XgSpiderCtx.Watch ? WatchTable : Ctx == XgSpiderCtx.Read ? ReadTable : RoamTable;

        /// <summary>
        /// Picks an idle action by weight among those off cooldown, skipping the one it did last. With
        /// <paramref name="mustAct"/> "nothing" is not an option. Returns true when an action started.
        /// </summary>
        bool PickIdle(bool mustAct)
        {
            var table = Table;
            double total = 0;
            bool lastOnly = false;
            for (int pass = 0; pass < 2 && total <= 0; pass++)
            {
                // Pass 1 only runs when nothing else is allowed: then the last action may come again.
                lastOnly = pass == 1;
                foreach (var e in table)
                {
                    if (!Eligible(e, lastOnly)) continue;
                    total += WeightOf(e);
                }
            }
            double nothing = mustAct || lastOnly ? 0 : NothingWeight;
            if (total + nothing <= 0)
            {
                idleLeft = Ctx == XgSpiderCtx.Watch ? 1.5 + rng.NextDouble() * 1.5 : 99;
                return false;
            }
            double roll = rng.NextDouble() * (total + nothing);
            if (roll >= total)
            {
                // Just stands and reads for a while longer.
                idleLeft = Ctx == XgSpiderCtx.Roam ? 99 : 1.5 + rng.NextDouble() * 1.5;
                return false;
            }
            foreach (var e in table)
            {
                if (!Eligible(e, lastOnly)) continue;
                roll -= WeightOf(e);
                if (roll < 0) { Begin(e.act, Duration(e.act)); return true; }
            }
            return false;
        }

        bool Eligible(Entry e, bool lastOnly)
        {
            if (WeightOf(e) <= 0 || !Allowed(e.act)) return false;
            if (Clock < readyAt[(int)e.act]) return false;
            return lastOnly ? e.act == Last : e.act != Last;
        }

        // ───────────── events ─────────────

        /// <summary>The cursor rushed at it, or a window opened beside it: a jolt and a quick back-off away from (awayX, awayY).</summary>
        public bool Startle(double awayX, double awayY)
        {
            if (Suspended || Clock < startleReady || Act == XgSpiderAct.Startle || Act == XgSpiderAct.Dance) return false;
            double n = Math.Sqrt(awayX * awayX + awayY * awayY);
            if (n < 1e-6) { awayX = 0; awayY = -1; n = 1; }
            AwayX = awayX / n; AwayY = awayY / n;
            startleReady = Clock + StartleGap;
            Trip = null; RestLeft = 0;
            Begin(XgSpiderAct.Startle, Duration(XgSpiderAct.Startle));
            then = XgSpiderAct.Look;
            return true;
        }

        /// <summary>An ability was learned or a record set: a little spin and dance as soon as it is free to (queued while dragged).</summary>
        public void Joy() { pendingJoy = true; joyAt = Clock; }

        /// <summary>Starts a given action now (the views use it for a thinking moment or a pause on a word).</summary>
        public bool Do(XgSpiderAct a, double seconds = -1)
        {
            if (Suspended || a == XgSpiderAct.None) return false;
            Trip = null;
            Begin(a, seconds > 0 ? seconds : Duration(a));
            return true;
        }

        /// <summary>
        /// Arrived somewhere and rests there. Actions may come up during the rest (sleep after a long quiet).
        /// </summary>
        public void Rest(double seconds)
        {
            Trip = null; cur = 0;
            RestLeft = Math.Max(RestLeft, seconds);
            idleLeft = Math.Min(idleLeft, .2 + rng.NextDouble() * .6);
        }

        /// <summary>
        /// Arrived at a waypoint: with the given chance it starts an idle action right away (the crawler between words).
        /// Returns whether one started.
        /// </summary>
        public bool Arrive(double chance)
        {
            Trip = null;
            if (Suspended || Act != XgSpiderAct.None || rng.NextDouble() >= chance) return false;
            return PickIdle(true);
        }

        // ───────────── trips ─────────────

        /// <summary>Plans a walk, drawing the gait, speed, bend and an optional stop.</summary>
        public XgSpiderTrip Plan(double sx, double sy, double ex, double ey, XgSpiderGait? force = null)
        {
            double dx = ex - sx, dy = ey - sy;
            double len = Math.Sqrt(dx * dx + dy * dy);
            var g = force ?? PickGait(len);
            if (Reduced && (g == XgSpiderGait.Dash || g == XgSpiderGait.Hop)) g = XgSpiderGait.Scurry;
            XgSpiderGaits.Range(g, out double lo, out double hi);
            double speed = lo + (hi - lo) * rng.NextDouble();
            if (Reduced) speed = Math.Min(speed, 1.15);
            double bend = 0;
            double sign = rng.NextDouble() < .5 ? -1 : 1;
            if (Ctx == XgSpiderCtx.Roam) bend = rng.NextDouble() < .55 ? rng.NextDouble() * .05 : .12 + rng.NextDouble() * .18;
            else if (Ctx == XgSpiderCtx.Read) bend = rng.NextDouble() * .1;
            else bend = rng.NextDouble() * .08;
            bend *= sign * (Reduced ? .5 : 1);
            var t = new XgSpiderTrip { sx = sx, sy = sy, ex = ex, ey = ey, length = len, gait = g, speed = speed, bend = bend };
            // Control point of the curve: off the middle of the chord, sideways.
            double nx = len > 1e-6 ? -dy / len : 0, ny = len > 1e-6 ? dx / len : 0;
            t.cx = (sx + ex) * .5 + nx * bend * len; t.cy = (sy + ey) * .5 + ny * bend * len;
            double stopChance = Ctx == XgSpiderCtx.Roam ? (len > 150 ? .4 : 0) : Ctx == XgSpiderCtx.Read ? (len > 110 ? .12 : 0) : 0;
            if (rng.NextDouble() < stopChance && g != XgSpiderGait.Dash)
            {
                t.stopU = .35 + rng.NextDouble() * .35;
                t.holdSeconds = .35 + rng.NextDouble() * .75;
            }
            lastGait = g;
            Trip = t;
            return t;
        }

        XgSpiderGait PickGait(double len)
        {
            double c = 0, w = 0, s = 0, d = 0, h = 0;
            switch (Ctx)
            {
                case XgSpiderCtx.Roam: c = 20; w = 42; s = 30; d = len >= 160 ? 8 : 0; break;
                case XgSpiderCtx.Watch: w = 25; s = 60; d = len >= 200 ? 10 : 0; break;
                default: c = 14; w = 44; s = 30; h = 12; break;
            }
            if (Reduced) { d = 0; h = 0; s *= .5; }
            if (len < 60) { s *= .3; d = 0; h *= .3; }
            // The gait just used comes less often, so successive trips differ.
            switch (lastGait)
            {
                case XgSpiderGait.Creep: c *= .3; break;
                case XgSpiderGait.Walk: w *= .3; break;
                case XgSpiderGait.Scurry: s *= .3; break;
                case XgSpiderGait.Dash: d *= .3; break;
                default: h *= .3; break;
            }
            double total = c + w + s + d + h;
            if (total <= 0) return XgSpiderGait.Walk;
            double r = rng.NextDouble() * total;
            if ((r -= c) < 0) return XgSpiderGait.Creep;
            if ((r -= w) < 0) return XgSpiderGait.Walk;
            if ((r -= s) < 0) return XgSpiderGait.Scurry;
            if ((r -= d) < 0) return XgSpiderGait.Dash;
            return XgSpiderGait.Hop;
        }

        /// <summary>
        /// One frame of a trip: the point to steer to (a little ahead on the curve) and the speed scale to walk at,
        /// eased in from rest and out into a mid-path stop. Returns true when it has arrived (within <paramref name="tolerance"/>).
        /// </summary>
        public bool Steer(double dt, double px, double py, double tolerance, out double aimX, out double aimY, out double scale)
        {
            aimX = px; aimY = py; scale = 0;
            var t = Trip;
            if (t == null) return true;
            t.Advance(px, py);
            double ddx = t.ex - px, ddy = t.ey - py;
            bool arrived = ddx * ddx + ddy * ddy <= tolerance * tolerance;
            t.Point(Math.Min(1, t.u + .22), out aimX, out aimY);
            if (t.u >= .88 || t.length < 40) { aimX = t.ex; aimY = t.ey; }
            if (arrived) { scale = cur; return true; }

            double target = t.speed * (1 + .08 * Math.Sin(Clock * .9 + driftPhase));
            if (t.stopU >= 0 && !t.stopDone && t.u >= t.stopU)
            {
                target = 0;
                if (!t.holding && cur < .08 * t.speed)
                {
                    t.holding = true;
                    // Standing there it may glance around.
                    if (!Reduced && rng.NextDouble() < .6 && t.holdSeconds > .5) { Variant = rng.NextDouble() < .5 ? -1 : 1; BeginMicroLook(Math.Min(t.holdSeconds, .9)); }
                }
                if (t.holding && t.holdSeconds <= 0 && Act == XgSpiderAct.None)
                {
                    t.holding = false; t.stopDone = true;
                    // After the stop it sometimes changes its mind.
                    if (rng.NextDouble() < .3) redirect = true;
                }
            }
            double rate = XgSpiderGaits.Accel(t.gait);
            cur += (target - cur) * (1 - Math.Exp(-rate * Math.Max(0, dt)));
            scale = cur;
            if (t.gait == XgSpiderGait.Hop)
            {
                hopPhase += dt * 7;
                double s = Math.Sin(hopPhase);
                scale = cur * (.25 + 1.5 * s * s);
            }
            return false;
        }

        void BeginMicroLook(double seconds)
        {
            Begin(XgSpiderAct.Look, Math.Max(.5, seconds));
            // A mid-path glance does not cool the ordinary look down.
            readyAt[(int)XgSpiderAct.Look] = Clock;
            RestLeft = ActSeconds;
        }

        /// <summary>True once after it decided to turn around somewhere new (the view then picks another target).</summary>
        public bool TakeRedirect() { bool r = redirect; redirect = false; return r; }
    }

    /// <summary>The body pose an action asks for. Lengths are in body-size units (the view multiplies by the walker's size).</summary>
    public struct XgBodyPose
    {
        /// <summary>Radians added to the heading.</summary>
        public double yaw;
        /// <summary>Multiplies the body scale.</summary>
        public double scale;
        /// <summary>Up (+) or down (−) on the screen, and sideways.</summary>
        public double lift, sway;
        /// <summary>1 awake, near 0 asleep; and extra brightness of the eyes 0–1.</summary>
        public double eyeOpen, eyeBoost;
        /// <summary>0–1 how far the thread is let out (dangling).</summary>
        public double thread;
    }

    /// <summary>
    /// A leg's pose override: how much the foot leaves its planted spot (w), where it goes relative to the hip (a scale of
    /// the rest vector and a shift forward and outward, in size units).
    /// </summary>
    public struct XgLegPose { public double w, scale, fwd, lat; }

    /// <summary>
    /// Turns an action and its progress into poses: pure functions of the normalised time, so every action starts and ends
    /// in the neutral pose and the views can ease between them.
    /// </summary>
    public static class XgSpiderPoses
    {
        static double Smooth(double k) { k = Math.Max(0, Math.Min(1, k)); return k * k * (3 - 2 * k); }

        /// <summary>Ease in over the first <paramref name="inSec"/> seconds and out over the last <paramref name="outSec"/>.</summary>
        public static double Env(double t, double seconds, double inSec = .25, double outSec = .3)
        {
            if (seconds <= 0) return 0;
            return Math.Min(Smooth(t * seconds / Math.Max(1e-3, inSec)), Smooth((1 - t) * seconds / Math.Max(1e-3, outSec)));
        }

        static double Keys(double t, double[] ts, double[] vs)
        {
            if (t <= ts[0]) return vs[0];
            for (int i = 1; i < ts.Length; i++)
                if (t <= ts[i]) return vs[i - 1] + (vs[i] - vs[i - 1]) * Smooth((t - ts[i - 1]) / (ts[i] - ts[i - 1]));
            return vs[vs.Length - 1];
        }

        static readonly double[] LookT = { 0, .25, .5, .65, .85, 1 };
        static readonly double[] LookV = { 0, .8, .8, -.7, -.7, 0 };

        public static double Amp(bool reduced) => reduced ? .45 : 1;

        public static XgBodyPose Body(XgSpiderAct a, double t, double seconds, int variant, bool reduced)
        {
            var p = new XgBodyPose { scale = 1, eyeOpen = 1 };
            double amp = Amp(reduced), env = Env(t, seconds), clock = t * seconds, v = variant >= 0 ? 1 : -1;
            switch (a)
            {
                case XgSpiderAct.Look:
                    p.yaw = amp * v * Keys(t, LookT, LookV); p.eyeBoost = .6 * env; break;
                case XgSpiderAct.Groom:
                    p.yaw = v * .25 * amp * env; p.eyeOpen = 1 - .2 * env; break;
                case XgSpiderAct.Stretch:
                    p.scale = 1 + .06 * amp * env;
                    p.yaw = .08 * amp * Math.Sin(clock * 22) * Smooth((t - .7) / .15) * Smooth((1 - t) / .1);
                    break;
                case XgSpiderAct.Drum:
                    p.scale = 1 + .015 * amp * Math.Sin(clock * 14) * env; p.eyeBoost = .3 * env; break;
                case XgSpiderAct.Hop:
                {
                    int n = variant == 2 ? 2 : 1;
                    double k = t * n - Math.Floor(t * n);
                    if (t >= 1) k = 0;
                    double h = Math.Sin(Math.PI * k);
                    p.lift = 14 * amp * h; p.scale = 1 + .14 * amp * h; break;
                }
                case XgSpiderAct.Think:
                    p.scale = 1 - .12 * amp * env;
                    p.yaw = amp * (.14 * Math.Sin(clock * 9) + v * .18) * env;
                    p.eyeBoost = env; break;
                case XgSpiderAct.Sleep:
                {
                    double e = Env(t, seconds, .9, .8);
                    p.scale = 1 - .09 * e + .03 * e * Math.Sin(clock * 1.6);
                    p.eyeOpen = 1 - .85 * e; p.yaw = v * .2 * e; break;
                }
                case XgSpiderAct.Dangle:
                {
                    // Down the thread in the first part, a sway while it hangs, back up in the last part.
                    double down = Smooth(clock / 1.2), up = Smooth((seconds - clock) / 1.1);
                    double e = Math.Min(down, up);
                    p.lift = -64 * e; p.sway = 5 * Math.Sin(clock * 2.2) * e; p.thread = e;
                    p.yaw = .12 * Math.Sin(clock * 1.7) * e; p.eyeBoost = .4 * e; break;
                }
                case XgSpiderAct.Startle:
                {
                    double k = Math.Exp(-t * 5) * Smooth((1 - t) / .3);
                    p.scale = 1 + .22 * amp * k; p.lift = 8 * amp * Math.Sin(Math.PI * Math.Min(1, t / .25));
                    p.eyeBoost = Smooth((1 - t) / .3); break;
                }
                case XgSpiderAct.Dance:
                {
                    // The spin eases from start to finish and ends at the same facing (whole turns).
                    p.yaw = v * 2 * Math.PI * (reduced ? 1 : 2) * Smooth(t);
                    p.scale = 1 + .08 * amp * Math.Sin(clock * 10) * env;
                    p.lift = 6 * amp * Math.Abs(Math.Sin(clock * 9)) * env; p.eyeBoost = env; break;
                }
                case XgSpiderAct.Scan:
                    p.yaw = .45 * amp * Math.Sin(clock * (2 * Math.PI / 1.2)) * env; p.eyeBoost = .5 * env; break;
                case XgSpiderAct.Linger:
                    p.yaw = .1 * amp * Math.Sin(clock * 7) * env; p.eyeBoost = .3 * env; break;
            }
            return p;
        }

        /// <summary>The pose of leg <paramref name="i"/> of <paramref name="count"/> (0 and 1 are the front pair, left then right).</summary>
        public static XgLegPose Leg(int i, int count, XgSpiderAct a, double t, double seconds, int variant, bool reduced)
        {
            var p = new XgLegPose { scale = 1 };
            double amp = Amp(reduced), env = Env(t, seconds), clock = t * seconds;
            bool front = i < 2, rear = i >= count - 2;
            switch (a)
            {
                case XgSpiderAct.Groom:
                {
                    // One front leg is drawn through the "mouth" (rubbing), then the other.
                    int first = variant >= 0 ? 0 : 1;
                    if (!front) break;
                    bool isFirst = i == first;
                    double win = isFirst ? Env(t / .6, seconds * .6, .25, .2) * (t <= .6 ? 1 : 0)
                                         : Env((t - .4) / .6, seconds * .6, .2, .3) * (t >= .4 ? 1 : 0);
                    p.w = win; p.scale = .5; p.fwd = 8; p.lat = -3 + 2.5 * amp * Math.Sin(clock * 17);
                    break;
                }
                case XgSpiderAct.Stretch:
                    p.w = env * Math.Min(1, amp + .2); p.scale = 1 + .45 * amp;
                    p.fwd = (front ? 10 : rear ? -8 : 0) * amp; p.lat = 1.2 * amp * Math.Sin(clock * 20) * Smooth((t - .7) / .15);
                    break;
                case XgSpiderAct.Drum:
                case XgSpiderAct.Linger:
                {
                    if (!front) break;
                    double rate = a == XgSpiderAct.Drum ? 2 * Math.PI * 3.5 : 2 * Math.PI * 1.6;
                    double ph = clock * rate + (i == 1 ? Math.PI : 0);
                    p.w = env * (.5 + .5 * Math.Sin(ph)) * (a == XgSpiderAct.Drum ? 1 : .6) * Math.Min(1, amp + .2);
                    p.scale = .75; p.fwd = 6;
                    break;
                }
                case XgSpiderAct.Hop:
                {
                    int n = variant == 2 ? 2 : 1;
                    double k = t * n - Math.Floor(t * n);
                    p.w = Math.Sin(Math.PI * k) * .8 * amp; p.scale = 1.3; break;
                }
                case XgSpiderAct.Think:
                    p.w = env * .85 * Math.Min(1, amp + .2); p.scale = 1.22 + .05 * Math.Sin(clock * 9) * amp; p.lat = 3 * amp; break;
                case XgSpiderAct.Sleep:
                {
                    double e = Env(t, seconds, .9, .8);
                    p.w = e * .9; p.scale = .38; break;
                }
                case XgSpiderAct.Dangle:
                {
                    double down = Smooth(clock / 1.2), up = Smooth((seconds - clock) / 1.1);
                    p.w = Math.Min(down, up); p.scale = .45; p.fwd = -6; break;
                }
                case XgSpiderAct.Startle:
                    p.w = Math.Exp(-t * 4) * .9 * Smooth((1 - t) / .3); p.scale = 1.35; break;
                case XgSpiderAct.Dance:
                {
                    if (!front) break;
                    double ph = clock * 2 * Math.PI * 2.2 + (i == 1 ? Math.PI : 0);
                    p.w = env * .7 * Math.Abs(Math.Sin(ph / 2)); p.scale = .6; p.fwd = 12; break;
                }
            }
            return p;
        }
    }

    /// <summary>
    /// Tells when the cursor rushes at the spider: fast, aimed at it and close. Pointer and spider positions are in the
    /// same space (the spider's layer); the update is called once per frame.
    /// </summary>
    public sealed class XgSpiderThreat
    {
        double lx, ly;
        bool has;
        /// <summary>Unit vector from the cursor to the spider (the way to back off).</summary>
        public double AwayX, AwayY;
        /// <summary>Pointer speed (units per second) that counts as a rush.</summary>
        public double MinSpeed = 650;
        /// <summary>Cosine of the widest angle between the pointer's motion and the line to the spider.</summary>
        public double MinAim = .6;

        public void Reset() { has = false; }

        public bool Update(double dt, double px, double py, double sx, double sy, double near)
        {
            if (!has) { lx = px; ly = py; has = true; return false; }
            dt = Math.Max(1e-3, dt);
            double vx = (px - lx) / dt, vy = (py - ly) / dt;
            lx = px; ly = py;
            double dx = sx - px, dy = sy - py;
            double d = Math.Sqrt(dx * dx + dy * dy), speed = Math.Sqrt(vx * vx + vy * vy);
            if (d > near || d < 1e-3 || speed < MinSpeed) return false;
            if ((vx * dx + vy * dy) / (speed * d) < MinAim) return false;
            AwayX = dx / d; AwayY = dy / d;
            return true;
        }
    }
}
