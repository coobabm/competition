using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgBlackjackTests
    {
        sealed class Host : IXgHost
        {
            public double money=1000; public int debits,credits;
            public double Money=>money; public double Compute=>1; public double VramMB=>1024; public string Blocker=>null;
            public bool Spend(double n){if(money<n)return false; money-=n;debits++;return true;}
            public void Earn(double n){money+=n;credits++;} public void Train(double n){}
        }
        [Test] public void AcesDowngradeOneAtATimeAndFacesAreTen()
        {
            Assert.AreEqual(21,BlackjackRules.Score(new[]{0,13,8})); // A,A,9: 11+1+9
            Assert.AreEqual(12,BlackjackRules.Score(new[]{0,13,9}));
            Assert.AreEqual(20,BlackjackRules.Score(new[]{10,12}));
            Assert.AreEqual(17,BlackjackRules.Score(new[]{0,5}));
        }
        [Test] public void NaturalPaysThreeToTwoButPushReturnsOnlyStake()
        {
            Assert.AreEqual(125,BlackjackRules.Returned(new[]{0,12},new[]{9,7},50));
            Assert.AreEqual(50,BlackjackRules.Returned(new[]{0,12},new[]{13,22},50));
            Assert.AreEqual(0,BlackjackRules.Returned(new[]{9,4,5},new[]{0,12},50));
            Assert.AreEqual(100,BlackjackRules.Returned(new[]{9,8},new[]{9,7},50));
            Assert.AreEqual(0,BlackjackRules.Returned(new[]{9,8,2},new[]{9,7},50));
        }
        [Test] public void StartShufflesAFullDeckAndCannotRedealOrPlayOtherGames()
        {
            var sim=new XgSim(); var host=new Host();
            sim.Casino.rng=12345;
            Assert.IsTrue(sim.StartBlackjack(50,host));
            Assert.IsTrue(sim.BlackjackActive);
            Assert.AreEqual(52,sim.Casino.blackjack.deck.Distinct().Count());
            Assert.AreEqual(4,sim.Casino.blackjack.position);
            Assert.IsFalse(sim.StartBlackjack(50,host));
            Assert.IsFalse(sim.TryCasinoBet("slots",0,50,host,out _));
            Assert.AreEqual(1,host.debits);
        }
        static XgSim Hand(int[] player,int[] dealer)
        {
            var sim=new XgSim(){Clock=100};
            sim.Casino.wagered=50;
            sim.Casino.blackjack=new XgBlackjackHand {active=true,stake=50,player=new List<int>(player),dealer=new List<int>(dealer),deck=Enumerable.Range(0,52).ToArray(),position=4};
            return sim;
        }
        [Test] public void DealerStandsOnSoftSeventeenAndSettlementIsIdempotent()
        {
            var sim=Hand(new[]{9,7},new[]{0,5});var host=new Host{money=950};
            Assert.IsTrue(sim.PlayBlackjack("stand",host));
            Assert.AreEqual(4,sim.Casino.blackjack.position);
            Assert.AreEqual(1050,host.money);Assert.AreEqual(1,host.credits);
            Assert.IsFalse(sim.PlayBlackjack("stand",host));Assert.IsFalse(sim.PlayBlackjack("hit",host));
            Assert.AreEqual(1,sim.Casino.rounds);
        }
        [Test] public void DoubleDebitsExactlyOneExtraStakeAndDrawsOnlyOnce()
        {
            var sim=Hand(new[]{4,5},new[]{9,6});var host=new Host{money=950};
            sim.Casino.blackjack.deck[4]=22; // rank 10
            Assert.IsTrue(sim.PlayBlackjack("double",host));
            Assert.AreEqual(100,sim.Casino.blackjack.stake);Assert.AreEqual(3,sim.Casino.blackjack.player.Count);
            Assert.AreEqual(1100,host.money);Assert.AreEqual(100,sim.Casino.wagered);Assert.AreEqual(200,sim.Casino.returned);
            Assert.IsFalse(sim.PlayBlackjack("double",host));Assert.AreEqual(1,host.debits);
        }
        [Test] public void UnaffordableDoubleDoesNotMutateHandOrDeck()
        {
            var sim=Hand(new[]{4,5},new[]{9,6});var host=new Host{money=49};
            Assert.IsFalse(sim.PlayBlackjack("double",host));
            Assert.AreEqual(4,sim.Casino.blackjack.position);Assert.AreEqual(50,sim.Casino.blackjack.stake);
            Assert.AreEqual(49,host.money);Assert.AreEqual(0,host.debits);
        }
        [Test] public void BustLosesAndHitCannotBeDuplicatedInSameFrame()
        {
            var sim=Hand(new[]{1,2},new[]{9,6});var host=new Host{money=950};
            Assert.IsTrue(sim.PlayBlackjack("hit",host));Assert.IsFalse(sim.PlayBlackjack("hit",host));
            Assert.AreEqual(3,sim.Casino.blackjack.player.Count);
            sim=Hand(new[]{9,8},new[]{9,6});sim.Casino.blackjack.deck[4]=5;
            Assert.IsTrue(sim.PlayBlackjack("hit",host));Assert.IsFalse(sim.Casino.blackjack.active);
            Assert.AreEqual(0,sim.Casino.history.Last().returned);Assert.AreEqual(950,host.money);
        }
        [Test] public void PendingHandSurvivesActualJsonRoundTrip()
        {
            var sim=Hand(new[]{4,5},new[]{9,6}); var host=new Host{money=950};
#if UNITY_5_3_OR_NEWER
            var copy=UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S));
#else
            var options=new System.Text.Json.JsonSerializerOptions{IncludeFields=true};
            var copy=System.Text.Json.JsonSerializer.Deserialize<XgState>(System.Text.Json.JsonSerializer.Serialize(sim.S,options),options);
#endif
            var restored=new XgSim(copy){Clock=100};Assert.IsTrue(restored.Casino.blackjack.active);
            Assert.IsFalse(restored.StartBlackjack(50,host));
            Assert.IsTrue(restored.PlayBlackjack("stand",host));Assert.AreEqual(950,host.money);
            Assert.IsFalse(restored.PlayBlackjack("stand",host));
        }
    }
}
