using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.UI
{
    /// <summary>One bounded mesh for weighted arrows; no per-edge GameObjects or Update loop.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BoardEdgesGraphic : UnityEngine.UI.MaskableGraphic
    {
        public struct Segment
        {
            public Vector2 from, to;
            public float weight;
            public Color tint;
        }

        private readonly List<Segment> _segments = new List<Segment>(210);

        public void SetSegments(List<Segment> segments)
        {
            _segments.Clear();
            _segments.AddRange(segments);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            for (int i = 0; i < _segments.Count; i++)
            {
                Segment segment = _segments[i];
                Vector2 delta = segment.to - segment.from;
                if (delta.sqrMagnitude < 1f) continue;
                Vector2 direction = delta.normalized;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                float width = Mathf.Lerp(1.8f, 7f, Mathf.Clamp01(Mathf.Abs(segment.weight)));
                // End caps stop short of node centers, so the direction arrow stays visible.
                Vector2 start = segment.from + direction * 19f;
                Vector2 end = segment.to - direction * 23f;
                Quad(mesh, start - perpendicular * width * .5f, start + perpendicular * width * .5f,
                    end + perpendicular * width * .5f, end - perpendicular * width * .5f, segment.tint);
                int offset = mesh.currentVertCount;
                mesh.AddVert(end + direction * 7f, segment.tint, Vector2.zero);
                mesh.AddVert(end - direction * 3f + perpendicular * 5f, segment.tint, Vector2.zero);
                mesh.AddVert(end - direction * 3f - perpendicular * 5f, segment.tint, Vector2.zero);
                mesh.AddTriangle(offset, offset + 1, offset + 2);
            }
        }

        private static void Quad(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c,
            Vector2 d, Color tint)
        {
            int offset = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddVert(d, tint, Vector2.zero);
            mesh.AddTriangle(offset, offset + 1, offset + 2);
            mesh.AddTriangle(offset, offset + 2, offset + 3);
        }
    }
}
