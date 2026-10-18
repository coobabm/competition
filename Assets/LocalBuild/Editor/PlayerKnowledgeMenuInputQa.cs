#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.CaseMenu;
using LingGuangV05.Desktop;
using LingGuangV05.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Run only in the serial Unity owner. Actual menu scene and normal InputSystem/UI dispatch;
// no Button.onClick.Invoke, ExecuteEvents pointer dispatch, or synthetic game/save fixture.
[InitializeOnLoad]
public static class PlayerKnowledgeMenuInputQa
{
    const string RunKey = "PlayerKnowledgeMenuInputQa.Root";
    const string SetupKey = "PlayerKnowledgeMenuInputQa.Setup";
    const string OverrideKey = "PlayerKnowledgeMenuInputQa.PreviousSaveOverride";
    const string DesktopKey = "PlayerKnowledgeMenuInputQa.PreviousDesktopPath";
    const string ExitCodeKey = "PlayerKnowledgeMenuInputQa.ExitCode";
    const string FrameRateKey = "PlayerKnowledgeMenuInputQa.FrameRate";
    const string VSyncKey = "PlayerKnowledgeMenuInputQa.VSync";
    const string BackgroundKey = "PlayerKnowledgeMenuInputQa.Background";
    const string ViewIndexKey = "PlayerKnowledgeMenuInputQa.ViewIndex";
    const string ViewCreatedKey = "PlayerKnowledgeMenuInputQa.ViewCreated";
    const string ViewCustomKey = "PlayerKnowledgeMenuInputQa.ViewCustomIndex";
    const string ViewTagKey = "PlayerKnowledgeMenuInputQa.ViewTag";
    const int ViewWidth = 1280, ViewHeight = 720;
    const BindingFlags ReflectionFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static string Root => SessionState.GetString(RunKey, "");
    static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
    static CaseMenuController menu;
    static CaseMenuHud hud;
    static Transform originalRig;
    static Mouse mouse;
    static Keyboard keyboard;
    static Mouse previousMouse;
    static Keyboard previousKeyboard;
    static readonly List<InputDevice> mutedDevices = new List<InputDevice>();
    static InputSettings originalInputSettings, verificationInputSettings;
    static Vector2 savedPointer, pointer;
    static IDisposable locale;
    static string language = "zh-CN";
    static int lastFrame;
    static double deadline;
    static bool finishing, guardedBeforeStart;
    static Report report;
    [Serializable] sealed class SceneEntry { public string path; public bool loaded, active; }
    [Serializable] sealed class SavedSetup { public List<SceneEntry> scenes = new List<SceneEntry>(); }
    [Serializable] sealed class Report
    {
        public string status = "running", failure = "", unityVersion;
        public string scope = "ComputerCaseMenu PlayMode; queued InputSystem mouse/keyboard events through production Physics hit testing and EventSystem. Language binding is a fixture, not a settings language-selector test. No physical human input, Main preload, gameplay, model, or save test.";
        public string isolation = "sceneLoaded runs after menu Awake and before Start. A runtime-only null displayRig makes the existing Preload argument guard return before loading Main; the reference is restored after the expected return. Independent save/profile overrides are defense in depth. No scene or BuildSettings asset is edited.";
        public int width, height, mouseDeviceId, keyboardDeviceId;
        public string inputDevices = "Temporary virtual Mouse/Keyboard; other Unity input devices muted and restored.";
        public List<string> checks = new List<string>(), screenshots = new List<string>(), errors = new List<string>();
    }

    static PlayerKnowledgeMenuInputQa() { EditorApplication.playModeStateChanged += State; }

