using System;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatView
    {
        RectTransform introInput;string introUiKey="";
        void RefreshGroupIntro()
        {
            if(root==null||hub?.S==null)return;
            bool active=hub.S.selected==YYChatHub.EraGroup&&hub.IntroPlaying;
            if(input!=null)input.interactable=!active;
            if(!active)
            {if(introInput!=null){introInput.gameObject.SetActive(false);Destroy(introInput.gameObject);introInput=null;}introUiKey="";return;}
            var host=hub.IntroHost;bool held=host==null||host.Held;
            choiceStrip.gameObject.SetActive(false);
            string key=hub.IntroRevision+":"+GameText.LanguageId+":"+held;
            if(introInput!=null&&introUiKey==key)return;
            if(introInput!=null){introInput.gameObject.SetActive(false);Destroy(introInput.gameObject);}
            introUiKey=key;var parent=root.Find("Conversation");if(parent==null)return;
            introInput=Rect("GroupIntroChoices",parent,Vector2.zero,Vector2.right,Vector2.zero,new Vector2(0,BottomHeight+InputHeight+ToolHeight));
            Fill(introInput,new Color32(236,244,255,255));
            var panel=introInput;var state=hub.S;var simulation=hub.runtime.Sim;int revision=hub.IntroRevision;
            bool Current()=>ReferenceEquals(panel,introInput)&&panel!=null&&panel.gameObject.activeInHierarchy
                &&ReferenceEquals(state,hub.S)&&ReferenceEquals(simulation,hub.runtime.Sim)&&hub.IntroRevision==revision&&hub.IntroPlaying&&hub.IntroHost?.Held==false;
            if(held){Text(Rect("Held",introInput,Vector2.zero,Vector2.one,new Vector2(14,10),new Vector2(-14,-10)),GameText.T("稍后继续这段群聊。","This conversation will continue shortly."),15,C.Muted,TextAlignmentOptions.Center);return;}
            var choices=host.Choices();float height=choices.Length==0?30:Mathf.Min(40,(InputHeight+ToolHeight-8)/choices.Length);
            if(choices.Length==0)Text(Rect("Waiting",introInput,Vector2.zero,Vector2.one,new Vector2(14,50),new Vector2(-14,-8)),
                GameText.T(host.Typing?"群友正在输入…":"继续阅读心里话，或跳过这段介绍。",host.Typing?"Someone is typing…":"Continue the thought, or skip this introduction."),15,C.Muted,TextAlignmentOptions.Center);
            for(int i=0;i<choices.Length;i++)
            {
                string value=choices[i];var button=FlatButton(introInput,value,()=>{if(Current()){hub.Send(YYChatHub.EraGroup,value);dirty=true;stickBottom=true;}},14,C.Ink,Color.white);
                var rt=(RectTransform)button.transform;rt.anchorMin=new Vector2(0,1);rt.anchorMax=Vector2.one;
                rt.offsetMin=new Vector2(12,-8-(i+1)*height);rt.offsetMax=new Vector2(-12,-12-i*height);
                var label=button.GetComponentInChildren<TMP_Text>();label.richText=false;label.textWrappingMode=TextWrappingModes.Normal;
            }
            var skip=FlatButton(introInput,GameText.T("跳过介绍","Skip introduction"),()=>{if(Current()){host.Skip(revision);dirty=true;stickBottom=true;}},13,C.Muted,Color.white);
            Place((RectTransform)skip.transform,new Vector2(1,0),new Vector2(-178,8),new Vector2(-12,38));
        }
        float IntroLinkCard(float y,YYMessage message,float width)
        {
            const float height=106;float w=Mathf.Max(180,Mathf.Min(430,width-44));
            var card=Rect("GroupAppLink",scrollContent,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-y-height),new Vector2(18+w,-y));
            Fill(card,Color.white);var outline=card.gameObject.AddComponent<Outline>();outline.effectColor=new Color32(82,149,222,255);outline.effectDistance=new Vector2(1,-1);
            Text(Rect("Sender",card,new Vector2(0,1),Vector2.one,new Vector2(12,-38),new Vector2(-12,-8)),message.text,13,C.Ink,TextAlignmentOptions.TopLeft).richText=false;
            var simulation=hub.runtime.Sim;var state=hub.S;
            var link=FlatButton(card,GameText.T(message.introLabelZh,message.introLabelEn),()=>
            {if(card!=null&&card.gameObject.activeInHierarchy&&ReferenceEquals(state,hub.S)&&ReferenceEquals(simulation,hub.runtime.Sim))hub.OpenIntroLink(message);},15,new Color32(28,100,186,255),new Color32(232,243,255,255));
            var rt=(RectTransform)link.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(10,12);rt.offsetMax=new Vector2(-10,-42);
            link.GetComponentInChildren<TMP_Text>().richText=false;return height+12;
        }
    }
}
