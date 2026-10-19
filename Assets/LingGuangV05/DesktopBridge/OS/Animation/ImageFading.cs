using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Fades an <see cref="Image"/> in or out and deactivates it after a fade-out (boot logo, setup backgrounds, game
    /// transitions). Replaces DreamOS's ImageFading (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// The curve fade runs on scaled time. The "frame skip" variant steps the alpha by 0.08 every 0.04 s instead, for
    /// slow machines; its fade-out keeps the old behaviour of snapping to transparent after one step.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("LingGuang/OS/Animation/Image Fading")]
    public class ImageFading : MonoBehaviour
    {
        [Header("Settings")]
        public bool doPingPong = false;
        public bool frameSkip = false;
        [Range(0.5f, 12)] public float fadeSpeed = 2f;
        [SerializeField] private AnimationCurve fadeCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
        [SerializeField] private EnableBehaviour enableBehaviour;

        [Header("Events")]
        public UnityEvent onFadeIn = new UnityEvent();
        public UnityEvent onFadeInEnd = new UnityEvent();
        public UnityEvent onFadeOut = new UnityEvent();
        public UnityEvent onFadeOutEnd = new UnityEvent();

        const float FrameSkipDelay = 0.04f;
        const float FrameSkipStep = 0.08f;

        Image targetImg;

        public enum EnableBehaviour { None, FadeIn, FadeOut }

        void OnEnable()
        {
            if (enableBehaviour == EnableBehaviour.FadeIn) { FadeIn(); }
            else if (enableBehaviour == EnableBehaviour.FadeOut) { FadeOut(); }
        }

        public void FadeIn()
        {
            PrepareFade(0);
            onFadeIn.Invoke();

            if (!frameSkip) { RestartCoroutine(nameof(DoFadeIn)); }
            else { RestartCoroutine(nameof(DoFadeInFrameSkip)); }
        }

        public void FadeOut()
        {
            PrepareFade(1);
            onFadeOut.Invoke();

            if (!frameSkip) { RestartCoroutine(nameof(DoFadeOut)); }
            else { RestartCoroutine(nameof(DoFadeOutFrameSkip)); }
        }

        // Wakes the object, caches the image and snaps it to the starting alpha.
        void PrepareFade(float startAlpha)
        {
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            if (targetImg == null) { targetImg = GetComponent<Image>(); }

            SetAlpha(startAlpha);
        }

        void RestartCoroutine(string routine)
        {
            StopCoroutine(routine);
            StartCoroutine(routine);
        }

        void SetAlpha(float alpha)
        {
            Color c = targetImg.color;
            targetImg.color = new Color(c.r, c.g, c.b, alpha);
        }

        IEnumerator DoFadeIn()
        {
            StopCoroutine(nameof(DoFadeOut));

            Color from = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, 0);
            Color to = new Color(from.r, from.g, from.b, 1);
            float elapsedTime = 0;

            while (targetImg.color.a < 0.99f)
            {
                elapsedTime += Time.deltaTime;
                targetImg.color = Color.Lerp(from, to, fadeCurve.Evaluate(elapsedTime * fadeSpeed));
                yield return null;
            }

            SetAlpha(1);
            onFadeInEnd.Invoke();
            if (doPingPong) { StartCoroutine(nameof(DoFadeOut)); }
        }

        IEnumerator DoFadeOut()
        {
            StopCoroutine(nameof(DoFadeIn));

            Color from = targetImg.color;
            Color to = new Color(from.r, from.g, from.b, 0);
            float elapsedTime = 0;

            while (targetImg.color.a > 0.01f)
            {
                elapsedTime += Time.deltaTime;
                targetImg.color = Color.Lerp(from, to, fadeCurve.Evaluate(elapsedTime * fadeSpeed));
                yield return null;
            }

            SetAlpha(0);
            onFadeOutEnd.Invoke();
            gameObject.SetActive(false);
        }

        IEnumerator DoFadeInFrameSkip()
        {
            StopCoroutine(nameof(DoFadeOutFrameSkip));

            float startingAlpha = targetImg.color.a;

            yield return new WaitForSeconds(FrameSkipDelay);

            if (targetImg.color.a < 0.99f)
            {
                SetAlpha(startingAlpha + FrameSkipStep);
                StartCoroutine(nameof(DoFadeInFrameSkip));
            }

            else
            {
                SetAlpha(1);
                onFadeInEnd.Invoke();
                if (doPingPong) { StartCoroutine(nameof(DoFadeOutFrameSkip)); }
            }
        }

        IEnumerator DoFadeOutFrameSkip()
        {
            StopCoroutine(nameof(DoFadeInFrameSkip));

            float startingAlpha = targetImg.color.a;

            yield return new WaitForSeconds(FrameSkipDelay);

            if (targetImg.color.a < 0.99f)
            {
                SetAlpha(startingAlpha - FrameSkipStep);
                StartCoroutine(nameof(DoFadeInFrameSkip));
            }

            else
            {
                SetAlpha(0);
                onFadeOutEnd.Invoke();
                gameObject.SetActive(false);
            }
        }
    }
}
