using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgAuthoredPublicEventsTests
    {
        sealed class Fixture
        {
            public XgState state = new XgState();
            public object identity = new object();
            public bool current = true, saveSucceeds = true, throwOnSave;
            public int saves;
            public string disk;
            public Action duringSave;
            public XgEpilogueFlow flow;
            public Fixture()
            {
                flow = new XgEpilogueFlow(state, identity, () => current, () =>
                {
                    saves++; duringSave?.Invoke();
                    if (throwOnSave) throw new InvalidOperationException("injected atomic save failure");
                    if (!saveSucceeds) return false;
                    disk = JsonUtility.ToJson(state); return true;
                });
            }
            public void Ready(string route = "online", string night = "completed")
            {
                Assert.IsTrue(flow.CommitRelease(flow.CaptureBinding(), Release(route)).Success);
                if (route == "online")
                {
                    Assert.IsTrue(flow.RecordLaunchNightResult(flow.CaptureBinding(), new XgLaunchNightReceipt
                    {
                        receiptId = "night:actual", sourceRef = "night-run:actual", releaseReceiptId = "release:actual",
                        artifactId = "artifact:actual", ending = night, endedAt = 20170101
                    }).Success);
                    Assert.IsTrue(flow.EnterEpilogue(flow.CaptureBinding()).Success);
                }
                Assert.IsTrue(flow.Advance(flow.CaptureBinding()).Success);
                Assert.IsTrue(flow.Advance(flow.CaptureBinding()).Success);
                Assert.AreEqual("route", state.epilogue.phase);
            }
            public XgEpilogueResult Publish() => flow.PublishAuthoredPublicEvents(flow.CaptureBinding(), Catalog());
        }
        static ReleaseOutcome Release(string route) => new ReleaseOutcome
        {
            route = route, decisionOwner = "player", shutdownPin = "unpinned", committedAt = 20161231,
            artifactId = route == "local_only" ? "" : "artifact:actual", releaseReceiptId = "release:actual",
            publicName = "Public name", publicDescription = "Public description", publicLimitations = "Limitations", publicTerms = "Terms",
            prerequisites = new XgReleasePrerequisites
            {
                memoryCardsReceiptId = "memory:actual", redTeamOneReceiptId = "red:1", redTeamTwoReceiptId = "red:2",
                redTeamThreeReceiptId = "red:3", fixedThreeSlotsReceiptId = "slots:actual"
            }
        };
        static XgPublicDiscourseCatalog Catalog()
        {
            var resource = Resources.Load<TextAsset>("LingGuangV05/Epilogue/ending_public_discourse");
            Assert.IsNotNull(resource); return XgPublicDiscourseCatalog.Load(resource.text);
        }

        [TestCase("online")]
        [TestCase("public_model")]
        public void RouteChoiceAndPublicationAloneNeverCreateTheScript(string route)
        {
            var f = new Fixture(); var release = Release(route);
            release.publicEventScriptVersion = 1;
            release.publicEventSources.Add(new XgAuthoredPublicSource { bodyEn = "injected source" });
            release.publicEventIds.Add("online.paperplane_trial_posted");
            Assert.IsTrue(f.flow.CommitRelease(f.flow.CaptureBinding(), release).Success);
            Assert.IsEmpty(f.state.releaseOutcome.publicEventSources);
            Assert.IsEmpty(f.state.releaseOutcome.publicEventIds);
            int writes = f.saves;
            Assert.IsFalse(f.Publish().Success); Assert.AreEqual(writes, f.saves);
        }
        [TestCase("online", 7)]
        [TestCase("public_model", 5)]
        public void SavedScriptContainsApprovedBodiesAndAllRequiredReceipts(string route, int count)
        {
            var f = new Fixture(); f.Ready(route);
            Assert.IsTrue(f.Publish().Success);
            var restored = JsonUtility.FromJson<XgState>(f.disk);
            var flow = new XgEpilogueFlow(restored, new object(), () => true, () => false);
            var ids = flow.SavedPublicEventIds(flow.CaptureBinding());
            Assert.AreEqual(count, ids.Length); Assert.AreEqual(5, restored.releaseOutcome.publicEventSources.Count);
            var catalog = Catalog();
            foreach (var row in catalog.All.Where(x => x.route == route))
            {
                var sources = flow.PublicSourcesForRecord(flow.CaptureBinding(), row.id);
                Assert.AreEqual(1, sources.Count);
                var source = sources[0];
                Assert.AreEqual("authored_fiction", source.sourceKind);
                Assert.AreEqual(row.bodyZh, source.bodyZh); Assert.AreEqual(row.bodyEn, source.bodyEn);
                Assert.AreEqual("artifact:actual", source.artifactId); Assert.AreEqual("release:actual", source.releaseReceiptId);
                Assert.That(source.eventIds, Is.EquivalentTo(row.requiredPublicEventIds));
                Assert.IsNotNull(flow.FindPublicSource(flow.CaptureBinding(), source.sourceRef));
                var context = new XgPublicDiscourseContext(6, 20170613, row.platform, new[] { row.platform }, route, source.artifactId, ids);
                Assert.IsTrue(catalog.Visible(row, context), row.id);
            }
            Assert.IsFalse(restored.chapterComplete, "creating fictional source posts is not chapter completion");
        }
        [TestCase(false)]
        [TestCase(true)]
        public void FailedOrThrowingAtomicSaveRestoresExactReferencesAndNoEvidence(bool throws)
        {
            var f = new Fixture(); f.Ready();
            var release = f.state.releaseOutcome; var ep = f.state.epilogue; string disk = f.disk;
            var token = f.flow.CaptureBinding(); f.saveSucceeds = false; f.throwOnSave = throws;
            Assert.AreEqual("save_failed", f.flow.PublishAuthoredPublicEvents(token, Catalog()).Code);
            Assert.AreSame(release, f.state.releaseOutcome); Assert.AreSame(ep, f.state.epilogue); Assert.AreEqual(disk, f.disk);
            Assert.IsEmpty(f.flow.SavedPublicEventIds(token)); Assert.IsEmpty(f.state.releaseOutcome.publicEventSources);
            f.saveSucceeds = true; f.throwOnSave = false;
            Assert.IsTrue(f.flow.PublishAuthoredPublicEvents(token, Catalog()).Success);
        }
        [Test]
        public void CandidateNeverLeaksThroughQueriesOrReentrantCreationBeforeSaveSuccess()
        {
            var f = new Fixture(); f.Ready(); XgEpilogueResult nested = null;
            f.duringSave = () =>
            {
                var token = f.flow.CaptureBinding();
                Assert.AreEqual(5, f.state.releaseOutcome.publicEventSources.Count, "the atomic snapshot contains the complete source batch");
                Assert.IsEmpty(f.flow.SavedPublicEventIds(token));
                Assert.IsNull(f.flow.FindPublicSource(token, f.state.releaseOutcome.publicEventSources[0].sourceRef));
                nested = f.flow.PublishAuthoredPublicEvents(token, Catalog());
            };
            Assert.IsTrue(f.Publish().Success); Assert.AreEqual("save_in_progress", nested.Code);
            Assert.AreEqual(7, f.flow.SavedPublicEventIds(f.flow.CaptureBinding()).Length);
        }
        [Test]
        public void DuplicateOldRevisionAndForeignOwnerHaveNoSecondWrite()
        {
            var f = new Fixture(); f.Ready(); var old = f.flow.CaptureBinding(); Assert.IsTrue(f.Publish().Success); int writes = f.saves;
            Assert.AreEqual("stale_binding", f.flow.PublishAuthoredPublicEvents(old, Catalog()).Code);
            Assert.AreEqual("public_events_already_created", f.Publish().Code);
            var other = new Fixture(); other.Ready();
            Assert.AreEqual("stale_binding", other.flow.PublishAuthoredPublicEvents(f.flow.CaptureBinding(), Catalog()).Code);
            f.current = false; Assert.AreEqual("stale_binding", f.Publish().Code);
            Assert.IsEmpty(f.flow.SavedPublicEventIds(f.flow.CaptureBinding())); Assert.AreEqual(writes, f.saves);
        }
        [Test]
        public void RebindingDuringSuccessfulSaveLeavesOnlyTheOldSnapshotAndNoNewOwnerProjection()
        {
            var f = new Fixture(); f.Ready(); f.duringSave = () => f.current = false;
            Assert.AreEqual("saved_detached", f.Publish().Code);
            Assert.IsEmpty(f.flow.SavedPublicEventIds(f.flow.CaptureBinding()));
            Assert.AreEqual(5, JsonUtility.FromJson<XgState>(f.disk).releaseOutcome.publicEventSources.Count);
        }
        [Test]
        public void LocalAndPowerCutCannotCreatePublicUsageScenes()
        {
            var local = new Fixture(); local.Ready("local_only"); int writes = local.saves;
            Assert.AreEqual("no_public_artifact", local.Publish().Code); Assert.AreEqual(writes, local.saves);
            Assert.IsEmpty(local.state.releaseOutcome.publicEventSources);
            var stopped = new Fixture(); stopped.Ready("online", "power_cut"); writes = stopped.saves;
            Assert.AreEqual("online_service_stopped", stopped.Publish().Code); Assert.AreEqual(writes, stopped.saves);
            Assert.IsEmpty(stopped.flow.SavedPublicEventIds(stopped.flow.CaptureBinding()));
            var context = new XgPublicDiscourseContext(6, 20170613, "news", new[] { "news" }, "online", "artifact:actual");
            var catalog = Catalog(); Assert.IsNotNull(catalog.Get("H-01", context)); Assert.IsNull(catalog.Get("R1-N", context));
        }
        [Test]
        public void ArbitraryEventIdsSourcesAndEditedApprovedTextAreRejected()
        {
            var f = new Fixture(); f.Ready(); int writes = f.saves;
            Assert.IsFalse(XgAuthoredPublicEvents.AllowsEvent("online.invented"));
            Assert.IsFalse(f.flow.RecordPublicEvent(f.flow.CaptureBinding(), new XgPublicEventReceipt
            { id = "online.paperplane_trial_posted", artifactId = "artifact:actual", occurredAt = 20170613, sourceRef = "https://example.invalid/fake" }).Success);
            var catalog = Catalog(); catalog.All.Single(x => x.id == "R1-F").bodyEn += " unapproved text";
            Assert.AreEqual("invalid_public_script", f.flow.PublishAuthoredPublicEvents(f.flow.CaptureBinding(), catalog).Code);
            Assert.AreEqual(writes, f.saves); Assert.IsEmpty(f.state.releaseOutcome.publicEventSources);
        }
        [Test]
        public void LocalSourceResolverRejectsForeignLinksAndReturnsDefensiveCopies()
        {
            var f = new Fixture(); f.Ready(); Assert.IsTrue(f.Publish().Success); var token = f.flow.CaptureBinding();
            var original = f.state.releaseOutcome.publicEventSources[0];
            var copy = f.flow.FindPublicSource(token, original.sourceRef);
            copy.bodyEn = "changed copy"; copy.eventIds[0] = "online.invented";
            Assert.AreNotEqual(copy.bodyEn, original.bodyEn); Assert.AreNotEqual(copy.eventIds[0], original.eventIds[0]);
            foreach (var link in new[] { "https://example.invalid", "file:///secret", "javascript:alert(1)", "xg-public://epilogue/other/R1-F/v1", original.sourceRef + "?extra=1" })
                Assert.IsNull(f.flow.FindPublicSource(token, link));
            var cloned = f.state.releaseOutcome.Copy(); cloned.publicEventSources[0].eventIds[0] = "other";
            Assert.AreNotEqual("other", original.eventIds[0]);
        }
        [Test]
        public void CorruptedSourceBodyCannotSupplyTheEventIdDespiteStoredReceipt()
        {
            var f = new Fixture(); f.Ready(); Assert.IsTrue(f.Publish().Success);
            var source = f.state.releaseOutcome.publicEventSources.Single(x => x.approvedTextId == "R1-F");
            source.bodyEn += " changed";
            Assert.IsNull(f.flow.FindPublicSource(f.flow.CaptureBinding(), source.sourceRef));
            Assert.That(f.flow.SavedPublicEventIds(f.flow.CaptureBinding()), Does.Not.Contain("online.paperplane_trial_posted").And.Not.Contain("online.changelog_published"));
        }
        [Test]
        public void CompletedReplayReadsSavedSourcesWithoutReplayingTheScript()
        {
            var f = new Fixture(); f.Ready(); Assert.IsTrue(f.Publish().Success);
            Assert.IsTrue(f.flow.Advance(f.flow.CaptureBinding()).Success);
            Assert.IsTrue(f.flow.SaveArticle(f.flow.CaptureBinding(), "zhou_open_door", 1, "public:zhou_open_door").Success);
            Assert.IsTrue(f.flow.OpenArticle(f.flow.CaptureBinding(), "zhou_open_door").Success);
            Assert.IsTrue(f.flow.Complete(f.flow.CaptureBinding(), "zhou_open_door", 1).Success);
            int writes = f.saves; Assert.AreEqual("read_only_completed", f.Publish().Code); Assert.AreEqual(writes, f.saves);
            Assert.AreEqual(7, f.flow.SavedPublicEventIds(f.flow.CaptureBinding()).Length);
        }
        [Test]
        public void HistoricalDateNeverBackdatesAUseEventBeforeItsRelease()
        {
            var f = new Fixture(); f.Ready("public_model"); f.state.releaseOutcome.committedAt = 20180101; f.state.epilogue.historicalReview = true;
            int writes = f.saves; Assert.AreEqual("historical_review_no_public_events", f.Publish().Code); Assert.AreEqual(writes, f.saves);
        }
    }
}
