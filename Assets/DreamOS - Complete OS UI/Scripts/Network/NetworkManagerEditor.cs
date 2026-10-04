#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(NetworkManager))]
    public class NetworkManagerEditor : Editor
    {
        private NetworkManager networkTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            networkTarget = (NetworkManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Network");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Network List");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Network List", "Network List"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var networkItems = serializedObject.FindProperty("networkItems");
            var currentNetworkIndex = serializedObject.FindProperty("currentNetworkIndex");

            var networkPreset = serializedObject.FindProperty("networkPreset");
            var signalDisconnected = serializedObject.FindProperty("signalDisconnected");
            var signalWeak = serializedObject.FindProperty("signalWeak");
            var signalNormal = serializedObject.FindProperty("signalNormal");
            var signalStrong = serializedObject.FindProperty("signalStrong");
            var signalBest = serializedObject.FindProperty("signalBest");
            var networkIndicators = serializedObject.FindProperty("networkIndicators");

            var dynamicNetwork = serializedObject.FindProperty("dynamicNetwork");
            var isConnected = serializedObject.FindProperty("isConnected");
            var defaultSpeed = serializedObject.FindProperty("defaultSpeed");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);

                    if (networkTarget.networkItems.Count > 0)
                    {
                        if (Application.isPlaying == true) { GUI.enabled = false; }
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.BeginHorizontal();
                        GUI.enabled = false;
                        EditorGUILayout.LabelField(new GUIContent("Selected Network:"), customSkin.FindStyle("Text"), GUILayout.Width(106));
                        if (Application.isPlaying == false) { GUI.enabled = true; }
                        if (networkTarget.networkItems.Count > currentNetworkIndex.intValue) { EditorGUILayout.LabelField(new GUIContent(networkTarget.networkItems[currentNetworkIndex.intValue].networkID), customSkin.FindStyle("Text")); }
                        GUILayout.EndHorizontal();

                        currentNetworkIndex.intValue = EditorGUILayout.IntSlider(currentNetworkIndex.intValue, 0, networkTarget.networkItems.Count - 1);

                        GUI.enabled = true;
                        GUILayout.Space(2);
                        GUILayout.EndVertical();
                    }

                    else { EditorGUILayout.HelpBox("Network list is empty.", MessageType.Warning); }

                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(networkItems, new GUIContent("Network Items"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(networkPreset, customSkin, "Network Preset");
                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(networkIndicators, new GUIContent("Network Indicators"), true);
                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    dynamicNetwork.boolValue = DreamOSEditorHandler.DrawToggle(dynamicNetwork.boolValue, customSkin, "Dynamic Network");

                    if (dynamicNetwork.boolValue == true)
                    {
                        GUI.enabled = false;
                        isConnected.boolValue = DreamOSEditorHandler.DrawToggle(isConnected.boolValue, customSkin, "Is Connected");
                        GUI.enabled = true;
                        DreamOSEditorHandler.DrawProperty(signalDisconnected, customSkin, "Disconnected Icon");
                        DreamOSEditorHandler.DrawProperty(signalWeak, customSkin, "Weak Signal Icon");
                        DreamOSEditorHandler.DrawProperty(signalNormal, customSkin, "Strong Normal Icon");
                        DreamOSEditorHandler.DrawProperty(signalStrong, customSkin, "Strong Signal Icon");
                        DreamOSEditorHandler.DrawProperty(signalBest, customSkin, "Best Signal Icon");
                    }

                    else
                    {
                        DreamOSEditorHandler.DrawProperty(defaultSpeed, customSkin, "Default Speed");

                        EditorGUILayout.HelpBox("'Dynamic Network' is disabled. There won't be any dynamic network items, " +
                            "'Default Speed' will be used instead.", MessageType.Info);
                    }

                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif