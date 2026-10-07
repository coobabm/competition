using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>The shelves of the 道具 page. Only 结构 and 技巧 have items today; the others wait for the fun items.</summary>
    public enum XgItemCategory { Structure, Technique, Power, Data, Earning, Story }

    /// <summary>One item on the 道具 page: the tech node it buys, its shelf and the year the idea is from.</summary>
    public sealed class XgItemDef
    {
        /// <summary>The tech node (<see cref="XgCatalog.Node"/>) bought through the usual purchase.</summary>
        public string id = "";
        public XgItemCategory category;
        /// <summary>The year of the paper or product (0 = none shown). Past the game's year it is "early".</summary>
        public int year;
    }

    /// <summary>
    /// The item list of the 道具 page (参数量与数据量主线 §6), data-driven: architectures (结构) and research techniques
    /// (技巧) come from the tech nodes, so prices, prerequisites and ownership stay the tech tree's. New shelves
    /// (电力, 数据, 挣钱, 剧情) add items to <see cref="Extra"/>; the page shows every shelf that has one.
    /// </summary>
    public static class XgItems
    {
        /// <summary>The game's year: an item from later is shown as an early discovery.</summary>
        public const int GameYear = 2016;

        public static readonly XgItemCategory[] Order =
        {
            XgItemCategory.Structure, XgItemCategory.Technique, XgItemCategory.Power, XgItemCategory.Data, XgItemCategory.Earning, XgItemCategory.Story,
        };

        static readonly string[] Names = { "结构", "技巧", "电力", "数据", "挣钱", "剧情" };
        static readonly string[] NamesEn = { "Structures", "Techniques", "Power", "Data", "Earning", "Story" };

        public static string CategoryName(XgItemCategory c, bool english) => english ? NamesEn[(int)c] : Names[(int)c];

        /// <summary>Years of the research techniques (the architectures carry their own in <see cref="XgArch.year"/>).</summary>
        static readonly Dictionary<string, int> TechniqueYears = new Dictionary<string, int>
        {
            { "weights", 1958 }, { "learnrule", 1958 }, { "step", 1958 }, { "bias", 1958 }, { "chainrule", 1970 },
            { "sigmoid", 1986 }, { "momentum", 1986 }, { "augment", 1998 }, { "relu", 2010 }, { "rmsprop", 2012 },
            { "gradclip", 2013 }, { "wordvec", 2013 }, { "dropout", 2014 }, { "adam", 2014 }, { "cudnn", 2014 },
            { "transfer", 2014 }, { "beamsearch", 2014 }, { "batchnorm", 2015 }, { "irnn", 2015 }, { "subword", 2016 },
            { "position", 2017 }, { "warmup", 2017 }, { "sft", 2022 }, { "cot", 2022 },
        };

        /// <summary>Items from systems outside the tech tree (the fun items being designed). Added once, at load.</summary>
        public static readonly List<XgItemDef> Extra = new List<XgItemDef>();

        static List<XgItemDef> fromNodes;

        /// <summary>Every item: the tech tree's architectures and techniques in tree order, then <see cref="Extra"/>.</summary>
        public static List<XgItemDef> All()
        {
            if (fromNodes == null)
            {
                fromNodes = new List<XgItemDef>();
                foreach (var n in XgCatalog.Nodes)
                {
                    if (n.kind == XgNodeKind.Arch)
                    {
                        var a = XgCatalog.Arch(n.target);
                        fromNodes.Add(new XgItemDef { id = n.id, category = XgItemCategory.Structure, year = a != null ? a.year : 0 });
                    }
                    else if (n.kind == XgNodeKind.Research)
                        fromNodes.Add(new XgItemDef { id = n.id, category = XgItemCategory.Technique, year = TechniqueYears.TryGetValue(n.target ?? n.id, out int y) ? y : 0 });
                }
            }
            var all = new List<XgItemDef>(fromNodes);
            all.AddRange(Extra);
            return all;
        }

        /// <summary>Whether a node is one of the items (the 科技 tree keeps the rest: widths, layers, packs, automation…).</summary>
        public static bool IsItem(string nodeId)
        {
            foreach (var d in All()) if (d.id == nodeId) return true;
            return false;
        }

        public static bool Early(XgItemDef d) => d != null && d.year > GameYear;
    }
}
