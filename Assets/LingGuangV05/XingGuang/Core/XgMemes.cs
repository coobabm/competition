using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One phrase for a text desk: its label, the date it appeared online and where it comes from.</summary>
    public sealed class XgPhrase
    {
        public string text;
        /// <summary>The right answer to the desk's question.</summary>
        public bool yes;
        /// <summary>First day the phrase may appear (yyyymmdd). Later memes stay hidden until the game clock reaches them.</summary>
        public int since = 20150101;
        public string source = "", why = "";
        /// <summary>English wording of <see cref="text"/> and <see cref="why"/>; empty = same as the Chinese (the 2016 desks quote Chinese phrases as they are).</summary>
        public string textEn = "", whyEn = "";
        /// <summary>老司机题: reads like the other answer. Only from level 3.</summary>
        public bool trick;
        public int level = 1;
        public string topic = "";
    }

    /// <summary>A 2016 hot word (design v1.1 §14.2): when it may appear and where it goes.</summary>
    public sealed class XgHotWord
    {
        public string word = "", use = "";
        /// <summary>First day it may appear (yyyymmdd); 20150101 = all year.</summary>
        public int since = 20150101;
        /// <summary>Destinations, design v1.1 §14 abbreviations: D 弹幕桌, S 短信桌, H 标题党桌, R 评论桌, M 表情包桌,
        /// Y YY 群, Z 老周（被问时）, W “我”的独白, A AI 词联想, T 贴吧, UI 界面, 喵 喵鱼.</summary>
        public string[] to = new string[0];
        /// <summary>Only a few times (memes that peak at the very end of 2016).</summary>
        public bool rare;
        public bool GoesTo(string destination) => Array.IndexOf(to, destination) >= 0;
    }

    /// <summary>
    /// The 2016 internet desks: danmaku sentiment, SMS spam, clickbait, fake reviews and Chinglish.
    /// Every phrase carries its answer, so the truth is known by construction. Nothing dated after the
    /// game clock is shown (the calendar runs from 2016-05-24 to New Year's Eve): 葛优躺, 洪荒之力 and the like
    /// wait for their month. Post-2016 memes (EraLexicon) never appear.
    /// </summary>
    public static partial class XgMemes
    {
        public const int MaxLevel = 3;

        static XgPhrase P(string text, bool yes, string why, string source = "", int since = 20150101, int level = 1, bool trick = false, string topic = "")
        { return new XgPhrase { text = text, yes = yes, why = why, source = source, since = since, level = trick ? 3 : level, trick = trick, topic = topic }; }

        /// <summary>Hot topics, one per game day (only those that already happened).</summary>
        public static readonly (string topic, int since)[] Topics =
        {
            ("太阳的后裔", 20160224), ("AlphaGo", 20160309), ("科比", 20160414), ("papi酱", 20160301),
            ("集五福", 20160128), ("老司机", 20160101), ("友谊的小船", 20160331), ("叶问3", 20160304),
            ("魔兽", 20160608), ("欧洲杯", 20160611), ("葛优躺", 20160725), ("里约奥运", 20160806), ("洪荒之力", 20160808),
            ("阴阳师", 20160902), ("iPhone 7", 20160908), ("蓝瘦香菇", 20161010), ("双11", 20161101), ("厉害了我的哥", 20161105),
            ("你的名字", 20161202),
        };

        /// <summary>Days a topic counts as this month's (design v1.1 §7.0: the month's topics replace the old ones).</summary>
        public const int FreshTopicDays = 45;

        /// <summary>Today's hot topic: one of the recent ones if any, otherwise any that already happened.</summary>
        public static string TopicOf(int today)
        {
            var open = new List<string>();
            var fresh = new List<string>();
            int now = DayIndex(today);
            foreach (var t in Topics)
            {
                if (t.since > today) continue;
                open.Add(t.topic);
                if (now - DayIndex(t.since) <= FreshTopicDays) fresh.Add(t.topic);
            }
            if (fresh.Count > 0) open = fresh;
            if (open.Count == 0) return "";
            return open[((now % open.Count) + open.Count) % open.Count];
        }

        static XgHotWord W(string word, int since, string use, string to, bool rare = false)
        { return new XgHotWord { word = word, since = since, use = use, to = to.Split(' '), rare = rare }; }

        /// <summary>Design v1.1 §14.2, with month and destination. Banned post-2016 memes are in EraLexicon.</summary>
        public static readonly XgHotWord[] HotWords =
        {
            W("老司机", 20150101, "夸人熟练", "D A"), W("带带我", 20150101, "夸人熟练", "D A"),
            W("666", 20150101, "基础弹幕", "D"), W("给跪了", 20150101, "基础弹幕", "D"), W("233", 20150101, "基础弹幕", "D"),
            W("前方高能", 20150101, "基础弹幕", "D"), W("弹幕护体", 20150101, "基础弹幕", "D"),
            W("友谊的小船说翻就翻", 20160331, "NaN 时：模型的小船说翻就翻", "W"),
            W("吃瓜群众", 20150101, "围观", "D T"), W("前排卖瓜子", 20150101, "围观", "D T"),
            W("一言不合就", 20150101, "一言不合就 NaN", "W Y"),
            W("套路", 20150101, "短信桌：这些全是套路", "S W"), W("城市套路深", 20150101, "短信桌引导语", "S W"),
            W("撩", 20150101, "自由语气词示例“会撩”", "A"),
            W("狗带", 20150101, "低频", "D"), W("我想静静", 20150101, "低频", "D"), W("然并卵", 20150101, "低频", "D"), W("主要看气质", 20160101, "低频", "D"),
            W("为什么不问问神奇海螺", 20150101, "贴吧回帖", "T"),
            W("宝宝心里苦", 20150101, "AI 短句（阶段 4）", "A"), W("吓死宝宝了", 20150101, "AI 短句（阶段 4）", "A"),
            W("屁股", 20160524, "守望先锋", "Y"),
            W("为了部落", 20160608, "魔兽", "Y M"),
            W("感觉身体被掏空", 20160727, "显卡 65°C 降频", "UI W"),
            W("葛优躺", 20160725, "表情包", "M"),
            W("洪荒之力", 20160808, "评级 S：我已经用了洪荒之力", "W A M"), W("鬼知道我经历了什么", 20160808, "评级 S", "W A M"),
            W("醒醒", 20160821, "群友叫人起来看球", "Y"),
            W("小目标", 20160829, "订单页标题", "UI"),
            W("SSR", 20160902, "这轮非酋了", "W A Y"), W("非酋", 20160902, "这轮非酋了", "W A Y"), W("欧皇", 20160902, "抽卡", "W A Y"),
            W("蓝瘦香菇", 20161010, "训练发散、订单失败", "M W A"),
            W("厉害了我的哥", 20161105, "评级 S", "D W"),
            W("没想到吧", 20150101, "涌现时刻的独白备选", "W"),
            W("辣眼睛", 20150101, "反话卡", "D"), W("有毒", 20150101, "反话卡", "D"), W("停不下来", 20150101, "反话卡", "D"),
            W("一人我饮酒醉", 20150101, "低级弹幕卡", "D"),
            W("谢邀", 20150101, "贴吧学知乎腔", "T"), W("人在美国刚下飞机", 20150101, "贴吧学知乎腔", "T"),
            W("心好累", 20161220, "极少量", "D", true), W("我可能是个假的", 20161220, "极少量", "D", true),
            W("老铁", 20161201, "极少量", "D", true), W("双击 666", 20161201, "极少量", "D", true),
            W("洋垃圾", 20150101, "显卡吧、喵鱼", "T 喵 Z"), W("垃圾佬", 20150101, "显卡吧、喵鱼", "T 喵 Z"), W("战未来", 20150101, "显卡吧、喵鱼", "T 喵 Z"),
            W("等等党", 20150101, "显卡吧、喵鱼", "T 喵 Z"), W("买新不买旧", 20150101, "显卡吧、喵鱼", "T 喵 Z"),
        };

        /// <summary>Hot words that may go to a destination today.</summary>
        public static List<XgHotWord> HotWordsFor(string destination, int today)
        {
            var list = new List<XgHotWord>();
            foreach (var w in HotWords) if (w.since <= today && w.GoesTo(destination)) list.Add(w);
            return list;
        }

        static int DayIndex(int yyyymmdd)
        {
            int y = yyyymmdd / 10000, m = yyyymmdd / 100 % 100, d = yyyymmdd % 100;
            try { return (int)(new DateTime(y, Math.Max(1, m), Math.Max(1, d)) - new DateTime(2016, 1, 1)).TotalDays; }
            catch (ArgumentException) { return 0; }
        }

        public static string Question(string desk)
        {
            switch (desk)
            {
                case "danmu": return "这条弹幕是在夸吗？";
                case "spam": return "这条是垃圾短信吗？";
                case "headline": return "这是标题党吗？";
                case "review": return "这条评论是刷单刷出来的吗？";
                case "translate": return "这句英文说得对吗？";
                case SenseDesk: return "这句话说得对吗？";
                default: return "是吗？";
            }
        }

        public static string QuestionEn(string desk)
        {
            switch (desk)
            {
                case "danmu": return "Is this comment praise?";
                case "spam": return "Is this SMS spam?";
                case "headline": return "Is this clickbait?";
                case "review": return "Is this a paid fake review?";
                case "translate": return "Is this English correct?";
                case SenseDesk: return "Is this statement right?";
                default: return "Yes?";
            }
        }

        public static XgPhrase[] Bank(string desk)
        {
            switch (desk)
            {
                case "danmu": return Danmu;
                case "spam": return Spam;
                case "headline": return Headline;
                case "review": return Review;
                case "translate": return Chinglish;
                case SenseDesk: return Sense;
                default: return null;
            }
        }

        public static bool IsTextDesk(string desk) { return Bank(desk) != null; }

        /// <summary>
        /// Picks a phrase: the answer first (about half yes), then a phrase with that answer, open by date and level.
        /// From level 3, about one card in seven is a 老司机 trick. Today's topic shows up three times as often.
        /// </summary>
        public static XgPhrase Pick(string desk, Random r, int level, int today, string topic)
        {
            var bank = Bank(desk);
            if (bank == null) return null;
            bool want = r.NextDouble() < .5;
            bool trick = level >= 3 && r.NextDouble() < .15;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var pool = new List<XgPhrase>();
                double total = 0;
                foreach (var p in bank)
                {
                    if (p.since > today || p.level > level) continue;
                    if (attempt < 2 && p.yes != want) continue;
                    if (attempt == 0 && p.trick != trick) continue;
                    pool.Add(p); total += Weight(p, topic);
                }
                if (pool.Count == 0) continue;
                double x = r.NextDouble() * total;
                foreach (var p in pool) { x -= Weight(p, topic); if (x <= 0) return p; }
                return pool[pool.Count - 1];
            }
            return bank[0];
        }

        static double Weight(XgPhrase p, string topic) { return !string.IsNullOrEmpty(topic) && p.topic == topic ? 3 : 1; }

        // ───────────── 弹幕情绪：这条弹幕是在夸吗？ ─────────────

        static readonly XgPhrase[] Danmu =
        {
            P("666666", true, "「6」=「溜」，夸操作厉害。", "游戏直播弹幕"),
            P("给跪了", true, "佩服到跪下，是夸。", "网络用语"),
            P("up 主辛苦了", true, "感谢做视频的人，是夸。", "B 站弹幕"),
            P("这波操作可以", true, "夸操作。", "游戏直播弹幕"),
            P("23333333 笑死", true, "「233」是猫扑第 233 号表情，捶地大笑。笑成这样是夸。", "猫扑表情 233"),
            P("BGM 一响，鸡皮疙瘩起来了", true, "被背景音乐打动，是夸。", "B 站弹幕"),
            P("前方高能，非战斗人员请撤离", true, "提醒后面有精彩内容，是在捧。", "B 站弹幕「前方高能」"),
            P("这剪辑我服", true, "服 = 佩服，是夸。", "B 站弹幕"),
            P("猴赛雷！", true, "粤语「好犀利」，好厉害。猴年春晚吉祥物让它火了一把。", "2016 年 1 月，猴年春晚吉祥物「康康」", 20160105),
            P("宋仲基太帅了啊啊啊", true, "夸人帅。", "《太阳的后裔》，2016-02-24 开播", 20160224, topic: "太阳的后裔"),
            P("papi 酱说出了我的心声", true, "说到心坎里，是夸。", "papi 酱短视频，2016 年初走红", 20160301, topic: "papi酱"),
            P("AlphaGo 这一步神了", true, "神了 = 太厉害了。", "AlphaGo 对李世石，2016-03-09 至 15 日", 20160309, topic: "AlphaGo"),
            P("曼巴永不退场", true, "致敬科比，是夸。", "科比退役战，2016-04-14", 20160414, topic: "科比"),
            P("老司机带带我", true, "老司机 = 老手。求老手带，是夸对方懂行。", "云南山歌《老司机带带我》，2016 年初走红", 20160101, topic: "老司机"),
            P("我要打十个", true, "叶问的台词，用来夸打得帅。", "电影《叶问 3》，2016-03-04 上映", 20160304, topic: "叶问3"),
            P("这是用了洪荒之力吧", true, "夸拼尽全力。", "傅园慧里约奥运采访，2016-08-08", 20160808, topic: "洪荒之力"),
            P("为了部落！", true, "《魔兽》电影的口号，看得热血，是在捧。", "电影《魔兽》，2016-06-08 上映", 20160608, topic: "魔兽"),
            P("维京战吼，啪！啪！啪！", true, "学冰岛球迷的战吼，是在捧。", "欧洲杯冰岛队，2016-06", 20160628, topic: "欧洲杯"),
            P("女排精神！", true, "致敬中国女排夺冠，是夸。", "里约奥运女排夺冠，2016-08-21", 20160821, topic: "里约奥运"),
            P("极乐净土，单曲循环一百遍", true, "停不下来，是夸。", "《极乐净土》宅舞，2016 年夏", 20160801),
            P("欧皇附体，十连出 SSR", true, "「欧皇」= 运气好的人，又羡慕又夸。", "《阴阳师》抽卡，2016-09", 20160902, topic: "阴阳师"),
            P("厉害了我的哥", true, "夸对方厉害。", "2016 年 11 月走红", 20161105, topic: "厉害了我的哥"),
            P("老铁，双击 666", true, "直播间求点赞，也是在捧。", "直播间用语，2016 年底", 20161215, level: 2),

            P("辣眼睛", false, "看到难看的东西，眼睛被辣到。是骂。", "2016 年流行语"),
            P("我选择狗带", false, "「狗带」= go die，表示受不了。不是夸。", "黄子韬的英文 rap，2016 年 1 月爆红", 20160101),
            P("这届 up 主不行", false, "改自「这届人民不行」，是嫌弃。", "「这届人民不行」，2016-03 走红", 20160324),
            P("看不下去了，关了", false, "不看了，是差评。", "B 站弹幕"),
            P("退钱！", false, "觉得不值，是骂。", "网络用语"),
            P("尴尬癌都犯了", false, "尴尬到受不了，是吐槽。", "2015 年流行语"),
            P("宝宝心里苦", false, "委屈，是吐槽。", "2016 年流行语"),
            P("感觉身体被掏空", false, "累到不行，是吐槽。", "《感觉身体被掏空》，2016-07-27", 20160727, topic: "葛优躺"),
            P("又是非酋的一天", false, "「非酋」= 运气差，是自嘲，不是夸。", "《阴阳师》抽卡，2016-09", 20160902, topic: "阴阳师"),
            P("蓝瘦，香菇", false, "「难受，想哭」，不是夸。", "广西小哥的视频，2016-10", 20161010, topic: "蓝瘦香菇"),
            P("又是广告，告诉我怎么跳过", false, "嫌广告烦，不是夸。", "视频网站弹幕"),
            P("友谊的小船说翻就翻，up 主你变了", false, "说对方变了、关系翻船，是抱怨。", "喃东尼漫画，2016-03-31", 20160331, topic: "友谊的小船"),
            P("来了来了", false, "只是打招呼，不是夸。", "B 站弹幕", level: 2),
            P("空降 3:20", false, "告诉别人跳到哪里看，不是夸。", "B 站弹幕", level: 2),
            P("打卡", false, "签到，不是夸。", "B 站弹幕", level: 2),
            P("第一！", false, "抢沙发，不是夸。", "论坛和弹幕", level: 2),
            P("前排卖瓜子饮料矿泉水", false, "吃瓜群众在占座，没有在夸。", "「吃瓜群众」，2016 年流行语", level: 2),
            P("弹幕护体", false, "用弹幕挡住吓人的画面，不是夸。", "B 站弹幕", level: 2),

            P("真棒，一集水了四十分钟", false, "反话：「真棒」是讽刺注水。", "反讽弹幕", trick: true),
            P("好厉害哦，字幕全是错别字", false, "反话：嫌字幕差。", "反讽弹幕", trick: true),
            P("感谢 up 主，又浪费了我十分钟", false, "反话：「感谢」是在骂浪费时间。", "反讽弹幕", trick: true),
            P("这 up 主有毒，我循环了十遍", true, "「有毒」= 停不下来，是夸。", "网络用语", trick: true),
            P("吓死我了，再来一遍", true, "嘴上喊怕，其实爱看，是夸。", "B 站弹幕", trick: true),
            P("丧心病狂，但我喜欢", true, "看着像骂，结尾说喜欢，是夸。", "B 站弹幕", trick: true),
        };

        // ───────────── 垃圾短信：这条是垃圾短信吗？ ─────────────

        static readonly XgPhrase[] Spam =
        {
            P("【我是歌手】恭喜您被栏目组抽中为场外幸运观众，获得奖金 98000 元，请登录 wsgs-hd.cc 领取", true, "冒充综艺节目中奖，经典诈骗。", "冒充综艺中奖诈骗，2015—2016 年常见"),
            P("老板您好，本公司可代开各类发票，联系 138****2016", true, "代开发票是违法广告。", "代开发票短信"),
            P("亲，一手货源正品代购，加微信 dg2016 了解一下", true, "群发的微商广告。", "微商刷屏，2015—2016"),
            P("您的银行卡积分可兑换 368 元现金，回复 Y 领取", true, "积分兑现金 + 让你回复，是钓鱼。", "积分兑换诈骗"),
            P("澳门首家线上赌场上线啦，注册即送 88 元", true, "赌博广告。", "赌博广告短信"),
            P("低价出售 iPhone 6s，全新未拆封，只要 1888", true, "价格离谱的群发广告。", "手机诈骗广告"),
            P("【集五福】恭喜获得敬业福！点击 jwf2016.cc 领取 2016 元现金", true, "冒充支付宝集五福，链接是假的。", "支付宝集五福，2016 年春节", 20160128, topic: "集五福"),
            P("papi 酱同款面膜今日特价，戳链接抢购", true, "蹭热点的群发广告。", "papi 酱走红，2016 年初", 20160301, topic: "papi酱"),
            P("宋仲基同款军装外套 5 折包邮，仅限今天", true, "蹭剧的群发广告。", "《太阳的后裔》，2016-02-24 开播", 20160224, topic: "太阳的后裔"),
            P("您好，我是房产中介小王，城南新盘首付只要 5 万", true, "陌生推销。", "推销短信", level: 2),
            P("【三星】您的 Note7 需召回更换，请点击 sx-note7.cc 登记身份证和银行卡", true, "官方召回不会要银行卡，链接是假的。", "Note7 召回，2016-09", 20160903),
            P("【iPhone 7】首发预约资格已到账，点击 ip7-cn.cc 领取", true, "苹果不会发这种短信。", "iPhone 7 发售，2016-09", 20160908, topic: "iPhone 7"),
            P("【阴阳师】SSR 必出礼包已到账，回复 Y 领取", true, "回复就扣费的套路。", "《阴阳师》，2016-09", 20160902, topic: "阴阳师"),
            P("双 11 全场 1 折！点 tb1111.cc 抢购", true, "假淘宝链接。", "双 11，2016-11", 20161101, topic: "双11"),

            P("您的快递已到小区门口菜鸟驿站，取件码 3721", false, "正常的取件通知。", "快递通知"),
            P("周三下午的组会改到 4 点，记得带论文", false, "认识的人发的正事。", "日常短信"),
            P("妈：吃饭没？天热别老开着电脑", false, "妈发的。", "日常短信"),
            P("【中国移动】您本月流量已使用 80%", false, "运营商的正常提醒。", "运营商通知"),
            P("表姐：周末回家吃饭不？", false, "家里人。", "日常短信"),
            P("【12306】您已购 6 月 3 日 K1234 次车票，请按时乘车", false, "正常的购票通知。", "12306 通知"),
            P("【招商银行】您尾号 0529 的卡消费 45.00 元", false, "正常的消费提醒，没有让你点链接。", "银行通知"),
            P("老周：网吧那台机子修好了，有空来拿", false, "老周发的。", "日常短信"),
            P("【菜鸟驿站】您的双 11 包裹已到驿站，取件码 3-2-1106", false, "正常的取件通知。", "双 11 快递，2016-11", 20161112, topic: "双11"),
            P("【中国移动】充 100 送 20，回复 1 办理", false, "运营商自己的正规推广，不算垃圾短信。", "运营商推广", level: 2),

            P("我是你领导，明天上午来我办公室一趟，先把这个号存一下", true, "冒充领导，下一步就是借钱。", "冒充领导诈骗", trick: true),
            P("爸，我换号了，这是新号码。没钱了，先打 2000 到这张卡", true, "冒充子女要钱。", "冒充亲人诈骗", trick: true),
            P("【中国移动】尊敬的客户，您的积分即将清零，请登录 10086jf.cc 兑换", true, "网址不是 10086.cn，是钓鱼。", "钓鱼短信", trick: true),
            P("【电业局】您户本月电费 238.6 元，请按时到营业厅或网上缴纳", false, "正规缴费通知：没有可疑链接，没让你转账。", "电费通知", trick: true),
        };

        // ───────────── 标题党：这是标题党吗？ ─────────────

        static readonly XgPhrase[] Headline =
        {
            P("震惊！男子半夜去网吧，结果……", true, "「震惊」+ 省略号，吊胃口。", "UC 震惊部"),
            P("99% 的人都不知道，原来西瓜要这样切", true, "「99% 的人不知道」是套路。", "公众号标题"),
            P("看完这个视频，我哭了", true, "只说情绪，不说内容。", "公众号标题"),
            P("刚刚，朋友圈炸了！", true, "「刚刚」+「炸了」，什么都没说。", "公众号标题"),
            P("转疯了！这三种食物千万不能一起吃", true, "「转疯了」「千万不能」，吓人骗转发。", "养生谣言"),
            P("这个县城火了，原因竟然是……", true, "「竟然」+ 省略号。", "公众号标题"),
            P("不转不是中国人！", true, "道德绑架骗转发。", "朋友圈"),
            P("太可怕了！手机充电时千万别做这件事", true, "吓人 + 不说是什么事。", "公众号标题"),
            P("宋仲基竟然……看完我沉默了", true, "蹭明星 + 省略号。", "《太阳的后裔》热播", 20160224, topic: "太阳的后裔"),
            P("papi 酱一条广告卖了 2200 万，背后的真相令人深思", true, "「真相令人深思」是套路。", "papi 酱广告拍卖，2016-04-21", 20160421, topic: "papi酱"),
            P("葛优躺火了，原因竟然是……", true, "「竟然」+ 省略号。", "「葛优躺」，2016-07 走红", 20160725, topic: "葛优躺"),
            P("震惊！这个 App 让你的照片秒变名画", true, "「震惊！」+ 不说是哪个。", "Prisma 刷屏，2016-07", 20160711),
            P("傅园慧的洪荒之力，原来是这样来的……", true, "「原来」+ 省略号。", "里约奥运，2016-08", 20160809, topic: "洪荒之力"),
            P("iPhone 7 取消耳机孔，网友炸了", true, "「网友炸了」，只说情绪。", "iPhone 7 发布，2016-09-08", 20160908, topic: "iPhone 7"),
            P("蓝瘦香菇是什么梗？看完我笑出了声", true, "只说看完的情绪，不说内容。", "「蓝瘦香菇」，2016-10", 20161011, topic: "蓝瘦香菇"),
            P("你绝对想不到，今年双 11 的海报是谁做的", true, "「你绝对想不到」吊胃口。", "鲁班 AI，2016-11", 20161111, topic: "双11"),
            P("神秘棋手 Master 网上连胜，它到底是谁？", true, "用问句吊胃口，不给答案。", "Master，2016-12-29 起", 20161230),

            P("市气象台：明日最高气温 33℃，注意防暑", false, "直接说事。", "新闻"),
            P("AlphaGo 4:1 战胜李世石", false, "直接说结果。", "AlphaGo 人机大战，2016-03-15", 20160315, topic: "AlphaGo"),
            P("科比告别战砍下 60 分", false, "直接说结果。", "科比退役战，2016-04-14", 20160414, topic: "科比"),
            P("莱昂纳多凭《荒野猎人》获奥斯卡最佳男主角", false, "直接说结果。", "第 88 届奥斯卡，2016-02-29", 20160229),
            P("《太阳的后裔》今晚大结局", false, "平实的预告。", "《太阳的后裔》，2016-04-14 收官", 20160414, topic: "太阳的后裔"),
            P("支付宝集五福活动结束，敬业福最难集", false, "说清楚了事情。", "支付宝集五福，2016 年春节", 20160208, topic: "集五福"),
            P("县人民医院：6 月 1 日起门诊时间调整", false, "通知。", "新闻"),
            P("本市高考 6 月 7 日开考，考点公布", false, "通知。", "新闻"),
            P("暴雨预警：今晚到明天有大到暴雨", false, "预警，内容都在标题里。", "新闻"),
            P("Prisma 用神经网络把照片转成油画风格", false, "直接说事。", "Prisma，2016-07-11", 20160711),
            P("中国女排时隔 12 年再夺奥运冠军", false, "直接说结果。", "里约奥运，2016-08-21", 20160821, topic: "里约奥运"),
            P("天宫二号发射成功", false, "直接说事。", "天宫二号，2016-09-15", 20160915),
            P("鲍勃·迪伦获诺贝尔文学奖", false, "直接说结果。", "诺贝尔文学奖，2016-10-13", 20161013),
            P("双 11 全天成交 1207 亿元", false, "数字大，但只是陈述。", "双 11，2016-11-12", 20161112, topic: "双11"),

            P("震惊！一个围棋程序打败了世界冠军", true, "事情是真的，但「震惊！」的写法就是标题党。只看写法。", "AlphaGo，2016-03", 20160309, trick: true, topic: "AlphaGo"),
            P("震惊！县城网吧老板用旧显卡训练出人工智能", true, "「震惊！」开头，标题党。", "灵光", trick: true),
            P("霍金开通微博，24 小时粉丝破两百万", false, "数字惊人，但写法是平实的陈述。", "霍金开微博，2016-04-12", 20160412, trick: true),
            P("《美人鱼》票房突破 30 亿", false, "数字大，但只是陈述。", "《美人鱼》，2016 年春节档", 20160310, trick: true),
        };

        // ───────────── 刷单评论：这条评论是刷出来的吗？ ─────────────

        static readonly XgPhrase[] Review =
        {
            P("好评好评好评，老板人很好，下次还来", true, "重复、没有内容。", "淘宝刷单"),
            P("宝贝收到了，很好，五星", true, "模板句。", "淘宝刷单"),
            P("质量不错，物流很快，包装很好，很满意", true, "四连短句，哪件商品都能用。", "淘宝刷单"),
            P("非常好非常好非常好非常好", true, "复制粘贴。", "淘宝刷单"),
            P("掌柜服务态度超好，必须五星好评！！！给力！", true, "感叹号堆满，没说商品。", "淘宝刷单"),
            P("收到货了，物超所值，强烈推荐，亲们放心买", true, "像广告词。", "淘宝刷单"),
            P("好好好好好好好好好好好好", true, "凑字数。", "淘宝刷单"),
            P("和描述一致，好评！", true, "模板句。", "淘宝刷单", level: 2),

            P("显卡风扇有点响，玩 LOL 够用了，就是电费涨了", false, "有具体的用法和缺点，是真买家。", "真实评价"),
            P("比网吧那台快多了，但是驱动装了半天", false, "具体，有好有坏。", "真实评价"),
            P("快递三天才到，盒子压扁了，东西倒是没坏", false, "具体的经过。", "真实评价"),
            P("颜色比图片深一点，一洗就缩水，差评", false, "具体的毛病。", "真实评价"),
            P("买来给我爸用的，他说字太小看不清", false, "具体的使用场景。", "真实评价"),
            P("说是宋仲基同款，到手就是普通外套，算了", false, "真实的失望。", "《太阳的后裔》同款", 20160224, topic: "太阳的后裔"),
            P("风扇声音太大，晚上睡不着，退了", false, "具体的毛病。", "真实评价"),
            P("表姐推荐的，用了一个月，键盘 F 键有点松", false, "有时间、有细节。", "真实评价"),

            P("差评！发货太快了，我还没准备好钱包就到了", true, "假差评真好评，内容空，刷单常见套路。", "刷单套路", trick: true),
            P("好评！电源适配器坏了联系客服，第二天就换了新的", false, "开头是「好评！」，但后面有具体经过，是真买家。", "真实评价", trick: true),
            P("五星，因为老板答应返现五块", true, "好评返现买来的评价，也算刷的。", "好评返现", trick: true),
        };

        // ───────────── 中式英语：这句英文说得对吗？ ─────────────

        static readonly XgPhrase[] Chinglish =
        {
            P("no zuo no die", false, "中式英语：不作死就不会死。", "网络流行语，2013 年起"),
            P("you can you up, no can no BB", false, "中式英语：你行你上。", "网络流行语，2014 年起"),
            P("good good study, day day up", false, "中式英语：好好学习，天天向上。", "老笑话"),
            P("people mountain people sea", false, "中式英语：人山人海，应为 a sea of people。", "老笑话"),
            P("give you some color to see see", false, "中式英语：给你点颜色看看。", "老笑话"),
            P("I very like it", false, "应为 I like it very much。", "常见语法错误"),
            P("open the light", false, "开灯应为 turn on the light。", "常见搭配错误"),
            P("We two who and who?", false, "中式英语：咱俩谁跟谁。", "老笑话"),
            P("Add oil!", false, "「加油」的直译，2016 年英语里还不这么说。", "中式英语", level: 2),
            P("Slip carefully", false, "「小心地滑」译错了，应为 Caution: wet floor。", "公共标识", level: 2),
            P("「老司机」翻译成 old driver", false, "老司机是「老手」，应译 old hand 或 veteran。", "「老司机」，2016 年初走红", 20160101, level: 2, topic: "老司机"),
            P("「你行你上」翻译成 you can you up", false, "直译，英语里不这么说。", "网络流行语", level: 2),

            P("Excuse me, where is the toilet?", true, "正确。", "课本"),
            P("It's raining cats and dogs.", true, "地道习语：雨很大。", "英语习语"),
            P("How much is this graphics card?", true, "正确。", "日常英语"),
            P("I'm fine, thank you. And you?", true, "课本句子，没有错。", "课本"),
            P("Welcome to Beijing!", true, "正确。", "日常英语"),
            P("Hello, my name is LingGuang.", true, "正确。", "灵光"),

            P("How old are you?", true, "「怎么老是你」是网络笑话，但这句英文本身完全正确。", "网络笑话", trick: true),
            P("Long time no see.", true, "源自中式英语，但早就被英语接受了。", "英语外来说法", trick: true),
            P("lose face", true, "源自中文「丢脸」，英语词典早已收录。", "英语外来说法", trick: true),
            P("Good good study", false, "看着眼熟，但还是中式英语。", "老笑话", trick: true),
        };
    }
}
