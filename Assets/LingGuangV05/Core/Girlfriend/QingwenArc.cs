using System;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>One answer the player can pick in a beat: what it does to her, and what she says back (zh, en pairs).</summary>
    public sealed class GfArcChoice
    {
        public string zh = "", en = "";
        public int affection;
        public string flag = "";
        public string[] reply = new string[0];
        /// <summary>What she remembers of it (her memory list, read by her prompt later). Empty = nothing.</summary>
        public string memoryZh = "";
    }

    /// <summary>One conversation of the side story: when it comes, what she says, and the answers on offer.</summary>
    public sealed class GfArcBeat
    {
        public string key = "";
        /// <summary>The lab has to be this far along (stage, abilities learned, rounds trained).</summary>
        public int stage, abilities, epochs;
        public string[] lines = new string[0];
        /// <summary>Extra lines (zh, en pairs) once he has told her he is teaching a computer (<see cref="QingwenArc.FlagToldMl"/>).</summary>
        public string[] toldMlLines = new string[0];
        public GfArcChoice[] choices = new GfArcChoice[0];
        /// <summary>For the model when he types something else: what she just asked him about.</summary>
        public string askedZh = "", askedEn = "";
    }

    /// <summary>
    /// 晴雯的支线 (2026-10-18): thirteen conversations in YY (her best friend, her family, her dorm in Wuhan) that come with the lab's progress, not with her schedule, so
    /// she is there even when the desktop clock says she is asleep or out. Each one offers three answers as buttons;
    /// they move her affection and set flags later beats read. Anything else he types goes to her model with what she
    /// just asked (<see cref="FreeSituation"/>). She never learns what the program is: he can only say he is teaching
    /// a computer. Pure; YYGirlfriend.Arc.cs plays it.
    /// </summary>
    public static class QingwenArc
    {
        /// <summary>Seconds of play between two beats.</summary>
        public const double Gap = 100;
        /// <summary>Seconds she waits for his answer before the beat closes unanswered.</summary>
        public const double OpenFor = 300;
        public const int UnansweredCost = 2;
        public const string FlagToldMl = "arc.toldMl", FlagPromisedHome = "arc.home", FlagForgot = "arc.forgot", FlagRemembered = "arc.remembered", FlagPromisedFuture = "arc.future";
        const string DonePrefix = "arc:";

        static GfArcChoice C(string zh, string en, int affection, string memoryZh, string flag, params string[] reply)
            => new GfArcChoice { zh = zh, en = en, affection = affection, memoryZh = memoryZh ?? "", flag = flag ?? "", reply = reply };

        public static readonly GfArcBeat[] Beats =
        {
            new GfArcBeat
            {
                key = "hello",
                lines = new[] { "你是不是又在弄电脑", "Let me guess. The computer again" },
                askedZh = "你问他是不是又在弄电脑。", askedEn = "You asked whether he is at the computer again.",
                choices = new[]
                {
                    C("嗯，还在弄电脑", "Yeah, still on the computer", 1, null, null,
                        "就知道[白眼]", "Knew it [白眼]", "弄完早点睡 别又通宵", "Go to bed when you're done. No all-nighter"),
                    C("想你了，睡不着", "Missed you. Can't sleep", 3, "他说想我想得睡不着", null,
                        "油嘴滑舌[偷笑]", "Smooth talker [偷笑]", "……我也是", "…Me too"),
                    C("在忙，晚点说", "Busy, talk later", -2, null, null,
                        "哦", "Oh", "那你忙吧", "Go on then"),
                },
            },
            new GfArcBeat
            {
                key = "busy", epochs = 3,
                lines = new[] { "你最近天天在电脑前 到底在弄啥", "You're at that computer every day. What are you doing?", "室友都问我你是不是在挖矿[疑问]", "My roommates think you're mining bitcoin [疑问]" },
                askedZh = "你问他最近天天在电脑前弄什么。", askedEn = "You asked what he does at the computer every day.",
                choices = new[]
                {
                    C("在自学机器学习，教电脑认字", "Teaching myself machine learning. Teaching a computer to read", 2, "他在自学机器学习，教电脑认字", FlagToldMl,
                        "认字？？", "Read??", "电脑还要你教？", "Computers need you to teach them?", "……听不懂 但感觉好厉害的样子", "…No idea what that means, but it sounds impressive", "教会了给我看看", "Show me when it learns"),
                    C("打游戏呢", "Gaming", -1, null, null,
                        "又是英雄联盟[白眼]", "League again [白眼]", "可别玩到忘记回我", "Just don't forget to reply to me"),
                    C("秘密，以后告诉你", "Secret. I'll tell you later", 0, null, null,
                        "神神秘秘的", "So mysterious", "行吧 我等着", "Fine. I'll wait"),
                },
            },
            new GfArcBeat
            {
                key = "xiaoyu", epochs = 8,
                lines = new[] { "小雨跟她男朋友吵架了", "Xiaoyu had a fight with her boyfriend", "半夜给我打电话 哭了一个多小时", "She called me at midnight and cried for over an hour", "就因为那男的打游戏 一晚上不回她消息[生气]", "All because he was gaming and ignored her messages all night [生气]", "你们男生是不是都这样", "Are all you boys like that?" },
                askedZh = "你说闺蜜赵小雨（在郑州读护理）跟男朋友吵架，因为他打游戏一晚上不回消息，她半夜打电话哭；你问他男生是不是都这样。", askedEn = "Your best friend Zhao Xiaoyu fought with her boyfriend for ignoring her while gaming; you asked whether all boys are like that.",
                choices = new[]
                {
                    C("她也在郑州？有事让她找我", "She's in Zhengzhou too? Tell her to call me if she needs anything", 2, "他说小雨在郑州有事可以找他", null,
                        "你还挺讲义气", "Look at you, Mr Loyal", "我跟她说了 她说你比她男朋友靠谱[偷笑]", "I told her. She says you're more reliable than her boyfriend [偷笑]"),
                    C("一晚上不回消息确实过分", "Ignoring her all night is out of line", 1, null, null,
                        "你也知道啊", "Oh, you know that?", "忙的时候给我留句话，好吗", "Leave me a message when you are busy, okay?"),
                    C("男生打游戏很正常", "Boys game. It's normal", -2, null, null,
                        "正常？", "Normal?", "行 我知道了", "Right. Noted"),
                },
            },
            new GfArcBeat
            {
                key = "home", stage = 2,
                lines = new[] { "我妈刚打电话 又问我有没有谈恋爱", "Mum just called and asked again whether I'm seeing anyone", "我说没有[流汗]", "I said no [流汗]", "你说……哪天跟她说比较好", "When do you think… I should tell her?" },
                askedZh = "你爸妈还不知道你们在谈恋爱。你妈刚打电话又问，你说没有，问他哪天坦白比较好。", askedEn = "Your parents don't know about you two. Your mum asked again on the phone, you said no, and you asked him when to tell her.",
                choices = new[]
                {
                    C("等我做出点样子，我去你家", "Once I've made something of myself, I'll come to your house", 3, "他说等做出点样子就来我家见我爸妈", FlagPromisedHome,
                        "真的？", "Really?", "那你得先过我爸那关 他会拉你下象棋", "You'll have to get past my dad first. He'll make you play chess"),
                    C("现在就说，我不怕", "Tell her now. I'm not scared", 1, null, null,
                        "你不怕我怕[大哭]", "You're not scared, but I am [大哭]", "她会念叨死我的", "She'll nag me to death"),
                    C("再瞒一阵吧", "Keep it quiet a bit longer", -1, null, null,
                        "嗯", "Mm", "我也是这么想的 就是有点累", "That's what I thought too. It's just tiring"),
                },
            },
            new GfArcBeat
            {
                key = "roach", stage = 2,
                lines = new[] { "啊啊啊啊啊啊", "AAAAAAAAH", "宿舍飞进来一只蟑螂 会飞的！！", "A cockroach flew into the dorm. A FLYING one!!", "我现在站在桌子上 何佳拿着拖鞋在追它", "I'm standing on my desk and He Jia's chasing it with a slipper", "你快说点什么 转移一下我注意力", "Say something, quick. Distract me" },
                askedZh = "宿舍飞进来一只会飞的蟑螂，你吓得站到桌子上，室友何佳拿拖鞋在追，你让他说点什么转移注意力。", askedEn = "A flying cockroach got into the dorm; you're standing on your desk while He Jia chases it, and you asked him to distract you.",
                choices = new[]
                {
                    C("我给你讲道题吧，像高中那样", "Let me walk you through a maths problem, like in high school", 2, "蟑螂飞进宿舍那晚他说给我讲题", null,
                        "……你有病吧[偷笑]", "…Are you mad [偷笑]", "不过真的有用 何佳打到了！！", "It actually worked though. He Jia got it!!"),
                    C("武汉的蟑螂还会飞？", "Wuhan cockroaches can fly?", 0, null, null,
                        "会！！还很大！！", "YES!! AND THEY'RE HUGE!!", "你来武汉住一晚就知道了", "Spend one night in Wuhan and you'll see"),
                    C("拍下来给我看看", "Take a picture for me", -1, null, null,
                        "你是不是有病", "Are you out of your mind?", "我手都在抖", "My hands are shaking"),
                },
            },
            new GfArcBeat
            {
                key = "exam", stage = 3,
                lines = new[] { "下周就期末考了", "Finals are next week", "财务管理我一章都没看完[大哭]", "I haven't finished a single chapter of Financial Management [大哭]", "雨桐都看第二遍了", "Yutong's already on her second read" },
                askedZh = "你下周期末考，财务管理一章都没看完，室友陈雨桐都看第二遍了，你很慌，想要他安慰。", askedEn = "Finals are next week and you haven't finished a chapter of Financial Management. You're panicking and want comfort.",
                choices = new[]
                {
                    C("别慌，现在开始还来得及", "Don't panic. There's still time", 1, null, null,
                        "你说得轻巧", "Easy for you to say", "……嗯 那我今晚先看两章", "…Mm. I'll do two chapters tonight"),
                    C("挂了就补考呗，没事", "If you fail, you resit. No big deal", -2, null, null,
                        "你就不能盼我点好吗[生气]", "Could you at least hope I pass [生气]", "乌鸦嘴", "Jinx"),
                    C("今晚视频，我陪你复习", "Video call tonight. I'll study with you", 3, "期末前他陪我视频复习", null,
                        "你又不会会计", "You don't even know accounting", "……那十点 不许迟到", "…Ten o'clock then. Don't be late"),
                },
            },
            new GfArcBeat
            {
                key = "cat", stage = 3,
                lines = new[] { "我爸刚打电话说 煤球两天没吃东西了", "Dad just called. Meiqiu hasn't eaten for two days", "他骑电动车带它去了镇上的兽医站", "He took him to the vet in town on the e-bike", "医生说它老了 肠胃不好[委屈]", "The vet says he's old and his stomach's bad [委屈]", "我爸说它平时趴在电视上 这两天老钻我床底下", "Dad says he usually lies on the TV, but these two days he keeps hiding under my bed" },
                askedZh = "你家的黑猫煤球两天没吃东西，你爸带它去了兽医站，医生说是老了；它这两天老钻你床底下，你在武汉回不去，很担心。", askedEn = "Your black cat Meiqiu stopped eating; the vet says he's getting old and he keeps hiding under your bed. You're stuck in Wuhan and worried.",
                choices = new[]
                {
                    C("它是想你了。八岁了吧？", "He misses you. He's eight now, right?", 3, "煤球生病的时候他记得煤球八岁了", null,
                        "你还记得它几岁", "You remember how old he is", "……考完试我就回去 天天抱着它", "…I'm going home right after finals to hold him every day"),
                    C("让你爸给它买点好猫粮", "Get your dad to buy him good cat food", 1, null, null,
                        "医生开了处方粮 好贵", "The vet prescribed special food. So expensive", "我爸二话没说就买了", "Dad bought it without a word"),
                    C("猫过两天就好了", "Cats bounce back in a couple of days", -1, null, null,
                        "你又没养过猫", "You've never had a cat", "算了", "Never mind"),
                },
            },
            new GfArcBeat
            {
                key = "mengmeng", stage = 3,
                lines = new[] { "王萌说她们营业厅要裁人", "Wang Meng says her phone shop is laying people off", "她可能要去深圳打工了", "She might go to Shenzhen for work", "高中我们三个说好一辈子待一块的", "In high school the three of us swore we'd always stick together", "现在一个郑州一个武汉 她再一走就散完了", "Now it's Zhengzhou and Wuhan. If she goes too, that's all of us scattered" },
                askedZh = "你高中同学王萌在县城移动营业厅上班，要被裁了，可能去深圳打工，你有点难过。", askedEn = "Your high-school friend Wang Meng may lose her job at the county phone shop and go to Shenzhen. You're sad about it.",
                choices = new[]
                {
                    C("散了也还能在 YY 上聚", "Even apart, there's always YY", 2, null, null,
                        "也是", "True", "我去把「一中高三七班」那个群翻出来", "I'll dig out the \"No.1 High, Class 7\" group"),
                    C("深圳机会多，是好事", "Shenzhen has jobs. It's a good thing", 1, null, null,
                        "她也这么说", "That's what she says", "就是要坐好久的火车", "It's just such a long train ride"),
                    C("我们俩不也是一个郑州一个武汉", "We're apart too, me in Zhengzhou and you in Wuhan", 0, null, null,
                        "……", "…", "那不一样 你又跑不了[哼]", "That's different. You can't get away [哼]"),
                },
            },
            new GfArcBeat
            {
                key = "day", stage = 4,
                lines = new[] { "问你个事", "Question for you", "你还记得你答应过我什么吗", "Do you remember what you promised me?" },
                askedZh = "你问他还记不记得答应过你什么（他答应过不再通宵弄电脑，也答应过你六级过了带你去鼓浪屿）。", askedEn = "You asked whether he remembers what he promised you (no more all-nighters at the computer; Gulangyu once you pass CET-6).",
                choices = new[]
                {
                    C("不再通宵弄电脑……", "No more all-nighters at the computer…", 3, "他记得答应过我不再通宵", FlagRemembered,
                        "算你有良心", "So you do have a heart", "那现在几点了[白眼]", "So what time is it now [白眼]"),
                    C("六级过了带你去鼓浪屿", "Gulangyu once you pass CET-6", 2, "他记得鼓浪屿的约定", FlagRemembered,
                        "你记得！", "You remember!", "那你等着 这次我一定过", "Then wait for it. I'm passing this time"),
                    C("……我答应过很多事吗？", "…Did I promise that many things?", -4, "他不记得答应过我什么", FlagForgot,
                        "林晴雯现在不想说话", "Lin Qingwen does not wish to speak right now"),
                },
            },
            new GfArcBeat
            {
                key = "dad200", stage = 4,
                lines = new[] { "跟你说个秘密", "Want to hear a secret?", "我爸刚偷偷往我卡里打了两百块", "Dad just secretly put two hundred in my account", "短信说别告诉你妈 考完试买点好吃的", "His text said: don't tell your mum, buy something nice after exams", "可他自己那双鞋都穿开胶了 上次视频我看见的[委屈]", "And his own shoes are coming apart. I saw on our last video call [委屈]" },
                askedZh = "你爸林国强背着你妈偷偷给你卡里打了两百块，可他自己的鞋都穿开胶了，你心里不是滋味。", askedEn = "Your dad secretly sent you two hundred yuan behind your mum's back, though his own shoes are falling apart. It got to you.",
                choices = new[]
                {
                    C("拿这钱给他买双新鞋", "Use it to buy him new shoes", 3, "他让我拿爸爸给的钱给爸爸买鞋", null,
                        "……我怎么没想到", "…Why didn't I think of that", "他穿四十二码 回去就买", "He's a size 42. I'll buy them when I'm home"),
                    C("你爸人真好", "Your dad's really nice", 1, null, null,
                        "嗯 他就是不会说话", "Mm. He just doesn't say much", "我们家都是我妈说话", "At home it's Mum who does the talking"),
                    C("那请我喝奶茶", "Then buy me a milk tea", -1, null, null,
                        "你想得美", "In your dreams", "百香果双响炮 只给我自己买[哼]", "Passion fruit double, just for me [哼]"),
                },
            },
            new GfArcBeat
            {
                key = "future", stage = 5,
                lines = new[] { "我室友问我", "My roommate asked me", "你男朋友整天对着电脑 你们以后怎么办", "\"Your boyfriend's glued to a computer. What's your future?\"", "我没答上来", "I didn't know what to say", "你说呢", "What would you say?" },
                toldMlLines = new[] { "你那个教电脑认字的 教会了没", "And your computer that's learning to read. Has it learned?" },
                askedZh = "你问他：他整天对着电脑，你们以后怎么办。", askedEn = "You asked him what your future is, with him always at the computer.",
                choices = new[]
                {
                    C("等我做出点名堂，就去你那边", "Once this works out, I'm moving to your city", 3, "他说做出名堂就来我这边", FlagPromisedFuture,
                        "你说的哦[爱心]", "You said it [爱心]", "我截图了", "I took a screenshot"),
                    C("还没想好", "I haven't figured it out", -1, null, null,
                        "……嗯", "…Mm", "我也是", "Me neither"),
                    C("别听她们瞎说", "Don't listen to them", 0, null, null,
                        "我没听她们的", "I'm not listening to them", "我是在问你", "I'm asking you"),
                },
            },
            new GfArcBeat
            {
                key = "yutong", stage = 5,
                lines = new[] { "雨桐决定考研了", "Yutong's decided to sit the postgrad exam", "暑假都不回家 留校复习", "She's staying on campus all summer to study", "她问我要不要一起考", "She asked if I want to sit it with her", "我怕考不上……你觉得呢", "I'm scared I won't get in… What do you think?" },
                askedZh = "你室友陈雨桐决定考研，劝你一起考，你怕考不上，拿不准，问他的意见。", askedEn = "Your roommate Chen Yutong is sitting the postgrad exam and wants you to join; you're afraid you won't get in and asked what he thinks.",
                choices = new[]
                {
                    C("你想考就考，我支持你", "If you want to, go for it. I'm with you", 3, "我说想考研时他说支持我", null,
                        "你都不问我考哪", "You didn't even ask where", "……谢谢", "…Thanks"),
                    C("考研好，学历高找工作容易", "Do it. A master's makes jobs easier", 1, null, null,
                        "你跟我妈说的一模一样", "You sound exactly like my mum", "我再想想", "I'll think about it"),
                    C("考了就更没时间理我了吧", "Then you'll have even less time for me", -2, null, null,
                        "？？？", "???", "到底是谁天天对着电脑没时间理人", "Who's the one glued to a computer with no time for anyone?"),
                },
            },
            new GfArcBeat
            {
                key = "photo", stage = 6,
                lines = new[] { "收拾钱包翻出来一张照片", "Found a photo while sorting out my wallet", "高中毕业那天在一中教学楼前照的", "Graduation day, in front of the No.1 High teaching building", "你在我旁边比剪刀手 土死了", "You're next to me doing a peace sign. So tacky", "你那时候都毕业两年了 还专门跑回来", "You'd left school two years before and still came back for it" },
                askedZh = "你翻出高中毕业那天和他在一中教学楼前的合照：他早毕业两年，还专门跑回来，在你旁边比剪刀手。", askedEn = "You found the photo from your high-school graduation: he had left two years before, came back anyway, and stood next to you doing a peace sign.",
                choices = new[]
                {
                    C("那天我就是专门回去找你的", "I went back that day just to see you", 3, "他说毕业那天是专门回去找我的", null,
                        "骗人", "Liar", "……那你怎么拖到跨年才说", "…Then why did you wait until New Year's Eve to say anything?"),
                    C("拍给我看看", "Send me a picture", 1, null, null,
                        "不给 你太丑了", "No. You look awful", "等见面当面看", "You can see it in person"),
                    C("我现在也会比剪刀手", "I still do the peace sign", 0, null, null,
                        "……笨蛋", "…Dummy", "早点睡[月亮]", "Go to bed early [月亮]"),
                },
            },
        };

        public static GfArcBeat Beat(string key) => Array.Find(Beats, b => b.key == key);

        public static bool Done(GirlfriendState s, GfArcBeat b) => s != null && b != null && s.done != null && s.done.Contains(DonePrefix + b.key);

        /// <summary>The beat waiting for his answer, if any.</summary>
        public static GfArcBeat Open(GirlfriendState s) => s == null || string.IsNullOrEmpty(s.arcOpen) ? null : Beat(s.arcOpen);

        /// <summary>The next beat due now: the lab far enough, none open, the last one <see cref="Gap"/> seconds back.</summary>
        public static GfArcBeat Due(GirlfriendState s, int stage, int abilities, int epochs, double game)
        {
            if (s == null || !s.started || !string.IsNullOrEmpty(s.arcOpen)) return null;
            if (s.arcAt > 0 && game - s.arcAt < Gap) return null;
            foreach (var b in Beats)
            {
                if (Done(s, b)) continue;
                if (stage < b.stage || abilities < b.abilities || epochs < b.epochs) return null; // in order
                return b;
            }
            return null;
        }

        /// <summary>
        /// Her lines for a beat (zh, en pairs). The first one opens with her hello for the time of day; the later ones
        /// add the toldMl lines when he told her what he is doing.
        /// </summary>
        public static string[] Lines(GirlfriendState s, GfArcBeat b, int hour, int minute)
        {
            var lines = b.lines;
            if (b.key == "hello" && !s.done.Contains("life:firstNight")) lines = Concat(GirlfriendRules.FirstHello(hour, minute), lines);
            if (b.toldMlLines.Length > 0 && GirlfriendRules.Has(s, FlagToldMl)) lines = Concat(lines, b.toldMlLines);
            return lines;
        }

        static string[] Concat(string[] a, string[] b)
        {
            var r = new string[a.Length + b.Length];
            a.CopyTo(r, 0); b.CopyTo(r, a.Length);
            return r;
        }

        /// <summary>The beat has been sent: it waits for his answer. Her ordinary first hello is then not needed.</summary>
        public static void Opened(GirlfriendState s, GfArcBeat b, double game)
        {
            if (s == null || b == null) return;
            s.arcOpen = b.key; s.arcAt = game;
            if (b.key == "hello" && !s.done.Contains("life:firstNight")) s.done.Add("life:firstNight");
        }

        public static string[] Choices(GfArcBeat b, bool english)
        {
            var r = new string[b.choices.Length];
            for (int i = 0; i < r.Length; i++) r[i] = english ? b.choices[i].en : b.choices[i].zh;
            return r;
        }

        /// <summary>One of the beat's answers, by its exact text in either language. Null for anything else he typed.</summary>
        public static GfArcChoice Match(GfArcBeat b, string text)
        {
            if (b == null || text == null) return null;
            text = text.Trim();
            foreach (var c in b.choices) if (c.zh == text || c.en == text) return c;
            return null;
        }

        /// <summary>
        /// His answer to the open beat. A picked answer moves her affection, sets its flag and memory, and returns it (her
        /// reply is its lines). Anything else closes the beat too, with no change: her model answers it. Null when no
        /// beat is open.
        /// </summary>
        public static GfArcBeat Answer(GirlfriendState s, string text, out GfArcChoice picked)
        {
            picked = null;
            var b = Open(s);
            if (b == null) return null;
            picked = Match(b, text);
            if (picked != null)
            {
                GirlfriendRules.AddAffection(s, picked.affection);
                if (picked.flag.Length > 0) GirlfriendRules.Mark(s, picked.flag);
                if (picked.memoryZh.Length > 0) GirlfriendRules.Remember(s, "arc." + b.key, "他说：" + picked.memoryZh);
            }
            Close(s, b);
            return b;
        }

        /// <summary>He left the beat unanswered too long: it closes and costs a little.</summary>
        public static bool Expire(GirlfriendState s, double game)
        {
            var b = Open(s);
            if (b == null || game - s.arcAt < OpenFor) return false;
            GirlfriendRules.AddAffection(s, -UnansweredCost);
            Close(s, b);
            return true;
        }

        static void Close(GirlfriendState s, GfArcBeat b)
        {
            s.arcOpen = "";
            if (!s.done.Contains(DonePrefix + b.key)) s.done.Add(DonePrefix + b.key);
        }

        /// <summary>For her model when he typed his own answer to a beat.</summary>
        public static string FreeSituation(GfArcBeat b, bool english)
            => english
                ? b.askedEn + " He did not pick an answer; he wrote the line above in his own words. Take it in your own way, and if it dodged your question, ask once more."
                : b.askedZh + "他没有照你想听的说，而是自己说了上面那句。按你的性格接住他这句话；要是他躲开了你的问题，就再追问一次。";
    }
}
