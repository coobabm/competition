#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(ReminderManager))]
    public class ReminderManagerEditor : Editor
    {
        private ReminderManager rTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            rTarget = (ReminderManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var reminderPreset = serializedObject.FindProperty("reminderPreset");
            var reminderParent = serializedObject.FindProperty("reminderParent");
            var reminderModal = serializedObject.FindProperty("reminderModal");
            var eventTitleObject = serializedObject.FindProperty("eventTitleObject");
            var hourSelector = serializedObject.FindProperty("hourSelector");
            var minuteSelector = serializedObject.FindProperty("minuteSelector");
            var meridiemSelector = serializedObject.FindProperty("meridiemSelector");
            var typeSelector = serializedObject.FindProperty("typeSelector");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            DreamOSEditorHandler.DrawProperty(reminderPreset, customSkin, "Reminder Preset");
            DreamOSEditorHandler.DrawProperty(reminderParent, customSkin, "Reminder Parent");
            DreamOSEditorHandler.DrawProperty(reminderModal, customSkin, "Reminder Modal");
            DreamOSEditorHandler.DrawProperty(eventTitleObject, customSkin, "Event Title Object");
            DreamOSEditorHandler.DrawProperty(hourSelector, customSkin, "Hour Selector");
            DreamOSEditorHandler.DrawProperty(minuteSelector, customSkin, "Minute Selector");
            DreamOSEditorHandler.DrawProperty(meridiemSelector, customSkin, "Meridiem Selector");
            DreamOSEditorHandler.DrawProperty(typeSelector, customSkin, "Type Selector");

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif