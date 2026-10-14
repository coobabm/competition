namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        public bool gradualSystems = true;
        public bool trainingIntroDelivered, treeIntroDelivered, paidAssessmentReached;
    }

    public sealed partial class XgSim
    {
        public static readonly System.Collections.Generic.Dictionary<string, int> FeatureUnlock = new System.Collections.Generic.Dictionary<string, int>
        {
            { "power", 1 }, { "vram", 3 }, { "shop", 2 }, { "data", 3 },
            { "crowd", 4 }, { "quality", 4 }, { "outsourcing", 4 },
            { "flywheel", 5 }, { "casino", 5 }, { "games", 3 }
        };
        public bool SystemUnlocked(string id) => !S.gradualSystems || !FeatureUnlock.TryGetValue(id, out int stage) || S.stage >= stage;
        public bool FeatureVisible(string feature)
        {
            bool migrated = S.migratedBeats.Count > 0;
            if (S.openingFocus && OpeningLocksFeature(feature)) return false;
            switch (feature)
            {
                case "teach": case "label": return true;
                // 老周 no longer introduces pages (design v1.1 §9: he only answers), so pages open with the progress itself.
                case "train": return TrainingUnlocked(XgTrack.Vision) || TrainingUnlocked(XgTrack.Sequence);
                case "tree": return S.treeIntroDelivered || migrated || S.paidAssessmentReached || S.best.Count > 0 || S.trainedParamsK > 0;
                case "contracts": return S.stage >= 4 && S.best.Exists(b => Grade(BestScore(b.dataset)) >= 1);
                case "repo": return S.best.Count > 0;
                case "board": return UseBoard;
                // The album opens with the first card.
                case "cards": return S.achievements.Count > 0;
                case "chat": return true;
                // 接线 (XgSim.Wiring.cs): from stage 2 or the first checkpoint.
                case "wiring": return WiringOpen;
                case "final": return S.stage >= 6;
                case "save-model": return S.epochs > 0;
                case "coop": return CollaborationEnabled;
                case "attention": return S.stage >= 5;
                case "lingguang-contact": return S.stage >= 3;
                case "project": return ProjectActive || EndingAvailable || S.chapterComplete;
                default: return false;
            }
        }
        /// <summary>The pages the first-minutes focus keeps shut (XgSim.Opening.cs); labelling, training and chat stay.</summary>
        static bool OpeningLocksFeature(string feature)
        {
            switch (feature)
            {
                case "tree": case "contracts": case "repo": case "board": case "cards": case "wiring": case "coop": case "attention": case "final": case "project": return true;
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
