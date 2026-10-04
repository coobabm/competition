#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine.Rendering;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(UIManager))]
    [System.Serializable]
    public class UIManagerEditor : Editor
    {
        GUISkin customSkin;
        private UIManager uimTarget;

        protected static float foldoutItemSpace = 2;
        protected static float foldoutTopSpace = 5;
        protected static float foldoutBottomSpace = 2;

        protected static bool showAudio = false;
        protected static bool showColors = false;
        protected static bool showEffects = false;
        protected static bool showFonts = false;
        protected static bool showLocalization = false;

        void OnEnable()
        {
            uimTarget = (UIManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            if (customSkin == null)
            {
                EditorGUILayout.HelpBox("Editor variables are missing. You can manually fix this by deleting the " +
                    "'DreamOS > Resources > Editor' folder and then re-import the package. \n\nIf you're still seeing this " +
                    "dialog even after the re-import, contact me with this ID: " + UIManager.buildID, MessageType.Error);

                if (GUILayout.Button("Contact")) { Email(); }
                return;
            }

            // Foldout style
            GUIStyle foldoutStyle = customSkin.FindStyle("Foldout");

            // Header_UIM
            DreamOSEditorHandler.DrawHeader(customSkin, "Header_UIM", 8);

            #region Audio
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(foldoutTopSpace);
            GUILayout.BeginHorizontal();
            showAudio = EditorGUILayout.Foldout(showAudio, "Audio", true, foldoutStyle);
            showAudio = GUILayout.Toggle(showAudio, new GUIContent(""), customSkin.FindStyle("ToggleHelper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(foldoutBottomSpace);

            if (showAudio)
            {
                var hoverSound = serializedObject.FindProperty("hoverSound");
                var clickSound = serializedObject.FindProperty("clickSound");
                var errorSound = serializedObject.FindProperty("errorSound");
                var notificationSound = serializedObject.FindProperty("notificationSound");
                var enableKeystrokes = serializedObject.FindProperty("enableKeystrokes");
                var enableKeyboardKeystroke = serializedObject.FindProperty("enableKeyboardKeystroke");
                var enableMouseKeystroke = serializedObject.FindProperty("enableMouseKeystroke");
                var keyboardStrokes = serializedObject.FindProperty("keyboardStrokes");
                var mouseStrokes = serializedObject.FindProperty("mouseStrokes");

                DreamOSEditorHandler.DrawProperty(hoverSound, customSkin, "Hover Sound");
                DreamOSEditorHandler.DrawProperty(clickSound, customSkin, "Click Sound");
                DreamOSEditorHandler.DrawProperty(errorSound, customSkin, "Error Sound");
                DreamOSEditorHandler.DrawProperty(notificationSound, customSkin, "Notification Sound");

                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Space(-3);
                enableKeystrokes.boolValue = DreamOSEditorHandler.DrawTogglePlain(enableKeystrokes.boolValue, customSkin, "Enable Keystrokes");
                GUILayout.Space(4);

                if (enableKeystrokes.boolValue == true)
                {
                    enableKeyboardKeystroke.boolValue = DreamOSEditorHandler.DrawToggle(enableKeyboardKeystroke.boolValue, customSkin, "Enable Keyboard");
                    enableMouseKeystroke.boolValue = DreamOSEditorHandler.DrawToggle(enableMouseKeystroke.boolValue, customSkin, "Enable Mouse");
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(keyboardStrokes, new GUIContent("Keyboard Strokes"), true);
                    EditorGUILayout.PropertyField(mouseStrokes, new GUIContent("Mouse Strokes"), true);
                    EditorGUI.indentLevel = 0;
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
            GUILayout.Space(foldoutItemSpace);
            #endregion

            #region Colors
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(foldoutTopSpace);
            GUILayout.BeginHorizontal();
            showColors = EditorGUILayout.Foldout(showColors, "Colors", true, foldoutStyle);
            showColors = GUILayout.Toggle(showColors, new GUIContent(""), customSkin.FindStyle("ToggleHelper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(foldoutBottomSpace);

            if (showColors)
            {
                var windowBGColorDark = serializedObject.FindProperty("windowBGColorDark");
                var backgroundColorDark = serializedObject.FindProperty("backgroundColorDark");
                var primaryColorDark = serializedObject.FindProperty("primaryColorDark");
                var secondaryColorDark = serializedObject.FindProperty("secondaryColorDark");
                var highlightedColorDark = serializedObject.FindProperty("highlightedColorDark");
                var highlightedColorSecondaryDark = serializedObject.FindProperty("highlightedColorSecondaryDark");
                var taskBarColorDark = serializedObject.FindProperty("taskBarColorDark");
                var highlightedColorCustom = serializedObject.FindProperty("highlightedColorCustom");
                var highlightedColorSecondaryCustom = serializedObject.FindProperty("highlightedColorSecondaryCustom");

                DreamOSEditorHandler.DrawProperty(highlightedColorDark, customSkin, "Accent Color");
                DreamOSEditorHandler.DrawProperty(highlightedColorSecondaryDark, customSkin, "Accent Reversed");
                DreamOSEditorHandler.DrawProperty(primaryColorDark, customSkin, "Primary Color");
                DreamOSEditorHandler.DrawProperty(secondaryColorDark, customSkin, "Secondary Color");
                DreamOSEditorHandler.DrawProperty(backgroundColorDark, customSkin, "Background Color");
                DreamOSEditorHandler.DrawProperty(windowBGColorDark, customSkin, "Window BG Color");
                DreamOSEditorHandler.DrawProperty(taskBarColorDark, customSkin, "Taskbar Color");

                GUILayout.Space(12);
                GUILayout.Label("Custom Theme", EditorStyles.boldLabel);
                DreamOSEditorHandler.DrawProperty(highlightedColorCustom, customSkin, "Accent Color");
                DreamOSEditorHandler.DrawProperty(highlightedColorSecondaryCustom, customSkin, "Accent Reversed");
            }

            GUILayout.EndVertical();
            GUILayout.Space(foldoutItemSpace);
            #endregion

            #region Effects
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(foldoutTopSpace);
            GUILayout.BeginHorizontal();
            showEffects = EditorGUILayout.Foldout(showEffects, "Effects", true, foldoutStyle);
            showEffects = GUILayout.Toggle(showEffects, new GUIContent(""), customSkin.FindStyle("ToggleHelper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(foldoutBottomSpace);

            if (showEffects)
            {
                var enableUIBlur = serializedObject.FindProperty("enableUIBlur");

                if (GraphicsSettings.defaultRenderPipeline == null) { GUI.enabled = true; }
                else { GUI.enabled = false; }

                enableUIBlur.boolValue = DreamOSEditorHandler.DrawToggle(enableUIBlur.boolValue, customSkin, "Enable UI Blur", "This feature works with the built-in pipeline only.");
           
                GUI.enabled = true;
            }

            GUILayout.EndVertical();
            GUILayout.Space(foldoutItemSpace);
            #endregion

            #region Fonts
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(foldoutTopSpace);
            GUILayout.BeginHorizontal();
            showFonts = EditorGUILayout.Foldout(showFonts, "Fonts", true, foldoutStyle);
            showFonts = GUILayout.Toggle(showFonts, new GUIContent(""), customSkin.FindStyle("ToggleHelper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(foldoutBottomSpace);

            if (showFonts)
            {
                var systemFontThin = serializedObject.FindProperty("systemFontThin");
                var systemFontLight = serializedObject.FindProperty("systemFontLight");
                var systemFontRegular = serializedObject.FindProperty("systemFontRegular");
                var systemFontSemiBold = serializedObject.FindProperty("systemFontSemiBold");
                var systemFontBold = serializedObject.FindProperty("systemFontBold");

                DreamOSEditorHandler.DrawProperty(systemFontThin, customSkin, "Font Thin");
                DreamOSEditorHandler.DrawProperty(systemFontLight, customSkin, "Font Light");
                DreamOSEditorHandler.DrawProperty(systemFontRegular, customSkin, "Font Regular");
                DreamOSEditorHandler.DrawProperty(systemFontSemiBold, customSkin, "Font Semibold");
                DreamOSEditorHandler.DrawProperty(systemFontBold, customSkin, "Font Bold");
            }

            GUILayout.EndVertical();
            GUILayout.Space(foldoutItemSpace);
            #endregion

            #region Localization
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(foldoutTopSpace);
            GUILayout.BeginHorizontal();
            showLocalization = EditorGUILayout.Foldout(showLocalization, "Localization", true, foldoutStyle);
            showLocalization = GUILayout.Toggle(showLocalization, new GUIContent(""), customSkin.FindStyle("ToggleHelper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(foldoutBottomSpace);

            if (showLocalization)
            {
                var enableLocalization = serializedObject.FindProperty("enableLocalization");
                var localizationSettings = serializedObject.FindProperty("localizationSettings");

                enableLocalization.boolValue = DreamOSEditorHandler.DrawToggle(enableLocalization.boolValue, customSkin, "Enable Localization (Beta)");

                if (enableLocalization.boolValue == true)
                {
                    DreamOSEditorHandler.DrawPropertyCW(localizationSettings, customSkin, "Localization Settings", 130);

                    if (uimTarget.localizationSettings != null)
                    {
                        if (GUILayout.Button("Open LocalizationSettings", customSkin.button)) { Selection.activeObject = uimTarget.localizationSettings; }
                        EditorGUILayout.HelpBox("Localization is enabled. You can use the Localization Settings asset to manage localization.", MessageType.Info);
                    }
                    else { EditorGUILayout.HelpBox("Localization is enabled, but 'LocalizationSettings' is missing.", MessageType.Warning); }
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(foldoutItemSpace);
            #endregion

            #region Settings
            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 14);

            var enableDynamicUpdate = serializedObject.FindProperty("enableDynamicUpdate");
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(-2);
            GUILayout.BeginHorizontal();
            enableDynamicUpdate.boolValue = GUILayout.Toggle(enableDynamicUpdate.boolValue, new GUIContent("Enable Dynamic Update"), customSkin.FindStyle("Toggle"));
            enableDynamicUpdate.boolValue = GUILayout.Toggle(enableDynamicUpdate.boolValue, new GUIContent(""), customSkin.FindStyle("ToggleHelper"));
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (enableDynamicUpdate.boolValue == true)
            {
                EditorGUILayout.HelpBox("When this option is enabled, all objects connected to this manager will be dynamically updated synchronously. " +
                    "Basically; consumes more resources, but allows dynamic changes at runtime/editor.", MessageType.Info);
            }

            else
            {
                EditorGUILayout.HelpBox("When this option is disabled, all objects connected to this manager will be updated only once on awake. " +
                    "Basically; has better performance, but it's static.", MessageType.Info);
            }

            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to defaults", customSkin.button)) { ResetToDefaults(); }
            GUILayout.EndHorizontal();
            #endregion

            #region Add-ons
            GUILayout.Space(14);
            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("Header_AddOn"));
            GUILayout.BeginVertical();
            if (GUILayout.Button("90s Desktop", customSkin.button)) { Application.OpenURL("https://u3d.as/3aqZ"); }
            if (GUILayout.Button("Steam Messaging", customSkin.button)) { Application.OpenURL("https://u3d.as/2QTF"); }
            GUILayout.EndVertical();
            #endregion

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();

            #region Support
            GUILayout.Space(14);
            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("Header_Support"));
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Need help? Contact me via:", customSkin.FindStyle("Text"));
            GUILayout.EndHorizontal();       
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Documentation", customSkin.button)) { Docs(); }
            if (GUILayout.Button("Support", customSkin.button)) { Email(); }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("ID: " + UIManager.buildID);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            #endregion
        }

        void Docs() { Application.OpenURL("https://docs.michsky.com/docs/dreamos/"); }
        void Email() { Application.OpenURL("https://www.michsky.com/contact/"); }

        void ResetToDefaults()
        {
            if (EditorUtility.DisplayDialog("Reset to defaults", "Are you sure you want to reset UI Manager values to default?", "Yes", "Cancel"))
            {
                try
                {
                    if (GraphicsSettings.defaultRenderPipeline != null)
                    {
                        EditorPrefs.SetInt("DreamOS.SRPChecker", 1);

                        Preset defaultPreset = Resources.Load<Preset>("UI Manager/DreamOS UIM Presets/SRP Default");
                        defaultPreset.ApplyTo(Resources.Load("UI Manager/DreamOS UI Manager"));
                    }

                    else
                    {
                        Preset defaultPreset = Resources.Load<Preset>("UI Manager/DreamOS UIM Presets/Default");
                        defaultPreset.ApplyTo(Resources.Load("UI Manager/DreamOS UI Manager"));
                    }

                    Selection.activeObject = null;
                    Selection.activeObject = Resources.Load("UI Manager/DreamOS UI Manager");

                    Debug.Log("<b>[DreamOS UI Manager]</b> Resetting successful.");
                }

                catch { Debug.LogWarning("<b>[UI Manager]</b> Resetting failed."); }
            }
        }
    }
}
#endif