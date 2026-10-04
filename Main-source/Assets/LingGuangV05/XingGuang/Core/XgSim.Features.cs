namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        public bool trainingIntroDelivered, treeIntroDelivered, paidAssessmentReached;
    }

    public sealed partial class XgSim
    {
        public bool FeatureVisible(string feature)
        {
            bool migrated = S.migratedBeats.Count > 0;
            switch (feature)
            {
                case "label": return true;
                // 老周 no longer introduces pages (design v1.1 §9: he only answers), so pages open with the progress itself.
                case "train": return TrainingUnlocked(XgTrack.Vision) || TrainingUnlocked(XgTrack.Sequence);
                case "tree": return S.treeIntroDelivered || migrated || S.paidAssessmentReached || S.best.Count > 0;
                case "contracts": return S.best.Count > 0;
                case "repo": return S.best.Count > 0;
                case "board": return UseBoard && S.epochs > 0;
                case "wall": return UseBoard && ActiveWall != null;
                case "chat": return true;
                case "final": return S.stage >= 6;
                case "save-model": return S.epochs > 0;
                case "coop": return CollaborationEnabled;
                case "attention": return S.stage >= 5;
                case "lingguang-contact": return S.stage >= 3;
                case "project": return ProjectActive || EndingAvailable || S.chapterComplete;
                default: return false;
            }
        }
        public void MarkIntroDelivered(string beat)
        {
            if (beat == "lz_trainable") S.trainingIntroDelivered = true;
            if (beat == "lz_tree") S.treeIntroDelivered = true;
        }
    }
}
