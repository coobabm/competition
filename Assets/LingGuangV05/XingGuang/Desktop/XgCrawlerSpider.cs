using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The 摆渡百科 crawler's walker, as plain geometry in the page's content space (y up): a body steered toward a
    /// target, eight legs in two alternating groups, each a two-segment chain solved by the law of cosines with the knee
    /// bent the same way all the time. Written to move smoothly at any frame rate:
    /// the heading turns through an angular velocity that is itself damped (no instant start or stop of a turn), the speed
    /// follows its target by exponential smoothing with an acceleration cap, and the body only ever moves along its
    /// heading. A leg steps when its foot falls too far behind its rest spot, but only when no leg is in the air, and then
    /// the whole group (front-left, second-right, … the tetrapod pattern) goes together, so at most half the legs are up
    /// and the groups alternate. A step is an eased arc (smoothstep over a duration that shrinks with speed, the foot
    /// rising and falling on a half sine) aimed where the rest spot will be when the foot lands, plus a little lead.
    /// The body sways and bobs with the legs' lift. The new foothold is asked from <see cref="FindFoothold"/> (a word near
    /// the spot ahead), and <see cref="Planted"/> fires when the foot lands. A hidden-body walker only shows its legs
    /// (the sophon easter egg).
    /// </summary>
    public sealed class XgSpiderWalker
    {
        public sealed class Leg
        {
            /// <summary>Hip and rest spot in the body frame (x forward, y to the left).</summary>
            public Vector2 hip, rest;
            public float side;
            public int group;
            public Vector2 foot, from, to;
            public float t = 1;
            public bool stepping;
            /// <summary>Foothold word under the foot (-1 for bare page).</summary>
            public int word = -1;
            public Vector2 hipWorld, knee;
            /// <summary>Pose override (an action such as grooming or curling up): 0–1 weight and the foot's spot in the body frame.</summary>
            public float poseW;
            public Vector2 poseLocal;
            /// <summary>Where the foot is drawn: the planted foot, lifted while it steps, or blended toward the pose spot.</summary>
            public Vector2 shown;
            /// <summary>Which way the knee bends (+1 or −1, the turning sense of the hip-to-foot line): fixed per leg, so the knee never flips.</summary>
            public float bend = 1;
            /// <summary>0–1: how high the foot is above the page (0 planted, 1 at the top of a step).</summary>
            public float lift;
            /// <summary>Seconds this step takes, and how much longer a foot put on a spot by <see cref="StepTo"/> stays there.</summary>
            public float dur = .2f, hold;
            /// <summary>The foot stands on a spot chosen by <see cref="StepTo"/> (a tap, a press): it may be farther than a walking foot reaches, until it leaves.</summary>
            public bool pinned;
        }

        /// <summary>A word near a point within a radius: its index and the point to plant on, or -1.</summary>
        public delegate int Foothold(Vector2 near, float radius, out Vector2 at);

        public Foothold FindFoothold;
        public Action<XgSpiderWalker, Leg> Planted;

        public Vector2 pos, vel;
        public float heading = -Mathf.PI / 2;
        public float size = 1;
        public float maxSpeed = 70, turnRate = 2.6f;
        public float upper = 30, lower = 36;
        public bool hideBody;
        /// <summary>Body scale on top of the size (breathing while it watches).</summary>
        public float bodyScale = 1;
        /// <summary>0–1: eyes wide and bright (it is looking at the player).</summary>
        public float eyeBoost;
        /// <summary>1 awake, near 0 asleep: how open the eyes are.</summary>
        public float eyeOpen = 1;
        /// <summary>Eye brightness an action asks for on top of <see cref="eyeBoost"/> (the larger of the two shows).</summary>
        public float eyeExtra;
        /// <summary>Turns the whole body (and its feet's rest spots) by this many radians without changing the walking heading.</summary>
        public float yaw;
        /// <summary>Body and hips shifted on the page (a hop, hanging on a thread); the planted feet stay.</summary>
        public Vector2 offset;
        /// <summary>Distance, in 40-unit body sizes, over which it slows down before a target.</summary>
        public float brake = 40;
        /// <summary>Little up-and-down of the body with the steps (screen units; set by <see cref="Tick"/>).</summary>
        public float bob;
        /// <summary>Tuning: how quickly the speed rises / falls to what is wanted (per second), the turn's proportional gain and its damping (per second).</summary>
        public float accelRate = 6, brakeRate = 7, turnGain = 5, turnDamp = 12;
        /// <summary>Tuning: scales the body's sway and bob with the steps (0 off, 1 as designed).</summary>
        public float sway = 1;
        public readonly Leg[] legs;

        // Smoothed motion state. All of it is advanced with 1 - exp(-k dt), so it does not depend on the frame rate.
        float clock, angVel, speedNow, gaitSpeed, shiftL, lastHeading, stepDur = .2f, stride = 22;
        Vector2 lastPos, extVel, moveVel, moveDir, bodyShift;
        bool seated, carryNext;
        int lastGroup = 1;
        readonly float[] need;

        const float TeleportSize = 160, TapHold = .25f, TapSwing = .2f;

        public Vector2 Forward => new Vector2(Mathf.Cos(heading + yaw), Mathf.Sin(heading + yaw));
        public Vector2 Left => new Vector2(-Mathf.Sin(heading + yaw), Mathf.Cos(heading + yaw));
        /// <summary>Speed as a multiple of <see cref="maxSpeed"/>: the gait and the step rhythm follow it.</summary>
        public float SpeedRatio => vel.magnitude / Mathf.Max(1, maxSpeed);
        /// <summary>How many feet are in the air right now (never more than half of the legs while it walks).</summary>
        public int Airborne { get { int n = 0; foreach (var l in legs) if (l.stepping) n++; return n; } }

        /// <summary>Which of the four leg pairs (front to back) a walker with fewer legs keeps.</summary>
        static int[] Pairs(int legCount) => legCount <= 4 ? new[] { 0, 3 } : legCount <= 6 ? new[] { 0, 1, 3 } : new[] { 0, 1, 2, 3 };

        public XgSpiderWalker(Vector2 start, float size, int legCount = 8)
        {
            pos = lastPos = start; this.size = size;
            lastHeading = heading;
            upper = 30 * size; lower = 36 * size; maxSpeed = 70 * size;
            float[] hx = { 7, 2.5f, -2.5f, -7 }, rx = { 36, 16, -8, -28 }, ry = { 30, 40, 40, 32 };
            var pairs = Pairs(legCount);
            legs = new Leg[pairs.Length * 2];
            need = new float[legs.Length];
            for (int i = 0; i < legs.Length; i++)
            {
                int order = i / 2, pair = pairs[order];
                float side = i % 2 == 0 ? 1 : -1;
                var leg = new Leg
                {
                    side = side, hip = new Vector2(hx[pair], 4.5f * side) * size, rest = new Vector2(rx[pair], ry[pair] * side) * size,
                    // Alternating gait: front-left, second-right, … step together.
                    group = (order + (side > 0 ? 0 : 1)) % 2,
                };
                leg.bend = RestBend(leg);
                leg.foot = leg.from = leg.to = leg.shown = ToWorld(leg.rest);
                legs[i] = leg;
            }
        }

        /// <summary>The knee turns the way that puts it outward when the foot is on its rest spot; that sense is kept for the leg's life.</summary>
        float RestBend(Leg leg)
        {
            var d = leg.rest - leg.hip;
            float len = d.magnitude;
            if (len < 1e-3f) return 1;
            float reach = Mathf.Clamp(len, Mathf.Abs(upper - lower) + .01f, upper + lower - .01f);
            float a = Mathf.Acos(Mathf.Clamp((upper * upper + reach * reach - lower * lower) / (2 * upper * reach), -1, 1));
            float phi = Mathf.Atan2(d.y, d.x);
            float outPlus = Mathf.Sin(phi + a) * leg.side, outMinus = Mathf.Sin(phi - a) * leg.side;
            return outPlus >= outMinus ? 1 : -1;
        }

        public Vector2 ToWorld(Vector2 local) => pos + Forward * local.x + Left * local.y;

        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        /// <summary>
        /// Puts every foot on its rest spot at once, standing. For a body that was moved a long way from outside (a recall,
        /// a new page): a walker that only stepped there would run all its legs after it.
        /// </summary>
        public void Reseat()
        {
            foreach (var l in legs)
            {
                var r = ToWorld(l.rest);
                l.foot = l.from = l.to = l.shown = r;
                l.t = 1; l.stepping = false; l.pinned = false; l.hold = 0; l.lift = 0; l.word = -1;
            }
            lastPos = pos; lastHeading = heading; extVel = Vector2.zero;
        }

        /// <summary>The body is being carried this frame (call before <see cref="Tick"/>): the feet leave the page and dangle, easing toward their rest spots.</summary>
        public void Carry() { carryNext = true; }

        /// <summary>Walks toward a target for one frame. Speed scales the gait; reduced keeps the body steady.</summary>
        public void Tick(float dt, Vector2 target, float speedScale, bool reduced)
        {
            if (dt <= 0) return;
            dt = Mathf.Min(dt, .1f);
            clock += dt;
            bool carry = carryNext; carryNext = false;
            if (!seated) { seated = true; Reseat(); }

            // Whatever moved the body from outside since the last tick (a lerp to a perch, a clamp, a drag): a long jump
            // re-seats the feet, a short one just counts as speed for the gait.
            var moved = carry ? Vector2.zero : pos - lastPos;
            if (!carry && moved.sqrMagnitude > TeleportSize * TeleportSize * size * size) { Reseat(); moved = Vector2.zero; }
            extVel = Vector2.Lerp(extVel, moved / dt, 1 - Mathf.Exp(-14 * dt));
            // Someone else turned it (a look toward the card): that hand owns the heading, so no leftover spin.
            if (heading != lastHeading) angVel = 0;

            // Steer: a damped turn toward the target. The wanted turn rate is proportional to the angle left (capped by
            // turnRate) and the real one follows it with its own lag, so a turn eases in and out instead of starting at full rate.
            var to = target - pos;
            float dist = to.magnitude;
            float err = 0, wantTurn = 0;
            if (dist > 2)
            {
                err = Mathf.DeltaAngle(heading * Mathf.Rad2Deg, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                float aim = Mathf.Clamp01(dist / (8 * size));   // a target almost under it no longer twitches the heading
                wantTurn = Mathf.Clamp(err * turnGain, -turnRate, turnRate) * aim;
            }
            angVel += (wantTurn - angVel) * (1 - Mathf.Exp(-turnDamp * dt));
            heading += angVel * dt;

            // Speed: slow before the target and when facing away from it, eased toward that, with a cap on the acceleration.
            float speedWant = 0;
            if (dist > 2)
                speedWant = maxSpeed * speedScale * Mathf.Clamp01(dist / Mathf.Max(1, brake * size)) * Mathf.Clamp01(1.2f - Mathf.Abs(err) * Mathf.Rad2Deg / 120f);
            float rate = speedWant > speedNow ? accelRate : brakeRate;
            float dv = (speedWant - speedNow) * (1 - Mathf.Exp(-rate * dt));
            float cap = Mathf.Max(1, maxSpeed) * 12 * dt;
            speedNow = Mathf.Max(0, speedNow + Mathf.Clamp(dv, -cap, cap));
            vel = new Vector2(Mathf.Cos(heading), Mathf.Sin(heading)) * speedNow;
            pos += vel * dt;
            lastHeading = heading;

            // The gait follows the speed the body really has (its own plus what came from outside): a creep takes slow,
            // short steps; a dash a quick flurry of long ones.
            moveVel = vel + extVel;
            float spd = Mathf.Min(moveVel.magnitude, Mathf.Max(1, maxSpeed) * 4);
            gaitSpeed += (spd - gaitSpeed) * (1 - Mathf.Exp(-10 * dt));
            moveDir = moveVel.sqrMagnitude > 1 ? moveVel.normalized : Vector2.zero;
            float ratio = gaitSpeed / Mathf.Max(1, maxSpeed);
            float moving = Mathf.Clamp01(gaitSpeed / (6 * size));
            stride = 22 * size * (.55f + .45f * Mathf.Min(ratio, 1) + .12f * Mathf.Max(0, Mathf.Min(ratio, 2.6f) - 1));
            stepDur = Mathf.Clamp(.8f * stride / Mathf.Max(gaitSpeed, 1), .10f, Mathf.Lerp(.2f, .28f, moving));

            // Sway and bob follow the legs: the body leans away from the group in the air and dips as the groups change over.
            float g0 = 0, g1 = 0; int n0 = 0, n1 = 0;
            foreach (var l in legs) { if (l.group == 0) { g0 += l.lift; n0++; } else { g1 += l.lift; n1++; } }
            float swing = (n0 > 0 ? g0 / n0 : 0) - (n1 > 0 ? g1 / n1 : 0);
            float mv = Mathf.Clamp01(gaitSpeed / (10 * size));
            float follow = 1 - Mathf.Exp(-18 * dt);
            shiftL += ((reduced ? 0 : swing * 1.3f * size * mv * sway) - shiftL) * follow;
            bodyShift = Left * shiftL;
            bob += ((reduced ? 0 : (.5f - Mathf.Abs(swing)) * 1.1f * size * mv * sway) - bob) * follow;

            // Feet in the air fly their arc; those that land are announced.
            foreach (var l in legs)
            {
                if (carry)
                {
                    l.stepping = false; l.pinned = false; l.hold = 0;
                    l.foot = Vector2.Lerp(l.foot, ToWorld(l.rest), 1 - Mathf.Exp(-10 * dt));
                    l.lift += (.55f - l.lift) * (1 - Mathf.Exp(-8 * dt));
                }
                else if (l.stepping)
                {
                    l.t += dt / Mathf.Max(.02f, l.dur);
                    if (l.t >= 1)
                    {
                        l.t = 1; l.stepping = false; l.foot = l.to; l.lift = 0;
                        if (l.pinned) l.hold = TapHold;
                        Planted?.Invoke(this, l);
                    }
                    else
                    {
                        l.foot = Vector2.Lerp(l.from, l.to, Ease(l.t));
                        l.lift = Mathf.Sin(l.t * Mathf.PI);
                    }
                }
                else
                {
                    if (l.hold > 0) l.hold -= dt;
                    l.lift = Mathf.MoveTowards(l.lift, 0, 6 * dt);
                }
            }
            if (!carry) LaunchGroup(moving);

            // Hips, the reach limit of a planted foot, the drawn foot and the knee.
            float maxReach = (upper + lower) * .96f, minReach = (upper + lower) * .3f;
            var left = Left;
            foreach (var l in legs)
            {
                var hipBase = ToWorld(l.hip) + bodyShift;
                l.hipWorld = hipBase + offset;
                if (!l.pinned && !l.stepping)
                {
                    // A planted foot is never farther than the leg reaches, nor under its own hip (the knee would whip round it).
                    var d = l.foot - hipBase;
                    float len = d.magnitude;
                    if (len > maxReach) l.foot = hipBase + d * (maxReach / len);
                    else if (len < minReach && l.poseW < .5f) l.foot = hipBase + (len > 1e-3f ? d / len : left * l.side) * minReach;
                }
                var up = l.foot + (Vector2.up * (3.2f * size) + left * (l.side * 2.2f * size)) * l.lift;
                if (l.stepping && !l.pinned && l.poseW < .5f)
                {
                    var sd = up - hipBase;
                    float sl = sd.magnitude;
                    if (sl < minReach) up = hipBase + (sl > 1e-3f ? sd / sl : left * l.side) * minReach;
                }
                l.shown = l.poseW > .001f ? Vector2.Lerp(up, ToWorld(l.poseLocal) + offset, l.poseW) : up;
                l.knee = Knee(l);
            }
            lastPos = pos;
        }

        /// <summary>
        /// Stands still unless some foot has fallen behind: then, with every foot on the page, the group whose foot is
        /// farthest behind (the other one if the same group went last and the lag is about equal) lifts together with the
        /// feet of its own that are at least half way to needing a step.
        /// </summary>
        void LaunchGroup(float moving)
        {
            foreach (var l in legs) if (l.stepping) return;
            float thr = stride * (Settled ? 1.6f : 1f);
            float m0 = 0, m1 = 0;
            for (int i = 0; i < legs.Length; i++)
            {
                var l = legs[i];
                need[i] = 0;
                if (l.hold > 0) continue;
                var off = l.foot - ToWorld(l.rest);
                float d = off.magnitude;
                if (moving > .01f && moveDir.sqrMagnitude > .5f)
                {
                    // On the move only a foot behind its spot (or off to the side, or far ahead after a turn) counts as late.
                    float along = Vector2.Dot(off, moveDir);
                    var lat = off - moveDir * along;
                    float behind = Mathf.Max(0, -along);
                    d = Mathf.Lerp(d, Mathf.Sqrt(lat.sqrMagnitude + behind * behind) + Mathf.Max(0, along - 1.5f * stride), moving);
                }
                need[i] = d / thr;
                if (l.group == 0) m0 = Mathf.Max(m0, need[i]); else m1 = Mathf.Max(m1, need[i]);
            }
            int pick;
            if (m0 >= 1 && m1 >= 1) pick = m0 * (lastGroup == 0 ? .85f : 1) >= m1 * (lastGroup == 1 ? .85f : 1) ? 0 : 1;
            else if (m0 >= 1) pick = 0;
            else if (m1 >= 1) pick = 1;
            else return;
            lastGroup = pick;
            for (int i = 0; i < legs.Length; i++)
                if (legs[i].group == pick && need[i] >= .55f && legs[i].hold <= 0) BeginStep(legs[i], moving);
        }

        /// <summary>
        /// Asks leg <paramref name="i"/> for a pose: weight 0–1, the rest vector scaled about the hip, and a shift forward and
        /// outward (in body sizes). Weight 0 hands the foot back to its planted spot.
        /// </summary>
        public void SetLegPose(int i, float w, float scale, float fwd, float lat)
        {
            if (i < 0 || i >= legs.Length) return;
            var l = legs[i];
            l.poseW = Mathf.Clamp01(w);
            l.poseLocal = l.hip + (l.rest - l.hip) * scale + new Vector2(fwd, lat * l.side) * size;
        }

        /// <summary>Lifts a planted leg and puts it down on a given spot (a tap on a word while it watches); it stays there a moment.</summary>
        public void StepTo(Leg l, Vector2 to, int word)
        {
            if (l == null || l.stepping) return;
            l.from = l.foot; l.to = to; l.t = 0; l.stepping = true; l.word = word;
            l.dur = TapSwing; l.pinned = true; l.hold = 0;
        }

        /// <summary>Stand still: legs only step when the body has really moved away from them.</summary>
        public bool Settled;

        void BeginStep(Leg l, float moving)
        {
            // Aim where the rest spot will be when the foot lands (the body's own speed, its turn), a little ahead of it
            // while on the move, so the foot is passed over during the stance and the group has time to alternate.
            float dur = stepDur;
            var body = pos + moveVel * dur;
            float h = heading + yaw + angVel * dur;
            var f = new Vector2(Mathf.Cos(h), Mathf.Sin(h));
            var aim = body + f * l.rest.x + new Vector2(-f.y, f.x) * l.rest.y + moveDir * (.5f * stride * moving);
            l.from = l.foot; l.t = 0; l.stepping = true;
            l.word = -1; l.dur = dur; l.pinned = false; l.hold = 0;
            l.to = aim;
            if (FindFoothold != null)
            {
                int w = FindFoothold(aim, 18 * size, out var at);
                if (w >= 0) { l.word = w; l.to = at; }
            }
        }

        /// <summary>Knee of a two-segment leg from hip to foot, always bent the leg's own way (<see cref="Leg.bend"/>), so it cannot flip.</summary>
        Vector2 Knee(Leg l)
        {
            var d = l.shown - l.hipWorld;
            float len = d.magnitude;
            if (len < 1e-3f) return l.hipWorld + Left * l.side * upper;
            float reach = Mathf.Clamp(len, Mathf.Abs(upper - lower) + .01f, upper + lower - .01f);
            float cosA = (upper * upper + reach * reach - lower * lower) / (2 * upper * reach);
            float a = Mathf.Acos(Mathf.Clamp(cosA, -1, 1));
            float ang = Mathf.Atan2(d.y, d.x) + a * l.bend;
            return l.hipWorld + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * upper;
        }

        /// <summary>Draws the walker (legs first, then the body unless hidden).</summary>
        public void Draw(VertexHelper vh, Color legColor, Color bodyColor, Color eyeColor, float lift, Color? halo = null)
        {
            var dark = legColor * .55f; dark.a = legColor.a * .5f;
            // A hop leaves a faint shadow on the page where the body was.
            if (!hideBody && offset.y > 1.5f)
            {
                var sc = new Color(0, 0, 0, Mathf.Clamp01(.2f - offset.y * .004f) * legColor.a);
                Oval(vh, pos - Forward * 10 * size * bodyScale, Forward, Left, 12 * size * bodyScale, 8.5f * size * bodyScale, sc);
            }
            foreach (var l in legs)
            {
                // A lifted foot leaves a faint mark where it will come down.
                if (l.lift > .08f && l.poseW < .5f) { var mark = dark; mark.a *= Mathf.Min(1, l.lift * 1.5f); XgDraw.Disc(vh, l.foot, 1.5f * size, mark, 6); }
                if (halo.HasValue)
                {
                    // A light rim around a dark leg: readable on dark and light windows alike.
                    XgDraw.Seg(vh, l.hipWorld, l.knee, 2.4f * size + 2.2f, halo.Value);
                    XgDraw.Seg(vh, l.knee, l.shown, 1.7f * size + 2.2f, halo.Value);
                    XgDraw.Disc(vh, l.knee, 1.7f * size + 1.1f, halo.Value, 8);
                }
                else
                {
                    // A soft shadow under each leg, then the leg.
                    var shadow = new Vector2(1.5f, -1.5f) * size;
                    XgDraw.Seg(vh, l.hipWorld + shadow, l.knee + shadow, 2.6f * size, dark);
                    XgDraw.Seg(vh, l.knee + shadow, l.shown + shadow, 2f * size, dark);
                }
                XgDraw.Seg(vh, l.hipWorld, l.knee, 2.4f * size, legColor);
                XgDraw.Seg(vh, l.knee, l.shown, 1.7f * size, legColor);
                XgDraw.Disc(vh, l.knee, 1.7f * size, legColor, 8);
                XgDraw.Disc(vh, l.shown, (1.8f + .8f * l.lift) * size, legColor, 8);
            }
            if (hideBody) return;
            float bs = size * bodyScale;
            var f = Forward; var left = Left;
            var c = pos + offset + bodyShift + new Vector2(0, bob + lift);
            // A neon rim around the abdomen, then the body itself.
            var rim = eyeColor; rim.a *= .55f;
            Oval(vh, c - f * 10 * bs, f, left, 13.2f * bs, 9.7f * bs, rim);
            Oval(vh, c - f * 10 * bs, f, left, 12 * bs, 8.5f * bs, bodyColor);
            Oval(vh, c + f * 3 * bs, f, left, 7 * bs, 6 * bs, bodyColor * 1.1f);
            Oval(vh, c + f * 9 * bs, f, left, 4.5f * bs, 4 * bs, bodyColor * 1.2f);
            // A thin neon stripe along the abdomen and two eyes.
            XgDraw.Seg(vh, c - f * 18 * bs, c - f * 3 * bs, 1.4f * bs, eyeColor * new Color(1, 1, 1, .6f));
            float boost = Mathf.Max(eyeBoost, eyeExtra);
            float eye = 1.3f * bs * (1 + boost * .8f) * (.2f + .8f * Mathf.Clamp01(eyeOpen));
            if (boost > .01f && eyeOpen > .3f)
            {
                var glow = eyeColor; glow.a *= .5f * boost;
                XgSoftDraw.Halo(vh, c + f * 11 * bs, 7 * bs * boost, glow, 12);
            }
            XgDraw.Disc(vh, c + f * 11 * bs + left * 2 * bs, eye, eyeColor, 8);
            XgDraw.Disc(vh, c + f * 11 * bs - left * 2 * bs, eye, eyeColor, 8);
        }

        static readonly Vector2[] OvalPoints = new Vector2[18];

        static void Oval(VertexHelper vh, Vector2 c, Vector2 f, Vector2 left, float rf, float rl, Color color)
        {
            color.a = Mathf.Clamp01(color.a);
            for (int i = 0; i < OvalPoints.Length; i++)
            {
                float a = i * Mathf.PI * 2 / OvalPoints.Length;
                OvalPoints[i] = c + f * (Mathf.Cos(a) * rf) + left * (Mathf.Sin(a) * rl);
            }
            XgDraw.Poly(vh, OvalPoints, color);
        }
    }

    /// <summary>A code-drawn layer of the crawler page; its owner fills the mesh. Never takes input.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgCrawlerLayer : MaskableGraphic
    {
        public Action<VertexHelper> draw;
        protected override void OnPopulateMesh(VertexHelper vh) { vh.Clear(); draw?.Invoke(vh); }
        public void Redraw() => SetVerticesDirty();
    }

    /// <summary>Pointer over the crawler's page: where it is (the spider chases it when close) and clicks (a new target).</summary>
    public sealed class XgCrawlerInput : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler, IPointerEnterHandler
    {
        public Action<PointerEventData> Move, Click;
        public Action Exit;
        public void OnPointerEnter(PointerEventData e) => Move?.Invoke(e);
        public void OnPointerMove(PointerEventData e) => Move?.Invoke(e);
        public void OnPointerExit(PointerEventData e) => Exit?.Invoke();
        public void OnPointerClick(PointerEventData e) => Click?.Invoke(e);
    }
}
