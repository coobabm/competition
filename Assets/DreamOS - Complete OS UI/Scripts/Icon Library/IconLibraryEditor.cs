#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(IconLibrary))]
    public class IconLibraryEditor : Editor
    {
        private GUISkin customSkin;

        void OnEnable()
        {
            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            // Tab_Settings
            var alwaysUpdate = serializedObject.FindProperty("alwaysUpdate");
            var optimizeUpdates = serializedObject.FindProperty("optimizeUpdates");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 8);
            alwaysUpdate.boolValue = DreamOSEditorHandler.DrawToggle(alwaysUpdate.boolValue, customSkin, "Always Update");
            optimizeUpdates.boolValue = DreamOSEditorHandler.DrawToggle(optimizeUpdates.boolValue, customSkin, "Optimize Update");

            // Content
            var icons = serializedObject.FindProperty("icons");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 8);
            GUILayout.BeginVertical();
            EditorGUI.indentLevel = 1;

            EditorGUILayout.PropertyField(icons, new GUIContent("Icon List"), true);
            icons.isExpanded = true;

            EditorGUI.indentLevel = 0;
            GUILayout.EndVertical();

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif