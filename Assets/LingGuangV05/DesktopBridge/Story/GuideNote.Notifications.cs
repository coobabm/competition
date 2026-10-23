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
    /// <summary>
    /// The 任务 queue on the desktop (2026-10-21, replaces the sticky to-do note's list): a 「任务」 button that pulses three
    /// strong 1.6 s cycles for news, then breathes faintly with a dot until opened (static under reduced motion; closing the
    /// notice stops the pulse, the dot stays), a silent 300×96 corner notice that merges what happened within 2 s and
    /// stays 4 s, and a panel with at most five tasks (progress, 「去看看」), the waiting ones and the guide's current
    /// step on top. The queue itself lives in the save (XgTasks.cs); this file only shows it. The guide still runs
    /// behind (TopStep, the lab's 「去做 ›」) but its yellow note is no longer shown.
    /// </summary>
    public sealed partial class GuideNote
    {
        const float TaskPanelWidth = 380, TaskRowHeight = 52, TaskMerge = 2f, TaskToastStay = 4f, TaskToastFadeIn = .24f, TaskBlinkCycle = 1.6f;
        const int TaskBlinkStrongCycles = 3;
        const float TaskBlinkStrong = .85f, TaskBlinkBreath = .22f;
        const float TaskClearAfter = 3f, TaskTickEvery = .5f;
        static readonly Color PanelBg = new Color32(22, 28, 36, 250), PanelRow = new Color32(29, 37, 48, 255), PanelLine = new Color32(42, 52, 64, 255);
        static readonly Color PanelInk = new Color32(232, 236, 239, 255), PanelSoft = new Color32(185, 194, 201, 255), PanelDim = new Color32(152, 162, 171, 255);
        static readonly Color GoFill = new Color32(23, 50, 38, 255), GoInk = new Color32(191, 240, 214, 255), DoneInk = new Color32(63, 207, 142, 255);
        static readonly Color BarBack = new Color32(40, 49, 60, 255), BarFill = new Color32(74, 143, 209, 255), Warm = new Color32(255, 191, 60, 255);

        XgSim taskOwner;
        RectTransform taskButtonRoot, taskToast, taskPanel;
        Image taskButtonImage, taskUnreadDot;
        TMP_Text taskButtonText, taskToastTitle, taskToastBody, taskEmpty, taskViewCaption;
        TMP_Text panelTitle, panelSub, panelNowLabel, panelWaiting, panelEmpty;
        RectTransform panelNow;
        CanvasGroup taskToastGroup;
        bool taskListOpen, toastLive, taskUnread;
        float toastAge, blinkSince = -100, nextTaskTick, firstPendingAt = -1, finishedSince = -1;
        CaseMenuController taskMenu;
        XgGuideStep noticeStep;
        readonly List<XgTaskEvent> pendingEvents = new List<XgTaskEvent>();
        readonly TaskRow[] taskRows = new TaskRow[XgSim.TaskCapacity];

        sealed class TaskRow
        {
            public RectTransform rt, bar, fill;
            public TMP_Text title, body, progress, goLabel;
            public Button go;
            public CanvasGroup group;
            public XgTaskDef task;
        }

        bool TaskReduced=>GameSettings.ReducedMotion || lab!=null&&lab.Sim!=null&&lab.Sim.S.reduceFx;
        bool TaskPresentationAllowed()=>!XgIdleWindow.IsCompact&&!StoryFocus.Exclusive&&!CutscenePlaying&&!XgStageCurtain.Busy&&!XgEmergenceCutscene.Busy
            &&(taskMenu==null||taskMenu.HasEnteredGame&&!taskMenu.IsTransitioning&&!taskMenu.IsSettingsOpen);
        ChapterOneSim House0 => runtime != null ? runtime.Sim : null;

        /// <summary>Opens the 任务 panel: ticked tasks leave, waiting ones come in, the button stops blinking.</summary>
        public void OpenTaskList()
        {
            if(!TaskPresentationAllowed()||taskPanel==null||lab==null||lab.Sim==null)return;
            taskListOpen=true;taskUnread=false;blinkSince=-100;
            lab.Sim.ClearFinishedTasks(); // arrivals while the panel is open need no notice
            finishedSince=-1;
            taskPanel.gameObject.SetActive(true);RefreshTaskPanel();
            ClickTaskSound();
        }

        void CloseTaskList(){taskListOpen=false;if(taskPanel!=null)taskPanel.gameObject.SetActive(false);ClickTaskSound();}

        void ClickTaskSound(){if(lab!=null&&lab.Sim!=null&&!lab.Sim.S.mute)lab.GetComponentInChildren<XgJuice>()?.Play(XgJuice.Sfx.Id.Click,1,.4f);}

        /// <summary>The guide's steps for the (hidden) note. The task queue is fed separately in <see cref="TickTaskNotifications"/>.</summary>
        List<XgGuideStep> ObserveTaskNotifications(List<XgGuideStep> steps)
        {
            if(lab==null||lab.Sim==null)return new List<XgGuideStep>();
            BindTaskOwner();
            var visible=new List<XgGuideStep>();
            if(!runtime.Sim.InPrologue&&lab.Sim.TeachingTreeOpen)
                foreach(var step in steps)if(step!=null&&step.id!="open.exe"&&step.id!="setup.answer"&&step.id!="lesson.judge")visible.Add(step);
            return visible;
        }

        void BindTaskOwner()
        {
            if(lab==null||lab.Sim==null)return;
            if(!ReferenceEquals(taskOwner,lab.Sim))
            {
                taskOwner=lab.Sim;pendingEvents.Clear();firstPendingAt=-1;toastLive=false;taskListOpen=false;taskUnread=false;noticeStep=null;
                nextTaskTick=0;finishedSince=-1;blinkSince=-100;toastAge=0;
                if(taskPanel!=null)taskPanel.gameObject.SetActive(false);
                if(taskToast!=null)taskToast.gameObject.SetActive(false);
                if(busy){StopAllCoroutines();busy=false;pending=null;}
                foreach(var row in rows)if(row!=null)row.group.alpha=1;
            }
        }

        void BuildTaskNotifications(PrologueDesk d)
        {
            taskMenu=FindAnyObjectByType<CaseMenuController>();
            // The player opens task hints from the original top-right task-list position.
            taskButtonRoot=PrologueDesk.Rect("Tasks entry",d.layer,Vector2.one,Vector2.one,new Vector2(-128,-50),new Vector2(-16,-16));
            taskButtonImage=PrologueDesk.Fill(taskButtonRoot,new Color32(42,58,76,255),true);
            var button=taskButtonRoot.gameObject.AddComponent<Button>();button.targetGraphic=taskButtonImage;
            button.onClick.AddListener(()=>{if(!TaskPresentationAllowed())return;if(taskListOpen)CloseTaskList();else OpenTaskList();});
            taskButtonText=d.Text(PrologueDesk.Rect("Label",taskButtonRoot,Vector2.zero,Vector2.one,new Vector2(8,0),new Vector2(-16,0)),"",15,Color.white,TextAlignmentOptions.Center);
            taskUnreadDot=PrologueDesk.Fill(PrologueDesk.Rect("Unread",taskButtonRoot,Vector2.one,Vector2.one,new Vector2(-12,-12),new Vector2(-4,-4)),new Color32(255,107,94,255),false);
            taskUnreadDot.sprite=PrologueDesk.Circle();
            // Kept for GuideNote.Show (the hidden note's empty line).
            taskEmpty=d.Text(PrologueDesk.Rect("Empty task list",body,new Vector2(0,1),Vector2.one,new Vector2(10,-65),new Vector2(-10,-30)),"",16,Soft,TextAlignmentOptions.Center);
            taskEmpty.gameObject.SetActive(false);

            // Corner notice: 300×96, right bottom above the taskbar.
            taskToast=PrologueDesk.Rect("Task notification",d.layer,new Vector2(1,0),new Vector2(1,0),new Vector2(-316,48),new Vector2(-16,144));
            PrologueDesk.Fill(taskToast,new Color32(26,33,43,250),true);
            taskToastGroup=taskToast.gameObject.AddComponent<CanvasGroup>();
            taskToastTitle=d.Text(PrologueDesk.Rect("Title",taskToast,Vector2.zero,Vector2.one,new Vector2(14,50),new Vector2(-36,-10)),"",16,new Color32(255,210,125,255),TextAlignmentOptions.TopLeft);
            taskToastBody=d.Text(PrologueDesk.Rect("Body",taskToast,Vector2.zero,Vector2.one,new Vector2(14,8),new Vector2(-96,-34)),"",14,PanelSoft,TextAlignmentOptions.TopLeft);
            taskToastBody.overflowMode=TextOverflowModes.Ellipsis;
            var view=PrologueDesk.Rect("View task",taskToast,new Vector2(1,0),new Vector2(1,0),new Vector2(-86,10),new Vector2(-12,36));
            var viewImage=PrologueDesk.Fill(view,GoFill,true);var viewButton=view.gameObject.AddComponent<Button>();viewButton.targetGraphic=viewImage;
            taskViewCaption=d.Text(PrologueDesk.Rect("Label",view,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),GameText.T("查看","View"),14,GoInk,TextAlignmentOptions.Center);
            viewButton.onClick.AddListener(()=>{if(!TaskPresentationAllowed())return;toastLive=false;OpenTaskList();});
            var close=PrologueDesk.Rect("Dismiss task notice",taskToast,Vector2.one,Vector2.one,new Vector2(-30,-28),new Vector2(-6,-4));
            var closeImage=PrologueDesk.Fill(close,Color.clear,true);var closeButton=close.gameObject.AddComponent<Button>();closeButton.targetGraphic=closeImage;
            d.Text(PrologueDesk.Rect("Label",close,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"×",18,PanelSoft,TextAlignmentOptions.Center);
            // Closing the notice stops the pulse; the unread dot stays until the panel is opened.
            closeButton.onClick.AddListener(()=>{if(TaskPresentationAllowed()){toastLive=false;blinkSince=-100;ClickTaskSound();}});
            taskToast.gameObject.SetActive(false);

            BuildTaskPanel(d);
        }

        void BuildTaskPanel(PrologueDesk d)
        {
            // Below the top-right task button; grows downwards with its rows.
            taskPanel=PrologueDesk.Rect("Task panel",d.layer,Vector2.one,Vector2.one,new Vector2(-16-TaskPanelWidth,-258),new Vector2(-16,-58));
            taskPanel.pivot=Vector2.one;taskPanel.anchoredPosition=new Vector2(-16,-58);
            PrologueDesk.Fill(taskPanel,PanelBg,true);
            panelTitle=d.Text(PrologueDesk.Rect("Title",taskPanel,new Vector2(0,1),Vector2.one,new Vector2(16,-34),new Vector2(-40,-10)),"",17,PanelInk,TextAlignmentOptions.MidlineLeft);
            panelTitle.fontStyle=FontStyles.Bold;panelTitle.textWrappingMode=TextWrappingModes.NoWrap;
            var x=PrologueDesk.Rect("Close",taskPanel,Vector2.one,Vector2.one,new Vector2(-34,-34),new Vector2(-8,-8));
            var xImage=PrologueDesk.Fill(x,Color.clear,true);var xButton=x.gameObject.AddComponent<Button>();xButton.targetGraphic=xImage;
            d.Text(PrologueDesk.Rect("Label",x,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"×",18,PanelSoft,TextAlignmentOptions.Center);
            xButton.onClick.AddListener(CloseTaskList);
            panelSub=d.Text(PrologueDesk.Rect("Sub",taskPanel,new Vector2(0,1),Vector2.one,new Vector2(16,-54),new Vector2(-16,-34)),"",12,PanelDim,TextAlignmentOptions.MidlineLeft);
            panelSub.gameObject.SetActive(false);

            // 「眼下最该做」: the guide's current step, one line, its own 去看看.
            panelNow=PrologueDesk.Rect("Now",taskPanel,new Vector2(0,1),Vector2.one,new Vector2(12,-92),new Vector2(-12,-40));
            PrologueDesk.Fill(panelNow,new Color32(36,46,30,255),false);
            panelNowLabel=d.Text(PrologueDesk.Rect("Label",panelNow,Vector2.zero,Vector2.one,new Vector2(10,0),new Vector2(-84,0)),"",13,new Color32(240,226,160,255),TextAlignmentOptions.MidlineLeft);
            panelNowLabel.textWrappingMode=TextWrappingModes.Normal;panelNowLabel.overflowMode=TextOverflowModes.Truncate;panelNowLabel.enableAutoSizing=true;panelNowLabel.fontSizeMin=10;panelNowLabel.fontSizeMax=13;
            GoButton(d,panelNow,()=>{var s=TopStep;if(s!=null&&Follow(s))CloseTaskList();});

            for(int i=0;i<taskRows.Length;i++)
            {
                var r=new TaskRow();
                r.rt=PrologueDesk.Rect("Task "+i,taskPanel,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);
                PrologueDesk.Fill(r.rt,PanelRow,false);
                r.group=r.rt.gameObject.AddComponent<CanvasGroup>();
                r.title=d.Text(PrologueDesk.Rect("Title",r.rt,new Vector2(0,1),Vector2.one,new Vector2(12,-28),new Vector2(-92,-6)),"",15,PanelInk,TextAlignmentOptions.MidlineLeft);
                r.title.fontStyle=FontStyles.Bold;r.title.textWrappingMode=TextWrappingModes.NoWrap;r.title.overflowMode=TextOverflowModes.Ellipsis;
                r.body=d.Text(PrologueDesk.Rect("Body",r.rt,new Vector2(0,1),Vector2.one,new Vector2(12,-48),new Vector2(-92,-28)),"",13,PanelSoft,TextAlignmentOptions.MidlineLeft);
                r.body.textWrappingMode=TextWrappingModes.NoWrap;r.body.overflowMode=TextOverflowModes.Ellipsis;
                r.bar=PrologueDesk.Rect("Progress",r.rt,new Vector2(0,0),new Vector2(1,0),new Vector2(12,10),new Vector2(-140,14));
                PrologueDesk.Fill(r.bar,BarBack,false);
                r.fill=PrologueDesk.Rect("Fill",r.bar,Vector2.zero,new Vector2(0,1),Vector2.zero,Vector2.zero);
                PrologueDesk.Fill(r.fill,BarFill,false);
                r.progress=d.Text(PrologueDesk.Rect("Count",r.rt,new Vector2(1,0),new Vector2(1,0),new Vector2(-132,2),new Vector2(-12,22)),"",12,PanelDim,TextAlignmentOptions.MidlineRight);
                r.progress.textWrappingMode=TextWrappingModes.NoWrap;
                int index=i;
                r.go=GoButton(d,r.rt,()=>{var t=taskRows[index].task;var step=t==null?null:t.Jump();if(step!=null&&Follow(step))CloseTaskList();},true);
                r.goLabel=r.go.GetComponentInChildren<TMP_Text>();
                taskRows[i]=r;
            }
            panelWaiting=d.Text(PrologueDesk.Rect("Waiting",taskPanel,new Vector2(0,0),new Vector2(1,0),new Vector2(16,8),new Vector2(-16,28)),"",12,PanelDim,TextAlignmentOptions.MidlineLeft);
            panelWaiting.textWrappingMode=TextWrappingModes.NoWrap;panelWaiting.overflowMode=TextOverflowModes.Ellipsis;
            panelEmpty=d.Text(PrologueDesk.Rect("Empty",taskPanel,new Vector2(0,1),Vector2.one,new Vector2(16,-150),new Vector2(-16,-96)),"",15,PanelSoft,TextAlignmentOptions.Center);
            taskPanel.gameObject.SetActive(false);
        }

        Button GoButton(PrologueDesk d,RectTransform parent,System.Action onClick,bool row=false)
        {
            var rt=row?PrologueDesk.Rect("Go",parent,new Vector2(1,1),new Vector2(1,1),new Vector2(-84,-34),new Vector2(-12,-8))
                      :PrologueDesk.Rect("Go",parent,new Vector2(1,0),new Vector2(1,1),new Vector2(-76,4),new Vector2(-6,-4));
            var img=PrologueDesk.Fill(rt,GoFill,true);var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=img;
            d.Text(PrologueDesk.Rect("Label",rt,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),GameText.T("去看看","Go"),13,GoInk,TextAlignmentOptions.Center);
            b.onClick.AddListener(()=>{if(TaskPresentationAllowed())onClick();});
            return b;
        }

        void RefreshTaskPanel()
        {
            if(taskPanel==null||lab==null||lab.Sim==null)return;
            var sim=lab.Sim;var c=new XgTaskContext(sim,House0);bool en=GameText.IsEnglish;
            var shown=sim.TasksShown;int waiting=sim.TasksWaiting;
            panelTitle.text=GameText.T("任务","Tasks")+"  <size=13><color=#98a2ab>"+shown.Count+"/"+XgSim.TaskCapacity+(waiting>0?GameText.T(" · +"+waiting+" 排队中"," · +"+waiting+" waiting"):"")+"</color></size>";
            panelSub.text="";
            var now=TopStep;bool hasNow=now!=null&&runtime!=null&&runtime.Sim!=null&&!runtime.Sim.InPrologue;
            panelNow.gameObject.SetActive(hasNow);
            if(hasNow)panelNowLabel.text=GameText.T("眼下最该做：","Right now: ")+now.Text(en);
            float y=hasNow?100:42;
            for(int i=0;i<taskRows.Length;i++)
            {
                var r=taskRows[i];var t=i<shown.Count?XgTaskBook.Find(shown[i]):null;r.task=t;
                r.rt.gameObject.SetActive(t!=null);
                if(t==null)continue;
                bool done=sim.TaskDone(t.id);
                r.title.text=(done?"<color=#3fcf8e>✓</color> ":"")+t.Title(en);
                r.body.text="";r.body.gameObject.SetActive(false);
                r.group.alpha=done?.6f:1;
                bool bar=t.HasProgress;
                r.bar.gameObject.SetActive(bar);r.progress.gameObject.SetActive(bar);
                if(bar){r.fill.anchorMax=new Vector2(done?1:(float)t.Fraction(c),1);r.progress.text=done?"":t.ProgressText(c);}
                r.go.gameObject.SetActive(!done&&t.Jump()!=null);
                float h=bar?TaskRowHeight:40;
                r.rt.offsetMin=new Vector2(12,-y-h);r.rt.offsetMax=new Vector2(-12,-y);
                y+=h+8;
            }
            panelEmpty.gameObject.SetActive(shown.Count==0);
            if(shown.Count==0){panelEmpty.text=GameText.T("暂无任务","No tasks");y+=70;}
            var names=new List<string>();foreach(var id in sim.S.taskWaiting){var w=XgTaskBook.Find(id);if(w!=null)names.Add(w.Title(en));}
            panelWaiting.gameObject.SetActive(names.Count>0);
            if(names.Count>0){panelWaiting.text=GameText.T("排队中：","Waiting: ")+string.Join(GameText.T("、",", "),names);y+=24;}
            taskPanel.sizeDelta=new Vector2(TaskPanelWidth,y+10);
        }

        void TickTaskNotifications(bool ready)
        {
            if(taskButtonRoot==null)return;
            BindTaskOwner();
            bool show=ready&&lab!=null&&lab.Sim!=null&&lab.Sim.TeachingTreeOpen;
            taskButtonRoot.gameObject.SetActive(ready);
            taskButtonText.text=GameText.T("任务提示","Tasks");taskViewCaption.text=GameText.T("查看","View");
            float now=Time.unscaledTime;
            // Feed the queue (the save) twice a second; collect what happened for one merged notice.
            if(lab!=null&&lab.Sim!=null&&runtime!=null&&runtime.Sim!=null&&!runtime.Sim.InPrologue&&now>=nextTaskTick)
            {
                nextTaskTick=now+TaskTickEvery;
                var events=lab.Sim.TickTasks(House0);
                if(events.Count>0&&taskListOpen){RefreshTaskPanel();events.RemoveAll(e=>e.kind!=XgTaskEventKind.Completed);}
                if(events.Count>0){if(pendingEvents.Count==0)firstPendingAt=now;pendingEvents.AddRange(events);}
                // A ticked task leaves after a few seconds when the panel is closed (opening the panel clears it at once).
                bool anyFinished=false;foreach(var id in lab.Sim.TasksShown)if(lab.Sim.TaskDone(id)){anyFinished=true;break;}
                if(anyFinished&&!taskListOpen){if(finishedSince<0)finishedSince=now;else if(now-finishedSince>=TaskClearAfter){var more=lab.Sim.ClearFinishedTasks();finishedSince=-1;if(more.Count>0){if(pendingEvents.Count==0)firstPendingAt=now;pendingEvents.AddRange(more);}}}
                else if(!anyFinished)finishedSince=-1;
                if(taskListOpen)RefreshTaskPanel();
            }
            // Pulse: a 1.6 s cosine cycle, three strong cycles, then a faint breath until the panel is opened (or the
            // notice is closed). Static under reduced motion; the unread dot carries the news either way.
            float b=now-blinkSince,pulse=0;
            if(!TaskReduced&&blinkSince>-50&&taskUnread)
                pulse=(1-Mathf.Cos(2*Mathf.PI*b/TaskBlinkCycle))*.5f*(b<TaskBlinkCycle*TaskBlinkStrongCycles?TaskBlinkStrong:TaskBlinkBreath);
            taskButtonImage.color=Color.Lerp(new Color32(42,58,76,255),Warm,pulse);
            taskUnreadDot.gameObject.SetActive(taskUnread);
            if(show&&!toastLive&&pendingEvents.Count>0&&now-firstPendingAt>=TaskMerge)ShowTaskNotice();
            taskToast.gameObject.SetActive(show&&toastLive);
            if(show&&toastLive)
            {
                toastAge+=Time.unscaledDeltaTime;
                taskToastGroup.alpha=TaskReduced?1:toastAge<TaskToastFadeIn?toastAge/TaskToastFadeIn:toastAge>TaskToastStay?Mathf.Clamp01((TaskToastStay+.36f-toastAge)/.36f):1;
                if(toastAge>TaskToastStay+.36f)toastLive=false;
            }
            if(!show&&taskListOpen){taskListOpen=false;if(taskPanel!=null)taskPanel.gameObject.SetActive(false);}
        }

        void ShowTaskNotice()
        {
            bool en=GameText.IsEnglish;int fresh=0,queued=0,done=0;XgTaskDef one=null;
            foreach(var e in pendingEvents){if(e.task==null)continue;one=e.task;if(e.kind==XgTaskEventKind.Arrived)fresh++;else if(e.kind==XgTaskEventKind.Queued)queued++;else done++;}
            int total=fresh+queued+done;pendingEvents.Clear();firstPendingAt=-1;
            if(total==0)return;
            if(total>1)
            {
                taskToastTitle.text=GameText.T(total+"项任务有更新",total+" task updates");
                var parts=new List<string>();
                if(fresh>0)parts.Add(GameText.T("新任务 "+fresh,fresh+" new"));if(done>0)parts.Add(GameText.T("完成 "+done,done+" done"));if(queued>0)parts.Add(GameText.T("排队 "+queued,queued+" waiting"));
                taskToastBody.text=string.Join(" · ",parts);
            }
            else if(done>0){taskToastTitle.text=GameText.T("任务完成","Task done");taskToastBody.text=one.Title(en);}
            else if(queued>0){taskToastTitle.text=GameText.T("任务更新","Task update");taskToastBody.text=one.Title(en)+GameText.T(" · 已排队：任务已满 5/5，完成一个后补上"," · waiting: list full, it joins when one is done");}
            else{taskToastTitle.text=GameText.T("新任务","New task");taskToastBody.text=one.Title(en)+GameText.T("：","：")+one.Body(en);}
            noticeStep=null;toastAge=0;toastLive=true;
            if(fresh+queued>0&&!taskListOpen){taskUnread=true;blinkSince=Time.unscaledTime;}
            // Automatic notices are silent: there is no notification-sound option, and the click sound is for the player's own clicks.
        }

        void DestroyTaskNotifications(){if(taskButtonRoot!=null)Destroy(taskButtonRoot.gameObject);if(taskToast!=null)Destroy(taskToast.gameObject);if(taskPanel!=null)Destroy(taskPanel.gameObject);}
    }
}
