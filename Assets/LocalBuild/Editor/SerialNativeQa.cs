#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using LingGuangV05.Core.Chat;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class SerialNativeQa
{
 const string Key="SerialNativeQa.Output";
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 [Serializable]public sealed class Request{public string id,reply,failure;public int callbacks,dispatchOrder;public bool dispatched;public double queuedAt,dispatchedAt,callbackAt;[NonSerialized]public Action<string> callback;}
 [Serializable]public sealed class Report{public string scope="Owned StartEditorQa service, real native model, synthetic prompts only; empty isolated Play scene, no game runtime or player save",model,architecture,mode,error,startedUtc,finishedUtc;public int port,serviceId,maxNativeCalls,maxActualTasks;public bool success,singletonPreserved,serviceStopped,cancelObservedBeforeTaskCompleted,nextStartedAfterCancelledTaskCompleted;public List<Request> requests=new List<Request>();public List<string> checks=new List<string>();}
 static string Output=>SessionState.GetString(Key,"");
 static LocalLlm service,original;static GameObject owned;static Report report;static double deadline,phaseAt;static int phase,dispatchCount;static Task[] cancelledTasks;static object owner;static Request cancel,next;static bool finishing;
 static SerialNativeQa(){EditorApplication.playModeStateChanged+=State;}
 public static void Run()
 {
  if(!Application.isBatchMode)throw new InvalidOperationException("Run this destructive empty-scene fixture only in an isolated batch editor.");
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit mode.");
  string path=Path.GetFullPath(Path.Combine("LocalVerification","SerialNative",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));Directory.CreateDirectory(path);SessionState.SetString(Key,path);
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
 }
 static void State(PlayModeStateChange state)
 {
  if(Output.Length==0)return;
  if(state==PlayModeStateChange.EnteredPlayMode){try{Start();}catch(Exception e){Finish(false,e.ToString());}}
  if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;bool ok=File.Exists(Path.Combine(Output,"passed.txt"));SessionState.EraseString(Key);EditorApplication.Exit(ok?0:1);}
 }
 static object Field(object value,string name)=>value.GetType().GetField(name,Flags).GetValue(value);
 static void Check(bool condition,string text){if(!condition)throw new Exception(text);report.checks.Add(text);}
 static int FreePort(){var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int p=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();return p;}
 static void Start()
 {
  report=new Report{startedUtc=DateTime.UtcNow.ToString("O"),port=FreePort()};original=LocalLlm.Instance;Check(UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>()==null,"No game runtime or save owner in isolated scene");
  owned=new GameObject("Owned Serial Native QA");owned.SetActive(false);service=owned.AddComponent<LocalLlm>();owned.SetActive(true);service.StartEditorQa(report.port,Path.Combine(Output,"owned-native"));
  phase=0;dispatchCount=0;finishing=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Tick;
 }
 static Request Chat(string id,LlmSeat seat)
 {
  var r=new Request{id=id,queuedAt=EditorApplication.timeSinceStartup};r.callback=text=>{r.callbacks++;r.reply=text??"";r.callbackAt=EditorApplication.timeSinceStartup;};report.requests.Add(r);
  service.Chat(new List<KeyValuePair<string,string>>{new KeyValuePair<string,string>("system","Synthetic QA. Give a short direct answer. Do not explain."),new KeyValuePair<string,string>("user","Reply with OK. Fixture "+id)},8,0,r.callback,false,null,seat,LlmLane.Visible);Observe();return r;
 }
 static Task[] ActualTasks()
 {
  object native=Field(service,"native");if(native==null)return new Task[0];var tasks=(HashSet<Task>)Field(native,"running");lock(tasks)return tasks.ToArray();
 }
 static void Observe()
 {
  int n=service.NativeCallsInFlight;int real=ActualTasks().Count(t=>!t.IsCompleted);report.maxNativeCalls=Math.Max(report.maxNativeCalls,n);report.maxActualTasks=Math.Max(report.maxActualTasks,real);
  if(n>1||real>1)throw new Exception("Native inference overlap: counter="+n+", incomplete actual Tasks="+real);
  foreach(object job in (IEnumerable)Field(service,"active"))
  {
   var action=Field(job,"done") as Action<string>;var r=report.requests.FirstOrDefault(x=>ReferenceEquals(x.callback,action)&&action!=null);if(r==null||r.dispatched)continue;
   r.dispatched=true;r.dispatchOrder=++dispatchCount;r.dispatchedAt=EditorApplication.timeSinceStartup;
   if(r==next){report.nextStartedAfterCancelledTaskCompleted=cancelledTasks!=null&&cancelledTasks.All(t=>t.IsCompleted);if(!report.nextStartedAfterCancelledTaskCompleted)throw new Exception("Next request dispatched before cancelled native Task returned.");}
  }
 }
 static void Tick()
 {
  if(finishing)return;
  try{
   if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Native serial QA timed out in phase "+phase);
   if(!ReferenceEquals(LocalLlm.Instance,original))throw new Exception("QA changed game singleton.");
   if(service.State==LocalLlm.Status.Failed)throw new Exception("Real model failed: "+service.Detail);
   Observe();
   switch(phase)
   {
    case 0:
     if(!service.Ready)break;Check(service.OwnsServer&&service.ParallelSlots==1,"Owned one-slot real model service ready");report.model=service.ModelName;report.architecture=service.Architecture;report.mode=service.LaunchMode.ToString();report.serviceId=service.OwnedServiceId;
     Check(!string.IsNullOrEmpty(report.model)&&!string.IsNullOrEmpty(report.architecture),"Actual model and native architecture recorded");
     Chat("blocker",LlmSeat.Others);Chat("other-first",LlmSeat.Others);Chat("other-second",LlmSeat.Others);Chat("player-ai",LlmSeat.LingGuang);phase=1;deadline=EditorApplication.timeSinceStartup+160;break;
    case 1:
     if(report.requests.Any(r=>r.callbacks==0)||service.Busy||service.NativeCallsInFlight>0)break;
     foreach(var r in report.requests)Check(r.callbacks==1&&!string.IsNullOrWhiteSpace(r.reply),"Exactly one nonempty native callback: "+r.id);
     Check(report.requests.Find(r=>r.id=="player-ai").dispatchOrder==2,"Queued player AI runs immediately after blocker, ahead of older other chats");
     owner=new object();cancel=new Request{id="cancel-judge",queuedAt=EditorApplication.timeSinceStartup};report.requests.Add(cancel);
     string prompt=string.Join(" ",Enumerable.Repeat("This is a synthetic timing fixture with no player content.",120))+" Answer yes or no: is two plus two four?";
     Check(service.JudgeForEditorQa(prompt,0,(p,f,raw)=>{cancel.callbacks++;cancel.failure=f??"";cancel.reply=raw??"";cancel.callbackAt=EditorApplication.timeSinceStartup;},()=>{cancel.dispatched=true;cancel.dispatchOrder=++dispatchCount;cancel.dispatchedAt=EditorApplication.timeSinceStartup;return true;},owner),"Real cancellation judge accepted");
     cancelledTasks=ActualTasks();report.cancelObservedBeforeTaskCompleted=cancelledTasks.Any(t=>!t.IsCompleted);Check(report.cancelObservedBeforeTaskCompleted,"Cancellation requested while actual native Task is incomplete");
     service.CancelJudges(owner);next=Chat("after-cancel",LlmSeat.LingGuang);phaseAt=EditorApplication.timeSinceStartup;phase=2;deadline=phaseAt+160;break;
    case 2:
     if(next.callbacks==0||cancel.callbacks==0||service.NativeCallsInFlight>0||service.Busy)break;
     Check(cancel.callbacks==1&&cancel.failure=="cancelled","Cancelled native judge callback exactly once with cancelled status");Check(next.callbacks==1&&!string.IsNullOrWhiteSpace(next.reply),"Queued request after cancel completes once with real reply");
     Check(report.nextStartedAfterCancelledTaskCompleted,"Next dispatch occurred only after actual cancelled Task completed");Check(report.maxNativeCalls==1&&report.maxActualTasks<=1,"Observed native and actual Task peak remain serial");
     phase=3;phaseAt=EditorApplication.timeSinceStartup;deadline=phaseAt+10;break;
    case 3:
     if(EditorApplication.timeSinceStartup-phaseAt<1)break;Check(report.requests.All(r=>r.callbacks==1),"No duplicate callbacks after settling");Finish(true,"");break;
   }
  }catch(Exception e){Finish(false,e.ToString());}
 }
 static void Finish(bool success,string error)
 {
  if(finishing)return;finishing=true;EditorApplication.update-=Tick;
  if(report==null)report=new Report();report.error=error;
  try{if(service!=null)service.StopEditorQa();report.serviceStopped=report.serviceId<=0||LocalLlm.ServiceStopped(report.serviceId);if(owned!=null)UnityEngine.Object.Destroy(owned);}
  catch(Exception e){success=false;report.error+="\nCleanup: "+e;}
  report.singletonPreserved=ReferenceEquals(LocalLlm.Instance,original);report.success=success&&report.singletonPreserved&&report.serviceStopped;report.finishedUtc=DateTime.UtcNow.ToString("O");
  Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"result.json"),JsonUtility.ToJson(report,true));if(report.success)File.WriteAllText(Path.Combine(Output,"passed.txt"),"passed");Debug.Log("SerialNativeQa "+(report.success?"PASS":"FAIL")+": "+report.error);EditorApplication.ExitPlaymode();
 }
}
#endif
