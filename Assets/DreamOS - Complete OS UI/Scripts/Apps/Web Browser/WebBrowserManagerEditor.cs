#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(WebBrowserManager))]
    public class WebBrowserManagerEditor : Editor
    {
        private WebBrowserManager webTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            webTarget = (WebBrowserManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_WebBrowser");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Settings");
            toolbarTabs[1] = new GUIContent("Resources");
            
            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var networkManager = serializedObject.FindProperty("networkManager");
            var webLibrary = serializedObject.FindProperty("webLibrary");
            var tabPreset = serializedObject.FindProperty("tabPreset");
            var tabParent = serializedObject.FindProperty("tabParent");
            var pageViewer = serializedObject.FindProperty("pageViewer");
            var newTabButton = serializedObject.FindProperty("newTabButton");
            var backButton = serializedObject.FindProperty("backButton");
            var forwardButton = serializedObject.FindProperty("forwardButton");
            var urlField = serializedObject.FindProperty("urlField");
            var favoriteButton = serializedObject.FindProperty("favoriteButton");
            var favoriteAnimator = serializedObject.FindProperty("favoriteAnimator");
            var favoritePreset = serializedObject.FindProperty("favoritePreset");
            var favoritesParent = serializedObject.FindProperty("favoritesParent");
            var downloadPreset = serializedObject.FindProperty("downloadPreset");
            var downloadsParent = serializedObject.FindProperty("downloadsParent");
            var downloadsPanel = serializedObject.FindProperty("downloadsPanel");
            var musicPlayerApp = serializedObject.FindProperty("musicPlayerApp");
            var notepadApp = serializedObject.FindProperty("notepadApp");
            var photoGalleryApp = serializedObject.FindProperty("photoGalleryApp");
            var videoPlayerApp = serializedObject.FindProperty("videoPlayerApp");

            var rememberTabsOnLaunch = serializedObject.FindProperty("rememberTabsOnLaunch");
            var openDownloadsPanel = serializedObject.FindProperty("openDownloadsPanel");
            var maxTabLimit = serializedObject.FindProperty("maxTabLimit");
            var timeoutDuration = serializedObject.FindProperty("timeoutDuration");
            var defaultNetworkSpeed = serializedObject.FindProperty("defaultNetworkSpeed");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
            DreamOSEditorHandler.DrawProperty(networkManager, customSkin, "Network Manager");
            DreamOSEditorHandler.DrawProperty(webLibrary, customSkin, "Web Library");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    rememberTabsOnLaunch.boolValue = DreamOSEditorHandler.DrawToggle(rememberTabsOnLaunch.boolValue, customSkin, "Remember Tabs On Launch", "Recover tabs on app re-launch.");
                    openDownloadsPanel.boolValue = DreamOSEditorHandler.DrawToggle(openDownloadsPanel.boolValue, customSkin, "Open Downloads Panel", "Opens the download panel automatically when starting downloading a file.");
                    DreamOSEditorHandler.DrawProperty(maxTabLimit, customSkin, "Max Tab Limit");
                    DreamOSEditorHandler.DrawProperty(timeoutDuration, customSkin, "Timeout Duration", "Set the timout duration for the pages that can't be reached.");
                    DreamOSEditorHandler.DrawProperty(defaultNetworkSpeed, customSkin, "Default Speed", "This value will be used as long as the network manager is not linked.");
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(tabPreset, customSkin, "Tab Preset");
                    DreamOSEditorHandler.DrawProperty(tabParent, customSkin, "Tab Parent");
                    DreamOSEditorHandler.DrawProperty(pageViewer, customSkin, "Page Viewer");
                    DreamOSEditorHandler.DrawProperty(newTabButton, customSkin, "New Tab Button");
                    DreamOSEditorHandler.DrawProperty(backButton, customSkin, "Back Button");
                    DreamOSEditorHandler.DrawProperty(forwardButton, customSkin, "Forward Button");
                    DreamOSEditorHandler.DrawProperty(urlField, customSkin, "URL Field");
                    DreamOSEditorHandler.DrawProperty(favoriteButton, customSkin, "Favorite Buton");
                    DreamOSEditorHandler.DrawProperty(favoriteAnimator, customSkin, "Favorite Animator");
                    DreamOSEditorHandler.DrawProperty(favoritePreset, customSkin, "Favorite Preset");
                    DreamOSEditorHandler.DrawProperty(favoritesParent, customSkin, "Favorites Parent");
                    DreamOSEditorHandler.DrawProperty(downloadPreset, customSkin, "Download Preset");
                    DreamOSEditorHandler.DrawProperty(downloadsParent, customSkin, "Downloads Parent");
                    DreamOSEditorHandler.DrawProperty(downloadsPanel, customSkin, "Downloads Panel");
                    DreamOSEditorHandler.DrawProperty(musicPlayerApp, customSkin, "Music Player App");
                    DreamOSEditorHandler.DrawProperty(notepadApp, customSkin, "Notepad App");
                    DreamOSEditorHandler.DrawProperty(photoGalleryApp, customSkin, "Photo Gallery App");
                    DreamOSEditorHandler.DrawProperty(videoPlayerApp, customSkin, "Video Player App");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif