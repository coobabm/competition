using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Emergence
{
    [Serializable]
    public sealed class BoardNodeVisual
    {
        public int id;
        public int q;
        public int r;
        public int kind;
        public int variant;
        public int charge;
        public int threshold = 2;
        public bool fired;
    }

    [Serializable]
    public sealed class BoardEdgeVisual
    {
        public int source;
        public int target;
        public int delay;
        public bool echo;
        public bool blocked;
        public bool special;
    }

    /// <summary>A texture-free, core-independent drawing surface for the neural board.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NeuralBoardGraphic : UnityEngine.UI.MaskableGraphic
    {
        public List<BoardNodeVisual> nodes = new List<BoardNodeVisual>();
        public List<BoardEdgeVisual> edges = new List<BoardEdgeVisual>();
        public HashSet<int> previewIds = new HashSet<int>();
        public int selectedId = -1;
        public int hoveredId = -1;
        public bool showEmptyCells = true;
        public bool membrane;
        public float clock;

        public float CellRadius => 39f;

        private static readonly Color Cyan = new Color(0.424f, 0.902f, 0.827f, 1f);
        private static readonly Color Gold = new Color(0.851f, 0.725f, 0.455f, 1f);
        private static readonly Color Violet = new Color(0.710f, 0.608f, 0.929f, 1f);
        private static readonly Color Blue = new Color(0.385f, 0.605f, 0.756f, 1f);
        private static readonly Color Slate = new Color(0.231f, 0.384f, 0.451f, 1f);
        private static readonly Color Membrane = new Color(0.929f, 0.580f, 0.549f, 1f);
        private static readonly Color Ink = new Color(0.027f, 0.071f, 0.102f, 1f);

        private readonly Dictionary<int, BoardNodeVisual> byId = new Dictionary<int, BoardNodeVisual>();
        private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        private readonly Dictionary<int, NodePulse> nodePulses = new Dictionary<int, NodePulse>();
        private readonly List<int> expiredPulses = new List<int>();
        private readonly List<EdgePulse> edgePulses = new List<EdgePulse>();

        private sealed class NodePulse
        {
            public float age;
            public float intensity;
        }

        private sealed class EdgePulse
        {
            public int source;
            public int target;
            public float age;
            public float duration;
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            SetVerticesDirty();
        }

        public Vector2 CellPosition(int q, int r)
        {
            return new Vector2((q + r * 0.5f) * 1.732050808f * CellRadius, r * 1.5f * CellRadius);
        }

        public bool TryHitCell(Vector2 local, out int q, out int r)
        {
            float fr = local.y / (1.5f * CellRadius);
            float fq = local.x / (1.732050808f * CellRadius) - fr * 0.5f;
            float fs = -fq - fr;
            int rq = Mathf.RoundToInt(fq);
            int rr = Mathf.RoundToInt(fr);
            int rs = Mathf.RoundToInt(fs);
            float dq = Mathf.Abs(rq - fq);
            float dr = Mathf.Abs(rr - fr);
            float ds = Mathf.Abs(rs - fs);
            if (dq > dr && dq > ds) rq = -rr - rs;
            else if (dr > ds) rr = -rq - rs;
            q = rq;
            r = rr;
            return Mathf.Max(Mathf.Abs(q), Mathf.Abs(r), Mathf.Abs(q + r)) <= 4
                && (CellPosition(q, r) - local).sqrMagnitude <= 26f * 26f;
        }

        public int HitNode(Vector2 local)
        {
            if (nodes == null) return -1;
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                BoardNodeVisual node = nodes[i];
                if (node != null && (CellPosition(node.q, node.r) - local).sqrMagnitude <= 27f * 27f)
                    return node.id;
            }
            return -1;
        }

        public void PulseNode(int id, float intensity = 1f)
        {
            if (!nodePulses.TryGetValue(id, out NodePulse pulse))
            {
                pulse = new NodePulse();
                nodePulses.Add(id, pulse);
            }
            pulse.age = 0f;
            pulse.intensity = Mathf.Clamp(intensity, 0.2f, 2f);
            SetVerticesDirty();
        }

        public void PulseEdge(int source, int target)
        {
            // A bounded tail prevents a long rescore sequence from accumulating hidden animation work.
            if (edgePulses.Count >= 80) edgePulses.RemoveAt(0);
            edgePulses.Add(new EdgePulse { source = source, target = target, duration = 0.48f });
            SetVerticesDirty();
        }

        public void Tick(float dt)
        {
            dt = Mathf.Max(0f, dt);
            clock += dt;
            expiredPulses.Clear();
            foreach (KeyValuePair<int, NodePulse> pair in nodePulses)
            {
                pair.Value.age += dt;
                if (pair.Value.age >= 0.9f) expiredPulses.Add(pair.Key);
            }
            for (int i = 0; i < expiredPulses.Count; i++) nodePulses.Remove(expiredPulses[i]);
            for (int i = edgePulses.Count - 1; i >= 0; i--)
            {
                edgePulses[i].age += dt;
                if (edgePulses[i].age >= edgePulses[i].duration) edgePulses.RemoveAt(i);
            }
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            byId.Clear();
            occupied.Clear();
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    BoardNodeVisual node = nodes[i];
                    if (node == null) continue;
                    byId[node.id] = node;
                    occupied.Add(new Vector2Int(node.q, node.r));
                }
            }

            DrawBackground(vh);
            if (membrane) DrawMembrane(vh);
            if (edges != null)
            {
                for (int i = 0; i < edges.Count; i++)
                {
                    BoardEdgeVisual edge = edges[i];
                    if (edge != null && byId.TryGetValue(edge.source, out BoardNodeVisual from)
                        && byId.TryGetValue(edge.target, out BoardNodeVisual to)) DrawEdge(vh, edge, from, to);
                }
            }
            for (int i = 0; i < edgePulses.Count; i++) DrawEdgePulse(vh, edgePulses[i]);
            if (nodes != null)
                for (int i = 0; i < nodes.Count; i++)
                    if (nodes[i] != null) DrawNode(vh, nodes[i]);
        }

        private void DrawBackground(VertexHelper vh)
        {
            // The slow, quiet orbital marks provide a sense of instrument scale behind the grid.
            Vector2 orbit = new Vector2(Mathf.Min(343f, Mathf.Max(120f, rectTransform.rect.width * 0.5f - 20f)),
                Mathf.Min(302f, Mathf.Max(120f, rectTransform.rect.height * 0.5f - 19f)));
            AddEllipseArc(vh, orbit, 0.65f, 0f, 360f, Alpha(Slate, 0.19f), 120);
            AddEllipseArc(vh, orbit + Vector2.one * 7f, 0.8f, 18f, 69f, Alpha(Cyan, 0.12f), 18);
            AddEllipseArc(vh, orbit + Vector2.one * 7f, 0.8f, 198f, 249f, Alpha(Cyan, 0.12f), 18);
            for (int i = 0; i < 72; i++)
            {
                float angle = i * Mathf.PI / 36f;
                Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float length = i % 6 == 0 ? 5f : 2f;
                Vector2 edge = Vector2.Scale(radial, orbit - Vector2.one * 6f);
                AddLine(vh, edge, edge - radial * length, 0.7f, Alpha(Slate, i % 6 == 0 ? 0.45f : 0.20f));
            }

            for (int q = -4; q <= 4; q++)
            {
                int minR = Mathf.Max(-4, -q - 4);
                int maxR = Mathf.Min(4, -q + 4);
                for (int r = minR; r <= maxR; r++)
                {
                    Vector2 p = CellPosition(q, r);
                    bool full = occupied.Contains(new Vector2Int(q, r));
                    if (membrane && q < 0) AddDisc(vh, p, 34f, Alpha(Membrane, 0.025f), 6, 30f);
                    if (showEmptyCells || full)
                    {
                        // Short corner marks retain the hex lattice without making a wall of outlines.
                        for (int corner = 0; corner < 6; corner++)
                        {
                            float a = (30f + 60f * corner) * Mathf.Deg2Rad;
                            float b = (30f + 60f * (corner + 1)) * Mathf.Deg2Rad;
                            Vector2 va = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 35.5f;
                            Vector2 vb = p + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 35.5f;
                            AddLine(vh, va, Vector2.Lerp(va, vb, 0.23f), 0.55f, Alpha(Slate, full ? 0.23f : 0.14f));
                            AddLine(vh, Vector2.Lerp(va, vb, 0.77f), vb, 0.55f, Alpha(Slate, full ? 0.23f : 0.14f));
                        }
                    }
                    if (!full && showEmptyCells)
                    {
                        AddDisc(vh, p, 1.35f, Alpha(Slate, 0.65f), 8);
                        AddLine(vh, p + Vector2.left * 5f, p + Vector2.right * 5f, 0.65f, Alpha(Slate, 0.23f));
                        AddLine(vh, p + Vector2.down * 5f, p + Vector2.up * 5f, 0.65f, Alpha(Slate, 0.23f));
                    }
                }
            }
        }

        private void DrawMembrane(VertexHelper vh)
        {
            // q < 0 and q >= 0 meet halfway between two axial columns, which is diagonal on screen.
            Vector2 a = new Vector2((-0.5f - 3.85f * 0.5f) * 1.732050808f * CellRadius, -3.85f * 1.5f * CellRadius);
            Vector2 b = new Vector2((-0.5f + 4.25f * 0.5f) * 1.732050808f * CellRadius, 4.25f * 1.5f * CellRadius);
            AddLine(vh, a, b, 10f, Alpha(Membrane, 0.025f));
            const int segments = 30;
            for (int i = 0; i < segments; i++)
                AddLine(vh, Vector2.Lerp(a, b, (float)i / segments), Vector2.Lerp(a, b, (i + 0.54f) / segments),
                    1.15f, Alpha(Membrane, 0.42f));
            AddDiamond(vh, a, 3f, Alpha(Membrane, 0.70f));
            AddDiamond(vh, b, 3f, Alpha(Membrane, 0.70f));
        }

        private void DrawEdge(VertexHelper vh, BoardEdgeVisual edge, BoardNodeVisual from, BoardNodeVisual to)
        {
            if (from.id == to.id) return;
            EdgeGeometry(from, to, out Vector2 a, out Vector2 control, out Vector2 b);
            bool preview = previewIds != null && previewIds.Contains(from.id) && previewIds.Contains(to.id);
            bool selected = selectedId == from.id || selectedId == to.id;
            Color tone = edge.echo ? Violet : edge.delay > 1 ? Gold : edge.special ? Blue : Cyan;
            if (edge.blocked) tone = Slate;
            float opacity = edge.blocked ? 0.22f : preview ? 0.95f : selected ? 0.77f : 0.38f;
            AddCurve(vh, a, control, b, preview || selected ? 5.6f : 3.8f, Alpha(tone, opacity * 0.08f), 12, false);
            AddCurve(vh, a, control, b, preview ? 1.65f : 1.15f, Alpha(tone, opacity), 12, edge.echo || edge.blocked);

            Vector2 arrow = Bezier(a, control, b, 0.77f);
            Vector2 tangent = BezierTangent(a, control, b, 0.77f).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            if (!edge.blocked)
            {
                AddTriangle(vh, arrow + tangent * 3.8f, arrow - tangent * 2f + normal * 2.8f,
                    arrow - tangent * 2f - normal * 2.8f, Alpha(tone, Mathf.Min(1f, opacity + 0.18f)));
            }
            else
            {
                Vector2 middle = Bezier(a, control, b, 0.5f);
                AddLine(vh, middle + new Vector2(-3f, -3f), middle + new Vector2(3f, 3f), 1f, Alpha(Slate, 0.70f));
                AddLine(vh, middle + new Vector2(-3f, 3f), middle + new Vector2(3f, -3f), 1f, Alpha(Slate, 0.70f));
            }
            if (edge.delay > 1 && !edge.blocked)
            {
                Vector2 middle = Bezier(a, control, b, 0.43f);
                AddDisc(vh, middle, 3.3f, Ink, 12);
                AddRing(vh, middle, 2.8f, 0.8f, Alpha(Gold, 0.85f), 12);
            }
        }

        private void DrawEdgePulse(VertexHelper vh, EdgePulse pulse)
        {
            if (!byId.TryGetValue(pulse.source, out BoardNodeVisual from)
                || !byId.TryGetValue(pulse.target, out BoardNodeVisual to) || from.id == to.id) return;
            EdgeGeometry(from, to, out Vector2 a, out Vector2 c, out Vector2 b);
            float t = Mathf.Clamp01(pulse.age / pulse.duration);
            Color tone = Cyan;
            if (edges != null)
                for (int i = 0; i < edges.Count; i++)
                    if (edges[i] != null && edges[i].source == pulse.source && edges[i].target == pulse.target)
                    {
                        tone = edges[i].echo ? Violet : edges[i].delay > 1 ? Gold : Cyan;
                        break;
                    }
            for (int tail = 4; tail >= 0; tail--)
            {
                float at = t - tail * 0.035f;
                if (at < 0f) continue;
                Vector2 point = Bezier(a, c, b, at);
                float alpha = (1f - tail / 5f) * 0.92f;
                AddDisc(vh, point, 4.5f - tail * 0.4f, Alpha(tone, alpha * 0.10f), 12);
                AddDisc(vh, point, 1.8f - tail * 0.15f, Alpha(tone, alpha), 10);
            }
        }

        private void DrawNode(VertexHelper vh, BoardNodeVisual node)
        {
            Vector2 p = CellPosition(node.q, node.r);
            Color tone = NodeColor(node);
            bool selected = selectedId == node.id;
            bool hovered = hoveredId == node.id;
            bool preview = previewIds != null && previewIds.Contains(node.id);
            float idle = 0.5f + Mathf.Sin(clock * 1.4f + node.id * 1.73f) * 0.5f;
            float radius = node.kind == 5 ? 22f : 20f;
            float activity = node.fired ? 0.40f : 0.82f;
            if (preview || selected || hovered) activity = 1f;

            AddDisc(vh, p, radius + 11f, Alpha(tone, selected || preview ? 0.05f : 0.015f + idle * 0.008f), 32);
            AddDisc(vh, p, radius + 6f, Alpha(tone, selected || preview ? 0.075f : 0.027f), 32);
            AddDisc(vh, p + Vector2.down * 2f, radius + 1.5f, new Color(0.008f, 0.025f, 0.037f, 0.84f), 32);
            AddDisc(vh, p, radius, Color.Lerp(Ink, tone, node.fired ? 0.045f : 0.085f), 32);
            AddRing(vh, p, radius, selected || preview ? 1.65f : 1.1f, Alpha(tone, activity), 36);
            AddRing(vh, p, radius - 3.8f, 0.6f, Alpha(tone, activity * 0.22f), 32);

            // A tiny upper specular arc keeps the node legible as a circular instrument.
            AddArc(vh, p, radius - 1.6f, 0.8f, 50f, 124f, Alpha(tone, node.fired ? 0.18f : 0.48f), 12);
            if (node.variant > 0)
            {
                Color variantColor = node.variant == 1 ? Gold : node.variant == 2 ? Violet : Cyan;
                AddArc(vh, p, radius + 3.4f, 1.65f, 30f, 150f, Alpha(variantColor, 0.95f), 20);
                if (node.variant >= 3) AddArc(vh, p, radius + 3.4f, 1.65f, 210f, 330f, Alpha(variantColor, 0.95f), 20);
                Vector2 badge = p + new Vector2(radius * 0.77f, radius * 0.77f);
                AddDisc(vh, badge, 4.2f, Ink, 12);
                AddDiamond(vh, badge, 2.7f, Alpha(variantColor, 1f));
            }

            if (node.kind == 5)
            {
                AddArc(vh, p, radius + 5f, 0.8f, -35f, 35f, Alpha(Gold, 0.7f), 12);
                AddArc(vh, p, radius + 5f, 0.8f, 145f, 215f, Alpha(Gold, 0.7f), 12);
            }

            int dotCount = Mathf.Clamp(node.threshold, 1, 6);
            for (int i = 0; i < dotCount; i++)
            {
                Vector2 dot = p + new Vector2((i - (dotCount - 1) * 0.5f) * 6f, -11f);
                bool filled = node.fired || i < node.charge;
                if (filled) AddDisc(vh, dot, 1.65f, Alpha(tone, node.fired ? 0.42f : 0.95f), 10);
                else AddRing(vh, dot, 1.65f, 0.7f, Alpha(tone, 0.36f), 10);
            }

            if (selected || hovered)
            {
                float rad = radius + 8f;
                float opacity = selected ? 0.95f : 0.50f;
                for (int i = 0; i < 4; i++)
                    AddArc(vh, p, rad, selected ? 1.3f : 0.85f, 16f + i * 90f, 74f + i * 90f, Alpha(Gold, opacity), 10);
            }
            else if (preview)
            {
                AddRing(vh, p, radius + 6f, 0.8f, Alpha(tone, 0.25f + idle * 0.17f), 36);
            }
            if (node.fired)
            {
                Vector2 mark = p + new Vector2(0f, radius + 4.5f);
                AddLine(vh, mark + new Vector2(-2.8f, 0f), mark + new Vector2(-0.7f, -2.1f), 0.9f, Alpha(tone, 0.60f));
                AddLine(vh, mark + new Vector2(-0.7f, -2.1f), mark + new Vector2(3.5f, 2.3f), 0.9f, Alpha(tone, 0.60f));
            }

            if (nodePulses.TryGetValue(node.id, out NodePulse pulse))
            {
                float t = pulse.age / 0.9f;
                float fade = Mathf.Pow(1f - t, 2f) * pulse.intensity;
                AddDisc(vh, p, radius + 4f, Alpha(tone, fade * 0.17f), 36);
                AddRing(vh, p, radius + 5f + t * 26f, 1.5f * (1f - t) + 0.3f, Alpha(tone, fade * 0.78f), 40);
                AddRing(vh, p, radius, 2.5f, Alpha(Color.Lerp(tone, Color.white, 0.55f), fade), 36);
            }
        }

        private Color NodeColor(BoardNodeVisual node)
        {
            if (node.variant == 1) return Gold;
            if (node.variant == 2) return Violet;
            switch (node.kind)
            {
                case 1: return new Color(0.471f, 0.886f, 0.796f, 1f);
                case 2: return new Color(0.941f, 0.675f, 0.471f, 1f);
                case 3: return new Color(0.741f, 0.675f, 0.929f, 1f);
                case 4: return new Color(0.569f, 0.773f, 0.945f, 1f);
                case 5: return new Color(0.898f, 0.773f, 0.537f, 1f);
                default: return new Color(0.694f, 0.796f, 0.816f, 1f);
            }
        }

        private void EdgeGeometry(BoardNodeVisual from, BoardNodeVisual to, out Vector2 a, out Vector2 c, out Vector2 b)
        {
            Vector2 start = CellPosition(from.q, from.r);
            Vector2 end = CellPosition(to.q, to.r);
            Vector2 delta = end - start;
            float distance = delta.magnitude;
            Vector2 direction = distance > 0.001f ? delta / distance : Vector2.right;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            float bend = Mathf.Clamp(distance * 0.075f, 4f, 20f);
            c = (start + end) * 0.5f + normal * bend;
            a = start + (c - start).normalized * (from.kind == 5 ? 24f : 22f);
            b = end - (end - c).normalized * (to.kind == 5 ? 25f : 23f);
        }

        private static Color Alpha(Color value, float alpha)
        {
            value.a = Mathf.Clamp01(alpha);
            return value;
        }

        private static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }

        private static Vector2 BezierTangent(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            return 2f * ((1f - t) * (c - a) + t * (b - c));
        }

        private void Vertex(VertexHelper vh, Vector2 point, Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = point;
            vertex.color = tint * color;
            vertex.uv0 = Vector2.zero;
            vh.AddVert(vertex);
        }

        private void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int index = vh.currentVertCount;
            Vertex(vh, a, tint);
            Vertex(vh, b, tint);
            Vertex(vh, c, tint);
            vh.AddTriangle(index, index + 1, index + 2);
        }

        private void AddDiamond(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            AddTriangle(vh, center + Vector2.up * radius, center + Vector2.right * radius, center + Vector2.down * radius, tint);
            AddTriangle(vh, center + Vector2.up * radius, center + Vector2.down * radius, center + Vector2.left * radius, tint);
        }

        private void AddDisc(VertexHelper vh, Vector2 center, float radius, Color tint, int segments, float rotation = 0f)
        {
            int first = vh.currentVertCount;
            Vertex(vh, center, tint);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments + rotation * Mathf.Deg2Rad;
                Vertex(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, tint);
            }
            for (int i = 0; i < segments; i++) vh.AddTriangle(first, first + i + 1, first + i + 2);
        }

        private void AddRing(VertexHelper vh, Vector2 center, float radius, float thickness, Color tint, int segments)
        {
            AddArc(vh, center, radius, thickness, 0f, 360f, tint, segments);
        }

        private void AddArc(VertexHelper vh, Vector2 center, float radius, float thickness, float startAngle, float endAngle, Color tint, int segments)
        {
            int first = vh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(startAngle, endAngle, (float)i / segments) * Mathf.Deg2Rad;
                Vector2 radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vertex(vh, center + radial * (radius - thickness * 0.5f), tint);
                Vertex(vh, center + radial * (radius + thickness * 0.5f), tint);
                if (i == 0) continue;
                int last = first + i * 2;
                vh.AddTriangle(last - 2, last - 1, last);
                vh.AddTriangle(last, last - 1, last + 1);
            }
        }

        private void AddEllipseArc(VertexHelper vh, Vector2 radii, float thickness, float startAngle, float endAngle, Color tint, int segments)
        {
            int first = vh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(startAngle, endAngle, (float)i / segments) * Mathf.Deg2Rad;
                Vector2 radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 point = Vector2.Scale(radial, radii);
                Vector2 normal = new Vector2(radial.x / radii.x, radial.y / radii.y).normalized * thickness * 0.5f;
                Vertex(vh, point - normal, tint);
                Vertex(vh, point + normal, tint);
                if (i == 0) continue;
                int last = first + i * 2;
                vh.AddTriangle(last - 2, last - 1, last);
                vh.AddTriangle(last, last - 1, last + 1);
            }
        }

        private void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 0.0001f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * (width * 0.5f);
            int first = vh.currentVertCount;
            Vertex(vh, a - n, tint);
            Vertex(vh, a + n, tint);
            Vertex(vh, b - n, tint);
            Vertex(vh, b + n, tint);
            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first + 2, first + 1, first + 3);
        }

        private void AddCurve(VertexHelper vh, Vector2 a, Vector2 c, Vector2 b, float width, Color tint, int segments, bool dashed)
        {
            int first = vh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector2 p = Bezier(a, c, b, t);
                Vector2 d = BezierTangent(a, c, b, t).normalized;
                Vector2 n = new Vector2(-d.y, d.x) * width * 0.5f;
                Vertex(vh, p - n, tint);
                Vertex(vh, p + n, tint);
                if (i == 0 || dashed && i % 3 == 0) continue;
                int last = first + i * 2;
                vh.AddTriangle(last - 2, last - 1, last);
                vh.AddTriangle(last, last - 1, last + 1);
            }
        }
    }
}
