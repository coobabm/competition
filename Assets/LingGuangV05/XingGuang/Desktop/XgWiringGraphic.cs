using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// One layer of the 接线 canvas, drawn by <see cref="XgWiringPage"/>: the back layer has the scanner grid and the wires
    /// and takes clicks (unplug a wire); the front layer has the ports, the energy pulses and
    /// the work items and never takes input. Pivot top-left: local (x, −y) is the canvas pixel (x, y).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgWiringGraphic : MaskableGraphic, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public XgWiringPage page;
        public bool front;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (page == null) return;
            if (front) page.DrawFront(vh); else page.DrawBack(vh);
        }
        public void OnPointerClick(PointerEventData e) { if (!front && page != null) page.CanvasClick(e); }
        public void OnBeginDrag(PointerEventData e) { if (!front && page != null) page.BeginPan(e); }
        public void OnDrag(PointerEventData e) { if (!front && page != null) page.DragPan(e); }
        public void OnEndDrag(PointerEventData e) { if (!front && page != null) page.EndPan(e); }
        public void OnScroll(PointerEventData e) { if (page != null) page.CanvasScroll(e); }
    }

    public sealed class XgWiringHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public XgWiringPage page;
        public string key;
        public void OnPointerEnter(PointerEventData e) { if (page != null) page.Hover(key, true); }
        public void OnPointerExit(PointerEventData e) { if (page != null) page.Hover(key, false); }
        public void OnBeginDrag(PointerEventData e) { if (page != null) page.BeginNodeDrag(key, e); }
        public void OnDrag(PointerEventData e) { if (page != null) page.DragNode(e); }
        public void OnEndDrag(PointerEventData e) { if (page != null) page.EndNodeDrag(); }
        public void OnScroll(PointerEventData e) { if (page != null) page.CanvasScroll(e); }
        void OnDisable() { if (page != null) page.Hover(key, false); }
    }

    public sealed class XgWiringPort : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public XgWiringPage page;
        public string key;
        public bool input;
        public void OnBeginDrag(PointerEventData e) { if (page != null && e.button == PointerEventData.InputButton.Left) page.BeginPortDrag(key, input, e); }
        public void OnDrag(PointerEventData e) { if (page != null && e.button == PointerEventData.InputButton.Left) page.DragPort(e); }
        public void OnDrop(PointerEventData e) { if (page != null && e.button == PointerEventData.InputButton.Left && e.pointerDrag != null && e.pointerDrag.GetComponent<XgWiringPort>() != null && e.pointerDrag.GetComponent<XgWiringPort>().page == page) page.DropPort(key, input); }
        public void OnEndDrag(PointerEventData e) { if (page != null && e.button == PointerEventData.InputButton.Left) page.EndPortDrag(); }
    }

    /// <summary>Soft (anti-aliased, glowing) primitives in the style of the 皮层拓扑 view (XgCortexGraphic).</summary>
    public static class XgSoftDraw
    {
        static Color Clear(Color c) { c.a = 0; return c; }

        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
        {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.position = a; v.color = ca; vh.AddVert(v);
            v.position = b; v.color = cb; vh.AddVert(v);
            v.position = c; v.color = cc; vh.AddVert(v);
            v.position = d; v.color = cd; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>A line with a solid core and edges fading to nothing (a glow when the feather is wide).</summary>
        public static void Seg(VertexHelper vh, Vector2 a, Vector2 b, float core, float feather, Color col)
        {
            var d = b - a; if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized;
            Vector2 c0 = n * (core * .5f), f0 = n * (core * .5f + feather);
            var edge = Clear(col);
            Quad(vh, a - c0, a + c0, b + c0, b - c0, col, col, col, col);
            if (feather <= 0) return;
            Quad(vh, a + c0, a + f0, b + f0, b + c0, col, edge, edge, col);
            Quad(vh, a - f0, a - c0, b - c0, b - f0, edge, col, col, edge);
        }

        /// <summary>A disc brightest at the centre that fades out at the rim.</summary>
        public static void Halo(VertexHelper vh, Vector2 c, float r, Color col, int n = 16)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = col; v.position = c; vh.AddVert(v);
            v.color = Clear(col);
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.position = c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r); vh.AddVert(v); }
            for (int i = 0; i < n; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % n);
        }

        /// <summary>A solid disc with a feathered rim.</summary>
        public static void Disc(VertexHelper vh, Vector2 c, float r, float feather, Color col, int n = 16)
        {
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = col; v.position = c; vh.AddVert(v);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                v.color = col; v.position = c + dir * r; vh.AddVert(v);
                v.color = Clear(col); v.position = c + dir * (r + feather); vh.AddVert(v);
            }
            for (int i = 0; i < n; i++)
            {
                int a = start + 1 + i * 2, b = start + 1 + (i + 1) % n * 2;
                vh.AddTriangle(start, a, b);
                vh.AddTriangle(a, a + 1, b + 1); vh.AddTriangle(a, b + 1, b);
            }
        }

        /// <summary>A thin ring (port outlines, arrival ripples).</summary>
        public static void Ring(VertexHelper vh, Vector2 c, float r, float w, Color col, int n = 24)
        {
            Vector2 prev = c + new Vector2(r, 0);
            for (int i = 1; i <= n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                var p = c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                Seg(vh, prev, p, w, .8f, col);
                prev = p;
            }
        }

        /// <summary>A point on a cubic Bézier.</summary>
        public static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float u = 1 - t;
            return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
        }
    }
}
