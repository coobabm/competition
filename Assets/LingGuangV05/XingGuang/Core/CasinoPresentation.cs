using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    /// <summary>Read-only presentation policy: no payouts, RNG or progression changes.</summary>
    public static class CasinoPresentation
    {
        public static int WinTier(XgCasinoRound round) => round == null || round.returned <= round.stake ? 0
            : round.returned >= round.stake * 3 || round.returned - round.stake >= 500 ? 2 : 1;
        public static int WinStreak(IList<XgCasinoRound> history)
        {
            int n=0;
            if(history!=null) for(int i=history.Count-1;i>=0 && n<8;i--)
            { if(WinTier(history[i])==0)break; n++; }
            return n;
        }
        public static float DealDelay(bool player,int index,int oldPlayers,int oldDealer,float afterFlip)
        {
            if(oldPlayers==0 && oldDealer==0)return (index*2+(player?0:1))*.16f;
            return player ? (index-oldPlayers)*.16f : afterFlip+(index-oldDealer)*.16f;
        }
    }
}