    [MenuItem("Tools/Local Verification/Player knowledge menu real input")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Root.Length != 0)
            throw new InvalidOperationException("Run once from clean EditMode; no concurrent Unity QA.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene changes: finish them before this scene-switching QA.");
        string dir = Path.GetFullPath(Path.Combine("LocalVerification", "PlayerKnowledgeMenuInput", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "UnusedSave"));
        Directory.CreateDirectory(Path.Combine(dir, "UnusedDesktopProfile"));
        SessionState.SetString(SetupKey, JsonUtility.ToJson(new SavedSetup { scenes = EditorSceneManager.GetSceneManagerSetup()
            .Select(x => new SceneEntry { path = x.path, loaded = x.isLoaded, active = x.isActive }).ToList() }));
        SessionState.SetString(OverrideKey, ChapterOneRuntime.VerificationSaveDirectory ?? "");
        var pathField = typeof(Michsky.DreamOS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic);
        SessionState.SetString(DesktopKey, pathField?.GetValue(null) as string ?? "");
        SessionState.SetInt(FrameRateKey, Application.targetFrameRate);
        SessionState.SetInt(VSyncKey, QualitySettings.vSyncCount);
        SessionState.SetBool(BackgroundKey, Application.runInBackground);
        SessionState.SetInt(ExitCodeKey, 1);
        SessionState.SetInt(ViewCustomKey, -1);
        SessionState.SetString(RunKey, dir);
        try
        {
            PrepareGameView();
            EditorSceneManager.OpenScene(CaseMenuController.ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error)
        {
            try { RestoreEditor(); }
            catch (Exception cleanup) { WriteCleanupFailure(dir, cleanup); }
            finally { if (Application.isBatchMode) EditorApplication.Exit(1); }
            throw new InvalidOperationException("Could not prepare isolated menu input QA", error);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if (Root.Length == 0) return;
        Application.targetFrameRate = 30; QualitySettings.vSyncCount = 0; Application.runInBackground = true;
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(Root, "UnusedSave");
        typeof(Michsky.DreamOS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, Path.Combine(Root, "UnusedDesktopProfile") + Path.DirectorySeparatorChar);
        guardedBeforeStart = false;
        SceneManager.sceneLoaded -= GuardPreload;
        SceneManager.sceneLoaded += GuardPreload;
    }

    static void GuardPreload(Scene scene, LoadSceneMode mode)
    {
        if (Root.Length == 0 || scene.path != CaseMenuController.ScenePath) return;
        menu = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<CaseMenuController>(true)).Single();
        hud = menu.GetComponent<CaseMenuHud>();
        // Awake has built the real HUD. Start has not yet entered Preload.
        if (hud == null || menu.displayRig == null) throw new InvalidOperationException("Expected menu Awake-before-Start isolation point missing.");
        originalRig = menu.displayRig;
        menu.displayRig = null;
        guardedBeforeStart = true;
    }

    static void State(PlayModeStateChange state)
    {
        if (Root.Length == 0) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.targetFrameRate = 30; QualitySettings.vSyncCount = 0; Application.runInBackground = true;
            report = new Report { unityVersion = Application.unityVersion };
            finishing = false; lastFrame = -1; deadline = EditorApplication.timeSinceStartup + 180;
            routines.Clear(); routines.Push(FramesAndChecks());
            Application.logMessageReceived += Log;
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Reload;
            Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x => x.GetType().FullName == "UnityEditor.GameView")?.Focus();
            Save();
        }
        if (state == PlayModeStateChange.ExitingPlayMode && !finishing)
        {
            if (report != null) { report.status = "interrupted"; report.failure = "PlayMode stopped before completion"; Save(); }
            CleanupPlay();
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            string dir = Root;
            bool ok = SessionState.GetInt(ExitCodeKey, 1) == 0 && File.Exists(Path.Combine(dir, "passed.txt"));
            try
            {
                if (!CleanupPlay()) ok = false;
                RestoreEditor();
            }
            catch (Exception error)
            {
                ok = false; WriteCleanupFailure(dir, error);
                try { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
                catch (Exception fallback) { WriteCleanupFailure(dir, fallback); }
            }
            finally
            {
                Debug.Log("PLAYER_KNOWLEDGE_MENU_INPUT " + (ok ? "PASS" : "FAIL/INTERRUPTED") + ": " + dir);
                if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
            }
        }
    }

    static IEnumerator FramesAndChecks()
    {
        yield return Frames(4);
        Check(guardedBeforeStart && menu != null && hud != null, "real menu Awake-to-Start guard executed");
        Check(menu.displayRig == null && !string.IsNullOrEmpty(menu.LastError), "Preload returned through expected invalid-rig guard");
        Check(!menu.IsMainReady && menu.GetComponent<CaseMenuMainDisplay>().Runtime == null, "no real desktop runtime initialized");
        menu.displayRig = originalRig;
        AssertIsolation();
        Check(menu.isActiveAndEnabled && menu.menuCamera.isActiveAndEnabled, "production menu controller and camera remain active");
        SetupVirtualInput();
        Check(mouse.added && keyboard.added && mouse.enabled && keyboard.enabled
            && !mouse.leftButton.isPressed && !keyboard.anyKey.isPressed, "isolated virtual mouse and keyboard are available");
        report.mouseDeviceId = mouse.deviceId; report.keyboardDeviceId = keyboard.deviceId;
        savedPointer = pointer = mouse.position.ReadValue();
        Check(EventSystem.current != null && EventSystem.current.currentInputModule != null, "scene EventSystem and normal input module are active");
        // Fixed GameView pixels must be real before rays and overlay coordinates are tested.
        // Do not replace this with a camera RenderTexture or fake 1x1 screen coordinates.
        double pixelsDeadline = EditorApplication.timeSinceStartup + 12;
        while ((Screen.width != ViewWidth || Screen.height != ViewHeight) && EditorApplication.timeSinceStartup < pixelsDeadline)
        { RepaintGameView(); yield return null; }
        report.width = Screen.width; report.height = Screen.height;
        Check(report.width == ViewWidth && report.height == ViewHeight,
            "fixed GameView pixels established: " + report.width + "x" + report.height + "; a graphics-capable Editor is required");
        Check(menu.menuCamera.pixelWidth == ViewWidth && menu.menuCamera.pixelHeight == ViewHeight, "menu camera and GameView share real pixel dimensions");
        locale = GameText.Bind(() => language);
        // The failed-preload status is isolation scaffolding, not product copy under test.
        hud.SetLoading(-1, null);
        hud.SetHint(GameText.T("帮助输入验证：未加载游戏进度", "Help input verification: gameplay was not loaded"));
        Pointer(new Vector2(24, 24), false); Keys(); yield return Frames(3);
        foreach (bool english in new[] { false, true })
        {
            yield return OpenSettingsByRay();
            yield return Click(Button("PlayerHelp"));
            Check(hud.IsPlayerHelpOpen && hud.IsSettingsOpen, "Help opens through EventSystem pointer input");
            Check(EventSystem.current.currentSelectedGameObject == Button("PlayerHelpBack").gameObject, "opening help selects its visible Back control");
            // Switch while the help is already open, exercising production LateUpdate reflow.
            language = english ? GameText.English : "zh-CN";
            GameText.PublishChanged(); yield return Frames(4);
            string tag = english ? "en" : "zh";
            Check(Text("Audience").text == PlayerKnowledgeCopy.Audience(english), "live language switch updates explicit player audience: " + tag);
            Check(Text("FormalExplanations").text.Contains(PlayerKnowledgeCopy.Title1(english))
                && Text("FormalExplanations").text.Contains(PlayerKnowledgeCopy.Title2(english)), "both formal texts use selected language: " + tag);
            Check(menu.GetComponentsInChildren<Button>(true).Any(x => x.name == "SixStages"), "approved K04 optional reading is present");
            Check(Text("FormalExplanations").text.Contains(PlayerKnowledgeCopy.Summary4(english)), "approved six-stage summary uses selected language: " + tag);
            foreach (string body in BodyNames) Check(!Text(body).gameObject.activeSelf, "optional reading starts collapsed: " + body + " " + tag);
            yield return Capture(tag + "-01-help-top");
            var scroll = Scroll();
            float before = scroll.content.anchoredPosition.y;
            Pointer(ScreenPoint(scroll.viewport), false, new Vector2(0, -120)); yield return Frames(3);
            Pointer(pointer, false); yield return Frames(3);
            Check(scroll.content.anchoredPosition.y > before + 1, "queued mouse wheel scrolls real clipped content: " + tag);
            yield return Capture(tag + "-02-wheel-scroll");
            for (int i = 0; i < EntryNames.Length; i++)
            {
                var button = Button(EntryNames[i]);
                yield return BringIntoView((RectTransform)button.transform);
                yield return Click(button);
                Check(Text(BodyNames[i]).gameObject.activeInHierarchy, "pointer expands " + EntryNames[i] + " " + tag);
                yield return Capture(tag + "-03-expanded-" + i);
                var body = Text(BodyNames[i]).rectTransform;
                float bottom = -body.anchoredPosition.y + body.rect.height;
                yield return DragToTop(bottom - scroll.viewport.rect.height + 4);
                Check(body.GetWorldCornersVisibleBottom(scroll.viewport), "end of expanded explanation is reachable: " + EntryNames[i] + " " + tag);
                yield return Capture(tag + "-04-bottom-" + i);
                yield return BringIntoView((RectTransform)button.transform);
                yield return Click(button);
                Check(!Text(BodyNames[i]).gameObject.activeSelf, "pointer collapses " + EntryNames[i] + " " + tag);
            }
            yield return Click(Button("PlayerHelpBack"));
            Check(!hud.IsPlayerHelpOpen && hud.IsSettingsOpen, "Back pointer returns to settings: " + tag);
            yield return Click(Button("PlayerHelp"));
            Check(hud.IsPlayerHelpOpen && Scroll().verticalNormalizedPosition > .99f, "reopen returns to the top: " + tag);
            foreach (string body in BodyNames) Check(!Text(body).gameObject.activeSelf, "reopen resets optional reading: " + body + " " + tag);
            Keys(Key.Escape); yield return Frames(3); Keys(); yield return Frames(3);
            Check(!hud.IsPlayerHelpOpen && hud.IsSettingsOpen, "first queued Escape returns to settings: " + tag);
            Check(EventSystem.current.currentSelectedGameObject == Button("PlayerHelp").gameObject, "Escape restores visible Help focus: " + tag);
            yield return Capture(tag + "-05-escape-settings");
            Keys(Key.Escape); yield return Frames(3); Keys(); yield return Frames(3);
            Check(!hud.IsSettingsOpen && !hud.IsPlayerHelpOpen, "second queued Escape closes settings: " + tag);
            yield return Capture(tag + "-06-escape-menu");
            AssertIsolation();
        }
        Check(!Directory.EnumerateFiles(Path.Combine(Root, "UnusedSave"), "*", SearchOption.AllDirectories).Any(), "no isolated game save was created either");
        Pointer(savedPointer, false); Keys(); yield return Frames(3);
    }

    static readonly string[] EntryNames = { "GpuSupplement", "SixStages", "TrainingWorkflow", "ContextAndTraining" };
    static readonly string[] BodyNames = { "GpuExplanation", "SixStagesExplanation", "TrainingWorkflowExplanation", "ContextAndTrainingExplanation" };
    static Button Button(string name) => menu.GetComponentsInChildren<Button>(true).Single(x => x.name == name);
    static TMP_Text Text(string name) => menu.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == name);
    static ScrollRect Scroll() => menu.GetComponentsInChildren<ScrollRect>(true).Single(x => x.name == "PlayerHelpViewport");
    static Vector2 ScreenPoint(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));

    static IEnumerator OpenSettingsByRay()
    {
        Vector3 p = menu.menuCamera.WorldToScreenPoint(menu.settingsButton.transform.position + Vector3.back * .025f);
        Check(p.z > 0 && p.x > 0 && p.x < Screen.width && p.y > 0 && p.y < Screen.height, "settings key projects inside GameView");
        Check(menu.HitButton(p) == menu.settingsButton, "production Physics ray resolves the actual settings key");
        Pointer(p, false); yield return Frames(3);
        Pointer(p, true); yield return Frames(3);
        Check(menu.settingsButton.IsPressed && !menu.IsSettingsOpen, "actual key is depressed but settings waits for release");
        Pointer(p, false); yield return Frames(4);
        Check(menu.IsSettingsOpen, "release of the actual settings key opens settings");
    }

    static void AssertHit(RectTransform target, Vector2 point)
    {
        Check(RectTransformUtility.RectangleContainsScreenPoint(target, point, null), "target contains queued pointer: " + target.name);
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        var hit = hits.FirstOrDefault(x => x.gameObject != null).gameObject;
        Check(hit != null && (hit.transform == target || hit.transform.IsChildOf(target)), "top EventSystem raycast reaches " + target.name);
    }

    static IEnumerator Click(Button button)
    {
        Check(button.isActiveAndEnabled && button.IsInteractable(), "visible enabled input target: " + button.name);
        var rect = (RectTransform)button.transform;
        Vector2 point = ScreenPoint(rect);
        AssertHit(rect, point);
        Pointer(point, false); yield return Frames(2);
        Pointer(point, true); yield return Frames(2);
        Pointer(point, false); yield return Frames(4);
    }

    static IEnumerator BringIntoView(RectTransform target)
    {
        yield return DragToTop(-target.anchoredPosition.y - 12);
        Check(RectTransformUtility.RectangleContainsScreenPoint(Scroll().viewport, ScreenPoint(target), null), "scrollbar drag exposes button " + target.name);
    }

    static IEnumerator DragToTop(float desiredTop)
    {
        var scroll = Scroll(); var scrollbar = scroll.verticalScrollbar;
        var track = (RectTransform)scrollbar.transform;
        float range = Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height);
        float desired = range > 0 ? 1 - Mathf.Clamp01(desiredTop / range) : 1;
        var handle = scrollbar.handleRect;
        Vector2 start = ScreenPoint(handle);
        AssertHit(track, start);
        float half = handle.rect.height * .5f;
        Vector3 endLocal = new Vector3(track.rect.center.x, track.rect.yMin + half + desired * Mathf.Max(0, track.rect.height - handle.rect.height), 0);
        Vector2 end = RectTransformUtility.WorldToScreenPoint(null, track.TransformPoint(endLocal));
        Pointer(start, false); yield return Frames(2);
        Pointer(start, true); yield return Frames(2);
        Pointer(end, true); yield return Frames(3);
        Pointer(end, false); yield return Frames(3);
        Check(Mathf.Abs(scroll.verticalNormalizedPosition - desired) < .03f, "real scrollbar drag reaches requested reading position");
    }

    static bool GetWorldCornersVisibleBottom(this RectTransform text, RectTransform viewport)
    {
        Vector3[] corners = new Vector3[4]; text.GetWorldCorners(corners);
        Vector2 local = viewport.InverseTransformPoint((corners[0] + corners[3]) * .5f);
        return local.y >= viewport.rect.yMin - 5 && local.y <= viewport.rect.yMax + 5;
    }

    static void Pointer(Vector2 point, bool pressed, Vector2 scroll = default)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point, delta = point - pointer, scroll = scroll }.WithButton(MouseButton.Left, pressed));
        pointer = point;
    }
    static void Keys(params Key[] keys)
    { keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    static IEnumerator Frames(int count) { while (count-- > 0) yield return null; }
    static IEnumerator Capture(string name)
    {
        yield return Frames(2);
        string path = Path.Combine(Root, name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        double limit = EditorApplication.timeSinceStartup + 8;
        while ((!File.Exists(path) || new FileInfo(path).Length <= 100) && EditorApplication.timeSinceStartup < limit) yield return null;
        Check(File.Exists(path) && new FileInfo(path).Length > 100, "actual GameView screenshot saved: " + name);
        report.screenshots.Add(path); Save();
    }

    static void AssertIsolation()
    {
        var main = SceneManager.GetSceneByPath(MainSceneEntry.ScenePath);
        Check(!main.IsValid() || !main.isLoaded, "Main scene was never loaded");
        Check(Object.FindObjectsByType<ChapterOneRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0, "no player-save runtime exists");
        Check(!menu.HasEnteredGame && menu.TransitionCount == 0 && !menu.IsTransitioning, "no power-on transition or gameplay entry occurred");
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || finishing) return;
        try
        {
            // Check even if batch GameView stops producing frames.
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Actual-menu input QA exceeded 180 seconds");
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            if (report.errors.Count != 0) throw new InvalidOperationException(report.errors[0]);
            if (Object.FindObjectsByType<ChapterOneRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Isolation violation: game runtime unexpectedly loaded");
            while (routines.Count != 0)
            {
                var routine = routines.Peek();
                if (!routine.MoveNext()) { routines.Pop(); continue; }
                if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                return;
            }
            report.status = "passed"; Save(); File.WriteAllText(Path.Combine(Root, "passed.txt"), "Actual menu input checks passed; screenshots require visual review.");
            Finish();
        }
        catch (Exception e) { report.status = "failed"; report.failure = e.ToString(); Save(); Finish(); }
    }
    static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        report.checks.Add(label); Save();
    }
    static void Log(string message, string stack, LogType type)
    { if (!finishing && report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.errors.Add(message + "\n" + stack); }
    static void Save()
    {
        if (report == null || Root.Length == 0) return;
        try { File.WriteAllText(Path.Combine(Root, "report.json"), JsonUtility.ToJson(report, true)); }
        catch (Exception error)
        { report.status = "failed"; report.failure += "\nReport write failed: " + error.Message; SessionState.SetInt(ExitCodeKey, 1); WriteCleanupFailure(Root, error); }
    }
    static void Reload()
    {
        if (Root.Length == 0 || finishing) return;
        if (report != null) { report.status = "interrupted"; report.failure = "Assembly reload interrupted input QA"; Save(); }
        Finish();
    }
    static void Finish()
    {
        finishing = true;
        SessionState.SetInt(ExitCodeKey, report != null && report.status == "passed" ? 0 : 1);
        try
        {
            if (!CleanupPlay())
            {
                SessionState.SetInt(ExitCodeKey, 1);
                if (report != null) { report.status = "failed"; report.failure += "\nInput/Editor cleanup failed"; Save(); }
            }
        }
        finally
        {
            try { EditorApplication.ExitPlaymode(); }
            catch (Exception error)
            {
                WriteCleanupFailure(Root, error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else EditorApplication.isPlaying = false;
            }
        }
    }

    static bool CleanupPlay()
    {
        bool ok = true;
        void Restore(Action action)
        {
            try { action(); }
            catch (Exception error) { ok = false; SessionState.SetInt(ExitCodeKey, 1); WriteCleanupFailure(Root, error); }
        }
        EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
        AssemblyReloadEvents.beforeAssemblyReload -= Reload; SceneManager.sceneLoaded -= GuardPreload;
        InputSystem.onDeviceChange -= MuteExternalDevice;
        Restore(() => { if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse); });
        Restore(() => { if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); });
        mouse = null; keyboard = null;
        foreach (var device in mutedDevices.ToArray())
            Restore(() => { if (device != null && device.added) InputSystem.EnableDevice(device); });
        mutedDevices.Clear();
        Restore(() => { if (previousMouse != null && previousMouse.added && previousMouse.enabled) previousMouse.MakeCurrent(); });
        Restore(() => { if (previousKeyboard != null && previousKeyboard.added && previousKeyboard.enabled) previousKeyboard.MakeCurrent(); });
        previousMouse = null; previousKeyboard = null;
        Restore(() => { if (originalInputSettings != null) InputSystem.settings = originalInputSettings; });
        Restore(() => { if (verificationInputSettings != null) Object.DestroyImmediate(verificationInputSettings); });
        originalInputSettings = null; verificationInputSettings = null;
        Restore(() => { if (menu != null && originalRig != null) menu.displayRig = originalRig; });
        Restore(() => locale?.Dispose()); locale = null; routines.Clear();
        Restore(RestorePerformance);
        return ok;
    }

    static void SetupVirtualInput()
    {
        originalInputSettings = InputSystem.settings;
        verificationInputSettings = Object.Instantiate(originalInputSettings);
        verificationInputSettings.hideFlags = HideFlags.HideAndDontSave;
        verificationInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        verificationInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        verificationInputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        InputSystem.settings = verificationInputSettings;
        previousMouse = Mouse.current; previousKeyboard = Keyboard.current;
        // Mute devices only inside Unity. No OS pointer movement and no events sent to physical devices.
        mutedDevices.Clear();
        foreach (var device in InputSystem.devices.Where(x => x.enabled).ToArray())
        { mutedDevices.Add(device); InputSystem.DisableDevice(device); }
        mouse = InputSystem.AddDevice<Mouse>("PlayerKnowledgeQaMouse");
        keyboard = InputSystem.AddDevice<Keyboard>("PlayerKnowledgeQaKeyboard");
        mouse.MakeCurrent(); keyboard.MakeCurrent();
        InputSystem.onDeviceChange += MuteExternalDevice;
    }

    static void MuteExternalDevice(InputDevice device, InputDeviceChange change)
    {
        if (change != InputDeviceChange.Added && change != InputDeviceChange.Enabled && change != InputDeviceChange.Reconnected) return;
        if (device == mouse || device == keyboard || finishing || !device.enabled) return;
        if (!mutedDevices.Contains(device)) mutedDevices.Add(device);
        InputSystem.DisableDevice(device);
    }

    static object ViewSizeGroup()
    {
        var assembly = typeof(EditorWindow).Assembly;
        var type = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(type);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        return type.GetMethod("GetGroup").Invoke(sizes, new object[] { GameViewSizeGroupType.Standalone });
    }

    static void PrepareGameView()
    {
        var assembly = typeof(EditorWindow).Assembly;
        var viewType = assembly.GetType("UnityEditor.GameView");
        bool exists = Resources.FindObjectsOfTypeAll<EditorWindow>().Any(x => x.GetType() == viewType);
        var view = EditorWindow.GetWindow(viewType);
        SessionState.SetBool(ViewCreatedKey, !exists);
        var selected = viewType.GetProperty("selectedSizeIndex", ReflectionFlags);
        SessionState.SetInt(ViewIndexKey, (int)selected.GetValue(view));
        var group = ViewSizeGroup();
        int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        int builtIn = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
        string tag = "PlayerKnowledgeQA-" + DateTime.UtcNow.Ticks;
        SessionState.SetString(ViewTagKey, tag);
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var modeType = assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(modeType, "FixedResolution"), ViewWidth, ViewHeight, tag });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        SessionState.SetInt(ViewCustomKey, total - builtIn);
        selected.SetValue(view, total);
        view.Show(); view.Focus(); view.Repaint();
    }

    static void RepaintGameView()
    {
        var view = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x => x.GetType().FullName == "UnityEditor.GameView");
        if (view != null) { view.Repaint(); view.Focus(); }
    }

    static void RestoreGameView()
    {
        var group = ViewSizeGroup();
        var view = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x => x.GetType().FullName == "UnityEditor.GameView");
        int custom = SessionState.GetInt(ViewCustomKey, -1);
        int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        int builtIn = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
        if (view != null)
            view.GetType().GetProperty("selectedSizeIndex", ReflectionFlags).SetValue(view, Mathf.Clamp(SessionState.GetInt(ViewIndexKey, 0), 0, Mathf.Max(0, total - 1)));
        // Remove only the uniquely named custom size created by this run.
        var labels = group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null) as string[];
        string tag = SessionState.GetString(ViewTagKey, "");
        if (custom >= 0 && tag.Length != 0 && labels != null && builtIn + custom < labels.Length
            && labels[builtIn + custom].Contains(tag))
            group.GetType().GetMethod("RemoveCustomSize", ReflectionFlags).Invoke(group, new object[] { custom });
        if (view != null && SessionState.GetBool(ViewCreatedKey, false)) view.Close();
    }

    static void RestorePerformance()
    {
        Application.targetFrameRate = SessionState.GetInt(FrameRateKey, Application.targetFrameRate);
        QualitySettings.vSyncCount = SessionState.GetInt(VSyncKey, QualitySettings.vSyncCount);
        Application.runInBackground = SessionState.GetBool(BackgroundKey, Application.runInBackground);
    }

    static void RestoreEditor()
    {
        try
        {
            string old = SessionState.GetString(OverrideKey, ""); ChapterOneRuntime.VerificationSaveDirectory = old.Length == 0 ? null : old;
            string desktop = SessionState.GetString(DesktopKey, "");
            typeof(Michsky.DreamOS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, desktop.Length == 0 ? null : desktop);
            string setup = SessionState.GetString(SetupKey, "");
            var saved = setup.Length == 0 ? null : JsonUtility.FromJson<SavedSetup>(setup);
            var valid = saved?.scenes?.Where(x => x != null && !string.IsNullOrEmpty(x.path) && File.Exists(x.path)).ToArray() ?? Array.Empty<SceneEntry>();
            var loaded = valid.Where(x => x.loaded).ToArray();
            var active = loaded.FirstOrDefault(x => x.active);
            if (active == null)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
            {
                EditorSceneManager.RestoreSceneManagerSetup(valid.Select(x => new SceneSetup
                    { path = x.path, isLoaded = x.loaded, isActive = ReferenceEquals(x, active) }).ToArray());
            }
        }
        finally
        {
            try { RestoreGameView(); }
            finally
            {
                RestorePerformance();
                foreach (string key in new[] { RunKey, SetupKey, OverrideKey, DesktopKey, ViewTagKey }) SessionState.EraseString(key);
                foreach (string key in new[] { FrameRateKey, VSyncKey, ExitCodeKey, ViewIndexKey, ViewCustomKey }) SessionState.EraseInt(key);
                foreach (string key in new[] { BackgroundKey, ViewCreatedKey }) SessionState.EraseBool(key);
            }
        }
    }

    static void WriteCleanupFailure(string dir, Exception error)
    {
        try { if (!string.IsNullOrEmpty(dir)) File.AppendAllText(Path.Combine(dir, "cleanup-errors.txt"), error + "\n"); }
        catch { /* Cleanup and batch exit must still finish if the report directory is unavailable. */ }
    }
}
#endif
