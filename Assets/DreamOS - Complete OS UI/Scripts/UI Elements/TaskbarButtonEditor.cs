#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TaskbarButton))]
    public class TaskbarButtonEditor : Editor
    {
        private TaskbarButton buttonTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            buttonTarget = (TaskbarButton)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Button");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Settings");
            toolbarTabs[1] = new GUIContent("Resources");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var buttonTitle = serializedObject.FindProperty("buttonTitle");
            var defaultPinState = serializedObject.FindProperty("defaultPinState");
            var openWindowsOnly = serializedObject.FindProperty("openWindowsOnly");

            var appElements = serializedObject.FindProperty("appElements");
            var buttonAnimator = serializedObject.FindProperty("buttonAnimator");
            var windowManager = serializedObject.FindProperty("windowManager");
            var contextMenu = serializedObject.FindProperty("contextMenu");
            var contextBlur = serializedObject.FindProperty("contextBlur");
            var headerButton = serializedObject.FindProperty("headerButton");
            var closeButton = serializedObject.FindProperty("closeButton");
            var pinButton = serializedObject.FindProperty("pinButton");
            var unpinButton = serializedObject.FindProperty("unpinButton");

            var onClick = serializedObject.FindProperty("onClick");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    DreamOSEditorHandler.DrawProperty(buttonTitle, customSkin, "App Title");
                    DreamOSEditorHandler.DrawProperty(openWindowsOnly, customSkin, "Open Windows Only");
                    if (!openWindowsOnly.boolValue)
                        DreamOSEditorHandler.DrawProperty(defaultPinState, customSkin, "Default Pin State");

                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onClick, new GUIContent("On Click"), true);
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(buttonAnimator, customSkin, "Button Animator");
                    DreamOSEditorHandler.DrawProperty(windowManager, customSkin, "Window Manager");
                    DreamOSEditorHandler.DrawProperty(contextMenu, customSkin, "Context Menu");
                    DreamOSEditorHandler.DrawProperty(contextBlur, customSkin, "Context Blur");
                    DreamOSEditorHandler.DrawProperty(headerButton, customSkin, "Header Button");
                    DreamOSEditorHandler.DrawProperty(closeButton, customSkin, "Close Button");
                    DreamOSEditorHandler.DrawProperty(pinButton, customSkin, "Pin Button");
                    DreamOSEditorHandler.DrawProperty(unpinButton, customSkin, "Unpin Button");
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(appElements, new GUIContent("App Elements"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndHorizontal();
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif