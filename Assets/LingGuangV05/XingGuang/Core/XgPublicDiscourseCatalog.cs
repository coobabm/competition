using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core.Story;

namespace LingGuangV05.XingGuang
{
    /// <summary>A public projection only. Never pass a save, chat history, labels or private rules here.</summary>
    public sealed class XgPublicDiscourseContext
    {
        public readonly int stage, publicDate;
        public readonly string platform, route, artifactId;
        readonly HashSet<string> installed, events;
        public XgPublicDiscourseContext(int stage,int publicDate,string platform,IEnumerable<string> installedPlatforms,
            string route="",string artifactId="",IEnumerable<string> publicEventIds=null)
        {
            this.stage=stage;this.publicDate=publicDate;this.platform=platform??"";
            this.route=route??"";this.artifactId=artifactId??"";
            installed=new HashSet<string>(installedPlatforms??Array.Empty<string>(),StringComparer.Ordinal);
            events=new HashSet<string>(publicEventIds??Array.Empty<string>(),StringComparer.Ordinal);
        }
        public bool Installed(string name)=>installed.Contains(name);
        public bool HasPublicEvent(string id)=>events.Contains(id);
    }

    [Serializable] public sealed class XgPublicDiscourseRecord
    {
        public string id="",platform="",route="",kind="",scope="",topicId="",titleZh="",titleEn="",bodyZh="",bodyEn="";
        public int stage,since;
        public bool requiresArtifact;
        public string[] sourceIds=Array.Empty<string>(),requiredPublicEventIds=Array.Empty<string>(),legacyNewsIds=Array.Empty<string>();
        public string Title(bool english)=>english?titleEn:titleZh;
        public string Body(bool english)=>english?bodyEn:bodyZh;
    }

    [Serializable] public sealed class XgPublicDiscourseSource
    {
        public string id="",title="",url="";
        public string authorsZh="",authorsEn="",publication="";
        public int since;
    }

