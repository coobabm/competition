using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    // A window-local read-only collection. It owns its demonstrations, never the story's presenter.
    public sealed class XgUnifiedAtlas : MonoBehaviour
    {
        const int PageSize = 7;
        XingGuangView owner;
        XgUi ui;
        XgSim bound;
        object boundWorld;
        string originTab;
        RectTransform frame, bodyArea, bodyContent, demoArea, sequenceScale;
        TMP_Text heading, detailTitle, body, pageLabel, footnote;
        XgBtn close, previous, next, action;
        readonly XgBtn[] tabs = new XgBtn[3], rows = new XgBtn[PageSize];
        List<XgAtlasEntry> entries = new List<XgAtlasEntry>();
        XgAtlasEntry selected;
        XgAtlasCategory category;
        XgStageSequenceDemo sequence;
        XgAtlasTinyDemo tiny;
        XgCortexGraphic network;
        bool english, reduced, viewerOpen, treeOpen;
        int page;
        float refreshIn;
        string selectedId = "";
        readonly List<bool> collectionRights = new List<bool>();
        XgAtlasCategory cachedCategory = (XgAtlasCategory)(-1);
        string laidOutBody;
        float laidOutWidth = -1, laidOutHeight = -1;
        public bool Showing => gameObject.activeSelf;
        public XgAtlasCategory Category => category;
        public string SelectedId => selectedId;
        public bool ReplayVisible => tiny != null && tiny.Showing || sequence != null && sequence.Visible;
        public bool Current => isActiveAndEnabled && bound != null && owner != null && owner.Visible && ReferenceEquals(bound,owner.Sim)
            && ReferenceEquals(boundWorld,owner.Controller.runtime?.Sim) && owner.Tab==originTab
            && !StoryFocus.Exclusive && !owner.SetupShowing;
        string T(string zh,string en) => english ? en : zh;

        public static XgUnifiedAtlas Create(XingGuangView view, RectTransform parent, XgUi kit)
        {
            var root=Rect("Unified atlas",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var atlas=root.gameObject.AddComponent<XgUnifiedAtlas>();
            atlas.owner=view;atlas.ui=kit;atlas.Build(root);
            view.Controller.Window.onMinimize.AddListener(atlas.Close);
            view.Controller.Window.onClose.AddListener(atlas.Close);
            root.gameObject.SetActive(false);return atlas;
        }
        void Build(RectTransform root)
        {
            Panel(root,new Color(0,0,0,.88f)).raycastTarget=true;
            frame=ui.Card(root,"Atlas collection",Vector2.zero,Vector2.one,new Vector2(18,18),new Vector2(-18,-18));
            heading=ui.Text(Strip("Title",frame,10,34,18,150),"",23,XgDark.Ink,TextAlignmentOptions.MidlineLeft);
            close=ui.Button(frame,"",Close,16);close.rt.gameObject.name="Atlas close";PlaceTopRight(close,126,10,108,34);
            for(int i=0;i<3;i++)
            {
                int group=i;tabs[i]=ui.Button(frame,"",()=>SelectCategory((XgAtlasCategory)group),16);
                tabs[i].rt.gameObject.name="Atlas category "+i;PlaceTopLeft(tabs[i],18+i*240,52,232,38);
            }
            var list=Rect("Entries",frame,Vector2.zero,new Vector2(0,1),new Vector2(18,58),new Vector2(270,-102));
            for(int i=0;i<PageSize;i++)
            {
                int row=i;rows[i]=ui.Button(list,"",()=>SelectRow(row),15);rows[i].rt.gameObject.name="Atlas entry "+i;
                rows[i].rt.anchorMin=new Vector2(0,1-(i+1f)/PageSize);rows[i].rt.anchorMax=new Vector2(1,1-i/(float)PageSize);
                rows[i].rt.offsetMin=new Vector2(0,4);rows[i].rt.offsetMax=new Vector2(0,-4);
                rows[i].label.textWrappingMode=TextWrappingModes.Normal;
            }
            previous=ui.Button(frame,"",()=>ChangePage(-1),16);PlaceBottomLeft(previous,18,14,64,34);
            next=ui.Button(frame,"",()=>ChangePage(1),16);PlaceBottomLeft(next,206,14,64,34);
            pageLabel=ui.Text(Rect("Page",frame,Vector2.zero,Vector2.zero,new Vector2(86,14),new Vector2(202,48)),"",15,XgDark.Muted,TextAlignmentOptions.Center);
            var detail=Rect("Detail",frame,Vector2.zero,Vector2.one,new Vector2(290,58),new Vector2(-18,-102));
            Panel(detail,XgDark.Page);
            detailTitle=ui.Text(Strip("Entry title",detail,10,58,16,190),"",20,XgDark.Ink,TextAlignmentOptions.MidlineLeft);
            detailTitle.textWrappingMode=TextWrappingModes.Normal;
            action=ui.Button(detail,"",ReplayOrUnlock,15);action.rt.gameObject.name="Atlas replay or unlock";PlaceTopRight(action,178,18,162,36);
            bodyArea=Rect("Entry text",detail,Vector2.zero,Vector2.one,new Vector2(16,16),new Vector2(-16,-78));
            Panel(bodyArea,Color.clear).raycastTarget=true;bodyArea.gameObject.AddComponent<RectMask2D>();
            var scroll=bodyArea.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.viewport=bodyArea;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;
            bodyContent=Rect("Text content",bodyArea,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);bodyContent.pivot=new Vector2(.5f,1);
            body=ui.Text(Rect("Body",bodyContent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"",17,XgDark.Ink,TextAlignmentOptions.TopLeft);
            body.textWrappingMode=TextWrappingModes.Normal;scroll.content=bodyContent;
            demoArea=Rect("Read only demonstration",detail,Vector2.zero,Vector2.one,new Vector2(8,8),new Vector2(-8,-136));
            demoArea.gameObject.AddComponent<RectMask2D>();
            tiny=new XgAtlasTinyDemo(ui,demoArea);
            network=Rect("Network",demoArea,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject.AddComponent<XgCortexGraphic>();
            network.raycastTarget=false;network.Font=ui.font;network.gameObject.SetActive(false);
            footnote=ui.Text(Rect("Collection note",frame,Vector2.zero,new Vector2(1,0),new Vector2(290,12),new Vector2(-18,48)),"",14,XgDark.Muted,TextAlignmentOptions.MidlineLeft);
            footnote.textWrappingMode=TextWrappingModes.Normal;
        }
        public void Open()
        {
            StopDemos();bound=owner.Sim;boundWorld=owner.Controller.runtime?.Sim;originTab=owner.Tab;
            category=XgAtlasCategory.Abilities;page=0;selectedId="";gameObject.SetActive(true);transform.SetAsLastSibling();Refresh(true);
        }
        public void Close(){StopDemos();selected=null;selectedId="";gameObject.SetActive(false);}
        void OnDisable(){StopDemos();selected=null;selectedId="";}
        void OnDestroy()
        {
            if(owner==null || owner.Controller==null || owner.Controller.Window==null)return;
            owner.Controller.Window.onMinimize.RemoveListener(Close);
            owner.Controller.Window.onClose.RemoveListener(Close);
        }
        void StopDemos(){tiny?.End();sequence?.End();if(network!=null)network.gameObject.SetActive(false);}
        public void SelectCategory(XgAtlasCategory value)
        {
            if(!Current || value<XgAtlasCategory.Abilities || value>XgAtlasCategory.Networks)return;
            category=value;page=0;selectedId="";Refresh(true);
        }
        void ChangePage(int direction)
        {
            if(!Current)return;page=Mathf.Clamp(page+direction,0,Mathf.Max(0,(entries.Count-1)/PageSize));selectedId="";Refresh(true);
        }
        void SelectRow(int row)
        {
            if(!Current)return;int index=page*PageSize+row;if(index<0 || index>=entries.Count)return;
            selectedId=entries[index].id;Refresh(true);
        }
        // Exact entitlement comparison, without rebuilding strings or allocating an entry list on idle ticks.
        bool RightsChanged()
        {
            int index=0;bool changed=false;
            if(category==XgAtlasCategory.Abilities)
                for(int stage=1;stage<=XgSim.AbilityCount;stage++)CompareRight(XgUnifiedAtlasCatalog.AbilityOpen(bound,stage),ref index,ref changed);
            else if(category==XgAtlasCategory.Phenomena)
            {
                foreach(var node in XgCatalog.Nodes)if(node.kind==XgNodeKind.Phenomenon)CompareRight(bound.AtlasLit(node),ref index,ref changed);
            }
            else foreach(var arch in XgCatalog.Archs)CompareRight(XgUnifiedAtlasCatalog.NetworkOpen(bound,arch.id),ref index,ref changed);
            if(collectionRights.Count>index){collectionRights.RemoveRange(index,collectionRights.Count-index);changed=true;}
            return changed;
        }
        void CompareRight(bool value,ref int index,ref bool changed)
        {
            if(index==collectionRights.Count){collectionRights.Add(value);changed=true;}
            else if(collectionRights[index]!=value){collectionRights[index]=value;changed=true;}
            index++;
        }
        void Refresh(bool force)
        {
            if(!Current){Close();return;}
            refreshIn=.25f;
            bool nextEnglish=bound.English, nextReduced=bound.S.reduceFx || PlayerPrefs.GetInt(StoryDesktopPresenter.ReduceMotionPref,0)==1;
            bool rights=XgUnifiedAtlasCatalog.ViewerOpen(bound);
            bool nextTreeOpen=owner.FeatureOpen("tree");
            bool collectionChanged=RightsChanged();
            bool rebuild=nextEnglish!=english || category!=cachedCategory || collectionChanged;
            force|=rebuild || nextReduced!=reduced || rights!=viewerOpen || nextTreeOpen!=treeOpen;
            if(!force)return;
            english=nextEnglish;reduced=nextReduced;viewerOpen=rights;treeOpen=nextTreeOpen;
            if(rebuild){entries=XgUnifiedAtlasCatalog.Entries(bound,category,english);cachedCategory=category;}
            page=Mathf.Clamp(page,0,Mathf.Max(0,(entries.Count-1)/PageSize));
            if(entries.Count>0 && !entries.Exists(x=>x.id==selectedId))selectedId=entries[page*PageSize].id;
            var entry=entries.Find(x=>x.id==selectedId);
            force|=selected==null || entry==null || selected.id!=entry.id || selected.unlocked!=entry.unlocked || selected.body!=entry.body;
            selected=entry;
            heading.text=T("图鉴","Atlas");close.Set(T("关闭","Close"),true);
            string[] names={T("能力演示","Ability demonstrations"),T("训练现象","Training phenomena"),T("网络结构","Network structures")};
            for(int i=0;i<3;i++)tabs[i].Set(names[i],true,category==(XgAtlasCategory)i ? XgDark.OnFill : XgDark.Button);
            for(int i=0;i<PageSize;i++)
            {
                int index=page*PageSize+i;rows[i].Show(index<entries.Count);
                if(index>=entries.Count)continue;var item=entries[index];
                rows[i].Set((item.unlocked?"✓ ":"◇ ")+item.title,true,item.id==selectedId?XgDark.OnFill:XgDark.Button,item.unlocked?XgDark.Ink:XgDark.Muted);
            }
            previous.Set("‹",page>0);next.Set("›",(page+1)*PageSize<entries.Count);
            pageLabel.text=(page+1)+" / "+Mathf.Max(1,(entries.Count+PageSize-1)/PageSize);
            footnote.text=T("已收录的能力、现象与网络结构。","Your collected abilities, phenomena and network structures.");
            if(force)ShowSelected();
        }
        void ShowSelected()
        {
            StopDemos();bodyContent.anchoredPosition=Vector2.zero;
            demoArea.anchorMax=Vector2.one;demoArea.offsetMax=new Vector2(-8,-136);
            detailTitle.text=selected==null?T("尚无记录","No entries yet"):selected.title;
            body.text=selected==null?"":selected.body;action.Show(false);demoArea.gameObject.SetActive(false);
            bodyArea.anchorMin=Vector2.zero;bodyArea.offsetMin=new Vector2(16,16);bodyArea.offsetMax=new Vector2(-16,-78);
            if(selected==null)return;
            if(selected.category==XgAtlasCategory.Abilities && selected.unlocked)
            {
                action.Show(true);action.Set(viewerOpen?T("重新演示","Replay"):T("解锁看它学","Unlock viewer"),viewerOpen || owner.FeatureOpen("tree"));
                if(!viewerOpen){body.text+="\n\n"+T("购买「权重可视化」后可以重览能力演示。","Buy Weight viewer in Tech to replay ability demonstrations.");return;}
                bodyArea.anchorMin=new Vector2(0,1);bodyArea.offsetMin=new Vector2(16,-130);
                demoArea.gameObject.SetActive(true);
                if(selected.stage<=3)tiny.Begin(selected.stage,english,reduced);
                else
                {
                    if(sequence==null && PrologueDirector.Desk!=null)
                    {
                        sequence=new XgStageSequenceDemo(PrologueDirector.Desk,demoArea);
                        sequenceScale=demoArea.Find("Sequence Ability Demo") as RectTransform;
                    }
                    if(sequence!=null)sequence.Begin(selected.stage,english,reduced);
                    else body.text=T("演示暂不可用，请关闭后重开图鉴。","Demonstration unavailable. Close and reopen the atlas.");
                }
            }
            else if(selected.category==XgAtlasCategory.Networks)
            {
                if(!selected.unlocked){action.Show(true);action.Set(T("去科技查看","View in Tech"),owner.FeatureOpen("tree"));return;}
                bodyArea.anchorMin=new Vector2(0,.57f);bodyArea.offsetMin=new Vector2(16,4);
                demoArea.anchorMax=new Vector2(1,.55f);demoArea.offsetMax=new Vector2(-8,-4);demoArea.gameObject.SetActive(true);
                network.GlyphArch=selected.id;network.Reduced=reduced;network.Tokens=english?new[]{"I","have","read","this","book","before"}:new[]{"这","本","书","我","看","过"};
                network.Source=english?new[]{"The","cat","sat","on","the","mat"}:new[]{"那","只","猫","坐","在","垫"};
                network.gameObject.SetActive(true);network.Animate();network.SyncLabels();
            }
        }
        void ReplayOrUnlock()
        {
            if(!Current || selected==null)return;
            if(selected.category==XgAtlasCategory.Abilities && XgUnifiedAtlasCatalog.CanReplay(bound,selected.stage)){ShowSelected();return;}
            string target=selected.category==XgAtlasCategory.Networks?selected.id:XgUnifiedAtlasCatalog.ViewerNode;
            if(!owner.FeatureOpen("tree"))return;
            Close();owner.ShowTab("tree");owner.Tree.FocusNodeDetails(target);
        }
        void Update()
        {
            if(!Current){Close();return;}
            refreshIn-=Time.unscaledDeltaTime;if(refreshIn<=0)Refresh(false);
            tiny.Tick(Time.unscaledDeltaTime,reduced);sequence?.Tick();
            if(network.gameObject.activeSelf && !reduced){network.Animate();network.SyncLabels();}
        }
        void LateUpdate()
        {
            if(!Current){Close();return;}
            float width=Mathf.Max(1,bodyArea.rect.width);
            float height=bodyArea.rect.height;
            if(laidOutBody!=body.text || !Mathf.Approximately(laidOutWidth,width) || !Mathf.Approximately(laidOutHeight,height))
            {
                laidOutBody=body.text;laidOutWidth=width;laidOutHeight=height;
                float contentHeight=Mathf.Max(height,body.GetPreferredValues(body.text,width,0).y+8);
                if(!Mathf.Approximately(bodyContent.sizeDelta.y,contentHeight))bodyContent.sizeDelta=new Vector2(0,contentHeight);
            }
            if(sequenceScale!=null)
            {
                var scale=Vector3.one*Mathf.Min(1,Mathf.Min(demoArea.rect.width/896f,demoArea.rect.height/414f));
                if(sequenceScale.localScale!=scale)sequenceScale.localScale=scale;
            }
        }
    }
}
