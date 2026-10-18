using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgTeachingNote
    {
        public long id, learningRecordId;
        public string correction = "";
        public string text = "", textEn = "", why = "", whyEn = "", dataset = "";
        public bool truth, playerAnswer;
        public int seed, digit;
        public string patternA = "", patternB = "", teachingImageKind = "", answerText = "", aiAnswer = "";
        public int teachingImageKey;
        public int[] teachingImageKeys;
        public string teachingPosition = "";
    }
    public sealed partial class XgState
    {
        public string teachingPack = "";
        public List<string> teachingPacksSeen = new List<string>();
        public List<XgTeachingNote> teachingNotebook = new List<XgTeachingNote>();
        public long teachingNoteSerial;
        public List<string> teachingChessIntroSolved = new List<string>();
    }
    public sealed partial class XgSim
    {
        public void RecordTeachingQuestion(string text, string textEn, bool truth, bool playerAnswer,
            string why, string whyEn, string dataset, XgCard card = null, string answerText = "", string aiAnswer = "", string correction = "")
        {
            if (S.teachingNotebook == null) S.teachingNotebook = new List<XgTeachingNote>();
            var n = new XgTeachingNote { id = NextLearningNoteId(), text = text, textEn = textEn,
                truth = truth, playerAnswer = playerAnswer, why = why, whyEn = whyEn, dataset = dataset, answerText = answerText, aiAnswer = aiAnswer, correction = correction };
            if (card != null) { n.seed = card.seed; n.digit = card.digit; n.patternA = card.patternA; n.patternB = card.patternB; n.teachingImageKind=card.teachingImageKind; n.teachingImageKey=card.teachingImageKey; n.teachingImageKeys=card.teachingImageKeys==null?null:(int[])card.teachingImageKeys.Clone(); n.teachingPosition=card.teachingPosition; }
            S.teachingNotebook.Add(n);
            RecordTeachingAsked(n);
        }
        public string TeachingPackBlocker(XgTeachingPack p, bool purchasing = false)
        {
            if (p == null) return T("没有这张题桌", "Unknown question desk");
            if (S.stage < p.stage) return T("第 " + p.stage + " 阶段开放", "Opens in stage " + p.stage);
            if(p.needs=="imagenet"&&!Owns("imagenet"))return T("先取得 ImageNet 数据包", "Obtain the ImageNet data pack first");
            if (p.needs == "vision" && S.firstSpecialty != "vision" && !Has("lenet") && !Has("conv"))
                return T("先选择看图线或购买卷积", "Choose the vision path or buy convolution first");
            if (p.needs == "memory" && !Has("rnn")) return T("先购买循环记忆", "Buy recurrent memory first");
            if (p.needs == "lstm" && !Has("lstm") && !Has("gru")) return T("先购买门控记忆", "Buy gated memory first");
            if (!purchasing && p.buy && !Has(p.NodeId)) return T("在科技的数据分支购买", "Buy this in Tech's Data branch");
            if (!purchasing && p.buy && !TeachingNodeInstalled(p.NodeId))
                return T("在资料文件夹下载后拖进练页安装", "Download it in your Files folder, then drag it into Train to install");
            return null;
        }
        public bool TeachingPackAvailable(string id) => TeachingPackBlocker(XgTeachingPacks.Find(id)) == null;
        public bool TeachingPackDeskOpen(string dataset)
        {
            foreach (var p in XgStageLessons.Desks) if (p.dataset == dataset && TeachingPackAvailable(p.id)) return true;
            return false;
        }
        public bool SelectTeachingPack(string id)
        {
            var p = XgTeachingPacks.Find(id);
            if (TeachingPackBlocker(p) != null) return false;
            S.teachingPack = id; S.desk = p.dataset;
            if (!S.desksOpen.Contains(p.dataset)) S.desksOpen.Add(p.dataset);
            if (S.teachingPacksSeen == null) S.teachingPacksSeen = new List<string>();
            if (!S.teachingPacksSeen.Contains(id)) S.teachingPacksSeen.Add(id);
            return true;
        }
        void ApplyTeachingPackPurchase(XgNode node)
        {
            if (!node.id.StartsWith("pack.")) return;
            var pack = XgTeachingPacks.Find(node.id.Substring(5)); if (pack == null) return;
            QueueTeachingData(pack.dataset, node.id);
            Say(T("开始下载题库：" + pack.name, "Downloading question desk: " + pack.nameEn));
        }
    }
}
