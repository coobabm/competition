using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Michsky.DreamOS
{
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("DreamOS/Animation/Image Fading")]
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

        // Helpers
        float frameDelay = 0.04f;
        Image targetImg;

        public enum EnableBehaviour { None, FadeIn, FadeOut }

        void OnEnable()
        {
            if (enableBehaviour == EnableBehaviour.FadeIn) { FadeIn(); }
            else if (enableBehaviour == EnableBehaviour.FadeOut) { FadeOut(); }
        }

        public void FadeIn()
        {
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            if (targetImg == null) { targetImg = GetComponent<Image>(); }

            targetImg.color = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, 0);      
            onFadeIn.Invoke();

            if (!frameSkip)
            {
                StopCoroutine("DoFadeIn");
                StartCoroutine("DoFadeIn");
            }

            else
            {
                StopCoroutine("DoFadeInFrameSkip");
                StartCoroutine("DoFadeInFrameSkip");
            }
        }

        public void FadeOut()
        {
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            if (targetImg == null) { targetImg = GetComponent<Image>(); }

            targetImg.color = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, 1);
            onFadeOut.Invoke();

            if (!frameSkip)
            {
                StopCoroutine("DoFadeOut");
                StartCoroutine("DoFadeOut");
            }

            else
            {
                StopCoroutine("DoFadeOutFrameSkip");
                StartCoroutine("DoFadeOutFrameSkip");
            }
        }

        IEnumerator DoFadeIn()
        {
            StopCoroutine("DoFadeOut");

            Color startingPoint = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, 0);
            float elapsedTime = 0;

            while (targetImg.color.a < 0.99f)
            {
                elapsedTime += Time.deltaTime;
                targetImg.color = Color.Lerp(startingPoint, new Color(startingPoint.r, startingPoint.g, startingPoint.b, 1), fadeCurve.Evaluate(elapsedTime * fadeSpeed)); ;
                yield return null;
            }

            targetImg.color = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, 1);
            onFadeInEnd.Invoke();
            if (doPingPong) { StartCoroutine("DoFadeOut"); }
        }

        IEnumerator DoFadeOut()
        {
            StopCoroutine("DoFadeIn");

            Color startingPoint = targetImg.color;
            float elapsedTime = 0;

            while (targetImg.color.a > 0.01f)
            {
                elapsedTime += Time.deltaTime;
                targetImg.color = Color.Lerp(startingPoint, new Color(startingPoint.r, startingPoint.g, startingPoint.b, 0), fadeCurve.Evaluate(elapsedTime * fadeSpeed)); ;
                yield return null;
            }

            targetImg.color = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, 0);
            onFadeOutEnd.Invoke();
            gameObject.SetActive(false);
        }

        IEnumerator DoFadeInFrameSkip()
        {
            StopCoroutine("DoFadeOutFrameSkip");

            float startingAlpha = targetImg.color.a;

            yield return new WaitForSeconds(frameDelay);

            if (targetImg.color.a < 0.99f)
            {
                targetImg.color = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, startingAlpha + 0.08f);
                StartCoroutine("DoFadeInFrameSkip");
            }

            else
            {
                targetImg.color = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, 1);
                onFadeInEnd.Invoke();
                if (doPingPong) { StartCoroutine("DoFadeOutFrameSkip"); }
            }
        }

        IEnumerator DoFadeOutFrameSkip()
        {
            StopCoroutine("DoFadeInFrameSkip");

            float startingAlpha = targetImg.color.a;

            yield return new WaitForSeconds(frameDelay);

            if (targetImg.color.a < 0.99f)
            {
                targetImg.color = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, startingAlpha - 0.08f);
                StartCoroutine("DoFadeInFrameSkip");
            }

            else
            {
                targetImg.color = new Color(targetImg.color.r, targetImg.color.g, targetImg.color.b, 0);
                onFadeOutEnd.Invoke();
                gameObject.SetActive(false);
            }
        }
    }
}