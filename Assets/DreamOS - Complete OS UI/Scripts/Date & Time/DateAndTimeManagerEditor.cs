#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(DateAndTimeManager))]
    public class DateAndTimeManagerEditor : Editor
    {
        private DateAndTimeManager timeTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            timeTarget = (DateAndTimeManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_DateTime");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Time");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Time", "Time"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var timeMultiplier = serializedObject.FindProperty("timeMultiplier");
            var currentDay = serializedObject.FindProperty("currentDay");
            var currentMonth = serializedObject.FindProperty("currentMonth");
            var currentYear = serializedObject.FindProperty("currentYear");
            var currentHour = serializedObject.FindProperty("currentHour");
            var currentMinute = serializedObject.FindProperty("currentMinute");
            var currentSecond = serializedObject.FindProperty("currentSecond");
            var monthData = serializedObject.FindProperty("monthData");

            var timedEvents = serializedObject.FindProperty("timedEvents");

            var notificationIcon = serializedObject.FindProperty("notificationIcon");

            var useSystemTime = serializedObject.FindProperty("useSystemTime");
            var saveTimeData = serializedObject.FindProperty("saveTimeData");
            var useShortTimeFormat = serializedObject.FindProperty("useShortTimeFormat");
            var defaultTimeFormat = serializedObject.FindProperty("defaultTimeFormat");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    DreamOSEditorHandler.DrawProperty(timeMultiplier, customSkin, "Time Multiplier");
                    DreamOSEditorHandler.DrawProperty(currentYear, customSkin, "Current Year");
                    DreamOSEditorHandler.DrawProperty(currentMonth, customSkin, "Current Month");
                    DreamOSEditorHandler.DrawProperty(currentDay, customSkin, "Current Day");
                    DreamOSEditorHandler.DrawProperty(currentHour, customSkin, "Current Hour");
                    DreamOSEditorHandler.DrawProperty(currentMinute, customSkin, "Current Minute");
                    DreamOSEditorHandler.DrawProperty(currentSecond, customSkin, "Current Second");

                    if (saveTimeData.boolValue == true)
                        EditorGUILayout.HelpBox("Save Time Data is enabled. Some of these variables won't be used if there's a stored data.", MessageType.Info);

                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(timedEvents, new GUIContent("Timed Events"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawProperty(notificationIcon, customSkin, "Notification Icon");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    useSystemTime.boolValue = DreamOSEditorHandler.DrawToggle(useSystemTime.boolValue, customSkin, "Use System Time", "Use the OS time instead of the simulated one.");
                    if (useSystemTime.boolValue == true) { GUI.enabled = false; }

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    useShortTimeFormat.boolValue = DreamOSEditorHandler.DrawTogglePlain(useShortTimeFormat.boolValue, customSkin, "Use Short Time Format", "Use AM/PM short time format.");
                    GUILayout.Space(3);
                    if (useShortTimeFormat.boolValue == true) { DreamOSEditorHandler.DrawPropertyCW(defaultTimeFormat, customSkin, "Default Time Format", 128); }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    saveTimeData.boolValue = DreamOSEditorHandler.DrawTogglePlain(saveTimeData.boolValue, customSkin, "Save Time Data", "Save the time data at runtime.");
                    GUILayout.Space(3);
                    if (saveTimeData.boolValue == true && GUILayout.Button("Delete Saved Data", customSkin.button)) { timeTarget.DeleteSavedData(); }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(monthData, new GUIContent("Month Data"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();

                    if (EditorGUI.EndChangeCheck() && monthData.arraySize > 12) { monthData.arraySize = 12; }
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif