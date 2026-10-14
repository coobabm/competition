using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The spider's "it is learning" language, shared by the resident on the desktop and the 摆渡百科 crawler page:
    /// <list type="bullet">
    /// <item>threads: a thin glowing line from every recently read word back to its abdomen, fading over a few
    /// seconds, so a web of what it has read builds up around it;</item>
    /// <item>motes: the characters of a read word break into small glyph bits that are sucked into the abdomen;</item>
    /// <item>leg pulses: a bright dot runs up the leg that touched the word, foot to knee to hip;</item>
    /// <item>digestion: the abdomen glows brighter the more it has just read, with a ring when a sample is credited;</item>
    /// <item>thinking: now and then it stops, its threads light up one after another and a faint ripple goes out;</item>
    /// <item>reach: while it watches a hesitating player, threads stretch toward the card's words.</item>
    /// </list>
    /// Colours come from the 皮层拓扑 palette (<see cref="XgCortexGraphic.N"/>). With 减少特效 there are fewer threads
    /// and motes and no ripple. Everything is drawn into the owner's mesh; nothing here takes input or allocates per frame
    /// once the pools are warm.
    /// </summary>
    public sealed class XgSpiderFx
    {
        struct Thread { public Vector2 at; public float age; public Color color; }
        struct Mote { public Vector2 from, ctrl, pos; public float t, life, size; public Color color; }
        struct Pulse { public XgSpiderWalker.Leg leg; public float t; public Color color; }

        public bool reduced;
        readonly List<Thread> threads = new List<Thread>();
        readonly List<Mote> motes = new List<Mote>();
        readonly List<Pulse> pulses = new List<Pulse>();
        readonly List<Vector2> reach = new List<Vector2>();
        float digest, ringT = -1, thinkT = -1, rippleT = -1, reachT;
        readonly System.Random rng = new System.Random();

        const float ThinkSeconds = 1.8f;
        static readonly Color[] Palette = { XgCortexGraphic.N.Teal, XgCortexGraphic.N.Accent, XgCortexGraphic.N.Purple, XgCortexGraphic.N.Gold, XgCortexGraphic.N.Good };

        int ThreadCap => reduced ? 8 : 26;
        int MoteCap => reduced ? 10 : 90;
        float ThreadLife => reduced ? 1.6f : 4.5f;

        /// <summary>Threads alive now (a thinking moment needs a few to light up).</summary>
        public int Threads => threads.Count;
        /// <summary>A thinking moment is going on.</summary>
        public bool Thinking => thinkT >= 0;
        /// <summary>0–1: how full of freshly read words it is.</summary>
        public float Digest => digest;

        public static Color Accent(System.Random r) => Palette[r.Next(Palette.Length)];

        /// <summary>A word was read: a thread to it, glyph bits flying in, a pulse up the leg, a little more glow.</summary>
        public void Read(XgSpiderWalker w, XgSpiderWalker.Leg leg, Rect word, Color color, int chars)
        {
            if (threads.Count >= ThreadCap) threads.RemoveAt(0);
            threads.Add(new Thread { at = word.center, age = 0, color = color });
            int bits = reduced ? 1 : Mathf.Clamp(chars * 2 + 1, 3, 7);
            var belly = Belly(w);
            for (int i = 0; i < bits && motes.Count < MoteCap; i++)
            {
                var from = new Vector2(Mathf.Lerp(word.xMin, word.xMax, (float)rng.NextDouble()), Mathf.Lerp(word.yMin, word.yMax, (float)rng.NextDouble()));
                var side = new Vector2(-(belly - from).y, (belly - from).x).normalized * ((float)rng.NextDouble() - .5f) * 50;
                motes.Add(new Mote { from = from, pos = from, ctrl = (from + belly) * .5f + side + Vector2.up * 12, t = -(float)rng.NextDouble() * .15f, life = .45f + (float)rng.NextDouble() * .3f, size = 1.4f + (float)rng.NextDouble() * 1.6f, color = color });
            }
            if (leg != null && !reduced) pulses.Add(new Pulse { leg = leg, t = 0, color = color });
            digest = Mathf.Min(1, digest + .18f);
        }

        /// <summary>A sample was credited: a short ring around the abdomen.</summary>
        public void Credit() { ringT = 0; }

        /// <summary>Starts a thinking moment (it stops; threads light up in turn; a ripple goes out).</summary>
        public void Think() { thinkT = 0; rippleT = reduced ? -1 : 0; }

        /// <summary>Points it stretches threads toward (the card's words while the player hesitates); empty to let go.</summary>
        public void SetReach(IList<Vector2> points)
        {
            reach.Clear();
            if (points != null) for (int i = 0; i < points.Count && i < 6; i++) reach.Add(points[i]);
        }

        public void Clear() { threads.Clear(); motes.Clear(); pulses.Clear(); reach.Clear(); digest = 0; ringT = thinkT = rippleT = -1; }

        static Vector2 Belly(XgSpiderWalker w) => w.pos + w.offset - w.Forward * 10 * w.size * w.bodyScale;

        public void Tick(float dt, XgSpiderWalker w)
        {
            float life = ThreadLife;
            for (int i = threads.Count - 1; i >= 0; i--)
            {
                var t = threads[i];
                // Threads hold while it thinks, so the web it lights up is the one it just spun.
                if (thinkT < 0) t.age += dt;
                if (t.age >= life) threads.RemoveAt(i); else threads[i] = t;
            }
            var belly = Belly(w);
            for (int i = motes.Count - 1; i >= 0; i--)
            {
                var m = motes[i];
                m.t += dt / m.life;
                if (m.t >= 1) { motes.RemoveAt(i); digest = Mathf.Min(1, digest + .02f); continue; }
                float k = Mathf.Max(0, m.t); k = k * k;
                m.pos = (1 - k) * (1 - k) * m.from + 2 * (1 - k) * k * m.ctrl + k * k * belly;
                motes[i] = m;
            }
            for (int i = pulses.Count - 1; i >= 0; i--)
            {
                var p = pulses[i]; p.t += dt / .32f;
                if (p.t >= 1) pulses.RemoveAt(i); else pulses[i] = p;
            }
            digest = Mathf.Max(0, digest - dt * .12f);
            if (ringT >= 0) { ringT += dt; if (ringT > .45f) ringT = -1; }
            if (thinkT >= 0) { thinkT += dt; if (thinkT > ThinkSeconds) thinkT = -1; }
            if (rippleT >= 0) { rippleT += dt; if (rippleT > 1.4f) rippleT = -1; }
            reachT += dt;
        }

        /// <summary>Threads and the reach, under the spider.</summary>
        public void DrawBehind(VertexHelper vh, XgSpiderWalker w, float alpha = 1)
        {
            var belly = Belly(w);
            float life = ThreadLife;
            int n = threads.Count;
            // While thinking, a bright wave runs along the threads from the oldest to the newest.
            float wave = thinkT >= 0 ? thinkT / ThinkSeconds * (n + 2) - 1 : -100;
            for (int i = 0; i < n; i++)
            {
                var t = threads[i];
                float fade = 1 - t.age / life;
                float lit = Mathf.Clamp01(1 - Mathf.Abs(wave - i) * .8f);
                var c = Color.Lerp(t.color, Color.white, lit * .6f);
                c.a = (.16f + .5f * lit) * fade * alpha;
                XgSoftDraw.Seg(vh, t.at, belly, .7f + lit, 2.2f + 2 * lit, c);
                var dot = c; dot.a = Mathf.Min(1, c.a * 2.2f);
                XgDraw.Disc(vh, t.at, 1.4f + lit * 1.6f, dot, 8);
            }
            if (reach.Count > 0)
            {
                for (int i = 0; i < reach.Count; i++)
                {
                    // Each thread grows out toward its word and sways a little.
                    float grow = Mathf.Clamp01(reachT * .8f - i * .15f);
                    var to = Vector2.Lerp(belly, reach[i], grow) + Vector2.up * Mathf.Sin(reachT * 2 + i) * 3;
                    var c = XgCortexGraphic.N.Teal; c.a = .35f * alpha;
                    XgSoftDraw.Seg(vh, belly, to, .6f, 2.4f, c);
                }
            }
            else reachT = 0;
        }

        /// <summary>Glyph bits, leg pulses, the abdomen's glow, the credit ring and the thinking ripple, over the spider's legs.</summary>
        public void DrawFront(VertexHelper vh, XgSpiderWalker w, float alpha = 1)
        {
            foreach (var m in motes)
            {
                if (m.t < 0) continue;
                var c = m.color; c.a = (1 - m.t * .6f) * alpha;
                float s = m.size * (1 - m.t * .7f);
                XgDraw.Box(vh, m.pos - new Vector2(s, s * .7f), m.pos + new Vector2(s, s * .7f), c);
            }
            foreach (var p in pulses)
            {
                var l = p.leg;
                var at = p.t < .5f ? Vector2.Lerp(l.shown, l.knee, p.t * 2) : Vector2.Lerp(l.knee, l.hipWorld, (p.t - .5f) * 2);
                var c = p.color; c.a = (1 - p.t * .5f) * alpha;
                XgSoftDraw.Halo(vh, at, 5 * w.size, c, 12);
                XgDraw.Disc(vh, at, 1.4f * w.size, Color.Lerp(c, Color.white, .5f), 8);
            }
            var belly = Belly(w);
            float glow = digest + (thinkT >= 0 ? .4f * Mathf.Sin(thinkT / ThinkSeconds * Mathf.PI) : 0);
            if (glow > .02f)
            {
                var g = XgCortexGraphic.N.Teal; g.a = Mathf.Clamp01(glow) * .55f * alpha;
                XgSoftDraw.Halo(vh, belly, (10 + 10 * glow) * w.size, g, 18);
            }
            if (ringT >= 0)
            {
                float k = ringT / .45f;
                var c = XgCortexGraphic.N.Gold; c.a = (1 - k) * .8f * alpha;
                XgSoftDraw.Ring(vh, belly, (8 + 16 * k) * w.size, 1.2f, c, 20);
            }
            if (rippleT >= 0)
            {
                float k = rippleT / 1.4f;
                var c = XgCortexGraphic.N.Accent; c.a = (1 - k) * .35f * alpha;
                XgSoftDraw.Ring(vh, w.pos, (20 + 140 * k) * w.size, 1, c, 40);
            }
        }
    }
}
