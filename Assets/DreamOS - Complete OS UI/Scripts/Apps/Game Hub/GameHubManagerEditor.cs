#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(GameHubManager))]
    public class GameHubManagerEditor : Editor
    {
        private GameHubManager ghManager;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            ghManager = (GameHubManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_GameHub");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var games = serializedObject.FindProperty("games");

            var gameContent = serializedObject.FindProperty("gameContent");
            var gameParent = serializedObject.FindProperty("gameParent");
            var gameTransition = serializedObject.FindProperty("gameTransition");
            var sliderIndicator = serializedObject.FindProperty("sliderIndicator");
            var sliderIndicatorParent = serializedObject.FindProperty("sliderIndicatorParent");
            var libraryPreset = serializedObject.FindProperty("libraryPreset");
            var libraryParent = serializedObject.FindProperty("libraryParent");
            var transitionHelper = serializedObject.FindProperty("transitionHelper");
            var sliderBanner = serializedObject.FindProperty("sliderBanner");
            var sliderIcon = serializedObject.FindProperty("sliderIcon");
            var sliderDescription = serializedObject.FindProperty("sliderDescription");
            var sliderPlayButton = serializedObject.FindProperty("sliderPlayButton");
            var targetCanvas = serializedObject.FindProperty("targetCanvas");

            var useLocalization = serializedObject.FindProperty("useLocalization");
            var sliderTimer = serializedObject.FindProperty("sliderTimer");
            var transitionSpeed = serializedObject.FindProperty("transitionSpeed");
            var sliderScaleSpeed = serializedObject.FindProperty("sliderScaleSpeed");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginHorizontal();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(games, new GUIContent("Games"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndHorizontal();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(gameContent, customSkin, "Game Content");
                    DreamOSEditorHandler.DrawProperty(gameParent, customSkin, "Game Parent");
                    DreamOSEditorHandler.DrawProperty(gameTransition, customSkin, "Game Transition");
                    DreamOSEditorHandler.DrawProperty(sliderIndicator, customSkin, "Slider Indicator");
                    DreamOSEditorHandler.DrawProperty(sliderIndicatorParent, customSkin, "Indicator Parent");
                    DreamOSEditorHandler.DrawProperty(libraryPreset, customSkin, "Library Preset");
                    DreamOSEditorHandler.DrawProperty(libraryParent, customSkin, "library Parent");
                    DreamOSEditorHandler.DrawProperty(transitionHelper, customSkin, "Transition Helper");
                    DreamOSEditorHandler.DrawProperty(sliderBanner, customSkin, "Slider Banner");
                    DreamOSEditorHandler.DrawProperty(sliderIcon, customSkin, "Slider Icon");
                    DreamOSEditorHandler.DrawProperty(sliderDescription, customSkin, "Slider Description");
                    DreamOSEditorHandler.DrawProperty(sliderPlayButton, customSkin, "Slider Play Button");
                    DreamOSEditorHandler.DrawProperty(targetCanvas, customSkin, "Target Canvas");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    useLocalization.boolValue = DreamOSEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization");
                    DreamOSEditorHandler.DrawProperty(sliderTimer, customSkin, "Slider Timer");
                    DreamOSEditorHandler.DrawProperty(transitionSpeed, customSkin, "Transition Speed");
                    DreamOSEditorHandler.DrawProperty(sliderScaleSpeed, customSkin, "Scale Speed");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif