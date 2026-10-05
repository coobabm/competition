#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(CommanderManager))]
    public class CommanderManagerEditor : Editor
    {
        private CommanderManager cTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            cTarget = (CommanderManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Commander");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Playlists", "Playlists"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var commands = serializedObject.FindProperty("commands");
           
            var commandInput = serializedObject.FindProperty("commandInput");
            var commandHistory = serializedObject.FindProperty("commandHistory");
            var scrollbar = serializedObject.FindProperty("scrollbar");

            var textColor = serializedObject.FindProperty("textColor");
            var timeColor = serializedObject.FindProperty("timeColor");
            var helpCommand = serializedObject.FindProperty("helpCommand");
            var errorText = serializedObject.FindProperty("errorText");
            var onEnableText = serializedObject.FindProperty("onEnableText");
            var enableHelpCommand = serializedObject.FindProperty("enableHelpCommand");
            var useTypewriterEffect = serializedObject.FindProperty("useTypewriterEffect");
            var typewriterDelay = serializedObject.FindProperty("typewriterDelay");
            var antiFlicker = serializedObject.FindProperty("antiFlicker");
            var getTimeData = serializedObject.FindProperty("getTimeData");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(commands, new GUIContent("Commands"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    DreamOSEditorHandler.DrawProperty(commandInput, customSkin, "Command Input");
                    DreamOSEditorHandler.DrawProperty(commandHistory, customSkin, "Command History");
                    DreamOSEditorHandler.DrawProperty(scrollbar, customSkin, "Scrollbar");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    DreamOSEditorHandler.DrawProperty(textColor, customSkin, "Text Color");
                    DreamOSEditorHandler.DrawProperty(timeColor, customSkin, "Time Color");
                    DreamOSEditorHandler.DrawProperty(helpCommand, customSkin, "Help Command");
                    DreamOSEditorHandler.DrawPropertyCW(errorText, customSkin, "Error Feedback", -3);
                    DreamOSEditorHandler.DrawPropertyCW(onEnableText, customSkin, "On Enable Feedback", -3);
                    getTimeData.boolValue = DreamOSEditorHandler.DrawToggle(getTimeData.boolValue, customSkin, "Get Time Data");
                    enableHelpCommand.boolValue = DreamOSEditorHandler.DrawToggle(enableHelpCommand.boolValue, customSkin, "Enable Help Command");
                    antiFlicker.boolValue = DreamOSEditorHandler.DrawToggle(antiFlicker.boolValue, customSkin, "Anti Flicker");
                    useTypewriterEffect.boolValue = DreamOSEditorHandler.DrawToggle(useTypewriterEffect.boolValue, customSkin, "Use Typewriter Effect");
                    if (useTypewriterEffect.boolValue == true) { DreamOSEditorHandler.DrawProperty(typewriterDelay, customSkin, "Typewriter Delay"); }
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif