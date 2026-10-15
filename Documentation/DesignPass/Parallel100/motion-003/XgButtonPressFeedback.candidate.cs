using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// A normal uGUI Button with a small label press. Business effects own the button rect;
    /// the separate label effect cannot replace their Knock, sound or result presentation.
    /// </summary>
    public sealed class XgButtonPressFeedback : Button, ICancelHandler
    {
        public RectTransform Label { get; set; }

        XgJuice pressFx, hostFx;
        float elapsed, duration;
        int? feedbackPointerId;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Left) PressLabel(eventData.pointerId);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (feedbackPointerId == eventData.pointerId) ResetPress();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            // Input modules send Up before clearing eligibleForClick on drag takeover.
            // Do not implement IDragHandler: the parent ScrollRect must retain the drag.
            if (eventData.button == PointerEventData.InputButton.Left &&
                feedbackPointerId == eventData.pointerId && eventData.dragging &&
                eventData.pointerPress != eventData.pointerDrag)
                ResetPress();
            // Ordinary releases keep their rebound and the inherited click dispatch.
        }

        public void OnCancel(BaseEventData eventData)
        {
            // Navigation Cancel clears feedback only; it does not consume the event
            // or change the input module's independent pointer click eligibility.
            var pointer = eventData as PointerEventData;
            if (pointer == null || feedbackPointerId == pointer.pointerId) ResetPress();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            // Run before onClick: callbacks may disable, hide or rebuild the button.
            PressLabel();
            base.OnSubmit(eventData);
        }

        void PressLabel(int? pointerId = null)
        {
            if (!IsActive() || !IsInteractable() || Label == null) return;
            hostFx = transform.parent != null ? transform.parent.GetComponentInParent<XgJuice>() : null;
            if (hostFx != null && (!hostFx.isActiveAndEnabled || !hostFx.Visible())) return;

            // Knock needs no particle canvas or audio Init. Allocate only on first use.
            // A private stack allows cancellation without clearing the host's business FX.
            if (pressFx == null) pressFx = Label.gameObject.AddComponent<XgJuice>();
            pressFx.enabled = true;
            bool reduced = hostFx != null && hostFx.Reduced;
            duration = reduced ? .12f : .18f;
            elapsed = 0;
            // Only a successfully started press becomes the owner. Submit has no pointer.
            feedbackPointerId = pointerId;
            pressFx.Knock(Label, reduced ? .015f : .045f,
                reduced ? Vector2.zero : Vector2.down, duration);
            // Repeated presses reuse Knock's saved resting transform, never the deformed one.
        }

        void LateUpdate()
        {
            if (pressFx == null || !pressFx.enabled) return;
            elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (!IsInteractable() || Label == null || elapsed >= duration ||
                (hostFx != null && (!hostFx.isActiveAndEnabled || !hostFx.Visible())))
                ResetPress();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (state == SelectionState.Disabled) ResetPress();
        }

        protected override void OnDisable()
        {
            ResetPress();
            base.OnDisable();
        }

        void ResetPress()
        {
            feedbackPointerId = null;
            if (pressFx == null) return;
            pressFx.Clear();
            pressFx.enabled = false;
        }
    }
}
