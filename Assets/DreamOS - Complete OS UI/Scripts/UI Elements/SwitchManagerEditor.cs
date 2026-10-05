#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(SwitchManager))]
    public class SwitchManagerEditor : Editor
    {
        private GUISkin customSkin;
        private SwitchManager switchTarget;
        private int currentTab;

        private void OnEnable()
        {
            switchTarget = (SwitchManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Switch");

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

            var onValueChanged = serializedObject.FindProperty("onValueChanged");
            var onEvents = serializedObject.FindProperty("onEvents");
            var offEvents = serializedObject.FindProperty("offEvents");

            var switchAnimator = serializedObject.FindProperty("switchAnimator");
            var highlightCG = serializedObject.FindProperty("highlightCG");

            var fadingMultiplier = serializedObject.FindProperty("fadingMultiplier");
            var isOn = serializedObject.FindProperty("isOn");
            var isInteractable = serializedObject.FindProperty("isInteractable");
            var invokeAtStart = serializedObject.FindProperty("invokeAtStart");
            var useEventTrigger = serializedObject.FindProperty("useEventTrigger");
            var useSounds = serializedObject.FindProperty("useSounds");
            var useUINavigation = serializedObject.FindProperty("useUINavigation");
            var saveValue = serializedObject.FindProperty("saveValue");
            var saveKey = serializedObject.FindProperty("saveKey");
            var dataCategory = serializedObject.FindProperty("dataCategory");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    isOn.boolValue = DreamOSEditorHandler.DrawToggle(isOn.boolValue, customSkin, "Is On");
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    saveValue.boolValue = DreamOSEditorHandler.DrawTogglePlain(saveValue.boolValue, customSkin, "Save Value");
                    GUILayout.Space(3);

                    if (saveValue.boolValue == true)
                    {
                        DreamOSEditorHandler.DrawPropertyPlainCW(saveKey, customSkin, "Save Key:", 70);
                        EditorGUILayout.HelpBox("Each switch should has its own unique key.", MessageType.Info);
                    }

                    GUILayout.EndVertical();

                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onValueChanged, new GUIContent("On Value Changed"), true);
                    EditorGUILayout.PropertyField(onEvents, new GUIContent("On Events"), true);
                    EditorGUILayout.PropertyField(offEvents, new GUIContent("Off Events"), true);
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(switchAnimator, customSkin, "Switch hAnimator");
                    DreamOSEditorHandler.DrawProperty(highlightCG, customSkin, "Highlight CG");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    isOn.boolValue = DreamOSEditorHandler.DrawToggle(isOn.boolValue, customSkin, "Is On");
                    isInteractable.boolValue = DreamOSEditorHandler.DrawToggle(isInteractable.boolValue, customSkin, "Is Interactable");
                    invokeAtStart.boolValue = DreamOSEditorHandler.DrawToggle(invokeAtStart.boolValue, customSkin, "Invoke At Start", "Process events on awake.");
                    useEventTrigger.boolValue = DreamOSEditorHandler.DrawToggle(useEventTrigger.boolValue, customSkin, "Use Event Trigger", "Eliminates input issues when combining the object with others. Scrolling is disabled when this option is enabled.");
                    useSounds.boolValue = DreamOSEditorHandler.DrawToggle(useSounds.boolValue, customSkin, "Use Switch Sounds");
                    useUINavigation.boolValue = DreamOSEditorHandler.DrawToggle(useUINavigation.boolValue, customSkin, "Use UI Navigation", "Enables controller navigation.");

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    saveValue.boolValue = DreamOSEditorHandler.DrawTogglePlain(saveValue.boolValue, customSkin, "Save Value");
                    GUILayout.Space(3);

                    if (saveValue.boolValue == true)
                    {
                        DreamOSEditorHandler.DrawPropertyCW(saveKey, customSkin, "Save Key", 100);
                        DreamOSEditorHandler.DrawPropertyCW(dataCategory, customSkin, "Data Category", 100);
                        EditorGUILayout.HelpBox("Each switch should has its own unique key.", MessageType.Info);
                    }

                    GUILayout.EndVertical();

                    DreamOSEditorHandler.DrawProperty(fadingMultiplier, customSkin, "Fading Multiplier");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif