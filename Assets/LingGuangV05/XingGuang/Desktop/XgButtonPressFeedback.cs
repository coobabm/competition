using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// A normal uGUI Button with an independent visual-shell press. Business effects own the button rect;
    /// the separate visual effect cannot replace their Knock, sound or result presentation.
    /// </summary>
    public sealed class XgButtonPressFeedback : Button, ICancelHandler
    {
        public RectTransform Label { get; set; }
        public bool V4Press { get; set; }
        public RectTransform PressTarget { get; set; }
        RectTransform Target => V4Press && PressTarget != null ? PressTarget : Label;

        XgJuice pressFx, hostFx;
        bool reducedPress;
        int? feedbackPointerId, heldPointerId;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (IsActive() && IsInteractable())
            {
                var hold = GetComponent<XgHold>();
                if (hold == null || !hold.Held || heldPointerId == null) heldPointerId = eventData.pointerId;
            }
            PressLabel(eventData.pointerId);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (feedbackPointerId == eventData.pointerId) ResetPress();
            // XgHold ends on exit and does not restart on re-entry. Keep its background
            // consistent, even after the short label pulse has already finished.
            // Do not dispatch another Up: XgHold itself owns its business callbacks.
            if (heldPointerId == eventData.pointerId && eventData.button == PointerEventData.InputButton.Left)
            {
                var hold = GetComponent<XgHold>();
                if (hold != null && hold.isActiveAndEnabled)
                {
                    base.OnPointerUp(eventData);
                    heldPointerId = null;
                }
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (heldPointerId == eventData.pointerId) heldPointerId = null;
            // Input modules dispatch Up before clearing eligibility on drag takeover.
            // Leave IDragHandler to the parent ScrollRect and keep ordinary click rules.
            if (feedbackPointerId == eventData.pointerId && eventData.dragging &&
                eventData.pointerPress != eventData.pointerDrag) ResetPress();
            else if (feedbackPointerId == eventData.pointerId) ReleaseLabel();
        }

        public void OnCancel(BaseEventData eventData)
        {
            var pointer = eventData as PointerEventData;
            if (pointer == null || feedbackPointerId == pointer.pointerId) ResetPress();
            // Cancel affects presentation only; do not consume or fabricate a click.
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            // Run before onClick: callbacks may disable, hide or rebuild the button.
            PressLabel();
            ReleaseLabel();
            base.OnSubmit(eventData);
        }

        void PressLabel(int? pointerId = null)
        {
            if (!IsActive() || !IsInteractable() || Target == null) return;
            hostFx = transform.parent != null ? transform.parent.GetComponentInParent<XgJuice>() : null;
            if (hostFx != null && (!hostFx.isActiveAndEnabled || !hostFx.Visible())) return;

            if (V4Press && hostFx != null && hostFx.Reduced) { ResetPress(); return; }

            // Knock needs no particle canvas or audio Init. Allocate only on first use.
            // A private stack allows cancellation without clearing the host's business FX.
            if (pressFx == null) pressFx = Target.gameObject.AddComponent<XgJuice>();
            pressFx.TrackKnockLayoutChanges = true;
            pressFx.enabled = true;
            feedbackPointerId = pointerId;
            reducedPress = hostFx != null && hostFx.Reduced;
            if (V4Press) pressFx.HoldPress(Target);
            else pressFx.Knock(Target, reducedPress ? .015f : .045f,
                reducedPress ? Vector2.zero : Vector2.down, reducedPress ? .12f : .18f);
        }

        void ReleaseLabel()
        {
            if (!V4Press) return;
            feedbackPointerId = null;
            if (V4Press && pressFx != null && Target != null) pressFx.ReleasePress(Target);
        }

        void LateUpdate()
        {
            if (pressFx == null || !pressFx.enabled) return;
            if (!IsInteractable() || Target == null || !pressFx.HasActiveKnock(Target) ||
                (hostFx != null && (!hostFx.isActiveAndEnabled || !hostFx.Visible())))
            {
                ResetPress();
                return;
            }
            if (hostFx != null && hostFx.Reduced)
            {
                if (V4Press) ResetPress();
                else if (!reducedPress) { reducedPress = true; pressFx.SettleKnock(Target); }
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (state == SelectionState.Disabled) ResetPress();
        }

        protected override void OnDisable()
        {
            heldPointerId = null;
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
