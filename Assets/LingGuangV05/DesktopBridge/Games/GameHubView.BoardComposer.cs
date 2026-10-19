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
        RectTransform composerArea;TMP_Text composerTitle,composerHelp,composerStatus,composeLabel,resetComposeLabel;
        readonly Image[] composerSquares=new Image[64],composerPieces=new Image[64];
        ChessSprites composerSprites;ChessComposer composer;ChessGame composedBoard;
        XgSim composerOwner;XgState composerState;string composedFen;int composerSelected=-1;
        bool composerWasBought,composerPlayed;float composerNextStep;Button composeButton,resetComposeButton;
        bool ComposerBought=>TiebaHub.Lab()?.BoardLogicLevel>=4;
        bool CanCompose=>ComposerBought&&showing=="board.composer"&&ReferenceEquals(composerOwner,TiebaHub.Lab())&&ReferenceEquals(composerState,composerOwner?.S)&&XgBoardPictureDrag.Visible(composerArea);
        void BuildBoardComposer()
        {
            composerArea=PrologueDesk.Rect("board.composer",main,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);areas["board.composer"]=composerArea;
            composerTitle=Text(PrologueDesk.Rect("Title",composerArea,new Vector2(0,1),Vector2.one,new Vector2(35,-78),new Vector2(-35,-24)),"",29,Side);
            var board=PrologueDesk.Rect("Composed chess board",composerArea,new Vector2(0,1),new Vector2(0,1),new Vector2(35,-620),new Vector2(547,-108));
            for(int rank=0;rank<8;rank++)for(int file=0;file<8;file++)
            {
                int square=rank*8+file;
                var rect=PrologueDesk.Rect("Compose square "+square,board,new Vector2(file/8f,rank/8f),new Vector2((file+1)/8f,(rank+1)/8f),Vector2.zero,Vector2.zero);
                composerSquares[square]=PrologueDesk.Fill(rect,Color.white);
                var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=composerSquares[square];button.onClick.AddListener(()=>PlayComposedSquare(square));
                var icon=PrologueDesk.Rect("Piece",rect,Vector2.zero,Vector2.one,new Vector2(6,5),new Vector2(-6,-5));
                composerPieces[square]=icon.gameObject.AddComponent<Image>();composerPieces[square].raycastTarget=false;composerPieces[square].preserveAspect=true;
            }
            Text(PrologueDesk.Rect("Board coordinates",composerArea,new Vector2(0,1),new Vector2(0,1),new Vector2(35,-655),new Vector2(547,-624)),"a–h  /  1–8 ↑",17,Side,TextAlignmentOptions.Center);
            var side=PrologueDesk.Rect("Composer controls",composerArea,new Vector2(0,0),Vector2.one,new Vector2(590,50),new Vector2(-35,-110));
            composerHelp=Text(PrologueDesk.Rect("Help",side,new Vector2(0,.55f),Vector2.one,Vector2.zero,Vector2.zero),"",20,Side);
            composerStatus=Text(PrologueDesk.Rect("Status",side,new Vector2(0,.24f),new Vector2(1,.55f),Vector2.zero,Vector2.zero),"",21,Side);
            composeButton=ComposerButton(side,"Compose new puzzle",new Vector2(0,.12f),new Vector2(1,.22f),StartComposition,out composeLabel);
            resetComposeButton=ComposerButton(side,"Reset composed puzzle",Vector2.zero,new Vector2(1,.10f),ResetComposition,out resetComposeLabel);
            composerArea.gameObject.SetActive(false);SyncBoardComposer();PaintComposedBoard();
        }
        Button ComposerButton(RectTransform parent,string name,Vector2 min,Vector2 max,Action action,out TMP_Text label)
        {
            var rect=PrologueDesk.Rect(name,parent,min,max,Vector2.zero,Vector2.zero);var fill=PrologueDesk.Fill(rect,new Color32(74,103,151,255));
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=fill;button.onClick.AddListener(()=>action());
            label=Text(PrologueDesk.Rect("Label",rect,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"",19,Color.white,TextAlignmentOptions.Center);return button;
        }
        void SyncBoardComposer()
        {
            if(composerArea==null)return;var sim=TiebaHub.Lab();bool bought=ComposerBought;
            if(!ReferenceEquals(composerOwner,sim)||!ReferenceEquals(composerState,sim?.S)||composerWasBought!=bought)
            {
                composerOwner=sim;composerState=sim?.S;composerWasBought=bought;composer=null;composedBoard=null;composedFen=null;composerSelected=-1;composerPlayed=false;
                RelabelBoardComposer();PaintComposedBoard();
            }
            navFills["board.composer"].gameObject.SetActive(bought);
            if(!bought&&showing=="board.composer")Show("home");
        }
        void RelabelBoardComposer()
        {
            navLabels["board.composer"].text=T("自出棋题","Compose mate");if(composerTitle==null)return;
            composerTitle.text=T("自出棋题 · 逻辑 Lv4","Compose a chess puzzle · Logic Lv4");
            composerHelp.text=T("让它摆一个王后对单王的一步杀，再用合法走子逐一验证。\n\n白先，点白子再点目标格。一手将死黑王即可，所有合法将杀都算。\n\n这是本地原创练习，不替代原有公开题库；不生成对局样本。", "Compose a king-and-queen versus king mate in one. Every puzzle is checked with legal moves.\n\nWhite moves first. Select a white piece, then a square. Any legal checkmate counts.\n\nOriginal local practice. No game-record samples.");
            composeLabel.text=T("让它出一题","Compose a puzzle");resetComposeLabel.text=T("重摆这道题","Reset this puzzle");
            composerStatus.text=composedBoard==null?T("点击出题，完成验证后才显示棋盘。","Compose a puzzle; its board appears only after verification."):T("白先，一步将杀。","White to move: mate in one.");
        }
        void StartComposition()
        {
            if(!CanCompose)return;
            composer=new ChessComposer(UnityEngine.Random.Range(0,int.MaxValue));composerNextStep=Time.unscaledTime;composedBoard=null;composedFen=null;composerSelected=-1;composerPlayed=false;
            composerStatus.text=T("正在摆棋并验证……","Composing and verifying…");PaintComposedBoard();
        }
        void TickBoardComposer()
        {
            if(composer==null||composer.Finished||!CanCompose||Time.unscaledTime<composerNextStep)return;
            composerNextStep=Time.unscaledTime+.1f;
            composer.Step();if(!composer.Finished)return;
            if(composer.Fen==null){composerStatus.text=T("这次未找到合格棋题，请重新出题。","No verified puzzle found. Try composing again.");return;}
            composedFen=composer.Fen;composedBoard=new ChessGame(composedFen);
            composerStatus.text=composerOwner.AiDisplayName+T(" 已出一题。白先，一步将杀。"," composed a puzzle. White to move: mate in one.");PaintComposedBoard();
        }
        void ResetComposition()
        {
            if(!CanCompose||composedFen==null)return;composedBoard=new ChessGame(composedFen);composerSelected=-1;composerPlayed=false;
            composerStatus.text=T("已重摆。白先，一步将杀。","Reset. White to move: mate in one.");PaintComposedBoard();
        }
        void PlayComposedSquare(int square)
        {
            if(!CanCompose||composedBoard==null||composerPlayed)return;char piece=composedBoard[square%8,square/8];
            if(piece!='.'&&char.IsUpper(piece)){composerSelected=square;PaintComposedBoard();return;}
            if(composerSelected<0)return;
            if(!composedBoard.Move(composerSelected%8,composerSelected/8,square%8,square/8))
            {composerStatus.text=T("这一步不合法，请重新选目标。","That move is illegal. Choose another destination.");return;}
            composerPlayed=true;composerSelected=-1;
            composerStatus.text=composedBoard.Result==ChessGame.WhiteWins&&composedBoard.ResultReasonEnglish=="Checkmate"?T("将杀！可以重摆或让它再出一题。","Checkmate! Reset or compose another puzzle."):T("这一步合法，但没有将杀。重摆再试。","Legal move, but not mate. Reset and try again.");PaintComposedBoard();
        }
        void PaintComposedBoard()
        {
            if(composerArea==null)return;
            if(composedBoard!=null&&composerSprites==null)composerSprites=new ChessSprites();
            for(int square=0;square<64;square++)
            {
                var cell=composerSquares[square];if(cell==null)continue;
                cell.color=square==composerSelected?new Color32(220,199,90,255):(square%8+square/8)%2==0?new Color32(112,145,104,255):new Color32(238,235,205,255);
                var icon=composerPieces[square];icon.sprite=composedBoard==null?null:composerSprites.Get(composedBoard[square%8,square/8]);icon.enabled=icon.sprite!=null;
            }
            if(resetComposeButton!=null)resetComposeButton.interactable=composedFen!=null;
        }
    }
}
