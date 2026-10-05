using UnityEngine;
using UnityEngine.UI;

namespace HongmengOS.Retro
{
    /// <summary>Texture-free rounded UI face with a directional machined/plastic bevel.</summary>
    [AddComponentMenu("")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CrtShape : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float cornerRadius = 12f;
        [SerializeField, Min(0f)] private float bevelWidth = 3f;
        [SerializeField] private Color topColor = Color.white;
        [SerializeField] private Color bottomColor = Color.gray;
        [SerializeField] private Color bevelLight = Color.white;
        [SerializeField] private Color bevelDark = Color.black;

        private const int CornerSteps = 10;
        private const int PerimeterCount = (CornerSteps + 1) * 4;

        public void Configure(float radius, float bevel, Color top, Color bottom,
            Color light, Color dark)
        {
            cornerRadius = Mathf.Max(0f, radius);
            bevelWidth = Mathf.Max(0f, bevel);
            topColor = top;
            bottomColor = bottom;
            bevelLight = light;
            bevelDark = dark;
            color = Color.white;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;
            float bevel = Mathf.Min(bevelWidth, Mathf.Min(rect.width, rect.height) * 0.45f);
            float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
            Rect inner = new Rect(rect.x + bevel, rect.y + bevel,
                rect.width - bevel * 2f, rect.height - bevel * 2f);

            for (int i = 0; i < PerimeterCount; i++)
            {
                Vector2 point = Perimeter(rect, radius, i);
                Vector2 direction = point - rect.center;
                direction.Normalize();
                float light = Mathf.Clamp01(0.5f + (direction.y - direction.x) * 0.45f);
                Color face = Color.Lerp(bottomColor, topColor, Mathf.InverseLerp(rect.yMin, rect.yMax, point.y));
                AddVertex(mesh, rect, point,
                    bevel > 0f ? Color.Lerp(bevelDark, bevelLight, light) : face);
            }
            for (int i = 0; i < PerimeterCount; i++)
            {
                Vector2 point = Perimeter(inner, Mathf.Max(0f, radius - bevel), i);
                AddVertex(mesh, rect, point,
                    Color.Lerp(bottomColor, topColor, Mathf.InverseLerp(rect.yMin, rect.yMax, point.y)));
            }
            AddVertex(mesh, rect, inner.center, Color.Lerp(bottomColor, topColor, 0.5f));
            int center = PerimeterCount * 2;
            for (int i = 0; i < PerimeterCount; i++)
            {
                int next = (i + 1) % PerimeterCount;
                mesh.AddTriangle(i, next, PerimeterCount + i);
                mesh.AddTriangle(next, PerimeterCount + next, PerimeterCount + i);
                mesh.AddTriangle(PerimeterCount + i, PerimeterCount + next, center);
            }
        }

        private void AddVertex(VertexHelper mesh, Rect rect, Vector2 point, Color vertexColor)
        {
            mesh.AddVert(point, vertexColor * color,
                new Vector2((point.x - rect.x) / rect.width, (point.y - rect.y) / rect.height));
        }

        private static Vector2 Perimeter(Rect rect, float radius, int index)
        {
            int corner = index / (CornerSteps + 1);
            float fraction = (index % (CornerSteps + 1)) / (float)CornerSteps;
            float angle = (180f + corner * 90f + fraction * 90f) * Mathf.Deg2Rad;
            float cx = corner == 0 || corner == 3 ? rect.xMin + radius : rect.xMax - radius;
            float cy = corner < 2 ? rect.yMin + radius : rect.yMax - radius;
            return new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
        }
    }
}
