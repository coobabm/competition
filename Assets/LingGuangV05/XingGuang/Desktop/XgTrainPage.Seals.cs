using System;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTrainPage
    {
        RectTransform sealViewport, sealContent, sealHeader, holdoutSlot, checkpointSlot, rateSlot, monitorSlot, weightsSlot;
        RectTransform paceRegion, moduleScrollbar;
        ScrollRect moduleScroll;
        XgTrainingSeal[] moduleSeals;
        bool layingOutSeals;

        // Each seal belongs to the tool it will reveal. The train action and pace remain
        // outside the scroll viewport, so inspecting lower modules never loses the action.
        void BuildTrainingSeals()
        {
            sealViewport=Rect("TrainingModulesViewport",left,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            Panel(sealViewport,Color.clear).raycastTarget=true;
            sealViewport.gameObject.AddComponent<RectMask2D>();
            sealContent=Rect("TrainingModules",sealViewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);
            sealContent.pivot=new Vector2(.5f,1);
            moduleScroll=sealViewport.gameObject.AddComponent<ScrollRect>();
            moduleScroll.viewport=sealViewport;moduleScroll.content=sealContent;
            moduleScroll.horizontal=false;moduleScroll.vertical=true;moduleScroll.scrollSensitivity=28;
            moduleScroll.movementType=ScrollRect.MovementType.Clamped;
            moduleScrollbar=Rect("TrainingModulesScrollbar",left,new Vector2(1,0),Vector2.one,new Vector2(-8,0),Vector2.zero);
            Panel(moduleScrollbar,new Color(1,1,1,.035f));
            var handle=Rect("Handle",moduleScrollbar,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var handleImage=Panel(handle,new Color(1,1,1,.24f));
            var scrollbar=moduleScrollbar.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect=handle;scrollbar.targetGraphic=handleImage;scrollbar.direction=Scrollbar.Direction.BottomToTop;
            moduleScroll.verticalScrollbar=scrollbar;
            sealHeader=Rect("ModelAndCheckpoint",sealContent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            summary.rt.SetParent(sealHeader,false);saveModel.rt.SetParent(sealHeader,false);
            foreach(var tab in trackTabs)tab.rt.SetParent(sealHeader,false);
            curveCard.SetParent(sealContent,false);
            checkpointSlot=Rect("CheckpointModule",sealHeader,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            holdoutSlot=Rect("HoldoutModule",curveCard,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            weightsSlot=Rect("WeightsModule",chartArea,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            rateSlot=Rect("RateModule",trainRow,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            paceRegion=(RectTransform)risk.transform.parent;
            monitorSlot=Rect("MonitorModule",paceRegion,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            moduleSeals=new[] {
                XgTrainingSeal.ForModule(holdoutSlot,ui,view,"holdout","train.holdout","看看它能否答对新题","Check its answers on unseen questions"),
                XgTrainingSeal.ForModule(rateSlot,ui,view,"rate","shared.lr","选择每轮迈多大一步","Choose the step size for each round"),
                XgTrainingSeal.ForModule(checkpointSlot,ui,view,"checkpoint","train.checkpoint","回到表现最好的记录","Restore the best available checkpoint"),
                XgTrainingSeal.ForModule(monitorSlot,ui,view,"monitor","train.monitor","先看看这一轮的风险","Preview the risk of this round"),
                XgTrainingSeal.ForModule(weightsSlot,ui,view,"weights","train.weights","看神经元如何学会规律","Watch neurons learn patterns")
            };
            sealViewport.gameObject.AddComponent<XgTrainingModuleLayout>().Changed=LayoutTrainingSeals;
            trainRow.SetAsLastSibling();
        }

        // No Refresh, searches or geometry work in Update. Size changes and actual page
        // refreshes update these rectangles; the viewport clips rather than shrinking text.
        void LayoutTrainingSeals()
        {
            if(layingOutSeals||moduleSeals==null)return;
            layingOutSeals=true;
            try
            {
                bool checkpoint=Sim.TrainPart("checkpoint"),rate=Sim.TrainPart("rate"),monitor=Sim.TrainPart("monitor"),holdout=Sim.TrainPart("holdout");
                float headerHeight=checkpoint?TabsHeight:XgTrainingSeal.MinimumHeight;
                float rateHeight=rate?RateStripHeight:XgTrainingSeal.MinimumHeight;
                float paceHeight=monitor?112:52+XgTrainingSeal.MinimumHeight;
                float controlsHeight=paceHeight+rateHeight+RateStripGap;
                trainRow.offsetMin=Vector2.zero;trainRow.offsetMax=new Vector2(0,controlsHeight);
                train.rt.offsetMax=new Vector2(240,-rateHeight-RateStripGap);
                paceRegion.offsetMax=new Vector2(-8,-rateHeight-RateStripGap);
                TopBlock(rateSlot,0,rateHeight,0,0);
                TopBlock(monitorSlot,52,XgTrainingSeal.MinimumHeight,0,0);
                sealViewport.offsetMin=new Vector2(0,controlsHeight+RowGap);
                sealViewport.offsetMax=new Vector2(-12,0);
                moduleScrollbar.offsetMin=new Vector2(-8,controlsHeight+RowGap);
                TopBlock(sealHeader,0,headerHeight,0,0);
                checkpointSlot.anchorMin=checkpointSlot.anchorMax=Vector2.one;
                checkpointSlot.offsetMin=new Vector2(-240,-headerHeight);checkpointSlot.offsetMax=Vector2.zero;
                summary.rt.offsetMax=new Vector2(checkpoint?-108:-248,0);
                // Numbers, a local validation placeholder, answer sheet, then curve and
                // its local weight diagram. No seal covers the paid base curve or sheet.
                float holdoutHeight=holdout?0:XgTrainingSeal.MinimumHeight+8;
                float curveHeight=Mathf.Max(440+holdoutHeight,sealViewport.rect.height-headerHeight-RowGap);
                float contentHeight=headerHeight+RowGap+curveHeight;
                sealContent.sizeDelta=new Vector2(0,contentHeight);
                moduleScrollbar.gameObject.SetActive(contentHeight>sealViewport.rect.height+.5f);
                TopBlock(curveCard,headerHeight+RowGap,curveHeight,0,0);
                TopBlock(holdoutSlot,32,XgTrainingSeal.MinimumHeight,12,12);
                TopBlock(sheetArea,32+holdoutHeight,180,12,12);
                chartArea.anchorMin=Vector2.zero;chartArea.anchorMax=Vector2.one;
                chartArea.offsetMin=new Vector2(58,30);chartArea.offsetMax=new Vector2(-12,-220-holdoutHeight);
                diagnosticArea.anchorMin=Vector2.zero;diagnosticArea.anchorMax=new Vector2(1,0);
                diagnosticArea.offsetMin=new Vector2(5,3);diagnosticArea.offsetMax=new Vector2(-5,90);
                weightsSlot.anchorMin=Vector2.zero;weightsSlot.anchorMax=new Vector2(1,0);
                weightsSlot.offsetMin=new Vector2(5,3);weightsSlot.offsetMax=new Vector2(-5,90);
                chart.rectTransform.anchorMin=Vector2.zero;chart.rectTransform.anchorMax=Vector2.one;
                chart.rectTransform.offsetMin=new Vector2(4,96);chart.rectTransform.offsetMax=new Vector2(-4,-4);
                foreach(var seal in moduleSeals)seal.RefreshState();
            }
            finally {layingOutSeals=false;}
        }
        static void TopBlock(RectTransform rect,float top,float height,float insetLeft,float insetRight)
        {
            rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;
            rect.offsetMin=new Vector2(insetLeft,-top-height);rect.offsetMax=new Vector2(-insetRight,-top);
        }
    }

    public sealed class XgTrainingModuleLayout : MonoBehaviour
    {
        public Action Changed;
        void OnRectTransformDimensionsChange(){Changed?.Invoke();}
    }
}
