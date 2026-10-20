using System;

namespace LingGuangV05.XingGuang
{
    public static partial class XgTechTreeCopy
    {
        // Prices, publication years and AI names are not numerical effects.
        public static bool HasNumericEffect(XgNode node, XgSim sim)
        {
            if (node == null || sim.NodeMystery(node)) return false;
            string effect = All.TryGetValue(node.id, out var copy) ? copy.effect : Effect(node, sim);
            foreach (char ch in effect) if (ch >= '0' && ch <= '9') return true;
            return effect.Contains("减半") || effect.Contains("六档");
        }

        public static XgKnowledge.Entry KnowledgeFor(XgNode node, XgSim sim)
        {
            if (node == null || sim.NodeMystery(node)) return null;
            All.TryGetValue(node.id, out var copy);
            string name = copy != null ? copy.name : node.name;
            string english = copy != null ? copy.nameEn : node.nameEn;
            if (node.kind == XgNodeKind.Depth) name = "网络深度";
            if (node.kind == XgNodeKind.Width) name = "网络宽度";
            // Use exact authored terminology, not a fuzzy match which can open an unrelated entry.
            var existing = XgKnowledge.Find(name);
            if (existing != null) return existing;
            string alias = node.id == "shared.lr" ? "学习率" : node.id == "lrschedule" ? "学习率调度"
                : node.id == "backprop" ? "小批量" : node.id == "chainrule" ? "自动求导" : null;
            if (alias != null && XgKnowledge.Find(alias) is XgKnowledge.Entry found) return found;
            // Feature/game nodes have no scientific glossary article. Display their own existing
            // description in the same reader, explicitly as a game function, without inventing facts.
            string zh = copy != null ? copy.effect : node.note;
            string en = copy != null ? copy.effectEn : node.noteEn;
            return new XgKnowledge.Entry((name ?? node.id).Replace("{AI}", sim.AiDisplayName),
                (english ?? node.id).Replace("{AI}", sim.AiDisplayName), 2,
                "游戏功能说明\n\n" + (zh ?? "").Replace("{AI}", sim.AiDisplayName),
                "Game function\n\n" + (en ?? "").Replace("{AI}", sim.AiDisplayName), Array.Empty<string>());
        }
    }
}
