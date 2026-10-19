using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.Story;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang {
 // One desktop-level owner; pending stages live in the save until the closing fade finishes.
 public sealed class XgStageCurtain:MonoBehaviour {
  static XgStageCurtain active;XingGuangController owner;XgSim sim;RectTransform root,card,demoRoot;CanvasGroup group;TMP_Text heading,body,buttonLabel;Button nextButton;PrologueDesk desk;XgRealTrainPanel demo;
  XgStageSequenceDemo sequence;readonly List<string> unlockRows=new List<string>();int unlockPage;
  int stage,step;float began;string fullLine;StoryDesktopPresenter presenter;
  public static bool Busy=>active!=null&&active.owner!=null&&active.owner.Sim!=null&&active.owner.Sim.StagePresentationHeld;
  public static bool Showing=>active!=null&&active.root!=null&&active.root.gameObject.activeSelf;
  public static int Step=>active==null?-1:active.step;
  public static bool CoversAbility(int ability)=>Busy&&active.owner.Sim.S.stagePresentations.Contains(ability);
  public static XgStageCurtain Install(XingGuangController owner){var c=owner.GetComponent<XgStageCurtain>()??owner.gameObject.AddComponent<XgStageCurtain>();c.owner=owner;active=c;StoryFocus.ExclusiveProbe=()=>Busy||XgEpilogueDirector.IsHolding(owner);return c;}
  string T(string zh,string en)=>sim!=null&&sim.English?en:zh;
  void Update(){if(owner==null||owner.View==null)return;if(sim!=owner.Sim){StopVisual();sim=owner.Sim;}if(sim==null)return;
   if(!Showing){if(!sim.StagePresentationHeld)return;desk=PrologueDirector.Desk;if(desk==null)return;if(presenter==null)presenter=owner.GetComponent<StoryDesktopPresenter>();if(presenter!=null&&(presenter.CutscenePlaying||presenter.HookPlaying))return;if(root==null)Build();stage=sim.S.stagePresentations[0];root.gameObject.SetActive(true);group.alpha=1;Enter(0);}
   if(step==1&&demo!=null&&!demo.isActiveAndEnabled)demo.TickStageDemo();if(step==1&&sequence!=null)sequence.Tick();
   root.SetAsLastSibling();float t=Time.unscaledTime-began;if(step==0&&t>=2.5f)Next();else if(step==2)body.maxVisibleCharacters=Mathf.Min(fullLine.Length,(int)(t*18));else if(step==4){group.alpha=1-Mathf.Clamp01(t/.4f);if(t>=.4f){sim.CompleteStagePresentation(stage);owner.runtime.MarkDirty();StopVisual();}}
   if(step<4&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)Next();
  }
  void Build(){root=PrologueDesk.Rect("Stage Curtain",desk.layer.parent as RectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);group=root.gameObject.AddComponent<CanvasGroup>();PrologueDesk.Fill(root,new Color(0,0,0,.7f)).raycastTarget=true;
   card=PrologueDesk.Centered("Stage Card",root,Vector2.zero,new Vector2(960,540));var rounded=card.gameObject.AddComponent<LingGuangV05.Desktop.YY.YYRoundRect>();rounded.radius=8;rounded.color=new Color32(12,20,36,255);
   heading=desk.Text(PrologueDesk.Rect("Heading",card,new Vector2(0,1),Vector2.one,new Vector2(32,-100),new Vector2(-32,-32)),"",34,Color.white,TextAlignmentOptions.Center);
   body=desk.Text(PrologueDesk.Rect("Body",card,Vector2.zero,Vector2.one,new Vector2(40,90),new Vector2(-40,-120)),"",26,Color.white,TextAlignmentOptions.Center);body.textWrappingMode=TextWrappingModes.Normal;
   demoRoot=PrologueDesk.Centered("Live Demo",card,Vector2.zero,new Vector2(960,540));
   var rt=PrologueDesk.Centered("Continue",card,new Vector2(0,-222),new Vector2(160,40));var bg=PrologueDesk.Fill(rt,new Color32(40,80,110,255));nextButton=rt.gameObject.AddComponent<Button>();nextButton.targetGraphic=bg;nextButton.onClick.AddListener(Next);buttonLabel=desk.Text(PrologueDesk.Rect("Text",rt,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"",20,Color.white,TextAlignmentOptions.Center);
   sequence=new XgStageSequenceDemo(desk,demoRoot);root.gameObject.SetActive(false);
  }
  void Enter(int value){step=value;began=Time.unscaledTime;demo?.EndStageDemo();sequence?.End();demoRoot.gameObject.SetActive(false);heading.gameObject.SetActive(true);body.gameObject.SetActive(true);body.maxVisibleCharacters=int.MaxValue;nextButton.gameObject.SetActive(value<4);buttonLabel.text=T("继续","Continue");card.sizeDelta=new Vector2(value==1?960:value==0?root.rect.width*.6f:720,540);heading.text="";body.text="";body.fontSize=26;
   if(value==0){string[] zh={"","一月","二月","三月","四月","五月","六月","七月","八月","九月","十月","十一月","十二月"};var date=GameCalendar.FirstDay(stage);heading.text=T(zh[date.Month],date.ToString("MMMM",System.Globalization.CultureInfo.InvariantCulture));body.text=MonthSubtitle();}
   if(value==1){heading.gameObject.SetActive(false);body.gameObject.SetActive(false);demoRoot.gameObject.SetActive(true);if(stage>=4){sequence.Begin(stage,sim.English);demo=null;}else{demo=owner.View.GetComponent<XgRealTrainPanel>();if(demo!=null)demo.BeginStageDemo(demoRoot,stage);else{body.gameObject.SetActive(true);body.text=T("演示暂不可用","Demonstration unavailable");}}nextButton.transform.SetAsLastSibling();}
   if(value==2){heading.text=sim.AiDisplayName;fullLine=FirstLine(stage);body.text=fullLine;body.maxVisibleCharacters=0;}
   if(value==3){BuildUnlocks(stage);unlockPage=0;ShowUnlockPage();body.fontSize=sim.English?20:24;}
   if(value==4){card.gameObject.SetActive(false);}
  }
  string FirstLine(int n){switch(n){case 2:return sim.SeedSaysYes?T("是。","Yes."):T("否。","No.");case 3:return T("0。我看见了。","Zero. I can see it.");case 4:return T("你说的，我记住了。","I remember what you say.");case 5:return T("这句话，我想换一种说法。","I want to say that another way.");default:return T("如果只用注意力呢？","What if we used only attention?");}}
  string MonthSubtitle(){
   var world=owner.runtime==null?null:owner.runtime.Sim;
   if(world!=null&&world.Debt>0)return T("房租还没交，电脑上的新东西倒越来越多。","The rent is still unpaid. The computer keeps changing.");
   switch(GameCalendar.FirstDay(stage).Month){
    case 7:return T("风扇轻轻转着。它终于不只会猜了。","The fan hums softly. It can do more than guess now.");
    case 8:return T("窗外还是热的，屏幕里多了一点变化。","It is still hot outside. Something on the screen has changed.");
    case 9:return T("下班天黑得早了。它读过的话，留得更久了。","It is dark after work now. The words it reads stay longer.");
    case 10:return T("天气凉下来，它开始读懂更长的话。","The weather is cooler. It is beginning to follow longer sentences.");
    case 11:return T("桌上还是那台电脑，说话的方式却变了。","The same computer is on the desk. The way it speaks has changed.");
    case 12:return T("今年快结束了。我还想试试另一种办法。","The year is nearly over. I want to try another way.");
    default:return T("下班回来，电脑还亮着。","Back from work. The computer is still on.");
   }
  }
  void BuildUnlocks(int n){
   unlockRows.Clear();unlockRows.Add(T("能力：","Ability: ")+XgSim.AbilityName(n,sim.English));
   if(n==2)unlockRows.Add(T("新软件：硬件商店","New app: hardware shop"));
   else if(n==3)unlockRows.Add(T("新软件：桌面小游戏","New app: desktop games"));
   else if(n==4)unlockRows.Add(T("新软件：摆渡众包","New app: Bodu Crowd"));
   var snapshot=sim.StageUnlockSnapshot(n);
   // Old saves never recorded a before/after diff. Do not invent newly unlocked rows.
   if(snapshot==null){unlockRows.Add(T("旧存档未记录新增项目，请查看科技与题桌。","This older save has no unlock history. Check research and desks."));return;}
   foreach(var pack in XgStageLessons.Desks)if(snapshot.desks!=null&&snapshot.desks.Contains(pack.id))
    unlockRows.Add(T("新题桌：","New desk: ")+T(pack.name,pack.nameEn));
   foreach(var id in snapshot.nodes??new List<string>()){var node=XgCatalog.Node(id);if(node!=null)
    unlockRows.Add(T("新科技：","New research: ")+T(node.name,node.nameEn));}

  }
  void ShowUnlockPage(){
   int pages=Math.Max(1,(unlockRows.Count+5)/6);heading.text=T("新的变化","What changed")+(pages>1?"  "+(unlockPage+1)+" / "+pages:"");
   var lines=new List<string>();for(int i=unlockPage*6;i<unlockRows.Count&&i<unlockPage*6+6;i++)lines.Add("• "+unlockRows[i]);body.text=string.Join("\n",lines);
   buttonLabel.text=unlockPage+1<pages?T("继续","Continue"):T("好","Done");
  }
  public void Next(){if(!Showing||step>=4)return;if(step==3&&(unlockPage+1)*6<unlockRows.Count){unlockPage++;ShowUnlockPage();return;}if(step==2&&body.maxVisibleCharacters<fullLine.Length){body.maxVisibleCharacters=fullLine.Length;began=Time.unscaledTime-fullLine.Length/18f;return;}Enter(step+1);}
  void StopVisual(){demo?.EndStageDemo();sequence?.End();if(root!=null){root.gameObject.SetActive(false);card.gameObject.SetActive(true);}step=-1;}
  void OnDestroy(){StopVisual();if(active==this){active=null;StoryFocus.ExclusiveProbe=null;}if(root!=null)Destroy(root.gameObject);}
 }
}
