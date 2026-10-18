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
        TMP_Text title,taskLabel,pinLabel,conceptLabel;
        XgBrainWallpaper brain;
        XgBrainWindowFeedback feedback;
        void DataChanged(){brain?.GetComponent<XgBrainWallpaperPresentation>()?.DataChanged();}
        bool top=true,wasOwned,legacyCleaned;
        float nextObserve,titleUntil,conceptUntil;
        string lastName="",phenomenonTitle="";
        readonly Vector3[] corners=new Vector3[4];
        public bool Visible=>window!=null&&window.gameObject.activeInHierarchy;
        public bool AlwaysOnTop=>top;
        public RectTransform Window=>window;
        public RectTransform TaskButton=>taskButton;
        public XgBrainWallpaper Drawing=>brain;
        public static XgBrainWindow Install(XingGuangController owner)
        {
            var v=owner.GetComponent<XgBrainWindow>()??owner.gameObject.AddComponent<XgBrainWindow>();
            v.controller=owner;active=v;return v;
        }
        string T(string zh,string en)=>bound!=null&&bound.English?en:zh;
        void Update()
        {
            if(controller==null)return;
            if(Time.unscaledTime>=nextObserve){nextObserve=Time.unscaledTime+.25f;Observe();}
            if(!Visible)return;
            ResizeDrawing();
            if(titleUntil>0&&Time.unscaledTime>=titleUntil){titleUntil=0;phenomenonTitle="";UpdateTitle();}
            if(conceptUntil>0&&Time.unscaledTime>=conceptUntil){conceptUntil=0;conceptLabel.gameObject.SetActive(false);}
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
            if(sim!=bound){Detach();bound=sim;if(bound!=null){bound.NodeBought+=Bought;bound.CardAnswered+=Answered;bound.ComboTier+=Combo;bound.EpochDone+=Epoch;bound.AbilityEmerged+=Ability;bound.PhenomenonFound+=Phenomenon;board=bound.Board;if(board!=null)board.ConceptsChanged+=Concepts;}wasOwned=false;feedback?.ResetFeedback();Hide();}
            if(bound==null)return;
            if(board!=bound.Board){if(board!=null)board.ConceptsChanged-=Concepts;board=bound.Board;if(board!=null)board.ConceptsChanged+=Concepts;}
            bool owned=bound.Has("brain.window")&&controller.runtime!=null&&controller.runtime.Sim!=null&&controller.runtime.isActiveAndEnabled&&!controller.runtime.Sim.InPrologue;
            if(!owned){if(taskButton!=null)taskButton.gameObject.SetActive(false);Hide();wasOwned=false;return;}
            if(window==null){desk=PrologueDirector.Desk;if(desk==null)return;Build();}
            if(taskButton!=null&&!taskButton.gameObject.activeSelf)taskButton.gameObject.SetActive(true);
            if(!wasOwned){wasOwned=true;Show();}
            string name=bound.AiDisplayName+T("的脑子","'s brain");if(name!=lastName){lastName=name;UpdateTitle();if(taskLabel!=null)taskLabel.text=name;}
        }
        void Build()
        {
            client=desk.Window("AI Brain Window","",Vector2.zero,new Vector2(320,240),out window);
            window.gameObject.AddComponent<Canvas>();window.gameObject.AddComponent<GraphicRaycaster>();
            var bar=(RectTransform)window.Find("Title");
            title=bar.Find("Text").GetComponent<TMP_Text>();title.fontSize=13;title.enableAutoSizing=false;title.overflowMode=TextOverflowModes.Ellipsis;title.textWrappingMode=TextWrappingModes.NoWrap;
            title.rectTransform.offsetMax=new Vector2(-130,0);
            var close=bar.Find("Close").GetComponent<Button>();close.onClick.RemoveAllListeners();close.onClick.AddListener(Hide);
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
            feedback=PrologueDesk.Rect("Action Feedback",client,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject.AddComponent<XgBrainWindowFeedback>();feedback.raycastTarget=false;feedback.concepts=brain.GetComponentInChildren<XgWallpaperConcepts>();
            conceptLabel=desk.Text(PrologueDesk.Rect("Concept Name",client,new Vector2(0,0),new Vector2(1,0),new Vector2(8,7),new Vector2(-8,29)),"",13,Color.white,TextAlignmentOptions.Center);conceptLabel.raycastTarget=false;conceptLabel.gameObject.SetActive(false);
            var resize=PrologueDesk.Rect("Resize",window,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero);resize.anchorMin=resize.anchorMax=Vector2.right;resize.offsetMin=new Vector2(-16,0);resize.offsetMax=new Vector2(0,16);
            PrologueDesk.Fill(resize,new Color(1,1,1,.3f));var grip=resize.gameObject.AddComponent<XgBrainWindowDrag>();grip.owner=this;grip.resize=true;
            InstallTaskButton();PlaceBesideApp();ResizeDrawing();
        }
        TMP_Text AddButton(RectTransform bar,string id,string text,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction click)
        {
            var r=PrologueDesk.Rect(id,bar,new Vector2(1,.5f),new Vector2(1,.5f),min,max);var img=PrologueDesk.Fill(r,new Color32(175,199,225,255));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=img;b.onClick.AddListener(click);
            var label=desk.Text(PrologueDesk.Rect("Text",r,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),text,11,PrologueDesk.Ink,TextAlignmentOptions.Center);label.raycastTarget=false;return label;
        }
        void InstallTaskButton()
        {
            RectTransform bar=null;
            var canvas=window.GetComponentInParent<Canvas>().rootCanvas;
            foreach(var rt in canvas.GetComponentsInChildren<RectTransform>(true))if(rt.name=="Taskbar"){bar=rt;break;}
            if(bar==null)return;
            var oldPin=bar.Find("Brain Pin Toggle");if(oldPin!=null)oldPin.gameObject.SetActive(false);
            var area=bar.Find("Aero Notification Area") as RectTransform;float right=area!=null?area.rect.width:242;
            taskButton=PrologueDesk.Rect("AI Brain Taskbar",bar,new Vector2(1,0),Vector2.one,new Vector2(-right-334,3),new Vector2(-right-268,-3));
            var bg=PrologueDesk.Fill(taskButton,new Color32(48,82,107,255));var button=taskButton.gameObject.AddComponent<Button>();button.targetGraphic=bg;button.onClick.AddListener(Show);
            taskLabel=desk.Text(PrologueDesk.Rect("Text",taskButton,Vector2.zero,Vector2.one,new Vector2(5,0),new Vector2(-5,0)),"",12,Color.white,TextAlignmentOptions.Center);taskLabel.raycastTarget=false;taskLabel.overflowMode=TextOverflowModes.Ellipsis;
            LingGuangV05.Desktop.UiTip.Add(button,()=>T("打开 ","Open ")+bound.AiDisplayName+T("的脑子 · 关闭后点这里重新打开","'s brain · reopen here after closing"));
        }
        void PlaceBesideApp()
        {
            var app=controller.Window!=null?controller.Window.windowContainer:null;if(app==null){Clamp();return;}
            app.GetWorldCorners(corners);Vector2 right=window.parent.InverseTransformPoint(corners[2]);
            window.anchoredPosition=new Vector2(right.x+window.rect.width*.5f+12,right.y-window.rect.height*.5f);Clamp();
        }
        void UpdateTitle(){if(title!=null)title.text=lastName+(string.IsNullOrEmpty(phenomenonTitle)?"":" · "+phenomenonTitle);}
        public void SetTop(bool value){top=value;if(pinLabel!=null)pinLabel.text=T(top?"取消置顶":"置顶",top?"Unpin":"Pin");if(top&&window!=null)window.SetAsLastSibling();}
        public void Show(){if(bound==null||!bound.Has("brain.window")||window==null)return;window.gameObject.SetActive(true);DataChanged();window.SetAsLastSibling();Clamp();}
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
        void ResizeDrawing(){if(virtualBrain==null)return;var scale=new Vector3(client.rect.width/1280,client.rect.height/832,1);if(virtualBrain.localScale!=scale)virtualBrain.localScale=scale;}
        void Bought(XgNode n){DataChanged();if(n.id=="brain.window"){Observe();return;}if(!Visible)return;if(n.kind==XgNodeKind.Dataset||n.id.StartsWith("pack.")){feedback.NewRegion(n.target??n.id);Caption(T("新题库：","New data: ")+T(n.name,n.nameEn));}}
        void Answered(XgAnswerTrace a){if(a==null||!a.Settled)return;DataChanged();if(Visible)feedback.Region(a.Region,a.Outcome.correct);}
        void Combo(int tier){DataChanged();if(Visible)feedback.Gold();}
        void Epoch(XgEpoch e){DataChanged();if(Visible)feedback.Sweep();}
        void Ability(int value){DataChanged();if(Visible){feedback.Rebirth();feedback.concepts?.RearrangeForAbility(value);}}
        public static void KnowledgeShown(XgSim source,string name)
        {
            var v=active;if(v==null||v.bound!=source||!v.Visible)return;
            v.DataChanged();v.feedback.Knowledge(source.SelectedTrack==XgTrack.Vision?"vision":"sequence",false);
            v.phenomenonTitle=name;v.titleUntil=Time.unscaledTime+3;v.UpdateTitle();
        }
        void Phenomenon(XgPhenomenon p){DataChanged();if(!Visible)return;feedback.Knowledge(bound.SelectedTrack==XgTrack.Vision?"vision":"sequence",false);phenomenonTitle=T(p.name,p.nameEn);titleUntil=Time.unscaledTime+3;UpdateTitle();}
        void Concepts(IReadOnlyList<XgConceptTransition> changes)
        {
            DataChanged();if(!Visible||bound.S.board?.concepts==null)return;
            foreach(var c in changes)if(c.Kind!=XgConceptTransitionKind.Decayed){foreach(var cell in bound.S.board.concepts)if(cell.id==c.Id){feedback.Concept(c.Region);Caption((cell.key??"").Replace("<","‹").Replace(">","›"));return;}}
        }
        void Caption(string value){conceptLabel.text=value;conceptLabel.gameObject.SetActive(true);conceptUntil=Time.unscaledTime+2;}
        void Detach(){if(bound!=null){bound.NodeBought-=Bought;bound.CardAnswered-=Answered;bound.ComboTier-=Combo;bound.EpochDone-=Epoch;bound.AbilityEmerged-=Ability;bound.PhenomenonFound-=Phenomenon;}if(board!=null)board.ConceptsChanged-=Concepts;board=null;bound=null;}
        void OnDestroy(){if(active==this)active=null;Detach();if(window!=null)Destroy(window.gameObject);if(taskButton!=null)Destroy(taskButton.gameObject);}
    }
    public sealed class XgBrainWindowDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IPointerDownHandler
    {
        public XgBrainWindow owner;public bool resize;Vector2 last;
        public void OnPointerDown(PointerEventData e){if(owner?.Window!=null)owner.Window.SetAsLastSibling();}
        public void OnBeginDrag(PointerEventData e){if(owner?.Window!=null)RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)owner.Window.parent,e.position,e.pressEventCamera,out last);}
        public void OnDrag(PointerEventData e){if(owner?.Window==null)return;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)owner.Window.parent,e.position,e.pressEventCamera,out var now);owner.MoveResize(now-last,resize);last=now;}
    }
}
