#if UNITY_EDITOR
using System;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.UI;

public static class TechDisplayGeometryChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    public static void Run()
    {
        var slab = typeof(XgTechLinksGraphic).GetMethod("BlockedInterval", BindingFlags.Static | BindingFlags.NonPublic);
        bool Hit(Vector2 a, Vector2 b, Rect rect, float padding, out Vector2 range)
        {
            object[] args = { a, b, rect, padding, Vector2.zero };
            bool result = (bool)slab.Invoke(null, args); range = (Vector2)args[4]; return result;
        }
        var box = new Rect(-20, -80, 40, 60);
        Check(Hit(Vector2.zero, new Vector2(0, -100), box, 0, out var cut)
            && Mathf.Approximately(cut.x, .2f) && Mathf.Approximately(cut.y, .8f), "Vertical link must leave the description clear");
        Check(Hit(new Vector2(0, -100), Vector2.zero, box, 0, out cut)
            && Mathf.Approximately(cut.x, .2f) && Mathf.Approximately(cut.y, .8f), "Reversed link clips identically");
        Check(!Hit(Vector2.zero, Vector2.right * 100, box, 3, out cut), "Horizontal icon links remain intact");
        Check(Hit(new Vector2(-100, -50), new Vector2(100, -50), box, 3, out cut)
            && Mathf.Approximately(cut.x, .385f) && Mathf.Approximately(cut.y, .615f), "Stroke width and gutter are excluded");

        var stage = new GameObject("Tech display geometry fixture", typeof(RectTransform));
        try
        {
            var graphGo = new GameObject("Links", typeof(RectTransform), typeof(CanvasRenderer), typeof(XgTechLinksGraphic));
            graphGo.transform.SetParent(stage.transform, false);
            var ownerGo = new GameObject("Node", typeof(RectTransform)); ownerGo.transform.SetParent(stage.transform, false);
            var owner = (RectTransform)ownerGo.transform;
            var graph = graphGo.GetComponent<XgTechLinksGraphic>();
            graph.Exclude(owner, box);
            int segment = graph.Add(Vector2.zero, new Vector2(0, -100), 2); graph.Paint(segment, Color.yellow);
            var populate = typeof(XgTechLinksGraphic).GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { typeof(VertexHelper) }, null);
            using (var vh = new VertexHelper())
            {
                populate.Invoke(graph, new object[] { vh });
                Check(vh.currentVertCount == 8, "Text interruption produces two quads");
                var vertex = new UIVertex();
                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref vertex, i);
                    Check(vertex.position.y >= -17.01f || vertex.position.y <= -82.99f, "No line vertex inside padded text bounds");
                }
                owner.localScale = Vector3.one * .4f;
                populate.Invoke(graph, new object[] { vh });
                Check(vh.currentVertCount == 8, "Reveal animation retains the full text exclusion");
                owner.anchoredPosition = Vector2.down * 200; graph.Move(segment, Vector2.down * 200);
                populate.Invoke(graph, new object[] { vh });
                Check(vh.currentVertCount == 8, "Growth layout moves retain text exclusion");
                ownerGo.SetActive(false);
                populate.Invoke(graph, new object[] { vh });
                Check(vh.currentVertCount == 4, "Hidden nodes do not cut unrelated links");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(stage); }

        var geometry = typeof(XgTechTreePage).Assembly.GetType("LingGuangV05.Desktop.XingGuang.XgTechTooltipGeometry");
        var place = geometry.GetMethod("Place", BindingFlags.NonPublic | BindingFlags.Static);
        int cases = 0;
        foreach (float width in new[] { 240f, 800f, 1400f })
        foreach (float height in new[] { 240f, 600f, 900f })
        foreach (float zoom in new[] { .5f, 1f, 1.5f })
        foreach (float nodeX in new[] { -200f, 80f, 500f, 1600f })
        foreach (float nodeTop in new[] { 50f, 150f, 350f, 800f })
        {
            // Header reserves 78px; a 40px taskbar clips the lower edge.
            float bottom = 40, top = height - 78;
            float nodeBottom = nodeTop - 156 * zoom;
            float w = Mathf.Min(312, width - 8);
            float free = Mathf.Max(top - 4 - nodeTop - 10, nodeBottom - bottom - 8);
            float h = Mathf.Min(300, Mathf.Max(1, Mathf.Min(top - bottom - 8, free)));
            object[] args = { 0f, bottom, width, top, w, h, nodeX, nodeTop, nodeBottom, 0f, 0f };
            place.Invoke(null, args);
            float x = (float)args[9], y = (float)args[10];
            Check(x - w / 2 >= 3.99f && x + w / 2 <= width - 3.99f, "Tooltip horizontal viewport clamp");
            Check(y <= top - 3.99f && y - h >= bottom + 3.99f, "Tooltip stays below header and above taskbar");
            cases++;
        }
        Debug.Log("TECH_DISPLAY_GEOMETRY_PASS: clipping mesh, stable layout exclusions, " + cases + " tooltip bounds cases");
    }
}
#endif
