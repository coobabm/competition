#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(MultiInstanceManager))]
    public class MultiInstanceManagerEditor : Editor
    {
        private GUISkin customSkin;
        private MultiInstanceManager mimTarget;
        private int currentTab;
        private bool createdComp;

        private void OnEnable()
        {
            mimTarget = (MultiInstanceManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override bool RequiresConstantRepaint() { return true; }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_InstanceManager");

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            // Foldout style
            GUIStyle foldoutStyle = customSkin.FindStyle("Foldout");

            var playerCamera = serializedObject.FindProperty("playerCamera");
            var manageProjectors = serializedObject.FindProperty("manageProjectors");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);

                    for (int i = 0; i < mimTarget.instances.Count; i++)
                    {
                        // Draw Action Buttons
                        GUILayout.Space(6);
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        if (mimTarget.instances[i].worldSpaceManager == null) { GUI.enabled = false; }
                        else { GUI.enabled = true; }

                        if (GUILayout.Button("Select", customSkin.button, GUILayout.Width(50)))
                        {
                            Selection.activeObject = mimTarget.instances[i].worldSpaceManager.transform.parent;
                        }

                        GUI.enabled = true;

                        if (GUILayout.Button("Delete", customSkin.button, GUILayout.Width(50)))
                        {
                            if (EditorUtility.DisplayDialog("Delete Instance #" + i, "Are you sure you want to delete the instance? " +
                                "This will delete ALL of the instance resources/objects and cannot be undone.", "Yes", "Cancel"))
                            {
                                try { DeleteInstance(i); }
                                catch { Debug.LogWarning("<b>[Multi Instance Manager]</b> Something went wrong while deleting the instance."); }
                            }
                        }

                        GUILayout.Space(6);
                        GUILayout.EndHorizontal();

                        // Start Item Background
                        GUILayout.Space(-30);
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);
                        GUILayout.BeginVertical();
                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        mimTarget.instances[i].isExpanded = EditorGUILayout.Foldout(mimTarget.instances[i].isExpanded, "Instance #" + i.ToString(), true, foldoutStyle);
                        mimTarget.instances[i].isExpanded = GUILayout.Toggle(mimTarget.instances[i].isExpanded, new GUIContent(""), customSkin.FindStyle("ToggleHelper"));
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2);

                        if (mimTarget.instances[i].isExpanded)
                        {
                            // Core Resources
                            GUILayout.Label("Core Resources", customSkin.FindStyle("Text"), GUILayout.Width(140));

                            // World Space Manager
                            GUILayout.BeginHorizontal(EditorStyles.helpBox);
                            GUILayout.Label("World Space Manager", customSkin.FindStyle("Text"), GUILayout.Width(140));
                            mimTarget.instances[i].worldSpaceManager = EditorGUILayout.ObjectField(mimTarget.instances[i].worldSpaceManager, typeof(WorldSpaceManager), true) as WorldSpaceManager;
                            GUILayout.EndHorizontal();

                            // Instance Canvas
                            GUILayout.BeginHorizontal(EditorStyles.helpBox);
                            GUILayout.Label("Instance Canvas", customSkin.FindStyle("Text"), GUILayout.Width(140));
                            mimTarget.instances[i].instanceCanvas = EditorGUILayout.ObjectField(mimTarget.instances[i].instanceCanvas, typeof(Canvas), true) as Canvas;
                            GUILayout.EndHorizontal();

                            if (mimTarget.instances[i].worldSpaceManager == null || mimTarget.instances[i].instanceCanvas == null)
                            {
                                EditorGUILayout.HelpBox("'World Space Manager' and/or 'Instance Canvas' is missing.", MessageType.Warning);
                                GUI.enabled = false;
                            }

                            // User Manager
                            GUILayout.BeginHorizontal(EditorStyles.helpBox);
                            GUILayout.Label("User Manager", customSkin.FindStyle("Text"), GUILayout.Width(140));
                            mimTarget.instances[i].userManager = EditorGUILayout.ObjectField(mimTarget.instances[i].userManager, typeof(UserManager), true) as UserManager;
                            GUILayout.EndHorizontal();

                            if (GUILayout.Button("Initialize Instance", customSkin.button))
                            {
                                mimTarget.AutoWizard(i);
                            }
                        }

                        GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                    }

                    if (mimTarget.instances.Count < 10)
                    {
                        GUILayout.Space(2);
                        GUILayout.BeginHorizontal();

                        if (mimTarget.instances.Count > 0 && GUILayout.Button("Initialize All", customSkin.button))
                        {
                            for (int i = 0; i < mimTarget.instances.Count; i++)
                            {
                                mimTarget.AutoWizard(i);
                            }
                        }

                        if (GUILayout.Button("+ Create new instance", customSkin.button))
                        {
                            MultiInstanceManager.InstanceItem item = new MultiInstanceManager.InstanceItem();
                            mimTarget.instances.Add(item);
                            EditorUtility.SetDirty(this);
                            return;
                        }

                        GUILayout.EndHorizontal();
                    }

                    else 
                    { 
                        EditorGUILayout.HelpBox("You've reached to the max instance limit (10).", MessageType.Info); 
                    }

                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(playerCamera, customSkin, "Player Camera");
                    break;
            }

            if (mimTarget.playerCamera == null) 
            { 
                EditorGUILayout.HelpBox("Player Camera is missing.", MessageType.Warning); 
            }
      
            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }

        private void DeleteInstance(int index)
        {
            if (mimTarget.instances[index].worldSpaceManager == null) { mimTarget.instances.RemoveAt(index); }
            else if (mimTarget.instances[index].worldSpaceManager.transform.parent.gameObject != null)
            {
                DestroyImmediate(mimTarget.instances[index].worldSpaceManager.transform.parent.gameObject);
                mimTarget.instances.RemoveAt(index);
            }

            // Undo.RecordObject(this, "Removed DreamOS instance");
            EditorSceneManager.MarkSceneDirty(mimTarget.gameObject.scene);
        }
    }
}
#endif