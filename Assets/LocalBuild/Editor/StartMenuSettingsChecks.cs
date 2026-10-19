#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.CaseMenu;
using LingGuangV05.Desktop;
using LingGuangV05.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Isolated real-UI callbacks in EditMode; no runtime bootstrap, normal save IO, or model startup.</summary>
public static class StartMenuSettingsChecks
{
    static int checks;
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); checks++; }

    [MenuItem("LingGuang/Verification/Start menu shared settings")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run in isolated EditMode.");
        if (SceneManager.GetSceneByPath(MainSceneEntry.ScenePath).isLoaded || Object.FindAnyObjectByType<CaseMenuController>() != null)
            throw new InvalidOperationException("Use an empty editor scene; verification opens and closes its own Main scene.");

        string[] keys = { GameSettings.VolumePref, GameSettings.ReduceMotionPref, GameSettings.FullscreenPref };
        bool[] existed = keys.Select(PlayerPrefs.HasKey).ToArray();
        float volumePref = PlayerPrefs.GetFloat(keys[0]);
        int motionPref = PlayerPrefs.GetInt(keys[1]), fullscreenPref = PlayerPrefs.GetInt(keys[2]);
        float listenerVolume = AudioListener.volume;
        bool fullscreen = Screen.fullScreen;
        var activeScene = SceneManager.GetActiveScene();
        Scene scene = default;
        GameObject titleRoot = null;
        IDisposable locale = null;
        checks = 0;
        try
        {
            locale = GameText.Bind(() => GameText.Chinese);
            scene = EditorSceneManager.OpenScene(MainSceneEntry.ScenePath, OpenSceneMode.Additive);
            CheckCompactControlPanelActions(scene);
            var menu = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AeroStartMenu>(true)).Single();
            menu.Close();
            for (int i = 0; i < 3; i++) { menu.Toggle(); menu.Close(); }
            Check(menu.programs.Count(entry => entry.gameSettings) == 1, "Repeated opening adds exactly one settings entry.");
            var entry = menu.programs.Single(item => item.gameSettings);
            Check(entry.row.transform.GetSiblingIndex() == 0, "Settings is first in the scrollable program list.");
            Check(entry.row.GetComponentInChildren<TMP_Text>(true).text == "设置", "Chinese settings label.");
            menu.Toggle(); menu.SetSearch("音量");
            Check(entry.row.activeSelf, "Chinese settings keyword search.");
            menu.SubmitSearch("音量");
            var fallback = menu.GetComponent<DesktopSettingsOverlay>();
            Check(fallback != null, "Direct Main uses the shared HUD fallback.");
            var directHud = (CaseMenuHud)typeof(DesktopSettingsOverlay).GetField("hud", PrivateInstance).GetValue(fallback);
            Check(directHud != null && directHud.IsSettingsOpen && !menu.menu.activeSelf, "Search submit opens settings and closes Start.");
            Check(directHud.transform.parent == null, "Fallback is rooted outside the offscreen LCD canvas.");
            var slider = directHud.GetComponentsInChildren<Slider>(true).Single();
            slider.value = .31f;
            Check(Mathf.Abs(AudioListener.volume - .31f) < .001f && Mathf.Abs(PlayerPrefs.GetFloat(keys[0]) - .31f) < .001f, "Desktop volume uses the existing master key.");
            SetToggle(directHud, "ReduceMotion", true);
            Check(PlayerPrefs.GetInt(keys[1]) == 1, "Desktop reduce motion uses the existing key.");
            SetToggle(directHud, "Fullscreen", !fullscreen);
            Check(PlayerPrefs.GetInt(keys[2]) == (!fullscreen ? 1 : 0), "Desktop fullscreen uses the existing key.");
            CheckHelp(directHud);
            Button(directHud, "Return").onClick.Invoke();
            Check(!directHud.IsSettingsOpen, "Back closes the direct-Main overlay.");
            menu.OpenSettings();
            Check(ReferenceEquals(directHud, typeof(DesktopSettingsOverlay).GetField("hud", PrivateInstance).GetValue(fallback)), "Repeated direct-Main opening reuses one HUD.");
            fallback.Close();
            // This MonoBehaviour does not execute lifecycle callbacks in EditMode.
            // Exercise its real cleanup callback explicitly before removing the fixture component.
            typeof(DesktopSettingsOverlay).GetMethod("OnDestroy", PrivateInstance).Invoke(fallback, null);
            Object.DestroyImmediate(fallback);
            Check(directHud == null, "Fallback cleanup destroys its root overlay.");

            // Inject only the completed-entry state; do not run Awake/Start or load any game save.
            titleRoot = new GameObject("Settings verification title");
            SceneManager.MoveGameObjectToScene(titleRoot, scene);
            var title = titleRoot.AddComponent<CaseMenuController>();
            title.startButton = Child(titleRoot, "Power").AddComponent<CaseMenuButton>();
            title.settingsButton = Child(titleRoot, "Settings").AddComponent<CaseMenuButton>();
            var titleHud = titleRoot.AddComponent<CaseMenuHud>();
            titleHud.Build(TMP_Settings.defaultFontAsset, title.SetVolume, title.SetReducedMotion,
                title.SetFullscreen, title.CloseSettings, null);
            titleHud.SetChromeAlpha(0);
            typeof(CaseMenuController).GetField("hud", PrivateInstance).SetValue(title, titleHud);
            typeof(CaseMenuController).GetField("<HasEnteredGame>k__BackingField", PrivateInstance).SetValue(title, true);
            int sceneCount = SceneManager.sceneCount;
            menu.Toggle(); entry.row.GetComponent<Button>().onClick.Invoke();
            Check(titleHud.IsSettingsOpen && title.IsSettingsOpen, "Desktop button opens the already-built title HUD.");
            Check(menu.GetComponent<DesktopSettingsOverlay>() == null, "Title entry creates no fallback HUD.");
            Check(SceneManager.sceneCount == sceneCount && title.TransitionCount == 0, "Opening settings does not load a scene or transition.");
            titleHud.GetComponentsInChildren<Slider>(true).Single().value = .64f;
            Check(Mathf.Abs(PlayerPrefs.GetFloat(keys[0]) - .64f) < .001f, "Title HUD uses the same volume key.");
            CheckHelp(titleHud);
            Button(titleHud, "Return").onClick.Invoke();
            Check(!titleHud.IsSettingsOpen && title.HasEnteredGame && !title.IsReturningToTitle, "Back returns to the desktop state, never the title.");

            locale.Dispose(); locale = GameText.Bind(() => GameText.English);
            menu.Toggle();
            Check(entry.row.GetComponentInChildren<TMP_Text>(true).text == "Settings", "Reopen updates the English menu label.");
            menu.SetSearch("settings"); menu.SubmitSearch("settings");
            Check(titleHud.IsSettingsOpen, "English search opens the same title HUD.");
            Check(titleHud.GetComponentsInChildren<TMP_Text>(true).Single(label => label.name == "SettingsTitle").text == "Settings", "Reused HUD refreshes English settings labels.");
            title.CloseSettings();
            Debug.Log("START_MENU_SETTINGS_PASS " + checks + ". Actual Main start-menu and HUD callbacks in isolated EditMode; title entered state injected. Not full boot, physical input, native fullscreen, or disk-save acceptance.");
        }
        finally
        {
            if (titleRoot != null) Object.DestroyImmediate(titleRoot);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
            locale?.Dispose();
            if (existed[0]) PlayerPrefs.SetFloat(keys[0], volumePref); else PlayerPrefs.DeleteKey(keys[0]);
            if (existed[1]) PlayerPrefs.SetInt(keys[1], motionPref); else PlayerPrefs.DeleteKey(keys[1]);
            if (existed[2]) PlayerPrefs.SetInt(keys[2], fullscreenPref); else PlayerPrefs.DeleteKey(keys[2]);
            PlayerPrefs.Save(); AudioListener.volume = listenerVolume; Screen.fullScreen = fullscreen;
        }
    }

    static void CheckCompactControlPanelActions(Scene scene)
    {
        var runtime = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ChapterOneRuntime>(true)).Single();
        var saves = runtime.GetComponent<SaveSlotsPanel>() ?? runtime.gameObject.AddComponent<SaveSlotsPanel>();
        typeof(SaveSlotsPanel).GetField("runtime", PrivateInstance).SetValue(saves, runtime);
        typeof(SaveSlotsPanel).GetMethod("Update", PrivateInstance).Invoke(saves, null);
        var window = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LingGuangV05.Desktop.OS.WindowManager>(true)).Single(w => w.name == "Settings");
        var actions = window.windowContent.Find("GameActions") as RectTransform;
        Check(actions != null && actions.parent == window.windowContent, "Save/funds actions are inside the control-panel client area.");
        Canvas.ForceUpdateCanvases();
        var buttons = actions.GetComponentsInChildren<Button>(true);
        Check(buttons.Length == 2, "One compact save button and one compact funds button.");
        foreach (var button in buttons)
        {
            var rect = (RectTransform)button.transform;
            Check(rect.rect.height <= 28.01f && rect.rect.width <= 140.01f, "Control-panel action is compact: " + button.name);
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var corner in corners)
                Check(window.windowContent.rect.Contains(window.windowContent.InverseTransformPoint(corner)), "Action stays within the client area: " + button.name);
        }
    }

    static GameObject Child(GameObject parent, string name)
    { var child = new GameObject(name); child.transform.SetParent(parent.transform); return child; }
    static Button Button(CaseMenuHud hud, string name) => hud.GetComponentsInChildren<Button>(true).Single(button => button.name == name);
    static void SetToggle(CaseMenuHud hud, string name, bool value)
    {
        var toggle = hud.GetComponentsInChildren<Toggle>(true).Single(item => item.name == name);
        toggle.SetIsOnWithoutNotify(!value); toggle.isOn = value;
    }
    static void CheckHelp(CaseMenuHud hud)
    {
        Button(hud, "PlayerHelp").onClick.Invoke();
        Check(hud.IsPlayerHelpOpen && hud.IsSettingsOpen, "Help stays inside settings.");
        hud.ClosePlayerHelp();
        Check(!hud.IsPlayerHelpOpen && hud.IsSettingsOpen, "Closing help returns to settings without closing it.");
    }
}
#endif
