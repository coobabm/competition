using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.YY
{
    public sealed class EndingPrivatePreview
    {
        internal object save, chat, lab;
        internal EndingPrivateOffer offer;
        public string Text => offer?.text ?? "";
        public bool English => offer?.english ?? false;
    }
    public sealed partial class YYChatHub
    {
        bool EndingDisclosureReady => S != null && runtime?.Sim != null && ReferenceEquals(runtime.Sim, linked)
            && ZhouContactReady && EndingPrivateDisclosure.ExplicitYyContact(TiebaHub.Instance?.ZhouQa?.yyLinkSource)
            && AppInstalled && !runtime.Sim.InPrologue && Conversation(LaoZhou) != null
            && XingGuangController.Instance != null && XingGuangController.Instance.BoundTo(linked)
            && !IsTyping(LaoZhou) && ChoicesFor != LaoZhou;
        List<EndingPrivateOffer> EndingOffers(bool en) => EndingPrivateDisclosure.Offers(
            EndingDisclosureReady ? FindLab()?.S : null, S?.endingPrivate, EndingDisclosureReady, en);
        public string[] EndingPrivateChoices()
        {
            var offers = EndingOffers(GameText.IsEnglish);
            return offers.Count == 0 ? null : offers.ConvertAll(o => o.label).ToArray();
        }
        public EndingPrivatePreview PreviewEndingPrivate(string label)
        {
            foreach (var offer in EndingOffers(GameText.IsEnglish))
                if (offer.label == label) return new EndingPrivatePreview { save = linked, chat = S, lab = FindLab()?.S, offer = offer };
            return null;
        }
        public bool CurrentEndingPrivate(EndingPrivatePreview preview)
        {
            if (preview?.offer == null || !EndingDisclosureReady || !ReferenceEquals(linked, preview.save)
                || !ReferenceEquals(S, preview.chat) || !ReferenceEquals(FindLab()?.S, preview.lab) || preview.English != GameText.IsEnglish) return false;
            foreach (var offer in EndingOffers(preview.English))
                if (SameOffer(preview.offer, offer)) return true;
            return false;
        }
        static bool SameOffer(EndingPrivateOffer a, EndingPrivateOffer b) => a.kind == b.kind && a.scope == b.scope
            && a.route == b.route && a.text == b.text && string.Join("\n",a.replies) == string.Join("\n",b.replies);
        public bool SendEndingPrivate(EndingPrivatePreview preview)
        {
            if (!CurrentEndingPrivate(preview)) return false;
            var offer = preview.offer;
            // Reviewed fixed copy remains verbatim; the AI answer was already dynamically guarded.
            if (XgEndingTextGuard.FilterReviewed(offer.text, preview.English) != offer.text) return false;
            foreach (var reply in offer.replies) if (XgEndingTextGuard.FilterReviewed(reply, preview.English) != reply) return false;
            var state = S.endingPrivate ?? (S.endingPrivate = new EndingPrivateState());
            var receipt = EndingPrivateDisclosure.Receive(state, offer, Guid.NewGuid().ToString("N"), Now);
            if (receipt == null) return false;
            var conversation = Conversation(LaoZhou);
            conversation.messages.Add(new YYMessage { from = Me, text = receipt.text, rawPlayerText = receipt.text,
                gameSeconds = Now, dailyEventId = receipt.sourceRef, laoZhouObserved = true });
            foreach (var reply in receipt.replies) conversation.messages.Add(new YYMessage { from = LaoZhou, text = reply,
                gameSeconds = Now, dailyEventId = receipt.sourceRef, laoZhouObserved = true });
            if (!IsShowing(LaoZhou)) conversation.unread += receipt.replies.Length;
            // The actual sent text may inform later chat. Merely previewing never reaches this path.
            TiebaHub.Instance.ZhouObserveYy(receipt.text, Now, conversation.messages.Count - receipt.replies.Length - 1);
            Touch(); // Both exchange and receipt are serialized by the normal chat save, together.
            return true;
        }
    }
}
