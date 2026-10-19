using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Mail
{
    // Mount on an existing MailInboxView content object. All persistence and final routing belong to the owner.
    // Plain Win7 colours and text; no blur, particle system, generated images or model calls.
    public sealed class EndingLetterPresenter : MonoBehaviour
    {
        static readonly Color Paper=new Color32(250,250,247,255),Ink=new Color32(34,40,52,255),Muted=new Color32(80,93,108,255),Edge=new Color32(187,200,216,255);
        EndingLetterCatalog catalog;
        EndingLetterSession session;
        EndingLetterCallbacks callbacks;
        RectTransform root,content,viewport,footer,consent;
        ScrollRect scroll;
        TMP_FontAsset font;
        WindowManager nativeWindow;
        TMP_Text title,source,status,previewText,consentText,consentTitle;
        Button open,save,continueButton,finish,back,read,later,smaller,larger,publicArchive;
        Button mailRow;
        readonly List<(string id,TMP_Text text)> paragraphs=new List<(string,TMP_Text)>();
        EndingLetterView rendered=EndingLetterView.Closed;
        bool english,restoring,readToEnd,readingGesture;
        int size=19,bindingEpoch;
        float restoreAt,savePositionAt;
        string queuedPosition="";
        public event Action StateChanged;
        public EndingLetterSession Session=>session;
        public RectTransform Root=>root;
        public bool Visible=>root!=null&&root.gameObject.activeInHierarchy;
        public EndingLetterInput Snapshot=>session?.Input;
        // Presentation geometry only. Never persisted as an opened/read/completion receipt.
        public bool ReadToEnd=>readToEnd;

        public static EndingLetterPresenter Install(RectTransform host,TMP_FontAsset font,WindowManager window)
        {
            if(host==null)throw new ArgumentNullException(nameof(host));
            var presenter=host.GetComponent<EndingLetterPresenter>()??host.gameObject.AddComponent<EndingLetterPresenter>();
            if(presenter.session!=null)return presenter;
            var asset=Resources.Load<TextAsset>(EndingLetterCatalog.ResourcePath);
            if(asset==null)throw new InvalidOperationException("Ending letters resource missing");
            presenter.Initialize(host,font,window,new EndingLetterCatalog(JsonUtility.FromJson<EndingLetterResource>(asset.text)));
            return presenter;
        }
        // Explicit catalogue injection also permits isolated UI verification without altering production resources.
        public void Initialize(RectTransform host,TMP_FontAsset textFont,WindowManager window,EndingLetterCatalog contentCatalog)
        {
            if(session!=null)throw new InvalidOperationException("Ending letter presenter is already initialized");
            font=textFont;nativeWindow=window;catalog=contentCatalog;session=new EndingLetterSession(catalog);
            if(nativeWindow!=null){nativeWindow.onClose.AddListener(OnNativeDismissed);nativeWindow.onMinimize.AddListener(OnNativeDismissed);}
            Build(host);session.Changed+=OnSessionChanged;root.gameObject.SetActive(false);
        }
        public void Bind(EndingLetterInput input,EndingLetterCallbacks actions,bool show=false)
        {
            ResetReadGate();bindingEpoch++;callbacks=actions;queuedPosition="";rendered=EndingLetterView.Closed;
            english=input?.english??false;SetPreferences(input?.reduceFx??false,input?.soundEnabled??false);
            session.Bind(input,actions);root.gameObject.SetActive(show&&input!=null);
            if(show){root.SetAsLastSibling();Render();}
        }
        public void ShowInbox()
        {
            if(session?.Input==null||!session.Current)return;
            ResetReadGate();var input=session.Input;session.Bind(input,callbacks);root.gameObject.SetActive(true);root.SetAsLastSibling();Render();
        }
        public void RequestOpen(){if(!Visible)ShowInbox();ClickFeedback();session?.OpenPrompt();}
        public void SetLanguage(bool value)
        {
            if(english==value)return;FlushPosition();english=value;
            if(Visible){RebuildParagraphs();Render();RestorePosition();}
        }
        public void SetPreferences(bool reduceFx,bool soundEnabled)
        {
            // Preferences do not mutate the save or replace its identity/revision.
            reduceMotion=reduceFx;sound=soundEnabled;ApplyTransitions();
        }
        bool reduceMotion,sound;
        public void BindRow(Button row)
        {
            if(row==null)return;
            mailRow=row;ApplyTransition(row);
            int epoch=bindingEpoch;var gesture=row.GetComponent<EndingLetterOpenGesture>()??row.gameObject.AddComponent<EndingLetterOpenGesture>();
            gesture.Select=()=>{if(epoch!=bindingEpoch||!session.Current)return;ClickFeedback();ShowInbox();};
            gesture.Open=()=>{if(epoch!=bindingEpoch||!session.Current)return;RequestOpen();};
        }
        public string RowStatus
        {
            get {var i=session?.Input;if(i==null)return "";return Copy(i.readOnly?"UI-ARCHIVE":i.opened?"UI-UNFINISHED":"UI-UNREAD");}
        }
        public string RowTitle=>session?.Input==null?"":catalog.Get(session.Input.articleId).Title(english);
        public string Warning=>Copy("UI-WARN");
        public string Sender=>Copy("UI-SENDER");
        public bool Unread=>session?.Input!=null&&session.Input.saved&&!session.Input.opened&&!session.Input.readOnly;
        string Copy(string id)=>catalog.Ui(id,english);

        void OpenPublicArchive()
        {
            if(session==null||session.Busy||!session.Current||callbacks?.RequestPublicArchive==null)return;
            FlushPosition();if(!session.Current||session.Busy)return;
            var i=session.Input;var article=catalog.Get(i.articleId);
            callbacks.RequestPublicArchive(new EndingLetterRequest{owner=i.owner,revision=i.revision,articleId=i.articleId,
                sourceArticleId=article.sourceArticleId,version=article.version});
        }
        void Build(RectTransform parent)
        {
            root=Rect("EndingLetter",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Fill(root,Paper);
            title=Text(Rect("Title",root,new Vector2(0,1),Vector2.one,new Vector2(22,-60),new Vector2(-22,-12)),24,Ink);title.fontStyle=FontStyles.Bold;
            source=Text(Rect("Source",root,new Vector2(0,1),Vector2.one,new Vector2(22,-125),new Vector2(-22,-64)),13,Muted);
            var area=Rect("Scroll",root,Vector2.zero,Vector2.one,new Vector2(18,210),new Vector2(-18,-134));
            Fill(area,Paper);scroll=area.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
            viewport=Rect("Viewport",area,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);viewport.gameObject.AddComponent<RectMask2D>();Fill(viewport,new Color(0,0,0,0));
            content=Rect("Paragraphs",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);content.pivot=new Vector2(.5f,1);
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(12,12,8,16);layout.spacing=18;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.content=content;scroll.viewport=viewport;scroll.onValueChanged.AddListener(OnScrolled);
            area.gameObject.AddComponent<EndingLetterReadingGesture>().Scrolled=()=>
            {if(!restoring&&Visible&&WindowVisible()&&session?.View==EndingLetterView.Reading)readingGesture=true;};
            previewText=Text(Rect("Notice",content,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),18,Ink);
            footer=Rect("Actions",root,Vector2.zero,new Vector2(1,0),new Vector2(18,8),new Vector2(-18,202));
            status=Text(Rect("Status",footer,new Vector2(0,1),Vector2.one,new Vector2(4,-38),new Vector2(-4,0)),13,Muted);
            save=Button("SaveArticle",footer,()=>session.SaveArticle());
            continueButton=Button("ContinueEpilogue",footer,()=>session.SaveArticle(true));
            open=Button("OpenLetter",footer,()=>session.OpenPrompt());
            finish=Button("CompleteEnding",footer,()=>{if(!readToEnd||restoring)return;FlushPosition();session.Complete();});
            back=Button("BackToDesktop",footer,()=>{FlushPosition();session.Back();});
            smaller=Button("SmallerText",footer,()=>ResizeText(-1));larger=Button("LargerText",footer,()=>ResizeText(1));
            consent=Rect("OpenConsent",root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Fill(consent,new Color32(233,239,247,255));
            consentTitle=Text(Rect("Title",consent,new Vector2(0,1),Vector2.one,new Vector2(30,-82),new Vector2(-30,-28)),23,Ink);
            consentText=Text(Rect("Notice",consent,Vector2.zero,Vector2.one,new Vector2(30,150),new Vector2(-30,-100)),18,Ink);
            read=Button("ReadFirst",consent,()=>session.OpenAccepted());later=Button("NotNow",consent,()=>session.CancelOpen());
            publicArchive=Button("OpenPublicLetters",root,OpenPublicArchive);
            var archiveRect=(RectTransform)publicArchive.transform;archiveRect.anchorMin=archiveRect.anchorMax=Vector2.one;
            archiveRect.offsetMin=new Vector2(-212,-54);archiveRect.offsetMax=new Vector2(-22,-12);
            LayoutButtons();ApplyTransitions();
        }
        Button Button(string name,RectTransform parent,Action action)
        {
            var rt=Rect(name,parent,Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,42));
            var bg=Fill(rt,new Color32(223,233,245,255));rt.gameObject.AddComponent<Outline>().effectColor=Edge;
            var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=bg;button.onClick.AddListener(()=>{ClickFeedback();action();});
            Text(Rect("Text",rt,Vector2.zero,Vector2.one,new Vector2(9,3),new Vector2(-9,-3)),14,Ink).alignment=TextAlignmentOptions.Center;
            if(nativeWindow!=null){var focus=rt.gameObject.AddComponent<LingGuangV05.Desktop.ChapterOneWindowFocus>();focus.Window=nativeWindow;}
            return button;
        }
        void OnSessionChanged()
        {
            if(session.View!=EndingLetterView.Reading)ResetReadGate();
            if(session.View==EndingLetterView.Closed&&root!=null)root.gameObject.SetActive(false);
            if(Visible)Render();StateChanged?.Invoke();
        }
        void Render()
        {
            var input=session.Input;if(input==null)return;
            var article=catalog.Get(input.articleId);bool reading=session.View==EndingLetterView.Reading;
            title.text=article.Title(english);
            bool archiveAvailable=callbacks?.RequestPublicArchive!=null;
            publicArchive.gameObject.SetActive(archiveAvailable);publicArchive.interactable=!session.Busy;
            publicArchive.GetComponentInChildren<TMP_Text>().text=english?"All three public letters":"三封公开来信";
            title.rectTransform.offsetMax=new Vector2(archiveAvailable?-230:-22,-12);
            source.text=Copy("UI-PUBLIC")+" · 2017-06-13\n"+(input.saved?Copy("UI-SENDER"):Copy("UI-SAVE-NOTE"));
            previewText.gameObject.SetActive(!reading);scroll.gameObject.SetActive(true);
            previewText.text=session.View==EndingLetterView.Complete?Copy("UI-COMPLETED"):input.readOnly?Copy("UI-ARCHIVE"):Copy("UI-WARN")+"\n\n"+Copy("UI-SAVE-NOTE");
            if(!reading){foreach(var p in paragraphs)p.text.gameObject.SetActive(false);scroll.verticalNormalizedPosition=1;}
            consent.gameObject.SetActive(session.View==EndingLetterView.Consent);
            consentTitle.text=Copy("UI-OPEN-TITLE");consentText.text=Copy("UI-CONFIRM");
            Label(save,"UI-SAVE");Label(continueButton,"UI-CONTINUE");Label(open,"UI-OPEN");Label(finish,"UI-FINISH");
            Label(back,input.readOnly?"UI-REPLAY":"UI-BACK");Label(read,"UI-READ");Label(later,"UI-LATER");Label(smaller,"UI-SMALLER");Label(larger,"UI-LARGER");
            save.gameObject.SetActive(!reading&&!input.saved&&!input.readOnly);
            continueButton.gameObject.SetActive(!reading&&!input.saved&&!input.readOnly);
            open.gameObject.SetActive(!reading&&(input.saved||input.readOnly)&&session.View!=EndingLetterView.Complete);
            finish.gameObject.SetActive(reading&&!input.readOnly);finish.interactable=!session.Busy&&input.completionEligible&&readToEnd&&!restoring;
            back.interactable=session.PendingKind!="complete";open.interactable=save.interactable=continueButton.interactable=read.interactable=later.interactable=!session.Busy;
            smaller.gameObject.SetActive(reading);larger.gameObject.SetActive(reading);smaller.interactable=size>16;larger.interactable=size<26;
            status.text=session.ErrorCopyId.Length>0?Copy(session.ErrorCopyId):session.Busy?Copy("UI-SAVING")
                :session.View==EndingLetterView.Complete?Copy("UI-COMPLETED"):reading&&!input.readOnly&&!input.completionEligible?Copy("UI-UNAVAILABLE")
                :reading&&!input.readOnly&&!readToEnd?(english?"Scroll to the end of the letter to finish reading.":"读到信末后，可以保存并返回标题。")
                :input.readOnly?Copy("UI-ARCHIVE"):input.saved?Copy("UI-SAVED"):"";
            if(reading&&rendered!=EndingLetterView.Reading){RebuildParagraphs();RestorePosition();}
            if(session.View==EndingLetterView.Consent&&rendered!=EndingLetterView.Consent&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(later.gameObject);
            rendered=session.View;LayoutButtons();
        }
        void Label(Button button,string key)=>button.GetComponentInChildren<TMP_Text>(true).text=Copy(key);
        void RebuildParagraphs()
        {
            if(session?.Input==null)return;
            var article=catalog.Get(session.Input.articleId);
            // Public text stays intact; a separately labelled private appendix follows only verified evidence.
            var displayed=new List<EndingLetterParagraph>(article.paragraphs);
            var appendix=catalog.EligibleAppendix(article.id,session.Input.personalDisclosure);
            if(appendix!=null)displayed.Add(new EndingLetterParagraph { id=appendix.id,
                zh="私人附记 · 回复你主动发送的消息\n\n"+appendix.zh,
                en="Private note · In response to the message you sent\n\n"+appendix.en });
            while(paragraphs.Count<displayed.Count)
            {var text=Text(Rect("Paragraph",content,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),size,Ink);paragraphs.Add(("",text));}
            for(int i=0;i<paragraphs.Count;i++)
            {
                bool active=i<displayed.Count;paragraphs[i].text.gameObject.SetActive(active);if(!active)continue;
                var p=displayed[i];var text=paragraphs[i].text;paragraphs[i]=(p.id,text);text.name="Paragraph_"+p.id;text.text=p.Text(english);text.fontSize=size;text.lineSpacing=9;
                bool replyEnd=p.id=="signature"||appendix!=null&&p.id==appendix.id;
                var notice=text.rectTransform.Find("AiContentNotice");
                if(replyEnd&&notice==null)notice=LingGuangV05.Runtime.AiContentNotice.CreateFooter(text.rectTransform,text.font,Muted).transform.parent;
                if(notice!=null){notice.gameObject.SetActive(replyEnd);notice.GetComponentInChildren<TMP_Text>(true).text=LingGuangV05.Runtime.AiContentNotice.Footer(english);}
                text.margin=new Vector4(0,0,0,replyEnd?28:0);
                text.fontStyle=p.id=="signature"?FontStyles.Italic:FontStyles.Normal;
            }
        }
        void ResizeText(int delta){FlushPosition();size=Mathf.Clamp(size+delta,16,26);RebuildParagraphs();RestorePosition();Render();}
        void ResetReadGate(){readToEnd=false;readingGesture=false;if(finish!=null)finish.interactable=false;}
        void RestorePosition(){ResetReadGate();restoring=true;restoreAt=Time.unscaledTime+.05f;}
        void ObserveReadingGeometry()
        {
            if(restoring||readToEnd||session?.View!=EndingLetterView.Reading||!session.Current||!Visible||!WindowVisible())return;
            bool gesture=readingGesture;readingGesture=false;
            if(viewport.rect.height<=1||content.rect.height<=1||paragraphs.Count==0)return;
            TMP_Text last=null;bool allVisible=true;
            foreach(var paragraph in paragraphs)
            {
                if(!paragraph.text.gameObject.activeInHierarchy)continue;
                last=paragraph.text;var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,last.rectTransform);
                if(bounds.min.y<viewport.rect.yMin-1||bounds.max.y>viewport.rect.yMax+1)allVisible=false;
            }
            if(last==null)return;
            var end=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,last.rectTransform);
            bool shortLetter=content.rect.height<=viewport.rect.height+1&&allVisible;
            bool scrolledEnd=gesture&&scroll.verticalNormalizedPosition<=.001f&&end.min.y>=viewport.rect.yMin-1&&end.min.y<=viewport.rect.yMax;
            if(!shortLetter&&!scrolledEnd)return;
            readToEnd=true;Render(); // Enables the explicit button only; never invokes Complete or an owner write.
        }
        string VisibleParagraph()
        {
            string result="greeting";float top=viewport.rect.yMax;
            foreach(var p in paragraphs)
            {
                if(!p.text.gameObject.activeSelf)continue;
                var b=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,p.text.rectTransform);
                if(b.max.y>=top-2)result=p.id;
                if(b.min.y<=top&&b.max.y>=top)return p.id;
            }
            return result;
        }
        void OnScrolled(Vector2 position)
        {
            if(restoring||session?.View!=EndingLetterView.Reading||!Visible)return;
            queuedPosition=VisibleParagraph();
            // Supplemental text resumes at the public signature; public paragraph IDs and save protocol remain stable.
            if(queuedPosition=="LP1"||queuedPosition=="LP2"||queuedPosition=="LP4")queuedPosition="signature";
            savePositionAt=Time.unscaledTime+.3f;
        }
        void FlushPosition()
        {
            if(queuedPosition.Length==0||session==null||session.Busy)return;
            if(session.SavePosition(queuedPosition)||session.Input?.paragraphId==queuedPosition||session.Input?.readOnly==true)queuedPosition="";
        }
        bool WindowVisible()
        {
            if(nativeWindow==null)return isActiveAndEnabled;
            if(!nativeWindow.isOn||!nativeWindow.gameObject.activeInHierarchy)return false;
            var group=nativeWindow.GetComponent<CanvasGroup>();return group==null||group.alpha>.01f;
        }
        void Update()
        {
            if(session==null||!Visible)return;
            if(!session.Current){session.Hide();return;}
            // Alpha can fall to zero while an already-open window restarts its native entry
            // animation. Actual close/minimize events own dismissal; opacity only gates updates.
            if(!WindowVisible())return;
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true&&session.PendingKind!="complete")
            {ClickFeedback();if(session.View==EndingLetterView.Consent)session.CancelOpen();else{FlushPosition();session.Back();}return;}
            if(restoring&&Time.unscaledTime>=restoreAt)
            {
                Canvas.ForceUpdateCanvases();scroll.StopMovement();scroll.verticalNormalizedPosition=1;
                var input=session.Input;var item=paragraphs.Find(p=>p.id==input.paragraphId);
                if(item.text!=null)
                {var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(content,item.text.rectTransform);float distance=content.rect.yMax-bounds.max.y;float hidden=content.rect.height-viewport.rect.height;if(hidden>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(1-distance/hidden);}
                restoring=false;
            }
            if(!restoring&&Time.unscaledTime>=savePositionAt)FlushPosition();
            ObserveReadingGeometry();
        }
        void ClickFeedback(){if(sound){XgJuice.Sfx.Init(gameObject);XgJuice.Sfx.Play(XgJuice.Sfx.Id.Click,1,.4f);}}
        void ApplyTransitions()
        {
            if(root==null)return;foreach(var b in root.GetComponentsInChildren<Button>(true))ApplyTransition(b);if(mailRow!=null)ApplyTransition(mailRow);
        }
        void ApplyTransition(Button b)
        {b.transition=Selectable.Transition.ColorTint;var c=b.colors;c.fadeDuration=reduceMotion?0:.08f;c.highlightedColor=new Color32(238,246,255,255);c.pressedColor=new Color32(165,198,232,255);b.colors=c;}
        void LayoutButtons()
        {
            if(root==null||back==null)return;
            // A single column keeps full English labels usable in a narrow reading pane.
            Place(save,0,1,102,148);foreach(var b in new[]{open,finish,continueButton})Place(b,0,1,52,98);
            Place(back,0,1,2,48);back.gameObject.SetActive(true);
            Place(smaller,0,.5f,100,134);Place(larger,.5f,1,100,134);
            var st=status.rectTransform;st.anchorMin=st.anchorMax=new Vector2(0,0);st.anchorMax=new Vector2(1,0);
            st.offsetMin=new Vector2(4,152);st.offsetMax=new Vector2(-4,192);
            Place(read,0,1,82,132);Place(later,0,1,24,74);
        }
        static void Place(Button b,float left,float right,float bottom,float top)
        {var r=(RectTransform)b.transform;r.anchorMin=new Vector2(left,0);r.anchorMax=new Vector2(right,0);r.offsetMin=new Vector2(4,bottom);r.offsetMax=new Vector2(-4,top);}
        void OnRectTransformDimensionsChange(){if(root!=null){LayoutButtons();if(Visible&&session?.View==EndingLetterView.Reading)RestorePosition();}}
        void OnNativeDismissed(){ResetReadGate();if(session==null)return;FlushPosition();session.Hide();}
        void OnDisable(){OnNativeDismissed();}
        void OnDestroy(){if(nativeWindow!=null){nativeWindow.onClose.RemoveListener(OnNativeDismissed);nativeWindow.onMinimize.RemoveListener(OnNativeDismissed);}if(session!=null)session.Changed-=OnSessionChanged;if(root!=null)Destroy(root.gameObject);}
        static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;return r;}
        static Image Fill(RectTransform r,Color color){var image=r.gameObject.AddComponent<Image>();image.color=color;return image;}
        TMP_Text Text(RectTransform r,float fontSize,Color color)
        {var text=r.gameObject.AddComponent<TextMeshProUGUI>();if(font!=null)text.font=font;text.fontSize=fontSize;text.color=color;text.richText=false;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Overflow;return text;}
    }
    // Shares the ScrollRect object so native wheel/drag dispatch still reaches ScrollRect itself.
    // Restoration and direct geometry changes do not grant the long-letter reading gate.
    public sealed class EndingLetterReadingGesture : MonoBehaviour,IScrollHandler,IDragHandler
    {
        public Action Scrolled;
        public void OnScroll(PointerEventData e){if(e.scrollDelta.sqrMagnitude>0)Scrolled?.Invoke();}
        public void OnDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&e.delta.sqrMagnitude>0)Scrolled?.Invoke();}
    }
    public sealed class EndingLetterOpenGesture : MonoBehaviour,IPointerClickHandler,ISubmitHandler
    {
        public Action Select,Open;
        public void OnPointerClick(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;if(e.clickCount>=2)Open?.Invoke();else Select?.Invoke();}
        public void OnSubmit(BaseEventData e)=>Open?.Invoke();
    }
}
