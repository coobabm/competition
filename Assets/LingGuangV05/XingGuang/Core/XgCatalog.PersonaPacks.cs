using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public static partial class XgCatalog
    {
        internal static void ReloadPersonaNodes(){nodes=null;}
        static void AddPersonaPackNodes(List<XgNode> list)
        {
            var archive=N(XgSim.PersonaArchiveNode,"persona",null,XgNodeKind.Research,null,0,XgSim.PersonaArchivePrice,0,0,"性格档案","Personality archive","阶段2起，累计赞/踩15次或发现性格漂移后，解锁三角图、近14游戏日变化与真实原因。","From stage 2, after 15 ratings or observed drift: unlock the triangle, 14 game-day history and real causes.");
            archive.stage=2;archive.lane="persona";list.Add(archive);
            foreach(var p in XgSim.PersonaPacks)
            {
                var n=N("persona."+p.id,"persona",p.id=="daily"?null:"train.curve",XgNodeKind.Research,p.id,0,p.price,0,0,p.name,p.nameEn,p.desc,p.descEn);
                n.stage=p.stage;n.lane="persona";list.Add(n);
            }
        }
    }
}
