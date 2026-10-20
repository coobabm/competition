using System;
using System.Collections.Generic;
namespace LingGuangV05.Core.Games
{
    public readonly struct XiangqiRookMove
    {
        public readonly int From,To;
        public XiangqiRookMove(int from,int to){From=from;To=to;}
    }
    /// <summary>Explicitly restricted 9x10 generals-and-one-red-rook engine, not full Xiangqi.</summary>
    public sealed class XiangqiRookEndgame
    {
        // rank 0 is Red's back rank. +1 Red general, -1 Black general, +2 Red rook.
        readonly int[] cells;
        public int Turn {get;private set;}
        public int this[int square]=>cells[square];
        public XiangqiRookEndgame(int[] position,int turn=1)
        {
            if(position==null||position.Length!=90||turn!=1&&turn!=-1)throw new ArgumentException("Invalid restricted Xiangqi position");
            cells=(int[])position.Clone();Turn=turn;int red=0,black=0,rook=0;
            for(int i=0;i<90;i++)
            {
                int p=cells[i];if(p==1){red++;if(!Palace(i,1))throw new ArgumentException("Red general outside palace");}
                else if(p==-1){black++;if(!Palace(i,-1))throw new ArgumentException("Black general outside palace");}
                else if(p==2)rook++;else if(p!=0)throw new ArgumentException("Only generals and one red rook are supported");
            }
            if(red!=1||black!=1||rook>1)throw new ArgumentException("Restricted endgame needs exactly two generals and at most one red rook");
        }
        public int[] Position()=>(int[])cells.Clone();
        public XiangqiRookEndgame Copy()=>new XiangqiRookEndgame(cells,Turn);
        static bool Palace(int at,int side)=>at%9>=3&&at%9<=5&&(side==1?at/9<=2:at/9>=7);
        bool ClearLine(int from,int to)
        {
            if(from==to||from%9!=to%9&&from/9!=to/9)return false;
            int step=from%9==to%9?(to>from?9:-9):(to>from?1:-1);
            for(int at=from+step;at!=to;at+=step)if(cells[at]!=0)return false;return true;
        }
        public bool InCheck(int side)
        {
            if(side!=1&&side!=-1)throw new ArgumentOutOfRangeException(nameof(side));
            int king=Array.IndexOf(cells,side),enemy=Array.IndexOf(cells,-side);
            // Flying-generals rule: an unobstructed same file is illegal for either general.
            if(king%9==enemy%9&&ClearLine(king,enemy))return true;
            if(side==-1){int rook=Array.IndexOf(cells,2);if(rook>=0&&ClearLine(rook,king))return true;}
            return false;
        }
        bool Pseudo(int from,int to)
        {
            if(from<0||from>=90||to<0||to>=90||from==to)return false;
            int piece=cells[from],target=cells[to];if(piece==0||Math.Sign(piece)!=Turn||target!=0&&Math.Sign(target)==Turn||Math.Abs(target)==1)return false;
            if(Math.Abs(piece)==1)return Palace(to,Turn)&&Math.Abs(from%9-to%9)+Math.Abs(from/9-to/9)==1;
            return piece==2&&ClearLine(from,to);
        }
        public List<XiangqiRookMove> LegalMoves()
        {
            var moves=new List<XiangqiRookMove>();
            for(int from=0;from<90;from++)if(cells[from]!=0&&Math.Sign(cells[from])==Turn)
                for(int to=0;to<90;to++)
                {
                    if(!Pseudo(from,to))continue;int piece=cells[from],capture=cells[to];cells[from]=0;cells[to]=piece;
                    bool legal=!InCheck(Turn);cells[from]=piece;cells[to]=capture;if(legal)moves.Add(new XiangqiRookMove(from,to));
                }
            return moves;
        }
        public bool Move(XiangqiRookMove move)
        {
            foreach(var legal in LegalMoves())if(legal.From==move.From&&legal.To==move.To)
            {cells[move.To]=cells[move.From];cells[move.From]=0;Turn=-Turn;return true;}return false;
        }
        public bool Checkmate=>InCheck(Turn)&&LegalMoves().Count==0;
    }
    /// <summary>One candidate per Step, never publishing an unchecked or unsolved position.</summary>
    public sealed class XiangqiRookComposer
    {
        readonly int[] candidates=new int[90];readonly bool mirror;
        readonly List<XiangqiRookMove> solutions=new List<XiangqiRookMove>();
        int[] position;
        public int Attempts {get;private set;}
        public bool Finished=>position!=null||Attempts>=90;
        public int[] Position=>position==null?null:(int[])position.Clone();
        public IReadOnlyList<XiangqiRookMove> Solutions=>solutions;
        public XiangqiRookComposer(int seed)
        {
            var random=new Random(seed);mirror=random.Next(2)==1;for(int i=0;i<90;i++)candidates[i]=i;
            for(int i=89;i>0;i--){int j=random.Next(i+1),old=candidates[i];candidates[i]=candidates[j];candidates[j]=old;}
        }
        int Transform(int at)=>mirror?at/9*9+8-at%9:at;
        public void Step()
        {
            if(Finished)return;int rook=Transform(candidates[Attempts++]),red=4,black=Transform(84);
            if(rook==red||rook==black)return;var cells=new int[90];cells[red]=1;cells[black]=-1;cells[rook]=2;
            var board=new XiangqiRookEndgame(cells);if(board.InCheck(1)||board.InCheck(-1))return;
            var verified=new List<XiangqiRookMove>();
            foreach(var move in board.LegalMoves())
            {var after=board.Copy();if(after.Move(move)&&after.Turn==-1&&after.Checkmate)verified.Add(move);}
            if(verified.Count==0)return;position=cells;solutions.AddRange(verified);
        }
    }
}
