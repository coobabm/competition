using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    abstract class XgPackQuestion : XgSocialSurface
    {
        protected bool submitted;
        protected string promptZh, promptEn;
        protected XgCard QuestionCard;
        protected string last = "";
        int nextFrame;
        protected float elapsed;
        Action[] keys = new Action[0];
        protected string chosenText = "", aiText = "";
        string predictedAnswer="",lastAiName="";
        bool predictionCaptured,lastAiVisible;
        bool? predictionCorrect,lastAiCorrect;
        TMP_Text lastAnswerLabel,lastAiLabel;
        protected Color UnsureInk => C.Ui.dark ? XgDark.Muted : new Color32(93,101,115,255);
        protected Color PredictionInk => predictionCaptured && predictedAnswer==aiText && !predictionCorrect.HasValue ? UnsureInk : purple;
        protected XgPackQuestion(XgMiniContext c) : base(c) { }
        public override void Build() { submitted = false; elapsed = 0; QuestionCard=null; chosenText=aiText=""; ClearPredictionFeedback(); keys=new Action[0]; DrawQuestion(); }
        protected abstract void DrawQuestion();
        protected void ClearPredictionFeedback(){predictionCaptured=false;predictionCorrect=null;predictedAnswer="";}
        // Picks a question already possible on the in-game date (since: yyyymmdd, 0 = any day) and skips the last few shown.
        protected int PickFresh(int[] since,List<int> recent)
        {
            var open=new List<int>();for(int i=0;i<since.Length;i++)if(since[i]==0||C.Sim.Today>=since[i])open.Add(i);
            var fresh=open.FindAll(i=>!recent.Contains(i));if(fresh.Count==0)fresh=open;
            int pick=fresh[C.Random.Next(fresh.Count)];
            recent.Add(pick);while(recent.Count>Math.Max(1,open.Count/2))recent.RemoveAt(0);
            return pick;
        }
        // Capture the displayed answer before the player's action can train or settle it.
        protected void CapturePrediction(bool? correct)
        {
            predictedAnswer=aiText;predictionCaptured=!string.IsNullOrEmpty(aiText);
            bool unsure=aiText=="……不知道"||aiText=="…not sure"||aiText=="不知道"||aiText=="Not sure"||aiText=="无法判断"||aiText=="Cannot determine";
            predictionCorrect=unsure?null:correct;
        }
        protected void Header(string zh, string en, string instructionZh, string instructionEn)
        {
            Clear(XgUi.T(zh,en),XgUi.T(instructionZh,instructionEn));
            lastAnswerLabel=T("LastAnswer",20,437,860,18,last,15);
            lastAiLabel=T("LastAiAnswer",20,455,860,19,"",14);
            PaintLastFeedback();
        }
        void PaintLastFeedback()
        {
            if(lastAnswerLabel!=null)lastAnswerLabel.text=last;
            if(lastAiLabel==null)return;
            lastAiLabel.text=!lastAiVisible?"":XgUi.T("上题 ","Previous: ")+lastAiName+XgUi.T("：",": ")
                +(lastAiCorrect.HasValue?(lastAiCorrect.Value?XgUi.T("✓ 答对","✓ correct"):XgUi.T("× 答错","× incorrect")):XgUi.T("… 不知道","… not sure"));
            lastAiLabel.color=!lastAiCorrect.HasValue?UnsureInk:lastAiCorrect.Value?green:red;
            lastAiLabel.richText=false;lastAiLabel.raycastTarget=false;
        }
        protected void Submit(bool correct,string whyZh,string whyEn)
        {
            if (submitted || !C.IsActive) return;
            submitted = true; nextFrame = Time.frameCount;
            lastAiVisible=predictionCaptured;
            lastAiCorrect=predictionCorrect;lastAiName=C.Sim.AiDisplayName;
            var pack = XgTeachingPacks.Find(C.Id);
            C.Sim.RecordTeachingQuestion(promptZh,promptEn,correct,true,whyZh,whyEn,pack == null ? "sense" : pack.dataset,QuestionCard,chosenText,aiText);
            C.ReportTimed(correct,elapsed);
            last = XgUi.T(correct ? "✓ " : "× ",correct ? "✓ " : "× ") + XgUi.T(whyZh,whyEn);
            PaintLastFeedback(); // Static retained feedback; next-question timing is unchanged.
        }
        protected void GuessBinary(bool truth,string yesZh,string yesEn,string noZh,string noEn)
        {
            if(!C.Sim.TeachingAiReady)return;
            bool guess=C.Random.NextDouble()<C.Skill?truth:!truth;
            aiText=C.Skill<.55?XgUi.T("……不知道","…not sure"):guess?XgUi.T(yesZh,yesEn):XgUi.T(noZh,noEn);
            CapturePrediction(C.Skill<.55?(bool?)null:guess==truth);
        }
        protected void ShowAi(float x=25,float y=70,float width=850)
        {
            if(!C.Sim.TeachingAiReady)return;
            string answer=string.IsNullOrEmpty(aiText)?XgUi.T("……不知道","…not sure"):aiText;
            T("PackAiAnswer",x,y,width,26,C.Sim.AiDisplayName+"："+answer+(C.Sim.Has("sigmoid")?" · "+Math.Round(C.Skill*100)+"%":""),17,string.IsNullOrEmpty(aiText)?UnsureInk:PredictionInk);
        }
        protected void Choices(string[] zh,string[] en,int answer,float top = 270,string whyZh=null,string whyEn=null,float left=25,float buttonWidth=420,float columnStep=435)
        {
            keys=new Action[zh.Length];
            var order = new List<int>(); for(int i=0;i<zh.Length;i++) order.Add(i); Shuffle(order);
            for(int i=0;i<order.Count;i++)
            {
                int truthIndex=order[i]; int slot=i;
                B("Choice"+i,left+(i%2)*columnStep,top+(i/2)*64,buttonWidth,56,(i+1)+". "+XgUi.T(zh[truthIndex],en[truthIndex]),
                    keys[i]=()=>{if(submitted)return;chosenText=XgUi.T(zh[truthIndex],en[truthIndex]);ChoiceSelected(truthIndex,answer);Submit(truthIndex==answer,whyZh??zh[answer],whyEn??en[answer]);},22);
            }
            if(C.Sim.TeachingAiReady)
            {
                int guessed=Guess(answer,zh.Length);
                aiText=C.Skill<.55?XgUi.T("……不知道","…not sure"):XgUi.T(zh[guessed],en[guessed]);
                CapturePrediction(C.Skill<.55?(bool?)null:guessed==answer);
                T("AiAnswer",left,top-40,buttonWidth+columnStep,35,C.Sim.AiDisplayName+"："+aiText,20,PredictionInk);
            }
        }
        protected virtual void ChoiceSelected(int choice,int expected) { }
        public override void Tick(float dt)
        {
            if(disposed || !C.IsActive)return;
            if(submitted){if(Time.frameCount>nextFrame)Build();return;}
            elapsed+=dt;
            var kb=Keyboard.current;if(kb!=null){var digits=new[]{kb.digit1Key,kb.digit2Key,kb.digit3Key,kb.digit4Key,kb.digit5Key,kb.digit6Key};for(int i=0;i<keys.Length&&i<digits.Length;i++)if(digits[i].wasPressedThisFrame){C.PlayerInput(keys[i]);break;}}
        }
    }

    sealed class XgOddPack : XgPackQuestion
    {
        // The last option is always the odd one. 2026-10-21: 20 more sets with 2016 things, each shown only from its date.
        static readonly string[][] Zh={new[]{"猫","狗","鱼","桌子"},new[]{"苹果","梨","香蕉","显卡"},new[]{"键盘","鼠标","显示器","西瓜"},new[]{"北京","上海","广州","月球"},new[]{"GTX 1070","GTX 1080","Titan X","RX 480"},new[]{"红色","蓝色","绿色","三角形"},
            new[]{"微信","QQ","陌陌","Windows 10"},new[]{"《太阳的后裔》","《欢乐颂》","《琅琊榜》","《王者荣耀》"},new[]{"孙杨","宁泽涛","傅园慧","林丹"},new[]{"GTX 1060","GTX 1070","GTX 1080","i7-6700K"},
            new[]{"U盘","移动硬盘","SD 卡","路由器"},new[]{"Windows 7","Windows 10","OS X","Office 2016"},new[]{"《魔兽》","《美国队长3》","《疯狂动物城》","《三体》"},new[]{"里约","伦敦","雅典","索契"},
            new[]{"支付宝","微信支付","Apple Pay","淘宝"},new[]{"李世石","柯洁","古力","AlphaGo"},new[]{"红包","饺子","春联","粽子"},new[]{"iPhone 6s","小米 5","华为 P9","Kindle"},
            new[]{"斗鱼","熊猫TV","虎牙","知乎"},new[]{"Ctrl+C","Ctrl+V","Ctrl+X","Alt+F4"},new[]{"感知机","卷积网络","LSTM","Excel 表格"},new[]{"《Pokémon GO》","《守望先锋》","《皇室战争》","《权力的游戏》"},
            new[]{"MP3","WAV","FLAC","JPG"},new[]{"蓝瘦香菇","洪荒之力","友谊的小船","红烧牛肉面"},new[]{"QQ空间","微博","人人网","QQ音乐"},new[]{"春节","中秋","端午","双十一"}};
        static readonly string[][] En={new[]{"Cat","Dog","Fish","Table"},new[]{"Apple","Pear","Banana","Graphics card"},new[]{"Keyboard","Mouse","Monitor","Watermelon"},new[]{"Beijing","Shanghai","Guangzhou","Moon"},new[]{"GTX 1070","GTX 1080","Titan X","RX 480"},new[]{"Red","Blue","Green","Triangle"},
            new[]{"WeChat","QQ","Momo","Windows 10"},new[]{"Descendants of the Sun","Ode to Joy","Nirvana in Fire","Honor of Kings"},new[]{"Sun Yang","Ning Zetao","Fu Yuanhui","Lin Dan"},new[]{"GTX 1060","GTX 1070","GTX 1080","i7-6700K"},
            new[]{"USB stick","External drive","SD card","Router"},new[]{"Windows 7","Windows 10","OS X","Office 2016"},new[]{"Warcraft","Captain America: Civil War","Zootopia","The Three-Body Problem"},new[]{"Rio","London","Athens","Sochi"},
            new[]{"Alipay","WeChat Pay","Apple Pay","Taobao"},new[]{"Lee Sedol","Ke Jie","Gu Li","AlphaGo"},new[]{"Red envelopes","Dumplings","Spring couplets","Zongzi"},new[]{"iPhone 6s","Mi 5","Huawei P9","Kindle"},
            new[]{"Douyu","Panda TV","Huya","Zhihu"},new[]{"Ctrl+C","Ctrl+V","Ctrl+X","Alt+F4"},new[]{"Perceptron","Convolutional net","LSTM","Excel sheet"},new[]{"Pokémon GO","Overwatch","Clash Royale","Game of Thrones"},
            new[]{"MP3","WAV","FLAC","JPG"},new[]{"Blue thin mushroom (slang)","Primal power (slang)","Friendship boat (slang)","Braised beef noodles"},new[]{"Qzone","Weibo","Renren","QQ Music"},new[]{"Spring Festival","Mid-Autumn","Dragon Boat","Singles' Day"}};
        static readonly int[] Since={0,0,0,0,20160803,0, 0,20160418,0,20160803, 0,0,20160608,0, 20160218,20160309,0,20160415, 0,0,0,20160706, 0,20161010,0,0};
        static readonly List<int> Recent=new List<int>();
        static XgOddPack(){Zh=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Zh,global::LingGuangV05.XingGuang.XgQuestionBank.OddZh);En=global::LingGuangV05.XingGuang.XgQuestionBank.Join(En,global::LingGuangV05.XingGuang.XgQuestionBank.OddEn);Since=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Since,global::LingGuangV05.XingGuang.XgQuestionBank.OddSince);} // appends XgQuestionBank (2026-10-21)
        public XgOddPack(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int i=PickFresh(Since,Recent);
            promptZh=i==4?"哪个不是 NVIDIA 显卡？":"哪个不属于同一类？";promptEn=i==4?"Which is not an NVIDIA graphics card?":"Which belongs to a different category?";
            Header("找不同","Odd one out","点出四个选项里不属于同一类的那个。","Pick the option in a different category.");
            T("Question",35,110,830,85,XgUi.T(promptZh,promptEn),30);Choices(Zh[i],En[i],3);
        }
    }
    sealed class XgSequencePack : XgPackQuestion
    {
        public XgSequencePack(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int a=C.Random.Next(1,10),d=C.Random.Next(2,6),kind=C.Random.Next(3);
            var n=new int[5];for(int i=0;i<5;i++)n[i]=kind==0?a+i*d:kind==1?a*(int)Math.Pow(2,i):a+i*i;
            promptZh=promptEn=string.Join("，",new[]{n[0],n[1],n[2],n[3]})+"，?";
            Header("找规律","Next in line","选出规律里的下一个数。","Choose the next number in the pattern.");
            T("Sequence",35,115,830,90,promptZh,38);
            string[] opts={n[4].ToString(),(n[4]+d).ToString(),(n[4]-1).ToString(),(n[4]+1).ToString()};Choices(opts,opts,0);
        }
    }
    sealed class XgMinePack : XgPackQuestion
    {
        public XgMinePack(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            const int side=5;var mines=new bool[25];int target=C.Random.Next(25);
            for(int i=0;i<25;i++)mines[i]=C.Random.NextDouble()<.2;
            int neighbour=target%side<side-1?target+1:target-1;mines[neighbour]=false; // At least one revealed adjacent number makes the target deducible.
            promptZh="高亮格是雷吗？";promptEn="Is the highlighted cell a mine?";
            Header("扫雷判官","Minesweeper judge","数字是周围八格的地雷数。根据已经翻开的格子判断黄色格。","Numbers count adjacent mines. Deduce the yellow cell from the revealed board.");
            for(int y=0;y<side;y++)for(int x=0;x<side;x++)
            {
                int i=y*side+x,count=0;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if((dx!=0||dy!=0)&&x+dx>=0&&x+dx<side&&y+dy>=0&&y+dy<side&&mines[(y+dy)*side+x+dx])count++;
                var cell=C.Box("Cell"+i,50+x*60,95+y*60,57,57,i==target?new Color32(255,213,84,255):Color.white);
                T("Value"+i,50+x*60,95+y*60,57,57,i==target?"?":mines[i]?"●":count==0?"":count.ToString(),27,i==target?purple:green);
            }
            T("Question",395,130,450,70,XgUi.T(promptZh,promptEn),28);
            QuestionCard=new XgCard{dataset="logic",teachingImageKind="mines",teachingImageKey=target,teachingImageKeys=Array.ConvertAll(mines,m=>m?1:0)};
            bool truth=mines[target];GuessBinary(truth,"是雷","Mine","不是雷","Safe");ShowAi(395,205,450);
            B("Mine",405,245,200,75,XgUi.T("是雷","Mine"),()=>{chosenText=XgUi.T("是雷","Mine");Submit(truth,truth?"这里是雷。":"这里安全。",truth?"That cell contains a mine.":"That cell is safe.");},28);
            B("Safe",620,245,200,75,XgUi.T("不是雷","Safe"),()=>{chosenText=XgUi.T("不是雷","Safe");Submit(!truth,truth?"这里是雷。":"这里安全。",truth?"That cell contains a mine.":"That cell is safe.");},28);
        }
    }
    sealed class XgGomokuPack : XgPackQuestion
    {
        readonly int[] board=new int[81];
        public XgGomokuPack(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            Array.Clear(board,0,board.Length);int dir=C.Random.Next(4),dx=dir==1?0:1,dy=dir==0?0:dir==3?-1:1;
            int x=C.Random.Next(1,4),y=dy<0?C.Random.Next(5,8):C.Random.Next(1,4),gap=C.Random.Next(5);
            for(int i=0;i<5;i++)if(i!=gap)board[(y+i*dy)*9+x+i*dx]=1;
            for(int i=0;i<6;i++){int k=C.Random.Next(81);if(board[k]==0 && k!=(y+gap*dy)*9+x+gap*dx)board[k]=2;}
            if(C.Sim.TeachingAiReady){int guess=(y+gap*dy)*9+x+gap*dx;
                if(C.Random.NextDouble()>C.Skill){var empty=new List<int>();for(int k=0;k<81;k++)if(board[k]==0)empty.Add(k);guess=empty[C.Random.Next(empty.Count)];}
                aiText=C.Skill<.55?XgUi.T("……不知道","…not sure"):((char)('A'+guess%9)).ToString()+(guess/9+1);
                CapturePrediction(C.Skill<.55?(bool?)null:WinningPoint(guess));}
            QuestionCard=new XgCard{dataset="go",teachingImageKind="gomoku",teachingImageKeys=(int[])board.Clone()};
            promptZh="黑棋下一步在哪里连成五子？";promptEn="Where can Black make five in a row?";
            Header("五子棋 · 一步连五","Gomoku · five in a row","点击一个空位。任意合法连五都算对。","Tap an empty intersection. Any move making five counts.");
            for(int r=0;r<9;r++)for(int c=0;c<9;c++)
            {
                int k=r*9+c;var b=B("Point"+k,32+c*37,90+r*37,35,35,board[k]==0?"＋":board[k]==1?"●":"○",()=>Move(k),25);
                b.image.color=new Color32(222,185,122,255);b.label.color=board[k]==2?Color.white:Color.black;
            }
            T("Question",410,150,430,140,XgUi.T(promptZh,promptEn),30);ShowAi(410,300,430);
        }
        bool WinningPoint(int k)
        {
            if(k<0||k>=board.Length||board[k]!=0)return false;
            int x=k%9,y=k/9;int[] dx={1,0,1,1},dy={0,1,1,-1};
            for(int d=0;d<4;d++){int count=1;foreach(int sign in new[]{-1,1})for(int n=1;n<9;n++){int xx=x+dx[d]*n*sign,yy=y+dy[d]*n*sign;if(xx<0||xx>=9||yy<0||yy>=9||board[yy*9+xx]!=1)break;count++;}if(count>=5)return true;}
            return false;
        }
        void Move(int k)
        {
            if(board[k]!=0 || submitted)return;chosenText=((char)('A'+k%9)).ToString()+(k/9+1);board[k]=1;int x=k%9,y=k/9;bool win=false;
            int[] dx={1,0,1,1},dy={0,1,1,-1};
            for(int d=0;d<4;d++) {int count=1;foreach(int sign in new[]{-1,1})for(int n=1;n<9;n++){int xx=x+dx[d]*n*sign,yy=y+dy[d]*n*sign;if(xx<0||xx>=9||yy<0||yy>=9||board[yy*9+xx]!=1)break;count++;}if(count>=5)win=true;}
            Submit(win,win?"五子连成一线！":"这一手还没有连成五子。",win?"Five in a row!":"That move does not make five.");
        }
    }
    sealed class XgIdiomPack : XgPackQuestion
    {
        static readonly string[] Words={"一心一意","意气风发","发扬光大","大快人心","心想事成","成千上万","万事如意","意味深长","长生不老","老当益壮","壮志凌云","云淡风轻","轻而易举","举一反三","三心二意","意犹未尽","尽善尽美","美中不足","足智多谋","谋事在人","人山人海","海阔天空","空前绝后","后来居上","上下一心"};
        int index,nextIndex,questionRound;bool authenticity,proposalReal;string proposal;
        static XgIdiomPack(){Words=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Words,global::LingGuangV05.XingGuang.XgQuestionBank.Idioms);} // appends XgQuestionBank (2026-10-21)
        public XgIdiomPack(XgMiniContext c):base(c) {index=c.Random.Next(Words.Length-1);}
        protected override void DrawQuestion()
        {
            string word=Words[index];char end=word[word.Length-1];
            var successors=new List<int>();for(int i=0;i<Words.Length;i++)if(Words[i][0]==end)successors.Add(i);
            nextIndex=successors[C.Random.Next(successors.Count)];
            authenticity=++questionRound%4==0;
            if(authenticity){DrawAuthenticity(word);return;}
            var options=new List<string>{Words[nextIndex]};while(options.Count<3){string other=Words[C.Random.Next(Words.Length)];if(other[0]!=end&&!options.Contains(other))options.Add(other);}
            promptZh=word+" → ?";promptEn="Continue the idiom: "+word+" → ?";
            Header("成语接龙","Idiom chain","选首字与上一个成语尾字相同的真成语。","Choose a real idiom starting with the previous idiom's final character.");
            T("Idiom",35,100,830,100,word+" → ?",42);Choices(options.ToArray(),options.ToArray(),0,255);
        }
        void DrawAuthenticity(string word)
        {
            // The candidate is the question. The player's label concerns whether it is a real idiom,
            // independently of any separate AI guess above the answers.
            proposalReal=C.Random.NextDouble()<C.Skill;proposal=proposalReal?Words[nextIndex]:word[word.Length-1]+"电猫飞";
            promptZh=word+" → "+proposal+"\n这是一个真成语吗？";
            promptEn=word+" → "+proposal+"\nIs the proposed continuation a real Chinese idiom?";
            Header("它接的是真成语吗？","Is that a real idiom?","首字接上还不够，也要是一个真成语。","Matching the first character is not enough; it must also be a real idiom.");
            T("ProposedIdiom",35,100,830,150,C.Sim.AiDisplayName+XgUi.T("接：", " proposes: ")+proposal+"\n"+XgUi.T(promptZh,promptEn),28);
            Choices(new[]{"真成语","造出来的"},new[]{"Real idiom","Made up"},proposalReal?0:1,300,
                proposalReal?proposal+"是真成语。":proposal+"不是成语；可以接「"+Words[nextIndex]+"」。",
                proposalReal?proposal+" is a real idiom.":proposal+" is made up; "+Words[nextIndex]+" is a valid continuation.");
        }
        protected override void ChoiceSelected(int choice,int expected)
        {
            // A correct continuation becomes the next displayed idiom, including branches and wraparound.
            // A wrong choice starts a fresh chain; shared combo settlement handles the mistake.
            index=choice==expected?(authenticity&&!proposalReal?index:nextIndex):C.Random.Next(Words.Length);
        }
    }
}
