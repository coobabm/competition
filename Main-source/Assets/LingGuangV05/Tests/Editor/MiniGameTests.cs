using System;
using System.Diagnostics;
using LingGuangV05.Core.Games;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>The 围棋 (9 × 9 Go) and 国际象棋 (chess) mini-game engines: rules, scoring and the deterministic AIs.</summary>
    public sealed class MiniGameTests
    {
        // ------------------------------------------------------------------ Go

        static void Play(GoGame g, params int[] xy)
        {
            for (int i = 0; i < xy.Length; i += 2)
                Assert.IsTrue(g.Place(xy[i], xy[i + 1]), $"move ({xy[i]},{xy[i + 1]}) should be legal");
        }

        [Test]
        public void GoSurroundedStoneIsCaptured()
        {
            var g = new GoGame();
            // B(3,4) W(4,4) B(5,4) W(0,0) B(4,3) W(8,8): the white stone at (4,4) is in atari.
            Play(g, 3, 4, 4, 4, 5, 4, 0, 0, 4, 3, 8, 8);
            Assert.AreEqual(1, g.Liberties(4, 4));
            Assert.AreEqual(3, g.Liberties(3, 4));
            Play(g, 4, 5);
            Assert.AreEqual(GoGame.Empty, g[4, 4]);
            Assert.AreEqual(1, g.Captures(GoGame.Black));
            Assert.AreEqual(0, g.Captures(GoGame.White));
            Assert.AreEqual(GoGame.White, g.ToMove);
            Assert.AreEqual(4, g.LastX); Assert.AreEqual(5, g.LastY);
            Assert.AreEqual(7, g.Moves);
        }

        [Test]
        public void GoSuicideIsIllegal()
        {
            var g = new GoGame();
            Play(g, 0, 1, 5, 5, 1, 0);
            Assert.AreEqual(GoGame.White, g.ToMove);
            Assert.IsFalse(g.IsLegal(0, 0));
            Assert.IsFalse(g.Place(0, 0), "a stone with no liberties that captures nothing is suicide");
            Assert.AreEqual(GoGame.White, g.ToMove, "an illegal move does not use the turn");
            Assert.AreEqual(GoGame.Empty, g[0, 0]);
            Assert.IsFalse(g.Place(5, 5), "occupied");
            Assert.IsFalse(g.Place(9, 0), "off the board");
        }

        [Test]
        public void GoKoCannotBeRetakenAtOnce()
        {
            var g = new GoGame();
            // Black: (1,0) (0,1) (1,2) (2,1); White: (2,0) (3,1) (2,2), then White takes at (1,1).
            Play(g, 1, 0, 2, 0, 0, 1, 3, 1, 1, 2, 2, 2, 2, 1, 1, 1);
            Assert.AreEqual(GoGame.Empty, g[2, 1], "White's (1,1) captured the black stone at (2,1)");
            Assert.AreEqual(1, g.Captures(GoGame.White));
            Assert.AreEqual(2, g.KoX); Assert.AreEqual(1, g.KoY);
            Assert.IsFalse(g.Place(2, 1), "immediate recapture is ko");
            Play(g, 7, 7, 7, 6); // a ko threat elsewhere and an answer
            Assert.IsTrue(g.Place(2, 1), "after one exchange elsewhere Black may retake");
            Assert.AreEqual(GoGame.Empty, g[1, 1]);
            Assert.AreEqual(1, g.Captures(GoGame.Black));
            Assert.IsFalse(g.Place(1, 1), "and now White is the one barred by ko");
        }

        [Test]
        public void GoTwoPassesEndTheGame()
        {
            var g = new GoGame();
            Assert.IsTrue(g.Pass());
            Assert.IsFalse(g.Ended);
            Assert.AreEqual(-1, g.LastX);
            Play(g, 4, 4);          // White plays, which resets the pass count
            Assert.IsTrue(g.Pass());
            Assert.IsFalse(g.Ended);
            Assert.IsTrue(g.Pass());
            Assert.IsTrue(g.Ended);
            Assert.AreNotEqual(0, g.Winner);
            Assert.IsFalse(g.Place(0, 0), "no moves once the game is over");
            Assert.IsFalse(g.Pass());
            Assert.IsFalse(g.Best(out _, out _));

            var empty = new GoGame();
            empty.Pass(); empty.Pass();
            empty.Score(out double b, out double w);
            Assert.AreEqual(0, b); Assert.AreEqual(6.5, w);
            Assert.AreEqual(GoGame.White, empty.Winner, "komi decides an empty board");
        }

        [Test]
        public void GoAreaScoringCountsStonesAndTerritory()
        {
            var g = new GoGame();
            // Black walls column 4, White walls column 5.
            for (int y = 0; y < GoGame.Size; y++) Play(g, 4, y, 5, y);
            g.Score(out double b, out double w);
            Assert.AreEqual(9 + 36, b, "nine stones plus columns 0-3");
            Assert.AreEqual(9 + 27 + 6.5, w, "nine stones plus columns 6-8 plus komi");
            Assert.AreEqual(0, g.Winner, "no winner until the game ends");
            g.Pass(); g.Pass();
            Assert.AreEqual(GoGame.Black, g.Winner);

            // A region touching both colours is neutral.
            var n = new GoGame();
            Play(n, 0, 0, 8, 8);
            n.Score(out b, out w);
            Assert.AreEqual(1, b); Assert.AreEqual(1 + 6.5, w);
        }

        [Test]
        public void GoAiCapturesAndSaves()
        {
            // White's (4,4) stone is in atari; Black to move should take it.
            var g = new GoGame();
            Play(g, 3, 4, 4, 4, 5, 4, 0, 0, 4, 3, 8, 8);
            Assert.IsTrue(g.Best(out int x, out int y));
            Assert.AreEqual((4, 5), (x, y), "Black captures the stone in atari");

            // Now White to move with its stone in atari after Black's (4,3): White should extend out.
            var s = new GoGame();
            Play(s, 3, 4, 4, 4, 5, 4, 8, 8, 4, 3);
            Assert.AreEqual(1, s.Liberties(4, 4));
            Assert.IsTrue(s.Best(out x, out y));
            Assert.AreEqual((4, 5), (x, y), "White saves its stone");
            Assert.IsTrue(s.Place(x, y));
            Assert.AreEqual(3, s.Liberties(4, 4));
        }

        [Test]
        public void GoAiSelfPlayIsLegalAndEnds()
        {
            var g = new GoGame();
            var watch = Stopwatch.StartNew();
            int aiMoves = 0;
            while (!g.Ended && g.Moves < 2000)
            {
                if (g.Best(out int x, out int y))
                {
                    Assert.IsTrue(g.IsLegal(x, y));
                    Assert.IsTrue(g.Place(x, y), $"AI move ({x},{y}) at turn {g.Moves} must be legal");
                }
                else Assert.IsTrue(g.Pass());
                aiMoves++;
            }
            watch.Stop();
            Assert.IsTrue(g.Ended, "two passes end the self-play game");
            Assert.That(g.Winner, Is.EqualTo(GoGame.Black).Or.EqualTo(GoGame.White));
            Assert.Less(watch.Elapsed.TotalMilliseconds / aiMoves, 5.0, "each AI move takes under 5 ms");
            TestContext.WriteLine($"Go self-play: {g.Moves} turns, winner {g.Winner}, {watch.Elapsed.TotalMilliseconds / aiMoves:F3} ms/move");

            // Same position, same move.
            var a = new GoGame(); var b = new GoGame();
            for (int i = 0; i < 20; i++)
            {
                bool pa = a.Best(out int ax, out int ay), pb = b.Best(out int bx, out int by);
                Assert.AreEqual((pa, ax, ay), (pb, bx, by));
                if (pa) { a.Place(ax, ay); b.Place(bx, by); } else { a.Pass(); b.Pass(); }
            }
        }

        // ------------------------------------------------------------------ Chess

        const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";

        [Test]
        public void ChessPerftFromTheStart()
        {
            var g = new ChessGame();
            Assert.AreEqual(20, g.LegalMoves().Count);
            Assert.AreEqual(20, g.Perft(1));
            Assert.AreEqual(400, g.Perft(2));
            Assert.AreEqual(8902, g.Perft(3));
            Assert.AreEqual(ChessGame.StartFen, g.ToFen(), "perft leaves the position untouched");
        }

        [Test]
        public void ChessPerftKiwipete()
        {
            var g = ChessGame.FromFen(Kiwipete);
            Assert.AreEqual(Kiwipete, g.ToFen());
            Assert.AreEqual(48, g.Perft(1));
            Assert.AreEqual(2039, g.Perft(2));
            Assert.AreEqual(97862, g.Perft(3));
        }

        [Test]
        public void ChessBoardIndexerUsesFenLetters()
        {
            var g = new ChessGame();
            Assert.AreEqual('R', g[0, 0]);
            Assert.AreEqual('K', g[4, 0]);
            Assert.AreEqual('P', g[4, 1]);
            Assert.AreEqual('.', g[4, 3]);
            Assert.AreEqual('q', g[3, 7]);
            Assert.AreEqual('n', g[6, 7]);
            Assert.AreEqual(ChessGame.White, g.SideToMove);
            Assert.IsFalse(g.LastMove.IsValid);
            Assert.IsTrue(g.Move(4, 1, 4, 3));
            Assert.AreEqual("e2e4", g.LastMove.ToString());
            Assert.AreEqual(ChessGame.Black, g.SideToMove);
            Assert.AreEqual("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", g.ToFen());
            Assert.IsFalse(g.Move(4, 3, 4, 4), "not White's turn");
            Assert.IsFalse(g.Move(0, 6, 0, 3), "pawns do not jump three squares");
        }

        [Test]
        public void ChessFoolsMateIsCheckmate()
        {
            var g = new ChessGame();
            Assert.IsTrue(g.Move(5, 1, 5, 2));  // f3
            Assert.IsTrue(g.Move(4, 6, 4, 4));  // e5
            Assert.IsTrue(g.Move(6, 1, 6, 3));  // g4
            Assert.IsTrue(g.Move(3, 7, 7, 3));  // Qh4#
            Assert.IsTrue(g.InCheck);
            Assert.AreEqual(ChessGame.BlackWins, g.Result);
            Assert.AreEqual("将死", g.ResultReason);
            Assert.AreEqual("Checkmate", g.ResultReasonEnglish);
            Assert.AreEqual(0, g.LegalMoves().Count);
            Assert.IsFalse(g.Move(0, 1, 0, 2), "no moves after mate");
            Assert.IsFalse(g.Best().IsValid);
        }

        [Test]
        public void ChessStalemateIsADraw()
        {
            var g = ChessGame.FromFen("7k/8/6K1/8/8/8/5Q2/8 w - - 0 1");
            Assert.AreEqual(ChessGame.Ongoing, g.Result);
            Assert.IsTrue(g.Move(5, 1, 5, 6));  // Qf7, Black has no move but is not in check
            Assert.IsFalse(g.InCheck);
            Assert.AreEqual(ChessGame.Draw, g.Result);
            Assert.AreEqual("逼和", g.ResultReason);
            Assert.AreEqual("Stalemate", g.ResultReasonEnglish);

            Assert.AreEqual(ChessGame.Draw, ChessGame.FromFen("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1").Result, "loaded stalemate");
        }

        [Test]
        public void ChessEnPassant()
        {
            var g = new ChessGame();
            Assert.IsTrue(g.Move(4, 1, 4, 3));  // e4
            Assert.IsTrue(g.Move(0, 6, 0, 5));  // a6
            Assert.IsTrue(g.Move(4, 3, 4, 4));  // e5
            Assert.IsTrue(g.Move(3, 6, 3, 4));  // d5
            Assert.IsTrue(g.Move(4, 4, 3, 5));  // exd6 e.p.
            Assert.IsTrue(g.LastMove.IsEnPassant);
            Assert.IsTrue(g.LastMove.IsCapture);
            Assert.AreEqual('P', g[3, 5]);
            Assert.AreEqual('.', g[3, 4], "the passed pawn is removed");
            Assert.AreEqual('.', g[4, 4]);

            // The right expires after one move.
            var late = new ChessGame();
            late.Move(4, 1, 4, 3); late.Move(0, 6, 0, 5); late.Move(4, 3, 4, 4); late.Move(3, 6, 3, 4);
            late.Move(7, 1, 7, 2); late.Move(7, 6, 7, 5);
            Assert.IsFalse(late.Move(4, 4, 3, 5), "en passant only immediately");
        }

        [Test]
        public void ChessCastlingCannotPassThroughCheck()
        {
            // A black rook on f8 covers f1, so O-O is illegal, but O-O-O is fine.
            var g = ChessGame.FromFen("4kr2/8/8/8/8/8/8/R3K2R w KQ - 0 1");
            Assert.IsFalse(g.Move(4, 0, 6, 0), "king may not cross an attacked square");
            Assert.IsTrue(g.Move(4, 0, 2, 0));
            Assert.IsTrue(g.LastMove.IsCastle);
            Assert.AreEqual('K', g[2, 0]);
            Assert.AreEqual('R', g[3, 0]);
            Assert.AreEqual('.', g[0, 0]);

            var check = ChessGame.FromFen("4r1k1/8/8/8/8/8/8/R3K2R w KQ - 0 1");
            Assert.IsTrue(check.InCheck);
            Assert.IsFalse(check.Move(4, 0, 6, 0), "not out of check");
            Assert.IsFalse(check.Move(4, 0, 2, 0), "not out of check");

            var normal = ChessGame.FromFen("r3k2r/8/8/8/8/8/8/R3K2R b KQkq - 0 1");
            Assert.IsTrue(normal.Move(4, 7, 6, 7));
            Assert.AreEqual('r', normal[5, 7]);
            Assert.AreEqual("r4rk1/8/8/8/8/8/8/R3K2R w KQ - 1 2", normal.ToFen());
        }

        [Test]
        public void ChessPromotionDefaultsToQueenAndCanUnderpromote()
        {
            var q = ChessGame.FromFen("8/P7/8/8/8/8/8/k6K w - - 0 1");
            Assert.IsTrue(q.Move(0, 6, 0, 7));
            Assert.AreEqual('Q', q[0, 7]);
            Assert.IsTrue(q.LastMove.IsPromotion);

            var n = ChessGame.FromFen("8/P7/8/8/8/8/8/k6K w - - 0 1");
            Assert.IsTrue(n.Move(0, 6, 0, 7, 'n'));
            Assert.AreEqual('N', n[0, 7]);
            Assert.AreEqual(ChessGame.Draw, n.Result, "K+N v K is insufficient material");
            Assert.AreEqual("子力不足", n.ResultReason);

            var black = ChessGame.FromFen("k6K/8/8/8/8/8/1p6/8 b - - 0 1");
            Assert.IsTrue(black.Move(1, 1, 1, 0, 'R'));
            Assert.AreEqual('r', black[1, 0]);
        }

        [Test]
        public void ChessDrawRules()
        {
            Assert.AreEqual(ChessGame.Draw, ChessGame.FromFen("8/8/8/8/8/8/8/k6K w - - 0 1").Result, "K v K");
            Assert.AreEqual(ChessGame.Draw, ChessGame.FromFen("8/8/8/8/8/8/8/kb5K w - - 0 1").Result, "K+B v K");
            Assert.AreEqual(ChessGame.Ongoing, ChessGame.FromFen("8/8/8/8/8/8/8/kr5K w - - 0 1").Result, "a rook can mate");

            var fifty = ChessGame.FromFen("8/8/8/8/8/8/2R5/k6K w - - 99 80");
            Assert.AreEqual(ChessGame.Ongoing, fifty.Result);
            Assert.IsTrue(fifty.Move(2, 1, 1, 1));
            Assert.AreEqual(ChessGame.Draw, fifty.Result);
            Assert.AreEqual("Fifty-move rule", fifty.ResultReasonEnglish);

            var rep = new ChessGame();
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(rep.Move(6, 0, 5, 2)); Assert.IsTrue(rep.Move(6, 7, 5, 5));
                Assert.IsTrue(rep.Move(5, 2, 6, 0)); Assert.IsTrue(rep.Move(5, 5, 6, 7));
            }
            Assert.AreEqual(ChessGame.Draw, rep.Result);
            Assert.AreEqual("Threefold repetition", rep.ResultReasonEnglish);
        }

        [Test]
        public void ChessAiFindsMateInOne()
        {
            var g = ChessGame.FromFen("6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1");
            var m = g.Best(2);
            Assert.AreEqual("a1a8", m.ToString(), "back-rank mate");
            Assert.IsTrue(g.Move(m));
            Assert.AreEqual(ChessGame.WhiteWins, g.Result);

            var b = ChessGame.FromFen("r5k1/8/8/8/8/8/5PPP/6K1 b - - 0 1");
            Assert.AreEqual("a8a1", b.Best(3).ToString());

            // A free queen is taken.
            var grab = ChessGame.FromFen("4k3/8/8/3q4/8/8/8/3RK3 w - - 0 1");
            Assert.AreEqual("d1d5", grab.Best(2).ToString());
        }

        [Test]
        public void ChessAiIsFastAndDeterministic()
        {
            var g = new ChessGame();
            var watch = Stopwatch.StartNew();
            var m3 = g.Best(3);
            watch.Stop();
            Assert.IsTrue(m3.IsValid);
            Assert.Less(watch.Elapsed.TotalMilliseconds, 500, "depth 3 from the start answers in under half a second");
            Assert.AreEqual(m3, new ChessGame().Best(3));

            var kiwi = ChessGame.FromFen(Kiwipete);
            watch.Restart();
            var mk = kiwi.Best(3);
            watch.Stop();
            TestContext.WriteLine($"start d3 {m3}, kiwipete d3 {mk} in {watch.Elapsed.TotalMilliseconds:F1} ms");
            Assert.Less(watch.Elapsed.TotalMilliseconds, 2000);
        }

        [Test]
        public void ChessAiSelfPlayStaysLegal()
        {
            var g = new ChessGame();
            var watch = Stopwatch.StartNew();
            while (g.Result == ChessGame.Ongoing && g.Plies < 200)
            {
                var m = g.Best(2);
                Assert.IsTrue(m.IsValid);
                Assert.Contains(m, g.LegalMoves());
                Assert.IsTrue(g.Move(m), $"AI move {m} at ply {g.Plies} must be legal");
            }
            watch.Stop();
            Assert.That(g.Plies, Is.LessThanOrEqualTo(200));
            if (g.Result == ChessGame.Ongoing) Assert.AreEqual(200, g.Plies);
            else Assert.IsNotEmpty(g.ResultReason);
            TestContext.WriteLine($"Chess self-play: {g.Plies} plies, result {g.Result} {g.ResultReasonEnglish}, {watch.Elapsed.TotalMilliseconds / Math.Max(1, g.Plies):F1} ms/ply, {g.ToFen()}");
        }
    }
}
