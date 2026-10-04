using UnityEngine;

namespace HongmengOS.Retro
{
    /// <summary>
    /// Mirrors the controller's indicator colour onto a physical model lens.
    /// The Graphic may be hidden: its colour remains the authoritative state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RetroCrtLampVisual : MonoBehaviour
    {
        public RetroCrtController controller;
        public Renderer lamp;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock originalBlock;
        private MaterialPropertyBlock workingBlock;
        private Renderer capturedRenderer;
        private bool originalWasEmpty;
        private bool captured;
        private bool hasLastColor;
        private Color lastColor;
        private Color lastEmission;

        private void OnEnable()
        {
            if (originalBlock == null) originalBlock = new MaterialPropertyBlock();
            if (workingBlock == null) workingBlock = new MaterialPropertyBlock();
            CaptureRenderer();
        }

        private void LateUpdate()
        {
            if (capturedRenderer != lamp) CaptureRenderer();
            if (!captured || capturedRenderer == null || controller == null || controller.powerLamp == null) return;

            Color color = controller.powerLamp.color;
            float intensity = controller.IsPowered || controller.IsTransitioning ? 0.4f : 0.008f;
            Color emission = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
            if (hasLastColor && color.Equals(lastColor) && emission.Equals(lastEmission)) return;

            // Preserve any unrelated per-renderer overrides added since enabling.
            workingBlock.Clear();
            capturedRenderer.GetPropertyBlock(workingBlock);
            workingBlock.SetColor(BaseColorId, color);
            workingBlock.SetColor(ColorId, color);
            workingBlock.SetColor(EmissionColorId, emission);
            capturedRenderer.SetPropertyBlock(workingBlock);
            lastColor = color;
            lastEmission = emission;
            hasLastColor = true;
        }

        private void CaptureRenderer()
        {
            RestoreOriginal();
            if (lamp == null || originalBlock == null) return;
            capturedRenderer = lamp;
            originalBlock.Clear();
            capturedRenderer.GetPropertyBlock(originalBlock);
            originalWasEmpty = originalBlock.isEmpty;
            captured = true;
            hasLastColor = false;
        }

        private void RestoreOriginal()
        {
            if (captured && capturedRenderer != null)
                capturedRenderer.SetPropertyBlock(originalWasEmpty ? null : originalBlock);
            capturedRenderer = null;
            captured = false;
            hasLastColor = false;
        }

        private void OnDisable()
        {
            RestoreOriginal();
        }

        private void OnDestroy()
        {
            RestoreOriginal();
        }
    }
}
