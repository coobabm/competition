#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(MessagingManager))]
    public class MessagingManagerEditor : Editor
    {
        private MessagingManager mesTarget;
        private GUISkin customSkin;
        private int currentEditorTab = 0;

        private void OnEnable()
        {
            mesTarget = (MessagingManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Messaging");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentEditorTab = DreamOSEditorHandler.DrawTabs(currentEditorTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Chat List", "Chat List"), customSkin.FindStyle("Tab_Content")))
                currentEditorTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentEditorTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentEditorTab = 2;

            GUILayout.EndHorizontal();

            var chatList = serializedObject.FindProperty("chatList");

            var chatParent = serializedObject.FindProperty("chatParent");
            var chatViewer = serializedObject.FindProperty("chatViewer");
            var chatItem = serializedObject.FindProperty("chatItem");
            var textMessageSent = serializedObject.FindProperty("textMessageSent");
            var textMessageRecieved = serializedObject.FindProperty("textMessageRecieved");
            var audioMessageSent = serializedObject.FindProperty("audioMessageSent");
            var audioMessageRecieved = serializedObject.FindProperty("audioMessageRecieved");
            var imageMessageSent = serializedObject.FindProperty("imageMessageSent");
            var imageMessageRecieved = serializedObject.FindProperty("imageMessageRecieved");
            var chatMessageTimer = serializedObject.FindProperty("chatMessageTimer");
            var chatLayout = serializedObject.FindProperty("chatLayout");
            var messageDate = serializedObject.FindProperty("messageDate");
            var beginningIndicator = serializedObject.FindProperty("beginningIndicator");
            var messageInput = serializedObject.FindProperty("messageInput");
            var storyTellerAnimator = serializedObject.FindProperty("storyTellerAnimator");
            var storyTellerList = serializedObject.FindProperty("storyTellerList");
            var storyTellerObject = serializedObject.FindProperty("storyTellerObject");
            var photoGalleryManager = serializedObject.FindProperty("photoGalleryManager");

            var debugStoryTeller = serializedObject.FindProperty("debugStoryTeller");
            var useNotifications = serializedObject.FindProperty("useNotifications");
            var useLocalization = serializedObject.FindProperty("useLocalization");
            var dynamicSorting = serializedObject.FindProperty("dynamicSorting");
            var receivedMessageSFX = serializedObject.FindProperty("receivedMessageSFX");
            var sentMessageSFX = serializedObject.FindProperty("sentMessageSFX");
            var messageStoring = serializedObject.FindProperty("messageStoring");
            var saveMessageHistory = serializedObject.FindProperty("saveMessageHistory");
            var notificationIcon = serializedObject.FindProperty("notificationIcon");
            var audioMessageNotification = serializedObject.FindProperty("audioMessageNotification");
            var imageMessageNotification = serializedObject.FindProperty("imageMessageNotification");

            switch (currentEditorTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(chatList, new GUIContent("List"), true);
                    EditorGUI.indentLevel = 0;            
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawPropertyCW(chatParent, customSkin, "Chat Parent", 130);
                    DreamOSEditorHandler.DrawPropertyCW(chatViewer, customSkin, "Chat Viewer", 130);
                    DreamOSEditorHandler.DrawPropertyCW(chatLayout, customSkin, "Chat Layout", 130);
                    DreamOSEditorHandler.DrawPropertyCW(chatItem, customSkin, "Chat Item", 130);
                    DreamOSEditorHandler.DrawPropertyCW(textMessageSent, customSkin, "Text Msg Sent", 130);
                    DreamOSEditorHandler.DrawPropertyCW(textMessageRecieved, customSkin, "Text Msg Recieved", 130);
                    DreamOSEditorHandler.DrawPropertyCW(imageMessageSent, customSkin, "Image Msg Sent", 130);
                    DreamOSEditorHandler.DrawPropertyCW(imageMessageRecieved, customSkin, "Image Msg Recieved", 130);
                    DreamOSEditorHandler.DrawPropertyCW(audioMessageSent, customSkin, "Audio Msg Sent", 130);
                    DreamOSEditorHandler.DrawPropertyCW(audioMessageRecieved, customSkin, "Audio Msg Recieved", 130);
                    DreamOSEditorHandler.DrawPropertyCW(chatMessageTimer, customSkin, "Chat Msg Timer", 130);
                    DreamOSEditorHandler.DrawPropertyCW(messageDate, customSkin, "Message Date", 130);
                    DreamOSEditorHandler.DrawPropertyCW(beginningIndicator, customSkin, "Beginning Indicator", 130);
                    DreamOSEditorHandler.DrawPropertyCW(messageInput, customSkin, "Message Input", 130);
                    DreamOSEditorHandler.DrawPropertyCW(storyTellerAnimator, customSkin, "StoryTeller Animator", 130);
                    DreamOSEditorHandler.DrawPropertyCW(storyTellerList, customSkin, "StoryTeller List", 130);
                    DreamOSEditorHandler.DrawPropertyCW(storyTellerObject, customSkin, "StoryTeller Object", 130);
                    DreamOSEditorHandler.DrawPropertyCW(photoGalleryManager, customSkin, "Photo Manager", 130);
                    if (saveMessageHistory.boolValue == true) { DreamOSEditorHandler.DrawPropertyCW(messageStoring, customSkin, "Message Storing", 130); }
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    debugStoryTeller.boolValue = DreamOSEditorHandler.DrawToggle(debugStoryTeller.boolValue, customSkin, "Debug StoryTeller", "Allow creating StoryTeller sequences using their ID.");
                    useNotifications.boolValue = DreamOSEditorHandler.DrawToggle(useNotifications.boolValue, customSkin, "Use Notifications", "Create notifiation when receiving new messages.");
                    useLocalization.boolValue = DreamOSEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Allow or disallow localization.");
                    dynamicSorting.boolValue = DreamOSEditorHandler.DrawToggle(dynamicSorting.boolValue, customSkin, "Dynamic Sorting", "Changes chat sorting at runtime depending on unread status.");
                    saveMessageHistory.boolValue = DreamOSEditorHandler.DrawToggle(saveMessageHistory.boolValue, customSkin, "Save Message History", "Saves the message history and loads the data on re-launch.");
                    DreamOSEditorHandler.DrawPropertyCW(sentMessageSFX, customSkin, "Sent Message SFX", 150);
                    DreamOSEditorHandler.DrawPropertyCW(receivedMessageSFX, customSkin, "Received Message SFX", 150);
                    DreamOSEditorHandler.DrawPropertyCW(notificationIcon, customSkin, "Notification Icon", 150);
                    DreamOSEditorHandler.DrawPropertyCW(audioMessageNotification, customSkin, "Audio Notification Text", -3);
                    DreamOSEditorHandler.DrawPropertyCW(imageMessageNotification, customSkin, "Image Notification Text", -3);
                    break;
            }

            if (saveMessageHistory.boolValue == true && mesTarget.messageStoring == null)
            {
                EditorGUILayout.HelpBox("'Save Sent Messages' is enabled but 'Message Storing' is not assigned. " +
                    "Please add and/or assign 'Message Storing' component via Resources tab.", MessageType.Error);

                if (GUILayout.Button("+  Create Message Storing", customSkin.button))
                {
                    MessageStoring tempMS = mesTarget.gameObject.AddComponent<MessageStoring>();
                    mesTarget.messageStoring = tempMS;
                    tempMS.messagingManager = mesTarget;

                    PrefabUtility.RecordPrefabInstancePropertyModifications(mesTarget);
                    Undo.RecordObject(this, "Created message storing");
                    EditorUtility.SetDirty(this);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mesTarget.gameObject.scene);
                }
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif