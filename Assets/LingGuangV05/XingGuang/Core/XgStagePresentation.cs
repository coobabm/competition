using System.Collections.Generic;
namespace LingGuangV05.XingGuang {
 [System.Serializable] public sealed class XgStageUnlockSnapshot { public int stage; public List<string> nodes=new List<string>(),desks=new List<string>(); }
 public sealed partial class XgState {
  public List<XgStageUnlockSnapshot> stageUnlockSnapshots=new List<XgStageUnlockSnapshot>();
  public int stagePresentationVersion;
  public List<int> stagePresentations=new List<int>();
 }
 public sealed partial class XgSim {
  public bool StagePresentationHeld=>S.stagePresentations!=null&&S.stagePresentations.Count>0;
  public void RepairStagePresentations(){if(S.stageUnlockSnapshots==null)S.stageUnlockSnapshots=new List<XgStageUnlockSnapshot>();S.stageUnlockSnapshots.RemoveAll(x=>x==null||x.stage<2||x.stage>6);if(S.stagePresentations==null)S.stagePresentations=new List<int>();S.stagePresentations.RemoveAll(x=>x<2||x>6||x>S.stage);if(S.stagePresentationVersion==0){S.stagePresentations.Clear();S.stagePresentationVersion=1;}}
  public void QueueStagePresentation(int stage){if(stage>=2&&stage<=6&&!S.stagePresentations.Contains(stage))S.stagePresentations.Add(stage);}
  public bool CompleteStagePresentation(int stage){if(!StagePresentationHeld||S.stagePresentations[0]!=stage)return false;S.stagePresentations.RemoveAt(0);S.stageUnlockSnapshots.RemoveAll(x=>x.stage==stage);return true;}
  public XgStageUnlockSnapshot StageUnlockSnapshot(int stage){if(S.stageUnlockSnapshots==null)return null;return S.stageUnlockSnapshots.Find(x=>x!=null&&x.stage==stage);}
  XgStageUnlockSnapshot CaptureStageUnlocks(){
   var snapshot=new XgStageUnlockSnapshot{stage=S.stage};
   foreach(var node in XgCatalog.Nodes)if(node.kind!=XgNodeKind.Phenomenon&&node.kind!=XgNodeKind.Ability&&NodeVisible(node))snapshot.nodes.Add(node.id);
   foreach(var desk in XgStageLessons.Desks)if(TeachingPackBlocker(desk)==null)snapshot.desks.Add(desk.id);
   return snapshot;
  }
  void SaveStageUnlockDifference(XgStageUnlockSnapshot before){
   var after=CaptureStageUnlocks();after.nodes.RemoveAll(id=>before.nodes.Contains(id)||Has(id));after.desks.RemoveAll(id=>before.desks.Contains(id));
   if(S.stageUnlockSnapshots==null)S.stageUnlockSnapshots=new List<XgStageUnlockSnapshot>();
   S.stageUnlockSnapshots.RemoveAll(x=>x==null||x.stage==after.stage);S.stageUnlockSnapshots.Add(after);
  }

 }
}
