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
        /// <summary>短信桌: the sender line (10086, 106 号段, a phone number…). Shown above the text; the answer never depends on it alone.</summary>
        public string from = "";
        public bool family;
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

        static XgPhrase B(XgPhrase phrase, string textEn, string whyEn)
        { phrase.textEn = textEn; phrase.whyEn = whyEn; return phrase; }

        /// <summary>An SMS with its sender.</summary>
        static XgPhrase M(string from, string text, bool yes, string why, string source = "", int since = 20150101, int level = 1, bool trick = false, string topic = "")
        { var p = P(text, yes, why, source, since, level, trick, topic); p.from = from; p.family = Array.IndexOf(new[] { "妈", "爸", "表姐", "表哥", "姐", "外婆", "奶奶" }, from) >= 0; return p; }

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
        /// When <paramref name="allowTemplates"/> is true, about a third of text desk picks are assembled from
        /// parameterized 2016 slot templates so hand-labelling does not repeat the same two dozen lines.
        /// </summary>
        public static XgPhrase Pick(string desk, Random r, int level, int today, string topic, bool includeFamily = true, bool allowTemplates = false)
        {
            var bank = Bank(desk);
            if (bank == null) return null;
            bool want = r.NextDouble() < .5;
            bool trick = level >= 3 && r.NextDouble() < .15;
            if (allowTemplates && desk != SenseDesk && desk != "translate" && r.NextDouble() < .35)
            {
                var templated = PickTemplated(desk, r, level, today, want, trick);
                if (templated != null) return templated;
            }
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var pool = new List<XgPhrase>();
                double total = 0;
                foreach (var p in bank)
                {
                    if (p.since > today || p.level > level || !includeFamily && p.family) continue;
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

        // ───────────── 参数化模板 + 2016 词槽组合（解决刷空题库后的重复感） ─────────────

        static T Choose<T>(Random r, T[] items) => items[r.Next(items.Length)];

        static (string text, int since) ChooseAvailable(Random r, (string text, int since)[] items, int today)
        {
            var available = new List<(string text, int since)>();
            foreach (var item in items) if (item.since <= today) available.Add(item);
            return available[r.Next(available.Count)];
        }

        static readonly string[] DanmuTargets = { "这波走位", "这段剪辑", "这个转场", "这段配音", "刚才那波操作", "这个踩点" };
        static readonly string[] DanmuPraises = { "太溜了 666666", "给跪了，up 主辛苦了", "看得鸡皮疙瘩都起来了", "可以的，这剪辑我服" };
        static readonly string[] DanmuGripes = { "真的辣眼睛，看不下去了", "尴尬癌都犯了，关了", "一言不合就插广告，退钱！" };
        static readonly string[] DanmuCheckins = { "空降 02:15，前排打卡", "前排卖瓜子矿泉水，打卡", "弹幕护体，第一！", "来了来了，前排占座" };
        static readonly string[] DanmuIronyBad = { "一集水了三十分钟回忆", "字幕把人名全打错了", "背景音乐吵得听不清人话" };
        static readonly string[] DanmuIronyGood = { "害我单曲循环了二十遍", "大半夜看得我泡了两桶面", "笑得我眼泪都止不住" };

        static readonly (string code, string name)[] SpamBanks = { ("95588", "工商银行"), ("95555", "招商银行"), ("95533", "建设银行"), ("95599", "农业银行") };
        static readonly string[] SpamFakeDomains = { "icbc-vip.cc", "cmb-jf88.cc", "ccb-hd.cc", "bank-2016.cc" };
        static readonly string[] SpamShops = { "天猫商城", "聚美优品", "京东商城", "苏宁易购", "唯品会" };
        static readonly string[] SpamCouriers = { "顺丰速运", "圆通速递", "中通快递", "韵达快运", "菜鸟驿站" };
        static readonly string[] SpamStations = { "小区门口菜鸟驿站", "南门丰巢快递柜", "星光网吧旁代收点", "3 号楼下超市货架" };

        static readonly string[] HeadlineOpeners = { "震惊！", "刚刚曝光！", "朋友圈传疯了！" };
        static readonly string[] HeadlineSubjects = { "小伙半夜在网吧连坐三天", "男子把旧电脑拆开清理灰尘", "姑娘连喝一星期可乐", "房东打开租客退租的房间", "大学生花三百块淘了张旧显卡" };
        static readonly string[] HeadlineHooks = { "结果所有人沉默了……", "打开一看彻底惊呆了！", "背后的原因99%的人都不知道", "看完这一幕我哭了", "最后一个动作让人不敢相信！" };
        static readonly string[] HeadlinePlainEvents =
        {
            "市气象台：明日午后局部有雷阵雨，请注意携带雨具",
            "市交通委：3 路公交车末班延时至 22:30，方便市民夜间出行",
            "供电局：周四上午 9:00 至 12:00 城南片区线路检修停电通知",
            "市交通委：学校周边路段早晚高峰实行临时交通管制",
            "市市政公用局：老旧小区水表改造工程将于本周六正式启动",
        };
        static readonly (string text, int since)[] HeadlineTrickYes =
        {
            ("震惊！围棋世界冠军连输三盘，对手的真实身份竟然是……", 20160312),
            ("千万别在夜里十一点后去星光网吧！去过的人竟然都……", 20150101),
            ("男子花三百块买二手显卡，插上电脑那一刻全家沉默了！", 20150101),
        };
        static readonly (string text, int since)[] HeadlineTrickNo =
        {
            ("2016 年全国高考报名人数达 940 万，考试于 6 月 7 日上午九点开始", 20160606),
            ("千万别忘带准考证！市招办 6 月 1 日发布 2016 高考考前提醒", 20160601),
            ("震惊世界的引力波是怎么测出来的？LIGO 实验原理图解", 20160211),
            ("千万别忘带证件！铁路部门提醒乘客凭有效身份证件进站乘车", 20150101),
        };

        static readonly string[] ReviewSpamOpens = { "好评好评好评", "宝贝收到了，非常满意", "质量不错，物流很快，包装很好", "掌柜服务态度超好，必须五星好评" };
        static readonly string[] ReviewSpamMids = { "跟描述的一模一样", "打开包装很有档次", "比实体店便宜太多了" };
        static readonly string[] ReviewSpamTails = { "老板人很好，下次还来！", "物超所值，强烈推荐，亲们放心买！", "非常好非常好，全五分给力！！！" };
        // Keep each product's usage and faults together; a lamp cannot run a game or have a mouse switch.
        static readonly (string intro, (string text, int since)[] pros, string[] cons)[] ReviewRealProducts =
        {
            ("淘的二手显卡用了两天，",
                new[] { ("玩守望先锋帧数够用", 20160524), ("插上电脑一次点亮", 20150101), ("包装盒塞了泡沫没压坏", 20150101) },
                new[] { "就是风扇转起来有点响", "不过快递走了四天才到", "就是挡板有一道小划痕，凑合用吧" }),
            ("给出租屋买的台灯到了，",
                new[] { ("看书时亮度够用", 20150101), ("灯臂角度能调，摆桌角正好", 20150101), ("包装盒塞了泡沫没压坏", 20150101) },
                new[] { "就是底座有点轻，碰一下会晃", "不过快递走了四天才到", "就是外壳有一道小划痕，凑合用吧" }),
            ("星光网吧同款鼠标收到了，",
                new[] { ("微动按起来手感挺脆", 20150101), ("握着不顶手，滚轮刻度清楚", 20150101), ("包装盒塞了泡沫没压坏", 20150101) },
                new[] { "但是线材稍微有点硬，习惯性好评", "不过快递走了四天才到", "就是外壳有一道小划痕，凑合用吧" }),
            ("替室友带的机械键盘试了下，",
                new[] { ("按键回弹挺利落，打字没漏字", 20150101), ("键帽字符印得清楚", 20150101), ("包装盒塞了泡沫没压坏", 20150101) },
                new[] { "就是空格键声音有点大", "但是线材稍微有点硬，习惯性好评", "不过快递走了四天才到" }),
        };
        static readonly string[] ReviewTrickYes =
        {
            "差评！发货太快了，害我机箱还没到显卡就先到了，必须给老板五星好评！",
            "本来想给差评的，结果打开一看质量好得离谱，物超所值，强烈推荐购买！",
            "在星光网吧用了三年电脑，都没见过这么好用的宝贝，掌柜人超好，全五分好评！",
        };
        static readonly string[] ReviewTrickNo =
        {
            "好评！二手电源刚插上有点啸叫，找客服换了一根供电线就好了，带 980Ti 很稳",
            "冲着掌柜说的「物超所值」买的，结果风扇轴承是歪的，退货还要我自己垫运费",
            "默认五星好评免得卖家一直打电话骚扰，其实硅脂都干透了，大家自己避坑",
        };

        // Translate the already-selected slots, so both languages retain the same product, amount and date.
        static readonly (string zh, string en)[] TemplateTranslations = BuildTemplateTranslations();

        static (string zh, string en)[] BuildTemplateTranslations()
        {
            var slots = new[]
            {
                ("这波走位", "That movement"), ("这段剪辑", "This editing"), ("这个转场", "This transition"),
                ("这段配音", "This voice-over"), ("刚才那波操作", "That last play"), ("这个踩点", "This beat timing"),
                ("太溜了 666666", "is so slick, 666666"), ("给跪了，up 主辛苦了", "has me bowing down; thanks for the hard work, uploader"),
                ("看得鸡皮疙瘩都起来了", "gave me goosebumps"), ("可以的，这剪辑我服", "is great; respect for the editing"),
                ("真的辣眼睛，看不下去了", "is painful to watch; I cannot keep watching"),
                ("尴尬癌都犯了，关了", "makes me cringe; closing it"), ("一言不合就插广告，退钱！", "keeps inserting ads; refund me!"),
                ("空降 02:15，前排打卡", "Skipping to 02:15, checking in up front"),
                ("前排卖瓜子矿泉水，打卡", "Seeds and water for sale in the front row; checking in"),
                ("弹幕护体，第一！", "Comment shield up, first!"), ("来了来了，前排占座", "Here I am, saving a front-row seat"),
                ("一集水了三十分钟回忆", "thirty minutes of flashback filler in one episode"),
                ("字幕把人名全打错了", "the subtitles got every name wrong"), ("背景音乐吵得听不清人话", "the music drowns out the dialogue"),
                ("害我单曲循环了二十遍", "you made me replay it twenty times"), ("大半夜看得我泡了两桶面", "watching this at midnight made me cook two cups of noodles"),
                ("笑得我眼泪都止不住", "I laughed until I could not stop crying"),
                ("这 up 主有毒，", "This uploader is addictive: "), ("真棒，", "Just great: "), ("，感谢 up 主替我省时间", "; thanks for saving me time, uploader"),
                ("工商银行", "ICBC"), ("招商银行", "China Merchants Bank"), ("建设银行", "China Construction Bank"), ("农业银行", "Agricultural Bank of China"),
                ("天猫商城", "Tmall"), ("聚美优品", "Jumei"), ("京东商城", "JD"), ("苏宁易购", "Suning"), ("唯品会", "Vipshop"),
                ("顺丰速运", "SF Express"), ("圆通速递", "YTO Express"), ("中通快递", "ZTO Express"), ("韵达快运", "Yunda Express"),
                ("小区门口菜鸟驿站", "the Cainiao pickup point at the estate gate"), ("南门丰巢快递柜", "the Hive Box locker at the south gate"),
                ("星光网吧旁代收点", "the pickup point beside Starlight Internet Cafe"), ("3 号楼下超市货架", "the supermarket shelf below Building 3"), ("菜鸟驿站", "Cainiao Station"),
                ("我是房东，旧卡不用了，这个月房租 1350 元直接打到新卡 6222 **** ", "This is your landlord. My old card is no longer in use. Transfer this month's 1350 yuan rent to my new card 6222 **** "),
                ("供电局", "Power company"), ("您户本月电费 ", "Your electricity bill this month is "), (" 元，请按时到营业厅或网上缴纳，无需回复。", " yuan. Pay on time at a service counter or online. No reply needed."),
                ("尊敬的客户，您尾号 ", "Dear customer, your account ending "), (" 积分满 ", " has points totaling "), (" 可兑 ", " redeemable for "), (" 元现金，请登录 ", " yuan cash. Visit "), (" 领取", " to claim"),
                ("会员专享满 199 减 100 优惠券明日到期，戳 t.cn/", "Members: your 100-yuan-off-199 coupon expires tomorrow. Shop at t.cn/"), (" 抢购，退订回 TD", "; reply TD to unsubscribe"),
                ("您的包裹已到", "Your parcel is at "), ("，取件码 ", ". Pickup code: "), ("您尾号 ", "Your savings card ending "), (" 的储蓄卡消费 ", " was charged "), (" 元，余额 ", " yuan; balance "), (" 元。", " yuan."),
                ("震惊！", "Shocking! "), ("刚刚曝光！", "Just revealed! "), ("朋友圈传疯了！", "Going viral in Moments! "),
                ("小伙半夜在网吧连坐三天", "A young man spends three straight nights in an internet cafe"),
                ("男子把旧电脑拆开清理灰尘", "A man opens an old computer to remove the dust"), ("姑娘连喝一星期可乐", "A woman drinks cola for a week"),
                ("房东打开租客退租的房间", "A landlord opens the room after a tenant moves out"), ("大学生花三百块淘了张旧显卡", "A student buys an old graphics card for 300 yuan"),
                ("结果所有人沉默了……", "and the result leaves everyone speechless..."), ("打开一看彻底惊呆了！", "what was inside left everyone stunned!"),
                ("背后的原因99%的人都不知道", "99% of people do not know the reason"), ("看完这一幕我哭了", "this scene made me cry"), ("最后一个动作让人不敢相信！", "the final move is unbelievable!"),
                ("市气象台：明日午后局部有雷阵雨，请注意携带雨具", "City weather office: scattered thunderstorms tomorrow afternoon; bring rain gear"),
                ("市交通委：3 路公交车末班延时至 22:30，方便市民夜间出行", "City transport office: the last Route 3 bus now leaves at 22:30 for evening travelers"),
                ("供电局：周四上午 9:00 至 12:00 城南片区线路检修停电通知", "Power company: maintenance outage in the south district on Thursday, 9:00-12:00"),
                ("市交通委：学校周边路段早晚高峰实行临时交通管制", "City transport office: temporary traffic controls near schools during morning and evening rush hours"),
                ("市市政公用局：老旧小区水表改造工程将于本周六正式启动", "City utilities office: water-meter upgrades in older estates begin this Saturday"),
                ("震惊！围棋世界冠军连输三盘，对手的真实身份竟然是……", "Shocking! World Go champion loses three straight games; the opponent's real identity is..."),
                ("千万别在夜里十一点后去星光网吧！去过的人竟然都……", "Never visit Starlight Internet Cafe after 11 p.m.! Everyone who went there..."),
                ("男子花三百块买二手显卡，插上电脑那一刻全家沉默了！", "A man buys a used graphics card for 300 yuan; plugging it in leaves the family speechless!"),
                ("2016 年全国高考报名人数达 940 万，考试于 6 月 7 日上午九点开始", "China's 2016 college entrance exam has 9.4 million applicants; exams start at 9 a.m. on June 7"),
                ("千万别忘带准考证！市招办 6 月 1 日发布 2016 高考考前提醒", "Do not forget your admission ticket! City admissions office issues its 2016 exam reminder on June 1"),
                ("震惊世界的引力波是怎么测出来的？LIGO 实验原理图解", "How were the gravitational waves that stunned the world detected? An illustrated guide to the LIGO experiment"),
                ("千万别忘带证件！铁路部门提醒乘客凭有效身份证件进站乘车", "Do not forget your ID! Railway officials remind passengers to enter and travel with valid identification"),
                ("好评好评好评", "Great great great"), ("宝贝收到了，非常满意", "Item received, very satisfied"), ("质量不错，物流很快，包装很好", "Good quality, fast shipping, great packaging"),
                ("掌柜服务态度超好，必须五星好评", "The seller is so nice; must give five stars"), ("跟描述的一模一样", "exactly as described"), ("打开包装很有档次", "looks classy out of the box"), ("比实体店便宜太多了", "much cheaper than physical shops"),
                ("老板人很好，下次还来！", "nice seller, will buy again!"), ("物超所值，强烈推荐，亲们放心买！", "great value, highly recommended, buy with confidence!"), ("非常好非常好，全五分给力！！！", "very good, very good, full marks!!!"),
                ("淘的二手显卡用了两天，", "I used the secondhand graphics card for two days: "), ("给出租屋买的台灯到了，", "The desk lamp for my rented room arrived: "),
                ("星光网吧同款鼠标收到了，", "The mouse matching Starlight Internet Cafe's arrived: "), ("替室友带的机械键盘试了下，", "I tried the mechanical keyboard I bought for my roommate: "),
                ("玩守望先锋帧数够用", "the frame rate is enough for Overwatch"), ("插上电脑一次点亮", "the computer booted on the first try"), ("包装盒塞了泡沫没压坏", "foam in the box protected it from damage"),
                ("就是风扇转起来有点响", "but the fan is a bit loud"), ("不过快递走了四天才到", "but delivery took four days"), ("就是挡板有一道小划痕，凑合用吧", "but the bracket has a small scratch; it will do"),
                ("看书时亮度够用", "it is bright enough for reading"), ("灯臂角度能调，摆桌角正好", "the adjustable arm fits the corner of my desk"), ("就是底座有点轻，碰一下会晃", "but the light base wobbles when touched"),
                ("就是外壳有一道小划痕，凑合用吧", "but the shell has a small scratch; it will do"), ("微动按起来手感挺脆", "the microswitch clicks feel crisp"), ("握着不顶手，滚轮刻度清楚", "the grip is comfortable and the wheel steps are distinct"),
                ("但是线材稍微有点硬，习惯性好评", "but the cable is a little stiff; giving a positive review out of habit"), ("按键回弹挺利落，打字没漏字", "the keys spring back cleanly without missed keystrokes"), ("键帽字符印得清楚", "the keycap lettering is clear"), ("就是空格键声音有点大", "but the space bar is a little loud"),
                ("差评！发货太快了，害我机箱还没到显卡就先到了，必须给老板五星好评！", "Bad review! Shipping was so fast the graphics card beat my case here; I must give the seller five stars!"),
                ("本来想给差评的，结果打开一看质量好得离谱，物超所值，强烈推荐购买！", "I meant to leave a bad review, but the quality was amazingly good when I opened it. Great value, highly recommended!"),
                ("在星光网吧用了三年电脑，都没见过这么好用的宝贝，掌柜人超好，全五分好评！", "Three years using computers at Starlight Internet Cafe and I have never seen such a great item. Lovely seller, five stars!"),
                ("好评！二手电源刚插上有点啸叫，找客服换了一根供电线就好了，带 980Ti 很稳", "Positive review! The used power supply whined at first; customer service replaced a power cable and it stopped. Stable with a 980Ti"),
                ("冲着掌柜说的「物超所值」买的，结果风扇轴承是歪的，退货还要我自己垫运费", "Bought it for the seller's 'great value' claim, but the fan bearing was crooked and I had to advance the return postage"),
                ("默认五星好评免得卖家一直打电话骚扰，其实硅脂都干透了，大家自己避坑", "Defaulting to five stars to stop the seller's calls, but the thermal paste was completely dry. Buyer beware"),
                ("嘴上说「有毒」，其实是夸内容吸引人。", "Calling it addictive praises how engaging it is."),
                ("反话：「真棒」「感谢」是在讽刺视频注水粗糙。", "Sarcasm: 'great' and 'thanks' criticize filler and poor production."),
                ("直接夸视频操作或制作水平。", "Direct praise for the play or production quality."), ("只是签到占座或报时间点，没有在夸。", "Only checking in, claiming a seat or giving a timestamp; not praise."), ("直接吐槽视频难看或广告烦。", "Direct criticism of an unpleasant video or annoying ads."),
                ("陌生号码冒充房东说换卡号要转账，经典诈片。", "An unknown number claims to be the landlord and asks for a transfer to a new card: a classic scam."),
                ("正规电费通知，无钓鱼链接，也没让你向私人卡号转账。", "A routine electricity bill with no phishing link or transfer to a private card."),
                ("伪基站冒充银行发陌生 .cc 链接领现金，是钓鱼短信。", "A spoofed bank message offering cash at an unfamiliar .cc link is phishing."),
                ("群发电商促销短信，末尾带「退订回 TD」，算垃圾短信。", "A mass retail promotion with 'reply TD to unsubscribe' counts as spam."),
                ("正常的快递取件码通知。", "A routine parcel pickup notification."), ("正常的银行消费提醒，没有链接也不索要验证码。", "A routine bank transaction alert, with no link or request for verification codes."),
                ("虽然借了真实事件或日常场景，但用「震惊！」「竟然……」留悬念吊胃口，属于标题党。", "It uses a real event or everyday scene but withholds the reveal behind sensational wording: clickbait."),
                ("虽然带有感叹词或大数字，但把核心事实与结论直接交代清楚，不是标题党。", "Despite an exclamation or a large number, it states the key facts directly: not clickbait."),
                ("用夸张开头加悬念省略号，只吊胃口不交代事实，是标题党。", "A sensational opening and an unfinished reveal tease the reader instead of giving facts: clickbait."),
                ("直接把通知内容说清楚，不是标题党。", "It states the notice directly: not clickbait."),
                ("表面欲扬先抑或套近乎，实则全是空洞套话，是刷单套路。", "A fake complaint or friendly tone disguises empty promotional praise: a fake-review pattern."),
                ("虽然表面带「好评」或「物超所值」，但写明了具体故障与真实体验，是真买家。", "Despite 'positive review' or 'great value', it describes specific faults and experience: a real buyer."),
                ("套话堆砌、哪件商品都能复制粘贴，是刷单评论。", "Generic praise that could be pasted onto any product: a fake review."),
                ("有具体的使用场景和优缺点细节，是真买家。", "Specific usage, strengths and drawbacks indicate a real buyer."),
            };
            Array.Sort(slots, (a, b) => b.Item1.Length.CompareTo(a.Item1.Length));
            return slots;
        }

        static string TemplateEnglish(string text)
        {
            foreach (var slot in TemplateTranslations) text = text.Replace(slot.zh, slot.en);
            return text.Replace("【", "[").Replace("】", "] ").Replace("，", ", ").Replace("。", ".");
        }

        /// <summary>Assembles a fresh 2016 card from slot templates while keeping core vocabulary bigrams aligned with the board.</summary>
        public static XgPhrase PickTemplated(string desk, Random r, int level, int today, bool want, bool trick)
        {
            var phrase = PickTemplateChinese(desk, r, level, today, want, trick);
            if (phrase != null) B(phrase, TemplateEnglish(phrase.text), TemplateEnglish(phrase.why));
            return phrase;
        }

        static XgPhrase PickTemplateChinese(string desk, Random r, int level, int today, bool want, bool trick)
        {
            if (today < 20150101) return null;
            switch (desk)
            {
                case "danmu":
                    if (trick)
                    {
                        return want
                            ? P("这 up 主有毒，" + Choose(r, DanmuIronyGood), true, "嘴上说「有毒」，其实是夸内容吸引人。", "B 站弹幕", trick: true)
                            : P("真棒，" + Choose(r, DanmuIronyBad) + "，感谢 up 主替我省时间", false, "反话：「真棒」「感谢」是在讽刺视频注水粗糙。", "反讽弹幕", trick: true);
                    }
                    if (want)
                        return P(Choose(r, DanmuTargets) + "，" + Choose(r, DanmuPraises), true, "直接夸视频操作或制作水平。", "B 站弹幕", level: Math.Min(2, level));
                    if (level >= 2 && r.Next(2) == 0)
                        return P(Choose(r, DanmuCheckins), false, "只是签到占座或报时间点，没有在夸。", "B 站弹幕", level: 2);
                    return P(Choose(r, DanmuTargets) + "，" + Choose(r, DanmuGripes), false, "直接吐槽视频难看或广告烦。", "B 站弹幕");

                case "spam":
                {
                    var bank = Choose(r, SpamBanks);
                    int tail = 1000 + r.Next(9000);
                    string code4 = (1000 + r.Next(9000)).ToString();
                    if (trick)
                    {
                        if (want)
                        {
                            int cardTail = 1000 + r.Next(9000);
                            return M("138 0000 " + code4, "我是房东，旧卡不用了，这个月房租 1350 元直接打到新卡 6222 **** " + cardTail, true, "陌生号码冒充房东说换卡号要转账，经典诈片。", "冒充房东诈片", trick: true);
                        }
                        int bill = 120 + r.Next(180);
                        return M("95598", "【供电局】您户本月电费 " + bill + ".40 元，请按时到营业厅或网上缴纳，无需回复。", false, "正规电费通知，无钓鱼链接，也没让你向私人卡号转账。", "电费通知", trick: true);
                    }
                    if (want)
                    {
                        if (r.Next(2) == 0)
                        {
                            int pts = (3 + r.Next(7)) * 1000;
                            int cash = pts / 10;
                            return M(bank.code, "【" + bank.name + "】尊敬的客户，您尾号 " + tail + " 积分满 " + pts + " 可兑 " + cash + " 元现金，请登录 " + Choose(r, SpamFakeDomains) + " 领取", true, "伪基站冒充银行发陌生 .cc 链接领现金，是钓鱼短信。", "伪基站钓鱼");
                        }
                        string shop = Choose(r, SpamShops);
                        return M("1069 " + code4 + " 0524", "【" + shop + "】会员专享满 199 减 100 优惠券明日到期，戳 t.cn/" + code4 + " 抢购，退订回 TD", true, "群发电商促销短信，末尾带「退订回 TD」，算垃圾短信。", "电商营销短信", level: Math.Min(2, level));
                    }
                    if (r.Next(2) == 0)
                    {
                        string courier = Choose(r, SpamCouriers);
                        string station = Choose(r, SpamStations);
                        int shelf = 1 + r.Next(9), box = 1000 + r.Next(9000);
                        return M("1065 " + code4 + " 1000", "【" + courier + "】您的包裹已到" + station + "，取件码 " + shelf + "-" + box + "。", false, "正常的快递取件码通知。", "快递通知");
                    }
                    int spent = 15 + r.Next(150), bal = 300 + r.Next(2000);
                    return M(bank.code, "【" + bank.name + "】您尾号 " + tail + " 的储蓄卡消费 " + spent + ".00 元，余额 " + bal + ".50 元。", false, "正常的银行消费提醒，没有链接也不索要验证码。", "银行通知");
                }

                case "headline":
                    if (trick)
                    {
                        var headline = ChooseAvailable(r, want ? HeadlineTrickYes : HeadlineTrickNo, today);
                        return want
                            ? P(headline.text, true, "虽然借了真实事件或日常场景，但用「震惊！」「竟然……」留悬念吊胃口，属于标题党。", "UC 震惊部", since: headline.since, trick: true)
                            : P(headline.text, false, "虽然带有感叹词或大数字，但把核心事实与结论直接交代清楚，不是标题党。", "新闻", since: headline.since, trick: true);
                    }
                    if (want)
                        return P(Choose(r, HeadlineOpeners) + Choose(r, HeadlineSubjects) + "，" + Choose(r, HeadlineHooks), true, "用夸张开头加悬念省略号，只吊胃口不交代事实，是标题党。", "公众号标题");
                    return P(Choose(r, HeadlinePlainEvents), false, "直接把通知内容说清楚，不是标题党。", "新闻");

                case "review":
                    if (trick)
                    {
                        return want
                            ? P(Choose(r, ReviewTrickYes), true, "表面欲扬先抑或套近乎，实则全是空洞套话，是刷单套路。", "刷单套路", trick: true)
                            : P(Choose(r, ReviewTrickNo), false, "虽然表面带「好评」或「物超所值」，但写明了具体故障与真实体验，是真买家。", "真实评价", trick: true);
                    }
                    if (want)
                        return P(Choose(r, ReviewSpamOpens) + "，" + Choose(r, ReviewSpamMids) + "，" + Choose(r, ReviewSpamTails), true, "套话堆砌、哪件商品都能复制粘贴，是刷单评论。", "淘宝刷单");
                    var product = Choose(r, ReviewRealProducts);
                    var pro = ChooseAvailable(r, product.pros, today);
                    return P(product.intro + pro.text + "，" + Choose(r, product.cons), false, "有具体的使用场景和优缺点细节，是真买家。", "真实评价", since: pro.since);

                default:
                    return null;
            }
        }

        // ───────────── 弹幕情绪：这条弹幕是在夸吗？ ─────────────

        static readonly XgPhrase[] Danmu =
        {
            P("666666", true, "「6」=「溜」，夸操作厉害。", "游戏直播弹幕"),
            P("给跪了", true, "佩服到跪下，是夸。", "网络用语"),
            P("请收下我的膝盖", true, "佩服得五体投地，是夸。", "B 站弹幕"),
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
            P("已举报，整段视频都是搬运抄袭的", false, "举报抄袭，是骂。", "B 站弹幕"),
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

            // 字面规则 vs 语义反转（老司机题）
            P("真棒，一集水了四十分钟", false, "反话：「真棒」是讽刺注水。", "反讽弹幕", trick: true),
            P("好厉害哦，字幕全是错别字", false, "反话：嫌字幕差。", "反讽弹幕", trick: true),
            P("感谢 up 主，又浪费了我十分钟", false, "反话：「感谢」是在骂浪费时间。", "反讽弹幕", trick: true),
            P("膝盖都坐麻了还没进入正题，散了散了", false, "虽然有「膝盖」，但不是「收下膝盖」，而是嫌拖沓。", "B 站弹幕", trick: true),
            P("这 up 主有毒，我循环了十遍", true, "「有毒」= 停不下来，是夸。", "网络用语", trick: true),
            P("吓死我了，再来一遍", true, "嘴上喊怕，其实爱看，是夸。", "B 站弹幕", trick: true),
            P("丧心病狂，但我喜欢", true, "看着像骂，结尾说喜欢，是夸。", "B 站弹幕", trick: true),
            P("举报了，大半夜放烤串视频，馋得我泡了两桶面", true, "嘴上说「举报」，其实是夸美食视频拍得太诱人。", "深夜放毒弹幕", trick: true),
            P("up 主赔钱！害我笑着笑着看哭了，眼泪止不住", true, "嘴上喊「赔钱」，其实是夸视频感人。", "B 站弹幕", trick: true),
        };

        // ───────────── 垃圾短信：这条是垃圾短信吗？ ─────────────

        static readonly XgPhrase[] Spam =
        {
            P("【我是歌手】恭喜您被栏目组抽中为场外幸运观众，获得奖金 98000 元，请登录 wsgs-hd.cc 领取", true, "冒充综艺节目中奖，经典诈片。", "冒充综艺中奖诈片，2015—2016 年常见"),
            P("老板您好，本公司可代开各类发票，联系 138****2016", true, "代开发票是违法广告。", "代开发票短信"),
            P("亲，一手货源正品代购，加微信 dg2016 了解一下", true, "群发的微商广告。", "微商刷屏，2015—2016"),
            P("您的银行卡积分可兑换 368 元现金，回复 Y 领取", true, "积分兑现金 + 让你回复，是钓鱼。", "积分兑换诈片"),
            P("澳门首家线上堵场上线啦，注册即送 88 元", true, "堵博广告。", "堵博广告短信"),
            P("低价出售 iPhone 6s，全新未拆封，只要 1888", true, "价格离谱的群发广告。", "手机诈片广告"),
            P("【集五福】恭喜获得敬业福！点击 jwf2016.cc 领取 2016 元现金", true, "冒充支付宝集五福，链接是假的。", "支付宝集五福，2016 年春节", 20160128, topic: "集五福"),
            P("papi 酱同款面膜今日特价，戳链接抢购", true, "蹭热点的群发广告。", "papi 酱走红，2016 年初", 20160301, topic: "papi酱"),
            P("宋仲基同款军装外套 5 折包邮，仅限今天", true, "蹭剧的群发广告。", "《太阳的后裔》，2016-02-24 开播", 20160224, topic: "太阳的后裔"),
            P("您好，我是房产中介小王，城南新盘首付只要 5 万", true, "陌生推销。", "推销短信", level: 2),
            P("【三星】您的 Note7 需召回更换，请点击 sx-note7.cc 登记身份证和银行卡", true, "官方召回不会要银行卡，链接是假的。", "Note7 召回，2016-09", 20160903),
            P("【iPhone 7】首发预约资格已到账，点击 ip7-cn.cc 领取", true, "苹果不会发这种短信。", "iPhone 7 发售，2016-09", 20160908, topic: "iPhone 7"),
            P("【阴阳师】SSR 必出礼包已到账，回复 Y 领取", true, "回复就扣费的套路。", "《阴阳师》，2016-09", 20160902, topic: "阴阳师"),
            P("双 11 全场 1 折！点 tb1111.cc 抢购", true, "假淘宝链接。", "双 11，2016-11", 20161101, topic: "双11"),

            P("您的快递已到小区门口菜鸟驿站，取件码 3721", false, "正常的取件通知。", "快递通知"),
            P("周三下午的面试改到 4 点，记得带简历和证件", false, "认识的人发的正事。", "日常短信"),
            M("妈", "吃饭没？天热别老开着电脑", false, "妈发的。", "日常短信"),
            P("【中国移动】您本月流量已使用 80%", false, "运营商的正常提醒。", "运营商通知"),
            M("表姐", "周末来我这吃饭不？", false, "家里人。", "日常短信"),
            P("【12306】您已购 6 月 3 日 K1234 次车票，请按时乘车", false, "正常的购票通知。", "12306 通知"),
            P("【招商银行】您尾号 0524 的卡消费 45.00 元", false, "正常的消费提醒，没有让你点链接。", "银行通知"),
            M("阿杰", "网吧换下来一台旧机子，你要不要？有空来拿", false, "阿杰发的。", "日常短信"),
            M("房东", "小伙子，水电费单贴你门上了，月底跟房租一起给就行", false, "房东发的日常水电通知，没让你给陌生卡号打钱。", "日常短信"),
            P("【菜鸟驿站】您的双 11 包裹已到驿站，取件码 3-2-1106", false, "正常的取件通知。", "双 11 快递，2016-11", 20161112, topic: "双11"),
            P("【中国移动】充 100 送 20，回复 1 办理", true, "运营商群发的充值促销推销，也算垃圾短信。", "运营商营销", level: 2),

            // 字面规则 vs 语义反转（老司机题）
            P("我是你领导，明天上午来我办公室一趟，先把这个号存一下", true, "冒充领导，下一步就是借钱。", "冒充领导诈片", trick: true),
            P("爸，我换号了，这是新号码。没钱了，先打 2000 到这张卡", true, "冒充子女要钱。", "冒充亲人诈片", trick: true),
            P("【中国移动】尊敬的客户，您的积分即将清零，请登录 10086jf.cc 兑换", true, "网址不是 10086.cn，是钓鱼。", "钓鱼短信", trick: true),
            P("【电业局】您户本月电费 238.6 元，请按时到营业厅或网上缴纳", false, "正规缴费通知：没有可疑链接，没让你转账。", "电费通知", trick: true),
            M("139 0000 4399", "我是房东，原来的卡注销了，这个月房租现在直接转到新卡 6228 **** 9981", true, "经典「我是房东换卡号」群发诈片，陌生号码直接让你转账。", "冒充房东诈片，2016 年高发", trick: true),
            M("1069 8800 1234", "【顺丰速运】您有一份违禁包裹被扣留，请立即登录 sf-check.cc 配合清查", true, "顺丰不会发陌生链接让你清查违禁包裹，是冒充快递钓鱼。", "快递诈片", trick: true),

            // 带发件人的短信：先看号码，再看内容。号码只是线索，内容才定输赢。
            M("10086", "尊敬的客户，您 5 月话费 58.30 元，账户余额 12.70 元。", false, "10086 的月结提醒，没有链接，没让你回复。", "运营商通知"),
            M("10086", "您已成功订购 10 元 300M 夜间流量包，23:00-7:00 有效。", false, "你自己发短信订的，营业厅给的回执。", "运营商通知"),
            M("10086", "【和包】您本次验证码 362814，5 分钟内有效，请勿告诉他人。", false, "验证码短信，而且提醒你别告诉别人。正常。", "运营商通知"),
            M("10086", "温馨提示：您的套餐内流量已用完，超出部分按 0.29 元/M 收费。", false, "正常的超额提醒，看着心疼，但不是垃圾短信。", "运营商通知"),
            M("1065 8888 1234", "【某某贷】凭身份证 3 分钟放款 5 万，无抵押！退订回 T", true, "群发贷款广告，「退订回 T」是营销短信的标配。", "106 号段营销短信"),
            M("1069 0123 4567", "【XX 商城】618 年中大促，全场满 199 减 100，戳 m.xxsc.cn，退订回 TD", true, "群发广告，后面挂着「退订回 TD」。", "618 年中大促，2016-06", 20160601),
            M("1069 5555 0001", "【聚美】亲爱的会员，你的 30 元无门槛券明天过期，回 TD 退订", true, "催你下单的营销短信。", "电商营销短信"),
            M("1065 0000 7788", "【驾校】暑假学车特惠 2980 元包拿证，学生凭证再减 200，退订回 T", true, "暑假群发的招生广告。", "暑期招生广告", 20160701),
            M("1069 2016 0911", "【XX 教育】开学季英语四六级包过班，报名立减 500，退订回 TD", true, "开学季群发广告。", "开学季广告", 20160901),
            M("1065 1111 1111", "【天猫】双 11 预售开始，定金 50 抵 150，速抢！退订回 TD", true, "双 11 前的营销短信。不是诈片，但算垃圾短信。", "双 11 预热，2016-10", 20161020, topic: "双11"),
            M("1069 1111 2016", "【XX 电器】双 11 返场，冰箱彩电低至 3 折，仅剩今天！退订回 T", true, "双 11 之后的返场广告，照样是群发。", "双 11 返场，2016-11", 20161112, topic: "双11"),
            M("1065 8000 4321", "【XX 健身】私教体验课 9.9 元，戳 t.cn/xxx 抢名额，退订回 TD", true, "群发广告。", "营销短信", level: 2),

            M("10086", "【中国移动】您的手机积分已满 5000 分，可兑换 450 元现金，请登录 10086cy.cc 领取。", true, "10086 不送现金，网址也不是 10086.cn。号码可以伪基站伪造。", "伪基站积分诈片，2016 年高发", trick: true),
            M("95588", "【工商银行】您的电子密码器于今日失效，请登录 www.icbcxv.cc 进行升级，逾期账户停用。", true, "银行不会让你点陌生网址升级密码器。伪基站冒充的银行号码。", "伪基站钓鱼短信", trick: true),
            M("106 9000 3311", "【三星】Note7 召回登记：请回复姓名、身份证号和银行卡号，退款 5988 元直接到账。", true, "正规召回去门店换机，不要你的卡号。", "Note7 召回，2016-09", 20160903),
            M("1065 7000 7777", "【三星服务】Note7 用户请尽快到店更换新机，详询 400 热线或附近服务中心。", false, "让你去服务中心，不要钱，不要卡号。正常通知。", "Note7 召回，2016-09", 20160903, level: 2),
            M("1065 0920 0000", "【Note7 全球召回】您的手机存在爆炸风险，点击 note7-zh.cc 填写银行卡领 1000 元补偿", true, "补偿要你填卡号，就是钓鱼。", "Note7 第二次召回，2016-10", 20161011),
            M("1069 1115 0001", "【淘宝】您双 11 购买的商品因库存不足需退款，请点击 tb-tk.cc 填写银行卡办理。", true, "淘宝退款原路退回，不用你填卡号。双 11 后最常见的骗局。", "双 11 退款诈片，2016-11", 20161115, topic: "双11"),
            M("1069 1115 0002", "【快递】您的双 11 包裹地址不详，无法派送，请点击 kd-sh.cc 重新填写。", true, "快递员会直接打电话，不会发陌生链接。", "双 11 快递诈片，2016-11", 20161113, topic: "双11"),
            M("1069 1111 9999", "【支付宝】您的双 11 订单退款 268 元已原路退回，请注意查收。", false, "原路退回，没让你点链接、没要卡号，是正常通知。", "双 11 退款通知", 20161115, level: 2, topic: "双11"),
            M("1065 2016 0808", "【里约奥运】恭喜您获得中国体育代表团纪念金币一枚，点击 ay2016.cc 填写地址领取", true, "中奖 + 陌生链接。", "里约奥运，2016-08", 20160806, topic: "里约奥运"),
            M("138 0000 2016", "我是你们班主任，学校要收资料费 360，打到这个卡：6222 **** 1234，下午前交齐", true, "冒充老师收钱。真要收费，学校会开收据。", "冒充老师诈片，2016-09 开学季", 20160901),
            M("1069 0000 0909", "【阴阳师】您的账号存在异常登录，请点击 yys-163.cc 验证身份，否则将封号。", true, "吓唬你点链接，盗号用的。", "《阴阳师》盗号，2016-09", 20160910, topic: "阴阳师"),
            M("189 0000 6666", "亲，我是淘宝店主，您的宝贝卡单了，加 QQ 2016xxxx 给您退款", true, "店主不会让你加 QQ 退款，下一步就是让你转账。", "淘宝卡单诈片"),
            M("135 0000 8888", "猜猜我是谁？我换号了，存一下，明天找你有点事", true, "「猜猜我是谁」+ 换号，下一步就是借钱。", "冒充熟人诈片"),
            M("1065 0000 0110", "【12306】您购买的 G1234 次列车因故停运，请点击 12306tk.cc 办理改签退款。", true, "12306 的网址是 12306.cn，退票去官网或车站。", "铁路退票诈片"),
            M("12306", "【铁路客服】您已购 10 月 1 日 G1234 次车票，6 车 8F 号，请按时乘车。", false, "国庆的购票回执，没链接、不要钱。", "国庆出行，2016-10", 20160925),
            M("95555", "【招商银行】您账户 0524 于 10 月 3 日支出 168.00 元，余额 2316.45 元。", false, "银行官方号码的消费提醒，没让你干什么。", "银行通知", 20161003),
            M("106 5500 1000", "【顺丰速运】您的快件 9527 已由小区丰巢柜签收，取件码 6688。", false, "正常的取件通知。", "快递通知", 20160601),
            M("10010", "【中国联通】您本月已使用流量 1.2G，套餐内剩余 800M。", false, "联通的正常提醒。", "运营商通知"),
            M("10000", "【中国电信】您的宽带将于 8 月 31 日到期，可到营业厅或网上营业厅续费。", false, "到期提醒，没有链接、没要卡号。", "运营商通知", 20160801),
            M("表姐", "中秋回不回老家？妈做了你爱吃的月饼，五仁的。", false, "家里人。五仁月饼虽然没人爱吃，也不是垃圾短信。", "日常短信", 20160910),
            M("阿杰", "国庆网吧通宵包夜 15，来不来？我给你留个位", false, "阿杰发的，熟人约网吧。", "日常短信", 20160928),
            M("妈", "给你寄的饺子到了没？放冰箱，别又拿泡面当晚饭。", false, "家人的消息，由你决定是否重要。"),
            M("爸", "转你五百了，买点好吃的，别跟你妈说。", false, "家人的消息，由你决定是否重要。"),
            M("外婆", "阳台的花开了，什么时候回来看看？", false, "家人的消息，由你决定是否重要。"),
            M("姐", "你走的时候是不是把那根网线带走了，我找半天。", false, "家人的消息，由你决定是否重要。"),
            M("表哥", "今晚老地方吃烧烤，我请。", false, "家人的消息，由你决定是否重要。"),
            M("妈", "下雨了，出门记得拿伞。", false, "家人的消息，由你决定是否重要。"),
            M("爸", "家里路由器又亮红灯，空了打个电话。", false, "家人的消息，由你决定是否重要。"),
            M("表姐", "电脑上照片怎么传手机？你教教我。", false, "家人的消息，由你决定是否重要。"),
            M("奶奶", "买了你爱吃的桃，回来拿。", false, "家人的消息，由你决定是否重要。"),
            M("外婆", "号码没存错吧？回我一下就行。", false, "家人的消息，由你决定是否重要。"),
            M("妈", "这周天凉，柜子里的外套找出来。", false, "家人的消息，由你决定是否重要。", since: 20161001),
            M("爸", "过年早点买票，别又买不到。", false, "家人的消息，由你决定是否重要。", since: 20161201),
            M("姐", "端午回来吗？我买了粽子。", false, "家人的消息，由你决定是否重要。", since: 20160601),
            M("妈", "看了新闻说手机会爆炸，你那个是不是三星的？赶紧换了", false, "妈发的，担心你。", "日常短信", 20160912),
            M("同学", "下周三交毕业论文初稿，导师说别再拖了", false, "同学提醒正事。", "日常短信"),
            M("1065 0000 6666", "【XX 直播】主播「小甜甜」邀请你进入直播间，送你 100 个钻石，点 live-xx.cc", true, "直播平台群发的拉新广告，链接来路不明。", "直播热，2016", 20160501, level: 2),
            M("1069 2016 0309", "【围棋】AlphaGo 同款人工智能炒股软件，月入十万，免费体验点 ag-gp.cc", true, "蹭 AlphaGo 的热点卖炒股软件，标准骗局。", "AlphaGo 人机大战之后，2016-03", 20160316, topic: "AlphaGo"),
            M("1065 2016 1010", "【宝宝心里苦】蓝瘦香菇？点击 xg-hb.cc 领 88 元红包安慰一下", true, "蹭热词 + 红包链接。", "「蓝瘦香菇」，2016-10", 20161012, topic: "蓝瘦香菇"),
            M("1069 0000 1234", "【小目标】先定一个能实现的小目标，比方说先赚他一个亿。理财咨询加微信 xmb666", true, "蹭「小目标」的理财广告。", "「小目标」，2016-08-29 走红", 20160830),
            M("10086", "【中国移动】充 50 送 50，仅限今日，回复 KT 办理。", true, "虽然发件人是 10086，但属于群发充值促销推销，算垃圾短信。", "运营商营销", level: 2, trick: true),
            M("10086", "【中国移动】您尾号 0524 的手机卡已完成实名登记核验，无需回复。", false, "实名制办理结果回执，不推销、不扣费、无链接，是正常通知。", "运营商通知", level: 2, trick: true),
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

            // 字面规则 vs 语义反转（老司机题）
            P("震惊！一个围棋程序打败了世界冠军", true, "事情是真的，但「震惊！」的写法就是标题党。只看写法。", "AlphaGo，2016-03", 20160309, trick: true, topic: "AlphaGo"),
            P("震惊！小伙在出租屋里关门三个月，打开门房东惊呆了……", true, "「震惊！」+「惊呆了」+ 省略号，标准标题党。", "UC 震惊部", trick: true),
            P("为什么男人都爱去网吧？最后一个原因太真实了……", true, "「为什么……最后一个原因」吊胃口不说内容，标准标题党。", "公众号标题", trick: true),
            P("霍金开通微博，24 小时粉丝破两百万", false, "数字惊人，但写法是平实的陈述。", "霍金开微博，2016-04-12", 20160412, trick: true),
            P("《美人鱼》票房突破 30 亿", false, "数字大，但只是陈述。", "《美人鱼》，2016 年春节档", 20160310, trick: true),
            P("消防提醒：千万别把电瓶车推上楼充电", false, "虽然有「千万别」，但把具体要求在标题里直接说清楚了，不是标题党。", "消防通知", trick: true),
            P("英伟达发布 GTX 1080 显卡，售价 599 美元起", false, "直接给出型号与售价，标准科技新闻。", "科技新闻，2016-05", 20160527, trick: true),
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
            P("星光网吧 32 号机同款鼠标，左键清脆，打守望先锋刚好", false, "有具体型号和使用感受，是真买家。", "真实评价", 20160524),
            P("快递三天才到，盒子压扁了，东西倒是没坏", false, "具体的经过。", "真实评价"),
            P("颜色比图片深一点，一洗就缩水，差评", false, "具体的毛病。", "真实评价"),
            P("买来给我爸用的，他说字太小看不清", false, "具体的使用场景。", "真实评价"),
            P("说是宋仲基同款，到手就是普通外套，算了", false, "真实的失望。", "《太阳的后裔》同款", 20160224, topic: "太阳的后裔"),
            P("风扇声音太大，晚上睡不着，退了", false, "具体的毛病。", "真实评价"),
            P("表姐推荐的，用了一个月，键盘 F 键有点松", false, "有时间、有细节。", "真实评价"),

            // 字面规则 vs 语义反转（老司机题）
            P("差评！发货太快了，我还没准备好钱包就到了", true, "假差评真好评，内容空，刷单常见套路。", "刷单套路", trick: true),
            P("差评！怪我自己买晚了，这么好用的散热硅脂居然才发现，必须五星好评！", true, "欲扬先抑的假差评，通篇像广告词，是刷单套路。", "刷单套路", trick: true),
            P("宝贝很好，给五星！（括号里的字删掉，一条五毛发截图结账）", true, "刷客复制文案时忘了删掉群主的括弧指令，铁证如山。", "刷单翻车", trick: true),
            P("五星，因为老板答应返现五块", true, "好评返现买来的评价，也算刷的。", "好评返现", trick: true),
            P("好评！电源适配器坏了联系客服，第二天就换了新的", false, "开头是「好评！」，但后面有具体经过，是真买家。", "真实评价", trick: true),
            P("五星好评！二手 GTX 960 挡板有点生锈，但烤机半小时稳定 72 度，没翻车", false, "虽然开头写「五星好评」，但有具体成色和烤机温度，是真买家。", "真实评价", trick: true),
            P("物超所值！买来装在出租屋旧机箱里的，风扇比原来那把安静多了", false, "虽然写了「物超所值」，但有具体安装场景和对比细节，是真买家。", "真实评价", trick: true),
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
            P("He is an old driver in Dota.", false, "中式英语：「老司机」指老手，英文应说 old hand 或 veteran，不能直译成 old driver。", "「老司机」，2016 年初走红", 20160101, level: 2, topic: "老司机"),
            P("Can you open the computer #32 for me?", false, "中式英语：网吧「开机」应为 turn on the computer，open 是拆开机箱。", "常见搭配错误", level: 2),
            P("Horse horse tiger tiger.", false, "中式英语：「马马虎虎」的逐字硬翻，英文应说 so-so。", "老笑话", level: 2),

            P("Excuse me, where is the toilet?", true, "正确。", "课本"),
            P("It's raining cats and dogs.", true, "地道习语：雨很大。", "英语习语"),
            P("How much is this graphics card?", true, "正确。", "日常英语"),
            P("I'm fine, thank you. And you?", true, "课本句子，没有错。", "课本"),
            P("Welcome to Beijing!", true, "正确。", "日常英语"),
            P("Hello, my name is LingGuang.", true, "正确。", "灵光"),

            // 字面规则 vs 语义反转（老司机题）
            P("How old are you?", true, "「怎么老是你」是网络笑话，但这句英文本身完全正确。", "网络笑话", trick: true),
            P("Long time no see.", true, "源自中式英语，但早就被英语接受了。", "英语外来说法", trick: true),
            P("lose face", true, "源自中文「丢脸」，英语词典早已收录。", "英语外来说法", trick: true),
            P("Paper tiger.", true, "源自中文「纸老虎」，但早已被英语词典正式收录。", "英语外来说法", trick: true),
            P("Brainwash.", true, "源自中文「洗脑」，早已成为标准英语单词。", "英语外来说法", trick: true),
            P("Good good study", false, "看着眼熟，但还是中式英语。", "老笑话", trick: true),
        };
    }
}
