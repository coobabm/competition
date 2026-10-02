using System.Collections.Generic;

namespace Emergence
{
    public static class Catalog
    {
        static readonly Dictionary<string, ContentDefinition> entries = new Dictionary<string, ContentDefinition>();
        // Per-round tuning data; intentionally independent of the propagation rules.
        // Calibrated against deterministic diagnostic builds, not human playtests.
        public static readonly int[] RoundTargets = { 300, 600, 900, 1700, 2600, 4000, 6000, 9000, 14000, 21000, 30000, 38000, 48000, 60000, 85000, 105000, 120000, 200000 };
        public static readonly string[] StructureIds = { "S01", "S02", "S03", "S04", "S05", "S06", "S07", "S08", "S09" };
        public static readonly string[] StructureNames = { "火花", "短链", "齐放", "汇流", "长链", "回荡", "满潮", "巨链", "共鸣" };
        public static readonly int[] StructureCharge = { 5, 10, 20, 25, 30, 30, 50, 60, 60 };
        public static readonly int[] StructureMult = { 1, 2, 3, 3, 4, 4, 6, 8, 8 };
        public static readonly int[] StructureChargeGrowth = { 5, 10, 15, 15, 20, 20, 30, 40, 40 };
        public static readonly int[] StructureMultGrowth = { 1, 1, 1, 1, 2, 2, 3, 3, 3 };
        public static readonly string[] SynapseIds = { "J01", "J02", "J03", "J04", "J05", "J06", "J07", "J08", "J09", "J10", "J11", "J12", "J13", "J15", "J16" };
        public static readonly string[] MemoryIds = { "M01", "M02", "M03", "M04", "M05", "M06", "M07", "M08", "M11" };
        public static readonly string[] DrugIds = { "P01", "P02", "P03", "P04" };

