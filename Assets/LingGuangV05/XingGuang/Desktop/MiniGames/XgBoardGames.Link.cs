using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    internal sealed class XgLinkGame:XgBoardMini
    {
        static readonly string[] Words={"太阳","月亮","星星","雪花","下雨","闪电","火","树","花","苹果","西瓜","香蕉","葡萄","鱼","猫","兔子","蜜蜂","蝴蝶","书","电话","钥匙","眼镜","雨伞","电脑","相机","礼物","时钟","气球","足球","咖啡"};
        readonly XgBtn[] tiles=new XgBtn[96];readonly XgMiniBoardIcon[] icons=new XgMiniBoardIcon[96];readonly Image[] segments=new Image[100];int[,] board=new int[8,12];
        int selected=-1,matched,proposalA=-1,proposalB=-1,proposals;float seconds,lineTime;bool over;TMP_Text stats,proposal;XgBtn yes,no;
        const float BX=165,BY=78,CW=59,CH=47;
        public XgLinkGame(XgMiniContext c):base(c){}
        public override void Build()
        {
            Frame("词图连连看","点对应的词与图；连线最多拐两次，可经过外围空白。灵光预连线需你审核。");
            Status.rectTransform.offsetMin=new Vector2(172,-479);Status.rectTransform.offsetMax=new Vector2(874,-454);Status.fontSize=15;
            stats=C.Text("Stats",18,90,145,65,"",18);stats.textWrappingMode=TextWrappingModes.Normal;
            C.Button("Shuffle",18,172,140,40,"洗牌 −5秒",()=>Mix(false));C.Button("Hint",18,222,140,40,"提示",Hint);
            proposal=C.Text("Proposal",18,276,140,66,"",15);proposal.textWrappingMode=TextWrappingModes.Normal;
            yes=C.Button("Approve",18,352,65,36,"对",()=>Review(true));no=C.Button("Reject",92,352,65,36,"错",()=>Review(false));
            C.Button("Restart",18,413,140,39,"重新开始",Reset);
            for(int r=1;r<7;r++)for(int col=1;col<11;col++)
            {int n=r*12+col;tiles[n]=C.Button("Pair"+n,BX+col*CW,BY+r*CH,CW-3,CH-3,"",()=>Select(n),18);
             var go=new GameObject("Symbol",typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(tiles[n].rt,false);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(7,4);rt.offsetMax=new Vector2(-7,-4);icons[n]=go.AddComponent<XgMiniBoardIcon>();icons[n].raycastTarget=false;}
            for(int n=0;n<100;n++){segments[n]=C.Box("Link"+n,0,0,1,1,Green);segments[n].gameObject.SetActive(false);}Reset();
        }
        void Reset(){Array.Clear(board,0,board.Length);int[] values=new int[60];for(int i=0;i<30;i++){values[2*i]=i+1;values[2*i+1]=-i-1;}Shuffle(values);int n=0;for(int r=1;r<7;r++)for(int col=1;col<11;col++)board[r,col]=values[n++];matched=0;seconds=150;selected=-1;over=false;proposals=0;proposalA=proposalB=-1;HideLine();Mix(true);Propose();Draw();}
        int Value(int n)=>board[n/12,n%12];
        bool Pair(int a,int b)=>a!=b&&Value(a)!=0&&Value(a)==-Value(b);
        List<int[]> Available(bool truthOnly)
        {var result=new List<int[]>();for(int a=0;a<96;a++)if(Value(a)>0)for(int b=0;b<96;b++)if(Value(b)<0&&(!truthOnly||Pair(a,b))&&XgMiniBoard.LinkPath(board,a,b)!=null)result.Add(new[]{a,b});return result;}
        void Draw()
        {
            for(int n=0;n<96;n++)if(tiles[n]!=null){int v=Value(n);tiles[n].rt.gameObject.SetActive(v!=0);tiles[n].label.text=v>0?Words[v-1]:"";icons[n].gameObject.SetActive(v<0);if(v<0)icons[n].SetIcon(-v-1);tiles[n].button.interactable=!over;tiles[n].image.color=n==selected?new Color(.73f,.93f,.79f):Color.white;tiles[n].label.color=Dark;}
            stats.text="剩 "+Math.Ceiling(seconds)+" 秒\n已配 "+matched+" / 30";yes.button.interactable=no.button.interactable=!over&&proposalA>=0;
            if(proposalA<0)proposal.text="灵光预连线已审核。";
        }
        void Select(int n)
        {
            if(over)return;if(n==proposalA||n==proposalB){Message("这两个图块是灵光的预连线，请先审核左侧对/错。 ");return;}
            if(selected<0){selected=n;Draw();Status.text=Value(n)>0?"已选："+Words[Value(n)-1]:"已选图卡，寻找对应的词。";return;}
            int a=selected;selected=-1;if(a==n){Draw();return;}
            if(!Pair(a,n)){Message("这两个不是对应的词图。 ");Draw();return;}
            var path=XgMiniBoard.LinkPath(board,a,n);if(path==null){Message("语义正确，但路径被挡住了（最多两拐）。 ");Draw();return;}
            Match(a,n,path);CheckDead();
        }
        void Match(int a,int b,List<int> path){ShowLine(path,Green);board[a/12,a%12]=board[b/12,b%12]=0;matched++;seconds=Math.Min(150,seconds+1.5f);C.Report(true);Status.text="配对成功："+matched+" / 30";if(matched==30){over=true;Message("全部连完！30 组词图已核对。 ");}Draw();}
        void Propose()
        {
            if(proposals>=4||over){proposalA=proposalB=-1;Draw();return;}var options=Available(C.Random.NextDouble()<C.Skill);
            if(options.Count==0){proposalA=proposalB=-1;return;}var pick=options[C.Random.Next(options.Count)];proposalA=pick[0];proposalB=pick[1];proposals++;proposal.text="灵光预连 "+proposals+" / 4\n紫线：配对正确吗？";ShowLine(XgMiniBoard.LinkPath(board,proposalA,proposalB),new Color(.65f,.3f,.85f),30);Draw();
        }
        void Review(bool accept)
        {
            if(over||proposalA<0)return;int a=proposalA,b=proposalB;bool truth=Pair(a,b);proposalA=proposalB=-1;
            if(accept&&truth)Match(a,b,XgMiniBoard.LinkPath(board,a,b));else {C.Report(accept==truth);Message(accept==truth?"拆对了，这不是同一组词图。":"审核错误，这组"+(truth?"本来对应。":"并不对应。"));}
            Propose();CheckDead();Draw();
        }
        void CheckDead(){if(over||proposalA>=0)return;if(Available(true).Count==0)Mix(true);}
        void Mix(bool automatic)
        {
            if(over||proposalA>=0)return;var positions=new List<int>();for(int n=0;n<96;n++)if(Value(n)!=0)positions.Add(n);var values=new int[positions.Count];for(int i=0;i<values.Length;i++)values[i]=Value(positions[i]);
            for(int attempt=0;attempt<80;attempt++){Shuffle(values);for(int i=0;i<values.Length;i++)board[positions[i]/12,positions[i]%12]=values[i];if(Available(true).Count>0)break;}
            if(!automatic)seconds=Math.Max(0,seconds-5);selected=-1;HideLine();Draw();Status.text=automatic?"棋盘已就绪。":"已洗牌，扣除 5 秒。";
        }
        void Hint(){if(over)return;if(proposalA>=0){ShowLine(XgMiniBoard.LinkPath(board,proposalA,proposalB),new Color(.65f,.3f,.85f),30);return;}var options=Available(C.Random.NextDouble()<C.Skill);if(options.Count==0)return;var p=options[C.Random.Next(options.Count)];ShowLine(XgMiniBoard.LinkPath(board,p[0],p[1]),Blue);C.Say("这是灵光的模拟建议，请自行核对。 ");}
        void HideLine(){foreach(var s in segments)s.gameObject.SetActive(false);lineTime=0;}
        void ShowLine(List<int> path,Color color,float duration=1.2f)
        {HideLine();if(path==null)return;for(int i=1;i<path.Count&&i<=segments.Length;i++){int a=path[i-1],b=path[i];float ax=BX+(a%12+.5f)*CW,ay=BY+(a/12+.5f)*CH,bx=BX+(b%12+.5f)*CW,by=BY+(b/12+.5f)*CH;var rt=segments[i-1].rectTransform;rt.offsetMin=new Vector2(Math.Min(ax,bx)-2,-Math.Max(ay,by)-2);rt.offsetMax=new Vector2(Math.Max(ax,bx)+2,-Math.Min(ay,by)+2);segments[i-1].color=color;segments[i-1].gameObject.SetActive(true);}lineTime=duration;}
        public override void Tick(float dt){if(over)return;seconds=Math.Max(0,seconds-dt);if(lineTime>0){lineTime-=dt;if(lineTime<=0)HideLine();}if(seconds<=0){over=true;C.Report(false);Message("时间到，已完成 "+matched+" / 30。可重新开始。 ");}Draw();}
    }
    /// <summary>Small native vector pictograms; no font-dependent emoji or external assets.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class XgMiniBoardIcon:MaskableGraphic
    {
        int icon=-1;VertexHelper helper;
        public void SetIcon(int value){if(icon==value)return;icon=value;SetVerticesDirty();}
        Vector2 P(float x,float y)=>new Vector2(rectTransform.rect.xMin+x*rectTransform.rect.width,rectTransform.rect.yMin+y*rectTransform.rect.height);
        void Tri(Vector2 a,Vector2 b,Vector2 c,Color col){int n=helper.currentVertCount;helper.AddVert(a,col,Vector2.zero);helper.AddVert(b,col,Vector2.zero);helper.AddVert(c,col,Vector2.zero);helper.AddTriangle(n,n+1,n+2);}
        void Line(float ax,float ay,float bx,float by,Color col,float width=.045f){Vector2 a=P(ax,ay),b=P(bx,by),d=(b-a).normalized;Vector2 n=new Vector2(-d.y,d.x)*rectTransform.rect.height*width*.5f;Tri(a+n,b+n,b-n,col);Tri(a+n,b-n,a-n,col);}
        void Rect(float x,float y,float w,float h,Color col){var a=P(x,y);var b=P(x+w,y);var c=P(x+w,y+h);var d=P(x,y+h);Tri(a,b,c,col);Tri(a,c,d,col);}
        void Circle(float x,float y,float r,Color col){Vector2 p=P(x,y);for(int i=0;i<20;i++){float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;Tri(p,P(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r),P(x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r),col);}}
        void T(float ax,float ay,float bx,float by,float cx,float cy,Color col)=>Tri(P(ax,ay),P(bx,by),P(cx,cy),col);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();helper=vh;Color ink=new Color(.15f,.22f,.3f),gold=new Color(.95f,.65f,.12f),green=new Color(.15f,.6f,.32f),red=new Color(.85f,.23f,.23f),blue=new Color(.18f,.5f,.8f),white=Color.white;
            switch(icon)
            {
                case 0:Circle(.5f,.5f,.22f,gold);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(.5f+Mathf.Cos(a)*.28f,.5f+Mathf.Sin(a)*.28f,.5f+Mathf.Cos(a)*.4f,.5f+Mathf.Sin(a)*.4f,gold);}break;
                case 1:Circle(.5f,.5f,.36f,gold);Circle(.66f,.65f,.32f,white);break;
                case 2:for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5+Mathf.PI/2,b=a+Mathf.PI*2/5;T(.5f+Mathf.Cos(a)*.42f,.5f+Mathf.Sin(a)*.42f,.5f+Mathf.Cos(b)*.18f,.5f+Mathf.Sin(b)*.18f,.5f,.5f,gold);}break;
                case 3:for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Line(.5f,.5f,.5f+Mathf.Cos(a)*.4f,.5f+Mathf.Sin(a)*.4f,blue);}break;
                case 4:Circle(.35f,.68f,.2f,blue);Circle(.6f,.7f,.23f,blue);for(int i=0;i<3;i++)Line(.3f+i*.2f,.4f,.23f+i*.2f,.15f,blue);break;
                case 5:T(.55f,.95f,.2f,.45f,.55f,.45f,gold);T(.45f,.55f,.8f,.55f,.45f,.05f,gold);break;
                case 6:T(.45f,.98f,.17f,.24f,.8f,.24f,red);Circle(.48f,.3f,.29f,red);T(.5f,.6f,.35f,.15f,.65f,.15f,gold);break;
                case 7:Rect(.44f,.07f,.13f,.35f,ink);T(.5f,.94f,.08f,.29f,.92f,.29f,green);T(.5f,.98f,.18f,.53f,.82f,.53f,green);break;
                case 8:Line(.5f,.1f,.5f,.6f,green);for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Circle(.5f+Mathf.Cos(a)*.19f,.64f+Mathf.Sin(a)*.19f,.15f,red);}Circle(.5f,.64f,.12f,gold);break;
                case 9:Circle(.35f,.44f,.27f,red);Circle(.65f,.44f,.27f,red);Line(.5f,.6f,.55f,.92f,green,.1f);break;
                case 10:Circle(.5f,.48f,.4f,green);Circle(.5f,.48f,.32f,red);for(int i=0;i<5;i++)Circle(.3f+i*.1f,.4f+(i%2)*.17f,.026f,ink);break;
                case 11:Circle(.5f,.5f,.38f,gold);Circle(.4f,.65f,.33f,white);break;
                case 12:for(int row=0;row<3;row++)for(int col=0;col<3-row;col++)Circle(.28f+col*.23f+row*.11f,.7f-row*.24f,.15f,new Color(.6f,.3f,.7f));Line(.5f,.85f,.6f,.99f,green);break;
                case 13:Circle(.48f,.5f,.3f,blue);T(.66f,.5f,.95f,.8f,.95f,.2f,blue);Circle(.34f,.59f,.045f,ink);break;
                case 14:Circle(.5f,.43f,.33f,gold);T(.2f,.55f,.14f,.98f,.45f,.72f,gold);T(.6f,.72f,.86f,.98f,.82f,.55f,gold);Circle(.38f,.49f,.04f,ink);Circle(.64f,.49f,.04f,ink);T(.45f,.34f,.55f,.34f,.5f,.25f,ink);break;
                case 15:Rect(.24f,.55f,.16f,.43f,new Color(.85f,.7f,.72f));Rect(.62f,.55f,.16f,.43f,new Color(.85f,.7f,.72f));Circle(.5f,.38f,.32f,new Color(.85f,.7f,.72f));Circle(.39f,.45f,.04f,ink);Circle(.65f,.45f,.04f,ink);break;
                case 16:Circle(.35f,.71f,.19f,blue);Circle(.65f,.71f,.19f,blue);Circle(.5f,.43f,.3f,gold);Rect(.34f,.38f,.32f,.1f,ink);Rect(.34f,.53f,.32f,.08f,ink);break;
                case 17:Circle(.27f,.65f,.25f,new Color(.7f,.3f,.8f));Circle(.73f,.65f,.25f,new Color(.7f,.3f,.8f));Circle(.3f,.3f,.19f,red);Circle(.7f,.3f,.19f,red);Line(.5f,.12f,.5f,.86f,ink,.09f);break;
                case 18:Rect(.1f,.14f,.36f,.72f,blue);Rect(.53f,.14f,.36f,.72f,blue);for(int i=0;i<3;i++){Line(.18f,.3f+i*.15f,.39f,.3f+i*.15f,white);Line(.61f,.3f+i*.15f,.82f,.3f+i*.15f,white);}break;
                case 19:Rect(.23f,.08f,.54f,.83f,ink);Rect(.3f,.25f,.4f,.55f,blue);Circle(.5f,.17f,.05f,white);break;
                case 20:Circle(.28f,.65f,.22f,gold);Circle(.28f,.65f,.12f,white);Line(.4f,.52f,.83f,.17f,gold,.12f);Line(.68f,.29f,.78f,.44f,gold,.12f);break;
                case 21:Circle(.25f,.5f,.22f,ink);Circle(.75f,.5f,.22f,ink);Circle(.25f,.5f,.16f,white);Circle(.75f,.5f,.16f,white);Line(.42f,.52f,.58f,.52f,ink);break;
                case 22:T(.5f,.9f,.06f,.49f,.94f,.49f,red);Line(.5f,.6f,.5f,.15f,ink);Line(.5f,.15f,.35f,.1f,ink);break;
                case 23:Rect(.07f,.28f,.86f,.63f,ink);Rect(.13f,.35f,.74f,.48f,blue);Rect(.43f,.11f,.14f,.2f,ink);Rect(.25f,.08f,.5f,.07f,ink);break;
                case 24:Rect(.05f,.18f,.9f,.61f,ink);Rect(.15f,.76f,.23f,.13f,ink);Circle(.55f,.49f,.24f,blue);Circle(.55f,.49f,.14f,white);break;
                case 25:Rect(.15f,.1f,.7f,.64f,red);Rect(.08f,.67f,.84f,.17f,red);Rect(.45f,.1f,.1f,.76f,gold);Circle(.36f,.88f,.13f,gold);Circle(.64f,.88f,.13f,gold);break;
                case 26:Circle(.5f,.5f,.43f,ink);Circle(.5f,.5f,.36f,white);Line(.5f,.5f,.5f,.8f,ink);Line(.5f,.5f,.75f,.35f,ink);break;
                case 27:Circle(.5f,.65f,.31f,red);Line(.5f,.35f,.45f,.05f,ink);T(.5f,.38f,.43f,.28f,.57f,.28f,red);break;
                case 28:Circle(.5f,.5f,.43f,ink);Circle(.5f,.5f,.37f,white);Circle(.5f,.5f,.14f,ink);for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Line(.5f+Mathf.Cos(a)*.15f,.5f+Mathf.Sin(a)*.15f,.5f+Mathf.Cos(a)*.4f,.5f+Mathf.Sin(a)*.4f,ink);}break;
                case 29:Rect(.17f,.21f,.55f,.52f,ink);Circle(.77f,.5f,.18f,ink);Circle(.77f,.5f,.1f,white);Line(.1f,.12f,.9f,.12f,ink);Line(.4f,.81f,.45f,.98f,red);break;
            }
        }
    }
}
