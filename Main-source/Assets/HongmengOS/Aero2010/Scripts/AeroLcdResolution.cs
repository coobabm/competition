using UnityEngine;

namespace HongmengOS.Aero2010
{
    /// <summary>
    /// Renders the offscreen desktop at the LCD panel's real on-screen pixel height (keeping the panel's
    /// aspect), so text and icons stay sharp at any window size or screen resolution. The desktop canvas
    /// scales with its camera's texture (CanvasScaler, 1920×1080 reference), so the layout itself does not
    /// change; only the pixel density does. The serialized texture asset is left untouched.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AeroLcdController))]
    public sealed class AeroLcdResolution : MonoBehaviour
    {
        public int minHeight = 720, maxHeight = 2160;
        [Min(0)] public float settleSeconds = .25f;
        AeroLcdController lcd;
        RenderTexture original, current;
        int wanted;
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
            int height = TargetHeight();
            if (height <= 0) return;
            if (texture != null && Mathf.Abs(texture.height - height) <= Mathf.Max(8, texture.height * .04f)) { wanted = 0; return; }
            // Wait for the size to settle so dragging the game window does not reallocate every frame.
            if (height != wanted) { wanted = height; wantedSince = Time.unscaledTime; return; }
            if (Time.unscaledTime - wantedSince < settleSeconds) return;
            Resize(height);
            wanted = 0;
        }

        int TargetHeight()
        {
            var canvas = lcd.display.canvas;
            var view = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (view == null) return 0;
            lcd.display.rectTransform.GetWorldCorners(Corners);
            float pixels = Mathf.Abs(view.WorldToScreenPoint(Corners[1]).y - view.WorldToScreenPoint(Corners[0]).y);
            if (float.IsNaN(pixels) || pixels < 1) return 0;
            int max = Mathf.Min(maxHeight, SystemInfo.maxTextureSize);
            return Mathf.Clamp(Mathf.RoundToInt(pixels / 8f) * 8, Mathf.Min(minHeight, max), max);
        }

        void Resize(int height)
        {
            int width = Mathf.RoundToInt(height * (float)original.width / original.height);
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
