using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.CaseMenu
{
    public sealed class CaseMenuHoverMark : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public GameObject mark;
        bool hovered, selected;
        void OnEnable() { Refresh(); }
        void OnDisable() { hovered=selected=false; Refresh(); }
        void Refresh() { if(mark!=null)mark.SetActive(hovered||selected); }
        public void OnPointerEnter(PointerEventData e){hovered=true;Refresh();}
        public void OnPointerExit(PointerEventData e){hovered=false;Refresh();}
        public void OnSelect(BaseEventData e){selected=true;Refresh();}
        public void OnDeselect(BaseEventData e){selected=false;Refresh();}
    }
}
