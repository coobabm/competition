using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace Michsky.DreamOS
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class PanelButton : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        // Content
        public Sprite buttonIcon;
        public string buttonText = "Button";

        // Resources
        [SerializeField] private CanvasGroup normalCG;
        [SerializeField] private CanvasGroup highlightCG;
        [SerializeField] private CanvasGroup pressCG;
        [SerializeField] private CanvasGroup selectCG;
        [SerializeField] private TextMeshProUGUI normalTextObj;
        [SerializeField] private TextMeshProUGUI highlightTextObj;
        [SerializeField] private TextMeshProUGUI pressedTextObj;
        [SerializeField] private TextMeshProUGUI selectedTextObj;
        [SerializeField] private Image normalImageObj;
        [SerializeField] private Image highlightImageObj;
        [SerializeField] private Image pressedImageObj;
        [SerializeField] private Image selectedImageObj;

        // Settings
        public bool isInteractable = true;
        public bool isSelected;
        public bool useLocalization = true;
        public bool useCustomText = false;
        public bool useUINavigation = false;
        public Navigation.Mode navigationMode = Navigation.Mode.Automatic;
        public GameObject selectOnUp;
        public GameObject selectOnDown;
        public GameObject selectOnLeft;
        public GameObject selectOnRight;
        public bool wrapAround = false;
        public bool useSounds = false;
        [Range(0, 15)] public float fadingMultiplier = 8;

        // Events
        public UnityEvent onClick = new UnityEvent();
        public UnityEvent onHover = new UnityEvent();

        // Helpers
        bool isInitialized;
        bool isPressedCGEmpty;
        Button targetButton;
        LocalizedObject localizedObject;

        void OnEnable()
        {
            if (!isInitialized) { Initialize(); }
            UpdateUI();
        }

        void Initialize()
        {
            if (!Application.isPlaying)
                return;

            if (useUINavigation == true) { AddUINavigation(); }
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }

            // Add required CG components
            if (normalCG == null) { normalCG = new GameObject().AddComponent<CanvasGroup>(); normalCG.gameObject.AddComponent<RectTransform>(); normalCG.transform.SetParent(transform); normalCG.gameObject.name = "Normal"; }
            if (highlightCG == null) { highlightCG = new GameObject().AddComponent<CanvasGroup>(); highlightCG.gameObject.AddComponent<RectTransform>(); highlightCG.transform.SetParent(transform); highlightCG.gameObject.name = "Highlight"; }
            if (pressCG == null) { pressCG = new GameObject().AddComponent<CanvasGroup>(); pressCG.gameObject.AddComponent<RectTransform>(); pressCG.transform.SetParent(transform); pressCG.gameObject.name = "Pressed"; isPressedCGEmpty = true; }
            if (selectCG == null) { selectCG = new GameObject().AddComponent<CanvasGroup>(); selectCG.gameObject.AddComponent<RectTransform>(); selectCG.transform.SetParent(transform); selectCG.gameObject.name = "Selected"; }

            normalCG.alpha = 1;
            highlightCG.alpha = 0;
            pressCG.alpha = 0;
            selectCG.alpha = 0;

            if (useLocalization)
            {
                localizedObject = gameObject.GetComponent<LocalizedObject>();

                if (localizedObject == null || !localizedObject.CheckLocalizationStatus()) { useLocalization = false; }
                else if (useLocalization && !string.IsNullOrEmpty(localizedObject.localizationKey))
                {
                    // Forcing button to take the localized output on awake
                    buttonText = localizedObject.GetKeyOutput(localizedObject.localizationKey);

                    // Change button text on language change
                    localizedObject.onLanguageChanged.AddListener(delegate
                    {
                        buttonText = localizedObject.GetKeyOutput(localizedObject.localizationKey);
                        UpdateUI();
                    });
                }
            }

            isInitialized = true;
        }

        public void AddUINavigation()
        {
            if (targetButton == null)
            {
                targetButton = gameObject.AddComponent<Button>();
                targetButton.transition = Selectable.Transition.None;
            }

            Navigation customNav = new Navigation();
            customNav.mode = navigationMode;

            if (navigationMode == Navigation.Mode.Vertical || navigationMode == Navigation.Mode.Horizontal) { customNav.wrapAround = wrapAround; }
            else if (navigationMode == Navigation.Mode.Explicit) { StartCoroutine("InitUINavigation", customNav); return; }

            targetButton.navigation = customNav;
        }

        public void DisableUINavigation()
        {
            if (targetButton != null)
            {
                Navigation customNav = new Navigation();
                Navigation.Mode navMode = Navigation.Mode.None;
                customNav.mode = navMode;
                targetButton.navigation = customNav;
            }
        }

        public void UpdateUI()
        {
            if (useCustomText)
                return;

            if (normalTextObj != null) { normalTextObj.text = buttonText; }
            if (highlightTextObj != null) { highlightTextObj.text = buttonText; }
            if (pressedTextObj != null) { pressedTextObj.text = buttonText; }
            if (selectedTextObj != null) { selectedTextObj.text = buttonText; }

            if (normalImageObj != null && buttonIcon != null) { normalImageObj.transform.parent.gameObject.SetActive(true); normalImageObj.sprite = buttonIcon; }
            else if (normalImageObj != null && buttonIcon == null) { normalImageObj.transform.parent.gameObject.SetActive(false); }

            if (highlightImageObj != null && buttonIcon != null) { highlightImageObj.transform.parent.gameObject.SetActive(true); highlightImageObj.sprite = buttonIcon; }
            else if (highlightImageObj != null && buttonIcon == null) { highlightImageObj.transform.parent.gameObject.SetActive(false); }

            if (pressedImageObj != null && buttonIcon != null) { pressedImageObj.transform.parent.gameObject.SetActive(true); pressedImageObj.sprite = buttonIcon; }
            else if (pressedImageObj != null && buttonIcon == null) { pressedImageObj.transform.parent.gameObject.SetActive(false); }

            if (selectedImageObj != null && buttonIcon != null) { selectedImageObj.transform.parent.gameObject.SetActive(true); selectedImageObj.sprite = buttonIcon; }
            else if (selectedImageObj != null && buttonIcon == null) { selectedImageObj.transform.parent.gameObject.SetActive(false); }

            if (isSelected)
            {
                normalCG.alpha = 0;
                highlightCG.alpha = 0;
                if (pressCG != null) { pressCG.alpha = 0; }
                selectCG.alpha = 1;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

        public void SetSelected(bool value)
        {
            isSelected = value;

            if (isSelected) { StartCoroutine("SetSelect"); }
            else { StartCoroutine("SetNormal"); }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable || isSelected || isPressedCGEmpty || pressCG == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            StartCoroutine("SetPressed");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isInteractable) { return; }
            if (!isPressedCGEmpty && !isSelected) { StartCoroutine("SetNormal"); }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) { return; }
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.clickSound); }

            onClick.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.hoverSound); }
            if (!isInteractable || isSelected) { return; }

            onHover.Invoke();
            StartCoroutine("SetHighlight");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable || isSelected)
                return;

            StartCoroutine("SetNormal");
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable || isSelected)
                return;

            StartCoroutine("SetHighlight");
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!isInteractable || isSelected)
                return;

            StartCoroutine("SetNormal");
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!isInteractable || isSelected)
                return;

            onClick.Invoke();
        }

        IEnumerator SetNormal()
        {
            StopCoroutine("SetHighlight");
            StopCoroutine("SetPressed");
            StopCoroutine("SetSelect");

            if (fadingMultiplier == 0) { normalCG.alpha = 1; }
            else
            {
                while (normalCG.alpha < 0.99f)
                {
                    normalCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;
                    highlightCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    if (pressCG != null) { pressCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier; }
                    selectCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    yield return null;
                }
            }

            normalCG.alpha = 1;
            highlightCG.alpha = 0;
            if (pressCG != null) { pressCG.alpha = 0; }
            selectCG.alpha = 0;
        }

        IEnumerator SetHighlight()
        {
            StopCoroutine("SetNormal");
            StopCoroutine("SetPressed");
            StopCoroutine("SetSelect");

            if (fadingMultiplier == 0) { highlightCG.alpha = 1; }
            else
            {
                while (highlightCG.alpha < 0.99f)
                {
                    normalCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    highlightCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;
                    if (pressCG != null) { pressCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier; }
                    selectCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    yield return null;
                }
            }

            normalCG.alpha = 0;
            highlightCG.alpha = 1;
            if (pressCG != null) { pressCG.alpha = 0; }
            selectCG.alpha = 0;
        }

        IEnumerator SetPressed()
        {
            StopCoroutine("SetNormal");
            StopCoroutine("SetHighlight");
            StopCoroutine("SetSelect");

            if (fadingMultiplier == 0) { pressCG.alpha = 1; }
            else
            {
                while (pressCG.alpha < 0.99f)
                {
                    normalCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    highlightCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    pressCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;
                    selectCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    yield return null;
                }
            }

            normalCG.alpha = 0;
            highlightCG.alpha = 0;
            pressCG.alpha = 1;
            selectCG.alpha = 0;
        }

        IEnumerator SetSelect()
        {
            StopCoroutine("SetNormal");
            StopCoroutine("SetHighlight");
            StopCoroutine("SetPressed");

            if (fadingMultiplier == 0) { selectCG.alpha = 1; }
            {
                while (selectCG.alpha < 0.99f)
                {
                    normalCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    highlightCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    if (pressCG != null) { pressCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier; }
                    selectCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;
                    yield return null;
                }
            }

            normalCG.alpha = 0;
            highlightCG.alpha = 0;
            if (pressCG != null) { pressCG.alpha = 0; }
            selectCG.alpha = 1;
        }
    }
}