#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Michsky.DreamOS;

namespace UnityEngine.UI
{
    namespace Michsky.DreamOS
    {
        [CustomEditor(typeof(UIGradientComplex))]
        public class UIGradientComplexEditor : Editor
        {
            private int currentTab;
            private GUISkin customSkin;

            private void OnEnable()
            {
                if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
                else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
            }

            public override void OnInspectorGUI()
            {
                DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Gradient");

                GUIContent[] toolbarTabs = new GUIContent[1];
                toolbarTabs[0] = new GUIContent("Settings");

                currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

                if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                    currentTab = 0;

                GUILayout.EndHorizontal();

                var _effectGradient = serializedObject.FindProperty("_effectGradient");
                var gradientType = serializedObject.FindProperty("gradientType");
                var offset = serializedObject.FindProperty("offset");
                var zoom = serializedObject.FindProperty("zoom");
                var complexGradient = serializedObject.FindProperty("complexGradient");

                switch (currentTab)
                {
                    case 0:
                        DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                        DreamOSEditorHandler.DrawProperty(_effectGradient, customSkin, "Gradient");
                        DreamOSEditorHandler.DrawProperty(gradientType, customSkin, "Type");
                        DreamOSEditorHandler.DrawProperty(offset, customSkin, "Offset");
                        DreamOSEditorHandler.DrawProperty(zoom, customSkin, "Zoom");
                        complexGradient.boolValue = DreamOSEditorHandler.DrawToggle(complexGradient.boolValue, customSkin, "Complex Gradient");
                        break;              
                }

                if (Application.isPlaying == false) { this.Repaint(); }
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif