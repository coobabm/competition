using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
public static class WorldYyLearningUiChecks
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static void Set(object x,string n,object v)=>x.GetType().GetField(n,F).SetValue(x,v);
 static object Get(object x,string n)=>x.GetType().GetField(n,F).GetValue(x);
 static object Call(object x,string n,params object[] a)=>x.GetType().GetMethod(n,F).Invoke(x,a);
 static void Check(bool b,string n){if(!b)throw new Exception(n);}
 static RectTransform Rect(Transform p,string n){var g=new GameObject(n,typeof(RectTransform));g.transform.SetParent(p,false);return (RectTransform)g.transform;}
 static List<Button> Rows(Transform p)=>p.GetComponentsInChildren<Button>(true).Where(x=>x.gameObject.activeSelf&&x.transform.parent==p).ToList();
 static string Label(Button b)=>b.GetComponentInChildren<TMP_Text>(true).text;
 [MenuItem("LingGuang/Checks/YY learning UI")]
 public static void Run(){CheckLocale(false);CheckLocale(true);Debug.Log("WorldYyLearningUiChecks PASS: real notebook/persona toolbar, 260px layout, 200 stored/50 shown, newest/search, send/delivery, tags and eight pack ownership in both languages.");}
 static void CheckLocale(bool english)
 {
  using(var locale=GameText.Bind(()=>english?GameText.English:GameText.Chinese)){
  var oldHub=YYChatHub.Instance;var hubProperty=typeof(YYChatHub).GetProperty("Instance");
  var talkField=typeof(XgYyTalk).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic);var oldTalk=talkField.GetValue(null);
  var go=new GameObject("YYLearningUiIsolated",typeof(RectTransform));go.SetActive(false);
  try{
   var rt=go.AddComponent<ChapterOneRuntime>();rt.SetSimulationForTests(new ChapterOneSim());
   var controller=go.AddComponent<XingGuangController>();controller.runtime=rt;var sim=new XgSim();sim.English=english;typeof(XingGuangController).GetProperty("Sim").SetValue(controller,sim);
   var hub=go.AddComponent<YYChatHub>();hub.runtime=rt;var state=new YYState{lingguangUnlocked=true,selected=YYChatHub.LingGuangId};state.conversations.Add(new YYConversation{id=YYChatHub.LingGuangId});typeof(YYChatHub).GetProperty("S").SetValue(hub,state);hubProperty.SetValue(null,hub);
   var talk=go.AddComponent<XgYyTalk>();Set(talk,"controller",controller);Set(talk,"hooked",hub);talkField.SetValue(null,talk);Call(talk,"Bind",sim);hub.LingGuangTalk=talk;sim.S.listeningUntil=1000000;
   var view=go.AddComponent<YYChatView>();Set(view,"hub",hub);Set(view,"font",TMP_Settings.defaultFontAsset);
   var root=Rect(go.transform,"Root");root.sizeDelta=new Vector2(1020,700);Set(view,"root",root);
   var main=Rect(root,"Conversation");main.anchorMin=Vector2.zero;main.anchorMax=Vector2.one;main.offsetMin=new Vector2(244,0);main.offsetMax=Vector2.zero;Rect(main,"Tools");
   var area=Rect(main,"Messages");area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;area.offsetMin=new Vector2(0,174);area.offsetMax=new Vector2(0,-58);Set(view,"messageArea",area);Set(view,"scrollContent",Rect(area,"MessagesContent"));
   for(int i=0;i<205;i++)sim.RecordAsked("sense","Probe "+i.ToString("000"),"yes","no",i);
   Check(sim.S.askedLog.Count==200&&sim.S.askedLog[0].question=="Probe 005","per-bank 200 newest retention");
   Call(view,"RefreshLearningTools");((Button)Get(view,"notebookTool")).onClick.Invoke();
   var drawer=(RectTransform)Get(view,"learningDrawer");var content=(RectTransform)Get(view,"learningContent");root.ForceUpdateRectTransforms();
   Check(drawer.gameObject.activeSelf&&Mathf.Abs(drawer.rect.height-260)<.1f,"real notebook button opens 260px drawer");Check(area.rect.height>=72,"default chat preserves at least three 24px text lines");
   var rows=Rows(content);Check(rows.Count==51,"one bank heading plus newest 50 questions");Check(Label(rows[1]).StartsWith("Probe 204")&&Label(rows[50]).StartsWith("Probe 155"),"newest first, correct 50 boundary");
   var search=(TMP_InputField)Get(view,"learningSearch");search.text="Probe 010";rows=Rows(content);Check(rows.Count==2&&Label(rows[1]).StartsWith("Probe 010"),"search reaches older retained entry");
   rows[1].onClick.Invoke();Check(talk.Thinking&&!drawer.gameObject.activeSelf,"actual notebook row sends and closes drawer");Call(talk,"Deliver",hub);var conv=hub.Conversation(YYChatHub.LingGuangId);Check(conv.messages.Last().learningComparison!=0,"real notebook reply comparison");
   ((Button)Get(view,"personaTool")).onClick.Invoke();rows=Rows(content);Check(XgSim.PersonaPacks.Count==8,"eight authored packs loaded");
   var question=rows.First(x=>x.interactable&&XgSim.PersonaPacks[0].questions.Any(q=>Label(x)==GameText.T(q.q,q.qEn)));question.onClick.Invoke();Check(talk.Thinking&&!drawer.gameObject.activeSelf,"actual personality question sends");Call(talk,"Deliver",hub);var reply=conv.messages.Last();Check(reply.learningPersona,"delivered reply is taggable");
   var messages=(RectTransform)Get(view,"scrollContent");Call(view,"LearningBubble",reply,0f,760f);var tags=Rows(messages);Check(tags.Count==9,"nine real tag buttons");
   // Core tag routing is verified by the production public API; invoking a tag's FloatUp coroutine requires Play mode.
   Check(XgYyTalk.TagLearningReply(reply.learningReplyId,XgSim.PersonaTags[0].id,out _),"first actual reply tag");Check(XgYyTalk.TagLearningReply(reply.learningReplyId,XgSim.PersonaTags[1].id,out _),"second actual reply tag");Check(!XgYyTalk.TagLearningReply(reply.learningReplyId,XgSim.PersonaTags[2].id,out _),"third tag rejected");
   foreach(var child in messages.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);Call(view,"LearningBubble",reply,0f,760f);Check(Rows(messages).All(x=>!x.interactable),"all tag buttons lock after two tags");
   sim.S.stage=6;sim.S.teachingFlow=false;var host=new XgUiMoneyHost();foreach(var p in XgSim.PersonaPacks){if(!sim.OwnsPersonaPack(p.id))Check(sim.BuyNode("persona."+p.id,host),"actual tech purchase "+p.id);Check(sim.OwnsPersonaPack(p.id),"pack ownership "+p.id);}
   ((Button)Get(view,"personaTool")).onClick.Invoke();rows=Rows(content);foreach(var p in XgSim.PersonaPacks)Check(rows.Any(x=>Label(x)==GameText.T(p.name,p.nameEn)&&!x.interactable),"owned pack heading "+p.id);
  }finally{UnityEngine.Object.DestroyImmediate(go);hubProperty.SetValue(null,oldHub);talkField.SetValue(null,oldTalk);}
  }
 }
 sealed class XgUiMoneyHost:IXgHost{public double Money{get;private set;}=1000000;public bool Spend(double n){if(n>Money)return false;Money-=n;return true;}public void Earn(double n){Money+=n;}public double Compute=>1;public double VramMB=>4096;public string Blocker=>null;public void Train(double seconds){}}
}
