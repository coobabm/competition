using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Era;
using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.YY
{
    /// <summary>
    /// 下班屁股群: how many are in it (the title shows 「下班屁股群（23）」), the four who talk, what was said there
    /// lately (the yy lines of era_events.json, 莉莉's trips included) and the preset lines it answers with when the
    /// local model is off.
    /// </summary>
    public static partial class YYGroup
    {
        /// <summary>Members in the title; four talk, the rest lurk.</summary>
        public const int Size = 23;

        public static readonly (string name, string nameEn, string who)[] Members =
        {
            ("阿杰", "A-Jie", "二十出头，网管出身，懂显卡和超频，爱吹牛，抽卡手黑，Steam 库里一堆没玩的游戏"),
            ("大伟", "Da Wei", "三十岁程序员，天天加班，骑士球迷，爱看电影，爱抱怨"),
            ("小刚", "Xiao Gang", "刚上班的小年轻，英雄联盟、文明玩家，嘴贫，看球熬夜"),
            ("莉莉", "Lili", "今年 6 月刚毕业，爱在群里分享自己的生活，出去玩一定发照片，说话爱用～和哈哈哈"),
        };

        /// <summary>Title with the member count: 「下班屁股群（23）」.</summary>
        public static string Title(YYContact c) => GameText.T(c.name, c.nameEn) + GameText.T("（" + Size + "）", " (" + Size + ")");

        /// <summary>For the model: the group's dated lines of the last three weeks, newest first.</summary>
        public static string Recent(DateTime today)
        {
            var sb = new StringBuilder();
            int n = 0;
            foreach (var e in EraContent.Events.Visible(EraEvents.YY, today))
            {
                if ((today.Date - e.date).TotalDays > 21 || n >= 4) break;
                sb.Append(n == 0 ? "群里最近的事（新的在前）：" : "；").Append(e.who).Append(" ").Append(e.date.Month).Append(" 月 ").Append(e.date.Day).Append(" 日说「").Append(e.text).Append("」");
                n++;
            }
            return n > 0 ? sb.Append("。可以顺着这些聊，别原样重复。").ToString() : "";
        }

        // Keywords, then zh / en line pairs. The last bucket (no keywords) is the default.
        static readonly (string[] keys, string[] lines)[] Pool =
        {
            (new[] { "屁股", "守望", "开黑", "上号", "游戏", "lol", "撸", "overwatch" }, new[] {
                "[阿杰] 来来来，上号，就差你了", "[A-Jie] Get on, we're one short",
                "[小刚] 今晚我玩源氏，谁也别抢", "[Xiao Gang] I'm Genji tonight, hands off",
                "[大伟] 等我下班，九点以后", "[Da Wei] After work. Nine-ish",
                "[阿杰] 昨晚连跪五把，今天我要洗刷耻辱", "[A-Jie] Lost five straight last night. Revenge today" }),
            (new[] { "加班", "上班", "老板", "领导", "累", "困", "work", "boss", "tired" }, new[] {
                "[大伟] 别提了，我今天又要加到十点", "[Da Wei] Don't. Here till ten again",
                "[小刚] 困得睁不开眼", "[Xiao Gang] Feeling hollowed out",
                "[莉莉] 你们上班的好惨，我先去躺会儿", "[Lili] You working people have it rough. I'm going for a nap",
                "[大伟] 我们老板又说要拥抱互联网+", "[Da Wei] My boss says we must embrace Internet Plus again" }),
            (new[] { "吃", "饭", "饿", "外卖", "宵夜", "food", "hungry" }, new[] {
                "[阿杰] 楼下黄焖鸡，十五块管饱", "[A-Jie] Braised chicken downstairs, fifteen yuan, fills you up",
                "[莉莉] 别馋我，我在减肥", "[Lili] Don't tempt me, I'm on a diet",
                "[小刚] 饿了么红包谁有，发一个", "[Xiao Gang] Anyone got an Ele.me coupon? Send one" }),
            (new[] { "钱", "工资", "穷", "显卡", "1080", "1070", "money", "salary", "gpu" }, new[] {
                "[阿杰] 1080 太贵，1070 才是真性价比", "[A-Jie] The 1080's too pricey. The 1070 is the real deal",
                "[大伟] 工资还没发，月底吃土", "[Da Wei] Payday hasn't come. Eating dirt till month end",
                "[小刚] 穷，下一个", "[Xiao Gang] Broke. Next question" }),
            (new[] { "灵光", "训练", "人工智能", "阿尔法", "alphago", "模型" }, new[] {
                "[大伟] 阿尔法狗那场我看了，围棋规则还是没弄懂", "[Da Wei] I watched that AlphaGo match. Still do not understand Go.",
                "[小刚] 阿尔法狗都赢李世石了，这新闻够聊一阵了", "[Xiao Gang] AlphaGo beat Lee Sedol. That will be news for a while.",
                "[阿杰] 这方面我也不懂，先看看新闻吧", "[A-Jie] I do not know much about that. I just follow the news.",
                "[莉莉] 听不懂，但感觉很厉害的样子", "[Lili] No idea what that means, but it sounds impressive" }),
            (new[] { "球", "奥运", "欧洲杯", "nba", "女排", "比赛", "football", "olympic" }, new[] {
                "[小刚] 今晚熬夜看球的扣 1", "[Xiao Gang] Type 1 if you're staying up for the match",
                "[大伟] 几点开始？我定个闹钟", "[Da Wei] What time? I'll set an alarm",
                "[莉莉] 我只看帅哥，比分你们告诉我", "[Lili] I only watch for the cute ones. Tell me the score" }),
            (new[] { "在吗", "有人", "人呢", "hello", "嗨" }, new[] {
                "[阿杰] 在，咋了", "[A-Jie] Here. What's up",
                "[莉莉] 冒泡～", "[Lili] *bubbles up*～",
                "[小刚] 有事说事", "[Xiao Gang] Spit it out" }),
            (new string[0], new[] {
                "[阿杰] 233333", "[A-Jie] 233333",
                "[小刚] 666", "[Xiao Gang] 666",
                "[莉莉] +1", "[Lili] +1",
                "[大伟] 有道理", "[Da Wei] Fair point",
                "[阿杰] 你说啥？刚在打团", "[A-Jie] What? I was in a team fight",
                "[莉莉] 哈哈哈哈哈", "[Lili] Hahahahaha",
                "[小刚] 我就静静地看着你们", "[Xiao Gang] I'll just quietly watch you all",
                "[大伟] 这个可以有", "[Da Wei] Now that I can get behind" }),
        };

        // A human contact has no access to the owner's private application. Keep this
        // check narrow so ordinary discussion of public AI news or annotation jobs survives.
        public static bool RevealsPrivateLab(string text, string privateName = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string lower = text.ToLowerInvariant();
            foreach (string phrase in new[] { "灵光.exe", "小光.exe", "你的训练", "你那个程序", "那个程序的", "你训练的", "你的模型", "你的ai", "你的 ai", "灵光训练", "灵光的", "给灵光", "lingguang.exe", "your training pack", "your model", "your ai", "still messing with that program", "what does yours do" })
                if (lower.Contains(phrase)) return true;
            if ((lower.Contains("训练包") || lower.Contains("training pack")) &&
                (lower.Contains("你") || lower.Contains("给") || lower.Contains("准备") || lower.Contains("your"))) return true;
            if (string.IsNullOrWhiteSpace(privateName)) privateName = LingGuangV05.Core.AppNames.AiZh;
            if (!string.IsNullOrWhiteSpace(privateName) && lower.Contains(privateName.ToLowerInvariant()))
                foreach (string fact in new[] { "训练", "样本", "数据包", "模型", ".exe", "学会", "阶段" })
                    if (lower.Contains(fact)) return true;
            return false;
        }

        public static int MigratePrivateKnowledge(YYState state, string privateName)
        {
            int changed = 0;
            var group = state?.conversations?.Find(c => c.id == "netbar");
            if (group == null) return changed;
            foreach (var message in group.messages)
                if (message.from != YYChatHub.Me && message.kind == YYKind.Text && RevealsPrivateLab(message.text, privateName))
                {
                    message.text = PrivateLabReplacement(message.text);
                    changed++;
                }
            return changed;
        }

        public static string PrivateLabReplacement(string original)
        {
            string prefix = "[阿杰] ";
            if (!string.IsNullOrEmpty(original) && original.StartsWith("[", StringComparison.Ordinal))
            {
                int end = original.IndexOf(']');
                if (end > 0 && end < 30) prefix = original.Substring(0, end + 1) + " ";
            }
            return prefix + GameText.T("你说的这个我没用过。", "I haven't used what you're talking about.");
        }

        /// <summary>A preset group line for <paramref name="last"/> (the player's line), not one said in the last few group lines.</summary>
        public static string Fallback(string last, YYConversation conv, Random rng)
        {
            string q = (last ?? "").ToLowerInvariant();
            var bucket = Pool[Pool.Length - 1];
            foreach (var b in Pool) if (Array.Exists(b.keys, k => q.Contains(k))) { bucket = b; break; }
            var recent = new HashSet<string>();
            if (conv != null) for (int i = conv.messages.Count - 1, seen = 0; i >= 0 && seen < 6; i--, seen++) recent.Add(conv.messages[i].text);
            int pairs = bucket.lines.Length / 2, start = rng.Next(pairs);
            string pick = null;
            for (int i = 0; i < pairs; i++)
            {
                int j = (start + i) % pairs;
                pick = GameText.T(bucket.lines[j * 2], bucket.lines[j * 2 + 1]);
                if (!recent.Contains(pick)) break;
            }
            return pick;
        }
    }
}
