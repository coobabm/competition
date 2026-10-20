using System;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.Games
{
    public sealed partial class GameHubView
    {
        RectTransform xiangqiArea;TMP_Text xiangqiTitle,xiangqiHelp,xiangqiStatus,xiangqiNewLabel,xiangqiResetLabel;
        readonly Image[] xiangqiCells=new Image[90],xiangqiDiscs=new Image[90];readonly TMP_Text[] xiangqiGlyphs=new TMP_Text[90];
        XiangqiRookComposer xiangqiComposer;XiangqiRookEndgame xiangqiBoard;int[] xiangqiPosition;
        XgSim xiangqiOwner;XgState xiangqiState;int xiangqiSelected=-1;bool xiangqiWasBought,xiangqiPlayed;float xiangqiNextStep;
        Button xiangqiNewButton,xiangqiResetButton;
        bool CanXiangqi=>ComposerBought&&showing=="board.xiangqi"&&ReferenceEquals(xiangqiOwner,TiebaHub.Lab())&&ReferenceEquals(xiangqiState,xiangqiOwner?.S)&&XgBoardPictureDrag.Visible(xiangqiArea);
        void BuildXiangqiComposer()
        {
            xiangqiArea=PrologueDesk.Rect("board.xiangqi",main,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);areas["board.xiangqi"]=xiangqiArea;
            xiangqiTitle=Text(PrologueDesk.Rect("Title",xiangqiArea,new Vector2(0,1),Vector2.one,new Vector2(35,-78),new Vector2(-35,-24)),"",29,Side);
            var board=PrologueDesk.Rect("Generals and rook board",xiangqiArea,new Vector2(0,1),new Vector2(0,1),new Vector2(35,-648),new Vector2(521,-108));
            PrologueDesk.Fill(board,new Color32(226,197,150,255),false);
            for(int file=0;file<9;file++)PrologueDesk.Fill(PrologueDesk.Rect("File",board,new Vector2((file+.5f)/9,.05f),new Vector2((file+.5f)/9,.95f),new Vector2(-.7f,0),new Vector2(.7f,0)),new Color32(112,83,49,255),false);
            for(int rank=0;rank<10;rank++)PrologueDesk.Fill(PrologueDesk.Rect("Rank",board,new Vector2(.5f/9,(rank+.5f)/10),new Vector2(8.5f/9,(rank+.5f)/10),new Vector2(0,-.7f),new Vector2(0,.7f)),new Color32(112,83,49,255),false);
            foreach(float y in new[]{-189f,189f})foreach(float angle in new[]{-45f,45f})
            {
                var diagonal=PrologueDesk.Centered("Palace diagonal",board,new Vector2(0,y),new Vector2(152.74f,1.4f));
                diagonal.localEulerAngles=new Vector3(0,0,angle);PrologueDesk.Fill(diagonal,new Color32(112,83,49,255),false);
            }
            for(int rank=0;rank<10;rank++)for(int file=0;file<9;file++)
            {
                int at=rank*9+file;
                var rect=PrologueDesk.Rect("Xiangqi square "+at,board,new Vector2(file/9f,rank/10f),new Vector2((file+1)/9f,(rank+1)/10f),Vector2.zero,Vector2.zero);
                xiangqiCells[at]=PrologueDesk.Fill(rect,Color.clear);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=xiangqiCells[at];button.onClick.AddListener(()=>PlayXiangqiSquare(at));
                var disc=PrologueDesk.Rect("Piece",rect,Vector2.zero,Vector2.one,new Vector2(5,5),new Vector2(-5,-5));
                xiangqiDiscs[at]=PrologueDesk.Fill(disc,new Color32(249,225,181,255),false);xiangqiDiscs[at].sprite=PrologueDesk.Circle();
                xiangqiGlyphs[at]=Text(PrologueDesk.Rect("Glyph",disc,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"",28,Side,TextAlignmentOptions.Center);
            }
            Text(PrologueDesk.Rect("Coordinates",xiangqiArea,new Vector2(0,1),new Vector2(0,1),new Vector2(35,-682),new Vector2(521,-652)),"A–I  /  1–10 ↑",16,Side,TextAlignmentOptions.Center);
            var side=PrologueDesk.Rect("Xiangqi controls",xiangqiArea,Vector2.zero,Vector2.one,new Vector2(565,50),new Vector2(-35,-108));
            xiangqiHelp=Text(PrologueDesk.Rect("Help",side,new Vector2(0,.52f),Vector2.one,Vector2.zero,Vector2.zero),"",19,Side);
            xiangqiStatus=Text(PrologueDesk.Rect("Status",side,new Vector2(0,.24f),new Vector2(1,.52f),Vector2.zero,Vector2.zero),"",21,Side);
            xiangqiNewButton=ComposerButton(side,"Compose Xiangqi puzzle",new Vector2(0,.12f),new Vector2(1,.22f),StartXiangqiComposition,out xiangqiNewLabel);
            xiangqiResetButton=ComposerButton(side,"Reset Xiangqi puzzle",Vector2.zero,new Vector2(1,.1f),ResetXiangqiComposition,out xiangqiResetLabel);
            xiangqiArea.gameObject.SetActive(false);SyncXiangqiComposer();PaintXiangqi();
        }
        void SyncXiangqiComposer()
        {
            if(xiangqiArea==null)return;var sim=TiebaHub.Lab();bool bought=ComposerBought;
            if(!ReferenceEquals(xiangqiOwner,sim)||!ReferenceEquals(xiangqiState,sim?.S)||xiangqiWasBought!=bought)
            {
                xiangqiOwner=sim;xiangqiState=sim?.S;xiangqiWasBought=bought;xiangqiComposer=null;xiangqiBoard=null;xiangqiPosition=null;xiangqiSelected=-1;xiangqiPlayed=false;
                RelabelXiangqiComposer();PaintXiangqi();
            }
            navFills["board.xiangqi"].gameObject.SetActive(bought);if(!bought&&showing=="board.xiangqi")Show("home");
        }
        void RelabelXiangqiComposer()
        {
            navLabels["board.xiangqi"].text=T("将帅车残局","Rook endgame");if(xiangqiTitle==null)return;
            xiangqiTitle.text=T("将帅车一步杀 · 逻辑 Lv4","Generals-and-rook mate in one · Logic Lv4");
            xiangqiHelp.text=T("它自己摆残局，再检查全部合法着法和黑方应对；验证有解才显示。\n\n红先。点红子，再点目标。将帅只能在九宫内直走一格，不能照面；车直走，不能越子。\n\n仅支持将、帅和一辆红车的残局；不是完整象棋自由对弈。不生成对局样本。", "Generals and one red rook: mate in one, verified against every legal defence.\n\nRed first. Select a piece, then a square. Generals stay in their palaces, move one orthogonal step and cannot face on an open file. The rook moves straight without jumping.\n\nLimited endgame practice; no game-record samples.");
            xiangqiNewLabel.text=T("让它摆一题并验证","Compose and verify");xiangqiResetLabel.text=T("重摆这道题","Reset this puzzle");
            xiangqiStatus.text=xiangqiBoard==null?T("点击摆题，红先一步将杀。","Compose an endgame: Red mates in one."):T("红先，一步将杀。","Red to move: mate in one.");
        }
        void StartXiangqiComposition()
        {
            if(!CanXiangqi)return;xiangqiComposer=new XiangqiRookComposer(UnityEngine.Random.Range(0,int.MaxValue));xiangqiNextStep=Time.unscaledTime;xiangqiBoard=null;xiangqiPosition=null;xiangqiSelected=-1;xiangqiPlayed=false;
            xiangqiStatus.text=T("正在摆棋并检查合法应对……","Composing and checking legal replies…");PaintXiangqi();
        }
        void TickXiangqiComposer()
        {
            if(xiangqiComposer==null||xiangqiComposer.Finished||!CanXiangqi||Time.unscaledTime<xiangqiNextStep)return;
            xiangqiNextStep=Time.unscaledTime+.1f;
            xiangqiComposer.Step();if(!xiangqiComposer.Finished)return;
            xiangqiPosition=xiangqiComposer.Position;if(xiangqiPosition==null){xiangqiStatus.text=T("未找到验证通过的题，请重试。","No verified puzzle found. Try again.");return;}
            xiangqiBoard=new XiangqiRookEndgame(xiangqiPosition);
            xiangqiStatus.text=xiangqiOwner.AiDisplayName+T(" 摆好并解过了。红先一步杀。"," composed and verified it. Red mates in one.");PaintXiangqi();
        }
        void ResetXiangqiComposition()
        {
            if(!CanXiangqi||xiangqiPosition==null)return;xiangqiBoard=new XiangqiRookEndgame(xiangqiPosition);xiangqiSelected=-1;xiangqiPlayed=false;
            xiangqiStatus.text=T("已重摆。红先，一步将杀。","Reset. Red to move: mate in one.");PaintXiangqi();
        }
        void PlayXiangqiSquare(int at)
        {
            if(!CanXiangqi||xiangqiBoard==null||xiangqiPlayed)return;
            if(xiangqiBoard[at]>0){xiangqiSelected=at;PaintXiangqi();return;}if(xiangqiSelected<0)return;
            if(!xiangqiBoard.Move(new XiangqiRookMove(xiangqiSelected,at)))
            {xiangqiStatus.text=T("这步不合法：检查九宫、阻挡和将帅照面。","Illegal: check the palace, blockers and facing generals.");return;}
            xiangqiPlayed=true;xiangqiSelected=-1;xiangqiStatus.text=xiangqiBoard.Turn==-1&&xiangqiBoard.Checkmate?
                T("将杀！黑方没有合法解围。","Checkmate! Black has no legal escape."):T("这步合法，但不是将杀。重摆再试。","Legal, but not mate. Reset and try again.");PaintXiangqi();
        }
        void PaintXiangqi()
        {
            if(xiangqiArea==null)return;
            for(int i=0;i<90;i++)
            {
                if(xiangqiCells[i]==null)continue;int piece=xiangqiBoard==null?0:xiangqiBoard[i];
                xiangqiCells[i].color=i==xiangqiSelected?new Color32(245,223,104,180):Color.clear;
                xiangqiDiscs[i].enabled=piece!=0;xiangqiGlyphs[i].text=piece==1?T("帅","K"):piece==-1?T("将","K"):piece==2?T("车","R"):"";
                xiangqiGlyphs[i].color=piece>0?new Color32(173,47,43,255):new Color32(40,43,46,255);
            }
            if(xiangqiResetButton!=null)xiangqiResetButton.interactable=xiangqiPosition!=null;
        }
    }
}