    /// <summary>Authored, bilingual read-only material. Selection has no chat, reward or delivery side effects.</summary>
    public sealed class XgPublicDiscourseCatalog
    {
        readonly List<XgPublicDiscourseRecord> records;
        readonly Dictionary<string,XgPublicDiscourseSource> sources;
        public IReadOnlyList<XgPublicDiscourseRecord> All=>records.AsReadOnly();
        public XgPublicDiscourseSource Source(string id)=>id!=null&&sources.TryGetValue(id,out var source)?source:null;
        XgPublicDiscourseCatalog(List<XgPublicDiscourseRecord> records,Dictionary<string,XgPublicDiscourseSource> sources)
        {this.records=records;this.sources=sources;}
        public static bool Platform(string value)=>value=="news"||value=="forum"||value=="site"||value=="miaoyu"||value=="yy";
        public static bool Route(string value)=>value==""||value=="online"||value=="public_model"||value=="local_only";
        static string Text(Dictionary<string,object> row,string key)=>row.TryGetValue(key,out var value)?value as string??"":"";
        static int Number(Dictionary<string,object> row,string key)
        {
            if(!row.TryGetValue(key,out var value))throw new FormatException("Missing "+key);
            if(!int.TryParse(Convert.ToString(value,CultureInfo.InvariantCulture),NumberStyles.Integer,CultureInfo.InvariantCulture,out var number))
                throw new FormatException("Invalid "+key);
            return number;
        }
        static string[] Strings(Dictionary<string,object> row,string key)
        {
            if(!row.TryGetValue(key,out var raw))return Array.Empty<string>();
            if(!(raw is List<object> list))throw new FormatException("Invalid "+key);
            var result=new List<string>();var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var value in list)
            {if(!(value is string text)||string.IsNullOrWhiteSpace(text)||!seen.Add(text))throw new FormatException("Invalid "+key+" value");result.Add(text);}
            return result.ToArray();
        }
        static bool Date(int value)=>DateTime.TryParseExact(value.ToString(CultureInfo.InvariantCulture),"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _);
        public static XgPublicDiscourseCatalog Load(string json)
        {
            var root=StoryJson.Parse(json) as Dictionary<string,object>;
            if(root==null||!root.TryGetValue("records",out var raw)||!(raw is List<object> rows)
                ||!root.TryGetValue("sources",out var sourceRaw)||!(sourceRaw is List<object> sourceRows))throw new FormatException("Missing public discourse records/sources");
            var sources=new Dictionary<string,XgPublicDiscourseSource>(StringComparer.Ordinal);
            foreach(var item in sourceRows)
            {
                if(!(item is Dictionary<string,object> row))throw new FormatException("Invalid public source");
                var source=new XgPublicDiscourseSource{id=Text(row,"id"),title=Text(row,"title"),url=Text(row,"url"),since=Number(row,"since"),
                    authorsZh=Text(row,"authorsZh"),authorsEn=Text(row,"authorsEn"),publication=Text(row,"publication")};
                if(string.IsNullOrWhiteSpace(source.id)||string.IsNullOrWhiteSpace(source.title)||!Date(source.since)||sources.ContainsKey(source.id))throw new FormatException("Invalid public source "+source.id);
                sources.Add(source.id,source);
            }
            var records=new List<XgPublicDiscourseRecord>();var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var item in rows)
            {
                if(!(item is Dictionary<string,object> row))throw new FormatException("Invalid public discourse row");
                if(!row.TryGetValue("requiresArtifact",out var requires)||!(requires is bool))throw new FormatException("Missing artifact gate");
                var record=new XgPublicDiscourseRecord{id=Text(row,"id"),platform=Text(row,"platform"),route=Text(row,"route"),kind=Text(row,"kind"),scope=Text(row,"scope"),topicId=Text(row,"topicId"),stage=Number(row,"stage"),since=Number(row,"since"),
                    requiresArtifact=(bool)requires,sourceIds=Strings(row,"sourceIds"),requiredPublicEventIds=Strings(row,"requiredPublicEventIds"),legacyNewsIds=Strings(row,"legacyNewsIds"),
                    titleZh=Text(row,"titleZh"),titleEn=Text(row,"titleEn"),bodyZh=Text(row,"bodyZh"),bodyEn=Text(row,"bodyEn")};
                if(string.IsNullOrWhiteSpace(record.id)||!ids.Add(record.id)||!Platform(record.platform)||!Route(record.route)||record.stage<1||record.stage>6||!Date(record.since)
                    ||string.IsNullOrWhiteSpace(record.titleZh)||string.IsNullOrWhiteSpace(record.titleEn)||string.IsNullOrWhiteSpace(record.bodyZh)||string.IsNullOrWhiteSpace(record.bodyEn)
                    ||record.kind!="historical"&&record.kind!="fictional")throw new FormatException("Invalid public discourse "+record.id);
                if(record.requiresArtifact&&(record.route==""||record.route=="local_only"))throw new FormatException("Artifact requires a public release route: "+record.id);
                if(record.requiredPublicEventIds.Length>0&&!record.requiresArtifact)throw new FormatException("Public experience requires an artifact: "+record.id);
                foreach(var id in record.sourceIds)
                    if(!sources.TryGetValue(id,out var source)||source.since>record.since)throw new FormatException("Missing/future source for "+record.id+": "+id);
                if(record.kind=="historical"&&record.sourceIds.Length==0)throw new FormatException("Historical record needs sources: "+record.id);
                records.Add(record);
            }
            records.Sort((a,b)=>{int order=a.since.CompareTo(b.since);return order!=0?order:string.CompareOrdinal(a.id,b.id);});
            return new XgPublicDiscourseCatalog(records,sources);
        }
        public bool Visible(XgPublicDiscourseRecord record,XgPublicDiscourseContext context)
        {
            return EvidenceAllows(record,context)&&record.platform==context.platform&&context.Installed(record.platform);
        }
        bool EvidenceAllows(XgPublicDiscourseRecord record,XgPublicDiscourseContext context)
        {
            if(record==null||context==null||!records.Contains(record)||record.stage>context.stage||record.since>context.publicDate)return false;
            if(record.route.Length>0&&record.route!=context.route)return false;
            if(record.requiresArtifact&&(string.IsNullOrWhiteSpace(context.artifactId)||context.route=="local_only"))return false;
            foreach(var id in record.requiredPublicEventIds)if(!context.HasPublicEvent(id))return false;
            return true;
        }
        public List<XgPublicDiscourseRecord> Available(XgPublicDiscourseContext context)
        {var result=new List<XgPublicDiscourseRecord>();foreach(var record in records)if(Visible(record,context))result.Add(record);return result;}
        /// <summary>Public excerpts in the installed browser; not delivery, installation, or native-platform read receipts.</summary>
        public List<XgPublicDiscourseRecord> BrowserArchive(XgPublicDiscourseContext browser,string sourcePlatform)
        {
            var result=new List<XgPublicDiscourseRecord>();
            if(browser==null||browser.platform!="news"||!browser.Installed("news")||!Platform(sourcePlatform))return result;
            foreach(var record in records)if(record.platform==sourcePlatform&&EvidenceAllows(record,browser))result.Add(record);
            return result;
        }
        public XgPublicDiscourseRecord Get(string id,XgPublicDiscourseContext context)
        {foreach(var record in records)if(record.id==id&&Visible(record,context))return record;return null;}
    }
}
