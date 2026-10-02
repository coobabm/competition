using UnityEngine;
using UnityEngine.InputSystem;

namespace Emergence
{
    /// <summary>Small, bounded mouse parallax on the background only; all menu UI remains stationary.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SparkNeuralBackgroundMotion : MonoBehaviour
    {
        [SerializeField] private Vector2 maximumOffset = new Vector2(15f, 10f);
        [SerializeField] private Vector2 maximumTilt = new Vector2(1.1f, 1.5f);
        [SerializeField] private float maximumRoll = 0.25f;
        [SerializeField, Range(1f, 1.15f)] private float overscan = 1.055f;
        [SerializeField, Min(0.1f)] private float response = 4.5f;

        private RectTransform target;
        private RectTransform canvasRect;
        private Canvas canvas;
        private Vector2 restPosition;
        private Vector3 restScale;
        private Quaternion restRotation;
        private Vector2 current;
        private bool initialized;

        private void Awake()
        {
            target = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) { enabled = false; return; }
            canvas = canvas.rootCanvas;
            canvasRect = canvas.transform as RectTransform;
            restPosition = target.anchoredPosition;
            restRotation = target.localRotation;
            restScale = target.localScale;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized || canvasRect == null) return;
            Vector2 desired = Vector2.zero;
            if (Application.isFocused && Mouse.current != null)
            {
                Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null : canvas.worldCamera;
                Vector2 local;
                Rect bounds = canvasRect.rect;
                if (bounds.width > 0f && bounds.height > 0f &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, Mouse.current.position.ReadValue(), uiCamera, out local) &&
                    bounds.Contains(local))
                {
                    desired = new Vector2(
                        Mathf.Clamp((local.x - bounds.center.x) / (bounds.width * 0.5f), -1f, 1f),
                        Mathf.Clamp((local.y - bounds.center.y) / (bounds.height * 0.5f), -1f, 1f));
                }
            }

            ApplyMotion(desired, Time.unscaledDeltaTime);
        }

        // Kept separate from input sampling so corner coverage and return-to-centre can be tested.
        private void ApplyMotion(Vector2 desired, float deltaTime)
        {
            desired.x = Mathf.Clamp(desired.x, -1f, 1f);
            desired.y = Mathf.Clamp(desired.y, -1f, 1f);
            current = Vector2.Lerp(current, desired, 1f - Mathf.Exp(-response * deltaTime));
            target.anchoredPosition = restPosition - Vector2.Scale(current, maximumOffset);
            target.localRotation = restRotation * Quaternion.Euler(
                -current.y * maximumTilt.x, current.x * maximumTilt.y, -current.x * maximumRoll);
            target.localScale = restScale * overscan;
        }

        private void OnDisable()
        {
            current = Vector2.zero;
            if (!initialized || target == null) return;
            target.anchoredPosition = restPosition;
            target.localRotation = restRotation;
            target.localScale = restScale;
        }
    }
}
