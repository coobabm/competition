using System;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgEpilogueFlowTests
    {
        sealed class Fixture
        {
            public XgState state = new XgState();
            public object identity = new object();
            public bool current = true, saveSucceeds = true;
            public int saves;
            public string disk = "";
            public Action duringSave;
            public XgEpilogueFlow flow;
            public Fixture()
            {
                flow = new XgEpilogueFlow(state, identity, () => current, () =>
                {
                    saves++; duringSave?.Invoke();
                    if (!saveSucceeds) return false;
                    disk = JsonUtility.ToJson(state); return true;
                });
            }
            public XgEpilogueResult Commit(string route = "online") => flow.CommitRelease(flow.CaptureBinding(), Release(route));
            public void ToLetter()
            {
                Assert.IsTrue(Commit().Success);
                Assert.IsTrue(flow.RecordLaunchNightResult(flow.CaptureBinding(), Night()).Success);
                Assert.IsTrue(flow.EnterEpilogue(flow.CaptureBinding()).Success);
                for (int i = 0; i < 3; i++) Assert.IsTrue(flow.Advance(flow.CaptureBinding()).Success);
                Assert.IsTrue(flow.SaveArticle(flow.CaptureBinding(), "zhou_open_door", 1, "public:zhou_open_door").Success);
                Assert.IsTrue(flow.OpenArticle(flow.CaptureBinding(), "zhou_open_door").Success);
            }
        }
        [TestCase("online", "zhou_open_door")]
        [TestCase("public_model", "zhou_copies")]
        [TestCase("local_only", "zhou_unpressed_button")]
        public void CompletedNewRouteSurvivesSimulationConstructionWithoutInventingLegacyAnswer(string route, string letter)
        {
            var f = new Fixture();
            Assert.IsTrue(f.Commit(route).Success);
            if (route == "online") Assert.IsTrue(f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), Night()).Success);
            Assert.IsTrue(f.flow.EnterEpilogue(f.flow.CaptureBinding()).Success);
            for (int i = 0; i < 3; i++) Assert.IsTrue(f.flow.Advance(f.flow.CaptureBinding()).Success);
            Assert.IsTrue(f.flow.SaveArticle(f.flow.CaptureBinding(), letter, 1, "public:" + letter).Success);
            Assert.IsTrue(f.flow.OpenArticle(f.flow.CaptureBinding(), letter).Success);
            Assert.IsTrue(f.flow.Complete(f.flow.CaptureBinding(), letter, 1).Success);
            string receipt = f.state.epilogue.completionReceipt;
            var first = new XgSim(JsonUtility.FromJson<XgState>(f.disk));
            var second = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(first.S)));
            foreach (var sim in new[] { first, second })
            {
                Assert.IsTrue(sim.S.chapterComplete);
                Assert.AreEqual(receipt, sim.S.epilogue.completionReceipt);
                Assert.AreEqual("completed", sim.S.epilogue.phase);
                Assert.IsTrue(string.IsNullOrEmpty(sim.S.ending), "never fabricate a legacy ending choice");
                Assert.IsFalse(sim.S.endingReflectionAnswered, "new completion is not the old reflection event");
            }
        }

        static XgReleasePrerequisites Proof() => new XgReleasePrerequisites
        {
            memoryCardsReceiptId = "memory:actual", redTeamOneReceiptId = "red:1", redTeamTwoReceiptId = "red:2",
            redTeamThreeReceiptId = "red:3", fixedThreeSlotsReceiptId = "slots:actual"
        };
        static XgLaunchNightReceipt Night(string ending = "completed") => new XgLaunchNightReceipt
        {
            receiptId = "night:actual", sourceRef = "launch-run:actual", releaseReceiptId = "release:actual",
            artifactId = "artifact:actual", ending = ending, endedAt = 20170101
        };
        static ReleaseOutcome Release(string route) => new ReleaseOutcome
        {
            route = route, decisionOwner = "player", shutdownPin = "unpinned", committedAt = 20161231,
            releaseReceiptId = "release:actual", artifactId = route == "local_only" ? "" : "artifact:actual", prerequisites = Proof(),
            publicName = "A page", publicDescription = "Description", publicLimitations = "Limitations", publicTerms = "Terms"
        };

        [TestCase("online", "zhou_open_door")]
        [TestCase("public_model", "zhou_copies")]
        [TestCase("local_only", "zhou_unpressed_button")]
        public void RoutesCommitWithoutCompletingOrRewritingLegacy(string route, string letter)
        {
            var f = new Fixture(); f.state.ending = "E3-1";
            Assert.IsTrue(f.Commit(route).Success);
            Assert.AreEqual("E3-1", f.state.ending); Assert.IsFalse(f.state.chapterComplete);
            Assert.AreEqual(letter, f.state.epilogue.letterId); Assert.AreEqual(20170613, f.state.epilogue.displayDate);
            Assert.AreEqual(route == "online" ? "launch_night" : "date", f.state.epilogue.phase);
            Assert.IsTrue(XgEpilogueFlow.HasCommittedRelease(f.state));
            Assert.AreEqual(route != "online", XgEpilogueFlow.CanEnterEpilogue(f.state));
            Assert.AreEqual(0, f.state.releaseOutcome.publicEventIds.Count);
        }
        [Test]
        public void OnlineCanPublishWithoutNightButCannotEnterNewsSaveLetterOrComplete()
        {
            var f = new Fixture(); Assert.IsTrue(f.Commit().Success);
            int writes = f.saves; var token = f.flow.CaptureBinding();
            Assert.AreEqual("launch_night_incomplete", f.flow.EnterEpilogue(token).Code);
            Assert.AreEqual("launch_night_incomplete", f.flow.Advance(token).Code);
            Assert.AreEqual("launch_night_incomplete", f.flow.SaveArticle(token, "zhou_open_door", 1, "public:zhou_open_door").Code);
            Assert.AreEqual("launch_night_incomplete", f.flow.OpenArticle(token, "zhou_open_door").Code);
            Assert.AreEqual("launch_night_incomplete", f.flow.Complete(token, "zhou_open_door", 1).Code);
            Assert.IsFalse(f.state.chapterComplete); Assert.AreEqual(writes, f.saves);
            // A stale or malformed phase alone cannot grant readiness either.
            f.state.epilogue.phase = "letter";
            Assert.AreEqual("launch_night_incomplete", f.flow.SaveArticle(token, "zhou_open_door", 1, "public:zhou_open_door").Code);
        }
        [TestCase("completed")]
        [TestCase("power_cut")]
        public void RealNightTerminalReceiptIsSavedBeforeEnteringEpilogue(string ending)
        {
            var f = new Fixture(); Assert.IsTrue(f.Commit().Success);
            Assert.IsTrue(f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), Night(ending)).Success);
            Assert.AreEqual("launch_night", f.state.epilogue.phase);
            Assert.IsTrue(XgEpilogueFlow.CanEnterEpilogue(f.state));
            Assert.AreEqual("phase_not_advanceable", f.flow.Advance(f.flow.CaptureBinding()).Code);
            Assert.IsFalse(f.state.chapterComplete);
            var restored = JsonUtility.FromJson<XgState>(f.disk);
            Assert.IsTrue(XgEpilogueFlow.CanEnterEpilogue(restored));
            Assert.AreEqual(ending, restored.releaseOutcome.launchNight.ending);
            Assert.IsTrue(f.flow.EnterEpilogue(f.flow.CaptureBinding()).Changed);
            Assert.AreEqual("date", f.state.epilogue.phase);
            Assert.IsTrue(f.flow.Advance(f.flow.CaptureBinding()).Success); Assert.AreEqual("headline", f.state.epilogue.phase);
            int writes = f.saves;
            Assert.IsFalse(f.flow.EnterEpilogue(f.flow.CaptureBinding()).Changed);
            Assert.IsFalse(f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), Night(ending)).Changed);
            Assert.AreEqual(writes, f.saves);
        }
        [TestCase("public_model")]
        [TestCase("local_only")]
        public void NonOnlineRoutesNeedNoNightAndRejectFabricatedNight(string route)
        {
            var f = new Fixture(); Assert.IsTrue(f.Commit(route).Success);
            Assert.IsTrue(XgEpilogueFlow.CanEnterEpilogue(f.state)); Assert.AreEqual("date", f.state.epilogue.phase);
            Assert.AreEqual("launch_night_not_required", f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), Night()).Code);
            Assert.IsTrue(f.flow.Advance(f.flow.CaptureBinding()).Success); Assert.AreEqual("headline", f.state.epilogue.phase);
        }
        [Test]
        public void PublicationCannotSmuggleNightAndReceiptMustBelongToActualRelease()
        {
            var f = new Fixture(); var release = Release("online"); release.launchNight = Night();
            Assert.IsTrue(f.flow.CommitRelease(f.flow.CaptureBinding(), release).Success);
            Assert.IsNull(f.state.releaseOutcome.launchNight); Assert.IsFalse(XgEpilogueFlow.CanEnterEpilogue(f.state));
            var receipt = Night(); receipt.releaseReceiptId = "other-release";
            Assert.IsFalse(f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), receipt).Success);
            receipt = Night(); receipt.artifactId = "other-artifact";
            Assert.IsFalse(f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), receipt).Success);
            receipt = Night(); receipt.sourceRef = "";
            Assert.IsFalse(f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), receipt).Success);
            receipt = Night("still_running");
            Assert.IsFalse(f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), receipt).Success);
            receipt = Night(); receipt.endedAt = 20161230;
            Assert.IsFalse(f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), receipt).Success);
        }
        [Test]
        public void NightReceiptAndEntryFailuresRollBackIndependentlyForRetry()
        {
            var f = new Fixture(); Assert.IsTrue(f.Commit().Success);
            var token = f.flow.CaptureBinding(); f.saveSucceeds = false;
            Assert.AreEqual("save_failed", f.flow.RecordLaunchNightResult(token, Night()).Code);
            Assert.IsNull(f.state.releaseOutcome.launchNight); Assert.IsFalse(XgEpilogueFlow.CanEnterEpilogue(f.state));
            f.saveSucceeds = true; Assert.IsTrue(f.flow.RecordLaunchNightResult(token, Night()).Success);
            Assert.AreEqual("stale_binding", f.flow.EnterEpilogue(token).Code);
            token = f.flow.CaptureBinding(); f.saveSucceeds = false;
            Assert.AreEqual("save_failed", f.flow.EnterEpilogue(token).Code);
            Assert.AreEqual("launch_night", f.state.epilogue.phase); Assert.IsNotNull(f.state.releaseOutcome.launchNight);
            f.saveSucceeds = true; Assert.IsTrue(f.flow.EnterEpilogue(token).Success);
        }
        [Test]
        public void OldCompletionIsPreservedAndOldEndingDoesNotInferRoute()
        {
            var f = new Fixture(); f.state.ending = "E2"; f.state.chapterComplete = true;
            Assert.IsFalse(XgEpilogueFlow.HasCommittedRelease(f.state)); Assert.IsNull(f.state.epilogue);
            Assert.IsTrue(f.Commit("local_only").Success); Assert.IsTrue(f.state.chapterComplete);
        }
        [Test]
        public void LegacySixQuestionsCannotReplaceMissingNewPrerequisites()
        {
            var f = new Fixture(); f.state.examDone = 6; f.state.fullOpen = true; f.state.letterRead = true;
            var release = Release("online"); release.prerequisites.redTeamThreeReceiptId = "";
            Assert.AreEqual("missing_prerequisite_receipts", f.flow.CommitRelease(f.flow.CaptureBinding(), release).Code);
            Assert.AreEqual(0, f.saves); Assert.IsNull(f.state.releaseOutcome);
        }
        [Test]
        public void AiDecisionRequiresItsOwnMatchingRouteReceipt()
        {
            var f = new Fixture(); var r = Release("public_model"); r.decisionOwner = "ai";
            Assert.IsFalse(f.flow.CommitRelease(f.flow.CaptureBinding(), r).Success);
            r.prerequisites.aiRouteDecisionReceiptId = "ai:actual"; r.prerequisites.aiChosenRoute = "online";
            Assert.IsFalse(f.flow.CommitRelease(f.flow.CaptureBinding(), r).Success);
            r.prerequisites.aiChosenRoute = "public_model";
            Assert.IsTrue(f.flow.CommitRelease(f.flow.CaptureBinding(), r).Success);
        }
        [Test]
        public void FailedPublicationRollsBackAndCanRetrySameWindow()
        {
            var f = new Fixture(); var token = f.flow.CaptureBinding(); f.saveSucceeds = false;
            Assert.AreEqual("save_failed", f.flow.CommitRelease(token, Release("online")).Code);
            Assert.IsNull(f.state.releaseOutcome); Assert.IsNull(f.state.epilogue); Assert.IsTrue(f.flow.IsCurrent(token));
            f.saveSucceeds = true; Assert.IsTrue(f.flow.CommitRelease(token, Release("online")).Success);
        }
        [Test]
        public void ARouteDoesNotCreateUsageEventsAndEventsNeedTheirOwnSavedSource()
        {
            var f = new Fixture(); var r = Release("online"); r.publicEventIds.Add("online.paperplane_trial_posted");
            Assert.IsTrue(f.flow.CommitRelease(f.flow.CaptureBinding(), r).Success);
            Assert.IsEmpty(f.state.releaseOutcome.publicEventIds);
            var receipt = new XgPublicEventReceipt { id = "online.paperplane_trial_posted", artifactId = "artifact:actual", occurredAt = 20170613 };
            Assert.IsFalse(f.flow.RecordPublicEvent(f.flow.CaptureBinding(), receipt).Success);
            Assert.IsTrue(f.flow.RecordLaunchNightResult(f.flow.CaptureBinding(), Night()).Success);
            Assert.IsTrue(f.flow.EnterEpilogue(f.flow.CaptureBinding()).Success);
            Assert.IsTrue(f.flow.Advance(f.flow.CaptureBinding()).Success);
            Assert.IsTrue(f.flow.Advance(f.flow.CaptureBinding()).Success);
            receipt.sourceRef = "fictional-post:trial-1";
            int writes = f.saves;
            Assert.AreEqual("missing_saved_public_source", f.flow.RecordPublicEvent(f.flow.CaptureBinding(), receipt).Code);
            Assert.IsEmpty(f.state.releaseOutcome.publicEventIds); Assert.AreEqual(writes, f.saves);
        }
        [Test]
        public void LocalRouteNeverProducesPublicArtifactOrEvents()
        {
            var f = new Fixture(); var r = Release("local_only"); r.artifactId = "invalid";
            Assert.IsFalse(f.flow.CommitRelease(f.flow.CaptureBinding(), r).Success);
            Assert.IsTrue(f.Commit("local_only").Success);
            Assert.IsFalse(f.flow.RecordPublicEvent(f.flow.CaptureBinding(), new XgPublicEventReceipt()).Success);
        }
        [Test]
        public void StaleRevisionAndDifferentSaveIdentityRejectCallbacks()
        {
            var f = new Fixture(); var old = f.flow.CaptureBinding(); Assert.IsTrue(f.Commit().Success);
            Assert.AreEqual("stale_binding", f.flow.Advance(old).Code);
            var other = new Fixture(); Assert.AreEqual("stale_binding", other.flow.Advance(f.flow.CaptureBinding()).Code);
            f.current = false; Assert.AreEqual("stale_binding", f.flow.Advance(f.flow.CaptureBinding()).Code);
        }
        [Test]
        public void OpeningReadingAndReloadingNeverCompleteTheChapter()
        {
            var f = new Fixture(); f.ToLetter();
            Assert.IsTrue(f.flow.SaveReadingPosition(f.flow.CaptureBinding(), "zhou_open_door", "p03").Success);
            Assert.IsFalse(f.state.chapterComplete);
            var restored = JsonUtility.FromJson<XgState>(f.disk);
            Assert.AreEqual("reading", restored.epilogue.phase); Assert.AreEqual("p03", restored.epilogue.readParagraphId);
            Assert.IsTrue(restored.epilogue.consentShown); Assert.IsFalse(restored.chapterComplete);
            Assert.IsTrue(XgEpilogueFlow.HasCommittedRelease(restored));
        }
        [Test]
        public void CompletionCandidateIsPersistedTogetherAndFailureRestoresExactState()
        {
            var f = new Fixture(); f.ToLetter(); var ep = f.state.epilogue; var release = f.state.releaseOutcome;
            var token = f.flow.CaptureBinding(); string disk = f.disk; bool candidateObserved = false;
            f.duringSave = () => { candidateObserved = f.state.chapterComplete && f.state.epilogue.phase == "completed"; };
            f.saveSucceeds = false;
            Assert.IsFalse(f.flow.Complete(token, "zhou_open_door", 1).Success); Assert.IsTrue(candidateObserved);
            Assert.AreSame(ep, f.state.epilogue); Assert.AreSame(release, f.state.releaseOutcome);
            Assert.IsFalse(f.state.chapterComplete); Assert.AreEqual(disk, f.disk);
            f.saveSucceeds = true; Assert.IsTrue(f.flow.Complete(token, "zhou_open_door", 1).Success);
            var restored = JsonUtility.FromJson<XgState>(f.disk);
            Assert.IsTrue(restored.chapterComplete); Assert.IsNotEmpty(restored.epilogue.completionReceipt);
            Assert.AreEqual(1, restored.epilogue.letterVersion);
        }
        [Test]
        public void DuplicateCompletionHasNoSecondWriteAndReplayIsReadOnly()
        {
            var f = new Fixture(); f.ToLetter();
            Assert.IsTrue(f.flow.Complete(f.flow.CaptureBinding(), "zhou_open_door", 1).Changed); int writes = f.saves;
            Assert.IsFalse(f.flow.Complete(f.flow.CaptureBinding(), "zhou_open_door", 1).Changed);
            Assert.IsFalse(f.flow.SaveReadingPosition(f.flow.CaptureBinding(), "zhou_open_door", "p04").Success);
            Assert.AreEqual(writes, f.saves);
        }
        [Test]
        public void ReentrantSaveCannotCommitAgain()
        {
            var f = new Fixture(); f.ToLetter(); XgEpilogueResult nested = null;
            f.duringSave = () => nested = f.flow.Complete(f.flow.CaptureBinding(), "zhou_open_door", 1);
            Assert.IsTrue(f.flow.Complete(f.flow.CaptureBinding(), "zhou_open_door", 1).Success);
            Assert.IsFalse(nested.Changed);
        }
        [Test]
        public void ReboundDuringSaveNeverRequestsNewSaveTitleRouting()
        {
            var f = new Fixture(); f.ToLetter(); f.duringSave = () => f.current = false;
            Assert.AreEqual("saved_detached", f.flow.Complete(f.flow.CaptureBinding(), "zhou_open_door", 1).Code);
        }
    }
}
