using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Michsky.DreamOS
{
    public class HoverEffect : MonoBehaviour
    {
        public Image targetImage;
        public Canvas targetCanvas;

        // Helpers
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
            if (targetCanvas != null && (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera || targetCanvas.renderMode == RenderMode.WorldSpace)) { ProcessPosition(targetCanvas.worldCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue())); }
            else { ProcessPosition(Mouse.current.position.ReadValue()); }
        }

        void ProcessPosition(Vector2 pos)
        {
            targetImage.transform.position = pos;
        }

        public void StartFadeIn()
        {
            gameObject.SetActive(true);

            StopCoroutine("DoFadeOut");
            StopCoroutine("DoFadeIn");
            StartCoroutine("DoFadeIn");
        }

        public void StartFadeOut()
        {
            if (!gameObject.activeInHierarchy)
                return;

            StopCoroutine("DoFadeOut");
            StopCoroutine("DoFadeIn");
            StartCoroutine("DoFadeOut");
        }

        IEnumerator DoFadeIn()
        {
            while (targetImage.color.a < transitionAlpha)
            {
                targetImage.color = Color.Lerp(targetImage.color, new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b, transitionAlpha), Time.deltaTime * speed); ;
                yield return null;
            }

            targetImage.color = new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b, transitionAlpha);
        }

        IEnumerator DoFadeOut()
        {
            while (targetImage.color.a > 0.01f)
            {
                targetImage.color = Color.Lerp(targetImage.color, new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b, 0), Time.deltaTime * speed); ;
                yield return null;
            }

            targetImage.color = new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b, 0);
            gameObject.SetActive(false);
        }
    }
}