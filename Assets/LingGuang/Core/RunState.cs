using System;
using System.Collections.Generic;
using System.Linq;

namespace LingGuang.Core
{
    public sealed class ShopOffer
    {
        public string id;
        public int price;
        public bool sold;
        public ShopOffer Clone() => (ShopOffer)MemberwiseClone();
    }

    public sealed class RunStats
    {
        public Dictionary<string, int> patterns = new Dictionary<string, int>();
        public int reverbCount;
        public float maxOvershoot;
        public int itemsUsed;
        public int drugsUsed;
        public int warnings;
        public int hiddenCollected;
        public int rotations;
        public int moves;
        public int confirms;
        public int roundsWithFamiliar;
        public int knownCount;
        public int familiarEver;
        public int broken;
        public int charactersMet;
        public bool longRelation;
        public RunStats Clone()
        {
            var r = (RunStats)MemberwiseClone();
            r.patterns = new Dictionary<string, int>(patterns);
            return r;
        }
    }

    public sealed class ConfirmResult
    {
        public List<SimEvent> events;
        public ChainStats stats;
        public string pattern;
    }

    public sealed class SettleReport
    {
        public RoundScore score;
        public bool patternVoided;
        public bool stageEnded;
        public int newStage = -1;
        public bool gameOver;
        public bool victory;
        public List<int> driftedCells = new List<int>();
        public List<int> disabledCells = new List<int>();
        public int prunedEdges;
        public int decayedEdges;
        public List<int> memoryCells;
        public int coinsGained;
        public int insightGained;
        public List<string> log = new List<string>();
    }

    public sealed partial class RunState
    {
        public GameConfig cfg;
        public HexBoard board;
        public int seed;
        public Rng rng;
        public int stage;
        public int roundInStage;
        public Phase phase = Phase.Prepare;

        public Dictionary<int, Spark> sparks = new Dictionary<int, Spark>();
        public Dictionary<int, Node> nodes = new Dictionary<int, Node>();
        public Dictionary<long, Edge> edges = new Dictionary<long, Edge>();
        public int[] cellSpark;
        public Dictionary<Region, RegionState> regions = new Dictionary<Region, RegionState>();
        public Dictionary<long, int> weakLinks = new Dictionary<long, int>();
        public List<Pickup> pickups = new List<Pickup>();
        public HashSet<int> disabledCells = new HashSet<int>();
        public int nextId = 1;

        public List<Shape> candidates = new List<Shape>();
        public bool placedThisRound;
        public int startsLeft, movesLeft;
        public bool trialUsed;
        public int drugsUsedThisRound;
        public List<string> items = new List<string>();
        public List<string> drugs = new List<string>();
        public int coins;
        public List<ShopOffer> shop = new List<ShopOffer>();
        public string[] gateByStage = new string[6];
        public List<ActiveEffect> effects = new List<ActiveEffect>();
        public Region earphoneRegion = Region.C;
        public int conchSpark = -1;
        public bool separationUnlocked;
        public int memoryCounter;

        // insight (领悟)
        public int insight;
        public bool reviewActive;
        public Dictionary<string, int> patternLevel = new Dictionary<string, int>();
        public Dictionary<string, int> patternProgress = new Dictionary<string, int>();
        public Dictionary<string, int> patternsThisRound = new Dictionary<string, int>();
        public HashSet<int> engravedMemories = new HashSet<int>();

        // scoring for the current round
        public int roundLight;
        public float roundAddMult;
        public int patternLight;
        public float patternMult;
        public string roundTopPattern;
        public string lastRoundTopPattern;
        public string lastPattern;
        public int freshK;
        public bool firstConfirmDone;
        public bool firstFireDone;
        public List<int> firedNodesThisRound = new List<int>();
        public List<RoundScore> history = new List<RoundScore>();
        public RunStats stats = new RunStats();

        public int RoundIndex => stage * 3 + roundInStage;
        public bool IsBossRound => roundInStage == 2;
        public int Target => cfg.Target(RoundIndex);
        public EffectDef CurrentGate => IsBossRound && gateByStage[stage] != null ? EffectDef.Get(gateByStage[stage]) : null;
        public EffectDef StageGate => gateByStage[stage] != null ? EffectDef.Get(gateByStage[stage]) : null;
        public int ItemSlots => cfg.itemSlotsByStage[Math.Min(stage, cfg.itemSlotsByStage.Length - 1)];

        // ------------------------------------------------------------------ construction

        RunState() { }

