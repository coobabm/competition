using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The desktop's standard button (2026-10-19). Replaces DreamOS's ButtonManager: same GUID, serialized fields and
    /// public API, so every authored button, popup, taskbar entry and app page keeps binding unchanged.
    ///
    /// A button is four stacked state layers (normal / highlight / pressed / disabled), each a CanvasGroup holding its
    /// own label and icon; hover, press and disable fade between the layers at <see cref="fadingMultiplier"/> per second
    /// on unscaled time. Missing layers are created on first use. Also: auto-fit padding and spacing, optional click
    /// ripple and hover glow, double-click detection, UI navigation (automatic, directional or explicit targets) and
    /// localized labels.
    ///
    /// <see cref="UpdateUI"/> is public because label bindings refresh the button from outside, possibly before this
    /// component's own OnEnable (it initializes first) and possibly while the object is inactive (it then only writes
    /// content and leaves coroutines and initialization for the activation).
    /// </summary>
    [ExecuteInEditMode]
    public class ButtonManager : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        // Content
        public Sprite buttonIcon;
        public string buttonText = "Button";
        [Range(0.1f, 10)] public float iconScale = 1;
        [Range(1, 200)] public float textSize = 24;
        public AudioClip hoverSound;
        public AudioClip clickSound;

        // Resources
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private CanvasGroup normalCG;
        [SerializeField] private CanvasGroup highlightCG;
        [SerializeField] private CanvasGroup pressedCG;
        [SerializeField] private CanvasGroup disabledCG;
        public TextMeshProUGUI normalTextObj;
        public TextMeshProUGUI highlightTextObj;
        public TextMeshProUGUI pressedTextObj;
        public TextMeshProUGUI disabledTextObj;
        public Image normalImageObj;
        public Image highlightImageObj;
        public Image pressedImageObj;
        public Image disabledImageObj;
        public GameObject rippleParent;
        public HoverEffect hoverEffect;

        // Auto Size
        public bool autoFitContent = true;
        public Padding padding;
        [Range(0, 100)] public int spacing = 12;
        [SerializeField] private HorizontalLayoutGroup disabledLayout;
        [SerializeField] private HorizontalLayoutGroup normalLayout;
        [SerializeField] private HorizontalLayoutGroup highlightedLayout;
        [SerializeField] private HorizontalLayoutGroup pressedLayout;
        public HorizontalLayoutGroup mainLayout;
        [SerializeField] private ContentSizeFitter mainFitter;
        [SerializeField] private ContentSizeFitter targetFitter;
        [SerializeField] private RectTransform targetRect;

        // Settings
        public bool isInteractable = true;
        public bool enableIcon = false;
        public bool enableText = true;
        public bool useCustomContent = false;
        [SerializeField] private bool useCustomTextSize = false;
        public bool checkForDoubleClick = true;
        public bool useLocalization = true;
        public bool bypassUpdateOnEnable = false;
        public bool useUINavigation = false;
        public Navigation.Mode navigationMode = Navigation.Mode.Automatic;
        public GameObject selectOnUp;
        public GameObject selectOnDown;
        public GameObject selectOnLeft;
        public GameObject selectOnRight;
        public bool wrapAround = false;
        public bool useSounds = false;
        public bool useRipple = true;
        public bool useHoverEffect = false;
        [Range(0.1f, 1)] public float doubleClickPeriod = 0.25f;
        [Range(0, 15)] public float fadingMultiplier = 8;

        // Ripple
        public RippleUpdateMode rippleUpdateMode = RippleUpdateMode.UnscaledTime;
        public Sprite rippleShape;
        [Range(0.1f, 8)] public float speed = 1f;
        [Range(0.5f, 25)] public float maxSize = 4f;
        public Color startColor = new Color(1f, 1f, 1f, 1f);
        public Color transitionColor = new Color(1f, 1f, 1f, 1f);
        public bool renderOnTop = false;
        public bool centered = false;

        // Hover Effect
        [Range(1f, 25)] public float heSpeed = 10f;
        [Range(0.1f, 25)] public float heSize = 2f;
        public Sprite heShape;
        [Range(0, 1)] public float heTransitionAlpha = 0.1f;

        // Events
        public UnityEvent onClick = new UnityEvent();
        public UnityEvent onDoubleClick = new UnityEvent();
        public UnityEvent onHover = new UnityEvent();
        public UnityEvent onLeave = new UnityEvent();
        public UnityEvent onSelect = new UnityEvent();
        public UnityEvent onDeselect = new UnityEvent();

        // Helpers (isInitialized is read by the startup regression tests through reflection)
        bool isInitialized;
        bool isPointerOn;
        bool isPressedCGEmpty;
        Button targetButton;
        LocalizedObject localizedObject;
        bool waitingForDoubleClickInput;
