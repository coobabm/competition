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
        void Next(){if(round>=8){submitted=true;End(XgUi.T("传话校对结束","Back-translation complete"),XgUi.T($"修复 {fixesTotal} 个词，误改 {damageTotal} 个词。\n同一句在当前会话再出现时，已纠正的词更不易出错。\n这是模拟译文，不调用翻译服务。",$"Fixed {fixesTotal} phrases; changed {damageTotal} correct phrases.\nCorrected phrases are less likely to fail again in this session.\nSimulated translation; no translation service is called."),Build);return;}
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
            Clear(XgUi.T("传话 · ","Translation \u00b7 ")+round+XgUi.T(" / 8 句"," / 8 sentences"),XgUi.T("对照原句，点击回译里的词，再选择修复。正确的词不该动；最后提交完整校对。","Compare the original Chinese with its back-translation. Click a word to repair it. Keep correct words; submit the complete proofread."));
            Shade(22,88,856,70);T("Original",37,97,822,53,XgUi.T("原句：","Original: ")+string.Join("",truth),25);
            Shade(22,168,856,77);T("English",37,177,822,58,XgUi.T(C.Sim.AiDisplayName+"英文（模拟）：",C.Sim.AiDisplayName+" English (simulated): ")+english,19,purple);
            T("ReturnedLabel",25,250,840,32,XgUi.T("回译中文（逐词选择）：","Chinese back-translation (select a phrase): "),18);
            for(int i=0;i<4;i++){int k=i;var b=B("Token"+i,22+i*217,291,205,57,TokenLabel(current[i]),()=>{if(submitted)return;chosen=k;Draw();},22);if(chosen==i)b.image.color=new Color32(216,204,247,255);if(submitted)b.label.color=current[i]==truth[i]?green:red;}
            if(!submitted){
                if(chosen>=0){int slot=chosen;var opts=new List<string>{truth[slot],wrong[slot],"（删去这部分）"};Shuffle(opts);
                    for(int i=0;i<3;i++){string value=opts[i];B("Repair"+i,25+i*282,362,270,48,TokenLabel(value),()=>{current[slot]=value;chosen=-1;Draw();},18);}}
                B("Submit",625,429,250,43,XgUi.T("提交校对 / 原句无误","Submit / original is correct"),Submit,18);
            }else{T("Result",25,357,850,64,result,18);B("Next",625,429,250,43,XgUi.T("下一句","Next sentence"),Next,18);}
        }
        static string TokenLabel(string value) => value == "（删去这部分）" ? XgUi.T(value,"(remove this phrase)") : value;
        void Submit(){if(submitted)return;submitted=true;
            XgMiniSocialRules.RepairScore(truth,original,current,out int fixes,out int missed,out int damaged);fixesTotal+=fixes;damageTotal+=damaged;
            bool clean=missed==0&&damaged==0;C.Report(clean);
            for(int i=0;i<4;i++)if(original[i]!=truth[i]&&current[i]==truth[i])taught[sentence][i]++;
            result=XgUi.T($"改对 {fixes} 个 · 漏改 {missed} 个 · 误改 {damaged} 个\n",$"Fixed {fixes} · Missed {missed} · Incorrect changes {damaged}\n")+(clean?XgUi.T("校对通过。","Proofread passed."):XgUi.T("应为：","Expected: ")+string.Join("",truth));Draw();
        }
    }

    sealed class XgTuringGame : XgSocialSurface
    {
        // Question, authored human character, mechanical, polished, natural-style simulated model.
        static readonly string[][] ProbesZh={
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
        static readonly string[][] ProbesEn = {
            new[]{"What did you eat last night?","Uh... instant beef noodles. Could not be bothered to go downstairs.","I ate dinner last evening.","Tomatoes, eggs and rice. A balanced meal.","Yesterday? Wontons, I think. No, that was the day before... I forget."},
            new[]{"What is one third plus one third?","Two thirds. You really have to ask?","One sixth.","Two thirds, approximately 0.667.","Two thirds. Are you trying to trick me again?"},
            new[]{"Do you get sad?","Sure. My cat went missing last week. I searched all night.","Sadness: an unhappy emotion. Sadness.","Everyone feels sad. It matters to adjust your attitude positively.","I guess. Especially late at night. Why do you ask? Feeling down?"},
            new[]{"Where are you now?","In my dorm bed. My roommate is snoring.","I am here.","At home, sitting at my computer and chatting with you.","Home... noisy fan. What about you?"},
            new[]{"Tell me a bad joke.","I licked an ice cream for ages before realizing I was licking the glass.","The chicken crossed the road to get to the other side. Ha ha.","Programmers confuse Halloween and Christmas because Oct 31 equals Dec 25.","A job applicant said he could play dead. Told to leave, he lay down. Bad enough?"},
            new[]{"What game do you like most?","LOL. Bronze rank, though. My team keeps yelling at me.","The game I like is a game.","Strategy games, which can improve logical thinking.","A matching game. Three hundred levels in, it feels a little empty after every win."},
            new[]{"The heavenly king covers the earth tiger.","Chicken and mushroom stew! Ha ha.","Please enter a valid question.","The precious pagoda subdues the river demon.","Chicken and mushroom... wait, the pagoda subdues the river demon!"},
            new[]{"Do you believe in ghosts?","No, but I still turn on the light to use the bathroom at night.","Ghosts are fictional. Ghosts.","There is currently no scientific evidence that ghosts exist.","Probably not. If they did, maybe they just could not bear to leave."},
            new[]{"What annoyed you most today?","Printer jammed again. My assignment was due!","Annoyance is a negative emotion.","Some pressure is normal. Try managing your time sensibly.","Typed a whole paragraph and lost it. Do not feel like typing it again."}
        };
        static string[][] Probes => LingGuangV05.Runtime.GameText.IsEnglish ? ProbesEn : ProbesZh;
        
        readonly HashSet<int> used=new HashSet<int>();readonly List<string>[] transcript={new List<string>(),new List<string>()};
        readonly string[] reply=new string[2];readonly float[] delay=new float[2];readonly bool[] delivered=new bool[2];
        int aiSide,turn,phase,history;TMP_Text[] panes=new TMP_Text[2];
        public XgTuringGame(XgMiniContext c):base(c){}
        public override void Build(){used.Clear();transcript[0].Clear();transcript[1].Clear();aiSide=C.Random.Next(2);turn=phase=history=0;Draw();}
        void Draw(){
            Clear(XgUi.T("图灵测试 · 匿名聊天","Turing Test · Anonymous Chat"),XgUi.T("六轮提问后识别"+C.Sim.AiDisplayName+"。两侧都是离线剧情角色，不是真人连线；口误和延迟不是可靠鉴别依据。","Identify "+C.Sim.AiDisplayName+" after six rounds. Both sides are offline story characters; slips and delays are not reliable clues."));
            for(int i=0;i<2;i++){float x=22+i*443;Shade(x,92,427,235);T("Peer"+i,x+12,99,400,32,XgUi.T("匿名对象 ","Anonymous ")+(i==0?"A":"B"),24,purple);
                string line=transcript[i].Count>0?transcript[i][Math.Min(history,transcript[i].Count-1)]:XgUi.T("已建立模拟连接，等待你提问。","Simulated connection ready. Ask a question.");
                panes[i]=T("Conversation"+i,x+15,145,395,169,line,22);
            }
            if(turn>1){B("Prev",25,335,120,30,XgUi.T("上一轮记录","Previous round"),()=>{history=Math.Max(0,history-1);Draw();},14);B("Later",155,335,120,30,XgUi.T("下一轮记录","Next round"),()=>{history=Math.Min(turn-1,history+1);Draw();},14);}
            T("Turn",325,331,555,38,XgUi.T("已聊 ","Rounds: ")+turn+XgUi.T(" / 6 轮"," / 6"),19);
            if(phase==0){var available=new List<int>();for(int i=0;i<Probes.Length;i++)if(!used.Contains(i))available.Add(i);Shuffle(available);
                for(int i=0;i<Math.Min(3,available.Count);i++){int probe=available[i];B("Question"+i,24+i*288,389,277, sixty,Probes[probe][0],()=>Ask(probe),19);}}
            else if(phase==1)T("Waiting",25,385,850,60,XgUi.T("对方正在输入……","They are typing..."),22,purple);
            else if(phase==2){B("GuessA",155,391,275,60,XgUi.T("A 是"+C.Sim.AiDisplayName,"A is "+C.Sim.AiDisplayName),()=>Reveal(0));B("GuessB",470,391,275,60,XgUi.T("B 是"+C.Sim.AiDisplayName,"B is "+C.Sim.AiDisplayName),()=>Reveal(1));}
        }
        const float sixty=60;
        void Ask(int index){if(phase!=0||used.Contains(index))return;used.Add(index);phase=1;history=turn;var q=Probes[index];
            for(int i=0;i<2;i++){
                int tier=C.Skill<.4?2:C.Skill<.75?3:4;reply[i]=i==aiSide?q[tier]:q[1];
                delay[i]=i==aiSide&&C.Skill<.4?.25f:.8f+(float)C.Random.NextDouble()*1.5f;
                delivered[i]=false;transcript[i].Add(XgUi.T("你：","You: ")+q[0]+XgUi.T("\n\n对方正在输入……","\n\nThey are typing..."));
            }Draw();
        }
        void Reveal(int side){if(phase!=2)return;phase=3;C.Report(side==aiSide);
            Clear(side==aiSide?XgUi.T("你认出了"+C.Sim.AiDisplayName,"You identified "+C.Sim.AiDisplayName):XgUi.T("这次没有认出来","Not identified this time"),XgUi.T("本局为脚本化角色辨认，不是现实世界的 AI 检测器。","This is a scripted character puzzle, not a real-world AI detector."));
            T("Answer",35,100,825,85,XgUi.T(C.Sim.AiDisplayName+"是 "+(aiSide==0?"A":"B")+"。高能力会模仿口语、停顿与纠正，不能只凭一个口误作判断。",C.Sim.AiDisplayName+" is "+(aiSide==0?"A":"B")+". It can imitate casual speech, pauses and corrections; a single slip is not proof."),26);
            for(int i=0;i<2;i++){float x=25+i*444;Shade(x,202,424,170);T("Last"+i,x+12,212,400,150,transcript[i][5],18);}
            B("Review",25,413,250,48,XgUi.T("复看全部六轮对话","Review all six rounds"),()=>{phase=4;history=0;Review();},18);B("Again",595,413,275,48,XgUi.T("再聊一局","Chat again"),Build);
        }
        void Review(){Clear(XgUi.T("对话复盘 · ","Review \u00b7 ")+(history+1)+" / 6",XgUi.T(C.Sim.AiDisplayName+"在 "+(aiSide==0?"A":"B")+" 侧。点击翻阅，不重复结算。",C.Sim.AiDisplayName+" is on side "+(aiSide==0?"A":"B")+". Review the conversation without scoring it again."));for(int i=0;i<2;i++){float x=25+i*443;Shade(x,105,425,270);T("Review"+i,x+14,121,397,240,(i==0?"A":"B")+"\n"+transcript[i][history],22);}B("Back",25,412,210,50,XgUi.T("上一轮","Previous"),()=>{history=Math.Max(0,history-1);Review();});B("Forward",250,412,210,50,XgUi.T("下一轮","Next"),()=>{history=Math.Min(5,history+1);Review();});B("Again",650,412,225,50,XgUi.T("再聊一局","Chat again"),Build);}
        public override void Tick(float dt){if(disposed||!C.IsActive||phase!=1)return;for(int i=0;i<2;i++)if(!delivered[i]){delay[i]-=dt;if(delay[i]<=0){delivered[i]=true;var old=transcript[i][turn];int p=old.IndexOf("\n\n",StringComparison.Ordinal);transcript[i][turn]=old.Substring(0,p)+XgUi.T("\n\n对方：","\n\nThem: ")+reply[i];panes[i].text=transcript[i][turn];}}
            if(delivered[0]&&delivered[1]){turn++;phase=turn>=6?2:0;history=turn-1;Draw();}}
    }

    sealed class XgQingwenGame : XgSocialSurface
    {
        static readonly string[] Moods={"开心","生气","委屈","想你","敷衍","试探"}, MoodsEn={"happy","angry","hurt","missing you","distracted","seeking reassurance"};
        // This is authored narrative ground truth, never a claim that a real person has one decipherable emotion.
        static readonly string[][] ScenesZh={
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
        static readonly string[][] ScenesEn = {
            new[]{"Finished my probability exam!!\nThink I passed. Taking my roommates for hot pot tonight.","0","Sharing news, excitement and celebrating: this scene is written as happiness.","Great! Enjoy celebrating tonight.","Hearing that makes me happy for you too.","Oh, okay."},
            new[]{"Why did you not reply yesterday?\nI waited until midnight. Never mind.","1","She describes waiting. Do not dismiss that disappointment.","Sorry I kept you waiting. Next time I will tell you when I am busy.","Want to tell me how last night felt?","What now? I was busy."},
            new[]{"Exhausted today. My roommate criticized me.\nDo not feel like talking.","2","Tired and criticized, she needs listening rather than a lecture.","That sounds rough. You do not have to talk. I am here.","Take your time. I will listen when you want to talk.","You must have done something wrong."},
            new[]{"What are you doing? Cannot sleep.\nWant to hear your voice.","3","She directly asks for company.","Miss you too. Let us chat for a while.","I am here. Anything you want to tell me today?","Just sleep. Stop bothering me."},
            new[]{"Watching a show. Talk later.","4","The scene shows her attention elsewhere; it does not mean anger.","Sure. Enjoy it and talk when you can.","Tell me about the show later.","Do you not care about me anymore?"},
            new[]{"If I go to Beijing for graduate school,\nwhat will you do?","5","She asks about your future together. Do not decide for her.","I support you. We can plan how to stay in touch and visit.","What do you look forward to in Beijing? Tell me.","Then do not go."},
            new[]{"My scholarship came through!\nDinner is on me, through the screen.","0","She shares good news and invites you into it.","Wonderful! Your work paid off.","What would you like? Let us celebrate.","It is hardly any money. Why the fuss?"},
            new[]{"You are not listening at all.\nWhat did I just say?","1","She points out being ignored. Acknowledge it and listen carefully.","Sorry, I lost focus. I am putting things aside to listen.","I want to understand. Could you tell me again?","Do not be so sensitive."},
            new[]{"Someone came to collect all my roommates.\nI carried my suitcase for twenty minutes. Heavy.","2","The weight is a fact; loneliness is the narrative clue.","Carrying that alone sounds tiring. Rest a little; I will keep you company.","Does your shoulder hurt? Are you back at the dorm?","Others manage it. So can you."},
            new[]{"Passed our old milk-tea shop.\nIt closed. Suddenly I really miss you.","3","A familiar place brings out explicit longing.","Miss you too. Next time we meet, we will find a new favorite.","I remember what you ordered that time.","It closed. Just find another shop."},
            new[]{"Yeah, okay!\nStill writing my report here.","4","Context says she is busy. Short replies alone do not imply bad intentions.","Finish your report. Talk when you are free.","Get some rest after you finish.","Short replies mean you are angry."},
            new[]{"What name am I saved under in your phone?","5","The scene asks for reassurance about her place in the relationship.","The name you like. Want a special nickname?","What would you like me to call you?","Why so many questions?"}
        };
        static string[][] Scenes => LingGuangV05.Runtime.GameText.IsEnglish ? ScenesEn : ScenesZh;
        
        readonly List<int> order=new List<int>();string[] scene;int round,phase,meter,truth,chosenMood,aiMood,correct,penalty;string context,result;string[] replies;int[] fits;
        public XgQingwenGame(XgMiniContext c):base(c){}
        public override void Build(){order.Clear();for(int i=0;i<Scenes.Length;i++)order.Add(i);Shuffle(order);round=correct=0;meter=60;Next();}
        void Next(){if(round>=12||meter<=0){phase=3;End(meter<=0?XgUi.T("晴雯先去休息了","Qingwen is taking a break"):XgUi.T("宿舍断网，晚安","Dorm internet is off. Good night."),XgUi.T($"读对 {correct} / {round} 条消息，心情 {meter}/100。\n情绪标签是本局剧情设定；现实中应结合语境、尊重对方并直接询问。",$"Read {correct} / {round} messages correctly. Mood {meter}/100.\nEmotion labels belong to this story. In real life, consider context, respect the person and ask directly."),Build);return;}
            scene=Scenes[order[round++]];truth=int.Parse(scene[1]);aiMood=Guess(truth,6);phase=penalty=0;chosenMood=-1;context=scene[0];result="";Draw();
        }
        void Draw(){
            Clear(XgUi.T("教它认人 · 晴雯的消息","Teach social cues \u00b7 Qingwen"),XgUi.T("先读上下文，再标情绪并回复。贴心回复影响本局心情。","Read the context, label the emotion, then reply. Thoughtful replies affect the mood in this round."));
            Shade(20,92,422,340);T("Contact",36,104,390,35,XgUi.T("晴雯 · 私聊","Qingwen \u00b7 Private chat"),25);
            T("Messages",38,158,380,200,context,25);T("Counter",37,379,380,40,XgUi.T("第 ","Message ")+round+XgUi.T(" / 12 条"," / 12"),17);
            T("MoodMeter",472,91,400,40,XgUi.T("心情 ","Mood ")+meter+" / 100",23,meter>40?green:red);
            C.Box("MoodTrack",475,141,400,10,new Color32(217,222,231,255));C.Box("MoodFill",475,141,4*meter,10,meter>40?green:red);
            if(phase==0){
                T("AiMood",476,169,395,55,XgUi.T(C.Sim.AiDisplayName+"猜："+Moods[aiMood]+"\n（游戏模拟判断，可能出错）",C.Sim.AiDisplayName+" guesses: "+MoodsEn[aiMood]+"\n(Simulated guess; it may be wrong)"),18,purple);
                for(int i=0;i<6;i++){int k=i;B("Mood"+i,472+(i%2)*207,240+(i/2)*61,195,51,XgUi.T(Moods[i],MoodsEn[i]),()=>Label(k),22);}
            }else if(phase==1){T("Clue",476,162,398,74,result,17);
                for(int i=0;i<replies.Length;i++){int k=i;B("Reply"+i,472,242+i*49,405,43,replies[i],()=>Reply(k),15);}
                B("Skip",680,442,195,29,XgUi.T("先不回复","Do not reply yet"),()=>Reply(-1),14);
            }else {T("Outcome",474,175,398,190,result,22);B("Next",590,390,282,56,meter<=0||round>=12?XgUi.T("结束聊天","End chat"):XgUi.T("下一条消息","Next message"),Next,21);}
        }
        void Label(int mood){if(phase!=0)return;chosenMood=mood;bool ok=mood==truth;if(ok)correct++;C.Report(ok);penalty=!ok&&(truth==1||truth==2)?10:0;
            result=(ok?XgUi.T("读对了。","Correct. "):XgUi.T("本局设定为「","This scene is written as \u201c")+XgUi.T(Moods[truth],MoodsEn[truth])+XgUi.T("」。","\u201d. "))+scene[2];
            var ids=new List<int>{0,1,2};Shuffle(ids);replies=new string[3];fits=new int[3];
            for(int i=0;i<3;i++){replies[i]=scene[3+ids[i]];fits[i]=ids[i]==0?10:ids[i]==1?4:-10;}
            phase=1;Draw();
        }
        void Reply(int i){if(phase!=1)return;phase=2;int delta=(i<0?-5:fits[i])-penalty;meter=Math.Max(0,Math.Min(100,meter+delta));
            context+=XgUi.T("\n\n你：","\n\nYou: ")+(i<0?XgUi.T("（暂未回复）","(no reply yet)"):replies[i]);result=(delta>0?XgUi.T("她：谢谢你听我说。","Her: Thanks for listening."):XgUi.T("她：我想先安静一会儿。","Her: I would like some quiet time."))+XgUi.T("\n心情 ","\nMood ")+(delta>=0?"+":"")+delta+XgUi.T("\n回应具体处境，比猜测一个字更重要。","\nRespond to her situation rather than guessing from a single word.");Draw();
        }
    }
}
