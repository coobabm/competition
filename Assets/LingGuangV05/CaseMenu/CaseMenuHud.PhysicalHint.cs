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
                var line=Panel("Leader",physicalHint,Teal);
                Place(line.rectTransform,new Vector2(0,.5f),Vector2.zero,new Vector2(30,2));
                physicalHintLabel=Label("Caption",physicalHint,"",40,Ink);
                Place(physicalHintLabel.rectTransform,new Vector2(0,.5f),new Vector2(44,0),new Vector2(200,60));
            }
            UpdatePhysicalHint();
        }
        void UpdatePhysicalHint()
        {
            if(physicalHint==null)return;
            bool show=physicalHintTarget!=null&&physicalHintCamera!=null&&!IsSettingsOpen&&!IsArchiveOpen&&titleActions!=null&&titleActions.interactable;
            physicalHint.gameObject.SetActive(show);if(!show)return;
            var point=physicalHintCamera.WorldToScreenPoint(physicalHintTarget.transform.position);
            if(point.z<=0){physicalHint.gameObject.SetActive(false);return;}
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot,point,null,out var local);
            physicalHint.anchoredPosition=local+new Vector2(44,0);
            physicalHintLabel.text=physicalHintTarget.isStart?T("开机","Power on"):T("设置","Settings");
            physicalHintLabel.color=physicalHintTarget.isStart?new Color32(83,184,255,255):new Color32(230,169,86,255);
        }
    }
}
