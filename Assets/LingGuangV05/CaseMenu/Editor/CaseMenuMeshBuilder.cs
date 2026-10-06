using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LingGuangV05.CaseMenu.Editor
{
    /// <summary>Small menu-only geometry accumulator. Repeated details share one mesh, not hundreds of renderers.</summary>
    internal sealed class CaseMenuMeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        internal void Box(Vector3 centre, Vector3 size, float bevel, Quaternion rotation = default)
        {
            if (rotation == default) rotation = Quaternion.identity;
            bevel = Mathf.Clamp(bevel, .00001f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .45f);
            Vector3[][] rings = {
                Rectangle(size.x - bevel * 2, size.y - bevel * 2, -size.z / 2, bevel * .5f),
                Rectangle(size.x, size.y, -size.z / 2 + bevel, bevel),
                Rectangle(size.x, size.y, size.z / 2 - bevel, bevel),
                Rectangle(size.x - bevel * 2, size.y - bevel * 2, size.z / 2, bevel * .5f)
            };
            Cap(rings[0], Vector3.back, centre, rotation);
            Cap(rings[3], Vector3.forward, centre, rotation);
            for (int band = 0; band < 3; band++)
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                Vector3 mid = (rings[band][i] + rings[band][next]) / 2;
                Vector3 normal = band == 0 ? Vector3.back : band == 2 ? Vector3.forward : new Vector3(mid.x, mid.y, 0);
                Quad(centre + rotation * rings[band][i], centre + rotation * rings[band][next],
                    centre + rotation * rings[band + 1][next], centre + rotation * rings[band + 1][i], rotation * normal);
            }
        }

        internal void Cylinder(Vector3 centre, float radius, float depth, int segments = 32, Quaternion rotation = default)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Vector3[] front = Circle(radius, -depth / 2, segments), back = Circle(radius, depth / 2, segments);
            Cap(front, Vector3.back, centre, rotation);
            Cap(back, Vector3.forward, centre, rotation);
            for (int i = 0; i < segments; i++)
            {
                int n = (i + 1) % segments;
                Vector3 normal = new Vector3(front[i].x + front[n].x, front[i].y + front[n].y, 0);
                Quad(centre + rotation * front[i], centre + rotation * front[n], centre + rotation * back[n],
                    centre + rotation * back[i], rotation * normal);
            }
        }

        internal void Ring(Vector3 centre, float outer, float inner, float depth, int segments = 40, Quaternion rotation = default)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Vector3[] of = Circle(outer, -depth / 2, segments), inf = Circle(inner, -depth / 2, segments);
            Vector3[] ob = Circle(outer, depth / 2, segments), inb = Circle(inner, depth / 2, segments);
            for (int i = 0; i < segments; i++)
            {
                int n = (i + 1) % segments;
                RingQuad(of[i], of[n], inf[n], inf[i], Vector3.back);
                RingQuad(ob[i], inb[i], inb[n], ob[n], Vector3.forward);
                Vector3 normal = new Vector3(of[i].x + of[n].x, of[i].y + of[n].y, 0);
                RingQuad(of[i], ob[i], ob[n], of[n], normal);
                RingQuad(inf[i], inf[n], inb[n], inb[i], -normal);
            }
            void RingQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                Quad(centre + rotation * a, centre + rotation * b, centre + rotation * c, centre + rotation * d, rotation * normal);
            }
        }

        internal void Ellipsoid(Vector3 centre, Vector3 radii, int segments = 28, int rows = 14)
        {
            for (int row = 0; row < rows; row++)
            for (int segment = 0; segment < segments; segment++)
            {
                Vector3 a = Point(row, segment), b = Point(row, segment + 1);
                Vector3 c = Point(row + 1, segment + 1), d = Point(row + 1, segment);
                Quad(a, b, c, d, (a + b + c + d) / 4 - centre);
            }
            Vector3 Point(int row, int segment)
            {
                // Offset the poles slightly to avoid degenerate triangles.
                float latitude = Mathf.Lerp(-Mathf.PI / 2 + .001f, Mathf.PI / 2 - .001f, (float)row / rows);
                float longitude = segment * Mathf.PI * 2 / segments;
                return centre + Vector3.Scale(radii, new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude),
                    Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude)));
            }
        }

        internal void Frustum(Vector3 centre, float frontRadius, float rearRadius, float depth, int segments = 40, Quaternion rotation = default)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Vector3[] front = Circle(frontRadius, -depth / 2, segments), back = Circle(rearRadius, depth / 2, segments);
            for (int i = 0; i < segments; i++)
            {
                int n = (i + 1) % segments;
                Vector3 normal = new Vector3(front[i].x + front[n].x, front[i].y + front[n].y, (frontRadius - rearRadius) * 2 / depth * frontRadius);
                Quad(centre + rotation * front[i], centre + rotation * front[n], centre + rotation * back[n], centre + rotation * back[i], rotation * normal);
            }
        }

        internal void Tube(IReadOnlyList<Vector3> points, float radius, int sides = 8)
        {
            if (points.Count < 2) return;
            var rings = new Vector3[points.Count][];
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 tangent = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 guide = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .96f ? Vector3.right : Vector3.up;
                Vector3 x = Vector3.Cross(tangent, guide).normalized, y = Vector3.Cross(tangent, x).normalized;
                rings[i] = new Vector3[sides];
                for (int j = 0; j < sides; j++)
                {
                    float a = j * Mathf.PI * 2 / sides;
                    rings[i][j] = points[i] + radius * (Mathf.Cos(a) * x + Mathf.Sin(a) * y);
                }
            }
            for (int i = 0; i < rings.Length - 1; i++)
            for (int j = 0; j < sides; j++)
            {
                int n = (j + 1) % sides;
                Vector3 normal = (rings[i][j] + rings[i][n]) / 2 - points[i];
                Quad(rings[i][j], rings[i][n], rings[i + 1][n], rings[i + 1][j], normal);
            }
        }

        internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { Vector3 swap = b; b = d; d = swap; }
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            for (int i = 0; i < 4; i++) normals.Add(normal);
            uvs.Add(Vector2.zero); uvs.Add(Vector2.right); uvs.Add(Vector2.one); uvs.Add(Vector2.up);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        private void Cap(Vector3[] points, Vector3 normal, Vector3 centre, Quaternion rotation)
        {
            Vector3 middle = Vector3.zero;
            foreach (Vector3 point in points) middle += point;
            middle /= points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 a = centre + rotation * middle, b = centre + rotation * points[i], c = centre + rotation * points[(i + 1) % points.Length];
                Vector3 n = rotation * normal;
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0) { Vector3 swap = b; b = c; c = swap; }
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                for (int j = 0; j < 3; j++) { normals.Add(n); uvs.Add(Vector2.zero); triangles.Add(start + j); }
            }
        }

        internal Mesh Save(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            string path = CaseMenuModelBuilder.ArtRoot + "/Meshes/" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            return existing;
        }

        private static Vector3[] Circle(float radius, float z, int count)
        {
            var result = new Vector3[count];
            for (int i = 0; i < count; i++) { float angle = i * Mathf.PI * 2 / count; result[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z); }
            return result;
        }

        private static Vector3[] Rectangle(float width, float height, float z, float bevel)
        {
            float x = width / 2, y = height / 2, c = Mathf.Min(bevel, Mathf.Min(x, y) * .8f);
            return new[] { new Vector3(-x+c,-y,z), new Vector3(x-c,-y,z), new Vector3(x,-y+c,z), new Vector3(x,y-c,z),
                new Vector3(x-c,y,z), new Vector3(-x+c,y,z), new Vector3(-x,y-c,z), new Vector3(-x,-y+c,z) };
        }
    }
}
