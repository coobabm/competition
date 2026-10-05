using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Michsky.DreamOS
{
    [ExecuteInEditMode]
    [CreateAssetMenu(fileName = "New UI Manager Manager", menuName = "DreamOS/New UI Manager")]
    public class UIManager : ScriptableObject
    {
        public static string buildID = "R201-240131";

        // Tab_Settings
        [HideInInspector] public bool enableDynamicUpdate = true;
        [HideInInspector] public bool enableExtendedColorPicker = true;
        [HideInInspector] public bool editorHints = true;
        public SelectedTheme selectedTheme;

        // Audio
        public AudioClip hoverSound;
        public AudioClip clickSound;
        public AudioClip errorSound;
        public AudioClip notificationSound;
        public bool enableKeystrokes = true;
        public bool enableKeyboardKeystroke = true;
        public bool enableMouseKeystroke = true;
        public List<AudioClip> keyboardStrokes = new List<AudioClip>();
        public List<AudioClip> mouseStrokes = new List<AudioClip>();

        // System Theme Vars
        public Color highlightedColorDark = new Color(255, 255, 255, 255);
        public Color highlightedColorSecondaryDark = new Color(255, 255, 255, 255);
        public Color primaryColorDark = new Color(255, 255, 255, 255);
        public Color secondaryColorDark = new Color(255, 255, 255, 255);
        public Color windowBGColorDark = new Color(255, 255, 255, 255);
        public Color backgroundColorDark = new Color(255, 255, 255, 255);
        public Color taskBarColorDark = new Color(255, 255, 255, 255);

        // Custom Theme Vars
        public Color highlightedColorCustom = new Color(255, 255, 255, 255);
        public Color highlightedColorSecondaryCustom = new Color(255, 255, 255, 255);

        // Effects
        public bool enableUIBlur = true;

        // Fonts
        public TMP_FontAsset systemFontThin;
        public TMP_FontAsset systemFontLight;
        public TMP_FontAsset systemFontRegular;
        public TMP_FontAsset systemFontSemiBold;
        public TMP_FontAsset systemFontBold;

        // Localization
        public bool enableLocalization;
        public LocalizationSettings localizationSettings;
        public LocalizationLanguage currentLanguage;
        public static bool isLocalizationEnabled = false;

        public enum SelectedTheme
        {
            Default,
            Custom
        }
    }
}