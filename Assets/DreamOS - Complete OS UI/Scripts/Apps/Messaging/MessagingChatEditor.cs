#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(MessagingChat))]
    public class MessagingChatEditor : Editor
    {
        private GUISkin customSkin;
        private MessagingChat mcTarget;

        void OnEnable()
        {
            mcTarget = (MessagingChat)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            // Tab_Settings
            var useDynamicMessages = serializedObject.FindProperty("useDynamicMessages");
            var useStoryTeller = serializedObject.FindProperty("useStoryTeller");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 8);
            useDynamicMessages.boolValue = DreamOSEditorHandler.DrawToggle(useDynamicMessages.boolValue, customSkin, "Use Dynamic Messages");
            useStoryTeller.boolValue = DreamOSEditorHandler.DrawToggle(useStoryTeller.boolValue, customSkin, "Use StoryTeller [Beta]");

            // Content
            var messageList = serializedObject.FindProperty("messageList");
            var dynamicMessages = serializedObject.FindProperty("dynamicMessages");
            var storyTeller = serializedObject.FindProperty("storyTeller");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 8);
            GUILayout.BeginVertical();
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(messageList, new GUIContent("Message List"), true);
            EditorGUI.indentLevel = 0;
            GUILayout.EndVertical();

            if (useDynamicMessages.boolValue == true)
            {
                GUILayout.Space(5);
                GUILayout.BeginVertical();
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(dynamicMessages, new GUIContent("Dynamic Messages"), true);
                EditorGUI.indentLevel = 0;
                GUILayout.EndVertical();
            }

            if (useStoryTeller.boolValue == true)
            {
                GUILayout.Space(5);
                GUILayout.BeginVertical();
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(storyTeller, new GUIContent("StoryTeller"), true);
                EditorGUI.indentLevel = 0;
                GUILayout.EndVertical();
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif