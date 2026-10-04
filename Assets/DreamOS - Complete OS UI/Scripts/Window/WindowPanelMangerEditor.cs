#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using static UnityEngine.GraphicsBuffer;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(WindowPanelManager))]
    public class WindowPanelManagerEditor : Editor
    {
        private WindowPanelManager wpmTarget;
        private GUISkin customSkin;
        private int currentTab;
        string newPanelName = "New Tab";
        Sprite panelIcon;

        private void OnEnable()
        {
            wpmTarget = (WindowPanelManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_WindowPanelManager");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = DreamOSEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Chat List", "Chat List"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var panels = serializedObject.FindProperty("panels");
            var currentPanelIndex = serializedObject.FindProperty("currentPanelIndex");
            var onPanelChanged = serializedObject.FindProperty("onPanelChanged");

            var indicator = serializedObject.FindProperty("indicator");

            var cullPanels = serializedObject.FindProperty("cullPanels");
            var initializeButtons = serializedObject.FindProperty("initializeButtons");
            var bypassAnimationOnEnable = serializedObject.FindProperty("bypassAnimationOnEnable");
            var panelAnimationSpeed = serializedObject.FindProperty("panelAnimationSpeed");
            var indicatorDuration = serializedObject.FindProperty("indicatorDuration");
            var indicatorCurve = serializedObject.FindProperty("indicatorCurve");

            switch (currentTab)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);

                    if (wpmTarget.currentPanelIndex > wpmTarget.panels.Count - 1) { wpmTarget.currentPanelIndex = 0; }
                    if (wpmTarget.panels.Count != 0)
                    {
                        if (Application.isPlaying == true) { GUI.enabled = false; }
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.BeginHorizontal();

                        GUI.enabled = false;
                        EditorGUILayout.LabelField(new GUIContent("Current Panel:"), customSkin.FindStyle("Text"), GUILayout.Width(82));
                        GUI.enabled = true;
                        EditorGUILayout.LabelField(new GUIContent(wpmTarget.panels[currentPanelIndex.intValue].panelName), customSkin.FindStyle("Text"));
                      
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2);

                        currentPanelIndex.intValue = EditorGUILayout.IntSlider(currentPanelIndex.intValue, 0, wpmTarget.panels.Count - 1);

                        if (Application.isPlaying == false && wpmTarget.panels[currentPanelIndex.intValue].panelObject != null)
                        {
                            for (int i = 0; i < wpmTarget.panels.Count; i++)
                            {
                                if (i == currentPanelIndex.intValue)
                                {
                                    var tempCG = wpmTarget.panels[currentPanelIndex.intValue].panelObject.GetComponent<CanvasGroup>();
                                    if (tempCG != null) { tempCG.alpha = 1; }
                                }

                                else if (wpmTarget.panels[i].panelObject != null)
                                {
                                    var tempCG = wpmTarget.panels[i].panelObject.GetComponent<CanvasGroup>();
                                    if (tempCG != null) { tempCG.alpha = 0; }
                                }
                            }
                        }

                        if (wpmTarget.panels[wpmTarget.currentPanelIndex].panelObject != null && GUILayout.Button("Select Current Panel", customSkin.button)) { Selection.activeObject = wpmTarget.panels[wpmTarget.currentPanelIndex].panelObject; }
                        GUI.enabled = true;
                        GUILayout.EndVertical();
                    }

                    else { EditorGUILayout.HelpBox("Panel list is empty. Create a new item to see more options.", MessageType.Info); }

                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(panels, new GUIContent("Panel Items"), true);

                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();

                    if (wpmTarget.panels.Count != 0 && wpmTarget.panels[wpmTarget.panels.Count - 1] != null
                        && wpmTarget.panels[wpmTarget.panels.Count - 1].panelObject != null
                        && wpmTarget.panels[wpmTarget.panels.Count - 1].panelButton != null)
                    {
                        DreamOSEditorHandler.DrawHeader(customSkin, "Header_Customization", 10);
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.BeginHorizontal();
                        
                        EditorGUILayout.LabelField(new GUIContent("Panel Name"), customSkin.FindStyle("Text"), GUILayout.Width(85));
                        newPanelName = (string)EditorGUILayout.TextField(newPanelName);
                        
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(new GUIContent("Panel Icon"), customSkin.FindStyle("Text"), GUILayout.Width(85));
                        panelIcon = (Sprite)EditorGUILayout.ObjectField(panelIcon, typeof(Sprite), true);

                        GUILayout.EndHorizontal();

                        if (GUILayout.Button("+  Create a new panel", customSkin.button))
                        {
                            GameObject panelGO = Instantiate(wpmTarget.panels[0].panelObject.gameObject, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                            panelGO.transform.SetParent(wpmTarget.panels[0].panelObject.transform.parent, false);
                            panelGO.gameObject.name = newPanelName;

                            Transform contentGO = panelGO.transform.Find("Content");

                            foreach (Transform child in contentGO)
                                DestroyImmediate(child.gameObject);

                            try 
                            {
                                TMPro.TextMeshProUGUI panelTitleGO = panelGO.transform.Find("Title").GetComponent<TMPro.TextMeshProUGUI>(); ;
                                panelTitleGO.text = newPanelName;
                            }

                            catch { }

                            CanvasGroup tempCG = panelGO.GetComponent<CanvasGroup>();
                            tempCG.alpha = 0;

                            GameObject buttonGO = Instantiate(wpmTarget.panels[0].panelButton.gameObject, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                            buttonGO.transform.SetParent(wpmTarget.panels[0].panelButton.transform.parent, false);
                            buttonGO.gameObject.name = newPanelName;

                            PanelButton tempNDB = buttonGO.gameObject.GetComponent<PanelButton>();
                            tempNDB.onClick.RemoveAllListeners();

                            ButtonManager tempBM = buttonGO.GetComponent<ButtonManager>();
                            tempBM.buttonText = newPanelName;
                            tempBM.buttonIcon = panelIcon;

                            WindowPanelManager.PanelItem newPanelItem = new WindowPanelManager.PanelItem();
                            newPanelItem.panelName = newPanelName;
                            newPanelItem.panelObject = panelGO.GetComponent<Animator>();
                            newPanelItem.panelButton = buttonGO.GetComponent<PanelButton>();
                            wpmTarget.panels.Add(newPanelItem);
                        }

                        GUILayout.EndVertical();

                        DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                        EditorGUILayout.PropertyField(onPanelChanged, new GUIContent("On Panel Changed"), true);
                    }

                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(indicator, customSkin, "Indicator");
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    cullPanels.boolValue = DreamOSEditorHandler.DrawToggle(cullPanels.boolValue, customSkin, "Cull Panels", "Disables unused panels.");
                    initializeButtons.boolValue = DreamOSEditorHandler.DrawToggle(initializeButtons.boolValue, customSkin, "Initialize Buttons", "Automatically adds necessary events to buttons.");
                    bypassAnimationOnEnable.boolValue = DreamOSEditorHandler.DrawToggle(bypassAnimationOnEnable.boolValue, customSkin, "Bypass Animation On Enable");
                    DreamOSEditorHandler.DrawProperty(panelAnimationSpeed, customSkin, "Animation Speed");
                    DreamOSEditorHandler.DrawProperty(indicatorDuration, customSkin, "Indicator Duration");
                    DreamOSEditorHandler.DrawProperty(indicatorCurve, customSkin, "Indicator Curve");
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif