using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.Tieba
{
    public sealed partial class TiebaView
    {
        void PublicDiscourseHome()
        {
            if(XgPublicDiscourseDesktop.Native("forum").Count==0)return;
            var owner=XgPublicDiscourseDesktop.Capture();
            Line(Card(),XgPublicDiscourseDesktop.Heading,18,Link,()=>{if(owner.Current)Go("public.discourse","");});
        }
        void DrawPublicDiscourse(string id)
        {
            var owner=XgPublicDiscourseDesktop.Capture();
            Line(Card(),XgPublicDiscourseDesktop.Heading,20,Ink);
            var rows=XgPublicDiscourseDesktop.Native("forum");
            foreach(var row in rows)
            {
                if(!string.IsNullOrEmpty(id)&&id!=row.id)continue;
                var card=Card();string selected=row.id;
                Line(card,XgPublicDiscourseDesktop.Escape(row.Title(GameText.IsEnglish)),20,Link,()=>{if(owner.Current)Go("public.discourse",selected);});
                Line(card,XgPublicDiscourseDesktop.Caption(row),13,Muted);
                if(!string.IsNullOrEmpty(id))
                {
                    Line(card,XgPublicDiscourseDesktop.Escape(row.Body(GameText.IsEnglish)),18,Ink);
                    string sources=XgPublicDiscourseDesktop.Sources(row);if(sources.Length>0)Line(card,XgPublicDiscourseDesktop.Escape(sources),13,Muted);
                }
            }
            if(!string.IsNullOrEmpty(id))Line(Card(),XgPublicDiscourseDesktop.Heading,16,Link,()=>{if(owner.Current)Go("public.discourse","");});
            Line(Card(),XgPublicDiscourseDesktop.ArchiveNote,13,Muted);
        }
    }
}
