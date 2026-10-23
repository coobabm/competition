using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XingGuangView
    {
        XgPersonaArchiveWindow personaArchive;
        public void OpenPersonaArchive()
        {
            if (!Visible || Sim == null || StoryFocus.Exclusive || SetupShowing || Tab != "home" || !FeatureOpen("home")) return;
            HideProfileMore();
            if (!Sim.PersonaArchiveUnlocked) { ShowTab("tree"); Tree.FocusNodeDetails(XgSim.PersonaArchiveNode); return; }
            if (personaArchive == null) personaArchive = XgPersonaArchiveWindow.Create(this, root, ui);
            personaArchive.Open();
        }
    }

    public sealed class XgPersonaArchiveWindow : MonoBehaviour
    {
        XingGuangView owner;
        XgSim bound;
        object world;
        TMP_Text heading, legend, history, axes;
        RectTransform content, viewport;
        XgBtn close;
        XgPersonaArchiveGraphic graphic;
        bool english;
        int revision = -1, day;
        float refreshIn;
        Vector2 measuredSize;
        bool layoutDirty;
        readonly double[] shownActual=new double[3],shownWish=new double[3];
        string T(string zh, string en) => english ? en : zh;
        static string Safe(string text) => (text ?? "").Replace("<", "‹").Replace(">", "›");
        bool Current => owner != null && bound != null && ReferenceEquals(bound, owner.Sim) && ReferenceEquals(world, owner.Controller.runtime?.Sim)
            && owner.Visible && owner.Tab == "home" && bound.PersonaArchiveUnlocked && !owner.SetupShowing && !StoryFocus.Exclusive;
        public static XgPersonaArchiveWindow Create(XingGuangView view, RectTransform parent, XgUi ui)
        {
            var root = Rect("Personality archive", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var window = root.gameObject.AddComponent<XgPersonaArchiveWindow>(); window.owner = view;
            Panel(root, new Color(0,0,0,.9f));
            var frame = ui.Card(root,"Personality archive frame",Vector2.zero,Vector2.one,new Vector2(18,18),new Vector2(-18,-18));
            window.heading = ui.Text(Strip("Title",frame,10,38,18,140),"",21,XgDark.Ink,TextAlignmentOptions.MidlineLeft);
            window.close = ui.Button(frame,"",window.Close,15); PlaceTopRight(window.close,126,12,108,34);
            var left = Rect("Personality chart",frame,Vector2.zero,new Vector2(.42f,1),new Vector2(16,62),new Vector2(-8,-56));
            window.graphic = left.gameObject.AddComponent<XgPersonaArchiveGraphic>();window.graphic.raycastTarget=false;
            window.axes = ui.Text(Rect("Axis labels",left,new Vector2(0,.49f),Vector2.one,Vector2.zero,Vector2.zero),"",13,XgDark.Ink,TextAlignmentOptions.TopLeft);
            window.axes.raycastTarget=false;
            window.axes.textWrappingMode=TextWrappingModes.Normal;
            window.legend = ui.Text(Rect("Chart legend",frame,Vector2.zero,new Vector2(.42f,0),new Vector2(18,10),new Vector2(-8,57)),"",12,XgDark.Muted,TextAlignmentOptions.TopLeft);
            window.legend.textWrappingMode=TextWrappingModes.Normal;
            window.viewport=Rect("History scroll",frame,new Vector2(.42f,0),Vector2.one,new Vector2(12,16),new Vector2(-18,-58));
            Panel(window.viewport,Color.clear);window.viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=window.viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.viewport=window.viewport;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;
            window.content=Rect("History content",window.viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);window.content.pivot=new Vector2(.5f,1);
            window.history=ui.Text(Rect("History text",window.content,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"",15,XgDark.Ink,TextAlignmentOptions.TopLeft);
            window.history.textWrappingMode=TextWrappingModes.Normal;scroll.content=window.content;
            view.Controller.Window.onClose.AddListener(window.Close);view.Controller.Window.onMinimize.AddListener(window.Close);
            root.gameObject.SetActive(false);return window;
        }
        public void Open() { bound=owner.Sim;world=owner.Controller.runtime?.Sim;revision=-1;gameObject.SetActive(true);transform.SetAsLastSibling();Refresh(); }
        public void Close() { gameObject.SetActive(false);bound=null;world=null; }
        void OnDestroy()
        {
            if(owner==null || owner.Controller==null || owner.Controller.Window==null)return;
            owner.Controller.Window.onClose.RemoveListener(Close);owner.Controller.Window.onMinimize.RemoveListener(Close);
        }
        void Update()
        {
            if(!Current){Close();return;}
            refreshIn-=Time.unscaledDeltaTime;if(refreshIn>0)return;refreshIn=.25f;
            if(revision!=bound.S.personaArchiveRevision || day!=bound.Today || english!=GameText.IsEnglish || AxesChanged())Refresh();
        }
        bool AxesChanged()
        {
            for(int i=0;i<3;i++)if(shownActual[i]!=bound.ActualAxis(i) || shownWish[i]!=bound.TargetAxis(i))return true;
            return false;
        }
        void LateUpdate()
        {
            if(bound==null)return;
            var size=viewport.rect.size;
            if(!layoutDirty && (size-measuredSize).sqrMagnitude<.25f)return;
            measuredSize=size;layoutDirty=false;
            float h=Mathf.Max(viewport.rect.height,history.GetPreferredValues(history.text,Mathf.Max(1,viewport.rect.width),0).y+12);
            if(Mathf.Abs(content.rect.height-h)>.5f)content.sizeDelta=new Vector2(0,h);
        }
        void Refresh()
        {
            english=GameText.IsEnglish;revision=bound.S.personaArchiveRevision;day=bound.Today;
            for(int i=0;i<3;i++){shownActual[i]=bound.ActualAxis(i);shownWish[i]=bound.TargetAxis(i);}
            heading.text=Safe(bound.AiDisplayName)+T(" · 性格档案"," · Personality archive");close.Set(T("关闭","Close"),true);
            var days=bound.PersonaArchiveDays;var reasons=bound.PersonaArchiveReasons;
            graphic.Set(bound,days);
            axes.text=T("温度（上） · 玩心（右） · 主见（左）","Warmth (top) · Play (right) · Opinion (left)");
            legend.text=T("青色：当前表现　灰色：开局愿望\n14日轨迹：青·温度　蓝·玩心　橙·主见","Cyan: current · Grey: opening wish\n14 days: cyan Warmth · blue Play · orange Opinion");
            var b=new StringBuilder();
            b.Append(T("<b>开局写下</b>\n","<b>Opening wish</b>\n")).Append(Safe(bound.Profile.personality)).Append("\n\n");
            for(int i=0;i<3;i++)b.Append(T(XgSim.AxisNames[i],XgSim.AxisNamesEn[i])).Append("  ").Append(bound.ActualAxis(i).ToString("0",CultureInfo.InvariantCulture)).Append(" / ").Append(bound.TargetAxis(i).ToString("0",CultureInfo.InvariantCulture)).Append('\n');
            b.Append(T("当前表现 / 开局愿望\n\n<b>变化日</b>\n","Current / opening wish\n\n<b>Days with changes</b>\n"));
            if(days.Count==0)b.Append(T("还没有记录；下一次性格变化会记在这里。\n","No records yet; the next personality change will appear here.\n"));
            foreach(var row in days)b.Append(Date(row.day)).Append("  ").Append(row.warmth).Append(" / ").Append(row.play).Append(" / ").Append(row.opinion).Append('\n');
            b.Append(T("\n<b>真实原因（最近在前）</b>\n","\n<b>Recorded causes (newest first)</b>\n"));
            if(reasons.Count==0)b.Append(T("下一次实际性格变化后会记在这里。","The next real personality change will appear here."));
            for(int i=reasons.Count-1;i>=0;i--)b.Append(Date(reasons[i].day)).Append(" · ").Append(Safe(bound.PersonaReasonText(reasons[i]))).Append("\n\n");
            history.text=b.ToString();layoutDirty=true;
        }
        string Date(int value) => value==0?T("日期未同步","Date unavailable"):value.ToString("0000-00-00",CultureInfo.InvariantCulture);
    }

    // Graphic itself only requires RectTransform. Runtime-created custom graphics
    // need a CanvasRenderer or UGUI silently skips their mesh rebuild/submission.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgPersonaArchiveGraphic : MaskableGraphic
    {
        double[] actual=new double[3],wish=new double[3];
        readonly List<XgPersonaDay> days=new List<XgPersonaDay>();
        static readonly Color[] Colors={new Color(.37f,.89f,.71f),new Color(.55f,.68f,1),new Color(1,.65f,.4f)};
        public void Set(XgSim sim,IReadOnlyList<XgPersonaDay> rows)
        {
            for(int i=0;i<3;i++){actual[i]=sim.ActualAxis(i);wish[i]=sim.TargetAxis(i);}days.Clear();foreach(var row in rows)days.Add(row);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;Vector2 center=new Vector2(r.center.x,r.yMin+r.height*.73f);
            float radius=Mathf.Min(r.width*.36f,r.height*.2f);
            Vector2[] edge={center+new Vector2(0,radius),center+new Vector2(radius*.866f,-radius*.5f),center+new Vector2(-radius*.866f,-radius*.5f)};
            for(int i=0;i<3;i++){Line(vh,center,edge[i],new Color(.4f,.45f,.55f,.4f),1);Line(vh,edge[i],edge[(i+1)%3],new Color(.4f,.45f,.55f,.5f),1);}
            for(int i=0;i<3;i++){int next=(i+1)%3;Line(vh,Vector2.Lerp(center,edge[i],(float)wish[i]/100),Vector2.Lerp(center,edge[next],(float)wish[next]/100),new Color(.65f,.68f,.75f),2);Line(vh,Vector2.Lerp(center,edge[i],(float)actual[i]/100),Vector2.Lerp(center,edge[next],(float)actual[next]/100),Colors[0],3);}
            Vector2 origin=new Vector2(r.xMin+10,r.yMin+15);float width=r.width-20,height=r.height*.35f;
            Line(vh,origin,origin+new Vector2(width,0),new Color(.4f,.45f,.55f),1);Line(vh,origin,origin+new Vector2(0,height),new Color(.4f,.45f,.55f),1);
            if(days.Count==0)return;
            DateTime first=Parse(days[0].day),last=Parse(days[days.Count-1].day);double span=Math.Max(1,(last-first).TotalDays);
            for(int axis=0;axis<3;axis++)for(int i=0;i<days.Count;i++)
            {
                Vector2 p=Point(days[i],axis,origin,width,height,first,span);Line(vh,p-Vector2.one*2,p+Vector2.one*2,Colors[axis],3);
                if(i>0)Line(vh,Point(days[i-1],axis,origin,width,height,first,span),p,Colors[axis],2);
            }
        }
        static DateTime Parse(int day) => DateTime.TryParseExact(day.ToString(),"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)?date:DateTime.MinValue;
        static Vector2 Point(XgPersonaDay day,int axis,Vector2 origin,float width,float height,DateTime first,double span)
            =>origin+new Vector2((float)((Parse(day.day)-first).TotalDays/span)*width,(float)(axis==0?day.warmth:axis==1?day.play:day.opinion)/100*height);
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,Color color,float width)
        {
            Vector2 n=(b-a).normalized; n=new Vector2(-n.y,n.x)*width*.5f;int start=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
