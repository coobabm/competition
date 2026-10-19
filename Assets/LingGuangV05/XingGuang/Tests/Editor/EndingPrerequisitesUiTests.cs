using System;
using LingGuangV05.XingGuang;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class EndingPrerequisitesUiTests
    {
        object owner, state;
        long revision;
        Action<EndingUiResult> pending;
        EndingUiRequest request;
        int calls;
        EndingUiSession session;
        EndingUiSnapshot Snapshot() => new EndingUiSnapshot { owner = owner, stateOwner = state, revision = revision };
        [SetUp] public void Setup()
        {
            owner = new object(); state = new object(); revision = 1; calls = 0; pending = null; request = null;
            session = new EndingUiSession();
            session.Bind(Snapshot(), new EndingUiCallbacks {
                IsCurrent = (o, s, r) => ReferenceEquals(o, owner) && ReferenceEquals(s, state) && r == revision,
                FilterText = (text, english) => text,
                FilterReviewedText = (text, english) => text,
                Execute = (r, done) => { calls++; request = r; pending = done; }
            });
            session.SetVisible(true);
        }
        [Test] public void SaveFailureKeepsSnapshotAndAllowsExplicitRetry()
        {
            Assert.That(session.Send(EndingUiAction.PinThree, ids: new[] { "one", "two", "shutdown" }), Is.True);
            var before = session.Snapshot;
            pending(new EndingUiResult());
            Assert.That(session.Snapshot, Is.SameAs(before));
            Assert.That(session.Failed, Is.True); Assert.That(session.Busy, Is.False);
            Assert.That(session.Send(EndingUiAction.PinThree, ids: new[] { "one", "two", "shutdown" }), Is.True);
            Assert.That(calls, Is.EqualTo(2));
        }
        [Test] public void SuccessfulReceiptUsesNewRevisionAndDuplicateCallbackIsIgnored()
        {
            session.Send(EndingUiAction.ChooseRoute, "online");
            var callback = pending;
            revision++;
            var accepted = Snapshot(); accepted.route = "online"; accepted.routeLocked = true;
            callback(new EndingUiResult { success = true, snapshot = accepted });
            Assert.That(session.Snapshot.revision, Is.EqualTo(2));
            Assert.That(session.Snapshot.route, Is.EqualTo("online"));
            accepted.route = "local_only";
            callback(new EndingUiResult { success = true, snapshot = accepted });
            Assert.That(session.Snapshot.route, Is.EqualTo("online"));
        }
        [Test] public void RebindRejectsCallbackForOldSave()
        {
            session.Send(EndingUiAction.CommitRelease); var callback = pending; var old = Snapshot();
            owner = new object(); state = new object(); revision = 8;
            session.Bind(Snapshot(), session.Callbacks);
            callback(new EndingUiResult { success = true, snapshot = old });
            Assert.That(session.Snapshot.owner, Is.SameAs(owner)); Assert.That(session.Snapshot.revision, Is.EqualTo(8));
        }
        [Test] public void HiddenAndReadOnlyWindowsCannotIssueCommands()
        {
            session.Send(EndingUiAction.StartNight); var callback = pending; var before = session.Snapshot;
            session.SetVisible(false); revision++;
            callback(new EndingUiResult { success = true, snapshot = Snapshot() });
            Assert.That(session.Snapshot, Is.SameAs(before));
            Assert.That(session.Send(EndingUiAction.AdvanceNight, seconds: .5), Is.False);
            var readOnly = Snapshot(); readOnly.readOnly = true; session.Bind(readOnly, session.Callbacks); session.SetVisible(true);
            Assert.That(session.Send(EndingUiAction.StartNight), Is.False);
        }
        [Test] public void AdvanceIsBoundedAndOnlyOneRequestCanBePending()
        {
            Assert.That(session.Send(EndingUiAction.AdvanceNight, seconds: 1.01), Is.False);
            Assert.That(session.Send(EndingUiAction.AdvanceNight, seconds: double.NaN), Is.False);
            Assert.That(session.Send(EndingUiAction.AdvanceNight, seconds: .5), Is.True);
            Assert.That(request.seconds, Is.EqualTo(.5));
            Assert.That(session.Send(EndingUiAction.PullPower), Is.False);
            Assert.That(calls, Is.EqualTo(1));
        }
        [Test] public void MissingContentGuardFailsClosedAndSourceKeepsBinding()
        {
            int opened = 0; session.Callbacks.OpenSource = (o, s, r, id) => {
                Assert.That(o, Is.SameAs(owner)); Assert.That(s, Is.SameAs(state)); Assert.That(r, Is.EqualTo(revision));
                Assert.That(id, Is.EqualTo("record:7")); opened++;
            };
            session.Source("record:7"); Assert.That(opened, Is.EqualTo(1));
            session.Callbacks.FilterText = null;
            Assert.That(session.Send(EndingUiAction.ChooseRoute, "online"), Is.False);
            session.Callbacks.FilterText = (value, english) => value; session.Callbacks.FilterReviewedText = null;
            Assert.That(session.Send(EndingUiAction.ChooseRoute, "online"), Is.False);
        }
        [Test] public void SnapshotAndCommandListsAreCopied()
        {
            var snapshot = Snapshot(); snapshot.pinnedIds.Add("original"); session.Bind(snapshot, session.Callbacks);
            snapshot.pinnedIds.Clear(); Assert.That(session.Snapshot.pinnedIds.Count, Is.EqualTo(1));
            var ids = new[] { "one", "two", "three" }; session.Send(EndingUiAction.PinThree, ids: ids); ids[0] = "changed";
            Assert.That(request.ids[0], Is.EqualTo("one"));
        }
        [Test] public void MissingOptionalSnapshotFieldsUseEmptyDefaults()
        {
            var snapshot = Snapshot(); snapshot.preview = null; snapshot.night = null; snapshot.memories = null;
            snapshot.aiQuotes = null; snapshot.pinnedIds = null; snapshot.blocker = null;
            session.Bind(snapshot, session.Callbacks);
            Assert.That(session.Snapshot.preview, Is.Not.Null); Assert.That(session.Snapshot.night, Is.Not.Null);
            Assert.That(session.Snapshot.memories.Count, Is.EqualTo(0)); Assert.That(session.Snapshot.blocker.zh, Is.EqualTo(""));
        }
        [Test] public void TrialSubmissionKeepsExactAttemptAndCopiesPicksWithoutScoring()
        {
            var picks = new[] { 0, 1, 1, 0 };
            session.Send(EndingUiAction.SubmitTrial, question: 3, attemptId: "trial:actual", picks: picks);
            picks[1] = 0;
            Assert.That(request.question, Is.EqualTo(3)); Assert.That(request.attemptId, Is.EqualTo("trial:actual"));
            Assert.That(request.picks, Is.EqualTo(new[] { 0, 1, 1, 0 }));
        }
        [Test] public void TrialDisplayCopiesItemsAndDoesNotContainAnAnswerKey()
        {
            var snapshot = Snapshot();
            var item = new EndingTrialItemView { prompt = new EndingUiText { zh = "原题", en = "Original" } };
            item.choices.Add(new EndingUiText { zh = "原选项", en = "Original choice" }); snapshot.trial.items.Add(item);
            session.Bind(snapshot, session.Callbacks); item.choices[0].zh = "changed";
            Assert.That(session.Snapshot.trial.items[0].choices[0].zh, Is.EqualTo("原选项"));
            Assert.That(typeof(EndingTrialItemView).GetField("answer"), Is.Null);
            Assert.That(typeof(EndingTrialItemView).GetField("wrongLine"), Is.Null);
        }
    }
}
