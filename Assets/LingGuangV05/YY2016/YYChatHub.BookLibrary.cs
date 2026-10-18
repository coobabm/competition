using System;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatHub
    {
        // Receipt and text share the existing serialized YY row. Text equality is never a receipt.
        public bool RecordBookCompletion(ChapterOneSim owner, string key, string text, out bool added)
        {
            added = false;
            if (S == null || !S.lingguangUnlocked || !ReferenceEquals(linked, owner)
                || !ReferenceEquals(runtime?.Sim, owner) || string.IsNullOrEmpty(key)) return false;
            var conversation = Conversation(LingGuangId);
            string receipt = "library:" + key;
            if (conversation.messages.Exists(m => m.attentionWord == receipt)) return true;
            conversation.messages.Add(new YYMessage { from = LingGuangId, text = text, attentionWord = receipt, gameSeconds = Now });
            if (!IsShowing(LingGuangId)) conversation.unread++;
            added = true; Touch(); return true;
        }
        public void NotifyBookCompletion(ChapterOneSim owner, string text)
        {
            if (!ReferenceEquals(linked, owner) || !ReferenceEquals(runtime?.Sim, owner)) return;
            var state = S;
            var contact = Contact(LingGuangId);
            string sender = contact == null ? LingGuangId : GameText.T(contact.name, contact.nameEn);
            DesktopNotifications.Notify(runtime, "YY", sender, text, IsShowing(LingGuangId), () =>
            {
                if (ReferenceEquals(S, state) && ReferenceEquals(runtime?.Sim, owner) && router != null)
                    router.Open(AppId, LingGuangId);
            });
        }
    }
}
