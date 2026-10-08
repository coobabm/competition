using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The 摆渡百科 crawler's walker, as plain geometry in the page's content space (y up): a body steered toward a
    /// target, eight legs in two alternating groups, each a two-segment chain solved by the law of cosines with the knee
    /// bent away from the body. A leg steps when its foot falls too far behind its rest spot; the new foothold is asked
    /// from <see cref="FindFoothold"/> (a word near the spot ahead), and <see cref="Planted"/> fires when the foot lands.
    /// A hidden-body walker only shows its legs (the sophon easter egg).
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
        public float bob;
        public readonly Leg[] legs;
        float stepTime = .13f, clock;

        public Vector2 Forward => new Vector2(Mathf.Cos(heading), Mathf.Sin(heading));
        public Vector2 Left => new Vector2(-Mathf.Sin(heading), Mathf.Cos(heading));

        /// <summary>Which of the four leg pairs (front to back) a walker with fewer legs keeps.</summary>
        static int[] Pairs(int legCount) => legCount <= 4 ? new[] { 0, 3 } : legCount <= 6 ? new[] { 0, 1, 3 } : new[] { 0, 1, 2, 3 };

        public XgSpiderWalker(Vector2 start, float size, int legCount = 8)
        {
            pos = start; this.size = size;
            upper = 30 * size; lower = 36 * size; maxSpeed = 70 * size;
            float[] hx = { 7, 2.5f, -2.5f, -7 }, rx = { 36, 16, -8, -28 }, ry = { 30, 40, 40, 32 };
            var pairs = Pairs(legCount);
            legs = new Leg[pairs.Length * 2];
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
                leg.foot = leg.from = leg.to = ToWorld(leg.rest);
                legs[i] = leg;
            }
        }

        public Vector2 ToWorld(Vector2 local) => pos + Forward * local.x + Left * local.y;

        bool GroupStepping(int group) { foreach (var l in legs) if (l.group == group && l.stepping) return true; return false; }

        /// <summary>Walks toward a target for one frame. Speed scales the gait; reduced keeps the body steady.</summary>
        public void Tick(float dt, Vector2 target, float speedScale, bool reduced)
        {
            clock += dt;
            // Steer: turn toward the target at a limited rate, slow down near it.
            var to = target - pos;
            float dist = to.magnitude;
            if (dist > 2)
            {
                float want = Mathf.Atan2(to.y, to.x);
                float delta = Mathf.DeltaAngle(heading * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                heading += Mathf.Clamp(delta, -turnRate * dt, turnRate * dt);
            }
            float speed = maxSpeed * speedScale * Mathf.Clamp01(dist / (40 * size)) * Mathf.Clamp01(1.2f - Mathf.Abs(Mathf.DeltaAngle(heading * Mathf.Rad2Deg, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg)) / 120f);
            var wantVel = Forward * speed;
            vel = Vector2.Lerp(vel, wantVel, 1 - Mathf.Exp(-6 * dt));
            pos += vel * dt;
            bob = reduced ? 0 : Mathf.Sin(clock * 14) * .8f * Mathf.Clamp01(vel.magnitude / (20 * size));

            float stride = 22 * size;
            // Faster walking: quicker steps.
            stepTime = Mathf.Lerp(.16f, .1f, Mathf.Clamp01(vel.magnitude / Mathf.Max(1, maxSpeed)));
            foreach (var l in legs)
            {
                l.hipWorld = ToWorld(l.hip);
                if (l.stepping)
                {
                    l.t += dt / stepTime;
                    if (l.t >= 1) { l.t = 1; l.stepping = false; l.foot = l.to; Planted?.Invoke(this, l); }
                    else
                    {
                        float k = l.t * l.t * (3 - 2 * l.t);
                        l.foot = Vector2.Lerp(l.from, l.to, k) + Left * l.side * Mathf.Sin(l.t * Mathf.PI) * 5 * size;
                    }
                }
                else
                {
                    var rest = ToWorld(l.rest);
                    float off = Vector2.Distance(l.foot, rest);
                    bool tooFar = off > stride * 2.2f;
                    if (Settled && !tooFar && off < stride * 1.6f) { l.knee = Knee(l.hipWorld, l.foot, l.side); continue; }
                    if ((off > stride && !GroupStepping(1 - l.group)) || tooFar) BeginStep(l, rest);
                }
                l.knee = Knee(l.hipWorld, l.foot, l.side);
            }
        }

        /// <summary>Lifts a planted leg and puts it down on a given spot (a tap on a word while it watches).</summary>
        public void StepTo(Leg l, Vector2 to, int word)
        {
            if (l == null || l.stepping) return;
            l.from = l.foot; l.to = to; l.t = 0; l.stepping = true; l.word = word;
        }

        /// <summary>Stand still: legs only step when the body has really moved away from them.</summary>
        public bool Settled;

        void BeginStep(Leg l, Vector2 rest)
        {
            // Aim ahead of the rest spot by the distance the body covers in a step and a bit.
            var aim = rest + vel * (stepTime * 2.2f);
            l.from = l.foot; l.t = 0; l.stepping = true;
            l.word = -1;
            l.to = aim;
            if (FindFoothold != null)
            {
                int w = FindFoothold(aim, 18 * size, out var at);
                if (w >= 0) { l.word = w; l.to = at; }
            }
        }

        /// <summary>Knee of a two-segment leg from hip to foot, bent outward (toward <paramref name="side"/>).</summary>
        Vector2 Knee(Vector2 hip, Vector2 foot, float side)
        {
            var d = foot - hip;
            float len = d.magnitude;
            if (len < 1e-3f) return hip + Left * side * upper;
            float reach = Mathf.Clamp(len, Mathf.Abs(upper - lower) + .01f, upper + lower - .01f);
            float cosA = (upper * upper + reach * reach - lower * lower) / (2 * upper * reach);
            float a = Mathf.Acos(Mathf.Clamp(cosA, -1, 1));
            float baseAngle = Mathf.Atan2(d.y, d.x);
            var k1 = hip + new Vector2(Mathf.Cos(baseAngle + a), Mathf.Sin(baseAngle + a)) * upper;
            var k2 = hip + new Vector2(Mathf.Cos(baseAngle - a), Mathf.Sin(baseAngle - a)) * upper;
            var outward = Left * side;
            return Vector2.Dot(k1 - hip, outward) >= Vector2.Dot(k2 - hip, outward) ? k1 : k2;
        }

        /// <summary>Draws the walker (legs first, then the body unless hidden).</summary>
        public void Draw(VertexHelper vh, Color legColor, Color bodyColor, Color eyeColor, float lift, Color? halo = null)
        {
            var dark = legColor * .55f; dark.a = legColor.a * .5f;
            foreach (var l in legs)
            {
                if (halo.HasValue)
                {
                    // A light rim around a dark leg: readable on dark and light windows alike.
                    XgDraw.Seg(vh, l.hipWorld, l.knee, 2.4f * size + 2.2f, halo.Value);
                    XgDraw.Seg(vh, l.knee, l.foot, 1.7f * size + 2.2f, halo.Value);
                    XgDraw.Disc(vh, l.knee, 1.7f * size + 1.1f, halo.Value, 8);
                }
                else
                {
                    // A soft shadow under each leg, then the leg.
                    var shadow = new Vector2(1.5f, -1.5f) * size;
                    XgDraw.Seg(vh, l.hipWorld + shadow, l.knee + shadow, 2.6f * size, dark);
                    XgDraw.Seg(vh, l.knee + shadow, l.foot + shadow, 2f * size, dark);
                }
                XgDraw.Seg(vh, l.hipWorld, l.knee, 2.4f * size, legColor);
                XgDraw.Seg(vh, l.knee, l.foot, 1.7f * size, legColor);
                XgDraw.Disc(vh, l.knee, 1.7f * size, legColor, 8);
                XgDraw.Disc(vh, l.foot, (l.stepping ? 2.6f : 1.8f) * size, legColor, 8);
            }
            if (hideBody) return;
            float bs = size * bodyScale;
            var f = Forward; var left = Left;
            var c = pos + new Vector2(0, bob + lift);
            // A neon rim around the abdomen, then the body itself.
            var rim = eyeColor; rim.a *= .55f;
            Oval(vh, c - f * 10 * bs, f, left, 13.2f * bs, 9.7f * bs, rim);
            Oval(vh, c - f * 10 * bs, f, left, 12 * bs, 8.5f * bs, bodyColor);
            Oval(vh, c + f * 3 * bs, f, left, 7 * bs, 6 * bs, bodyColor * 1.1f);
            Oval(vh, c + f * 9 * bs, f, left, 4.5f * bs, 4 * bs, bodyColor * 1.2f);
            // A thin neon stripe along the abdomen and two eyes.
            XgDraw.Seg(vh, c - f * 18 * bs, c - f * 3 * bs, 1.4f * bs, eyeColor * new Color(1, 1, 1, .6f));
            float eye = 1.3f * bs * (1 + eyeBoost * .8f);
            if (eyeBoost > .01f)
            {
                var glow = eyeColor; glow.a *= .5f * eyeBoost;
                XgSoftDraw.Halo(vh, c + f * 11 * bs, 7 * bs * eyeBoost, glow, 12);
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
