using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgDailyReceipt
    {
        public string condition = "", eventId = "", subjectId = "";
    }
    public sealed partial class XgState
    {
        // Only evidence needed to establish optional unlocks, not a replacement for activity history.
        public List<XgDailyReceipt> dailyReceipts = new List<XgDailyReceipt>();
    }
    public sealed class XgDailyNode
    {
        public readonly string id, condition, name, nameEn;
        public readonly int stage, price;
        public XgDailyNode(string id, string condition, int stage, int price, string name, string nameEn)
        { this.id = "daily." + id; this.condition = condition; this.stage = stage; this.price = price; this.name = name; this.nameEn = nameEn; }
    }
    public sealed class XgDailyProgress
    {
        public string condition, text, textEn;
        public int count, required;
        public bool complete => required > 0 && count >= required;
    }
    public static class XgDailyLife
    {
        static readonly XgDailyNode[] nodes = {
            new XgDailyNode("dice", "games3", 2, 200, "概率直觉", "Probability intuition"),
            new XgDailyNode("news", "news5", 2, 150, "时事", "Current affairs"),
            new XgDailyNode("surf", "tieba", 2, 120, "冲浪", "Surfing"),
            new XgDailyNode("bargain", "miaoyu", 3, 250, "砍价", "Bargaining"),
            new XgDailyNode("care", "gf", 3, 200, "体贴", "Considerate"),
            new XgDailyNode("slang", "memes5", 3, 180, "网络用语", "Internet slang"),
            new XgDailyNode("rhythm", "music3", 4, 300, "节奏", "Rhythm") };
        public static IEnumerable<XgDailyNode> Nodes { get { foreach (var n in nodes) yield return n; } }
        public static bool DisclosesBirthday(string text)
        {
            string value=(text??"").Replace(" ","").ToLowerInvariant();
            return XgDailyLifeCopy.ConfirmedBirthday(value);
        }
        public static void AddNodes(List<XgNode> list)
        {
            foreach (var n in nodes) list.Add(new XgNode { id=n.id, tree="research", lane="daily", target=n.id,
                kind=XgNodeKind.Research, stage=n.stage, cost=n.price, name=n.name, nameEn=n.nameEn,
                note=ConditionText(n.condition,false)+"。可选日常能力，不是升阶前置。",
                noteEn=ConditionText(n.condition,true)+". Optional daily ability; never required to advance." });
        }
        public static XgDailyNode Node(string id) { foreach (var n in nodes) if (n.id == id) return n; return null; }
        public static int Required(string condition)
        { switch (condition) { case "game1": case "board": case "tieba": case "miaoyu": return 1; case "games10": case "gf": return 10; case "news5": case "memes5": return 5; case "games3": case "music3": return 3; default: return 0; } }
        internal static string Bucket(string condition) => condition == "game1" || condition == "games3" ? "games10" : condition;
        internal static bool DistinctSubjects(string condition) => condition == "news5" || condition == "music3";
        public static bool BoardGame(string id) => id == "ludo" || id == "checkers_cn" || id == "monopoly";
        internal static bool Game(string id) => BoardGame(id) || id == "gomoku" || id == "go" || id == "chess" || id == "xiangqi_endgame" || id == "chess_mate";
        public static string ConditionText(string id, bool english)
        {
            switch (id) {
                case "game1": return english ? "Finish a game against your AI" : "和它下完任意一局";
                case "games3": return english ? "Finish 3 games against your AI (any board game)" : "和自己的AI下完任意棋类，共3局";
                case "games10": return english ? "Finish 10 games against your AI" : "和它下满10局";
                case "board": return english ? "Finish aeroplane chess, Chinese checkers or Monopoly with your AI" : "和它玩完一局飞行棋、跳棋或大富翁";
                case "news5": return english ? "Finish reading 5 different Bodu news articles" : "读完5篇不同的摆渡新闻";
                case "tieba": return english ? "Receive a reply to your own forum post" : "自己的贴吧帖子收到回复";
                case "miaoyu": return english ? "Complete a purchase on Miaoyu" : "在喵鱼完成一次购买";
                case "gf": return english ? "Send Qingwen 10 messages" : "和晴雯聊天，实际发送10句";
                case "memes5": return english ? "Send 5 meme stickers in the After-work group" : "在下班屁股群实际发送5次斗图";
                case "music3": return english ? "Finish listening to 3 different songs" : "完整听完3首不同的歌";
                default: return english ? "Unknown condition" : "未知条件";
            }
        }
    }
    public sealed partial class XgSim
    {
        static bool DailyKey(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256;
        List<XgDailyReceipt> DailyValid(string condition)
        {
            var found = new List<XgDailyReceipt>(); var keys = new HashSet<string>(StringComparer.Ordinal);
            var subjects = new HashSet<string>(StringComparer.Ordinal);
            string bucket = XgDailyLife.Bucket(condition); int cap = XgDailyLife.Required(bucket);
            if (S.dailyReceipts == null || cap == 0) return found;
            foreach (var r in S.dailyReceipts) {
                if (r == null || r.condition != bucket || !DailyKey(r.eventId) || !DailyKey(r.subjectId) || !keys.Add(r.eventId)) continue;
                if (XgDailyLife.DistinctSubjects(bucket) && !subjects.Add(r.subjectId)) continue;
                found.Add(r); if (found.Count == cap) break;
            }
            return found;
        }
        bool DailyCommit(string condition, string eventId, string subjectId)
        {
            if (!DailyKey(eventId) || !DailyKey(subjectId)) return false;
            var existing = DailyValid(condition);
            if (existing.Count >= XgDailyLife.Required(condition)) return false;
            foreach (var r in existing) if (r.eventId == eventId || XgDailyLife.DistinctSubjects(condition) && r.subjectId == subjectId) return false;
            if (S.dailyReceipts == null) S.dailyReceipts = new List<XgDailyReceipt>();
            S.dailyReceipts.Add(new XgDailyReceipt { condition = condition, eventId = eventId, subjectId = subjectId });
            DailyNoticeTransition(condition, existing.Count, existing.Count + 1);
            return true;
        }
        public XgDailyProgress DailyProgress(string condition) => new XgDailyProgress { condition = condition,
            count = Math.Min(XgDailyLife.Required(condition), DailyValid(condition).Count), required = XgDailyLife.Required(condition),
            text = XgDailyLife.ConditionText(condition, false), textEn = XgDailyLife.ConditionText(condition, true) };
        // Hosts call these only after the named operation actually commits. Reuse the same persisted eventId on replay.
        public bool RecordDailyGameCompleted(string gameId, string completionId, bool againstPrivateAi)
        {
            if (!againstPrivateAi || !XgDailyLife.Game(gameId)) return false;
            bool changed = DailyCommit("games10", completionId, gameId);
            if (XgDailyLife.BoardGame(gameId)) changed |= DailyCommit("board", completionId, gameId);
            return changed;
        }
        public bool DailyNewsAlreadyRead(string articleId) { foreach(var r in DailyValid("news5")) if(r.subjectId==articleId)return true; return false; }
        public bool RecordDailyNewsRead(string articleId, string completionId) => DailyCommit("news5", completionId, articleId);
        public bool RecordDailyMiaoyuPurchase(string transactionId, string itemId) => DailyCommit("miaoyu", transactionId, itemId);
        public bool RecordDailyOwnPostReply(string threadId, string replyId, bool ownThread, bool otherAuthor)
            => ownThread && otherAuthor && DailyCommit("tieba", replyId, threadId);
        public bool RecordDailySentMessage(string conversationId, string messageId, string text, string stickerId = "")
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (conversationId == "qingwen" && string.IsNullOrEmpty(stickerId)) return DailyCommit("gf", messageId, conversationId);
            return conversationId == "netbar" && DailyKey(stickerId) && DailyCommit("memes5", messageId, stickerId);
        }
        public bool RecordDailySongCompleted(string songId, string playbackId) => DailyCommit("music3", playbackId, songId);
        public bool DailyNodeReady(string id) { var n = XgDailyLife.Node(id); return n != null && S.stage >= n.stage && DailyProgress(n.condition).complete; }
        public string DailyNodeBlocker(string id)
        {
            var n = XgDailyLife.Node(id); if (n == null) return null;
            if (S.stage < n.stage) return T("阶段", "Stage ") + n.stage;
            var p = DailyProgress(n.condition); return p.complete ? null : T(p.text, p.textEn) + " (" + p.count + "/" + p.required + ")";
        }
        public bool DailyProbabilityQuestions => Has("daily.dice");
        public bool DailyRiskEnhancementAvailable(bool actualRiskForecastOwned) => DailyProbabilityQuestions && actualRiskForecastOwned;
        public bool DailyNewsQuestions => Has("daily.news");
        public bool DailyDataLinkHighlight => Has("daily.surf");
        public bool DailyBargainingAvailable => Has("daily.bargain");
        public bool DailyConsiderateTag => Has("daily.care");
        public bool DailyKnownDateReminders => Has("daily.care");
        public bool DailySlangHelp => Has("daily.slang");
        public bool DailyRhythmFeedback => Has("daily.rhythm");
    }
}
