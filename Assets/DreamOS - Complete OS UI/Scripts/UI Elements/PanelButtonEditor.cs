#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PanelButton))]
    public class PanelButtonEditor : Editor
    {
        private PanelButton buttonTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            buttonTarget = (PanelButton)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Button");

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

            var buttonIcon = serializedObject.FindProperty("buttonIcon");
            var buttonText = serializedObject.FindProperty("buttonText");

            var normalCG = serializedObject.FindProperty("normalCG");
            var highlightCG = serializedObject.FindProperty("highlightCG");
            var pressCG = serializedObject.FindProperty("pressCG");
            var selectCG = serializedObject.FindProperty("selectCG");
            var normalTextObj = serializedObject.FindProperty("normalTextObj");
            var highlightTextObj = serializedObject.FindProperty("highlightTextObj");
            var pressedTextObj = serializedObject.FindProperty("pressedTextObj");
            var selectedTextObj = serializedObject.FindProperty("selectedTextObj");
            var normalImageObj = serializedObject.FindProperty("normalImageObj");
            var highlightImageObj = serializedObject.FindProperty("highlightImageObj");
            var pressedImageObj = serializedObject.FindProperty("pressedImageObj");
            var selectedImageObj = serializedObject.FindProperty("selectedImageObj");

            var isInteractable = serializedObject.FindProperty("isInteractable");
            var isSelected = serializedObject.FindProperty("isSelected");
            var useLocalization = serializedObject.FindProperty("useLocalization");
            var useCustomText = serializedObject.FindProperty("useCustomText");
            var useSounds = serializedObject.FindProperty("useSounds");
            var useUINavigation = serializedObject.FindProperty("useUINavigation");
            var fadingMultiplier = serializedObject.FindProperty("fadingMultiplier");

            var onClick = serializedObject.FindProperty("onClick");
            var onHover = serializedObject.FindProperty("onHover");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    DreamOSEditorHandler.DrawPropertyCW(buttonIcon, customSkin, "Button Icon", 80);
                    if (useCustomText.boolValue == false) { DreamOSEditorHandler.DrawPropertyCW(buttonText, customSkin, "Button Text", 80); }
                    if (buttonTarget.buttonIcon != null || useCustomText.boolValue == false) { buttonTarget.UpdateUI(); }

                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onClick, new GUIContent("On Click"), true);
                    EditorGUILayout.PropertyField(onHover, new GUIContent("On Hover"), true);
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(normalCG, customSkin, "Normal CG");
                    DreamOSEditorHandler.DrawProperty(highlightCG, customSkin, "Highlight CG");
                    DreamOSEditorHandler.DrawProperty(pressCG, customSkin, "Press CG");
                    DreamOSEditorHandler.DrawProperty(selectCG, customSkin, "Select CG");
                    DreamOSEditorHandler.DrawProperty(normalTextObj, customSkin, "Normal Text");
                    DreamOSEditorHandler.DrawProperty(highlightTextObj, customSkin, "Highlight Text");
                    DreamOSEditorHandler.DrawProperty(pressedTextObj, customSkin, "Pressed Text");
                    DreamOSEditorHandler.DrawProperty(selectedTextObj, customSkin, "Selected Text");
                    DreamOSEditorHandler.DrawProperty(normalImageObj, customSkin, "Normal Image");
                    DreamOSEditorHandler.DrawProperty(highlightImageObj, customSkin, "Highlight Image");
                    DreamOSEditorHandler.DrawProperty(pressedImageObj, customSkin, "Pressed Image");
                    DreamOSEditorHandler.DrawProperty(selectedImageObj, customSkin, "Selected Image");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    isInteractable.boolValue = DreamOSEditorHandler.DrawToggle(isInteractable.boolValue, customSkin, "Is Interactable");
                    isSelected.boolValue = DreamOSEditorHandler.DrawToggle(isSelected.boolValue, customSkin, "Is Selected");
                    useLocalization.boolValue = DreamOSEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Bypasses localization functions when disabled.");
                    useCustomText.boolValue = DreamOSEditorHandler.DrawToggle(useCustomText.boolValue, customSkin, "Use Custom Text", "Bypasses inspector values and allows manual editing.");
                    useUINavigation.boolValue = DreamOSEditorHandler.DrawToggle(useUINavigation.boolValue, customSkin, "Use UI Navigation", "Enables controller navigation.");
                    useSounds.boolValue = DreamOSEditorHandler.DrawToggle(useSounds.boolValue, customSkin, "Use Button Sounds");
                    DreamOSEditorHandler.DrawProperty(fadingMultiplier, customSkin, "Fading Multiplier", "Set the animation fade multiplier.");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif