namespace LingGuangV05.XingGuang
{
    /// <summary>Read-only eligibility. Never normalizes a legacy ending into a release route.</summary>
    public static class XgEndingArchiveRules
    {
        public static bool BeforeChoice(XgState state)
        {
            var d = state?.endingPreparation;
            var r = state?.releaseOutcome;
            var ep = state?.epilogue;
            return state != null && state.version == 1 && !state.chapterComplete && string.IsNullOrEmpty(state.ending)
                && d != null && d.version == 1 && d.phase == "asked" && !string.IsNullOrEmpty(d.memoryReviewReceipt)
                && !string.IsNullOrEmpty(d.siReadReceipt) && !string.IsNullOrEmpty(d.finalPageReceipt)
                && string.IsNullOrEmpty(d.route) && string.IsNullOrEmpty(d.pinReceipt)
                && (r == null || string.IsNullOrEmpty(r.route) && string.IsNullOrEmpty(r.releaseReceiptId))
                && (ep == null || string.IsNullOrEmpty(ep.completionReceipt));
        }
        public static bool Completed(XgState state)
        {
            var ep = state?.epilogue;
            return state != null && state.version == 1 && state.chapterComplete && XgEpilogueFlow.CanEnterEpilogue(state)
                && ep != null && ep.schemaVersion == 1 && ep.phase == "completed" && !string.IsNullOrEmpty(ep.completionReceipt)
                && ep.letterId == XgEpilogueFlow.RecommendedLetter(state.releaseOutcome.route)
                && ep.letterVersion > 0 && ep.savedAt > 0 && ep.opened && ep.consentShown;
        }
        public static bool LegacyCompleted(XgState state) => state != null && state.version == 1 && state.chapterComplete
            && !XgEpilogueFlow.HasCommittedRelease(state)
            && (state.ending == "E1" || state.ending == "E2" || state.ending == "E3-1" || state.ending == "E3-2");
    }
}
