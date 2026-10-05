using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Problem kinds (XgVerdict ids) the player has cured by changing settings (对症下药 cards).</summary>
        public List<string> curedKinds = new List<string>();
        /// <summary>Cards the player has looked at (in the album or when they flipped in); the rest are "NEW".</summary>
        public List<string> cardsViewed = new List<string>();
    }

    public sealed partial class XgRun
    {
        /// <summary>The last problem the verdict named and the settings it was named under (not saved).</summary>
        [NonSerialized] internal string lastProblem = "", lastProblemSettings = "";
    }

    /// <summary>What the big card shows on its face (XgHoloCard).</summary>
    public sealed class XgCardFace
    {
        public string id = "", title = "", titleEn = "", kicker = "", kickerEn = "", glyph = "", glyph2 = "", headline = "", headlineEn = "", body = "", bodyEn = "", stamp = "", stampEn = "", serial = "";
        public int rarity, stage;
        public string category = "";
    }

    /// <summary>
    /// The 成就 album: every achievement is a holographic card to collect. Categories follow how the game is played:
    /// the walls passed, the walls worked out alone, the slow roads, the cures read off the 训练图式, the phenomena of
    /// the 图鉴, the data, a few funny moments, and the endings. Rarity sets the foil (普通 silver, 稀有 gold, 史诗
    /// pearl, 传说 laser; 「灵光一现」 the prism).
    /// </summary>
    public sealed partial class XgSim
    {
        public const string CardWall = "wall", CardInsight = "insight", CardRoad = "road", CardCure = "cure", CardPhenomenon = "phenomenon",
            CardData = "data", CardFun = "fun", CardLife = "life", CardEnding = "ending";
        public static readonly string[] CardCategories = { CardWall, CardInsight, CardRoad, CardCure, CardPhenomenon, CardData, CardFun, CardLife, CardEnding };
        /// <summary>Combo needed for the 手速 card, and the cures for 妙手回春.</summary>
        public const int ComboCard = 50, CuresForCard = 5, LogsForCard = 1000, CasinoLossCard = 5000, CasinoStreakCard = 5, SantiErasCard = 3;

        public static string CategoryName(string category, bool english)
        {
            switch (category)
            {
                case CardWall: return english ? "Breakthroughs" : "突破";
                case CardInsight: return english ? "Insights" : "自悟";
                case CardRoad: return english ? "The slow road" : "笨办法";
                case CardCure: return english ? "Diagnosis" : "对症下药";
                case CardPhenomenon: return english ? "Phenomena" : "现象图鉴";
                case CardData: return english ? "Data" : "数据";
                case CardFun: return english ? "Moments" : "趣事";
                case CardLife: return english ? "Off the clock" : "下班以后";
                default: return english ? "Endings" : "结局";
            }
        }

        public static string RarityName(int rarity, bool english)
        {
            switch (rarity)
            {
                case 0: return english ? "Common" : "普通";
                case 1: return english ? "Rare" : "稀有";
                case 2: return english ? "Epic" : "史诗";
                case 3: return english ? "Legendary" : "传说";
                default: return english ? "Hidden legend" : "隐藏传说";
            }
        }

        static XgAchievement Card(string category, int rarity, string id, string glyph, string zh, string en, string note, string noteEn, string flavor = "", string flavorEn = "", bool hidden = false, string glyph2 = "")
            => new XgAchievement { id = id, category = category, rarity = rarity, glyph = glyph, glyph2 = glyph2, name = zh, nameEn = en, note = note, noteEn = noteEn, flavor = flavor, flavorEn = flavorEn, hidden = hidden };

        static XgAchievement Life(int rarity, string id, string glyph, string zh, string en, string note, string noteEn, string hint, string hintEn, string flavor, string flavorEn, string glyph2 = "")
        {
            var a = Card(CardLife, rarity, id, glyph, zh, en, note, noteEn, flavor, flavorEn, true, glyph2);
            a.hint = hint; a.hintEn = hintEn;
            return a;
        }

        static XgAchievement[] BuildAlbum()
        {
            var list = new List<XgAchievement>
            {
                // 突破: every wall passed, however.
                Card(CardWall, 1, "wall.combo", "异", "第一道墙", "The first wall", "过了阶段 1 的墙「异或」。", "Passed stage 1's wall, XOR.", "1969 年 Minsky 和 Papert 证明一层学不会异或，神经网络冷了十几年。", "In 1969 Minsky and Papert showed one layer cannot learn XOR; neural nets went cold for over a decade."),
                Card(CardWall, 1, "wall.structure", "图", "看懂整张图", "The whole picture", "过了阶段 2 的墙「看不懂整张图」。", "Passed stage 2's wall.", "图看邻居，句子看前文：不是每个格子都要连每个格子。", "Pictures look at neighbours, sentences at what came before."),
                Card(CardWall, 1, "wall.length", "忆", "学会记住", "Learning to remember", "过了阶段 3 的墙「长句失忆」。", "Passed stage 3's wall.", "让它自己决定记住什么、忘掉什么。", "Let it decide what to keep and what to forget."),
                Card(CardWall, 1, "wall.degrade", "深", "越深越好", "Deeper is better", "过了附加墙「越深越差」。", "Passed the extra wall, deeper is worse.", "给每一层留一条捷径，信号就能原样穿过去。", "Give every layer a shortcut and the signal passes untouched."),
                Card(CardWall, 1, "wall.translation", "译", "先读完再说", "Read it all first", "过了阶段 4 的墙「翻译不了」。", "Passed stage 4's wall.", "先把整句读完，再从头说一遍。", "Read the whole sentence, then say it again from the start."),
                Card(CardWall, 1, "wall.parallel", "注", "只要注意力", "Attention only", "过了阶段 5 的墙「串行瓶颈」。", "Passed stage 5's wall.", "拿掉循环，每个字直接看所有字。", "Drop the loop: every word looks at every word."),
                Card(CardWall, 1, "wall.pretrain", "模", "预训练", "Pre-training", "阶段 6 把预训练跑通。", "Ran pre-training through in stage 6.", "规模和稳定，两样都要。", "Scale and stability: you need both."),

                // 自悟: the same walls without the secret (ids kept from the first achievements, for old saves).
                Card(CardInsight, 2, "insight.combo", "异", "自己想通了异或", "Worked out XOR", "第 1 阶段没买秘籍就过了墙。", "Passed stage 1 without the secret."),
                Card(CardInsight, 2, "insight.structure", "邻", "看邻居，读前文", "Neighbours and context", "第 2 阶段没买秘籍就过了墙。", "Passed stage 2 without the secret."),
                Card(CardInsight, 2, "insight.length", "忘", "学会忘记", "Learning to forget", "第 3 阶段没买秘籍就过了墙。", "Passed stage 3 without the secret."),
                Card(CardInsight, 2, "insight.translation", "译", "先读完再说", "Read it all first", "第 4 阶段没买秘籍就过了墙。", "Passed stage 4 without the secret."),
                Card(CardInsight, 2, "insight.parallel", "注", "也想到了", "Thought of it too", "第 5 阶段在它开口之前，自己只用了注意力。", "In stage 5 you went attention-only before it said so."),
                Card(CardInsight, 2, "insight.pretrain", "模", "规模和稳定", "Scale and stability", "第 6 阶段没买秘籍就让预训练跑通。", "Got pre-training through in stage 6 without the secret."),
                Card(CardInsight, 4, "lingguang", "灵", "灵光一现", "A Flash of Insight", "六个阶段全部自悟。", "Worked out all six stages yourself.", "没有秘籍，没有提示，只有一次又一次地试。这张卡只发给你。", "No secrets, no hints, only trying again and again. This card is yours alone.", hidden: true, glyph2: "光"),

                // 笨办法: the training-method roads.
                Card(CardRoad, 1, "road.features", "笨", "笨办法也行", "The slow way works", "不换结构，靠「特征工程」过了一面墙。", "Passed a wall with feature engineering instead of a new structure.", "深度学习之前，人们就是这样替机器想特征的。", "Before deep learning, this is how people thought up features for machines."),
                Card(CardRoad, 1, "road.batchnorm", "扛", "硬扛", "Toughed it out", "没开跨层直连，靠 BatchNorm 过了「越深越差」。", "Passed deeper-is-worse without skip connections, on BatchNorm.", "2015 年，BatchNorm 先让深网络能练；残差才真正解决。", "In 2015 BatchNorm made deep nets trainable first; residuals truly solved it."),
                Card(CardRoad, 2, "road.both", "双", "两条路都走过", "Both roads", "既换过结构过墙，也换过练法过墙。", "Passed walls both by a new structure and by a training method."),

                // 对症下药: cures read off the 训练图式.
                Card(CardCure, 1, "cure.first", "药", "对症下药", "The right medicine", "照「训练图式」的结论改了设置，问题解决了。", "Changed the settings by the training map's verdict and the problem went away.", "先看清卡在哪，再动手。", "See where it is stuck first, then act."),
                Card(CardCure, 2, "cure.five", "医", "妙手回春", "Healing hands", "治好 " + CuresForCard + " 种不同的问题。", "Cured " + CuresForCard + " different kinds of problem."),

                // 数据.
                Card(CardData, 1, "data.junkoff", "伪", "去伪存真", "Out with the junk", "把一个淘货杂包关掉，不让它参与训练。", "Switched a junk pack off for training.", "数据多不一定好，标错的会把它往反方向拉。", "More data is not always better: wrong labels pull the other way."),
                Card(CardData, 1, "data.flywheel", "轮", "数据飞轮", "The data flywheel", "订单回传的用户日志攒到 " + LogsForCard + " 条。", "Collected " + LogsForCard + " user logs from contracts.", "用户越多，数据越多，模型越好，用户越多。", "More users, more data, a better model, more users."),

                // 趣事.
                Card(CardFun, 0, "fun.nan", "炸", "炸炉", "Kaboom", "学习率太大，训练第一次炸成 NaN。", "The rate was too high and training blew up into NaN.", "炼丹嘛，炸几次炉很正常。", "Alchemy: a few explosions are normal."),
                Card(CardFun, 1, "fun.combo", "连", "手速", "Fast hands", "连击达到 " + ComboCard + "。", "Reached a combo of " + ComboCard + "."),
                Card(CardFun, 0, "fun.firstcard", "卡", "收藏家", "Collector", "打开成就页，看第一张卡。", "Opened the album to look at a card."),

                // 下班以后: hidden cards for the life outside the lab (the casino, 晴雯, 三体, the desktop). The album
                // shows only a rumour until one is found; the desktop apps call EarnSecret, the lab's own chat earns here.
                Life(0, "life.casino.first", "赌", "小赌怡情", "A little flutter", "在 888.vip 下了第一注。", "Placed a first bet on 888.vip.", "听说有个网站，一夜就能把显卡钱赚回来。", "Rumour has it a website pays back a graphics card overnight.",
                    "庄家从来不急。", "The house is never in a hurry."),
                Life(1, "life.casino.blackjack", "A", "黑杰克", "Blackjack", "二十一点开局就拿到黑杰克。", "Dealt a natural blackjack.", "有人在二十一点的桌上等一张 A。", "Someone at the blackjack table is waiting for an ace.",
                    "一张 A，一张 K。概率 4.8%，这次是你。", "An ace and a king: 4.8%, and this time it was you."),
                Life(2, "life.casino.jackpot", "7", "三个七", "Triple seven", "老虎机转出三个一样的。", "Hit three of a kind on the slots.", "老虎机据说真的会吐钱。", "They say the slot machine really does pay out.",
                    "二十倍。别告诉晴雯。", "Twenty times the stake. Don't tell Qingwen."),
                Life(1, "life.casino.broke", "光", "一把梭", "All in", "在赌场输到连最小的筹码都押不起。", "Lost until not even the smallest chip was affordable.", "据说有人在 888.vip 输掉了买显卡的钱。", "Word is someone lost the GPU money on 888.vip.",
                    "显卡没买成，模型还在等。", "No new graphics card. The model is still waiting."),
                Life(2, "life.casino.loss", "庄", "久赌必输", "The house always wins", "在赌场累计净输 ¥" + CasinoLossCard + "。", "Lost ¥" + CasinoLossCard + " net at the casino.", "赌场的账本上，有一页写着你的名字。", "There is a page with your name in the casino's ledger.",
                    "每一种玩法的期望都是负的：模型一算就知道，你偏要试。", "Every game has a negative expectation: the model could have told you, you had to try."),
                Life(4, "life.casino.streak", "赢", "赌神", "God of Gamblers", "在赌场连赢 " + CasinoStreakCard + " 把。", "Won " + CasinoStreakCard + " casino rounds in a row.", "传说有人在 888.vip 一直赢。转一下这张卡。", "Legend has it someone never lost at 888.vip. Turn this card.",
                    "转过来看看：赢的背面，写的是输。", "Turn it over: on the back of winning is losing.", "输"),

                Life(0, "life.gf.chat", "晴", "下班回消息", "Texting back", "在 YY 上给晴雯发了第一句话。", "Sent Qingwen a first message on YY.", "工作再忙，也有人在等你回消息。", "However busy work is, someone is waiting for your reply.",
                    "调参可以等，她不行。", "The hyperparameters can wait. She can't."),
                Life(1, "life.gf.packet", "红", "520", "520", "给晴雯发了一个吉利数字的红包。", "Sent Qingwen a red packet with a lucky number.", "有些数字，比金额更重要。", "Some numbers matter more than the amount.",
                    "5.20、13.14、520、1314：她都懂。", "5.20, 13.14, 520, 1314: she gets all of them."),
                Life(0, "life.gf.fight", "冷", "冷战", "The cold war", "和晴雯吵了第一架。", "Had a first fight with Qingwen.", "聊天窗口有时候比 NaN 还吓人。", "A chat window can be scarier than NaN.",
                    "为了什么吵的，已经想不起来了。", "Nobody remembers what it was about."),
                Life(1, "life.gf.makeup", "和", "床头吵架床尾和", "Making up", "吵架以后，和晴雯和好了。", "Made up with Qingwen after a fight.", "吵完以后，还有一件事要做。", "After a fight, there is one more thing to do.",
                    "这比调参难。", "Harder than tuning a model."),
                Life(1, "life.gf.forgot", "忘", "钢铁直男", "Forgot again", "忘了一个对她很重要的日子。", "Forgot a day that mattered to her.", "日历上有些日子，没有闹钟会提醒你。", "Some days on the calendar have no alarm.",
                    "模型记得一万个字，你忘了一个日子。", "The model remembers ten thousand words; you forgot one date."),
                Life(1, "life.gf.iphone", "果", "肾六换七", "The iPhone 7", "送了晴雯一部 iPhone 7。", "Gave Qingwen an iPhone 7.", "九月有个新手机，很多人在等。", "A new phone came out in September and many were waiting.",
                    "¥5388，两张显卡的钱。", "¥5,388: the price of two graphics cards."),
                Life(2, "life.gf.sweet", "甜", "热恋", "Head over heels", "晴雯的好感升到「甜」。", "Qingwen's affection reached Sweet.", "有人说，恋爱比训练模型容易。", "Some say love is easier than training a model.",
                    "没有损失函数，没有验证集，可它就是在变好。", "No loss function, no validation set, and still it got better."),
                Life(2, "life.gf.caught", "替", "替身被识破", "Caught out", "让它替你回消息，被晴雯发现了。", "Let it answer Qingwen for you, and she noticed.", "据说有人让 AI 替自己谈恋爱。", "They say someone let an AI do their dating.",
                    "它学你说话，没学像。", "It learned to talk like you. Not well enough."),
                Life(3, "life.gf.newyear", "跨", "跨年", "New Year's Eve", "收到晴雯 23:59 的那条消息。", "Got Qingwen's message at 23:59.", "2016 年的最后一分钟，会有一条消息。", "There is a message in the last minute of 2016.",
                    "零点之前，她先想到了你。", "Before midnight, she thought of you first."),
                Life(4, "life.love.ask", "爱", "她爱我吗", "Does she love me?", "让它读完你们所有的聊天记录，回答那个问题。", "Let it read every message you two sent and answer the question.", "有个问题，你一直想问它。转一下这张卡。", "There is a question you have always wanted to ask it. Turn this card.",
                    "它读了每一句话，给了你一个数字。转过来看，那只是一个数字。", "It read every line and gave you a number. Turn it over: it is only a number.", "数"),
                Life(2, "life.love.never", "算", "有些事不必知道", "Some things stay unread", "它要读你们的聊天记录时，你说了「算了」。", "When it offered to read your messages, you said never mind.", "有个问题，问出口以后可以收回。", "There is a question you can take back after asking.",
                    "「好。」", "\"All right.\""),

                Life(0, "life.santi.login", "V", "V 装备", "The V-suit", "在摆渡上找到了「三体」的游戏登录页。", "Found the Three Body game login on Bodu.", "据说有个网游，要穿 V 装备才能进。", "There is said to be an online game you need a V-suit for.",
                    "需要 V 装备与邀请码。", "Requires a V-suit and an invitation code."),
                Life(1, "life.santi.code", "邀", "有缘人", "Those it was meant for", "坚持输入邀请码，被放了进去。", "Kept entering the invitation code until it let you in.", "邀请码无效？再试试。", "Invalid code? Try again.",
                    "本游戏只对有缘人开放。", "This game is only open to those it was meant for."),
                Life(2, "life.santi.play", "三", "玩三体", "Playing Three Body", "在三体游戏里度过了一个纪元。", "Lived through an era in the Three Body game.", "三颗太阳的世界，有人在玩。", "Someone is playing in a world with three suns.",
                    "恒纪元浸泡，乱纪元脱水。猜错一次，文明重来。", "Soak in a stable era, dehydrate in a chaotic one. Guess wrong once and the civilisation starts over."),
                Life(4, "life.santi.civ", "恒", "文明的种子", "Seed of civilisation", "在三体游戏里带一个文明连过 " + SantiErasCard + " 个纪元。", "Brought a civilisation through " + SantiErasCard + " eras in the Three Body game.", "有人说那个游戏能玩通关。转一下这张卡。", "Some say that game can be beaten. Turn this card.",
                    "转过来：恒纪元的背面，是乱纪元。", "Turn it over: behind every stable era is a chaotic one.", "乱"),
                Life(3, "life.santi.silence", "默", "不要回答", "Do not answer", "对它连说三次「不要回答」。", "Told it \"do not answer\" three times.", "有一句话，说三遍，它就不说话了。", "Say one sentence three times and it falls silent.",
                    "不要回答！不要回答！！不要回答！！！", "Do not answer! Do not answer!! Do not answer!!!"),
                Life(1, "life.santi.bug", "虫", "虫子", "Bugs", "跟它提起虫子。", "Mentioned bugs to it.", "它对某种小东西有话要说。", "It has something to say about a certain small creature.",
                    "虫子从来没有被真正战胜过。", "The bugs have never truly been defeated."),
                Life(2, "life.santi.wallfacer", "壁", "面壁者", "Wallfacer", "让它叫你「面壁者」。", "Had it call you Wallfacer.", "名字可以随便起吗？可以。", "Can you call yourself anything? You can.",
                    "你的计划，只有你自己知道。", "Only you know your plan."),
                Life(2, "life.santi.sophon", "智", "智子锁死", "Locked by the sophon", "想把它拖进回收站，文件却在 sophon.dll 中打开。", "Tried to drag it to the bin; the file was open in sophon.dll.", "有个程序，删不掉。", "There is a program that cannot be deleted.",
                    "操作无法完成，因为文件已在 sophon.dll 中打开。", "The action can't be completed because the file is open in sophon.dll."),

                Life(1, "life.game.won", "棋", "人类的骄傲", "Pride of humanity", "下棋赢了自己训练的模型。", "Beat your own model at a board game.", "它学会下棋以后，你还赢得了吗？", "Once it learns the game, can you still win?",
                    "趁现在。", "While you still can."),
                Life(2, "life.game.lost", "负", "青出于蓝", "The student wins", "下棋输给了自己训练的模型。", "Lost a board game to your own model.", "迟早有一天，它会赢你。", "Sooner or later it will beat you.",
                    "它学的每一步，都是你教的。", "Every move it knows, you taught it."),
                Life(1, "life.hw.burn", "烧", "烧了", "Burnt", "新显卡烧了 PCIe，跳闸了。", "A new graphics card burnt the PCIe slot and tripped the breaker.", "有一款显卡，据说功耗写少了。", "One graphics card, they say, understates its power draw.",
                    "闻到焦味的时候，已经晚了。", "By the time you smell it, it's too late."),
                Life(2, "life.hw.titan", "信", "信仰", "Faith", "买了一张 TITAN X。", "Bought a TITAN X.", "显卡里也有信仰。", "Even graphics cards have a faith.",
                    "¥9999，信仰无价。", "¥9,999. Faith is priceless."),
                Life(0, "life.redpacket.slow", "慢", "手慢了", "Too slow", "群里的红包没抢到。", "Missed a red packet in a group chat.", "群里的红包，有人永远抢不到。", "Some people never get the group red packets.",
                    "手慢了，红包派完了。", "Too slow: the red packet is gone."),

                // 结局 (hidden until reached).
                Card(CardEnding, 3, "end.E1", "停", "听见关机就停下", "It stops when told", "写下了那条规则。", "Wrote the rule.", hidden: true),
                Card(CardEnding, 3, "end.E2", "自", "没有那条规则", "No such rule", "没写那条规则。", "Did not write the rule.", hidden: true),
                Card(CardEnding, 3, "end.E3", "托", "托付", "Entrusted", "让它自己选。", "Let it choose.", hidden: true),
            };
            // 现象图鉴: one common card per phenomenon of the board.
            foreach (var p in XgPhenomena.All)
                list.Add(Card(CardPhenomenon, 0, "ph." + p.id, p.name.Substring(0, 1), p.name, p.nameEn, "第一次观察到「" + p.name + "」。", "Saw " + p.nameEn + " for the first time.", p.why, p.whyEn));
            return list.ToArray();
        }

        /// <summary>Cards in one category, in album order.</summary>
        public static List<XgAchievement> CardsIn(string category)
        {
            var list = new List<XgAchievement>();
            foreach (var a in Achievements) if (a.category == category) list.Add(a);
            return list;
        }

        public int CardsOwned(string category = null)
        {
            int n = 0;
            foreach (var a in Achievements) if ((category == null || a.category == category) && HasAchievement(a.id)) n++;
            return n;
        }

        /// <summary>Opening the album and looking at a card earns 收藏家 (the album's own first card).</summary>
        public void LookedAtCard(string id) { MarkCardViewed(id); Earn("fun.firstcard"); }

        /// <summary>The card was on screen (flipped in after earning it, or opened in the album).</summary>
        public void MarkCardViewed(string id)
        {
            if (S.cardsViewed == null) S.cardsViewed = new List<string>();
            if (Achievement(id) != null && !S.cardsViewed.Contains(id)) S.cardsViewed.Add(id);
        }

        public bool CardIsNew(string id) => HasAchievement(id) && (S.cardsViewed == null || !S.cardsViewed.Contains(id));

        public int NewCards()
        {
            int n = 0;
            foreach (var id in S.achievements) if (Achievement(id) != null && CardIsNew(id)) n++;
            return n;
        }

        // ───────────── earning ─────────────

        /// <summary>Cards that follow from the state (checked every tick; Earn ignores cards already owned).</summary>
        void TickCards()
        {
            foreach (var p in XgPhenomena.All) if (PhenomenonSeen(p.id)) Earn("ph." + p.id);
            foreach (var w in CardWalls) if (w != "pretrain" && WallPassedById(w)) Earn("wall." + w);
            if (S.pretrain >= 1 - 1e-9) Earn("wall.pretrain");
            string features = null, structure = null;
            if (S.wallRoutes != null)
                foreach (var r in S.wallRoutes)
                {
                    if (r.EndsWith("=" + RouteFeatures, StringComparison.Ordinal)) features = r; else structure = r;
                }
            if (features != null) Earn("road.features");
            if (features != null && structure != null) Earn("road.both");
            if (S.logsTotal >= LogsForCard) Earn("data.flywheel");
            if (S.bestCombo >= ComboCard) Earn("fun.combo");
            if (S.nanEvents > 0) Earn("fun.nan");
            if (S.curedKinds != null && S.curedKinds.Count > 0) Earn("cure.first");
            if (S.curedKinds != null && S.curedKinds.Count >= CuresForCard) Earn("cure.five");
            if (S.loveVerdicts > 0) Earn("life.love.ask");
            if (S.loveConsent == -1) Earn("life.love.never");
            if (Profile != null && (Profile.callMe == "面壁者" || string.Equals(Profile.callMe, "Wallfacer", StringComparison.OrdinalIgnoreCase))) Earn("life.santi.wallfacer");
            if (!string.IsNullOrEmpty(S.ending)) Earn(S.ending.StartsWith("E3", StringComparison.Ordinal) ? "end.E3" : "end." + S.ending);
        }

        /// <summary>
        /// A hidden card of the life outside the lab, earned by the desktop apps (888.vip, YY, Bodu, the hardware shop).
        /// Only 下班以后 cards can be earned this way. Returns true when the card is new.
        /// </summary>
        public bool EarnSecret(string id)
        {
            var a = Achievement(id);
            if (a == null || a.category != CardLife || HasAchievement(id)) return false;
            Earn(id);
            return true;
        }

        /// <summary>A casino round settled (XgSim.Casino.cs, XgBlackjackRules.cs): first bet, jackpots, streaks, ruin.</summary>
        void CasinoSettled(XgCasinoRound round, bool natural, IXgHost host)
        {
            Earn("life.casino.first");
            if (round.game == "slots" && round.returned >= round.stake * 20) Earn("life.casino.jackpot");
            if (natural && round.returned > round.stake * 2) Earn("life.casino.blackjack");
            int streak = 0;
            for (int i = Casino.history.Count - 1; i >= 0 && Casino.history[i].returned > Casino.history[i].stake; i--) streak++;
            if (streak >= CasinoStreakCard) Earn("life.casino.streak");
            if (Casino.wagered - Casino.returned >= CasinoLossCard) Earn("life.casino.loss");
            if (host != null && host.Money < 10) Earn("life.casino.broke");
        }

        bool WallPassedById(string id) { var w = WallById(id); return w != null && WallPassed(w); }

        /// <summary>
        /// 对症下药: the verdict named a problem; after a settings change the same run is no longer stuck. Called after
        /// every board epoch (XgSim.Trace.cs).
        /// </summary>
        void WatchCure(XgRun run)
        {
            var v = Verdict(run);
            string settings = TraceSettings(run);
            if (v.problem) { run.lastProblem = v.id; run.lastProblemSettings = settings; return; }
            // Fields are not saved: a freshly loaded run starts with nothing to cure.
            if (string.IsNullOrEmpty(run.lastProblem) || settings == run.lastProblemSettings) return;
            if (v.id == "improving" || v.id == "passed")
            {
                if (S.curedKinds == null) S.curedKinds = new List<string>();
                if (!S.curedKinds.Contains(run.lastProblem)) S.curedKinds.Add(run.lastProblem);
                run.lastProblem = "";
            }
        }

        // ───────────── the face of a card ─────────────

        /// <summary>
        /// The face of any card: album cards by id, and the self-insight cards the walls hand out (wall ids, 近亲繁殖,
        /// the auto-labelling idea). Serial is the card's place in its category.
        /// </summary>
        public static XgCardFace CardFace(string id)
        {
            var face = new XgCardFace { id = id };
            var a = Achievement(id);
            if (a == null)
            {
                // A self-insight card by wall id (the flip-in after a wall is worked out alone).
                InsightCardText(id, out int stage, out string name, out string nameEn, out string golden, out string goldenEn, out string why, out string whyEn);
                face.stage = stage; face.category = CardInsight;
                face.rarity = id == InbreedingId || id == EpiphanyCardId || id == "degrade" ? 1 : 2;
                face.title = name; face.titleEn = nameEn;
                face.kicker = id == InbreedingId ? "现象" : "自悟 · 第 " + stage + " 阶段"; face.kickerEn = id == InbreedingId ? "Phenomenon" : "Insight · stage " + stage;
                face.glyph = id == "degrade" ? "捷" : id == EpiphanyCardId ? "替" : id == InbreedingId ? "近" : stage >= 1 && stage <= 6 ? new[] { "", "异", "邻", "忘", "译", "注", "模" }[stage] : "灵";
                face.headline = golden; face.headlineEn = goldenEn; face.body = why; face.bodyEn = whyEn;
                face.stamp = id == InbreedingId ? "已发现" : "已自悟"; face.stampEn = id == InbreedingId ? "FOUND" : "SOLVED";
                face.serial = id == "degrade" || id == EpiphanyCardId || id == InbreedingId ? "附卡" : "No." + stage + "/6";
                return face;
            }
            var mates = CardsIn(a.category);
            face.category = a.category; face.rarity = a.rarity; face.glyph = a.glyph; face.glyph2 = a.glyph2;
            face.title = a.name; face.titleEn = a.nameEn;
            face.kicker = CategoryName(a.category, false) + " · " + RarityName(a.rarity, false); face.kickerEn = CategoryName(a.category, true) + " · " + RarityName(a.rarity, true);
            face.headline = a.note; face.headlineEn = a.noteEn;
            face.body = a.flavor; face.bodyEn = a.flavorEn;
            face.stamp = a.rarity >= 3 ? "传说" : a.category == CardPhenomenon ? "已发现" : "已收集"; face.stampEn = a.rarity >= 3 ? "LEGEND" : a.category == CardPhenomenon ? "FOUND" : "GOT IT";
            face.serial = "No." + (mates.IndexOf(a) + 1) + "/" + mates.Count;
            return face;
        }
    }
}
