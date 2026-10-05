#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(MailManager))]
    public class MailManagerEditor : Editor
    {
        private MailManager mmTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            mmTarget = (MailManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Mail");

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

            var mailList = serializedObject.FindProperty("mailList");

            var mailViewer = serializedObject.FindProperty("mailViewer");
            var inboxParent = serializedObject.FindProperty("inboxParent");
            var sentParent = serializedObject.FindProperty("sentParent");
            var junkParent = serializedObject.FindProperty("junkParent");
            var attachmentPanel = serializedObject.FindProperty("attachmentPanel");
            var attachmentParent = serializedObject.FindProperty("attachmentParent");
            var itemTemplate = serializedObject.FindProperty("itemTemplate");
            var mailTemplate = serializedObject.FindProperty("mailTemplate");
            var attachmentItem = serializedObject.FindProperty("attachmentItem");
            var musicManager = serializedObject.FindProperty("musicManager");
            var noteManager = serializedObject.FindProperty("noteManager");
            var pictureManager = serializedObject.FindProperty("pictureManager");
            var videoManager = serializedObject.FindProperty("videoManager");

            var useLocalization = serializedObject.FindProperty("useLocalization");
            var fromPrefix = serializedObject.FindProperty("fromPrefix");
            var fromSuffix = serializedObject.FindProperty("fromSuffix");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(mailList, new GUIContent("Mail Items"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(mailViewer, customSkin, "Mail Viewer");
                    DreamOSEditorHandler.DrawProperty(inboxParent, customSkin, "Inbox Parent");
                    DreamOSEditorHandler.DrawProperty(sentParent, customSkin, "Sent Parent");
                    DreamOSEditorHandler.DrawProperty(junkParent, customSkin, "Junk Parent");
                    DreamOSEditorHandler.DrawProperty(attachmentPanel, customSkin, "Attachment Panel");
                    DreamOSEditorHandler.DrawProperty(attachmentParent, customSkin, "Attachment Parent");
                    DreamOSEditorHandler.DrawProperty(itemTemplate, customSkin, "Item Template");
                    DreamOSEditorHandler.DrawProperty(mailTemplate, customSkin, "Mail Template");
                    DreamOSEditorHandler.DrawProperty(attachmentItem, customSkin, "Attachment Item");
                    DreamOSEditorHandler.DrawProperty(musicManager, customSkin, "Music Manager");
                    DreamOSEditorHandler.DrawProperty(noteManager, customSkin, "Note Manager");
                    DreamOSEditorHandler.DrawProperty(pictureManager, customSkin, "Picture Manager");
                    DreamOSEditorHandler.DrawProperty(videoManager, customSkin, "Video Manager");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    useLocalization.boolValue = DreamOSEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization");
                    DreamOSEditorHandler.DrawProperty(fromPrefix, customSkin, "From Prefix");
                    DreamOSEditorHandler.DrawProperty(fromSuffix, customSkin, "From Suffix");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif