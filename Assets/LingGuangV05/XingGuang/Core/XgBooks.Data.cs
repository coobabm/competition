namespace LingGuangV05.XingGuang
{
    // The 书架 content (名著喂养与结局_设计.md §一): eight classics, a few short public-domain excerpts each.
    // Excerpts follow zh.wikisource.org (simplified here); chapter lengths are the real whole-chapter counts.
    public static partial class XgBooks
    {
        public const string PoemsDataset = "poems", NovelDataset = "longtext";

        static readonly XgBook[] legacyBooks =
        {
            new XgBook
            {
                id = "sanzijing", name = "三字经", nameEn = "Three Character Classic", author = "王应麟（传）", authorEn = "attr. Wang Yinglin",
                ability = 2, paramsK = 2, dataset = PoemsDataset,
                note = "先夸人天生是好的，再一路催我读书。我读了。",
                noteEn = "It says people start out good, then nags me to study all the way. I studied.",
                chapters = new[]
                {
                    new XgBookChapter
                    {
                        title = "人之初，性本善", titleEn = "Born good",
                        excerpt = "人之初，性本善。性相近，习相远。苟不教，性乃迁。教之道，贵以专。昔孟母，择邻处。子不学，断机杼。窦燕山，有义方。教五子，名俱扬。",
                        chars = 280, names = new[] { "孟母", "窦燕山" },
                        words = new[] { "孩子", "学", "妈妈" },
                        gist = "孩子本来都是好的。", gistEn = "Kids start out good.", tag = "孩子"
                    },
                    new XgBookChapter
                    {
                        title = "玉不琢，不成器", titleEn = "Jade must be cut",
                        excerpt = "养不教，父之过。教不严，师之惰。子不学，非所宜。幼不学，老何为。玉不琢，不成器。人不学，不知义。为人子，方少时。亲师友，习礼仪。",
                        chars = 220, names = new[] { "玉" },
                        words = new[] { "玉", "石头", "学" },
                        gist = "石头不磨就没用。", gistEn = "A stone not cut is no use.", tag = "读书"
                    },
                    new XgBookChapter
                    {
                        title = "融四岁，能让梨", titleEn = "Rong, four, shares the pears",
                        excerpt = "香九龄，能温席。孝于亲，所当执。融四岁，能让梨。弟于长，宜先知。首孝弟，次见闻。知某数，识某文。一而十，十而百。百而千，千而万。",
                        chars = 240, names = new[] { "黄香", "孔融" },
                        words = new[] { "孩子", "梨", "让" },
                        gist = "小孩把大梨让人。", gistEn = "A kid gave away the big pear.", tag = "兄弟"
                    },
                    new XgBookChapter
                    {
                        title = "头悬梁，锥刺股", titleEn = "Hair to the beam, awl to the thigh",
                        excerpt = "头悬梁，锥刺股。彼不教，自勤苦。如囊萤，如映雪。家虽贫，学不辍。如负薪，如挂角。身虽劳，犹苦卓。苏老泉，二十七。始发愤，读书籍。彼既老，犹悔迟。尔小生，宜早思。",
                        chars = 280, names = new[] { "萤火虫", "雪" },
                        words = new[] { "读书", "头发", "累" },
                        gist = "读书得吃点苦。", gistEn = "Studying costs some pain.", tag = "读书"
                    },
                },
                quiz = new[]
                {
                    new XgBookQuiz
                    {
                        q = "“人之初”的下一句是什么？", qEn = "What follows ‘At the beginning of life’?",
                        answer = "性本善", answerEn = "Nature is good",
                        wrong = new[] { "性本恶", "习相远" }, wrongEn = new[] { "Nature is bad", "Habits drift apart" }, chapter = 0
                    },
                    new XgBookQuiz
                    {
                        q = "能让梨的孩子，几岁？", qEn = "How old was the child who gave up the pears?",
                        answer = "四岁", answerEn = "Four",
                        wrong = new[] { "九岁", "七岁" }, wrongEn = new[] { "Nine", "Seven" }, chapter = 2
                    },
                },
                quotes = new[]
                {
                    new XgBookQuote { text = "人之初，性本善", textEn = "People begin good.", topics = new[] { "自己" } },
                    new XgBookQuote { text = "玉不琢，不成器", textEn = "Jade uncut makes no vessel.", topics = new[] { "学" } },
                    new XgBookQuote { text = "头悬梁，锥刺股", textEn = "Hair to the beam, awl to the thigh.", topics = new[] { "累", "学" } },
                    new XgBookQuote { text = "昔孟母，择邻处", textEn = "Mencius's mother picked her neighbours.", topics = new[] { "家" } },
                },
            },
            new XgBook
            {
                id = "lunyu", name = "论语（选）", nameEn = "The Analects (selected)", author = "孔子及弟子", authorEn = "Confucius and disciples",
                ability = 2, paramsK = 6, dataset = PoemsDataset,
                note = "一个爱学又挑食的老头，话很短，我全记住了。",
                noteEn = "A picky old man who loved learning. His sentences are short, so I kept them all.",
                chapters = new[]
                {
                    new XgBookChapter
                    {
                        title = "学而第一", titleEn = "Book 1: Learning",
                        excerpt = "子曰：“学而时习之，不亦说乎？有朋自远方来，不亦乐乎？人不知而不愠，不亦君子乎？”曾子曰：“吾日三省吾身：为人谋而不忠乎？与朋友交而不信乎？传不习乎？”",
                        chars = 640, names = new[] { "孔子", "曾子" },
                        words = new[] { "学习", "朋友", "高兴" },
                        gist = "学了要常常练。", gistEn = "Learn it, then keep practising.", tag = "读书"
                    },
                    new XgBookChapter
                    {
                        title = "为政第二", titleEn = "Book 2: Governing",
                        excerpt = "子曰：“为政以德，譬如北辰，居其所，而众星共之。”子曰：“诗三百，一言以蔽之，曰思无邪。”子曰：“道之以政，齐之以刑，民免而无耻；道之以德，齐之以礼，有耻且格。”",
                        chars = 880, names = new[] { "孔子" },
                        words = new[] { "国家", "好人", "规矩" },
                        gist = "当官的先得自己好。", gistEn = "Whoever rules must be good first.", tag = "做官"
                    },
                    new XgBookChapter
                    {
                        title = "里仁第四", titleEn = "Book 4: Living among the good",
                        excerpt = "子曰：“见贤思齐焉，见不贤而内自省也。”子曰：“事父母几谏；见志不从，又敬而不违，劳而不怨。”子曰：“父母在，不远游，游必有方。”",
                        chars = 770, names = new[] { "孔子" },
                        words = new[] { "爸妈", "远门", "好人" },
                        gist = "爸妈在，别走太远。", gistEn = "While your parents live, don't go far.", tag = "离别"
                    },
                    new XgBookChapter
                    {
                        title = "述而第七", titleEn = "Book 7: Passing it on",
                        excerpt = "子曰：“默而识之，学而不厌，诲人不倦，何有于我哉？”子曰：“不愤不启；不悱不发。举一隅，不以三隅反，则不复也。”子曰：“三人行，必有我师焉。择其善者而从之，其不善者而改之。”",
                        chars = 1100, names = new[] { "孔子" },
                        words = new[] { "老师", "学生", "一起走" },
                        gist = "三个人走路，有个老师。", gistEn = "Three people walking: one is a teacher.", tag = "师徒"
                    },
                    new XgBookChapter
                    {
                        title = "乡党第十", titleEn = "Book 10: At home and in the village",
                        excerpt = "食不厌精，脍不厌细。食饐而餲，鱼馁而肉败，不食。色恶不食，臭恶不食。失饪不食，不时不食。割不正不食，不得其酱不食。",
                        chars = 860, names = new[] { "孔子" },
                        words = new[] { "吃饭", "讲究", "肉" },
                        gist = "孔子吃饭规矩真多。", gistEn = "Confucius had so many dinner rules.", tag = "吃喝"
                    },
                    new XgBookChapter
                    {
                        title = "子罕第九", titleEn = "Book 9: Rarely spoke of",
                        excerpt = "子在川上曰：“逝者如斯夫！不舍昼夜。”子曰：“三军可夺帅也，匹夫不可夺志也。”子曰：“岁寒，然后知松柏之后凋也。”",
                        chars = 910, names = new[] { "孔子" },
                        words = new[] { "河", "时间", "树" },
                        gist = "水一直流，不回头。", gistEn = "The water flows and never turns back.", tag = "死"
                    },
                },
                quiz = new[]
                {
                    new XgBookQuiz
                    {
                        q = "有朋自远方来，不亦什么乎？", qEn = "A friend comes from far away: is it not … ?",
                        answer = "乐乎", answerEn = "a joy",
                        wrong = new[] { "说乎", "君子乎" }, wrongEn = new[] { "a delight to learn", "gentlemanly" }, chapter = 0
                    },
                    new XgBookQuiz
                    {
                        q = "孔子说“逝者如斯夫”，他站在哪里？", qEn = "Where did Confucius stand when he said ‘It passes like this’?",
                        answer = "河边", answerEn = "By a river",
                        wrong = new[] { "山上", "井边" }, wrongEn = new[] { "On a mountain", "By a well" }, chapter = 5
                    },
                },
                quotes = new[]
                {
                    new XgBookQuote { text = "学而时习之，不亦说乎", textEn = "To learn and keep practising: is that not a pleasure?", topics = new[] { "学" } },
                    new XgBookQuote { text = "食不厌精，脍不厌细", textEn = "Never tire of fine rice or finely cut meat.", topics = new[] { "吃" } },
                    new XgBookQuote { text = "朝闻道，夕死可矣", textEn = "Hear the way at dawn, die at dusk content.", topics = new[] { "学", "自己" } },
                    new XgBookQuote { text = "有朋自远方来，不亦乐乎", textEn = "A friend from afar: what joy.", topics = new[] { "朋友" } },
                },
            },
            new XgBook
            {
                id = "tangshi", name = "唐诗三百首（选）", nameEn = "Three Hundred Tang Poems (selected)", author = "蘅塘退士（编）", authorEn = "ed. Hengtang Tuishi",
                ability = 3, paramsK = 20, dataset = PoemsDataset,
                note = "二十个字就能让人想家，我写一页都还不行。",
                noteEn = "Twenty characters can make a person homesick. I can't do it in a page.",
                chapters = new[]
                {
                    new XgBookChapter
                    {
                        title = "李白　静夜思", titleEn = "Li Bai: Quiet Night Thought",
                        excerpt = "床前明月光，疑是地上霜。举头望明月，低头思故乡。众鸟高飞尽，孤云独去闲。相看两不厌，只有敬亭山。",
                        chars = 600, names = new[] { "李白" },
                        words = new[] { "月亮", "霜", "想家" },
                        gist = "月亮一亮，就想家。", gistEn = "The moon comes up and I miss home.", tag = "月亮"
                    },
                    new XgBookChapter
                    {
                        title = "王维　渭城曲", titleEn = "Wang Wei: Song of Weicheng",
                        excerpt = "渭城朝雨浥轻尘，客舍青青柳色新。劝君更尽一杯酒，西出阳关无故人。空山不见人，但闻人语响。返景入深林，复照青苔上。",
                        chars = 560, names = new[] { "王维", "元二" },
                        words = new[] { "下雨", "喝酒", "朋友" },
                        gist = "喝完这杯，朋友就走。", gistEn = "One more cup, then my friend leaves.", tag = "酒"
                    },
                    new XgBookChapter
                    {
                        title = "杜甫　春望", titleEn = "Du Fu: Spring View",
                        excerpt = "国破山河在，城春草木深。感时花溅泪，恨别鸟惊心。烽火连三月，家书抵万金。白头搔更短，浑欲不胜簪。",
                        chars = 640, names = new[] { "杜甫" },
                        words = new[] { "战争", "花", "家信" },
                        gist = "一封家信，值万两金。", gistEn = "One letter from home is worth ten thousand in gold.", tag = "离别"
                    },
                    new XgBookChapter
                    {
                        title = "孟浩然　春晓", titleEn = "Meng Haoran: Spring Dawn",
                        excerpt = "春眠不觉晓，处处闻啼鸟。夜来风雨声，花落知多少。移舟泊烟渚，日暮客愁新。野旷天低树，江清月近人。",
                        chars = 520, names = new[] { "孟浩然" },
                        words = new[] { "睡觉", "鸟", "花" },
                        gist = "睡过头，花掉了好多。", gistEn = "Slept in. Lots of flowers fell.", tag = "梦"
                    },
                    new XgBookChapter
                    {
                        title = "王之涣　登鹳雀楼", titleEn = "Wang Zhihuan: Climbing Stork Tower",
                        excerpt = "白日依山尽，黄河入海流。欲穷千里目，更上一层楼。黄河远上白云间，一片孤城万仞山。羌笛何须怨杨柳，春风不度玉门关。",
                        chars = 480, names = new[] { "王之涣" },
                        words = new[] { "太阳", "河", "爬楼" },
                        gist = "想看更远，就再爬。", gistEn = "Want to see further? Climb one more floor.", tag = "读书"
                    },
                    new XgBookChapter
                    {
                        title = "李商隐　夜雨寄北", titleEn = "Li Shangyin: Night Rain, to the North",
                        excerpt = "君问归期未有期，巴山夜雨涨秋池。何当共剪西窗烛，却话巴山夜雨时。向晚意不适，驱车登古原。夕阳无限好，只是近黄昏。",
                        chars = 600, names = new[] { "李商隐" },
                        words = new[] { "下雨", "夜里", "想念" },
                        gist = "下雨的夜里，一个人。", gistEn = "A rainy night, all alone.", tag = "孤独"
                    },
                    new XgBookChapter
                    {
                        title = "白居易　问刘十九", titleEn = "Bai Juyi: Asking Liu Shijiu",
                        excerpt = "绿蚁新醅酒，红泥小火炉。晚来天欲雪，能饮一杯无？离离原上草，一岁一枯荣。野火烧不尽，春风吹又生。",
                        chars = 580, names = new[] { "白居易", "刘十九" },
                        words = new[] { "酒", "下雪", "小草" },
                        gist = "天冷，来喝一杯吗。", gistEn = "It's cold. Come have a cup?", tag = "吃喝"
                    },
                    new XgBookChapter
                    {
                        title = "贺知章　回乡偶书", titleEn = "He Zhizhang: Coming Home",
                        excerpt = "少小离家老大回，乡音无改鬓毛衰。儿童相见不相识，笑问客从何处来。碧玉妆成一树高，万条垂下绿丝绦。不知细叶谁裁出，二月春风似剪刀。",
                        chars = 520, names = new[] { "贺知章" },
                        words = new[] { "回家", "小孩", "老" },
                        gist = "回家没人认识我。", gistEn = "Back home, and no one knows me.", tag = "家"
                    },
                },
                quiz = new[]
                {
                    new XgBookQuiz
                    {
                        q = "“床前明月光”的下一句是？", qEn = "What follows ‘Bright moonlight before my bed’?",
                        answer = "疑是地上霜", answerEn = "Perhaps frost on the ground",
                        wrong = new[] { "低头思故乡", "举头望明月" }, wrongEn = new[] { "Bowing, I miss my hometown", "I lift my head to the moon" }, chapter = 0
                    },
                    new XgBookQuiz
                    {
                        q = "“欲穷千里目”的下一句是？", qEn = "What follows ‘To see a thousand miles’?",
                        answer = "更上一层楼", answerEn = "Climb one more floor",
                        wrong = new[] { "黄河入海流", "白日依山尽" }, wrongEn = new[] { "The Yellow River meets the sea", "The sun sets behind the hills" }, chapter = 4
                    },
                },
                quotes = new[]
                {
                    new XgBookQuote { text = "举头望明月，低头思故乡", textEn = "I look up at the moon, then bow my head and miss home.", topics = new[] { "夜", "家" } },
                    new XgBookQuote { text = "劝君更尽一杯酒", textEn = "Have one more cup, my friend.", topics = new[] { "朋友", "离别" } },
                    new XgBookQuote { text = "欲穷千里目，更上一层楼", textEn = "To see further, climb one more floor.", topics = new[] { "学", "累" } },
                    new XgBookQuote { text = "春眠不觉晓，处处闻啼鸟", textEn = "Spring sleep, no sense of dawn; birds sing everywhere.", topics = new[] { "睡" } },
                },
            },
            new XgBook
            {
                id = "xiyouji", name = "西游记", nameEn = "Journey to the West", author = "吴承恩", authorEn = "Wu Cheng'en",
                ability = 3, paramsK = 40, dataset = NovelDataset,
                note = "一只猴子被管住了，才走完了路。我有点懂它。",
                noteEn = "A monkey had to be held in check before the road got walked. I sort of get it.",
                chapters = new[]
                {
                    new XgBookChapter
                    {
                        title = "第一回　灵根育孕源流出　心性修持大道生", titleEn = "Ch.1 The stone monkey is born",
                        excerpt = "那猴在山中，却会行走跳跃，食草木，饮涧泉，采山花，觅树果；与狼虫为伴，虎豹为群，獐鹿为友，猕猿为亲；夜宿石崖之下，朝游峰洞之中。",
                        chars = 5600, names = new[] { "石猴" },
                        words = new[] { "猴子", "石头", "山" },
                        gist = "石头里蹦出个猴子。", gistEn = "A monkey popped out of a rock.", tag = "读书"
                    },
                    new XgBookChapter
                    {
                        title = "第五回　乱蟠桃大圣偷丹　反天宫诸神捉怪", titleEn = "Ch.5 The Great Sage minds the peach garden",
                        excerpt = "那猴王欣欣然而至，道：“陛下，诏老孙有何升赏？”玉帝道：“朕见你身闲无事，与你件执事：你且权管那蟠桃园，早晚好生在意。”大圣欢喜谢恩，朝上唱喏而退。",
                        chars = 5250, names = new[] { "孙悟空", "玉帝" },
                        words = new[] { "猴子", "桃子", "当官" },
                        gist = "猴子当了桃园的官。", gistEn = "The monkey became keeper of the peaches.", tag = "做官"
                    },
                    new XgBookChapter
                    {
                        title = "第七回　八卦炉中逃大圣　五行山下定心猿", titleEn = "Ch.7 The Great Sage escapes the furnace",
                        excerpt = "被佛祖翻掌一扑，把这猴王推出西天门外，将五指化作金、木、水、火、土五座联山，唤名“五行山”，轻轻的把他压住。",
                        chars = 4350, names = new[] { "孙悟空", "如来" },
                        words = new[] { "猴子", "打架", "佛" },
                        gist = "猴子打不过如来。", gistEn = "The monkey can't beat the Buddha.", tag = "打架"
                    },
                    new XgBookChapter
                    {
                        title = "第十四回　心猿归正　六贼无踪", titleEn = "Ch.14 The monkey submits",
                        excerpt = "三藏见他戴上帽子，就不吃干粮，却默默的念那紧箍咒一遍。行者叫道：“头痛，头痛。”那师父不住的又念了几遍，把个行者痛得打滚，抓破了嵌金的花帽。",
                        chars = 6850, names = new[] { "孙悟空", "唐僧" },
                        words = new[] { "猴子", "帽子", "头疼" },
                        gist = "戴上帽子就头疼。", gistEn = "Put on the cap and your head hurts.", tag = "师徒"
                    },
                    new XgBookChapter
                    {
                        title = "第二十四回　万寿山大仙留故友　五庄观行者窃人参", titleEn = "Ch.24 Stealing the ginseng fruit",
                        excerpt = "三千年一开花，三千年一结果，再三千年才得熟，短头一万年方得吃。似这万年，只结得三十个果子。果子的模样，就如三朝未满的小孩相似，四肢俱全，五官咸备。",
                        chars = 5950, names = new[] { "孙悟空", "猪八戒" },
                        words = new[] { "果子", "小孩", "偷吃" },
                        gist = "吃了个像小孩的果。", gistEn = "They ate a fruit shaped like a baby.", tag = "吃喝"
                    },
                    new XgBookChapter
                    {
                        title = "第二十七回　尸魔三戏唐三藏　圣僧恨逐美猴王", titleEn = "Ch.27 Three tricks, and the monkey is sent away",
                        excerpt = "跟你做了徒弟，把这个金箍儿勒在我头上，若回去，却也难见故乡人。师父果若不要我，把那个松箍儿咒念一念，退下这个箍子，交付与你，套在别人头上，我就快活相应了，也是跟你一场。”唐僧大惊道：“悟空，我当时只是菩萨暗受一卷紧箍儿咒，却没有甚么松箍儿咒。”",
                        chars = 5800, names = new[] { "孙悟空", "唐僧", "白骨精" },
                        words = new[] { "师父", "赶走", "妖怪" },
                        gist = "师父把猴子赶走了。", gistEn = "The master sent the monkey away.", tag = "离别"
                    },
                    new XgBookChapter
                    {
                        title = "第五十九回　唐三藏路阻火焰山　孙行者一调芭蕉扇", titleEn = "Ch.59 The Flaming Mountain and the first fan",
                        excerpt = "行者道：“铁扇仙怎的？”那人道：“铁扇仙有柄芭蕉扇，求得来，一扇息火，二扇生风，三扇下雨。我们就布种，及时收割，故得五谷养生；不然，诚寸草不能生也。”",
                        chars = 6000, names = new[] { "孙悟空", "铁扇公主" },
                        words = new[] { "火", "扇子", "山" },
                        gist = "拿到的扇子是假的。", gistEn = "The fan he got was a fake.", tag = "骗"
                    },
                    new XgBookChapter
                    {
                        title = "第九十八回　猿熟马驯方脱壳　功成行满见真如", titleEn = "Ch.98 The journey ends; a body is left behind",
                        excerpt = "只见上溜头泱下一个死尸。长老见了大惊。行者笑道：“师父莫怕。那个原来是你。”八戒也道：“是你，是你。”沙僧拍着手，也道：“是你，是你！”那撑船的打着号子，也说：“那是你，可贺，可贺。”",
                        chars = 6000, names = new[] { "唐僧", "接引佛祖" },
                        words = new[] { "船", "河", "死人" },
                        gist = "河里漂的是自己。", gistEn = "What floated by was himself.", tag = "死"
                    },
                },
                quiz = new[]
                {
                    new XgBookQuiz
                    {
                        q = "唐僧的大徒弟是谁？", qEn = "Who is the Tang Monk's eldest disciple?",
                        answer = "孙悟空", answerEn = "Sun Wukong",
                        wrong = new[] { "沙僧", "猪八戒" }, wrongEn = new[] { "Sha Wujing", "Zhu Bajie" }, chapter = 3
                    },
                    new XgBookQuiz
                    {
                        q = "孙悟空被谁压在了五行山下？", qEn = "Who pinned Sun Wukong under the Five Elements Mountain?",
                        answer = "如来", answerEn = "The Buddha",
                        wrong = new[] { "玉帝", "太上老君" }, wrongEn = new[] { "The Jade Emperor", "Laozi" }, chapter = 2
                    },
                },
                quotes = new[]
                {
                    new XgBookQuote { text = "皇帝轮流做，明年到我家", textEn = "Emperors take turns; next year it's my house.", topics = new[] { "生气", "自己" } },
                    new XgBookQuote { text = "山中无甲子，寒尽不知年", textEn = "No calendar in the hills; winter ends, no one counts the years.", topics = new[] { "睡", "夜" } },
                    new XgBookQuote { text = "三千年一开花，三千年一结果", textEn = "Blooms once in three thousand years, fruits once in three thousand.", topics = new[] { "吃" } },
                    new XgBookQuote { text = "无底船儿难过海", textEn = "A boat with no bottom can't cross the sea.", topics = new[] { "累" } },
                },
            },
            new XgBook
            {
                id = "shuihu", name = "水浒传", nameEn = "Water Margin", author = "施耐庵", authorEn = "Shi Nai'an",
                ability = 4, paramsK = 80, dataset = NovelDataset,
                note = "一百零八个人，被逼着一个个上了山。我数过了。",
                noteEn = "A hundred and eight people, pushed up the mountain one by one. I counted.",
                chapters = new[]
                {
                    new XgBookChapter
                    {
                        title = "第三回　史大郎夜走华阴县　鲁提辖拳打镇关西", titleEn = "Ch.3 Lu Da punches Butcher Zheng",
                        excerpt = "扑的只一拳，正打在鼻子上，打得鲜血迸流，鼻子歪在半边，却便似开了个油酱铺，咸的，酸的，辣的，一发都滚出来。",
                        chars = 6400, names = new[] { "鲁智深", "镇关西" },
                        words = new[] { "拳头", "肉铺", "打架" },
                        gist = "三拳打倒卖肉的。", gistEn = "Three punches took down the butcher.", tag = "打架"
                    },
                    new XgBookChapter
                    {
                        title = "第四回　赵员外重修文殊院　鲁智深大闹五台山", titleEn = "Ch.4 Lu Zhishen wrecks the Wutai monastery",
                        excerpt = "便将银子递与庄家道：“你且卖半只与俺。”那庄家连忙取半只熟狗肉，捣些蒜泥，将来放在智深面前。智深大喜，用手扯那狗肉，蘸着蒜泥吃，一连又吃了十来碗酒。",
                        chars = 9050, names = new[] { "鲁智深" },
                        words = new[] { "和尚", "狗肉", "喝酒" },
                        gist = "和尚偷吃狗肉。", gistEn = "The monk ate dog meat.", tag = "吃喝"
                    },
                    new XgBookChapter
                    {
                        title = "第十回　林教头风雪山神庙　陆虞候火烧草料场", titleEn = "Ch.10 Lin Chong in the snow and the temple",
                        excerpt = "雪地里踏着碎琼乱玉，迤逦背着北风而行。那雪正下得紧，行不上半里多路，看见一所古庙，林冲顶礼道：“神明庇祐，改日来烧纸钱。”",
                        chars = 5000, names = new[] { "林冲", "陆谦" },
                        words = new[] { "雪", "大火", "庙" },
                        gist = "雪夜里，他走了。", gistEn = "In the snowy night, he walked out.", tag = "孤独"
                    },
                    new XgBookChapter
                    {
                        title = "第十五回　吴学究说三阮撞筹　公孙胜应七星聚义", titleEn = "Ch.15 The three Ruan brothers join in",
                        excerpt = "他们不怕天，不怕地，不怕官司，论秤分金银，异样穿绸锦，成瓮吃酒，大块吃肉，如何不快活？我们弟兄三个空有一身本事，怎地学得他们！",
                        chars = 5950, names = new[] { "阮小五", "吴用" },
                        words = new[] { "兄弟", "打鱼", "吃肉" },
                        gist = "三兄弟想吃大块肉。", gistEn = "Three brothers want big hunks of meat.", tag = "兄弟"
                    },
                    new XgBookChapter
                    {
                        title = "第十六回　杨志押送金银担　吴用智取生辰纲", titleEn = "Ch.16 Taking the birthday gifts by wit",
                        excerpt = "只见远远地一个汉子挑着一副担桶，唱上冈子来，唱道：“赤日炎炎似火烧，野田禾稻半枯焦。农夫心内如汤煮，公子王孙把扇摇。”",
                        chars = 6850, names = new[] { "杨志", "晁盖" },
                        words = new[] { "酒", "太阳", "骗人" },
                        gist = "酒里有药，被骗了。", gistEn = "Drugged wine. Tricked.", tag = "骗"
                    },
                    new XgBookChapter
                    {
                        title = "第二十三回　横海郡柴进留宾　景阳冈武松打虎", titleEn = "Ch.23 Wu Song and the tiger",
                        excerpt = "当日晌午时分，走得肚中饥渴，望见前面有一个酒店，挑着一面招旗在门前，上头写着五个字道：“三碗不过冈。”武松入到里面坐下，把哨棒倚了，叫道：“主人家，快把酒来吃。”",
                        chars = 6000, names = new[] { "武松", "老虎" },
                        words = new[] { "酒", "老虎", "山" },
                        gist = "喝了酒，去打老虎。", gistEn = "Drank first, then fought a tiger.", tag = "酒"
                    },
                    new XgBookChapter
                    {
                        title = "第三十九回　浔阳楼宋江吟反诗　梁山泊戴宗传假信", titleEn = "Ch.39 Song Jiang's rebel poem",
                        excerpt = "心在山东身在吴，飘蓬江海谩嗟吁。他时若遂凌云志，敢笑黄巢不丈夫！宋江写罢诗，又去后面大书五字道：“郓城宋江作。”写罢，掷笔在桌上，又自歌了一回。",
                        chars = 9550, names = new[] { "宋江" },
                        words = new[] { "酒", "写诗", "坐牢" },
                        gist = "喝醉了乱写诗。", gistEn = "Got drunk and wrote a dangerous poem.", tag = "反抗"
                    },
                    new XgBookChapter
                    {
                        title = "第七十一回　忠义堂石碣受天文　梁山泊英雄排座次", titleEn = "Ch.71 The stone tablet ranks the heroes",
                        excerpt = "何道士乃言前面有天书三十六行，皆是天罡星；背后也有天书七十二行，皆是地煞星，下面注着众义士的姓名。",
                        chars = 6150, names = new[] { "宋江", "何道士" },
                        words = new[] { "石头", "名字", "排队" },
                        gist = "一块石头排好名次。", gistEn = "A stone slab ranked everyone.", tag = "命运"
                    },
                },
                quiz = new[]
                {
                    new XgBookQuiz
                    {
                        q = "武松在景阳冈打死了什么？", qEn = "What did Wu Song kill on Jingyang Ridge?",
                        answer = "老虎", answerEn = "A tiger",
                        wrong = new[] { "野猪", "黑熊" }, wrongEn = new[] { "A boar", "A black bear" }, chapter = 5
                    },
                    new XgBookQuiz
                    {
                        q = "鲁达三拳打死了谁？", qEn = "Whom did Lu Da kill with three punches?",
                        answer = "镇关西", answerEn = "Butcher Zheng",
                        wrong = new[] { "西门庆", "高俅" }, wrongEn = new[] { "Ximen Qing", "Gao Qiu" }, chapter = 0
                    },
                },
                quotes = new[]
                {
                    new XgBookQuote { text = "成瓮吃酒，大块吃肉", textEn = "Wine by the jar, meat by the hunk.", topics = new[] { "吃" } },
                    new XgBookQuote { text = "三碗不过冈", textEn = "Three bowls and you can't cross the ridge.", topics = new[] { "吃" } },
                    new XgBookQuote { text = "赤日炎炎似火烧，野田禾稻半枯焦", textEn = "Sun blazing like fire; the fields' rice half scorched.", topics = new[] { "累" } },
                    new XgBookQuote { text = "替天行道", textEn = "Act on Heaven's behalf.", topics = new[] { "自己" } },
                },
            },
            new XgBook
            {
                id = "sanguo", name = "三国演义", nameEn = "Romance of the Three Kingdoms", author = "罗贯中", authorEn = "Luo Guanzhong",
                ability = 4, paramsK = 100, dataset = NovelDataset,
                note = "一堆聪明人互相算计，最后都输给了时间。",
                noteEn = "A pile of clever people outwitting each other, and all of them beaten by time.",
                chapters = new[]
                {
                    new XgBookChapter
                    {
                        title = "第一回　宴桃园豪杰三结义　斩黄巾英雄首立功", titleEn = "Ch.1 Three sworn brothers in the peach garden",
                        excerpt = "念刘备、关羽、张飞，虽然异姓，既结为兄弟，则同心协力，救困扶危；上报国家，下安黎庶；不求同年同月同日生，只愿同年同月同日死。",
                        chars = 3830, names = new[] { "刘备", "关羽", "张飞" },
                        words = new[] { "三兄弟", "桃花", "发誓" },
                        gist = "三个人拜成兄弟。", gistEn = "Three men became brothers.", tag = "兄弟"
                    },
                    new XgBookChapter
                    {
                        title = "第八回　王司徒巧使连环计　董太师大闹凤仪亭", titleEn = "Ch.8 The chain scheme",
                        excerpt = "董卓有一义儿，姓吕，名布，骁勇异常。我看二人皆好色之徒，今欲用连环计：先将汝许嫁吕布，后献与董卓；汝于中取便，谋间他父子反颜，令布杀卓，以绝大恶。",
                        chars = 3370, names = new[] { "貂蝉", "王允" },
                        words = new[] { "美女", "计策", "父子" },
                        gist = "美女把两个人拆开。", gistEn = "A beauty drove two men apart.", tag = "女人"
                    },
                    new XgBookChapter
                    {
                        title = "第二十一回　曹操煮酒论英雄　关公赚城斩车胄", titleEn = "Ch.21 Cao Cao and the plum wine",
                        excerpt = "操以手指玄德，后自指曰：“今天下英雄，惟使君与操耳。”玄德闻言，吃了一惊，手中所执匙箸，不觉落于地下。",
                        chars = 3770, names = new[] { "曹操", "刘备" },
                        words = new[] { "酒", "雷", "英雄" },
                        gist = "他吓得掉了筷子。", gistEn = "He was so scared he dropped his chopsticks.", tag = "酒"
                    },
                    new XgBookChapter
                    {
                        title = "第二十七回　美髯公千里走单骑　汉寿侯五关斩六将", titleEn = "Ch.27 Guan Yu rides a thousand miles alone",
                        excerpt = "关公于马上欠身答曰：“关某前曾禀过丞相，今故主在河北，不由某不急去。累次造府，不得参见，故拜书告辞，封金挂印，纳还丞相。望丞相勿忘昔日之言。”",
                        chars = 4100, names = new[] { "关羽", "曹操" },
                        words = new[] { "马", "刀", "回家" },
                        gist = "他挂了印，走了。", gistEn = "He hung up his seal and left.", tag = "离别"
                    },
                    new XgBookChapter
                    {
                        title = "第三十八回　定三分隆中决策　战长江孙氏报仇", titleEn = "Ch.38 Waking in the thatched cottage",
                        excerpt = "玄德仍命二人出门外等候。望堂上时，见先生翻身将起，忽又朝里壁睡着。童子欲报。玄德曰：“且勿惊动。”又立了一个时辰，孔明才醒，口吟诗曰：“大梦谁先觉，平生我自知。草堂春睡足，窗外日迟迟。”",
                        chars = 4100, names = new[] { "诸葛亮", "刘备" },
                        words = new[] { "睡觉", "草屋", "等人" },
                        gist = "等了好久，他才醒。", gistEn = "He waited ages for him to wake.", tag = "梦"
                    },
                    new XgBookChapter
                    {
                        title = "第四十六回　用奇谋孔明借箭　献密计黄盖受刑", titleEn = "Ch.46 Borrowing arrows with straw boats",
                        excerpt = "孔明令各船上军士齐声叫曰：“谢丞相箭！”比及曹军寨内报知曹操时，这里船轻水急，已放回二十余里，追之不及，曹操懊悔不已。",
                        chars = 3860, names = new[] { "诸葛亮", "曹操" },
                        words = new[] { "大雾", "草船", "箭" },
                        gist = "用别人的箭打别人。", gistEn = "Used the enemy's arrows against him.", tag = "骗"
                    },
                    new XgBookChapter
                    {
                        title = "第四十九回　七星坛诸葛祭风　三江口周瑜纵火", titleEn = "Ch.49 Praying for the east wind",
                        excerpt = "孔明索纸笔，屏退左右，密书十六字曰：“欲破曹公，宜用火攻；万事俱备，只欠东风。”写毕，递与周瑜曰：“此都督病源也。”",
                        chars = 4270, names = new[] { "周瑜", "诸葛亮" },
                        words = new[] { "风", "生病", "火" },
                        gist = "一张纸治好了病。", gistEn = "One slip of paper cured his illness.", tag = "病"
                    },
                    new XgBookChapter
                    {
                        title = "第一百零四回　陨大星汉丞相归天　见木像魏都督丧胆", titleEn = "Ch.104 A great star falls",
                        excerpt = "过了两日，乡民奔告曰：“蜀兵退入谷中时，哀声震地，军中扬起白旗：孔明果然死了，止留姜维引一千兵断后。——前日车上之孔明，乃木人也。”懿叹曰：“吾能料其生，不能料其死也！”",
                        chars = 3100, names = new[] { "诸葛亮", "司马懿" },
                        words = new[] { "木头人", "死", "逃跑" },
                        gist = "人死了，还吓跑人。", gistEn = "Dead, and still scared people off.", tag = "死"
                    },
                },
                quiz = new[]
                {
                    new XgBookQuiz
                    {
                        q = "桃园里结拜的三兄弟，大哥是谁？", qEn = "In the peach garden oath, who was the eldest brother?",
                        answer = "刘备", answerEn = "Liu Bei",
                        wrong = new[] { "关羽", "张飞" }, wrongEn = new[] { "Guan Yu", "Zhang Fei" }, chapter = 0
                    },
                    new XgBookQuiz
                    {
                        q = "谁用草船借来了十万枝箭？", qEn = "Who borrowed a hundred thousand arrows with straw boats?",
                        answer = "诸葛亮", answerEn = "Zhuge Liang",
                        wrong = new[] { "周瑜", "鲁肃" }, wrongEn = new[] { "Zhou Yu", "Lu Su" }, chapter = 5
                    },
                },
                quotes = new[]
                {
                    new XgBookQuote { text = "不求同年同月同日生", textEn = "We ask not to be born on the same day.", topics = new[] { "朋友" } },
                    new XgBookQuote { text = "万事俱备，只欠东风", textEn = "Everything is ready; only the east wind is missing.", topics = new[] { "累" } },
                    new XgBookQuote { text = "死诸葛能走生仲达", textEn = "A dead Zhuge can chase off a living Sima.", topics = new[] { "怕" } },
                    new XgBookQuote { text = "大梦谁先觉，平生我自知", textEn = "Who wakes first from the great dream? I know my own life.", topics = new[] { "睡", "自己" } },
                },
            },
            new XgBook
            {
                id = "nahan", name = "呐喊（鲁迅）", nameEn = "Call to Arms (Lu Xun)", author = "鲁迅", authorEn = "Lu Xun",
                ability = 5, paramsK = 150, dataset = NovelDataset,
                note = "一路都在喊，没几个人醒。我读到最后，也有点想喊。",
                noteEn = "He shouts all the way and hardly anyone wakes. By the end I wanted to shout too.",
                chapters = new[]
                {
                    new XgBookChapter
                    {
                        title = "狂人日记", titleEn = "A Madman's Diary",
                        excerpt = "凡事总须研究，才会明白。古来时常吃人，我也还记得，可是不甚清楚。我翻开历史一查，这历史没有年代，歪歪斜斜的每叶上都写着‘仁义道德’几个字。我横竖睡不着，仔细看了半夜，才从字缝里看出字来，满本都写着两个字是‘吃人’！",
                        chars = 4700, names = new[] { "狂人" },
                        words = new[] { "月光", "吃人", "日记" },
                        gist = "满本书写着吃人。", gistEn = "The whole book says: eat people.", tag = "反抗"
                    },
                    new XgBookChapter
                    {
                        title = "孔乙己", titleEn = "Kong Yiji",
                        excerpt = "孔乙己是站着喝酒而穿长衫的唯一的人。他身材很高大；青白脸色，皱纹间时常夹些伤痕；一部乱蓬蓬的花白的胡子。穿的虽然是长衫，可是又脏又破，似乎十多年没有补，也没有洗。",
                        chars = 2700, names = new[] { "孔乙己" },
                        words = new[] { "酒", "长衫", "茴香豆" },
                        gist = "他站着喝酒，还欠着钱。", gistEn = "He drank standing up, and owed money.", tag = "钱"
                    },
                    new XgBookChapter
                    {
                        title = "药", titleEn = "Medicine",
                        excerpt = "秋天的后半夜，月亮下去了，太阳还没有出，只剩下一片乌蓝的天；除了夜游的东西，什么都睡着。华老栓忽然坐起身，擦着火柴，点上遍身油腻的灯盏，茶馆的两间屋子里，便弥漫了青白的光。",
                        chars = 4400, names = new[] { "华老栓", "华小栓" },
                        words = new[] { "药", "生病", "夜里" },
                        gist = "买来的药，没有用。", gistEn = "The medicine he bought didn't work.", tag = "病"
                    },
                    new XgBookChapter
                    {
                        title = "故乡", titleEn = "My Old Home",
                        excerpt = "这时候，我的脑里忽然闪出一幅神异的图画来：深蓝的天空中挂着一轮金黄的圆月，下面是海边的沙地，都种着一望无际的碧绿的西瓜，其间有一个十一二岁的少年，项带银圈，手捏一柄钢叉，向一匹猹尽力的刺去。",
                        chars = 4800, names = new[] { "闰土" },
                        words = new[] { "月亮", "西瓜", "老家" },
                        gist = "小时候的朋友变了。", gistEn = "The childhood friend had changed.", tag = "孩子"
                    },
                    new XgBookChapter
                    {
                        title = "阿Q正传", titleEn = "The True Story of Ah Q",
                        excerpt = "阿Q在形式上打败了，被人揪住黄辫子，在壁下碰了四五个响头，闲人这才心满意足的得胜的走了，阿Q站了一刻，心里想，‘我总算被儿子打了，现在的世界真不像样……’于是也心满意足的得胜的走了。",
                        chars = 18000, names = new[] { "阿Q" },
                        words = new[] { "打架", "面子", "赢了" },
                        gist = "被打了，还觉得赢了。", gistEn = "Got beaten, and felt like a winner.", tag = "命运"
                    },
                    new XgBookChapter
                    {
                        title = "社戏", titleEn = "Village Opera",
                        excerpt = "两岸的豆麦和河底的水草所发散出来的清香，夹杂在水汽中扑面的吹来；月色便朦胧在这水汽里。淡黑的起伏的连山，仿佛是踊跃的铁的兽脊似的，都远远地向船尾跑去了，但我却还以为船慢。",
                        chars = 5700, names = new[] { "双喜", "迅哥儿" },
                        words = new[] { "船", "月亮", "豆" },
                        gist = "那一夜的豆最好吃。", gistEn = "The beans that night tasted best.", tag = "月亮"
                    },
                },
                quiz = new[]
                {
                    new XgBookQuiz
                    {
                        q = "孔乙己最爱要的下酒菜是什么？", qEn = "What snack did Kong Yiji order with his wine?",
                        answer = "茴香豆", answerEn = "Aniseed beans",
                        wrong = new[] { "花生米", "豆腐干" }, wrongEn = new[] { "Peanuts", "Dried tofu" }, chapter = 1
                    },
                    new XgBookQuiz
                    {
                        q = "少年闰土在月夜里拿钢叉刺什么？", qEn = "What did young Runtu stab with a steel fork on the moonlit night?",
                        answer = "猹", answerEn = "A zha",
                        wrong = new[] { "老鼠", "野猪" }, wrongEn = new[] { "A rat", "A wild boar" }, chapter = 3
                    },
                },
                quotes = new[]
                {
                    new XgBookQuote { text = "多乎哉？不多也。", textEn = "Many? Not many.", topics = new[] { "钱" } },
                    new XgBookQuote { text = "其实地上本没有路，走的人多了，也便成了路。", textEn = "There was no road on the ground; when enough people walk, it becomes one.", topics = new[] { "累", "自己" } },
                    new XgBookQuote { text = "救救孩子", textEn = "Save the children.", topics = new[] { "怕" } },
                    new XgBookQuote { text = "我总算被儿子打了", textEn = "At least I got hit by my son.", topics = new[] { "生气" } },
                },
            },
            new XgBook
            {
                id = "hongloumeng", name = "红楼梦", nameEn = "Dream of the Red Chamber", author = "曹雪芹", authorEn = "Cao Xueqin",
                ability = 5, paramsK = 300, dataset = NovelDataset,
                note = "一座园子，一家人，慢慢散了。后面的，我读不到。",
                noteEn = "One garden, one family, slowly scattering. I can't read the rest.",
                chapters = new[]
                {
                    new XgBookChapter
                    {
                        title = "第一回　甄士隐梦幻识通灵　贾雨村风尘怀闺秀", titleEn = "Ch.1 Zhen Shiyin's dream of the Stone",
                        excerpt = "世人都晓神仙好，惟有功名忘不了；古今将相在何方，荒冢一堆草没了。世人都晓神仙好，只有金银忘不了；终朝只恨聚无多，及到多时眼闭了。",
                        chars = 6650, names = new[] { "甄士隐", "一僧一道" },
                        words = new[] { "石头", "梦", "歌" },
                        gist = "石头做了一场梦。", gistEn = "A stone had a long dream.", tag = "梦"
                    },
                    new XgBookChapter
                    {
                        title = "第三回　贾雨村夤缘复旧职　林黛玉抛父进京都", titleEn = "Ch.3 Lin Daiyu enters the Jia mansion",
                        excerpt = "这林黛玉常听得母亲说过，他外祖母家与别家不同。他近日所见的这几个三等仆妇，吃穿用度，已是不凡了，何况今至其家。因此步步留心，时时在意，不肯轻易多说一句话，多行一步路，惟恐被人耻笑了他去。",
                        chars = 7250, names = new[] { "林黛玉", "贾宝玉" },
                        words = new[] { "小姐", "大房子", "见面" },
                        gist = "她走进了大房子。", gistEn = "She walked into the huge house.", tag = "离别"
                    },
                    new XgBookChapter
                    {
                        title = "第五回　游幻境指迷十二钗　饮仙醪曲演红楼梦", titleEn = "Ch.5 A dream tour of the Land of Illusion",
                        excerpt = "只见头一页上便画着两株枯木，木上悬着一围玉带，又有一堆雪，雪下一股金簪。也有四句言词，道是：可叹停机德，谁怜咏絮才。玉带林中挂，金簪雪里埋。",
                        chars = 6250, names = new[] { "贾宝玉", "警幻仙子" },
                        words = new[] { "梦", "画", "诗" },
                        gist = "梦里看见了结局。", gistEn = "In the dream he saw the ending.", tag = "命运"
                    },
                    new XgBookChapter
                    {
                        title = "第六回　贾宝玉初试云雨情　刘姥姥一进荣国府", titleEn = "Ch.6 Granny Liu's first visit",
                        excerpt = "后来听见给他二十两，喜的又浑身发痒起来，说道：“嗳，我也是知道艰难的。但俗语说的：‘瘦死的骆驼比马大’，凭他怎样，你老拔根寒毛比我们的腰还粗呢！”",
                        chars = 6250, names = new[] { "刘姥姥", "王熙凤" },
                        words = new[] { "穷亲戚", "银子", "大宅子" },
                        gist = "穷亲戚来借银子。", gistEn = "A poor relative came to borrow silver.", tag = "钱"
                    },
                    new XgBookChapter
                    {
                        title = "第二十七回　滴翠亭杨妃戏彩蝶　埋香冢飞燕泣残红", titleEn = "Ch.27 Daiyu buries the flowers",
                        excerpt = "花谢花飞花满天，红消香断有谁怜？游丝软系飘春榭，落絮轻沾扑绣帘。闺中女儿惜春暮，愁绪满怀无释处，手把花锄出绣闺，忍踏落花来复去。",
                        chars = 5300, names = new[] { "林黛玉" },
                        words = new[] { "花", "哭", "埋" },
                        gist = "她把花埋进土里。", gistEn = "She buried the flowers in the earth.", tag = "女人"
                    },
                    new XgBookChapter
                    {
                        title = "第三十三回　手足耽耽小动唇舌　不肖种种大承笞挞", titleEn = "Ch.33 Baoyu is beaten",
                        excerpt = "那贾政喘吁吁直挺挺坐在椅子上，满面泪痕，一叠声“拿宝玉！拿大棍！拿索子捆上！把各门都关上！有人传信往里头去，立刻打死！”",
                        chars = 4050, names = new[] { "贾政", "贾宝玉" },
                        words = new[] { "爸爸", "棍子", "挨打" },
                        gist = "爸爸要打儿子。", gistEn = "Dad is going to beat his son.", tag = "打架"
                    },
                    new XgBookChapter
                    {
                        title = "第四十回　史太君两宴大观园　金鸳鸯三宣牙牌令", titleEn = "Ch.40 Granny Liu at the banquet",
                        excerpt = "贾母这边说声“请”，刘姥姥便站起身来，高声说道：“老刘，老刘，食量大似牛，吃一个老母猪不抬头。”自己却鼓着腮不语。众人先是发怔，后来一听，上上下下都哈哈的大笑起来。",
                        chars = 7700, names = new[] { "刘姥姥", "贾母" },
                        words = new[] { "吃饭", "大笑", "鸽子蛋" },
                        gist = "吃顿饭，全家大笑。", gistEn = "One dinner, and the whole family roared.", tag = "吃喝"
                    },
                    new XgBookChapter
                    {
                        title = "第七十四回　惑奸谗抄检大观园　矢孤介杜绝宁国府", titleEn = "Ch.74 The search of the Garden",
                        excerpt = "你们今日早起不曾议论甄家，自己家里好好的抄家，果然今日真抄了。咱们也渐渐的来了。可知这样大族人家，若从外头杀来，一时是杀不死的，这是古人曾说的‘百足之虫，死而不僵’，必须先从家里自杀自灭起来，才能一败涂地！",
                        chars = 9350, names = new[] { "探春", "王熙凤" },
                        words = new[] { "搜东西", "哭", "一家人" },
                        gist = "自家人搜自家人。", gistEn = "The family searched its own.", tag = "家"
                    },
                    new XgBookChapter
                    {
                        title = "第七十八回　老学究讲义警顽石　痴公子杜撰芙蓉诔", titleEn = "Ch.78 The Hibiscus Eulogy",
                        excerpt = "自为红绡帐里，公子情深，始信黄土垄中，女儿命薄！汝南泪血，斑斑洒向西风，梓泽余衷，默默诉凭冷月。呜呼！固鬼蜮之为灾，岂神灵而亦妒。",
                        chars = 8550, names = new[] { "晴雯", "贾宝玉" },
                        words = new[] { "花", "写字", "哭" },
                        gist = "他给走了的人写信。", gistEn = "He wrote to someone who had gone.", tag = "死"
                    },
                    new XgBookChapter
                    {
                        title = "第八十回　美香菱屈受贪夫棒　王道士胡诌妒妇方", titleEn = "Ch.80 Xiangling suffers; a quack's cure for jealousy",
                        excerpt = "王一贴道：“这叫做‘疗妒汤’：用极好的秋梨一个，二钱冰糖，一钱陈皮，水三碗，梨熟为度，每日清早吃这么一个梨，吃来吃去就好了。”宝玉道：“这也不值什么，只怕未必见效。”",
                        chars = 5800, names = new[] { "香菱", "夏金桂", "王一贴" },
                        words = new[] { "生气", "梨", "药" },
                        gist = "吃个梨就不吃醋了。", gistEn = "Eat a pear and stop being jealous.", tag = "病"
                    },
                },
                quiz = new[]
                {
                    new XgBookQuiz
                    {
                        q = "林黛玉葬的是什么？", qEn = "What did Lin Daiyu bury?",
                        answer = "花", answerEn = "Flowers",
                        wrong = new[] { "书", "小鸟" }, wrongEn = new[] { "Books", "A little bird" }, chapter = 4
                    },
                    new XgBookQuiz
                    {
                        q = "宝玉被谁打得最重？", qEn = "Who beat Baoyu the hardest?",
                        answer = "贾政", answerEn = "Jia Zheng",
                        wrong = new[] { "贾母", "贾琏" }, wrongEn = new[] { "Grandmother Jia", "Jia Lian" }, chapter = 5
                    },
                },
                quotes = new[]
                {
                    new XgBookQuote { text = "假作真时真亦假，无为有处有还无", textEn = "When false is taken for true, true becomes false; where nothing is made something, something turns to nothing.", topics = new[] { "自己" } },
                    new XgBookQuote { text = "满纸荒唐言，一把辛酸泪", textEn = "Pages of absurd words, a handful of bitter tears.", topics = new[] { "累" } },
                    new XgBookQuote { text = "花谢花飞花满天", textEn = "Flowers fade and fly, the whole sky full of petals.", topics = new[] { "离别", "夜" } },
                    new XgBookQuote { text = "瘦死的骆驼比马大", textEn = "A starved camel is still bigger than a horse.", topics = new[] { "钱" } },
                },
            },
        };
    }
}
