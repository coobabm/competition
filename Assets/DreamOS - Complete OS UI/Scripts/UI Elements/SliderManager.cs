using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace Michsky.DreamOS
{
    [RequireComponent(typeof(Slider))]
    public class SliderManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        // Resources
        public Slider mainSlider;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private CanvasGroup highlightCG;

        // Saving
        public bool saveValue = false;
        public bool invokeOnAwake = true;
        public string saveKey = "My Slider";
        public DreamOSDataManager.DataCategory dataCategory = DreamOSDataManager.DataCategory.Apps;

        // Settings
        public bool isInteractable = true;
        public bool usePercent = false;
        public bool showValue = true;
        public bool showPopupValue = true;
        public bool useRoundValue = false;
        public bool useSounds = false;
        [Range(1, 15)] public float fadingMultiplier = 8;

        // Events
        [System.Serializable] public class SliderEvent : UnityEvent<float> { }
        [SerializeField] public SliderEvent onValueChanged = new SliderEvent();

        void Awake()
        {
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }

            if (highlightCG == null) { highlightCG = new GameObject().AddComponent<CanvasGroup>(); highlightCG.gameObject.AddComponent<RectTransform>(); highlightCG.transform.SetParent(transform); highlightCG.gameObject.name = "Highlight"; }
            if (mainSlider == null) { mainSlider = gameObject.GetComponent<Slider>(); }

            highlightCG.alpha = 0;
            highlightCG.gameObject.SetActive(false);
            float saveVal = mainSlider.value;

            if (saveValue)
            {
                if (DreamOSDataManager.ContainsJsonKey(dataCategory, saveKey)) { saveVal = DreamOSDataManager.ReadFloatData(dataCategory, saveKey); }
                else { DreamOSDataManager.WriteFloatData(dataCategory, saveKey, saveVal); }

                mainSlider.value = saveVal;
                mainSlider.onValueChanged.AddListener(delegate { DreamOSDataManager.WriteFloatData(dataCategory, saveKey, mainSlider.value); });
            }

            mainSlider.onValueChanged.AddListener(delegate
            {
                onValueChanged.Invoke(mainSlider.value);
                UpdateUI();
            });

            if (invokeOnAwake) { onValueChanged.Invoke(mainSlider.value); }
            UpdateUI();
        }

        public void Interactable(bool value)
        {
            isInteractable = value;
            mainSlider.interactable = isInteractable;
        }

        public void AddUINavigation()
        {
            Navigation customNav = new Navigation();
            customNav.mode = Navigation.Mode.Automatic;
            mainSlider.navigation = customNav;
        }

        public void UpdateUI()
        {
            if (valueText == null)
                return;

            if (useRoundValue)
            {
                if (usePercent && valueText != null) { valueText.text = Mathf.Round(mainSlider.value * 1.0f).ToString() + "%"; }
                else if (valueText != null) { valueText.text = Mathf.Round(mainSlider.value * 1.0f).ToString(); }
            }

            else
            {
                if (usePercent && valueText != null) { valueText.text = mainSlider.value.ToString("F1") + "%"; }
                else if (valueText != null) { valueText.text = mainSlider.value.ToString("F1"); }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.hoverSound); }
            if (!isInteractable) { return; }

            StartCoroutine("SetHighlight");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine("SetNormal");
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine("SetHighlight");
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine("SetNormal");
        }

        IEnumerator SetNormal()
        {
            StopCoroutine("SetHighlight");

            while (highlightCG.alpha > 0.01f)
            {
                highlightCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                yield return null;
            }

            highlightCG.alpha = 0;
            highlightCG.gameObject.SetActive(false);
        }

        IEnumerator SetHighlight()
        {
            StopCoroutine("SetNormal");
            highlightCG.gameObject.SetActive(true);

            while (highlightCG.alpha < 0.99f)
            {
                highlightCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;
                yield return null;
            }

            highlightCG.alpha = 1;
        }
    }
}