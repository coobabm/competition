using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Opens and closes a panel with a scale or size animation on unscaled time (context menus, tray popups,
    /// notification panels). Replaces DreamOS's UIPopup (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// "Horizontal" and "Vertical" animate the RectTransform size from zero to the authored size; with fitter mode the
    /// size is first measured after the ContentSizeFitter has run. The animation coroutines are started by name so a
    /// new in/out animation cancels its opposite.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("LingGuang/OS/Animation/UI Popup")]
    public class UIPopup : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool closeOnDisable = false;
        [SerializeField] private bool instantOut = false;
        [Tooltip("Enables content size fitter mode.")]
        [SerializeField] private bool fitterMode = false;
        [SerializeField] private StartBehaviour startBehaviour;

        [Header("Animation")]
        [SerializeField] private AnimationMode animationMode;
        [SerializeField] private AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
        [Range(0.5f, 10)] public float curveSpeed = 4f;

        [Header("Events")]
        public UnityEvent onEnable = new UnityEvent();
        public UnityEvent onVisible = new UnityEvent();
        public UnityEvent onDisable = new UnityEvent();

        RectTransform rect;
        CanvasGroup cg;
        Vector2 rectHelper;
        bool isInitialized = false;
        bool isFitterInitialized = false;
        [HideInInspector] public bool isOn = false;

        public enum AnimationMode { Scale, Horizontal, Vertical }
        public enum StartBehaviour { Default, Disabled, Static }

        void Start()
        {
            if (startBehaviour == StartBehaviour.Disabled)
            {
                gameObject.SetActive(false);
            }
        }

        void OnEnable()
        {
            if (!isInitialized) { Initialize(); }
            if (playOnEnable) { PlayIn(); }
        }

        void OnDisable()
        {
            if (closeOnDisable)
            {
                gameObject.SetActive(false);
                isOn = false;
            }
        }

        void Initialize()
        {
            if (rect == null) { rect = GetComponent<RectTransform>(); }
            if (cg == null) { cg = GetComponent<CanvasGroup>(); }
            if (startBehaviour == StartBehaviour.Disabled || startBehaviour == StartBehaviour.Static) { rectHelper = rect.sizeDelta; }

            isInitialized = true;
        }

        public void ResetFitterData()
        {
            isFitterInitialized = false;
        }

        public void Animate()
        {
            if (isOn) { PlayOut(); }
            else { PlayIn(); }
        }

        public void PlayIn()
        {
            gameObject.SetActive(true);

            if (fitterMode && !isFitterInitialized)
            {
                cg.alpha = 0;
                StartCoroutine(nameof(InitFitter));
                return;
            }

            if (cg != null)
            {
                bool running = gameObject.activeInHierarchy;

                switch (animationMode)
                {
                    case AnimationMode.Scale:
                        if (running) { StartCoroutine(nameof(ScaleIn)); }
                        else { cg.alpha = 1; rect.localScale = new Vector3(1, 1, 1); }
                        break;

                    case AnimationMode.Horizontal:
                        if (running) { StartCoroutine(nameof(HorizontalIn)); }
                        else { cg.alpha = 1; }
                        break;

                    case AnimationMode.Vertical:
                        if (running) { StartCoroutine(nameof(VerticalIn)); }
                        else { cg.alpha = 1; }
                        break;
                }
            }

            isOn = true;
            onEnable.Invoke();
        }

        public void PlayOut()
        {
            if (instantOut)
            {
                gameObject.SetActive(false);
                isOn = false;
                return;
            }

            if (cg != null)
            {
                bool running = gameObject.activeInHierarchy;

                switch (animationMode)
                {
                    case AnimationMode.Scale:
                        if (running) { StartCoroutine(nameof(ScaleOut)); }
                        else { cg.alpha = 0; rect.localScale = new Vector3(0, 0, 0); gameObject.SetActive(false); }
                        break;

                    case AnimationMode.Horizontal:
                        if (running) { StartCoroutine(nameof(HorizontalOut)); }
                        else { cg.alpha = 0; gameObject.SetActive(false); }
                        break;

                    case AnimationMode.Vertical:
                        if (running) { StartCoroutine(nameof(VerticalOut)); }
                        else { cg.alpha = 0; gameObject.SetActive(false); }
                        break;
                }
            }

            isOn = false;
            onDisable.Invoke();
        }

        // Fitter mode: let the ContentSizeFitter lay the panel out once, remember that size, then play the animation.
        IEnumerator InitFitter()
        {
            yield return new WaitForSecondsRealtime(0.04f);

            ContentSizeFitter csf = GetComponent<ContentSizeFitter>();
            csf.enabled = false;

            rectHelper = rect.sizeDelta;
            isFitterInitialized = true;
            PlayIn();
        }

        // The size an expanding panel animates to: the live size by default, the remembered one otherwise.
        Vector2 ExpandedSize()
        {
            if (!fitterMode && startBehaviour == StartBehaviour.Default) { return rect.sizeDelta; }
            return rectHelper;
        }

        IEnumerator ScaleIn()
        {
            StopCoroutine(nameof(ScaleOut));

            float elapsedTime = 0;

            rect.localScale = new Vector3(0, 0, 0);
            cg.alpha = 0;

            while (rect.localScale.x < 0.99)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float smoothValue = Mathf.Lerp(0, 1, animationCurve.Evaluate(elapsedTime * curveSpeed));

                rect.localScale = new Vector3(smoothValue, smoothValue, smoothValue);
                cg.alpha = smoothValue;

                yield return null;
            }

            cg.alpha = 1;
            rect.localScale = new Vector3(1, 1, 1);
            onVisible.Invoke();
        }

        IEnumerator ScaleOut()
        {
            StopCoroutine(nameof(ScaleIn));

            float elapsedTime = 0;

            rect.localScale = new Vector3(1, 1, 1);
            cg.alpha = 1;

            while (rect.localScale.x > 0.01)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float smoothValue = Mathf.Lerp(1, 0, animationCurve.Evaluate(elapsedTime * curveSpeed));

                rect.localScale = new Vector3(smoothValue, smoothValue, smoothValue);
                cg.alpha = smoothValue;

                yield return null;
            }

            cg.alpha = 0;
            rect.localScale = new Vector3(0, 0, 0);
            gameObject.SetActive(false);
        }

        IEnumerator HorizontalIn()
        {
            StopCoroutine(nameof(HorizontalOut));

            float elapsedTime = 0;

            Vector2 startPos = new Vector2(0, rect.sizeDelta.y);
            Vector2 endPos = ExpandedSize();

            rect.sizeDelta = startPos;

            while (rect.sizeDelta.x <= endPos.x - 0.1f)
            {
                elapsedTime += Time.unscaledDeltaTime;

                cg.alpha += Time.unscaledDeltaTime * (curveSpeed * 2);
                rect.sizeDelta = Vector2.Lerp(startPos, endPos, animationCurve.Evaluate(elapsedTime * curveSpeed));

                yield return null;
            }

            cg.alpha = 1;
            rect.sizeDelta = endPos;
            onVisible.Invoke();
        }

        IEnumerator HorizontalOut()
        {
            StopCoroutine(nameof(HorizontalIn));

            float elapsedTime = 0;

            Vector2 startPos = rect.sizeDelta;
            Vector2 endPos = new Vector2(0, rect.sizeDelta.y);

            while (rect.sizeDelta.x >= 0.1f)
            {
                elapsedTime += Time.unscaledDeltaTime;

                cg.alpha -= Time.unscaledDeltaTime * (curveSpeed * 2);
                rect.sizeDelta = Vector2.Lerp(startPos, endPos, animationCurve.Evaluate(elapsedTime * curveSpeed));

                yield return null;
            }

            cg.alpha = 0;
            rect.sizeDelta = endPos;
            rect.gameObject.SetActive(false);
        }

        IEnumerator VerticalIn()
        {
            StopCoroutine(nameof(VerticalOut));

            float elapsedTime = 0;

            Vector2 startPos = new Vector2(rect.sizeDelta.x, 0);
            Vector2 endPos = ExpandedSize();

            rect.sizeDelta = startPos;

            while (rect.sizeDelta.y <= endPos.y - 0.1f)
            {
                elapsedTime += Time.unscaledDeltaTime;

                cg.alpha += Time.unscaledDeltaTime * (curveSpeed * 2);
                rect.sizeDelta = Vector2.Lerp(startPos, endPos, animationCurve.Evaluate(elapsedTime * curveSpeed));

                yield return null;
            }

            cg.alpha = 1;
            rect.sizeDelta = endPos;
            onVisible.Invoke();
        }

        IEnumerator VerticalOut()
        {
            StopCoroutine(nameof(VerticalIn));

            float elapsedTime = 0;

            Vector2 startPos = rect.sizeDelta;
            Vector2 endPos = new Vector2(rect.sizeDelta.x, 0);

            while (rect.sizeDelta.y >= 0.1f)
            {
                elapsedTime += Time.unscaledDeltaTime;

                cg.alpha -= Time.unscaledDeltaTime * (curveSpeed * 2);
                rect.sizeDelta = Vector2.Lerp(startPos, endPos, animationCurve.Evaluate(elapsedTime * curveSpeed));

                yield return null;
            }

            cg.alpha = 0;
            rect.sizeDelta = endPos;
            rect.gameObject.SetActive(false);
        }
    }
}
