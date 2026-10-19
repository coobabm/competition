using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The flat list button used in side panels and settings menus (icon + label). Four stacked canvas groups
    /// (normal, highlight, pressed, selected) cross-fade by hand on hover, press and selection, and the button can be
    /// kept "selected" like a tab. Replaces DreamOS's PanelButton (2026-10-19): same GUID, serialized fields and
    /// public API.
    ///
    /// Fades run on unscaled time so panels stay responsive while the game is paused; the state coroutines are
    /// started and stopped by name so a new state always cancels the other three.
    /// </summary>
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

            if (useUINavigation) { AddUINavigation(); }
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }

            // Buttons authored without all four states get empty groups so the fades below never hit null
            if (normalCG == null) { normalCG = CreateStateGroup("Normal"); }
            if (highlightCG == null) { highlightCG = CreateStateGroup("Highlight"); }
            if (pressCG == null) { pressCG = CreateStateGroup("Pressed"); isPressedCGEmpty = true; }
            if (selectCG == null) { selectCG = CreateStateGroup("Selected"); }

            normalCG.alpha = 1;
            highlightCG.alpha = 0;
            pressCG.alpha = 0;
            selectCG.alpha = 0;

            if (useLocalization)
            {
                localizedObject = gameObject.GetComponent<LocalizedObject>();

                if (localizedObject == null || !localizedObject.CheckLocalizationStatus()) { useLocalization = false; }
                else if (!string.IsNullOrEmpty(localizedObject.localizationKey))
                {
                    // Take the localized output right away, then follow language changes
                    buttonText = localizedObject.GetKeyOutput(localizedObject.localizationKey);

                    localizedObject.onLanguageChanged.AddListener(delegate
                    {
                        buttonText = localizedObject.GetKeyOutput(localizedObject.localizationKey);
                        UpdateUI();
                    });
                }
            }

            isInitialized = true;
        }

        CanvasGroup CreateStateGroup(string stateName)
        {
            CanvasGroup group = new GameObject().AddComponent<CanvasGroup>();
            group.gameObject.AddComponent<RectTransform>();
            group.transform.SetParent(transform);
            group.gameObject.name = stateName;
            return group;
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
            else if (navigationMode == Navigation.Mode.Explicit) { StartCoroutine(nameof(InitUINavigation), customNav); return; }

            targetButton.navigation = customNav;
        }

        public void DisableUINavigation()
        {
            if (targetButton != null)
            {
                Navigation customNav = new Navigation();
                customNav.mode = Navigation.Mode.None;
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

            ApplyIcon(normalImageObj);
            ApplyIcon(highlightImageObj);
            ApplyIcon(pressedImageObj);
            ApplyIcon(selectedImageObj);

            if (isSelected)
            {
                normalCG.alpha = 0;
                highlightCG.alpha = 0;
                if (pressCG != null) { pressCG.alpha = 0; }
                selectCG.alpha = 1;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

        // Each state's icon sits in its own parent object, which is hidden when the button has no icon
        void ApplyIcon(Image stateImage)
        {
            if (stateImage == null) { return; }

            stateImage.transform.parent.gameObject.SetActive(buttonIcon != null);
            if (buttonIcon != null) { stateImage.sprite = buttonIcon; }
        }

        public void SetSelected(bool value)
        {
            isSelected = value;

            if (isSelected) { StartCoroutine(nameof(SetSelect)); }
            else { StartCoroutine(nameof(SetNormal)); }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable || isSelected || isPressedCGEmpty || pressCG == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            StartCoroutine(nameof(SetPressed));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isInteractable) { return; }
            if (!isPressedCGEmpty && !isSelected) { StartCoroutine(nameof(SetNormal)); }
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
            StartCoroutine(nameof(SetHighlight));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable || isSelected)
                return;

            StartCoroutine(nameof(SetNormal));
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable || isSelected)
                return;

            StartCoroutine(nameof(SetHighlight));
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!isInteractable || isSelected)
                return;

            StartCoroutine(nameof(SetNormal));
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!isInteractable || isSelected)
                return;

            onClick.Invoke();
        }

        IEnumerator InitUINavigation(Navigation nav)
        {
            yield return new WaitForSecondsRealtime(0.1f);

            if (selectOnUp != null) { nav.selectOnUp = selectOnUp.GetComponent<Selectable>(); }
            if (selectOnDown != null) { nav.selectOnDown = selectOnDown.GetComponent<Selectable>(); }
            if (selectOnLeft != null) { nav.selectOnLeft = selectOnLeft.GetComponent<Selectable>(); }
            if (selectOnRight != null) { nav.selectOnRight = selectOnRight.GetComponent<Selectable>(); }

            targetButton.navigation = nav;
        }

        // One state fades in while the other three fade out; starting a state cancels the others' fades
        IEnumerator SetNormal()
        {
            StopOtherStateFades(nameof(SetNormal));

            if (fadingMultiplier == 0) { normalCG.alpha = 1; }
            else
            {
                while (normalCG.alpha < 0.99f)
                {
                    StepFade(normalCG);
                    yield return null;
                }
            }

            SnapTo(normalCG);
        }

        IEnumerator SetHighlight()
        {
            StopOtherStateFades(nameof(SetHighlight));

            if (fadingMultiplier == 0) { highlightCG.alpha = 1; }
            else
            {
                while (highlightCG.alpha < 0.99f)
                {
                    StepFade(highlightCG);
                    yield return null;
                }
            }

            SnapTo(highlightCG);
        }

        IEnumerator SetPressed()
        {
            StopOtherStateFades(nameof(SetPressed));

            if (fadingMultiplier == 0) { pressCG.alpha = 1; }
            else
            {
                while (pressCG.alpha < 0.99f)
                {
                    StepFade(pressCG);
                    yield return null;
                }
            }

            SnapTo(pressCG);
        }

        IEnumerator SetSelect()
        {
            StopOtherStateFades(nameof(SetSelect));

            if (fadingMultiplier == 0) { selectCG.alpha = 1; }
            else
            {
                while (selectCG.alpha < 0.99f)
                {
                    StepFade(selectCG);
                    yield return null;
                }
            }

            SnapTo(selectCG);
        }

        void StopOtherStateFades(string current)
        {
            if (current != nameof(SetNormal)) { StopCoroutine(nameof(SetNormal)); }
            if (current != nameof(SetHighlight)) { StopCoroutine(nameof(SetHighlight)); }
            if (current != nameof(SetPressed)) { StopCoroutine(nameof(SetPressed)); }
            if (current != nameof(SetSelect)) { StopCoroutine(nameof(SetSelect)); }
        }

        // One frame of the cross-fade: the target gains alpha, every other state loses it
        void StepFade(CanvasGroup target)
        {
            float step = Time.unscaledDeltaTime * fadingMultiplier;
            normalCG.alpha += normalCG == target ? step : -step;
            highlightCG.alpha += highlightCG == target ? step : -step;
            if (pressCG != null) { pressCG.alpha += pressCG == target ? step : -step; }
            selectCG.alpha += selectCG == target ? step : -step;
        }

        void SnapTo(CanvasGroup target)
        {
            normalCG.alpha = normalCG == target ? 1 : 0;
            highlightCG.alpha = highlightCG == target ? 1 : 0;
            if (pressCG != null) { pressCG.alpha = pressCG == target ? 1 : 0; }
            selectCG.alpha = selectCG == target ? 1 : 0;
        }
    }
}
