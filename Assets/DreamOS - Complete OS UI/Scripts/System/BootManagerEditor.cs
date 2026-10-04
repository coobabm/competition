#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(BootManager))]
    public class BootManagerEditor : Editor
    {
        private BootManager bootTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            bootTarget = (BootManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Boot");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Events");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            bootTarget.currentEditorTab = DreamOSEditorHandler.DrawTabs(bootTarget.currentEditorTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Events", "Events"), customSkin.FindStyle("Tab_Content")))
                bootTarget.currentEditorTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                bootTarget.currentEditorTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                bootTarget.currentEditorTab = 2;

            GUILayout.EndHorizontal();

            var bootAnimator = serializedObject.FindProperty("bootAnimator");
            var userManager = serializedObject.FindProperty("userManager");

            var onBootStart = serializedObject.FindProperty("onBootStart");
            var onBootEnd = serializedObject.FindProperty("onBootEnd");
            var onRebootStart = serializedObject.FindProperty("onRebootStart");
            var onRebootEnd = serializedObject.FindProperty("onRebootEnd");

            var bootOnEnable = serializedObject.FindProperty("bootOnEnable");
            var fadeFrameSkip = serializedObject.FindProperty("fadeFrameSkip");
            var bootTime = serializedObject.FindProperty("bootTime");
            var initTime = serializedObject.FindProperty("initTime");
            var fadeSpeed = serializedObject.FindProperty("fadeSpeed");

            switch (bootTarget.currentEditorTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 6);
                    EditorGUILayout.PropertyField(onBootStart, new GUIContent("On Boot Start"));
                    EditorGUILayout.PropertyField(onBootEnd, new GUIContent("On Boot End"));
                    EditorGUILayout.PropertyField(onRebootStart, new GUIContent("On Reboot Start"));
                    EditorGUILayout.PropertyField(onRebootEnd, new GUIContent("On Reboot End"));
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(bootAnimator, customSkin, "Boot Animator");
                    DreamOSEditorHandler.DrawProperty(userManager, customSkin, "User Manager");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    bootOnEnable.boolValue = DreamOSEditorHandler.DrawToggle(bootOnEnable.boolValue, customSkin, "Boot On Enable", "Start boot process on enable.");
                    fadeFrameSkip.boolValue = DreamOSEditorHandler.DrawToggle(fadeFrameSkip.boolValue, customSkin, "Fade Frame Skip", "Enable frame skip on the fade-in animation.");
                    DreamOSEditorHandler.DrawProperty(bootTime, customSkin, "Boot Duration", "Set the boot duration/time.");
                    DreamOSEditorHandler.DrawProperty(initTime, customSkin, "Init Duration", "Set the desktop initialization duration. This will eliminate freezing while activating the desktop from the lock screen.");
                    DreamOSEditorHandler.DrawProperty(fadeSpeed, customSkin, "Fade Speed", "Set the reboot/shutdown fade speed.");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif