using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Michsky.DreamOS
{
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

        // Helpers
        float cachedStartLength = 2;
        float cachedOutLength = 0.5f;
        bool isRebootInProgress;
        bool isShutdownInProgress;

        // Editor variables
#if UNITY_EDITOR
        public int currentEditorTab;
#endif

        void Awake()
        {
            // Set target canvas if it's missing
            if (targetCanvas == null) { targetCanvas = GetComponentInParent<Canvas>(); }

            // Get animator state lengths
            if (bootAnimator != null) { cachedStartLength = DreamOSInternalTools.GetAnimatorClipLength(bootAnimator, "BootScreen_Start") + 0.1f; }
            if (bootAnimator != null) { cachedOutLength = DreamOSInternalTools.GetAnimatorClipLength(bootAnimator, "BootScreen_Out") + 0.1f; }
        }

        void OnEnable()
        {
            if (bootOnEnable)
            {
                Boot();
            }
        }

        void OnDisable()
        {
            if (bootAnimator != null) 
            { 
                bootAnimator.gameObject.SetActive(false); 
            }
        }

        public void Boot()
        {
            if (bootAnimator.gameObject.activeInHierarchy) { return; }
            if (userManager == null) { Debug.LogError("<b>[Boot Manager]</b> User Manager is missing, but it's an essential variable for boot to work.", this); return; }

            // Disable or enable objects on boot
            userManager.setupScreen.gameObject.SetActive(false);
            userManager.lockScreen.gameObject.SetActive(true);
            userManager.desktopScreen.gameObject.SetActive(true);

            if (userManager.desktopScreen.gameObject.activeInHierarchy) { userManager.desktopScreen.Play("Instant Out"); }
            Invoke("BootHelper", initTime);

            onBootStart.Invoke();
        }

        void BootHelper()
        {
            if (!gameObject.activeInHierarchy)
                return;

            userManager.lockScreen.gameObject.SetActive(false);
            userManager.desktopScreen.gameObject.SetActive(false);
            bootAnimator.gameObject.SetActive(true);
            bootAnimator.enabled = true;
            bootAnimator.Play("Start");

            StopCoroutine("StartBootProcess");
            StartCoroutine("StartBootProcess");
        }

        public void Reboot()
        {
            if (isRebootInProgress)
                return;

            UnityEvent events = new UnityEvent();
            events.AddListener(delegate
            {
                onRebootEnd.Invoke();
                isRebootInProgress = false;

                targetCanvas.gameObject.SetActive(false);
                targetCanvas.gameObject.SetActive(true);
            });

            DoFadeInAnimation(events);
            isRebootInProgress = true;
            onRebootStart.Invoke();
        }

        public void Shutdown()
        {
            if (isShutdownInProgress)
                return;

            UnityEvent events = new UnityEvent();
            events.AddListener(delegate 
            {
                isShutdownInProgress = false; 
                targetCanvas.gameObject.SetActive(false); 
            });

            DoFadeInAnimation(events);
            isShutdownInProgress = true;
        }

        void DoFadeInAnimation(UnityEvent externalEvents)
        {
            GameObject animObj = new GameObject();
            animObj.name = "Boot Fade Animation (Temp)";
            animObj.transform.SetParent(targetCanvas.transform, false);
            animObj.transform.SetAsLastSibling();

            Image animImg = animObj.AddComponent<Image>();
            animImg.color = new Color(0, 0, 0, 0);

            RectTransform animRect = animObj.gameObject.GetComponent<RectTransform>();
            animRect.anchorMin = new Vector2(0, 0);
            animRect.anchorMax = new Vector2(1, 1);
            animRect.offsetMin = new Vector2(0, 0);
            animRect.offsetMax = new Vector2(0, 0);

            ImageFading animFading = animObj.AddComponent<ImageFading>();
            animFading.frameSkip = fadeFrameSkip;
            animFading.onFadeInEnd.AddListener(delegate
            {
                externalEvents.Invoke();
                Destroy(animObj);
            });
            animFading.fadeSpeed = fadeSpeed;
            animFading.FadeIn();
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

            StartCoroutine("DisableBootScreen");
        }

        IEnumerator DisableBootScreenAnimator()
        {
            yield return new WaitForSeconds(cachedStartLength);
            bootAnimator.enabled = false;
        }

        IEnumerator DisableBootScreen()
        {
            yield return new WaitForSeconds(cachedOutLength);
            bootAnimator.gameObject.SetActive(false);
        }
    }
}