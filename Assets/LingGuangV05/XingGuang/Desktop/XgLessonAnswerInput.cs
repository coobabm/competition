using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    // Button retains its visuals/navigation; this component owns answer dispatch.
    // No callback runs on pointer-down, and canceled presses cannot submit a lesson.
    public sealed class XgLessonAnswerInput : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, ISubmitHandler, ICancelHandler
    {
        Button button;
        Func<Action> capture;
        Action pending;
        int pointerId;

        public void Bind(Button target, Func<Action> snapshot) { button = target; capture = snapshot; }
        bool Ready => button != null && button.IsActive() && button.IsInteractable() && capture != null;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            pending = null;
            if (!Ready) return;
            pointerId = data.pointerId;
            pending = capture();
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || data.pointerId != pointerId) return;
            var submit = pending;
            pending = null; // Consume before dispatch, including reentrant callbacks.
            if (Ready) submit?.Invoke();
        }

        public void OnSubmit(BaseEventData data)
        {
            if (Ready) capture()?.Invoke();
        }

        void OnDisable() { pending = null; }
        public void OnCancel(BaseEventData data) { pending = null; }
        void OnApplicationFocus(bool focused) { if (!focused) pending = null; }
    }
}
