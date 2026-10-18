using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    internal sealed class XgSentenceGame:XgBoardMini
    {
        readonly XgBtn[] source=new XgBtn[12],answer=new XgBtn[12];readonly List<int> placed=new List<int>();readonly HashSet<int> known=new HashSet<int>();
        int[] deck,order;string[] words;int round,correct;bool revealed;float time;TMP_Text clock,reveal;
        public XgSentenceGame(XgMiniContext c):base(c){}
        public override void Build()
        {
            Frame("句子排序","点候选词依次放入答案；点答案词退回。每局 8 句，每句 45 秒。原始语序为评分标准。");
            Status.rectTransform.offsetMin=new Vector2(22,-417);Status.rectTransform.offsetMax=new Vector2(872,-367);Status.fontSize=17;
            clock=C.Text("Clock",22,87,850,29,"",20);C.Text("AnswerLabel",22,120,850,23,"你的句子（点击可撤回）",16);
            for(int i=0;i<12;i++){int p=i;answer[i]=C.Button("Answer"+i,22+i%6*142,147+i/6*44,136,38,"",()=>Remove(p),18);source[i]=C.Button("Source"+i,22+i%6*142,246+i/6*44,136,38,"",()=>Place(p),18);}
            reveal=C.Text("Reveal",22,326,850,38,"",17);reveal.textWrappingMode=TextWrappingModes.Normal;
            C.Button("Submit",22,424,195,39,"交卷 Enter",Submit);C.Button("Next",234,424,195,39,"下一句 Space",Next);C.Button("Shuffle",446,424,195,39,"清空 / 重排",()=>{if(!revealed){placed.Clear();Shuffle(order);Draw();}});C.Button("Restart",658,424,195,39,"新一局",Reset);Reset();
        }
        void Reset(){deck=new int[Raw.Length];for(int i=0;i<deck.Length;i++)deck[i]=i;Shuffle(deck);round=correct=0;NewRound();}
        void NewRound(){words=Raw[deck[round]].Split('/');order=new int[words.Length];for(int i=0;i<order.Length;i++)order[i]=i;Shuffle(order);placed.Clear();revealed=false;time=45;reveal.text="";Status.text="把全部词块放入上方答案，再交卷。";Draw();}
        void Place(int position){if(revealed||position>=order.Length)return;int w=order[position];if(placed.Contains(w))return;placed.Add(w);Draw();}
        void Remove(int position){if(revealed||position>=placed.Count)return;placed.RemoveAt(position);Draw();}
        void Draw(){for(int i=0;i<12;i++){source[i].label.text=i<order.Length?words[order[i]]:"";source[i].button.interactable=!revealed&&i<order.Length&&!placed.Contains(order[i]);answer[i].label.text=i<placed.Count?words[placed[i]]:"·";answer[i].button.interactable=!revealed&&i<placed.Count;}clock.text="第 "+(round+1)+" / 8 句　剩余 "+Math.Ceiling(time)+" 秒　排对 "+correct;}
        void Submit()
        {
            if(revealed)return;revealed=true;int positions=0;for(int i=0;i<placed.Count;i++)if(words[placed[i]]==words[i])positions++;
            bool exact=positions==words.Length;int units=exact?2:positions>=words.Length*.7?1:0;if(exact)correct++;
            C.Report(units>0);var ai=(string[])words.Clone();bool memo=known.Contains(deck[round]);if(!memo&&C.Random.NextDouble()>C.Skill)Shuffle(ai);
            reveal.text="原句："+string.Concat(words);Status.text=(exact?"排对！":units==1?"基本通顺。":"顺序不对。")+" 在位 "+positions+" / "+words.Length+(XgUi.T("\n灵光（模拟","\nLingGuang (simulated")).Replace(XgUi.T("灵光","LingGuang"), C.Sim.AiDisplayName)+(memo?"，本次会话已记住":"")+"）："+string.Concat(ai);
            if(exact)known.Add(deck[round]);Draw();
        }
        void Next(){if(!revealed)return;if(round>=7){Status.text="本局结束：排对 "+correct+" / 8。点击新一局。";return;}round++;NewRound();}
        public override void Tick(float dt){if(!revealed){time=Math.Max(0,time-dt);clock.text="第 "+(round+1)+" / 8 句　剩余 "+Math.Ceiling(time)+" 秒　排对 "+correct;if(time<=0)Submit();}if(C.KeyDown(Key.Enter))Submit();if(C.KeyDown(Key.Space))Next();}
        static readonly string[] Raw={
"今天/北京/的/天气/非常/适合/出去/放风筝。",
"这部/电影/的/特效/比/我/想象/中/好/得多。",
"小明/昨天/晚上/在/家里/看/了/一场/足球/比赛。",
"我/打算/下个月/和/朋友/一起/去/成都/吃/火锅。",
"地铁/二号线/因为/信号/故障/暂时/停运/了。",
"老板/要求/我们/周五/之前/把/方案/发给/他。",
"她/每天/早上/都/会/在/小区/里/跑步/半小时。",
"这家/餐厅/的/红烧肉/做得/特别/地道。",
"手机/没电/之后/我/才/发现/充电器/忘/带/了。",
"下班/高峰/期/路上/堵得/一动/也/不动。",
"他/把/所有/的/积蓄/都/投/进/了/股市。",
"图书馆/里/不能/大声/说话/也/不能/吃/东西。",
"网上/购物/已经/成为/年轻人/生活/的/一部分。",
"孩子们/在/操场/上/开心/地/玩/着/游戏。",
"中国/女排/在/里约/奥运会/上/夺得/了/金牌。",
"谷歌/的/围棋/程序/打败/了/世界/冠军/李世石。",
"很多/人/睡前/都/喜欢/刷/一会儿/朋友圈。",
"滴滴/和/优步/中国/在/今年/八月/宣布/合并。",
"春节/的/时候/大家/都/会/抢/微信/红包。",
"听说/这个/周末/会/下/一场/大/暴雨。",
"我/在/淘宝/上/买/了/一件/很/便宜/的/外套。",
"她/的/微博/一/晚上/就/多/了/十万/粉丝。",
"公司/新/推出/的/智能/手环/可以/监测/睡眠/质量。",
"机器/学习/需要/大量/带/标签/的/数据。",
"神经网络/的/层数/越/多/训练/就/越/费电。",
"这块/显卡/跑/一晚上/就/能/训练/完/这个/模型。",
"高考/结束/以后/同学们/终于/可以/好好/睡/一觉/了。",
"他/做事/总是/慢吞吞/的/让/人/很/着急。",
"北京/到/上海/的/高铁/只/需要/四个/半/小时。",
"夏天/到/了/大家/都/想/去/海边/度假。",
"老师/让/我们/把/课文/背/下来/再/默写/一遍。",
"新/版本/上线/后/用户/反馈/速度/快/了/很多。",
"他/因为/堵车/迟到/了/半个/小时/被/老板/批评/了。",
"直男/最/爱/说/的/一句/话/是/多/喝/热水。",
"外卖/小哥/冒着/大雨/把/午饭/送到/了/公司。",
"周杰伦/的/新/专辑/一/发布/就/卖/断货/了。",
"他的/手机/屏幕/摔碎/了/却/还/能/用。",
"傅园慧/说/自己/已经/使出/了/洪荒之力。",
};
}
}
