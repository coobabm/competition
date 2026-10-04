#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(SetupManager))]
    public class SetupManagerEditor : Editor
    {
        private SetupManager setupTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            setupTarget = (SetupManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Setup");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var steps = serializedObject.FindProperty("steps");
            var userManager = serializedObject.FindProperty("userManager");
            var setupScreen = serializedObject.FindProperty("setupScreen");
            var firstNameInput = serializedObject.FindProperty("firstNameInput");
            var lastNameInput = serializedObject.FindProperty("lastNameInput");
            var passwordInput = serializedObject.FindProperty("passwordInput");
            var passwordRetypeInput = serializedObject.FindProperty("passwordRetypeInput");
            var secQuestionInput = serializedObject.FindProperty("secQuestionInput");
            var secAnswerInput = serializedObject.FindProperty("secAnswerInput");

            switch (currentTab)
            {             
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(steps, new GUIContent("Step List"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(userManager, customSkin, "User Manager");
                    DreamOSEditorHandler.DrawProperty(setupScreen, customSkin, "Setup Screen");
                    DreamOSEditorHandler.DrawProperty(firstNameInput, customSkin, "First Name Input");
                    DreamOSEditorHandler.DrawProperty(lastNameInput, customSkin, "Last Name Input");
                    DreamOSEditorHandler.DrawProperty(passwordInput, customSkin, "Password Input");
                    DreamOSEditorHandler.DrawProperty(passwordRetypeInput, customSkin, "Password RT Input");
                    DreamOSEditorHandler.DrawProperty(secQuestionInput, customSkin, "Sec Question Input");
                    DreamOSEditorHandler.DrawProperty(secAnswerInput, customSkin, "Sec Answer Input");
                    break;          
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif