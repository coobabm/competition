#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(WidgetManager))]
    public class WidgetManagerEditor : Editor
    {
        private WidgetManager wmTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            wmTarget = (WidgetManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_WidgetManager");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Network List");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Widget List", "Widget List"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var widgetItems = serializedObject.FindProperty("widgetItems");

            var libraryItem = serializedObject.FindProperty("libraryItem");
            var libraryParent = serializedObject.FindProperty("libraryParent");
            var widgetParent = serializedObject.FindProperty("widgetParent");

            var useLocalization = serializedObject.FindProperty("useLocalization");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(widgetItems, new GUIContent("Widget Items"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 10);
                    DreamOSEditorHandler.DrawProperty(libraryItem, customSkin, "Library Item");
                    DreamOSEditorHandler.DrawProperty(libraryParent, customSkin, "Library Parent");
                    DreamOSEditorHandler.DrawProperty(widgetParent, customSkin, "Widget Parent");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
                    useLocalization.boolValue = DreamOSEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Allow or disallow localization.");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif