using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>
    /// Her offline lines (design §4.4 失败兜底): when the local model is not running or fails, she still answers in
    /// her tier's voice and the time of day's mood. Each tier × period has 7 lines (4 for the tier, 3 for the period);
    /// a line is never one of her last five.
    /// </summary>
    public static class GirlfriendLines
    {
        public enum Period { Night, Day, Evening }

        public static Period PeriodOf(DateTime clock)
        {
            int h = clock.Hour;
            if (h >= 22 || h < 6) return Period.Night;
            return h < 17 ? Period.Day : Period.Evening;
        }

        // Each entry: zh, en. Bubbles inside one line are separated by "|".
        static readonly string[][] Tier =
        {
            /* Cold */    new[] { "嗯", "Mm", "哦", "Oh", "随便", "Whatever", "知道了", "Got it" },
            /* Distant */ new[] { "嗯 知道了", "Mm, got it", "还行吧", "It's OK I guess", "哦 那你忙", "Oh, you go and be busy then", "没什么事", "Nothing much" },
            /* Normal */  new[] { "哈哈哈 真的假的", "Hahaha really?", "嗯嗯 然后呢", "Mm mm, and then?", "好吧|你开心就好", "Fine|as long as you're happy", "我刚也在想这个", "I was just thinking that" },
            /* Warm */    new[] { "笨蛋|你怎么才回我", "Dummy|why did you take so long", "嘿嘿 我就知道[偷笑]", "Hehe, I knew it [偷笑]", "你吃饭了没|别又泡面", "Have you eaten?|not instant noodles again", "想你了[害羞]", "Miss you [害羞]" },
            /* Sweet */   new[] { "你终于回我了！！|我等了好久[委屈]", "Finally!!|I waited so long [委屈]", "我跟你说|今天发生了好多事|等你有空慢慢讲", "Listen|so much happened today|I'll tell you when you're free", "你说的我都记着呢[爱心]", "I remember everything you say [爱心]", "好想快点放假见你", "I want the holidays to come so I can see you" },
        };

        static readonly string[][,] ByPeriod =
        {
            /* Cold */ new string[,] { { "困了", "Sleepy", "睡了", "Going to sleep", "明天再说", "Tomorrow" }, { "在上课", "In class", "忙", "Busy", "晚点", "Later" }, { "在吃饭", "Eating", "刚回来", "Just got back", "嗯 有事吗", "Mm, what is it" } },
            /* Distant */ new string[,] { { "这么晚还不睡", "Still up this late", "我要睡了", "I'm going to sleep", "早点休息吧", "Get some rest" }, { "刚下课", "Just out of class", "在图书馆", "At the library", "等会儿说", "Talk in a bit" }, { "刚吃完饭", "Just ate", "在宿舍", "In the dorm", "今天有点累", "A bit tired today" } },
            /* Normal */ new string[,] { { "你又熬夜[白眼]", "Up late again [白眼]", "我在床上刷手机", "In bed on my phone", "室友都睡了 我小声打字", "Roommates are asleep, I'm typing quietly" }, { "刚下课 饿死了", "Just out of class, starving", "今天食堂的饭好难吃", "Canteen food was awful today", "下午还有课[流汗]", "More class this afternoon [流汗]" }, { "刚洗完澡", "Just showered", "在追剧", "Watching a drama", "晚饭吃的麻辣烫", "Had malatang for dinner" } },
            /* Warm */ new string[,] { { "你怎么还不睡|又弄电脑", "Why aren't you asleep|computer again?", "我也睡不着[月亮]", "I can't sleep either [月亮]", "陪我聊会儿嘛", "Chat with me a bit" }, { "上课好无聊|想你", "Class is so boring|miss you", "偷偷在课上回你[偷笑]", "Replying secretly in class [偷笑]", "中午吃什么好呢", "What should I have for lunch" }, { "我洗完澡啦", "Out of the shower", "今天的晚霞超好看", "The sunset was gorgeous tonight", "你吃饭了没|不许吃泡面", "Have you eaten?|no instant noodles" } },
            /* Sweet */ new string[,] { { "还不睡的话|那我陪你[月亮]", "If you're not sleeping|then I'll stay up with you [月亮]", "晚安之前要先说想我", "Say you miss me before good night", "今天也很喜欢你[爱心]", "Liked you a lot today too [爱心]" }, { "下课第一件事就是看手机|看你回没回", "First thing after class I check my phone|to see if you replied", "我今天上课走神了|都怪你", "I zoned out in class today|your fault", "等放假了你来接我好不好", "Will you pick me up when the holidays start?" }, { "吃饭的时候室友问我在笑什么", "My roommate asked why I was smiling at dinner", "你在干嘛|我好无聊|想你", "What are you doing|I'm bored|miss you", "我把你的照片设成锁屏了[害羞]", "I set your photo as my lock screen [害羞]" } },
        };

        /// <summary>
        /// One offline reply (bubbles split by "|"): her tier's and the period's lines, none like her last five
        /// (<paramref name="similar"/> is the YY layer's XgSpeechPolicy.Similar; null compares exactly).
        /// </summary>
        public static string[] Pick(GirlfriendState s, DateTime clock, bool english, Func<string, string, bool> similar = null)
        {
            int t = (int)GirlfriendRules.Tier(s);
            var pool = new List<(string zh, string en)>();
            var tier = Tier[t];
            for (int i = 0; i + 1 < tier.Length; i += 2) pool.Add((tier[i], tier[i + 1]));
            var p = ByPeriod[t];
            int row = (int)PeriodOf(clock);
            for (int i = 0; i + 1 < p.GetLength(1); i += 2) pool.Add((p[row, i], p[row, i + 1]));
            similar = similar ?? ((a, b) => a == b);
            var fresh = new List<(string zh, string en)>();
            foreach (var line in pool)
            {
                bool seen = false;
                foreach (var r in s.recentLines) if (similar(r, line.zh) || similar(r, line.en)) { seen = true; break; }
                if (!seen) fresh.Add(line);
            }
            if (fresh.Count == 0) fresh = pool;
            var pick = fresh[GirlfriendRules.Pick(s, fresh.Count)];
            Note(s, pick.zh);
            return (english ? pick.en : pick.zh).Split('|');
        }

        /// <summary>Remembers a line she sent so the library does not repeat it (the last five).</summary>
        public static void Note(GirlfriendState s, string line)
        {
            if (s == null || string.IsNullOrEmpty(line)) return;
            s.recentLines.Add(line);
            while (s.recentLines.Count > 5) s.recentLines.RemoveAt(0);
        }

        /// <summary>A free topic when the model is off: one bubble pair per topic key.</summary>
        public static string[] Topic(string key, bool english)
        {
            switch (key)
            {
                case "roommate": return english ? new[] { "my roommate snores like a tractor", "I can't sleep [流汗]" } : new[] { "我室友打呼噜跟拖拉机一样", "根本睡不着[流汗]" };
                case "photo": return english ? new[] { "[图片] the cat at the dorm gate", "it ran when I tried to pet it" } : new[] { "[图片] 宿舍门口那只橘猫", "我一摸它就跑了" };
                case "drama": return english ? new[] { "Love O2O is so sweet", "why don't you play games like Xiao Nai" } : new[] { "《微微一笑很倾城》好甜", "你打游戏怎么就没肖奈那么帅" };
                case "food": return english ? new[] { "had hotpot with my roommates", "have you eaten?" } : new[] { "和室友吃了火锅", "你吃饭了没" };
                case "missYou": return english ? new[] { "nothing", "just wanted to say hi [害羞]" } : new[] { "没什么", "就是想找你说话[害羞]" };
                default: return english ? new[] { "what are you doing", "on the computer again?" } : new[] { "在干嘛", "又在弄电脑？" };
            }
        }
    }
}
