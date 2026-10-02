using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Quiet hemispheres underneath the real hex board; never participates in picking.</summary>
    public sealed class NeuralBackdrop : MonoBehaviour
    {
        Mesh mesh;

        public static NeuralWaveField Create(Transform parent, BoardView board)
        {
            var field = parent.gameObject.AddComponent<NeuralWaveField>();
            field.Initialize();
            for (int side = 0; side < 2; side++)
            {
                bool found = false;
                var bounds = new Bounds();
                foreach (int cell in board.state.board.AllCells())
                {
                    bool left = board.state.board.Col(cell) <= board.state.cfg.midlineLeftCol;
                    if (left != (side == 0)) continue;
                    var p = board.CellPos(cell);
                    if (!found) { bounds = new Bounds(p, Vector3.zero); found = true; }
                    else bounds.Encapsulate(p);
                }
                if (!found) continue;
                var go = new GameObject(side == 0 ? "Left Hemisphere" : "Right Hemisphere");
                go.transform.SetParent(parent, false);
                go.AddComponent<NeuralBackdrop>().Build(bounds, side, field);
            }
            return field;
        }

        void Build(Bounds bounds, int side, NeuralWaveField field)
        {
            const int segments = 96;
            float rx = bounds.extents.x + 0.56f, ry = bounds.extents.y + 0.78f;
            var center = bounds.center;
            // Leave a narrow visual fissure without changing any cell or bridge position.
            center.x += side == 0 ? -0.24f : 0.24f;
            field.SetLobe(side, center, new Vector2(rx, ry));
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            vertices[0] = center;
            colors[0] = new Color(0.0016f, 0.003f, 0.010f, 0.82f);
            var rim = Gfx.MakeLine("Brain contour", transform, Gfx.Additive, -7, 0.033f);
            rim.loop = true;
            rim.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(a), y = Mathf.Sin(a);
                var p = center + new Vector3(Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), 0.76f) * rx,
                    Mathf.Sign(y) * Mathf.Pow(Mathf.Abs(y), 0.84f) * ry, 0);
                vertices[i + 1] = p;
                uv[i + 1] = new Vector2((p.x - center.x) / rx, (p.y - center.y) / ry);
                colors[i + 1] = new Color(0.0024f, 0.006f, 0.016f, 0.72f);
                rim.SetPosition(i, p);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            mesh = new Mesh { name = "LG_Hemisphere", vertices = vertices, triangles = triangles, colors = colors, uv = uv };
            mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = field.FieldMaterial != null ? field.FieldMaterial : Gfx.Alpha;
            renderer.sortingOrder = -8;
            Gfx.SetColor(renderer, Color.white);
            Gfx.SetLineColor(rim, new Color(0.24f, 0.23f, 0.48f, 0.34f));

            for (int fold = 0; fold < 4; fold++)
            {
                var line = Gfx.MakeLine("Cortical fold", transform, Gfx.Alpha, -6, 0.033f);
                line.positionCount = 28;
                for (int i = 0; i < 28; i++)
                {
                    float u = i / 27f;
                    float x = Mathf.Lerp(-0.69f, 0.69f, u) * rx;
                    float y = (fold - 1.5f) * ry * 0.37f
                        + Mathf.Sin(u * 8f + fold * 1.7f + side) * 0.075f;
                    line.SetPosition(i, center + new Vector3(x, y));
                }
                Gfx.SetLineColor(line, new Color(0.0008f, 0.0012f, 0.004f, 0.65f));
            }
        }

        void OnDestroy()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }
    }
}
