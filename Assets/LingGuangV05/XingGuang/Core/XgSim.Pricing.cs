using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// 定价重做（小丑牌式，2026-10-24，Design/定价重做_小丑牌式_20261024.md）. Every technology quote is one formula:
    /// the authored price at the old scale (<see cref="RawPrice"/>) is compressed and put on the stage's scale,
    /// <c>Nice(max(floor[s], scale[s] × raw^0.75))</c>, so the cheapest things of a stage cost about a quarter of a
    /// minute of a typical player's income, ordinary ones one to two minutes, and the stage's big item (the width that
    /// reaches the next ability) three to four. Prices are rounded to small round numbers (<see cref="NicePrice"/>), the
    /// old ×10 "at least ¥100" floor no longer touches the tree, and the depth and width ladders never step down.
    /// The 加薪 and 自动答题 ladders have their own curves (<see cref="XgCatalog.RaiseCost"/>, <see cref="XgCatalog.AutoCost"/>).
    /// Prices the owner chose by hand (daily life, board games, personality packs and archive) keep their numbers.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Compression of the authored prices: 0.75 turns a ×40 spread within a stage into about ×16.</summary>
        public const double PriceCurve = .75;
        /// <summary>Scale of each stage's quotes (index = node stage; 0 unused).</summary>
        public static readonly double[] StagePriceScale = { 1, .78, 1.4, 3, 3.1, 5, 6.5 };
        /// <summary>The cheapest quote of each stage (about a quarter of a minute of that stage's income).</summary>
        public static readonly double[] StagePriceFloor = { 1, 10, 30, 60, 120, 250, 500 };
        /// <summary>A deeper or wider model costs at least this much more than the one before it.</summary>
        public const double LadderStep = 1.25;

        /// <summary>Small round numbers: whole yuan under 20, then steps of 5, 10, 50 and 100.</summary>
        public static double NicePrice(double price)
        {
            if (!(price > 0) || double.IsInfinity(price)) return price;
            double step = price < 20 ? 1 : price < 100 ? 5 : price < 1000 ? 10 : price < 10000 ? 50 : 100;
            return Math.Max(1, Math.Round(price / step, MidpointRounding.AwayFromZero) * step);
        }

        /// <summary>The stage formula on an authored price at the old scale.</summary>
        public static double TechQuote(double raw, int stage)
        {
            if (!(raw > 0) || double.IsInfinity(raw)) return raw > 0 ? raw : 0;
            int s = Math.Max(1, Math.Min(StagePriceScale.Length - 1, stage));
            return NicePrice(Math.Max(StagePriceFloor[s], StagePriceScale[s] * Math.Pow(raw, PriceCurve)));
        }

        /// <summary>Prices the owner picked by hand (用户选择A): kept as they are.</summary>
        public static bool HandPricedNode(XgNode node)
            => node != null && (node.id == PersonaArchiveNode || node.id.StartsWith("persona.", StringComparison.Ordinal)
                || XgBoardLogic.IsNode(node.id) || node.id == XgBoardRecords.Node || XgDailyLife.Node(node.id) != null);

        /// <summary>The authored price at the old scale (before this retune's formula): the input of <see cref="TechQuote"/>.</summary>
        public static double RawPrice(XgNode node)
        {
            if (node == null) return double.PositiveInfinity;
            // Earlier "approved ordinary prices" were already final numbers; the rest is the compact story pacing (×0.25).
            bool final = node.id == LedgerNode || node.id == DataEntryNode || node.id == LibraryNode || IsRelicExpansion(node.id) || IsMoleArithmeticNode(node.id);
            double raw = final ? Math.Ceiling(node.cost) : Math.Ceiling(node.cost * StoryResearchPrice);
            return raw * (node.tree != "label" ? EarlyTechnologyPriceFactor(node) : 1);
        }

        /// <summary>
        /// The list price of a node before relic and diagnosis discounts (label-tree levels are priced by
        /// <see cref="LabelNodeCost"/>). Static: the data market's catalog shows the same numbers as the tree.
        /// </summary>
        public static double ListPrice(XgNode node)
        {
            if (node == null) return double.PositiveInfinity;
            if (node.id == PersonaArchiveNode) return PersonaArchivePrice;
            if (node.id.StartsWith("persona.", StringComparison.Ordinal)) return PersonaPrice(node.id.Substring(8));
            if (HandPricedNode(node) || node.tree == "label") return Math.Ceiling(node.cost);
            double price = TechQuote(RawPrice(node), node.stage);
            // Ladders never step down (s.d2 was authored dearer than the stage-two s.d3 it leads to).
            if ((node.kind == XgNodeKind.Depth || node.kind == XgNodeKind.Width) && !string.IsNullOrEmpty(node.parent))
            {
                var parent = XgCatalog.Node(node.parent);
                if (parent != null && parent.kind == node.kind)
                {
                    double before = ListPrice(parent), floor = NicePrice(before * LadderStep);
                    if (floor <= before) floor = before + 1;
                    price = Math.Max(price, floor);
                }
            }
            return price;
        }
    }
}
