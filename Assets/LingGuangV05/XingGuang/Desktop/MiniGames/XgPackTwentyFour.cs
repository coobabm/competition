using System;
using System.Collections.Generic;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgTwentyFourPack : XgPackQuestion
    {
        readonly List<double> values=new List<double>();
        readonly List<string> expressions=new List<string>();
        int a=-1,b=-1;
        bool possible, building;
        string proof;
        public XgTwentyFourPack(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            values.Clear(); expressions.Clear();original.Clear();a=b=-1;building=false;
            for(int i=0;i<4;i++){int n=C.Random.Next(1,14);values.Add(n);expressions.Add(n.ToString());}
            possible=Solve(values,expressions,out proof);GuessBinary(possible,"能","Yes","不能","No");
            promptZh=string.Join("，",expressions)+" 能凑出24吗？";promptEn="Can "+string.Join(", ",expressions)+" make 24?";
            Paint();
        }
        void Paint()
        {
            Header("算 24","Make 24","每张牌只能用一次。把两张牌拖到 A / B，再选运算；也可依次点击。","Use every card once. Drag two cards onto A / B, then choose an operation; tapping works too.");
            ShowAi(45,66,810);
            for(int i=0;i<values.Count;i++)
            {
                int k=i;var card=B("Number"+i,45+i*210,95,190,90,expressions[i],()=>Select(k),26);
                card.image.color=i==a||i==b?new Color32(235,218,255,255):Color.white;
                C.Pointer(card.rt,null,null,p=>{if(!building)return;if(45+k*210+p.x<450)a=k;else b=k;if(a==b)b=-1;Paint();});
            }
            if(!building)
            {
                B("Yes",165,260,260,85,XgUi.T("能","Yes"),()=>{chosenText=XgUi.T("能","Yes");if(!possible){Submit(false,"这四张牌不能凑出24。","These cards cannot make 24.");return;}building=true;Paint();},30);
                B("No",475,260,260,85,XgUi.T("不能","No"),()=>{chosenText=XgUi.T("不能","No");Submit(!possible,possible?proof+" = 24":"这四张牌不能凑出24。",possible?proof+" = 24":"These cards cannot make 24.");},30);
                return;
            }
            T("A",100,205,310,55,"A: "+(a>=0?expressions[a]:XgUi.T("拖一张牌到这里","Drag a card here")),23);
            T("B",485,205,310,55,"B: "+(b>=0?expressions[b]:XgUi.T("再拖一张","Drag another card")),23);
            string[] signs={"+","−","×","÷"};for(int i=0;i<4;i++){int op=i;B("Op"+i,120+i*170,290,145,65,signs[i],()=>Combine(op),32);}
            B("RestartExpression",325,370,250,45,XgUi.T("重排这四张牌","Reset these cards"),ResetExpression,18);
        }
        readonly List<double> original=new List<double>();
        void ResetExpression()
        {
            // The original four numbers are captured on the first combine, never replaced by a new question.
            if(original.Count==4){values.Clear();expressions.Clear();foreach(double n in original){values.Add(n);expressions.Add(n.ToString());}}
            a=b=-1;Paint();
        }
        void Select(int k)
        {
            if(!building||submitted)return;
            if(a==k)a=-1;else if(b==k)b=-1;else if(a<0)a=k;else b=k;Paint();
        }
        void Combine(int op)
        {
            if(submitted||a<0||b<0||a==b)return;
            if(values.Count==4){original.Clear();original.AddRange(values);}
            double x=values[a],y=values[b];if(op==3&&Math.Abs(y)<1e-9){C.Say(XgUi.T("不能除以零。","Cannot divide by zero."));return;}
            double n=op==0?x+y:op==1?x-y:op==2?x*y:x/y;
            string expr="("+expressions[a]+new[]{"+","−","×","÷"}[op]+expressions[b]+")";
            int hi=Math.Max(a,b),lo=Math.Min(a,b);values.RemoveAt(hi);values.RemoveAt(lo);expressions.RemoveAt(hi);expressions.RemoveAt(lo);
            values.Add(n);expressions.Add(expr);a=b=-1;
            if(values.Count==1){chosenText=expr+" = "+n.ToString("0.###");bool ok=Math.Abs(n-24)<1e-6;Submit(ok,ok?expr+" = 24":expr+" = "+n.ToString("0.###")+"；可用 "+proof,ok?expr+" = 24":expr+" = "+n.ToString("0.###")+"; try "+proof);}
            else Paint();
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
