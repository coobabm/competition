using System;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatView
    {
        Button dataReviewTool,dataReviewPrevious,dataReviewNext;
        RectTransform dataReviewPanel,dataReviewContent,dataReviewMain;
        TMP_InputField dataReviewSearch;
        TMP_Text dataReviewTitle,dataReviewPage,dataReviewHint;
        XgSim dataReviewOwner;
        int dataReviewOffset;
        string dataReviewStamp="";
        float dataReviewAddedWidth;
        Vector2 dataReviewMainMax;
        const int DataReviewPageSize=30;
        const float DataReviewWidth=320;
        void OnDisable()=>CloseDataReview();

        void RefreshDataReviewTools()
        {
            if(root==null||hub==null||hub.S==null)return;
            var main=root.Find("Conversation") as RectTransform;
            var tools=main!=null?main.Find("Tools") as RectTransform:null;
            if(tools==null)return;
            if(dataReviewTool==null)
            {
                dataReviewTool=FlatButton(tools,"",ToggleDataReview,14,C.Ink,Color.white);
                Place((RectTransform)dataReviewTool.transform,new Vector2(0,.5f),new Vector2(284,-14),new Vector2(370,14));
                AddFocus(dataReviewTool.gameObject);
                UiTip.Add((RectTransform)dataReviewTool.transform,()=>GameText.T("购买「数据回顾」后查看学习历史；未购买时点这里前往科技。","Buy Learning review to browse its history. Click here to find the technology while locked."));
            }
            var sim=XgYyTalk.LearningSim;bool visible=LingGuangShown&&sim!=null;
            dataReviewTool.gameObject.SetActive(visible);
            dataReviewTool.GetComponentInChildren<TMP_Text>().text=GameText.T("数据","Data")+(sim!=null&&sim.DataReviewOpen?"":" · ◇");
            if(!ReferenceEquals(sim,dataReviewOwner)) { CloseDataReview();dataReviewOwner=sim;dataReviewOffset=0;dataReviewStamp=""; }
            if(!visible||!sim.DataReviewOpen){CloseDataReview();return;}
            if(dataReviewPanel==null||!dataReviewPanel.gameObject.activeSelf)return;
            string stamp=sim.S.learningRecordSerial+":"+(sim.S.askedLog?.Count??0)+":"+(sim.S.dataFiles?.Count??0)+":"+GameText.LanguageId;
            if(stamp!=dataReviewStamp){dataReviewStamp=stamp;DrawDataReview();}
        }
        void ToggleDataReview()
        {
            var sim=XgYyTalk.LearningSim;if(sim==null||!LingGuangShown)return;
            if(!sim.DataReviewOpen)
            {
                XgYyTalk.OpenLearningTech();
                XingGuangController.Instance?.View?.Tree?.FocusNodeDetails(XgSim.DataReviewNode);
                return;
            }
            if(dataReviewPanel!=null&&dataReviewPanel.gameObject.activeSelf){CloseDataReview();return;}
            CloseLearningDrawer();BuildDataReview();dataReviewOwner=sim;dataReviewOffset=0;
            dataReviewMain=root.Find("Conversation") as RectTransform;
            dataReviewMainMax=dataReviewMain.offsetMax;
            var container=window!=null?window.windowContainer as RectTransform:null;
            if(container!=null&&container.parent is RectTransform parent)
            {
                dataReviewAddedWidth=Mathf.Min(DataReviewWidth,Mathf.Max(0,parent.rect.width-container.rect.width-24));
                container.sizeDelta+=new Vector2(dataReviewAddedWidth,0);
            }
            dataReviewMain.offsetMax=dataReviewMainMax-new Vector2(DataReviewWidth+4,0);
            dataReviewPanel.gameObject.SetActive(true);dataReviewPanel.SetAsLastSibling();DrawDataReview();lastSignature="";dirty=true;
        }
        void CloseDataReview()
        {
            if(dataReviewPanel==null||!dataReviewPanel.gameObject.activeSelf)return;
            dataReviewPanel.gameObject.SetActive(false);
            if(dataReviewMain!=null)dataReviewMain.offsetMax=dataReviewMainMax;
            var container=window!=null?window.windowContainer as RectTransform:null;
            if(container!=null&&dataReviewAddedWidth>0)container.sizeDelta-=new Vector2(dataReviewAddedWidth,0);
            dataReviewAddedWidth=0;lastSignature="";dirty=true;
        }
        void BuildDataReview()
        {
            if(dataReviewPanel!=null)return;
            dataReviewPanel=Rect("DataReviewPanel",root,new Vector2(1,0),Vector2.one,new Vector2(-DataReviewWidth,0),Vector2.zero);
            Fill(dataReviewPanel,new Color32(239,244,250,255));
            dataReviewTitle=Text(Rect("Title",dataReviewPanel,new Vector2(0,1),Vector2.one,new Vector2(12,-38),new Vector2(-40,-6)),"",16,C.Ink,TextAlignmentOptions.MidlineLeft);
            var close=FlatButton(dataReviewPanel,"×",CloseDataReview,18,C.Ink,Color.white);
            Place((RectTransform)close.transform,Vector2.one,new Vector2(-36,-34),new Vector2(-6,-6));
            var search=Rect("Search",dataReviewPanel,new Vector2(0,1),Vector2.one,new Vector2(10,-76),new Vector2(-10,-42));Fill(search,Color.white);
            dataReviewSearch=BuildInput(search);dataReviewSearch.lineType=TMP_InputField.LineType.SingleLine;dataReviewSearch.onSubmit.RemoveAllListeners();
            dataReviewSearch.onValueChanged.AddListener(_=>{dataReviewOffset=0;DrawDataReview();});
            dataReviewHint=Text(Rect("Hint",dataReviewPanel,new Vector2(0,1),Vector2.one,new Vector2(12,-116),new Vector2(-12,-80)),"",12,C.Muted,TextAlignmentOptions.MidlineLeft);
            var list=Rect("List",dataReviewPanel,Vector2.zero,Vector2.one,new Vector2(8,40),new Vector2(-8,-120));Fill(list,new Color(0,0,0,0));list.gameObject.AddComponent<RectMask2D>();
            var scroll=list.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=25;
            dataReviewContent=Rect("Content",list,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);dataReviewContent.pivot=new Vector2(.5f,1);scroll.content=dataReviewContent;scroll.viewport=list;
            dataReviewPrevious=FlatButton(dataReviewPanel,"",()=>{dataReviewOffset=Math.Max(0,dataReviewOffset-DataReviewPageSize);DrawDataReview();},12,C.Ink,Color.white);
            Place((RectTransform)dataReviewPrevious.transform,Vector2.zero,new Vector2(8,6),new Vector2(78,34));
            dataReviewNext=FlatButton(dataReviewPanel,"",()=>{dataReviewOffset+=DataReviewPageSize;DrawDataReview();},12,C.Ink,Color.white);
            Place((RectTransform)dataReviewNext.transform,new Vector2(1,0),new Vector2(-78,6),new Vector2(-8,34));
            dataReviewPage=Text(Rect("Page",dataReviewPanel,Vector2.zero,Vector2.right,new Vector2(82,6),new Vector2(-82,34)),"",12,C.Muted,TextAlignmentOptions.Center);
            foreach(var control in dataReviewPanel.GetComponentsInChildren<Selectable>(true))AddFocus(control.gameObject);
            dataReviewPanel.gameObject.SetActive(false);
        }
        static string ReviewKind(string kind)=>kind=="book"?GameText.T("书","Book"):kind=="data"?GameText.T("数据包","Data pack"):
            kind=="chat"?GameText.T("对话","Chat"):GameText.T("题","Question");
        void DrawDataReview()
        {
            var sim=XgYyTalk.LearningSim;if(dataReviewContent==null||sim==null||!sim.DataReviewOpen)return;
            for(int i=dataReviewContent.childCount-1;i>=0;i--){var child=dataReviewContent.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
            dataReviewTitle.richText=false;
            dataReviewTitle.text=sim.AiDisplayName+GameText.T(" · 学过的"," · Learning history");
            ((TMP_Text)dataReviewSearch.placeholder).text=GameText.T("搜索所有记录","Search all records");
            dataReviewHint.text=GameText.T("拖一道文字题到聊天区，再考它一次。\n点书名可重读。旧记录不补造时间。","Drag a text question into chat to retest.\nClick a book to reread. Old dates stay unknown.");
            var page=sim.QueryDataReview(dataReviewSearch.text,dataReviewOffset,DataReviewPageSize);
            if(dataReviewOffset>=page.total&&dataReviewOffset>0){dataReviewOffset=Math.Max(0,(page.total-1)/DataReviewPageSize*DataReviewPageSize);page=sim.QueryDataReview(dataReviewSearch.text,dataReviewOffset,DataReviewPageSize);}
            dataReviewPrevious.GetComponentInChildren<TMP_Text>().text=GameText.T("上一页","Previous");dataReviewNext.GetComponentInChildren<TMP_Text>().text=GameText.T("下一页","Next");
            dataReviewPrevious.interactable=dataReviewOffset>0;dataReviewNext.interactable=page.HasMore;
            dataReviewPage.text=(dataReviewOffset/DataReviewPageSize+1)+" · "+page.total;
            float y=0;int priorStage=-1;
            foreach(var row in page.rows)
            {
                if(row.stage!=priorStage)
                {
                    string stage=row.stage>0?GameText.T("能力阶段 "+row.stage,"Ability stage "+row.stage):GameText.T("阶段未记录","Stage not recorded");
                    Text(Rect("Stage",dataReviewContent,new Vector2(0,1),Vector2.one,new Vector2(5,-y-26),new Vector2(-5,-y)),stage,13,C.Muted,TextAlignmentOptions.MidlineLeft);y+=30;priorStage=row.stage;
                }
                var entry=row.record;var input=entry!=null?sim.LearningRetestInput(entry.id):null;
                bool retest=input!=null&&!input.requiresImage;
                string title=GameText.T(row.title,string.IsNullOrEmpty(row.titleEn)?row.title:row.titleEn);
                string label=ReviewKind(row.kind)+" · "+(entry!=null?sim.LearningTimestamp(entry):GameText.T("时间未记录","Time not recorded"))+"\n"+title;
                if(entry!=null)label+="\n"+sim.LearningAnswerSummary(entry);
                if(input!=null&&input.requiresImage)label+="\n"+GameText.T("含原图 · 在教页查看","Includes image · review in Teach");
                var captured=row;var owner=sim;
                var b=FlatButton(dataReviewContent,label,()=>
                {
                    if(!ReferenceEquals(owner,XgYyTalk.LearningSim)||!owner.DataReviewOpen)return;
                    if(!string.IsNullOrEmpty(captured.bookId))XgReadingWindow.Open(XingGuangController.Instance,captured.bookId);
                    else if(input!=null&&input.requiresImage){XingGuangController.Instance?.Open();XingGuangController.Instance?.View?.ShowTab("teach");}
                },12,C.Ink,Color.white);
                var rect=(RectTransform)b.transform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(0,-y-104);rect.offsetMax=new Vector2(0,-y);
                var text=b.GetComponentInChildren<TMP_Text>();text.richText=false;text.alignment=TextAlignmentOptions.TopLeft;text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Ellipsis;
                UiTip.Add(rect,()=>label);AddFocus(b.gameObject);
                if(retest){var drag=b.gameObject.AddComponent<YYDataReviewDrag>();drag.canDrag=()=>ReferenceEquals(owner,XgYyTalk.LearningSim)&&owner.DataReviewOpen&&LingGuangShown&&IsOpen;
                    drag.drop=e=>RetestDataReview(owner,entry.id,e);drag.feedback=s=>dataReviewHint.text=s;}
                y+=108;
            }
            if(page.total==0){Text(Rect("Empty",dataReviewContent,new Vector2(0,1),Vector2.one,new Vector2(8,-70),new Vector2(-8,0)),GameText.T("还没有符合条件的学习记录。","No matching learning records yet."),13,C.Muted,TextAlignmentOptions.MidlineLeft);y=74;}
            dataReviewContent.sizeDelta=new Vector2(0,y);dataReviewContent.anchoredPosition=Vector2.zero;
        }
        void RetestDataReview(XgSim owner,long id,PointerEventData e)
        {
            if(!ReferenceEquals(owner,XgYyTalk.LearningSim)||!owner.DataReviewOpen||!LingGuangShown||!IsOpen||messageArea==null||!messageArea.gameObject.activeInHierarchy)return;
            if(!RectTransformUtility.RectangleContainsScreenPoint(messageArea,e.position,e.pressEventCamera))return;
            var hit=e.pointerCurrentRaycast.gameObject;
            if(hit==null||!hit.transform.IsChildOf(messageArea))return;
            var input=owner.LearningRetestInput(id);if(input==null||input.requiresImage)return;
            bool sent=XgYyTalk.AskFromNotebook(owner.LearningRecord(id));
            dataReviewHint.text=sent?GameText.T("已发出，等它回答。","Sent. Waiting for its answer."):GameText.T("它正在忙，等回复后再拖一次。","It is busy. Drag again after its reply.");
        }
    }
    public sealed class YYDataReviewDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public Func<bool> canDrag;
        public Action<PointerEventData> drop;
        public Action<string> feedback;
        bool held;Graphic graphic;Color oldColor;
        public void OnBeginDrag(PointerEventData e)
        {
            held=e.button==PointerEventData.InputButton.Left&&canDrag!=null&&canDrag();if(!held)return;
            graphic=GetComponent<Graphic>();if(graphic!=null){oldColor=graphic.color;graphic.color=new Color32(214,234,251,255);}
            feedback?.Invoke(GameText.T("松到左侧聊天区，重新考它。","Release over the chat to ask again."));
        }
        public void OnDrag(PointerEventData e){}
        public void OnEndDrag(PointerEventData e){bool send=held;Restore();if(send&&canDrag!=null&&canDrag())drop?.Invoke(e);}
        void OnDisable()=>Restore();
        void Restore(){if(held&&graphic!=null)graphic.color=oldColor;held=false;}
    }
}