        public static RunState NewRun(GameConfig cfg, int seed, bool separationUnlocked = false)
        {
            var s = new RunState { cfg = cfg, seed = seed, rng = new Rng(seed), separationUnlocked = separationUnlocked };
            s.board = new HexBoard(cfg);
            s.board.GenerateSecondaryGullies(cfg, s.rng);
            s.cellSpark = Enumerable.Repeat(-1, s.board.CellCount).ToArray();
            foreach (Region r in new[] { Region.I, Region.K, Region.C, Region.F, Region.T })
                s.regions[r] = new RegionState { region = r, plasticity = cfg.plasticityInit };
            // gates: one per stage, drawn from the seed
            for (int st = 0; st < 6; st++)
            {
                var pool = EffectDef.All.Where(d => d.kind == DefKind.Gate && d.stage == st && (d.id != "separation" || separationUnlocked)).ToList();
                s.gateByStage[st] = pool.Count > 0 ? pool[s.rng.Next(pool.Count)].id : null;
            }
            // instinct sparks in region I
            var iCells = s.board.AllCells().Where(c => s.board.regionOf[c] == Region.I).ToList();
            s.rng.Shuffle(iCells);
            for (int i = 0; i < cfg.instinctCount && i < iCells.Count; i++)
                s.AddSpark(Shape.Instinct, iCells[i], 0, false);
            s.StartRound();
            return s;
        }

        /// <summary>Blank board for tests/sandbox: no instincts, no random gullies, regions unlocked up to stage.</summary>
        public static RunState CreateSandbox(GameConfig cfg, int stage = 5, int seed = 1)
        {
            var s = new RunState { cfg = cfg, seed = seed, rng = new Rng(seed), stage = stage };
            s.board = new HexBoard(cfg);
            s.cellSpark = Enumerable.Repeat(-1, s.board.CellCount).ToArray();
            foreach (Region r in new[] { Region.I, Region.K, Region.C, Region.F, Region.T })
                s.regions[r] = new RegionState { region = r, plasticity = cfg.plasticityInit };
            s.startsLeft = 99;
            s.movesLeft = 99;
            return s;
        }

        public RunState Clone()
        {
            var c = (RunState)MemberwiseClone();
            c.rng = rng.Clone();
            c.sparks = sparks.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
            c.nodes = nodes.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
            c.edges = edges.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
            c.cellSpark = (int[])cellSpark.Clone();
            c.regions = regions.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
            c.weakLinks = new Dictionary<long, int>(weakLinks);
            c.pickups = pickups.Select(p => p.Clone()).ToList();
            c.disabledCells = new HashSet<int>(disabledCells);
            c.candidates = new List<Shape>(candidates);
            c.items = new List<string>(items);
            c.drugs = new List<string>(drugs);
            c.shop = shop.Select(o => o.Clone()).ToList();
            c.gateByStage = (string[])gateByStage.Clone();
            c.effects = effects.Select(e => e.Clone()).ToList();
            c.firedNodesThisRound = new List<int>(firedNodesThisRound);
            c.history = new List<RoundScore>(history);
            c.stats = stats.Clone();
            c.patternLevel = new Dictionary<string, int>(patternLevel);
            c.patternProgress = new Dictionary<string, int>(patternProgress);
            c.patternsThisRound = new Dictionary<string, int>(patternsThisRound);
            c.engravedMemories = new HashSet<int>(engravedMemories);
            c.insightLog = new List<string>(insightLog);
            return c;
        }

        // ------------------------------------------------------------------ helpers

        public static int RegionStage(Region r) => r switch
        {
            Region.I => 0, Region.K => 1, Region.C => 2, Region.F => 3, Region.T => 4, _ => 99
        };

        public bool IsUnlocked(int cell) => board.Exists(cell) && RegionStage(board.regionOf[cell]) <= stage;
        public bool IsActiveCell(int cell) => IsUnlocked(cell) && !disabledCells.Contains(cell);
        public RegionState RegionStateOf(Region r) => regions.TryGetValue(r, out var rs) ? rs : null;
        public Spark SparkAt(int cell) => cell >= 0 && cell < cellSpark.Length && cellSpark[cell] >= 0 ? sparks[cellSpark[cell]] : null;
        public Node NodeOf(Spark sp) => nodes[sp.nodeId];

