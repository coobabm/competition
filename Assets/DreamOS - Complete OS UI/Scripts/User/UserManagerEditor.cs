#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(UserManager))]
    public class UserManagerEditor : Editor
    {
        private UserManager userTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            userTarget = (UserManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            DreamOSEditorHandler.DrawComponentHeader(customSkin, "TopHeader_UserManager");

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

            var minNameCharacter = serializedObject.FindProperty("minNameCharacter");
            var maxNameCharacter = serializedObject.FindProperty("maxNameCharacter");
            var minPasswordCharacter = serializedObject.FindProperty("minPasswordCharacter");
            var maxPasswordCharacter = serializedObject.FindProperty("maxPasswordCharacter");

            var systemUsername = serializedObject.FindProperty("systemUsername");
            var systemLastname = serializedObject.FindProperty("systemLastname");
            var systemPassword = serializedObject.FindProperty("systemPassword");
            var systemSecurityQuestion = serializedObject.FindProperty("systemSecurityQuestion");
            var systemSecurityAnswer = serializedObject.FindProperty("systemSecurityAnswer");

            var ppLibrary = serializedObject.FindProperty("ppLibrary");
            var ppItem = serializedObject.FindProperty("ppItem");
            var ppParent = serializedObject.FindProperty("ppParent");
            var ppIndex = serializedObject.FindProperty("ppIndex");
            var saveProfilePicture = serializedObject.FindProperty("saveProfilePicture");

            var onLogin = serializedObject.FindProperty("onLogin");
            var onLock = serializedObject.FindProperty("onLock");
            var onWrongPassword = serializedObject.FindProperty("onWrongPassword");

            var bootManager = serializedObject.FindProperty("bootManager");
            var setupScreen = serializedObject.FindProperty("setupScreen");
            var lockScreen = serializedObject.FindProperty("lockScreen");
            var desktopScreen = serializedObject.FindProperty("desktopScreen");
            var lockScreenPassword = serializedObject.FindProperty("lockScreenPassword");
            var wrongPassError = serializedObject.FindProperty("wrongPassError");
            var lockScreenBlur = serializedObject.FindProperty("lockScreenBlur");
         
            var disableUserCreating = serializedObject.FindProperty("disableUserCreating");
            var disableLockScreen = serializedObject.FindProperty("disableLockScreen");

            switch (currentTab)
            {
                case 0:
                    Color defaultColor = GUI.color;
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Content", 6);

                    if (disableUserCreating.boolValue == false)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField(new GUIContent("Min / Max Name Char"), customSkin.FindStyle("Text"), GUILayout.Width(150));
                        GUILayout.BeginHorizontal();

                        minNameCharacter.intValue = EditorGUILayout.IntSlider(minNameCharacter.intValue, 0, maxNameCharacter.intValue - 1);
                        maxNameCharacter.intValue = EditorGUILayout.IntSlider(maxNameCharacter.intValue, minNameCharacter.intValue + 1, 20);

                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Min / Max Password Char"), customSkin.FindStyle("Text"), GUILayout.Width(150));

                        GUILayout.BeginHorizontal();

                        minPasswordCharacter.intValue = EditorGUILayout.IntSlider(minPasswordCharacter.intValue, 0, maxPasswordCharacter.intValue - 1);
                        maxPasswordCharacter.intValue = EditorGUILayout.IntSlider(maxPasswordCharacter.intValue, minPasswordCharacter.intValue + 1, 20);

                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }

                    else
                    {
                        DreamOSEditorHandler.DrawProperty(systemUsername, customSkin, "Username");
                        DreamOSEditorHandler.DrawProperty(systemLastname, customSkin, "Lastname");
                        DreamOSEditorHandler.DrawProperty(systemPassword, customSkin, "Password");
                        DreamOSEditorHandler.DrawProperty(systemSecurityQuestion, customSkin, "Security Question");
                        DreamOSEditorHandler.DrawProperty(systemSecurityAnswer, customSkin, "Security Answer");
                    }

                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    DreamOSEditorHandler.DrawPropertyCW(ppLibrary, customSkin, "Profile Picture Library", 140);
                    if (disableUserCreating.boolValue == false) { saveProfilePicture.boolValue = DreamOSEditorHandler.DrawToggle(saveProfilePicture.boolValue, customSkin, "Save Profile Picture"); }

                    if (userTarget.ppLibrary != null)
                    {
                        if (userTarget.ppLibrary.pictures.Count != 0)
                        {
                            GUILayout.Space(-3);
                            GUILayout.BeginHorizontal();
                            GUI.backgroundColor = Color.clear;

                            GUILayout.Box(DreamOSEditorHandler.TextureFromSprite(userTarget.ppLibrary.pictures[ppIndex.intValue].pictureSprite), GUILayout.Width(52), GUILayout.Height(52));

                            GUI.backgroundColor = defaultColor;
                            GUILayout.BeginVertical();
                            GUILayout.Space(4);
                            GUI.enabled = false;

                            EditorGUILayout.LabelField(new GUIContent("Selected Profile Picture"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                           
                            GUI.enabled = true;
                            GUILayout.Space(-4);

                            EditorGUILayout.LabelField(new GUIContent(userTarget.ppLibrary.pictures[ppIndex.intValue].pictureID), customSkin.FindStyle("Text"), GUILayout.Width(112));
                            GUILayout.Space(-4);
                            ppIndex.intValue = EditorGUILayout.IntSlider(ppIndex.intValue, 0, userTarget.ppLibrary.pictures.Count - 1);

                            GUILayout.Space(2);
                            GUILayout.EndVertical();
                            GUILayout.EndHorizontal();
                        }

                        else { EditorGUILayout.HelpBox("There is no item in the library.", MessageType.Warning); }
                    }

                    else { EditorGUILayout.HelpBox("Profile Picture Library is missing.", MessageType.Error); }

                    GUILayout.EndVertical();

                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onLogin, new GUIContent("On Login"));
                    EditorGUILayout.PropertyField(onLock, new GUIContent("On Lock"));
                    EditorGUILayout.PropertyField(onWrongPassword, new GUIContent("On Wrong Password"));
                    break;

                case 1:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    DreamOSEditorHandler.DrawProperty(setupScreen, customSkin, "Setup Screen");
                    DreamOSEditorHandler.DrawProperty(bootManager, customSkin, "Boot Manager");
                    DreamOSEditorHandler.DrawProperty(desktopScreen, customSkin, "Desktop Screen");
                    DreamOSEditorHandler.DrawProperty(lockScreen, customSkin, "Lock Screen");
                    DreamOSEditorHandler.DrawProperty(lockScreenPassword, customSkin, "Lock Screen Pass");
                    DreamOSEditorHandler.DrawProperty(wrongPassError, customSkin, "Wrong Pass Error");
                    DreamOSEditorHandler.DrawProperty(lockScreenBlur, customSkin, "Lock Screen Blur");
                    DreamOSEditorHandler.DrawProperty(ppItem, customSkin, "PP Button");
                    DreamOSEditorHandler.DrawProperty(ppParent, customSkin, "PP Parent");          
                    break;

                case 2:
                    DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    disableLockScreen.boolValue = DreamOSEditorHandler.DrawToggle(disableLockScreen.boolValue, customSkin, "Disable Lock Screen", "This option will be bypassed if the user has a password.");
                    disableUserCreating.boolValue = DreamOSEditorHandler.DrawToggle(disableUserCreating.boolValue, customSkin, "Disable User Creation");
                    if (disableUserCreating.boolValue == true) { EditorGUILayout.HelpBox("Disable User Creation is enabled. You can change the default user settings by switching to the first tab.", MessageType.Info); }
                    break;
            }

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif