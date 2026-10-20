"""2026-10-21 题库扩充补丁：让每个文字类题包在开放当天就有 20+ 道可抽的题。

用法:  python -I apply_question_expansion.py <Unity 工程根目录>  [--check]
每处替换都要求原文锚点恰好出现一次；任何一处不符就一个文件都不写。--check 只校验不写。
"""
import sys, os, re

M = 'Assets/LingGuangV05/XingGuang/Desktop/MiniGames/'
C = 'Assets/LingGuangV05/XingGuang/Core/'
EDITS = []
def edit(path, old, new): EDITS.append((path, old, new))

# ── 基类：按游戏日期抽题，并避开最近出过的题 ──
edit(M + 'XgPackPuzzles.cs',
'''        protected void ClearPredictionFeedback(){predictionCaptured=false;predictionCorrect=null;predictedAnswer="";}''',
'''        protected void ClearPredictionFeedback(){predictionCaptured=false;predictionCorrect=null;predictedAnswer="";}
        // Picks a question already possible on the in-game date (since: yyyymmdd, 0 = any day) and skips the last few shown.
        protected int PickFresh(int[] since,List<int> recent)
        {
            var open=new List<int>();for(int i=0;i<since.Length;i++)if(since[i]==0||C.Sim.Today>=since[i])open.Add(i);
            var fresh=open.FindAll(i=>!recent.Contains(i));if(fresh.Count==0)fresh=open;
            int pick=fresh[C.Random.Next(fresh.Count)];
            recent.Add(pick);while(recent.Count>Math.Max(1,open.Count/2))recent.RemoveAt(0);
            return pick;
        }''')

# ── 找不同（语义辨析） 6 → 26 ──
edit(M + 'XgPackPuzzles.cs',
'''        static readonly string[][] Zh={new[]{"猫","狗","鱼","桌子"},new[]{"苹果","梨","香蕉","显卡"},new[]{"键盘","鼠标","显示器","西瓜"},new[]{"北京","上海","广州","月球"},new[]{"GTX 1070","GTX 1080","Titan X","RX 480"},new[]{"红色","蓝色","绿色","三角形"}};
        static readonly string[][] En={new[]{"Cat","Dog","Fish","Table"},new[]{"Apple","Pear","Banana","Graphics card"},new[]{"Keyboard","Mouse","Monitor","Watermelon"},new[]{"Beijing","Shanghai","Guangzhou","Moon"},new[]{"GTX 1070","GTX 1080","Titan X","RX 480"},new[]{"Red","Blue","Green","Triangle"}};
        public XgOddPack(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int i=C.Random.Next(Zh.Length); if(i==4 && C.Sim.Today<20160803)i=0;''',
'''        // The last option is always the odd one. 2026-10-21: 20 more sets with 2016 things, each shown only from its date.
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
        public XgOddPack(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int i=PickFresh(Since,Recent);''')

# ── 短句找错字（文字纠错） 6 → 30 ──
G = M + 'XgStageLessonGames.cs'
edit(G, '''using System;
using UnityEngine;''', '''using System;
using System.Collections.Generic;
using UnityEngine;''')
edit(G,
'''        static readonly string[][] Zh={new[]{"我","已经","准被","好了"},new[]{"请","保持","安静"},new[]{"今天","天气","晴朗"},new[]{"这件事","值得","在接再厉"},new[]{"他","戴上","眼睛"},new[]{"出门前","记得","检察","门窗"}};
        static readonly string[][] En={new[]{"I","am","redy","now"},new[]{"Please","keep","quiet"},new[]{"Today's","weather","is sunny"},new[]{"This","deserves","more effert"},new[]{"He","wears","glassess"},new[]{"Before leaving","remember to","chek","the windows"}};
        static readonly int[] Bad={2,-1,-1,2,2,2};
        static readonly string[] Fix={"准备","没错","没错","再接再厉","眼镜","检查"},FixEn={"ready","No mistake","No mistake","effort","glasses","check"};
        public XgSentenceLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int n=C.Random.Next(Zh.Length);''',
'''        // 2026-10-21: 24 more sentences (homophone slips, many from 2016); dated ones appear only from the day they could be written.
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
        public XgSentenceLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int n=PickFresh(Since,Recent);''')

