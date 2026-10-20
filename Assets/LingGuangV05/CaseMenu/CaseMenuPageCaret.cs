using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.CaseMenu
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CaseMenuPageCaret : MonoBehaviour
    {
        TMP_Text heading;Image caret;string measured;
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(heading==null)heading=GetComponent<TMP_Text>();
            string clean=heading.text.Replace(" ▏","");if(clean!=heading.text)heading.text=clean;
            if(caret==null){var go=new GameObject("PageCaret",typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);caret=go.GetComponent<Image>();caret.raycastTarget=false;}
            if(measured!=clean){measured=clean;var r=caret.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,.5f);r.pivot=new Vector2(0,.5f);r.anchoredPosition=new Vector2(Mathf.Min(heading.preferredWidth,heading.rectTransform.rect.width-12)+12,0);r.sizeDelta=new Vector2(3,heading.fontSize*.8f);}
            float a=GameSettings.ReducedMotion?1:.45f+.55f*(.5f+.5f*Mathf.Sin(Time.unscaledTime*3));
            caret.color=new Color(90/255f,214/255f,180/255f,a);
        }
    }
}
