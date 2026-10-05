#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(SpeechRecognition))]
    public class SpeechRecognitionEditor : Editor
    {
        private SpeechRecognition srTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            srTarget = (SpeechRecognition)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_SpeechRecognition");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Chat List", "Chat List"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

#if UNITY_STANDALONE_WIN || UNITY_WSA
            var keywords = serializedObject.FindProperty("keywords");
            var commands = serializedObject.FindProperty("commands");
            var listeningMessages = serializedObject.FindProperty("listeningMessages");
            var onKeywordCall = serializedObject.FindProperty("onKeywordCall");
            var onDismiss = serializedObject.FindProperty("onDismiss");
           
            var coriPopup = serializedObject.FindProperty("coriPopup");
            var listeningText = serializedObject.FindProperty("listeningText");
            var listeningEffect = serializedObject.FindProperty("listeningEffect");
            var dismissEffect = serializedObject.FindProperty("dismissEffect");
            var taskbarShortcut = serializedObject.FindProperty("taskbarShortcut");

            var enableKeywords = serializedObject.FindProperty("enableKeywords");
            var enableLogs = serializedObject.FindProperty("enableLogs");
            var dismissAfter = serializedObject.FindProperty("dismissAfter");
            var hypotheses = serializedObject.FindProperty("hypotheses");
            var recognitions = serializedObject.FindProperty("recognitions");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginHorizontal();
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(keywords, new GUIContent("Keywords"), true);

                    EditorGUI.indentLevel = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(commands, new GUIContent("Commands"), true);

                    EditorGUI.indentLevel = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(listeningMessages, new GUIContent("Listening Messages"), true);

                    EditorGUI.indentLevel = 0;
                    GUILayout.EndHorizontal();
               
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onKeywordCall, new GUIContent("On Keyword Call"));
                    EditorGUILayout.PropertyField(onDismiss, new GUIContent("On Dismiss"));
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(coriPopup, customSkin, "Cori Popup");
                    DreamOSEditorHandler.DrawProperty(listeningText, customSkin, "Listening Text");
                    DreamOSEditorHandler.DrawProperty(taskbarShortcut, customSkin, "Taskbar Shortcut");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    DreamOSEditorHandler.DrawProperty(dismissAfter, customSkin, "Dismiss After (s)");
                    DreamOSEditorHandler.DrawProperty(listeningEffect, customSkin, "Listening Effect");
                    DreamOSEditorHandler.DrawProperty(dismissEffect, customSkin, "Dismiss Effect");
                    enableKeywords.boolValue = DreamOSEditorHandler.DrawToggle(enableKeywords.boolValue, customSkin, "Enable Keywords");
                    enableLogs.boolValue = DreamOSEditorHandler.DrawToggle(enableLogs.boolValue, customSkin, "Enable Logs");
                    GUILayout.Space(8);
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(hypotheses, new GUIContent("Hypotheses"));
                    EditorGUILayout.PropertyField(recognitions, new GUIContent("Recognitions"));
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
#else
            EditorGUILayout.HelpBox("Speech Recognition is only available for Windows 10, 11 and UWP.", MessageType.Info);
#endif
        }
    }
}
#endif