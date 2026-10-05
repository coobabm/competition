using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Design v1.1 §3: the 游戏中心 games train the lab, follow its strength and earn a reaction that grows with the stage.</summary>
    public sealed class XgGamesTests
    {
        static XgSim Lab(int stage = 1)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.stage = stage;
            sim.Profile = new XgProfile { name = "小灯", self = "本机", callMe = "老大" };
            return sim;
        }

        [Test]
        public void AFinishedGameAddsSamplesAndCountsTheScore()
        {
            var sim = Lab();
            double before = sim.Labels("go");
            int added = sim.GamePlayed(XgGames.Go, XgGameOutcome.PlayerWon, 40);
            Assert.AreEqual(XgGames.SamplesFor(XgGames.Go, XgGameOutcome.PlayerWon, 40), added);
            Assert.Greater(added, 0);
            Assert.AreEqual(before + added, sim.Labels("go"), 1e-9, "go games feed the 围棋局面 dataset");

            sim.GamePlayed(XgGames.Go, XgGameOutcome.PlayerLost, 30);
            sim.GamePlayed(XgGames.Go, XgGameOutcome.Draw, 50);
            var r = sim.GameRecord(XgGames.Go);
            Assert.AreEqual(3, r.played);
            Assert.AreEqual(1, r.playerWins);
            Assert.AreEqual(1, r.playerLosses);
            Assert.AreEqual(1, r.draws);
            Assert.AreEqual(120, r.moves);
            Assert.AreEqual(0, sim.GameRecord(XgGames.Chess).played, "each game keeps its own score");
        }

        [Test]
        public void ChessTrainsReasoningAndAWinIsWorthMore()
        {
            var sim = Lab();
            double before = sim.Labels("logic");
            int lost = sim.GamePlayed(XgGames.Chess, XgGameOutcome.PlayerLost, 60);
            Assert.AreEqual(before + lost, sim.Labels("logic"), 1e-9);
            Assert.Greater(XgGames.SamplesFor(XgGames.Chess, XgGameOutcome.PlayerWon, 60), lost);
        }

        [Test]
        public void EveryFewMovesAddSamples()
        {
            var sim = Lab();
            Assert.AreEqual(0, sim.GameMove(XgGames.Gomoku, 9));
            Assert.AreEqual(XgGames.SamplesPerBatch, sim.GameMove(XgGames.Gomoku, XgGames.MovesPerBatch));
            Assert.AreEqual(0, sim.GameMove(XgGames.Gomoku, 0));
            Assert.AreEqual(XgGames.SamplesPerBatch, sim.GameRecord(XgGames.Gomoku).samples);
            Assert.AreEqual(XgGames.SamplesPerBatch, sim.Labels(sim.GameDataset(XgGames.Gomoku)), 1e-9);
        }

        [Test]
        public void TheAiPlaysFromItsStageAndGetsStronger()
        {
            Assert.IsFalse(XgGames.AiPlays(XgGames.Gomoku, 2));
            Assert.IsTrue(XgGames.AiPlays(XgGames.Gomoku, 3));
            Assert.IsFalse(XgGames.AiPlays(XgGames.Go, 3));
            Assert.IsTrue(XgGames.AiPlays(XgGames.Go, 4));
            Assert.IsFalse(XgGames.AiPlays(XgGames.Chess, 5));
            Assert.IsTrue(XgGames.AiPlays(XgGames.Chess, 6));

            Assert.AreEqual(0, XgGames.Skill(XgGames.Go, 3, 1));
            Assert.Less(XgGames.Skill(XgGames.Gomoku, 3, .5), XgGames.Skill(XgGames.Gomoku, 3, .95));
            Assert.Less(XgGames.Skill(XgGames.Go, 4, .5), XgGames.Skill(XgGames.Go, 6, .5));
            Assert.LessOrEqual(XgGames.Skill(XgGames.Gomoku, 6, 1), .98);

            Assert.AreEqual(3, XgGames.ChessDepth(5, 0), "before stage 6 the built-in program searches");
            Assert.AreEqual(1, XgGames.ChessDepth(6, .3));
            Assert.AreEqual(3, XgGames.ChessDepth(6, .95));
            Assert.Greater(XgGames.ChessRandomChance(6, .2), XgGames.ChessRandomChance(6, .9));
            Assert.AreEqual(0, XgGames.ChessRandomChance(5, 0));
        }

        [Test]
        public void ReactionsGrowWithTheStage()
        {
            CollectionAssert.AreEquivalent(new[] { XgReaction.Blink, XgReaction.YesNo }, XgGames.Tier(1));
            CollectionAssert.AreEquivalent(XgGames.Tier(1), XgGames.Tier(2));
            CollectionAssert.Contains(XgGames.Tier(3), XgReaction.WordIcon);
            CollectionAssert.Contains(XgGames.Tier(4), XgReaction.ReviewNote);
            CollectionAssert.Contains(XgGames.Tier(5), XgReaction.RecordFile);
            CollectionAssert.Contains(XgGames.Tier(6), XgReaction.RecycleBin);
            CollectionAssert.DoesNotContain(XgGames.Tier(5), XgReaction.RecycleBin);
            for (double roll = 0; roll < 1; roll += .01)
                CollectionAssert.Contains(XgGames.Tier(4), XgGames.PickReaction(4, XgReaction.None, roll));
        }

        [Test]
        public void NeverTheSameReactionTwiceInARow()
        {
            var rng = new Random(7);
            for (int stage = 1; stage <= 6; stage++)
            {
                var sim = Lab(stage);
                var last = XgReaction.None;
                var seen = new HashSet<XgReaction>();
                for (int i = 0; i < 200; i++)
                {
                    var r = sim.NextGameReaction(rng.NextDouble());
                    Assert.AreNotEqual(last, r, "stage " + stage);
                    CollectionAssert.Contains(XgGames.Tier(stage), r);
                    seen.Add(r);
                    last = r;
                }
                Assert.AreEqual(XgGames.Tier(stage).Length, seen.Count, "every reaction of stage " + stage + " shows up");
            }
            for (int count = 1; count <= 5; count++)
                for (int last = -1; last < count; last++)
                    foreach (var roll in new[] { 0, .5, .999999, 1.0 })
                    {
                        int i = XgGames.NoRepeat(count, last, roll);
                        Assert.That(i >= 0 && i < count);
                        if (count > 1) Assert.AreNotEqual(last, i);
                    }
        }

        [Test]
        public void LinesUseTheProfileAndTheScore()
        {
            var sim = Lab(5);
            sim.GamePlayed(XgGames.Gomoku, XgGameOutcome.PlayerLost, 30);
            sim.GamePlayed(XgGames.Gomoku, XgGameOutcome.PlayerLost, 30);
            sim.GamePlayed(XgGames.Gomoku, XgGameOutcome.PlayerWon, 30);
            var c = sim.GameContext(XgGames.Gomoku, XgGameOutcome.PlayerWon, 30);
            Assert.IsTrue(c.aiPlayed);
            var score = XgGames.Score(c, 0);
            StringAssert.Contains("2:1", score.zh);
            StringAssert.Contains("2:1", score.en);
            for (double roll = 0; roll < 1; roll += .05)
                foreach (var outcome in new[] { XgGameOutcome.PlayerWon, XgGameOutcome.PlayerLost, XgGameOutcome.Draw })
                    foreach (var played in new[] { true, false })
                    {
                        c.outcome = outcome; c.aiPlayed = played;
                        var lines = new List<(string zh, string en)> { XgGames.Sentence(c, roll), XgGames.Score(c, roll), XgGames.Title(c, roll), XgGames.Taunt(c, roll), XgGames.RecycleName(c), XgGames.Word(outcome, played, roll), XgGames.YesNo(outcome) };
                        lines.AddRange(XgGames.Review(c, roll));
                        foreach (var l in lines)
                        {
                            Assert.IsNotEmpty(l.zh); Assert.IsNotEmpty(l.en);
                            Assert.IsFalse(l.zh.Contains("{") || l.en.Contains("{"), l.zh + " / " + l.en);
                        }
                    }
            c.outcome = XgGameOutcome.PlayerLost; c.aiPlayed = true;
            Assert.AreEqual("赢", XgGames.Word(XgGameOutcome.PlayerLost, true, .9).zh, "it won");
            Assert.AreEqual("是", XgGames.YesNo(XgGameOutcome.PlayerWon).zh);
            Assert.AreEqual("否", XgGames.YesNo(XgGameOutcome.PlayerLost).zh);
            Assert.AreEqual("你的棋", XgGames.RecycleName(c).zh);
            var sentence = XgGames.Sentence(c, 0);
            Assert.IsTrue(sentence.zh.Contains("老大") || sentence.zh.Contains("本机"), sentence.zh);
        }

        [Test]
        public void PersonalityPicksTheVoice()
        {
            var c = new XgGameContext { game = XgGames.Chess, callMe = "老大", self = "本机", outcome = XgGameOutcome.PlayerLost, aiPlayed = true, moves = 40, play = 90, warmth = 50, opinion = 50 };
            var playful = XgGames.Taunt(c, 0).zh;
            c.play = 50; c.warmth = 90;
            var warm = XgGames.Taunt(c, 0).zh;
            c.warmth = 40;
            var curt = XgGames.Taunt(c, 0).zh;
            Assert.AreNotEqual(playful, warm);
            Assert.AreNotEqual(warm, curt);
            StringAssert.Contains("本机", curt);
        }

        [Test]
        public void CommentaryCoversEverySituation()
        {
            var c = new XgGameContext { callMe = "老大", self = "本机" };
            foreach (XgMoveSituation s in Enum.GetValues(typeof(XgMoveSituation)))
            {
                Assert.GreaterOrEqual(XgGames.CommentCount(s), 2, s.ToString());
                for (int i = 0; i < XgGames.CommentCount(s); i++)
                {
                    var l = XgGames.Comment(s, i, c);
                    Assert.IsNotEmpty(l.zh); Assert.IsNotEmpty(l.en);
                    Assert.IsFalse(l.zh.Contains("{") || l.en.Contains("{"));
                }
                if (s != XgMoveSituation.Quiet) Assert.IsTrue(XgGames.ShouldComment(s, .99));
            }
            Assert.IsFalse(XgGames.ShouldComment(XgMoveSituation.Quiet, .99));
        }
    }
}
