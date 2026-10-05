using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;

namespace Michsky.DreamOS
{
    public class AppCreator : EditorWindow
    {
        static AppCreator window;
        static string objectPath;
        protected Texture2D bannerTexture;
        protected GUIStyle panelStyle;
        private GUISkin customSkin;

        // Editor variables
        public GameObject emptyApp;
        public GameObject emptyAppWithTabs;
        public GameObject desktopButton;
        public GameObject taskbarShortcut;
        public AppLibrary appLibrary;
        public bool enableNavbar = true;
        public bool enableBlurManager = true;
        public bool createDesktopShortcut = true;
        public bool createTaskbarButton = true;

        // App variables
        public Sprite appIcon;
        public string appName = "App Name";
        public Transform appParent;
        public Transform shortcutParent;
        public Transform taskbarParent;

        [MenuItem("Tools/DreamOS/App Creator", false, 0)]
        public static void ShowWindow()
        {
            window = GetWindow<AppCreator>("DreamOS - App Creator");
            window.minSize = new Vector2(434, 520);
            window.maxSize = new Vector2(434, 520);
        }

        static void GetObjectPath()
        {
            objectPath = AssetDatabase.GetAssetPath(Resources.Load("UI Manager/DreamOS UI Manager"));
            objectPath = objectPath.Replace("Resources/UI Manager/DreamOS UI Manager.asset", "").Trim();
            objectPath = objectPath + "Prefabs/";
        }

        void OnEnable()
        {
            bannerTexture = (Texture2D)Resources.Load("Banner-DAC", typeof(Texture2D));

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        void OnGUI()
        {
            // Custom panel
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.textColor = GUI.skin.label.normal.textColor;
            panelStyle.margin = new RectOffset(15, 15, 0, 15);
            panelStyle.padding = new RectOffset(12, 12, 12, 12);

            // Banner
            GUILayout.BeginHorizontal();
            GUILayout.Label(bannerTexture, GUILayout.Width(428), GUILayout.Height(90));
            GUILayout.Space(-125);
            GUILayout.BeginVertical();
            GUILayout.Space(54);

            if (GUILayout.Button("Documentation", GUILayout.Width(110), GUILayout.Height(24)))
                Application.OpenURL("https://docs.michsky.com/docs/dreamos/");

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            // Panel
            EditorGUILayout.BeginVertical(panelStyle);

            // App Information
            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("DAC_Info"));
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);

            appIcon = (Sprite)EditorGUILayout.ObjectField(appIcon, typeof(Sprite), true, GUILayout.Width(65), GUILayout.Height(65));

            GUILayout.Space(4);
            GUILayout.BeginVertical();

            EditorGUILayout.LabelField(new GUIContent("App icon and name"), customSkin.FindStyle("Text"), GUILayout.Width(120), GUILayout.Height(20));
            appName = (string)EditorGUILayout.TextField(appName, GUILayout.Height(40));

            GUILayout.EndVertical();
            GUILayout.Space(6);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Settings
            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("DAC_Settings"));
            enableNavbar = DreamOSEditorHandler.DrawToggle(enableNavbar, customSkin, "Enable Navigation Bar");

            if (!EditorPrefs.HasKey("DreamOS.PipelineUpgrader") && GraphicsSettings.defaultRenderPipeline != null) { GUI.enabled = false; }
            enableBlurManager = DreamOSEditorHandler.DrawToggle(enableBlurManager, customSkin, "Enable Blur Effect");
            GUI.enabled = true;
            createDesktopShortcut = DreamOSEditorHandler.DrawToggle(createDesktopShortcut, customSkin, "Create Desktop Shortcut");
            createTaskbarButton = DreamOSEditorHandler.DrawToggle(createTaskbarButton, customSkin, "Create Taskbar Button");

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField(new GUIContent("App Parent"), customSkin.FindStyle("Text"), GUILayout.Width(120));
            appParent = (Transform)EditorGUILayout.ObjectField(appParent, typeof(Transform), true);

            GUILayout.Space(4);

            if (GUILayout.Button("Find Automatically", GUILayout.Width(140)))
            {
                try { appParent = GameObject.Find("Apps & Windows").transform; }
                catch { Debug.Log("<b>[App Creator]</b> Cannot find the app parent."); }
            }

            GUILayout.EndHorizontal();

            if (createDesktopShortcut == false)
                GUI.enabled = false;

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField(new GUIContent("Shortcut Parent"), customSkin.FindStyle("Text"), GUILayout.Width(120));
            shortcutParent = (Transform)EditorGUILayout.ObjectField(shortcutParent, typeof(Transform), true);

            GUILayout.Space(4);

            if (GUILayout.Button("Find Automatically", GUILayout.Width(140)))
            {
                try { shortcutParent = GameObject.Find("Desktop List").transform; }
                catch { Debug.Log("<b>[App Creator]</b> Cannot find the desktop list."); }
            }

