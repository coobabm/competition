using System;
using NUnit.Framework;
using LingGuangV05.Core.Games;
namespace LingGuangV05.Tests
{
    public sealed class XiangqiRookEndgameTests
    {
        static int[] Position(int red,int black,int rook)
        {var cells=new int[90];cells[red]=1;cells[black]=-1;if(rook>=0)cells[rook]=2;return cells;}
        [Test] public void GeneralStaysInPalaceAndCannotMoveDiagonally()
        {
            var board=new XiangqiRookEndgame(Position(3,86,67));
            Assert.False(board.Move(new XiangqiRookMove(3,2)));Assert.False(board.Move(new XiangqiRookMove(3,13)));
            Assert.True(board.Move(new XiangqiRookMove(3,12)));
        }
        [Test] public void FacingGeneralsAreCheckAndCannotBeExposed()
        {
            var exposed=new XiangqiRookEndgame(Position(4,85,-1));Assert.True(exposed.InCheck(1));Assert.True(exposed.InCheck(-1));
            var blocked=new XiangqiRookEndgame(Position(4,85,40));Assert.False(blocked.InCheck(1));
            Assert.False(blocked.Move(new XiangqiRookMove(40,41)));Assert.True(blocked.Move(new XiangqiRookMove(40,49)));
        }
        [Test] public void RookCannotJumpOwnGeneralOrMoveDiagonally()
        {
            var board=new XiangqiRookEndgame(Position(13,84,4));
            Assert.False(board.Move(new XiangqiRookMove(4,22)));Assert.False(board.Move(new XiangqiRookMove(4,14)));
            Assert.True(board.Move(new XiangqiRookMove(4,5)));
        }
        [Test] public void BlackCanCaptureAnUnprotectedCheckingRook()
        {
            var board=new XiangqiRookEndgame(Position(5,84,75),-1);Assert.True(board.InCheck(-1));Assert.False(board.Checkmate);
            Assert.True(board.Move(new XiangqiRookMove(84,75)));Assert.AreEqual(-1,board[75]);Assert.AreEqual(0,board[84]);
        }
        [Test] public void KnownPositionIsSolvedByLegalMovesAndEveryDefenceIsRejected()
        {
            var board=new XiangqiRookEndgame(Position(4,84,67));Assert.False(board.InCheck(1));Assert.False(board.InCheck(-1));
            Assert.True(board.Move(new XiangqiRookMove(67,66)));Assert.True(board.Checkmate);Assert.IsEmpty(board.LegalMoves());
            Assert.False(board.Move(new XiangqiRookMove(84,85)));Assert.False(board.Move(new XiangqiRookMove(84,75)));
        }
        [TestCase(0)][TestCase(91)][TestCase(1729)]
        public void ComposerPublishesOnlyVerifiedMatesWithinBound(int seed)
        {
            var c=new XiangqiRookComposer(seed);Assert.IsNull(c.Position);c.Step();Assert.AreEqual(1,c.Attempts);
            for(int i=1;i<90&&!c.Finished;i++)c.Step();Assert.True(c.Finished);Assert.NotNull(c.Position);Assert.IsNotEmpty(c.Solutions);
            var board=new XiangqiRookEndgame(c.Position);Assert.False(board.InCheck(1));Assert.False(board.InCheck(-1));
            foreach(var solution in c.Solutions){var proof=board.Copy();Assert.True(proof.Move(solution));Assert.True(proof.Checkmate);Assert.AreEqual(-1,proof.Turn);Assert.False(proof.InCheck(1));}
            var leaked=c.Position;leaked[4]=0;Assert.AreEqual(1,c.Position[4]);
        }
        [Test] public void UnsupportedPiecesAndPalaceViolationsAreExplicitlyRejected()
        {
            var bad=Position(4,84,67);bad[30]=3;Assert.Throws<ArgumentException>(()=>new XiangqiRookEndgame(bad));
            Assert.Throws<ArgumentException>(()=>new XiangqiRookEndgame(Position(2,84,67)));
        }
    }
}
