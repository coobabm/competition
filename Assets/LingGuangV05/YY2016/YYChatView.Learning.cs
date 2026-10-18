using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatView
    {
        Button notebookTool,personaTool;
        RectTransform learningDrawer,learningContent;
        TMP_InputField learningSearch;
        string learningMode="",learningStamp="";
        const float LearningHeight=260;
        void RefreshLearningTools()
        {
            if(root==null||hub==null||hub.S==null)return;
            var main=root.Find("Conversation") as RectTransform;var tools=main!=null?main.Find("Tools") as RectTransform:null;if(tools==null)return;
            if(notebookTool==null)
            {
                notebookTool=FlatButton(tools,GameText.T("题本","Notebook"),()=>ToggleLearningDrawer("book"),14,C.Ink,Color.white);
                Place((RectTransform)notebookTool.transform,new Vector2(0,.5f),new Vector2(86,-14),new Vector2(174,14));
                personaTool=FlatButton(tools,GameText.T("性格","Personality"),()=>ToggleLearningDrawer("persona"),14,C.Ink,Color.white);
                Place((RectTransform)personaTool.transform,new Vector2(0,.5f),new Vector2(178,-14),new Vector2(280,14));
                AddFocus(notebookTool.gameObject);AddFocus(personaTool.gameObject);
            }
            bool show=LingGuangShown&&XgYyTalk.LearningSim!=null;
            notebookTool.gameObject.SetActive(show);personaTool.gameObject.SetActive(show);
            foreach(string name in new[]{"File","Shot","Shake","History"}){var old=tools.Find("Tool"+name);if(old!=null)old.gameObject.SetActive(!show);}
            if(!show&&learningDrawer!=null&&learningDrawer.gameObject.activeSelf)CloseLearningDrawer();
            var sim=XgYyTalk.LearningSim;
            if(show&&learningMode.Length>0&&sim!=null)
            {
                string stamp=sim.S.stage+":"+sim.S.askedLog.Count+":"+sim.S.personaPacks.Count+":"+GameText.IsEnglish;
                if(stamp!=learningStamp){learningStamp=stamp;DrawLearningRows();}
            }
        }
        void ToggleLearningDrawer(string mode)
        {
            if(learningMode==mode){CloseLearningDrawer();return;}
            if(learningDrawer==null)BuildLearningDrawer();if(learningDrawer==null)return;
            learningMode=mode;learningDrawer.gameObject.SetActive(true);learningSearch.SetTextWithoutNotify("");
            messageArea.offsetMin=new Vector2(messageArea.offsetMin.x,BottomHeight+InputHeight+ToolHeight+LearningHeight);
            learningDrawer.SetAsLastSibling();DrawLearningRows();dirty=true;lastSignature="";
        }
        void CloseLearningDrawer()
        {
            learningMode="";if(learningDrawer!=null)learningDrawer.gameObject.SetActive(false);
            if(messageArea!=null)messageArea.offsetMin=new Vector2(messageArea.offsetMin.x,BottomHeight+InputHeight+ToolHeight);
            dirty=true;lastSignature="";
        }
        void BuildLearningDrawer()
        {
            var main=root.Find("Conversation") as RectTransform;if(main==null)return;
            float bottom=BottomHeight+InputHeight+ToolHeight;
            learningDrawer=Rect("LearningDrawer",main,Vector2.zero,new Vector2(1,0),new Vector2(0,bottom),new Vector2(0,bottom+LearningHeight));Fill(learningDrawer,new Color32(244,248,253,255));
            var searchBox=Rect("Search",learningDrawer,new Vector2(0,1),Vector2.one,new Vector2(8,-38),new Vector2(-138,-6));Fill(searchBox,Color.white);
            learningSearch=BuildInput(searchBox);learningSearch.lineType=TMP_InputField.LineType.SingleLine;learningSearch.onSubmit.RemoveAllListeners();
            ((TMP_Text)learningSearch.placeholder).text=GameText.T("搜索题目","Search questions");learningSearch.onValueChanged.AddListener(_=>DrawLearningRows());
            var random=FlatButton(learningDrawer,GameText.T("抽一道","Random"),RandomNotebook,12,C.Ink,Color.white);Place((RectTransform)random.transform,new Vector2(1,1),new Vector2(-132,-36),new Vector2(-48,-8));
            var close=FlatButton(learningDrawer,"×",CloseLearningDrawer,18,C.Ink,Color.white);Place((RectTransform)close.transform,new Vector2(1,1),new Vector2(-42,-36),new Vector2(-8,-8));
            var area=Rect("List",learningDrawer,Vector2.zero,Vector2.one,new Vector2(8,6),new Vector2(-8,-44));area.gameObject.AddComponent<RectMask2D>();Fill(area,new Color(0,0,0,0));
            var sc=area.gameObject.AddComponent<ScrollRect>();sc.horizontal=false;sc.movementType=ScrollRect.MovementType.Clamped;sc.scrollSensitivity=25;
            learningContent=Rect("Content",area,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);learningContent.pivot=new Vector2(.5f,1);sc.viewport=area;sc.content=learningContent;
            foreach(var selectable in learningDrawer.GetComponentsInChildren<Selectable>(true))AddFocus(selectable.gameObject);
        }
        bool LearningMatch(string text)=>string.IsNullOrEmpty(learningSearch.text)||(text??"").IndexOf(learningSearch.text,StringComparison.OrdinalIgnoreCase)>=0;
        void LearningRow(ref float y,string label,Action action,bool enabled=true,float height=46)
        {
            var b=FlatButton(learningContent,label,()=>action?.Invoke(),13,C.Ink,enabled?Color.white:new Color32(225,228,233,255));b.interactable=enabled&&action!=null;
            var r=(RectTransform)b.transform;r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.offsetMin=new Vector2(0,-y-height);r.offsetMax=new Vector2(0,-y);
            var t=b.GetComponentInChildren<TMP_Text>();t.richText=false;t.alignment=TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Ellipsis;
            UiTip.Add(r,()=>label);AddFocus(b.gameObject);y+=height+4;
        }
        void DrawLearningRows()
        {
            if(learningContent==null||learningMode.Length==0)return;
            for(int i=learningContent.childCount-1;i>=0;i--){var old=learningContent.GetChild(i).gameObject;old.SetActive(false);Destroy(old);}
            var sim=XgYyTalk.LearningSim;if(sim==null)return;sim.RepairYyLearning();float y=0;
            if(learningMode=="book")
            {
                var grouped=new Dictionary<string,List<XgAskedEntry>>();var order=new List<string>();
                for(int i=sim.S.askedLog.Count-1;i>=0;i--){var e=sim.S.askedLog[i];if(e==null||!LearningMatch(e.question))continue;string bankId=e.bankId??"general";if(!grouped.TryGetValue(bankId,out var rows)){grouped.Add(bankId,rows=new List<XgAskedEntry>());order.Add(bankId);}if(rows.Count<50)rows.Add(e);}
                foreach(string bank in order)
                {
                    var dataset=XgCatalog.Dataset(bank);LearningRow(ref y,dataset!=null?GameText.T(dataset.name,dataset.nameEn):bank,null,false,26);
                    foreach(var e in grouped[bank])
                    {
                        string label=e.question+"\n"+GameText.T("你：","You: ")+e.playerAnswer+(string.IsNullOrEmpty(e.aiAnswer)?"":" · "+sim.AiDisplayName+": "+e.aiAnswer);
                        LearningRow(ref y,label,()=>{if(XgYyTalk.AskFromNotebook(e))CloseLearningDrawer();});
                    }
                }
                if(order.Count==0)LearningRow(ref y,GameText.T("先去「教」答几题，题目就会留在这里。","Answer in Teach to collect questions here."),null,false,58);
            }
            else foreach(var pack in XgSim.PersonaPacks)
            {
                bool owned=sim.OwnsPersonaPack(pack.id);LearningRow(ref y,GameText.T(pack.name,pack.nameEn)+(owned?"":" · ¥"+pack.price.ToString("0")),owned?(Action)null:()=>{CloseLearningDrawer();XgYyTalk.OpenLearningTech();},!owned,32);
                if(!owned){LearningRow(ref y,GameText.T("去科技购买 · "+pack.desc,"Buy in Tech · "+pack.descEn),()=>{CloseLearningDrawer();XgYyTalk.OpenLearningTech();},true,38);continue;}
                foreach(var q in pack.questions)
                {
                    string label=GameText.T(q.q,q.qEn);if(!LearningMatch(label))continue;
                    bool ready=sim.S.stage>=q.stage;LearningRow(ref y,label+(ready?"":GameText.T(" · 第 "+q.stage+" 阶段"," · Stage "+q.stage)),()=>{if(XgYyTalk.AskPersona(pack,q))CloseLearningDrawer();},ready);
                }
            }
            learningContent.sizeDelta=new Vector2(0,y);
        }
        void RandomNotebook()
        {
            var sim=XgYyTalk.LearningSim;if(sim==null)return;
            if(learningMode=="book")
            {
                var rows=sim.S.askedLog.FindAll(e=>e!=null&&LearningMatch(e.question));if(rows.Count>0&&XgYyTalk.AskFromNotebook(rows[UnityEngine.Random.Range(0,rows.Count)]))CloseLearningDrawer();
            }
            else
            {
                var rows=new List<(XgPersonaPack pack,XgPersonaQuestion q)>();foreach(var p in XgSim.PersonaPacks)if(sim.OwnsPersonaPack(p.id))foreach(var q in p.questions)if(sim.S.stage>=q.stage&&LearningMatch(GameText.T(q.q,q.qEn)))rows.Add((p,q));
                if(rows.Count>0){var row=rows[UnityEngine.Random.Range(0,rows.Count)];if(XgYyTalk.AskPersona(row.pack,row.q))CloseLearningDrawer();}
            }
        }
        float LearningBubble(YYMessage message,float y,float width)
        {
            if(message.from!=YYChatHub.LingGuangId)return 0;float used=0;
            if(message.learningComparison!=0)
            {
                string note=message.learningComparison>0?GameText.T("和你教的一样 ✓","Matches what you taught ✓"):GameText.T("和你教的不一样 ✗","Differs from what you taught ✗");
                Text(Rect("NotebookComparison",scrollContent,new Vector2(0,1),Vector2.one,new Vector2(60,-y-22),new Vector2(-16,-y)),note,12,C.Muted,TextAlignmentOptions.MidlineLeft);used=26;
            }
            if(!message.learningPersona||message.learningReplyId<=0)return used;
            var sim=XgYyTalk.LearningSim;var reply=sim?.S.yyReplies?.Find(x=>x.id==message.learningReplyId);if(reply==null)return used;
            float col=Mathf.Max(64,(width-82)/3f);int index=0;
            foreach(var tag in XgSim.PersonaTags)
            {
                bool chosen=reply.tags!=null&&reply.tags.Contains(tag.id);string text=(chosen?"✓ ":"")+GameText.T(tag.name,tag.nameEn);
                var b=FlatButton(scrollContent,text,()=>{if(XgYyTalk.TagLearningReply(message.learningReplyId,tag.id,out var feedback)){lastSignature="";dirty=true;StartCoroutine(FloatUp(feedback,tag.delta>=0));}},12,C.Ink,chosen?new Color32(214,240,226,255):Color.white);
                b.interactable=!chosen&&(reply.tags==null||reply.tags.Count<2);var rt=(RectTransform)b.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);int row=index/3,column=index%3;
                rt.offsetMin=new Vector2(60+column*col,-y-used-row*26-23);rt.offsetMax=new Vector2(60+(column+1)*col-4,-y-used-row*26);AddFocus(b.gameObject);index++;
            }
            return used+78;
        }
    }
}
