#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Story;
using LingGuangV05.Desktop.OS;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LingGuangV05.CaseMenu.Editor
{
    /// <summary>
    /// Explicit Editor Play QA; verifies early additive Main preload and a continuous, same-camera entry.
    /// Begin only after the operator backs up the player save; the operator restores it after stopping Play.
    /// This tool restores touched preferences and temporary runtime flags even on failure or interruption.
    /// </summary>
    public static class CaseMenuPlayQa
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report
        {
            public string scope = "Editor Play; queued InputSystem events for menu; existing LCD relay for desktop. Not a Player-build or audible-listening certification.";
            public bool complete;
            public string phase, outputDirectory;
            public int passed, failed;
            public float elapsedSeconds;
            public float configuredBootSeconds, observedBootSeconds, clickToDesktopSeconds;
            public float fanPeakVolume, fanPeakPitch, fanStableVolume, fanStablePitch;
            public List<Check> checks = new List<Check>();
            public List<string> errors = new List<string>();
            public List<string> screenshots = new List<string>();
        }

        public static Report Result { get; private set; }
        public static bool Running { get; private set; }
        public static string OutputDirectory { get; private set; }
        static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        static CaseMenuController menu;
        static Mouse mouse;
        static Keyboard keyboard;
        static Vector2 savedPointer, queuedPointer;
        static float savedVolume;
        static bool volumeExisted, motionExisted, fullscreenExisted;
        static float volumePref;
        static int motionPref, fullscreenPref;
        static double startedAt;
        static int previousFrame;
        static bool cleaningUp;
        static ChapterOneRuntime runtime;
        static bool previousDiskSave, previousTick, runtimeFlagsCaptured;
        static PrologueDirector prologue;
        static StoryDirector story;
        static bool previousPrologueEnabled, previousStoryEnabled, pausedNarrative;
        static AudioSource fanSource;

        public static void Begin()
        {
            if (Running) throw new InvalidOperationException("Case menu QA is already running.");
            if (!Application.isPlaying || !SceneManager.GetSceneByPath(CaseMenuController.ScenePath).isLoaded)
                throw new InvalidOperationException("Enter ComputerCaseMenu Play Mode before invoking Begin().");
            menu = Object.FindAnyObjectByType<CaseMenuController>();
            mouse = Mouse.current; keyboard = Keyboard.current;
            if (menu == null || !menu.isActiveAndEnabled || mouse == null || keyboard == null)
                throw new InvalidOperationException("QA needs a running menu plus existing mouse and keyboard devices.");
            if (mouse.leftButton.isPressed || keyboard.anyKey.isPressed)
                throw new InvalidOperationException("Release physical mouse/keyboard buttons before starting QA.");

            savedPointer = queuedPointer = mouse.position.ReadValue();
            savedVolume = AudioListener.volume;
            volumeExisted = PlayerPrefs.HasKey(CaseMenuController.VolumePref);
            motionExisted = PlayerPrefs.HasKey(CaseMenuController.ReduceMotionPref);
            fullscreenExisted = PlayerPrefs.HasKey(CaseMenuController.FullscreenPref);
            volumePref = PlayerPrefs.GetFloat(CaseMenuController.VolumePref, .8f);
            motionPref = PlayerPrefs.GetInt(CaseMenuController.ReduceMotionPref, 0);
            fullscreenPref = PlayerPrefs.GetInt(CaseMenuController.FullscreenPref, 0);
            runtime = null; prologue = null; story = null; pausedNarrative = false; runtimeFlagsCaptured = false;
            OutputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Documentation/CaseMenu/QA/",
                DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(OutputDirectory);
            Result = new Report { phase = "starting", outputDirectory = OutputDirectory };
            startedAt = EditorApplication.timeSinceStartup;
            previousFrame = -1; cleaningUp = false; Running = true;
            routines.Clear(); routines.Push(Run());
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Advance;
            EditorApplication.playModeStateChanged += OnMode;
            AssemblyReloadEvents.beforeAssemblyReload += OnReload;
            LingGuangV05.Editor.ChapterOneQaReloadGuard.LockForPlayQa(50);
            var gameView = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .FirstOrDefault(w => w.GetType().FullName == "UnityEditor.GameView");
            if (gameView != null) gameView.Focus();
            Save();
        }

        static IEnumerator Run()
        {
            Phase("real desktop preload");
            yield return Until(() => menu.IsMainReady || !string.IsNullOrEmpty(menu.LastError), 25, "Desktop preload finishes");
            Require("Real Main is ready before pressing Start", menu.IsMainReady, menu.LastError);
            var display = menu.GetComponent<CaseMenuMainDisplay>();
            Require("Native LCD texture is rendered but input is gated", display != null && display.InputSurface != null
                && !display.InputSurface.inputEnabled && display.InputSurface.sourceCamera.enabled
                && display.InputSurface.sourceCamera.targetTexture != null);
            var screenImage = display.InputSurface.GetComponent<RawImage>();
            Require("Preloaded desktop stays physically black until the power click", screenImage != null
                && !display.IsScreenPowered && Mathf.Max(screenImage.color.r, screenImage.color.g, screenImage.color.b) < .01f);
            Require("Preloading alone cannot start or complete the visible boot", !display.IsBootComplete
                && display.BootProgress == 0 && !display.IsBootScreenVisible);
            var preloadedRuntime = display.Runtime;
            Require("Real gameplay is initialized, with simulation paused before Start", preloadedRuntime != null
                && preloadedRuntime.Sim != null && !preloadedRuntime.enabled);
            double clockBefore = preloadedRuntime.Sim.S.gameSeconds;
            yield return Seconds(.2f);
            Require("Waiting at the menu does not consume gameplay time", preloadedRuntime.Sim.S.gameSeconds == clockBefore);
            int cameraId = menu.menuCamera.GetInstanceID();
            int mainHandle = SceneManager.GetSceneByPath(MainSceneEntry.ScenePath).handle;
            Phase("physical input");
            Pointer(new Vector2(24, 24), false); Keys();
            yield return Frames(3);
            Require("All button and startup sounds assigned", menu.hoverClip != null && menu.pressClip != null
                && menu.releaseClip != null && menu.bootClip != null && menu.fanClip != null);
            fanSource = menu.GetComponents<AudioSource>().Single(a => a.clip == menu.fanClip && a.loop);
            Require("Fan is silent and stopped before power on", !fanSource.isPlaying && fanSource.volume == 0);
            Capture("01-idle");
            Vector2 start = SwitchPoint(menu.startButton), setup = SwitchPoint(menu.settingsButton);
            Require("Power switch is physically hittable", menu.HitButton(start) == menu.startButton, start.ToString());
            Require("Settings switch is physically hittable", menu.HitButton(setup) == menu.settingsButton, setup.ToString());
            Pointer(start, false); yield return Frames(3);
            Require("Queued mouse enters physical switch", menu.startButton.IsHovered);
            float restZ = menu.startButton.keycap.localPosition.z;
            Pointer(start, true); yield return Seconds(.15f);
            Require("Mouse down depresses real keycap without entering Main", menu.startButton.IsPressed
                && menu.startButton.keycap.localPosition.z > restZ + .002f && !menu.IsTransitioning);
            Pointer(new Vector2(24, 24), true); yield return Frames(2);
            Pointer(new Vector2(24, 24), false); yield return Frames(3);
            Require("Release outside cancels activation", !menu.startButton.IsPressed && !menu.IsTransitioning);

            Pointer(start, false); yield return Frames(2);
            Pointer(start, true); yield return Frames(2);
            Keys(Key.Tab); yield return Frames(2); Keys(); yield return Frames(2);
            Keys(Key.Space); yield return Frames(2); Keys(); yield return Frames(2);
            Require("Other input source cannot steal or release mouse capture", menu.startButton.IsPressed
                && !menu.settingsButton.IsPressed && !menu.IsSettingsOpen && !menu.IsTransitioning);
            Pointer(new Vector2(24, 24), true); yield return Frames(2);
            Pointer(new Vector2(24, 24), false); yield return Frames(2);

            Phase("settings");
            Pointer(setup, false); yield return Frames(2);
            Pointer(setup, true); yield return Frames(2);
            Require("Settings opens on release, not down", menu.settingsButton.IsPressed && !menu.IsSettingsOpen);
            Pointer(setup, false); yield return Frames(3);
            Require("Settings modal opened through physical button", menu.IsSettingsOpen);
            var slider = menu.GetComponentsInChildren<Slider>(true).Single(s => s.name == "VolumeSlider");
            slider.value = .37f; yield return Frames(2);
            Require("Settings slider changes total volume and stored preference", Mathf.Abs(AudioListener.volume - .37f) < .001f
                && Mathf.Abs(PlayerPrefs.GetFloat(CaseMenuController.VolumePref, -1) - .37f) < .001f);
            slider.value = 0; yield return Frames(2);
            Require("Zero volume mutes the global listener", AudioListener.volume == 0);
            slider.value = .37f;
            var toggle = menu.GetComponentsInChildren<Toggle>(true).Single(t => t.name == "ReduceMotion");
            toggle.isOn = true; yield return Frames(2);
            Require("Reduce-motion preference reaches both switches", menu.startButton.ReducedMotion
                && menu.settingsButton.ReducedMotion && PlayerPrefs.GetInt(CaseMenuController.ReduceMotionPref) == 1);
            toggle.isOn = false; yield return Frames(2);
            Require("Full camera move restored for transition QA", !menu.startButton.ReducedMotion
                && PlayerPrefs.GetInt(CaseMenuController.ReduceMotionPref, 1) == 0);
            Capture("02-settings");
            Keys(Key.Escape); yield return Frames(2); Keys(); yield return Frames(2);
            Require("Escape closes modal", !menu.IsSettingsOpen);

            // The first Tab selects Start; the second selects Settings. All confirms are queued input.
            Pointer(new Vector2(24, 24), false); yield return Frames(2);
            Keys(Key.Tab); yield return Frames(2); Keys(); yield return Frames(2);
            Require("Tab selects power switch", menu.startButton.IsHovered);
            Keys(Key.Tab); yield return Frames(2); Keys(); yield return Frames(2);
            Require("Tab selects settings switch", menu.settingsButton.IsHovered);
            Keys(Key.Enter); yield return Frames(2);
            Require("Keyboard confirm waits for release", !menu.IsSettingsOpen && menu.settingsButton.IsPressed);
            Keys(); yield return Frames(3);
            Require("Keyboard release opens settings", menu.IsSettingsOpen);
            Keys(Key.Escape); yield return Frames(2); Keys(); yield return Frames(2);

            Phase("continuous camera entry into already-running Main");
            Pointer(start, false); yield return Frames(2);
            Pointer(start, true); yield return Frames(2);
            double bootClickAt = EditorApplication.timeSinceStartup;
            Pointer(start, false); yield return Frames(2);
            Require("Physical power click starts exactly one transition", menu.IsTransitioning && menu.TransitionCount == 1);
            Require("Only the accepted power click starts the native LCD backlight", display.IsScreenPowered);
            Result.configuredBootSeconds = CaseMenuMainDisplay.BootDuration;
            Require("Power click begins a visible incomplete boot", display.IsBootScreenVisible
                && !display.IsBootComplete && display.BootProgress >= 0 && display.BootProgress < 1);
            Require("Gameplay remains gated during the visible boot", !display.HasEnteredDesktop
                && !display.InputSurface.inputEnabled && !display.Runtime.enabled);
            menu.BeginGame(); menu.BeginGame();
            Require("Repeated start commands are ignored during flight", menu.TransitionCount == 1);
            float previousBootProgress = display.BootProgress;
            double bootObservedCompleteAt = -1;
            foreach (float progress in new[] { .25f, .5f, .85f })
            {
                yield return Until(() =>
                {
                    if (display.IsBootComplete && bootObservedCompleteAt < 0) bootObservedCompleteAt = EditorApplication.timeSinceStartup;
                    return menu != null && menu.TransitionProgress >= progress;
                }, 8, "Camera reaches " + progress);
                Record("Flight position at " + progress, true, menu.menuCamera.transform.position + "; progress=" + menu.TransitionProgress);
                Require("Flight has no camera swap or scene reload at " + progress,
                    menu.menuCamera.GetInstanceID() == cameraId && menu.menuCamera.isActiveAndEnabled
                    && SceneManager.GetSceneByPath(MainSceneEntry.ScenePath).handle == mainHandle);
                var fade = menu.GetComponentsInChildren<CanvasGroup>(true).FirstOrDefault(g => g.name == "TransitionFade");
                Require("No blackout during flight at " + progress, fade == null || fade.alpha == 0);
                Require("Physical LCD backlight is on during flight at " + progress, display.IsScreenPowered
                    && screenImage.color.r > .3f && screenImage.color.g > .3f && screenImage.color.b > .3f,
                    "Backlight only: the screen-contained boot can still cover the real desktop.");
                Require("Boot progress remains bounded and monotonic at " + progress,
                    display.BootProgress >= previousBootProgress && display.BootProgress <= 1,
                    "bootProgress=" + display.BootProgress);
                previousBootProgress = display.BootProgress;
                if (progress <= .5f)
                {
                    Require("Loading remains visible in the LCD at " + progress,
                        display.IsBootScreenVisible && !display.IsBootComplete && display.BootProgress < 1);
                    Require("Loading cannot release gameplay input at " + progress,
                        !display.HasEnteredDesktop && !display.InputSurface.inputEnabled && !display.Runtime.enabled);
                }
                Capture("03-flight-" + Mathf.RoundToInt(progress * 100));
                yield return Frames(2);
            }
            yield return Until(() =>
            {
                if (display.IsBootComplete && bootObservedCompleteAt < 0) bootObservedCompleteAt = EditorApplication.timeSinceStartup;
                return menu.HasEnteredGame;
            }, 8, "Same-camera movement finishes and enters desktop");
            Result.clickToDesktopSeconds = (float)(EditorApplication.timeSinceStartup - bootClickAt);
            Result.observedBootSeconds = bootObservedCompleteAt >= 0 ? (float)(bootObservedCompleteAt - bootClickAt) : -1;
            Require("Boot completes and its screen-contained artwork disappears before gameplay",
                display.IsBootComplete && Mathf.Abs(display.BootProgress - 1) < .001f && !display.IsBootScreenVisible);
            // At least the configured boot (3 s since the 2026-10-22 opening pace; it was 4.2 s).
            Require("Visible boot is not bypassed by camera arrival", Result.observedBootSeconds >= CaseMenuMainDisplay.BootDuration
                && Result.clickToDesktopSeconds >= CaseMenuMainDisplay.BootDuration,
                "configured=" + Result.configuredBootSeconds + "s; observedCompletion=" + Result.observedBootSeconds
                + "s; clickToDesktop=" + Result.clickToDesktopSeconds + "s (completion sampled once per rendered frame)");
            Require("Entry retains the original camera and preloaded Main", menu.menuCamera.GetInstanceID() == cameraId
                && menu.menuCamera.isActiveAndEnabled && SceneManager.GetSceneByPath(MainSceneEntry.ScenePath).handle == mainHandle
                && SceneManager.GetSceneByPath(CaseMenuController.ScenePath).isLoaded && display.HasEnteredDesktop);

            Phase("Main startup and desktop smoke");
            runtime = Object.FindAnyObjectByType<ChapterOneRuntime>();
            Require("Main has one ChapterOneRuntime", runtime != null && Count<ChapterOneRuntime>() == 1);
            previousDiskSave = runtime.useDiskSave; previousTick = runtime.tickEnabled;
            runtimeFlagsCaptured = true;
            runtime.useDiskSave = false; runtime.tickEnabled = false;
            Require("Main entry remains safe to call twice", MainSceneEntry.EnsureInitialized() && MainSceneEntry.EnsureInitialized());
            Require("Eight gameplay services are present", runtime.GetComponent<StoryDirector>() != null
                && runtime.GetComponent<XingGuangController>() != null && runtime.GetComponent<YYChatHub>() != null
                && runtime.GetComponent<YYGirlfriend>() != null && Count<MailInboxView>() == 1
                && runtime.GetComponent<XgMarketRelay>() != null
                && runtime.GetComponent<XgAfterthoughtRelay>() != null && runtime.GetComponent<LocalLlm>() != null);
            Require("YY and Mail views are not duplicated", Count<YYChatView>() == 1 && Count<MailInboxView>() == 1);
            var retiredWindows = Object.FindObjectsByType<WindowManager>(FindObjectsInactive.Include)
                .Where(w => w.gameObject.scene == runtime.gameObject.scene && w.name == "Photo Gallery").ToArray();
            Require("Retired native app has no window, taskbar or start-menu launch entry",
                retiredWindows.All(w => !w.isOn && !w.gameObject.activeSelf && (w.taskbarButton == null || !w.taskbarButton.gameObject.activeSelf))
                && Object.FindObjectsByType<AeroStartMenu>(FindObjectsInactive.Include)
                    .Where(m => m.gameObject.scene == runtime.gameObject.scene)
                    .All(m => m.programs == null || m.programs.All(p => p.window == null || !retiredWindows.Contains(p.window))));
            Require("One active EventSystem and AudioListener across both loaded scenes",
                Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude).Count(e => e.isActiveAndEnabled) == 1
                && Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Count(a => a.isActiveAndEnabled) == 1);
            Require("Physical menu no longer accepts input after arrival", menu.HitButton(start) == null && !menu.IsSettingsOpen);
            Require("Master volume survives desktop entry", Mathf.Abs(AudioListener.volume - .37f) < .001f);
            Result.fanStableVolume = fanSource.volume; Result.fanStablePitch = fanSource.pitch;
            Require("Fan audio source ramps in volume and pitch then settles to a continuous idle",
                Result.fanPeakVolume >= .20f && Result.fanPeakPitch >= 1.4f
                && fanSource.isPlaying && Mathf.Abs(fanSource.volume - .032f) < .001f
                && Mathf.Abs(fanSource.pitch - .9f) < .001f,
                "peak=" + Result.fanPeakVolume + "/" + Result.fanPeakPitch + "; stable=" + fanSource.volume + "/" + fanSource.pitch);

            yield return Until(() => PrologueDirector.Desk != null, 5, "Desktop presentation is ready");
            prologue = Object.FindAnyObjectByType<PrologueDirector>(); story = runtime.GetComponent<StoryDirector>();
            previousPrologueEnabled = prologue != null && prologue.enabled;
            previousStoryEnabled = story != null && story.enabled;
            pausedNarrative = true;
            if (prologue != null) prologue.enabled = false;
            if (story != null) story.enabled = false;
            VoiceLines.Stop();
            yield return Frames(3);
            var lcd = Object.FindAnyObjectByType<AeroLcdInputSurface>();
            Require("LCD input restored for ordinary desktop use", lcd != null && lcd.inputEnabled);
            Capture("04-main-desktop");
            var icon = PrologueDirector.Desk.icons.Find("Calculator") as RectTransform;
            // The native Calculator hosts 摆渡众包, which arrives through a group link: no icon until it is installed.
            yield return Seconds(1.2f);
            Require("Uninstalled 摆渡众包 has no desktop shortcut", icon != null && !icon.gameObject.activeSelf);
            var crowd = runtime.GetComponent<XingGuangController>().Sim.AppReceipt("zhongbao");
            crowd.installed = true;
            yield return Seconds(1.2f);
            Require("Native Calculator shortcut exists once installed", icon != null && icon.gameObject.activeInHierarchy);
            LingGuangV05.Editor.ChapterOnePlayQa.Click(icon);
            LingGuangV05.Editor.ChapterOnePlayQa.Click(icon);
            yield return Seconds(.8f);
            var calculator = Object.FindObjectsByType<WindowManager>(FindObjectsInactive.Include).FirstOrDefault(w => w.name == "Calculator");
            Require("Calculator opens through real LCD pointer relay", calculator != null && calculator.isOn
                && calculator.gameObject.activeInHierarchy && calculator.GetComponent<CanvasGroup>().alpha > .5f);
            Capture("05-main-calculator");
            calculator.CloseWindow();
            crowd.installed = false;
            yield return Frames(4);
            foreach (string screenshot in Result.screenshots)
                Require("Screenshot written: " + Path.GetFileName(screenshot), File.Exists(screenshot) && new FileInfo(screenshot).Length > 0);
            Require("No runtime error/exception logged during QA", Result.errors.Count == 0, string.Join("\n", Result.errors));
        }

        static Vector2 SwitchPoint(CaseMenuButton button)
        {
            var point = menu.menuCamera.WorldToScreenPoint(button.transform.position + Vector3.back * .025f);
            Require("Switch projects inside view: " + button.name, point.z > 0 && point.x > 0 && point.x < Screen.width
                && point.y > 0 && point.y < Screen.height, point.ToString());
            return point;
        }
        static int Count<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include).Length;
        static void Pointer(Vector2 position, bool pressed)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, delta = position - queuedPointer }
                .WithButton(MouseButton.Left, pressed));
            queuedPointer = position;
        }
        static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        static IEnumerator Frames(int frames) { for (int i = 0; i < frames; i++) yield return null; }
        static IEnumerator Seconds(float seconds)
        {
            double end = EditorApplication.timeSinceStartup + seconds;
            while (EditorApplication.timeSinceStartup < end) yield return null;
        }
        static IEnumerator Until(Func<bool> predicate, float timeout, string label)
        {
            double end = EditorApplication.timeSinceStartup + timeout;
            while (!predicate() && EditorApplication.timeSinceStartup < end) yield return null;
            Require(label, predicate());
        }
        static void Capture(string name)
        {
            string path = Path.Combine(OutputDirectory, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Result.screenshots.Add(path); Save();
        }
        static void Phase(string phase) { Result.phase = phase; Save(); }
        static void Record(string name, bool passed, string detail = "")
        {
            Result.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (passed) Result.passed++; else Result.failed++;
            Save();
        }
        static void Require(string name, bool passed, string detail = "")
        {
            Record(name, passed, detail);
            if (!passed) throw new InvalidOperationException(name + ": " + detail);
        }
        static void Advance()
        {
            if (!Running) return;
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - startedAt > 45)
            { Record("QA interrupted or exceeded 45 second deadline", false, Result.phase); Finish(false); return; }
            if (previousFrame == Time.frameCount) return;
            previousFrame = Time.frameCount;
            if (fanSource != null)
            {
                Result.fanPeakVolume = Mathf.Max(Result.fanPeakVolume, fanSource.volume);
                Result.fanPeakPitch = Mathf.Max(Result.fanPeakPitch, fanSource.pitch);
            }
            try
            {
                while (routines.Count > 0)
                {
                    var routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
                Finish(true);
            }
            catch (Exception error) { Record("QA exception", false, error.ToString()); Finish(false); }
        }
        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (Running && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                Result.errors.Add(condition + "\n" + stackTrace);
        }
        static void OnMode(PlayModeStateChange state)
        {
            if (Running && state == PlayModeStateChange.ExitingPlayMode)
            { Record("Play stopped before QA completed", false, Result.phase); Finish(false); }
        }
        static void OnReload()
        {
            if (Running) { Record("Assembly reload interrupted QA", false, Result.phase); Finish(false); }
        }
        static void Finish(bool completed)
        {
            if (cleaningUp || !Running) return;
            cleaningUp = true;
            EditorApplication.update -= Advance;
            EditorApplication.playModeStateChanged -= OnMode;
            AssemblyReloadEvents.beforeAssemblyReload -= OnReload;
            Application.logMessageReceived -= OnLog;
            try
            {
                if (mouse != null && mouse.added) Pointer(savedPointer, false);
                if (keyboard != null && keyboard.added) Keys();
                if (runtime != null && runtimeFlagsCaptured) { runtime.useDiskSave = previousDiskSave; runtime.tickEnabled = previousTick; }
                if (pausedNarrative)
                {
                    if (story != null) story.enabled = previousStoryEnabled;
                    if (prologue != null) prologue.enabled = previousPrologueEnabled;
                }
                AudioListener.volume = savedVolume;
                if (volumeExisted) PlayerPrefs.SetFloat(CaseMenuController.VolumePref, volumePref); else PlayerPrefs.DeleteKey(CaseMenuController.VolumePref);
                if (motionExisted) PlayerPrefs.SetInt(CaseMenuController.ReduceMotionPref, motionPref); else PlayerPrefs.DeleteKey(CaseMenuController.ReduceMotionPref);
                if (fullscreenExisted) PlayerPrefs.SetInt(CaseMenuController.FullscreenPref, fullscreenPref); else PlayerPrefs.DeleteKey(CaseMenuController.FullscreenPref);
                PlayerPrefs.Save();
                // On a pre-handoff failure also restore the still-live menu's reduced-motion state.
                if (menu != null)
                {
                    menu.SetReducedMotion(motionPref != 0);
                    if (!motionExisted) PlayerPrefs.DeleteKey(CaseMenuController.ReduceMotionPref);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception error) { Result.failed++; Result.errors.Add("Cleanup: " + error); }
            finally
            {
                routines.Clear(); Running = false;
                Result.complete = completed && Result.failed == 0;
                Result.elapsedSeconds = (float)(EditorApplication.timeSinceStartup - startedAt);
                Result.phase = Result.complete ? "completed" : "failed";
                Save();
                LingGuangV05.Editor.ChapterOneQaReloadGuard.Release();
            }
            Debug.Log("[CaseMenu QA] " + Result.passed + " passed / " + Result.failed + " failed. " + OutputDirectory);
        }
        static void Save()
        {
            if (Result != null && !string.IsNullOrEmpty(OutputDirectory))
                File.WriteAllText(Path.Combine(OutputDirectory, "report.json"), JsonUtility.ToJson(Result, true));
        }
    }
}
#endif
