using LingGuangV05.Desktop.Story;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Opaque app surfaces keep lower windows and the desktop behind them.</summary>
    public sealed class XgBrainWindowGlass : MonoBehaviour
    {
        public const string BackingName = "Opaque App Backing";
        float next;

        void Start() { RefreshWindows(); }

        void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + .1f;
            RefreshWindows();
        }

        void RefreshWindows()
        {
            // Prepare inactive windows too, so their first open frame already has a solid base.
            foreach (var window in FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (window.transform.IsChildOf(transform.parent)) EnsureOpaque(window.windowContainer);
            var windows = transform.parent.Find("Apps & Windows");
            if (windows == null) return;
            foreach (Transform child in windows)
                if (child.Find("Title") != null && child.Find("Client") != null)
                    EnsureOpaque(child as RectTransform);
        }

        public static void EnsureOpaque(RectTransform frame)
        {
            if (frame == null || frame.Find(BackingName) != null) return;
            var plate = PrologueDesk.Rect(BackingName, frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            plate.gameObject.layer = frame.gameObject.layer;
            plate.SetAsFirstSibling();
            // A backing fills transparent gaps without fading text, changing photos, or
            // overriding the window's normal open/close CanvasGroup animation.
            PrologueDesk.Fill(plate, new Color32(18, 29, 43, 255), true);
            plate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }
    }
}
