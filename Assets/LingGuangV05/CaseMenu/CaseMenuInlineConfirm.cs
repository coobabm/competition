using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.CaseMenu
{
    public sealed class CaseMenuInlineConfirm : MonoBehaviour,IPointerExitHandler,IDeselectHandler
    {
        TMP_Text label;string normal,question;bool armed;Action action;
        public void Bind(Button button,string caption,string confirmation,Action execute)
        {label=button.GetComponentInChildren<TMP_Text>();normal=caption;question=confirmation;action=execute;button.onClick.AddListener(Click);}
        void Click(){if(armed){Reset();action?.Invoke();}else{armed=true;label.text=question;label.color=new Color32(224,122,99,255);}}
        void Reset(){armed=false;if(label!=null){label.text=normal;label.color=new Color(.94f,.91f,.85f);}}
        public void OnPointerExit(PointerEventData e){Reset();}
        public void OnDeselect(BaseEventData e){Reset();}
        void OnDisable(){Reset();}
    }
}
