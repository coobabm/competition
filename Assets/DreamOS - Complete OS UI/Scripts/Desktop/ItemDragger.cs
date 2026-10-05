using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.DreamOS
{
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
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.System;

        private RectTransform dragAreaInternal
        {
            get
            {
                if (dragContainer != null && dragContainer.dragBorder != null)
                {
                    RectTransform newArea = transform.parent as RectTransform;
                    return newArea;
                }
                else if (dragContainer != null) { return dragContainer.dragBorder; }
                else { return null; }
            }
        }

        public new void Start()
        {
            if (dragObject == null) { dragObject = GetComponent<RectTransform>(); }
            if (dragContainer == null) { dragContainer = gameObject.GetComponentInParent<ItemDragContainer>(); }
            if (dragContainer != null) { dragContainer.items.Add(this); }

            if (dragContainer == null)
                return;

            if (rememberPosition && dragContainer.dragMode == ItemDragContainer.DragMode.Snapped && DreamOSDataManager.ContainsJsonKey(dataCat, string.Format("{0}_{1}{2}", gameObject.name, saveKey, "Index")))
            {
                transform.SetSiblingIndex(DreamOSDataManager.ReadIntData(dataCat, string.Format("{0}_{1}{2}", gameObject.name, saveKey, "Index")));
            }

            else if (rememberPosition && dragContainer.dragMode == ItemDragContainer.DragMode.Free)
            {
                UpdateObject();
            }
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (dragContainer == null || data.button != PointerEventData.InputButton.Left)
                return;

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
            if (dragContainer == null || data.button != PointerEventData.InputButton.Left)
                return;

            if (dragContainer.dragMode == ItemDragContainer.DragMode.Free)
            {
                Vector2 localPointerPosition;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragAreaInternal, data.position, data.pressEventCamera, out localPointerPosition))
                {
                    Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
                    dragObject.localPosition = originalPanelLocalPosition + offsetToOriginal;
                }

                ClampToArea();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragContainer == null)
                return;

            if (dragContainer.dragMode == ItemDragContainer.DragMode.Free && rememberPosition) { UpdatePositionData(); }
            else if (dragContainer.dragMode == ItemDragContainer.DragMode.Snapped)
            {
                if (dragContainer.objectBeingDragged == gameObject) { dragContainer.objectBeingDragged = null; }
                if (rememberPosition) { UpdateIndexData(); }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (dragContainer == null || dragContainer.dragMode != ItemDragContainer.DragMode.Snapped)
                return;

            GameObject objectBeingDragged = dragContainer.objectBeingDragged;
            
            if (objectBeingDragged != null && objectBeingDragged != gameObject)
                objectBeingDragged.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }

        public void ClampToArea()
        {
            Vector3 pos = dragObject.localPosition;
            Vector3 minPosition = dragAreaInternal.rect.min - dragObject.rect.min;
            Vector3 maxPosition = dragAreaInternal.rect.max - dragObject.rect.max;
           
            pos.x = Mathf.Clamp(dragObject.localPosition.x, minPosition.x, maxPosition.x);
            pos.y = Mathf.Clamp(dragObject.localPosition.y, minPosition.y, maxPosition.y);

            dragObject.localPosition = pos;
        }

        public void UpdateObject(bool readData = true)
        {
            if (!rememberPosition || dragContainer == null || dragContainer.gridLayoutGroup == null) { return; }
            if (!DreamOSDataManager.ContainsJsonKey(dataCat, string.Format("{0}_{1}{2}", gameObject.name, saveKey, "PosX"))) { UpdatePositionData(); }
            if (readData)
            {
                float x = DreamOSDataManager.ReadFloatData(dataCat, string.Format("{0}_{1}{2}", gameObject.name, saveKey, "PosX"));
                float y = DreamOSDataManager.ReadFloatData(dataCat, string.Format("{0}_{1}{2}", gameObject.name, saveKey, "PosY"));

                Vector3 tempPos = new Vector3(x, y, 0);
                transform.position = tempPos;
                dragObject.sizeDelta = new Vector2(dragContainer.gridLayoutGroup.cellSize.x, dragContainer.gridLayoutGroup.cellSize.y);
            }
        }

        public void UpdatePositionData()
        {
            DreamOSDataManager.WriteFloatData(dataCat, string.Format("{0}_{1}{2}", gameObject.name, saveKey, "PosX"), transform.position.x);
            DreamOSDataManager.WriteFloatData(dataCat, string.Format("{0}_{1}{2}", gameObject.name, saveKey, "PosY"), transform.position.y);
        }

        public void UpdateIndexData(bool updateParent = true)
        {
            if (!updateParent) { DreamOSDataManager.WriteIntData(dataCat, string.Format("{0}_{1}{2}", gameObject.name, saveKey, "Index"), transform.GetSiblingIndex()); }
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