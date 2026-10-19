using System;
using NUnit.Framework;
using LingGuangV05.Core.Games;
namespace LingGuangV05.Tests
{
    public sealed class LudoGameTests
    {
        [Test] public void FiveAndSixTakeOffButOtherRollsPass()
        {
            var g=new LudoGame();Assert.True(g.Roll(4));Assert.AreEqual(1,g.S.turn);Assert.AreEqual(0,g.S.die);
            Assert.True(g.Roll(5));Assert.True(g.Move(0));Assert.AreEqual(0,g.Position(1,0));Assert.AreEqual(0,g.S.turn);
            Assert.True(g.Roll(6));Assert.True(g.Move(0));Assert.AreEqual(0,g.Position(0,0));Assert.AreEqual(0,g.S.turn);
        }
        [Test] public void ColourJumpOccursOnceAndDoesNotSkipHome()
        {
            var g=new LudoGame();Assert.AreEqual(8,g.Landing(0,3,1));Assert.AreEqual(8,g.Landing(1,3,1));
            Assert.AreEqual(24,g.Landing(0,23,1));Assert.AreEqual(28,g.Landing(0,27,1));Assert.AreEqual(25,g.Landing(0,27,4));
        }
        [Test] public void LandingCapturesBothStackedEnemyPlanes()
        {
            var g=new LudoGame(new LudoState{planes=new[]{0,-1,15,15}});
            Assert.True(g.Roll(1));Assert.True(g.Move(0));Assert.AreEqual(-1,g.Position(1,0));Assert.AreEqual(-1,g.Position(1,1));
        }
        [Test] public void ExactHomeEndsOnlyAfterBothPlanesFinish()
        {
            var g=new LudoGame(new LudoState{planes=new[]{28,27,-1,-1}});
            Assert.True(g.Roll(1));Assert.False(g.Move(0));Assert.True(g.Move(1));Assert.True(g.Over);Assert.AreEqual(0,g.S.winner);
            int actions=g.S.actions;Assert.False(g.Roll(6));Assert.False(g.Move(1));Assert.AreEqual(actions,g.S.actions);
        }
        [Test] public void InvalidMoveAndRerollDoNotMutateTheTurn()
        {
            var g=new LudoGame();Assert.False(g.Move(0));Assert.False(g.Roll(7));Assert.True(g.Roll(5));
            Assert.False(g.Roll(6));Assert.False(g.Move(8));Assert.AreEqual(5,g.S.die);Assert.AreEqual(1,g.S.rolls);
        }
        [Test] public void BuiltInTacticsPreferAnAvailableCapture()
        {
            var g=new LudoGame(new LudoState{planes=new[]{0,10,15,-1}});g.Roll(1);
            Assert.AreEqual(0,g.PickPlane(1,new Random(7)));
        }
    }
}
