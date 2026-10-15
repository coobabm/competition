using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 终章 · 逐层点亮 (它在学_可视化借鉴设计 §2.5, after bbycroft/llm-viz): 12 Transformer blocks stacked in a 2D
    /// isometric view, lit from the bottom up as pre-training passes each twelfth. Each block shows its insides on the
    /// front face — four attention heads as little causal grids, the MLP as a slab of neuron stripes — and the residual
    /// stream runs up through all of them. Lit blocks glow in the colour of the current loss (hot while it is high, teal
    /// once it is low); the block being trained pulses and fills; a stalled one flickers dim; the last block lighting
    /// sends one sweep up the residual stream. Same dark scanner look as <see cref="XgCortexGraphic"/>: glow is
    /// vertex-alpha falloff, no post-processing. The draw loop allocates nothing.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgPretrainStackGraphic : MaskableGraphic
    {
        public const int Blocks = 12;
        public const float FlashSeconds = 1.3f, SweepSeconds = 1.7f;

        // Block geometry in design units: footprint W × D, slab height H, one block every Step.
        const float W = 150, D = 40, H = 16, Step = 26, C = .866f, Sv = .35f;
        // The residual stream's footprint point (between the heads and the MLP on the front face).
        const float StreamX = 70, StreamY = 20;
        const int Heads = 4, HeadCells = 3, MlpStripes = 12;
        const float HeadU0 = 6, HeadPitch = 21, HeadWidth = 17, MlpU0 = 94, MlpU1 = 145;
        // Design bounds (projected): the stack, the stream's ends and room for the layer tags on the right.
        const float MinX = -42, MaxX = 160, MinY = -76, MaxY = 312;

        static readonly Color Teal = new Color32(40, 230, 210, 255), Hot = new Color32(255, 154, 60, 255);
        static readonly Color Bad = new Color32(255, 82, 110, 255), Star = new Color32(255, 240, 190, 255);
        static readonly Color Glass = new Color32(17, 26, 64, 255), Dim = new Color32(50, 62, 108, 255), Edge = new Color32(110, 180, 255, 255);

        int lit = -1, prevLit = -1;
        float fill, heat;
        bool running, stalled, reduced;
        readonly float[] flashAt = new float[Blocks];
        float sweepAt = -99;

        VertexHelper vh;
        float sc = 1;
        Vector2 origin;
        static readonly Vector2 DesignCenter = new Vector2((MinX + MaxX) * .5f, (MinY + MaxY) * .5f);

        public int Lit => Mathf.Max(0, lit);

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            for (int i = 0; i < Blocks; i++) flashAt[i] = -99;
        }

        /// <summary>Forget the last state (another save): the next <see cref="SetState"/> lights without flashes.</summary>
        public void Snap()
        {
            prevLit = -1; lit = -1; sweepAt = -99;
            for (int i = 0; i < Blocks; i++) flashAt[i] = -99;
            SetVerticesDirty();
        }

        /// <summary>
        /// The stack's state: <paramref name="litBlocks"/> lit (0–12), <paramref name="blockFill"/> how far the next one
        /// is, <paramref name="lossHeat"/> 1 = untrained loss, 0 = as low as it goes.
        /// </summary>
        public void SetState(int litBlocks, float blockFill, float lossHeat, bool isRunning, bool isStalled, bool reduceFx)
        {
            litBlocks = Mathf.Clamp(litBlocks, 0, Blocks);
            float now = Time.unscaledTime;
            if (prevLit >= 0 && litBlocks > prevLit && !reduceFx)
            {
                // Each newly lit block flashes; several at once ripple upwards.
                for (int i = prevLit; i < litBlocks; i++) flashAt[i] = now + (i - prevLit) * .12f;
                if (litBlocks == Blocks) sweepAt = now + (litBlocks - prevLit) * .12f + .5f;
            }
            else if (prevLit < 0 && litBlocks == Blocks && !reduceFx) sweepAt = now + .4f;
            bool changed = litBlocks != lit || Mathf.Abs(blockFill - fill) > .002f || Mathf.Abs(lossHeat - heat) > .002f
                || isRunning != running || isStalled != stalled || reduceFx != reduced;
            prevLit = litBlocks; lit = litBlocks;
            fill = Mathf.Clamp01(blockFill); heat = Mathf.Clamp01(lossHeat);
            running = isRunning; stalled = isStalled; reduced = reduceFx;
            if (changed) SetVerticesDirty();
        }

        /// <summary>Called every frame while shown: redraws only while something moves.</summary>
        public void Tick()
        {
            if (Animating) SetVerticesDirty();
        }

        bool Animating
        {
            get
            {
                if (reduced) return false;
                if (lit >= 0 && lit < Blocks && (running || stalled)) return true;
                float now = Time.unscaledTime;
                if (now - sweepAt < SweepSeconds + .2f) return true;
                for (int i = 0; i < Blocks; i++) if (now - flashAt[i] < FlashSeconds + .1f) return true;
                return false;
            }
        }

        /// <summary>Where block <paramref name="index"/>'s tag goes, relative to this rect's centre (for a centre-anchored label).</summary>
        public Vector2 TagPosition(int index)
        {
            var r = GetPixelAdjustedRect();
            Fit(r);
            return P(Iso(W, 0, index * Step + H * .5f)) + new Vector2(16 * sc, 0) - r.center;
        }

        /// <summary>The colour lit blocks take at a loss heat: hot orange while the loss is high, teal once it is low.</summary>
        public static Color LitColor(float lossHeat) => Color.Lerp(Teal, Hot, Mathf.Clamp01(lossHeat));

        // ---------------------------------------------------------------- mapping

        void Fit(Rect r)
        {
            sc = Mathf.Max(.01f, Mathf.Min(r.width / (MaxX - MinX), r.height / (MaxY - MinY)));
            origin = r.center;
        }

        static Vector2 Iso(float x, float y, float z) => new Vector2((x - y) * C, z - (x + y) * Sv);
        Vector2 P(Vector2 d) => origin + (d - DesignCenter) * sc;
        Vector2 IsoP(float x, float y, float z) => P(Iso(x, y, z));

        static float Hash(int i) { float x = Mathf.Sin(i * 12.9898f + 78.233f) * 43758.547f; return x - Mathf.Floor(x); }
        static Color A(Color c, float a) { c.a *= Mathf.Clamp01(a); return c; }
        static Color Clear(Color c) { c.a = 0; return c; }

        // ---------------------------------------------------------------- primitives (screen space)

        void Quad4(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
        {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.position = a; v.color = ca; vh.AddVert(v);
            v.position = b; v.color = cb; vh.AddVert(v);
            v.position = c; v.color = cc; vh.AddVert(v);
            v.position = d; v.color = cd; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>A line with a solid core and edges fading to nothing (anti-aliased; a glow when the feather is wide).</summary>
        void SoftSeg(Vector2 a, Vector2 b, float core, float feather, Color col)
        {
            var d = b - a; if (d.sqrMagnitude < 1e-6f || col.a <= .004f) return;
            var n = new Vector2(-d.y, d.x).normalized;
            Vector2 c0 = n * (core * .5f), f0 = n * (core * .5f + feather);
            var edge = Clear(col);
            Quad4(a - c0, a + c0, b + c0, b - c0, col, col, col, col);
            Quad4(a + c0, a + f0, b + f0, b + c0, col, edge, edge, col);
            Quad4(a - f0, a - c0, b - c0, b - f0, edge, col, col, edge);
        }

        /// <summary>A soft ellipse, brightest at the centre.</summary>
        void Halo(Vector2 c, float rx, float ry, Color col, int n = 28)
        {
            if (col.a <= .004f) return;
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = col; v.position = c; vh.AddVert(v);
            v.color = Clear(col);
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.position = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); vh.AddVert(v); }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % n);
        }

        void Face(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
        {
            if (col.a <= .004f) return;
            XgDraw.Quad(vh, a, b, c, d, col);
        }

        /// <summary>A cell on block-front face y = D: u along the block (0–W), v up the slab (0–H).</summary>
        void FrontCell(float z0, float u0, float u1, float v0, float v1, Color col)
        {
            if (col.a <= .004f) return;
            XgDraw.Quad(vh, IsoP(u0, D, z0 + v0), IsoP(u0, D, z0 + v1), IsoP(u1, D, z0 + v1), IsoP(u1, D, z0 + v0), col);
        }

        // ---------------------------------------------------------------- mesh

        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width < 4 || r.height < 4 || lit < 0) return;
            vh = v;
            try
            {
                Fit(r);
                float t = Time.unscaledTime;
                Color tint = LitColor(heat);
                float sweep = reduced ? -1 : (t - sweepAt) / SweepSeconds;
                bool sweeping = sweep >= 0 && sweep <= 1;
                // The comet's height on the stream while sweeping (below the first block to above the last).
                float bottomZ = -12, topZ = (Blocks - 1) * Step + H + 22;
                float cometZ = Mathf.Lerp(bottomZ, topZ, Mathf.SmoothStep(0, 1, Mathf.Clamp01(sweep)));

                for (int i = 0; i < Blocks; i++)
                {
                    float z0 = i * Step;
                    Stream(i == 0 ? bottomZ : z0 - (Step - H), z0 + H, i < lit, tint);
                    float boost = sweeping ? Mathf.Exp(-Sq((z0 + H * .5f - cometZ) / 22f)) : 0;
                    Block(i, z0, t, tint, boost);
                }
                Stream((Blocks - 1) * Step + H, topZ, lit >= Blocks, tint);
                if (sweeping) Comet(cometZ, sweep, bottomZ);
            }
            finally { vh = null; }
        }

        static float Sq(float x) => x * x;

        /// <summary>The residual stream between two heights: a dim rail, bright where the blocks below it are lit.</summary>
        void Stream(float za, float zb, bool on, Color tint)
        {
            Vector2 a = IsoP(StreamX, StreamY, za), b = IsoP(StreamX, StreamY, zb);
            if (on)
            {
                SoftSeg(a, b, 1.6f * sc, 4 * sc, A(tint, .18f));
                SoftSeg(a, b, 1.4f * sc, .9f * sc, A(Color.Lerp(tint, Color.white, .45f), .9f));
            }
            else SoftSeg(a, b, 1.2f * sc, .8f * sc, A(Dim, .7f));
        }

        void Block(int i, float z0, float t, Color tint, float boost)
        {
            float z1 = z0 + H;
            bool isLit = i < lit;
            bool active = i == lit && lit < Blocks;
            bool stall = active && stalled;
            bool training = active && running && !stalled;
            float flash = reduced ? 0 : 1 - Mathf.Clamp01((t - flashAt[i]) / FlashSeconds);
            if (t < flashAt[i]) flash = 0;

            // How strongly the block shows, and in which colour.
            Color col; float k;
            if (isLit) { col = tint; k = .75f + .25f * boost; }
            else if (stall)
            {
                // A dying fluorescent tube: mostly dim, with irregular short flickers.
                float f = reduced ? .4f : Hash(Mathf.FloorToInt(t * 13)) > .62f ? .85f : .25f + .1f * Hash(Mathf.FloorToInt(t * 31) + 7);
                col = Color.Lerp(Dim, Bad, .7f); k = f;
            }
            else if (training)
            {
                float pulse = reduced ? .5f : .5f + .5f * Mathf.Sin(t * 5.2f);
                col = Color.Lerp(Dim, tint, .35f + .4f * fill); k = .45f + .3f * pulse;
            }
            else { col = Dim; k = .35f; }
            if (boost > 0 && !isLit) k = Mathf.Max(k, boost * .6f);

            // Soft glow behind lit and working blocks.
            Vector2 centre = IsoP(W * .5f, D * .5f, z0 + H * .5f);
            if (isLit) Halo(centre, 100 * sc, 44 * sc, A(tint, .1f + .25f * boost + .35f * flash));
            else if (training) Halo(centre, 104 * sc, 46 * sc, A(tint, .08f + .1f * k));
            else if (stall) Halo(centre, 96 * sc, 42 * sc, A(Bad, .06f * k));

            // Faces: top lightest, the long front face mid, the short side darkest; unlit blocks are dark glass.
            Vector2 t00 = IsoP(0, 0, z1), tW0 = IsoP(W, 0, z1), tWD = IsoP(W, D, z1), t0D = IsoP(0, D, z1);
            Vector2 b0D = IsoP(0, D, z0), bWD = IsoP(W, D, z0), bW0 = IsoP(W, 0, z0);
            Color fillTop = isLit ? Color.Lerp(Glass, col, .55f) : Color.Lerp(Glass, col, .25f * k);
            Color fillFront = isLit ? Color.Lerp(Glass, col, .38f) : Color.Lerp(Glass, col, .18f * k);
            Color fillSide = isLit ? Color.Lerp(Glass, col, .22f) : Color.Lerp(Glass, col, .1f * k);
            float glassA = isLit ? .82f : .62f;
            Face(t00, tW0, tWD, t0D, A(fillTop, glassA));
            Face(b0D, t0D, tWD, bWD, A(fillFront, glassA));
            Face(bWD, tWD, tW0, bW0, A(fillSide, glassA));

            Insides(i, z0, t, col, isLit, training, stall, k, boost);

            // Edges: bright rims on lit blocks, thin dim rims otherwise.
            Color rim = isLit ? Color.Lerp(col, Color.white, .25f + .5f * flash) : stall ? col : Color.Lerp(Dim, Edge, training ? .5f : .15f);
            float ra = isLit ? .9f : stall ? .9f * k : training ? .55f + .3f * k : .45f;
            float core = (isLit ? 1.2f : .9f) * sc, feather = (isLit ? 1.6f + 3 * flash : .8f) * sc;
            Color rc = A(rim, ra);
            SoftSeg(t00, tW0, core, feather, rc); SoftSeg(tW0, tWD, core, feather, rc);
            SoftSeg(tWD, t0D, core, feather, rc); SoftSeg(t0D, t00, core, feather, rc);
            SoftSeg(t0D, b0D, core, feather, rc); SoftSeg(tWD, bWD, core, feather, rc); SoftSeg(tW0, bW0, core, feather, rc);
            SoftSeg(b0D, bWD, core, feather, A(rim, ra * .7f)); SoftSeg(bWD, bW0, core, feather, A(rim, ra * .7f));

            // The block being trained fills along its bottom edge.
            if (training && fill > 0)
            {
                Color bar = A(Color.Lerp(tint, Color.white, .3f), .9f);
                FrontCell(z0, 0, W * fill, .4f, 2.1f, bar);
            }

            // Lighting up: a white flash over the faces, fading.
            if (flash > 0)
            {
                Color w = A(Star, .55f * flash);
                Face(t00, tW0, tWD, t0D, w);
                Face(b0D, t0D, tWD, bWD, A(Star, .35f * flash));
                Halo(centre, (100 + 40 * (1 - flash)) * sc, (44 + 18 * (1 - flash)) * sc, A(Star, .3f * flash));
            }
        }

        /// <summary>The block's insides on its front face: four causal attention grids and the MLP's neuron stripes.</summary>
        void Insides(int i, float z0, float t, Color col, bool isLit, bool training, bool stall, float k, float boost)
        {
            const int parts = Heads * HeadCells * HeadCells + MlpStripes;
            float cu = HeadWidth / HeadCells, cv = (H - 5) / HeadCells;
            int part = 0;
            for (int h = 0; h < Heads; h++)
            {
                float hu = HeadU0 + h * HeadPitch;
                for (int r = 0; r < HeadCells; r++)
                    for (int c = 0; c < HeadCells; c++, part++)
                    {
                        // Causal attention: a token sees itself and what came before (row r, columns 0..r).
                        bool on = c <= r;
                        float v1 = H - 2.5f - r * cv, v0 = v1 - cv + .7f;
                        float u0 = hu + c * cu + .35f, u1 = u0 + cu - .7f;
                        float w = Hash(i * 97 + h * 13 + r * 5 + c);
                        float a;
                        Color cc = col;
                        if (isLit) { a = on ? .45f + .5f * w : .1f; cc = Color.Lerp(col, Color.white, on ? .25f * w + .3f * boost : 0); }
                        else if (training)
                        {
                            bool learnt = part < fill * parts;
                            float shimmer = reduced ? .5f : .5f + .5f * Mathf.Sin(t * 7 + w * 6.28f);
                            a = learnt ? (on ? .35f + .4f * w : .08f) : .06f + .12f * shimmer * w;
                        }
                        else if (stall) a = (on ? .25f : .06f) * k;
                        else a = on ? .12f : .05f;
                        FrontCell(z0, u0, u1, v0, v1, A(cc, a));
                    }
            }
            float su = (MlpU1 - MlpU0) / MlpStripes;
            FrontCell(z0, MlpU0 - 1, MlpU1 + .5f, 1.6f, H - 1.6f, A(col, isLit ? .18f : .06f));
            for (int s = 0; s < MlpStripes; s++, part++)
            {
                float w = Hash(i * 53 + s * 11 + 3);
                float a;
                if (isLit) a = .3f + .6f * w + .2f * boost;
                else if (training)
                {
                    bool learnt = part < fill * parts;
                    float shimmer = reduced ? .5f : .5f + .5f * Mathf.Sin(t * 6 + s * .9f);
                    a = learnt ? .3f + .5f * w * (.6f + .4f * shimmer) : .05f + .08f * shimmer;
                }
                else if (stall) a = .2f * w * k + .04f;
                else a = .06f + .05f * w;
                float u0 = MlpU0 + s * su + .45f;
                FrontCell(z0, u0, u0 + su - .9f, 2.4f, H - 2.4f, A(isLit ? Color.Lerp(col, Color.white, .2f * w) : col, a));
            }
        }

        /// <summary>The finishing sweep: a comet rising up the residual stream with a fading tail.</summary>
        void Comet(float z, float p, float bottomZ)
        {
            float fadeOut = 1 - Mathf.SmoothStep(.85f, 1, p);
            Vector2 head = IsoP(StreamX, StreamY, z), tail = IsoP(StreamX, StreamY, Mathf.Max(bottomZ, z - 70));
            Quad4(tail + new Vector2(-2.2f * sc, 0), tail + new Vector2(2.2f * sc, 0), head + new Vector2(2.2f * sc, 0), head + new Vector2(-2.2f * sc, 0),
                Clear(Star), Clear(Star), A(Star, .85f * fadeOut), A(Star, .85f * fadeOut));
            Halo(head, 22 * sc, 22 * sc, A(Star, .45f * fadeOut));
            Halo(head, 6 * sc, 6 * sc, A(Color.white, .95f * fadeOut), 16);
        }
    }
}
