using System;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
namespace LingGuangV05.Desktop.Games
{
    public sealed partial class GameHubView
    {
        XgBoardPictureDrop boardReader;TMP_Text readerTitle,readerHelp,readerResult;
        XgSim readerOwner;XgState readerState;bool readerWasBought;
        bool ReaderBought=>TiebaHub.Lab()?.BoardLogicLevel>=2;
        void BuildBoardReader()
        {
            var area=PrologueDesk.Rect("board.reader",main,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            areas["board.reader"]=area;
            readerTitle=Text(PrologueDesk.Rect("Title",area,new Vector2(0,1),Vector2.one,new Vector2(40,-95),new Vector2(-40,-30)),"",30,Side);
            readerHelp=Text(PrologueDesk.Rect("Help",area,new Vector2(0,.55f),new Vector2(1,1),new Vector2(40,0),new Vector2(-40,-110)),"",20,Side);
            var drop=PrologueDesk.Rect("Board pixel drop",area,new Vector2(.05f,.15f),new Vector2(.95f,.52f),Vector2.zero,Vector2.zero);
            PrologueDesk.Fill(drop,new Color32(219,225,240,255));
            readerResult=Text(PrologueDesk.Rect("Result",drop,Vector2.zero,Vector2.one,new Vector2(22,16),new Vector2(-22,-16)),"",23,Side,TextAlignmentOptions.Center);
            boardReader=drop.gameObject.AddComponent<XgBoardPictureDrop>();
            boardReader.CanAccept=()=>showing=="board.reader"&&ReaderBought&&ReferenceEquals(readerOwner,TiebaHub.Lab())&&ReferenceEquals(readerState,readerOwner?.S);
            boardReader.Accept=ReadPublicBoard;
            area.gameObject.SetActive(false);SyncBoardReader();
        }
        void SyncBoardReader()
        {
            if(boardReader==null)return;
            var sim=TiebaHub.Lab();bool bought=ReaderBought;
            if(!ReferenceEquals(readerOwner,sim)||!ReferenceEquals(readerState,sim?.S)||readerWasBought!=bought)
            {readerOwner=sim;readerState=sim?.S;readerWasBought=bought;boardReader.Owner=sim;RelabelBoardReader();}
            navFills["board.reader"].gameObject.SetActive(bought);
            if(!bought&&showing=="board.reader")Show("home");
        }
        void RelabelBoardReader()
        {
            navLabels["board.reader"].text=T("棋盘读图","Board reader");
            if(readerTitle==null)return;
            readerTitle.text=T("棋盘读图 · 逻辑 Lv2","Board reader · Logic Lv2");
            readerHelp.text=T("在摆渡搜索「五子棋」，或打开贴吧首页的棋类交流帖。把棋盘原图拖到下方。\n仅支持本站标准15路五子棋图（240×240），按黑棋下一手判断。横向 A–O，纵向 1–15 从上往下。", "Search Bodu for ‘gomoku’, or open the board-game post on Tieba home. Drag its original board image below.\nSupports only these standard 15×15 Gomoku images (240×240), with Black to move. Columns A–O; rows 1–15 top to bottom.");
            readerResult.text=T("将棋盘原图拖到这里","Drop the original board image here");
        }
        void ReadPublicBoard(byte[] rgb)
        {
            if(!ReaderBought||!ReferenceEquals(readerOwner,TiebaHub.Lab())||!ReferenceEquals(readerState,readerOwner?.S))return;
            if(!GomokuPicture.TryRead(rgb,240,240,out var position))
            {readerResult.text=T("未认出本站标准棋盘图。","This image is not a supported standard board.");return;}
            if(!GomokuLogic.Pick(position,1,readerOwner.BoardLogicLevel,new System.Random(),readerOwner.BoardLearnedBias(),out int x,out int y))return;
            readerResult.text=readerOwner.AiDisplayName+T(" 建议黑棋："," suggests Black: ")+GomokuLogic.Point(x,y)+T("\n已从图片识别 ","\nRead ")+position.Moves+T(" 颗棋子。"," stones from the image.");
        }
    }
}
