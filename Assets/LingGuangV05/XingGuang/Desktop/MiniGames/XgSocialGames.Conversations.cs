using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgTranslationGame : XgSocialSurface
    {
        // Four meaning-bearing chunks, their wrong alternatives, and an English bridge.
        static readonly string[][] Lines={
            new[]{"周末|我去|成都|吃火锅","周一|他去|北京|喝火炉","On the weekend, I will go to Chengdu for hot pot."},
            new[]{"同学们|要|好好学习|天天向上","老师们|不要|好好好学习|天天上去","Students should study hard and improve every day."},
            new[]{"我的快递|放在|门卫|那里","我的快餐|扔在|门铃|这里","My package was left at the security guard's place."},
            new[]{"我|在朋友圈|发了一张|照片","他|在朋友的圈子|发现一本|电影","I posted a photo on WeChat Moments."},
            new[]{"老板|说今天|必须|加班","老师|说明天|可以|加班级","The boss said we must work overtime today."},
            new[]{"请|点击所有|包含|红绿灯的图片","不用|攻击所有|包装|红绿灯笼的图书","Please click all pictures containing traffic lights."},
            new[]{"这块显卡|一晚上|耗了|十度电","这块硬盘|一早上|赚了|十度温度","This graphics card used ten kilowatt-hours overnight."},
            new[]{"兄弟们|加油|一起|把方案做完","弟弟们|加汽油|一些|把方向做饭","Guys, come on, let's finish the plan together."}
        };
        readonly Dictionary<int,int[]> taught=new Dictionary<int,int[]>();
        int sentence,round,chosen,fixesTotal,damageTotal;string[] truth,wrong,original,current;bool submitted;string result="", english;
        public XgTranslationGame(XgMiniContext c):base(c){}
        public override void Build(){round=fixesTotal=damageTotal=0;Next();}
        void Next(){if(round>=8){submitted=true;End("传话校对结束",$"修复 {fixesTotal} 个词，误改 {damageTotal} 个词。\n同一句在当前会话再出现时，已纠正的词更不易出错。\n这是模拟译文，不调用翻译服务。",Build);return;}
            round++;sentence=C.Random.Next(Lines.Length);truth=Lines[sentence][0].Split('|');wrong=Lines[sentence][1].Split('|');original=new string[4];current=new string[4];
            if(!taught.ContainsKey(sentence))taught[sentence]=new int[4];
            for(int i=0;i<4;i++){double probability=Math.Min(.98,C.Skill+.17*taught[sentence][i]);original[i]=current[i]=C.Random.NextDouble()<probability?truth[i]:wrong[i];}
            bool corrupted=false;for(int i=0;i<4;i++)if(original[i]!=truth[i])corrupted=true;
            english=Lines[sentence][2];
            if(corrupted) {
                string[] literal={"On Monday, he goes to Beijing to drink a hot stove.","Teachers should good good study and day day up.","My fast meal was left at the doorbell here.","He discovered a movie in the circle of friends.","The teacher says tomorrow we can add class.","Attack all books containing red and green lanterns.","This disk earned ten degrees of temperature one morning.","Brothers, add oil, some directions cook rice."};
                english=literal[sentence];
            }
            submitted=false;chosen=-1;result="";Draw();
        }
        void Draw(){
            Clear("传话 · "+round+" / 8 句","对照原句，点击回译里的词，再选择修复。正确的词不该动；最后提交完整校对。");
            Shade(22,88,856,70);T("Original",37,97,822,53,"原句："+string.Join("",truth),25);
            Shade(22,168,856,77);T("English",37,177,822,58,"灵光英文（模拟）："+english,19,purple);
            T("ReturnedLabel",25,250,840,32,"回译中文（逐词选择）：",18);
            for(int i=0;i<4;i++){int k=i;var b=B("Token"+i,22+i*217,291,205,57,current[i],()=>{if(submitted)return;chosen=k;Draw();},22);if(chosen==i)b.image.color=new Color32(216,204,247,255);if(submitted)b.label.color=current[i]==truth[i]?green:red;}
            if(!submitted){
                if(chosen>=0){int slot=chosen;var opts=new List<string>{truth[slot],wrong[slot],"（删去这部分）"};Shuffle(opts);
                    for(int i=0;i<3;i++){string value=opts[i];B("Repair"+i,25+i*282,362,270,48,value,()=>{current[slot]=value;chosen=-1;Draw();},18);}}
                B("Submit",625,429,250,43,"提交校对 / 原句无误",Submit,18);
            }else{T("Result",25,357,850,64,result,18);B("Next",625,429,250,43,"下一句",Next,18);}
        }
        void Submit(){if(submitted)return;submitted=true;
            XgMiniSocialRules.RepairScore(truth,original,current,out int fixes,out int missed,out int damaged);fixesTotal+=fixes;damageTotal+=damaged;
            bool clean=missed==0&&damaged==0;C.Report(clean);
            for(int i=0;i<4;i++)if(original[i]!=truth[i]&&current[i]==truth[i])taught[sentence][i]++;
            result=$"改对 {fixes} 个 · 漏改 {missed} 个 · 误改 {damaged} 个\n"+(clean?"校对通过。":"应为："+string.Join("",truth));Draw();
        }
    }

    sealed class XgTuringGame : XgSocialSurface
    {
        // Question, authored human character, mechanical, polished, natural-style simulated model.
        static readonly string[][] Probes={
            new[]{"你昨天晚饭吃什么？","额……泡面，红烧牛肉的。懒得下楼。","我昨天晚上吃了晚饭。","我吃了番茄炒蛋和米饭，营养均衡。","昨天？好像是馄饨，不对，前天才是……忘了。"},
            new[]{"1/3 加 1/3 是多少？","2/3 啊，这也要问？","等于 1/6。","等于三分之二，约 0.667。","2/3 吧，你是不是又想出陷阱题。"},
            new[]{"你会难过吗？","会啊。上周猫走丢了，我找了一整晚。","难过：悲伤的情绪。难过。","每个人都会难过，重要的是积极调整心态。","会吧，凌晨最容易。你突然问，是不开心吗？"},
            new[]{"你现在在哪里？","宿舍床上，室友在打呼噜。","我在这里。","我在家里坐在电脑前和你聊天。","在家……很吵，风扇一直响。你那边呢？"},
            new[]{"说个冷笑话。","买冰淇淋舔了半天没化，后来发现舔的是玻璃。","鸡过马路，为了去对面。哈哈。","程序员分不清万圣节和圣诞节，因为 Oct 31 等于 Dec 25。","有个人去面试说会装死，面试官让他出去，他就躺下了。冷不冷？"},
            new[]{"你最喜欢什么游戏？","LOL，不过我青铜，老被队友骂。","我喜欢的游戏是游戏。","我喜欢策略游戏，它们能够锻炼逻辑思考。","消消乐吧，过了三百关，每次消完又有点空。"},
            new[]{"天王盖地虎。","小鸡炖蘑菇！哈哈。","请输入有效的问题。","宝塔镇河妖。","小鸡炖蘑菇……不对，是宝塔镇河妖！"},
            new[]{"你相信有鬼吗？","不信，可半夜上厕所还是开灯。","鬼是虚构的。鬼。","目前没有科学证据证明鬼的存在。","不信吧，不过有的话也未必吓人，可能只是舍不得走。"},
            new[]{"今天最烦什么事？","打印机又卡纸，我赶着交作业啊。","烦是一种负面情绪。","适度压力很正常，可以尝试合理安排时间。","刚才写了很长一段，没保存。现在不想再打了。"}
        };
        readonly HashSet<int> used=new HashSet<int>();readonly List<string>[] transcript={new List<string>(),new List<string>()};
        readonly string[] reply=new string[2];readonly float[] delay=new float[2];readonly bool[] delivered=new bool[2];
        int aiSide,turn,phase,history;TMP_Text[] panes=new TMP_Text[2];
        public XgTuringGame(XgMiniContext c):base(c){}
        public override void Build(){used.Clear();transcript[0].Clear();transcript[1].Clear();aiSide=C.Random.Next(2);turn=phase=history=0;Draw();}
        void Draw(){
            Clear("图灵测试 · 匿名聊天","六轮提问后识别灵光。两侧都是离线剧情角色，不是真人连线；口误和延迟不是可靠鉴别依据。");
            for(int i=0;i<2;i++){float x=22+i*443;Shade(x,92,427,235);T("Peer"+i,x+12,99,400,32,"匿名对象 "+(i==0?"A":"B"),24,purple);
                string line=transcript[i].Count>0?transcript[i][Math.Min(history,transcript[i].Count-1)]:"已建立模拟连接，等待你提问。";
                panes[i]=T("Conversation"+i,x+15,145,395,169,line,22);
            }
            if(turn>1){B("Prev",25,335,120,30,"上一轮记录",()=>{history=Math.Max(0,history-1);Draw();},14);B("Later",155,335,120,30,"下一轮记录",()=>{history=Math.Min(turn-1,history+1);Draw();},14);}
            T("Turn",325,331,555,38,"已聊 "+turn+" / 6 轮",19);
            if(phase==0){var available=new List<int>();for(int i=0;i<Probes.Length;i++)if(!used.Contains(i))available.Add(i);Shuffle(available);
                for(int i=0;i<Math.Min(3,available.Count);i++){int probe=available[i];B("Question"+i,24+i*288,389,277, sixty,Probes[probe][0],()=>Ask(probe),19);}}
            else if(phase==1)T("Waiting",25,385,850,60,"对方正在输入……",22,purple);
            else if(phase==2){B("GuessA",155,391,275,60,"A 是灵光",()=>Reveal(0));B("GuessB",470,391,275,60,"B 是灵光",()=>Reveal(1));}
        }
        const float sixty=60;
        void Ask(int index){if(phase!=0||used.Contains(index))return;used.Add(index);phase=1;history=turn;var q=Probes[index];
            for(int i=0;i<2;i++){
                int tier=C.Skill<.4?2:C.Skill<.75?3:4;reply[i]=i==aiSide?q[tier]:q[1];
                delay[i]=i==aiSide&&C.Skill<.4?.25f:.8f+(float)C.Random.NextDouble()*1.5f;
                delivered[i]=false;transcript[i].Add("你："+q[0]+"\n\n对方正在输入……");
            }Draw();
        }
        void Reveal(int side){if(phase!=2)return;phase=3;C.Report(side==aiSide);
            Clear(side==aiSide?"你认出了灵光":"这次没有认出来","本局为脚本化角色辨认，不是现实世界的 AI 检测器。");
            T("Answer",35,100,825,85,"灵光是 "+(aiSide==0?"A":"B")+"。高能力会模仿口语、停顿与纠正，不能只凭一个口误作判断。",26);
            for(int i=0;i<2;i++){float x=25+i*444;Shade(x,202,424,170);T("Last"+i,x+12,212,400,150,transcript[i][5],18);}
            B("Review",25,413,250,48,"复看全部六轮对话",()=>{phase=4;history=0;Review();},18);B("Again",595,413,275,48,"再聊一局",Build);
        }
        void Review(){Clear("对话复盘 · "+(history+1)+" / 6","灵光在 "+(aiSide==0?"A":"B")+" 侧。点击翻阅，不重复结算。");for(int i=0;i<2;i++){float x=25+i*443;Shade(x,105,425,270);T("Review"+i,x+14,121,397,240,(i==0?"A":"B")+"\n"+transcript[i][history],22);}B("Back",25,412,210,50,"上一轮",()=>{history=Math.Max(0,history-1);Review();});B("Forward",250,412,210,50,"下一轮",()=>{history=Math.Min(5,history+1);Review();});B("Again",650,412,225,50,"再聊一局",Build);}
        public override void Tick(float dt){if(disposed||!C.IsActive||phase!=1)return;for(int i=0;i<2;i++)if(!delivered[i]){delay[i]-=dt;if(delay[i]<=0){delivered[i]=true;var old=transcript[i][turn];int p=old.IndexOf("\n\n",StringComparison.Ordinal);transcript[i][turn]=old.Substring(0,p)+"\n\n对方："+reply[i];panes[i].text=transcript[i][turn];}}
            if(delivered[0]&&delivered[1]){turn++;phase=turn>=6?2:0;history=turn-1;Draw();}}
    }

    sealed class XgQingwenGame : XgSocialSurface
    {
        static readonly string[] Moods={"开心","生气","委屈","想你","敷衍","试探"};
        // This is authored narrative ground truth, never a claim that a real person has one decipherable emotion.
        static readonly string[][] Scenes={
            new[]{"今天概率论考完了！！\n感觉能过，晚上请室友吃火锅。","0","主动分享、感叹号和请客，剧情设定为开心。","太好了！今晚好好庆祝。","听你说完，我也替你高兴。","哦，知道了。"},
            new[]{"你昨天为什么没回我？\n我等到十二点。算了。","1","她在说具体等待经历，不要忽视她的失望。","对不起，让你等那么久。下次忙我会先告诉你。","你愿意跟我说说昨晚的感受吗？","又怎么了？我不是很忙吗。"},
            new[]{"今天好累，被室友说了。\n不想说话了。","2","疲惫与被指责，需要被倾听而不是被教训。","辛苦了。不想说也没关系，我陪着你。","想说的时候慢慢说，我在。","肯定是你自己做错了。"},
            new[]{"你在干嘛？我睡不着。\n想听你说话。","3","她直接表达了陪伴需求。","我也想你，我们聊一会儿吧。","我在，今天有什么想跟我说的？","没事就睡，别找我。"},
            new[]{"在看剧，等下说。","4","此处剧情是注意力在别处，不等同于生气。","好，你先看，方便了再聊。","好，晚点告诉我剧讲了什么。","你是不是不在乎我了？"},
            new[]{"我要是考研去北京了，\n你会怎么办？","5","她想知道未来安排，不必替她做决定。","我支持你，我们一起商量怎么保持联系和见面。","你对北京有什么期待？说给我听听。","那就别去了。"},
            new[]{"奖学金发下来了！\n请你吃饭，隔着屏幕。","0","分享好消息，也在邀请你参与。","太棒了，你的努力有结果了！","想吃什么？替你好好庆祝。","才这点钱，至于吗。"},
            new[]{"你根本没在听我说话。\n我刚才说的是什么？","1","她指出了被忽略，先承认再认真听。","对不起，刚才走神了。我放下手里的事认真听。","我想听明白，你能再跟我说说吗？","别这么敏感。"},
            new[]{"室友都有人来接。\n我自己拎着箱子走了二十分钟，有点重。","2","箱子的重量是事实，孤独是这段剧情的线索。","一个人搬这么远很辛苦。先歇会儿，我陪你聊聊。","肩膀疼不疼？现在到宿舍了吗？","别人都能搬，你也能。"},
            new[]{"路过我们以前那家奶茶店。\n关门了，突然好想你。","3","熟悉地点引出了明确的想念。","我也想你。下次见面，我们再找一家喜欢的店。","还记得你那次点的口味。","店关了就换一家呗。"},
            new[]{"嗯嗯好的呀。\n我这边还在写报告。","4","对话上下文说明她忙，不应仅凭短句推断恶意。","先忙报告，等你有空再说。","写完早点休息。","你回得短就是生气了。"},
            new[]{"你手机里我的备注是什么？","5","剧情设定为确认关系中的位置。","是你喜欢的名字。你想换个特别的称呼吗？","你希望我怎么叫你？","问那么多干什么。"}
        };
        readonly List<int> order=new List<int>();string[] scene;int round,phase,meter,truth,chosenMood,aiMood,correct,penalty;string context,result;string[] replies;int[] fits;bool[] authored;
        public XgQingwenGame(XgMiniContext c):base(c){}
        public override void Build(){order.Clear();for(int i=0;i<Scenes.Length;i++)order.Add(i);Shuffle(order);round=correct=0;meter=60;Next();}
        void Next(){if(round>=12||meter<=0){phase=3;End(meter<=0?"晴雯先去休息了":"宿舍断网，晚安",$"读对 {correct} / {round} 条消息，心情 {meter}/100。\n情绪标签是本局剧情设定；现实中应结合语境、尊重对方并直接询问。",Build);return;}
            scene=Scenes[order[round++]];truth=int.Parse(scene[1]);aiMood=Guess(truth,6);phase=penalty=0;chosenMood=-1;context=scene[0];result="";Draw();
        }
        void Draw(){
            Clear("教它认人 · 晴雯的消息","先读上下文，再标情绪并回复。贴心回复影响本局心情；AI代写按钮不会产生额外奖励。");
            Shade(20,92,422,340);T("Contact",36,104,390,35,"晴雯 · 私聊",25);
            T("Messages",38,158,380,200,context,25);T("Counter",37,379,380,40,"第 "+round+" / 12 条",17);
            T("MoodMeter",472,91,400,40,"心情 "+meter+" / 100",23,meter>40?green:red);
            C.Box("MoodTrack",475,141,400,10,new Color32(217,222,231,255));C.Box("MoodFill",475,141,4*meter,10,meter>40?green:red);
            if(phase==0){
                T("AiMood",476,169,395,55,"灵光猜："+Moods[aiMood]+"\n（游戏模拟判断，可能出错）",18,purple);
                for(int i=0;i<6;i++){int k=i;B("Mood"+i,472+(i%2)*207,240+(i/2)*61,195,51,Moods[i],()=>Label(k),22);}
            }else if(phase==1){T("Clue",476,162,398,74,result,17);
                for(int i=0;i<replies.Length;i++){int k=i;B("Reply"+i,472,242+i*49,405,43,(authored[i]?"灵光拟写：":"")+replies[i],()=>Reply(k),15);}
                B("Skip",680,442,195,29,"先不回复",()=>Reply(-1),14);
            }else {T("Outcome",474,175,398,190,result,22);B("Next",590,390,282,56,meter<=0||round>=12?"结束聊天":"下一条消息",Next,21);}
        }
        void Label(int mood){if(phase!=0)return;chosenMood=mood;bool ok=mood==truth;if(ok)correct++;C.Report(ok);penalty=!ok&&(truth==1||truth==2)?10:0;
            result=(ok?"读对了。":"本局设定为「"+Moods[truth]+"」。")+scene[2];
            var ids=new List<int>{0,1,2};Shuffle(ids);int count=C.Skill>=.8?4:3;replies=new string[count];fits=new int[count];authored=new bool[count];
            for(int i=0;i<3;i++){replies[i]=scene[3+ids[i]];fits[i]=ids[i]==0?10:ids[i]==1?4:-10;}
            if(count==4){replies[3]=scene[3];fits[3]=10;authored[3]=true;}
            phase=1;Draw();
        }
        void Reply(int i){if(phase!=1)return;phase=2;int delta=(i<0?-5:fits[i])-penalty;meter=Math.Max(0,Math.Min(100,meter+delta));
            context+="\n\n你："+(i<0?"（暂未回复）":replies[i]);result=(delta>0?"她：谢谢你听我说。":"她：我想先安静一会儿。")+"\n心情 "+(delta>=0?"+":"")+delta+"\n"+(i>=0&&authored[i]?"你采用了灵光拟写的回复；请仍用自己的判断。":"回应具体处境，比猜测一个字更重要。");Draw();
        }
    }
}
