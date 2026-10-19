using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgEndingArchiveRulesTests
    {
        static XgState Before() => new XgState { endingPreparation = new XgEndingPreparationState
        { phase = "asked", memoryReviewReceipt = "memory", siReadReceipt = "si", finalPageReceipt = "page" } };
        [Test] public void OnlyActualUnchosenStateIsAValidCheckpoint()
        {
            var s = Before(); Assert.IsTrue(XgEndingArchiveRules.BeforeChoice(s));
            s.endingPreparation.route = "online"; Assert.IsFalse(XgEndingArchiveRules.BeforeChoice(s));
            s = Before(); s.endingPreparation.phase = "pinning"; Assert.IsFalse(XgEndingArchiveRules.BeforeChoice(s));
            s = Before(); s.chapterComplete = true; Assert.IsFalse(XgEndingArchiveRules.BeforeChoice(s));
            s = Before(); s.endingPreparation.memoryReviewReceipt = ""; Assert.IsFalse(XgEndingArchiveRules.BeforeChoice(s));
            s = Before(); s.releaseOutcome = new ReleaseOutcome { releaseReceiptId = "committed" }; Assert.IsFalse(XgEndingArchiveRules.BeforeChoice(s));
        }
        [TestCase("E1")][TestCase("E2")][TestCase("E3-1")][TestCase("E3-2")]
        public void LegacyIdsStayLegacyAndUnfinishedLegacyIsNotACompletion(string ending)
        {
            var s = Before(); s.ending = ending;
            Assert.IsFalse(XgEndingArchiveRules.BeforeChoice(s)); Assert.IsFalse(XgEndingArchiveRules.LegacyCompleted(s));
            s.chapterComplete = true;
            Assert.IsTrue(XgEndingArchiveRules.LegacyCompleted(s)); Assert.IsFalse(XgEndingArchiveRules.Completed(s)); Assert.AreEqual(ending, s.ending);
            Assert.IsNull(s.releaseOutcome);
        }
        [TestCase("online")][TestCase("public_model")][TestCase("local_only")]
        public void OnlyTheRealFlowCompletionCountsAndOnlyItsRouteCounts(string route)
        {
            var s = new XgState(); var identity = new object(); int writes = 0;
            var flow = new XgEpilogueFlow(s, identity, () => true, () => { writes++; return true; });
            var release = new ReleaseOutcome { route = route, decisionOwner = "player", committedAt = 20161231,
                releaseReceiptId = "release:fixture", artifactId = route == "local_only" ? "" : "artifact:fixture",
                publicName = "Fixture release", publicDescription = "Explicit test preview",
                publicLimitations = "Test-only limits", publicTerms = "Test-only terms",
                prerequisites = new XgReleasePrerequisites { memoryCardsReceiptId = "memory", redTeamOneReceiptId = "one",
                    redTeamTwoReceiptId = "two", redTeamThreeReceiptId = "three", fixedThreeSlotsReceiptId = "pins" } };
            var committed = flow.CommitRelease(flow.CaptureBinding(), release);
            Assert.IsTrue(committed.Success, committed.Code);
            if (route == "online") Assert.IsTrue(flow.RecordLaunchNightResult(flow.CaptureBinding(), new XgLaunchNightReceipt
                { receiptId = "night", sourceRef = "night-source", releaseReceiptId = release.releaseReceiptId, artifactId = release.artifactId,
                  ending = "completed", endedAt = 20170101 }).Success);
            Assert.IsTrue(flow.EnterEpilogue(flow.CaptureBinding()).Success);
            for (int i = 0; i < 3; i++) Assert.IsTrue(flow.Advance(flow.CaptureBinding()).Success);
            string letter = XgEpilogueFlow.RecommendedLetter(route);
            Assert.IsTrue(flow.SaveArticle(flow.CaptureBinding(), letter, 1, letter).Success);
            Assert.IsTrue(flow.OpenArticle(flow.CaptureBinding(), letter).Success);
            Assert.IsFalse(XgEndingArchiveRules.Completed(s), "opened is not completed");
            Assert.IsTrue(flow.Complete(flow.CaptureBinding(), letter, 1).Success);
            int beforeRead = writes;
            Assert.IsTrue(XgEndingArchiveRules.Completed(s)); Assert.IsFalse(XgEndingArchiveRules.LegacyCompleted(s));
            Assert.AreEqual(route, s.releaseOutcome.route); Assert.AreEqual(beforeRead, writes, "read projection performs no persistence");
            s.epilogue.completionReceipt = ""; Assert.IsFalse(XgEndingArchiveRules.Completed(s));
        }
        [Test] public void GenericChapterFlagAndAllPublicLettersNeverClaimNewCompletion()
        {
            var s = new XgState { chapterComplete = true, epilogue = new EpilogueState { phase = "completed" } };
            foreach (var id in EndingLetterCatalog.ArticleIds) s.epilogue.seenContentIds.Add(id);
            Assert.IsFalse(XgEndingArchiveRules.Completed(s)); Assert.IsFalse(XgEndingArchiveRules.LegacyCompleted(s));
        }
    }
}
