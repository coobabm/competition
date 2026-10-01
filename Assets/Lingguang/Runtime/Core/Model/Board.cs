using System;
using System.Collections.Generic;

namespace Lingguang.Core
{
    /// <summary>
    /// 棋盘的静态几何（创建后不再改变，可在多个 GameState 快照间共享）：
    /// 格子与区域、固定沟、窄桥、本局次级沟、灯塔锚点。
    /// 老年失效格属于局内状态，放在 GameState.DisabledCells。
    /// </summary>
    public sealed class Board
    {
        public BoardLayout Layout { get; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public IReadOnlyList<Cell> Cells => cells;
        public IReadOnlyList<Cell> Anchors => anchors;
        public IReadOnlyCollection<CellEdge> SecondaryGrooves => secondary;
        /// <summary>生成次级沟用了几次抽取（调试面板显示）。</summary>
        public int SecondaryGrooveAttempts { get; private set; }

        readonly List<Cell> cells = new List<Cell>();
        readonly Dictionary<Cell, char> regionOf = new Dictionary<Cell, char>();
        readonly Dictionary<char, Stage> unlockOf = new Dictionary<char, Stage>();
        readonly HashSet<CellEdge> fixedGrooves = new HashSet<CellEdge>();
        readonly Dictionary<CellEdge, Stage> bridges = new Dictionary<CellEdge, Stage>();
        readonly List<Cell> anchors = new List<Cell>();
        HashSet<CellEdge> secondary = new HashSet<CellEdge>();

        Board(BoardLayout layout)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            ParseRegions();
            ParseRows();
            BuildMidline();
            BuildSealedRegions();
            if (layout.ExtraFixedGrooves != null)
                foreach (var pair in layout.ExtraFixedGrooves) fixedGrooves.Add(pair.ToEdge());
            if (layout.LighthouseAnchors != null) anchors.AddRange(layout.LighthouseAnchors);
        }

        /// <summary>按规格生成棋盘；rng 为 null 时不生成次级沟（单元测试用）。</summary>
        public static Board Create(BoardLayout layout, Rng rng)
        {
            var board = new Board(layout);
            if (rng != null) board.GenerateSecondaryGrooves(rng);
            return board;
        }

        /// <summary>指定次级沟（测试或调试复现用）。</summary>
        public static Board CreateWithSecondaryGrooves(BoardLayout layout, IEnumerable<CellEdge> grooves)
        {
            var board = new Board(layout);
            board.secondary = new HashSet<CellEdge>(grooves);
            return board;
        }

        public bool Exists(Cell c) => regionOf.ContainsKey(c);
        public char RegionOf(Cell c) => regionOf.TryGetValue(c, out char r) ? r : '\0';
        public bool IsUnlocked(Cell c, Stage stage) => regionOf.TryGetValue(c, out char r) && unlockOf[r] <= stage;
        public bool IsBridge(Cell a, Cell b) => bridges.ContainsKey(new CellEdge(a, b));
        public bool IsGroove(Cell a, Cell b, Stage stage) => IsGrooveWith(a, b, stage, secondary);

        public int CountRegion(char region)
        {
            int n = 0;
            foreach (var kv in regionOf) if (kv.Value == region) n++;
            return n;
        }

        /// <summary>阶段 stage 下所有已解锁格子是否连通（无向、沟不通）。</summary>
        public bool IsConnected(Stage stage) => IsConnectedWith(stage, secondary);

        /// <summary>可作为次级沟的边：同区域相邻、不是固定沟、不是窄桥。已排序，保证随机结果只取决于种子。</summary>
        public List<CellEdge> SecondaryGrooveCandidates()
        {
            var set = new SortedSet<CellEdge>();
            foreach (var c in cells)
            {
                for (int d = 0; d < Hex.DirectionCount; d++)
                {
                    var n = Hex.Neighbor(c, d);
                    if (!Exists(n) || RegionOf(n) != RegionOf(c)) continue;
                    var e = new CellEdge(c, n);
                    if (fixedGrooves.Contains(e) || bridges.ContainsKey(e)) continue;
                    set.Add(e);
                }
            }
            return new List<CellEdge>(set);
        }

        // ---------------- 构建 ----------------

        void ParseRegions()
        {
            if (Layout.Regions == null || Layout.Regions.Length == 0) throw new FormatException("棋盘缺少区域定义");
            foreach (var def in Layout.Regions)
            {
                if (string.IsNullOrEmpty(def.Letter)) throw new FormatException("区域字母为空");
                unlockOf[def.Letter[0]] = def.UnlockStage;
            }
        }

