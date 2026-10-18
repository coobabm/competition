using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    // UI tools do not alter the simulated model's mathematics or old saved weights.
    public static class XgTrainingCatalog
    {
        public static void AddNodes(List<XgNode> nodes)
        {
            XgTrainingLessons.AddAtlas(nodes);
            // Arithmetic game upgrades use their approved ordinary prices.
            nodes.Add(new XgNode { id=XgSim.MolePowersNode, tree="research", lane="research", target=XgSim.MolePowersNode,
                stage=2, cost=800, kind=XgNodeKind.Research, name="算术：幂与根式", nameEn="Arithmetic: powers and roots",
                note="再多一种算法。\n▶ 加入幂与根式题", noteEn="One more way to calculate.\n▶ Adds powers and roots" });
            nodes.Add(new XgNode { id=XgSim.MoleCalculusNode, parent=XgSim.MolePowersNode, tree="research", lane="research", target=XgSim.MoleCalculusNode,
                stage=3, cost=3000, kind=XgNodeKind.Research, name="算术：微积分", nameEn="Arithmetic: calculus",
                note="看看变化的规律。\n▶ 加入极限、导数、积分", noteEn="Explore how things change.\n▶ Adds limits, derivatives and integrals" });
            nodes.Add(new XgNode { id=XgSim.MoleAutoNode, tree="research", lane="research", target=XgSim.MoleAutoNode,
                stage=2, cost=1500, kind=XgNodeKind.Research, name="算术自动作答", nameEn="Arithmetic auto-answer",
                note="让它试着敲一下。\n▶ 开放自动打地鼠", noteEn="Let it try a whack.\n▶ Enables automatic whack-a-mole" });
            nodes.Add(new XgNode { id=XgSim.LibraryNode, tree="research", lane="research", target=XgSim.LibraryNode,
                stage=2, cost=500, kind=XgNodeKind.Research, name="图书馆网页", nameEn="Library website",
                note="开放付费图书馆网页；书籍单独购买，已有书架继续可读。", noteEn="Opens the paid library website. Books are sold separately; existing shelf access remains." });
            // Approved ordinary prices: appended directly, outside XgCatalog.StagePrice.
            nodes.Add(new XgNode { id=XgSim.RelicExpansion8Node, tree="research", lane="research", target=XgSim.RelicExpansion8Node,
                stage=3, cost=12000, kind=XgNodeKind.Research, name="道具扩容 I", nameEn="Relic slots I",
                note="稀有道具装备格从4格扩到8格；已装备道具保持。可选升级，不是升阶前置。", noteEn="Expand equipped relic slots from 4 to 8; keep equipped items. Optional, never required to advance." });
            nodes.Add(new XgNode { id=XgSim.RelicExpansion12Node, parent=XgSim.RelicExpansion8Node, tree="research", lane="research", target=XgSim.RelicExpansion12Node,
                stage=4, cost=48000, kind=XgNodeKind.Research, name="道具扩容 II", nameEn="Relic slots II",
                note="稀有道具装备格从8格扩到12格；已装备道具保持。需先购买道具扩容I。", noteEn="Expand equipped relic slots from 8 to 12; keep equipped items. Requires Relic slots I." });
            Add(nodes,XgSim.DataEntryNode,"train.curve",50,"数据导入","Data Import","把资料装进大脑。\n▶ 开放资料夹、安装与网址","Load files into its brain.\n▶ Opens Files, drag import and URLs");
            Add(nodes,XgSim.LedgerNode,"train.curve",15,"记账本","Ledger","看清每笔钱的去向。\n▶ 打开资金与电力面板","Track where each payment goes.\n▶ Opens funds and power details");
            Add(nodes,"train.holdout","train.curve",60,"留出验证集","Holdout set","留些新题考它。\n▶ 显示新题成绩","Keep unseen questions.\n▶ Shows validation results");
            Add(nodes,"train.checkpoint","shared.lr",80,"检查点 Checkpoint","Checkpoint","存好最好的一版。\n▶ 恢复最佳成绩","Save its best version.\n▶ Restores the best checkpoint");
            Add(nodes,"train.monitor","train.checkpoint",120,"训练监控","Training monitor","先看看这轮风险。\n▶ 显示训练风险","Preview the next round.\n▶ Shows training risks");
            Add(nodes,"train.weights","train.curve",100,"权重可视化","Weight viewer","看看它在注意哪。\n▶ 打开看它学","See what it looks at.\n▶ Opens the learning viewer");
        }
        static void Add(List<XgNode> nodes,string id,string parent,double price,string zh,string en,string note,string noteEn)
        {
            if(nodes.Exists(n=>n.id==id)) return;
            nodes.Add(new XgNode{id=id,parent=parent,tree="research",lane="research",target=id,stage=1,cost=price,kind=XgNodeKind.Research,name=zh,nameEn=en,note=note,noteEn=noteEn});
        }
    }
    public sealed partial class XgState
    {
        public List<string> trainParts = new List<string>();
    }
    public sealed partial class XgSim
    {
        public bool TrainPart(string id)
        {
            if (id == "data") return CanInstallData;
            // Legacy saves keep information that was already visible before the new opening.
            if(!S.teachingFlow) return true;
            switch(id)
            {
                case "curve": case "sheet": case "button": return TeachingTrainOpen;
                case "holdout": return Has("train.holdout");
                case "rate": return Has("shared.lr");
                case "checkpoint": return Has("train.checkpoint");
                case "monitor": return Has("train.monitor");
                case "weights": return Has("train.weights");
                case "next": case "learning": return S.epochs>=3;
                case "ceiling": return S.trainParts!=null && S.trainParts.Contains("ceiling");
                case "model":
                    int structures=0; foreach(var archId in S.unlocked) { if(XgCatalog.Node(archId)?.kind==XgNodeKind.Arch)structures++; } return structures>1;
                case "gear":
                    foreach(var ownedId in S.unlocked) { var node=XgCatalog.Node(ownedId); if(node!=null && node.kind==XgNodeKind.Research && !ownedId.StartsWith("train.") && ownedId!="brain.window" && ownedId!="label.ai") return true; }
                    return false;
                case "books": return S.books!=null && S.books.Count>0;
                case "ledger": return LedgerOpen;
                case "tracks": return TrainingUnlocked(XgTrack.Vision) && TrainingUnlocked(XgTrack.Sequence);
                case "rate-auto": return Has("adam");
                default: return S.trainParts!=null && S.trainParts.Contains(id);
            }
        }
    }
}



