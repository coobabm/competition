using UnityEngine;

namespace HongmengOS.Aero2010
{
    /// <summary>
    /// Uses the panel aspect during boot and the actual viewport aspect in fullscreen desktop mode.
    /// The runtime texture follows settled window size for crisp text without changing shared assets.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AeroLcdController))]
    public sealed class AeroLcdResolution : MonoBehaviour
    {
        public int minHeight = 720, maxHeight = 2160;
        [Min(0)] public float settleSeconds = .25f;
        AeroLcdController lcd;
        RenderTexture original, current;
        bool viewportResolution;
        Vector2Int wanted;
        float wantedSince;
        static readonly Vector3[] Corners = new Vector3[4];

        public RenderTexture Current => lcd != null && lcd.sourceCamera != null ? lcd.sourceCamera.targetTexture : null;

        void Awake() { lcd = GetComponent<AeroLcdController>(); }

        void LateUpdate()
        {
            if (lcd == null || lcd.display == null || lcd.sourceCamera == null) return;
            var texture = lcd.sourceCamera.targetTexture;
            if (original == null) original = texture;
            if (original == null) return;
            var size = TargetSize();
            if (size.x <= 0 || size.y <= 0) return;
            if (texture != null && texture.width == size.x && texture.height == size.y) { wanted = Vector2Int.zero; return; }
            // Wait for the size to settle so dragging the game window does not reallocate every frame.
            if (size != wanted) { wanted = size; wantedSince = Time.unscaledTime; return; }
            if (Time.unscaledTime - wantedSince < settleSeconds) return;
            Resize(size);
            wanted = Vector2Int.zero;
        }

        // Warm the final render target before the camera flight. No allocation or canvas
        // aspect change should occur at the physical-panel/fullscreen handoff.
        public void PrepareViewportResolution() { viewportResolution = true; RefreshNow(); }

        public void RefreshNow()
        {
            if (lcd == null || lcd.sourceCamera == null || lcd.display == null) return;
            if (original == null) original = lcd.sourceCamera.targetTexture;
            if (original == null) return;
            var size = TargetSize();
            var texture = lcd.sourceCamera.targetTexture;
            if (size.x > 0 && size.y > 0 && (texture == null || texture.width != size.x || texture.height != size.y)) Resize(size);
            wanted = Vector2Int.zero;
        }

        Vector2Int TargetSize()
        {
            var canvas = lcd.display.canvas;
            if (viewportResolution || canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Preserve the actual viewport aspect, including 16:10 and ultrawide. Render
                // one texel per screen pixel up to the GPU/4K-height limit; never stretch 16:9.
                int limit = SystemInfo.maxTextureSize;
                float scale = Mathf.Min(1f, Mathf.Min((float)maxHeight / Mathf.Max(1, Screen.height), (float)limit / Mathf.Max(1, Screen.width)));
                return new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(Screen.width * scale)), Mathf.Max(1, Mathf.RoundToInt(Screen.height * scale)));
            }
            var view = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (view == null) return Vector2Int.zero;
            lcd.display.rectTransform.GetWorldCorners(Corners);
            float pixels = Mathf.Abs(view.WorldToScreenPoint(Corners[1]).y - view.WorldToScreenPoint(Corners[0]).y);
            if (float.IsNaN(pixels) || pixels < 1) return Vector2Int.zero;
            int max = Mathf.Min(maxHeight, SystemInfo.maxTextureSize);
            int height = Mathf.Clamp(Mathf.RoundToInt(pixels / 8f) * 8, Mathf.Min(minHeight, max), max);
            return new Vector2Int(Mathf.RoundToInt(height * (float)original.width / original.height), height);
        }

        void Resize(Vector2Int size)
        {
            int width = size.x, height = size.y;
            var desc = original.descriptor;
            desc.width = width; desc.height = height;
            var texture = new RenderTexture(desc)
            {
                name = original.name + " (" + width + "x" + height + ")",
                filterMode = original.filterMode,
                wrapMode = original.wrapMode,
            };
            texture.Create();
            lcd.sourceCamera.targetTexture = texture;
            lcd.display.texture = texture;
            Free();
            current = texture;
        }

        void Free()
        {
            if (current == null) return;
            current.Release();
            Destroy(current);
            current = null;
        }

        void OnDestroy()
        {
            if (current == null) return;
            if (lcd != null && lcd.sourceCamera != null && lcd.sourceCamera.targetTexture == current) lcd.sourceCamera.targetTexture = original;
            if (lcd != null && lcd.display != null && lcd.display.texture == current) lcd.display.texture = original;
            Free();
        }
    }
}
