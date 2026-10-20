using System;
using System.Collections.Generic;
using System.Linq;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.XingGuang
{
    // Game-only virtual documents. This component has no filesystem or network access.
    // Lives on the active runtime, not the inactive WindowManager root.
    public sealed partial class XgFilesWindow : MonoBehaviour
    {
        public const string AppId = "ai-files";
        public WindowManager window;
        public RectTransform client;
        public TMP_FontAsset font;
        public TMP_Text desktopLabel, titleLabel;
        public Canvas sourceCanvas;
        public GameObject shortcutRoot;
        // The desktop folder is always browsable after the opening; installation keeps its technology gate.
        public bool CanBrowse
        {
            get { var owner=XingGuangController.Instance;return Sim!=null&&owner!=null&&ReferenceEquals(owner.Sim,Sim)&&owner.runtime!=null&&owner.runtime.Sim!=null&&owner.runtime.isActiveAndEnabled&&!owner.runtime.Sim.InPrologue; }
        }
        public XgSim Sim { get; private set; }
        public string DisplayName => T("资料文件夹", "Training files");
        public bool InstallAllowed => Sim != null && Sim.CanInstallData;
        public string InstallReason => Sim != null ? Sim.DataInstallBlocker() : T("安装入口尚未接入。", "Installation is not connected yet.");
        readonly HashSet<string> selected = new HashSet<string>();
        readonly Dictionary<string, Image> faces = new Dictionary<string, Image>();
        RectTransform rows, ghost;
        RectTransform previewViewport;
        RawImage previewImage;
        ScrollRect previewScroll;
        TMP_Text preview, breadcrumb, footer, backLabel, selectLabel, installSideHint;
        bool built;
        string folder="";
        string snapshot, lastName, previewId;
        float nextObserve;
        int generation;
        public XgFilesPayload ActivePayload { get; private set; }
        string T(string zh, string en) => Sim != null && Sim.English ? en : zh;
        public void Register()
        {
            if (window == null) return;
            DesktopWindowIndex.RegisterTree(window.gameObject);
            window.onClose.AddListener(CancelDrag);
            window.onMinimize.AddListener(CancelDrag);
            window.onOpen.AddListener(Opened);
        }
        void Awake()
        {
            // The native template's static atlas does not include new file vocabulary.
            // Reuse the desktop's shared CJK font, including for the shell labels.
            var cjk=LingGuangV05.Desktop.Story.PrologueDesk.CjkFont();
            if(cjk!=null)font=cjk;
            ApplyShellFont(titleLabel);ApplyShellFont(desktopLabel);
            Register();
        }
        void ApplyShellFont(TMP_Text label)
        {
            if(label==null||font==null)return;
            label.font=font;label.fontSharedMaterial=font.material;
        }
        void Opened()
        {
            if (!CanBrowse) return;
            EnsureBuilt(); Refresh();
        }
        public void Show()
        {
            if (!CanBrowse) return;
            EnsureBuilt(); window.OpenWindow(); Refresh();
        }
        // Navigation only. The player must still select and drag a ready file into Train.
        public void ShowTrainingData(XgSim expected)
        {
            if (!ReferenceEquals(Sim, expected) || !CanBrowse) return;
            CancelDrag(); selected.Clear(); folder = "packs"; previewId = null;
            Show();
        }
        public void Bind(XgSim sim)
        {
            if (ReferenceEquals(Sim, sim)) return;
            CancelDrag(); selected.Clear(); folder = ""; snapshot = null; previewId = null;
            Sim = sim; generation++; lastName = null; ResetDataUrl(); BindDownloadFeedback(sim);
            if (window != null && window.isOn) window.CloseWindow();
            UpdateNames();
            RefreshShortcut();
        }
        void Update()
        {
            if (Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame) CancelDrag();
            if (Time.unscaledTime < nextObserve) return;
            nextObserve = Time.unscaledTime + .25f;
            var owner = XingGuangController.Instance;
            Bind(owner != null ? owner.Sim : null);
            UpdateNames();
            RefreshShortcut();
            if (window == null || !window.isOn || !window.gameObject.activeInHierarchy || !built || Sim == null) return;
            var visibleGroup=window.GetComponent<CanvasGroup>();if(visibleGroup!=null&&visibleGroup.alpha<.01f)return;
            RefreshDataUrl();
            if (ActivePayload != null) return;
            var key = StateKey(); if (key != snapshot) Refresh();
        }
        bool shortcutPlaced;
        float shortcutArrangeAfter;
        void RefreshShortcut()
        {
            if(shortcutRoot==null)return;
            bool show=CanBrowse;
            if(show&&!shortcutRoot.activeSelf)shortcutArrangeAfter=Time.unscaledTime+.5f;
            shortcutRoot.SetActive(show);
            if(!show||shortcutPlaced||Time.unscaledTime<shortcutArrangeAfter)return;
            // Keep this essential folder in the first desktop column, not beyond the monitor
            // after the older desktop's saved icon order has been restored.
            shortcutRoot.transform.SetSiblingIndex(0);
            LingGuangV05.Desktop.Story.DesktopIconOrder.ArrangeNow(shortcutRoot.transform as RectTransform);
            shortcutPlaced=true;
        }
        string StateKey()
        {
            return Sim.English + ":" + InstallAllowed + ":" + InstallReason + ":" + folder + ":" + string.Join(";", Sim.S.dataFiles.Select(f => f.fileId + ":" + f.installed))
                + "|" + string.Join(";", Sim.DownloadQueue().Select(DownloadSignature))+RecordsKey();
        }
        void UpdateNames()
        {
            string value = DisplayName; if (lastName == value) return; lastName = value;
            if (titleLabel != null) titleLabel.text = value;
            if (desktopLabel != null) desktopLabel.text = value;
            if (window != null && window.taskbarButton != null)
            {
                var task = window.taskbarButton; task.buttonTitle = value;
                if (task.headerButton != null) { task.headerButton.buttonText = value; task.headerButton.UpdateUI(); }
                var hover = task.GetComponent<HongmengOS.Aero2010.AeroShellHover>();
                if (hover != null && hover.tooltip != null)
                    foreach (var label in hover.tooltip.GetComponentsInChildren<TMP_Text>(true)) {ApplyShellFont(label);label.text = value;}
            }
            snapshot = null;
        }
        void EnsureBuilt()
        {
            if (built || client == null) return; built = true;
            client.GetComponent<Image>().color = new Color32(247, 250, 253, 255);
            var toolbar = Rect("Toolbar", client, new Vector2(0,1), Vector2.one, new Vector2(8,-42), new Vector2(-8,-5));
            backLabel = Button(toolbar, "Back", new Vector2(0,0), new Vector2(90,37), () => { CancelDrag(); folder="";previewId=null; selected.Clear(); Refresh(); });
            breadcrumb = Text(Rect("Path", toolbar, Vector2.zero, Vector2.one,new Vector2(100,0),new Vector2(-160,0)), "", 16);
            selectLabel = Button(toolbar,"SelectReady",new Vector2(-150,0),new Vector2(150,37), SelectReady, true);
            BuildDataUrl();
            var body = Rect("Body",client,Vector2.zero,Vector2.one,new Vector2(8,42),new Vector2(-8,-168));
            var list = Rect("List",body,Vector2.zero,new Vector2(.64f,1),Vector2.zero,new Vector2(-8,0));
            var viewport = Rect("Viewport",list,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            viewport.gameObject.AddComponent<Image>().color=Color.white; viewport.gameObject.AddComponent<RectMask2D>();
            rows=Rect("Rows",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);rows.pivot=new Vector2(.5f,1);
            var scroll=list.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=rows;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var side=Rect("File details",body,new Vector2(.64f,0),Vector2.one,new Vector2(8,0),Vector2.zero);
            var detail=Rect("Preview",side,Vector2.zero,Vector2.one,new Vector2(0,124),Vector2.zero);
            var installHelp=Rect("Installation guidance",side,Vector2.zero,Vector2.right,Vector2.zero,new Vector2(0,116));
            installHelp.gameObject.AddComponent<Image>().color=new Color32(222,235,247,255);
            installSideHint=Text(Rect("Instructions",installHelp,Vector2.zero,Vector2.one,new Vector2(12,8),new Vector2(-12,-8)),"",14);
            installSideHint.richText=false;
            installSideHint.alignment=TextAlignmentOptions.TopLeft;
            installSideHint.textWrappingMode=TextWrappingModes.Normal;
            detail.gameObject.AddComponent<Image>().color=new Color32(231,239,248,255);
            previewImage=Rect("Image",detail,new Vector2(0,1),Vector2.one,new Vector2(12,-192),new Vector2(-12,-12)).gameObject.AddComponent<RawImage>();previewImage.raycastTarget=false;previewImage.gameObject.SetActive(false);
            previewViewport=Rect("Preview viewport",detail,Vector2.zero,Vector2.one,new Vector2(14,12),new Vector2(-14,-12));
            previewViewport.gameObject.AddComponent<Image>().color=Color.clear;previewViewport.gameObject.AddComponent<RectMask2D>();
            var previewContent=Rect("Text",previewViewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);previewContent.pivot=new Vector2(.5f,1);
            preview=Text(previewContent,"",16);preview.richText=false;
            preview.alignment=TextAlignmentOptions.TopLeft;preview.textWrappingMode=TextWrappingModes.Normal;preview.overflowMode=TextOverflowModes.Overflow;
            previewContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            previewScroll=previewViewport.gameObject.AddComponent<ScrollRect>();previewScroll.viewport=previewViewport;previewScroll.content=previewContent;previewScroll.horizontal=false;previewScroll.movementType=ScrollRect.MovementType.Clamped;
            BuildRecordPreviewPager(detail);
            footer=Text(Rect("Status",client,Vector2.zero,Vector2.right,new Vector2(12,4),new Vector2(-12,36)),"",14);
        }
        public void Refresh()
        {
            if (!built || Sim == null) return;
            CancelDrag(); snapshot=StateKey(); faces.Clear();
            foreach(Transform child in rows) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            selected.RemoveWhere(id => !Sim.DataFileReady(id));
            backLabel.text=T("← 返回","← Back");selectLabel.text=T("选中待装","Select ready");
            installSideHint.text=T("安装到哪里？\n打开 ","Where to install?\nOpen ")+Sim.AiDisplayName+T(".exe → 练，将待安装包拖到「拖到这里安装数据包」。\nCtrl 可多选。",".exe → Train. Drop ready packs onto “Drop data packs here to install”.\nCtrl selects multiple packs.");
            if(!InstallAllowed)installSideHint.text=T("暂时不能安装\n","Installation unavailable\n")+InstallReason;
            breadcrumb.text=DisplayName+(folder.Length>0 ? " › "+FolderTitle(folder) : "");
            ShowPreview(T("单击预览；双击打开文件夹。\n按住 Ctrl 可以多选数据包。","Click to preview; double-click a folder to open.\nHold Ctrl to select multiple data packs."),null,false);
            int index=0;
            if (folder.Length==0)
            {
                AddRow(index++,"packs",T("训练数据","Training data"),T("已下载的游戏资料。","Downloaded game data."),false,true);
                AddRow(index++,"readme",T("使用说明.txt","Readme.txt"),T("在这里查看下载的数据包。选中待安装文件，拖到训练窗口。","Browse downloaded data packs here. Select ready files and drag them to the training window."),false,false);
                AddRow(index++,"training",T("训练日志.txt","Training log.txt"),TrainingLog(),false,false);
                AddRow(index++,"answers",T("答题记录.csv","Answers.csv"),"",false,false);
                AddRow(index++,"asked",T("对话题本.csv","Conversation questions.csv"),"",false,false);
                AddRow(index++,"notes",FolderTitle("notes"),T("已经答过的真实题目和标注。","Actual answered questions and labels."),false,true);
                AddRow(index++,"images",FolderTitle("images"),T("来自已答题目的训练图片。","Training images from answered questions."),false,true);
                if(Sim.Has("train.checkpoint")) AddRow(index++,"models",FolderTitle("models"),T("存档里实际保存的模型。","Models actually saved in this game."),false,true);
            }
            else if(folder=="packs")
            {
                foreach(var file in Sim.S.dataFiles)
                {
                    string name=Sim.BoardDataFileName(file.fileId)??FileName(file.dataset,file.nodeId);
                    string state=file.installed?T("已安装","Installed"):T("待安装","Ready to install");
                    string size=file.sizeMB>0 ? "\n"+XgSim.SizeText(file.sizeMB):"";
                    AddRow(index++,file.fileId,name+"  ·  "+state,name+"\n"+state+size,Sim.DataFileReady(file.fileId),false);
                }
                foreach(var file in Sim.DownloadQueue()) AddQueueRow(index++,file);
                if(index==0) preview.text=T("还没有下载的数据包。","No downloaded data packs yet.");
            }
            else BuildRecords(ref index);
            rows.sizeDelta=new Vector2(0,index*46);UpdateSelection();RefreshDataUrl();
        }
        string FileName(string dataset,string node)
        {
            var n=string.IsNullOrEmpty(node)?null:XgCatalog.Node(node);var d=XgCatalog.Dataset(dataset);
            return (n!=null?(Sim.English?n.nameEn:n.name):d!=null?(Sim.English?d.nameEn:d.name):!string.IsNullOrEmpty(node)?node:dataset)+".zip";
        }
        void AddRow(int index,string id,string label,string detail,bool ready,bool isFolder,Func<Texture2D> picture=null)
        {
            var r=Rect("File",rows,new Vector2(0,1),Vector2.one,new Vector2(0,-(index+1)*46),new Vector2(0,-index*46-2));
            var face=r.gameObject.AddComponent<Image>();face.color=Color.white;faces[id]=face;
            Text(Rect("Name",r,Vector2.zero,Vector2.one,new Vector2(12,0),new Vector2(-10,0)),label,16);
            var input=r.gameObject.AddComponent<XgFilesRow>();input.owner=this;input.fileId=id;input.ready=ready;input.folder=isFolder;input.detail=detail;input.picture=picture;
            if(id==previewId){if(IsRecordCsv(id))ShowRecordCsv(false);else ShowPreview(detail,picture,false);}
        }
        public void Pick(XgFilesRow row, PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left)return;
            if(row.folder && e.clickCount>=2){folder=row.fileId;previewId=null;selected.Clear();Refresh();return;}
            bool control=Keyboard.current!=null&&(Keyboard.current.leftCtrlKey.isPressed||Keyboard.current.rightCtrlKey.isPressed);
            if(!control)selected.Clear();
            if(row.ready){if(!selected.Add(row.fileId)&&control)selected.Remove(row.fileId);}
            previewId=row.fileId;if(IsRecordCsv(previewId))ShowRecordCsv(true);else ShowPreview(row.detail,row.picture,true);UpdateSelection();
        }
        void SelectReady(){if(folder!="packs")return;selected.Clear();foreach(var f in Sim.S.dataFiles)if(Sim.DataFileReady(f.fileId))selected.Add(f.fileId);UpdateSelection();}
        void UpdateSelection()
        {
            foreach(var pair in faces)pair.Value.color=selected.Contains(pair.Key)?new Color32(205,229,251,255):Color.white;
            footer.text=selected.Count+T(" 个文件已选中"," files selected")+(InstallAllowed?"":"  ·  "+InstallReason);
        }
        public void BeginFileDrag(XgFilesRow row,PointerEventData e)
        {
            CancelDrag();if(!row.ready||Sim==null||!Sim.DataFileReady(row.fileId)||sourceCanvas==null)return;
            if(!selected.Contains(row.fileId)){selected.Clear();selected.Add(row.fileId);UpdateSelection();}
            ActivePayload=new XgFilesPayload(this,Sim,generation,selected.Where(Sim.DataFileReady));
            ghost=Rect("Virtual file drag",sourceCanvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(220,38));ghost.pivot=new Vector2(0,1);
            ghost.gameObject.AddComponent<Image>().color=new Color32(211,232,252,240);
            var group=ghost.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            Text(Rect("Label",ghost,Vector2.zero,Vector2.one,new Vector2(8,0),new Vector2(-8,0)),selected.Count+T(" 个数据包"," data packs"),16);
            MoveDrag(e);
        }
        public void MoveDrag(PointerEventData e)
        {
            if(ghost==null)return;
            Vector2 p;if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)sourceCanvas.transform,e.position,e.pressEventCamera,out p))ghost.anchoredPosition=p+new Vector2(12,-12);
        }
        public bool IsCurrent(XgFilesPayload payload)=>payload!=null&&ReferenceEquals(payload,ActivePayload)&&ReferenceEquals(payload.Source,Sim)&&payload.Generation==generation&&window!=null&&window.isOn;
        public void CancelDrag(){ActivePayload=null;if(ghost!=null)Destroy(ghost.gameObject);ghost=null;}
        void OnDisable(){CancelDrag();}
        void LateUpdate()
        {
            // Native OpenWindow invokes onOpen before playing In; closing inside that
            // callback is overwritten by In. Enforce revoked entry access after it returns.
            if(window!=null&&window.isOn&&!CanBrowse)window.CloseWindow();
        }
        void OnDestroy(){UnbindDownloadFeedback();CancelDrag();if(window!=null){window.onClose.RemoveListener(CancelDrag);window.onMinimize.RemoveListener(CancelDrag);window.onOpen.RemoveListener(Opened);}}
        RectTransform Rect(string name,Transform parent,Vector2 a,Vector2 b,Vector2 min,Vector2 max)
        {var g=new GameObject(name,typeof(RectTransform));g.layer=parent.gameObject.layer;var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=a;r.anchorMax=b;r.offsetMin=min;r.offsetMax=max;return r;}
        TMP_Text Text(RectTransform r,string value,int size)
        {var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.enableAutoSizing=false;t.color=new Color32(32,49,65,255);t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;t.overflowMode=TextOverflowModes.Ellipsis;t.textWrappingMode=TextWrappingModes.NoWrap;return t;}
        TMP_Text Button(Transform parent,string name,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action,bool right=false)
        {var r=Rect(name,parent,right?Vector2.right:Vector2.zero,right?Vector2.right:Vector2.zero,pos,pos+size);r.gameObject.AddComponent<Image>().color=new Color32(222,235,247,255);var b=r.gameObject.AddComponent<Button>();b.onClick.AddListener(action);return Text(Rect("Label",r,Vector2.zero,Vector2.one,new Vector2(8,0),new Vector2(-8,0)),"",14);}
    }
    // Immutable identities and a source-save identity, never a dataset alias or real path.
    public sealed class XgFilesPayload
    {
        public readonly XgSim Source;
        internal readonly int Generation;
        readonly string[] ids;
        readonly XgFilesWindow owner;
        bool consumed;
        internal XgFilesPayload(XgFilesWindow owner,XgSim source,int generation,IEnumerable<string> values){this.owner=owner;Source=source;Generation=generation;ids=values.Distinct().ToArray();}
        public IReadOnlyList<string> FileIds=>Array.AsReadOnly(ids);
        public bool IsCurrent=>!consumed&&owner!=null&&owner.IsCurrent(this);
        public bool SourceStillCurrent=>owner!=null&&owner.IsCurrent(this);
        public bool CanInstallInto(XgSim target)=>IsCurrent&&ReferenceEquals(target,Source)&&owner.InstallAllowed;
        public bool TryConsume(XgSim target)
        {if(!CanInstallInto(target))return false;consumed=true;return true;}
    }
    public sealed class XgFilesRow:MonoBehaviour,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public XgFilesWindow owner;public string fileId,detail;public bool ready,folder;public Func<Texture2D> picture;
        public XgFilesPayload Payload=>owner!=null?owner.ActivePayload:null;
        public void OnPointerClick(PointerEventData e)=>owner.Pick(this,e);
        public void OnBeginDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)owner.BeginFileDrag(this,e);}
        public void OnDrag(PointerEventData e)=>owner.MoveDrag(e);
        public void OnEndDrag(PointerEventData e)=>owner.CancelDrag();
    }
}
