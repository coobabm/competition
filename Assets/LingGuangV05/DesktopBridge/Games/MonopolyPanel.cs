using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.Games
{
    public sealed class MonopolyPanel : HubGame
    {
        MonopolyGame game=new MonopolyGame();
        readonly TMP_Text[] squares=new TMP_Text[20];readonly Image[] fills=new Image[20];
        TMP_Text status,account,rollLabel,buyLabel,skipLabel,againLabel;Button rollButton,buyButton,skipButton;
        bool counted;float aiAt=-1;
        static readonly Color Red=new Color32(204,80,82,255),Blue=new Color32(62,130,194,255);
        public override string Id=>"monopoly";
        public override string Title=>T("大富翁","Monopoly-style");
        public override string Blurb=>T("20格小地图，8轮比现金加地产。¥1,500游戏币开局，和真钱无关。","20 squares, 8 rounds. Start with ¥1,500 in play money; most cash plus property wins.");
        public override string Glyph=>"¥";
        public override Color Accent=>new Color32(174,126,49,255);
        public override void Build(RectTransform area)
        {
            PrologueDesk.Fill(area,new Color32(245,242,234,255));
            var board=PrologueDesk.Centered("Property board",area,new Vector2(-180,-12),new Vector2(610,610));PrologueDesk.Fill(board,new Color32(252,250,243,255));
            for(int i=0;i<20;i++)
            {
                var cell=PrologueDesk.Centered("Square "+i,board,Cell(i),new Vector2(88,88));fills[i]=PrologueDesk.Fill(cell,Color.white,false);
                squares[i]=Text(PrologueDesk.Rect("Square text",cell,Vector2.zero,Vector2.one,new Vector2(3,3),new Vector2(-3,-3)),"",14,new Color32(48,43,31,255),TextAlignmentOptions.Center);
                squares[i].enableAutoSizing=true;squares[i].fontSizeMin=10;squares[i].fontSizeMax=14;
            }
            account=Text(PrologueDesk.Centered("Accounts",board,new Vector2(0,0),new Vector2(350,345)),"",21,new Color32(48,43,31,255),TextAlignmentOptions.Center);
            var side=PrologueDesk.Rect("Side",area,new Vector2(1,0),Vector2.one,new Vector2(-345,32),new Vector2(-28,-64));
            status=Text(PrologueDesk.Rect("Status",side,new Vector2(0,.43f),Vector2.one,Vector2.zero,Vector2.zero),"",20,new Color32(48,43,31,255));
            rollButton=Button(side,"Roll",new Vector2(0,.31f),new Vector2(1,.41f),Accent,Roll,out rollLabel);
            buyButton=Button(side,"Buy property",new Vector2(0,.20f),new Vector2(1,.29f),Accent,()=>Purchase(true),out buyLabel);
            skipButton=Button(side,"Skip purchase",new Vector2(0,.11f),new Vector2(1,.19f),new Color32(130,128,118,255),()=>Purchase(false),out skipLabel);
            Button(side,"Restart",Vector2.zero,new Vector2(1,.09f),new Color32(130,128,118,255),RestartRound,out againLabel);Draw();
        }
        static Vector2 Cell(int i)
        {if(i<=5)return new Vector2(-240+i*96,240);if(i<=10)return new Vector2(240,240-(i-5)*96);if(i<=15)return new Vector2(240-(i-10)*96,-240);return new Vector2(-240,-240+(i-15)*96);}
        public override void Refresh()=>Draw();
        public override void RestartRound(){AbandonRoundRecord();game=new MonopolyGame();counted=false;aiAt=-1;sampleNote="";ClearComment();Draw();}
        void Roll()
        {
            if(!CanInteract||game.Over||game.S.turn!=0||game.S.pending>=0)return;
            if(game.S.moves==0)BeginRoundRecord();DoRoll();AfterAction();
        }
        void DoRoll(){int today=Lab!=null&&Lab.TodayKnown?Lab.Today:0;game.Roll(Rng.Next(1,7),today,Rng.Next(MonopolyGame.EligibleChance(today).Count));}
        void Purchase(bool buy){if(!CanInteract||game.Over||game.S.turn!=0)return;if(game.ChoosePurchase(buy))AfterAction();}
        void AfterAction()
        {
            if(game.Over&&!counted){counted=true;Finished(game.S.winner==0?XgGameOutcome.PlayerWon:game.S.winner==1?XgGameOutcome.PlayerLost:XgGameOutcome.Draw,game.S.moves,game.S.log);}
            aiAt=!game.Over&&game.S.turn==1?Time.unscaledTime+.7f:-1;Draw();
        }
        void Update()
        {
            if(!CanInteract||game.Over||game.S.turn!=1)return;
            if(aiAt<0){aiAt=Time.unscaledTime+.7f;return;}if(Time.unscaledTime<aiAt)return;
            if(game.S.pending<0)DoRoll();else game.ChoosePurchase(game.ShouldBuy(AiPlays?Lab.BoardLogicLevel:1,AiPlays?Lab.BoardSelfPolicy(Id):1));AfterAction();
        }
        void Draw()
        {
            if(status==null)return;
            for(int i=0;i<20;i++)
            {
                int owner=game.S.owners[i];fills[i].color=owner==0?new Color32(249,220,218,255):owner==1?new Color32(215,231,247,255):MonopolyGame.Property(i)?Color.white:new Color32(238,226,192,255);
                string tokens=(game.S.position[0]==i?"<color=#CC5052>●1</color> ":"")+(game.S.position[1]==i?"<color=#3E82C2>●2</color>":"");
                squares[i].text=T(MonopolyGame.Names[i],MonopolyGame.NamesEn[i])+"\n"+(MonopolyGame.Property(i)?"¥"+MonopolyGame.Price(i)+"\n"+T("租 ","Rent ")+MonopolyGame.Rent(i):i==18?"−¥100":i==0?"+¥200":"")+"\n"+tokens;
            }
            account.text="<b>"+T("游戏币账户","Play money")+"</b>\n\n<color=#CC5052>●1 "+T("你","You")+"</color>\n"+Money(0)+"\n\n<color=#3E82C2>●2 "+OpponentName+"</color>\n"+Money(1)+"\n\n<size=15>"+T("钱和地产仅属于本局\n地价=200+格号×20；租金为地价20%\n现金不足付账即破产","Only this round's play money\nPrice=200+square×20; rent=20%\nUnable to pay a bill means bankruptcy")+"</size>";
            string state=game.Over?game.S.winner==2?T("平局。","Draw."):game.S.winner==0?T("你赢了！","You won!"):OpponentName+T("赢了。"," won."):
                game.S.turn==1?OpponentName+T("正在行动……"," is playing…"):game.S.pending>=0?T("买下这块地，还是留着现金？","Buy this property or keep the cash?"):T("轮到你，掷骰子。","Your turn. Roll the die.");
            status.text="<b><size=27>"+Title+"</size></b>\n\n"+state+"\n\n"+T("骰子：","Die: ")+(game.S.die==0?"—":game.S.die.ToString())+"\n"+T("已走回合：你 ","Turns: you ")+game.S.turns[0]+"/8 · "+game.S.turns[1]+"/8"+
                (game.LastChance<0?"":"\n\n"+T(MonopolyGame.Chance[game.LastChance],MonopolyGame.ChanceEn[game.LastChance]))+LabLines();
            bool ours=!game.Over&&game.S.turn==0;rollButton.interactable=ours&&game.S.pending<0;
            buyButton.interactable=ours&&game.S.pending>=0&&game.S.cash[0]>=MonopolyGame.Price(game.S.pending);skipButton.interactable=ours&&game.S.pending>=0;
            rollLabel.text=T("掷骰子","Roll die");buyLabel.text=T("购买地产","Buy property")+(game.S.pending>=0?" · ¥"+MonopolyGame.Price(game.S.pending):"");skipLabel.text=T("不买，结束回合","Skip; end turn");againLabel.text=T("重新开局","New game");
        }
        string Money(int p)=>T("现金 ","Cash ")+"¥"+game.S.cash[p]+" · "+T("地产 ","Property ")+"¥"+game.PropertyValue(p)+"\n"+T("总值 ","Worth ")+"¥"+game.Worth(p);
    }
}
