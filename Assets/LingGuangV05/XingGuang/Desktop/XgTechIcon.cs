using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The picture inside a 科技 square, drawn as vector shapes so it needs no icon sheet (the package's own icons are
    /// fantasy art). One shape per kind of node; a padlock in the corner while the square is locked.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgTechIcon : MaskableGraphic
    {
        public enum Kind { None, Network, Layers, Channels, Data, Spark, Knob, Gear, Check, Eye, Star, Key }

        Kind kind;
        bool locked;
        public Color lockColor = new Color32(214, 238, 245, 255);

        public void Set(Kind k, bool isLocked)
        {
            if (k == kind && isLocked == locked) return;
            kind = k; locked = isLocked; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float u = Mathf.Min(r.width, r.height);
            if (u <= 0) return;
            Vector2 c = r.center;
            Color k = color;
            switch (kind)
            {
                case Kind.Network:
                {
                    Vector2[] a = { c + new Vector2(-.32f, .18f) * u, c + new Vector2(-.32f, -.18f) * u };
                    Vector2[] b = { c + new Vector2(0, .28f) * u, c, c + new Vector2(0, -.28f) * u };
                    Vector2 o = c + new Vector2(.32f, 0) * u;
                    var wire = new Color(k.r, k.g, k.b, k.a * .55f);
                    foreach (var p in a) foreach (var q in b) Seg(vh, p, q, u * .04f, wire);
                    foreach (var q in b) Seg(vh, q, o, u * .04f, wire);
                    foreach (var p in a) Disc(vh, p, u * .08f, k);
                    foreach (var q in b) Disc(vh, q, u * .08f, k);
                    Disc(vh, o, u * .1f, k);
                    break;
                }
                case Kind.Layers:
                    for (int i = 0; i < 4; i++) Quad(vh, c + new Vector2(0, (1.5f - i) * u * .22f), new Vector2(u * .74f, u * .13f), k);
                    break;
                case Kind.Channels:
                    for (int i = 0; i < 5; i++)
                    {
                        float h = u * (.3f + .12f * (i % 3));
                        Quad(vh, new Vector2(c.x + (i - 2) * u * .17f, c.y - u * .36f + h * .5f), new Vector2(u * .11f, h), k);
                    }
                    break;
                case Kind.Data:
                    for (int i = 0; i < 3; i++)
                    {
                        var at = c + new Vector2(0, (1 - i) * u * .27f);
                        Quad(vh, at, new Vector2(u * .7f, u * .2f), k);
                        Disc(vh, at + new Vector2(u * .24f, 0), u * .04f, new Color(0, 0, 0, .55f));
                    }
                    break;
                case Kind.Spark: Star(vh, c, u * .42f, u * .11f, 4, k); break;
                case Kind.Star: Star(vh, c, u * .42f, u * .18f, 5, k); break;
                case Kind.Knob:
                    Ring(vh, c, u * .34f, u * .08f, k);
                    Seg(vh, c, c + new Vector2(-.2f, .2f) * u, u * .09f, k);
                    Disc(vh, c, u * .07f, k);
                    break;
                case Kind.Gear:
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Seg(vh, c + d * u * .2f, c + d * u * .42f, u * .13f, k);
                    }
                    Ring(vh, c, u * .26f, u * .14f, k);
                    break;
                case Kind.Check:
                    Seg(vh, c + new Vector2(-.34f, 0) * u, c + new Vector2(-.1f, -.26f) * u, u * .12f, k);
                    Seg(vh, c + new Vector2(-.15f, -.26f) * u, c + new Vector2(.36f, .3f) * u, u * .12f, k);
                    break;
                case Kind.Eye:
                    Ellipse(vh, c, u * .44f, u * .24f, u * .06f, k);
                    Disc(vh, c, u * .13f, k);
                    break;
                case Kind.Key:
                    Ring(vh, c + new Vector2(-.2f, .12f) * u, u * .17f, u * .07f, k);
                    Seg(vh, c + new Vector2(-.06f, -.02f) * u, c + new Vector2(.36f, -.38f) * u, u * .08f, k);
                    Seg(vh, c + new Vector2(.22f, -.26f) * u, c + new Vector2(.32f, -.16f) * u, u * .07f, k);
                    break;
            }
            if (locked) Padlock(vh, c + new Vector2(.36f, .34f) * u, u * .26f);
        }

        void Padlock(VertexHelper vh, Vector2 at, float s)
        {
            Quad(vh, at + new Vector2(0, -s * .2f), new Vector2(s * 1.1f, s * .85f), new Color32(7, 12, 18, 255));
            Quad(vh, at + new Vector2(0, -s * .2f), new Vector2(s * .85f, s * .62f), lockColor);
            Ring(vh, at + new Vector2(0, s * .3f), s * .32f, s * .14f, lockColor, true);
        }

        static void Quad(VertexHelper vh, Vector2 centre, Vector2 size, Color color)
        {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            Vector2 h = size * .5f;
            v.position = centre + new Vector2(-h.x, -h.y); vh.AddVert(v);
            v.position = centre + new Vector2(-h.x, h.y); vh.AddVert(v);
            v.position = centre + new Vector2(h.x, h.y); vh.AddVert(v);
            v.position = centre + new Vector2(h.x, -h.y); vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        static void Seg(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a; Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = color;
            v.position = a - n; vh.AddVert(v); v.position = a + n; vh.AddVert(v);
            v.position = b + n; vh.AddVert(v); v.position = b - n; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        static void Disc(VertexHelper vh, Vector2 c, float radius, Color color)
        {
            int start = vh.currentVertCount, n = 18;
            var v = UIVertex.simpleVert; v.color = color; v.position = c; vh.AddVert(v);
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.position = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; vh.AddVert(v); }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % n);
        }

        static void Ring(VertexHelper vh, Vector2 c, float radius, float width, Color color, bool upperHalf = false)
            => Ellipse(vh, c, radius, radius, width, color, upperHalf);

        static void Ellipse(VertexHelper vh, Vector2 c, float rx, float ry, float width, Color color, bool upperHalf = false)
        {
            int n = 28, steps = upperHalf ? n / 2 : n;
            var v = UIVertex.simpleVert; v.color = color;
            int start = vh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                v.position = c + new Vector2(d.x * (rx + width * .5f), d.y * (ry + width * .5f)); vh.AddVert(v);
                v.position = c + new Vector2(d.x * (rx - width * .5f), d.y * (ry - width * .5f)); vh.AddVert(v);
            }
            for (int i = 0; i < steps; i++)
            {
                int a = start + i * 2;
                vh.AddTriangle(a, a + 1, a + 3); vh.AddTriangle(a, a + 3, a + 2);
            }
        }

        static void Star(VertexHelper vh, Vector2 c, float outer, float inner, int points, Color color)
        {
            int start = vh.currentVertCount, n = points * 2;
            var v = UIVertex.simpleVert; v.color = color; v.position = c; vh.AddVert(v);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI / points + Mathf.PI / 2;
                float rad = i % 2 == 0 ? outer : inner;
                v.position = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad; vh.AddVert(v);
            }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % n);
        }
    }
}
