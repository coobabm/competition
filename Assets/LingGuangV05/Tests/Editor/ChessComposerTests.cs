using System;
using NUnit.Framework;
using LingGuangV05.Core.Games;
namespace LingGuangV05.Tests
{
    public sealed class ChessComposerTests
    {
        static ChessComposer Compose(int seed)
        {var c=new ChessComposer(seed);for(int i=0;i<64&&!c.Finished;i++)c.Step();Assert.True(c.Finished);Assert.NotNull(c.Fen);return c;}
        [TestCase(0)][TestCase(27)][TestCase(104729)]
        public void EveryPublishedSolutionIsLegalMateAndInitialPositionHasNoCheck(int seed)
        {
            var c=Compose(seed);var board=new ChessGame(c.Fen);Assert.False(board.InCheck);Assert.AreEqual(ChessGame.Ongoing,board.Result);
            Assert.False(new ChessGame(c.Fen.Replace(" w "," b ")).InCheck);Assert.IsNotEmpty(c.Solutions);
            int pieces=0;for(int rank=0;rank<8;rank++)for(int file=0;file<8;file++)if(board[file,rank]!='.')pieces++;
            Assert.AreEqual(3,pieces);
            foreach(var move in c.Solutions)
            {var proof=new ChessGame(c.Fen);Assert.True(proof.Move(move));Assert.AreEqual(ChessGame.WhiteWins,proof.Result);Assert.AreEqual("Checkmate",proof.ResultReasonEnglish);Assert.IsEmpty(proof.LegalMoves());}
        }
        [Test] public void CompositionIsBoundedIncrementalAndDeterministic()
        {
            var a=new ChessComposer(42);Assert.IsNull(a.Fen);a.Step();Assert.AreEqual(1,a.Attempts);
            while(!a.Finished)a.Step();var b=Compose(42);Assert.AreEqual(a.Fen,b.Fen);Assert.AreEqual(a.Attempts,b.Attempts);Assert.LessOrEqual(a.Attempts,64);
            string before=a.Fen;int count=a.Attempts;a.Step();Assert.AreEqual(before,a.Fen);Assert.AreEqual(count,a.Attempts);
        }
    }
}
