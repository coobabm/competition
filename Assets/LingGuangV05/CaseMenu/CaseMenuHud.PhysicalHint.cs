using UnityEngine;
using TMPro;

namespace LingGuangV05.CaseMenu
{
    public sealed partial class CaseMenuHud
    {
        CaseMenuButton physicalHintTarget;
        Camera physicalHintCamera;
        RectTransform physicalHint;
        TMP_Text physicalHintLabel;
        public void SetPhysicalHint(CaseMenuButton target,Camera camera)
        {
            physicalHintTarget=target;physicalHintCamera=camera;
            if(physicalHint==null&&chromeGroup!=null)
            {
                physicalHint=Rect("PhysicalButtonHint",chromeGroup.transform);
                Place(physicalHint,new Vector2(.5f,.5f),Vector2.zero,new Vector2(250,60));
                physicalHint.pivot=new Vector2(0,.5f);
                var line=Panel("Leader",physicalHint,new Color32(207,207,200,255));
                Place(line.rectTransform,new Vector2(0,.5f),Vector2.zero,new Vector2(30,2));
                physicalHintLabel=Label("Caption",physicalHint,"",40,new Color32(245,244,239,255));
                var bold=LingGuangV05.Desktop.XingGuang.XgUiV4Fonts.SansSemibold;
                if(bold!=null)physicalHintLabel.font=bold;else physicalHintLabel.fontStyle=FontStyles.Bold;
                physicalHintLabel.overflowMode=TextOverflowModes.Overflow;
                Place(physicalHintLabel.rectTransform,new Vector2(0,.5f),new Vector2(44,0),new Vector2(200,60));
            }
            UpdatePhysicalHint();
        }
        void UpdatePhysicalHint()
        {
            if(physicalHint==null)return;
            bool show=physicalHintTarget!=null&&physicalHintCamera!=null&&!IsSettingsOpen&&!IsArchiveOpen&&titleActions!=null&&titleActions.interactable;
            physicalHint.gameObject.SetActive(show);if(!show)return;
            var center=physicalHintTarget.keycap!=null?physicalHintTarget.keycap.position:physicalHintTarget.transform.position;
            var point=physicalHintCamera.WorldToScreenPoint(center);
            if(point.z<=0){physicalHint.gameObject.SetActive(false);return;}
            var canvas=canvasRoot.GetComponent<Canvas>();
            var uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot,point,uiCamera,out var local);
            // Start the leader outside the projected switch, independent of camera framing.
            float radius=physicalHintTarget.isStart?.066f:.049f;
            var edge=physicalHintCamera.WorldToScreenPoint(center+physicalHintCamera.transform.right*radius);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot,edge,uiCamera,out var edgeLocal);
            physicalHint.anchoredPosition=local+new Vector2(Mathf.Abs(edgeLocal.x-local.x)+4,0);
            physicalHintLabel.text=physicalHintTarget.isStart?T("开机","Power on"):T("设置","Settings");
        }
    }
}
