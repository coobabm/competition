using System;
using System.Collections.Generic;

namespace LingGuang.Core
{
    public enum Shape { Conduct = 0, Cone = 1, Converge = 2, Instinct = 3 }
    public enum Region { None = 0, I, K, C, F, T }
    public enum Stage { Infant = 0, Child, Youth, Adult, Middle, Old }

    /// <summary>
    /// Pointy-top hex grid, odd-r offset coordinates, rows grow downward (row 0 = forehead).
    /// Directions clockwise: 0 upper-right, 1 right, 2 lower-right, 3 lower-left, 4 left, 5 upper-left.
    /// Cells are addressed by index = row * Cols + col.
    /// </summary>
    public sealed class HexBoard
    {
        public readonly int Cols;
        public readonly int Rows;
        public readonly Region[] regionOf;
        readonly int midLeft;
        readonly HashSet<long> outerGaps = new HashSet<long>();
        readonly Dictionary<long, int> bridgeOpenStage = new Dictionary<long, int>();
        public readonly HashSet<long> secondaryGullies = new HashSet<long>();
        public bool GenerationFailed;

        static readonly int[,] EvenOffsets = { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 } };
        static readonly int[,] OddOffsets  = { { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

        public static readonly Region[] StageRegion = { Region.I, Region.K, Region.C, Region.F, Region.T, Region.None };

        public HexBoard(GameConfig cfg)
        {
            Rows = cfg.mapRows.Length;
            Cols = 0;
            foreach (var row in cfg.mapRows) Cols = Math.Max(Cols, row.Length);
            regionOf = new Region[Cols * Rows];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    char ch = c < cfg.mapRows[r].Length ? cfg.mapRows[r][c] : '.';
                    regionOf[Index(c, r)] = ch switch
                    {
                        'I' => Region.I, 'K' => Region.K, 'C' => Region.C, 'F' => Region.F, 'T' => Region.T, _ => Region.None
                    };
                }
            midLeft = cfg.midlineLeftCol;
            for (int i = 0; i + 3 < cfg.outerGapPairs.Length; i += 4)
                outerGaps.Add(Key(Index(cfg.outerGapPairs[i], cfg.outerGapPairs[i + 1]), Index(cfg.outerGapPairs[i + 2], cfg.outerGapPairs[i + 3])));
            foreach (var b in cfg.bridges)
                bridgeOpenStage[Key(Index(midLeft, b.row), Index(midLeft + 1, b.row))] = b.openStage;
        }

        public int CellCount => Cols * Rows;
        public int Index(int c, int r) => r * Cols + c;
        public int Col(int i) => i % Cols;
        public int Row(int i) => i / Cols;
        public bool Exists(int i) => i >= 0 && i < regionOf.Length && regionOf[i] != Region.None;
        public static long Key(int a, int b) => a < b ? ((long)a << 20) | (uint)b : ((long)b << 20) | (uint)a;

        /// <summary>Neighbour in direction d, or -1 when off-grid / no cell.</summary>
        public int Neighbor(int cell, int d)
        {
            int c = Col(cell), r = Row(cell);
            d = ((d % 6) + 6) % 6;
            int nc, nr;
            if ((r & 1) == 0) { nc = c + EvenOffsets[d, 0]; nr = r + EvenOffsets[d, 1]; }
            else { nc = c + OddOffsets[d, 0]; nr = r + OddOffsets[d, 1]; }
            if (nc < 0 || nr < 0 || nc >= Cols || nr >= Rows) return -1;
            int n = Index(nc, nr);
            return regionOf[n] == Region.None ? -1 : n;
        }

        public int DirectionTo(int a, int b)
        {
            for (int d = 0; d < 6; d++) if (Neighbor(a, d) == b) return d;
            return -1;
        }

        public int HexDistance(int a, int b)
        {
            Cube(a, out int ax, out int ay, out int az);
            Cube(b, out int bx, out int by, out int bz);
            return Math.Max(Math.Abs(ax - bx), Math.Max(Math.Abs(ay - by), Math.Abs(az - bz)));
        }

        void Cube(int i, out int x, out int y, out int z)
        {
            int c = Col(i), r = Row(i);
            x = c - (r - (r & 1)) / 2;
            z = r;
            y = -x - z;
        }

        public bool CrossesMidline(int a, int b)
        {
            int ca = Col(a), cb = Col(b);
            return (ca <= midLeft) != (cb <= midLeft);
        }

        public bool IsBridge(int a, int b) => bridgeOpenStage.ContainsKey(Key(a, b));

        /// <summary>True when a gully sits on the edge a-b for the given stage (or the cells are not adjacent).</summary>
        public bool IsGully(int a, int b, int stage)
        {
            if (!Exists(a) || !Exists(b)) return true;
            long k = Key(a, b);
            if (CrossesMidline(a, b))
            {
                if (bridgeOpenStage.TryGetValue(k, out int open)) return stage < open;
                return true;
            }
            Region ra = regionOf[a], rb = regionOf[b];
            if ((ra == Region.T) != (rb == Region.T)) return !outerGaps.Contains(k);
            return secondaryGullies.Contains(k);
        }

        public bool IsFixedGully(int a, int b)
        {
            Region ra = regionOf[a], rb = regionOf[b];
            if (CrossesMidline(a, b)) return true;
            if ((ra == Region.T) != (rb == Region.T)) return true;
            return false;
        }

        public IEnumerable<int> AllCells()
        {
            for (int i = 0; i < regionOf.Length; i++) if (regionOf[i] != Region.None) yield return i;
        }

        /// <summary>Random 3-4 interior gullies; full-board connectivity with every bridge open must hold.</summary>
        public void GenerateSecondaryGullies(GameConfig cfg, Rng rng)
        {
            var interior = new List<long>();
            foreach (int a in AllCells())
                for (int d = 0; d < 6; d++)
                {
                    int b = Neighbor(a, d);
                    if (b < 0 || b < a) continue;
                    if (regionOf[a] != regionOf[b]) continue;
                    if (IsFixedGully(a, b) || IsBridge(a, b)) continue;
                    interior.Add(Key(a, b));
                }
            for (int attempt = 0; attempt < 200; attempt++)
            {
                secondaryGullies.Clear();
                int n = rng.Range(cfg.secondaryGulliesMin, cfg.secondaryGulliesMax + 1);
                while (secondaryGullies.Count < n && secondaryGullies.Count < interior.Count)
                    secondaryGullies.Add(interior[rng.Next(interior.Count)]);
                if (IsConnected(99)) return;
            }
            secondaryGullies.Clear();
            GenerationFailed = true; // board itself is disconnected: check mapRows / outerGapPairs
        }

        public bool IsConnected(int stage)
        {
            int start = -1, total = 0;
            foreach (int c in AllCells()) { total++; if (start < 0) start = c; }
            if (start < 0) return true;
            var seen = new HashSet<int> { start };
            var q = new Queue<int>(); q.Enqueue(start);
            while (q.Count > 0)
            {
                int a = q.Dequeue();
                for (int d = 0; d < 6; d++)
                {
                    int b = Neighbor(a, d);
                    if (b < 0 || seen.Contains(b) || IsGully(a, b, stage)) continue;
                    seen.Add(b); q.Enqueue(b);
                }
            }
            return seen.Count == total;
        }

        /// <summary>Path distance that never crosses a gully, restricted to cells passing the filter.</summary>
        public Dictionary<int, int> PathDistances(int from, int maxDist, int stage, Func<int, bool> cellOk)
        {
            var dist = new Dictionary<int, int> { [from] = 0 };
            var q = new Queue<int>(); q.Enqueue(from);
            while (q.Count > 0)
            {
                int a = q.Dequeue();
                int da = dist[a];
                if (da >= maxDist) continue;
                for (int d = 0; d < 6; d++)
                {
                    int b = Neighbor(a, d);
                    if (b < 0 || dist.ContainsKey(b) || IsGully(a, b, stage) || !cellOk(b)) continue;
                    dist[b] = da + 1; q.Enqueue(b);
                }
            }
            return dist;
        }
    }

    /// <summary>Deterministic xorshift RNG whose state lives inside RunState (so snapshots stay reproducible).</summary>
    public sealed class Rng
    {
        public uint state;
        public Rng(int seed) { state = (uint)seed * 2654435761u ^ 0x9E3779B9u; if (state == 0) state = 1; Next(); }
        public Rng Clone() => new Rng(0) { state = state };
        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            state = x == 0 ? 1u : x;
            return state;
        }
        public int Next(int max) => max <= 0 ? 0 : (int)(NextUInt() % (uint)max);
        public int Next() => (int)(NextUInt() & 0x7FFFFFFF);
        public int Range(int min, int maxExclusive) => min + Next(Math.Max(1, maxExclusive - min));
        public bool Chance(int permille) => Next(1000) < permille;
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = Next(i + 1); (list[i], list[j]) = (list[j], list[i]); }
        }
    }
}
