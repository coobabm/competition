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

        // Helpers
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

        [System.Serializable] public class Padding 
        {
            public int left = 18;
            public int right = 18; 
            public int top = 15; 
            public int bottom = 15; 
        }

        void Start()
        {
            if (targetCanvas == null) 
            { 
                targetCanvas = GetComponentInParent<Canvas>(); 
            }
        }

        void OnEnable()
        {
            if (!isInitialized) { Initialize(); }
            if (!bypassUpdateOnEnable) { UpdateUI(); }
        }

        void OnDisable()
        {
            if (!isInteractable)
                return;

            if (disabledCG != null) { disabledCG.alpha = 0; }
            if (normalCG != null) { normalCG.alpha = 1; }
            if (highlightCG != null) { highlightCG.alpha = 0; }
            if (pressedCG != null) { pressedCG.alpha = 0; }
        }

        void Initialize()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            // Check if raycasting is available
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }

            // Check for UI navigation
            if (useUINavigation) { AddUINavigation(); }

            // Add required CG components
            if (normalCG == null) { normalCG = new GameObject().AddComponent<CanvasGroup>(); normalCG.gameObject.AddComponent<RectTransform>(); normalCG.transform.SetParent(transform); normalCG.gameObject.name = "Normal"; }
            if (highlightCG == null) { highlightCG = new GameObject().AddComponent<CanvasGroup>(); highlightCG.gameObject.AddComponent<RectTransform>(); highlightCG.transform.SetParent(transform); highlightCG.gameObject.name = "Highlight"; }
            if (pressedCG == null) { pressedCG = new GameObject().AddComponent<CanvasGroup>(); pressedCG.gameObject.AddComponent<RectTransform>(); pressedCG.transform.SetParent(transform); pressedCG.gameObject.name = "Pressed"; isPressedCGEmpty = true; }
            if (disabledCG == null) { disabledCG = new GameObject().AddComponent<CanvasGroup>(); disabledCG.gameObject.AddComponent<RectTransform>(); disabledCG.transform.SetParent(transform); disabledCG.gameObject.name = "Disabled"; }

            // Set default CG states
            normalCG.alpha = 1;
            highlightCG.alpha = 0;
            pressedCG.alpha = 0;
            disabledCG.alpha = 0;

            // Localization calls
            if (useLocalization && !useCustomContent)
            {
                localizedObject = gameObject.GetComponent<LocalizedObject>();

                if (localizedObject == null || !localizedObject.CheckLocalizationStatus()) { useLocalization = false; }
                else if (localizedObject != null && !string.IsNullOrEmpty(localizedObject.localizationKey))
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

            // Ripple calls
            maxSize = Mathf.Clamp(maxSize, 0.5f, 1000f);
            if (useRipple && rippleParent != null) { rippleParent.SetActive(false); }
            else if (!useRipple && rippleParent != null) { Destroy(rippleParent); }

            // Hover calls
            if (useHoverEffect && hoverEffect != null) { hoverEffect.gameObject.SetActive(false); }

            // Finalize the button
            isInitialized = true;
        }

        public void UpdateUI()
        {
            if (!autoFitContent)
            {
                if (mainFitter != null) { mainFitter.enabled = false; }
                if (mainLayout != null) { mainLayout.enabled = false; }
                if (disabledLayout != null) { disabledLayout.childForceExpandWidth = false; }
                if (normalLayout != null) { normalLayout.childForceExpandWidth = false; }
                if (highlightedLayout != null) { highlightedLayout.childForceExpandWidth = false; }
                if (pressedLayout != null) { pressedLayout.childForceExpandWidth = false; }
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
            }

            else
            {
                if (disabledLayout != null) { disabledLayout.childForceExpandWidth = true; }
                if (normalLayout != null) { normalLayout.childForceExpandWidth = true; }
                if (highlightedLayout != null) { highlightedLayout.childForceExpandWidth = true; }
                if (pressedLayout != null) { pressedLayout.childForceExpandWidth = true; }
                if (mainFitter != null) { mainFitter.enabled = true; }
                if (mainLayout != null) { mainLayout.enabled = true; }
                if (targetFitter != null) { targetFitter.enabled = true; }
            }

            if (disabledLayout != null && autoFitContent) { disabledLayout.padding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom); disabledLayout.spacing = spacing; }
            if (normalLayout != null && autoFitContent) { normalLayout.padding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom); normalLayout.spacing = spacing; }
            if (highlightedLayout != null && autoFitContent) { highlightedLayout.padding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom); highlightedLayout.spacing = spacing; }
            if (pressedLayout != null && autoFitContent) { pressedLayout.padding = new RectOffset(padding.left, padding.right, padding.top, padding.bottom); pressedLayout.spacing = spacing; }

            if (enableText && !useCustomContent)
            {
                if (normalTextObj != null)
                {
                    normalTextObj.gameObject.SetActive(true);
                    normalTextObj.text = buttonText;
                    if (!useCustomTextSize) { normalTextObj.fontSize = textSize; }
                }

                if (highlightTextObj != null)
                {
                    highlightTextObj.gameObject.SetActive(true);
                    highlightTextObj.text = buttonText;
                    if (!useCustomTextSize) { highlightTextObj.fontSize = textSize; }
                }

                if (pressedTextObj != null)
                {
                    pressedTextObj.gameObject.SetActive(true);
                    pressedTextObj.text = buttonText;
                    if (!useCustomTextSize) { pressedTextObj.fontSize = textSize; }
                }

                if (disabledTextObj != null)
                {
                    disabledTextObj.gameObject.SetActive(true);
                    disabledTextObj.text = buttonText;
                    if (!useCustomTextSize) { disabledTextObj.fontSize = textSize; }
                }
            }

            else if (!enableText)
            {
                if (normalTextObj != null) { normalTextObj.gameObject.SetActive(false); }
                if (highlightTextObj != null) { highlightTextObj.gameObject.SetActive(false); }
                if (pressedTextObj != null) { pressedTextObj.gameObject.SetActive(false); }
                if (disabledTextObj != null) { disabledTextObj.gameObject.SetActive(false); }
            }

            if (enableIcon && buttonIcon != null && !useCustomContent)
            {
                Vector3 tempScale = new Vector3(iconScale, iconScale, iconScale);

                if (normalImageObj != null) { normalImageObj.transform.parent.gameObject.SetActive(true); normalImageObj.sprite = buttonIcon; normalImageObj.transform.localScale = tempScale; }
                if (highlightImageObj != null) { highlightImageObj.transform.parent.gameObject.SetActive(true); highlightImageObj.sprite = buttonIcon; highlightImageObj.transform.localScale = tempScale; }
                if (pressedImageObj != null) { pressedImageObj.transform.parent.gameObject.SetActive(true); pressedImageObj.sprite = buttonIcon; pressedImageObj.transform.localScale = tempScale; }
                if (disabledImageObj != null) { disabledImageObj.transform.parent.gameObject.SetActive(true); disabledImageObj.sprite = buttonIcon; disabledImageObj.transform.localScale = tempScale; }
            }

            else if (!enableIcon || buttonIcon == null)
            {
                if (normalImageObj != null) { normalImageObj.transform.parent.gameObject.SetActive(false); }
                if (highlightImageObj != null) { highlightImageObj.transform.parent.gameObject.SetActive(false); }
                if (pressedImageObj != null) { pressedImageObj.transform.parent.gameObject.SetActive(false); }
                if (disabledImageObj != null) { disabledImageObj.transform.parent.gameObject.SetActive(false); }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying && autoFitContent)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                if (disabledCG != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(disabledCG.GetComponent<RectTransform>()); }
                if (normalCG != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(normalCG.GetComponent<RectTransform>()); }
                if (highlightCG != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(highlightCG.GetComponent<RectTransform>()); }
                if (pressedCG != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(pressedCG.GetComponent<RectTransform>()); }
            }
