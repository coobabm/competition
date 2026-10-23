using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The drag handle of a window (its title bar): dragging moves <see cref="dragObject"/> inside
    /// <see cref="dragArea"/>, a double click toggles fullscreen and any click focuses the window
    /// (2026-10-19). Replaces DreamOS's WindowDragger: same GUID, serialized fields and public API.
    ///
    /// Without a drag area the first canvas found is used (with a fallback of the topmost RectTransform parent),
    /// and a transparent Image is added when the handle has none so it can receive pointer events.
    /// </summary>
    public class WindowDragger : UIBehaviour, IBeginDragHandler, IDragHandler, IPointerClickHandler
    {
        [Header("Resources")]
        public RectTransform dragArea;
        public RectTransform dragObject;

        private Vector2 originalLocalPointerPosition;
        private Vector3 originalPanelLocalPosition;
        private bool dragging;

        [HideInInspector] public WindowManager wManager;

        public new void Start()
        {
            if (dragArea == null)
            {
#if UNITY_2023_2_OR_NEWER
                var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
#else
                var canvas = (Canvas)FindObjectsOfType(typeof(Canvas))[0];
#endif
                dragArea = canvas.GetComponent<RectTransform>();
            }

            // Pointer events need a raycast target
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }
        }

        private RectTransform dragObjectInternal
        {
            get { return dragObject == null ? (transform as RectTransform) : dragObject; }
        }

        private RectTransform dragAreaInternal
        {
            get
            {
                if (dragArea != null) { return dragArea; }

                RectTransform root = transform as RectTransform;
                while (root.parent != null && root.parent is RectTransform) { root = root.parent as RectTransform; }
                return root;
            }
        }

        private bool IsFullscreen { get { return wManager != null && wManager.isFullscreen; } }

        public void OnBeginDrag(PointerEventData data)
        {
            // Only the left button moves a window; dragging a background window brings it to the front first.
            dragging = data.button == PointerEventData.InputButton.Left && !IsFullscreen;
            if (!dragging) { return; }
            if (wManager != null) { wManager.FocusToWindow(); }

            originalPanelLocalPosition = dragObjectInternal.localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragAreaInternal, data.position, data.pressEventCamera, out originalLocalPointerPosition);
        }

        public void OnDrag(PointerEventData data)
        {
            if (!dragging || data.button != PointerEventData.InputButton.Left || IsFullscreen) { return; }

            Vector2 localPointerPosition;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragAreaInternal, data.position, data.pressEventCamera, out localPointerPosition))
            {
                Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
                dragObjectInternal.localPosition = originalPanelLocalPosition + offsetToOriginal;
            }

            ClampToArea();
        }

        /// <summary>Keeps the dragged window fully inside the drag area.</summary>
        public void ClampToArea()
        {
            RectTransform target = dragObjectInternal;
            RectTransform area = dragAreaInternal;

            Vector3 pos = target.localPosition;
            Vector3 minPosition = area.rect.min - target.rect.min;
            Vector3 maxPosition = area.rect.max - target.rect.max;

            pos.x = Mathf.Clamp(pos.x, minPosition.x, maxPosition.x);
            pos.y = Mathf.Clamp(pos.y, minPosition.y, maxPosition.y);

            target.localPosition = pos;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (wManager == null) { return; }
            if (wManager.allowGestures && eventData.clickCount == 2 && eventData.button == PointerEventData.InputButton.Left) { wManager.FullscreenWindow(); }
            wManager.FocusToWindow();
        }
    }
}
