using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Purchased, independent desktop readout. Closing hides its drawings; the taskbar button survives.</summary>
    public sealed class XgBrainWindow : MonoBehaviour
    {
        static XgBrainWindow active;
        XingGuangController controller;
        XgSim bound; XgBoard board;
        PrologueDesk desk;
        RectTransform window,client,virtualBrain,taskButton;
        TMP_Text title,pinLabel,conceptLabel;
        XgBrainWallpaper brain;
        XgBrainWindowFeedback feedback;
        bool Current=>controller!=null&&bound!=null&&ReferenceEquals(bound,controller.Sim);
        public bool ReducedEffects=>bound!=null&&bound.S.reduceFx||controller?.View?.Juice!=null&&controller.View.Juice.Reduced||GamePerformance.Low;
        void DataChanged(){if(Current&&Visible)brain?.GetComponent<XgBrainWallpaperPresentation>()?.DataChanged();}
        bool top=true,wasOwned,legacyCleaned;
        float nextObserve,titleUntil,conceptUntil;
        string lastName="",phenomenonTitle="",nanRegion="",pendingEmergence="";
        int pendingStage,pendingAbility;
        readonly Vector3[] corners=new Vector3[4];
        public bool Visible=>window!=null&&window.gameObject.activeInHierarchy;
        public bool AlwaysOnTop=>top;
        public RectTransform Window=>window;
        public RectTransform TaskButton=>taskButton;
        public XgBrainWallpaper Drawing=>brain;
        public XgBrainReactionGate OnsetGate { get; } = new XgBrainReactionGate();
        public static XgBrainWindow Install(XingGuangController owner)
        {
            var v=owner.GetComponent<XgBrainWindow>()??owner.gameObject.AddComponent<XgBrainWindow>();
            v.controller=owner;active=v;return v;
        }
        string T(string zh,string en)=>bound!=null&&bound.English?en:zh;
        void Update()
        {
            if(controller==null)return;
            if(controller.Sim!=bound||Time.unscaledTime>=nextObserve){nextObserve=Time.unscaledTime+.25f;Observe();}
            if(!Visible)return;
            ResizeDrawing();
            FlushStageFeedback();
            if(!ReducedEffects&&titleUntil>0&&Time.unscaledTime>=titleUntil){titleUntil=0;phenomenonTitle="";UpdateTitle();}
            if(!ReducedEffects&&conceptUntil>0&&Time.unscaledTime>=conceptUntil){conceptUntil=0;conceptLabel.gameObject.SetActive(false);}
        }
        void LateUpdate()
        {
            if(!XgStageCurtain.Busy&&Visible&&top&&window.GetSiblingIndex()!=window.parent.childCount-1)window.SetAsLastSibling();
        }
        void Observe()
        {
            if(!legacyCleaned&&PrologueDirector.Desk!=null)
            {
                var parent=PrologueDirector.Desk.layer.parent;
                foreach(string legacyName in new[]{"Brain Wallpaper","Brain Wallpaper Overlay"})
                {var old=parent.Find(legacyName);if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}}
                legacyCleaned=true;
            }
            var sim=controller.Sim;
            if(sim!=bound){Detach();bound=sim;if(bound!=null){bound.NodeBought+=Bought;bound.CardAnswered+=Answered;bound.ComboTier+=Combo;bound.EpochDone+=Epoch;bound.AbilityEmerged+=Ability;bound.PhenomenonFound+=Phenomenon;bound.StageAdvanced+=StageAdvanced;bound.Emerged+=Emerged;bound.EpochStarted+=EpochStarted;bound.Assessed+=Assessed;bound.DataFileInstalled+=DataInstalled;bound.Diverged+=Diverged;board=bound.Board;if(board!=null)board.ConceptsChanged+=Concepts;}wasOwned=false;feedback?.ResetFeedback();Hide();}
            if(bound==null)return;
            if(board!=bound.Board){if(board!=null)board.ConceptsChanged-=Concepts;board=bound.Board;if(board!=null)board.ConceptsChanged+=Concepts;}
            bool owned=bound.Has("brain.window")&&controller.runtime!=null&&controller.runtime.Sim!=null&&controller.runtime.isActiveAndEnabled&&!controller.runtime.Sim.InPrologue;
            if(!owned){if(taskButton!=null)taskButton.gameObject.SetActive(false);Hide();wasOwned=false;return;}
            if(window==null){desk=PrologueDirector.Desk;if(desk==null)return;Build();}
            if(taskButton==null)InstallTaskButton();
            if(taskButton!=null&&!taskButton.gameObject.activeSelf)taskButton.gameObject.SetActive(true);
            if(!wasOwned){wasOwned=true;Show();}
            string name=bound.AiDisplayName+T("的脑子","'s brain");if(name!=lastName){lastName=name;UpdateTitle();}
        }
        void Build()
        {
            client=desk.Window("AI Brain Window","",Vector2.zero,new Vector2(320,240),out window);
            window.gameObject.AddComponent<Canvas>();window.gameObject.AddComponent<GraphicRaycaster>();
            var bar=(RectTransform)window.Find("Title");
            window.GetComponent<Image>().color=new Color32(18,26,43,255);
            bar.GetComponent<Image>().color=new Color32(11,17,28,255);
            bar.Find("Shine").GetComponent<Image>().color=new Color32(18,26,43,255);
            title=bar.Find("Text").GetComponent<TMP_Text>();title.fontSize=13;title.enableAutoSizing=false;title.overflowMode=TextOverflowModes.Ellipsis;title.textWrappingMode=TextWrappingModes.NoWrap;
            title.color=new Color32(226,233,255,255);
            title.rectTransform.offsetMax=new Vector2(-130,0);
            var close=bar.Find("Close").GetComponent<Button>();close.onClick.RemoveAllListeners();close.onClick.AddListener(Hide);
            close.GetComponent<Image>().color=new Color32(54,31,39,255);
            var closeRt=(RectTransform)close.transform;closeRt.offsetMin=new Vector2(-29,-11);closeRt.offsetMax=new Vector2(-4,11);
            AddButton(bar,"Minimize","−",new Vector2(-57,-11),new Vector2(-32,11),Hide);
            pinLabel=AddButton(bar,"Pin","",new Vector2(-126,-11),new Vector2(-60,11),()=>SetTop(!top));SetTop(true);
            var drag=bar.gameObject.AddComponent<XgBrainWindowDrag>();drag.owner=this;
            PrologueDesk.Fill(client,new Color32(5,12,23,255));client.gameObject.AddComponent<RectMask2D>();
            virtualBrain=PrologueDesk.Centered("Brain Drawing",client,Vector2.zero,new Vector2(1280,832));
            brain=XgBrainWallpaper.InstallWindow(controller,virtualBrain);
            // Keep the drawing non-editable while its opaque client receives inspection input.
            client.GetComponent<Image>().raycastTarget=true;
            client.gameObject.AddComponent<XgBrainWallpaperHover>().owner=brain.GetComponent<XgBrainWallpaperPresentation>();
            // Feedback shares the fitted drawing coordinates; letterboxing must not move its event locations.
            feedback=PrologueDesk.Rect("Action Feedback",virtualBrain,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject.AddComponent<XgBrainWindowFeedback>();feedback.raycastTarget=false;feedback.concepts=brain.GetComponentInChildren<XgWallpaperConcepts>();feedback.ReducedMotion=()=>ReducedEffects;feedback.OnsetGate=OnsetGate;
            conceptLabel=desk.Text(PrologueDesk.Rect("Concept Name",client,new Vector2(0,0),new Vector2(1,0),new Vector2(8,7),new Vector2(-8,29)),"",13,Color.white,TextAlignmentOptions.Center);conceptLabel.raycastTarget=false;conceptLabel.gameObject.SetActive(false);
            conceptLabel.enableAutoSizing=true;conceptLabel.fontSizeMin=9;conceptLabel.fontSizeMax=13;conceptLabel.textWrappingMode=TextWrappingModes.NoWrap;conceptLabel.overflowMode=TextOverflowModes.Ellipsis;
            var resize=PrologueDesk.Rect("Resize",window,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero);resize.anchorMin=resize.anchorMax=Vector2.right;resize.offsetMin=new Vector2(-16,0);resize.offsetMax=new Vector2(0,16);
            PrologueDesk.Fill(resize,new Color(1,1,1,.3f));var grip=resize.gameObject.AddComponent<XgBrainWindowDrag>();grip.owner=this;grip.resize=true;
            InstallTaskButton();PlaceBesideApp();ResizeDrawing();
        }
        TMP_Text AddButton(RectTransform bar,string id,string text,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction click)
        {
            var r=PrologueDesk.Rect(id,bar,new Vector2(1,.5f),new Vector2(1,.5f),min,max);var img=PrologueDesk.Fill(r,new Color32(28,40,61,255));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=img;b.onClick.AddListener(click);
            var label=desk.Text(PrologueDesk.Rect("Text",r,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),text,11,new Color32(226,233,255,255),TextAlignmentOptions.Center);label.raycastTarget=false;return label;
        }
        void InstallTaskButton()
        {
            RectTransform bar=null;
            var canvas=window.GetComponentInParent<Canvas>().rootCanvas;
            foreach(var rt in canvas.GetComponentsInChildren<RectTransform>(true))if(rt.name=="Taskbar"){bar=rt;break;}
            if(bar==null)return;
            var oldPin=bar.Find("Brain Pin Toggle");if(oldPin!=null)oldPin.gameObject.SetActive(false);
            taskButton=PrologueDesk.Centered("AI Brain Taskbar",bar,Vector2.zero,new Vector2(32,32));
            XgTaskbarUtilities.Attach(bar,taskButton);
            var bg=PrologueDesk.Fill(taskButton,new Color32(48,82,107,90));var button=taskButton.gameObject.AddComponent<Button>();button.targetGraphic=bg;
            button.onClick.AddListener(()=>{if(Visible)Hide();else Show();});
            var icon=PrologueDesk.Centered("Brain Icon",taskButton,Vector2.zero,new Vector2(28,28)).gameObject.AddComponent<XgBrainTrayGraphic>();icon.raycastTarget=false;
            LingGuangV05.Desktop.UiTip.Add(button,()=>bound.AiDisplayName+T("的脑子 · 点击显示或收起","'s brain · click to show or hide"));
        }
        void PlaceBesideApp()
        {
            var app=controller.Window!=null?controller.Window.windowContainer:null;if(app==null){Clamp();return;}
            app.GetWorldCorners(corners);Vector2 right=window.parent.InverseTransformPoint(corners[2]);
            window.anchoredPosition=new Vector2(right.x+window.rect.width*.5f+12,right.y-window.rect.height*.5f);Clamp();
        }
        void UpdateTitle(){if(title!=null)title.text=lastName+(!string.IsNullOrEmpty(nanRegion)?" · loss = NaN":string.IsNullOrEmpty(phenomenonTitle)?"":" · "+phenomenonTitle);}
        public void SetTop(bool value){top=value;if(pinLabel!=null)pinLabel.text=T(top?"取消置顶":"置顶",top?"Unpin":"Pin");if(top&&window!=null)window.SetAsLastSibling();}
        public void Show(){if(!Current||!bound.Has("brain.window")||window==null)return;window.gameObject.SetActive(true);DataChanged();window.SetAsLastSibling();Clamp();}
        public void Hide(){if(window!=null)window.gameObject.SetActive(false);}
        public void MoveResize(Vector2 delta,bool resize)
        {
            if(window==null)return;
            if(resize){var old=window.sizeDelta;var next=new Vector2(Mathf.Clamp(old.x+delta.x,240,960),Mathf.Clamp(old.y-delta.y,180,720));window.sizeDelta=next;window.anchoredPosition+=new Vector2((next.x-old.x)*.5f,-(next.y-old.y)*.5f);}
            else window.anchoredPosition+=delta;
            Clamp();ResizeDrawing();
        }
        public void Clamp()
        {
            if(window==null||!(window.parent is RectTransform p))return;var r=p.rect;var h=window.sizeDelta*.5f;
            window.anchoredPosition=new Vector2(Mathf.Clamp(window.anchoredPosition.x,r.xMin+h.x,r.xMax-h.x),Mathf.Clamp(window.anchoredPosition.y,r.yMin+h.y+42,r.yMax-h.y));
        }
        public static float DrawingScale(Vector2 size)=>Mathf.Max(0,Mathf.Min(size.x/1280,size.y/832));
        void ResizeDrawing(){if(virtualBrain==null)return;float fit=DrawingScale(client.rect.size);var scale=new Vector3(fit,fit,1);if(virtualBrain.localScale!=scale)virtualBrain.localScale=scale;}
        string RegionName(string region)=>region=="vision"?T("看图区","Vision"):region=="tone"?T("语气区","Tone"):region=="logic"?T("推理区","Reasoning"):T("读字区","Reading");
        string TrackRegion(int track)=>XgBoardData.DisplayRegion(bound.Run((XgTrack)track).dataset);
        void ClearNaN(string region){if(nanRegion==region){nanRegion="";UpdateTitle();}}
        void Bought(XgNode n){if(!Current||n==null)return;DataChanged();if(n.id=="brain.window"){Observe();return;}if(!Visible)return;if(n.kind==XgNodeKind.Dataset||n.id.StartsWith("pack.")){feedback.NewRegion(n.target??n.id);Caption(T("新题库：","New data: ")+T(n.name,n.nameEn));}}
        void Answered(XgAnswerTrace a){if(!Current||a==null||!a.Settled)return;string region=XgBoardData.DisplayRegion(a.Dataset);ClearNaN(region);DataChanged();if(Visible){feedback.Answer(a.Dataset,a.Outcome.correct);Caption(RegionName(region)+T(a.Outcome.correct?" · 答对":" · 答错",a.Outcome.correct?" · Correct":" · Incorrect"));}}
        void Combo(int tier){if(!Current)return;DataChanged();if(Visible){feedback.Gold();Caption(T("连对 ×","Streak ×")+(tier+2)+" · "+XgJuice.StreakName(tier+2));}}
        void EpochStarted(XgEpoch e){if(!Current||e==null)return;string region=TrackRegion(e.track);ClearNaN(region);DataChanged();if(Visible){feedback.Training(region);Caption(RegionName(region)+T(" · 训练中"," · Training"));}}
        void Epoch(XgEpoch e){if(!Current||e==null||e.diverged)return;string region=TrackRegion(e.track);ClearNaN(region);DataChanged();if(Visible){feedback.TrainDataset(bound.Run((XgTrack)e.track).dataset);Caption(RegionName(region)+T(" · 第 "," · Epoch ")+e.epoch);}}
        void Assessed(XgAssessment a){if(!Current||a==null)return;DataChanged();if(Visible){string region=XgBoardData.DisplayRegion(a.dataset);feedback.Assessment(region,a.record);Caption(RegionName(region)+T(" · 评估 "," · Assessment ")+a.acc.ToString("P0")+" · "+XgCatalog.GradeNames[Mathf.Clamp(a.grade,0,XgCatalog.GradeNames.Length-1)]);}}
        void DataInstalled(string fileId){if(!Current)return;var file=bound.S.dataFiles.Find(value=>value.fileId==fileId&&value.installed);if(file==null)return;DataChanged();if(Visible){feedback.NewRegion(file.dataset);var data=XgCatalog.Dataset(file.dataset);Caption(T("数据已安装 · ","Data installed · ")+(data==null?file.dataset:T(data.name,data.nameEn))+" → "+RegionName(XgBoardData.DisplayRegion(file.dataset)));}}
        void Diverged(int track){if(!Current)return;nanRegion=TrackRegion(track);DataChanged();UpdateTitle();if(Visible){feedback.Diverged(nanRegion);Caption(RegionName(nanRegion)+" · loss = NaN");}}
        void StageAdvanced(int from){if(!Current||!bound.Has("brain.window"))return;pendingStage=Mathf.Max(pendingStage,from+1);}
        void Emerged(int stage,string line){if(!Current||!bound.Has("brain.window"))return;pendingAbility=Mathf.Max(pendingAbility,stage);pendingEmergence=line??"";}
        void Ability(int value){if(!Current)return;DataChanged();if(bound.StagePresentationHeld||XgStageCurtain.Busy){pendingAbility=Mathf.Max(pendingAbility,value);return;}if(Visible){feedback.Rebirth();feedback.concepts?.RearrangeForAbility(value);Caption(XgSim.AbilityName(value,bound.English));}}
        void FlushStageFeedback()
        {
            if(!Current||bound.StagePresentationHeld||XgStageCurtain.Busy||pendingStage==0&&pendingAbility==0)return;
            DataChanged();
            if(pendingStage>0){int stage=Mathf.Clamp(pendingStage,0,XgCatalog.StageNames.Length-1);feedback.Stage(stage);Caption(T("第 ","Stage ")+stage+" · "+T(XgCatalog.StageNames[stage],XgCatalog.StageNamesEn[stage]));}
            else{feedback.Rebirth();Caption(string.IsNullOrEmpty(pendingEmergence)?XgSim.AbilityName(pendingAbility,bound.English):pendingEmergence);}
            if(pendingAbility>0)feedback.concepts?.RearrangeForAbility(pendingAbility);
            pendingStage=pendingAbility=0;pendingEmergence="";
        }
        public static void KnowledgeShown(XgSim source,string name)
        {
            var v=active;if(v==null||!v.Current||v.bound!=source||!v.Visible)return;
            v.DataChanged();v.feedback.Knowledge(source.SelectedTrack==XgTrack.Vision?"vision":"sequence",false);
            v.phenomenonTitle=name;v.titleUntil=Time.unscaledTime+3;v.UpdateTitle();
        }
        void Phenomenon(XgPhenomenon p){if(!Current||p==null)return;DataChanged();if(!Visible)return;feedback.Knowledge(bound.SelectedTrack==XgTrack.Vision?"vision":"sequence",false);phenomenonTitle=T(p.name,p.nameEn);titleUntil=Time.unscaledTime+3;UpdateTitle();}
        void Concepts(IReadOnlyList<XgConceptTransition> changes)
        {
            if(!Current)return;DataChanged();if(!Visible||bound.S.board?.concepts==null)return;
            foreach(var c in changes)if(c.Kind!=XgConceptTransitionKind.Decayed){foreach(var cell in bound.S.board.concepts)if(cell.id==c.Id){feedback.Concept(c.Region);Caption((cell.key??"").Replace("<","‹").Replace(">","›"));return;}}
        }
        void Caption(string value){if(conceptLabel==null)return;conceptLabel.text=value;conceptLabel.gameObject.SetActive(true);conceptUntil=Time.unscaledTime+3;}
        void Detach(){if(bound!=null){bound.NodeBought-=Bought;bound.CardAnswered-=Answered;bound.ComboTier-=Combo;bound.EpochDone-=Epoch;bound.AbilityEmerged-=Ability;bound.PhenomenonFound-=Phenomenon;bound.StageAdvanced-=StageAdvanced;bound.Emerged-=Emerged;bound.EpochStarted-=EpochStarted;bound.Assessed-=Assessed;bound.DataFileInstalled-=DataInstalled;bound.Diverged-=Diverged;}if(board!=null)board.ConceptsChanged-=Concepts;board=null;bound=null;OnsetGate.Clear();pendingStage=pendingAbility=0;pendingEmergence=nanRegion=phenomenonTitle=lastName="";titleUntil=conceptUntil=0;if(conceptLabel!=null)conceptLabel.gameObject.SetActive(false);}
        void OnDestroy(){if(active==this)active=null;Detach();if(window!=null)Destroy(window.gameObject);if(taskButton!=null)Destroy(taskButton.gameObject);}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgBrainTrayGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float scale=Mathf.Min(r.width,r.height)/28;
            var points=new[]{new Vector2(-3,10),new Vector2(-9,5),new Vector2(-9,-3),new Vector2(-3,-9),new Vector2(3,10),new Vector2(9,5),new Vector2(9,-3),new Vector2(3,-9),new Vector2(0,1)};
            for(int i=0;i<points.Length;i++)points[i]=r.center+points[i]*scale;
            var edges=new[]{0,1,1,2,2,3,4,5,5,6,6,7,0,4,3,7,1,8,6,8,3,8,4,8};
            var line=new Color32(134,204,238,255);
            for(int i=0;i<edges.Length;i+=2)XgDraw.Seg(vh,points[edges[i]],points[edges[i+1]],1.6f*scale,line);
            for(int i=0;i<points.Length;i++)XgDraw.Disc(vh,points[i],(i==8?2.8f:2)*scale,i==8?new Color32(202,172,255,255):new Color32(164,238,231,255),12);
        }
    }
    public sealed class XgBrainWindowDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IPointerDownHandler
    {
        public XgBrainWindow owner;public bool resize;Vector2 last;
        public void OnPointerDown(PointerEventData e){if(owner?.Window!=null)owner.Window.SetAsLastSibling();}
        public void OnBeginDrag(PointerEventData e){if(owner?.Window!=null)RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)owner.Window.parent,e.position,e.pressEventCamera,out last);}
        public void OnDrag(PointerEventData e){if(owner?.Window==null)return;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)owner.Window.parent,e.position,e.pressEventCamera,out var now);owner.MoveResize(now-last,resize);last=now;}
    }
}