#endif

            if (!Application.isPlaying || !gameObject.activeInHierarchy) { return; }
            if (!isInteractable) { StartCoroutine("SetDisabled"); }
            else if (isInteractable && disabledCG.alpha == 1) { StartCoroutine("SetNormal"); }

            StartCoroutine("LayoutFix");
        }

        public void UpdateState()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy) { return; }
            if (!isInteractable) { StartCoroutine("SetDisabled"); }
            else if (isInteractable) { StartCoroutine("SetNormal"); }
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
            if (!isInteractable) { StartCoroutine("SetDisabled"); }
            else if (isInteractable && disabledCG.alpha == 1) { StartCoroutine("SetNormal"); }
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

        public void InvokeOnClick() { onClick.Invoke(); }

        public void CreateRipple(Vector2 pos)
        {
            // If Ripple Parent is assigned, create the object and get the necessary components
            if (rippleParent != null)
            {
                GameObject rippleObj = new GameObject();
                Image trImage = rippleObj.AddComponent<Image>();
                trImage.raycastTarget = false;
                trImage.sprite = rippleShape;
                rippleObj.name = "Ripple";
                rippleParent.SetActive(true);
                rippleObj.transform.SetParent(rippleParent.transform);

                if (renderOnTop) { rippleParent.transform.SetAsLastSibling(); }

                if (centered) { rippleObj.transform.localPosition = new Vector2(0f, 0f); }
                else { rippleObj.transform.position = pos; }

                rippleObj.AddComponent<Ripple>();
                Ripple tempRipple = rippleObj.GetComponent<Ripple>();
                tempRipple.speed = speed;
                tempRipple.maxSize = maxSize;
                tempRipple.startColor = startColor;
                tempRipple.transitionColor = transitionColor;

                if (rippleUpdateMode == RippleUpdateMode.Normal) { tempRipple.unscaledTime = false; }
                else { tempRipple.unscaledTime = true; }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) { return; }
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.clickSound); }

            // Set default state
            if (gameObject.activeInHierarchy) 
            {
                if (!isPointerOn) { StartCoroutine("SetNormal"); }
                else { StartCoroutine("SetHighlight"); }
            }

            // Invoke click actions
            onClick.Invoke();

            // Check for double click
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
                StopCoroutine("CheckForDoubleClick");
                StartCoroutine("CheckForDoubleClick");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) { return; }
            if (!isPressedCGEmpty) { StartCoroutine("SetPressed"); }
            if (useRipple && isPointerOn) 
            {
                if (targetCanvas != null && (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera || targetCanvas.renderMode == RenderMode.WorldSpace)) { CreateRipple(targetCanvas.worldCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue())); }
                else { CreateRipple(Mouse.current.position.ReadValue()); }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isInteractable) { return; }
            if (!isPressedCGEmpty && !isPointerOn) { StartCoroutine("SetNormal"); }
            else if (isPointerOn) { StartCoroutine("SetHighlight"); }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) { return; }
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.hoverSound); }

            isPointerOn = true;

            StartCoroutine("SetHighlight");
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
            StartCoroutine("SetNormal");
            onLeave.Invoke();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable) { return; }
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.hoverSound); }

            StartCoroutine("SetHighlight");
            onSelect.Invoke();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine("SetNormal");
            onDeselect.Invoke();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!isInteractable) { return; }
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.clickSound); }
            if (EventSystem.current.currentSelectedGameObject != gameObject) { StartCoroutine("SetNormal"); }

            onClick.Invoke();
        }

        IEnumerator LayoutFix()
        {
            yield return new WaitForSecondsRealtime(0.025f);
          
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
         
            if (transform.parent != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>()); }
            if (disabledCG != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(disabledCG.GetComponent<RectTransform>()); }
            if (normalCG != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(normalCG.GetComponent<RectTransform>()); }
            if (highlightCG != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(highlightCG.GetComponent<RectTransform>()); }
        }

        IEnumerator SetNormal()
        {
            StopCoroutine("SetHighlight");
            StopCoroutine("SetPressed");
            StopCoroutine("SetDisabled");

            if (fadingMultiplier == 0) { normalCG.alpha = 1; }
            else
            {
                while (normalCG.alpha < 0.99f)
                {
                    normalCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;
                    highlightCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    disabledCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    if (pressedCG != null) { pressedCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier; }
                    yield return null;
                }
            }

            normalCG.alpha = 1;
            highlightCG.alpha = 0;
            disabledCG.alpha = 0;
            if (pressedCG != null) { pressedCG.alpha = 0; }
        }

        IEnumerator SetHighlight()
        {
            StopCoroutine("SetNormal");
            StopCoroutine("SetPressed");
            StopCoroutine("SetDisabled");

            if (fadingMultiplier == 0) { highlightCG.alpha = 1; }
            else
            {
                while (highlightCG.alpha < 0.99f)
                {
                    normalCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    highlightCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;      
                    disabledCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    if (pressedCG != null) { pressedCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier; }
                    yield return null;
                }
            }

            normalCG.alpha = 0;
            highlightCG.alpha = 1;
            disabledCG.alpha = 0;
            if (pressedCG != null) { pressedCG.alpha = 0; }
        }

        IEnumerator SetPressed()
        {
            StopCoroutine("SetNormal");
            StopCoroutine("SetHighlight");
            StopCoroutine("SetDisabled");

            if (fadingMultiplier == 0) { pressedCG.alpha = 1; }
            else
            {
                while (pressedCG.alpha < 0.99f)
                {
                    normalCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    highlightCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    disabledCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    if (pressedCG != null) { pressedCG.alpha += Time.unscaledDeltaTime * fadingMultiplier; }
                    yield return null;
                }
            }

            normalCG.alpha = 0;
            highlightCG.alpha = 0;
            disabledCG.alpha = 0;
            if (pressedCG != null) { pressedCG.alpha = 1; }
        }

        IEnumerator SetDisabled()
        {
            StopCoroutine("SetNormal");
            StopCoroutine("SetHighlight");

            if (fadingMultiplier == 0) { disabledCG.alpha = 1; }
            else
            {
                while (disabledCG.alpha < 0.99f)
                {
                    normalCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    highlightCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    disabledCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;
                    if (pressedCG != null) { pressedCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier; }
                    yield return null;
                }
            }

            normalCG.alpha = 0;
            highlightCG.alpha = 0;
            disabledCG.alpha = 1;
            if (pressedCG != null) { pressedCG.alpha = 0; }
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