using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LingGuang.Game
{
    /// <summary>
    /// Low-resolution world, native-resolution glow and HUD. The input camera never
    /// targets a texture, so existing ScreenToWorldPoint / picking stays unchanged.
    /// Owns all temporary resources; no render-pipeline assets or project settings change.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera)), DefaultExecutionOrder(1000)]
    public sealed class PixelBoardPresentation : MonoBehaviour
    {
        public const int ReferenceHeight = 320;
        public RenderTexture WorldTexture => worldTexture;
        public Camera WorldCamera => worldCamera;
        public RawImage Display => display;
        public bool ReduceFlash { get; set; }
        public NeuralWaveField WaveField { get; set; }

        Camera inputCamera, worldCamera;
        RenderTexture worldTexture;
        GameObject displayRoot;
        RawImage display;
        Material composite;
        int originalMask;
        bool originalPostProcessing;
        UniversalAdditionalCameraData inputData;
        bool initialized;

        public static Vector2Int BufferSize(int width, int height)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            int scale = Mathf.Max(1, Mathf.RoundToInt(height / (float)ReferenceHeight));
            return new Vector2Int(Mathf.CeilToInt(width / (float)scale), Mathf.CeilToInt(height / (float)scale));
        }

        void OnEnable()
        {
            if (initialized) return;
            var shader = Shader.Find("LingGuang/PixelComposite");
            inputCamera = GetComponent<Camera>();
            if (shader == null || !shader.isSupported || inputCamera.targetTexture != null)
            {
                Debug.LogWarning("[LingGuang] Pixel compositor unavailable; retaining the normal camera.", this);
                enabled = false;
                return;
            }

            originalMask = inputCamera.cullingMask;
            inputData = inputCamera.GetUniversalAdditionalCameraData();
            originalPostProcessing = inputData.renderPostProcessing;
            var go = new GameObject("Pixel World Camera");
            go.transform.SetParent(transform, false);
            worldCamera = go.AddComponent<Camera>();
            worldCamera.CopyFrom(inputCamera);
            worldCamera.tag = "Untagged";
            var data = worldCamera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.renderShadows = false;

            composite = new Material(shader) { name = "LG_PixelComposite", hideFlags = HideFlags.DontSave };
            displayRoot = new GameObject("Pixel Board Display", typeof(RectTransform), typeof(Canvas));
            displayRoot.transform.SetParent(transform, false);
            var canvas = displayRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;
            var image = new GameObject("Pixel World", typeof(RectTransform), typeof(RawImage));
            image.transform.SetParent(displayRoot.transform, false);
            display = image.GetComponent<RawImage>();
            display.raycastTarget = false;
            display.material = composite;
            var rect = display.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            inputCamera.cullingMask = 0;
            inputData.renderPostProcessing = false;
            initialized = true;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (!initialized || worldCamera == null) return;
            var size = BufferSize(Screen.width, Screen.height);
            if (worldTexture == null || worldTexture.width != size.x || worldTexture.height != size.y)
            {
                ReleaseTexture();
                var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                    ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
                worldTexture = new RenderTexture(size.x, size.y, 24, format, RenderTextureReadWrite.Linear)
                {
                    name = "LG_PixelWorld", filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp, antiAliasing = 1,
                    useMipMap = false, autoGenerateMips = false, hideFlags = HideFlags.DontSave
                };
                worldTexture.Create();
                display.texture = worldTexture;
            }
            // CameraRig runs earlier. Copy its projection, not its screen-sized render target.
            worldCamera.transform.SetPositionAndRotation(inputCamera.transform.position, inputCamera.transform.rotation);
            worldCamera.orthographic = inputCamera.orthographic;
            worldCamera.orthographicSize = inputCamera.orthographicSize;
            worldCamera.fieldOfView = inputCamera.fieldOfView;
            worldCamera.aspect = inputCamera.aspect;
            worldCamera.nearClipPlane = inputCamera.nearClipPlane;
            worldCamera.farClipPlane = inputCamera.farClipPlane;
            worldCamera.backgroundColor = inputCamera.backgroundColor;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.cullingMask = originalMask;
            worldCamera.depth = inputCamera.depth - 1f;
            worldCamera.allowHDR = true;
            worldCamera.allowMSAA = false;
            worldCamera.allowDynamicResolution = false;
            worldCamera.targetTexture = worldTexture;
            composite.SetFloat("_GlowStrength", ReduceFlash ? 0.22f : 0.48f);
            bool water = WaveField != null && WaveField.ActiveCount(Time.time) > 0;
            composite.SetFloat("_WaterRefraction", water && !ReduceFlash ? 0.032f : 0f);
            if (water)
            {
                WaveField.ApplyTo(composite, Time.time);
                float distance = Mathf.Abs(inputCamera.transform.position.z);
                var origin = inputCamera.ViewportToWorldPoint(new Vector3(0, 0, distance));
                var u = inputCamera.ViewportToWorldPoint(new Vector3(1, 0, distance)) - origin;
                var v = inputCamera.ViewportToWorldPoint(new Vector3(0, 1, distance)) - origin;
                composite.SetVector("_WaterViewOrigin", origin);
                composite.SetVector("_WaterViewU", u);
                composite.SetVector("_WaterViewV", v);
            }
        }

        void ReleaseTexture()
        {
            if (worldCamera != null) worldCamera.targetTexture = null;
            if (display != null) display.texture = null;
            if (worldTexture == null) return;
            worldTexture.Release();
            Release(worldTexture);
            worldTexture = null;
        }

        void OnDisable()
        {
            if (!initialized) return;
            initialized = false;
            if (inputCamera != null) inputCamera.cullingMask = originalMask;
            if (inputData != null) inputData.renderPostProcessing = originalPostProcessing;
            if (worldCamera != null) worldCamera.enabled = false;
            if (displayRoot != null) displayRoot.SetActive(false);
            ReleaseTexture();
            if (worldCamera != null) Release(worldCamera.gameObject);
            Release(displayRoot);
            Release(composite);
            worldCamera = null;
            displayRoot = null;
            display = null;
            composite = null;
        }

        static void Release(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
