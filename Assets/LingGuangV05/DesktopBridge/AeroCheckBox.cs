using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>A Win7 checkbox drawn for a DreamOS switch: grey edge, pale gradient box, a dark blue tick when on.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AeroCheckBox : MaskableGraphic
    {
        public SwitchManager source;
        bool shown;

        void LateUpdate()
        {
            if (source == null || source.isOn == shown) return;
            shown = source.isOn;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            Quad(vh, r, new Color32(142, 143, 143, 255), new Color32(142, 143, 143, 255));
            r = Inset(r, 1);
            Quad(vh, r, new Color32(244, 244, 244, 255), new Color32(222, 222, 222, 255));
            r = Inset(r, 2);
            Quad(vh, r, new Color32(232, 236, 241, 255), new Color32(250, 251, 252, 255));
            if (!shown) return;
            var c = new Color32(30, 58, 95, 255);
            float s = r.width;
            Line(vh, new Vector2(r.xMin + s * .10f, r.yMin + s * .52f), new Vector2(r.xMin + s * .40f, r.yMin + s * .18f), 2.2f, c);
            Line(vh, new Vector2(r.xMin + s * .36f, r.yMin + s * .18f), new Vector2(r.xMin + s * .95f, r.yMin + s * .90f), 2.2f, c);
        }

        static Rect Inset(Rect r, float a) => new Rect(r.xMin + a, r.yMin + a, Mathf.Max(0, r.width - 2 * a), Mathf.Max(0, r.height - 2 * a));

        void Quad(VertexHelper v, Rect r, Color32 top, Color32 bottom)
        {
            int i = v.currentVertCount;
            v.AddVert(new Vector2(r.xMin, r.yMin), Tint(bottom), Vector2.zero);
            v.AddVert(new Vector2(r.xMin, r.yMax), Tint(top), Vector2.zero);
            v.AddVert(new Vector2(r.xMax, r.yMax), Tint(top), Vector2.zero);
            v.AddVert(new Vector2(r.xMax, r.yMin), Tint(bottom), Vector2.zero);
            v.AddTriangle(i, i + 1, i + 2); v.AddTriangle(i, i + 2, i + 3);
        }

        void Line(VertexHelper v, Vector2 a, Vector2 b, float width, Color32 c)
        {
            var n = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            int i = v.currentVertCount;
            v.AddVert(a - n, Tint(c), Vector2.zero); v.AddVert(a + n, Tint(c), Vector2.zero);
            v.AddVert(b + n, Tint(c), Vector2.zero); v.AddVert(b - n, Tint(c), Vector2.zero);
            v.AddTriangle(i, i + 1, i + 2); v.AddTriangle(i, i + 2, i + 3);
        }

        Color32 Tint(Color32 c) => (Color)c * color;
    }
}
