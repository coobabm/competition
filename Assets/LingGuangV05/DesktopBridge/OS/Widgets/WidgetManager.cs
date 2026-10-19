using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The desktop widgets: spawns each configured widget onto the desktop plus a switch row in the widget library,
    /// and enables or disables them by index or ID.
    /// Replaces DreamOS's WidgetManager (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    public class WidgetManager : MonoBehaviour
    {
        // List
        public List<WidgetItem> widgetItems = new List<WidgetItem>();

        // Resources
        [SerializeField] private GameObject libraryItem;
        [SerializeField] private Transform libraryParent;
        [SerializeField] private Transform widgetParent;

        // Settings
        [SerializeField] private bool useLocalization = true;

        public enum DefaultWidgetState { Enabled, Disabled }

        [System.Serializable]
        public class WidgetItem
        {
            public string ID = "ID";
            public string title = "Title";
            [TextArea(2, 4)] public string description = "Description";
            public Sprite icon;
            public GameObject widgetPrefab;
            public DefaultWidgetState defaultState = DefaultWidgetState.Disabled;
            [HideInInspector] public WidgetPreset preset;
            [HideInInspector] public WidgetLibraryItem libraryItem;

            [Header("Localization")]
            public string titleKey;
            public string descriptionKey;
        }

        void Awake()
        {
            ListWidgets();
        }

        public void ListWidgets()
        {
            if (widgetItems.Count == 0 || widgetParent == null)
                return;

            foreach (Transform child in widgetParent) { Destroy(child.gameObject); }
            foreach (Transform child in libraryParent) { Destroy(child.gameObject); }

            for (int i = 0; i < widgetItems.Count; ++i)
            {
                WidgetPreset preset = SpawnWidget(i);
                SpawnLibraryItem(i);

                // The preset starts disabled and only restores its saved state once everything is wired
                preset.enabled = true;
            }
        }

        WidgetPreset SpawnWidget(int index)
        {
            WidgetItem item = widgetItems[index];

            GameObject go = Instantiate(item.widgetPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(widgetParent, false);
            go.gameObject.name = item.title;

            WidgetPreset preset = go.GetComponent<WidgetPreset>();
            preset.manager = this;
            preset.index = index;
            preset.ID = item.ID;
            preset.defaultState = item.defaultState;
            item.preset = preset;

            // A draggable widget may only be moved inside the widget area
            WindowDragger dragger = preset.GetComponent<WindowDragger>();
            if (dragger != null) { dragger.dragArea = widgetParent.GetComponent<RectTransform>(); }

            return preset;
        }

        void SpawnLibraryItem(int index)
        {
            WidgetItem item = widgetItems[index];

            GameObject go = Instantiate(libraryItem, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(libraryParent, false);
            go.gameObject.name = item.title;

            WidgetLibraryItem libraryEntry = go.GetComponent<WidgetLibraryItem>();
            libraryEntry.manager = this;
            libraryEntry.widgetIndex = index;
            libraryEntry.iconImage.sprite = item.icon;
            item.libraryItem = libraryEntry;
            libraryEntry.itemSwitch.onEvents.AddListener(delegate { EnableWidget(libraryEntry.widgetIndex); });
            libraryEntry.itemSwitch.offEvents.AddListener(delegate { DisableWidget(libraryEntry.widgetIndex); });

            // Plain text unless localization is on, keyed, and the label really is localized
            LocalizedObject titleLoc = libraryEntry.titleText.gameObject.GetComponent<LocalizedObject>();
            LocalizedObject descriptionLoc = libraryEntry.descriptionText.gameObject.GetComponent<LocalizedObject>();

            if (!useLocalization || string.IsNullOrEmpty(item.titleKey) || titleLoc == null || !titleLoc.CheckLocalizationStatus())
            {
                libraryEntry.titleText.text = item.title;
                libraryEntry.descriptionText.text = item.description;
            }
            else
            {
                titleLoc.localizationKey = item.titleKey;
                titleLoc.onLanguageChanged.AddListener(delegate { libraryEntry.titleText.text = titleLoc.GetKeyOutput(titleLoc.localizationKey); });
                titleLoc.InitializeItem();
                titleLoc.UpdateItem();

                descriptionLoc.localizationKey = item.descriptionKey;
                descriptionLoc.onLanguageChanged.AddListener(delegate { libraryEntry.descriptionText.text = descriptionLoc.GetKeyOutput(descriptionLoc.localizationKey); });
                descriptionLoc.InitializeItem();
                descriptionLoc.UpdateItem();
            }
        }

        public void EnableWidget(int widgetIndex)
        {
            widgetItems[widgetIndex].preset.SetEnabled();
        }

        public void EnableWidget(string widgetID)
        {
            int index = FindWidgetIndex(widgetID);
            if (index >= 0) { EnableWidget(widgetItems[index].preset.index); }
        }

        public void DisableWidget(int widgetIndex)
        {
            widgetItems[widgetIndex].preset.SetDisabled();
        }

        public void DisableWidget(string widgetID)
        {
            int index = FindWidgetIndex(widgetID);
            if (index >= 0) { DisableWidget(widgetItems[index].preset.index); }
        }

        int FindWidgetIndex(string widgetID)
        {
            for (int i = 0; i < widgetItems.Count; i++)
            {
                if (widgetItems[i].ID == widgetID) { return i; }
            }

            return -1;
        }
    }
}
