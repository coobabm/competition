using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.Zhongbao;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgLabelPage
    {
        XgBtn uncertainSort,suspectTray,hardTray;
        RectTransform teachingReviewOverlay,teachingToolsViewport;
        XgSim reviewToolOwner;
        readonly Dictionary<string,double> teachingSortScores=new Dictionary<string,double>();
        void BuildTeachingHallTools(RectTransform viewport)
        {
            teachingToolsViewport=viewport;
            uncertainSort=ui.Button(hallBox,"",()=>{Sim.SetTeachingUncertainFirst(!Sim.TeachingUncertainFirst);teachingSortScores.Clear();RebuildTabs();},13);
            uncertainSort.rt.name="TeachUncertainSort";PlaceTopLeft(uncertainSort,6,36,HallW-12,28);
            suspectTray=ui.Button(hallBox,"",()=>OpenTeachingReview(true),13);suspectTray.rt.name="TeachSuspectedTray";
            hardTray=ui.Button(hallBox,"",()=>OpenTeachingReview(false),13);hardTray.rt.name="TeachHardTray";
        }
        void RefreshTeachingHallTools()
        {
            if(uncertainSort==null)return;
            if(teachingReviewOverlay!=null&&reviewToolOwner!=Sim)CloseTeachingReview();
            bool show=!WorkContext;float y=36;
            bool sort=show&&Sim.Has("sigmoid");uncertainSort.rt.gameObject.SetActive(sort);
            if(sort){PlaceTopLeft(uncertainSort,6,y,HallW-12,28);uncertainSort.Set(Sim.TeachingUncertainFirst?T("✓ 先教拿不准的","✓ Unsure first"):T("先教拿不准的","Unsure first"),true);y+=32;}
            bool clean=show&&Sim.Has("dataclean");suspectTray.rt.gameObject.SetActive(clean);
            if(clean){int n=Sim.TeachingReviewCount(true);PlaceTopLeft(suspectTray,6,y,HallW-12,28);suspectTray.Set(T("? 疑似错题 ","? Suspected labels ")+n,n>0);y+=32;}
            bool hard=show&&Sim.Has("label.hard");hardTray.rt.gameObject.SetActive(hard);
            if(hard){int n=Sim.TeachingReviewCount(false);PlaceTopLeft(hardTray,6,y,HallW-12,28);hardTray.Set(T("复核 ","Review ")+n,n>0);y+=32;}
            teachingToolsViewport.offsetMax=new Vector2(-1,-y);
        }
        double TeachingSortScore(string dataset)
        {
            if(!teachingSortScores.TryGetValue(dataset,out double score))teachingSortScores[dataset]=score=Sim.TeachingDeskUncertainty(dataset);return score;
        }
        void CloseTeachingReview()
        {
            if(teachingReviewOverlay!=null){teachingReviewOverlay.gameObject.SetActive(false);MiniGames.XgMiniContext.DestroyObject(teachingReviewOverlay.gameObject);teachingReviewOverlay=null;}
            reviewToolOwner=null;
        }
        void OpenTeachingReview(bool suspect)
        {
            CloseTeachingReview();var rows=Sim.TeachingReviewTray(suspect);if(rows.Count==0)return;
            var row=rows[0];var owner=Sim;reviewToolOwner=owner;
            teachingReviewOverlay=Rect("TeachingReviewOverlay",root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            Panel(teachingReviewOverlay,new Color(.94f,.96f,1,1)).raycastTarget=true;teachingReviewOverlay.SetAsLastSibling();
            string question=row.dataset=="spam"?T("这条是诈骗短信吗？","Is this a scam text?"):row.dataset=="review"?T("这条是刷单评论吗？","Is this a fake review?"):T("这条是标题党吗？","Is this clickbait?");
            ui.Text(Rect("Heading",teachingReviewOverlay,new Vector2(0,1),Vector2.one,new Vector2(25,-65),new Vector2(-25,-15)),suspect?T("? 疑似错题","? Suspected label"):T("复核","Review"),24,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
            ui.Text(Rect("Question",teachingReviewOverlay,Vector2.zero,Vector2.one,new Vector2(30,170),new Vector2(-30,-85)),T(row.text,row.textEn)+"\n\n"+question,24,XgPalette.Ink,TextAlignmentOptions.Center);
            var result=ui.Text(Rect("Result",teachingReviewOverlay,new Vector2(0,0),new Vector2(1,0),new Vector2(25,110),new Vector2(-25,160)),"",17,XgPalette.Ink,TextAlignmentOptions.Center);
            bool answered=false;Action<bool> submit=answer=>{
                if(answered||Sim!=owner||teachingReviewOverlay==null||!teachingReviewOverlay.gameObject.activeInHierarchy)return;
                if(!owner.ResolveTeachingReview(row.id,answer,out bool right))return;
                if(right){answered=true;result.text=T("已核验。", "Checked.")+" "+T(row.why,row.whyEn);RefreshTeachingHallTools();}
                else result.text=T("再看一眼题目。", "Take another look at the question.");
            };
            var yesReview=ui.Button(teachingReviewOverlay,T("是","Yes"),()=>submit(true),20);yesReview.rt.name="ReviewYes";yesReview.rt.anchorMin=yesReview.rt.anchorMax=new Vector2(.5f,0);yesReview.rt.offsetMin=new Vector2(-220,50);yesReview.rt.offsetMax=new Vector2(-20,100);
            var noReview=ui.Button(teachingReviewOverlay,T("否","No"),()=>submit(false),20);noReview.rt.name="ReviewNo";noReview.rt.anchorMin=noReview.rt.anchorMax=new Vector2(.5f,0);noReview.rt.offsetMin=new Vector2(20,50);noReview.rt.offsetMax=new Vector2(220,100);
            var close=ui.Button(teachingReviewOverlay,T("返回","Back"),CloseTeachingReview,16);close.rt.name="CloseTeachingReview";close.rt.anchorMin=close.rt.anchorMax=Vector2.one;close.rt.offsetMin=new Vector2(-125,-55);close.rt.offsetMax=new Vector2(-15,-15);
        }
    }
}
