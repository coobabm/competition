using Michsky.UI.Reach;
using UnityEngine;

namespace Emergence
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class SparkMenuLocaleBootstrap : MonoBehaviour
    {
        private const string LanguagePreferenceKey = "Spark.Menu.Language";

        public LocalizationManager manager;
        public SettingsDescriptionManager settingsDescription;

        private HorizontalSelector languageSelector;

        private void Awake()
        {
            if (manager == null) return;

            // Reach still owns locale changes; this menu only owns persistence.
            manager.setLanguageOnAwake = false;
            manager.saveLanguageChanges = false;
            languageSelector = manager.languageSelector;
            if (languageSelector != null) languageSelector.saveSelected = false;

            if (manager.UIManagerAsset == null || !manager.UIManagerAsset.enableLocalization) return;
            LocalizationSettings settings = manager.UIManagerAsset.localizationSettings;
            if (settings == null || settings.languages == null) return;

            string languageID = ResolveLanguage(settings);
            if (string.IsNullOrEmpty(languageID)) return;

            // Run before Reach's -100 Awake so its selector starts at this locale.
            manager.SetLanguage(languageID);
            if (languageSelector != null && languageSelector.onValueChanged != null)
                languageSelector.onValueChanged.AddListener(OnLanguageSelected);
        }

        private static string ResolveLanguage(LocalizationSettings settings)
        {
            string savedLanguage = PlayerPrefs.GetString(LanguagePreferenceKey, "en-US");
            if (IsAvailable(settings, savedLanguage)) return savedLanguage;
            if (IsAvailable(settings, settings.defaultLanguageID)) return settings.defaultLanguageID;
            if (IsAvailable(settings, "en-US")) return "en-US";

            foreach (LocalizationSettings.Language language in settings.languages)
            {
                if (language != null && language.localizationLanguage != null &&
                    !string.IsNullOrEmpty(language.languageID)) return language.languageID;
            }

            return null;
        }

        private static bool IsAvailable(LocalizationSettings settings, string languageID)
        {
            if (string.IsNullOrEmpty(languageID)) return false;
            foreach (LocalizationSettings.Language language in settings.languages)
            {
                if (language != null && language.localizationLanguage != null &&
                    language.languageID == languageID) return true;
            }

            return false;
        }

        private void OnLanguageSelected(int index)
        {
            // HorizontalSelector invokes the item's SetLanguage listener first.
            if (manager == null || manager.currentLanguageAsset == null) return;

            PlayerPrefs.SetString(LanguagePreferenceKey, manager.currentLanguageAsset.languageID);
            PlayerPrefs.Save();

            if (settingsDescription != null && settingsDescription.isActiveAndEnabled)
                settingsDescription.SetDefault();
        }

        private void OnDestroy()
        {
            if (languageSelector != null && languageSelector.onValueChanged != null)
                languageSelector.onValueChanged.RemoveListener(OnLanguageSelected);
        }
    }
}
