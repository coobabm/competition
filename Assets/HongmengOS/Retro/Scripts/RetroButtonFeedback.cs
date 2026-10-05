using UnityEngine;
using UnityEngine.EventSystems;
namespace HongmengOS.Retro
{
    public sealed class RetroButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RetroBevelGraphic bevel;
        public void OnPointerDown(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left && bevel!=null) bevel.Inset=true; }
        public void OnPointerUp(PointerEventData e) { if(bevel!=null) bevel.Inset=false; }
        public void OnPointerExit(PointerEventData e) { if(bevel!=null) bevel.Inset=false; }
        private void OnDisable() { if(bevel!=null) bevel.Inset=false; }
    }
}
