using System.Collections.Generic;
using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public class XgCasinoPresentationTests
    {
        static XgCasinoRound R(int stake,int returned)=>new XgCasinoRound{stake=stake,returned=returned};
        [Test] public void OnlyNetProfitIsAWin()
        {
            Assert.AreEqual(0,CasinoPresentation.WinTier(R(50,0)));
            Assert.AreEqual(0,CasinoPresentation.WinTier(R(50,50)));
            Assert.AreEqual(1,CasinoPresentation.WinTier(R(50,100)));
            Assert.AreEqual(2,CasinoPresentation.WinTier(R(50,1000)));
            Assert.AreEqual(0,CasinoPresentation.WinTier(null));
        }
        [Test] public void PushOrLossBreaksConsecutiveWinsWithoutChangingHistory()
        {
            var h=new List<XgCasinoRound>{R(50,0),R(50,100),R(50,100)};
            Assert.AreEqual(2,CasinoPresentation.WinStreak(h));
            h.Add(R(50,50));Assert.AreEqual(0,CasinoPresentation.WinStreak(h));
            h.Add(R(50,100));Assert.AreEqual(1,CasinoPresentation.WinStreak(h));
            Assert.AreEqual(5,h.Count);Assert.AreEqual(100,h[4].returned);
        }
        [Test] public void InitialDealAlternatesPlayerDealerAndFollowupWaitsForHoleFlip()
        {
            Assert.AreEqual(0,CasinoPresentation.DealDelay(true,0,0,0,0));
            Assert.AreEqual(.16f,CasinoPresentation.DealDelay(false,0,0,0,0),.0001f);
            Assert.AreEqual(.32f,CasinoPresentation.DealDelay(true,1,0,0,0),.0001f);
            Assert.AreEqual(.48f,CasinoPresentation.DealDelay(false,1,0,0,0),.0001f);
            Assert.AreEqual(0,CasinoPresentation.DealDelay(true,2,2,2,.3f));
            Assert.AreEqual(.3f,CasinoPresentation.DealDelay(false,2,2,2,.3f),.0001f);
            Assert.AreEqual(.46f,CasinoPresentation.DealDelay(false,3,2,2,.3f),.0001f);
        }
    }
}
