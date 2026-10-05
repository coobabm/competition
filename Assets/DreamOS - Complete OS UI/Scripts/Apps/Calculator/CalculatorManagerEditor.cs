#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(CalculatorManager))]
    public class CalculatorManagerEditor : Editor
    {
        private CalculatorManager cTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            cTarget = (CalculatorManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var displayText = serializedObject.FindProperty("displayText");
            var displayOperator = serializedObject.FindProperty("displayOperator");
            var displayResult = serializedObject.FindProperty("displayResult");
            var displayPreview = serializedObject.FindProperty("displayPreview");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            DreamOSEditorHandler.DrawProperty(displayText, customSkin, "Display Text");
            DreamOSEditorHandler.DrawProperty(displayOperator, customSkin, "Display Operator");
            DreamOSEditorHandler.DrawProperty(displayResult, customSkin, "Display Result");
            DreamOSEditorHandler.DrawProperty(displayPreview, customSkin, "Display Preview");

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif