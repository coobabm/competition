using System;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.Games
{
    public sealed class LudoPanel : HubGame
    {
        LudoGame game=new LudoGame();
        readonly RectTransform[] tokens=new RectTransform[4];
        readonly Button[] tokenButtons=new Button[4];
        readonly TMP_Text[] tokenLabels=new TMP_Text[4];
        TMP_Text status,rollLabel,againLabel,dieLabel;
        Button rollButton;RectTransform board;
        bool counted;float aiAt=-1;
        static readonly Color Red=new Color32(207,74,72,255),Blue=new Color32(65,132,204,255);
        public override string Id=>"ludo";
        public override string Title=>T("飞行棋","Aeroplane chess");
        public override string Blurb=>T("两人各2架，28格环道。掷5或6起飞，撞回对手，先全部回家获胜。","Two planes each on a 28-cell loop. Launch on 5 or 6, send rivals back, and bring both home.");
        public override string Glyph=>"✈";
        public override Color Accent=>Blue;
        public override void Build(RectTransform area)
        {
            PrologueDesk.Fill(area,new Color32(241,243,247,255));
            board=PrologueDesk.Centered("Aeroplane board",area,new Vector2(-180,-10),new Vector2(610,610));
            PrologueDesk.Fill(board,new Color32(250,250,252,255));
            for(int i=0;i<28;i++)
            {
                int square=i;var cell=PrologueDesk.Centered("Loop "+i,board,Cell(i),new Vector2(48,48));
                var color=i%4==0?Red:i%4==2?Blue:i%4==1?new Color32(232,181,62,255):new Color32(78,162,125,255);
                PrologueDesk.Fill(cell,color,false).sprite=PrologueDesk.Circle();
                Text(PrologueDesk.Rect("Number",cell,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),(i+1).ToString(),17,Color.white,TextAlignmentOptions.Center);
            }
            Text(PrologueDesk.Centered("Player base label",board,new Vector2(-110,95),new Vector2(165,35)),T("你的基地","Your base"),20,Red,TextAlignmentOptions.Center);
            Text(PrologueDesk.Centered("Opponent base label",board,new Vector2(110,95),new Vector2(165,35)),T("对方基地","Opponent base"),20,Blue,TextAlignmentOptions.Center);
            Text(PrologueDesk.Centered("Home label",board,new Vector2(0,-80),new Vector2(200,40)),T("终点","Home"),22,new Color32(60,65,76,255),TextAlignmentOptions.Center);
            for(int i=0;i<4;i++)
            {
                int index=i;var token=PrologueDesk.Centered("Plane "+i,board,Vector2.zero,new Vector2(34,34));tokens[i]=token;
                var fill=PrologueDesk.Fill(token,i<2?Red:Blue);fill.sprite=PrologueDesk.Circle();
                var outline=token.gameObject.AddComponent<Outline>();outline.effectColor=Color.white;outline.effectDistance=new Vector2(2,2);
                tokenButtons[i]=token.gameObject.AddComponent<Button>();tokenButtons[i].targetGraphic=fill;
                tokenButtons[i].onClick.AddListener(()=>Move(index));
                tokenLabels[i]=Text(PrologueDesk.Rect("Plane number",token,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),(i%2+1).ToString(),19,Color.white,TextAlignmentOptions.Center);
            }
            var side=PrologueDesk.Rect("Side",area,new Vector2(1,0),Vector2.one,new Vector2(-345,32),new Vector2(-28,-64));
            status=Text(PrologueDesk.Rect("Status",side,new Vector2(0,.38f),Vector2.one,Vector2.zero,Vector2.zero),"",20,new Color32(38,46,62,255));
            dieLabel=Text(PrologueDesk.Rect("Die",side,new Vector2(0,.25f),new Vector2(1,.38f),Vector2.zero,Vector2.zero),"",44,Blue,TextAlignmentOptions.Center);
            rollButton=Button(side,"Roll",new Vector2(0,.13f),new Vector2(1,.23f),Blue,Roll,out rollLabel);
            Button(side,"Restart",new Vector2(0,0),new Vector2(1,.10f),new Color32(125,134,149,255),RestartRound,out againLabel);
            Draw();
        }
        static Vector2 Cell(int square)
        {float angle=(square*360f/28+90)*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*265;}
        // View-only slots: the core position, turn and random die never change here.
        // A 34px token cannot use the old +/-7px offsets: the later sibling covers its centre.
        static Vector2 PlanePoint(LudoGame state,int player,int plane,out float size)
        {
            size=34;
            int p=state.Position(player,plane);
            if(p<0)return new Vector2((player==0?-110:110)+(plane==0?-24:24),42);
            if(p==LudoGame.Home)return new Vector2((player==0?-55:55)+(plane==0?-18:18),-125);
            int square=state.Global(player,p),index=player*2+plane,count=0,slot=0;
            for(int i=0;i<4;i++)
                if(state.Global(i/2,state.Position(i/2,i%2))==square)
                {if(i<index)slot++;count++;}
            var centre=Cell(square);
            if(count==1)return centre;
            // Two planes get disjoint left/right buttons. Three/four use a compact 2x2 grid,
            // including imported states with opposing colours on the same square.
            // Every button stays inside its cell area and clear of neighbouring-cell buttons.
            if(count==2){size=22;return centre+new Vector2(slot==0?-12:12,0);}
            size=18;return centre+new Vector2(slot%2==0?-10:10,slot<2?10:-10);
        }
        public override void Refresh()=>Draw();
        public override void RestartRound()
        {AbandonRoundRecord();game=new LudoGame();counted=false;aiAt=-1;sampleNote="";ClearComment();Draw();}
        void Roll()
        {
            if(!CanInteract||game.Over||game.S.turn!=0||game.S.die!=0)return;
            if(game.S.rolls==0)BeginRoundRecord();
            game.Roll(Rng.Next(1,7));AfterAction();
        }
        void Move(int plane)
        {
            if(!CanInteract||game.Over||game.S.turn!=0||plane<0||plane>1)return;
            if(game.Move(plane))AfterAction();
        }
        void AfterAction()
        {
            if(game.Over&&!counted){counted=true;Finished(game.S.winner==0?XgGameOutcome.PlayerWon:XgGameOutcome.PlayerLost,game.S.actions,game.S.log);}
            aiAt=!game.Over&&game.S.turn==1?Time.unscaledTime+.65f:-1;Draw();
        }
        void Update()
        {
            if(!CanInteract||game.Over||game.S.turn!=1)return;
            if(aiAt<0){aiAt=Time.unscaledTime+.65f;return;}
            if(Time.unscaledTime<aiAt)return;
            if(game.S.die==0)game.Roll(Rng.Next(1,7));
            else game.Move(game.PickPlane(AiPlays?Lab.BoardLogicLevel:1,Rng,AiPlays?Lab.BoardSelfPolicy(Id):1));
            AfterAction();
        }
        void Draw()
        {
            if(status==null)return;
            var legal=game.LegalPlanes();
            for(int i=0;i<4;i++)
            {
                tokens[i].anchoredPosition=PlanePoint(game,i/2,i%2,out float size);
                tokens[i].sizeDelta=Vector2.one*size;
                tokenLabels[i].fontSize=size==34?19:size==22?14:12;
                tokens[i].GetComponent<Outline>().effectDistance=Vector2.one*(size==34?2:1);
                tokenButtons[i].interactable=i<2&&!game.Over&&game.S.turn==0&&legal.Contains(i);
                tokenLabels[i].text=(i%2+1).ToString();
            }
            string state=game.Over?(game.S.winner==0?T("你赢了！","You won!"):OpponentName+T("赢了。"," won.")):
                game.S.turn==1?OpponentName+T("正在选择……"," is choosing…"):game.S.die==0?T("轮到你，掷骰子。","Your turn. Roll the die."):T("点你要移动的飞机。","Click the plane you want to move.");
            status.text="<b><size=27>"+Title+"</size></b>\n\n"+T("对手：","Opponent: ")+OpponentName+"\n\n"+state+"\n\n"+
                T("你的终点：","Your planes home: ")+game.AtHome(0)+"/2\n"+T("对方终点：","Opponent planes home: ")+game.AtHome(1)+"/2\n\n"+
                T("5或6起飞 · 6可再掷\n同色前跳 · 终点反弹","Launch on 5/6 · Roll again on 6\nColour jump · Bounce at home")+LabLines();
            dieLabel.text=game.S.die==0?"—":game.S.die.ToString();
            rollLabel.text=T("掷骰子","Roll die");againLabel.text=T("重新开局","New game");
            rollButton.interactable=!game.Over&&game.S.turn==0&&game.S.die==0;
        }
    }
}
