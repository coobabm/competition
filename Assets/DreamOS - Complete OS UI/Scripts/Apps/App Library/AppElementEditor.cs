#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(AppElement))]
    public class AppElementEditor : Editor
    {
        private AppElement aeTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            aeTarget = (AppElement)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var appLibrary = serializedObject.FindProperty("appLibrary");
            var siblings = serializedObject.FindProperty("siblings");
            var appID = serializedObject.FindProperty("appID");
            var elementType = serializedObject.FindProperty("elementType");
            var iconSize = serializedObject.FindProperty("iconSize");
            var useGradient = serializedObject.FindProperty("useGradient");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            DreamOSEditorHandler.DrawProperty(appLibrary, customSkin, "App Library");
            if (aeTarget.appLibrary != null)
            {
                GUILayout.BeginHorizontal();
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(siblings, new GUIContent("Siblings"), true);
                EditorGUI.indentLevel = 0;
                GUILayout.EndHorizontal();
            }

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);

            if (aeTarget.appLibrary != null)
            {
                DreamOSEditorHandler.DrawProperty(appID, customSkin, "App ID");

                if (aeTarget.tempAppIndex >= aeTarget.appLibrary.apps.Count
                    || aeTarget.appLibrary.apps[aeTarget.tempAppIndex].appTitle != aeTarget.appID)
                {
                    EditorGUILayout.HelpBox("App ID cannot be found in the library.", MessageType.Error);

                    if (GUILayout.Button("Update"))
                    {
                        aeTarget.UpdateLibrary();
                        aeTarget.UpdateElement();
                    }
                }

                else
                {
                    DreamOSEditorHandler.DrawProperty(elementType, customSkin, "Element Type");

                    if (elementType.enumValueIndex == 1)
                    {
                        DreamOSEditorHandler.DrawProperty(iconSize, customSkin, "Icon Size");
                        useGradient.boolValue = DreamOSEditorHandler.DrawToggle(useGradient.boolValue, customSkin, "Use Gradient");
                    }
                }
            }

            else { EditorGUILayout.HelpBox("App Library is missing.", MessageType.Error); }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif