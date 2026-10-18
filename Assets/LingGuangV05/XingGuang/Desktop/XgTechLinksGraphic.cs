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
        readonly List<Segment> segments = new List<Segment>();
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
            foreach (var line in segments)
            {
                if (line.color.a <= 0) continue;
                var delta = (line.b - line.a).normalized;
                var normal = new Vector2(-delta.y, delta.x) * (line.width * .5f);
                int start = vh.currentVertCount;
                vh.AddVert(line.a - normal, line.color, Vector2.zero);
                vh.AddVert(line.a + normal, line.color, Vector2.zero);
                vh.AddVert(line.b + normal, line.color, Vector2.zero);
                vh.AddVert(line.b - normal, line.color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
