using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public static partial class XgCatalog
    {
        internal static void ReloadPersonaNodes(){nodes=null;}
        static void AddPersonaPackNodes(List<XgNode> list)
        {
            foreach(var p in XgSim.PersonaPacks)
            {
                var n=N("persona."+p.id,"persona",p.id=="daily"?null:"train.curve",XgNodeKind.Research,p.id,0,p.price,0,0,p.name,p.nameEn,p.desc,p.descEn);
                n.stage=p.stage;n.lane="persona";list.Add(n);
            }
        }
    }
}
