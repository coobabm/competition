using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Read-only projection of learned concepts. No demonstration/random cells.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed partial class XgWallpaperConcepts : MaskableGraphic
    {
        public struct Cell
        {
            public int id;
            public Vector2 uv;
            public Color tint;
            public float strength;
        }
        public struct Link
        {
            public int from, to;
            public Vector2 a, b;
            public float strength;
        }
        public struct Pin
        {
            public int id;
            public Vector2 uv;
            public float inset;
            public bool seed;
        }
        readonly List<Cell> cells = new List<Cell>();
        readonly List<Link> links = new List<Link>();
        readonly List<Pin> pins = new List<Pin>();
        readonly Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();
        readonly HashSet<long> linkedPairs = new HashSet<long>();
        readonly int[] regionCounts = new int[4];
        XingGuangController controller;
        XgBrainWallpaper wallpaper;
        XgBrainWallpaperPresentation presentation;
        Transform initialCenter;
        float next;
        // Keep a UI mesh below Unity's vertex limit. Distant-cell LOD is handled
        // separately from the one-to-one foreground concepts.
        public const int MeshCellLimit = 512;
        public IReadOnlyList<Cell> Cells => cells;
        public IReadOnlyList<Link> Links => links;
        public IReadOnlyList<Pin> Pins => pins;

        public static void Install(XingGuangController controller, XgBrainWallpaper wallpaper)
        {
            var rect = PrologueDesk.Rect("Learned Concepts", wallpaper.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.SetAsFirstSibling(); // Wires run behind the input/output and seed glyphs.
            var graphic = rect.gameObject.AddComponent<XgWallpaperConcepts>();
            graphic.raycastTarget = false; graphic.controller = controller; graphic.wallpaper = wallpaper;
            graphic.presentation = wallpaper.GetComponent<XgBrainWallpaperPresentation>();
            graphic.initialCenter = wallpaper.transform.Find("Center");
            graphic.BuildRegionLabels();
            graphic.ObserveLessonSource();
        }

        public static float Strength(XgConcept concept)
        {
            if (double.IsNaN(concept.w) || double.IsNaN(concept.s)) return 0;
            return Mathf.Sqrt(Mathf.Clamp01((float)(Math.Abs(concept.w) / XgBoard.WeightLimit))
                * Mathf.Clamp01((float)(concept.s / 10)));
        }

        public static Color Tint(XgConcept concept)
        {
            var tint = concept.w > 0 ? XgCortexGraphic.N.Good : concept.w < 0 ? XgCortexGraphic.N.Bad : XgCortexGraphic.N.Dim;
            float brightness = Mathf.Lerp(.24f, 1, Strength(concept));
            return new Color(tint.r * brightness, tint.g * brightness, tint.b * brightness, 1);
        }

        static int Region(string region)
        {
            switch (region) { case "vision": return 0; case "sequence": return 1; case "tone": return 2; default: return 3; }
        }

        void Update()
        {
            ObserveLessonSource();
            TickLessonPulse();
            if (wallpaper == null || controller == null || controller.runtime == null || !controller.runtime.isActiveAndEnabled) return;
            float reveal = Mathf.SmoothStep(0, 1, (wallpaper.Reveal.Age - XgWallpaperReveal.CompleteSeconds) / .6f);
            // A completed lesson may glow immediately during the initial region fade.
            float alpha = (presentation != null ? presentation.LightLevel : 1)
                * (wallpaper.Reveal.Visible ? Mathf.Max(reveal, LessonPulse) : 0);
            canvasRenderer.SetAlpha(alpha); SetRegionAlpha(alpha);
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + .1f;
            cells.Clear(); positions.Clear(); links.Clear(); pins.Clear(); linkedPairs.Clear(); Array.Clear(regionCounts, 0, regionCounts.Length);
            var board = controller.Sim?.S.board;
            SyncRegions(board);
            if (wallpaper.Reveal.Visible && board?.concepts != null)
                for (int pass = 0; pass < 2; pass++)
                foreach (var concept in board.concepts)
                {
                    // Protected endgame rules keep a foreground slot even when
                    // the rest of the board exceeds the visual cell limit.
                    if (concept == null || concept.pinned != (pass == 0)) continue;
                    if (concept.seed)
                    {
                        if (concept.region == "logic" && concept.key == XgSim.SeedKey)
                        {
                            positions[concept.id] = XgBrainWallpaper.SeedPosition;
                            if (concept.pinned) pins.Add(new Pin { id = concept.id, uv = XgBrainWallpaper.SeedPosition, inset = 17, seed = true });
                        }
                        continue;
                    }
                    if (cells.Count == MeshCellLimit) continue;
                    int region = Region(concept.region), index = regionCounts[region]++;
                    var position = RegionCenter(region) + Vector2.Scale(new Vector2(.06f, .06f), LayoutPosition(architectures[region], index, regionTotals[region]));
                    positions[concept.id] = position;
                    cells.Add(new Cell { id = concept.id, uv = position, tint = Tint(concept), strength = Strength(concept) });
                    if (concept.pinned) pins.Add(new Pin { id = concept.id, uv = position, inset = 8 });
                }
            if (wallpaper.Reveal.Visible && board?.links != null)
                foreach (var link in board.links)
                {
                    if (links.Count >= MeshCellLimit * 4) break;
                    if (link == null || link.a == link.b || link.c < 0 || double.IsNaN(link.c)
                        || !positions.TryGetValue(link.a, out var a) || !positions.TryGetValue(link.b, out var b)) continue;
                    long pair = ((long)Math.Min(link.a, link.b) << 32) | (uint)Math.Max(link.a, link.b);
                    if (!linkedPairs.Add(pair)) continue;
                    links.Add(new Link { from = link.a, to = link.b, a = a, b = b, strength = Mathf.Clamp01((float)(link.c / XgBoard.LinkThreshold)) });
                }
            if (initialCenter != null) initialCenter.gameObject.SetActive(cells.Count == 0 || reveal < 1);
            SetVerticesDirty();
        }

        public bool TryLocalPosition(int id, out Vector2 point)
        {
            var area = rectTransform.rect;
            if (positions.TryGetValue(id, out var uv)) { point = area.min + Vector2.Scale(area.size, uv); return true; }
            point = default; return false;
        }

        public Vector2 RivetPoint(Pin pin, int corner)
        {
            var area = rectTransform.rect;
            float scale = pin.seed ? 1 : Mathf.Min(area.width / 1920, area.height / 1080);
            return area.min + Vector2.Scale(area.size, pin.uv) + new Vector2(corner % 2 == 0 ? -1 : 1, corner < 2 ? -1 : 1) * pin.inset * scale;
        }

        public int HitConcept(Vector2 screen, Camera camera)
        {
            if (!wallpaper.Reveal.Visible || canvasRenderer.GetAlpha() < .1f
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, camera, out var local)) return -1;
            var area = rectTransform.rect;
            float radius = 9 * Mathf.Min(area.width / 1920, area.height / 1080);
            float nearest = radius * radius; int result = -1;
            foreach (var cell in cells)
            {
                var position = area.min + Vector2.Scale(area.size, cell.uv);
                float distance = (position - local).sqrMagnitude;
                if (distance <= nearest) { nearest = distance; result = cell.id; }
            }
            return result;
        }

        public string DescribeConcept(int id)
        {
            if (!positions.ContainsKey(id) || controller?.Sim?.S.board?.concepts == null) return null;
            foreach (var concept in controller.Sim.S.board.concepts)
                if (concept != null && concept.id == id)
                {
                    string name = (concept.key ?? "").Replace("+", " + ");
                    if (!string.IsNullOrEmpty(concept.alt)) name += " / " + concept.alt.Replace("+", " + ");
                    name = name.Replace("<", "‹").Replace(">", "›");
                    if (name.Length > 120) name = name.Substring(0, 120) + "…";
                    string direction = concept.w > 0 ? GameText.T("是", "Yes") : concept.w < 0 ? GameText.T("否", "No") : GameText.T("未偏向", "Undecided");
                    string strength = double.IsNaN(concept.w) || double.IsInfinity(concept.w) ? "—" : Math.Abs(concept.w).ToString("0.0", CultureInfo.InvariantCulture);
                    long count = Math.Max(0, concept.trainedCards);
                    string cards = concept.trainingCountKnown ? count + GameText.T(" 张", " cards")
                        : count > 0 ? GameText.T("至少 ", "At least ") + count + GameText.T(" 张", " cards") : GameText.T("历史张数未记录", "Past card count unavailable");
                    return name + " · " + direction + " " + strength + " · " + cards + (concept.pinned ? GameText.T(" · 底层规则", " · Ground rule") : "");
                }
            return null;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var area = GetPixelAdjustedRect();
            float scale = Mathf.Min(area.width / 1920, area.height / 1080);
            DrawRegions(vh, area, scale);
            DrawLessonHalo(vh, area, scale);
            foreach (var link in links)
            {
                Vector2 a = area.min + Vector2.Scale(area.size, link.a), b = area.min + Vector2.Scale(area.size, link.b);
                var ink = XgCortexGraphic.N.Wire;
                ink.a = Mathf.Lerp(.18f, .55f, link.strength);
                float width = Mathf.Lerp(.7f, 1.5f, link.strength) * scale;
                var glow = ink; glow.a *= .15f;
                XgDraw.Seg(vh, a, b, width * 4, glow);
                XgDraw.Seg(vh, a, b, width, ink);
            }
            foreach (var cell in cells)
            {
                var point = area.min + Vector2.Scale(area.size, cell.uv);
                float radius = Mathf.Lerp(2.7f, 4.8f, cell.strength) * scale;
                var tint = LessonTint(cell);
                var halo = tint; halo.a = .09f;
                XgDraw.Disc(vh, point, radius * 2.8f, halo, 12);
                halo.a = .16f; XgDraw.Disc(vh, point, radius * 1.8f, halo, 12);
                XgDraw.Disc(vh, point, radius, tint, 12);
            }
            foreach (var pin in pins)
                for (int corner = 0; corner < 4; corner++)
                {
                    var point = RivetPoint(pin, corner);
                    float rivetScale = pin.seed ? 1 : scale;
                    XgDraw.Disc(vh, point, 2.5f * rivetScale, new Color32(24, 33, 43, 255), 10);
                    XgDraw.Disc(vh, point, 1.6f * rivetScale, new Color32(174, 187, 196, 255), 10);
                    XgDraw.Disc(vh, point + new Vector2(-.45f, .45f) * rivetScale, .55f * rivetScale, new Color32(235, 242, 246, 255), 8);
                }
        }
    }
}
