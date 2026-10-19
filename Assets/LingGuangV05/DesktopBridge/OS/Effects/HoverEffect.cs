using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The soft light that follows the mouse cursor over a button (the glow image tracks the pointer and fades with
    /// hover). Replaces DreamOS's HoverEffect (2026-10-19): same GUID, serialized fields and public API.
    /// <see cref="ButtonManager"/> sets speed/transitionAlpha and calls StartFadeIn/StartFadeOut.
    /// </summary>
    public class HoverEffect : MonoBehaviour
    {
        public Image targetImage;
        public Canvas targetCanvas;

        [HideInInspector] public float speed;
        [HideInInspector] public float transitionAlpha;
        [HideInInspector] public bool fadeIn;
        [HideInInspector] public bool fadeOut;

        void Start()
        {
            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }
        }

        void Update()
        {
            // No mouse (touch-only device): nothing to follow.
            Mouse mouse = Mouse.current;
            if (mouse == null) { return; }

            Vector2 screenPos = mouse.position.ReadValue();

            if (targetCanvas != null && (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera || targetCanvas.renderMode == RenderMode.WorldSpace))
            {
                ProcessPosition(targetCanvas.worldCamera.ScreenToWorldPoint(screenPos));
            }

            else { ProcessPosition(screenPos); }
        }

        void ProcessPosition(Vector2 pos)
        {
            targetImage.transform.position = pos;
        }

        public void StartFadeIn()
        {
            gameObject.SetActive(true);

            StopCoroutine(nameof(DoFadeOut));
            StopCoroutine(nameof(DoFadeIn));
            StartCoroutine(nameof(DoFadeIn));
        }

        public void StartFadeOut()
        {
            if (!gameObject.activeInHierarchy)
                return;

            StopCoroutine(nameof(DoFadeOut));
            StopCoroutine(nameof(DoFadeIn));
            StartCoroutine(nameof(DoFadeOut));
        }

        void SetAlpha(float alpha)
        {
            Color c = targetImage.color;
            targetImage.color = new Color(c.r, c.g, c.b, alpha);
        }

        IEnumerator DoFadeIn()
        {
            while (targetImage.color.a < transitionAlpha)
            {
                Color c = targetImage.color;
                targetImage.color = Color.Lerp(c, new Color(c.r, c.g, c.b, transitionAlpha), Time.deltaTime * speed);
                yield return null;
            }

            SetAlpha(transitionAlpha);
        }

        IEnumerator DoFadeOut()
        {
            while (targetImage.color.a > 0.01f)
            {
                Color c = targetImage.color;
                targetImage.color = Color.Lerp(c, new Color(c.r, c.g, c.b, 0), Time.deltaTime * speed);
                yield return null;
            }

            SetAlpha(0);
            gameObject.SetActive(false);
        }
    }
}
