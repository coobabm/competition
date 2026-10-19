using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Core.Story;
using NUnit.Framework;

public sealed class ZhouReviewEvidenceTests
{
    const string ReviewZh = "题和结果我都看了。答错的也留着，别光给我看对的。";
    const string ReviewEn = "I've looked at the tasks and the results. Keep the wrong ones too, don't only show me the right ones.";
    static GameState SavedCheck()
    {
        var save = new GameState(); save.story = new StoryState();
        save.story.MarkFired("era_expert_invite"); save.story.MarkFired("era_expert_result");
        save.story.SetFlag("era.expert.requested", 1); save.story.SetFlag("era.expert.passed", 1);
        save.story.RecordFirst(new StoryFirst { id = "era.delivery.receipt", value = 20, text = "Explicit authored delivery receipt fixture" });
        save.story.RecordFirst(new StoryFirst { id = "era.expert.receipt", value = .9, text = "Independent translation exam transcript fixture" });
        return save;
    }
    static ForumState Conversation(string review)
    {
        var forum = new ForumState(); var conv = forum.Conversation(ForumLibrary.ZhouNow);
        conv.messages.Add(new ForumMessage { from = ForumLibrary.Me, text = "Please review the material I sent." });
        conv.messages.Add(new ForumMessage { from = ForumLibrary.ZhouNow, text = review }); return forum;
    }
    [TestCase(ReviewZh)] [TestCase(ReviewEn)]
    public void CompletedReviewWithActualOwnConversationIsKnown(string text)
    { Assert.IsTrue(ZhouReviewEvidence.HasReview(SavedCheck(), Conversation(text))); }
    [Test]
    public void PriorHighStageAndContactAloneNeverEstablishReview()
    {
        var save = new GameState(); var forum = new ForumState { metZhouNow = true };
        save.labState = "{\"stage\":6}";
        Assert.IsFalse(ZhouReviewEvidence.HasReview(save, forum));
        Assert.IsEmpty(forum.conversations, "read-only evidence lookup must not create conversations");
    }
    [Test]
    public void DroppedMessageAcknowledgementWithoutReceivedMessageIsNotKnowledge()
    {
        var save = SavedCheck();
        save.storyMessageReceipts.Add("era_expert_result\u001fzhou_now\u001f" + ReviewZh);
        Assert.IsFalse(ZhouReviewEvidence.HasReview(save, new ForumState()));
    }
    [Test]
    public void MissingReceiptOrUnfinishedSceneCannotGrantKnowledge()
    {
        var save = SavedCheck(); var forum = Conversation(ReviewZh);
        save.story.firsts.RemoveAll(r => r.id == "era.expert.receipt");
        Assert.IsFalse(ZhouReviewEvidence.HasReview(save, forum));
        save = SavedCheck(); save.story.Unfire("era_expert_result");
        Assert.IsFalse(ZhouReviewEvidence.HasReview(save, forum));
    }
    [Test]
    public void QuotedReviewByPlayerOrAnotherNpcIsNotTheReviewersMessage()
    {
        var save = SavedCheck(); var forum = Conversation(ReviewZh);
        forum.conversations[0].messages[1].from = ForumLibrary.Me;
        Assert.IsFalse(ZhouReviewEvidence.HasReview(save, forum));
        forum.conversations[0].messages[1].from = ForumLibrary.LaoZhou;
        Assert.IsFalse(ZhouReviewEvidence.HasReview(save, forum));
        forum.conversations[0].messages[1].from = ForumLibrary.ZhouNow;
        forum.conversations[0].messages.RemoveAt(0);
        Assert.IsFalse(ZhouReviewEvidence.HasReview(save, forum), "no prior player contact");
    }
    [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(.8)] [TestCase(1.1)]
    public void InvalidOrUnpassedExamNeverBecomesKnown(double score)
    {
        var save = SavedCheck(); save.story.GetFirst("era.expert.receipt").value = score;
        Assert.IsFalse(ZhouReviewEvidence.HasReview(save, Conversation(ReviewEn)));
    }
    [Test]
    public void ReplacementSaveCannotBorrowOldReceipts()
    { Assert.IsFalse(ZhouReviewEvidence.HasReview(new GameState(), Conversation(ReviewZh))); }
}
