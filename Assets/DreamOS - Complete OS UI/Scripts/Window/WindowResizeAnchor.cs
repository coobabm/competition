using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.DreamOS
{
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

        public void SetMinSize(int value)
        {
            // Should match with the canvas scaler values - default 1920 * 1080
            int tempXValue = 1920 * value / 100;
            minSize.x = -(1920 - tempXValue);

            int tempYValue = 1080 * value / 100;
            minSize.y = -(1080 - tempYValue);
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
            if (targetRect == null)
                return;

            targetRect.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, data.position, data.pressEventCamera, out previousPointerPosition);
        }

        public void OnDrag(PointerEventData data)
        {
            if (targetRect == null)
                return;

            sizeDelta = targetRect.sizeDelta;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, data.position, data.pressEventCamera, out currentPointerPosition);
            resizeValue = currentPointerPosition - previousPointerPosition;

            if (!invertDeltaX) { sizeDelta += new Vector2(resizeValue.x, -resizeValue.y); }
            else { sizeDelta += new Vector2(-resizeValue.x, -resizeValue.y); }

            sizeDelta = new Vector2(Mathf.Clamp(sizeDelta.x, minSize.x, maxSize.x), Mathf.Clamp(sizeDelta.y, minSize.y, maxSize.y));
            targetRect.sizeDelta = sizeDelta;
            previousPointerPosition = currentPointerPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Transform parentObj = transform.parent;
            WindowDragger wd = parentObj.GetComponentInChildren<WindowDragger>();

            if (wd != null) { wd.ClampToArea(); }
        }
    }
}