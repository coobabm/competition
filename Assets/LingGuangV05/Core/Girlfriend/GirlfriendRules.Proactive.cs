using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Girlfriend
{
    public static partial class GirlfriendRules
    {
        // ───────────── her own messages (design §3: 主动找主角) ─────────────

        /// <summary>Most messages she starts in one calendar day, by tier (冷 never starts a conversation).</summary>
        public static int ProactivePerDay(GirlfriendTier t)
        {
            switch (t) { case GirlfriendTier.Sweet: return 5; case GirlfriendTier.Warm: return 3; case GirlfriendTier.Normal: return 2; case GirlfriendTier.Distant: return 1; default: return 0; }
        }

        /// <summary>Seconds of play between two of her own messages (a calendar day is under a minute of play, so the count per day alone would flood).</summary>
        public static double ProactiveGap(GirlfriendTier t)
        {
            switch (t) { case GirlfriendTier.Sweet: return 70; case GirlfriendTier.Warm: return 100; case GirlfriendTier.Normal: return 150; case GirlfriendTier.Distant: return 280; default: return 1e9; }
        }

        /// <summary>Her first hello depends on the desktop clock: 22:01–5:00 still up, then morning, lunch, afternoon, dinner.</summary>
        public static string[] FirstHello(int hour, int minute)
        {
            int m = hour * 60 + minute;
            if (m > 22 * 60 || m <= 5 * 60) return new[] { "还没睡？", "Still up?" };
            if (m <= 11 * 60) return new[] { "醒了吗？", "Are you up?" };
            if (m <= 13 * 60) return new[] { "吃午饭了吗？", "Had lunch yet?" };
            if (m <= 17 * 60) return new[] { "在干嘛呢？", "What are you up to?" };
            return new[] { "吃晚饭了吗？", "Had dinner yet?" };
        }

        /// <summary>Seconds of play she waits before shaking the window (暖 and 甜 only).</summary>
        public static double ShakeAfter(GirlfriendTier t) => t == GirlfriendTier.Sweet ? 40 : t == GirlfriendTier.Warm ? 60 : double.PositiveInfinity;

        /// <summary>
        /// What she starts by herself now, if anything: her very first 「还没睡？」, a shake when he has gone quiet
        /// (warm and sweet), a holiday hint, a reminder about the film tickets, the next dated thing in her life, or
        /// a free topic for the model (kind Topic). Nothing while he owes her an answer, and never more often than
        /// her tier allows. Marks what it returns as done.
        /// </summary>
        public static GfEvent NextProactive(GirlfriendState s, GfNow now, GfActivity a)
        {
            if (s == null || !s.started || !a.Present) return null;
            var tier = Tier(s);
            if (!s.done.Contains("life:firstNight"))
            {
                s.done.Add("life:firstNight");
                return Proactive(s, now, GfEvent.Say("life:firstNight", FirstHello(now.clock.Hour, now.clock.Minute)));
            }
            // The weekend question is the one the AI will not answer for him: it comes even when she is cold, quiet, or still waiting for his answer.
            if (s.asking.Length == 0 && !s.done.Contains("life:weekendAsk"))
            {
                var weekend = Array.Find(Life, l => l.key == "weekendAsk");
                if (weekend != null && now.clock.Date >= weekend.from && now.clock.Date <= weekend.to && Has(s, weekend.needsFlag))
                {
                    s.done.Add("life:weekendAsk");
                    s.asking = weekend.asks; s.askDay = now.day;
                    return Proactive(s, now, GfEvent.Say("life:weekendAsk", weekend.lines));
                }
            }
            if (s.awaitingReply)
            {
                if (s.shookFor != s.herLastGame && s.unansweredRun < 3 && now.game - s.herLastGame >= ShakeAfter(tier) && s.asking.Length == 0)
                {
                    s.shookFor = s.herLastGame;
                    var e = GfEvent.Of(GfEventKind.Shake, Mark(s, FlagShook) ? "first.shake" : "shake");
                    return e;
                }
                return null;
            }
            if (s.asking.Length > 0 || now.game < s.nextProactiveGame) return null;
            if (s.proactiveDay != now.day) { s.proactiveDay = now.day; s.proactiveToday = 0; }

            var h = HolidayOn(now.clock);
            if (h != null && tier != GirlfriendTier.Cold && !s.done.Contains("hint:" + h.key) && !s.done.Contains("wish:" + h.key) && s.dayGame >= 12)
            {
                s.done.Add("hint:" + h.key);
                return Proactive(s, now, GfEvent.Say("hint:" + h.key, h.hint));
            }
            foreach (var p in s.promises)
                if (p.key == "movie" && p.state == 0 && now.clock.Date >= MovieDay.AddDays(-2) && now.clock.Date < MovieDay && !HasTickets(s, now.day) && !s.done.Contains("tickets?"))
                {
                    s.done.Add("tickets?");
                    return Proactive(s, now, GfEvent.Say("tickets?", "票你买了吗 淘货上有", "Have you bought the tickets? Taohuo has them"));
                }
            if (s.proactiveToday >= ProactivePerDay(tier)) return null;
            var life = DueLife(s, now);
            if (life != null && (tier >= GirlfriendTier.Distant || life.key == "aiWorry"))
            {
                s.done.Add("life:" + life.key);
                if (life.memoryKey.Length > 0) Remember(s, life.memoryKey, "她说：" + life.memoryZh);
                if (life.asks.Length > 0) { s.asking = life.asks; s.askDay = now.day; }
                if (life.key == "aiWorry") { s.asking = "aiWorry"; s.askDay = now.day; }
                return Proactive(s, now, GfEvent.Say("life:" + life.key, life.lines));
            }
            if (tier >= GirlfriendTier.Normal && Roll(s) < (tier == GirlfriendTier.Normal ? .5 : .75))
                return Proactive(s, now, GfEvent.Of(GfEventKind.Topic, Topic(s)));
            s.nextProactiveGame = now.game + ProactiveGap(tier) * .5;
            return null;
        }

        static GfEvent Proactive(GirlfriendState s, GfNow now, GfEvent e)
        {
            s.proactiveToday++;
            s.nextProactiveGame = now.game + ProactiveGap(Tier(s)) * (.7 + .6 * Roll(s));
            return e;
        }

        /// <summary>Her free topics; the last one (想你) only when she is sweet on him.</summary>
        public static readonly string[] TopicKeys = { "roommate", "photo", "drama", "food", "friend", "family", "school", "phone", "music", "whatDoing", "missYou" };

        static string Topic(GirlfriendState s)
        {
            var t = Tier(s);
            int n = t == GirlfriendTier.Sweet ? TopicKeys.Length : TopicKeys.Length - 1;
            return TopicKeys[Pick(s, n)];
        }

        /// <summary>What a free topic asks the model to write about.</summary>
        public static string TopicPrompt(string key, bool english)
        {
            switch (key)
            {
                case "roommate": return english ? "You message him first with something about one of your roommates (He Jia's snoring, Su Xiaoxiao's loud calls, Tang Tian hogging the bathroom, Tingting, or Yutong helping you study). Pick one you have not talked about lately." : "你主动找他，说一件室友的事（何佳打呼噜、苏晓晓半夜打电话、唐甜占卫生间、婷婷、雨桐给你讲题之类），挑最近没说过的。";
                case "friend": return english ? "You message him first with news from your friends: Zhao Xiaoyu (your high-school deskmate, studying nursing in Zhengzhou), Wang Meng (works at the county phone shop), or the class YY group. Pick one you have not talked about lately." : "你主动找他，说一件朋友的新鲜事：高中同桌赵小雨（在郑州读护理）、在县城移动营业厅上班的王萌，或者「一中高三七班」群里的事，挑最近没说过的。";
                case "family": return english ? "You message him first about home: your mum's nagging on the phone, your quiet dad, the cat Meiqiu, grandma in the village, or your cousin. Pick one you have not talked about lately." : "你主动找他，说家里的事：你妈打电话唠叨、话少的老爸、猫煤球、乡下的奶奶或者表妹，挑最近没说过的。";
                case "school": return english ? "You message him first about school: the library with Yutong, the end-of-term exams coming, PE, a class you dozed through." : "你主动找他，说学校的事：跟雨桐泡图书馆、快期末了、体育课、上课打瞌睡之类。";
                case "phone": return english ? "You message him first about your phone: the battery dying, a Happy Elimination level you are stuck on, running out of data, something you saw on Taohuo." : "你主动找他，说手机上的事：又没电了、开心消消乐卡关、流量不够、在淘货看中的东西之类。";
                case "music": return english ? "You message him first about a song you have on repeat (2016: Joker Xue's The Actor, A Little Happiness) and ask what he listens to." : "你主动找他，聊你在单曲循环的歌（2016 年：薛之谦《演员》、《小幸运》之类），问他在听什么。";
                case "photo": return english ? "You message him first and share a photo: write it as [图片] followed by a short description of what is in it, then one line about it." : "你主动找他分享一张照片：写成「[图片]」加一句照片里是什么，再说一句感想。";
                case "drama": return english ? "You message him first about the drama you are watching (early 2016: Ode to Joy, Descendants of the Sun)." : "你主动找他聊你在追的剧（2016 年上半年：《欢乐颂》《太阳的后裔》之类）。";
                case "food": return english ? "You message him first about what you ate today or ask if he has eaten." : "你主动找他，说你今天吃了什么，或者问他吃了没。";
                case "missYou": return english ? "You message him first because you miss him; say it in your own shy, teasing way." : "你主动找他，因为想他了；用你嘴硬又撒娇的方式说。";
                default: return english ? "You message him first and ask what he is doing, half complaining that he is always on the computer." : "你主动找他，问他在干嘛，半抱怨他又在弄电脑。";
            }
        }

        // ───────────── good night and the sleep nag (design §3, §7 熬夜) ─────────────

        /// <summary>Seconds of play she is around on one calendar day before she says good night (when it is night on the clock).</summary>
        public static double EveningBudget(GirlfriendState s, GfNow now, GfActivity a)
        {
            double lo = Summer(now.clock) ? 240 : a.classTomorrow ? 150 : 260;
            return lo + DayDie(s, now.day, 3) * 160;
        }

        /// <summary>
        /// Her good night, when she has been around long enough tonight. After 1 a.m. with him still on the computer,
        /// it becomes 「你又要通宵弄电脑吗」. She then sleeps until the calendar turns.
        /// </summary>
        public static GfEvent Goodnight(GirlfriendState s, GfNow now, GfActivity a)
        {
            if (s == null || !s.started || !a.Present || s.awaitingReply) return null;
            double h = now.HourOfDay;
            bool night = h >= 22 || h < 6;
            if (!night || s.dayGame < EveningBudget(s, now, a)) return null;
            s.asleepDay = now.day;
            var t = Tier(s);
            if (h >= 1 && h < 6)
            {
                if (t == GirlfriendTier.Cold) return GfEvent.Say("goodnight", "睡了", "Going to sleep");
                return GfEvent.Say("goodnight", "你又要通宵弄电脑吗", "Pulling another all-nighter on that computer?", t >= GirlfriendTier.Warm ? "我先睡了 你也早点睡 笨蛋[月亮]" : "我先睡了", t >= GirlfriendTier.Warm ? "I'm going to sleep. You too, dummy [月亮]" : "I'm off to bed");
            }
            switch (t)
            {
                case GirlfriendTier.Sweet: return GfEvent.Say("goodnight", "我要睡啦", "Going to sleep now", "晚安 梦里见[亲亲]", "Good night, see you in my dreams [亲亲]");
                case GirlfriendTier.Warm: return GfEvent.Say("goodnight", "宿舍要断网了 晚安[月亮]", "Dorm WiFi's about to go, good night [月亮]");
                case GirlfriendTier.Normal: return GfEvent.Say("goodnight", "困了 晚安", "Sleepy. Good night");
                default: return GfEvent.Say("goodnight", "睡了", "Going to sleep");
            }
        }

        /// <summary>After 1 a.m., with class in the morning and a long chat tonight, she tells him to sleep (at most every third day). Three more lines from him and her mood drops.</summary>
        public static GfEvent SleepNag(GirlfriendState s, GfNow now, GfActivity a)
        {
            if (s == null || !a.Present || !a.classTomorrow || s.awaitingReply) return null;
            double h = now.HourOfDay;
            if (h < 1 || h >= 6 || s.linesDay != now.day || s.linesToday < 4) return null;
            if (s.naggedDay >= 0 && now.day - s.naggedDay < 3) return null;
            s.naggedDay = now.day; s.linesAfterNag = 0;
            return GfEvent.Say("nag", "都几点了 你快去睡觉", "Do you know what time it is? Go to sleep", "我明天早八[困]", "I've got an 8 a.m. tomorrow [困]");
        }

        // ───────────── 跨年 (design §7) ─────────────

        /// <summary>Her 12-31 23:59 message, one of five by tier. Once.</summary>
        public static GfEvent NewYearMessage(GirlfriendState s)
        {
            if (s == null || !s.started || !Mark(s, FlagNewYear)) return null;
            switch (Tier(s))
            {
                case GirlfriendTier.Sweet: return GfEvent.Say("newyear", "还有一分钟！！", "One minute left!!", "2017 也要一直在一起 我说的[爱心]", "Together all through 2017 too, I mean it [爱心]", "新年快乐 我想你了", "Happy New Year. I miss you");
                case GirlfriendTier.Warm: return GfEvent.Say("newyear", "快零点了 你还在电脑前吧", "Nearly midnight. Still at the computer, right?", "新年快乐 笨蛋[玫瑰]", "Happy New Year, dummy [玫瑰]");
                case GirlfriendTier.Normal: return GfEvent.Say("newyear", "新年快乐呀", "Happy New Year", "明年见[微笑]", "See you next year [微笑]");
                case GirlfriendTier.Distant: return GfEvent.Say("newyear", "新年快乐", "Happy New Year");
                default: return GfEvent.Say("newyear", "嗯 新年快乐", "Mm. Happy New Year");
            }
        }

        /// <summary>The epilogue's extra line (design §7): two versions, by whether affection is 70 or more.</summary>
        public static (string zh, string en) EpilogueLine(GirlfriendState s)
        {
            if (s != null && s.affection >= 70) return ("手机亮了。她说：「新的一年，我们见面吧。」", "The phone lit up. She wrote: \"New year. Let's see each other.\"");
            return ("手机亮了一下，又暗了。是群消息。", "The phone lit up and went dark again. A group message.");
        }
    }
}
