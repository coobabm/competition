#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(NotificationManager))]
    public class NotificationManagerEditor : Editor
    {
        private NotificationManager nmTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            nmTarget = (NotificationManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Notification");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Resources");
            toolbarTabs[1] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var notificationParent = serializedObject.FindProperty("notificationParent");
            var popupNotificationParent = serializedObject.FindProperty("popupNotificationParent");
            var notificationButton = serializedObject.FindProperty("notificationButton");
            var popupNotification = serializedObject.FindProperty("popupNotification");
            var standardNotification = serializedObject.FindProperty("standardNotification");

            var popupDuration = serializedObject.FindProperty("popupDuration");

            switch (currentTab)
            {             
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(notificationParent, customSkin, "Parent");
                    DreamOSEditorHandler.DrawProperty(popupNotificationParent, customSkin, "Popup Parent");
                    DreamOSEditorHandler.DrawProperty(notificationButton, customSkin, "Button");
                    DreamOSEditorHandler.DrawProperty(standardNotification, customSkin, "Standard Prefab");
                    DreamOSEditorHandler.DrawProperty(popupNotification, customSkin, "Popup Prefab");
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    DreamOSEditorHandler.DrawProperty(popupDuration, customSkin, "Popup Duration");
                    break;            
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif