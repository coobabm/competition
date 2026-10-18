namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        // Enabled only by BeginOpening: existing saves retain every page they already unlocked.
        public bool teachingFlow;
        public bool teachIntroSaid, teachThirdSaid, teachTreeSaid, teachTrainSaid;
    }

    public sealed partial class XgSim
    {
        public int TeachingAnswers => S.handCorrect + S.handWrong;
        public bool TeachingTreeOpen => !S.teachingFlow || TeachingAnswers >= 12 || Has("train.curve");
        public bool TeachingTrainOpen => !S.teachingFlow || Has("train.curve");
        public bool TeachingAiReady => Has("label.ai");

        public string TeachingNodeBlocker(XgNode node)
        {
            if (node.id.StartsWith("pack."))
            {
                var blocked = TeachingPackBlocker(XgTeachingPacks.Find(node.id.Substring(5)), true);
                if (blocked != null) return blocked;
            }
            if (node.id == "train.curve" && !TeachingTreeOpen)
                return T("先做完 12 道题", "Answer twelve questions first");
            if (node.id == "label.ai" && (TeachingAnswers < 24 || S.epochs < 1))
                return T("做完 24 题，并训练一轮", "Answer 24 questions and finish one training round");
            if (S.teachingFlow && !Has("train.curve") && node.id != "train.curve")
                return T("先买训练曲线", "Buy Training curve first");
            return null;
        }
    }
}
