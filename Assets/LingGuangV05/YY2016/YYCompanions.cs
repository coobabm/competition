using System;
using LingGuangV05.Core;
using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.YY
{
    public static partial class YYGroup
    {
        public static string FutureNameReply(string query)
        {
            string q = (query ?? "").ToLowerInvariant();
            if (q.Contains("gpt")) return GameText.T("[大伟] GPT？硬盘分区表那个？\n[小刚] 会说话的分区表，你电脑成精了？", "[Da Wei] GPT? The partition table?\n[Xiao Gang] Your hard disk talks now?");
            if (q.Contains("claude")) return GameText.T("[莉莉] 克劳德？法国人？\n[阿杰] 还 code 呢，你又接外包了？", "[Lili] Claude? A French guy?\n[A-Jie] Code? Another freelance job?");
            if (q.Contains("豆包")) return GameText.T("[小刚] 豆包不是吃的吗？\n[阿杰] 大半夜别说吃的，我饿了。", "[Xiao Gang] Isn't that a bean bun?\n[A-Jie] Stop talking about food, I'm hungry.");
            if (q.Contains("deepseek")) return GameText.T("[大伟] deep 什么 seek，捉迷藏？\n[小刚] 藏得挺深，确实没听过。", "[Da Wei] Deep what seek? Hide-and-seek?\n[Xiao Gang] Well hidden. Never heard of it.");
            if (q.Contains("千问")) return GameText.T("[莉莉] 屈原那个不是叫天问吗？\n[阿杰] 千问？老师布置的题都没这么多。", "[Lili] Isn't Qu Yuan's poem called Heavenly Questions?\n[A-Jie] A thousand questions? Even school wasn't that bad.");
            if (q.Contains("文心一言")) return GameText.T("[大伟] 文心雕龙我倒是听过。\n[小刚] 今天群里怎么开始上语文课了？", "[Da Wei] I've heard of The Literary Mind.\n[Xiao Gang] Is this a literature class now?");
            return null;
        }
    }

    public sealed partial class YYChatHub
    {
        static string LastPlayerLine(YYConversation c)
        {
            if (c != null) for (int i = c.messages.Count - 1; i >= 0; i--) if (c.messages[i].from == Me) return c.messages[i].text;
            return "";
        }

        void TickFriend()
        {
            if (runtime.TestMode || runtime.Sim.InPrologue || !S.lingguangUnlocked) return;
            int month = GameCalendar.Now(runtime.Sim.S).Month;
            if (S.friendMonth >= month) return;
            // One current-month message, never a burst of backlogged months on loading an old save.
            S.friendMonth = month;
            string[] zh = { "", "", "", "", "", "还窝在电脑前呢？出门晒晒，快长蘑菇了。", "散伙饭吃完了，平时最吵的那个哭得最凶。你当年是不是也这样？", "[图片] 青海湖边的油菜花。风大得头发全糊脸上，给你挑了张没人的。", "女排那场我看完天都亮了！隔壁敲墙让我小点声。", "开始上班才知道，早八原来算宽松的。", "[图片] 国庆景区门口。看人比看山多，你在家待着也挺明智。", "说好不买，零点还是下了一单。别笑，你 Steam 库里那堆游戏玩了吗？", "想回家了。过年找个晚上出来吃饭，别又说等下载完。" };
            string[] en = { "", "", "", "", "", "Still at your PC? Go get some sun before you grow mushrooms.", "Graduation dinner is over. The loudest one cried the most. Were you like that too?", "[Photo] Rapeseed flowers beside Qinghai Lake. The wind ruined every selfie, so here's the scenery.", "Watched the volleyball match till dawn! The neighbour knocked on the wall.", "Started working. Suddenly 8 a.m. lectures seem generous.", "[Photo] National Day crowds. More people than mountains. Staying home was smart.", "Swore I wouldn't shop. Ordered at midnight anyway. Played all those Steam games yet?", "Want to go home. Let's eat together over New Year. Don't say you're waiting for a download." };
            Receive("oldfriend", GameText.T(zh[Math.Max(5, month)], en[Math.Max(5, month)]));
        }

        string FriendFallback(string query, YYConversation conv)
        {
            string q = (query ?? "").ToLowerInvariant();
            string[] zh, en;
            if (q.Contains("旅游") || q.Contains("照片") || q.Contains("风景") || q.Contains("travel")) {
                zh = new[] { "我还存了好多张，回头给你看。你也别光看照片，找天出门。", "下次一起去啊，你负责拎包，我负责迷路。", "拍出来真没现场好看，尤其那个风，照片里吹不到。" };
                en = new[] { "I've got more photos. But go see somewhere yourself too.", "Come next time. You carry the bags; I'll get us lost.", "The photo misses the best part: the wind." };
            } else if (q.Contains("忙") || q.Contains("累") || q.Contains("work")) {
                zh = new[] { "忙完吱一声，别把饭忘了。", "行，那我不吵你。记得起来走两步。", "咱俩下次碰面比比谁黑眼圈大。" };
                en = new[] { "Message me when you're done. Don't skip dinner.", "I'll leave you to it. Remember to stretch.", "Next time let's compare our eye bags." };
            } else {
                zh = new[] { "你还是那个德行，哈哈。然后呢？", "等会儿，我认真看一遍。你是说刚才那事？", "行啊，下次见面当面跟我讲。", "这话也就跟我说说，换别人早不理你了。", "我在路上，信号一阵一阵的。你接着说。", "看到了，刚在吃饭。" };
                en = new[] { "Same old you, haha. What happened next?", "Wait, let me read that again. You mean the thing you mentioned?", "Tell me about it when we meet.", "Only I put up with you saying that.", "On the road, patchy signal. Keep going.", "Just saw it. I was eating." };
            }
            int first = offlineRng.Next(zh.Length);
            for (int n = 0; n < zh.Length; n++) {
                int i = (first + n) % zh.Length; string line = GameText.T(zh[i], en[i]);
                if (!RepeatsInYY(conv, line)) return line;
            }
            return GameText.T("那你现在打算怎么办？", "What are you going to do now?");
        }

        void TrackGirlfriendStage()
        {
            var lab = FindLab(); if (lab == null) return;
            int stage = lab.S.stage;
            if (S.gfStage == stage) return;
            S.gfIgnoredStage = S.gfStage == stage - 1 && S.gfReceived > 0 && S.gfReplies == 0 ? S.gfStage : 0;
            S.gfStage = stage; S.gfReceived = S.gfReplies = 0;
        }

        void NoteGirlfriendReply(string text)
        {
            TrackGirlfriendStage();
            if (S.gfIgnoredStage == S.gfStage - 1 && S.gfIgnoredStage > 0
                && Array.Exists(new[] { "在忙", "抱歉", "不好意思", "没看到", "刚看到", "sorry", "busy" }, k => text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                FindLab()?.EarnSecret("life.gf.busy");
            S.gfReplies++;
        }
    }
}
