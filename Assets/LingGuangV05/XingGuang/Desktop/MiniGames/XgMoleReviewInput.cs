using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    /// <summary>Captures the displayed judgment at press time. The sibling Button owns presentation only.</summary>
    public sealed class XgMoleReviewInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, IPointerExitHandler, ISubmitHandler, ICancelHandler
    {
        public Func<Action> Capture;
        public Func<bool> Available;
        Action pending;
        int pointerId;
        bool released;
        bool Ready
        {
            get
            {
                var button = GetComponent<Button>();
                return isActiveAndEnabled && button != null && button.IsActive() && button.IsInteractable()
                    && Available != null && Available();
            }
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            Clear();
            if (!Ready) return;
            pointerId = e.pointerId;
            pending = Capture?.Invoke();
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || e.pointerId != pointerId) return;
            if (!Ready || e.dragging || !e.eligibleForClick) { Clear(); return; }
            released = true;
        }
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || e.pointerId != pointerId) return;
            var action = pending;
            bool accept = released && Ready && e.eligibleForClick && !e.dragging;
            Clear(); // Consume before training, whose callbacks can reenter the UI.
            if (accept) action?.Invoke();
        }
        public void OnSubmit(BaseEventData e)
        {
            Clear();
            if (Ready) Capture?.Invoke()?.Invoke();
        }
        public void OnPointerExit(PointerEventData e) { if (e.pointerId == pointerId) Clear(); }
        public void OnCancel(BaseEventData e) { Clear(); }
        void Update() { if (pending != null && !Ready) Clear(); }
        void OnDisable() { Clear(); }
        void Clear() { pending = null; released = false; }
    }
}
