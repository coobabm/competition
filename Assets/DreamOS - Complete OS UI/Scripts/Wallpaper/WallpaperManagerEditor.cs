#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(WallpaperManager))]
    public class WallpaperManagerEditor : Editor
    {
        private WallpaperManager wmTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            wmTarget = (WallpaperManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            Color defaultColor = GUI.color;
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Wallpaper");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Settings");
            toolbarTabs[1] = new GUIContent("Resources");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 0;

            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var wallpaperLibrary = serializedObject.FindProperty("wallpaperLibrary");
            var wallpaperItem = serializedObject.FindProperty("wallpaperItem");
         
            var wallpaperIndex = serializedObject.FindProperty("wallpaperIndex");
            var saveSelected = serializedObject.FindProperty("saveSelected");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    DreamOSEditorHandler.DrawProperty(wallpaperLibrary, customSkin, "Wallpaper Library");    
                    saveSelected.boolValue = DreamOSEditorHandler.DrawToggle(saveSelected.boolValue, customSkin, "Save Selected");

                    if (wmTarget.wallpaperLibrary != null)
                    {
                        if (wmTarget.wallpaperLibrary.wallpapers.Count != 0)
                        {
                            GUILayout.Space(-2);
                            GUILayout.BeginHorizontal();      
                            GUILayout.FlexibleSpace();
                            GUI.backgroundColor = Color.clear;

                            GUILayout.Box(DreamOSEditorHandler.TextureFromSprite(wmTarget.wallpaperLibrary.wallpapers[wallpaperIndex.intValue].wallpaperSprite), GUILayout.Width(245), GUILayout.Height(140));

                            GUI.backgroundColor = defaultColor;
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                            GUILayout.Space(-2);
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(2);

                            GUI.enabled = false;
                            EditorGUILayout.LabelField(new GUIContent("Selected Wallpaper:"), customSkin.FindStyle("Text"), GUILayout.Width(116));
                            GUI.enabled = true;
                            EditorGUILayout.LabelField(new GUIContent(wmTarget.wallpaperLibrary.wallpapers[wallpaperIndex.intValue].wallpaperID), customSkin.FindStyle("Text"), GUILayout.Width(112));

                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(2);

                            wallpaperIndex.intValue = EditorGUILayout.IntSlider(wallpaperIndex.intValue, 0, wmTarget.wallpaperLibrary.wallpapers.Count - 1);

                            GUILayout.Space(2);
                            GUILayout.EndHorizontal();
                            GUILayout.Space(2);
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(2);

                            if (GUILayout.Button("Update Wallpaper Objects"))
                            {
#if UNITY_2023_2_OR_NEWER
                                int objectCount = FindObjectsByType<WallpaperObject>(FindObjectsSortMode.None).Length;
#else
                                int objectCount = FindObjectsOfType(typeof(WallpaperObject)).Length;
#endif

                                for (int i = 0; i < objectCount; ++i)
                                {
#if UNITY_2023_2_OR_NEWER
                                    WallpaperObject wo = FindObjectsByType<WallpaperObject>(FindObjectsSortMode.None)[i];
#else
                                    WallpaperObject wo = (WallpaperObject)FindObjectsOfType(typeof(WallpaperObject))[i];
#endif
                                    wo.UpdateWallpaper(wmTarget);
                                }

                                wmTarget.enabled = false;
                                wmTarget.enabled = true;
                            }

                            GUILayout.Space(2);
                            GUILayout.EndHorizontal();
                            GUILayout.Space(2);
                        }

                        else { EditorGUILayout.HelpBox("There is no item in the library.", MessageType.Warning); }
                    }

                    else { EditorGUILayout.HelpBox("Wallpaper Library is missing.", MessageType.Error); }

                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(wallpaperItem, customSkin, "Wallpaper Item");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif