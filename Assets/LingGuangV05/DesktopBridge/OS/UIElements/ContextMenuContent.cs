using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Makes an area of the desktop answer right-clicks with a menu (2026-10-19). Replaces DreamOS's
    /// ContextMenuContent: same GUID, serialized fields and public API.
    ///
    /// The item list is plain data (<see cref="menuItems"/>, which other scripts add to and remove from at runtime).
    /// On a right-click it rebuilds the shared menu's "Item List" from the manager's button/separator presets, wires each
    /// button's click to the item's event followed by closing the menu, then positions and opens the menu; any click while
    /// it is open closes it. Item text goes through <see cref="LocalizedObject"/> when a key is given and localization is on.
    /// </summary>
    [AddComponentMenu("LingGuang/OS/UI Elements/Context Menu Content")]
    public class ContextMenuContent : MonoBehaviour, IPointerClickHandler
    {
        [Header("Resources")]
        public ContextMenuManager contextManager;
        public Transform itemParent;

        [Header("Items")]
        public List<MenuItem> menuItems = new List<MenuItem>();

        Coroutine refreshRoutine;

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
            // The area needs a raycast target to receive clicks.
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
                ContextMenuManager[] managers = FindObjectsByType<ContextMenuManager>(FindObjectsSortMode.None);
#else
                ContextMenuManager[] managers = FindObjectsOfType<ContextMenuManager>();
#endif
                if (managers.Length == 0)
                {
                    Debug.Log("<b>[Context Menu]</b> Context Manager is missing.", this);
                    return;
                }

                contextManager = managers[0];
            }

            itemParent = contextManager.contentRect.transform.Find("Item List").transform;
            ClearItems();
        }

        public void ProcessContent()
        {
            ClearItems();

            bool builtAny = false;
            for (int i = 0; i < menuItems.Count; i++)
            {
                MenuItem item = menuItems[i];
                GameObject preset = item.contextItemType == ContextItemType.Button ? contextManager.buttonPreset : contextManager.separatorPreset;
                if (preset == null)
                {
                    Debug.LogError("<b>[Context Menu]</b> At least one of the item preset is missing.", this);
                    continue;
                }

                GameObject go = Instantiate(preset, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(itemParent, false);
                builtAny = true;

                if (item.contextItemType == ContextItemType.Button) { SetUpButton(go.GetComponent<ButtonManager>(), item); }
            }

            // The layout needs a nudge once the new items exist.
            if (builtAny)
            {
                if (refreshRoutine != null) { StopCoroutine(refreshRoutine); }
                refreshRoutine = StartCoroutine(ExecuteAfterTime(0.01f));
            }

            contextManager.SetContextMenuPosition();
            contextManager.Open();
        }

        void SetUpButton(ButtonManager button, MenuItem item)
        {
            LocalizedObject localized = button.gameObject.GetComponent<LocalizedObject>();
            if (string.IsNullOrEmpty(item.localizationKey) || localized == null || !localized.CheckLocalizationStatus()) { button.SetText(item.itemText); }
            else { button.SetText(localized.GetKeyOutput(item.localizationKey)); }

            button.SetIcon(item.itemIcon);

            button.onClick.AddListener(item.onClick.Invoke);
            button.onClick.AddListener(contextManager.Close);
        }

        void ClearItems()
        {
            foreach (Transform child in itemParent) { Destroy(child.gameObject); }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (contextManager.isOn) { contextManager.Close(); }
            else if (eventData.button == PointerEventData.InputButton.Right) { ProcessContent(); }
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
