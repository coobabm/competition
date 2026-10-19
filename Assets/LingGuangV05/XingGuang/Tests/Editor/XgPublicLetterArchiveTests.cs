using System;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgPublicLetterArchiveTests
    {
        // Explicit terminal-release fixture: exercises the archive transaction, not natural gameplay prerequisites.
        sealed class Fixture
        {
            public readonly XgState state=new XgState();
            public readonly object identity=new object();
            public bool current=true,succeeds=true;
            public int saves;
            public string disk;
            public readonly XgEpilogueFlow flow;
            public Fixture()
            {
                state.ending="E3-legacy";
                state.releaseOutcome=new ReleaseOutcome{route="local_only",decisionOwner="player",releaseReceiptId="fixture:release",committedAt=20161231,
                    prerequisites=new XgReleasePrerequisites{memoryCardsReceiptId="m",redTeamOneReceiptId="r1",redTeamTwoReceiptId="r2",redTeamThreeReceiptId="r3",fixedThreeSlotsReceiptId="slots"}};
                state.epilogue=new EpilogueState{phase="letter",letterId="zhou_unpressed_button"};
                flow=new XgEpilogueFlow(state,identity,()=>current,()=>{saves++;if(!succeeds)return false;disk=JsonUtility.ToJson(state);return true;});
            }
            public XgEpilogueResult Save(string id="zhou_copies")=>flow.SavePublicArticle(flow.CaptureBinding(),id,1,id);
        }
        [Test]
        public void PublicBookmarkDoesNotGrantEndingMailOrCompletion()
        {
            var f=new Fixture();Assert.IsTrue(f.Save().Success);
            Assert.IsTrue(f.state.epilogue.HasPublicArticle("zhou_copies",1));
            Assert.AreEqual("letter",f.state.epilogue.phase);Assert.AreEqual("zhou_unpressed_button",f.state.epilogue.letterId);
            Assert.AreEqual(0,f.state.epilogue.savedAt);Assert.IsFalse(f.state.epilogue.opened);Assert.IsFalse(f.state.epilogue.consentShown);
            Assert.AreEqual("",f.state.epilogue.completionReceipt);Assert.IsFalse(f.state.chapterComplete);Assert.AreEqual("E3-legacy",f.state.ending);
            Assert.AreEqual("wrong_article",f.flow.SaveArticle(f.flow.CaptureBinding(),"zhou_copies",1,"zhou_copies").Code);
            Assert.AreEqual("wrong_article",f.flow.OpenArticle(f.flow.CaptureBinding(),"zhou_copies").Code);
            Assert.AreEqual("wrong_article",f.flow.Complete(f.flow.CaptureBinding(),"zhou_copies",1).Code);
            Assert.AreEqual("letter_not_ready",f.flow.Complete(f.flow.CaptureBinding(),"zhou_unpressed_button",1).Code);
        }
        [Test]
        public void FailureRestoresExactReferencesAndRetryIsIdempotent()
        {
            var f=new Fixture();var before=f.state.epilogue;var release=f.state.releaseOutcome;f.succeeds=false;
            Assert.AreEqual("save_failed",f.Save().Code);Assert.AreSame(before,f.state.epilogue);Assert.AreSame(release,f.state.releaseOutcome);
            Assert.AreEqual(0,before.publicArticleArchives.Count);Assert.AreEqual(0,before.revision);Assert.IsFalse(f.state.chapterComplete);
            f.succeeds=true;Assert.IsTrue(f.Save().Changed);int writes=f.saves;int revision=f.state.epilogue.revision;
            Assert.AreEqual("unchanged",f.Save().Code);Assert.AreEqual(writes,f.saves);Assert.AreEqual(revision,f.state.epilogue.revision);
            var restored=JsonUtility.FromJson<XgState>(f.disk);Assert.IsTrue(restored.epilogue.HasPublicArticle("zhou_copies",1));
        }
        [Test]
        public void OldNullFieldAndCompletedReplayPreserveTheirExistingSemantics()
        {
            var f=new Fixture();f.state.epilogue.publicArticleArchives=null;f.state.epilogue.phase="completed";
            f.state.epilogue.opened=true;f.state.epilogue.savedAt=20170613;f.state.epilogue.readParagraphId="signature";
            f.state.epilogue.completionReceipt="old:receipt";f.state.epilogue.completedAt=20170613;f.state.chapterComplete=true;
            Assert.IsTrue(f.Save().Success);Assert.IsTrue(f.state.chapterComplete);Assert.AreEqual("completed",f.state.epilogue.phase);
            Assert.AreEqual("old:receipt",f.state.epilogue.completionReceipt);Assert.AreEqual("signature",f.state.epilogue.readParagraphId);
            Assert.IsTrue(f.state.epilogue.opened);Assert.AreEqual(20170613,f.state.epilogue.savedAt);
            var copy=f.state.epilogue.Copy();copy.publicArticleArchives[0].sourceArticleId="changed";
            Assert.IsTrue(f.state.epilogue.HasPublicArticle("zhou_copies",1));
        }
        [Test]
        public void StaleSlotOrRevisionAndInvalidSourceCannotWrite()
        {
            var f=new Fixture();var token=f.flow.CaptureBinding();Assert.IsTrue(f.Save().Success);int writes=f.saves;
            Assert.AreEqual("stale_binding",f.flow.SavePublicArticle(token,"zhou_open_door",1,"zhou_open_door").Code);
            var other=new Fixture();Assert.AreEqual("stale_binding",other.flow.SavePublicArticle(f.flow.CaptureBinding(),"zhou_open_door",1,"zhou_open_door").Code);
            f.current=false;Assert.AreEqual("stale_binding",f.Save("zhou_open_door").Code);f.current=true;
            Assert.AreEqual("invalid_public_article",f.flow.SavePublicArticle(f.flow.CaptureBinding(),"private_letter",1,"private_letter").Code);
            Assert.AreEqual("invalid_public_article",f.flow.SavePublicArticle(f.flow.CaptureBinding(),"zhou_open_door",1,"other").Code);
            Assert.AreEqual(writes,f.saves);Assert.AreEqual(0,other.saves);
        }
        [Test]
        public void BeforePublicColumnAndLegacyOnlySavesRemainUnavailable()
        {
            var f=new Fixture();f.state.epilogue.phase="date";Assert.AreEqual("public_letters_unavailable",f.Save().Code);
            f.state.releaseOutcome=null;f.state.chapterComplete=true;Assert.IsFalse(XgEpilogueFlow.CanBrowsePublicLetters(f.state));
            Assert.AreEqual("public_letters_unavailable",f.Save().Code);Assert.AreEqual(0,f.saves);Assert.IsTrue(f.state.chapterComplete);
        }
    }
}
