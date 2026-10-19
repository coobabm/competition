using System.Collections;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// A dropdown-style panel (start menu, tray popups, quick settings) that grows from its collapsed size to
    /// <see cref="panelSize"/> while fading in, and back again on close (2026-10-19). Replaces DreamOS's
    /// PopupPanelManager: same GUID, serialized fields and public API.
    ///
    /// With <see cref="disableAnimation"/> the component turns itself off in Awake and Open / Close just toggle
    /// the object. The horizontal animation keeps the original loop conditions, which test the panel height
    /// rather than its width; animation coroutines are started and stopped by name, like before.
    /// </summary>
    public class PopupPanelManager : MonoBehaviour
    {
        [Header("Settings")]
        public bool enableBlurAnim = true;
        public bool useTransition = true;
        public bool disableOnOut = true;
        public bool disableAnimation = false;
        public float closeOn = 25;
        public float panelSize = 100;

        [Header("Animation")]
        public DefaultState defaultPanelState;
        public AnimationDirection animationDirection;
        [SerializeField] private AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
        [SerializeField] [Range(0.5f, 10)] private float curveSpeed = 3;
        [SerializeField] [Range(1f, 12)] private float fadeSpeed = 3;

        RectTransform objectRect;
        CanvasGroup objectCG;
        UIBlur bManager;
        [HideInInspector] public bool isOn;

        public enum DefaultState { Minimized, Expanded }
        public enum AnimationDirection { Vertical, Horizontal }

        bool IsVertical { get { return animationDirection == AnimationDirection.Vertical; } }

        void Awake()
        {
            objectRect = gameObject.GetComponent<RectTransform>();

            if (disableAnimation)
            {
                this.enabled = false;
                return;
            }

            if (useTransition)
            {
                objectCG = gameObject.GetComponent<CanvasGroup>();
                SetFade(0, false);
            }

            if (defaultPanelState == DefaultState.Minimized)
            {
                SetAxisSize(closeOn);
                isOn = false;
            }
            else if (defaultPanelState == DefaultState.Expanded)
            {
                SetAxisSize(panelSize);
                if (useTransition) { SetFade(1, true); }
                isOn = true;
            }

            if (enableBlurAnim) { bManager = gameObject.GetComponent<UIBlur>(); }
            if (!isOn && disableOnOut && defaultPanelState != DefaultState.Expanded) { gameObject.SetActive(false); }
        }

        void OnDisable()
        {
            isOn = false;
        }

        public void AnimatePanel()
        {
            gameObject.SetActive(true);

            if (isOn)
            {
                if (useTransition && !disableAnimation) { objectCG.blocksRaycasts = false; objectCG.interactable = false; }
                if (enableBlurAnim && !disableAnimation) { bManager.BlurOutAnim(); }

                ClosePanel();
            }
            else
            {
                if (useTransition && !disableAnimation) { objectCG.blocksRaycasts = true; objectCG.interactable = true; }
                if (enableBlurAnim && !disableAnimation) { bManager.BlurInAnim(); }

                OpenPanel();
            }
        }

        public void OpenPanel()
        {
            if (objectRect == null) { objectRect = gameObject.GetComponent<RectTransform>(); }
            if (isOn) { return; }

            gameObject.SetActive(true);
            isOn = true;
            DesktopSfx.Play(DesktopSound.MenuOpen);

            if (disableAnimation) { return; }

            if (IsVertical)
            {
                StopCoroutine(nameof(VerticalExpand));
                StartCoroutine(nameof(VerticalExpand));
            }
            else
            {
                StopCoroutine(nameof(HorizontalExpand));
                StartCoroutine(nameof(HorizontalExpand));
            }

            if (useTransition)
            {
                StartCoroutine(nameof(FadeIn));

                objectCG.blocksRaycasts = true;
                objectCG.interactable = true;
            }

            if (enableBlurAnim) { bManager.BlurInAnim(); }
        }

        public void ClosePanel()
        {
            if (objectRect == null || !isOn) { return; }

            isOn = false;

            if (disableAnimation)
            {
                gameObject.SetActive(false);
                return;
            }

            if (IsVertical)
            {
                StopCoroutine(nameof(VerticalMinimize));
                StartCoroutine(nameof(VerticalMinimize));
            }
            else
            {
                StopCoroutine(nameof(HorizontalMinimize));
                StartCoroutine(nameof(HorizontalMinimize));
            }

            if (useTransition)
            {
                StopCoroutine(nameof(FadeOut));
                StartCoroutine(nameof(FadeOut));

                if (disableOnOut)
                {
                    StopCoroutine(nameof(CheckForDisable));
                    StartCoroutine(nameof(CheckForDisable));
                }

                objectCG.blocksRaycasts = false;
                objectCG.interactable = false;
            }

            if (enableBlurAnim) { bManager.BlurOutAnim(); }
        }

        public void InstantMinimized()
        {
            if (objectRect == null || objectCG == null) { return; }

            objectRect.sizeDelta = new Vector2(objectRect.sizeDelta.x, closeOn);
            objectCG.alpha = 0;
        }

        public void InstantExpanded()
        {
            if (objectRect == null || objectCG == null) { return; }

            objectRect.sizeDelta = new Vector2(objectRect.sizeDelta.x, panelSize);
            objectCG.alpha = 1;
        }

        // Sets the size along the animation axis only.
        void SetAxisSize(float size)
        {
            objectRect.sizeDelta = IsVertical ? new Vector2(objectRect.sizeDelta.x, size) : new Vector2(size, objectRect.sizeDelta.y);
        }

        void SetFade(float alpha, bool interactive)
        {
            objectCG.alpha = alpha;
            objectCG.interactable = interactive;
            objectCG.blocksRaycasts = interactive;
        }

        // Expand/minimize coroutines are thin wrappers so each keeps its own name for Start/StopCoroutine; the
        // opposite one is stopped when it starts. Loop conditions are copied from the originals, including the
        // horizontal ones that read sizeDelta.y.
        IEnumerator VerticalExpand()
        {
            StopCoroutine(nameof(VerticalMinimize));
            return ResizeTo(new Vector2(objectRect.sizeDelta.x, panelSize), () => objectRect.sizeDelta.y < panelSize - 0.1f);
        }

        IEnumerator VerticalMinimize()
        {
            StopCoroutine(nameof(VerticalExpand));
            return ResizeTo(new Vector2(objectRect.sizeDelta.x, closeOn), () => objectRect.sizeDelta.y > closeOn + 0.1f);
        }

        IEnumerator HorizontalExpand()
        {
            StopCoroutine(nameof(HorizontalMinimize));
            return ResizeTo(new Vector2(panelSize, objectRect.sizeDelta.y), () => objectRect.sizeDelta.y < panelSize - 0.1f);
        }

        IEnumerator HorizontalMinimize()
        {
            StopCoroutine(nameof(HorizontalExpand));
            return ResizeTo(new Vector2(closeOn, objectRect.sizeDelta.y), () => objectRect.sizeDelta.y > panelSize + closeOn);
        }

        IEnumerator ResizeTo(Vector2 endPos, System.Func<bool> keepGoing)
        {
            float elapsedTime = 0;
            Vector2 startPos = objectRect.sizeDelta;

            while (keepGoing())
            {
                elapsedTime += Time.deltaTime;
                objectRect.sizeDelta = Vector2.Lerp(startPos, endPos, animationCurve.Evaluate(elapsedTime * curveSpeed));
                yield return null;
            }

            objectRect.sizeDelta = endPos;
        }

        IEnumerator FadeIn()
        {
            StopCoroutine(nameof(FadeOut));
            return FadeTo(0, 1, true);
        }

        IEnumerator FadeOut()
        {
            StopCoroutine(nameof(FadeIn));
            return FadeTo(objectCG.alpha, 0, false);
        }

        IEnumerator FadeTo(float startValue, float target, bool rising)
        {
            float elapsedTime = 0;

            while (rising ? objectCG.alpha < 0.99f : objectCG.alpha > 0.01f)
            {
                elapsedTime += Time.deltaTime;
                objectCG.alpha = Mathf.Lerp(startValue, target, animationCurve.Evaluate(elapsedTime * fadeSpeed));
                yield return null;
            }

            objectCG.alpha = target;
        }

        IEnumerator CheckForDisable()
        {
            while (objectCG.alpha > 0) { yield return null; }
            gameObject.SetActive(false);
        }
    }
}
