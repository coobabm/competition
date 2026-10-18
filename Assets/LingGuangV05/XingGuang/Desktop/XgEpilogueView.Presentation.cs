using System;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    // Display snapshots only. The director supplies records already accepted by public-context gates.
    public sealed class XgEpilogueCard
    {
        public string title="", excerpt="", caption="";
    }

    public sealed partial class XgEpilogueView
    {
        Image backdrop;
        RectTransform calendar, reactions;
        TMP_Text month, day, calendarNote, closureRule;
        readonly RectTransform[] cardRects=new RectTransform[3];
        readonly CanvasGroup[] cardGroups=new CanvasGroup[3];
        readonly TMP_Text[] cardTitles=new TMP_Text[3],cardBodies=new TMP_Text[3],cardCaptions=new TMP_Text[3];
        readonly Vector2[] cardPositions=new Vector2[3];
        string motion="",finalHeading="",finalDate="";
        float elapsed,duration;
        bool motionEnglish,reduced,completionVisual,historical;
        int calendarFrame=-1,cardCount;
        public bool TransitionPlaying=>motion.Length>0&&isActiveAndEnabled&&gameObject.activeInHierarchy;
        public bool CompletionVisualActive=>completionVisual&&isActiveAndEnabled&&gameObject.activeInHierarchy;

        void BuildPresentation(RectTransform root,TMP_FontAsset font)
        {
            backdrop=root.GetComponent<Image>();var ui=new XgUi(font,null,true);
            heading.richText=body.richText=date.richText=false;
            calendar=XgUi.Rect("EpilogueCalendar",root,new Vector2(.23f,.29f),new Vector2(.77f,.72f),Vector2.zero,Vector2.zero);
            XgUi.Panel(calendar,new Color32(242,239,230,255));
            month=ui.Text(XgUi.Rect("Month",calendar,new Vector2(0,.68f),Vector2.one,new Vector2(16,0),new Vector2(-16,-6)),"",26,new Color32(62,70,83,255),TextAlignmentOptions.Center);
            day=ui.Text(XgUi.Rect("Day",calendar,new Vector2(0,.23f),new Vector2(1,.72f),Vector2.zero,Vector2.zero),"",64,new Color32(32,42,56,255),TextAlignmentOptions.Center);
            calendarNote=ui.Text(XgUi.Rect("Caption",calendar,Vector2.zero,new Vector2(1,.24f),new Vector2(12,8),new Vector2(-12,0)),"",14,new Color32(62,70,83,255),TextAlignmentOptions.Center);
            month.richText=day.richText=calendarNote.richText=false;
            reactions=XgUi.Rect("PublicReactionCards",root,Vector2.zero,Vector2.one,new Vector2(32,132),new Vector2(-32,-132));
            for(int i=0;i<3;i++)
            {
                var card=XgUi.Rect("PublicCard"+i,reactions,new Vector2(0,1-(i+1)/3f),new Vector2(1,1-i/3f),new Vector2(0,5),new Vector2(0,-5));
                cardRects[i]=card;cardPositions[i]=card.anchoredPosition;
                XgUi.Panel(card,new Color32(24,35,48,255));cardGroups[i]=card.gameObject.AddComponent<CanvasGroup>();
                cardTitles[i]=ui.Text(XgUi.Rect("Title",card,new Vector2(0,.69f),Vector2.one,new Vector2(14,0),new Vector2(-14,-4)),"",18,XgDark.Ink,TextAlignmentOptions.MidlineLeft);
                cardBodies[i]=ui.Text(XgUi.Rect("Excerpt",card,new Vector2(0,.23f),new Vector2(1,.7f),new Vector2(14,0),new Vector2(-14,0)),"",16,XgDark.Ink,TextAlignmentOptions.TopLeft);
                cardCaptions[i]=ui.Text(XgUi.Rect("SourceKind",card,Vector2.zero,new Vector2(1,.23f),new Vector2(14,3),new Vector2(-14,0)),"",12,XgDark.Muted,TextAlignmentOptions.MidlineLeft);
                foreach(var text in new[]{cardTitles[i],cardBodies[i],cardCaptions[i]}){text.richText=false;text.enableAutoSizing=true;text.fontSizeMin=11;}
            }
            closureRule=ui.Text(XgUi.Rect("SavedShutdownRule",root,Vector2.zero,Vector2.one,new Vector2(32,132),new Vector2(-32,-132)),"",18,new Color32(143,52,43,255),TextAlignmentOptions.BottomLeft);
            closureRule.richText=false;closureRule.raycastTarget=false;
            ResetPresentation();
        }
        void ResetPresentation()
        {
            motion="";elapsed=duration=0;completionVisual=false;calendarFrame=-1;
            if(calendar!=null){calendar.gameObject.SetActive(false);calendar.localScale=Vector3.one;}
            if(reactions!=null)reactions.gameObject.SetActive(false);
            if(closureRule!=null)closureRule.gameObject.SetActive(false);
            if(backdrop!=null)backdrop.color=new Color32(9,14,21,255);
            if(body!=null){body.gameObject.SetActive(true);body.color=XgDark.Ink;body.rectTransform.offsetMin=new Vector2(32,132);}
            if(heading!=null)heading.color=XgDark.Ink;
            if(date!=null)date.color=XgDark.Muted;
            if(error!=null)error.gameObject.SetActive(true);
        }
        void ConfigurePresentation(EpilogueState ep,bool english,bool reduceFx,XgEpilogueCard[] cards)
        {
            motionEnglish=english;reduced=reduceFx;historical=ep.historicalReview;
            finalHeading=heading.text;finalDate=date.text;
            if(ep.phase=="date")
            {
                // This is a presentation anchor, not a simulation clock assignment.
                calendar.gameObject.SetActive(true);body.gameObject.SetActive(false);
                SetCalendar(6);heading.text=english?"Another morning":"又一个早晨";
                if(!reduced)StartPresentation("date",historical?1:7);
            }
            else if(ep.phase=="headline"&&!reduced)StartPresentation("headline",.8f);
            else if(ep.phase=="route"&&cards!=null&&cards.Length>0)
            {
                cardCount=Math.Min(3,cards.Length);reactions.gameObject.SetActive(true);body.gameObject.SetActive(false);
                for(int i=0;i<3;i++)
                {
                    cardRects[i].gameObject.SetActive(i<cardCount);if(i>=cardCount)continue;
                    cardTitles[i].text=cards[i].title;cardBodies[i].text=cards[i].excerpt;cardCaptions[i].text=cards[i].caption;
                    cardGroups[i].alpha=1;cardRects[i].anchoredPosition=cardPositions[i];
                }
                if(!reduced)StartPresentation("cards",2);
            }
        }
        void StartPresentation(string kind,float seconds)
        {
            motion=kind;elapsed=0;duration=seconds;
            next.Set(motionEnglish?"Skip animation":"跳过动画",true);
            ApplyPresentation(0);
        }
        void AdvancePresentation()
        {
            if(TransitionPlaying||completionVisual){FinishPresentation();return;}
            nextAction?.Invoke();
        }
        public void SetReducedMotion(bool value)
        {
            if(reduced==value)return;reduced=value;
            if(!value||motion.Length==0)return;
            if(completionVisual)ApplyPresentation(1);else FinishPresentation();
        }
        void SetCalendar(int frame)
        {
            if(frame==calendarFrame)return;calendarFrame=frame;
            if(frame==0){month.text=motionEnglish?"DECEMBER 2016":"2016 年 12 月";day.text="31";}
            else
            {
                string[] months={"","JANUARY","FEBRUARY","MARCH","APRIL","MAY","JUNE"};
                month.text=motionEnglish?months[frame]+" 2017":"2017 年 "+frame+" 月";
                day.text=frame==6?"13":"—";
            }
            calendarNote.text=frame==6?(motionEnglish?"Morning":"清晨"):(motionEnglish?"Several months later":"几个月后");
        }
        static Color Alpha(Color color,float value){color.a=Mathf.Clamp01(value);return color;}
        void ApplyPresentation(float t)
        {
            if(motion=="date")
            {
                if(historical)
                {heading.text=motionEnglish?"Historical view":"历史回看";date.text=finalDate;SetCalendar(6);backdrop.color=Alpha(new Color32(9,14,21,255),Mathf.Lerp(.8f,1,t));return;}
                bool turning=elapsed>=4;calendar.gameObject.SetActive(turning);
                heading.text=turning?(motionEnglish?"Another morning":"又一个早晨"):(motionEnglish?"Several months later":"几个月后");
                date.text=turning?finalDate:(motionEnglish?"31 DECEMBER 2016":"2016 年 12 月 31 日");
                backdrop.color=Alpha(new Color32(9,14,21,255),Mathf.Clamp01(elapsed/1.2f));
                if(turning)
                {
                    float progress=Mathf.Clamp01((elapsed-4)/3);SetCalendar(Mathf.Min(6,Mathf.FloorToInt(progress*7)));
                    float turn=progress>=1?1:Mathf.Repeat(progress*7,1);
                    calendar.localScale=new Vector3(1,.94f+.06f*Mathf.Sin(turn*Mathf.PI),1);
                }
            }
            else if(motion=="headline")
            {heading.color=Alpha(XgDark.Ink,t);body.color=Alpha(XgDark.Ink,t);}
            else if(motion=="cards")
            {
                for(int i=0;i<cardCount;i++)
                {float entry=Mathf.Clamp01((elapsed-i*.65f)/.7f);cardGroups[i].alpha=entry;cardRects[i].anchoredPosition=cardPositions[i]+Vector2.down*(1-entry)*12;}
            }
            else if(motion=="complete")
            {
                float opacity=reduced?1:Mathf.Clamp01(elapsed/.65f);
                heading.color=Alpha(new Color32(35,42,51,255),opacity);body.color=Alpha(new Color32(48,55,64,255),opacity);
                closureRule.color=Alpha(new Color32(143,52,43,255),opacity);
            }
        }
        void FinishPresentation()
        {
            if(motion=="date")
            {calendar.gameObject.SetActive(true);calendar.localScale=Vector3.one;SetCalendar(6);date.text=finalDate;heading.text=historical?(motionEnglish?"Historical view":"历史回看"):finalHeading;backdrop.color=new Color32(9,14,21,255);}
            else if(motion=="headline"){heading.color=body.color=XgDark.Ink;}
            else if(motion=="cards")for(int i=0;i<cardCount;i++){cardGroups[i].alpha=1;cardRects[i].anchoredPosition=cardPositions[i];}
            else if(motion=="complete"){elapsed=duration;ApplyPresentation(1);completionVisual=false;}
            motion="";next.Set(motionEnglish?"Continue":"继续",true);
        }
        // Called after the durable completion event; this never creates or changes a save receipt.
        public void ShowCompletion(string playerLine,string aiLine,string pinnedRule,bool english,bool reduceFx)
        {
            ResetPresentation();gameObject.SetActive(true);transform.SetAsLastSibling();
            motionEnglish=english;reduced=reduceFx;completionVisual=true;
            nextAction=inboxAction=backAction=browseAction=null;
            next.Show(true);inbox.Show(false);back.Show(false);browse.Show(false);error.gameObject.SetActive(false);
            backdrop.color=new Color32(250,249,245,255);date.text="";
            heading.text=english?"This is where this chapter ends.":"这一段，写到这里。";
            body.text="";
            if(!string.IsNullOrWhiteSpace(playerLine))body.text=(english?"Your last line\n":"你写下的最后一句\n")+playerLine;
            if(!string.IsNullOrWhiteSpace(aiLine))body.text+=(body.text.Length>0?"\n\n":"")+(english?"Its final line\n":"它留下的最后一句\n")+aiLine;
            closureRule.text=string.IsNullOrWhiteSpace(pinnedRule)?"":(english?"Pinned shutdown card · ":"钉下的关机卡 · ")+pinnedRule;
            closureRule.gameObject.SetActive(closureRule.text.Length>0);
            if(closureRule.text.Length>0)body.rectTransform.offsetMin=new Vector2(32,200);
            StartPresentation("complete",3);next.Set(english?"Return to title":"返回标题",true);
        }
        void Update()
        {
            if(motion.Length==0)return;
            elapsed+=Time.unscaledDeltaTime;
            ApplyPresentation(Mathf.Clamp01(elapsed/duration));
            if(elapsed>=duration)FinishPresentation();
        }
        void OnDisable(){ResetPresentation();}
    }
}
