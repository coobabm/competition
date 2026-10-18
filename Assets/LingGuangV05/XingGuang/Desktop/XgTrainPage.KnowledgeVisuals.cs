using static LingGuangV05.Desktop.XingGuang.XgUi;
using System;using System.Collections;using System.Text.RegularExpressions;
using LingGuangV05.XingGuang;using LingGuangV05.Desktop.Story;using UnityEngine;using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang
{
 public sealed partial class XgTrainPage
 {
  RectTransform lessonBox,knowledgeFlightVisual;XgKnowledgeHalo knowledgeHalo;Coroutine knowledgeFlight;bool knowledgeFlying;
  static readonly Regex KnowledgeTermPattern=new Regex("Vanishing gradient|Learning rate|Noisy labels|Not a Number|Overfitting|Grokking|Emergence|Ceiling|NaN|ReLU|dropout|过拟合|学习率|涌现|天花板|顿悟|随机失活|激活函数|检查点",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
  public static string KnowledgeTerms(string text)=>KnowledgeTermPattern.Replace(text??"",m=>"<b><color=#E8B65A>"+m.Value+"</color></b>");
  string KnowledgeTitle(XgLessonDefinition card){string name=T(card.title,card.titleEn);string term=card.id=="grokking"?"Grokking":card.id=="emergence"?"Emergence":"";return term.Length>0&&name.IndexOf(term,StringComparison.OrdinalIgnoreCase)<0?name+" · "+term:name;}
  void UpdateKnowledgeHalo(string id)
  {
   if(lessonBox!=null){float height=Mathf.Min(460,Mathf.Max(300,root.rect.height-24));lessonBox.sizeDelta=new Vector2(Mathf.Min(height*.88f,Mathf.Max(250,root.rect.width-24)),height);lessonTitle.fontSize=20;lessonTitle.rectTransform.offsetMin=new Vector2(24,-88);lessonTitle.rectTransform.offsetMax=new Vector2(-24,-16);lessonBody.rectTransform.offsetMax=new Vector2(-24,-100);lessonBody.fontSize=height<360?15:17;}
   if(knowledgeHalo==null){var go=new GameObject("Observed curve halo",typeof(RectTransform),typeof(CanvasRenderer));var rt=(RectTransform)go.transform;rt.SetParent(lessonLayer,false);rt.sizeDelta=new Vector2(36,36);knowledgeHalo=go.AddComponent<XgKnowledgeHalo>();knowledgeHalo.raycastTarget=false;rt.SetAsFirstSibling();}
   Vector2 point=Vector2.zero;int index;float value;
   bool valid=Sim.TrainingLessonCurvePoint(id,Sim.Run(Track),out index,out value)&&chart!=null&&chart.LessonPoint(index,value,out point);
   knowledgeHalo.gameObject.SetActive(valid);if(valid)knowledgeHalo.rectTransform.position=chart.rectTransform.TransformPoint(point);
  }
  void CollectKnowledgeVisual()
  {
   if(knowledgeFlying||string.IsNullOrEmpty(shownLessonId))return;
   var owner=Sim;var card=XgTrainingLessons.Card(shownLessonId);bool fresh=!owner.S.lessonCollected.Contains(shownLessonId);owner.CollectTrainingLesson(shownLessonId);
   if(!fresh){shownLessonId="";reviewLessonId="";HideTrainingLessons();view.Refresh(true);return;}
   var target=view.NavButton("tree");knowledgeFlying=true;lessonCollect.button.interactable=false;knowledgeFlight=view.StartCoroutine(FlyKnowledge(owner,card,target));
  }
  IEnumerator FlyKnowledge(XgSim owner,XgLessonDefinition card,RectTransform target)
  {
   // Store the collection before animating; closing a window or quitting cannot lose it.
   Transform common=target!=null?target.parent:lessonLayer.parent;while(common!=null&&!lessonLayer.IsChildOf(common))common=common.parent;
   if(common!=null&&target!=null){knowledgeFlightVisual=UnityEngine.Object.Instantiate(lessonBox,common,true);knowledgeFlightVisual.name="Knowledge card flying to Tech";knowledgeFlightVisual.SetAsLastSibling();foreach(var g in knowledgeFlightVisual.GetComponentsInChildren<Graphic>())g.raycastTarget=false;foreach(var b in knowledgeFlightVisual.GetComponentsInChildren<Button>())b.interactable=false;lessonBox.gameObject.SetActive(false);
    Vector3 begin=knowledgeFlightVisual.position,scale=knowledgeFlightVisual.localScale;float duration=(GamePerformance.Low||owner.S.reduceFx) ? .16f : .55f;
    for(float t=0;t<duration;t+=Time.unscaledDeltaTime){if(!ReferenceEquals(owner,Sim)||!view.Visible)break;float u=Mathf.Clamp01(t/duration);u=u*u*(3-2*u);knowledgeFlightVisual.position=Vector3.Lerp(begin,target.TransformPoint(target.rect.center),u);knowledgeFlightVisual.localScale=scale*Mathf.Lerp(1,.055f,u);yield return null;}
   }
   if(knowledgeFlightVisual!=null)UnityEngine.Object.Destroy(knowledgeFlightVisual.gameObject);knowledgeFlightVisual=null;knowledgeFlight=null;knowledgeFlying=false;if(lessonBox!=null)lessonBox.gameObject.SetActive(true);
   shownLessonId="";reviewLessonId="";lessonLayer.gameObject.SetActive(false);owner.TrainingLessonModalOpen=false;
   if(ReferenceEquals(owner,Sim)&&view.Visible){if(card!=null&&!string.IsNullOrEmpty(card.voice))InnerVoice.Say(card.voice,card.voiceEn,3,null,false);view.Refresh(true);}
  }
  void CancelKnowledgeFlight(){if(knowledgeFlight!=null&&view!=null)view.StopCoroutine(knowledgeFlight);knowledgeFlight=null;knowledgeFlying=false;if(knowledgeFlightVisual!=null)UnityEngine.Object.Destroy(knowledgeFlightVisual.gameObject);knowledgeFlightVisual=null;if(lessonBox!=null)lessonBox.gameObject.SetActive(true);}
 }
}



