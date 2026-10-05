using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>A day that matters to her (design §2, §7). Wishing first pays, forgetting costs and shows in her signature.</summary>
    public sealed class GfHoliday
    {
        public string key, zh, en;
        public DateTime date;
        /// <summary>Extra days the wish still counts (Christmas Eve and Day are one holiday).</summary>
        public int span;
        /// <summary>Words that count as a wish.</summary>
        public string[] wish;
        /// <summary>Affection for wishing before she brings it up, after she hints, and for forgetting.</summary>
        public int first, late, forgot;
        /// <summary>Her line on the day if he has not said anything yet (zh, en pairs; one bubble each).</summary>
        public string[] hint;
        /// <summary>Her reply to a wish.</summary>
        public string[] thanks;
        /// <summary>Her signature for three days after he forgot (empty: no change).</summary>
        public string sulkZh = "", sulkEn = "";
        /// <summary>What the prompt says about today, from her point of view.</summary>
        public string situationZh, situationEn;
    }

    /// <summary>A dated thing she brings up by herself (her exam, her cat, the lipstick), once per save.</summary>
    public sealed class GfLife
    {
        public string key;
        public DateTime from, to;
        /// <summary>Bubbles (zh, en pairs).</summary>
        public string[] lines;
        /// <summary>What she remembers having said, and the word it is recognised by (empty: nothing).</summary>
        public string memoryKey = "", memoryZh = "", memoryEn = "";
        /// <summary>Minimum tier, and a flag that must be set (the AI questions).</summary>
        public GirlfriendTier minTier = GirlfriendTier.Cold;
        public string needsFlag = "";
        /// <summary>A question that waits for his yes (a promise key), or "" for none.</summary>
        public string asks = "";
    }

    public static partial class GirlfriendRules
    {
        public static readonly DateTime Birthday = new DateTime(2016, 9, 27);
        public static readonly DateTime MovieDay = new DateTime(2016, 12, 2);

        public static readonly GfHoliday[] Holidays =
        {
            new GfHoliday { key = "qixi", zh = "七夕", en = "Qixi", date = new DateTime(2016, 8, 9), wish = new[] { "七夕", "情人节", "qixi", "valentine" }, first = 4, late = 2, forgot = -6,
                hint = new[] { "今天七夕诶", "Today's Qixi, you know", "你就没什么要说的吗[撇嘴]", "Nothing to say to me? [撇嘴]" },
                thanks = new[] { "哼 算你有良心[害羞]", "Hmph, you do have a heart after all [害羞]" },
                sulkZh = "七夕而已 无所谓", sulkEn = "It's only Qixi. Whatever.",
                situationZh = "今天是七夕（中国情人节）。", situationEn = "Today is Qixi, the Chinese Valentine's Day." },
            new GfHoliday { key = "midautumn", zh = "中秋", en = "Mid-Autumn", date = new DateTime(2016, 9, 15), wish = new[] { "中秋", "月饼", "mid-autumn", "mooncake" }, first = 4, late = 2, forgot = -6,
                hint = new[] { "中秋了 宿舍发了月饼 五仁的[白眼]", "Mid-Autumn. The dorm handed out mooncakes. Five-nut ones [白眼]", "你那边月亮圆吗", "Is the moon round where you are?" },
                thanks = new[] { "中秋快乐呀 今年又没一起过[月亮]", "Happy Mid-Autumn. Another one apart [月亮]" },
                sulkZh = "月亮再圆有什么用", sulkEn = "What good is a full moon",
                situationZh = "今天是中秋节，你在学校过，和他异地。", situationEn = "Today is the Mid-Autumn Festival; you are spending it at university, far from him." },
            new GfHoliday { key = "birthday", zh = "生日", en = "birthday", date = Birthday, wish = new[] { "生日快乐", "生日", "birthday" }, first = 4, late = 2, forgot = -6,
                hint = new[] { "今天什么日子你知道吗", "Do you know what day it is today?" },
                thanks = new[] { "你居然记得！！", "You remembered!!", "笨蛋 谢谢[爱心]", "Dummy. Thank you [爱心]" },
                sulkZh = "又老了一岁 有人记得吗", sulkEn = "A year older. Does anyone remember",
                situationZh = "今天是你（林晴雯）的生日，9 月 27 日。不是他的生日，他如果说生日快乐，是在给你庆生。", situationEn = "Today, 27 September, is YOUR (Lin Qingwen's) birthday, not his. If he says happy birthday, he is wishing you." },
            new GfHoliday { key = "national", zh = "国庆", en = "National Day", date = new DateTime(2016, 10, 1), wish = new[] { "国庆", "national day" }, first = 1, late = 0, forgot = 0,
                hint = new[] { "国庆七天 宿舍就剩我一个了", "Seven days off and I'm the only one left in the dorm" },
                thanks = new[] { "国庆快乐 你又在家宅着吧", "Happy National Day. Staying in again, I bet" },
                situationZh = "今天是国庆节，放七天假，室友都回家了。", situationEn = "It is National Day: a week off, and your roommates have all gone home." },
            new GfHoliday { key = "singles", zh = "双十一", en = "Singles' Day", date = new DateTime(2016, 11, 11), wish = new string[0], first = 0, late = 0, forgot = 0,
                hint = new[] { "今天双十一！！", "It's Singles' Day!!", "帮我清空购物车好不好[可怜]", "Clear my shopping cart for me? [可怜]" },
                thanks = new string[0],
                situationZh = "今天是双十一，你撒娇让他帮你清空购物车（开玩笑的成分多）。", situationEn = "It is Singles' Day; you are half-joking that he should empty your shopping cart." },
            new GfHoliday { key = "xmas", zh = "圣诞", en = "Christmas", date = new DateTime(2016, 12, 24), span = 1, wish = new[] { "圣诞", "平安夜", "christmas", "xmas" }, first = 4, late = 2, forgot = -6,
                hint = new[] { "平安夜 室友都收到苹果了", "Christmas Eve. My roommates all got apples", "我的呢[委屈]", "Where's mine? [委屈]" },
                thanks = new[] { "圣诞快乐[玫瑰] 虽然是洋节", "Merry Christmas [玫瑰], foreign holiday or not" },
                sulkZh = "平安夜 一个人也挺好", sulkEn = "Christmas Eve alone is fine too",
                situationZh = "今天是平安夜 / 圣诞节，宿舍楼下有人在卖苹果。", situationEn = "It is Christmas Eve / Christmas; someone is selling apples outside the dorm." },
            new GfHoliday { key = "newyear", zh = "跨年", en = "New Year's Eve", date = new DateTime(2016, 12, 31), wish = new[] { "新年快乐", "跨年", "新年", "happy new year", "new year" }, first = 4, late = 2, forgot = 0,
                hint = new[] { "今天跨年 你在干嘛", "It's New Year's Eve. What are you up to?" },
                thanks = new[] { "新年快乐！明年也要在一起", "Happy New Year! Together next year too" },
                situationZh = "今天是 12 月 31 日，跨年夜。", situationEn = "It is 31 December, New Year's Eve." },
        };

        /// <summary>The holiday on this date (or within its span), or null.</summary>
        public static GfHoliday HolidayOn(DateTime date)
        {
            foreach (var h in Holidays) if (date.Date >= h.date && date.Date <= h.date.AddDays(h.span)) return h;
            return null;
        }

        /// <summary>No classes: the National Day week and the Mid-Autumn long weekend.</summary>
        public static bool Holiday(DateTime date)
        {
            date = date.Date;
            return (date >= new DateTime(2016, 10, 1) && date <= new DateTime(2016, 10, 7)) || (date >= new DateTime(2016, 9, 15) && date <= new DateTime(2016, 9, 17));
        }

        /// <summary>
        /// Her life over the year, as things she brings up herself (design §1, §3, §4.6). Each is sent once, in its
        /// window, when she is around. Several leave a memory that a gift or a question of his can match later.
        /// </summary>
        public static readonly GfLife[] Life =
        {
            new GfLife { key = "firstNight", from = new DateTime(2016, 5, 24), to = new DateTime(2016, 12, 31), lines = new[] { "还没睡？", "Still up?" } },
            new GfLife { key = "exams", from = new DateTime(2016, 5, 25), to = new DateTime(2016, 5, 31), lines = new[] { "期末复习好烦 下礼拜就放假了", "Revising for finals is the worst. Holidays next week", "放假回去找你玩[调皮]", "I'll come and see you when I'm home [调皮]" } },
            new GfLife { key = "home", from = new DateTime(2016, 6, 1), to = new DateTime(2016, 6, 12), lines = new[] { "到家了！我妈做了一桌子菜", "Home! Mum cooked a whole table of food", "煤球胖了一圈 我都抱不动了", "Meiqiu got so fat I can't lift him" },
                memoryKey = "煤球", memoryZh = "她家的猫叫煤球", memoryEn = "her cat is called Meiqiu (Coal Ball)" },
            new GfLife { key = "drama", from = new DateTime(2016, 6, 8), to = new DateTime(2016, 6, 30), lines = new[] { "《欢乐颂》补完了 我是关雎尔本尔", "Finished Ode to Joy. I am literally Guan Ju'er", "你都不陪我看[撇嘴]", "You never watch it with me [撇嘴]" },
                memoryKey = "欢乐颂", memoryZh = "她在追《欢乐颂》", memoryEn = "she binge-watched Ode to Joy" },
            new GfLife { key = "plush", from = new DateTime(2016, 6, 15), to = new DateTime(2016, 7, 31), lines = new[] { "同学送了她男朋友一个熊本熊公仔", "A classmate's boyfriend gave her a Kumamon plush", "好可爱啊啊啊", "So cute aaaah" },
                memoryKey = "熊本熊", memoryZh = "她说熊本熊公仔好可爱", memoryEn = "she said a Kumamon plush is so cute" },
            new GfLife { key = "band", from = new DateTime(2016, 6, 20), to = new DateTime(2016, 8, 20), lines = new[] { "我要减肥 从明天开始跑步", "I'm going on a diet. Running from tomorrow", "你监督我[奋斗]", "You keep me honest [奋斗]" },
                memoryKey = "跑步", memoryZh = "她说要开始跑步减肥", memoryEn = "she said she would start running to lose weight" },
            new GfLife { key = "milktea", from = new DateTime(2016, 7, 1), to = new DateTime(2016, 12, 31), lines = new[] { "好想喝奶茶 外卖还要配送费", "I want bubble tea so bad and delivery costs extra" },
                memoryKey = "奶茶", memoryZh = "她想喝奶茶", memoryEn = "she wanted bubble tea" },
            new GfLife { key = "weiwei", from = new DateTime(2016, 8, 12), to = new DateTime(2016, 8, 31), lines = new[] { "《微微一笑很倾城》电影上了", "Love O2O is out in cinemas", "肖奈大神好帅 你学学人家[偷笑]", "Xiao Nai is so cool. Learn from him [偷笑]" } },
            new GfLife { key = "qixiAsk", from = new DateTime(2016, 8, 1), to = new DateTime(2016, 8, 8), lines = new[] { "七夕那天视频吗", "Video call on Qixi?" }, asks = "qixi" },
            new GfLife { key = "back", from = new DateTime(2016, 9, 1), to = new DateTime(2016, 9, 10), lines = new[] { "回学校了 又要异地了", "Back at uni. Long distance again", "宿舍 23:30 断电断网 以后晚上只能用流量找你了", "Power and WiFi go off at 23:30 in the dorm, so it's mobile data at night now" } },
            new GfLife { key = "battery", from = new DateTime(2016, 9, 5), to = new DateTime(2016, 10, 31), lines = new[] { "手机老没电 一天充三次", "My phone keeps dying. I charge it three times a day", "下午上课就关机了 不是不回你哈", "It dies in afternoon class, I'm not ignoring you" },
                memoryKey = "没电", memoryZh = "她说手机老没电", memoryEn = "she said her phone keeps running out of battery" },
            new GfLife { key = "case", from = new DateTime(2016, 9, 10), to = new DateTime(2016, 11, 30), lines = new[] { "隔壁宿舍那对用情侣手机壳", "The couple next door have matching phone cases", "好幼稚哦 [偷笑]", "So childish [偷笑]" },
                memoryKey = "手机壳", memoryZh = "她说隔壁宿舍用情侣手机壳", memoryEn = "she mentioned the matching phone cases next door" },
            new GfLife { key = "lipstick", from = new DateTime(2016, 10, 1), to = new DateTime(2016, 10, 31), lines = new[] { "朋友圈全是 YSL 星辰", "My feed is all YSL Star lipstick", "根本抢不到[大哭] 想要", "Impossible to get [大哭]. I want one" },
                memoryKey = "星辰", memoryZh = "她想要 YSL 星辰口红", memoryEn = "she wants the YSL Star lipstick" },
            new GfLife { key = "exam", from = new DateTime(2016, 10, 24), to = new DateTime(2016, 10, 30), lines = new[] { "下周三考概率论", "Probability exam next Wednesday", "完蛋 一点都不会[流汗]", "I'm doomed, I know nothing [流汗]" },
                memoryKey = "概率论", memoryZh = "她下周三（11 月 2 日）考概率论", memoryEn = "she has a probability exam next Wednesday (2 November)" },
            new GfLife { key = "examDone", from = new DateTime(2016, 11, 2), to = new DateTime(2016, 11, 6), lines = new[] { "概率论考完了！！", "Probability exam done!!", "感觉能过[胜利]", "I think I passed [胜利]" } },
            new GfLife { key = "movieAsk", from = new DateTime(2016, 11, 25), to = new DateTime(2016, 12, 1), lines = new[] { "《你的名字。》12 月 2 号上映", "Your Name comes out on 2 December", "我们一起看吧 你在你那边 我在我这边 同一场", "Let's watch it together, you there and me here, the same showing" }, asks = "movie",
                memoryKey = "你的名字", memoryZh = "她想 12 月 2 日一起看《你的名字。》", memoryEn = "she wants to watch Your Name together on 2 December" },
            new GfLife { key = "cet", from = new DateTime(2016, 12, 5), to = new DateTime(2016, 12, 16), lines = new[] { "17 号考六级 我单词还没背完", "CET-6 on the 17th and I haven't finished my vocab", "你都不监督我[委屈]", "You never check on me [委屈]" },
                memoryKey = "六级", memoryZh = "她 12 月 17 日考六级", memoryEn = "she sits the CET-6 English exam on 17 December" },
            new GfLife { key = "aiWorry", from = new DateTime(2016, 5, 24), to = new DateTime(2016, 12, 31), lines = new[] { "你那个 AI", "That AI of yours", "它会不会比我还懂你", "Does it understand you better than I do?" }, needsFlag = FlagKnowsAi },
        };

        /// <summary>The life line due now, if any (the first unsent one whose window contains today).</summary>
        public static GfLife DueLife(GirlfriendState s, GfNow now)
        {
            if (s == null) return null;
            foreach (var l in Life)
            {
                if (s.done.Contains("life:" + l.key)) continue;
                if (now.clock.Date < l.from || now.clock.Date > l.to) continue;
                if (Tier(s) < l.minTier) continue;
                if (l.needsFlag.Length > 0 && !Has(s, l.needsFlag)) continue;
                if (l.key == "aiWorry" && now.day - KnewAiDay(s) < 5) continue;
                return l;
            }
            return null;
        }

        static int KnewAiDay(GirlfriendState s)
        {
            foreach (var d in s.done) if (d.StartsWith("knewAi@", StringComparison.Ordinal) && int.TryParse(d.Substring(7), out int day)) return day;
            return int.MaxValue / 2;
        }
    }
}
