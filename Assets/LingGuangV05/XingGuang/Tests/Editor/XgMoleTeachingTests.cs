using System;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgMoleTeachingTests
    {
        sealed class Host : IXgHost
        {
            public double Money => 10000;
            public double Compute => 1;
            public double VramMB => 8192;
            public string Blocker => null;
            public bool Spend(double amount) => true;
            public void Earn(double amount) => throw new Exception("Review must not pay");
            public void Train(double seconds) { }
        }
        static XgSim Open(out long ticket)
        {
            var s = new XgSim(); s.MiniGameSurfaceCheck = () => true;
            ticket = s.BeginMiniGame("arith_mole", new Host()); Assert.AreNotEqual(0, ticket);
            return s;
        }
        static XgMoleQuestion Calculus()
        {
            for (int i = 0; i < 100; i++) { var q = XgMoleArithmetic.Generate(i, 3); if (q.Kind >= XgMoleMathKind.Limit) return q; }
            throw new Exception("No calculus fixture");
        }
        [Test] public void ExplicitHumanCalculusLabelTrainsExactlyOnceWithoutSecondReward()
        {
            var s = Open(out _); var q = Calculus(); long token = s.NextMoleQuestionToken();
            Assert.IsTrue(s.RegisterMoleLesson(token, q));
            long cards = s.S.board.cards; double labels = s.Labels("arith"), income = s.S.totalIncome;
            var r = s.TeachMoleStatement(token, q.Truth);
            Assert.IsTrue(r.accepted && r.correct && r.trained);
            Assert.AreEqual(cards + 1, s.S.board.cards);
            Assert.IsTrue(s.S.board.concepts.Any(c => c.key.Contains("算符:")));
            Assert.AreEqual(labels, s.Labels("arith")); Assert.AreEqual(income, s.S.totalIncome);
            Assert.IsTrue(s.S.teachingNotebook.Last().playerAnswer == q.Truth);
            Assert.IsFalse(s.TeachMoleStatement(token, q.Truth).accepted);
            Assert.AreEqual(cards + 1, s.S.board.cards);
        }
        [Test] public void IncorrectHumanLabelIsNotReplacedWithGeneratedTruth()
        {
            var s = Open(out _); var q = Calculus(); long token = s.NextMoleQuestionToken();
            s.RegisterMoleLesson(token, q); long before = s.S.board.cards;
            var r = s.TeachMoleStatement(token, !q.Truth);
            Assert.IsTrue(r.accepted); Assert.IsFalse(r.correct || r.trained);
            Assert.AreEqual(before, s.S.board.cards);
            Assert.AreEqual(!q.Truth, s.S.teachingNotebook.Last().playerAnswer);
            Assert.IsFalse(s.TeachMoleStatement(token, q.Truth).accepted);
        }
        [Test] public void PredictionAndLeavingAQuestionNeverTeachIt()
        {
            var s = Open(out _); var q = Calculus(); long before = s.S.board.cards;
            s.RegisterMoleLesson(s.NextMoleQuestionToken(), q); s.PredictMole(q.Card());
            Assert.AreEqual(before, s.S.board.cards); Assert.AreEqual(0, s.S.teachingNotebook.Count);
        }
        [Test] public void HiddenCancelledRebuiltAndReloadedOwnersRejectOldLabels()
        {
            var s = Open(out long ticket); var q = Calculus(); long token = s.NextMoleQuestionToken();
            s.RegisterMoleLesson(token, q); s.MiniGameSurfaceCheck = () => false;
            Assert.IsFalse(s.TeachMoleStatement(token, q.Truth).accepted);
            s.MiniGameSurfaceCheck = () => true; s.CancelMiniGame(ticket);
            Assert.IsFalse(s.TeachMoleStatement(token, q.Truth).accepted);
            s.BeginMiniGame("arith_mole", new Host());
            Assert.IsFalse(s.TeachMoleStatement(token, q.Truth).accepted);
            long fresh = s.NextMoleQuestionToken(); s.RegisterMoleLesson(fresh, q); s.ClearMoleLessons();
            Assert.IsFalse(s.TeachMoleStatement(fresh, q.Truth).accepted);
            var reloaded = new XgSim(s.S); reloaded.MiniGameSurfaceCheck = () => true;
            reloaded.BeginMiniGame("arith_mole", new Host());
            Assert.IsFalse(reloaded.TeachMoleStatement(fresh, q.Truth).accepted);
        }
        [Test] public void ExplicitFalseLabelTrainsWithoutRewritingItAsTrue()
        {
            var s = Open(out _); XgMoleQuestion q = null;
            for (int i = 0; i < 1000; i++)
            {
                var candidate = XgMoleArithmetic.Generate(i, 3);
                if (candidate.Kind >= XgMoleMathKind.Limit && !candidate.Truth) { q = candidate; break; }
            }
            Assert.IsNotNull(q); long token = s.NextMoleQuestionToken();
            Assert.IsTrue(s.RegisterMoleLesson(token, q)); long before = s.S.board.cards;
            var result = s.TeachMoleStatement(token, false);
            Assert.IsTrue(result.accepted && result.correct && result.trained);
            Assert.AreEqual(before + 1, s.S.board.cards);
            Assert.IsFalse(s.S.teachingNotebook.Last().playerAnswer);
            Assert.IsFalse(s.S.teachingNotebook.Last().truth);
        }
        [Test] public void BlockedAttemptDoesNotConsumeTheHumanLesson()
        {
            var s = Open(out _); var q = Calculus(); long token = s.NextMoleQuestionToken();
            Assert.IsTrue(s.RegisterMoleLesson(token, q)); long before = s.S.board.cards;
            s.MiniGameSurfaceCheck = () => false;
            Assert.IsFalse(s.TeachMoleStatement(token, q.Truth).accepted);
            s.MiniGameSurfaceCheck = () => true; s.UseBoard = false;
            Assert.IsFalse(s.TeachMoleStatement(token, q.Truth).accepted);
            Assert.IsFalse(s.MoleLessonAnswered(token)); Assert.AreEqual(before, s.S.board.cards);
            s.UseBoard = true;
            Assert.IsTrue(s.TeachMoleStatement(token, q.Truth).trained);
            Assert.AreEqual(before + 1, s.S.board.cards);
        }
        [Test] public void LessonBoundEvictsOldTokensAndKeepsTheNewestAnswerable()
        {
            var s = Open(out _); var q = Calculus(); long oldest = s.NextMoleQuestionToken();
            Assert.IsTrue(s.RegisterMoleLesson(oldest, q)); long newest = oldest;
            for (int i = 0; i < 64; i++) { newest = s.NextMoleQuestionToken(); Assert.IsTrue(s.RegisterMoleLesson(newest, q)); }
            Assert.IsFalse(s.TeachMoleStatement(oldest, q.Truth).accepted);
            Assert.IsTrue(s.TeachMoleStatement(newest, q.Truth).trained);
            Assert.IsFalse(s.RegisterMoleLesson(newest, q));
        }
    }
}
