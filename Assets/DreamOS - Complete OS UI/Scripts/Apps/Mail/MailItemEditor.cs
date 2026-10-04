#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(MailItem))]
    public class MailItemEditor : Editor
    {
        private GUISkin customSkin;

        void OnEnable()
        {
            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            // Content
            var mailFolder = serializedObject.FindProperty("mailFolder");
            var subject = serializedObject.FindProperty("subject");
            var fromName = serializedObject.FindProperty("fromName");
            var from = serializedObject.FindProperty("from");
            var to = serializedObject.FindProperty("to");
            var time = serializedObject.FindProperty("time");
            var date = serializedObject.FindProperty("date");
            var contactImage = serializedObject.FindProperty("contactImage");
            var useCustomContent = serializedObject.FindProperty("useCustomContent");
            var mailContent = serializedObject.FindProperty("mailContent");
            var customContentPrefab = serializedObject.FindProperty("customContentPrefab");
            var attachments = serializedObject.FindProperty("attachments");

            var subjectKey = serializedObject.FindProperty("subjectKey");
            var contentKey = serializedObject.FindProperty("contentKey");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 8);
            DreamOSEditorHandler.DrawProperty(mailFolder, customSkin, "Mail Folder");
            DreamOSEditorHandler.DrawProperty(subject, customSkin, "Subject");
            DreamOSEditorHandler.DrawProperty(fromName, customSkin, "From Name");
            DreamOSEditorHandler.DrawProperty(from, customSkin, "From");
            DreamOSEditorHandler.DrawProperty(to, customSkin, "To");
            DreamOSEditorHandler.DrawProperty(time, customSkin, "Time");
            DreamOSEditorHandler.DrawProperty(date, customSkin, "Date");
            DreamOSEditorHandler.DrawProperty(contactImage, customSkin, "Contact Image");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 8);
            useCustomContent.boolValue = DreamOSEditorHandler.DrawToggle(useCustomContent.boolValue, customSkin, "Use Custom Content");

            if (useCustomContent.boolValue == false) { DreamOSEditorHandler.DrawPropertyCW(mailContent, customSkin, "Mail Content", -3); }
            else { DreamOSEditorHandler.DrawProperty(customContentPrefab, customSkin, "Content Prefab"); }

            GUILayout.BeginVertical();
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(attachments, new GUIContent("Attachments"), true);
            EditorGUI.indentLevel = 0;
            GUILayout.EndVertical();

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Languages", 8);
            DreamOSEditorHandler.DrawProperty(subjectKey, customSkin, "Subject Key");
            if (useCustomContent.boolValue == false) { DreamOSEditorHandler.DrawProperty(contentKey, customSkin, "Content Key"); }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif