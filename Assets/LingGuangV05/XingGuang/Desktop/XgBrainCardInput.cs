using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Native pointer events only. Cards stay on the shelf; the page owns the drag ghost and draft.</summary>
    public sealed class XgBrainCardInput : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, ICancelHandler
    {
        public Action Click, Cancel;
        public Action<PointerEventData> BeginDrag, Drag, EndDrag;
        public bool EscapeCancels;
        bool dragged, dragging;

        public void OnPointerDown(PointerEventData e)
        { if (e.button == PointerEventData.InputButton.Left) dragged = false; }

        public void OnPointerClick(PointerEventData e)
        { if (e.button == PointerEventData.InputButton.Left && !dragged) Click?.Invoke(); }

        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            dragged = dragging = true;
            BeginDrag?.Invoke(e);
        }

        public void OnDrag(PointerEventData e)
        { if (dragging) Drag?.Invoke(e); }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false;
            EndDrag?.Invoke(e);
        }

        public void OnCancel(BaseEventData e) { dragging = false; Cancel?.Invoke(); }
        void OnDisable() { if (dragging || EscapeCancels) { dragging = false; Cancel?.Invoke(); } }
        void Update()
        {
            if (EscapeCancels && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Cancel?.Invoke();
        }
    }
}
