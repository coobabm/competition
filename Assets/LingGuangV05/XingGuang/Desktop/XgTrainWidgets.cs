using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// A filled rectangle with a one-pixel border, solid or dashed: the chips, tiles and slots of the 训练 page. A dashed
    /// border means "not yours yet" (a multiplier that is missing); a solid one means "on". The fill is the graphic's own colour.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgFrameGraphic : MaskableGraphic
    {
        Color edge = Color.clear;
        bool dashed;
        float width = 1;

        public void Style(Color fill, Color border, bool dashedBorder = false, float borderWidth = 1)
        {
            if (color == fill && edge == border && dashed == dashedBorder && Mathf.Approximately(width, borderWidth)) return;
            color = fill; edge = border; dashed = dashedBorder; width = borderWidth;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width < 2 || r.height < 2) return;
            if (color.a > 0) Quad(vh, r.xMin, r.yMin, r.xMax, r.yMax, color);
            if (edge.a <= 0) return;
            float w = Mathf.Max(1, width);
            Edge(vh, r.xMin, r.yMax - w, r.xMax, r.yMax, true);
            Edge(vh, r.xMin, r.yMin, r.xMax, r.yMin + w, true);
            Edge(vh, r.xMin, r.yMin + w, r.xMin + w, r.yMax - w, false);
            Edge(vh, r.xMax - w, r.yMin + w, r.xMax, r.yMax - w, false);
        }

        void Edge(VertexHelper vh, float x0, float y0, float x1, float y1, bool horizontal)
        {
            if (!dashed) { Quad(vh, x0, y0, x1, y1, edge); return; }
            const float on = 4, off = 3;
            if (horizontal) for (float x = x0; x < x1; x += on + off) Quad(vh, x, y0, Mathf.Min(x1, x + on), y1, edge);
            else for (float y = y0; y < y1; y += on + off) Quad(vh, x0, y, x1, Mathf.Min(y1, y + on), edge);
        }

        static void Quad(VertexHelper vh, float x0, float y0, float x1, float y1, Color c)
        {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = c;
            v.position = new Vector3(x0, y0); vh.AddVert(v);
            v.position = new Vector3(x0, y1); vh.AddVert(v);
            v.position = new Vector3(x1, y1); vh.AddVert(v);
            v.position = new Vector3(x1, y0); vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
