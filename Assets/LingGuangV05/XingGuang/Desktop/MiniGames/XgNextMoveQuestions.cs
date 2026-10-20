using System;
using System.Collections.Generic;
using LingGuangV05.Core.Games;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgNextMoveQuestions : XgPackQuestion
    {
        Texture2D picture;Gomoku board;bool attack;TMPro.TMP_Text prediction;
        public XgNextMoveQuestions(XgMiniContext context):base(context){}
        protected override void DrawQuestion()
        {
            if(picture!=null)XgMiniContext.DestroyObject(picture);
            board=new Gomoku();attack=C.Random.Next(2)==0;int stone=attack?1:2;
            int row=C.Random.Next(3,12),start=C.Random.Next(1,8),gap=C.Random.Next(1,4);bool vertical=C.Random.Next(2)==0;
            for(int i=0;i<5;i++)if(i!=gap)board.Place(vertical?row:start+i,vertical?start+i:row,stone);
            // Four remote opposite-colour stones keep normal counts without supplying the solver an answer.
            for(int i=0;i<4;i++)board.Place(i*3,0,3-stone);
            promptZh=attack?"黑棋下一步在哪里连五？":"黑棋走哪一步能挡住白棋下一步连五？";
            promptEn=attack?"Where does Black make five next?":"Where must Black block White's immediate five?";
            Header("下一步走哪","Next move","从四个合法落点选择。图片题可把棋盘拖到右侧识别。","Choose one of four legal moves. Drag picture questions to the reader on the right.");
            var cells=new int[225];for(int y=0;y<15;y++)for(int x=0;x<15;x++)cells[y*15+x]=board[x,y];
            QuestionCard=new XgCard{dataset="go",teachingImageKind="board15",teachingImageKeys=cells};
            var rgb=GomokuPicture.Render(board);picture=new Texture2D(240,240,TextureFormat.RGB24,false);picture.LoadRawTextureData(rgb);picture.Apply();picture.filterMode=FilterMode.Point;
            var img=C.Rect("Board picture",30,95,300,300).gameObject.AddComponent<RawImage>();img.texture=picture;img.uvRect=new Rect(0,1,1,-1);
            T("Coordinates",30,397,320,28,"A–O  /  1–15 ↓",15);
            T("Question",365,95,500,66,XgUi.T(promptZh,promptEn),24);
            var drop=C.Box("Read picture",365,168,500,74,new Color32(228,231,241,255));drop.raycastTarget=true;
            prediction=T("Board prediction",375,172,480,65,"",18);
            bool imageMode=C.Sim.BoardLogicLevel>=2;
            if(imageMode)
            {
                prediction.text=XgUi.T("把左侧棋盘图拖到这里，让它从图片认棋。","Drag the board here to read its pixels.");
                img.gameObject.AddComponent<XgBoardPictureDrag>();
                var target=drop.gameObject.AddComponent<XgBoardPictureDrop>();target.Owner=C.Sim;int generation=version;
                target.CanAccept=()=>!disposed&&version==generation&&C.IsActive&&!submitted;
                target.Accept=Read;
            }
            else Predict(board);
            var correct=new List<int>();var other=new List<int>();
            for(int y=0;y<15;y++)for(int x=0;x<15;x++)if(board[x,y]==0){int at=y*15+x;if(board.Score(x,y,stone)>=100000)correct.Add(at);else other.Add(at);}
            if(correct.Count==0)return;Shuffle(other);var choices=new List<int>{correct[0],other[0],other[1],other[2]};Shuffle(choices);
            for(int i=0;i<4;i++)
            {
                int choice=choices[i];string label=GomokuLogic.Point(choice%15,choice/15);
                B("BoardChoice"+i,365+(i%2)*255,260+(i/2)*68,245,58,label,()=>
                {if(submitted||!C.IsActive)return;chosenText=label;bool good=correct.Contains(choice);Submit(good,(attack?"能连五的落点：":"必须挡住：")+GomokuLogic.Point(correct[0]%15,correct[0]/15),(attack?"Winning point: ":"Required block: ")+GomokuLogic.Point(correct[0]%15,correct[0]/15));},23);
            }
        }
        void Predict(Gomoku position)
        {
            if(!GomokuLogic.Pick(position,1,C.Sim.BoardLogicLevel,C.Random,C.Sim.BoardLearnedBias(),out int x,out int y)){aiText=XgUi.T("无法判断","Cannot determine");CapturePrediction(null);}
            else {aiText=GomokuLogic.Point(x,y);CapturePrediction(board[x,y]==0&&board.Score(x,y,attack?1:2)>=100000);}
            prediction.text=C.Sim.AiDisplayName+"："+aiText;prediction.color=PredictionInk;
        }
        void Read(byte[] rgb)
        {
            if(!GomokuPicture.TryRead(rgb,240,240,out var recognized)){ClearPredictionFeedback();prediction.text=XgUi.T("没有认出清晰棋盘，请重试。","Board not recognized; try a clear crop.");prediction.color=UnsureInk;return;}
            Predict(recognized);
        }
        public override void Dispose(){if(picture!=null)XgMiniContext.DestroyObject(picture);base.Dispose();}
    }
}