        static Catalog()
        {
            Add("J01", "磷火", "敏感节点每次计分，倍率 +4。回声会再次触发。", ContentCategory.Synapse, 5);
            Add("J02", "石砌", "普通节点每次计分，灵感 +15。", ContentCategory.Synapse, 5);
            Add("J03", "节律", "同一拍至少 2 个节点放电：每个节点使倍率 +1。", ContentCategory.Synapse, 5);
            Add("J04", "余烬", "刺激结束时，每个保留 1 点电位的节点使灵感 +10。", ContentCategory.Synapse, 5);
            Add("J05", "起爆", "起点首次放电时，每条有效出边使灵感 +8。", ContentCategory.Synapse, 5);
            Add("J06", "回路", "每次回声重计分，灵感额外 +20。", ContentCategory.Synapse, 5);
            Add("J07", "老化", "刺激结束时，倍率增加已完成轮数的一半（向下取整）。", ContentCategory.Synapse, 5);
            Add("J08", "收藏", "刺激结束时，每份已植入记忆使倍率 +1。", ContentCategory.Synapse, 5);
            Add("J09", "深渊", "最长触发层数达到 4，最终倍率 ×2。", ContentCategory.Synapse, 8, true);
            Add("J10", "聚焦", "核心每次被动计分，最终倍率 ×1.5。回声再次生效。", ContentCategory.Synapse, 8, true);
            Add("J11", "双子", "第二次刺激，最终倍率 ×2。", ContentCategory.Synapse, 8, true);
            Add("J12", "复读", "起点周围相邻节点首次放电后，立即重计分一次。不会递归。", ContentCategory.Synapse, 8, true);
            Add("J13", "镜面", "复制左侧相邻突触本次刺激累积的乘法效果。", ContentCategory.Synapse, 8, true);
            Add("J15", "导线", "绑定两个节点，建立双向连接。可穿过隔膜，出售后消失。", ContentCategory.Synapse, 5, false, 2);
            Add("J16", "透镜", "绑定两个相距 2–3 格的节点，建立一条远程单向连接。", ContentCategory.Synapse, 5, false, 2);
            Add("M01", "初次相遇", "永久连接两个不相邻的节点，建立双向桥。", ContentCategory.Memory, 3, false, 2);
            Add("M02", "选择性遗忘", "永久封锁两个节点之间的双向连接。", ContentCategory.Memory, 3, false, 2);
            Add("M03", "刻骨铭心", "赋予镀金：此节点每次计分额外获得 20 灵感。", ContentCategory.Memory, 3, false, 1);
            Add("M04", "心有灵犀", "赋予共振：此节点每次计分使倍率 +4。", ContentCategory.Memory, 3, false, 1);
            Add("M05", "似曾相识", "为一条有效有向连接开启回声。2 拍后再次抵达，休眠节点重计分。", ContentCategory.Memory, 3, false, 2);
            Add("M06", "重新理解", "将普通节点变为敏感，或将敏感变为普通。", ContentCategory.Memory, 3, false, 1);
            Add("M07", "漫长等待", "指定有向连接的传输延迟 +1 拍，上限 3 拍。", ContentCategory.Memory, 3, false, 2);
            Add("M08", "觉醒", "将普通节点变为核心。棋盘上最多存在 2 个核心。", ContentCategory.Memory, 3, false, 1);
            Add("M11", "多重", "赋予多重：此节点每次计分使最终倍率 ×1.5。", ContentCategory.Memory, 6, true, 1);
            Add("P01", "导通液", "指定节点全部出边强度 +1。仅持续下次刺激。", ContentCategory.Drug, 2, false, 1);
            Add("P02", "回响液", "指定节点全部出边开启回声。仅持续下次刺激。", ContentCategory.Drug, 2, false, 1);
            Add("P03", "桥接液", "指定节点连接半径 2 内所有未放电节点。仅持续下次刺激。", ContentCategory.Drug, 2, false, 1);
            Add("P04", "兴奋剂", "指定节点每次计分使倍率额外 +10。仅持续下次刺激。", ContentCategory.Drug, 2, false, 1);
            for (int i = 0; i < StructureIds.Length; i++)
                Add(StructureIds[i], StructureNames[i] + "星图", "永久提升「" + StructureNames[i] + "」一级：灵感 +" + StructureChargeGrowth[i] + "，倍率 +" + StructureMultGrowth[i] + "。系统自动选择得分最高的结构。", ContentCategory.Star, 3);
        }
        static void Add(string id, string name, string description, ContentCategory category, int price, bool rare = false, int targetCount = 0)
        {
            entries.Add(id, new ContentDefinition { id = id, name = name, description = description, category = category, price = price, rare = rare, targetCount = targetCount });
        }
        public static ContentDefinition Get(string id)
        {
            ContentDefinition value;
            return id != null && entries.TryGetValue(id, out value) ? value : new ContentDefinition { id = id ?? "", name = "未知物品", description = "无法识别该物品。" };
        }
        public static string NodeName(NodeKind kind)
        {
            switch (kind) { case NodeKind.Sensitive: return "敏感"; case NodeKind.Capacitor: return "蓄能"; case NodeKind.Delay: return "延迟"; case NodeKind.Projector: return "远投"; case NodeKind.Core: return "核心"; default: return "普通"; }
        }
        public static string NodeDescription(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Sensitive: return "阈值 1 · 灵感 7\n一份信号即可点亮，适合长链接力。";
                case NodeKind.Capacitor: return "阈值 3 · 灵感 15\n被动放电输出 2，适合汇集后强力扩散。";
                case NodeKind.Delay: return "阈值 2 · 灵感 10\n被动触发后等待一拍，重新安排汇流时序。";
                case NodeKind.Projector: return "阈值 2 · 灵感 5\n自然出边覆盖半径 2，跨越空白继续传播。";
                case NodeKind.Core: return "阈值 2 · 灵感 20\n被动触发时至少有 3 个来源，每个来源额外 +10 灵感。";
                default: return "阈值 2 · 灵感 10\n汇合两份信号后放电，是网络的稳定基础。";
            }
        }
        public static string VariantName(NodeVariant variant)
        {
            switch (variant) { case NodeVariant.Gold: return "镀金"; case NodeVariant.Resonant: return "共振"; case NodeVariant.Multiple: return "多重"; default: return "原生"; }
        }
        public static string VariantDescription(NodeVariant variant)
        {
            switch (variant) { case NodeVariant.Gold: return "每次计分：灵感 +20"; case NodeVariant.Resonant: return "每次计分：倍率 +4"; case NodeVariant.Multiple: return "每次计分：最终倍率 ×1.5"; default: return "无额外变体效果"; }
        }
    }
}
