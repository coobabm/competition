using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// A normal uGUI Button with a small label press. Business effects own the button rect;
    /// the separate label effect cannot replace their Knock, sound or result presentation.
    /// </summary>
    public sealed class XgButtonPressFeedback : Button
    {
        public RectTransform Label { get; set; }

        XgJuice pressFx, hostFx;
        float elapsed, duration;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Left) PressLabel();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            // Run before onClick: callbacks may disable, hide or rebuild the button.
            PressLabel();
            base.OnSubmit(eventData);
        }

        void PressLabel()
        {
            if (!IsActive() || !IsInteractable() || Label == null) return;
            hostFx = transform.parent != null ? transform.parent.GetComponentInParent<XgJuice>() : null;
            if (hostFx != null && (!hostFx.isActiveAndEnabled || !hostFx.Visible())) return;

            // Knock needs no particle canvas or audio Init. Allocate only on first use.
            // A private stack allows cancellation without clearing the host's business FX.
            if (pressFx == null) pressFx = Label.gameObject.AddComponent<XgJuice>();
            pressFx.TrackKnockLayoutChanges = true;
            pressFx.enabled = true;
            bool reduced = hostFx != null && hostFx.Reduced;
            duration = reduced ? .12f : .18f;
            elapsed = 0;
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
            if (pressFx == null) return;
            pressFx.Clear();
            pressFx.enabled = false;
        }
    }
}
