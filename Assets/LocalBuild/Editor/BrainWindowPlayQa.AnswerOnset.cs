#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.XingGuang;

public static partial class BrainWindowPlayQa
{
    // Isolated fixture only: retain the existing thirteenth card and actual host/Answer route.
    static void RunRealAnswerOnsetCheck()
    {
        var sim = ctl.Sim;
        var card = sim.Card(XgMemes.SenseDesk);
        var board = sim.Board;
        var feedback = window.Window.GetComponentInChildren<XgBrainWindowFeedback>();
        Check(sim.S.handCorrect + sim.S.handWrong == 12, "Onset regression uses the thirteenth real answer");
        Check(!card.family && !card.law, "Thirteenth card uses ordinary board teaching");
        foreach (int tier in XgCatalog.ComboTiers)
            Check(!(sim.S.combo < tier && sim.S.combo + (card.trick ? 2 : 1) >= tier),
                "Thirteenth answer avoids combo onset tier " + tier);
        Check(window.Visible && window.Drawing.Reveal.Visible, "Brain layers visible for real answer ordering");

        // The previous twelve real answers may have learned every element on this card.
        // Reset only this region in the isolated QA board so a real new-concept event is guaranteed.
        // Do not change the card, player preferences, quality level, or invoke presentation events.
        board.Reinitialise(XgBoardData.Region(card.dataset));
        window.OnsetGate.Clear();
        feedbackBefore = feedback.FeedbackCount;
        int correctBefore = feedback.CorrectCount;
        int startsBefore = feedback.AnimationStarts;
        int mergedBefore = feedback.MergedCount;
        bool reduced = feedback.Reduced;
        string displayRegion = XgBoardData.DisplayRegion(card.dataset);
        long answerCardId = card.id;
        var order = new List<string>();
        int matchingSettled = 0;
        Action<IReadOnlyList<XgConceptTransition>> conceptsChanged = changes =>
        {
            foreach (var change in changes)
                if (change.Region == displayRegion)
                { order.Add("ConceptsChanged"); break; }
        };
        Action<XgAnswerTrace> cardAnswered = answer =>
        {
            if (answer != null && answer.CardId == answerCardId && answer.Settled)
            { matchingSettled++; order.Add("CardAnswered"); }
        };
        board.ConceptsChanged += conceptsChanged;
        sim.CardAnswered += cardAnswered;
        try
        {
            var outcome = sim.Answer(XgMemes.SenseDesk, card.truth, ctl.Host);
            Check(outcome.correct && matchingSettled == 1, "One real correct answer settles exactly once");
            int conceptAt = order.IndexOf("ConceptsChanged");
            int answerAt = order.IndexOf("CardAnswered");
            Check(conceptAt >= 0 && answerAt > conceptAt, "Real ConceptsChanged precedes settled CardAnswered");
            Check(feedback.CorrectCount == correctBefore + 1 && feedback.FeedbackCount > feedbackBefore
                && feedback.LastReaction == "correct", "Real answer retains static correct feedback");
            if (reduced)
            {
                passed.Add("SKIP: real answer beam onset assertion because current reduced-motion/low-quality policy is active; event order and static feedback verified without changing preferences");
                Check(feedback.AnimationStarts == startsBefore, "Current reduced-motion policy starts no answer animation");
            }
            else
            {
                Check(feedback.AnimationStarts == startsBefore + 1,
                    "Real settled answer starts one beam despite preceding concept changes");
                Check(feedback.MergedCount == mergedBefore,
                    "Preceding concept changes do not reject settled answer at the shared gate");
            }
        }
        finally
        {
            board.ConceptsChanged -= conceptsChanged;
            sim.CardAnswered -= cardAnswered;
            File.WriteAllText(Path.Combine(output, "answer-onset-order.txt"),
                "Isolated fixture: current thirteenth card; board region reset before answering to guarantee new concepts.\n"
                + "Reduced policy: " + reduced + "\nOrder: " + string.Join(" -> ", order)
                + "\nAnimationStarts: " + startsBefore + " -> " + feedback.AnimationStarts
                + "\nMergedCount: " + mergedBefore + " -> " + feedback.MergedCount
                + "\nCorrectCount: " + correctBefore + " -> " + feedback.CorrectCount + "\n");
        }
    }
}
#endif