        public static long PairKey(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        public static void SplitPair(long k, out int a, out int b) { a = (int)(k >> 32); b = (int)(k & 0xFFFFFFFF); }

        public void AddWeakLinks(List<int> nodeIds)
        {
            var uniq = nodeIds.Distinct().ToList();
            for (int i = 0; i < uniq.Count; i++)
                for (int j = i + 1; j < uniq.Count; j++)
                {
                    long k = PairKey(uniq[i], uniq[j]);
                    weakLinks.TryGetValue(k, out int v);
                    weakLinks[k] = v + 1;
                }
        }

        public int WeakLink(int nodeA, int nodeB) => weakLinks.TryGetValue(PairKey(nodeA, nodeB), out int v) ? v : 0;

        public Spark AddSpark(Shape shape, int cell, int dir, bool placedByPlayer)
        {
            var sp = new Spark
            {
                id = nextId++, shape = shape, dir = ((dir % 6) + 6) % 6, cell = cell, light = cfg.Shape(shape).light,
                placedByPlayer = placedByPlayer, placedRound = RoundIndex
            };
            var n = new Node { id = nextId++, region = board.regionOf[cell] };
            n.sparks.Add(sp.id);
            sp.nodeId = n.id;
            sparks[sp.id] = sp;
            nodes[n.id] = n;
            cellSpark[cell] = sp.id;
            RebuildEdges();
            return sp;
        }

        public Spark AddSpark(Shape shape, int col, int row, int dir) => AddSpark(shape, board.Index(col, row), dir, false);

        public void RemoveSpark(Spark sp)
        {
            RemoveEdgesOf(sp.id, true, true);
            cellSpark[sp.cell] = -1;
            sparks.Remove(sp.id);
            var n = nodes[sp.nodeId];
            n.sparks.Remove(sp.id);
            if (n.sparks.Count == 0)
            {
                nodes.Remove(n.id);
                foreach (var k in weakLinks.Keys.ToList())
                {
                    SplitPair(k, out int a, out int b);
                    if (a == n.id || b == n.id) weakLinks.Remove(k);
                }
            }
            if (conchSpark == sp.id) conchSpark = -1;
            RebuildEdges();
        }

        void RemoveEdgesOf(int sparkId, bool outgoing, bool incoming)
        {
            foreach (var k in edges.Keys.ToList())
            {
                var e = edges[k];
                if ((outgoing && e.from == sparkId) || (incoming && e.to == sparkId)) edges.Remove(k);
            }
        }

        /// <summary>Edges exist wherever an output direction points at a neighbouring spark without a gully. Counts persist.</summary>
        public void RebuildEdges()
        {
            var valid = new HashSet<long>();
            foreach (var sp in sparks.Values)
            {
                foreach (int rel in Dirs(sp))
                {
                    int nc = board.Neighbor(sp.cell, (rel + sp.dir) % 6);
                    if (nc < 0 || !IsUnlocked(nc) || board.IsGully(sp.cell, nc, stage)) continue;
                    int t = cellSpark[nc];
                    if (t < 0) continue;
                    long k = Edge.Key(sp.id, t);
                    valid.Add(k);
                    if (!edges.ContainsKey(k)) edges[k] = new Edge { from = sp.id, to = t };
                }
            }
            foreach (var k in edges.Keys.ToList()) if (!valid.Contains(k)) edges.Remove(k);
        }

        public int[] Dirs(Spark sp) => sp.dirsOverride ?? cfg.Shape(sp.shape).dirs;

        public Edge EdgeBetween(Spark a, Spark b) => edges.TryGetValue(Edge.Key(a.id, b.id), out var e) ? e : null;

        public List<int> LighthouseCells()
        {
            var result = new HashSet<int>();
            int R = cfg.lighthouseRadiusByStage[Math.Min(stage, cfg.lighthouseRadiusByStage.Length - 1)];
            var anchors = board.AllCells().Where(c => board.Row(c) == cfg.lighthouseAnchorRow && IsActiveCell(c)).ToList();
            if (R > 0)
            {
                foreach (int a in anchors)
                    foreach (var kv in board.PathDistances(a, R - 1, stage, IsActiveCell)) result.Add(kv.Key);
            }
            else
            {
                var focus = FocusCells();
                if (focus.Count > 0) foreach (int c in focus) result.Add(c);
                else foreach (int a in anchors) result.Add(a);
            }
            var list = result.ToList();
            list.Sort();
            return list;
        }

        /// <summary>Old age: the deepest memory (most triggered) whose members are still alive.</summary>
        public List<int> FocusCells()
        {
            if (cfg.lighthouseFocus == "Character")
            {
                var best = sparks.Values.Where(sp => sp.isCharacter && IsActiveCell(sp.cell))
                    .OrderByDescending(sp => CharacterRelation(sp)).ThenBy(sp => sp.id).FirstOrDefault();
                if (best != null) return new List<int> { best.cell };
            }
            foreach (var n in nodes.Values.Where(n => n.isMemory).OrderByDescending(n => n.triggerCount).ThenBy(n => n.id))
            {
                var cells = n.sparks.Select(id => sparks[id].cell).Where(IsActiveCell).ToList();
                if (cells.Count > 0) return cells;
            }
            return new List<int>();
        }

        public int CharacterRelation(Spark ch)
        {
            int best = 0;
            foreach (var e in edges.Values)
            {
                if (e.pruned || (e.from != ch.id && e.to != ch.id)) continue;
                int other = e.from == ch.id ? e.to : e.from;
                if (sparks.TryGetValue(other, out var o) && o.isCharacter) continue; // only bonds with your own sparks
                best = Math.Max(best, e.count);
            }
            return best;
        }

        public void CollectPickup(Pickup p)
        {
            stats.hiddenCollected++;
            if (items.Count < ItemSlots && !items.Contains(p.itemId)) { items.Add(p.itemId); stats.itemsUsed++; }
            else coins += 2;
        }

        public bool IsAnchored(Spark sp)
        {
            foreach (var e in edges.Values)
                if (!e.pruned && e.count > 0 && (e.from == sp.id || e.to == sp.id)) return true;
            return false;
        }
    }
}
