using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatHub
    {
        public bool BoundTo(ChapterOneSim sim) => ReferenceEquals(linked,sim);
        public bool ZhouContactReady => TiebaHub.Instance!=null&&TiebaHub.Instance.ZhouYyLinked&&TiebaHub.Instance.ZhouStateBoundTo(linked);
        public void NoteZhouLinked()
        {
            if(!ZhouContactReady)return;
            var c=Conversation(LaoZhou);
            if(c.messages.Exists(m=>m.kind==YYKind.System&&m.attentionWord=="zhou.contact.accepted"))return;
            c.messages.Add(new YYMessage{from="system",kind=YYKind.System,attentionWord="zhou.contact.accepted",text=GameText.T("你与老周已成为 YY 联系人。","You and Lao Zhou are now YY contacts."),gameSeconds=Now});Touch();
        }
        public void AppendZhouAuthored(string from,string text)
        {
            if(!ZhouContactReady)return;
            var c=Conversation(LaoZhou);c.messages.Add(new YYMessage{from=from,text=text,gameSeconds=Now,laoZhouObserved=true});
            if(from!=Me&&!IsShowing(LaoZhou))c.unread++;
        }
        public void RefreshZhouAuthored()=>Touch();
    }
}
