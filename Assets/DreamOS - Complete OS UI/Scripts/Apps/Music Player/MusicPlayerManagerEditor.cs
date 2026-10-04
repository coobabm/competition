#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(MusicPlayerManager))]
    public class MusicPlayerManagerEditor : Editor
    {
        private MusicPlayerManager mpTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            mpTarget = (MusicPlayerManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_MusicPlayer");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Playlists", "Playlists"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var libraryPlaylist = serializedObject.FindProperty("libraryPlaylist");
            var modPlaylist = serializedObject.FindProperty("modPlaylist");
            var customPlaylists = serializedObject.FindProperty("customPlaylists");

            var audioSource = serializedObject.FindProperty("audioSource");
            var libraryParent = serializedObject.FindProperty("libraryParent");
            var playlistParent = serializedObject.FindProperty("playlistParent");
            var playlistPanelParent = serializedObject.FindProperty("playlistPanelParent");
            var playlistTrackPreset = serializedObject.FindProperty("playlistTrackPreset");
            var playlistPanelPreset = serializedObject.FindProperty("playlistPanelPreset");
            var playlistItemPreset = serializedObject.FindProperty("playlistItemPreset");
            var musicPanelManager = serializedObject.FindProperty("musicPanelManager");
            var nowPlayingListTitle = serializedObject.FindProperty("nowPlayingListTitle");

            var repeat = serializedObject.FindProperty("repeat");
            var shuffle = serializedObject.FindProperty("shuffle");
            var sortListByName = serializedObject.FindProperty("sortListByName");
            var enablePopupNotification = serializedObject.FindProperty("enablePopupNotification");
            var playlistSingularLabel = serializedObject.FindProperty("playlistSingularLabel");
            var playlistPluralLabel = serializedObject.FindProperty("playlistPluralLabel");
            var notificationIcon = serializedObject.FindProperty("notificationIcon");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    DreamOSEditorHandler.DrawProperty(libraryPlaylist, customSkin, "Library Playlist");
                    DreamOSEditorHandler.DrawProperty(modPlaylist, customSkin, "Mod Playlist");
                    GUILayout.BeginHorizontal();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(customPlaylists, new GUIContent("Custom Playlists"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndHorizontal();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawPropertyCW(audioSource, customSkin, "Audio Source", 130);
                    DreamOSEditorHandler.DrawPropertyCW(libraryParent, customSkin, "Library Parent", 130);
                    DreamOSEditorHandler.DrawPropertyCW(playlistParent, customSkin, "Playlist Parent", 130);
                    DreamOSEditorHandler.DrawPropertyCW(playlistPanelParent, customSkin, "Playlist Panel Parent", 130);
                    DreamOSEditorHandler.DrawPropertyCW(playlistTrackPreset, customSkin, "Playlist Track Preset", 130);
                    DreamOSEditorHandler.DrawPropertyCW(playlistPanelPreset, customSkin, "Playlist Panel Preset", 130);
                    DreamOSEditorHandler.DrawPropertyCW(playlistItemPreset, customSkin, "Playlist Item Preset", 130);
                    DreamOSEditorHandler.DrawPropertyCW(musicPanelManager, customSkin, "Panel Manager", 130);
                    DreamOSEditorHandler.DrawPropertyCW(nowPlayingListTitle, customSkin, "Now Playing Title", 130);
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    repeat.boolValue = DreamOSEditorHandler.DrawToggle(repeat.boolValue, customSkin, "Repeat");
                    shuffle.boolValue = DreamOSEditorHandler.DrawToggle(shuffle.boolValue, customSkin, "Shuffle");
                    sortListByName.boolValue = DreamOSEditorHandler.DrawToggle(sortListByName.boolValue, customSkin, "Sort List By Name");
                    enablePopupNotification.boolValue = DreamOSEditorHandler.DrawToggle(enablePopupNotification.boolValue, customSkin, "Enable Popup Notification");       
                    DreamOSEditorHandler.DrawProperty(playlistSingularLabel, customSkin, "Singular Label");
                    DreamOSEditorHandler.DrawProperty(playlistPluralLabel, customSkin, "Plural Label");
                    DreamOSEditorHandler.DrawProperty(notificationIcon, customSkin, "Notification Icon");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif