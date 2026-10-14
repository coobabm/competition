using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public sealed class XgMiniGoPuzzle
    {
        public readonly string Name,Type,Goal,Why,NameEn,GoalEn,WhyEn; public readonly int X,Y,Difficulty; readonly string[] rows;
        // English strings are plain data; the desktop layer picks zh/en through XgUi.T.
        public XgMiniGoPuzzle(string name,string type,int difficulty,int x,int y,string goal,string why,string[] rows,string nameEn=null,string goalEn=null,string whyEn=null)
        {Name=name;Type=type;Difficulty=difficulty;X=x;Y=y;Goal=goal;Why=why;this.rows=rows;NameEn=nameEn??name;GoalEn=goalEn??goal;WhyEn=whyEn??why;}
        public int[,] Board(){var b=new int[9,9];for(int y=0;y<9;y++)for(int x=0;x<9;x++)b[y,x]=rows[y][x]=='X'?1:rows[y][x]=='O'?2:0;return b;}
        public bool Judge(int[,] original,int x,int y)
        {
            if(!XgMiniBoard.GoPlay(original,x,y,1,out var b,out _))return false;
            int libs=XgMiniBoard.Liberties(b,X,Y);
            switch(Type)
            {
                case "capture":return b[Y,X]!=2;
                case "atari":return b[Y,X]!=2||libs==1;
                case "escape":return b[Y,X]==1&&libs>=2;
                case "double":
                    var seen=new HashSet<int>();int groups=0;
                    for(int gy=0;gy<9;gy++)for(int gx=0;gx<9;gx++)if(b[gy,gx]==2&&!seen.Contains(gy*9+gx))
                    {var g=XgMiniBoard.GoGroup(b,gx,gy,out var l);foreach(int s in g)seen.Add(s);if(l.Count==1)groups++;}return groups>=2;
                case "ladder":return b[Y,X]==2&&libs==1&&XgMiniBoard.Liberties(b,x,y)>=2&&Ladder(b,0);
                case "kill":return b[Y,X]!=2||WhiteLoses(b);
                default:return false;
            }
        }
        bool KillsNow(int[,] b)
        {for(int y=0;y<9;y++)for(int x=0;x<9;x++)if(XgMiniBoard.GoPlay(b,x,y,1,out var n,out _)&&n[Y,X]!=2)return true;return false;}
        bool WhiteLoses(int[,] b)
        {
            if(!KillsNow(b))return false;
            for(int y=0;y<9;y++)for(int x=0;x<9;x++)if(XgMiniBoard.GoPlay(b,x,y,2,out var n,out _)&&!KillsNow(n))return false;
            return true;
        }
        bool Ladder(int[,] b,int depth)
        {
            if(depth>40)return false;if(b[Y,X]!=2)return true;XgMiniBoard.GoGroup(b,X,Y,out var libs);if(libs.Count!=1)return false;
            int last=0;foreach(int s in libs)last=s;
            if(!XgMiniBoard.GoPlay(b,last%9,last/9,2,out var escaped,out int capture))return true;if(capture>0)return false;
            XgMiniBoard.GoGroup(escaped,X,Y,out var next);if(next.Count>=3)return false;if(next.Count==1)return true;
            foreach(int s in next)if(XgMiniBoard.GoPlay(escaped,s%9,s/9,1,out var attack,out _)&&XgMiniBoard.Liberties(attack,s%9,s/9)>=2&&XgMiniBoard.Liberties(attack,X,Y)==1&&Ladder(attack,depth+1))return true;
            return false;
        }
        public static readonly XgMiniGoPuzzle[] All = {
new XgMiniGoPuzzle("提子入门","capture",1,4,4,"吃掉 ▲ 白子","白子只剩一口气。占住最后一口气，就把它提掉了。",new[]{".........",".........",".........","....X....","...XOX...",".........",".........",".........","........."},"Capture Basics","Capture the white stone marked ▲","The white stone has only one liberty left. Take that last liberty and it is captured."),
new XgMiniGoPuzzle("叫吃","atari",1,3,4,"叫吃 ▲ 白棋（让它只剩一口气）","白棋有两口气，堵住任意一口就是「叫吃」：它只剩一口气了。",new[]{".........",".........",".........","...XX....","..XOOX...",".........",".........",".........","........."},"Atari","Atari the white stones marked ▲ (leave them one liberty)","White has two liberties. Block either one and it is in \"atari\": only one liberty left."),
new XgMiniGoPuzzle("提两子","capture",1,3,4,"吃掉 ▲ 两个白子","两子连在一起，共用同一口气。堵住它，两子一起被提。",new[]{".........",".........",".........","...XX....","..XOOX...","...X.....",".........",".........","........."},"Capture Two","Capture the two white stones marked ▲","The two stones are connected and share the same liberty. Block it and both are captured together."),
new XgMiniGoPuzzle("边上提子","capture",1,0,4,"吃掉 ▲ 白棋","靠边的棋气少。别忘了数边上，最后一口气在 (A, 7)。",new[]{".........",".........",".........","X........","OX.......","OX.......",".........",".........","........."},"Edge Capture","Capture the white stones marked ▲","Stones on the edge have fewer liberties. Remember to count along the edge: the last liberty is at (A, 7)."),
new XgMiniGoPuzzle("角上提子","capture",1,0,0,"吃掉角上的 ▲ 白棋","角上两子，最后一口气是 (A, 2)。角上的棋最容易被吃。",new[]{"OOX......",".X.......",".........",".........",".........",".........",".........",".........","........."},"Corner Capture","Capture the white stones in the corner marked ▲","Two stones in the corner; the last liberty is (A, 2). Stones in the corner are the easiest to capture."),
new XgMiniGoPuzzle("逃跑","escape",2,4,4,"▲ 黑子被叫吃了，救它！","被叫吃就往空的地方「长」：长出去后黑棋有三口气，白棋追不上了。",new[]{".........",".........",".........","....O....","...OXO...",".........",".........",".........","........."},"Escape","The black stone marked ▲ is in atari. Save it!","When in atari, \"extend\" into open space: after extending, Black has three liberties and White cannot catch it."),
new XgMiniGoPuzzle("边上逃跑","escape",2,1,4,"▲ 黑子被叫吃了，救它！","只有一个方向可以长，长出去后气比原来多。",new[]{".........",".........",".........",".O.......","OXO......",".........",".........",".........","........."},"Edge Escape","The black stone marked ▲ is in atari. Save it!","There is only one direction to extend, and the extended stone has more liberties than before."),
new XgMiniGoPuzzle("先吃再逃","escape",3,4,4,"▲ 黑子被叫吃了，救它！（先别急着长）","往下长只会剩一口气，照样被吃。先看上面：那颗白子也只剩一口气，先吃掉它，黑棋自然多出气来。",new[]{".........",".........",".........","...XOX...","...OXO...","...O.O...",".........",".........","........."},"Capture, Then Escape","The black stone marked ▲ is in atari. Save it! (Don't rush to extend)","Extending downward leaves only one liberty and it is still captured. Look above first: that white stone also has just one liberty. Capture it and Black gains liberties naturally."),
new XgMiniGoPuzzle("双叫吃","double",2,3,4,"一手同时叫吃两块白棋","两块白棋共有一个气点。落在那里，它们同时只剩一口气，白棋只能救一块。「第四局，神之一手」就是这样的一击。",new[]{".........",".........",".........","...X.X...","..XO.OX..",".........",".........",".........","........."},"Double Atari","One move that puts both white groups in atari","The two white groups share one liberty. Play there and both are left with a single liberty, so White can only save one. The famous \"divine move\" of Game 4 was a strike like this."),
new XgMiniGoPuzzle("征子","ladder",3,4,3,"叫吃 ▲ 白子，并且白棋怎么逃都逃不掉（征子）","征子：连续叫吃，白棋一路长，永远只有两口气，最后被逼出去提光。方向选错，白棋长出三口气就逃走了。",new[]{".........",".........","....X....","..X.O....","....X....",".........",".........",".........","........."},"Ladder","Atari the white stone marked ▲ so that White cannot escape however it runs (a ladder)","A ladder: keep ataring, White keeps extending and always has just two liberties, until it is driven out and captured. Pick the wrong direction and White extends to three liberties and escapes."),
new XgMiniGoPuzzle("征子 2","ladder",3,2,4,"叫吃 ▲ 白子，让它逃不掉（征子）","先看白棋长出去之后，是不是始终只有两口气。右边那颗黑子是接应。",new[]{".........",".........","..X.....X",".........",".XO......","..X......",".........",".........","........."},"Ladder 2","Atari the white stone marked ▲ so it cannot escape (a ladder)","First check whether White, after extending, always has only two liberties. The black stone on the right is the supporting stone."),
new XgMiniGoPuzzle("叫吃的方向","kill",2,0,0,"吃掉角上的 ▲ 白子（选对叫吃的方向）","从 (A, 2) 叫吃：白子只能往 (B, 1) 长，长出去还是被吃。从 (B, 1) 叫吃，白子长到 (A, 2)，反而有两口气。",new[]{"O.X......","..X......",".........",".........",".........",".........",".........",".........","........."},"Direction of Atari","Capture the white stone in the corner marked ▲ (choose the right direction to atari from)","Atari from (A, 2): White can only extend to (B, 1) and is still captured. Atari from (B, 1): White extends to (A, 2) and ends up with two liberties instead."),
new XgMiniGoPuzzle("叫吃的方向·边","kill",2,1,1,"吃掉 ▲ 三个白子（选对叫吃的方向）","白棋靠着边，只能往一边长。叫吃要朝着它长不出去的方向。",new[]{".X.......",".O.......","XOX......","XOX......",".X.......",".........",".........",".........","........."},"Direction of Atari: Edge","Capture the three white stones marked ▲ (choose the right direction to atari from)","White hugs the edge and can only extend one way. Atari it from the side that stops it from extending."),
};
}
}
