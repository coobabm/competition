using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Read-only surface adapter. Release evidence is supplied by the verified release/save owner.</summary>
    public static class XgPublicDiscourseDesktop
    {
        static XgPublicDiscourseCatalog catalog;
        static bool loaded;
        static ChapterOneSim releaseOwner;
        static ChapterOneSim observedOwner;
        static int bindingSerial;
        static long releaseRevision;
        static int displayDate;
        static string releaseRoute="",artifactId="";
        static string[] publicEvents=Array.Empty<string>();
        static BoduView browser;
        public static XgPublicDiscourseCatalog Catalog
        {
            get
            {
                if(!loaded)
                {
                    loaded=true;var resource=Resources.Load<TextAsset>("LingGuangV05/Epilogue/ending_public_discourse");
                    if(resource!=null)try{catalog=XgPublicDiscourseCatalog.Load(resource.text);}catch(FormatException e){Debug.LogError("Public discourse: "+e.Message);}
                }
                return catalog;
            }
        }
        /// <summary>Call after a real commit/load, using only published receipts. This never makes a release succeed.</summary>
        public static void SetReleaseProjection(ChapterOneSim owner,long revision,int publicDate,string route,string artifact,IEnumerable<string> eventIds)
        {
            if(owner==null||!XgPublicDiscourseCatalog.Route(route??""))throw new ArgumentException("Invalid public release projection");
            if(!ReferenceEquals(owner,Owner))return;
            // A revision identifies an immutable persisted projection. Old callbacks cannot replace it.
            if(ReferenceEquals(releaseOwner,owner)&&revision<=releaseRevision)return;
            releaseOwner=owner;releaseRevision=revision;displayDate=publicDate;releaseRoute=route??"";artifactId=artifact??"";
            publicEvents=(eventIds??Array.Empty<string>()).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        }
        static ChapterOneSim Owner=>XingGuangController.Instance?.runtime?.Sim;
        public readonly struct Token
        {
            readonly ChapterOneSim owner;readonly long revision;
            internal Token(ChapterOneSim owner,long revision){this.owner=owner;this.revision=revision;}
            public bool Current=>owner!=null&&ReferenceEquals(owner,Owner)&&revision==Revision;
        }
        static long Revision=>ReferenceEquals(releaseOwner,Owner)?releaseRevision:0;
        public static Token Capture()=>new Token(Owner,Revision);
        public static XgPublicDiscourseContext Context(string platform)
        {
            var ctl=XingGuangController.Instance;var owner=ctl?.runtime?.Sim;var installed=new List<string>();
            if(!ReferenceEquals(owner,observedOwner)){observedOwner=owner;bindingSerial++;}
            if(owner==null||ctl.Sim==null||owner.InPrologue)return new XgPublicDiscourseContext(0,0,platform,installed);
            if(browser==null)browser=UnityEngine.Object.FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);
            if(browser!=null){installed.Add("news");installed.Add("site");}
            if(TiebaHub.Instance?.View!=null)installed.Add("forum");
            if(YYChatHub.Instance?.View!=null)installed.Add("yy");
            if(LingGuangV05.Desktop.Miaoyu.MiaoyuView.Instance!=null&&ctl.Sim.S.stage>=3&&StudyDesktopGates.Allows("miaoyu"))installed.Add("miaoyu");
            bool published=ReferenceEquals(releaseOwner,owner);
            int today=GameCalendar.Yyyymmdd(GameCalendar.Now(owner.S));
            return new XgPublicDiscourseContext(ctl.Sim.S.stage,published&&displayDate>0?displayDate:today,platform,installed,
                published?releaseRoute:"",published?artifactId:"",published?publicEvents:null);
        }
        public static string Stamp
        {
            get{var c=Context("news");return bindingSerial+"/"+c.stage+"/"+c.publicDate+"/"+Revision+"/"+c.route+"/"+c.artifactId;}
        }
        public static List<XgPublicDiscourseRecord> Native(string platform)=>Catalog?.Available(Context(platform))??new List<XgPublicDiscourseRecord>();
        public static List<XgPublicDiscourseRecord> Archive(string platform)=>Catalog?.BrowserArchive(Context("news"),platform)??new List<XgPublicDiscourseRecord>();
        public static string PlatformName(string platform)=>GameText.IsEnglish
            ?platform=="news"?"Bodu":platform=="forum"?"Forum":platform=="site"?"Public site":platform=="miaoyu"?"Miaoyu":"YY"
            :platform=="news"?"摆渡":platform=="forum"?"贴吧":platform=="site"?"公共主题站":platform=="miaoyu"?"喵鱼":"YY";
        public static string Heading=>GameText.T("讨论存档","Discussion archive");
        public static string ArchiveNote=>GameText.T("资料说明：历史条目附原始链接；讨论摘录中的人物与事件为游戏创作。公开内容收录于此，可随时回看。","Archive notes: Historical entries link to their sources. People and events in the discussion excerpts are fictional. These public posts remain available to revisit.");
        public static string Caption(XgPublicDiscourseRecord row)
        {
            string date=DateTime.ParseExact(row.since.ToString(CultureInfo.InvariantCulture),"yyyyMMdd",CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
            string kind=row.kind=="historical"?GameText.T("历史资料摘要","Historical summary"):GameText.T("讨论摘录","Discussion excerpt");
            // since is an availability gate, not an invented publication timestamp for fictional posts.
            return (row.kind=="historical"?date+" · ":"")+kind+(row.scope=="public_sample"?GameText.T(" · 别人的公开样站"," · Someone else's public example site"):"");
        }
        public static string Sources(XgPublicDiscourseRecord row)
        {
            var parts=new List<string>();foreach(string id in row.sourceIds)
            {
                var source=Catalog.Source(id);if(source==null)continue;
                string authors=GameText.IsEnglish?source.authorsEn:source.authorsZh;
                string details=source.title;
                if(!string.IsNullOrEmpty(authors))details+="\n"+GameText.T("作者／贡献者：","Authors / contributors: ")+authors;
                if(!string.IsNullOrEmpty(source.publication))details+="\n"+GameText.T("出处：","Publication: ")+source.publication;
                parts.Add(details+"\n"+source.url);
            }
            return string.Join("\n",parts);
        }
        public static string Escape(string text)=>(text??"").Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;");
    }
}
