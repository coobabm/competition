using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    public static class XgSocialGames
    {
        public static XgMiniGame Create(string id, XgMiniContext c)
        {
            switch(id) {
                case "esp": return new XgEspGame(c);
                case "captcha12306": return new XgCaptchaGame(c);
                case "feihualing": return new XgPoetryGame(c);
                case "doutu": return new XgMemeGame(c);
                case "zhaoshuijun": return new XgReviewGame(c);
                case "shuishiwodi": return new XgUndercoverGame(c);
                case "kaixincidian": return new XgQuizGame(c);
                case "chuanhua": return new XgTranslationGame(c);
                case "turing": return new XgTuringGame(c);
                case "qingwen": return new XgQingwenGame(c);
                default: return null;
            }
        }
    }

    /// <summary>Only layout and lifecycle helpers are shared; each game owns its own rules/state machine.</summary>
    abstract class XgSocialSurface : XgMiniGame
    {
        protected int version;
        protected bool disposed;
        protected TMP_Text timer;
        protected readonly Color purple = new Color32(115,78,184,255), green = new Color32(34,126,93,255), red = new Color32(174,49,56,255);
        protected XgSocialSurface(XgMiniContext c):base(c) { }
        protected void Clear(string title,string rule)
        {
            version++; timer=null;
            for(int i=C.Root.childCount-1;i>=0;i--) {
                var g=C.Root.GetChild(i).gameObject; g.SetActive(false); XgMiniContext.DestroyObject(g);
            }
            C.Box("SocialBackground",0,0,900,480,new Color32(243,246,251,255));
            C.Text("Title",18,8,660,35,title,26);
            C.Text("Rule",18,44,862,38,rule,15);
        }
        protected XgBtn B(string id,float x,float y,float w,float h,string text,Action f,float size=20)
        {
            int v=version;
            return C.Button(id,x,y,w,h,text,()=> { if(!disposed && v==version) f(); },size);
        }
        protected TMP_Text T(string id,float x,float y,float w,float h,string text,float size=20,Color? color=null) => C.Text(id,x,y,w,h,text,size,color);
        protected void Shade(float x,float y,float w,float h) => C.Box("Card",x,y,w,h,Color.white);
        protected void Shuffle<TValue>(IList<TValue> list) { for(int i=list.Count-1;i>0;i--) { int j=C.Random.Next(i+1); TValue t=list[i];list[i]=list[j];list[j]=t; } }
        protected int Guess(int truth,int count) => XgMiniContent.Guess(C.Random,C.Skill,truth,count);
        protected void Clock(float left) { if(timer!=null) { int sec=Math.Max(0,(int)Math.Ceiling(left)); timer.text=XgUi.T("剩余 "+sec+" 秒",sec+" s left"); } }
        protected void End(string title,string detail,Action restart)
        {
            Clear(title,XgUi.T("本局结束，来看看成绩。","Round over. Here is how it went."));
            T("Summary",60,125,780,180,detail,25);
            B("Restart",320,365,260,60,XgUi.T("再来一局","Play again"),restart);
        }
        public override void Dispose() { disposed=true;version++; }
    }

    sealed class XgEspGame : XgSocialSurface
    {
        static readonly string[] Moods={"开心","难过","生气","佩服","讽刺","害怕","无奈","感动"},MoodsEn={"Happy","Sad","Angry","Impressed","Sarcastic","Scared","Helpless","Touched"};
        // {chat line (zh), mood (zh key), chat line (en)}; the mood key is looked up in Moods/MoodsEn at display time.
        static readonly string[][] Items={new[]{"哈哈哈哈哈哈","开心","hahahahahaha"},new[]{"蓝瘦香菇","难过","feeling blue, so blue"},new[]{"气死我了！！","生气","I am SO mad!!"},new[]{"这操作我服","佩服","That move? Respect."},new[]{"你们城里人真会玩","讽刺","You city folks sure know how to have fun"},new[]{"吓死宝宝了","害怕","You scared the baby out of me"},new[]{"友谊的小船说翻就翻","无奈","The boat of friendship capsizes in a blink"},new[]{"好暖，泪目了","感动","So warm, I am tearing up"},new[]{"666666","佩服","gg gg gg gg"},new[]{"我也是醉了","无奈","I am speechless, honestly"},new[]{"宝宝心里苦","难过","Baby is hurting inside"},new[]{"233333","开心","lolololol"}};
        static string Mood(string zh)=>XgUi.T(zh,MoodsEn[Array.IndexOf(Moods,zh)]);
        TMP_Text hidden; string[] item,options; int round,hits,labels,truth,ai; float left; bool answered;
        public XgEspGame(XgMiniContext c):base(c){}
        public override void Build(){round=hits=labels=0;Next();}
        void Next(){
            if(round>=12){End(XgUi.T("暗号对照表","Code Cross-check"),XgUi.T($"12 条弹幕：默契 {hits} 次，读对情绪 {labels} 次。\n对上暗号不一定就对，灵光也会猜错。",$"12 comments: {hits} matches, {labels} moods read right.\nMatching codes do not make it right; LingGuang can guess wrong too."),Build);return;}
            round++;answered=false;left=8;item=Items[C.Random.Next(Items.Length)];
            var choices=new List<string>(Moods);choices.Remove(item[1]);Shuffle(choices);choices.RemoveRange(3,choices.Count-3);choices.Add(item[1]);Shuffle(choices);options=choices.ToArray();truth=Array.IndexOf(options,item[1]);ai=Guess(truth,4);
            Clear(XgUi.T("对暗号 · "+round+" / 12","Code Match · "+round+" / 12"),XgUi.T("8 秒内为弹幕选情绪。你先选，灵光再翻牌，看看你们对不对得上暗号。","Pick the mood of the comment within 8 s. You choose first, then LingGuang flips its card: do your codes match?"));
            Shade(25,97,850,130);T("Message",50,110,800,100,XgUi.T(item[0],item[2]),36);
            timer=T("Timer",690,9,200,32,"",19);Clock(left);
            for(int i=0;i<4;i++){int k=i;B("Emotion"+i,30+i*215,250,195,72,(i+1)+"  "+Mood(options[i]),()=>Pick(k));}
            hidden=T("Hidden",50,351,790,80,XgUi.T("灵光已经选好……等你翻牌。","LingGuang has chosen... waiting for you to flip."),23,purple);
        }
        void Pick(int k){if(answered)return;answered=true;if(hidden!=null)hidden.gameObject.SetActive(false);if(k==ai)hits++;if(k==truth)labels++;if(k>=0)C.ReportTimed(k==truth,8-left);
            T("Reveal",35,340,830,95,XgUi.T($"你：{(k<0?"未选择":Mood(options[k]))}   灵光：{Mood(options[ai])}   正解：{Mood(item[1])}\n"+(k==ai?"暗号对上了。":"没有对上，记住这次差别。"),$"You: {(k<0?"no pick":Mood(options[k]))}   LingGuang: {Mood(options[ai])}   Answer: {Mood(item[1])}\n"+(k==ai?"The code matched.":"No match; remember this difference.")),22);
            B("Next",685,428,180,40,XgUi.T("下一条","Next comment"),Next,18);
        }
        public override void Tick(float dt){if(disposed||answered||!C.IsActive)return;left-=dt;Clock(left);if(left<=0){Pick(-1);return;}for(int i=0;i<4;i++)if(C.KeyDown((Key)((int)Key.Digit1+i))){Pick(i);break;}}
    }

    sealed class XgCaptchaGame : XgSocialSurface
    {
        // IDs match the shared native XgMiniBoardIcon atlas; reuse vector art instead of new assets.
        static readonly string[] Names={"太阳","月亮","星星","雪花","下雨","闪电","火","树","花","苹果","西瓜","香蕉","葡萄","鱼","猫","兔子","蜜蜂","蝴蝶","书","电话","钥匙","眼镜","雨伞","电脑","相机","礼物","时钟","气球","足球","咖啡"},
            NamesEn={"Sun","Moon","Star","Snowflake","Rain","Lightning","Fire","Tree","Flower","Apple","Watermelon","Banana","Grapes","Fish","Cat","Rabbit","Bee","Butterfly","Book","Phone","Key","Glasses","Umbrella","Computer","Camera","Gift","Clock","Balloon","Football","Coffee"};
        static readonly string[] Categories={"水果","动物","电子设备","日常用品"},CategoriesEn={"Fruit","Animals","Electronics","Everyday items"};
        static readonly int[][] CategoryItems={new[]{9,10,11,12},new[]{13,14,15,16,17},new[]{19,23,24},new[]{18,20,21,22,25,26,27,28,29}};
        int round,passed,truth,selected,target;readonly int[] items=new int[8];readonly XgBtn[] cells=new XgBtn[8];
        bool specific,done,hinted;float left,hintTime;
        string TargetName=>specific?XgUi.T(Names[target],NamesEn[target]):XgUi.T(Categories[target],CategoriesEn[target]);
        public XgCaptchaGame(XgMiniContext c):base(c){}
        public override void Build(){round=passed=0;Next();}
        void Next(){if(round>=12){End(XgUi.T("抢票验证结束","Ticket captcha finished"),XgUi.T($"通过 {passed} / 12 屏。\n八格都要判断对，选中的和没选的都算。",$"Passed {passed} / 12 screens.\nAll eight cells have to be right, picked or not."),Build);return;}round++;left=8;Generate();}
        bool Matches(int item) => specific ? item==target : Array.IndexOf(CategoryItems[target],item)>=0;
        void Generate(){
            done=false;hinted=false;selected=truth=0;specific=round%3==0;
            target=specific?9+C.Random.Next(21):C.Random.Next(Categories.Length);
            hintTime=.5f+2.2f*(1-(float)C.Skill);
            var yes=new List<int>();var no=new List<int>();
            for(int id=9;id<Names.Length;id++)if(Matches(id))yes.Add(id);else no.Add(id);
            Shuffle(yes);Shuffle(no);
            // Exactly 1–3 matches, and therefore 5–7 distractors on every screen.
            int count=1+C.Random.Next(3);
            for(int i=0;i<8;i++)items[i]=i<count?yes[i%yes.Count]:no[i-count];
            Shuffle(items);
            for(int i=0;i<8;i++)if(Matches(items[i]))truth|=1<<i;
            Clear(XgUi.T("12306 验证码 · "+round+" / 12","12306 Captcha · "+round+" / 12"),XgUi.T("8 秒内点出所有符合要求的生活物件。紫色「灵」是灵光圈的，点不点由你。","Tap every matching everyday object within 8 s. A purple \"LG\" is a cell LingGuang picked; tapping is up to you."));
            T("Ask",25,88,650,38,XgUi.T("请选择全部「"+TargetName+"」","Select all: "+TargetName),27);
            timer=T("Timer",700,90,175,35,"",19);Clock(left);
            for(int i=0;i<8;i++){
                int k=i;float x=25+(i%4)*216,y=138+(i/4)*113;
                cells[i]=B("Cell"+i,x,y,201,100,(i+1)+"",()=>Toggle(k));
                cells[i].image.color=Color.white;cells[i].label.color=XgPalette.Ink;cells[i].label.alignment=TextAlignmentOptions.TopLeft;
                var icon=C.Rect("ObjectIcon"+i,x+70,y+18,61,58).gameObject.AddComponent<XgMiniBoardIcon>();
                icon.raycastTarget=false;icon.SetIcon(items[i]);
                var caption=T("ObjectName"+i,x+40,y+78,121,20,XgUi.T(Names[items[i]],NamesEn[items[i]]),15);
                caption.alignment=TextAlignmentOptions.Center;
            }
            B("Submit",595,385,280,52,XgUi.T("提交验证（Enter）","Submit (Enter)"),()=>Submit(false));
            B("Refresh",25,385,205,52,XgUi.T("换一组图（时间照走）","New image (clock keeps running)"),()=>{if(!done)Generate();},16);
        }
        void Toggle(int i){if(done)return;selected^=1<<i;cells[i].image.color=(selected&(1<<i))!=0?new Color32(190,222,248,255):Color.white;}
        void Submit(bool expired){if(done)return;done=true;bool ok=!expired&&XgMiniSocialRules.ExactMask(selected,truth,8);if(ok)passed++;if(!expired)C.ReportTimed(ok,8-left);
            int missed=0,extra=0;
            for(int i=0;i<8;i++){
                bool match=(truth&(1<<i))!=0,picked=(selected&(1<<i))!=0;
                if(match&&!picked)missed++;if(!match&&picked)extra++;
                cells[i].label.text=(i+1)+" "+(match?(picked?XgUi.T("选对","Correct"):XgUi.T("漏选","Missed")):(picked?XgUi.T("误选","Extra"):XgUi.T("不选","Skip")));
                cells[i].label.color=match==picked?green:red;
            }
            string explanation=expired?XgUi.T("验证码过期","Captcha expired"):ok?XgUi.T("验证通过","Verified"):XgUi.T("漏选 "+missed+" · 误选 "+extra,"Missed "+missed+" · Extra "+extra);
            T("Result",250,383,330,57,explanation+XgUi.T("\n目标：","\nTarget: ")+TargetName,18,ok?green:red);
            B("Next",650,441,225,34,XgUi.T("下一屏","Next screen"),Next,17);
        }
        public override void Tick(float dt){if(disposed||done||!C.IsActive)return;left-=dt;hintTime-=dt;Clock(left);if(left<=0){Submit(true);return;}
            if(!hinted&&hintTime<=0){hinted=true;for(int i=0;i<8;i++){bool truthCell=(truth&(1<<i))!=0;bool says=C.Random.NextDouble()<(.5+.47*C.Skill)?truthCell:!truthCell;if(says){cells[i].label.text=(i+1)+XgUi.T("  灵","  LG");cells[i].label.color=purple;}}C.Say(XgUi.T("我先圈了几个，你看看对不对。","I marked a few. Do they look right to you?"));}
            for(int i=0;i<8;i++)if(C.KeyDown((Key)((int)Key.Digit1+i)))Toggle(i);if(C.KeyDown(Key.Enter))Submit(false);
        }
    }
}
