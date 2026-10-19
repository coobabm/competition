using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Plays the desktop's boot, reboot and shutdown sequences (2026-10-19). Replaces DreamOS's BootManager: same GUID,
    /// serialized fields and public API.
    ///
    /// <see cref="Boot"/> hides the setup, lock and desktop screens, shows the boot animation for
    /// <see cref="bootTime"/> seconds, then opens either the setup screen (no user yet) or the lock screen.
    /// <see cref="Reboot"/> and <see cref="Shutdown"/> fade a black overlay in over the canvas first; shutdown in the
    /// "Main" scene returns to the title screen instead.
    /// </summary>
    public class BootManager : MonoBehaviour
    {
        // Resources
        public Animator bootAnimator;
        [SerializeField] private UserManager userManager;
        [SerializeField] private Canvas targetCanvas;

        // Settings
        public bool bootOnEnable = true;
        [SerializeField] private bool fadeFrameSkip = false;
        [Range(0, 30)] public float bootTime = 4;
        [Range(0, 5)] public float initTime = 0.5f;
        [Range(0.5f, 12)] public float fadeSpeed = 1.5f;

        // Events
        public UnityEvent onBootStart = new UnityEvent();
        public UnityEvent onBootEnd = new UnityEvent();
        public UnityEvent onRebootStart = new UnityEvent();
        public UnityEvent onRebootEnd = new UnityEvent();

        float cachedOutLength = 0.5f;
        bool isRebootInProgress;
        bool isShutdownInProgress;
        Coroutine bootRoutine;

#if UNITY_EDITOR
        // Only the scene serialization needs this one now that the old inspector is gone.
        public int currentEditorTab;
#endif

        void Awake()
        {
            if (targetCanvas == null) { targetCanvas = GetComponentInParent<Canvas>(); }

            if (bootAnimator != null)
            {
                cachedOutLength = DreamOSInternalTools.GetAnimatorClipLength(bootAnimator, "BootScreen_Out") + 0.1f;
            }
        }

        void OnEnable()
        {
            if (bootOnEnable) { Boot(); }
        }

        void OnDisable()
        {
            if (bootAnimator != null) { bootAnimator.gameObject.SetActive(false); }
        }

        public void Boot()
        {
            if (bootAnimator.gameObject.activeInHierarchy) { return; }
            if (userManager == null) { Debug.LogError("<b>[Boot Manager]</b> User Manager is missing, but it's an essential variable for boot to work.", this); return; }

            userManager.setupScreen.gameObject.SetActive(false);
            userManager.lockScreen.gameObject.SetActive(true);
            userManager.desktopScreen.gameObject.SetActive(true);

            if (userManager.desktopScreen.gameObject.activeInHierarchy) { userManager.desktopScreen.Play("Instant Out"); }
            Invoke(nameof(BootHelper), initTime);

            onBootStart.Invoke();
        }

        void BootHelper()
        {
            if (!gameObject.activeInHierarchy) { return; }

            userManager.lockScreen.gameObject.SetActive(false);
            userManager.desktopScreen.gameObject.SetActive(false);
            bootAnimator.gameObject.SetActive(true);
            bootAnimator.enabled = true;
            bootAnimator.Play("Start");

            if (bootRoutine != null) { StopCoroutine(bootRoutine); }
            bootRoutine = StartCoroutine(StartBootProcess());
        }

        public void Reboot()
        {
            if (isRebootInProgress) { return; }

            UnityEvent whenCovered = new UnityEvent();
            whenCovered.AddListener(delegate
            {
                onRebootEnd.Invoke();
                isRebootInProgress = false;

                // Toggling the canvas restarts everything under it, which is what brings the boot back up.
                targetCanvas.gameObject.SetActive(false);
                targetCanvas.gameObject.SetActive(true);
            });

            FadeToBlack(whenCovered);
            isRebootInProgress = true;
            onRebootStart.Invoke();
        }

        public void Shutdown()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main")
            {
                GameExit.ReturnToTitle();
                return;
            }
            if (isShutdownInProgress) { return; }

            UnityEvent whenCovered = new UnityEvent();
            whenCovered.AddListener(delegate
            {
                isShutdownInProgress = false;
                targetCanvas.gameObject.SetActive(false);
            });

            FadeToBlack(whenCovered);
            isShutdownInProgress = true;
        }

        /// <summary>Stretches a transparent black image over the canvas, fades it in, then runs the callback and removes it.</summary>
        void FadeToBlack(UnityEvent whenCovered)
        {
            GameObject overlay = new GameObject();
            overlay.name = "Boot Fade Animation (Temp)";
            overlay.transform.SetParent(targetCanvas.transform, false);
            overlay.transform.SetAsLastSibling();

            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0);

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            ImageFading fading = overlay.AddComponent<ImageFading>();
            fading.frameSkip = fadeFrameSkip;
            fading.onFadeInEnd.AddListener(delegate
            {
                whenCovered.Invoke();
                Destroy(overlay);
            });
            fading.fadeSpeed = fadeSpeed;
            fading.FadeIn();
        }

        IEnumerator StartBootProcess()
        {
            yield return new WaitForSeconds(bootTime);

            if (bootTime != 0) { bootAnimator.Play("Out"); }
            else { bootAnimator.Play("Disabled"); }

            onBootEnd.Invoke();

            if (userManager.userCreated)
            {
                userManager.setupScreen.gameObject.SetActive(false);
                userManager.OpenLockScreen();
            }
            else
            {
                userManager.setupScreen.gameObject.SetActive(true);
            }

            StartCoroutine(HideBootScreen());
        }

        IEnumerator HideBootScreen()
        {
            yield return new WaitForSeconds(cachedOutLength);
            bootAnimator.gameObject.SetActive(false);
        }
    }
}
