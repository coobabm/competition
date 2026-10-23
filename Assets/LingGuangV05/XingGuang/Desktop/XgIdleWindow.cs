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
        // Notification-sized title monitor (design v3, second visual revision): one size, no Standard/Mini switch.
        public static readonly Vector2 WindowSize = new Vector2(256,148);
        XingGuangController owner;
        CaseMenuController menu;
        XgSim bound;
        XgUi ui;
        XgBtn entry, pause, review, restore, more, pin, quit;
        RectTransform surface, monitor, options, face, wallet, entryHost;
        TMP_Text heading, rate;
        Image dot;
        bool topmost;
        float quitArmed=-10;
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
        readonly Color green = new Color32(59,243,138,255), stoppedInk = new Color32(146,182,164,255);
        readonly Color runningDot = new Color32(137,216,184,255), pausedDot = new Color32(140,155,171,255), amberDot = new Color32(230,199,136,255);
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
                if(!Ready || bound!=owner.Sim){Restore();bound=owner.Sim;}
            }
            if(!compact)return;
            if(!Ready){Restore();return;}
            // A story transition must not become invisible behind the compact screen.
            if(XgStageCurtain.Busy || StoryFocus.Exclusive || XgEmergenceCutscene.Busy || menu!=null&&menu.IsTransitioning){Restore();return;}
            if(Time.unscaledTime>=nextStatus)
            {
            nextStatus=Time.unscaledTime+.25f;
            var sim=owner.Sim;
            bool paused=sim.AutoPaused, full=sim.ReviewFull;
            // Income that still flows beside a full inbox stays "earning"; review only shows as the block when it stops income.
            bool earning=!paused&&XgIdleRules.IncomeRate(sim,owner.Host)>0;
            heading.text=sim.AiDisplayName+" · "+(earning?T("努力赚钱中","Earning money"):paused?T("已暂停","Paused"):BlockReason(sim,full));
            dot.color=earning?runningDot:paused?pausedDot:amberDot;
            double recentTotal=0;foreach(double value in recentIncome)recentTotal+=value;
            string perSecond=T("/秒","/s");
            rate.text=T("近10秒平均 ","10s average ")+(!earning?"<color=#52DC94>¥0.00"+perSecond+"</color>":recentIncome.Count<10?T("统计中","measuring"):"<color=#52DC94>¥"+(recentTotal/10).ToString("0.00",CultureInfo.InvariantCulture)+perSecond+"</color>");
            Color ink=earning?green:stoppedInk;
            for(int i=0;i<newDigits.Length;i++)oldDigits[i].color=newDigits[i].color=ink;
            pause.label.text=paused?T("继续挂机","Resume"):T("暂停挂机","Pause");
            int pendingReview=0;if(full)foreach(var desk in XgCatalog.Desks)pendingReview+=sim.ReviewCount(desk.id);
            review.label.text=full?T("处理复核 ","Review ")+pendingReview:T("返回游戏","Return to game");
            if(options.gameObject.activeSelf)
            {
                pin.label.text=topmost?T("置顶：开","Always on top: On"):T("置顶：关","Always on top: Off");
                quit.label.text=Time.unscaledTime-quitArmed<3?T("再点一次退出","Click again to quit"):T("退出游戏","Quit game");
            }
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
        // A short, specific reason in place of a generic "needs attention".
        string BlockReason(XgSim sim,bool reviewFull)
        {
            var global=sim.AutoGlobalState(owner.Host);
            if(reviewFull&&(global==XgAutoDeskState.InboxFull||global==XgAutoDeskState.Running))return T("等待复核","Waiting for review");
            switch(global)
            {
                case XgAutoDeskState.Held: return sim.QualityFrozen?T("账号冻结中","Account frozen"):sim.CaptchaPending?T("等待验证码","Captcha needed"):T("验证暂停中","Captcha pause");
                case XgAutoDeskState.Project: return T("研发占用显卡","Research has the GPU");
                case XgAutoDeskState.NoPower: string blocker=owner.Host?.Blocker;return !string.IsNullOrEmpty(blocker)&&blocker.Length<=8?blocker:T("显卡没在算","GPU not running");
                case XgAutoDeskState.Off: return T("自动答题未开","Auto-answer off");
                default: return T("没有可自动的题桌","No eligible desk");
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
            // One window size: the tray glyph enters directly (locked: Enter focuses the technology node).
            entry=ui.Button(entryHost,"",()=>{if(!Ready)return;Click();Enter();},12);
            entry.rt.name="Idle window entry";entry.image.color=new Color32(25,48,53,190);
            entry.label.gameObject.SetActive(false);
            entry.rt.anchorMin=Vector2.zero;entry.rt.anchorMax=Vector2.one;entry.rt.offsetMin=entry.rt.offsetMax=Vector2.zero;
            var frame=Rect("Monitor glyph",entry.visual,Vector2.zero,Vector2.zero,new Vector2(5,10),new Vector2(27,26));RoundSurface(Panel(frame,new Color32(197,216,219,255)),3);
            Panel(Rect("Screen glyph",frame,Vector2.zero,Vector2.one,new Vector2(2,3),new Vector2(-2,-2)),new Color32(19,50,45,255)).raycastTarget=false;
            Panel(Rect("Stand glyph",entry.visual,Vector2.zero,Vector2.zero,new Vector2(14,6),new Vector2(18,11)),new Color32(197,216,219,255)).raycastTarget=false;
            RoundSurface(Panel(Rect("Foot glyph",entry.visual,Vector2.zero,Vector2.zero,new Vector2(10,4),new Vector2(22,7)),new Color32(197,216,219,255)),2);
            RoundSurface(Panel(Rect("Power glyph",entry.visual,Vector2.zero,Vector2.zero,new Vector2(22,11),new Vector2(25,14)),green),2);
            UiTip.Add(entry.rt,()=>XgIdleRules.Unlocked(owner?.Sim)?T("挂机小窗","Idle window"):T("挂机小窗 · 点击解锁自动答题","Idle window · Unlock automatic answering"));
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
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=WindowSize;scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            Panel(Rect("Backdrop",surface,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),new Color32(12,17,20,255));
            // Exactly the native window in a player; centred and whole inside a larger editor game view.
            monitor=Rect("Title monitor",surface,new Vector2(.5f,.5f),new Vector2(.5f,.5f),-WindowSize/2,WindowSize/2);
            var shell=Panel(monitor,new Color32(234,224,201,255));RoundSurface(shell,12);
            var titleArt=Resources.Load<Texture2D>("LingGuangV05/Branding/IdleMonitor");
            if(titleArt!=null)
            {
                shell.color=Color.clear;
                var art=Rect("Original title artwork",monitor,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject.AddComponent<RawImage>();
                art.texture=titleArt;art.raycastTarget=false;
            }
            // Screen opening of the prototype (16px sides, 12px top), ending just above the art's orange power light.
            face=Rect("Screen",monitor,new Vector2(.0625f,.262f),new Vector2(.9375f,.919f),Vector2.zero,Vector2.zero);
            RoundSurface(Panel(face,new Color32(3,22,21,255)),5);
            var body=Panel(Rect("Screen body",face,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(0,-21)),new Color32(12,42,35,255));RoundSurface(body,5);body.raycastTarget=false;
            var bar=Rect("Top bar",face,new Vector2(0,1),Vector2.one,new Vector2(0,-21),Vector2.zero);
            var native=GetComponent<XgIdleNativeWindow>()??gameObject.AddComponent<XgIdleNativeWindow>();
            Panel(bar,Color.clear).gameObject.AddComponent<XgIdleDragHandle>().Window=native;
            dot=Panel(Rect("Status dot",bar,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(6,-2.5f),new Vector2(11,2.5f)),runningDot);RoundSurface(dot,3);dot.raycastTarget=false;
            heading=Text(bar,"Heading",new Vector2(15,-21),new Vector2(-46,0),9,new Color32(172,201,191,255));
            restore=TopButton(bar,"↗",()=>{Click();Restore();},44,20);
            UiTip.Add(restore.rt,()=>T("返回游戏","Return to game"));
            more=TopButton(bar,"···",()=>{Click();quitArmed=-10;options.gameObject.SetActive(!options.gameObject.activeSelf);nextStatus=0;},23,22);
            wallet=Rect("Rolling wallet",face,new Vector2(0,1),Vector2.one,new Vector2(9,-54),new Vector2(-9,-23));
            oldDigits=new TMP_Text[12];newDigits=new TMP_Text[12];
            for(int i=0;i<12;i++)
            {
                var cell=Rect("Digit "+i,wallet,new Vector2(i/12f,0),new Vector2((i+1)/12f,1),Vector2.zero,Vector2.zero);cell.gameObject.AddComponent<RectMask2D>();
                oldDigits[i]=Digit(cell,"Old");newDigits[i]=Digit(cell,"New");
            }
            rate=Text(face,"Recent rate",new Vector2(9,-68),new Vector2(-9,-54),9,new Color32(156,170,189,255));
            pause=ActionButton(()=>{Click();owner.Sim.AutoPaused=!owner.Sim.AutoPaused;owner.runtime.MarkDirty();nextStatus=0;});
            pause.rt.anchorMin=Vector2.zero;pause.rt.anchorMax=new Vector2(.5f,0);pause.rt.offsetMin=new Vector2(9,5);pause.rt.offsetMax=new Vector2(-3,25);
            review=ActionButton(()=>{Click();if(owner.Sim.ReviewFull)OpenReview();else Restore();});
            review.rt.anchorMin=new Vector2(.5f,0);review.rt.anchorMax=new Vector2(1,0);review.rt.offsetMin=new Vector2(3,5);review.rt.offsetMax=new Vector2(-9,25);
            // The more menu holds only always-on-top and quit; offline earnings belong to settings.
            options=Rect("Idle options",face,Vector2.one,Vector2.one,new Vector2(-112,-67),new Vector2(-2,-21));
            RoundSurface(Panel(options,new Color32(20,44,36,255)),5);
            pin=ui.Button(options,"",()=>{Click();topmost=!topmost;native.SetTopmost(topmost);nextStatus=0;},9);PlaceTopLeft(pin,3,3,104,19);
            quit=ui.Button(options,"",()=>{Click();if(Time.unscaledTime-quitArmed<3)QuitGame();else{quitArmed=Time.unscaledTime;nextStatus=0;}},9);PlaceTopLeft(quit,3,24,104,19);
            options.gameObject.SetActive(false);
            if(titleArt==null)
            {
                var stand=Rect("Monitor stand",monitor,new Vector2(.45f,0),new Vector2(.55f,.18f),Vector2.zero,Vector2.zero);RoundSurface(Panel(stand,new Color32(234,224,201,255)),6);
                var foot=Rect("Monitor foot",monitor,new Vector2(.30f,0),new Vector2(.70f,.06f),Vector2.zero,Vector2.zero);RoundSurface(Panel(foot,new Color32(234,224,201,255)),8);
            }
            fx=go.AddComponent<XgJuice>();fx.Init(surface,monitor,owner.Font,owner.gameObject);
            SyncFeedback();
            surface.gameObject.SetActive(false);
        }
        XgBtn TopButton(RectTransform bar,string glyph,UnityEngine.Events.UnityAction onClick,float fromRight,float width)
        {
            var button=ui.Button(bar,glyph,onClick,11);PlaceTopRight(button,fromRight,1,width,19);
            button.image.color=Color.clear;button.label.color=new Color32(138,175,163,255);
            button.label.rectTransform.offsetMin=button.label.rectTransform.offsetMax=Vector2.zero;
            return button;
        }
        XgBtn ActionButton(UnityEngine.Events.UnityAction onClick)
        {
            var button=ui.Button(face,"",onClick,9);
            button.image.color=new Color32(22,53,43,255);button.label.color=new Color32(173,219,194,255);
            button.label.rectTransform.offsetMin=new Vector2(2,0);button.label.rectTransform.offsetMax=new Vector2(-2,0);
            return button;
        }
        TMP_Text Digit(RectTransform parent,string name)
        {
            var rt=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var text=ui.Text(rt," ",26,green,TextAlignmentOptions.Center);text.raycastTarget=false;return text;
        }
        TMP_Text Text(RectTransform parent,string name,Vector2 min,Vector2 max,float size,Color color)
        {var text=ui.Text(Rect(name,parent,new Vector2(0,1),Vector2.one,min,max),"",size,color,TextAlignmentOptions.MidlineLeft);text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;return text;}
        void SetDigits(double amount)
        {
            string next=("¥"+amount.ToString("0.00",CultureInfo.InvariantCulture)).PadLeft(12);
            if(next.Length>12)next=("¥"+amount.ToString("0.00E+0",CultureInfo.InvariantCulture)).PadLeft(12);
            if(next==digits)return;
            for(int i=0;i<12;i++){oldDigits[i].text=digits.Length==12?digits[i].ToString():next[i].ToString();newDigits[i].text=next[i].ToString();}
            int visible=next.TrimStart().Length,first=12-visible;
            // Left-aligned under the status, like the prototype's money row.
            float cell=Mathf.Min(1f/Mathf.Max(1,visible),.075f);
            for(int i=0;i<12;i++)
            {
                var slot=(RectTransform)newDigits[i].rectTransform.parent;
                slot.anchorMin=new Vector2((i-first)*cell,0);
                slot.anchorMax=new Vector2((i-first+1)*cell,1);
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
            bound=owner.Sim;incomeStart=owner.Sim.S.idleAutomaticIncome;displayed=lastIncome=0;recentIncome.Clear();digits="";SetDigits(displayed);nextDigit=Time.unscaledTime+1;nextStatus=0;
            surface.gameObject.SetActive(true);options.gameObject.SetActive(false);entryHost.gameObject.SetActive(false);Click();
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.enabled&&canvas.transform!=surface&&!canvas.transform.IsChildOf(surface)){hiddenCanvases.Add(canvas);canvas.enabled=false;}
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(camera.enabled){hiddenCameras.Add(camera);camera.enabled=false;}
            var native=GetComponent<XgIdleNativeWindow>()??gameObject.AddComponent<XgIdleNativeWindow>();
            native.SetTopmost(topmost);native.Enter();
        }
        public void Restore(){if(!compact)return;compact=false;IsCompact=false;Application.targetFrameRate=previousFrameRate;QualitySettings.vSyncCount=previousVsync;foreach(var canvas in hiddenCanvases)if(canvas!=null)canvas.enabled=true;hiddenCanvases.Clear();foreach(var camera in hiddenCameras)if(camera!=null)camera.enabled=true;hiddenCameras.Clear();if(surface!=null)surface.gameObject.SetActive(false);GetComponent<XgIdleNativeWindow>()?.Restore();}
        // Review full: back in the game on the first desk with pending review.
        void OpenReview()
        {
            Restore();
            foreach(var d in XgCatalog.Desks)if(owner.Sim.ReviewCount(d.id)>0){owner.Sim.S.teachingPack="";owner.Sim.SelectDesk(d.id);break;}
            owner.Open();owner.View?.ShowTab("teach");
        }
        // Same rule as the title's quit: a failed save never quits silently.
        void QuitGame()
        {
            var rt=owner.runtime;
            if(rt!=null&&rt.useDiskSave&&!rt.TestMode&&rt.Sim!=null&&!rt.Sim.InPrologue&&!rt.SaveNow())
            {heading.text=T("未退出：","Cannot quit: ")+rt.SaveStatus;nextStatus=Time.unscaledTime+4;quitArmed=-10;return;}
            // Leave the compact OS window first so the saved player resolution is the normal one.
            Restore();PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy(){Restore();if(surface!=null)Destroy(surface.gameObject);if(entryHost!=null)Destroy(entryHost.gameObject);}
    }
}
