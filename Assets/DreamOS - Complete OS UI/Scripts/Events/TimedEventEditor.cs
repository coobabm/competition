#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(TimedEvent))]
    public class TimedEventEditor : Editor
    {
        private TimedEvent teTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            teTarget = (TimedEvent)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var timer = serializedObject.FindProperty("timer");
            var enableAtStart = serializedObject.FindProperty("enableAtStart");
            var timerAction = serializedObject.FindProperty("timerAction");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
            DreamOSEditorHandler.DrawProperty(timer, customSkin, "Timer (s)");
            enableAtStart.boolValue = DreamOSEditorHandler.DrawToggle(enableAtStart.boolValue, customSkin, "Enable At Start");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
            EditorGUILayout.PropertyField(timerAction, new GUIContent("Timer Events"), true);

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif