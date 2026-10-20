using System;
using System.Collections.Generic;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Games;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    [Serializable] sealed class XgMatePuzzle { public string id,fen,name,nameEn,hint,hintEn; public int mate,rating; public bool introductory; }
    [Serializable] sealed class XgMateFile { public XgMatePuzzle[] puzzles; }
    sealed class XgMatePack : XgPackQuestion
    {
        static XgMatePuzzle[] bank;
        ChessGame board;
        readonly ChessSprites sprites=new ChessSprites();
        XgMatePuzzle puzzle;
        int selected=-1, moves, student;
        bool promotionPending;
        readonly int maxMoves;
        public XgMatePack(XgMiniContext c,int mate):base(c){maxMoves=mate;}
        protected override void DrawQuestion()
        {
            if(bank==null)
            {
                var asset=Resources.Load<TextAsset>("LingGuangV05/Teaching/chess_mates_verified");
                bank=asset==null?new XgMatePuzzle[0]:JsonUtility.FromJson<XgMateFile>(asset.text).puzzles;
            }
            if(C.Sim.S.teachingChessIntroSolved==null)C.Sim.S.teachingChessIntroSolved=new List<string>();
            var solved=C.Sim.S.teachingChessIntroSolved;bool allIntro=true;
            foreach(var p in bank)if(p.introductory&&!solved.Contains(p.id))allIntro=false;
            var eligible=new List<XgMatePuzzle>();foreach(var p in bank)if(p.mate==maxMoves&&(allIntro||p.introductory))eligible.Add(p);
            eligible.Sort((a,b)=>a.introductory!=b.introductory?(a.introductory?-1:1):a.rating.CompareTo(b.rating));
            if(eligible.Count==0){Header("棋题暂不可用","Chess puzzles unavailable","缺少已验证题库，请检查游戏安装。","The verified puzzle bank is missing. Check the installation.");return;}
            int count=C.Sim.MiniGameRecord(C.Id).correct;puzzle=eligible[count%eligible.Count];
            foreach(var candidate in eligible)if(candidate.introductory&&!solved.Contains(candidate.id)){puzzle=candidate;break;}
            board=new ChessGame(puzzle.fen);student=board.SideToMove;moves=0;selected=-1;promotionPending=false;
            QuestionCard=new LingGuangV05.XingGuang.XgCard{dataset="chess",teachingImageKind="chess",teachingPosition=puzzle.fen};
            promptZh=puzzle.name;promptEn=puzzle.nameEn;SuggestMove();Paint();
        }
        void Paint()
        {
            Header(maxMoves==1?"国际象棋 · 一步杀":"国际象棋 · 两步杀",maxMoves==1?"Chess · mate in one":"Chess · mate in two",
                "先点棋子，再点目标格。只要走出将杀就算对，不限标准答案。","Select a piece, then its destination. Any legal mating continuation is accepted.");
            for(int r=0;r<8;r++)for(int f=0;f<8;f++)
            {
                int square=r*8+f;float x=30+f*40,y=92+(7-r)*40;
                var b=B("Square"+square,x,y,40,40,"",()=>Click(square));
                b.image.color=selected==square?new Color32(220,199,90,255):(f+r)%2==0?new Color32(112,145,104,255):new Color32(238,235,205,255);
                char piece=board[f,r];var sprite=sprites.Get(piece);
                if(sprite!=null){var icon=C.Box("Piece"+square,x+3,y+2,34,36,Color.white);icon.sprite=sprite;icon.preserveAspect=true;}
                else if(piece!='.')b.label.text=piece.ToString();
            }
            T("Puzzle",390,102,475,80,XgUi.T(puzzle.name,puzzle.nameEn),28);
            T("Side",390,186,475,38,student==ChessGame.White?XgUi.T("白先","White to move"):XgUi.T("黑先","Black to move"),23);
            ShowAi(390,265,475);
            T("Moves",390,230,475,38,XgUi.T("已走 "+moves+" / "+maxMoves,"Moves "+moves+" / "+maxMoves),20);
            B("Hint",390,300,180,42,XgUi.T("提示","Hint"),()=>C.Say(XgUi.T(puzzle.hint,puzzle.hintEn)),18);
            T("Credit",390,365,465,55,puzzle.introductory?XgUi.T("入门题","Introductory puzzle"):"Lichess · CC0 · "+puzzle.rating+" · "+puzzle.id,15);
        }
        void Click(int square)
        {
            if(submitted||promotionPending||board==null)return;
            char c=board[square%8,square/8];bool own=c!='.'&&(char.IsUpper(c)?ChessGame.White:ChessGame.Black)==student;
            if(own){selected=square;Paint();return;}if(selected<0)return;
            var legal=new List<ChessMove>();foreach(var m in board.LegalMoves())if(m.From==selected&&m.To==square)legal.Add(m);
            if(legal.Count==0){C.Say(XgUi.T("这一步不合法，请重新选目标。","That move is illegal. Choose another destination."));return;}
            if(legal.Count>1)
            {
                promotionPending=true;
                for(int i=0;i<legal.Count;i++){var m=legal[i];B("Promote"+i,390+i*115,275,105,48,m.Promotion.ToString().ToUpperInvariant(),()=>{promotionPending=false;Play(m);},24);}
            }
            else Play(legal[0]);
        }
        void Play(ChessMove move)
        {
            if(!board.Move(move))return;chosenText+=(chosenText.Length>0?" ":"")+move.ToString();moves++;selected=-1;
            if(board.Result==student&&board.ResultReasonEnglish=="Checkmate"){if(puzzle.introductory&&!C.Sim.S.teachingChessIntroSolved.Contains(puzzle.id))C.Sim.S.teachingChessIntroSolved.Add(puzzle.id);Submit(true,"将杀！","Checkmate!");return;}
            if(moves>=maxMoves||board.Result!=ChessGame.Ongoing){Submit(false,"这一手没能在规定步数内将杀。","That continuation did not mate within the allowed moves.");return;}
            // A mate-in-two first move must force mate against every legal defence. Find an escaping
            // defence if one exists; otherwise choose the defence with the fewest mating replies.
            ChessMove defence=ChessMove.None;int least=int.MaxValue;bool escapes=false;
            foreach(var reply in board.LegalMoves())
            {
                var after=new ChessGame(board.ToFen());after.Move(reply);int count=MatingReplies(after,student);
                if(count<least){least=count;defence=reply;}if(count==0){escapes=true;break;}
            }
            if(!defence.IsValid){Submit(false,"对手没有合法着法，但没有被将杀。","The opponent has no legal move, but is not checkmated.");return;}
            board.Move(defence);chosenText+=" ["+defence+"]";SuggestMove(!escapes);Paint();
            if(escapes)Submit(false,"对手走 "+defence+" 后，可以逃脱将杀。","After "+defence+", the opponent escapes mate.");
            else C.Say(XgUi.T("对手应了 "+defence+"，请走最后一步。","The opponent answered "+defence+". Find the final move."));
        }
        void SuggestMove(bool capture=true)
        {
            aiText="";if(!C.Sim.TeachingAiReady)return;
            if(C.Skill<.55){aiText=XgUi.T("……不知道","…not sure");if(capture)CapturePrediction(null);return;}
            var legal=new List<ChessMove>(board.LegalMoves());if(legal.Count==0)return;
            ChessMove suggestion=legal[C.Random.Next(legal.Count)];
            if(C.Random.NextDouble()<C.Skill)foreach(var move in legal)
            {
                var next=new ChessGame(board.ToFen());next.Move(move);
                bool wins=next.Result==student&&next.ResultReasonEnglish=="Checkmate";
                if(!wins&&maxMoves-moves>=2&&next.Result==ChessGame.Ongoing)
                {wins=true;foreach(var defence in next.LegalMoves()){var reply=new ChessGame(next.ToFen());reply.Move(defence);if(MatingReplies(reply,student)==0){wins=false;break;}}}
                if(wins){suggestion=move;break;}
            }
            aiText=suggestion.ToString();
            // An escaping defence is already a finished player answer. Its new preview must not
            // replace the prediction the player actually saw before making that move.
            if(capture)CapturePrediction(SuggestionWins(suggestion));
        }
        bool SuggestionWins(ChessMove suggestion)
        {
            var next=new ChessGame(board.ToFen());if(!next.Move(suggestion))return false;
            if(next.Result==student&&next.ResultReasonEnglish=="Checkmate")return true;
            if(maxMoves-moves<2||next.Result!=ChessGame.Ongoing)return false;
            foreach(var defence in next.LegalMoves()){var reply=new ChessGame(next.ToFen());reply.Move(defence);if(MatingReplies(reply,student)==0)return false;}
            return true;
        }
        internal static int MatingReplies(ChessGame position,int player)
        {
            if(position.Result!=ChessGame.Ongoing)return 0;int count=0;
            foreach(var move in position.LegalMoves())
            {
                var after=new ChessGame(position.ToFen());after.Move(move);
                if(after.Result==player&&after.ResultReasonEnglish=="Checkmate")count++;
            }
            return count;
        }
        public override void Dispose(){sprites.Dispose();base.Dispose();}
    }
}
