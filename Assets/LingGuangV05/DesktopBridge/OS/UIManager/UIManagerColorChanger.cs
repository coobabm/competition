using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Pushes a set of colors into a <see cref="UIManager"/> asset (optionally on Start), so the themed elements pick
    /// them up. If dynamic updating is off, it is turned on for one second so the elements get to refresh. Replaces
    /// DreamOS's UIManagerColorChanger (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [AddComponentMenu("LingGuang/OS/UI Manager/UI Manager Color Changer")]
    public class UIManagerColorChanger : MonoBehaviour
    {
        [Header("Resources")]
        public UIManager targetUIManager;

        [Header("Colors")]
        public Color accent = new Color32(0, 150, 255, 255);
        public Color accentMatch = new Color32(255, 255, 255, 255);
        public Color primary = new Color32(255, 255, 255, 255);
        public Color secondary = new Color32(25, 35, 45, 255);
        public Color windowBackground = new Color32(20, 30, 40, 240);
        public Color background = new Color32(30, 40, 50, 200);
        public Color taskbar = new Color32(25, 35, 45, 205);

        [Header("Settings")]
        [SerializeField] private bool applyOnStart;

        void Start()
        {
            if (applyOnStart) { ApplyColors(); }
        }

        public void ApplyColors()
        {
            if (targetUIManager == null)
            {
                Debug.LogError("Cannot apply the changes due to missing 'Target UI Manager'.", this);
                return;
            }

            targetUIManager.highlightedColorDark = accent;
            targetUIManager.highlightedColorSecondaryDark = accentMatch;
            targetUIManager.primaryColorDark = primary;
            targetUIManager.secondaryColorDark = secondary;
            targetUIManager.windowBGColorDark = windowBackground;
            targetUIManager.backgroundColorDark = background;
            targetUIManager.taskBarColorDark = taskbar;

            if (!targetUIManager.enableDynamicUpdate)
            {
                targetUIManager.enableDynamicUpdate = true;
                Invoke(nameof(DisableDynamicUpdate), 1);
            }
        }

        void DisableDynamicUpdate()
        {
            targetUIManager.enableDynamicUpdate = false;
        }
    }
}
