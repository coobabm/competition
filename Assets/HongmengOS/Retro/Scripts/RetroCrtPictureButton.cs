using UnityEngine;

namespace HongmengOS.Retro
{
    /// <summary>
    /// A physical front-panel picture key. Bind Adjust to a UI Button's onClick;
    /// picture settings remain adjustable while the CRT display is powered off.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RetroCrtPictureButton : MonoBehaviour
    {
        public enum Control { Focus, Brightness }

        public RetroCrtController controller;
        public Control parameter;
        [Tooltip("Signed normalized adjustment per press, normally -0.05 or +0.05.")]
        [Range(-1f, 1f)] public float step = 0.05f;

        public void Adjust()
        {
            if (!isActiveAndEnabled || controller == null || !controller.isActiveAndEnabled) return;
            if (float.IsNaN(step) || float.IsInfinity(step)) return;

            float previous = parameter == Control.Focus ? controller.Focus : controller.Brightness;
            if (float.IsNaN(previous) || float.IsInfinity(previous)) return;
            float next = Mathf.Clamp01(previous + step);
            if (Mathf.Approximately(previous, next)) return;

            // Do not gate on CanInteract or IsPowered: these are physical keys,
            // not controls inside the desktop image.
            if (parameter == Control.Focus) controller.SetFocus(next);
            else controller.SetBrightness(next);
            controller.PlayKnobTick();
        }

        private void OnValidate()
        {
            step = float.IsNaN(step) || float.IsInfinity(step) ? 0.05f : Mathf.Clamp(step, -1f, 1f);
        }
    }
}
