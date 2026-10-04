using System;
using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Core.Games;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>Design v1.1 §3 (五子棋), §8 (both 老周 at the rules page, the last message, 2017).</summary>
    public sealed class EndgameTests
    {
        [Test]
        public void FiveInARowWinsAndTheAiBlocksAndFinishes()
        {
            var g = new Gomoku();
            for (int i = 0; i < 4; i++) { Assert.IsTrue(g.Place(3 + i, 7, Gomoku.Black)); Assert.IsTrue(g.Place(3 + i, 9, Gomoku.White)); }
            Assert.IsTrue(g.Best(Gomoku.White, out int x, out int y));
            Assert.AreEqual(9, y, "with its own four it finishes instead of defending");
            g.Place(x, y, Gomoku.White);
            Assert.AreEqual(Gomoku.White, g.Winner);
            Assert.IsFalse(g.Place(0, 0, Gomoku.Black), "no moves after a win");

            var block = new Gomoku();
            for (int i = 0; i < 3; i++) block.Place(5 + i, 5, Gomoku.Black);
            block.Place(0, 0, Gomoku.White); block.Place(14, 14, Gomoku.White);
            Assert.IsTrue(block.Best(Gomoku.White, out x, out y));
            Assert.AreEqual(5, y, "it blocks an open three");
            Assert.That(x, Is.EqualTo(4).Or.EqualTo(8));
        }

        [Test]
        public void BothLaoZhouAnswerTheRuleQuestionTheirOwnWay()
        {
            var f = new LaoZhouFacts { stage = 6, finale = true, today = new DateTime(2016, 12, 31) };
            Assert.AreEqual("这得你自己定。", LaoZhouFuture.Reply("底层规则要不要写关机？", f, 1, false));
            StringAssert.Contains("拿不出来", LaoZhouFuture.Reply("底层规则要不要写关机？", f, 2, false), "on a repeat, between the lines");
            StringAssert.Contains("当然要写", LaoZhouNow.Reply("给 AI 写关机规则，要不要写？", false, false, finale: true));
            StringAssert.Contains("Transformer", LaoZhouNow.Reply("谢谢你", false, false, ended: true), "the epilogue");
        }

        [Test]
        public void TheEndingTurnsTheClockTo2017()
        {
            var s = new GameState { gameSeconds = 1000 };
            GameCalendar.Advance(s, GameCalendar.DayIndex(GameCalendar.Ending));
            Assert.AreEqual(GameCalendar.Ending, GameCalendar.Now(s).Date);
            Assert.IsTrue(GameCalendar.NewYear(s));
            Assert.AreEqual(new DateTime(2017, 1, 1, 0, 0, 0), GameCalendar.Now(s));
            s.gameSeconds += 90;
            Assert.AreEqual(new DateTime(2017, 1, 1, 0, 1, 30), GameCalendar.Now(s));
            Assert.IsFalse(GameCalendar.NewYear(s), "once");
        }
    }
}
