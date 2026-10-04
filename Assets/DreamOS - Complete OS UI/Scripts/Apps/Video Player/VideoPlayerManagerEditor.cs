#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using static Michsky.DreamOS.NotepadManager;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(VideoPlayerManager))]
    public class VideoPlayerManagerEditor : Editor
    {
        private VideoPlayerManager videoTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            videoTarget = (VideoPlayerManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_VideoPlayer");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var videoItems = serializedObject.FindProperty("videoItems");

            var videoPlayer = serializedObject.FindProperty("videoPlayer");
            var audioSource = serializedObject.FindProperty("audioSource");
            var videoParent = serializedObject.FindProperty("videoParent");
            var videoPreset = serializedObject.FindProperty("videoPreset");
            var panelManager = serializedObject.FindProperty("panelManager");
            var videoControls = serializedObject.FindProperty("videoControls");
            var miniPlayer = serializedObject.FindProperty("miniPlayer");

            var hideControlsIn = serializedObject.FindProperty("hideControlsIn");
            var seekTime = serializedObject.FindProperty("seekTime");
            var videoPanelName = serializedObject.FindProperty("videoPanelName"); 

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(videoItems, new GUIContent("Video Items"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(videoPlayer, customSkin, "Video Player");
                    DreamOSEditorHandler.DrawProperty(audioSource, customSkin, "Audio Source");
                    DreamOSEditorHandler.DrawProperty(videoParent, customSkin, "Video Parent");
                    DreamOSEditorHandler.DrawProperty(videoPreset, customSkin, "Video Preset");
                    DreamOSEditorHandler.DrawProperty(panelManager, customSkin, "Panel Manager");
                    DreamOSEditorHandler.DrawProperty(videoControls, customSkin, "Video Controls");
                    DreamOSEditorHandler.DrawProperty(miniPlayer, customSkin, "Mini Player");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    DreamOSEditorHandler.DrawProperty(hideControlsIn, customSkin, "Hide Controls In (s)");
                    DreamOSEditorHandler.DrawProperty(seekTime, customSkin, "Seek Time (s)");
                    DreamOSEditorHandler.DrawProperty(videoPanelName, customSkin, "Video Panel Name");
                    break;            
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif