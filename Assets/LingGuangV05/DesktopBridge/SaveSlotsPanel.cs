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
    public sealed class SaveSlotsPanel : MonoBehaviour
    {
        ChapterOneRuntime runtime;
        XgUi ui;
        RectTransform overlay, card;
        TMP_Text status;
        readonly TMP_Text[] descriptions = new TMP_Text[20];
        readonly XgBtn[] loads = new XgBtn[20], chapters = new XgBtn[6];
        int archiveSection, selectedSlot, pendingNewSlot;
        readonly int[] cardSlots=new int[20];
        TMP_InputField archiveName;
        readonly ArchiveThumbnailView[] thumbnailViews=new ArchiveThumbnailView[20];
        XgBtn loadCommand, renameCommand, deleteCommand;
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
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { if(confirmationWindow!=null&&confirmationWindow.gameObject.activeSelf){pending=null;ShowConfirmation(false);}else Close(); }
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
            Text("AddressText",28,53,960,32,T("我的电脑  ›  本地磁盘 (C:)  ›  ","Computer  ›  Local Disk (C:)  ›  ")+(XingGuangController.Instance?.Sim?.AiDisplayName ?? runtime.Sim.S.aiName)+T("  ›  手动存档","  ›  Manual saves"),16);
            Panel(At("Commands",6,94,1008,44),new Color32(231,241,250,255));
            Button("SaveCurrent",18,100,145,30,T("保存当前进度","Save current"),SaveSelectedOrCurrent);
            Button("NewArchive",170,100,115,30,T("新建存档","New save"),CreateArchive);
            loadCommand=Button("LoadSelected",365,100,90,30,T("读取","Load"),()=>Request(T("读取选中存档？","Load selected save?"),()=>{if(runtime.LoadArchive(selectedSlot,archiveSection==2))Close();else{Refresh();status.text=runtime.SaveStatus;}}));
            renameCommand=Button("Rename",462,100,90,30,T("重命名","Rename"),()=>{archiveName.gameObject.SetActive(true);archiveName.text=runtime.DescribeArchiveCard(selectedSlot).name;archiveName.Select();archiveName.ActivateInputField();});
            deleteCommand=Button("Delete",559,100,90,30,T("删除","Delete"),()=>Request(T("删除选中存档？","Delete selected save?"),()=>{bool ok=runtime.RemoveManualArchive(selectedSlot);selectedSlot=0;Refresh();if(!ok)status.text=runtime.SaveStatus;}));
            Panel(At("Navigation",6,140,176,515),new Color32(240,246,251,255));
            Button("Manual",16,155,155,38,T("手动存档","Manual saves"),()=>{archiveSection=0;selectedSlot=0;Refresh();});
            Button("Automatic",16,204,155,38,T("自动存档","Autosave"),()=>{archiveSection=1;selectedSlot=0;Refresh();});
            Button("Chapters",16,253,155,38,T("章节回顾","Chapters"),()=>{archiveSection=2;selectedSlot=0;Refresh();});
            for(int i=0;i<20;i++) {
                int row=i;float x=198+(i%4)*198,y=150+(i/4)*98;
                loads[i]=Button("ArchiveItem"+i,x,y,188,90,"",()=>{if(cardSlots[row]<0){CreateArchive();return;}selectedSlot=cardSlots[row];Refresh();});
                descriptions[i]=ui.Text(Rect("Name",loads[i].rt,Vector2.zero,Vector2.one,new Vector2(8,1),new Vector2(-8,-53)),"",14,new Color32(30,45,60,255),TextAlignmentOptions.Midline);
                descriptions[i].textWrappingMode=TextWrappingModes.Normal;
                var thumb=Rect("Thumbnail",loads[i].rt,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-42,-51),new Vector2(42,-4));
                thumb.gameObject.AddComponent<RawImage>();thumbnailViews[i]=thumb.gameObject.AddComponent<ArchiveThumbnailView>();
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
            var inputRoot=At("ArchiveName",198,657,400,38);Panel(inputRoot,Color.white);
            archiveName=inputRoot.gameObject.AddComponent<TMP_InputField>();
            var inputText=ui.Text(Rect("Text",inputRoot,Vector2.zero,Vector2.one,new Vector2(6,0),new Vector2(-6,0)),"",18,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
            archiveName.textViewport=inputRoot;archiveName.textComponent=inputText;archiveName.characterLimit=12;
            archiveName.onSubmit.AddListener(value=>{
                if(string.IsNullOrWhiteSpace(value))return;
                if(pendingNewSlot>0){int slot=pendingNewSlot;if(!runtime.SaveSlot(slot)){status.text=runtime.SaveStatus;return;}saveProgressUntil=Time.unscaledTime+.6f;pendingNewSlot=0;selectedSlot=slot;}
                bool named=runtime.RenameManualArchive(selectedSlot,value);string failure=runtime.SaveStatus;archiveName.gameObject.SetActive(false);Refresh();if(!named)status.text=failure;
            });
            archiveName.gameObject.SetActive(false);
        }
        void SaveSelectedOrCurrent()
        {
            int slot=archiveSection==0&&selectedSlot>1?selectedSlot:1;
            Action save=()=>{bool ok=runtime.SaveSlot(slot);if(ok)saveProgressUntil=Time.unscaledTime+.6f;Refresh();if(!ok)status.text=runtime.SaveStatus;};
            if(slot>1)Request(T("覆盖选中存档？","Overwrite selected save?"),save);else save();
        }
        void CreateArchive()
        {
            int slot=runtime.FirstEmptyManualArchive();if(slot==0)return;
            archiveSection=0;selectedSlot=0;Refresh();pendingNewSlot=slot;archiveName.gameObject.SetActive(true);
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
            pending = null;pendingNewSlot=0; ShowConfirmation(false); status.text = "";
            if(archiveName!=null)archiveName.gameObject.SetActive(false);
            int count=0;
            if(archiveSection==0){for(int slot=2;slot<=ChapterOneRuntime.ManualArchiveLimit+1;slot++)if(runtime.ArchiveExists(slot))cardSlots[count++]=slot; if(count<20)cardSlots[count++]=-1;}
            else if(archiveSection==1)cardSlots[count++]=1;
            else for(int slot=1;slot<=6;slot++)cardSlots[count++]=slot;
            for(int i=0;i<20;i++) {
                bool show=i<count;loads[i].Show(show);if(!show)continue;int slot=cardSlots[i];
                if(slot<0){descriptions[i].text=T("＋ 新建存档","＋ New save");thumbnailViews[i].gameObject.SetActive(false);loads[i].button.interactable=true;continue;}
                var info=runtime.DescribeArchiveCard(slot,archiveSection==2);thumbnailViews[i].gameObject.SetActive(true);
                descriptions[i].text=(archiveSection==2?T("第 "+slot+" 阶段","Stage "+slot):info.name)+"\n"+(info.exists?info.savedAt:T("尚未解锁","Locked"));
                thumbnailViews[i].Bind(runtime,slot,archiveSection==2,info.revision);
                loads[i].button.interactable=info.exists;
                loads[i].button.image.color=selectedSlot==slot?new Color32(199,228,249,255):new Color32(247,250,253,255);
            }
            bool selected=selectedSlot>0&&runtime.ArchiveExists(selectedSlot,archiveSection==2);
            loadCommand.button.interactable=selected;
            renameCommand.button.interactable=deleteCommand.button.interactable=selected&&archiveSection==0;
            if(selected){var info=runtime.DescribeArchiveCard(selectedSlot,archiveSection==2);status.text=info.name+T(" · 第 "+info.stage+" 阶段 · "," · Stage "+info.stage+" · ")+info.gameDate+" · "+info.savedAt;}
        }

        void Close()
        {
            if (!IsOpen) return;
            overlay.gameObject.SetActive(false); runtime.tickEnabled = tickBefore; IsOpen = false; pending = null;pendingNewSlot=0;
        }
    }
}
