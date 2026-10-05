using UnityEngine;
using UnityEngine.EventSystems;

namespace HongmengOS.Retro
{
    /// <summary>Mechanical front-panel control: drag, scroll, or click to step.</summary>
    [DisallowMultipleComponent]
    public sealed class RetroCrtKnob : MonoBehaviour, IPointerDownHandler,
        IBeginDragHandler, IDragHandler, IPointerUpHandler, IScrollHandler
    {
        public enum Control { Focus, Brightness }
        public RetroCrtController controller;
        public Control parameter;
        [Tooltip("The indicator needle only; not the fixed dial face or tick ring.")]
        public RectTransform dial;
        [Range(0f, 1f)] public float normalizedValue = 0.5f;
        [Min(30f)] public float dragRangePixels = 180f;
        [Range(0.01f, 0.25f)] public float scrollStep = 0.05f;
        [Range(0.01f, 0.25f)] public float clickStep = 0.1f;

        private Canvas canvas;
        private bool pressed;
        private bool dragged;
        private int pressedPointerId;
        private float lastTickValue;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable()
        {
            pressed = false;
            dragged = false;
            SynchronizeFromController();
        }

        private void OnValidate()
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);
            dragRangePixels = Mathf.Max(30f, dragRangePixels);
            scrollStep = Mathf.Clamp(scrollStep, 0.01f, 0.25f);
            clickStep = Mathf.Clamp(clickStep, 0.01f, 0.25f);
        }

        public void SynchronizeFromController()
        {
            if (controller != null)
                normalizedValue = parameter == Control.Focus ? controller.Focus : controller.Brightness;
            normalizedValue = Mathf.Clamp01(normalizedValue);
            lastTickValue = normalizedValue;
            RotateNeedle();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || controller == null) return;
            pressed = true;
            dragged = false;
            pressedPointerId = eventData.pointerId;
            SynchronizeFromController();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (pressed && eventData.pointerId == pressedPointerId) dragged = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!pressed || eventData.pointerId != pressedPointerId || controller == null) return;
            dragged = true;
            float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            float delta = (eventData.delta.x + eventData.delta.y) / (dragRangePixels * scale);
            ApplyValue(normalizedValue + delta, false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pressed || eventData.pointerId != pressedPointerId) return;
            pressed = false;
            var hitRect = transform as RectTransform;
            bool releasedInside = hitRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                hitRect, eventData.position, eventData.pressEventCamera);
            if (!dragged && releasedInside && controller != null)
            {
                float next = normalizedValue >= 0.9999f ? 0f : Mathf.Min(1f, normalizedValue + clickStep);
                ApplyValue(next, true);
            }
            dragged = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (controller == null) return;
            SynchronizeFromController();
            float delta = eventData.scrollDelta.y;
            if (Mathf.Abs(delta) < 0.001f) delta = eventData.scrollDelta.x;
            if (Mathf.Abs(delta) < 0.001f) return;
            ApplyValue(normalizedValue + delta * scrollStep, true);
        }

        private void ApplyValue(float value, bool discreteStep)
        {
            float previous = normalizedValue;
            normalizedValue = Mathf.Clamp01(value);
            if (parameter == Control.Focus) controller.SetFocus(normalizedValue);
            else controller.SetBrightness(normalizedValue);
            RotateNeedle();
            if (Mathf.Approximately(previous, normalizedValue)) return;
            if (discreteStep || Mathf.Abs(normalizedValue - lastTickValue) >= 0.04f)
            {
                lastTickValue = normalizedValue;
                controller.PlayKnobTick();
            }
        }

        private void RotateNeedle()
        {
            if (dial != null)
                dial.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(130f, -130f, normalizedValue));
        }

        private void OnDisable()
        {
            pressed = false;
            dragged = false;
        }
    }
}
