using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Plays the spider's behaviour (<see cref="XgSpiderBrain"/>) on a <see cref="XgSpiderWalker"/>: each frame it turns the
    /// current action into a body pose and leg poses (<see cref="XgSpiderPoses"/>), eases the walker toward them so
    /// that actions begin and end smoothly (and an interrupted one relaxes instead of snapping), and draws the little
    /// extras that belong to an action (the thread it hangs on, the sleeping "z"s, the startle mark). Shared by the
    /// resident on the desktop and the 摆渡百科 crawler. Nothing here allocates per frame.
    /// </summary>
    public sealed class XgSpiderMotion
    {
        public readonly XgSpiderBrain brain;
        float yaw, scale = 1, liftX, liftY, eyeOpen = 1, eyeBoost, thread, travel;
        float[] lw = new float[8], ls = new float[8], lf = new float[8], ll = new float[8];
        Vector2 anchor;
        bool hanging;

        public XgSpiderMotion(System.Random rng) { brain = new XgSpiderBrain(rng); for (int i = 0; i < ls.Length; i++) ls[i] = 1; }

        /// <summary>Multiplier for the body scale (crouching, stretching, the jolt of a startle).</summary>
        public float Scale => scale;
        /// <summary>Extra eye brightness the current action wants (0–1).</summary>
        public float EyeBoost => eyeBoost;

        static float Rate(float dt, float rate) => 1 - Mathf.Exp(-rate * dt);

        /// <summary>Eases the walker toward the pose of the current action (or the neutral pose when there is none).</summary>
        public void Apply(float dt, XgSpiderWalker w, bool reduced)
        {
            if (w == null) return;
            brain.Reduced = reduced;
            var act = brain.Act;
            var b = act == XgSpiderAct.None
                ? new XgBodyPose { scale = 1, eyeOpen = 1 }
                : XgSpiderPoses.Body(act, brain.ActT, brain.ActSeconds, brain.Variant, reduced);
            float k = Rate(dt, 12);
            if (act == XgSpiderAct.Dance) yaw = (float)b.yaw;
            else yaw += Mathf.DeltaAngle(yaw * Mathf.Rad2Deg, (float)b.yaw * Mathf.Rad2Deg) * Mathf.Deg2Rad * k;
            scale += ((float)b.scale - scale) * k;
            liftX += ((float)b.sway - liftX) * k;
            // A skipping gait leaves the page a little at each skip.
            float skip = brain.Resting ? 0 : (float)brain.TravelLift;
            travel += (skip - travel) * Rate(dt, 18);
            float liftTarget = (float)b.lift + travel * 7;
            liftY += (liftTarget - liftY) * k;
            eyeOpen += ((float)b.eyeOpen - eyeOpen) * Rate(dt, act == XgSpiderAct.Sleep ? 3 : 10);
            eyeBoost += ((float)b.eyeBoost - eyeBoost) * k;
            thread += ((float)b.thread - thread) * k;

            if (act == XgSpiderAct.Dangle && !hanging) { anchor = w.pos + new Vector2(0, 18 * w.size); hanging = true; }
            else if (act != XgSpiderAct.Dangle && thread < .02f) hanging = false;

            float s = w.size;
            w.yaw = yaw;
            w.offset = new Vector2(liftX, liftY) * s;
            w.eyeOpen = eyeOpen;
            w.eyeExtra = eyeBoost;
            w.bodyScale = scale + travel * .06f;
            int n = Mathf.Min(w.legs.Length, lw.Length);
            for (int i = 0; i < n; i++)
            {
                var p = act == XgSpiderAct.None ? new XgLegPose { scale = 1 } : XgSpiderPoses.Leg(i, w.legs.Length, act, brain.ActT, brain.ActSeconds, brain.Variant, reduced);
                lw[i] += ((float)p.w - lw[i]) * k;
                ls[i] += ((float)p.scale - ls[i]) * k;
                lf[i] += ((float)p.fwd - lf[i]) * k;
                ll[i] += ((float)p.lat - ll[i]) * k;
                w.SetLegPose(i, lw[i], ls[i], lf[i], ll[i]);
            }
        }

        /// <summary>The thread a dangling spider hangs on, under it.</summary>
        public void DrawBehind(VertexHelper vh, XgSpiderWalker w, float alpha)
        {
            if (thread < .02f || !hanging) return;
            var body = w.pos + w.offset;
            var rim = new Color(214 / 255f, 238 / 255f, 245 / 255f, .8f * alpha * thread);
            var core = new Color(30 / 255f, 50 / 255f, 60 / 255f, .9f * alpha * thread);
            XgDraw.Seg(vh, anchor, body, 1.4f, rim);
            XgDraw.Seg(vh, anchor, body, .6f, core);
            XgDraw.Disc(vh, anchor, 1.8f, rim, 8);
        }

        /// <summary>Sleeping "z"s drifting up, and the "!" of a startle.</summary>
        public void DrawFront(VertexHelper vh, XgSpiderWalker w, float alpha)
        {
            var act = brain.Act;
            float s = w.size;
            var body = w.pos + w.offset;
            if (act == XgSpiderAct.Sleep && eyeOpen < .5f)
            {
                var ink = XgDark.Link;
                for (int i = 0; i < 3; i++)
                {
                    float ph = Mathf.Repeat((float)brain.ActElapsed * .35f + i / 3f, 1);
                    float size = (2.2f + 2.2f * ph) * s;
                    var at = body + new Vector2(10 * s + ph * 12 * s + Mathf.Sin(ph * 6 + i) * 2, 10 * s + ph * 26 * s);
                    ink.a = Mathf.Sin(ph * Mathf.PI) * .85f * alpha;
                    var a = at + new Vector2(-size, size); var bb = at + new Vector2(size, size);
                    var c = at + new Vector2(-size, -size); var d = at + new Vector2(size, -size);
                    XgDraw.Seg(vh, a, bb, 1.1f, ink); XgDraw.Seg(vh, bb, c, 1.1f, ink); XgDraw.Seg(vh, c, d, 1.1f, ink);
                }
            }
            else if (act == XgSpiderAct.Startle && brain.ActT < .7f)
            {
                var ink = XgDark.Hot;
                float k = (float)brain.ActT;
                ink.a = (1 - k / .7f) * alpha;
                var top = body + new Vector2(0, 30 * s + 6 * Mathf.Sin(k * 20) * (1 - k));
                XgDraw.Seg(vh, top, top + new Vector2(0, 9 * s), 2f, ink);
                XgDraw.Disc(vh, top - new Vector2(0, 3 * s), 1.5f, ink, 8);
            }
        }
    }
}
