#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(BSODManager))]
    public class BSODManagerEditor : Editor
    {
        private BSODManager bmTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            bmTarget = (BSODManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_BSOD");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var steps = serializedObject.FindProperty("steps");
            var BSODScreen = serializedObject.FindProperty("BSODScreen");
            var targetCanvas = serializedObject.FindProperty("targetCanvas");
            var onCrashStart = serializedObject.FindProperty("onCrashStart");
            var onCrashEnd = serializedObject.FindProperty("onCrashEnd");
            var progressText = serializedObject.FindProperty("progressText");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginHorizontal();
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(steps, new GUIContent("Steps"));

                    EditorGUI.indentLevel = 0;
                    GUILayout.EndHorizontal();

                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onCrashStart, new GUIContent("On Crash Start"), true);
                    EditorGUILayout.PropertyField(onCrashEnd, new GUIContent("On Crash End"), true);
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(BSODScreen, customSkin, "BSOD Screen");
                    DreamOSEditorHandler.DrawProperty(targetCanvas, customSkin, "Target Canvas");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    DreamOSEditorHandler.DrawProperty(progressText, customSkin, "Progress Text");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif