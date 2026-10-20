using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public static partial class XgCatalog
    {
        static void AddDataReviewNode(List<XgNode> nodes)
        {
            nodes.Add(new XgNode { id=XgSim.DataReviewNode, parent="brain.window", tree="research", lane="persona",
                stage=2, cost=150, kind=XgNodeKind.Research, name="数据回顾", nameEn="Learning review",
                note="翻看一路学过的。\n▶ 开放 YY 数据侧页", noteEn="Revisit its learning.\n▶ Opens YY's Data panel" });
        }
    }
}
