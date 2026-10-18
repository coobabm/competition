#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;
public static partial class TeachingFullRoundChecks
{
    static void Complete(object game,bool correct)
    {
        string id=Packs[index].id;var surface=Surface();
        if(id.EndsWith("_swipe")){var phrase=(XgPhrase)Field(game,"phrase");Press(surface,phrase.yes==correct?"Left":"Right");return;}
        if(id=="minesweeper_yn"){var c=(XgCard)Field(game,"QuestionCard");bool mine=c.teachingImageKeys[c.teachingImageKey]==1;Press(surface,mine==correct?"Mine":"Safe");return;}
        if(id=="meme_word"&&surface.GetComponentInChildren<TMPro.TMP_InputField>()!=null){var input=surface.GetComponentInChildren<TMPro.TMP_InputField>();int mood=(int)Field(game,"mood");input.text=correct?XgTeachingMoods.English[mood]:"computer";Press(surface,"SubmitCaption");return;}
        if(id=="make24"){Complete24(game,correct);return;}
        if(id=="captcha")
        {
            if((bool)Field(game,"textCaptcha")){string code=(string)Field(game,"captchaCode");if(!correct)code=(code[0]=='A'?"B":"A")+code.Substring(1);foreach(char c in code)Press(surface,"Type"+c);Press(surface,"SubmitCode");}
            else {int[] items=(int[])Field(game,"items");int target=(int)Field(game,"target");for(int i=0;i<items.Length;i++)if(items[i]==target)Press(surface,"Cell"+i);if(!correct)Press(surface,"Cell0");Press(surface,"Submit");}return;
        }
        if(id=="gomoku_win")
        {
            var board=(int[])Field(game,"board");for(int k=0;k<81;k++)if(board[k]==0&&GomokuWins(board,k)==correct){Press(surface,"Point"+k);return;}throw new Exception("No gomoku fixture choice");
        }
        if(id=="chess_mate1"||id=="chess_mate2"){CompleteChess(game,correct);return;}
        if(id=="arith_mole")
        {
            for(int tick=0;tick<300;tick++){
                ((XgMiniGame)game).Tick(.05f);var expires=(float[])Field(game,"expires");var wrong=(bool[])Field(game,"wrong");
                for(int i=0;i<9;i++)if(expires[i]>0&&wrong[i]!=correct){var holes=(Array)Field(game,"holes");var h=holes.GetValue(i);((Action)h.GetType().GetField("Down",Flags).GetValue(h)).Invoke();((XgMiniGame)game).Tick(61);Require(!(bool)Field(game,"playing"),"Mole session not ended");return;}
            }throw new Exception("No mole matching fixture outcome");
        }
        if(id=="spy")
        {
            Press(surface,"Specific");((XgMiniGame)game).Tick(3);var sides=(int[])Field(game,"sides");for(int i=1;i<4;i++)Press(surface,sides[i]==sides[0]?"Like"+i:"Unlike"+i);Press(surface,"GoVote");int odd=(int)Field(game,"odd");Press(surface,"Vote"+(correct?odd:(odd+1)%4));return;
        }
        if(id=="chinglish"){
            int qi=(int)Field(game,"questionIndex");var errors=(int[][])game.GetType().GetField("ErrorSpans",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            int selected=errors[qi][0];if(!correct){selected=0;while(Array.IndexOf(errors[qi],selected)>=0)selected++;}
            Press(surface,"ErrorSpan"+selected);if(!correct)return;
        }
        var keys=(Action[])Field(game,"keys");for(int i=0;i<keys.Length;i++){
            int answer=ClosureInt(keys[i].Target,"answer"),choice=ClosureInt(keys[i].Target,"truthIndex");
            if((choice==answer)==correct){Press(surface,"Choice"+i);return;}
        }
        throw new Exception("No choice for "+id);
    }
    static int ClosureInt(object value,string name,int depth=0)
    {
        if(value==null||depth>5)throw new MissingFieldException(name);
        var f=value.GetType().GetField(name,Flags);if(f!=null)return (int)f.GetValue(value);
        foreach(var field in value.GetType().GetFields(Flags))if(field.FieldType.Name.Contains("DisplayClass"))try{return ClosureInt(field.GetValue(value),name,depth+1);}catch(MissingFieldException){}
        throw new MissingFieldException(value.GetType().Name,name);
    }
    static bool GomokuWins(int[] b,int k)
    {
        int[] dx={1,0,1,1},dy={0,1,1,-1};for(int d=0;d<4;d++){int n=1;foreach(int sign in new[]{-1,1})for(int step=1;step<9;step++){int x=k%9+dx[d]*step*sign,y=k/9+dy[d]*step*sign;if(x<0||x>=9||y<0||y>=9||b[y*9+x]!=1)break;n++;}if(n>=5)return true;}return false;
    }
    static bool ForceMate(ChessGame board,ChessMove move,int turns)
    {
        int student=board.SideToMove;var after=new ChessGame(board.ToFen());after.Move(move);
        if(after.Result==student&&after.ResultReasonEnglish=="Checkmate")return true;if(turns==1||after.Result!=ChessGame.Ongoing)return false;
        foreach(var defence in after.LegalMoves()){var b=new ChessGame(after.ToFen());b.Move(defence);bool possible=false;foreach(var end in b.LegalMoves())if(ForceMate(b,end,1)){possible=true;break;}if(!possible)return false;}return true;
    }
    static void CompleteChess(object game,bool correct)
    {
        var board=(ChessGame)Field(game,"board");int max=(int)Field(game,"maxMoves");var chosen=ChessMove.None;
        foreach(var move in board.LegalMoves())if(ForceMate(board,move,max)==correct){chosen=move;break;}
        Require(chosen.IsValid,"No desired chess continuation");ChessClick(game,chosen);
        if((bool)Field(game,"submitted"))return;
        board=(ChessGame)Field(game,"board");foreach(var move in board.LegalMoves())if(ForceMate(board,move,1)==correct){ChessClick(game,move);return;}
        throw new Exception("No chess final choice");
    }
    static void ChessClick(object game,ChessMove move)
    {
        Press(Surface(),"Square"+move.From);Press(Surface(),"Square"+move.To);
        if((bool)Field(game,"promotionPending")){
            var choices=new List<ChessMove>();var board=(ChessGame)Field(game,"board");foreach(var m in board.LegalMoves())if(m.From==move.From&&m.To==move.To)choices.Add(m);
            for(int i=0;i<choices.Count;i++)if(choices[i].Promotion==move.Promotion){Press(Surface(),"Promote"+i);break;}
        }
    }
    static void Complete24(object game,bool correct)
    {
        bool possible=(bool)Field(game,"possible");if(!possible){Press(Surface(),correct?"No":"Yes");return;}
        if(!correct){Press(Surface(),"No");return;}
        Press(Surface(),"Yes");var values=(List<double>)Field(game,"values");var plan=new List<int[]>();Require(Plan24(new List<double>(values),plan),"Solver says possible but cannot build");
        foreach(var step in plan){Press(Surface(),"Number"+step[0]);Press(Surface(),"Number"+step[1]);Press(Surface(),"Op"+step[2]);}
    }
    static bool Plan24(List<double> values,List<int[]> plan)
    {
        if(values.Count==1)return Math.Abs(values[0]-24)<1e-6;
        for(int a=0;a<values.Count;a++)for(int b=0;b<values.Count;b++)if(a!=b)for(int op=0;op<4;op++){
            if(op==3&&Math.Abs(values[b])<1e-9)continue;double v=op==0?values[a]+values[b]:op==1?values[a]-values[b]:op==2?values[a]*values[b]:values[a]/values[b];
            var next=new List<double>();for(int i=0;i<values.Count;i++)if(i!=a&&i!=b)next.Add(values[i]);next.Add(v);plan.Add(new[]{a,b,op});if(Plan24(next,plan))return true;plan.RemoveAt(plan.Count-1);
        }return false;
    }
}
#endif
