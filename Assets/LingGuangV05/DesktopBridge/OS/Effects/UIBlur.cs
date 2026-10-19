using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Animates the blur strength of the built-in-pipeline blur material behind windows, menus and the lock screen.
    /// Replaces DreamOS's UIBlur (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// Only active on the built-in render pipeline (the material's "_Size" property by default); under SRP the
    /// calls are no-ops.
    /// </summary>
    [ExecuteInEditMode]
    public class UIBlur : MonoBehaviour
    {
        [Header("Resources")]
        public Material blurMaterial;

        [Header("Settings")]
        [Range(0.0f, 10)] public float blurValue = 5.0f;
        [Range(0.1f, 50)] public float animationSpeed = 25;
        public string customProperty = "_Size";

        float currentBlurValue;

        static bool UsesScriptableRenderPipeline => GraphicsSettings.defaultRenderPipeline != null;

        void Awake()
        {
            if (UsesScriptableRenderPipeline || blurMaterial == null) { return; }
            if (customProperty == null) { customProperty = "_Size"; }

            blurMaterial.SetFloat(customProperty, 0);
        }

        public void BlurInAnim()
        {
            if (UsesScriptableRenderPipeline || gameObject.activeInHierarchy == false)
                return;

            StopCoroutine(nameof(BlurOut));
            StopCoroutine(nameof(BlurIn));
            StartCoroutine(nameof(BlurIn));
        }

        public void BlurOutAnim()
        {
            if (UsesScriptableRenderPipeline || gameObject.activeInHierarchy == false)
                return;

            StopCoroutine(nameof(BlurIn));
            StopCoroutine(nameof(BlurOut));
            StartCoroutine(nameof(BlurOut));
        }

        public void SetBlurValue(float cbv) { blurValue = cbv; }

        IEnumerator BlurIn()
        {
            currentBlurValue = blurMaterial.GetFloat(customProperty);

            while (currentBlurValue <= blurValue)
            {
                currentBlurValue += Time.deltaTime * animationSpeed;
                if (currentBlurValue >= blurValue) { currentBlurValue = blurValue; }
                blurMaterial.SetFloat(customProperty, currentBlurValue);
                yield return null;
            }
        }

        IEnumerator BlurOut()
        {
            currentBlurValue = blurMaterial.GetFloat(customProperty);

            while (currentBlurValue >= 0)
            {
                currentBlurValue -= Time.deltaTime * animationSpeed;
                if (currentBlurValue <= 0) { currentBlurValue = 0; }
                blurMaterial.SetFloat(customProperty, currentBlurValue);
                yield return null;
            }
        }
    }
}
