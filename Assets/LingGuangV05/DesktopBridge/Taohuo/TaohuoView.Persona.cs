using System;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
namespace LingGuangV05.Desktop.Taohuo
{
    public sealed partial class TaohuoView
    {
        int BuildPersonaPacks(string query,XgSim lab,IXgHost host)
        {
            int count=0;
            foreach(var pack in XgSim.PersonaPacks)
            {
                if(query.Length>0&&(pack.name+pack.nameEn+pack.desc+pack.descEn).IndexOf(query,StringComparison.OrdinalIgnoreCase)<0)continue;
                string id=pack.id;
                var row=Row(list,"persona."+id,126,Color.white);
                var words=PrologueDesk.Rect("Words",row,Vector2.zero,Vector2.one,new Vector2(16,12),new Vector2(-170,-12));
                Label(words,"Name",new Vector2(0,1),Vector2.one,new Vector2(0,-28),Vector2.zero,T(pack.name,pack.nameEn),18,Ink);
                Label(words,"Kind",new Vector2(0,1),Vector2.one,new Vector2(0,-52),new Vector2(0,-30),T("性格题包 · 买后在YY打开","Personality pack · open in YY"),13,Orange);
                Label(words,"Description",Vector2.zero,Vector2.one,Vector2.zero,new Vector2(0,-54),T(pack.desc,pack.descEn),14,Muted);
                var right=PrologueDesk.Rect("Right",row,new Vector2(1,0),Vector2.one,new Vector2(-160,12),new Vector2(-12,-12));
                Label(right,"Price",new Vector2(0,1),Vector2.one,new Vector2(0,-36),Vector2.zero,pack.price==0?T("免费","Free"):Money(pack.price),22,Orange,TextAlignmentOptions.TopRight);
                var button=Btn(right,"Buy",Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,42),Orange,"",15,Color.white,()=>BuyPersonaFromShop(id),out var label);
                buyButtons.Add((button,label,()=>PersonaPurchaseState(id)));
                UiTip.Add(button,()=>lab.PersonaPackBlocker(id,host)??T("和科技树是同一个购买","The same purchase as in Tech"));count++;
            }
            return count;
        }
        (bool,string) PersonaPurchaseState(string id)
        {
            var lab=Lab;var host=FindAnyObjectByType<XingGuangController>()?.Host;
            if(lab==null)return (false,"—");
            string blocked=lab.PersonaPackBlocker(id,host);
            return (blocked==null,blocked??T("立即购买","Buy now"));
        }
        public bool BuyPersonaFromShop(string id)
        {
            var lab=Lab;var host=FindAnyObjectByType<XingGuangController>()?.Host;
            if(lab==null||host==null)return false;
            bool ok=lab.BuyPersonaPack(id,host);
            if(ok){Dirty();Say(T("题包到了，在YY的「性格」里打开。","Your pack is ready in YY's Personality drawer."));}
            else Say(lab.PersonaPackBlocker(id,host)??T("现在买不了。","Unavailable right now."));
            signature="";return ok;
        }
    }
}
