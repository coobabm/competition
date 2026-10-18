using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// A filled rectangle with a one-pixel border, solid or dashed: the chips, tiles and slots of the 训练 page. A dashed
    /// border means "not yours yet" (a multiplier that is missing); a solid one means "on". The fill is the graphic's own colour.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgFrameGraphic : MaskableGraphic
    {
        Color edge = Color.clear;
        bool dashed;
        float width = 1;

        public void Style(Color fill, Color border, bool dashedBorder = false, float borderWidth = 1)
        {
            if (color == fill && edge == border && dashed == dashedBorder && Mathf.Approximately(width, borderWidth)) return;
            color = fill; edge = border; dashed = dashedBorder; width = borderWidth;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width < 2 || r.height < 2) return;
            if (color.a > 0) Quad(vh, r.xMin, r.yMin, r.xMax, r.yMax, color);
            if (edge.a <= 0) return;
            float w = Mathf.Max(1, width);
            Edge(vh, r.xMin, r.yMax - w, r.xMax, r.yMax, true);
            Edge(vh, r.xMin, r.yMin, r.xMax, r.yMin + w, true);
            Edge(vh, r.xMin, r.yMin + w, r.xMin + w, r.yMax - w, false);
            Edge(vh, r.xMax - w, r.yMin + w, r.xMax, r.yMax - w, false);
        }

        void Edge(VertexHelper vh, float x0, float y0, float x1, float y1, bool horizontal)
        {
            if (!dashed) { Quad(vh, x0, y0, x1, y1, edge); return; }
            const float on = 4, off = 3;
            if (horizontal) for (float x = x0; x < x1; x += on + off) Quad(vh, x, y0, Mathf.Min(x1, x + on), y1, edge);
            else for (float y = y0; y < y1; y += on + off) Quad(vh, x0, y, x1, Mathf.Min(y1, y + on), edge);
        }

        static void Quad(VertexHelper vh, float x0, float y0, float x1, float y1, Color c)
        {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = c;
            v.position = new Vector3(x0, y0); vh.AddVert(v);
            v.position = new Vector3(x0, y1); vh.AddVert(v);
            v.position = new Vector3(x1, y1); vh.AddVert(v);
            v.position = new Vector3(x1, y0); vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The round's progress bar (进度条仿真, XgSim.TrainPace.cs) the way a 2016 training console drew it: a segmented
    /// fill in a dark track, a bright head, tick marks where 加载数据 / 训练 / 验证 meet, a thin sheen running along the
    /// fill while it moves, the fill dimmed to amber while it stalls, and a short flash at the jump to 100%.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgPaceBarGraphic : MaskableGraphic
    {
        static readonly Color Back = XgDark.Track, Fill = XgDark.Accent, Head = XgDark.Good, Stall = XgDark.Hot, Mark = XgDark.Muted;
        float progress, sheen, flash;
        bool active, moving, stalled;
        /// <summary>Progress where the phases meet (the tick marks).</summary>
        public float LoadEnd = .12f, TrainEnd = .92f;
        /// <summary>No sheen, a gentler flash (the reduced-motion setting).</summary>
        public bool Reduced;
        /// <summary>Width of one fill segment and the gap after it, in pixels.</summary>
        public float Cell = 5, Gap = 1;

        public void Show(float k, bool running, bool isMoving, bool isStalled)
        {
            k = Mathf.Clamp01(k);
            if (Mathf.Approximately(k, progress) && running == active && isMoving == moving && isStalled == stalled) return;
            progress = k; active = running; moving = isMoving; stalled = isStalled;
            SetVerticesDirty();
        }

        /// <summary>The jump to 100%: the bar shows full and flashes for a moment.</summary>
        public void Finish() { progress = 1; flash = 1; SetVerticesDirty(); }

        public bool Flashing => flash > 0;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool dirty = false;
            if (active && moving && !stalled && !Reduced) { sheen += dt; dirty = true; }
            if (flash > 0) { flash = Mathf.Max(0, flash - dt * (Reduced ? 4 : 2.6f)); dirty = true; }
            if (dirty) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width < 4 || r.height < 2) return;
            Box(vh, r.xMin, r.yMin, r.xMax, r.yMax, Back);
            // The console's brackets.
            Box(vh, r.xMin, r.yMin, r.xMin + 1, r.yMax, XgDark.Line);
            Box(vh, r.xMax - 1, r.yMin, r.xMax, r.yMax, XgDark.Line);
            float inner = r.width - 4, x0 = r.xMin + 2, y0 = r.yMin + 2, y1 = r.yMax - 2;
            if (y1 <= y0) { y0 = r.yMin; y1 = r.yMax; }
            float shown = flash > 0 ? 1 : progress;
            float w = inner * shown;
            Color fill = stalled ? Color.Lerp(Fill, Stall, .35f) * new Color(.62f, .62f, .62f, 1) : Fill;
            if (w > 0)
            {
                float step = Mathf.Max(2, Cell + Gap);
                for (float x = 0; x < w; x += step) Box(vh, x0 + x, y0, x0 + Mathf.Min(w, x + Cell), y1, fill);
                if (active || flash > 0) Box(vh, x0 + Mathf.Max(0, w - 2), y0, x0 + w, y1, stalled ? Stall : Head);
                // The sheen: a narrow highlight running along the fill while the round moves.
                if (active && moving && !stalled && !Reduced && w > 8)
                {
                    const float band = 16;
                    float at = (sheen * 140) % (w + band) - band;
                    float a = Mathf.Max(0, at), b = Mathf.Min(w, at + band * .5f), c = Mathf.Min(w, at + band);
                    var glow = new Color(1, 1, 1, .32f);
                    var none = new Color(1, 1, 1, 0);
                    if (b > a) Gradient(vh, x0 + a, y0, x0 + b, y1, at >= 0 ? none : Color.Lerp(none, glow, -at / (band * .5f)), glow);
                    if (c > b) Gradient(vh, x0 + b, y0, x0 + c, y1, glow, none);
                }
            }
            // Tick marks at the phase boundaries, sticking out above and below the bar.
            foreach (float m in new[] { LoadEnd, TrainEnd })
            {
                float x = x0 + inner * m;
                Box(vh, x - .5f, r.yMin - 3, x + .5f, r.yMin, Mark);
                Box(vh, x - .5f, r.yMax, x + .5f, r.yMax + 3, Mark);
                Box(vh, x - .5f, y0, x + .5f, y1, new Color(Back.r, Back.g, Back.b, .9f));
            }
            if (flash > 0) Box(vh, r.xMin, r.yMin, r.xMax, r.yMax, new Color(.85f, 1, .95f, flash * (Reduced ? .3f : .65f)));
        }

        static void Box(VertexHelper vh, float x0, float y0, float x1, float y1, Color c) => Gradient(vh, x0, y0, x1, y1, c, c);

        /// <summary>A rectangle whose colour runs from <paramref name="left"/> to <paramref name="right"/>.</summary>
        static void Gradient(VertexHelper vh, float x0, float y0, float x1, float y1, Color left, Color right)
        {
            if (x1 <= x0 || y1 <= y0) return;
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.color = left; v.position = new Vector3(x0, y0); vh.AddVert(v);
            v.position = new Vector3(x0, y1); vh.AddVert(v);
            v.color = right; v.position = new Vector3(x1, y1); vh.AddVert(v);
            v.position = new Vector3(x1, y0); vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    /// <summary>
    /// 双曲线 on the curve card (它在学_可视化借鉴设计.md §2.1): 练过的题 as a solid line, 新题 (the validation accuracy the
    /// lab scores) as a dashed one, the model's ceiling dashed thin and grey, red dots where 新题 went back, and the
    /// space between the two lines shaded faint red where it is wide (它在背答案, XgSim.Holdout.cs).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgTwoCurveGraphic : MaskableGraphic
    {
        public static readonly Color PracticeColor = new Color32(150, 196, 214, 255);
        public static readonly Color NewColor = new Color32(93, 202, 165, 255);
        public static readonly Color GapColor = new Color32(226, 75, 74, 255);
        static readonly Color GridColor = new Color32(27, 45, 59, 255);

        readonly List<float> practice = new List<float>(), fresh = new List<float>();
        float ceiling, wide = 1, highlight;
        Color ceilingColor = XgDark.Muted;
        /// <summary>Red dots on the rounds where 新题 went back.</summary>
        public bool MarkDrops = true;
        public bool ShowValidation = true;
        public int Capacity = 160;
        public float Min { get; private set; }
        public float Max { get; private set; } = 1;

        /// <summary>The model's ceiling (0 = none). Set before <see cref="SetData"/> so the range includes it.</summary>
        public void SetCeiling(float acc, Color color) { ceiling = acc; ceilingColor = color; }

        /// <summary>Both series, right-aligned point for point; a gap of <paramref name="wideGap"/> or more is shaded.</summary>
        public void SetData(List<float> practiceAcc, List<float> newAcc, float wideGap)
        {
            practice.Clear(); fresh.Clear();
            int n = Mathf.Min(practiceAcc.Count, newAcc.Count);
            for (int i = 0; i < n; i++) { practice.Add(practiceAcc[practiceAcc.Count - n + i]); fresh.Add(newAcc[newAcc.Count - n + i]); }
            wide = Mathf.Max(1e-4f, wideGap);
            float lo = float.PositiveInfinity, hi = float.NegativeInfinity;
            foreach (var v in practice) { lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
            foreach (var v in fresh) { lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
            if (ceiling > 0) { lo = Mathf.Min(lo, ceiling); hi = Mathf.Max(hi, ceiling); }
            if (hi < lo) { lo = 0; hi = 1; }
            // Zoom in as the curve flattens, so 98.7% → 99.1% is still visible.
            float span = Mathf.Max(.02f, hi - lo);
            Min = Mathf.Clamp01(Mathf.Floor((lo - span * .15f) * 100) / 100);
            Max = Mathf.Clamp01(Mathf.Ceil((hi + span * .1f) * 100) / 100);
            if (Max - Min < .02f) Min = Mathf.Max(0, Max - .02f);
            SetVerticesDirty();
        }

        public void PulseLastSegment(float amount) { highlight = Mathf.Clamp01(amount); SetVerticesDirty(); }
        void Update() { if (highlight <= 0) return; highlight = Mathf.Max(0, highlight - Time.unscaledDeltaTime * 1.5f); SetVerticesDirty(); }

        /// <summary>Local position of the newest point's gap middle (for the 它在背答案 note), and whether it is wide.</summary>
        public bool LastGap(out Vector2 middle)
        {
            var r = GetPixelAdjustedRect();
            middle = new Vector2(r.xMax, r.center.y);
            if (fresh.Count == 0) return false;
            float p = practice[practice.Count - 1], f = fresh[fresh.Count - 1];
            middle = new Vector2(r.xMax, (Y(r, p) + Y(r, f)) * .5f);
            return p - f >= wide;
        }

        public bool LessonPoint(int index,float expected,out Vector2 point)
        {
            point=Vector2.zero;if(index<0||index>=fresh.Count||!fresh[index].Equals(expected))return false;
            float value=ShowValidation?fresh[index]:practice[index];if(float.IsNaN(value)||float.IsInfinity(value))return false;
            var r=GetPixelAdjustedRect();point=new Vector2(X(r,index,fresh.Count),Y(r,value));return !float.IsNaN(point.y)&&!float.IsInfinity(point.y)&&point.x>=r.xMin&&point.x<=r.xMax;
        }
        float Y(Rect r, float v) => r.yMin + r.height * Mathf.InverseLerp(Min, Max, v);
        float Step(Rect r) => r.width / Mathf.Max(1, Capacity - 1);
        float X(Rect r, int i, int count) => r.xMax - Step(r) * (count - 1 - i);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width <= 1 || r.height <= 1) return;
            for (int i = 0; i <= 4; i++)
            {
                float y = r.yMin + r.height * i / 4f;
                XgDraw.Seg(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), 1, GridColor);
            }
            if (ceiling > 0 && ceiling >= Min && ceiling <= Max)
            {
                float y = Y(r, ceiling);
                for (float x = r.xMin; x < r.xMax; x += 10) XgDraw.Seg(vh, new Vector2(x, y), new Vector2(Mathf.Min(r.xMax, x + 4), y), 1.2f, ceilingColor);
            }
            int n = fresh.Count;
            // The gap: faint red between the lines, stronger the wider it is, only where it is wide.
            for (int i = 1; i < n; i++)
            {
                float g0 = practice[i - 1] - fresh[i - 1], g1 = practice[i] - fresh[i];
                float g = Mathf.Max(g0, g1);
                if (g < wide) continue;
                float a = Mathf.Lerp(.1f, .24f, Mathf.Clamp01((g - wide) / (wide * 2)));
                float xa = X(r, i - 1, n), xb = X(r, i, n);
                XgDraw.Quad(vh, new Vector2(xa, Y(r, fresh[i - 1])), new Vector2(xa, Y(r, practice[i - 1])), new Vector2(xb, Y(r, practice[i])), new Vector2(xb, Y(r, fresh[i])), new Color(GapColor.r, GapColor.g, GapColor.b, a));
            }
            for (int i = 1; i < n; i++)
                XgDraw.Seg(vh, new Vector2(X(r, i - 1, n), Y(r, practice[i - 1])), new Vector2(X(r, i, n), Y(r, practice[i])), 2f, PracticeColor);
            if(ShowValidation) Dashed(vh, r, fresh, 2.6f, 7, 4, NewColor);
            if (MarkDrops)
                for (int i = 1; i < n; i++)
                    if (fresh[i - 1] - fresh[i] > .0005f) XgDraw.Disc(vh, new Vector2(X(r, i, n), Y(r, fresh[i])), 3.5f, GapColor, 10);
            if (highlight > 0 && n >= 2)
                XgDraw.Seg(vh, new Vector2(X(r, n - 2, n), Y(r, fresh[n - 2])), new Vector2(X(r, n - 1, n), Y(r, fresh[n - 1])), 3 + highlight * 5, new Color(1, .72f, .1f, highlight));
        }

        /// <summary>A polyline in dashes whose pattern runs on across the points.</summary>
        void Dashed(VertexHelper vh, Rect r, List<float> data, float width, float dash, float gap, Color color)
        {
            int n = data.Count;
            if (n < 2) return;
            float phase = 0;
            for (int i = 1; i < n; i++)
            {
                var a = new Vector2(X(r, i - 1, n), Y(r, data[i - 1]));
                var b = new Vector2(X(r, i, n), Y(r, data[i]));
                float len = Vector2.Distance(a, b);
                if (len < 1e-4f) continue;
                var dir = (b - a) / len;
                float t = 0;
                while (t < len)
                {
                    bool on = phase < dash;
                    float left = on ? dash - phase : dash + gap - phase;
                    float run = Mathf.Min(left, len - t);
                    if (on) XgDraw.Seg(vh, a + dir * t, a + dir * (t + run), width, color);
                    t += run; phase += run;
                    if (phase >= dash + gap) phase = 0;
                }
            }
        }
    }
}
