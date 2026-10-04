using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Michsky.DreamOS
{
    [DisallowMultipleComponent]
    public class ItemDragContainer : MonoBehaviour
    {
        [Header("Resources")]
        public RectTransform dragBorder;
        [HideInInspector] public GridLayoutGroup gridLayoutGroup;
        [HideInInspector] public HorizontalLayoutGroup horLayoutGroup;
        [HideInInspector] public VerticalLayoutGroup verLayoutGroup;

        [Header("Settings")]
        public PreferredLayout preferredLayout = PreferredLayout.Grid;
        public DragMode dragMode = DragMode.Free;

        [HideInInspector] public List<ItemDragger> items = new List<ItemDragger>();
        public GameObject objectBeingDragged { get; set; }
        public enum DragMode { Snapped, Free }
        public enum PreferredLayout { Grid, Horizontal, Vertical }

        void Awake()
        {
            objectBeingDragged = null;

            if (dragBorder == null) { dragBorder = gameObject.GetComponent<RectTransform>(); }
            if (preferredLayout == PreferredLayout.Grid) { gridLayoutGroup = gameObject.GetComponent<GridLayoutGroup>(); }
            else if (preferredLayout == PreferredLayout.Horizontal) { horLayoutGroup = gameObject.GetComponent<HorizontalLayoutGroup>(); }
            else if (preferredLayout == PreferredLayout.Vertical) { verLayoutGroup = gameObject.GetComponent<VerticalLayoutGroup>(); }
        }

        void OnEnable()
        {
            if (gridLayoutGroup != null) 
            { 
                UpdateDragMode(); 
            }
        }

        public void FreeDragMode() 
        {
            // Unity Grid Layout Group being an ahole, so we're delaying the method call
            Invoke("FreeDragModeHelper", 0.1f);
        }

        void FreeDragModeHelper()
        {
            dragMode = DragMode.Free;
            gridLayoutGroup.enabled = false;

            for (int i = 0; i < items.Count; ++i)
            {
                if (items[i] == null)
                    return;

                items[i].UpdateObject();
            }
        }

        public void SnappedDragMode() 
        { 
            dragMode = DragMode.Snapped;
            gridLayoutGroup.enabled = true;
        }

        public void UpdateDragMode()
        {
            if (dragMode == DragMode.Free) { FreeDragMode(); }
            else { SnappedDragMode(); }
        }
    }
}