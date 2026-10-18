#if UNITY_EDITOR
using System;using System.Collections.Generic;using System.Reflection;using System.Linq;
using LingGuangV05.Core;using LingGuangV05.Runtime;using LingGuangV05.XingGuang;using LingGuangV05.Desktop;using LingGuangV05.Desktop.XingGuang;using UnityEngine;using UnityEditor;using Michsky.DreamOS;
public static class DesktopHotpathChecks
{
 const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;static int count;static void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;Debug.Log("[DesktopHotpathChecks] PASS "+text);}
 [MenuItem("LingGuang/Verification/Desktop hot paths")]
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run in Edit Mode with isolated inactive fixture");count=0;var root=new GameObject("Desktop hotpath isolated fixture",typeof(RectTransform));root.SetActive(false);var rt=root.AddComponent<ChapterOneRuntime>();rt.SetSimulationForTests(new ChapterOneSim());var ctl=root.AddComponent<XingGuangController>();ctl.runtime=rt;var sim=new XgSim();typeof(XingGuangController).GetProperty("Sim").SetValue(ctl,sim);IDisposable locale=null;
  try
  {
   var state=rt.Sim.S;state.aiName="测试";state.aiSelf="我";state.aiCallMe="你";state.personality="测试性格";state.toneWords=new List<string>{"认真"};
   var method=typeof(XingGuangController).GetMethod("SyncProfile",Flags);var sync=(Action<GameState>)Delegate.CreateDelegate(typeof(Action<GameState>),ctl,method);sync(state);var original=sim.Profile;
   for(int i=0;i<100;i++)sync(state);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<2000;i++)sync(state);long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
   Check(allocated==0,"unchanged profile: 2000 real calls allocate 0 bytes (measured "+allocated+")");Check(ReferenceEquals(original,sim.Profile),"unchanged profile does not replace personality");
   state.toneWords[0]="活泼";sync(state);Check(sim.Profile.words[0]=="活泼"&&!ReferenceEquals(original,sim.Profile),"in-place tone edit detected");state.targetWarmth+=1;sync(state);Check(sim.Profile.warmth==state.targetWarmth,"numeric profile edit detected");state.toneWords=null;sync(state);Check(sim.Profile.words.Count==0,"legacy null tones accepted without list churn");original=sim.Profile;locale=GameText.Bind(()=>GameText.English);sync(state);Check(!ReferenceEquals(original,sim.Profile),"locale change refreshes translated profile");
   var frame=new GameObject("Fixture frame",typeof(RectTransform));frame.transform.SetParent(root.transform,false);var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1920,1030);var window=root.AddComponent<WindowManager>();window.windowContainer=(RectTransform)frame.transform;window.windowDragger=null;
   typeof(DesktopWindowIndex).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);var listed=DesktopWindowIndex.Get(rt);Check(listed.Contains(window),"actual inactive window included by scene traversal");var native=UnityEngine.Object.FindObjectsByType<WindowManager>(FindObjectsInactive.Include,FindObjectsSortMode.None);Check(native.All(w=>listed.Contains(w)),"shared index covers all current scene windows");int scans=DesktopWindowIndex.WindowScans;for(int i=0;i<1000;i++)DesktopWindowIndex.Get(rt);Check(DesktopWindowIndex.WindowScans==scans,"1000 queries reuse registry without rediscovering the scene");
   var defaults=root.AddComponent<DesktopWindowDefaults>();var prepare=typeof(DesktopWindowDefaults).GetMethod("Prepare",Flags);root.name="Web Browser";prepare.Invoke(defaults,new object[]{window,false});Check(Vector2.Distance(window.windowContainer.rect.size,DesktopWindowDefaults.PreferredSize("Web Browser"))<.1,"native browser first-open dimensions preserved");prepare.Invoke(defaults,new object[]{window,true});window.windowContainer.anchoredPosition+=new Vector2(53,29);var position=window.windowContainer.anchoredPosition;var size=window.windowContainer.sizeDelta;prepare.Invoke(defaults,new object[]{window,false});Check(window.windowContainer.anchoredPosition==position&&window.windowContainer.sizeDelta==size,"later player move/resize preserved");
   var extra=new GameObject("Late window fixture",typeof(RectTransform));extra.SetActive(false);extra.transform.SetParent(root.transform,false);var late=extra.AddComponent<WindowManager>();DesktopWindowIndex.RegisterTree(extra);Check(DesktopWindowIndex.Get(rt).Contains(late),"factory explicitly registers dynamically added never-enabled inactive window");UnityEngine.Object.DestroyImmediate(extra);Check(!DesktopWindowIndex.Get(rt).Contains(late),"query prunes never-enabled destroyed window");
   Debug.Log("DESKTOP_HOTPATH_PASS "+count+" checks; allocation result applies only unchanged SyncProfile. No FPS claim.");
  }
  finally{locale?.Dispose();UnityEngine.Object.DestroyImmediate(root);typeof(DesktopWindowIndex).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);}
 }
}
#endif