# ── 翻译打分 6 → 21 ──
edit(G,
'''        static readonly string[] Sources={"请明天上午九点给我打电话。","我把钥匙落在家里了。","这条路比那条路更短。","我已经在这里住了两年。","我对海鲜过敏。","下雨了，我们把窗户关上吧。"};
        static readonly string[] Candidates={"Please call me at nine tomorrow morning.","I forgot my keys at school.","This road is longer than that one.","I have lived here for two years.","I really enjoy eating seafood.","It is raining. Let us open the windows."};
        static readonly int[] Grades={0,1,1,0,2,2},WrongWords={-1,5,3,-1,-1,-1};
        int question;bool pinpoint;
        public XgTranslationLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            question=C.Random.Next(Sources.Length);''',
'''        // 2026-10-21: 15 more pairs so the stage-five desk has 20+ from its first day.
        static readonly string[] Sources={"请明天上午九点给我打电话。","我把钥匙落在家里了。","这条路比那条路更短。","我已经在这里住了两年。","我对海鲜过敏。","下雨了，我们把窗户关上吧。",
            "里约奥运会八月开幕。","我的手机屏幕摔碎了。","这台电脑的显卡是 GTX 1080。","外面在下大雨，记得带伞。","我昨天晚上在网吧通宵。","这首歌在网上很火。","请把音量调小一点。","快递明天下午送到。",
            "我不喜欢吃辣的。","下周一学校开学。","别忘了给手机充电。","电影院里请保持安静。","这个游戏我已经通关了。","我们约好周末去爬山。","他每天坐地铁上班。"};
        static readonly string[] Candidates={"Please call me at nine tomorrow morning.","I forgot my keys at school.","This road is longer than that one.","I have lived here for two years.","I really enjoy eating seafood.","It is raining. Let us open the windows.",
            "The Rio Olympics open in August.","My phone screen got cracked.","This computer's graphics card is a GTX 1070.","It's raining hard outside; remember your umbrella.","I stayed up all night at a library yesterday.","This song is very popular online.","Please turn the volume up a little.","The parcel arrives tomorrow afternoon.",
            "I really love spicy food.","School starts next Monday.","Don't forget to charge your laptop.","Please make some noise in the cinema.","I have already finished this game.","We agreed to go swimming this weekend.","He takes the bus to work every day."};
        static readonly int[] Grades={0,1,1,0,2,2, 0,0,1,0,1,0,1,0, 2,0,1,2,0,1,1},WrongWords={-1,5,3,-1,-1,-1, -1,-1,7,-1,7,-1,4,-1, -1,-1,5,-1,-1,4,3};
        static readonly List<int> Recent=new List<int>();
        int question;bool pinpoint;
        public XgTranslationLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            question=PickFresh(new int[Sources.Length],Recent);''')

# ── 挑好答案 5 → 20 ──
edit(G,
'''        static readonly string[] Q={"我把电脑电源拔了，文件还会在吗？","水烧开以后一直冒泡，是怎么回事？","帮我把‘请明天回复’说得礼貌一点。","明天有重要会议，今晚怎么安排？","我忘记一个单词，能先给你描述吗？"};
        static readonly string[] QEn={"Will my files remain after unplugging my PC?","Why does boiling water keep bubbling?","Make 'reply tomorrow' more polite.","I have an important meeting tomorrow. What should I do tonight?","Can I describe a word I cannot remember?"};
        static readonly string[] Good={"保存到硬盘的文件通常还在；没保存的修改可能丢失。","水受热汽化，产生的水蒸气形成气泡。","方便的话，能请你明天回复吗？谢谢。","先准备资料、设好闹钟，再留出足够睡眠。","可以。告诉我它的意思、用途或你记得的片段。"};
        static readonly string[] GoodEn={"Files saved to disk usually remain; unsaved changes may be lost.","Heat turns water into vapor, forming bubbles.","Could you please reply tomorrow if convenient? Thank you.","Prepare your materials and alarm, then allow enough sleep.","Yes. Describe its meaning, use, or any part you remember."};
        static readonly string[] Bad={"拔电源会把所有硬盘文件立刻清空。","因为水里一直有鱼在呼吸。","你明天必须回复，不许拖延！","不需要准备，熬夜玩到早上就好。","不行，必须先知道完整拼写才能问。"};
        static readonly string[] BadEn={"Unplugging instantly erases all files on every drive.","Fish in the water keep breathing.","You must reply tomorrow. No excuses!","Skip preparation and play games until morning.","No. You must know the full spelling before asking."};
        public XgBetterAnswerLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int i=C.Random.Next(Q.Length);''',
'''        // 2026-10-21: 15 more so the stage-six desk has 20+.
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
        public XgBetterAnswerLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int i=PickFresh(global::LingGuangV05.XingGuang.XgQuestionBank.Join(new int[Q.Length-global::LingGuangV05.XingGuang.XgQuestionBank.CompareSince.Length],global::LingGuangV05.XingGuang.XgQuestionBank.CompareSince),Recent);''')

