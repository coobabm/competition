using System;
using System.Collections;
using System.Globalization;
using LingGuangV05.CaseMenu;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    // Presentation of the existing automation, never a second simulation or income source.
    public sealed class XgIdleWindow : MonoBehaviour
    {
        public static bool IsCompact { get; private set; }
        XingGuangController owner;
        CaseMenuController menu;
        XgSim bound;
        XgUi ui;
        XgBtn entry, pause, review, offline;
        RectTransform surface, monitor, options;
        TMP_Text heading, state;
        TMP_Text[] oldDigits, newDigits;
        XgJuice fx;
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
            if(Time.unscaledTime>=nextObserve)
            {
                nextObserve=Time.unscaledTime+.25f;
                if(entry==null && Ready && PrologueDirector.Desk!=null && owner.Font!=null)Build();
                if(entry!=null)entry.rt.gameObject.SetActive(Ready&&!compact);
                if(!Ready || bound!=owner.Sim){Restore();bound=owner.Sim;}
                if(entry!=null)entry.label.text=XgIdleRules.Unlocked(owner.Sim)?T("挂机","Idle"):T("挂机 · 解锁 ›","Idle · Unlock ›");
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
            review.label.text=owner.Sim.ReviewFull?T("处理复核","Review"):T("返回游戏","Restore");
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
            for(int i=0;i<newDigits.Length;i++)
            {
                bool changed=oldDigits[i].text!=newDigits[i].text;
                oldDigits[i].rectTransform.anchoredPosition=new Vector2(0,changed?eased*32:32);
                newDigits[i].rectTransform.anchoredPosition=new Vector2(0,changed?(eased-1)*32:0);
            }
        }
        void Build()
        {
            ui=new XgUi(owner.Font,null,true);
            entry=ui.Button(PrologueDirector.Desk.layer,T("挂机 · 解锁 ›","Idle · Unlock ›"),Enter,13);
            PlaceBottomLeft(entry,260,42,132,30);
            var go=new GameObject("Compact earning window",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.transform.SetParent(transform,false);surface=(RectTransform)go.transform;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(288,168);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            Panel(Rect("Backdrop",surface,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),new Color32(12,17,20,255));
            monitor=Rect("Title monitor",surface,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-128,-74),new Vector2(128,74));
            var shell=Panel(monitor,new Color32(234,224,201,255));RoundSurface(shell,12);
            var titleArt=Resources.Load<Texture2D>("LingGuangV05/Branding/IdleMonitor");
            if(titleArt!=null)
            {
                shell.color=Color.clear;
                var art=Rect("Original title artwork",monitor,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject.AddComponent<RawImage>();
                art.texture=titleArt;art.raycastTarget=false;
            }
            var face=Rect("Screen",monitor,Vector2.zero,Vector2.one,new Vector2(16,30),new Vector2(-16,-12));
            RoundSurface(Panel(face,new Color32(8,24,20,255)),8);
            heading=Text(face,"Heading",new Vector2(8,-22),new Vector2(-8,-3),12,new Color32(204,225,214,255));
            var native=GetComponent<XgIdleNativeWindow>()??gameObject.AddComponent<XgIdleNativeWindow>();
            heading.raycastTarget=true;heading.gameObject.AddComponent<XgIdleDragHandle>().Window=native;
            var row=Rect("Rolling wallet",face,new Vector2(0,1),Vector2.one,new Vector2(10,-61),new Vector2(-10,-26));
            oldDigits=new TMP_Text[12];newDigits=new TMP_Text[12];
            for(int i=0;i<12;i++)
            {
                var cell=Rect("Digit "+i,row,new Vector2(i/12f,0),new Vector2((i+1)/12f,1),Vector2.zero,Vector2.zero);cell.gameObject.AddComponent<RectMask2D>();
                oldDigits[i]=Digit(cell,"Old");newDigits[i]=Digit(cell,"New");
            }
            state=Text(face,"Status",new Vector2(8,-79),new Vector2(-8,-61),10,new Color32(139,167,151,255));
            pause=ui.Button(face,"",()=>{Click();owner.Sim.AutoPaused=!owner.Sim.AutoPaused;owner.runtime.MarkDirty();nextStatus=0;},11);PlaceBottomLeft(pause,8,5,58,22);
            review=ui.Button(face,"",()=>{Click();bool full=owner.Sim.ReviewFull;Restore();if(full){foreach(var d in XgCatalog.Desks)if(owner.Sim.ReviewCount(d.id)>0){owner.Sim.S.teachingPack="";owner.Sim.SelectDesk(d.id);break;}owner.Open();owner.View?.ShowTab("teach");}},11);PlaceBottomLeft(review,72,5,88,22);
            var more=ui.Button(face,"···",()=>{Click();options.gameObject.SetActive(true);},12);PlaceBottomLeft(more,166,5,50,22);
            options=Rect("Idle options",face,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            RoundSurface(Panel(options,new Color32(8,24,20,255)),8);
            var explanation=Text(options,"Offline explanation",new Vector2(9,-50),new Vector2(-9,-5),11,new Color32(204,225,214,255));
            explanation.text=T("离线只结算金钱\n50% 效率 · 最多 8 小时","Money only while away\n50% efficiency · up to 8 hours");
            offline=ui.Button(options,"",()=>{Click();XgOfflineIncome.SetEnabled(owner,!XgOfflineIncome.Enabled(owner));},11);PlaceBottomLeft(offline,8,35,208,25);
            var back=ui.Button(options,T("返回","Back"),()=>{Click();options.gameObject.SetActive(false);},11);PlaceBottomLeft(back,8,5,96,24);
            var restore=ui.Button(options,T("恢复游戏窗口","Restore game"),()=>{Click();Restore();},11);PlaceBottomLeft(restore,110,5,106,24);
            options.gameObject.SetActive(false);
            var lamp=Rect("Orange power lamp",monitor,Vector2.zero,Vector2.zero,new Vector2(231,22),new Vector2(237,28));RoundSurface(Panel(lamp,new Color32(244,153,73,255)),3);
            ui.Text(Rect("Brand",monitor,Vector2.zero,new Vector2(1,0),new Vector2(18,17),new Vector2(-30,30)),T("我的电脑活了","MY COMPUTER IS ALIVE"),9,new Color32(91,88,76,255),TextAlignmentOptions.MidlineLeft);
            fx=go.AddComponent<XgJuice>();fx.Init(surface,monitor,owner.Font,owner.gameObject);
            surface.gameObject.SetActive(false);
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
            digits=next;rollStart=Time.unscaledTime;
        }
        void Click(){if(fx!=null){fx.Muted=owner.Sim.S.mute;fx.Reduced=Reduced;fx.Play(XgJuice.Sfx.Id.Click,1,.35f);}}
        public void Enter()
        {
            if(compact||!Ready||StoryFocus.Exclusive||XgStageCurtain.Busy||XgEmergenceCutscene.Busy)return;
            if(!XgIdleRules.Unlocked(owner.Sim))
            {
                owner.Open();owner.View?.ShowTab("tree");owner.View?.Tree.FocusNodeDetails(XgIdleRules.UnlockNode);return;
            }
            if(surface==null)Build();
            compact=true;IsCompact=true;previousFrameRate=Application.targetFrameRate;previousVsync=QualitySettings.vSyncCount;QualitySettings.vSyncCount=0;Application.targetFrameRate=30;
            bound=owner.Sim;incomeStart=owner.Sim.S.idleAutomaticIncome;displayed=lastIncome=0;recentIncome.Clear();digits="";SetDigits(displayed);nextDigit=Time.unscaledTime+1;
            surface.gameObject.SetActive(true);options.gameObject.SetActive(false);entry.rt.gameObject.SetActive(false);Click();
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.enabled&&canvas.transform!=surface&&!canvas.transform.IsChildOf(surface)){hiddenCanvases.Add(canvas);canvas.enabled=false;}
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(camera.enabled){hiddenCameras.Add(camera);camera.enabled=false;}
            GetComponent<XgIdleNativeWindow>()?.Enter();
            var native=GetComponent<XgIdleNativeWindow>();if(native==null){native=gameObject.AddComponent<XgIdleNativeWindow>();native.Enter();}
        }
        public void Restore(){if(!compact)return;compact=false;IsCompact=false;Application.targetFrameRate=previousFrameRate;QualitySettings.vSyncCount=previousVsync;foreach(var canvas in hiddenCanvases)if(canvas!=null)canvas.enabled=true;hiddenCanvases.Clear();foreach(var camera in hiddenCameras)if(camera!=null)camera.enabled=true;hiddenCameras.Clear();if(surface!=null)surface.gameObject.SetActive(false);GetComponent<XgIdleNativeWindow>()?.Restore();}
        void OnDestroy(){Restore();if(surface!=null)Destroy(surface.gameObject);if(entry!=null)Destroy(entry.rt.gameObject);}
    }
}
