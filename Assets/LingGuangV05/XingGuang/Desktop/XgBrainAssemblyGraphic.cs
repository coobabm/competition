using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Static brain silhouette and model-membership links. No activity or inference is simulated.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgBrainAssemblyGraphic : MaskableGraphic
    {
        public RectTransform Core;
        public readonly List<RectTransform> Members = new List<RectTransform>();
        public readonly List<Color> MemberColors = new List<Color>();
        public bool Highlighted;

        // A symmetric, gently lobed silhouette with a central cleft, in normalized canvas coordinates.
        static readonly Vector2[] BrainEdge =
        {
            new Vector2(.50f,.10f), new Vector2(.44f,.055f), new Vector2(.36f,.045f), new Vector2(.28f,.065f),
            new Vector2(.20f,.06f), new Vector2(.13f,.12f), new Vector2(.09f,.22f), new Vector2(.045f,.29f),
            new Vector2(.035f,.43f), new Vector2(.065f,.52f), new Vector2(.045f,.63f), new Vector2(.09f,.75f),
            new Vector2(.17f,.79f), new Vector2(.22f,.88f), new Vector2(.34f,.93f), new Vector2(.50f,.90f),
            new Vector2(.66f,.93f), new Vector2(.78f,.88f), new Vector2(.83f,.79f), new Vector2(.91f,.75f),
            new Vector2(.955f,.63f), new Vector2(.935f,.52f), new Vector2(.965f,.43f), new Vector2(.955f,.29f),
            new Vector2(.91f,.22f), new Vector2(.87f,.12f), new Vector2(.80f,.06f), new Vector2(.72f,.065f),
            new Vector2(.64f,.045f), new Vector2(.56f,.055f)
        };

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            Vector2 Point(Vector2 p) => new Vector2(r.xMin + p.x * r.width, r.yMax - p.y * r.height);
            Ellipse(vh, Point(new Vector2(.25f,.40f)), new Vector2(r.width * .20f, r.height * .31f), new Color(.10f,.48f,.47f,.035f));
            Ellipse(vh, Point(new Vector2(.75f,.40f)), new Vector2(r.width * .20f, r.height * .31f), new Color(.44f,.32f,.68f,.035f));
            Ellipse(vh, Point(new Vector2(.50f,.76f)), new Vector2(r.width * .22f, r.height * .17f), new Color(.62f,.43f,.11f,.035f));
            Color edge = Highlighted ? new Color(.10f,.48f,.47f,.72f) : new Color(.44f,.59f,.61f,.30f);
            for (int i = 0; i < BrainEdge.Length; i++)
            {
                Vector2 a = Point(BrainEdge[(i + BrainEdge.Length - 1) % BrainEdge.Length]);
                Vector2 b = Point(BrainEdge[i]), c = Point(BrainEdge[(i + 1) % BrainEdge.Length]), d = Point(BrainEdge[(i + 2) % BrainEdge.Length]);
                Vector2 previous = b;
                for (int step = 1; step <= 8; step++)
                {
                    float t = step / 8f, t2 = t * t, t3 = t2 * t;
                    Vector2 p = .5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3);
                    XgSoftDraw.Seg(vh, previous, p, Highlighted ? 2 : 1.2f, .55f, edge); previous = p;
                }
            }
            if (Core == null) return;
            Vector2 origin = rectTransform.InverseTransformPoint(Core.TransformPoint(Core.rect.center));
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i] == null || !Members[i].gameObject.activeSelf) continue;
                Vector2 end = rectTransform.InverseTransformPoint(Members[i].TransformPoint(Members[i].rect.center));
                Color color = i < MemberColors.Count ? MemberColors[i] : edge; color.a = .55f;
                Vector2 control = new Vector2(end.x, origin.y), previous = origin;
                for (int s = 1; s <= 20; s++)
                {
                    float t = s / 20f;
                    Vector2 p = (1 - t) * (1 - t) * origin + 2 * (1 - t) * t * control + t * t * end;
                    XgSoftDraw.Seg(vh, previous, p, 1.6f, .6f, color); previous = p;
                }
            }
        }

        static void Ellipse(VertexHelper vh, Vector2 center, Vector2 radius, Color color)
        {
            int first = vh.currentVertCount; var vertex = UIVertex.simpleVert;
            vertex.position = center; vertex.color = color; vh.AddVert(vertex);
            const int steps = 48;
            for (int i = 0; i < steps; i++)
            {
                float angle = i * Mathf.PI * 2 / steps;
                vertex.position = center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y); vh.AddVert(vertex);
            }
            for (int i = 0; i < steps; i++) vh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % steps);
        }
    }
}
