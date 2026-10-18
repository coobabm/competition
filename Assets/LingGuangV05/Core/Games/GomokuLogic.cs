using System;
using System.Collections.Generic;
namespace LingGuangV05.Core.Games
{
    /// <summary>Local rule search. It receives board cells, never a puzzle answer or an accuracy percentage.</summary>
    public static class GomokuLogic
    {
        public static bool Pick(Gomoku board,int stone,int level,Random rng,int[] learned,out int x,out int y)
        {
            x=y=-1;if(board==null||board.Winner!=0||board.Full||stone<1||stone>2)return false;
            var candidates=new List<(double score,int x,int y)>();int other=3-stone;
            for(int a=0;a<Gomoku.Size;a++)for(int b=0;b<Gomoku.Size;b++)if(board[a,b]==0)
            {
                double own=board.Score(a,b,stone),rival=board.Score(a,b,other);
                double score=own*1.1+rival-(Math.Abs(a-7)+Math.Abs(b-7))*.01;
                if(level>=3&&learned!=null&&learned.Length==225)score+=Math.Max(-20,Math.Min(20,learned[b*15+a]))*.01;
                candidates.Add((score,a,b));
            }
            if(candidates.Count==0)return false;
            if(level<=0){var random=candidates[(rng??new Random()).Next(candidates.Count)];x=random.x;y=random.y;return true;}
            // Guaranteed immediate wins take precedence over an opponent's immediate threat.
            foreach(var c in candidates)if(board.Score(c.x,c.y,stone)>=100000){x=c.x;y=c.y;return true;}
            foreach(var c in candidates)if(board.Score(c.x,c.y,other)>=100000){x=c.x;y=c.y;return true;}
            candidates.Sort((a,b)=>b.score.CompareTo(a.score));var best=candidates[0];double bestValue=double.NegativeInfinity;
            int count=level>=3?Math.Min(8,candidates.Count):1;
            for(int i=0;i<count;i++)
            {
                var c=candidates[i];double value=c.score;
                if(level>=3)
                {
                    var after=board.Copy();after.Place(c.x,c.y,stone);
                    // Explore an actual adversarial reply then our next best continuation.
                    if(after.Best(other,out int rx,out int ry))
                    {
                        double threat=after.Score(rx,ry,other);after.Place(rx,ry,other);
                        if(after.Winner==other)value=-1000000;
                        else if(after.Best(stone,out int nx,out int ny))value+=after.Score(nx,ny,stone)*.65-threat*.7;
                    }
                }
                if(value>bestValue){bestValue=value;best=c;}
            }
            x=best.x;y=best.y;return true;
        }
        public static string Point(int x,int y)=>(char)('A'+x)+(y+1).ToString();
        public static bool ReadPoint(string text,out int x,out int y)
        {x=y=-1;if(string.IsNullOrEmpty(text)||text.Length<2)return false;x=text[0]-'A';if(!int.TryParse(text.Substring(1),out int row))return false;y=row-1;return x>=0&&x<15&&y>=0&&y<15;}
        public static Gomoku Replay(IList<string> moves)
        {var board=new Gomoku();if(moves==null)return board;for(int i=0;i<moves.Count;i++){if(!ReadPoint(moves[i],out int x,out int y)||!board.Place(x,y,i%2+1))return null;}return board;}
    }
}
