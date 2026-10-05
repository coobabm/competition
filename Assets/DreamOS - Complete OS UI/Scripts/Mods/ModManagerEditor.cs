#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(ModManager))]
    public class ModManagerEditor : Editor
    {
        private ModManager mmTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            mmTarget = (ModManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_ModManager");

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

            var modLibraryElement = serializedObject.FindProperty("modLibraryElement");
            var modLibraryParent = serializedObject.FindProperty("modLibraryParent");
            var noModsIndicator = serializedObject.FindProperty("noModsIndicator");

            var enableMusicPlayerModule = serializedObject.FindProperty("enableMusicPlayerModule");
            var enableMusicPlayerImportLogs = serializedObject.FindProperty("enableMusicPlayerImportLogs");
            var enableNotepadModule = serializedObject.FindProperty("enableNotepadModule");
            var enablePhotoGalleryModule = serializedObject.FindProperty("enablePhotoGalleryModule");
            var enableVideoPlayerModule = serializedObject.FindProperty("enableVideoPlayerModule");

            var musicPlayer = serializedObject.FindProperty("musicPlayer");
            var musicPlayerID = serializedObject.FindProperty("musicPlayerID");
            var notepad = serializedObject.FindProperty("notepad");
            var notepadID = serializedObject.FindProperty("notepadID");
            var photoGallery = serializedObject.FindProperty("photoGallery");
            var photoGalleryID = serializedObject.FindProperty("photoGalleryID");
            var videoPlayer = serializedObject.FindProperty("videoPlayer");
            var videoPlayerID = serializedObject.FindProperty("videoPlayerID");

            var subPath = serializedObject.FindProperty("subPath");
            var dataName = serializedObject.FindProperty("dataName");
            var fileExtension = serializedObject.FindProperty("fileExtension");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    enableMusicPlayerModule.boolValue = DreamOSEditorHandler.DrawTogglePlain(enableMusicPlayerModule.boolValue, customSkin, "Music Player Module");
                    GUILayout.Space(3);

                    if (mmTarget.enableMusicPlayerModule == true)
                    {
                        enableMusicPlayerImportLogs.boolValue = DreamOSEditorHandler.DrawToggle(enableMusicPlayerImportLogs.boolValue, customSkin, "Enable Import Logs");
                        DreamOSEditorHandler.DrawProperty(musicPlayer, customSkin, "Music Player");
                        DreamOSEditorHandler.DrawProperty(musicPlayerID, customSkin, "Music Player ID");
                    }

                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    enableNotepadModule.boolValue = DreamOSEditorHandler.DrawTogglePlain(enableNotepadModule.boolValue, customSkin, "Notepad Module");
                    GUILayout.Space(3);

                    if (mmTarget.enableNotepadModule == true)
                    {
                        DreamOSEditorHandler.DrawProperty(notepad, customSkin, "Notepad");
                        DreamOSEditorHandler.DrawProperty(notepadID, customSkin, "Notepad ID");
                    }

                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    enablePhotoGalleryModule.boolValue = DreamOSEditorHandler.DrawTogglePlain(enablePhotoGalleryModule.boolValue, customSkin, "Photo Gallery Module");
                    GUILayout.Space(3);

                    if (mmTarget.enablePhotoGalleryModule == true)
                    {
                        DreamOSEditorHandler.DrawProperty(photoGallery, customSkin, "Photo Gallery Module");
                        DreamOSEditorHandler.DrawProperty(photoGalleryID, customSkin, "Photo Gallery ID");
                    }

                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    enableVideoPlayerModule.boolValue = DreamOSEditorHandler.DrawTogglePlain(enableVideoPlayerModule.boolValue, customSkin, "Video Player Module");
                    GUILayout.Space(3);

                    if (mmTarget.enableVideoPlayerModule == true)
                    {
                        DreamOSEditorHandler.DrawProperty(videoPlayer, customSkin, "Video Player");
                        DreamOSEditorHandler.DrawProperty(videoPlayerID, customSkin, "Video Player ID");
                    }

                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(modLibraryElement, customSkin, "Mod Library Element");
                    DreamOSEditorHandler.DrawProperty(modLibraryParent, customSkin, "Mod Library Parent");
                    DreamOSEditorHandler.DrawProperty(noModsIndicator, customSkin, "No Mods Indicator");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    DreamOSEditorHandler.DrawProperty(subPath, customSkin, "Sub Path");
                    DreamOSEditorHandler.DrawProperty(dataName, customSkin, "Data Name");
                    DreamOSEditorHandler.DrawProperty(fileExtension, customSkin, "File Extension");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif