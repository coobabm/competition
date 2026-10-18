using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Miaoyu
{
    public sealed partial class MiaoyuView
    {
        void BuildPublicDiscourse()
        {
            foreach(var record in XgPublicDiscourseDesktop.Native("miaoyu"))
            {
                string body=record.Title(GameText.IsEnglish)+"\n"+XgPublicDiscourseDesktop.Caption(record)+"\n\n"+record.Body(GameText.IsEnglish);
                string sources=XgPublicDiscourseDesktop.Sources(record);if(sources.Length>0)body+="\n\n"+sources;
                var row=Row(list,"Public listing discussion "+record.id,180,Color.white);
                var text=Label(row,"Discussion",Vector2.zero,Vector2.one,new Vector2(16,12),new Vector2(-16,-12),body,17,Ink,TextAlignmentOptions.TopLeft);
                text.richText=false;
                var layout=row.GetComponent<UnityEngine.UI.LayoutElement>();
                if(layout!=null)layout.preferredHeight=Mathf.Max(180,text.GetPreferredValues(body,Mathf.Max(240,list.rect.width-32),0).y+30);
            }
            var note=Row(list,"Public archive notes",90,Color.white);
            var label=Label(note,"Notes",Vector2.zero,Vector2.one,new Vector2(16,12),new Vector2(-16,-12),XgPublicDiscourseDesktop.ArchiveNote,13,Muted,TextAlignmentOptions.TopLeft);
            label.richText=false;
            var noteLayout=note.GetComponent<UnityEngine.UI.LayoutElement>();
            if(noteLayout!=null)noteLayout.preferredHeight=Mathf.Max(90,label.GetPreferredValues(label.text,Mathf.Max(240,list.rect.width-32),0).y+30);
        }
    }
}
