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
            var now = GameCalendar.Now(runtime.Sim.S);
            int month = now.Month;
            if (S.friendMonth >= month) return;
            // 考研党: December's line is the last week before the exam, not the first of the month.
            if (month == 12 && now.Day < 17) return;
            // One current-month message, never a burst of backlogged months on loading an old save.
            S.friendMonth = month;
            string[] zh = { "", "", "", "", "",
                "论文终于交了。你呢，答辩准备得怎么样？",
                "答辩完了，回学校收拾宿舍。四年的东西，一个行李箱就装完了，有点难受。你那边呢？",
                "在图书馆占座，早上七点半就排队，比抢火车票还凶。你还窝在电脑前呢？",
                "报了个考研班，一天坐八个钟头，腰都直不起来。老师说现在不拼，十二月哭都没地方哭。",
                "九月了，离考试只剩一百来天。我把倒计时贴在床头，每天撕一页，越撕越慌。",
                "国庆不回家了，图书馆照常开门，人比平时还多。你呢，工作有着落没？",
                "现场确认明天就截止了，我还在纠结到底报哪所，改来改去一晚上了。帮我拿个主意？",
                "离考试没几天了，复习到看见字就想吐。考完请你吃饭，你可别又说等下载完。你一个人撑了大半年都没喊过累，我这几天也能撑住。" };
            string[] en = { "", "", "", "", "",
                "Thesis finally handed in. How's your defence prep?",
                "Defence is done. I went back to pack up the dorm. Four years fit in one suitcase. Feels strange. How about you?",
                "Holding a seat at the library, queuing from half past seven, worse than buying train tickets. Still stuck at your PC?",
                "Signed up for a cram class: eight hours a day, my back is gone. The teacher says if we don't push now, we'll cry in December.",
                "It's September. About a hundred days to the exam. I stuck a countdown by my bed and tear off a page a day. Gets scarier every time.",
                "Not going home for National Day. The library is open and busier than usual. Any luck with a job?",
                "Confirmation closes tomorrow and I still can't decide which school to pick. Changed my mind all night. Help me choose?",
                "A few days to the exam. I feel sick at the sight of words. I'll treat you to a meal afterwards. Don't say you're waiting for a download. You held on alone for most of a year without once complaining, so I can hold on these few days." };
            Receive("oldfriend", GameText.T(zh[Math.Max(5, month)], en[Math.Max(5, month)]));
        }

        string FriendFallback(string query, YYConversation conv)
        {
            string q = (query ?? "").ToLowerInvariant();
            string[] zh, en;
            if (q.Contains("考研") || q.Contains("复习") || q.Contains("图书馆") || q.Contains("背") || q.Contains("study") || q.Contains("exam")) {
                zh = new[] { "今天背了三十页，脑子里全是政治大题。", "英语阅读错了一半，我先去洗把脸冷静冷静。", "我书包放座位上就不敢走远，怕被人占了。", "等考完了，我要睡三天三夜。" };
                en = new[] { "Thirty pages memorised today. My head is all politics essays.", "Got half the English reading wrong. Going to splash my face.", "I leave my bag on the seat and never wander far, in case someone takes it.", "When it's over I'm sleeping for three days." };
            } else if (q.Contains("忙") || q.Contains("累") || q.Contains("work")) {
                zh = new[] { "忙完吱一声，别把饭忘了。", "行，那我不吵你。记得起来走两步。", "咱俩下次碰面比比谁黑眼圈大。" };
                en = new[] { "Message me when you're done. Don't skip dinner.", "I'll leave you to it. Remember to stretch.", "Next time let's compare our eye bags." };
            } else {
                zh = new[] { "你还是那个德行，哈哈。然后呢？", "等会儿，我认真看一遍。你是说刚才那事？", "行啊，下次见面当面跟我讲。", "这话也就跟我说说，换别人早不理你了。", "我在去图书馆的路上，信号一阵一阵的。你接着说。", "看到了，刚在吃饭。" };
                en = new[] { "Same old you, haha. What happened next?", "Wait, let me read that again. You mean the thing you mentioned?", "Tell me about it when we meet.", "Only I put up with you saying that.", "On my way to the library, patchy signal. Keep going.", "Just saw it. I was eating." };
            }
            // The longer he keeps at it, the more they say what they see in him (never in reply to every line).
            var lab = FindLab();
            int rank = lab != null ? lab.S.stage : 1;
            if (rank >= 3 && offlineRng.Next(3) == 0) {
                zh = rank >= 5
                    ? new[] { "我背书背到崩溃的时候就想想你：没人逼你，你自己一个人撑到现在。我也能撑。", "说真的，你比我活得明白。我闷头考研，你在外面真把东西做出来了。" }
                    : new[] { "说真的，我闷头背书，你在外面自己折腾，有时候我挺羡慕你。", "你这个人，看着吊儿郎当，其实做事比谁都认真。" };
                en = rank >= 5
                    ? new[] { "When I'm cracking over my books I think of you: nobody pushes you, and you've kept going alone. So can I.", "Honestly, you see life more clearly than I do. I cram for the exam; you actually built something out there." }
                    : new[] { "Honestly, I cram in the library while you tinker out there on your own, and sometimes I envy you.", "You look like a slacker, but you take things more seriously than anyone." };
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
