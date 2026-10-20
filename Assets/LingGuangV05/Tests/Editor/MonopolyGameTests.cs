using System.Linq;
using NUnit.Framework;
using LingGuangV05.Core.Games;
namespace LingGuangV05.Tests
{
    public sealed class MonopolyGameTests
    {
        [Test] public void UnownedPropertyRequiresExplicitAffordableChoice()
        {
            var g=new MonopolyGame();Assert.True(g.Roll(1,20160601,0));Assert.AreEqual(1,g.S.pending);Assert.AreEqual(1500,g.S.cash[0]);Assert.AreEqual(0,g.S.moves);
            Assert.False(g.Roll(1,20160601,0));Assert.True(g.ChoosePurchase(true));Assert.AreEqual(0,g.S.owners[1]);Assert.AreEqual(1280,g.S.cash[0]);Assert.AreEqual(1500,g.Worth(0));Assert.AreEqual(1,g.S.turn);
            Assert.False(g.ChoosePurchase(true));
        }
        [Test] public void CannotBuyUnaffordableLandButCanSkip()
        {
            var state=new MonopolyGame().S;state.cash[0]=100;var g=new MonopolyGame(state);Assert.True(g.Roll(1,20160601,0));
            Assert.False(g.ChoosePurchase(true));Assert.AreEqual(100,g.S.cash[0]);Assert.AreEqual(-1,g.S.owners[1]);Assert.True(g.ChoosePurchase(false));Assert.AreEqual(1,g.S.turns[0]);
        }
        [Test] public void PassingStartPaysAndRentTransfersOnlyPlayMoney()
        {
            var state=new MonopolyGame().S;state.position[0]=19;state.owners[1]=1;var g=new MonopolyGame(state);
            Assert.True(g.Roll(2,20160601,0));Assert.AreEqual(1656,g.S.cash[0]);Assert.AreEqual(1544,g.S.cash[1]);Assert.AreEqual(-1,g.S.pending);Assert.AreEqual(1,g.S.moves);
        }
        [Test] public void ElectricBillCanCauseAnActualTerminalBankruptcy()
        {
            var state=new MonopolyGame().S;state.position[0]=17;state.cash[0]=99;var g=new MonopolyGame(state);
            Assert.True(g.Roll(1,20160601,0));Assert.AreEqual(-1,g.S.cash[0]);Assert.AreEqual(1,g.S.winner);Assert.AreEqual(1,g.S.moves);
            Assert.False(g.Roll(1,20160601,0));Assert.False(g.ChoosePurchase(false));
        }
        [Test] public void DatedChanceCardsCannotAppearBeforeTheirEvents()
        {
            CollectionAssert.AreEqual(new[]{2,3,4,5,6},MonopolyGame.EligibleChance(20160601));
            Assert.False(MonopolyGame.EligibleChance(20160820).Contains(7));Assert.True(MonopolyGame.EligibleChance(20160821).Contains(7));
            Assert.False(MonopolyGame.EligibleChance(20160915).Contains(1));Assert.True(MonopolyGame.EligibleChance(20160916).Contains(1));
            Assert.False(MonopolyGame.EligibleChance(20161110).Contains(0));Assert.True(MonopolyGame.EligibleChance(20161111).Contains(0));
        }
        [Test] public void ChanceMovementResolvesTheDestinationPropertyWithoutExtraTurn()
        {
            var g=new MonopolyGame();int pick=MonopolyGame.EligibleChance(20160821).IndexOf(7);
            Assert.True(g.Roll(2,20160821,pick));Assert.AreEqual(5,g.S.position[0]);Assert.AreEqual(5,g.S.pending);Assert.AreEqual(0,g.S.turns[0]);Assert.AreEqual(7,g.LastChance);
            Assert.True(g.ChoosePurchase(false));Assert.AreEqual(1,g.S.turns[0]);Assert.AreEqual(1,g.S.moves);
        }
        [Test] public void EightTurnsEachUsesCashPlusPropertyAndSupportsDraw()
        {
            var state=new MonopolyGame().S;state.turn=1;state.position[1]=9;state.turns=new[]{8,7};state.moves=15;state.owners[1]=0;state.cash[0]-=MonopolyGame.Price(1);
            var g=new MonopolyGame(state);Assert.True(g.Roll(1,20160601,0));Assert.AreEqual(16,g.S.moves);Assert.AreEqual(2,g.S.winner);
            Assert.AreEqual(1500,g.Worth(0));Assert.AreEqual(1500,g.Worth(1));
        }
        [Test] public void PlanningKeepsCashReserveAndNeverMutatesState()
        {
            var state=new MonopolyGame().S;state.cash[0]=300;var g=new MonopolyGame(state);g.Roll(1,20160601,0);
            int[] cash=g.S.cash.ToArray(),owners=g.S.owners.ToArray();string[] log=g.S.log.ToArray();
            Assert.True(g.ShouldBuy(0));Assert.False(g.ShouldBuy(3));CollectionAssert.AreEqual(cash,g.S.cash);CollectionAssert.AreEqual(owners,g.S.owners);CollectionAssert.AreEqual(log,g.S.log);
        }
        [Test] public void InvalidInputsNeverAdvanceTheRound()
        {
            var g=new MonopolyGame();Assert.False(g.Roll(0,20160601,0));Assert.False(g.Roll(7,20160601,0));Assert.False(g.Roll(1,20160601,-1));Assert.False(g.Roll(1,20160601,5));
            Assert.IsEmpty(g.S.log);Assert.AreEqual(0,g.S.moves);Assert.AreEqual(1500,g.S.cash[0]);
        }
    }
}
