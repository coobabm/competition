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
        Button archiveConfirm, archiveNewGame;
        TMP_Text archiveStatus;
        Func<ChapterOneRuntime> titleRuntime;
        Action archiveStart, quitRequested, prepareArchiveRuntime;
        int archivePage;
        bool archiveLoaded, pendingArchiveOpen, newGameConfirmation;
        Button archiveClose;
        int selectedArchive;
        static string T(string zh,string en)=>GameText.T(zh,en);
        public bool IsArchiveOpen=>archiveGroup!=null&&archiveGroup.gameObject.activeSelf;
        public bool HasTitleSelection
        {
            get { var obj=EventSystem.current?.currentSelectedGameObject;return obj!=null&&titleActions!=null&&titleActions.interactable&&obj.transform.IsChildOf(titleActions.transform); }
        }
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
            var read=ActionButton(root,"ReadArchives",T("读取存档 [L]","Load game [L]"),new Vector2(-70,130),OpenArchives);
            var quit=ActionButton(root,"QuitGame",T("退出游戏 [Q]","Quit game [Q]"),new Vector2(-310,130),()=>quitRequested?.Invoke());
            var endings=ActionButton(root,"EndingArchives",T("结局档案","Endings"),new Vector2(-550,130),OpenEndingArchives);
            Link(read,endings,quit);Link(quit,read,endings);Link(endings,quit,read);
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
            archivePage=0;selectedArchive=0;archiveLoaded=false;newGameConfirmation=false;BuildArchivePage(rt);
        }
        void BuildArchivePage(ChapterOneRuntime rt)
        {
            endingViewEpoch++;
            if(archiveGroup!=null){archiveGroup.gameObject.SetActive(false);if(Application.isPlaying)Destroy(archiveGroup.gameObject);else DestroyImmediate(archiveGroup.gameObject);}
            var backdrop=Panel("TitleArchives",canvasRoot,new Color(0,0,0,.86f),true);Stretch(backdrop.rectTransform);
            archiveGroup=backdrop.gameObject.AddComponent<CanvasGroup>();
            var card=Panel("ArchiveCard",backdrop.transform,Surface,true);Place(card.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(900,690));
            var heading=Label("Heading",card.transform,T("选择存档","Choose a saved session"),28,Ink);Place(heading.rectTransform,new Vector2(0,1),new Vector2(30,-20),new Vector2(840,45));
            var intro=Label("Hint",card.transform,archivePage==0?T("手动存档 · 选择进度，确认后开机。","Manual saves · Choose a session, then confirm to power on."):T("系统自动存档 · 每五分钟保存一次。","System autosave · Saved every five minutes."),17,Muted);Place(intro.rectTransform,new Vector2(0,1),new Vector2(30,-72),new Vector2(840,35));
            Button first=null,previous=null;
            int count=archivePage==0?5:1;
            for(int rowIndex=0;rowIndex<count;rowIndex++)
            {
                int slot=archivePage==0?ChapterOneRuntime.ArchiveDisplaySlot(rowIndex):1;
                int choice=slot;string description;
                try{description=rt.ArchiveDescription(slot);}catch(Exception){description=T("无法读取","Unreadable save");}
                var row=ActionButton(card.transform,"Archive"+slot,(slot==1?T("自动存档","Autosave"):T("手动存档 ","Manual slot ")+(slot-1))+" · "+description,new Vector2(-30,498-rowIndex*62),()=>SelectArchive(choice));
                row.GetComponent<RectTransform>().sizeDelta=new Vector2(840,52);row.interactable=rt.ArchiveExists(slot);
                if(first==null)first=row;if(previous!=null)Link(previous,previous.navigation.selectOnUp,row);previous=row;
            }
            var page=ActionButton(card.transform,"ArchivePage",archivePage==0?T("系统自动存档 ›","System autosave ›"):T("‹ 手动存档","‹ Manual saves"),new Vector2(-30,150),()=>{if(!archiveLoaded)selectedArchive=0;archivePage=1-archivePage;BuildArchivePage(rt);});
            var pageNumber=Label("PageNumber",card.transform,(archivePage+1)+" / 2",15,Muted);Place(pageNumber.rectTransform,new Vector2(0,0),new Vector2(30,155),new Vector2(180,35));
            archiveStatus=Label("Status",card.transform,"",17,Muted);Place(archiveStatus.rectTransform,new Vector2(0,0),new Vector2(30,83),new Vector2(840,52));archiveStatus.textWrappingMode=TextWrappingModes.Normal;
            archiveConfirm=ActionButton(card.transform,"LoadSelected",T("读取并开机","Load and power on"),new Vector2(-30,25),LoadSelected);archiveConfirm.interactable=selectedArchive>0&&!archiveLoaded;
            var close=archiveClose=ActionButton(card.transform,"CloseArchives",archiveLoaded?T("关闭并开机","Close and power on"):T("返回","Back"),new Vector2(-270,25),CloseArchiveByPlayer);
            archiveNewGame=ActionButton(card.transform,"StartNewGame",T("开始新游戏","Start new game"),new Vector2(-510,25),StartNewGameFromArchives);
            if(previous!=null)Link(previous,previous.navigation.selectOnUp,page);Link(page,previous,archiveConfirm);Link(archiveConfirm,previous,close);Link(close,archiveConfirm,archiveNewGame);Link(archiveNewGame,close,first);if(first!=null){var n=first.navigation;n.selectOnUp=archiveNewGame;first.navigation=n;}
            RefreshArchiveSelection();EventSystem.current?.SetSelectedGameObject(close.gameObject);
            if(Application.isPlaying && !GameSettings.ReducedMotion)StartCoroutine(AnimateArchive(archiveGroup,card.rectTransform));
        }
        void SelectArchive(int slot)
        {selectedArchive=slot;archiveLoaded=false;newGameConfirmation=false;RefreshArchiveSelection();}
        void RefreshArchiveSelection()
        {
            if(archiveStatus==null)return;
            archiveStatus.text=newGameConfirmation?T("将重新播放开场。已有手动存档保留，当前自动存档留作备份。再次点击确认。","Replay the opening. Manual saves are retained; the current autosave is backed up. Click again to confirm."):archiveLoaded?T("已读取。关闭此面板后开机。","Loaded. Close this panel to power on."):
                selectedArchive>0?T("已选择进度，点击“读取”确认。当前主档保留备份。","Session selected. Confirm Load; the current primary keeps a backup."):
                (titleRuntime?.Invoke()?.HasAnyArchive ?? false)?T("请选择存档；系统自动档在最后一页。","Choose a save. The system autosave is on the last page."):
                T("还没有存档。点击“开始新游戏”进入。","No saves yet. Click Start new game to begin.");
            archiveNewGame.GetComponentInChildren<TMP_Text>().text=newGameConfirmation?T("确认开始新游戏","Confirm new game"):T("开始新游戏","Start new game");
            archiveConfirm.interactable=selectedArchive>0&&!archiveLoaded;
            archiveConfirm.GetComponentInChildren<TMP_Text>().text=T("读取","Load");
            archiveClose.GetComponentInChildren<TMP_Text>().text=archiveLoaded?T("关闭并开机","Close and power on"):T("返回","Back");
            foreach(var button in archiveGroup.GetComponentsInChildren<Button>())
                if(button.name.StartsWith("Archive")&&int.TryParse(button.name.Substring(7),out int slot))
                {var colors=button.colors;colors.normalColor=slot==selectedArchive?new Color(.55f,.8f,1f):Color.white;button.colors=colors;}
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
            if(rt.HasAnyArchive&&!newGameConfirmation){newGameConfirmation=true;RefreshArchiveSelection();return;}
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
        {endingViewEpoch++;if(archiveGroup!=null)archiveGroup.gameObject.SetActive(false);selectedArchive=0;archiveLoaded=false;EventSystem.current?.SetSelectedGameObject(null);}
    }
}
