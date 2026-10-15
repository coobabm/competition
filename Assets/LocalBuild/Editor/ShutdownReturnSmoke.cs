// Isolated original-monitor boot and accepted reverse-shutdown regression.
// Run in a graphical Editor, without -quit/-nographics. LocalLlm is suppressed before attachment.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.CaseMenu;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using Michsky.DreamOS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ShutdownReturnSmoke
{
    const string Key = "ShutdownReturnSmoke.";
    static string Output => SessionState.GetString(Key + "Output", "");
    static string SaveRoot => Path.Combine(Output, "Save");
    static readonly FieldInfo ProfilePath = typeof(DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic);
    static string isolationError;
    static bool started, finished;
    static int checks, titleLoads, mainLoads;
    static Mouse mouse, previousMouse;
    static InputSettings originalInputSettings, verificationInputSettings;
    static readonly List<Mouse> mutedMice = new List<Mouse>();
    static readonly List<Pose> entry = new List<Pose>();
    static readonly List<string> rows = new List<string>();
    static Vector3 titlePosition;
    static Quaternion titleRotation;
    static float titleFov;
    static bool bootShot;
    static Renderer[] titleRoom;
    static CameraClearFlags titleClearFlags;
    static Color titleBackground;
    struct Pose { public float progress, fov; public Vector3 position; public Quaternion rotation; }

    static ShutdownReturnSmoke()
    {
        EditorApplication.update += StartWhenPlaying;
        EditorApplication.playModeStateChanged += PlayModeChanged;
    }

    [MenuItem("Tools/LingGuang/Verify Shutdown Return (isolated)")]
    public static void Run() { Begin(false); }
    public static void RunAndExit() { Begin(true); }
    static void Begin(bool exit)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before running this check.");
        string output = Path.GetFullPath(Path.Combine("LocalVerification", "ShutdownReturn", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8)));
        Directory.CreateDirectory(output);
        SessionState.SetString(Key + "Output", output);
        SessionState.SetBool(Key + "Exit", exit);
        SessionState.SetBool(Key + "HadReduce", PlayerPrefs.HasKey(CaseMenuController.ReduceMotionPref));
        SessionState.SetInt(Key + "Reduce", PlayerPrefs.GetInt(CaseMenuController.ReduceMotionPref));
        SessionState.SetString(Key + "PreviousSaveRoot", ChapterOneRuntime.VerificationSaveDirectory ?? "");
        SessionState.SetString(Key + "PreviousProfile", (string)ProfilePath?.GetValue(null) ?? "");
        SessionState.SetBool(Key + "RunInBackground", Application.runInBackground);
        PlayerPrefs.SetInt(CaseMenuController.ReduceMotionPref, 0);
        var seed = new ChapterOneSim();
        if (!seed.CompletePrologue(new PrologueProfile { name = "回程种子", self = "我", callMe = "你", personality = "好奇" }))
            throw new InvalidOperationException("Could not create isolated completed-prologue fixture.");
        seed.S.lastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!new AtomicSaveStore(SaveRoot).TrySave(JsonUtility.ToJson(seed.S), out string error)) throw new IOException(error);
        File.WriteAllText(Path.Combine(output, "result.txt"), "RUNNING\n");
        File.WriteAllText(Path.Combine(output, "checks.txt"), "Seeded isolated completed-prologue fixture; normal entries remain real disk mode.\nLocalLlm is disabled: no model/chat readiness coverage.\n");
        SessionState.SetBool(Key + "Active", true);
        EditorSceneManager.OpenScene(CaseMenuController.ScenePath);
        Application.runInBackground = true;
        var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.Show(); view.Focus();
        EditorApplication.isPlaying = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IsolateBeforeSceneLoad()
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        started = finished = false; checks = titleLoads = mainLoads = 0;
        isolationError = null; rows.Clear(); entry.Clear();
        ChapterOneRuntime.VerificationSaveDirectory = SaveRoot;
        if (ProfilePath == null) { isolationError = "DreamOS profile override field is unavailable."; return; }
        string profile = Path.Combine(Output, "DesktopProfile") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(profile); ProfilePath.SetValue(null, profile);
        SceneManager.sceneLoaded -= BeforeMainConfigure;
        SceneManager.sceneLoaded += BeforeMainConfigure;
        rows.Add("cycle,phase,frame,realtime,progress,x,y,z,qx,qy,qz,qw,fov,mainHandle,inputEnabled,powered,fadeAlpha");
    }

    static void BeforeMainConfigure(Scene scene, LoadSceneMode mode)
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        if (scene.path == CaseMenuController.ScenePath) titleLoads++;
        if (scene.path != MainSceneEntry.ScenePath) return;
        mainLoads++;
        try
        {
            var runtime = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ChapterOneRuntime>(true)).Single();
            Require(runtime.useDiskSave && !runtime.TestMode, "Main starts in normal disk mode");
            Require(Path.GetFullPath(runtime.SavePath).StartsWith(Path.GetFullPath(SaveRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Main Awake already uses isolated disk path");
            // Our subscription predates MainDisplay.Preload's subscription. A disabled,
            // uninitialized component makes MainSceneEntry skip Attach/Begin entirely.
            var llm = runtime.GetComponent<LocalLlm>() ?? runtime.gameObject.AddComponent<LocalLlm>();
            Require(llm.State == LocalLlm.Status.Off && !llm.OwnsServer, "LocalLlm has not started before suppression");
            llm.enabled = false;
        }
        catch (Exception e) { isolationError = e.ToString(); }
    }

    static void StartWhenPlaying()
    {
        if (!SessionState.GetBool(Key + "Active", false) || !EditorApplication.isPlaying || started || EditorApplication.isCompiling) return;
        started = true;
        var go = new GameObject("Shutdown Return Verification Runner");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<ShutdownReturnSmokeRunner>().StartCoroutine(Guarded(AllChecks()));
    }

    static IEnumerator Guarded(IEnumerator work)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(work);
        while (stack.Count > 0)
        {
            object yielded = null; Exception failure = null;
            try
            {
                if (isolationError != null) throw new InvalidOperationException(isolationError);
                CheckLlm();
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                yielded = stack.Peek().Current;
            }
            catch (Exception e) { failure = e; }
            if (failure != null) { Finish(false, failure.ToString()); yield break; }
            if (yielded is IEnumerator nested) stack.Push(nested);
            else yield return yielded;
        }
        Finish(true, "All requested routes completed. Screenshot review remains manual.");
    }

    static IEnumerator AllChecks()
    {
        // This isolated injected-input test must keep running while the user uses other apps.
        // Clone settings so neither the project's asset nor production focus behavior changes.
        originalInputSettings = InputSystem.settings;
        verificationInputSettings = Object.Instantiate(originalInputSettings);
        verificationInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        verificationInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = verificationInputSettings;
        previousMouse = Mouse.current;
        // Resizing/focusing the GameView can emit native pointer events after our queued
        // input and replace Mouse.current. Isolate only Unity devices, never the OS cursor.
        mutedMice.Clear();
        foreach (var device in InputSystem.devices.OfType<Mouse>().Where(m => m.enabled).ToArray())
        { mutedMice.Add(device); InputSystem.DisableDevice(device); }
        mouse = InputSystem.AddDevice<Mouse>("ShutdownReturnVerificationMouse");
        mouse.MakeCurrent();
        string expectedName = "回程种子";
        for (int cycle = 0; cycle < 3; cycle++)
        {
            yield return Wait(() => FindMenu() != null && FindMenu().IsMainReady, "ready title");
            var menu = FindMenu(); var display = menu.GetComponent<CaseMenuMainDisplay>();
            if (cycle == 0)
            {
                titlePosition = menu.menuCamera.transform.position;
                titleRotation = menu.menuCamera.transform.rotation; titleFov = menu.menuCamera.fieldOfView;
            }
            CheckTitle(menu);
            Check(display.Runtime.useDiskSave && !display.Runtime.TestMode && display.Runtime.Sim.S.aiName == expectedName, "cycle " + cycle + ": real disk fixture restored at title preload");
            bool edge = cycle == 2;
            typeof(NamingDesktopSmoke).GetMethod("SetViewSize", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { cycle == 0 ? 1440 : 1920, cycle == 0 ? 850 : 1080 });
            yield return null; yield return null; yield return null;
            menu.SetReducedMotion(edge);
            titleRoom = menu.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && !r.transform.IsChildOf(menu.displayRig)).ToArray();
            titleClearFlags = menu.menuCamera.clearFlags;
            titleBackground = menu.menuCamera.backgroundColor;
            yield return Shot(cycle + "-01-title");
            entry.Clear();
            bootShot = false;
            yield return PressPower(menu);
            float deadline = Time.realtimeSinceStartup + 45;
            while (!menu.HasEnteredGame)
            {
                Require(Time.realtimeSinceStartup < deadline, "Entry timeout: " + menu.LastError);
                RecordEntry(cycle, menu, display);
                yield return null;
            }
            RecordEntry(cycle, menu, display);
            if (!edge) Check(bootShot, "Original firmware logo and progress are visible during camera flight");
            Check(menu.TransitionCount == 1 && entry.Count > 4, "cycle " + cycle + ": physical press entered once over multiple frames");
            CheckPhysicalMonitor(display);
            Check(SceneManager.GetActiveScene().path == MainSceneEntry.ScenePath, "Main is the active scene");
            var runtime = display.Runtime;
            Check(runtime.useDiskSave && !runtime.TestMode && runtime.Sim.S.aiName == expectedName, "cycle " + cycle + ": desktop recovered saved marker");
            yield return Shot(cycle + "-02-physical-monitor-desktop");
            // Wait for the real calendar/chapter overlay to finish before clicking the desktop.
            // Directly invoking the LCD relay beneath that overlay would bypass normal input.
            yield return new WaitForSecondsRealtime(8);
            yield return Wait(() => !Object.FindAnyObjectByType<StoryDesktopPresenter>().CutscenePlaying
                && display.InputSurface.inputEnabled, "chapter overlay to finish naturally");
            if (cycle == 0)
            {
                var blocked = typeof(ChapterOneRuntime).GetField("persistenceBlocked", BindingFlags.Instance | BindingFlags.NonPublic);
                Require(blocked != null, "Save protection field is available");
                byte[] oldBytes = File.ReadAllBytes(runtime.SavePath);
                try
                {
                    blocked.SetValue(runtime, true);
                    Object.FindAnyObjectByType<BootManager>(FindObjectsInactive.Include).Shutdown();
                    Check(!menu.IsReturningToTitle && !menu.IsTransitioning && menu.HasEnteredGame && display.InputSurface.inputEnabled
                        && runtime.gameObject.scene.isLoaded && oldBytes.SequenceEqual(File.ReadAllBytes(runtime.SavePath)),
                        "Actual SaveNow failure keeps desktop open/input enabled and disk unchanged");
                }
                finally { blocked.SetValue(runtime, false); }
                yield return Shot("0-03-save-failure-stays-on-desktop");
            }
            InnerVoice.Say("准备保存并关机。", "Ready to save and shut down.", 30);
            InnerVoice.Say("这句在关机后不应继续。", "This queued line must not survive shutdown.", 30);
            Check(InnerVoice.Busy && InnerVoice.Pending >= 1, "cycle " + cycle + ": narration accepts two lines after boot/re-entry");
            int saveCount = 0; bool savedBeforeReturn = true;
            runtime.Saving += () => { saveCount++; savedBeforeReturn &= !menu.IsReturningToTitle; };
            byte[] beforeMemory = null;
            if (edge)
            {
                beforeMemory = File.ReadAllBytes(runtime.SavePath);
                runtime.SetSimulationForTests(new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(runtime.Sim.S))));
                runtime.Sim.S.aiName = "仅内存回程";
                Check(runtime.TestMode && !runtime.useDiskSave, "Only third cycle uses explicit memory TestMode");
                Time.timeScale = 0;
            }
            else
            {
                expectedName = cycle == 0 ? "回程甲" : "回程乙";
                runtime.Sim.S.aiName = expectedName; runtime.MarkDirty();
                Require(ReadSavedName() != expectedName, "Marker must be unsaved before shutdown");
            }
            var boot = Object.FindAnyObjectByType<BootManager>(FindObjectsInactive.Include);
            Require(boot != null, "Real BootManager is missing");
            var capturedMain = runtime.gameObject.scene;
            int oldMenuId = menu.GetInstanceID(), titleLoadBefore = titleLoads, mainLoadBefore = mainLoads;
            if (cycle == 1)
            {
                typeof(NamingDesktopSmoke).GetMethod("SetViewSize", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { 1600, 1000 });
                yield return null; yield return null; yield return null;
                CheckPhysicalMonitor(display);
                var start = Object.FindAnyObjectByType<AeroStartMenu>(FindObjectsInactive.Include);
                Require(start != null, "Real start menu is missing");
                yield return ClickSource(display.InputSurface, FindBoundButton(start, nameof(AeroStartMenu.Toggle)));
                Check(start.menu.activeInHierarchy, "Real source start button opens native menu");
                yield return Shot("1-03-start-menu");
                yield return ClickSource(display.InputSurface, FindBoundButton(start, nameof(AeroStartMenu.RequestShutdown)), false);
            }
            else boot.Shutdown();
            Require(menu != null && menu.IsReturningToTitle, "Real shutdown starts return before destroying old menu");
            Check(edge ? saveCount == 0 : saveCount == 1 && savedBeforeReturn && ReadSavedName() == expectedName,
                edge ? "Memory shutdown does not invoke disk save" : "Real shutdown writes changed marker before animation exactly once");
            if (edge)
            {
                Check(Time.timeScale == 1, "Shutdown normalizes a paused timeScale");
                // Hold time stopped again to prove animation itself uses unscaled time.
                Time.timeScale = 0;
                for (int i = 0; i < 4; i++) boot.Shutdown();
                Check(saveCount == 0, "Repeated shutdown remains deduplicated in memory case");
            }
            yield return ObserveReturn(cycle, menu, display, capturedMain, beforeMemory, boot, () => saveCount);
            yield return Wait(() => FindMenu() != null && FindMenu().GetInstanceID() != oldMenuId && FindMenu().IsMainReady, "reloaded title");
            Check(titleLoads == titleLoadBefore + 1 && mainLoads == mainLoadBefore + 1, "cycle " + cycle + ": exactly one title and one Main reload");
            CheckTitle(FindMenu());
            Check(!FindMenu().HasEnteredGame && !FindMenu().IsReturningToTitle, "Reloaded title is unlocked");
            Check(FindMenu().GetComponent<CaseMenuMainDisplay>().Runtime.Sim.S.aiName == expectedName, "Saved disk marker survives return; memory-only marker is absent");
            yield return Shot(cycle + "-07-returned-title");
            Time.timeScale = 1;
        }
        Check(titleLoads == 4 && mainLoads == 4, "Three shutdowns finish on fourth clickable title/Main preload");
    }

    static IEnumerator ObserveReturn(int cycle, CaseMenuController menu, CaseMenuMainDisplay display, Scene originalMain,
        byte[] unchangedDisk, BootManager boot, Func<int> saves)
    {
        float previous = -1, deadline = Time.realtimeSinceStartup + 45;
        int frames = 0, moving = 0, fadeFrames = 0; bool sawEnd = false, repeatSent = false, middleShot = false;
        Vector3 previousPosition = menu.menuCamera.transform.position;
        Quaternion previousRotation = menu.menuCamera.transform.rotation;
        float previousFov = menu.menuCamera.fieldOfView, previousFade = -1;
        var bootScreen = menu.GetComponent<CaseMenuBootScreen>();
        var cover = (CanvasGroup)typeof(CaseMenuBootScreen).GetField("cover", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootScreen);
        Vector3 desktopPosition = previousPosition;
        var externalCanvases = originalMain.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var originalRaycasters = originalMain.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GraphicRaycaster>(true)).ToArray();
        var voice = Object.FindAnyObjectByType<InnerVoice>(FindObjectsInactive.Include);
        var voiceGroup = voice == null ? null : (CanvasGroup)typeof(InnerVoice).GetField("group", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(voice);
        while (menu != null)
        {
            Require(Time.realtimeSinceStartup < deadline, "Return timeout");
            float p = menu.ReturnTransitionProgress;
            Require(menu.IsReturningToTitle && menu.IsTransitioning, "Return/input lock remains active");
            Require(p >= previous - .00001f && p >= 0 && p <= 1, "Return progress is monotonic 0..1");
            Require(originalMain.IsValid() && originalMain.isLoaded && display.Runtime != null && display.Runtime.gameObject.scene.handle == originalMain.handle, "Original Main stays loaded throughout return");
            Require(!display.InputSurface.inputEnabled, "Desktop input is locked during return");
            Require(!InnerVoice.Busy && InnerVoice.Pending == 0 && (voiceGroup == null || voiceGroup.alpha == 0), "Narration queue and subtitle are cleared during shutdown");
            Require(externalCanvases.All(c => c == null || !c.enabled) && originalRaycasters.All(r => r == null || !r.enabled), "Main overlay captions/toasts and raycasters stay disabled");
            Require(menu.HitButton(PowerPoint(menu)) == null, "Title interaction is locked during return");
            var cam = menu.menuCamera;
            Require(Vector3.Distance(previousPosition, cam.transform.position) < Mathf.Max(.10f, Vector3.Distance(titlePosition, desktopPosition) * .28f), "No adjacent-frame camera teleport");
            Require(Quaternion.Angle(previousRotation, cam.transform.rotation) < 20 && Mathf.Abs(previousFov - cam.fieldOfView) < 12, "Rotation/FOV remain continuous");
            if (p <= .00001f)
            {
                Require(Vector3.Distance(cam.transform.position, desktopPosition) < .001f, "Screen fades before camera departs");
                Require(cover.alpha >= previousFade - .0001f, "Shutdown fade grows monotonically");
                if (cover.alpha > .001f && cover.alpha < .999f) fadeFrames++;
                previousFade = cover.alpha;
            }
            else
            {
                Require(!display.IsScreenPowered && cover.alpha > .999f, "Screen is fully dark before return flight");
                var expected = EntryAt(1 - p);
                Require(Vector3.Distance(cam.transform.position, expected.position) < .065f
                    && Quaternion.Angle(cam.transform.rotation, expected.rotation) < 1
                    && Mathf.Abs(cam.fieldOfView - expected.fov) < .25f, "Return retraces observed entry camera path/FOV");
                if (p > previous + .00001f) moving++;
            }
            if (!repeatSent && p > .2f)
            {
                int count = saves(); float progress = p;
                boot.Shutdown(); boot.Shutdown(); GameExit.ReturnToTitle();
                InnerVoice.Say("迟到的回调不应出现。", "A late callback must stay silent.", 30);
                Require(menu.IsReturningToTitle && menu.ReturnTransitionProgress == progress && saves() == count, "Repeated shutdown does not restart/save again");
                Require(!InnerVoice.Busy && InnerVoice.Pending == 0, "Late narration callback is rejected during shutdown");
                repeatSent = true;
            }
            Trace(cycle, "return", menu, display, p, cover.alpha); frames++;
            previous = p; previousPosition = cam.transform.position; previousRotation = cam.transform.rotation; previousFov = cam.fieldOfView;
            if (!middleShot && p >= .45f && p < .9f)
            {
                middleShot = true;
                // Request asynchronous capture without skipping observation of the next game frame.
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, cycle + "-05-return-middle.png"));
            }
            if (p >= .99999f)
            {
                sawEnd = true;
                Require(Vector3.Distance(cam.transform.position, titlePosition) < .001f && Quaternion.Angle(cam.transform.rotation, titleRotation) < .05f
                    && Mathf.Abs(cam.fieldOfView - titleFov) < .001f, "Old camera reaches exact original title pose before reload");
                if (unchangedDisk != null) Require(File.ReadAllBytes(new AtomicSaveStore(SaveRoot).SavePath).SequenceEqual(unchangedDisk), "Memory return does not write isolated disk before reload");
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, cycle + "-06-return-landed.png"));
            }
            yield return null;
        }
        Check(sawEnd && frames >= 8 && moving >= 5 && fadeFrames >= 2 && repeatSent && middleShot,
            "cycle " + cycle + ": observed fade, multi-frame reverse path, deduplication, landing then unload (frames=" + frames + ", moving=" + moving + ", fade=" + fadeFrames + ")");
        Check(!originalMain.isLoaded, "Original Main unloads only after observed landing");
    }

    static IEnumerator PressPower(CaseMenuController menu)
    {
        var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.Focus();
        yield return new WaitForSecondsRealtime(.35f);
        Vector2 point = PowerPoint(menu);
        Require(menu.HitButton(point) == menu.startButton, "Physical power button hit test");
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null;
        Require(menu.startButton.IsPressed, "Real input press reaches physical key (current=" + Mouse.current?.name
            + ", added=" + mouse.added + ", enabled=" + mouse.enabled + ", pressed=" + mouse.leftButton.isPressed
            + ", focus=" + Application.isFocused + ")");
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null;
        Require(menu.IsTransitioning, "Real input release starts entry");
    }

    static Vector2 PowerPoint(CaseMenuController menu)
    {
        var collider = menu.startButton.GetComponentInChildren<Collider>();
        Require(collider != null, "Power key collider exists");
        return menu.menuCamera.WorldToScreenPoint(collider.bounds.center);
    }
    static void CheckTitle(CaseMenuController menu)
    {
        Check(Vector3.Distance(menu.menuCamera.transform.position, titlePosition) < .001f && Quaternion.Angle(menu.menuCamera.transform.rotation, titleRotation) < .05f
            && Mathf.Abs(menu.menuCamera.fieldOfView - titleFov) < .001f, "Title camera is at original pose");
        Check(menu.HitButton(PowerPoint(menu)) == menu.startButton && !menu.IsTransitioning, "Title power button is available");
    }
    static void CheckPhysicalMonitor(CaseMenuMainDisplay display)
    {
        Canvas.ForceUpdateCanvases();
        var menu = display.GetComponent<CaseMenuController>();
        var surface = display.InputSurface; var canvas = surface.displayRect.GetComponentInParent<Canvas>();
        var corners = new Vector3[4]; surface.displayRect.GetWorldCorners(corners);
        var lower = menu.menuCamera.WorldToViewportPoint(corners[0]);
        var upper = menu.menuCamera.WorldToViewportPoint(corners[2]);
        Check(canvas.renderMode == RenderMode.WorldSpace && canvas.enabled && surface.inputEnabled && display.HasEnteredDesktop
            && surface.displayRect.IsChildOf(menu.displayRig) && GameObject.Find("Fullscreen Desktop") == null,
            "Desktop remains on original physical monitor, with native canvas/input restored and no fullscreen overlay");
        Check(lower.x > .001f && lower.y > .001f && upper.x < .999f && upper.y < .999f,
            "Original camera framing retains visible monitor bezel at " + Screen.width + "x" + Screen.height);
        var renderers = menu.displayRig.GetComponentsInChildren<Renderer>();
        Check(renderers.Any(r => r.enabled), "Physical monitor model remains rendered after boot");
        Check(titleRoom.Length > 0 && titleRoom.All(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
            && menu.menuCamera.clearFlags == titleClearFlags && menu.menuCamera.backgroundColor == titleBackground,
            "Original title room stays visible after desktop entry and resize; camera background is unchanged");
        foreach (var uv in new[] { new Vector2(.025f,.025f), new Vector2(.975f,.025f), new Vector2(.025f,.975f), new Vector2(.975f,.975f) })
        {
            Rect bounds = surface.displayRect.rect;
            Vector3 world = surface.displayRect.TransformPoint(new Vector3(Mathf.Lerp(bounds.xMin, bounds.xMax, uv.x), Mathf.Lerp(bounds.yMin, bounds.yMax, uv.y)));
            Vector2 point = menu.menuCamera.WorldToScreenPoint(world);
            var rt = surface.sourceCamera.targetTexture;
            Vector2 expected = new Vector2(uv.x * rt.width, (surface.flipY ? 1 - uv.y : uv.y) * rt.height);
            Require(surface.TryMap(point, out var mapped) && Vector2.Distance(mapped, expected) < 2, "Physical LCD corner input round-trip");
        }
        Check(true, "All four physical LCD corners map correctly into source desktop");
    }
    static Component FindBoundButton(AeroStartMenu target, string method)
    {
        var candidates = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>()
            .Concat(Object.FindObjectsByType<ButtonManager>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        var matches = candidates.Where(component =>
        {
            var action = component is Button button ? button.onClick : ((ButtonManager)component).onClick;
            return Enumerable.Range(0, action.GetPersistentEventCount())
                .Any(i => action.GetPersistentTarget(i) == target && action.GetPersistentMethodName(i) == method);
        }).ToArray();
        Require(matches.Length == 1, "Exactly one native button is bound to " + method + ": " + matches.Length);
        return matches[0];
    }
    static IEnumerator ClickSource(AeroLcdInputSurface surface, Component button, bool yieldAfter = true)
    {
        Require(button != null && button.gameObject.activeInHierarchy && surface.inputEnabled
            && (button is Button native ? native.IsInteractable() : ((ButtonManager)button).isInteractable), "Real source button is clickable");
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        Vector2 source = RectTransformUtility.WorldToScreenPoint(surface.sourceCamera, rect.TransformPoint(rect.rect.center));
        var rt = surface.sourceCamera.targetTexture;
        Vector2 uv = new Vector2(source.x / (rt != null ? rt.width : surface.sourceCamera.pixelWidth), source.y / (rt != null ? rt.height : surface.sourceCamera.pixelHeight));
        if (surface.flipY) uv.y = 1 - uv.y;
        Rect bounds = surface.displayRect.rect;
        Vector3 world = surface.displayRect.TransformPoint(new Vector3(Mathf.Lerp(bounds.xMin, bounds.xMax, uv.x), Mathf.Lerp(bounds.yMin, bounds.yMax, uv.y)));
        var canvas = surface.displayRect.GetComponentInParent<Canvas>();
        Vector2 physical = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, world);
        Require(surface.TryMap(physical, out var mapped) && Vector2.Distance(source, mapped) < 2, "Physical LCD maps to actual source button");
        var data = new PointerEventData(EventSystem.current) { position = physical, button = PointerEventData.InputButton.Left, pointerId = -1 };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<AeroLcdInputSurface>() == surface,
            "Real screen raycast reaches LCD instead of bypassing a modal overlay");
        surface.OnPointerEnter(data); surface.OnPointerDown(data); yield return null;
        surface.OnPointerUp(data);
        if (yieldAfter) yield return null;
    }
    static void RecordEntry(int cycle, CaseMenuController menu, CaseMenuMainDisplay display)
    {
        var cam = menu.menuCamera;
        Require(display.InputSurface.displayRect.IsChildOf(menu.displayRig)
            && display.InputSurface.displayRect.GetComponentInParent<Canvas>().renderMode == RenderMode.WorldSpace
            && GameObject.Find("Fullscreen Desktop") == null, "LCD stays physically mounted throughout the original boot flight");
        if (menu.TransitionProgress < 1)
            Require(!display.InputSurface.inputEnabled && !display.Runtime.enabled, "Boot keeps desktop input and story paused until camera arrival");
        if (!bootShot && menu.TransitionProgress > .25f && menu.TransitionProgress < .75f && display.IsBootScreenVisible)
        {
            var boot = menu.GetComponent<CaseMenuBootScreen>();
            var content = (CanvasGroup)typeof(CaseMenuBootScreen).GetField("content", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boot);
            if (content.alpha > .5f)
            {
                bootShot = true;
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, cycle + "-00-restored-boot.png"));
            }
        }
        entry.Add(new Pose { progress = menu.TransitionProgress, position = cam.transform.position, rotation = cam.transform.rotation, fov = cam.fieldOfView });
        Trace(cycle, "entry", menu, display, menu.TransitionProgress, -1);
    }
    static Pose EntryAt(float progress)
    {
        for (int i = 1; i < entry.Count; i++)
        {
            if (entry[i].progress < progress) continue;
            var a = entry[i - 1]; var b = entry[i]; float t = Mathf.InverseLerp(a.progress, b.progress, progress);
            return new Pose { position = Vector3.Lerp(a.position, b.position, t), rotation = Quaternion.Slerp(a.rotation, b.rotation, t), fov = Mathf.Lerp(a.fov, b.fov, t) };
        }
        return entry[entry.Count - 1];
    }
    static void Trace(int cycle, string phase, CaseMenuController menu, CaseMenuMainDisplay display, float p, float alpha)
    {
        var c = menu.menuCamera; var v = c.transform.position; var q = c.transform.rotation;
        rows.Add(string.Join(",", new[] { cycle.ToString(), phase, Time.frameCount.ToString(), F(Time.realtimeSinceStartup), F(p), F(v.x), F(v.y), F(v.z), F(q.x), F(q.y), F(q.z), F(q.w), F(c.fieldOfView), display.Runtime.gameObject.scene.handle.ToString(), surfaceBool(display.InputSurface.inputEnabled), surfaceBool(display.IsScreenPowered), F(alpha) }));
    }
    static string F(float n) => n.ToString("R", CultureInfo.InvariantCulture);
    static string surfaceBool(bool value) => value ? "1" : "0";
    static CaseMenuController FindMenu() => Object.FindAnyObjectByType<CaseMenuController>();
    static string ReadSavedName()
    {
        var read = new AtomicSaveStore(SaveRoot).Load(); Require(read.Success, "Isolated save has valid checksum");
        return JsonUtility.FromJson<GameState>(read.Payload).aiName;
    }
    static void CheckLlm()
    {
        foreach (var llm in Object.FindObjectsByType<LocalLlm>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Require(!llm.enabled && llm.State == LocalLlm.Status.Off && !llm.OwnsServer && llm.OwnedServiceId == 0, "LocalLlm remains disabled without owning a model service");
    }
    static IEnumerator Wait(Func<bool> condition, string name)
    {
        float deadline = Time.realtimeSinceStartup + 60;
        while (!condition()) { Require(Time.realtimeSinceStartup < deadline, "Timeout waiting for " + name); yield return null; }
    }
    static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG()); }
        finally { Object.Destroy(texture); }
        yield return null;
    }
    static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static void Check(bool ok, string message)
    {
        Require(ok, message); checks++;
        File.AppendAllText(Path.Combine(Output, "checks.txt"), "PASS " + message + "\n");
    }
    static void Finish(bool pass, string detail)
    {
        if (finished) return; finished = true;
        File.WriteAllLines(Path.Combine(Output, "frames.csv"), rows);
        File.WriteAllText(Path.Combine(Output, "result.txt"), (pass ? "PASS" : "FAIL") + "\nChecks: " + checks + "\n" + detail + "\n");
        SessionState.SetInt(Key + "ExitCode", pass ? 0 : 1);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        foreach (var device in mutedMice) if (device != null && device.added) InputSystem.EnableDevice(device);
        mutedMice.Clear();
        if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
        if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
        if (verificationInputSettings != null) Object.Destroy(verificationInputSettings);
        originalInputSettings = verificationInputSettings = null;
        Time.timeScale = 1;
        if (pass) Debug.Log("ShutdownReturnSmoke PASS: " + Output); else Debug.LogError("ShutdownReturnSmoke FAIL: " + detail);
        // Keep save overrides until play objects finish OnApplicationQuit/OnDestroy.
        EditorApplication.isPlaying = false;
    }
    static void PlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + "Active", false)) return;
        SceneManager.sceneLoaded -= BeforeMainConfigure;
        ChapterOneRuntime.VerificationSaveDirectory = SessionState.GetString(Key + "PreviousSaveRoot", "");
        ProfilePath?.SetValue(null, SessionState.GetString(Key + "PreviousProfile", ""));
        if (SessionState.GetBool(Key + "HadReduce", false)) PlayerPrefs.SetInt(CaseMenuController.ReduceMotionPref, SessionState.GetInt(Key + "Reduce", 0));
        else PlayerPrefs.DeleteKey(CaseMenuController.ReduceMotionPref);
        PlayerPrefs.Save();
        Application.runInBackground = SessionState.GetBool(Key + "RunInBackground", false);
        SessionState.SetBool(Key + "Active", false);
        if (SessionState.GetBool(Key + "Exit", false)) EditorApplication.Exit(SessionState.GetInt(Key + "ExitCode", 1));
    }
}

public sealed class ShutdownReturnSmokeRunner : MonoBehaviour { }
