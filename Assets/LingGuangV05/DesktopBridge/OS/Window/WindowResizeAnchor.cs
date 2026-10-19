using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The corner handle that resizes a window (2026-10-19). Replaces DreamOS's WindowResizeAnchor: same GUID,
    /// serialized fields and public API. <see cref="WindowManager"/> instantiates it from the resize preset,
    /// points <see cref="targetRect"/> at the window container and picks the corner and minimum size.
    ///
    /// The size limits are stored as deltas from the container's authored size, not absolute sizes; the minimum
    /// assumes the canvas scaler's 1920 x 1080 reference resolution.
    /// </summary>
    public class WindowResizeAnchor : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
    {
        [Header("Resources")]
        public RectTransform targetRect;

        [Header("Settings")]
        public Vector2 minSize;
        public Vector2 maxSize;

        Vector2 currentPointerPosition;
        Vector2 previousPointerPosition;

        Vector2 sizeDelta;
        Vector2 resizeValue;

        bool invertDeltaX = false;

        /// <param name="value">Minimum window size as a percent of the 1920 x 1080 reference canvas.</param>
        public void SetMinSize(int value)
        {
            minSize.x = -(1920 - 1920 * value / 100);
            minSize.y = -(1080 - 1080 * value / 100);
        }

        public void SetAnchor(WindowManager.ResizeAnchor anchor)
        {
            RectTransform anchorRect = GetComponent<RectTransform>();

            if (anchor == WindowManager.ResizeAnchor.BottomLeft)
            {
                anchorRect.anchorMin = new Vector2(0, 0);
                anchorRect.anchorMax = new Vector2(0, 0);

                WindowManager.SetPivot(anchorRect, new Vector2(0, 0));
                WindowManager.SetPivot(targetRect, new Vector2(1, 1));

                invertDeltaX = true;
            }
            else if (anchor == WindowManager.ResizeAnchor.BottomRight)
            {
                anchorRect.anchorMin = new Vector2(1, 0);
                anchorRect.anchorMax = new Vector2(1, 0);

                WindowManager.SetPivot(anchorRect, new Vector2(1, 0));
                WindowManager.SetPivot(targetRect, new Vector2(0, 1));

                invertDeltaX = false;
            }

            anchorRect.anchoredPosition = new Vector2(0, 0);
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (targetRect == null) { return; }

            targetRect.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, data.position, data.pressEventCamera, out previousPointerPosition);
        }

        public void OnDrag(PointerEventData data)
        {
            if (targetRect == null) { return; }

            sizeDelta = targetRect.sizeDelta;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, data.position, data.pressEventCamera, out currentPointerPosition);
            resizeValue = currentPointerPosition - previousPointerPosition;

            // Dragging down grows the window (y is inverted); a left-hand handle also inverts x.
            float deltaX = invertDeltaX ? -resizeValue.x : resizeValue.x;
            sizeDelta += new Vector2(deltaX, -resizeValue.y);

            sizeDelta = new Vector2(Mathf.Clamp(sizeDelta.x, minSize.x, maxSize.x), Mathf.Clamp(sizeDelta.y, minSize.y, maxSize.y));
            targetRect.sizeDelta = sizeDelta;
            previousPointerPosition = currentPointerPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            WindowDragger dragger = transform.parent.GetComponentInChildren<WindowDragger>();
            if (dragger != null) { dragger.ClampToArea(); }
        }
    }
}
