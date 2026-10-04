using UnityEngine;
using TMPro;

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/UI Manager/UI Manager Font Changer")]
    public class UIManagerFontChanger : MonoBehaviour
    {
        [Header("Resources")]
        public UIManager targetUIManager;

        [Header("Fonts")]
        public TMP_FontAsset thinFont;
        public TMP_FontAsset lightFont;
        public TMP_FontAsset regularFont;
        public TMP_FontAsset semiboldFont;
        public TMP_FontAsset boldFont;

        [Header("Settings")]
        [SerializeField] private bool applyOnStart;

        void Start()
        {
            if (applyOnStart)
            {
                ApplyFonts();
            }
        }

        public void ApplyFonts()
        {
            if (targetUIManager == null)
            {
                Debug.LogError("Cannot apply the changes due to missing 'Target UI Manager'.", this);
                return;
            }

            if (thinFont != null) { targetUIManager.systemFontThin = thinFont; }
            if (lightFont != null) { targetUIManager.systemFontLight = lightFont; }
            if (regularFont != null) { targetUIManager.systemFontRegular = regularFont; }
            if (semiboldFont != null) { targetUIManager.systemFontSemiBold = semiboldFont; }
            if (boldFont != null) { targetUIManager.systemFontBold = boldFont; }

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