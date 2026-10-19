using System;
using System.Collections.Generic;
using System.Text;
namespace LingGuangV05.Core.Games
{
    /// <summary>Bounded KQK mate-in-one composition. Every emitted puzzle is solved with the legal chess engine.</summary>
    public sealed class ChessComposer
    {
        readonly int[] candidates=new int[64];readonly int symmetry;
        readonly List<ChessMove> solutions=new List<ChessMove>();
        public string Fen {get;private set;}
        public int Attempts {get;private set;}
        public bool Finished=>Fen!=null||Attempts>=64;
        public IReadOnlyList<ChessMove> Solutions=>solutions;
        public ChessComposer(int seed)
        {
            var random=new Random(seed);symmetry=random.Next(8);
            for(int i=0;i<64;i++)candidates[i]=i;
            for(int i=63;i>0;i--){int j=random.Next(i+1),old=candidates[i];candidates[i]=candidates[j];candidates[j]=old;}
        }
        int Transform(int square)
        {
            int x=square%8,y=square/8;if(symmetry>=4)x=7-x;
            for(int i=0;i<symmetry%4;i++){int previous=x;x=7-y;y=previous;}return y*8+x;
        }
        /// <summary>Tests at most one candidate per call. Invalid and unsolved positions are never published.</summary>
        public void Step()
        {
            if(Finished)return;
            int queen=Transform(candidates[Attempts++]),whiteKing=Transform(45),blackKing=Transform(63);
            if(queen==whiteKing||queen==blackKing)return;
            var cells=new char[64];for(int i=0;i<64;i++)cells[i]='.';cells[queen]='Q';cells[whiteKing]='K';cells[blackKing]='k';
            string position=Position(cells);var board=new ChessGame(position+" w - - 0 1");
            // The non-moving king may not already be in check; neither king may be adjacent.
            if(board.InCheck||board.Result!=ChessGame.Ongoing||new ChessGame(position+" b - - 0 1").InCheck)return;
            var found=new List<ChessMove>();
            foreach(var move in board.LegalMoves())
            {
                var reply=new ChessGame(board.ToFen());
                if(reply.Move(move)&&reply.Result==ChessGame.WhiteWins&&reply.ResultReasonEnglish=="Checkmate")found.Add(move);
            }
            if(found.Count==0)return;
            Fen=board.ToFen();solutions.AddRange(found);
        }
        static string Position(char[] cells)
        {
            var fen=new StringBuilder();
            for(int rank=7;rank>=0;rank--)
            {
                if(rank<7)fen.Append('/');int empty=0;
                for(int file=0;file<8;file++)
                {char piece=cells[rank*8+file];if(piece=='.'){empty++;continue;}if(empty>0){fen.Append(empty);empty=0;}fen.Append(piece);}
                if(empty>0)fen.Append(empty);
            }
            return fen.ToString();
        }
    }
}
