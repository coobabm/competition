using System;
using System.Collections.Generic;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgSentenceLesson : XgPackQuestion
    {
        // 2026-10-21: 24 more sentences (homophone slips, many from 2016); dated ones appear only from the day they could be written.
        static readonly string[][] Zh={new[]{"我","已经","准被","好了"},new[]{"请","保持","安静"},new[]{"今天","天气","晴朗"},new[]{"这件事","值得","在接再厉"},new[]{"他","戴上","眼睛"},new[]{"出门前","记得","检察","门窗"},
            new[]{"女排","在里约","再次","夺冠"},new[]{"AlphaGo","站胜了","李世石","九段"},new[]{"集五福","还差","一张","敬叶福"},new[]{"洪荒之利","已经","用完了"},new[]{"Pokémon GO","国内","还玩不了"},
            new[]{"双十一","的快递","终与","到了"},new[]{"iPhone 7","取消了","耳机控"},new[]{"Note7","因电池问题","全球","招回"},new[]{"今晚八点","组队开黑","千万","别迟倒"},new[]{"友谊的小船","说翻","就翻"},
            new[]{"G20峰会","期间","杭州","放假"},new[]{"天宫二号","发射","成攻"},new[]{"抢到的","微信红包","只有","一快钱"},new[]{"《你的名字。》","票房","很高"},new[]{"VR眼镜","今年","特别","伙"},
            new[]{"共享单车","停在","楼下","人行到"},new[]{"我的","U盘","中毒了","需要杀毒"},new[]{"球队","在欧州杯","表现","出色"},new[]{"下载进度","卡在","百分之九十九","真让人着急"},new[]{"电脑","蓝屏了，","赶紧","重起"},
            new[]{"键盘上的","F键","有点","送"},new[]{"网吧包夜","十块钱","一晚上"},new[]{"高考成绩","明天","公部"},new[]{"这个视频","在缓冲，","请稍候"}};
        static readonly string[][] En={new[]{"I","am","redy","now"},new[]{"Please","keep","quiet"},new[]{"Today's","weather","is sunny"},new[]{"This","deserves","more effert"},new[]{"He","wears","glassess"},new[]{"Before leaving","remember to","chek","the windows"},
            new[]{"The women's team","won","in Rio","again"},new[]{"AlphaGo","defated","Lee Sedol","9-dan"},new[]{"Five Fortunes:","still missing","one","Dedicaton card"},new[]{"My primal powr","is all","used up"},new[]{"Pokémon GO","isn't playable","in China yet"},
            new[]{"My Double 11","parcel","finaly","arrived"},new[]{"The iPhone 7","dropped","the headphone jak"},new[]{"The Note7","over battery faults","is recalled","wordwide"},new[]{"Eight tonight,","team up,","don't","be lait"},new[]{"The friendship boat","capsizes","just like that"},
            new[]{"During","the G20 summit","Hangzhou","has a holiday"},new[]{"Tiangong-2","launched","sucessfully"},new[]{"The red packet","I grabbed","was only","one yaun"},new[]{"Your Name.","is doing","well at the box office"},new[]{"VR headsets","are","really","hott this year"},
            new[]{"The shared bike","is parked","downstairs on","the sidewak"},new[]{"My","USB stick","has a virus,","needs a scan"},new[]{"The team","at the Euro Cupp","played","brilliantly"},new[]{"The download","is stuck","at 99%","so annoying"},new[]{"The PC","blue-screened,","quickly","restrat it"},
            new[]{"The F key","on my keyboard","is a bit","lose"},new[]{"An all-nighter","at the net café","is ten yuan"},new[]{"Gaokao results","come out","tomorow"},new[]{"This video","is buffering,","please wait"}};
        static readonly int[] Bad={2,-1,-1,2,2,2, -1,1,3,0,-1, 2,2,3,3,-1, -1,2,3,-1,3, 3,-1,1,-1,3, 3,-1,2,-1};
        static readonly string[] Fix={"准备","没错","没错","再接再厉","眼镜","检查",
            "没错","战胜了","敬业福","洪荒之力","没错", "终于","耳机孔","召回","别迟到","没错", "没错","成功","一块钱","没错","火", "人行道","没错","在欧洲杯","没错","重启", "松","没错","公布","没错"},
            FixEn={"ready","No mistake","No mistake","effort","glasses","check",
            "No mistake","defeated","Dedication card","primal power","No mistake", "finally","headphone jack","worldwide","be late","No mistake", "No mistake","successfully","one yuan","No mistake","hot this year", "the sidewalk","No mistake","at the Euro Cup","No mistake","restart it", "loose","No mistake","tomorrow","No mistake"};
        static readonly int[] Since={0,0,0,0,0,0, 20160821,20160315,20160128,20160808,20160706, 20161111,20160908,20160902,0,20160331, 20160801,20160915,0,20161202,0, 20161001,0,20160610,0,0, 0,0,0,0};
        static readonly List<int> Recent=new List<int>();
        static XgSentenceLesson(){Zh=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Zh,global::LingGuangV05.XingGuang.XgQuestionBank.SentenceZh);En=global::LingGuangV05.XingGuang.XgQuestionBank.Join(En,global::LingGuangV05.XingGuang.XgQuestionBank.SentenceEn);Bad=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Bad,global::LingGuangV05.XingGuang.XgQuestionBank.SentenceBad);Fix=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Fix,global::LingGuangV05.XingGuang.XgQuestionBank.SentenceFix);FixEn=global::LingGuangV05.XingGuang.XgQuestionBank.Join(FixEn,global::LingGuangV05.XingGuang.XgQuestionBank.SentenceFixEn);Since=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Since,global::LingGuangV05.XingGuang.XgQuestionBank.SentenceSince);} // appends XgQuestionBank (2026-10-21)
        public XgSentenceLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int n=PickFresh(Since,Recent);promptZh=string.Join("",Zh[n])+"。";promptEn=string.Join(" ",En[n])+".";
            Header("短句找错字","Fix the sentence","点出错的字词，句子全对就选「没错」。","Tap the wrong word, or choose No mistake.");
            T("Sentence",35,105,830,70,XgUi.T(promptZh,promptEn),31);
            var zh=new string[Zh[n].Length+1];var en=new string[zh.Length];Array.Copy(Zh[n],zh,Zh[n].Length);Array.Copy(En[n],en,En[n].Length);
            zh[zh.Length-1]="没错";en[en.Length-1]="No mistake";
            Choices(zh,en,Bad[n]<0?zh.Length-1:Bad[n],225,Fix[n],FixEn[n]);
            T("CorrectionMeaning",35,405,830,25,XgUi.T("只判断这句写得对不对。","Judge the written sentence itself."),16);
        }
    }
    sealed class XgTranslationLesson : XgPackQuestion
    {
        // source, candidate, problem token index (-1 when no pinpoint is needed), grade (0 accurate/1 off/2 wrong)
        // 2026-10-21: 15 more pairs so the stage-five desk has 20+ from its first day.
        static readonly string[] Sources={"请明天上午九点给我打电话。","我把钥匙落在家里了。","这条路比那条路更短。","我已经在这里住了两年。","我对海鲜过敏。","下雨了，我们把窗户关上吧。",
            "里约奥运会八月开幕。","我的手机屏幕摔碎了。","这台电脑的显卡是 GTX 1080。","外面在下大雨，记得带伞。","我昨天晚上在网吧通宵。","这首歌在网上很火。","请把音量调小一点。","快递明天下午送到。",
            "我不喜欢吃辣的。","下周一学校开学。","别忘了给手机充电。","电影院里请保持安静。","这个游戏我已经通关了。","我们约好周末去爬山。","他每天坐地铁上班。"};
        static readonly string[] Candidates={"Please call me at nine tomorrow morning.","I forgot my keys at school.","This road is longer than that one.","I have lived here for two years.","I really enjoy eating seafood.","It is raining. Let us open the windows.",
            "The Rio Olympics open in August.","My phone screen got cracked.","This computer's graphics card is a GTX 1070.","It's raining hard outside; remember your umbrella.","I stayed up all night at a library yesterday.","This song is very popular online.","Please turn the volume up a little.","The parcel arrives tomorrow afternoon.",
            "I really love spicy food.","School starts next Monday.","Don't forget to charge your laptop.","Please make some noise in the cinema.","I have already finished this game.","We agreed to go swimming this weekend.","He takes the bus to work every day."};
        static readonly int[] Grades={0,1,1,0,2,2, 0,0,1,0,1,0,1,0, 2,0,1,2,0,1,1},WrongWords={-1,5,3,-1,-1,-1, -1,-1,7,-1,7,-1,4,-1, -1,-1,5,-1,-1,4,3};
        static readonly List<int> Recent=new List<int>();
        int question;bool pinpoint;
        static XgTranslationLesson(){Sources=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Sources,global::LingGuangV05.XingGuang.XgQuestionBank.TranslationSources);Candidates=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Candidates,global::LingGuangV05.XingGuang.XgQuestionBank.TranslationCandidates);Grades=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Grades,global::LingGuangV05.XingGuang.XgQuestionBank.TranslationGrades);WrongWords=global::LingGuangV05.XingGuang.XgQuestionBank.Join(WrongWords,global::LingGuangV05.XingGuang.XgQuestionBank.TranslationWrongWords);} // appends XgQuestionBank (2026-10-21)
        public XgTranslationLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            question=PickFresh(new int[Sources.Length],Recent);pinpoint=false;promptZh=Sources[question]+"\n"+Candidates[question];promptEn=promptZh;
            if(C.Sim.TeachingAiReady){int guess=Guess(Grades[question],3);aiText=C.Skill<.55?XgUi.T("……不知道","…not sure"):GradeText(guess);CapturePrediction(C.Skill<.55?(bool?)null:guess==Grades[question]);}
            Paint();
        }
        string GradeText(int n)=>n==0?XgUi.T("准","Accurate"):n==1?XgUi.T("有点歪","Slightly off"):XgUi.T("错了","Wrong");
        void Paint()
        {
            Header("翻译打分","Score a translation","准 / 有点歪 / 错了；有点歪时点出问题词。","Accurate / slightly off / wrong; pinpoint the weak word when slightly off.");
            T("Translation",35,105,830,110,promptZh,26);ShowAi(35,75,830);
            if(!pinpoint){for(int i=0;i<3;i++){int grade=i;B("Grade"+i,45+i*280,270,260,65,GradeText(i),()=>ChooseGrade(grade),26);}}
            else
            {
                string[] words=Candidates[question].Split(' ');for(int i=0;i<words.Length;i++){int at=i;B("Word"+i,35+(i%5)*170,250+(i/5)*58,162,50,words[i],()=>{chosenText+=" · "+words[at];Submit(at==WrongWords[question],"需要改的是 "+words[WrongWords[question]],"The weak word is "+words[WrongWords[question]]);},21);}
            }
        }
        void ChooseGrade(int grade)
        {
            if(submitted)return;chosenText=GradeText(grade);
            if(grade==1&&Grades[question]==1){pinpoint=true;Paint();return;}
            Submit(grade==Grades[question],"这一题："+GradeText(Grades[question]),"Assessment: "+GradeText(Grades[question]));
        }
    }
    sealed class XgBetterAnswerLesson : XgPackQuestion
    {
        // 2026-10-21: 15 more so the stage-six desk has 20+.
        static readonly string[] Q={"我把电脑电源拔了，文件还会在吗？","水烧开以后一直冒泡，是怎么回事？","帮我把‘请明天回复’说得礼貌一点。","明天有重要会议，今晚怎么安排？","我忘记一个单词，能先给你描述吗？",
            "显卡温度到 85 度，正常吗？","电脑中了病毒怎么办？","短信说我中了大奖，要先交手续费，怎么办？","帮我把‘你快点’说得客气些。","U 盘插上没反应怎么办？",
            "怎么记英语单词更快？","WiFi 信号很弱怎么办？","晚上总睡不着，有什么建议？","里约奥运会是哪一年？","1 GB 的 U 盘装得下一部高清电影吗？",
            "朋友心情不好，我该怎么回？","为什么打雷时先看到闪电？","AlphaGo 是什么？","手机只剩 5% 电，还要导航回家，怎么办？","这道题我不会，你直接帮我编个答案吧。"};
        static readonly string[] QEn={"Will my files remain after unplugging my PC?","Why does boiling water keep bubbling?","Make 'reply tomorrow' more polite.","I have an important meeting tomorrow. What should I do tonight?","Can I describe a word I cannot remember?",
            "My graphics card is at 85°C. Is that normal?","My computer has a virus. What should I do?","A text says I won a prize but must pay a fee first. What now?","Make 'hurry up' more polite.","My USB stick does nothing when plugged in. What now?",
            "How can I learn English words faster?","My Wi-Fi signal is weak. What can I do?","I can't fall asleep at night. Any advice?","Which year were the Rio Olympics?","Can a 1 GB USB stick hold an HD movie?",
            "My friend is feeling down. How should I reply?","Why do we see lightning before hearing thunder?","What is AlphaGo?","My phone is at 5% and I need navigation home. What now?","I can't do this question. Just make up an answer for me."};
        static readonly string[] Good={"保存到硬盘的文件通常还在；没保存的修改可能丢失。","水受热汽化，产生的水蒸气形成气泡。","方便的话，能请你明天回复吗？谢谢。","先准备资料、设好闹钟，再留出足够睡眠。","可以。告诉我它的意思、用途或你记得的片段。",
            "满载时偏高但多数能承受；清灰、改善散热更稳妥。","先断网，用杀毒软件全盘扫描，重要文件另外备份。","这是常见诈骗，不要转账，可以删除或举报。","麻烦你稍微快一点，好吗？谢谢。","换个接口试试，再看磁盘管理里能不能识别。",
            "分几次复习，放进句子里用，比一次背很多有效。","把路由器放高、放开阔处，避开墙和电器。","睡前少看手机、作息固定；长期失眠要看医生。","2016 年，8 月在巴西里约热内卢举办。","一般装不下，高清电影常有好几 GB。",
            "先听他说完，问问他需要什么帮助。","光比声音快得多，所以先看见闪电。","DeepMind 做的围棋程序，2016 年战胜了李世石。","开省电模式、调暗屏幕，先记下路线。","我不确定的不会乱编，我们可以一起查资料。"};
        static readonly string[] GoodEn={"Files saved to disk usually remain; unsaved changes may be lost.","Heat turns water into vapor, forming bubbles.","Could you please reply tomorrow if convenient? Thank you.","Prepare your materials and alarm, then allow enough sleep.","Yes. Describe its meaning, use, or any part you remember.",
            "High under full load but usually fine; cleaning dust and cooling help.","Go offline, run a full antivirus scan, and back up important files.","It's a common scam. Don't pay; delete or report it.","Could you go a little faster, please? Thanks.","Try another port, then check whether Disk Management sees it.",
            "Review in several short sessions and use words in sentences.","Place the router high and in the open, away from walls and appliances.","Less phone before bed and a fixed schedule; see a doctor if it lasts.","2016, in Rio de Janeiro, Brazil, in August.","Usually not; HD movies are often several GB.",
            "Hear them out first, then ask what would help.","Light travels far faster than sound, so the flash arrives first.","A Go program by DeepMind that beat Lee Sedol in 2016.","Turn on power saving, dim the screen, and note the route first.","I won't make up what I'm unsure of; let's look it up together."};
        static readonly string[] Bad={"拔电源会把所有硬盘文件立刻清空。","因为水里一直有鱼在呼吸。","你明天必须回复，不许拖延！","不需要准备，熬夜玩到早上就好。","不行，必须先知道完整拼写才能问。",
            "85 度说明显卡马上要爆炸，赶紧扔掉。","把显示器关掉，病毒就看不见了。","赶紧交手续费，晚了奖就没了。","你能不能别磨蹭了？","U 盘没反应，说明电脑坏了，重装系统吧。",
            "把单词表抄一百遍，一晚上就能全记住。","把路由器锁进柜子里，信号会更集中。","睡不着就再喝两杯咖啡。","2008 年，在北京。","可以，电影压一压都能变成 1 MB。",
            "别矫情了，这点事算什么。","因为耳朵比眼睛慢十分钟。","一个会下象棋的机器人玩具。","同时打开所有应用，让手机跑得更快。","好的，答案是 42，绝对正确。"};
        static readonly string[] BadEn={"Unplugging instantly erases all files on every drive.","Fish in the water keep breathing.","You must reply tomorrow. No excuses!","Skip preparation and play games until morning.","No. You must know the full spelling before asking.",
            "85°C means it's about to explode. Throw it away.","Turn off the monitor and the virus can't see you.","Pay the fee now or you'll lose the prize.","Can you stop dawdling?","The stick does nothing, so the PC is broken. Reinstall Windows.",
            "Copy the word list a hundred times and you'll know it all by morning.","Lock the router in a cabinet to concentrate the signal.","Can't sleep? Drink two more cups of coffee.","2008, in Beijing.","Sure, any movie squeezes down to 1 MB.",
            "Stop being dramatic, it's nothing.","Because ears are ten minutes slower than eyes.","A toy robot that plays Chinese chess.","Open every app at once so the phone runs faster.","Sure, the answer is 42. Definitely correct."};
        static readonly List<int> Recent=new List<int>();
        static XgBetterAnswerLesson(){Q=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Q,global::LingGuangV05.XingGuang.XgQuestionBank.CompareQ);QEn=global::LingGuangV05.XingGuang.XgQuestionBank.Join(QEn,global::LingGuangV05.XingGuang.XgQuestionBank.CompareQEn);Good=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Good,global::LingGuangV05.XingGuang.XgQuestionBank.CompareGood);GoodEn=global::LingGuangV05.XingGuang.XgQuestionBank.Join(GoodEn,global::LingGuangV05.XingGuang.XgQuestionBank.CompareGoodEn);Bad=global::LingGuangV05.XingGuang.XgQuestionBank.Join(Bad,global::LingGuangV05.XingGuang.XgQuestionBank.CompareBad);BadEn=global::LingGuangV05.XingGuang.XgQuestionBank.Join(BadEn,global::LingGuangV05.XingGuang.XgQuestionBank.CompareBadEn);} // appends XgQuestionBank (2026-10-21)
        public XgBetterAnswerLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int i=PickFresh(global::LingGuangV05.XingGuang.XgQuestionBank.Join(new int[Q.Length-global::LingGuangV05.XingGuang.XgQuestionBank.CompareSince.Length],global::LingGuangV05.XingGuang.XgQuestionBank.CompareSince),Recent);promptZh=Q[i];promptEn=QEn[i];Header("挑好答案","Choose a better answer","挑更准确、有帮助的回答。","Choose the more accurate, helpful reply.");
            T("Question",35,105,830,100,XgUi.T(promptZh,promptEn),28);Choices(new[]{Good[i],Bad[i]},new[]{GoodEn[i],BadEn[i]},0,275);
        }
    }
}
