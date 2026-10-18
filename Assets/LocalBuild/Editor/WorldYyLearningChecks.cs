using System;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using UnityEngine;
using UnityEditor;
public static class WorldYyLearningChecks
{
 const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Fields).SetValue(obj,value);
 static object Get(object obj,string field)=>obj.GetType().GetField(field,Fields).GetValue(obj);
 static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Fields).Invoke(obj,args);
 static void Check(bool yes,string name){if(!yes)throw new Exception(name);}
 [MenuItem("LingGuang/Checks/YY learning routes")]
 public static void Run()
 {
  var hubProperty=typeof(YYChatHub).GetProperty("Instance");var oldHub=YYChatHub.Instance;
  var talkField=typeof(XgYyTalk).GetField("instance",BindingFlags.NonPublic|BindingFlags.Static);var oldTalk=talkField.GetValue(null);
  var go=new GameObject("YYLearningIsolatedCheck");go.SetActive(false);
  try{
   var rt=go.AddComponent<ChapterOneRuntime>();rt.SetSimulationForTests(new ChapterOneSim());
   var controller=go.AddComponent<XingGuangController>();controller.runtime=rt;var sim=new XgSim();typeof(XingGuangController).GetProperty("Sim").SetValue(controller,sim);
   var hub=go.AddComponent<YYChatHub>();hub.runtime=rt;var state=new YYState{lingguangUnlocked=true,selected=YYChatHub.LingGuangId};state.conversations.Add(new YYConversation{id=YYChatHub.LingGuangId});typeof(YYChatHub).GetProperty("S").SetValue(hub,state);hubProperty.SetValue(null,hub);
   var talk=go.AddComponent<XgYyTalk>();Set(talk,"controller",controller);Set(talk,"hooked",hub);talkField.SetValue(null,talk);Call(talk,"Bind",sim);hub.LingGuangTalk=talk;
   // Deterministic existing silence response avoids model/network requests; real send and delivery paths still execute.
   sim.S.listeningUntil=1000000;
   sim.RecordTeachingQuestion("三加五是八吗？","Is three plus five eight?",true,true,"","","sense");
   Check(sim.S.askedLog.Count==1,"actual teaching record reaches YY notebook");
   Check(XgYyTalk.AskFromNotebook(sim.S.askedLog[0]),"notebook send accepted");
   Check(talk.Thinking && Get(talk,"due")!=null,"notebook actually invokes Heard and queues an answer");
   Call(talk,"Deliver",hub);var conv=hub.Conversation(YYChatHub.LingGuangId);var message=conv.messages[conv.messages.Count-1];
   Check(message.from==YYChatHub.LingGuangId && message.learningReplyId>0 && message.learningComparison!=0,"delivered notebook answer has comparison and reply ID");
   var pack=XgSim.PersonaPacks[0];var question=pack.questions.Find(q=>q.stage<=sim.S.stage);Check(question!=null,"early persona question exists");
   Check(XgYyTalk.AskPersona(pack,question),"persona question send accepted");Check(talk.Thinking,"persona question actually invokes Heard");Call(talk,"Deliver",hub);
   message=conv.messages[conv.messages.Count-1];Check(message.learningPersona && message.learningReplyId>0,"persona reply offers tags");
   Check(XgYyTalk.TagLearningReply(message.learningReplyId,"warm",out _),"first tag accepted");Check(XgYyTalk.TagLearningReply(message.learningReplyId,"own",out _),"second tag accepted");Check(!XgYyTalk.TagLearningReply(message.learningReplyId,"funny",out _),"third tag rejected");
   var unlock=XgSim.ChatUnlocks[0];hub.Send(YYChatHub.LingGuangId," "+unlock.phrase);Call(talk,"Deliver",hub);Check(!sim.ChatNodeReady(unlock.node),"leading whitespace remains a nonmatch through real hub");
   hub.Send(YYChatHub.LingGuangId,unlock.phrase);Check(!sim.ChatNodeReady(unlock.node),"sending alone never unlocks");Call(talk,"Deliver",hub);Check(sim.ChatNodeReady(unlock.node),"exact delivered reply unlocks node");
   sim.S.stage=6;Check(XgYyTalk.AskFromNotebook(sim.S.askedLog[0]),"stage-six notebook send");
   // Controlled model-output fixture tests delivery-trigger detection, never a production forced event.
   Set(Get(talk,"due"),"text","下一题：3 加 5 等于多少？");Call(talk,"Deliver",hub);Check(sim.S.quizQueued.Contains("q6c"),"delivered notebook continuation queues its factual quiz");
   Debug.Log("WorldYyLearningChecks PASS: notebook/persona real send-delivery, 2 tags, exact phrase after reply, notebook continuation.");
  }finally{UnityEngine.Object.DestroyImmediate(go);hubProperty.SetValue(null,oldHub);talkField.SetValue(null,oldTalk);}
 }
}
