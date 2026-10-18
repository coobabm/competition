using System;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgLabelPage
    {
        RectTransform classicThresholdRoot;
        Slider classicThreshold;
        TMP_Text classicThresholdText;
        XgCard classicToolCard;
        XgSim classicToolOwner;
        void HideClassicTeachingTools()
        {
            if(classicThresholdRoot!=null)classicThresholdRoot.gameObject.SetActive(false);
            classicToolCard=null;classicToolOwner=null;
        }
        void RefreshClassicTeachingTools(XgCard card,bool queued)
        {
            if(WorkContext||Mode!=0||queued||!XgSim.DirectTruthJudgment(card)||!XgSim.BinaryToolDataset(card.dataset))return;
            classicToolCard=card;classicToolOwner=Sim;
            if(Sim.TeachingAiReady)
            {
                var p=Sim.PredictTeachingBinary(card);
                string answer=p.guessed&&Math.Abs(Sim.TeachingThreshold(card.dataset)-.5)<.001?T("……不知道","…not sure"):YesNoWord(p.yes);
                suggestion.text=Sim.AiDisplayName+T("：",": ")+answer+(Sim.Has("sigmoid")?" · "+Math.Round(p.confidence*100)+"%":"");
                suggestion.enableAutoSizing=false;suggestion.fontSize=18;
                suggestion.rectTransform.offsetMin=new Vector2(12,-342);suggestion.rectTransform.offsetMax=new Vector2(-12,-300);
                if(Sim.Has("label.hard"))foreach(var review in Sim.TeachingReviewTray(false))if(review.dataset==card.dataset&&review.text==card.question){suggestion.text+=" · "+T("复核","Review");break;}
            }
            if(!Sim.Has("bias"))return;
            if(classicThresholdRoot==null)
            {
                classicThresholdRoot=Rect("ClassicDecisionTools",body,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-215,-364),new Vector2(215,-343));
                classicThresholdText=ui.Text(Rect("ThresholdLabel",classicThresholdRoot,Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(118,0)),"",13,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
                var track=Rect("ClassicDecisionThreshold",classicThresholdRoot,Vector2.zero,Vector2.one,new Vector2(126,5),new Vector2(0,-5));
                var background=Panel(track,new Color(.78f,.81f,.86f));background.raycastTarget=true;
                var handle=Rect("Handle",track,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(14,20));
                var image=Panel(handle,XgPalette.Accent);image.raycastTarget=true;
                classicThreshold=track.gameObject.AddComponent<Slider>();classicThreshold.handleRect=handle;classicThreshold.targetGraphic=image;classicThreshold.minValue=.05f;classicThreshold.maxValue=.95f;
                classicThreshold.onValueChanged.AddListener(value=>{
                    if(classicToolOwner!=Sim||classicToolCard==null||classicToolCard.id!=CurrentCard.id||!classicThresholdRoot.gameObject.activeInHierarchy)return;
                    if(Sim.SetTeachingThreshold(classicToolCard.dataset,value))Refresh();
                });
            }
            classicThresholdRoot.gameObject.SetActive(true);
            classicThreshold.SetValueWithoutNotify((float)Sim.TeachingThreshold(card.dataset));
            classicThresholdText.text=T("门槛 ","Threshold ")+Math.Round(Sim.TeachingThreshold(card.dataset)*100)+"%";
        }
    }
}
