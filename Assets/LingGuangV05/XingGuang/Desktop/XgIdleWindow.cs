using System;
using System.Collections;
using System.Globalization;
using LingGuangV05.CaseMenu;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.OS;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    // Presentation of the existing automation, never a second simulation or income source.
    [DefaultExecutionOrder(-100)]
    public sealed class XgIdleWindow : MonoBehaviour
    {
        public static bool IsCompact { get; private set; }
        XingGuangController owner;
        CaseMenuController menu;
        XgSim bound;
        XgUi ui;
        XgBtn entry, miniEntry, pause, review, offline, sizeToggle, more, optionsBack, optionsRestore, reviewTasks;
        RectTransform surface, monitor, options, face, wallet, entryMenu, entryHost;
        TMP_Text heading, state, metricLabel, details, explanation;
        bool mini;
        public bool Mini => mini;
        public static Vector2 WindowSize(bool small) => small ? new Vector2(360,280) : new Vector2(560,430);
        TMP_Text[] oldDigits, newDigits;
        XgJuice fx, entryFx;
        double displayed, incomeStart, lastIncome;
        readonly System.Collections.Generic.Queue<double> recentIncome=new System.Collections.Generic.Queue<double>();
        readonly System.Collections.Generic.List<Canvas> hiddenCanvases=new System.Collections.Generic.List<Canvas>();
        readonly System.Collections.Generic.List<Camera> hiddenCameras=new System.Collections.Generic.List<Camera>();
        float nextObserve, nextDigit, rollStart, nextStatus;
        bool compact;
        int previousFrameRate, previousVsync;
        string digits = "";
        readonly Color green = new Color32(59,243,138,255);
        public static XgIdleWindow Install(XingGuangController controller)
        {
            var result=controller.GetComponent<XgIdleWindow>()??controller.gameObject.AddComponent<XgIdleWindow>();
            result.owner=controller;return result;
        }
        bool Ready => owner?.Sim!=null && owner.runtime?.Sim!=null && !owner.runtime.Sim.InPrologue && (menu==null||menu.HasEnteredGame);
        bool Reduced => owner.Sim.S.reduceFx || GameSettings.ReducedMotion || GamePerformance.Low;
        string T(string zh,string en)=>GameText.T(zh,en);
        void Start(){menu=FindAnyObjectByType<CaseMenuController>();}
        void Update()
        {
            if(owner==null)return;
            SyncFeedback();
            if(Time.unscaledTime>=nextObserve)
            {
                nextObserve=Time.unscaledTime+.25f;
                if(Ready && PrologueDirector.Desk?.layer!=null && owner.Font!=null)
                {
                    if(entry==null || entry.rt==null)Build();
                    else BindEntryLayer(PrologueDirector.Desk.layer);
                }
                if(entryHost!=null)entryHost.gameObject.SetActive(Ready&&!compact);
                if(entryMenu!=null&&(!Ready||compact))entryMenu.gameObject.SetActive(false);
                if(!Ready || bound!=owner.Sim){Restore();bound=owner.Sim;}
                if(miniEntry?.rt!=null)miniEntry.label.text=T("迷你","Mini");
            }
            if(!compact)return;
            if(!Ready){Restore();return;}
            // A story transition must not become invisible behind the compact screen.
            if(XgStageCurtain.Busy || StoryFocus.Exclusive || XgEmergenceCutscene.Busy || menu!=null&&menu.IsTransitioning){Restore();return;}
            if(Time.unscaledTime>=nextStatus)
            {
            nextStatus=Time.unscaledTime+.25f;
            bool paused=owner.Sim.AutoPaused;
            double rate=XgIdleRules.IncomeRate(owner.Sim,owner.Host);
            heading.text=owner.Sim.AiDisplayName+" · "+(paused?T("已暂停","Paused"):rate>0?T("努力赚钱中","Earning money"):T("等待处理","Needs attention"));
            double recentTotal=0;foreach(double value in recentIncome)recentTotal+=value;
            state.text=paused?T("收益已暂停 · ¥0/秒","Paused · ¥0/s"):rate>0?(recentIncome.Count<10?T("本次收入 · 速率统计中","Session income · measuring rate"):T("近10秒均值 ¥","10s average ¥")+(recentTotal/10).ToString("0.00")+T("/秒","/s")):owner.Sim.ReviewFull?T("复核已满 · 点击处理","Review full · open review"):T("暂未满足自动收益条件","Automation currently unavailable");
            pause.label.text=paused?T("继续","Resume"):T("暂停","Pause");
            review.label.text=T("返回游戏","Return to game");
            metricLabel.text=T("本次挂机收入","THIS SESSION");
            int pendingReview=0;foreach(var desk in XgCatalog.Desks)pendingReview+=owner.Sim.ReviewCount(desk.id);
            details.text=T("基础自动答题","Automatic answering")+"  ·  "+T("待复核 ","Pending review ")+pendingReview;
            sizeToggle.label.text=mini?T("标准","Standard"):T("迷你","Mini");
            if(options!=null&&options.gameObject.activeSelf)offline.label.text=XgOfflineIncome.Enabled(owner)?T("离线收益：开","Offline earnings: On"):T("离线收益：关","Offline earnings: Off");
            }
            if(Time.unscaledTime>=nextDigit)
            {
                nextDigit=Time.unscaledTime+1;
                double settled=Math.Max(0,owner.Sim.S.idleAutomaticIncome-incomeStart);
                recentIncome.Enqueue(Math.Max(0,settled-lastIncome));lastIncome=settled;
                if(recentIncome.Count>10)recentIncome.Dequeue();
                displayed=Reduced?settled:XgIdleRules.CatchUp(displayed,settled);
                SetDigits(displayed);
            }
            float p=Reduced?1:Mathf.Clamp01((Time.unscaledTime-rollStart)/.72f);
            float eased=p*p*(3-2*p);
            float rollHeight=wallet.rect.height;
            for(int i=0;i<newDigits.Length;i++)
            {
                bool changed=oldDigits[i].text!=newDigits[i].text;
                oldDigits[i].rectTransform.anchoredPosition=new Vector2(0,changed?eased*rollHeight:rollHeight);
                newDigits[i].rectTransform.anchoredPosition=new Vector2(0,changed?(eased-1)*rollHeight:0);
            }
        }
        void Build()
        {
            ui=new XgUi(owner.Font,null,true);
            var taskbar=FindTaskbar(PrologueDirector.Desk.layer);if(taskbar==null)return;
            BuildEntry(taskbar);
            if(surface!=null)return;
            BuildSurface();
        }
        static RectTransform FindTaskbar(RectTransform layer)
        {
            if(layer==null)return null;
            var canvas=layer.GetComponentInParent<Canvas>();if(canvas==null)return null;
            foreach(var rect in canvas.rootCanvas.GetComponentsInChildren<RectTransform>(true))if(rect.name=="Taskbar")return rect;
            return null;
        }
        void BuildEntry(RectTransform taskbar)
        {
            if(entry?.rt!=null)return;
            entryHost=Rect("Idle feedback host",taskbar,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(32,32));
            entryFx=entryHost.gameObject.AddComponent<XgJuice>();
            XgTaskbarUtilities.Attach(taskbar,entryHost);
            SyncFeedback();
            entry=ui.Button(entryHost,"",()=>
            {
                if(!Ready)return;
                Click();
                if(!XgIdleRules.Unlocked(owner.Sim)){Enter();return;}
                entryMenu.gameObject.SetActive(!entryMenu.gameObject.activeSelf);
            },12);
            entry.rt.name="Idle window entry";entry.image.color=new Color32(25,48,53,190);
            entry.label.gameObject.SetActive(false);
            entry.rt.anchorMin=Vector2.zero;entry.rt.anchorMax=Vector2.one;entry.rt.offsetMin=entry.rt.offsetMax=Vector2.zero;
            var frame=Rect("Monitor glyph",entry.visual,Vector2.zero,Vector2.zero,new Vector2(5,10),new Vector2(27,26));RoundSurface(Panel(frame,new Color32(197,216,219,255)),3);
            Panel(Rect("Screen glyph",frame,Vector2.zero,Vector2.one,new Vector2(2,3),new Vector2(-2,-2)),new Color32(19,50,45,255)).raycastTarget=false;
            Panel(Rect("Stand glyph",entry.visual,Vector2.zero,Vector2.zero,new Vector2(14,6),new Vector2(18,11)),new Color32(197,216,219,255)).raycastTarget=false;
            RoundSurface(Panel(Rect("Foot glyph",entry.visual,Vector2.zero,Vector2.zero,new Vector2(10,4),new Vector2(22,7)),new Color32(197,216,219,255)),2);
            RoundSurface(Panel(Rect("Power glyph",entry.visual,Vector2.zero,Vector2.zero,new Vector2(22,11),new Vector2(25,14)),green),2);
            UiTip.Add(entry.rt,()=>XgIdleRules.Unlocked(owner?.Sim)?T("挂机小窗 · 标准 / 迷你","Idle window · Standard / Mini"):T("挂机小窗 · 点击解锁自动答题","Idle window · Unlock automatic answering"));
            entryMenu=Rect("Idle size menu",entry.rt,new Vector2(1,1),new Vector2(1,1),new Vector2(-190,8),new Vector2(0,104));
            RoundSurface(Panel(entryMenu,new Color32(22,35,40,255)),8);
            var standard=ui.Button(entryMenu,T("标准挂机窗口","Standard idle window"),()=>{SetMini(false);entryMenu.gameObject.SetActive(false);Enter();},13);PlaceBottomLeft(standard,8,52,174,36);
            miniEntry=ui.Button(entryMenu,T("迷你","Mini"),()=>{SetMini(true);entryMenu.gameObject.SetActive(false);Enter();},13);PlaceBottomLeft(miniEntry,8,8,174,36);
            entryMenu.gameObject.SetActive(false);
        }
        void BindEntryLayer(RectTransform layer)
        {
            // Keep the entry on the current shared desktop after scene/desktop reconstruction.
            // Do not activate or reorder the shared layer: story and menu visibility still own it.
            var taskbar=FindTaskbar(layer);
            if(taskbar!=null&&entryHost!=null&&entryHost.parent?.parent!=taskbar)XgTaskbarUtilities.Attach(taskbar,entryHost);
        }
        void BuildSurface()
        {
            var go=new GameObject("Compact earning window",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.transform.SetParent(transform,false);surface=(RectTransform)go.transform;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=WindowSize(mini);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            Panel(Rect("Backdrop",surface,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),new Color32(12,17,20,255));
            monitor=Rect("Title monitor",surface,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-260,-200),new Vector2(260,200));
            var shell=Panel(monitor,new Color32(234,224,201,255));RoundSurface(shell,12);
            var titleArt=Resources.Load<Texture2D>("LingGuangV05/Branding/IdleMonitor");
            if(titleArt!=null)
            {
                shell.color=Color.clear;
                var art=Rect("Original title artwork",monitor,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject.AddComponent<RawImage>();
                art.texture=titleArt;art.raycastTarget=false;
            }
            face=Rect("Screen",monitor,new Vector2(.066f,.20f),new Vector2(.935f,.91f),Vector2.zero,Vector2.zero);
            RoundSurface(Panel(face,new Color32(8,24,20,255)),8);
            heading=Text(face,"Heading",new Vector2(8,-22),new Vector2(-8,-3),12,new Color32(204,225,214,255));
            var native=GetComponent<XgIdleNativeWindow>()??gameObject.AddComponent<XgIdleNativeWindow>();
            heading.raycastTarget=true;heading.gameObject.AddComponent<XgIdleDragHandle>().Window=native;
            wallet=Rect("Rolling wallet",face,new Vector2(0,1),Vector2.one,new Vector2(12,-142),new Vector2(-12,-68));
            oldDigits=new TMP_Text[12];newDigits=new TMP_Text[12];
            for(int i=0;i<12;i++)
            {
                var cell=Rect("Digit "+i,wallet,new Vector2(i/12f,0),new Vector2((i+1)/12f,1),Vector2.zero,Vector2.zero);cell.gameObject.AddComponent<RectMask2D>();
                oldDigits[i]=Digit(cell,"Old");newDigits[i]=Digit(cell,"New");
            }
            state=Text(face,"Status",new Vector2(8,-79),new Vector2(-8,-61),10,new Color32(139,167,151,255));
            metricLabel=Text(face,"Session income label",new Vector2(12,-62),new Vector2(-12,-42),11,new Color32(139,167,151,255));
            details=Text(face,"Automation detail",new Vector2(12,-190),new Vector2(-12,-166),13,new Color32(139,167,151,255));
            pause=ui.Button(face,"",()=>{Click();owner.Sim.AutoPaused=!owner.Sim.AutoPaused;owner.runtime.MarkDirty();nextStatus=0;},11);PlaceBottomLeft(pause,8,5,58,22);
            review=ui.Button(face,T("返回游戏","Return to game"),()=>{Click();Restore();},11);PlaceBottomLeft(review,72,5,88,22);
            more=ui.Button(face,"···",()=>{Click();options.gameObject.SetActive(true);},15);
            sizeToggle=ui.Button(face,"",()=>{Click();SetMini(!mini);},11);
            options=Rect("Idle options",face,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            RoundSurface(Panel(options,new Color32(8,24,20,255)),8);
            explanation=Text(options,"Offline explanation",new Vector2(12,-75),new Vector2(-12,-12),13,new Color32(204,225,214,255));
            explanation.text=T("离线只结算金钱\n50% 效率 · 最多 8 小时","Money only while away\n50% efficiency · up to 8 hours");
            offline=ui.Button(options,"",()=>{Click();XgOfflineIncome.SetEnabled(owner,!XgOfflineIncome.Enabled(owner));},11);PlaceBottomLeft(offline,8,35,208,25);
            optionsBack=ui.Button(options,T("返回","Back"),()=>{Click();options.gameObject.SetActive(false);},12);
            optionsRestore=ui.Button(options,T("返回桌面","Show desktop"),()=>{Click();RestoreToDesktop();},12);
            reviewTasks=ui.Button(options,T("打开复核","Open review"),()=>{Click();Restore();foreach(var d in XgCatalog.Desks)if(owner.Sim.ReviewCount(d.id)>0){owner.Sim.S.teachingPack="";owner.Sim.SelectDesk(d.id);break;}owner.Open();owner.View?.ShowTab("teach");},12);
            options.gameObject.SetActive(false);
            if(titleArt==null)
            {
                var stand=Rect("Monitor stand",monitor,new Vector2(.45f,0),new Vector2(.55f,.18f),Vector2.zero,Vector2.zero);RoundSurface(Panel(stand,new Color32(234,224,201,255)),6);
                var foot=Rect("Monitor foot",monitor,new Vector2(.30f,0),new Vector2(.70f,.06f),Vector2.zero,Vector2.zero);RoundSurface(Panel(foot,new Color32(234,224,201,255)),8);
            }
            fx=go.AddComponent<XgJuice>();fx.Init(surface,monitor,owner.Font,owner.gameObject);
            SyncFeedback();
            ApplyLayout();
            surface.gameObject.SetActive(false);
        }
        public void SetMini(bool value)
        {
            mini=value;ApplyLayout();nextStatus=0;
            if(compact)GetComponent<XgIdleNativeWindow>()?.SetMini(value);
        }
        void ApplyLayout()
        {
            if(surface==null||face==null)return;
            surface.GetComponent<CanvasScaler>().referenceResolution=WindowSize(mini);
            Vector2 size=mini?new Vector2(332,255):new Vector2(520,400);
            monitor.sizeDelta=size;
            float width=size.x*.869f;
            heading.rectTransform.offsetMin=new Vector2(12,-32);heading.rectTransform.offsetMax=new Vector2(-106,-7);heading.fontSize=mini?12:16;
            PlaceTopRight(more,38,7,28,24);PlaceTopRight(sizeToggle,102,7,60,24);
            metricLabel.rectTransform.offsetMin=new Vector2(12,mini?-51:-64);metricLabel.rectTransform.offsetMax=new Vector2(-12,mini?-33:-42);
            wallet.offsetMin=new Vector2(12,mini?-105:-144);wallet.offsetMax=new Vector2(-12,mini?-52:-68);
            foreach(var text in oldDigits)text.fontSize=mini?29:46;
            foreach(var text in newDigits)text.fontSize=mini?29:46;
            state.rectTransform.offsetMin=new Vector2(12,mini?-130:-176);state.rectTransform.offsetMax=new Vector2(-12,mini?-107:-148);state.fontSize=mini?10:14;
            details.gameObject.SetActive(!mini);details.rectTransform.offsetMin=new Vector2(12,-209);details.rectTransform.offsetMax=new Vector2(-12,-181);
            float buttonHeight=mini?28:34;float gap=8;float half=(width-24-gap)/2;
            PlaceBottomLeft(pause,12,10,half,buttonHeight);PlaceBottomLeft(review,12+half+gap,10,half,buttonHeight);
            pause.label.fontSize=review.label.fontSize=mini?12:15;
            explanation.fontSize=mini?11:13;explanation.rectTransform.offsetMin=new Vector2(12,-60);explanation.rectTransform.offsetMax=new Vector2(-12,-8);
            PlaceBottomLeft(offline,12,mini?82:110,width-24,28);
            PlaceBottomLeft(reviewTasks,12,mini?46:72,width-24,28);
            PlaceBottomLeft(optionsBack,12,10,half,30);PlaceBottomLeft(optionsRestore,12+half+gap,10,half,30);
            sizeToggle.label.text=mini?T("标准","Standard"):T("迷你","Mini");
        }
        TMP_Text Digit(RectTransform parent,string name)
        {
            var rt=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var text=ui.Text(rt," ",25,green,TextAlignmentOptions.Center);text.raycastTarget=false;return text;
        }
        TMP_Text Text(RectTransform parent,string name,Vector2 min,Vector2 max,float size,Color color)
        {var text=ui.Text(Rect(name,parent,new Vector2(0,1),Vector2.one,min,max),"",size,color,TextAlignmentOptions.MidlineLeft);text.raycastTarget=false;text.overflowMode=TextOverflowModes.Ellipsis;return text;}
        void SetDigits(double amount)
        {
            string next=("¥"+amount.ToString("0.00",CultureInfo.InvariantCulture)).PadLeft(12);
            if(next.Length>12)next=("¥"+amount.ToString("0.00E+0",CultureInfo.InvariantCulture)).PadLeft(12);
            if(next==digits)return;
            for(int i=0;i<12;i++){oldDigits[i].text=digits.Length==12?digits[i].ToString():next[i].ToString();newDigits[i].text=next[i].ToString();}
            int visible=next.TrimStart().Length,first=12-visible;
            float cell=Mathf.Min(.8f/Mathf.Max(1,visible),.095f),left=.5f-visible*cell*.5f;
            for(int i=0;i<12;i++)
            {
                var slot=(RectTransform)newDigits[i].rectTransform.parent;
                slot.anchorMin=new Vector2(left+(i-first)*cell,0);
                slot.anchorMax=new Vector2(left+(i-first+1)*cell,1);
                slot.offsetMin=slot.offsetMax=Vector2.zero;
            }
            digits=next;rollStart=Time.unscaledTime;
        }
        void SyncFeedback()
        {
            if(owner?.Sim==null)return;
            // Both hierarchies must expose the policy before pointer-down/submit feedback runs.
            bool reduced=Reduced, muted=owner.Sim.S.mute;
            if(fx!=null){fx.Reduced=reduced;fx.Muted=muted;}
            if(entryFx!=null){entryFx.Reduced=reduced;entryFx.Muted=muted;}
        }
        void Click(){SyncFeedback();(compact?fx:entryFx)?.Play(XgJuice.Sfx.Id.Click,1,.35f);}
        public void Enter()
        {
            if(compact||!Ready||StoryFocus.Exclusive||XgStageCurtain.Busy||XgEmergenceCutscene.Busy)return;
            if(!XgIdleRules.Unlocked(owner.Sim))
            {
                owner.Open();owner.View?.ShowTab("tree");owner.View?.Tree.FocusNodeDetails(XgIdleRules.UnlockNode);return;
            }
            if(surface==null){Build();if(surface==null)return;}
            compact=true;IsCompact=true;previousFrameRate=Application.targetFrameRate;previousVsync=QualitySettings.vSyncCount;QualitySettings.vSyncCount=0;Application.targetFrameRate=30;
            bound=owner.Sim;incomeStart=owner.Sim.S.idleAutomaticIncome;displayed=lastIncome=0;recentIncome.Clear();digits="";SetDigits(displayed);nextDigit=Time.unscaledTime+1;
            surface.gameObject.SetActive(true);options.gameObject.SetActive(false);entryHost.gameObject.SetActive(false);entryMenu.gameObject.SetActive(false);Click();
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.enabled&&canvas.transform!=surface&&!canvas.transform.IsChildOf(surface)){hiddenCanvases.Add(canvas);canvas.enabled=false;}
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(camera.enabled){hiddenCameras.Add(camera);camera.enabled=false;}
            GetComponent<XgIdleNativeWindow>()?.SetMini(mini);GetComponent<XgIdleNativeWindow>()?.Enter();
            var native=GetComponent<XgIdleNativeWindow>();if(native==null){native=gameObject.AddComponent<XgIdleNativeWindow>();native.Enter();}
        }
        public void Restore(){if(!compact)return;compact=false;IsCompact=false;Application.targetFrameRate=previousFrameRate;QualitySettings.vSyncCount=previousVsync;foreach(var canvas in hiddenCanvases)if(canvas!=null)canvas.enabled=true;hiddenCanvases.Clear();foreach(var camera in hiddenCameras)if(camera!=null)camera.enabled=true;hiddenCameras.Clear();if(surface!=null)surface.gameObject.SetActive(false);GetComponent<XgIdleNativeWindow>()?.Restore();}
        public void RestoreToDesktop()
        {
            Restore();
            // Returning to the game restores the untouched window context. Only this explicit action minimizes apps.
            foreach(var window in FindObjectsByType<WindowManager>(FindObjectsSortMode.None))
            {
                var group=window.GetComponent<CanvasGroup>();
                if(window.isOn&&window.gameObject.activeInHierarchy&&(group==null||group.alpha>.5f))window.MinimizeWindow();
            }
        }
        void OnDestroy(){Restore();if(surface!=null)Destroy(surface.gameObject);if(entryHost!=null)Destroy(entryHost.gameObject);}
    }
}
