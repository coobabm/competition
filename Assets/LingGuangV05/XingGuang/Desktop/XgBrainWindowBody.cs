using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Static shared anatomy behind the actual learned cells. No demo neurons, polling or animation.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgBrainWindowBody : MaskableGraphic
    {
        public const float DesignWidth = 1280, DesignHeight = 832;
        // Cortex's cropped side-view viewport (x56..824, y18..536), with room for edge glow.
        public static float AnatomyScale => Mathf.Min(DesignWidth * .92f / 768, DesignHeight * .92f / 518);
        public static Vector2 AnatomyUv(Vector2 point) => new Vector2(.5f + (point.x - 440) * AnatomyScale / DesignWidth,
            .5f - (point.y - 277) * AnatomyScale / DesignHeight);
        public static Vector2 AnatomyRadius(Vector2 radius) => new Vector2(radius.x * AnatomyScale / DesignWidth, radius.y * AnatomyScale / DesignHeight);

        public static void Install(XgBrainWallpaper wallpaper)
        {
            var rect = PrologueDesk.Rect("Cortex Anatomy", wallpaper.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.SetAsFirstSibling();
            rect.gameObject.AddComponent<XgBrainWindowBody>().raycastTarget = false;
        }

        Vector2 Point(Rect rect, Vector2 anatomy) => rect.min + Vector2.Scale(rect.size, AnatomyUv(anatomy));

        void Surface(VertexHelper vh, Rect rect, IReadOnlyList<Vector2> outline, Vector2 center, Color fill, float scale)
        {
            int start = vh.currentVertCount;
            vh.AddVert(Point(rect, center), fill, Vector2.zero);
            foreach (var point in outline) vh.AddVert(Point(rect, point), fill, Vector2.zero);
            for (int i = 0; i < outline.Count; i++)
            {
                vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % outline.Count);
                var a = Point(rect, outline[i]); var b = Point(rect, outline[(i + 1) % outline.Count]);
                var glow = XgCortexGraphic.N.Edge; glow.a = .055f;
                XgDraw.Seg(vh, a, b, 7 * scale, glow);
                var edge = XgCortexGraphic.N.Edge; edge.a = .48f;
                XgDraw.Seg(vh, a, b, 1.15f * scale, edge);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect = GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width / DesignWidth, rect.height / DesignHeight);
            Surface(vh, rect, XgCortexGraphic.BrainStemOutline, new Vector2(632, 470), new Color32(17, 26, 64, 105), scale);
            // The faint anatomical surface never substitutes for learned cells or wires.
            Surface(vh, rect, XgCortexGraphic.BrainOutline, new Vector2(430, 270), new Color32(17, 26, 64, 155), scale);
            var ink = XgCortexGraphic.N.Sulcus; ink.a = .36f;
            for (int curve = 0; curve < XgCortexGraphic.SulcusCount; curve++)
                for (int i = 0; i < 16; i++)
                    XgDraw.Seg(vh, Point(rect, XgCortexGraphic.SulcusPoint(curve, i / 16f)),
                        Point(rect, XgCortexGraphic.SulcusPoint(curve, (i + 1) / 16f)), 1.1f * scale, ink);
        }
    }
}
