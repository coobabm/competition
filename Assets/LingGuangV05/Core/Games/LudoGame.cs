using System;
using System.Collections.Generic;
namespace LingGuangV05.Core.Games
{
    [Serializable] public sealed class LudoState
    {
        // -1 base, 0 launch, 1..27 loop, 28 home. The second player's launch is opposite.
        public int[] planes = { -1, -1, -1, -1 };
        // An engine constructed this state; inline JSON placeholders only run field initializers.
        public bool initialized;
        public int turn, die, rolls, actions, winner = -1;
        public List<string> log = new List<string>();
    }
    /// <summary>Authored short aeroplane chess: 28 cells, two players, two planes, takeoff on 5/6.</summary>
    public sealed class LudoGame
    {
        public const int Loop = 28, Home = 28;
        public readonly LudoState S;
        public bool Over => S.winner >= 0;
        public LudoGame(LudoState state = null)
        {
            S = state ?? new LudoState();
            if(S.planes==null||S.planes.Length!=4||S.turn<0||S.turn>1||S.die<0||S.die>6||S.winner < -1||S.winner>1)
                throw new ArgumentException("Invalid aeroplane game state");
            foreach(int p in S.planes)if(p < -1||p>Home)throw new ArgumentException("Invalid plane position");
            if(S.log==null)S.log=new List<string>();
            S.initialized=true;
        }
        public int Position(int player,int plane)=>S.planes[player*2+plane];
        public int Global(int player,int progress)=>progress<0||progress>=Home?-1:(player*14+progress)%Loop;
        public int AtHome(int player)=>(Position(player,0)==Home?1:0)+(Position(player,1)==Home?1:0);
        // One colour jump only. It never skips into the home or chains indefinitely.
        public int Landing(int player,int progress,int die)
        {
            if(player<0||player>1||progress < -1||progress>=Home||die<1||die>6)return -2;
            if(progress<0)return die>=5?0:-2;
            int result=progress+die;if(result>Home)result=Home-(result-Home);
            if(result>0&&result<Home&&Global(player,result)%4==player*2&&result+4<Home)result+=4;
            return result;
        }
        public List<int> LegalPlanes()
        {
            var legal=new List<int>();if(Over||S.die==0)return legal;
            for(int p=0;p<2;p++)if(Landing(S.turn,Position(S.turn,p),S.die)>=0)legal.Add(p);
            return legal;
        }
        /// <summary>The caller draws one fair die; this method never rerolls for an opponent.</summary>
        public bool Roll(int die)
        {
            if(Over||S.die!=0||die<1||die>6)return false;
            S.die=die;S.rolls++;S.log.Add("roll:"+S.turn+":"+die);
            if(LegalPlanes().Count==0){S.log.Add("pass:"+S.turn);EndTurn();}
            return true;
        }
        public bool Move(int plane)
        {
            if(Over||S.die==0||plane<0||plane>1)return false;
            int player=S.turn,from=Position(player,plane),to=Landing(player,from,S.die);
            if(to<0)return false;
            S.planes[player*2+plane]=to;
            int global=Global(player,to);
            if(global>=0)for(int p=0;p<2;p++)if(Global(1-player,Position(1-player,p))==global)
            {S.planes[(1-player)*2+p]=-1;S.log.Add("capture:"+player+":"+p);}
            S.log.Add("move:"+player+":"+plane+":"+from+":"+to);
            if(AtHome(player)==2){S.winner=player;S.actions++;S.die=0;S.log.Add("winner:"+player);}
            else EndTurn();
            return true;
        }
        void EndTurn(){int die=S.die;S.actions++;S.die=0;if(die!=6)S.turn=1-S.turn;}
        public int PickPlane(int level,Random random,int learnedStyle=1)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));
            var legal=LegalPlanes();if(legal.Count==0)return -1;
            if(level<=0)return legal[random.Next(legal.Count)];
            int best=legal[0];double bestScore=double.NegativeInfinity;
            foreach(int plane in legal)
            {
                int from=Position(S.turn,plane),to=Landing(S.turn,from,S.die),global=Global(S.turn,to);
                double score=to==Home?100:to-from;
                for(int p=0;p<2;p++)if(global>=0&&Global(1-S.turn,Position(1-S.turn,p))==global)score+=50;
                if(from<0)score+=5;
                if(level>=3&&global>=0)
                    for(int die=1;die<=6;die++)for(int p=0;p<2;p++)
                    {
                        int enemy=Landing(1-S.turn,Position(1-S.turn,p),die);
                        if(enemy>=0&&Global(1-S.turn,enemy)==global)score-=level>=4?(learnedStyle==0?4:learnedStyle==2?12:8):8;
                    }
                if(score>bestScore){bestScore=score;best=plane;}
            }
            return best;
        }
    }
}
