using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The "&lt; Item &gt;" picker used by the settings pages (language, resolution, ...). Shows one item at a time with
    /// a cross-fading helper label, optional icon and dot indicators; the choice can be kept in the DreamOS data
    /// files. Replaces DreamOS's HorizontalSelector (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// The animator is switched off a moment after every transition so it does not fight the layout the rest of the
    /// time.
    /// </summary>
    [DisallowMultipleComponent]
    public class HorizontalSelector : MonoBehaviour
    {
        // Resources
        public TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI labelHelper;
        public Image labelIcon;
        [SerializeField] private Image labelIconHelper;
        public Transform indicatorParent;
        public GameObject indicatorObject;
        [SerializeField] private Animator selectorAnimator;
        [SerializeField] private HorizontalLayoutGroup contentLayout;
        [SerializeField] private HorizontalLayoutGroup contentLayoutHelper;

        // Saving
        public bool saveSelected = false;
        public string saveKey = "DataKey";
        public DreamOSDataManager.DataCategory dataCategory = DreamOSDataManager.DataCategory.System;

        // Tab_Settings
        public bool enableIndicator = true;
        public bool enableIcon = false;
        public bool invokeOnAwake = true;
        public bool invertAnimation;
        public bool loopSelection;
        public bool useLocalization = true;
        [Range(0.25f, 2.5f)] public float iconScale = 1;
        [Range(1, 50)] public int contentSpacing = 15;
        public int defaultIndex = 0;
        [HideInInspector] public int index = 0;

        // Items
        public List<Item> items = new List<Item>();

        // Events
        public HorizontalSelectorEvent onValueChanged = new HorizontalSelectorEvent();

        // Helpers
        LocalizedObject localizedObject;
        float cachedStateLength;

        [System.Serializable]
        public class Item
        {
            public string itemTitle = "Item Title";
            public string localizationKey;
            public Sprite itemIcon;
            public UnityEvent onItemSelect = new UnityEvent();
        }

        [System.Serializable]
        public class HorizontalSelectorEvent : UnityEvent<int> { }

        void Awake()
        {
            if (selectorAnimator == null) { selectorAnimator = gameObject.GetComponent<Animator>(); }
            if (label == null || labelHelper == null)
            {
                Debug.LogError("<b>[Horizontal Selector]</b> Cannot initalize the object due to missing resources.", this);
                return;
            }

            InitializeSelector();
            UpdateContentLayout();

            if (invokeOnAwake && items.Count > 0)
            {
                items[index].onItemSelect.Invoke();
                onValueChanged.Invoke(index);
            }

            cachedStateLength = DreamOSInternalTools.GetAnimatorClipLength(selectorAnimator, "HorizontalSelector_Next");
        }

        void OnEnable()
        {
            if (gameObject.activeInHierarchy) { StartCoroutine(nameof(DisableAnimator)); }
            if (items.Count > 0 && useLocalization && !string.IsNullOrEmpty(items[index].localizationKey)
                && localizedObject != null && localizedObject.CheckLocalizationStatus())
            {
                label.text = localizedObject.GetKeyOutput(items[index].localizationKey);
            }
        }

        public void InitializeSelector()
        {
            if (items.Count == 0)
                return;

            if (saveSelected)
            {
                if (DreamOSDataManager.ContainsJsonKey(dataCategory, saveKey)) { defaultIndex = DreamOSDataManager.ReadIntData(dataCategory, saveKey); }
                else { DreamOSDataManager.WriteIntData(dataCategory, saveKey, defaultIndex); }
            }

            label.text = items[defaultIndex].itemTitle;
            labelHelper.text = label.text;

            if (enableIcon)
            {
                if (labelIcon != null) { labelIcon.gameObject.SetActive(true); labelIcon.sprite = items[defaultIndex].itemIcon; }
                if (labelIconHelper != null) { labelIconHelper.gameObject.SetActive(true); labelIconHelper.sprite = items[defaultIndex].itemIcon; }
            }
            else
            {
                if (labelIcon != null) { labelIcon.gameObject.SetActive(false); }
                if (labelIconHelper != null) { labelIconHelper.gameObject.SetActive(false); }
            }

            index = defaultIndex;

            if (enableIndicator) { UpdateIndicators(); }
            else { Destroy(indicatorParent.gameObject); }

            if (useLocalization)
            {
                localizedObject = gameObject.GetComponent<LocalizedObject>();

                if (localizedObject == null || !localizedObject.CheckLocalizationStatus()) { useLocalization = false; }
                else { localizedObject.onLanguageChanged.AddListener(delegate { UpdateUI(); }); }
            }
        }

        public void PreviousItem() { Step(false); }

        public void NextItem() { Step(true); }

        /// <summary>
        /// Moves one item forward or back: the old text slides out through the helper label while the new one slides
        /// in. Without looping, pressing past either end only refreshes the indicators.
        /// </summary>
        void Step(bool forward)
        {
            if (items.Count == 0) { return; }
            StopCoroutine(nameof(DisableAnimator));

            selectorAnimator.enabled = true;

            bool atEnd = forward ? index == items.Count - 1 : index == 0;
            if (loopSelection || !atEnd)
            {
                // The helper label keeps the outgoing text for the animation
                labelHelper.text = label.text;
                if (labelIcon != null && enableIcon) { labelIconHelper.sprite = labelIcon.sprite; }

                if (forward) { index = index + 1 >= items.Count ? 0 : index + 1; }
                else { index = index == 0 ? items.Count - 1 : index - 1; }

                label.text = GetItemText(index);
                if (labelIcon != null && enableIcon) { labelIcon.sprite = items[index].itemIcon; }

                DesktopSfx.Play(DesktopSound.Select);
                items[index].onItemSelect.Invoke();
                onValueChanged.Invoke(index);

                // Restart playback from the idle state, then slide
                selectorAnimator.Play(null);
                selectorAnimator.StopPlayback();
                selectorAnimator.Play(forward != invertAnimation ? "Next" : "Prev");
            }

            if (saveSelected) { DreamOSDataManager.WriteIntData(dataCategory, saveKey, index); }
            if (enableIndicator)
            {
                for (int i = 0; i < items.Count; ++i) { SetIndicatorState(indicatorParent.GetChild(i).gameObject, i == index); }
            }

            if (gameObject.activeInHierarchy) { StartCoroutine(nameof(DisableAnimator)); }
        }

        public void UpdateUI()
        {
            selectorAnimator.enabled = true;

            label.text = GetItemText(index);

            if (labelIcon != null && enableIcon)
            {
                labelIcon.gameObject.SetActive(true);
                labelIconHelper.gameObject.SetActive(true);
                labelIcon.sprite = items[index].itemIcon;
            }
            else if (!enableIcon && labelIcon != null)
            {
                labelIcon.gameObject.SetActive(false);
                labelIconHelper.gameObject.SetActive(false);
            }

            UpdateContentLayout();
            UpdateIndicators();

            if (gameObject.activeInHierarchy) { StartCoroutine(nameof(DisableAnimator)); }
        }

        public void UpdateEditorUI()
        {
            if (labelIcon != null && enableIcon) { labelIcon.sprite = items[index].itemIcon; }
            else if (!enableIcon && labelIcon != null)
            {
                labelIcon.gameObject.SetActive(false);
                labelIconHelper.gameObject.SetActive(false);
            }
        }

        public void UpdateIndicators()
        {
            if (!enableIndicator)
                return;

            foreach (Transform child in indicatorParent) { Destroy(child.gameObject); }
            for (int i = 0; i < items.Count; ++i)
            {
                GameObject dot = Instantiate(indicatorObject, new Vector3(0, 0, 0), Quaternion.identity);
                dot.transform.SetParent(indicatorParent, false);
                dot.name = items[i].itemTitle;
                SetIndicatorState(dot, i == index);
            }
        }

        public void UpdateContentLayout()
        {
            if (contentLayout != null) { contentLayout.spacing = contentSpacing; }
            if (contentLayoutHelper != null) { contentLayoutHelper.spacing = contentSpacing; }
            if (labelIcon != null)
            {
                labelIcon.transform.localScale = new Vector3(iconScale, iconScale, iconScale);
                labelIconHelper.transform.localScale = new Vector3(iconScale, iconScale, iconScale);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(label.transform.parent.GetComponent<RectTransform>());
        }

        public void CreateNewItem(string title)
        {
            Item item = new Item();
            item.itemTitle = title;
            items.Add(item);
        }

        public void RemoveItem(string itemTitle)
        {
            var item = items.Find(x => x.itemTitle == itemTitle);
            items.Remove(item);
            InitializeSelector();
        }

        public void AddNewItem()
        {
            Item item = new Item();
            items.Add(item);
        }

        string GetItemText(int itemIndex)
        {
            Item item = items[itemIndex];
            if (useLocalization && !string.IsNullOrEmpty(item.localizationKey)) { return localizedObject.GetKeyOutput(item.localizationKey); }
            return item.itemTitle;
        }

        static void SetIndicatorState(GameObject indicator, bool active)
        {
            indicator.transform.Find("On").gameObject.SetActive(active);
            indicator.transform.Find("Off").gameObject.SetActive(!active);
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(cachedStateLength + 0.1f);
            selectorAnimator.enabled = false;
        }
    }
}
