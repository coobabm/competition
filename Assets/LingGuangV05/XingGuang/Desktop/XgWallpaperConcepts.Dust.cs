using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgWallpaperConcepts
    {
        readonly int[] dustCounts = new int[4];
        readonly Color[] dustColors = new Color[4];
        public int CollapsedCount { get; private set; }
        public int DustGroups(int region) => region >= 0 && region < 4 ? Mathf.Min(16, Mathf.CeilToInt(Mathf.Sqrt(dustCounts[region]))) : 0;
        public int CollapsedInRegion(int region) => region >= 0 && region < 4 ? dustCounts[region] : 0;

        void ClearDust()
        {
            CollapsedCount = 0;
            System.Array.Clear(dustCounts, 0, 4); System.Array.Clear(dustColors, 0, 4);
        }

        void GatherDust(LingGuangV05.XingGuang.XgConcept concept)
        {
            int region = Region(concept.region); dustCounts[region]++; CollapsedCount++;
            // Running mean stays bounded even for very large late-game boards.
            dustColors[region] = Color.Lerp(dustColors[region], Tint(concept), 1f / dustCounts[region]);
        }

        void DrawDust(VertexHelper vh, Rect area, float scale)
        {
            // Retain the aggregation data and region description, but omit decorative dust in Reduced mode.
            if (StructureReduced) return;
            for (int region = 0; region < 4; region++)
            {
                int groups = DustGroups(region);
                for (int i = 0; i < groups; i++)
                {
                    float angle = i * 2.39996323f + region * .7f;
                    float radius = .066f + i % 3 * .002f;
                    var uv = RegionCenter(region) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    var at = area.min + Vector2.Scale(area.size, uv);
                    var ink = dustColors[region]; ink.a = .5f;
                    // Each small cloud aggregates many real off-screen concepts, without individual IDs or invented links.
                    XgSoftDraw.Halo(vh, at, (3 + Mathf.Log(1 + dustCounts[region] / (float)groups)) * scale, new Color(ink.r, ink.g, ink.b, .08f), 8);
                    XgDraw.Box(vh, at - Vector2.one * scale, at + Vector2.one * scale, ink);
                }
            }
        }
    }
}
