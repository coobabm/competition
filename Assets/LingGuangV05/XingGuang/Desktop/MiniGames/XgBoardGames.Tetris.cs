using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    internal sealed class XgTetrisGame:XgBoardMini
    {
        static readonly int[][,] Shapes={new int[,]{{0,0,0,0},{1,1,1,1},{0,0,0,0},{0,0,0,0}},new int[,]{{1,1},{1,1}},new int[,]{{0,1,0},{1,1,1},{0,0,0}},new int[,]{{0,1,1},{1,1,0},{0,0,0}},new int[,]{{1,1,0},{0,1,1},{0,0,0}},new int[,]{{0,0,1},{1,1,1},{0,0,0}},new int[,]{{1,0,0},{1,1,1},{0,0,0}}};
        static readonly Color[] Colors={new Color(.2f,.75f,.8f),new Color(.95f,.78f,.2f),new Color(.6f,.4f,.9f),new Color(.3f,.8f,.4f),new Color(.95f,.4f,.4f),new Color(.95f,.6f,.2f),new Color(.3f,.5f,.9f)};
        static readonly string[] Names={"全连接 I","池化 O","注意力 T","卷积 S","LSTM Z","Dropout L","词向量 J"};
        readonly Image[] cells=new Image[200];readonly Queue<int> bag=new Queue<int>();readonly Image[] preview=new Image[16];readonly Image[] heldPreview=new Image[16];
        int[,] board=new int[22,10],piece;int type,x,y,next,held=-1,lines;bool usedHold,over,paused;float fall,repeat;TMP_Text details;int[,] suggestion;int sx,sy;
        public XgTetrisGame(XgMiniContext c):base(c){}
        public override void Build()
        {
            Frame("显存俄罗斯方块","方向键移动 / ↑旋转 / 空格直落 / C暂存 / P暂停。满一行才尝试真实训练。");
            Status.rectTransform.offsetMin=new Vector2(500,-208);Status.rectTransform.offsetMax=new Vector2(870,-98);
            for(int n=0;n<200;n++)cells[n]=C.Box("Cell"+n,264+n%10*19,86+n/10*19,18,18,Dark);
            C.Text("NextTitle",22,93,220,30,"下一层",19);C.Text("HoldTitle",22,245,220,30,"暂存槽（每块一次）",19);
            for(int n=0;n<16;n++){preview[n]=C.Box("Next"+n,30+n%4*25,132+n/4*25,23,23,Pale);heldPreview[n]=C.Box("Held"+n,30+n%4*25,282+n/4*25,23,23,Pale);}
            details=C.Text("Stats",495,213,380,60,"",18);
            C.Button("Left",495,290,80,40,"←",()=>Move(-1,0));C.Button("Rotate",585,290,100,40,"旋转",Rotate);C.Button("Right",695,290,80,40,"→",()=>Move(1,0));
            C.Button("Drop",495,340,130,40,"直落",Drop);C.Button("Hold",635,340,140,40,"暂存 C",Hold);
            C.Button("Hint",495,390,280,36,(XgUi.T("灵光落点建议","LingGuang landing tip")).Replace(XgUi.T("灵光","LingGuang"), C.Sim.AiDisplayName),Hint);
            C.Button("Restart",22,407,205,40,"重新开始",Reset);C.Button("Pause",790,290,85,90,"暂停",()=>{paused=!paused;Draw();});Reset();
        }
        int Take(){if(bag.Count==0){int[] a={0,1,2,3,4,5,6};Shuffle(a);foreach(int i in a)bag.Enqueue(i);}return bag.Dequeue();}
        void Reset(){Array.Clear(board,0,board.Length);bag.Clear();held=-1;lines=0;over=paused=false;next=Take();Spawn();}
        void Spawn(int replace=-1){type=replace<0?next:replace;if(replace<0)next=Take();piece=(int[,])Shapes[type].Clone();x=3;y=0;fall=0;usedHold=false;suggestion=null;over=XgMiniBoard.Collides(board,piece,x,y);Draw();}
        bool Move(int dx,int dy){if(over||paused||XgMiniBoard.Collides(board,piece,x+dx,y+dy))return false;x+=dx;y+=dy;Draw();return true;}
        void Rotate(){if(over||paused)return;var r=XgMiniBoard.Rotate(piece);foreach(int dx in new[]{0,-1,1,-2,2})if(!XgMiniBoard.Collides(board,r,x+dx,y)){piece=r;x+=dx;Draw();return;}}
        void Drop(){if(over||paused)return;while(!XgMiniBoard.Collides(board,piece,x,y+1))y++;Lock();}
        void Hold(){if(over||paused||usedHold)return;int prior=held;held=type;Spawn(prior);usedHold=true;Draw();}
        void Lock()
        {
            for(int py=0;py<piece.GetLength(0);py++)for(int px=0;px<piece.GetLength(1);px++)if(piece[py,px]!=0)board[y+py,x+px]=type+1;
            int cleared=XgMiniBoard.ClearRows(board);lines+=cleared;for(int i=0;i<cleared;i++)C.Report(true);
            for(int bx=0;bx<10;bx++)if(board[0,bx]!=0||board[1,bx]!=0){over=true;Draw();return;}Spawn();
        }
        void Preview(Image[] target,int t){for(int n=0;n<16;n++)target[n].color=t>=0&&n/4<Shapes[t].GetLength(0)&&n%4<Shapes[t].GetLength(1)&&Shapes[t][n/4,n%4]!=0?Colors[t]:Pale;}
        void Draw()
        {
            for(int n=0;n<200;n++)cells[n].color=board[n/10+2,n%10]==0?Dark:Colors[board[n/10+2,n%10]-1];
            if(suggestion!=null)Paint(suggestion,sx,sy,new Color(.55f,.3f,.8f));
            if(!over){int gy=y;while(!XgMiniBoard.Collides(board,piece,x,gy+1))gy++;Paint(piece,x,gy,new Color(.3f,.35f,.4f));Paint(piece,x,y,Colors[type]);}
            Preview(preview,next);Preview(heldPreview,held);
            Status.text=over?"显存溢出！\n点击重新开始。":paused?"已暂停":"当前层："+Names[type]+"\n下一层："+Names[next];
            details.text="已清 "+lines+" 行　速度 "+(lines/8+1)+"\n消行触发训练；成本/阻塞由主系统决定。";
        }
        void Paint(int[,] p,int ox,int oy,Color color){for(int py=0;py<p.GetLength(0);py++)for(int px=0;px<p.GetLength(1);px++)if(p[py,px]!=0&&oy+py>=2&&oy+py<22&&ox+px>=0&&ox+px<10)cells[(oy+py-2)*10+ox+px].color=color;}
        void Hint()
        {
            if(over||paused)return;var options=new List<Tuple<double,int[,],int,int>>();var r=(int[,])piece.Clone();
            for(int rotation=0;rotation<4;rotation++,r=XgMiniBoard.Rotate(r))for(int col=-2;col<10;col++)
            {
                if(XgMiniBoard.Collides(board,r,col,y))continue;int row=y;while(!XgMiniBoard.Collides(board,r,col,row+1))row++;
                var test=(int[,])board.Clone();for(int py=0;py<r.GetLength(0);py++)for(int px=0;px<r.GetLength(1);px++)if(r[py,px]!=0)test[row+py,col+px]=1;
                int cleared=XgMiniBoard.ClearRows(test),height=0,holes=0;for(int xx=0;xx<10;xx++){bool occupied=false;for(int yy=0;yy<22;yy++){if(test[yy,xx]!=0){if(!occupied)height+=22-yy;occupied=true;}else if(occupied)holes++;}}
                options.Add(Tuple.Create(cleared*100.0-holes*25-height,(int[,])r.Clone(),col,row));
            }
            if(options.Count==0)return;options.Sort((a,b)=>b.Item1.CompareTo(a.Item1));int rank=C.Random.NextDouble()<C.Skill?0:C.Random.Next(options.Count);var p=options[rank];suggestion=p.Item2;sx=p.Item3;sy=p.Item4;Draw();C.Say((XgUi.T("紫色是灵光模拟的建议落点；你仍需自己旋转和移动。 ","Purple marks LingGuang's suggested landing; you still rotate and move the piece yourself.")).Replace(XgUi.T("灵光","LingGuang"), C.Sim.AiDisplayName));
        }
        public override void Tick(float dt)
        {
            if(C.KeyDown(Key.P)){paused=!paused;Draw();}if(over||paused)return;
            if(C.KeyDown(Key.UpArrow)||C.KeyDown(Key.X))Rotate();if(C.KeyDown(Key.Space)){Drop();return;}if(C.KeyDown(Key.C))Hold();
            repeat-=dt;if(C.KeyDown(Key.LeftArrow)||C.KeyDown(Key.RightArrow))repeat=0;
            if(repeat<=0){if(C.KeyHeld(Key.LeftArrow))Move(-1,0);else if(C.KeyHeld(Key.RightArrow))Move(1,0);repeat=.12f;}
            fall+=dt;float interval=C.KeyHeld(Key.DownArrow)?.055f:Math.Max(.1f,.8f-lines/8*.07f);
            if(fall>=interval){fall=0;if(!Move(0,1))Lock();}
        }
    }
}
