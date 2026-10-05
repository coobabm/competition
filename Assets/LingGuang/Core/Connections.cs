using System;
using System.Collections.Generic;

namespace LingGuang.Core
{
    public sealed partial class RunState
    {
        // Only 64 masks; reuse the arrays in both simulator and presentation, without per-frame allocations.
        static readonly int[][] RoutedDirections = BuildRoutedDirections();

        static int[][] BuildRoutedDirections()
        {
            var result = new int[64][];
            for (int mask = 0; mask < result.Length; mask++)
            {
                var dirs = new List<int>(6);
                for (int d = 0; d < 6; d++) if ((mask & (1 << d)) != 0) dirs.Add(d);
                result[mask] = dirs.ToArray();
            }
            return result;
        }

        public int ConnectionCapacity(Spark sp) => Math.Min(6, (sp.dirsOverride ?? cfg.Shape(sp.shape).dirs).Length);

        // Editing retains actual live outgoing edges, reclaiming directions whose targets moved away.
        // Explicitly cut routes stay cut across rebuilds; retained edges keep their learning.
        int RoutingMask(Spark sp)
        {
            int mask = 0;
            foreach (int rel in Dirs(sp))
            {
                int cell = board.Neighbor(sp.cell, (rel + sp.dir) % 6);
                var target = SparkAt(cell);
                var edge = target != null ? EdgeBetween(sp, target) : null;
                if (edge != null && !edge.pruned && IsActiveCell(cell)) mask |= 1 << rel;
            }
            return mask;
        }

        bool ValidateConnection(int sourceId, int targetId, out string why)
        {
            why = null;
            if (phase != Phase.Prepare) why = "只能在每轮准备阶段、两次发动之间改线";
            else if (!sparks.TryGetValue(sourceId, out var source) || !sparks.TryGetValue(targetId, out var target)) why = "请先选择两个灵光";
            else if (sourceId == targetId) why = "请选择另一个灵光";
            else if (source.isCharacter) why = "不能修改流光的输出；可以从自己的灵光连向它";
            else if (!IsActiveCell(source.cell) || !IsActiveCell(target.cell)) why = "未解锁或失效的格子不能连线";
            else if (source.nodeId == target.nodeId) why = "记忆内部共同闪烁，只能调整它的对外连接";
            else if (board.DirectionTo(source.cell, target.cell) < 0) why = "只能连接相邻的灵光，请先移动到相邻格";
            else if (board.IsGully(source.cell, target.cell, stage)) why = "沟壑阻断连接，请经由已开放的窄桥";
            return why == null;
        }

        bool PlanConnection(int sourceId, int targetId, out int mask, out string why)
        {
            mask = 0;
            if (!ValidateConnection(sourceId, targetId, out why)) return false;
            var source = sparks[sourceId];
            int rel = (board.DirectionTo(source.cell, sparks[targetId].cell) - source.dir + 6) % 6;
            int capacity = ConnectionCapacity(source);
            mask = capacity == 1 ? 1 << rel : RoutingMask(source) | (1 << rel);
            if (RoutedDirections[mask].Length > capacity)
            {
                why = $"{cfg.Shape(source.shape).name}最多 {capacity} 条输出，请先断开一条";
                return false;
            }
            return true;
        }

        public bool CanConnect(int sourceId, int targetId, bool bidirectional, out string why)
        {
            if (!PlanConnection(sourceId, targetId, out _, out why)) return false;
            return !bidirectional || PlanConnection(targetId, sourceId, out _, out why);
        }

        /// <summary>
        /// Set one or both directed links atomically. Wiring is independent of movement/plasticity;
        /// instincts and memory boundaries are editable, characters and memory internals are not.
        /// One-output sparks reroute instead of gaining extra outputs. Unchanged edges retain learning.
        /// </summary>
        public bool TryConnect(int sourceId, int targetId, bool bidirectional = false)
        {
            if (!PlanConnection(sourceId, targetId, out int forwardMask, out var why)) return Fail(why);
            int reverseMask = 0;
            if (bidirectional && !PlanConnection(targetId, sourceId, out reverseMask, out why)) return Fail(why);

            sparks[sourceId].connectionMask = forwardMask;
            if (bidirectional) sparks[targetId].connectionMask = reverseMask;
            ResetPrunedConnection(sourceId, targetId);
            if (bidirectional) ResetPrunedConnection(targetId, sourceId);
            RebuildEdges();
            lastError = null;
            return true;
        }

        void ResetPrunedConnection(int from, int to)
        {
            long key = Edge.Key(from, to);
            if (edges.TryGetValue(key, out var edge) && edge.pruned) edges.Remove(key);
        }

        public bool CanDisconnect(int sourceId, int targetId, out string why)
        {
            if (!ValidateConnection(sourceId, targetId, out why)) return false;
            if (!edges.ContainsKey(Edge.Key(sourceId, targetId))) why = "这个方向没有连接；双向线需分别断开";
            return why == null;
        }

        /// <summary>Cut only source → target. Reconnecting creates a newborn edge, not a restored trained one.</summary>
        public bool Disconnect(int sourceId, int targetId)
        {
            if (!CanDisconnect(sourceId, targetId, out var why)) return Fail(why);
            var source = sparks[sourceId];
            int rel = (board.DirectionTo(source.cell, sparks[targetId].cell) - source.dir + 6) % 6;
            source.connectionMask = RoutingMask(source) & ~(1 << rel);
            RebuildEdges();
            lastError = null;
            return true;
        }
    }
}
