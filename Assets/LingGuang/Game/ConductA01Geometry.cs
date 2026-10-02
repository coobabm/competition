using System.Collections.Generic;
using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Geometry ported from the supplied A01 HTML, seed 7, default parameters.
    /// Coordinates use its S unit, with output along +X. No simulation RNG is consumed.</summary>
    public sealed class ConductA01Geometry
    {
        public const float GlyphRadius = 0.3f, Slim = 0.42f, Notch = 0.22f, AxonLength = 0.75f;
        public const float WorldUnit = BoardView.HexSize * 0.6f;
        public readonly struct Stroke
        {
            public readonly Vector2 A, B;
            public readonly float Width, Alpha, Highlight;
            public Stroke(Vector2 a, Vector2 b, float width, float alpha, float highlight = 0)
            { A = a; B = b; Width = width; Alpha = alpha; Highlight = highlight; }
        }
        public readonly List<Stroke> Strokes = new List<Stroke>();
        public Vector2[] Axon { get; private set; }
        public Vector2[] Dendrite { get; private set; }
        public float Phase { get; private set; }
        public readonly Vector2[] Polygon = { new Vector2(0.39f, 0), new Vector2(0, 0.126f),
            new Vector2(-0.246f, 0), new Vector2(0, -0.126f) };

        // The JS source's mulberry32, including unsigned shifts and 32-bit overflow.
        sealed class RandomStream
        {
            uint state;
            public RandomStream(int seed) { state = unchecked((uint)(seed * 977 + 13)); }
            public float Next()
            {
                unchecked
                {
                    state += 0x6D2B79F5;
                    uint t = (state ^ (state >> 15)) * (1u | state);
                    t ^= t + ((t ^ (t >> 7)) * (61u | t));
                    return (float)(((t ^ (t >> 14)) / 4294967296.0));
                }
            }
        }

        public ConductA01Geometry(int seed = 7)
        {
            var r = new RandomStream(seed);
            Axon = Branch(r, new Vector2((1.3f - Notch * .9f) * GlyphRadius, 0),
                (r.Next() - .5f) * .06f, AxonLength, 0, .10f, 0, false);
            Vector2 end = Axon[6], delta = end - Axon[4];
            float angle = Mathf.Atan2(delta.y, delta.x);
            int count = 2 + (r.Next() < .5f ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                float a = angle + (i - (count - 1) / 2f) * .55f + (r.Next() - .5f) * .2f;
                float length = .07f + r.Next() * .06f;
                Vector2 tip = end + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * length;
                Strokes.Add(new Stroke(end, tip, .5f, .6f));
                // Bouton: tiny cross, still legible through the low-resolution world pass.
                Strokes.Add(new Stroke(tip - Vector2.up * .008f, tip + Vector2.up * .008f, 1, .6f));
            }
            Dendrite = Branch(r, new Vector2(-.82f * GlyphRadius, 0), Mathf.PI + (r.Next() - .5f) * .25f,
                .5f, 2, .32f, 1.5f, true);
            for (int i = 0; i < 2; i++)
            {
                float sign = i % 2 != 0 ? 1 : -1, t = .35f + r.Next() * .4f;
                var start = new Vector2(-.82f * GlyphRadius * t, sign * Slim * GlyphRadius * (1 - t));
                float a = Mathf.PI + sign * (.75f + r.Next() * .55f);
                Branch(r, start, a, .2f + r.Next() * .1f, 1, .4f, .9f, true);
            }
            Strokes.Add(new Stroke(Vector2.zero, new Vector2(0, Slim * .72f * GlyphRadius), .5f, .46f));
            Strokes.Add(new Stroke(Vector2.zero, new Vector2(0, -Slim * .72f * GlyphRadius), .5f, .46f));
            if (r.Next() < .6f) Strokes.Add(new Stroke(Vector2.zero, new Vector2(-.82f * .72f * GlyphRadius, 0), .5f, .46f));
            Phase = r.Next() * 6.28f;
            Membrane(1, 1.5f, .66f);
            Membrane(.72f, .5f, .39f);
        }

        Vector2[] Branch(RandomStream r, Vector2 position, float angle, float length, int depth,
            float wiggle, float children, bool dendrite, int level = 0)
        {
            var pts = new Vector2[7]; pts[0] = position;
            for (int i = 1; i <= 6; i++)
            {
                angle += (r.Next() - .5f) * wiggle;
                position += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (length / 6);
                pts[i] = position;
                Strokes.Add(new Stroke(pts[i - 1], position, level > 0 ? .5f : 1,
                    dendrite ? .57f * (level > 0 ? .55f : .85f) : .78f, dendrite ? 0 : .45f));
            }
            if (depth > 0)
            {
                int count = Mathf.Max(0, Mathf.FloorToInt(children * (.6f + r.Next() * .8f) + .5f));
                for (int i = 0; i < count; i++)
                {
                    int at = 2 + Mathf.FloorToInt(r.Next() * 3);
                    float a = angle + (r.Next() < .5f ? -1 : 1) * (.35f + r.Next() * .55f);
                    float l = length * (.42f + r.Next() * .22f);
                    Branch(r, pts[at], a, l, depth - 1, wiggle * 1.15f, children * .7f, dendrite, level + 1);
                }
            }
            return pts;
        }

        void Membrane(float scale, float width, float alpha)
        {
            // Exact gap measured around the axon-facing tip. Keep the two double-line rails open.
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = Polygon[i] * scale, b = Polygon[(i + 1) % 4] * scale;
                Vector2 d = (b - a).normalized;
                if (i == 0) a += d * (Notch * GlyphRadius * scale);
                if (i == 3) b -= d * (Notch * GlyphRadius * scale);
                Strokes.Add(new Stroke(a, b, width, alpha));
            }
        }

        public static Vector2 Along(Vector2[] points, float u)
        {
            float total = 0;
            for (int i = 1; i < points.Length; i++) total += Vector2.Distance(points[i - 1], points[i]);
            float d = Mathf.Clamp01(u) * total;
            for (int i = 1; i < points.Length; i++)
            {
                float length = Vector2.Distance(points[i - 1], points[i]);
                if (d <= length) return Vector2.Lerp(points[i - 1], points[i], length > 0 ? d / length : 0);
                d -= length;
            }
            return points[points.Length - 1];
        }

        public Mesh CreateLineMesh()
        {
            var positions = new List<Vector3>(); var uv = new List<Vector2>();
            var directions = new List<Vector3>(); var colors = new List<Color>(); var indices = new List<int>();
            foreach (var s in Strokes)
            {
                int start = positions.Count;
                for (int j = 0; j < 4; j++)
                {
                    positions.Add((j < 2 ? (Vector3)s.A : (Vector3)s.B) * WorldUnit);
                    uv.Add(new Vector2(j % 2 == 0 ? -1 : 1, s.Width));
                    directions.Add((s.B - s.A).normalized);
                    colors.Add(new Color(s.Highlight, 0, 0, s.Alpha));
                }
                indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
                indices.Add(start + 2); indices.Add(start + 1); indices.Add(start + 3);
            }
            var mesh = new Mesh { name = "A01 shared filament mesh", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(positions); mesh.SetUVs(0, uv); mesh.SetUVs(1, directions);
            mesh.SetColors(colors); mesh.SetTriangles(indices, 0);
            mesh.bounds = new Bounds(Vector3.right * .08f, new Vector3(1.3f, 1.1f, .1f));
            return mesh;
        }

        public Sprite CreateIcon()
        {
            const int size = 160;
            var pixels = new Color[size * size];
            // Icon is UP-oriented to retain NeuronArt's existing rotation convention.
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((y + .5f) / size * 2.4f - 1.1f, -((x + .5f) / size - .5f) * 2.4f);
                float alpha = Mathf.Exp(-p.sqrMagnitude / .012f) * .5f;
                foreach (var s in Strokes)
                {
                    Vector2 ab = s.B - s.A;
                    float u = Mathf.Clamp01(Vector2.Dot(p - s.A, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
                    float d = Vector2.Distance(p, s.A + ab * u);
                    alpha = Mathf.Max(alpha, Mathf.Clamp01(1 - d / (.009f * s.Width + .009f)) * s.Alpha);
                }
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "A01 Conduct HUD", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixels(pixels); tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            sprite.name = "A01 Conduct"; sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
