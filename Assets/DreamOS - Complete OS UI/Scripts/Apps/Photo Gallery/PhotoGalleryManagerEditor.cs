#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(PhotoGalleryManager))]
    public class PhotoGalleryManagerEditor : Editor
    {
        private PhotoGalleryManager photoTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            photoTarget = (PhotoGalleryManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_PhotoGallery");

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

            var photoItems = serializedObject.FindProperty("photoItems");

            var pictureLibraryPreset = serializedObject.FindProperty("pictureLibraryPreset");
            var pictureLibraryParent = serializedObject.FindProperty("pictureLibraryParent");
            var imageViewer = serializedObject.FindProperty("imageViewer");
            var viewerTitle = serializedObject.FindProperty("viewerTitle");
            var viewerDescription = serializedObject.FindProperty("viewerDescription");
            var viewerPanelName = serializedObject.FindProperty("viewerPanelName");
            var nextButton = serializedObject.FindProperty("nextButton");
            var previousButton = serializedObject.FindProperty("previousButton");
            var panelManager = serializedObject.FindProperty("panelManager");

            var sortListByName = serializedObject.FindProperty("sortListByName");
            var allowArrowNavigation = serializedObject.FindProperty("allowArrowNavigation");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(photoItems, new GUIContent("Photo Items"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(pictureLibraryPreset, customSkin, "Library Preset");
                    DreamOSEditorHandler.DrawProperty(pictureLibraryParent, customSkin, "Library Parent");
                    DreamOSEditorHandler.DrawProperty(imageViewer, customSkin, "Image Viewer");
                    DreamOSEditorHandler.DrawProperty(viewerTitle, customSkin, "Viewer Title");
                    DreamOSEditorHandler.DrawProperty(viewerDescription, customSkin, "Viewer Description");
                    DreamOSEditorHandler.DrawProperty(nextButton, customSkin, "Next Button");
                    DreamOSEditorHandler.DrawProperty(previousButton, customSkin, "Previous Button");
                    DreamOSEditorHandler.DrawProperty(panelManager, customSkin, "Panel Manager");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    allowArrowNavigation.boolValue = DreamOSEditorHandler.DrawToggle(allowArrowNavigation.boolValue, customSkin, "Allow Navigation With Arrow Keys");
                    DreamOSEditorHandler.DrawProperty(viewerPanelName, customSkin, "Viewer Panel Name");
                    break;            
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif