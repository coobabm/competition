using UnityEngine;

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/UI Manager/UI Manager Color Changer")]
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
            if (applyOnStart)
            {
                ApplyColors();
            }
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
                Invoke("DisableDynamicUpdate", 1);
            }
        }

        void DisableDynamicUpdate()
        {
            targetUIManager.enableDynamicUpdate = false;
        }
    }
}