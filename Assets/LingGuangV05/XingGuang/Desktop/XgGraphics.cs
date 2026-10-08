using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Training curve: train / validation accuracy history plus the deployed checkpoint line.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgChartGraphic : MaskableGraphic
    {
        public static readonly Color TrainColor = new Color32(93, 202, 165, 255);
        public static readonly Color ValColor = new Color32(239, 159, 39, 255);
        public static readonly Color BestColor = new Color32(169, 155, 242, 255);
        static readonly Color Grid = new Color32(27, 45, 59, 255);

        readonly List<float> train = new List<float>(), val = new List<float>();
        float best, ceiling;
        Color ceilingColor = BestColor;
        float highlight;
        bool lossMode;
        /// <summary>Draws a red dot on every round that went back (the training page).</summary>
        public bool MarkDrops;
        static readonly Color DropColor = new Color32(226, 75, 74, 255);
        public float Min { get; private set; } = 0;
        public float Max { get; private set; } = 1;
        public int Capacity = 160;

        /// <summary>The model's ceiling as a dashed line (an accuracy; 0 = none). Set before <see cref="SetData"/> so the range includes it.</summary>
        public void SetCeiling(float acc, Color color) { ceiling = acc; ceilingColor = color; }

        public void SetData(List<float> trainAcc, List<float> valAcc, float bestAcc, bool simulatedLoss = false)
        {
            lossMode = simulatedLoss;
            train.Clear(); foreach (float v in trainAcc) train.Add(lossMode ? -Mathf.Log(Mathf.Max(.0001f, v)) : v);
            val.Clear(); foreach (float v in valAcc) val.Add(lossMode ? -Mathf.Log(Mathf.Max(.0001f, v)) : v);
            best = lossMode ? 0 : bestAcc;
            float lo = float.PositiveInfinity, hi = float.NegativeInfinity;
            foreach (var v in train) { lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
            foreach (var v in val) { lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
            if (best > 0) { lo = Mathf.Min(lo, best); hi = Mathf.Max(hi, best); }
            if (ceiling > 0) { float c = Ceiling(); lo = Mathf.Min(lo, c); hi = Mathf.Max(hi, c); }
            if (hi < lo) { lo = 0; hi = 1; }
            // Zoom in as the curve flattens, so 98.7% → 99.1% is still visible.
            float span = Mathf.Max(.02f, hi - lo);
            Min = Mathf.Max(0, Mathf.Floor((lo - span * .15f) * 100) / 100);
            Max = Mathf.Ceil((hi + span * .1f) * 100) / 100;
            if (!lossMode) { Min = Mathf.Clamp01(Min); Max = Mathf.Clamp01(Max); }
            if (Max - Min < .02f) Min = Mathf.Max(0, Max - .02f);
            SetVerticesDirty();
        }

        float Ceiling() => lossMode ? -Mathf.Log(Mathf.Max(.0001f, ceiling)) : ceiling;

        public void PulseLastSegment(float amount) { highlight = Mathf.Clamp01(amount); SetVerticesDirty(); }
        void Update() { if (highlight <= 0) return; highlight = Mathf.Max(0, highlight - Time.unscaledDeltaTime * 1.5f); SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width <= 1 || r.height <= 1) return;
            for (int i = 0; i <= 4; i++)
            {
                float y = r.yMin + r.height * i / 4f;
                Line(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), 1, Grid);
            }
            if (best > 0 && best >= Min && best <= Max)
            {
                float y = Y(r, best);
                for (float x = r.xMin; x < r.xMax; x += 12) Line(vh, new Vector2(x, y), new Vector2(Mathf.Min(r.xMax, x + 7), y), 2, BestColor);
            }
            if (ceiling > 0 && Ceiling() >= Min && Ceiling() <= Max)
            {
                float y = Y(r, Ceiling());
                for (float x = r.xMin; x < r.xMax; x += 10) Line(vh, new Vector2(x, y), new Vector2(Mathf.Min(r.xMax, x + 5), y), 1.5f, ceilingColor);
            }
            Poly(vh, r, train, 2.5f, TrainColor);
            Poly(vh, r, val, 3f, ValColor);
            if (MarkDrops) { Drops(vh, r, train); Drops(vh, r, val); }
            if (highlight > 0 && train.Count >= 2)
            {
                float step = r.width / Mathf.Max(1, Capacity - 1);
                Line(vh, new Vector2(r.xMax - step, Y(r, train[train.Count - 2])), new Vector2(r.xMax, Y(r, train[train.Count - 1])), 3 + highlight * 5, new Color(1, .72f, .1f, highlight));
            }
        }

        /// <summary>Red dots where the series went back (accuracy fell, or the loss rose).</summary>
        void Drops(VertexHelper vh, Rect r, List<float> data)
        {
            if (data.Count < 2) return;
            float step = r.width / Mathf.Max(1, Capacity - 1);
            float x0 = r.xMax - step * (data.Count - 1);
            float minStep = lossMode ? .002f : .0005f;
            for (int i = 1; i < data.Count; i++)
            {
                float change = lossMode ? data[i] - data[i - 1] : data[i - 1] - data[i];
                if (change > minStep) Disc(vh, new Vector2(x0 + step * i, Y(r, data[i])), 3.5f, DropColor);
            }
        }

        static void Disc(VertexHelper vh, Vector2 c, float radius, Color color)
        {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            v.position = c; vh.AddVert(v);
            for (int k = 0; k <= 10; k++)
            {
                float a = k / 10f * Mathf.PI * 2;
                v.position = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; vh.AddVert(v);
            }
            for (int k = 1; k <= 10; k++) vh.AddTriangle(i, i + k, i + k + 1);
        }

        float Y(Rect r, float v) { return r.yMin + r.height * Mathf.InverseLerp(Min, Max, v); }

        void Poly(VertexHelper vh, Rect r, List<float> data, float width, Color color)
        {
            if (data.Count < 2) return;
            float step = r.width / Mathf.Max(1, Capacity - 1);
            float x0 = r.xMax - step * (data.Count - 1);
            for (int i = 1; i < data.Count; i++)
                Line(vh, new Vector2(x0 + step * (i - 1), Y(r, data[i - 1])), new Vector2(x0 + step * i, Y(r, data[i])), width, color);
        }

        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            v.position = a - n; vh.AddVert(v);
            v.position = a + n; vh.AddVert(v);
            v.position = b + n; vh.AddVert(v);
            v.position = b - n; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    /// <summary>
    /// Bounded teaching diagrams of the simulation's six capability stages, not invented Qwen internals.
    /// Largest mesh is a 28×28 field (3,136 vertices); no textures, model requests or simulation mutations.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgStageGraphic : MaskableGraphic
    {
        int stage, modelDepth, epoch;
        bool vision;
        float accuracy, utilization, progress;
        string arch = "";
        static readonly Color Ink = new Color32(59, 91, 219, 255), Good = new Color32(40, 165, 130, 255), Dim = new Color32(194, 202, 218, 255);

        public void Show(XgSim sim, XgRun run, int shownStage)
        {
            stage = Mathf.Clamp(shownStage, 1, 6); modelDepth = run.depth; epoch = run.epoch; arch = run.arch;
            vision = run.track == (int)XgTrack.Vision; accuracy = (float)run.valAcc;
            utilization = (float)sim.GpuUtilization(run); progress = (float)run.epochProgress;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width < 4 || r.height < 4) return;
            switch (stage)
            {
                case 1: Weights(vh, r); break;
                case 2: Network(vh, r); break;
                case 3: if (vision) SpatialAndDepth(vh, r, false); else Memory(vh, r, false); break;
                case 4: if (vision) SpatialAndDepth(vh, r, true); else Memory(vh, r, true); break;
                case 5: Attention(vh, r); break;
                default: Transformer(vh, r); break;
            }
        }

        void Weights(VertexHelper vh, Rect r)
        {
            float size = Mathf.Min(r.height, r.width * .62f), cell = size / 28;
            var start = new Vector2(r.xMin, r.center.y - size * .5f);
            for (int y = 0; y < 28; y++) for (int x = 0; x < 28; x++)
            {
                // Intentionally labelled teaching field: visualizes positive/negative feature weights.
                float w = Mathf.Sin(x * .35f + epoch * .3f) * Mathf.Cos(y * .3f - epoch * .12f) * Mathf.Clamp01(.15f + accuracy);
                var c = Color.Lerp(Color.white, w < 0 ? new Color32(240, 130, 110, 255) : Ink, Mathf.Abs(w));
                var p = start + new Vector2(x, y) * cell;
                XgDraw.Box(vh, p, p + Vector2.one * (cell * .9f), c);
            }
            float x0 = r.xMin + size + 8, width = Mathf.Max(2, r.xMax - x0);
            for (int i = 0; i < 6; i++)
            {
                float value = .2f + .8f * Mathf.Abs(Mathf.Sin(epoch * .4f + i * 1.7f));
                float y = r.yMin + i * r.height / 6;
                XgDraw.Box(vh, new Vector2(x0, y), new Vector2(x0 + width * value, y + r.height / 9), i % 2 == 0 ? Ink : Good);
            }
        }

        void Network(VertexHelper vh, Rect r)
        {
            int hidden = Mathf.Clamp(modelDepth, 1, 3), columns = hidden + 2;
            for (int layer = 0; layer < columns; layer++)
            {
                int count = layer == columns - 1 ? 2 : 4;
                float x = Mathf.Lerp(r.xMin + 6, r.xMax - 6, layer / (float)(columns - 1));
                for (int node = 0; node < count; node++)
                {
                    var p = new Vector2(x, Mathf.Lerp(r.yMin + 6, r.yMax - 6, (node + .5f) / count));
                    if (layer > 0)
                        for (int from = 0; from < 4; from++)
                        {
                            var q = new Vector2(Mathf.Lerp(r.xMin + 6, r.xMax - 6, (layer - 1f) / (columns - 1)), Mathf.Lerp(r.yMin + 6, r.yMax - 6, (from + .5f) / 4));
                            XgDraw.Seg(vh, q, p, 1, new Color(.35f, .45f, .8f, .22f + .45f * progress));
                        }
                    XgDraw.Disc(vh, p, Mathf.Min(5, r.height / 15), layer == columns - 1 ? Good : Ink, 12);
                }
            }
        }

        void SpatialAndDepth(VertexHelper vh, Rect r, bool residual)
        {
            float tile = Mathf.Min(r.height * .65f, r.width * .23f), step = tile / 5;
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
            {
                var p = new Vector2(r.xMin + x * step, r.center.y - tile * .5f + y * step);
                XgDraw.Box(vh, p, p + Vector2.one * (step - 1), x == y || x + y == 4 ? Ink : Dim);
            }
            var plot = new Rect(r.xMin + tile + 12, r.yMin + 3, Mathf.Max(4, r.width - tile - 15), r.height - 6);
            XgDraw.Seg(vh, plot.min, new Vector2(plot.xMax, plot.yMin), 1, Dim);
            XgDraw.Seg(vh, plot.min, new Vector2(plot.xMin, plot.yMax), 1, Dim);
            Vector2 before = default;
            for (int d = 1; d <= 32; d++)
            {
                // The degradation penalty is exactly the simulation's -3 percentage points after depth 12.
                float value = residual ? Mathf.Min(.95f, .6f + d * .01f) : Mathf.Max(0, .88f - Mathf.Max(0, d - 12) * .03f);
                var p = new Vector2(Mathf.Lerp(plot.xMin, plot.xMax, (d - 1f) / 31), plot.yMin + value * plot.height);
                if (d > 1) XgDraw.Seg(vh, before, p, 2, residual ? Good : Ink);
                if (d == Mathf.Clamp(modelDepth, 1, 32)) XgDraw.Disc(vh, p, 3, XgPalette.Gold, 12);
                before = p;
            }
        }

        void Memory(VertexHelper vh, Rect r, bool gated)
        {
            for (int i = 0; i < 16; i++)
            {
                float x = r.xMin + r.width * i / 16;
                float a = Mathf.Pow(gated ? .98f : .9f, i);
                XgDraw.Box(vh, new Vector2(x, r.yMin + r.height * .35f), new Vector2(x + r.width / 16 - 1, r.yMax), Color.Lerp(Color.white, gated ? Good : Ink, a));
                XgDraw.Box(vh, new Vector2(x, r.yMin), new Vector2(x + r.width / 16 - 1, r.yMin + r.height * .25f), Color.Lerp(Color.white, Dim, Mathf.Pow(.9f, i)));
            }
        }

        void Attention(VertexHelper vh, Rect r)
        {
            int n = 5;
            for (int row = 0; row < n; row++) for (int col = 0; col < n; col++)
            {
                float w = row == col ? .95f : ((row + col) % 3 == 0 ? .35f : .08f);
                var min = new Vector2(r.xMin + col * r.width / n, r.yMin + row * r.height / n);
                XgDraw.Box(vh, min + Vector2.one, min + new Vector2(r.width / n - 1, r.height / n - 1), Color.Lerp(Color.white, Ink, w));
            }
            // Utilization shown separately by text, explicitly as game simulation, never GPU telemetry.
            XgDraw.Box(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + r.width * utilization, r.yMin + 3), Good);
        }

        void Transformer(VertexHelper vh, Rect r)
        {
            int n = 6;
            for (int head = 0; head < 3; head++)
            {
                var c = head == 0 ? Ink : head == 1 ? Good : XgPalette.Gold;
                float y = Mathf.Lerp(r.yMin + 5, r.yMax - 5, head / 2f);
                for (int token = 0; token < n; token++)
                {
                    var p = new Vector2(Mathf.Lerp(r.xMin + 5, r.xMax - 5, token / 5f), y);
                    XgDraw.Disc(vh, p, 3, c, 10);
                    for (int next = token + 1; next < n; next++)
                        XgDraw.Seg(vh, p, new Vector2(Mathf.Lerp(r.xMin + 5, r.xMax - 5, next / 5f), y + (head == 1 ? -3 : 3)), .7f, new Color(c.r, c.g, c.b, .35f));
                }
            }
        }
    }

    /// <summary>星光 icon: indigo tile, a four-point star over a small three-layer network.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgIconGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float u = Mathf.Min(r.width, r.height);
            if (u <= 0) return;
            Vector2 c = r.center;
            Rounded(vh, c, u * .48f, u * .11f, new Color32(126, 148, 255, 255), new Color32(36, 40, 120, 255));
            Rounded(vh, c, u * .425f, u * .09f, new Color32(64, 84, 196, 255), new Color32(24, 26, 82, 255));
            Color wire = new Color(.75f, .82f, 1f, .55f);
            Vector2[] left = { c + new Vector2(-.27f, .17f) * u, c + new Vector2(-.27f, -.17f) * u };
            Vector2[] mid = { c + new Vector2(0, .24f) * u, c + new Vector2(0, 0) * u, c + new Vector2(0, -.24f) * u };
            Vector2 right = c + new Vector2(.27f, 0) * u;
            foreach (var a in left) foreach (var b in mid) Seg(vh, a, b, u * .022f, wire);
            foreach (var b in mid) Seg(vh, b, right, u * .022f, wire);
            foreach (var p in left) Disc(vh, p, u * .045f, new Color32(190, 205, 255, 255));
            foreach (var p in mid) Disc(vh, p, u * .04f, new Color32(190, 205, 255, 255));
            Disc(vh, right, u * .05f, new Color32(190, 205, 255, 255));
            Vector2 s = c + new Vector2(.12f, .14f) * u;
            Disc(vh, s, u * .17f, new Color(1, .9f, .5f, .18f));
            Star(vh, s, u * .2f, u * .05f, new Color32(255, 236, 150, 255));
            Disc(vh, s, u * .035f, Color.white);
        }

        static void Star(VertexHelper vh, Vector2 c, float outer, float inner, Color color)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color; v.position = c; vh.AddVert(v);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4 + Mathf.PI / 2;
                float rad = i % 2 == 0 ? outer : inner;
                v.position = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad; vh.AddVert(v);
            }
            for (int i = 0; i < 8; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 8);
        }

        static void Disc(VertexHelper vh, Vector2 c, float radius, Color color)
        {
            int start = vh.currentVertCount, n = 20;
            var v = UIVertex.simpleVert; v.color = color; v.position = c; vh.AddVert(v);
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.position = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; vh.AddVert(v); }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % n);
        }

        static void Seg(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a; Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            v.position = a - n; vh.AddVert(v); v.position = a + n; vh.AddVert(v);
            v.position = b + n; vh.AddVert(v); v.position = b - n; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>Rounded square with a vertical gradient (fan around the centre).</summary>
        static void Rounded(VertexHelper vh, Vector2 c, float half, float radius, Color top, Color bottom)
        {
            var pts = new List<Vector2>();
            Vector2[] corners = { new Vector2(half - radius, half - radius), new Vector2(-half + radius, half - radius), new Vector2(-half + radius, -half + radius), new Vector2(half - radius, -half + radius) };
            for (int k = 0; k < 4; k++)
                for (int i = 0; i <= 6; i++)
                {
                    float a = (k * 90 + i * 15) * Mathf.Deg2Rad;
                    pts.Add(c + corners[k] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = Color.Lerp(bottom, top, .5f); v.position = c; vh.AddVert(v);
            foreach (var p in pts) { v.position = p; v.color = Color.Lerp(bottom, top, Mathf.InverseLerp(c.y - half, c.y + half, p.y)); vh.AddVert(v); }
            for (int i = 0; i < pts.Count; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % pts.Count);
        }
    }
}

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// A procedurally "handwritten" digit for the 标注台. Each digit has several ways of writing it (a 1 with or without
    /// a flag and foot, an open or closed 4, a crossed 7, a looped 2 …), drawn as smooth pen strokes. The card seed
    /// picks the style and how messy the hand is: some cards are neat, some lean, wobble, squash or overshoot,
    /// and the share of messy ones grows with the desk level.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgDigitGraphic : MaskableGraphic
    {
        // Variants per digit; each variant is one or more pen strokes of (x, y) pairs in a unit box, y up.
        static readonly float[][][][] Styles =
        {
            new[] { // 0: generated ellipses (see Zero); entries only count the variants
                new[] { new float[0] }, new[] { new float[0] }, new[] { new float[0] },
            },
            new[] { // 1
                new[] { new[] { .42f, .78f, .55f, .9f, .55f, .1f } },
                new[] { new[] { .52f, .9f, .48f, .1f } },
                new[] { new[] { .4f, .76f, .56f, .9f, .56f, .1f }, new[] { .44f, .1f, .68f, .1f } },
                new[] { new[] { .42f, .76f, .53f, .86f, .6f, .92f, .57f, .5f, .53f, .1f } },
            },
            new[] { // 2
                new[] { new[] { .25f, .72f, .35f, .86f, .55f, .9f, .72f, .8f, .72f, .62f, .25f, .12f, .78f, .12f } },
                new[] { new[] { .28f, .78f, .45f, .9f, .66f, .86f, .7f, .66f, .5f, .42f, .26f, .12f, .44f, .2f, .36f, .26f, .3f, .14f, .8f, .1f } },
                new[] { new[] { .26f, .8f, .5f, .9f, .72f, .78f, .66f, .58f, .3f, .14f, .76f, .14f } },
            },
            new[] { // 3
                new[] { new[] { .27f, .85f, .7f, .85f, .48f, .55f, .7f, .45f, .72f, .25f, .55f, .1f, .27f, .15f } },
                new[] { new[] { .26f, .78f, .42f, .9f, .64f, .88f, .7f, .72f, .56f, .56f, .44f, .54f, .56f, .52f, .72f, .38f, .7f, .18f, .5f, .08f, .26f, .16f } },
                new[] { new[] { .3f, .84f, .66f, .82f, .46f, .56f }, new[] { .46f, .56f, .7f, .42f, .68f, .2f, .48f, .1f, .28f, .18f } },
            },
            new[] { // 4
                new[] { new[] { .62f, .1f, .62f, .9f, .22f, .35f, .8f, .35f } },
                new[] { new[] { .32f, .9f, .26f, .44f, .8f, .44f }, new[] { .62f, .72f, .6f, .1f } },
                new[] { new[] { .3f, .88f, .28f, .5f, .6f, .5f, .66f, .62f }, new[] { .66f, .9f, .64f, .1f } },
            },
            new[] { // 5
                new[] { new[] { .72f, .88f, .32f, .88f, .28f, .55f, .6f, .58f, .74f, .4f, .68f, .18f, .5f, .1f, .27f, .17f } },
                new[] { new[] { .34f, .88f, .3f, .56f, .52f, .62f, .7f, .5f, .72f, .26f, .52f, .1f, .26f, .2f }, new[] { .34f, .88f, .74f, .9f } },
                new[] { new[] { .7f, .9f, .36f, .86f, .32f, .58f, .46f, .62f, .66f, .56f, .7f, .34f, .56f, .14f, .3f, .14f } },
            },
            new[] { // 6
                new[] { new[] { .68f, .88f, .4f, .65f, .28f, .35f, .35f, .14f, .55f, .1f, .7f, .25f, .65f, .45f, .45f, .5f, .3f, .38f } },
                new[] { new[] { .64f, .92f, .46f, .74f, .32f, .5f, .3f, .26f, .44f, .1f, .62f, .12f, .7f, .28f, .6f, .42f, .4f, .42f, .32f, .3f } },
                new[] { new[] { .56f, .92f, .36f, .56f, .34f, .2f, .5f, .1f, .66f, .22f, .62f, .4f, .42f, .44f, .34f, .32f } },
            },
            new[] { // 7
                new[] { new[] { .25f, .88f, .75f, .88f, .45f, .1f } },
                new[] { new[] { .25f, .86f, .76f, .88f, .52f, .44f, .42f, .1f }, new[] { .34f, .5f, .68f, .5f } },
                new[] { new[] { .24f, .74f, .27f, .88f, .76f, .88f, .5f, .1f } },
            },
            new[] { // 8
                new[] { new[] { .5f, .52f, .3f, .68f, .36f, .86f, .5f, .9f, .64f, .86f, .7f, .68f, .5f, .52f, .28f, .35f, .32f, .14f, .5f, .1f, .68f, .14f, .72f, .35f, .5f, .52f } },
                new[] { new[] { .64f, .82f, .5f, .9f, .34f, .82f, .36f, .64f, .66f, .4f, .68f, .18f, .5f, .08f, .32f, .18f, .34f, .4f, .64f, .64f, .64f, .82f } },
            },
            new[] { // 9
                new[] { new[] { .7f, .62f, .55f, .5f, .35f, .55f, .3f, .72f, .42f, .88f, .6f, .88f, .7f, .72f, .68f, .4f, .6f, .1f } },
                new[] { new[] { .7f, .72f, .58f, .88f, .38f, .86f, .3f, .7f, .4f, .56f, .6f, .56f, .7f, .7f, .7f, .1f } },
                new[] { new[] { .68f, .76f, .5f, .9f, .32f, .8f, .32f, .62f, .5f, .54f, .68f, .66f, .66f, .4f, .5f, .1f } },
            },
        };

        int digit = -1, seed, level = 1;
        public void Show(int value, int cardSeed, int deskLevel = 1)
        {
            if (value == digit && cardSeed == seed && deskLevel == level) return;
            digit = value; seed = cardSeed; level = deskLevel; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (digit < 0 || digit > 9) return;
            var r = GetPixelAdjustedRect();
            float size = Mathf.Min(r.width, r.height);
            if (size <= 0) return;
            var rng = new System.Random(seed);
            float J() => (float)(rng.NextDouble() * 2 - 1);
            // How messy this hand is: about a third neat, the rest leaning further the higher the desk level.
            double roll = rng.NextDouble();
            float messy = roll < .35 - .05 * (level - 1) ? .15f : roll < .8 ? .5f : .9f;
            messy = Mathf.Clamp01(messy + J() * .1f);
            var styles = Styles[digit];
            int style = rng.Next(styles.Length);
            var strokes = digit == 0 ? Zero(rng, style) : Copy(styles[style]);

            float slant = J() * (.1f + .28f * messy), angle = J() * .22f * messy;
            float sx = 1 + J() * .2f * messy, sy = 1 + J() * .14f * messy;
            float jitter = .02f + .04f * messy, wobble = .012f + .03f * messy;
            float baseWidth = size * (.065f + (float)rng.NextDouble() * .04f + .015f * messy);
            float phase = (float)rng.NextDouble() * 10, freq = 6 + (float)rng.NextDouble() * 8;
            float ca = Mathf.Cos(angle), sa = Mathf.Sin(angle);

            // Shape every stroke: jitter the key points, smooth them, wobble along the path, then slant, squash and turn.
            var paths = new List<List<Vector2>>();
            foreach (var stroke in strokes)
            {
                var keys = new List<Vector2>();
                for (int i = 0; i + 1 < stroke.Length; i += 2) keys.Add(new Vector2(stroke[i], stroke[i + 1]) + new Vector2(J(), J()) * jitter);
                if (keys.Count < 2) continue;
                // A hurried hand overshoots the last point now and then.
                if (messy > .4f && rng.NextDouble() < .4)
                {
                    var end = keys[keys.Count - 1]; keys[keys.Count - 1] = end + (end - keys[keys.Count - 2]).normalized * .06f * messy;
                }
                var smooth = Smooth(keys, 6);
                var path = new List<Vector2>(smooth.Count);
                for (int i = 0; i < smooth.Count; i++)
                {
                    float t = i / (float)Mathf.Max(1, smooth.Count - 1);
                    var p = smooth[i] + new Vector2(Mathf.Sin(phase + t * freq), Mathf.Cos(phase * 1.3f + t * freq * .8f)) * wobble;
                    p -= new Vector2(.5f, .5f);
                    p = new Vector2(p.x * sx + p.y * slant, p.y * sy);
                    p = new Vector2(p.x * ca - p.y * sa, p.x * sa + p.y * ca);
                    path.Add(p);
                }
                paths.Add(path);
            }
            if (paths.Count == 0) return;

            // Keep the whole digit inside the paper, centred with a little drift.
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
            foreach (var path in paths) foreach (var p in path) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            float fit = Mathf.Min(1, .86f / Mathf.Max(.01f, Mathf.Max(max.x - min.x, max.y - min.y)));
            Vector2 centre = (min + max) * .5f, drift = new Vector2(J() * .04f, J() * .03f) * (1 + messy);

            var ink = color;
            foreach (var path in paths)
            {
                Vector2 prev = default;
                for (int i = 0; i < path.Count; i++)
                {
                    float t = i / (float)Mathf.Max(1, path.Count - 1);
                    // Pen pressure: thicker in the middle of a stroke, thinner where the pen lands and lifts.
                    float width = baseWidth * (.72f + .38f * Mathf.Sin(t * Mathf.PI)) * (1 + J() * .05f);
                    var q = r.center + ((path[i] - centre) * fit + drift) * size;
                    Disc(vh, q, width * .5f, ink);
                    if (i > 0) Seg(vh, prev, q, width, ink);
                    prev = q;
                }
            }
        }

        static float[][] Copy(float[][] strokes) { var copy = new float[strokes.Length][]; for (int i = 0; i < strokes.Length; i++) copy[i] = (float[])strokes[i].Clone(); return copy; }

        /// <summary>0: an oval, a narrow tilted oval, or one that does not quite close (the pen starts and stops at the top).</summary>
        static float[][] Zero(System.Random rng, int style)
        {
            float rx = style == 1 ? .19f : .27f, ry = .39f, gap = style == 2 ? .5f : -.25f;
            int n = 18;
            var pts = new float[(n + 1) * 2];
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.PI / 2 + (i / (float)n) * (Mathf.PI * 2 - gap * .5f);
                pts[i * 2] = .5f + Mathf.Cos(a) * rx * (1 + (float)(rng.NextDouble() - .5) * .08f);
                pts[i * 2 + 1] = .5f + Mathf.Sin(a) * ry;
            }
            return new[] { pts };
        }

        /// <summary>Catmull–Rom through the key points: pen strokes curve instead of turning at sharp corners.</summary>
        static List<Vector2> Smooth(List<Vector2> keys, int steps)
        {
            var result = new List<Vector2>();
            for (int i = 0; i + 1 < keys.Count; i++)
            {
                Vector2 p0 = keys[Mathf.Max(0, i - 1)], p1 = keys[i], p2 = keys[i + 1], p3 = keys[Mathf.Min(keys.Count - 1, i + 2)];
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps, t2 = t * t, t3 = t2 * t;
                    result.Add(.5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3));
                }
            }
            result.Add(keys[keys.Count - 1]);
            return result;
        }

        static void Disc(VertexHelper vh, Vector2 c, float radius, Color color)
        {
            int start = vh.currentVertCount, n = 14;
            var v = UIVertex.simpleVert; v.color = color; v.position = c; vh.AddVert(v);
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.position = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; vh.AddVert(v); }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % n);
        }

        static void Seg(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-4f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            v.position = a - n; vh.AddVert(v); v.position = a + n; vh.AddVert(v);
            v.position = b + n; vh.AddVert(v); v.position = b - n; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
