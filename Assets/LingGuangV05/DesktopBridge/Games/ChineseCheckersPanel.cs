using System;
using System.Collections.Generic;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.Games
{
    public sealed class ChineseCheckersPanel : HubGame
    {
        ChineseCheckers game=new ChineseCheckers();
        readonly Image[] holes=new Image[ChineseCheckers.Holes.Length];
        readonly TMP_Text[] numbers=new TMP_Text[ChineseCheckers.Holes.Length];
        TMP_Text status,againLabel,passLabel;Button passButton;
        int selected=-1;bool counted;float aiAt=-1;
        static readonly Color Red=new Color32(204,80,82,255),Blue=new Color32(62,130,194,255),Gold=new Color32(241,185,63,255);
        public override string Id=>"checkers_cn";
        public override string Title=>T("跳棋","Chinese checkers");
        public override string Blurb=>T("走一步，或者隔子连跳。两人各 6 颗，40 回合内谁进对角多谁赢。","Step, or chain-jump over pieces. Six each; after 40 turns, most pieces in the opposite corner wins.");
        public override string Glyph=>"✦";
        public override Color Accent=>new Color32(65,150,113,255);
        public override void Build(RectTransform area)
        {
            PrologueDesk.Fill(area,new Color32(239,244,240,255));
            var board=PrologueDesk.Centered("Chinese checkers board",area,new Vector2(-180,-12),new Vector2(610,610));
            PrologueDesk.Fill(board,new Color32(248,250,246,255));
            for(int i=0;i<ChineseCheckers.Holes.Length;i++)
            {
                int index=i;var p=ChineseCheckers.Holes[i];
                var cell=PrologueDesk.Centered("Hole "+i,board,new Vector2(46*(p.q+p.r*.5f),-40*p.r),new Vector2(34,34));
                holes[i]=PrologueDesk.Fill(cell,Color.white);holes[i].sprite=PrologueDesk.Circle();
                var button=cell.gameObject.AddComponent<Button>();button.targetGraphic=holes[i];button.onClick.AddListener(()=>Click(index));
                numbers[i]=Text(PrologueDesk.Rect("Piece",cell,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"",18,Color.white,TextAlignmentOptions.Center);
            }
            Text(PrologueDesk.Centered("Red home",board,new Vector2(0,-278),new Vector2(300,30)),T("你的目标角","Your target camp"),18,Red,TextAlignmentOptions.Center);
            Text(PrologueDesk.Centered("Blue home",board,new Vector2(0,278),new Vector2(300,30)),T("对方目标角","Opponent target camp"),18,Blue,TextAlignmentOptions.Center);
            var side=PrologueDesk.Rect("Side",area,new Vector2(1,0),Vector2.one,new Vector2(-345,32),new Vector2(-28,-64));
            status=Text(PrologueDesk.Rect("Status",side,new Vector2(0,.26f),Vector2.one,Vector2.zero,Vector2.zero),"",20,new Color32(40,58,48,255));
            passButton=Button(side,"No legal move",new Vector2(0,.13f),new Vector2(1,.23f),Accent,Pass,out passLabel);
            Button(side,"Restart",new Vector2(0,0),new Vector2(1,.10f),new Color32(125,139,132,255),RestartRound,out againLabel);
            Draw();
        }
        public override void RestartRound(){AbandonRoundRecord();game=new ChineseCheckers();selected=-1;counted=false;aiAt=-1;sampleNote="";ClearComment();Draw();}
        public override void Refresh()=>Draw();
        void Click(int hole)
        {
            if(!CanInteract||game.Over||game.S.turn!=0)return;
            int piece=game.Occupant(hole);
            if(piece>=0&&piece<6){selected=selected==piece?-1:piece;Draw();return;}
            if(selected<0)return;
            bool legal=false;foreach(var m in game.LegalMoves(selected))if(m.destination==hole){legal=true;break;}
            if(!legal)return;
            if(game.S.moves==0)BeginRoundRecord();
            if(game.Move(selected,hole)){selected=-1;AfterAction();}
        }
        void Pass()
        {
            if(!CanInteract||game.Over||game.S.turn!=0||game.AllMoves().Count!=0)return;
            if(game.S.moves==0)BeginRoundRecord();if(game.Pass())AfterAction();
        }
        void AfterAction()
        {
            if(game.Over&&!counted)
            {
                counted=true;
                Finished(game.S.winner==0?XgGameOutcome.PlayerWon:game.S.winner==1?XgGameOutcome.PlayerLost:XgGameOutcome.Draw,game.S.moves,game.S.log);
            }
            aiAt=!game.Over&&game.S.turn==1?Time.unscaledTime+.7f:-1;Draw();
        }
        void Update()
        {
            if(!CanInteract||game.Over||game.S.turn!=1)return;
            if(aiAt<0){aiAt=Time.unscaledTime+.7f;return;}if(Time.unscaledTime<aiAt)return;
            var move=game.Pick(AiPlays?Lab.BoardLogicLevel:1,Rng,AiPlays?Lab.BoardSelfPolicy(Id):1);
            if(move==null)game.Pass();else game.Move(move.piece,move.destination);
            AfterAction();
        }
        void Draw()
        {
            if(status==null)return;
            var targets=new HashSet<int>();if(selected>=0)foreach(var m in game.LegalMoves(selected))targets.Add(m.destination);
            for(int i=0;i<holes.Length;i++)
            {
                int p=game.Occupant(i);numbers[i].text=p<0?"":(p%6+1).ToString();
                holes[i].color=p>=0?(p==selected?Gold:p<6?Red:Blue):targets.Contains(i)?Gold:ChineseCheckers.Holes[i].r>3?new Color32(242,210,210,255):ChineseCheckers.Holes[i].r < -3?new Color32(208,225,242,255):new Color32(215,226,217,255);
            }
            string state=game.Over?game.S.winner==2?T("平局。","Draw."):game.S.winner==0?T("你赢了！","You won!"):OpponentName+T("赢了。"," won."):
                game.S.turn==1?OpponentName+T("正在选择路线……"," is choosing a route…"):selected<0?T("点选一颗红子。","Select a red piece."):T("点亮起的目标格，可一次连跳。","Choose a highlighted destination, including a chain jump.");
            status.text="<b><size=27>"+Title+"</size></b>\n\n"+T("对手：","Opponent: ")+OpponentName+"\n\n"+state+"\n\n"+
                T("进入目标角：你 ","Pieces home: you ")+game.HomeCount(0)+"/6 · "+T("对方 ","opponent ")+game.HomeCount(1)+"/6\n"+
                T("已走回合：你 ","Turns used: you ")+game.S.turns[0]+"/40 · "+T("对方 ","opponent ")+game.S.turns[1]+"/40\n\n"+
                T("隔一颗子跳到空位；不限谁的子。\n不吃子。40回合后比进角数。","Jump over one piece into an empty hole.\nNo captures. Compare home counts after 40 turns each.")+LabLines();
            passLabel.text=T("无路可走，停一回合","No legal move: pass");againLabel.text=T("重新开局","New game");
            passButton.interactable=!game.Over&&game.S.turn==0&&game.AllMoves().Count==0;
        }
    }
}