#if UNITY_EDITOR
        public int latestTabIndex = 0;
#endif

        public enum RippleUpdateMode { Normal, UnscaledTime }

        [System.Serializable]
        public class Padding
        {
            public int left = 18;
            public int right = 18;
            public int top = 15;
            public int bottom = 15;
        }

        void Start()
        {
            if (targetCanvas == null) { targetCanvas = GetComponentInParent<Canvas>(); }
        }

        void OnEnable()
        {
            if (!isInitialized) { Initialize(); }
            if (!bypassUpdateOnEnable) { UpdateUI(); }
        }

        void OnDisable()
        {
            if (!isInteractable) { return; }

            if (disabledCG != null) { disabledCG.alpha = 0; }
            if (normalCG != null) { normalCG.alpha = 1; }
            if (highlightCG != null) { highlightCG.alpha = 0; }
            if (pressedCG != null) { pressedCG.alpha = 0; }
        }

        void Initialize()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { return; }
#endif

            // The root needs a raycast target to receive pointer events.
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }

            if (useUINavigation) { AddUINavigation(); }

            // Authored buttons bring their own state layers; fill in any that are missing.
            if (normalCG == null) { normalCG = CreateStateGroup("Normal"); }
            if (highlightCG == null) { highlightCG = CreateStateGroup("Highlight"); }
            if (pressedCG == null) { pressedCG = CreateStateGroup("Pressed"); isPressedCGEmpty = true; }
            if (disabledCG == null) { disabledCG = CreateStateGroup("Disabled"); }

            normalCG.alpha = 1;
            highlightCG.alpha = 0;
            pressedCG.alpha = 0;
            disabledCG.alpha = 0;

            if (useLocalization && !useCustomContent)
            {
                localizedObject = gameObject.GetComponent<LocalizedObject>();

                if (localizedObject == null || !localizedObject.CheckLocalizationStatus()) { useLocalization = false; }
                else if (!string.IsNullOrEmpty(localizedObject.localizationKey))
                {
                    // Take the localized text right away, then follow language changes.
                    buttonText = localizedObject.GetKeyOutput(localizedObject.localizationKey);
                    localizedObject.onLanguageChanged.AddListener(delegate
                    {
                        buttonText = localizedObject.GetKeyOutput(localizedObject.localizationKey);
                        UpdateUI();
                    });
                }
            }

            maxSize = Mathf.Clamp(maxSize, 0.5f, 1000f);
            if (rippleParent != null)
            {
                if (useRipple) { rippleParent.SetActive(false); }
                else { Destroy(rippleParent); }
            }

            if (useHoverEffect && hoverEffect != null) { hoverEffect.gameObject.SetActive(false); }

            isInitialized = true;
        }

        CanvasGroup CreateStateGroup(string groupName)
        {
            GameObject groupObject = new GameObject();
            CanvasGroup group = groupObject.AddComponent<CanvasGroup>();
            groupObject.AddComponent<RectTransform>();
            groupObject.transform.SetParent(transform);
            groupObject.name = groupName;
            return group;
        }

        public void UpdateUI()
        {
            // Authored-label bindings may refresh before this component's OnEnable.
            if (!isInitialized && gameObject.activeInHierarchy) { Initialize(); }

            ApplyLayoutMode();
            ApplyText();
            ApplyIcon();

#if UNITY_EDITOR
            if (!Application.isPlaying && autoFitContent)
            {
                RebuildLayout(this);
                RebuildLayout(disabledCG);
                RebuildLayout(normalCG);
                RebuildLayout(highlightCG);
                RebuildLayout(pressedCG);
            }
#endif

            if (!Application.isPlaying || !gameObject.activeInHierarchy) { return; }
            if (!isInteractable) { StartCoroutine(nameof(SetDisabled)); }
            else if (disabledCG.alpha == 1) { StartCoroutine(nameof(SetNormal)); }

            StartCoroutine(nameof(LayoutFix));
        }

        void ApplyLayoutMode()
        {
            if (!autoFitContent)
            {
                if (mainFitter != null) { mainFitter.enabled = false; }
                if (mainLayout != null) { mainLayout.enabled = false; }
                SetChildForceExpandWidth(false);

                if (targetFitter != null)
                {
                    targetFitter.enabled = false;

                    if (targetRect != null)
                    {
                        targetRect.anchorMin = new Vector2(0, 0);
                        targetRect.anchorMax = new Vector2(1, 1);
                        targetRect.offsetMin = new Vector2(0, 0);
                        targetRect.offsetMax = new Vector2(0, 0);
                    }
                }

                return;
            }

            SetChildForceExpandWidth(true);
            if (mainFitter != null) { mainFitter.enabled = true; }
            if (mainLayout != null) { mainLayout.enabled = true; }
            if (targetFitter != null) { targetFitter.enabled = true; }

            ApplyPadding(disabledLayout);
            ApplyPadding(normalLayout);
            ApplyPadding(highlightedLayout);
            ApplyPadding(pressedLayout);
        }

        void SetChildForceExpandWidth(bool value)
        {
            if (disabledLayout != null) { disabledLayout.childForceExpandWidth = value; }
            if (normalLayout != null) { normalLayout.childForceExpandWidth = value; }
            if (highlightedLayout != null) { highlightedLayout.childForceExpandWidth = value; }
            if (pressedLayout != null) { pressedLayout.childForceExpandWidth = value; }
        }

        void ApplyPadding(HorizontalLayoutGroup layout)
        {
            if (layout == null) { return; }

            layout.padding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom);
            layout.spacing = spacing;
        }

        void ApplyText()
        {
            if (enableText && !useCustomContent)
            {
                ShowLabel(normalTextObj);
                ShowLabel(highlightTextObj);
                ShowLabel(pressedTextObj);
                ShowLabel(disabledTextObj);
            }
            else if (!enableText)
            {
                HideLabel(normalTextObj);
                HideLabel(highlightTextObj);
                HideLabel(pressedTextObj);
                HideLabel(disabledTextObj);
            }
        }

        void ShowLabel(TextMeshProUGUI label)
        {
            if (label == null) { return; }

            label.gameObject.SetActive(true);
            label.text = buttonText;
            if (!useCustomTextSize) { label.fontSize = textSize; }
        }

        static void HideLabel(TextMeshProUGUI label)
        {
            if (label != null) { label.gameObject.SetActive(false); }
        }

        void ApplyIcon()
        {
            if (enableIcon && buttonIcon != null && !useCustomContent)
            {
                Vector3 scale = new Vector3(iconScale, iconScale, iconScale);
                ShowIcon(normalImageObj, scale);
                ShowIcon(highlightImageObj, scale);
                ShowIcon(pressedImageObj, scale);
                ShowIcon(disabledImageObj, scale);
            }
            else if (!enableIcon || buttonIcon == null)
            {
                HideIcon(normalImageObj);
                HideIcon(highlightImageObj);
                HideIcon(pressedImageObj);
                HideIcon(disabledImageObj);
            }
        }

        // The icon image sits in a holder object; the holder is what gets toggled.
        void ShowIcon(Image icon, Vector3 scale)
        {
            if (icon == null) { return; }

            icon.transform.parent.gameObject.SetActive(true);
            icon.sprite = buttonIcon;
            icon.transform.localScale = scale;
        }

        static void HideIcon(Image icon)
        {
            if (icon != null) { icon.transform.parent.gameObject.SetActive(false); }
        }

        static void RebuildLayout(Component target)
        {
            if (target != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(target.GetComponent<RectTransform>()); }
        }

        public void UpdateState()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy) { return; }
            if (!isInteractable) { StartCoroutine(nameof(SetDisabled)); }
            else { StartCoroutine(nameof(SetNormal)); }
        }

        public void SetText(string text) { buttonText = text; UpdateUI(); }

        public void SetIcon(Sprite icon)
        {
            if (icon != null) { buttonIcon = icon; }
            else { enableIcon = false; }

            UpdateUI();
        }

        public void Interactable(bool value)
        {
            isInteractable = value;

            if (!gameObject.activeInHierarchy) { return; }
            if (!isInteractable) { StartCoroutine(nameof(SetDisabled)); }
            else if (disabledCG.alpha == 1) { StartCoroutine(nameof(SetNormal)); }
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
            else if (navigationMode == Navigation.Mode.Explicit)
            {
                // The explicit targets may not be ready yet; InitUINavigation fills them in shortly.
                StartCoroutine(nameof(InitUINavigation), customNav);
                return;
            }

            targetButton.navigation = customNav;
        }

        public void DisableUINavigation()
        {
            if (targetButton == null) { return; }

            Navigation customNav = new Navigation();
            customNav.mode = Navigation.Mode.None;
            targetButton.navigation = customNav;
        }

        public void InvokeOnClick() { onClick.Invoke(); }

        public void CreateRipple(Vector2 pos)
        {
            if (rippleParent == null) { return; }

            GameObject rippleObj = new GameObject();
            Image rippleImage = rippleObj.AddComponent<Image>();
            rippleImage.raycastTarget = false;
            rippleImage.sprite = rippleShape;
            rippleObj.name = "Ripple";
            rippleParent.SetActive(true);
            rippleObj.transform.SetParent(rippleParent.transform);

            if (renderOnTop) { rippleParent.transform.SetAsLastSibling(); }

            if (centered) { rippleObj.transform.localPosition = new Vector2(0f, 0f); }
            else { rippleObj.transform.position = pos; }

            Ripple ripple = rippleObj.AddComponent<Ripple>();
            ripple.speed = speed;
            ripple.maxSize = maxSize;
            ripple.startColor = startColor;
            ripple.transitionColor = transitionColor;
            ripple.unscaledTime = rippleUpdateMode != RippleUpdateMode.Normal;
        }

        void PlayUISound(bool click)
        {
            if (!useSounds || AudioManager.instance == null) { return; }

            var uiAsset = AudioManager.instance.UIManagerAsset;
            AudioManager.instance.audioSource.PlayOneShot(click ? uiAsset.clickSound : uiAsset.hoverSound);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) { return; }
            PlayUISound(true);

            // Back to the resting layer the pointer is actually on.
            if (gameObject.activeInHierarchy)
            {
                if (!isPointerOn) { StartCoroutine(nameof(SetNormal)); }
                else { StartCoroutine(nameof(SetHighlight)); }
            }

            onClick.Invoke();

            if (!checkForDoubleClick) { return; }
            if (waitingForDoubleClickInput)
            {
                onDoubleClick.Invoke();
                waitingForDoubleClickInput = false;
                return;
            }

            waitingForDoubleClickInput = true;

            if (gameObject.activeInHierarchy)
            {
                StopCoroutine(nameof(CheckForDoubleClick));
                StartCoroutine(nameof(CheckForDoubleClick));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) { return; }
            if (!isPressedCGEmpty) { StartCoroutine(nameof(SetPressed)); }
            if (!useRipple || !isPointerOn) { return; }

            Vector2 mouse = Mouse.current.position.ReadValue();
            if (targetCanvas != null && (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera || targetCanvas.renderMode == RenderMode.WorldSpace))
            {
                CreateRipple(targetCanvas.worldCamera.ScreenToWorldPoint(mouse));
            }
            else { CreateRipple(mouse); }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isInteractable) { return; }
            if (!isPressedCGEmpty && !isPointerOn) { StartCoroutine(nameof(SetNormal)); }
            else if (isPointerOn) { StartCoroutine(nameof(SetHighlight)); }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) { return; }
            PlayUISound(false);

            isPointerOn = true;

            StartCoroutine(nameof(SetHighlight));
            onHover.Invoke();

            if (useHoverEffect && hoverEffect != null)
            {
                hoverEffect.speed = heSpeed;
                hoverEffect.targetImage.sprite = heShape;
                hoverEffect.targetImage.transform.localScale = new Vector3(heSize, heSize, heSize);
                hoverEffect.transitionAlpha = heTransitionAlpha;
                hoverEffect.StartFadeIn();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable) { return; }
            if (useHoverEffect && hoverEffect != null) { hoverEffect.StartFadeOut(); }

            isPointerOn = false;
            StartCoroutine(nameof(SetNormal));
            onLeave.Invoke();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable) { return; }
            PlayUISound(false);

            StartCoroutine(nameof(SetHighlight));
            onSelect.Invoke();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!isInteractable) { return; }

            StartCoroutine(nameof(SetNormal));
            onDeselect.Invoke();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!isInteractable) { return; }
            PlayUISound(true);
            if (EventSystem.current.currentSelectedGameObject != gameObject) { StartCoroutine(nameof(SetNormal)); }

            onClick.Invoke();
        }

        IEnumerator LayoutFix()
        {
            yield return new WaitForSecondsRealtime(0.025f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

            if (transform.parent != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>()); }
            RebuildLayout(disabledCG);
            RebuildLayout(normalCG);
            RebuildLayout(highlightCG);
        }

        // State fades. Each one cancels the fades that would fight it (disable leaves a pending press alone, as shipped),
        // then moves every layer toward its end state until the target layer is fully shown.
        IEnumerator SetNormal()
        {
            StopCoroutine(nameof(SetHighlight));
            StopCoroutine(nameof(SetPressed));
            StopCoroutine(nameof(SetDisabled));

            while (FadeStep(normalCG)) { yield return null; }
            ApplyFinalState(normalCG);
        }

        IEnumerator SetHighlight()
        {
            StopCoroutine(nameof(SetNormal));
            StopCoroutine(nameof(SetPressed));
            StopCoroutine(nameof(SetDisabled));

            while (FadeStep(highlightCG)) { yield return null; }
            ApplyFinalState(highlightCG);
        }

        IEnumerator SetPressed()
        {
            StopCoroutine(nameof(SetNormal));
            StopCoroutine(nameof(SetHighlight));
            StopCoroutine(nameof(SetDisabled));

            while (FadeStep(pressedCG)) { yield return null; }
            ApplyFinalState(pressedCG);
        }

        IEnumerator SetDisabled()
        {
            StopCoroutine(nameof(SetNormal));
            StopCoroutine(nameof(SetHighlight));

            while (FadeStep(disabledCG)) { yield return null; }
            ApplyFinalState(disabledCG);
        }

        /// <summary>One frame of a state fade; false once the target is (nearly) shown or fading is off.</summary>
        bool FadeStep(CanvasGroup target)
        {
            if (fadingMultiplier == 0 || target.alpha >= 0.99f) { return false; }

            float step = Time.unscaledDeltaTime * fadingMultiplier;
            normalCG.alpha += normalCG == target ? step : -step;
            highlightCG.alpha += highlightCG == target ? step : -step;
            disabledCG.alpha += disabledCG == target ? step : -step;
            if (pressedCG != null) { pressedCG.alpha += pressedCG == target ? step : -step; }
            return true;
        }

        void ApplyFinalState(CanvasGroup shown)
        {
            normalCG.alpha = normalCG == shown ? 1 : 0;
            highlightCG.alpha = highlightCG == shown ? 1 : 0;
            disabledCG.alpha = disabledCG == shown ? 1 : 0;
            if (pressedCG != null) { pressedCG.alpha = pressedCG == shown ? 1 : 0; }
        }

        IEnumerator CheckForDoubleClick()
        {
            yield return new WaitForSecondsRealtime(doubleClickPeriod);
            waitingForDoubleClickInput = false;
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
    }
}