# ── 中式英语改错 4 → 21；唐诗下一字 五言/七言各 20；划卡英文兜底 ──
P = M + 'XgPackContentGames.cs'
edit(P, '''using System;
using LingGuangV05.XingGuang;''', '''using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;''')
edit(P, '''            promptZh=phrase.text;promptEn=phrase.textEn;''',
'''            promptZh=phrase.text;promptEn=string.IsNullOrEmpty(phrase.textEn)?phrase.text:phrase.textEn; // authored banks are Chinese-only''')
edit(P,
'''        static readonly string[] Five={"床前明月光","疑是地上霜","举头望明月","低头思故乡","白日依山尽","黄河入海流","欲穷千里目","更上一层楼","春眠不觉晓","处处闻啼鸟"};
        static readonly string[] Seven={"日照香炉生紫烟","遥看瀑布挂前川","飞流直下三千尺","疑是银河落九天","两岸猿声啼不住","轻舟已过万重山"};''',
'''        // 2026-10-21: 20 lines per bank (锄禾/草/相思; 早发白帝城/黄鹤楼/九月九日/绝句 added).
        static readonly string[] Five={"床前明月光","疑是地上霜","举头望明月","低头思故乡","白日依山尽","黄河入海流","欲穷千里目","更上一层楼","春眠不觉晓","处处闻啼鸟",
            "锄禾日当午","汗滴禾下土","谁知盘中餐","粒粒皆辛苦","离离原上草","一岁一枯荣","野火烧不尽","春风吹又生","红豆生南国","春来发几枝"};
        static readonly string[] Seven={"日照香炉生紫烟","遥看瀑布挂前川","飞流直下三千尺","疑是银河落九天","两岸猿声啼不住","轻舟已过万重山",
            "朝辞白帝彩云间","千里江陵一日还","故人西辞黄鹤楼","烟花三月下扬州","孤帆远影碧空尽","唯见长江天际流","独在异乡为异客","每逢佳节倍思亲",
            "遥知兄弟登高处","遍插茱萸少一人","两个黄鹂鸣翠柳","一行白鹭上青天","窗含西岭千秋雪","门泊东吴万里船"};''')
