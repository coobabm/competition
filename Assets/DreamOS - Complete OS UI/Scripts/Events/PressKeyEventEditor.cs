#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS.Events
{
    [CustomEditor(typeof(PressKeyEvent))]
    public class PressKeyEventEditor : Editor
    {
        private PressKeyEvent pkeTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            pkeTarget = (PressKeyEvent)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var hotkey = serializedObject.FindProperty("hotkey");
            var pressAction = serializedObject.FindProperty("pressAction");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
            EditorGUILayout.PropertyField(hotkey, new GUIContent(""), true);

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
            EditorGUILayout.PropertyField(pressAction, new GUIContent("Press Key Events"), true);

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif