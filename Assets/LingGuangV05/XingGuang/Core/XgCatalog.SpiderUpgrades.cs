using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public static partial class XgCatalog
    {
        static void AddSpiderNodes(List<XgNode> nodes)
        {
            SpiderNode(nodes, XgSim.SpiderAcquireNode, "", 1500, "灵宠蜘蛛", "Resident spider", "让它读读网页。\n▶ 开放网页采集", "Let it read the web.\n▶ Enables webpage reading");
            SpiderNode(nodes, XgSim.SpiderFastNode, XgSim.SpiderAcquireNode, 4500, "敏捷爬行", "Agile crawler", "脚步和阅读更快。\n▶ 爬行读取速度翻倍", "Move and read faster.\n▶ Doubles crawl and reading speed");
            SpiderNode(nodes, XgSim.SpiderBrowseNode, XgSim.SpiderFastNode, 12000, "自主浏览", "Autonomous browsing", "自己翻看新页面。\n▶ 自动翻页切页读桌面", "Explore the next page.\n▶ Auto-scroll, switch tabs and read the desktop");
            SpiderNode(nodes, XgSim.SpiderAutoAnswerNode, XgSim.SpiderAcquireNode, 24000, "快速自动答题", "Fast automatic answers", "更快伸手作答。\n▶ 每题约一秒", "Reach for answers sooner.\n▶ About one second per answer");
        }
        static void SpiderNode(List<XgNode> nodes, string id, string parent, double cost, string zh, string en, string note, string noteEn)
        {
            if (nodes.Exists(n => n.id == id)) return;
            nodes.Add(new XgNode { id=id, target=id, parent=parent, tree="research", lane="research", stage=3,
                cost=cost, kind=XgNodeKind.Research, name=zh, nameEn=en, note=note, noteEn=noteEn });
        }
    }
    public sealed partial class XgState
    {
        // False in old saves: an already-owned spider keeps its old reading/navigation abilities.
        public bool spiderPurchasedInitially;
    }
    public sealed partial class XgSim
    {
        public const string SpiderAcquireNode="spider.acquire", SpiderFastNode="spider.fast", SpiderBrowseNode="spider.browse", SpiderAutoAnswerNode="spider.autoanswer";
        public bool LegacySpiderAbilities => !S.spiderPurchasedInitially && S.spiderIntroDone;
        public bool SpiderReadingUnlocked => Has(SpiderAcquireNode) || LegacySpiderAbilities;
        public bool SpiderAutomaticBrowsing => Has(SpiderBrowseNode) || LegacySpiderAbilities;
        public float SpiderTravelSpeed => Has(SpiderFastNode) ? 2f : 1f;
        public float SpiderReadingSpeed => Has(SpiderFastNode) ? 2f : 1f;
        public float SpiderAnswerPeriod => Has(SpiderAutoAnswerNode) ? 1f : 3f;
        void RepairSpiderUpgrades()
        {
            if (!LegacySpiderAbilities) return;
            // A pre-upgrade spider already read the desktop and followed pages; keep these owned.
            if (!S.unlocked.Contains(SpiderAcquireNode)) S.unlocked.Add(SpiderAcquireNode);
            if (!S.unlocked.Contains(SpiderBrowseNode)) S.unlocked.Add(SpiderBrowseNode);
        }
        public void BeginPurchasedSpiderIntro()
        {
            if (!LegacySpiderAbilities) S.spiderPurchasedInitially = true;
            S.spiderIntroDone = true;
        }
    }
}