edit(P,
'''"I really like this game.","I am very like this game.","I very likes this game.","I like very this game."}};
        static readonly string[][] Spans={new[]{"Good good","study,","day day","up."},new[]{"Please","don't","touch","me."},new[]{"Carefully","slide."},new[]{"I","very","like","this game."}};
        static readonly int[][] ErrorSpans={new[]{0,2},new[]{3},new[]{1},new[]{1}};
        static readonly string[] ContextEn={"A slogan about diligent study and daily progress.","A museum display sign warning visitors not to touch the exhibit.","A sign warning that the floor is slippery.","Express strong enjoyment of this game."};
        static readonly string[] ReasonZh={"good good、day day 都是逐字硬译。","标牌提醒勿触摸展品，不是在说别碰我。","地滑是 slippery，不是让人 slide。","very 不能直接修饰 like。"};
        static readonly string[] ReasonEn={"Good good and day day are both literal mistranslations.","The sign warns about touching the exhibit, not touching the speaker.","A slippery floor is a hazard, not an instruction to slide.","Very cannot directly modify like."};
        int questionIndex;bool choosingRewrite;''',
'''"I really like this game.","I am very like this game.","I very likes this game.","I like very this game."},
            // 2026-10-21: 17 more, so the stage-five pack has 20+.
            new[]{"You can does it!","你能做到！（给朋友鼓劲）","You can do it!","You cans do it!","You can doing it!","You can did it!"},
            new[]{"Fall into the water carefully.","小心落水。（水边标牌）","Caution: deep water.","Carefully fall into water.","Be careful fall water.","Water falls carefully."},
            new[]{"Civilized toilet, convenient for you.","文明如厕，方便你我。（洗手间标牌）","Please keep the restroom clean.","Civilize the toilet.","The toilet is civilized for you.","Convenient civilized toilet."},
            new[]{"Open the light, please.","请开灯。","Please turn on the light.","Please open on the light.","Open light, please.","Please light the open."},
            new[]{"People mountain people sea.","人山人海。","There are crowds everywhere.","People are mountain and sea.","Mountain of people sea.","Many mountain people."},
            new[]{"Don't forget to brings your belongings.","别忘了带好随身物品。（车厢广播）","Don't forget to bring your belongings.","Don't forget to bringing your belongings.","Don't forget bring your belongings.","Don't forget to brought your belongings."},
            new[]{"Please minds your head.","请小心碰头。（矮门标牌）","Please mind your head.","Please minding your head.","Please minded your head.","Please to mind your head."},
            new[]{"Give you some color to see see.","给你点颜色看看。","I'll teach you a lesson.","I'll show you some colors.","Give you color see.","Some color you see see."},
            new[]{"Let me see see.","让我看看。","Let me have a look.","Let me see look.","Me see see.","Let see me."},
            new[]{"No zuo no die.","不作死就不会死。","Play with fire and you'll get burned.","No do no die.","Don't zuo die.","No die no zuo."},
            new[]{"Please don't play the phone.","上课请不要玩手机。","Please don't use your phone.","Please don't play phone.","Don't play the phones please.","Please no play phone."},
            new[]{"My computer is died.","我的电脑死机了。","My computer has frozen.","My computer is dead die.","My computer died is.","Computer my is died."},
            new[]{"I have a good news.","我有一个好消息。","I have some good news.","I have good newses.","I has a good news.","A good news I have."},
            new[]{"I am sweat a lot in this heat.","这么热，我正在大量出汗。","I am sweating a lot in this heat.","I is sweating a lot in this heat.","I am sweats a lot in this heat.","I am to sweating a lot in this heat."},
            new[]{"WiFi is weak, can't up the net.","WiFi 信号太弱，上不了网。","The Wi-Fi is weak; I can't get online.","WiFi weak, up the net can't.","WiFi can't the net up.","Weak WiFi, net can't up."},
            new[]{"Hi, I want a ticket go Shanghai.","你好，我要一张去上海的票。","Hi, I'd like a ticket to Shanghai.","Hi, a ticket go Shanghai want.","I want go Shanghai ticket.","Hi, ticket want go Shanghai."},
            new[]{"Please don't walks on the grass.","请不要踩草坪。（草坪标牌）","Please don't walk on the grass.","Please don't walking on the grass.","Please don't walked on the grass.","Please doesn't walk on the grass."}};
        static readonly string[][] Spans={new[]{"Good good","study,","day day","up."},new[]{"Please","don't","touch","me."},new[]{"Carefully","slide."},new[]{"I","very","like","this game."},
            new[]{"You can","does","it!"},new[]{"Fall into","the water","carefully."},new[]{"Civilized","toilet,","convenient","for you."},new[]{"Open","the light,","please."},
            new[]{"People","mountain","people","sea."},new[]{"Don't forget","to brings","your belongings."},new[]{"Please","minds","your head."},new[]{"Give you","some color","to see","see."},
            new[]{"Let me","see","see."},new[]{"No","zuo","no","die."},new[]{"Please","don't","play","the phone."},new[]{"My computer","is","died."},
            new[]{"I have","a","good news."},new[]{"I","am sweat","a lot","in this heat."},new[]{"WiFi is weak,","can't","up","the net."},new[]{"Hi,","I want","a ticket","go Shanghai."},
            new[]{"Please","don't","walks","on the grass."}};
        static readonly int[][] ErrorSpans={new[]{0,2},new[]{3},new[]{1},new[]{1},
            new[]{1},new[]{0,2},new[]{0,2},new[]{0},
            new[]{1,3},new[]{1},new[]{1},new[]{1,3},
            new[]{2},new[]{1},new[]{2},new[]{1,2},
            new[]{1},new[]{1},new[]{2,3},new[]{3},
            new[]{2}};
        static readonly string[] ContextEn={"A slogan about diligent study and daily progress.","A museum display sign warning visitors not to touch the exhibit.","A sign warning that the floor is slippery.","Express strong enjoyment of this game.",
            "Encouraging a friend by saying that they can do it.","A sign warning people near deep water.","A restroom sign asking people to keep it clean.","Asking someone to switch on the light.",
            "Describing a huge crowd.","A reminder on a bus or train not to forget to bring your belongings.","A polite warning at a low doorway asking people to mind their heads.","A movie-style threat to teach someone a lesson.",
            "Asking to take a look at something.","Warning that reckless behaviour brings trouble.","A teacher asking students not to use phones in class.","Complaining that the computer stopped responding.",
            "Announcing good news to friends.","Describing how much you are sweating right now in the heat.","Complaining that the internet isn't working.","Buying a train ticket at the station.",
            "A lawn sign asking visitors not to walk on the grass."};
        static readonly string[] ReasonZh={"good good、day day 都是逐字硬译。","标牌提醒勿触摸展品，不是在说别碰我。","地滑是 slippery，不是让人 slide。","very 不能直接修饰 like。",
            "情态动词 can 后面要用动词原形 do，不能用 does。","标牌是提醒别落水，不是让人小心地掉下去。","文明如厕是请保持卫生，逐字翻成 civilized、convenient 意思全变了。","开灯用 turn on，不用 open。",
            "人山人海是人很多，逐字译成 mountain、sea 外国人看不懂。","forget to do 表示别忘了去做某事；不定式 to 后应接原形 bring，不能接 brings。","祈使句要用动词原形 mind；please 后不能用第三人称单数 minds。","给你点颜色看看是要教训人，see see 是逐字重复。",
            "看看不用重复 see，说 have a look。","zuo 是拼音，外国人看不懂，可以说 play with fire。","玩手机说 use your phone，play 后面通常接游戏或乐器。","死机是 frozen；is died 语法也不对。",
            "news 不可数，不能加 a。","描述正在出汗时，进行时应为 am sweating；am 后不能直接接动词原形 sweat。","上网是 get online，up the net 是逐字硬译。","去上海的票是 a ticket to Shanghai，不用 go。",
            "否定祈使句用 do not 加动词原形；应为 walk，不能用 walks。"};
        static readonly string[] ReasonEn={"Good good and day day are both literal mistranslations.","The sign warns about touching the exhibit, not touching the speaker.","A slippery floor is a hazard, not an instruction to slide.","Very cannot directly modify like.",
            "The modal verb can takes the base form do, not does.","The sign warns against falling in, not how to fall in.","Word-for-word civilized and convenient change the meaning entirely.","Lights are turned on, not opened.",
            "It means a huge crowd; mountain and sea are word-for-word.","In this reminder, to introduces an infinitive and requires the base form bring, not brings.","An imperative uses the base form mind; please does not take the third-person form minds here.","It means teaching someone a lesson; see see is a literal repeat.",
            "No need to repeat see; say have a look.","Zuo is pinyin; English speakers would say play with fire.","Phones are used; play usually takes a game or an instrument.","A stuck computer has frozen; is died is also ungrammatical.",
            "News is uncountable, so no a.","For an action happening now, use the progressive am sweating; am cannot directly take the base verb sweat.","Going online is get online; up the net is word-for-word.","A ticket to Shanghai needs no go.",
            "A negative imperative uses do not followed by the base verb walk, not walks."};
        static readonly List<int> Recent=new List<int>();
        int questionIndex;bool choosingRewrite;''')
edit(P, '''            questionIndex=C.Random.Next(Zh.Length);choosingRewrite=false;''',
'''            questionIndex=PickFresh(new int[Zh.Length],Recent);choosingRewrite=false;''')

# ── 谁是卧底 6 → 20 组 ──
D = M + 'XgSocialGames.Deduction.cs'
edit(D, '''            new[]{"咖啡","奶茶","一杯拿在手里，常排队买","苦苦的，熬夜时提神","甜甜的，可选珍珠和糖度","苦","珍珠"}};''',
'''            new[]{"咖啡","奶茶","一杯拿在手里，常排队买","苦苦的，熬夜时提神","甜甜的，可选珍珠和糖度","苦","珍珠"},
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
            new[]{"西瓜","哈密瓜","夏天切开吃的瓜","绿皮红瓤黑籽","黄绿皮橙色瓤，新疆的甜","红瓤","新疆"}};''')
edit(D, '''            new[]{"Coffee","Milk tea","A cup in your hand, often a queue to buy","Bitter, keeps you awake late at night","Sweet, with optional pearls and sugar level","bitter","pearls"}};''',
'''            new[]{"Coffee","Milk tea","A cup in your hand, often a queue to buy","Bitter, keeps you awake late at night","Sweet, with optional pearls and sugar level","bitter","pearls"},
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
            new[]{"Watermelon","Hami melon","A melon you slice in summer","Green rind, red flesh, black seeds","Yellow-green rind, orange flesh, sweet from Xinjiang","red flesh","Xinjiang"}};''')

# ── 原诗还是改编 8 → 20 ──
edit(C + 'XgTeachingPoetry.cs',
'''"I raise my head to the bright moon, then lower it to play on the computer.",false,"举头望明月，低头思故乡。","I raise my head to the bright moon, then lower it and think of home.","李白《静夜思》","Li Bai, Quiet Night Thoughts")
        };''',
'''"I raise my head to the bright moon, then lower it to play on the computer.",false,"举头望明月，低头思故乡。","I raise my head to the bright moon, then lower it and think of home.","李白《静夜思》","Li Bai, Quiet Night Thoughts"),
            // 2026-10-21: six more poems, each as the original and a 2016 parody (20 quotes).
            new XgPoemQuote("锄禾日当午，汗滴禾下土。","Hoeing at high noon, sweat drips onto the soil.",true,"锄禾日当午，汗滴禾下土。","Hoeing at high noon, sweat drips onto the soil.","李绅《悯农》","Li Shen, Pity the Farmers"),
            new XgPoemQuote("锄禾日当午，WiFi 没信号。","Hoeing at high noon, and no Wi-Fi signal.",false,"锄禾日当午，汗滴禾下土。","Hoeing at high noon, sweat drips onto the soil.","李绅《悯农》","Li Shen, Pity the Farmers"),
            new XgPoemQuote("离离原上草，一岁一枯荣。","Lush grass on the plain; each year it withers and thrives.",true,"离离原上草，一岁一枯荣。","Lush grass on the plain; each year it withers and thrives.","白居易《赋得古原草送别》","Bai Juyi, Grass on the Ancient Plain"),
            new XgPoemQuote("离离原上草，显卡又涨价。","Lush grass on the plain; graphics cards went up again.",false,"离离原上草，一岁一枯荣。","Lush grass on the plain; each year it withers and thrives.","白居易《赋得古原草送别》","Bai Juyi, Grass on the Ancient Plain"),
            new XgPoemQuote("两个黄鹂鸣翠柳，一行白鹭上青天。","Two orioles sing in green willows; a line of egrets climbs the blue sky.",true,"两个黄鹂鸣翠柳，一行白鹭上青天。","Two orioles sing in green willows; a line of egrets climbs the blue sky.","杜甫《绝句》","Du Fu, Quatrain"),
            new XgPoemQuote("两个黄鹂鸣翠柳，一行代码改到天亮。","Two orioles sing in green willows; one line of code, fixed until dawn.",false,"两个黄鹂鸣翠柳，一行白鹭上青天。","Two orioles sing in green willows; a line of egrets climbs the blue sky.","杜甫《绝句》","Du Fu, Quatrain"),
            new XgPoemQuote("独在异乡为异客，每逢佳节倍思亲。","Alone, a stranger in a strange land; at every festival I miss my family more.",true,"独在异乡为异客，每逢佳节倍思亲。","Alone, a stranger in a strange land; at every festival I miss my family more.","王维《九月九日忆山东兄弟》","Wang Wei, Remembering My Brothers on the Double Ninth"),
            new XgPoemQuote("独在异乡为异客，每逢佳节抢红包。","Alone, a stranger in a strange land; at every festival I grab red packets.",false,"独在异乡为异客，每逢佳节倍思亲。","Alone, a stranger in a strange land; at every festival I miss my family more.","王维《九月九日忆山东兄弟》","Wang Wei, Remembering My Brothers on the Double Ninth"),
            new XgPoemQuote("红豆生南国，春来发几枝。","Red beans grow in the south; how many sprigs in spring?",true,"红豆生南国，春来发几枝。","Red beans grow in the south; how many sprigs in spring?","王维《相思》","Wang Wei, Longing"),
            new XgPoemQuote("红豆生南国，快递几天到。","Red beans grow in the south; how many days until delivery?",false,"红豆生南国，春来发几枝。","Red beans grow in the south; how many sprigs in spring?","王维《相思》","Wang Wei, Longing"),
            new XgPoemQuote("朝辞白帝彩云间，千里江陵一日还。","At dawn I leave Baidi amid bright clouds; a thousand li to Jiangling in a day.",true,"朝辞白帝彩云间，千里江陵一日还。","At dawn I leave Baidi amid bright clouds; a thousand li to Jiangling in a day.","李白《早发白帝城》","Li Bai, Leaving Baidi at Dawn"),
            new XgPoemQuote("朝辞白帝彩云间，千里快递一日还。","At dawn I leave Baidi amid bright clouds; a thousand-li parcel, back in a day.",false,"朝辞白帝彩云间，千里江陵一日还。","At dawn I leave Baidi amid bright clouds; a thousand li to Jiangling in a day.","李白《早发白帝城》","Li Bai, Leaving Baidi at Dawn")
        };''')

# ── 弹幕夸还是骂 14 → 30 ──
edit(C + 'XgTeachingDanmu.cs',
'''            new XgDatedDanmu("主播辛苦了，讲得真清楚。","Thanks for streaming. That explanation was clear.",0),''',
'''            new XgDatedDanmu("主播辛苦了，讲得真清楚。","Thanks for streaming. That explanation was clear.",0),
            // 2026-10-21: 14 undated lines so the stage-two pack opens with 20+, plus two Rio lines.
            new XgDatedDanmu("解说声音真好听，听着很舒服。","The commentator's voice is so pleasant to listen to.",0),
            new XgDatedDanmu("画质糊成这样，还好意思开直播？","Picture this blurry and you still go live?",1),
            new XgDatedDanmu("哇，又掉线了，网速真快呢。","Wow, disconnected again. Such fast internet.",2),
            new XgDatedDanmu("这一手微操，我回放看了三遍。","That micro-play — I watched the replay three times.",0),
            new XgDatedDanmu("能不能别刷屏了，烦死了。","Can you stop spamming the chat? So annoying.",1),
            new XgDatedDanmu("这波团战指挥得太稳了！","That team-fight call was rock steady!",0),
            new XgDatedDanmu("好棒哦，又在泉水里挂机了。","Great job, idling at base again.",2),
            new XgDatedDanmu("主播唱得真好听，再来一首！","The streamer sings so well. One more!",0),
            new XgDatedDanmu("广告比正片还长，受不了了。","The ads are longer than the show. I can't stand it.",1),
            new XgDatedDanmu("真是大师操作，闪现撞墙。","A true master: flashed straight into a wall.",2),
            new XgDatedDanmu("UP 主剪得用心，字幕都对齐了。","The uploader edited with care; even the subtitles line up.",0),
            new XgDatedDanmu("别吵了，好好看比赛行不行。","Stop arguing and just watch the match.",1),
            new XgDatedDanmu("厉害厉害，一局死了十五次。","Impressive: fifteen deaths in one game.",2),
            new XgDatedDanmu("这个结局没想到，编剧牛！","Didn't see that ending coming. Great writing!",0),
            new XgDatedDanmu("里约开幕式看得我热血沸腾！","The Rio opening ceremony fired me up!",0,20160806),
            new XgDatedDanmu("女排这一局打得太提气了！","Women's volleyball, that set was so uplifting!",0,20160816),''')

# ── 刷单评论 19 → 25 ──
edit(C + 'XgMemes.cs',
'''            P("五星，因为老板答应返现五块", true, "好评返现买来的评价，也算刷的。", "好评返现", trick: true),''',
'''            P("五星，因为老板答应返现五块", true, "好评返现买来的评价，也算刷的。", "好评返现", trick: true),
            // 2026-10-21: six more reviews for variety.
            new XgPhrase { text="五星好评，亲们闭眼入，不买后悔。刷单任务已完成，请按约返现五元。", textEn="Five stars, buy without hesitation! Fake-review task complete; please send the agreed five-yuan cashback.", why="明确提到完成刷单任务和约定返现，不能只凭好评语气判断。", whyEn="It explicitly mentions a fake-review task and agreed cashback; praise alone is not proof.", yes=true, source="淘宝刷单", since=20150101, level=1, trick=false },
            new XgPhrase { text="收到了，手感很好，好评好评，老板发财。按刷单群模板写好了，截图已交，等返现。", textEn="Received, feels great, great review, best wishes to the seller. Copied the fake-review group template and submitted the screenshot; waiting for cashback.", why="评论明确暴露刷单群模板、截图交任务和返现。", whyEn="It explicitly reveals a fake-review group template, screenshot submission and cashback.", yes=true, source="淘宝刷单", since=20150101, level=1, trick=false },
            new XgPhrase { text="自拍杆收到，蓝牙连了两次才连上，拍照还行", textEn="The selfie stick arrived. Bluetooth took two tries to connect; photos are fine.", why="有具体的使用过程和小毛病。", whyEn="It describes a specific use experience and a small fault.", yes=false, source="真实评价", since=20150101, level=1, trick=false },
            new XgPhrase { text="充电宝标一万毫安，实际给手机充了两次多一点", textEn="The power bank is rated at 10,000 mAh; it actually charged my phone a little over twice.", why="有具体的实测结果。", whyEn="It gives a specific result from actual use.", yes=false, source="真实评价", since=20150101, level=1, trick=false },
            new XgPhrase { text="网线比想象中短，好在够从路由器拉到电脑", textEn="The network cable is shorter than expected, but reaches from the router to my PC.", why="具体的长度和用法。", whyEn="It describes the cable length in a specific use case.", yes=false, source="真实评价", since=20150101, level=1, trick=false },
            new XgPhrase { text="晒图返两元红包，所以给五星", textEn="Posting photos earns a two-yuan cashback, so I gave five stars.", why="好评返现买来的评价，也算刷的。", whyEn="A positive review bought with cashback also counts as a paid review.", yes=true, source="好评返现", since=20150101, level=3, trick=true },''')

# ── 标题党：一半当月新闻，一半题库 ──
edit(C + 'XgSim.TeachingTools.cs',
'''            XgPhrase Pick()=>dataset=="headline"?(XgTeachingHeadlines.Pick(Today,random)??XgMemes.Pick(dataset,random,3,Today,"",false)):XgMemes.Pick(dataset,random,3,Today,"",false);''',
'''            // Headlines: half from this month's browser news, half from the authored bank, so every stage has 20+ distinct items.
            XgPhrase Pick()=>dataset=="headline"&&random.Next(2)==0?(XgTeachingHeadlines.Pick(Today,random)??XgMemes.Pick(dataset,random,3,Today,"",false)):XgMemes.Pick(dataset,random,3,Today,"",false);''')


# ════ 第二阶段：每个题库接上 XgQuestionBank（build_bank.py 生成）里的约 1000 道新题 ════
B = 'global::LingGuangV05.XingGuang.XgQuestionBank'
def append_ctor(path, anchor, cls, body):
    edit(path, anchor, f'        static {cls}(){{{body}}} // appends {B.split(".")[-1]} (2026-10-21)\n' + anchor)
append_ctor(G, '''        public XgSentenceLesson(XgMiniContext c):base(c){}''', 'XgSentenceLesson',
    f'Zh={B}.Join(Zh,{B}.SentenceZh);En={B}.Join(En,{B}.SentenceEn);Bad={B}.Join(Bad,{B}.SentenceBad);Fix={B}.Join(Fix,{B}.SentenceFix);FixEn={B}.Join(FixEn,{B}.SentenceFixEn);Since={B}.Join(Since,{B}.SentenceSince);')
append_ctor(G, '''        public XgTranslationLesson(XgMiniContext c):base(c){}''', 'XgTranslationLesson',
    f'Sources={B}.Join(Sources,{B}.TranslationSources);Candidates={B}.Join(Candidates,{B}.TranslationCandidates);Grades={B}.Join(Grades,{B}.TranslationGrades);WrongWords={B}.Join(WrongWords,{B}.TranslationWrongWords);')
append_ctor(G, '''        public XgBetterAnswerLesson(XgMiniContext c):base(c){}''', 'XgBetterAnswerLesson',
    f'Q={B}.Join(Q,{B}.CompareQ);QEn={B}.Join(QEn,{B}.CompareQEn);Good={B}.Join(Good,{B}.CompareGood);GoodEn={B}.Join(GoodEn,{B}.CompareGoodEn);Bad={B}.Join(Bad,{B}.CompareBad);BadEn={B}.Join(BadEn,{B}.CompareBadEn);')
Z = M + 'XgPackPuzzles.cs'
append_ctor(Z, '''        public XgOddPack(XgMiniContext c):base(c){}''', 'XgOddPack',
    f'Zh={B}.Join(Zh,{B}.OddZh);En={B}.Join(En,{B}.OddEn);Since={B}.Join(Since,{B}.OddSince);')
append_ctor(Z, '''        public XgIdiomPack(XgMiniContext c):base(c) {index=c.Random.Next(Words.Length-1);}''', 'XgIdiomPack',
    f'Words={B}.Join(Words,{B}.Idioms);')
append_ctor(P, '''        public XgPackPoem(XgMiniContext c):base(c){}''', 'XgPackPoem',
    f'Five={B}.Join(Five,{B}.Five);Seven={B}.Join(Seven,{B}.Seven);')
append_ctor(P, '''        public XgPackEnglish(XgMiniContext c):base(c){}''', 'XgPackEnglish',
    f'Zh={B}.Join(Zh,{B}.EnglishZh);Spans={B}.Join(Spans,{B}.EnglishSpans);ErrorSpans={B}.Join(ErrorSpans,{B}.EnglishErrorSpans);ContextEn={B}.Join(ContextEn,{B}.EnglishContextEn);ReasonZh={B}.Join(ReasonZh,{B}.EnglishReasonZh);ReasonEn={B}.Join(ReasonEn,{B}.EnglishReasonEn);')
append_ctor(D, '''        public XgUndercoverGame(XgMiniContext c):base(c){}''', 'XgUndercoverGame',
    f'Pairs={B}.Join(Pairs,{B}.Pairs);PairsEn={B}.Join(PairsEn,{B}.PairsEn);')
append_ctor(C + 'XgTeachingDanmu.cs', '''        public static XgDatedDanmu Pick(int today,Random random)''', 'XgTeachingDanmu',
    'Lines=XgQuestionBank.Join(Lines,XgQuestionBank.Danmu);')
append_ctor(C + 'XgTeachingPoetry.cs', '''        public static string Explain(XgPoemQuote q,bool english)''', 'XgTeachingPoetry',
    'Quotes=XgQuestionBank.Join(Quotes,XgQuestionBank.Quotes);')
append_ctor(C + 'XgMemes.cs', '''        public static XgPhrase[] Bank(string desk)''', 'XgMemes',
    'Spam=XgQuestionBank.Join(Spam,XgQuestionBank.Spam);Review=XgQuestionBank.Join(Review,XgQuestionBank.Review);Headline=XgQuestionBank.Join(Headline,XgQuestionBank.Headline);')
NEW_FILES = [('XgQuestionBank.cs', C + 'XgQuestionBank.cs')]  # (file next to this script, project path)


def main():
    root = sys.argv[1]; check = '--check' in sys.argv
    here = os.path.dirname(os.path.abspath(__file__))
    for local, target in NEW_FILES:
        if not os.path.exists(os.path.join(here, local)): print('NOT APPLIED: run build_bank.py first (missing ' + local + ')'); sys.exit(1)
    texts, problems = {}, []
    for path, old, new in EDITS:
        full = os.path.join(root, path)
        if path not in texts:
            if not os.path.exists(full): problems.append('missing file ' + path); texts[path] = None; continue
            raw = open(full, 'rb').read(); texts[path] = (raw.startswith(b'\xef\xbb\xbf'), raw.decode('utf-8-sig'))
        if texts[path] is None: continue
        bom, t = texts[path]
        crlf = '\r\n' in t
        o, n = (old.replace('\n', '\r\n'), new.replace('\n', '\r\n')) if crlf else (old, new)
        # Already applied? Ignore phase-2 constructor lines, which land inside phase-1 blocks.
        plain = re.sub(r'[ \t]*static \w+\(\)\{[^\n]*// appends XgQuestionBank[^\n]*\r?\n', '', t)
        if n in t or n in plain: continue
        if t.count(o) != 1: problems.append(f'{path}: anchor found {t.count(o)}x: {old[:60]!r}'); continue
        texts[path] = (bom, t.replace(o, n))
    if problems:
        print('NOT APPLIED:\n  ' + '\n  '.join(problems)); sys.exit(1)
    if check: print('check OK:', len(EDITS), 'edits in', len(texts), 'files'); return
    for path, v in texts.items():
        bom, t = v
        open(os.path.join(root, path), 'wb').write((b'\xef\xbb\xbf' if bom else b'') + t.encode('utf-8'))
    for local, target in NEW_FILES:
        open(os.path.join(root, target), 'wb').write(open(os.path.join(here, local), 'rb').read())
    print('applied', len(EDITS), 'edits to', len(texts), 'files, plus', len(NEW_FILES), 'generated file(s)')

if __name__ == '__main__':
    main()
