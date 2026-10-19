using System;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.Tieba
{
    public sealed partial class TiebaHub
    {
        // Expected forum/YY owners and the displayed destination reject clicks from a replaced save or route.
        public bool ShareNewsWithZhou(string articleId, string channel, ForumState expected, YYState expectedYy)
        {
            if(S == null || !ReferenceEquals(S,expected) || runtime?.Sim == null
                || !ReferenceEquals(runtime.Sim,linked) || runtime.Sim.InPrologue || !ZhouIdle()
                || IsTyping(ForumLibrary.LaoZhou) || ChoicesFor == ForumLibrary.LaoZhou) return false;
            var yy = YYChatHub.Instance;
            if(channel == LaoZhouNewsComments.Yy && (yy == null || !ReferenceEquals(yy.S,expectedYy)
                || !yy.BoundTo(runtime.Sim) || !yy.ZhouContactReady || !yy.AppInstalled)) return false;
            var article = XgNewsCatalog.Get(articleId);
            if(article == null || article.brief || string.IsNullOrWhiteSpace(article.body) || string.IsNullOrWhiteSpace(article.bodyEn)
                || !DateTime.TryParseExact(article.date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var published)) return false;
            bool en = GameText.IsEnglish;
            string text = LaoZhouNewsComments.SharedText(en ? article.titleEn : article.title,article.sourceUrl,en);
            if(!LaoZhouNewsComments.Record(ZhouQa,articleId,channel,published,GameCalendar.Now(runtime.Sim.S),article.sourceUrl,text,en,Now)) return false;
            string reply = LaoZhouNewsComments.Reply(articleId,en);
            if(channel == LaoZhouNewsComments.Yy)
            {
                yy.AppendZhouAuthored(YYChatHub.Me,text);
                yy.AppendZhouAuthored(YYChatHub.LaoZhou,reply);
            }
            else
            {
                ZhouForumLine(ForumLibrary.LaoZhou,ForumLibrary.Me,text);
                ZhouForumLine(ForumLibrary.LaoZhou,ForumLibrary.LaoZhou,reply);
            }
            // Only the actual shared title/link enter his knowledge. No private reading or training receipts.
            LaoZhouQuestions.Observe(ZhouQa,"news-share:" + articleId,channel,text,Now);
            if(channel == LaoZhouNewsComments.Yy) yy.RefreshZhouAuthored();
            Touch();
            return true;
        }
    }
}
