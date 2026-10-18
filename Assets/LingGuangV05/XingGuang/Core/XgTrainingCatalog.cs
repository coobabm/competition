using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    // UI tools do not alter the simulated model's mathematics or old saved weights.
    public static class XgTrainingCatalog
    {
        public static void AddNodes(List<XgNode> nodes)
        {
            XgTrainingLessons.AddAtlas(nodes);
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



