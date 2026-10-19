using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Multi-stop gradient (horizontal, vertical or diamond) driven by a Unity <see cref="Gradient"/>, blended into
    /// the graphic's vertex colours. Replaces DreamOS's UIGradientComplex (2026-10-19): same GUID, serialized fields
    /// and public API. The class used to sit in a namespace nested under UnityEngine.UI; it now lives with the rest
    /// of the desktop OS types.
    ///
    /// With "complex gradient" on, the mesh triangles are split at every colour/alpha key so the stops render
    /// sharply instead of being interpolated across a single quad.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LingGuang/OS/Effects/UI Gradient (Advanced)")]
    public class UIGradientComplex : BaseMeshEffect
    {
        [SerializeField]
        Type gradientType;

        [SerializeField]
        Blend blendMode = Blend.Multiply;

        [SerializeField]
        bool complexGradient = true;

        [SerializeField]
        [Range(-1, 1)] float offset = 0f;

        [SerializeField]
        [Range(0.1f, 10)] float zoom = 1f;

        [SerializeField]
        Gradient _effectGradient = new Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) } };

        public Blend BlendMode
        {
            get { return blendMode; }
            set { blendMode = value; graphic.SetVerticesDirty(); }
        }

        public Gradient EffectGradient
        {
            get { return _effectGradient; }
            set { _effectGradient = value; graphic.SetVerticesDirty(); }
        }

        public Type GradientType
        {
            get { return gradientType; }
            set { gradientType = value; graphic.SetVerticesDirty(); }
        }

        public bool ModifyVertices
        {
            get { return complexGradient; }
            set { complexGradient = value; graphic.SetVerticesDirty(); }
        }

        public float Offset
        {
            get { return offset; }
            set { offset = Mathf.Clamp(value, -1f, 1f); graphic.SetVerticesDirty(); }
        }

        public float Zoom
        {
            get { return zoom; }
            set { zoom = Mathf.Clamp(value, 0.1f, 10f); graphic.SetVerticesDirty(); }
        }

        public enum Type { Horizontal, Vertical, Diamond }
        public enum Blend { Override, Add, Multiply }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper.currentVertCount == 0)
                return;

            List<UIVertex> vertexList = new List<UIVertex>();
            helper.GetUIVertexStream(vertexList);
            int vertexCount = vertexList.Count;

            switch (GradientType)
            {
                case Type.Horizontal:
                case Type.Vertical:
                    ApplyLinear(helper, vertexList);
                    break;

                case Type.Diamond:
                    ApplyDiamond(helper, vertexList, vertexCount);
                    break;
            }
        }

        void ApplyLinear(VertexHelper helper, List<UIVertex> vertexList)
        {
            bool vertical = GradientType == Type.Vertical;
            Rect bounds = GetBounds(vertexList);
            float min = vertical ? bounds.yMin : bounds.xMin;
            float extent = vertical ? bounds.height : bounds.width;

            float scale = extent == 0f ? 0f : 1f / extent / Zoom;
            float zoomOffset = (1 - (1 / Zoom)) * 0.5f;
            float shift = (Offset * (1 - zoomOffset)) - zoomOffset;

            if (ModifyVertices)
                SplitTrianglesAtGradientStops(vertexList, bounds, zoomOffset, helper);

            UIVertex vertex = new UIVertex();
            for (int i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref vertex, i);
                float position = vertical ? vertex.position.y : vertex.position.x;
                vertex.color = BlendColor(vertex.color, EffectGradient.Evaluate((position - min) * scale - shift));
                helper.SetUIVertex(vertex, i);
            }
        }

        void ApplyDiamond(VertexHelper helper, List<UIVertex> vertexList, int vertexCount)
        {
            Rect bounds = GetBounds(vertexList);
            float height = bounds.height == 0f ? 0f : 1f / bounds.height / Zoom;
            float radius = bounds.center.y / 2f;
            Vector3 center = (Vector3.right + Vector3.up) * radius + Vector3.forward * vertexList[0].position.z;

            if (ModifyVertices)
            {
                // Fan every triangle edge around one extra centre vertex.
                helper.Clear();
                for (int i = 0; i < vertexCount; i++) helper.AddVert(vertexList[i]);

                UIVertex centralVertex = new UIVertex();
                centralVertex.position = center;
                centralVertex.normal = vertexList[0].normal;
                centralVertex.uv0 = new Vector2(0.5f, 0.5f);
                centralVertex.color = Color.white;
                helper.AddVert(centralVertex);

                for (int i = 1; i < vertexCount; i++) helper.AddTriangle(i - 1, i, vertexCount);
                helper.AddTriangle(0, vertexCount - 1, vertexCount);
            }

            UIVertex vertex = new UIVertex();
            for (int i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref vertex, i);
                vertex.color = BlendColor(vertex.color, EffectGradient.Evaluate(
                    Vector3.Distance(vertex.position, center) * height - Offset));
                helper.SetUIVertex(vertex, i);
            }
        }

        Rect GetBounds(List<UIVertex> vertices)
        {
            float left = vertices[0].position.x;
            float right = left;
            float bottom = vertices[0].position.y;
            float top = bottom;

            for (int i = vertices.Count - 1; i >= 1; --i)
            {
                float x = vertices[i].position.x;
                float y = vertices[i].position.y;

                if (x > right)
                    right = x;
                else if (x < left)
                    left = x;

                if (y > top)
                    top = y;
                else if (y < bottom)
                    bottom = y;
            }

            return new Rect(left, bottom, right - left, top - bottom);
        }

        // Re-emits the triangle list so that no triangle straddles a gradient stop: each one is cut along every stop
        // that crosses it.
        void SplitTrianglesAtGradientStops(List<UIVertex> vertexList, Rect bounds, float zoomOffset, VertexHelper helper)
        {
            List<float> stops = FindStops(zoomOffset, bounds);

            if (stops.Count == 0)
                return;

            helper.Clear();
            int count = vertexList.Count;

            for (int i = 0; i < count; i += 3)
            {
                float[] positions = GetPositions(vertexList, i);
                List<int> originIndices = new List<int>(3);
                List<UIVertex> starts = new List<UIVertex>(3);
                List<UIVertex> ends = new List<UIVertex>(2);

                for (int s = 0; s < stops.Count; s++)
                {
                    int initialCount = helper.currentVertCount;
                    bool hadEnds = ends.Count > 0;
                    bool earlyStart = false;

                    for (int p = 0; p < 3; p++)
                    {
                        if (!originIndices.Contains(p) && positions[p] < stops[s])
                        {
                            int next = (p + 1) % 3;
                            var start = vertexList[p + i];

                            if (positions[next] > stops[s])
                            {
                                originIndices.Insert(0, p);
                                starts.Insert(0, start);
                                earlyStart = true;
                            }

                            else
                            {
                                originIndices.Add(p);
                                starts.Add(start);
                            }
                        }
                    }

                    if (originIndices.Count == 0)
                        continue;
                    if (originIndices.Count == 3)
                        break;

                    foreach (var start in starts)
                        helper.AddVert(start);

                    ends.Clear();

                    foreach (int index in originIndices)
                    {
                        int oppositeIndex = (index + 1) % 3;

                        if (positions[oppositeIndex] < stops[s])
                            oppositeIndex = (oppositeIndex + 1) % 3;
                        ends.Add(CreateSplitVertex(vertexList[index + i], vertexList[oppositeIndex + i], stops[s]));
                    }

                    if (ends.Count == 1)
                    {
                        int oppositeIndex = (originIndices[0] + 2) % 3;
                        ends.Add(CreateSplitVertex(vertexList[originIndices[0] + i], vertexList[oppositeIndex + i], stops[s]));
                    }

                    foreach (var end in ends)
                        helper.AddVert(end);

                    if (hadEnds)
                    {
                        helper.AddTriangle(initialCount - 2, initialCount, initialCount + 1);
                        helper.AddTriangle(initialCount - 2, initialCount + 1, initialCount - 1);

                        if (starts.Count > 0)
                        {
                            if (earlyStart)
                                helper.AddTriangle(initialCount - 2, initialCount + 3, initialCount);
                            else
                                helper.AddTriangle(initialCount + 1, initialCount + 3, initialCount - 1);
                        }
                    }

                    else
                    {
                        int vertexTotal = helper.currentVertCount;
                        helper.AddTriangle(initialCount, vertexTotal - 2, vertexTotal - 1);

                        if (starts.Count > 1)
                            helper.AddTriangle(initialCount, vertexTotal - 1, initialCount + 1);
                    }

                    starts.Clear();
                }

                if (ends.Count > 0)
                {
                    if (starts.Count == 0)
                    {
                        for (int p = 0; p < 3; p++)
                        {
                            if (!originIndices.Contains(p) && positions[p] > stops[stops.Count - 1])
                            {
                                int next = (p + 1) % 3;
                                UIVertex end = vertexList[p + i];

                                if (positions[next] > stops[stops.Count - 1])
                                    starts.Insert(0, end);
                                else
                                    starts.Add(end);
                            }
                        }
                    }

                    foreach (var start in starts)
                        helper.AddVert(start);

                    int vertexTotal = helper.currentVertCount;

                    if (starts.Count > 1)
                    {
                        helper.AddTriangle(vertexTotal - 4, vertexTotal - 2, vertexTotal - 1);
                        helper.AddTriangle(vertexTotal - 4, vertexTotal - 1, vertexTotal - 3);
                    }

                    else if (starts.Count > 0)
                        helper.AddTriangle(vertexTotal - 3, vertexTotal - 1, vertexTotal - 2);
                }

                else
                {
                    helper.AddVert(vertexList[i]);
                    helper.AddVert(vertexList[i + 1]);
                    helper.AddVert(vertexList[i + 2]);
                    int vertexTotal = helper.currentVertCount;
                    helper.AddTriangle(vertexTotal - 3, vertexTotal - 2, vertexTotal - 1);
                }
            }
        }

        // The coordinate the gradient runs along (x for horizontal, y for vertical).
        float AxisPosition(UIVertex vertex)
        {
            return GradientType == Type.Horizontal ? vertex.position.x : vertex.position.y;
        }

        float[] GetPositions(List<UIVertex> vertexList, int index)
        {
            return new float[]
            {
                AxisPosition(vertexList[index]),
                AxisPosition(vertexList[index + 1]),
                AxisPosition(vertexList[index + 2]),
            };
        }

        List<float> FindStops(float zoomOffset, Rect bounds)
        {
            List<float> stops = new List<float>();
            float shift = Offset * (1 - zoomOffset);
            float startBoundary = zoomOffset - shift;
            float endBoundary = (1 - zoomOffset) - shift;

            foreach (var color in EffectGradient.colorKeys)
            {
                if (color.time >= endBoundary)
                    break;

                if (color.time > startBoundary)
                    stops.Add((color.time - startBoundary) * Zoom);
            }

            foreach (var alpha in EffectGradient.alphaKeys)
            {
                if (alpha.time >= endBoundary)
                    break;

                if (alpha.time > startBoundary)
                    stops.Add((alpha.time - startBoundary) * Zoom);
            }

            float min = bounds.xMin;
            float size = bounds.width;

            if (GradientType == Type.Vertical)
            {
                min = bounds.yMin;
                size = bounds.height;
            }

            stops.Sort();

            for (int i = 0; i < stops.Count; i++)
            {
                stops[i] = (stops[i] * size) + min;

                // Stops closer than two units would only produce slivers.
                if (i > 0 && Mathf.Abs(stops[i] - stops[i - 1]) < 2)
                {
                    stops.RemoveAt(i);
                    --i;
                }
            }

            return stops;
        }

        // A vertex on the edge vertex1-vertex2 where it crosses the stop, with the uv interpolated to match.
        UIVertex CreateSplitVertex(UIVertex vertex1, UIVertex vertex2, float stop)
        {
            bool horizontal = GradientType == Type.Horizontal;

            float dx = vertex1.position.x - vertex2.position.x;
            float dy = vertex1.position.y - vertex2.position.y;
            float uvx = vertex1.uv0.x - vertex2.uv0.x;
            float uvy = vertex1.uv0.y - vertex2.uv0.y;

            float ratio = horizontal
                ? (vertex1.position.x - stop) / dx
                : (vertex1.position.y - stop) / dy;

            UIVertex splitVertex = new UIVertex();
            splitVertex.position = horizontal
                ? new Vector3(stop, vertex1.position.y - (dy * ratio), vertex1.position.z)
                : new Vector3(vertex1.position.x - (dx * ratio), stop, vertex1.position.z);
            splitVertex.normal = vertex1.normal;
            splitVertex.uv0 = new Vector2(vertex1.uv0.x - (uvx * ratio), vertex1.uv0.y - (uvy * ratio));
            splitVertex.color = Color.white;
            return splitVertex;
        }

        Color BlendColor(Color colorA, Color colorB)
        {
            switch (BlendMode)
            {
                default: return colorB;
                case Blend.Add: return colorA + colorB;
                case Blend.Multiply: return colorA * colorB;
            }
        }
    }
}
