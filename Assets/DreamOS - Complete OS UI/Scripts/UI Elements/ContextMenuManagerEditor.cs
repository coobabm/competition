#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(ContextMenuManager))]
    public class ContextMenuManagerEditor : Editor
    {
        private ContextMenuManager cmTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            cmTarget = (ContextMenuManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_ContextMenu");

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

            var menuPreset = serializedObject.FindProperty("menuPreset");
            var buttonPreset = serializedObject.FindProperty("buttonPreset");
            var separatorPreset = serializedObject.FindProperty("separatorPreset");
            var cameraSource = serializedObject.FindProperty("cameraSource");
            var targetCamera = serializedObject.FindProperty("targetCamera");
            var targetCanvas = serializedObject.FindProperty("targetCanvas");

            var vBorderTop = serializedObject.FindProperty("vBorderTop");
            var vBorderBottom = serializedObject.FindProperty("vBorderBottom");
            var hBorderLeft = serializedObject.FindProperty("hBorderLeft");
            var hBorderRight = serializedObject.FindProperty("hBorderRight");

            var enableBlur = serializedObject.FindProperty("enableBlur");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    DreamOSEditorHandler.DrawProperty(vBorderTop, customSkin, "Vertical Top");
                    DreamOSEditorHandler.DrawProperty(vBorderBottom, customSkin, "Vertical Bottom");
                    DreamOSEditorHandler.DrawProperty(hBorderLeft, customSkin, "Horizontal Left");
                    DreamOSEditorHandler.DrawProperty(hBorderRight, customSkin, "Horizontal Right");
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(targetCanvas, customSkin, "Target Canvas");
                    DreamOSEditorHandler.DrawProperty(menuPreset, customSkin, "Menu Preset");
                    DreamOSEditorHandler.DrawProperty(buttonPreset, customSkin, "Button Preset");
                    DreamOSEditorHandler.DrawProperty(separatorPreset, customSkin, "Seperator Preset");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    enableBlur.boolValue = DreamOSEditorHandler.DrawToggle(enableBlur.boolValue, customSkin, "Enable Blur");
                    DreamOSEditorHandler.DrawProperty(cameraSource, customSkin, "Camera Source");

                    if (cmTarget.cameraSource == ContextMenuManager.CameraSource.Custom)
                        DreamOSEditorHandler.DrawProperty(targetCamera, customSkin, "Target Camera");

                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif