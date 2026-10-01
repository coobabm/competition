using System;
using System.Collections.Generic;

namespace Lingguang.Core
{
    /// <summary>
    /// 一局的完整状态（Model 层，不引用 UnityEngine）。
    /// Clone() 是深拷贝，用于 Esc 撤销与试运行；Board 与 Config 是只读的，拷贝间共享。
    /// </summary>
    public sealed class GameState
    {
        public readonly GameConfig Config;
        public readonly Board Board;
        public readonly int Seed;
        public Rng Rng;

        public Stage Stage;
        public int RoundInStage;
        /// <summary>本阶段关口 id（阶段第 1 轮公开，Boss 轮生效）；null 表示本阶段无关口。</summary>
        public string StageGateId;
        public bool SeparationUnlocked;

        public readonly SortedDictionary<int, Glow> Glows = new SortedDictionary<int, Glow>();
        public readonly Dictionary<Cell, int> CellGlow = new Dictionary<Cell, int>();
        public int NextGlowId = 1;

        public readonly Dictionary<EdgeKey, EdgeStats> Edges = new Dictionary<EdgeKey, EdgeStats>();
        /// <summary>微弱连接：两灵光 id 组成的无序对 → 强度。</summary>
        public readonly Dictionary<long, int> WeakLinks = new Dictionary<long, int>();

        public readonly SortedDictionary<int, Memory> Memories = new SortedDictionary<int, Memory>();
        public int NextMemoryId = 0;
        /// <summary>已沉淀的区域（按区域字母）。</summary>
        public readonly SortedDictionary<char, SettledRegion> SettledRegions = new SortedDictionary<char, SettledRegion>();

        /// <summary>老年灯塔照着的灵光 id（记忆最深刻的地方）；-1 表示没有。</summary>
        public int LighthouseFocusGlowId = -1;
        public readonly HashSet<Cell> DisabledCells = new HashSet<Cell>();
        public readonly List<Pickup> Pickups = new List<Pickup>();
        public readonly List<string> Inventory = new List<string>();
        public int Coins;

        public RoundState Round = new RoundState();
        public PatternKind LastRoundTopPattern = PatternKind.None;
        public PatternKind FreshnessLastPattern = PatternKind.None;
        public int FreshnessStreak;

        public bool RunOver;
        public bool RunWon;
        public int TotalRoundsPlayed;

        public GameState(GameConfig config, Board board, Rng rng, int seed)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
            Seed = seed;
        }

        public StageDef StageDef => Config.GetStage(Stage);
        public bool IsBossRound => RoundInStage == Config.RoundsPerStage - 1;
        public int CurrentTarget => StageDef.Targets[RoundInStage];

        /// <summary>格子存在、已解锁、未失效：可放置、参与传播。</summary>
        public bool IsCellActive(Cell c) => Board.IsUnlocked(c, Stage) && !DisabledCells.Contains(c);

        public Glow GlowAt(Cell c) => CellGlow.TryGetValue(c, out int id) ? Glows[id] : null;

        public EdgeStats GetOrCreateEdge(EdgeKey key)
        {
            if (!Edges.TryGetValue(key, out var st))
            {
                st = new EdgeStats();
                Edges.Add(key, st);
            }
            return st;
        }

        /// <summary>格子所在区域若已沉淀，返回其共用可塑度记录；否则 null。</summary>
        public SettledRegion SettledAt(Cell c) => SettledRegions.TryGetValue(Board.RegionOf(c), out var r) ? r : null;

        public int EdgeCount(EdgeKey key) => Edges.TryGetValue(key, out var st) ? st.Count : 0;

        public GameState Clone() => CloneWithConfig(Config);

