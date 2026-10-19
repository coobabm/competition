using System;
using System.Linq;
using NUnit.Framework;
using LingGuangV05.Core.Games;
namespace LingGuangV05.Tests
{
    public sealed class ChineseCheckersTests
    {
        [Test] public void StarHasSixPieceOppositeCampsAndNoDuplicateHoles()
        {
            var g=new ChineseCheckers();Assert.AreEqual(73,ChineseCheckers.Holes.Length);
            Assert.AreEqual(6,ChineseCheckers.Holes.Count(p=>p.r < -3));Assert.AreEqual(6,ChineseCheckers.Holes.Count(p=>p.r>3));
            Assert.AreEqual(12,g.S.pieces.Distinct().Count());Assert.AreEqual(0,g.HomeCount(0));Assert.AreEqual(0,g.HomeCount(1));
        }
        [Test] public void StepsAndRealMultiHopRoutesAreLegalAndNeverCapture()
        {
            var state=new ChineseCheckers().S;state.pieces[0]=ChineseCheckers.Hole(0,0);state.pieces[6]=ChineseCheckers.Hole(1,0);state.pieces[7]=ChineseCheckers.Hole(2,-1);
            var g=new ChineseCheckers(state);var moves=g.LegalMoves(0);
            Assert.True(moves.Any(m=>m.destination==ChineseCheckers.Hole(0,1)&&!m.Jump));
            var chain=moves.Find(m=>m.destination==ChineseCheckers.Hole(2,-2));Assert.NotNull(chain);Assert.GreaterOrEqual(chain.path.Count,5);
            int[] enemies=g.S.pieces.Skip(6).ToArray();Assert.True(g.Move(0,chain.destination));CollectionAssert.AreEqual(enemies,g.S.pieces.Skip(6));Assert.AreEqual(12,g.S.pieces.Distinct().Count());
        }
        [Test] public void CannotMoveOpponentOntoOccupiedHoleOrPassWithALegalMove()
        {
            var g=new ChineseCheckers();string before=string.Join(",",g.S.pieces);
            Assert.False(g.Move(6,ChineseCheckers.Hole(0,0)));Assert.False(g.Move(0,g.S.pieces[1]));Assert.False(g.Pass());
            Assert.AreEqual(before,string.Join(",",g.S.pieces));Assert.AreEqual(0,g.S.moves);
        }
        [Test] public void FortyTurnsEachProducesAnActualDrawWhenHomeCountsTie()
        {
            var state=new ChineseCheckers().S;state.turn=1;state.turns=new[]{40,39};state.moves=79;
            var g=new ChineseCheckers(state);var m=g.AllMoves()[0];Assert.True(g.Move(m.piece,m.destination));
            Assert.AreEqual(80,g.S.moves);Assert.AreEqual(2,g.S.winner);Assert.False(g.Move(m.piece,m.destination));Assert.False(g.Pass());
        }
        [Test] public void SixthPieceHomeImmediatelyWins()
        {
            var targets=Enumerable.Range(0,ChineseCheckers.Holes.Length).Where(i=>ChineseCheckers.Target(0,i)).ToArray();
            var top=Enumerable.Range(0,ChineseCheckers.Holes.Length).Where(i=>ChineseCheckers.Target(1,i)).ToArray();
            var state=new ChineseCheckersState{pieces=targets.Skip(1).Concat(new[]{ChineseCheckers.Hole(-3,3)}).Concat(top).ToArray()};
            var g=new ChineseCheckers(state);Assert.AreEqual(5,g.HomeCount(0));Assert.True(g.Move(5,ChineseCheckers.Hole(-3,4)));Assert.AreEqual(0,g.S.winner);Assert.AreEqual(6,g.HomeCount(0));
        }
        [Test] public void PlanningNeverMutatesTheBoardAndLevelZeroPrefersSteps()
        {
            var g=new ChineseCheckers();string before=string.Join(",",g.S.pieces);var random=new Random(8);
            var basic=g.Pick(0,random);Assert.NotNull(basic);Assert.False(basic.Jump);
            Assert.NotNull(g.Pick(3,random));Assert.AreEqual(before,string.Join(",",g.S.pieces));Assert.AreEqual(0,g.S.moves);Assert.IsEmpty(g.S.log);
        }
    }
}
