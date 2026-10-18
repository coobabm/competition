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
        Button archiveConfirm;
        TMP_Text archiveStatus;
        Func<ChapterOneRuntime> titleRuntime;
        Action archiveStart, quitRequested;
        int selectedArchive;
        static string T(string zh,string en)=>GameText.T(zh,en);
        public bool IsArchiveOpen=>archiveGroup!=null&&archiveGroup.gameObject.activeSelf;
        public bool HasTitleSelection
        {
            get { var obj=EventSystem.current?.currentSelectedGameObject;return obj!=null&&titleActions!=null&&titleActions.interactable&&obj.transform.IsChildOf(titleActions.transform); }
        }
        public void ConfigureTitleActions(Func<ChapterOneRuntime> runtime,Action start,Action quit)
        {titleRuntime=runtime;archiveStart=start;quitRequested=quit;}
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
            Link(read,quit,quit);Link(quit,read,read);
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
        public void OpenArchives()
        {
            if(titleActions==null||!titleActions.interactable||IsSettingsOpen)return;
            var rt=titleRuntime?.Invoke();
            if(rt==null){ShowError(T("桌面正在准备，请稍后读取存档。","The desktop is preparing. Please try loading shortly."));return;}
            if(archiveGroup!=null){archiveGroup.gameObject.SetActive(false);if(Application.isPlaying)Destroy(archiveGroup.gameObject);else DestroyImmediate(archiveGroup.gameObject);}
            var backdrop=Panel("TitleArchives",canvasRoot,new Color(0,0,0,.86f),true);Stretch(backdrop.rectTransform);
            archiveGroup=backdrop.gameObject.AddComponent<CanvasGroup>();
            var card=Panel("ArchiveCard",backdrop.transform,Surface,true);Place(card.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(900,690));
            var heading=Label("Heading",card.transform,T("读取存档","Load game"),28,Ink);Place(heading.rectTransform,new Vector2(0,1),new Vector2(30,-20),new Vector2(840,45));
            var intro=Label("Hint",card.transform,T("读取所选进度，然后开机。自动存档在第 1 档。","Load a saved session, then power on. Slot 1 is the autosave."),17,Muted);Place(intro.rectTransform,new Vector2(0,1),new Vector2(30,-72),new Vector2(840,35));
            Button first=null,previous=null;
            for(int slot=1;slot<=6;slot++)
            {
                int choice=slot;string description;
                try{description=rt.ArchiveDescription(slot);}catch(Exception){description=T("无法读取","Unreadable save");}
                var row=ActionButton(card.transform,"Archive"+slot,slot+" · "+description,new Vector2(-30,560-slot*62),()=>SelectArchive(choice));
                row.GetComponent<RectTransform>().sizeDelta=new Vector2(840,52);row.interactable=rt.ArchiveExists(slot);
                if(first==null)first=row;if(previous!=null)Link(previous,previous.navigation.selectOnUp,row);previous=row;
            }
            archiveStatus=Label("Status",card.transform,"",17,Muted);Place(archiveStatus.rectTransform,new Vector2(0,0),new Vector2(30,83),new Vector2(840,72));archiveStatus.textWrappingMode=TextWrappingModes.Normal;
            archiveConfirm=ActionButton(card.transform,"LoadSelected",T("读取并开机","Load and power on"),new Vector2(-30,25),LoadSelected);archiveConfirm.interactable=false;
            var close=ActionButton(card.transform,"CloseArchives",T("返回","Back"),new Vector2(-270,25),CloseArchives);
            if(previous!=null)Link(previous,previous.navigation.selectOnUp,archiveConfirm);Link(archiveConfirm,previous,close);Link(close,archiveConfirm,first);if(first!=null){var n=first.navigation;n.selectOnUp=close;first.navigation=n;}
            selectedArchive=0;EventSystem.current?.SetSelectedGameObject(close.gameObject);
        }
        void SelectArchive(int slot)
        {selectedArchive=slot;archiveConfirm.interactable=true;archiveStatus.text=T("读取第 "+slot+" 档？当前主档会保留一份备份。","Load slot "+slot+"? A backup of the current primary save will be kept.");}
        void LoadSelected()
        {
            var rt=titleRuntime?.Invoke();if(rt==null||selectedArchive==0)return;
            try{if(!rt.LoadArchive(selectedArchive)){archiveStatus.text=rt.SaveStatus;return;}CloseArchives();archiveStart?.Invoke();}
            catch(Exception e){archiveStatus.text=T("读取失败：","Load failed: ")+e.Message;}
        }
        public void CloseArchives()
        {if(archiveGroup!=null)archiveGroup.gameObject.SetActive(false);selectedArchive=0;EventSystem.current?.SetSelectedGameObject(null);}
    }
}
