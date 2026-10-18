using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    internal sealed class XgGoGame:XgBoardMini
    {
        readonly XgBtn[] cells=new XgBtn[81];int[,] board;int index;bool done;float seconds;TMP_Text timer;XgMiniGoPuzzle Puzzle=>XgMiniGoPuzzle.All[index];
        string PName=>XgUi.T(Puzzle.Name,Puzzle.NameEn);
        string PGoal=>XgUi.T(Puzzle.Goal,Puzzle.GoalEn);
        public XgGoGame(XgMiniContext c):base(c){}
        public override void Build()
        {
            Frame(XgUi.T("AlphaGo · 死活题","AlphaGo · Life and Death"),XgUi.T("黑先；列 A B C D E F G H J，行 1～9。▲ 是目标；落在不能下的点不算错。","Black plays first; columns A B C D E F G H J, rows 1-9. ▲ is the target; a move that is not allowed does not count as a mistake."));
            C.Box("Board",20,88,378,378,new Color(.83f,.67f,.43f));
            for(int y=0;y<9;y++)for(int x=0;x<9;x++){int n=y*9+x;cells[n]=C.Button("Point"+n,25+x*41,93+y*41,39,39,"+",()=>Play(n),23);}
            timer=C.Text("Time",440,90,400,26,"",17);
            C.Button("Next",440,324,195,42,XgUi.T("下一题","Next puzzle"),()=>{if(done){index=(index+1)%XgMiniGoPuzzle.All.Length;Reset();}else Message(XgUi.T("先完成当前题；也可重试当前局面。 ","Finish this puzzle first; you can also retry the current position. "));});
            C.Button("Restart",650,324,195,42,XgUi.T("重试","Retry"),Reset);
            C.Button("Hint",440,378,405,42,XgUi.T(("请灵光支一招（H）").Replace("灵光", C.Sim.AiDisplayName),("Ask LingGuang for a tip (H)").Replace("LingGuang", C.Sim.AiDisplayName)),Hint);Reset();
        }
        void Reset(){board=Puzzle.Board();done=false;seconds=Puzzle.Difficulty==3?25:20;Message(PName+"\n"+PGoal+"\n"+XgUi.T("限时 "+seconds+" 秒。","Time limit: "+seconds+" s."));Draw();}
        void Draw(){for(int n=0;n<81;n++){int v=board[n/9,n%9];cells[n].label.text=v==0?"+":v==1?"●":"○";if(n==Puzzle.Y*9+Puzzle.X&&v!=0)cells[n].label.text+="▲";cells[n].image.color=new Color(.88f,.74f,.5f);cells[n].label.color=Dark;}}
        void Play(int n)
        {
            if(done)return;if(!XgMiniBoard.GoPlay(board,n%9,n/9,1,out var b,out int cap)){Message(XgUi.T("非法落子：该点有子，或落子后无气。 ","Illegal move: the point is occupied, or the stone would have no liberties. "));return;}
            bool ok=Puzzle.Judge(board,n%9,n/9);board=b;done=true;Draw();C.ReportTimed(ok,(Puzzle.Difficulty==3?25:20)-seconds);Message(XgUi.T((ok?"正解！":"这步未达成目标。")+"\n提子 "+cap+" 枚\n"+Puzzle.Why,(ok?"Correct!":"That move does not reach the goal.")+"\nCaptured: "+cap+"\n"+Puzzle.WhyEn));
        }
        void Hint()
        {
            if(done)return;var legal=new List<int>();var good=new List<int>();for(int n=0;n<81;n++)if(XgMiniBoard.GoPlay(board,n%9,n/9,1,out _,out _)){legal.Add(n);if(Puzzle.Judge(board,n%9,n/9))good.Add(n);}
            var pool=C.Random.NextDouble()<C.Skill&&good.Count>0?good:legal;if(pool.Count==0)return;int p=pool[C.Random.Next(pool.Count)];
            Message(XgUi.T(("灵光建议下在 ").Replace("灵光", C.Sim.AiDisplayName)+"ABCDEFGHJ"[p%9]+" "+(p/9+1)+"。\n目标："+Puzzle.Goal,("LingGuang would play at ").Replace("LingGuang", C.Sim.AiDisplayName)+"ABCDEFGHJ"[p%9]+" "+(p/9+1)+".\nGoal: "+Puzzle.GoalEn));cells[p].label.text="?";
        }
        public override void Tick(float dt){if(done)return;seconds=Math.Max(0,seconds-dt);timer.text=XgUi.T("第 "+(index+1)+" / 13 题　剩余 "+Math.Ceiling(seconds)+" 秒","Puzzle "+(index+1)+" / 13　"+Math.Ceiling(seconds)+" s left");if(seconds<=0){done=true;C.Report(false);Message(XgUi.T("时间到。\n"+Puzzle.Why,"Time is up.\n"+Puzzle.WhyEn));}if(C.KeyDown(Key.H))Hint();}
    }
    internal sealed class XgMergeGame:XgBoardMini
    {
        int[,] board=new int[4,4];readonly TMP_Text[] labels=new TMP_Text[16];readonly UnityEngine.UI.Image[] tiles=new UnityEngine.UI.Image[16];int moves;bool over;Vector2 start;
        public XgMergeGame(XgMiniContext c):base(c){}
        public override void Build()
        {
            Frame("2048 · 合成数据","方向键 / WASD 或在棋盘上滑动。相同数据包合并；无效移动不生成新包。");
            for(int n=0;n<16;n++){int x=n%4,y=n/4;tiles[n]=C.Box("Tile"+n,24+x*93,92+y*93,88,88,Pale);labels[n]=C.Text("Value"+n,30+x*93,96+y*93,76,80,"",23);labels[n].alignment=TextAlignmentOptions.Center;}
            var input=C.Rect("Swipe",20,88,376,376);C.Pointer(input,p=>start=p,null,p=>{var d=p-start;if(d.sqrMagnitude<400)return;Move(Math.Abs(d.x)>Math.Abs(d.y)?(d.x>0?2:0):(d.y>0?3:1));}).GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            string[] arrows={"←","↑","→","↓"};for(int d=0;d<4;d++){int dir=d;C.Button("Move"+d,438+d*102,300,94,42,arrows[d],()=>Move(dir));}
            C.Button("Hint",440,357,400,42,(XgUi.T("问灵光：往哪边合？","Ask LingGuang: which way to merge?")).Replace(XgUi.T("灵光","LingGuang"), C.Sim.AiDisplayName),Hint);C.Button("Restart",440,414,400,42,"重新开始",Reset);Reset();
        }
        void Reset(){Array.Clear(board,0,16);moves=0;over=false;Spawn();Spawn();Draw();}
        void Spawn(){var empty=new List<int>();for(int n=0;n<16;n++)if(board[n/4,n%4]==0)empty.Add(n);if(empty.Count==0)return;int p=empty[C.Random.Next(empty.Count)];board[p/4,p%4]=C.Random.NextDouble()<.9?1:2;}
        void Move(int dir){if(over)return;if(!XgMiniBoard.Slide(board,dir,out int gain))return;moves++;Spawn();if(gain>0)C.Report(true);over=!XgMiniBoard.CanSlide(board);Draw();}
        void Draw(){int max=0;for(int n=0;n<16;n++){int r=board[n/4,n%4];max=Math.Max(max,r);double v=100*Math.Pow(2,r-1);labels[n].text=r==0?"":v>=10000?(v/10000).ToString("0.#")+"万":v.ToString("0");tiles[n].color=r==0?Pale:Color.Lerp(new Color(.82f,.9f,.98f),new Color(.93f,.65f,.28f),Math.Min(1,r/12f));}
            Status.text="步数："+moves+"\n最高："+(max==0?0:100*Math.Pow(2,max-1)).ToString("N0")+" 条\n目标：合成 COCO 级（约20万）\n"+(over?"CPU 过载：没有合法移动，请重新开始。":max>=12?"已达到目标！可继续合成。":"合并才结算；滑动和提示不结算。");}
        void Hint(){if(over)return;int best=0;double score=double.NegativeInfinity;for(int d=0;d<4;d++){var b=(int[,])board.Clone();if(!XgMiniBoard.Slide(b,d,out int gain))continue;int empty=0;foreach(int v in b)if(v==0)empty++;double s=gain*10+empty;if(s>score){score=s;best=d;}}
            int dir=C.Random.NextDouble()<C.Skill?best:C.Random.Next(4);Message((XgUi.T("灵光建议：","LingGuang suggests: ")).Replace(XgUi.T("灵光","LingGuang"), C.Sim.AiDisplayName)+new[]{"左","上","右","下"}[dir]+"。\n根据当前能力模拟，可能选到无效方向。 ");}
        public override void Tick(float dt){if(C.KeyDown(Key.LeftArrow)||C.KeyDown(Key.A))Move(0);if(C.KeyDown(Key.UpArrow)||C.KeyDown(Key.W))Move(1);if(C.KeyDown(Key.RightArrow)||C.KeyDown(Key.D))Move(2);if(C.KeyDown(Key.DownArrow)||C.KeyDown(Key.S))Move(3);if(C.KeyDown(Key.Space))Hint();if(C.KeyDown(Key.R))Reset();}
    }
}
