#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Reflection;
using LingGuangV05.Core;using LingGuangV05.Runtime;using LingGuangV05.XingGuang;using LingGuangV05.Desktop;using UnityEditor;using UnityEngine;
public static class IdleSaveAndStageSnapshotQa
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static readonly List<string> passed=new List<string>();
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);passed.Add(message);}
 [Serializable]sealed class Report{public string status,detail;public string[] checks;}
 [MenuItem("LingGuang/Verification/Idle save and stage snapshot (isolated disk)")]
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run in Edit Mode; isolated inactive runtime only");
  string output=Path.GetFullPath(Path.Combine("LocalVerification","IdleSaveAndStageSnapshot",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));Directory.CreateDirectory(output);passed.Clear();GameObject go=null;var report=new Report{status="passed",detail="Real Unity JsonUtility and isolated AtomicSaveStore; timer boundaries controlled by reflection, not a five-minute wall-clock run."};
  var input=typeof(ChapterOneRuntime).GetField("lastDesktopInput",BindingFlags.Static|BindingFlags.NonPublic);object oldInput=input?.GetValue(null);
  try
  {
   go=new GameObject("Isolated idle save QA");go.SetActive(false);var rt=go.AddComponent<ChapterOneRuntime>();rt.useDiskSave=true;typeof(ChapterOneRuntime).GetField("SaveDirectoryOverride",Flags).SetValue(rt,Path.Combine(output,"Save"));rt.EnsureInitialized();
   var p=PrologueProfile.Read("嘴硬心软");p.name="快照测试";p.self="我";p.callMe="你";Check(rt.CompletePrologue(p),"isolated initial save succeeds");int saves=0;rt.Saving+=()=>saves++;
   var elapsed=typeof(ChapterOneRuntime).GetField("sinceSave",Flags);var dirty=typeof(ChapterOneRuntime).GetField("dirty",Flags);
   Action<float> due=seconds=>{rt.MarkDirty();elapsed.SetValue(rt,seconds);};Action idle=()=>input.SetValue(null,Time.realtimeSinceStartupAsDouble-2.1);
   Check(ChapterOneRuntime.AutosaveSeconds==300,"autosave interval is 300 seconds");idle();due(299.9f);Check(!rt.TryAutosave()&&saves==0,"no auto write before five minutes");due(300);ChapterOneRuntime.NotifyDesktopInput();Check(!rt.TryAutosave()&&saves==0&&(bool)dirty.GetValue(rt),"input postpones due save without clearing dirty progress");
   input.SetValue(null,Time.realtimeSinceStartupAsDouble-1.9);Check(!rt.TryAutosave(),"1.9 seconds idle still postpones");idle();rt.Sim.S.money=123456;Check(rt.TryAutosave()&&saves==1,"idle due save writes exactly once");Check(!rt.TryAutosave()&&saves==1,"no duplicate write without another interval");
   due(300);ChapterOneRuntime.NotifyDesktopInput();Check(rt.SaveNow()&&saves==2,"manual save bypasses input delay");rt.Sim.S.money=654321;rt.MarkDirty();ChapterOneRuntime.NotifyDesktopInput();typeof(ChapterOneRuntime).GetMethod("OnApplicationPause",Flags).Invoke(rt,new object[]{true});Check(saves==3,"pause save remains immediate");rt.Sim.S.money=777777;rt.MarkDirty();typeof(ChapterOneRuntime).GetMethod("OnApplicationQuit",Flags).Invoke(rt,null);Check(saves==4,"quit save remains immediate");Check(rt.ReloadSave()&&rt.Sim.S.money==777777,"actual disk reload keeps latest immediate save");
   var order=Attribute.GetCustomAttribute(typeof(DesktopSaveActivity),typeof(DefaultExecutionOrder)) as DefaultExecutionOrder;Check(order!=null&&order.order< -1500,"input reporter executes before persistence Update");
   var lab=new XgSim();var advance=typeof(XgSim).GetMethod("AdvanceStage",Flags);var snapshot=typeof(XgSim).GetMethod("StageUnlockSnapshot");Check(snapshot!=null,"stage snapshot patch installed");for(int i=1;i<6;i++)advance.Invoke(lab,new object[]{i});
   string json=JsonUtility.ToJson(lab.S);var restored=new XgSim(JsonUtility.FromJson<XgState>(json));Check(restored.S.stagePresentations.Count==5,"JsonUtility restores five pending stages");
   for(int i=2;i<=6;i++){var a=snapshot.Invoke(lab,new object[]{i});var b=snapshot.Invoke(restored,new object[]{i});Check(a!=null&&b!=null&&JsonUtility.ToJson(a)==JsonUtility.ToJson(b),"JsonUtility retains exact unlock snapshot stage "+i);}
   rt.Sim.S.labState=json;rt.MarkDirty();Check(rt.SaveNow()&&rt.ReloadSave(),"save containing queued snapshots survives disk roundtrip");var disk=new XgSim(JsonUtility.FromJson<XgState>(rt.Sim.S.labState));Check(snapshot.Invoke(disk,new object[]{2})!=null&&snapshot.Invoke(disk,new object[]{6})!=null,"disk lab history retains earliest and latest stage");
   Check(disk.CompleteStagePresentation(2)&&snapshot.Invoke(disk,new object[]{2})==null&&snapshot.Invoke(disk,new object[]{3})!=null,"completion only consumes displayed history");
  }
  catch(Exception e){report.status="failed";report.detail+="\n"+e;}
  finally{if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(input!=null&&oldInput!=null)input.SetValue(null,oldInput);report.checks=passed.ToArray();File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));}
  Debug.Log("[IdleSaveAndStageSnapshotQa] "+report.status+" "+output);if(report.status!="passed")throw new Exception(report.detail);
 }
}
#endif
