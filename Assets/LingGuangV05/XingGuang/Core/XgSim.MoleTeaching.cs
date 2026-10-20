using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed class XgMoleTeachingResult
    {
        public bool accepted, correct, trained;
    }

    public sealed partial class XgSim
    {
        sealed class MoleLesson
        {
            public long ticket;
            public XgMoleQuestion question;
            public bool consumed;
        }
        // Transient ownership only. Loading, changing games or rebuilding the surface cannot replay a lesson.
        readonly Dictionary<long, MoleLesson> moleLessons = new Dictionary<long, MoleLesson>();
        readonly Queue<long> moleLessonOrder = new Queue<long>();
        public void ClearMoleLessons() { moleLessons.Clear(); moleLessonOrder.Clear(); }
        public bool RegisterMoleLesson(long token, XgMoleQuestion question)
        {
            if (token <= 0 || question == null || miniTicket == 0 || (miniId != "arith_mole" && miniId != "dadishu") || moleLessons.ContainsKey(token)) return false;
            moleLessons.Add(token, new MoleLesson { ticket = miniTicket, question = question });
            moleLessonOrder.Enqueue(token);
            while (moleLessonOrder.Count > 64) moleLessons.Remove(moleLessonOrder.Dequeue());
            return true;
        }
        public bool MoleLessonAnswered(long token) => moleLessons.TryGetValue(token, out var lesson) && lesson.ticket == miniTicket && lesson.consumed;

        /// <summary>Explicit human true/false correction. No wages, score or extra sample reward; auto actions never call this.</summary>
        public XgMoleTeachingResult TeachMoleStatement(long token, bool playerLabel)
        {
            var result = new XgMoleTeachingResult();
            if (!UseBoard || !MiniGameSurfaceActive || miniTicket == 0 || MiniGameBlocker(miniId) != null
                || !moleLessons.TryGetValue(token, out var lesson) || lesson.ticket != miniTicket || lesson.consumed) return result;
            lesson.consumed = true;
            result.accepted = true;
            result.correct = playerLabel == lesson.question.Truth;
            var card = lesson.question.Card();
            // Match ordinary checked teaching: a correct human label is accepted as supervision.
            // An incorrect attempt displays its correction but cannot silently supply the generated truth as a label.
            if (result.correct)
            {
                TeachBoard(card, playerLabel);
                RememberTaught(card);
                RememberCorrection(card);
                result.trained = true;
            }
            RecordTeachingQuestion(card.question, card.questionEn, card.truth, playerLabel,
                lesson.question.Why, lesson.question.WhyEn, "arith", card,
                playerLabel ? T("题目正确", "The statement is true") : T("题目错误", "The statement is false"), "");
            return result;
        }
    }
}
