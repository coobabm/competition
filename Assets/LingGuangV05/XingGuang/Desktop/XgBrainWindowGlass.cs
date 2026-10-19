using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Opaque app surfaces keep lower windows and the desktop behind them.</summary>
    public sealed class XgBrainWindowGlass : MonoBehaviour
    {
        public const string BackingName = "Opaque App Backing";
        float next;
        Transform appWindows;
        int childCount = -1;

        void Start() { appWindows = transform.parent.Find("Apps & Windows"); RefreshWindows(); }

        void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + .5f;
            if (appWindows == null) appWindows = transform.parent.Find("Apps & Windows");
            if (appWindows == null || appWindows.childCount == childCount) return;
            RefreshWindows();
        }

        void RefreshWindows()
        {
            // Prepare inactive windows too, so their first open frame already has a solid base.
            foreach (var window in transform.parent.GetComponentsInChildren<WindowManager>(true))
                EnsureOpaque(window.windowContainer);
            var windows = appWindows;
            if (windows == null) return;
            childCount = windows.childCount;
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
