using System;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LingGuangV05.CaseMenu
{
    public sealed partial class CaseMenuHud
    {
        CanvasGroup titleActions, archiveGroup;
        Button archiveConfirm, archiveNewGame, archiveCreate;
        TMP_Text archiveStatus;
        Func<ChapterOneRuntime> titleRuntime;
        Action archiveStart, quitRequested, prepareArchiveRuntime;
        int newGameConfirmEpoch;
        bool archiveLoaded, pendingArchiveOpen, newGameConfirmation;
        Button archiveClose;
        int selectedArchive;
        static string T(string zh,string en)=>GameText.T(zh,en);
        public bool IsArchiveOpen=>archiveGroup!=null&&archiveGroup.gameObject.activeSelf;
        public bool HasTitleSelection
        {
            get { var obj=EventSystem.current?.currentSelectedGameObject;return obj!=null&&titleActions!=null&&titleActions.interactable&&obj.transform.IsChildOf(titleActions.transform); }
        }
        static readonly string[] TitleEntryNames={"ReadArchives","EndingArchives","QuitGame"};
        public int TitleSelectionIndex { get {var selected=EventSystem.current?.currentSelectedGameObject;if(selected==null||!HasTitleSelection)return -1;return Array.IndexOf(TitleEntryNames,selected.name);} }
        public void SelectTitleEntry(int index){if(index>=0&&index<3&&titleActions!=null)EventSystem.current?.SetSelectedGameObject(titleActions.transform.Find(TitleEntryNames[index]).gameObject);}
        public void ConfigureTitleActions(Func<ChapterOneRuntime> runtime,Action start,Action quit,Action prepare=null)
        {titleRuntime=runtime;archiveStart=start;quitRequested=quit;prepareArchiveRuntime=prepare;}
        public bool BlocksTitlePointer(Vector2 screenPoint)
        {
            if (IsArchiveOpen || IsSettingsOpen) return true;
            if (titleActions == null || !titleActions.blocksRaycasts) return false;
            // Test this point against this HUD, not EventSystem's previous-frame/global
            // pointer hover (the preloaded desktop can have its own raycast surfaces).
            foreach (var button in titleActions.GetComponentsInChildren<Button>())
                if (button.isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform, screenPoint, null)) return true;
            return false;
        }
        void BuildTitleActions()
        {
            var root=Rect("TitleActions",canvasRoot);Stretch(root);
            titleActions=root.gameObject.AddComponent<CanvasGroup>();
            var read=ActionButton(root,"ReadArchives",T("存档","Saves"),new Vector2(-70,130),OpenArchives);
            var quit=ActionButton(root,"QuitGame",T("退出","Quit"),new Vector2(-310,130),()=>quitRequested?.Invoke());
            var endings=ActionButton(root,"EndingArchives",T("结局","Endings"),new Vector2(-550,130),OpenEndingArchives);
            int i=0;
            foreach(var b in new[]{read,endings,quit}) {
                Place((RectTransform)b.transform,Vector2.zero,new Vector2(128+i++*112,104),new Vector2(96,48));
                b.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
                var caption=b.GetComponentInChildren<TMP_Text>();caption.fontSize=28;caption.alignment=TextAlignmentOptions.MidlineLeft;caption.color=new Color32(128,126,119,255);
                b.transition=Selectable.Transition.None;
                var line=Panel("HoverUnderline",b.transform,Teal);Place(line.rectTransform,Vector2.zero,Vector2.zero,new Vector2(64,2));
                var feedback=b.gameObject.AddComponent<CaseMenuHoverMark>();feedback.mark=line.gameObject;line.gameObject.SetActive(false);
            }
            Link(read,quit,endings);Link(endings,read,quit);Link(quit,endings,read);
        }
        Button ActionButton(Transform parent,string name,string caption,Vector2 position,Action action)
        {
            var image=Panel(name,parent,Control,true);Place(image.rectTransform,new Vector2(1,0),position,new Vector2(220,46));
            var button=image.gameObject.AddComponent<Button>();Style(button,image);button.onClick.AddListener(()=>{uiClick?.Invoke();action();});
            var text=Label("Label",image.transform,caption,18,Ink);Stretch(text.rectTransform);text.alignment=TextAlignmentOptions.Midline;return button;
        }
        void SetTitleActionsVisible(bool visible)
        {
            if(titleActions==null)return;bool selected=HasTitleSelection;titleActions.alpha=visible?1:0;titleActions.interactable=visible;titleActions.blocksRaycasts=visible;
            if(!visible){CloseArchives();if(selected)EventSystem.current?.SetSelectedGameObject(null);}
        }
        public void RequestPowerOn()
        {
            if(titleActions==null||!titleActions.interactable||IsSettingsOpen||IsArchiveOpen)return;
            var rt=titleRuntime?.Invoke();
            if(rt==null){prepareArchiveRuntime?.Invoke();return;}
            if(pendingArchiveOpen){pendingArchiveOpen=false;OpenArchives();return;}
            if(!rt.HasAnyArchive){archiveStart?.Invoke();return;}
            OpenArchives();
        }
        public void OpenArchives()
        {
            if(titleActions==null||!titleActions.interactable||IsSettingsOpen)return;
            var rt=titleRuntime?.Invoke();
            if(rt==null){pendingArchiveOpen=true;prepareArchiveRuntime?.Invoke();return;}
            selectedArchive=0;archiveLoaded=false;newGameConfirmation=false;BuildArchivePage(rt);
        }
        void BuildArchivePage(ChapterOneRuntime rt)
        {
            SetSubpageChrome(true);
            endingViewEpoch++;
            if(archiveGroup!=null){archiveGroup.gameObject.SetActive(false);if(Application.isPlaying)Destroy(archiveGroup.gameObject);else DestroyImmediate(archiveGroup.gameObject);}
            var backdrop=Panel("TitleArchives",canvasRoot,new Color(0,0,0,.72f),true);Stretch(backdrop.rectTransform);
            archiveGroup=backdrop.gameObject.AddComponent<CanvasGroup>();
            var card=Panel("ArchiveCard",backdrop.transform,Color.clear,true);Place(card.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1480,820));
            var heading=Label("Heading",card.transform,T("存档","Saves"),52,Ink);Place(heading.rectTransform,new Vector2(0,1),new Vector2(30,-20),new Vector2(1200,70));
            heading.gameObject.AddComponent<CaseMenuPageCaret>();
            var intro=Label("Hint",card.transform,T("系统存档（自动）在最前，手动存档在后 · 选择进度，确认后开机。","System autosave first, then manual saves · Choose a session, then confirm to power on."),17,Muted);Place(intro.rectTransform,new Vector2(0,1),new Vector2(30,-72),new Vector2(840,35));
            intro.gameObject.SetActive(false);
            Button first=null,previous=null;
            var viewport=Panel("ArchiveViewport",card.transform,Color.clear,true).rectTransform;
            Place(viewport,new Vector2(0,1),new Vector2(30,-142),new Vector2(1420,460));viewport.gameObject.AddComponent<RectMask2D>();
            var scroller=viewport.gameObject.AddComponent<ScrollRect>();scroller.horizontal=false;scroller.movementType=ScrollRect.MovementType.Clamped;scroller.scrollSensitivity=30;
            var entries=Rect("ArchiveCards",viewport);Place(entries,new Vector2(0,1),Vector2.zero,new Vector2(1420,460));
            scroller.viewport=viewport;scroller.content=entries;
            // One list: the system autosave (disk slot 1) first, then the manual slots that exist.
            int visibleIndex=0;
            for(int rowIndex=0;rowIndex<=ChapterOneRuntime.ManualArchiveLimit;rowIndex++)
            {
                int slot=ChapterOneRuntime.ArchiveDisplaySlot(rowIndex);
                var info=rt.DescribeArchiveCard(slot);
                if(!info.exists&&!info.pendingNewGame)continue;
                int cardIndex=visibleIndex++;
                int choice=slot;
                var row=ActionButton(entries,"Archive"+slot,"",Vector2.zero,()=>SelectArchive(choice));
                Place((RectTransform)row.transform,new Vector2(0,1),new Vector2((cardIndex%4)*355,-(cardIndex/4)*226),new Vector2(330,200));
                row.GetComponent<Image>().color=info.autosave?new Color(Teal.r,Teal.g,Teal.b,.08f):new Color(1,1,1,.035f);
                row.interactable=info.exists;
                var thumbnail=Panel("Thumbnail",row.transform,new Color(.1f,.15f,.16f,.7f));
                Place(thumbnail.rectTransform,new Vector2(0,1),new Vector2(0,0),new Vector2(330,110));
                var art=Rect("ThumbnailImage",thumbnail.transform).gameObject.AddComponent<RawImage>();Stretch(art.rectTransform);
                art.gameObject.AddComponent<LingGuangV05.Desktop.ArchiveThumbnailView>().Bind(rt,slot);
                var name=Label("SaveName",row.transform,info.name,26,Ink);
                Place(name.rectTransform,new Vector2(0,1),new Vector2(0,-120),new Vector2(330,35));
                string when=info.pendingNewGame?info.savedAt:info.autosave?T("自动保存 · ","Autosaved · ")+info.savedAt:info.savedAt;
                var date=Label("SaveInfo",row.transform,when,18,info.pendingNewGame?Amber:Muted);Place(date.rectTransform,new Vector2(0,1),new Vector2(0,-160),new Vector2(330,28));
                if(info.autosave){var badge=Panel("AutoBadge",thumbnail.transform,new Color(Teal.r,Teal.g,Teal.b,.9f));Place(badge.rectTransform,new Vector2(0,1),new Vector2(8,-8),new Vector2(64,24));
                    var badgeText=Label("Text",badge.transform,T("自动","Auto"),15,new Color(.06f,.09f,.09f,1));Stretch(badgeText.rectTransform);badgeText.alignment=TextAlignmentOptions.Midline;}
                var stage=Panel("Stage",thumbnail.transform,new Color(0,0,0,.62f));Place(stage.rectTransform,Vector2.zero,new Vector2(8,6),new Vector2(122,18));
                for(int s=0;s<6;s++){var cell=Panel("StageCell"+s,stage.transform,s<info.stage?Teal:new Color(.32f,.38f,.36f,1));Place(cell.rectTransform,Vector2.zero,new Vector2(6+s*19,5),new Vector2(15,8));}
                var outline=row.gameObject.AddComponent<Outline>();outline.effectColor=Teal;outline.effectDistance=new Vector2(2,-2);outline.enabled=false;
                if(info.exists){
                    var actions=Panel("CardActions",thumbnail.transform,new Color(0,0,0,.72f),true).rectTransform;Stretch(actions);
                    CardAction(actions,"ReadCard",T("读取","Load"),0,()=>{SelectArchive(choice);LoadSelected();},false);
                    if(slot>1){CardAction(actions,"SaveCard",T("保存","Save"),1,()=>{if(rt.SaveSlot(choice))BuildArchivePage(rt);else archiveStatus.text=rt.SaveStatus;},true);
                        CardAction(actions,"DeleteCard",T("删除","Delete"),2,()=>{if(rt.RemoveManualArchive(choice)){selectedArchive=0;BuildArchivePage(rt);}else archiveStatus.text=rt.SaveStatus;},true);}
                    var hover=row.gameObject.AddComponent<CaseMenuHoverMark>();hover.mark=actions.gameObject;actions.gameObject.SetActive(false);
                }
                if(first==null)first=row;if(previous!=null)Link(previous,previous.navigation.selectOnUp,row);previous=row;
            }
            // On the title a new save is a new game: the same confirmed flow as 开始新游戏 (manual saves are kept).
            var add=archiveCreate=ActionButton(entries,"CreateSave",T("＋ 新建存档","＋ New save"),Vector2.zero,StartNewGameFromArchives);
            Place((RectTransform)add.transform,new Vector2(0,1),new Vector2((visibleIndex%4)*355,-(visibleIndex/4)*226),new Vector2(330,200));visibleIndex++;
            var addHint=Label("CreateSaveHint",add.transform,T("开始新游戏 · 旧存档保留","Starts a new game · Old saves kept"),17,Muted);
            Place(addHint.rectTransform,new Vector2(.5f,0),new Vector2(0,22),new Vector2(310,28));addHint.alignment=TextAlignmentOptions.Midline;
            entries.sizeDelta=new Vector2(1420,Mathf.Max(460,Mathf.CeilToInt(visibleIndex/4f)*226));
            var activeTab=Label("ActiveArchiveTab",card.transform,T("全部","All"),26,Ink);Place(activeTab.rectTransform,new Vector2(0,1),new Vector2(30,-100),new Vector2(160,38));
            var activeLine=Panel("ActiveTabUnderline",card.transform,Teal);Place(activeLine.rectTransform,new Vector2(0,1),new Vector2(30,-137),new Vector2(80,3));
            archiveStatus=Label("Status",card.transform,"",17,Muted);Place(archiveStatus.rectTransform,new Vector2(0,0),new Vector2(30,83),new Vector2(840,52));archiveStatus.textWrappingMode=TextWrappingModes.Normal;
            archiveConfirm=ActionButton(card.transform,"LoadSelected",T("读取并开机","Load and power on"),new Vector2(-30,25),LoadSelected);archiveConfirm.interactable=selectedArchive>0&&!archiveLoaded;
            var close=archiveClose=ActionButton(card.transform,"CloseArchives",archiveLoaded?T("开机","Power on"):T("返回","Back"),new Vector2(-270,25),CloseArchiveByPlayer);
            archiveNewGame=ActionButton(card.transform,"StartNewGame",T("开始新游戏","Start new game"),new Vector2(-510,25),StartNewGameFromArchives);
            Place((RectTransform)close.transform,Vector2.zero,new Vector2(30,25),new Vector2(200,48));close.GetComponent<Image>().color=Color.clear;
            Place((RectTransform)archiveConfirm.transform,Vector2.zero,new Vector2(250,25),new Vector2(200,48));archiveConfirm.GetComponent<Image>().color=Color.clear;
            Place((RectTransform)archiveNewGame.transform,new Vector2(1,1),new Vector2(-30,-96),new Vector2(260,44));archiveNewGame.GetComponent<Image>().color=Color.clear;
            if(previous!=null)Link(previous,previous.navigation.selectOnUp,archiveConfirm);Link(archiveConfirm,previous,close);Link(close,archiveConfirm,archiveNewGame);Link(archiveNewGame,close,first);if(first!=null){var n=first.navigation;n.selectOnUp=archiveNewGame;first.navigation=n;}
            newGameConfirmation=false;newGameConfirmEpoch++;RefreshArchiveSelection();EventSystem.current?.SetSelectedGameObject(close.gameObject);
            if(Application.isPlaying && !GameSettings.ReducedMotion)StartCoroutine(AnimateArchive(archiveGroup,card.rectTransform));
        }
        void CardAction(RectTransform parent,string name,string caption,int index,Action action,bool confirm)
        {
            var button=ActionButton(parent,name,caption,Vector2.zero,()=>{});
            Place((RectTransform)button.transform,new Vector2(0,.5f),new Vector2(8+index*106,0),new Vector2(100,48));
            button.GetComponent<Image>().color=Color.clear;button.GetComponentInChildren<TMP_Text>().fontSize=22;
            button.onClick.RemoveAllListeners();
            if(confirm)button.gameObject.AddComponent<CaseMenuInlineConfirm>().Bind(button,caption,caption+"？",()=>{uiClick?.Invoke();action();});
            else button.onClick.AddListener(()=>{uiClick?.Invoke();action();});
        }
        void SelectArchive(int slot)
        {selectedArchive=slot;archiveLoaded=false;newGameConfirmation=false;RefreshArchiveSelection();}
        void RefreshArchiveSelection()
        {
            if(archiveStatus==null)return;
            archiveStatus.text=newGameConfirmation?T("再点一次确认开始新游戏：旧的系统存档会备份，手动存档保留。","Click again to start a new game: the old autosave is backed up, manual saves are kept."):archiveLoaded?T("已读取","Loaded"):"";
            archiveNewGame.GetComponentInChildren<TMP_Text>().text=newGameConfirmation?T("确定重来？","Start over?"):T("开始新游戏","Start new game");
            if(archiveCreate!=null)archiveCreate.GetComponentInChildren<TMP_Text>().text=newGameConfirmation?T("确定重来？","Start over?"):T("＋ 新建存档","＋ New save");
            archiveConfirm.interactable=selectedArchive>0&&!archiveLoaded;
            archiveConfirm.GetComponentInChildren<TMP_Text>().text=T("读取","Load");
            archiveClose.GetComponentInChildren<TMP_Text>().color=archiveLoaded?Amber:Ink;
            archiveClose.GetComponentInChildren<TMP_Text>().text=archiveLoaded?T("开机","Power on"):T("返回","Back");
            foreach(var button in archiveGroup.GetComponentsInChildren<Button>())
                if(button.name.StartsWith("Archive")&&int.TryParse(button.name.Substring(7),out int slot))
                {var outline=button.GetComponent<Outline>();if(outline!=null)outline.enabled=slot==selectedArchive; if(archiveLoaded&&slot==selectedArchive){var info=button.transform.Find("SaveInfo")?.GetComponent<TMP_Text>();if(info!=null)info.text=T("已读取","Loaded");}}
        }
        System.Collections.IEnumerator AnimateArchive(CanvasGroup group,RectTransform card)
        {
            float time=0;
            while(group!=null&&group.gameObject.activeSelf&&time<.18f)
            {
                time+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(time/.18f);
                group.alpha=t;card.localScale=Vector3.one*Mathf.Lerp(.97f,1f,1-Mathf.Pow(1-t,3));yield return null;
            }
            if(group!=null){group.alpha=1;card.localScale=Vector3.one;}
        }
        void LoadSelected()
        {
            var rt=titleRuntime?.Invoke();if(rt==null||selectedArchive==0)return;
            newGameConfirmation=false;
            try{if(!rt.LoadArchive(selectedArchive)){archiveStatus.text=rt.SaveStatus;return;}archiveLoaded=true;RefreshArchiveSelection();}
            catch(Exception e){archiveStatus.text=T("读取失败：","Load failed: ")+e.Message;}
        }
        void StartNewGameFromArchives()
        {
            var rt=titleRuntime?.Invoke();if(rt==null)return;
            if(rt.HasAnyArchive&&!newGameConfirmation)
            {
                newGameConfirmation=true;int epoch=++newGameConfirmEpoch;RefreshArchiveSelection();
                if(Application.isPlaying)StartCoroutine(ExpireNewGameConfirmation(epoch,EventSystem.current?.currentSelectedGameObject));
                return;
            }
            // ResetProgress archives the primary and its backup before replacing the session.
            // Manual slots and ending/chapter archives are untouched; backup failure aborts.
            try
            {
                if(!rt.ResetProgress(true)){archiveStatus.text=rt.SaveStatus;return;}
                archiveLoaded=true;newGameConfirmation=false;
                CloseArchiveByPlayer();
            }
            catch(Exception e){archiveStatus.text=T("无法开始新游戏：","Could not start a new game: ")+e.Message;}
        }
        // Moving the pointer off the button keeps the question; clicking elsewhere or four seconds cancel it.
        System.Collections.IEnumerator ExpireNewGameConfirmation(int epoch,GameObject armedOn)
        {
            float until=Time.unscaledTime+4f;
            while(epoch==newGameConfirmEpoch&&newGameConfirmation&&IsArchiveOpen)
            {
                var selected=EventSystem.current?.currentSelectedGameObject;
                bool elsewhere=selected!=armedOn&&!(selected!=null&&(selected==archiveNewGame?.gameObject||selected==archiveCreate?.gameObject));
                if(elsewhere||Time.unscaledTime>=until){newGameConfirmation=false;RefreshArchiveSelection();yield break;}
                yield return null;
            }
        }
        System.Collections.IEnumerator StartAfterArchiveClosed()
        {
            // Let the removed modal leave the rendered frame before starting the existing boot animation.
            yield return null;
            if(!IsArchiveOpen && !IsSettingsOpen)archiveStart?.Invoke();
        }
        public void CloseArchiveByPlayer()
        {
            bool start=archiveLoaded;CloseArchives();
            if(start){if(Application.isPlaying)StartCoroutine(StartAfterArchiveClosed());else archiveStart?.Invoke();}
        }
        public void CloseArchives()
        {endingViewEpoch++;if(archiveGroup!=null)archiveGroup.gameObject.SetActive(false);SetSubpageChrome(IsSettingsOpen);selectedArchive=0;archiveLoaded=false;EventSystem.current?.SetSelectedGameObject(null);}
    }
}
