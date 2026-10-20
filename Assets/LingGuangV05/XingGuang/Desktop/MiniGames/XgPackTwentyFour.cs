using System;
using System.Collections.Generic;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgTwentyFourPack : XgPackQuestion
    {
        readonly List<double> values=new List<double>(), original=new List<double>();
        readonly List<string> expressions=new List<string>();
        readonly Stack<Tuple<double[],string[]>> history=new Stack<Tuple<double[],string[]>>();
        int a=-1,b=-1;
        bool possible,building;
        string proof,message="";
        static readonly string[] Signs={"+","−","×","÷"};
        public XgTwentyFourPack(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            values.Clear();expressions.Clear();original.Clear();history.Clear();a=b=-1;building=false;message="";
            for(int i=0;i<4;i++){int n=C.Random.Next(1,14);values.Add(n);original.Add(n);expressions.Add(n.ToString());}
            possible=Solve(values,expressions,out proof);GuessBinary(possible,"能","Yes","不能","No");
            promptZh=string.Join("，",expressions)+" 能凑出24吗？";promptEn="Can "+string.Join(", ",expressions)+" make 24?";Paint();
        }
        void Paint()
        {
            Header("算 24","Make 24","选两张牌，再选运算。可拖入 A / B；全部用完后提交，可随时撤销。","Pick two cards, then an operation. Or drag to A / B. Use all cards, then submit; undo is available.");
            ShowAi(45,66,810);
            for(int i=0;i<values.Count;i++)
            {
                int k=i;float x=45+i*210;
                var card=B("Number"+i,x,100,190,75,(i==a?"A · ":i==b?"B · ":"")+expressions[i],()=>Select(k),25);
                card.image.color=i==a||i==b?(C.Ui.dark?XgDark.AccentSoft:new Color32(225,214,250,255)):(C.Ui.dark?XgDark.Button:Color.white);
                bool dragging=false;RectTransform ghost=null;
                int generation=version;
                C.Pointer(card.rt,null,p=>{
                    if(!building||submitted||generation!=version)return;
                    dragging=true;
                    if(ghost==null){ghost=C.Box("DragCard",0,0,150,52,C.Ui.dark?XgDark.AccentSoft:new Color32(225,214,250,230)).rectTransform;
                        var label=C.Ui.Text(XgUi.Rect("Value",ghost,Vector2.zero,Vector2.one,new Vector2(8,0),new Vector2(-8,0)),expressions[k],20,C.Ui.dark?XgDark.Ink:Color.black,TMPro.TextAlignmentOptions.Center);label.raycastTarget=false;}
                    ghost.anchoredPosition=new Vector2(x+p.x,-(100+p.y));
                },p=>{
                    if(!dragging||generation!=version)return;
                    Drop(k,new Vector2(x+p.x,100+p.y));
                });
            }
            if(!building)
            {
                B("Yes",165,260,260,85,XgUi.T("能 · 开始组合","Yes · Build it"),()=>{chosenText=XgUi.T("能","Yes");if(!possible){Submit(false,"这四张牌不能凑出24。","These cards cannot make 24.");return;}building=true;Paint();},27);
                B("No",475,260,260,85,XgUi.T("不能","No"),()=>{chosenText=XgUi.T("不能","No");Submit(!possible,possible?proof+" = 24":"这四张牌不能凑出24。",possible?proof+" = 24":"These cards cannot make 24.");},30);return;
            }
            B("SlotA",80,190,330,58,"A: "+(a>=0?expressions[a]:XgUi.T("拖入或点选第一张","Drag or pick first card")),()=>{a=-1;message="";Paint();},21);
            B("SlotB",490,190,330,58,"B: "+(b>=0?expressions[b]:XgUi.T("拖入或点选第二张","Drag or pick second card")),()=>{b=-1;message="";Paint();},21);
            B("Swap",420,199,60,40,"⇄",()=>{int t=a;a=b;b=t;message="";Paint();},24).button.interactable=a>=0||b>=0;
            for(int i=0;i<4;i++){int op=i;var btn=B("Op"+i,120+i*170,262,145,52,Signs[i],()=>Combine(op),30);btn.button.interactable=a>=0&&b>=0&&a!=b;}
            string status=values.Count==1?XgUi.T("算式结果：","Expression result: ")+values[0].ToString("0.###")
                :a<0?XgUi.T("先选择 A","Choose A first"):b<0?XgUi.T("再选择 B；减除按 A → B 运算","Choose B; subtraction and division use A → B")
                :XgUi.T("已选两张牌，选择运算符","Two cards selected; choose an operation");
            T("WorkStatus",45,322,810,33,string.IsNullOrEmpty(message)?status:message,18,string.IsNullOrEmpty(message)?purple:red);
            B("Undo",45,370,160,45,XgUi.T("撤销一步","Undo"),Undo,18).button.interactable=history.Count>0;
            B("ClearSelection",215,370,160,45,XgUi.T("取消选择","Clear picks"),()=>{a=b=-1;message="";Paint();},18).button.interactable=a>=0||b>=0;
            B("RestartExpression",385,370,210,45,XgUi.T("重排这四张牌","Reset these cards"),ResetExpression,18);
            B("SubmitExpression",625,370,230,45,XgUi.T("提交算式","Submit expression"),SubmitExpression,20).button.interactable=values.Count==1;
        }
        internal static int DropSlot(Vector2 point)
        {if(point.y<190||point.y>248)return -1;if(point.x>=80&&point.x<=410)return 0;if(point.x>=490&&point.x<=820)return 1;return -1;}
        void Drop(int k,Vector2 point)
        {
            if(!C.IsActive||submitted||!building)return;
            int slot=DropSlot(point);
            if(slot<0)message=XgUi.T("松手位置不在 A / B 槽内，牌已归位。","Drop inside A or B; the card returned to its place.");
            else {if(slot==0){a=k;if(b==k)b=-1;}else{b=k;if(a==k)a=-1;}message="";}
            Paint();
        }
        void ResetExpression()
        {if(submitted)return;values.Clear();expressions.Clear();foreach(double n in original){values.Add(n);expressions.Add(n.ToString());}history.Clear();a=b=-1;message="";Paint();}
        void Undo()
        {if(submitted||history.Count==0)return;var snapshot=history.Pop();values.Clear();values.AddRange(snapshot.Item1);expressions.Clear();expressions.AddRange(snapshot.Item2);a=b=-1;message="";Paint();}
        void Select(int k)
        {if(!building||submitted)return;if(a==k)a=-1;else if(b==k)b=-1;else if(a<0)a=k;else b=k;message="";Paint();}
        void Combine(int op)
        {
            if(submitted||a<0||b<0||a==b)return;
            double x=values[a],y=values[b];
            if(op==3&&Math.Abs(y)<1e-9){message=XgUi.T("不能除以零。交换 A / B 或重新选牌。","Cannot divide by zero. Swap A / B or choose other cards.");Paint();return;}
            history.Push(Tuple.Create(values.ToArray(),expressions.ToArray()));
            double n=op==0?x+y:op==1?x-y:op==2?x*y:x/y;
            string expr="("+expressions[a]+Signs[op]+expressions[b]+")";
            int hi=Math.Max(a,b),lo=Math.Min(a,b);values.RemoveAt(hi);values.RemoveAt(lo);expressions.RemoveAt(hi);expressions.RemoveAt(lo);
            values.Add(n);expressions.Add(expr);a=b=-1;message="";Paint();
            var fx=C.Root.GetComponentInParent<XgJuice>();
            var resultCard=C.Root.Find("Number"+(values.Count-1)) as RectTransform;
            if(fx!=null&&C.IsActive&&resultCard!=null)fx.Knock(resultCard,.08f);
        }
        void SubmitExpression()
        {
            if(submitted||values.Count!=1)return;double n=values[0];string expr=expressions[0];
            chosenText=expr+" = "+n.ToString("0.###");bool ok=Math.Abs(n-24)<1e-6;
            Submit(ok,ok?expr+" = 24":expr+" = "+n.ToString("0.###")+"；可用 "+proof,ok?expr+" = 24":expr+" = "+n.ToString("0.###")+"; try "+proof);
        }
        internal static bool Solve(List<double> numbers,List<string> forms,out string result)
        {
            if(numbers.Count==1){result=forms[0];return Math.Abs(numbers[0]-24)<1e-6;}
            for(int i=0;i<numbers.Count;i++)for(int j=0;j<numbers.Count;j++)if(i!=j)
            {
                for(int op=0;op<4;op++)
                {
                    if(op==3&&Math.Abs(numbers[j])<1e-9)continue;
                    double n=op==0?numbers[i]+numbers[j]:op==1?numbers[i]-numbers[j]:op==2?numbers[i]*numbers[j]:numbers[i]/numbers[j];
                    var next=new List<double>();var text=new List<string>();for(int k=0;k<numbers.Count;k++)if(k!=i&&k!=j){next.Add(numbers[k]);text.Add(forms[k]);}
                    next.Add(n);text.Add("("+forms[i]+new[]{"+","−","×","÷"}[op]+forms[j]+")");
                    if(Solve(next,text,out result))return true;
                }
            }
            result="";return false;
        }
    }
}
