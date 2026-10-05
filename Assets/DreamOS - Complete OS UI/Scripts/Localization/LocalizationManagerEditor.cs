#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LocalizationManager))]
    public class LocalizationManagerEditor : Editor
    {
        private LocalizationManager lmTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            lmTarget = (LocalizationManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var UIManagerAsset = serializedObject.FindProperty("UIManagerAsset");
            var languageSelectors = serializedObject.FindProperty("languageSelectors");

            var setLanguageOnAwake = serializedObject.FindProperty("setLanguageOnAwake");
            var updateItemsOnSet = serializedObject.FindProperty("updateItemsOnSet");
            var saveLanguageChanges = serializedObject.FindProperty("saveLanguageChanges");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            DreamOSEditorHandler.DrawProperty(UIManagerAsset, customSkin, "UI Manager");
            GUILayout.BeginVertical();
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(languageSelectors, new GUIContent("Language Selectors"), true);
            EditorGUI.indentLevel = 0;
            GUILayout.EndVertical();

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            setLanguageOnAwake.boolValue = DreamOSEditorHandler.DrawToggle(setLanguageOnAwake.boolValue, customSkin, "Set Language On Awake");
            updateItemsOnSet.boolValue = DreamOSEditorHandler.DrawToggle(updateItemsOnSet.boolValue, customSkin, "Update Items On Language Set");
            saveLanguageChanges.boolValue = DreamOSEditorHandler.DrawToggle(saveLanguageChanges.boolValue, customSkin, "Save Language Changes");
            LocalizationManager.enableLogs = DreamOSEditorHandler.DrawToggle(LocalizationManager.enableLogs, customSkin, "Enable Logs");

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif