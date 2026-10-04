using Michsky.DreamOS;
using UnityEngine;

namespace HongmengOS.Aero2010
{
    /// <summary>Owns only shell geometry; DreamOS keeps window lifecycle and task button animation states.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class AeroTaskbarLayout : MonoBehaviour
    {
        [System.Serializable] public struct Entry
        {
            public TaskbarButton button;
            public WindowManager window;
            public RectTransform rect;
            public RectTransform icon;
            public CanvasGroup visibility;
            public CanvasGroup windowGroup;
            public AeroShellGraphic face;
        }
        public Entry[] entries;
        public float buttonWidth = 68;
        public float gap = 5;
        private RectTransform area;
        private void Awake() { area = transform as RectTransform; }
        private void LateUpdate()
        {
            if (area == null || entries == null) return;
            int count = 0, top = -1;
            WindowManager focus = null;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.window == null || !e.window.isOn) continue;
                count++;
                if (e.window.gameObject.activeInHierarchy && e.windowGroup != null && e.windowGroup.alpha > .5f && e.window.transform.GetSiblingIndex() > top)
                { focus = e.window; top = e.window.transform.GetSiblingIndex(); }
            }
            float width = Mathf.Clamp((area.rect.width - Mathf.Max(0, count - 1) * gap) / Mathf.Max(1, count), 24, buttonWidth);
            float x = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                bool visible = e.window != null && e.window.isOn;
                if (e.rect == null) continue;
                Vector2 size = new Vector2(visible ? width : 0, 48);
                Vector2 position = new Vector2(x, 0);
                if (e.rect.sizeDelta != size) e.rect.sizeDelta = size;
                if (e.rect.anchoredPosition != position) e.rect.anchoredPosition = position;
                if (e.visibility != null)
                {
                    e.visibility.alpha = visible ? 1 : 0;
                    e.visibility.interactable = visible;
                    e.visibility.blocksRaycasts = visible;
                }
                if (e.icon != null)
                {
                    Vector2 iconPosition = new Vector2(width * .5f, 0);
                    if (e.icon.anchoredPosition != iconPosition) e.icon.anchoredPosition = iconPosition;
                    Vector2 iconSize = Vector2.one * Mathf.Min(36, width - 10);
                    if (e.icon.sizeDelta != iconSize) e.icon.sizeDelta = iconSize;
                }
                if (e.face != null) e.face.Selected = visible && e.window == focus;
                if (visible) x += width + gap;
            }
        }
    }
}
