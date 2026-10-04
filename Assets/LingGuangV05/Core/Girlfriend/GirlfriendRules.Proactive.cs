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
                return Proactive(s, now, GfEvent.Say("life:firstNight", Life[0].lines));
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

        public static readonly string[] TopicKeys = { "roommate", "photo", "drama", "food", "whatDoing", "missYou" };

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
                case "roommate": return english ? "You message him first to complain about a roommate (snoring, hogging the bathroom, a loud phone call)." : "你主动找他，吐槽室友（打呼噜、占卫生间、半夜打电话之类）。";
                case "photo": return english ? "You message him first and share a photo: write it as [图片] followed by a short description of what is in it, then one line about it." : "你主动找他分享一张照片：写成「[图片]」加一句照片里是什么，再说一句感想。";
                case "drama": return english ? "You message him first about the drama you are watching (2016: Ode to Joy, Love O2O, Nirvana in Fire is old)." : "你主动找他聊你在追的剧（2016 年：《欢乐颂》《微微一笑很倾城》之类）。";
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
