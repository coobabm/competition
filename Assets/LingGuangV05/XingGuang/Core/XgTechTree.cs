using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// One square on the 科技 page: a single catalog node, or a chain of catalog nodes that the simulation treats as
    /// the levels of one thing. Presentation only: the members stay ordinary catalog nodes, bought one at a time through
    /// <see cref="XgSim.BuyNode"/>, so prices, prerequisites, stage gates and the save are exactly the catalog's.
    /// </summary>
    public sealed class XgTechEntry
    {
        /// <summary>The first member's id; the page names its square after it.</summary>
        public string id;
        /// <summary>Chain order (each member's parent is the one before it). A single node has one member.</summary>
        public readonly List<XgNode> members = new List<XgNode>();
        public XgNode First => members[0];
        public bool IsChain => members.Count > 1;
        /// <summary>Where the square is drawn: the first member's stage and lane.</summary>
        public int Stage => First.stage;
        public string Lane => First.lane;
        public bool Covers(string nodeId) { foreach (var n in members) if (n.id == nodeId) return true; return false; }
    }

    /// <summary>
    /// The 科技 tree as squares with levels (the Upgrade Tree look). Built from <see cref="XgCatalog.Nodes"/> at runtime:
    /// depth caps, width caps and automation steps form ladders in the catalog (each step's parent is the step before),
    /// so each ladder becomes one square whose level is the number of steps owned. Nodes the simulation already counts
    /// in levels (加薪, 自动答题) are one square with their own level. Everything else is one square per node.
    /// </summary>
    public static class XgTechTree
    {
        static List<XgTechEntry> entries;
        static Dictionary<string, XgTechEntry> byNode;
        static XgNode[] builtFrom;

        /// <summary>Every square in catalog order (rebuilt if the catalog array is replaced).</summary>
        public static IReadOnlyList<XgTechEntry> Entries { get { Ensure(); return entries; } }

        /// <summary>The square that shows this catalog node, or null.</summary>
        public static XgTechEntry EntryOf(string nodeId)
        {
            Ensure();
            return nodeId != null && byNode.TryGetValue(nodeId, out var e) ? e : null;
        }

        /// <summary>Kinds whose catalog ladders become one square with levels.</summary>
        public static bool Ladder(XgNodeKind kind) => kind == XgNodeKind.Depth || kind == XgNodeKind.Width || kind == XgNodeKind.Auto;

        static void Ensure()
        {
            var nodes = XgCatalog.Nodes;
            if (entries != null && ReferenceEquals(builtFrom, nodes)) return;
            builtFrom = nodes;
            entries = new List<XgTechEntry>();
            byNode = new Dictionary<string, XgTechEntry>();
            var index = new Dictionary<string, XgNode>();
            foreach (var n in nodes) index[n.id] = n;
            // Children of the same kind, so a ladder can be walked from its first step.
            var next = new Dictionary<string, List<XgNode>>();
            foreach (var n in nodes)
                if (Ladder(n.kind) && n.parent != null && index.TryGetValue(n.parent, out var p) && p.kind == n.kind)
                {
                    if (!next.TryGetValue(p.id, out var list)) next[p.id] = list = new List<XgNode>();
                    list.Add(n);
                }
            foreach (var n in nodes)
            {
                if (byNode.ContainsKey(n.id)) continue;
                // A ladder step is drawn by its ladder's first step.
                if (Ladder(n.kind) && n.parent != null && index.TryGetValue(n.parent, out var parent) && parent.kind == n.kind) continue;
                var entry = new XgTechEntry { id = n.id };
                entry.members.Add(n); byNode[n.id] = entry;
                // Follow the ladder while it stays a single line; a fork starts squares of its own.
                for (var at = n; Ladder(n.kind) && next.TryGetValue(at.id, out var children) && children.Count == 1;)
                {
                    at = children[0];
                    if (byNode.ContainsKey(at.id)) break;
                    entry.members.Add(at); byNode[at.id] = entry;
                }
                entries.Add(entry);
            }
            // Steps after a fork (none in the current catalog) still get a square each.
            foreach (var n in nodes)
                if (!byNode.ContainsKey(n.id))
                {
                    var entry = new XgTechEntry { id = n.id };
                    entry.members.Add(n); byNode[n.id] = entry; entries.Add(entry);
                }
        }

        /// <summary>Levels owned: ladder steps owned, or the node's own level (加薪 ×n, 自动答题 Lv n), or 0/1.</summary>
        public static int Level(XgTechEntry e, XgSim sim)
        {
            if (e.IsChain) { int owned = 0; foreach (var n in e.members) if (sim.Has(n.id)) owned++; return owned; }
            var node = e.First;
            if (XgSim.IsAtlas(node)) return sim.Status(node, null) == XgSim.NodeStatus.Owned ? 1 : 0;
            if (node.tree == "label") return sim.LabelNodeLevel(node);
            return sim.Has(node.id) ? 1 : 0;
        }

        public static int MaxLevel(XgTechEntry e) => e.IsChain ? e.members.Count : Math.Max(1, e.First.maxLevel);

        /// <summary>The catalog node the next purchase buys; null when the square is maxed (or never bought: the 图鉴).</summary>
        public static XgNode Next(XgTechEntry e, XgSim sim)
        {
            if (e.IsChain) { foreach (var n in e.members) if (!sim.Has(n.id)) return n; return null; }
            if (XgSim.IsAtlas(e.First)) return null;
            return Level(e, sim) < MaxLevel(e) ? e.First : null;
        }

        /// <summary>The simulation's status of the next purchase; Owned once every level is owned.</summary>
        public static XgSim.NodeStatus Status(XgTechEntry e, XgSim sim, IXgHost host)
        {
            if (!e.IsChain) return sim.Status(e.First, host);
            var next = Next(e, sim);
            return next == null ? XgSim.NodeStatus.Owned : sim.Status(next, host);
        }

        public static double Cost(XgTechEntry e, XgSim sim)
        {
            var next = Next(e, sim);
            return next == null ? double.PositiveInfinity : sim.NodeCost(next);
        }

        /// <summary>Shown once any of its levels is revealed (the simulation decides what each stage reveals).</summary>
        public static bool Visible(XgTechEntry e, XgSim sim)
        {
            foreach (var n in e.members) if (sim.NodeVisible(n)) return true;
            return false;
        }

        /// <summary>Buys one level through the simulation's own purchase path.</summary>
        public static bool Buy(XgTechEntry e, XgSim sim, IXgHost host)
        {
            var next = Next(e, sim);
            return next != null && sim.BuyNode(next.id, host);
        }

        /// <summary>
        /// The square's name. A ladder is named for what it raises, with the track of its next step (the first depth
        /// and width steps are shared by both tracks, later ones belong to one).
        /// </summary>
        public static string Name(XgTechEntry e, XgSim sim)
        {
            if (!e.IsChain) return sim.NodeName(e.First);
            var at = Next(e, sim) ?? e.members[e.members.Count - 1];
            bool en = sim.English;
            string track = at.tree == "vision" ? (en ? "Vision " : "视觉 ") : at.tree == "sequence" ? (en ? "Sequence " : "序列 ") : "";
            switch (e.First.kind)
            {
                case XgNodeKind.Depth: return track + (en ? (track.Length > 0 ? "depth" : "Depth") : "层数");
                case XgNodeKind.Width: return track + (en ? (track.Length > 0 ? "width" : "Width") : "宽度");
                default: return en ? "Automation" : "自动化";
            }
        }
    }
}
