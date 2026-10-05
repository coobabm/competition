#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(WindowManager))]
    public class WindowManagerEditor : Editor
    {
        private WindowManager wmTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            wmTarget = (WindowManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_WindowManager");

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

            var normalizeImage = serializedObject.FindProperty("normalizeImage");
            var fullscreenImage = serializedObject.FindProperty("fullscreenImage");
            var taskbarButton = serializedObject.FindProperty("taskbarButton");
            var windowDragger = serializedObject.FindProperty("windowDragger");
            var resizePreset = serializedObject.FindProperty("resizePreset");
            var navbarRect = serializedObject.FindProperty("navbarRect");

            var onOpen = serializedObject.FindProperty("onOpen");
            var onClose = serializedObject.FindProperty("onClose");
            var onMinimize = serializedObject.FindProperty("onMinimize");
            var onFullscreen = serializedObject.FindProperty("onFullscreen");

            var useBackgroundBlur = serializedObject.FindProperty("useBackgroundBlur");
            var disableAtStart = serializedObject.FindProperty("disableAtStart");
            var hasNavDrawer = serializedObject.FindProperty("hasNavDrawer");
            var allowGestures = serializedObject.FindProperty("allowGestures");
            var enableMobileMode = serializedObject.FindProperty("enableMobileMode");
            var windowContainer = serializedObject.FindProperty("windowContainer");
            var windowContent = serializedObject.FindProperty("windowContent");
            var defaultNavbarState = serializedObject.FindProperty("defaultNavbarState");
            var minNavbarWidth = serializedObject.FindProperty("minNavbarWidth");
            var maxNavbarWidth = serializedObject.FindProperty("maxNavbarWidth");
            var navbarCurveSpeed = serializedObject.FindProperty("navbarCurveSpeed");
            var navbarCurve = serializedObject.FindProperty("navbarCurve");
            var resizeAnchor = serializedObject.FindProperty("resizeAnchor");
            var minWindowSize = serializedObject.FindProperty("minWindowSize");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    if (wmTarget.GetComponent<CanvasGroup>().alpha == 0 && GUILayout.Button("Set Visible", customSkin.button)) { wmTarget.GetComponent<CanvasGroup>().alpha = 1; }
                    else if (wmTarget.GetComponent<CanvasGroup>().alpha == 1 && GUILayout.Button("Set Invisible", customSkin.button)) { wmTarget.GetComponent<CanvasGroup>().alpha = 0; }

                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onOpen, new GUIContent("On Open"), true);
                    EditorGUILayout.PropertyField(onClose, new GUIContent("On Close"), true);
                    EditorGUILayout.PropertyField(onMinimize, new GUIContent("On Minimize"), true);
                    EditorGUILayout.PropertyField(onFullscreen, new GUIContent("On Fullscreen"), true);
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(windowContainer, customSkin, "Window Container");

                    if (hasNavDrawer.boolValue == true)
                    {
                        DreamOSEditorHandler.DrawProperty(windowContent, customSkin, "Main Content");
                        DreamOSEditorHandler.DrawProperty(navbarRect, customSkin, "Navbar Panel");
                    }

                    DreamOSEditorHandler.DrawProperty(taskbarButton, customSkin, "Taskbar Button");

                    if (enableMobileMode.boolValue == false)
                    {
                        DreamOSEditorHandler.DrawProperty(normalizeImage, customSkin, "Normalize Image");
                        DreamOSEditorHandler.DrawProperty(fullscreenImage, customSkin, "Fullscreen Image");
                        DreamOSEditorHandler.DrawProperty(windowDragger, customSkin, "Window Dragger");
                        DreamOSEditorHandler.DrawProperty(resizePreset, customSkin, "Resize Preset");
                    }

                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    disableAtStart.boolValue = DreamOSEditorHandler.DrawToggle(disableAtStart.boolValue, customSkin, "Disable At Start", "Disable the window object at start.");
                    allowGestures.boolValue = DreamOSEditorHandler.DrawToggle(allowGestures.boolValue, customSkin, "Allow Gestures");
                    enableMobileMode.boolValue = DreamOSEditorHandler.DrawToggle(enableMobileMode.boolValue, customSkin, "Enable Mobile Mode");

                    if (enableMobileMode.boolValue == false)
                    {
                        useBackgroundBlur.boolValue = DreamOSEditorHandler.DrawToggle(useBackgroundBlur.boolValue, customSkin, "Use Background Blur");

                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Space(-3);
                        hasNavDrawer.boolValue = DreamOSEditorHandler.DrawTogglePlain(hasNavDrawer.boolValue, customSkin, "Use Nav Drawer");
                        GUILayout.Space(4);

                        if (hasNavDrawer.boolValue == true)
                        {
                            GUILayout.BeginHorizontal(EditorStyles.helpBox);

                            EditorGUILayout.LabelField(new GUIContent("Min Navbar Width"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                            minNavbarWidth.floatValue = EditorGUILayout.Slider(minNavbarWidth.floatValue, 1, maxNavbarWidth.floatValue - 1);

                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal(EditorStyles.helpBox);

                            EditorGUILayout.LabelField(new GUIContent("Max Navbar Width"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                            maxNavbarWidth.floatValue = EditorGUILayout.Slider(maxNavbarWidth.floatValue, minNavbarWidth.floatValue + 1, 1000);

                            GUILayout.EndHorizontal();

                            DreamOSEditorHandler.DrawPropertyCW(navbarCurveSpeed, customSkin, "Curve Speed", 120);
                            DreamOSEditorHandler.DrawPropertyCW(navbarCurve, customSkin, "Animation Curve", 120);
                            DreamOSEditorHandler.DrawPropertyCW(defaultNavbarState, customSkin, "Default State", 120);

                            if (wmTarget.navbarRect == null || wmTarget.windowContent == null)
                            {
                                EditorGUILayout.HelpBox("There are missing resources!", MessageType.Warning);

                                DreamOSEditorHandler.DrawProperty(windowContent, customSkin, "Main Content");
                                DreamOSEditorHandler.DrawProperty(navbarRect, customSkin, "Navbar Panel");
                            }
                        }

                        GUILayout.EndVertical();
                    }

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    if (Application.isPlaying) { GUI.enabled = false; }
                    DreamOSEditorHandler.DrawPropertyPlainCW(resizeAnchor, customSkin, "Resize Anchor", 120);
                    if (wmTarget.resizeAnchor != WindowManager.ResizeAnchor.Disabled) { DreamOSEditorHandler.DrawPropertyCW(minWindowSize, customSkin, "Min Window Size", 120); }
                    GUILayout.EndVertical();
                    GUI.enabled = true;

                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif