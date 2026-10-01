using System;
using System.Collections.Generic;

namespace Lingguang.Core
{
    public struct EdgeInfo
    {
        public EdgeKey Key;
        public Glow Source;
        public Glow Target;
        public int Direction;
        public bool IsBridge;
        public bool IsMemoryInternal;
    }

    /// <summary>
    /// 光丝的几何判定（规格第 5 节）：A 的某个输出方向指向的相邻格有灵光 B、边不是沟、两格都有效 → A→B。
    /// 光丝的跨轮数据（导通次数等）放在 GameState.Edges，按 (A.id, B.id) 索引。
    /// </summary>
    public static class EdgeGeometry
    {
        /// <summary>灵光 g 当前的实际输出方向 =（形状方向 + 朝向）mod 6。</summary>
        public static int[] OutputDirections(GameConfig cfg, Glow g)
        {
            int[] rel = cfg.GetShape(g.Shape).Directions;
            var dirs = new int[rel.Length];
            for (int i = 0; i < rel.Length; i++) dirs[i] = Hex.Mod6(rel[i] + g.Orientation);
            return dirs;
        }

        /// <summary>当前所有光丝，按源 id、方向排序。O(灵光数 × 6)。</summary>
        public static List<EdgeInfo> Enumerate(GameState s, bool includePruned, bool includeMemoryInternal)
        {
            var list = new List<EdgeInfo>();
            foreach (var g in s.Glows.Values)
            {
                if (!s.IsCellActive(g.Cell)) continue;
                foreach (int dir in OutputDirections(s.Config, g))
                {
                    var t = Hex.Neighbor(g.Cell, dir);
                    if (!s.IsCellActive(t) || s.Board.IsGroove(g.Cell, t, s.Stage)) continue;
                    if (!s.CellGlow.TryGetValue(t, out int tid)) continue;
                    var tg = s.Glows[tid];
                    bool internalEdge = g.MemoryId >= 0 && g.MemoryId == tg.MemoryId;
                    if (internalEdge && !includeMemoryInternal) continue;
                    var key = new EdgeKey(g.Id, tid);
                    if (!includePruned && s.Edges.TryGetValue(key, out var st) && st.Pruned) continue;
                    list.Add(new EdgeInfo
                    {
                        Key = key, Source = g, Target = tg, Direction = dir,
                        IsBridge = s.Board.IsBridge(g.Cell, t), IsMemoryInternal = internalEdge,
                    });
                }
            }
            return list;
        }
    }

    /// <summary>直接修改棋盘的操作。只做规则校验，不管次数限制与撤销（由 GameSession 负责）。</summary>
    public static class BoardOps
    {
        public static bool CanPlace(GameState s, Cell c)
        {
            if (!s.IsCellActive(c) || s.CellGlow.ContainsKey(c)) return false;
            foreach (var p in s.Pickups) if (p.Cell == c) return false;
            return true;
        }

        /// <summary>直接放一个灵光（开局本能、测试、调试用）；只检查格子存在且为空。</summary>
        public static Glow AddGlow(GameState s, ShapeKind shape, Cell cell, int orientation, bool instinct = false)
        {
            if (!s.Board.Exists(cell)) throw new ArgumentException("格子不存在 " + cell);
            if (s.CellGlow.ContainsKey(cell)) throw new ArgumentException("格子已有灵光 " + cell);
            var g = new Glow
            {
                Id = s.NextGlowId++,
                Shape = shape,
                Orientation = Hex.Mod6(orientation),
                Cell = cell,
                IsInstinct = instinct,
                Light = s.Config.GetShape(shape).Light,
                RootRoundsLeft = instinct ? 0 : s.Config.RootingRounds,
            };
            s.Glows.Add(g.Id, g);
            s.CellGlow.Add(cell, g.Id);
            return g;
        }

        /// <summary>旋转：A 发出的全部光丝删除（导通次数清零），按新朝向重新形成。沉淀区域内的灵光消耗区域可塑度。</summary>
        public static bool Rotate(GameState s, int glowId, int orientation, out string reason)
        {
            reason = null;
            if (!s.Glows.TryGetValue(glowId, out var g)) { reason = "灵光不存在"; return false; }
            if (g.IsInstinct) { reason = "本能灵光不可旋转"; return false; }
            if (!s.IsCellActive(g.Cell)) { reason = "格子已失效"; return false; }
            int o = Hex.Mod6(orientation);
            if (o == g.Orientation) { reason = "朝向未改变"; return false; }
            if (!TryPayPlasticity(s, g.Cell, g.Cell, out reason)) return false;
            RemoveEdgesFrom(s, glowId);
            g.Orientation = o;
            return true;
        }

        /// <summary>移动：A 发出和指向 A 的全部光丝删除，在新位置重新形成。</summary>
        public static bool Move(GameState s, int glowId, Cell to, out string reason)
        {
            reason = null;
            if (!s.Glows.TryGetValue(glowId, out var g)) { reason = "灵光不存在"; return false; }
            if (g.IsInstinct) { reason = "本能灵光不可移动"; return false; }
            // 规格写“移动成员消耗可塑度”，但记忆占满成员格、内部没有空位，移出又会破坏记忆，规则待定，先禁止。
            if (g.MemoryId >= 0) { reason = "暂不支持移动记忆成员（规则待定）"; return false; }
            if (!CanPlace(s, to)) { reason = "目标格不可放置"; return false; }
            if (!TryPayPlasticity(s, g.Cell, to, out reason)) return false;

            RemoveEdgesFrom(s, glowId);
            RemoveEdgesTo(s, glowId);
            WeakLinks.RemoveAll(s, glowId);
            s.CellGlow.Remove(g.Cell);
            g.Cell = to;
            s.CellGlow.Add(to, glowId);
            return true;
        }

        /// <summary>移除一个灵光（无依漂走）：删掉它的全部光丝与微弱连接，格子空出。</summary>
        public static void RemoveGlow(GameState s, int glowId)
        {
            if (!s.Glows.TryGetValue(glowId, out var g)) return;
            RemoveEdgesFrom(s, glowId);
            RemoveEdgesTo(s, glowId);
            WeakLinks.RemoveAll(s, glowId);
            s.CellGlow.Remove(g.Cell);
            s.Glows.Remove(glowId);
        }

        /// <summary>
        /// 沉淀区域的调整代价：from、to 所在的已沉淀区域各扣一次（同一区域只扣一次）；任一区域不够则整次操作失败、不扣。
        /// </summary>
        public static bool TryPayPlasticity(GameState s, Cell from, Cell to, out string reason)
        {
            reason = null;
            int cost = s.Config.MemoryAdjustCost;
            var a = s.SettledAt(from);
            var b = s.SettledAt(to);
            if (b == a) b = null;
            if ((a != null && a.Plasticity < cost) || (b != null && b.Plasticity < cost))
            {
                reason = "沉淀区域可塑度不足";
                return false;
            }
            if (a != null) a.Plasticity -= cost;
            if (b != null) b.Plasticity -= cost;
            return true;
        }

        static void RemoveEdgesFrom(GameState s, int glowId)
        {
            var remove = new List<EdgeKey>();
            foreach (var k in s.Edges.Keys) if (k.From == glowId) remove.Add(k);
            foreach (var k in remove) s.Edges.Remove(k);
        }

        static void RemoveEdgesTo(GameState s, int glowId)
        {
            var remove = new List<EdgeKey>();
            foreach (var k in s.Edges.Keys) if (k.To == glowId) remove.Add(k);
            foreach (var k in remove) s.Edges.Remove(k);
        }
    }
}
