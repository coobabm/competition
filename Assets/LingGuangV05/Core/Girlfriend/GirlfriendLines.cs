using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>
    /// Her offline lines (design §4.4 失败兜底): when the local model is not running or fails, she still answers in
    /// her tier's voice. His line picks the kind of answer first (a greeting, 想你, 晚安, a question, a gift…, by
    /// keyword); otherwise she says something that fits her tier and the time of day, the summer at home, the
    /// dated things going on in her life, or a thing he told her. Each tier × period has twelve general lines and
    /// every keyword three moods of four; a line is never like one of her last <see cref="RecentLimit"/>.
    /// </summary>
    public static class GirlfriendLines
    {
        public enum Period { Night, Day, Evening }

        /// <summary>How many of her recent lines (any source) the library stays clear of.</summary>
        public const int RecentLimit = 24;

        public static Period PeriodOf(DateTime clock)
        {
            int h = clock.Hour;
            if (h >= 22 || h < 6) return Period.Night;
            return h < 17 ? Period.Day : Period.Evening;
        }

        // Each entry: zh, en. Bubbles inside one line are separated by "|".
        static readonly string[][] Tier =
        {
            /* Cold */    new[] { "嗯", "Mm", "哦", "Oh", "随便", "Whatever", "知道了", "Got it", "噢", "Right", "行吧", "Fine" },
            /* Distant */ new[] { "嗯 知道了", "Mm, got it", "还行吧", "It's OK I guess", "哦 那你忙", "Oh, you go and be busy then", "没什么事", "Nothing much", "你开心就好", "Whatever makes you happy", "是吗", "Is that so" },
            /* Normal */  new[] { "哈哈哈 真的假的", "Hahaha really?", "嗯嗯 然后呢", "Mm mm, and then?", "好吧|你开心就好", "Fine|as long as you're happy", "我刚也在想这个", "I was just thinking that", "你说的哦|我记住了", "You said it|I'm holding you to it", "也是醉了[流汗]", "Unbelievable [流汗]" },
            /* Warm */    new[] { "笨蛋|你怎么才回我", "Dummy|why did you take so long", "嘿嘿 我就知道[偷笑]", "Hehe, I knew it [偷笑]", "你吃饭了没|别又泡面", "Have you eaten?|not instant noodles again", "想你了[害羞]", "Miss you [害羞]", "你今天有没有想我", "Did you think of me today?", "哼|就知道你会这么说", "Hmph|knew you'd say that" },
            /* Sweet */   new[] { "你终于回我了！！|我等了好久[委屈]", "Finally!!|I waited so long [委屈]", "我跟你说|今天发生了好多事|等你有空慢慢讲", "Listen|so much happened today|I'll tell you when you're free", "你说的我都记着呢[爱心]", "I remember everything you say [爱心]", "好想快点放假见你", "I want the holidays to come so I can see you", "今天也是喜欢你的一天[害羞]", "Another day of liking you [害羞]", "你是不是偷偷想我了|老实交代", "Have you been secretly missing me?|confess" },
        };

        // Per tier: night, day, evening.
        static readonly string[][][] ByPeriod =
        {
            /* Cold */ new[]
            {
                new[] { "困了", "Sleepy", "睡了", "Going to sleep", "明天再说", "Tomorrow", "很晚了", "It's late", "不想说话", "Don't feel like talking", "早点睡吧", "Go to sleep" },
                new[] { "在上课", "In class", "忙", "Busy", "晚点", "Later", "在图书馆", "At the library", "没空", "No time", "等会儿", "In a bit" },
                new[] { "在吃饭", "Eating", "刚回来", "Just got back", "嗯 有事吗", "Mm, what is it", "在洗衣服", "Doing laundry", "累了", "Tired", "你说", "Go on" },
            },
            /* Distant */ new[]
            {
                new[] { "这么晚还不睡", "Still up this late", "我要睡了", "I'm going to sleep", "早点休息吧", "Get some rest", "明天还有课", "Class tomorrow", "室友都睡了", "My roommates are asleep", "有事明天说", "Tell me tomorrow" },
                new[] { "刚下课", "Just out of class", "在图书馆", "At the library", "等会儿说", "Talk in a bit", "在赶作业", "Rushing an assignment", "下午还有课", "More class this afternoon", "食堂人好多", "The canteen is packed" },
                new[] { "刚吃完饭", "Just ate", "在宿舍", "In the dorm", "今天有点累", "A bit tired today", "在洗头", "Washing my hair", "在看剧", "Watching something", "刚跑完步", "Just back from a run" },
            },
            /* Normal */ new[]
            {
                new[] { "你又熬夜[白眼]", "Up late again [白眼]", "我在床上刷手机", "In bed on my phone", "室友都睡了 我小声打字", "Roommates are asleep, I'm typing quietly", "宿舍断网了 用流量跟你聊", "Dorm WiFi's off, I'm on mobile data", "睡不着|你陪我聊会儿", "Can't sleep|talk to me a bit", "明天早上又起不来了[流汗]", "I'll never get up tomorrow [流汗]" },
                new[] { "刚下课 饿死了", "Just out of class, starving", "今天食堂的饭好难吃", "Canteen food was awful today", "下午还有课[流汗]", "More class this afternoon [流汗]", "老师又拖堂了", "The teacher ran over again", "在图书馆占座|人超多", "Saving a seat at the library|so crowded", "点名差点没赶上", "Nearly missed roll call" },
                new[] { "刚洗完澡", "Just showered", "在追剧", "Watching a drama", "晚饭吃的麻辣烫", "Had malatang for dinner", "室友在敷面膜|吓死宝宝了", "My roommate's in a face mask|scared me to death", "楼下又有人在弹吉他", "Someone's playing guitar downstairs again", "刚从超市回来|买了一堆零食", "Back from the supermarket|bought loads of snacks" },
            },
            /* Warm */ new[]
            {
                new[] { "你怎么还不睡|又弄电脑", "Why aren't you asleep|computer again?", "我也睡不着[月亮]", "I can't sleep either [月亮]", "陪我聊会儿嘛", "Chat with me a bit", "被窝里好暖和|就差你了", "My duvet's so warm|just missing you", "你那边下雨了吗|我这边好大的雨", "Is it raining there?|it's pouring here", "明天要早起|可是还想跟你说话", "Early start tomorrow|but I still want to talk" },
                new[] { "上课好无聊|想你", "Class is so boring|miss you", "偷偷在课上回你[偷笑]", "Replying secretly in class [偷笑]", "中午吃什么好呢", "What should I have for lunch", "今天穿了你说好看的那件", "Wore the one you said looked nice today", "老师在上面念课件|我在下面想你", "The teacher's reading the slides|I'm thinking of you", "刚在食堂看到一对情侣|好腻歪", "Saw a couple in the canteen|so lovey-dovey" },
                new[] { "我洗完澡啦", "Out of the shower", "今天的晚霞超好看", "The sunset was gorgeous tonight", "你吃饭了没|不许吃泡面", "Have you eaten?|no instant noodles", "室友问我在跟谁聊天|我说你猜[偷笑]", "My roommate asked who I'm chatting with|I said guess [偷笑]", "我在操场散步|风好舒服", "Walking round the track|lovely breeze", "刚和室友吃了烤串[呲牙]", "Just had kebabs with my roommates [呲牙]" },
            },
            /* Sweet */ new[]
            {
                new[] { "还不睡的话|那我陪你[月亮]", "If you're not sleeping|then I'll stay up with you [月亮]", "晚安之前要先说想我", "Say you miss me before good night", "今天也很喜欢你[爱心]", "Liked you a lot today too [爱心]", "我把被子裹成一团|假装是你", "I've rolled up in my duvet|pretending it's you", "好想听你声音|可是室友睡了", "I want to hear your voice|but my roommates are asleep", "你数到三|我就去睡[月亮]", "Count to three|then I'll sleep [月亮]" },
                new[] { "下课第一件事就是看手机|看你回没回", "First thing after class I check my phone|to see if you replied", "我今天上课走神了|都怪你", "I zoned out in class today|your fault", "等放假了你来接我好不好", "Will you pick me up when the holidays start?", "我在本子上画了个你[偷笑]", "I drew you in my notebook [偷笑]", "今天的奶茶算你请的|我说的", "Today's bubble tea is on you|I've decided", "午休梦到你了[害羞]", "Dreamed of you in my nap [害羞]" },
                new[] { "吃饭的时候室友问我在笑什么", "My roommate asked why I was smiling at dinner", "你在干嘛|我好无聊|想你", "What are you doing|I'm bored|miss you", "我把你的照片设成锁屏了[害羞]", "I set your photo as my lock screen [害羞]", "刚路过一家奶茶店|想起你了", "Walked past a bubble tea shop|thought of you", "今天的月亮好圆|你那边也能看到吗[月亮]", "The moon is so round tonight|can you see it too? [月亮]", "在听你推荐的歌|单曲循环了", "Listening to the song you recommended|on repeat" },
            },
        };

        /// <summary>The summer at home (June to August), in place of the school lines. By mood: cool, plain, fond.</summary>
        static readonly string[][] Summer =
        {
            new[] { "在家", "At home", "热死了", "So hot", "在睡觉", "Sleeping", "在看电视", "Watching TV" },
            new[] { "在家吹空调|哪都不想去", "AC at home|don't want to go anywhere", "我妈又在念叨我", "Mum's nagging me again", "煤球又趴我键盘上了", "Meiqiu's lying on my keyboard again", "县城好热|冰棍都化了", "The town's so hot|my ice lolly melted" },
            new[] { "在家好无聊|想你", "Bored at home|miss you", "我妈问我是不是谈恋爱了[害羞]", "Mum asked if I'm seeing someone [害羞]", "要不要出来|就一会儿", "Want to meet up?|just for a bit", "煤球也想你了[偷笑]", "Meiqiu misses you too [偷笑]" },
        };

        /// <summary>What she is busy with these days, by the key of a dated thing in her life (GirlfriendRules.Life) she already told him about.</summary>
        static readonly Dictionary<string, string[]> LifeNow = new Dictionary<string, string[]>
        {
            { "exams", new[] { "在复习|好烦", "Revising|so annoying", "还有一门没考[流汗]", "One exam left [流汗]" } },
            { "home", new[] { "在家|我妈天天做好吃的", "At home|Mum cooks something good every day" } },
            { "drama", new[] { "在补《欢乐颂》|曲筱绡好好笑", "Catching up on Ode to Joy|Qu Xiaoxiao is hilarious" } },
            { "band", new[] { "今天跑了两圈|腿好酸", "Ran two laps today|my legs ache" } },
            { "milktea", new[] { "好想喝奶茶", "I want bubble tea so bad" } },
            { "weiwei", new[] { "还在想肖奈[偷笑]", "Still thinking about Xiao Nai [偷笑]" } },
            { "back", new[] { "宿舍又要断网了", "The dorm WiFi's about to go off again" } },
            { "battery", new[] { "手机又快没电了", "My phone's nearly dead again" } },
            { "lipstick", new[] { "星辰还是没抢到[大哭]", "Still couldn't get the Star lipstick [大哭]" } },
            { "exam", new[] { "在看概率论|完全看不懂", "Reading probability|I don't understand a thing" } },
            { "examDone", new[] { "考完了好轻松", "So relaxed now the exam's over" } },
            { "cet", new[] { "在背单词|abandon 都背了八遍了", "Learning vocab|I've done \"abandon\" eight times" } },
        };

        /// <summary>Answers by what his line is about. Lines per mood: cool (冷/淡), plain (平), fond (暖/甜).</summary>
        sealed class Route
        {
            public string key;
            public string[] words;
            public string[][] lines;
        }

        static readonly Route[] Routes =
        {
            new Route { key = "goodnight", words = new[] { "晚安", "去睡", "睡了", "睡觉了", "good night", "goodnight", "going to bed", "off to bed" }, lines = new[]
            {
                new[] { "嗯 晚安", "Mm, night", "睡吧", "Go to sleep", "晚安", "Night", "哦 去吧", "Oh, go on then" },
                new[] { "晚安|早点睡别玩手机", "Good night|no phone in bed", "去吧去吧 晚安[月亮]", "Go on, good night [月亮]", "这么早？|好吧 晚安", "This early?|fine, good night", "晚安|明天见", "Good night|see you tomorrow" },
                new[] { "晚安笨蛋|梦里要有我[月亮]", "Night, dummy|I'd better be in your dreams [月亮]", "不许偷偷再玩电脑|晚安[亲亲]", "No sneaking back to the computer|good night [亲亲]", "好吧|那我也睡了|晚安[爱心]", "Fine|I'll sleep too|good night [爱心]", "晚安|今天也很想你", "Good night|missed you today too" },
            } },
            new Route { key = "sorry", words = new[] { "对不起", "抱歉", "我错了", "sorry", "my fault" }, lines = new[]
            {
                new[] { "哦", "Oh", "知道了", "Fine", "嗯", "Mm", "算了", "Forget it" },
                new[] { "知道错了就好", "As long as you know", "哼 这次先原谅你", "Hmph, forgiven this once", "下不为例哦", "Don't let it happen again", "好啦 我没生气", "OK, I'm not angry" },
                new[] { "笨蛋|我又没真生气", "Dummy|I wasn't really angry", "那你下次早点回我", "Then reply sooner next time", "哼|看在你认错快的份上[撇嘴]", "Hmph|since you said sorry so fast [撇嘴]", "好啦好啦|抱抱[拥抱]", "OK OK|hug [拥抱]" },
            } },
            new Route { key = "angry", words = new[] { "生气", "不理我", "怎么了", "不开心", "angry", "mad at me", "upset", "what's wrong" }, lines = new[]
            {
                new[] { "没有", "No", "你说呢", "What do you think", "没事", "It's nothing", "不想说", "Don't want to talk about it" },
                new[] { "没有啊|你怎么这么问", "No|why do you ask", "有一点点|就一点点", "A little|just a little", "没生气|就是有点累", "Not angry|just a bit tired", "你猜[白眼]", "Guess [白眼]" },
                new[] { "才没有|你哄哄我就好了", "Course not|just sweet-talk me a bit", "没有啦笨蛋|逗你的[偷笑]", "No, dummy|just teasing [偷笑]", "本来有一点|看到你消息就没了", "I was a bit|gone now you've messaged", "生气了|要你哄[委屈]", "I am|you have to make it up to me [委屈]" },
            } },
            new Route { key = "love", words = new[] { "想你", "爱你", "喜欢你", "miss you", "love you" }, lines = new[]
            {
                new[] { "哦", "Oh", "是吗", "Really", "嗯", "Mm", "知道了", "OK" },
                new[] { "突然这么肉麻[流汗]", "Where did that come from [流汗]", "哼 算你有良心", "Hmph, you have a heart after all", "嗯嗯|我也是", "Mm mm|me too", "那你还不多跟我说话", "Then talk to me more" },
                new[] { "我也想你[害羞]", "I miss you too [害羞]", "笨蛋|我更想你", "Dummy|I miss you more", "那你快点来见我嘛", "Then hurry up and come see me", "我就知道[偷笑]|我也是[爱心]", "I knew it [偷笑]|me too [爱心]" },
            } },
            new Route { key = "gift", words = new[] { "礼物", "买了", "送你", "快递", "惊喜", "gift", "present", "bought you", "surprise" }, lines = new[]
            {
                new[] { "不用了", "No need", "哦 谢谢", "Oh, thanks", "别乱花钱", "Don't waste money", "嗯", "Mm" },
                new[] { "什么东西啊|神神秘秘的", "What is it|so mysterious", "真的假的|你别乱花钱啊", "Seriously?|don't waste your money", "我猜猜|是吃的吗", "Let me guess|is it food?", "好吧 那我等着", "OK, I'll wait for it" },
                new[] { "啊啊啊是什么[惊讶]|快告诉我", "Aaah what is it [惊讶]|tell me", "你怎么这么好|我都不好意思了[害羞]", "Why are you so nice|now I'm embarrassed [害羞]", "我要每天查快递了[偷笑]", "I'll check the tracking every day [偷笑]", "不许告诉我|我要自己拆", "Don't tell me|I want to open it myself" },
            } },
            new Route { key = "morning", words = new[] { "早安", "早上好", "起床了", "good morning", "morning" }, lines = new[]
            {
                new[] { "早", "Morning", "嗯 早", "Mm, morning", "刚醒", "Just woke up", "早 有事吗", "Morning, what is it" },
                new[] { "早呀|你居然起这么早", "Morning|you're up early for once", "早|我还没睡醒[困]", "Morning|still half asleep [困]", "早安|今天要早起[流汗]", "Morning|early start today [流汗]", "早|昨晚又熬夜了吧", "Morning|up late again last night?" },
                new[] { "早安笨蛋[太阳]", "Morning, dummy [太阳]", "一睁眼就看到你的消息[害羞]", "Your message was the first thing I saw [害羞]", "早|梦到你了", "Morning|dreamed about you", "早安|今天也要想我哦", "Morning|think of me today too" },
            } },
            new Route { key = "whatDoing", words = new[] { "在干嘛", "干嘛呢", "在干什么", "做什么呢", "忙什么", "what are you doing", "what you up to", "what are you up to" }, lines = new[]
            {
                new[] { "没干嘛", "Nothing", "躺着", "Lying down", "发呆", "Spacing out", "有事吗", "What is it" },
                new[] { "刚洗完头|你呢", "Just washed my hair|you?", "在刷微博|好无聊", "Scrolling Weibo|so boring", "在追剧|你又在弄电脑吧", "Watching a drama|you're on the computer again, right?", "在等你找我啊", "Waiting for you to message me" },
                new[] { "在想你啊|这还用问", "Thinking of you|obviously", "在等你消息|等了好久", "Waiting for your message|for ages", "躺床上抱着手机|就等你了[害羞]", "In bed with my phone|just waiting for you [害羞]", "刚吃完零食|在想你在干嘛", "Just had snacks|wondering what you're up to" },
            } },
            new Route { key = "ate", words = new[] { "吃饭", "吃了", "吃什么", "饿", "外卖", "have you eaten", "eat", "dinner", "lunch", "hungry" }, lines = new[]
            {
                new[] { "吃了", "Yes", "还没", "Not yet", "等会儿吃", "Later", "不饿", "Not hungry" },
                new[] { "吃了|黄焖鸡米饭", "Yes|braised chicken rice", "还没 等室友一起", "Not yet, waiting for my roommates", "吃了麻辣烫|你呢", "Had malatang|you?", "不想吃|减肥中", "Don't want to|on a diet" },
                new[] { "吃了|你呢 不许又吃泡面", "Yes|you? no instant noodles again", "还没|想吃你上次说的那家", "Not yet|I want that place you mentioned", "吃了好多|胖了不许嫌弃我[可怜]", "Ate loads|you're not allowed to mind if I get fat [可怜]", "你先说你吃了没|不许骗我", "You first, have you eaten|no lying" },
            } },
            new Route { key = "work", words = new[] { "加班", "通宵", "模型", "显卡", "电脑", "网吧", "装机", "训练", "调参", "all-nighter", "computer", "graphics card", "model", "internet cafe", "training" }, lines = new[]
            {
                new[] { "哦", "Oh", "又是电脑", "Computer again", "随你", "Up to you", "你忙吧", "You get on with it" },
                new[] { "又是电脑|你那电脑比我还亲", "Computer again|you love it more than me", "别又通宵|身体要紧", "No all-nighter again|your health matters", "网吧好玩吗|有没有妹子[白眼]", "Is the internet cafe fun?|any girls there? [白眼]", "你说的那些我听不懂|反正别太累", "I don't get any of that|just don't overdo it" },
                new[] { "你又要通宵啊|那我陪你到困", "Another all-nighter?|then I'll stay up with you till I'm sleepy", "好厉害|虽然听不懂[呲牙]", "Impressive|even if I don't get it [呲牙]", "弄完了要第一个告诉我哦", "Tell me first when it's done", "别光顾着电脑|也想想我", "Don't just think about the computer|think of me too" },
            } },
            new Route { key = "tired", words = new[] { "好累", "累死", "困", "好烦", "无聊", "tired", "sleepy", "bored", "exhausted" }, lines = new[]
            {
                new[] { "那就休息", "Then rest", "哦", "Oh", "早点睡", "Sleep early", "嗯", "Mm" },
                new[] { "那你歇会儿|别硬撑", "Take a break|don't push it", "我也好无聊|陪我聊天", "I'm bored too|talk to me", "你就是电脑玩太多了", "You're on the computer too much", "无聊就来找我啊", "If you're bored, talk to me" },
                new[] { "抱抱[拥抱]|别太累了", "Hug [拥抱]|don't wear yourself out", "那我陪你|说点开心的", "Then I'll keep you company|let's talk about something nice", "心疼你|早点睡好不好", "Poor you|sleep early, OK?", "无聊就想我嘛[调皮]", "If you're bored, think about me [调皮]" },
            } },
            new Route { key = "greeting", words = new[] { "在吗", "在不在", "在么", "你好", "嗨", "hello", "hi", "hey", "are you there", "you there" }, lines = new[]
            {
                new[] { "在", "Here", "嗯", "Mm", "有事？", "What's up?", "在 怎么了", "Here, what is it" },
                new[] { "在呢|怎么啦", "Here|what's up", "在啊|找我干嘛[疑问]", "Yes|what do you want [疑问]", "在|刚想找你", "Here|was just about to message you", "不在[调皮]", "Not here [调皮]" },
                new[] { "在在在|一直在等你", "Here here here|been waiting for you", "你终于找我了[委屈]", "Finally you message me [委屈]", "在呀|想我了吧", "Here|missed me, right?", "在|你怎么才来", "Here|what took you so long" },
            } },
            new Route { key = "laugh", words = new[] { "哈哈", "嘿嘿", "233", "lol", "haha" }, lines = new[]
            {
                new[] { "呵呵", "Heh", "嗯", "Mm", "有那么好笑吗", "Is it that funny", "哦", "Oh" },
                new[] { "你笑什么|傻乎乎的", "What are you laughing at|silly", "哈哈哈哈哈", "Hahahaha", "笑什么笑[白眼]", "Stop laughing [白眼]", "有那么好笑吗[疑问]", "Is it that funny? [疑问]" },
                new[] { "哈哈哈|你笑起来肯定很傻[偷笑]", "Hahaha|you must look so silly laughing [偷笑]", "嘿嘿|跟你聊天就是开心", "Hehe|talking to you is fun", "笑得我也想笑了", "Now you've got me laughing", "傻笑什么|是不是在想我", "What's the silly grin for|thinking of me?" },
            } },
            new Route { key = "question", words = new string[0], lines = new[]
            {
                new[] { "不知道", "Don't know", "随便", "Whatever", "还行", "It's OK", "你说呢", "You tell me" },
                new[] { "这个嘛|等会儿跟你说", "Well|I'll tell you later", "还行吧|你怎么突然问这个", "It's OK|why do you ask", "你猜[调皮]", "Guess [调皮]", "嗯…让我想想", "Mm… let me think" },
                new[] { "你想知道啊|那你先说想我[调皮]", "Want to know?|say you miss me first [调皮]", "挺好的|你关心我呀[害羞]", "Pretty good|you care about me [害羞]", "我跟你说|一言难尽[流汗]", "Well|it's a long story [流汗]", "等你来了我当面告诉你", "I'll tell you in person when you come" },
            } },
            new Route { key = "short", words = new string[0], lines = new[]
            {
                new[] { "嗯", "Mm", "哦", "Oh", "行", "OK", "好", "Fine" },
                new[] { "嗯什么嗯[白眼]", "What's with the \"mm\" [白眼]", "你就回一个字？", "Just one word?", "好吧|那我去看剧了", "Fine|I'll go watch my drama", "你是不是在忙", "Are you busy?" },
                new[] { "就一个字啊[委屈]", "Just one word? [委屈]", "敷衍|罚你多说两句", "Lazy|say two more lines as punishment", "你在忙吗|忙完再理我", "Are you busy?|talk to me when you're done", "哼|不理你了[撇嘴]", "Hmph|not talking to you [撇嘴]" },
            } },
        };

        static readonly string[] ShortLines = { "嗯", "嗯嗯", "恩", "哦", "噢", "好", "好的", "好吧", "行", "ok", "okay", "mm", "oh", "k", "fine", "sure" };
        static readonly string[] QuestionWords = { "什么", "怎么", "为什么", "哪", "几", "多少", "谁", "吗", "呢", "how", "what", "why", "where", "when", "who" };

        static int Mood(GirlfriendTier t) => t <= GirlfriendTier.Distant ? 0 : t == GirlfriendTier.Normal ? 1 : 2;

        /// <summary>His line is a question: a question mark, or a question word.</summary>
        public static bool IsQuestion(string his)
        {
            string t = (his ?? "").Trim().ToLowerInvariant();
            if (t.Length == 0) return false;
            if (t.IndexOf('？') >= 0 || t.IndexOf('?') >= 0) return true;
            foreach (var w in QuestionWords)
            {
                if (w[0] < 128) { if ((" " + t + " ").Contains(" " + w + " ")) return true; }
                else if (t.Contains(w)) return true;
            }
            return false;
        }

        /// <summary>The kind of answer his line asks for (a key of the keyword routes), or "" for none.</summary>
        public static string RouteOf(string his)
        {
            string t = (his ?? "").Trim().ToLowerInvariant();
            if (t.Length == 0) return "";
            foreach (var r in Routes)
                foreach (var w in r.words)
                {
                    if (w[0] < 128 && w.Length <= 3) { if (Words(t).Contains(w)) return r.key; }
                    else if (t.Contains(w)) return r.key;
                }
            if (IsQuestion(t)) return "question";
            string bare = t.Trim('。', '.', '!', '！', '~', '～', ' ');
            foreach (var s in ShortLines) if (bare == s) return "short";
            return "";
        }

        static HashSet<string> Words(string t)
        {
            var set = new HashSet<string>();
            foreach (var w in t.Split(new[] { ' ', ',', '.', '!', '?', '，', '。', '！', '？', '~' }, StringSplitOptions.RemoveEmptyEntries)) set.Add(w);
            return set;
        }

        static void AddPairs(List<(string zh, string en)> pool, string[] pairs)
        {
            for (int i = 0; i + 1 < pairs.Length; i += 2) pool.Add((pairs[i], pairs[i + 1]));
        }

        /// <summary>School lines do not fit the summer at home.</summary>
        static bool School(string zh) =>
            zh.Contains("课") || zh.Contains("食堂") || zh.Contains("宿舍") || zh.Contains("图书馆") || zh.Contains("室友") || zh.Contains("点名") || zh.Contains("老师") || zh.Contains("操场");

        /// <summary>The general pool: tier and period lines (school lines swapped for home ones in summer), her life these days, a thing he told her.</summary>
        static List<(string zh, string en)> General(GirlfriendState s, DateTime clock)
        {
            var tier = GirlfriendRules.Tier(s);
            int t = (int)tier;
            bool summer = GirlfriendRules.Summer(clock);
            var pool = new List<(string zh, string en)>();
            AddPairs(pool, Tier[t]);
            AddPairs(pool, ByPeriod[t][(int)PeriodOf(clock)]);
            if (summer) { pool.RemoveAll(p => School(p.zh)); AddPairs(pool, Summer[Mood(tier)]); }
            if (tier >= GirlfriendTier.Distant)
                foreach (var l in GirlfriendRules.Life)
                    if (LifeNow.TryGetValue(l.key, out var lines) && s.done.Contains("life:" + l.key) && clock.Date >= l.from && clock.Date <= l.to.AddDays(3))
                        AddPairs(pool, lines);
            if (tier >= GirlfriendTier.Normal) { var m = HisMemory(s); if (m.zh != null) pool.Add(m); }
            return pool;
        }

        /// <summary>The latest thing he told her (GirlfriendRules.RememberHim), said back to him: 「我记着呢|你……」.</summary>
        static (string zh, string en) HisMemory(GirlfriendState s)
        {
            for (int i = s.memories.Count - 1; i >= 0; i--)
            {
                string m = s.memories[i];
                if (!m.StartsWith("他说：", StringComparison.Ordinal)) continue;
                string what = m.Substring(3).Trim();
                if (what.Length < 3) continue;
                bool cjk = false;
                foreach (char c in what) if (c >= 0x4e00 && c <= 0x9fff) { cjk = true; break; }
                if (cjk)
                {
                    if (what.StartsWith("他", StringComparison.Ordinal)) what = "你" + what.Substring(1);
                    return ("我记着呢|" + what, "I remember|you told me: " + what);
                }
                string you = " " + what + " ";
                foreach (var (a, b) in new[] { (" he is ", " you are "), (" he's ", " you're "), (" he was ", " you were "), (" he ", " you "), (" his ", " your "), (" him ", " you ") })
                    you = you.Replace(a, b).Replace(a.Substring(0, 2).ToUpperInvariant() + a.Substring(2), b);
                you = you.Trim();
                return ("我记着呢|" + you, "I remember|" + you);
            }
            return (null, null);
        }

        static bool Seen(GirlfriendState s, (string zh, string en) line, Func<string, string, bool> similar)
        {
            foreach (var r in s.recentLines) if (similar(r, line.zh) || similar(r, line.en)) return true;
            return false;
        }

        /// <summary>
        /// One offline reply (bubbles split by "|"). <paramref name="his"/> is his latest line: when it is a greeting,
        /// 想你, 晚安, a question and so on, the answer is of that kind; otherwise the general pool for her tier and
        /// the time of day. Never a line like one of her last <see cref="RecentLimit"/>
        /// (<paramref name="similar"/> is the YY layer's XgSpeechPolicy.Similar; null compares exactly); when every
        /// line is that recent, the one said longest ago.
        /// </summary>
        public static string[] Pick(GirlfriendState s, DateTime clock, bool english, Func<string, string, bool> similar = null, string his = null)
        {
            similar = similar ?? ((a, b) => a == b);
            var tier = GirlfriendRules.Tier(s);
            var pools = new List<List<(string zh, string en)>>();
            string key = RouteOf(his);
            if (key.Length > 0)
            {
                var route = Array.Find(Routes, r => r.key == key);
                var lines = new List<(string zh, string en)>();
                AddPairs(lines, route.lines[Mood(tier)]);
                if (GirlfriendRules.Summer(clock)) lines.RemoveAll(p => School(p.zh));
                pools.Add(lines);
            }
            var general = General(s, clock);
            pools.Add(general);
            foreach (var pool in pools)
            {
                var fresh = pool.FindAll(l => !Seen(s, l, similar));
                if (fresh.Count == 0) continue;
                return Take(s, fresh[GirlfriendRules.Pick(s, fresh.Count)], english);
            }
            // Everything was said lately: the general line said longest ago.
            var oldest = general[0];
            int best = int.MaxValue;
            foreach (var l in general)
            {
                int at = s.recentLines.FindLastIndex(r => similar(r, l.zh) || similar(r, l.en));
                if (at < best) { best = at; oldest = l; }
            }
            return Take(s, oldest, english);
        }

        static string[] Take(GirlfriendState s, (string zh, string en) pick, bool english)
        {
            Note(s, pick.zh);
            return (english ? pick.en : pick.zh).Split('|');
        }

        /// <summary>Remembers a line she sent (any source) so the library does not repeat it (the last <see cref="RecentLimit"/>).</summary>
        public static void Note(GirlfriendState s, string line)
        {
            if (s == null || string.IsNullOrEmpty(line)) return;
            if (s.recentLines.Count > 0 && s.recentLines[s.recentLines.Count - 1] == line) return;
            s.recentLines.Add(line);
            while (s.recentLines.Count > RecentLimit) s.recentLines.RemoveAt(0);
        }

        static readonly Dictionary<string, string[]> Topics = new Dictionary<string, string[]>
        {
            { "roommate", new[] { "我室友打呼噜跟拖拉机一样|根本睡不着[流汗]", "My roommate snores like a tractor|I can't sleep [流汗]", "室友半夜打电话|声音超大[白眼]", "My roommate's on the phone at midnight|so loud [白眼]", "室友占着卫生间一个小时了", "My roommate's been in the bathroom for an hour" } },
            { "photo", new[] { "[图片] 宿舍门口那只橘猫|我一摸它就跑了", "[图片] the cat at the dorm gate|it ran when I tried to pet it", "[图片] 今天的晚霞|好看吧", "[图片] tonight's sunset|pretty, right?", "[图片] 食堂新出的酸菜鱼|看着好吃 其实一般", "[图片] the canteen's new fish dish|looks good, tastes meh" } },
            { "drama", new[] { "《微微一笑很倾城》好甜|你打游戏怎么就没肖奈那么帅", "Love O2O is so sweet|why don't you play games like Xiao Nai", "《欢乐颂》你看了没|樊胜美好惨", "Have you watched Ode to Joy?|poor Fan Shengmei", "在追剧|追到半夜停不下来", "Watching a drama|can't stop, it's past midnight" } },
            { "food", new[] { "和室友吃了火锅|你吃饭了没", "Had hotpot with my roommates|have you eaten?", "今天吃了麻辣烫|加了好多辣[流汗]", "Had malatang today|way too spicy [流汗]", "好饿|想吃烤串", "So hungry|I want kebabs" } },
            { "missYou", new[] { "没什么|就是想找你说话[害羞]", "Nothing|just wanted to talk to you [害羞]", "你在干嘛|我有点想你", "What are you doing|I kind of miss you", "突然好想你[委屈]", "Suddenly missing you [委屈]" } },
            { "whatDoing", new[] { "在干嘛|又在弄电脑？", "What are you doing|on the computer again?", "人呢|在忙什么", "Where are you|busy with what?", "你今天干嘛了|跟我说说", "What did you do today|tell me" } },
        };

        /// <summary>A free topic when the model is off: a line of that topic she has not said lately (bubbles).</summary>
        public static string[] Topic(string key, bool english, GirlfriendState s = null, Func<string, string, bool> similar = null)
        {
            if (!Topics.TryGetValue(key ?? "", out var pairs)) pairs = Topics["whatDoing"];
            var pool = new List<(string zh, string en)>();
            AddPairs(pool, pairs);
            if (s == null) return (english ? pool[0].en : pool[0].zh).Split('|');
            similar = similar ?? ((a, b) => a == b);
            var fresh = pool.FindAll(l => !Seen(s, l, similar));
            if (fresh.Count == 0) fresh = pool;
            return Take(s, fresh[GirlfriendRules.Pick(s, fresh.Count)], english);
        }
    }
}
