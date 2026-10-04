using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HongmengOS.Aero2010
{
    /// <summary>
    /// Relays one desktop pointer from the visible LCD to its offscreen uGUI canvas.
    /// The source canvases stay enabled for rendering; their GraphicRaycasters must
    /// stay disabled so the normal input module cannot also send unwarped events.
    /// The display uses a full (0,0,1,1) RawImage UV rect. No hardware pointer state
    /// is changed. Lists and event data are reused after initialization.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("HongmengOS/Aero LCD Input Surface")]
    public sealed class AeroLcdInputSurface : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler,
        IPointerDownHandler, IPointerUpHandler, IScrollHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform displayRect;
        public Camera sourceCamera;
        public GraphicRaycaster[] sourceRaycasters;
        public bool inputEnabled = true;
        // Flat LCD: no barrel distortion or rounded-corner rejection.
        public bool flipY;
        [Min(0.05f)] public float doubleClickSeconds = 0.30f;

        private sealed class ButtonState
        {
            public PointerEventData data;
            public bool pressed;
            public GameObject clickTarget;
            public GameObject lastClickTarget;
            public float lastClickTime = -100;
            public int clickCount;
        }

        private readonly ButtonState[] buttons = { new ButtonState(), new ButtonState(), new ButtonState() };
        private readonly List<RaycastResult> results = new List<RaycastResult>(128);
        private EventSystem eventSystem;
        private PointerEventData hoverData;
        private Canvas displayCanvas;
        private Vector2 physicalPosition;
        private int physicalPointerId;
        private bool havePointer;
        private bool pointerInside;
        private bool cancelling;
        private bool wasInputEnabled;

        private void Awake()
        {
            if (displayRect == null) displayRect = transform as RectTransform;
            if (displayRect != null) displayCanvas = displayRect.GetComponentInParent<Canvas>();
            wasInputEnabled = inputEnabled;
            EnsureEventData();
        }

        private void OnDisable() { CancelInteraction(); havePointer = false; pointerInside = false; }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelInteraction(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelInteraction(); }

        private void Update()
        {
            if (!inputEnabled)
            {
                if (wasInputEnabled) CancelInteraction();
                wasInputEnabled = false;
                return;
            }
            wasInputEnabled = true;
        }

        private void LateUpdate()
        {
            if (!inputEnabled) return;
            // Refresh even when stationary: opening a window can change the object
            // beneath the cursor without producing a hardware pointer-move event.
            if (havePointer && (pointerInside || AnyPressed())) RefreshPointer();
        }

        public void SetInputEnabled(bool value)
        {
            inputEnabled = value;
            wasInputEnabled = value;
            if (!value) CancelInteraction();
        }

        public bool TryMap(Vector2 screenPosition, out Vector2 sourcePixels)
        {
            return TryMap(screenPosition, DisplayEventCamera(), out sourcePixels);
        }

        /// <summary>
        /// Returns false in the area outside the flat display.
        /// When geometry is valid, sourcePixels is still populated outside the
        /// display so a captured drag can finish cleanly beyond the screen edge.
        /// </summary>
        public bool TryMap(Vector2 screenPosition, Camera displayEventCamera, out Vector2 sourcePixels)
        {
            sourcePixels = Vector2.zero;
            if (displayRect == null || sourceCamera == null) return false;
            Rect rect = displayRect.rect;
            if (rect.width <= 0 || rect.height <= 0) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(displayRect, screenPosition, displayEventCamera, out Vector2 local)) return false;
            Vector2 uv = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
            Vector2 sourceUv = new Vector2(uv.x, flipY ? 1 - uv.y : uv.y);
            var texture = sourceCamera.targetTexture;
            int width = texture != null ? texture.width : sourceCamera.pixelWidth;
            int height = texture != null ? texture.height : sourceCamera.pixelHeight;
            if (width <= 0 || height <= 0) return false;
            sourcePixels = new Vector2(sourceUv.x * width, sourceUv.y * height);
            return uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1
                && sourceUv.x >= 0 && sourceUv.x <= 1 && sourceUv.y >= 0 && sourceUv.y <= 1;
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (!Capture(data)) return;
            pointerInside = true;
            RefreshPointer();
        }

        public void OnPointerMove(PointerEventData data)
        {
            if (Capture(data)) RefreshPointer();
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (!Capture(data)) return;
            pointerInside = false;
            if (hoverData != null) SetHover(null);
            RefreshPressed();
        }

        public void OnPointerDown(PointerEventData physical)
        {
            if (!Capture(physical) || !Ready()) return;
            int index = (int)physical.button;
            if (index < 0 || index >= buttons.Length) return;
            var state = buttons[index];
            if (state.pressed) return;
            var data = state.data;
            if (!MapAndRaycast(data, true)) return;
            GameObject target = data.pointerCurrentRaycast.gameObject;
            if (target == null) return;

            data.button = physical.button;
            data.pointerId = physical.pointerId;
            data.delta = Vector2.zero;
            data.pressPosition = data.position;
            data.pointerPressRaycast = data.pointerCurrentRaycast;
            data.eligibleForClick = true;
            data.useDragThreshold = true;
            data.dragging = false;
            data.scrollDelta = Vector2.zero;
            data.pointerEnter = target;
            state.pressed = true;
            pointerInside = true;

            var selected = ExecuteEvents.GetEventHandler<ISelectHandler>(target);
            if (eventSystem.currentSelectedGameObject != selected) eventSystem.SetSelectedGameObject(null, data);
            GameObject pressTarget = ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerDownHandler);
            // A handler can power off the screen during pointer-down.
            if (!state.pressed || !inputEnabled || !isActiveAndEnabled) return;
            state.clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
            if (pressTarget == null) pressTarget = state.clickTarget;
            float now = Time.unscaledTime;
            state.clickCount = state.lastClickTarget == state.clickTarget && now - state.lastClickTime <= doubleClickSeconds
                ? state.clickCount + 1 : 1;
            data.clickCount = state.clickCount;
            data.clickTime = now;
            data.pointerPress = pressTarget;
            data.rawPointerPress = target;
            data.pointerClick = state.clickTarget;
            data.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(target);
            if (data.pointerDrag != null) ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.initializePotentialDrag);
            RefreshHover();
        }

        public void OnPointerUp(PointerEventData physical)
        {
            if (!Capture(physical) || !EnsureEventData()) return;
            int index = (int)physical.button;
            if (index < 0 || index >= buttons.Length) return;
            var state = buttons[index];
            if (!state.pressed) return;
            var data = state.data;
            bool validHit = inputEnabled && MapAndRaycast(data, true);
            GameObject target = validHit ? data.pointerCurrentRaycast.gameObject : null;
            GameObject clickTarget = target != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) : null;
            bool click = inputEnabled && data.eligibleForClick && !data.dragging && state.clickTarget != null && state.clickTarget == clickTarget;
            state.pressed = false;
            if (data.pointerPress != null) ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);
            if (click && inputEnabled && isActiveAndEnabled)
            {
                state.lastClickTarget = state.clickTarget;
                state.lastClickTime = Time.unscaledTime;
                ExecuteEvents.Execute(state.clickTarget, data, ExecuteEvents.pointerClickHandler);
            }
            else if (data.dragging && target != null && inputEnabled)
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.dropHandler);
            if (data.pointerDrag != null && data.dragging) ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.endDragHandler);
            ClearPress(state);
            RefreshHover();
        }

        public void OnScroll(PointerEventData physical)
        {
            if (!Capture(physical) || !Ready() || !MapAndRaycast(hoverData, true)) return;
            hoverData.scrollDelta = physical.scrollDelta;
            ExecuteEvents.ExecuteHierarchy(hoverData.pointerCurrentRaycast.gameObject, hoverData, ExecuteEvents.scrollHandler);
            hoverData.scrollDelta = Vector2.zero;
        }

        public void OnInitializePotentialDrag(PointerEventData data) { Capture(data); }
        public void OnBeginDrag(PointerEventData data) { if (Capture(data)) RefreshPointer(); }
        public void OnDrag(PointerEventData data) { if (Capture(data)) RefreshPointer(); }
        // Release owns the synthetic end-drag. The physical input module invokes
        // end-drag after pointer-up, which must not produce a second synthetic event.
        public void OnEndDrag(PointerEventData data) { }

        public void CancelInteraction()
        {
            if (cancelling) return;
            cancelling = true;
            try
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    var state = buttons[i];
                    if (state.data == null) continue;
                    bool pressed = state.pressed;
                    state.pressed = false;
                    state.data.eligibleForClick = false;
                    if (pressed && state.data.pointerPress != null)
                        ExecuteEvents.Execute(state.data.pointerPress, state.data, ExecuteEvents.pointerUpHandler);
                    if (state.data.dragging && state.data.pointerDrag != null)
                        ExecuteEvents.Execute(state.data.pointerDrag, state.data, ExecuteEvents.endDragHandler);
                    ClearPress(state);
                    state.lastClickTarget = null;
                    state.lastClickTime = -100;
                }
                if (hoverData != null) SetHover(null);
                if (eventSystem != null && Owns(eventSystem.currentSelectedGameObject)) eventSystem.SetSelectedGameObject(null);
            }
            finally { cancelling = false; }
        }

        private bool Capture(PointerEventData data)
        {
            if (data == null) return false;
            // One desktop mouse pointer, with independent left/right/middle state.
            // Do not let a second touch steal a drag that is already in progress.
            if (havePointer && physicalPointerId != data.pointerId && AnyPressed()) return false;
            physicalPointerId = data.pointerId;
            physicalPosition = data.position;
            havePointer = true;
            return true;
        }

        private bool EnsureEventData()
        {
            var current = EventSystem.current;
            if (current == null) return false;
            if (eventSystem == current && hoverData != null) return true;
            CancelInteraction();
            eventSystem = current;
            hoverData = new PointerEventData(current);
            for (int i = 0; i < buttons.Length; i++) buttons[i].data = new PointerEventData(current) { button = (PointerEventData.InputButton)i };
            return true;
        }

        private bool Ready() => inputEnabled && isActiveAndEnabled && sourceCamera != null && EnsureEventData();
        private bool AnyPressed()
        {
            for (int i = 0; i < buttons.Length; i++) if (buttons[i].pressed) return true;
            return false;
        }

        private void RefreshPointer()
        {
            if (!Ready()) return;
            RefreshHover();
            RefreshPressed();
        }

        private void RefreshHover()
        {
            if (!Ready()) return;
            bool valid = MapAndRaycast(hoverData, pointerInside);
            SetHover(valid ? hoverData.pointerCurrentRaycast.gameObject : null);
            if (hoverData.delta.sqrMagnitude > 0)
                for (int i = 0; i < hoverData.hovered.Count; i++)
                    if (hoverData.hovered[i] != null) ExecuteEvents.Execute(hoverData.hovered[i], hoverData, ExecuteEvents.pointerMoveHandler);
        }

        private void RefreshPressed()
        {
            if (!Ready()) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                var state = buttons[i];
                if (!state.pressed) continue;
                var data = state.data;
                MapAndRaycast(data, true);
                data.pointerEnter = hoverData.pointerEnter;
                if (data.pointerDrag == null || data.delta.sqrMagnitude == 0) continue;
                float threshold = eventSystem.pixelDragThreshold;
                if (!data.dragging && (!data.useDragThreshold || (data.position - data.pressPosition).sqrMagnitude >= threshold * threshold))
                {
                    ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.beginDragHandler);
                    if (!state.pressed || !inputEnabled || !isActiveAndEnabled) continue;
                    data.dragging = true;
                    data.eligibleForClick = false;
                    if (data.pointerPress != data.pointerDrag)
                    {
                        if (data.pointerPress != null) ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);
                        data.pointerPress = null;
                        data.rawPointerPress = null;
                    }
                }
                if (data.dragging && data.pointerDrag != null) ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.dragHandler);
            }
        }

        private bool MapAndRaycast(PointerEventData data, bool allowHit)
        {
            data.Reset();
            bool inImage = TryMap(physicalPosition, DisplayEventCamera(), out Vector2 position);
            data.delta = position - data.position;
            data.position = position;
            data.pointerId = physicalPointerId;
            data.displayIndex = sourceCamera != null ? sourceCamera.targetDisplay : 0;
            data.pointerCurrentRaycast = default;
            if (!inImage || !allowHit || sourceRaycasters == null) return false;
            results.Clear();
            for (int i = 0; i < sourceRaycasters.Length; i++)
            {
                var raycaster = sourceRaycasters[i];
                if (raycaster == null || !raycaster.gameObject.activeInHierarchy) continue;
                // Disabled GraphicRaycasters can be manually queried; disabling
                // removes them only from the automatic EventSystem registry.
                raycaster.Raycast(data, results);
            }
            RaycastResult best = default;
            for (int i = 0; i < results.Count; i++)
                if (results[i].gameObject != null && (best.gameObject == null || CompareRaycasts(results[i], best) < 0)) best = results[i];
            data.pointerCurrentRaycast = best;
            return best.gameObject != null;
        }

        private void SetHover(GameObject target)
        {
            if (hoverData.pointerEnter == target)
            {
                if (target == null) hoverData.hovered.Clear();
                return;
            }
            Transform common = CommonParent(hoverData.pointerEnter, target);
            Transform current = hoverData.pointerEnter != null ? hoverData.pointerEnter.transform : null;
            while (current != null && current != common)
            {
                Transform parent = current.parent;
                hoverData.fullyExited = true;
                ExecuteEvents.Execute(current.gameObject, hoverData, ExecuteEvents.pointerExitHandler);
                hoverData.hovered.Remove(current.gameObject);
                current = parent;
            }
            hoverData.pointerEnter = target;
            current = target != null ? target.transform : null;
            while (current != null && current != common)
            {
                Transform parent = current.parent;
                hoverData.reentered = false;
                ExecuteEvents.Execute(current.gameObject, hoverData, ExecuteEvents.pointerEnterHandler);
                hoverData.hovered.Add(current.gameObject);
                current = parent;
            }
            if (target == null) hoverData.hovered.Clear();
        }

        private static Transform CommonParent(GameObject left, GameObject right)
        {
            if (left == null || right == null) return null;
            for (Transform a = left.transform; a != null; a = a.parent)
                for (Transform b = right.transform; b != null; b = b.parent)
                    if (a == b) return a;
            return null;
        }

        private Camera DisplayEventCamera()
        {
            if (displayCanvas == null && displayRect != null) displayCanvas = displayRect.GetComponentInParent<Canvas>();
            return displayCanvas != null && displayCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? displayCanvas.worldCamera : null;
        }

        private bool Owns(GameObject target)
        {
            if (target == null || sourceRaycasters == null) return false;
            for (int i = 0; i < sourceRaycasters.Length; i++)
                if (sourceRaycasters[i] != null && target.transform.IsChildOf(sourceRaycasters[i].transform)) return true;
            return false;
        }

        private static void ClearPress(ButtonState state)
        {
            state.pressed = false;
            state.clickTarget = null;
            var data = state.data;
            data.eligibleForClick = false;
            data.dragging = false;
            data.pointerPress = null;
            data.rawPointerPress = null;
            data.pointerClick = null;
            data.pointerDrag = null;
            data.pointerPressRaycast = default;
        }

        private static int CompareRaycasts(RaycastResult a, RaycastResult b)
        {
            if (a.module != b.module)
            {
                Camera ac = a.module.eventCamera, bc = b.module.eventCamera;
                if (ac != null && bc != null && ac.depth != bc.depth) return bc.depth.CompareTo(ac.depth);
                if (a.module.sortOrderPriority != b.module.sortOrderPriority) return b.module.sortOrderPriority.CompareTo(a.module.sortOrderPriority);
                if (a.module.renderOrderPriority != b.module.renderOrderPriority) return b.module.renderOrderPriority.CompareTo(a.module.renderOrderPriority);
            }
            if (a.sortingLayer != b.sortingLayer) return SortingLayer.GetLayerValueFromID(b.sortingLayer).CompareTo(SortingLayer.GetLayerValueFromID(a.sortingLayer));
            if (a.sortingOrder != b.sortingOrder) return b.sortingOrder.CompareTo(a.sortingOrder);
            if (a.depth != b.depth && a.module.rootRaycaster == b.module.rootRaycaster) return b.depth.CompareTo(a.depth);
            if (a.distance != b.distance) return a.distance.CompareTo(b.distance);
            return a.index.CompareTo(b.index);
        }
    }
}