        /// <summary>换一份配置继续本局（热重载）。地图形状必须相同，Board 仍共享。</summary>
        public GameState CloneWithConfig(GameConfig config)
        {
            var c = new GameState(config, Board, Rng.Clone(), Seed)
            {
                Stage = Stage,
                RoundInStage = RoundInStage,
                StageGateId = StageGateId,
                SeparationUnlocked = SeparationUnlocked,
                NextGlowId = NextGlowId,
                NextMemoryId = NextMemoryId,
                LighthouseFocusGlowId = LighthouseFocusGlowId,
                Coins = Coins,
                Round = Round.Clone(),
                LastRoundTopPattern = LastRoundTopPattern,
                FreshnessLastPattern = FreshnessLastPattern,
                FreshnessStreak = FreshnessStreak,
                RunOver = RunOver,
                RunWon = RunWon,
                TotalRoundsPlayed = TotalRoundsPlayed,
            };
            foreach (var kv in Glows) c.Glows.Add(kv.Key, kv.Value.Clone());
            foreach (var kv in CellGlow) c.CellGlow.Add(kv.Key, kv.Value);
            foreach (var kv in Edges) c.Edges.Add(kv.Key, kv.Value.Clone());
            foreach (var kv in WeakLinks) c.WeakLinks.Add(kv.Key, kv.Value);
            foreach (var kv in Memories) c.Memories.Add(kv.Key, kv.Value.Clone());
            foreach (var kv in SettledRegions) c.SettledRegions.Add(kv.Key, kv.Value.Clone());
            c.DisabledCells.UnionWith(DisabledCells);
            foreach (var p in Pickups) c.Pickups.Add(p.Clone());
            c.Inventory.AddRange(Inventory);
            return c;
        }
    }

    /// <summary>
    /// “发光单位”：普通灵光用自身 id；记忆用 MemoryBase + 记忆 id。
    /// 记忆对外表现为一个大灵光：共用电量、一起闪。
    /// </summary>
    public static class Units
    {
        public const int MemoryBase = 1 << 24;

        public static int Of(GameState s, Glow g) => g.MemoryId >= 0 ? MemoryBase + g.MemoryId : g.Id;
        public static bool IsMemory(int unit) => unit >= MemoryBase;
        public static Memory MemoryOf(GameState s, int unit) => s.Memories[unit - MemoryBase];

        public static bool IsFired(GameState s, int unit) => IsMemory(unit) ? MemoryOf(s, unit).Fired : s.Glows[unit].Fired;

        public static int Charge(GameState s, int unit) => IsMemory(unit) ? MemoryOf(s, unit).Charge : s.Glows[unit].Charge;

        public static int FireCount(GameState s, int unit) => IsMemory(unit) ? MemoryOf(s, unit).FireCount : s.Glows[unit].FireCount;

        public static void SetCharge(GameState s, int unit, int value)
        {
            if (IsMemory(unit)) MemoryOf(s, unit).Charge = value;
            else s.Glows[unit].Charge = value;
        }

        public static void AddCharge(GameState s, int unit, int amount)
        {
            if (IsMemory(unit)) MemoryOf(s, unit).Charge += amount;
            else s.Glows[unit].Charge += amount;
        }

        /// <summary>标记闪光：电量清零、本轮闪光次数 +1；记忆的成员一并标记。</summary>
        public static void MarkFired(GameState s, int unit)
        {
            if (IsMemory(unit))
            {
                var ch = MemoryOf(s, unit);
                ch.Fired = true;
                ch.Charge = 0;
                ch.FireCount++;
                foreach (int id in ch.Members) { var m = s.Glows[id]; m.Fired = true; m.Charge = 0; m.FireCount = ch.FireCount; }
            }
            else
            {
                var g = s.Glows[unit];
                g.Fired = true;
                g.Charge = 0;
                g.FireCount++;
            }
        }

        /// <summary>按 id 升序的成员列表。</summary>
        public static List<Glow> Members(GameState s, int unit)
        {
            var list = new List<Glow>();
            if (IsMemory(unit)) foreach (int id in MemoryOf(s, unit).Members) list.Add(s.Glows[id]);
            else list.Add(s.Glows[unit]);
            return list;
        }

        /// <summary>代表 id（记忆取最小成员 id），用于确定性排序。</summary>
        public static int Representative(GameState s, int unit) => IsMemory(unit) ? MemoryOf(s, unit).Members[0] : unit;

        /// <summary>单位是否还有至少一个处于有效格的成员。</summary>
        public static bool IsActive(GameState s, int unit)
        {
            if (!IsMemory(unit)) return s.IsCellActive(s.Glows[unit].Cell);
            foreach (int id in MemoryOf(s, unit).Members)
                if (s.IsCellActive(s.Glows[id].Cell)) return true;
            return false;
        }
    }

    public static class WeakLinks
    {
        public static long Key(int a, int b)
        {
            int lo = Math.Min(a, b), hi = Math.Max(a, b);
            return ((long)lo << 32) | (uint)hi;
        }

        public static void Increment(GameState s, int a, int b)
        {
            if (a == b) return;
            long k = Key(a, b);
            s.WeakLinks.TryGetValue(k, out int v);
            s.WeakLinks[k] = v + 1;
        }

        public static int Strength(GameState s, int a, int b) => s.WeakLinks.TryGetValue(Key(a, b), out int v) ? v : 0;

        /// <summary>与 id 有微弱连接的灵光，升序。O(连接数)，连接数上限约 42×41/2。</summary>
        public static List<int> PartnersOf(GameState s, int id)
        {
            var list = new List<int>();
            int min = Math.Max(1, s.Config.WeakLinkMinStrength);
            foreach (var kv in s.WeakLinks)
            {
                if (kv.Value < min) continue;
                int lo = (int)(kv.Key >> 32), hi = (int)(kv.Key & 0xFFFFFFFF);
                if (lo == id) list.Add(hi);
                else if (hi == id) list.Add(lo);
            }
            list.Sort();
            return list;
        }

        public static void RemoveAll(GameState s, int id)
        {
            var remove = new List<long>();
            foreach (var kv in s.WeakLinks)
            {
                int lo = (int)(kv.Key >> 32), hi = (int)(kv.Key & 0xFFFFFFFF);
                if (lo == id || hi == id) remove.Add(kv.Key);
            }
            foreach (long k in remove) s.WeakLinks.Remove(k);
        }
    }
}
