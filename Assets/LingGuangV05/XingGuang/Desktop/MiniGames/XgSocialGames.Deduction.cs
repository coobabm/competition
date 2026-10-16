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
            new[]{"咖啡","奶茶","一杯拿在手里，常排队买","苦苦的，熬夜时提神","甜甜的，可选珍珠和糖度","苦","珍珠"}};
        // English counterpart of Pairs: same order and shape.
        static readonly string[][] PairsEn={
            new[]{"Dumpling","Wonton","Filling wrapped in dough, eaten boiled","Crescent-shaped, fished out and dipped in vinegar","Thin wrapper, often served in seaweed and dried-shrimp soup","crescent","soup"},
            new[]{"Cola","Sprite","Fizzes the moment you open it","Black, with a red label","Clear, with a lemony taste","black","clear"},
            new[]{"Chopsticks","Spoon","Used at every meal, washed afterwards","A pair of two sticks, for picking up food","Round, can hold soup","pick","scoop"},
            new[]{"Phone","Tablet","Touch screen, needs charging","Carried in a pocket for making calls","Big screen, a bit awkward to carry around","pocket","big screen"},
            new[]{"WeChat","QQ","You can chat and send pictures","Moments and red packets, even Mom uses it","Penguin avatar, sun and moon levels","Moments","penguin"},
            new[]{"Coffee","Milk tea","A cup in your hand, often a queue to buy","Bitter, keeps you awake late at night","Sweet, with optional pearls and sugar level","bitter","pearls"}};
        static readonly string[] Names={"你","灵光","小周","阿杰"},NamesEn={"You","LingGuang","Xiao Zhou","A-Jie"};
        static string Nm(int i)=>XgUi.T(Names[i],NamesEn[i]);
        readonly bool[] alive=new bool[4];readonly int[] sides=new int[4],marks=new int[4],descSide=new int[4];readonly string[] desc=new string[4];
        readonly List<string> history=new List<string>();
        string[] pair,pairEn;int odd,round,phase,goodVotes;float delay;
        public XgUndercoverGame(XgMiniContext c):base(c){}
        public override void Build(){int pi=C.Random.Next(Pairs.Length);pair=Pairs[pi];pairEn=PairsEn[pi];odd=C.Random.Next(4);int majority=C.Random.Next(2);for(int i=0;i<4;i++){alive[i]=true;sides[i]=i==odd?1-majority:majority;}round=goodVotes=0;history.Clear();NextRound();}
        void NextRound(){round++;phase=0;for(int i=0;i<4;i++){marks[i]=-1;desc[i]=null;descSide[i]=-1;}Draw();}
        void Draw(){
            Clear(XgUi.T("谁是卧底 · 第 "+round+" 轮","Who Is Undercover · Round "+round),XgUi.T("四人里一人拿相近词。先描述，再判断他人描述与你的词是否相像，最后投票。","One of the four holds a similar-but-different word. First describe, then judge whether the others' descriptions resemble your word, then vote."));
            T("Word",20,82,860,36,XgUi.T("你的词：「"+pair[sides[0]]+"」  · 不要直接泄露词语","Your word: \""+pairEn[sides[0]]+"\"  · Do not give the word away"),22,purple);
            for(int i=0;i<4;i++){
                int k=i;float x=18+i*220;Shade(x,126,210,194);T("Name"+i,x+12,134,185,30,Nm(i)+(i==2?XgUi.T("（老周师弟）"," (junior)"):"")+(alive[i]?"":XgUi.T("（已出局）"," (out)")),21);
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
            bool correct=selected==odd;if(correct)goodVotes++;C.Report(correct);
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
                T("Explain",32,351,836,55,XgUi.T("灵光学得越好，越会顺着别人的话装。看描述说了什么，别看语气。","The better LingGuang learns, the better it plays along. Weigh what is said, not the tone."),18);
                B("Again",330,415,240,50,XgUi.T("重新发词","Deal new words"),Build);
            }else B("Continue",330,415,240,50,XgUi.T("再描述一轮","Describe another round"),NextRound);
        }
        public override void Tick(float dt){if(!disposed&&C.IsActive&&phase==1){delay-=dt;if(delay<=0)RevealDescriptions();}}
    }

    /// <summary>
    /// 开心辞典 on the 常识判断 desk: the quiz-show frame (twelve questions up a ladder, a clock, safe points, two lifelines) around
    /// the desk's own yes/no statements, answered with 对 / 错. The statements come from the desk's bank (XgMemes.SenseBank) and 求助灵光
    /// answers with its real yes/no ability (XgSim.SenseHelp), so early on it is often wrong.
    /// </summary>
    sealed class XgQuizGame : XgSocialSurface
    {
        const float QuestionSeconds = 15;
        readonly HashSet<string> used = new HashSet<string>();
        XgPhrase statement;
        int correct;
        bool ended, locked, aiUsed, swapUsed;
        float left;
        TMP_Text hint, ladder;
        XgBtn yes, no;
        public XgQuizGame(XgMiniContext c) : base(c) { }
        public override void Build() { correct = 0; ended = locked = aiUsed = swapUsed = false; used.Clear(); NextQuestion(false); }

        void NextQuestion(bool keepTime)
        {
            statement = XgMiniContent.SenseStatement(C.Random, correct, C.Sim.Today, used);
            used.Add(statement.text);
            if (!keepTime) left = QuestionSeconds;
            locked = false; Draw();
        }
        string Statement => XgUi.T(statement.text, string.IsNullOrEmpty(statement.textEn) ? statement.text : statement.textEn);
        string Why => XgUi.T(statement.why, string.IsNullOrEmpty(statement.whyEn) ? statement.why : statement.whyEn);
        static string Word(bool value) => value ? XgUi.T("对", "True") : XgUi.T("错", "False");

        void Draw()
        {
            Clear(XgUi.T("开心辞典 · 第 " + (correct + 1) + " / 12 题", "Quiz Show · Question " + (correct + 1) + " / 12"),
                XgUi.T("判断这句话对不对，答对一题往上爬一级。第 4、8 题是安全点；求助灵光和换一题各有一次。",
                    "Is the statement true or false? Every right answer climbs a rung. Questions 4 and 8 are safe points; you get one ask-LingGuang and one swap."));
            Shade(20, 94, 620, 150); T("Question", 42, 108, 575, 124, Statement, 31);
            timer = T("Timer", 708, 8, 175, 36, "", 19); Clock(left);
            yes = B("True", 20, 262, 300, 128, Word(true), () => Answer(true), 46);
            no = B("False", 340, 262, 300, 128, Word(false), () => Answer(false), 46);
            yes.image.color = new Color32(214, 238, 224, 255); yes.label.color = green;
            no.image.color = new Color32(246, 220, 222, 255); no.label.color = red;
            Shade(666, 91, 214, 282); ladder = T("Ladder", 681, 102, 187, 260, Ladder(), 14);
            hint = T("AiHint", 25, 398, 410, 74, XgUi.T("拿不准的话，可以问问灵光。", "Not sure? You can ask LingGuang."), 17, purple);
            hint.textWrappingMode = TextWrappingModes.Normal;
            B("Ai", 666, 381, 214, 30, XgUi.T("求助灵光" + (aiUsed ? "（已用）" : ""), "Ask LingGuang" + (aiUsed ? " (used)" : "")), UseAi, 16);
            B("Swap", 666, 416, 214, 30, XgUi.T("换一题" + (swapUsed ? "（已用）" : ""), "Swap question" + (swapUsed ? " (used)" : "")), Swap, 15);
            B("Quit", 686, 450, 175, 25, XgUi.T("放弃并保留成绩", "Quit and keep score"), () => Finish(false, true), 14);
        }

        string Ladder()
        {
            string s = XgUi.T("已答对 " + correct + " 题\n安全点 " + XgMiniSocialRules.SafeLevel(correct) + "\n\n", "Correct: " + correct + "\nSafe level: " + XgMiniSocialRules.SafeLevel(correct) + "\n\n");
            for (int n = 12; n >= 1; n--)
                s += (n == correct + 1 ? "> " : "  ") + n + "  " + (n == 4 || n == 8 ? XgUi.T("安全点", "safe") : "") + (n <= correct ? XgUi.T(" 已通过", " cleared") : "") + "\n";
            return s;
        }

        void Answer(bool says)
        {
            if (ended || locked) return;
            locked = true;
            bool ok = says == statement.yes;
            C.ReportTimed(ok, QuestionSeconds - left);
            if (ok) correct++;
            ladder.text = Ladder();
            yes.button.interactable = no.button.interactable = false;
            hint.text = XgUi.T(ok ? "答对了：" : "答错了：", ok ? "Right: " : "Wrong: ") + Why; hint.color = ok ? green : red;
            if (!ok) { Finish(false, false); return; }
            if (correct >= 12) { Finish(true, false); return; }
            B("NextQuestion", 450, 408, 190, 56, XgUi.T("继续下一题", "Next question"), () => NextQuestion(false), 19);
        }

        void UseAi()
        {
            if (ended || locked || aiUsed) return;
            aiUsed = true;
            // Its real yes/no ability decides: a coin toss early on, never wrong on a taught statement once it has learned.
            bool says = C.Sim.SenseHelp(statement, C.Random);
            var mark = says ? yes : no;
            mark.label.text = Word(says) + "  <size=45%><color=#734EB8>" + XgUi.T("灵光选了这个", "LingGuang's pick") + "</color></size>";
            double accuracy = C.Sim.YesNoAccuracy;
            bool learned = accuracy >= XgSim.SenseVoiceLearned;
            hint.text = XgUi.T("灵光觉得：「" + Word(says) + "」。" + (learned ? "这类题它学会了。" : "它现在大约能答对 " + (int)Math.Round(accuracy * 100) + "%。"),
                "LingGuang says \"" + Word(says) + "\". " + (learned ? "It has learned this kind." : "It gets about " + (int)Math.Round(accuracy * 100) + "% of these right."));
            hint.color = purple;
        }

        void Swap() { if (ended || locked || swapUsed) return; swapUsed = true; NextQuestion(true); }

        void Finish(bool won, bool quit)
        {
            if (ended) return;
            ended = true; locked = true;
            int kept = won || quit ? correct : XgMiniSocialRules.SafeLevel(correct);
            string reveal = !won && !quit
                ? XgUi.T("\n刚才那句其实是「" + Word(statement.yes) + "」：" + statement.why, "\nThat one was actually \"" + Word(statement.yes) + "\": " + Why)
                : "";
            End(won ? XgUi.T("十二题通关", "All twelve cleared") : quit ? XgUi.T("见好就收", "Quit while ahead") : XgUi.T("挑战结束", "Challenge over"),
                XgUi.T("本局答对 " + correct + " 题，保住第 " + kept + " 级。" + reveal, "You got " + correct + " right and keep rung " + kept + "." + reveal), Build);
        }

        public override void Tick(float dt)
        {
            if (disposed || ended || locked || !C.IsActive) return;
            left -= dt; Clock(left);
            if (left <= 0) { Finish(false, false); return; }
            if (C.KeyDown(Key.Digit1) || C.KeyDown(Key.LeftArrow)) Answer(true);
            else if (C.KeyDown(Key.Digit2) || C.KeyDown(Key.RightArrow)) Answer(false);
        }
    }
}
