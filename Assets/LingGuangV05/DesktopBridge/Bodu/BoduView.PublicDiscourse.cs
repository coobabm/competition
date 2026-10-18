using System.Linq;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.Bodu
{
    public sealed partial class BoduView
    {
        string discoursePlatform="news",discourseId="";
        string discourseSource="";
        bool discourseManifest;
        public void OpenPublicDiscourse(string platform="news",string id="")
        {
            if(!LingGuangV05.XingGuang.XgPublicDiscourseCatalog.Platform(platform))return;
            discoursePlatform=platform;discourseId=id??"";discourseSource="";discourseManifest=false;page="discourse";signature="";
            if(window!=null&&!window.isOn)window.OpenWindow();
        }
        void PublicDiscourseHome()
        {
            if(XgPublicDiscourseDesktop.Archive("news").Count==0)return;
            var owner=XgPublicDiscourseDesktop.Capture();
            Line(XgPublicDiscourseDesktop.Heading,18,Link,()=>{if(owner.Current)OpenPublicDiscourse();});
        }
        void PublicDiscoursePage()
        {
            var owner=XgPublicDiscourseDesktop.Capture();
            Line(T("返回新闻首页","Back to headlines"),16,Blue,()=>{if(owner.Current){page="home";signature="";}});
            Line(XgPublicDiscourseDesktop.Heading,23,Ink);
            Line(T("新闻、帖子和群里的旧话题。","News, posts and old group topics."),14,Muted);
            var epilogue=LingGuangV05.Desktop.XingGuang.XingGuangController.Instance?.Epilogue;
            var release=epilogue?.PublicModelRelease();
            if(discourseManifest)
            {
                Line(T("返回讨论存档","Back to discussion archive"),16,Blue,()=>{if(owner.Current){discourseManifest=false;signature="";}});
                Line(T("发布清单","Release manifest"),24,Ink);
                Line(T("仅校验发布说明，不包含模型权重文件。","Checks the release notes only, excluding model weight files."),16,Muted);
                if(release==null){Line(T("此内容当前不可用。","This content is not available now."),17,Muted);return;}
                if(!LingGuangV05.XingGuang.XgVirtualReleaseManifest.Recorded(release))
                {Line(T("这次发布没有保存清单。","No manifest was saved for this release."),17,Muted);return;}
                string version=release.publicManifestVersion==LingGuangV05.XingGuang.XgVirtualReleaseManifest.Version?"1":T("未知","Unknown");
                Line(T("清单版本：","Manifest version: ")+version,16,Ink);
                Line(T("校验码：","Checksum: ")+XgPublicDiscourseDesktop.Escape(release.publicManifestSha256),14,Ink);
                bool valid=LingGuangV05.XingGuang.XgVirtualReleaseManifest.Valid(release);
                Line(valid?T("校验一致。","Checksum matches."):T("清单校验失败，内容或版本可能已改变。","Manifest verification failed. Its content or version may have changed."),17,Muted);
                if(valid)
                {
                    Line(T("产品名：","Product name: ")+XgPublicDiscourseDesktop.Escape(release.publicName),18,Ink);
                    Line(T("说明：","Description: ")+XgPublicDiscourseDesktop.Escape(release.publicDescription),16,Ink);
                    Line(T("能力：","Capabilities: ")+XgPublicDiscourseDesktop.Escape(release.publicCapabilities),16,Ink);
                    Line(T("限制：","Limitations: ")+XgPublicDiscourseDesktop.Escape(release.publicLimitations),16,Ink);
                    Line(T("维护与许可：","Maintenance and license: ")+XgPublicDiscourseDesktop.Escape(release.publicTerms),16,Ink);
                }
                return;
            }
            if(release!=null)Line(T("发布清单 ›","Release manifest ›"),16,Blue,()=>
            {if(owner.Current&&epilogue.PublicModelRelease()!=null){discourseManifest=true;signature="";}});
            if(discourseSource.Length>0)
            {
                var source=epilogue?.FindPublicSource(discourseSource);
                Line(T("返回讨论","Back to discussion"),16,Blue,()=>{if(owner.Current){discourseSource="";signature="";}});
                if(source==null){Line(T("此内容当前不可用。","This content is not available now."),17,Muted);return;}
                Line(XgPublicDiscourseDesktop.Escape(GameText.IsEnglish?source.titleEn:source.titleZh),24,Ink);
                Line(XgPublicDiscourseDesktop.Escape(GameText.IsEnglish?source.bodyEn:source.bodyZh),18,Ink);
                Line(T("游戏内公开记录 · 虚构人物与事件","In-game public record · Fictional people and events"),14,Muted);
                return;
            }
            foreach(string platform in new[]{"news","forum","site","miaoyu","yy"})
            {
                if(XgPublicDiscourseDesktop.Archive(platform).Count==0)continue;
                string selected=platform;Line(XgPublicDiscourseDesktop.PlatformName(platform),16,Blue,()=>{if(owner.Current)OpenPublicDiscourse(selected);});
            }
            var rows=XgPublicDiscourseDesktop.Archive(discoursePlatform);
            if(discourseId.Length>0)
            {
                var chosen=rows.FirstOrDefault(r=>r.id==discourseId);
                if(chosen==null){Line(T("此内容当前不可用。","This content is not available now."),17,Muted);return;}
                Line(XgPublicDiscourseDesktop.Escape(chosen.Title(GameText.IsEnglish)),24,Ink);
                Line(XgPublicDiscourseDesktop.Caption(chosen),13,Muted);
                Line(XgPublicDiscourseDesktop.Escape(chosen.Body(GameText.IsEnglish)),18,Ink);
                string sources=XgPublicDiscourseDesktop.Sources(chosen);if(sources.Length>0)Line(XgPublicDiscourseDesktop.Escape(sources),13,Muted);
                if(epilogue!=null)foreach(var savedSource in epilogue.PublicSourcesForRecord(chosen.id))
                {
                    string sourceRef=savedSource.sourceRef;
                    Line(T("查看公开原帖 ›","Read the public source ›"),16,Blue,()=>
                    {
                        if(!owner.Current||epilogue.FindPublicSource(sourceRef)==null)return;
                        discourseSource=sourceRef;signature="";
                    });
                }
                Line(XgPublicDiscourseDesktop.ArchiveNote,13,Muted);
                return;
            }
            foreach(var row in rows)
            {
                string id=row.id;Line(XgPublicDiscourseDesktop.Escape(row.Title(GameText.IsEnglish)),19,Blue,()=>{if(owner.Current)OpenPublicDiscourse(discoursePlatform,id);});
                Line(XgPublicDiscourseDesktop.Caption(row),13,Muted);
            }
            Line(XgPublicDiscourseDesktop.ArchiveNote,13,Muted);
        }
    }
}
