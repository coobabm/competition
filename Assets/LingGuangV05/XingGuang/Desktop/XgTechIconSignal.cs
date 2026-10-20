using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Authored atlas paths, animated inside the icon; never moves the node or its hit rectangle.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgTechIconSignal : MaskableGraphic
    {
        [Serializable] public sealed class Point { public float x, y; public bool start; }
        [Serializable] public sealed class Path { public string id; public Point[] points; public int activeMs, restMs; }
        [Serializable] sealed class Atlas { public Path[] nodes; }
        static Dictionary<string, Path> paths;
        XgTechTreePage page;
        Path path;
        bool requested, concealed;
        float began, phase, strength = 1;
        bool reduced;

        static Path Find(string id)
        {
            if (paths == null)
            {
                paths = new Dictionary<string, Path>();
                var text = Resources.Load<TextAsset>("LingGuangV05/Icons/tech/signal_paths");
                var atlas = text == null ? null : JsonUtility.FromJson<Atlas>(text.text);
                if (atlas?.nodes != null) foreach (var p in atlas.nodes) paths[p.id] = p;
            }
            return id != null && paths.TryGetValue(id, out var value) ? value : null;
        }

        public void Configure(XgTechTreePage owner, string id, bool hidden, bool locked)
        {
            page = owner; path = Find(id); concealed = hidden; strength = locked ? .35f : 1;
            raycastTarget = false;
            enabled = requested && path != null && !concealed;
            SetVerticesDirty();
        }
        public void Show(bool show)
        {
            requested = show; began = Time.unscaledTime; phase = 0;
            reduced = page != null && page.ReducedMotion;
            enabled = show && path != null && !concealed;
            SetVerticesDirty();
        }
        void Update()
        {
            if (page == null || !page.NodeMotionVisible || !Application.isFocused) { Show(false); return; }
            bool nextReduced = page.ReducedMotion;
            float next = nextReduced ? 0 : (Time.unscaledTime - began) % ((path.activeMs + path.restMs) / 1000f);
            if (next != phase || reduced != nextReduced) { phase = next; reduced = nextReduced; SetVerticesDirty(); }
        }
        void OnApplicationFocus(bool focus) { if (!focus) Show(false); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!requested || concealed || path?.points == null || path.points.Length < 2) return;
            float duration = path.activeMs / 1000f;
            if (!reduced && phase >= duration) return;
            var r = GetPixelAdjustedRect();
            float scale = Mathf.Min(r.width, r.height) / 128f;
            float progress = reduced ? 1 : phase / duration;
            float fade = reduced ? .4f : Mathf.Min(1, Mathf.Min(progress * 8, (1 - progress) * 8));
            float cursor = progress * (path.points.Length - 1);
            for (int i = 1; i < path.points.Length; i++)
            {
                if (path.points[i].start) continue;
                float tail = reduced ? 1 : Mathf.Clamp01(1 - (cursor - i) / 15f);
                if (!reduced && (i > cursor || tail <= 0)) continue;
                var p = path.points[i - 1]; var q = path.points[i];
                var a = r.center + new Vector2(p.x - 64, 64 - p.y) * scale;
                var b = r.center + new Vector2(q.x - 64, 64 - q.y) * scale;
                float alpha = strength * fade * tail;
                Segment(vh, a, b, Mathf.Max(2, 5 * scale), new Color(.4f, 1, .84f, alpha * .12f));
                Segment(vh, a, b, Mathf.Max(1, 2 * scale), new Color(.55f, 1, .9f, alpha));
            }
        }
        static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            if ((b - a).sqrMagnitude < .0001f) return;
            Vector2 delta = (b - a).normalized, side = new Vector2(-delta.y, delta.x) * width * .5f;
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert; v.color = tint;
            v.position = a - side; vh.AddVert(v); v.position = a + side; vh.AddVert(v);
            v.position = b + side; vh.AddVert(v); v.position = b - side; vh.AddVert(v);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
