using System;
using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>Refreshes explicitly assigned native controls without recreating their data or listeners.</summary>
    [DisallowMultipleComponent]
    public sealed class DesktopNativeContentLocalization : MonoBehaviour
    {
        public LocalizationManager languageManager;
        public ContextMenuManager[] contextMenus = Array.Empty<ContextMenuManager>();
        public HorizontalSelector[] selectors = Array.Empty<HorizontalSelector>();
        public GameHubManager[] gameHubs = Array.Empty<GameHubManager>();

        private readonly Dictionary<HorizontalSelector.Item, ItemSource> sources =
            new Dictionary<HorizontalSelector.Item, ItemSource>();
        private Coroutine pendingHubRefresh;
        private bool started;

        private sealed class ItemSource
        {
            public string original;
            public string output;
            public bool externallyChanged;
        }

        private void OnEnable()
        {
            GameText.Changed += OnLanguageChanged;
            if (started) Refresh();
        }

        private void Start()
        {
            started = true;
            Refresh();
        }

        private void OnDisable()
        {
            GameText.Changed -= OnLanguageChanged;
            if (pendingHubRefresh != null) StopCoroutine(pendingHubRefresh);
            pendingHubRefresh = null;
        }

        private void OnLanguageChanged()
        {
            // Native Awake/OnEnable must finish before invoking native UpdateUI methods.
            if (started) Refresh();
        }

        public void Refresh()
        {
            if (languageManager == null) languageManager = LocalizationManager.instance;
            if (contextMenus != null)
                foreach (var menu in contextMenus)
                    if (menu != null && menu.isActiveAndEnabled && menu.isOn && menu.contentRect != null) menu.Close();

            var liveItems = new HashSet<HorizontalSelector.Item>();
            if (selectors != null)
                foreach (var selector in selectors)
                {
                    if (selector == null || IsLanguageSelector(selector) || selector.items == null) continue;
                    foreach (var item in selector.items)
                    {
                        if (item == null) continue;
                        liveItems.Add(item);
                        if (!sources.TryGetValue(item, out var source))
                        {
                            source = new ItemSource { original = item.itemTitle };
                            sources.Add(item, source);
                        }
                        if (source.externallyChanged) continue;
                        if (source.output != null && item.itemTitle != source.output && item.itemTitle != source.original)
                        {
                            source.externallyChanged = true;
                            continue;
                        }

                        string value = NativeOutput(selector, item.localizationKey);
                        if (string.IsNullOrEmpty(value)) value = TranslateAuthoredItem(source.original);
                        item.itemTitle = source.output = value;
                    }

                    // UpdateUI does not invoke selection events. Never change index/defaultIndex.
                    if (selector.isActiveAndEnabled && selector.label != null &&
                        selector.index >= 0 && selector.index < selector.items.Count)
                        selector.UpdateUI();
                }

            // Reminder hour/minute lists are rebuilt natively; do not retain abandoned items forever.
            var removed = new List<HorizontalSelector.Item>();
            foreach (var item in sources.Keys) if (!liveItems.Contains(item)) removed.Add(item);
            foreach (var item in removed) sources.Remove(item);

            if (isActiveAndEnabled && gameHubs != null && gameHubs.Length > 0)
            {
                if (pendingHubRefresh != null) StopCoroutine(pendingHubRefresh);
                pendingHubRefresh = StartCoroutine(RefreshGameHubsAfterInitialization());
            }
        }

        private bool IsLanguageSelector(HorizontalSelector selector)
        {
            return languageManager != null && languageManager.languageSelectors != null &&
                languageManager.languageSelectors.Contains(selector);
        }

        private string NativeOutput(HorizontalSelector selector, string key)
        {
            if (string.IsNullOrEmpty(key) || languageManager == null) return null;
            var language = languageManager.currentLanguageAsset;
            var localized = selector.GetComponent<LocalizedObject>();
            if (language == null || localized == null || language.tableList == null ||
                localized.tableIndex < 0 || localized.tableIndex >= language.tableList.Count) return null;
            var table = language.tableList[localized.tableIndex];
            if (table == null || table.tableContent == null) return null;
            foreach (var entry in table.tableContent)
                if (entry != null && entry.key == key) return entry.value;
            return null;
        }

        private static string TranslateAuthoredItem(string source)
        {
            switch (source)
            {
                case "AM": return GameText.T("上午", "AM");
                case "PM": return GameText.T("下午", "PM");
                case "Once": return GameText.T("仅一次", "Once");
                case "Daily": return GameText.T("每天", "Daily");
                default: return GameText.Source(source);
            }
        }

        private IEnumerator RefreshGameHubsAfterInitialization()
        {
            yield return null;
            pendingHubRefresh = null;
            if (gameHubs == null) yield break;
            foreach (var hub in gameHubs)
            {
                if (hub == null || !hub.isActiveAndEnabled || hub.games == null || hub.games.Count == 0) continue;
                // A live native indicator proves Awake created the slider; the frame delay lets
                // native OnEnable select its current indicator. Inactive hubs refresh themselves on open.
                bool initialized = false;
                foreach (var indicator in hub.GetComponentsInChildren<GameHubSliderIndicatorItem>(true))
                    if (indicator != null && indicator.bar != null && indicator.animator != null &&
                        indicator.gameIndex >= 0 && indicator.gameIndex < hub.games.Count)
                    { initialized = true; break; }
                if (initialized) hub.UpdateSliderInfo();
            }
        }
    }
}
