#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(WorldSpaceManager))]
    public class WorldSpaceManagerEditor : Editor
    {
        private WorldSpaceManager wsTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            wsTarget = (WorldSpaceManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_WorldSpace");

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

            var onEnter = serializedObject.FindProperty("onEnter");
            var onEnterEnd = serializedObject.FindProperty("onEnterEnd");
            var onExit = serializedObject.FindProperty("onExit");
            var onExitEnd = serializedObject.FindProperty("onExitEnd");
            var onTriggerEnter = serializedObject.FindProperty("onTriggerEnter");
            var onTriggerExit = serializedObject.FindProperty("onTriggerExit");

            var mainCamera = serializedObject.FindProperty("mainCamera");
            var enterMount = serializedObject.FindProperty("enterMount");
            var projectorCam = serializedObject.FindProperty("projectorCam");
            var osCanvas = serializedObject.FindProperty("osCanvas");
            var pressKeyEvent = serializedObject.FindProperty("pressKeyEvent");

            var playerTag = serializedObject.FindProperty("playerTag");
            var warmComponents = serializedObject.FindProperty("warmComponents");
            var requiresOpening = serializedObject.FindProperty("requiresOpening");
            var autoGetIn = serializedObject.FindProperty("autoGetIn");
            var setCursorState = serializedObject.FindProperty("setCursorState");     
            var positionMode = serializedObject.FindProperty("positionMode");
            var selectedTagIndex = serializedObject.FindProperty("selectedTagIndex");
            var useMipMap = serializedObject.FindProperty("useMipMap");
            var dynamicRTSize = serializedObject.FindProperty("dynamicRTSize");
            var rtWidth = serializedObject.FindProperty("rtWidth");
            var rtHeight = serializedObject.FindProperty("rtHeight");
            var rendererImage = serializedObject.FindProperty("rendererImage");
            var audioSources = serializedObject.FindProperty("audioSources");
            var audioBlendSpeed = serializedObject.FindProperty("audioBlendSpeed");
            var transitionCurve = serializedObject.FindProperty("transitionCurve");
            var transitionTime = serializedObject.FindProperty("transitionTime");
            var getInKey = serializedObject.FindProperty("getInKey");
            var getOutKey = serializedObject.FindProperty("getOutKey");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 10);
                    GUILayout.BeginHorizontal();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(audioSources, new GUIContent("Dynamic Audio Sources"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndHorizontal();

                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onEnter, new GUIContent("On Enter"), true);
                    EditorGUILayout.PropertyField(onEnterEnd, new GUIContent("On Enter End"), true);
                    EditorGUILayout.PropertyField(onExit, new GUIContent("On Exit"), true);
                    EditorGUILayout.PropertyField(onExitEnd, new GUIContent("On Exit End"), true);
                    EditorGUILayout.PropertyField(onTriggerEnter, new GUIContent("On Trigger Enter"), true);
                    EditorGUILayout.PropertyField(onTriggerExit, new GUIContent("On Trigger Exit"), true);
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(mainCamera, customSkin, "Main Camera");
                    DreamOSEditorHandler.DrawProperty(projectorCam, customSkin, "Projector Cam");
                    DreamOSEditorHandler.DrawProperty(rendererImage, customSkin, "Renderer Image");
                    DreamOSEditorHandler.DrawProperty(enterMount, customSkin, "Enter Mount");
                    DreamOSEditorHandler.DrawProperty(osCanvas, customSkin, "OS Canvas");
                    DreamOSEditorHandler.DrawProperty(pressKeyEvent, customSkin, "Press Key Event", "Sets the lock screen PEK interaction.");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    requiresOpening.boolValue = DreamOSEditorHandler.DrawToggle(requiresOpening.boolValue, customSkin, "Require Opening At Start", "Makes DreamOS disabled at start.");
                    autoGetIn.boolValue = DreamOSEditorHandler.DrawToggle(autoGetIn.boolValue, customSkin, "Auto Get In On Trigger");
                    setCursorState.boolValue = DreamOSEditorHandler.DrawToggle(setCursorState.boolValue, customSkin, "Set Cursor State", "Set cursor state on enter/exit. If you want to manually set the state, disable this option.");
                    warmComponents.boolValue = DreamOSEditorHandler.DrawToggle(warmComponents.boolValue, customSkin, "Warm Components", "If you are experiencing freezing during migration, you can enable this option.");
                    useMipMap.boolValue = DreamOSEditorHandler.DrawToggle(useMipMap.boolValue, customSkin, "Use Mip Map", "Enable mip map for render texture.");
                    dynamicRTSize.boolValue = DreamOSEditorHandler.DrawToggle(dynamicRTSize.boolValue, customSkin, "Dynamic Render Texture Size", "Matches the rendering size with current screen size.");

                    if (dynamicRTSize.boolValue == false)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);
                        EditorGUILayout.LabelField(new GUIContent("Render Width/Height"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(rtWidth, new GUIContent(""));
                        EditorGUILayout.PropertyField(rtHeight, new GUIContent(""));
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(new GUIContent("Player Tag"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    selectedTagIndex.intValue = EditorGUILayout.Popup(selectedTagIndex.intValue, UnityEditorInternal.InternalEditorUtility.tags);
                    playerTag.stringValue = UnityEditorInternal.InternalEditorUtility.tags[selectedTagIndex.intValue].ToString();
                    GUILayout.EndHorizontal();
                    DreamOSEditorHandler.DrawProperty(positionMode, customSkin, "Position Mode");
                    DreamOSEditorHandler.DrawProperty(transitionCurve, customSkin, "Transition Curve");
                    DreamOSEditorHandler.DrawProperty(transitionTime, customSkin, "Transition Time");
                    DreamOSEditorHandler.DrawProperty(audioBlendSpeed, customSkin, "Audio Blend Speed");
                    EditorGUILayout.PropertyField(getInKey, new GUIContent("Get In Key"), true);
                    EditorGUILayout.PropertyField(getOutKey, new GUIContent("Get Out Key"), true);
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif