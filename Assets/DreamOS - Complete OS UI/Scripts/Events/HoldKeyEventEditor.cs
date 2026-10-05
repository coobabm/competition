#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(HoldKeyEvent))]
    public class HoldKeyEventEditor : Editor
    {
        private HoldKeyEvent hkeTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            hkeTarget = (HoldKeyEvent)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var hotkey = serializedObject.FindProperty("hotkey");
            var holdAction = serializedObject.FindProperty("holdAction");
            var releaseAction = serializedObject.FindProperty("releaseAction");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
            EditorGUILayout.PropertyField(hotkey, new GUIContent(""), true);

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
            EditorGUILayout.PropertyField(holdAction, new GUIContent("Hold Events"), true);
            EditorGUILayout.PropertyField(releaseAction, new GUIContent("Release Events"), true);

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif