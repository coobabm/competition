using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Tints an Image or a TMP text with one of the UIManager's theme colors and gives texts the system font of the
    /// chosen weight. Updates every frame while the UIManager's dynamic update is on, otherwise once on Awake.
    /// Replaces DreamOS's UIManagerElement (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("LingGuang/OS/UI Manager/UI Manager Element")]
    public class UIManagerElement : MonoBehaviour
    {
        // Resources
        public UIManager themeManagerAsset;

        // Options
        public ObjectType objectType;
        public ColorType colorType = ColorType.Primary;
        public FontType fontType = FontType.Regular;
        public bool keepAlphaValue = false;
        public bool useCustomFont = false;
        public bool useCustomColor = false;

        Image imageObject;
        TextMeshProUGUI textObject;

        public enum ObjectType
        {
            Text,
            Image
        }

        public enum ColorType
        {
            WindowBackground,
            Background,
            Primary,
            Secondary,
            Accent,
            AccentReversed,
            Taskbar
        }

        public enum FontType
        {
            Thin,
            Light,
            Regular,
            Semibold,
            Bold
        }

        void Awake()
        {
            this.enabled = true;

            if (themeManagerAsset == null) { themeManagerAsset = Resources.Load<UIManager>("UI Manager/Desktop UI Manager"); }
            if (objectType == ObjectType.Image && imageObject == null) { imageObject = gameObject.GetComponent<Image>(); }
            else if (objectType == ObjectType.Text && textObject == null) { textObject = gameObject.GetComponent<TextMeshProUGUI>(); }
            if (!themeManagerAsset.enableDynamicUpdate) { UpdateElement(); this.enabled = false; }
        }

        void Update()
        {
            if (themeManagerAsset == null) { return; }
            if (themeManagerAsset.enableDynamicUpdate) { UpdateElement(); }
        }

        public void UpdateElement()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && objectType == ObjectType.Image && imageObject == null) { imageObject = gameObject.GetComponent<Image>(); }
            else if (!Application.isPlaying && objectType == ObjectType.Text && textObject == null) { textObject = gameObject.GetComponent<TextMeshProUGUI>(); }
#endif

            if (objectType == ObjectType.Image && imageObject != null)
            {
                // Images take the taskbar color too, but only when the alpha is not kept.
                ApplyThemeColor(imageObject, allowTaskbar: !keepAlphaValue);
            }
            else if (objectType == ObjectType.Text && textObject != null)
            {
                if (!useCustomColor) { ApplyThemeColor(textObject, allowTaskbar: false); }
                if (!useCustomFont) { ApplyThemeFont(); }
            }
        }

        void ApplyThemeColor(Graphic target, bool allowTaskbar)
        {
            if (!TryGetThemeColor(allowTaskbar, out Color themeColor)) { return; }

            target.color = keepAlphaValue ? new Color(themeColor.r, themeColor.g, themeColor.b, target.color.a) : themeColor;
        }

        // The theme color for colorType; the two accent types follow the selected theme. False when there is none.
        bool TryGetThemeColor(bool allowTaskbar, out Color color)
        {
            bool customTheme = themeManagerAsset.selectedTheme == UIManager.SelectedTheme.Custom;

            switch (colorType)
            {
                case ColorType.WindowBackground: color = themeManagerAsset.windowBGColorDark; return true;
                case ColorType.Background: color = themeManagerAsset.backgroundColorDark; return true;
                case ColorType.Primary: color = themeManagerAsset.primaryColorDark; return true;
                case ColorType.Secondary: color = themeManagerAsset.secondaryColorDark; return true;
                case ColorType.Taskbar: color = themeManagerAsset.taskBarColorDark; return allowTaskbar;
                case ColorType.Accent: color = customTheme ? themeManagerAsset.highlightedColorCustom : themeManagerAsset.highlightedColorDark; return true;
                case ColorType.AccentReversed: color = customTheme ? themeManagerAsset.highlightedColorSecondaryCustom : themeManagerAsset.highlightedColorSecondaryDark; return true;
            }

            color = default;
            return false;
        }

        void ApplyThemeFont()
        {
            switch (fontType)
            {
                case FontType.Thin: textObject.font = themeManagerAsset.systemFontThin; break;
                case FontType.Light: textObject.font = themeManagerAsset.systemFontLight; break;
                case FontType.Regular: textObject.font = themeManagerAsset.systemFontRegular; break;
                case FontType.Semibold: textObject.font = themeManagerAsset.systemFontSemiBold; break;
                case FontType.Bold: textObject.font = themeManagerAsset.systemFontBold; break;
            }
        }
    }
}
