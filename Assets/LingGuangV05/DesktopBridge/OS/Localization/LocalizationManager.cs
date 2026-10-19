using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Holds the desktop's current language: loads the saved choice from the System data file, pushes the language to
    /// every registered <see cref="LocalizedObject"/>, fills the language selectors in Settings, and saves the choice
    /// again. Replaces DreamOS's LocalizationManager (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class LocalizationManager : MonoBehaviour
    {
        // Static Instance
        public static LocalizationManager instance;

        // Resources
        public UIManager UIManagerAsset;
        public List<HorizontalSelector> languageSelectors = new List<HorizontalSelector>();

        // Tab_Settings
        public bool setLanguageOnAwake = true;
        public bool updateItemsOnSet = true;
        public bool saveLanguageChanges = true;
        public static bool enableLogs = true;

        // Helpers
        public string currentLanguage;
        public LocalizationLanguage currentLanguageAsset;
        public List<LocalizedObject> localizedItems = new List<LocalizedObject>();

        const string LanguageKey = "Language";
        readonly DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.System;

        /// <summary>First UIManager asset loaded in memory (the desktop only ever has one), or null.</summary>
        internal static UIManager FindUIManager()
        {
            Object[] managers = Resources.FindObjectsOfTypeAll(typeof(UIManager));
            return managers.Length > 0 ? (UIManager)managers[0] : null;
        }

        void Awake()
        {
            instance = this;

            if (UIManagerAsset == null) { UIManagerAsset = FindUIManager(); }
            if (UIManagerAsset == null || !UIManagerAsset.enableLocalization) { return; }
            if (setLanguageOnAwake) { InitializeLanguage(); }

            foreach (HorizontalSelector selector in languageSelectors)
            {
                if (selector != null) { PopulateLanguageSelector(selector); }
            }
        }

        void PopulateLanguageSelector(HorizontalSelector selector)
        {
            List<LocalizationSettings.Language> languages = UIManagerAsset.localizationSettings.languages;
            selector.items.Clear();

            for (int i = 0; i < languages.Count; i++)
            {
                selector.CreateNewItem(languages[i].localizedName);

                string languageID = languages[i].languageID;
                selector.items[i].onItemSelect.AddListener(() => SetLanguage(languageID));

                if (languages[i].localizationLanguage == currentLanguageAsset)
                {
                    selector.index = i;
                    selector.defaultIndex = i;
                }
            }

            selector.UpdateUI();
        }

        public void InitializeLanguage()
        {
            if (DreamOSDataManager.ContainsJsonKey(dataCat, LanguageKey)) { currentLanguage = DreamOSDataManager.ReadStringData(dataCat, LanguageKey); }
            else { currentLanguage = UIManagerAsset.localizationSettings.defaultLanguageID; }

            SetLanguage(currentLanguage);
        }

        public void SetLanguageByIndex(int index)
        {
            SetLanguage(UIManagerAsset.localizationSettings.languages[index].languageID);
        }

        public void SetLanguage(string langID)
        {
            if (UIManagerAsset == null || !UIManagerAsset.enableLocalization)
            {
                UIManager.isLocalizationEnabled = false;
                return;
            }

            currentLanguageAsset = FindLanguageAsset(langID);

            if (currentLanguageAsset == null) { Debug.Log("<b>[Localization Manager]</b> No language named <b>" + langID + "</b> found.", this); return; }
            currentLanguage = currentLanguageAsset.languageName + " (" + currentLanguageAsset.languageID + ")";

            if (updateItemsOnSet) { RefreshLocalizedItems(); }

            if (saveLanguageChanges) { DreamOSDataManager.WriteStringData(dataCat, LanguageKey, currentLanguageAsset.languageID); }

            UIManagerAsset.currentLanguage = currentLanguageAsset;
            UIManager.isLocalizationEnabled = true;
        }

        public static void SetLanguageWithoutNotify(string langID)
        {
            DreamOSDataManager.WriteStringData(DreamOSDataManager.DataCategory.System, LanguageKey, langID);
        }

        /// <summary>
        /// Accepts a language ID, "Name (ID)" (the form kept in currentLanguage), or the vendor's third spelling
        /// (languageName == langID + ")"). The first language that matches any form wins, even if its asset is unset.
        /// </summary>
        LocalizationLanguage FindLanguageAsset(string langID)
        {
            foreach (LocalizationSettings.Language language in UIManagerAsset.localizationSettings.languages)
            {
                if (language.languageID == langID
                    || language.languageName + " (" + language.languageID + ")" == langID
                    || language.languageName == langID + ")")
                {
                    return language.localizationLanguage;
                }
            }

            return null;
        }

        void RefreshLocalizedItems()
        {
            // Destroyed items are dropped as they are met. The index is deliberately not stepped back after a removal,
            // so the item that slides into the freed slot is skipped this round, as it always was.
            for (int i = 0; i < localizedItems.Count; i++)
            {
                if (localizedItems[i] == null) { localizedItems.RemoveAt(i); }
                else if (localizedItems[i].gameObject.activeInHierarchy && localizedItems[i].updateMode != LocalizedObject.UpdateMode.OnDemand) { localizedItems[i].UpdateItem(); }
            }
        }
    }
}
