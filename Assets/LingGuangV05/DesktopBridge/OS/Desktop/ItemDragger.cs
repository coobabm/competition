using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// A draggable desktop item (icon or widget) inside an <see cref="ItemDragContainer"/> (2026-10-19).
    /// Replaces DreamOS's ItemDragger: same GUID, serialized fields and public API.
    ///
    /// Snapped containers reorder siblings as the item is dragged over its neighbours; Free containers move the
    /// item with the pointer, clamped to its parent. With <see cref="rememberPosition"/> the sibling index or the
    /// world position is saved in the System data category under "&lt;object name&gt;_&lt;saveKey&gt;Index" /
    /// "...PosX" / "...PosY" and restored on the next start.
    /// </summary>
    [DisallowMultipleComponent]
    public class ItemDragger : UIBehaviour, IPointerEnterHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Resources")]
        public ItemDragContainer dragContainer;
        private RectTransform dragObject;

        [Header("Settings")]
        public bool rememberPosition = false;
        [SerializeField] private string saveKey;

        // Helpers
        private Vector2 originalLocalPointerPosition;
        private Vector3 originalPanelLocalPosition;
        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.System;

        // Items move inside their parent when the container has a drag border (the usual case).
        private RectTransform dragAreaInternal
        {
            get
            {
                if (dragContainer != null && dragContainer.dragBorder != null) { return transform.parent as RectTransform; }
                else if (dragContainer != null) { return dragContainer.dragBorder; }
                else { return null; }
            }
        }

        string DataKey(string suffix) { return string.Format("{0}_{1}{2}", gameObject.name, saveKey, suffix); }

        public new void Start()
        {
            if (dragObject == null) { dragObject = GetComponent<RectTransform>(); }
            if (dragContainer == null) { dragContainer = gameObject.GetComponentInParent<ItemDragContainer>(); }
            if (dragContainer != null) { dragContainer.items.Add(this); }

            if (dragContainer == null) { return; }

            if (rememberPosition && dragContainer.dragMode == ItemDragContainer.DragMode.Snapped && DreamOSDataManager.ContainsJsonKey(dataCat, DataKey("Index")))
            {
                transform.SetSiblingIndex(DreamOSDataManager.ReadIntData(dataCat, DataKey("Index")));
            }
            else if (rememberPosition && dragContainer.dragMode == ItemDragContainer.DragMode.Free)
            {
                UpdateObject();
            }
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (dragContainer == null || data.button != PointerEventData.InputButton.Left) { return; }

            if (dragContainer.dragMode == ItemDragContainer.DragMode.Snapped) { dragContainer.objectBeingDragged = gameObject; }
            else
            {
                transform.SetAsLastSibling();
                originalPanelLocalPosition = dragObject.localPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(dragAreaInternal, data.position, data.pressEventCamera, out originalLocalPointerPosition);
            }
        }

        public void OnDrag(PointerEventData data)
        {
            if (dragContainer == null || data.button != PointerEventData.InputButton.Left) { return; }
            if (dragContainer.dragMode != ItemDragContainer.DragMode.Free) { return; }

            Vector2 localPointerPosition;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragAreaInternal, data.position, data.pressEventCamera, out localPointerPosition))
            {
                Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
                dragObject.localPosition = originalPanelLocalPosition + offsetToOriginal;
            }

            ClampToArea();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragContainer == null) { return; }

            if (dragContainer.dragMode == ItemDragContainer.DragMode.Free && rememberPosition) { UpdatePositionData(); }
            else if (dragContainer.dragMode == ItemDragContainer.DragMode.Snapped)
            {
                if (dragContainer.objectBeingDragged == gameObject) { dragContainer.objectBeingDragged = null; }
                if (rememberPosition) { UpdateIndexData(); }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (dragContainer == null || dragContainer.dragMode != ItemDragContainer.DragMode.Snapped) { return; }

            GameObject objectBeingDragged = dragContainer.objectBeingDragged;

            if (objectBeingDragged != null && objectBeingDragged != gameObject)
            {
                objectBeingDragged.transform.SetSiblingIndex(transform.GetSiblingIndex());
            }
        }

        /// <summary>Keeps the item fully inside the drag area.</summary>
        public void ClampToArea()
        {
            RectTransform area = dragAreaInternal;

            Vector3 pos = dragObject.localPosition;
            Vector3 minPosition = area.rect.min - dragObject.rect.min;
            Vector3 maxPosition = area.rect.max - dragObject.rect.max;

            pos.x = Mathf.Clamp(pos.x, minPosition.x, maxPosition.x);
            pos.y = Mathf.Clamp(pos.y, minPosition.y, maxPosition.y);

            dragObject.localPosition = pos;
        }

        /// <summary>
        /// Free mode with a grid: puts the item back where it was saved (saving the current spot first if none
        /// exists) and gives it the grid's cell size. <paramref name="readData"/> false only ensures a save exists.
        /// </summary>
        public void UpdateObject(bool readData = true)
        {
            if (!rememberPosition || dragContainer == null || dragContainer.gridLayoutGroup == null) { return; }
            if (!DreamOSDataManager.ContainsJsonKey(dataCat, DataKey("PosX"))) { UpdatePositionData(); }
            if (readData)
            {
                float x = DreamOSDataManager.ReadFloatData(dataCat, DataKey("PosX"));
                float y = DreamOSDataManager.ReadFloatData(dataCat, DataKey("PosY"));

                transform.position = new Vector3(x, y, 0);
                dragObject.sizeDelta = new Vector2(dragContainer.gridLayoutGroup.cellSize.x, dragContainer.gridLayoutGroup.cellSize.y);
            }
        }

        public void UpdatePositionData()
        {
            DreamOSDataManager.WriteFloatData(dataCat, DataKey("PosX"), transform.position.x);
            DreamOSDataManager.WriteFloatData(dataCat, DataKey("PosY"), transform.position.y);
        }

        /// <summary>Saves this item's sibling index, or (default) that of every ItemDragger in the container.</summary>
        public void UpdateIndexData(bool updateParent = true)
        {
            if (!updateParent) { DreamOSDataManager.WriteIntData(dataCat, DataKey("Index"), transform.GetSiblingIndex()); }
            else
            {
                for (int i = 0; i < dragContainer.transform.childCount; ++i)
                {
                    dragContainer.transform.GetChild(i).GetComponent<ItemDragger>().UpdateIndexData(false);
                }
            }
        }
    }
}
