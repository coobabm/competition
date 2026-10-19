using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgVirtualReleaseManifestTests
    {
        static ReleaseOutcome Release(string route = "public_model") => new ReleaseOutcome
        {
            route = route, decisionOwner = "player", committedAt = 20161231,
            releaseReceiptId = "private-receipt-fixture", artifactId = route == "local_only" ? "" : "virtual-artifact:fixture",
            prerequisites = new XgReleasePrerequisites { memoryCardsReceiptId = "private-memory-fixture",
                redTeamOneReceiptId = "1", redTeamTwoReceiptId = "2", redTeamThreeReceiptId = "3", fixedThreeSlotsReceiptId = "slots" },
            publicName = "样例", publicDescription = "Description \"quoted\"\nline", publicCapabilities = "Yes / no",
            publicLimitations = "Not a diagnosis", publicTerms = "Credit the source"
        };
        [Test]
        public void PublicSnapshotUsesAllowlistedFieldsAndSurvivesSaveRoundtrip()
        {
            var state = new XgState(); string disk = "";
            var flow = new XgEpilogueFlow(state, new object(), () => true, () => { disk = JsonUtility.ToJson(state); return true; });
            Assert.IsTrue(flow.CommitRelease(flow.CaptureBinding(), Release()).Success);
            var restored = JsonUtility.FromJson<XgState>(disk).releaseOutcome;
            Assert.IsTrue(XgVirtualReleaseManifest.Valid(restored));
            Assert.AreEqual("748e5aefb159128f3cca9cfd1dd051108eb9c28282be7c3a043f30e786bbfdff", restored.publicManifestSha256, "Independent fixed UTF-8 canonical fixture digest");
            StringAssert.Contains("\"modelFileStatus\": \"not_exported\"", restored.publicManifestSnapshot);
            StringAssert.DoesNotContain("private-memory-fixture", restored.publicManifestSnapshot);
            StringAssert.DoesNotContain("private-receipt-fixture", restored.publicManifestSnapshot);
            var copy = restored.Copy(); copy.publicCapabilities = "Changed claim";
            Assert.IsFalse(XgVirtualReleaseManifest.Valid(copy));
            Assert.IsTrue(XgVirtualReleaseManifest.Valid(restored), "copy edits do not mutate the saved snapshot");
            copy = restored.Copy(); copy.publicManifestSnapshot += " ";
            Assert.IsFalse(XgVirtualReleaseManifest.Valid(copy));
            copy = restored.Copy(); copy.publicManifestSha256 = new string('0', 64);
            Assert.IsFalse(XgVirtualReleaseManifest.Valid(copy));
            copy = restored.Copy(); copy.publicManifestVersion = "future/2";
            Assert.IsFalse(XgVirtualReleaseManifest.Valid(copy));
        }
        [Test]
        public void FailedPublicationRestoresExactStateAndRetryCannotSmuggleAnIncomingSnapshot()
        {
            var oldRelease = new ReleaseOutcome(); var oldEpilogue = new EpilogueState();
            var state = new XgState { releaseOutcome = oldRelease, epilogue = oldEpilogue };
            bool succeed = false; int saves = 0;
            var flow = new XgEpilogueFlow(state, new object(), () => true, () =>
            { saves++; Assert.IsTrue(XgVirtualReleaseManifest.Valid(state.releaseOutcome)); return succeed; });
            var supplied = Release(); supplied.publicManifestSnapshot = "untrusted incoming snapshot";
            supplied.publicManifestSha256 = "not a checksum";
            var token = flow.CaptureBinding();
            Assert.IsFalse(flow.CommitRelease(token, supplied).Success);
            Assert.AreSame(oldRelease, state.releaseOutcome); Assert.AreSame(oldEpilogue, state.epilogue);
            Assert.IsFalse(XgVirtualReleaseManifest.Recorded(state.releaseOutcome));
            Assert.AreEqual("untrusted incoming snapshot", supplied.publicManifestSnapshot);
            succeed = true; Assert.IsTrue(flow.CommitRelease(token, supplied).Success);
            Assert.IsTrue(XgVirtualReleaseManifest.Valid(state.releaseOutcome)); Assert.AreEqual(2, saves);
            string snapshot = state.releaseOutcome.publicManifestSnapshot;
            Assert.IsTrue(flow.CommitRelease(flow.CaptureBinding(), supplied).Success);
            Assert.AreEqual(2, saves); Assert.AreEqual(snapshot, state.releaseOutcome.publicManifestSnapshot);
        }
        [Test]
        public void LegacyCompletedReleaseStaysReadableWithoutBackfilledManifest()
        {
            var state = new XgState { releaseOutcome = Release(), chapterComplete = true,
                epilogue = new EpilogueState { phase = "completed", completionReceipt = "old-completion" } };
            string before = JsonUtility.ToJson(state); int saves = 0;
            var flow = new XgEpilogueFlow(state, new object(), () => true, () => { saves++; return true; });
            Assert.IsTrue(XgEpilogueFlow.CanEnterEpilogue(state));
            Assert.IsFalse(XgVirtualReleaseManifest.Recorded(state.releaseOutcome.Copy()));
            Assert.IsFalse(XgVirtualReleaseManifest.Valid(state.releaseOutcome));
            Assert.IsTrue(flow.CommitRelease(flow.CaptureBinding(), Release()).Success);
            Assert.AreEqual(0, saves); Assert.AreEqual(before, JsonUtility.ToJson(state));
        }
        [TestCase("online")]
        [TestCase("local_only")]
        public void OtherRoutesDoNotAcquirePublicModelManifest(string route)
        {
            var state = new XgState(); var flow = new XgEpilogueFlow(state, new object(), () => true, () => true);
            var supplied = Release(route); supplied.publicManifestSnapshot = "forged"; supplied.publicManifestVersion = "forged";
            Assert.IsTrue(flow.CommitRelease(flow.CaptureBinding(), supplied).Success);
            Assert.IsFalse(XgVirtualReleaseManifest.Recorded(state.releaseOutcome));
        }
    }
}
