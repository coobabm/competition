using System;
using System.Collections.Generic;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.YY;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Bodu
{
    public sealed partial class BoduView
    {
        static bool newsLoaded;
        string newsId="";
        static void EnsureNews()
        {
            if(newsLoaded)return;
            var asset=Resources.Load<TextAsset>("LingGuangV05/Browser/browser_news");
            if(asset==null)return;XgNewsCatalog.Load(asset.text);newsLoaded=true;
        }
        static List<XgNewsArticle> AvailableNews(DateTime today){EnsureNews();return XgNewsCatalog.Available(today);}
        void DataNewsRow(XgNewsArticle article)
        {
            string ad=article.kind=="水文"?T(" · 推广"," · Sponsored"):"";
            Action open=article.brief?null:()=>{newsId=article.id;page="news";signature="";};
            Line("<color=#1A0DAB>"+T(article.title,article.titleEn)+"</color>\n<size=12><color=#787878>"+article.date+ad+"</color></size>",18,Ink,open);
        }
        RectTransform NewsColumn(RectTransform parent,string name,float share)
        {
            var column=PrologueDesk.Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var size=column.gameObject.AddComponent<LayoutElement>();size.flexibleWidth=share;size.minWidth=share>1?260:180;
            var layout=column.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=12;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
            return column;
        }
        void DataNewsHome(DateTime today)
        {
            EnsureNews();var lab=TiebaHub.Lab();int stage=lab?.S.stage??1;
            var available=XgNewsCatalog.Home(today,stage);var original=content;RectTransform hot=null;
            if(stage>=6)
            {
                var row=PrologueDesk.Rect("News and trends",content,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                var layout=row.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=24;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
                content=NewsColumn(row,"Latest news",3);hot=NewsColumn(row,"Hot searches",1);
            }
            for(int i=0;i<Math.Min(8,available.Count);i++)DataNewsRow(available[i]);
            if(available.Count==0)Line(T("今天还没有新闻。","No headlines yet today."),16,Muted);
            if(available.Count>8)Line(T("<u>更多往日新闻 ›</u>","<u>Older headlines ›</u>"),15,Blue,()=>{page="news.archive";signature="";});
            if(hot!=null)
            {
                content=hot;Line(T("热搜","Trending"),21,Ink);
                for(int i=0;i<Math.Min(5,available.Count);i++)DataNewsRow(available[i]);
                var hub=TiebaHub.Instance;
                if(hub!=null)
                {
                    Line(T("贴吧正在聊","On the forum"),18,Ink);int count=0;
                    foreach(var thread in hub.Library.Threads)if(Core.Forum.ForumLibrary.Visible(thread,hub.Context()))
                    {var selected=thread;Line(T(thread.title,thread.titleEn),15,Blue,()=>hub.View?.Open("thread",selected.id));if(++count==3)break;}
                }
            }
            content=original;
        }
        void DataNewsPage(DateTime today,bool archive)
        {
            Line(T("<u>返回新闻首页</u>","<u>Back to headlines</u>"),16,Blue,()=>{page="home";signature="";});
            foreach(var a in AvailableNews(today))
            {
                if(archive){DataNewsRow(a);continue;}if(a.id!=newsId||a.brief)continue;
                Line(T(a.title,a.titleEn),25,Ink);Line(a.date+(a.kind=="水文"?T(" · 推广"," · Sponsored"):""),13,Muted);
                Line(T(a.body??"",a.bodyEn??""),18,Ink);
                var zhou = TiebaHub.Instance;
                if(zhou?.ZhouQa != null && LaoZhouNewsComments.Supported(a.id))
                {
                    var article = a; var expected = zhou.S; var expectedYy = YYChatHub.Instance?.S;
                    var shared = LaoZhouNewsComments.Receipt(zhou.ZhouQa,a.id);
                    string channel = shared?.channel ?? LaoZhouNewsComments.Channel(zhou.ZhouQa);
                    Line(LaoZhouNewsComments.Label(channel,shared != null,GameText.IsEnglish),16,shared != null ? Muted : Blue,shared != null ? (Action)null : () =>
                    {
                        if(!ReferenceEquals(zhou,TiebaHub.Instance) || window == null || !window.isOn || page != "news" || newsId != article.id)return;
                        if(zhou.ShareNewsWithZhou(article.id,channel,expected,expectedYy))signature="";
                    });
                    // An inline view of the reply earned by this share; opening/reading the article never earns one.
                    if(shared != null)
                    {
                        Line(T("老周：","Lao Zhou: ")+LaoZhouNewsComments.Reply(a.id,GameText.IsEnglish),16,Ink);
                        Line(AiContentNotice.Footer(GameText.IsEnglish),12,Muted);
                    }
                }
                var lab=TiebaHub.Lab();
                if(lab!=null)
                {
                    var article=a; var owner=lab; string receipt="news-read:"+a.id;
                    bool read=owner.DailyNewsAlreadyRead(a.id);
                    Line(read?T("已读完", "Finished reading"):T("<u>我已读完这篇 ›</u>", "<u>I finished reading this article ›</u>"),16,read?Muted:Blue,read?(Action)null:()=>
                    {
                        if(!ReferenceEquals(owner,TiebaHub.Lab())||window==null||!window.isOn||page!="news"||newsId!=article.id)return;
                        // Revalidate publication and body eligibility at the actual confirmation.
                        foreach(var current in AvailableNews(Save()!=null?LingGuangV05.Core.GameCalendar.Now(Save()):today))
                            if(current.id==article.id&&!current.brief&&!string.IsNullOrWhiteSpace(current.body)&&!string.IsNullOrWhiteSpace(current.bodyEn))
                            {owner.RecordReadNewsArticle(current.id,receipt);signature="";break;}
                    });
                }
                if(lab!=null&&XgNewsCatalog.Teachable(a))
                {
                    bool taught=lab.NewsAlreadyTaught(a.id);var article=a;
                    Line(taught?T("已交给","Already given to ")+lab.AiDisplayName:T("<u>转给它看 ›</u>","<u>Show it this article ›</u>"),16,taught?Muted:Blue,taught?(Action)null:()=>{if(lab.TeachNews(article.id))signature="";});
                }
                var related=XgNewsCatalog.Related(a.id,today);
                if(related.Count>0){Line(T("相关阅读","Related reading"),20,Ink);foreach(var other in related)DataNewsRow(other);}
                break;
            }
        }
    }
}
