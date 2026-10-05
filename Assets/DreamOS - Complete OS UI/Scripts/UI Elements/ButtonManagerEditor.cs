#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ButtonManager))]
    public class ButtonManagerEditor : Editor
    {
        private ButtonManager buttonTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            buttonTarget = (ButtonManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Button");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            buttonTarget.latestTabIndex = DreamOSEditorHandler.DrawTabs(buttonTarget.latestTabIndex, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                buttonTarget.latestTabIndex = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                buttonTarget.latestTabIndex = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                buttonTarget.latestTabIndex = 2;

            GUILayout.EndHorizontal();

            var normalCG = serializedObject.FindProperty("normalCG");
            var highlightCG = serializedObject.FindProperty("highlightCG");
            var pressedCG = serializedObject.FindProperty("pressedCG");
            var disabledCG = serializedObject.FindProperty("disabledCG");
            var normalTextObj = serializedObject.FindProperty("normalTextObj");
            var highlightTextObj = serializedObject.FindProperty("highlightTextObj");
            var pressedTextObj = serializedObject.FindProperty("pressedTextObj");
            var disabledTextObj = serializedObject.FindProperty("disabledTextObj");
            var normalImageObj = serializedObject.FindProperty("normalImageObj");
            var highlightImageObj = serializedObject.FindProperty("highlightImageObj");
            var pressedImageObj = serializedObject.FindProperty("pressedImageObj");
            var disabledImageObj = serializedObject.FindProperty("disabledImageObj");

            var buttonIcon = serializedObject.FindProperty("buttonIcon");
            var buttonText = serializedObject.FindProperty("buttonText");
            var iconScale = serializedObject.FindProperty("iconScale");
            var textSize = serializedObject.FindProperty("textSize");

            var autoFitContent = serializedObject.FindProperty("autoFitContent");
            var padding = serializedObject.FindProperty("padding");
            var spacing = serializedObject.FindProperty("spacing");
            var disabledLayout = serializedObject.FindProperty("disabledLayout");
            var normalLayout = serializedObject.FindProperty("normalLayout");
            var highlightedLayout = serializedObject.FindProperty("highlightedLayout");
            var pressedLayout = serializedObject.FindProperty("pressedLayout");
            var mainLayout = serializedObject.FindProperty("mainLayout");
            var mainFitter = serializedObject.FindProperty("mainFitter");
            var targetFitter = serializedObject.FindProperty("targetFitter");
            var targetRect = serializedObject.FindProperty("targetRect");

            var isInteractable = serializedObject.FindProperty("isInteractable");
            var enableIcon = serializedObject.FindProperty("enableIcon");
            var enableText = serializedObject.FindProperty("enableText");
            var useCustomTextSize = serializedObject.FindProperty("useCustomTextSize");
            var useUINavigation = serializedObject.FindProperty("useUINavigation");
            var navigationMode = serializedObject.FindProperty("navigationMode");
            var wrapAround = serializedObject.FindProperty("wrapAround");
            var selectOnUp = serializedObject.FindProperty("selectOnUp");
            var selectOnDown = serializedObject.FindProperty("selectOnDown");
            var selectOnLeft = serializedObject.FindProperty("selectOnLeft");
            var selectOnRight = serializedObject.FindProperty("selectOnRight");
            var checkForDoubleClick = serializedObject.FindProperty("checkForDoubleClick");
            var useLocalization = serializedObject.FindProperty("useLocalization");
            var useSounds = serializedObject.FindProperty("useSounds");
            var doubleClickPeriod = serializedObject.FindProperty("doubleClickPeriod");
            var fadingMultiplier = serializedObject.FindProperty("fadingMultiplier");
            var useCustomContent = serializedObject.FindProperty("useCustomContent");
            var bypassUpdateOnEnable = serializedObject.FindProperty("bypassUpdateOnEnable");

            var onClick = serializedObject.FindProperty("onClick");
            var onDoubleClick = serializedObject.FindProperty("onDoubleClick");
            var onHover = serializedObject.FindProperty("onHover");
            var onLeave = serializedObject.FindProperty("onLeave");

            var rippleParent = serializedObject.FindProperty("rippleParent");
            var useRipple = serializedObject.FindProperty("useRipple");
            var renderOnTop = serializedObject.FindProperty("renderOnTop");
            var centered = serializedObject.FindProperty("centered");
            var rippleShape = serializedObject.FindProperty("rippleShape");
            var speed = serializedObject.FindProperty("speed");
            var maxSize = serializedObject.FindProperty("maxSize");
            var startColor = serializedObject.FindProperty("startColor");
            var transitionColor = serializedObject.FindProperty("transitionColor");

            var hoverEffect = serializedObject.FindProperty("hoverEffect");
            var heSpeed = serializedObject.FindProperty("heSpeed");
            var heShape = serializedObject.FindProperty("heShape");
            var heTransitionAlpha = serializedObject.FindProperty("heTransitionAlpha");
            var useHoverEffect = serializedObject.FindProperty("useHoverEffect");
            var heSize = serializedObject.FindProperty("heSize");

            switch (buttonTarget.latestTabIndex)
            {
                case 0:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);

                    if (useCustomContent.boolValue == false)
                    {
                        if (buttonTarget.normalImageObj != null)
                        {
                            GUILayout.BeginVertical(EditorStyles.helpBox);
                            GUILayout.Space(-3);

                            enableIcon.boolValue = DreamOSEditorHandler.DrawTogglePlain(enableIcon.boolValue, customSkin, "Enable Icon");

                            GUILayout.Space(4);

                            if (enableIcon.boolValue == true)
                            {
                                DreamOSEditorHandler.DrawPropertyCW(buttonIcon, customSkin, "Button Icon", 80);
                                DreamOSEditorHandler.DrawPropertyCW(iconScale, customSkin, "Icon Scale", 80);
                                if (enableText.boolValue == true) { DreamOSEditorHandler.DrawPropertyCW(spacing, customSkin, "Spacing", 80); }
                            }

                            GUILayout.EndVertical();
                        }

                        if (buttonTarget.normalTextObj != null)
                        {
                            GUILayout.BeginVertical(EditorStyles.helpBox);
                            GUILayout.Space(-3);

                            enableText.boolValue = DreamOSEditorHandler.DrawTogglePlain(enableText.boolValue, customSkin, "Enable Text");

                            GUILayout.Space(4);

                            if (enableText.boolValue == true)
                            {
                                DreamOSEditorHandler.DrawPropertyCW(buttonText, customSkin, "Button Text", 80);
                                if (useCustomTextSize.boolValue == false) { DreamOSEditorHandler.DrawPropertyCW(textSize, customSkin, "Text Size", 80); }
                            }

                            GUILayout.EndVertical();
                        }

                        if (Application.isPlaying == false) { buttonTarget.UpdateUI(); }
                    }

                    else { EditorGUILayout.HelpBox("'Use Custom Content' is enabled. Content is now managed manually.", MessageType.Info); }

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);

                    autoFitContent.boolValue = DreamOSEditorHandler.DrawTogglePlain(autoFitContent.boolValue, customSkin, "Auto-Fit Content", "Sets the width based on the button content.");

                    GUILayout.Space(4);

                    if (autoFitContent.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.PropertyField(padding, new GUIContent(" Padding"), true);
                        EditorGUI.indentLevel = 0;
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();

                    isInteractable.boolValue = DreamOSEditorHandler.DrawToggle(isInteractable.boolValue, customSkin, "Is Interactable");

                    if (Application.isPlaying == true && GUILayout.Button("Update UI", customSkin.button)) { buttonTarget.UpdateUI(); }

                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onClick, new GUIContent("On Click"), true);
                    EditorGUILayout.PropertyField(onDoubleClick, new GUIContent("On Double Click"), true);
                    EditorGUILayout.PropertyField(onHover, new GUIContent("On Hover"), true);
                    EditorGUILayout.PropertyField(onLeave, new GUIContent("On Leave"), true);
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(normalCG, customSkin, "Normal CG");
                    DreamOSEditorHandler.DrawProperty(highlightCG, customSkin, "Highlight CG");
                    DreamOSEditorHandler.DrawProperty(pressedCG, customSkin, "Pressed CG");
                    DreamOSEditorHandler.DrawProperty(disabledCG, customSkin, "Disabled CG");

                    if (enableText.boolValue == true)
                    {
                        DreamOSEditorHandler.DrawProperty(normalTextObj, customSkin, "Normal Text");
                        DreamOSEditorHandler.DrawProperty(highlightTextObj, customSkin, "Highlighted Text");
                        DreamOSEditorHandler.DrawProperty(pressedTextObj, customSkin, "Pressed Text");
                        DreamOSEditorHandler.DrawProperty(disabledTextObj, customSkin, "Disabled Text");
                    }

                    if (enableIcon.boolValue == true)
                    {
                        DreamOSEditorHandler.DrawProperty(normalImageObj, customSkin, "Normal Icon");
                        DreamOSEditorHandler.DrawProperty(highlightImageObj, customSkin, "Highlight Icon");
                        DreamOSEditorHandler.DrawProperty(pressedImageObj, customSkin, "Pressed Icon");
                        DreamOSEditorHandler.DrawProperty(disabledImageObj, customSkin, "Disabled Icon");
                    }

                    DreamOSEditorHandler.DrawProperty(disabledLayout, customSkin, "Disabled Layout");
                    DreamOSEditorHandler.DrawProperty(normalLayout, customSkin, "Normal Layout");
                    DreamOSEditorHandler.DrawProperty(highlightedLayout, customSkin, "Highlighted Layout");
                    DreamOSEditorHandler.DrawProperty(pressedLayout, customSkin, "Pressed Layout");

                    if (autoFitContent.boolValue == true)
                    {
                        DreamOSEditorHandler.DrawProperty(mainLayout, customSkin, "Main Layout");
                        DreamOSEditorHandler.DrawProperty(mainFitter, customSkin, "Main Fitter");
                        DreamOSEditorHandler.DrawProperty(targetFitter, customSkin, "Target Fitter");
                        DreamOSEditorHandler.DrawProperty(targetRect, customSkin, "Target Rect");
                    }

                    if (useRipple.boolValue == true) { DreamOSEditorHandler.DrawProperty(rippleParent, customSkin, "Ripple Parent"); }
                    if (useHoverEffect.boolValue == true) { DreamOSEditorHandler.DrawProperty(hoverEffect, customSkin, "Hover Effect"); }

                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    DreamOSEditorHandler.DrawProperty(fadingMultiplier, customSkin, "Fading Multiplier", "Set the animation fade multiplier.");
                    DreamOSEditorHandler.DrawProperty(doubleClickPeriod, customSkin, "Double Click Period");
                    isInteractable.boolValue = DreamOSEditorHandler.DrawToggle(isInteractable.boolValue, customSkin, "Is Interactable");
                    bypassUpdateOnEnable.boolValue = DreamOSEditorHandler.DrawToggle(bypassUpdateOnEnable.boolValue, customSkin, "Bypass Update On Enable");
                    useCustomContent.boolValue = DreamOSEditorHandler.DrawToggle(useCustomContent.boolValue, customSkin, "Use Custom Content", "Bypasses inspector values and allows manual editing.");
                    if (useCustomContent.boolValue == true || enableText.boolValue == false) { GUI.enabled = false; }
                    useCustomTextSize.boolValue = DreamOSEditorHandler.DrawToggle(useCustomTextSize.boolValue, customSkin, "Use Custom Text Size");
                    useLocalization.boolValue = DreamOSEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Bypasses localization functions when disabled.");
                    GUI.enabled = true;
                    checkForDoubleClick.boolValue = DreamOSEditorHandler.DrawToggle(checkForDoubleClick.boolValue, customSkin, "Check For Double Click");
                    useSounds.boolValue = DreamOSEditorHandler.DrawToggle(useSounds.boolValue, customSkin, "Use Button Sounds");
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);

                    useUINavigation.boolValue = DreamOSEditorHandler.DrawTogglePlain(useUINavigation.boolValue, customSkin, "Use UI Navigation", "Enables controller navigation.");

                    GUILayout.Space(4);

                    if (useUINavigation.boolValue == true)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        DreamOSEditorHandler.DrawPropertyPlain(navigationMode, customSkin, "Navigation Mode");

                        if (buttonTarget.navigationMode == UnityEngine.UI.Navigation.Mode.Horizontal)
                        {
                            EditorGUI.indentLevel = 1;
                            wrapAround.boolValue = DreamOSEditorHandler.DrawToggle(wrapAround.boolValue, customSkin, "Wrap Around");
                            EditorGUI.indentLevel = 0;
                        }

                        else if (buttonTarget.navigationMode == UnityEngine.UI.Navigation.Mode.Vertical)
                        {
                            wrapAround.boolValue = DreamOSEditorHandler.DrawTogglePlain(wrapAround.boolValue, customSkin, "Wrap Around");
                        }

                        else if (buttonTarget.navigationMode == UnityEngine.UI.Navigation.Mode.Explicit)
                        {
                            EditorGUI.indentLevel = 1;
                            DreamOSEditorHandler.DrawPropertyPlain(selectOnUp, customSkin, "Select On Up");
                            DreamOSEditorHandler.DrawPropertyPlain(selectOnDown, customSkin, "Select On Down");
                            DreamOSEditorHandler.DrawPropertyPlain(selectOnLeft, customSkin, "Select On Left");
                            DreamOSEditorHandler.DrawPropertyPlain(selectOnRight, customSkin, "Select On Right");
                            EditorGUI.indentLevel = 0;
                        }

                        GUILayout.EndVertical();
                    }

                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-2);           
                    useRipple.boolValue = DreamOSEditorHandler.DrawTogglePlain(useRipple.boolValue, customSkin, "Use Ripple");                
                    GUILayout.Space(4);

                    if (useRipple.boolValue == true)
                    {
                        renderOnTop.boolValue = DreamOSEditorHandler.DrawToggle(renderOnTop.boolValue, customSkin, "Render On Top");
                        centered.boolValue = DreamOSEditorHandler.DrawToggle(centered.boolValue, customSkin, "Centered");
                        DreamOSEditorHandler.DrawProperty(rippleShape, customSkin, "Shape");
                        DreamOSEditorHandler.DrawProperty(speed, customSkin, "Speed");
                        DreamOSEditorHandler.DrawProperty(maxSize, customSkin, "Max Size");
                        DreamOSEditorHandler.DrawProperty(startColor, customSkin, "Start Color");
                        DreamOSEditorHandler.DrawProperty(transitionColor, customSkin, "Transition Color");
                    }

                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-2);
                    useHoverEffect.boolValue = DreamOSEditorHandler.DrawTogglePlain(useHoverEffect.boolValue, customSkin, "Use Hover Effect");
                    GUILayout.Space(4);

                    if (useHoverEffect.boolValue == true)
                    {
                        DreamOSEditorHandler.DrawProperty(heShape, customSkin, "Shape");
                        DreamOSEditorHandler.DrawProperty(heSpeed, customSkin, "Transition Speed");
                        DreamOSEditorHandler.DrawProperty(heSize, customSkin, "Shape Size");
                        DreamOSEditorHandler.DrawProperty(heTransitionAlpha, customSkin, "Transition Alpha");
                    }

                    GUILayout.EndVertical();
                    buttonTarget.UpdateUI();
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif