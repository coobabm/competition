using LingGuangV05.Core;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatView
    {
        RectTransform endingPrivatePreviewPanel;
        EndingPrivatePreview endingPrivatePreview;
        void CloseEndingPrivatePreview()
        {
            if (endingPrivatePreviewPanel != null) Destroy(endingPrivatePreviewPanel.gameObject);
            endingPrivatePreviewPanel = null; endingPrivatePreview = null;
        }
        void PollEndingPrivatePreview()
        {
            if (endingPrivatePreview == null) return;
            if (!IsOpen || hub == null || hub.S?.selected != YYChatHub.LaoZhou || !hub.CurrentEndingPrivate(endingPrivatePreview)
                || Keyboard.current?.escapeKey.wasPressedThisFrame == true) CloseEndingPrivatePreview();
            if(endingPrivatePreviewPanel==null||Keyboard.current==null)return;
            var scroll=endingPrivatePreviewPanel.GetComponentInChildren<ScrollRect>();
            if(scroll==null)return;
            if(Keyboard.current.endKey.wasPressedThisFrame)scroll.verticalNormalizedPosition=0;
            else if(Keyboard.current.homeKey.wasPressedThisFrame)scroll.verticalNormalizedPosition=1;
            else if(Keyboard.current.pageDownKey.wasPressedThisFrame||Keyboard.current.pageUpKey.wasPressedThisFrame)
            {
                float hidden=Mathf.Max(1,scroll.content.rect.height-scroll.viewport.rect.height);
                float page=scroll.viewport.rect.height/hidden;
                scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+(Keyboard.current.pageUpKey.wasPressedThisFrame?page:-page));
            }
        }
        bool TryPreviewEndingPrivate(string label)
        {
            var preview = hub.PreviewEndingPrivate(label); if (preview == null) return false;
            CloseEndingPrivatePreview(); endingPrivatePreview = preview;
            var panel = endingPrivatePreviewPanel = Rect("EndingPrivatePreview", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Fill(panel, C.Page); panel.SetAsLastSibling();
            var heading = Text(Rect("Title",panel,new Vector2(0,1),Vector2.one,new Vector2(20,-64),new Vector2(-20,-12)),
                GameText.T("发给老周之前","Before sending to Lao Zhou"),22,C.Ink,TextAlignmentOptions.TopLeft); heading.richText=false;
            var notice = Text(Rect("Notice",panel,new Vector2(0,1),Vector2.one,new Vector2(20,-132),new Vector2(-20,-70)),
                GameText.T("发送后，老周会知道下面这些内容。取消不会发送。","Sending will tell Lao Zhou the following. Cancel sends nothing."),15,C.Ink,TextAlignmentOptions.TopLeft);
            notice.richText=false; notice.overflowMode=TextOverflowModes.Overflow;
            var area=Rect("PreviewScroll",panel,Vector2.zero,Vector2.one,new Vector2(20,78),new Vector2(-20,-144)); Fill(area,Color.white);
            var previewScroll=area.gameObject.AddComponent<ScrollRect>();previewScroll.horizontal=false;previewScroll.movementType=ScrollRect.MovementType.Clamped;previewScroll.scrollSensitivity=32;
            var view=Rect("Viewport",area,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);view.gameObject.AddComponent<RectMask2D>();Fill(view,Color.white);
            var body=Rect("ExactMessage",view,new Vector2(0,1),Vector2.one,new Vector2(12,0),new Vector2(-12,0));body.pivot=new Vector2(.5f,1);
            var text=Text(body,preview.Text,18,C.Ink,TextAlignmentOptions.TopLeft);text.richText=false;text.overflowMode=TextOverflowModes.Overflow;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            previewScroll.viewport=view;previewScroll.content=body;
            var send=FlatButton(panel,GameText.T("发送以上原文","Send the text above"),()=>
            {
                if (endingPrivatePreview != preview || !IsOpen) return;
                bool sent=hub.SendEndingPrivate(preview);CloseEndingPrivatePreview();
                if(sent){dirty=true;stickBottom=true;}
            },15,Color.white,C.Mine);
            var cancel=FlatButton(panel,GameText.T("取消","Cancel"),CloseEndingPrivatePreview,15,C.Ink,Color.white);
            var sr=(RectTransform)send.transform;sr.anchorMin=new Vector2(.5f,0);sr.anchorMax=new Vector2(1,0);sr.offsetMin=new Vector2(6,18);sr.offsetMax=new Vector2(-20,62);
            var cr=(RectTransform)cancel.transform;cr.anchorMin=Vector2.zero;cr.anchorMax=new Vector2(.5f,0);cr.offsetMin=new Vector2(20,18);cr.offsetMax=new Vector2(-6,62);
            AddFocus(send.gameObject);AddFocus(cancel.gameObject);
            Canvas.ForceUpdateCanvases();previewScroll.verticalNormalizedPosition=1;
            send.interactable=body.rect.height<=view.rect.height+1;
            previewScroll.onValueChanged.AddListener(_=>{if(send!=null&&previewScroll.verticalNormalizedPosition<=.01f)send.interactable=true;});
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(cancel.gameObject);
            return true;
        }
    }
}
