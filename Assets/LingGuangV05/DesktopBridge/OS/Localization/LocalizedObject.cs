using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Binds one UI element (a TMP text, an image, an audio source, or any component through the
    /// <see cref="onLanguageChanged"/> event) to a localization key, and re-applies the current language's value
    /// whenever it is enabled or the language changes. Also offers key lookups for scripts. Replaces DreamOS's
    /// LocalizedObject (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LingGuang/OS/Localization/Localized Object")]
    public class LocalizedObject : MonoBehaviour
    {
        // Resources
        public LocalizationSettings localizationSettings;
        public TextMeshProUGUI textObj;
        public AudioSource audioObj;
        public Image imageObj;

        // Settings
        public int tableIndex = -1;
        public string localizationKey;
        public ObjectType objectType = ObjectType.TextMeshPro;
        public UpdateMode updateMode = UpdateMode.OnEnable;
        public bool rebuildLayoutOnUpdate;
        [SerializeField] private bool forceAddToManager = false;
#if UNITY_EDITOR
        public bool showOutputOnEditor = true;
#endif

        // Events
        public LanguageChangedEvent onLanguageChanged = new LanguageChangedEvent();

        [System.Serializable]
        public class LanguageChangedEvent : UnityEvent<string> { }

        // Helpers
        public bool isInitialized = false;

        public enum UpdateMode { OnEnable, OnDemand }
        public enum ObjectType { TextMeshPro, Custom, ComponentDriven, Audio, Image }

        void Awake()
        {
            if (LocalizationManager.instance != null && !LocalizationManager.instance.UIManagerAsset.enableLocalization)
            {
                Destroy(this);
                return;
            }

            InitializeItem();
        }

        void OnEnable()
        {
            if (LocalizationManager.instance == null) { Destroy(this); return; }
            if (!isInitialized || LocalizationManager.instance.currentLanguageAsset == null) { return; }
            if (updateMode == UpdateMode.OnEnable) { UpdateItem(); }
        }

        public void InitializeItem()
        {
            if (isInitialized)
                return;

            if (LocalizationManager.instance == null)
            {
                UIManager uiManager = LocalizationManager.FindUIManager();
                if (uiManager == null || !uiManager.enableLocalization) { return; }

                GameObject autoManager = new GameObject("Localization Manager [Auto Generated]");
                autoManager.AddComponent<LocalizationManager>();
            }

            LocalizationManager manager = LocalizationManager.instance;
            if (manager != null && !manager.UIManagerAsset.enableLocalization) { Destroy(this); return; }
            if (manager == null || manager.UIManagerAsset == null || !manager.UIManagerAsset.enableLocalization) { return; }
            if (forceAddToManager && !manager.localizedItems.Contains(this)) { manager.localizedItems.Add(this); }

            if (objectType == ObjectType.TextMeshPro && textObj == null) { textObj = gameObject.GetComponent<TextMeshProUGUI>(); }
            else if (objectType == ObjectType.Audio && audioObj == null) { audioObj = gameObject.GetComponent<AudioSource>(); }
            else if (objectType == ObjectType.Image && imageObj == null) { imageObj = gameObject.GetComponent<Image>(); }

            // Note: with forceAddToManager the item is registered twice, as before.
            manager.localizedItems.Add(this);
            isInitialized = true;
        }

        public void ReInitializeItem()
        {
            isInitialized = false;
            InitializeItem();
        }

        public void UpdateItem()
        {
            if (!CanReadTables())
                return;

            switch (objectType)
            {
                case ObjectType.TextMeshPro:
                    if (textObj != null) { ApplyText(); }
                    break;
                case ObjectType.Audio:
                    if (audioObj != null) { ApplyAudio(); }
                    break;
                case ObjectType.Image:
                    if (imageObj != null) { ApplySprite(); }
                    break;
                case ObjectType.Custom:
                case ObjectType.ComponentDriven:
                    ApplyToListeners();
                    break;
            }

            if (rebuildLayoutOnUpdate && gameObject.activeInHierarchy) { StartCoroutine("RebuildLayout"); }
        }

        void ApplyText()
        {
            LocalizationLanguage.TableContent entry = FindEntry(localizationKey);
            if (entry == null) { return; }

            if (string.IsNullOrEmpty(entry.value)) { LogEmptyForUpdate(); return; }

            textObj.text = entry.value;
            onLanguageChanged.Invoke(entry.value);
        }

        void ApplyAudio()
        {
            LocalizationLanguage.TableContent entry = FindEntry(localizationKey);
            if (entry == null) { return; }

            if (entry.audioValue == null) { LogEmptyForUpdate(); return; }

            audioObj.clip = entry.audioValue;
        }

        void ApplySprite()
        {
            LocalizationLanguage.TableContent entry = FindEntry(localizationKey);
            if (entry == null) { return; }

            if (entry.spriteValue == null) { LogEmptyForUpdate(); return; }

            imageObj.sprite = entry.spriteValue;
        }

        void ApplyToListeners()
        {
            LocalizationLanguage.TableContent entry = FindEntry(localizationKey);
            if (entry == null || string.IsNullOrEmpty(entry.value)) { return; }

            onLanguageChanged.Invoke(entry.value);
        }

        public bool CheckLocalizationStatus()
        {
            if (!isInitialized) { InitializeItem(); }
            LocalizationManager manager = LocalizationManager.instance;
            return manager != null && manager.UIManagerAsset != null && manager.UIManagerAsset.enableLocalization;
        }

        public string GetKeyOutput(string key)
        {
            if (!CanReadTablesAfterInit())
                return LocalizationSettings.notInitializedText;

            LocalizationLanguage language = LocalizationManager.instance.currentLanguageAsset;
            LocalizationLanguage.TableContent entry = FindEntry(key);

            if (entry != null && string.IsNullOrEmpty(entry.value))
            {
                LogKeyProblem("The output value for '" + key + "' is empty in " + language.languageName + ".");
                return "EMPTY_KEY_IN_" + language.languageID + ": " + key;
            }

            if (entry == null)
            {
                LogKeyProblem("The specified key '" + key + "' could not be found in " + language.languageName + ".");
                return "MISSING_KEY_IN_" + language.languageID + ": " + key;
            }

            return entry.value;
        }

        public AudioClip GetKeyOutputAudio(string key)
        {
            return GetAssetOutput(key, entry => entry.audioValue);
        }

        public Sprite GetKeyOutputSprite(string key)
        {
            return GetAssetOutput(key, entry => entry.spriteValue);
        }

        public static string GetKeyOutput(string tableID, string tableKey)
        {
            LocalizationLanguage.TableContent entry = FindEntry(tableID, tableKey);
            return entry?.value;
        }

        public static AudioClip GetKeyOutputAudio(string tableID, string tableKey)
        {
            LocalizationLanguage.TableContent entry = FindEntry(tableID, tableKey);
            return entry?.audioValue;
        }

        public static Sprite GetKeyOutputSprite(string tableID, string tableKey)
        {
            LocalizationLanguage.TableContent entry = FindEntry(tableID, tableKey);
            return entry?.spriteValue;
        }

        // The current language's table this object points at (throws if tableIndex is out of range, as before).
        LocalizationLanguage.TableList CurrentTable()
        {
            return LocalizationManager.instance.currentLanguageAsset.tableList[tableIndex];
        }

        LocalizationLanguage.TableContent FindEntry(string key)
        {
            foreach (LocalizationLanguage.TableContent entry in CurrentTable().tableContent)
            {
                if (key == entry.key) { return entry; }
            }

            return null;
        }

        // Static lookup through the UIManager's current language, by table ID and key; null when anything is missing.
        static LocalizationLanguage.TableContent FindEntry(string tableID, string tableKey)
        {
            UIManager uiManager = LocalizationManager.FindUIManager();
            if (uiManager == null || !uiManager.enableLocalization) { return null; }

            foreach (LocalizationLanguage.TableList tableList in uiManager.currentLanguage.tableList)
            {
                if (tableList.table.tableID != tableID) { continue; }

                foreach (LocalizationLanguage.TableContent entry in tableList.tableContent)
                {
                    if (entry.key == tableKey) { return entry; }
                }

                return null;
            }

            return null;
        }

        bool CanReadTables()
        {
            LocalizationManager manager = LocalizationManager.instance;
            return isInitialized && manager != null && manager.currentLanguageAsset != null && manager.currentLanguageAsset.tableList.Count != 0;
        }

        // The Get* lookups first make sure a language is loaded.
        bool CanReadTablesAfterInit()
        {
            LocalizationManager manager = LocalizationManager.instance;
            if (manager != null && manager.currentLanguageAsset == null) { manager.InitializeLanguage(); }
            return CanReadTables();
        }

        T GetAssetOutput<T>(string key, Func<LocalizationLanguage.TableContent, T> pick) where T : UnityEngine.Object
        {
            if (!CanReadTablesAfterInit())
                return null;

            LocalizationLanguage language = LocalizationManager.instance.currentLanguageAsset;
            LocalizationLanguage.TableContent entry = FindEntry(key);

            if (entry == null)
            {
                LogKeyProblem("The specified key '" + key + "' could not be found in " + language.languageName + ".");
                return null;
            }

            T value = pick(entry);
            if (value == null)
            {
                LogKeyProblem("The output value for '" + key + "' is empty in " + language.languageName + ".");
                return null;
            }

            return value;
        }

        void LogEmptyForUpdate()
        {
            LogKeyProblem("The specified key '" + localizationKey + "' could not be found or the output value is empty for " + LocalizationManager.instance.currentLanguageAsset.languageName + ".");
        }

        void LogKeyProblem(string message)
        {
            if (LocalizationManager.enableLogs) { Debug.Log("<b>[Localized Object]</b> " + message, this); }
        }

        IEnumerator RebuildLayout()
        {
            yield return new WaitForSecondsRealtime(0.025f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }
}
