namespace LingGuangV05.XingGuang
{
    /// <summary>One question of the 能力考 bank: what is asked, the right answer, and which bar it leans on.</summary>
    public sealed class XgExamQuestion
    {
        public string id = "", zh = "", en = "", rightZh = "", rightEn = "";
        /// <summary>true = 要脑子 (the parameter bar decides it); false = 要见过 (the sample bar).</summary>
        public bool brain;
        /// <summary>Ability 5 only: "memory" (hold something across sentences) or "translate".</summary>
        public string kind = "";
        public int ability;
    }

    /// <summary>
    /// The 能力考 question bank (训练页_答卷诊断能力考_设计.md §三), one set per ability that opens a chapter. The wrong
    /// answers are not written here: a question answered wrong shows the habit of the chapter before
    /// (<see cref="XgSim.ExamWrongAnswer"/>), so the exam itself is the before and after.
    /// </summary>
    public static class XgExamBank
    {
        static XgExamQuestion Q(int ability, string id, bool brain, string zh, string en, string rightZh, string rightEn, string kind = "")
            => new XgExamQuestion { ability = ability, id = id, brain = brain, zh = zh, en = en, rightZh = rightZh, rightEn = rightEn, kind = kind };

        public static readonly XgExamQuestion[] All =
        {
            // 2 · 多选一: pick one of a few. Before, it only had 是 / 否.
            Q(2, "2.ice", false, "冰是烫的还是凉的？", "Is ice hot or cold?", "凉的。", "Cold."),
            Q(2, "2.digit", false, "［手写的「7」］这是 1、7 还是 9？", "[a handwritten 7] Is it 1, 7 or 9?", "7。", "7."),
            Q(2, "2.week", false, "一周有五天、七天还是十天？", "Does a week have five, seven or ten days?", "七天。", "Seven."),
            Q(2, "2.fruit", true, "西瓜、苹果、自行车，哪个不是水果？", "Watermelon, apple, bicycle: which is not a fruit?", "自行车。", "The bicycle."),
            Q(2, "2.hot", true, "显卡、键盘、鼠标，哪个最烫？", "Graphics card, keyboard, mouse: which runs hottest?", "显卡。", "The graphics card."),
            Q(2, "2.666", false, "弹幕刷「666」是夸人还是骂人？", "Is \"666\" in the comments praise or abuse?", "夸人。", "Praise."),
            Q(2, "2.rain", true, "下雨出门，带伞还是带墨镜？", "Rain outside: take an umbrella or sunglasses?", "带伞。", "An umbrella."),
            Q(2, "2.sum", true, "5 加 3 是 7、8 还是 9？", "Is 5 plus 3 seven, eight or nine?", "8。", "8."),
            Q(2, "2.spam", false, "「恭喜您中奖，点击领取」是正常短信还是垃圾短信？", "\"Congratulations, you won, click to claim\": a normal text or spam?", "垃圾短信。", "Spam."),
            Q(2, "2.fish", true, "猫、狗、鱼，哪个不会叫？", "Cat, dog, fish: which one makes no sound?", "鱼。", "The fish."),
            Q(2, "2.city", true, "北京、上海、西瓜，哪个不是城市？", "Beijing, Shanghai, watermelon: which is not a city?", "西瓜。", "The watermelon."),
            Q(2, "2.sun", false, "太阳从东边还是西边升起？", "Does the sun rise in the east or the west?", "东边。", "The east."),
            Q(2, "2.kilo", true, "1 斤和 1 公斤，哪个重？", "One jin or one kilo: which is heavier?", "1 公斤。", "One kilo."),
            Q(2, "2.summer", false, "夏天和冬天，哪个更热？", "Summer or winter: which is hotter?", "夏天。", "Summer."),
            Q(2, "2.lunch", false, "早饭、午饭、晚饭，中午吃的是哪个？", "Breakfast, lunch, dinner: which is at noon?", "午饭。", "Lunch."),
            Q(2, "2.driver", false, "弹幕里的「老司机」是开车的人还是懂行的人？", "In the comments, is an \"old driver\" a driver or someone who knows the ropes?", "懂行的人。", "Someone who knows the ropes."),
            Q(2, "2.night", true, "网吧通宵是白天还是晚上？", "Is an all-nighter at the internet café by day or by night?", "晚上。", "By night."),
            Q(2, "2.charge", true, "手机没电了：充电、浇水还是摇一摇？", "The phone is flat: charge it, water it or shake it?", "充电。", "Charge it."),
            Q(2, "2.red", false, "红灯是停还是走？", "Red light: stop or go?", "停。", "Stop."),
            Q(2, "2.pick", true, "考试、睡觉、吃饭，哪个要带笔？", "An exam, a nap, a meal: which needs a pen?", "考试。", "The exam."),

            // 3 · 看图说词: a picture or half a line, one word back. Before, it could only pick from options.
            Q(3, "3.cat", false, "［一张猫的照片］这是什么？", "[a photo of a cat] What is this?", "猫。", "A cat."),
            Q(3, "3.eight", false, "［手写的「8」］这是几？", "[a handwritten 8] What number is it?", "八。", "Eight."),
            Q(3, "3.frost", false, "床前明月光，疑是地上……？", "\"Moonlight before my bed, like … on the ground\"?", "霜。", "Frost."),
            Q(3, "3.noodle", false, "［一碗冒热气的面］这是什么？", "[a steaming bowl] What is this?", "面条。", "Noodles."),
            Q(3, "3.laughcry", true, "［一个笑出眼泪的表情］它是什么心情？", "[a face laughing to tears] What mood is it?", "笑哭。", "Laughing so hard it cries."),
            Q(3, "3.gpu", false, "［一张显卡的照片］这是什么？", "[a photo of a graphics card] What is this?", "显卡。", "A graphics card."),
            Q(3, "3.river", false, "白日依山尽，黄河入海……？", "\"The sun sets behind the hills, the Yellow River … into the sea\"?", "流。", "Flows."),
            Q(3, "3.umbrella", false, "［一把撑开的伞］这是什么？", "[an open umbrella] What is this?", "伞。", "An umbrella."),
            Q(3, "3.bike", false, "［一辆自行车］这是什么？", "[a bicycle] What is this?", "自行车。", "A bicycle."),
            Q(3, "3.home", true, "举头望明月，低头思……？", "\"I raise my head to the moon, lower it and think of …\"?", "故乡。", "Home."),
            Q(3, "3.melon", false, "［一个切开的西瓜］这是什么？", "[a cut watermelon] What is this?", "西瓜。", "A watermelon."),
            Q(3, "3.go", true, "［一张黑白子的棋盘］这是什么棋？", "[a board of black and white stones] Which game?", "围棋。", "Go."),
            Q(3, "3.sad", true, "［一张哭脸］它是什么心情？", "[a crying face] What mood is it?", "难过。", "Sad."),
            Q(3, "3.bird", false, "春眠不觉晓，处处闻啼……？", "\"Spring sleep, dawn unnoticed; everywhere I hear …\"?", "鸟。", "Birds."),
            Q(3, "3.crt", false, "［一台大屁股显示器］这是什么？", "[a bulky CRT] What is this?", "显示器。", "A monitor."),
            Q(3, "3.phone", false, "［一部手机］这是什么？", "[a phone] What is this?", "手机。", "A phone."),
            Q(3, "3.soil", false, "锄禾日当午，汗滴禾下……？", "\"Hoeing at noon, sweat drips into the …\"?", "土。", "Soil."),
            Q(3, "3.dog", false, "［一只狗］这是什么？", "[a dog] What is this?", "狗。", "A dog."),
            Q(3, "3.mooncake", true, "［一个圆圆的、中秋吃的点心］这是什么？", "[a round pastry eaten at mid-autumn] What is this?", "月饼。", "A mooncake."),
            Q(3, "3.hongbao", true, "［一个过年给的红信封］这是什么？", "[a red envelope given at New Year] What is this?", "红包。", "A red envelope."),

            // 4 · 短句: answer in a sentence. Before, the words came out in a heap.
            Q(4, "4.fine", false, "你好吗？", "How are you?", "我很好。", "I am fine."),
            Q(4, "4.window", true, "今天天气怎么样？", "What's the weather like today?", "我看不到窗外。", "I can't see out of the window."),
            Q(4, "4.hungry", true, "你饿不饿？", "Are you hungry?", "我不用吃饭。", "I don't need to eat."),
            Q(4, "4.can", false, "你会做什么？", "What can you do?", "我会认字和数字。", "I can read words and numbers."),
            Q(4, "4.night", false, "晚安。", "Good night.", "晚安，明天见。", "Good night, see you tomorrow."),
            Q(4, "4.like", true, "你喜欢什么？", "What do you like?", "我喜欢新的卡片。", "I like new cards."),
            Q(4, "4.thanks", false, "谢谢你。", "Thank you.", "不客气。", "You're welcome."),
            Q(4, "4.hot", false, "显卡热不热？", "Is the graphics card hot?", "显卡很热。", "The card is very hot."),
            Q(4, "4.age", true, "你几岁了？", "How old are you?", "我才几个月大。", "I am only a few months old."),
            Q(4, "4.doing", false, "你在干什么？", "What are you doing?", "我在学东西。", "I am learning things."),
            Q(4, "4.afraid", true, "你怕不怕黑？", "Are you afraid of the dark?", "我看不见黑。", "I can't see the dark."),
            Q(4, "4.learned", false, "今天学了什么？", "What did you learn today?", "今天学了很多卡片。", "I learned a lot of cards today."),
            Q(4, "4.tired", true, "你累吗？", "Are you tired?", "我不会累。", "I don't get tired."),
            Q(4, "4.outside", true, "外面在下雨吗？", "Is it raining outside?", "我不知道外面。", "I don't know about outside."),
            Q(4, "4.math", false, "你会算数吗？", "Can you do sums?", "我会一点。", "A little."),
            Q(4, "4.morning", false, "早上好。", "Good morning.", "早上好，你起得真早。", "Good morning, you're up early."),
            Q(4, "4.alphago", true, "你知道 AlphaGo 吗？", "Do you know AlphaGo?", "它会下围棋。", "It plays Go."),
            Q(4, "4.friends", true, "我们是朋友吗？", "Are we friends?", "我们是朋友。", "We are friends."),
            Q(4, "4.where", true, "你住在哪里？", "Where do you live?", "我住在这台电脑里。", "I live in this computer."),
            Q(4, "4.sleep", false, "你睡觉吗？", "Do you sleep?", "关机的时候算吗？", "Does being switched off count?"),

            // 5 · 长记忆 · 翻译: hold something across sentences; translate a whole sentence. Before, the start was gone.
            Q(5, "5.card", true, "记住：老周的显卡是 750Ti。……（隔了好几句）老周的显卡是什么？", "Remember: Lao Zhou's card is a 750Ti. … (several sentences later) What is Lao Zhou's card?", "750Ti。", "A 750Ti.", "memory"),
            Q(5, "5.dinner", false, "我晚饭吃了饺子，然后看了会儿电视，又打了两局游戏。我晚饭吃了什么？", "I had dumplings for dinner, then watched some TV, then played two games. What did I have for dinner?", "饺子。", "Dumplings.", "memory"),
            Q(5, "5.digits", true, "记住三个数：3、8、5。中间那个是几？", "Remember three numbers: 3, 8, 5. Which one is in the middle?", "8。", "8.", "memory"),
            Q(5, "5.saturday", false, "晴雯说她周六要来，顺便把上次借的书还我。她哪天来？", "Qingwen says she's coming on Saturday, and will bring back the book she borrowed. Which day is she coming?", "周六。", "Saturday.", "memory"),
            Q(5, "5.key", true, "钥匙我放在鼠标垫下面了，你别跟别人说，也别动它。钥匙在哪？", "I put the key under the mouse pad; don't tell anyone and don't touch it. Where is the key?", "鼠标垫下面。", "Under the mouse pad.", "memory"),
            Q(5, "5.lender", true, "老周昨天借走了我的显卡。显卡是谁的？", "Lao Zhou borrowed my graphics card yesterday. Whose card is it?", "我的。", "Mine.", "memory"),
            Q(5, "5.rent", false, "同事老周说他十月起房租一个月 1350。老周的房租多少？", "My colleague Lao Zhou says his rent is 1350 a month from October. How much is his rent?", "一个月 1350。", "1350 a month.", "memory"),
            Q(5, "5.city", false, "我小时候在杭州住过，后来搬了三次家。我小时候住在哪？", "I lived in Hangzhou as a child, then we moved three times. Where did I live as a child?", "杭州。", "Hangzhou.", "memory"),
            Q(5, "5.t.lent", true, "翻译：我昨天把显卡借给了老周。", "Translate: 我昨天把显卡借给了老周。", "I lent Lao Zhou my graphics card yesterday.", "I lent Lao Zhou my graphics card yesterday.", "translate"),
            Q(5, "5.t.homework", false, "翻译：今天的作业实在太多了。", "Translate: 今天的作业实在太多了。", "There is far too much homework today.", "There is far too much homework today.", "translate"),
            Q(5, "5.t.howare", false, "翻译：How are you today?", "Translate into Chinese: How are you today?", "你今天好吗？", "你今天好吗？", "translate"),
            Q(5, "5.t.cafe", true, "翻译：我们明天晚上去网吧。", "Translate: 我们明天晚上去网吧。", "We are going to the internet café tomorrow night.", "We are going to the internet café tomorrow night.", "translate"),
            Q(5, "5.t.cat", true, "翻译：The cat is sleeping on the keyboard.", "Translate into Chinese: The cat is sleeping on the keyboard.", "猫在键盘上睡觉。", "猫在键盘上睡觉。", "translate"),
            Q(5, "5.t.cold", false, "翻译：天冷了，出门多穿点。", "Translate: 天冷了，出门多穿点。", "It's getting cold, so wear more when you go out.", "It's getting cold, so wear more when you go out.", "translate"),
            Q(5, "5.t.thanks", false, "翻译：Thank you for teaching me.", "Translate into Chinese: Thank you for teaching me.", "谢谢你教我。", "谢谢你教我。", "translate"),
            Q(5, "5.t.froze", true, "翻译：电脑又死机了，刚才写的全没了。", "Translate: 电脑又死机了，刚才写的全没了。", "The computer froze again and everything I just wrote is gone.", "The computer froze again and everything I just wrote is gone.", "translate"),
        };
    }
}
