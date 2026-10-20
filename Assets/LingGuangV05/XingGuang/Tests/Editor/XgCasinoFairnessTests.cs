using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Pure draw/paytable checks, not the scam site's lifetime return or playable flow.
    /// CasinoScamTests exercise the real freeze/fee path; no product behavior is disabled here.</summary>
    public sealed class XgCasinoFairnessTests
    {
        const int Rounds=200000;
        const long Stake=100;
        static Func<int,int> Draw(long seed)
        {
            var sim=new XgSim();sim.Casino.rng=seed;
            // Existing draw primitive directly: no fake host or test-mode product switch.
            return (Func<int,int>)Delegate.CreateDelegate(typeof(Func<int,int>),sim,
                typeof(XgSim).GetMethod("CasinoNext",BindingFlags.Instance|BindingFlags.NonPublic));
        }
        static IEnumerable<object[]> Games()
        {
            yield return new object[]{"slots",215.0/216,96.0/216};
            yield return new object[]{"dice",213.0/216,105.0/216};
            yield return new object[]{"roulette",36.5/37,18.0/37};
            yield return new object[]{"dragon",165.75/169,78.0/169};
        }
        [TestCaseSource(nameof(Games))]
        public void RawDrawAndPaytableReturnMatchesTheoreticalRtp(string game,double rtp,double winRate)
        {
            foreach(int choice in new[]{0,1})
            {
                if(game=="slots"&&choice==1)continue;
                var next=Draw(20161005+choice);int wins=0,losses=0;long returned=0;
                for(int i=0;i<Rounds;i++)
                {
                    int[] values=new int[game=="roulette"?1:game=="dragon"?2:3];
                    for(int j=0;j<values.Length;j++)values[j]=next(game=="roulette"?37:game=="dragon"?13:6)+(game=="dice"||game=="dragon"?1:0);
                    long payout=CasinoRules.Payout(Stake,CasinoRules.ReturnQuarters(game,choice,values));
                    returned+=payout;if(payout>Stake)wins++;else if(payout==0)losses++;
                }
                Assert.AreEqual(rtp,returned/(double)(Rounds*Stake),.02);
                Assert.AreEqual(winRate,wins/(double)Rounds,.01);Assert.Greater(losses,0);
            }
        }
        [Test]
        public void DrawPrimitiveIsIndependentOfResultHistory()
        {
            var a=new XgSim();var b=new XgSim();a.Casino.rng=b.Casino.rng=777;
            var method=typeof(XgSim).GetMethod("CasinoNext",BindingFlags.Instance|BindingFlags.NonPublic);
            var left=(Func<int,int>)Delegate.CreateDelegate(typeof(Func<int,int>),a,method);
            var right=(Func<int,int>)Delegate.CreateDelegate(typeof(Func<int,int>),b,method);
            for(int i=0;i<2000;i++){Assert.AreEqual(left(37),right(37));b.Casino.returned+=100000;b.Casino.history.Add(new XgCasinoRound{returned=100000,stake=1});}
        }
        [Test]
        public void EveryRouletteNumberAndDragonCardComesUpAtItsFairFrequency()
        {
            var next=Draw(31337);var pockets=new int[37];var cards=new int[14];const int n=111000;
            for(int i=0;i<n;i++){pockets[next(37)]++;if(i<78000){cards[next(13)+1]++;cards[next(13)+1]++;}}
            for(int k=0;k<37;k++)Assert.AreEqual(n/37.0,pockets[k],n/37.0*.08);
            for(int k=1;k<=13;k++)Assert.AreEqual(78000*2/13.0,cards[k],78000*2/13.0*.05);
        }
        static int Hard(IList<int> hand)
        {
            int total = 0;
            foreach (int card in hand) total += Math.Min(BlackjackRules.Rank(card), 10);
            return total;
        }

        /// <summary>"hit", "stand" or "double" from the player's hand and the dealer's up card (dealer stands on all 17s).</summary>
        static string BasicStrategy(IList<int> player, int upRank)
        {
            int up = upRank == 1 ? 11 : Math.Min(upRank, 10);
            int score = BlackjackRules.Score(player), hard = Hard(player);
            bool soft = score != hard, two = player.Count == 2;
            if (soft)
            {
                if (score >= 19) return "stand";
                if (score == 18) return up >= 3 && up <= 6 && two ? "double" : up == 2 || up == 7 || up == 8 ? "stand" : "hit";
                if (score == 17) return two && up >= 3 && up <= 6 ? "double" : "hit";
                if (score >= 15) return two && up >= 4 && up <= 6 ? "double" : "hit";
                return two && up >= 5 && up <= 6 ? "double" : "hit";
            }
            if (score >= 17) return "stand";
            if (score >= 13) return up <= 6 ? "stand" : "hit";
            if (score == 12) return up >= 4 && up <= 6 ? "stand" : "hit";
            if (score == 11) return two && up <= 10 ? "double" : "hit";
            if (score == 10) return two && up <= 9 ? "double" : "hit";
            if (score == 9) return two && up >= 3 && up <= 6 ? "double" : "hit";
            return "hit";
        }


        [Test]
        public void RawBlackjackRulesWithBasicStrategyReturnAboutNinetyNinePointFivePercent()
        {
            var next=Draw(21212121);long wagered=0,returned=0;int naturals=0,wins=0;
            for(int i=0;i<Rounds;i++)
            {
                var deck=BlackjackRules.Shuffle(next);int pos=4;long stake=Stake;
                var player=new List<int>{deck[0],deck[2]};var dealer=new List<int>{deck[1],deck[3]};
                if(!BlackjackRules.Natural(player)&&!BlackjackRules.Natural(dealer))
                {
                    while(true)
                    {
                        string action=BasicStrategy(player,BlackjackRules.Rank(dealer[0]));
                        if(action=="double")stake*=2;
                        if(action!="stand")player.Add(deck[pos++]);
                        int score=BlackjackRules.Score(player);
                        if(score>21)break;
                        if(action!="hit"||score==21){while(BlackjackRules.Score(dealer)<17)dealer.Add(deck[pos++]);break;}
                    }
                }
                long payout=BlackjackRules.Returned(player,dealer,stake);wagered+=stake;returned+=payout;
                if(payout>stake)wins++;if(BlackjackRules.Natural(player))naturals++;
                Assert.AreEqual(52,new HashSet<int>(deck).Count);
                if(dealer.Count>2)Assert.Less(BlackjackRules.Score(dealer.GetRange(0,dealer.Count-1)),17);
            }
            Assert.AreEqual(.995,returned/(double)wagered,.02);
            Assert.AreEqual(.0483,naturals/(double)Rounds,.005);Assert.Greater(wins/(double)Rounds,.4);
        }
    }
}
