using UnityEngine;
using UnityEngine.EventSystems;

namespace Emergence
{
    /// <summary>Presentation-only feedback for the duplicated neural menu. Reach owns actions and audio.</summary>
    [DisallowMultipleComponent]
    public sealed class SparkNeuralButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private CanvasGroup clickFlash;
        [SerializeField] private float hoverLift = 7f;
        [SerializeField] private float hoverScale = 1.025f;
        [SerializeField] private float pressDepth = 5f;
        private Vector2 restPosition;
        private Vector3 restScale;
        private bool hovered;
        private bool selected;
        private bool pressed;
        private float pulse;
        private bool initialized;

        private void Awake()
        {
            if (visual == null) visual = transform as RectTransform;
            if (visual == null) { enabled = false; return; }
            restPosition = visual.anchoredPosition;
            restScale = visual.localScale;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            bool active = hovered || selected;
            float lift = pressed ? -pressDepth : active ? hoverLift : 0f;
            float scale = pressed ? 0.976f : active ? hoverScale : 1f;
            float blend = 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime);
            visual.anchoredPosition = Vector2.Lerp(visual.anchoredPosition,
                restPosition + Vector2.up * lift, blend);
            visual.localScale = Vector3.Lerp(visual.localScale,
                restScale * (scale + Mathf.Sin(pulse * Mathf.PI) * 0.015f), blend);
            pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 4.5f);
            if (clickFlash != null) clickFlash.alpha = pulse * 0.24f;
        }

        private void OnDisable()
        {
            hovered = selected = pressed = false;
            pulse = 0f;
            if (initialized && visual != null)
            {
                visual.anchoredPosition = restPosition;
                visual.localScale = restScale;
            }
            if (clickFlash != null) clickFlash.alpha = 0f;
        }

        public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) pressed = true;
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) pressed = false;
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) pulse = 1f;
        }
        public void OnSelect(BaseEventData eventData) { selected = true; }
        public void OnDeselect(BaseEventData eventData) { selected = false; pressed = false; }
        public void OnSubmit(BaseEventData eventData) { pulse = 1f; }
    }
}
