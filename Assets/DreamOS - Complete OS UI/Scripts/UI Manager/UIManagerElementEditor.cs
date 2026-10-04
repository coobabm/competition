#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(UIManagerElement))]
    public class UIManagerElementEditor : Editor
    {
        private UIManagerElement tmTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            tmTarget = (UIManagerElement)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var themeManagerAsset = serializedObject.FindProperty("themeManagerAsset");
         
            var objectType = serializedObject.FindProperty("objectType");
            var colorType = serializedObject.FindProperty("colorType");
            var fontType = serializedObject.FindProperty("fontType");
            var keepAlphaValue = serializedObject.FindProperty("keepAlphaValue");
            var useCustomFont = serializedObject.FindProperty("useCustomFont");
            var useCustomColor = serializedObject.FindProperty("useCustomColor");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            DreamOSEditorHandler.DrawProperty(themeManagerAsset, customSkin, "UI Manager");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);

            if (tmTarget.themeManagerAsset != null)
            {
                DreamOSEditorHandler.DrawProperty(objectType, customSkin, "Object Type");
                DreamOSEditorHandler.DrawProperty(colorType, customSkin, "Color Type");

                if (tmTarget.objectType == UIManagerElement.ObjectType.Text)
                {
                    DreamOSEditorHandler.DrawProperty(fontType, customSkin, "Font Type");
                    useCustomFont.boolValue = DreamOSEditorHandler.DrawToggle(useCustomFont.boolValue, customSkin, "Use Custom Font");
                    useCustomColor.boolValue = DreamOSEditorHandler.DrawToggle(useCustomColor.boolValue, customSkin, "Use Custom Color");
                }

                keepAlphaValue.boolValue = DreamOSEditorHandler.DrawToggle(keepAlphaValue.boolValue, customSkin, "Keep Alpha Value");
            }

            else { EditorGUILayout.HelpBox("'UI Manager' is missing.", MessageType.Error); }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif