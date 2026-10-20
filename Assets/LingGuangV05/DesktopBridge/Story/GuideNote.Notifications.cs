using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.CaseMenu;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.Story
{
    public sealed partial class GuideNote
    {
        readonly XgGuideNotificationState taskNotifications=new XgGuideNotificationState();
        XgSim taskOwner;
        RectTransform taskButtonRoot,taskToast;
        Image taskButtonImage,taskUnreadDot;
        TMP_Text taskButtonText,taskToastTitle,taskToastBody,taskEmpty,taskViewCaption;
        CanvasGroup taskToastGroup;
        bool taskListOpen,toastLive;
        float toastAge,unreadSince;
        CaseMenuController taskMenu;
        XgGuideStep noticeStep;
        bool TaskReduced=>GameSettings.ReducedMotion || lab!=null&&lab.Sim!=null&&lab.Sim.S.reduceFx;
        bool TaskPresentationAllowed()=>!XgIdleWindow.IsCompact&&!StoryFocus.Exclusive&&!CutscenePlaying&&!XgStageCurtain.Busy&&!XgEmergenceCutscene.Busy
            &&(taskMenu==null||taskMenu.HasEnteredGame&&!taskMenu.IsTransitioning&&!taskMenu.IsSettingsOpen);
        public void OpenTaskList()
        {
            if(!TaskPresentationAllowed()||note==null)return;
            taskListOpen=true;collapsed=false;taskNotifications.Acknowledge();toastLive=false;
            note.gameObject.SetActive(true);Layout();
            if(taskEmpty!=null)taskEmpty.gameObject.SetActive(shown.Count==0);
            ClickTaskSound();
        }
        void ClickTaskSound(){if(lab!=null&&lab.Sim!=null&&!lab.Sim.S.mute)lab.GetComponentInChildren<XgJuice>()?.Play(XgJuice.Sfx.Id.Click,1,.4f);}
        void AcknowledgeTaskNotice(){taskNotifications.Acknowledge();toastLive=false;ClickTaskSound();}
        List<XgGuideStep> ObserveTaskNotifications(List<XgGuideStep> steps)
        {
            if(lab==null||lab.Sim==null)return new List<XgGuideStep>();
            if(!ReferenceEquals(taskOwner,lab.Sim))
            {
                taskOwner=lab.Sim;taskNotifications.Reset();toastLive=false;taskListOpen=false;noticeStep=null;
                if(busy){StopAllCoroutines();busy=false;pending=null;}
                foreach(var row in rows)if(row!=null)row.group.alpha=1;
            }
            // The original lesson still receives TopStep. Only the task surface starts empty.
            var visible=new List<XgGuideStep>();
            if(!runtime.Sim.InPrologue&&lab.Sim.TeachingTreeOpen)
                foreach(var step in steps)if(step!=null&&step.id!="open.exe"&&step.id!="setup.answer"&&step.id!="lesson.judge")visible.Add(step);
            bool before=taskNotifications.Unread;
            taskNotifications.Observe(visible,Time.unscaledTime,lab.Sim.TeachingTreeOpen);
            if(!before&&taskNotifications.Unread)unreadSince=Time.unscaledTime;
            if(taskListOpen)taskNotifications.Acknowledge();
            return visible;
        }
        void BuildTaskNotifications(PrologueDesk d)
        {
            taskMenu=FindAnyObjectByType<CaseMenuController>();
            taskButtonRoot=PrologueDesk.Rect("Tasks entry",d.layer,new Vector2(0,0),new Vector2(0,0),new Vector2(160,5),new Vector2(252,35));
            taskButtonImage=PrologueDesk.Fill(taskButtonRoot,new Color32(42,58,76,255),true);
            var button=taskButtonRoot.gameObject.AddComponent<Button>();button.targetGraphic=taskButtonImage;
            button.onClick.AddListener(()=>{if(!TaskPresentationAllowed())return;if(taskListOpen){taskListOpen=false;note.gameObject.SetActive(false);AcknowledgeTaskNotice();}else OpenTaskList();});
            taskButtonText=d.Text(PrologueDesk.Rect("Label",taskButtonRoot,Vector2.zero,Vector2.one,new Vector2(8,0),new Vector2(-16,0)),"",15,Color.white,TextAlignmentOptions.Center);
            taskUnreadDot=PrologueDesk.Fill(PrologueDesk.Rect("Unread",taskButtonRoot,Vector2.one,Vector2.one,new Vector2(-12,-12),new Vector2(-5,-5)),new Color32(255,191,84,255),false);
            taskUnreadDot.sprite=PrologueDesk.Circle();
            taskEmpty=d.Text(PrologueDesk.Rect("Empty task list",body,new Vector2(0,1),Vector2.one,new Vector2(10,-65),new Vector2(-10,-30)),"",16,Soft,TextAlignmentOptions.Center);
            taskEmpty.gameObject.SetActive(false);
            taskToast=PrologueDesk.Rect("Task notification",d.layer,new Vector2(1,0),new Vector2(1,0),new Vector2(-314,48),new Vector2(-14,144));
            PrologueDesk.Fill(taskToast,new Color32(33,46,62,250),true);
            taskToastGroup=taskToast.gameObject.AddComponent<CanvasGroup>();
            taskToastTitle=d.Text(PrologueDesk.Rect("Title",taskToast,Vector2.zero,Vector2.one,new Vector2(12,65),new Vector2(-35,-8)),"",16,new Color32(255,210,125,255),TextAlignmentOptions.TopLeft);
            taskToastBody=d.Text(PrologueDesk.Rect("Body",taskToast,Vector2.zero,Vector2.one,new Vector2(12,31),new Vector2(-12,-32)),"",14,Color.white,TextAlignmentOptions.TopLeft);
            taskToastBody.textWrappingMode=TextWrappingModes.Normal;taskToastBody.overflowMode=TextOverflowModes.Ellipsis;
            var view=PrologueDesk.Rect("View task",taskToast,Vector2.zero,Vector2.zero,new Vector2(205,5),new Vector2(288,29));
            var viewImage=PrologueDesk.Fill(view,new Color32(65,91,118,255),true);var viewButton=view.gameObject.AddComponent<Button>();viewButton.targetGraphic=viewImage;
            taskViewCaption=d.Text(PrologueDesk.Rect("Label",view,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),GameText.T("查看","View"),14,Color.white,TextAlignmentOptions.Center);
            viewButton.onClick.AddListener(()=>{if(!TaskPresentationAllowed())return;var step=noticeStep;OpenTaskList();if(step!=null&&shown.Exists(s=>s.id==step.id))Follow(step);});
            var close=PrologueDesk.Rect("Dismiss task notice",taskToast,Vector2.one,Vector2.one,new Vector2(-30,-27),new Vector2(-5,-3));
            var closeImage=PrologueDesk.Fill(close,Color.clear,true);var closeButton=close.gameObject.AddComponent<Button>();closeButton.targetGraphic=closeImage;
            d.Text(PrologueDesk.Rect("Label",close,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"×",18,Color.white,TextAlignmentOptions.Center);
            closeButton.onClick.AddListener(()=>{if(TaskPresentationAllowed())AcknowledgeTaskNotice();});
            taskToast.gameObject.SetActive(false);
        }
        void TickTaskNotifications(bool ready)
        {
            if(taskButtonRoot==null)return;
            taskButtonRoot.gameObject.SetActive(ready);
            taskEmpty.text=GameText.T("暂无任务","No tasks yet");taskButtonText.text=GameText.T("任务","Tasks");
            taskViewCaption.text=GameText.T("查看","View");
            taskUnreadDot.gameObject.SetActive(taskNotifications.Unread);
            float time=Time.unscaledTime-unreadSince;
            float pulse=!TaskReduced&&taskNotifications.Unread?(Mathf.Sin(time*Mathf.PI*2/1.6f)+1)*.5f*(time<4.8f?.58f:.2f):0;
            taskButtonImage.color=Color.Lerp(new Color32(42,58,76,255),new Color32(211,147,53,255),pulse);
            if(ready&&!toastLive)
            {
                bool fresh=taskNotifications.HasNew;var batch=taskNotifications.Drain(Time.unscaledTime,false);
                if(batch!=null)
                {
                    noticeStep=batch.Count==1?batch[0]:null;toastAge=0;toastLive=true;taskNotifications.Displayed();
                    taskToastTitle.text=batch.Count>1?GameText.T(batch.Count+"项任务有更新",batch.Count+" tasks updated"):GameText.T(fresh?"新任务":"任务更新",fresh?"New task":"Task updated");
                    taskToastBody.text=noticeStep!=null?(GameText.IsEnglish?noticeStep.en:noticeStep.zh)+"\n"+GameText.T("打开任务列表查看下一步。","Open Tasks for the next step."):GameText.T("任务列表已有新内容。","There is new information in Tasks.");
                    ClickTaskSound(); // Existing UI audio respects the game's mute flag.
                }
            }
            taskToast.gameObject.SetActive(ready&&toastLive);
            if(ready&&toastLive)
            {
                toastAge+=Time.unscaledDeltaTime;
                taskToastGroup.alpha=TaskReduced?1:toastAge<.24f?toastAge/.24f:toastAge>4.24f?Mathf.Clamp01((4.48f-toastAge)/.24f):1;
                if(toastAge>4.48f)toastLive=false;
            }
        }
        void DestroyTaskNotifications(){if(taskButtonRoot!=null)Destroy(taskButtonRoot.gameObject);if(taskToast!=null)Destroy(taskToast.gameObject);}
    }
}
