using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgWallpaperConcepts
    {
        static readonly string[] RegionIds = { "vision", "sequence", "tone", "logic" };
        readonly string[] architectures = { "", "", "tone", "" };
        readonly int[] regionTotals = new int[4];
        readonly TMP_Text[] regionLabels = new TMP_Text[4];
        readonly Vector2[] ellipse = new Vector2[49];
        public System.Collections.Generic.IReadOnlyList<string> Architectures => architectures;
        public static Vector2 RegionCenter(int region) => XgBrainWindowBody.AnatomyUv(XgCortexGraphic.PresentationCenter(region));
        public static Vector2 RegionRadius(int region) => XgBrainWindowBody.AnatomyRadius(XgCortexGraphic.PresentationRadius(region));
        public static Vector2 RegionLabelPosition(int region) => XgBrainWindowBody.AnatomyUv(XgCortexGraphic.PresentationLabel(region));
        // A common anatomical extent keeps real cells and structural guides inside their lobe.
        public static Vector2 RegionContentRadius(int region) => RegionRadius(region) * .62f;
        static Vector2 RegionOffset(int region, Vector2 oldOffset) => Vector2.Scale(RegionContentRadius(region), oldOffset / .06f);
        public static string RegionName(int region) => region == 0 ? GameText.T("看图区", "Vision") : region == 1 ? GameText.T("读字区", "Reading")
            : region == 2 ? GameText.T("语气区", "Tone") : GameText.T("推理区", "Reasoning");
        public int HitRegion(Vector2 screen, Camera camera)
        {
            if (!wallpaper.Reveal.Visible || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, camera, out var local)) return -1;
            var area = rectTransform.rect;
            var uv = new Vector2((local.x - area.xMin) / area.width, (local.y - area.yMin) / area.height);
            for (int region = 0; region < 4; region++)
            {
                var offset = uv - RegionCenter(region);
                var radius = RegionRadius(region);
                if (offset.x * offset.x / (radius.x * radius.x) + offset.y * offset.y / (radius.y * radius.y) <= 1) return region;
            }
            return -1;
        }

        public string RegionStructure(int region)
        {
            if (region == 2) return GameText.T("三条性格轴", "Three personality axes");
            var arch = controller.Sim?.RegionWiringOrLast(RegionIds[region]);
            return arch != null ? GameText.T(arch.name, arch.nameEn) : GameText.T("等待学习", "Waiting to learn");
        }

        public string RegionLore(int region)
        {
            if (region == 2) return GameText.T("它从你教的内容和对话里的赞、踩，慢慢学出自己的语气。\n\n温度决定冷静还是热情，玩心决定正经还是调皮，主见决定顺从还是坚持自己的看法。外圈光晕也会跟着变。",
                "It learns its tone from your lessons and ratings in conversation.\n\nWarmth shapes how reserved or friendly it is; play shapes how serious or cheeky; opinion shapes how readily it agrees. The outer glow changes with them.");
            var arch = controller.Sim?.RegionWiringOrLast(RegionIds[region]);
            if (arch == null) return GameText.T("这个区还没有接上学习结构。继续教它、练它，学到的概念会在这里亮起来。", "This area has no learning structure yet. Keep teaching and training it, and learned concepts will light up here.");
            string lore = XgItemLore.Get(arch.id, GameText.IsEnglish);
            string text = string.IsNullOrEmpty(lore) ? GameText.T(arch.note, arch.noteEn) : lore;
            if (CollapsedInRegion(region) > 0) text += GameText.T("\n\n远处星尘合并显示 ", "\n\nDistant stardust represents ")
                + CollapsedInRegion(region) + GameText.T(" 个概念，学习数据完整保留。", " concepts; all learned data is retained.");
            if (arch.id == "attention") text += GameText.T("\n\n教会一句后，光束回看原句前几个字。这是注意力结构示意，不是模型真实权重。", "\n\nAfter a lesson, the beam revisits its opening characters. This illustrates attention structure, not actual model weights.");
            if (arch.id == "perceptron" || arch.id == "mlp" || arch.id == "transformer") text += GameText.T("\n\n空心点是结构位置，实心亮点是已经学到的概念。", "\n\nHollow dots mark structure; filled glowing dots are learned concepts.");
            return text;
        }

        void BuildRegionLabels()
        {
            for (int i = 0; i < 4; i++)
            {
                var center = RegionLabelPosition(i);
                var label = PrologueDesk.Rect("Region " + RegionIds[i], transform, center, center,
                    new Vector2(-150, -16), new Vector2(150, 16)).gameObject.AddComponent<TextMeshProUGUI>();
                label.font = PrologueDesk.CjkFont(); label.fontSize = 24; label.color = new Color32(144, 170, 186, 255);
                label.alignment = TextAlignmentOptions.Center; label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = 24;
                label.raycastTarget = false; regionLabels[i] = label;
            }
        }

        void SetRegionAlpha(float alpha)
        {
            foreach (var label in regionLabels) if (label != null) label.canvasRenderer.SetAlpha(alpha);
        }

        void SyncRegions(XgBoardState board)
        {
            System.Array.Clear(regionTotals, 0, regionTotals.Length);
            int count = 0;
            if (wallpaper.Reveal.Visible && board?.concepts != null)
                for (int pass = 0; pass < 2; pass++)
                foreach (var concept in board.concepts)
                {
                    if (concept == null || concept.seed || concept.pinned != (pass == 0)) continue;
                    if (count >= MeshCellLimit) break;
                    count++;
                    regionTotals[Region(concept.region)]++;
                }
            for (int i = 0; i < 4; i++)
            {
                var arch = controller.Sim?.RegionWiringOrLast(RegionIds[i]);
                architectures[i] = i == 2 ? "tone" : arch?.id ?? "";
                string title = RegionName(i);
                string structure = i == 2 ? GameText.T("三条性格轴", "Three personality axes")
                    : arch != null ? GameText.T(arch.name, arch.nameEn) : GameText.T("等待学习", "Waiting to learn");
                if (regionLabels[i] != null) regionLabels[i].text = title + " · " + structure;
            }
        }

        static float Spread(int index, int count) => count <= 1 ? 0 : -1 + 2f * index / (count - 1);

        // Every position belongs to an actual concept. Architecture changes move
        // the same concept IDs; no decorative neurons are substituted for them.
        public static Vector2 LayoutPosition(string arch, int index, int count)
        {
            if (count <= 1) return Vector2.zero;
            switch (arch)
            {
                case "perceptron": return new Vector2(0, Spread(index, count));
                case "mlp": case "tone":
                    return new Vector2(Spread(index % 3, 3), Spread(index / 3, (count + 2) / 3));
                case "lenet": case "alexnet": case "vgg": case "googlenet": case "resnet": case "textcnn": case "caption":
                    int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
                    return new Vector2(Spread(index % columns, columns), Spread(index / columns, (count + columns - 1) / columns));
                case "rnn": case "lstm": case "gru":
                    float angle = Mathf.PI * .5f - index * Mathf.PI * 2 / count;
                    return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                case "seq2seq": case "attention":
                    int left = (count + 1) / 2;
                    bool encoder = index < left;
                    int local = encoder ? index : index - left, size = encoder ? left : count - left;
                    float phase = Mathf.PI * .5f - local * Mathf.PI * 2 / Mathf.Max(1, size);
                    return new Vector2((encoder ? -.55f : .55f) + Mathf.Cos(phase) * .42f, Mathf.Sin(phase) * .8f);
                default:
                    float radius = arch == "transformer" ? Mathf.Sqrt((index + .5f) / count) : Mathf.Min(.95f, Mathf.Sqrt(index) * .103f);
                    float turn = index * 2.39996323f;
                    return new Vector2(Mathf.Cos(turn), Mathf.Sin(turn)) * radius;
            }
        }

        void EllipseLine(VertexHelper vh, Vector2 center, Vector2 radius, float width, Color ink)
        {
            for (int i = 0; i < ellipse.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / (ellipse.Length - 1);
                ellipse[i] = center + Vector2.Scale(radius, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
            }
            XgDraw.Polyline(vh, ellipse, width, ink);
        }

        void DrawRegions(VertexHelper vh, Rect area, float scale)
        {
            if (wallpaper == null || !wallpaper.Reveal.Visible) return;
            for (int i = 0; i < 4; i++)
            {
                var center = area.min + Vector2.Scale(area.size, RegionCenter(i));
                var radius = Vector2.Scale(area.size, RegionContentRadius(i));
                var ink = XgCortexGraphic.N.Wire; ink.a = regionTotals[i] > 0 ? .2f : .065f;
                EllipseLine(vh, center, Vector2.Scale(area.size, RegionRadius(i)), .6f * scale, ink);
                string arch = architectures[i];
                if (arch == "rnn" || arch == "lstm" || arch == "gru")
                {
                    EllipseLine(vh, center, radius, scale, ink);
                    DrawRecurrentSignal(vh, area, scale, i);
                    DrawMemoryGates(vh, area, scale, i, arch);
                }
                else if (arch == "seq2seq") DrawSequenceBottleneck(vh, area, scale, i);
                else if (arch == "attention")
                {
                    EllipseLine(vh, center - new Vector2(radius.x * .55f, 0), Vector2.Scale(radius, new Vector2(.42f, .8f)), scale, ink);
                    EllipseLine(vh, center + new Vector2(radius.x * .55f, 0), Vector2.Scale(radius, new Vector2(.42f, .8f)), scale, ink);
                    XgDraw.Seg(vh, center - new Vector2(0, 8 * scale), center + new Vector2(0, 8 * scale), (arch == "attention" ? 3 : 1) * scale, ink);
                    DrawAttentionBeam(vh, area, scale);
                }
                else if (IsConvolution(arch)) DrawConvolution(vh, area, scale, i);
                else if (arch == "perceptron") DrawStructureSockets(vh, area, scale, i, false);
                else if (arch == "mlp") DrawMlpGuides(vh, area, scale, i);
                else if (arch == "tone")
                    for (int column = 0; column < 3; column++)
                        XgDraw.Seg(vh, center + new Vector2(Spread(column, 3) * radius.x, -radius.y), center + new Vector2(Spread(column, 3) * radius.x, radius.y), scale, ink);
                else if (arch == "transformer") DrawNebula(vh, area, scale, i);
            }
        }
    }
}
