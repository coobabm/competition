using System.Collections.Generic;
using UnityEngine;

namespace Michsky.DreamOS
{
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
            // We don't need to process further if there are no items
            if (widgetItems.Count == 0 || widgetParent == null)
                return;

            // Delete each cached items before creating presets
            foreach (Transform child in widgetParent) { Destroy(child.gameObject); }
            foreach (Transform child in libraryParent) { Destroy(child.gameObject); }

            // Process each widget item
            for (int i = 0; i < widgetItems.Count; ++i)
            {
                // Spawn widget item
                GameObject go = Instantiate(widgetItems[i].widgetPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(widgetParent, false);
                go.gameObject.name = widgetItems[i].title;

                // Get and set the widger preset component
                WidgetPreset tempPreset = go.GetComponent<WidgetPreset>();
                tempPreset.manager = this;
                tempPreset.index = i;
                tempPreset.ID = widgetItems[i].ID;
                tempPreset.defaultState = widgetItems[i].defaultState;
                widgetItems[i].preset = tempPreset;

                if (tempPreset.GetComponent<WindowDragger>() != null)
                {
                    WindowDragger tempDragger = tempPreset.GetComponent<WindowDragger>();
                    tempDragger.dragArea = widgetParent.GetComponent<RectTransform>();
                }

                // Spawn library item
                GameObject libraryGO = Instantiate(libraryItem, new Vector3(0, 0, 0), Quaternion.identity);
                libraryGO.transform.SetParent(libraryParent, false);
                libraryGO.gameObject.name = widgetItems[i].title;

                // Get and set the library item component
                WidgetLibraryItem tempLibPreset = libraryGO.GetComponent<WidgetLibraryItem>();
                tempLibPreset.manager = this;
                tempLibPreset.widgetIndex = i;
                tempLibPreset.iconImage.sprite = widgetItems[i].icon;
                widgetItems[i].libraryItem = tempLibPreset;
                tempLibPreset.itemSwitch.onEvents.AddListener(delegate { EnableWidget(tempLibPreset.widgetIndex); });
                tempLibPreset.itemSwitch.offEvents.AddListener(delegate { DisableWidget(tempLibPreset.widgetIndex); });

                // Check for localization
                LocalizedObject tempTitleLoc = tempLibPreset.titleText.gameObject.GetComponent<LocalizedObject>();
                LocalizedObject tempDescLoc = tempLibPreset.descriptionText.gameObject.GetComponent<LocalizedObject>();

                if (!useLocalization || string.IsNullOrEmpty(widgetItems[i].titleKey) || tempTitleLoc == null || !tempTitleLoc.CheckLocalizationStatus()) 
                {
                    tempLibPreset.titleText.text = widgetItems[i].title;
                    tempLibPreset.descriptionText.text = widgetItems[i].description;
                }

                else if (tempTitleLoc != null)
                {
                    tempTitleLoc.localizationKey = widgetItems[i].titleKey;
                    tempTitleLoc.onLanguageChanged.AddListener(delegate { tempLibPreset.titleText.text = tempTitleLoc.GetKeyOutput(tempTitleLoc.localizationKey); });
                    tempTitleLoc.InitializeItem();
                    tempTitleLoc.UpdateItem();

                    tempDescLoc.localizationKey = widgetItems[i].descriptionKey;
                    tempDescLoc.onLanguageChanged.AddListener(delegate { tempLibPreset.descriptionText.text = tempDescLoc.GetKeyOutput(tempDescLoc.localizationKey); });
                    tempDescLoc.InitializeItem();
                    tempDescLoc.UpdateItem();
                }

                // Enable the Widget Preset component
                tempPreset.enabled = true;
            }
        }

        public void EnableWidget(int widgetIndex)
        {
            widgetItems[widgetIndex].preset.SetEnabled();
        }

        public void EnableWidget(string widgetID)
        {
            for (int i = 0; i < widgetItems.Count; i++)
            {
                if (widgetItems[i].ID == widgetID)
                {
                    EnableWidget(widgetItems[i].preset.index);
                    break;
                }
            }
        }

        public void DisableWidget(int widgetIndex)
        {
            widgetItems[widgetIndex].preset.SetDisabled();
        }

        public void DisableWidget(string widgetID)
        {
            for (int i = 0; i < widgetItems.Count; i++)
            {
                if (widgetItems[i].ID == widgetID)
                {
                    DisableWidget(widgetItems[i].preset.index);
                    break;
                }
            }
        }
    }
}