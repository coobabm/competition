using System;
using System.Collections;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>Owns only game locale persistence and bridges native locale changes to game presenters.</summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class DesktopLanguageController : MonoBehaviour
    {
        public const string PreferenceKey = "LingGuangV05.Desktop.Language";
        public LocalizationManager manager;
        public LocalizedObject changeSignal;
        public Transform desktopRoot;
        private IDisposable binding;
        private bool started;
        public string LanguageId => manager != null && manager.currentLanguageAsset != null
            ? GameText.Normalize(manager.currentLanguageAsset.languageID) : GameText.Chinese;

        private void Awake()
        {
            if (manager == null || manager.UIManagerAsset == null) return;
            manager.setLanguageOnAwake = false;
            manager.saveLanguageChanges = false;
            // Native Awake executes at -100. Configure it before any UI control can initialize.
            LocalizationManager.instance = manager;
            binding = GameText.Bind(() => LanguageId);
            manager.SetLanguage(GameText.Normalize(PlayerPrefs.GetString(PreferenceKey, GameText.Chinese)));
            if (changeSignal != null) changeSignal.onLanguageChanged.AddListener(OnNativeLanguageChanged);
        }
        private void Start() { started = true; ApplyLanguage(false); }
        private void OnDestroy()
        {
            if (changeSignal != null) changeSignal.onLanguageChanged.RemoveListener(OnNativeLanguageChanged);
            binding?.Dispose(); binding = null;
        }
        private void OnNativeLanguageChanged(string ignored) { ApplyLanguage(started); }
        public void SetLanguage(string languageId)
        {
            if (manager == null) return;
            manager.SetLanguage(GameText.Normalize(languageId));
            // The sentinel may still be inactive during bootstrap.
            if (changeSignal == null || !changeSignal.isInitialized) ApplyLanguage(started);
        }
        public void ToggleLanguage() => SetLanguage(LanguageId == GameText.Chinese ? GameText.English : GameText.Chinese);
        private void ApplyLanguage(bool persist)
        {
            if (persist)
            {
                PlayerPrefs.SetString(PreferenceKey, LanguageId);
                PlayerPrefs.Save();
            }
            foreach (HorizontalSelector selector in manager.languageSelectors)
            {
                if (selector == null || selector.items.Count == 0) continue;
                selector.index = selector.defaultIndex = LanguageId == GameText.English ? 1 : 0;
                selector.UpdateUI();
            }
            GameText.PublishChanged();
        }
    }
}
