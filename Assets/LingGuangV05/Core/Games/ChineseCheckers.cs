using System;
using System.Collections.Generic;
namespace LingGuangV05.Core.Games
{
    public readonly struct CheckersHole
    {
        public readonly int q,r;
        public CheckersHole(int q,int r){this.q=q;this.r=r;}
    }
    public sealed class CheckersMove
    {
        public int piece,destination;
        public readonly List<int> path=new List<int>();
        public bool Jump=>path.Count>2;
    }
    [Serializable] public sealed class ChineseCheckersState
    {
        public int[] pieces;
        public int turn,moves,winner=-1;
        public int[] turns={0,0};
        public List<string> log=new List<string>();
    }
    /// <summary>A 73-hole three-row star, two opposing six-piece camps; at most 40 turns each.</summary>
    public sealed class ChineseCheckers
    {
        public static readonly CheckersHole[] Holes=BuildHoles();
        static readonly int[,] Directions={{1,0},{1,-1},{0,-1},{-1,0},{-1,1},{0,1}};
        public readonly ChineseCheckersState S;
        public bool Over=>S.winner>=0;
        public ChineseCheckers(ChineseCheckersState state=null)
        {
            S=state??new ChineseCheckersState();
            if(S.pieces==null)
            {
                S.pieces=new int[12];int a=0,b=6;
                for(int i=0;i<Holes.Length;i++){if(Holes[i].r < -3)S.pieces[a++]=i;if(Holes[i].r>3)S.pieces[b++]=i;}
            }
            if(S.pieces.Length!=12||S.turns==null||S.turns.Length!=2||S.turn<0||S.turn>1||S.winner < -1||S.winner>2)throw new ArgumentException("Invalid checkers state");
            var occupied=new HashSet<int>();foreach(int p in S.pieces)if(p<0||p>=Holes.Length||!occupied.Add(p))throw new ArgumentException("Invalid checkers piece");
            if(S.log==null)S.log=new List<string>();
        }
        static CheckersHole[] BuildHoles()
        {
            var cells=new Dictionary<string,CheckersHole>();
            Action<int,int> add=(q,r)=>cells[q+":"+r]=new CheckersHole(q,r);
            for(int q=-3;q<=3;q++)for(int r=-3;r<=3;r++)if(Math.Abs(q+r)<=3)add(q,r);
            for(int row=1;row<=3;row++)for(int q=row;q<=3;q++)
            {
                int x=q,y=-3-row;
                for(int rotation=0;rotation<6;rotation++){add(x,y);int next=-y;y=x+y;x=next;}
            }
            var list=new List<CheckersHole>(cells.Values);list.Sort((a,b)=>a.r!=b.r?a.r.CompareTo(b.r):a.q.CompareTo(b.q));return list.ToArray();
        }
        public static int Hole(int q,int r){for(int i=0;i<Holes.Length;i++)if(Holes[i].q==q&&Holes[i].r==r)return i;return -1;}
        public int Occupant(int hole){for(int i=0;i<12;i++)if(S.pieces[i]==hole)return i;return -1;}
        public int HomeCount(int player){int n=0;for(int i=player*6;i<player*6+6;i++)if(Target(player,S.pieces[i]))n++;return n;}
        public static bool Target(int player,int hole)=>player==0?Holes[hole].r>3:Holes[hole].r < -3;
        public List<CheckersMove> LegalMoves(int piece)
        {
            var result=new List<CheckersMove>();if(Over||piece<0||piece>=12||piece/6!=S.turn)return result;
            int start=S.pieces[piece];var origin=Holes[start];
            for(int d=0;d<6;d++)
            {
                int next=Hole(origin.q+Directions[d,0],origin.r+Directions[d,1]);
                if(next>=0&&Occupant(next)<0){var m=new CheckersMove{piece=piece,destination=next};m.path.Add(start);m.path.Add(next);result.Add(m);}
            }
            var queue=new Queue<List<int>>();queue.Enqueue(new List<int>{start});var visited=new HashSet<int>{start};
            while(queue.Count>0)
            {
                var path=queue.Dequeue();int at=path[path.Count-1];var cell=Holes[at];
                for(int d=0;d<6;d++)
                {
                    int over=Hole(cell.q+Directions[d,0],cell.r+Directions[d,1]),land=Hole(cell.q+2*Directions[d,0],cell.r+2*Directions[d,1]);
                    if(over<0||land<0||over==start||Occupant(over)<0||Occupant(land)>=0||!visited.Add(land))continue;
                    var next=new List<int>(path){over,land};queue.Enqueue(next);
                    var m=new CheckersMove{piece=piece,destination=land};m.path.AddRange(next);result.Add(m);
                }
            }
            return result;
        }
        public List<CheckersMove> AllMoves()
        {var result=new List<CheckersMove>();for(int p=S.turn*6;p<S.turn*6+6;p++)result.AddRange(LegalMoves(p));return result;}
        public bool Move(int piece,int destination)
        {
            CheckersMove chosen=null;foreach(var m in LegalMoves(piece))if(m.destination==destination){chosen=m;break;}
            if(chosen==null)return false;
            S.pieces[piece]=destination;S.log.Add("move:"+S.turn+":"+piece+":"+string.Join(",",chosen.path));EndTurn();return true;
        }
        public bool Pass(){if(Over||AllMoves().Count!=0)return false;S.log.Add("pass:"+S.turn);EndTurn();return true;}
        void EndTurn()
        {
            int player=S.turn;S.turns[player]++;S.moves++;
            if(HomeCount(player)==6)S.winner=player;
            else if(S.turns[0]>=40&&S.turns[1]>=40)S.winner=HomeCount(0)>HomeCount(1)?0:HomeCount(1)>HomeCount(0)?1:2;
            S.turn=1-player;
        }
        public CheckersMove Pick(int level,Random random,int learnedStyle=1)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));var moves=AllMoves();if(moves.Count==0)return null;
            if(level<=0){var steps=moves.FindAll(m=>!m.Jump);return steps.Count>0?steps[random.Next(steps.Count)]:moves[random.Next(moves.Count)];}
            int player=S.turn,sign=player==0?1:-1;CheckersMove best=moves[0];double bestScore=double.NegativeInfinity;
            foreach(var m in moves)
            {
                int from=S.pieces[m.piece];double score=sign*(Holes[m.destination].r-Holes[from].r)*4+(m.Jump?2:0);
                if(Target(player,m.destination))score+=20;if(Target(player,from)&&!Target(player,m.destination))score-=40;
                if(level>=3)
                {
                    // Evaluate the team's next reachable endpoints on the resulting actual board.
                    S.pieces[m.piece]=m.destination;double reach=0;
                    for(int p=player*6;p<player*6+6;p++)foreach(var next in LegalMoves(p))reach=Math.Max(reach,sign*(Holes[next.destination].r-Holes[S.pieces[p]].r));
                    S.pieces[m.piece]=from;score+=reach*(level>=4?(learnedStyle==0 ? .3 : learnedStyle==2 ? 1.2 : .6):.6);
                }
                if(score>bestScore){bestScore=score;best=m;}
            }
            return best;
        }
    }
}
