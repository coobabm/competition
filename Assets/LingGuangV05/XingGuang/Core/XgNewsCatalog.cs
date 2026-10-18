using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core.Story;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgNewsArticle
    { public string id,date,kind,title,titleEn,body,bodyEn,sourceUrl; public bool brief; }
    public static class XgNewsCatalog
    {
        static readonly List<XgNewsArticle> articles=new List<XgNewsArticle>();
        public static IReadOnlyList<XgNewsArticle> All=>articles;
        static string S(Dictionary<string,object> d,string k)=>d.TryGetValue(k,out var v)?v as string??"":"";
        public static void Load(string json)
        {
            var root=StoryJson.Parse(json) as Dictionary<string,object>;
            if(root==null||!root.TryGetValue("articles",out var raw)||!(raw is List<object> rows))throw new FormatException("Missing news articles");
            var loaded=new List<XgNewsArticle>();var ids=new HashSet<string>();
            foreach(var row in rows)
            {
                if(!(row is Dictionary<string,object> d))throw new FormatException("Invalid news row");
                var a=new XgNewsArticle{id=S(d,"id"),date=S(d,"date"),kind=S(d,"kind"),title=S(d,"title"),titleEn=S(d,"titleEn"),body=S(d,"body"),bodyEn=S(d,"bodyEn"),sourceUrl=S(d,"sourceUrl"),brief=d.TryGetValue("brief",out var b)&&b is bool flag&&flag};
                if(a.id.Length==0||!ids.Add(a.id)||a.title.Length==0||a.titleEn.Length==0||!DateTime.TryParseExact(a.date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var day)||day.Year!=2016||(!a.brief&&(a.body.Length==0||a.bodyEn.Length==0)))throw new FormatException("Invalid news article "+a.id);
                loaded.Add(a);
            }
            articles.Clear();articles.AddRange(loaded);
        }
        public static XgNewsArticle Get(string id){foreach(var a in articles)if(a.id==id)return a;return null;}
        public static List<XgNewsArticle> Available(DateTime today)
        {
            var list=new List<XgNewsArticle>();foreach(var a in articles)if(DateTime.ParseExact(a.date,"yyyy-MM-dd",CultureInfo.InvariantCulture)<=today.Date)list.Add(a);
            list.Sort((a,b)=>{int c=string.CompareOrdinal(b.date,a.date);return c!=0?c:string.CompareOrdinal(a.id,b.id);});return list;
        }
        // The archive stays chronological. Higher stages promote technology within the
        // recent fortnight, never leaking a future story or erasing newer headlines.
        public static List<XgNewsArticle> Home(DateTime today,int stage)
        {
            var list=Available(today);if(stage<3)return list;
            list.Sort((a,b)=>{double Score(XgNewsArticle x)=>DateTime.ParseExact(x.date,"yyyy-MM-dd",CultureInfo.InvariantCulture).Ticks/(double)TimeSpan.TicksPerDay+(x.kind=="科技"?Math.Min(12,(stage-2)*3):0);int c=Score(b).CompareTo(Score(a));return c!=0?c:string.CompareOrdinal(b.date,a.date);});return list;
        }
        public static List<XgNewsArticle> Related(string id,DateTime today)
        {
            var chosen=Get(id);var all=Available(today);var result=new List<XgNewsArticle>();
            for(int pass=0;pass<2;pass++)foreach(var a in all)if(a.id!=id&&!a.brief&&!result.Contains(a)&&(pass==1||chosen!=null&&a.kind==chosen.kind)){result.Add(a);if(result.Count==3)return result;}
            return result;
        }
        public static bool Teachable(XgNewsArticle a)=>a!=null&&!a.brief&&a.kind=="科技"&&(a.body.Contains("神经网络")||a.body.Contains("翻译")||a.body.Contains("AlphaGo")||a.body.Contains("注意力")||a.body.Contains("图像描述"));
    }
    public sealed partial class XgState { public List<string> newsTaught=new List<string>(); }
    public sealed partial class XgSim
    {
        public bool NewsAlreadyTaught(string id)=>S.newsTaught!=null&&S.newsTaught.Contains(id);
        public bool TeachNews(string id)
        {
            var a=XgNewsCatalog.Get(id);if(!XgNewsCatalog.Teachable(a)||NewsAlreadyTaught(id)||!TodayKnown)return false;
            int day=int.Parse(a.date.Replace("-",""),CultureInfo.InvariantCulture);if(day>Today)return false;
            if(S.newsTaught==null)S.newsTaught=new List<string>();
            S.newsTaught.Add(id);
            // A real supplied passage, not a synthetic yes/no answer or an invented chat.
            string passage=T(a.body,a.bodyEn);
            int count=0;foreach(char c in passage)if(char.IsLetterOrDigit(c))count++;
            AddGameSamples(ReadDataset,Math.Max(1,count));
            ApplyMemoryOps(new[]{new XgMemoryOp{kind=XgMemoryOpKind.Add,subject=MemorySubjects[4],importance=2,text=T("玩家给我看了新闻：","The player showed me this news: ")+T(a.title,a.titleEn)+"。"+passage}});
            Say(T("资料已交给","Material given to ")+AiDisplayName+T("。","."));return true;
        }
    }
}
