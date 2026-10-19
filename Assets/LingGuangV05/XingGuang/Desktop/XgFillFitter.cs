using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// A ContentSizeFitter for a scroll content that is never shorter than its viewport: the content is its layout's
    /// preferred height, or the viewport's height when that is more, so a flexible child (the 装备 card) takes the space
    /// that is left and the column only scrolls when its modules need more than the viewport.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class XgFillFitter : UIBehaviour, ILayoutSelfController
    {
        public RectTransform viewport;
        RectTransform rect;
        RectTransform Rect => rect != null ? rect : rect = (RectTransform)transform;

        public void SetLayoutHorizontal() { }

        public void SetLayoutVertical()
        {
            float h = LayoutUtility.GetPreferredHeight(Rect);
            if (viewport != null) h = Mathf.Max(h, viewport.rect.height);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        }

        public void SetDirty() { if (IsActive()) LayoutRebuilder.MarkLayoutForRebuild(Rect); }
        protected override void OnEnable() { base.OnEnable(); SetDirty(); }
        protected override void OnDisable() { LayoutRebuilder.MarkLayoutForRebuild(Rect); base.OnDisable(); }
    }

    /// <summary>Tells a <see cref="XgFillFitter"/> when its viewport changes size (the window was resized).</summary>
    public sealed class XgFillViewport : UIBehaviour
    {
        public XgFillFitter fitter;
        protected override void OnRectTransformDimensionsChange() { if (fitter != null) fitter.SetDirty(); }
    }
}
