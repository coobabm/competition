using System;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    internal sealed class XgForgeGame:XgBoardMini
    {
        sealed class Model { public int Level,Heat,Streak;public bool Dead,Saved,Submitted; }
        readonly Model[] models=new Model[3];readonly XgBtn[] choices=new XgBtn[3];readonly bool[] research=new bool[8];readonly XgBtn[] papers=new XgBtn[8];
        static readonly string[] Research={"梯度裁剪：爆炸签 −2","Dropout：涨了签 +4","学习率预热：顿悟签 +1","早停：平台期签 +3","数据增强：退步签转涨了","残差连接：2平台签转涨了","动量：连续2涨额外+1","勤存档：首次爆炸变退步"};
        int selected,charge,day=1,lost;float frenzy,cooldown;bool over;TMP_Text stats,odds;string result="先加载一个模型，再敲击抽签。";
        Model Current=>models[selected];
        public XgForgeGame(XgMiniContext c):base(c){}
        public override void Build()
        {
            Frame("模型锻造 · ForgeTax","加载 → 敲击抽签 → 涨级/平台/退步/爆炸。成功抽签仅提交真实训练请求，不直接改参数。");
            Status.rectTransform.offsetMin=new Vector2(22,-296);Status.rectTransform.offsetMax=new Vector2(422,-151);
            for(int i=0;i<3;i++){int p=i;choices[i]=C.Button("Model"+i,20+i*140,93,130,49,"",()=>{selected=p;Draw();});}
            stats=C.Text("Stats",440,92,425,70,"",18);odds=C.Text("Odds",440,167,425,85,"",17);odds.textWrappingMode=TextWrappingModes.Normal;
            C.Button("Load",20,304,125,45,"加载显卡",()=>{if(!over&&!Current.Dead){Current.Heat=100;Draw();}});
            C.Button("Strike",155,304,130,45,"敲击 / 空格",Strike);
            C.Button("Frenzy",295,304,125,45,"入神 / F",Frenzy);
            C.Text("Papers",440,257,425,26,"投稿当前模型换论文（需 +2，模型消耗）",17);
            for(int i=0;i<8;i++){int p=i;papers[i]=C.Button("Paper"+i,440+i%2*216,288+i/2*38,208,34,Research[i],()=>Submit(p),14);}
            C.Button("NewDay",20,368,195,43,"结束今天 / 补货",NewDay);C.Button("Reset",225,368,195,43,"重新开始",Reset);
            C.Text("Scope",20,420,845,44,"本局模型、论文是锻造小游戏状态；没有独立钱包。主系统负责训练样本、电费、硬件与质量门槛。",16);Reset();
        }
        void Reset(){Array.Clear(research,0,research.Length);day=1;lost=charge=selected=0;frenzy=cooldown=0;over=false;Supply();result="先加载，再敲击抽签。目标：锻造 +18。";Draw();}
        void Supply(){for(int i=0;i<3;i++)models[i]=new Model();}
        void NewDay(){if(over)return;bool allExploded=true;foreach(var m in models)allExploded&=m.Dead&&!m.Submitted;lost=allExploded?lost+1:0;if(lost>=3){over=true;result="连续三批模型报废，本局结束。可重新开始。";Draw();return;}day++;Supply();selected=0;frenzy=0;result="第 "+day+" 天：新到三个模型。";Draw();}
        int[] Tickets(){var m=Current;int[] t={10,2,5,m.Level==0?0:3,m.Level};if(research[0])t[4]=Math.Max(0,t[4]-2);if(research[1])t[0]+=4;if(research[2])t[1]++;if(research[3])t[2]+=3;if(research[4]&&t[3]>0){t[3]--;t[0]++;}if(research[5]){t[2]-=2;t[0]+=2;}return t;}
        void Strike()
        {
            var m=Current;if(over||m.Dead||cooldown>0)return;if(m.Heat<=0){result="显卡凉了，先加载。";Draw();return;}
            cooldown=.16f;var t=Tickets();int total=0;foreach(int w in t)total+=w;int roll=C.Random.Next(total),outcome=0;for(;outcome<4;outcome++){if(roll<t[outcome])break;roll-=t[outcome];}
            if(frenzy>0&&outcome==4)outcome=2;if(frenzy<=0)m.Heat=Math.Max(0,m.Heat-14);
            if(outcome==4&&research[7]&&!m.Saved){m.Saved=true;outcome=3;}
            charge=Math.Min(10,charge+1);int before=m.Level;
            if(outcome<2){m.Streak++;m.Level+=outcome==1?2:1;if(research[6]&&m.Streak%2==0)m.Level++;C.Report(true);}
            else {m.Streak=0;if(outcome==3)m.Level=Math.Max(0,m.Level-1);if(outcome==4)m.Dead=true;}
            result=new[]{"涨了","顿悟","平台期","退步","梯度爆炸"}[outcome]+"：+"+before+" → "+(m.Dead?"报废":"+"+m.Level)+"\n"+(outcome<2?"已提交训练请求；实际结果见主系统反馈。":"本次无训练奖励。 ");
            if(m.Level>=18&&!m.Dead){over=true;result+="\n达到 +18，锻造成功！";}Draw();
        }
        void Frenzy(){if(over||charge<10||frenzy>0)return;charge=0;frenzy=4;result="入神 4 秒：爆炸变平台，显存不降温。";Draw();}
        void Submit(int p){if(over||research[p]||Current.Dead||Current.Level<2)return;research[p]=true;Current.Dead=true;Current.Submitted=true;result="投稿成功："+Research[p]+"\n模型已消耗，论文作用于下一次抽签。";Draw();}
        void Draw()
        {
            for(int i=0;i<3;i++)choices[i].label.text=(selected==i?"> ":"")+"模型 "+(i+1)+"\n"+(models[i].Submitted?"已投稿":models[i].Dead?"报废":"+"+models[i].Level);
            Status.text=result;stats.text="第 "+day+" 天　报废批次 "+lost+" / 3\n显存温度 "+Current.Heat+"　入神 "+charge+" / 10"+(frenzy>0?"（"+frenzy.ToString("0.0")+" 秒）":"");
            var t=Tickets();int sum=0;foreach(int n in t)sum+=n;odds.text="签袋（按权重抽取，不是命中时机）\n涨 "+(100*t[0]/sum)+"%　悟 "+(100*t[1]/sum)+"%　平 "+(100*t[2]/sum)+"%\n退 "+(100*t[3]/sum)+"%　爆 "+(frenzy>0?0:100*t[4]/sum)+"%";
            for(int i=0;i<8;i++){papers[i].label.text=(research[i]?"已获 ":"")+Research[i];papers[i].button.interactable=!over&&!research[i]&&!Current.Dead&&Current.Level>=2;}
        }
        public override void Tick(float dt){cooldown=Math.Max(0,cooldown-dt);if(frenzy>0){frenzy=Math.Max(0,frenzy-dt);Draw();}if(C.KeyDown(Key.Space))C.PlayerInput(Strike);if(C.KeyDown(Key.F))C.PlayerInput(Frenzy);}
    }
}
