using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgWallpaperConcepts
    {
        const float StructureSeconds = .7f;
        readonly Dictionary<int, Vector2> structureOrigins = new Dictionary<int, Vector2>();
        readonly string[] structureIds = new string[4];
        readonly bool[] movingRegions = new bool[4];
        readonly float[] structureClocks = new float[4];
        readonly List<Cell>[] structureColumns = { new List<Cell>(), new List<Cell>(), new List<Cell>() };
        XgBoardState structureBoard;
        float structureStarted = -100;
        readonly List<string> attentionWords = new List<string>();
        readonly TMPro.TMP_Text[] attentionLabels = new TMPro.TMP_Text[6];
        float attentionStarted;
        public int AttentionCount => attentionWords.Count;
        public int AttentionIndex => attentionWords.Count == 0 || structureIds[1] != "attention" || !isActiveAndEnabled ? -1
            : StructureReduced ? 0 : Mathf.FloorToInt(Mathf.Max(0, Time.unscaledTime - attentionStarted) / .65f) % attentionWords.Count;

        void ClearAttention()
        {
            attentionWords.Clear();
            foreach (var label in attentionLabels) if (label != null) label.gameObject.SetActive(false);
        }

        void RememberAttention(XgCard card)
        {
            if (structureIds[1] != "attention" || XgBoardData.Region(card.dataset) != "sequence") return;
            ClearAttention();
            var words = System.Globalization.StringInfo.GetTextElementEnumerator(XgSim.LessonText(card));
            while (words.MoveNext() && attentionWords.Count < attentionLabels.Length)
            {
                string word = words.GetTextElement();
                if (!string.IsNullOrWhiteSpace(word)) attentionWords.Add(word);
            }
            attentionStarted = Time.unscaledTime;
        }

        public Vector2 AttentionTarget(int index)
        {
            if (index < 0 || index >= attentionWords.Count || AttentionIndex < 0) return default;
            return RegionCenter(1) + new Vector2(-.033f, Mathf.Lerp(.038f, -.038f, (index + .5f) / attentionWords.Count));
        }

        void UpdateAttentionLabels()
        {
            int selected = AttentionIndex;
            for (int i = 0; i < attentionLabels.Length; i++)
            {
                bool visible = selected >= 0 && i < attentionWords.Count && wallpaper.Reveal.Visible;
                if (visible && attentionLabels[i] == null)
                {
                    var rect = LingGuangV05.Desktop.Story.PrologueDesk.Rect("Source character " + i, transform,
                        Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                    var label = rect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
                    label.font = LingGuangV05.Desktop.Story.PrologueDesk.CjkFont(); label.fontSize = 11;
                    label.alignment = TMPro.TextAlignmentOptions.Center; label.richText = false; label.raycastTarget = false;
                    attentionLabels[i] = label;
                }
                var current = attentionLabels[i]; if (current == null) continue;
                current.gameObject.SetActive(visible); if (!visible) continue;
                current.rectTransform.anchorMin = current.rectTransform.anchorMax = AttentionTarget(i);
                current.rectTransform.sizeDelta = new Vector2(20, 15);
                current.text = attentionWords[i]; current.color = i == selected ? XgCortexGraphic.N.Gold : XgCortexGraphic.N.Wire;
                current.canvasRenderer.SetAlpha(canvasRenderer.GetAlpha());
            }
        }

        void DrawAttentionBeam(VertexHelper vh, Rect area, float scale)
        {
            int selected = AttentionIndex; if (selected < 0) return;
            var from = area.min + Vector2.Scale(area.size, RegionCenter(1));
            var to = area.min + Vector2.Scale(area.size, AttentionTarget(selected) + new Vector2(.008f, 0));
            var ink = XgCortexGraphic.N.Gold; ink.a = .65f;
            XgSoftDraw.Seg(vh, from, to, 1.4f * scale, 4 * scale, ink);
            XgDraw.Ring(vh, from, 3 * scale, scale, ink, 12);
        }
        bool StructureReduced => controller?.View?.Juice != null && controller.View.Juice.Reduced;
        bool StructureMoving => System.Array.IndexOf(movingRegions, true) >= 0 && Time.unscaledTime - structureStarted <= StructureSeconds;

        void ClearStructure()
        {
            structureBoard = null; structureOrigins.Clear();
            ClearAttention();
            ClearDust();
            System.Array.Clear(structureIds, 0, structureIds.Length);
            System.Array.Clear(movingRegions, 0, movingRegions.Length);
            structureStarted = -100; next = 0;
        }

        void ObserveStructure(XgBoardState board)
        {
            bool sameBoard = ReferenceEquals(board, structureBoard), changed = false;
            if (!sameBoard) ClearStructure();
            structureBoard = board;
            for (int region = 0; region < 4; region++)
            {
                string id = region == 2 ? "tone" : controller.Sim?.RegionWiringOrLast(RegionIds[region])?.id ?? "";
                if (id == structureIds[region]) continue;
                if (region == 1) ClearAttention();
                movingRegions[region] = sameBoard && (id == "mlp" || id == "seq2seq" || id == "transformer" || IsRecurrent(id) || IsConvolution(id)) && !StructureReduced;
                structureClocks[region] = Time.unscaledTime;
                structureIds[region] = id; changed = true;
            }
            if (changed)
            {
                structureOrigins.Clear();
                if (sameBoard && !StructureReduced)
                    foreach (var cell in cells)
                        if (movingRegions[cell.region]) structureOrigins[cell.id] = cell.uv;
                structureStarted = Time.unscaledTime; next = 0;
            }
            if (StructureReduced || Time.unscaledTime - structureStarted > StructureSeconds)
            {
                if (System.Array.IndexOf(movingRegions, true) >= 0) next = 0; // Render the exact final layout once.
                structureOrigins.Clear();
                System.Array.Clear(movingRegions, 0, movingRegions.Length);
            }
        }

        static bool IsConvolution(string id) => id == "lenet" || id == "alexnet" || id == "vgg" || id == "googlenet" || id == "resnet";
        bool RotatingNebula => !StructureReduced && System.Array.IndexOf(structureIds, "transformer") >= 0;

        Vector2 RotateNebula(int region, Vector2 offset)
        {
            float angle = StructureReduced ? 0 : (Time.unscaledTime - structureClocks[region]) * .075f;
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return new Vector2(offset.x * c - offset.y * s, offset.x * s + offset.y * c);
        }

        public Vector2 NebulaSocket(int region, int index)
        {
            if (region < 0 || region >= 4 || structureIds[region] != "transformer" || index < 0 || index >= 10 || !isActiveAndEnabled) return default;
            return RegionCenter(region) + RotateNebula(region, LayoutPosition("transformer", index, 10) * .06f);
        }

        void DrawNebula(VertexHelper vh, Rect area, float scale, int region)
        {
            float arrival = movingRegions[region] ? Mathf.SmoothStep(0, 1, (Time.unscaledTime - structureStarted) / StructureSeconds) : 1;
            var haze = XgCortexGraphic.N.Teal; haze.a = .035f * arrival;
            XgSoftDraw.Halo(vh, area.min + Vector2.Scale(area.size, RegionCenter(region)), 62 * scale, haze, 32);
            var wire = XgCortexGraphic.N.Wire; wire.a = .11f * arrival;
            var socket = XgCortexGraphic.N.Teal; socket.a = .4f * arrival;
            // Ten hollow structure sockets, every pair connected. Real learned cells stay distinct.
            for (int i = 0; i < 10; i++)
            {
                var from = area.min + Vector2.Scale(area.size, NebulaSocket(region, i));
                for (int j = i + 1; j < 10; j++)
                    XgDraw.Seg(vh, from, area.min + Vector2.Scale(area.size, NebulaSocket(region, j)), .65f * scale, wire);
                XgDraw.Ring(vh, from, 3 * scale, .7f * scale, socket, 12);
            }
        }
        static bool IsRecurrent(string id) => id == "rnn" || id == "lstm" || id == "gru";
        static int MemoryGates(string id) => id == "lstm" ? 3 : id == "gru" ? 2 : 0;
        bool ScanningStructure => !StructureReduced && (IsConvolution(structureIds[0]) || IsRecurrent(structureIds[1]) || AttentionIndex >= 0);

        public Vector3 RecurrentSignal(int region)
        {
            if (region != 1 || !IsRecurrent(structureIds[region]) || !isActiveAndEnabled) return default;
            float phase = StructureReduced ? .2f : Mathf.Repeat((Time.unscaledTime - structureClocks[region]) / 4, 1);
            float angle = Mathf.PI * .5f - phase * Mathf.PI * 2;
            Vector2 at = RegionCenter(region) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .06f;
            return new Vector3(at.x, at.y, Mathf.Exp(-(MemoryGates(structureIds[region]) > 0 ? .15f : 3) * phase));
        }

        void DrawRecurrentSignal(VertexHelper vh, Rect area, float scale, int region)
        {
            if (region != 1 || !IsRecurrent(structureIds[region])) return;
            var signal = RecurrentSignal(region);
            Vector2 at = area.min + Vector2.Scale(area.size, new Vector2(signal.x, signal.y));
            var ink = XgCortexGraphic.N.Teal; ink.a = signal.z * .9f;
            var glow = ink; glow.a *= .3f;
            XgSoftDraw.Halo(vh, at, 16 * scale, glow, 16);
            XgSoftDraw.Halo(vh, at, 5 * scale, ink, 12);
            // The dimming signal illustrates short memory, not an extra learned concept.
            if (StructureReduced) return;
            float phase = Mathf.Repeat((Time.unscaledTime - structureClocks[region]) / 4, 1);
            for (int i = 1; i <= 6; i++)
            {
                float behind = phase - i * .014f; if (behind < 0) break;
                float angle = Mathf.PI * .5f - behind * Mathf.PI * 2;
                var uv = RegionCenter(region) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .06f;
                ink.a = signal.z * .4f * (1 - i / 7f);
                XgSoftDraw.Halo(vh, area.min + Vector2.Scale(area.size, uv), 3 * scale, ink, 8);
            }
        }

        public float GateRetention(int region, int gate)
        {
            if (region != 1 || !isActiveAndEnabled) return 0;
            int gates = MemoryGates(structureIds[region]);
            if (gate < 0 || gate >= gates || gate % 2 != 0) return 0;
            if (StructureReduced) return .4f;
            float cycles = (Time.unscaledTime - structureClocks[region]) / 4;
            float pass = (gate + 1f) / (gates + 1);
            if (cycles < pass) return 0;
            // A bounded retained trace stays at selected gates; no new concept is created.
            float age = Mathf.Repeat(cycles - pass, 1) * 4;
            return .65f * Mathf.Exp(-.18f * age);
        }

        void DrawMemoryGates(VertexHelper vh, Rect area, float scale, int region, string arch)
        {
            int gates = MemoryGates(arch);
            for (int gate = 0; gate < gates; gate++)
            {
                float phase = (gate + 1f) / (gates + 1), angle = Mathf.PI * .5f - phase * Mathf.PI * 2;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 uv = RegionCenter(region) + direction * .06f;
                Vector2 point = area.min + Vector2.Scale(area.size, uv);
                var gold = XgCortexGraphic.N.Gold; gold.a = .6f;
                XgSoftDraw.Seg(vh, point + new Vector2(-4, -7) * scale, point + new Vector2(-4, 7) * scale, 1.4f * scale, scale, gold);
                XgSoftDraw.Seg(vh, point + new Vector2(4, -7) * scale, point + new Vector2(4, 7) * scale, 1.4f * scale, scale, gold);
                float retained = GateRetention(region, gate); if (retained <= 0) continue;
                Vector2 inside = area.min + Vector2.Scale(area.size, RegionCenter(region) + direction * .045f);
                gold.a = retained;
                XgSoftDraw.Halo(vh, inside, 7 * scale, gold, 12);
                var line = gold; line.a *= .3f;
                XgDraw.Seg(vh, point, inside, .8f * scale, line);
            }
        }
        int GridDivisions(int region) => Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(9, regionTotals[region]))) - 1, 2, 8);

        public Rect ScanWindow(int region)
        {
            if (region != 0 || !IsConvolution(structureIds[region]) || !isActiveAndEnabled) return default;
            int divisions = GridDivisions(region), span = divisions >= 4 ? 2 : 1, last = divisions - span;
            int points = (last + 1) * (last + 1);
            float step = StructureReduced ? 0 : Mathf.PingPong((Time.unscaledTime - structureClocks[region]) / .35f, points - 1);
            int from = Mathf.FloorToInt(step), to = Mathf.Min(from + 1, points - 1);
            Vector2 a = ScanGridPoint(from, last), b = ScanGridPoint(to, last);
            Vector2 cell = Vector2.Lerp(a, b, Mathf.SmoothStep(0, 1, step - from));
            Vector2 min = RegionCenter(region) - Vector2.one * .06f + cell * (.12f / divisions);
            return new Rect(min, Vector2.one * (.12f * span / divisions));
        }

        static Vector2 ScanGridPoint(int step, int last)
        {
            int row = step / (last + 1), column = step % (last + 1);
            return new Vector2(row % 2 == 0 ? column : last - column, row);
        }

        void DrawConvolution(VertexHelper vh, Rect area, float scale, int region)
        {
            var center = area.min + Vector2.Scale(area.size, RegionCenter(region));
            var radius = Vector2.Scale(area.size, Vector2.one * .06f);
            var ink = XgCortexGraphic.N.Wire; ink.a = .17f;
            int divisions = GridDivisions(region);
            for (int i = 0; i <= divisions; i++)
            {
                float t = -1 + 2f * i / divisions;
                XgDraw.Seg(vh, center + new Vector2(t * radius.x, -radius.y), center + new Vector2(t * radius.x, radius.y), .65f * scale, ink);
                XgDraw.Seg(vh, center + new Vector2(-radius.x, t * radius.y), center + new Vector2(radius.x, t * radius.y), .65f * scale, ink);
            }
            var window = ScanWindow(region); if (window.width <= 0) return;
            Vector2 min = area.min + Vector2.Scale(area.size, window.min), max = area.min + Vector2.Scale(area.size, window.max);
            var fill = XgCortexGraphic.N.Teal; fill.a = .045f;
            XgDraw.Box(vh, min, max, fill);
            var line = XgCortexGraphic.N.Teal; line.a = .55f;
            var lowerRight = new Vector2(max.x, min.y); var upperLeft = new Vector2(min.x, max.y);
            XgSoftDraw.Seg(vh, min, lowerRight, scale, 2 * scale, line);
            XgSoftDraw.Seg(vh, lowerRight, max, scale, 2 * scale, line);
            XgSoftDraw.Seg(vh, max, upperLeft, scale, 2 * scale, line);
            XgSoftDraw.Seg(vh, upperLeft, min, scale, 2 * scale, line);
            if (structureIds[region] == "resnet") DrawResidualPaths(vh, area, scale, region);
        }

        public Vector2 ResidualPoint(int region, int route, float progress)
        {
            if (region != 0 || structureIds[region] != "resnet" || !isActiveAndEnabled || route < 0 || route > 1) return default;
            float t = Mathf.Clamp01(progress);
            return RegionCenter(region) + new Vector2(Mathf.Lerp(-.06f, .06f, t),
                (route == 0 ? 1 : -1) * .052f * Mathf.Sin(t * Mathf.PI));
        }

        void DrawResidualPaths(VertexHelper vh, Rect area, float scale, int region)
        {
            var ink = XgCortexGraphic.N.Gold;
            ink.a = .4f * (movingRegions[region] ? Mathf.SmoothStep(0, 1, (Time.unscaledTime - structureStarted) / StructureSeconds) : 1);
            for (int route = 0; route < 2; route++)
            {
                Vector2 previous = area.min + Vector2.Scale(area.size, ResidualPoint(region, route, 0));
                for (int i = 1; i <= 24; i++)
                {
                    Vector2 nextPoint = area.min + Vector2.Scale(area.size, ResidualPoint(region, route, i / 24f));
                    XgSoftDraw.Seg(vh, previous, nextPoint, 1.1f * scale, 1.8f * scale, ink);
                    previous = nextPoint;
                }
            }
            // Hollow endpoints are structure sockets, never synthetic learned concepts or links.
            for (int edge = 0; edge <= 1; edge++)
                XgDraw.Ring(vh, area.min + Vector2.Scale(area.size, ResidualPoint(region, 0, edge)), 4 * scale, scale, ink, 16);
        }

        public float SequenceNeck(int region)
        {
            if (region != 1 || structureIds[region] != "seq2seq" || !isActiveAndEnabled) return 0;
            float t = movingRegions[region] ? Mathf.SmoothStep(0, 1, (Time.unscaledTime - structureStarted) / StructureSeconds) : 1;
            return Mathf.Lerp(.03f, .006f, t);
        }

        void DrawSequenceBottleneck(VertexHelper vh, Rect area, float scale, int region)
        {
            float neck = SequenceNeck(region); if (neck <= 0) return;
            float split = Mathf.InverseLerp(.03f, .006f, neck);
            Vector2 origin = RegionCenter(region);
            var ink = XgCortexGraphic.N.Teal; ink.a = .32f;
            for (int side = -1; side <= 1; side += 2)
            {
                var uv = origin + new Vector2(side * Mathf.Lerp(.012f, .033f, split), 0);
                EllipseLine(vh, area.min + Vector2.Scale(area.size, uv),
                    Vector2.Scale(area.size, new Vector2(.0252f, .048f)), scale, ink);
            }
            // A narrow structural channel joins encoder and decoder; it is not a learned link.
            var gold = XgCortexGraphic.N.Gold; gold.a = .5f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 previous = default;
                for (int i = 0; i <= 24; i++)
                {
                    float x = -1 + i / 12f;
                    var uv = origin + new Vector2(x * .033f, side * Mathf.Lerp(neck, .032f, x * x));
                    var point = area.min + Vector2.Scale(area.size, uv);
                    if (i > 0) XgSoftDraw.Seg(vh, previous, point, scale, 1.4f * scale, gold);
                    previous = point;
                }
            }
        }

        Vector2 StructurePosition(int id, int region, Vector2 target)
        {
            if (structureIds[region] == "transformer") target = RegionCenter(region) + RotateNebula(region, target - RegionCenter(region));
            if (!movingRegions[region] || !structureOrigins.TryGetValue(id, out var from)) return target;
            return Vector2.Lerp(from, target, Mathf.SmoothStep(0, 1, (Time.unscaledTime - structureStarted) / StructureSeconds));
        }

        void DrawMlpGuides(VertexHelper vh, Rect area, float scale, int region)
        {
            foreach (var column in structureColumns) column.Clear();
            foreach (var cell in cells) if (cell.region == region) structureColumns[cell.layer].Add(cell);
            // Architecture guides join actual displayed concepts in adjacent layers. They are
            // deliberately dimmer than learned links and never enter the Core link collection.
            var ink = XgCortexGraphic.N.Wire;
            ink.a = .18f * (StructureMoving ? Mathf.SmoothStep(0, 1, (Time.unscaledTime - structureStarted) / StructureSeconds) : 1);
            var center = area.min + Vector2.Scale(area.size, RegionCenter(region));
            var radius = Vector2.Scale(area.size, new Vector2(.06f, .06f));
            var guide = ink; guide.a *= .35f;
            for (int layer = 0; layer < 3; layer++)
                XgDraw.Seg(vh, center + new Vector2((layer - 1) * radius.x, -radius.y),
                    center + new Vector2((layer - 1) * radius.x, radius.y), scale, guide);
            for (int layer = 0; layer < 2; layer++)
            {
                int drawn = 0;
                foreach (var from in structureColumns[layer])
                    foreach (var to in structureColumns[layer + 1])
                    {
                        if (drawn++ >= 32) break;
                        XgSoftDraw.Seg(vh, area.min + Vector2.Scale(area.size, from.uv),
                            area.min + Vector2.Scale(area.size, to.uv), .65f * scale, 1.4f * scale, ink);
                    }
            }
            DrawStructureSockets(vh, area, scale, region, true);
        }

        void DrawStructureSockets(VertexHelper vh, Rect area, float scale, int region, bool mlp)
        {
            float phase = mlp && movingRegions[region] ? Mathf.SmoothStep(0, 1, (Time.unscaledTime - structureStarted) / StructureSeconds) : 1;
            var center = area.min + Vector2.Scale(area.size, RegionCenter(region));
            var radius = Vector2.Scale(area.size, new Vector2(.06f, .06f));
            var ink = XgCortexGraphic.N.Wire; ink.a = .2f;
            // Hollow sockets describe available structure, not learned concepts. Filled learned
            // points and the Core link index remain exclusively backed by the actual board.
            for (int i = 0; i < 9; i++)
            {
                var initial = LayoutPosition("perceptron", i, 9);
                var target = LayoutPosition(mlp ? "mlp" : "perceptron", i, 9);
                var point = center + Vector2.Scale(radius, Vector2.Lerp(initial, target, phase));
                XgDraw.Ring(vh, point, 3.6f * scale, .6f * scale, ink, 12);
                if (!mlp || i % 3 == 2) continue;
                var line = ink; line.a = .1f * phase;
                for (int j = i % 3 + 1; j < 9; j += 3)
                {
                    var end = center + Vector2.Scale(radius, Vector2.Lerp(LayoutPosition("perceptron", j, 9), LayoutPosition("mlp", j, 9), phase));
                    XgDraw.Seg(vh, point, end, .6f * scale, line);
                }
            }
        }
    }
}
