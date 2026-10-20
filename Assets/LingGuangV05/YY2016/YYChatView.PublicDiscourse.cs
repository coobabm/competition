using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatView
    {
        bool discourseOpen;
        string discourseStamp="";
        float discourseCheckAt;
        void PollPublicDiscourse()
        {
            if(Time.unscaledTime<discourseCheckAt)return;discourseCheckAt=Time.unscaledTime+.5f;
            string now=XgPublicDiscourseDesktop.Stamp;
            if(now==discourseStamp)return;discourseStamp=now;dirty=true;lastSignature="";
        }
        float AppendPublicDiscourse(float y,float width)
        {
            // Read-only group-topic archive: never fabricate messages, membership or unread counts.
            if(hub.S.selected!=YYChatHub.EraGroup)return y;
            var rows=XgPublicDiscourseDesktop.Native("yy");if(rows.Count==0)return y;
            var owner=XgPublicDiscourseDesktop.Capture();
            var heading=Text(Rect("Public discussion archive",scrollContent,new Vector2(0,1),Vector2.one,new Vector2(24,-y-48),new Vector2(-24,-y)),XgPublicDiscourseDesktop.Heading,16,C.Ink,TextAlignmentOptions.MidlineLeft);
            heading.raycastTarget=true;var button=heading.gameObject.AddComponent<Button>();button.targetGraphic=heading;
            button.onClick.AddListener(()=>{if(!owner.Current)return;discourseOpen=!discourseOpen;lastSignature="";dirty=true;stickBottom=false;});
            y+=56;if(!discourseOpen)return y;
            foreach(var row in rows)
            {
                string content=row.Title(GameText.IsEnglish)+"\n"+XgPublicDiscourseDesktop.Caption(row)+"\n\n"+row.Body(GameText.IsEnglish);
                string sources=XgPublicDiscourseDesktop.Sources(row);if(sources.Length>0)content+="\n\n"+sources;
                var text=Text(Rect("Public topic "+row.id,scrollContent,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero),content,15,C.Ink,TextAlignmentOptions.TopLeft);
                text.richText=false;float height=text.GetPreferredValues(content,Mathf.Max(200,width-48),0).y+20;
                text.rectTransform.offsetMin=new Vector2(24,-y-height);text.rectTransform.offsetMax=new Vector2(-24,-y);
                y+=height+12;
                if(row.kind!="historical")
                {
                    var notice=Text(Rect("PublicReplyNotice:"+row.id,scrollContent,new Vector2(0,1),Vector2.one,new Vector2(24,-y-22),new Vector2(-24,-y)),"",12,C.Muted,TextAlignmentOptions.MidlineLeft);
                    AiContentNotice.BindFooter(notice);notice.alignment=TextAlignmentOptions.MidlineLeft;y+=30;
                }
            }
            var note=Text(Rect("Public archive notes",scrollContent,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero),XgPublicDiscourseDesktop.ArchiveNote,13,C.Ink,TextAlignmentOptions.TopLeft);
            note.richText=false;float noteHeight=note.GetPreferredValues(note.text,Mathf.Max(200,width-48),0).y+20;
            note.rectTransform.offsetMin=new Vector2(24,-y-noteHeight);note.rectTransform.offsetMax=new Vector2(-24,-y);y+=noteHeight+12;
            return y;
        }
    }
}
