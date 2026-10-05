using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Michsky.DreamOS
{
    [RequireComponent(typeof(CanvasGroup))]
    public class WindowManager : MonoBehaviour, IPointerClickHandler
    {
        // Resources
        public Animator windowAnimator;
        public RectTransform windowContainer;
        public RectTransform windowContent;
        public RectTransform navbarRect;
        public TaskbarButton taskbarButton;
        public WindowDragger windowDragger;
        [SerializeField] private GameObject resizePreset;

        // Fullscreen & minimize
        public GameObject fullscreenImage;
        public GameObject normalizeImage;

        // Settings
        [SerializeField] private bool disableAtStart = true;
        [SerializeField] private bool waitForAnimationCompletion;
        [SerializeField] private bool enableMobileMode = false;
        public bool allowGestures = true;
        public bool useBackgroundBlur = true;
        [SerializeField] private bool hasNavDrawer = true;
        public float minNavbarWidth = 75;
        public float maxNavbarWidth = 300;
        [Range(0, 10)] public float navbarCurveSpeed = 4f;
        [SerializeField] private AnimationCurve navbarCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
        public DefaultState defaultNavbarState = DefaultState.Minimized;
        public ResizeAnchor resizeAnchor = ResizeAnchor.BottomRight;
        [Range(25, 100)] public int minWindowSize = 50;

        // Events
        public UnityEvent onOpen = new UnityEvent();
        public UnityEvent onClose = new UnityEvent();
        public UnityEvent onMinimize = new UnityEvent();
        public UnityEvent onFullscreen = new UnityEvent();

        public enum DefaultState { Minimized, Expanded }

        // Helpers
        float left;
        float right;
        float top;
        float bottom;
        float cachedStateLength = 1;
        bool isNavbarOpen = true;
        UIBlur windowBGBlur;
        RectTransform windowRect;
        WindowResizeAnchor cachedResizeAnchor;
        [HideInInspector] public bool isOn;
        [HideInInspector] public bool isNormalized;
        [HideInInspector] public bool isFullscreen;
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Apps;

        public enum ResizeAnchor { Disabled, BottomLeft, BottomRight }

        void Awake()
        {
            // Get the animator
            if (windowAnimator == null) { windowAnimator = gameObject.GetComponent<Animator>(); }

            // Get the animator state length
            cachedStateLength = DreamOSInternalTools.GetAnimatorClipLength(windowAnimator, "Window_In") + 0.1f;

            // Initialize the window
            InitializeWindow();
        }

        void Start()
        {
            // A first explicit open can run before Start on an initially inactive window.
            if (disableAtStart && !isOn) { gameObject.SetActive(false); }
        }

        void OnEnable()
        {
            if (enableMobileMode)
                return;

            if (hasNavDrawer)
            {
                if (navbarRect == null || windowContent == null)
                {
                    Debug.LogError("<b>[Window Manager]</b> Navbar is enabled but its resources are missing!");
                    return;
                }

                if (!DreamOSDataManager.ContainsJsonKey(dataCat, gameObject.name + "_NavDrawer"))
                {
                    if (defaultNavbarState == DefaultState.Expanded) { DreamOSDataManager.WriteBooleanData(dataCat, gameObject.name + "_NavDrawer", true); }
                    else { DreamOSDataManager.WriteBooleanData(dataCat, gameObject.name + "_NavDrawer", false); }
                }

                else if (DreamOSDataManager.ReadBooleanData(dataCat, gameObject.name + "_NavDrawer"))
                {
                    defaultNavbarState = DefaultState.Expanded;
                    isNavbarOpen = true;
                }

                else if (!DreamOSDataManager.ReadBooleanData(dataCat, gameObject.name + "_NavDrawer"))
                {
                    defaultNavbarState = DefaultState.Minimized;
                    isNavbarOpen = false;
                }

                if (defaultNavbarState == DefaultState.Minimized)
                {
                    navbarRect.sizeDelta = new Vector2(minNavbarWidth, navbarRect.sizeDelta.y);
                    windowContent.offsetMin = new Vector2(minNavbarWidth, windowContent.offsetMin.y);
                }

                else if (defaultNavbarState == DefaultState.Expanded)
                {
                    navbarRect.sizeDelta = new Vector2(maxNavbarWidth, navbarRect.sizeDelta.y);
                    windowContent.offsetMin = new Vector2(maxNavbarWidth, windowContent.offsetMin.y);
                }
            }
        }

        void OnDisable()
        {
            if (gameObject.activeInHierarchy || isOn)
            {
                CloseWindow();
                gameObject.SetActive(false);
            }
        }

        public void InitializeWindow()
        {
            // Don't process further events if mobile mode is enabled
            if (enableMobileMode) { return; }

            // Assign taskar variables
            if (taskbarButton != null) 
            { 
                taskbarButton.windowManager = this; 
                taskbarButton.InitializeButton();
            }

            // Assign blur variables
            if (useBackgroundBlur) { windowBGBlur = gameObject.GetComponent<UIBlur>(); }

            // Assign window dragger variables
            if (windowDragger != null) { windowDragger.wManager = this; }

            // Get RectTransform and set offset values
            windowRect = gameObject.GetComponent<RectTransform>();
            left = windowRect.offsetMin.x;
            right = -windowRect.offsetMax.x;
            top = -windowRect.offsetMax.y;
            bottom = windowRect.offsetMin.y;

            // Change fullscreen image state
            if (fullscreenImage != null && normalizeImage != null)
            {
                fullscreenImage.SetActive(true);
                normalizeImage.SetActive(false);
            }

            // Initialize Resize Preset
            InitializeResizePreset();
        }

        public void AnimateNavbar()
        {
            if (navbarRect == null || windowContent == null)
                return;

            StopCoroutine("DoNavbarExpand");
            StopCoroutine("DoNavbarMinimize");
            StopCoroutine("DoContentExpand");
            StopCoroutine("DoContentMinimize");

            if (isNavbarOpen)
            {
                StartCoroutine("DoNavbarMinimize");
                StartCoroutine("DoContentMinimize");

                DreamOSDataManager.WriteBooleanData(dataCat, gameObject.name + "_NavDrawer", false);
                defaultNavbarState = DefaultState.Minimized;
                isNavbarOpen = false;
            }

            else
            {
                StartCoroutine("DoNavbarExpand");
                StartCoroutine("DoContentExpand");

                DreamOSDataManager.WriteBooleanData(dataCat, gameObject.name + "_NavDrawer", true);
                defaultNavbarState = DefaultState.Expanded;
                isNavbarOpen = true;
            }
        }

        public void OpenWindow()
        {
            FocusToWindow();

            isOn = true;
            gameObject.SetActive(true);
            onOpen.Invoke();

            if (enableMobileMode)
                return;

            windowAnimator.enabled = true;

            if (!windowAnimator.GetCurrentAnimatorStateInfo(0).IsName("Fullscreen") && (!windowAnimator.GetCurrentAnimatorStateInfo(0).IsName("Normalize"))) { windowAnimator.Play("In"); }
            if (taskbarButton != null && !enableMobileMode) { taskbarButton.SetOpen(); }
            if (windowBGBlur != null) { windowBGBlur.BlurInAnim(); }

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator", false);
        }

        public void CloseWindow()
        {
            isOn = false;
            onClose.Invoke();

            if (enableMobileMode) { return; }
            if (useBackgroundBlur && windowBGBlur != null) { windowBGBlur.BlurOutAnim(); }
            if (taskbarButton != null) { taskbarButton.SetClosed(); }
            if (!gameObject.activeInHierarchy) { return; }

            windowAnimator.enabled = true;
            windowAnimator.Play("Out");

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator", true);
        }

        public void MinimizeWindow()
        {
            onMinimize.Invoke();
            windowAnimator.enabled = true;
            windowAnimator.Play("Minimize");

            if (taskbarButton != null) { taskbarButton.SetMinimized(); }
            if (useBackgroundBlur && windowBGBlur != null) { windowBGBlur.BlurOutAnim(); }

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator", false);
        }

        public void FullscreenWindow()
        {
            onFullscreen.Invoke();
            windowAnimator.enabled = true;

            if (!isFullscreen)
            {
                isFullscreen = true;
                isNormalized = false;

                if (fullscreenImage != null && normalizeImage != null)
                {
                    fullscreenImage.SetActive(false);
                    normalizeImage.SetActive(true);
                }

                StartCoroutine("SetFullscreen");
            }

            else
            {
                isFullscreen = false;
                isNormalized = true;

                if (fullscreenImage != null && normalizeImage != null)
                {
                    fullscreenImage.SetActive(true);
                    normalizeImage.SetActive(false);
                }

                StartCoroutine("SetNormalized");
            }

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator", false);
        }

        public void InitializeResizePreset()
        {
            // Don't initialize the preset if it's disabled
            if (resizeAnchor == ResizeAnchor.Disabled || resizePreset == null || windowContainer == null)
                return;

            // Create the preset if it's null
            if (cachedResizeAnchor == null)
            {
                GameObject go = Instantiate(resizePreset, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                go.transform.SetParent(windowContainer, false);

                cachedResizeAnchor = go.GetComponent<WindowResizeAnchor>();
                cachedResizeAnchor.targetRect = windowContainer;
            }

            // Set the min size
            cachedResizeAnchor.SetMinSize(minWindowSize);

            // Set the anchor
            cachedResizeAnchor.SetAnchor(resizeAnchor);
        }

        public void FocusToWindow() 
        { 
            gameObject.transform.SetAsLastSibling(); 
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            FocusToWindow();
        }

        public static void SetPivot(RectTransform rectTransform, Vector2 pivot)
        {
            var offset = pivot - rectTransform.pivot;
            offset.Scale(rectTransform.rect.size);
            var wordlPos = rectTransform.position + rectTransform.TransformVector(offset);
            rectTransform.pivot = pivot;
            rectTransform.position = wordlPos;
        }

        IEnumerator DoNavbarExpand()
        {
            float elapsedTime = 0;
            Vector2 startPos = navbarRect.sizeDelta;
            Vector2 endPos = new Vector2(maxNavbarWidth, navbarRect.sizeDelta.y);

            if (navbarCurveSpeed == 0) { navbarRect.sizeDelta = endPos; }
            else
            {
                while (navbarRect.sizeDelta.x <= endPos.x - 0.1f)
                {
                    elapsedTime += Time.deltaTime;
                    navbarRect.sizeDelta = Vector2.Lerp(startPos, endPos, navbarCurve.Evaluate(elapsedTime * navbarCurveSpeed));
                    yield return null;
                }
            }

            navbarRect.sizeDelta = endPos;
        }

        IEnumerator DoNavbarMinimize()
        {
            float elapsedTime = 0;
            Vector2 startPos = new Vector2(navbarRect.sizeDelta.x, navbarRect.sizeDelta.y);
            Vector2 endPos = new Vector2(minNavbarWidth, navbarRect.sizeDelta.y);

            if (navbarCurveSpeed == 0) { navbarRect.sizeDelta = endPos; }
            else
            {
                while (navbarRect.sizeDelta.x >= endPos.x)
                {
                    elapsedTime += Time.deltaTime;
                    navbarRect.sizeDelta = Vector2.Lerp(startPos, endPos, navbarCurve.Evaluate(elapsedTime * navbarCurveSpeed));
                    yield return null;
                }
            }

            navbarRect.sizeDelta = endPos;
        }

        IEnumerator DoContentExpand()
        {
            float elapsedTime = 0;

            Vector2 startPos = windowContent.offsetMin;
            Vector2 endPos = new Vector2(maxNavbarWidth, windowContent.offsetMin.y);

            if (navbarCurveSpeed == 0) { windowContent.offsetMin = endPos; }
            else
            {
                while (windowContent.offsetMin.x < endPos.x - 0.1f)
                {
                    windowContent.offsetMin = Vector2.Lerp(startPos, endPos, navbarCurve.Evaluate(elapsedTime * navbarCurveSpeed));
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            windowContent.offsetMin = endPos;
        }

        IEnumerator DoContentMinimize()
        {
            float elapsedTime = 0;

            Vector2 startPos = windowContent.offsetMin;
            Vector2 endPos = new Vector2(minNavbarWidth, windowContent.offsetMin.y);

            if (navbarCurveSpeed == 0) { windowContent.offsetMin = endPos; }
            else
            {
                while (windowContent.offsetMin.x > endPos.x)
                {
                    windowContent.offsetMin = Vector2.Lerp(startPos, endPos, navbarCurve.Evaluate(elapsedTime * navbarCurveSpeed));
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            windowContent.offsetMin = endPos;
        }

        IEnumerator SetFullscreen()
        {
            left = windowContainer.offsetMin.x;
            right = -windowContainer.offsetMax.x;
            top = -windowContainer.offsetMax.y;
            bottom = windowContainer.offsetMin.y;

            windowAnimator.Play("Fullscreen");

            // Left and bottom
            windowContainer.offsetMin = new Vector2(0, 0);

            // Right and top
            windowContainer.offsetMax = new Vector2(0, 0);

            isFullscreen = true;
            isNormalized = false;

            // Check for resize object and disable it
            if (cachedResizeAnchor != null) { cachedResizeAnchor.gameObject.SetActive(false); }

            yield return null;
        }

        IEnumerator SetNormalized()
        {
            windowAnimator.Play("Normalize");

            // Left and bottom
            windowContainer.offsetMin = new Vector2(left, bottom);

            // Right and top
            windowContainer.offsetMax = new Vector2(-right, -top);

            isFullscreen = false;
            isNormalized = true;

            // Check for resize object and enable it
            if (cachedResizeAnchor != null) { cachedResizeAnchor.gameObject.SetActive(true); }

            yield return null;
        }

        IEnumerator DisableAnimator(bool disableObject)
        {
            yield return new WaitForSeconds(cachedStateLength);
            // Frame stalls must not freeze a hongmengos window halfway through
            // minimize/restore: the cached duration belongs to the In clip.
            if (waitForAnimationCompletion || (taskbarButton != null && taskbarButton.OpenWindowsOnly))
            {
                while (windowAnimator.isActiveAndEnabled &&
                    (windowAnimator.IsInTransition(0) || windowAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f))
                    yield return null;
            }
            windowAnimator.enabled = false;
            if (disableObject) { gameObject.SetActive(false); }
        }
    }
}