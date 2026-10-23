using System;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop
{
    [DefaultExecutionOrder(EscapeKey.SaveSlotsOrder)]
    public sealed class SaveSlotsPanel : MonoBehaviour
    {
        ChapterOneRuntime runtime;
        XgUi ui;
        RectTransform overlay, card;
        TMP_Text status;
        // System autosave + 20 manual slots + the new-save card.
        const int CardCount = ChapterOneRuntime.ManualArchiveLimit + 2;
        readonly TMP_Text[] descriptions = new TMP_Text[CardCount];
        readonly XgBtn[] loads = new XgBtn[CardCount], chapters = new XgBtn[6];
        int archiveSection, selectedSlot, pendingNewSlot;
        readonly int[] cardSlots=new int[CardCount];
        TMP_InputField archiveName;
        readonly ArchiveThumbnailView[] thumbnailViews=new ArchiveThumbnailView[CardCount];
        readonly GameObject[] autoBadges=new GameObject[CardCount];
        RectTransform cardContent; ScrollRect cardScroll;
        XgBtn loadCommand, renameCommand, deleteCommand, nameCommit;
        Action pending;
        XgBtn confirm;
        XgBtn settingsSave, settingsFunds;
        bool settingsEnglish;
        bool installed, tickBefore;
        RectTransform confirmationWindow, confirmationShield, saveProgress; float saveProgressUntil;
        float retry;
        public static bool IsOpen { get; private set; }
        static string T(string zh, string en) => GameText.T(zh, en);

        public static void Attach(ChapterOneRuntime runtime)
        {
            if (runtime != null && runtime.GetComponent<SaveSlotsPanel>() == null) runtime.gameObject.AddComponent<SaveSlotsPanel>();
        }
        void Awake() { runtime = GetComponent<ChapterOneRuntime>(); ChapterOneRuntime.ArchiveThumbnailProvider=CaptureDesktopThumbnail; }
        void OnDestroy() { IsOpen = false; }
        void Update()
        {
            if(saveProgress!=null){bool busy=Time.unscaledTime<saveProgressUntil;saveProgress.gameObject.SetActive(busy);if(busy)saveProgress.anchorMax=new Vector2(1-Mathf.Clamp01((saveProgressUntil-Time.unscaledTime)/.6f),1);}
            // Esc backs out one step: the confirmation, then a name being typed, then the panel.
            if (IsOpen && EscapeKey.TryConsume()) { if(confirmationWindow!=null&&confirmationWindow.gameObject.activeSelf){pending=null;ShowConfirmation(false);}else if(archiveName!=null&&archiveName.gameObject.activeSelf)CancelNaming();else Close(); }
            if (installed)
            {
                if (settingsEnglish != GameText.IsEnglish) RefreshSettingsLabels();
                return;
            }
            if (Time.unscaledTime < retry) return;
            retry = Time.unscaledTime + 1;
            var window = Array.Find(FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), w => w.name == "Settings");
            if (window?.windowContent == null) return;
            ui = new XgUi(PrologueDesk.CjkFont(), window);
            // Compact actions belong to the client area, below the native caption and its controls.
            var actions = Rect("GameActions", window.windowContent, Vector2.one, Vector2.one,
                new Vector2(-276, -40), new Vector2(-12, -12));
            settingsFunds = ui.Button(actions, "", () =>
            {
                var funds = runtime.GetComponent<FundsPanel>() ?? runtime.gameObject.AddComponent<FundsPanel>();
                funds.Open();
            }, 12);
            settingsFunds.rt.name = "FundsSettingsButton";
            settingsFunds.rt.anchorMin = Vector2.zero; settingsFunds.rt.anchorMax = new Vector2(0, 1);
            settingsFunds.rt.offsetMin = Vector2.zero; settingsFunds.rt.offsetMax = new Vector2(116, 0);
            settingsSave = ui.Button(actions, "", Open, 12);
            settingsSave.rt.name = "SaveSlotsSettingsButton";
            settingsSave.rt.anchorMin = new Vector2(1, 0); settingsSave.rt.anchorMax = Vector2.one;
            settingsSave.rt.offsetMin = new Vector2(-140, 0); settingsSave.rt.offsetMax = Vector2.zero;
            actions.SetAsLastSibling(); installed = true;
            RefreshSettingsLabels();
        }
        public static byte[] CaptureDesktopThumbnail()
        {
            // The game's LCD render texture excludes OS windows and our screen-space archive/settings overlays.
            if(!Application.isPlaying||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return null;
            var title=FindAnyObjectByType<CaseMenu.CaseMenuMainDisplay>();
            if(title!=null&&!title.HasEnteredDesktop)return null;
            var lcd=FindAnyObjectByType<HongmengOS.Aero2010.AeroLcdController>();
            var source=lcd!=null?lcd.display?.texture:null;
            if(source==null)return null;
            var target=RenderTexture.GetTemporary(384,216,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;Texture2D image=null;
            try {
                Graphics.Blit(source,target);RenderTexture.active=target;
                image=new Texture2D(384,216,TextureFormat.RGB24,false);image.ReadPixels(new UnityEngine.Rect(0,0,384,216),0,0);image.Apply();return image.EncodeToPNG();
            } finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(image!=null)Destroy(image);}
        }
        void RefreshSettingsLabels()
        {
            settingsEnglish = GameText.IsEnglish;
            settingsFunds.Set(T("资金账本", "Funds ledger"), true);
            settingsSave.Set(T("存档与章节", "Saves & chapters"), true);
        }
        void Build()
        {
            var canvasObject = new GameObject("SaveSlotsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 31000;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366, 900); scaler.matchWidthOrHeight = .5f;
            overlay = Rect("SaveSlots", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(overlay, Color.clear);
            card = Rect("Card", overlay, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-510, -360), new Vector2(510, 360));
            Panel(card, new Color32(181,207,230,255));
            Panel(At("Client",6,44,1008,670),Color.white);
            Text("Title",14,5,800,32,T("存档","Saves"),20);
            Button("Close",964,5,46,29,"×",Close);
            Panel(At("Address",16,52,988,35),new Color32(245,249,252,255));
            Text("AddressText",28,53,960,32,T("我的电脑  ›  本地磁盘 (C:)  ›  ","Computer  ›  Local Disk (C:)  ›  ")+(XingGuangController.Instance?.Sim?.AiDisplayName ?? runtime.Sim.S.aiName)+T("  ›  存档","  ›  Saves"),16);
            Panel(At("Commands",6,94,1008,44),new Color32(231,241,250,255));
            Button("SaveCurrent",18,100,145,30,T("保存当前进度","Save current"),SaveSelectedOrCurrent);
            Button("NewArchive",170,100,180,30,T("保存到新存档","Save to new slot"),CreateArchive);
            loadCommand=Button("LoadSelected",365,100,90,30,T("读取","Load"),()=>Request(T("读取选中存档？","Load selected save?"),()=>{if(runtime.LoadArchive(selectedSlot,archiveSection==2))Close();else{Refresh();status.text=runtime.SaveStatus;}}));
            renameCommand=Button("Rename",462,100,90,30,T("重命名","Rename"),()=>{ShowNameField(true);archiveName.text=runtime.DescribeArchiveCard(selectedSlot).name;archiveName.Select();archiveName.ActivateInputField();});
            deleteCommand=Button("Delete",559,100,90,30,T("删除","Delete"),()=>Request(T("删除选中存档？","Delete selected save?"),()=>{bool ok=runtime.RemoveManualArchive(selectedSlot);selectedSlot=0;Refresh();if(!ok)status.text=runtime.SaveStatus;}));
            Panel(At("Navigation",6,140,176,515),new Color32(240,246,251,255));
            Button("Manual",16,155,155,38,T("存档","Saves"),()=>{archiveSection=0;selectedSlot=0;Refresh();cardScroll.verticalNormalizedPosition=1;});
            Button("Chapters",16,204,155,38,T("章节回顾","Chapters"),()=>{archiveSection=2;selectedSlot=0;Refresh();cardScroll.verticalNormalizedPosition=1;});
            // Six rows of cards scroll inside the client area so the last row never covers the status line.
            var viewport=At("ArchiveViewport",192,146,816,506);Panel(viewport,Color.clear);viewport.gameObject.AddComponent<RectMask2D>();
            cardContent=Rect("ArchiveCards",viewport,new Vector2(0,1),Vector2.one,new Vector2(6,-506),Vector2.zero);
            cardScroll=viewport.gameObject.AddComponent<ScrollRect>();cardScroll.horizontal=false;cardScroll.movementType=ScrollRect.MovementType.Clamped;
            cardScroll.scrollSensitivity=30;cardScroll.viewport=viewport;cardScroll.content=cardContent;
            for(int i=0;i<CardCount;i++) {
                int row=i;float x=(i%4)*198,y=4+(i/4)*98;
                loads[i]=Button("ArchiveItem"+i,0,0,188,90,"",()=>{if(cardSlots[row]<0){CreateArchive();return;}selectedSlot=cardSlots[row];Refresh();});
                loads[i].rt.SetParent(cardContent,false);loads[i].rt.offsetMin=new Vector2(x,-y-90);loads[i].rt.offsetMax=new Vector2(x+188,-y);
                descriptions[i]=ui.Text(Rect("Name",loads[i].rt,Vector2.zero,Vector2.one,new Vector2(8,1),new Vector2(-8,-53)),"",14,new Color32(30,45,60,255),TextAlignmentOptions.Midline);
                descriptions[i].textWrappingMode=TextWrappingModes.Normal;
                var thumb=Rect("Thumbnail",loads[i].rt,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-42,-51),new Vector2(42,-4));
                thumb.gameObject.AddComponent<RawImage>();thumbnailViews[i]=thumb.gameObject.AddComponent<ArchiveThumbnailView>();
                var badge=Rect("AutoBadge",loads[i].rt,new Vector2(0,1),new Vector2(0,1),new Vector2(4,-20),new Vector2(42,-4));Panel(badge,new Color32(61,176,89,255)).raycastTarget=false;
                ui.Text(Rect("Text",badge,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),T("自动","Auto"),11,Color.white,TextAlignmentOptions.Midline);autoBadges[i]=badge.gameObject;
            }
            status=Text("Status",198,657,630,52,"",17);
            var track=At("SaveProgress",198,707,630,3);saveProgress=Rect("Fill",track,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Panel(saveProgress,new Color32(61,176,89,255));
            confirmationShield=Rect("ConfirmationShield",card,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Panel(confirmationShield,Color.clear);confirmationShield.gameObject.SetActive(false);
            confirmationWindow=At("SaveConfirmation",270,260,480,180);Panel(confirmationWindow,new Color32(217,232,245,255));
            var body=Rect("Body",confirmationWindow,Vector2.zero,Vector2.one,new Vector2(6,6),new Vector2(-6,-32));Panel(body,Color.white);
            var question=ui.Text(Rect("Question",body,Vector2.zero,Vector2.one,new Vector2(18,60),new Vector2(-18,-12)),"",20,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
            question.textWrappingMode=TextWrappingModes.Normal;
            confirm=ui.Button(body,T("确认","Confirm"),()=>{var action=pending;pending=null;ShowConfirmation(false);action?.Invoke();},16);confirm.rt.name="Confirm";
            confirm.rt.anchorMin=confirm.rt.anchorMax=new Vector2(1,0);confirm.rt.offsetMin=new Vector2(-220,14);confirm.rt.offsetMax=new Vector2(-120,48);
            var cancel=ui.Button(body,T("取消","Cancel"),()=>{pending=null;ShowConfirmation(false);},16);cancel.rt.name="CancelConfirmation";
            cancel.rt.anchorMin=cancel.rt.anchorMax=new Vector2(1,0);cancel.rt.offsetMin=new Vector2(-110,14);cancel.rt.offsetMax=new Vector2(-10,48);
            confirmationWindow.gameObject.SetActive(false);
            // The name field and its 保存 sit in the command bar, clear of the status line.
            var inputRoot=At("ArchiveName",660,100,250,30);Panel(inputRoot,Color.white);
            archiveName=inputRoot.gameObject.AddComponent<TMP_InputField>();
            var inputText=ui.Text(Rect("Text",inputRoot,Vector2.zero,Vector2.one,new Vector2(6,0),new Vector2(-6,0)),"",18,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
            archiveName.textViewport=inputRoot;archiveName.textComponent=inputText;archiveName.characterLimit=12;
            archiveName.onSubmit.AddListener(CommitArchiveName);
            archiveName.onEndEdit.AddListener(value=>{if(!archiveName.wasCanceled)CommitArchiveName(value);});
            nameCommit=Button("CommitArchiveName",916,100,88,30,T("保存","Save"),()=>CommitArchiveName(archiveName.text));
            ShowNameField(false);
        }
        void ShowNameField(bool show){archiveName.gameObject.SetActive(show);nameCommit.Show(show);}
        void CancelNaming(){pendingNewSlot=0;ShowNameField(false);status.text="";}
        void CommitArchiveName(string value)
        {
            // Enter, leaving the field and 保存 all commit; a hidden field ignores the end-edit its own closing sends.
            if(archiveName==null||!archiveName.gameObject.activeInHierarchy||string.IsNullOrWhiteSpace(value))return;
            if(pendingNewSlot>0){int slot=pendingNewSlot;if(!runtime.SaveSlot(slot)){status.text=runtime.SaveStatus;return;}saveProgressUntil=Time.unscaledTime+.6f;pendingNewSlot=0;selectedSlot=slot;}
            bool named=runtime.RenameManualArchive(selectedSlot,value);string failure=runtime.SaveStatus;ShowNameField(false);Refresh();if(!named)status.text=failure;
        }
        void SaveSelectedOrCurrent()
        {
            int slot=archiveSection==0&&selectedSlot>1?selectedSlot:1;
            Action save=()=>{bool ok=runtime.SaveSlot(slot);if(ok)saveProgressUntil=Time.unscaledTime+.6f;Refresh();if(!ok)status.text=runtime.SaveStatus;};
            if(slot>1)Request(T("覆盖选中存档？","Overwrite selected save?"),save);else save();
        }
        void CreateArchive()
        {
            int slot=runtime.FirstEmptyManualArchive();if(slot==0){status.text=T("存档位已满：请先删除一个手动存档。","All save slots are full. Delete a manual save first.");return;}
            archiveSection=0;selectedSlot=0;Refresh();pendingNewSlot=slot;ShowNameField(true);
            status.text=T("输入名称后按回车或点“保存”，当前进度会存到新存档。","Type a name, then press Enter or Save to store current progress in a new slot.");
            archiveName.text=T("存档 ","Save ")+(slot-1);archiveName.Select();archiveName.ActivateInputField();
        }

        TMP_Text Text(string name, float x, float y, float w, float h, string value, float size)
            => ui.Text(At(name, x, y, w, h), value, size, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
        RectTransform At(string name, float x, float y, float w, float h)
            => Rect(name, card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - h), new Vector2(x + w, -y));
        XgBtn Button(string name, float x, float y, float w, float h, string title, UnityEngine.Events.UnityAction action)
        {
            var b = ui.Button(card, title, action, 15); b.rt.name = name;
            b.rt.anchorMin = b.rt.anchorMax = new Vector2(0, 1); b.rt.offsetMin = new Vector2(x, -y - h); b.rt.offsetMax = new Vector2(x + w, -y);
            return b;
        }
        void ShowConfirmation(bool show){confirmationShield.gameObject.SetActive(show);confirmationWindow.gameObject.SetActive(show);if(show){confirmationShield.SetAsLastSibling();confirmationWindow.SetAsLastSibling();}}
        void Request(string text, Action action) { pending=action;ShowConfirmation(true);confirmationWindow.Find("Body/Question").GetComponent<TMP_Text>().text=text; }
        public void Open()
        {
            if(IsOpen){if(overlay!=null)overlay.SetAsLastSibling();return;}
            if(runtime==null)runtime=GetComponent<ChapterOneRuntime>();
            if (runtime?.Sim==null || runtime.Sim.InPrologue) return;
            // The Start menu can open saves before the Settings window's delayed install pass.
            if(ui==null)ui=new XgUi(PrologueDesk.CjkFont(),null);
            if (overlay == null) Build();
            tickBefore = runtime.tickEnabled; runtime.tickEnabled = false; IsOpen = true;
            overlay.gameObject.SetActive(true); Refresh();
        }
        void Refresh()
        {
            // A new-save name being typed survives a refresh; only a commit, Close or another 新建 ends it.
            bool naming=pendingNewSlot>0&&archiveName!=null&&archiveName.gameObject.activeSelf;
            pending = null; if(!naming)pendingNewSlot=0; ShowConfirmation(false); status.text = "";
            if(archiveName!=null&&!naming)ShowNameField(false);
            int count=0;
            // The system autosave (slot 1) comes first, then manual saves, then the new-save card.
            if(archiveSection==0){if(runtime.ArchiveExists(1))cardSlots[count++]=1;for(int slot=2;slot<=ChapterOneRuntime.ManualArchiveLimit+1;slot++)if(runtime.ArchiveExists(slot))cardSlots[count++]=slot; if(runtime.FirstEmptyManualArchive()>0)cardSlots[count++]=-1;}
            else for(int slot=1;slot<=6;slot++)cardSlots[count++]=slot;
            cardContent.offsetMin=new Vector2(6,-Mathf.Max(506,4+Mathf.CeilToInt(count/4f)*98));
            for(int i=0;i<CardCount;i++) {
                bool show=i<count;loads[i].Show(show);if(!show)continue;int slot=cardSlots[i];
                bool auto=archiveSection==0&&slot==1;autoBadges[i].SetActive(auto);
                if(slot<0){descriptions[i].text=T("＋ 保存当前进度到新存档","+ Save current progress to a new slot");thumbnailViews[i].gameObject.SetActive(false);loads[i].button.interactable=true;loads[i].button.image.color=new Color32(247,250,253,255);continue;}
                var info=runtime.DescribeArchiveCard(slot,archiveSection==2);thumbnailViews[i].gameObject.SetActive(true);
                descriptions[i].text=(archiveSection==2?T("第 "+slot+" 阶段","Stage "+slot):info.name)+"\n"+(info.exists?(auto?T("自动 · ","Auto · "):"")+info.savedAt:T("尚未解锁","Locked"));
                thumbnailViews[i].Bind(runtime,slot,archiveSection==2,info.revision);
                loads[i].button.interactable=info.exists;
                loads[i].button.image.color=selectedSlot==slot?new Color32(199,228,249,255):auto?new Color32(226,245,232,255):new Color32(247,250,253,255);
            }
            bool selected=selectedSlot>0&&runtime.ArchiveExists(selectedSlot,archiveSection==2);
            loadCommand.button.interactable=selected;
            // The system autosave can be loaded but not renamed or deleted.
            renameCommand.button.interactable=deleteCommand.button.interactable=selected&&archiveSection==0&&selectedSlot>1;
            if(selected){var info=runtime.DescribeArchiveCard(selectedSlot,archiveSection==2);status.text=info.name+T(" · 第 "+info.stage+" 阶段 · "," · Stage "+info.stage+" · ")+info.gameDate+" · "+info.savedAt
                +(archiveSection==0&&selectedSlot==1?T(" · 每五分钟自动保存"," · Saves automatically every five minutes"):"");}
        }

        void Close()
        {
            if (!IsOpen) return;
            if(archiveName!=null)ShowNameField(false);
            overlay.gameObject.SetActive(false); runtime.tickEnabled = tickBefore; IsOpen = false; pending = null;pendingNewSlot=0;
        }
    }
}
