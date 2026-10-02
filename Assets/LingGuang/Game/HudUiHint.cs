using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuang.Game
{
    /// <summary>Hover/focus descriptions for compact HUD controls. Does not intercept button clicks.</summary>
    public sealed class HudUiHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public Hud Owner;
        public string Description;
        public void OnPointerEnter(PointerEventData e) { if (Owner != null) Owner.ActiveUiHint = this; }
        public void OnPointerExit(PointerEventData e) => Clear();
        public void OnSelect(BaseEventData e) { if (Owner != null) Owner.ActiveUiHint = this; }
        public void OnDeselect(BaseEventData e) => Clear();
        void OnDisable() => Clear();
        void Clear() { if (Owner != null && Owner.ActiveUiHint == this) Owner.ActiveUiHint = null; }
    }
}
