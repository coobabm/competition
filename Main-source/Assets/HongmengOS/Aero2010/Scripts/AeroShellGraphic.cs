using UnityEngine;
using UnityEngine.UI;

namespace HongmengOS.Aero2010
{
    /// <summary>Small, texture-free shell decorations. Geometry is cached until its state or rect changes.</summary>
    [AddComponentMenu("HongmengOS/Aero Shell Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]

    public sealed class AeroShellGraphic : MaskableGraphic
    {
        public enum Shape { Glass, TaskButton, Orb, Avatar, Network, Volume, Search, Chevron }
        public Shape shape;
        public float radius = 5;
        public Color top = new Color(.24f, .44f, .65f, .86f);
        public Color bottom = new Color(.055f, .18f, .32f, .94f);
        private bool hovered, pressed, selected;
        public bool Selected { get => selected; set { if (selected == value) return; selected = value; SetVerticesDirty(); } }
        public void SetHovered(bool value) { if (hovered == value) return; hovered = value; SetVerticesDirty(); }
        public void SetPressed(bool value) { if (pressed == value) return; pressed = value; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            if (shape == Shape.Orb) { Orb(vh, r); return; }
            if (shape == Shape.Avatar) { Avatar(vh, r); return; }
            if (shape == Shape.Network) { Network(vh, r); return; }
            if (shape == Shape.Volume) { Volume(vh, r); return; }
            if (shape == Shape.Search) { Search(vh, r); return; }
            if (shape == Shape.Chevron) { Chevron(vh, r); return; }
            Color a = top, b = bottom;
            if (shape == Shape.TaskButton)
            {
                float light = selected ? .31f : (hovered ? .23f : .025f);
                a = new Color(.72f, .88f, 1f, selected || hovered ? .50f : .055f);
                b = new Color(.12f, .38f, .62f, light + .15f);
            }
            else if (hovered) { a = Color.Lerp(a, new Color(.60f, .80f, 1f, .94f), .28f); }
            if (pressed) { a *= .72f; b *= .85f; }
            Rounded(vh, r, radius, a, b);
            if (shape == Shape.TaskButton ? selected || hovered : a.a > .02f || b.a > .02f)
            {
                Quad(vh, new Rect(r.xMin + radius, r.yMax - 1, Mathf.Max(0, r.width - radius * 2), 1), new Color(.80f, .92f, 1, .66f));
                Quad(vh, new Rect(r.xMin + radius, r.yMin, Mathf.Max(0, r.width - radius * 2), 1), new Color(.015f, .08f, .16f, .75f));
            }
            if (shape == Shape.TaskButton && selected)
                Rounded(vh, new Rect(r.xMin + 8, r.yMin + 1, Mathf.Max(0, r.width - 16), 3), 1, new Color(.76f, .94f, 1f, .90f), new Color(.16f, .65f, 1f, .7f));
        }

        private void Orb(VertexHelper v, Rect r)
        {
            float d = Mathf.Min(r.width, r.height);
            Rect q = new Rect(r.center.x - d * .5f, r.center.y - d * .5f, d, d);
            Ellipse(v, q, new Color(.012f, .10f, .21f, 1), new Color(.02f, .19f, .34f, 1));
            q = Inset(q, 1.4f);
            Ellipse(v, q, new Color(.47f, .83f, 1f, 1), new Color(.02f, .26f, .49f, 1));
            q = Inset(q, 1.7f);
            Ellipse(v, q, new Color(.10f, .42f, .70f, 1), new Color(.015f, .14f, .29f, 1));
            if (hovered || selected) Ellipse(v, Inset(q, 1), new Color(.22f, .77f, 1f, .38f), new Color(.09f, .40f, .88f, .13f));
            float unit = d * .205f, gap = d * .045f;
            Vector2 c = r.center;
            Color red = new Color(1f, .34f, .15f), green = new Color(.49f, .84f, .20f), blue = new Color(.11f, .59f, .96f), gold = new Color(1f, .81f, .16f);
            float darken = pressed ? .70f : 1;
            Quad(v, new Rect(c.x - gap * .5f - unit, c.y + gap * .5f, unit, unit), red * darken);
            Quad(v, new Rect(c.x + gap * .5f, c.y + gap * .5f, unit, unit), green * darken);
            Quad(v, new Rect(c.x - gap * .5f - unit, c.y - gap * .5f - unit, unit, unit), blue * darken);
            Quad(v, new Rect(c.x + gap * .5f, c.y - gap * .5f - unit, unit, unit), gold * darken);
            Ellipse(v, new Rect(q.xMin + d * .11f, q.center.y + d * .06f, q.width - d * .22f, d * .25f), new Color(1, 1, 1, .39f), new Color(1, 1, 1, 0));
        }

        private void Avatar(VertexHelper v, Rect r)
        {
            Rounded(v, r, 8, new Color(.87f, .95f, 1), new Color(.35f, .59f, .79f));
            Rounded(v, Inset(r, 3), 6, new Color(.07f, .29f, .48f), new Color(.03f, .13f, .28f));
            Ellipse(v, new Rect(r.center.x - r.width * .14f, r.center.y, r.width * .28f, r.height * .28f), new Color(.91f, .97f, 1), new Color(.56f, .80f, .96f));
            Ellipse(v, new Rect(r.center.x - r.width * .28f, r.yMin + r.height * .15f, r.width * .56f, r.height * .36f), new Color(.80f, .93f, 1), new Color(.27f, .58f, .84f));
        }
        private void Network(VertexHelper v, Rect r)
        {
            for (int i = 0; i < 4; i++) Quad(v, new Rect(r.xMin + i * r.width * .23f, r.yMin + 2, r.width * .15f, r.height * (.20f + .19f * i)), Color.white);
        }
        private void Volume(VertexHelper v, Rect r)
        {
            Quad(v, new Rect(r.xMin + 1, r.center.y - 4, 5, 8), Color.white);
            Triangle(v, new Vector2(r.xMin + 5, r.center.y + 4), new Vector2(r.xMin + 12, r.yMax - 2), new Vector2(r.xMin + 12, r.yMin + 2), Color.white);
            Triangle(v, new Vector2(r.xMin + 5, r.center.y + 4), new Vector2(r.xMin + 12, r.yMin + 2), new Vector2(r.xMin + 5, r.center.y - 4), Color.white);
            Arc(v, new Vector2(r.xMin + 11, r.center.y), 7, -55, 55, 1.5f, Color.white);
            Arc(v, new Vector2(r.xMin + 11, r.center.y), 11, -50, 50, 1.2f, new Color(1, 1, 1, .8f));
        }
        private void Search(VertexHelper v, Rect r)
        {
            float s = Mathf.Min(r.width, r.height);
            Vector2 c = new Vector2(r.center.x - s * .11f, r.center.y + s * .10f);
            Arc(v, c, s * .24f, 0, 360, 1.8f, new Color(.24f, .34f, .42f, 1));
            Line(v, c + new Vector2(s * .18f, -s * .18f), c + new Vector2(s * .40f, -s * .40f), 2.5f, new Color(.24f, .34f, .42f, 1));
        }
        private void Chevron(VertexHelper v, Rect r)
        {
            Triangle(v, new Vector2(r.xMin + r.width * .35f, r.yMin + r.height * .23f), new Vector2(r.xMax - r.width * .20f, r.center.y), new Vector2(r.xMin + r.width * .35f, r.yMax - r.height * .23f), color);
        }

        private void Rounded(VertexHelper v, Rect r, float rad, Color upper, Color lower)
        {
            rad = Mathf.Clamp(rad, 0, Mathf.Min(r.width, r.height) * .5f);
            int first = v.currentVertCount;
            v.AddVert(r.center, Tint(Color.Lerp(lower, upper, .5f)), Vector2.zero);
            const int steps = 6;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 c = new Vector2(corner == 0 || corner == 3 ? r.xMax - rad : r.xMin + rad, corner < 2 ? r.yMax - rad : r.yMin + rad);
                for (int j = 0; j <= steps; j++)
                {
                    float angle = (corner * 90f + j * 90f / steps) * Mathf.Deg2Rad;
                    Vector2 p = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rad;
                    v.AddVert(p, Tint(Color.Lerp(lower, upper, Mathf.InverseLerp(r.yMin, r.yMax, p.y))), Vector2.zero);
                }
            }
            int count = 4 * (steps + 1);
            for (int i = 0; i < count; i++) v.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % count);
        }
        private void Ellipse(VertexHelper v, Rect r, Color upper, Color lower)
        {
            int first = v.currentVertCount;
            v.AddVert(r.center, Tint(Color.Lerp(lower, upper, .5f)), Vector2.zero);
            const int steps = 48;
            for (int i = 0; i < steps; i++)
            {
                float a = i * Mathf.PI * 2 / steps;
                Vector2 p = r.center + new Vector2(Mathf.Cos(a) * r.width * .5f, Mathf.Sin(a) * r.height * .5f);
                v.AddVert(p, Tint(Color.Lerp(lower, upper, (Mathf.Sin(a) + 1) * .5f)), Vector2.zero);
            }
            for (int i = 0; i < steps; i++) v.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % steps);
        }
        private void Arc(VertexHelper v, Vector2 center, float rad, float start, float end, float width, Color c)
        {
            const int steps = 28;
            for (int i = 0; i < steps; i++)
            {
                float a = Mathf.Lerp(start, end, i / (float)steps) * Mathf.Deg2Rad;
                float b = Mathf.Lerp(start, end, (i + 1) / (float)steps) * Mathf.Deg2Rad;
                Line(v, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * rad, width, c);
            }
        }
        private void Line(VertexHelper v, Vector2 a, Vector2 b, float width, Color c)
        {
            Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            int i = v.currentVertCount;
            v.AddVert(a - n, Tint(c), Vector2.zero); v.AddVert(a + n, Tint(c), Vector2.zero);
            v.AddVert(b + n, Tint(c), Vector2.zero); v.AddVert(b - n, Tint(c), Vector2.zero);
            v.AddTriangle(i, i + 1, i + 2); v.AddTriangle(i, i + 2, i + 3);
        }
        private void Quad(VertexHelper v, Rect r, Color c)
        {
            int i = v.currentVertCount;
            v.AddVert(new Vector2(r.xMin, r.yMin), Tint(c), Vector2.zero); v.AddVert(new Vector2(r.xMin, r.yMax), Tint(c), Vector2.zero);
            v.AddVert(new Vector2(r.xMax, r.yMax), Tint(c), Vector2.zero); v.AddVert(new Vector2(r.xMax, r.yMin), Tint(c), Vector2.zero);
            v.AddTriangle(i, i + 1, i + 2); v.AddTriangle(i, i + 2, i + 3);
        }
        private void Triangle(VertexHelper v, Vector2 a, Vector2 b, Vector2 c, Color fill)
        {
            int i = v.currentVertCount;
            v.AddVert(a, Tint(fill), Vector2.zero); v.AddVert(b, Tint(fill), Vector2.zero); v.AddVert(c, Tint(fill), Vector2.zero); v.AddTriangle(i, i + 1, i + 2);
        }
        private Color32 Tint(Color c) => c * color;
        private static Rect Inset(Rect r, float amount) => new Rect(r.xMin + amount, r.yMin + amount, Mathf.Max(0, r.width - amount * 2), Mathf.Max(0, r.height - amount * 2));
    }
}
