using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// One desktop window: open / close / minimize / fullscreen animation, taskbar button, optional side navbar
    /// that remembers whether it was expanded, and the corner resize handle (2026-10-19). Replaces DreamOS's
    /// WindowManager: same GUID, serialized fields and public API, so the app windows in Main.unity and the
    /// 70-odd scripts that open and close windows bind unchanged.
    ///
    /// Window lifecycle quirks that the story and desktop code rely on are kept as they were: the window
    /// registers itself in <see cref="DesktopWindowIndex"/> in Awake and OnEnable, deactivates itself on Start
    /// (unless opened first), and closing is a state flag plus an "Out" animation that deactivates later.
    /// </summary>
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
        public enum ResizeAnchor { Disabled, BottomLeft, BottomRight }

        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Apps;

        // Window rect (offsets from the parent) remembered while fullscreen, so Normalize can restore it
        float left;
        float right;
        float top;
        float bottom;
        float cachedStateLength = 1;
        bool isNavbarOpen = true;
        UIBlur windowBGBlur;
        RectTransform windowRect;
        WindowResizeAnchor cachedResizeAnchor;
        bool sfxMinimized;
        [HideInInspector] public bool isOn;
        [HideInInspector] public bool isNormalized;
        [HideInInspector] public bool isFullscreen;

        string NavDrawerKey { get { return gameObject.name + "_NavDrawer"; } }

        void Awake()
        {
            DesktopWindowIndex.Register(this);
            if (windowAnimator == null) { windowAnimator = gameObject.GetComponent<Animator>(); }

            cachedStateLength = DreamOSInternalTools.GetAnimatorClipLength(windowAnimator, "Window_In") + 0.1f;
            InitializeWindow();
        }

        void Start()
        {
            // A first explicit open can run before Start on an initially inactive window.
            if (disableAtStart && !isOn) { gameObject.SetActive(false); }
        }

        void OnEnable()
        {
            DesktopWindowIndex.Register(this);
            if (enableMobileMode) { return; }
            if (!hasNavDrawer) { return; }

            if (navbarRect == null || windowContent == null)
            {
                Debug.LogError("<b>[Window Manager]</b> Navbar is enabled but its resources are missing!");
                return;
            }

            // The saved navbar state wins over the authored default; the first run stores the default.
            if (!DreamOSDataManager.ContainsJsonKey(dataCat, NavDrawerKey))
            {
                DreamOSDataManager.WriteBooleanData(dataCat, NavDrawerKey, defaultNavbarState == DefaultState.Expanded);
            }
            else if (DreamOSDataManager.ReadBooleanData(dataCat, NavDrawerKey))
            {
                defaultNavbarState = DefaultState.Expanded;
                isNavbarOpen = true;
            }
            else
            {
                defaultNavbarState = DefaultState.Minimized;
                isNavbarOpen = false;
            }

            float width = defaultNavbarState == DefaultState.Expanded ? maxNavbarWidth : minNavbarWidth;
            navbarRect.sizeDelta = new Vector2(width, navbarRect.sizeDelta.y);
            windowContent.offsetMin = new Vector2(width, windowContent.offsetMin.y);
        }

        void OnDestroy() { DesktopWindowIndex.Unregister(this); }

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
            if (enableMobileMode) { return; }

            if (taskbarButton != null)
            {
                taskbarButton.windowManager = this;
                taskbarButton.InitializeButton();
            }

            if (useBackgroundBlur) { windowBGBlur = gameObject.GetComponent<UIBlur>(); }
            if (windowDragger != null) { windowDragger.wManager = this; }

            windowRect = gameObject.GetComponent<RectTransform>();
            left = windowRect.offsetMin.x;
            right = -windowRect.offsetMax.x;
            top = -windowRect.offsetMax.y;
            bottom = windowRect.offsetMin.y;

            SetFullscreenIcons(false);
            InitializeResizePreset();
        }

        public void AnimateNavbar()
        {
            if (navbarRect == null || windowContent == null) { return; }

            StopCoroutine(nameof(DoNavbarExpand));
            StopCoroutine(nameof(DoNavbarMinimize));
            StopCoroutine(nameof(DoContentExpand));
            StopCoroutine(nameof(DoContentMinimize));

            if (isNavbarOpen)
            {
                StartCoroutine(nameof(DoNavbarMinimize));
                StartCoroutine(nameof(DoContentMinimize));

                DreamOSDataManager.WriteBooleanData(dataCat, NavDrawerKey, false);
                defaultNavbarState = DefaultState.Minimized;
                isNavbarOpen = false;
            }
            else
            {
                StartCoroutine(nameof(DoNavbarExpand));
                StartCoroutine(nameof(DoContentExpand));

                DreamOSDataManager.WriteBooleanData(dataCat, NavDrawerKey, true);
                defaultNavbarState = DefaultState.Expanded;
                isNavbarOpen = true;
            }
        }

        public void OpenWindow()
        {
            // Sound only when the window actually appears (not when focusing one already on screen)
            if (!isOn || sfxMinimized || !gameObject.activeInHierarchy) { DesktopSfx.Play(DesktopSound.WindowOpen); }
            sfxMinimized = false;

            FocusToWindow();

            isOn = true;
            gameObject.SetActive(true);
            onOpen.Invoke();

            if (enableMobileMode) { return; }

            windowAnimator.enabled = true;

            // A same-frame Minimize may still report the previous In state. The default -Infinity
            // time can preserve that state instead of replacing the pending Minimize request.
            // Explicitly start the authored entry animation; keep the fullscreen/normalize guard.
            AnimatorStateInfo state = windowAnimator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Fullscreen") && !state.IsName("Normalize")) { windowAnimator.Play("In", 0, 0f); }
            if (taskbarButton != null) { taskbarButton.SetOpen(); }
            if (windowBGBlur != null) { windowBGBlur.BlurInAnim(); }

            StopCoroutine(nameof(DisableAnimator));
            StartCoroutine(nameof(DisableAnimator), false);
        }

        public void CloseWindow()
        {
            if (isOn && gameObject.activeInHierarchy) { DesktopSfx.Play(DesktopSound.WindowClose); }
            sfxMinimized = false;

            isOn = false;
            onClose.Invoke();

            if (enableMobileMode) { return; }
            if (useBackgroundBlur && windowBGBlur != null) { windowBGBlur.BlurOutAnim(); }
            if (taskbarButton != null) { taskbarButton.SetClosed(); }
            if (!gameObject.activeInHierarchy) { return; }

            windowAnimator.enabled = true;
            windowAnimator.Play("Out");

            StopCoroutine(nameof(DisableAnimator));
            StartCoroutine(nameof(DisableAnimator), true);
        }

        public void MinimizeWindow()
        {
            if (!sfxMinimized) { DesktopSfx.Play(DesktopSound.WindowMinimize); }
            sfxMinimized = true;

            onMinimize.Invoke();
            windowAnimator.enabled = true;
            windowAnimator.Play("Minimize");

            if (taskbarButton != null) { taskbarButton.SetMinimized(); }
            if (useBackgroundBlur && windowBGBlur != null) { windowBGBlur.BlurOutAnim(); }

            StopCoroutine(nameof(DisableAnimator));
            StartCoroutine(nameof(DisableAnimator), false);
        }

        public void FullscreenWindow()
        {
            DesktopSfx.Play(DesktopSound.WindowMaximize);
            onFullscreen.Invoke();
            windowAnimator.enabled = true;

            if (!isFullscreen)
            {
                isFullscreen = true;
                isNormalized = false;
                SetFullscreenIcons(true);
                StartCoroutine(SetFullscreen());
            }
            else
            {
                isFullscreen = false;
                isNormalized = true;
                SetFullscreenIcons(false);
                StartCoroutine(SetNormalized());
            }

            StopCoroutine(nameof(DisableAnimator));
            StartCoroutine(nameof(DisableAnimator), false);
        }

        public void InitializeResizePreset()
        {
            if (resizeAnchor == ResizeAnchor.Disabled || resizePreset == null || windowContainer == null) { return; }

            if (cachedResizeAnchor == null)
            {
                GameObject go = Instantiate(resizePreset, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                go.transform.SetParent(windowContainer, false);

                cachedResizeAnchor = go.GetComponent<WindowResizeAnchor>();
                cachedResizeAnchor.targetRect = windowContainer;
            }

            cachedResizeAnchor.SetMinSize(minWindowSize);
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

        /// <summary>Changes a rect's pivot without moving it on screen.</summary>
        public static void SetPivot(RectTransform rectTransform, Vector2 pivot)
        {
            var offset = pivot - rectTransform.pivot;
            offset.Scale(rectTransform.rect.size);
            var wordlPos = rectTransform.position + rectTransform.TransformVector(offset);
            rectTransform.pivot = pivot;
            rectTransform.position = wordlPos;
        }

        // The "maximize" glyph shows while normal-sized, the "restore" glyph while fullscreen.
        void SetFullscreenIcons(bool fullscreen)
        {
            if (fullscreenImage == null || normalizeImage == null) { return; }

            fullscreenImage.SetActive(!fullscreen);
            normalizeImage.SetActive(fullscreen);
        }

        // The four navbar coroutines keep their names (they are started and stopped by name) and their exact
        // loop conditions: the minimize loops test >= / > against the target, so they only end if the
        // lerp lands just under it, as before.
        IEnumerator DoNavbarExpand() { return ResizeNavbar(maxNavbarWidth, true); }
        IEnumerator DoNavbarMinimize() { return ResizeNavbar(minNavbarWidth, false); }
        IEnumerator DoContentExpand() { return ResizeContent(maxNavbarWidth, true); }
        IEnumerator DoContentMinimize() { return ResizeContent(minNavbarWidth, false); }

        IEnumerator ResizeNavbar(float targetWidth, bool expanding)
        {
            float elapsedTime = 0;
            Vector2 startPos = navbarRect.sizeDelta;
            Vector2 endPos = new Vector2(targetWidth, navbarRect.sizeDelta.y);

            if (navbarCurveSpeed == 0) { navbarRect.sizeDelta = endPos; }
            else
            {
                while (expanding ? navbarRect.sizeDelta.x <= endPos.x - 0.1f : navbarRect.sizeDelta.x >= endPos.x)
                {
                    elapsedTime += Time.deltaTime;
                    navbarRect.sizeDelta = Vector2.Lerp(startPos, endPos, navbarCurve.Evaluate(elapsedTime * navbarCurveSpeed));
                    yield return null;
                }
            }

            navbarRect.sizeDelta = endPos;
        }

        IEnumerator ResizeContent(float targetOffset, bool expanding)
        {
            float elapsedTime = 0;
            Vector2 startPos = windowContent.offsetMin;
            Vector2 endPos = new Vector2(targetOffset, windowContent.offsetMin.y);

            if (navbarCurveSpeed == 0) { windowContent.offsetMin = endPos; }
            else
            {
                while (expanding ? windowContent.offsetMin.x < endPos.x - 0.1f : windowContent.offsetMin.x > endPos.x)
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

            windowContainer.offsetMin = new Vector2(0, 0);
            windowContainer.offsetMax = new Vector2(0, 0);

            isFullscreen = true;
            isNormalized = false;

            if (cachedResizeAnchor != null) { cachedResizeAnchor.gameObject.SetActive(false); }

            yield return null;
        }

        IEnumerator SetNormalized()
        {
            windowAnimator.Play("Normalize");

            windowContainer.offsetMin = new Vector2(left, bottom);
            windowContainer.offsetMax = new Vector2(-right, -top);

            isFullscreen = false;
            isNormalized = true;

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
