using System;

namespace LingGuangV05.Core.Forum
{
    /// <summary>Knowledge of the authored translation check comes from this save's receipts AND received conversation.</summary>
    public static class ZhouReviewEvidence
    {
        const string ReviewZh = "题和结果我都看了。答错的也留着，别光给我看对的。";
        const string ReviewEn = "I've looked at the tasks and the results. Keep the wrong ones too, don't only show me the right ones.";
        public static bool HasReview(GameState save, ForumState forum)
        {
            var story = save?.story;
            if (story == null || forum?.conversations == null
                || !story.HasFired("era_expert_invite") || !story.HasFired("era_expert_result")
                || story.GetFlag("era.expert.requested") <= 0 || story.GetFlag("era.expert.passed") <= 0) return false;
            var delivery = story.GetFirst("era.delivery.receipt");
            var review = story.GetFirst("era.expert.receipt");
            if (delivery == null || delivery.value != 20 || string.IsNullOrWhiteSpace(delivery.text)
                || review == null || string.IsNullOrWhiteSpace(review.text)
                || double.IsNaN(review.value) || double.IsInfinity(review.value) || review.value < .85 || review.value > 1) return false;
            // Delivery markers alone are insufficient: ending cleanup can acknowledge a dropped story message.
            // Do not call ForumState.Conversation here; a knowledge query must not create a conversation.
            foreach (var conversation in forum.conversations)
            {
                if (conversation == null || conversation.id != ForumLibrary.ZhouNow || conversation.messages == null) continue;
                bool receivedPlayerMessage = false;
                foreach (var message in conversation.messages)
                {
                    if (message == null) continue;
                    if (message.from == ForumLibrary.Me && !string.IsNullOrWhiteSpace(message.text)) receivedPlayerMessage = true;
                    else if (receivedPlayerMessage && message.from == ForumLibrary.ZhouNow
                        && (string.Equals(message.text, ReviewZh, StringComparison.Ordinal)
                            || string.Equals(message.text, ReviewEn, StringComparison.Ordinal))) return true;
                }
            }
            return false;
        }
    }
}
