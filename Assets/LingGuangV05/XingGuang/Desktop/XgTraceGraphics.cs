using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 训练图式, the network half: the run's layers drawn as boxes in perspective (input → layers → answer), the way
    /// papers draw a network, with the wiring as connections (dense fans, receptive-field cones, loops, a bottleneck,
    /// attention lines, shortcuts). Box thickness is the share of cells a layer holds; the layer where training is stuck
    /// is drawn red. Labels are placed by the page from <see cref="Layout"/>, which is in 0–1 space.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgNetworkGraphic : MaskableGraphic
    {
        public static readonly Color Healthy = new Color32(220, 228, 250, 255), HealthyEdge = new Color32(59, 91, 219, 255);
        public static readonly Color Sick = new Color32(255, 225, 225, 255), SickEdge = new Color32(214, 48, 49, 255);
        public static readonly Color Empty = new Color32(240, 242, 246, 255), EmptyEdge = new Color32(160, 170, 188, 255);
        static readonly Color Wire = new Color32(150, 160, 182, 255), Skip = new Color32(47, 158, 68, 255), LoopColor = new Color32(240, 120, 32, 255);

        /// <summary>One drawn box in 0–1 space: its front face, its depth (thickness) and how it is coloured.</summary>
        public struct Box { public Rect front; public float thick; public int layer; public bool input, output, bottleneck, gap, sick, empty; public float relay, garble; }

        public readonly List<Box> Layout = new List<Box>();
        XgNetworkHealth health;
        /// <summary>Layers drawn at most; deeper networks show the first and last ones around a gap.</summary>
        public const int MaxDrawn = 7;

        public void SetData(XgNetworkHealth h)
        {
            health = h;
            Layout.Clear();
            if (h == null) { SetVerticesDirty(); return; }
            var drawn = new List<int>();
            if (h.depth <= MaxDrawn) for (int l = 1; l <= h.depth; l++) drawn.Add(l);
            else { for (int l = 1; l <= 3; l++) drawn.Add(l); drawn.Add(0); for (int l = h.depth - 2; l <= h.depth; l++) drawn.Add(l); }
            bool bottleneck = h.wiring == XgWiring.EncoderDecoder;
            if (bottleneck) drawn.Insert(drawn.Count / 2, -1);
            int slots = drawn.Count + 2;
            float step = .92f / slots;
            float size = Mathf.Lerp(.42f, .78f, Mathf.Clamp01(Mathf.Log(Mathf.Max(2, h.width), 2) / 9f));
            float firstRelay = h.layers.Count > 0 ? (float)h.layers[0].relay : 1, firstGarble = h.layers.Count > 0 ? (float)h.layers[0].garble : 0;
            Layout.Add(new Box { front = Centered(.04f + step * .5f, .46f, step * .55f, .56f), thick = .02f, input = true, relay = firstRelay, garble = firstGarble });
            for (int i = 0; i < drawn.Count; i++)
            {
                int l = drawn[i];
                float cx = .04f + step * (i + 1.5f);
                if (l == 0) { Layout.Add(new Box { front = Centered(cx, .46f, step * .3f, .1f), gap = true }); continue; }
                if (l < 0) { Layout.Add(new Box { front = Centered(cx, .46f, step * .22f, size * .32f), thick = .015f, bottleneck = true, relay = 1 }); continue; }
                var layer = h.layers[l - 1];
                float share = h.cap > 0 ? (float)layer.concepts / h.cap : 0;
                Layout.Add(new Box
                {
                    front = Centered(cx, .46f, step * .34f, size * .9f), thick = Mathf.Lerp(.012f, .06f, Mathf.Clamp01(share * 2)), layer = l,
                    sick = layer.problem.Length > 0, empty = layer.concepts == 0, relay = (float)layer.relay, garble = (float)layer.garble,
                });
            }
            Layout.Add(new Box { front = Centered(.04f + step * (slots - .5f), .46f, step * .18f, .34f), thick = .01f, output = true, relay = 1 });
            SetVerticesDirty();
        }

        static Rect Centered(float cx, float cy, float w, float h) => new Rect(cx - w * .5f, cy - h * .5f, w, h);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (health == null || Layout.Count < 2 || r.width <= 1 || r.height <= 1) return;
            // Connections first, boxes on top.
            for (int i = 0; i + 1 < Layout.Count; i++) Connect(vh, r, Layout[i], Layout[i + 1], i);
            if (health.wiring == XgWiring.Attention || health.wiring == XgWiring.AnyToAny)
            {
                var last = Px(r, Layout[Layout.Count - 2].front);
                for (int i = 0; i < Layout.Count - 2; i++) if (!Layout[i].gap) Curve(vh, Top(last), Top(Px(r, Layout[i].front)), r.height * .18f, 1.2f, LoopColor);
            }
            foreach (var b in Layout) DrawBox(vh, r, b);
            // Loops and shortcuts go over the boxes, so they are never hidden behind a face.
            for (int i = 1; i < Layout.Count - 1; i++) Loop(vh, r, Layout[i]);
            if (health.skip)
                for (int i = 1; i + 1 < Layout.Count - 1; i++) if (!Layout[i].gap && !Layout[i + 1].gap)
                    Curve(vh, Top(Px(r, Layout[i].front)), Top(Px(r, Layout[i + 1].front)), r.height * .08f, 2.4f, Skip);
            SignalBand(vh, r);
        }

        /// <summary>
        /// 传话 as a ribbon over the network, flowing from the first layer to the answer: its width is how much of each
        /// layer's vote is left when it arrives, its colour how garbled it is (blue clean → red noise). With skip
        /// connections the ribbon stays full and green the whole way.
        /// </summary>
        void SignalBand(VertexHelper vh, Rect r)
        {
            float y = r.yMin + r.height * .93f, maxHalf = r.height * .05f;
            Vector2 prevTop = default, prevBottom = default; Color prevColor = default; bool started = false;
            for (int i = 1; i < Layout.Count; i++)
            {
                var b = Layout[i];
                if (b.gap) continue;
                float x = Px(r, b.front).center.x;
                float half = Mathf.Max(1f, maxHalf * Mathf.Clamp01(b.relay));
                float noise = b.relay > 0 ? Mathf.Clamp01(b.garble / b.relay) : 1;
                Color c = health.skip ? Skip : Color.Lerp(HealthyEdge, SickEdge, noise);
                var top = new Vector2(x, y + half); var bottom = new Vector2(x, y - half);
                if (started) Quad(vh, prevBottom, bottom, top, prevTop, Color.Lerp(prevColor, c, .5f));
                prevTop = top; prevBottom = bottom; prevColor = c; started = true;
            }
            // Arrow head at the answer.
            if (started) Quad(vh, prevTop + new Vector2(0, maxHalf * .6f), prevTop + new Vector2(maxHalf * 1.6f, -(prevTop.y - prevBottom.y) * .5f), prevBottom - new Vector2(0, maxHalf * .6f), prevBottom, prevColor);
        }

        void Connect(VertexHelper vh, Rect r, Box a, Box b, int index)
        {
            if (a.gap || b.gap) return;
            Rect pa = Px(r, a.front), pb = Px(r, b.front);
            if (health.wiring == XgWiring.LocalShared && !b.output)
            {
                // Receptive field: a small window on this box, a cone to one point on the next (dashed).
                var win = new Rect(pa.x + pa.width * .5f, pa.y + pa.height * .45f, pa.width * .3f, pa.height * .2f);
                Outline(vh, win, 1.2f, HealthyEdge);
                var tip = new Vector2(pb.x + pb.width * .4f, pb.center.y);
                foreach (var c in new[] { new Vector2(win.xMin, win.yMin), new Vector2(win.xMax, win.yMin), new Vector2(win.xMin, win.yMax), new Vector2(win.xMax, win.yMax) })
                    Dashed(vh, c, tip, 1f, Wire);
                return;
            }
            // Dense fan: every row of one box to every row of the next.
            int n = a.input || b.output ? 3 : 4;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    Line(vh, new Vector2(pa.xMax, Mathf.Lerp(pa.yMin, pa.yMax, (i + .5f) / n)), new Vector2(pb.xMin, Mathf.Lerp(pb.yMin, pb.yMax, (j + .5f) / n)), .8f, Wire);
        }

        /// <summary>The loop of a recurrent layer: it feeds itself the previous step (gated loops get a little gate bar).</summary>
        void Loop(VertexHelper vh, Rect r, Box b)
        {
            bool loop = health.wiring == XgWiring.Recurrent || health.wiring == XgWiring.GatedRecurrent || health.wiring == XgWiring.EncoderDecoder;
            if (!loop || b.gap || b.output || b.bottleneck || b.input || b.layer <= 0) return;
            var pb = Px(r, b.front);
            var d = new Vector2(r.width * b.thick, r.width * b.thick * .8f);
            var top = Top(pb) + d * .5f;
            Curve(vh, top + new Vector2(-pb.width * .3f, 0), top + new Vector2(pb.width * .3f, 0), r.height * .1f, 2.2f, LoopColor);
            if (health.wiring == XgWiring.GatedRecurrent) Line(vh, top + new Vector2(-6, r.height * .075f), top + new Vector2(6, r.height * .075f), 3f, LoopColor);
        }

        void DrawBox(VertexHelper vh, Rect r, Box b)
        {
            var f = Px(r, b.front);
            if (b.gap)
            {
                for (int i = 0; i < 3; i++) Dot(vh, new Vector2(f.x + f.width * (i + .5f) / 3, f.center.y), 2.5f, EmptyEdge);
                return;
            }
            // Boxes fade with what is left of their votes at the answer (传话).
            Color fill = b.input ? Empty : b.sick ? Sick : b.empty ? Empty : Color.Lerp(Empty, Healthy, Mathf.Clamp01(.25f + b.relay));
            Color edge = b.input ? EmptyEdge : b.sick ? SickEdge : b.empty ? EmptyEdge : HealthyEdge;
            if (b.output)
            {
                Dot(vh, new Vector2(f.center.x, f.yMax - f.height * .25f), f.width * .45f, HealthyEdge);
                Dot(vh, new Vector2(f.center.x, f.yMin + f.height * .25f), f.width * .45f, HealthyEdge);
                return;
            }
            // Perspective box: front face, then the top and the right side shifted up-right by the thickness.
            var d = new Vector2(r.width * b.thick, r.width * b.thick * .8f);
            var p0 = new Vector2(f.xMin, f.yMin); var p1 = new Vector2(f.xMax, f.yMin); var p2 = new Vector2(f.xMax, f.yMax); var p3 = new Vector2(f.xMin, f.yMax);
            Quad(vh, p3, p2, p2 + d, p3 + d, Color.Lerp(fill, Color.white, .35f));
            Quad(vh, p1, p1 + d, p2 + d, p2, Color.Lerp(fill, edge, .15f));
            Quad(vh, p0, p1, p2, p3, fill);
            float w = b.sick ? 2.4f : 1.4f;
            Line(vh, p0, p1, w, edge); Line(vh, p1, p2, w, edge); Line(vh, p2, p3, w, edge); Line(vh, p3, p0, w, edge);
            Line(vh, p3, p3 + d, w, edge); Line(vh, p2, p2 + d, w, edge); Line(vh, p1, p1 + d, w, edge);
            Line(vh, p3 + d, p2 + d, w, edge); Line(vh, p2 + d, p1 + d, w, edge);
            if (b.input)
                for (int i = 1; i < 4; i++) { Line(vh, new Vector2(f.xMin, Mathf.Lerp(f.yMin, f.yMax, i / 4f)), new Vector2(f.xMax, Mathf.Lerp(f.yMin, f.yMax, i / 4f)), .8f, EmptyEdge); }
        }

        static Rect Px(Rect r, Rect n) => new Rect(r.x + n.x * r.width, r.y + n.y * r.height, n.width * r.width, n.height * r.height);
        static Vector2 Top(Rect p) => new Vector2(p.center.x, p.yMax);
        static Vector2 Bottom(Rect p) => new Vector2(p.center.x, p.yMin);

        static void Outline(VertexHelper vh, Rect p, float w, Color c)
        {
            Line(vh, new Vector2(p.xMin, p.yMin), new Vector2(p.xMax, p.yMin), w, c); Line(vh, new Vector2(p.xMax, p.yMin), new Vector2(p.xMax, p.yMax), w, c);
            Line(vh, new Vector2(p.xMax, p.yMax), new Vector2(p.xMin, p.yMax), w, c); Line(vh, new Vector2(p.xMin, p.yMax), new Vector2(p.xMin, p.yMin), w, c);
        }

        static void Dashed(VertexHelper vh, Vector2 a, Vector2 b, float w, Color c)
        {
            float len = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.FloorToInt(len / 9f));
            for (int i = 0; i < n; i += 2) Line(vh, Vector2.Lerp(a, b, (float)i / n), Vector2.Lerp(a, b, Mathf.Min(1, (i + 1f) / n)), w, c);
        }

        /// <summary>A bow from a to b bulging by <paramref name="bulge"/> pixels (negative bows downward).</summary>
        static void Curve(VertexHelper vh, Vector2 a, Vector2 b, float bulge, float w, Color c)
        {
            var mid = (a + b) * .5f + new Vector2(0, bulge);
            var prev = a;
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                var p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * mid + t * t * b;
                Line(vh, prev, p, w, c); prev = p;
            }
        }

        static void Dot(VertexHelper vh, Vector2 c, float radius, Color color)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            v.position = c; vh.AddVert(v);
            for (int i = 0; i <= 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2;
                v.position = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; vh.AddVert(v);
                if (i > 0) vh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            v.position = a; vh.AddVert(v); v.position = b; vh.AddVert(v); v.position = c; vh.AddVert(v); v.position = d; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        public static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            Quad(vh, a - n, a + n, b + n, b - n, color);
        }
    }

    /// <summary>
    /// 训练图式, the time half: test and training accuracy per epoch on the board's scale, the wall's line, the cells in
    /// use as a band along the bottom, and a vertical mark at every epoch the trace noted something (red = a problem,
    /// grey = a settings change, purple = a phenomenon).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgTraceGraphic : MaskableGraphic
    {
        static readonly Color Grid = new Color32(214, 222, 235, 255), Band = new Color32(228, 232, 240, 255), BandFull = new Color32(255, 214, 214, 255);
        static readonly Color Change = new Color32(150, 160, 182, 255), Phenomenon = new Color32(132, 94, 247, 255);
        XgTrace trace;
        float goal;
        /// <summary>Epoch span on screen (first and last point), for the page's axis labels.</summary>
        public int FromEpoch { get; private set; }
        public int ToEpoch { get; private set; }

        public void SetData(XgTrace t, float goalLine)
        {
            trace = t; goal = goalLine;
            FromEpoch = t != null && t.points.Count > 0 ? t.points[0].epoch : 0;
            ToEpoch = t != null && t.points.Count > 0 ? t.points[t.points.Count - 1].epoch : 0;
            SetVerticesDirty();
        }

        /// <summary>Accuracies are drawn from coin (50%) to 100%.</summary>
        float Y(Rect r, float acc) => r.yMin + r.height * (.18f + .82f * Mathf.Clamp01((acc - .5f) / .5f));
        float X(Rect r, int epoch) => ToEpoch <= FromEpoch ? r.xMax : r.xMin + r.width * (epoch - FromEpoch) / (float)(ToEpoch - FromEpoch);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width <= 1 || r.height <= 1) return;
            for (int i = 0; i <= 4; i++) XgNetworkGraphic.Line(vh, new Vector2(r.xMin, Y(r, .5f + i * .125f)), new Vector2(r.xMax, Y(r, .5f + i * .125f)), 1, Grid);
            if (trace == null || trace.points.Count == 0) return;
            var pts = trace.points;
            // Cells band: the bottom 14% fills with the share of cells in use, red where they are all taken.
            for (int i = 0; i < pts.Count; i++)
            {
                float x0 = i == 0 ? X(r, pts[i].epoch) - 1 : X(r, pts[i - 1].epoch), x1 = X(r, pts[i].epoch) + (pts.Count == 1 ? 1 : 0);
                float share = pts[i].cap > 0 ? Mathf.Clamp01((float)pts[i].cells / pts[i].cap) : 0;
                var c = share >= 1 ? BandFull : Band;
                XgNetworkGraphic.Line(vh, new Vector2(Mathf.Lerp(x0, x1, .5f), r.yMin), new Vector2(Mathf.Lerp(x0, x1, .5f), r.yMin + r.height * .14f * share), Mathf.Max(1, x1 - x0 + .5f), c);
            }
            if (goal > .5f)
                for (float x = r.xMin; x < r.xMax; x += 12) XgNetworkGraphic.Line(vh, new Vector2(x, Y(r, goal)), new Vector2(Mathf.Min(r.xMax, x + 7), Y(r, goal)), 2, XgChartGraphic.BestColor);
            foreach (var e in trace.events)
            {
                if (e.epoch < FromEpoch) continue;
                var c = XgSim.TraceEventIsProblem(e) ? XgNetworkGraphic.SickEdge : e.id == "change" ? Change : Phenomenon;
                XgNetworkGraphic.Line(vh, new Vector2(X(r, e.epoch), r.yMin), new Vector2(X(r, e.epoch), r.yMax), e.id == "change" ? 1.2f : 1.8f, new Color(c.r, c.g, c.b, .8f));
            }
            for (int i = 1; i < pts.Count; i++)
            {
                XgNetworkGraphic.Line(vh, new Vector2(X(r, pts[i - 1].epoch), Y(r, pts[i - 1].train)), new Vector2(X(r, pts[i].epoch), Y(r, pts[i].train)), 1.6f, XgChartGraphic.TrainColor);
                XgNetworkGraphic.Line(vh, new Vector2(X(r, pts[i - 1].epoch), Y(r, pts[i - 1].test)), new Vector2(X(r, pts[i].epoch), Y(r, pts[i].test)), 2.6f, XgChartGraphic.ValColor);
            }
        }
    }
}
