using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgUndercoverGame : XgSocialSurface
    {
        // word A, word B, common description, A detail, B detail, A tag, B tag.
        static readonly string[][] Pairs={
            new[]{"饺子","馄饨","面皮包着馅，煮熟了吃","月牙形，捞出来蘸醋","皮薄，常配紫菜虾皮汤","月牙","汤"},
            new[]{"可乐","雪碧","一打开就冒气泡","黑色的，红色瓶标","透明的，一股柠檬味","黑色","透明"},
            new[]{"筷子","勺子","每顿饭用，用完要洗","一双两根，夹菜用","圆圆的，可以盛汤","夹","舀"},
            new[]{"手机","平板","触摸屏，需要充电","随身揣在口袋里打电话","屏幕大，出门不太方便带","口袋","大屏"},
            new[]{"微信","QQ","可以聊天发图片","朋友圈和红包，妈妈也用","企鹅头像，太阳和月亮等级","朋友圈","企鹅"},
            new[]{"咖啡","奶茶","一杯拿在手里，常排队买","苦苦的，熬夜时提神","甜甜的，可选珍珠和糖度","苦","珍珠"},
            // 2026-10-21: 14 more pairs (20 in all).
            new[]{"支付宝","微信支付","手机扫码就能付钱","蓝色图标，过年集五福","绿色图标，群里发红包","蓝色","绿色"},
            new[]{"U盘","移动硬盘","插在电脑上存文件","小小一根，挂在钥匙扣上","有点沉，要接数据线","钥匙扣","数据线"},
            new[]{"网吧","机房","一排排电脑坐满了人","按小时收费，可以包夜","学校上课用，老师盯着","包夜","上课"},
            new[]{"斗鱼","B站","在上面看视频和弹幕","主播直播打游戏","小电视图标，番剧和鬼畜","直播","小电视"},
            new[]{"耳机","音箱","用来听歌","戴在头上，别人听不见","放在桌上，全屋都听得见","戴","全屋"},
            new[]{"泡面","火锅","热乎乎的，要加水或汤","一个人加班，三分钟搞定","一群人围着锅涮","三分钟","一群人"},
            new[]{"高铁","飞机","出远门坐的","在地上跑，从站台上车","在天上飞，在登机口等","站台","天上"},
            new[]{"台式机","笔记本","打游戏写作业都靠它","大机箱放在桌子底下","合上盖子能装进书包","机箱","书包"},
            new[]{"共享单车","电动车","骑着出门的两个轮子","扫码开锁，骑完随手停","要充电，拧把手就走","扫码","充电"},
            new[]{"微博","朋友圈","发照片和心情的地方","陌生人也能看，有热搜","只有好友看得到，能点赞","热搜","好友"},
            new[]{"月饼","汤圆","过节才吃的甜点心","圆饼，中秋配月亮","圆球，元宵煮着吃","中秋","元宵"},
            new[]{"王者荣耀","英雄联盟","五个人一队推塔","拿手机就能玩","要坐在电脑前打","手机","电脑"},
            new[]{"短信","邮件","用来发文字消息","手机上发，一条七十个字","要写主题，可以带附件","七十字","附件"},
            new[]{"西瓜","哈密瓜","夏天切开吃的瓜","绿皮红瓤黑籽","黄绿皮橙色瓤，新疆的甜","红瓤","新疆"}};
        // English counterpart of Pairs: same order and shape.
        static readonly string[][] PairsEn={
            new[]{"Dumpling","Wonton","Filling wrapped in dough, eaten boiled","Crescent-shaped, fished out and dipped in vinegar","Thin wrapper, often served in seaweed and dried-shrimp soup","crescent","soup"},
            new[]{"Cola","Sprite","Fizzes the moment you open it","Black, with a red label","Clear, with a lemony taste","black","clear"},
            new[]{"Chopsticks","Spoon","Used at every meal, washed afterwards","A pair of two sticks, for picking up food","Round, can hold soup","pick","scoop"},
            new[]{"Phone","Tablet","Touch screen, needs charging","Carried in a pocket for making calls","Big screen, a bit awkward to carry around","pocket","big screen"},
            new[]{"WeChat","QQ","You can chat and send pictures","Moments and red packets, even Mom uses it","Penguin avatar, sun and moon levels","Moments","penguin"},
            new[]{"Coffee","Milk tea","A cup in your hand, often a queue to buy","Bitter, keeps you awake late at night","Sweet, with optional pearls and sugar level","bitter","pearls"},
            new[]{"Alipay","WeChat Pay","Pay by scanning with your phone","Blue icon, collect the Five Fortunes at New Year","Green icon, send red packets in group chats","blue","green"},
            new[]{"USB stick","External drive","Plugs into a computer to store files","Tiny, hangs on a keyring","A bit heavy, needs a cable","keyring","cable"},
            new[]{"Net café","Computer lab","Rows of computers full of people","Paid by the hour, all-nighters allowed","For class at school, the teacher watching","all-nighter","class"},
            new[]{"Douyu","Bilibili","Watch videos with bullet comments","Streamers playing games live","Little TV icon, anime and remixes","live","little TV"},
            new[]{"Headphones","Speakers","For listening to music","Worn on your head, nobody else hears","On the desk, the whole room hears","worn","whole room"},
            new[]{"Instant noodles","Hotpot","Hot food with water or broth","Ready in three minutes when working late alone","A group cooking around one pot","three minutes","group"},
            new[]{"High-speed rail","Plane","For long trips","Runs on the ground, board at a platform","Flies in the sky, wait at the gate","platform","sky"},
            new[]{"Desktop PC","Laptop","For games and homework","Big tower under the desk","Closes up and fits in a backpack","tower","backpack"},
            new[]{"Shared bike","E-bike","Two wheels you ride around town","Scan to unlock, leave it anywhere","Needs charging, twist the grip to go","scan","charge"},
            new[]{"Weibo","WeChat Moments","Where you post photos and moods","Strangers can see it, there are trending topics","Only friends see it, and they can like it","trending","friends"},
            new[]{"Mooncake","Tangyuan","Sweet festival food","Round cake, for the Mid-Autumn moon","Round balls, boiled for the Lantern Festival","Mid-Autumn","Lantern"},
            new[]{"Honor of Kings","League of Legends","Five per team, push the towers","Played on a phone","Played sitting at a computer","phone","computer"},
            new[]{"Text message","Email","For sending written messages","Sent from a phone, 70 characters each","Has a subject line, can carry attachments","70 characters","attachment"},
            new[]{"Watermelon","Hami melon","A melon you slice in summer","Green rind, red flesh, black seeds","Yellow-green rind, orange flesh, sweet from Xinjiang","red flesh","Xinjiang"}};
        string[] Names => new[]{"你",C.Sim.AiDisplayName,"虚拟玩家 A","虚拟玩家 B"};
        string[] NamesEn => new[]{"You",C.Sim.AiDisplayName,"Virtual player A","Virtual player B"};
        string Nm(int i)=>XgUi.T(Names[i],NamesEn[i]);
        readonly bool[] alive=new bool[4];readonly int[] sides=new int[4],marks=new int[4],descSide=new int[4];readonly string[] desc=new string[4];
        readonly List<string> history=new List<string>();
        string[] pair,pairEn;int odd,round,phase,goodVotes;float delay;
        static XgUndercoverGame(){Pairs=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Pairs,global::LingGuangV05.XingGuang.XgQuestionBank.Pairs);PairsEn=global::LingGuangV05.XingGuang.XgQuestionBank.Join(PairsEn,global::LingGuangV05.XingGuang.XgQuestionBank.PairsEn);} // appends XgQuestionBank (2026-10-21)
        public XgUndercoverGame(XgMiniContext c):base(c){}
        public override void Build(){int pi=C.Random.Next(Pairs.Length);pair=Pairs[pi];pairEn=PairsEn[pi];odd=C.Random.Next(4);int majority=C.Random.Next(2);for(int i=0;i<4;i++){alive[i]=true;sides[i]=i==odd?1-majority:majority;}round=goodVotes=0;history.Clear();NextRound();}
        void NextRound(){round++;phase=0;for(int i=0;i<4;i++){marks[i]=-1;desc[i]=null;descSide[i]=-1;}Draw();}
        void Draw(){
            Clear(XgUi.T("谁是卧底 · 第 "+round+" 轮","Who Is Undercover · Round "+round),XgUi.T("四人里一人拿相近词。先描述，再判断他人描述与你的词是否相像，最后投票。","One of the four holds a similar-but-different word. First describe, then judge whether the others' descriptions resemble your word, then vote."));
            T("Word",20,82,860,36,XgUi.T("你的词：「"+pair[sides[0]]+"」  · 不要直接泄露词语","Your word: \""+pairEn[sides[0]]+"\"  · Do not give the word away"),22,purple);
            for(int i=0;i<4;i++){
                int k=i;float x=18+i*220;Shade(x,126,210,194);T("Name"+i,x+12,134,185,30,Nm(i)+(alive[i]?"":XgUi.T("（已出局）"," (out)")),21);
                T("Desc"+i,x+12,172,185,90,alive[i]?(desc[i]??XgUi.T("待描述","Not described yet")):XgUi.T("旁观中","Watching"),19);
                if(phase==2&&i!=0&&alive[i]){
                    B("Like"+i,x+8,267,94,43,(marks[i]==1?XgUi.T("[选] ","[x] "):"")+XgUi.T("像","Alike"),()=>Mark(k,1),17);
                    B("Unlike"+i,x+108,267,94,43,(marks[i]==0?XgUi.T("[选] ","[x] "):"")+XgUi.T("不像","Unlike"),()=>Mark(k,0),17);
                }
                if(phase==3&&alive[i])B("Vote"+i,x+12,267,185,43,XgUi.T("投给 "+Names[i],"Vote "+NamesEn[i]),()=>Vote(k),18);
            }
            if(phase==0){
                T("Prompt",22,335,850,30,XgUi.T("轮到你描述：选一句不泄露词语的描述","Your turn to describe: pick a line that does not give the word away"),19);
                B("Common",20,378,280, sixty,XgUi.T(pair[2],pairEn[2]),()=>Describe(-1),18);
                B("Specific",310,378,280,sixty,XgUi.T(pair[3+sides[0]],pairEn[3+sides[0]]),()=>Describe(sides[0]),18);
                B("Tag",600,378,280,sixty,XgUi.T(pair[5+sides[0]],pairEn[5+sides[0]]),()=>Describe(sides[0]),18);
            }else if(phase==1)T("Wait",25,353,840,70,XgUi.T("其他人正在想怎么描述……","The others are thinking up their descriptions..."),22,purple);
            else if(phase==2){bool all=true;for(int i=1;i<4;i++)if(alive[i]&&marks[i]<0)all=false;
                T("Hint",25,339,830,38,XgUi.T("先标好像不像，揭晓时对答案；太笼统的描述，两个词都说得通。","Mark who sounds alike, then check at the reveal. A vague description fits both words."),16);
                if(all)B("GoVote",620,391,260,58,XgUi.T("全部标好，投票","All marked, vote"),()=>{phase=3;Draw();});
            }else if(phase==3)T("VoteHint",25,351,825,80,XgUi.T("结合描述和你手里的词，选出拿不同词的人（也可能是你）。","Using the descriptions and your own word, pick who holds the different word (it may be you)."),22);
        }
        const float sixty=60;
        void Describe(int specificity){if(phase!=0)return;desc[0]=specificity<0?XgUi.T(pair[2],pairEn[2]):XgUi.T(pair[3+sides[0]],pairEn[3+sides[0]]);descSide[0]=specificity;phase=1;delay=.8f+(1-(float)C.Skill)*1.4f;Draw();}
        void RevealDescriptions(){
            for(int i=1;i<4;i++)if(alive[i]){
                descSide[i]=sides[i];desc[i]=XgUi.T(pair[3+sides[i]],pairEn[3+sides[i]]);
                if(i==1){if(C.Skill<.35){desc[i]=XgUi.T("是……否……","Yes... no...");descSide[i]=-2;}
                    else if(C.Skill<.62)desc[i]=XgUi.T(pair[5+sides[i]],pairEn[5+sides[i]]);
                    else if(C.Skill>=.8&&i==odd&&C.Random.NextDouble()<.65){descSide[i]=1-sides[i];desc[i]=XgUi.T(pair[3+descSide[i]]+"，我也这么觉得。",pairEn[3+descSide[i]]+", I think so too.");}}
                else if(C.Random.NextDouble()<.25){desc[i]=XgUi.T(pair[2],pairEn[2]);descSide[i]=-1;}
            }phase=2;Draw();
        }
        void Mark(int seat,int mark){if(phase!=2||!alive[seat])return;marks[seat]=mark;Draw();}
        void Vote(int selected){if(phase!=3||!alive[selected])return;phase=4;
            bool correct=selected==odd;if(correct)goodVotes++;
            C.Sim.RecordTeachingQuestion("谁拿到了不同的词？我的词："+pair[sides[0]],
                "Who holds the different word? My word: "+pairEn[sides[0]],correct,true,
                Names[odd]+"拿到「"+pair[sides[odd]]+"」。",NamesEn[odd]+" holds "+pairEn[sides[odd]]+".","longtext",null,Nm(selected));
            C.Report(correct);
            int[] votes=new int[4];votes[selected]++;
            for(int i=1;i<4;i++)if(alive[i]){int pick=odd;double accuracy=i==1?C.Skill:.65;if(i==odd||C.Random.NextDouble()>accuracy){var available=new List<int>();for(int j=0;j<4;j++)if(alive[j]&&j!=i)available.Add(j);pick=available[C.Random.Next(available.Count)];}votes[pick]++;}
            int top=-1,max=0;bool tied=false;for(int i=0;i<4;i++)if(alive[i]){if(votes[i]>max){max=votes[i];top=i;tied=false;}else if(votes[i]==max)tied=true;}
            string tally="";for(int i=0;i<4;i++)if(alive[i])tally+=XgUi.T(Names[i]+" "+votes[i]+"票  ",NamesEn[i]+" "+votes[i]+(votes[i]==1?" vote  ":" votes  "));
            history.Add(XgUi.T("第"+round+"轮：","Round "+round+": ")+tally);
            if(!tied&&top>=0)alive[top]=false;
            int count=0;foreach(bool a in alive)if(a)count++;
            bool finished=!alive[odd]||!alive[0]||count<=2||round>=3;
            Clear(XgUi.T("投票结果","Vote result"),tally);
            string outcome=tied?XgUi.T("平票，无人出局。","Tie: nobody is out."):XgUi.T(Names[top]+" 出局。",NamesEn[top]+" is out.");
            T("VoteResult",32,110,836,100,outcome+(finished?XgUi.T("\n身份揭晓："+Names[odd]+"是卧底，词是「"+pair[sides[odd]]+"」。","\nReveal: "+NamesEn[odd]+" is the undercover, with the word \""+pairEn[sides[odd]]+"\"."):XgUi.T("\n身份暂不公开，继续比较描述。","\nIdentities stay hidden for now; keep comparing descriptions.")),24);
            T("History",32,225,836,115,string.Join("\n",history),18);
            if(finished){
                T("Explain",32,351,836,55,XgUi.T(C.Sim.AiDisplayName+"学得越好，越会顺着别人的话装。看描述说了什么，别看语气。","The better "+C.Sim.AiDisplayName+" learns, the better it plays along. Weigh what is said, not the tone."),18);
                B("Again",330,415,240,50,XgUi.T("重新发词","Deal new words"),Build);
            }else B("Continue",330,415,240,50,XgUi.T("再描述一轮","Describe another round"),NextRound);
        }
        public override void Tick(float dt){if(!disposed&&C.IsActive&&phase==1){delay-=dt;if(delay<=0)RevealDescriptions();}}
    }

    /// <summary>Unlimited statement judging: advance after each answer; the clock only grants a quick-answer bonus.</summary>
    sealed class XgQuizGame : XgSocialSurface
    {
        const float QuestionSeconds = 15;
        readonly HashSet<string> used = new HashSet<string>();
        XgPhrase statement;
        bool locked, aiSays;
        float left, feedbackLeft;
        string lastFeedback = "";
        bool lastCorrect;
        int answeredFrame;
        TMP_Text hint, progress, questionText, aiText;
        string feedbackZh = "", feedbackEn = "";
        XgBtn yes, no;
        public XgQuizGame(XgMiniContext c) : base(c) { }
        public override void Build() { used.Clear(); NextQuestion(); }

        void NextQuestion()
        {
            statement = XgMiniContent.SenseStatement(C.Random, C.Sim.TeachingAnswers, C.Sim.Today, used);
            used.Add(statement.text); left = QuestionSeconds; locked = false;
            aiSays = C.Sim.SenseHelp(statement, C.Random);
            Draw();
        }
        string Why => XgUi.T(statement.why, string.IsNullOrEmpty(statement.whyEn) ? statement.why : statement.whyEn);
        static string Word(bool value) => value ? XgUi.T("对", "True") : XgUi.T("错", "False");
        string Progress()
        {
            int count = C.Sim.TeachingAnswers, combo = C.Sim.S.combo;
            int multiplier = C.Sim.StreakMultiplier, toNext = C.Sim.StreakToNext;
            // The multiplier is the big number, in its tier's colour and growing with it; the streak and the way to the next step around it.
            string hex = ColorUtility.ToHtmlStringRGB(XgJuice.StreakColor(multiplier));
            string next = toNext > 0 ? XgUi.T("再对 " + toNext + " 题 ×" + (multiplier + 1), toNext + " more: ×" + (multiplier + 1)) : XgUi.T("满倍", "Max");
            return XgUi.T("连对 ", "Streak ") + combo
                + "\n<size=" + (34 + 6 * multiplier) + "><color=#" + hex + ">×" + multiplier + "</color></size>"
                + (multiplier > 1 ? "\n<color=#" + hex + ">" + XgJuice.StreakName(multiplier) + "</color>" : "")
                + "\n<size=14>" + next + "</size>"
                + "\n\n" + XgUi.T("第 " + (count / 12 + 1) + " 套 · " + (count % 12) + "/12", "Set " + (count / 12 + 1) + " · " + (count % 12) + "/12");
        }
        string AiAnswer => !C.Sim.TeachingAiReady ? "" : C.Sim.AiDisplayName + "：" + (C.Sim.YesNoAccuracy < .55
            ? XgUi.T("……不知道", "…not sure") : Word(aiSays) + " · " + (int)Math.Round(C.Sim.YesNoAccuracy * 100) + "%");
        void Draw()
        {
            Clear(XgUi.T("常识判断", "Common sense"), XgUi.T("判断题目对错；连对越多奖励越高，最高五倍。", "Judge the statement. A longer streak pays more, up to five times."));
            Shade(20, 94, 620, 150);
            questionText = T("Question", 42, 108, 575, 124, XgUi.T(statement.text, string.IsNullOrEmpty(statement.textEn) ? statement.text : statement.textEn), 31);
            timer = T("Timer", 708, 8, 175, 36, "", 19); Clock(left);
            aiText = T("AiAnswer", 25, 244, 610, 38, AiAnswer, 22, purple);
            yes = B("True", 20, 290, 300, 100, Word(true), () => Answer(true), 46);
            no = B("False", 340, 290, 300, 100, Word(false), () => Answer(false), 46);
            yes.image.color = C.Ui.dark ? XgDark.AccentSoft : new Color32(214, 238, 224, 255); yes.label.color = green;
            no.image.color = C.Ui.dark ? new Color32(52, 28, 31, 255) : new Color32(246, 220, 222, 255); no.label.color = red;
            Shade(666, 91, 214, 282); progress = T("Progress", 681, 102, 187, 260, Progress(), 18);
            hint = T("Feedback", 25, 398, 610, 74, feedbackLeft > 0 ? lastFeedback : "", 17, lastCorrect ? green : red);
            hint.textWrappingMode = TextWrappingModes.Normal;
        }
        void Answer(bool says)
        {
            if (locked || !C.IsActive) return;
            locked = true;
            bool ok = says == statement.yes;
            C.Sim.RecordTeachingQuestion(statement.text, statement.textEn, statement.yes, says, statement.why, statement.whyEn, "sense");
            C.ReportTimed(ok, QuestionSeconds - left);
            progress.text = Progress();
            yes.button.interactable = no.button.interactable = false;
            (says ? yes : no).image.color = ok ? green : red;
            if (C.Ui.dark) (says ? yes : no).label.color = XgDark.Page;
            string aiMark = C.Sim.TeachingAiReady ? "  " + C.Sim.AiDisplayName + (aiSays == statement.yes ? " ✓" : " ×") : "";
            feedbackZh = (ok ? "答对了：" : "答错了：") + statement.why + aiMark;
            feedbackEn = (ok ? "Right: " : "Wrong: ") + (string.IsNullOrEmpty(statement.whyEn) ? statement.why : statement.whyEn) + aiMark;
            hint.text = XgUi.T(feedbackZh, feedbackEn);
            hint.color = ok ? green : red;
            lastFeedback = hint.text; lastCorrect = ok; feedbackLeft = 1.5f; answeredFrame = Time.frameCount;
        }
        public override void RefreshLanguage()
        {
            // Keep the same statement, countdown, answer lock, random state and reward ticket.
            var title = C.Root.Find("Title")?.GetComponent<TMP_Text>();
            var rule = C.Root.Find("Rule")?.GetComponent<TMP_Text>();
            if (title != null) title.text = XgUi.T("常识判断", "Common sense");
            if (rule != null) rule.text = XgUi.T("判断题目对错；连对越多奖励越高，最高五倍。", "Judge the statement. A longer streak pays more, up to five times.");
            if (questionText != null) questionText.text = XgUi.T(statement.text, string.IsNullOrEmpty(statement.textEn) ? statement.text : statement.textEn);
            if (aiText != null) aiText.text = AiAnswer;
            if (yes != null) yes.label.text = Word(true);
            if (no != null) no.label.text = Word(false);
            if (progress != null) progress.text = Progress();
            lastFeedback = XgUi.T(feedbackZh, feedbackEn);
            if (hint != null) hint.text = feedbackLeft > 0 ? lastFeedback : "";
            Clock(left);
        }
        public override void Tick(float dt)
        {
            if (disposed || !C.IsActive) return;
            if (locked) { if (Time.frameCount > answeredFrame) NextQuestion(); return; }
            if (feedbackLeft > 0) { feedbackLeft -= dt; if (feedbackLeft <= 0) hint.text = ""; }
            left = Mathf.Max(0, left - dt); Clock(left);
            if (C.KeyDown(Key.Digit1) || C.KeyDown(Key.LeftArrow)) C.PlayerInput(() => Answer(true));
            else if (C.KeyDown(Key.Digit2) || C.KeyDown(Key.RightArrow)) C.PlayerInput(() => Answer(false));
        }
    }
}
