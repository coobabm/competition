#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(NotepadManager))]
    public class NotepadManagerEditor : Editor
    {
        private NotepadManager notepadTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            notepadTarget = (NotepadManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Notepad");

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

            var noteItems = serializedObject.FindProperty("noteItems");

            var noteLibraryParent = serializedObject.FindProperty("noteLibraryParent");
            var noteLibraryPreset = serializedObject.FindProperty("noteLibraryPreset");
            var windowManager = serializedObject.FindProperty("windowManager");
            var viewerAnimator = serializedObject.FindProperty("viewerAnimator");
            var viewerTitle = serializedObject.FindProperty("viewerTitle");
            var viewerContent = serializedObject.FindProperty("viewerContent");
            var deleteButton = serializedObject.FindProperty("deleteButton");

            var openNoteOnEnable = serializedObject.FindProperty("openNoteOnEnable");
            var useLocalization = serializedObject.FindProperty("useLocalization");
            var saveCustomNotes = serializedObject.FindProperty("saveCustomNotes");
            var notepadStoring = serializedObject.FindProperty("notepadStoring");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(noteItems, new GUIContent("Note Items"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(noteLibraryPreset, customSkin, "Library Preset");
                    DreamOSEditorHandler.DrawProperty(noteLibraryParent, customSkin, "Library Parent");
                    DreamOSEditorHandler.DrawProperty(viewerAnimator, customSkin, "Viewer Animator");
                    DreamOSEditorHandler.DrawProperty(viewerTitle, customSkin, "Viewer Title");
                    DreamOSEditorHandler.DrawProperty(viewerContent, customSkin, "Viewer Content");
                    DreamOSEditorHandler.DrawProperty(deleteButton, customSkin, "Delete Button");
                    DreamOSEditorHandler.DrawProperty(windowManager, customSkin, "Window Manager");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    openNoteOnEnable.boolValue = DreamOSEditorHandler.DrawToggle(openNoteOnEnable.boolValue, customSkin, "Open Note On Enable", "Opens the first note item on enable.");
                    useLocalization.boolValue = DreamOSEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Allow or disallow localization.");

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);
                    saveCustomNotes.boolValue = DreamOSEditorHandler.DrawTogglePlain(saveCustomNotes.boolValue, customSkin, "Save Custom Notes");
                    GUILayout.Space(3);

                    if (saveCustomNotes.boolValue == true)
                    {
                        DreamOSEditorHandler.DrawProperty(notepadStoring, customSkin, "Notepad Storing");

                        if (notepadTarget.notepadStoring == null)
                        {
                            EditorGUILayout.HelpBox("'Save Custom Notes' is enabled but 'Notepad Storing' is missing. " +
                                "Please add and/or assign a valid 'Notepad Storing' component.", MessageType.Error);

                            if (GUILayout.Button("+  Create Notepad Storing", customSkin.button))
                            {
                                NotepadStoring tempNS = notepadTarget.gameObject.AddComponent<NotepadStoring>();
                                notepadTarget.notepadStoring = tempNS;
                                tempNS.notepadManager = notepadTarget;

                                PrefabUtility.RecordPrefabInstancePropertyModifications(notepadTarget);
                                Undo.RecordObject(tempNS, "Created notepad storing");
                                EditorUtility.SetDirty(tempNS);
                                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(notepadTarget.gameObject.scene);
                            }
                        }
                    }

                    GUILayout.EndVertical();
                    break;            
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif