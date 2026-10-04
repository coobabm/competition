using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/UI Elements/Context Menu Content")]
    public class ContextMenuContent : MonoBehaviour, IPointerClickHandler
    {
        [Header("Resources")]
        public ContextMenuManager contextManager;
        public Transform itemParent;

        [Header("Items")]
        public List<MenuItem> menuItems = new List<MenuItem>();

        // Helpers
        GameObject selectedItem;

        [System.Serializable]
        public class MenuItem
        {
            public string itemText;
            public string localizationKey;
            public Sprite itemIcon;
            public ContextItemType contextItemType;
            public UnityEvent onClick = new UnityEvent();
        }

        public enum ContextItemType { Button, Separator }

        void Awake()
        {
            // Check if raycasting is available
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }
        }

        void Start()
        {
            if (contextManager == null)
            {
#if UNITY_2023_2_OR_NEWER
                try { contextManager = FindObjectsByType<ContextMenuManager>(FindObjectsSortMode.None)[0]; }
#else
                try { contextManager = (ContextMenuManager)FindObjectsOfType(typeof(ContextMenuManager))[0]; }
#endif
                catch { Debug.Log("<b>[Context Menu]</b> Context Manager is missing.", this); return; }
            }

            itemParent = contextManager.contentRect.transform.Find("Item List").transform;
            foreach (Transform child in itemParent) { Destroy(child.gameObject); }
        }

        public void ProcessContent()
        {
            foreach (Transform child in itemParent) { Destroy(child.gameObject); }
            for (int i = 0; i < menuItems.Count; ++i)
            {
                bool skipProcess = false;

                if (menuItems[i].contextItemType == ContextItemType.Button && contextManager.buttonPreset != null) { selectedItem = contextManager.buttonPreset; }
                else if (menuItems[i].contextItemType == ContextItemType.Separator && contextManager.separatorPreset != null) { selectedItem = contextManager.separatorPreset; }
                else
                {
                    Debug.LogError("<b>[Context Menu]</b> At least one of the item preset is missing.", this);
                    skipProcess = true;
                }

                if (!skipProcess)
                {
                    GameObject go = Instantiate(selectedItem, new Vector3(0, 0, 0), Quaternion.identity);
                    go.transform.SetParent(itemParent, false);

                    if (menuItems[i].contextItemType == ContextItemType.Button)
                    { 
                        ButtonManager btn = go.GetComponent<ButtonManager>();

                        // Check for localization
                        LocalizedObject tempLoc = btn.gameObject.GetComponent<LocalizedObject>();
                        if (string.IsNullOrEmpty(menuItems[i].localizationKey) || tempLoc == null || !tempLoc.CheckLocalizationStatus()) { btn.SetText(menuItems[i].itemText); }
                        else if (tempLoc != null) { btn.SetText(tempLoc.GetKeyOutput(menuItems[i].localizationKey)); }

                        // Set icon
                        if (menuItems[i].itemIcon == null) { btn.SetIcon(null); }
                        else { btn.SetIcon(menuItems[i].itemIcon); }

                        // Add events
                        ButtonManager itemButton = go.GetComponent<ButtonManager>();
                        itemButton.onClick.AddListener(menuItems[i].onClick.Invoke);
                        itemButton.onClick.AddListener(contextManager.Close);
                    }

                    StopCoroutine("ExecuteAfterTime");
                    StartCoroutine("ExecuteAfterTime", 0.01f);
                }
            }

            contextManager.SetContextMenuPosition();
            contextManager.Open();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (contextManager.isOn) { contextManager.Close(); }
            else if (eventData.button == PointerEventData.InputButton.Right && !contextManager.isOn) { ProcessContent(); }
        }

        IEnumerator ExecuteAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            itemParent.gameObject.SetActive(false);
            itemParent.gameObject.SetActive(true);
        }

        public void CreateNewButton(string title, Sprite icon)
        {
            MenuItem item = new MenuItem();
            item.itemText = title;
            item.itemIcon = icon;
            menuItems.Add(item);
        }
    }
}