        void ParseRows()
        {
            if (Layout.Rows == null || Layout.Rows.Length == 0) throw new FormatException("棋盘缺少地图行");
            Height = Layout.Rows.Length;
            for (int row = 0; row < Layout.Rows.Length; row++)
            {
                string[] tokens = (Layout.Rows[row] ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                Width = Math.Max(Width, tokens.Length);
                for (int col = 0; col < tokens.Length; col++)
                {
                    string t = tokens[col];
                    if (t == ".") continue;
                    char letter = t[0];
                    if (!unlockOf.ContainsKey(letter))
                        throw new FormatException("地图第 " + row + " 行第 " + col + " 列出现未定义的区域字母 " + letter);
                    var cell = new Cell(col, row);
                    regionOf[cell] = letter;
                    cells.Add(cell);
                }
            }
            cells.Sort();
        }

        void BuildMidline()
        {
            int left = Layout.MidlineLeftColumn;
            if (left < 0) return;
            var bridgeRows = new Dictionary<int, Stage>();
            if (Layout.MidlineBridges != null)
                foreach (var b in Layout.MidlineBridges) bridgeRows[b.Row] = b.OpenStage;

            foreach (var c in cells)
            {
                if (c.Col != left) continue;
                for (int d = 0; d < Hex.DirectionCount; d++)
                {
                    var n = Hex.Neighbor(c, d);
                    if (!Exists(n) || n.Col != left + 1) continue;
                    var e = new CellEdge(c, n);
                    if (n.Row == c.Row && bridgeRows.TryGetValue(c.Row, out Stage open)) bridges[e] = open;
                    else fixedGrooves.Add(e);
                }
            }
        }

        void BuildSealedRegions()
        {
            if (Layout.SealedRegions == null) return;
            var gaps = new HashSet<CellEdge>();
            if (Layout.SealedRegionGaps != null)
                foreach (var g in Layout.SealedRegionGaps) gaps.Add(g.ToEdge());

            foreach (string sealedLetter in Layout.SealedRegions)
            {
                if (string.IsNullOrEmpty(sealedLetter)) continue;
                char letter = sealedLetter[0];
                foreach (var c in cells)
                {
                    if (RegionOf(c) != letter) continue;
                    for (int d = 0; d < Hex.DirectionCount; d++)
                    {
                        var n = Hex.Neighbor(c, d);
                        if (!Exists(n) || RegionOf(n) == letter) continue;
                        var e = new CellEdge(c, n);
                        if (!gaps.Contains(e)) fixedGrooves.Add(e);
                    }
                }
            }
        }

        void GenerateSecondaryGrooves(Rng rng)
        {
            var candidates = SecondaryGrooveCandidates();
            var empty = new HashSet<CellEdge>();
            // 只检查原始地图本身连通的阶段，避免改坏地图后无限重抽。
            var stagesToCheck = new List<Stage>();
            foreach (Stage st in Enum.GetValues(typeof(Stage)))
                if (IsConnectedWith(st, empty)) stagesToCheck.Add(st);

            int min = Math.Max(0, Layout.SecondaryGroovesMin);
            int max = Math.Max(min, Layout.SecondaryGroovesMax);
            int attempts = Math.Max(1, Layout.SecondaryGrooveMaxAttempts);
            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                int count = Math.Min(candidates.Count, rng.Range(min, max + 1));
                var pool = new List<CellEdge>(candidates);
                var picked = new HashSet<CellEdge>();
                for (int i = 0; i < count; i++)
                {
                    int j = rng.Range(i, pool.Count);
                    var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
                    picked.Add(pool[i]);
                }

                bool ok = true;
                foreach (var st in stagesToCheck)
                    if (!IsConnectedWith(st, picked)) { ok = false; break; }
                if (!ok) continue;

                secondary = picked;
                SecondaryGrooveAttempts = attempt;
                return;
            }
            throw new InvalidOperationException("次级沟在 " + attempts + " 次抽取内都无法保持连通，请检查地图形状");
        }

        bool IsGrooveWith(Cell a, Cell b, Stage stage, HashSet<CellEdge> sec)
        {
            var e = new CellEdge(a, b);
            if (bridges.TryGetValue(e, out Stage open)) return stage < open;
            return fixedGrooves.Contains(e) || sec.Contains(e);
        }

        bool IsConnectedWith(Stage stage, HashSet<CellEdge> sec)
        {
            int total = 0;
            Cell start = default;
            foreach (var c in cells)
                if (IsUnlocked(c, stage)) { if (total == 0) start = c; total++; }
            if (total <= 1) return true;

            var seen = new HashSet<Cell> { start };
            var queue = new Queue<Cell>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                for (int d = 0; d < Hex.DirectionCount; d++)
                {
                    var n = Hex.Neighbor(c, d);
                    if (!IsUnlocked(n, stage) || seen.Contains(n) || IsGrooveWith(c, n, stage, sec)) continue;
                    seen.Add(n);
                    queue.Enqueue(n);
                }
            }
            return seen.Count == total;
        }
    }
}
