using System.Collections.Generic;
using System.Linq;
using LingGuang.Core;
using TMPro;
using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Static board (cells, gullies, bridges, lighthouse) plus live spark/edge views. No rule logic.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        public const float HexSize = 0.62f;
        public static readonly float Sqrt3 = Mathf.Sqrt(3f);

        public RunState state;
        public bool showInternal;
        Vector2 origin;
        readonly Dictionary<int, SpriteRenderer> cellFill = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<int, SpriteRenderer> cellOutline = new Dictionary<int, SpriteRenderer>();
        readonly List<LineRenderer> gullyLines = new List<LineRenderer>();
        readonly Dictionary<int, SparkView> sparkViews = new Dictionary<int, SparkView>();
        readonly Dictionary<long, EdgeView> edgeViews = new Dictionary<long, EdgeView>();
        readonly Dictionary<int, SpriteRenderer> pickupViews = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<int, LineRenderer> memoryHulls = new Dictionary<int, LineRenderer>();
        readonly Dictionary<long, LineRenderer> weakViews = new Dictionary<long, LineRenderer>();
        public readonly HashSet<int> collapsedMemories = new HashSet<int>();
        Transform cellsRoot, gullyRoot, edgeRoot, sparkRoot, miscRoot;
        SpriteRenderer hoverSr, selectSr, lighthouseSr;
        readonly List<SpriteRenderer> lighthouseCover = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> previewMarks = new List<SpriteRenderer>();
        SparkView ghost;
        TMP_FontAsset font;

        // display values during playback (node id -> charge / threshold)
        public readonly Dictionary<int, int> dispCharge = new Dictionary<int, int>();
        public readonly Dictionary<int, int> dispThreshold = new Dictionary<int, int>();
        public readonly Dictionary<long, int> dispEdgeCount = new Dictionary<long, int>();
        public bool usingDisplay;

        public static readonly Color[] RegionColors =
        {
            Color.clear,
            new Color(1f, 0.72f, 0.42f),   // I
            new Color(0.5f, 1f, 0.68f),    // K
            new Color(0.78f, 0.55f, 1f),   // C
            new Color(0.42f, 0.85f, 1f),   // F
            new Color(1f, 0.52f, 0.68f),   // T
        };

        public static readonly string[] RegionNames = { "", "后部感觉区", "感觉运动带", "中央奖赏区", "前额区", "左右颞区" };

        public void Init(RunState s, TMP_FontAsset fontAsset)
        {
            state = s;
            font = fontAsset;
            foreach (Transform c in transform) Destroy(c.gameObject);
            cellFill.Clear(); cellOutline.Clear(); gullyLines.Clear(); sparkViews.Clear(); edgeViews.Clear(); pickupViews.Clear(); memoryHulls.Clear(); weakViews.Clear();
            gullyStage = -1;
            lighthouseCover.Clear(); previewMarks.Clear(); collapsedMemories.Clear();
            cellsRoot = NewRoot("Cells"); gullyRoot = NewRoot("Gullies"); edgeRoot = NewRoot("Edges"); sparkRoot = NewRoot("Sparks"); miscRoot = NewRoot("Misc");

            var b = s.board;
            float w = HexSize * Sqrt3 * (b.Cols + 0.5f);
            float h = HexSize * 1.5f * (b.Rows - 1);
            origin = new Vector2(-w / 2f + HexSize * Sqrt3 / 2f, h / 2f);

            foreach (int c in b.AllCells())
            {
                var p = CellPos(c);
                var fill = Gfx.MakeSprite("cell" + c, cellsRoot, Gfx.Hex, Gfx.Alpha, 0, HexSize * 2f);
                fill.transform.position = p;
                cellFill[c] = fill;
                var ol = Gfx.MakeSprite("outline" + c, cellsRoot, Gfx.HexOutline, Gfx.Additive, 1, HexSize * 2f);
                ol.transform.position = p;
                cellOutline[c] = ol;
            }
            hoverSr = Gfx.MakeSprite("hover", miscRoot, Gfx.HexOutline, Gfx.Additive, 5, HexSize * 2f);
            selectSr = Gfx.MakeSprite("select", miscRoot, Gfx.HexOutline, Gfx.Additive, 6, HexSize * 2.08f);
            Gfx.SetColor(selectSr, Gfx.HDR(new Color(1f, 0.95f, 0.6f), 2.2f));
            hoverSr.enabled = false; selectSr.enabled = false;

            // lighthouse glyph below the anchors
            var anchors = b.AllCells().Where(c => b.Row(c) == s.cfg.lighthouseAnchorRow).ToList();
            Vector3 lp = anchors.Count > 0 ? anchors.Aggregate(Vector3.zero, (acc, c) => acc + CellPos(c)) / anchors.Count : Vector3.zero;
            lighthouseSr = Gfx.MakeSprite("lighthouse", miscRoot, Gfx.SoftDot, Gfx.Additive, 3, 1.4f);
            lighthouseSr.transform.position = lp + new Vector3(0, -HexSize * 1.6f, 0);
            Gfx.SetColor(lighthouseSr, Gfx.HDR(new Color(1f, 0.9f, 0.65f), 1.6f));
            var core = Gfx.MakeSprite("lighthouseCore", lighthouseSr.transform, Gfx.Disc, Gfx.Additive, 4, 0.18f);
            Gfx.SetColor(core, Gfx.HDR(new Color(1f, 0.95f, 0.8f), 3f));

            ghost = SparkView.Create(miscRoot, font);
            ghost.gameObject.SetActive(false);
            RebuildGullies();
            Sync();
        }

        Transform NewRoot(string n)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        public Vector3 CellPos(int cell)
        {
            var b = state.board;
            int c = b.Col(cell), r = b.Row(cell);
            return new Vector3(origin.x + HexSize * Sqrt3 * (c + 0.5f * (r & 1)), origin.y - HexSize * 1.5f * r, 0f);
        }

        public Vector3 LighthousePos => lighthouseSr != null ? lighthouseSr.transform.position : Vector3.zero;

        public int CellAt(Vector3 world)
        {
            int best = -1; float bd = HexSize * 0.95f;
            foreach (int c in state.board.AllCells())
            {
                float d = Vector2.Distance(world, CellPos(c));
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        public static float DirAngle(int d) => (60f - 60f * d) * Mathf.Deg2Rad;
        public static Vector3 DirVec(int d) => new Vector3(Mathf.Cos(DirAngle(d)), Mathf.Sin(DirAngle(d)), 0);

        Vector3 Corner(int cell, float angleRad) => CellPos(cell) + new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * HexSize;

        public Bounds UnlockedBounds()
        {
            var cells = state.board.AllCells().Where(c => state.IsUnlocked(c)).ToList();
            if (cells.Count == 0) cells = state.board.AllCells().ToList();
            var bb = new Bounds(CellPos(cells[0]), Vector3.zero);
            foreach (int c in cells) bb.Encapsulate(CellPos(c));
            bb.Encapsulate(LighthousePos);
            bb.Expand(new Vector3(HexSize * 2.2f, HexSize * 2.2f, 0));
            return bb;
        }

        public Bounds FullBounds()
        {
            var bb = new Bounds(Vector3.zero, Vector3.zero);
            foreach (int c in state.board.AllCells()) bb.Encapsulate(CellPos(c));
            bb.Encapsulate(LighthousePos);
            bb.Expand(new Vector3(HexSize * 2.2f, HexSize * 2.2f, 0));
            return bb;
        }

        // ------------------------------------------------------------------ gullies

        int gullyStage = -1;

        public void RebuildGullies()
        {
            gullyStage = state.stage;
            foreach (var l in gullyLines) Destroy(l.gameObject);
            gullyLines.Clear();
            var b = state.board;
            foreach (int a in b.AllCells())
                for (int d = 0; d < 3; d++) // each edge once (directions 0..2 + their opposite cover all)
                {
                    int n = b.Neighbor(a, d);
                    if (n < 0) continue;
                    bool bridge = b.IsBridge(a, n);
                    bool gully = b.IsGully(a, n, state.stage);
                    if (!gully && !bridge) continue;
                    float ang = DirAngle(d);
                    var p1 = Corner(a, ang + 30f * Mathf.Deg2Rad);
                    var p2 = Corner(a, ang - 30f * Mathf.Deg2Rad);
                    var lr = Gfx.MakeLine("gully", gullyRoot, gully ? Gfx.Alpha : Gfx.Additive, 2, gully ? 0.12f : 0.05f);
                    lr.positionCount = 2;
                    lr.SetPosition(0, p1); lr.SetPosition(1, p2);
                    if (gully)
                    {
                        bool secondary = b.secondaryGullies.Contains(HexBoard.Key(a, n));
                        Gfx.SetLineColor(lr, secondary ? new Color(0.0f, 0.0f, 0.02f, 0.95f) : new Color(0.0f, 0.0f, 0.0f, 1f));
                        if (!secondary) lr.widthMultiplier = 0.16f;
                    }
                    else Gfx.SetLineColor(lr, Gfx.HDR(new Color(1f, 0.85f, 0.55f), 1.2f)); // open bridge
                    gullyLines.Add(lr);
                }
        }

        // ------------------------------------------------------------------ sync

        public void Sync()
        {
            var s = state;
            var mods = new Mods(s);
            float t = Time.time;
            foreach (var kv in cellFill)
            {
                int c = kv.Key;
                var reg = s.board.regionOf[c];
                var col = RegionColors[(int)reg];
                bool unlocked = s.IsUnlocked(c);
                bool disabled = s.disabledCells.Contains(c);
                var rs = s.RegionStateOf(reg);
                Color fillC;
                if (!unlocked) fillC = new Color(0.12f, 0.12f, 0.16f, 0.12f);
                else if (disabled) fillC = new Color(0.05f, 0.05f, 0.05f, 0.85f);
                else fillC = new Color(col.r * 0.22f, col.g * 0.22f, col.b * 0.26f, rs != null && rs.sedimented ? 0.5f : 0.38f);
                Gfx.SetColor(kv.Value, fillC);
                Color olC = !unlocked ? new Color(0.25f, 0.25f, 0.3f, 0.08f) : disabled ? new Color(0.3f, 0.3f, 0.3f, 0.1f) : Gfx.HDR(col, 0.32f);
                Gfx.SetColor(cellOutline[c], olC);
            }

            // sparks
            var alive = new HashSet<int>(s.sparks.Keys);
            foreach (var id in sparkViews.Keys.ToList())
                if (!alive.Contains(id)) { sparkViews[id].Despawn(); sparkViews.Remove(id); }
            foreach (var sp in s.sparks.Values)
            {
                if (!sparkViews.TryGetValue(sp.id, out var v))
                {
                    v = SparkView.Create(sparkRoot, font);
                    sparkViews[sp.id] = v;
                    v.transform.position = CellPos(sp.cell);
                }
                var node = s.nodes[sp.nodeId];
                int charge = usingDisplay && dispCharge.TryGetValue(node.id, out int dc) ? dc : node.charge;
                int thr = usingDisplay && dispThreshold.TryGetValue(node.id, out int dt) ? dt : mods.Threshold(node);
                v.target = CellPos(sp.cell);
                v.Apply(s, sp, node, charge, thr, s.disabledCells.Contains(sp.cell), showInternal, sp.id == s.conchSpark && s.items.Contains("conch"));
            }

            // edges
            foreach (var k in edgeViews.Keys.ToList())
                if (!s.edges.ContainsKey(k)) { Destroy(edgeViews[k].gameObject); edgeViews.Remove(k); }
            foreach (var kv in s.edges)
            {
                var e = kv.Value;
                if (!edgeViews.TryGetValue(kv.Key, out var ev))
                {
                    ev = EdgeView.Create(edgeRoot);
                    edgeViews[kv.Key] = ev;
                }
                var a = s.sparks[e.from]; var bsp = s.sparks[e.to];
                bool internalMem = a.nodeId == bsp.nodeId;
                int count = usingDisplay && dispEdgeCount.TryGetValue(kv.Key, out int ec) ? ec : e.count;
                int lvl = count >= s.cfg.myelinAt ? 2 : count >= s.cfg.thickAt ? 1 : 0;
                int N = s.cfg.decayRoundsByStage[Mathf.Min(s.stage, 5)];
                bool fading = !e.conductedThisRound && e.idleRounds >= N - 1;
                ev.Set(CellPos(a.cell), CellPos(bsp.cell), lvl, e.pruned, internalMem, fading, mods.BridgePenalty(e), s.disabledCells.Contains(a.cell) || s.disabledCells.Contains(bsp.cell));
            }

            // pickups
            foreach (var c in pickupViews.Keys.ToList())
                if (!s.pickups.Any(p => p.cell == c)) { Destroy(pickupViews[c].gameObject); pickupViews.Remove(c); }
            foreach (var p in s.pickups)
            {
                if (pickupViews.ContainsKey(p.cell)) continue;
                var sr = Gfx.MakeSprite("pickup", miscRoot, Gfx.Disc, Gfx.Additive, 8, 0.2f);
                sr.transform.position = CellPos(p.cell);
                sr.transform.rotation = Quaternion.Euler(0, 0, 45);
                Gfx.SetColor(sr, Gfx.HDR(new Color(1f, 0.85f, 0.3f), 2f));
                pickupViews[p.cell] = sr;
            }

            // memory hulls
            foreach (var id in memoryHulls.Keys.ToList())
                if (!s.nodes.ContainsKey(id)) { Destroy(memoryHulls[id].gameObject); memoryHulls.Remove(id); }
            foreach (var n in s.nodes.Values.Where(x => x.isMemory))
            {
                if (!memoryHulls.TryGetValue(n.id, out var lr))
                {
                    lr = Gfx.MakeLine("memory" + n.id, sparkRoot, Gfx.Additive, 9, 0.5f);
                    memoryHulls[n.id] = lr;
                }
                var pts = OrderChain(n.sparks.Select(id => CellPos(s.sparks[id].cell)).ToList());
                lr.positionCount = pts.Count;
                lr.SetPositions(pts.ToArray());
                bool collapsed = collapsedMemories.Contains(n.id);
                lr.widthMultiplier = collapsed ? 0.9f : 0.42f;
                float k = Mathf.Clamp01(n.plasticity / (float)s.cfg.plasticityInit);
                Gfx.SetLineColor(lr, Gfx.HDR(Color.Lerp(new Color(1f, 0.8f, 0.5f), new Color(0.6f, 0.85f, 1f), k), collapsed ? 0.5f : 0.22f));
            }
            // weak links (ripple co-coverage), faint until active
            foreach (var k in weakViews.Keys.ToList())
                if (!s.weakLinks.ContainsKey(k)) { Destroy(weakViews[k].gameObject); weakViews.Remove(k); }
            foreach (var kv in s.weakLinks)
            {
                RunState.SplitPair(kv.Key, out int na, out int nb);
                if (!s.nodes.TryGetValue(na, out var A) || !s.nodes.TryGetValue(nb, out var B)) continue;
                if (!weakViews.TryGetValue(kv.Key, out var wl))
                {
                    wl = Gfx.MakeLine("weak", edgeRoot, Gfx.Additive, 4, 0.02f);
                    wl.positionCount = 2;
                    weakViews[kv.Key] = wl;
                }
                wl.SetPosition(0, CellPos(s.sparks[A.sparks[0]].cell));
                wl.SetPosition(1, CellPos(s.sparks[B.sparks[0]].cell));
                bool active = kv.Value >= s.cfg.weakLinkActiveAt;
                wl.widthMultiplier = active ? 0.035f : 0.015f;
                Gfx.SetLineColor(wl, Gfx.HDR(new Color(0.75f, 0.7f, 1f), active ? 0.5f : 0.12f + 0.05f * kv.Value));
            }
            if (gullyStage != state.stage) RebuildGullies();
        }

        static List<Vector3> OrderChain(List<Vector3> pts)
        {
            // nearest-neighbour chain for a simple hull-ish stroke
            var res = new List<Vector3>();
            if (pts.Count == 0) return res;
            var left = new List<Vector3>(pts);
            var cur = left[0]; left.RemoveAt(0); res.Add(cur);
            while (left.Count > 0)
            {
                int bi = 0; float bd = float.MaxValue;
                for (int i = 0; i < left.Count; i++) { float d = (left[i] - cur).sqrMagnitude; if (d < bd) { bd = d; bi = i; } }
                cur = left[bi]; left.RemoveAt(bi); res.Add(cur);
            }
            return res;
        }

        public void SnapshotDisplay()
        {
            var mods = new Mods(state);
            dispCharge.Clear(); dispThreshold.Clear(); dispEdgeCount.Clear();
            foreach (var n in state.nodes.Values) { dispCharge[n.id] = n.charge; dispThreshold[n.id] = mods.Threshold(n); }
            foreach (var kv in state.edges) dispEdgeCount[kv.Key] = kv.Value.count;
            usingDisplay = true;
        }

        public void EndDisplay() { usingDisplay = false; Sync(); }

        public SparkView ViewAt(int cell)
        {
            var sp = state.SparkAt(cell);
            return sp != null && sparkViews.TryGetValue(sp.id, out var v) ? v : null;
        }

        public SparkView ViewOf(int sparkId) => sparkViews.TryGetValue(sparkId, out var v) ? v : null;

        public EdgeView EdgeAt(int cellA, int cellB)
        {
            var a = state.SparkAt(cellA); var b = state.SparkAt(cellB);
            if (a == null || b == null) return null;
            edgeViews.TryGetValue(Edge.Key(a.id, b.id), out var ev);
            return ev;
        }

        public void SetEdgeDisplayCount(int cellA, int cellB, int count)
        {
            var a = state.SparkAt(cellA); var b = state.SparkAt(cellB);
            if (a == null || b == null) return;
            dispEdgeCount[Edge.Key(a.id, b.id)] = count;
        }

        // ------------------------------------------------------------------ hover / selection / ghost / previews

        public void SetHover(int cell, bool valid)
        {
            hoverSr.enabled = cell >= 0;
            if (cell < 0) return;
            hoverSr.transform.position = CellPos(cell);
            Gfx.SetColor(hoverSr, valid ? Gfx.HDR(new Color(0.7f, 0.95f, 1f), 1.4f) : new Color(1f, 0.3f, 0.3f, 0.6f));
        }

        public void SetSelected(int cell)
        {
            selectSr.enabled = cell >= 0;
            if (cell >= 0) selectSr.transform.position = CellPos(cell);
        }

        public void SetGhost(bool on, int cell, Shape shape, int dir)
        {
            ghost.gameObject.SetActive(on && cell >= 0);
            if (!on || cell < 0) return;
            ghost.transform.position = CellPos(cell);
            ghost.target = CellPos(cell);
            ghost.ApplyGhost(state, shape, dir);
        }

        public void ShowLighthouseCover(bool on)
        {
            foreach (var sr in lighthouseCover) Destroy(sr.gameObject);
            lighthouseCover.Clear();
            if (!on) return;
            foreach (int c in state.LighthouseCells())
            {
                var sr = Gfx.MakeSprite("lh", miscRoot, Gfx.HexOutline, Gfx.Additive, 4, HexSize * 1.7f);
                sr.transform.position = CellPos(c);
                Gfx.SetColor(sr, Gfx.HDR(new Color(1f, 0.9f, 0.6f), 0.5f));
                lighthouseCover.Add(sr);
            }
        }

        public void ShowPreview(Dictionary<int, List<int>> firedByBeat)
        {
            foreach (var sr in previewMarks) Destroy(sr.gameObject);
            previewMarks.Clear();
            if (firedByBeat == null) return;
            foreach (var kv in firedByBeat)
                foreach (int c in kv.Value)
                {
                    var sr = Gfx.MakeSprite("preview", miscRoot, Gfx.Ring, Gfx.Additive, 12, 0.5f + 0.18f * kv.Key);
                    sr.transform.position = CellPos(c);
                    Gfx.SetColor(sr, Gfx.HDR(kv.Key == 0 ? new Color(1f, 1f, 0.7f) : kv.Key == 1 ? new Color(0.6f, 1f, 0.8f) : new Color(0.6f, 0.8f, 1f), 1.2f));
                    previewMarks.Add(sr);
                }
        }

        void Update()
        {
            if (state == null) return;
            foreach (var ev in edgeViews.Values) ev.Tick();
            foreach (var sv in sparkViews.Values) sv.Tick();
            if (ghost != null && ghost.gameObject.activeSelf) ghost.Tick();
            if (selectSr.enabled) selectSr.transform.localScale = Vector3.one * HexSize * 2.08f * (1f + 0.03f * Mathf.Sin(Time.time * 5f));
            foreach (var kv in pickupViews) kv.Value.transform.localScale = Vector3.one * (0.2f + 0.04f * Mathf.Sin(Time.time * 4f + kv.Key));
        }
    }

    // =====================================================================

    public sealed class SparkView : MonoBehaviour
    {
        SpriteRenderer glow, core, ringBg, outline;
        LineRenderer chargeArc;
        readonly List<LineRenderer> spikes = new List<LineRenderer>();
        TextMeshPro label;
        public Vector3 target;
        public float flash;         // 0..1 fire flash
        public float brightness;    // from charge
        Color baseColor = Color.white;
        float pulse;
        float targetRatio, shownRatio;
        bool arcDirty;

        void DrawArc()
        {
            float ratio = shownRatio;
            int segs = Mathf.Max(2, Mathf.CeilToInt(48 * ratio) + 1);
            chargeArc.positionCount = ratio > 0.005f ? segs : 0;
            for (int i = 0; i < segs && ratio > 0.005f; i++)
            {
                float a = Mathf.PI / 2f - (i / (float)(segs - 1)) * ratio * Mathf.PI * 2f;
                chargeArc.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 0.27f);
            }
            Gfx.SetLineColor(chargeArc, Gfx.HDR(Color.Lerp(baseColor, Color.white, 0.5f), disabled ? 0.3f : 1.6f + ratio * 1.5f));
        }
        bool disabled;

        public static SparkView Create(Transform parent, TMP_FontAsset font)
        {
            var go = new GameObject("Spark");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<SparkView>();
            v.glow = Gfx.MakeSprite("glow", go.transform, Gfx.SoftDot, Gfx.Additive, 10, 1.1f);
            v.ringBg = Gfx.MakeSprite("ringBg", go.transform, Gfx.Ring, Gfx.Alpha, 11, 0.62f);
            v.core = Gfx.MakeSprite("core", go.transform, Gfx.Disc, Gfx.Additive, 13, 0.2f);
            v.outline = Gfx.MakeSprite("outline", go.transform, Gfx.HexOutline, Gfx.Additive, 12, 0.5f);
            v.chargeArc = Gfx.MakeLine("charge", go.transform, Gfx.Additive, 14, 0.055f);
            v.chargeArc.useWorldSpace = false;
            var lgo = new GameObject("label");
            lgo.transform.SetParent(go.transform, false);
            lgo.transform.localPosition = new Vector3(0, -0.4f, 0);
            v.label = lgo.AddComponent<TextMeshPro>();
            if (font != null) v.label.font = font;
            v.label.fontSize = 1.8f;
            v.label.alignment = TextAlignmentOptions.Center;
            v.label.rectTransform.sizeDelta = new Vector2(1.6f, 0.4f);
            v.label.sortingOrder = 20;
            v.label.color = new Color(1, 1, 1, 0.7f);
            return v;
        }

        public static Color ShapeColor(Spark sp) => sp == null ? Color.white : ShapeColor(sp.shape, sp.isCharacter);

        public static Color ShapeColor(Shape shape, bool character)
        {
            if (character) return new Color(1f, 0.55f, 0.85f);
            return shape switch
            {
                Shape.Conduct => new Color(0.55f, 0.9f, 1f),
                Shape.Cone => new Color(0.65f, 1f, 0.6f),
                Shape.Converge => new Color(0.85f, 0.6f, 1f),
                Shape.Instinct => new Color(1f, 0.78f, 0.45f),
                _ => Color.white,
            };
        }

        void SetSpikes(int[] dirs, int orient, Color c, float len)
        {
            while (spikes.Count < dirs.Length)
            {
                var lr = Gfx.MakeLine("spike", transform, Gfx.Additive, 13, 0.06f);
                lr.useWorldSpace = false;
                lr.positionCount = 2;
                spikes.Add(lr);
            }
            for (int i = 0; i < spikes.Count; i++)
            {
                bool on = i < dirs.Length;
                spikes[i].gameObject.SetActive(on);
                if (!on) continue;
                var dv = BoardView.DirVec((dirs[i] + orient) % 6);
                spikes[i].SetPosition(0, dv * 0.12f);
                spikes[i].SetPosition(1, dv * len);
                Gfx.SetLineColor(spikes[i], c);
            }
        }

        public void Apply(RunState s, Spark sp, Node node, int charge, int threshold, bool isDisabled, bool showInternal, bool conch)
        {
            disabled = isDisabled;
            baseColor = ShapeColor(sp);
            var dirs = s.Dirs(sp);
            float ratio = threshold > 0 ? Mathf.Clamp01(charge / (float)threshold) : 0f;
            brightness = ratio;
            SetSpikes(dirs, sp.dir, Gfx.HDR(baseColor, disabled ? 0.2f : 1.3f), sp.shape == Shape.Instinct ? 0.24f : 0.34f);
            targetRatio = ratio;
            arcDirty = true;
            Gfx.SetColor(ringBg, disabled ? new Color(0.2f, 0.2f, 0.2f, 0.3f) : new Color(baseColor.r * 0.3f, baseColor.g * 0.3f, baseColor.b * 0.35f, 0.75f));
            outline.gameObject.SetActive(sp.shape == Shape.Instinct || sp.shape == Shape.Converge || node.isMemory || conch);
            outline.transform.localRotation = Quaternion.Euler(0, 0, sp.shape == Shape.Converge ? 30 : 0);
            outline.transform.localScale = Vector3.one * (node.isMemory ? 0.62f : 0.46f);
            Gfx.SetColor(outline, conch ? Gfx.HDR(new Color(0.4f, 1f, 0.9f), 2f) : node.isMemory ? Gfx.HDR(new Color(1f, 0.9f, 0.7f), 1.2f) : Gfx.HDR(baseColor, 0.9f));
            string txt = "";
            if (showInternal) txt = $"{charge}/{threshold}";
            else if (charge != 0) txt = $"{charge / 2f:0.#}/{threshold / 2f:0.#}";
            if (sp.isCharacter) txt = (txt.Length > 0 ? txt + " " : "") + RunState.PersonalityNames[Mathf.Clamp(sp.personality, 0, 4)];
            label.text = txt;
            ApplyColors();
        }

        public void ApplyGhost(RunState s, Shape shape, int dir)
        {
            baseColor = ShapeColor(shape, false);
            SetSpikes(s.cfg.Shape(shape).dirs, dir, Gfx.HDR(baseColor, 0.9f), 0.34f);
            targetRatio = shownRatio = 0f;
            chargeArc.positionCount = 0;
            Gfx.SetColor(ringBg, new Color(baseColor.r * 0.3f, baseColor.g * 0.3f, baseColor.b * 0.3f, 0.4f));
            outline.gameObject.SetActive(shape == Shape.Converge);
            label.text = "";
            brightness = 0.1f;
            ApplyColors();
        }

        void ApplyColors()
        {
            float b = disabled ? 0.1f : 0.35f + brightness * 1.4f + flash * 5f;
            Gfx.SetColor(core, Gfx.HDR(Color.Lerp(baseColor, Color.white, 0.4f + flash * 0.5f), 1.2f + b));
            Gfx.SetColor(glow, Gfx.HDR(baseColor, (disabled ? 0.05f : 0.25f + brightness * 0.6f + flash * 2.4f) * (1f + 0.06f * Mathf.Sin(pulse))));
            glow.transform.localScale = Vector3.one * (0.9f + flash * 0.9f + brightness * 0.25f);
            core.transform.localScale = Vector3.one * (0.2f + flash * 0.12f);
        }

        public void Flash(float amount = 1f) { flash = Mathf.Max(flash, amount); }

        float appear;     // 0 -> 1 when spawned
        float vanish = -1f;
        Vector3 driftDir;

        /// <summary>Drift away and fade (无依 / 流光离开), then destroy.</summary>
        public void Despawn()
        {
            vanish = 0f;
            driftDir = (transform.position.sqrMagnitude > 0.01f ? transform.position.normalized : Vector3.up) * 1.2f;
            target = transform.position + driftDir;
        }

        public void Tick()
        {
            pulse += Time.deltaTime * 2.2f;
            if (appear < 1f) { appear = Mathf.Min(1f, appear + Time.deltaTime * 3f); transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, appear); }
            if (vanish >= 0f)
            {
                vanish += Time.deltaTime;
                transform.localScale = Vector3.one * Mathf.Max(0f, 1f - vanish / 0.9f);
                if (vanish > 0.9f) { Destroy(gameObject); return; }
            }
            if (arcDirty || Mathf.Abs(shownRatio - targetRatio) > 0.002f)
            {
                // charge drains visibly ("电一点点用光") instead of snapping
                float speed = targetRatio < shownRatio ? 3.5f : 14f;
                shownRatio = Mathf.MoveTowards(shownRatio, targetRatio, Time.deltaTime * speed);
                DrawArc();
                arcDirty = false;
            }
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-Time.deltaTime * 14f));
            if (flash > 0f) flash = Mathf.Max(0f, flash - Time.deltaTime * 3.2f);
            ApplyColors();
        }
    }

    // =====================================================================

    public sealed class EdgeView : MonoBehaviour
    {
        LineRenderer lr;
        Vector3 a, b;
        int level;
        bool pruned, internalMem, fading, bridgePenalty, dead;
        float swell, stable, seed;
        const int Pts = 14;

        public static EdgeView Create(Transform parent)
        {
            var go = new GameObject("Edge");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<EdgeView>();
            v.lr = Gfx.MakeLine("line", go.transform, Gfx.Additive, 5, 0.05f);
            v.lr.positionCount = Pts;
            v.lr.numCapVertices = 0;
            // taper towards the target: shows direction without an arrow head
            v.lr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.75f, 0.8f), new Keyframe(1f, 0.3f));
            v.seed = Random.value * 100f;
            return v;
        }

        public void Set(Vector3 from, Vector3 to, int lvl, bool isPruned, bool isInternal, bool isFading, bool bridgePen, bool isDead)
        {
            a = from; b = to; level = lvl; pruned = isPruned; internalMem = isInternal; fading = isFading; bridgePenalty = bridgePen; dead = isDead;
            Tick();
        }

        public void Disturb() { swell = 1f; }
        public void Stabilize() { stable = 1f; }

        public void Tick()
        {
            float dt = Time.deltaTime;
            swell = Mathf.Max(0, swell - dt * 3f);
            stable = Mathf.Max(0, stable - dt * 2f);
            var dir = (b - a);
            var len = dir.magnitude;
            if (len < 1e-4f) return;
            var n = new Vector3(-dir.y, dir.x, 0) / len;
            var off = n * 0.075f; // separate the two directions
            var p0 = a + dir * 0.2f + off;
            var p1 = b - dir * 0.24f + off;
            float sway = pruned ? 0.004f : level == 0 ? 0.05f : level == 1 ? 0.022f : 0.004f;
            if (fading) sway *= 2.2f;
            float t = Time.time * (fading ? 2.6f : 1.4f) + seed;
            for (int i = 0; i < Pts; i++)
            {
                float u = i / (float)(Pts - 1);
                float env = Mathf.Sin(u * Mathf.PI);
                float w = (Mathf.PerlinNoise(t, u * 2f + seed) - 0.5f) * 2f * sway * env;
                w += Mathf.Sin(u * Mathf.PI) * swell * 0.05f * Mathf.Sin(t * 9f + u * 6f);
                lr.SetPosition(i, Vector3.Lerp(p0, p1, u) + n * w);
            }
            float width = level == 0 ? 0.035f : level == 1 ? 0.07f : 0.11f;
            width *= 1f + swell * 0.9f + stable * 0.5f;
            lr.widthMultiplier = width;
            Color c = level == 0 ? new Color(0.45f, 0.75f, 1f) : level == 1 ? new Color(0.7f, 0.88f, 1f) : new Color(1f, 0.92f, 0.75f);
            if (bridgePenalty) c = Color.Lerp(c, new Color(1f, 0.6f, 0.35f), 0.5f);
            float inten = pruned ? 0.12f : internalMem ? 0.25f : 0.55f + level * 0.35f;
            if (dead) inten = 0.06f;
            inten += swell * 1.2f + stable * 2f;
            var col = Gfx.HDR(c, inten);
            col.a = pruned ? 0.35f : 1f;
            Gfx.SetLineColor(lr, col);

        }
    }
}
