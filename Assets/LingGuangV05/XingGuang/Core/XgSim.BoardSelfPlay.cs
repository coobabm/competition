using System;
using System.Collections.Generic;
using LingGuangV05.Core.Games;
namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgBoardPolicy
    {
        public string game="";public int style,rounds,points;
    }
    public sealed partial class XgState
    {
        public string boardSelfGame="";public int boardSelfRotation;
        public LudoState boardSelfLudo;
        public ChineseCheckersState boardSelfCheckers;
        public MonopolyState boardSelfMonopoly;
        public List<XgBoardPolicy> boardPolicies=new List<XgBoardPolicy>();
    }
    public sealed partial class XgSim
    {
        // Unity inline serialization can expand null optional engines into empty objects.
        // Never touch the selected game, its saved random-seed inputs, or any inactive object with evidence.
        void RepairBoardSelfPlayPlaceholders()
        {
            string active=string.IsNullOrEmpty(S.boardSelfGame)?"gomoku":S.boardSelfGame;
            var ludo=S.boardSelfLudo;
            if(active!="ludo"&&ludo!=null&&!ludo.initialized&&ludo.turn==0&&ludo.die==0&&ludo.rolls==0&&ludo.actions==0&&
                ludo.winner==-1&&EmptyBoardTrace(ludo.log)&&DefaultBoardArray(ludo.planes,4,-1))S.boardSelfLudo=null;
            var checkers=S.boardSelfCheckers;
            if(active!="checkers_cn"&&checkers!=null&&checkers.turn==0&&checkers.moves==0&&
                checkers.winner==-1&&EmptyBoardTrace(checkers.log)&&EmptyBoardArray(checkers.pieces)&&DefaultBoardArray(checkers.turns,2,0))S.boardSelfCheckers=null;
            var monopoly=S.boardSelfMonopoly;
            if(active!="monopoly"&&monopoly!=null&&monopoly.turn==0&&monopoly.moves==0&&monopoly.die==0&&
                monopoly.pending==-1&&monopoly.winner==-1&&EmptyBoardTrace(monopoly.log)&&EmptyBoardArray(monopoly.owners)&&
                DefaultBoardArray(monopoly.position,2,0)&&DefaultBoardArray(monopoly.cash,2,1500)&&DefaultBoardArray(monopoly.turns,2,0))S.boardSelfMonopoly=null;
        }
        static bool EmptyBoardTrace(List<string> trace)=>trace==null||trace.Count==0;
        static bool EmptyBoardArray(int[] array)=>array==null||array.Length==0;
        static bool DefaultBoardArray(int[] array,int length,int value)
        {
            if(EmptyBoardArray(array))return true;
            if(array.Length!=length)return false;
            foreach(int cell in array)if(cell!=value)return false;
            return true;
        }
        string SelfPlayGame=>string.IsNullOrEmpty(S.boardSelfGame)?"gomoku":S.boardSelfGame;
        int SelfPlayMoves=>SelfPlayGame=="ludo"?S.boardSelfLudo?.actions??0:SelfPlayGame=="checkers_cn"?S.boardSelfCheckers?.moves??0:SelfPlayGame=="monopoly"?S.boardSelfMonopoly?.moves??0:S.boardSelfMoves?.Count??0;
        void ResetSelfPlayGame(){S.boardSelfLudo=null;S.boardSelfCheckers=null;S.boardSelfMonopoly=null;}
        static bool PolicyGame(string game)=>game=="ludo"||game=="checkers_cn"||game=="monopoly";
        int TrialPolicy(int player)=>(S.boardSelfRounds.Count+player)%3;
        /// <summary>Laplace-smoothed real win/draw records choose one of three bounded heuristics. Default retains Lv3 weights.</summary>
        public int BoardSelfPolicy(string game)
        {
            if(BoardLogicLevel<4||!PolicyGame(game)||S.boardPolicies==null)return 1;
            int best=1;double score=.5;
            foreach(var p in S.boardPolicies)
            {
                if(p==null||p.game!=game||p.style<0||p.style>2||p.rounds<=0||p.points<0||p.points>p.rounds*2)continue;
                double value=(p.points+2.0)/(p.rounds*2+4.0);
                if(value>score){score=value;best=p.style;}
            }
            return best;
        }
        void TickOtherBoardSelfPlay(int night,int today)
        {
            if(!PolicyGame(SelfPlayGame))return;
            var rng=new Random(unchecked(night*397+SelfPlayMoves*31+S.boardSelfRounds.Count*17));
            if(SelfPlayGame=="ludo")
            {
                var game=new LudoGame(S.boardSelfLudo);S.boardSelfLudo=game.S;
                if(!game.Over&&game.S.die==0)game.Roll(rng.Next(1,7));
                if(!game.Over&&game.S.die!=0)game.Move(game.PickPlane(4,rng,TrialPolicy(game.S.turn)));
                if(game.Over)CompleteOtherBoardSelfPlay(game.S.winner,game.S.actions,game.S.log);
            }
            else if(SelfPlayGame=="checkers_cn")
            {
                var game=new ChineseCheckers(S.boardSelfCheckers);S.boardSelfCheckers=game.S;
                if(!game.Over){var move=game.Pick(4,rng,TrialPolicy(game.S.turn));if(move==null)game.Pass();else game.Move(move.piece,move.destination);}
                if(game.Over)CompleteOtherBoardSelfPlay(game.S.winner,game.S.moves,game.S.log);
            }
            else
            {
                var game=new MonopolyGame(S.boardSelfMonopoly);S.boardSelfMonopoly=game.S;
                if(!game.Over&&game.S.pending<0)game.Roll(rng.Next(1,7),today,rng.Next(MonopolyGame.EligibleChance(today).Count));
                if(!game.Over&&game.S.pending>=0)game.ChoosePurchase(game.ShouldBuy(4,TrialPolicy(game.S.turn)));
                if(game.Over)CompleteOtherBoardSelfPlay(game.S.winner,game.S.moves,game.S.log);
            }
        }
        void CompleteOtherBoardSelfPlay(int winner,int moves,List<string> log)
        {
            if(S.boardRounds==null)S.boardRounds=new List<XgBoardRound>();
            var round=new XgBoardRound{game=SelfPlayGame,ticket="self-"+Guid.NewGuid().ToString("N"),selfPlay=true,recording=true,privateAi=false,
                moves=moves,outcome=(int)(winner==0?XgGameOutcome.PlayerWon:winner==1?XgGameOutcome.PlayerLost:XgGameOutcome.Draw),
                samples=Math.Min(XgBoardRecords.SampleCap(SelfPlayGame),log.Count),selfPolicy0=TrialPolicy(0),selfPolicy1=TrialPolicy(1)};
            round.log.AddRange(log);S.boardRounds.Add(round);S.boardSelfRounds.Add(round.ticket);ResetSelfPlayGame();
        }
        void LearnOtherBoardSelfPlay(XgBoardBatch batch)
        {
            if(!PolicyGame(batch.game)||S.boardRounds==null)return;
            if(S.boardPolicies==null)S.boardPolicies=new List<XgBoardPolicy>();
            foreach(var r in S.boardRounds)
            {
                if(r==null||!r.selfPlay||r.game!=batch.game||r.fileId!=batch.fileId||!batch.rounds.Contains(r.ticket))continue;
                for(int player=0;player<2;player++)
                {
                    int style=player==0?r.selfPolicy0:r.selfPolicy1;if(style<0||style>2)continue;
                    var p=S.boardPolicies.Find(x=>x!=null&&x.game==r.game&&x.style==style);
                    if(p==null){p=new XgBoardPolicy{game=r.game,style=style};S.boardPolicies.Add(p);}
                    if(p.rounds>=64){p.rounds/=2;p.points/=2;}
                    bool won=r.outcome==(int)(player==0?XgGameOutcome.PlayerWon:XgGameOutcome.PlayerLost);
                    p.rounds++;p.points+=r.outcome==(int)XgGameOutcome.Draw?1:won?2:0;
                }
            }
        }
    }
}