            GUILayout.EndHorizontal();
            GUI.enabled = true;

            if (createTaskbarButton == false)
                GUI.enabled = false;

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField(new GUIContent("Taskbar Parent"), customSkin.FindStyle("Text"), GUILayout.Width(120));
            taskbarParent = (Transform)EditorGUILayout.ObjectField(taskbarParent, typeof(Transform), true);

            GUILayout.Space(4);

            if (GUILayout.Button("Find Automatically", GUILayout.Width(140)))
            {
                try { taskbarParent = GameObject.Find("Taskbar/Shortcut List").transform; }
                catch { Debug.Log("<b>[App Creator]</b> Cannot find the taskbar."); }
            }

            GUILayout.EndHorizontal();
            GUI.enabled = true;

            // Status
            GUILayout.Space(30);

            if (appParent == null || createDesktopShortcut == true && shortcutParent == null || createTaskbarButton == true && taskbarParent == null)
                GUILayout.Box(new GUIContent(""), customSkin.FindStyle("DAC_StatusWarning"));
            else if (appParent != null || createDesktopShortcut == true && shortcutParent != null || createTaskbarButton != true && taskbarParent == null)
                GUILayout.Box(new GUIContent(""), customSkin.FindStyle("DAC_StatusOK"));

            GUILayout.BeginVertical();
            GUILayout.Space(-40);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (appParent == null) { GUI.enabled = false; }
            if (createDesktopShortcut == true && shortcutParent == null) { GUI.enabled = false; }
            if (createTaskbarButton == true && taskbarParent == null) { GUI.enabled = false; }

            if (GUILayout.Button("", customSkin.FindStyle("Button_CreateApp")))
            {
                GetObjectPath();

                if (enableNavbar == true) { CreateAppWithNavbar(); }
                else { CreateAppWithoutNavbar(); }
            }

            if (Application.isPlaying == false) { this.Repaint(); }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        void CreateAppWithNavbar()
        {
            AppLibrary.AppItem app = new AppLibrary.AppItem();
            app.appIconBig = appIcon;
            app.appIconMedium = appIcon;
            app.appIconSmall = appIcon;
            app.appTitle = appName;
            appLibrary.apps.Add(app);

            GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + "Apps/Empty App With Navbar"
               + ".prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
            clone.name = appName;
            clone.transform.SetParent(appParent, false);

            Image headerImage = clone.transform.Find("Container/Content/Navigation Panel/Header/Icon").GetComponent<Image>();
            headerImage.sprite = appIcon;

            TextMeshProUGUI headerText = clone.transform.Find("Container/Content/Navigation Panel/Header/Text").GetComponent<TextMeshProUGUI>();
            headerText.text = appName;

            NavbarExpandButton nde = clone.transform.Find("Container/Content/Navigation Panel/Expand & Minimize").GetComponent<NavbarExpandButton>();
            nde.appName = appName;

            WindowManager wManager = clone.GetComponent<WindowManager>();

            if (createDesktopShortcut == true) { CreateDesktopButton(wManager); }
            if (createTaskbarButton == true) { CreateTaskbarButton(wManager); }

            if (enableBlurManager == false) { wManager.useBackgroundBlur = false; }
            else { CreateBlurMaterial(clone); }

            AppElement headerIElement = headerImage.GetComponent<AppElement>();
            headerIElement.elementType = AppElement.ElementType.Icon;
            headerIElement.appID = appName;
            headerIElement.tempAppIndex = appLibrary.apps.Count - 1;
            headerIElement.UpdateElement();

            AppElement headerTElement = headerText.GetComponent<AppElement>();
            headerTElement.elementType = AppElement.ElementType.Title;
            headerTElement.appID = appName;
            headerTElement.tempAppIndex = appLibrary.apps.Count - 1;
            headerTElement.UpdateElement();

            Selection.activeObject = clone;
            Undo.RegisterCreatedObjectUndo(clone, clone.name);
        }

