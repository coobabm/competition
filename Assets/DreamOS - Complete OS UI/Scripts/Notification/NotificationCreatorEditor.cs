#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(NotificationCreator))]
    public class NotificationCreatorEditor : Editor
    {
        private NotificationCreator ntfmcTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            ntfmcTarget = (NotificationCreator)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var icon = serializedObject.FindProperty("icon");
            var title = serializedObject.FindProperty("title");
            var description = serializedObject.FindProperty("description");
            var notificationButtons = serializedObject.FindProperty("notificationButtons");
          
            var notificationManager = serializedObject.FindProperty("notificationManager");
          
            var enableSound = serializedObject.FindProperty("enableSound");
            var createOnEnable = serializedObject.FindProperty("createOnEnable");
            var notificationType = serializedObject.FindProperty("notificationType");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
            DreamOSEditorHandler.DrawPropertyCW(icon, customSkin, "Icon", 80);
            DreamOSEditorHandler.DrawPropertyCW(title, customSkin, "Title", 80);
            DreamOSEditorHandler.DrawPropertyCW(description, customSkin, "Description", -3);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(notificationButtons, new GUIContent("Notification Buttons"), true);
            EditorGUI.indentLevel = 0;
            GUILayout.EndVertical();

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            enableSound.boolValue = DreamOSEditorHandler.DrawToggle(enableSound.boolValue, customSkin, "Enable Sound");
            createOnEnable.boolValue = DreamOSEditorHandler.DrawToggle(createOnEnable.boolValue, customSkin, "Create On Enable");
            DreamOSEditorHandler.DrawPropertyCW(notificationType, customSkin, "Notification Type", 132);

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif