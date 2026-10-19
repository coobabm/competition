using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Forum
{
    [Serializable] public sealed class LaoZhouNewsReceipt
    {
        public string articleId = "", channel = "", sourceUrl = "", sharedText = "", reply = "";
        public double sharedAt;
    }

    /// <summary>Three optional, source-scoped replies. Never infers a share from private progress or reading.</summary>
    public static class LaoZhouNewsComments
    {
        public const string Yy = "yy_dm", Tieba = "tieba_dm";
        public static bool Supported(string id) => id == "lz2_t01_alphago" || id == "lz2_t03_tay" || id == "lz2_t04_resnet";
        public static LaoZhouNewsReceipt Receipt(LaoZhouQaState state, string id)
            => Supported(id) ? state?.newsComments?.Find(r => r != null && r.articleId == id) : null;
        // Legacy contact preserves old chat access, but does not prove the new explicit Add-on-YY action.
        public static string Channel(LaoZhouQaState state) => state?.yyLinkSource == "tieba_dm_accept" ? Yy : Tieba;
        public static bool CanShare(LaoZhouQaState state, string id, string channel, DateTime publication, DateTime today)
        {
            if(state == null || !Supported(id) || channel != Channel(state) || Receipt(state,id) != null) return false;
            DateTime earliest = id == "lz2_t01_alphago" ? new DateTime(2016,3,16)
                : id == "lz2_t03_tay" ? new DateTime(2016,3,26) : new DateTime(2016,1,1);
            return publication.Year == 2016 && publication.Date >= earliest && today.Date >= publication.Date;
        }
        public static string Reply(string id, bool en)
        {
            switch(id)
            {
                case "lz2_t01_alphago": return LaoZhouQuestions.T(en,"围棋这件事，它证明过了。别的事情，还得另外试。","It proved itself at Go. Other jobs need their own tests.");
                case "lz2_t03_tay": return LaoZhouQuestions.T(en,"先分清：它这次被诱导着说了什么，和它以后都学成了什么。不是看见一句坏话，就能断定里面改了哪一处。","Separate what it was prompted to say this time from what it may have learned for later. One offensive reply cannot tell us which internal part changed.");
                case "lz2_t04_resnet": return LaoZhouQuestions.T(en,"看来结构怎么搭，也得算进账里。光数层数不够。","It looks as though the structure matters too. Counting layers is not enough.");
                default: return "";
            }
        }
        public static string SharedText(string title, string url, bool en)
            => LaoZhouQuestions.T(en,"这篇新闻，你怎么看？","What do you think of this article?") + "\n" + title + "\n" + url;
        public static string Label(string channel, bool sent, bool en)
        {
            string destination = channel == Yy ? "YY" : LaoZhouQuestions.T(en,"贴吧私信","Tieba DM");
            return LaoZhouQuestions.T(en,sent ? "已发给老周" : "发给老周",sent ? "Sent to Lao Zhou" : "Send to Lao Zhou") + " (" + destination + ")";
        }
        // The host calls this only as part of the explicit, synchronous message append. No background queue.
        public static bool Record(LaoZhouQaState state, string id, string channel, DateTime publication, DateTime today,
            string sourceUrl, string sharedText, bool en, double at)
        {
            if(!CanShare(state,id,channel,publication,today) || string.IsNullOrWhiteSpace(sharedText)
                || !Uri.TryCreate(sourceUrl,UriKind.Absolute,out var source) || source.Scheme != "https"
                || double.IsNaN(at) || double.IsInfinity(at) || at < 0) return false;
            if(state.newsComments == null) state.newsComments = new List<LaoZhouNewsReceipt>();
            state.newsComments.Add(new LaoZhouNewsReceipt {articleId=id,channel=channel,sourceUrl=sourceUrl,
                sharedText=sharedText,reply=Reply(id,en),sharedAt=at});
            return true;
        }
    }
}