        void CreateAppWithoutNavbar()
        {
            AppLibrary.AppItem app = new AppLibrary.AppItem();
            app.appIconBig = appIcon;
            app.appIconMedium = appIcon;
            app.appIconSmall = appIcon;
            app.appTitle = appName;
            appLibrary.apps.Add(app);

            GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + "Apps/Empty App"
                + ".prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
            clone.name = appName;
            clone.transform.SetParent(appParent, false);

            Image headerImage = clone.transform.Find("Container/Content/App Icon").GetComponent<Image>();
            headerImage.sprite = appIcon;

            TextMeshProUGUI headerText = clone.transform.Find("Container/Content/App Title").GetComponent<TextMeshProUGUI>();
            headerText.text = appName;

            WindowManager wManager = clone.GetComponent<WindowManager>();

            if (createDesktopShortcut == true) { CreateDesktopButton(wManager); }
            if (createTaskbarButton == true) { CreateTaskbarButton(wManager); }

            if (enableBlurManager == false) { wManager.useBackgroundBlur = false; }
            else { CreateBlurMaterial(clone); }

            AppElement headerIElement = headerImage.GetComponent<AppElement>();
            headerIElement.elementType = AppElement.ElementType.Icon;
            headerIElement.iconSize = AppElement.IconSize.Medium;
            headerIElement.appID = appName;
            headerIElement.tempAppIndex = appLibrary.apps.Count - 1;
            headerIElement.UpdateElement();

            AppElement headerTElement = headerText.GetComponent<AppElement>();
            headerTElement.elementType = AppElement.ElementType.Title;
            headerTElement.appID = appName;
            headerTElement.tempAppIndex = appLibrary.apps.Count - 1;
            headerTElement.UpdateElement();

            Selection.activeObject = clone;
            Undo.RegisterCreatedObjectUndo(clone, clone.name);
        }

        void CreateDesktopButton(WindowManager wManager)
        {
            GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + "UI Elements/Button/Desktop Button"
              + ".prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
            clone.name = appName;
            clone.transform.SetParent(shortcutParent, false);

            Image iconObj = clone.transform.Find("Icon Background/Icon").GetComponent<Image>();
            iconObj.sprite = appIcon;

            TextMeshProUGUI textObj = clone.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            textObj.text = appName;

            ButtonManager tmpBtnm = clone.GetComponent<ButtonManager>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(tmpBtnm.onDoubleClick, new UnityEngine.Events.UnityAction(wManager.OpenWindow));

            AppElement iconElement = iconObj.GetComponent<AppElement>();
            iconElement.elementType = AppElement.ElementType.Icon;
            iconElement.iconSize = AppElement.IconSize.Medium;
            iconElement.appID = appName;
            iconElement.tempAppIndex = appLibrary.apps.Count - 1;
            iconElement.UpdateElement();

            AppElement textElement = textObj.GetComponent<AppElement>();
            textElement.elementType = AppElement.ElementType.Title;
            textElement.appID = appName;
            textElement.tempAppIndex = appLibrary.apps.Count - 1;
            textElement.UpdateElement();

            Undo.RegisterCreatedObjectUndo(clone, clone.name);
        }

        void CreateTaskbarButton(WindowManager wManager)
        {
            GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + "UI Elements/Button/Taskbar Button"
              + ".prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
            clone.name = appName;
            clone.transform.SetParent(taskbarParent, false);

            wManager.taskbarButton = clone.GetComponent<TaskbarButton>();

            Image aIconObj = clone.transform.Find("Icon").GetComponent<Image>();
            aIconObj.sprite = appIcon;

            TaskbarButton tbb = clone.GetComponent<TaskbarButton>();
            tbb.buttonTitle = appName;
            tbb.windowManager = wManager;

            wManager.taskbarButton = tbb;

            AppElement tbiElement = aIconObj.GetComponent<AppElement>();
            tbiElement.elementType = AppElement.ElementType.Icon;
            tbiElement.iconSize = AppElement.IconSize.Small;
            tbiElement.appID = appName;
            tbiElement.tempAppIndex = appLibrary.apps.Count - 1;
            tbiElement.UpdateElement();

            AppElement tbbElement = aIconObj.transform.parent.transform.Find("Active/Background").GetComponent<AppElement>();
            tbbElement.elementType = AppElement.ElementType.Gradient;
            tbbElement.appID = appName;
            tbbElement.tempAppIndex = appLibrary.apps.Count - 1;
            tbbElement.UpdateElement();

            AppElement tbsElement = aIconObj.transform.parent.transform.Find("Seperator").GetComponent<AppElement>();
            tbsElement.elementType = AppElement.ElementType.Gradient;
            tbsElement.appID = appName;
            tbsElement.tempAppIndex = appLibrary.apps.Count - 1;
            tbsElement.UpdateElement();

            Undo.RegisterCreatedObjectUndo(clone, clone.name);
        }

        void CreateBlurMaterial(GameObject clone)
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                string matPath = objectPath.Replace("/Prefabs/", "/");
                AssetDatabase.CopyAsset(matPath + "Materials/Blur_NewAppBG.mat", matPath + "Materials/Blur_" + appName + "BG" + ".mat");

                UIBlur blurManager = clone.GetComponent<UIBlur>();
                blurManager.blurMaterial = (Material)AssetDatabase.LoadAssetAtPath(matPath + "Materials/Blur_" + appName + "BG" + ".mat", typeof(Material));

                Image blurImage = clone.transform.Find("Container/Blur BG").GetComponent<Image>();
                blurImage.material = blurManager.blurMaterial;
            }

            else
            {
                UIBlur blurManager = clone.GetComponent<UIBlur>();
                blurManager.blurMaterial = null;
                blurManager.enabled = false;
            }
        }
    }
}