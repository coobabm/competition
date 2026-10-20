using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        public List<XgCard> workCards = new List<XgCard>();
        public string workDesk = "sense";
        public int workSubmitted, workCorrect;
        public double workIncome;
    }

    public sealed partial class XgSim
    {
        // Company-owned submissions have their own persistent queue. They never consume
        // a private lesson or become training examples in the player's local model.
        public XgCard WorkCard(string desk)
        {
            if (XgCatalog.CrowdDesk(desk) == null) return null;
            if (S.workCards == null) S.workCards = new List<XgCard>();
            S.workCards.RemoveAll(c => c == null);
            foreach (var card in S.workCards)
                if (card.dataset == desk && Valid(card)) return card;
            var next = CreateLabelCard(desk, false, true);
            var lesson = S.cards.Find(c => c != null && c.dataset == desk);
            // Text banks are finite; avoid presenting the very same statement in both
            // open windows even though the generated card already has an independent id.
            for (int tries = 0; tries < 12 && lesson != null && !string.IsNullOrEmpty(next.question)
                && next.question == lesson.question; tries++) next = CreateLabelCard(desk, false, true);
            next.gold = false;
            next.timeLimit = 0;
            S.workCards.RemoveAll(c => c.dataset == desk);
            S.workCards.Add(next);
            return next;
        }

        public bool TryAnswerWork(string desk, long cardId, bool answer, IXgHost host, out XgAnswer result)
        {
            result = default;
            if (host == null || XgCatalog.CrowdDesk(desk) == null || !CrowdDeskOpen(desk)) return false;
            var card = WorkCard(desk);
            if (card.id != cardId) return false;
            result = new XgAnswer { accepted = true, truth = card.truth, correct = answer == card.truth,
                bounty = card.bounty, trick = card.trick };
            S.workSubmitted++;
            if (result.correct)
            {
                Hit(XgCatalog.Desk(desk).comboWindow);
                result.pay = ManualPayFor(desk, card.level) * ComboMultiplier * (card.bounty ? BountyMultiplier : 1);
                host.Earn(result.pay);
                S.totalIncome += result.pay;
                S.workCorrect++;
                QualityHandCorrect();
            }
            else
            {
                BreakCombo();
                result.fine = ChargeHandFine(desk, card.level, host);
            }
            result.combo = S.combo;
            S.workIncome += result.pay - result.fine;
            S.workCards.RemoveAll(c => c.dataset == desk);
            return true;
        }
    }
}
