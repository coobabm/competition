using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    // One mesh for the complete dependency graph. Moving its parent does not rebuild its geometry.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgTechLinksGraphic : MaskableGraphic
    {
        struct Segment { public Vector2 a, b; public float width; public Color color; }
        struct Exclusion { public RectTransform owner; public Rect local; public bool visible; public Vector2 position; }
        readonly List<Segment> segments = new List<Segment>();
        readonly List<Exclusion> exclusions = new List<Exclusion>();
        readonly List<Rect> exclusionBounds = new List<Rect>();
        readonly List<Vector2> intervals = new List<Vector2>();
        public void Exclude(RectTransform owner, Rect localBounds)
        {
            exclusions.Add(new Exclusion { owner = owner, local = localBounds });
            SetVerticesDirty();
        }
        public void RefreshExclusions()
        {
            for (int i = 0; i < exclusions.Count; i++)
            {
                var item = exclusions[i];
                bool visible = item.owner != null && item.owner.gameObject.activeInHierarchy;
                Vector2 position = item.owner != null ? item.owner.anchoredPosition : Vector2.zero;
                if (item.visible == visible && item.position == position) continue;
                item.visible = visible; item.position = position; exclusions[i] = item;
                SetVerticesDirty();
            }
        }
        public int Add(Vector2 a, Vector2 b, float width)
        {
            segments.Add(new Segment { a = a, b = b, width = width, color = Color.clear });
            SetVerticesDirty(); return segments.Count - 1;
        }
        public void Paint(int index, Color value)
        {
            var line = segments[index];
            if (line.color == value) return;
            line.color = value; segments[index] = line; SetVerticesDirty();
        }
        public void Move(int index, Vector2 delta)
        {
            var line = segments[index]; line.a += delta; line.b += delta;
            segments[index] = line; SetVerticesDirty();
        }
        public int MeshBuilds { get; private set; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            MeshBuilds++;
            vh.Clear();
            exclusionBounds.Clear();
            foreach (var exclusion in exclusions)
            {
                var owner = exclusion.owner;
                if (owner == null || !owner.gameObject.activeInHierarchy) continue;
                // Ignore the node's reveal tween, while following its permanent layout moves.
                Vector2 a = rectTransform.InverseTransformPoint(owner.parent.TransformPoint(owner.anchoredPosition + exclusion.local.min));
                Vector2 b = rectTransform.InverseTransformPoint(owner.parent.TransformPoint(owner.anchoredPosition + exclusion.local.max));
                exclusionBounds.Add(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)));
            }
            foreach (var line in segments)
            {
                if (line.color.a <= 0) continue;
                intervals.Clear();
                foreach (var bounds in exclusionBounds)
                    if (BlockedInterval(line.a, line.b, bounds, line.width * .5f + 2, out var interval)) intervals.Add(interval);
                intervals.Sort((a, b) => a.x.CompareTo(b.x));
                float cursor = 0;
                foreach (var interval in intervals)
                {
                    if (interval.x > cursor) Draw(line, cursor, interval.x);
                    cursor = Mathf.Max(cursor, interval.y);
                }
                if (cursor < 1) Draw(line, cursor, 1);
            }
            void Draw(Segment line, float from, float to)
            {
                Vector2 a = Vector2.Lerp(line.a, line.b, from), b = Vector2.Lerp(line.a, line.b, to);
                if ((b - a).sqrMagnitude < .0001f) return;
                var delta = (b - a).normalized;
                var normal = new Vector2(-delta.y, delta.x) * (line.width * .5f);
                int start = vh.currentVertCount;
                vh.AddVert(a - normal, line.color, Vector2.zero);
                vh.AddVert(a + normal, line.color, Vector2.zero);
                vh.AddVert(b + normal, line.color, Vector2.zero);
                vh.AddVert(b - normal, line.color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
        }

        // Parametric slab intersection: return only the part hidden by a padded text rectangle.
        internal static bool BlockedInterval(Vector2 a, Vector2 b, Rect bounds, float padding, out Vector2 interval)
        {
            float enter = 0, exit = 1;
            var d = b - a;
            bool Axis(float start, float delta, float min, float max)
            {
                if (Mathf.Abs(delta) < .00001f) return start >= min && start <= max;
                float t0 = (min - start) / delta, t1 = (max - start) / delta;
                enter = Mathf.Max(enter, Mathf.Min(t0, t1));
                exit = Mathf.Min(exit, Mathf.Max(t0, t1));
                return enter <= exit;
            }
            bool hit = Axis(a.x, d.x, bounds.xMin - padding, bounds.xMax + padding)
                && Axis(a.y, d.y, bounds.yMin - padding, bounds.yMax + padding);
            interval = new Vector2(enter, exit);
            return hit;
        }
    }
}
