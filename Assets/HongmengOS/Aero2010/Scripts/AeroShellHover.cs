using UnityEngine;
using UnityEngine.EventSystems;

namespace HongmengOS.Aero2010
{
    [DisallowMultipleComponent]
    public sealed class AeroShellHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public AeroShellGraphic graphic;
        public GameObject tooltip;
        public void OnPointerEnter(PointerEventData data) { if (graphic != null) graphic.SetHovered(true); if (tooltip != null) tooltip.SetActive(true); }
        public void OnPointerExit(PointerEventData data) { ResetState(); }
        public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left && graphic != null) graphic.SetPressed(true); }
        public void OnPointerUp(PointerEventData data) { if (graphic != null) graphic.SetPressed(false); }
        private void OnDisable() { ResetState(); }
        private void ResetState() { if (graphic != null) { graphic.SetHovered(false); graphic.SetPressed(false); } if (tooltip != null) tooltip.SetActive(false); }
    }
}
