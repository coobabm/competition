using Michsky.DreamOS;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// Keeps a window's content laid out at its normal (design) size and scales it uniformly to fill the
    /// window, like CanvasScaler's Expand mode for one window. Maximising the window makes every button
    /// and text larger in proportion instead of spreading the same small controls over a bigger area.
    /// The smaller ratio decides the scale; the other axis gets the extra room through the layout's anchors.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiFitScale : MonoBehaviour
    {
        public Vector2 designSize = new Vector2(1266, 763);
        public float minScale = .5f, maxScale = 4f;
        RectTransform rect;
        Vector2 lastParent = new Vector2(-1, -1);

        /// <summary>
        /// Fits <paramref name="root"/> to its parent. The design size is the parent's size while the window
        /// is not maximised; if it already is, <paramref name="fallback"/> is used.
        /// </summary>
        public static UiFitScale Attach(RectTransform root, WindowManager window, Vector2 fallback)
        {
            if (root == null) return null;
            var fit = root.GetComponent<UiFitScale>() ?? root.gameObject.AddComponent<UiFitScale>();
            var parent = root.parent as RectTransform;
            var measured = parent != null ? parent.rect.size : Vector2.zero;
            bool normal = window == null || !window.isFullscreen;
            fit.designSize = normal && measured.x > 1 && measured.y > 1 ? measured : fallback;
            fit.Apply(true);
            return fit;
        }

        public float Scale => rect != null ? rect.localScale.x : 1;

        void OnEnable() => Apply(true);

        // The window's maximise/restore animation resizes the container over several frames.
        void LateUpdate() => Apply(false);

        public void Apply(bool force)
        {
            if (rect == null) rect = transform as RectTransform;
            var parent = rect != null ? rect.parent as RectTransform : null;
            if (parent == null || designSize.x <= 1 || designSize.y <= 1) return;
            var size = parent.rect.size;
            if (!force && size == lastParent) return;
            lastParent = size;
            if (size.x <= 1 || size.y <= 1) return;
            float s = Mathf.Clamp(Mathf.Min(size.x / designSize.x, size.y / designSize.y), minScale, maxScale);
            // The normal window differs from the design size only by canvas rounding; keep it at exactly 1.
            if (Mathf.Abs(s - 1) < .01f) s = 1;
            var center = new Vector2(.5f, .5f);
            if (rect.anchorMin != center || rect.anchorMax != center)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = center;
                rect.anchoredPosition = Vector2.zero;
            }
            rect.sizeDelta = size / s;
            rect.localScale = new Vector3(s, s, 1);
        }
    }
}
